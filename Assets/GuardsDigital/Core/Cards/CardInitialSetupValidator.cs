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

                if (!definitionsByReference.ContainsKey(instance.DefinitionReference))
                {
                    throw new ArgumentException("Card instance references an unresolved definition ID or content revision.", nameof(instances));
                }

                if (!instance.State.IsInitialInHand)
                {
                    throw new ArgumentException("Initial card instances must start in hand with unplayed, unresolved, face-up state.", nameof(instances));
                }
            }
        }
    }
}
