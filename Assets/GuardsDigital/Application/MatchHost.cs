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
    public sealed class MatchHost : IMatchHost
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
        private MatchParticipants seatedParticipants;
        private MatchSeatBinding seatBinding;
        private bool executing;
        private long sequence;

        public MatchHost(IMatchSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
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
                try
                {
                    DuringSubmissionForTest?.Invoke();
                    return Execute(submission);
                }
                finally
                {
                    executing = false;
                }
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

        public string CaptureSnapshot() => session.CaptureSnapshot();

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
                }
                finally
                {
                    executing = false;
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
                    Version = inner.Version,
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

                    if (applied.FinalCommand == null)
                        return CachedAgreementOutcome(
                            submission, bound, true,
                            TeamAgreementSubmissionRejectionReason.None, string.Empty,
                            session.Projection.Version, 0);

                    var final = Execute(MatchSubmission.ForCommand(applied.FinalCommand));
                    if (!final.Accepted)
                        return CachedAgreementOutcome(
                            submission, bound, false,
                            TeamAgreementSubmissionRejectionReason.FinalRulesChoiceRefused,
                            final.Detail, session.Projection.Version, 0);

                    teamAgreement.Clear();
                    return CachedAgreementOutcome(
                        submission, bound, true,
                        TeamAgreementSubmissionRejectionReason.None, string.Empty,
                        final.Version, final.Sequence);
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

        private SeatProjection ProjectionForBound(MatchSeat seat)
        {
            var source = session.Projection;
            var agreement = teamAgreement.ProjectionFor(source, CurrentSeats(), seat);
            return SeatProjectionFactory.ForSeat(source, seat, agreement);
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

        private MatchHostResult Execute(MatchSubmission submission)
        {
            switch (submission.Kind)
            {
                case MatchSubmissionKind.Command:
                    return FromCommand(submission, session.Execute(submission.Command));
                case MatchSubmissionKind.Undo:
                    return FromOperation(submission, session.Undo());
                case MatchSubmissionKind.Restart:
                    return FromOperation(submission, session.Restart());
                case MatchSubmissionKind.RestoreSnapshot:
                    return FromOperation(submission, session.RestoreSnapshot(submission.SnapshotPayload));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(submission),
                        submission.Kind,
                        "A submission kind the host cannot execute reached it. Every member of " +
                        nameof(MatchSubmissionKind) + " that mutates authority has a case here.");
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
