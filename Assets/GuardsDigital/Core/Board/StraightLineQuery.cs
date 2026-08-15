using System;

namespace GuardsDigital.Core.Board
{
    /// <summary>
    /// The p.19 straight-line rule: "A straight line is a sequence of spaces arranged in a file. A
    /// single space counts as a straight line. Two units are in a straight line if they are adjacent,
    /// or if you can draw a straight line of spaces through spaces occupied by both of those units."
    ///
    /// It answers direction rather than only membership, because the rule's consumer is p.18's Push -
    /// "move it in a straight line directly away from the pushing unit" - which needs to know which of
    /// the six files the two objects share before it can walk away along it. Purely geometric: nothing
    /// here reads occupancy or the map, so an off-board coordinate is still on a straight line. Whether
    /// a space may be entered is <see cref="GuardsDigital.Core.Rules.Modifications.ObstacleQuery"/>'s
    /// question, and keeping the two apart is what lets a blocked push still know its direction.
    /// </summary>
    public static class StraightLineQuery
    {
        private static readonly HexDirection[] Directions =
        {
            HexDirection.East,
            HexDirection.NorthEast,
            HexDirection.NorthWest,
            HexDirection.West,
            HexDirection.SouthWest,
            HexDirection.SouthEast
        };

        /// <summary>
        /// The direction and space count from <paramref name="from"/> to <paramref name="to"/> when the
        /// two share a file, or <c>false</c> when they do not.
        ///
        /// Identical coordinates report <c>false</c>. p.19's "a single space counts as a straight line"
        /// describes the degenerate line itself, not two units standing in one space, which p.18's
        /// one-obstacle-per-space rule forbids outright; a caller asking for the direction between an
        /// object and itself has no direction to be given.
        /// </summary>
        public static bool TryGetDirection(
            HexCoordinate from,
            HexCoordinate to,
            out HexDirection direction,
            out int distance)
        {
            direction = default;
            distance = 0;
            if (from.Equals(to))
            {
                return false;
            }

            var separation = HexDistanceQuery.Calculate(from, to);
            foreach (var candidate in Directions)
            {
                if (Step(from, candidate, separation).Equals(to))
                {
                    direction = candidate;
                    distance = separation;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether two spaces lie on one file. Adjacency is the <paramref name="separation"/>-of-one
        /// case rather than a separate rule, which is why p.19 states both in one sentence.
        /// </summary>
        public static bool AreInStraightLine(HexCoordinate first, HexCoordinate second) =>
            TryGetDirection(first, second, out _, out _);

        /// <summary>
        /// The coordinate <paramref name="steps"/> spaces along <paramref name="direction"/>. Zero steps
        /// is the origin itself, which is what a zero-space push (p.18) resolves to.
        /// </summary>
        public static HexCoordinate Step(HexCoordinate origin, HexDirection direction, int steps)
        {
            if (steps < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(steps), steps, "A straight-line step count must not be negative.");
            }

            var current = origin;
            for (var index = 0; index < steps; index++)
            {
                current = current.GetNeighbor(direction);
            }

            return current;
        }
    }
}
