using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class MinimalHexGridPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_LoadsExpandedVisibleHexTilesUnderFixedCamera()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

            Assert.That(tiles, Has.Length.EqualTo(19));
            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(tiles, Has.All.Matches<HexTileView>(tile =>
            {
                var edgeRenderer = tile.GetComponentInChildren<LineRenderer>();
                return tile.GetComponent<MeshRenderer>().enabled &&
                       tile.GetComponent<MeshFilter>().sharedMesh != null &&
                       edgeRenderer != null &&
                       edgeRenderer.enabled;
            }));
        }

        [UnityTest]
        public IEnumerator Scene_LoadsVisibleRedAndBluePrimitiveHeroes()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);

            Assert.That(heroes, Has.Length.EqualTo(2));
            Assert.That(
                heroes.Select(hero => hero.Occupancy.HeroId.Value),
                Is.EquivalentTo(new[]
                {
                    PlaceholderOneVsOneParticipants.RedHeroIdValue,
                    PlaceholderOneVsOneParticipants.BlueHeroIdValue
                }));
            Assert.That(heroes, Has.All.Matches<PrimitiveHeroView>(hero =>
                hero.GetComponent<MeshRenderer>().enabled &&
                hero.GetComponent<MeshFilter>().sharedMesh != null));

            var redHero = FindRedHero(heroes);
            var blueHero = heroes.Single(hero => hero != redHero);
            Assert.That(redHero.TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(blueHero.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(redHero.GetComponent<MeshRenderer>().sharedMaterial.color.r,
                Is.GreaterThan(redHero.GetComponent<MeshRenderer>().sharedMaterial.color.b));
            Assert.That(blueHero.GetComponent<MeshRenderer>().sharedMaterial.color.b,
                Is.GreaterThan(blueHero.GetComponent<MeshRenderer>().sharedMaterial.color.r));
            Assert.That(redHero.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.Not.EqualTo(blueHero.GetComponent<MeshFilter>().sharedMesh.vertexCount));
        }

        [UnityTest]
        public IEnumerator Scene_SelectsVisibleCardOfferThenClicksReachableDestinationInOneClick()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);
            var hero = FindRedHero(heroes);
            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var obstacles = Object.FindObjectsByType<PlaceholderObstacleView>(FindObjectsSortMode.None);
            var origin = hero.Coordinate;
            var originOccupancy = hero.Occupancy;
            var destination = new HexCoordinate(2, 0);
            var destinationTile = tiles.Single(tile => tile.Coordinate.Equals(destination));

            var selectedCard = controller.VisibleCardInstances[0];
            var selected = controller.TrySelectCard(selectedCard.Id);
            var selectedAction = controller.TrySelectAction(
                new ActionDefinitionId("primary-movement"));
            var highlightedCoordinates = tiles
                .Where(tile => tile.IsHighlighted)
                .Select(tile => tile.Coordinate)
                .ToArray();
            var expectedDestinations = MovementReachabilityQuery.FindReachableDestinations(
                    originOccupancy,
                    tiles.Select(tile => tile.Coordinate),
                    heroes.Select(candidate => candidate.Occupancy),
                    obstacles.Select(obstacle => obstacle.Occupancy),
                    controller.LastCardMovementAllowance)
                .Select(reachable => reachable.Destination)
                .ToArray();
            var moved = pointerInput.TryHandlePointerRay(CreateDownRay(destinationTile.transform.position));
            var activeAfterMove = controller.IsMovementSessionActive;
            var remainingAfterMove = controller.RemainingMovementAllowance;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(selected, Is.True);
            Assert.That(selectedAction, Is.True);
            Assert.That(highlightedCoordinates, Is.EquivalentTo(expectedDestinations));
            Assert.That(highlightedCoordinates, Has.No.Member(origin));
            Assert.That(highlightedCoordinates, Has.No.Member(new HexCoordinate(1, 0)));
            Assert.That(highlightedCoordinates, Has.No.Member(new HexCoordinate(0, 1)));
            Assert.That(highlightedCoordinates, Has.No.Member(heroes.Single(candidate => candidate != hero).Coordinate));
            Assert.That(moved, Is.True);
            Assert.That(activeAfterMove, Is.False);
            Assert.That(remainingAfterMove, Is.EqualTo(0));
            Assert.That(controller.IsMovementSessionActive, Is.False);
            Assert.That(controller.MovementRoute, Is.EqualTo(new[]
            {
                origin,
                new HexCoordinate(1, -1),
                new HexCoordinate(2, -1),
                destination
            }));
            Assert.That(hero.Coordinate, Is.EqualTo(destination));
            Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
        }

        [UnityTest]
        public IEnumerator Scene_SelectsBlueHeroAndMovesItWithItsOwnCard()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);
            var redHero = FindRedHero(heroes);
            var blueHero = heroes.Single(hero => hero != redHero);
            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var redCardIds = controller.VisibleCardInstances.Select(card => card.Id).ToArray();
            var blueOrigin = blueHero.Coordinate;

            Assert.That(controller.ActiveHeroView, Is.SameAs(redHero));
            Assert.That(redCardIds.All(id => id.Value.StartsWith("red-")), Is.True);
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(blueHero.transform.position)), Is.True);

            var blueCards = controller.VisibleCardInstances.ToArray();
            Assert.That(controller.ActiveHeroView, Is.SameAs(blueHero));
            Assert.That(blueCards.All(card => card.OwnerId.Equals(blueHero.Occupancy.HeroId)), Is.True);
            Assert.That(blueCards.All(card => card.Id.Value.StartsWith("blue-")), Is.True);
            Assert.That(blueCards.Select(card => card.Id).Intersect(redCardIds), Is.Empty);
            Assert.That(controller.TrySelectCard(blueCards[0].Id), Is.True);
            Assert.That(controller.TrySelectAction(
                new ActionDefinitionId("secondary-movement")), Is.True);
            Assert.That(controller.PendingCardChoice.Origin, Is.EqualTo(blueOrigin));
            Assert.That(controller.HighlightedCoordinates, Has.No.Member(redHero.Coordinate));

            var destination = new HexCoordinate(1, -1);
            var destinationTile = tiles.Single(tile => tile.Coordinate.Equals(destination));
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(destinationTile.transform.position)), Is.True);

            Physics.SyncTransforms();
            yield return null;

            Assert.That(blueHero.Coordinate, Is.EqualTo(destination));
            Assert.That(redHero.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(controller.IsCardResolutionPending, Is.False);
            Assert.That(controller.MovementRoute, Is.EqualTo(new[] { blueOrigin, destination }));
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(redHero.transform.position)), Is.True);
            Assert.That(controller.ActiveHeroView, Is.SameAs(redHero));
            Assert.That(controller.VisibleCardInstances.Select(card => card.Id), Is.EquivalentTo(redCardIds));
        }

        [UnityTest]
        public IEnumerator Scene_SelectsVisibleAttackAndClicksAdjacentEnemyHero()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);
            var redHero = FindRedHero(heroes);
            var blueHero = heroes.Single(hero => hero != redHero);
            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            var routeDrill = controller.VisibleCardInstances.Single(card =>
                card.Id.Value == "red-route-drill-card");

            Assert.That(controller.TrySelectCard(routeDrill.Id), Is.True);
            Assert.That(
                controller.SelectedCardOffers.Select(offer => offer.ActionDefinitionId.Value),
                Has.Member("primary-attack"));
            Assert.That(controller.TrySelectAction(new ActionDefinitionId("primary-attack")), Is.True);
            Assert.That(controller.IsAttackTargetPending, Is.True);

            var targeted = pointerInput.TryHandlePointerRay(CreateDownRay(blueHero.transform.position));
            Physics.SyncTransforms();
            yield return null;

            Assert.That(targeted, Is.True);
            Assert.That(controller.ActiveHeroView, Is.SameAs(redHero));
            Assert.That(controller.IsDefenseResponsePending, Is.True);
            Assert.That(controller.PendingDefenseChoice.ChooserId, Is.EqualTo(blueHero.Occupancy.HeroId));
            Assert.That(controller.PendingDefenseChoice.AttackTotal, Is.EqualTo(3));
            Assert.That(controller.CardStatus, Does.Contain("Defense response pending"));
            Assert.That(redHero.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(blueHero.Coordinate, Is.EqualTo(new HexCoordinate(0, -1)));
        }

        [UnityTest]
        public IEnumerator Scene_ClicksTwoPartialMovementWaypointsThenStopsWithAllowanceRemaining()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var hero = FindRedHero(Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None));
            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var origin = hero.Coordinate;
            var firstDestination = new HexCoordinate(-1, 0);
            var secondDestination = new HexCoordinate(-2, 0);

            Assert.That(controller.TrySelectCard(controller.VisibleCardInstances[0].Id), Is.True);
            Assert.That(controller.TrySelectAction(new ActionDefinitionId("primary-movement")), Is.True);
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(
                tiles.Single(tile => tile.Coordinate.Equals(firstDestination)).transform.position)), Is.True);
            Assert.That(controller.IsMovementSessionActive, Is.True);
            Assert.That(controller.RemainingMovementAllowance, Is.EqualTo(2));
            Assert.That(controller.PendingCardChoice.Origin, Is.EqualTo(firstDestination));
            Assert.That(tiles.Where(tile => tile.IsHighlighted).Select(tile => tile.Coordinate),
                Is.EquivalentTo(controller.PendingCardDestinations.Select(destination => destination.Destination)));

            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(
                tiles.Single(tile => tile.Coordinate.Equals(secondDestination)).transform.position)), Is.True);
            Assert.That(hero.Coordinate, Is.EqualTo(secondDestination));
            Assert.That(controller.IsMovementSessionActive, Is.True);
            Assert.That(controller.RemainingMovementAllowance, Is.EqualTo(1));
            Assert.That(controller.MovementRoute, Is.EqualTo(new[]
            {
                origin,
                firstDestination,
                secondDestination
            }));
            Assert.That(controller.TryStopCardMovement(), Is.True);

            Physics.SyncTransforms();
            yield return null;

            Assert.That(hero.Coordinate, Is.EqualTo(secondDestination));
            Assert.That(controller.IsMovementSessionActive, Is.False);
            Assert.That(controller.RemainingMovementAllowance, Is.EqualTo(1));
            Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
        }

        private static Ray CreateDownRay(Vector3 target)
        {
            return new Ray(target + (Vector3.up * 2f), Vector3.down);
        }

        private static PrimitiveHeroView FindRedHero(PrimitiveHeroView[] heroes)
        {
            return heroes.Single(hero =>
                hero.Occupancy.HeroId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue);
        }
    }
}
