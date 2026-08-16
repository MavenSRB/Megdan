using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The one mechanical answer to whether an action or effect may target or affect a unit.
    /// It combines source-declared immunity with standing rulebook immunity, so offer generation and
    /// final command resolution cannot disagree and no card has to reimplement a protected unit rule.
    /// </summary>
    public static class ImmunityQuery
    {
        public static bool IsImmuneTo(
            RuleObject target,
            RuleActivity activity,
            RuleObject actingUnit,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (!target.IsDefined)
            {
                return false;
            }

            if (activity == RuleActivity.None)
            {
                throw new ArgumentException("An immunity question requires an activity.", nameof(activity));
            }

            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));

            return modifications.IsImmuneTo(target, activity, actingUnit) ||
                HasStandingHeavyImmunity(target, board, participants, modifications);
        }

        private static bool HasStandingHeavyImmunity(
            RuleObject target,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (!IntrinsicKindQuery.IsMinionMiniature(target))
            {
                return false;
            }

            var heavy = board.Minions.SingleOrDefault(minion => minion.MinionId.Equals(target.MinionId));
            if (heavy == null || heavy.MinionType != StandardMinionType.Heavy ||
                !BattleZoneQuery.IsInStartingBattleZone(board.MapDefinition, heavy.OccupiedSpace.SpaceId))
            {
                return false;
            }

            var battleZone = BattleZoneQuery.GetStartingBattleZone(board.MapDefinition).Id;
            var units = board.Minions
                .Where(minion =>
                    board.MapDefinition.GetZoneForSpace(minion.OccupiedSpace.SpaceId).Id.Equals(battleZone))
                .Select(minion => RuleObject.ForMinion(minion.MinionId))
                .Concat(board.Occupancies
                    .Where(hero =>
                    {
                        var space = board.MapDefinition.Spaces.SingleOrDefault(candidate =>
                            candidate.Kind == BoardMapSpaceKind.Playable &&
                            candidate.Coordinate.Equals(hero.Coordinate));
                        return space != null &&
                            board.MapDefinition.GetZoneForSpace(space.Id).Id.Equals(battleZone);
                    })
                    .Select(hero => RuleObject.ForHero(hero.HeroId)))
                .Where(unit => EffectiveUnitKindQuery.Is(unit, EffectiveUnitKind.Minion, modifications));

            return units.Any(unit => Accompanies(unit, heavy, board, participants));
        }

        private static bool Accompanies(
            RuleObject unit,
            MinionOccupancy heavy,
            IBoardDisplacementAuthority board,
            MatchParticipants participants)
        {
            if (IntrinsicKindQuery.IsMinionMiniature(unit))
            {
                if (unit.MinionId.Equals(heavy.MinionId))
                {
                    return false;
                }

                var minion = board.Minions.Single(candidate => candidate.MinionId.Equals(unit.MinionId));
                return minion.TeamId.Equals(heavy.TeamId) &&
                    minion.MinionType != StandardMinionType.Heavy;
            }

            return IntrinsicKindQuery.IntrinsicUnitKind(unit) == EffectiveUnitKind.Hero &&
                participants.GetTeamForHero(unit.HeroId).Id.Equals(heavy.TeamId);
        }
    }
}
