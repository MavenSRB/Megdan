using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    public enum WidgetUpgradeBranch { FirstSibling = 0, SecondSibling = 1 }

    /// <summary>Internal staged hands, not a substitute for the real progression transaction.</summary>
    public static class WidgetProgressedHands
    {
        public static ConfiguredHand CreateTierTwoHand(HeroId owner, string prefix, WidgetUpgradeBranch branch) =>
            Create(owner, prefix, branch, new[] { WidgetCardContent.DiversionaryAttack, WidgetCardContent.TakeOff, WidgetCardContent.FlamingBreath },
                new[] { WidgetCardContent.AirborneAttack, WidgetCardContent.DragOff, WidgetCardContent.Nibble });
        public static ConfiguredHand CreateTierThreeHand(HeroId owner, string prefix, WidgetUpgradeBranch branch) =>
            Create(owner, prefix, branch, new[] { WidgetCardContent.DiversionaryAssault, WidgetCardContent.SafeLanding, WidgetCardContent.ScorchingBreath },
                new[] { WidgetCardContent.AirborneAssault, WidgetCardContent.CarryAway, WidgetCardContent.Gnaw });

        private static ConfiguredHand Create(HeroId owner, string prefix, WidgetUpgradeBranch branch,
            CardDefinitionId[] first, CardDefinitionId[] second)
        {
            if (!Enum.IsDefined(typeof(WidgetUpgradeBranch), branch)) throw new ArgumentOutOfRangeException(nameof(branch));
            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Staged cards require owner and per-seat identity.");
            var inHand = new HashSet<CardDefinitionId>(branch == WidgetUpgradeBranch.FirstSibling ? first : second)
                { WidgetCardContent.FightAsOne, WidgetCardContent.DragonBond };
            var cards = WidgetCardContent.DealtDefinitions.Select(definition => inHand.Contains(definition.Id)
                ? CardInstance.CreateInHand(new CardInstanceId(prefix + "-" + definition.Id.Value), definition.Reference, owner)
                : CardInstance.CreateInDeck(new CardInstanceId(prefix + "-" + definition.Id.Value), definition.Reference, owner)).ToArray();
            return new ConfiguredHand(WidgetCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand), cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
