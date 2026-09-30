using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.21j. Razzle's staged Tier III hand: both basics (Stunt Doubles, whose basic Attack
    /// Twin Strike repeats from a different body, and Crowd Control), Ransack (a retrieve for every other
    /// body in radius), Aaaand it's gone! (the push and the retreat of this body) and Spectacle (the
    /// repeat by a different body), with Twin Strike in his deck for the staging to unlock.
    /// </summary>
    public static class RazzleProgressedHands
    {
        public static ConfiguredHand CreateTierThreeHand(HeroId owner, string prefix)
        {
            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Staged cards require an owner and seat prefix.");
            var inHand = new HashSet<CardDefinitionId>
            {
                RazzleCardContent.StuntDoubles,
                RazzleCardContent.CrowdControl,
                RazzleCardContent.Ransack,
                RazzleCardContent.AaaandItsGone,
                RazzleCardContent.Spectacle
            };
            var cards = RazzleCardContent.DealtDefinitions.Select(definition =>
                inHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)
                    : CardInstance.CreateInDeck(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference, owner)).ToArray();
            return new ConfiguredHand(RazzleCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
