using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MinimalHexGridSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Milestone1_2_HexGrid.unity";
        private const int ExpandedGridRadius = 2;
        private const int ExpandedGridTileCount = 19;
        private const float Tolerance = 0.0001f;

        [Test]
        public void Scene_ContainsExpandedPlaceholderGridAtDerivedPositions()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Object.FindFirstObjectByType<PlaceholderTestGridView>().EnsureGrid();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var origin = new HexCoordinate(0, 0);
            var expectedCoordinates = CreateRadiusTwoCoordinates();

            Assert.That(tiles, Has.Length.EqualTo(ExpandedGridTileCount));
            Assert.That(tiles.Select(tile => tile.Coordinate), Is.EquivalentTo(expectedCoordinates));
            Assert.That(tiles.Select(tile => tile.Coordinate), Is.SupersetOf(origin.GetNeighbors()));
            Assert.That(tiles.Select(tile => tile.Coordinate), Does.Contain(origin));

            foreach (var tile in tiles)
            {
                var expectedPosition = HexWorldLayout.ToWorld(tile.Coordinate, tile.OuterRadius);
                Assert.That(
                    Vector3.Distance(tile.transform.localPosition, expectedPosition),
                    Is.LessThan(Tolerance),
                    $"Tile {tile.Coordinate} is not at its derived logical position.");
                Assert.That(tile.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
                Assert.That(tile.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.Null);
            }
        }

        [Test]
        public void Scene_ExpandedGridSupportsDistancesAndAlternateRoutes()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Object.FindFirstObjectByType<PlaceholderTestGridView>().EnsureGrid();
            var coordinates = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                .Select(tile => tile.Coordinate)
                .ToHashSet();

            Assert.That(coordinates, Has.Member(new HexCoordinate(0, 0)));
            Assert.That(coordinates, Has.Member(new HexCoordinate(1, 0)));
            Assert.That(HexDistance(new HexCoordinate(0, 0), new HexCoordinate(1, 0)), Is.EqualTo(1));

            Assert.That(coordinates, Has.Member(new HexCoordinate(-2, 0)));
            Assert.That(coordinates, Has.Member(new HexCoordinate(0, -1)));
            Assert.That(HexDistance(new HexCoordinate(-2, 0), new HexCoordinate(0, -1)), Is.EqualTo(2));

            Assert.That(coordinates, Has.Member(new HexCoordinate(1, 0)));
            Assert.That(HexDistance(new HexCoordinate(-2, 0), new HexCoordinate(1, 0)), Is.EqualTo(3));

            Assert.That(coordinates, Has.Member(new HexCoordinate(-1, 0)));
            Assert.That(coordinates, Has.Member(new HexCoordinate(-1, -1)));
            Assert.That(new HexCoordinate(-2, 0).GetNeighbors(), Has.Member(new HexCoordinate(-1, 0)));
            Assert.That(new HexCoordinate(-1, 0).GetNeighbors(), Has.Member(new HexCoordinate(0, -1)));
            Assert.That(new HexCoordinate(-2, 0).GetNeighbors(), Has.Member(new HexCoordinate(-1, -1)));
            Assert.That(new HexCoordinate(-1, -1).GetNeighbors(), Has.Member(new HexCoordinate(0, -1)));
        }

        [Test]
        public void Scene_UsesOneFixedTopDownCameraWithPointerInputAndNoCameraController()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].name, Is.EqualTo("Fixed Camera"));
            Assert.That(cameras[0].GetComponents<MonoBehaviour>(), Has.All.TypeOf<PrimitiveHeroPointerInput>());
            Assert.That(cameras[0].transform.position.y, Is.GreaterThan(0f));
            Assert.That(
                new Vector2(cameras[0].transform.position.x, cameras[0].transform.position.z).magnitude,
                Is.LessThan(Tolerance));
            Assert.That(Vector3.Angle(cameras[0].transform.forward, Vector3.down), Is.LessThan(Tolerance));
        }

        [Test]
        public void Scene_UsesOneLightGrayFillMaterialForEveryTile()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Object.FindFirstObjectByType<PlaceholderTestGridView>().EnsureGrid();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var fillMaterials = tiles
                .Select(tile => tile.GetComponent<MeshRenderer>().sharedMaterial)
                .Distinct()
                .ToArray();

            Assert.That(fillMaterials, Has.Length.EqualTo(1));
            Assert.That(Mathf.Abs(fillMaterials[0].color.r - fillMaterials[0].color.g), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(fillMaterials[0].color.g - fillMaterials[0].color.b), Is.LessThan(0.05f));
            Assert.That(fillMaterials[0].color.grayscale, Is.GreaterThan(0.6f));
        }

        [Test]
        public void Scene_UsesOneDarkerGrayClosedEdgeForEveryTile()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Object.FindFirstObjectByType<PlaceholderTestGridView>().EnsureGrid();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var fillColor = tiles[0].GetComponent<MeshRenderer>().sharedMaterial.color;
            var edgeRenderers = tiles.Select(tile => tile.GetComponentInChildren<LineRenderer>()).ToArray();

            Assert.That(edgeRenderers, Has.All.Not.Null);
            Assert.That(edgeRenderers.All(edge => edge.loop && edge.positionCount == 6), Is.True);
            Assert.That(edgeRenderers.Select(edge => edge.sharedMaterial).Distinct().Count(), Is.EqualTo(1));

            var edgeColor = edgeRenderers[0].sharedMaterial.color;
            Assert.That(Mathf.Abs(edgeColor.r - edgeColor.g), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(edgeColor.g - edgeColor.b), Is.LessThan(0.05f));
            Assert.That(edgeColor.grayscale, Is.LessThan(fillColor.grayscale - 0.25f));
        }

        [Test]
        public void Scene_ContainsOnePrimitiveHeroAtItsAuthoritativeOccupiedCoordinate()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Object.FindFirstObjectByType<PlaceholderTestGridView>().EnsureGrid();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);

            Assert.That(heroes, Has.Length.EqualTo(1));

            var hero = heroes[0];
            var expectedPosition = HexWorldLayout.ToWorld(hero.Occupancy.Coordinate, hero.OuterRadius) +
                (Vector3.up * hero.VerticalOffset);

            Assert.That(hero.Occupancy.HeroId.Value, Is.EqualTo(PrimitiveHeroView.DefaultHeroId));
            Assert.That(tiles.Select(tile => tile.Coordinate), Does.Contain(hero.Occupancy.Coordinate));
            Assert.That(
                Vector3.Distance(hero.transform.localPosition, expectedPosition),
                Is.LessThan(Tolerance));
        }

        [Test]
        public void Scene_ContainsVisiblePlaceholderObstaclesOnUniqueEmptySpaces()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Object.FindFirstObjectByType<PlaceholderTestGridView>().EnsureGrid();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var hero = Object.FindFirstObjectByType<PrimitiveHeroView>();
            var obstacles = Object.FindObjectsByType<PlaceholderObstacleView>(FindObjectsSortMode.None);
            var expectedCoordinates = new[] { new HexCoordinate(1, 0), new HexCoordinate(0, 1) };

            Assert.That(obstacles, Has.Length.EqualTo(2));
            Assert.That(obstacles.Select(obstacle => obstacle.Coordinate), Is.EquivalentTo(expectedCoordinates));
            Assert.That(obstacles.Select(obstacle => obstacle.Coordinate), Has.No.Member(hero.Coordinate));
            Assert.That(obstacles.Select(obstacle => obstacle.Coordinate).Distinct().Count(), Is.EqualTo(obstacles.Length));
            Assert.That(tiles.Select(tile => tile.Coordinate), Is.SupersetOf(expectedCoordinates));
            Assert.That(obstacles, Has.All.Matches<PlaceholderObstacleView>(obstacle =>
                obstacle.GetComponent<MeshRenderer>().enabled &&
                obstacle.GetComponent<MeshFilter>().sharedMesh != null));
        }


        [Test]
        public void Scene_ContainsInputSystemPointerSelectionPath()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controllers = Object.FindObjectsByType<HeroSelectionController>(FindObjectsSortMode.None);
            var selectors = Object.FindObjectsByType<PrimitiveHeroPointerSelector>(FindObjectsSortMode.None);
            var inputs = Object.FindObjectsByType<PrimitiveHeroPointerInput>(FindObjectsSortMode.None);
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);

            Assert.That(controllers, Has.Length.EqualTo(1));
            Assert.That(selectors, Has.Length.EqualTo(1));
            Assert.That(inputs, Has.Length.EqualTo(1));
            Assert.That(controllers[0].MovementAllowance, Is.GreaterThan(1));
            Assert.That(controllers[0].IsCardDrivenMovementEnabled, Is.True);
            Assert.That(controllers[0].VisibleCardInstances.Count, Is.EqualTo(2));
            Assert.That(selectors[0].SelectionController, Is.SameAs(controllers[0]));
            Assert.That(inputs[0].EventCamera, Is.Not.Null);
            Assert.That(inputs[0].Selectors, Has.Member(selectors[0]));
            Assert.That(heroes[0].GetComponent<CapsuleCollider>(), Is.Not.Null);
        }

        [Test]
        public void Scene_SelectingPrimaryMovementOfferHighlightsAuthoritativeCardDestinations()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Object.FindFirstObjectByType<PlaceholderTestGridView>().EnsureGrid();
            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var hero = Object.FindFirstObjectByType<PrimitiveHeroView>();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);

            var selectedCard = controller.VisibleCardInstances[0];
            Assert.That(controller.TrySelectCard(selectedCard.Id), Is.True);
            Assert.That(controller.TrySelectAction(
                new ActionDefinitionId("primary-movement")), Is.True);

            var highlightedCoordinates = tiles
                .Where(tile => tile.IsHighlighted)
                .Select(tile => tile.Coordinate)
                .ToArray();

            Assert.That(controller.IsHeroSelected, Is.True);
            var obstacles = Object.FindObjectsByType<PlaceholderObstacleView>(FindObjectsSortMode.None);
            var expectedDestinations = MovementReachabilityQuery.FindReachableDestinations(
                    hero.Occupancy,
                    tiles.Select(tile => tile.Coordinate),
                    new[] { hero.Occupancy },
                    obstacles.Select(obstacle => obstacle.Occupancy),
                    controller.LastCardMovementAllowance)
                .Select(destination => destination.Destination)
                .ToArray();

            Assert.That(controller.HighlightedCoordinates, Is.EquivalentTo(expectedDestinations));
            Assert.That(highlightedCoordinates, Is.EquivalentTo(expectedDestinations));
            Assert.That(tiles.Single(tile => tile.Coordinate.Equals(hero.Coordinate)).IsHighlighted, Is.False);
            Assert.That(highlightedCoordinates, Has.No.Member(new HexCoordinate(1, 0)));
            Assert.That(highlightedCoordinates, Has.No.Member(new HexCoordinate(0, 1)));
        }

        private static HashSet<HexCoordinate> CreateRadiusTwoCoordinates()
        {
            var coordinates = new HashSet<HexCoordinate>();
            for (var q = -ExpandedGridRadius; q <= ExpandedGridRadius; q++)
            {
                var minimumR = Mathf.Max(-ExpandedGridRadius, -q - ExpandedGridRadius);
                var maximumR = Mathf.Min(ExpandedGridRadius, -q + ExpandedGridRadius);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    coordinates.Add(new HexCoordinate(q, r));
                }
            }

            return coordinates;
        }

        private static int HexDistance(HexCoordinate first, HexCoordinate second)
        {
            var firstS = -first.Q - first.R;
            var secondS = -second.Q - second.R;
            return (Mathf.Abs(first.Q - second.Q) + Mathf.Abs(first.R - second.R) + Mathf.Abs(firstS - secondS)) / 2;
        }
    }
}
