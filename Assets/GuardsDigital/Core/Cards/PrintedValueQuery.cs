using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    public static class PrintedValueQuery
    {
        public static ValueResult CalculatePrintedInitiative(
            CardInstance card,
            IEnumerable<CardDefinition> definitions)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            var definitionsByReference = ActionOfferQuery.BuildDefinitionLookup(definitions);
            if (!definitionsByReference.TryGetValue(card.DefinitionReference, out var definition))
            {
                throw new ArgumentException(
                    "Card instance references an unresolved definition ID or content revision.",
                    nameof(card));
            }

            return new ValueResult(
                PrintedValueChannel.Initiative,
                new[]
                {
                    new ValueContribution(
                        ValueContributionKind.PrintedIcon,
                        card.Id,
                        PrintedValueChannel.Initiative,
                        definition.Initiative.Value)
                });
        }

        public static ValueResult CalculatePrintedValue(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            PrintedValueChannel channel) =>
            CalculatePrintedValue(
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                definitions,
                channel);

        /// <summary>
        /// The same printed value, for a caller that holds the three identities an offer carries rather
        /// than the offer itself.
        ///
        /// Milestone 7.2b added it for the effect frame: a capability sees its resolution's source card,
        /// definition reference and action definition, and never an <see cref="ActionOffer"/>, which is
        /// authority-owned and does not survive into the frame. It is an overload rather than a second
        /// derivation, and the offer-shaped entry point above now delegates to it, so a printed icon is
        /// read in exactly one place however it is asked for.
        /// </summary>
        public static ValueResult CalculatePrintedValue(
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            PrintedValueChannel channel)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("Printed value query requires a channel.", nameof(channel));
            }

            var definitionsByReference = ActionOfferQuery.BuildDefinitionLookup(definitions);
            if (!definitionsByReference.TryGetValue(sourceDefinitionReference, out var definition))
            {
                throw new ArgumentException(
                    "Action offer references an unresolved definition ID or content revision.",
                    nameof(sourceDefinitionReference));
            }

            var actionDefinition = definition.ActionDefinitions
                .SingleOrDefault(action => action.Id.Equals(actionDefinitionId));
            if (actionDefinition == null)
            {
                throw new ArgumentException(
                    "Action offer references an unresolved action definition ID.",
                    nameof(actionDefinitionId));
            }

            var contributions = actionDefinition.BaseValues
                .Where(value => value.Channel == channel)
                .Select(value => new ValueContribution(
                    ValueContributionKind.PrintedIcon,
                    sourceCardInstanceId,
                    actionDefinitionId,
                    value.Channel,
                    value.Amount));

            return new ValueResult(channel, contributions);
        }
    }
}
