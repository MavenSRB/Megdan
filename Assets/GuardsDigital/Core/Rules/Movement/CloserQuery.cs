using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Rules.Movement
{
    /// <summary>
    /// The p.18 `Closer (Space)` rule: "A space is closer to you than another space if there are fewer
    /// spaces on the shortest path of spaces connecting you and that space (including spaces with
    /// obstacles)."
    ///
    /// The parenthesis is the whole contract. Distance here counts spaces *through* obstacles, so this
    /// is the geometric separation <see cref="HexDistanceQuery"/> already computes and deliberately not
    /// a reachable-path length: a destination can be closer to you while no legal route to it exists,
    /// and p.18's `Nearest` and `Farther / Farthest` entries define themselves the same way. Whether
    /// the mover can actually get there is the obstacle and reachability question, asked separately.
    ///
    /// It exists because milestone 6.4's direct Move must apply a source-relative destination policy -
    /// card text such as `move a unit towards you` - through a typed query rather than effect-local
    /// geometry, which is that milestone's fourth acceptance check. There is no separate Pull keyword;
    /// `towards` is this comparison plus the effect's own distance policy.
    /// </summary>
    public static class CloserQuery
    {
        /// <summary>
        /// Whether <paramref name="candidate"/> is closer to <paramref name="source"/> than
        /// <paramref name="comparison"/> is. Equal separation is not closer, so a sideways step around
        /// the source is refused by a `towards` policy exactly as an outward one is.
        /// </summary>
        public static bool IsCloserTo(
            HexCoordinate source,
            HexCoordinate candidate,
            HexCoordinate comparison) =>
            HexDistanceQuery.Calculate(source, candidate) <
            HexDistanceQuery.Calculate(source, comparison);

        /// <summary>
        /// The candidates a `towards` policy admits: those strictly closer to <paramref name="source"/>
        /// than the moving object's own starting space. Ordered by axial coordinate so a projected
        /// candidate list is deterministic across replay, which every choice this feeds requires.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> FilterCloserTo(
            HexCoordinate source,
            HexCoordinate origin,
            IEnumerable<HexCoordinate> candidates)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            return Array.AsReadOnly(candidates
                .Where(candidate => IsCloserTo(source, candidate, origin))
                .OrderBy(candidate => candidate.Q)
                .ThenBy(candidate => candidate.R)
                .ToArray());
        }
    }
}
