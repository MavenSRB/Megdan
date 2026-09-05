using System.Linq;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z12. The NINETEENTH AMENDMENT's "give tokens a bit of depth".
    ///
    /// <para>The whole feature rests on three claims, and each is asserted here rather than looked at:
    /// the piece's silhouette from directly above does not move (so the standing top-down orthographic
    /// board is UNCHANGED and no perf or layout answer is disturbed), the piece is exactly one depth
    /// tall, and the new wall faces OUTWARD - which matters because Unity culls back faces, so the
    /// mirror of that winding is not a subtle shading difference but a piece with no visible side at
    /// all, in the one view nobody looks at until a camera tilts.</para>
    /// </summary>
    public sealed class BoardPieceDepthTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void ASolidPiece_KeepsTheFlatOnesSilhouetteExactly()
        {
            var flat = Hexagon(1.4f);
            var solid = BoardPieceDepth.ExtrudeFan(
                flat.Vertices, flat.TextureCoordinates, flat.Triangles, BoardPieceDepth.Standard);

            var flatFootprint = Footprint(flat.Vertices);
            var solidFootprint = Footprint(solid.Vertices);

            Assert.That(solidFootprint.Length, Is.EqualTo(flatFootprint.Length),
                "Every added vertex shares an existing vertex's x and z, so the shape seen from " +
                "straight above has exactly the same distinct points.");
            foreach (var point in flatFootprint)
            {
                Assert.That(
                    solidFootprint.Any(candidate => Vector2.Distance(candidate, point) <= Tolerance),
                    Is.True,
                    "The silhouette moved, so the standing board view is not unchanged after all.");
            }
        }

        [Test]
        public void ASolidPiece_IsExactlyOneDepthTallAndKeepsAFullRingOnTheBoardPlane()
        {
            var flat = Hexagon(1f);
            var solid = BoardPieceDepth.ExtrudeFan(
                flat.Vertices, flat.TextureCoordinates, flat.Triangles, BoardPieceDepth.Standard);

            Assert.That(solid.Vertices.Min(vertex => vertex.y), Is.EqualTo(0f).Within(Tolerance),
                "It grows UPWARD: downward would push the piece through the tiles, their debug " +
                "edges, and the artwork underlay beneath both.");
            Assert.That(
                solid.Vertices.Max(vertex => vertex.y),
                Is.EqualTo(BoardPieceDepth.Standard).Within(Tolerance));

            // The target ring fits itself to the convex contour of a mesh's LOWEST vertices, so a
            // full perimeter must still stand on the board plane at the original radius or every
            // fitted cue on this piece silently changes shape.
            var lowest = solid.Vertices.Where(vertex => Mathf.Abs(vertex.y) <= Tolerance).ToArray();
            Assert.That(lowest.Length, Is.EqualTo(6));
            Assert.That(
                lowest.All(vertex => Mathf.Abs(new Vector2(vertex.x, vertex.z).magnitude - 1f) <=
                    Tolerance),
                Is.True,
                "The ring the cue is measured from is the same ring at the same radius.");
        }

        [Test]
        public void ASolidPieceWallsFaceOutward()
        {
            var flat = Hexagon(1f);
            var solid = BoardPieceDepth.ExtrudeFan(
                flat.Vertices, flat.TextureCoordinates, flat.Triangles, BoardPieceDepth.Standard);

            var wallStart = flat.Triangles.Length;
            Assert.That(solid.Triangles.Length - wallStart, Is.EqualTo(6 * 6),
                "Six sides, two triangles each.");

            for (var index = wallStart; index < solid.Triangles.Length; index += 3)
            {
                var a = solid.Vertices[solid.Triangles[index]];
                var b = solid.Vertices[solid.Triangles[index + 1]];
                var c = solid.Vertices[solid.Triangles[index + 2]];
                // Unity is left-handed and culls counter-clockwise faces, so its geometric normal is
                // cross(b - a, c - a) with the left-handed cross product - which is what
                // Vector3.Cross computes.
                var normal = Vector3.Cross(b - a, c - a);
                var centre = (a + b + c) / 3f;
                var outward = new Vector3(centre.x, 0f, centre.z);

                Assert.That(Vector3.Dot(normal.normalized, outward.normalized),
                    Is.GreaterThan(0.9f),
                    "A wall wound the other way is invisible from outside the piece, and the only " +
                    "camera that would ever have shown it is the tilted one this depth exists for.");
            }
        }

        [Test]
        public void ATopFaceKeepsTheFlatFansOwnTrianglesAndTextureCoordinates()
        {
            var flat = Hexagon(1f);
            var solid = BoardPieceDepth.ExtrudeFan(
                flat.Vertices, flat.TextureCoordinates, flat.Triangles, BoardPieceDepth.Standard);

            for (var index = 0; index < flat.Triangles.Length; index++)
            {
                Assert.That(solid.Triangles[index], Is.EqualTo(flat.Triangles[index]),
                    "The face a player looks at is the same face, drawn at a different height.");
            }

            for (var index = 0; index < flat.TextureCoordinates.Length; index++)
            {
                Assert.That(solid.TextureCoordinates[index],
                    Is.EqualTo(flat.TextureCoordinates[index]),
                    "A printed token face must not be re-mapped by being made solid.");
            }
        }

        [Test]
        public void ADepthThatIsNotAPositiveNumberIsRefusedRatherThanDrawnFlat()
        {
            var flat = Hexagon(1f);

            Assert.That(
                () => BoardPieceDepth.ExtrudeFan(
                    flat.Vertices, flat.TextureCoordinates, flat.Triangles, 0f),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(
                () => BoardPieceDepth.ExtrudeFan(
                    new[] { Vector3.zero, Vector3.right },
                    System.Array.Empty<Vector2>(),
                    new[] { 0, 1, 1 },
                    BoardPieceDepth.Standard),
                Throws.ArgumentException,
                "A fan needs a centre and at least three perimeter vertices.");
        }

        /// <summary>
        /// The same flat hex <see cref="FlatHexMeshBuilder.Create"/> builds, as arrays, so the
        /// extrusion can be proven with no engine object anywhere in the case.
        /// </summary>
        private static ExtrudedFan Hexagon(float outerRadius)
        {
            var vertices = new Vector3[7];
            var uv = new Vector2[7];
            var triangles = new int[18];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (var side = 0; side < 6; side++)
            {
                var angle = Mathf.Deg2Rad * (30f + (side * 60f));
                var x = Mathf.Cos(angle) * outerRadius;
                var z = Mathf.Sin(angle) * outerRadius;
                vertices[side + 1] = new Vector3(x, 0f, z);
                uv[side + 1] = new Vector2(
                    (x / (outerRadius * 2f)) + 0.5f, (z / (outerRadius * 2f)) + 0.5f);
                triangles[side * 3] = 0;
                triangles[(side * 3) + 1] = ((side + 1) % 6) + 1;
                triangles[(side * 3) + 2] = side + 1;
            }

            return new ExtrudedFan(vertices, uv, null, triangles);
        }

        private static Vector2[] Footprint(Vector3[] vertices)
        {
            var points = new System.Collections.Generic.List<Vector2>();
            foreach (var vertex in vertices)
            {
                var point = new Vector2(vertex.x, vertex.z);
                if (!points.Any(existing => Vector2.Distance(existing, point) <= Tolerance))
                {
                    points.Add(point);
                }
            }

            return points.ToArray();
        }
    }
}
