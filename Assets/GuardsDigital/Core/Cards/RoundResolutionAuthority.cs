using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Cards
{
    public enum RoundResolutionPhase
    {
        AwaitingCommitments = 0,
        ResolvingTurn = 1,
        EndOfRound = 2,
        GameEnded = 3
    }

    public sealed class RoundResolutionAuthority
    {
        public const int TurnsPerRound = 4;

        private readonly IReadOnlyList<CardDefinition> definitions;
        private readonly IReadOnlyList<CardInstance> instances;
        private readonly MatchParticipants participants;
        private readonly CardActionResolutionAuthority cardActions;
        private readonly List<RoundTurnSlot> turnSlots = new List<RoundTurnSlot>();
        private readonly List<CompletedRoundRecord> completedRounds = new List<CompletedRoundRecord>();
        private readonly List<EndOfRoundStageOutcome> endOfRoundOutcomes =
            new List<EndOfRoundStageOutcome>();
        private readonly EndOfRoundFixtureCapabilities endOfRoundCapabilities;
        private readonly IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> endOfRoundHandlers;
        private readonly AdjacentMoveAuthority board;
        private TieBreakerState tieBreakerState;
        private readonly ActiveEffectLifecycle timing;
        private readonly MarkerAttachmentRegister markers;
        private readonly GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority systemTransitions;
        private readonly GuardsDigital.Core.Rules.LanePush.LanePushConsumption lanePush;

        public RoundResolutionAuthority(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            TieBreakerState initialTieBreakerState,
            int roundIndex,
            RoundResolutionComposition composition)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            var cardActions = composition.CardActions;
            var board = composition.Board;
            var timing = composition.Timing;
            var markers = composition.Markers;
            var compositionTransitions = composition.SystemTransitions;
            var compositionLanePush = composition.LanePush;
            var endOfRoundCapabilities = composition.EndOfRoundCapabilities;
            var endOfRoundHandlers = composition.EndOfRoundHandlers;

            this.definitions = Array.AsReadOnly(
                ActionOfferQuery.BuildDefinitionLookup(definitions).Values.ToArray());
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var cards = instances.ToArray();
            if (cards.Any(card => card == null) ||
                cards.Select(card => card.Id).Distinct().Count() != cards.Length)
            {
                throw new ArgumentException(
                    "Round card instances must be non-null and have unique IDs.",
                    nameof(instances));
            }

            this.instances = Array.AsReadOnly(cards);
            this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
            foreach (var card in cards)
            {
                if (!this.definitions.Any(definition => definition.Reference.Equals(card.DefinitionReference)))
                {
                    throw new ArgumentException(
                        "Every round card must reference a current definition.",
                        nameof(instances));
                }

                participants.GetHero(card.OwnerId);
            }

            if (!initialTieBreakerState.IsDefined)
            {
                throw new ArgumentException("Round authority requires Tie Breaker state.", nameof(initialTieBreakerState));
            }

            if (roundIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundIndex));
            }

            tieBreakerState = initialTieBreakerState;
            this.cardActions = cardActions;
            this.board = board;
            this.timing = timing ?? new ActiveEffectLifecycle();
            this.markers = markers ?? new MarkerAttachmentRegister();

            // An absent transition authority is substituted with one over the empty catalog rather than
            // left null, so every caller can ask where a transition is stopped and get the honest "no
            // transition exists in this build" instead of a reference check.
            this.systemTransitions = compositionTransitions ??
                new GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority(
                    GuardsDigital.Core.Rules.Transitions.SystemTransitionCatalog.Empty);

            // Milestone 6.11. Absent outside a configured match, where there is no Lane to push and no
            // Wave counters to spend; the two consumption checkpoints say so rather than substituting a
            // push nothing could win.
            lanePush = compositionLanePush;
            this.endOfRoundCapabilities = endOfRoundCapabilities ??
                (board != null && GuardsDigital.Core.Rules.Modifications.BattleZoneQuery
                        .TryGetCurrentBattleZone(board, out _)
                    ? EndOfRoundFixtureCapabilities.ConfiguredMatch()
                    : EndOfRoundFixtureCapabilities.SyntheticRepeatedRound());
            var handlers = endOfRoundHandlers != null
                ? new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>(endOfRoundHandlers)
                : new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>
                {
                    {
                        EndOfRoundStage.RetrieveCardsAndCancelActiveEffects,
                        new SyntheticCardRetrievalHandler()
                    }
                };
            if (handlers.Any(pair =>
                    pair.Key == EndOfRoundStage.Complete ||
                    pair.Value == null))
            {
                throw new ArgumentException(
                    "End-of-round handlers require executable stages and non-null handlers.",
                    nameof(endOfRoundHandlers));
            }

            this.endOfRoundHandlers = new System.Collections.ObjectModel.ReadOnlyDictionary<
                EndOfRoundStage,
                IEndOfRoundStageHandler>(handlers);
            RoundIndex = roundIndex;
            TurnIndex = 1;
            Phase = RoundResolutionPhase.AwaitingCommitments;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(this.instances, this.participants);
        }

        public int RoundIndex { get; private set; }

        public int TurnIndex { get; private set; }

        public RoundResolutionPhase Phase { get; private set; }

        public TieBreakerState TieBreakerState =>
            CurrentTurnAuthority?.TieBreakerState ?? tieBreakerState;

        public IReadOnlyList<RoundTurnSlot> TurnSlots => turnSlots.AsReadOnly();

        public IReadOnlyList<CompletedRoundRecord> CompletedRounds => completedRounds.AsReadOnly();

        public EndOfRoundStage? CurrentEndOfRoundStage { get; private set; }

        public IReadOnlyList<EndOfRoundStageOutcome> EndOfRoundOutcomes =>
            endOfRoundOutcomes.AsReadOnly();

        public PendingMinionBattleChoice PendingMinionBattleChoice { get; private set; }

        public PendingLanePushRequest PendingLanePushRequest { get; private set; }

        /// <summary>Milestone 6.4a's paused after-action correction, or <c>null</c>.</summary>
        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection => CardActions?.PendingDisplacedMinionCorrection;

        public TurnCardCommitmentAuthority CurrentCommitmentAuthority { get; private set; }

        public TurnResolutionAuthority CurrentTurnAuthority { get; private set; }

        internal IReadOnlyList<CardDefinition> Definitions => definitions;

        internal IReadOnlyList<CardInstance> Instances => instances;

        internal MatchParticipants Participants => participants;

        internal CardActionResolutionAuthority CardActions => cardActions;

        public ActiveEffectLifecycle Timing => timing;

        /// <summary>Milestone 6.2's marker attachments, held beside <see cref="Timing"/>.</summary>
        public MarkerAttachmentRegister Markers => markers;

        /// <summary>Milestone 6.10a's system-transition continuation, held beside <see cref="Markers"/>.</summary>
        public GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority SystemTransitions =>
            systemTransitions;

        /// <summary>
        /// Milestone 6.11's Lane-Push transition, or <c>null</c> in a composition with no Lane to push.
        /// </summary>
        public GuardsDigital.Core.Rules.LanePush.LanePushConsumption LanePush => lanePush;

        /// <summary>
        /// Begins a declared multi-step system transition (milestone 6.10a).
        ///
        /// The instance identity is composed from the round cursor and the definition rather than minted
        /// from a counter, so a replay of the same command prefix produces the same identity: a counter
        /// would rebuild an equal match under different names and break the byte-identity the aggregate
        /// invariance fixture pins.
        ///
        /// This exists because a bounded synthetic transition has no rules of its own to be begun by.
        /// Milestone 6.11's Lane-Push respawn and milestone 7.5's progression begin theirs from their
        /// own rules and do not reach this path.
        /// </summary>
        public RoundResolutionResult TryBeginSystemTransition(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionDefinitionId definitionId)
        {
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            var instanceId = new GuardsDigital.Core.Rules.Transitions.SystemTransitionInstanceId(
                $"round-{RoundIndex}-turn-{TurnIndex}-{definitionId.Value}");
            var result = systemTransitions.TryBegin(definitionId, instanceId);
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(TranslateTransitionRejection(result.RejectionReason));
            }

            return RoundResolutionResult.Accept(new[]
            {
                RoundResolutionEvent.SystemTransitionProgressed(RoundIndex, TurnIndex, result)
            });
        }

        /// <summary>
        /// Answers the paused step of the running system transition (milestone 6.10a), and closes the
        /// Lane Push behind it when that answer was the last one it was waiting for (milestone 6.11).
        /// </summary>
        public RoundResolutionResult TryAnswerSystemTransitionStep(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionChoiceCommand command)
        {
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            var wasLanePush = lanePush != null && lanePush.IsPaused;
            var result = systemTransitions.TryAnswer(command);
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(TranslateTransitionRejection(result.RejectionReason));
            }

            var events = new List<RoundResolutionEvent>
            {
                RoundResolutionEvent.SystemTransitionProgressed(RoundIndex, TurnIndex, result)
            };
            if (!wasLanePush || !result.TransitionCompleted)
            {
                return RoundResolutionResult.Accept(events);
            }

            return CloseFinishedLanePush(lanePush.ReportAfterAnswer(), events);
        }

        /// <summary>
        /// p.8's end-of-round push, at the stage milestone 6.1 built and left refusing.
        ///
        /// A push that stops on the sidebar's Place leaves the stage where it is, so the round waits for
        /// the answer rather than advancing past a transition that has not finished. A second advance
        /// while one is stopped is refused for the same reason.
        /// </summary>
        private RoundResolutionResult ResolveRequestedLanePush(EndOfRoundStage stage)
        {
            if (lanePush == null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.UnavailableLanePushTransition);
            }

            if (lanePush.IsRunning)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IncompleteLanePushTransition);
            }

            var request = PendingLanePushRequest;
            var consumption = lanePush.TryConsume(
                request.RequestId, request.WinningSide, request.LosingSide, TieBreakerState);
            if (!consumption.Accepted)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.RefusedLanePushTransition);
            }

            var events = new List<RoundResolutionEvent>();
            if (consumption.IsPaused)
            {
                events.Add(RoundResolutionEvent.SystemTransitionPaused(
                    RoundIndex, TurnIndex, consumption.PendingStep));
                return CompleteEndOfRoundStage(stage, EndOfRoundStageOutcomeKind.Paused, events);
            }

            return CloseFinishedLanePush(consumption, events);
        }

        /// <summary>
        /// Everything both checkpoints do once a push has finished: forget the request it consumed,
        /// report p.8's stages, and end the game if one of the page's two victory sentences did.
        /// </summary>
        private RoundResolutionResult CloseFinishedLanePush(
            GuardsDigital.Core.Rules.LanePush.LanePushConsumptionResult consumption,
            List<RoundResolutionEvent> events)
        {
            if (consumption.IsPaused)
            {
                events.Add(RoundResolutionEvent.SystemTransitionPaused(
                    RoundIndex, TurnIndex, consumption.PendingStep));
                return RoundResolutionResult.Accept(events);
            }

            var finished = consumption.Finished;
            events.AddRange(finished.Outcomes.Select(outcome =>
                RoundResolutionEvent.LanePushStageResolved(RoundIndex, TurnIndex, outcome)));

            var endOfRoundRequest = PendingLanePushRequest;
            PendingLanePushRequest = null;
            cardActions?.ConsumeImmediateLanePush();

            if (finished.EndedTheGame)
            {
                Phase = RoundResolutionPhase.GameEnded;
                events.Add(RoundResolutionEvent.EndOfRoundBoundary(
                    RoundResolutionEventKind.GameEnded,
                    RoundIndex,
                    EndOfRoundStage.ResolveRequestedLanePush));
                return RoundResolutionResult.Accept(events);
            }

            // An end-of-round push completes the stage it was consumed at; a mid-turn one owes the
            // Initiative advance p.8 withheld while it ran.
            if (endOfRoundRequest != null &&
                CurrentEndOfRoundStage == EndOfRoundStage.ResolveRequestedLanePush)
            {
                return CompleteEndOfRoundStage(
                    EndOfRoundStage.ResolveRequestedLanePush,
                    EndOfRoundStageOutcomeKind.Completed,
                    events);
            }

            if (CurrentTurnAuthority == null)
            {
                return RoundResolutionResult.Accept(events);
            }

            var continued = CurrentTurnAuthority.ContinueAfterImmediateLanePush();
            if (!continued.Accepted)
            {
                throw new InvalidOperationException(
                    "A finished mid-turn Lane Push could not hand the turn back to Initiative order.");
            }

            events.AddRange(continued.Events.Select(value =>
                RoundResolutionEvent.FromTurn(RoundIndex, TurnIndex, value)));
            if (CurrentTurnAuthority.IsEnded)
            {
                CompleteCurrentTurn(events);
            }

            return RoundResolutionResult.Accept(events);
        }

        private static RoundResolutionRejectionReason TranslateTransitionRejection(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionRejectionReason reason)
        {
            switch (reason)
            {
                case GuardsDigital.Core.Rules.Transitions.SystemTransitionRejectionReason.UnknownDefinition:
                    return RoundResolutionRejectionReason.UnknownSystemTransition;
                case GuardsDigital.Core.Rules.Transitions.SystemTransitionRejectionReason.TransitionAlreadyRunning:
                    return RoundResolutionRejectionReason.SystemTransitionAlreadyRunning;
                case GuardsDigital.Core.Rules.Transitions.SystemTransitionRejectionReason.NoPendingStep:
                    return RoundResolutionRejectionReason.NoPendingSystemTransitionStep;
                default:
                    // Wrong instance, stale step, wrong chooser and an illegal option are all one thing
                    // to a caller: this answer is not about the step that is paused, and the
                    // continuation is exactly as it was.
                    return RoundResolutionRejectionReason.IllegalSystemTransitionChoice;
            }
        }

        internal RoundResolutionRestoreState CaptureRestoreState()
        {
            var rewards = CardActions?.DefeatRewards == null
                ? DefeatRewardRestoreStateHolder.Absent()
                : DefeatRewardRestoreStateHolder.Present(CardActions.DefeatRewards.CaptureRestoreState());
            return new RoundResolutionRestoreState(
                new RoundCursorRestoreState(RoundIndex, TurnIndex, Phase, TieBreakerState),
                new RoundHistoryRestoreState(TurnSlots, CompletedRounds),
                new EndOfRoundRestoreState(
                    CurrentEndOfRoundStage,
                    EndOfRoundOutcomes,
                    PendingMinionBattleChoice,
                    PendingLanePushRequest),
                new RoundCardActionsRestoreState(CardActions != null, rewards),
                systemTransitions.CaptureRestoreState());
        }

        internal static RoundResolutionAuthority Restore(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            RoundResolutionRestoreState state,
            TurnCardCommitmentAuthority commitmentAuthority,
            TurnResolutionAuthority turnAuthority,
            CardActionResolutionAuthority cardActions,
            ActiveEffectLifecycle timing,
            MarkerAttachmentRegister markers,
            AdjacentMoveAuthority board,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority systemTransitions,
            GuardsDigital.Core.Rules.LanePush.LanePushConsumption lanePush)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var roundIndex = state.Cursor.RoundIndex;
            var turnIndex = state.Cursor.TurnIndex;
            var phase = state.Cursor.Phase;
            var tieBreakerState = state.Cursor.TieBreakerState;
            var currentEndOfRoundStage = state.EndOfRound.CurrentStage;
            var pendingMinionBattleChoice = state.EndOfRound.PendingMinionBattleChoice;
            var pendingLanePushRequest = state.EndOfRound.PendingLanePushRequest;
            var authority = new RoundResolutionAuthority(
                definitions,
                instances,
                participants,
                tieBreakerState,
                roundIndex,
                RoundResolutionComposition.ForMatch(
                    cardActions, board, timing, markers, systemTransitions, lanePush));
            if (turnIndex < 1 || turnIndex > TurnsPerRound || !Enum.IsDefined(typeof(RoundResolutionPhase), phase))
            {
                throw new ArgumentException("Restored round cursor is invalid.");
            }

            authority.RoundIndex = roundIndex;
            authority.TurnIndex = turnIndex;
            authority.Phase = phase;
            authority.tieBreakerState = tieBreakerState;
            authority.turnSlots.Clear();
            authority.turnSlots.AddRange(state.History.TurnSlots);
            authority.completedRounds.Clear();
            authority.completedRounds.AddRange(state.History.CompletedRounds);
            authority.endOfRoundOutcomes.Clear();
            authority.endOfRoundOutcomes.AddRange(state.EndOfRound.Outcomes);
            authority.CurrentEndOfRoundStage = currentEndOfRoundStage;
            authority.CurrentCommitmentAuthority = commitmentAuthority;
            authority.CurrentTurnAuthority = turnAuthority;
            authority.PendingMinionBattleChoice = pendingMinionBattleChoice;
            authority.PendingLanePushRequest = pendingLanePushRequest;

            var awaitingShape = phase == RoundResolutionPhase.AwaitingCommitments &&
                commitmentAuthority != null && turnAuthority == null && !currentEndOfRoundStage.HasValue;
            var resolvingShape = phase == RoundResolutionPhase.ResolvingTurn &&
                commitmentAuthority != null && turnAuthority != null && !turnAuthority.IsEnded &&
                !currentEndOfRoundStage.HasValue;
            var endShape = (phase == RoundResolutionPhase.EndOfRound || phase == RoundResolutionPhase.GameEnded) &&
                commitmentAuthority != null && turnAuthority != null && turnAuthority.IsEnded &&
                currentEndOfRoundStage.HasValue;
            if (!awaitingShape && !resolvingShape && !endShape)
            {
                throw new ArgumentException("Restored round phase does not match its active authority or cursor.");
            }

            var pendingMinionBattleShape = pendingMinionBattleChoice == null ||
                (phase == RoundResolutionPhase.EndOfRound &&
                    currentEndOfRoundStage == EndOfRoundStage.MinionBattle &&
                    board != null);
            if (!pendingMinionBattleShape)
            {
                throw new ArgumentException("Restored Minion Battle choice does not match the round cursor.");
            }

            if (pendingMinionBattleChoice != null)
            {
                var expectedPlan = MinionBattleRules.Plan(
                    roundIndex,
                    board,
                    participants,
                    cardActions?.CurrentModifications(timing, participants, tieBreakerState) ??
                        GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty);
                if (expectedPlan.PendingChoice == null)
                {
                    throw new ArgumentException("Restored Minion Battle choice is no longer required by authoritative state.");
                }

                PendingMinionBattleChoice expected;
                try
                {
                    expected = expectedPlan.PendingChoice.WithSelected(
                        pendingMinionBattleChoice.SelectedUnits);
                }
                catch (ArgumentException exception)
                {
                    throw new ArgumentException(
                        "Restored Minion Battle selection violates authoritative Heavy-last ordering.",
                        exception);
                }

                if (!MinionBattleChoicesMatch(expected, pendingMinionBattleChoice))
                {
                    throw new ArgumentException("Restored Minion Battle choice does not match authoritative candidates.");
                }
            }

            if (pendingLanePushRequest != null &&
                (phase != RoundResolutionPhase.EndOfRound ||
                    !currentEndOfRoundStage.HasValue ||
                    (int)currentEndOfRoundStage.Value < (int)EndOfRoundStage.RemoveTokensAndReturnMarkers ||
                    pendingLanePushRequest.RoundIndex != roundIndex ||
                    board == null ||
                    participants.GetTeam(pendingLanePushRequest.WinningTeamId).Side !=
                        pendingLanePushRequest.WinningSide ||
                    participants.GetTeam(pendingLanePushRequest.LosingTeamId).Side !=
                        pendingLanePushRequest.LosingSide ||
                    !pendingLanePushRequest.CauseResolutionId.Equals(
                        new EndOfRoundResolutionId($"round-{roundIndex}-minion-battle")) ||
                    !GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.TryGetCurrentBattleZone(
                        board, out var currentZone) ||
                    !currentZone.Id.Equals(pendingLanePushRequest.BattleZoneId) ||
                    board.Minions.Any(value =>
                        value.TeamSide == pendingLanePushRequest.LosingSide &&
                        GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInCurrentBattleZone(
                            board, value.OccupiedSpace.SpaceId))))
            {
                throw new ArgumentException("Restored Lane-Push request does not match authoritative end-of-round state.");
            }

            return authority;
        }

        private static bool MinionBattleChoicesMatch(
            PendingMinionBattleChoice expected,
            PendingMinionBattleChoice actual)
        {
            return expected.ResolutionId.Equals(actual.ResolutionId) &&
                expected.ChoiceId.Equals(actual.ChoiceId) &&
                expected.LosingTeamId.Equals(actual.LosingTeamId) &&
                expected.ChoosingSide == actual.ChoosingSide &&
                expected.BattleZoneId.Equals(actual.BattleZoneId) &&
                expected.RequiredRemovalCount == actual.RequiredRemovalCount &&
                expected.Candidates.SequenceEqual(actual.Candidates) &&
                expected.SelectedUnits.SequenceEqual(actual.SelectedUnits) &&
                expected.LegalSelectUnits.SequenceEqual(actual.LegalSelectUnits) &&
                expected.LegalDeselectUnits.SequenceEqual(actual.LegalDeselectUnits);
        }

        public RoundResolutionResult TryCommitCard(HeroId heroId, CardInstanceId cardInstanceId)
        {
            return ExecuteCommitment(() => CurrentCommitmentAuthority.TryCommitCard(
                heroId,
                cardInstanceId));
        }

        public RoundResolutionResult TryPass(HeroId heroId)
        {
            return ExecuteCommitment(() => CurrentCommitmentAuthority.TryPass(heroId));
        }

        public RoundResolutionResult TryChooseTiedActor(
            TeamSide choosingSide,
            CardInstanceId cardInstanceId)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryChooseTiedActor(
                choosingSide,
                cardInstanceId));
        }

        public RoundResolutionResult TryPerformHold(CardInstanceId cardInstanceId)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryPerformHold(cardInstanceId));
        }

        public RoundResolutionResult TryStartMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryStartMovement(resolutionId, offer));
        }

        public RoundResolutionResult TryAnswerMovementChoice(MovementChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerMovementChoice(command));
        }

        public RoundResolutionResult TryPerformFastTravel(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            GuardsDigital.Core.Board.HexCoordinate destination,
            IEnumerable<MinionOccupancy> minions)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryPerformFastTravel(
                resolutionId,
                replacementOffer,
                replacedMovementActionDefinitionId,
                destination,
                minions));
        }

        public RoundResolutionResult TryPerformClear(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryPerformClear(
                resolutionId,
                replacementOffer,
                replacedAttackActionDefinitionId,
                selectedTokenIds));
        }

        public RoundResolutionResult TryStartAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryStartAttack(resolutionId, offer));
        }

        public RoundResolutionResult TryStartSkill(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryStartSkill(resolutionId, offer));
        }

        public RoundResolutionResult TryAnswerEffectChoice(EffectChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerEffectChoice(command));
        }

        public RoundResolutionResult TryAnswerDisplacedMinionCorrection(
            GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionCommand command)
        {
            return ExecuteTurnCommand(() =>
                CurrentTurnAuthority.TryAnswerDisplacedMinionCorrection(command));
        }

        public RoundResolutionResult TryAnswerAttackTarget(AttackTargetChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerAttackTarget(command));
        }

        public RoundResolutionResult TryAnswerHeroRespawn(HeroRespawnCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerHeroRespawn(command));
        }

        public RoundResolutionResult TryAnswerDefenseResponse(DefenseResponseChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerDefenseResponse(command));
        }

        public RoundResolutionResult TryAnswerMinionBattleChoice(MinionBattleChoiceCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (Phase != RoundResolutionPhase.EndOfRound ||
                CurrentEndOfRoundStage != EndOfRoundStage.MinionBattle ||
                PendingMinionBattleChoice == null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.NoPendingMinionBattleChoice);
            }

            var choice = PendingMinionBattleChoice;
            if (!command.ResolutionId.Equals(choice.ResolutionId) ||
                !command.ChoiceId.Equals(choice.ChoiceId))
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.StaleMinionBattleChoice);
            }

            if (command.ChoosingSide != choice.ChoosingSide)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.WrongMinionBattleChooser);
            }

            if (command.Kind == MinionBattleChoiceCommandKind.Select)
            {
                if (!choice.LegalSelectUnits.Contains(command.Unit))
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
                }

                PendingMinionBattleChoice = choice.WithSelected(
                    choice.SelectedUnits.Concat(new[] { command.Unit }));
                return RoundResolutionResult.Accept(new[]
                {
                    RoundResolutionEvent.MinionBattleSelectionChanged(
                        RoundIndex,
                        PendingMinionBattleChoice,
                        command.Unit,
                        command.Kind)
                });
            }

            if (command.Kind == MinionBattleChoiceCommandKind.Deselect)
            {
                if (!choice.LegalDeselectUnits.Contains(command.Unit))
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
                }

                PendingMinionBattleChoice = choice.WithSelected(
                    choice.SelectedUnits.Where(value => !value.Equals(command.Unit)));
                return RoundResolutionResult.Accept(new[]
                {
                    RoundResolutionEvent.MinionBattleSelectionChanged(
                        RoundIndex,
                        PendingMinionBattleChoice,
                        command.Unit,
                        command.Kind)
                });
            }

            if (!choice.CanConfirm)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IncompleteMinionBattleSelection);
            }

            return CompleteMinionBattle(
                choice.ResolutionId,
                choice.BattleZoneId,
                participants.Teams.Single(value => value.Side != choice.ChoosingSide),
                participants.GetTeam(choice.LosingTeamId),
                choice.SelectedUnits);
        }

        private RoundResolutionResult ResolveMinionBattle()
        {
            if (board == null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.MissingRequiredEndOfRoundHandler);
            }

            var plan = MinionBattleRules.Plan(
                RoundIndex,
                board,
                participants,
                cardActions?.CurrentModifications(timing, participants, TieBreakerState) ??
                    GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty);
            if (plan.PendingChoice != null)
            {
                PendingMinionBattleChoice = plan.PendingChoice;
                return RoundResolutionResult.Accept(new[]
                {
                    RoundResolutionEvent.MinionBattleChoiceRequested(RoundIndex, plan.PendingChoice),
                    RoundResolutionEvent.EndOfRoundStageOutcome(
                        RoundIndex,
                        EndOfRoundStage.MinionBattle,
                        EndOfRoundStageOutcomeKind.Paused)
                });
            }

            if (!plan.LosingTeamId.IsDefined)
            {
                return CompleteMinionBattle(
                    plan.ResolutionId,
                    plan.BattleZoneId,
                    null,
                    null,
                    Array.Empty<GuardsDigital.Core.Rules.Modifications.RuleObject>());
            }

            return CompleteMinionBattle(
                plan.ResolutionId,
                plan.BattleZoneId,
                participants.GetTeam(plan.WinningTeamId),
                participants.GetTeam(plan.LosingTeamId),
                plan.AutomaticRemovalUnits);
        }

        /// <summary>
        /// Removes the effective minion units selected by the battle. A hero removal is not Defeat:
        /// it awards nothing and changes no Life; it only takes that hero off board for respawn.
        /// </summary>
        private RoundResolutionResult CompleteMinionBattle(
            EndOfRoundResolutionId resolutionId,
            GuardsDigital.Core.Board.ZoneId battleZoneId,
            Team winningTeam,
            Team losingTeam,
            IEnumerable<GuardsDigital.Core.Rules.Modifications.RuleObject> removalUnits)
        {
            var units = (removalUnits ?? throw new ArgumentNullException(nameof(removalUnits))).ToArray();
            if (units.Distinct().Count() != units.Length)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
            }

            var modifications = cardActions?.CurrentModifications(timing, participants, TieBreakerState) ??
                GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty;
            var candidates = units.Select(unit => ResolveBattleCandidate(unit, modifications)).ToArray();
            if (candidates.Any(value => !value.HasValue) || candidates.Any(value =>
                    losingTeam == null || value.Value.TeamSide != losingTeam.Side))
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
            }

            var ordered = candidates.Select(value => value.Value)
                .OrderBy(value => value.IsHeavy ? 1 : 0)
                .ThenBy(value => GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.Of(value.Unit))
                .ThenBy(value => GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.Of(value.Unit) ==
                    GuardsDigital.Core.Rules.Modifications.RuleObjectKind.Hero
                    ? value.Unit.HeroId.Value
                    : value.Unit.MinionId.Value, StringComparer.Ordinal)
                .ToArray();
            var events = new List<RoundResolutionEvent>();
            foreach (var candidate in ordered)
            {
                if (GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.Of(candidate.Unit) ==
                    GuardsDigital.Core.Rules.Modifications.RuleObjectKind.Hero)
                {
                    board.CommitValidatedHeroRemoval(candidate.Unit.HeroId);
                }
                else
                {
                    board.CommitValidatedMinionRemoval(candidate.Unit.MinionId);
                }

                events.Add(RoundResolutionEvent.MinionBattleUnitRemoved(
                    RoundIndex,
                    resolutionId,
                    losingTeam.Id,
                    losingTeam.Side,
                    candidate.Unit));
            }

            // `This Round` remains live through the Minion Battle it can modify. The round-ending
            // checkpoint occurs only after that battle has counted and removed effective minions.
            foreach (var timingEvent in timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.RoundEnd,
                RoundIndex,
                TurnsPerRound),
                TieBreakerState))
            {
                events.Add(RoundResolutionEvent.FromTiming(RoundIndex, TurnsPerRound, timingEvent));
            }

            PendingMinionBattleChoice = null;
            var postRoundModifications = cardActions?.CurrentModifications(
                timing, participants, TieBreakerState) ??
                GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty;
            if (losingTeam != null && ordered.Length > 0 &&
                !HasEffectiveMinionInBattleZone(losingTeam.Side, postRoundModifications))
            {
                PendingLanePushRequest = new PendingLanePushRequest(
                    new LanePushRequestId($"round-{RoundIndex}-minion-battle-lane-push"),
                    RoundIndex,
                    battleZoneId,
                    winningTeam.Id,
                    winningTeam.Side,
                    losingTeam.Id,
                    losingTeam.Side,
                    resolutionId);
                events.Add(RoundResolutionEvent.LanePushRequested(RoundIndex, PendingLanePushRequest));
            }

            endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(
                EndOfRoundStage.MinionBattle,
                EndOfRoundStageOutcomeKind.Completed));
            CurrentEndOfRoundStage = EndOfRoundStage.RemoveTokensAndReturnMarkers;
            events.Add(RoundResolutionEvent.EndOfRoundStageOutcome(
                RoundIndex,
                EndOfRoundStage.MinionBattle,
                EndOfRoundStageOutcomeKind.Completed));
            return RoundResolutionResult.Accept(events);
        }

        private BattleRemovalCandidate? ResolveBattleCandidate(
            GuardsDigital.Core.Rules.Modifications.RuleObject unit,
            GuardsDigital.Core.Rules.Modifications.RuleModificationSet modifications)
        {
            if (!GuardsDigital.Core.Rules.Modifications.EffectiveUnitKindQuery.Is(
                    unit,
                    GuardsDigital.Core.Rules.Modifications.EffectiveUnitKind.Minion,
                    modifications))
            {
                return null;
            }

            if (GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.IsMinionMiniature(unit))
            {
                var minion = board.Minions.SingleOrDefault(value => value.MinionId.Equals(unit.MinionId));
                return minion != null && GuardsDigital.Core.Rules.Modifications.BattleZoneQuery
                        .IsInCurrentBattleZone(board, minion.OccupiedSpace.SpaceId)
                    ? new BattleRemovalCandidate(
                        unit, minion.TeamSide, minion.MinionType == StandardMinionType.Heavy)
                    : (BattleRemovalCandidate?)null;
            }

            if (GuardsDigital.Core.Rules.Modifications.IntrinsicKindQuery.Of(unit) !=
                    GuardsDigital.Core.Rules.Modifications.RuleObjectKind.Hero ||
                !board.TryGetHeroCoordinate(unit.HeroId, out var coordinate))
            {
                return null;
            }

            var space = board.MapDefinition.Spaces.Single(candidate =>
                candidate.Kind == GuardsDigital.Core.Board.BoardMapSpaceKind.Playable &&
                candidate.Coordinate.Equals(coordinate));
            return GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInCurrentBattleZone(
                    board, space.Id)
                ? new BattleRemovalCandidate(
                    unit, participants.GetTeamForHero(unit.HeroId).Side, false)
                : (BattleRemovalCandidate?)null;
        }

        private bool HasEffectiveMinionInBattleZone(
            TeamSide side,
            GuardsDigital.Core.Rules.Modifications.RuleModificationSet modifications)
        {
            return board.Minions.Any(minion =>
                    minion.TeamSide == side &&
                    GuardsDigital.Core.Rules.Modifications.EffectiveUnitKindQuery.Is(
                        GuardsDigital.Core.Rules.Modifications.RuleObject.ForMinion(minion.MinionId),
                        GuardsDigital.Core.Rules.Modifications.EffectiveUnitKind.Minion,
                        modifications) &&
                    GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInCurrentBattleZone(
                        board, minion.OccupiedSpace.SpaceId)) ||
                board.Occupancies.Any(hero =>
                {
                    var unit = GuardsDigital.Core.Rules.Modifications.RuleObject.ForHero(hero.HeroId);
                    if (participants.GetTeamForHero(hero.HeroId).Side != side ||
                        !GuardsDigital.Core.Rules.Modifications.EffectiveUnitKindQuery.Is(
                            unit,
                            GuardsDigital.Core.Rules.Modifications.EffectiveUnitKind.Minion,
                            modifications))
                    {
                        return false;
                    }

                    var space = board.MapDefinition.Spaces.Single(candidate =>
                        candidate.Kind == GuardsDigital.Core.Board.BoardMapSpaceKind.Playable &&
                        candidate.Coordinate.Equals(hero.Coordinate));
                    return GuardsDigital.Core.Rules.Modifications.BattleZoneQuery
                        .IsInCurrentBattleZone(board, space.Id);
                });
        }

        private readonly struct BattleRemovalCandidate
        {
            public BattleRemovalCandidate(
                GuardsDigital.Core.Rules.Modifications.RuleObject unit,
                TeamSide teamSide,
                bool isHeavy)
            {
                Unit = unit;
                TeamSide = teamSide;
                IsHeavy = isHeavy;
            }

            public GuardsDigital.Core.Rules.Modifications.RuleObject Unit { get; }
            public TeamSide TeamSide { get; }
            public bool IsHeavy { get; }
        }

        public RoundResolutionResult TryAdvanceEndOfRound()
        {
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.EndOfRound || !CurrentEndOfRoundStage.HasValue)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.NotResolvingEndOfRound);
            }

            var stage = CurrentEndOfRoundStage.Value;
            if (PendingMinionBattleChoice != null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IncompleteMinionBattleSelection);
            }

            // Milestone 6.11. p.8's push now runs here rather than stopping the round. A push that
            // stops on the sidebar's Place is answered by its own command, so a second advance while
            // one is stopped is refused rather than beginning a second push.
            if (stage == EndOfRoundStage.ResolveRequestedLanePush && PendingLanePushRequest != null)
            {
                return ResolveRequestedLanePush(stage);
            }

            if (stage == EndOfRoundStage.MinionBattle &&
                endOfRoundCapabilities.IsAvailable(EndOfRoundStage.MinionBattle) &&
                board != null)
            {
                return ResolveMinionBattle();
            }

            // Milestone 6.12. Handled here rather than through an IEndOfRoundStageHandler for the reason
            // Minion Battle is: the stage's collaborators are the board and the marker register, which
            // the authority already holds and the handler context deliberately does not carry.
            if (stage == EndOfRoundStage.RemoveTokensAndReturnMarkers &&
                endOfRoundCapabilities.IsAvailable(EndOfRoundStage.RemoveTokensAndReturnMarkers))
            {
                return ResolveComponentCleanup(stage);
            }

            EndOfRoundStageResolution resolution;
            if (!endOfRoundCapabilities.IsAvailable(stage))
            {
                resolution = EndOfRoundStageResolution.NotApplicable();
            }
            else
            {
                if (!endOfRoundHandlers.TryGetValue(stage, out var handler))
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.MissingRequiredEndOfRoundHandler);
                }

                resolution = handler.TryResolve(new EndOfRoundStageContext(
                    RoundIndex,
                    instances,
                    participants));
            }

            var events = new List<RoundResolutionEvent>();
            foreach (var retrieval in resolution.RetrievedCards ?? Array.Empty<CardRetrievalFact>())
            {
                events.Add(RoundResolutionEvent.RetrievedCard(RoundIndex, stage, retrieval));
            }

            if (resolution.ActiveEffectCancellationBoundaryReached)
            {
                events.AddRange(timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                    TimingCheckpoint.CardsRetrieved,
                    RoundIndex,
                    TurnIndex),
                    TieBreakerState).Select(value => RoundResolutionEvent.FromTiming(
                        RoundIndex, TurnIndex, value)));
                events.Add(RoundResolutionEvent.EndOfRoundBoundary(
                    RoundResolutionEventKind.ActiveEffectCancellationCheckpoint,
                    RoundIndex,
                    stage));
            }

            return CompleteEndOfRoundStage(stage, resolution.OutcomeKind, events);
        }

        /// <summary>
        /// Rulebook p.8's "Remove Tokens, Return Markers": "Remove all tokens from the board. Return all
        /// markers back into the supply."
        ///
        /// Deterministic and unconditional. It asks no player anything, so unlike Minion Battle and the
        /// Lane Push it can never pause, which is why it carries no continuation and adds no persisted
        /// state - the stage cursor that already crosses the snapshot is the whole of where it is.
        ///
        /// Order inside the stage is tokens then markers, matching the page's two sentences. Nothing
        /// depends on it: a token is board state and a marker is not, so neither can change what the
        /// other's rule sees.
        /// </summary>
        private RoundResolutionResult ResolveComponentCleanup(EndOfRoundStage stage)
        {
            var events = new List<RoundResolutionEvent>();

            if (board != null)
            {
                foreach (var token in GuardsDigital.Core.Components.ComponentCleanupRules
                             .TokensRemovedAtEndOfRound(board.PlacedTokens))
                {
                    events.Add(RoundResolutionEvent.TokenRemovedAtEndOfRound(
                        RoundIndex,
                        board.CommitValidatedTokenReclamation(token.Id)));
                }
            }

            foreach (var attachment in GuardsDigital.Core.Components.ComponentCleanupRules
                         .MarkersReturnedAtEndOfRound(markers.Attachments))
            {
                events.Add(RoundResolutionEvent.MarkerReturnedAtEndOfRound(
                    RoundIndex,
                    markers.Return(attachment.Id)));
            }

            return CompleteEndOfRoundStage(stage, EndOfRoundStageOutcomeKind.Completed, events);
        }

        /// <summary>
        /// The tail every end-of-round stage shares: report what the stage did, stop if it paused or
        /// ended the game, otherwise move to the next stage or close the round.
        ///
        /// Extracted at milestone 6.11 because the Lane-Push stage stopped being a refusal and became a
        /// stage that can pause, end the game, or complete like any other, and a second copy of this
        /// tail is a second place for the round's own order to drift.
        /// </summary>
        private RoundResolutionResult CompleteEndOfRoundStage(
            EndOfRoundStage stage,
            EndOfRoundStageOutcomeKind outcomeKind,
            List<RoundResolutionEvent> events)
        {
            events.Add(RoundResolutionEvent.EndOfRoundStageOutcome(
                RoundIndex,
                stage,
                outcomeKind));
            if (outcomeKind == EndOfRoundStageOutcomeKind.Paused)
            {
                return RoundResolutionResult.Accept(events);
            }

            if (outcomeKind == EndOfRoundStageOutcomeKind.GameEnded)
            {
                endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(stage, outcomeKind));
                Phase = RoundResolutionPhase.GameEnded;
                events.Add(RoundResolutionEvent.EndOfRoundBoundary(
                    RoundResolutionEventKind.GameEnded,
                    RoundIndex,
                    stage));
                return RoundResolutionResult.Accept(events);
            }

            // Synthetic/no-board configurations still cross the same round-ending timing boundary
            // when their Minion Battle stage is skipped or handled generically.
            if (stage == EndOfRoundStage.MinionBattle)
            {
                events.AddRange(timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                    TimingCheckpoint.RoundEnd,
                    RoundIndex,
                    TurnsPerRound),
                    TieBreakerState).Select(value => RoundResolutionEvent.FromTiming(
                        RoundIndex, TurnsPerRound, value)));
            }

            endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(stage, outcomeKind));
            var nextStage = (EndOfRoundStage)((int)stage + 1);
            if (nextStage != EndOfRoundStage.Complete)
            {
                CurrentEndOfRoundStage = nextStage;
                return RoundResolutionResult.Accept(events);
            }

            var completedRound = new CompletedRoundRecord(
                RoundIndex,
                turnSlots,
                endOfRoundOutcomes);
            completedRounds.Add(completedRound);
            events.Add(RoundResolutionEvent.CompletedRoundBoundary(
                RoundResolutionEventKind.RoundCompleted,
                completedRound,
                RoundIndex));

            RoundIndex++;
            TurnIndex = 1;
            turnSlots.Clear();
            endOfRoundOutcomes.Clear();
            CurrentEndOfRoundStage = null;
            CurrentTurnAuthority = null;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(instances, participants);
            Phase = RoundResolutionPhase.AwaitingCommitments;
            events.Add(RoundResolutionEvent.CompletedRoundBoundary(
                RoundResolutionEventKind.NextRoundStarted,
                completedRound,
                RoundIndex));
            return RoundResolutionResult.Accept(events);
        }

        private RoundResolutionResult ExecuteCommitment(Func<TurnCardCommitmentResult> command)
        {
            if (Phase == RoundResolutionPhase.EndOfRound)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.RoundAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.AwaitingCommitments)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.NotAwaitingCommitments);
            }

            var result = command();
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.CommitmentCommandRejected,
                    result.RejectionReason);
            }

            var events = result.Events
                .Select(ruleEvent => RoundResolutionEvent.FromCommitment(
                    RoundIndex,
                    TurnIndex,
                    ruleEvent))
                .ToList();
            if (CurrentCommitmentAuthority.IsRevealed)
            {
                StartCurrentTurn(events);
            }

            return RoundResolutionResult.Accept(events);
        }

        private RoundResolutionResult ExecuteTurnCommand(Func<TurnResolutionResult> command)
        {
            if (Phase == RoundResolutionPhase.EndOfRound)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.RoundAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.ResolvingTurn || CurrentTurnAuthority == null)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.NotResolvingTurn);
            }

            var result = command();
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.TurnCommandRejected,
                    turnReason: result.RejectionReason);
            }

            var events = result.Events
                .Select(ruleEvent => RoundResolutionEvent.FromTurn(
                    RoundIndex,
                    TurnIndex,
                    ruleEvent))
                .ToList();
            if (CurrentTurnAuthority.IsEnded)
            {
                CompleteCurrentTurn(events);
            }

            return RoundResolutionResult.Accept(events);
        }

        private void StartCurrentTurn(ICollection<RoundResolutionEvent> events)
        {
            var slot = new RoundTurnSlot(
                RoundIndex,
                TurnIndex,
                CurrentCommitmentAuthority.ExpectedHeroIds.Select(heroId =>
                    new TurnCardAssignment(
                        heroId,
                        CurrentCommitmentAuthority.GetRevealedCardInstanceId(heroId),
                        CurrentCommitmentAuthority.GetStatus(heroId) == TurnCardCommitmentStatus.Passed)),
                instances,
                participants);
            turnSlots.Add(slot);
            events.Add(RoundResolutionEvent.SlotRecorded(slot));

            CurrentTurnAuthority = new TurnResolutionAuthority(
                definitions,
                instances,
                participants,
                tieBreakerState,
                cardActions,
                timing,
                RoundIndex,
                TurnIndex,
                lanePush);
            Phase = RoundResolutionPhase.ResolvingTurn;
            foreach (var timingEvent in timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.CardsPlayedAndRevealed,
                RoundIndex,
                TurnIndex),
                tieBreakerState))
                events.Add(RoundResolutionEvent.FromTiming(RoundIndex, TurnIndex, timingEvent));
            var start = CurrentTurnAuthority.TryStart();
            if (!start.Accepted)
            {
                throw new InvalidOperationException("A validated round turn could not start.");
            }

            foreach (var turnEvent in start.Events)
            {
                events.Add(RoundResolutionEvent.FromTurn(RoundIndex, TurnIndex, turnEvent));
            }

            if (CurrentTurnAuthority.IsEnded)
            {
                CompleteCurrentTurn(events);
            }
        }

        private void CompleteCurrentTurn(ICollection<RoundResolutionEvent> events)
        {
            tieBreakerState = CurrentTurnAuthority.TieBreakerState;
            if (TurnIndex == TurnsPerRound)
            {
                Phase = RoundResolutionPhase.EndOfRound;
                CurrentEndOfRoundStage = EndOfRoundStage.MinionBattle;
                events.Add(RoundResolutionEvent.Boundary(
                    RoundResolutionEventKind.EndOfRoundEntered,
                    RoundIndex,
                    TurnIndex));
                return;
            }

            TurnIndex++;
            CurrentTurnAuthority = null;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(instances, participants);
            Phase = RoundResolutionPhase.AwaitingCommitments;
            events.Add(RoundResolutionEvent.Boundary(
                RoundResolutionEventKind.TurnAdvanced,
                RoundIndex,
                TurnIndex));
        }
    }
}
