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
            : this(heroId, cardInstanceId, passedWithEmptyHand, false)
        {
        }

        /// <param name="isFacedown">
        /// Milestone 9.32a. The card in this slot was placed facedown by p.10's Swap a Card (Bushido:
        /// "if you swap a resolved or discarded gold card this way, the new card is facedown"). With a
        /// card it is the authority's record; with no card and no Pass it is a seat's view of a slot
        /// whose card is withheld from it (<see cref="IsWithheld"/>, p.4: facedown cards are hidden).
        /// </param>
        public TurnCardAssignment(
            HeroId heroId,
            CardInstanceId? cardInstanceId,
            bool passedWithEmptyHand,
            bool isFacedown)
            : this(heroId, cardInstanceId, passedWithEmptyHand, isFacedown, CardColor.None)
        {
        }

        /// <param name="knownColor">
        /// Milestone 9.32g. On a withheld slot only: the colour every seat knows the facedown card
        /// has, though not which card it is - a card Bushido turns facedown can only be a gold (the
        /// user's 2026-10-02 presentation ruling). <see cref="CardColor.None"/> everywhere else.
        /// </param>
        public TurnCardAssignment(
            HeroId heroId,
            CardInstanceId? cardInstanceId,
            bool passedWithEmptyHand,
            bool isFacedown,
            CardColor knownColor)
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
            var withheld = isFacedown && !hasCard;
            if ((hasCard ? 1 : 0) + (passedWithEmptyHand ? 1 : 0) + (withheld ? 1 : 0) != 1)
            {
                throw new ArgumentException(
                    "A turn-card assignment requires exactly one committed card, explicit empty-hand Pass, " +
                    "or withheld facedown card.",
                    nameof(cardInstanceId));
            }

            if (knownColor != CardColor.None && !withheld)
            {
                throw new ArgumentException(
                    "Only a withheld facedown slot carries a known colour; any other names its card.",
                    nameof(knownColor));
            }

            if (!Enum.IsDefined(typeof(CardColor), knownColor) || knownColor == CardColor.Purple)
            {
                throw new ArgumentException("A known colour is a printed hand colour.", nameof(knownColor));
            }

            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            PassedWithEmptyHand = passedWithEmptyHand;
            IsFacedown = isFacedown;
            KnownColor = knownColor;
        }

        /// <summary>
        /// Milestone 9.32a. A seat's view of a slot holding a facedown card it may not read: the
        /// identity is absent, the fact that a card is there is not.
        /// </summary>
        public static TurnCardAssignment Withheld(HeroId heroId, CardColor knownColor = CardColor.None) =>
            new TurnCardAssignment(heroId, null, false, true, knownColor);

        public HeroId HeroId { get; }

        public CardInstanceId? CardInstanceId { get; }

        public bool PassedWithEmptyHand { get; }

        /// <summary>Milestone 9.32a. The slot's card was placed facedown (p.10); see the constructor.</summary>
        public bool IsFacedown { get; }

        /// <summary>Milestone 9.32a. A facedown card is here and this view may not name it.</summary>
        public bool IsWithheld => IsFacedown && !CardInstanceId.HasValue;

        /// <summary>
        /// Milestone 9.32g. The colour a withheld facedown card is known to have, or
        /// <see cref="CardColor.None"/>. Presentation only: p.10 gives a facedown card no colour for
        /// any rule, and nothing in the rules reads this.
        /// </summary>
        public CardColor KnownColor { get; }

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
        public IReadOnlyList<HeroId> OptionalHeroIds { get; }

        public RoundTurnSlot(
            int roundIndex,
            int turnIndex,
            IEnumerable<TurnCardAssignment> assignments,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            IEnumerable<HeroId> optionalHeroIds = null)
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

            OptionalHeroIds = Array.AsReadOnly((optionalHeroIds ?? Enumerable.Empty<HeroId>()).Distinct().ToArray());
            foreach (var hero in OptionalHeroIds) participants.GetHero(hero);
            var expectedHeroIds = participants.Heroes
                .Where(hero => !OptionalHeroIds.Contains(hero.Id) || assignmentArray.Any(value => value.HeroId.Equals(hero.Id)))
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
                    "A turn slot requires exactly one assignment for every participating hero.",
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
