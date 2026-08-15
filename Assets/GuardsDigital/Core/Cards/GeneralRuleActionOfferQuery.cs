using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public static class GeneralRuleActionOfferQuery
    {
        public static readonly ActionDefinition HoldDefinition = new ActionDefinition(
            new ActionDefinitionId("general-hold"),
            ActionKind.Hold,
            ActionDefinitionOrigin.GeneralRuleSecondary,
            Array.Empty<PrintedValueDefinition>(),
            new ActionDefinitionPolicy(
                CardTextApplicationPolicy.Suppressed,
                false,
                false));

        public static readonly ActionDefinition FastTravelDefinition = new ActionDefinition(
            new ActionDefinitionId("general-fast-travel"),
            ActionKind.FastTravel,
            ActionDefinitionOrigin.Replacement,
            Array.Empty<PrintedValueDefinition>(),
            new ActionDefinitionPolicy(
                CardTextApplicationPolicy.Suppressed,
                false,
                false));

        public static readonly ActionDefinition ClearDefinition = new ActionDefinition(
            new ActionDefinitionId("general-clear"),
            ActionKind.Clear,
            ActionDefinitionOrigin.Replacement,
            Array.Empty<PrintedValueDefinition>(),
            new ActionDefinitionPolicy(
                CardTextApplicationPolicy.Suppressed,
                false,
                false));

        /// <param name="modifications">
        /// The modifications in effect; <see cref="RuleModificationSet.Empty"/> is the named
        /// no-modification value (milestone 6.8f). A restriction naming Fast Travel
        /// removes the offer rather than rejecting the command later: an action the actor cannot perform
        /// is not an action they are offered.
        /// </param>
        public static ActionOffer? GetAvailableFastTravelOffer(
            ActionDefinitionId replacedMovementActionDefinitionId,
            CardInstanceId sourceCardInstanceId,
            HeroId actorId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            if (IsRestricted(actorId, ActionKind.FastTravel, modifications))
            {
                return null;
            }

            return GetAvailableReplacementOffer(
                replacedMovementActionDefinitionId,
                sourceCardInstanceId,
                actorId,
                definitions,
                instances,
                ActionKind.Movement,
                FastTravelDefinition);
        }

        /// <summary>
        /// Whether a restriction currently bars this actor from an action kind. Restrictions are consulted
        /// per named activity, so a restriction on one general-rule action leaves the others offered.
        /// </summary>
        private static bool IsRestricted(
            HeroId actorId,
            ActionKind actionKind,
            RuleModificationSet modifications) =>
            modifications != null && actorId.IsDefined && modifications.IsRestricted(
                RuleObject.ForHero(actorId), RuleActivityQuery.ForAction(actionKind));

        /// <summary>
        /// Takes no modification set by the 6.8f audit's explicit disposition: no implemented
        /// modification family can change a Clear offer today - restrictions reach activities through
        /// <see cref="RuleActivityQuery.ForAction"/>, and no declared restriction names Clear. The
        /// milestone that first makes Clear restrictable adds the parameter with its consumer.
        /// </summary>
        public static ActionOffer? GetAvailableClearOffer(
            ActionDefinitionId replacedAttackActionDefinitionId,
            CardInstanceId sourceCardInstanceId,
            HeroId actorId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            return GetAvailableReplacementOffer(
                replacedAttackActionDefinitionId,
                sourceCardInstanceId,
                actorId,
                definitions,
                instances,
                ActionKind.Attack,
                ClearDefinition);
        }

        private static ActionOffer? GetAvailableReplacementOffer(
            ActionDefinitionId replacedActionDefinitionId,
            CardInstanceId sourceCardInstanceId,
            HeroId actorId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            ActionKind replacedKind,
            ActionDefinition replacementDefinition)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var turnOffers = ActionOfferQuery.GetAvailableTurnActionOffers(
                materializedDefinitions,
                materializedInstances,
                actorId,
                sourceCardInstanceId);
            var matchingOffers = turnOffers
                .Where(offer => offer.ActionDefinitionId.Equals(replacedActionDefinitionId))
                .ToArray();
            if (matchingOffers.Length != 1)
            {
                return null;
            }

            var replacedOffer = matchingOffers[0];
            var sourceDefinition = materializedDefinitions.Single(definition =>
                definition.Reference.Equals(replacedOffer.SourceDefinitionReference));
            var replacedAction = sourceDefinition.ActionDefinitions.Single(action =>
                action.Id.Equals(replacedActionDefinitionId));
            if (replacedAction.Kind != replacedKind || !replacedAction.DefaultPolicy.CanBeReplaced)
            {
                return null;
            }

            return new ActionOffer(
                replacedOffer.SourceCardInstanceId,
                replacedOffer.SourceDefinitionReference,
                replacementDefinition.Id,
                replacedOffer.ActorId,
                ActionInvocationContext.Replacement,
                true);
        }

        public static ActionOffer? GetAvailableHoldOffer(
            CardInstanceId sourceCardInstanceId,
            HeroId actorId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException("Hold offer query requires a source card ID.", nameof(sourceCardInstanceId));
            }

            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Hold offer query requires an actor ID.", nameof(actorId));
            }

            var definitionsByReference = ActionOfferQuery.BuildDefinitionLookup(definitions);
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var cards = instances.ToArray();
            if (cards.Any(card => card == null) ||
                cards.Select(card => card.Id).Distinct().Count() != cards.Length)
            {
                throw new ArgumentException(
                    "Hold offer card instances must be non-null and have unique IDs.",
                    nameof(instances));
            }

            var sourceCard = cards.SingleOrDefault(card => card.Id.Equals(sourceCardInstanceId));
            if (sourceCard == null ||
                !sourceCard.OwnerId.Equals(actorId) ||
                sourceCard.State.Location != CardLocation.TurnArea ||
                !sourceCard.State.HasBeenPlayed ||
                sourceCard.State.ResolutionStatus != CardResolutionStatus.Unresolved ||
                sourceCard.State.FaceOrientation != CardFaceOrientation.FaceUp ||
                !definitionsByReference.ContainsKey(sourceCard.DefinitionReference))
            {
                return null;
            }

            return new ActionOffer(
                sourceCard.Id,
                sourceCard.DefinitionReference,
                HoldDefinition.Id,
                actorId,
                ActionInvocationContext.GeneralRuleSecondary,
                true);
        }
    }
}
