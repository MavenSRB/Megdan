using System;
using System.Collections.Generic;
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
        private readonly List<SeatRosterEntry> roster = new List<SeatRosterEntry>();

        public PrivateSessionClient(
            PrivateSessionIdentity identity,
            SeatId targetSeat,
            string sessionKey,
            IPrivateSessionClientOutbox outbox)
        {
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            if (!targetSeat.IsDefined)
            {
                throw new ArgumentException("A client claims one configured seat.", nameof(targetSeat));
            }

            this.targetSeat = targetSeat;
            this.sessionKey = sessionKey ?? string.Empty;
            this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
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

                    outbox.Send(PrivateSessionFrameCodec.HelloFrame(new SessionHelloMessage(
                        identity, PrivateSessionKeyProof.Compute(challenge.Nonce, sessionKey))));
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
                    outbox.Send(PrivateSessionFrameCodec.ClaimSeatFrame(new SeatClaimMessage(targetSeat)));
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

                default:
                    Fault();
                    return;
            }
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
