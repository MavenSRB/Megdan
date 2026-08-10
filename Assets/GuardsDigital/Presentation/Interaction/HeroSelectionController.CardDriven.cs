using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
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
        private readonly Dictionary<HeroId, PlaceholderMovementHand> placeholderHands =
            new Dictionary<HeroId, PlaceholderMovementHand>();
        private bool placeholderOneVsOneHandsInitialized;
        private MatchParticipants placeholderParticipants;
        private PlaceholderOneVsOneTestConfiguration placeholderConfiguration;

        private PlaceholderMovementHand placeholderHand;
        private TurnCardCommitmentAuthority turnCardCommitmentAuthority;
        private TurnResolutionAuthority turnResolutionAuthority;
        private CardActionResolutionAuthority cardResolutionAuthority;
        private CardInstanceId? selectedCardInstanceId;
        private ActionOffer? selectedActionOffer;
        private IReadOnlyList<HexCoordinate> lastCardMovementRoute = Array.Empty<HexCoordinate>();
        private int cardResolutionSequence;
        private int lastCardMovementAllowance;
        private int lastCardRemainingAllowance;
        private string cardStatus = "Select a test card.";

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
                    return ActionOfferQuery
                        .GetAvailableTurnActionOffers(
                            placeholderConfiguration.CardDefinitions,
                            GetAllPlaceholderCardInstances(),
                            turnResolutionAuthority.SelectedActor.ActorId,
                            turnResolutionAuthority.SelectedActor.CardInstanceId)
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

        public bool IsVisibleTurnActive =>
            turnResolutionAuthority != null &&
            turnResolutionAuthority.IsStarted &&
            !turnResolutionAuthority.IsEnded;

        public bool IsVisibleTurnEnded =>
            turnResolutionAuthority != null && turnResolutionAuthority.IsEnded;

        public SelectedInitiativeActor VisibleTurnActor =>
            turnResolutionAuthority != null ? turnResolutionAuthority.SelectedActor : default;

        public bool IsTurnCardRevealComplete =>
            turnCardCommitmentAuthority != null && turnCardCommitmentAuthority.IsRevealed;

        public IReadOnlyList<HexCoordinate> CardMovementRoute => lastCardMovementRoute;

        public int CardRemainingMovementAllowance => IsCardMovementResolutionPending
            ? PendingCardChoice.RemainingAllowance
            : lastCardRemainingAllowance;

        public int LastCardMovementAllowance => lastCardMovementAllowance;

        public ActionInvocationContext LastCardInvocationContext { get; private set; }

        public CardTextApplicationPolicy LastCardTextApplicationPolicy { get; private set; }

        public CardActionResolutionRejectionReason LastCardRejectionReason { get; private set; }

        public TurnResolutionRejectionReason LastTurnRejectionReason { get; private set; }

        public AttackResolutionOutcome LastAttackOutcome { get; private set; }

        public string CardStatus => cardStatus;

        public TieBreakerState CurrentTieBreakerState
        {
            get
            {
                InitializeCardDrivenMovementIfEnabled();
                return placeholderConfiguration != null
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
            turnCardCommitmentAuthority = null;
            turnCardCommitmentEvents.Clear();
            turnResolutionAuthority = null;
            turnResolutionEvents.Clear();
            ResetCardDrivenMovementPresentation();
            InitializeCardDrivenMovementIfEnabled();
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
            EnsureTurnCardCommitmentAuthority();
            return turnCardCommitmentAuthority != null
                ? turnCardCommitmentAuthority.GetStatus(side)
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

            var cardId = turnCardCommitmentAuthority.GetRevealedCardInstanceId(side);
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
            EnsureTurnCardCommitmentAuthority();
            return turnCardCommitmentAuthority != null &&
                turnCardCommitmentAuthority.CanPass(
                    side,
                    GetAllPlaceholderCardInstances(),
                    placeholderParticipants);
        }

        public bool TryConfirmSelectedTurnCard()
        {
            InitializeCardDrivenMovementIfEnabled();
            EnsureTurnCardCommitmentAuthority();
            if (!cardDrivenMovementEnabled ||
                IsCardResolutionPending ||
                !selectedCardInstanceId.HasValue ||
                turnCardCommitmentAuthority == null)
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
            var result = turnCardCommitmentAuthority.TryCommitCard(
                side,
                selectedCard.Id,
                GetAllPlaceholderCardInstances(),
                placeholderParticipants);
            if (!result.Accepted)
            {
                cardStatus = $"Turn card rejected: {result.RejectionReason}.";
                return false;
            }

            turnCardCommitmentEvents.AddRange(result.Events);
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            if (turnCardCommitmentAuthority.IsRevealed && TryStartVisibleTurn())
            {
                return true;
            }

            cardStatus = turnCardCommitmentAuthority.IsRevealed
                ? "Both turn cards revealed together. Board bindings are unavailable for turn actions."
                : $"{FormatSide(side)} committed a facedown card. Awaiting {FormatSide(Opposite(side))}.";
            return true;
        }

        public bool TryPassTurnCard(TeamSide side)
        {
            InitializeCardDrivenMovementIfEnabled();
            EnsureTurnCardCommitmentAuthority();
            if (!cardDrivenMovementEnabled || IsCardResolutionPending || turnCardCommitmentAuthority == null)
            {
                return false;
            }

            var result = turnCardCommitmentAuthority.TryPass(
                side,
                GetAllPlaceholderCardInstances(),
                placeholderParticipants);
            if (!result.Accepted)
            {
                cardStatus = $"Pass rejected: {result.RejectionReason}.";
                return false;
            }

            turnCardCommitmentEvents.AddRange(result.Events);
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            if (turnCardCommitmentAuthority.IsRevealed && TryStartVisibleTurn())
            {
                return true;
            }

            cardStatus = turnCardCommitmentAuthority.IsRevealed
                ? "Turn commitments complete. Board bindings are unavailable for turn actions."
                : $"{FormatSide(side)} passed with no card. Awaiting {FormatSide(Opposite(side))}.";
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
                return GetActionDefinition(matchingOffers[0]).Kind == ActionKind.Attack
                    ? StartVisibleTurnAttack(matchingOffers[0])
                    : StartVisibleTurnMovement(matchingOffers[0]);
            }

            RebuildMoveAuthority();
            cardResolutionAuthority = new CardActionResolutionAuthority(moveAuthority);
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
                cardResolutionAuthority = null;
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
                var turnResult = turnResolutionAuthority.TryAnswerAttackTarget(command);
                if (!turnResult.Accepted)
                {
                    LastTurnRejectionReason = turnResult.RejectionReason;
                    cardStatus = $"Turn Attack target rejected: {turnResult.RejectionReason}. " +
                        "Choose a legal enemy hero.";
                    return false;
                }

                turnResolutionEvents.AddRange(turnResult.Events);
                cardResolutionEvents.AddRange(turnResult.Events
                    .Where(ruleEvent => ruleEvent.Kind == TurnResolutionEventKind.AttackActionProgressed)
                    .Select(ruleEvent => ruleEvent.CardActionEvent));
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
                return SubmitVisibleTurnMovementChoice(command);
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
            cardResolutionAuthority = null;
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

            if (string.IsNullOrWhiteSpace(cardStatus))
            {
                cardStatus = "Select a test card.";
            }
        }

        private void ResetCardDrivenMovementPresentation()
        {
            placeholderHand = null;
            cardResolutionAuthority = null;
            turnResolutionAuthority = null;
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            cardResolutionEvents.Clear();
            turnResolutionEvents.Clear();
            lastCardMovementRoute = Array.Empty<HexCoordinate>();
            lastCardMovementAllowance = 0;
            lastCardRemainingAllowance = 0;
            LastCardInvocationContext = ActionInvocationContext.None;
            LastCardTextApplicationPolicy = CardTextApplicationPolicy.None;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
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
            EnsureTurnCardCommitmentAuthority();
        }

        private void EnsureTurnCardCommitmentAuthority()
        {
            if (turnCardCommitmentAuthority == null && placeholderConfiguration != null)
            {
                turnCardCommitmentAuthority = new TurnCardCommitmentAuthority();
            }
        }

        private bool TryStartVisibleTurn()
        {
            if (turnResolutionAuthority != null ||
                turnCardCommitmentAuthority == null ||
                !turnCardCommitmentAuthority.IsRevealed ||
                placeholderConfiguration == null ||
                placeholderParticipants == null ||
                heroView == null ||
                tileViews == null)
            {
                return false;
            }

            RebuildMoveAuthority();
            if (moveAuthority == null)
            {
                return false;
            }

            cardResolutionAuthority = new CardActionResolutionAuthority(moveAuthority);
            turnResolutionAuthority = new TurnResolutionAuthority(
                placeholderConfiguration.CardDefinitions,
                GetAllPlaceholderCardInstances(),
                placeholderParticipants,
                placeholderConfiguration.TieBreakerState,
                cardResolutionAuthority);
            var result = turnResolutionAuthority.TryStart();
            if (!result.Accepted)
            {
                LastTurnRejectionReason = result.RejectionReason;
                cardStatus = $"Turn start rejected: {result.RejectionReason}.";
                turnResolutionAuthority = null;
                cardResolutionAuthority = null;
                return false;
            }

            turnResolutionEvents.AddRange(result.Events);
            LastTurnRejectionReason = TurnResolutionRejectionReason.None;
            RefreshVisibleTurnActor("Both turn cards revealed together.");
            return true;
        }

        private bool StartVisibleTurnMovement(ActionOffer offer)
        {
            selectedActionOffer = offer;
            cardResolutionSequence++;
            cardResolutionEvents.Clear();
            lastCardMovementRoute = Array.Empty<HexCoordinate>();
            var result = turnResolutionAuthority.TryStartMovement(
                new ActionResolutionId($"visible-turn-movement-{cardResolutionSequence}"),
                offer);
            if (!result.Accepted)
            {
                LastTurnRejectionReason = result.RejectionReason;
                cardStatus = $"Turn action rejected: {result.RejectionReason}.";
                selectedActionOffer = null;
                return false;
            }

            turnResolutionEvents.AddRange(result.Events);
            cardResolutionEvents.AddRange(result.Events
                .Where(ruleEvent => ruleEvent.Kind == TurnResolutionEventKind.MovementActionProgressed)
                .Select(ruleEvent => ruleEvent.CardActionEvent));
            var pending = turnResolutionAuthority.PendingMovementResolution;
            var choice = turnResolutionAuthority.PendingMovementChoice;
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
            var result = turnResolutionAuthority.TryAnswerMovementChoice(command);
            if (!result.Accepted)
            {
                LastTurnRejectionReason = result.RejectionReason;
                cardStatus = $"Turn choice rejected: {result.RejectionReason}. Choose again.";
                return false;
            }

            turnResolutionEvents.AddRange(result.Events);
            cardResolutionEvents.AddRange(result.Events
                .Where(ruleEvent => ruleEvent.Kind == TurnResolutionEventKind.MovementActionProgressed)
                .Select(ruleEvent => ruleEvent.CardActionEvent));
            lastCardRemainingAllowance = turnResolutionAuthority.PendingMovementResolution != null
                ? turnResolutionAuthority.PendingMovementResolution.RemainingAllowance
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
            if (turnResolutionAuthority.PendingMovementChoice != null)
            {
                cardStatus = $"{completedCardName}: {completedContext} Movement has " +
                    $"{turnResolutionAuthority.PendingMovementChoice.RemainingAllowance} remaining. " +
                    "Choose another destination or Stop.";
                RefreshCardChoiceHighlights(turnResolutionAuthority.PendingMovementChoice);
                return true;
            }

            var completedCost = lastCardMovementAllowance - lastCardRemainingAllowance;
            ClearSelectionVisuals();
            selectedActionOffer = null;
            RefreshVisibleTurnActor(
                $"{completedCardName}: {completedContext} Movement completed ({completedCost} space" +
                (completedCost == 1 ? ")." : "s)."));
            return true;
        }

        private void RefreshVisibleTurnActor(string statusPrefix)
        {
            ClearSelectionVisuals();
            selectedActionOffer = null;
            if (turnResolutionAuthority.IsEnded)
            {
                selectedCardInstanceId = null;
                cardStatus = $"{statusPrefix} Turn ended.";
                return;
            }

            if (!turnResolutionAuthority.SelectedActor.IsDefined)
            {
                selectedCardInstanceId = null;
                cardStatus = $"{statusPrefix} Awaiting the Initiative tie choice.";
                return;
            }

            var actor = turnResolutionAuthority.SelectedActor;
            FocusVisibleTurnHero(actor.ActorId);
            selectedCardInstanceId = actor.CardInstanceId;
            cardStatus = $"{statusPrefix} {FormatSide(placeholderParticipants.GetTeamForHero(actor.ActorId).Side)} " +
                $"acts at Initiative {actor.InitiativeTotal}. Choose a Movement or Attack action.";
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
                cardResolutionAuthority = null;
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
            var result = turnResolutionAuthority.TryStartAttack(
                new ActionResolutionId($"visible-turn-attack-{cardResolutionSequence}"),
                offer);
            if (!result.Accepted)
            {
                LastTurnRejectionReason = result.RejectionReason;
                cardStatus = $"Turn Attack rejected: {result.RejectionReason}.";
                selectedActionOffer = null;
                return false;
            }

            turnResolutionEvents.AddRange(result.Events);
            cardResolutionEvents.AddRange(result.Events
                .Where(ruleEvent => ruleEvent.Kind == TurnResolutionEventKind.AttackActionProgressed)
                .Select(ruleEvent => ruleEvent.CardActionEvent));
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
                return SubmitVisibleTurnDefenseResponse(command, selectedCardId);
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
            cardResolutionAuthority = null;
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

            var result = turnResolutionAuthority.TryAnswerDefenseResponse(command);
            if (!result.Accepted)
            {
                LastTurnRejectionReason = result.RejectionReason;
                cardStatus = $"Turn Defense response rejected: {result.RejectionReason}. Choose again.";
                return false;
            }

            turnResolutionEvents.AddRange(result.Events);
            var attackEvents = result.Events
                .Where(ruleEvent => ruleEvent.Kind == TurnResolutionEventKind.AttackActionProgressed)
                .Select(ruleEvent => ruleEvent.CardActionEvent)
                .ToArray();
            cardResolutionEvents.AddRange(attackEvents);
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
            RefreshVisibleTurnActor(resultLabel);
            return true;
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
            GUILayout.Label("PHASE 3 DIRECTOR TEST HANDS - reveal, then resolve the current Initiative actor");
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
            if (selectedCardInstanceId.HasValue &&
                !IsVisibleTurnActive &&
                GUILayout.Button("CONFIRM FACEDOWN", GUILayout.Width(170f), GUILayout.Height(46f)))
            {
                TryConfirmSelectedTurnCard();
            }

            foreach (var offer in SelectedCardOffers)
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
