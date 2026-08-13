using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards
{
    public sealed class TurnCardAssignment
    {
        public TurnCardAssignment(
            HeroId heroId,
            CardInstanceId? cardInstanceId,
            bool passedWithEmptyHand)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A turn-card assignment requires a canonical hero ID.", nameof(heroId));
            }

            if (cardInstanceId.HasValue && !cardInstanceId.Value.IsDefined)
            {
                throw new ArgumentException(
                    "A turn-card assignment requires a defined card instance ID.",
                    nameof(cardInstanceId));
            }

            var hasCard = cardInstanceId.HasValue && cardInstanceId.Value.IsDefined;
            if (hasCard == passedWithEmptyHand)
            {
                throw new ArgumentException(
                    "A turn-card assignment requires exactly one committed card or explicit empty-hand Pass.",
                    nameof(cardInstanceId));
            }

            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            PassedWithEmptyHand = passedWithEmptyHand;
        }

        public HeroId HeroId { get; }

        public CardInstanceId? CardInstanceId { get; }

        public bool PassedWithEmptyHand { get; }

        public PlayerId GetControllingPlayerId(MatchParticipants participants)
        {
            return (participants ?? throw new ArgumentNullException(nameof(participants)))
                .GetPlayerForHero(HeroId).Id;
        }

        public TeamSide GetTeamSide(MatchParticipants participants)
        {
            return (participants ?? throw new ArgumentNullException(nameof(participants)))
                .GetTeamForHero(HeroId).Side;
        }
    }

    public sealed class RoundTurnSlot
    {
        public RoundTurnSlot(
            int roundIndex,
            int turnIndex,
            IEnumerable<TurnCardAssignment> assignments,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (roundIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundIndex));
            }

            if (turnIndex < 1 || turnIndex > RoundResolutionAuthority.TurnsPerRound)
            {
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            }

            if (assignments == null)
            {
                throw new ArgumentNullException(nameof(assignments));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var assignmentArray = assignments.ToArray();
            if (assignmentArray.Any(assignment => assignment == null))
            {
                throw new ArgumentException("Turn-slot assignments must not contain null entries.", nameof(assignments));
            }

            var expectedHeroIds = participants.Heroes
                .Select(hero => hero.Id)
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal)
                .ToArray();
            if (assignmentArray.Length != expectedHeroIds.Length ||
                assignmentArray.Select(assignment => assignment.HeroId).Distinct().Count() != assignmentArray.Length ||
                !assignmentArray.Select(assignment => assignment.HeroId).OrderBy(
                        heroId => heroId.Value,
                        StringComparer.Ordinal)
                    .SequenceEqual(expectedHeroIds))
            {
                throw new ArgumentException(
                    "A turn slot requires exactly one assignment for every configured hero.",
                    nameof(assignments));
            }

            var cardArray = instances.ToArray();
            if (cardArray.Any(card => card == null) ||
                cardArray.Select(card => card.Id).Distinct().Count() != cardArray.Length)
            {
                throw new ArgumentException(
                    "Turn-slot card instances must be non-null and have unique IDs.",
                    nameof(instances));
            }

            var assignedCardIds = assignmentArray
                .Where(assignment => assignment.CardInstanceId.HasValue)
                .Select(assignment => assignment.CardInstanceId.Value)
                .ToArray();
            if (assignedCardIds.Distinct().Count() != assignedCardIds.Length)
            {
                throw new ArgumentException(
                    "A committed card cannot be assigned to more than one hero.",
                    nameof(assignments));
            }

            foreach (var assignment in assignmentArray)
            {
                participants.GetPlayerForHero(assignment.HeroId);
                participants.GetTeamForHero(assignment.HeroId);
                if (!assignment.CardInstanceId.HasValue)
                {
                    continue;
                }

                var card = cardArray.SingleOrDefault(candidate =>
                    candidate.Id.Equals(assignment.CardInstanceId.Value));
                if (card == null || !card.OwnerId.Equals(assignment.HeroId))
                {
                    throw new ArgumentException(
                        "Every committed turn-slot card must exist and be owned by its assigned hero.",
                        nameof(assignments));
                }
            }

            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Assignments = Array.AsReadOnly(assignmentArray
                .OrderBy(assignment => assignment.HeroId.Value, StringComparer.Ordinal)
                .ToArray());
        }

        public int RoundIndex { get; }

        public int TurnIndex { get; }

        public IReadOnlyList<TurnCardAssignment> Assignments { get; }
    }
}
