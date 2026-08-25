using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Tigerclaw hand took. Milestone 8.23, and the
    /// same choice <see cref="SabinaUpgradeBranch"/> names for the reason it names it: rulebook p.15
    /// offers a pair at each Tier and the player takes one, so a single seat can only ever hold three
    /// of a Tier's six cards, and the untaken sibling becomes p.16's item.
    /// </summary>
    public enum TigerclawUpgradeBranch
    {
        /// <summary>Combat Reflexes, Sidestep, Pick Pocket - and their Tier III successors.</summary>
        FirstSibling = 0,

        /// <summary>Backstab, Parry, Poisoned Dagger - and their Tier III successors.</summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Tigerclaw's setup as it stands after rulebook p.15 has upgraded him - one seat's hand at Tier II
    /// or Tier III, staged directly for a fixture. Milestone 8.23, on the terms
    /// <see cref="WaspProgressedHands"/> records and the three sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations
    /// the page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>His branches are the first where the two siblings of a colour resolve differently in
    /// kind.</b> Blue's pair is one Ranged Block and one non-Ranged Block - Sidestep against Parry, and
    /// Evade against Riposte - so which branch a staged hand takes decides which attacks it can stop at
    /// all. Green's is a coin card against a marker card. That is content rather than fixture design,
    /// and it is why both branches matter to the gate rather than one standing for the other.</para>
    ///
    /// <para>The two basics kept in hand are Blink Strike and Blend Into Shadows. Nimble Strike is
    /// p.20's Handicap card and is already out of the box at p.6's setup, which is why
    /// <see cref="TigerclawCardContent.DealtDefinitions"/> is what these hands are built from.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class TigerclawProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            TigerclawCardContent.CombatReflexes, TigerclawCardContent.Sidestep,
            TigerclawCardContent.PickPocket
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            TigerclawCardContent.Backstab, TigerclawCardContent.Parry,
            TigerclawCardContent.PoisonedDagger
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            TigerclawCardContent.LeapingStrike, TigerclawCardContent.Evade,
            TigerclawCardContent.MasterThief
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            TigerclawCardContent.BackstabWithABallista, TigerclawCardContent.Riposte,
            TigerclawCardContent.PoisonedDart
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, TigerclawUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == TigerclawUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// Cloak and Daggers is dealt to the deck here, exactly as p.6's setup and the initial-setup
        /// validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, TigerclawUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == TigerclawUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "Tigerclaw's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                TigerclawCardContent.BlinkStrike,
                TigerclawCardContent.BlendIntoShadows
            };
            var cards = TigerclawCardContent.DealtDefinitions
                .Select(definition =>
                {
                    var instanceId = new CardInstanceId(
                        instanceIdPrefix + "-" + definition.Id.Value);
                    return inHand.Contains(definition.Id)
                        ? CardInstance.CreateInHand(instanceId, definition.Reference, ownerId)
                        : CardInstance.CreateInDeck(instanceId, definition.Reference, ownerId);
                })
                .ToArray();
            return new ConfiguredHand(
                TigerclawCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
