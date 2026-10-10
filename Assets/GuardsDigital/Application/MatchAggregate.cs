using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Transitions;
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
            MatchConfiguration configuration,
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

        public MatchConfiguration Configuration { get; }
        public AdjacentMoveAuthority Board { get; }
        public CardActionResolutionAuthority CardActions { get; }
        public RoundResolutionAuthority Round { get; }
        public IReadOnlyList<RoundResolutionEvent> RoundEvents { get; }

        /// <summary>Graphs whose rule-event log restarts from the single setup entry.</summary>
        public static MatchGraphSet ForSetup(
            MatchConfiguration configuration,
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
            MatchConfiguration configuration,
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
    /// authorities below this root mutate in place, unlike <c>HeroCoinLedger.Award</c>,
    /// whose plan-validate-commit shape this boundary is modelled on. The guarantee to the caller is
    /// the same - a refused operation leaves no partial state - but the mechanism differs, and
    /// converting the authorities to plan-only is deeper work than 6.1f; `RISK-STATE-002` records
    /// what that difference still exposes.
    ///
    /// Because the mechanism is a rollback, the comparison that decides whether to roll back is part
    /// of the guarantee and runs unconditionally. Milestone 6.1i briefly made it optional, which
    /// left the shipping configuration paying for a pre-image it could never act on.
    /// </remarks>
    internal sealed class MatchAggregate
    {
        private readonly MatchSessionInitialState initialState;
        private readonly RuleEventLogProjection eventLog = new RuleEventLogProjection();
        private readonly AppendOnlyEventLog<RoundResolutionEvent> roundEvents =
            new AppendOnlyEventLog<RoundResolutionEvent>();

        private MatchConfiguration configuration;
        private AdjacentMoveAuthority board;
        private CardActionResolutionAuthority cardActions;
        private RoundResolutionAuthority round;
        private AuthoritativeSnapshotCapture memoizedAuthoritative;

        /// <summary>
        /// Armed only by <see cref="CorruptNextPreImagePayloadForTest"/>, and cleared by the capture
        /// it corrupts. Null in every shipped path.
        /// </summary>
        private string nextPreImagePayloadCorruptionForTest;

        /// <summary>
        /// The completed-round history's serialized bytes, written once per round instead of once
        /// per command. A derived cache: it validates itself against the round authority's own
        /// records on every capture and rebuilds on any divergence, so a restore, a replay and a
        /// rollback can each ignore it and stay correct.
        /// </summary>
        private readonly CompletedRoundsSerializationCache completedRoundsCache =
            new CompletedRoundsSerializationCache();

        public MatchAggregate(MatchSessionInitialState initialState)
        {
            this.initialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        }

        public MatchConfiguration Configuration => configuration;
        public AdjacentMoveAuthority Board => board;
        public CardActionResolutionAuthority CardActions => cardActions;
        public RoundResolutionAuthority Round => round;
        public IReadOnlyList<RuleEventLogEntry> EventLogEntries => eventLog.Entries;

        /// <summary>
        /// The current round's ordered events as an immutable value; appending is possible only
        /// through the commit boundary.
        /// </summary>
        public IReadOnlyList<RoundResolutionEvent> RoundEvents => roundEvents.Entries;

        /// <summary>
        /// The authoritative snapshot of the current graphs, computed once per state change.
        /// </summary>
        public string CaptureAuthoritativeSnapshot()
        {
            return CaptureAuthoritative().Payload;
        }

        /// <summary>
        /// The authoritative serialization of the current graphs as a capture, taken once per
        /// state change.
        /// </summary>
        /// <remarks>
        /// The memo is what removed most of finding `F613-08`'s cost. A command captured the
        /// pre-image and then, on acceptance, the projection serialized the same graphs again; and
        /// the next command's pre-image serialized them a third time, still unchanged. Only two
        /// events can invalidate this: an accepted commit, and a graph replacement. Both are single
        /// sites here, which is the property milestone 6.1f bought - before it, a fifth field could
        /// be assigned anywhere and the memo would have been unsound.
        ///
        /// Since milestone 7.9b what is memoized is the capture rather than the encoded payload:
        /// the accepted-command path needs only the capture, and the string is assembled by
        /// whichever reader first asks for it.
        /// </remarks>
        internal AuthoritativeSnapshotCapture CaptureAuthoritative()
        {
            return memoizedAuthoritative ?? (memoizedAuthoritative = CaptureAuthoritativeFresh());
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

        private AuthoritativeSnapshotCapture CaptureAuthoritativeFresh()
        {
            return AuthoritativeSnapshotSerializer.Capture(round, board, completedRoundsCache);
        }

        /// <summary>Detaches the current graphs and streams so another aggregate can adopt them.</summary>
        /// <remarks>
        /// Milestone 8.35z43 also made this THIS aggregate's rollback for a failed
        /// <see cref="Restore"/>, paired with <see cref="Adopt"/>. It is the right one there and the
        /// wrong one for a command: a restore REPLACES the graphs, so the ones it displaces are still
        /// intact and putting them back is an assignment; a command mutates them in place, where only
        /// <see cref="CapturePreImage"/>'s serialized description survives. The difference matters
        /// because an assignment cannot fail and a re-deserialization can - and a rollback that throws
        /// on the failure path is what leaves a session damaged.
        /// </remarks>
        public MatchGraphSet Capture()
        {
            return MatchGraphSet.ForRestore(
                configuration,
                board,
                cardActions,
                round,
                eventLog.Entries,
                roundEvents.Entries);
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
            // One register, shared by the dispatcher that gives and returns markers and the round
            // authority whose snapshot section persists them.
            var rebuiltMarkers = new MarkerAttachmentRegister();
            // Milestones 6.10a and 6.11. Rebuilt from the trusted catalog rather than carried across, so
            // a restart and a retained-prefix Undo begin with a full Wave pool, no push running and no
            // transition paused, and re-derive any paused one by replaying the commands that paused it.
            var rebuiltLanePushAuthority = new LanePushTransitionAuthority(
                rebuiltBoard, initialState.Setup.WaveCount);
            // Milestone 7.5. p.15's leveling is the second production consumer of the continuation
            // contract, and its rules are rebuilt here beside the Lane Push's for the same reason: they
            // are about this match's own participants, content and cards. The states a purchase moves
            // are bound by the round authority below, which is what owns them.
            var rebuiltLevelUp = new GuardsDigital.Core.Rules.Progression.HeroLevelUpStepRules(
                rebuiltConfiguration.Participants,
                rebuiltConfiguration.CardDefinitions,
                rebuiltConfiguration.CardInstances);
            var rebuiltTransitions = new SystemTransitionAuthority(
                MatchSystemTransitions.CreateCatalog(initialState.ModeRules),
                MatchSystemTransitions.CreateRules(
                    rebuiltLanePushAuthority,
                    rebuiltLevelUp,
                    initialState.ModeRules,
                    rebuiltBoard,
                    rebuiltConfiguration.Participants));
            var rebuiltLanePush = new LanePushConsumption(rebuiltLanePushAuthority, rebuiltTransitions);
            var rebuiltCardActions = new CardActionResolutionAuthority(
                rebuiltConfiguration.ContentCatalogs, rebuiltBoard, rebuiltMarkers);
            rebuiltCardActions.BindHeroDefeatBaseLifeCost(
                initialState.Experiments.HeroDefeatBaseLifeCostIsOne);
            var rebuiltRound = new RoundResolutionAuthority(
                rebuiltConfiguration.CardDefinitions,
                rebuiltConfiguration.CardInstances,
                rebuiltConfiguration.Participants,
                initialState.Scenario.InitialTieBreakerState,
                1,
                RoundResolutionComposition.ForMatch(
                    rebuiltCardActions,
                    rebuiltBoard,
                    initialState.CreateTiming(),
                    rebuiltMarkers,
                    rebuiltTransitions,
                    rebuiltLanePush,
                    rebuiltLevelUp,
                    // Milestone 7.4. Rebuilt from the composed setup rather than carried across, for the
                    // reason the Wave pool above it is: a restart and a retained-prefix Undo begin with
                    // the counters the setup states, and re-derive every spend by replaying the Defeats
                    // that spent them. The setup's per-team `LifeCount` is the one record of the number.
                    new GuardsDigital.Core.Rules.Consequences.TeamLifeState(
                        rebuiltConfiguration.Participants,
                        initialState.Setup.Teams.Select(team =>
                            new System.Collections.Generic.KeyValuePair<TeamId, int>(
                                team.TeamId, team.LifeCount))),
                    // Milestone 8.35z17. The staged Levels and balances travel beside the Life
                    // counters above them because they are the same kind of fact: a starting position
                    // the composed setup records, rebuilt from that record on every restart and every
                    // retained-prefix Undo rather than carried across. That is what makes a staged
                    // situation replay to itself instead of decaying to a dealt one.
                    initialState.StagedProgression.CreateProgressionState(),
                    initialState.StagedProgression.CreateCoinLedgerState(),
                    initialState.Experiments.MinionBattleRemovalCapEnabled
                        ? initialState.Experiments.MinionBattleRemovalCap
                        : (int?)null));
            // W-022. Bound on the fresh graph exactly as on every restored one (below).
            rebuiltRound.BindModeRules(initialState.ModeRules);

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
            restored.CardActions.BindHeroDefeatBaseLifeCost(
                initialState.Experiments.HeroDefeatBaseLifeCostIsOne);

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
                // Milestone 8.3. The configured session states the content its restore rebinds
                // against, which is what the serializer used to guess with a synthetic default under
                // a comment claiming this site already supplied it.
                configuration.ContentCatalogs,
                authoritativePayload,
                initialState.Scenario.CardDefinitions,
                initialState.AvailableMaps,
                initialState.Setup.Minions,
                minionBattleRemovalCap: initialState.Experiments.MinionBattleRemovalCapEnabled
                    ? initialState.Experiments.MinionBattleRemovalCap
                    : (int?)null,
                modeRules: initialState.ModeRules);

            // W-022. Bound here rather than by each caller, so a snapshot restore, a rollback and a
            // checkpoint Undo all rebind the same mode rules; existing matches bind null.
            restored.Round.BindModeRules(initialState.ModeRules);

            if (!restored.Board.SelectedMap.Equals(initialState.Setup.SelectedMap))
            {
                throw new InvalidOperationException("The restored snapshot does not match the configured setup map.");
            }

            return new RestoredGraphs(
                MatchConfiguration.Restore(
                    new MatchConfigurationRestoreState(
                        restored.Participants,
                        initialState.Scenario.CardDefinitions,
                        restored.Cards,
                        restored.Round.TieBreakerState,
                        // Milestone 8.28b. The side comes from the payload because it flips during
                        // play; the draw comes from the recorded scenario because it happened once, at
                        // setup, and no later state can restate it. A restore that dropped it would
                        // leave a live match unable to say how it started, which is the gap F-ER2-04
                        // recorded.
                        initialState.Scenario.SetupSeed)),
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
                // Prove the refusal moved nothing, and undo it when it did. This runs in every build
                // configuration because it is not a diagnostic: it is the mechanism by which a
                // refused command leaves no partial state. Milestone 6.1i gated it behind a
                // diagnostics mode on the reading that it was debug-grade self-verification, which
                // left the shipping configuration capturing a pre-image it could never use.
                //
                // It must serialize afresh rather than read the memo, because a mutation is exactly
                // what it is looking for and the memo would still hold the pre-command value. Only
                // the rule-event streams are excluded from the comparison, and only because they are
                // private to this aggregate and reachable solely from the commit below. The captures
                // compare the same serialized content the payload strings compared; neither side
                // encodes a string to answer.
                if (!preImage.Authoritative.ContentEquals(CaptureAuthoritativeFresh()))
                {
                    Rollback(preImage);
                    return MatchCommitOutcome<TPlan>.AtomicityViolation(planned);
                }

                return MatchCommitOutcome<TPlan>.Rejected(planned);
            }

            // The commit. The graphs kept this operation's state, so the events that state produced
            // are appended here, and this is the only place they can be: both streams are private.
            //
            // Milestone 7.8. The round-scoped stream is emptied when a round boundary crosses it, which
            // is what makes it the *current* round's events rather than a second whole-match log under
            // a name that says otherwise. It happens inside this loop rather than around it because a
            // single accepted command routinely carries a round's last events and the next round's
            // first: the advance that runs p.8's last step reports that step, freezes the completed
            // round and opens the new one, all in one batch. The whole-match log beside it appends
            // unconditionally, and the completed-round history keeps the frozen record, so nothing the
            // round produced is lost by this - it stops being *current*, which is the only claim this
            // stream makes. `Reset` leaves every outstanding snapshot of the old contents readable, so
            // the pre-image a refused command rolls back to is unaffected.
            foreach (var ruleEvent in planned.Events)
            {
                if (ruleEvent.BeginsCurrentRoundEvents)
                {
                    roundEvents.Reset();
                }

                roundEvents.Add(ruleEvent);

                // Milestone 8.53. The victory screen's per-hero board is counted here, in the same
                // pass that files the event and immediately before the append that writes the
                // rule-event log the seat-public rows are derived from. That is what makes the
                // scoreboard and the log two readings of ONE set of facts rather than two readings
                // of the match: a fact this counts is a fact that log carries, by construction.
                //
                // It is inside the accepted branch and nowhere else, which is the whole of what
                // makes it safe. A refused or faulted command never reaches this line, and the
                // rollback above restores the graphs - the scoreboard among them - from the
                // pre-image, so a command that moved a counter and then failed cannot leave the
                // counter moved. And because it counts only committed events, a retained-prefix
                // Undo that replays its prefix into a fresh aggregate re-counts exactly the same
                // facts in exactly the same order.
                round?.Scoreboard.Observe(ruleEvent);
            }

            eventLog.Append(planned.Events);
            memoizedAuthoritative = null;
            return MatchCommitOutcome<TPlan>.Committed(planned);
        }

        /// <summary>The pre-image a refused or faulted operation is rolled back to.</summary>
        /// <remarks>
        /// Every command captures one, so nothing here may cost more as the session lengthens -
        /// the second half of finding `F613-08`. Milestone 6.1i removed the expensive form, an
        /// encode of the whole accumulated log, but replaced it with an array copy of the same log
        /// and measured the result in encoded characters, which cannot see an array copy. Both
        /// streams are now append-only logs whose snapshots are the buffer and a length, so this
        /// captures two references and copies nothing.
        /// </remarks>
        public MatchPreImage CapturePreImage()
        {
            var corruption = nextPreImagePayloadCorruptionForTest;
            nextPreImagePayloadCorruptionForTest = null;
            return new MatchPreImage(
                CaptureAuthoritative(),
                eventLog.Entries,
                roundEvents.Entries,
                corruption);
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
        /// One-shot friend-assembly seam that makes the NEXT <see cref="CapturePreImage"/> report an
        /// unreadable payload, so a fixture can state what happens when the state a rollback would
        /// re-deserialize cannot be read back.
        /// </summary>
        /// <remarks>
        /// It exists because milestone 8.35z23 fixed all three causes that could produce such a state
        /// before recording that the rollback could not survive one, which leaves the hardening with a
        /// property and no way to reach it. The seam is deliberately internal, instance-scoped and
        /// one-shot, and it corrupts only the reported PAYLOAD: no runtime mode or public construction
        /// path can reach it, and nothing about the graphs themselves changes.
        /// </remarks>
        internal void CorruptNextPreImagePayloadForTest(string payload)
        {
            nextPreImagePayloadCorruptionForTest =
                payload ?? throw new ArgumentNullException(nameof(payload));
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
            roundEvents.Reset(graphs.RoundEvents);
            graphs.ApplyEventLogTo(eventLog);
            memoizedAuthoritative = null;
        }

        /// <summary>A deserialized snapshot's graphs, with the configuration rebuilt from it.</summary>
        private sealed class RestoredGraphs
        {
            public RestoredGraphs(
                MatchConfiguration configuration,
                RestoredAuthoritativeMatch restored)
            {
                Configuration = configuration;
                Board = restored.Board;
                CardActions = restored.CardActions;
                Round = restored.Round;
                Participants = restored.Participants;
            }

            public MatchConfiguration Configuration { get; }
            public AdjacentMoveAuthority Board { get; }
            public CardActionResolutionAuthority CardActions { get; }
            public RoundResolutionAuthority Round { get; }
            public MatchParticipants Participants { get; }
        }
    }

    internal sealed class MatchPreImage
    {
        private readonly string corruptedPayloadForTest;

        public MatchPreImage(
            AuthoritativeSnapshotCapture authoritative,
            IReadOnlyList<RuleEventLogEntry> eventLogEntries,
            IReadOnlyList<RoundResolutionEvent> roundEvents,
            string corruptedPayloadForTest = null)
        {
            Authoritative = authoritative ?? throw new ArgumentNullException(nameof(authoritative));
            EventLogEntries = eventLogEntries ?? throw new ArgumentNullException(nameof(eventLogEntries));
            RoundEvents = roundEvents ?? throw new ArgumentNullException(nameof(roundEvents));
            this.corruptedPayloadForTest = corruptedPayloadForTest;
        }

        public AuthoritativeSnapshotCapture Authoritative { get; }

        /// <summary>The capture's encoded payload, materialized only when a rollback needs it.</summary>
        /// <remarks>
        /// The override is <see cref="MatchAggregate.CorruptNextPreImagePayloadForTest"/>'s, and it is
        /// null everywhere else. Only the PAYLOAD is replaceable: <see cref="Authoritative"/> stays the
        /// true capture, so the refusal comparison in <see cref="MatchAggregate.Commit{TPlan}"/> reads
        /// the same state it always did and cannot be steered by this seam.
        /// </remarks>
        public string AuthoritativePayload => corruptedPayloadForTest ?? Authoritative.Payload;

        public IReadOnlyList<RuleEventLogEntry> EventLogEntries { get; }
        public IReadOnlyList<RoundResolutionEvent> RoundEvents { get; }
    }
}
