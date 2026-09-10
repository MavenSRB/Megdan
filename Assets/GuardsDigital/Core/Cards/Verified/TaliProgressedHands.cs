using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Which of a colour's two upgrade siblings a staged Tali hand took. Milestone
    /// 8.37, on the same terms <see cref="UrsafarUpgradeBranch"/> and its seven
    /// predecessors name theirs.
    ///
    /// <para><b>The split follows the hero's own two identities rather than an arbitrary grouping.</b>
    /// Every one of her three colours prints a Tier I or Tier II card whose two upgrade candidates
    /// read as one clean pair when read together - a raw, self-referential-Attack track and a
    /// modal/retrieval/totem track - and the two branches below take one side of that pair in each
    /// colour consistently, so a staged hand exercises one coherent half of her rather than a mix.</para>
    /// </summary>
    public enum TaliUpgradeBranch
    {
        /// <summary>Winter Spear/Scepter, Snowstorm, Wall of Frost/Pack Ice - the Ice-heavy track.</summary>
        FirstSibling = 0,

        /// <summary>Spirit Wolf/Bear, Guardian Spirit, Ancestral/Venerated Totem - the Totem/Spirit track.</summary>
        SecondSibling = 1
    }

    /// <summary>
    /// Tali's setup as it stands after rulebook p.15 has upgraded her - one seat's hand at Tier II or
    /// Tier III, staged directly for a fixture. Milestone 8.37, on the terms
    /// <see cref="WaspProgressedHands"/> and its seven successors record.
    ///
    /// <para>A live match cannot be seeded here: coins are earned only through p.13's Defeat, so the
    /// coin-earning rounds a real progression needs are outside any fixture's budget. What survives
    /// into a composed session is card <em>location</em>, so these hands state exactly the locations
    /// the page's own procedure produces and nothing else.</para>
    ///
    /// <para><b>Milestone 8.37a took the one line milestone 8.37 left named.</b> Blue's Tier III was
    /// Warrior Spirit on BOTH branches while Blizzard stayed deferred - its card definition was whole
    /// and valid to hold, exactly as every deferred face's is, but a staged hand built for journeys was
    /// more useful holding the composed sibling. Blizzard composes now, so the branch that reaches it
    /// takes its own thematic pairing back: Cold Snap upgrades to Snowstorm and Snowstorm to Blizzard,
    /// all three printing the identical displacement sentence, while the second branch keeps Guardian
    /// Spirit into Warrior Spirit and its retrieval. Each branch is now one line of hers end to end,
    /// which is what the pairing was for.</para>
    ///
    /// <para>The two basics kept in hand are Ice Blast and Commune with Spirits. Ice Shard is p.20's
    /// Handicap card and is already out of the box at p.6's setup, which is why
    /// <see cref="TaliCardContent.DealtDefinitions"/> is what these hands are built from.</para>
    ///
    /// <para>`DEV-004` development affordance: fixtures and one-shot scene requests only; a launched
    /// player composes p.6's setup and reaches these positions through p.15 alone.</para>
    /// </summary>
    public static class TaliProgressedHands
    {
        private static readonly CardDefinitionId[] TierTwoFirst =
        {
            TaliCardContent.WinterSpear, TaliCardContent.Snowstorm, TaliCardContent.WallOfFrost
        };

        private static readonly CardDefinitionId[] TierTwoSecond =
        {
            TaliCardContent.SpiritWolf, TaliCardContent.GuardianSpirit, TaliCardContent.AncestralTotem
        };

        private static readonly CardDefinitionId[] TierThreeFirst =
        {
            TaliCardContent.WinterScepter, TaliCardContent.Blizzard, TaliCardContent.PackIce
        };

        private static readonly CardDefinitionId[] TierThreeSecond =
        {
            TaliCardContent.SpiritBear, TaliCardContent.WarriorSpirit, TaliCardContent.VeneratedTotem
        };

        /// <summary>One seat's Tier II setup: the two basics plus its three Tier II cards in hand.</summary>
        public static ConfiguredHand CreateTierTwoHand(
            HeroId ownerId, string instanceIdPrefix, TaliUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == TaliUpgradeBranch.FirstSibling ? TierTwoFirst : TierTwoSecond);

        /// <summary>
        /// One seat's Tier III setup: the two basics plus its three Tier III cards in hand. No Ultimate
        /// unlock is performed here - the caller does that, exactly as
        /// <see cref="UrsafarProgressedHands.CreateTierThreeHand"/>'s own remarks describe, because
        /// only the configuration factory holds the authority that moves a card into the Ultimate area.
        /// </summary>
        public static ConfiguredHand CreateTierThreeHand(
            HeroId ownerId, string instanceIdPrefix, TaliUpgradeBranch branch) =>
            Create(
                ownerId,
                instanceIdPrefix,
                branch == TaliUpgradeBranch.FirstSibling ? TierThreeFirst : TierThreeSecond);

        private static ConfiguredHand Create(
            HeroId ownerId,
            string instanceIdPrefix,
            IReadOnlyList<CardDefinitionId> upgraded)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException(
                    "Tali's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat.", nameof(instanceIdPrefix));
            }

            var inHand = new HashSet<CardDefinitionId>(upgraded)
            {
                TaliCardContent.IceBlast,
                TaliCardContent.CommuneWithSpirits
            };
            var cards = TaliCardContent.DealtDefinitions
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
                TaliCardContent.DealtDefinitions,
                cards.Where(card => card.State.Location != CardLocation.Deck),
                cards.Where(card => card.State.Location == CardLocation.Deck));
        }
    }
}
