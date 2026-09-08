using GuardsDigital.Core.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    [DisallowMultipleComponent]
    public sealed class HexTileView : MonoBehaviour
    {
        [SerializeField]
        private int q;

        [SerializeField]
        private int r;

        [SerializeField]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        [SerializeField]
        private Material defaultMaterial;

        [SerializeField]
        private string spaceId;

        [SerializeField]
        private BoardMapSpaceKind spaceKind = BoardMapSpaceKind.Playable;

        [SerializeField]
        private string zoneId;

        public HexCoordinate Coordinate => new HexCoordinate(q, r);

        public float OuterRadius => outerRadius;

        public SpaceId SpaceId => new SpaceId(spaceId);

        public BoardMapSpaceKind SpaceKind => spaceKind;

        public bool IsPlayable => spaceKind == BoardMapSpaceKind.Playable;

        public ZoneId? ZoneId => string.IsNullOrWhiteSpace(zoneId) ? (ZoneId?)null : new ZoneId(zoneId);

        /// <summary>How much wider a highlight's rim is drawn than the ordinary debug perimeter.</summary>
        public const float RimWidthMultiplier = 2.4f;

        private Material defaultEdgeMaterial;
        private bool defaultEdgeEnabled;
        private float defaultEdgeWidth;
        private bool rimmed;

        public bool IsHighlighted { get; private set; }

        /// <summary>True while this space is drawing a highlight rim around its perimeter.</summary>
        public bool IsRimmed => rimmed;

        public HexCornerHighlight CornerHighlight => GetComponent<HexCornerHighlight>();

        public void Configure(HexCoordinate coordinate, float radius)
        {
            q = coordinate.Q;
            r = coordinate.R;
            outerRadius = radius;
            if (string.IsNullOrWhiteSpace(spaceId))
            {
                spaceId = $"q{coordinate.Q}_r{coordinate.R}";
            }
            ApplyLayout();
        }

        public void Configure(BoardMapSpace space, float radius, Material material)
        {
            if (space == null) throw new System.ArgumentNullException(nameof(space));
            q = space.Coordinate.Q;
            r = space.Coordinate.R;
            outerRadius = radius;
            spaceId = space.Id.Value;
            spaceKind = space.Kind;
            zoneId = space.ZoneId?.Value;
            defaultMaterial = material;
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = material;
                meshRenderer.enabled = DrawsAnything(material);
            }
            IsHighlighted = false;
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            transform.localPosition = HexWorldLayout.ToWorld(Coordinate, outerRadius);
        }

        public void SetHighlighted(bool highlighted, Material highlightMaterial) =>
            SetHighlighted(highlighted, highlightMaterial, null);

        /// <summary>
        /// Milestone 8.11a. Highlights the space, optionally drawing its perimeter in the highlight's
        /// own colour.
        ///
        /// <para>The rim reuses the debug edge already on every tile rather than adding a second
        /// renderer. That edge is switched off while the official artwork is underneath, which is
        /// exactly when a fill alone reads weakest, so the same line does two jobs and neither the
        /// caller nor the map view has to know which case it is in. Clearing a highlight puts the edge
        /// back the way the map view left it.</para>
        /// </summary>
        public void SetHighlighted(bool highlighted, Material highlightMaterial, Material rimMaterial)
        {
            if (highlighted && highlightMaterial == null)
            {
                throw new System.ArgumentNullException(nameof(highlightMaterial));
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            CaptureDefaultMaterial(meshRenderer);

            if (meshRenderer != null)
            {
                var applied = highlighted ? highlightMaterial : defaultMaterial;
                meshRenderer.sharedMaterial = applied;
                meshRenderer.enabled = DrawsAnything(applied);
            }

            ApplyRim(highlighted, rimMaterial);
            IsHighlighted = highlighted;
        }

        /// <summary>The child layer one space's Active-effect aura is drawn on.</summary>
        public const string ActiveEffectAuraLayerName = "Active Effect Aura";

        /// <summary>
        /// How far above the tile plane the aura lies: above the board's own surface, and BELOW
        /// everything a player acts on.
        ///
        /// <para>Milestone 8.35z63, and the number is chosen against the heights already in
        /// use rather than picked: the tile's debug perimeter sits at 0.015, the six-corner halo and
        /// core at 0.075 and 0.085, and a hero piece at 0.03. An aura at 0.010 is therefore under every
        /// mark and every piece on the board, which is the whole overlap rule - an ongoing effect may
        /// never cover an affordance a player is being asked to click.</para>
        /// </summary>
        public const float ActiveEffectAuraSurfaceOffset = 0.010f;

        /// <summary>Whether this space is currently tinted by a live Active effect's disk.</summary>
        public bool HasActiveEffectAura
        {
            get
            {
                var layer = transform.Find(ActiveEffectAuraLayerName);
                if (layer == null) return false;
                var renderer = layer.GetComponent<MeshRenderer>();
                return renderer != null && renderer.enabled;
            }
        }

        /// <summary>
        /// Tints, or clears, this space as part of a live Active effect's disk.
        ///
        /// <para><b>Its own layer, and deliberately not <see cref="SetHighlighted"/>.</b> That call
        /// swaps the tile's own fill and <see cref="IsHighlighted"/> means one specific thing across
        /// this project's whole fixture surface - "a whole-hex fill is standing in for a board CHOICE",
        /// which milestone 8.30h retired and a dozen journeys assert is false on every destination they
        /// click. An aura is not a choice and must not borrow that flag, or every one of those
        /// assertions would start reporting a fill that is not the one they are about. So the aura is a
        /// separate child renderer underneath the marks, and the two states can be read apart.</para>
        ///
        /// <para>The mesh and material are the caller's and are SHARED across every tinted space - one
        /// hex mesh at this board's radius, one violet tint - so a disk of sixty-one spaces costs sixty
        /// -one renderers and no extra native resources. This view owns neither and destroys
        /// neither.</para>
        /// </summary>
        public void SetActiveEffectAura(bool lit, Mesh sharedMesh, Material sharedMaterial)
        {
            var layer = transform.Find(ActiveEffectAuraLayerName);
            if (!lit)
            {
                if (layer != null)
                {
                    var existing = layer.GetComponent<MeshRenderer>();
                    if (existing != null) existing.enabled = false;
                }

                return;
            }

            if (sharedMesh == null) throw new System.ArgumentNullException(nameof(sharedMesh));
            if (sharedMaterial == null) throw new System.ArgumentNullException(nameof(sharedMaterial));

            if (layer == null)
            {
                layer = new GameObject(ActiveEffectAuraLayerName).transform;
                layer.SetParent(transform, false);
                // The same layer the space it belongs to is on, exactly as `BoardTargetRing` gives its
                // generated cue object the layer of the piece it hangs under.
                layer.gameObject.layer = gameObject.layer;
            }

            layer.localPosition = new Vector3(0f, ActiveEffectAuraSurfaceOffset, 0f);
            layer.localRotation = Quaternion.identity;
            auraGlide = Vector3.zero;
            layer.localScale = Vector3.one;
            var filter = layer.GetComponent<MeshFilter>() ??
                layer.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = sharedMesh;
            var renderer = layer.GetComponent<MeshRenderer>() ??
                layer.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = sharedMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = true;
        }

        private Vector3 auraGlide;

        /// <summary>How far this space's aura is currently offset from the space itself.</summary>
        public Vector3 ActiveEffectAuraGlide => auraGlide;

        /// <summary>
        /// Slides this space's aura quad, and only it, by a world offset in the board plane.
        ///
        /// <para><b>Milestone 8.35z63, the user's follow-up: "Does it fly with animation?"</b>
        /// While a carrier's token is being slid by the motion system, its whole disk is drawn offset
        /// by the same displacement, so the aura travels with the piece instead of teleporting a hex
        /// ahead of it. The lit SET never changes for this - it stays the settled, hex-derived one -
        /// and the offset returns to zero when the slide ends. Because a hex disk translated by a
        /// lattice vector IS the disk around the destination, the two agree exactly at the end and
        /// there is no snap to hide.</para>
        ///
        /// <para>Only X and Z are taken. The aura's height is
        /// <see cref="ActiveEffectAuraSurfaceOffset"/>'s to decide and a glide may never lift a tint
        /// over the marks and pieces it is required to stay beneath.</para>
        /// </summary>
        public void SetActiveEffectAuraGlide(Vector3 worldOffset)
        {
            var layer = transform.Find(ActiveEffectAuraLayerName);
            if (layer == null)
            {
                return;
            }

            auraGlide = new Vector3(worldOffset.x, 0f, worldOffset.z);
            layer.localPosition = new Vector3(
                auraGlide.x, ActiveEffectAuraSurfaceOffset, auraGlide.z);
        }

        public void ConfigureCornerHighlight(HexCornerHighlightTemplate template)
        {
            var highlight = GetComponent<HexCornerHighlight>() ??
                gameObject.AddComponent<HexCornerHighlight>();
            highlight.Configure(template);
        }

        public void SetCornerHighlighted(
            HexCornerHighlightKind kind,
            bool hovered,
            HexCornerHighlightKind contributionKind = HexCornerHighlightKind.None)
        {
            var highlight = CornerHighlight;
            if (highlight == null)
            {
                if (kind == HexCornerHighlightKind.None &&
                    contributionKind == HexCornerHighlightKind.None) return;
                throw new System.InvalidOperationException("The tile has no shared corner-highlight template.");
            }

            highlight.Set(kind, hovered, contributionKind);
        }

        private void ApplyRim(bool highlighted, Material rimMaterial)
        {
            var edge = GetComponentInChildren<LineRenderer>(true);
            if (edge == null) return;

            if (highlighted && rimMaterial != null)
            {
                if (!rimmed)
                {
                    defaultEdgeMaterial = edge.sharedMaterial;
                    defaultEdgeEnabled = edge.enabled;
                    defaultEdgeWidth = edge.startWidth;
                    rimmed = true;
                }

                edge.sharedMaterial = rimMaterial;
                edge.enabled = true;
                edge.startWidth = defaultEdgeWidth * RimWidthMultiplier;
                edge.endWidth = defaultEdgeWidth * RimWidthMultiplier;
                return;
            }

            if (!rimmed) return;
            edge.sharedMaterial = defaultEdgeMaterial;
            edge.enabled = defaultEdgeEnabled;
            edge.startWidth = defaultEdgeWidth;
            edge.endWidth = defaultEdgeWidth;
            rimmed = false;
        }

        /// <summary>
        /// Whether a fill draws anything at all. A fully transparent fill draws nothing by not
        /// being rendered, rather than by being rendered at zero alpha.
        ///
        /// <para><b>Milestone 8.31b, and the distinction is the whole defect.</b> Blending is a
        /// shader variant, and a built player keeps only the variants its build data asks for, so a
        /// fill blended purely at runtime came out opaque there: every space over the official
        /// artwork drew solid white and hid the board, while the Editor - which holds every variant
        /// - looked correct. A renderer that is switched off draws nothing on any build, under any
        /// stripping. Spaces stay pickable either way, because a space is chosen by where the
        /// pointer ray meets the board plane and never by what is drawn on it.</para>
        /// </summary>
        private static bool DrawsAnything(Material material) =>
            material != null && material.color.a > 0.001f;

        private void CaptureDefaultMaterial(MeshRenderer meshRenderer)
        {
            if (defaultMaterial == null && meshRenderer != null)
            {
                defaultMaterial = meshRenderer.sharedMaterial;
            }
        }

        private void Awake()
        {
            ApplyLayout();
            CaptureDefaultMaterial(GetComponent<MeshRenderer>());
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
