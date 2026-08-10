using System;
using System.Collections.Generic;
using System.Linq;
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
