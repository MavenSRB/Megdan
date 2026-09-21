using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    public static class PrintedValueQuery
    {
        /// <summary>
        /// One card's printed Initiative icon, as the turn order and the hero rail both read it.
        ///
        /// <para><b>Milestone 8.71a: the icon a sentence has REWRITTEN is what this returns.</b> Hanu
        /// the Trickster's Hurry up! prints "Set the PRINTED [Initiative] Initiative value of an
        /// unresolved card of a hero in range to 11", so the rewritten number is the printed number -
        /// which is why it comes back as the same single
        /// <see cref="ValueContributionKind.PrintedIcon"/> contribution the definition's own icon
        /// comes back as, with the same card named, rather than as a second contribution beside it.
        /// p.16's Initiative items and any hero-scoped modifier still compose ON TOP, because they
        /// compose on top of a printed icon and this is one (HA-Q1).</para>
        ///
        /// <para><b>It is read here rather than in the Initiative selection</b>, although that is
        /// where the order is decided, and the reason is that the order is not the only reader:
        /// <c>HeroOverviewPresentation</c> builds the rail's Initiative badge from this same call.
        /// A rewrite applied at the selection would have ordered the round by 11 while every seat's
        /// rail still drew 12, and the two readings could not be brought back together without
        /// presentation re-deriving the rule for itself.</para>
        /// </summary>
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
                        card.InitiativeOverride.IsDefined
                            ? card.InitiativeOverride.Value
                            : definition.Initiative.Value)
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
                if (definition.UltimateRule != null &&
                    definition.UltimateRule.EffectSourceId.Equals(actionDefinitionId))
                {
                    return new ValueResult(
                        channel,
                        definition.UltimateRule.PrintedValues
                            .Where(value => value.Channel == channel)
                            .Select(value => new ValueContribution(
                                ValueContributionKind.PrintedIcon,
                                sourceCardInstanceId,
                                actionDefinitionId,
                                value.Channel,
                                value.Amount)));
                }

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
