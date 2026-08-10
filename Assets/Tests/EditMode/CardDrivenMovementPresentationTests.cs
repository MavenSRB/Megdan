using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class CardDrivenMovementPresentationTests
    {
        [Test]
        public void PlaceholderHand_ContainsVisibleTestCardsWithTypedPrimaryAndSecondaryMovement()
        {
            var ownerId = new HeroId("hero");
            var hand = PlaceholderMovementHand.Create(ownerId);

            Assert.That(hand.Definitions, Has.Count.EqualTo(2));
            Assert.That(hand.Instances, Has.Count.EqualTo(2));
            Assert.That(hand.Definitions, Has.All.Matches<CardDefinition>(definition =>
                definition.DisplayName.StartsWith("TEST - ")));
            Assert.That(hand.Instances.Select(instance => instance.OwnerId), Has.All.EqualTo(ownerId));

            foreach (var instance in hand.Instances)
            {
                var offers = ActionOfferQuery.GetAvailableOffers(
                        hand.Definitions,
                        hand.Instances,
                        ownerId,
                        ActionOfferWindow.NormalPlay)
                    .Where(offer => offer.SourceCardInstanceId.Equals(instance.Id))
                    .ToArray();
                var primary = offers.Single(offer =>
                    offer.InvocationContext == ActionInvocationContext.PrintedPrimary &&
                    hand.Definitions.Single(candidate =>
                            candidate.Reference.Equals(offer.SourceDefinitionReference))
                        .ActionDefinitions.Single(action =>
                            action.Id.Equals(offer.ActionDefinitionId)).Kind == ActionKind.Movement);
                var secondary = offers.Single(offer =>
                    offer.InvocationContext == ActionInvocationContext.PrintedSecondary &&
                    hand.Definitions.Single(candidate =>
                            candidate.Reference.Equals(offer.SourceDefinitionReference))
                        .ActionDefinitions.Single(action =>
                            action.Id.Equals(offer.ActionDefinitionId)).Kind == ActionKind.Movement);
                var definition = hand.Definitions.Single(candidate =>
                    candidate.Reference.Equals(instance.DefinitionReference));

                Assert.That(PrintedValueQuery.CalculatePrintedValue(
                    primary,
                    hand.Definitions,
                    PrintedValueChannel.Movement).Total, Is.EqualTo(3));
                Assert.That(PrintedValueQuery.CalculatePrintedValue(
                    secondary,
                    hand.Definitions,
                    PrintedValueChannel.Movement).Total, Is.EqualTo(1));
                Assert.That(definition.ActionDefinitions.Single(action =>
                        action.Id.Equals(primary.ActionDefinitionId)).DefaultPolicy.CardTextApplication,
                    Is.EqualTo(CardTextApplicationPolicy.Applies));
                Assert.That(definition.ActionDefinitions.Single(action =>
                        action.Id.Equals(secondary.ActionDefinitionId)).DefaultPolicy.CardTextApplication,
                    Is.EqualTo(CardTextApplicationPolicy.Suppressed));
            }
        }

        [TestCase(TeamSide.Red, "[R] RED")]
        [TestCase(TeamSide.Blue, "[B] BLUE")]
        public void TieBreakerIndicator_DerivesTopRightLabelFromAuthoritativeSetup(
            TeamSide side,
            string expectedSideLabel)
        {
            var objects = new List<GameObject>();
            try
            {
                var scenario = CreateScenario(objects, 3, false, side);
                var indicatorRect = scenario.Controller.TieBreakerIndicatorRect;
                var indicatorColor = scenario.Controller.TieBreakerIndicatorColor;
                var indicatorScreenPosition = new Vector2(
                    indicatorRect.center.x,
                    Screen.height - indicatorRect.center.y);

                Assert.That(
                    scenario.Controller.CurrentTieBreakerState.RevealedSide,
                    Is.EqualTo(side));
                Assert.That(scenario.Controller.TieBreakerIndicatorText, Does.Contain("TIE BREAKER"));
                Assert.That(
                    scenario.Controller.TieBreakerIndicatorText,
                    Does.Contain(expectedSideLabel));
                Assert.That(indicatorRect.xMax, Is.EqualTo(Screen.width - 12f).Within(0.01f));
                Assert.That(indicatorRect.yMin, Is.EqualTo(12f).Within(0.01f));
                Assert.That(scenario.Controller.BlocksPointerAt(indicatorScreenPosition), Is.True);
                Assert.That(
                    side == TeamSide.Red ? indicatorColor.r : indicatorColor.b,
                    Is.GreaterThan(side == TeamSide.Red ? indicatorColor.b : indicatorColor.r));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void SelectingBlueHero_ExposesBlueOwnedCardsAndMovesBlueThroughSharedAuthority()
        {
            var objects = new List<GameObject>();
            try
            {
                var redObject = new GameObject("Red Hero");
                objects.Add(redObject);
                var redHero = redObject.AddComponent<PrimitiveHeroView>();
                redHero.Configure(
                    new HeroOccupancy(
                        new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue),
                        new HexCoordinate(0, 0)),
                    HexWorldLayout.DefaultOuterRadius);

                var blueObject = new GameObject("Blue Hero");
                objects.Add(blueObject);
                var blueHero = blueObject.AddComponent<PrimitiveHeroView>();
                blueHero.Configure(
                    new HeroOccupancy(
                        new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue),
                        new HexCoordinate(0, -1)),
                    HexWorldLayout.DefaultOuterRadius);

                var tiles = CreateRadiusTwoCoordinates()
                    .Select(coordinate => CreateTile(objects, coordinate))
                    .ToArray();
                var controllerObject = new GameObject("Two Hero Card Movement Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                controller.Configure(
                    redHero,
                    tiles,
                    CreateMaterial(Color.yellow),
                    otherHeroes: new[] { blueHero });
                controller.ConfigureCardDrivenMovement(true);

                var redCardIds = controller.VisibleCardInstances.Select(card => card.Id).ToArray();
                Assert.That(controller.ActiveHeroView, Is.SameAs(redHero));
                Assert.That(controller.VisibleCardInstances.All(card =>
                    card.OwnerId.Equals(redHero.Occupancy.HeroId)), Is.True);
                Assert.That(redCardIds.All(id => id.Value.StartsWith("red-")), Is.True);

                Assert.That(controller.TrySelectHero(blueHero), Is.True);
                var blueCardIds = controller.VisibleCardInstances.Select(card => card.Id).ToArray();

                Assert.That(controller.ActiveHeroView, Is.SameAs(blueHero));
                Assert.That(controller.VisibleCardInstances.All(card =>
                    card.OwnerId.Equals(blueHero.Occupancy.HeroId)), Is.True);
                Assert.That(blueCardIds.All(id => id.Value.StartsWith("blue-")), Is.True);
                Assert.That(blueCardIds.Intersect(redCardIds), Is.Empty);
                Assert.That(controller.TrySelectCard(blueCardIds[0]), Is.True);
                Assert.That(controller.TrySelectAction(
                    new ActionDefinitionId("secondary-movement")), Is.True);
                Assert.That(controller.PendingCardChoice.Origin, Is.EqualTo(blueHero.Coordinate));
                Assert.That(controller.HighlightedCoordinates, Has.No.Member(redHero.Coordinate));
                Assert.That(controller.TrySelectHero(redHero), Is.False);
                Assert.That(controller.ActiveHeroView, Is.SameAs(blueHero));
                Assert.That(controller.IsCardResolutionPending, Is.True);

                var destination = new HexCoordinate(1, -1);
                Assert.That(controller.TrySubmitCardDestination(destination), Is.True);
                Assert.That(blueHero.Coordinate, Is.EqualTo(destination));
                Assert.That(redHero.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void RouteDrill_ExposesAttackAndClickingAdjacentEnemyOpensVisibleDefensePrompt()
        {
            var objects = new List<GameObject>();
            try
            {
                var redObject = new GameObject("Red Hero");
                objects.Add(redObject);
                var redHero = redObject.AddComponent<PrimitiveHeroView>();
                redHero.Configure(
                    new HeroOccupancy(
                        new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue),
                        new HexCoordinate(0, 0)),
                    HexWorldLayout.DefaultOuterRadius);

                var blueObject = new GameObject("Blue Hero");
                objects.Add(blueObject);
                var blueHero = blueObject.AddComponent<PrimitiveHeroView>();
                blueHero.Configure(
                    new HeroOccupancy(
                        new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue),
                        new HexCoordinate(0, -1)),
                    HexWorldLayout.DefaultOuterRadius);
                var blueSelector = blueObject.AddComponent<PrimitiveHeroPointerSelector>();

                var tiles = CreateRadiusTwoCoordinates()
                    .Select(coordinate => CreateTile(objects, coordinate))
                    .ToArray();
                var controllerObject = new GameObject("Visible Attack Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                controller.Configure(
                    redHero,
                    tiles,
                    CreateMaterial(Color.yellow),
                    otherHeroes: new[] { blueHero });
                controller.ConfigureCardDrivenMovement(true);
                blueSelector.Configure(controller);

                var routeDrill = controller.VisibleCardInstances.Single(card =>
                    card.Id.Value == "red-route-drill-card");
                Assert.That(controller.TrySelectCard(routeDrill.Id), Is.True);
                Assert.That(
                    controller.SelectedCardOffers.Select(offer => offer.ActionDefinitionId.Value),
                    Is.EquivalentTo(new[] { "primary-movement", "secondary-movement", "primary-attack" }));

                Assert.That(controller.TrySelectAction(new ActionDefinitionId("primary-attack")), Is.True);
                Assert.That(controller.IsAttackTargetPending, Is.True);
                Assert.That(controller.CardStatus, Does.Contain("click the adjacent enemy hero"));
                Assert.That(controller.HighlightedCoordinates, Is.EqualTo(new[] { blueHero.Coordinate }));
                Assert.That(tiles.Single(tile => tile.Coordinate.Equals(blueHero.Coordinate)).IsHighlighted, Is.True);
                Assert.That(tiles.Where(tile => !tile.Coordinate.Equals(blueHero.Coordinate)),
                    Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));

                var targeted = blueSelector.TrySelectFromPointerRay(
                    new Ray(blueHero.transform.position + (Vector3.up * 2f), Vector3.down));

                Assert.That(targeted, Is.True);
                Assert.That(controller.ActiveHeroView, Is.SameAs(redHero));
                Assert.That(controller.IsAttackTargetPending, Is.False);
                Assert.That(controller.IsDefenseResponsePending, Is.True);
                Assert.That(controller.PendingDefenseChoice.ChooserId, Is.EqualTo(blueHero.Occupancy.HeroId));
                Assert.That(controller.PendingDefenseChoice.AttackTotal, Is.EqualTo(3));
                Assert.That(controller.PendingDefenseChoice.LegalDefenseOffers, Has.Count.EqualTo(2));
                Assert.That(controller.CardStatus, Does.Contain("Defense response pending"));
                Assert.That(controller.HighlightedCoordinates, Is.Empty);
                Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
                Assert.That(controller.CardResolutionEvents.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
                {
                    CardActionResolutionEventKind.AttackTargetChoiceRequested,
                    CardActionResolutionEventKind.AttackStarted,
                    CardActionResolutionEventKind.AttackTotalCalculated,
                    CardActionResolutionEventKind.DefenseResponseChoiceRequested
                }));
                Assert.That(controller.VisibleCardInstances, Has.All.Matches<CardInstance>(card =>
                    card.State.Location == CardLocation.Hand));
                Assert.That(controller.TrySelectHero(blueHero), Is.False);

                var defenseOffer = controller.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                    PrintedValueQuery.CalculatePrintedValue(
                        offer,
                        controller.VisibleCardDefinitions,
                        PrintedValueChannel.Defense).Total == 3);
                Assert.That(controller.TrySubmitDefenseCard(defenseOffer.SourceCardInstanceId), Is.True);
                Assert.That(controller.IsDefenseResponsePending, Is.False);
                Assert.That(controller.IsAttackOutcomePending, Is.False);
                Assert.That(controller.IsCardResolutionPending, Is.False);
                Assert.That(controller.PendingAttackResolution, Is.Null);
                Assert.That(controller.LastAttackOutcome,
                    Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended));
                var blueDirectorHand = controller.VisibleDirectorHands.Single(hand =>
                    hand.OwnerId.Equals(blueHero.Occupancy.HeroId));
                Assert.That(blueDirectorHand.Instances.Select(card => card.Id),
                    Has.No.Member(defenseOffer.SourceCardInstanceId));
                Assert.That(blueDirectorHand.Instances, Has.All.Matches<CardInstance>(card =>
                    card.State.Location == CardLocation.Hand));
                Assert.That(controller.CardStatus, Does.Contain("discarded for Defense 3"));
                Assert.That(controller.CardStatus, Does.Contain("Attack 3 vs Defense 3"));
                Assert.That(controller.CardStatus, Does.Contain("successfully defended"));
                Assert.That(controller.CardResolutionEvents.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
                {
                    CardActionResolutionEventKind.AttackTargetChoiceRequested,
                    CardActionResolutionEventKind.AttackStarted,
                    CardActionResolutionEventKind.AttackTotalCalculated,
                    CardActionResolutionEventKind.DefenseResponseChoiceRequested,
                    CardActionResolutionEventKind.DefenseCardDiscarded,
                    CardActionResolutionEventKind.DefenseTotalCalculated,
                    CardActionResolutionEventKind.DefenseResponseCompleted,
                    CardActionResolutionEventKind.AttackSuccessfullyDefended,
                    CardActionResolutionEventKind.AttackResolutionCompleted
                }));
                Assert.That(controller.TrySelectCard(routeDrill.Id), Is.True,
                    "Completing the Attack releases card selection for another action.");
                Assert.That(controller.TrySelectHero(blueHero), Is.True,
                    "Completing the Attack releases actor selection.");
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void ObstacleCourse_ExposesTypedRangedAttackAndUsesSharedVisibleDefenseFlow()
        {
            var objects = new List<GameObject>();
            try
            {
                var redObject = new GameObject("Red Ranged Hero");
                objects.Add(redObject);
                var redHero = redObject.AddComponent<PrimitiveHeroView>();
                redHero.Configure(
                    new HeroOccupancy(
                        new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue),
                        new HexCoordinate(0, 0)),
                    HexWorldLayout.DefaultOuterRadius);

                var blueObject = new GameObject("Blue Ranged Target");
                objects.Add(blueObject);
                var blueHero = blueObject.AddComponent<PrimitiveHeroView>();
                blueHero.Configure(
                    new HeroOccupancy(
                        new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue),
                        new HexCoordinate(0, -1)),
                    HexWorldLayout.DefaultOuterRadius);
                var blueSelector = blueObject.AddComponent<PrimitiveHeroPointerSelector>();

                var tiles = CreateRadiusTwoCoordinates()
                    .Select(coordinate => CreateTile(objects, coordinate))
                    .ToArray();
                var controllerObject = new GameObject("Visible Ranged Attack Controller");
                objects.Add(controllerObject);
                var controller = controllerObject.AddComponent<HeroSelectionController>();
                controller.Configure(
                    redHero,
                    tiles,
                    CreateMaterial(Color.yellow),
                    otherHeroes: new[] { blueHero });
                controller.ConfigureCardDrivenMovement(true);
                blueSelector.Configure(controller);

                var obstacleCourse = controller.VisibleCardInstances.Single(card =>
                    card.Id.Value == "red-obstacle-course-card");
                Assert.That(controller.TrySelectCard(obstacleCourse.Id), Is.True);
                var rangedOffer = controller.SelectedCardOffers.Single(offer =>
                    offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-ranged-attack")));
                Assert.That(rangedOffer.AttackRange, Is.EqualTo(AttackRangeKind.Ranged));
                Assert.That(PrintedValueQuery.CalculatePrintedValue(
                    rangedOffer,
                    controller.VisibleCardDefinitions,
                    PrintedValueChannel.Attack).Total, Is.EqualTo(2));
                Assert.That(PrintedValueQuery.CalculatePrintedValue(
                    rangedOffer,
                    controller.VisibleCardDefinitions,
                    PrintedValueChannel.Range).Total, Is.EqualTo(2));

                Assert.That(controller.TrySelectAction(rangedOffer.ActionDefinitionId), Is.True);
                Assert.That(controller.IsAttackTargetPending, Is.True);
                Assert.That(controller.CardStatus, Does.Contain("Ranged Attack 2 Range 2"));
                Assert.That(controller.CardStatus, Does.Contain("within range"));
                Assert.That(controller.HighlightedCoordinates, Is.EqualTo(new[] { blueHero.Coordinate }));

                var targeted = blueSelector.TrySelectFromPointerRay(
                    new Ray(blueHero.transform.position + (Vector3.up * 2f), Vector3.down));

                Assert.That(targeted, Is.True);
                Assert.That(controller.IsAttackTargetPending, Is.False);
                Assert.That(controller.IsDefenseResponsePending, Is.True);
                Assert.That(controller.PendingDefenseChoice.ChooserId, Is.EqualTo(blueHero.Occupancy.HeroId));
                Assert.That(controller.PendingDefenseChoice.AttackTotal, Is.EqualTo(2));
                Assert.That(controller.CardResolutionEvents.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
                {
                    CardActionResolutionEventKind.AttackTargetChoiceRequested,
                    CardActionResolutionEventKind.AttackStarted,
                    CardActionResolutionEventKind.AttackTotalCalculated,
                    CardActionResolutionEventKind.DefenseResponseChoiceRequested
                }));
                Assert.That(controller.HighlightedCoordinates, Is.Empty);
                Assert.That(controller.VisibleCardInstances, Has.All.Matches<CardInstance>(card =>
                    card.State.Location == CardLocation.Hand));
                Assert.That(controller.TryDeclineDefense(), Is.True);
                Assert.That(controller.IsDefenseResponsePending, Is.False);
                Assert.That(controller.IsAttackOutcomePending, Is.False);
                Assert.That(controller.PendingAttackResolution, Is.Null);
                Assert.That(controller.LastAttackOutcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
                Assert.That(controller.CardStatus, Does.Contain("Defense declined"));
                Assert.That(controller.CardStatus, Does.Contain("attack successful"));
                Assert.That(controller.CardResolutionEvents.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
                {
                    CardActionResolutionEventKind.AttackTargetChoiceRequested,
                    CardActionResolutionEventKind.AttackStarted,
                    CardActionResolutionEventKind.AttackTotalCalculated,
                    CardActionResolutionEventKind.DefenseResponseChoiceRequested,
                    CardActionResolutionEventKind.DefenseDeclined,
                    CardActionResolutionEventKind.DefenseResponseCompleted,
                    CardActionResolutionEventKind.AttackSuccessful,
                    CardActionResolutionEventKind.AttackResolutionCompleted
                }));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void SecondaryOffer_UsesTypedValueOneAndProjectsOnlyLegalAdjacentDestinations()
        {
            var objects = new List<GameObject>();
            try
            {
                var scenario = CreateScenario(objects, 99, true);
                var selectedCard = scenario.Controller.VisibleCardInstances[0];

                Assert.That(scenario.Controller.TrySelectCard(selectedCard.Id), Is.True);
                Assert.That(scenario.Controller.TrySelectAction(
                    new ActionDefinitionId("secondary-movement")), Is.True);

                Assert.That(scenario.Controller.PendingCardChoice, Is.Not.Null);
                Assert.That(scenario.Controller.PendingCardChoice.RemainingAllowance, Is.EqualTo(1));
                Assert.That(scenario.Controller.LastCardMovementAllowance, Is.EqualTo(1));
                Assert.That(scenario.Controller.LastCardInvocationContext,
                    Is.EqualTo(ActionInvocationContext.PrintedSecondary));
                Assert.That(scenario.Controller.LastCardTextApplicationPolicy,
                    Is.EqualTo(CardTextApplicationPolicy.Suppressed));
                Assert.That(scenario.Controller.PendingCardDestinations, Has.All.Matches<ReachableMovementDestination>(
                    destination => destination.Cost == 1 && destination.Route.Count == 2));
                Assert.That(scenario.Controller.HighlightedCoordinates,
                    Has.No.Member(new HexCoordinate(1, 0)));
                Assert.That(scenario.Controller.HighlightedCoordinates,
                    Has.No.Member(new HexCoordinate(0, 1)));
                Assert.That(scenario.Controller.TrySubmitCardDestination(new HexCoordinate(2, 0)), Is.False);
                Assert.That(scenario.Hero.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
                Assert.That(scenario.Controller.IsCardResolutionPending, Is.True);
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void PrimaryOffer_TwoWaypointStepsRemainPendingUntilPlayerStops()
        {
            var objects = new List<GameObject>();
            try
            {
                var scenario = CreateScenario(objects, 99, true);
                var firstDestination = new HexCoordinate(-1, 0);
                var secondDestination = new HexCoordinate(-2, 0);
                var selectedCard = scenario.Controller.VisibleCardInstances[1];

                scenario.Controller.TrySelectCard(selectedCard.Id);
                scenario.Controller.TrySelectAction(new ActionDefinitionId("primary-movement"));
                var expectedChoices = MovementReachabilityQuery.FindReachableDestinations(
                    scenario.Hero.Occupancy,
                    scenario.Tiles.Select(tile => tile.Coordinate),
                    new[] { scenario.Hero.Occupancy },
                    scenario.Obstacles.Select(obstacle => obstacle.Occupancy),
                    3);

                Assert.That(scenario.Controller.PendingCardDestinations.Select(choice => choice.Destination),
                    Is.EquivalentTo(expectedChoices.Select(choice => choice.Destination)));
                Assert.That(scenario.Controller.TrySubmitCardDestination(firstDestination), Is.True);

                Assert.That(scenario.Hero.Coordinate, Is.EqualTo(firstDestination));
                Assert.That(scenario.Controller.CurrentHeroOccupancy.Coordinate, Is.EqualTo(firstDestination));
                Assert.That(scenario.Controller.CardMovementRoute,
                    Is.EqualTo(new[] { new HexCoordinate(0, 0), firstDestination }));
                Assert.That(scenario.Controller.RemainingMovementAllowance, Is.EqualTo(2));
                Assert.That(scenario.Controller.PendingCardChoice.Origin, Is.EqualTo(firstDestination));
                Assert.That(scenario.Controller.IsCardResolutionPending, Is.True);
                Assert.That(scenario.Controller.SelectedCardInstanceId, Is.EqualTo(selectedCard.Id));
                Assert.That(scenario.Controller.SelectedActionOffer, Is.Not.Null);
                Assert.That(scenario.Controller.HighlightedCoordinates,
                    Is.EquivalentTo(scenario.Controller.PendingCardDestinations.Select(choice => choice.Destination)));
                Assert.That(scenario.Controller.TrySubmitCardDestination(secondDestination), Is.True);

                Assert.That(scenario.Hero.Coordinate, Is.EqualTo(secondDestination));
                Assert.That(scenario.Controller.CardMovementRoute,
                    Is.EqualTo(new[] { new HexCoordinate(0, 0), firstDestination, secondDestination }));
                Assert.That(scenario.Controller.RemainingMovementAllowance, Is.EqualTo(1));
                Assert.That(scenario.Controller.PendingCardChoice.Origin, Is.EqualTo(secondDestination));
                Assert.That(scenario.Controller.IsCardResolutionPending, Is.True);
                Assert.That(scenario.Controller.TryStopCardMovement(), Is.True);

                Assert.That(scenario.Hero.Coordinate, Is.EqualTo(secondDestination));
                Assert.That(scenario.Controller.RemainingMovementAllowance, Is.EqualTo(1));
                Assert.That(scenario.Controller.IsCardResolutionPending, Is.False);
                Assert.That(scenario.Controller.SelectedCardInstanceId, Is.Null);
                Assert.That(scenario.Controller.SelectedActionOffer, Is.Null);
                Assert.That(scenario.Controller.HighlightedCoordinates, Is.Empty);
                Assert.That(scenario.Controller.CardResolutionEvents.Select(ruleEvent => ruleEvent.Kind),
                    Is.EqualTo(new[]
                    {
                        CardActionResolutionEventKind.MovementActionStarted,
                        CardActionResolutionEventKind.MovementChoiceRequested,
                        CardActionResolutionEventKind.MovementStepCommitted,
                        CardActionResolutionEventKind.MovementChoiceRequested,
                        CardActionResolutionEventKind.MovementStepCommitted,
                        CardActionResolutionEventKind.MovementChoiceRequested,
                        CardActionResolutionEventKind.MovementActionCompleted
                    }));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void StopButtonCommand_CompletesAtZeroWithoutMoving()
        {
            var objects = new List<GameObject>();
            try
            {
                var scenario = CreateScenario(objects, 99, true);
                var origin = scenario.Hero.Coordinate;

                scenario.Controller.TrySelectCard(scenario.Controller.VisibleCardInstances[0].Id);
                scenario.Controller.TrySelectAction(new ActionDefinitionId("primary-movement"));
                Assert.That(scenario.Controller.TryStopCardMovement(), Is.True);

                Assert.That(scenario.Hero.Coordinate, Is.EqualTo(origin));
                Assert.That(scenario.Controller.CardMovementRoute, Is.EqualTo(new[] { origin }));
                Assert.That(scenario.Controller.RemainingMovementAllowance, Is.EqualTo(3));
                Assert.That(scenario.Controller.IsCardResolutionPending, Is.False);
                Assert.That(scenario.Controller.HighlightedCoordinates, Is.Empty);
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void StaleUiChoice_IsRejectedWithoutMutatingOrClearingPendingChoice()
        {
            var objects = new List<GameObject>();
            try
            {
                var scenario = CreateScenario(objects, 99, true);
                var origin = scenario.Hero.Coordinate;
                scenario.Controller.TrySelectCard(scenario.Controller.VisibleCardInstances[0].Id);
                scenario.Controller.TrySelectAction(new ActionDefinitionId("primary-movement"));
                var pending = scenario.Controller.PendingCardChoice;
                var legalDestination = pending.LegalDestinations[0].Destination;

                var accepted = scenario.Controller.SubmitCardMovementChoice(
                    MovementChoiceCommand.SelectDestination(
                        pending.ResolutionId,
                        new ActionResolutionStepId("stale-ui-choice"),
                        legalDestination));

                Assert.That(accepted, Is.False);
                Assert.That(scenario.Controller.LastCardRejectionReason,
                    Is.EqualTo(CardActionResolutionRejectionReason.StaleChoice));
                Assert.That(scenario.Controller.PendingCardChoice, Is.SameAs(pending));
                Assert.That(scenario.Controller.IsCardResolutionPending, Is.True);
                Assert.That(scenario.Hero.Coordinate, Is.EqualTo(origin));
                Assert.That(scenario.Controller.HighlightedCoordinates,
                    Is.EquivalentTo(pending.LegalDestinations.Select(choice => choice.Destination)));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void PointerDestination_SubmitsPendingChoiceAndCommitsObstacleDetour()
        {
            var objects = new List<GameObject>();
            try
            {
                var scenario = CreateScenario(objects, 99, true);
                var destination = new HexCoordinate(2, 0);
                var destinationTile = scenario.Tiles.Single(tile => tile.Coordinate.Equals(destination));
                var inputObject = new GameObject("Pointer Input");
                objects.Add(inputObject);
                var pointerInput = inputObject.AddComponent<PrimitiveHeroPointerInput>();
                pointerInput.Configure(null, new PrimitiveHeroPointerSelector[0]);

                scenario.Controller.TrySelectCard(scenario.Controller.VisibleCardInstances[0].Id);
                scenario.Controller.TrySelectAction(new ActionDefinitionId("primary-movement"));
                var handled = pointerInput.TryHandlePointerRay(
                    new Ray(destinationTile.transform.position + (Vector3.up * 2f), Vector3.down));

                Assert.That(handled, Is.True);
                Assert.That(scenario.Hero.Coordinate, Is.EqualTo(destination));
                Assert.That(scenario.Controller.CardMovementRoute, Is.EqualTo(new[]
                {
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, -1),
                    new HexCoordinate(2, -1),
                    destination
                }));
                Assert.That(scenario.Controller.IsCardResolutionPending, Is.False);
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void RevealedTurnCards_ExposeCurrentActorsMovementAndAdvanceAfterCompletion()
        {
            var objects = new List<GameObject>();
            try
            {
                var scenario = CreateTurnScenario(objects);
                var redCard = scenario.Configuration.RedHand.Instances[0];
                var blueCard = scenario.Configuration.BlueHand.Instances[1];

                Assert.That(scenario.Controller.TrySelectDirectorCard(redCard.Id), Is.True);
                Assert.That(scenario.Controller.TryConfirmSelectedTurnCard(), Is.True);
                Assert.That(scenario.Controller.TrySelectDirectorCard(blueCard.Id), Is.True);
                Assert.That(scenario.Controller.TryConfirmSelectedTurnCard(), Is.True);

                Assert.That(scenario.Controller.IsVisibleTurnActive, Is.True);
                Assert.That(scenario.Controller.VisibleTurnActor.CardInstanceId, Is.EqualTo(redCard.Id));
                Assert.That(scenario.Controller.ActiveHeroView, Is.SameAs(scenario.RedHero));
                Assert.That(scenario.Controller.SelectedCardInstanceId, Is.EqualTo(redCard.Id));
                Assert.That(scenario.Controller.SelectedCardOffers.Count, Is.EqualTo(2));
                Assert.That(scenario.Controller.SelectedCardOffers.All(offer =>
                    scenario.Configuration.CardDefinitions
                        .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                        .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Kind ==
                    ActionKind.Movement), Is.True);

                Assert.That(scenario.Controller.TrySelectAction(
                    new ActionDefinitionId("primary-movement")), Is.True);
                var destination = new HexCoordinate(2, 0);
                Assert.That(scenario.Controller.PendingCardDestinations.Any(candidate =>
                    candidate.Destination.Equals(destination)), Is.True);
                Assert.That(scenario.Controller.TrySubmitCardDestination(destination), Is.True);

                Assert.That(redCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
                Assert.That(blueCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
                Assert.That(scenario.RedHero.Coordinate, Is.EqualTo(destination));
                Assert.That(scenario.Controller.VisibleTurnActor.CardInstanceId, Is.EqualTo(blueCard.Id));
                Assert.That(scenario.Controller.ActiveHeroView, Is.SameAs(scenario.BlueHero));
                Assert.That(scenario.Controller.SelectedCardInstanceId, Is.EqualTo(blueCard.Id));
                Assert.That(scenario.Controller.CardStatus, Does.Contain("[B] BLUE"));
                Assert.That(scenario.Controller.CardStatus, Does.Contain("Initiative 4"));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        [Test]
        public void StaleTurnMovementChoice_PreservesActorCardOccupancyAndHighlights()
        {
            var objects = new List<GameObject>();
            try
            {
                var scenario = CreateTurnScenario(objects);
                var redCard = scenario.Configuration.RedHand.Instances[0];
                var blueCard = scenario.Configuration.BlueHand.Instances[1];
                scenario.Controller.TrySelectDirectorCard(redCard.Id);
                scenario.Controller.TryConfirmSelectedTurnCard();
                scenario.Controller.TrySelectDirectorCard(blueCard.Id);
                scenario.Controller.TryConfirmSelectedTurnCard();
                scenario.Controller.TrySelectAction(new ActionDefinitionId("primary-movement"));
                var pending = scenario.Controller.PendingCardChoice;
                var origin = scenario.RedHero.Occupancy;
                var highlights = scenario.Controller.HighlightedCoordinates.ToArray();

                var accepted = scenario.Controller.SubmitCardMovementChoice(
                    MovementChoiceCommand.Stop(
                        pending.ResolutionId,
                        new ActionResolutionStepId("stale-visible-turn-choice")));

                Assert.That(accepted, Is.False);
                Assert.That(scenario.Controller.LastTurnRejectionReason,
                    Is.EqualTo(TurnResolutionRejectionReason.MovementCommandRejected));
                Assert.That(scenario.Controller.PendingCardChoice, Is.SameAs(pending));
                Assert.That(scenario.RedHero.Occupancy, Is.EqualTo(origin));
                Assert.That(redCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
                Assert.That(scenario.Controller.VisibleTurnActor.CardInstanceId, Is.EqualTo(redCard.Id));
                Assert.That(scenario.Controller.HighlightedCoordinates, Is.EqualTo(highlights));
            }
            finally
            {
                DestroyAll(objects);
            }
        }

        private static TurnScenario CreateTurnScenario(ICollection<GameObject> objects)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var redHero = CreateHero(
                objects,
                "Red Turn Hero",
                configuration.RedHand.Instances[0].OwnerId,
                new HexCoordinate(0, 0));
            var blueHero = CreateHero(
                objects,
                "Blue Turn Hero",
                configuration.BlueHand.Instances[0].OwnerId,
                new HexCoordinate(-2, 0));
            var tiles = CreateRadiusTwoCoordinates()
                .Select(coordinate => CreateTile(objects, coordinate))
                .ToArray();
            var obstacles = new[]
            {
                CreateObstacle(objects, "turn-east-blocker", new HexCoordinate(1, 0)),
                CreateObstacle(objects, "turn-south-east-blocker", new HexCoordinate(0, 1))
            };
            var controllerObject = new GameObject("In-turn Movement Controller");
            objects.Add(controllerObject);
            var controller = controllerObject.AddComponent<HeroSelectionController>();
            controller.Configure(
                redHero,
                tiles,
                CreateMaterial(Color.yellow),
                99,
                obstacles,
                new[] { blueHero });
            controller.ConfigurePlaceholderTestMatch(configuration);
            controller.ConfigureCardDrivenMovement(true);
            return new TurnScenario(redHero, blueHero, controller, configuration);
        }

        private static PrimitiveHeroView CreateHero(
            ICollection<GameObject> objects,
            string name,
            HeroId heroId,
            HexCoordinate coordinate)
        {
            var gameObject = new GameObject(name);
            objects.Add(gameObject);
            var hero = gameObject.AddComponent<PrimitiveHeroView>();
            hero.Configure(
                new HeroOccupancy(heroId, coordinate),
                HexWorldLayout.DefaultOuterRadius);
            return hero;
        }

        private static Scenario CreateScenario(
            ICollection<GameObject> objects,
            int legacyAllowance,
            bool withObstacles,
            TeamSide? tieBreakerSide = null)
        {
            var heroObject = new GameObject("Hero");
            objects.Add(heroObject);
            var hero = heroObject.AddComponent<PrimitiveHeroView>();
            hero.Configure(
                new HeroOccupancy(new HeroId("hero"), new HexCoordinate(0, 0)),
                HexWorldLayout.DefaultOuterRadius);

            var tiles = CreateRadiusTwoCoordinates()
                .Select(coordinate => CreateTile(objects, coordinate))
                .ToArray();
            var obstacles = withObstacles
                ? new[]
                {
                    CreateObstacle(objects, "east-blocker", new HexCoordinate(1, 0)),
                    CreateObstacle(objects, "south-east-blocker", new HexCoordinate(0, 1))
                }
                : new PlaceholderObstacleView[0];
            var controllerObject = new GameObject("Card Movement Controller");
            objects.Add(controllerObject);
            var controller = controllerObject.AddComponent<HeroSelectionController>();
            controller.Configure(hero, tiles, CreateMaterial(Color.yellow), legacyAllowance, obstacles);
            if (tieBreakerSide.HasValue)
            {
                controller.ConfigurePlaceholderTestMatch(
                    PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(
                        tieBreakerSide.Value));
            }

            controller.ConfigureCardDrivenMovement(true);

            return new Scenario(hero, tiles, obstacles, controller);
        }

        private static HexTileView CreateTile(
            ICollection<GameObject> objects,
            HexCoordinate coordinate)
        {
            var gameObject = new GameObject($"Tile {coordinate}");
            objects.Add(gameObject);
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial(Color.gray);
            var tile = gameObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, HexWorldLayout.DefaultOuterRadius);
            return tile;
        }

        private static PlaceholderObstacleView CreateObstacle(
            ICollection<GameObject> objects,
            string id,
            HexCoordinate coordinate)
        {
            var gameObject = new GameObject($"Obstacle {coordinate}");
            objects.Add(gameObject);
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial(Color.black);
            var obstacle = gameObject.AddComponent<PlaceholderObstacleView>();
            obstacle.Configure(
                new ObstacleOccupancy(new ObstacleId(id), coordinate),
                HexWorldLayout.DefaultOuterRadius);
            return obstacle;
        }

        private static Material CreateMaterial(Color color)
        {
            return new Material(Shader.Find("Standard")) { color = color };
        }

        private static IEnumerable<HexCoordinate> CreateRadiusTwoCoordinates()
        {
            const int radius = 2;
            for (var q = -radius; q <= radius; q++)
            {
                var minimumR = System.Math.Max(-radius, -q - radius);
                var maximumR = System.Math.Min(radius, -q + radius);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }

        private static void DestroyAll(IEnumerable<GameObject> objects)
        {
            foreach (var gameObject in objects)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private readonly struct Scenario
        {
            public Scenario(
                PrimitiveHeroView hero,
                HexTileView[] tiles,
                PlaceholderObstacleView[] obstacles,
                HeroSelectionController controller)
            {
                Hero = hero;
                Tiles = tiles;
                Obstacles = obstacles;
                Controller = controller;
            }

            public PrimitiveHeroView Hero { get; }
            public HexTileView[] Tiles { get; }
            public PlaceholderObstacleView[] Obstacles { get; }
            public HeroSelectionController Controller { get; }
        }

        private readonly struct TurnScenario
        {
            public TurnScenario(
                PrimitiveHeroView redHero,
                PrimitiveHeroView blueHero,
                HeroSelectionController controller,
                PlaceholderOneVsOneTestConfiguration configuration)
            {
                RedHero = redHero;
                BlueHero = blueHero;
                Controller = controller;
                Configuration = configuration;
            }

            public PrimitiveHeroView RedHero { get; }

            public PrimitiveHeroView BlueHero { get; }

            public HeroSelectionController Controller { get; }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
        }
    }
}
