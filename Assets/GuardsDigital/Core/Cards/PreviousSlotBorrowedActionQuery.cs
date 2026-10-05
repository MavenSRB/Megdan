using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>One printed action, including a secondary ribbon, on a named previous-slot card.</summary>
    public readonly struct PreviousSlotBorrowedAction
    {
        public PreviousSlotBorrowedAction(CardInstanceId cardId, ActionDefinitionId actionId,
            ActionKind kind, int printedIndex)
        {
            CardId = cardId;
            ActionId = actionId;
            Kind = kind;
            PrintedIndex = printedIndex;
        }

        public CardInstanceId CardId { get; }
        public ActionDefinitionId ActionId { get; }
        public ActionKind Kind { get; }
        public int PrintedIndex { get; }
    }

    /// <summary>
    /// The previous-slot action pool for a clause that explicitly names an enemy hero's card.
    /// Unlike a borrowed-primary pool, this names the selected enemy's dashboard slot and offers
    /// each printed action on its face. A hidden or absent face offers none.
    /// </summary>
    public static class PreviousSlotBorrowedActionQuery
    {
        public static IReadOnlyList<PreviousSlotBorrowedAction> Eligible(
            HeroId actorId, HeroId enemyId, int roundIndex, int turnIndex,
            MatchParticipants participants, IEnumerable<RoundTurnSlot> slots,
            IEnumerable<CardDefinition> definitions, IEnumerable<CardInstance> instances)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            if (!actorId.IsDefined || !enemyId.IsDefined || roundIndex < 1 || turnIndex < 1)
                return Array.Empty<PreviousSlotBorrowedAction>();
            if (participants.GetTeamForHero(actorId).Side ==
                participants.GetTeamForHero(enemyId).Side || turnIndex == 1)
                return Array.Empty<PreviousSlotBorrowedAction>();

            var assignment = slots
                .Where(slot => slot != null && slot.RoundIndex == roundIndex &&
                    slot.TurnIndex == turnIndex - 1)
                .SelectMany(slot => slot.Assignments)
                .SingleOrDefault(value => value.HeroId.Equals(enemyId));
            if (assignment == null || assignment.IsFacedown || !assignment.CardInstanceId.HasValue)
                return Array.Empty<PreviousSlotBorrowedAction>();

            var card = instances.SingleOrDefault(value =>
                value.Id.Equals(assignment.CardInstanceId.Value));
            if (card == null || !card.OwnerId.Equals(enemyId) ||
                CardLifecycleAuthority.IsFacedownRestingCard(card))
                return Array.Empty<PreviousSlotBorrowedAction>();
            var definition = definitions.SingleOrDefault(value =>
                value.Reference.Equals(card.DefinitionReference));
            if (definition == null)
                return Array.Empty<PreviousSlotBorrowedAction>();

            return Array.AsReadOnly(definition.ActionDefinitions
                .Select((action, index) => new { action, index })
                .Where(candidate =>
                    !candidate.action.TextComposition.IsAwaited &&
                    (candidate.action.Origin == ActionDefinitionOrigin.PrintedPrimary ||
                     candidate.action.Origin == ActionDefinitionOrigin.PrintedSecondary ||
                     candidate.action.Origin == ActionDefinitionOrigin.Reaction))
                .Select(candidate => new PreviousSlotBorrowedAction(
                    card.Id, candidate.action.Id, candidate.action.Kind, candidate.index))
                .ToArray());
        }
    }
}
