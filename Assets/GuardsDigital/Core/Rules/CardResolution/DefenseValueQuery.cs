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
    public static class DefenseValueQuery
    {
        public static ValueResult Calculate(
            ActionOffer defenseOffer,
            IEnumerable<CardDefinition> definitions,
            HeroId defenderId,
            MatchParticipants participants,
            AdjacentMoveAuthority board)
        {
            if (!defenderId.IsDefined)
            {
                throw new ArgumentException("Defense value query requires a defined defender ID.", nameof(defenderId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var printed = PrintedValueQuery.CalculatePrintedValue(
                defenseOffer,
                definitions,
                PrintedValueChannel.Defense);
            var contributions = new List<ValueContribution>(printed.Contributions);
            contributions.AddRange(CalculateMinionContributions(defenderId, participants, board));

            return new ValueResult(PrintedValueChannel.Defense, contributions);
        }

        public static IReadOnlyList<ValueContribution> CalculateMinionContributions(
            HeroId defenderId,
            MatchParticipants participants,
            AdjacentMoveAuthority board)
        {
            if (!defenderId.IsDefined)
            {
                throw new ArgumentException("Defense value query requires a defined defender ID.", nameof(defenderId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var defenderOccupancies = board.Occupancies
                .Where(occupancy => occupancy.HeroId.Equals(defenderId))
                .ToArray();
            if (defenderOccupancies.Length != 1)
            {
                throw new ArgumentException("Defense value query requires the defender to occupy exactly one space.", nameof(defenderId));
            }

            var defenderTeam = participants.GetTeamForHero(defenderId).Side;
            var defenderCoordinate = defenderOccupancies[0].Coordinate;
            var contributions = new List<ValueContribution>();
            foreach (var minion in board.Minions.OrderBy(value => value.MinionId.Value, StringComparer.Ordinal))
            {
                var distance = HexDistanceQuery.Calculate(defenderCoordinate, minion.Coordinate);
                var amount = GetMinionDefenseModifier(defenderTeam, minion.TeamSide, minion.MinionType, distance);
                if (amount != 0)
                {
                    contributions.Add(new ValueContribution(
                        ValueContributionKind.MinionDefenseModifier,
                        minion.MinionId,
                        PrintedValueChannel.Defense,
                        amount));
                }
            }

            return Array.AsReadOnly(contributions.ToArray());
        }

        private static int GetMinionDefenseModifier(
            TeamSide defenderTeam,
            TeamSide minionTeam,
            StandardMinionType minionType,
            int distance)
        {
            var isFriendly = defenderTeam == minionTeam;
            if (distance == 1 &&
                (minionType == StandardMinionType.Melee || minionType == StandardMinionType.Heavy))
            {
                return isFriendly ? 1 : -1;
            }

            if (!isFriendly && minionType == StandardMinionType.Ranged && distance <= 2)
            {
                return -1;
            }

            return 0;
        }
    }
}
