using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.75b. Which of rulebook p.15's two siblings a staged Ignatia seat took at each
    /// upgrade, so a fixture can name the half of her surface it wants without driving nine level-ups.
    ///
    /// <para><b>Her ladders fork twice each, and both forks are open.</b> Unlike Hanu's, every one of
    /// her Tier II faces offers BOTH Tier III faces of its colour - the 8.75 inventory pass named one
    /// successor apiece on the Blue and Green ladders and 8.75's own fixtures corrected it - so the
    /// two branches below are a CHOICE of route rather than a pair of tracks. They are drawn along the
    /// lines her composition blocks were drawn on, so each branch reaches whole families rather than a
    /// scattering:</para>
    /// <list type="bullet">
    /// <item><description><b>FirstSibling</b> - Erratic Fireblast, Spontaneous Immolation and Path of
    /// Cinders at Tier II; Loosely-Aimed Firebolts, Violent Conflagration and Path of Flames at Tier
    /// III. This is the branch that carries the STRAIGHT-LINE attack pair (☀ "not in a straight line"
    /// / ☾ "in a straight line") with Loosely-Aimed Firebolts' printed "may repeat once on a different
    /// hero", the whole DISCARD line including Violent Conflagration's "discards a card, or is
    /// defeated" escalation and its moon Defeat, and the two longer MAGMA Paths - the up-to-3 and
    /// up-to-4 wakes, which are the only faces that can drain her three-token supply on the sun line
    /// alone.</description></item>
    /// <item><description><b>SecondSibling</b> - Crack of Doom, Searing Heat and Unstable Portal at
    /// Tier II; Imminent Eruption, Scorching Blaze and Chaos Gate at Tier III. This is the only branch
    /// that reaches the ADJACENT / AT-MAXIMUM-RANGE attack pair with Imminent Eruption's "may repeat
    /// once on a minion", either PUSH ("Move a friendly/enemy hero in radius 2 (2 or 3) spaces in a
    /// straight line"), or either SWAP, with Chaos Gate's trailing "you may move that unit 1 space" /
    /// "you may move 1 space".</description></item>
    /// </list>
    ///
    /// <para>Both basics stay in hand at every stage, exactly as p.6 leaves them: her Gold Chaos Bolt
    /// and her Silver Equilibrium are never upgraded away - and that matters for her more than the
    /// convention says, because Equilibrium is the one face in the set that frees the coin, so a
    /// staged hand without it could never reach the pause `ignatia-coin-sun@1` opens. Chaos Incarnate
    /// is reachable on BOTH branches, since p.16 unlocks it from the deck rather than upgrading into
    /// it.</para>
    ///
    /// <para>These are internal staged hands rather than a substitute for the real p.15 progression
    /// transaction - the same statement every other hero's file of this shape makes.</para>
    /// </summary>
    public enum IgnatiaUpgradeBranch
    {
        FirstSibling = 0,
        SecondSibling = 1
    }

    /// <summary>
    /// Milestone 8.75b. One seat's Ignatia hand at rulebook p.15's Tier II or Tier III stage, built
    /// from her own dealt definitions so a staged match holds real printed cards rather than a
    /// fixture's copy of them.
    /// </summary>
    public static class IgnatiaProgressedHands
    {
        public static ConfiguredHand CreateTierTwoHand(
            HeroId owner, string prefix, IgnatiaUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    IgnatiaCardContent.ErraticFireblast,
                    IgnatiaCardContent.SpontaneousImmolation,
                    IgnatiaCardContent.PathOfCinders
                },
                new[]
                {
                    IgnatiaCardContent.CrackOfDoom,
                    IgnatiaCardContent.SearingHeat,
                    IgnatiaCardContent.UnstablePortal
                });

        public static ConfiguredHand CreateTierThreeHand(
            HeroId owner, string prefix, IgnatiaUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    IgnatiaCardContent.LooselyAimedFirebolts,
                    IgnatiaCardContent.ViolentConflagration,
                    IgnatiaCardContent.PathOfFlames
                },
                new[]
                {
                    IgnatiaCardContent.ImminentEruption,
                    IgnatiaCardContent.ScorchingBlaze,
                    IgnatiaCardContent.ChaosGate
                });

        private static ConfiguredHand Create(
            HeroId owner,
            string prefix,
            IgnatiaUpgradeBranch branch,
            CardDefinitionId[] first,
            CardDefinitionId[] second)
        {
            if (!Enum.IsDefined(typeof(IgnatiaUpgradeBranch), branch))
            {
                throw new ArgumentOutOfRangeException(nameof(branch));
            }

            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException(
                    "Staged cards require a defined owner and a per-seat instance identity.");
            }

            var inHand = new HashSet<CardDefinitionId>(
                branch == IgnatiaUpgradeBranch.FirstSibling ? first : second)
            {
                IgnatiaCardContent.ChaosBolt,
                IgnatiaCardContent.Equilibrium
            };

            var cards = IgnatiaCardContent.DealtDefinitions
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
                IgnatiaCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
