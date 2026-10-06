using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 8.11a. Draws the official board face flat on the board plane, underneath everything
    /// the match puts on it.
    ///
    /// <para>The quad is built from <see cref="OfficialBoardArtwork.Corners"/> rather than from a
    /// position and a rotation, so the picture cannot drift away from the registration the conformance
    /// test measures - there is one statement of where the artwork goes, and both read it.</para>
    ///
    /// <para>It sits a little below the tiles. The tiles stay where they are and keep answering
    /// selection and highlighting; what changes when this view is present is that their fill is drawn
    /// transparent and their debug edge is switched off, so the artwork is what a player sees and the
    /// highlight materials still read against it.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OfficialBoardUnderlayView : MonoBehaviour
    {
        /// <summary>
        /// How far below the tile plane the face is drawn, in world units at radius 1. Far enough that
        /// nothing z-fights with the tiles at y = 0 or their edges at y = 0.015, and near enough that no
        /// camera this project uses can see the gap.
        /// </summary>
        public const float PlaneOffset = -0.05f;

        [SerializeField] private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        private Mesh generatedMesh;
        private Material generatedMaterial;

        public float OuterRadius => outerRadius;

        public bool HasArtwork => generatedMaterial != null && generatedMaterial.mainTexture != null;

        public int OwnedNativeResourceCount =>
            (generatedMesh != null ? 1 : 0) + (generatedMaterial != null ? 1 : 0);

        /// <summary>
        /// Points the view at a prepared face. The mesh and material are generated once and updated in
        /// place afterwards, so repeated rebuilds do not accumulate native resources.
        /// </summary>
        public void Configure(Texture2D artwork, float radius) => Configure(artwork, radius, OfficialBoardArtwork.Corners(radius));

        public void Configure(Texture2D artwork, float radius, Vector3[] corners)
        {
            if (artwork == null) throw new System.ArgumentNullException(nameof(artwork));
            if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(radius), radius, "The board underlay requires a finite positive outer radius.");
            }

            outerRadius = radius;

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            if (generatedMesh == null)
            {
                generatedMesh = new Mesh { name = "Owned Official Board Underlay" };
                generatedMesh.hideFlags = HideFlags.DontSave;
            }


            var height = PlaneOffset * radius;
            var vertices = new Vector3[corners.Length];
            for (var index = 0; index < corners.Length; index++)
            {
                vertices[index] = new Vector3(corners[index].x, height, corners[index].z);
            }

            generatedMesh.Clear();
            generatedMesh.vertices = vertices;
            // The corners run counter-clockwise in the XZ plane, which faces -Y; reversing each triangle
            // turns the face upward, toward the camera looking straight down.
            generatedMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            generatedMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            generatedMesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            generatedMesh.RecalculateBounds();
            meshFilter.sharedMesh = generatedMesh;

            if (generatedMaterial == null)
            {
                // Unlit, and it has to be. The face is a picture of a printed board: what it must show
                // is its own pixels, exactly, with nothing of the scene's lighting in them.
                //
                // Two attempts at doing this with a lit shader failed, and both are recorded
                // because the second looked like it should have worked. Tinting the albedo *and* the
                // emission with the texture doubled it. Blacking the albedo killed the diffuse term but
                // left the broad specular and reflection response to this scene's flat blue-grey
                // ambient, and switching both of those off still did not restore the artwork's own
                // colour. An unlit shader has none of that machinery to switch off.
                //
                // `RuntimeShaderResolutionTests` requires this name to be listed in Graphics settings'
                // always-included shaders, which is where it now is - see that file's own note. The
                // requirement is real: without the entry the shader reaches a player build only by
                // accident, through some other asset that happens to reference it.
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    throw new System.InvalidOperationException(
                        "The board underlay requires the URP Unlit shader.");
                }

                generatedMaterial = new Material(shader) { name = "Owned Official Board Underlay" };
                generatedMaterial.hideFlags = HideFlags.DontSave;
            }

            generatedMaterial.mainTexture = artwork;
            meshRenderer.sharedMaterial = generatedMaterial;
            meshRenderer.enabled = true;
            // The face is scenery: it must not light, shadow, or occlude anything the match draws on it.
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public void ReleaseOwnedResources()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null) meshFilter.sharedMesh = null;
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.sharedMaterial = null;
            DestroyOwned(generatedMesh);
            DestroyOwned(generatedMaterial);
            generatedMesh = null;
            generatedMaterial = null;
        }

        private void OnDestroy()
        {
            ReleaseOwnedResources();
        }

        private static void DestroyOwned(UnityEngine.Object target)
        {
            if (target == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }
    }
}
