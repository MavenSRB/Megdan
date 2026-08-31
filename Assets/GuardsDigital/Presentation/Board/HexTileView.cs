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

        public void ConfigureCornerHighlight(HexCornerHighlightTemplate template)
        {
            var highlight = GetComponent<HexCornerHighlight>() ??
                gameObject.AddComponent<HexCornerHighlight>();
            highlight.Configure(template);
        }

        public void SetCornerHighlighted(HexCornerHighlightKind kind, bool hovered)
        {
            var highlight = CornerHighlight;
            if (highlight == null)
            {
                if (kind == HexCornerHighlightKind.None) return;
                throw new System.InvalidOperationException("The tile has no shared corner-highlight template.");
            }

            highlight.Set(kind, hovered);
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
