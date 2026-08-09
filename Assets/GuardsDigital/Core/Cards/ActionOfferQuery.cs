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
            return GetAvailableOffers(definitions, instances, actorId, ActionOfferWindow.NormalPlay);
        }

        public static IReadOnlyList<ActionOffer> GetAvailableOffers(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            HeroId actorId,
            ActionOfferWindow window)
        {
            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Action offer query requires a defined actor ID.", nameof(actorId));
            }

            if (!Enum.IsDefined(typeof(ActionOfferWindow), window) || window == ActionOfferWindow.None)
            {
                throw new ArgumentException("Action offer query requires a defined offer window.", nameof(window));
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
                    if (!IsAvailableInWindow(actionDefinition.Origin, window))
                    {
                        continue;
                    }

                    if (actionDefinition.SourceCardStateTransition.IsDefined &&
                        instance.State.Location != actionDefinition.SourceCardStateTransition.RequiredLocation)
                    {
                        continue;
                    }

                    offers.Add(new ActionOffer(
                        instance.Id,
                        instance.DefinitionReference,
                        actionDefinition.Id,
                        actorId,
                        ToInvocationContext(actionDefinition.Origin),
                        true,
                        actionDefinition.AttackRange,
                        actionDefinition.SourceCardStateTransition));
                }
            }

            return Array.AsReadOnly(offers.ToArray());
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
            switch (origin)
            {
                case ActionDefinitionOrigin.PrintedPrimary:
                    return ActionInvocationContext.PrintedPrimary;
                case ActionDefinitionOrigin.PrintedSecondary:
                    return ActionInvocationContext.PrintedSecondary;
                case ActionDefinitionOrigin.GeneralRuleSecondary:
                    return ActionInvocationContext.GeneralRuleSecondary;
                case ActionDefinitionOrigin.Replacement:
                    return ActionInvocationContext.Replacement;
                case ActionDefinitionOrigin.Reaction:
                    return ActionInvocationContext.Reaction;
                case ActionDefinitionOrigin.TextGranted:
                    return ActionInvocationContext.TextGranted;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unknown action definition origin.");
            }
        }

        private static bool IsAvailableInWindow(ActionDefinitionOrigin origin, ActionOfferWindow window)
        {
            switch (window)
            {
                case ActionOfferWindow.NormalPlay:
                    return origin == ActionDefinitionOrigin.PrintedPrimary ||
                        origin == ActionDefinitionOrigin.PrintedSecondary;
                case ActionOfferWindow.GeneralRule:
                    return origin == ActionDefinitionOrigin.GeneralRuleSecondary;
                case ActionOfferWindow.Replacement:
                    return origin == ActionDefinitionOrigin.Replacement;
                case ActionOfferWindow.Reaction:
                    return origin == ActionDefinitionOrigin.Reaction;
                case ActionOfferWindow.TextGranted:
                    return origin == ActionDefinitionOrigin.TextGranted;
                default:
                    throw new ArgumentOutOfRangeException(nameof(window), window, "Unknown action offer window.");
            }
        }
    }
}
