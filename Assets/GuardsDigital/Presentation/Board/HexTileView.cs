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

        public HexCoordinate Coordinate => new HexCoordinate(q, r);

        public float OuterRadius => outerRadius;

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

        private void Awake()
        {
            ApplyLayout();
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
