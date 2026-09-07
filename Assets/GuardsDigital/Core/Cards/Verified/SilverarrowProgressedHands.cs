using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Silverarrow hand took. Milestone
    /// 8.35z46, and the same choice <see cref="DodgerUpgradeBranch"/> names for the
    /// reason it names it: rulebook p.15 offers a pair at each Tier and the player takes one, so a
    /// single seat can only ever hold three of a Tier's six cards, and the untaken sibling becomes
    /// p.16's item.
    /// </summary>
    public enum SilverarrowUpgradeBranch
    {
        /// <summary>
        /// Opportunity Shot, Entangling Vines, Divert Attention - and their Tier III successors Clear
        /// Shot, Grappling Branches and Lead Astray.
        /// </summary>
        FirstSibling = 0,

        /// <summary>
        /// Long Shot, Warning Shot, Fae Healing - and their Tier III successors Rain of Arrows,
        /// Treetop Sentinel and Nature's Blessing.
        /// </summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Silverarrow's setup as it stands after rulebook p.15 has upgraded her - one seat's hand at Tier
    /// II or Tier III, staged directly for a fixture. Milestone 8.35z46, on the terms
    /// <see cref="WaspProgressedHands"/> records and the six sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations
    /// the page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>Her two branches divide her kit along the line her own rulings fall on, and the split
    /// is what makes both branches matter to the gate rather than one standing for the other.</b> The
    /// SECOND branch is the only one that holds an "End of turn:" face at either Tier - Warning Shot
    /// at II and Treetop Sentinel at III - and it is also the only one holding a retrieval face (Fae
    /// Healing, Nature's Blessing, RULING 8) or Rain of Arrows' mandatory chain (RULING 38). The FIRST
    /// branch is where the Red modal survives to Tier III (Clear Shot, RULING 39), where the third
    /// movement-restriction Blue lands (Grappling Branches), and where both realised-distance
    /// self-moves live (Divert Attention at II, Lead Astray at III, RULING 37). So no single staged
    /// hand can reach her whole surface, and the four progressed arms between them do.</para>
    ///
    /// <para>The two basics kept in hand are Shoot and Scoot and Trailblazer. Keep the Distance is
    /// p.20's Handicap card and is already out of the box at p.6's setup, which is why
    /// <see cref="SilverarrowCardContent.DealtDefinitions"/> is what these hands are built from -
    /// and its "After the attack:" sentence stays parked on the p.20 deferral either way.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class SilverarrowProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            SilverarrowCardContent.OpportunityShot, SilverarrowCardContent.EntanglingVines,
            SilverarrowCardContent.DivertAttention
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            SilverarrowCardContent.LongShot, SilverarrowCardContent.WarningShot,
            SilverarrowCardContent.FaeHealing
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            SilverarrowCardContent.ClearShot, SilverarrowCardContent.GrapplingBranches,
            SilverarrowCardContent.LeadAstray
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            SilverarrowCardContent.RainOfArrows, SilverarrowCardContent.TreetopSentinel,
            SilverarrowCardContent.NaturesBlessing
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, SilverarrowUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == SilverarrowUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// Wild Hunt is dealt to the deck here, exactly as p.6's setup and the initial-setup validator
        /// require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, SilverarrowUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == SilverarrowUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "Silverarrow's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                SilverarrowCardContent.ShootAndScoot,
                SilverarrowCardContent.Trailblazer
            };
            var cards = SilverarrowCardContent.DealtDefinitions
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
                SilverarrowCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
