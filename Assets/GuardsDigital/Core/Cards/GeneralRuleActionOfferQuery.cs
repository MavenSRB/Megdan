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
            // Materialized once: the restriction below reads the replaced card's colour out of the
            // same two sequences the replacement query then walks, and a lazy caller enumerated twice
            // could answer the two halves from different content.
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));

            if (ActionRestrictionQuery.IsRestrictedFrom(
                actorId,
                ActionKind.FastTravel,
                modifications,
                // Milestone 8.20. p.11's Fast Travel replaces an entire Movement action, and the card
                // whose Movement it replaces is the card it is performed from - which is why this
                // query has taken that instance since it was written. A colour-qualified restriction
                // therefore reads the replaced card's colour, exactly as it would if the Movement
                // itself had been taken.
                ActionOfferQuery.SourceCardColor(
                    sourceCardInstanceId, materializedDefinitions, materializedInstances)))
            {
                return null;
            }

            return GetAvailableReplacementOffer(
                replacedMovementActionDefinitionId,
                sourceCardInstanceId,
                actorId,
                materializedDefinitions,
                materializedInstances,
                ActionKind.Movement,
                FastTravelDefinition);
        }

        /// <summary>
        /// Milestone 8.7 supplies the parameter the 6.8f audit deferred to "the milestone that first
        /// makes Clear restrictable". It is not that a card now prints one - none does - but that the
        /// consumption regime no longer lets an activity be declarable and unread: p.13's Clear is an
        /// action a hero performs, so a restriction naming it removes the offer exactly as Fast
        /// Travel's does.
        /// </summary>
        public static ActionOffer? GetAvailableClearOffer(
            ActionDefinitionId replacedAttackActionDefinitionId,
            CardInstanceId sourceCardInstanceId,
            HeroId actorId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));

            // p.13's Clear replaces an entire Attack action, so it reads the replaced card's colour
            // for the same reason Fast Travel above does.
            if (ActionRestrictionQuery.IsRestrictedFrom(
                actorId,
                ActionKind.Clear,
                modifications,
                ActionOfferQuery.SourceCardColor(
                    sourceCardInstanceId, materializedDefinitions, materializedInstances)))
            {
                return null;
            }

            return GetAvailableReplacementOffer(
                replacedAttackActionDefinitionId,
                sourceCardInstanceId,
                actorId,
                materializedDefinitions,
                materializedInstances,
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
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
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

            // Milestone 8.7. p.12 makes Hold the thing a hero does instead of acting, which is why it
            // is the one offer whose removal could strand a turn - and exactly why a rule that
            // forbade it has to be obeyed rather than quietly dropped. No source declares one; the
            // consumer exists so that none can be declared and ignored.
            //
            // Milestone 8.20 reads the colour of the card being held, because p.12's Hold is taken on
            // a card that has already been played into the turn slot - the same card the offer below
            // is built from - so a colour-qualified restriction has one to judge.
            if (ActionRestrictionQuery.IsRestrictedFrom(
                actorId,
                ActionKind.Hold,
                modifications,
                ActionOfferQuery.SourceCardColor(
                    sourceCardInstanceId,
                    definitionsByReference.Values.ToArray(),
                    cards)))
            {
                return null;
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
