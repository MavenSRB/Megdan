using System;
using System.Collections.Generic;
using System.Globalization;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>How the session layer sends and closes; implemented by the transport adapter.</summary>
    /// <remarks>
    /// The session state machines name only this, so they compile and test with no socket in
    /// sight. <see cref="Close"/> is a request: the adapter flushes what was sent, closes that one
    /// connection, and eventually reports <c>OnDisconnected</c> back.
    /// </remarks>
    public interface IPrivateSessionOutbox
    {
        void Send(int connectionOrdinal, byte[] frame);
        void Close(int connectionOrdinal);
    }

    /// <summary>
    /// Where a challenge nonce comes from. The Application assembly draws no platform entropy -
    /// the setup-randomness conformance rule scans for exactly that - so the adapter supplies it.
    /// </summary>
    public interface IPrivateSessionNonceSource
    {
        byte[] NextNonce();
    }

    /// <summary>What happened inside the session host, for a driver's bounded diagnostics.</summary>
    public enum PrivateSessionEventKind
    {
        None = 0,
        ConnectionOpened = 1,
        Admitted = 2,
        SeatClaimed = 3,
        SeatReserved = 4,
        Rejected = 5,
        Disconnected = 6,
        SessionComplete = 7
    }

    /// <summary>
    /// One session-host event. Carries the opaque connection ordinal, the seat where one is
    /// involved, and the rejection reason where there is one - and deliberately nothing else: no
    /// address, no hand, no other seat's state.
    /// </summary>
    public sealed class PrivateSessionEventRecord
    {
        public PrivateSessionEventRecord(
            PrivateSessionEventKind kind,
            int connectionOrdinal,
            SeatId seat,
            HeroId hero,
            TeamSide teamSide,
            SessionRejectionReason reason,
            int claimedSeats,
            int totalSeats)
        {
            Kind = kind;
            ConnectionOrdinal = connectionOrdinal;
            Seat = seat;
            Hero = hero;
            TeamSide = teamSide;
            Reason = reason;
            ClaimedSeats = claimedSeats;
            TotalSeats = totalSeats;
        }

        public PrivateSessionEventKind Kind { get; }
        public int ConnectionOrdinal { get; }
        public SeatId Seat { get; }
        public HeroId Hero { get; }
        public TeamSide TeamSide { get; }
        public SessionRejectionReason Reason { get; }
        public int ClaimedSeats { get; }
        public int TotalSeats { get; }

        public override string ToString() =>
            Kind + " ordinal=" + ConnectionOrdinal.ToString(CultureInfo.InvariantCulture) +
            (Seat.IsDefined ? " seat=" + Seat.Value : string.Empty) +
            (Reason != SessionRejectionReason.None ? " reason=" + Reason : string.Empty);
    }

    /// <summary>
    /// Milestone 8.31. The dedicated host's session state machine: admission by tuple and key,
    /// then one stable seat per connection, everything else refused by name.
    /// </summary>
    /// <remarks>
    /// <para>Transport-neutral and single-threaded by contract: every call arrives on the
    /// adapter's pump thread, the discipline that keeps the serialized-executor rule true when
    /// milestone 8.32 starts submitting commands. Nothing here touches sockets, threads, clocks or
    /// entropy - time arrives as an argument and nonces from an injected source.</para>
    /// <para>It holds the <see cref="IMatchHost"/> port for exactly one thing: the authoritative
    /// six-seat binding. No submission crosses this type in 8.31; putting commands and projections
    /// on the wire is milestones 8.32-8.33.</para>
    /// <para>Seat ownership is host state bound to one live connection. A disconnected claimed
    /// seat becomes <see cref="SeatOccupancyState.Reserved"/> and stays unclaimable: with no
    /// identity to recognize a returning player, releasing it would transfer one player's seat to
    /// whoever asks next, which the decision record forbids. Milestone 8.34's reconnect work is
    /// what turns a reservation back into a claim.</para>
    /// </remarks>
    public sealed class PrivateSessionHost
    {
        private enum ConnectionPhase
        {
            AwaitingHello = 0,
            Admitted = 1,
            Seated = 2
        }

        private sealed class ConnectionRecord
        {
            public ConnectionRecord(int ordinal, long helloDeadlineTicks, byte[] nonce)
            {
                Ordinal = ordinal;
                HelloDeadlineTicks = helloDeadlineTicks;
                Nonce = nonce;
            }

            public int Ordinal { get; }
            public long HelloDeadlineTicks { get; }
            public byte[] Nonce { get; }
            public ConnectionPhase Phase { get; set; }
            public SeatId Seat { get; set; }
        }

        private sealed class SeatHold
        {
            public SeatOccupancyState State { get; set; }
            public int HeldByOrdinal { get; set; }
        }

        private const int RetainedEventCount = 1024;

        private readonly IMatchHost matchHost;
        private readonly PrivateSessionIdentity identity;
        private readonly string sessionKey;
        private readonly IPrivateSessionOutbox outbox;
        private readonly IPrivateSessionNonceSource nonceSource;
        private readonly Dictionary<int, ConnectionRecord> connections =
            new Dictionary<int, ConnectionRecord>();
        private readonly Dictionary<SeatId, SeatHold> holds = new Dictionary<SeatId, SeatHold>();
        private readonly List<PrivateSessionEventRecord> events =
            new List<PrivateSessionEventRecord>();

        public PrivateSessionHost(
            IMatchHost matchHost,
            PrivateSessionIdentity identity,
            string sessionKey,
            IPrivateSessionOutbox outbox,
            IPrivateSessionNonceSource nonceSource = null)
        {
            this.matchHost = matchHost ?? throw new ArgumentNullException(nameof(matchHost));
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.sessionKey = sessionKey ?? string.Empty;
            this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            this.nonceSource = nonceSource;

            if (this.sessionKey.Length > 0 && nonceSource == null)
            {
                throw new ArgumentException(
                    "A keyed session requires a nonce source, or every challenge would be constant.",
                    nameof(nonceSource));
            }

            foreach (var seat in matchHost.Seats.Seats)
            {
                holds[seat.Id] = new SeatHold { State = SeatOccupancyState.Open };
            }
        }

        public PrivateSessionIdentity Identity => identity;

        public int TotalSeats => matchHost.Seats.Seats.Count;

        public int ClaimedSeatCount
        {
            get
            {
                var claimed = 0;
                foreach (var hold in holds.Values)
                {
                    if (hold.State == SeatOccupancyState.Claimed)
                    {
                        claimed++;
                    }
                }

                return claimed;
            }
        }

        /// <summary>Every seat is claimed by a live connection.</summary>
        public bool IsComplete => ClaimedSeatCount == TotalSeats;

        public int ConnectionCount => connections.Count;

        /// <summary>The current public seat map, in the authoritative seat order.</summary>
        public IReadOnlyList<SeatRosterEntry> Roster
        {
            get
            {
                var entries = new List<SeatRosterEntry>(TotalSeats);
                foreach (var seat in matchHost.Seats.Seats)
                {
                    entries.Add(new SeatRosterEntry(
                        seat.Id, seat.HeroId, seat.TeamSide, holds[seat.Id].State));
                }

                return entries;
            }
        }

        public bool TryGetSeatForConnection(int connectionOrdinal, out MatchSeat seat)
        {
            seat = null;
            if (connections.TryGetValue(connectionOrdinal, out var record) &&
                record.Phase == ConnectionPhase.Seated)
            {
                seat = matchHost.Seats.Find(record.Seat);
            }

            return seat != null;
        }

        /// <summary>The events since the last drain, oldest first; draining clears them.</summary>
        public IReadOnlyList<PrivateSessionEventRecord> DrainEvents()
        {
            var drained = events.ToArray();
            events.Clear();
            return drained;
        }

        public void OnConnected(int connectionOrdinal, long nowTicks)
        {
            if (connections.Count >= PrivateSessionProtocol.MaxConcurrentConnections)
            {
                outbox.Send(connectionOrdinal, PrivateSessionFrameCodec.RejectFrame(
                    new SessionRejectMessage(
                        SessionRejectionReason.TooManyConnections,
                        "The session holds its connection bound.")));
                outbox.Close(connectionOrdinal);
                Record(PrivateSessionEventKind.Rejected, connectionOrdinal,
                    default, default, TeamSide.None, SessionRejectionReason.TooManyConnections);
                return;
            }

            var nonce = nonceSource == null ? Array.Empty<byte>() : nonceSource.NextNonce();
            var record = new ConnectionRecord(
                connectionOrdinal,
                nowTicks + PrivateSessionProtocol.HandshakeTimeoutSeconds * TimeSpan.TicksPerSecond,
                nonce ?? Array.Empty<byte>());
            connections[connectionOrdinal] = record;
            outbox.Send(connectionOrdinal, PrivateSessionFrameCodec.ChallengeFrame(
                new SessionChallengeMessage(record.Nonce)));
            Record(PrivateSessionEventKind.ConnectionOpened, connectionOrdinal,
                default, default, TeamSide.None, SessionRejectionReason.None);
        }

        public void OnFrame(int connectionOrdinal, SessionFrame frame)
        {
            if (!connections.TryGetValue(connectionOrdinal, out var record))
            {
                return;
            }

            if (frame.Kind == SessionMessageKind.Goodbye)
            {
                Depart(record);
                outbox.Close(connectionOrdinal);
                return;
            }

            switch (record.Phase)
            {
                case ConnectionPhase.AwaitingHello:
                    if (frame.Kind != SessionMessageKind.Hello)
                    {
                        RejectAndClose(record, SessionRejectionReason.UnexpectedMessage,
                            "Admission begins with Hello.");
                        return;
                    }

                    HandleHello(record, frame.Payload);
                    return;

                case ConnectionPhase.Admitted:
                    if (frame.Kind != SessionMessageKind.ClaimSeat)
                    {
                        RejectAndClose(record, SessionRejectionReason.UnexpectedMessage,
                            "An admitted connection claims a seat next.");
                        return;
                    }

                    HandleClaim(record, frame.Payload);
                    return;

                case ConnectionPhase.Seated:
                    if (frame.Kind == SessionMessageKind.ClaimSeat)
                    {
                        // Refused without disconnecting, so the rejection leaves the seat map
                        // untouched: the connection keeps the seat it already holds.
                        outbox.Send(record.Ordinal, PrivateSessionFrameCodec.RejectFrame(
                            new SessionRejectMessage(
                                SessionRejectionReason.SeatChangeRefused,
                                "A seat is stable for the session.")));
                        Record(PrivateSessionEventKind.Rejected, record.Ordinal, record.Seat,
                            default, TeamSide.None, SessionRejectionReason.SeatChangeRefused);
                        return;
                    }

                    RejectAndClose(record, SessionRejectionReason.UnexpectedMessage,
                        "No further session message is defined after seating.");
                    return;
            }
        }

        /// <summary>The adapter found this connection's byte stream itself invalid.</summary>
        public void OnStreamRejected(int connectionOrdinal, SessionRejectionReason reason)
        {
            if (!connections.TryGetValue(connectionOrdinal, out var record))
            {
                return;
            }

            RejectAndClose(record, reason, "The connection's stream was refused.");
        }

        public void OnDisconnected(int connectionOrdinal)
        {
            if (!connections.TryGetValue(connectionOrdinal, out var record))
            {
                return;
            }

            Depart(record);
        }

        /// <summary>Closes every unadmitted connection whose handshake deadline has passed.</summary>
        public void Sweep(long nowTicks)
        {
            List<ConnectionRecord> expired = null;
            foreach (var record in connections.Values)
            {
                if (record.Phase == ConnectionPhase.AwaitingHello &&
                    nowTicks > record.HelloDeadlineTicks)
                {
                    (expired = expired ?? new List<ConnectionRecord>()).Add(record);
                }
            }

            if (expired == null)
            {
                return;
            }

            foreach (var record in expired)
            {
                RejectAndClose(record, SessionRejectionReason.HandshakeTimeout,
                    "The handshake missed its deadline.");
            }
        }

        private void HandleHello(ConnectionRecord record, byte[] payload)
        {
            if (!PrivateSessionFrameCodec.TryDecodeHello(payload, out var hello))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The Hello payload did not decode.");
                return;
            }

            var mismatch = identity.FirstMismatch(hello.Identity);
            if (mismatch != SessionRejectionReason.None)
            {
                // The refusal names the failing element and the host's own value of it - which the
                // client needs to report usefully - and nothing else about the session.
                RejectAndClose(record, mismatch, DescribeMismatch(mismatch));
                return;
            }

            if (sessionKey.Length > 0)
            {
                var expected = PrivateSessionKeyProof.Compute(record.Nonce, sessionKey);
                if (!PrivateSessionKeyProof.Matches(expected, hello.KeyProof))
                {
                    RejectAndClose(record, SessionRejectionReason.SessionKeyMismatch,
                        "The key proof did not answer the challenge.");
                    return;
                }
            }

            // Full is answered at admission, before the roster is disclosed: a claim of a specific
            // taken seat gets that seat's own reason below, so this is the only place the plain
            // "there is no seat left for you" answer can live without shadowing those.
            if (!AnySeatOpen())
            {
                RejectAndClose(record, SessionRejectionReason.SessionFull,
                    "Every seat is taken or reserved.");
                return;
            }

            record.Phase = ConnectionPhase.Admitted;
            outbox.Send(record.Ordinal, PrivateSessionFrameCodec.WelcomeFrame(
                new SessionWelcomeMessage(identity.Correlation, Roster)));
            Record(PrivateSessionEventKind.Admitted, record.Ordinal,
                default, default, TeamSide.None, SessionRejectionReason.None);
        }

        private void HandleClaim(ConnectionRecord record, byte[] payload)
        {
            if (!PrivateSessionFrameCodec.TryDecodeClaimSeat(payload, out var claim))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The claim payload did not decode.");
                return;
            }

            var seat = matchHost.Seats.Find(claim.Seat);
            if (seat == null)
            {
                RejectAndClose(record, SessionRejectionReason.UnknownSeat,
                    "No seat of that id is bound in this match.");
                return;
            }

            var targetHold = holds[seat.Id];
            if (targetHold.State == SeatOccupancyState.Claimed)
            {
                RejectAndClose(record, SessionRejectionReason.SeatAlreadyClaimed,
                    "That seat is held by a live connection.");
                return;
            }

            if (targetHold.State == SeatOccupancyState.Reserved)
            {
                RejectAndClose(record, SessionRejectionReason.SeatReserved,
                    "That seat is reserved for its disconnected player.");
                return;
            }

            targetHold.State = SeatOccupancyState.Claimed;
            targetHold.HeldByOrdinal = record.Ordinal;
            record.Phase = ConnectionPhase.Seated;
            record.Seat = seat.Id;

            outbox.Send(record.Ordinal, PrivateSessionFrameCodec.SeatGrantedFrame(
                new SeatGrantedMessage(
                    new SeatRosterEntry(seat.Id, seat.HeroId, seat.TeamSide, SeatOccupancyState.Claimed),
                    ClaimedSeatCount, TotalSeats)));
            BroadcastRoster();
            Record(PrivateSessionEventKind.SeatClaimed, record.Ordinal, seat.Id, seat.HeroId,
                seat.TeamSide, SessionRejectionReason.None);
            if (IsComplete)
            {
                Record(PrivateSessionEventKind.SessionComplete, record.Ordinal,
                    default, default, TeamSide.None, SessionRejectionReason.None);
            }
        }

        /// <summary>
        /// Ends the session on purpose: every connection is told Goodbye and closed. No seat is
        /// reserved by an orderly end - there is no session left to hold a reservation.
        /// </summary>
        public void Shutdown()
        {
            var open = new List<ConnectionRecord>(connections.Values);
            foreach (var record in open)
            {
                outbox.Send(record.Ordinal, PrivateSessionFrameCodec.GoodbyeFrame());
                outbox.Close(record.Ordinal);
                connections.Remove(record.Ordinal);
            }
        }

        private bool AnySeatOpen()
        {
            foreach (var hold in holds.Values)
            {
                if (hold.State == SeatOccupancyState.Open)
                {
                    return true;
                }
            }

            return false;
        }

        private void RejectAndClose(ConnectionRecord record, SessionRejectionReason reason, string detail)
        {
            outbox.Send(record.Ordinal, PrivateSessionFrameCodec.RejectFrame(
                new SessionRejectMessage(reason, detail)));
            outbox.Close(record.Ordinal);
            Record(PrivateSessionEventKind.Rejected, record.Ordinal, record.Seat,
                default, TeamSide.None, reason);
            Depart(record);
        }

        private void Depart(ConnectionRecord record)
        {
            connections.Remove(record.Ordinal);
            if (record.Phase != ConnectionPhase.Seated)
            {
                Record(PrivateSessionEventKind.Disconnected, record.Ordinal,
                    default, default, TeamSide.None, SessionRejectionReason.None);
                return;
            }

            var hold = holds[record.Seat];
            hold.State = SeatOccupancyState.Reserved;
            hold.HeldByOrdinal = 0;
            var seat = matchHost.Seats.Find(record.Seat);
            BroadcastRoster();
            Record(PrivateSessionEventKind.SeatReserved, record.Ordinal, record.Seat,
                seat == null ? default : seat.HeroId,
                seat == null ? TeamSide.None : seat.TeamSide,
                SessionRejectionReason.None);
            Record(PrivateSessionEventKind.Disconnected, record.Ordinal,
                record.Seat, default, TeamSide.None, SessionRejectionReason.None);
        }

        private void BroadcastRoster()
        {
            var frame = PrivateSessionFrameCodec.RosterUpdateFrame(new SeatRosterMessage(Roster));
            foreach (var record in connections.Values)
            {
                if (record.Phase != ConnectionPhase.AwaitingHello)
                {
                    outbox.Send(record.Ordinal, frame);
                }
            }
        }

        private string DescribeMismatch(SessionRejectionReason mismatch)
        {
            switch (mismatch)
            {
                case SessionRejectionReason.ProtocolFamilyMismatch:
                    return "Protocol family differs; host runs " + identity.ProtocolFamily + ".";
                case SessionRejectionReason.ProtocolVersionMismatch:
                    return "Protocol version differs; host runs " +
                        identity.ProtocolVersion.ToString(CultureInfo.InvariantCulture) + ".";
                case SessionRejectionReason.BuildIdentityMismatch:
                    return "Build identity differs; host runs " + identity.BuildIdentity + ".";
                case SessionRejectionReason.SnapshotSchemaMismatch:
                    return "Snapshot schema differs; host runs " +
                        identity.SnapshotSchemaVersion.ToString(CultureInfo.InvariantCulture) + ".";
                case SessionRejectionReason.MapRevisionMismatch:
                    return "Map revision differs; host runs " + identity.MapId + "@" +
                        identity.MapContentRevision.ToString(CultureInfo.InvariantCulture) + ".";
                case SessionRejectionReason.ContentDigestMismatch:
                    return "Registered content differs; host digest " + identity.ContentDigest + ".";
                default:
                    return "The admission tuple differs.";
            }
        }

        private void Record(
            PrivateSessionEventKind kind,
            int ordinal,
            SeatId seat,
            HeroId hero,
            TeamSide teamSide,
            SessionRejectionReason reason)
        {
            if (events.Count >= RetainedEventCount)
            {
                events.RemoveAt(0);
            }

            events.Add(new PrivateSessionEventRecord(
                kind, ordinal, seat, hero, teamSide, reason, ClaimedSeatCount, TotalSeats));
        }
    }
}
