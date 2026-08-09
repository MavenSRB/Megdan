using System;
using System.Collections.Generic;

namespace GuardsDigital.Core.Board
{
    public enum HexDirection
    {
        East,
        NorthEast,
        NorthWest,
        West,
        SouthWest,
        SouthEast
    }

    public readonly struct HexCoordinate : IEquatable<HexCoordinate>
    {
        private static readonly HexCoordinate[] DirectionOffsets =
        {
            new HexCoordinate(1, 0),
            new HexCoordinate(1, -1),
            new HexCoordinate(0, -1),
            new HexCoordinate(-1, 0),
            new HexCoordinate(-1, 1),
            new HexCoordinate(0, 1)
        };

        public HexCoordinate(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }

        public int R { get; }

        public HexCoordinate GetNeighbor(HexDirection direction)
        {
            var directionIndex = (int)direction;
            if (directionIndex < 0 || directionIndex >= DirectionOffsets.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown hex direction.");
            }

            var offset = DirectionOffsets[directionIndex];
            return new HexCoordinate(Q + offset.Q, R + offset.R);
        }

        public IReadOnlyList<HexCoordinate> GetNeighbors()
        {
            var neighbors = new HexCoordinate[DirectionOffsets.Length];
            for (var index = 0; index < DirectionOffsets.Length; index++)
            {
                neighbors[index] = new HexCoordinate(Q + DirectionOffsets[index].Q, R + DirectionOffsets[index].R);
            }

            return neighbors;
        }

        public bool Equals(HexCoordinate other)
        {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Q * 397) ^ R;
            }
        }

        public override string ToString()
        {
            return $"({Q}, {R})";
        }
    }
}
