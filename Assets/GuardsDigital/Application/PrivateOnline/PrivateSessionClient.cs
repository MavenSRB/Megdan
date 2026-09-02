using System;
using System.Collections.Generic;
using System.Globalization;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>The client half of <see cref="IPrivateSessionOutbox"/>: one connection, no ordinal.</summary>
    public interface IPrivateSessionClientOutbox
    {
        void Send(byte[] frame);
        void Close();
    }

    /// <summary>Where a client's session establishment stands.</summary>
    public enum PrivateSessionClientState
    {
        None = 0,

        /// <summary>The transport reported the connection open; the host speaks first.</summary>
        AwaitingChallenge = 1,

        /// <summary>Hello sent; the host decides admission.</summary>
        AwaitingWelcome = 2,

        /// <summary>Admitted and the claim sent; the host decides the seat.</summary>
        AwaitingSeat = 3,

        /// <summary>The target seat is held. 8.31's terminal success.</summary>
        Seated = 4,

        /// <summary>The host refused; <see cref="PrivateSessionClient.RejectionReason"/> says why.</summary>
        Rejected = 5,

        /// <summary>The connection ended without a refusal.</summary>
        Disconnected = 6,

        /// <summary>The host's own stream violated the protocol; nothing further is trusted.</summary>
        Faulted = 7
    }

    /// <summary>
    /// Milestone 8.31. One client's session state machine: answer the challenge, present the
    /// tuple, claim the one configured seat, and hold it.
    /// </summary>
    /// <remarks>
    /// Transport-neutral and single-threaded by the same pump contract as the host. The seat it
    /// claims is fixed at construction - a client is one player sitting down at one place - and a
    /// grant naming any other seat is a fault, not a fallback.
    /// </remarks>
    public sealed class PrivateSessionClient
    {
        private readonly PrivateSessionIdentity identity;
        private readonly SeatId targetSeat;
        private readonly string sessionKey;
        private readonly IPrivateSessionClientOutbox outbox;
        private readonly IPrivateSessionNonceSource nonceSource;
        private readonly List<SeatRosterEntry> roster = new List<SeatRosterEntry>();
        private PrivateSessionSecureChannel channel;
        private long gameplaySendSequence;
        private long gameplayReceiveSequence;

        public PrivateSessionClient(
            PrivateSessionIdentity identity,
            SeatId targetSeat,
            string sessionKey,
            IPrivateSessionClientOutbox outbox,
            IPrivateSessionNonceSource nonceSource = null)
        {
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            if (!targetSeat.IsDefined)
            {
                throw new ArgumentException("A client claims one configured seat.", nameof(targetSeat));
            }

            this.targetSeat = targetSeat;
            this.sessionKey = sessionKey ?? string.Empty;
            this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            this.nonceSource = nonceSource;
        }

        public PrivateSessionClientState State { get; private set; }

        public SeatId TargetSeat => targetSeat;

        /// <summary>Why the host refused, when <see cref="State"/> is <c>Rejected</c>.</summary>
        public SessionRejectionReason RejectionReason { get; private set; }

        /// <summary>The host's bounded rejection detail, empty otherwise.</summary>
        public string RejectionDetail { get; private set; } = string.Empty;

        /// <summary>The host's opaque session correlation, known from admission on.</summary>
        public string SessionCorrelation { get; private set; } = string.Empty;

        /// <summary>The granted seat's identity row, known from seating on.</summary>
        public SeatRosterEntry GrantedSeat { get; private set; }

        /// <summary>The latest roster the host published.</summary>
        public IReadOnlyList<SeatRosterEntry> Roster => roster;

        /// <summary>
        /// Milestone 8.32. This seat's latest owner-scoped projection as the host pushed it, or
        /// <c>null</c> before the first push. Everything a surface may show or submit is in here;
        /// nothing else about the match ever reaches this process.
        /// </summary>
        public NetworkSeatProjectionRecord LatestProjection { get; private set; }

        /// <summary>Milestone 8.32. The host's answer to this client's newest submission.</summary>
        public NetworkSubmissionResultMessage LastSubmissionResult { get; private set; }

        public void OnConnected()
        {
            if (State != PrivateSessionClientState.None)
            {
                return;
            }

            State = PrivateSessionClientState.AwaitingChallenge;
        }

        public void OnFrame(SessionFrame frame)
        {
            if (State == PrivateSessionClientState.Rejected ||
                State == PrivateSessionClientState.Disconnected ||
                State == PrivateSessionClientState.Faulted)
            {
                return;
            }

            // Milestone 8.32. The client's half of the seal discipline: once it has answered the
            // challenge on a keyed session, only sealed frames and plaintext Reject or Goodbye are
            // trusted from the host - anything else is an injection or a broken peer.
            if (frame.Kind == SessionMessageKind.Sealed)
            {
                if (channel == null || !channel.TryUnseal(frame.Payload, out frame))
                {
                    Fault();
                    return;
                }
            }
            else if (channel != null &&
                frame.Kind != SessionMessageKind.Reject &&
                frame.Kind != SessionMessageKind.Goodbye)
            {
                Fault();
                return;
            }

            if (frame.Kind == SessionMessageKind.Reject)
            {
                if (!PrivateSessionFrameCodec.TryDecodeReject(frame.Payload, out var reject))
                {
                    Fault();
                    return;
                }

                RejectionReason = reject.Reason;
                RejectionDetail = reject.Detail;
                if (State != PrivateSessionClientState.Seated)
                {
                    // A refusal after seating - a refused seat change - is recorded but does not
                    // unseat the client; the host keeps the seat map unchanged and so does this.
                    State = PrivateSessionClientState.Rejected;
                }

                return;
            }

            if (frame.Kind == SessionMessageKind.Goodbye)
            {
                State = PrivateSessionClientState.Disconnected;
                return;
            }

            if (frame.Kind == SessionMessageKind.RosterUpdate)
            {
                if (State != PrivateSessionClientState.AwaitingSeat &&
                    State != PrivateSessionClientState.Seated)
                {
                    Fault();
                    return;
                }

                if (!PrivateSessionFrameCodec.TryDecodeRosterUpdate(frame.Payload, out var update))
                {
                    Fault();
                    return;
                }

                roster.Clear();
                roster.AddRange(update.Roster);
                return;
            }

            switch (State)
            {
                case PrivateSessionClientState.AwaitingChallenge:
                    if (frame.Kind != SessionMessageKind.Challenge ||
                        !PrivateSessionFrameCodec.TryDecodeChallenge(frame.Payload, out var challenge))
                    {
                        Fault();
                        return;
                    }

                    // The Hello itself is plaintext - the host cannot unseal before it has
                    // verified the proof inside it - and everything after it travels sealed on a
                    // keyed session, both ways.
                    outbox.Send(PrivateSessionFrameCodec.HelloFrame(new SessionHelloMessage(
                        identity, PrivateSessionKeyProof.Compute(challenge.Nonce, sessionKey))));
                    if (sessionKey.Length > 0 && challenge.Nonce.Length > 0)
                    {
                        channel = PrivateSessionSecureChannel.ForClient(sessionKey, challenge.Nonce);
                    }

                    State = PrivateSessionClientState.AwaitingWelcome;
                    return;

                case PrivateSessionClientState.AwaitingWelcome:
                    if (frame.Kind != SessionMessageKind.Welcome ||
                        !PrivateSessionFrameCodec.TryDecodeWelcome(frame.Payload, out var welcome))
                    {
                        Fault();
                        return;
                    }

                    SessionCorrelation = welcome.SessionCorrelation;
                    roster.Clear();
                    roster.AddRange(welcome.Roster);
                    Send(PrivateSessionFrameCodec.ClaimSeatFrame(new SeatClaimMessage(targetSeat)));
                    State = PrivateSessionClientState.AwaitingSeat;
                    return;

                case PrivateSessionClientState.AwaitingSeat:
                    if (frame.Kind != SessionMessageKind.SeatGranted ||
                        !PrivateSessionFrameCodec.TryDecodeSeatGranted(frame.Payload, out var granted))
                    {
                        Fault();
                        return;
                    }

                    if (!granted.Seat.Seat.Equals(targetSeat))
                    {
                        Fault();
                        return;
                    }

                    GrantedSeat = granted.Seat;
                    State = PrivateSessionClientState.Seated;
                    return;

                case PrivateSessionClientState.Seated:
                    if (frame.Kind == SessionMessageKind.SeatProjection)
                    {
                        if (!NetworkMessageCodec.TryDecodeSeatProjection(frame.Payload, out var projection) ||
                            !AcceptEnvelope(projection.Envelope) ||
                            !NetworkSeatProjectionCodec.TryDecode(
                                projection.ProjectionPayload, out var record))
                        {
                            Fault();
                            return;
                        }

                        LatestProjection = record;
                        return;
                    }

                    if (frame.Kind == SessionMessageKind.SubmissionResult)
                    {
                        if (!NetworkMessageCodec.TryDecodeSubmissionResult(frame.Payload, out var result) ||
                            !AcceptEnvelope(result.Envelope))
                        {
                            Fault();
                            return;
                        }

                        LastSubmissionResult = result;
                        return;
                    }

                    Fault();
                    return;

                default:
                    Fault();
                    return;
            }
        }

        /// <summary>
        /// Milestone 8.32. Submits one command from this seat's own latest projection: the
        /// submission identity is the wire's own - drawn from the injected nonce source, never
        /// composed by presentation - and the expected version is the projection the answer was
        /// decided against.
        /// </summary>
        public bool TrySubmitCommand(DirectorCommand command, out string submissionId)
        {
            submissionId = null;
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (State != PrivateSessionClientState.Seated || LatestProjection == null)
            {
                return false;
            }

            if (nonceSource == null)
            {
                throw new InvalidOperationException(
                    "Submitting requires a nonce source for the wire's own submission identities.");
            }

            var nonce = nonceSource.NextNonce() ?? Array.Empty<byte>();
            var builder = new System.Text.StringBuilder("wire-", 5 + nonce.Length * 2);
            foreach (var value in nonce)
            {
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            submissionId = builder.ToString();
            gameplaySendSequence++;
            Send(NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    PrivateSessionProtocol.InitialConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version,
                NetworkCommandCodec.Encode(command))));
            return true;
        }

        /// <summary>An orderly close; plaintext by design, like the host's own Goodbye.</summary>
        public void SendGoodbye() => outbox.Send(PrivateSessionFrameCodec.GoodbyeFrame());

        private bool AcceptEnvelope(NetworkEnvelope envelope)
        {
            if (!string.Equals(envelope.SessionCorrelation, SessionCorrelation, StringComparison.Ordinal) ||
                envelope.Epoch != PrivateSessionProtocol.InitialConnectionEpoch ||
                envelope.Sequence != gameplayReceiveSequence + 1)
            {
                return false;
            }

            gameplayReceiveSequence = envelope.Sequence;
            return true;
        }

        private void Send(byte[] frame)
        {
            if (channel == null)
            {
                outbox.Send(frame);
                return;
            }

            var kind = (SessionMessageKind)PrivateSessionFrameCodec.ReadUInt16(frame, 6);
            var payload = new byte[frame.Length - PrivateSessionProtocol.FrameHeaderBytes];
            Array.Copy(frame, PrivateSessionProtocol.FrameHeaderBytes, payload, 0, payload.Length);
            outbox.Send(channel.Seal(kind, payload));
        }

        /// <summary>The transport found the host's byte stream itself invalid.</summary>
        public void OnStreamRejected(SessionRejectionReason reason)
        {
            if (State == PrivateSessionClientState.Rejected)
            {
                return;
            }

            RejectionReason = reason;
            Fault();
        }

        public void OnDisconnected()
        {
            if (State == PrivateSessionClientState.Rejected ||
                State == PrivateSessionClientState.Faulted)
            {
                return;
            }

            State = PrivateSessionClientState.Disconnected;
        }

        private void Fault()
        {
            State = PrivateSessionClientState.Faulted;
            outbox.Close();
        }
    }
}
