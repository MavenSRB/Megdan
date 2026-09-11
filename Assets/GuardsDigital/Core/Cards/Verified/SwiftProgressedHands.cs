using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Swift hand took. Milestone 8.38d, and the same
    /// choice every dealt hero names for the reason they name it: rulebook p.15 offers a pair at each
    /// Tier and the player takes one, so a single seat can only ever hold three of a Tier's six cards,
    /// and the untaken sibling becomes p.16's item.
    /// </summary>
    public enum SwiftUpgradeBranch
    {
        /// <summary>
        /// Prepared Shot, Assault Jump, Pin Down - and their Tier III successors Killshot, Drop Trooper
        /// and Killing Ground.
        /// </summary>
        FirstSibling = 0,

        /// <summary>
        /// Shotgun, Delayed Jump, Mark for Death - and their Tier III successors Super-Shotgun, Mobile
        /// Scout and Hunting Season.
        /// </summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Swift's setup as it stands after rulebook p.15 has upgraded her - one seat's hand at Tier II or
    /// Tier III, staged directly for a fixture. Milestone 8.38d, on the terms
    /// <see cref="WaspProgressedHands"/> records and the twelve sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations the
    /// page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>Her two branches divide her kit along the lines her own new vocabulary falls on, and the
    /// split is what makes both branches matter to the gate rather than one standing for the other.</b>
    /// The FIRST branch is the one that reaches milestone 8.38a's straight-line Attack band at both
    /// Tiers - Prepared Shot at II and Killshot at III - and it is also where the multi-unit push lands
    /// (Drop Trooper) and where the escalating terrain clause survives to Tier III (Killing Ground). The
    /// SECOND branch is the only one holding an "End of turn:" face at either Tier (Delayed Jump at II,
    /// Mobile Scout at III, which is also the only face that fast travels), the only one holding a
    /// bounty (Mark for Death at II, Hunting Season at III, and only the latter stands two promises),
    /// and the only one whose Attacks escalate a refused discard to a Defeat (Super-Shotgun). So no
    /// single staged hand can reach her whole surface, and the four progressed arms between them
    /// do.</para>
    ///
    /// <para>The two basics kept in hand are Reload! and Bounce. Backup is p.20's Handicap card and is
    /// already out of the box at p.6's setup, which is why
    /// <see cref="SwiftCardContent.DealtDefinitions"/> is what these hands are built from - and its
    /// borrow clause stays parked on the p.20 deferral either way.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class SwiftProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            SwiftCardContent.PreparedShot, SwiftCardContent.AssaultJump, SwiftCardContent.PinDown
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            SwiftCardContent.Shotgun, SwiftCardContent.DelayedJump, SwiftCardContent.MarkForDeath
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            SwiftCardContent.Killshot, SwiftCardContent.DropTrooper, SwiftCardContent.KillingGround
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            SwiftCardContent.SuperShotgun, SwiftCardContent.MobileScout,
            SwiftCardContent.HuntingSeason
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, SwiftUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == SwiftUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// Bullet Time is dealt to the deck here, exactly as p.6's setup and the initial-setup validator
        /// require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, SwiftUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == SwiftUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Swift's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                SwiftCardContent.Reload,
                SwiftCardContent.Bounce
            };
            var cards = SwiftCardContent.DealtDefinitions
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
                SwiftCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
