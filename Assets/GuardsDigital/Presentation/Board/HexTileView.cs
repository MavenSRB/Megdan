using GuardsDigital.Core.Board;
using GuardsDigital.Presentation.Units.Minions;
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

        /// <summary>
        /// Where the cue sits over the space. Milestone 8.11a, at the user's request: high enough to
        /// clear the piece standing there rather than sitting inside it, which is where the Minion
        /// Battle's own marker sits relative to the minion it names.
        /// </summary>
        private static readonly Vector3 ChoiceCueAnchorLocalPosition = new Vector3(0f, 0.45f, 0f);

        private static readonly Vector3 ChoiceCueLocalPosition = new Vector3(0f, 0.96f, -0.15f);

        private Material defaultEdgeMaterial;
        private bool defaultEdgeEnabled;
        private float defaultEdgeWidth;
        private bool rimmed;
        private Transform choiceCueAnchor;
        private TextMesh choiceCue;
        private TextMesh choiceCueBackground;

        public bool IsHighlighted { get; private set; }

        /// <summary>True while this space is drawing a highlight rim around its perimeter.</summary>
        public bool IsRimmed => rimmed;

        /// <summary>
        /// Milestone 8.11a, at the user's suggestion. Which board-facing cue this space is wearing -
        /// the same marker a minion wears when the Minion Battle asks which one to remove.
        /// </summary>
        public MinionChoiceCueKind ChoiceCueKind { get; private set; }

        /// <summary>
        /// Marks this space with the shared board choice cue, or clears it.
        ///
        /// <para>Milestone 8.11a, at the user's suggestion, and it is the answer to a problem colour
        /// could not solve. A rim and a tint have to contrast with whatever is painted underneath, and
        /// this board is painted pale sand, mid brown, jungle green and near-black rock - no single
        /// colour reads on all four. The cue does not have to: it carries its own dark backing, which is
        /// exactly why it was built that way for the Minion Battle, so it reads the same everywhere.</para>
        ///
        /// <para>Reusing it also means a player learns one mark. The glyph that says "this minion" when
        /// the Battle asks, and the glyph that says "this space" when a card's text asks, are the same
        /// glyph in the same two colours.</para>
        /// </summary>
        public void SetChoiceCue(MinionChoiceCueKind kind)
        {
            if (kind != MinionChoiceCueKind.None && kind != MinionChoiceCueKind.Highlighted &&
                kind != MinionChoiceCueKind.Selected)
            {
                throw new System.ArgumentOutOfRangeException(nameof(kind));
            }

            ChoiceCueKind = kind;
            if (kind == MinionChoiceCueKind.None)
            {
                if (choiceCue != null)
                {
                    choiceCue.gameObject.SetActive(false);
                    choiceCueBackground.gameObject.SetActive(false);
                }

                return;
            }

            if (choiceCue == null)
            {
                // A tile's own transform lies flat with no rotation, so a cue parented straight to it
                // would stand edge-on to the overhead camera and be invisible. The anchor carries the
                // board-facing rotation for the same reason the upright hero prism's does.
                var anchor = new GameObject("Space Choice Cue Anchor");
                anchor.transform.SetParent(transform, false);
                anchor.transform.localPosition = ChoiceCueAnchorLocalPosition;
                anchor.transform.localRotation = BoardChoiceCue.BoardFacingRotation;
                choiceCueAnchor = anchor.transform;
                BoardChoiceCue.CreatePair(
                    choiceCueAnchor,
                    "Space Choice Cue",
                    ChoiceCueLocalPosition,
                    mirrored: true,
                    out choiceCue,
                    out choiceCueBackground);
                choiceCue.characterSize = BoardChoiceCue.MarkerCharacterSize;
            }

            // Re-applied on every call rather than only at construction. The direction the framing
            // camera calls up is settled when the board is framed, which can be after an anchor already
            // exists, and an anchor holding a stale one is drawn visibly crooked.
            choiceCueAnchor.localRotation = BoardChoiceCue.BoardFacingRotation;
            choiceCue.gameObject.SetActive(true);
            choiceCueBackground.gameObject.SetActive(true);
            choiceCue.text = BoardChoiceCue.Marker;
            choiceCue.color = kind == MinionChoiceCueKind.Selected
                ? BoardChoiceCue.SelectedColor
                : BoardChoiceCue.HighlightedColor;
            BoardChoiceCue.Center(
                choiceCueAnchor, choiceCue, choiceCueBackground, ChoiceCueLocalPosition);
        }

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
                meshRenderer.sharedMaterial = highlighted ? highlightMaterial : defaultMaterial;
            }

            ApplyRim(highlighted, rimMaterial);
            IsHighlighted = highlighted;
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
