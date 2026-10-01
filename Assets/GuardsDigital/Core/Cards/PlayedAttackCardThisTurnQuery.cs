using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Whether the named hero played a card whose printed Primary action is Attack in the named turn.
    /// </summary>
    /// <remarks>
    /// This is a played-card query, not a performed-action history. Rulebook p.9 makes the Primary icon
    /// the card's type and p.10 makes the commitment a play, so Brogan's and Sabina's condition remains
    /// true when the hero Held, used a Secondary action, was defeated before acting, or has not acted
    /// yet. The immutable <see cref="RoundTurnSlot"/> already records exactly that committed identity;
    /// no second persisted fact is needed.
    /// </remarks>
    public static class PlayedAttackCardThisTurnQuery
    {
        public static bool HasPlayed(
            HeroId heroId,
            int roundIndex,
            int turnIndex,
            IEnumerable<RoundTurnSlot> turnSlots,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A played-card query requires a defined hero.", nameof(heroId));
            }

            if (roundIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundIndex));
            }

            if (turnIndex < 1 || turnIndex > RoundResolutionAuthority.TurnsPerRound)
            {
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            }

            var matchingSlots = (turnSlots ?? throw new ArgumentNullException(nameof(turnSlots)))
                .Where(slot => slot != null &&
                    slot.RoundIndex == roundIndex && slot.TurnIndex == turnIndex)
                .ToArray();
            if (matchingSlots.Length == 0)
            {
                return false;
            }

            if (matchingSlots.Length != 1)
            {
                throw new InvalidOperationException(
                    "Played-card history contains more than one slot for the same round and turn.");
            }

            var assignment = matchingSlots[0].Assignments.SingleOrDefault(value =>
                value.HeroId.Equals(heroId));
            if (assignment == null)
            {
                throw new InvalidOperationException(
                    $"Turn slot {roundIndex}:{turnIndex} has no assignment for hero '{heroId}'.");
            }

            if (!assignment.CardInstanceId.HasValue)
            {
                return false;
            }

            var card = (instances ?? throw new ArgumentNullException(nameof(instances)))
                .SingleOrDefault(candidate => candidate != null &&
                    candidate.Id.Equals(assignment.CardInstanceId.Value));
            if (card == null || !card.OwnerId.Equals(heroId))
            {
                throw new InvalidOperationException(
                    "A turn-slot assignment does not resolve to its hero-owned card instance.");
            }

            // Milestone 9.32a. p.10: a facedown resolved or discarded card "loses its type, color and
            // actions" - Bushido can leave one in a turn slot - so it is no Attack card (lesson 29).
            if (CardLifecycleAuthority.IsFacedownRestingCard(card))
            {
                return false;
            }

            var definition = (definitions ?? throw new ArgumentNullException(nameof(definitions)))
                .SingleOrDefault(candidate => candidate != null &&
                    candidate.Reference.Equals(card.DefinitionReference));
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "A played card does not resolve to a current card definition.");
            }

            var primary = definition.ActionDefinitions.SingleOrDefault(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            return primary != null && primary.Kind == ActionKind.Attack;
        }
    }
}
