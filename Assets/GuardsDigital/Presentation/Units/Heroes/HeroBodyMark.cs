using System;
using GuardsDigital.Presentation.Board;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardsDigital.Presentation.Units.Heroes
{
    /// <summary>
    /// Milestone 9.21i. The quiet per-body mark on a many-bodied hero's standee: body N wears N small
    /// cream pips on the lower edge of its token (RZ-Q1 bounds N at four), so two standees of one hero
    /// can be told apart on the board and in the pick without a font on the board.
    /// </summary>
    /// <remarks>
    /// <para><b>A PROPOSAL awaiting the user's choice</b> (9.21i's captures): the alternatives were a
    /// small numeral badge in a dark disc (the retired minion value-badge construction) and a letter.
    /// Pips were built because they need no board font and read as "which one" rather than as a value
    /// a card could change.</para>
    /// <para>Drawn only on a hero standing on more than one space, or on a body above the first, so
    /// every single-bodied hero's piece is unchanged. It is geometry laid on the token's own face
    /// plane under the piece's transform, so it moves, fades and hides with the standee.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HeroBodyMark : MonoBehaviour
    {
        public const string MarkObjectName = "Hero Body Mark";
        public const int PipSegments = 16;
        public const float PipRadius = 0.042f;
        public const float PipSpacing = 0.115f;
        public const float SurfaceOffset = 0.004f;
        public static readonly Color PipColor = new Color(0.97f, 0.94f, 0.84f, 1f);

        private Mesh ownedMesh;
        private Material ownedMaterial;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        /// <summary>The number of pips drawn, or 0 when the mark is hidden.</summary>
        public int VisiblePips { get; private set; }

        public bool IsVisible => meshRenderer != null && meshRenderer.enabled && VisiblePips > 0;

        public int OwnedNativeResourceCount =>
            (ownedMesh == null ? 0 : 1) + (ownedMaterial == null ? 0 : 1);

        public static HeroBodyMark GetOrCreate(Transform piece)
        {
            if (piece == null) throw new ArgumentNullException(nameof(piece));
            foreach (var candidate in piece.GetComponentsInChildren<HeroBodyMark>(true))
            {
                if (candidate.transform.parent == piece) return candidate;
            }

            var markObject = new GameObject(MarkObjectName);
            markObject.transform.SetParent(piece, false);
            return markObject.AddComponent<HeroBodyMark>();
        }

        /// <summary>
        /// Draws <paramref name="pips"/> pips along the lower edge of a piece whose visible edge is at
        /// <paramref name="ringRadius"/> from its centre, on the plane <paramref name="surfaceY"/>
        /// above the piece's own origin. "Lower" is the framing camera's screen-down, read from
        /// <see cref="BoardChoiceCue.ScreenUpInWorld"/> as every other board cue reads it.
        /// </summary>
        public void SetVisible(int pips, float ringRadius, float surfaceY)
        {
            if (pips < 1) throw new ArgumentOutOfRangeException(nameof(pips));
            if (ringRadius <= 0f || float.IsNaN(ringRadius) || float.IsInfinity(ringRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(ringRadius));
            }

            EnsureResources();
            transform.localPosition = new Vector3(0f, surfaceY + SurfaceOffset, 0f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            var parent = transform.parent;
            var down = -BoardChoiceCue.ScreenUpInWorld;
            var localDown = parent == null ? down : parent.InverseTransformDirection(down);
            localDown = new Vector3(localDown.x, 0f, localDown.z);
            localDown = localDown.sqrMagnitude < 1e-6f ? Vector3.back : localDown.normalized;
            Build(pips, ringRadius, localDown);
            VisiblePips = pips;
            meshRenderer.enabled = true;
        }

        public void SetHidden()
        {
            VisiblePips = 0;
            if (meshRenderer != null) meshRenderer.enabled = false;
        }

        private void Build(int pips, float ringRadius, Vector3 localDown)
        {
            // The pips sit on an arc of the ring centred on screen-down, one step of arc per pip.
            var baseAngle = Mathf.Atan2(localDown.z, localDown.x);
            var step = PipSpacing / ringRadius;
            var vertices = new Vector3[pips * (PipSegments + 1)];
            var triangles = new int[pips * PipSegments * 3];
            for (var pip = 0; pip < pips; pip++)
            {
                var angle = baseAngle + ((pip - ((pips - 1) * 0.5f)) * step);
                var centre = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
                var first = pip * (PipSegments + 1);
                vertices[first] = centre;
                for (var segment = 0; segment < PipSegments; segment++)
                {
                    var around = segment * Mathf.PI * 2f / PipSegments;
                    vertices[first + 1 + segment] = centre +
                        (new Vector3(Mathf.Cos(around), 0f, Mathf.Sin(around)) * PipRadius);
                    var offset = ((pip * PipSegments) + segment) * 3;
                    // Wound as the token face is, so the pip faces the camera looking down +Y.
                    triangles[offset] = first;
                    triangles[offset + 1] = first + 1 + ((segment + 1) % PipSegments);
                    triangles[offset + 2] = first + 1 + segment;
                }
            }

            ownedMesh.Clear();
            ownedMesh.SetVertices(vertices);
            ownedMesh.SetTriangles(triangles, 0);
            ownedMesh.RecalculateBounds();
            ownedMesh.RecalculateNormals();
        }

        private void EnsureResources()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            if (ownedMesh == null && meshFilter.sharedMesh != null &&
                meshFilter.sharedMesh.name == MarkObjectName)
            {
                ownedMesh = meshFilter.sharedMesh;
            }

            if (ownedMesh == null)
            {
                ownedMesh = new Mesh { name = MarkObjectName };
                ownedMesh.MarkDynamic();
                meshFilter.sharedMesh = ownedMesh;
            }

            if (ownedMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    throw new InvalidOperationException("The hero body mark requires the URP Unlit shader.");
                }

                ownedMaterial = new Material(shader) { name = MarkObjectName + " Pips", color = PipColor };
            }

            meshRenderer.sharedMaterial = ownedMaterial;
        }

        public void ReleaseOwnedResources()
        {
            var mesh = ownedMesh;
            var material = ownedMaterial;
            ownedMesh = null;
            ownedMaterial = null;
            if (meshFilter != null && meshFilter.sharedMesh == mesh) meshFilter.sharedMesh = null;
            if (meshRenderer != null) meshRenderer.sharedMaterial = null;
            DestroyOwned(mesh);
            DestroyOwned(material);
        }

        private static void DestroyOwned(UnityEngine.Object resource)
        {
            if (resource == null) return;
            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(resource);
            else UnityEngine.Object.DestroyImmediate(resource);
        }

        private void OnDestroy() => ReleaseOwnedResources();
    }
}
