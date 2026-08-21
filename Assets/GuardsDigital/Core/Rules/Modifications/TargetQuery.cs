using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

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
    /// board object within a rulebook distance of an origin, immunity filtered per activity. Milestone
    /// 8.8 adds the first relationship arm, <see cref="StandsInRelationTo"/>, because Wasp's two kinetic
    /// Skills print "enemy units" and a card finally needed one. The rest of the vocabulary (friendly,
    /// owner, controller, source) stays specified in <c>BOARD_CAPABILITIES.md</c> until a verified card
    /// needs it; adding it here untested would repeat the no-consumer defect 6.8c and 6.8b were
    /// scheduled to correct.
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

        /// <summary>
        /// Whether <paramref name="target"/> stands on the side of the board <paramref name="relation"/>
        /// names, measured from <paramref name="sourceHero"/>.
        ///
        /// <para>Milestone 8.8 opens the relationship half of p.19's vocabulary, and opens exactly the
        /// arm a verified card needed: Kinetic Repulse and Kinetic Blast print "up to 2 enemy units
        /// adjacent to you", so a targeting question can now say whose units it means. The wider
        /// vocabulary the capability document specifies - owner, controller, source - still waits for a
        /// card, for the reason the disk above waited: adding it with no consumer repeats the defect
        /// milestones 6.8b and 6.8c were scheduled to correct.</para>
        ///
        /// <para>It reuses <see cref="SourceTeamRelation"/> rather than minting a fourth team vocabulary.
        /// That enum already means "which side of the source a unit stands on", declared from the card's
        /// perspective so the declaration says the same thing whichever team plays it, and the pushing
        /// hero is this question's source.</para>
        ///
        /// <para>A token is on nobody's side. p.19 states that tokens are not units, so a relation that
        /// names a team excludes one, while <see cref="SourceTeamRelation.AnyTeam"/> - the declaration
        /// that names no side at all - admits it along with everything else. A hero the match does not
        /// list, or a source the match does not list, is on no known side either: this answers false
        /// rather than asking <see cref="MatchParticipants.GetTeamForHero"/> a question it throws on.</para>
        /// </summary>
        public static bool StandsInRelationTo(
            SourceTeamRelation relation,
            BoardObjectReference target,
            HeroId sourceHero,
            MatchParticipants participants,
            IBoardDisplacementAuthority board)
        {
            if (relation == SourceTeamRelation.None || !Enum.IsDefined(typeof(SourceTeamRelation), relation))
            {
                throw new ArgumentException(
                    "A targeting relation must name a side, or SourceTeamRelation.AnyTeam when it " +
                    "names none.",
                    nameof(relation));
            }

            if (relation == SourceTeamRelation.AnyTeam)
            {
                return true;
            }

            if (!sourceHero.IsDefined ||
                !TryFindTeam(BoardObjectReference.ForHero(sourceHero), participants, board, out var source) ||
                !TryFindTeam(target, participants, board, out var side))
            {
                return false;
            }

            return relation == SourceTeamRelation.OpposingSource
                ? !side.Equals(source)
                : side.Equals(source);
        }

        /// <summary>
        /// The team a board object belongs to. A hero's is the match's, a minion carries its own, and a
        /// token has none - which is why this reports whether there is one rather than returning a
        /// default that would compare equal to a real team.
        /// </summary>
        private static bool TryFindTeam(
            BoardObjectReference target,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            out TeamId teamId)
        {
            teamId = default;
            switch (target.Kind)
            {
                case BoardObjectKind.Hero:
                    if (participants == null ||
                        !participants.Heroes.Any(hero => hero.Id.Equals(target.HeroId)))
                    {
                        return false;
                    }

                    teamId = participants.GetTeamForHero(target.HeroId).Id;
                    return teamId.IsDefined;

                case BoardObjectKind.Minion:
                    if (board == null)
                    {
                        return false;
                    }

                    foreach (var minion in board.Minions)
                    {
                        if (minion.MinionId.Equals(target.MinionId))
                        {
                            teamId = minion.TeamId;
                            return teamId.IsDefined;
                        }
                    }

                    return false;

                default:
                    return false;
            }
        }
    }
}
