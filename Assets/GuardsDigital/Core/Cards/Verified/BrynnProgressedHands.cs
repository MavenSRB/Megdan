using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.69g. Which of rulebook p.15's two siblings a staged Brynn seat took at each upgrade,
    /// so a fixture can name the half of his surface it wants without driving nine level-ups.
    ///
    /// <para>His three ladders split the same way on every colour, and - more cleanly than any hero
    /// before him - the split falls along the lines his five composition blocks were drawn on, so each
    /// branch reaches a whole family rather than a scattering:</para>
    /// <list type="bullet">
    /// <item><description><b>FirstSibling</b> - Elevated Ambush, Cover Tracks and Log Trap at Tier II;
    /// Peak Precision, Hide Traces and Deadfall Trap at Tier III. This is the branch that carries the
    /// PRINTED PLUS (Elevated Ambush's and Peak Precision's "+2 attack" when the target is a hero
    /// adjacent to 3 or more obstacles), Peak Precision's "you may retrieve this card", the two
    /// tracking swaps with a trailing move (Cover Tracks' optional one space, Hide Traces' "up to 2"),
    /// and the trap escalation - Deadfall Trap is the only face of his that defeats a hero who cannot
    /// discard.</description></item>
    /// <item><description><b>SecondSibling</b> - Split Attack, Mountain Guide and True Grit at Tier II;
    /// Split Throw, Expedition Leader and Die Hard at Tier III. This is the only branch that reaches
    /// either SPLIT REPEAT - Split Attack's optional one on a different unit adjacent to him and Split
    /// Throw's `IfAble` one on a different target unit - either GUIDE's two sentences, and either grit
    /// face's "retrieve a discarded attack card" with its conditional move.</description></item>
    /// </list>
    ///
    /// <para>Both basics stay in hand at every stage, exactly as p.6 leaves them: his Gold Familiar
    /// Ground and his Silver Decoy are never upgraded away - and that matters for him, because Familiar
    /// Ground is the only face of his whose modal narrows an ATTACK TARGET by the printed predicate,
    /// and Decoy is the only one that moves an enemy unit at all.</para>
    ///
    /// <para>These are internal staged hands rather than a substitute for the real p.15 progression
    /// transaction - the same statement every other hero's file of this shape makes.</para>
    /// </summary>
    public enum BrynnUpgradeBranch
    {
        FirstSibling = 0,
        SecondSibling = 1
    }

    /// <summary>
    /// Milestone 8.69g. One seat's Brynn hand at rulebook p.15's Tier II or Tier III stage, built from
    /// his own dealt definitions so a staged match holds real printed cards rather than a fixture's
    /// copy of them.
    /// </summary>
    public static class BrynnProgressedHands
    {
        public static ConfiguredHand CreateTierTwoHand(
            HeroId owner, string prefix, BrynnUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    BrynnCardContent.ElevatedAmbush,
                    BrynnCardContent.CoverTracks,
                    BrynnCardContent.LogTrap
                },
                new[]
                {
                    BrynnCardContent.SplitAttack,
                    BrynnCardContent.MountainGuide,
                    BrynnCardContent.TrueGrit
                });

        public static ConfiguredHand CreateTierThreeHand(
            HeroId owner, string prefix, BrynnUpgradeBranch branch) =>
            Create(
                owner, prefix, branch,
                new[]
                {
                    BrynnCardContent.PeakPrecision,
                    BrynnCardContent.HideTraces,
                    BrynnCardContent.DeadfallTrap
                },
                new[]
                {
                    BrynnCardContent.SplitThrow,
                    BrynnCardContent.ExpeditionLeader,
                    BrynnCardContent.DieHard
                });

        private static ConfiguredHand Create(
            HeroId owner,
            string prefix,
            BrynnUpgradeBranch branch,
            CardDefinitionId[] first,
            CardDefinitionId[] second)
        {
            if (!Enum.IsDefined(typeof(BrynnUpgradeBranch), branch))
            {
                throw new ArgumentOutOfRangeException(nameof(branch));
            }

            if (!owner.IsDefined || string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException(
                    "Staged cards require a defined owner and a per-seat instance identity.");
            }

            var inHand = new HashSet<CardDefinitionId>(
                branch == BrynnUpgradeBranch.FirstSibling ? first : second)
            {
                BrynnCardContent.FamiliarGround,
                BrynnCardContent.Decoy
            };

            var cards = BrynnCardContent.DealtDefinitions
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
                BrynnCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location == CardLocation.Hand),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
