using System;
using UnityEngine;
using UnityEngine.Rendering;
using GuardsDigital.Presentation.Board;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35n (the FOURTH amendment, board highlight, from interim-2 screenshots): "every
    /// viewer's BOARD marks the acting hero's piece (SelectedActor is already public), so spectators
    /// can see at a glance who is doing something, not only read it."
    ///
    /// <para>A distinct gold ring, drawn additively above the board and independent of
    /// <see cref="Board.BoardTargetRing"/> and <see cref="Board.HexCornerHighlight"/> - the two
    /// existing board-cue idioms named in <c>Docs/AI/Project/CONFIGURED_PRESENTATION.md</c> - so it
    /// can never compete with an existing legality cue's state, colour, or tile-corner priority.
    /// Configured play already retains a <see cref="Board.BoardTargetRing"/> per hero piece but never
    /// activates it (every board choice moved to the hex-corner system at milestone 8.30h), and the
    /// hex-corner palette has no colour free for a new meaning; a third, single, always-additive
    /// marker avoids re-opening either system's priority chain and its large existing Play Mode
    /// surface for a change this narrow.</para>
    ///
    /// <para>One instance answers the whole board: at most one hero is ever the selected actor, so
    /// <see cref="BoardInteractionUnityView"/> owns exactly one marker and repositions it, rather than
    /// retaining one per piece. <see cref="SetHidden"/> is a distinct call from
    /// <see cref="SetVisible"/> rather than a null position, so "no actor stands" - the FOURTH
    /// amendment's own clearing rule - can never be expressed as a stray world position.</para>
    ///
    /// <para><b>Explicitly NOT live pre-commit aiming</b> (the amendment's own recorded non-goal): this
    /// marks the acting piece and nothing else - never a transient selection, a hovered destination, or
    /// any other in-progress choice the actor has not yet committed to. It reads
    /// <c>IMatchReadModel.SelectedActor</c> alone, the same public fact the narration lane's acting
    /// line reads.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ActingHeroMarker : MonoBehaviour
    {
        public const string MarkerObjectName = "Acting Hero Marker";
        public const int Subdivisions = 48;
        public const float RingWidthRatio = 0.22f;
        public const float RadiusMargin = 1.18f;
        // Even at the pulse's minimum scale, 1.64 * 0.78 * 0.94 clears the effect's 1.18.
        public const float EffectRadiusMargin = 1.64f;
        public const float SurfaceOffset = 0.05f;

        private Mesh ownedMesh;
        private bool isFaint;
        private Material ownedMaterial;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private float builtRadius = -1f;

        public bool IsVisible => meshRenderer != null && meshRenderer.enabled;
        public float VisibleRadius => builtRadius;
        public int OwnedNativeResourceCount =>
            (ownedMesh == null ? 0 : 1) + (ownedMaterial == null ? 0 : 1);

        /// <summary>
        /// Creates one retained marker. <see cref="BoardInteractionUnityView"/> owns exactly one
        /// through a private field - a plain C# reference, not a scene search - because it is not
        /// itself a Unity <c>Object</c> and survives a domain reload by being reconstructed rather
        /// than restored, unlike the piece hosts <see cref="Board.BoardTargetRing.GetOrCreate"/>
        /// exists to protect.
        /// </summary>
        public static ActingHeroMarker Create() => Create(false);

        /// <summary>
        /// Milestone 9.21i, USER-SETTLED 2026-09-30 ("I like both proposed + faint gold ring on other
        /// bodies"): <paramref name="faint"/> builds the same ring in the same gold at
        /// <see cref="FaintIntensity"/> of its light, for a many-bodied hero's standing bodies other
        /// than the acting one, so the set reads as one hero - one bright, the rest faint.
        /// </summary>
        public static ActingHeroMarker Create(bool faint)
        {
            var obj = new GameObject(faint ? MarkerObjectName + " (Other Body)" : MarkerObjectName);
            var created = obj.AddComponent<ActingHeroMarker>();
            created.isFaint = faint;
            created.EnsureResources();
            return created;
        }

        /// <summary>Milestone 9.21i. The share of the acting gold a faint (other-body) ring adds.</summary>
        public const float FaintIntensity = 0.35f;

        /// <summary>Milestone 9.21i. Whether this is a faint ring on one of the acting hero's other bodies.</summary>
        public bool IsFaint => isFaint;

        /// <summary>Milestone 9.21i. The gold this ring adds to the board.</summary>
        public Color RingColor => isFaint
            ? new Color(
                MatchHudTheme.ActingHeroMarkerColor.r * FaintIntensity,
                MatchHudTheme.ActingHeroMarkerColor.g * FaintIntensity,
                MatchHudTheme.ActingHeroMarkerColor.b * FaintIntensity,
                MatchHudTheme.ActingHeroMarkerColor.a)
            : MatchHudTheme.ActingHeroMarkerColor;

        /// <summary>
        /// Places and shows the marker at exactly one world position, ringed around a piece of the
        /// given radius. <paramref name="worldPosition"/> is read fresh from the acting hero's own
        /// current occupancy every call, so restore, replay, Undo, and an ordinary move all redraw it
        /// with no state of the marker's own.
        /// </summary>
        public void SetVisible(Vector3 worldPosition, float pieceRadius, bool hasActiveEffect = false)
        {
            EnsureResources();
            if (pieceRadius <= 0f || float.IsNaN(pieceRadius) || float.IsInfinity(pieceRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(pieceRadius));
            }

            transform.position = worldPosition + (Vector3.up * SurfaceOffset);
            // The exact rotation `Board.BoardTargetRing` applies to its own X/Z-parametrized ring
            // geometry - see that type's `SetCue` - so a flat annulus built the same way stands on
            // the same board plane rather than edge-on or facing away from the camera.
            transform.rotation = Quaternion.FromToRotation(
                Vector3.forward, BoardChoiceCue.ScreenUpInWorld);
            transform.localScale = Vector3.one;
            var radius = pieceRadius * (hasActiveEffect ? EffectRadiusMargin : RadiusMargin);
            if (!Mathf.Approximately(radius, builtRadius))
            {
                BuildRing(radius);
                builtRadius = radius;
            }

            meshRenderer.enabled = true;
        }

        /// <summary>The FOURTH amendment's clearing rule: no actor stands, so nothing is drawn.</summary>
        public void SetHidden()
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        private void EnsureResources()
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

            // Domain restoration may preserve the generated child and its renderer references while
            // clearing these managed ownership fields, exactly the trap
            // <see cref="Board.BoardTargetRing"/> already documents. Adopt only this component's own
            // named resources before creating anything.
            if (ownedMesh == null && meshFilter.sharedMesh != null &&
                meshFilter.sharedMesh.name == MarkerObjectName)
            {
                ownedMesh = meshFilter.sharedMesh;
            }

            if (ownedMesh == null)
            {
                ownedMesh = new Mesh { name = MarkerObjectName };
                ownedMesh.MarkDynamic();
                meshFilter.sharedMesh = ownedMesh;
            }

            if (ownedMaterial == null)
            {
                var retained = meshRenderer.sharedMaterial;
                ownedMaterial = retained != null && retained.name.StartsWith(
                    MarkerObjectName, StringComparison.Ordinal)
                    ? retained
                    : null;
            }

            if (ownedMaterial == null)
            {
                // Milestone 8.35z39. The same ring, drawn as LIGHT rather than as an opaque
                // painted line. Nothing about the mark's shape moves: `BuildRing` is untouched, so
                // the annulus keeps `RingWidthRatio` 0.22 inside `RadiusMargin` 1.18 of the piece it
                // circles, and the colour is still `MatchHudTheme.ActingHeroMarkerColor` - the
                // FOURTH amendment's "a distinct gold ring" means the same gold it always did. What
                // changes is that the ring now ADDS its gold to the board instead of covering the
                // board with it, so the artwork the actor is standing on stays visible through the
                // mark and the space reads as lit. See BoardOverlayMaterial.CreateLuminous for why
                // this is a blend factor rather than the Bloom the volume profile declares and the
                // renderer does not run.
                ownedMaterial = BoardOverlayMaterial.CreateLuminous(
                    MarkerObjectName + (isFaint ? " Faint Ring" : " Ring"), RingColor);
            }

            meshRenderer.sharedMaterial = ownedMaterial;
            if (!meshRenderer.enabled)
            {
                meshRenderer.enabled = false;
            }
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
        /// call this first for the same reason <see cref="Board.BoardTargetRing.ReleaseOwnedResources"/>
        /// does: Unity does not always deliver a generated child's <c>OnDestroy</c> before its renderer
        /// stops referencing a still-live native resource.
        /// </summary>
        internal void ReleaseOwnedResources()
        {
            var mesh = ownedMesh;
            var material = ownedMaterial;
            ownedMesh = null;
            ownedMaterial = null;
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
