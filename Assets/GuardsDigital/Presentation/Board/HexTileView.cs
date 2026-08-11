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

        public bool IsHighlighted { get; private set; }

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

        public void SetHighlighted(bool highlighted, Material highlightMaterial)
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

            IsHighlighted = highlighted;
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
