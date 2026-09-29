using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.8f. Mortimer's staged Tier III hand: both basics, Braaaaaaains...! (an up-to-three
    /// before-the-attack modal), Macabre Mayhem (the Zombie push) and Army of Darkness (the once-per-turn
    /// Replace), with Master of Puppets in his deck for the staging to unlock.
    /// </summary>
    public static class MortimerProgressedHands
    {
        public static ConfiguredHand CreateTierThreeHand(HeroId owner, string prefix)
        {
            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Staged cards require an owner and seat prefix.");
            var inHand = new HashSet<CardDefinitionId>
            {
                MortimerAttackFaces.KnifeOfTheLivingDead,
                MortimerCardContent.Awaken,
                MortimerAttackFaces.Braaaaaaains,
                MortimerBlueFaces.MacabreMayhem,
                MortimerGreenFaces.ArmyOfDarkness
            };
            var cards = MortimerCardContent.DealtDefinitions.Select(definition =>
                inHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)
                    : CardInstance.CreateInDeck(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)).ToArray();
            return new ConfiguredHand(MortimerCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
