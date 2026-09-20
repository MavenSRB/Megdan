using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's Smoke Bomb - one of her nineteen printed faces, composed at milestone
    /// 8.61a because it is the face whose clauses this block owns. The other eighteen belong to the
    /// blocks that own their clauses, and 8.61i states the whole set under one real content revision.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_MIN.md</c>'s printed
    /// inventory, read off the extracted front <c>min2__7348748</c> at milestone 8.61: Silver band,
    /// Basic, Initiative <b>12</b>, Defense <b>1</b>, <b>no Movement icon at all</b>, primary BASIC
    /// SKILL with a Radius <b>3</b> icon, no item. MI-05 is the rule the absent Movement follows - "A
    /// missing icon is absent, never zero" - so the definition below simply does not declare a
    /// Movement action, exactly as Rowenna's Throw the Gauntlet does not.</para>
    ///
    /// <para><b>Nothing here is registered</b>, and nothing can deal it: see
    /// <see cref="MinComponentContent"/>'s own remarks for why the gate is the point rather than an
    /// oversight. The revision is <see cref="ContentRevision"/>, the one real string milestone 8.61i
    /// minted for her whole set when it became whole.</para>
    /// </summary>
    public static partial class MinCardContent
    {
        /// <summary>
        /// Her Silver basic: "Place a [Smoke bomb] smoke bomb token in radius; enemy heroes cannot
        /// target you or another unit if the smoke bomb token is on a straight line between that enemy
        /// hero and their target."
        /// </summary>
        public static CardDefinitionId SmokeBomb { get; } = new CardDefinitionId("min-smoke-bomb");

        /// <summary>
        /// The printed text, transcribed. The bracketed glyph is the printed [Smoke bomb] icon, written
        /// the way this project writes every token glyph in a transcription.
        /// </summary>
        public const string SmokeBombText =
            "Place a [Smoke bomb] smoke bomb token in radius; enemy heroes cannot target you or " +
            "another unit if the smoke bomb token is on a straight line between that enemy hero and " +
            "their target.";

        private static readonly Lazy<CardDefinition> LazySmokeBomb =
            new Lazy<CardDefinition>(BuildSmokeBomb);

        /// <summary>
        /// The one definition this block composes, which milestone 8.61i's
        /// <see cref="AllDefinitions"/> reads in her shard's inventory order. The instance behind it is
        /// built once, so two readers of the same face compare the same object.
        /// </summary>
        public static CardDefinition SmokeBombDefinition() => LazySmokeBomb.Value;

        private static CardDefinition BuildSmokeBomb() => new CardDefinition(
            SmokeBomb,
            ContentRevision,
            "Smoke Bomb",
            new InitiativeValue(12),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryBasicSkill,
                    ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    MinEffectContent.SmokeBomb(SmokeBomb, ContentRevision)),

                // p.14's Defense action: the ribbon icon, suppressed card text, and the p.13 discard
                // every reaction performs. Her Movement ribbon is ABSENT (MI-05), so there is no
                // Movement action here to declare with a zero.
                new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(SmokeBombText));
    }
}
