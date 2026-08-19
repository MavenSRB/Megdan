using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
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
        public static ValueResult Calculate(
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            HeroId actorId,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
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
                : ItemBonusQuery.Apply(printed, heroItems, actorId);
        }

        /// <summary>The same total for a caller holding the offer, as the other four totals take it.</summary>
        public static ValueResult Calculate(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems) =>
            Calculate(
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                definitions,
                offer.ActorId,
                heroItems);
    }
}
