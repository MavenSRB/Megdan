using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The Radius Total: rulebook p.9's printed Radius icon with rulebook p.16's matching items
    /// composed onto it, and the only place that total is decided.
    ///
    /// p.9 states what the icon means - "the value on the Range/Radius icon determines the maximum
    /// Range/Radius of your action. If the card text mentions Range/Radius, it refers to this value" -
    /// so a card text that says "in Radius" is naming this number rather than carrying one of its own.
    /// That is the distinction milestone 7.2b exists to make representable: a fixed integer written
    /// into an effect's own parameters is card text, which p.16 never raises, while this is the printed
    /// icon, which it does.
    ///
    /// It is the fifth query to compose <see cref="ItemBonusQuery"/> and is shaped exactly like the
    /// four milestone 7.2a finished: the bonus is asked of the already-computed printed result, so
    /// "if an Action card has no matching icon, the item has no effect on that particular Action card"
    /// stays a question about what the card prints, and every contribution keeps its own provenance -
    /// the printed icon names its action, each item names the card instance that became it.
    /// </summary>
    public static class RadiusValueQuery
    {
        /// <param name="heroItems">
        /// The match's items, or <c>null</c> in a composition that holds none - the same named absence
        /// the Defense total expresses. p.16's Item Bonus is decided by <see cref="ItemBonusQuery"/>
        /// and not here.
        /// </param>
        /// <param name="modifications">
        /// Milestone 8.61h. The live set, so a source that changed p.16's own arithmetic reaches this
        /// total too - Min's "This round: Double your item bonuses", which the Radius icon is one of
        /// the six channels of. Required rather than optional by milestone 6.8f's rule: a caller that
        /// forgot it would compose a disk a source has already widened.
        /// <see cref="RuleModificationSet.Empty"/> is the named none.
        /// </param>
        public static ValueResult Calculate(
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            HeroId actorId,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            RuleModificationSet modifications)
            => Calculate(sourceCardInstanceId, sourceDefinitionReference,
                actionDefinitionId, definitions, actorId, heroItems, modifications,
                RuleValueApplicability.Always);

        public static ValueResult Calculate(
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            HeroId actorId,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            RuleModificationSet modifications,
            RuleValueApplicability performedAction)
        {
            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            var composed = Calculate(
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                definitions,
                actorId,
                heroItems,
                ItemBonusMultiplierQuery.BonusPerItem(actorId, modifications));
            var declared = modifications.ValueContributions(
                RuleObject.ForHero(actorId), PrintedValueChannel.Radius, performedAction);
            return declared.Count == 0 ? composed : new ValueResult(
                PrintedValueChannel.Radius, composed.Contributions.Concat(declared));
        }

        /// <summary>
        /// Milestone 9.30e. The same total with the acting card's own text raising it - Dimension
        /// Door's printed <c>0+</c>, "+1 [Radius] Radius for each other spell card removed from the
        /// spellbook". p.9's icon, p.16's items (R1) and the declared modifications are the overload
        /// above; the card's text is
        /// <see cref="RosterDynamicValueQuery.CardStateContributions"/>, asked of the cards as they
        /// stand, so a Place's offer and its resolution read one Radius.
        /// </summary>
        public static ValueResult Calculate(
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            HeroId actorId,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            RuleModificationSet modifications,
            IEnumerable<CardInstance> instances)
        {
            var materialized = (definitions ?? Enumerable.Empty<CardDefinition>()).ToArray();
            var composed = Calculate(
                sourceCardInstanceId, sourceDefinitionReference, actionDefinitionId, materialized,
                actorId, heroItems, modifications);
            var action = materialized
                .FirstOrDefault(definition => definition != null &&
                    definition.Reference.Equals(sourceDefinitionReference))?
                .ActionDefinitions
                .FirstOrDefault(candidate => candidate.Id.Equals(actionDefinitionId));
            var text = RosterDynamicValueQuery.CardStateContributions(
                action, sourceCardInstanceId, PrintedValueChannel.Radius, instances);
            return text.Count == 0
                ? composed
                : new ValueResult(PrintedValueChannel.Radius, composed.Contributions.Concat(text));
        }

        /// <summary>
        /// The same total for a caller that holds no rules set at all - <c>MinionDefeatBountyQuery</c>
        /// measures a standing promise's disk from a lifecycle and a ledger, and states p.16's
        /// per-item amount by name rather than by fabricating an empty set that would silently mean
        /// "unmodified". Milestone 8.61h.
        /// </summary>
        public static ValueResult Calculate(
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            HeroId actorId,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            int bonusPerItem)
        {
            if (!actorId.IsDefined)
            {
                throw new ArgumentException(
                    "A Radius total requires the hero whose action it is.", nameof(actorId));
            }

            var printed = PrintedValueQuery.CalculatePrintedValue(
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                definitions,
                PrintedValueChannel.Radius);
            return heroItems == null
                ? printed
                : ItemBonusQuery.Apply(printed, heroItems, actorId, bonusPerItem);
        }

        /// <summary>The same total for a caller holding the offer, as the other four totals take it.</summary>
        public static ValueResult Calculate(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            RuleModificationSet modifications) =>
            Calculate(
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                definitions,
                offer.ActorId,
                heroItems,
                modifications,
                ActionValueApplicabilityQuery.ForOffer(offer, definitions));
    }
}
