using System;

namespace GuardsDigital.Core.Board
{
    /// <summary>The three unoriented straight-line axes through one hex.</summary>
    public enum BoardPartitionAxis
    {
        None = 0,
        EastWest = 1,
        NorthEastSouthWest = 2,
        NorthWestSouthEast = 3
    }

    public enum BoardPartitionSide
    {
        Negative = -1,
        OnLine = 0,
        Positive = 1
    }

    /// <summary>
    /// A line fixed at the space and axis chosen when an effect was created. Spaces on
    /// the line belong to neither side; moving the choosing hero does not move it.
    /// This type answers geometry only, never whether an interaction is permitted.
    /// </summary>
    public readonly struct BoardPartitionLine : IEquatable<BoardPartitionLine>
    {
        private readonly bool isDefined;

        public BoardPartitionLine(HexCoordinate origin, BoardPartitionAxis axis)
        {
            if (axis != BoardPartitionAxis.EastWest &&
                axis != BoardPartitionAxis.NorthEastSouthWest &&
                axis != BoardPartitionAxis.NorthWestSouthEast)
                throw new ArgumentOutOfRangeException(nameof(axis), axis,
                    "A board partition names one of the three straight-line axes.");
            Origin = origin;
            Axis = axis;
            isDefined = true;
        }

        public bool IsDefined => isDefined;
        public HexCoordinate Origin { get; }
        public BoardPartitionAxis Axis { get; }

        public BoardPartitionSide SideOf(HexCoordinate space)
        {
            if (!isDefined)
                throw new InvalidOperationException("An undefined partition has no sides.");
            long offset = Axis == BoardPartitionAxis.EastWest
                ? (long)space.R - Origin.R
                : Axis == BoardPartitionAxis.NorthEastSouthWest
                    ? (long)space.Q + space.R - Origin.Q - Origin.R
                    : (long)space.Q - Origin.Q;
            return offset < 0 ? BoardPartitionSide.Negative
                : offset > 0 ? BoardPartitionSide.Positive
                : BoardPartitionSide.OnLine;
        }

        public bool Separates(HexCoordinate first, HexCoordinate second)
        {
            var firstSide = SideOf(first);
            var secondSide = SideOf(second);
            return firstSide != BoardPartitionSide.OnLine &&
                secondSide != BoardPartitionSide.OnLine &&
                firstSide != secondSide;
        }

        public bool Equals(BoardPartitionLine other) =>
            isDefined == other.isDefined &&
            (!isDefined || (Origin.Equals(other.Origin) && Axis == other.Axis));

        public override bool Equals(object obj) =>
            obj is BoardPartitionLine other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return !isDefined ? 0 : (Origin.GetHashCode() * 397) ^ (int)Axis;
            }
        }
    }
}
