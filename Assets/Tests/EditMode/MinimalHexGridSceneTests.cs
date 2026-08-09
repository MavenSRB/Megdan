using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Presentation.Board;
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
        public void Scene_UsesOneFixedCameraAndNoCameraController()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].name, Is.EqualTo("Fixed Camera"));
            Assert.That(cameras[0].GetComponents<MonoBehaviour>(), Is.Empty);
        }
    }
}
