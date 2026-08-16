using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Target eligibility, which rulebook p.19 separates from presence: "Whenever your action has any
    /// effect on another unit in any way, you target that unit." Targeting applies immunity - p.18: "If
    /// a unit is immune, you cannot target that unit" - while <see cref="UnitPresenceQuery"/> is the
    /// other half of the pair and consults none, so a presence question over the same spaces still
    /// counts what this query drops.
    ///
    /// Milestone 6.8k implements the disk-shaped slice the target-selection capability needs: every
    /// board object within a rulebook distance of an origin, immunity filtered per activity. The wider
    /// relationship vocabulary (friendly, enemy, owner, controller, source) stays specified in
    /// <c>BOARD_CAPABILITIES.md</c> until a verified card needs it; adding it here untested would
    /// repeat the no-consumer defect 6.8c and 6.8b were scheduled to correct.
    ///
    /// Distance counts spaces including spaces with obstacles (p.18, Near/Far), so this is
    /// <see cref="HexDistanceQuery"/> geometry, never a route length. Off-board coordinates hold no
    /// object, so the disk needs no board-membership test of its own.
    /// </summary>
    public static class TargetQuery
    {
        /// <summary>
        /// Every board object within <paramref name="range"/> spaces of <paramref name="origin"/> that
        /// <paramref name="targetingUnit"/> may target under <paramref name="activity"/>, in canonical
        /// axial order.
        ///
        /// A token is never immunity-filtered: p.19 states tokens are not units, so the rules-object
        /// vocabulary has no token member and no immunity can be addressed to one - its
        /// <see cref="BoardObjectReference.AsRuleObject"/> is the named absence. The object standing at
        /// the origin itself - the selecting unit, when the origin is its own space - is admitted only
        /// when <paramref name="includeSelf"/> says so, because p.19's relationship vocabulary keeps
        /// self and another distinct.
        /// </summary>
        public static IReadOnlyList<BoardObjectReference> FindTargetsWithin(
            HexCoordinate origin,
            int range,
            bool includeSelf,
            RuleObject targetingUnit,
            RuleActivity activity,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (range < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(range), range, "A targeting question cannot use a negative range.");
            }

            if (activity == RuleActivity.None)
            {
                throw new ArgumentException(
                    "A targeting question requires the activity doing the targeting, because p.18 " +
                    "declares immunity per activity.", nameof(activity));
            }

            var found = new List<BoardObjectReference>();
            for (var dq = -range; dq <= range; dq++)
            {
                for (var dr = Math.Max(-range, -dq - range); dr <= Math.Min(range, -dq + range); dr++)
                {
                    var coordinate = new HexCoordinate(origin.Q + dq, origin.R + dr);
                    if (!includeSelf && coordinate.Equals(origin))
                    {
                        continue;
                    }

                    if (!board.TryGetObjectAt(coordinate, out var occupant))
                    {
                        continue;
                    }

                    var asRuleObject = occupant.AsRuleObject();
                    if (asRuleObject.IsDefined && ImmunityQuery.IsImmuneTo(
                            asRuleObject, activity, targetingUnit, board, participants, modifications))
                    {
                        continue;
                    }

                    found.Add(occupant);
                }
            }

            return found;
        }
    }
}
