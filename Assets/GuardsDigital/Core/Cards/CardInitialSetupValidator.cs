using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    public static class CardInitialSetupValidator
    {
        public static void Validate(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var definitionsByReference = new Dictionary<CardDefinitionReference, CardDefinition>();
            var seenDefinitionIds = new HashSet<CardDefinitionId>();

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Card definitions must not contain null entries.", nameof(definitions));
                }

                if (!seenDefinitionIds.Add(definition.Id))
                {
                    throw new ArgumentException("Card definitions must have unique definition IDs.", nameof(definitions));
                }

                definitionsByReference.Add(definition.Reference, definition);
            }

            var seenInstanceIds = new HashSet<CardInstanceId>();
            foreach (var instance in instances)
            {
                if (instance == null)
                {
                    throw new ArgumentException("Card instances must not contain null entries.", nameof(instances));
                }

                if (!seenInstanceIds.Add(instance.Id))
                {
                    throw new ArgumentException("Card instances must have unique instance IDs.", nameof(instances));
                }

                if (!definitionsByReference.TryGetValue(instance.DefinitionReference, out var definition))
                {
                    throw new ArgumentException("Card instance references an unresolved definition ID or content revision.", nameof(instances));
                }

                // Milestone 9.30b. A spell card starts in one place and only a spell starts there: its
                // owner's spellbook, unplayed and facedown. Widened for exactly that location and that
                // kind of card, so a spell dealt to a hand and an action card put in the book are both
                // refused.
                if (definition.Progression.IsSpell)
                {
                    if (!instance.State.IsInitialInSpellbook)
                    {
                        throw new ArgumentException("A spell card starts in its owner's spellbook, unplayed and facedown.", nameof(instances));
                    }

                    continue;
                }

                // Milestone 7.5. p.6's setup puts a hero's cards in two places, not one: five in hand,
                // and "the remaining Tier II, Tier III, and Tier IV cards" in the deck. Both shapes are
                // exact - unplayed, unresolved, face-up - so the widening admits the second resting
                // place and nothing else; a card starting in the discard pile, the turn area, an item
                // slot or the Ultimate area is still refused.
                if (!instance.State.IsInitialInHand && !instance.State.IsInitialInDeck)
                {
                    throw new ArgumentException("Initial card instances must start in hand or in the deck with unplayed, unresolved, face-up state.", nameof(instances));
                }
            }

            // Milestone 7.1. The upgrade relationships a definition states are a claim about the set
            // around it, so they are checked once the set is known to be well formed rather than at
            // each definition's own construction.
            CardProgressionValidator.Validate(definitionsByReference.Values);
        }
    }
}
