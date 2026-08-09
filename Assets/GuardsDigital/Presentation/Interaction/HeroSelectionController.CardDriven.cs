using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    public sealed partial class HeroSelectionController
    {
        private const float CardPanelMargin = 12f;
        private const float CardPanelHeight = 184f;

        [SerializeField]
        private bool cardDrivenMovementEnabled;

        private readonly List<CardActionResolutionEvent> cardResolutionEvents =
            new List<CardActionResolutionEvent>();
        private readonly Dictionary<HeroId, PlaceholderMovementHand> placeholderHands =
            new Dictionary<HeroId, PlaceholderMovementHand>();
        private bool placeholderOneVsOneHandsInitialized;
        private MatchParticipants placeholderParticipants;

        private PlaceholderMovementHand placeholderHand;
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
                    ? placeholderHand.Definitions
                    : Array.Empty<CardDefinition>();
            }
        }

        public IReadOnlyList<CardInstance> VisibleCardInstances
        {
            get
            {
                InitializeCardDrivenMovementIfEnabled();
                return placeholderHand != null
                    ? placeholderHand.Instances
                    : Array.Empty<CardInstance>();
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
            IsCardMovementResolutionPending || IsAttackTargetPending || IsDefenseResponsePending;

        public bool IsCardMovementResolutionPending =>
            cardResolutionAuthority != null && cardResolutionAuthority.PendingChoice != null;

        public bool IsAttackTargetPending =>
            cardResolutionAuthority != null && cardResolutionAuthority.PendingAttackTargetChoice != null;

        public bool IsDefenseResponsePending =>
            cardResolutionAuthority != null && cardResolutionAuthority.PendingDefenseChoice != null;

        public PendingMovementChoice PendingCardChoice => cardResolutionAuthority?.PendingChoice;

        public PendingAttackTargetChoice PendingAttackTargetChoice =>
            cardResolutionAuthority?.PendingAttackTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice =>
            cardResolutionAuthority?.PendingDefenseChoice;

        public IReadOnlyList<ReachableMovementDestination> PendingCardDestinations =>
            PendingCardChoice != null
                ? PendingCardChoice.LegalDestinations
                : Array.Empty<ReachableMovementDestination>();

        public IReadOnlyList<CardActionResolutionEvent> CardResolutionEvents => cardResolutionEvents;

        public IReadOnlyList<HexCoordinate> CardMovementRoute => lastCardMovementRoute;

        public int CardRemainingMovementAllowance => IsCardMovementResolutionPending
            ? PendingCardChoice.RemainingAllowance
            : lastCardRemainingAllowance;

        public int LastCardMovementAllowance => lastCardMovementAllowance;

        public ActionInvocationContext LastCardInvocationContext { get; private set; }

        public CardTextApplicationPolicy LastCardTextApplicationPolicy { get; private set; }

        public CardActionResolutionRejectionReason LastCardRejectionReason { get; private set; }

        public string CardStatus => cardStatus;

        public void ConfigureCardDrivenMovement(bool enabled)
        {
            cardDrivenMovementEnabled = enabled;
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

            if (!placeholderHand.Instances.Any(instance => instance.Id.Equals(cardInstanceId)))
            {
                return false;
            }

            selectedCardInstanceId = cardInstanceId;
            selectedActionOffer = null;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
            cardStatus = "Card selected. Choose an available action.";
            ClearSelectionVisuals();
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

            var result = cardResolutionAuthority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    PendingAttackTargetChoice.ResolutionId,
                    PendingAttackTargetChoice.ChoiceId,
                    targetHeroId),
                placeholderHand.Definitions,
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
                "Defense response pending; Defense selection is introduced in milestone 2.4.";
            return true;
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
            return GetCardPanelRect().Contains(guiPosition);
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
            selectedCardInstanceId = null;
            selectedActionOffer = null;
            cardResolutionEvents.Clear();
            lastCardMovementRoute = Array.Empty<HexCoordinate>();
            lastCardMovementAllowance = 0;
            lastCardRemainingAllowance = 0;
            LastCardInvocationContext = ActionInvocationContext.None;
            LastCardTextApplicationPolicy = CardTextApplicationPolicy.None;
            LastCardRejectionReason = CardActionResolutionRejectionReason.None;
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

            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            placeholderParticipants = configuration.Participants;
            placeholderHands[configuration.RedHand.Instances[0].OwnerId] = configuration.RedHand;
            placeholderHands[configuration.BlueHand.Instances[0].OwnerId] = configuration.BlueHand;
            placeholderOneVsOneHandsInitialized = true;
        }

        private bool StartVisibleAttack(ActionOffer offer)
        {
            EnsurePlaceholderOneVsOneHands();
            var start = cardResolutionAuthority.StartAttackResolution(
                new ActionResolutionId($"visible-attack-{cardResolutionSequence}"),
                offer,
                placeholderHand.Definitions,
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

        private IReadOnlyList<CardInstance> GetAllPlaceholderCardInstances()
        {
            EnsurePlaceholderOneVsOneHands();
            return placeholderHands.Values
                .SelectMany(hand => hand.Instances)
                .OrderBy(instance => instance.Id.Value, StringComparer.Ordinal)
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
            var definition = placeholderHand.Definitions.Single(
                candidate => candidate.Reference.Equals(offer.SourceDefinitionReference));
            return definition.ActionDefinitions.Single(
                candidate => candidate.Id.Equals(offer.ActionDefinitionId));
        }

        private int GetPrintedActionValue(ActionOffer offer, ActionKind kind)
        {
            return PrintedValueQuery.CalculatePrintedValue(
                offer,
                placeholderHand.Definitions,
                kind == ActionKind.Attack
                    ? PrintedValueChannel.Attack
                    : PrintedValueChannel.Movement).Total;
        }

        private int GetPrintedRangeTotal(ActionOffer offer)
        {
            return PrintedValueQuery.CalculatePrintedValue(
                offer,
                placeholderHand.Definitions,
                PrintedValueChannel.Range).Total;
        }

        private static string FormatContext(ActionInvocationContext context)
        {
            return context == ActionInvocationContext.PrintedPrimary ? "Primary" : "Secondary";
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

        private void OnGUI()
        {
            if (!cardDrivenMovementEnabled)
            {
                return;
            }

            InitializeCardDrivenMovementIfEnabled();
            var panel = GetCardPanelRect();
            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, panel.height - 16f));
            var activeHeroLabel = heroView != null
                ? $"{heroView.TeamSide.ToString().ToUpperInvariant()} HERO"
                : "NO HERO";
            GUILayout.Label($"PHASE 2 TEST HAND - {activeHeroLabel} - choose a card, then an action");
            GUILayout.Label(cardStatus);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Cards", GUILayout.Width(54f));
            foreach (var instance in VisibleCardInstances)
            {
                var definition = VisibleCardDefinitions.Single(
                    candidate => candidate.Reference.Equals(instance.DefinitionReference));
                var previousColor = GUI.backgroundColor;
                GUI.backgroundColor = selectedCardInstanceId.HasValue &&
                    selectedCardInstanceId.Value.Equals(instance.Id)
                        ? new Color(0.35f, 0.75f, 1f)
                        : Color.white;
                GUI.enabled = !IsCardResolutionPending;
                if (GUILayout.Button(definition.DisplayName, GUILayout.Height(32f)))
                {
                    TrySelectCard(instance.Id);
                }

                GUI.enabled = true;
                GUI.backgroundColor = previousColor;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Actions", GUILayout.Width(54f));
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
            GUILayout.EndArea();
        }
    }
}
