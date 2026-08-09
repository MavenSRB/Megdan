using GuardsDigital.Presentation.Board;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class FlatHexMeshBuilderTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Create_BuildsSixTriangleMeshOnXZPlane()
        {
            var mesh = FlatHexMeshBuilder.Create();

            try
            {
                Assert.That(mesh.vertexCount, Is.EqualTo(7));
                Assert.That(mesh.triangles.Length, Is.EqualTo(18));
                Assert.That(mesh.vertices, Has.All.Matches<Vector3>(vertex => Mathf.Abs(vertex.y) < Tolerance));
                Assert.That(mesh.normals, Has.All.Matches<Vector3>(normal => normal.y > 0.99f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Create_PlacesEveryPerimeterVertexAtOuterRadius()
        {
            const float outerRadius = 2.25f;
            var mesh = FlatHexMeshBuilder.Create(outerRadius);

            try
            {
                for (var vertexIndex = 1; vertexIndex < mesh.vertices.Length; vertexIndex++)
                {
                    Assert.That(
                        mesh.vertices[vertexIndex].magnitude,
                        Is.EqualTo(outerRadius).Within(Tolerance));
                }
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
