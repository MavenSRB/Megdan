using System;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    public static class FlatHexMeshBuilder
    {
        private const int SideCount = 6;

        public static Mesh Create(float outerRadius = HexWorldLayout.DefaultOuterRadius)
        {
            if (outerRadius <= 0f || float.IsNaN(outerRadius) || float.IsInfinity(outerRadius))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outerRadius),
                    outerRadius,
                    "Hex outer radius must be a finite positive value.");
            }

            var vertices = new Vector3[SideCount + 1];
            var textureCoordinates = new Vector2[vertices.Length];
            var triangles = new int[SideCount * 3];

            vertices[0] = Vector3.zero;
            textureCoordinates[0] = new Vector2(0.5f, 0.5f);

            for (var side = 0; side < SideCount; side++)
            {
                var angle = Mathf.Deg2Rad * (30f + (side * 60f));
                var x = Mathf.Cos(angle) * outerRadius;
                var z = Mathf.Sin(angle) * outerRadius;
                vertices[side + 1] = new Vector3(x, 0f, z);
                textureCoordinates[side + 1] = new Vector2(
                    (x / (outerRadius * 2f)) + 0.5f,
                    (z / (outerRadius * 2f)) + 0.5f);

                var triangleIndex = side * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = ((side + 1) % SideCount) + 1;
                triangles[triangleIndex + 2] = side + 1;
            }

            var mesh = new Mesh
            {
                name = "Flat Hex Placeholder",
                vertices = vertices,
                uv = textureCoordinates,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
