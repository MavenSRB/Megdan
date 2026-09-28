using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.114f. Wuk's staged Tier III hand: both basics, Angry Stampede (the two-bodied
    /// Movement primary), Assert Dominance (a Skill printing no value) and Treetop Ride, with March of
    /// Nature in his deck for the staging to unlock.
    /// </summary>
    public static class WukProgressedHands
    {
        public static ConfiguredHand CreateTierThreeHand(HeroId owner, string prefix)
        {
            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Staged cards require an owner and seat prefix.");
            var inHand = new HashSet<CardDefinitionId>
            {
                WukCardContent.TreeSlam,
                WukCardContent.MysticSaplings,
                WukRedFaces.AngryStampede,
                WukBlueFaces.AssertDominance,
                WukGreenFaces.TreetopRide
            };
            var cards = WukCardContent.DealtDefinitions.Select(definition =>
                inHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)
                    : CardInstance.CreateInDeck(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)).ToArray();
            return new ConfiguredHand(WukCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
