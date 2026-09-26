using System.Reflection;
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

        [Test]
        public void PreparedTurretFace_PointsItsRenderedNormalTowardTheCamera()
        {
            var gameObject = new GameObject("Turret Test");
            var cameraObject = new GameObject("Turret Camera");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(3f, 8f, -4f);
                var view = gameObject.AddComponent<PlaceholderObstacleView>();
                view.Configure(new ObstacleOccupancy(
                    new ObstacleId("trinkets-turret:hero-trinkets"),
                    new HexCoordinate(0, 0)), 1f);

                var face = gameObject.transform.Find("Obstacle Standee");
                Assert.That(face, Is.Not.Null,
                    "The prepared Turret resource must replace the fallback block in this checkout.");
                typeof(PlaceholderObstacleView).GetMethod("LateUpdate",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                var localNormal = face.GetComponent<MeshFilter>().sharedMesh.normals[0];
                var renderedNormal = face.TransformDirection(localNormal).normalized;
                Assert.That(Camera.main, Is.Not.Null);
                var towardCamera = (Camera.main.transform.position - face.position).normalized;

                Assert.That(Vector3.Dot(renderedNormal, towardCamera), Is.GreaterThan(0.999f),
                    "Unity's Quad renders its local -Z face; that face must point at the camera.");
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
