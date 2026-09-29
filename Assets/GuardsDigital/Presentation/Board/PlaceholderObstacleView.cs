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
                // Milestone 9.16, the user's pick of the three captured treatments: the prepared
                // disc's figure alone, cut out of its whitened standee ring and field, outlined in
                // the board's thin black line, and as wide as the hex tokens (Mrak's rock, Wuk's
                // Trees) - no ring, no base. It is drawn through the shipped blended Unlit
                // template, so the texture's alpha is honoured: the opaque Unlit material this
                // replaces drew the file's transparent corners as a black square.
                var faceWidth = FaceWidth(outerRadius);
                var face = PreparedObstacleCutout.CutWithOutline(
                    texture, BoardPieceOutline.WorldWidth(outerRadius) / faceWidth);
                artFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
                artFace.name = "Obstacle Standee";
                artFace.transform.SetParent(transform, false);
                artFace.transform.localPosition = new Vector3(0f,
                    PlacedTokenView.DefaultVerticalOffset - verticalOffset + BoardPieceDepth.Standard, 0f);
                artFace.transform.localScale =
                    new Vector3(faceWidth, faceWidth * face.height / face.width, 1f);
                var collider = artFace.GetComponent<Collider>();
                if (collider != null) DestroyOwnedResource(collider);
                artMaterial = BoardOverlayMaterial.CreateUnlitBlended("Prepared Obstacle Standee", Color.white);
                artMaterial.mainTexture = face;
                artFace.GetComponent<MeshRenderer>().sharedMaterial = artMaterial;
            }
            artFace.SetActive(true);
            if (block != null) block.enabled = false;
        }

        /// <summary>
        /// Milestone 9.16. The prepared face's width: the flat-to-flat width of the hex tokens,
        /// which are pointy-top solid hexes of <see cref="PlacedTokenView.DiscRadiusFraction"/> of
        /// the tile, so the Turret stands on the footprint the rock and the Trees do.
        /// </summary>
        public static float FaceWidth(float outerRadius) =>
            2f * outerRadius * PlacedTokenView.DiscRadiusFraction * Mathf.Cos(30f * Mathf.Deg2Rad);

        private void LateUpdate()
        {
            if (artFace == null || !artFace.activeSelf || Camera.main == null) return;
            var camera = Camera.main.transform;
            var towardCamera = camera.position - artFace.transform.position;
            if (towardCamera.sqrMagnitude > 0.0001f)
                // Unity's Quad primitive renders its local -Z face. Aim that normal at the
                // camera; aiming +Z at it turns the prepared face away and back-face culls the
                // Turret while the fallback block is disabled. Milestone 9.16: roll by the
                // camera's own up. Under the straight-down match camera world up is almost
                // parallel to that normal, so rolling by it turned the figure's top away from
                // the frame's centre - the Turret was drawn upside down on the lower board.
                artFace.transform.rotation = Quaternion.LookRotation(-towardCamera, camera.up);
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
