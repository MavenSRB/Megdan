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
        Faulted = 7,

        /// <summary>
        /// Milestone 8.33. The one recoverable failure state: this seat detected a gap in the
        /// host's ordered stream, its view is known stale, and it has asked for one bounded
        /// current-projection resync. Still seated, still connected; it neither submits nor
        /// trusts its projection until the next complete projection returns it to
        /// <see cref="Seated"/>.
        /// </summary>
        Resynchronizing = 8,

        /// <summary>
        /// Milestone 8.34. The authority this seat was playing under is gone: the connection ended
        /// while the seat was held, without the host saying Goodbye and without refusing anything.
        /// </summary>
        /// <remarks>
        /// <para>Terminal, in the sense the decision record's host-loss row means it: nothing is
        /// submitted from here, no continuation is claimed, no authority migrates, and a surface
        /// showing this state carries no control a player could still press. What the seat last
        /// saw is kept, because a player is entitled to see the position they were in; what they
        /// cannot do is act on it.</para>
        /// <para>Leaving this state is not resuming from it. <see cref="PrivateSessionClient
        /// .BeginReconnect"/> starts a complete admission handshake again, and what comes back is
        /// a whole freshly encoded projection rather than any part of what was held here.</para>
        /// </remarks>
        HostLost = 9
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

        /// <summary>
        /// Milestone 8.34. The capability the host issued with this seat's grant, held here and
        /// nowhere else. It is deliberately not a property: nothing outside this machine needs to
        /// read it, and a surface that could read it could log it.
        /// </summary>
        private string reconnectCapability = string.Empty;
        private bool hasHeldSeat;

        /// <summary>
        /// Milestone 8.34. Whether the host has named a refusal on this connection. A connection
        /// the host refused and closed ended because it was told no, which is not the same event
        /// as the host disappearing, however similar the socket looks afterwards.
        /// </summary>
        private bool refusalReceived;

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
        /// Milestone 8.35a. Whether this session has left the lobby, learned from the host's
        /// announcement or - for a client that was not there to hear it - from the admission
        /// answer it re-ran on the way back.
        /// </summary>
        public bool HasMatchStarted { get; private set; }

        /// <summary>
        /// Milestone 8.35a. Whether this seat is ready, as the host's own roster states it.
        /// </summary>
        /// <remarks>
        /// Read from the roster rather than remembered from the request, so what a surface shows
        /// is the public answer six people share rather than one process's belief about it. A
        /// client that has asked and not yet been answered reads the previous value, which is the
        /// truth at that moment.
        /// </remarks>
        public bool IsReady
        {
            get
            {
                foreach (var entry in roster)
                {
                    if (entry.Seat.Equals(targetSeat))
                    {
                        return entry.IsReady;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Milestone 8.32. This seat's latest owner-scoped projection as the host pushed it, or
        /// <c>null</c> before the first push. Everything a surface may show or submit is in here;
        /// nothing else about the match ever reaches this process.
        /// </summary>
        public NetworkSeatProjectionRecord LatestProjection { get; private set; }

        /// <summary>Milestone 8.32. The host's answer to this client's newest submission.</summary>
        public NetworkSubmissionResultMessage LastSubmissionResult { get; private set; }

        /// <summary>Milestone 8.33. The host's answer to this client's newest agreement statement.</summary>
        public NetworkAgreementResultMessage LastAgreementResult { get; private set; }

        /// <summary>
        /// Milestone 8.33. Stale or replayed host frames set aside unapplied - each one a
        /// duplicate the ordered stream already delivered. A discard is convergence, not damage.
        /// </summary>
        public int DiscardedFrameCount { get; private set; }

        /// <summary>
        /// Milestone 8.33. Gaps healed by the very frame that revealed them: a complete projection
        /// that arrived past a missing frame is adopted whole, because a newer complete view is
        /// exactly what a resync would have answered with.
        /// </summary>
        public int RecoveredProjectionGapCount { get; private set; }

        /// <summary>Milestone 8.33. How many bounded resync requests this client has sent.</summary>
        public int ResyncRequestCount { get; private set; }

        /// <summary>
        /// Milestone 8.34. The connection epoch this client speaks under, as the host's grant
        /// named it. Every gameplay envelope it sends carries this, and it refuses to read one
        /// that does not.
        /// </summary>
        public int ConnectionEpoch { get; private set; } =
            PrivateSessionProtocol.InitialConnectionEpoch;

        /// <summary>
        /// Milestone 8.34. Whether this machine is on its way back to a seat it already held, so a
        /// surface can say "reconnecting" rather than "joining" while the handshake runs.
        /// </summary>
        public bool IsReconnecting { get; private set; }

        /// <summary>
        /// Milestone 8.34. Whether a return is even possible: this client held a seat, was issued
        /// a capability for it, and is in a state a fresh admission may be started from.
        /// </summary>
        /// <remarks>
        /// A refusal is not one of those states. A host that said no - to the tuple, to the key,
        /// or to the reclaim itself - has answered; asking again with the same answer to give is
        /// not a recovery, and <see cref="PrivateSessionClientState.Faulted"/> is worse, because
        /// nothing that stream said is trusted any more.
        /// </remarks>
        public bool CanAttemptReconnect =>
            reconnectCapability.Length > 0 &&
            (State == PrivateSessionClientState.HostLost ||
                State == PrivateSessionClientState.Disconnected);

        /// <summary>
        /// Milestone 8.34. Arms a complete second admission for the seat this client already held,
        /// to be driven by a new transport connection: the machine returns to its opening state,
        /// its sealed channel and both sequence counters are dropped because the connection they
        /// belonged to is gone, and the claim it will make is a reclaim carrying the capability.
        /// </summary>
        /// <returns>Whether a return could be armed at all.</returns>
        /// <remarks>
        /// The last projection is kept until the host replaces it. It is not carried into the new
        /// session as state - the host sends a complete projection and this machine adopts that
        /// whole - it is simply the last thing this player was shown, and blanking the board on
        /// the way back would tell them less than leaving it.
        /// </remarks>
        public bool BeginReconnect()
        {
            if (!CanAttemptReconnect)
            {
                return false;
            }

            State = PrivateSessionClientState.None;
            IsReconnecting = true;
            channel = null;
            gameplaySendSequence = 0;
            gameplayReceiveSequence = 0;
            RejectionReason = SessionRejectionReason.None;
            RejectionDetail = string.Empty;
            refusalReceived = false;
            return true;
        }

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
                State == PrivateSessionClientState.Faulted ||
                State == PrivateSessionClientState.HostLost)
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
                refusalReceived = true;
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
                    State != PrivateSessionClientState.Seated &&
                    State != PrivateSessionClientState.Resynchronizing)
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

            // Milestone 8.35a. The lobby is over. It is a statement about the seats, so only a
            // seated connection is sent one; hearing it in any other state means this stream is
            // not the stream this machine thinks it is.
            if (frame.Kind == SessionMessageKind.MatchStarted)
            {
                if (State != PrivateSessionClientState.Seated &&
                    State != PrivateSessionClientState.Resynchronizing)
                {
                    Fault();
                    return;
                }

                if (!PrivateSessionFrameCodec.TryDecodeMatchStarted(frame.Payload))
                {
                    Fault();
                    return;
                }

                HasMatchStarted = true;
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
                    // Milestone 8.35a. The admission answer says which phase the session is in, so
                    // a returning player knows before it claims anything that the lobby is over.
                    HasMatchStarted = welcome.MatchStarted;
                    // Milestone 8.34. A client that already held this seat asks for it back by
                    // presenting the capability, because an ordinary claim of a reserved seat is
                    // refused - and must stay refused, or a reservation would be anybody's.
                    Send(IsReconnecting && reconnectCapability.Length > 0
                        ? PrivateSessionFrameCodec.ReclaimSeatFrame(
                            new SeatReclaimMessage(targetSeat, reconnectCapability))
                        : PrivateSessionFrameCodec.ClaimSeatFrame(new SeatClaimMessage(targetSeat)));
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
                    // Milestone 8.34. The grant is where this connection learns which epoch it
                    // speaks under and how it would come back; both are the host's to say, and
                    // both arrive here and nowhere else.
                    ConnectionEpoch = granted.ConnectionEpoch;
                    if (granted.ReconnectCapability.Length > 0)
                    {
                        reconnectCapability = granted.ReconnectCapability;
                    }

                    hasHeldSeat = true;
                    IsReconnecting = false;
                    State = PrivateSessionClientState.Seated;
                    return;

                case PrivateSessionClientState.Seated:
                case PrivateSessionClientState.Resynchronizing:
                    HandleGameplayFrame(frame);
                    return;

                default:
                    Fault();
                    return;
            }
        }

        /// <summary>
        /// Milestone 8.33. The one ordered-stream discipline for every gameplay frame: a stale
        /// sequence is a duplicate or replay and is discarded unapplied; the next sequence is
        /// applied; and a gap is an explicit recovery, never a silent continuation - a complete
        /// projection past the gap converges on its own, while a result past the gap means the
        /// projection this seat holds is stale, so it asks for one bounded resync and stops
        /// acting until a complete projection arrives.
        /// </summary>
        private void HandleGameplayFrame(SessionFrame frame)
        {
            if (frame.Kind == SessionMessageKind.SeatProjection)
            {
                if (!NetworkMessageCodec.TryDecodeSeatProjection(frame.Payload, out var projection))
                {
                    Fault();
                    return;
                }

                var reading = ReadEnvelope(projection.Envelope);
                if (reading == EnvelopeReading.Invalid)
                {
                    Fault();
                    return;
                }

                if (reading == EnvelopeReading.Stale)
                {
                    DiscardedFrameCount++;
                    return;
                }

                if (!NetworkSeatProjectionCodec.TryDecode(projection.ProjectionPayload, out var record))
                {
                    Fault();
                    return;
                }

                if (reading == EnvelopeReading.Gap)
                {
                    RecoveredProjectionGapCount++;
                }

                gameplayReceiveSequence = projection.Envelope.Sequence;
                LatestProjection = record;
                if (State == PrivateSessionClientState.Resynchronizing)
                {
                    State = PrivateSessionClientState.Seated;
                }

                return;
            }

            if (frame.Kind == SessionMessageKind.SubmissionResult)
            {
                if (!NetworkMessageCodec.TryDecodeSubmissionResult(frame.Payload, out var result))
                {
                    Fault();
                    return;
                }

                var reading = ReadEnvelope(result.Envelope);
                if (reading == EnvelopeReading.Invalid)
                {
                    Fault();
                    return;
                }

                if (reading == EnvelopeReading.Stale)
                {
                    DiscardedFrameCount++;
                    return;
                }

                gameplayReceiveSequence = result.Envelope.Sequence;
                LastSubmissionResult = result;
                if (reading == EnvelopeReading.Gap)
                {
                    BeginResync();
                }

                return;
            }

            if (frame.Kind == SessionMessageKind.AgreementResult)
            {
                if (!NetworkMessageCodec.TryDecodeAgreementResult(frame.Payload, out var result))
                {
                    Fault();
                    return;
                }

                var reading = ReadEnvelope(result.Envelope);
                if (reading == EnvelopeReading.Invalid)
                {
                    Fault();
                    return;
                }

                if (reading == EnvelopeReading.Stale)
                {
                    DiscardedFrameCount++;
                    return;
                }

                gameplayReceiveSequence = result.Envelope.Sequence;
                LastAgreementResult = result;
                if (reading == EnvelopeReading.Gap)
                {
                    BeginResync();
                }

                return;
            }

            Fault();
        }

        /// <summary>
        /// Enters the recoverable state and asks for one bounded resync. One request per episode:
        /// a second gap while one is outstanding adds nothing the first answer will not heal.
        /// </summary>
        private void BeginResync()
        {
            if (State == PrivateSessionClientState.Resynchronizing)
            {
                return;
            }

            State = PrivateSessionClientState.Resynchronizing;
            ResyncRequestCount++;
            gameplaySendSequence++;
            Send(NetworkMessageCodec.RequestResyncFrame(new NetworkResyncRequestMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence))));
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

            submissionId = MintSubmissionId();
            gameplaySendSequence++;
            Send(NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version,
                NetworkCommandCodec.Encode(command))));
            return true;
        }

        /// <summary>
        /// Milestone 8.33. Submits one public agreement statement from this seat's own latest
        /// projected agreement surface, under exactly the command-submission discipline: the
        /// wire's own submission identity, the version the statement was decided against, and a
        /// refusal to act at all while the view is known stale.
        /// </summary>
        public bool TrySubmitAgreement(TeamAgreementCommand statement, out string submissionId)
        {
            submissionId = null;
            if (statement == null) throw new ArgumentNullException(nameof(statement));
            if (State != PrivateSessionClientState.Seated || LatestProjection == null)
            {
                return false;
            }

            submissionId = MintSubmissionId();
            gameplaySendSequence++;
            Send(NetworkMessageCodec.SubmitAgreementFrame(new NetworkAgreementSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version,
                statement)));
            return true;
        }

        /// <summary>
        /// Milestone 8.34. Sends one submission again under the identity it was first sent with:
        /// the retry a client makes when a lost connection took the answer with it.
        /// </summary>
        /// <remarks>
        /// <para>Keeping the identity is the whole point. A retry under a new name is a second
        /// question and the host would be right to execute it; a retry under the same name is the
        /// same question, and the host answers it from what it already did - the acceptance, or
        /// the refusal - without running anything twice. That is the failure policy's first row,
        /// and milestone 8.33's measured retry distance is what says the answer is still
        /// retained when a reconnect gets around to asking.</para>
        /// <para>The expected version is this seat's current one, not the one the lost attempt
        /// carried: a returning client has just been given a complete projection, and a retry that
        /// insisted on the older number would be answered as stale by a host that has already
        /// decided the question.</para>
        /// </remarks>
        public bool TryResubmitCommand(DirectorCommand command, string submissionId)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                throw new ArgumentException(
                    "A retry names the identity it is retrying.", nameof(submissionId));
            }

            if (State != PrivateSessionClientState.Seated || LatestProjection == null)
            {
                return false;
            }

            gameplaySendSequence++;
            Send(NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version,
                NetworkCommandCodec.Encode(command))));
            return true;
        }

        /// <summary>Milestone 8.34. The agreement half of <see cref="TryResubmitCommand"/>.</summary>
        public bool TryResubmitAgreement(TeamAgreementCommand statement, string submissionId)
        {
            if (statement == null) throw new ArgumentNullException(nameof(statement));
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                throw new ArgumentException(
                    "A retry names the identity it is retrying.", nameof(submissionId));
            }

            if (State != PrivateSessionClientState.Seated || LatestProjection == null)
            {
                return false;
            }

            gameplaySendSequence++;
            Send(NetworkMessageCodec.SubmitAgreementFrame(new NetworkAgreementSubmissionMessage(
                new NetworkEnvelope(
                    SessionCorrelation,
                    ConnectionEpoch,
                    gameplaySendSequence,
                    gameplaySendSequence),
                submissionId,
                LatestProjection.Version,
                statement)));
            return true;
        }

        private string MintSubmissionId()
        {
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

            return builder.ToString();
        }

        /// <summary>
        /// Milestone 8.35a. Tells the host this seat is ready, or that it is not.
        /// </summary>
        /// <remarks>
        /// <para>It says nothing about whether the seat then <em>is</em> ready:
        /// <see cref="IsReady"/> answers that, from the roster the host publishes, which is the
        /// only place readiness exists. A surface that lit its own control on this return value
        /// would be showing one process's belief instead of the table's shared state.</para>
        /// <para>Refused before it is sent from any state but <see cref="PrivateSessionClientState
        /// .Seated"/> and after the match has begun: readiness is a lobby statement about a held
        /// seat, and a client is the first thing that knows it is in neither position.</para>
        /// </remarks>
        public bool TrySetReadiness(bool isReady)
        {
            if (State != PrivateSessionClientState.Seated || HasMatchStarted)
            {
                return false;
            }

            Send(PrivateSessionFrameCodec.SetReadinessFrame(new SeatReadinessMessage(isReady)));
            return true;
        }

        /// <summary>An orderly close; plaintext by design, like the host's own Goodbye.</summary>
        public void SendGoodbye() => outbox.Send(PrivateSessionFrameCodec.GoodbyeFrame());

        private enum EnvelopeReading
        {
            /// <summary>Wrong session or epoch: a protocol violation, not a delivery fault.</summary>
            Invalid = 0,

            /// <summary>Exactly the next sequence.</summary>
            Next = 1,

            /// <summary>Past the next sequence: something between was never delivered.</summary>
            Gap = 2,

            /// <summary>At or before the last accepted sequence: a duplicate or a replay.</summary>
            Stale = 3
        }

        private EnvelopeReading ReadEnvelope(NetworkEnvelope envelope)
        {
            if (!string.Equals(envelope.SessionCorrelation, SessionCorrelation, StringComparison.Ordinal) ||
                envelope.Epoch != ConnectionEpoch)
            {
                return EnvelopeReading.Invalid;
            }

            if (envelope.Sequence <= gameplayReceiveSequence)
            {
                return EnvelopeReading.Stale;
            }

            return envelope.Sequence == gameplayReceiveSequence + 1
                ? EnvelopeReading.Next
                : EnvelopeReading.Gap;
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

        /// <summary>
        /// The transport says the connection ended.
        /// </summary>
        /// <remarks>
        /// Milestone 8.34 separates the two ways that happens. A connection that ends after an
        /// orderly close, a refusal, or before this client ever held a seat is simply
        /// <see cref="PrivateSessionClientState.Disconnected"/> - nothing was lost, and the
        /// decision record closes a pre-claim connection silently. A connection that ends while
        /// the seat was held, with nothing said, is the host-loss row: the authority this seat was
        /// playing under is gone as far as this process can tell, and
        /// <see cref="PrivateSessionClientState.HostLost"/> is the terminal state the record names
        /// for it.
        /// </remarks>
        public void OnDisconnected()
        {
            if (State == PrivateSessionClientState.Rejected ||
                State == PrivateSessionClientState.Faulted ||
                State == PrivateSessionClientState.Disconnected ||
                State == PrivateSessionClientState.HostLost)
            {
                return;
            }

            State = hasHeldSeat && !refusalReceived
                ? PrivateSessionClientState.HostLost
                : PrivateSessionClientState.Disconnected;
        }

        private void Fault()
        {
            State = PrivateSessionClientState.Faulted;
            outbox.Close();
        }
    }
}
