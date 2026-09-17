using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    public enum GarrusUpgradeBranch { FirstSibling = 0, SecondSibling = 1 }

    /// <summary>Internal staged hands, not a substitute for the real progression transaction.</summary>
    public static class GarrusProgressedHands
    {
        public static ConfiguredHand CreateTierTwoHand(HeroId owner, string prefix, GarrusUpgradeBranch branch) =>
            Create(owner, prefix, branch, new[] { GarrusCardContent.Chase, GarrusCardContent.Threaten, GarrusCardContent.MakeAStand },
                new[] { GarrusCardContent.BluntForce, GarrusCardContent.FormUp, GarrusCardContent.LightPilum });
        public static ConfiguredHand CreateTierThreeHand(HeroId owner, string prefix, GarrusUpgradeBranch branch) =>
            Create(owner, prefix, branch, new[] { GarrusCardContent.HuntDown, GarrusCardContent.Terrify, GarrusCardContent.BattleReady },
                new[] { GarrusCardContent.SendFlying, GarrusCardContent.Testudo, GarrusCardContent.HeavyPilum });

        private static ConfiguredHand Create(HeroId owner, string prefix, GarrusUpgradeBranch branch,
            CardDefinitionId[] first, CardDefinitionId[] second)
        {
            if (!Enum.IsDefined(typeof(GarrusUpgradeBranch), branch)) throw new ArgumentOutOfRangeException(nameof(branch));
            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Staged cards require owner and per-seat identity.");
            var inHand = new HashSet<CardDefinitionId>(branch == GarrusUpgradeBranch.FirstSibling ? first : second)
                { GarrusCardContent.AngryStrike, GarrusCardContent.ChillingHowl };
            var cards = GarrusCardContent.DealtDefinitions.Select(definition => inHand.Contains(definition.Id)
                ? CardInstance.CreateInHand(new CardInstanceId(prefix + "-" + definition.Id.Value), definition.Reference, owner)
                : CardInstance.CreateInDeck(new CardInstanceId(prefix + "-" + definition.Id.Value), definition.Reference, owner)).ToArray();
            return new ConfiguredHand(GarrusCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand), cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
