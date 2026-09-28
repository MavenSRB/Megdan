using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.112g. Mrak's staged Tier III hand: both basics, Epicentre and Rock Solid - so one hand
    /// shows both of his new rail layouts, three badges and Defense-and-Attack - and Ground Shaker's
    /// different-target repeat, with Rock and a Hard Place in his deck for the staging to unlock.
    /// </summary>
    public static class MrakProgressedHands
    {
        public static ConfiguredHand CreateTierThreeHand(HeroId owner, string prefix)
        {
            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Staged cards require an owner and seat prefix.");
            var inHand = new HashSet<CardDefinitionId>
            {
                MrakCardContent.Fissure,
                MrakCardContent.StoneGrip,
                MrakRedFaces.Epicentre,
                MrakBlueFaces.GroundShaker,
                MrakRedFaces.RockSolid
            };
            var cards = MrakCardContent.DealtDefinitions.Select(definition =>
                inHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)
                    : CardInstance.CreateInDeck(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)).ToArray();
            return new ConfiguredHand(MrakCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
