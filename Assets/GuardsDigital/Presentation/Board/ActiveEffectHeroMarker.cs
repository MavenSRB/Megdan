using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// The violet ring one hero piece wears while an Active effect is standing on it.
    ///
    /// <para><b>Milestone 8.35z63, from the user's own direction:</b> "when a hero has an
    /// active effect, we need to show it. Same purple color we use in hero tray to show active cards
    /// should higlight the effect target ... If tigerclaw has effect of his silver he should have
    /// pruple higlight."</para>
    ///
    /// <para><b>Deliberately the same shape as <see cref="Match.ActingHeroMarker"/>, one ring wider.</b>
    /// That marker already answers "who is acting" with a gold annulus drawn as light above the board,
    /// and the two facts are orthogonal - a hero can be acting AND carrying an effect, and the FOURTH
    /// amendment's gold must not be displaced by an ongoing condition. So this is a second annulus at
    /// <see cref="RadiusMargin"/> rather than a colour swapped into the first, and the two margins are
    /// far enough apart that both read at once: gold means "waiting on you", violet means "an effect is
    /// running on you", and where both apply both are drawn.</para>
    ///
    /// <para><b>One per piece, as a child of the piece.</b> <see cref="Match.ActingHeroMarker"/> is a
    /// singleton the board repositions because at most one hero is ever the selected actor; several
    /// heroes can carry effects at once, so this follows <see cref="BoardTargetRing"/> instead - found
    /// or created directly under the piece, moving with it for free. That is also what makes p.17's
    /// "an Active effect's Radius moves with its hero" cost nothing here: nothing positions this.</para>
    ///
    /// <para><b>No animation and no colour of its own.</b> It is a static tint: the conductors that
    /// own this HUD's motion are not consulted and no second animation authority is introduced. The
    /// colour arrives from the caller rather than being read here, so this file stays inside the board
    /// vocabulary and the ONE violet stays <c>MatchHudTheme.ActiveEffectAccent</c>'s to define.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ActiveEffectHeroMarker : MonoBehaviour
    {
        public const string MarkerObjectName = "Active Effect Hero Marker";
        public const int Subdivisions = 48;

        /// <summary>Thinner than the acting ring's 0.22, because it is the outer of the two.</summary>
        public const float RingWidthRatio = 0.15f;

        /// <summary>
        /// Outside <c>ActingHeroMarker.RadiusMargin</c> (1.18) by more than that ring's own width, so
        /// a hero who is both acting and carrying an effect wears two separate rings rather than one
        /// muddied band.
        /// </summary>
        public const float RadiusMargin = 1.46f;

        /// <summary>
        /// Under the acting ring's own 0.05 clearance, so where both are drawn the gold stays the
        /// upper of the two. Both are additive, so neither can hide the other.
        /// </summary>
        public const float SurfaceOffset = 0.035f;

        private Mesh ownedMesh;
        private Material ownedMaterial;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private float builtRadius = -1f;
        private Color builtColor;

        public bool IsVisible => meshRenderer != null && meshRenderer.enabled;

        public float VisibleRadius => builtRadius;

        /// <summary>The colour this ring is currently generated in, for a fixture to compare.</summary>
        public Color VisibleColor => ownedMaterial == null ? default : ownedMaterial.color;

        public int OwnedNativeResourceCount =>
            (ownedMesh == null ? 0 : 1) + (ownedMaterial == null ? 0 : 1);

        /// <summary>
        /// Finds the retained marker directly below one piece or creates it once, exactly as
        /// <see cref="BoardTargetRing.GetOrCreate"/> does and for the same reason: a runtime-created
        /// view may lose a non-serialized field across editor backup and domain restoration while its
        /// generated child survives.
        /// </summary>
        public static ActiveEffectHeroMarker GetOrCreate(Transform piece)
        {
            if (piece == null)
            {
                throw new ArgumentNullException(nameof(piece));
            }

            ActiveEffectHeroMarker found = null;
            foreach (var candidate in piece.GetComponentsInChildren<ActiveEffectHeroMarker>(true))
            {
                if (candidate.transform.parent != piece)
                {
                    continue;
                }

                if (found != null)
                {
                    throw new InvalidOperationException(
                        "One piece cannot own two Active-effect markers.");
                }

                found = candidate;
            }

            if (found != null)
            {
                return found;
            }

            var markerObject = new GameObject(MarkerObjectName);
            markerObject.transform.SetParent(piece, false);
            return markerObject.AddComponent<ActiveEffectHeroMarker>();
        }

        /// <summary>
        /// Shows the ring around a piece of the given radius, in the accent the caller states.
        ///
        /// <paramref name="boardPlaneLocalY"/> is where the board surface sits in the piece's own local
        /// space, so the ring lies on the board rather than at the height of whatever the piece happens
        /// to be standing at - the same separation <c>PrimitiveHeroView</c> already makes for its
        /// target ring.
        /// </summary>
        public void SetVisible(float pieceRadius, float boardPlaneLocalY, Color accent)
        {
            if (pieceRadius <= 0f || float.IsNaN(pieceRadius) || float.IsInfinity(pieceRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(pieceRadius));
            }

            EnsureResources(accent);
            transform.localPosition = new Vector3(0f, boardPlaneLocalY + SurfaceOffset, 0f);
            // The rotation `BoardTargetRing` and `ActingHeroMarker` both apply to X/Z-parametrized ring
            // geometry, so a flat annulus stands on the board plane rather than edge-on.
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var radius = pieceRadius * RadiusMargin;
            if (!Mathf.Approximately(radius, builtRadius))
            {
                BuildRing(radius);
                builtRadius = radius;
            }

            meshRenderer.enabled = true;
        }

        /// <summary>No effect stands on this hero, so nothing is drawn.</summary>
        public void SetHidden()
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        private void EnsureResources(Color accent)
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            // Adopt only this component's own named resources first; see `ActingHeroMarker` for the
            // domain-restoration trap this guards.
            if (ownedMesh == null && meshFilter.sharedMesh != null &&
                meshFilter.sharedMesh.name == MarkerObjectName)
            {
                ownedMesh = meshFilter.sharedMesh;
                builtRadius = -1f;
            }

            if (ownedMesh == null)
            {
                ownedMesh = new Mesh { name = MarkerObjectName };
                ownedMesh.MarkDynamic();
                meshFilter.sharedMesh = ownedMesh;
            }

            if (ownedMaterial != null && builtColor != accent)
            {
                DestroyOwnedResource(ownedMaterial);
                ownedMaterial = null;
            }

            if (ownedMaterial == null)
            {
                // Drawn as LIGHT rather than as paint, exactly as the acting ring is since milestone
                // 8.35z39 - a thin mark on a painted board reads as a mark when it adds its colour and
                // as a sticker when it replaces it. The aura this marker's own milestone paints on the
                // TILES is blended instead, because a whole-hex area of added light saturates toward
                // white and would out-shout every mark on top of it.
                ownedMaterial = BoardOverlayMaterial.CreateLuminous(
                    MarkerObjectName + " Ring", accent);
                builtColor = accent;
            }

            meshRenderer.sharedMaterial = ownedMaterial;
        }

        private void BuildRing(float outerRadius)
        {
            var innerRadius = outerRadius * (1f - RingWidthRatio);
            var vertices = new Vector3[Subdivisions * 2];
            for (var index = 0; index < Subdivisions; index++)
            {
                var angle = index * Mathf.PI * 2f / Subdivisions;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[index * 2] = direction * innerRadius;
                vertices[(index * 2) + 1] = direction * outerRadius;
            }

            var triangles = new int[Subdivisions * 6];
            for (var index = 0; index < Subdivisions; index++)
            {
                var innerA = index * 2;
                var outerA = innerA + 1;
                var innerB = ((index + 1) % Subdivisions) * 2;
                var outerB = innerB + 1;
                var offset = index * 6;
                triangles[offset] = innerA;
                triangles[offset + 1] = outerB;
                triangles[offset + 2] = outerA;
                triangles[offset + 3] = innerA;
                triangles[offset + 4] = innerB;
                triangles[offset + 5] = outerB;
            }

            ownedMesh.Clear();
            ownedMesh.SetVertices(vertices);
            ownedMesh.SetTriangles(triangles, 0);
            ownedMesh.RecalculateBounds();
            ownedMesh.RecalculateNormals();
        }

        /// <summary>
        /// Releases the two native resources this marker owns. Piece hosts that tear down a hierarchy
        /// call this first, for the reason <see cref="BoardTargetRing.ReleaseOwnedResources"/> gives:
        /// Unity does not always deliver a generated child's <c>OnDestroy</c> before its renderer stops
        /// referencing a still-live native resource.
        /// </summary>
        public void ReleaseOwnedResources()
        {
            var mesh = ownedMesh;
            var material = ownedMaterial;
            ownedMesh = null;
            ownedMaterial = null;
            builtRadius = -1f;
            if (meshFilter != null && meshFilter.sharedMesh == mesh)
            {
                meshFilter.sharedMesh = null;
            }

            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = null;
            }

            DestroyOwnedResource(mesh);
            DestroyOwnedResource(material);
        }

        private static void DestroyOwnedResource(UnityEngine.Object resource)
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
                DestroyImmediate(resource, true);
            }
        }

        private void OnDestroy() => ReleaseOwnedResources();
    }
}
