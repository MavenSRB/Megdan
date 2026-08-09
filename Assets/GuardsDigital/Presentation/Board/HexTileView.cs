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

        public HexCoordinate Coordinate => new HexCoordinate(q, r);

        public float OuterRadius => outerRadius;

        public bool IsHighlighted { get; private set; }

        public void Configure(HexCoordinate coordinate, float radius)
        {
            q = coordinate.Q;
            r = coordinate.R;
            outerRadius = radius;
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
