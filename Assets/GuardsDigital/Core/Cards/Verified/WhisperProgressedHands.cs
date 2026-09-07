using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Whisper hand took. Milestone
    /// 8.35z53, and the same choice <see cref="MisaUpgradeBranch"/> and
    /// <see cref="DodgerUpgradeBranch"/> name for the reason they name it: rulebook p.15 offers a pair
    /// at each Tier and the player takes one, so a single seat can only ever hold three of a Tier's six
    /// cards, and the untaken sibling becomes p.16's item.
    /// </summary>
    public enum WhisperUpgradeBranch
    {
        /// <summary>Sanguine Path, Sprouts of Panic, Shadow Walk - and their Tier III successors.</summary>
        FirstSibling = 0,

        /// <summary>Blood Fury, Lesser Evil, Cruel Twist - and their Tier III successors.</summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Whisper's setup as it stands after rulebook p.15 has upgraded her - one seat's hand at Tier II
    /// or Tier III, staged directly for a fixture. Milestone 8.35z53, on the
    /// terms <see cref="WaspProgressedHands"/> records and the seven sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations
    /// the page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>Her two branches divide her set along the RULINGS rather than along a role, and that is
    /// what makes both of them matter to the gate.</b> Whisper's upgrade pairs are unusually clean -
    /// each of her three Tier I cards names two siblings, and the two siblings of one colour are two
    /// DIFFERENT SENTENCES rather than the same sentence with a bigger number, which is the shape most
    /// of the roster has. So the split is not a convenience:</para>
    ///
    /// <list type="bullet">
    /// <item><description>The FIRST branch is the seat that MOVES. Sanguine Path and Blood Pilgrimage
    /// buy their step out of the board's own empty minion Spawn points (RULING 19's count, with a
    /// printed ceiling of two and then three); Sprouts of Panic and Blooming Nightmare push an enemy
    /// off a Spawn point two spaces and then three; Shadow Walk and Creeping Shadow place HER, at Range
    /// 4, into a Spawn point and then beside one. Nothing on this branch hands a decision to anybody
    /// else.</description></item>
    /// <item><description>The SECOND branch is the seat that hands decisions AWAY and exchanges places.
    /// Blood Fury and Blood Frenzy carry RULING 34's discard-gated repeat chain - the whole of it, and
    /// Blood Frenzy is the only face in the roster where "on different targets" can diverge from the
    /// engine's single-slot exclusion; Lesser Evil and Greater Good carry RULING 17's whole subject,
    /// the modal an ENEMY hero chooses; Cruel Twist and Sealed Fate carry the swap-then-step union.
    /// </description></item>
    /// </list>
    ///
    /// <para>So a first-branch Whisper never hands a bullet to an opponent and never swaps, and a
    /// second-branch Whisper never buys a step off the Spawn-point count and never places herself above
    /// Tier I. That is content rather than a fixture convenience, and it is why every journey names the
    /// branch it needs rather than letting one branch stand for the other.</para>
    ///
    /// <para><b>Between the two branches the whole deck is reachable</b>, which is the property a
    /// dealing owes: six Tier II cards and six Tier III cards across four arms, plus Grim Reaper, which
    /// both Tier III arms deal to the deck and the configuration factory then unlocks.</para>
    ///
    /// <para>The two basics kept in hand are Swift Justice and Death Seeker. Vile Accusation is p.20's
    /// Handicap card and is already out of the box at p.6's setup, which is why
    /// <see cref="WhisperCardContent.DealtDefinitions"/> is what these hands are built from.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class WhisperProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            WhisperCardContent.SanguinePath, WhisperCardContent.SproutsOfPanic,
            WhisperCardContent.ShadowWalk
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            WhisperCardContent.BloodFury, WhisperCardContent.LesserEvil,
            WhisperCardContent.CruelTwist
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            WhisperCardContent.BloodPilgrimage, WhisperCardContent.BloomingNightmare,
            WhisperCardContent.CreepingShadow
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            WhisperCardContent.BloodFrenzy, WhisperCardContent.GreaterGood,
            WhisperCardContent.SealedFate
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, WhisperUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == WhisperUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// Grim Reaper is dealt to the deck here, exactly as p.6's setup and the initial-setup
        /// validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, WhisperUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == WhisperUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "Whisper's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                WhisperCardContent.SwiftJustice,
                WhisperCardContent.DeathSeeker
            };
            var cards = WhisperCardContent.DealtDefinitions
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
                WhisperCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
