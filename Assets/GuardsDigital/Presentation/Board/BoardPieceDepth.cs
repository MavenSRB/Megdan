using System;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 8.35z12. How thick a flat board piece is, and the one way a flat generated
    /// disc becomes a solid one.
    ///
    /// <para><b>Why the board looks exactly the same afterwards.</b> The board camera is ORTHOGRAPHIC
    /// and looks straight down. An orthographic projection has no parallax, so raising a piece's face
    /// by <see cref="Standard"/> moves it by exactly zero pixels, and the wall added beneath that face
    /// projects to a line of zero width. The extrusion is therefore INVISIBLE from the standing board
    /// view and appears only when a camera tilts - which is the whole reason it can be permanent
    /// rather than something the spotlight switches on and off.</para>
    ///
    /// <para><b>Why upward and not downward.</b> Downward would push the piece through the tile plane
    /// at y = 0, its debug edges at y = 0.015, and the artwork underlay beneath both. Upward touches
    /// nothing: a hero token sits at y = 0.03 and a minion at y = 0.18, so a 0.16 skirt keeps a hero's
    /// face at 0.19 - level with the minions it never shares a space with - and every other piece keeps
    /// its own clearance exactly as milestone 8.11b set it.</para>
    ///
    /// <para><b>Why the target rings and their washes are undisturbed.</b>
    /// <see cref="BoardTargetFootprintKind.ConvexMeshBase"/> fits a ring to the convex contour of a
    /// mesh's LOWEST vertices, and every extrusion here keeps a full ring of vertices at the original
    /// y = 0 at the original radius. The hull it measures is therefore the same hull. The translucent
    /// wash sits at the renderer's <c>bounds.max.y</c>, so it rides up with the face - which is where
    /// a wash over a solid piece belongs.</para>
    /// </summary>
    public static class BoardPieceDepth
    {
        /// <summary>
        /// A flat piece's thickness in board units, where a hex's outer radius is 1.
        ///
        /// <para><b>Chosen against the tilt, not by eye.</b> Under an orthographic camera a tilt of t
        /// reveals exactly <c>depth * sin(t)</c> of a piece's side
        /// (<c>AttackSpotlightFraming.VisibleSideHeight</c>). At the spotlight's 16 degrees that is
        /// 0.044 board units against a hero token 1.62 wide - about 2.7% of its width standing, which
        /// reads as a solid counter under the spotlight's zoom without turning a board piece into a
        /// tower. The generated fallback hero is a 0.5-tall prism already, so this is the number that
        /// gives the OFFICIAL TOKEN the presence the prism always had.</para>
        /// </summary>
        public const float Standard = 0.16f;

        /// <summary>
        /// Extrudes a flat generated disc or hex upward into a solid one, in place.
        ///
        /// <para>The mesh handed in must be a fan on the XZ plane: one centre vertex at the origin
        /// followed by a perimeter ring, which is what both <see cref="FlatHexMeshBuilder"/> and the
        /// hero token builder produce. The result keeps the original perimeter ring at y = 0, adds the
        /// same ring at <paramref name="depth"/>, moves the face to the top, and walls the two
        /// together. Its silhouette from directly above is unchanged by construction, because every
        /// added vertex shares an existing vertex's x and z.</para>
        /// </summary>
        public static Mesh ExtrudeFan(Mesh flat, float depth)
        {
            if (flat == null) throw new ArgumentNullException(nameof(flat));
            var solid = ExtrudeFan(flat.vertices, flat.uv, flat.triangles, depth);
            var mesh = new Mesh
            {
                name = flat.name + " (Solid)",
                vertices = solid.Vertices,
                uv = solid.TextureCoordinates,
                normals = solid.Normals,
                triangles = solid.Triangles
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The extrusion as arithmetic on arrays, with no engine object anywhere in it.
        ///
        /// <para>It is split out from <see cref="ExtrudeFan(Mesh, float)"/> so that the claims this
        /// whole feature rests on - that the silhouette from above does not move, that the piece is
        /// exactly <paramref name="depth"/> tall, and that the wall faces outward rather than inward -
        /// are provable without a Unity runtime. Constructing a <c>Mesh</c> is engine-native; this is
        /// not.</para>
        /// </summary>
        public static ExtrudedFan ExtrudeFan(
            Vector3[] flatVertices, Vector2[] flatUv, int[] flatTriangles, float depth)
        {
            if (flatVertices == null) throw new ArgumentNullException(nameof(flatVertices));
            if (flatTriangles == null) throw new ArgumentNullException(nameof(flatTriangles));
            if (depth <= 0f || float.IsNaN(depth) || float.IsInfinity(depth))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depth), depth, "A piece's depth must be a finite positive value.");
            }

            flatUv = flatUv ?? Array.Empty<Vector2>();
            if (flatVertices.Length < 4)
            {
                throw new ArgumentException(
                    "An extruded fan needs a centre and at least three perimeter vertices.",
                    nameof(flatVertices));
            }

            var sideCount = flatVertices.Length - 1;
            var vertices = new Vector3[flatVertices.Length + sideCount];
            var uv = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];

            // The top face: the original fan, lifted.
            for (var index = 0; index < flatVertices.Length; index++)
            {
                var vertex = flatVertices[index];
                vertices[index] = new Vector3(vertex.x, vertex.y + depth, vertex.z);
                uv[index] = index < flatUv.Length ? flatUv[index] : new Vector2(0.5f, 0.5f);
                normals[index] = Vector3.up;
            }

            // The skirt: the same perimeter, left on the board plane.
            for (var side = 0; side < sideCount; side++)
            {
                var perimeter = flatVertices[side + 1];
                var target = flatVertices.Length + side;
                vertices[target] = new Vector3(perimeter.x, perimeter.y, perimeter.z);
                uv[target] = UvAt(flatUv, side + 1);
                var outward = new Vector3(perimeter.x, 0f, perimeter.z);
                normals[target] = outward.sqrMagnitude > 0f ? outward.normalized : Vector3.up;
            }

            var triangles = new int[flatTriangles.Length + (sideCount * 6)];
            Array.Copy(flatTriangles, triangles, flatTriangles.Length);
            var next = flatTriangles.Length;
            for (var side = 0; side < sideCount; side++)
            {
                var following = (side + 1) % sideCount;
                var topA = side + 1;
                var topB = following + 1;
                var bottomA = flatVertices.Length + side;
                var bottomB = flatVertices.Length + following;

                // Wound so the wall faces OUTWARD under the fan's own (centre, next, current) winding.
                // Unity culls back faces, so the mirror of this order is not a subtler shading
                // difference - it is a piece with no visible side at all, which only a tilted camera
                // would ever have shown. `ASolidPieceWallsFaceOutward` pins it.
                triangles[next++] = topA;
                triangles[next++] = bottomB;
                triangles[next++] = bottomA;
                triangles[next++] = topA;
                triangles[next++] = topB;
                triangles[next++] = bottomB;
            }

            return new ExtrudedFan(vertices, uv, normals, triangles);
        }

        private static Vector2 UvAt(Vector2[] uv, int index) =>
            index < uv.Length ? uv[index] : new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// Milestone 8.35z12. One extruded fan's arrays, before anything engine-native holds
    /// them. See <see cref="BoardPieceDepth.ExtrudeFan(Vector3[], Vector2[], int[], float)"/>.
    /// </summary>
    public readonly struct ExtrudedFan
    {
        public ExtrudedFan(
            Vector3[] vertices, Vector2[] textureCoordinates, Vector3[] normals, int[] triangles)
        {
            Vertices = vertices;
            TextureCoordinates = textureCoordinates;
            Normals = normals;
            Triangles = triangles;
        }

        public Vector3[] Vertices { get; }
        public Vector2[] TextureCoordinates { get; }
        public Vector3[] Normals { get; }
        public int[] Triangles { get; }
    }
}
