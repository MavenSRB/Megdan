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
    /// <para><b>The upgrade ladder is stated only as far as this block owns it.</b> p.15 makes Death
    /// Grenade upgrade into either face of Blue Tier III - Mine Field or Holy Death Grenade - and only
    /// the second of those is composed here; Mine Field is 8.61d's face and naming its definition ID
    /// from this file would be this block reaching into another's. 8.61i composes the whole set and is
    /// where the pair of candidates is completed; nothing about the two definitions below changes when
    /// it does.</para>
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

        /// <summary>
        /// The Skill primary both faces print. The identity string is every earlier set's - an action
        /// identity is local to its card - and only the C# member name is this block's, so five partial
        /// files over one class never collide on it.
        /// </summary>
        public static ActionDefinitionId GrenadePrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");

        /// <summary>The Movement ribbon both faces print, whose card text p.11 suppresses.</summary>
        public static ActionDefinitionId GrenadeSecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        /// <summary>The Defense ribbon both faces print.</summary>
        public static ActionDefinitionId GrenadeSecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

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

        // MERGE NOTE (8.61f -> main): this block's faces join the class aggregate declared in
        // MinCardContent.SmokeBomb.cs (LazyAllDefinitions concatenates GrenadeCards()) until 8.61i
        // mints the real set; the per-block AllDefinitions this partial carried on its branch is gone.

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
                        // 8.61i adds Mine Field, the sibling Tier III this block does not own.
                        HolyDeathGrenade));
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
                MinComponentContent.GrenadeRevision,
                displayName,
                new InitiativeValue(GrenadeInitiative),
                CardContentAuthority.VerifiedOfficial,
                progression,
                new[]
                {
                    new ActionDefinition(
                        GrenadePrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, GrenadeRadius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        MinEffectContent.Grenade(cardId, MinComponentContent.GrenadeRevision, holy)),
                    new ActionDefinition(
                        GrenadeSecondaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Movement, GrenadeSecondaryMovementValue)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        GrenadeSecondaryDefense,
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
