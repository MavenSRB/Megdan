using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The attacker's Attack value before p.14's card text speaks: rulebook p.9's printed Attack icon,
    /// p.16's matching items, and milestone 8.19c's source-declared contributions.
    ///
    /// It is promoted here for the reason milestone 8.6 promoted <see cref="RangeValueQuery"/> and 7.2b
    /// promoted <see cref="RadiusValueQuery"/> before it: a second reader appeared. Sabina's Big Sodding
    /// Gun declares "+2 Attack" from an Ultimate rather than from the card being played, so the total is
    /// no longer composed only where an attack is resolved, and two private compositions of one page's
    /// rule are how an attack's own total and a source's reading of it come to disagree.
    ///
    /// What stays in <c>AttackDefenseCardActionStrategyBackend</c> is p.14's card-text contribution,
    /// which needs the bound target and the turn's own history. That is a fact about the attack in
    /// flight rather than about the card, and the backend composes it onto this total.
    /// </summary>
    public static class AttackValueQuery
    {
        /// <param name="heroItems">
        /// The match's items, or <c>null</c> in a composition holding none - the same named absence the
        /// Range, Radius and Defense totals express.
        /// </param>
        /// <param name="modifications">
        /// The live set. Required rather than optional by milestone 6.8f's rule: a caller that forgot
        /// it would compose an Attack a source has already changed.
        /// </param>
        public static ValueResult Calculate(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            RuleModificationSet modifications)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (!offer.ActorId.IsDefined)
            {
                throw new ArgumentException(
                    "An Attack total requires the hero whose action it is.", nameof(offer));
            }

            var materialized = definitions as IReadOnlyList<CardDefinition> ?? definitions.ToArray();
            var printed = PrintedValueQuery.CalculatePrintedValue(
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                materialized,
                PrintedValueChannel.Attack);
            var composed = heroItems == null
                ? printed
                : ItemBonusQuery.Apply(printed, heroItems, offer.ActorId, modifications);
            var declared = modifications.ValueContributions(
                RuleObject.ForHero(offer.ActorId),
                PrintedValueChannel.Attack,
                ActionValueApplicabilityQuery.ForOffer(offer, materialized));
            return declared.Count == 0
                ? composed
                : new ValueResult(
                    PrintedValueChannel.Attack, composed.Contributions.Concat(declared));
        }
    }
}
