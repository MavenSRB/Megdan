using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Whether two spaces lie on one file. Adjacency is the separation-of-one case rather than a
        /// separate rule, which is why p.19 states both in one sentence, and one space is p.19's own
        /// degenerate line - "a single space counts as a straight line" - so a space is in a straight
        /// line with itself.
        ///
        /// That last answer is where this parts company with <see cref="TryGetDirection"/>, which
        /// reports <c>false</c> for identical coordinates. The two are asking different questions:
        /// membership of one file, which the degenerate line satisfies, and which of the six files to
        /// walk along, which a line of one space cannot name. Milestone 8.6 needs the membership
        /// reading, because <see cref="FilterOutStraightLine"/> is its complement and a complement that
        /// admitted the origin would say the actor's own space is not in a straight line from the
        /// actor.
        /// </summary>
        public static bool AreInStraightLine(HexCoordinate first, HexCoordinate second) =>
            first.Equals(second) || TryGetDirection(first, second, out _, out _);

        /// <summary>
        /// Whether an ordered sequence of spaces is p.19's straight line: "a sequence of spaces
        /// arranged in a file. A single space counts as a straight line."
        ///
        /// Both halves of the page's sentence are stated here rather than left to a caller's loop. A
        /// single space is a line; two or more are a line when every consecutive pair steps once along
        /// one shared direction, so a file with a gap in it, a bend, or a repeated space is not one.
        /// An empty sequence is no line at all - p.19 describes spaces arranged in a file, and there is
        /// nothing there to arrange.
        /// </summary>
        public static bool IsStraightLine(IReadOnlyList<HexCoordinate> spaces)
        {
            if (spaces == null)
            {
                throw new ArgumentNullException(nameof(spaces));
            }

            if (spaces.Count == 0)
            {
                return false;
            }

            if (spaces.Count == 1)
            {
                return true;
            }

            if (!TryGetDirection(spaces[0], spaces[1], out var direction, out var firstStep) ||
                firstStep != 1)
            {
                return false;
            }

            for (var index = 1; index < spaces.Count; index++)
            {
                if (!spaces[index].Equals(Step(spaces[0], direction, index)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The candidates that are **not** in a straight line from <paramref name="origin"/>, in
        /// canonical axial order - p.19's rule read as a complement, which is what Wasp's Telekinesis,
        /// Mass Telekinesis and both Boomerangs print: "in range, which is not in a straight line".
        ///
        /// It is stated once here rather than at each consuming card, because the complement of a rule
        /// is still that rule and a card that re-derived it could disagree with the query about the
        /// adjacent case p.19 calls out by name. The origin itself is dropped for the same reason: it
        /// is in a straight line with itself, so it cannot be in the complement.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> FilterOutStraightLine(
            HexCoordinate origin,
            IEnumerable<HexCoordinate> candidates)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            return Array.AsReadOnly(candidates
                .Where(candidate => !AreInStraightLine(origin, candidate))
                .OrderBy(candidate => candidate.Q)
                .ThenBy(candidate => candidate.R)
                .ToArray());
        }

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
