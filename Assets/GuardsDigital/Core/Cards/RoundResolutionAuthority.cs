using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

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
            this.endOfRoundCapabilities = endOfRoundCapabilities ??
                (board != null && board.MapDefinition.Zones.Count(value => value.IsStartingBattleZone) == 1
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

        public TurnCardCommitmentAuthority CurrentCommitmentAuthority { get; private set; }

        public TurnResolutionAuthority CurrentTurnAuthority { get; private set; }

        internal IReadOnlyList<CardDefinition> Definitions => definitions;

        internal IReadOnlyList<CardInstance> Instances => instances;

        internal MatchParticipants Participants => participants;

        internal CardActionResolutionAuthority CardActions => cardActions;

        public ActiveEffectLifecycle Timing => timing;

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
                new RoundCardActionsRestoreState(CardActions != null, rewards));
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
            AdjacentMoveAuthority board)
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
                RoundResolutionComposition.ForMatch(cardActions, board, timing));
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
                var expectedPlan = MinionBattleRules.Plan(roundIndex, board, participants);
                if (expectedPlan.PendingChoice == null)
                {
                    throw new ArgumentException("Restored Minion Battle choice is no longer required by authoritative state.");
                }

                PendingMinionBattleChoice expected;
                try
                {
                    expected = expectedPlan.PendingChoice.WithSelected(
                        pendingMinionBattleChoice.SelectedMinionIds);
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
                    board.MapDefinition.Zones.Count(value =>
                        value.Id.Equals(pendingLanePushRequest.BattleZoneId) && value.IsStartingBattleZone) != 1 ||
                    board.Minions.Any(value =>
                        value.TeamSide == pendingLanePushRequest.LosingSide &&
                        board.MapDefinition.GetZoneForSpace(value.OccupiedSpace.SpaceId).Id.Equals(
                            pendingLanePushRequest.BattleZoneId))))
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
                expected.SelectedMinionIds.SequenceEqual(actual.SelectedMinionIds) &&
                expected.LegalSelectMinionIds.SequenceEqual(actual.LegalSelectMinionIds) &&
                expected.LegalDeselectMinionIds.SequenceEqual(actual.LegalDeselectMinionIds);
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
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryPerformFastTravel(
                resolutionId,
                replacementOffer,
                replacedMovementActionDefinitionId,
                destination,
                minions));
        }

        public RoundResolutionResult TryStartAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryStartAttack(resolutionId, offer));
        }

        public RoundResolutionResult TryAnswerAttackTarget(AttackTargetChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerAttackTarget(command));
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
                if (!choice.LegalSelectMinionIds.Contains(command.MinionId))
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
                }

                PendingMinionBattleChoice = choice.WithSelected(
                    choice.SelectedMinionIds.Concat(new[] { command.MinionId }));
                return RoundResolutionResult.Accept(new[]
                {
                    RoundResolutionEvent.MinionBattleSelectionChanged(
                        RoundIndex,
                        PendingMinionBattleChoice,
                        command.MinionId,
                        command.Kind)
                });
            }

            if (command.Kind == MinionBattleChoiceCommandKind.Deselect)
            {
                if (!choice.LegalDeselectMinionIds.Contains(command.MinionId))
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
                }

                PendingMinionBattleChoice = choice.WithSelected(
                    choice.SelectedMinionIds.Where(value => !value.Equals(command.MinionId)));
                return RoundResolutionResult.Accept(new[]
                {
                    RoundResolutionEvent.MinionBattleSelectionChanged(
                        RoundIndex,
                        PendingMinionBattleChoice,
                        command.MinionId,
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
                choice.SelectedMinionIds);
        }

        private RoundResolutionResult ResolveMinionBattle()
        {
            if (board == null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.MissingRequiredEndOfRoundHandler);
            }

            var plan = MinionBattleRules.Plan(RoundIndex, board, participants);
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
                    Array.Empty<MinionId>());
            }

            return CompleteMinionBattle(
                plan.ResolutionId,
                plan.BattleZoneId,
                participants.GetTeam(plan.WinningTeamId),
                participants.GetTeam(plan.LosingTeamId),
                plan.AutomaticRemovalIds);
        }

        private RoundResolutionResult CompleteMinionBattle(
            EndOfRoundResolutionId resolutionId,
            GuardsDigital.Core.Board.ZoneId battleZoneId,
            Team winningTeam,
            Team losingTeam,
            IEnumerable<MinionId> removalIds)
        {
            var ids = (removalIds ?? throw new ArgumentNullException(nameof(removalIds))).ToArray();
            if (ids.Distinct().Count() != ids.Length)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
            }

            var candidates = ids.Select(id => board.Minions.SingleOrDefault(value => value.MinionId.Equals(id)))
                .ToArray();
            if (candidates.Any(value => value == null) ||
                candidates.Any(value =>
                    losingTeam == null || value.TeamSide != losingTeam.Side ||
                    !board.MapDefinition.GetZoneForSpace(value.OccupiedSpace.SpaceId).Id.Equals(battleZoneId)))
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.IneligibleMinionBattleSelection);
            }

            var ordered = candidates
                .OrderBy(value => value.MinionType == StandardMinionType.Heavy ? 1 : 0)
                .ThenBy(value => value.MinionId.Value, StringComparer.Ordinal)
                .ToArray();
            var events = new List<RoundResolutionEvent>();
            foreach (var minion in ordered)
            {
                board.CommitValidatedMinionRemoval(minion.MinionId);
                events.Add(RoundResolutionEvent.MinionBattleMinionRemoved(
                    RoundIndex,
                    resolutionId,
                    losingTeam.Id,
                    losingTeam.Side,
                    minion.MinionId));
            }

            PendingMinionBattleChoice = null;
            if (losingTeam != null && ordered.Length > 0 &&
                !board.Minions.Any(value =>
                    value.TeamSide == losingTeam.Side &&
                    board.MapDefinition.GetZoneForSpace(value.OccupiedSpace.SpaceId).Id.Equals(battleZoneId)))
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

            if (stage == EndOfRoundStage.ResolveRequestedLanePush && PendingLanePushRequest != null)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.MissingRequiredEndOfRoundHandler);
            }

            if (stage == EndOfRoundStage.MinionBattle &&
                endOfRoundCapabilities.IsAvailable(EndOfRoundStage.MinionBattle) &&
                board != null)
            {
                return ResolveMinionBattle();
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

            events.Add(RoundResolutionEvent.EndOfRoundStageOutcome(
                RoundIndex,
                stage,
                resolution.OutcomeKind));
            if (resolution.OutcomeKind == EndOfRoundStageOutcomeKind.Paused)
            {
                return RoundResolutionResult.Accept(events);
            }

            if (resolution.OutcomeKind == EndOfRoundStageOutcomeKind.GameEnded)
            {
                endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(stage, resolution.OutcomeKind));
                Phase = RoundResolutionPhase.GameEnded;
                events.Add(RoundResolutionEvent.EndOfRoundBoundary(
                    RoundResolutionEventKind.GameEnded,
                    RoundIndex,
                    stage));
                return RoundResolutionResult.Accept(events);
            }

            endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(stage, resolution.OutcomeKind));
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
                TurnIndex);
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
                foreach (var timingEvent in timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                    TimingCheckpoint.RoundEnd,
                    RoundIndex,
                    TurnIndex),
                    TieBreakerState))
                    events.Add(RoundResolutionEvent.FromTiming(RoundIndex, TurnIndex, timingEvent));
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
