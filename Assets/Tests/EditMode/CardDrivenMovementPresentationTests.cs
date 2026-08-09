using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
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
                    offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
                var secondary = offers.Single(offer =>
                    offer.InvocationContext == ActionInvocationContext.PrintedSecondary);
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
        public void PrimaryOffer_EarlyDestinationCommitsAuthorityRouteAndClearsSelections()
        {
            var objects = new List<GameObject>();
            try
            {
                var scenario = CreateScenario(objects, 99, true);
                var destination = new HexCoordinate(-1, 0);
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
                Assert.That(scenario.Controller.TrySubmitCardDestination(destination), Is.True);

                Assert.That(scenario.Hero.Coordinate, Is.EqualTo(destination));
                Assert.That(scenario.Controller.CurrentHeroOccupancy.Coordinate, Is.EqualTo(destination));
                Assert.That(scenario.Controller.CardMovementRoute,
                    Is.EqualTo(new[] { new HexCoordinate(0, 0), destination }));
                Assert.That(scenario.Controller.RemainingMovementAllowance, Is.EqualTo(2));
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

        private static Scenario CreateScenario(
            ICollection<GameObject> objects,
            int legacyAllowance,
            bool withObstacles)
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
    }
}
