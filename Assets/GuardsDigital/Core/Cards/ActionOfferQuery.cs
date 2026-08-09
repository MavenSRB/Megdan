using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public static class ActionOfferQuery
    {
        public static IReadOnlyList<ActionOffer> GetAvailableOffers(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            HeroId actorId)
        {
            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Action offer query requires a defined actor ID.", nameof(actorId));
            }

            var definitionsByReference = BuildDefinitionLookup(definitions);

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var offers = new List<ActionOffer>();
            foreach (var instance in instances)
            {
                if (instance == null)
                {
                    throw new ArgumentException("Card instances must not contain null entries.", nameof(instances));
                }

                if (!instance.OwnerId.Equals(actorId) ||
                    instance.State.Location != CardLocation.Hand ||
                    instance.State.HasBeenPlayed)
                {
                    continue;
                }

                if (!definitionsByReference.TryGetValue(instance.DefinitionReference, out var definition))
                {
                    throw new ArgumentException("Card instance references an unresolved definition ID or content revision.", nameof(instances));
                }

                foreach (var actionDefinition in definition.ActionDefinitions)
                {
                    offers.Add(new ActionOffer(
                        instance.Id,
                        instance.DefinitionReference,
                        actionDefinition.Id,
                        actorId,
                        ToInvocationContext(actionDefinition.Origin),
                        true));
                }
            }

            return offers;
        }

        internal static Dictionary<CardDefinitionReference, CardDefinition> BuildDefinitionLookup(
            IEnumerable<CardDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
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

            return definitionsByReference;
        }

        private static ActionInvocationContext ToInvocationContext(ActionDefinitionOrigin origin)
        {
            return (ActionInvocationContext)(int)origin;
        }
    }
}
