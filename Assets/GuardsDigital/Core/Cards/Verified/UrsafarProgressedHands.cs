using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Ursafar hand took. Milestone
    /// 8.35z50, and the same choice <see cref="BroganUpgradeBranch"/> and
    /// <see cref="MisaUpgradeBranch"/> name for the reason they name it: rulebook p.15 offers a pair at
    /// each Tier and the player takes one, so a single seat can only ever hold three of a Tier's six
    /// cards, and the untaken sibling becomes p.16's item.
    /// </summary>
    public enum UrsafarUpgradeBranch
    {
        /// <summary>Prey Abundance, Rampaging Beast, Eyes on the Prey - and their Tier III successors.</summary>
        FirstSibling = 0,

        /// <summary>Rip, Cold Ire, Instinctive Reaction - and their Tier III successors.</summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Ursafar's setup as it stands after rulebook p.15 has upgraded him - one seat's hand at Tier II
    /// or Tier III, staged directly for a fixture. Milestone 8.35z50, on the terms
    /// <see cref="WaspProgressedHands"/> records and the seven sets after it repeat.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations
    /// the page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>The two branches divide his set along the RULINGS rather than along a role, and that is
    /// what makes both of them matter to the gate.</b></para>
    ///
    /// <list type="bullet">
    /// <item><description>The FIRST branch carries Feeding Frenzy - the only face composing the
    /// two-minion arm of the target-survived removal this milestone built - and Apex Predator's
    /// discard-or-defeat escalation. It is the seat that clears the board.</description></item>
    /// <item><description>The SECOND branch carries Tear (RULING 35's Tier + 1 Life-counter spend,
    /// including the game-ending arm), Cold Ire's Movement raise, and both borrow-bearing Greens -
    /// Instinctive Reaction's "choose one" and Evolutionary Response's "choose one, or both". It is the
    /// seat that reaches THE UNIVERSAL BORROW RULE and the hero's signature mechanic.</description></item>
    /// </list>
    ///
    /// <para>So a first-branch Ursafar never borrows and never touches a Life counter, and a
    /// second-branch Ursafar removes at most one minion at a time (Prey Drive/Prey Abundance, both
    /// branches' Tier I/II Red) and never escalates a discard to a defeat. Every journey names the
    /// branch it needs rather than letting one branch stand for the other.</para>
    ///
    /// <para>The two basics kept in hand are Claws That Catch and Angry Roar. Jaws That Bite is p.20's
    /// Handicap card and is already out of the box at p.6's setup, which is why
    /// <see cref="UrsafarCardContent.DealtDefinitions"/> is what these hands are built from.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class UrsafarProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            UrsafarCardContent.PreyAbundance, UrsafarCardContent.RampagingBeast,
            UrsafarCardContent.EyesOnThePrey
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            UrsafarCardContent.Rip, UrsafarCardContent.ColdIre,
            UrsafarCardContent.InstinctiveReaction
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            UrsafarCardContent.FeedingFrenzy, UrsafarCardContent.UnstoppableForce,
            UrsafarCardContent.ApexPredator
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            UrsafarCardContent.Tear, UrsafarCardContent.EyesOfFlame,
            UrsafarCardContent.EvolutionaryResponse
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, UrsafarUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == UrsafarUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand.
        ///
        /// Unbound Fury is dealt to the deck here, exactly as p.6's setup and the initial-setup
        /// validator require, and the configuration factory then unlocks it through
        /// <c>CardLifecycleAuthority.TryUnlockUltimateFromDeck</c> - the one p.16 transition that may
        /// put a card in the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, UrsafarUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == UrsafarUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "Ursafar's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                UrsafarCardContent.ClawsThatCatch,
                UrsafarCardContent.AngryRoar
            };
            var cards = UrsafarCardContent.DealtDefinitions
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
                UrsafarCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
