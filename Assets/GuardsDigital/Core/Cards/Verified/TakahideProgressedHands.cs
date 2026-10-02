using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.32h. Takahide's staged Tier III hand as Ready for War leaves him (TK-Q5): all three
    /// gold cards - Float like a Butterfly, Sting like a Bee and Strike like a Tiger - and one Tier III
    /// face per colour (Hold my Sake, Tactical Gambit, Loyal Retainer), six cards in all, with Bushido
    /// back in his deck and Ready for War in his deck for the staging to unlock.
    /// </summary>
    public static class TakahideProgressedHands
    {
        public static ConfiguredHand CreateTierThreeHand(HeroId owner, string prefix)
        {
            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Staged cards require an owner and seat prefix.");
            var inHand = new HashSet<CardDefinitionId>
            {
                TakahideCardContent.FloatLikeAButterfly,
                TakahideCardContent.StingLikeABee,
                TakahideCardContent.StrikeLikeATiger,
                TakahideCardContent.HoldMySake,
                TakahideCardContent.TacticalGambit,
                TakahideCardContent.LoyalRetainer
            };
            var cards = TakahideCardContent.DealtDefinitions.Select(definition =>
                inHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)
                    : CardInstance.CreateInDeck(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)).ToArray();
            return new ConfiguredHand(
                TakahideCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
