using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
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
        public IEnumerator Scene_ClicksHeroThenHighlightedDestinationAndStopsMovementSession()
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
            var origin = hero.Coordinate;
            var destination = origin.GetNeighbor(HexDirection.East);
            var destinationTile = tiles.Single(tile => tile.Coordinate.Equals(destination));

            var selected = pointerInput.TryHandlePointerRay(CreateDownRay(hero.transform.position));
            var highlightedCoordinates = tiles
                .Where(tile => tile.IsHighlighted)
                .Select(tile => tile.Coordinate)
                .ToArray();
            var moved = pointerInput.TryHandlePointerRay(CreateDownRay(destinationTile.transform.position));
            var activeAfterMove = controller.IsMovementSessionActive;
            var remainingAfterMove = controller.RemainingMovementAllowance;
            Physics.SyncTransforms();
            yield return null;

            var stopped = pointerInput.TryHandlePointerRay(CreateDownRay(hero.transform.position));

            Assert.That(selected, Is.True);
            Assert.That(highlightedCoordinates, Is.EquivalentTo(origin.GetNeighbors()));
            Assert.That(highlightedCoordinates, Has.Length.EqualTo(6));
            Assert.That(highlightedCoordinates, Has.No.Member(origin));
            Assert.That(moved, Is.True);
            Assert.That(activeAfterMove, Is.True);
            Assert.That(remainingAfterMove, Is.EqualTo(HeroSelectionController.DefaultMovementAllowance - 1));
            Assert.That(controller.IsMovementSessionActive, Is.False);
            Assert.That(stopped, Is.True);
            Assert.That(hero.Coordinate, Is.EqualTo(destination));
            Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
        }

        private static Ray CreateDownRay(Vector3 target)
        {
            return new Ray(target + (Vector3.up * 2f), Vector3.down);
        }
    }
}
