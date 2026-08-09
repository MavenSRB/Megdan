using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class PlaceholderObstacleViewTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Configure_PlacesViewFromAuthoritativeObstacleOccupancy()
        {
            var gameObject = new GameObject("Obstacle Test");

            try
            {
                var view = gameObject.AddComponent<PlaceholderObstacleView>();
                var occupancy = new ObstacleOccupancy(new ObstacleId("test-obstacle"), new HexCoordinate(1, -1));

                view.Configure(occupancy, 1.25f);

                var expectedPosition = HexWorldLayout.ToWorld(occupancy.Coordinate, 1.25f) +
                    (Vector3.up * view.VerticalOffset);
                Assert.That(view.Occupancy, Is.EqualTo(occupancy));
                Assert.That(Vector3.Distance(view.transform.localPosition, expectedPosition), Is.LessThan(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EnsurePrimitiveVisual_CreatesVisiblePlaceholderMesh()
        {
            var gameObject = new GameObject("Obstacle Test");

            try
            {
                var view = gameObject.AddComponent<PlaceholderObstacleView>();

                view.EnsurePrimitiveVisual();

                Assert.That(gameObject.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
                Assert.That(gameObject.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.Null);
                Assert.That(gameObject.GetComponent<MeshRenderer>().enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
