using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PlaceholderObstacleView : MonoBehaviour
    {
        public const float DefaultWidth = 0.52f;
        public const float DefaultHeight = 0.38f;
        public const float DefaultVerticalOffset = 0.035f;

        [SerializeField]
        private string obstacleId = "placeholder-obstacle";

        [SerializeField]
        private int q;

        [SerializeField]
        private int r;

        [SerializeField]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        [SerializeField]
        private float width = DefaultWidth;

        [SerializeField]
        private float height = DefaultHeight;

        [SerializeField]
        private float verticalOffset = DefaultVerticalOffset;

        private Mesh ownedMesh;
        private Material ownedMaterial;
        private GameObject artFace;
        private Material artMaterial;

        public ObstacleOccupancy Occupancy => new ObstacleOccupancy(new ObstacleId(obstacleId), Coordinate);

        public HexCoordinate Coordinate => new HexCoordinate(q, r);

        public float OuterRadius => outerRadius;

        public float VerticalOffset => verticalOffset;

        public void Configure(ObstacleOccupancy occupancy, float radius)
        {
            obstacleId = occupancy.ObstacleId.Value;
            q = occupancy.Coordinate.Q;
            r = occupancy.Coordinate.R;
            outerRadius = radius;
            ApplyLayout();
            EnsurePrimitiveVisual();
            EnsurePreparedArtwork();
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
                ownedMesh = CreateBlockMesh(width, height);
                meshFilter.sharedMesh = ownedMesh;
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (meshRenderer.sharedMaterial == null)
            {
                ownedMaterial = CreatePlaceholderMaterial();
                meshRenderer.sharedMaterial = ownedMaterial;
            }
        }

        private void EnsurePreparedArtwork()
        {
            // This is a presentation address only. The authoritative obstacle identity and its
            // occupancy remain independent of whether the private standee is installed.
            var texture = obstacleId.StartsWith("trinkets-turret:",
                System.StringComparison.Ordinal)
                ? Resources.Load<Texture2D>("Private/Tokens/trinkets-turret")
                : null;
            var block = GetComponent<MeshRenderer>();
            if (texture == null)
            {
                if (block != null) block.enabled = true;
                if (artFace != null) artFace.SetActive(false);
                return;
            }
            if (artFace == null)
            {
                artFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
                artFace.name = "Obstacle Standee";
                artFace.transform.SetParent(transform, false);
                artFace.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                artFace.transform.localScale = new Vector3(0.86f, 0.86f, 1f);
                var collider = artFace.GetComponent<Collider>();
                if (collider != null) DestroyOwnedResource(collider);
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    throw new System.InvalidOperationException(
                        "The prepared obstacle standee requires the URP Unlit shader.");
                artMaterial = new Material(shader)
                {
                    name = "Prepared Obstacle Standee",
                    mainTexture = texture,
                    color = Color.white
                };
                artFace.GetComponent<MeshRenderer>().sharedMaterial = artMaterial;
            }
            artFace.SetActive(true);
            if (block != null) block.enabled = false;
        }

        private void LateUpdate()
        {
            if (artFace == null || !artFace.activeSelf || Camera.main == null) return;
            var towardCamera = Camera.main.transform.position - artFace.transform.position;
            if (towardCamera.sqrMagnitude > 0.0001f)
                artFace.transform.rotation = Quaternion.LookRotation(towardCamera, Vector3.up);
        }

        private static Mesh CreateBlockMesh(float blockWidth, float blockHeight)
        {
            var halfWidth = blockWidth / 2f;
            var vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -halfWidth),
                new Vector3(halfWidth, 0f, -halfWidth),
                new Vector3(halfWidth, 0f, halfWidth),
                new Vector3(-halfWidth, 0f, halfWidth),
                new Vector3(-halfWidth, blockHeight, -halfWidth),
                new Vector3(halfWidth, blockHeight, -halfWidth),
                new Vector3(halfWidth, blockHeight, halfWidth),
                new Vector3(-halfWidth, blockHeight, halfWidth)
            };
            var triangles = new[]
            {
                0, 4, 1, 1, 4, 5,
                1, 5, 2, 2, 5, 6,
                2, 6, 3, 3, 6, 7,
                3, 7, 0, 0, 7, 4,
                4, 7, 5, 5, 7, 6,
                3, 0, 2, 2, 0, 1
            };

            var mesh = new Mesh
            {
                name = "Placeholder Obstacle",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreatePlaceholderMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new System.InvalidOperationException(
                    "The placeholder obstacle requires the URP Lit shader.");
            }

            return new Material(shader)
            {
                name = "Placeholder Obstacle",
                color = new Color(0.18f, 0.16f, 0.12f, 1f)
            };
        }

        private void Awake()
        {
            ApplyLayout();
            EnsurePrimitiveVisual();
        }

        private void OnDestroy()
        {
            DestroyOwnedResource(ownedMesh);
            DestroyOwnedResource(ownedMaterial);
            DestroyOwnedResource(artMaterial);
            ownedMesh = null;
            ownedMaterial = null;
            artMaterial = null;
        }

        private static void DestroyOwnedResource(Object resource)
        {
            if (resource == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(resource);
            }
            else
            {
                DestroyImmediate(resource);
            }
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
