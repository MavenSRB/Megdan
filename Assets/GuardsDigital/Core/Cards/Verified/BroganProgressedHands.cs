using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Brogan hand took. Milestone 8.21, and the
    /// same choice <see cref="ArienUpgradeBranch"/> names for the reason it names it: rulebook p.15
    /// offers a pair at each Tier and the player takes one, so a single seat can only ever hold three
    /// of a Tier's six cards, and the untaken sibling becomes p.16's item.
    ///
    /// <para>The two are named by which sibling they take rather than by what a fixture uses them for,
    /// because that is what the page's choice is: neither is a default and neither is a variant.</para>
    /// </summary>
    public enum BroganUpgradeBranch
    {
        /// <summary>Bullrush, Mighty Punch, Bolster - and their Tier III successors.</summary>
        FirstSibling = 0,

        /// <summary>Throwing Axe, Shield Bash, War Drummer - and their Tier III successors.</summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Brogan's setup as it stands after rulebook p.15 has upgraded him - one seat's hand at Tier II or
    /// Tier III, staged directly for a fixture. Milestone 8.21, on the terms
    /// <see cref="WaspProgressedHands"/> records and <see cref="ArienProgressedHands"/> repeats.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em> - the unlock is the Ultimate's own position,
    /// and p.15's returns are why the lower-Tier cards sit in the deck - so these hands state exactly
    /// the locations the page's own procedure produces and nothing else. Items and Levels do not carry:
    /// the session derives fresh progression state.</para>
    ///
    /// <para>The three upgraded cards per hand are one of each colour, per p.15's own closing count
    /// ("exactly 5 cards, 1 of each color"), and the two branches together reach all six cards of a
    /// Tier - which is what lets a progression gate exercise every printed face rather than half of
    /// them.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class BroganProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            BroganCardContent.Bullrush, BroganCardContent.MightyPunch,
            BroganCardContent.Bolster
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            BroganCardContent.ThrowingAxe, BroganCardContent.ShieldBash,
            BroganCardContent.WarDrummer
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            BroganCardContent.FuriousCharge, BroganCardContent.SavageKick,
            BroganCardContent.Fortify
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            BroganCardContent.ThrowingSpear, BroganCardContent.Counterattack,
            BroganCardContent.MasterSkald
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, BroganUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == BroganUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// One Man Army is dealt to the deck here, exactly as p.6's setup and the initial-setup
        /// validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area, which keeps this staging on the same single writer the
        /// rules use.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, BroganUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == BroganUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Brogan's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                BroganCardContent.Onslaught,
                BroganCardContent.Bulwark
            };
            var cards = BroganCardContent.DealtDefinitions
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
                BroganCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
