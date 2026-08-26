using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Xargatha hand took. Milestone 8.24, and the
    /// same choice <see cref="TigerclawUpgradeBranch"/> names for the reason it names it: rulebook p.15
    /// offers a pair at each Tier and the player takes one, so a single seat can only ever hold three
    /// of a Tier's six cards, and the untaken sibling becomes p.16's item.
    /// </summary>
    public enum XargathaUpgradeBranch
    {
        /// <summary>Deadly Swipe, Petrifying Stare, Control - and their Tier III successors.</summary>
        FirstSibling = 0,

        /// <summary>Long Thrust, Fresh Converts, Constrict - and their Tier III successors.</summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Xargatha's setup as it stands after rulebook p.15 has upgraded her - one seat's hand at Tier II
    /// or Tier III, staged directly for a fixture. Milestone 8.24, on the terms
    /// <see cref="WaspProgressedHands"/> records and the four sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations
    /// the page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>Her two branches are the widest apart in the roster, and that is content.</b> The first
    /// takes the petrification line and the two remaining Movement primaries - a seat that stops enemy
    /// heroes moving and walks minions around. The second takes the Ranged Attacks, the retrievals and
    /// the two end-of-round Defeats - a seat that never petrifies anything and whose Skills do nothing
    /// at all until p.8's pre-battle moment. Which branch a staged hand takes therefore decides which
    /// of her mechanics exist in that match, which is why both matter to the gate rather than one
    /// standing for the other.</para>
    ///
    /// <para>The two basics kept in hand are Cleave and Siren's Call. Desperate Cleave is p.20's
    /// Handicap card and is already out of the box at p.6's setup, which is why
    /// <see cref="XargathaCardContent.DealtDefinitions"/> is what these hands are built from.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class XargathaProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            XargathaCardContent.DeadlySwipe, XargathaCardContent.PetrifyingStare,
            XargathaCardContent.Control
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            XargathaCardContent.LongThrust, XargathaCardContent.FreshConverts,
            XargathaCardContent.Constrict
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            XargathaCardContent.LethalSpin, XargathaCardContent.TurnIntoStatues,
            XargathaCardContent.Dominate
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            XargathaCardContent.RapidThrusts, XargathaCardContent.DevotedFollowers,
            XargathaCardContent.FinalEmbrace
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, XargathaUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == XargathaUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// Metamorphosis is dealt to the deck here, exactly as p.6's setup and the initial-setup
        /// validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, XargathaUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == XargathaUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "Xargatha's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                XargathaCardContent.Cleave,
                XargathaCardContent.SirensCall
            };
            var cards = XargathaCardContent.DealtDefinitions
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
                XargathaCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
