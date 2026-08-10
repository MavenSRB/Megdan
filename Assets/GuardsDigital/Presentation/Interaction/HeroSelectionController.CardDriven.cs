using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    public sealed partial class HeroSelectionController
    {
        private const float CardPanelMargin = 12f;
        private const float CardPanelHeight = 310f;
        private const float TieBreakerIndicatorWidth = 200f;
        private const float TieBreakerIndicatorHeight = 96f;

        [SerializeField]
        private bool cardDrivenMovementEnabled;

        private readonly List<CardActionResolutionEvent> cardResolutionEvents =
            new List<CardActionResolutionEvent>();
        private readonly List<TurnCardCommitmentEvent> turnCardCommitmentEvents =
            new List<TurnCardCommitmentEvent>();
        private readonly List<TurnResolutionEvent> turnResolutionEvents =
            new List<TurnResolutionEvent>();
        private readonly List<RoundResolutionEvent> roundResolutionEvents =
            new List<RoundResolutionEvent>();
        private readonly Dictionary<HeroId, PlaceholderMovementHand> placeholderHands =
            new Dictionary<HeroId, PlaceholderMovementHand>();
        private readonly List<string> directorUndoSnapshots = new List<string>();
        private bool placeholderOneVsOneHandsInitialized;
        private MatchParticipants placeholderParticipants;
        private PlaceholderOneVsOneTestConfiguration placeholderConfiguration;

        private PlaceholderMovementHand placeholderHand;
        private RoundResolutionAuthority roundResolutionAuthority;
        private CardActionResolutionAuthority cardResolutionAuthority;
        private CardInstanceId? selectedCardInstanceId;
        private ActionOffer? selectedActionOffer;
        private IReadOnlyList<HexCoordinate> lastCardMovementRoute = Array.Empty<HexCoordinate>();
        private int cardResolutionSequence;
        private int lastCardMovementAllowance;
        private int lastCardRemainingAllowance;
        private string cardStatus = "Select a test card.";
        private string directorInitialSnapshot;

        public bool IsCardDrivenMovementEnabled => cardDrivenMovementEnabled;

        public IReadOnlyList<CardDefinition> VisibleCardDefinitions
        {
            get
            {
                InitializeCardDrivenMovementIfEnabled();
                return placeholderHand != null
                    ? GetAllPlaceholderCardDefinitions()
                    : Array.Empty<CardDefinition>();
            }
        }

        public IReadOnlyList<CardInstance> VisibleCardInstances
        {
            get
            {
                InitializeCardDrivenMovementIfEnabled();
                return placeholderHand != null
                    ? GetVisibleHandInstances(placeholderHand)
                    : Array.Empty<CardInstance>();
            }
        }

        public IReadOnlyList<DirectorTestHand> VisibleDirectorHands
        {
            get
            {
                InitializeCardDrivenMovementIfEnabled();
                EnsurePlaceholderOneVsOneHands();
                EnsureRoundResolutionAuthority();
                if (placeholderConfiguration == null)
                {
                    return Array.Empty<DirectorTestHand>();
                }

                return new[]
                {
                    new DirectorTestHand(TeamSide.Red, placeholderConfiguration.RedHand),
                    new DirectorTestHand(TeamSide.Blue, placeholderConfiguration.BlueHand)
                };
            }
        }

        public CardInstanceId? SelectedCardInstanceId => selectedCardInstanceId;

        public ActionOffer? SelectedActionOffer => selectedActionOffer;

        public IReadOnlyList<ActionOffer> SelectedCardOffers
        {
            get
            {
                if (placeholderHand == null || !selectedCardInstanceId.HasValue || heroView == null)
                {
                    return Array.Empty<ActionOffer>();
                }

                if (IsVisibleTurnActive)
                {
                    var turnAuthority = roundResolutionAuthority.CurrentTurnAuthority;
                    return ActionOfferQuery
                        .GetAvailableTurnActionOffers(
                            placeholderConfiguration.CardDefinitions,
                            GetAllPlaceholderCardInstances(),
                            turnAuthority.SelectedActor.ActorId,
                            turnAuthority.SelectedActor.CardInstanceId)
                        .Where(offer =>
                        {
                            var kind = GetActionDefinition(offer).Kind;
                            return kind == ActionKind.Movement || kind == ActionKind.Attack;
                        })
                        .ToArray();
                }

                return ActionOfferQuery
                    .GetAvailableOffers(
                        placeholderHand.Definitions,
                        placeholderHand.Instances,
                        heroView.Occupancy.HeroId,
                        ActionOfferWindow.NormalPlay)
                    .Where(offer =>
                    {
                        var kind = GetActionDefinition(offer).Kind;
                        return kind == ActionKind.Movement || kind == ActionKind.Attack;
                    })
                    .Where(offer => offer.SourceCardInstanceId.Equals(selectedCardInstanceId.Value))
                    .ToArray();
            }
        }

        public bool IsCardResolutionPending =>
            IsCardMovementResolutionPending || IsAttackTargetPending || IsDefenseResponsePending || IsAttackOutcomePending;

        public bool IsCardMovementResolutionPending =>
            cardResolutionAuthority != null && cardResolutionAuthority.PendingChoice != null;

        public bool IsAttackTargetPending =>
            cardResolutionAuthority != null && cardResolutionAuthority.PendingAttackTargetChoice != null;

        public bool IsDefenseResponsePending =>
            cardResolutionAuthority != null && cardResolutionAuthority.PendingDefenseChoice != null;

        public bool IsAttackOutcomePending =>
            cardResolutionAuthority != null &&
            cardResolutionAuthority.PendingAttackResolution != null &&
            cardResolutionAuthority.PendingAttackResolution.Phase == AttackResolutionPhase.CompareOutcome;

        public PendingMovementChoice PendingCardChoice => cardResolutionAuthority?.PendingChoice;

        public PendingAttackTargetChoice PendingAttackTargetChoice =>
            cardResolutionAuthority?.PendingAttackTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice =>
            cardResolutionAuthority?.PendingDefenseChoice;

        public PendingAttackResolution PendingAttackResolution =>
            cardResolutionAuthority?.PendingAttackResolution;

        public IReadOnlyList<ReachableMovementDestination> PendingCardDestinations =>
            PendingCardChoice != null
                ? PendingCardChoice.LegalDestinations
                : Array.Empty<ReachableMovementDestination>();

        public IReadOnlyList<CardActionResolutionEvent> CardResolutionEvents => cardResolutionEvents;

        public IReadOnlyList<TurnCardCommitmentEvent> TurnCardCommitmentEvents =>
            turnCardCommitmentEvents;

        public IReadOnlyList<TurnResolutionEvent> TurnResolutionEvents => turnResolutionEvents;

        public IReadOnlyList<RoundResolutionEvent> RoundResolutionEvents => roundResolutionEvents;

        public IReadOnlyList<RoundTurnSlot> CompletedTurnSlots =>
            roundResolutionAuthority?.TurnSlots ?? Array.Empty<RoundTurnSlot>();

        public IReadOnlyList<CompletedRoundRecord> CompletedRounds =>
            roundResolutionAuthority?.CompletedRounds ?? Array.Empty<CompletedRoundRecord>();

        public EndOfRoundStage? VisibleEndOfRoundStage =>
            roundResolutionAuthority?.CurrentEndOfRoundStage;

        public int VisibleRoundIndex => roundResolutionAuthority?.RoundIndex ?? 0;

        public int VisibleTurnIndex => roundResolutionAuthority?.TurnIndex ?? 0;

        public RoundResolutionPhase VisibleRoundPhase =>
            roundResolutionAuthority?.Phase ?? RoundResolutionPhase.AwaitingCommitments;

        public bool IsVisibleRoundEnded =>
            roundResolutionAuthority != null &&
            roundResolutionAuthority.Phase == RoundResolutionPhase.EndOfRound;

        public bool IsVisibleTurnActive =>
            roundResolutionAuthority?.Phase == RoundResolutionPhase.ResolvingTurn &&
            roundResolutionAuthority.CurrentTurnAuthority != null &&
            roundResolutionAuthority.CurrentTurnAuthority.IsStarted &&
            !roundResolutionAuthority.CurrentTurnAuthority.IsEnded;

        public bool IsVisibleTurnEnded =>
            roundResolutionAuthority?.CurrentTurnAuthority != null &&
            roundResolutionAuthority.CurrentTurnAuthority.IsEnded;

        public SelectedInitiativeActor VisibleTurnActor =>
            roundResolutionAuthority?.CurrentTurnAuthority != null
                ? roundResolutionAuthority.CurrentTurnAuthority.SelectedActor
                : default;

        public bool IsTurnCardRevealComplete =>
            roundResolutionAuthority?.CurrentCommitmentAuthority != null &&
            roundResolutionAuthority.CurrentCommitmentAuthority.IsRevealed;

        public IReadOnlyList<HexCoordinate> CardMovementRoute => lastCardMovementRoute;

        public int CardRemainingMovementAllowance => IsCardMovementResolutionPending
            ? PendingCardChoice.RemainingAllowance
            : lastCardRemainingAllowance;

        public int LastCardMovementAllowance => lastCardMovementAllowance;

        public ActionInvocationContext LastCardInvocationContext { get; private set; }

        public CardTextApplicationPolicy LastCardTextApplicationPolicy { get; private set; }

        public CardActionResolutionRejectionReason LastCardRejectionReason { get; private set; }

        public TurnResolutionRejectionReason LastTurnRejectionReason { get; private set; }

        public RoundResolutionRejectionReason LastRoundRejectionReason { get; private set; }

        public AttackResolutionOutcome LastAttackOutcome { get; private set; }

        public string CardStatus => cardStatus;

        public bool CanUndoDirectorCommand => directorUndoSnapshots.Count > 0;

        public bool CanRestartDirectorScenario => directorUndoSnapshots.Count > 0;

        public int DirectorUndoEntryCount => directorUndoSnapshots.Count;

        public TieBreakerState CurrentTieBreakerState
        {
            get
            {
                InitializeCardDrivenMovementIfEnabled();
                return roundResolutionAuthority != null
                    ? roundResolutionAuthority.TieBreakerState
                    : placeholderConfiguration != null
                    ? placeholderConfiguration.TieBreakerState
                    : default;
            }
        }

        public string TieBreakerIndicatorText => FormatTieBreakerIndicator(CurrentTieBreakerState);

        public Color TieBreakerIndicatorColor => GetTieBreakerIndicatorColor(CurrentTieBreakerState);

        public Rect TieBreakerIndicatorRect => GetTieBreakerIndicatorRect();

        public void ConfigureCardDrivenMovement(bool enabled)
        {
            cardDrivenMovementEnabled = enabled;
            ResetCardDrivenMovementPresentation();
            InitializeCardDrivenMovementIfEnabled();
        }

        public void ConfigurePlaceholderTestMatch(
            PlaceholderOneVsOneTestConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (IsCardResolutionPending)
            {
                throw new InvalidOperationException(
                    "Cannot replace the placeholder match while a card resolution is pending.");
            }

            placeholderConfiguration = configuration;
            placeholderOneVsOneHandsInitialized = false;
            placeholderHands.Clear();
            placeholderParticipants = null;
            placeholderHand = null;
            turnCardCommitmentEvents.Clear();
            roundResolutionAuthority = null;
            roundResolutionEvents.Clear();
            turnResolutionEvents.Clear();
            ResetCardDrivenMovementPresentation();
            InitializeCardDrivenMovementIfEnabled();
        }

        public string CaptureAuthoritativeSnapshot()
        {
            InitializeCardDrivenMovementIfEnabled();
            if (roundResolutionAuthority == null || moveAuthority == null)
            {
                throw new InvalidOperationException("The visible authoritative match is not initialized.");
            }

            return AuthoritativeSnapshotSerializer.Serialize(roundResolutionAuthority, moveAuthority);
        }

        public void RestoreAuthoritativeSnapshot(string payload)
        {
            RestoreAuthoritativeSnapshot(payload, true);
        }

        public bool TryUndoDirectorCommand()
        {
            if (!CanUndoDirectorCommand)
            {
                return false;
            }

            var snapshotIndex = directorUndoSnapshots.Count - 1;
            var payload = directorUndoSnapshots[snapshotIndex];
            directorUndoSnapshots.RemoveAt(snapshotIndex);
            RestoreAuthoritativeSnapshot(payload, false);
            cardStatus = $"Undo restored round {VisibleRoundIndex}, turn {VisibleTurnIndex}/" +
                $"{RoundResolutionAuthority.TurnsPerRound}: {VisibleRoundPhase}.";
            return true;
        }

        public bool TryRestartDirectorScenario()
        {
            if (!CanRestartDirectorScenario || string.IsNullOrEmpty(directorInitialSnapshot))
            {
                return false;
            }

            var payload = directorInitialSnapshot;
            RestoreAuthoritativeSnapshot(payload, false);
            directorUndoSnapshots.Clear();
            cardStatus = $"Restarted initial scenario at round {VisibleRoundIndex}, turn {VisibleTurnIndex}/" +
                $"{RoundResolutionAuthority.TurnsPerRound}.";
            return true;
        }

        private void RestoreAuthoritativeSnapshot(string payload, bool resetDirectorHistory)
        {
            InitializeCardDrivenMovementIfEnabled();
            if (placeholderConfiguration == null)
            {
                throw new InvalidOperationException("The visible placeholder rules content is not initialized.");
            }

            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                payload,
                placeholderConfiguration.CardDefinitions);
            placeholderConfiguration = PlaceholderOneVsOneTestConfiguration.Restore(
                restored.Participants,
                placeholderConfiguration.CardDefinitions,
                restored.Cards,
                restored.Round.TieBreakerState);
            placeholderParticipants = restored.Participants;
            placeholderHands.Clear();
            placeholderHands[placeholderConfiguration.RedHand.Instances[0].OwnerId] = placeholderConfiguration.RedHand;
            placeholderHands[placeholderConfiguration.BlueHand.Instances[0].OwnerId] = placeholderConfiguration.BlueHand;
            placeholderOneVsOneHandsInitialized = true;
            moveAuthority = restored.Board;
            cardResolutionAuthority = restored.CardActions;
            roundResolutionAuthority = restored.Round;
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            cardResolutionEvents.Clear();
            turnCardCommitmentEvents.Clear();
            turnResolutionEvents.Clear();
            roundResolutionEvents.Clear();
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            LastAttackOutcome = AttackResolutionOutcome.None;

            if (resetDirectorHistory)
            {
                directorUndoSnapshots.Clear();
                directorInitialSnapshot = payload;
            }

            foreach (var view in new[] { heroView }.Concat(otherHeroViews ?? Array.Empty<PrimitiveHeroView>()).Where(view => view != null))
            {
                var occupancy = restored.Board.Occupancies.Single(value => value.HeroId.Equals(view.Occupancy.HeroId));
                view.Configure(occupancy, view.OuterRadius);
            }
            foreach (var view in obstacleViews ?? Array.Empty<GuardsDigital.Presentation.Board.PlaceholderObstacleView>())
            {
                if (view == null) continue;
                var occupancy = restored.Board.ObstacleOccupancies.Single(value => value.ObstacleId.Equals(view.Occupancy.ObstacleId));
                view.Configure(occupancy, view.OuterRadius);
            }

            placeholderHand = heroView != null && placeholderHands.TryGetValue(heroView.Occupancy.HeroId, out var activeHand)
                ? activeHand
                : null;
            ClearSelectionVisuals();
            if (IsVisibleTurnActive && VisibleTurnActor.IsDefined)
            {
                FocusVisibleTurnHero(VisibleTurnActor.ActorId);
                selectedCardInstanceId = VisibleTurnActor.CardInstanceId;
            }
            if (PendingCardChoice != null) RefreshCardChoiceHighlights(PendingCardChoice);
            if (PendingAttackTargetChoice != null) RefreshAttackTargetHighlights(PendingAttackTargetChoice);
            cardStatus = $"Restored round {VisibleRoundIndex}, turn {VisibleTurnIndex}/{RoundResolutionAuthority.TurnsPerRound}: " +
                $"{VisibleRoundPhase}.";
        }

        public bool TrySelectCard(CardInstanceId cardInstanceId)
        {
            InitializeCardDrivenMovementIfEnabled();
            if (!cardDrivenMovementEnabled || IsCardResolutionPending || placeholderHand == null)
            {
                return false;
            }

            if (!GetVisibleHandInstances(placeholderHand).Any(instance => instance.Id.Equals(cardInstanceId)))
            {
                return false;
            }

            var selectedInstance = placeholderHand.Instances.Single(instance =>
                instance.Id.Equals(cardInstanceId));
            if (TryGetConfiguredTeamSide(selectedInstance.OwnerId, out var selectedSide) &&
                GetTurnCardCommitmentStatus(selectedSide) !=
                TurnCardCommitmentStatus.AwaitingResponse)
            {
                return false;
            }

            selectedCardInstanceId = cardInstanceId;
            selectedActionOffer = null;
            LastAttackOutcome = AttackResolutionOutcome.None;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            cardStatus = "Card selected. Confirm it for the turn or choose an available test action.";
            ClearSelectionVisuals();
            return true;
        }

        public bool TrySelectDirectorCard(CardInstanceId cardInstanceId)
        {
            InitializeCardDrivenMovementIfEnabled();
            if (!cardDrivenMovementEnabled || IsCardResolutionPending || !cardInstanceId.IsDefined)
            {
                return false;
            }

            var hand = VisibleDirectorHands.SingleOrDefault(candidate =>
                candidate.Instances.Any(instance => instance.Id.Equals(cardInstanceId)));
            if (hand == null)
            {
                return false;
            }

            if (GetTurnCardCommitmentStatus(hand.TeamSide) !=
                TurnCardCommitmentStatus.AwaitingResponse)
            {
                return false;
            }

            var ownerHero = new[] { heroView }
                .Concat(otherHeroViews ?? Array.Empty<PrimitiveHeroView>())
                .FirstOrDefault(hero => hero != null && hero.Occupancy.HeroId.Equals(hand.OwnerId));
            if (ownerHero != null)
            {
                return TrySelectHero(ownerHero) && TrySelectCard(cardInstanceId);
            }

            placeholderHand = placeholderHands.TryGetValue(hand.OwnerId, out var ownerHand)
                ? ownerHand
                : null;
            return placeholderHand != null && TrySelectCard(cardInstanceId);
        }

        public TurnCardCommitmentStatus GetTurnCardCommitmentStatus(TeamSide side)
        {
            InitializeCardDrivenMovementIfEnabled();
            EnsurePlaceholderOneVsOneHands();
            EnsureRoundResolutionAuthority();
            return roundResolutionAuthority?.CurrentCommitmentAuthority != null
                ? roundResolutionAuthority.CurrentCommitmentAuthority.GetStatus(
                    GetSingleFixtureHeroId(side))
                : TurnCardCommitmentStatus.AwaitingResponse;
        }

        public string GetDirectorCommitmentLabel(TeamSide side)
        {
            var status = GetTurnCardCommitmentStatus(side);
            if (status == TurnCardCommitmentStatus.AwaitingResponse)
            {
                return "AWAITING CARD";
            }

            if (status == TurnCardCommitmentStatus.CardCommittedFacedown)
            {
                return "FACEDOWN COMMITTED";
            }

            if (status == TurnCardCommitmentStatus.Passed)
            {
                return "PASS - NO CARD";
            }

            var cardId = roundResolutionAuthority.CurrentCommitmentAuthority
                .GetRevealedCardInstanceId(GetSingleFixtureHeroId(side));
            var card = cardId.HasValue
                ? GetAllPlaceholderCardInstances().Single(instance => instance.Id.Equals(cardId.Value))
                : null;
            var definition = card != null
                ? placeholderConfiguration.CardDefinitions.Single(candidate =>
                    candidate.Reference.Equals(card.DefinitionReference))
                : null;
            return definition != null
                ? $"REVEALED: {definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}"
                : "REVEALED";
        }

        public bool CanPassTurnCard(TeamSide side)
        {
            InitializeCardDrivenMovementIfEnabled();
            EnsurePlaceholderOneVsOneHands();
            EnsureRoundResolutionAuthority();
            return roundResolutionAuthority?.Phase == RoundResolutionPhase.AwaitingCommitments &&
                roundResolutionAuthority.CurrentCommitmentAuthority.CanPass(
                    GetSingleFixtureHeroId(side));
        }

        public bool TryConfirmSelectedTurnCard()
        {
            InitializeCardDrivenMovementIfEnabled();
            if (!cardDrivenMovementEnabled ||
                IsCardResolutionPending ||
                !selectedCardInstanceId.HasValue ||
                roundResolutionAuthority == null)
            {
                return false;
            }

            var selectedCard = GetAllPlaceholderCardInstances().SingleOrDefault(instance =>
                instance.Id.Equals(selectedCardInstanceId.Value));
            if (selectedCard == null)
            {
                return false;
            }

            var side = placeholderParticipants.GetTeamForHero(selectedCard.OwnerId).Side;
            var undoSnapshot = CaptureDirectorCommandBoundary();
            var result = roundResolutionAuthority.TryCommitCard(selectedCard.OwnerId, selectedCard.Id);
            if (!result.Accepted)
            {
                RecordRoundRejection(result);
                cardStatus = $"Turn card rejected: {FormatRoundRejection(result)}.";
                return false;
            }

            RecordAcceptedDirectorCommand(undoSnapshot);
            RecordRoundEvents(result.Events);
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            if (roundResolutionAuthority.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                if (HasCompletePlaceholderRoundBindings())
                {
                    RefreshVisibleRoundState("Both turn cards revealed together.");
                }
                else
                {
                    cardStatus = "Both turn cards revealed together. Board bindings are unavailable for turn actions.";
                }
                return true;
            }

            cardStatus = $"Round {VisibleRoundIndex}, turn {VisibleTurnIndex}/{RoundResolutionAuthority.TurnsPerRound}: " +
                $"{FormatSide(side)} committed a facedown card. Awaiting {FormatSide(Opposite(side))}.";
            return true;
        }

        public bool TryPassTurnCard(TeamSide side)
        {
            InitializeCardDrivenMovementIfEnabled();
            if (!cardDrivenMovementEnabled || IsCardResolutionPending || roundResolutionAuthority == null)
            {
                return false;
            }

            var undoSnapshot = CaptureDirectorCommandBoundary();
            var result = roundResolutionAuthority.TryPass(GetSingleFixtureHeroId(side));
            if (!result.Accepted)
            {
                RecordRoundRejection(result);
                cardStatus = $"Pass rejected: {FormatRoundRejection(result)}.";
                return false;
            }

            RecordAcceptedDirectorCommand(undoSnapshot);
            RecordRoundEvents(result.Events);
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            if (roundResolutionAuthority.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                if (HasCompletePlaceholderRoundBindings())
                {
                    RefreshVisibleRoundState("Turn commitments complete.");
                }
                else
                {
                    cardStatus = "Turn commitments complete. Board bindings are unavailable for turn actions.";
                }
                return true;
            }

            cardStatus = $"Round {VisibleRoundIndex}, turn {VisibleTurnIndex}/{RoundResolutionAuthority.TurnsPerRound}: " +
                $"{FormatSide(side)} passed with no card. Awaiting {FormatSide(Opposite(side))}.";
            return true;
        }

        public bool TryPerformHold(CardInstanceId cardInstanceId)
        {
            InitializeCardDrivenMovementIfEnabled();
            if (!cardDrivenMovementEnabled || IsCardResolutionPending || roundResolutionAuthority == null)
            {
                return false;
            }

            var undoSnapshot = CaptureDirectorCommandBoundary();
            var result = roundResolutionAuthority.TryPerformHold(cardInstanceId);
            if (!result.Accepted)
            {
                RecordRoundRejection(result);
                cardStatus = $"Hold rejected: {FormatRoundRejection(result)}.";
                return false;
            }

            RecordAcceptedDirectorCommand(undoSnapshot);
            RecordRoundEvents(result.Events);
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
            RefreshVisibleRoundState("Hold completed.");
            return true;
        }

        public bool TryAdvanceEndOfRound()
        {
            InitializeCardDrivenMovementIfEnabled();
            if (!cardDrivenMovementEnabled || IsCardResolutionPending || roundResolutionAuthority == null)
            {
                return false;
            }

            var completedStage = roundResolutionAuthority.CurrentEndOfRoundStage;
            var undoSnapshot = CaptureDirectorCommandBoundary();
            var result = roundResolutionAuthority.TryAdvanceEndOfRound();
            if (!result.Accepted)
            {
                RecordRoundRejection(result);
                cardStatus = $"End-of-round progression rejected: {FormatRoundRejection(result)}.";
                return false;
            }

            RecordAcceptedDirectorCommand(undoSnapshot);
            RecordRoundEvents(result.Events);
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            var stageLabel = completedStage.HasValue
                ? FormatEndOfRoundStage(completedStage.Value)
                : "checkpoint";
            RefreshVisibleRoundState($"End of round: {stageLabel} processed.");
            return true;
        }

        public bool TryChooseTiedActor(TeamSide choosingSide, CardInstanceId cardInstanceId)
        {
            InitializeCardDrivenMovementIfEnabled();
            if (!cardDrivenMovementEnabled || IsCardResolutionPending || roundResolutionAuthority == null)
            {
                return false;
            }

            var undoSnapshot = CaptureDirectorCommandBoundary();
            var result = roundResolutionAuthority.TryChooseTiedActor(choosingSide, cardInstanceId);
            if (!result.Accepted)
            {
                RecordRoundRejection(result);
                cardStatus = $"Initiative choice rejected: {FormatRoundRejection(result)}.";
                return false;
            }

            RecordAcceptedDirectorCommand(undoSnapshot);
            RecordRoundEvents(result.Events);
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
            RefreshVisibleRoundState("Initiative tie resolved.");
            return true;
        }

        public bool TrySelectAction(ActionDefinitionId actionDefinitionId)
        {
            InitializeCardDrivenMovementIfEnabled();
            if (!cardDrivenMovementEnabled || IsCardResolutionPending || !selectedCardInstanceId.HasValue)
            {
                return false;
            }

            var matchingOffers = SelectedCardOffers
                .Where(offer => offer.ActionDefinitionId.Equals(actionDefinitionId))
                .ToArray();
            if (matchingOffers.Length != 1)
            {
                return false;
            }

            if (IsVisibleTurnActive)
            {
                var undoSnapshot = CaptureDirectorCommandBoundary();
                var accepted = GetActionDefinition(matchingOffers[0]).Kind == ActionKind.Attack
                    ? StartVisibleTurnAttack(matchingOffers[0])
                    : StartVisibleTurnMovement(matchingOffers[0]);
                if (accepted)
                {
                    RecordAcceptedDirectorCommand(undoSnapshot);
                }

                return accepted;
            }

            if (cardResolutionAuthority == null)
            {
                RebuildMoveAuthority();
                cardResolutionAuthority = new CardActionResolutionAuthority(moveAuthority);
            }
            selectedActionOffer = matchingOffers[0];
            cardResolutionSequence++;
            cardResolutionEvents.Clear();
            lastCardMovementRoute = Array.Empty<HexCoordinate>();
            var actionDefinition = GetActionDefinition(matchingOffers[0]);
            if (actionDefinition.Kind == ActionKind.Attack)
            {
                return StartVisibleAttack(matchingOffers[0]);
            }

            var start = cardResolutionAuthority.StartMovementResolution(
                new ActionResolutionId($"visible-movement-{cardResolutionSequence}"),
                matchingOffers[0],
                placeholderHand.Definitions,
                placeholderHand.Instances);

            if (!start.Accepted)
            {
                LastCardRejectionReason = start.RejectionReason;
                cardStatus = $"Action rejected: {start.RejectionReason}";
                if (roundResolutionAuthority == null)
                {
                    cardResolutionAuthority = null;
                }
                selectedActionOffer = null;
                return false;
            }

            cardResolutionEvents.AddRange(start.Events);
            lastCardMovementAllowance = start.PendingResolution.MovementAllowance;
            lastCardRemainingAllowance = start.PendingResolution.RemainingAllowance;
            LastCardInvocationContext = start.PendingResolution.InvocationContext;
            LastCardTextApplicationPolicy = GetActionDefinition(matchingOffers[0]).DefaultPolicy.CardTextApplication;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            lastCardMovementRoute = new[] { start.PendingChoice.Origin };
            cardStatus = $"{FormatContext(start.PendingResolution.InvocationContext)} Movement " +
                $"{start.PendingResolution.MovementAllowance}: choose a highlighted destination or Stop.";
            RefreshCardChoiceHighlights(start.PendingChoice);
            return true;
        }

        public bool TrySubmitAttackTarget(HeroId targetHeroId)
        {
            if (!IsAttackTargetPending || placeholderHand == null || placeholderParticipants == null)
            {
                return false;
            }

            var command = new AttackTargetChoiceCommand(
                PendingAttackTargetChoice.ResolutionId,
                PendingAttackTargetChoice.ChoiceId,
                targetHeroId);
            if (IsVisibleTurnActive)
            {
                var undoSnapshot = CaptureDirectorCommandBoundary();
                var roundResult = roundResolutionAuthority.TryAnswerAttackTarget(command);
                if (!roundResult.Accepted)
                {
                    RecordRoundRejection(roundResult);
                    cardStatus = $"Turn Attack target rejected: {FormatRoundRejection(roundResult)}. " +
                        "Choose a legal enemy hero.";
                    return false;
                }

                RecordAcceptedDirectorCommand(undoSnapshot);
                RecordRoundEvents(roundResult.Events);
                LastRoundRejectionReason = RoundResolutionRejectionReason.None;
                LastTurnRejectionReason = TurnResolutionRejectionReason.None;
                LastCardRejectionReason = CardActionResolutionRejectionReason.None;
                ClearSelectionVisuals();
                cardStatus = $"Attack {PendingAttackResolution.AttackTotal} targets " +
                    $"{FormatSide(placeholderParticipants.GetTeamForHero(targetHeroId).Side)} hero. " +
                    "Defense response pending: discard one listed card or decline.";
                return true;
            }

            var result = cardResolutionAuthority.AnswerAttackTarget(
                command,
                GetAllPlaceholderCardDefinitions(),
                GetAllPlaceholderCardInstances(),
                placeholderParticipants);
            if (!result.Accepted)
            {
                LastCardRejectionReason = result.RejectionReason;
                cardStatus = $"Attack target rejected: {result.RejectionReason}. Choose a legal enemy hero.";
                return false;
            }

            cardResolutionEvents.AddRange(result.Events);
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            ClearSelectionVisuals();
            var targetView = new[] { heroView }
                .Concat(otherHeroViews ?? Array.Empty<PrimitiveHeroView>())
                .FirstOrDefault(view => view != null && view.Occupancy.HeroId.Equals(targetHeroId));
            var targetLabel = targetView != null
                ? $"{targetView.TeamSide} hero"
                : targetHeroId.ToString();
            cardStatus = $"Attack {result.PendingResolution.AttackTotal} targets {targetLabel}. " +
                "Defense response pending: discard one listed card or decline.";
            return true;
        }

        public bool TrySubmitDefenseCard(CardInstanceId cardInstanceId)
        {
            if (!IsDefenseResponsePending)
            {
                return false;
            }

            return SubmitDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                PendingDefenseChoice.ResolutionId,
                PendingDefenseChoice.ChoiceId,
                cardInstanceId));
        }

        public bool TryDeclineDefense()
        {
            if (!IsDefenseResponsePending || !PendingDefenseChoice.CanDecline)
            {
                return false;
            }

            return SubmitDefenseResponse(DefenseResponseChoiceCommand.Decline(
                PendingDefenseChoice.ResolutionId,
                PendingDefenseChoice.ChoiceId));
        }

        public bool TrySubmitCardDestination(HexCoordinate destination)
        {
            if (!IsCardMovementResolutionPending)
            {
                return false;
            }

            var candidates = PendingCardChoice.LegalDestinations
                .Where(choice => choice.Destination.Equals(destination))
                .ToArray();
            if (candidates.Length != 1)
            {
                return false;
            }

            return SubmitCardMovementChoice(MovementChoiceCommand.SelectDestination(
                PendingCardChoice.ResolutionId,
                PendingCardChoice.ChoiceId,
                destination));
        }

        public bool TryStopCardMovement()
        {
            if (!IsCardMovementResolutionPending || !PendingCardChoice.CanStop)
            {
                return false;
            }

            return SubmitCardMovementChoice(MovementChoiceCommand.Stop(
                PendingCardChoice.ResolutionId,
                PendingCardChoice.ChoiceId));
        }

        public bool SubmitCardMovementChoice(MovementChoiceCommand command)
        {
            if (!IsCardMovementResolutionPending)
            {
                return false;
            }

            if (IsVisibleTurnActive)
            {
                var undoSnapshot = CaptureDirectorCommandBoundary();
                var accepted = SubmitVisibleTurnMovementChoice(command);
                if (accepted)
                {
                    RecordAcceptedDirectorCommand(undoSnapshot);
                }

                return accepted;
            }

            var choiceBeforeSubmission = PendingCardChoice;
            ReachableMovementDestination? selectedDestination = null;
            if (command.ChoiceKind == MovementChoiceKind.Destination)
            {
                var matchingDestinations = choiceBeforeSubmission.LegalDestinations
                    .Where(candidate => candidate.Destination.Equals(command.Destination))
                    .ToArray();
                if (matchingDestinations.Length == 1)
                {
                    selectedDestination = matchingDestinations[0];
                }
            }
            var result = cardResolutionAuthority.AnswerMovementChoice(command);
            if (!result.Accepted)
            {
                LastCardRejectionReason = result.RejectionReason;
                cardStatus = $"Choice rejected: {result.RejectionReason}. Choose again.";
                return false;
            }

            cardResolutionEvents.AddRange(result.Events);
            lastCardRemainingAllowance = result.PendingResolution != null
                ? result.PendingResolution.RemainingAllowance
                : selectedDestination.HasValue
                    ? choiceBeforeSubmission.RemainingAllowance - selectedDestination.Value.Cost
                    : choiceBeforeSubmission.RemainingAllowance;
            if (selectedDestination.HasValue)
            {
                lastCardMovementRoute = lastCardMovementRoute
                    .Concat(selectedDestination.Value.Route.Skip(1))
                    .ToArray();
            }

            var completedCardName = GetSelectedCardDefinition()?.DisplayName ?? "Test card";
            var completedContext = selectedActionOffer.HasValue
                ? FormatContext(selectedActionOffer.Value.InvocationContext)
                : "Movement";

            var updatedOccupancy = GetHeroOccupancyFromAuthority();
            heroView.Configure(updatedOccupancy, heroView.OuterRadius);
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            if (!result.IsComplete)
            {
                cardStatus = $"{completedCardName}: {completedContext} Movement has " +
                    $"{result.PendingChoice.RemainingAllowance} remaining. Choose another destination or Stop.";
                RefreshCardChoiceHighlights(result.PendingChoice);
                return true;
            }

            var completedCost = lastCardMovementAllowance - lastCardRemainingAllowance;
            ClearSelectionVisuals();
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            if (roundResolutionAuthority == null)
            {
                cardResolutionAuthority = null;
            }
            cardStatus = $"{completedCardName}: {completedContext} Movement completed ({completedCost} space" +
                (completedCost == 1 ? "). Select another card." : "s). Select another card.");
            return true;
        }

        public bool BlocksPointerAt(Vector2 screenPosition)
        {
            if (!cardDrivenMovementEnabled)
            {
                return false;
            }

            var guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return GetCardPanelRect().Contains(guiPosition) ||
                GetTieBreakerIndicatorRect().Contains(guiPosition);
        }

        private void InitializeCardDrivenMovementIfEnabled()
        {
            if (!cardDrivenMovementEnabled)
            {
                return;
            }

            EnsureBindings();
            if (heroView == null)
            {
                return;
            }

            var ownerId = heroView.Occupancy.HeroId;
            if (placeholderHand == null ||
                placeholderHand.Instances.Any(instance => !instance.OwnerId.Equals(ownerId)))
            {
                placeholderHand = GetOrCreatePlaceholderHand(ownerId);
            }

            EnsureRoundResolutionAuthority();

            if (string.IsNullOrWhiteSpace(cardStatus))
            {
                cardStatus = "Select a test card.";
            }
        }

        private void ResetCardDrivenMovementPresentation()
        {
            placeholderHand = null;
            cardResolutionAuthority = null;
            roundResolutionAuthority = null;
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            cardResolutionEvents.Clear();
            turnCardCommitmentEvents.Clear();
            turnResolutionEvents.Clear();
            roundResolutionEvents.Clear();
            directorUndoSnapshots.Clear();
            directorInitialSnapshot = null;
            lastCardMovementRoute = Array.Empty<HexCoordinate>();
            lastCardMovementAllowance = 0;
            lastCardRemainingAllowance = 0;
            LastCardInvocationContext = ActionInvocationContext.None;
            LastCardTextApplicationPolicy = CardTextApplicationPolicy.None;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            LastAttackOutcome = AttackResolutionOutcome.None;
            cardStatus = cardDrivenMovementEnabled ? "Select a test card." : string.Empty;
        }

        private PlaceholderMovementHand GetOrCreatePlaceholderHand(HeroId ownerId)
        {
            EnsurePlaceholderOneVsOneHands();
            if (placeholderHands.TryGetValue(ownerId, out var existingHand))
            {
                return existingHand;
            }

            var hand = PlaceholderMovementHand.Create(ownerId);
            placeholderHands[ownerId] = hand;
            return hand;
        }

        private void EnsurePlaceholderOneVsOneHands()
        {
            if (placeholderOneVsOneHandsInitialized)
            {
                return;
            }

            var configuration = placeholderConfiguration ??
                PlaceholderOneVsOneTestConfiguration.Create();
            placeholderConfiguration = configuration;
            placeholderParticipants = configuration.Participants;
            placeholderHands[configuration.RedHand.Instances[0].OwnerId] = configuration.RedHand;
            placeholderHands[configuration.BlueHand.Instances[0].OwnerId] = configuration.BlueHand;
            placeholderOneVsOneHandsInitialized = true;
        }

        private void EnsureRoundResolutionAuthority()
        {
            if (roundResolutionAuthority != null ||
                placeholderConfiguration == null ||
                placeholderParticipants == null)
            {
                return;
            }

            if (heroView != null)
            {
                var configuredOwners = new[]
                {
                    placeholderConfiguration.RedHand.Instances[0].OwnerId,
                    placeholderConfiguration.BlueHand.Instances[0].OwnerId
                };
                var boundOwners = new[] { heroView }
                    .Concat(otherHeroViews ?? Array.Empty<PrimitiveHeroView>())
                    .Where(view => view != null)
                    .Select(view => view.Occupancy.HeroId)
                    .ToArray();
                if (boundOwners.Any(owner => !configuredOwners.Contains(owner)))
                {
                    return;
                }
            }

            if (heroView != null && tileViews != null)
            {
                RebuildMoveAuthority();
            }

            cardResolutionAuthority = moveAuthority != null
                ? new CardActionResolutionAuthority(moveAuthority)
                : null;
            roundResolutionAuthority = new RoundResolutionAuthority(
                placeholderConfiguration.CardDefinitions,
                GetAllPlaceholderCardInstances(),
                placeholderParticipants,
                placeholderConfiguration.TieBreakerState,
                cardActions: cardResolutionAuthority);
            cardStatus = $"Round {VisibleRoundIndex}, turn {VisibleTurnIndex}/{RoundResolutionAuthority.TurnsPerRound}: " +
                "awaiting Red and Blue commitments.";
        }

        private bool HasCompletePlaceholderRoundBindings()
        {
            if (placeholderConfiguration == null)
            {
                return false;
            }

            var boundHeroIds = new[] { heroView }
                .Concat(otherHeroViews ?? Array.Empty<PrimitiveHeroView>())
                .Where(view => view != null)
                .Select(view => view.Occupancy.HeroId)
                .ToArray();
            var redOwner = placeholderConfiguration.RedHand.Instances[0].OwnerId;
            var blueOwner = placeholderConfiguration.BlueHand.Instances[0].OwnerId;
            return boundHeroIds.Contains(redOwner) && boundHeroIds.Contains(blueOwner);
        }

        private bool StartVisibleTurnMovement(ActionOffer offer)
        {
            selectedActionOffer = offer;
            cardResolutionSequence++;
            cardResolutionEvents.Clear();
            lastCardMovementRoute = Array.Empty<HexCoordinate>();
            var result = roundResolutionAuthority.TryStartMovement(
                new ActionResolutionId($"visible-turn-movement-{cardResolutionSequence}"),
                offer);
            if (!result.Accepted)
            {
                RecordRoundRejection(result);
                cardStatus = $"Turn action rejected: {FormatRoundRejection(result)}.";
                selectedActionOffer = null;
                return false;
            }

            RecordRoundEvents(result.Events);
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            var turnAuthority = roundResolutionAuthority.CurrentTurnAuthority;
            var pending = turnAuthority.PendingMovementResolution;
            var choice = turnAuthority.PendingMovementChoice;
            lastCardMovementAllowance = pending.MovementAllowance;
            lastCardRemainingAllowance = pending.RemainingAllowance;
            LastCardInvocationContext = pending.InvocationContext;
            LastCardTextApplicationPolicy = GetActionDefinition(offer).DefaultPolicy.CardTextApplication;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
            lastCardMovementRoute = new[] { choice.Origin };
            cardStatus = $"{FormatContext(pending.InvocationContext)} Movement " +
                $"{pending.MovementAllowance}: choose a highlighted destination or Stop.";
            RefreshCardChoiceHighlights(choice);
            return true;
        }

        private bool SubmitVisibleTurnMovementChoice(MovementChoiceCommand command)
        {
            var choiceBeforeSubmission = PendingCardChoice;
            ReachableMovementDestination? selectedDestination = null;
            if (command.ChoiceKind == MovementChoiceKind.Destination)
            {
                var matchingDestinations = choiceBeforeSubmission.LegalDestinations
                    .Where(candidate => candidate.Destination.Equals(command.Destination))
                    .ToArray();
                if (matchingDestinations.Length == 1)
                {
                    selectedDestination = matchingDestinations[0];
                }
            }

            var completedCardName = GetSelectedCardDefinition()?.DisplayName ?? "Test card";
            var completedContext = selectedActionOffer.HasValue
                ? FormatContext(selectedActionOffer.Value.InvocationContext)
                : "Movement";
            var result = roundResolutionAuthority.TryAnswerMovementChoice(command);
            if (!result.Accepted)
            {
                RecordRoundRejection(result);
                cardStatus = $"Turn choice rejected: {FormatRoundRejection(result)}. Choose again.";
                return false;
            }

            RecordRoundEvents(result.Events);
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            var turnAuthority = roundResolutionAuthority.CurrentTurnAuthority;
            lastCardRemainingAllowance = turnAuthority?.PendingMovementResolution != null
                ? turnAuthority.PendingMovementResolution.RemainingAllowance
                : selectedDestination.HasValue
                    ? choiceBeforeSubmission.RemainingAllowance - selectedDestination.Value.Cost
                    : choiceBeforeSubmission.RemainingAllowance;
            if (selectedDestination.HasValue)
            {
                lastCardMovementRoute = lastCardMovementRoute
                    .Concat(selectedDestination.Value.Route.Skip(1))
                    .ToArray();
            }

            var updatedOccupancy = GetHeroOccupancyFromAuthority();
            heroView.Configure(updatedOccupancy, heroView.OuterRadius);
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
            if (turnAuthority?.PendingMovementChoice != null)
            {
                cardStatus = $"{completedCardName}: {completedContext} Movement has " +
                    $"{turnAuthority.PendingMovementChoice.RemainingAllowance} remaining. " +
                    "Choose another destination or Stop.";
                RefreshCardChoiceHighlights(turnAuthority.PendingMovementChoice);
                return true;
            }

            var completedCost = lastCardMovementAllowance - lastCardRemainingAllowance;
            ClearSelectionVisuals();
            selectedActionOffer = null;
            RefreshVisibleRoundState(
                $"{completedCardName}: {completedContext} Movement completed ({completedCost} space" +
                (completedCost == 1 ? ")." : "s)."));
            return true;
        }

        private void RefreshVisibleRoundState(string statusPrefix)
        {
            ClearSelectionVisuals();
            selectedActionOffer = null;
            if (roundResolutionAuthority.Phase == RoundResolutionPhase.EndOfRound)
            {
                selectedCardInstanceId = null;
                cardStatus = $"{statusPrefix} ROUND {VisibleRoundIndex} COMPLETE - END OF ROUND. " +
                    $"Next checkpoint: {FormatEndOfRoundStage(roundResolutionAuthority.CurrentEndOfRoundStage.Value)}.";
                return;
            }

            if (roundResolutionAuthority.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                selectedCardInstanceId = null;
                cardStatus = $"{statusPrefix} Round {VisibleRoundIndex}, turn {VisibleTurnIndex}/" +
                    $"{RoundResolutionAuthority.TurnsPerRound}: awaiting Red and Blue commitments.";
                return;
            }

            var turnAuthority = roundResolutionAuthority.CurrentTurnAuthority;
            if (!turnAuthority.SelectedActor.IsDefined)
            {
                selectedCardInstanceId = null;
                cardStatus = $"{statusPrefix} Awaiting the Initiative tie choice.";
                return;
            }

            var actor = turnAuthority.SelectedActor;
            FocusVisibleTurnHero(actor.ActorId);
            selectedCardInstanceId = actor.CardInstanceId;
            cardStatus = $"{statusPrefix} Round {VisibleRoundIndex}, turn {VisibleTurnIndex}/" +
                $"{RoundResolutionAuthority.TurnsPerRound}: " +
                $"{FormatSide(placeholderParticipants.GetTeamForHero(actor.ActorId).Side)} " +
                $"acts at Initiative {actor.InitiativeTotal}. Choose Hold, Movement, or Attack.";
        }

        private void FocusVisibleTurnHero(HeroId actorId)
        {
            var boundHeroes = new[] { heroView }
                .Concat(otherHeroViews ?? Array.Empty<PrimitiveHeroView>())
                .Where(view => view != null)
                .Distinct()
                .ToArray();
            var actorView = boundHeroes.Single(view => view.Occupancy.HeroId.Equals(actorId));
            heroView = actorView;
            otherHeroViews = boundHeroes.Where(view => view != actorView).ToArray();
            placeholderHand = placeholderHands[actorId];
        }

        private bool StartVisibleAttack(ActionOffer offer)
        {
            EnsurePlaceholderOneVsOneHands();
            var start = cardResolutionAuthority.StartAttackResolution(
                new ActionResolutionId($"visible-attack-{cardResolutionSequence}"),
                offer,
                GetAllPlaceholderCardDefinitions(),
                GetAllPlaceholderCardInstances(),
                placeholderParticipants);
            if (!start.Accepted)
            {
                LastCardRejectionReason = start.RejectionReason;
                cardStatus = $"Attack rejected: {start.RejectionReason}";
                if (roundResolutionAuthority == null)
                {
                    cardResolutionAuthority = null;
                }
                selectedActionOffer = null;
                return false;
            }

            cardResolutionEvents.AddRange(start.Events);
            LastCardInvocationContext = start.PendingResolution.InvocationContext;
            LastCardTextApplicationPolicy = GetActionDefinition(offer).DefaultPolicy.CardTextApplication;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            var attackTotal = PrintedValueQuery.CalculatePrintedValue(
                offer,
                placeholderHand.Definitions,
                PrintedValueChannel.Attack).Total;
            cardStatus = offer.AttackRange == AttackRangeKind.Ranged
                ? $"{FormatContext(offer.InvocationContext)} Ranged Attack {attackTotal} " +
                    $"Range {GetPrintedRangeTotal(offer)}: click an enemy hero within range."
                : $"{FormatContext(offer.InvocationContext)} Attack {attackTotal}: " +
                    "click the adjacent enemy hero.";
            RefreshAttackTargetHighlights(start.PendingTargetChoice);
            return true;
        }

        private bool StartVisibleTurnAttack(ActionOffer offer)
        {
            selectedActionOffer = offer;
            cardResolutionSequence++;
            cardResolutionEvents.Clear();
            var result = roundResolutionAuthority.TryStartAttack(
                new ActionResolutionId($"visible-turn-attack-{cardResolutionSequence}"),
                offer);
            if (!result.Accepted)
            {
                RecordRoundRejection(result);
                cardStatus = $"Turn Attack rejected: {FormatRoundRejection(result)}.";
                selectedActionOffer = null;
                return false;
            }

            RecordRoundEvents(result.Events);
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            LastCardInvocationContext = offer.InvocationContext;
            LastCardTextApplicationPolicy = GetActionDefinition(offer).DefaultPolicy.CardTextApplication;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
            var attackTotal = PrintedValueQuery.CalculatePrintedValue(
                offer,
                placeholderHand.Definitions,
                PrintedValueChannel.Attack).Total;
            cardStatus = offer.AttackRange == AttackRangeKind.Ranged
                ? $"{FormatContext(offer.InvocationContext)} Ranged Attack {attackTotal} " +
                    $"Range {GetPrintedRangeTotal(offer)}: click an enemy hero within range."
                : $"{FormatContext(offer.InvocationContext)} Attack {attackTotal}: " +
                    "click the adjacent enemy hero.";
            RefreshAttackTargetHighlights(PendingAttackTargetChoice);
            return true;
        }

        private bool SubmitDefenseResponse(DefenseResponseChoiceCommand command)
        {
            var selectedCardId = command.ChoiceKind == DefenseResponseChoiceKind.SelectCards &&
                command.SelectedCardInstanceIds.Count == 1
                    ? (CardInstanceId?)command.SelectedCardInstanceIds[0]
                    : null;
            if (IsVisibleTurnActive)
            {
                var undoSnapshot = CaptureDirectorCommandBoundary();
                var accepted = SubmitVisibleTurnDefenseResponse(command, selectedCardId);
                if (accepted)
                {
                    RecordAcceptedDirectorCommand(undoSnapshot);
                }

                return accepted;
            }

            var result = cardResolutionAuthority.AnswerDefenseResponse(
                command,
                GetAllPlaceholderCardDefinitions(),
                GetAllPlaceholderCardInstances(),
                placeholderParticipants);
            if (!result.Accepted)
            {
                LastCardRejectionReason = result.RejectionReason;
                cardStatus = $"Defense response rejected: {result.RejectionReason}. Choose again.";
                return false;
            }

            cardResolutionEvents.AddRange(result.Events);
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            var comparisonResolution = result.PendingResolution;
            var defenseLabel = "Defense declined";
            if (result.PendingResolution.HasDefenseTotal)
            {
                var definition = selectedCardId.HasValue
                    ? GetAllPlaceholderCardInstances()
                        .Where(instance => instance.Id.Equals(selectedCardId.Value))
                        .Select(instance => GetAllPlaceholderCardDefinitions().Single(candidate =>
                            candidate.Reference.Equals(instance.DefinitionReference)))
                        .SingleOrDefault()
                    : null;
                defenseLabel = $"{definition?.DisplayName ?? "Defense card"} discarded for Defense " +
                    result.PendingResolution.DefenseTotal;
            }

            var outcomeResult = cardResolutionAuthority.ResolveAttackOutcome(
                GetAllPlaceholderCardDefinitions(),
                GetAllPlaceholderCardInstances(),
                placeholderParticipants);
            if (!outcomeResult.Accepted)
            {
                LastCardRejectionReason = outcomeResult.RejectionReason;
                cardStatus = $"Attack comparison rejected: {outcomeResult.RejectionReason}.";
                return false;
            }

            cardResolutionEvents.AddRange(outcomeResult.Events);
            LastAttackOutcome = outcomeResult.Outcome;
            var outcomeLabel = outcomeResult.Outcome == AttackResolutionOutcome.SuccessfullyDefended
                ? "successfully defended"
                : "attack successful";
            cardStatus = comparisonResolution.HasDefenseTotal
                ? $"{defenseLabel}. Attack {comparisonResolution.AttackTotal} vs Defense " +
                    $"{comparisonResolution.DefenseTotal}: {outcomeLabel}."
                : $"{defenseLabel} against Attack {comparisonResolution.AttackTotal}: {outcomeLabel}.";
            ClearSelectionVisuals();
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            if (roundResolutionAuthority == null)
            {
                cardResolutionAuthority = null;
            }
            return true;
        }

        private bool SubmitVisibleTurnDefenseResponse(
            DefenseResponseChoiceCommand command,
            CardInstanceId? selectedDefenseCardId)
        {
            var comparisonResolution = PendingAttackResolution;
            var selectedDefenseOffer = selectedDefenseCardId.HasValue
                ? PendingDefenseChoice.LegalDefenseOffers
                    .Where(offer => offer.SourceCardInstanceId.Equals(selectedDefenseCardId.Value))
                    .Cast<ActionOffer?>()
                    .SingleOrDefault()
                : null;

            var result = roundResolutionAuthority.TryAnswerDefenseResponse(command);
            if (!result.Accepted)
            {
                RecordRoundRejection(result);
                cardStatus = $"Turn Defense response rejected: {FormatRoundRejection(result)}. Choose again.";
                return false;
            }

            RecordRoundEvents(result.Events);
            LastRoundRejectionReason = RoundResolutionRejectionReason.None;
            var attackEvents = result.Events
                .Where(ruleEvent => ruleEvent.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    ruleEvent.TurnEvent.Kind == TurnResolutionEventKind.AttackActionProgressed)
                .Select(ruleEvent => ruleEvent.TurnEvent.CardActionEvent)
                .ToArray();
            LastAttackOutcome = attackEvents.Last(ruleEvent =>
                ruleEvent.Kind == CardActionResolutionEventKind.AttackResolutionCompleted).AttackOutcome;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
            var outcomeLabel = LastAttackOutcome == AttackResolutionOutcome.SuccessfullyDefended
                ? "successfully defended"
                : "attack successful";
            var defenseLabel = "Defense declined";
            if (selectedDefenseOffer.HasValue)
            {
                var defenseDefinition = GetAllPlaceholderCardDefinitions().Single(definition =>
                    definition.Reference.Equals(selectedDefenseOffer.Value.SourceDefinitionReference));
                var defenseTotal = PrintedValueQuery.CalculatePrintedValue(
                    selectedDefenseOffer.Value,
                    GetAllPlaceholderCardDefinitions(),
                    PrintedValueChannel.Defense).Total;
                defenseLabel = $"{defenseDefinition.DisplayName} discarded for Defense {defenseTotal}";
            }
            var resultLabel = selectedDefenseCardId.HasValue
                ? $"{defenseLabel}. Attack {comparisonResolution.AttackTotal} vs Defense " +
                    $"{attackEvents.Last(ruleEvent => ruleEvent.Kind == CardActionResolutionEventKind.DefenseResponseCompleted).DefenseTotal}: " +
                    $"{outcomeLabel}."
                : $"{defenseLabel} against Attack {comparisonResolution.AttackTotal}: {outcomeLabel}.";
            RefreshVisibleRoundState(resultLabel);
            return true;
        }

        private void RecordRoundEvents(IEnumerable<RoundResolutionEvent> events)
        {
            foreach (var roundEvent in events)
            {
                roundResolutionEvents.Add(roundEvent);
                if (roundEvent.Kind == RoundResolutionEventKind.CommitmentProgressed)
                {
                    turnCardCommitmentEvents.Add(roundEvent.CommitmentEvent);
                    continue;
                }

                if (roundEvent.Kind != RoundResolutionEventKind.TurnResolutionProgressed)
                {
                    continue;
                }

                var turnEvent = roundEvent.TurnEvent;
                turnResolutionEvents.Add(turnEvent);
                if (turnEvent.Kind == TurnResolutionEventKind.MovementActionProgressed ||
                    turnEvent.Kind == TurnResolutionEventKind.AttackActionProgressed)
                {
                    cardResolutionEvents.Add(turnEvent.CardActionEvent);
                }
            }
        }

        private string CaptureDirectorCommandBoundary()
        {
            return roundResolutionAuthority != null && moveAuthority != null
                ? AuthoritativeSnapshotSerializer.Serialize(roundResolutionAuthority, moveAuthority)
                : null;
        }

        private void RecordAcceptedDirectorCommand(string snapshot)
        {
            if (string.IsNullOrEmpty(snapshot))
            {
                return;
            }

            if (string.IsNullOrEmpty(directorInitialSnapshot))
            {
                directorInitialSnapshot = snapshot;
            }

            directorUndoSnapshots.Add(snapshot);
        }

        private void RecordRoundRejection(RoundResolutionResult result)
        {
            LastRoundRejectionReason = result.RejectionReason;
            if (result.TurnRejectionReason != TurnResolutionRejectionReason.None)
            {
                LastTurnRejectionReason = result.TurnRejectionReason;
            }
        }

        private static string FormatRoundRejection(RoundResolutionResult result)
        {
            if (result.CommitmentRejectionReason != TurnCardCommitmentRejectionReason.None)
            {
                return $"{result.RejectionReason} ({result.CommitmentRejectionReason})";
            }

            if (result.TurnRejectionReason != TurnResolutionRejectionReason.None)
            {
                return $"{result.RejectionReason} ({result.TurnRejectionReason})";
            }

            return result.RejectionReason.ToString();
        }

        private static string FormatEndOfRoundStage(EndOfRoundStage stage)
        {
            switch (stage)
            {
                case EndOfRoundStage.MinionBattle:
                    return "Minion Battle";
                case EndOfRoundStage.RemoveTokensAndReturnMarkers:
                    return "remove tokens / return markers";
                case EndOfRoundStage.ResolveRequestedLanePush:
                    return "resolve requested Lane Push";
                case EndOfRoundStage.RetrieveCardsAndCancelActiveEffects:
                    return "retrieve cards / cancel Active effects";
                case EndOfRoundStage.LevelUpAndUpgrade:
                    return "Level Up / upgrade";
                case EndOfRoundStage.CollectPityCoins:
                    return "collect Pity Coins";
                default:
                    return stage.ToString();
            }
        }

        private IReadOnlyList<CardInstance> GetAllPlaceholderCardInstances()
        {
            EnsurePlaceholderOneVsOneHands();
            return placeholderHands.Values
                .SelectMany(hand => hand.Instances)
                .OrderBy(instance => instance.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private IReadOnlyList<CardDefinition> GetAllPlaceholderCardDefinitions()
        {
            EnsurePlaceholderOneVsOneHands();
            return placeholderHands.Values
                .SelectMany(hand => hand.Definitions)
                .GroupBy(definition => definition.Reference)
                .Select(group => group.First())
                .OrderBy(definition => definition.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlyList<CardInstance> GetVisibleHandInstances(PlaceholderMovementHand hand)
        {
            return hand.Instances
                .Where(instance => instance.State.Location == CardLocation.Hand)
                .ToArray();
        }

        private void RefreshCardChoiceHighlights(PendingMovementChoice choice)
        {
            ClearTileHighlights();
            highlightedCoordinates.Clear();
            reachableDestinations.Clear();

            foreach (var candidate in choice.LegalDestinations)
            {
                highlightedCoordinates.Add(candidate.Destination);
                reachableDestinations[candidate.Destination] = candidate;
            }

            var destinations = new HashSet<HexCoordinate>(highlightedCoordinates);
            var material = EnsureHighlightMaterial();
            foreach (var tile in tileViews.Where(tile => tile != null))
            {
                tile.SetHighlighted(destinations.Contains(tile.Coordinate), material);
            }

            IsHeroSelected = true;
        }

        private void RefreshAttackTargetHighlights(PendingAttackTargetChoice choice)
        {
            ClearTileHighlights();
            highlightedCoordinates.Clear();
            reachableDestinations.Clear();

            var legalTargetIds = new HashSet<HeroId>(choice.LegalTargetHeroIds);
            var targetCoordinates = new[] { heroView }
                .Concat(otherHeroViews ?? Array.Empty<PrimitiveHeroView>())
                .Where(view => view != null && legalTargetIds.Contains(view.Occupancy.HeroId))
                .Select(view => view.Coordinate)
                .ToArray();
            highlightedCoordinates.AddRange(targetCoordinates);

            var coordinates = new HashSet<HexCoordinate>(targetCoordinates);
            var material = EnsureHighlightMaterial();
            foreach (var tile in tileViews.Where(tile => tile != null))
            {
                tile.SetHighlighted(coordinates.Contains(tile.Coordinate), material);
            }

            IsHeroSelected = true;
        }

        private CardDefinition GetSelectedCardDefinition()
        {
            if (!selectedCardInstanceId.HasValue || placeholderHand == null)
            {
                return null;
            }

            var instance = placeholderHand.Instances.Single(
                candidate => candidate.Id.Equals(selectedCardInstanceId.Value));
            return placeholderHand.Definitions.Single(
                definition => definition.Reference.Equals(instance.DefinitionReference));
        }

        private ActionDefinition GetActionDefinition(ActionOffer offer)
        {
            var definition = GetAllPlaceholderCardDefinitions().Single(
                candidate => candidate.Reference.Equals(offer.SourceDefinitionReference));
            return definition.ActionDefinitions.Single(
                candidate => candidate.Id.Equals(offer.ActionDefinitionId));
        }

        private int GetPrintedActionValue(ActionOffer offer, ActionKind kind)
        {
            return PrintedValueQuery.CalculatePrintedValue(
                offer,
                GetAllPlaceholderCardDefinitions(),
                kind == ActionKind.Attack
                    ? PrintedValueChannel.Attack
                    : PrintedValueChannel.Movement).Total;
        }

        private int GetPrintedRangeTotal(ActionOffer offer)
        {
            return PrintedValueQuery.CalculatePrintedValue(
                offer,
                GetAllPlaceholderCardDefinitions(),
                PrintedValueChannel.Range).Total;
        }

        private static string FormatContext(ActionInvocationContext context)
        {
            return context == ActionInvocationContext.PrintedPrimary ? "Primary" : "Secondary";
        }

        private static string FormatSide(TeamSide side)
        {
            return side == TeamSide.Red ? "[R] RED" : "[B] BLUE";
        }

        private static TeamSide Opposite(TeamSide side)
        {
            return side == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;
        }

        private HeroId GetSingleFixtureHeroId(TeamSide side)
        {
            var team = placeholderParticipants.Teams.Single(candidate => candidate.Side == side);
            return placeholderParticipants.GetHeroesForTeam(team.Id).Single().Id;
        }

        private bool TryGetConfiguredTeamSide(HeroId ownerId, out TeamSide side)
        {
            try
            {
                side = placeholderParticipants.GetTeamForHero(ownerId).Side;
                return true;
            }
            catch (KeyNotFoundException)
            {
                side = TeamSide.None;
                return false;
            }
        }

        private Rect GetCardPanelRect()
        {
            var width = Mathf.Min(820f, Mathf.Max(320f, Screen.width - (CardPanelMargin * 2f)));
            return new Rect(
                CardPanelMargin,
                Mathf.Max(CardPanelMargin, Screen.height - CardPanelHeight - CardPanelMargin),
                width,
                CardPanelHeight);
        }

        private Rect GetTieBreakerIndicatorRect()
        {
            var width = Mathf.Min(
                TieBreakerIndicatorWidth,
                Mathf.Max(120f, Screen.width - (CardPanelMargin * 2f)));
            return new Rect(
                Mathf.Max(CardPanelMargin, Screen.width - width - CardPanelMargin),
                CardPanelMargin,
                width,
                TieBreakerIndicatorHeight);
        }

        private static string FormatTieBreakerIndicator(TieBreakerState state)
        {
            if (!state.IsDefined)
            {
                return "TIE BREAKER COIN\n[?] UNKNOWN";
            }

            return state.RevealedSide == TeamSide.Red
                ? "TIE BREAKER COIN\n[R] RED"
                : "TIE BREAKER COIN\n[B] BLUE";
        }

        private static Color GetTieBreakerIndicatorColor(TieBreakerState state)
        {
            if (!state.IsDefined)
            {
                return new Color(0.3f, 0.3f, 0.3f);
            }

            return state.RevealedSide == TeamSide.Red
                ? new Color(0.72f, 0.16f, 0.12f)
                : new Color(0.12f, 0.32f, 0.72f);
        }

        private void DrawTieBreakerIndicator()
        {
            var state = CurrentTieBreakerState;
            if (!state.IsDefined)
            {
                return;
            }

            var indicatorRect = GetTieBreakerIndicatorRect();
            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;
            GUI.color = GetTieBreakerIndicatorColor(state);
            GUI.DrawTexture(indicatorRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
            GUI.Box(indicatorRect, GUIContent.none);

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            GUI.contentColor = Color.white;
            GUI.Label(
                indicatorRect,
                FormatTieBreakerIndicator(state),
                labelStyle);
            GUI.contentColor = previousContentColor;
        }

        private void OnGUI()
        {
            if (!cardDrivenMovementEnabled)
            {
                return;
            }

            InitializeCardDrivenMovementIfEnabled();
            DrawTieBreakerIndicator();
            var panel = GetCardPanelRect();
            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, panel.height - 16f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                $"PHASE 3 DIRECTOR TEST ROUND {VisibleRoundIndex} - TURN {VisibleTurnIndex}/" +
                $"{RoundResolutionAuthority.TurnsPerRound} - {VisibleRoundPhase.ToString().ToUpperInvariant()}");
            GUI.enabled = CanUndoDirectorCommand;
            if (GUILayout.Button("UNDO", GUILayout.Width(80f), GUILayout.Height(24f)))
            {
                TryUndoDirectorCommand();
            }

            GUI.enabled = CanRestartDirectorScenario;
            if (GUILayout.Button("RESTART", GUILayout.Width(90f), GUILayout.Height(24f)))
            {
                TryRestartDirectorScenario();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label(cardStatus);

            foreach (var hand in VisibleDirectorHands)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(hand.Label, GUILayout.Width(122f));
                var commitmentStatus = GetTurnCardCommitmentStatus(hand.TeamSide);
                GUILayout.Label(GetDirectorCommitmentLabel(hand.TeamSide));
                if (CanPassTurnCard(hand.TeamSide) &&
                    GUILayout.Button("PASS - NO CARD", GUILayout.Width(125f), GUILayout.Height(24f)))
                {
                    TryPassTurnCard(hand.TeamSide);
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(122f);
                foreach (var instance in hand.Instances)
                {
                    var previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = selectedCardInstanceId.HasValue &&
                        selectedCardInstanceId.Value.Equals(instance.Id)
                            ? new Color(0.35f, 0.75f, 1f)
                            : Color.white;
                    GUI.enabled = !IsCardResolutionPending &&
                        commitmentStatus == TurnCardCommitmentStatus.AwaitingResponse;
                    if (GUILayout.Button(
                            hand.GetCardButtonLabel(instance),
                            GUILayout.MinWidth(112f),
                            GUILayout.Height(46f)))
                    {
                        TrySelectDirectorCard(instance.Id);
                    }

                    GUI.enabled = true;
                    GUI.backgroundColor = previousColor;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Actions", GUILayout.Width(54f));
            if (IsVisibleRoundEnded &&
                GUILayout.Button("CONTINUE END OF ROUND", GUILayout.Width(200f), GUILayout.Height(46f)))
            {
                TryAdvanceEndOfRound();
            }

            if (IsVisibleTurnActive &&
                selectedCardInstanceId.HasValue &&
                GUILayout.Button("HOLD", GUILayout.Width(100f), GUILayout.Height(46f)))
            {
                TryPerformHold(selectedCardInstanceId.Value);
            }

            if (selectedCardInstanceId.HasValue &&
                !IsVisibleTurnActive &&
                GUILayout.Button("CONFIRM FACEDOWN", GUILayout.Width(170f), GUILayout.Height(46f)))
            {
                TryConfirmSelectedTurnCard();
            }

            var visibleActionOffers = IsVisibleTurnActive || !HasCompletePlaceholderRoundBindings()
                ? SelectedCardOffers
                : Array.Empty<ActionOffer>();
            foreach (var offer in visibleActionOffers)
            {
                var definition = GetActionDefinition(offer);
                var actionKind = definition.Kind;
                var printedValue = GetPrintedActionValue(offer, actionKind);
                var isSelected = selectedActionOffer.HasValue && selectedActionOffer.Value.Equals(offer);
                var previousColor = GUI.backgroundColor;
                GUI.backgroundColor = isSelected
                    ? new Color(1f, 0.75f, 0.25f)
                    : Color.white;
                GUI.enabled = !IsCardResolutionPending;
                var textPolicy = definition.DefaultPolicy.CardTextApplication == CardTextApplicationPolicy.Applies
                    ? "text applies"
                    : "text suppressed";
                var actionLabel = actionKind == ActionKind.Attack &&
                    offer.AttackRange == AttackRangeKind.Ranged
                        ? $"RANGED ATTACK {printedValue} RANGE {GetPrintedRangeTotal(offer)}"
                        : $"{actionKind.ToString().ToUpperInvariant()} {printedValue}";
                if (GUILayout.Button(
                        $"{FormatContext(offer.InvocationContext).ToUpperInvariant()} " +
                        $"{actionLabel}\n{textPolicy}",
                        GUILayout.Height(46f)))
                {
                    TrySelectAction(offer.ActionDefinitionId);
                }

                GUI.enabled = true;
                GUI.backgroundColor = previousColor;
            }

            if (IsCardMovementResolutionPending)
            {
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(0.95f, 0.45f, 0.35f);
                if (GUILayout.Button("STOP - move 0 spaces", GUILayout.Width(170f), GUILayout.Height(46f)))
                {
                    TryStopCardMovement();
                }

                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndHorizontal();

            var defenseChoice = PendingDefenseChoice;
            if (defenseChoice != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Defense", GUILayout.Width(54f));
                foreach (var offer in defenseChoice.LegalDefenseOffers)
                {
                    var definition = GetAllPlaceholderCardDefinitions().Single(candidate =>
                        candidate.Reference.Equals(offer.SourceDefinitionReference));
                    var defenseTotal = PrintedValueQuery.CalculatePrintedValue(
                        offer,
                        GetAllPlaceholderCardDefinitions(),
                        PrintedValueChannel.Defense).Total;
                    if (GUILayout.Button(
                            $"DISCARD {definition.DisplayName}\nDEFENSE {defenseTotal}",
                            GUILayout.MinWidth(106f),
                            GUILayout.Height(52f)))
                    {
                        TrySubmitDefenseCard(offer.SourceCardInstanceId);
                    }
                }

                GUILayout.FlexibleSpace();
                if (defenseChoice.CanDecline &&
                    GUILayout.Button("DECLINE DEFENSE", GUILayout.Width(150f), GUILayout.Height(52f)))
                {
                    TryDeclineDefense();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }
    }
}
