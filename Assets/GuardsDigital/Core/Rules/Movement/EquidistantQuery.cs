using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Rules.Movement
{
    /// <summary>
    /// "Without moving it away from you or closer to you" - the orbit destination policy Wasp's Lift
    /// Up, Control Gravity and Center of Mass print, stated as the exact boundary between p.18's
    /// `Closer (Space)` and its `Farther / Farthest` sibling.
    ///
    /// It owns no measure of its own. The card's sentence names two rules and negates both, so this
    /// asks <see cref="CloserQuery"/> twice - once each way round - and a destination is admitted only
    /// when neither answer is yes. That keeps the obstacle-inclusive distance p.17 and p.19 define in
    /// one place: were the equality written here as its own comparison, this query and a `towards`
    /// policy could disagree about what "closer" counts, which is precisely the split milestone 6.5ad
    /// corrected between the direct Move and Object Push readings of `up to`.
    ///
    /// Like <see cref="CloserQuery"/>, this is separation and never reachability. A space can be the
    /// same distance from the acting hero while no legal route reaches it; whether the mover can get
    /// there is the obstacle and reachability question, asked separately by whatever walks the board.
    ///
    /// Milestone 8.6.
    /// </summary>
    public static class EquidistantQuery
    {
        /// <summary>
        /// Whether <paramref name="candidate"/> is neither closer to nor farther from
        /// <paramref name="source"/> than <paramref name="comparison"/> is - the ring the moving object
        /// already stands on.
        ///
        /// The origin's own space satisfies this trivially, because a space is the same distance from
        /// the source as itself. That is the honest geometric answer and deliberately not a decision
        /// about whether standing still is a legal result: p.11 gives that decision to the node's
        /// failure policy, exactly as it does for a `towards` move, so this query never removes an
        /// option the policy is entitled to admit.
        /// </summary>
        public static bool IsEquidistantFrom(
            HexCoordinate source,
            HexCoordinate candidate,
            HexCoordinate comparison) =>
            !CloserQuery.IsCloserTo(source, candidate, comparison) &&
            !CloserQuery.IsCloserTo(source, comparison, candidate);

        /// <summary>
        /// The candidates an orbit policy admits: those exactly as far from <paramref name="source"/>
        /// as the moving object's own starting space. Ordered by axial coordinate so a projected
        /// candidate list is deterministic across replay, which every choice this feeds requires -
        /// the same ordering contract <see cref="CloserQuery.FilterCloserTo"/> states.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> FilterEquidistantTo(
            HexCoordinate source,
            HexCoordinate origin,
            IEnumerable<HexCoordinate> candidates)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            return Array.AsReadOnly(candidates
                .Where(candidate => IsEquidistantFrom(source, candidate, origin))
                .OrderBy(candidate => candidate.Q)
                .ThenBy(candidate => candidate.R)
                .ToArray());
        }
    }
}
