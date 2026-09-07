using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Dodger hand took. Milestone 8.35z34, and the
    /// same choice <see cref="XargathaUpgradeBranch"/> names for the reason it names it: rulebook p.15
    /// offers a pair at each Tier and the player takes one, so a single seat can only ever hold three
    /// of a Tier's six cards, and the untaken sibling becomes p.16's item.
    /// </summary>
    public enum DodgerUpgradeBranch
    {
        /// <summary>Finger of Death, Vampiric Shield, Darker Ritual - and their Tier III successors.</summary>
        FirstSibling = 0,

        /// <summary>Burning Skull, Weakness, Necromancy - and their Tier III successors.</summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Dodger's setup as it stands after rulebook p.15 has upgraded her - one seat's hand at Tier II or
    /// Tier III, staged directly for a fixture. Milestone 8.35z34, on the terms
    /// <see cref="WaspProgressedHands"/> records and the five sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations
    /// the page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>Her two branches divide her kit along a line no earlier hero's do: the second branch is
    /// the only one that can bring a minion back.</b> The first takes the escalating single-target
    /// Attacks, the near-unconditional Defense and the item-granting ritual - a seat that outlasts and
    /// out-values. The second takes the two drains and both Respawns - a seat that reduces what enemies
    /// hit for and rebuilds its own side's wave. Necromancy and Necromastery are Green siblings of
    /// Darker and Darkest Ritual, so a first-branch Dodger has no Respawn in her hand at all, and every
    /// journey that drives p.19's Respawn therefore names the second branch. That is content rather
    /// than a fixture convenience, and it is why both branches matter to the gate rather than one
    /// standing for the other.</para>
    ///
    /// <para>The two basics kept in hand are Dread Razor and Death Trap. Rusty Razor is p.20's Handicap
    /// card and is already out of the box at p.6's setup, which is why
    /// <see cref="DodgerCardContent.DealtDefinitions"/> is what these hands are built from.</para>
    ///
    /// <para><b>Every card in both branches now composes.</b> That was not true when this file was
    /// written: the first branch's Middlefinger of Death was parked on the attack-sequence bullet's
    /// subject narrowing, so a staged first-branch Tier III hand held a face a player could not
    /// resolve. Milestone 8.35z45 built that narrowing and composed the face, and the
    /// player-path gate's row moved with it - from an unreachable face to one proved by its own
    /// journey, because "one or both" is a different set of controls from the "Choose one" modals her
    /// other Red faces print.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class DodgerProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            DodgerCardContent.FingerOfDeath, DodgerCardContent.VampiricShield,
            DodgerCardContent.DarkerRitual
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            DodgerCardContent.BurningSkull, DodgerCardContent.Weakness,
            DodgerCardContent.Necromancy
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            DodgerCardContent.MiddlefingerOfDeath, DodgerCardContent.AegisOfDoom,
            DodgerCardContent.DarkestRitual
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            DodgerCardContent.BlazingSkull, DodgerCardContent.Enfeeblement,
            DodgerCardContent.Necromastery
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, DodgerUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == DodgerUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// Tide of Darkness is dealt to the deck here, exactly as p.6's setup and the initial-setup
        /// validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, DodgerUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == DodgerUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "Dodger's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                DodgerCardContent.DreadRazor,
                DodgerCardContent.DeathTrap
            };
            var cards = DodgerCardContent.DealtDefinitions
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
                DodgerCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
