using System;
using System.Collections.Generic;
using System.Linq;
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
    /// oversight. The revision is <see cref="MinComponentContent.PrepRevision"/>, a deliberately
    /// malformed placeholder that 8.61i replaces with the one real string every face of hers carries.</para>
    /// </summary>
    public static partial class MinCardContent
    {
        /// <summary>
        /// Her Silver basic: "Place a [Smoke bomb] smoke bomb token in radius; enemy heroes cannot
        /// target you or another unit if the smoke bomb token is on a straight line between that enemy
        /// hero and their target."
        /// </summary>
        public static CardDefinitionId SmokeBomb { get; } = new CardDefinitionId("min-smoke-bomb");

        /// <summary>p.9's Silver band with a Skill primary - "BASIC SKILL".</summary>
        public static ActionDefinitionId PrimaryBasicSkill { get; } =
            new ActionDefinitionId("primary-basic-skill");

        /// <summary>p.9's Defense ribbon icon, which every face printing one declares as a reaction.</summary>
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// The printed text, transcribed. The bracketed glyph is the printed [Smoke bomb] icon, written
        /// the way this project writes every token glyph in a transcription.
        /// </summary>
        public const string SmokeBombText =
            "Place a [Smoke bomb] smoke bomb token in radius; enemy heroes cannot target you or " +
            "another unit if the smoke bomb token is on a straight line between that enemy hero and " +
            "their target.";

        /// <summary>
        /// Her faces as this block leaves them: <b>one</b>, not nineteen.
        ///
        /// <para><b>Why the member exists in the first block rather than in the one that owns the
        /// set.</b> Seven Edit Mode sweeps reflect over every <c>*CardContent</c> class this assembly
        /// declares and assert that each exposes <c>AllDefinitions</c> - "or this sweep cannot see its
        /// faces" - so the moment a class of that name exists, it owes them a list or a whole suite
        /// goes silently uncovered. Rowenna's own first block, 8.57a, did exactly this and said so in
        /// as many words: "AllDefinitions is therefore five cards and not nineteen".</para>
        ///
        /// <para><b>What a later block of hers does with it.</b> A block composing more of her faces
        /// adds a <c>yield return</c> to <see cref="ComposedFaces"/> rather than a second
        /// <c>AllDefinitions</c>, which would not compile beside this one. 8.61i replaces both with
        /// the whole nineteen under one real content revision, and mints
        /// <c>DealtDefinitions</c> - deliberately absent here, because a class that declares it is
        /// asserting a dealt set the artwork catalog is then entitled to ask about.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>The one face of hers this block composes; see <see cref="AllDefinitions"/>.</summary>
        private static IEnumerable<CardDefinition> ComposedFaces()
        {
            yield return SmokeBombDefinition();
        }

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(
                () => Array.AsReadOnly(
                    // Until 8.61i mints the real aggregate, every parallel block appends its own
                    // public list here so the corpus sweeps read the whole set composed so far.
                    ComposedFaces().Concat(BorrowedRiderDefinitions)
                        .Concat(GrenadeCards().Select(GrenadeDefinition)).ToArray()));

        private static readonly Lazy<CardDefinition> LazySmokeBomb =
            new Lazy<CardDefinition>(BuildSmokeBomb);

        /// <summary>
        /// The one definition this block composes. It is a method rather than a bare property because
        /// 8.61i owns how her set is exposed; the instance behind it is built once, so two readers of
        /// the same face compare the same object.
        /// </summary>
        public static CardDefinition SmokeBombDefinition() => LazySmokeBomb.Value;

        private static CardDefinition BuildSmokeBomb() => new CardDefinition(
            SmokeBomb,
            MinComponentContent.PrepRevision,
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
                    MinEffectContent.SmokeBomb(SmokeBomb, MinComponentContent.PrepRevision)),

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
