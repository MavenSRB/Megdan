using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MinimalHexGridSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Milestone1_2_HexGrid.unity";
        private const float Tolerance = 0.0001f;

        [Test]
        public void Scene_ContainsOriginAndItsSixLogicalNeighborsAtDerivedPositions()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var origin = new HexCoordinate(0, 0);
            var expectedCoordinates = new HashSet<HexCoordinate>(origin.GetNeighbors())
            {
                origin
            };

            Assert.That(tiles, Has.Length.EqualTo(7));
            Assert.That(tiles.Select(tile => tile.Coordinate), Is.EquivalentTo(expectedCoordinates));

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
        public void Scene_UsesOneFixedTopDownCameraAndNoCameraController()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].name, Is.EqualTo("Fixed Camera"));
            Assert.That(cameras[0].GetComponents<MonoBehaviour>(), Is.Empty);
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
    }
}
