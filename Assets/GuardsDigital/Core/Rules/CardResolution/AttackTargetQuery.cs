using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public static class AttackTargetQuery
    {
        public static IReadOnlyList<HeroId> FindAdjacentEnemyHeroes(
            HeroId attackerId,
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
            var adjacentCoordinates = new HashSet<HexCoordinate>(
                attackerOccupancies[0].Coordinate.GetNeighbors());

            return Array.AsReadOnly(occupancyArray
                .Where(occupancy =>
                    !occupancy.HeroId.Equals(attackerId) &&
                    participantHeroIds.Contains(occupancy.HeroId) &&
                    adjacentCoordinates.Contains(occupancy.Coordinate) &&
                    !participants.GetTeamForHero(occupancy.HeroId).Id.Equals(attackerTeamId))
                .Select(occupancy => occupancy.HeroId)
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal)
                .ToArray());
        }
    }
}
