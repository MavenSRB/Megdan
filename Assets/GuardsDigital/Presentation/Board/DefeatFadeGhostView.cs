using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 8.35z6 (PLAN.md's SIXTEENTH amendment: "Defeat: shrink-and-fade rather than
    /// vanish").
    ///
    /// <para>Milestone 8.35y's kinetic pass built the Defeat cue's timing and motion math but could not
    /// deliver the visible shrink-and-fade itself: holding a defeated piece drawn meant re-activating a
    /// <c>GameObject</c> the board view (<see cref="Match.BoardInteractionUnityView"/>) had already
    /// deactivated the same frame the projection dropped it, behind that view's own back, on an object it
    /// pools and reuses. The complete unfiltered Play Mode run at that integration found the cost: five
    /// existing removal guards assert that a defeated piece's <c>GameObject</c> is inactive the instant
    /// <c>Render</c>/<c>RenderMinions</c> drops it from the projection - rulebook p.13's removal, and a
    /// real acceptance check of the milestones that wrote them
    /// (<c>MinimalHexGridPlayModeTests</c> x2, <c>StandardMinionViewTests</c>,
    /// <c>ConfiguredPointerSelectionTests</c>) - and the hold made all five report the piece still
    /// present. The hold was cut rather than traded for five weakened rules guards.</para>
    ///
    /// <para>This type is the fix, delivered pooling-safely per the SIXTEENTH amendment's own
    /// constraint: it is a bare, purely decorative VISUAL ECHO, spawned and owned entirely by the board
    /// view, that is NEVER the pooled hero/minion <c>GameObject</c>. The real piece's own
    /// <c>SetActive(false)</c> stays exactly as synchronous as the five guards require - this type is
    /// never in their way, because it carries no <c>PrimitiveHeroView</c>/<c>StandardMinionView</c>
    /// component, no collider, no pointer selector, and never touches the board's own
    /// <c>heroes</c>/<c>minions</c> pools. It is invisible to every identity-based
    /// <c>FindObjectsByType</c>/<c>Single</c> query the test suite already runs, and it cannot intercept a
    /// click meant for the tile or piece beneath it.</para>
    ///
    /// <para>It never reaches into the kinetic layer and the kinetic layer never reaches into it -
    /// <see cref="Match.BoardDefeatFadeDriver"/> ticks it independently of
    /// <c>KineticCueDriver</c>/<c>KineticCueUnityView</c>, which are untouched by this milestone.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DefeatFadeGhostView : MonoBehaviour
    {
        /// <summary>Mirrors <c>Match.KineticTiming.DefeatSeconds</c> (8.35y) so the echo fades over the
        /// same span the amendment already timed the Defeat cue to. Not a direct reference: Board must
        /// not depend upward on Match (the reverse is the standing dependency direction in this
        /// project), so the number is restated here with this comment as the tie between them.</summary>
        public const float DefaultFadeSeconds = 0.38f;

        /// <summary>The piece's scale at the end of the fade - shrunk, never to exactly zero, so the
        /// echo reads as leaving rather than as a mesh briefly turning inside out at scale zero.</summary>
        public const float ShrinkToScale = 0.15f;

        private MeshRenderer meshRenderer;
        private SpriteRenderer spriteRenderer;
        private Color baseSpriteColor;
        private Vector3 baseScale = Vector3.one;
        private float elapsed;
        private float duration = DefaultFadeSeconds;
        private Transform standeePanel;
        private Quaternion panelRotation;
        private bool hasStandee;
        private Material[] flatBaseMaterials;
        private Material[] standingBaseMaterials;
        private bool baseIsStanding;

        public Transform StandeePanel => standeePanel != null && standeePanel.gameObject.activeSelf ? standeePanel : null;

        /// <summary>Copy only visible geometry. The echo has no identity, scripts or picking surfaces.</summary>
        public void ConfigureStandee(Renderer source)
        {
            if (source == null) return;
            var filter = source.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;
            if (standeePanel == null)
            {
                standeePanel = new GameObject("Falling illustrated panel", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                standeePanel.SetParent(transform, false);
            }
            hasStandee = true;
            standeePanel.gameObject.SetActive(source.gameObject.activeSelf);
            standeePanel.position = source.transform.position;
            standeePanel.rotation = source.transform.rotation;
            standeePanel.localScale = source.transform.localScale;
            panelRotation = standeePanel.localRotation;
            standeePanel.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var renderer = standeePanel.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.shadowCastingMode = source.shadowCastingMode;
            renderer.receiveShadows = source.receiveShadows;
            // Keep the geometry even if the source was a top-down token at removal. The attack
            // spotlight can turn to an angled view after the authoritative removal has completed.
            RefreshRepresentation();
        }

        /// <summary>Whether this ghost is currently fading. False for a freshly created or a released
        /// (pooled, inactive) instance.</summary>
        public bool IsFading { get; private set; }

        /// <summary>How far through its fade this ghost is, 0 at spawn and 1 once finished.</summary>
        public float Progress01 => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

        /// <summary>A bare visual echo of a hero: its own <c>MeshRenderer</c>/<c>MeshFilter</c>, no other
        /// component. Call <see cref="ConfigureHero"/> before every <see cref="Begin"/>.</summary>
        public static DefeatFadeGhostView CreateHeroGhost()
        {
            var host = new GameObject("Defeat Fade Ghost (Hero)");
            var ghost = host.AddComponent<DefeatFadeGhostView>();
            host.AddComponent<MeshFilter>();
            ghost.meshRenderer = host.AddComponent<MeshRenderer>();
            host.SetActive(false);
            return ghost;
        }

        /// <summary>A bare visual echo of a minion: its own <c>SpriteRenderer</c>, no other component.
        /// Call <see cref="ConfigureMinion"/> before every <see cref="Begin"/>.</summary>
        public static DefeatFadeGhostView CreateMinionGhost()
        {
            var host = new GameObject("Defeat Fade Ghost (Minion)");
            var ghost = host.AddComponent<DefeatFadeGhostView>();
            ghost.spriteRenderer = host.AddComponent<SpriteRenderer>();
            host.SetActive(false);
            return ghost;
        }

        /// <summary>
        /// Copies a hero's visible mesh, materials and shadow settings at the instant it is removed. The
        /// mesh and materials are the SAME shared references the (now inactive) hero still holds - safe
        /// because this fade only ever changes <c>transform.localScale</c> for a hero, never a material
        /// property, and the reference is read-only for the fade's short lifetime.
        /// </summary>
        public void ConfigureHero(
            Transform source,
            Mesh mesh,
            Material[] materials,
            UnityEngine.Rendering.ShadowCastingMode shadowCastingMode,
            bool receiveShadows)
        {
            if (source == null) throw new System.ArgumentNullException(nameof(source));
            GetComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer.sharedMaterials = materials;
            flatBaseMaterials = materials;
            standingBaseMaterials = new Material[materials.Length];
            for (var index = 0; index < materials.Length; index++) standingBaseMaterials[index] = materials[0];
            baseIsStanding = false;
            meshRenderer.shadowCastingMode = shadowCastingMode;
            meshRenderer.receiveShadows = receiveShadows;
            ApplyTransform(source);
        }

        /// <summary>
        /// Copies a minion's visible sprite, colour and sorting order at the instant it is removed. The
        /// colour is copied into a field this ghost owns, so fading its OWN opacity never mutates the
        /// (now inactive) minion's own <c>SpriteRenderer</c>.
        /// </summary>
        public void ConfigureMinion(Transform source, Sprite sprite, Color color, int sortingOrder)
        {
            if (source == null) throw new System.ArgumentNullException(nameof(source));
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            baseSpriteColor = color;
            spriteRenderer.color = color;
            ApplyTransform(source);
        }

        private void ApplyTransform(Transform source)
        {
            if (standeePanel != null) standeePanel.gameObject.SetActive(false);
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (meshRenderer != null) meshRenderer.enabled = true;
            hasStandee = false;
            transform.SetPositionAndRotation(source.position, source.rotation);
            baseScale = source.localScale;
            transform.localScale = baseScale;
        }

        /// <summary>Starts (or restarts) the fade from its first frame and activates the ghost.
        /// <paramref name="durationSeconds"/> not greater than zero falls back to
        /// <see cref="DefaultFadeSeconds"/> rather than fading in zero time.</summary>
        public void Begin(float durationSeconds)
        {
            duration = durationSeconds > 0f ? durationSeconds : DefaultFadeSeconds;
            elapsed = 0f;
            IsFading = true;
            gameObject.SetActive(true);
            Apply(0f);
        }

        /// <summary>
        /// Advances the fade by one tick, in reference seconds. Returns true once the fade has completed
        /// - at that point the ghost has already deactivated itself and is ready to be pooled. Advancing
        /// a ghost that is not fading is a no-op that reports finished, so a driver never has to track
        /// which ghosts are live itself.
        /// </summary>
        public bool Advance(float deltaSeconds)
        {
            if (!IsFading)
            {
                return true;
            }

            elapsed += Mathf.Max(0f, deltaSeconds);
            var t = Progress01;
            Apply(t);
            if (t < 1f)
            {
                return false;
            }

            IsFading = false;
            gameObject.SetActive(false);
            return true;
        }

        /// <summary>Ends the fade immediately with no completion callback semantics of its own - used
        /// when the piece it was echoing has reappeared (Undo, restore, resync, Respawn) before the fade
        /// finished on its own, so the echo never outlives the event it was showing.</summary>
        public void CancelImmediately()
        {
            IsFading = false;
            gameObject.SetActive(false);
        }

        private void RefreshRepresentation()
        {
            var standing = hasStandee && standeePanel != null && standeePanel.gameObject.activeSelf;
            var camera = Camera.main;
            if (hasStandee && camera != null)
            {
                var elevation = Mathf.Asin(Mathf.Clamp(-camera.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                standing = elevation < (standing ? 70f : 69f);
            }
            if (standeePanel != null) standeePanel.gameObject.SetActive(standing);
            if (spriteRenderer != null) spriteRenderer.enabled = !standing;
            if (meshRenderer != null)
            {
                meshRenderer.enabled = true;
                if (baseIsStanding != standing)
                {
                    meshRenderer.sharedMaterials = standing ? standingBaseMaterials : flatBaseMaterials;
                    baseIsStanding = standing;
                }
            }
        }

        private void Apply(float t)
        {
            RefreshRepresentation();
            var standing = StandeePanel != null;
            var shrink = standing ? Mathf.Clamp01((t - 0.75f) / 0.25f) : t;
            transform.localScale = Vector3.Lerp(baseScale, baseScale * ShrinkToScale, shrink);
            if (standing)
            {
                var camera = Camera.main;
                if (camera != null)
                {
                    var facing = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
                    if (facing.sqrMagnitude > 0.0001f)
                        panelRotation = Quaternion.Inverse(transform.rotation) * Quaternion.LookRotation(facing, Vector3.up);
                }
                // Tip the unchanged rigid panel toward the viewer, feet fixed at the base. Complete
                // the fall at full size before the brief removal shrink; an away-facing fall made
                // the figure travel up-screen and read as levitation.
                var fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.70f));
                standeePanel.localRotation = panelRotation * Quaternion.Euler(-85f * fall, 0f, 0f);
            }
            if (spriteRenderer != null)
            {
                var opacity = 1f - t;
                spriteRenderer.color = new Color(
                    baseSpriteColor.r, baseSpriteColor.g, baseSpriteColor.b, baseSpriteColor.a * opacity);
            }

            // A hero's material is the standard opaque URP Lit/Unlit pair
            // (PrimitiveHeroView.CreatePlaceholderMaterial/CreateTokenFaceMaterial) with no alpha
            // channel wired up, so the shrink alone carries a hero's fade - the same "the scale punch
            // carries the flash on its own if the tint does nothing" idiom KineticCueUnityView's own
            // Flash cue already relies on for exactly the same reason.
        }
    }
}
