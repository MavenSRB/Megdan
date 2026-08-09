using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Units.Heroes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PrimitiveHeroView : MonoBehaviour
    {
        public const string DefaultHeroId = "primitive-hero";
        public const float DefaultBaseRadius = 0.32f;
        public const float DefaultHeight = 0.5f;
        public const float DefaultVerticalOffset = 0.03f;

        private const int SideCount = 16;

        [SerializeField]
        private string heroId = DefaultHeroId;

        [SerializeField]
        private int q;

        [SerializeField]
        private int r;

        [SerializeField]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        [SerializeField]
        private float baseRadius = DefaultBaseRadius;

        [SerializeField]
        private float height = DefaultHeight;

        [SerializeField]
        private float verticalOffset = DefaultVerticalOffset;

        public HeroOccupancy Occupancy => new HeroOccupancy(new HeroId(heroId), Coordinate);

        public HexCoordinate Coordinate => new HexCoordinate(q, r);

        public float OuterRadius => outerRadius;

        public float VerticalOffset => verticalOffset;

        public void Configure(HeroOccupancy occupancy, float radius)
        {
            heroId = occupancy.HeroId.Value;
            q = occupancy.Coordinate.Q;
            r = occupancy.Coordinate.R;
            outerRadius = radius;
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            transform.localPosition = HexWorldLayout.ToWorld(Coordinate, outerRadius) + (Vector3.up * verticalOffset);
        }

        public void EnsurePrimitiveVisual()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = CreateCylinderMesh(baseRadius, height);
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (meshRenderer.sharedMaterial == null)
            {
                meshRenderer.sharedMaterial = CreatePlaceholderMaterial();
            }
        }

        private static Mesh CreateCylinderMesh(float radius, float cylinderHeight)
        {
            var vertices = new Vector3[(SideCount * 2) + 2];
            var triangles = new int[SideCount * 12];
            var bottomCenterIndex = SideCount * 2;
            var topCenterIndex = bottomCenterIndex + 1;

            vertices[bottomCenterIndex] = Vector3.zero;
            vertices[topCenterIndex] = Vector3.up * cylinderHeight;

            for (var side = 0; side < SideCount; side++)
            {
                var angle = Mathf.Deg2Rad * ((360f / SideCount) * side);
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                vertices[side] = new Vector3(x, 0f, z);
                vertices[side + SideCount] = new Vector3(x, cylinderHeight, z);
            }

            var triangleIndex = 0;
            for (var side = 0; side < SideCount; side++)
            {
                var next = (side + 1) % SideCount;
                var top = side + SideCount;
                var nextTop = next + SideCount;

                triangles[triangleIndex++] = side;
                triangles[triangleIndex++] = top;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = top;
                triangles[triangleIndex++] = nextTop;

                triangles[triangleIndex++] = topCenterIndex;
                triangles[triangleIndex++] = nextTop;
                triangles[triangleIndex++] = top;

                triangles[triangleIndex++] = bottomCenterIndex;
                triangles[triangleIndex++] = side;
                triangles[triangleIndex++] = next;
            }

            var mesh = new Mesh
            {
                name = "Primitive Hero Placeholder",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreatePlaceholderMaterial()
        {
            var shader = Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Primitive Hero Placeholder",
                color = new Color(0.1f, 0.45f, 0.9f, 1f)
            };
            return material;
        }

        private void Awake()
        {
            ApplyLayout();
            EnsurePrimitiveVisual();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (outerRadius > 0f && !float.IsNaN(outerRadius) && !float.IsInfinity(outerRadius))
            {
                ApplyLayout();
            }
        }
#endif
    }
}
