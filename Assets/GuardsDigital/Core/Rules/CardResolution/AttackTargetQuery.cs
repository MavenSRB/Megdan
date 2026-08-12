using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public static class AttackTargetQuery
    {
        public static IReadOnlyList<AttackTarget> FindLegalEnemyTargets(
            HeroId attackerId,
            AttackRangeKind attackRange,
            int rangeTotal,
            MatchParticipants participants,
            AdjacentMoveAuthority board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            var heroes = FindLegalEnemyHeroes(
                attackerId, attackRange, rangeTotal, participants, board.Occupancies)
                .Select(AttackTarget.Hero);
            var attackerOccupancy = board.Occupancies.SingleOrDefault(value => value.HeroId.Equals(attackerId));
            if (!attackerOccupancy.HeroId.IsDefined)
            {
                return Array.AsReadOnly(heroes.ToArray());
            }

            var attackerTeam = participants.GetTeamForHero(attackerId).Id;
            var maximumDistance = attackRange == AttackRangeKind.NonRanged ? 1 : rangeTotal;
            var minions = board.Minions
                .Where(minion =>
                    !minion.TeamId.Equals(attackerTeam) &&
                    HexDistanceQuery.Calculate(attackerOccupancy.Coordinate, minion.Coordinate) <= maximumDistance &&
                    !IsImmuneHeavy(minion, board))
                .Select(minion => AttackTarget.Minion(minion.MinionId));

            return Array.AsReadOnly(heroes.Concat(minions)
                .OrderBy(target => target.Kind)
                .ThenBy(target => target.Kind == AttackTargetKind.Hero
                    ? target.HeroId.Value
                    : target.MinionId.Value, StringComparer.Ordinal)
                .ToArray());
        }

        public static IReadOnlyList<HeroId> FindLegalEnemyHeroes(
            HeroId attackerId,
            AttackRangeKind attackRange,
            int rangeTotal,
            MatchParticipants participants,
            IEnumerable<HeroOccupancy> occupancies)
        {
            if (!attackerId.IsDefined)
            {
                throw new ArgumentException("Attack target query requires a defined attacker ID.", nameof(attackerId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (occupancies == null)
            {
                throw new ArgumentNullException(nameof(occupancies));
            }

            if (attackRange != AttackRangeKind.NonRanged && attackRange != AttackRangeKind.Ranged)
            {
                throw new ArgumentException("Attack target query requires a supported range classification.", nameof(attackRange));
            }

            if (rangeTotal < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rangeTotal), rangeTotal, "Attack Range Total must be positive.");
            }

            var occupancyArray = occupancies.ToArray();
            var attackerOccupancies = occupancyArray
                .Where(occupancy => occupancy.HeroId.Equals(attackerId))
                .ToArray();
            if (attackerOccupancies.Length != 1)
            {
                return Array.Empty<HeroId>();
            }

            var participantHeroIds = new HashSet<HeroId>(participants.Heroes.Select(hero => hero.Id));
            var attackerTeamId = participants.GetTeamForHero(attackerId).Id;
            var maximumDistance = attackRange == AttackRangeKind.NonRanged ? 1 : rangeTotal;
            var attackerCoordinate = attackerOccupancies[0].Coordinate;

            return Array.AsReadOnly(occupancyArray
                .Where(occupancy =>
                    !occupancy.HeroId.Equals(attackerId) &&
                    participantHeroIds.Contains(occupancy.HeroId) &&
                    HexDistanceQuery.Calculate(attackerCoordinate, occupancy.Coordinate) <= maximumDistance &&
                    !participants.GetTeamForHero(occupancy.HeroId).Id.Equals(attackerTeamId))
                .Select(occupancy => occupancy.HeroId)
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal)
                .ToArray());
        }

        private static bool IsImmuneHeavy(
            QuickGameMinionSetupState candidate,
            AdjacentMoveAuthority board)
        {
            if (candidate.MinionType != StandardMinionType.Heavy)
            {
                return false;
            }

            var candidateZone = board.MapDefinition.GetZoneForSpace(candidate.OccupiedSpace.SpaceId);
            return candidateZone.IsStartingBattleZone && board.Minions.Any(other =>
                other.TeamId.Equals(candidate.TeamId) &&
                other.MinionType != StandardMinionType.Heavy &&
                board.MapDefinition.GetZoneForSpace(other.OccupiedSpace.SpaceId).Id.Equals(candidateZone.Id));
        }
    }
}
