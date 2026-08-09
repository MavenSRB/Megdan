using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    public static class PrintedValueQuery
    {
        public static PrintedValueResult CalculatePrintedValue(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            PrintedValueChannel channel)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("Printed value query requires a channel.", nameof(channel));
            }

            var definitionsByReference = ActionOfferQuery.BuildDefinitionLookup(definitions);
            if (!definitionsByReference.TryGetValue(offer.SourceDefinitionReference, out var definition))
            {
                throw new ArgumentException("Action offer references an unresolved definition ID or content revision.", nameof(offer));
            }

            var actionDefinition = definition.ActionDefinitions.SingleOrDefault(action => action.Id.Equals(offer.ActionDefinitionId));
            if (actionDefinition == null)
            {
                throw new ArgumentException("Action offer references an unresolved action definition ID.", nameof(offer));
            }

            var contributions = actionDefinition.BaseValues
                .Where(value => value.Channel == channel)
                .Select(value => new PrintedValueContribution(
                    PrintedValueContributionKind.PrintedIcon,
                    offer.SourceCardInstanceId,
                    offer.ActionDefinitionId,
                    value.Channel,
                    value.Amount));

            return new PrintedValueResult(channel, contributions);
        }
    }
}
