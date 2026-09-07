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
        SessionComplete = 7,

        /// <summary>Milestone 8.32. A seat's wire submission reached the authority and was accepted.</summary>
        SubmissionAccepted = 8,

        /// <summary>
        /// Milestone 8.32. A seat's wire submission was refused; the seat-level reason rides
        /// <see cref="PrivateSessionEventRecord.SubmissionRejection"/>. No command content, hand
        /// or detail text is recorded - the reason and the seat are the whole diagnostic.
        /// </summary>
        SubmissionRefused = 9,

        /// <summary>Milestone 8.33. A seat's public agreement statement was recorded.</summary>
        AgreementAccepted = 10,

        /// <summary>
        /// Milestone 8.33. A seat's agreement statement was refused; the reason rides
        /// <see cref="PrivateSessionEventRecord.AgreementRejection"/>.
        /// </summary>
        AgreementRefused = 11,

        /// <summary>
        /// Milestone 8.33. The host served one bounded current-projection resync - to a seat that
        /// asked, or beside a stale-projection refusal. This is the recovery decision the
        /// diagnostics correlate: which seat, and against which authoritative version.
        /// </summary>
        ResyncServed = 12,

        /// <summary>
        /// Milestone 8.34. A reserved seat returned: a new connection presented that seat's own
        /// reconnect capability, the host rotated the seat's connection epoch, and the seat is
        /// held again. The record carries the seat and the new ordinal - never the capability.
        /// </summary>
        SeatReclaimed = 13,

        /// <summary>
        /// Milestone 8.35a. A seat said it is ready, or that it is not. Public lobby state, so the
        /// record carries the seat and nothing else about the connection that sent it.
        /// </summary>
        SeatReadinessChanged = 14,

        /// <summary>
        /// Milestone 8.35a. Every seat is claimed and every seat is ready, so this session left the
        /// lobby. Recorded once, because it happens once.
        /// </summary>
        MatchStarted = 15,

        /// <summary>
        /// Milestone 8.35k. A seat given up rather than held: the player left deliberately, or the
        /// host released a reservation by hand. Distinct from <see cref="SeatReserved"/> because the
        /// two mean opposite things to a returning player, and a host log that called both
        /// "reserved" is what made the playtest's refusal unreadable.
        /// </summary>
        /// <remarks>
        /// Numbered 16 rather than appended beside <see cref="SeatReadinessChanged"/>, where it
        /// reads more naturally: this member was first written as a second <c>15</c>, which C#
        /// accepts silently. Two names for one value made a released seat indistinguishable from a
        /// match starting - <c>PrivateOnlineDrivers</c> would have logged <c>MATCH_STARTED</c> for
        /// a release, and the rehearsal driver's convergence test reads that same value. A new
        /// member takes the next free number; it never takes a neighbour's.
        /// </remarks>
        SeatReleased = 16,

        /// <summary>
        /// Milestone 8.35z37. A seat picked its hero under rulebook p.22's All Pick. Public
        /// lobby state - it is broadcast in the very next roster - so the record carries the seat and
        /// nothing else about the connection that sent it.
        /// </summary>
        /// <remarks>
        /// Numbered 17 because that is the next free number, on the rule <see cref="SeatReleased"/>
        /// records the hard way: a new member never takes a neighbour's value.
        /// </remarks>
        SeatHeroChosen = 17
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
            int totalSeats,
            SeatSubmissionRejectionReason submissionRejection = SeatSubmissionRejectionReason.None,
            TeamAgreementSubmissionRejectionReason agreementRejection =
                TeamAgreementSubmissionRejectionReason.None,
            long version = 0,
            string submissionId = null)
        {
            Kind = kind;
            ConnectionOrdinal = connectionOrdinal;
            Seat = seat;
            Hero = hero;
            TeamSide = teamSide;
            Reason = reason;
            ClaimedSeats = claimedSeats;
            TotalSeats = totalSeats;
            SubmissionRejection = submissionRejection;
            AgreementRejection = agreementRejection;
            Version = version;
            SubmissionId = submissionId ?? string.Empty;
        }

        public PrivateSessionEventKind Kind { get; }
        public int ConnectionOrdinal { get; }
        public SeatId Seat { get; }
        public HeroId Hero { get; }
        public TeamSide TeamSide { get; }
        public SessionRejectionReason Reason { get; }
        public int ClaimedSeats { get; }
        public int TotalSeats { get; }

        /// <summary>Milestone 8.32. Why a wire submission was refused; <c>None</c> otherwise.</summary>
        public SeatSubmissionRejectionReason SubmissionRejection { get; }

        /// <summary>Milestone 8.33. Why an agreement statement was refused; <c>None</c> otherwise.</summary>
        public TeamAgreementSubmissionRejectionReason AgreementRejection { get; }

        /// <summary>
        /// Milestone 8.33. The authoritative projection version when the event was recorded, so a
        /// diagnostic line correlates the wire's submission identity with the version it acted
        /// against - the milestone's third check, without a byte of private state.
        /// </summary>
        public long Version { get; }

        /// <summary>Milestone 8.33. The wire's own submission identity, empty where none applies.</summary>
        public string SubmissionId { get; }

        public override string ToString() =>
            Kind + " ordinal=" + ConnectionOrdinal.ToString(CultureInfo.InvariantCulture) +
            (Seat.IsDefined ? " seat=" + Seat.Value : string.Empty) +
            " version=" + Version.ToString(CultureInfo.InvariantCulture) +
            (SubmissionId.Length > 0 ? " submission-id=" + SubmissionId : string.Empty) +
            (Reason != SessionRejectionReason.None ? " reason=" + Reason : string.Empty) +
            (SubmissionRejection != SeatSubmissionRejectionReason.None
                ? " submission=" + SubmissionRejection
                : string.Empty) +
            (AgreementRejection != TeamAgreementSubmissionRejectionReason.None
                ? " agreement=" + AgreementRejection
                : string.Empty);
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

            /// <summary>
            /// Milestone 8.32. Armed when this connection's key proof verified on a keyed session;
            /// null on an unkeyed - loopback-bound - session, which stays plaintext.
            /// </summary>
            public PrivateSessionSecureChannel Channel { get; set; }

            /// <summary>Milestone 8.32. The last gameplay-envelope sequence accepted from this connection.</summary>
            public long GameplayReceiveSequence { get; set; }

            /// <summary>Milestone 8.32. The last gameplay-envelope sequence sent to this connection.</summary>
            public long GameplaySendSequence { get; set; }

            /// <summary>
            /// Milestone 8.34. The epoch this connection speaks under - its seat's epoch at the
            /// moment the seat was granted to it. Every gameplay envelope in either direction
            /// carries it, so a frame stamped with the epoch of the connection this one replaced
            /// is refused before it can mean anything.
            /// </summary>
            public int Epoch { get; set; }
        }

        private sealed class SeatHold
        {
            public SeatOccupancyState State { get; set; }
            public int HeldByOrdinal { get; set; }

            /// <summary>
            /// Milestone 8.34. The seat's current connection epoch: <c>0</c> before the seat has
            /// ever been held, then <see cref="PrivateSessionProtocol.InitialConnectionEpoch"/>,
            /// and one higher on every authorized return. It belongs to the seat rather than to a
            /// connection, because it is what tells this seat's connections apart.
            /// </summary>
            public int Epoch { get; set; }

            /// <summary>
            /// Milestone 8.34. The owner-private capability issued with this seat's grant, and the
            /// only thing that will bring the seat back while its reservation stands. It is host
            /// state: it is never broadcast, never projected, and never recorded in an event.
            /// </summary>
            public string Capability { get; set; } = string.Empty;

            /// <summary>
            /// Milestone 8.35a. Whether this seat has said it is ready. Public: it is broadcast in
            /// every roster, which is what makes six people able to see each other get ready.
            /// </summary>
            public bool IsReady { get; set; }
        }

        private const int RetainedEventCount = 1024;

        /// <summary>
        /// The match this session serves.
        /// </summary>
        /// <remarks>
        /// Milestone 8.35z37 made this rebindable, and it is the only mutable reference in
        /// the class. It is replaced at exactly one moment - the transition out of the lobby, by
        /// <see cref="EvaluateMatchStart"/>, from the composition the seam handed back - and the
        /// replacement is required to seat the identical seats, which is checked rather than
        /// assumed. Everything a connection holds (its seat, its epoch, its reconnect capability) is
        /// keyed by <see cref="SeatId"/>, so a match whose seats are unchanged is a match every live
        /// connection is still correctly attached to.
        /// </remarks>
        private IMatchHost matchHost;

        /// <summary>
        /// Milestone 8.35z61. The on-disk gameplay narration, or null when none is
        /// attached. See <see cref="AttachGameplayNarration"/>.
        /// </summary>
        private GameplayNarrationRecorder narration;
        private readonly PrivateSessionIdentity identity;
        private readonly string sessionKey;
        private readonly IPrivateSessionOutbox outbox;
        private readonly IPrivateSessionNonceSource nonceSource;
        private readonly Dictionary<int, ConnectionRecord> connections =
            new Dictionary<int, ConnectionRecord>();
        private readonly Dictionary<SeatId, SeatHold> holds = new Dictionary<SeatId, SeatHold>();
        private readonly List<PrivateSessionEventRecord> events =
            new List<PrivateSessionEventRecord>();
        private int discardedRetiredConnectionFrames;
        private int trimmedProjectionLogRows;
        private bool sessionEnded;

        /// <summary>
        /// Milestone 8.35a. Whether this session has left the lobby. It is set once and never
        /// cleared: a later disconnect reserves a seat, which is what the reconnect policy is for,
        /// and does not put six people back in a lobby they have already left.
        /// </summary>
        private bool matchStarted;

        /// <summary>
        /// Milestone 8.35z37. Rulebook p.22's All Pick over this session's seats. Built once
        /// at construction and never replaced, so a rebuilt roster or a reconnect meets the same
        /// stage - the type derives every answer from the choices rather than caching one.
        /// </summary>
        private readonly HeroSelectionStage selection;

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

            // Milestone 8.35z37. Rulebook p.22's All Pick, ordered by the coin the match
            // ALREADY holds rather than by a second draw: the composed match's own Tie Breaker is
            // what p.22's "the team showing on the Tie Breaker coin" names, and reading it here means
            // the coin that orders the picking is the coin the match plays with.
            var selectionSeats = new List<HeroSelectionSeat>(matchHost.Seats.Seats.Count);
            foreach (var seat in matchHost.Seats.Seats)
            {
                selectionSeats.Add(new HeroSelectionSeat(seat.Id, seat.TeamSide));
            }

            selection = new HeroSelectionStage(selectionSeats, CoinSideOf(matchHost));
        }

        /// <summary>
        /// p.22's coin, read off the composed match. A match whose coin is somehow unrevealed still
        /// has to produce a pick order, and Red is stated once here rather than left to a default
        /// that would silently mean "no team" - every configuration this build composes reveals a
        /// side, so this is a floor rather than a path.
        /// </summary>
        private static TeamSide CoinSideOf(IMatchHost host)
        {
            var revealed = host.Projection.TieBreakerState.RevealedSide;
            return revealed == TeamSide.Blue ? TeamSide.Blue : TeamSide.Red;
        }

        public PrivateSessionIdentity Identity => identity;

        /// <summary>
        /// Milestone 8.35z37. Rulebook p.22's hero selection for this session's seats, from
        /// the moment the host exists.
        /// </summary>
        /// <remarks>
        /// It stands from construction rather than being created when the first pick arrives,
        /// because the pick ORDER is a fact about the seats and the coin, and both exist before
        /// anybody has picked anything. A surface asks it whose turn it is and what is still
        /// available; nothing but this host may move it.
        /// </remarks>
        public HeroSelectionStage Selection => selection;

        /// <summary>
        /// Milestone 8.35z37. What composes the match when this session leaves the lobby, or
        /// <c>null</c> to keep serving the match this host was built around.
        /// </summary>
        /// <remarks>
        /// <para>Null is the default and it is what every path predating this seam gets: a session
        /// with no composer transitions exactly as milestone 8.35a wrote it, serving the match it was
        /// constructed with. That is the whole backward-compatibility guarantee, expressed as an
        /// absent optional rather than as a flag.</para>
        /// <para><b>Milestone 8.35z43 bounded that guarantee to what it was always for.</b>
        /// A session predating this seam never picks a hero, so leaving this null is only silent
        /// while the selection stays untouched. A session that COMPLETED a selection and has no
        /// composer is refused loudly at the transition rather than quietly fielding the printed
        /// line-up over the top of six accepted picks.</para>
        /// <para>Settable rather than a constructor argument because the thing that composes a scene
        /// and the thing that starts a listener are the same object, and it configures itself in a
        /// Unity <c>Start</c> that runs after this host is built. It is read exactly once, at the
        /// transition.</para>
        /// </remarks>
        public IDeferredMatchComposition Composition { get; set; }

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

        /// <summary>
        /// Milestone 8.34. Whether a seat this session has held is away, which is the condition the
        /// disconnect policy pauses new authoritative progress under.
        /// </summary>
        /// <remarks>
        /// The pause is what makes the rest of the policy true. A pending family, its ordered
        /// answers and the board the away player was looking at are preserved because nothing is
        /// allowed to move them, not because anything copies them aside; and the reprojection a
        /// returning player receives is therefore the position they left rather than a position
        /// they have to be told about.
        /// </remarks>
        public bool IsPaused
        {
            get
            {
                foreach (var hold in holds.Values)
                {
                    if (hold.State == SeatOccupancyState.Reserved)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Milestone 8.34. How many frames arrived for a connection this host no longer holds and
        /// were discarded unread.
        /// </summary>
        /// <remarks>
        /// This is the "discard old connection input" half of the reconnect policy, counted so a
        /// fixture can prove it happened. Discarding is convergence rather than damage: a lost
        /// connection's frames say nothing about the session its replacement is in, and reading
        /// one would be the only way a replaced connection could still move the match.
        /// </remarks>
        public int DiscardedRetiredConnectionFrames => discardedRetiredConnectionFrames;

        /// <summary>
        /// How many match-log rows the frame-bound compose has dropped to keep projection frames
        /// inside their bound, in total across every send. Zero in any healthy match - the retained
        /// windows are sized to keep the whole frame well under the bound - so a nonzero value is
        /// the observable trace that graceful degradation actually fired, counted for the same
        /// reason retired-connection discards are: silent coping is how the last frame-bound defect
        /// stayed invisible until it was terminal.
        /// </summary>
        public int TrimmedProjectionLogRows => trimmedProjectionLogRows;

        /// <summary>
        /// Milestone 8.34. The seat's current connection epoch, or <c>0</c> if that seat has never
        /// been held. Diagnostics only: the epoch is protocol state, and no rule reads it.
        /// </summary>
        public int EpochForSeat(SeatId seat) =>
            holds.TryGetValue(seat, out var hold) ? hold.Epoch : 0;

        /// <summary>
        /// Milestone 8.35a. Whether every seat is claimed and ready, so this session left the
        /// lobby. One-way: nothing that happens afterwards puts it back.
        /// </summary>
        public bool HasMatchStarted => matchStarted;

        /// <summary>
        /// Milestone 8.35a. How many claimed seats have said they are ready, for a host-side lobby
        /// surface and for bounded diagnostics. Public seat state throughout.
        /// </summary>
        public int ReadySeatCount
        {
            get
            {
                var ready = 0;
                foreach (var hold in holds.Values)
                {
                    if (hold.State == SeatOccupancyState.Claimed && hold.IsReady)
                    {
                        ready++;
                    }
                }

                return ready;
            }
        }

        /// <summary>The current public seat map, in the authoritative seat order.</summary>
        /// <remarks>
        /// Milestone 8.35z37: a seat that has PICKED a hero shows the hero it picked, and
        /// that is the entire downstream half of hero selection. The row has carried a hero id since
        /// milestone 8.31 and every client already reads it, so a pick reaches all six screens on
        /// the broadcast the host already makes, with no message kind and no byte added. Before the
        /// transition the row is a statement of intent; after it, the composed match's own seats and
        /// the choices agree by construction, because the match was composed FROM them.
        /// </remarks>
        public IReadOnlyList<SeatRosterEntry> Roster
        {
            get
            {
                var entries = new List<SeatRosterEntry>(TotalSeats);
                foreach (var seat in matchHost.Seats.Seats)
                {
                    var hold = holds[seat.Id];
                    var hasChosen = selection.TryGetChoice(seat.Id, out var chosen);
                    entries.Add(new SeatRosterEntry(
                        seat.Id,
                        hasChosen ? chosen.HeroId : seat.HeroId,
                        seat.TeamSide,
                        hold.State,
                        hold.IsReady,
                        hasChosen));
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
                // Milestone 8.34. A frame for a connection the host has let go - the one a
                // returning seat replaced, or one already refused - is discarded unread. It is
                // counted rather than silently swallowed so the policy's "discard old connection
                // input" is observable, and nothing about it can reach a seat or the authority.
                discardedRetiredConnectionFrames++;
                return;
            }

            // Milestone 8.32. Once this connection's key proof verified, only sealed frames and a
            // plaintext Goodbye are meaningful: a plaintext frame of any other kind on a sealed
            // connection is an injection or a broken peer, refused whole. Goodbye and Reject stay
            // plaintext in both directions - they carry no secret, a pre-admission refusal has no
            // keys yet, and forging a close signal is the denial of service an on-path party could
            // already commit with a reset.
            if (frame.Kind == SessionMessageKind.Sealed)
            {
                if (record.Channel == null || !record.Channel.TryUnseal(frame.Payload, out frame))
                {
                    RejectAndClose(record, SessionRejectionReason.SealViolation,
                        "The sealed frame did not verify.");
                    return;
                }
            }
            else if (record.Channel != null && frame.Kind != SessionMessageKind.Goodbye)
            {
                RejectAndClose(record, SessionRejectionReason.SealViolation,
                    "A sealed connection accepts sealed frames only.");
                return;
            }

            if (frame.Kind == SessionMessageKind.Goodbye)
            {
                // Milestone 8.35k. A Goodbye is the departure the player CHOSE, and it is the only
                // one the wire already distinguishes - which is why leaving can forfeit the seat
                // without a new message kind or a protocol move.
                Depart(record, forfeited: true);
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
                    if (frame.Kind == SessionMessageKind.ReclaimSeat)
                    {
                        HandleReclaim(record, frame.Payload);
                        return;
                    }

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

                    if (frame.Kind == SessionMessageKind.SubmitCommand)
                    {
                        HandleSubmission(record, frame.Payload);
                        return;
                    }

                    if (frame.Kind == SessionMessageKind.SubmitAgreement)
                    {
                        HandleAgreementSubmission(record, frame.Payload);
                        return;
                    }

                    if (frame.Kind == SessionMessageKind.SubmitUndo)
                    {
                        HandleUndoSubmission(record, frame.Payload);
                        return;
                    }

                    if (frame.Kind == SessionMessageKind.RequestResync)
                    {
                        HandleResyncRequest(record, frame.Payload);
                        return;
                    }

                    if (frame.Kind == SessionMessageKind.SetReadiness)
                    {
                        HandleReadiness(record, frame.Payload);
                        return;
                    }

                    if (frame.Kind == SessionMessageKind.ChooseHero)
                    {
                        HandleHeroChoice(record, frame.Payload);
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

            // Milestone 8.35k. A dropped connection is not a choice: the seat stays reserved and
            // its capability stays minted, which is the road a crashed player comes back along.
            Depart(record, forfeited: false);
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

                // Milestone 8.32. The proof verified, so both ends hold the key and this
                // connection's nonce: everything after this line travels sealed, both ways.
                record.Channel = PrivateSessionSecureChannel.ForHost(sessionKey, record.Nonce);
            }

            // Full is answered at admission, before the roster is disclosed: a claim of a specific
            // taken seat gets that seat's own reason below, so this is the only place the plain
            // "there is no seat left for you" answer can live without shadowing those.
            //
            // Milestone 8.34. A reserved seat is one a returning player may still take, so it
            // keeps admission open where an ordinary claim would not: a session whose every seat
            // is held by a live connection is full, and one holding a reservation is not. The
            // arriving connection learns only the roster, which is public at a real table, and
            // its reclaim is still refused unless it presents that seat's own capability.
            if (!AnySeatClaimable())
            {
                RejectAndClose(record, SessionRejectionReason.SessionFull,
                    "Every seat is held by a live connection.");
                return;
            }

            record.Phase = ConnectionPhase.Admitted;
            // Milestone 8.35a. The admission answer carries the session's public phase beside its
            // public roster, so a returning player learns from its own handshake that it is coming
            // back to a match rather than to a lobby - the broadcast that said so happened while it
            // was away.
            Deliver(record, PrivateSessionFrameCodec.WelcomeFrame(
                new SessionWelcomeMessage(
                    identity.Correlation, Roster, matchStarted, selection.FirstPickingTeam)));
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

            GrantSeat(record, seat, targetHold, PrivateSessionEventKind.SeatClaimed);
        }

        /// <summary>
        /// Milestone 8.34. A returning client's reclaim of a seat this session reserved for it.
        /// </summary>
        /// <remarks>
        /// <para>The whole of the decision record's reconnect row happens here, in its order:
        /// reauthenticate the same service identity - which in a session with no account system is
        /// the capability this host itself issued to that seat's holder - rotate the connection
        /// epoch, and then send the full compatible owner projection through the ordinary grant
        /// path. Discarding the old connection's input needs no step of its own: the connection
        /// this one replaces is already gone, and anything still arriving for it is discarded
        /// unread by <see cref="OnFrame"/>.</para>
        /// <para>Seat control never transfers. A live seat is refused by name and left undisturbed;
        /// a reservation with the wrong capability, or none, is refused before any private
        /// projection exists to leak - and both wrong-capability cases answer identically, so the
        /// refusal itself says nothing about the capability that would have worked.</para>
        /// </remarks>
        private void HandleReclaim(ConnectionRecord record, byte[] payload)
        {
            if (!PrivateSessionFrameCodec.TryDecodeReclaimSeat(payload, out var reclaim))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The reclaim payload did not decode.");
                return;
            }

            var seat = matchHost.Seats.Find(reclaim.Seat);
            if (seat == null)
            {
                RejectAndClose(record, SessionRejectionReason.UnknownSeat,
                    "No seat of that id is bound in this match.");
                return;
            }

            var hold = holds[seat.Id];
            if (hold.State == SeatOccupancyState.Claimed)
            {
                RejectAndClose(record, SessionRejectionReason.SeatAlreadyClaimed,
                    "That seat is held by a live connection.");
                return;
            }

            if (hold.State != SeatOccupancyState.Reserved ||
                hold.Capability.Length == 0 ||
                !PrivateSessionKeyProof.Matches(hold.Capability, reclaim.Capability))
            {
                RejectAndClose(record, SessionRejectionReason.ReconnectNotAuthorized,
                    "That seat's reconnect was not authorized.");
                return;
            }

            GrantSeat(record, seat, hold, PrivateSessionEventKind.SeatReclaimed);
        }

        /// <summary>
        /// Binds one connection to one seat and tells it so: the seat's epoch is set for a first
        /// claim or rotated for a return, the grant carries that epoch and the seat's own
        /// reconnect capability to exactly one recipient, and the connection receives the full
        /// owner-scoped projection it may act on.
        /// </summary>
        private void GrantSeat(
            ConnectionRecord record, MatchSeat seat, SeatHold hold, PrivateSessionEventKind kind)
        {
            if (hold.Epoch == 0)
            {
                hold.Epoch = PrivateSessionProtocol.InitialConnectionEpoch;
                hold.Capability = MintCapability(seat.Id);
            }
            else
            {
                // The rotation the decision record asks for, and the reason the envelope has
                // carried an epoch since 8.32: from here on the replaced connection's epoch is
                // one this host will never accept again.
                hold.Epoch++;
            }

            hold.State = SeatOccupancyState.Claimed;
            hold.HeldByOrdinal = record.Ordinal;
            record.Phase = ConnectionPhase.Seated;
            record.Seat = seat.Id;
            record.Epoch = hold.Epoch;

            Deliver(record, PrivateSessionFrameCodec.SeatGrantedFrame(
                new SeatGrantedMessage(
                    new SeatRosterEntry(seat.Id, seat.HeroId, seat.TeamSide, SeatOccupancyState.Claimed),
                    ClaimedSeatCount, TotalSeats, hold.Epoch, hold.Capability)));
            BroadcastRoster();
            // Milestone 8.32. A seated connection immediately receives its own owner-scoped view,
            // encoded per seat - never one shared payload - so what it holds is what 8.30a proved
            // it may hold. Milestone 8.34: a returning seat receives exactly the same thing, a
            // freshly encoded complete projection, never a client snapshot or a delta chain.
            SendProjectionTo(record);
            Record(kind, record.Ordinal, seat.Id, seat.HeroId,
                seat.TeamSide, SessionRejectionReason.None);
            if (IsComplete)
            {
                Record(PrivateSessionEventKind.SessionComplete, record.Ordinal,
                    default, default, TeamSide.None, SessionRejectionReason.None);
            }

            // Milestone 8.35a. A grant is one of the two things that can complete the lobby - the
            // sixth seat arriving where the other five are already ready - so the transition is
            // evaluated here as well as on a readiness change.
            EvaluateMatchStart();
        }

        /// <summary>
        /// Milestone 8.35a. One seated connection's readiness for its own seat.
        /// </summary>
        /// <remarks>
        /// <para>The message names no seat and the host looks none up: readiness is recorded
        /// against the seat this connection was granted, so a seat cannot be readied by anyone but
        /// the connection holding it. That is milestone 8.30a's caller-identity rule applied to a
        /// lobby message - the connection-to-seat binding decides, never the payload.</para>
        /// <para>Setting the value the seat already holds is accepted and broadcasts nothing: a
        /// surface that resends what it believes cannot make the roster flicker, and a repeat
        /// cannot be told from the original by anything a client observes.</para>
        /// <para>After the transition readiness has nothing left to decide, so it is refused by
        /// name and without disconnecting - the connection is in a running match, and closing it
        /// over a message that arrived one moment late would cost a player their seat.</para>
        /// </remarks>
        private void HandleReadiness(ConnectionRecord record, byte[] payload)
        {
            if (!PrivateSessionFrameCodec.TryDecodeSetReadiness(payload, out var readiness))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The readiness payload did not decode.");
                return;
            }

            if (matchStarted)
            {
                outbox.Send(record.Ordinal, PrivateSessionFrameCodec.RejectFrame(
                    new SessionRejectMessage(
                        SessionRejectionReason.UnexpectedMessage,
                        "Readiness is a lobby state; this match has begun.")));
                Record(PrivateSessionEventKind.Rejected, record.Ordinal, record.Seat,
                    default, TeamSide.None, SessionRejectionReason.UnexpectedMessage);
                return;
            }

            var hold = holds[record.Seat];
            if (hold.IsReady == readiness.IsReady)
            {
                return;
            }

            hold.IsReady = readiness.IsReady;
            var seat = matchHost.Seats.Find(record.Seat);
            BroadcastRoster();
            Record(PrivateSessionEventKind.SeatReadinessChanged, record.Ordinal, record.Seat,
                seat == null ? default : seat.HeroId,
                seat == null ? TeamSide.None : seat.TeamSide,
                SessionRejectionReason.None);
            EvaluateMatchStart();
        }

        /// <summary>
        /// Milestone 8.35z37. One seated connection picks its hero, under rulebook p.22's
        /// All Pick order.
        /// </summary>
        /// <remarks>
        /// <para>The seat is the connection's own and arrives from nowhere else, so the whole
        /// authorization question is answered before the payload is read. Everything after that is
        /// the selection stage's own refusals - unknown seat, unregistered hero, out of turn, hero
        /// already taken, selection confirmed - reported by name and WITHOUT disconnecting, exactly
        /// as a refused readiness is: a pick that arrived a moment late is a mistake, not an attack,
        /// and closing the connection would cost a player their seat over it.</para>
        /// <para>After the transition there is nothing left to pick, and saying so by name is
        /// better than letting a stale surface silently change a match already on the board.</para>
        /// </remarks>
        private void HandleHeroChoice(ConnectionRecord record, byte[] payload)
        {
            if (!PrivateSessionFrameCodec.TryDecodeChooseHero(payload, out var choice))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The hero choice payload did not decode.");
                return;
            }

            if (matchStarted)
            {
                outbox.Send(record.Ordinal, PrivateSessionFrameCodec.RejectFrame(
                    new SessionRejectMessage(
                        SessionRejectionReason.UnexpectedMessage,
                        "Hero selection is a lobby state; this match has begun.")));
                Record(PrivateSessionEventKind.Rejected, record.Ordinal, record.Seat,
                    default, TeamSide.None, SessionRejectionReason.UnexpectedMessage);
                return;
            }

            // 8.35z54. The picks run AFTER readiness. Refused by name and WITHOUT closing,
            // exactly as an out-of-turn pick is: a pick that arrived before the table filled is a
            // mistake, not an attack. The condition is the roster's own - every seat taken and every
            // seat ready - so this refusal and the surface that greys the control are reading the
            // same two facts rather than two implementations of one rule.
            if (!IsPickPhaseOpen)
            {
                outbox.Send(record.Ordinal, PrivateSessionFrameCodec.RejectFrame(
                    new SessionRejectMessage(
                        SessionRejectionReason.HeroSelectionRefused,
                        "Every seat must be taken and ready before the heroes are picked.")));
                Record(PrivateSessionEventKind.Rejected, record.Ordinal, record.Seat,
                    default, TeamSide.None, SessionRejectionReason.HeroSelectionRefused);
                return;
            }

            if (!selection.TryChoose(record.Seat, choice.HeroIdValue, out var refusal))
            {
                outbox.Send(record.Ordinal, PrivateSessionFrameCodec.RejectFrame(
                    new SessionRejectMessage(
                        SessionRejectionReason.HeroSelectionRefused,
                        HeroSelectionLabels.Refusal(refusal))));
                Record(PrivateSessionEventKind.Rejected, record.Ordinal, record.Seat,
                    default, TeamSide.None, SessionRejectionReason.HeroSelectionRefused);
                return;
            }

            var picked = matchHost.Seats.Find(record.Seat);
            BroadcastRoster();
            Record(PrivateSessionEventKind.SeatHeroChosen, record.Ordinal, record.Seat,
                picked == null ? default : picked.HeroId,
                picked == null ? TeamSide.None : picked.TeamSide,
                SessionRejectionReason.None);
            // 8.35z54. The LAST pick is what starts the match now, not the last readiness.
            EvaluateMatchStart();
        }

        /// <summary>
        /// 8.35z54. Whether rulebook p.22's picking has opened: every seat claimed by a live
        /// connection and every one of them ready.
        /// </summary>
        /// <remarks>
        /// The authority's half of the ordering the user's spec states. It asks the SAME two
        /// questions of the SAME roster that <c>PrivateLobbyModel.IsPickPhaseOpen</c> asks, which is
        /// how the host's refusal and every surface's greying stay one rule; the model cannot call
        /// this because a client has no host, and this cannot call the model because the model reads
        /// a published roster while the host holds the seats themselves.
        /// </remarks>
        public bool IsPickPhaseOpen =>
            !matchStarted && IsComplete && ReadySeatCount == TotalSeats;

        /// <summary>Whether any seat has chosen a hero, which is what distinguishes a session
        /// USING rulebook p.22's picking from one that never touched it.</summary>
        private bool AnySeatHasPicked()
        {
            foreach (var seat in matchHost.Seats.Seats)
            {
                if (selection.TryGetChoice(seat.Id, out _))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 8.35z54. Whether the host has a lobby step it could take back.
        /// </summary>
        /// <remarks>
        /// True while the picking is open and at least one pick has been made. Once the match has
        /// begun there is nothing here to undo - the transition is one-way by milestone 8.35a's
        /// rule - and before the picking opens the only "step" would be somebody's readiness, which
        /// is that player's own switch to flip rather than the host's.
        /// </remarks>
        public bool CanUndoLobbyStep => IsPickPhaseOpen && selection.LastPickedSeat.IsDefined;

        /// <summary>
        /// 8.35z54. The host's stuck-state escape hatch: steps the lobby back by one pick.
        /// </summary>
        /// <remarks>
        /// <para>The user asked for "an undo button there, which is director undo, if we get stuck
        /// we can get out". It borrows the SHAPE of the seat-bounded undo milestone 8.35f built -
        /// step back exactly one accepted step, refuse loudly at the bound, and republish so nobody
        /// is left reading a state that no longer stands - and it cannot borrow that undo's LEDGER,
        /// because a hero pick is not a <c>DirectorCommand</c> and never entered the match's
        /// accepted-command history. There is nothing there to rewind; what a stuck lobby needs
        /// rewound is the lobby.</para>
        /// <para><b>The one-way transition is the bound.</b> <see cref="CanUndoLobbyStep"/> is false
        /// the moment the match starts, so this refuses rather than reaching back across it - which
        /// is the whole reason the pick-phase gate and this share one condition.</para>
        /// <para>The roster is rebroadcast on success, so the seat that lost its pick is picking
        /// again on every screen at once. That is also why nothing here has to tell the surfaces
        /// what happened: they derive the order from the roster.</para>
        /// </remarks>
        /// <returns>Whether a step was taken back.</returns>
        public bool TryUndoLobbyStep()
        {
            if (!CanUndoLobbyStep || !selection.TryUndoLastPick(out var seat))
            {
                return false;
            }

            BroadcastRoster();
            Record(PrivateSessionEventKind.SeatHeroChosen, 0, seat,
                default, TeamSide.None, SessionRejectionReason.None);
            return true;
        }

        /// <summary>
        /// Milestone 8.35a. The one place the lobby ends: every seat claimed by a live connection
        /// and every one of them ready.
        /// </summary>
        /// <remarks>
        /// Host-derived and exactly once - the guard is the flag itself, so however many times this
        /// is called the announcement is made on one call and never again. It goes to seated
        /// connections only: a connection that is admitted but has not claimed anything is not part
        /// of what just started, and would have nowhere to put the statement.
        /// <para>Milestone 8.35z37: this is also the moment the match is BUILT, when a
        /// composer is attached. The order matters and is the shape of the whole seam - compose
        /// first, announce second - because the announcement is what sends every client to its board,
        /// and a client that reached the board before the board existed would draw the provisional
        /// one. Everything the composition depends on is settled by now and nothing after it can
        /// move: the seats are all claimed, all ready, and the selection is complete or was never
        /// used.</para>
        /// </remarks>
        private void EvaluateMatchStart()
        {
            if (matchStarted || !IsComplete || ReadySeatCount != TotalSeats)
            {
                return;
            }

            // 8.35z54. READINESS OPENS THE PICKING; THE LAST PICK STARTS THE MATCH. Once
            // anybody has picked, a match that began here would be fielding a line-up half the table
            // never chose - which is the shape the user hit from the other side, where picks were
            // accepted and then silently overwritten at the transition.
            //
            // EVERY seat picks, including the ones a bot is sitting in, which is the spec's own
            // "Bots need to pick heroes as well". The safety valve that let a wholly unpicked
            // selection start on readiness alone was tried and REMOVED: it made the picking
            // unreachable in the very flow that asks for it, because at the instant the sixth seat
            // readies nobody has picked yet - measured, the match started before a single hero was
            // chosen. An automated seat now presses its own pick when the order reaches it, exactly
            // as it presses its own readiness, so "everybody picks" is a statement about seats
            // rather than about who is holding the mouse.
            if (!selection.IsComplete)
            {
                return;
            }

            matchStarted = true;
            FillUnpickedSeats();
            // The final line-up reaches every client BEFORE the transition does, because the
            // transition is what sends them to their boards and a client composes its own from this
            // roster. Broadcasting after would race the thing that reads it.
            BroadcastRoster();
            if (ComposeMatchForSelection())
            {
                // 8.35z54. THE RECOMPOSED MATCH IS PUBLISHED BEFORE THE TRANSITION IS
                // ANNOUNCED, and leaving this out is what made a chosen line-up unplayable.
                //
                // Every seat had already been sent a projection of the PROVISIONAL match at seat
                // grant. Replacing `matchHost` moves the host's whole authority to a different
                // match and the wire never saw it - which is exactly the condition
                // `RepublishProjections` was written for at milestone 8.33 ("a host-side state
                // change the wire never saw... synchronization is the host's obligation wherever
                // state moved, not only where a wire submission moved it"). Without it every
                // client attached its HUD to a projection of a match nobody was playing: measured,
                // the seat's own hero disagreed with the roster's on ALL SIX seats, and each
                // seat's only offered command came back refused by name - "The payload claims hero
                // hero-arien, which seat blue-player-01 does not own" - so the match could not
                // advance one version.
                //
                // BEFORE the announcement, because the announcement is what sends a client to its
                // board, and a client that arrived there first would draw the provisional one.
                // The projections ride the ordinary gameplay sequence, so a client applies them
                // through the same envelope discipline as any other push - no wire change, and a
                // recomposed match at the same version is adopted because acceptance is by
                // sequence rather than by version.
                RepublishProjections();
            }

            var frame = PrivateSessionFrameCodec.MatchStartedFrame();
            foreach (var connection in connections.Values)
            {
                if (connection.Phase == ConnectionPhase.Seated)
                {
                    Deliver(connection, frame);
                }
            }

            Record(PrivateSessionEventKind.MatchStarted, 0,
                default, default, TeamSide.None, SessionRejectionReason.None);
        }

        /// <summary>
        /// Milestone 8.35z37. Gives a hero to every seat that reached the transition
        /// without picking one - a spawned test driver, or a player who readied without choosing.
        /// </summary>
        /// <remarks>
        /// <para>It is the selection stage's own <c>TryAssign</c> path, which bypasses p.22's ORDER
        /// and nothing else: the registration check and the one-copy rule both still apply, so a
        /// filled seat can never be handed a hero a player picked. First-available, in the printed
        /// pick order, which is deterministic and therefore replayable. A seeded random filler
        /// belongs with milestone 8.28b's recorded setup seed and stays owed rather than improvised
        /// here.</para>
        ///
        /// <para><b>It does nothing at all when NOBODY picked, and that guard is load-bearing.</b>
        /// A session where no hero was ever chosen must deal setup step 7's printed line-up - what
        /// this build ships - and first-available assignment would not reproduce it: seats are
        /// ordered by player identity, which puts <c>blue-player-01</c> first, so filling every seat
        /// from an empty selection would hand the Blue team the Red team's printed heroes. Leaving
        /// the selection incomplete is what makes the composer decline, and declining is what leaves
        /// the provisional - printed - match standing.</para>
        ///
        /// <para>Filling a PARTIAL selection, by contrast, is required rather than optional: the
        /// roster clients have been reading already shows the picks that were made, and composing
        /// the printed line-up instead would put a board on screen that contradicts it.</para>
        /// </remarks>
        private void FillUnpickedSeats()
        {
            if (selection.IsComplete || !AnySeatHasPicked())
            {
                return;
            }

            foreach (var seat in selection.PickOrder)
            {
                if (!selection.TryGetChoice(seat.Seat, out _))
                {
                    selection.TryAssign(seat.Seat, null, out _);
                }
            }
        }

        /// <summary>
        /// Milestone 8.35z37. Hands the session's decisions to the composer and rebinds this
        /// host to what it built.
        /// </summary>
        /// <remarks>
        /// <para><b>Two ways this does nothing at all, and each is a real path.</b> An incomplete
        /// selection: a session where nobody picked - every fixture and every rehearsal that predates
        /// the seam included - whose line-up is the printed one the provisional match already deals;
        /// recomposing it would produce the identical match, which is asserted rather than assumed,
        /// so not composing is the same answer for less work. A composer that returns null: a scene
        /// that has already composed exactly what was asked for and has nothing to rebuild.</para>
        ///
        /// <para><b>Milestone 8.35z43: "no composer attached" is no longer one of them.</b>
        /// It used to share the incomplete-selection return, which made a session that had accepted
        /// and broadcast six picks and could not honour them indistinguishable from one correctly
        /// leaving the printed line-up alone. The backward-compatibility guarantee is unchanged,
        /// because the condition that actually means "this session predates the seam" is the
        /// SELECTION being untouched, not the composer being absent - and those two only ever
        /// coincided. A completed selection with nowhere to send it is a wiring defect and is thrown
        /// on, naming the line-up that would have been discarded.</para>
        ///
        /// <para><b>The seat check is not defensive coding.</b> Every live connection's seat, epoch
        /// and reconnect capability is keyed by <see cref="SeatId"/>, so a recomposed match that
        /// seated different seats would silently reattach six people to the wrong heroes. The
        /// invariant holds by construction - seat ids are the format's positional player identities
        /// and a line-up cannot change the format - which is exactly why a violation would mean the
        /// composer built something other than what it was asked for, and is thrown on rather than
        /// coped with.</para>
        /// </remarks>
        private bool ComposeMatchForSelection()
        {
            if (!selection.IsComplete)
            {
                // Nobody picked anything. `FillUnpickedSeats` deliberately leaves an untouched
                // selection incomplete, and this is the guard that reading is for: the printed
                // line-up the provisional match already deals is the right answer, and composing it
                // again would produce the identical match for more work.
                return false;
            }

            var composer = Composition;
            if (composer == null)
            {
                // Milestone 8.35z43. The two conditions used to share one `return`, which
                // made this case - a selection that COMPLETED with no composer attached - silently
                // indistinguishable from the legitimate one above, and the players' picks were
                // dropped on the floor while every one of them had been accepted and broadcast.
                //
                // It is a wiring defect rather than a session state: production attaches the composer
                // in `PrivateLobbyViews` at the one moment it can, unconditionally, so reaching here
                // means something composed a host that can take picks and cannot honour them. That is
                // thrown on for the same reason the seat check below is - a silently wrong board is
                // worse than a loud stop - and it names the line-up that would have been discarded so
                // the report is about the players' choices rather than about a null.
                //
                // The cost of getting this wrong is measured rather than imagined: milestone
                // 8.35z37's own Play Mode journey drove a host with no composer for exactly this
                // reason, accepted six picks, fielded the printed line-up, and still passed - because
                // six heroes into six seats is always the same SET. This is what that failure sounds
                // like now.
                var named = new System.Text.StringBuilder();
                foreach (var seat in selection.PickOrder)
                {
                    if (!selection.TryGetChoice(seat.Seat, out var hero))
                    {
                        continue;
                    }

                    if (named.Length > 0)
                    {
                        named.Append(", ");
                    }

                    named.Append(seat.Seat.Value).Append('=').Append(hero.HeroIdValue);
                }

                throw new InvalidOperationException(
                    "A completed hero selection has no composition to hand it to, so the line-up " +
                    "every seat settled on would be discarded and the provisional match fielded in " +
                    "its place. The discarded line-up is " + named + ". A host that admits hero " +
                    "picks must be given an " + nameof(IDeferredMatchComposition) +
                    " before the transition.");
            }

            var composed = composer.ComposeForLineup(selection.BuildLineup());
            if (composed == null)
            {
                return false;
            }

            var before = matchHost.Seats.Seats;
            var after = composed.Seats.Seats;
            if (after.Count != before.Count)
            {
                throw new InvalidOperationException(
                    "A recomposed match must seat the same seats: this session holds " +
                    before.Count + " and the composition returned " + after.Count + ".");
            }

            for (var index = 0; index < before.Count; index++)
            {
                if (!before[index].Id.Equals(after[index].Id) ||
                    before[index].TeamSide != after[index].TeamSide)
                {
                    throw new InvalidOperationException(
                        "A recomposed match must seat the same seats in the same order: seat " +
                        before[index].Id.Value + " on " + before[index].TeamSide +
                        " became " + after[index].Id.Value + " on " + after[index].TeamSide + ".");
                }
            }

            matchHost = composed;
            return true;
        }

        /// <summary>
        /// Mints one seat's reconnect capability from the injected nonce source, bound to the seat
        /// and to this session.
        /// </summary>
        /// <remarks>
        /// <para>The Application assembly draws no platform entropy, so the randomness is the
        /// adapter's: the capability is exactly as unguessable as
        /// <see cref="IPrivateSessionNonceSource"/> is, and a host composed without one issues no
        /// capability at all and therefore honours no reclaim.</para>
        /// <para>The seat and the session correlation are mixed in so that two seats never share a
        /// capability even where the nonce source is a fixture's constant, and so that a
        /// capability from one session cannot be presented to another.</para>
        /// </remarks>
        private string MintCapability(SeatId seat)
        {
            if (nonceSource == null)
            {
                return string.Empty;
            }

            var nonce = nonceSource.NextNonce() ?? Array.Empty<byte>();
            var material = new System.Text.StringBuilder(nonce.Length * 2 + 64);
            foreach (var value in nonce)
            {
                material.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            material.Append('|').Append(seat.Value).Append('|').Append(identity.Correlation);
            return PrivateSessionContentDigest.HashText(material.ToString()).Substring(0, 32);
        }

        /// <summary>
        /// Milestone 8.32. One wire submission from a seated connection: the trusted caller is the
        /// seat this connection claimed, the submission identity and expected version come from the
        /// envelope fields outside the command payload, and the command DTO is mapped to the
        /// seat's own offered instance before the authority is asked.
        /// </summary>
        /// <remarks>
        /// <para>Protocol violations - a wrong session, epoch or sequence, or a payload that does
        /// not decode - close the connection: a real client of this build cannot produce them.
        /// Seat-level refusals answer with a <see cref="SessionMessageKind.SubmissionResult"/> and
        /// leave the connection open, because a stale view or a refused rule answer is an ordinary
        /// gameplay moment, not an attack.</para>
        /// <para>The decoded command reaches <see cref="IMatchHost.SubmitForSeat"/> even when it
        /// matches no offered instance, so 8.30a's own ordered refusals - stale before duplicate
        /// before ownership before unauthorized - answer with their exact reasons; an unmatched
        /// command cannot be accepted there, because acceptance requires membership in the same
        /// offered set the mapping searched.</para>
        /// </remarks>
        private void HandleSubmission(ConnectionRecord record, byte[] payload)
        {
            if (!NetworkMessageCodec.TryDecodeSubmitCommand(payload, out var message))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The submission payload did not decode.");
                return;
            }

            if (!AcceptGameplayEnvelope(record, message.Envelope))
            {
                return;
            }

            // Milestone 8.34. New authoritative progress pauses while a seat this session has held
            // is away. The refusal is answered by name and mutates nothing - not the authority,
            // not the version, and not the submission identity, which stays unused for the retry
            // this client will make when the session resumes.
            if (RefuseWhilePaused(record, message.Envelope.Correlation, message.SubmissionId))
            {
                return;
            }

            if (!NetworkCommandCodec.TryDecode(message.CommandPayload, out var decoded))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The command payload did not decode.");
                return;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            var command = decoded;
            foreach (var offered in matchHost.ProjectionForSeat(seat.Id).AuthorizedCommands)
            {
                if (offered.Equals(decoded))
                {
                    command = offered;
                    break;
                }
            }

            var result = matchHost.SubmitForSeat(seat.Id, new SeatSubmission(
                new SubmissionId(message.SubmissionId), message.ExpectedVersion, command));

            Deliver(record, NetworkMessageCodec.SubmissionResultFrame(new NetworkSubmissionResultMessage(
                NextEnvelope(record, message.Envelope.Correlation),
                message.SubmissionId,
                result.Accepted,
                result.RejectionReason,
                result.SessionRejectionReason,
                result.Detail,
                result.Version,
                result.Sequence)));

            Record(
                result.Accepted
                    ? PrivateSessionEventKind.SubmissionAccepted
                    : PrivateSessionEventKind.SubmissionRefused,
                record.Ordinal, record.Seat, seat.HeroId, seat.TeamSide,
                SessionRejectionReason.None, result.RejectionReason,
                submissionId: message.SubmissionId);

            if (result.Accepted)
            {
                PublishProjections(record, message.Envelope.Correlation);
            }
            else if (result.RejectionReason == SeatSubmissionRejectionReason.StaleProjection)
            {
                // The failure policy's row: a stale expected version is refused with no mutation
                // and offered one bounded current-projection resync. Offering it here, unasked,
                // is what lets a client that answered against an old view converge on the next
                // frame rather than waiting for someone else's acceptance to push one.
                if (SendProjectionTo(record, message.Envelope.Correlation))
                {
                    Record(PrivateSessionEventKind.ResyncServed, record.Ordinal, record.Seat,
                        seat.HeroId, seat.TeamSide, SessionRejectionReason.None);
                }
            }
        }

        /// <summary>
        /// Milestone 8.33. One public agreement statement from a seated connection, under exactly
        /// the submission discipline: envelope violations close the connection, the decoded
        /// statement is mapped to the seat's own projected offer before the coordinator is asked,
        /// and every seat-level refusal answers by name with nothing mutated.
        /// </summary>
        /// <remarks>
        /// An accepted statement republishes every seat's projection even though most statements
        /// move no authoritative version: the statement changed what every teammate's agreement
        /// surface says, and the statement that completes unanimity has moved the match itself.
        /// The final rules command is only ever composed by the host's coordinator - a seat that
        /// submits it directly is refused by the offered surface, which the wire tests prove.
        /// </remarks>
        private void HandleAgreementSubmission(ConnectionRecord record, byte[] payload)
        {
            if (!NetworkMessageCodec.TryDecodeSubmitAgreement(payload, out var message))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The agreement payload did not decode.");
                return;
            }

            if (!AcceptGameplayEnvelope(record, message.Envelope))
            {
                return;
            }

            if (RefuseAgreementWhilePaused(record, message.Envelope.Correlation, message.SubmissionId))
            {
                return;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            var statement = message.Statement;
            var agreement = matchHost.ProjectionForSeat(seat.Id).TeamAgreement;
            if (agreement != null)
            {
                foreach (var offered in agreement.AuthorizedCommands)
                {
                    if (offered.Equals(statement))
                    {
                        statement = offered;
                        break;
                    }
                }
            }

            var result = matchHost.SubmitAgreementForSeat(seat.Id, new TeamAgreementSubmission(
                new SubmissionId(message.SubmissionId), message.ExpectedVersion, statement));

            Deliver(record, NetworkMessageCodec.AgreementResultFrame(new NetworkAgreementResultMessage(
                NextEnvelope(record, message.Envelope.Correlation),
                message.SubmissionId,
                result.Accepted,
                result.RejectionReason,
                result.Detail,
                result.Version)));

            Record(
                result.Accepted
                    ? PrivateSessionEventKind.AgreementAccepted
                    : PrivateSessionEventKind.AgreementRefused,
                record.Ordinal, record.Seat, seat.HeroId, seat.TeamSide,
                SessionRejectionReason.None,
                agreementRejection: result.RejectionReason,
                submissionId: message.SubmissionId);

            if (result.Accepted)
            {
                PublishProjections(record, message.Envelope.Correlation);
            }
            else if (result.RejectionReason == TeamAgreementSubmissionRejectionReason.StaleProjection ||
                result.RejectionReason == TeamAgreementSubmissionRejectionReason.StaleProposal)
            {
                if (SendProjectionTo(record, message.Envelope.Correlation))
                {
                    Record(PrivateSessionEventKind.ResyncServed, record.Ordinal, record.Seat,
                        seat.HeroId, seat.TeamSide, SessionRejectionReason.None);
                }
            }
        }

        /// <summary>
        /// Milestone 8.33. A seat asked for one bounded current-projection resync. The answer is
        /// its ordinary projection push echoing the request's correlation - one complete bounded
        /// projection per request, never a delta chain.
        /// </summary>
        private void HandleResyncRequest(ConnectionRecord record, byte[] payload)
        {
            if (!NetworkMessageCodec.TryDecodeRequestResync(payload, out var message))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The resync request did not decode.");
                return;
            }

            if (!AcceptGameplayEnvelope(record, message.Envelope))
            {
                return;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            if (SendProjectionTo(record, message.Envelope.Correlation))
            {
                Record(PrivateSessionEventKind.ResyncServed, record.Ordinal, record.Seat,
                    seat.HeroId, seat.TeamSide, SessionRejectionReason.None);
            }
        }

        /// <summary>
        /// Milestone 8.34. Answers a command submission with the paused refusal when a seat is
        /// away, and reports whether it did.
        /// </summary>
        private bool RefuseWhilePaused(ConnectionRecord record, long correlation, string submissionId)
        {
            if (!IsPaused)
            {
                return false;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            Deliver(record, NetworkMessageCodec.SubmissionResultFrame(new NetworkSubmissionResultMessage(
                NextEnvelope(record, correlation),
                submissionId,
                false,
                SeatSubmissionRejectionReason.SessionPaused,
                MatchSessionRejectionReason.None,
                "A seat of this match is away; the session is holding still until it returns.",
                matchHost.Version,
                0)));
            Record(PrivateSessionEventKind.SubmissionRefused, record.Ordinal, record.Seat,
                seat == null ? default : seat.HeroId,
                seat == null ? TeamSide.None : seat.TeamSide,
                SessionRejectionReason.None, SeatSubmissionRejectionReason.SessionPaused,
                submissionId: submissionId);
            return true;
        }

        /// <summary>
        /// Milestone 8.35f. One seat's bounded rewind from a seated connection, under exactly the
        /// submission discipline: envelope violations close the connection, the pause refuses by
        /// name, and every seat-level refusal answers with nothing mutated.
        /// </summary>
        /// <remarks>
        /// An accepted rewind republishes every seat's projection rather than only the asking
        /// one's: a step back is a change to the shared board, and the other five seats have to see
        /// it. There is nothing to map to an offered instance here - an undo names no target - so
        /// the identity, the expected version and the seat's own published availability are the
        /// whole of what the host checks.
        /// </remarks>
        private void HandleUndoSubmission(ConnectionRecord record, byte[] payload)
        {
            if (!NetworkMessageCodec.TryDecodeSubmitUndo(payload, out var message))
            {
                RejectAndClose(record, SessionRejectionReason.MalformedFrame,
                    "The undo payload did not decode.");
                return;
            }

            if (!AcceptGameplayEnvelope(record, message.Envelope))
            {
                return;
            }

            if (RefuseUndoWhilePaused(record, message.Envelope.Correlation, message.SubmissionId))
            {
                return;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            var result = matchHost.SubmitUndoForSeat(seat.Id, new SeatUndoSubmission(
                new SubmissionId(message.SubmissionId), message.ExpectedVersion));

            Deliver(record, NetworkMessageCodec.UndoResultFrame(new NetworkUndoResultMessage(
                NextEnvelope(record, message.Envelope.Correlation),
                message.SubmissionId,
                result.Accepted,
                result.RejectionReason,
                result.SessionRejectionReason,
                result.Detail,
                result.Version)));

            Record(
                result.Accepted
                    ? PrivateSessionEventKind.SubmissionAccepted
                    : PrivateSessionEventKind.SubmissionRefused,
                record.Ordinal, record.Seat, seat.HeroId, seat.TeamSide,
                SessionRejectionReason.None, result.RejectionReason,
                submissionId: message.SubmissionId);

            if (result.Accepted)
            {
                PublishProjections(record, message.Envelope.Correlation);
            }
            else if (result.RejectionReason == SeatSubmissionRejectionReason.StaleProjection)
            {
                if (SendProjectionTo(record, message.Envelope.Correlation))
                {
                    Record(PrivateSessionEventKind.ResyncServed, record.Ordinal, record.Seat,
                        seat.HeroId, seat.TeamSide, SessionRejectionReason.None);
                }
            }
        }

        /// <summary>Milestone 8.35f. The undo half of <see cref="RefuseWhilePaused"/>.</summary>
        private bool RefuseUndoWhilePaused(
            ConnectionRecord record, long correlation, string submissionId)
        {
            if (!IsPaused)
            {
                return false;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            Deliver(record, NetworkMessageCodec.UndoResultFrame(new NetworkUndoResultMessage(
                NextEnvelope(record, correlation),
                submissionId,
                false,
                SeatSubmissionRejectionReason.SessionPaused,
                MatchSessionRejectionReason.None,
                "A seat of this match is away; the session is holding still until it returns.",
                matchHost.Version)));
            Record(PrivateSessionEventKind.SubmissionRefused, record.Ordinal, record.Seat,
                seat == null ? default : seat.HeroId,
                seat == null ? TeamSide.None : seat.TeamSide,
                SessionRejectionReason.None, SeatSubmissionRejectionReason.SessionPaused,
                submissionId: submissionId);
            return true;
        }

        /// <summary>Milestone 8.34. The agreement half of <see cref="RefuseWhilePaused"/>.</summary>
        private bool RefuseAgreementWhilePaused(
            ConnectionRecord record, long correlation, string submissionId)
        {
            if (!IsPaused)
            {
                return false;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            Deliver(record, NetworkMessageCodec.AgreementResultFrame(new NetworkAgreementResultMessage(
                NextEnvelope(record, correlation),
                submissionId,
                false,
                TeamAgreementSubmissionRejectionReason.SessionPaused,
                "A seat of this match is away; the session is holding still until it returns.",
                matchHost.Version)));
            Record(PrivateSessionEventKind.AgreementRefused, record.Ordinal, record.Seat,
                seat == null ? default : seat.HeroId,
                seat == null ? TeamSide.None : seat.TeamSide,
                SessionRejectionReason.None,
                agreementRejection: TeamAgreementSubmissionRejectionReason.SessionPaused,
                submissionId: submissionId);
            return true;
        }

        /// <summary>
        /// The one envelope rule every gameplay message obeys: right session, this connection's
        /// own epoch, and exactly the next sequence - anything else is a protocol violation that
        /// closes the connection before any seat-level answer.
        /// </summary>
        /// <remarks>
        /// Milestone 8.34 makes the epoch this connection's own rather than the protocol's first,
        /// which is what tells a returning seat's frames from those of the connection it replaced:
        /// a frame stamped with the retired epoch names an epoch this host will never accept
        /// again, and is refused before it can mean anything.
        /// </remarks>
        private bool AcceptGameplayEnvelope(ConnectionRecord record, NetworkEnvelope envelope)
        {
            if (!string.Equals(envelope.SessionCorrelation, identity.Correlation, StringComparison.Ordinal) ||
                envelope.Epoch != record.Epoch ||
                envelope.Sequence != record.GameplayReceiveSequence + 1)
            {
                RejectAndClose(record, SessionRejectionReason.EnvelopeMismatch,
                    "The envelope names the wrong session, epoch, or sequence.");
                return false;
            }

            record.GameplayReceiveSequence = envelope.Sequence;
            return true;
        }

        /// <summary>
        /// The projection frame's compose budget: the SEALED bound, which is the tighter of the two
        /// the frame must eventually pass, so one number governs plaintext and sealed links alike.
        /// </summary>
        internal const int ProjectionFrameBudgetBytes = PrivateSessionSecureChannel.MaxSealedPayloadBytes;

        /// <summary>
        /// One seat projection composed against the production budget - the exact decision
        /// <see cref="SendProjectionTo"/> acts on, stated once so a fixture can drive it past the
        /// bound without needing match state the retained windows exist to make impossible.
        /// </summary>
        internal static bool TryComposeProjectionFrame(
            NetworkSeatProjectionMessage message, out byte[] frame, out int droppedLogRows) =>
            NetworkMessageCodec.TrySeatProjectionFrameWithin(
                message, ProjectionFrameBudgetBytes, out frame, out droppedLogRows);

        /// <returns>
        /// Whether a projection frame was actually delivered - false when this seat's payload
        /// outgrew the frame bound and the connection was refused instead, so a caller about to
        /// record a served resync does not claim a delivery that never happened.
        /// </returns>
        private bool SendProjectionTo(ConnectionRecord record, long correlation = 0)
        {
            var seat = matchHost.Seats.Find(record.Seat);
            if (seat == null)
            {
                return false;
            }

            var projection = matchHost.ProjectionForSeat(seat.Id);
            var payload = NetworkSeatProjectionCodec.Encode(projection);

            // Milestone 8.35r. The seat-public match log travels in the projection's own frame, as
            // the whole retained window. THIS IS WHERE THE RECONNECT-HISTORY SEMANTICS LIVE, and
            // they are as short as they look: a seat that has just arrived, one that has just
            // returned under its reconnect capability, and one that never left all receive the same
            // bounded window of recent lines, because there is only one thing to send. The log is
            // recent history a player scrolls, not an archive replayed from the opening move.
            var log = new NetworkMatchLogRow[projection.MatchLog.Count];
            for (var index = 0; index < log.Length; index++)
            {
                log[index] = NetworkMatchLogCodec.ToRow(projection.MatchLog[index]);
            }

            // The frame-bound corrective's send discipline. The envelope names the NEXT sequence
            // without committing it, because committing first was half of the recorded table-killer:
            // when the unwindowed round history outgrew the frame, the sequence had already advanced
            // when the encode threw, so the seat read every later frame as a gap and every resync
            // was served down the same throwing path. The sequence now moves only beside a frame
            // that exists.
            var envelope = new NetworkEnvelope(
                identity.Correlation, record.Epoch, record.GameplaySendSequence + 1, correlation);
            if (!TryComposeProjectionFrame(
                new NetworkSeatProjectionMessage(envelope, payload, Array.AsReadOnly(log)),
                out var frame, out var droppedLogRows))
            {
                // The projection payload alone no longer fits any frame this protocol can carry.
                // No trimming can mend that, and nothing this seat is ever sent again would fit
                // either - so the failure is one connection's, loud and recoverable: a named
                // refusal the player sees, the seat left Reserved with its road back, and every
                // other seat still served. What it must never be again is a throw out of the
                // publish path that stalls the whole table with nothing on screen.
                RejectAndClose(record, SessionRejectionReason.OversizedFrame,
                    "This seat's projection (" + payload.Length.ToString(CultureInfo.InvariantCulture) +
                    " bytes) outgrew the session frame bound; the connection is closed rather than" +
                    " left waiting on a frame that cannot exist.");
                return false;
            }

            trimmedProjectionLogRows += droppedLogRows;
            record.GameplaySendSequence++;
            Deliver(record, frame);
            return true;
        }

        /// <summary>
        /// Every seated connection receives its own view of the state that just committed. When a
        /// seat initiated the publication, only that seat's projection echoes its request
        /// correlation; every observer still receives an unsolicited zero-correlation push.
        /// </summary>
        private void PublishProjections(
            ConnectionRecord initiatingRecord = null,
            long initiatingCorrelation = 0)
        {
            // A snapshot, because one seat's send may now end in RejectAndClose - the frame-bound
            // refusal - which removes that record. One connection's failure is one connection's:
            // the iteration survives it and every other seat is still served.
            foreach (var record in new List<ConnectionRecord>(connections.Values))
            {
                if (record.Phase == ConnectionPhase.Seated)
                {
                    SendProjectionTo(
                        record,
                        ReferenceEquals(record, initiatingRecord) ? initiatingCorrelation : 0);
                }
            }

            // Milestone 8.35z61. Every publish is a point the authoritative state moved, so
            // it is the honest place to ask the narration what is new. Written after the seats are
            // served: the file records what the table was actually shown.
            NarrateProgress();
        }

        /// <summary>
        /// Milestone 8.33. Republishes every seated connection's view after a host-side state
        /// change the wire never saw - a snapshot restore, restart, or Undo submitted through the
        /// authority's own port rather than by a seat. Synchronization is the host's obligation
        /// wherever state moved, not only where a wire submission moved it.
        /// </summary>
        public void RepublishProjections() => PublishProjections();

        /// <summary>
        /// Milestone 8.35z61. Attaches the on-disk gameplay narration, writing its header
        /// immediately and every row the match has already produced.
        /// </summary>
        /// <remarks>
        /// <para>Optional, and null by default, so a host composed without one behaves exactly as it
        /// did. It is attached HERE rather than in the Unity drivers because both host paths - the
        /// smoke driver and the real lobby - go through this class, and a narration wired into one
        /// of them would be missing from the other. That is how the 2026-09-06 playtest came to have
        /// a host log with no game in it.</para>
        /// <para>The recorder is a reader of the authoritative projection and never a second source
        /// of events, so nothing here can make the file and the in-game log disagree.</para>
        /// </remarks>
        public void AttachGameplayNarration(
            GameplayNarrationRecorder recorder,
            string buildIdentity = null,
            string mapIdentity = null)
        {
            narration = recorder;
            if (narration == null)
            {
                return;
            }

            // Who is sitting where, as the host knows it at this moment. "Held" means a connection
            // holds the seat now - which is the only occupancy fact this layer owns; a seat filled
            // by a local test driver holds its seat exactly as a person does, so the header states
            // occupancy rather than claiming to know a human from a bot.
            var introductions =
                new List<GameplaySeatIntroduction>();
            foreach (var seat in matchHost.Seats.Seats)
            {
                if (!seat.HeroId.IsDefined)
                {
                    continue;
                }

                var held = holds.TryGetValue(seat.Id, out var hold) &&
                    hold.State == SeatOccupancyState.Claimed;
                introductions.Add(
                    new GameplaySeatIntroduction(
                        seat.Id.Value, seat.HeroId.Value, held));
            }

            narration.WriteHeader(
                buildIdentity ?? identity.BuildIdentity,
                mapIdentity ?? identity.MapId,
                introductions);
            NarrateProgress();
        }

        /// <summary>
        /// Writes whatever the match has narrated since the last time this was asked. Called after
        /// every publish, which is every point the authoritative state moved.
        /// </summary>
        private void NarrateProgress()
        {
            if (narration == null || matchHost == null)
            {
                return;
            }

            var projection = matchHost.Projection;
            if (projection != null)
            {
                narration.Observe(projection.Version, projection.MatchLog);
            }
        }

        private NetworkEnvelope NextEnvelope(ConnectionRecord record, long correlation)
        {
            record.GameplaySendSequence++;
            return new NetworkEnvelope(
                identity.Correlation,
                record.Epoch,
                record.GameplaySendSequence,
                correlation);
        }

        /// <summary>
        /// Sends one composed frame, sealed when this connection's channel is armed. Challenge,
        /// Reject, and Goodbye do not come through here - they stay plaintext by design.
        /// </summary>
        private void Deliver(ConnectionRecord record, byte[] frame)
        {
            if (record.Channel == null)
            {
                outbox.Send(record.Ordinal, frame);
                return;
            }

            var kind = (SessionMessageKind)PrivateSessionFrameCodec.ReadUInt16(frame, 6);
            var payload = new byte[frame.Length - PrivateSessionProtocol.FrameHeaderBytes];
            Array.Copy(frame, PrivateSessionProtocol.FrameHeaderBytes, payload, 0, payload.Length);
            outbox.Send(record.Ordinal, record.Channel.Seal(kind, payload));
        }

        /// <summary>
        /// Ends the session on purpose: every connection is told Goodbye and closed, and every
        /// seat is released. No seat is reserved by an orderly end - there is no session left to
        /// hold a reservation.
        /// </summary>
        /// <remarks>
        /// Milestone 8.34 makes that sentence true in code rather than only in prose, and it is
        /// also the whole of reconnect-eligibility expiry. The architecture record gives a
        /// reservation exactly one lifetime - the live host session - with no clock anywhere in
        /// it, so eligibility ends when the session does: the reservation is released, the seat's
        /// capability is dropped, and nothing arriving afterwards can reclaim anything.
        /// </remarks>
        public void Shutdown()
        {
            var open = new List<ConnectionRecord>(connections.Values);
            foreach (var record in open)
            {
                outbox.Send(record.Ordinal, PrivateSessionFrameCodec.GoodbyeFrame());
                outbox.Close(record.Ordinal);
                connections.Remove(record.Ordinal);
            }

            foreach (var hold in holds.Values)
            {
                hold.State = SeatOccupancyState.Open;
                hold.HeldByOrdinal = 0;
                hold.Capability = string.Empty;
                hold.IsReady = false;
            }

            sessionEnded = true;
        }

        /// <summary>Milestone 8.34. Whether an orderly end has already released every seat.</summary>
        public bool HasEnded => sessionEnded;

        /// <summary>
        /// Whether any seat could still be taken by an arriving connection: an open one by a
        /// claim, or a reserved one by its own player's reclaim.
        /// </summary>
        private bool AnySeatClaimable()
        {
            foreach (var hold in holds.Values)
            {
                if (hold.State != SeatOccupancyState.Claimed)
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
            // Milestone 8.35k. A refusal is not a player choosing to leave, so a seated connection
            // the host refuses keeps its reservation and its road back.
            Depart(record, forfeited: false);
        }

        /// <summary>
        /// Milestone 8.35k. <paramref name="forfeited"/> is the user's own rule: LEAVE gives the seat
        /// up and a crash does not. A forfeited seat returns to Open with its capability cleared, so
        /// it is claimable again by an ordinary keyed claim; a dropped one stays Reserved holding the
        /// capability, which is the only thing that will get its player back into it.
        /// </summary>
        private void Depart(ConnectionRecord record, bool forfeited)
        {
            connections.Remove(record.Ordinal);
            if (record.Phase != ConnectionPhase.Seated)
            {
                Record(PrivateSessionEventKind.Disconnected, record.Ordinal,
                    default, default, TeamSide.None, SessionRejectionReason.None);
                return;
            }

            var hold = holds[record.Seat];
            hold.State = forfeited ? SeatOccupancyState.Open : SeatOccupancyState.Reserved;
            hold.HeldByOrdinal = 0;
            if (forfeited)
            {
                // The leaver chose the exposure. Clearing the capability is what makes the seat
                // plainly claimable again rather than leaving a token that could still reclaim it.
                hold.Capability = string.Empty;
                hold.Epoch = 0;
            }
            // Milestone 8.35a. A reserved lobby seat is not ready: nobody is sitting in it, and
            // leaving it ready would let five people plus an empty chair start a match. After the
            // transition the flag has no consumer left, so it is not disturbed - a returning player
            // should not read "NOT READY" beside a match already under way.
            if (!matchStarted)
            {
                hold.IsReady = false;
            }

            var seat = matchHost.Seats.Find(record.Seat);
            BroadcastRoster();
            Record(
                forfeited
                    ? PrivateSessionEventKind.SeatReleased
                    : PrivateSessionEventKind.SeatReserved,
                record.Ordinal, record.Seat,
                seat == null ? default : seat.HeroId,
                seat == null ? TeamSide.None : seat.TeamSide,
                SessionRejectionReason.None);
            Record(PrivateSessionEventKind.Disconnected, record.Ordinal,
                record.Seat, default, TeamSide.None, SessionRejectionReason.None);
        }

        /// <summary>
        /// Milestone 8.35k. The host's deliberate escape hatch: hand a reserved seat back so anybody
        /// may claim it plainly. Returns whether a reservation was actually released.
        /// </summary>
        /// <remarks>
        /// <para>This is the one road back to a seat that proves nothing about who is taking it, so
        /// it is guarded by the only thing that can stand in for proof: a deliberate act by the one
        /// person who already holds the authority. It is <b>never</b> on a timer and never automatic.
        /// The reservation's "no clock anywhere in it" property is a feature - an auto-release would
        /// hand a dropped player's seat to whoever reconnected first, which is precisely the theft
        /// the capability exists to prevent.</para>
        /// <para>It refuses a seat that is Claimed. A live player is never displaced by this control
        /// any more than by a returning one, and the host wanting the seat back is not a reason to
        /// take it from somebody sitting in it.</para>
        /// <para>Clearing the capability is the point rather than a tidy-up: a released seat that
        /// still held its token could be reclaimed by the departed player's machine after somebody
        /// else had taken it.</para>
        /// </remarks>
        public bool ReleaseSeat(SeatId seat)
        {
            if (!seat.IsDefined || !holds.TryGetValue(seat, out var hold) ||
                hold.State != SeatOccupancyState.Reserved)
            {
                return false;
            }

            hold.State = SeatOccupancyState.Open;
            hold.Capability = string.Empty;
            hold.Epoch = 0;
            hold.HeldByOrdinal = 0;
            if (!matchStarted)
            {
                hold.IsReady = false;
            }

            var released = matchHost.Seats.Find(seat);
            BroadcastRoster();
            Record(PrivateSessionEventKind.SeatReleased, 0, seat,
                released == null ? default : released.HeroId,
                released == null ? TeamSide.None : released.TeamSide,
                SessionRejectionReason.None);
            return true;
        }

        private void BroadcastRoster()
        {
            var frame = PrivateSessionFrameCodec.RosterUpdateFrame(
                new SeatRosterMessage(Roster, selection.FirstPickingTeam));
            foreach (var record in connections.Values)
            {
                if (record.Phase != ConnectionPhase.AwaitingHello)
                {
                    Deliver(record, frame);
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
                case SessionRejectionReason.MatchFormatMismatch:
                    return "Match format differs; host is playing " + identity.MatchFormatId + ".";
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
            SessionRejectionReason reason,
            SeatSubmissionRejectionReason submissionRejection = SeatSubmissionRejectionReason.None,
            TeamAgreementSubmissionRejectionReason agreementRejection =
                TeamAgreementSubmissionRejectionReason.None,
            string submissionId = null)
        {
            if (events.Count >= RetainedEventCount)
            {
                events.RemoveAt(0);
            }

            events.Add(new PrivateSessionEventRecord(
                kind, ordinal, seat, hero, teamSide, reason, ClaimedSeatCount, TotalSeats,
                submissionRejection, agreementRejection, matchHost.Version, submissionId));

            // Milestone 8.35z61. The session layer's own events - refusals, resyncs,
            // reconnects - written INLINE among the gameplay rows. A refused submission produces no
            // gameplay row at all, so on the evidence of the narration alone a seat whose input was
            // rejected would look identical to a seat that was never asked. These lines are what
            // tell those two apart, and the 2026-09-06 playtest is the case that needed them.
            narration?.WriteSessionEvent(
                matchHost.Version,
                kind.ToString(),
                "seat=" + (seat.IsDefined ? seat.Value : "(none)") +
                " hero=" + (hero.IsDefined ? hero.Value : "(none)") +
                " ordinal=" + ordinal.ToString(CultureInfo.InvariantCulture) +
                (reason == SessionRejectionReason.None
                    ? string.Empty
                    : " reason=" + reason) +
                (submissionRejection == SeatSubmissionRejectionReason.None
                    ? string.Empty
                    : " submissionRefused=" + submissionRejection) +
                (agreementRejection == TeamAgreementSubmissionRejectionReason.None
                    ? string.Empty
                    : " agreementRefused=" + agreementRejection));
        }
    }
}
