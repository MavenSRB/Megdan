using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;

namespace GuardsDigital.Application
{
    /// <summary>How a single attempted operation ended at the aggregate's commit boundary.</summary>
    internal enum MatchCommitStatus
    {
        /// <summary>State was accepted, and the events it produced were appended with it.</summary>
        Committed,

        /// <summary>The operation refused cleanly and touched nothing.</summary>
        Rejected,

        /// <summary>The operation refused but had already mutated state, which was rolled back.</summary>
        AtomicityViolation,

        /// <summary>The operation threw, and state was rolled back to its pre-image.</summary>
        Faulted
    }

    /// <summary>
    /// What an operation proposes to the aggregate: whether the graphs accepted it, and the ordered
    /// rule events that acceptance produced. The two travel together so the commit boundary can
    /// append the events only when the state that produced them is kept.
    /// </summary>
    internal interface IMatchCommitPlan
    {
        bool Accepted { get; }

        IReadOnlyList<RoundResolutionEvent> Events { get; }
    }

    internal sealed class MatchCommitOutcome<TPlan> where TPlan : IMatchCommitPlan
    {
        private MatchCommitOutcome(MatchCommitStatus status, TPlan plan, Exception fault)
        {
            Status = status;
            Plan = plan;
            Fault = fault;
        }

        public MatchCommitStatus Status { get; }

        /// <summary>The proposal itself. Meaningless when <see cref="Status"/> is Faulted.</summary>
        public TPlan Plan { get; }

        /// <summary>The thrown exception, and only set when <see cref="Status"/> is Faulted.</summary>
        public Exception Fault { get; }

        public static MatchCommitOutcome<TPlan> Committed(TPlan plan)
        {
            return new MatchCommitOutcome<TPlan>(MatchCommitStatus.Committed, plan, null);
        }

        public static MatchCommitOutcome<TPlan> Rejected(TPlan plan)
        {
            return new MatchCommitOutcome<TPlan>(MatchCommitStatus.Rejected, plan, null);
        }

        public static MatchCommitOutcome<TPlan> AtomicityViolation(TPlan plan)
        {
            return new MatchCommitOutcome<TPlan>(MatchCommitStatus.AtomicityViolation, plan, null);
        }

        public static MatchCommitOutcome<TPlan> Faulted(Exception fault)
        {
            return new MatchCommitOutcome<TPlan>(
                MatchCommitStatus.Faulted,
                default,
                fault ?? throw new ArgumentNullException(nameof(fault)));
        }
    }

    /// <summary>
    /// One complete set of authoritative graphs plus the event streams that belong with them. The
    /// four graphs are never replaced individually - every path that installs them installs all
    /// four - so carrying them as one value is what lets <see cref="MatchAggregate"/> have a single
    /// assignment site.
    /// </summary>
    internal sealed class MatchGraphSet
    {
        private readonly IReadOnlyList<RuleEventLogEntry> restoredEventLogEntries;
        private readonly MatchParticipants setupParticipants;
        private readonly TieBreakerState setupTieBreakerState;

        private MatchGraphSet(
            PlaceholderOneVsOneTestConfiguration configuration,
            AdjacentMoveAuthority board,
            CardActionResolutionAuthority cardActions,
            RoundResolutionAuthority round,
            IReadOnlyList<RoundResolutionEvent> roundEvents,
            IReadOnlyList<RuleEventLogEntry> restoredEventLogEntries,
            MatchParticipants setupParticipants,
            TieBreakerState setupTieBreakerState)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            CardActions = cardActions ?? throw new ArgumentNullException(nameof(cardActions));
            Round = round ?? throw new ArgumentNullException(nameof(round));
            RoundEvents = roundEvents ?? throw new ArgumentNullException(nameof(roundEvents));
            this.restoredEventLogEntries = restoredEventLogEntries;
            this.setupParticipants = setupParticipants;
            this.setupTieBreakerState = setupTieBreakerState;
        }

        public PlaceholderOneVsOneTestConfiguration Configuration { get; }
        public AdjacentMoveAuthority Board { get; }
        public CardActionResolutionAuthority CardActions { get; }
        public RoundResolutionAuthority Round { get; }
        public IReadOnlyList<RoundResolutionEvent> RoundEvents { get; }

        /// <summary>Graphs whose rule-event log restarts from the single setup entry.</summary>
        public static MatchGraphSet ForSetup(
            PlaceholderOneVsOneTestConfiguration configuration,
            AdjacentMoveAuthority board,
            CardActionResolutionAuthority cardActions,
            RoundResolutionAuthority round,
            MatchParticipants participants,
            TieBreakerState tieBreakerState,
            IReadOnlyList<RoundResolutionEvent> roundEvents)
        {
            return new MatchGraphSet(
                configuration,
                board,
                cardActions,
                round,
                roundEvents,
                null,
                participants ?? throw new ArgumentNullException(nameof(participants)),
                tieBreakerState);
        }

        /// <summary>Graphs that arrive with an existing ordered rule-event log to adopt.</summary>
        public static MatchGraphSet ForRestore(
            PlaceholderOneVsOneTestConfiguration configuration,
            AdjacentMoveAuthority board,
            CardActionResolutionAuthority cardActions,
            RoundResolutionAuthority round,
            IReadOnlyList<RuleEventLogEntry> eventLogEntries,
            IReadOnlyList<RoundResolutionEvent> roundEvents)
        {
            return new MatchGraphSet(
                configuration,
                board,
                cardActions,
                round,
                roundEvents,
                eventLogEntries ?? throw new ArgumentNullException(nameof(eventLogEntries)),
                null,
                default);
        }

        public void ApplyEventLogTo(RuleEventLogProjection eventLog)
        {
            if (restoredEventLogEntries != null)
            {
                eventLog.Restore(restoredEventLogEntries);
                return;
            }

            eventLog.ResetToSetup(setupParticipants, setupTieBreakerState);
        }
    }

    /// <summary>
    /// The aggregate root over authoritative match state. It owns the configuration, board,
    /// card-action, and round graphs, and the two ordered event streams those graphs produce, and
    /// it exposes exactly one boundary at which an operation's state and its events are kept or
    /// discarded together.
    /// </summary>
    /// <remarks>
    /// Before milestone 6.1f the four graphs were independently assignable fields on
    /// <see cref="MatchSession"/>, and <c>Execute</c> appended a command's events to the two streams
    /// after the fact, with nothing enforcing that the two agreed. Here the streams are private and
    /// published only as read-only views, so <see cref="Commit{TPlan}"/> is the only place in the
    /// assembly where an event can reach them.
    ///
    /// The rollback is a restore from a serialized pre-image rather than a deferred write: the
    /// authorities below this root mutate in place, unlike <c>DefeatRewardAccounting.Award</c>,
    /// whose plan-validate-commit shape this boundary is modelled on. The guarantee to the caller is
    /// the same - a refused operation leaves no partial state - but the mechanism differs, and
    /// converting the authorities to plan-only is deeper work than 6.1f. Milestone 6.1i owns the
    /// pre-image's cost.
    /// </remarks>
    internal sealed class MatchAggregate
    {
        private readonly MatchSessionInitialState initialState;
        private readonly MatchDiagnosticsMode diagnostics;
        private readonly RuleEventLogProjection eventLog = new RuleEventLogProjection();
        private readonly List<RoundResolutionEvent> roundEvents = new List<RoundResolutionEvent>();
        private readonly ReadOnlyCollection<RoundResolutionEvent> publishedRoundEvents;

        private PlaceholderOneVsOneTestConfiguration configuration;
        private AdjacentMoveAuthority board;
        private CardActionResolutionAuthority cardActions;
        private RoundResolutionAuthority round;
        private string memoizedAuthoritative;

        public MatchAggregate(MatchSessionInitialState initialState, MatchDiagnosticsMode diagnostics)
        {
            this.initialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
            this.diagnostics = diagnostics;
            publishedRoundEvents = new ReadOnlyCollection<RoundResolutionEvent>(roundEvents);
        }

        public PlaceholderOneVsOneTestConfiguration Configuration => configuration;
        public AdjacentMoveAuthority Board => board;
        public CardActionResolutionAuthority CardActions => cardActions;
        public RoundResolutionAuthority Round => round;
        public IReadOnlyList<RuleEventLogEntry> EventLogEntries => eventLog.Entries;

        /// <summary>A live read-only view; appending is possible only through the commit boundary.</summary>
        public IReadOnlyList<RoundResolutionEvent> RoundEvents => publishedRoundEvents;

        /// <summary>
        /// The authoritative snapshot of the current graphs, computed once per state change.
        /// </summary>
        /// <remarks>
        /// The memo is what removed most of finding `F613-08`'s cost. A command captured the
        /// pre-image and then, on acceptance, the projection serialized the same graphs again; and
        /// the next command's pre-image serialized them a third time, still unchanged. Only two
        /// events can invalidate this: an accepted commit, and a graph replacement. Both are single
        /// sites here, which is the property milestone 6.1f bought - before it, a fifth field could
        /// be assigned anywhere and the memo would have been unsound.
        /// </remarks>
        public string CaptureAuthoritativeSnapshot()
        {
            return memoizedAuthoritative ?? (memoizedAuthoritative = SerializeAuthoritative());
        }

        /// <summary>
        /// The full snapshot, authoritative state plus the rule-event log. Callers asking for this
        /// pay the envelope encode, which is proportional to the whole accumulated log; nothing on
        /// the per-command path calls it.
        /// </summary>
        public string CaptureSnapshot()
        {
            return RuleEventLogSnapshotEnvelope.Serialize(CaptureAuthoritativeSnapshot(), eventLog.Entries);
        }

        private string SerializeAuthoritative()
        {
            return AuthoritativeSnapshotSerializer.Serialize(round, board);
        }

        /// <summary>Detaches the current graphs and streams so another aggregate can adopt them.</summary>
        public MatchGraphSet Capture()
        {
            return MatchGraphSet.ForRestore(
                configuration,
                board,
                cardActions,
                round,
                eventLog.Entries.ToArray(),
                roundEvents.ToArray());
        }

        public void Adopt(MatchGraphSet graphs)
        {
            ReplaceGraphs(graphs ?? throw new ArgumentNullException(nameof(graphs)));
        }

        public void RebuildInitialState(bool useProvidedConfiguration)
        {
            var rebuiltConfiguration = useProvidedConfiguration && initialState.InitialConfiguration != null
                ? initialState.InitialConfiguration
                : initialState.Scenario.CreateConfiguration();
            var rebuiltBoard = initialState.Scenario.CreateBoard(
                initialState.AvailableMaps,
                initialState.Setup.Minions);
            var rebuiltCardActions = new CardActionResolutionAuthority(rebuiltBoard);
            var rebuiltRound = new RoundResolutionAuthority(
                rebuiltConfiguration.CardDefinitions,
                rebuiltConfiguration.CardInstances,
                rebuiltConfiguration.Participants,
                initialState.Scenario.InitialTieBreakerState,
                cardActions: rebuiltCardActions,
                timing: initialState.CreateTiming(),
                board: rebuiltBoard);

            ReplaceGraphs(MatchGraphSet.ForSetup(
                rebuiltConfiguration,
                rebuiltBoard,
                rebuiltCardActions,
                rebuiltRound,
                rebuiltConfiguration.Participants,
                initialState.Scenario.InitialTieBreakerState,
                Array.Empty<RoundResolutionEvent>()));
        }

        /// <summary>
        /// Installs the graphs a snapshot describes, and throws without changing anything when the
        /// payload cannot be read or does not belong to this setup. A restored snapshot begins a
        /// fresh round position, so it installs no prior round events.
        /// </summary>
        public void Restore(string payload)
        {
            var hasEnvelope = RuleEventLogSnapshotEnvelope.TryDeserialize(payload, out var envelope);
            var authoritativePayload = hasEnvelope ? envelope.AuthoritativePayload : payload;
            var restored = DeserializeAuthoritative(authoritativePayload);

            ReplaceGraphs(hasEnvelope
                ? MatchGraphSet.ForRestore(
                    restored.Configuration,
                    restored.Board,
                    restored.CardActions,
                    restored.Round,
                    envelope.Entries,
                    Array.Empty<RoundResolutionEvent>())
                : MatchGraphSet.ForSetup(
                    restored.Configuration,
                    restored.Board,
                    restored.CardActions,
                    restored.Round,
                    restored.Participants,
                    restored.Round.TieBreakerState,
                    Array.Empty<RoundResolutionEvent>()));
        }

        private RestoredGraphs DeserializeAuthoritative(string authoritativePayload)
        {
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                authoritativePayload,
                initialState.Scenario.CardDefinitions,
                initialState.AvailableMaps,
                initialState.Setup.Minions);

            if (!restored.Board.SelectedMap.Equals(initialState.Setup.SelectedMap))
            {
                throw new InvalidOperationException("The restored snapshot does not match the configured setup map.");
            }

            return new RestoredGraphs(
                PlaceholderOneVsOneTestConfiguration.Restore(
                    new PlaceholderConfigurationRestoreState(
                        restored.Participants,
                        initialState.Scenario.CardDefinitions,
                        restored.Cards,
                        restored.Round.TieBreakerState)),
                restored);
        }

        /// <summary>
        /// The one commit boundary. Runs <paramref name="plan"/> against the graphs, and keeps its
        /// state and its ordered events together or keeps neither.
        /// </summary>
        public MatchCommitOutcome<TPlan> Commit<TPlan>(Func<TPlan> plan) where TPlan : IMatchCommitPlan
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var preImage = CapturePreImage();
            TPlan planned;
            try
            {
                planned = plan();
            }
            catch (Exception exception)
            {
                Rollback(preImage);
                return MatchCommitOutcome<TPlan>.Faulted(exception);
            }

            if (!planned.Accepted)
            {
                // Debug-grade self-verification: prove the refusal moved nothing. It must serialize
                // afresh rather than read the memo, because a mutation is exactly what it is looking
                // for and the memo would still hold the pre-command value. Only the rule-event
                // streams are excluded from the comparison, and only because they are private to
                // this aggregate and reachable solely from the commit below.
                if (diagnostics == MatchDiagnosticsMode.Verified &&
                    !string.Equals(preImage.AuthoritativePayload, SerializeAuthoritative(), StringComparison.Ordinal))
                {
                    Rollback(preImage);
                    return MatchCommitOutcome<TPlan>.AtomicityViolation(planned);
                }

                return MatchCommitOutcome<TPlan>.Rejected(planned);
            }

            // The commit. The graphs kept this operation's state, so the events that state produced
            // are appended here, and this is the only place they can be: both streams are private.
            roundEvents.AddRange(planned.Events);
            eventLog.Append(planned.Events);
            memoizedAuthoritative = null;
            return MatchCommitOutcome<TPlan>.Committed(planned);
        }

        /// <summary>The pre-image a refused or faulted operation is rolled back to.</summary>
        /// <remarks>
        /// The two event streams travel as in-memory arrays rather than inside a serialized
        /// envelope. The envelope re-encodes the whole accumulated log, so carrying one here made
        /// per-command cost grow with session length, which is the second half of `F613-08`. Copying
        /// two arrays of references carries the same state at a cost that does not grow.
        /// </remarks>
        public MatchPreImage CapturePreImage()
        {
            return new MatchPreImage(
                CaptureAuthoritativeSnapshot(),
                eventLog.Entries.ToArray(),
                roundEvents.ToArray());
        }

        public void Rollback(MatchPreImage preImage)
        {
            if (preImage == null) throw new ArgumentNullException(nameof(preImage));
            var restored = DeserializeAuthoritative(preImage.AuthoritativePayload);
            ReplaceGraphs(MatchGraphSet.ForRestore(
                restored.Configuration,
                restored.Board,
                restored.CardActions,
                restored.Round,
                preImage.EventLogEntries,
                preImage.RoundEvents));
        }

        /// <summary>
        /// The single site at which authoritative graphs are assigned. Every path that installs
        /// state - initial build, snapshot restore, replay adoption, rollback - arrives here, so
        /// the four graphs and their two streams cannot drift out of step with one another.
        /// </summary>
        private void ReplaceGraphs(MatchGraphSet graphs)
        {
            configuration = graphs.Configuration;
            board = graphs.Board;
            cardActions = graphs.CardActions;
            round = graphs.Round;
            roundEvents.Clear();
            roundEvents.AddRange(graphs.RoundEvents);
            graphs.ApplyEventLogTo(eventLog);
            memoizedAuthoritative = null;
        }

        /// <summary>A deserialized snapshot's graphs, with the configuration rebuilt from it.</summary>
        private sealed class RestoredGraphs
        {
            public RestoredGraphs(
                PlaceholderOneVsOneTestConfiguration configuration,
                RestoredAuthoritativeMatch restored)
            {
                Configuration = configuration;
                Board = restored.Board;
                CardActions = restored.CardActions;
                Round = restored.Round;
                Participants = restored.Participants;
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
            public AdjacentMoveAuthority Board { get; }
            public CardActionResolutionAuthority CardActions { get; }
            public RoundResolutionAuthority Round { get; }
            public MatchParticipants Participants { get; }
        }
    }

    internal sealed class MatchPreImage
    {
        public MatchPreImage(
            string authoritativePayload,
            IReadOnlyList<RuleEventLogEntry> eventLogEntries,
            IReadOnlyList<RoundResolutionEvent> roundEvents)
        {
            AuthoritativePayload = authoritativePayload ?? throw new ArgumentNullException(nameof(authoritativePayload));
            EventLogEntries = eventLogEntries ?? throw new ArgumentNullException(nameof(eventLogEntries));
            RoundEvents = roundEvents ?? throw new ArgumentNullException(nameof(roundEvents));
        }

        public string AuthoritativePayload { get; }
        public IReadOnlyList<RuleEventLogEntry> EventLogEntries { get; }
        public IReadOnlyList<RoundResolutionEvent> RoundEvents { get; }
    }
}
