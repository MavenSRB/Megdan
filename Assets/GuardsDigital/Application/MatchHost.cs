using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.30. Which authority-mutating question a submission asks. It is the complete list
    /// of doors into <see cref="IMatchSession"/>, which is what lets a conformance check prove that
    /// the host is the only way in rather than one of two ways.
    /// </summary>
    /// <remarks>
    /// <c>CaptureSnapshot</c> has no member here on purpose: it reads and never advances the
    /// authoritative version, so it is a port property rather than a submission.
    /// </remarks>
    public enum MatchSubmissionKind
    {
        None = 0,
        Command = 1,
        Undo = 2,
        Restart = 3,
        RestoreSnapshot = 4
    }

    /// <summary>
    /// Milestone 8.30. Why the host refused, as distinct from why the authority did.
    /// </summary>
    /// <remarks>
    /// A separate enum rather than new members on <see cref="MatchSessionRejectionReason"/>, for the
    /// reason 8.29's threat model gives: a host refusal is never a rules answer, and a caller that
    /// reads one enum must not be able to present "another submission is executing" to a player as
    /// though a rule said no. A session refusal appears here as <see cref="SessionRefused"/> and
    /// carries the authority's own reason beside it.
    /// </remarks>
    public enum MatchHostRejectionReason
    {
        None = 0,
        SessionRefused = 1,

        /// <summary>
        /// The host was already executing a submission. The serialized executor refuses rather than
        /// interleaving, and refuses before the authority is touched, so a re-entrant local call or
        /// a second connection's message can never observe or produce a half-applied state.
        /// </summary>
        SubmissionInProgress = 2
    }

    /// <summary>
    /// Milestone 8.30. One authority-mutating request, in the transport-neutral form both the local
    /// director adapter and a future network host submit.
    /// </summary>
    /// <remarks>
    /// It carries what is being asked and nothing about who asked it. Trusted seat context,
    /// submission identity, and expected projection version are milestone 8.30a's, and the decision
    /// record is explicit that identity never travels inside the gameplay payload - so the field
    /// that would carry it must not exist here first.
    /// </remarks>
    public sealed class MatchSubmission
    {
        private MatchSubmission(
            MatchSubmissionKind kind,
            DirectorCommand command,
            string snapshotPayload)
        {
            Kind = kind;
            Command = command;
            SnapshotPayload = snapshotPayload;
        }

        /// <summary>The typed command an offered surface produced.</summary>
        public static MatchSubmission ForCommand(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            return new MatchSubmission(MatchSubmissionKind.Command, command, null);
        }

        public static MatchSubmission ForUndo() =>
            new MatchSubmission(MatchSubmissionKind.Undo, null, null);

        public static MatchSubmission ForRestart() =>
            new MatchSubmission(MatchSubmissionKind.Restart, null, null);

        public static MatchSubmission ForSnapshotRestore(string payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            return new MatchSubmission(MatchSubmissionKind.RestoreSnapshot, null, payload);
        }

        public MatchSubmissionKind Kind { get; }

        /// <summary>The command, and <c>null</c> for every other kind.</summary>
        public DirectorCommand Command { get; }

        /// <summary>The payload to restore, and <c>null</c> for every other kind.</summary>
        public string SnapshotPayload { get; }
    }

    /// <summary>
    /// Milestone 8.30. What one accepted submission did, in the order it did it.
    /// </summary>
    /// <remarks>
    /// This is the protocol-neutral half of the boundary: the bounded, immutable description of a
    /// state advance that a future network host maps to wire messages, and that the local adapter
    /// reads directly. It deliberately holds no projection - a consumer reads the state it is
    /// entitled to from the port, and 8.30a makes that reading owner-scoped. Putting a projection in
    /// the stream would hand every future consumer the global read model instead.
    /// </remarks>
    public sealed class MatchHostResultRecord
    {
        internal MatchHostResultRecord(
            long sequence,
            long version,
            MatchSubmissionKind kind,
            DirectorCommand command,
            IReadOnlyList<RoundResolutionEvent> events)
        {
            Sequence = sequence;
            Version = version;
            Kind = kind;
            Command = command;
            Events = events;
        }

        /// <summary>Its position in the host's one ordered stream, from 1 and contiguous.</summary>
        public long Sequence { get; }

        /// <summary>The authoritative projection version this submission published.</summary>
        public long Version { get; }

        public MatchSubmissionKind Kind { get; }

        /// <summary>The accepted command, and <c>null</c> for the three operation kinds.</summary>
        public DirectorCommand Command { get; }

        /// <summary>
        /// The ordered rule events this submission produced, empty for a submission that produced
        /// none. Undo, Restart and a snapshot restore reconstruct state rather than resolving rules,
        /// so their records carry no events and tell a consumer to re-read the state whole.
        /// </summary>
        public IReadOnlyList<RoundResolutionEvent> Events { get; }
    }

    /// <summary>
    /// Milestone 8.30. One consumer's read of the ordered result stream from its own cursor.
    /// </summary>
    public sealed class MatchHostResultStreamRead
    {
        internal MatchHostResultStreamRead(
            bool isComplete,
            IReadOnlyList<MatchHostResultRecord> records,
            long nextSequence)
        {
            IsComplete = isComplete;
            Records = records;
            NextSequence = nextSequence;
        }

        /// <summary>
        /// Whether every record between the caller's cursor and <see cref="NextSequence"/> is here.
        ///
        /// <para>False when the cursor is older than the retained window, or ahead of the host. A
        /// consumer that reads false must resynchronize from state rather than apply what it got:
        /// the records are then empty, so an incomplete read can never be mistaken for an ordered
        /// continuation with a gap in it.</para>
        /// </summary>
        public bool IsComplete { get; }

        public IReadOnlyList<MatchHostResultRecord> Records { get; }

        /// <summary>The cursor the caller holds after consuming this read.</summary>
        public long NextSequence { get; }
    }

    /// <summary>
    /// Milestone 8.30. What the host told the submitter about its own submission.
    /// </summary>
    /// <remarks>
    /// The submitter-facing half of the boundary, and deliberately richer than
    /// <see cref="MatchHostResultRecord"/>: an in-process caller gets the authority's own result
    /// object, with every family-specific refusal reason it already reported. A future network host
    /// answers its client from the bounded fields above it - <see cref="Accepted"/>,
    /// <see cref="RejectionReason"/>, <see cref="SessionRejectionReason"/>, <see cref="Detail"/>,
    /// <see cref="Version"/> and <see cref="Sequence"/> - and milestone 8.32 owns their codec.
    /// </remarks>
    public sealed class MatchHostResult
    {
        internal MatchHostResult(
            MatchSubmission submission,
            bool accepted,
            MatchHostRejectionReason rejectionReason,
            MatchSessionRejectionReason sessionRejectionReason,
            string detail,
            long version,
            long sequence,
            IReadOnlyList<RoundResolutionEvent> events,
            MatchSessionProjection projection,
            MatchCommandResult commandResult,
            MatchSessionOperationResult operationResult)
        {
            Submission = submission;
            Accepted = accepted;
            RejectionReason = rejectionReason;
            SessionRejectionReason = sessionRejectionReason;
            Detail = detail;
            Version = version;
            Sequence = sequence;
            Events = events;
            Projection = projection;
            CommandResult = commandResult;
            OperationResult = operationResult;
        }

        public MatchSubmission Submission { get; }
        public bool Accepted { get; }
        public MatchHostRejectionReason RejectionReason { get; }

        /// <summary>
        /// The authority's own reason, and <c>None</c> whenever the host refused before reaching it.
        /// </summary>
        public MatchSessionRejectionReason SessionRejectionReason { get; }

        public string Detail { get; }

        /// <summary>The authoritative version after this submission; unmoved by a refusal.</summary>
        public long Version { get; }

        /// <summary>
        /// The stream sequence this submission appended, and <c>0</c> when it was refused. A refusal
        /// advances neither the version nor the stream, which is 8.29's rule that no rejection
        /// advances state.
        /// </summary>
        public long Sequence { get; }

        public IReadOnlyList<RoundResolutionEvent> Events { get; }
        public MatchSessionProjection Projection { get; }

        /// <summary>The authority's command result, and <c>null</c> for the operation kinds.</summary>
        public MatchCommandResult CommandResult { get; }

        /// <summary>The authority's operation result, and <c>null</c> for a command.</summary>
        public MatchSessionOperationResult OperationResult { get; }
    }

    /// <summary>
    /// Milestone 8.30. The transport-neutral port in front of one authoritative match session.
    /// </summary>
    /// <remarks>
    /// <para>Every consumer of match authority goes through this: the local director adapter today,
    /// and the UGS/Relay host milestone 8.31 onward adds. Neither one may hold rules, and neither
    /// one may reach the session behind it - which is why the port carries the whole mutating
    /// surface, the ordered stream of what those mutations did, and the snapshot.</para>
    /// <para>It names no transport, no seat and no connection. The decision record keeps every UGS
    /// and UTP type in Unity adapters beneath this line; removing the provider deletes those
    /// adapters and leaves this interface and everything below it untouched.</para>
    /// </remarks>
    public interface IMatchHost
    {
        /// <summary>The authoritative projection version, advanced once per accepted submission.</summary>
        long Version { get; }

        /// <summary>The sequence of the newest record in the ordered result stream.</summary>
        long Sequence { get; }

        /// <summary>
        /// The current authoritative projection. Milestone 8.30a replaces this one global read model
        /// with six owner-scoped projections; until it does, this is the same full-trust view the
        /// director already had, and `F-ER2-07` records that.
        /// </summary>
        MatchSessionProjection Projection { get; }

        MatchHostResult Submit(MatchSubmission submission);

        /// <summary>
        /// The ordered records after <paramref name="afterSequence"/>, or an incomplete read when
        /// the host can no longer serve that cursor.
        /// </summary>
        MatchHostResultStreamRead ReadResults(long afterSequence);

        string CaptureSnapshot();

        /// <summary>
        /// Milestone 8.30a. The seats of this match, derived from the authoritative participant
        /// graph rather than configured beside it.
        /// </summary>
        MatchSeatBinding Seats { get; }

        /// <summary>
        /// Milestone 8.30a. One seat's authorized view. An unknown seat is a programming error here,
        /// because a caller that cannot name a real seat has nothing to be told about.
        /// </summary>
        SeatProjection ProjectionForSeat(SeatId seat);

        /// <summary>
        /// Milestone 8.30a. Executes one player submission as the seat it arrived on, having
        /// authorized it against that seat's own published surface.
        /// </summary>
        SeatSubmissionResult SubmitForSeat(SeatId seat, SeatSubmission submission);

        /// <summary>
        /// Records one bounded public preference/agreement statement as the trusted seat it arrived
        /// on. It reaches the rules authority only when the coordinator has unanimous agreement.
        /// </summary>
        TeamAgreementSubmissionResult SubmitAgreementForSeat(
            SeatId seat, TeamAgreementSubmission submission);

        /// <summary>
        /// Milestone 8.35f. Steps one seat's own turn back by one accepted command, having
        /// authorized the rewind against the bounds that seat's own projection published.
        /// </summary>
        SeatUndoResult SubmitUndoForSeat(SeatId seat, SeatUndoSubmission submission);

        /// <summary>
        /// Milestone 8.30a. The client bound to that seat: everything a player surface may see and
        /// do, and nothing the Director may.
        /// </summary>
        ISeatClient ClientForSeat(SeatId seat);
    }

    /// <summary>
    /// Milestone 8.30. The serialized executor around one Unity-free <see cref="IMatchSession"/>.
    /// </summary>
    /// <remarks>
    /// <para>Serialized in both senses the decision record needs. Concurrent submissions from
    /// different connections queue on one gate, so the authority never sees two at once; and a
    /// re-entrant submission - a callback that submits from inside a submission, which is what a
    /// transport callback would be - is refused outright rather than being let through by a lock the
    /// calling thread already holds.</para>
    /// <para>The host adds no rules and no authority of its own. It owns exactly three things the
    /// session does not: the order submissions execute in, the ordered stream of what they did, and
    /// the refusal that keeps that order true.</para>
    /// </remarks>
    public sealed partial class MatchHost : IMatchHost
    {
        /// <summary>
        /// How many records the ordered stream retains. A consumer whose cursor falls behind this is
        /// told to resynchronize instead of being handed a gap.
        ///
        /// <para>Bounded so that a long match cannot accumulate a second copy of its whole event
        /// history beside the authority's own log - the growth 7.9b's diagnostic work had to remove
        /// once already. The number is large enough for a local adapter, which consumes after every
        /// submission, and for a client that misses a burst; the reconnect window a network host
        /// actually needs is milestone 8.33's, sized there from measured traffic.</para>
        /// </summary>
        public const int RetainedResultCount = 64;

        /// <summary>
        /// Milestone 8.30a. How many submission identities the host remembers, so that a replay of
        /// one is recognized rather than executed a second time.
        ///
        /// <para>Milestone 8.33 sized it from measured retry traffic and made it the protocol
        /// constant the decision record asked for; see
        /// <see cref="PrivateSessionProtocol.SubmissionIdempotencyWindowEntries"/> for the
        /// measurement and the growth check that guards it.</para>
        /// </summary>
        public const int RetainedSubmissionIdCount =
            PrivateSessionProtocol.SubmissionIdempotencyWindowEntries;

        /// <summary>
        /// Milestone 8.35d. The most end-of-round stages one host-authored continuation will run
        /// before it stops and says so.
        ///
        /// <para>Rulebook p.8 has six stages and a round crosses them once, so any number above six
        /// is only a guard against a rules defect that keeps reporting progress without leaving the
        /// phase. It exists so that such a defect appears as a recorded refusal rather than as a
        /// host that never returns from a player's submission.</para>
        /// </summary>
        public const int EndOfRoundContinuationStageLimit = 32;

        /// <summary>
        /// Milestone 8.35d. How many refusals the continuation's own record keeps.
        /// </summary>
        public const int RetainedContinuationRefusalCount = 8;

        /// <summary>
        /// Milestone 8.34. What one consumed submission identity answered, so that an identical
        /// repeat of it is answered the same way instead of being executed a second time.
        /// </summary>
        /// <remarks>
        /// "Identical" is the whole of it: the same identity, from the same seat, asking the same
        /// thing. A repeat that matches on the identity alone is a different question wearing a
        /// used name, and it is refused rather than answered from here.
        /// </remarks>
        private sealed class CachedSubmissionOutcome
        {
            public SeatId Seat;
            public DirectorCommand Command;
            public TeamAgreementCommand Statement;
            public bool Accepted;
            public SeatSubmissionRejectionReason SeatReason;
            public TeamAgreementSubmissionRejectionReason AgreementReason;
            public MatchSessionRejectionReason SessionReason;
            public string Detail;
            public long Version;
            public long Sequence;

            public bool Answers(SeatId seat, DirectorCommand command) =>
                Command != null && Statement == null &&
                Seat.Equals(seat) && Command.Equals(command);

            public bool Answers(SeatId seat, TeamAgreementCommand statement) =>
                Statement != null && Command == null &&
                Seat.Equals(seat) && Statement.Equals(statement);

            /// <summary>Milestone 8.35f. An undo carries no payload, so its identity is the whole of it.</summary>
            public bool IsUndo;

            public bool AnswersUndo(SeatId seat) =>
                IsUndo && Command == null && Statement == null && Seat.Equals(seat);
        }

        private readonly IMatchSession session;
        private readonly object gate = new object();
        private readonly List<MatchHostResultRecord> retained = new List<MatchHostResultRecord>();
        private readonly HashSet<SubmissionId> consumedSubmissionIds = new HashSet<SubmissionId>();
        private readonly Queue<SubmissionId> consumedSubmissionOrder = new Queue<SubmissionId>();
        private readonly Dictionary<SubmissionId, CachedSubmissionOutcome> cachedOutcomes =
            new Dictionary<SubmissionId, CachedSubmissionOutcome>();
        private readonly Queue<SubmissionId> cachedOutcomeOrder = new Queue<SubmissionId>();
        private readonly TeamAgreementCoordinator teamAgreement = new TeamAgreementCoordinator();
        private readonly List<string> continuationRefusals = new List<string>();
        private MatchParticipants seatedParticipants;
        private MatchSeatBinding seatBinding;
        private bool executing;
        private long sequence;

        // Milestone 8.35f. Where one seat's own undo may not step past, tracked as the match runs
        // rather than searched for in the history afterwards. Four facts, and each is one clause of
        // the user's printed rule.
        private string undoTurnMarker = string.Empty;
        private SeatId undoActorSeat;

        /// <summary>
        /// Milestone 8.35r. The rewind run currently standing: whose it is, and how far it has gone.
        /// Held here rather than in the session because only this door knows WHICH hero rewound -
        /// <c>MatchSession.Undo</c> takes no seat and records nothing.
        /// </summary>
        private GuardsDigital.Core.Units.Heroes.HeroId undoRunHeroId;
        private int undoRunCount;

        private int undoFloor;
        private bool undoAttackUnderWay;

        /// <summary>
        /// Milestone 8.35p. Whether rulebook p.14's action was still resolving at the previous
        /// watermark refresh, so the refresh after it can tell an Attack that finished from one that
        /// was never running. A card text that names its own target answers and completes the whole
        /// action in one command, and the difference between those two states is the only place that
        /// moment is visible.
        /// </summary>
        private bool undoAttackStanding;

        /// <summary>
        /// Milestone 8.42e. What the acting seat's opponents owned at the previous watermark
        /// refresh, so the refresh after each accepted command can see whether that command
        /// touched any of it. Recomputed rather than carried across a turn boundary, because it is
        /// relative to whose turn it is.
        /// </summary>
        private string undoOpposingFingerprint = string.Empty;

        /// <summary>
        /// Milestone 8.42e. Why the floor stands where it does, when the reason is one a player
        /// would otherwise have to guess. Empty means the floor is simply the turn's own beginning.
        /// The SIXTH amendment's rule reaches the refusal text too: nothing is withheld without
        /// saying why, and "you have already touched an opponent" is not something the generic
        /// sentence conveys.
        /// </summary>
        private string undoFloorDetail = string.Empty;

        public MatchHost(IMatchSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            Experiments = (session as MatchSession)?.Experiments ?? ExperimentalMatchOptions.Disabled;
            clock = new MatchClock(Experiments);
            timeoutSeed=(session as MatchSession)?.SetupSeed.Value ?? 0;
            if(Experiments.TurnTimer && timeoutSeed==0) timeoutSeed=GuardsDigital.Core.Setup.SetupRandomnessAuthority.DrawSeed().Value;
        }

        /// <summary>
        /// Composes the host and the authority it fronts from one initial state.
        ///
        /// <para>The whole composition is Unity-free and takes only authoritative inputs, so no
        /// presentation-owned content selection can reach it - which is how `RISK-ONLINE-001`'s
        /// scene-fixture override stays mechanically outside the host composition rather than by
        /// convention.</para>
        /// </summary>
        public static MatchHost Compose(MatchSessionInitialState initialState)
        {
            if (initialState == null) throw new ArgumentNullException(nameof(initialState));
            return new MatchHost(new MatchSession(initialState));
        }

        public long Version => session.Projection.Version;

        public long Sequence
        {
            get { lock (gate) { return sequence; } }
        }

        public MatchSessionProjection Projection => session.Projection;

        /// <summary>
        /// Milestone 8.35d. Every reason the host's end-of-round continuation stopped without
        /// having reached a pause, a phase change or a finished game, oldest first.
        /// </summary>
        /// <remarks>
        /// It is empty in every ordinary match, and a non-empty list is a defect signal rather than
        /// a gameplay state: the continuation only ever submits the one command the authority
        /// itself says is the next thing to do, so a refusal means the two disagree. Recorded and
        /// readable instead of retried, because a host that keeps pressing a refused advance is the
        /// spin the seat surface was just relieved of.
        /// </remarks>
        public IReadOnlyList<string> EndOfRoundContinuationRefusals
        {
            get { lock (gate) { return continuationRefusals.ToArray(); } }
        }

        public MatchHostResult Submit(MatchSubmission submission)
        {
            if (submission == null) throw new ArgumentNullException(nameof(submission));

            lock (gate)
            {
                if (executing)
                {
                    // Monitor is re-entrant, so the lock above admitted a nested call on this
                    // thread. This flag is what makes the refusal about the submission rather than
                    // about the thread that made it.
                    return HostRefused(
                        submission,
                        MatchHostRejectionReason.SubmissionInProgress,
                        "The host is already executing a submission. A submission made from inside " +
                        "another one is refused before the authority is reached.");
                }

                executing = true;
                MatchHostResult result;
                try
                {
                    DuringSubmissionForTest?.Invoke();
                    result = Execute(submission, submission.Kind == MatchSubmissionKind.Undo);
                }
                finally
                {
                    executing = false;
                }

                // Milestone 8.35f. The Director's own door keeps the watermark honest without
                // raising the other-seat wall: a Director command is nobody's seat answer.
                if (result.Accepted)
                {
                    RefreshSeatUndoWatermark(default, submission.Command);
                }

                return result;
            }
        }

        public MatchHostResultStreamRead ReadResults(long afterSequence)
        {
            if (afterSequence < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(afterSequence), afterSequence, "A stream cursor is not negative.");
            }

            lock (gate)
            {
                if (afterSequence > sequence)
                {
                    return new MatchHostResultStreamRead(
                        false, Array.Empty<MatchHostResultRecord>(), sequence);
                }

                var oldestRetained = retained.Count == 0 ? sequence + 1 : retained[0].Sequence;
                if (afterSequence + 1 < oldestRetained && afterSequence < sequence)
                {
                    return new MatchHostResultStreamRead(
                        false, Array.Empty<MatchHostResultRecord>(), sequence);
                }

                return new MatchHostResultStreamRead(
                    true,
                    retained.Where(record => record.Sequence > afterSequence).ToArray(),
                    sequence);
            }
        }

        public string CaptureSnapshot() => CaptureExperimentalSnapshot();

        /// <summary>
        /// Milestone 8.30a. The seats, rebuilt whenever the authority publishes a different
        /// participant graph.
        /// </summary>
        /// <remarks>
        /// A derived cache in the same sense the Undo checkpoints are: discarding it would change
        /// only how often the binding is recomputed. Keying it on the authority's own participants
        /// object is what makes a snapshot restore or a session replacement reseat from the restored
        /// authority instead of carrying the seats it had before.
        /// </remarks>
        public MatchSeatBinding Seats
        {
            get
            {
                lock (gate)
                {
                    return CurrentSeats();
                }
            }
        }

        public SeatProjection ProjectionForSeat(SeatId seat)
        {
            lock (gate)
            {
                var bound = CurrentSeats().Find(seat);
                if (bound == null)
                {
                    throw new ArgumentException(
                        "No seat '" + seat + "' is bound in this match.", nameof(seat));
                }

                return ProjectionForBound(bound);
            }
        }

        public ISeatClient ClientForSeat(SeatId seat)
        {
            lock (gate)
            {
                var bound = CurrentSeats().Find(seat);
                if (bound == null)
                {
                    throw new ArgumentException(
                        "No seat '" + seat + "' is bound in this match.", nameof(seat));
                }

                return new SeatClient(this, bound);
            }
        }

        /// <summary>
        /// Milestone 8.30a. The whole player path into match authority.
        /// </summary>
        /// <remarks>
        /// <para>Every check below runs before the authority is touched and reads only committed
        /// state, so a refused submission cannot have applied a part of itself. They are ordered so
        /// that each answers a different question: is this a seat, is this answer about the board as
        /// it stands, has this attempt already been made, is the payload claiming an identity this
        /// seat does not have, and is this one of the things the seat's own projection offered.</para>
        /// <para>The last of those is the load-bearing one. A command is authorized because the seat
        /// published it, not because it looks legal, so a rules-legal command belonging to another
        /// seat is refused without the rules ever being consulted - and the authority still
        /// revalidates whatever gets through.</para>
        /// <para>Only a submission that reaches the authority consumes its identity. An envelope
        /// refusal leaves it unused, so a client whose expected version was stale re-reads and
        /// retries the same attempt rather than having to invent a second name for it.</para>
        /// </remarks>
        public SeatSubmissionResult SubmitForSeat(SeatId seat, SeatSubmission submission)
        {
            if (submission == null) throw new ArgumentNullException(nameof(submission));

            lock (gate)
            {
                var bound = CurrentSeats().Find(seat);
                if (bound == null)
                {
                    return SeatRefused(
                        submission,
                        null,
                        SeatSubmissionRejectionReason.UnknownSeat,
                        "No seat '" + seat + "' is bound in this match.");
                }

                if (executing)
                {
                    return SeatRefused(
                        submission,
                        bound,
                        SeatSubmissionRejectionReason.HostRefused,
                        "The host is already executing a submission.");
                }

                // Milestone 8.34. The identity is asked about before the version is, and the order
                // is the failure policy's: an identity that already reached the authority is
                // answered from what it did, not from what the board looks like now. Telling a
                // retry that its view is stale would invite the client to ask again under a new
                // name, which is the one way a submission gets executed twice.
                if (consumedSubmissionIds.Contains(submission.Id))
                {
                    if (cachedOutcomes.TryGetValue(submission.Id, out var cached) &&
                        cached.Answers(bound.Id, submission.Command))
                    {
                        return new SeatSubmissionResult(
                            submission,
                            cached.Accepted,
                            cached.SeatReason,
                            cached.SessionReason,
                            cached.Detail,
                            cached.Version,
                            cached.Sequence,
                            ProjectionForBound(bound));
                    }

                    return SeatRefused(
                        submission,
                        bound,
                        SeatSubmissionRejectionReason.DuplicateSubmission,
                        "Submission '" + submission.Id + "' has already reached the authority.");
                }

                var projection = ProjectionForBound(bound);
                if (submission.ExpectedVersion != projection.Version)
                {
                    return SeatRefused(
                        submission,
                        bound,
                        SeatSubmissionRejectionReason.StaleProjection,
                        "The submission expects version " + submission.ExpectedVersion +
                        " and the authoritative version is " + projection.Version + ".");
                }

                if(Experiments.TurnTimer && SharedBoardChoice.RequiresVote(session.Projection,submission.Command))
                    return SeatRefused(submission,bound,SeatSubmissionRejectionReason.UnauthorizedCommand,"This shared board choice requires a team vote.");
                var claimed = SeatCommandOwnership.Claim(submission.Command);
                if (!claimed.IsHeldBy(bound))
                {
                    return SeatRefused(
                        submission,
                        bound,
                        SeatSubmissionRejectionReason.OwnershipMismatch,
                        "The payload claims " + claimed + ", which seat " + bound.Id + " does not own.");
                }

                if (!projection.AuthorizedCommands.Contains(submission.Command))
                {
                    return SeatRefused(
                        submission,
                        bound,
                        SeatSubmissionRejectionReason.UnauthorizedCommand,
                        "The command is not in the " + projection.AuthorizedCommands.Count +
                        " this seat's projection published at version " + projection.Version + ".");
                }

                ConsumeSubmissionId(submission.Id);
                executing = true;
                MatchHostResult inner;
                try
                {
                    DuringSubmissionForTest?.Invoke();
                    inner = Execute(MatchSubmission.ForCommand(submission.Command));
                    if (inner.Accepted)
                    {
                        ContinueEndOfRound();
                    }
                }
                finally
                {
                    executing = false;
                }

                if (inner.Accepted)
                {
                    RefreshSeatUndoWatermark(bound.Id, submission.Command);
                }

                var outcome = new CachedSubmissionOutcome
                {
                    Seat = bound.Id,
                    Command = submission.Command,
                    Accepted = inner.Accepted,
                    SeatReason = inner.Accepted
                        ? SeatSubmissionRejectionReason.None
                        : SeatSubmissionRejectionReason.SessionRefused,
                    AgreementReason = TeamAgreementSubmissionRejectionReason.None,
                    SessionReason = inner.SessionRejectionReason,
                    Detail = inner.Detail,

                    // Milestone 8.35d. The version this answer settled on, which is the authority's
                    // own after anything the host continued through on its back. Reporting the
                    // version from before the continuation would make every seat's next submission
                    // stale by construction.
                    Version = inner.Accepted ? session.Projection.Version : inner.Version,
                    Sequence = inner.Sequence
                };
                CacheOutcome(submission.Id, outcome);

                return new SeatSubmissionResult(
                    submission,
                    outcome.Accepted,
                    outcome.SeatReason,
                    outcome.SessionReason,
                    outcome.Detail,
                    outcome.Version,
                    outcome.Sequence,
                    ProjectionForBound(bound));
            }
        }

        public TeamAgreementSubmissionResult SubmitAgreementForSeat(
            SeatId seat, TeamAgreementSubmission submission)
        {
            if (submission == null) throw new ArgumentNullException(nameof(submission));

            lock (gate)
            {
                var bound = CurrentSeats().Find(seat);
                if (bound == null)
                    return AgreementRefused(
                        null, TeamAgreementSubmissionRejectionReason.UnknownSeat,
                        "No seat '" + seat + "' is bound in this match.");
                if (executing)
                    return AgreementRefused(
                        bound, TeamAgreementSubmissionRejectionReason.HostRefused,
                        "The host is already executing a submission.");

                // Milestone 8.34. The identity before the version, exactly as the command path
                // orders them: a statement that already reached the coordinator is answered from
                // what it did there.
                if (consumedSubmissionIds.Contains(submission.Id))
                {
                    if (cachedOutcomes.TryGetValue(submission.Id, out var cached) &&
                        cached.Answers(bound.Id, submission.Command))
                    {
                        return new TeamAgreementSubmissionResult(
                            cached.Accepted,
                            cached.AgreementReason,
                            cached.Detail,
                            cached.Version,
                            cached.Sequence,
                            ProjectionForBound(bound));
                    }

                    return AgreementRefused(
                        bound, TeamAgreementSubmissionRejectionReason.DuplicateSubmission,
                        "Submission '" + submission.Id + "' has already been consumed.");
                }

                var projection = ProjectionForBound(bound);
                if (submission.ExpectedVersion != projection.Version)
                    return AgreementRefused(
                        bound, TeamAgreementSubmissionRejectionReason.StaleProjection,
                        "The submission expects version " + submission.ExpectedVersion +
                        " and the authoritative version is " + projection.Version + ".");
                if (projection.TeamAgreement == null ||
                    !projection.TeamAgreement.ProposalId.Equals(submission.Command.ProposalId))
                    return AgreementRefused(
                        bound, TeamAgreementSubmissionRejectionReason.StaleProposal,
                        "The agreement proposal has changed.");
                if (!projection.TeamAgreement.AuthorizedCommands.Contains(submission.Command))
                    return AgreementRefused(
                        bound, TeamAgreementSubmissionRejectionReason.IneligibleSeat,
                        "This seat was not offered that statement for the current proposal.");

                ConsumeSubmissionId(submission.Id);
                executing = true;
                try
                {
                    DuringSubmissionForTest?.Invoke();
                    var applied = teamAgreement.Apply(
                        session.Projection, CurrentSeats(), bound, submission.Command);
                    if (!applied.Accepted)
                        return CachedAgreementOutcome(
                            submission, bound, false,
                            TeamAgreementSubmissionRejectionReason.UnauthorizedStatement,
                            applied.Detail, session.Projection.Version, 0);

                    if (!applied.HasFinalAnswer)
                        return CachedAgreementOutcome(
                            submission, bound, true,
                            TeamAgreementSubmissionRejectionReason.None, string.Empty,
                            session.Projection.Version, 0);

                    var finalCommand = applied.FinalCommand;
                    MatchHostResult final;
                    if (applied.AgreedRemovals == null)
                    {
                        final = Execute(MatchSubmission.ForCommand(finalCommand));
                    }
                    else
                    {
                        // Milestone 9.29. Every choosing hero picked the same set, so the host
                        // carries it into the authority as the Director would: one existing Select
                        // per unit, each read off the contract standing at that moment, then the
                        // existing Confirm. Bounded, because each accepted Select moves one unit
                        // into the selection - after any stray one is taken back - and the Confirm
                        // ends the choice.
                        final = null;
                        var stepLimit = (applied.AgreedRemovals.Count * 2) + 2;
                        for (var step = 0; step < stepLimit; step++)
                        {
                            finalCommand = TeamAgreementCoordinator.NextMinionBattleCommand(
                                session.Projection, applied.AgreedRemovals);
                            if (finalCommand == null)
                            {
                                final = null;
                                break;
                            }

                            final = Execute(MatchSubmission.ForCommand(finalCommand));
                            if (!final.Accepted ||
                                finalCommand.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals)
                                break;
                        }

                        if (final != null && final.Accepted &&
                            finalCommand?.Kind != DirectorCommandKind.ConfirmMinionBattleRemovals)
                            final = null;
                    }

                    if (final == null || !final.Accepted)
                        return CachedAgreementOutcome(
                            submission, bound, false,
                            TeamAgreementSubmissionRejectionReason.FinalRulesChoiceRefused,
                            final?.Detail ?? "The agreed removals are not offered by the standing choice.",
                            session.Projection.Version, 0);

                    teamAgreement.Clear();

                    // Milestone 8.35d. A unanimous confirmation is the other way a seat-served
                    // match reaches or leaves an end-of-round stage - p.8's Minion Battle is
                    // confirmed exactly here - so the continuation runs from this door too.
                    ContinueEndOfRound();
                    RefreshSeatUndoWatermark(bound.Id, finalCommand);
                    return CachedAgreementOutcome(
                        submission, bound, true,
                        TeamAgreementSubmissionRejectionReason.None, string.Empty,
                        session.Projection.Version, final.Sequence);
                }
                finally
                {
                    executing = false;
                }
            }
        }

        private MatchSeatBinding CurrentSeats()
        {
            var participants = session.Projection.Participants;
            if (!ReferenceEquals(participants, seatedParticipants))
            {
                seatedParticipants = participants;
                seatBinding = MatchSeatBinding.ForParticipants(participants);
            }

            return seatBinding;
        }

        /// <summary>
        /// Milestone 8.35r. The standing rewind as a published fact, or none. Every seat receives
        /// the same one: that a player is taking their turn back is table-public, in the same sense
        /// and for the same reason as the standing decision's owner.
        /// </summary>
        private MatchUndoMarker CurrentUndoMarker() =>
            undoRunCount > 0 && undoRunHeroId.IsDefined
                ? MatchUndoMarker.Of(undoRunHeroId, undoRunCount)
                : MatchUndoMarker.None;

        private SeatProjection ProjectionForBound(MatchSeat seat)
        {
            var source = session.Projection;
            var agreement = teamAgreement.ProjectionFor(source, CurrentSeats(), seat);
            return SeatProjectionFactory.ForSeat(
                source, seat, agreement, CanSeatUndo(seat, out _), CurrentUndoMarker(), Experiments,
                CoordinationFor(seat), coordination.Revision, coordination.Generation, ClockState);
        }

        private void ConsumeSubmissionId(SubmissionId id)
        {
            consumedSubmissionIds.Add(id);
            consumedSubmissionOrder.Enqueue(id);
            while (consumedSubmissionOrder.Count > RetainedSubmissionIdCount)
            {
                consumedSubmissionIds.Remove(consumedSubmissionOrder.Dequeue());
            }
        }

        /// <summary>
        /// Milestone 8.34. Remembers what one consumed identity answered, for as long as the
        /// ordered result stream remembers the record it produced.
        /// </summary>
        /// <remarks>
        /// The two windows are deliberately different sizes, and the difference is the failure
        /// policy's two clauses. The identity window is the wider one, so a repeat is recognized
        /// as a repeat long after its answer has aged out; the outcome window is the result
        /// stream's own, so an answer is replayed only while the host can still account for it.
        /// A repeat inside the first and outside the second is refused rather than answered - it
        /// is still never executed twice. Milestone 8.33's measurement is what says a real
        /// reconnect retry lands inside the narrower one: the worst per-seat distance between two
        /// of a seat's own consumed identities across the deterministic walk is 27.
        /// </remarks>
        private void CacheOutcome(SubmissionId id, CachedSubmissionOutcome outcome)
        {
            cachedOutcomes[id] = outcome;
            cachedOutcomeOrder.Enqueue(id);
            while (cachedOutcomeOrder.Count > RetainedResultCount)
            {
                cachedOutcomes.Remove(cachedOutcomeOrder.Dequeue());
            }
        }

        private TeamAgreementSubmissionResult CachedAgreementOutcome(
            TeamAgreementSubmission submission,
            MatchSeat bound,
            bool accepted,
            TeamAgreementSubmissionRejectionReason reason,
            string detail,
            long version,
            long sequence)
        {
            CacheOutcome(submission.Id, new CachedSubmissionOutcome
            {
                Seat = bound.Id,
                Statement = submission.Command,
                Accepted = accepted,
                SeatReason = SeatSubmissionRejectionReason.None,
                AgreementReason = reason,
                SessionReason = MatchSessionRejectionReason.None,
                Detail = detail ?? string.Empty,
                Version = version,
                Sequence = sequence
            });

            return new TeamAgreementSubmissionResult(
                accepted, reason, detail ?? string.Empty, version, sequence,
                ProjectionForBound(bound));
        }

        private SeatSubmissionResult SeatRefused(
            SeatSubmission submission,
            MatchSeat seat,
            SeatSubmissionRejectionReason reason,
            string detail) =>
            new SeatSubmissionResult(
                submission,
                false,
                reason,
                MatchSessionRejectionReason.None,
                detail,
                session.Projection.Version,
                0,
                seat == null ? null : ProjectionForBound(seat));

        private TeamAgreementSubmissionResult AgreementRefused(
            MatchSeat seat,
            TeamAgreementSubmissionRejectionReason reason,
            string detail) =>
            new TeamAgreementSubmissionResult(
                false, reason, detail, session.Projection.Version, 0,
                seat == null ? null : ProjectionForBound(seat));

        /// <summary>
        /// Runs inside a submission, before the authority is reached. The one deterministic way to
        /// observe the serialized executor from a fixture: a hook that submits from in here is
        /// exactly the re-entrant transport callback the refusal exists for. Nothing in a build sets
        /// it, in the same sense as <see cref="MatchSession.FaultNextProjectionForTest"/>.
        /// </summary>
        internal Action DuringSubmissionForTest { get; set; }

        private MatchHostResult Execute(MatchSubmission submission, bool directorUndo = false)
        {
            BeforeTimedSubmission();
            var result = ExecuteInner(submission);
            if (result.Accepted) AfterTimedSubmission(submission, directorUndo);

            // Milestone 8.35r. Every accepted submission retires the standing rewind marker,
            // including an accepted Undo. A seat's own rewind run is re-established by
            // SubmitUndoForSeat immediately after this returns, from the count it captured before
            // calling in - so the run survives its own undo and nothing else. Clearing here rather
            // than at the several accepted-command sites is deliberate: this is the one boundary
            // every mutating submission passes through, and a marker that outlived a Restart or a
            // snapshot restore would be describing a history that no longer exists.
            if (result.Accepted)
            {
                undoRunHeroId = default;
                undoRunCount = 0;
            }

            return result;
        }

        private MatchHostResult ExecuteInner(MatchSubmission submission)
        {
            switch (submission.Kind)
            {
                case MatchSubmissionKind.Command:
                    return FromCommand(submission, session.Execute(submission.Command));
                case MatchSubmissionKind.Undo:
                    return FromOperation(submission, session.Undo());
                case MatchSubmissionKind.Restart:
                    var restart = session.Restart();
                    if (restart.Accepted) coordination.Clear();
                    return FromOperation(submission, restart);
                case MatchSubmissionKind.RestoreSnapshot:
                    return FromOperation(submission, session.RestoreSnapshot(UnwrapExperimentalSnapshot(submission.SnapshotPayload)));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(submission),
                        submission.Kind,
                        "A submission kind the host cannot execute reached it. Every member of " +
                        nameof(MatchSubmissionKind) + " that mutates authority has a case here.");
            }
        }

        /// <summary>
        /// Milestone 8.35d. Runs rulebook p.8's end-of-round sequence for a seat-served match, in
        /// the authority's own stage order, as far as it goes without asking anybody anything.
        /// </summary>
        /// <remarks>
        /// <para>Reached only from the two seat doors - <see cref="SubmitForSeat"/> and the
        /// unanimous final of <see cref="SubmitAgreementForSeat"/> - and never from
        /// <see cref="Submit"/>, which is the Director's. That is the whole of the opt-in: seat-
        /// served play advances itself and Director mode keeps stepping the round by hand, without
        /// a policy flag either surface could be composed with the wrong way round. Every path into
        /// end of round in a seat-served match is an accepted seat submission, so "when the phase
        /// is entered" and "after every accepted operation" are the same hook.</para>
        /// <para>It runs inside the caller's own <c>executing</c> window, so a re-entrant
        /// submission is refused while it works, and each advance is an ordinary accepted
        /// <see cref="DirectorCommand.AdvanceEndOfRound"/> appended to the one recorded stream.
        /// Replay, retained-prefix Undo, snapshots and restore therefore see the same command
        /// sequence they always did - the host authored some of it rather than a player, and
        /// nothing downstream can tell or needs to.</para>
        /// <para>It stops on a standing decision of ANY family, on leaving the phase, on the game
        /// ending, and on a refusal - which is a defect signal, recorded rather than retried.</para>
        /// </remarks>
        private void ContinueEndOfRound()
        {
            for (var stage = 0; stage < EndOfRoundContinuationStageLimit; stage++)
            {
                var projection = session.Projection;
                if (projection.Phase != RoundResolutionPhase.EndOfRound ||
                    projection.DecisionContract != null)
                {
                    return;
                }

                var advance = Execute(MatchSubmission.ForCommand(DirectorCommand.AdvanceEndOfRound()));
                if (!advance.Accepted)
                {
                    RecordContinuationRefusal(
                        "The end-of-round continuation was refused at version " + advance.Version +
                        " on stage " + (projection.CurrentEndOfRoundStage.HasValue
                            ? projection.CurrentEndOfRoundStage.Value.ToString()
                            : "none") +
                        " as " + advance.SessionRejectionReason + ": " + advance.Detail);
                    return;
                }
            }

            RecordContinuationRefusal(
                "The end-of-round continuation ran " + EndOfRoundContinuationStageLimit +
                " advances without leaving end of round or reaching a decision.");
        }

        /// <summary>
        /// Milestone 8.35f. Where this seat's own undo may not step past, brought up to date after
        /// every accepted submission.
        /// </summary>
        /// <remarks>
        /// <para>Tracked forward as the match runs rather than reconstructed by walking the
        /// recorded history backwards, because the two questions the bounds turn on - which seat
        /// answered a command, and whether an attack action has finished - are answered exactly
        /// once, at the moment each command is accepted, and nothing in the stream itself records
        /// them. It is a derived watermark in the same sense as the seat binding: discarding it
        /// would only cost the seat its undo until its next turn.</para>
        /// <para>The three bounds are the user's own rule. The <b>turn floor</b> is where the
        /// actor's card resolution became active with nothing taken yet, so an undo never reaches
        /// commitments, Initiative, an agreement or a previous turn. The <b>attack wall</b> rises
        /// the moment a target is chosen and settles above the whole p.14 action once it finishes -
        /// the defender's answer is information that cannot be un-shown, so the action is never
        /// undoable and there is no undo at all while it is running. Choices printed after that
        /// completed Attack are a new actor-owned segment above the wall and remain rewindable.
        /// The <b>other-seat wall</b>
        /// rises above anything a different seat answered inside this turn, so one player's rewind
        /// can never reach through another player's decision.</para>
        /// </remarks>
        private void RefreshSeatUndoWatermark(
            SeatId submittingSeat, DirectorCommand accepted, bool afterRewind = false)
        {
            var projection = session.Projection;
            var marker = TurnMarkerOf(projection);
            if (!string.Equals(marker, undoTurnMarker, StringComparison.Ordinal))
            {
                undoTurnMarker = marker;
                undoFloor = projection.AcceptedCommands.Count;
                undoAttackUnderWay = false;
                var actorId = projection.PendingTurnRelease != null
                    ? projection.PendingTurnRelease.ActorId
                    : projection.SelectedActor.ActorId;

                // Milestone 8.71b. The seat this turn's rewind belongs to is the seat that TAKES
                // this turn's decisions, and under Hanu the Trickster's The Ultimate Trick those
                // are not the same seat: the delegated hero submits nothing, so a rewind of theirs
                // would step back through commands another player sent, and every command the
                // deciding seat sends would otherwise raise the other-seat wall above itself and
                // leave the whole delegated action unrewindable by anyone (HA-Q4).
                //
                // Read once, where the actor's seat is read, and not per command: the delegation
                // stands for the whole of the action by construction - it ends when the action's
                // card resolves - and a seat whose rewind appeared and vanished inside one turn
                // would be a worse answer than either.
                //
                // What this does NOT relax is 8.42e's interaction wall. The fingerprint below is
                // taken against the DECIDING seat's opponents, so a delegated ENEMY hero moving is
                // a change to an opponent's own piece and walls the rewind exactly as any other
                // interaction does; a delegated ALLY's action stays the deciding seat's to take
                // back, which is the same line the user drew for a friendly buff.
                var deciderId = GuardsDigital.Core.Rules.CardResolution.DelegatedActionQuery.DecidingHeroFor(
                    projection.Definitions, projection.Cards, projection.Heroes, actorId);
                var actor = marker.Length == 0 ? null : CurrentSeats().FindByHero(deciderId);
                undoActorSeat = actor == null ? default : actor.Id;
                undoAttackStanding = projection.HasPendingAttackResolution;
                undoOpposingFingerprint = OpposingFingerprint(undoActorSeat);
                undoFloorDetail = string.Empty;
                return;
            }

            if (accepted != null && accepted.Kind == DirectorCommandKind.ChooseAttackTarget)
            {
                undoAttackUnderWay = true;
            }

            // Milestone 8.35p. The wall is "an Attack whose target has been chosen", and until this
            // milestone only the command that chooses one on rulebook p.14's ordinary path could
            // raise it. A card declaring AttackTargetBinding.ByCardText never emits that command:
            // Brogan's three charges and Tigerclaw's Blink Strike name their target inside the card
            // text through `set-attack-target@1`, and the player's command is an ordinary
            // AnswerEffectChoice. So for that whole class the wall never rose, and a driven charge
            // measured the consequence - the acting seat rewound its own RESOLVED Attack.
            //
            // Both halves are needed and each answers a different shape.
            //
            // (1) A standing resolution that has a target: the Attack has been answered and is still
            //     running. Before the target is named the same resolution is standing WITHOUT one -
            //     which is the charge's own destination question - and that stays rewindable, which
            //     is the amendment's rule exactly: the committed move is the actor's own command,
            //     rewindable until the attack target locks.
            //
            // (2) A resolution that was standing before this command and is gone after it: the
            //     Attack finished. The charge is why this is not redundant - naming the victim
            //     resolves the whole p.14 action inside that one command, so nothing is left standing
            //     to read and (1) alone never fires. The wall goes up on the finish so the clearing
            //     rule below settles the floor above the completed action, which is what the ordinary
            //     path gets for free from its own command kind.
            var resolution = projection.PendingAttackResolution;
            if (resolution != null ? resolution.Target.IsDefined : undoAttackStanding)
            {
                undoAttackUnderWay = true;
            }

            if (submittingSeat.IsDefined && !submittingSeat.Equals(undoActorSeat))
            {
                undoFloor = projection.AcceptedCommands.Count;
                undoFloorDetail = string.Empty;
            }

            // Milestone 8.42e. The INTERACTION WALL, the user's own rule from playtest on
            // 2026-09-18: "undo should stop at any interaction". A command that changed anything
            // the actor's OPPONENTS own puts the floor above itself, so a rewind can never step
            // back through it - the same shape as the other-seat wall above, and there for the
            // reason the attack wall already gives: they watched it happen to their own pieces and
            // it cannot be un-shown. It is narrower than the attack wall on purpose: what follows
            // the interaction, inside the same turn, stays the actor's own to take back.
            //
            // A rewind restores exactly what it undid, so the fingerprint is re-read on that path
            // WITHOUT raising the floor. Without that exception one accepted press would change
            // the board back, read as an interaction, and wall the seat out of its own next press.
            var opposing = OpposingFingerprint(undoActorSeat);
            if (!afterRewind &&
                !string.Equals(opposing, undoOpposingFingerprint, StringComparison.Ordinal))
            {
                undoFloor = projection.AcceptedCommands.Count;
                undoFloorDetail =
                    "This turn has already touched an opponent, and a rewind may not reach back " +
                    "through what another player has seen.";
            }

            undoOpposingFingerprint = opposing;

            // The p.14 action is over when no Attack resolution is standing. A card may immediately
            // continue with AFTER THE ATTACK choices, but those are a new actor-owned segment above
            // the wall: the defender's answer and the Attack remain below the floor, while a mistaken
            // branch choice (Walking Dead's Move rather than discard, for example) can be stepped
            // back. The other-seat owner check still withholds undo while an opponent is answering,
            // and their accepted answer raises the floor again, so this does not rewind through
            // information another player supplied.
            if (undoAttackUnderWay &&
                !projection.HasPendingAttackResolution)
            {
                undoFloor = projection.AcceptedCommands.Count;
                undoAttackUnderWay = false;
                undoFloorDetail = string.Empty;
            }

            undoAttackStanding = projection.HasPendingAttackResolution;
        }


        /// <summary>
        /// Milestone 8.42e. Everything the acting seat's OPPONENTS own, as one comparable string:
        /// where their heroes and minions stand, where their cards are, which markers their heroes
        /// hold, where the tokens they own sit, and their teams' life counters.
        /// </summary>
        /// <remarks>
        /// <para><b>The user's rule, from playtest on 2026-09-18: "undo should stop at any
        /// interaction".</b> 8.35f's attack wall already said that of rulebook p.14 - the defender's
        /// answer is information that cannot be un-shown - and this says it of everything else an
        /// opponent can watch happen to their own pieces. A card text that slides an enemy hero two
        /// spaces was rewindable until this milestone, measured five times in 2,400 commands of the
        /// seeded six-player walk, twice on an enemy HERO.</para>
        /// <para><b>Compared rather than deduced, and that is deliberate.</b> The alternative is to
        /// ask each effect whether it "interacted", which every new card would have to answer again
        /// and any card could answer wrongly by omission. What an opponent owns is observable at
        /// acceptance, so the watermark reads the board instead of trusting a declaration - the same
        /// reason the other three bounds are tracked forward here rather than reconstructed.</para>
        /// <para><b>Opponents only, by the user's decision.</b> A friendly buff or an ally's move
        /// stays rewindable; support heroes spend most of their text on allies and would otherwise
        /// lose their rewind on nearly every turn.</para>
        /// </remarks>
        private string OpposingFingerprint(SeatId actorSeat)
        {
            if (!actorSeat.IsDefined)
            {
                return string.Empty;
            }

            var seats = CurrentSeats();
            var actor = seats.Find(actorSeat);
            if (actor == null)
            {
                return string.Empty;
            }

            var side = actor.TeamSide;
            var opposingHeroes = new HashSet<GuardsDigital.Core.Units.Heroes.HeroId>(
                seats.Seats.Where(seat => seat.TeamSide != side).Select(seat => seat.HeroId));
            var opposingTeams = new HashSet<TeamId>(
                seats.Seats.Where(seat => seat.TeamSide != side).Select(seat => seat.TeamId));
            var projection = session.Projection;
            var builder = new System.Text.StringBuilder();

            foreach (var hero in projection.Heroes
                .Where(hero => opposingHeroes.Contains(hero.HeroId))
                .OrderBy(hero => hero.HeroId.Value, StringComparer.Ordinal))
            {
                builder.Append("h:").Append(hero.HeroId.Value).Append('@')
                    .Append(hero.Coordinate).Append(';');
            }

            foreach (var minion in projection.Minions
                .Where(minion => minion.TeamSide != side)
                .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal))
            {
                builder.Append("m:").Append(minion.MinionId.Value).Append('@')
                    .Append(minion.Coordinate).Append(';');
            }

            foreach (var card in projection.Cards
                .Where(card => opposingHeroes.Contains(card.OwnerId))
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal))
            {
                builder.Append("c:").Append(card.Id.Value).Append('@')
                    .Append(card.State.Location);

                // Milestone 8.71a. A REWRITTEN printed Initiative on an opponent's card is an
                // interaction, and before this line the fingerprint could not see one. Hanu the
                // Trickster's Hurry up! sets an enemy card's printed icon to 11 and moves nothing
                // else: no hero, no minion, no card location, no marker, no token, no life counter -
                // so every term above it compares equal and the turn stayed rewindable through a
                // change that had already reordered the round in front of the seat that owns the
                // card. That is precisely the "compare rather than deduce" promise of 8.42e's own
                // remarks failing at a state it had no term for, rather than a card declaring itself
                // wrongly.
                //
                // Appended only when a rewrite stands, so every board no Hurry up! has touched
                // produces the byte-identical string it produced before this milestone and no
                // shipped card's rewind moves.
                if (card.InitiativeOverride.IsDefined)
                {
                    builder.Append('!').Append(card.InitiativeOverride.Value);
                }

                builder.Append(';');
            }

            foreach (var marker in projection.HeldMarkers
                .Where(marker => opposingHeroes.Contains(marker.HolderHeroId))
                .OrderBy(marker => marker.Id.Value, StringComparer.Ordinal))
            {
                builder.Append("k:").Append(marker.Id.Value).Append('@')
                    .Append(marker.HolderHeroId.Value).Append(';');
            }

            foreach (var token in projection.PlacedTokens
                .Where(token => opposingHeroes.Contains(token.OwnerHeroId))
                .OrderBy(token => token.Id.Value, StringComparer.Ordinal))
            {
                builder.Append("t:").Append(token.Id.Value).Append('@')
                    .Append(token.Coordinate).Append('/')
                    .Append(token.ControllerHeroId.Value).Append(';');
            }

            foreach (var counter in projection.TeamLifeCounters
                .Where(counter => opposingTeams.Contains(counter.Key))
                .OrderBy(counter => counter.Key.Value, StringComparer.Ordinal))
            {
                builder.Append("l:").Append(counter.Key.Value).Append('@')
                    .Append(counter.Value).Append(';');
            }

            return builder.ToString();
        }

        /// <summary>
        /// One turn's identity, as the seat undo counts turns.
        /// </summary>
        /// <remarks>
        /// Milestone 8.35g. A turn holding still for its own player's release is the SAME turn it
        /// was while it resolved, and this has to say so: acknowledging the resolved card is what
        /// clears the selection, so keying on the selected actor alone would change the marker at
        /// the exact moment the resolution finished, reset the floor, and take the seat's rewind
        /// away in the window the whole release exists to keep it in. The pause carries the same
        /// hero and card the selection did, so the string is identical across the boundary.
        /// </remarks>
        private static string TurnMarkerOf(MatchSessionProjection projection)
        {
            var release = projection.PendingTurnRelease;
            if (release != null)
            {
                return projection.RoundIndex + ":" + projection.TurnIndex + ":" +
                    release.ActorId.Value + ":" + release.CardInstanceId.Value;
            }

            return !projection.IsTurnActive || !projection.SelectedActor.IsDefined
                ? string.Empty
                : projection.RoundIndex + ":" + projection.TurnIndex + ":" +
                    projection.SelectedActor.ActorId.Value + ":" +
                    projection.SelectedActor.CardInstanceId.Value;
        }

        /// <summary>
        /// Milestone 8.35f. Whether this seat may step back one command right now, and why not when
        /// it may not. The refusal detail is the same sentence the availability answers.
        /// </summary>
        private static bool CanRetractCommitment(MatchSessionProjection projection, GuardsDigital.Core.Units.Heroes.HeroId hero) =>
            projection.Phase == RoundResolutionPhase.AwaitingCommitments &&
            projection.Commitments.Any(c => c.Status == TurnCardCommitmentStatus.AwaitingResponse) &&
            projection.Commitments.Any(c => c.HeroId.Equals(hero) && c.Status == TurnCardCommitmentStatus.CardCommittedFacedown);

        private bool CanSeatUndo(MatchSeat seat, out string detail)
        {
            var projection = session.Projection;
            if (CanRetractCommitment(projection, seat.HeroId))
            {
                detail = string.Empty;
                return true;
            }
            if (!projection.IsHistoryAvailable)
            {
                detail = "This session has no retained history to step back through.";
                return false;
            }

            if (undoTurnMarker.Length == 0 || !undoActorSeat.IsDefined)
            {
                detail = "No turn is being resolved, so there is nothing of this seat's own to undo.";
                return false;
            }

            if (!undoActorSeat.Equals(seat.Id))
            {
                detail = "Seat " + seat.Id + " is not resolving this turn.";
                return false;
            }

            if (undoAttackUnderWay)
            {
                detail = "An Attack whose target has been chosen is resolving, and rulebook p.14's " +
                    "action is never undone once it has been answered.";
                return false;
            }

            if (projection.AcceptedCommands.Count <= undoFloor)
            {
                detail = undoFloorDetail.Length > 0
                    ? undoFloorDetail
                    : "This turn is already at the earliest state its own seat may return to.";
                return false;
            }

            var owner = SeatProjectionFactory.OwnerOfStandingDecision(projection);
            if (owner.IsDefined && !owner.IsOwnedBy(seat))
            {
                detail = "Another seat is answering a standing decision, and a rewind may not " +
                    "reach under it.";
                return false;
            }

            detail = string.Empty;
            return true;
        }

        public SeatUndoResult SubmitUndoForSeat(SeatId seat, SeatUndoSubmission submission)
        {
            if (submission == null) throw new ArgumentNullException(nameof(submission));

            lock (gate)
            {
                var bound = CurrentSeats().Find(seat);
                if (bound == null)
                {
                    return UndoRefused(
                        submission, null, SeatSubmissionRejectionReason.UnknownSeat,
                        "No seat '" + seat + "' is bound in this match.");
                }

                if (executing)
                {
                    return UndoRefused(
                        submission, bound, SeatSubmissionRejectionReason.HostRefused,
                        "The host is already executing a submission.");
                }

                // The identity before the version, exactly as the command and agreement paths order
                // them: a rewind that already happened is answered from what it did, never taken a
                // second time. This is the whole of what makes a double click safe, because an undo
                // is not idempotent.
                if (consumedSubmissionIds.Contains(submission.Id))
                {
                    if (cachedOutcomes.TryGetValue(submission.Id, out var cached) &&
                        cached.AnswersUndo(bound.Id))
                    {
                        return new SeatUndoResult(
                            submission, cached.Accepted, cached.SeatReason, cached.SessionReason,
                            cached.Detail, cached.Version, cached.Sequence, ProjectionForBound(bound));
                    }

                    return UndoRefused(
                        submission, bound, SeatSubmissionRejectionReason.DuplicateSubmission,
                        "Submission '" + submission.Id + "' has already reached the authority.");
                }

                var projection = ProjectionForBound(bound);
                if (submission.ExpectedVersion != projection.Version)
                {
                    return UndoRefused(
                        submission, bound, SeatSubmissionRejectionReason.StaleProjection,
                        "The submission expects version " + submission.ExpectedVersion +
                        " and the authoritative version is " + projection.Version + ".");
                }

                if (!CanSeatUndo(bound, out var why))
                {
                    return UndoRefused(
                        submission, bound, SeatSubmissionRejectionReason.UndoUnavailable, why);
                }

                ConsumeSubmissionId(submission.Id);
                executing = true;
                MatchHostResult inner;
                // Milestone 8.35r. Captured BEFORE the execute, because an accepted submission
                // retires the marker inside Execute; this is what lets a run of rewinds by one hero
                // count up instead of resetting to one each time.
                var continuingRun = undoRunHeroId.Equals(bound.HeroId) ? undoRunCount : 0;
                try
                {
                    DuringSubmissionForTest?.Invoke();
                    inner = Execute(CanRetractCommitment(session.Projection, bound.HeroId)
                        ? MatchSubmission.ForCommand(DirectorCommand.RetractCommitment(bound.HeroId))
                        : MatchSubmission.ForUndo());
                }
                finally
                {
                    executing = false;
                }

                if (inner.Accepted)
                {
                    undoRunHeroId = bound.HeroId;
                    undoRunCount = continuingRun + 1;
                }

                // The rewind cannot have crossed a turn boundary - the floor is inside this turn -
                // so this only lets the watermark observe the state it landed on.
                RefreshSeatUndoWatermark(default, null, afterRewind: true);

                var outcome = new CachedSubmissionOutcome
                {
                    Seat = bound.Id,
                    IsUndo = true,
                    Accepted = inner.Accepted,
                    SeatReason = inner.Accepted
                        ? SeatSubmissionRejectionReason.None
                        : SeatSubmissionRejectionReason.SessionRefused,
                    AgreementReason = TeamAgreementSubmissionRejectionReason.None,
                    SessionReason = inner.SessionRejectionReason,
                    Detail = inner.Detail,
                    Version = inner.Version,
                    Sequence = inner.Sequence
                };
                CacheOutcome(submission.Id, outcome);

                return new SeatUndoResult(
                    submission, outcome.Accepted, outcome.SeatReason, outcome.SessionReason,
                    outcome.Detail, outcome.Version, outcome.Sequence, ProjectionForBound(bound));
            }
        }

        private SeatUndoResult UndoRefused(
            SeatUndoSubmission submission,
            MatchSeat seat,
            SeatSubmissionRejectionReason reason,
            string detail) =>
            new SeatUndoResult(
                submission, false, reason, MatchSessionRejectionReason.None, detail,
                session.Projection.Version, 0, seat == null ? null : ProjectionForBound(seat));

        private void RecordContinuationRefusal(string detail)
        {
            continuationRefusals.Add(detail);
            if (continuationRefusals.Count > RetainedContinuationRefusalCount)
            {
                continuationRefusals.RemoveRange(
                    0, continuationRefusals.Count - RetainedContinuationRefusalCount);
            }
        }

        private MatchHostResult FromCommand(MatchSubmission submission, MatchCommandResult result)
        {
            if (!result.Accepted)
            {
                return SessionRefused(
                    submission, result.RejectionReason, result.Detail, result.Projection, result, null);
            }

            var events = result.Events == null
                ? Array.Empty<RoundResolutionEvent>()
                : result.Events.ToArray();
            return new MatchHostResult(
                submission,
                true,
                MatchHostRejectionReason.None,
                MatchSessionRejectionReason.None,
                string.Empty,
                result.Projection.Version,
                Append(result.Projection.Version, submission.Kind, submission.Command, events),
                events,
                result.Projection,
                result,
                null);
        }

        private MatchHostResult FromOperation(
            MatchSubmission submission, MatchSessionOperationResult result)
        {
            if (!result.Accepted)
            {
                return SessionRefused(
                    submission, result.RejectionReason, result.Detail, result.Projection, null, result);
            }

            var events = Array.Empty<RoundResolutionEvent>();
            return new MatchHostResult(
                submission,
                true,
                MatchHostRejectionReason.None,
                MatchSessionRejectionReason.None,
                string.Empty,
                result.Projection.Version,
                Append(result.Projection.Version, submission.Kind, null, events),
                events,
                result.Projection,
                null,
                result);
        }

        private long Append(
            long version,
            MatchSubmissionKind kind,
            DirectorCommand command,
            IReadOnlyList<RoundResolutionEvent> events)
        {
            sequence++;
            retained.Add(new MatchHostResultRecord(sequence, version, kind, command, events));
            if (retained.Count > RetainedResultCount)
            {
                retained.RemoveRange(0, retained.Count - RetainedResultCount);
            }

            return sequence;
        }

        private MatchHostResult SessionRefused(
            MatchSubmission submission,
            MatchSessionRejectionReason reason,
            string detail,
            MatchSessionProjection projection,
            MatchCommandResult commandResult,
            MatchSessionOperationResult operationResult) =>
            new MatchHostResult(
                submission,
                false,
                MatchHostRejectionReason.SessionRefused,
                reason,
                detail,
                projection.Version,
                0,
                Array.Empty<RoundResolutionEvent>(),
                projection,
                commandResult,
                operationResult);

        private MatchHostResult HostRefused(
            MatchSubmission submission, MatchHostRejectionReason reason, string detail) =>
            new MatchHostResult(
                submission,
                false,
                reason,
                MatchSessionRejectionReason.None,
                detail,
                session.Projection.Version,
                0,
                Array.Empty<RoundResolutionEvent>(),
                session.Projection,
                null,
                null);
    }
}
