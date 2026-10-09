using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.30i. Gydion's staged Tier III hand: both basics (Cantrip, and Prepare Spells, whose
    /// refill The Archwizard turns into "Cast Wish instead"), Greater Evocation (Sunburst), Greater
    /// Transmutation (Polymorph) and Greater Conjuration (Find Familiar and Cloud Kill), with The
    /// Archwizard in his deck for the staging to unlock and all twenty-two spells - Wish included - in
    /// his book.
    /// </summary>
    public static class GydionProgressedHands
    {
        public static ConfiguredHand CreateTierThreeHand(HeroId owner, string prefix)
        {
            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Staged cards require an owner and seat prefix.");
            var inHand = new HashSet<CardDefinitionId>
            {
                GydionCardContent.Cantrip,
                GydionCardContent.PrepareSpells,
                GydionCardContent.GreaterEvocation,
                GydionCardContent.GreaterTransmutation,
                GydionCardContent.GreaterConjuration
            };
            var cards = GydionCardContent.DealtDefinitions.Select(definition =>
                inHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)
                    : CardInstance.CreateInDeck(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)).ToArray();
            return new ConfiguredHand(
                GydionCardContent.DealtDefinitions.Concat(GydionCardContent.SpellDefinitions),
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck),
                GydionCardContent.SpellDefinitions.Select(spell => CardInstance.CreateInSpellbook(
                    new CardInstanceId(prefix + "-" + spell.Id.Value), spell.Reference, owner)));
        }
    }
}
