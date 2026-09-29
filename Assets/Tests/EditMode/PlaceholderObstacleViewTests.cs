using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Milestone 9.16, the user's pick: the Turret is its cut figure outlined in the board's thin
        /// black line, as wide as the hex tokens, upright and facing the camera, drawn with its alpha
        /// honoured - never the prepared standee disc, its whitened ring, or the black square the
        /// opaque Unlit material drew from the file's transparent corners.
        /// </summary>
        [Test]
        public void PreparedTurret_IsTheOutlinedCutFigureOnTheTokenFootprint()
        {
            // The test scene may already hold a tagged main camera; this case needs its own straight-
            // down one to be the camera the piece faces.
            var otherMainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
            foreach (var other in otherMainCameras) other.tag = "Untagged";
            var gameObject = new GameObject("Turret Test");
            var cameraObject = new GameObject("Turret Camera");

            try
            {
                cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0.6f, 30f, -0.4f);
                Assert.That(Camera.main.gameObject, Is.SameAs(cameraObject));
                var view = gameObject.AddComponent<PlaceholderObstacleView>();
                view.Configure(new ObstacleOccupancy(
                    new ObstacleId("trinkets-turret:hero-trinkets"),
                    new HexCoordinate(0, 0)), 1f);
                var face = gameObject.transform.Find("Obstacle Standee");
                Assert.That(face, Is.Not.Null,
                    "The prepared Turret resource must replace the fallback block in this checkout.");
                // Straight down, as the match camera looks, with the screen's up along world +Z.
                cameraObject.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                typeof(PlaceholderObstacleView).GetMethod("LateUpdate",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);

                // Footprint: as wide as the hex tokens, no taller than wide, centred on its space.
                var tokenWidth = 2f * PlacedTokenView.DiscRadiusFraction * Mathf.Cos(30f * Mathf.Deg2Rad);
                Assert.That(face.lossyScale.x, Is.EqualTo(tokenWidth).Within(Tolerance));
                Assert.That(face.lossyScale.y, Is.LessThanOrEqualTo(tokenWidth));
                Assert.That(new Vector2(face.position.x - gameObject.transform.position.x,
                    face.position.z - gameObject.transform.position.z).magnitude, Is.LessThan(Tolerance));

                // Facing the camera, and upright on screen rather than rolled by world up.
                var normal = face.TransformDirection(
                    face.GetComponent<MeshFilter>().sharedMesh.normals[0]).normalized;
                var towardCamera = (cameraObject.transform.position - face.position).normalized;
                Assert.That(Vector3.Dot(normal, towardCamera), Is.GreaterThan(0.999f));
                Assert.That(Vector3.Dot(face.up, cameraObject.transform.up), Is.GreaterThan(0.99f),
                    "The figure's top must point up the screen.");

                // Alpha honoured: a blended surface, not the opaque Unlit that drew a black square.
                var material = face.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(material.renderQueue, Is.GreaterThanOrEqualTo(3000));
                Assert.That(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True);
                Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));

                // No ring: not the prepared disc itself, a transparent border all round, and no
                // opaque whitened ground left anywhere in it.
                var texture = (Texture2D)material.mainTexture;
                Assert.That(texture, Is.Not.SameAs(Resources.Load<Texture2D>("Private/Tokens/trinkets-turret")));
                var pixels = texture.GetPixels32();
                int width = texture.width, height = texture.height;
                for (var x = 0; x < width; x++)
                {
                    Assert.That(pixels[x].a, Is.EqualTo(0));
                    Assert.That(pixels[((height - 1) * width) + x].a, Is.EqualTo(0));
                }

                for (var y = 0; y < height; y++)
                {
                    Assert.That(pixels[y * width].a, Is.EqualTo(0));
                    Assert.That(pixels[(y * width) + width - 1].a, Is.EqualTo(0));
                }

                var opaque = pixels.Count(pixel => pixel.a > 200);
                var whitened = pixels.Count(pixel => pixel.a > 200 &&
                    Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b)) > 225 &&
                    Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) - Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b)) < 25);
                Assert.That(whitened, Is.LessThan(opaque / 100), "The whitened standee ring or field survived.");

                // Outlined: along every row and column the figure's first solid pixel from either
                // side is the black line.
                for (var y = 0; y < height; y++)
                {
                    AssertLineAt(pixels, Enumerable.Range(0, width).Select(x => (y * width) + x));
                    AssertLineAt(pixels, Enumerable.Range(0, width).Reverse().Select(x => (y * width) + x));
                }

                for (var x = 0; x < width; x++)
                {
                    AssertLineAt(pixels, Enumerable.Range(0, height).Select(y => (y * width) + x));
                    AssertLineAt(pixels, Enumerable.Range(0, height).Reverse().Select(y => (y * width) + x));
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(gameObject);
                foreach (var other in otherMainCameras)
                    if (other != null) other.tag = "MainCamera";
            }
        }

        private static void AssertLineAt(Color32[] pixels, IEnumerable<int> scan)
        {
            foreach (var index in scan)
            {
                var pixel = pixels[index];
                if (pixel.a <= 128) continue;
                Assert.That(Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)), Is.LessThanOrEqualTo(40),
                    "The figure's edge must be the thin black outline.");
                return;
            }
        }
    }
}
