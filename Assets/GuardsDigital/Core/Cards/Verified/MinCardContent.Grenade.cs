using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.61f. Min the Dragonmonk's two Grenade faces as printed definitions - her Blue Tier
    /// II Death Grenade and the Blue Tier III it upgrades into, Holy Death Grenade.
    ///
    /// <para><b>The values are the shard's table, transcribed.</b> Death Grenade
    /// (<c>min10__7571085</c>): Initiative 11, secondary Defense 4, secondary Movement 2, primary
    /// Skill with Radius 2, Item icon Initiative. Holy Death Grenade (<c>min12__7571084</c>): the same
    /// card with a Defense of 5 and Tier III's classification. See <c>Docs/AI/Rules/HERO_MIN.md</c>,
    /// MI-01.</para>
    ///
    /// <para><b>The Blue ladder is whole here since milestone 8.61i.</b> p.15 makes Death Grenade
    /// upgrade into either face of Blue Tier III - Mine Field or Holy Death Grenade - and only the
    /// second of those is composed in this file; Mine Field is 8.61d's face, so while the two branches
    /// ran in parallel this pair named one candidate, and the block that composed her set completed
    /// it. Nothing else about the two definitions below moved when it did.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <c>MatchContentCatalogs</c> names no Min content, so no
    /// configured match can deal these two cards and nothing reachable can start them - milestone 7.9's
    /// gate, not an oversight.</para>
    /// </summary>
    public static partial class MinCardContent
    {
        /// <summary>Blue Tier II. "Place a [Grenade] Grenade token into a space in radius. ..."</summary>
        public static CardDefinitionId DeathGrenade { get; } =
            new CardDefinitionId("min-death-grenade");

        /// <summary>Blue Tier III. Death Grenade's three sentences with "Up to 2 enemy heroes".</summary>
        public static CardDefinitionId HolyDeathGrenade { get; } =
            new CardDefinitionId("min-holy-death-grenade");

        /// <summary>Death Grenade's printed text, transcribed whole.</summary>
        public const string DeathGrenadeText =
            "Place a [Grenade] Grenade token into a space in radius. End of turn: Up to 1 enemy hero " +
            "adjacent to that token discards a card, or is defeated. Remove the Grenade token.";

        /// <summary>Holy Death Grenade's printed text, transcribed whole.</summary>
        public const string HolyDeathGrenadeText =
            "Place a [Grenade] Grenade token into a space in radius. End of turn: Up to 2 enemy heroes " +
            "adjacent to that token discard a card, or are defeated. Remove the Grenade token.";

        /// <summary>The two faces this block composes.</summary>
        public static IEnumerable<CardDefinitionId> GrenadeCards() =>
            new[] { DeathGrenade, HolyDeathGrenade };

        /// <summary>One of the two printed definitions, by ID.</summary>
        public static CardDefinition GrenadeDefinition(CardDefinitionId cardId)
        {
            if (cardId.Equals(DeathGrenade))
            {
                return Compose(
                    DeathGrenade, "Death Grenade", secondaryDefense: 4,
                    printedText: DeathGrenadeText, holy: false,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Blue, CardTier.II, PrintedValueChannel.Initiative,
                        // p.15's fork, whole since 8.61i: Mine Field is 8.61d's face and Holy Death
                        // Grenade is this block's.
                        MineField, HolyDeathGrenade));
            }

            if (cardId.Equals(HolyDeathGrenade))
            {
                return Compose(
                    HolyDeathGrenade, "Holy Death Grenade", secondaryDefense: 5,
                    printedText: HolyDeathGrenadeText, holy: true,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Blue, CardTier.III, PrintedValueChannel.Initiative));
            }

            throw new ArgumentOutOfRangeException(
                nameof(cardId), cardId, "Milestone 8.61f composes the two Grenade faces and no other.");
        }

        /// <summary>
        /// Both faces print the same three icons and differ only in the Defense ribbon and in how many
        /// heroes their text reaches, so one composition writes both: a Skill primary carrying p.9's
        /// Radius 2 and the card's effect body, p.11's suppressed Movement ribbon, and the Defense
        /// ribbon p.14 discards the card for.
        /// </summary>
        private static CardDefinition Compose(
            CardDefinitionId cardId,
            string displayName,
            int secondaryDefense,
            string printedText,
            bool holy,
            CardProgressionProfile progression) =>
            new CardDefinition(
                cardId,
                ContentRevision,
                displayName,
                new InitiativeValue(GrenadeInitiative),
                CardContentAuthority.VerifiedOfficial,
                progression,
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, GrenadeRadius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        MinEffectContent.Grenade(cardId, ContentRevision, holy)),
                    new ActionDefinition(
                        SecondaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Movement, GrenadeSecondaryMovementValue)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        SecondaryDefense,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Defense, secondaryDefense)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                },
                CardSourceContent.Transcribed(printedText));

        /// <summary>Both faces print Initiative 11.</summary>
        private const int GrenadeInitiative = 11;

        /// <summary>Both faces print Radius 2 - the reach "in radius" names, before p.16's items.</summary>
        private const int GrenadeRadius = 2;

        /// <summary>Both faces print a Movement ribbon of 2.</summary>
        private const int GrenadeSecondaryMovementValue = 2;
    }
}
