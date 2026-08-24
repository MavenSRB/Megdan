using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The Range Total: rulebook p.9's printed Range icon with rulebook p.16's matching items composed
    /// onto it, and the only place that total is decided - the Range twin of
    /// <see cref="RadiusValueQuery"/>, and identical to it in every respect but the channel.
    ///
    /// p.9 states both icons in one sentence - "the value on the Range/Radius icon determines the
    /// maximum Range/Radius of your action. If the card text mentions Range/Radius, it refers to this
    /// value" - so a card text that says "in range" is naming this number rather than carrying one of
    /// its own, exactly as "in radius" names the other.
    ///
    /// Milestone 8.6 promoted it out of <c>AttackDefenseCardActionStrategyBackend</c>, where milestone
    /// 7.2a had written the same composition as a private helper. Wasp's Telekinesis and Mass
    /// Telekinesis are Skills whose text reads the printed Range, so a second reader existed for the
    /// first time; two private compositions of one page's rule are how an attack's reach and a skill's
    /// reach come to disagree about the same icon, and this is the shape milestone 7.2b already chose
    /// for Radius when its second reader appeared.
    /// </summary>
    public static class RangeValueQuery
    {
        /// <param name="heroItems">
        /// The match's items, or <c>null</c> in a composition that holds none - the same named absence
        /// the Radius and Defense totals express. p.16's Item Bonus is decided by
        /// <see cref="ItemBonusQuery"/> and not here.
        /// </param>
        /// <param name="modifications">
        /// Milestone 8.19c. The live set, so a source-declared Range modifier reaches this total -
        /// Sabina's Big Sodding Gun, "Your basic attack has +2 Range and +2 Attack". Required rather
        /// than optional, by milestone 6.8f's rule: a caller that forgot it would compose a Range the
        /// source has already changed. <see cref="RuleModificationSet.Empty"/> is the named none.
        /// </param>
        /// <param name="performedAction">
        /// Which class of action this total is for, so a qualified declaration is admitted only by the
        /// action it names. <see cref="ActionValueApplicabilityQuery"/> answers it from the offer.
        /// </param>
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
            if (!actorId.IsDefined)
            {
                throw new ArgumentException(
                    "A Range total requires the hero whose action it is.", nameof(actorId));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            var printed = PrintedValueQuery.CalculatePrintedValue(
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                definitions,
                PrintedValueChannel.Range);
            var composed = heroItems == null
                ? printed
                : ItemBonusQuery.Apply(printed, heroItems, actorId);
            var declared = modifications.ValueContributions(
                RuleObject.ForHero(actorId), PrintedValueChannel.Range, performedAction);
            return declared.Count == 0
                ? composed
                : new ValueResult(
                    PrintedValueChannel.Range, composed.Contributions.Concat(declared));
        }

        /// <summary>The same total for a caller holding the offer, as the other totals take it.</summary>
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
