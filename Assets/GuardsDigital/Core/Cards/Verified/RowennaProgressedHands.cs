using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.57g. Which of rulebook p.15's two siblings a staged Rowenna seat took at each
    /// upgrade, so a fixture can name the half of her surface it wants without driving nine level-ups.
    ///
    /// <para>Her three ladders split the same way on every colour, and the split decides which
    /// contracts a staged match can reach:</para>
    /// <list type="bullet">
    /// <item><description><b>FirstSibling</b> - Fair Share, Protect the Weak and Melee at Tier II;
    /// Paragon of Grace, Defend the Innocent and Grand Melee at Tier III. This is the branch that
    /// carries Defend the Innocent's own retrieve, Grand Melee's "Choose up to two times" and Paragon
    /// of Grace's Range item - the item Fabled Lance's printed Range 2 composes with.</description>
    /// </item>
    /// <item><description><b>SecondSibling</b> - Feat of Bravery, Accept Surrender and Opening Shots
    /// at Tier II; Paragon of Valor, Glorious Triumph and Opening Volley at Tier III. This is the only
    /// branch that reaches either Defeat Blue (Accept Surrender's empty-hand narrowing and Glorious
    /// Triumph's tripled assist coins) and the two opening-shot discards.</description></item>
    /// </list>
    ///
    /// <para>Both basics stay in hand at every stage, exactly as p.6 leaves them: her Gold Code of
    /// Chivalry and her Silver Throw the Gauntlet are never upgraded away.</para>
    ///
    /// <para>These are internal staged hands rather than a substitute for the real p.15 progression
    /// transaction - the same statement every other hero's file of this shape makes.</para>
    /// </summary>
    public enum RowennaUpgradeBranch
    {
        FirstSibling = 0,
        SecondSibling = 1
    }

    /// <summary>
    /// Milestone 8.57g. One seat's Rowenna hand at rulebook p.15's Tier II or Tier III stage, built
    /// from her own dealt definitions so a staged match holds real printed cards rather than a
    /// fixture's copy of them.
    /// </summary>
    public static class RowennaProgressedHands
    {
        public static ConfiguredHand CreateTierTwoHand(
            HeroId owner, string prefix, RowennaUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    RowennaCardContent.FairShare,
                    RowennaCardContent.ProtectTheWeak,
                    RowennaCardContent.Melee
                },
                new[]
                {
                    RowennaCardContent.FeatOfBravery,
                    RowennaCardContent.AcceptSurrender,
                    RowennaCardContent.OpeningShots
                });

        public static ConfiguredHand CreateTierThreeHand(
            HeroId owner, string prefix, RowennaUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    RowennaCardContent.ParagonOfGrace,
                    RowennaCardContent.DefendTheInnocent,
                    RowennaCardContent.GrandMelee
                },
                new[]
                {
                    RowennaCardContent.ParagonOfValor,
                    RowennaCardContent.GloriousTriumph,
                    RowennaCardContent.OpeningVolley
                });

        private static ConfiguredHand Create(
            HeroId owner,
            string prefix,
            RowennaUpgradeBranch branch,
            CardDefinitionId[] first,
            CardDefinitionId[] second)
        {
            if (!Enum.IsDefined(typeof(RowennaUpgradeBranch), branch))
            {
                throw new ArgumentOutOfRangeException(nameof(branch));
            }

            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException(
                    "Staged cards require a defined owner and a per-seat instance identity.");
            }

            var inHand = new HashSet<CardDefinitionId>(
                branch == RowennaUpgradeBranch.FirstSibling ? first : second)
            {
                RowennaCardContent.CodeOfChivalry,
                RowennaCardContent.ThrowTheGauntlet
            };

            var cards = RowennaCardContent.DealtDefinitions
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
                RowennaCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
