using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Misa hand took. Milestone 8.35z38, and the
    /// same choice <see cref="DodgerUpgradeBranch"/> and <see cref="XargathaUpgradeBranch"/> name for
    /// the reason they name it: rulebook p.15 offers a pair at each Tier and the player takes one, so a
    /// single seat can only ever hold three of a Tier's six cards, and the untaken sibling becomes
    /// p.16's item.
    /// </summary>
    public enum MisaUpgradeBranch
    {
        /// <summary>Matter of Honor, Gust of Wind, Discipline - and their Tier III successors.</summary>
        FirstSibling = 0,

        /// <summary>Power Shot, Dash and Slash, Living Tornado - and their Tier III successors.</summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Misa's setup as it stands after rulebook p.15 has upgraded her - one seat's hand at Tier II or
    /// Tier III, staged directly for a fixture. Milestone 8.35z38, on the terms
    /// <see cref="WaspProgressedHands"/> records and the six sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations
    /// the page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>Her two branches divide her set along the RULINGS rather than along a role, and that is
    /// what makes both of them matter to the gate.</b> Every ruling the user settled for Misa is
    /// observable on exactly one branch above Tier I, and the split is clean:</para>
    ///
    /// <list type="bullet">
    /// <item><description>The FIRST branch carries RULING 31's escalating free move (Discipline's two
    /// spaces, Mastery's three) and RULING 29's place-the-unit-you-moved-through rider (Gust of Wind,
    /// Crushing Squall). It is the seat that closes, absorbs and rearranges what it passed.</description></item>
    /// <item><description>The SECOND branch carries RULING 32's whole tense pair - Power Shot's "was at
    /// maximum range" and Thunder Shot's "is not adjacent", which exist as a pair and cannot be read
    /// apart - RULING 29's discard rider (Dash and Slash, Death from Above), and both Swap Skills
    /// (Living Tornado's maximum-radius pair, Storm Spirit's equal-distance one). It is the seat that
    /// fights at range and moves other people's units.</description></item>
    /// </list>
    ///
    /// <para>So a first-branch Misa has no ranged Attack above Tier I and no Swap at all, and a
    /// second-branch Misa never raises her Defense past Challenge Accepted's +2. That is content rather
    /// than a fixture convenience, and it is why every journey names the branch it needs rather than
    /// letting one branch stand for the other.</para>
    ///
    /// <para>The two basics kept in hand are Watch How I Soar and Swoop In. Leaf on the Wind is p.20's
    /// Handicap card and is already out of the box at p.6's setup, which is why
    /// <see cref="MisaCardContent.DealtDefinitions"/> is what these hands are built from.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class MisaProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            MisaCardContent.MatterOfHonor, MisaCardContent.GustOfWind,
            MisaCardContent.Discipline
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            MisaCardContent.PowerShot, MisaCardContent.DashAndSlash,
            MisaCardContent.LivingTornado
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            MisaCardContent.WorthyOpponent, MisaCardContent.CrushingSquall,
            MisaCardContent.Mastery
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            MisaCardContent.ThunderShot, MisaCardContent.DeathFromAbove,
            MisaCardContent.StormSpirit
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, MisaUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == MisaUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// Power Overwhelming is dealt to the deck here, exactly as p.6's setup and the initial-setup
        /// validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, MisaUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == MisaUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "Misa's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                MisaCardContent.WatchHowISoar,
                MisaCardContent.SwoopIn
            };
            var cards = MisaCardContent.DealtDefinitions
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
                MisaCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
