using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Bain hand took. Milestone 8.40i, and the same
    /// choice every dealt hero names for the reason they name it: rulebook p.15 offers a pair at each
    /// Tier and the player takes one, so a single seat can only ever hold three of a Tier's six cards,
    /// and the untaken sibling becomes p.16's item.
    /// </summary>
    public enum BainUpgradeBranch
    {
        /// <summary>
        /// Heavy Crossbow, Dead Man's Hand, Narrow Escape - and their Tier III successors Arbalest,
        /// We're Not Done Yet! and Perfect Getaway.
        /// </summary>
        FirstSibling = 0,

        /// <summary>
        /// Hand Crossbow, Drinking Buddies, Vantage Point - and their Tier III successors
        /// Hunter-Seeker, Another One! and High Ground.
        /// </summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Bain's setup as it stands after rulebook p.15 has upgraded him - one seat's hand at Tier II or
    /// Tier III, staged directly for a fixture. Milestone 8.40i, on the terms
    /// <see cref="WaspProgressedHands"/> records and the fourteen sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations the
    /// page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>The two branches follow his printed lineages rather than the inventory's table order,
    /// and the split is what makes both branches matter rather than one standing for the other.</b> The
    /// FIRST branch is the one that keeps his plain clear-line shot at both Tiers - Heavy Crossbow at
    /// II and Arbalest at III, which are also the only faces reaching Range 4 - and it is the only one
    /// holding the failure modal (We're Not Done Yet!) and the only Block that needs no marker to come
    /// back (Perfect Getaway). The SECOND branch is the only one holding a marker modal at either Tier
    /// (Hand Crossbow at II, Hunter-Seeker at III, and only the latter can take both bullets), the only
    /// one holding the shared retrieval (Drinking Buddies at II, Another One! at III, and only the
    /// latter repeats at the turn's end), and the only one holding a Movement primary at all. So no
    /// single staged hand can reach his whole surface, and the four progressed arms between them do.
    /// </para>
    ///
    /// <para>The two basics kept in hand are Dead or Alive and Get over here!. Side Quest is p.20's
    /// Handicap card and is already out of the box at p.6's setup, which is why
    /// <see cref="BainCardContent.DealtDefinitions"/> is what these hands are built from - and its give
    /// stays parked on the p.20 deferral either way.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class BainProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            BainCardContent.HeavyCrossbow, BainCardContent.DeadMansHand, BainCardContent.NarrowEscape
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            BainCardContent.HandCrossbow, BainCardContent.DrinkingBuddies,
            BainCardContent.VantagePoint
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            BainCardContent.Arbalest, BainCardContent.WereNotDoneYet, BainCardContent.PerfectGetaway
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            BainCardContent.HunterSeeker, BainCardContent.AnotherOne, BainCardContent.HighGround
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, BainUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == BainUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// A Complicated Profession is dealt to the deck here, exactly as p.6's setup and the
        /// initial-setup validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, BainUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == BainUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Bain's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                BainCardContent.DeadOrAlive,
                BainCardContent.GetOverHere
            };
            var cards = BainCardContent.DealtDefinitions
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
                BainCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
