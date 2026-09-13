using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Cutter hand took. Milestone 8.39h, and the
    /// same choice every dealt hero names for the reason they name it: rulebook p.15 offers a pair at
    /// each Tier and the player takes one, so a single seat can only ever hold three of a Tier's six
    /// cards, and the untaken sibling becomes p.16's item.
    /// </summary>
    public enum CutterUpgradeBranch
    {
        /// <summary>
        /// Bold Thrust, Barrage, Ramming Speed - and their Tier III successors Fearless Lunge,
        /// Broadside and Crashland.
        /// </summary>
        FirstSibling = 0,

        /// <summary>
        /// Evasive Shot, X Marks the Spot, Outmaneuver - and their Tier III successors Tumble Shot, A
        /// Fistful of Coins and Outsmart.
        /// </summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Cutter's setup as it stands after rulebook p.15 has upgraded her - one seat's hand at Tier II or
    /// Tier III, staged directly for a fixture. Milestone 8.39h, on the terms
    /// <see cref="WaspProgressedHands"/> records and the thirteen sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations the
    /// page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>The two branches follow her printed lineages rather than the inventory's table
    /// order, and the split is what makes both branches matter rather than one standing for the
    /// other.</b> The FIRST branch is the one that reaches her melee lunge at both Tiers - Bold Thrust
    /// at II and Fearless Lunge at III - and it is also the only one holding the excluding repeat
    /// (Broadside) and the only one whose charge reaches five spaces (Crashland). The SECOND branch is
    /// the only one holding a Ranged Attack at either Tier (Evasive Shot at II, Tumble Shot at III,
    /// which are also the only faces that retreat), the only one holding an enemy-chosen modal (X Marks
    /// the Spot at II, A Fistful of Coins at III, and only the latter can end the match), and the only
    /// one holding a minion Swap. So no single staged hand can reach her whole surface, and the four
    /// progressed arms between them do.</para>
    ///
    /// <para>The two basics kept in hand are Walk the Plank and Grappling Bolt. Few More Steps is
    /// p.20's Handicap card and is already out of the box at p.6's setup, which is why
    /// <see cref="CutterCardContent.DealtDefinitions"/> is what these hands are built from - and its
    /// push clause stays parked on the p.20 deferral either way.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class CutterProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            CutterCardContent.BoldThrust, CutterCardContent.Barrage, CutterCardContent.RammingSpeed
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            CutterCardContent.EvasiveShot, CutterCardContent.XMarksTheSpot,
            CutterCardContent.Outmaneuver
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            CutterCardContent.FearlessLunge, CutterCardContent.Broadside, CutterCardContent.Crashland
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            CutterCardContent.TumbleShot, CutterCardContent.AFistfulOfCoins,
            CutterCardContent.Outsmart
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, CutterUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == CutterUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// Legend of the Skies is dealt to the deck here, exactly as p.6's setup and the initial-setup
        /// validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, CutterUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == CutterUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Cutter's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                CutterCardContent.WalkThePlank,
                CutterCardContent.GrapplingBolt
            };
            var cards = CutterCardContent.DealtDefinitions
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
                CutterCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
