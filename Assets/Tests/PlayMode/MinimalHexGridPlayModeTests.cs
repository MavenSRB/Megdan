using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
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
        public IEnumerator Scene_LoadsOneVisiblePrimitiveHero()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);

            Assert.That(heroes, Has.Length.EqualTo(1));
            Assert.That(heroes[0].GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(heroes[0].GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
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

            var hero = Object.FindFirstObjectByType<PrimitiveHeroView>();
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
            var moved = pointerInput.TryHandlePointerRay(CreateDownRay(destinationTile.transform.position));
            var activeAfterMove = controller.IsMovementSessionActive;
            var remainingAfterMove = controller.RemainingMovementAllowance;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(selected, Is.True);
            Assert.That(selectedAction, Is.True);
            var expectedDestinations = MovementReachabilityQuery.FindReachableDestinations(
                    originOccupancy,
                    tiles.Select(tile => tile.Coordinate),
                    new[] { originOccupancy },
                    obstacles.Select(obstacle => obstacle.Occupancy),
                    controller.LastCardMovementAllowance)
                .Select(destination => destination.Destination)
                .ToArray();
            Assert.That(highlightedCoordinates, Is.EquivalentTo(expectedDestinations));
            Assert.That(highlightedCoordinates, Has.No.Member(origin));
            Assert.That(highlightedCoordinates, Has.No.Member(new HexCoordinate(1, 0)));
            Assert.That(highlightedCoordinates, Has.No.Member(new HexCoordinate(0, 1)));
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

        private static Ray CreateDownRay(Vector3 target)
        {
            return new Ray(target + (Vector3.up * 2f), Vector3.down);
        }
    }
}
