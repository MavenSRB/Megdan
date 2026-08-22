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
                    if (!IsAvailableInWindow(actionDefinition, window))
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
                        ToInvocationContext(actionDefinition),
                        true,
                        actionDefinition.AttackRange,
                        actionDefinition.SourceCardStateTransition));
                }
            }

            return Array.AsReadOnly(offers.ToArray());
        }

        public static IReadOnlyList<ActionOffer> GetAvailableTurnActionOffers(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            HeroId actorId,
            CardInstanceId sourceCardInstanceId)
        {
            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Turn action offer query requires a defined actor ID.", nameof(actorId));
            }

            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "Turn action offer query requires a defined source card instance ID.",
                    nameof(sourceCardInstanceId));
            }

            var definitionsByReference = BuildDefinitionLookup(definitions);
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var materializedInstances = instances.ToArray();
            if (materializedInstances.Any(instance => instance == null))
            {
                throw new ArgumentException("Card instances must not contain null entries.", nameof(instances));
            }

            var matchingCards = materializedInstances
                .Where(instance => instance.Id.Equals(sourceCardInstanceId))
                .ToArray();
            if (matchingCards.Length != 1)
            {
                return Array.Empty<ActionOffer>();
            }

            var card = matchingCards[0];
            if (!card.OwnerId.Equals(actorId) ||
                card.State.Location != CardLocation.TurnArea ||
                !card.State.HasBeenPlayed ||
                card.State.ResolutionStatus != CardResolutionStatus.Unresolved ||
                card.State.FaceOrientation != CardFaceOrientation.FaceUp)
            {
                return Array.Empty<ActionOffer>();
            }

            if (!definitionsByReference.TryGetValue(card.DefinitionReference, out var definition))
            {
                throw new ArgumentException(
                    "Turn card instance references an unresolved definition ID or content revision.",
                    nameof(instances));
            }

            var offers = definition.ActionDefinitions
                .Where(actionDefinition =>
                    IsAvailableInWindow(actionDefinition, ActionOfferWindow.NormalPlay) &&
                    (!actionDefinition.SourceCardStateTransition.IsDefined ||
                        card.State.Location == actionDefinition.SourceCardStateTransition.RequiredLocation))
                .Select(actionDefinition => new ActionOffer(
                    card.Id,
                    card.DefinitionReference,
                    actionDefinition.Id,
                    actorId,
                    ToInvocationContext(actionDefinition),
                    true,
                    actionDefinition.AttackRange,
                    actionDefinition.SourceCardStateTransition))
                .ToArray();
            return Array.AsReadOnly(offers);
        }

        /// <summary>
        /// Resolves the action definition an offer was built from. This lives beside the lookup it
        /// depends on because the resolution backends all need the same answer, and three private
        /// copies of it could drift apart without anything noticing.
        /// </summary>
        internal static ActionDefinition FindActionDefinition(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions)
        {
            var definitionsByReference = BuildDefinitionLookup(definitions);
            var definition = definitionsByReference[offer.SourceDefinitionReference];
            return definition.ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));
        }

        /// <summary>
        /// The same resolution for a caller that runs before the offer has been validated, and so must
        /// treat an offer naming nothing as a question it cannot answer rather than as a fault.
        ///
        /// Milestone 6.5af needs this because it judges an offer ahead of the start that would check
        /// it: a forged offer naming an action no definition carries has to reach the existing typed
        /// rejection, not fault inside a probe on the way there.
        /// </summary>
        internal static bool TryFindActionDefinition(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            out ActionDefinition action)
        {
            action = null;
            if (!BuildDefinitionLookup(definitions)
                .TryGetValue(offer.SourceDefinitionReference, out var definition))
            {
                return false;
            }

            action = definition.ActionDefinitions
                .SingleOrDefault(candidate => candidate.Id.Equals(offer.ActionDefinitionId));
            return action != null;
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

        /// <summary>
        /// How an offered action is being invoked.
        ///
        /// <para>Milestone 8.11 gave it the definition for the same reason
        /// <see cref="IsAvailableInWindow"/> has it: p.14's Block is printed as the card's primary and
        /// performed only in reaction, so where it is printed and how it is invoked are two different
        /// answers. Every other action's invocation follows straight from its origin.</para>
        /// </summary>
        private static ActionInvocationContext ToInvocationContext(ActionDefinition action)
        {
            if (action.DefenseResolution == PrintedDefenseResolution.Block)
            {
                return ActionInvocationContext.Reaction;
            }

            switch (action.Origin)
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
                    throw new ArgumentOutOfRangeException(nameof(action), action.Origin, "Unknown action definition origin.");
            }
        }

        /// <summary>
        /// Which window an action may be offered in.
        ///
        /// <para>Milestone 8.11 gave it the whole definition rather than the origin alone, for one
        /// card shape: p.14's Block is printed as the card's <em>primary</em> action - milestone 8.3's
        /// rule requires that, because that is where the face prints it - and is nevertheless
        /// performable only in reaction: "You cannot perform a Defense action on an already played
        /// card! It can only be performed in response to an attack targeting you, by discarding
        /// exactly one card from your hand." Origin says where on the face an action is printed; the
        /// window says when it may be taken, and for a Block those two differ. Every other Defense
        /// action this build has is printed as a secondary and already carries the Reaction origin.</para>
        /// </summary>
        private static bool IsAvailableInWindow(ActionDefinition action, ActionOfferWindow window)
        {
            var origin = action.Origin;
            var isPrintedBlock = action.DefenseResolution == PrintedDefenseResolution.Block;
            switch (window)
            {
                case ActionOfferWindow.NormalPlay:
                    return !isPrintedBlock &&
                        (origin == ActionDefinitionOrigin.PrintedPrimary ||
                        origin == ActionDefinitionOrigin.PrintedSecondary);
                case ActionOfferWindow.GeneralRule:
                    return origin == ActionDefinitionOrigin.GeneralRuleSecondary;
                case ActionOfferWindow.Replacement:
                    return origin == ActionDefinitionOrigin.Replacement;
                case ActionOfferWindow.Reaction:
                    return origin == ActionDefinitionOrigin.Reaction || isPrintedBlock;
                case ActionOfferWindow.TextGranted:
                    return origin == ActionDefinitionOrigin.TextGranted;
                default:
                    throw new ArgumentOutOfRangeException(nameof(window), window, "Unknown action offer window.");
            }
        }
    }
}
