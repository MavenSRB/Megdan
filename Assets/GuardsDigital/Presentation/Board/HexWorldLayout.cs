using System;
using GuardsDigital.Core.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    public static class HexWorldLayout
    {
        public const float DefaultOuterRadius = 1f;

        public static Vector3 ToWorld(HexCoordinate coordinate, float outerRadius = DefaultOuterRadius)
        {
            if (outerRadius <= 0f || float.IsNaN(outerRadius) || float.IsInfinity(outerRadius))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outerRadius),
                    outerRadius,
                    "Hex outer radius must be a finite positive value.");
            }

            var x = Mathf.Sqrt(3f) * outerRadius * (coordinate.Q + (coordinate.R * 0.5f));
            var z = -1.5f * outerRadius * coordinate.R;
            return new Vector3(x, 0f, z);
        }
    }
}
