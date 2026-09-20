using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.61j. Which of rulebook p.15's two siblings a staged Min seat took at each upgrade,
    /// so a fixture can name the half of her surface it wants without driving nine level-ups.
    ///
    /// <para>Her three ladders split the same way on every colour, and the split decides which
    /// contracts a staged match can reach:</para>
    /// <list type="bullet">
    /// <item><description><b>FirstSibling</b> - Tiger Stance, Cluster Mine and Vanish at Tier II;
    /// Dragon Stance, Mine Field and Ruse at Tier III. This is the branch that carries the two
    /// step-then-push riders (Tiger's one space, Dragon's one or two), the two widest mine
    /// placements - Cluster Mine's 1 blast + 2 duds and Mine Field's 2 blasts + 1 dud, which is the
    /// only face that springs two blasts on one crossing - and Ruse, the only Block that may re-place
    /// the bomb.</description></item>
    /// <item><description><b>SecondSibling</b> - Viper Stance, Death Grenade and Inner Strength at
    /// Tier II; Cobra Stance, Holy Death Grenade and Perfect Self at Tier III. This is the only
    /// branch that reaches either Grenade's end-of-turn window - and Holy Death Grenade is the one
    /// face that asks it of TWO heroes - the two bomb-swapping riders, and both doubling faces,
    /// Perfect Self's deck-card item included.</description></item>
    /// </list>
    ///
    /// <para>Both basics stay in hand at every stage, exactly as p.6 leaves them: her Gold Fast as
    /// Lightning and her Silver Smoke Bomb are never upgraded away - and that matters more for her
    /// than for most, because Fast as Lightning's whole rider is a reading of the RED card she is
    /// holding, so every branch of this file keeps the borrow's pool alive.</para>
    ///
    /// <para>These are internal staged hands rather than a substitute for the real p.15 progression
    /// transaction - the same statement every other hero's file of this shape makes.</para>
    /// </summary>
    public enum MinUpgradeBranch
    {
        FirstSibling = 0,
        SecondSibling = 1
    }

    /// <summary>
    /// Milestone 8.61j. One seat's Min hand at rulebook p.15's Tier II or Tier III stage, built from
    /// her own dealt definitions so a staged match holds real printed cards rather than a fixture's
    /// copy of them.
    /// </summary>
    public static class MinProgressedHands
    {
        public static ConfiguredHand CreateTierTwoHand(
            HeroId owner, string prefix, MinUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    MinCardContent.TigerStance,
                    MinCardContent.ClusterMine,
                    MinCardContent.Vanish
                },
                new[]
                {
                    MinCardContent.ViperStance,
                    MinCardContent.DeathGrenade,
                    MinCardContent.InnerStrength
                });

        public static ConfiguredHand CreateTierThreeHand(
            HeroId owner, string prefix, MinUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    MinCardContent.DragonStance,
                    MinCardContent.MineField,
                    MinCardContent.Ruse
                },
                new[]
                {
                    MinCardContent.CobraStance,
                    MinCardContent.HolyDeathGrenade,
                    MinCardContent.PerfectSelf
                });

        private static ConfiguredHand Create(
            HeroId owner,
            string prefix,
            MinUpgradeBranch branch,
            CardDefinitionId[] first,
            CardDefinitionId[] second)
        {
            if (!Enum.IsDefined(typeof(MinUpgradeBranch), branch))
            {
                throw new ArgumentOutOfRangeException(nameof(branch));
            }

            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException(
                    "Staged cards require a defined owner and a per-seat instance identity.");
            }

            var inHand = new HashSet<CardDefinitionId>(
                branch == MinUpgradeBranch.FirstSibling ? first : second)
            {
                MinCardContent.FastAsLightning,
                MinCardContent.SmokeBomb
            };

            var cards = MinCardContent.DealtDefinitions
                .Select(definition => inHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference,
                        owner)
                    : CardInstance.CreateInDeck(
                        new CardInstanceId(prefix + "-" + definition.Id.Value),
                        definition.Reference,
                        owner))
                .ToArray();

            return new ConfiguredHand(
                MinCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
