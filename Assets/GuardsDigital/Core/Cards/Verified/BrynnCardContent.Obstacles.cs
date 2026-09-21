using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's four faces whose clauses milestone 8.69a owns: Familiar Ground, High Ground,
    /// Elevated Ambush and Over the Top. The other fifteen belong to the blocks that own their clauses,
    /// and 8.69f states the whole set under one real content revision.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_BRYNN.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.69: Familiar Ground
    /// (<c>brynn__7690719</c>) Gold, Basic, Initiative <b>12</b>, Defense <b>2</b>, Movement <b>1</b>,
    /// primary BASIC ATTACK - RANGED with Attack <b>2</b> and Range <b>3</b>, no item; High Ground
    /// (<c>brynn3__7696235</c>) Red Tier I, 9/6/4, Attack <b>4+</b>, no item; Elevated Ambush
    /// (<c>brynn4__7696236</c>) Red Tier II, 9/6/4, Attack <b>5+</b>, Initiative item; Over the Top
    /// (<c>brynn18__7550498</c>) Purple Tier IV, no values at all. BR-06 is the rule the absent icons
    /// follow - "A missing icon is absent, never zero" - so the two Reds declare no Range or Radius and
    /// the Ultimate declares no action.</para>
    ///
    /// <para><b>The printed "+" is the contribution and not a second number.</b> p.14 states the
    /// notation outright ("The '+' sign signifies that the value can be increased by the card text"),
    /// so High Ground prints Attack 4 and Elevated Ambush Attack 5, each with one
    /// <see cref="RosterValueContributionDefinition"/> of +2 on the Attack channel - Tigerclaw's
    /// Backstab shape, and BR-Q6's reading of when it is read.</para>
    ///
    /// <para><b>Two of the three progression pairs cross a block boundary and are therefore left
    /// unstated here.</b> p.15 forks Red twice - High Ground climbs to Elevated Ambush or Split Attack,
    /// and either of those to Peak Precision or Split Throw - and Split Attack, Split Throw and Peak
    /// Precision are 8.69b's faces. Naming an id a sibling branch owns is what Min's series refused
    /// four times over, so High Ground names only the half this block composes and Elevated Ambush
    /// names none; 8.69f states both ladders whole.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's gate
    /// rather than an oversight - see <see cref="BrynnComponentContent"/>'s own remarks. The revision
    /// is <see cref="BrynnComponentContent.PrepRevision"/>, a deliberately malformed placeholder that
    /// 8.69f replaces with the one real string every face of his carries.</para>
    /// </summary>
    public static partial class BrynnCardContent
    {
        // -----------------------------------------------------------------------------------------
        // Identities.
        //
        // The four action identities below are LOCAL to a card - every hero in the roster prints
        // "primary-attack" on its Attack faces - so the strings are the corpus's own and only the C#
        // members are his. They are declared by this block because it is the first, on the arrangement
        // Min's 8.61a used: a partial class cannot declare one member twice, so the sibling wave blocks
        // REUSE these rather than restating them, and 8.69f folds in whatever else the set needs.
        // -----------------------------------------------------------------------------------------

        /// <summary>p.9's Gold band with a Ranged Attack primary - "BASIC ATTACK - RANGED".</summary>
        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        /// <summary>p.9's Red band with an Attack primary - five of his faces.</summary>
        public static ActionDefinitionId PrimaryAttack { get; } =
            new ActionDefinitionId("primary-attack");

        /// <summary>p.9's Movement ribbon, whose own card text p.11 suppresses.</summary>
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        /// <summary>p.14's numeric Defense ribbon, spent by discarding the card from hand.</summary>
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// His Gold basic: "Choose one - • Target a unit adjacent to you. • Target a hero in range who
        /// is adjacent to 3 or more obstacles. ( You, other heroes, minions, tokens and terrain are
        /// obstacles )."
        /// </summary>
        public static CardDefinitionId FamiliarGround { get; } =
            new CardDefinitionId("brynn-familiar-ground");

        /// <summary>His Red Tier I: "Target a unit adjacent to you. If you target a hero adjacent to 3
        /// or more obstacles, +2 [Attack] attack."</summary>
        public static CardDefinitionId HighGround { get; } =
            new CardDefinitionId("brynn-high-ground");

        /// <summary>His Red Tier II, printing High Ground's text over a higher Attack.</summary>
        public static CardDefinitionId ElevatedAmbush { get; } =
            new CardDefinitionId("brynn-elevated-ambush");

        /// <summary>His Purple Tier IV: "While you are performing actions all enemy heroes in play
        /// count as adjacent to 3 or more obstacles."</summary>
        public static CardDefinitionId OverTheTop { get; } =
            new CardDefinitionId("brynn-over-the-top");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed.
        // -----------------------------------------------------------------------------------------

        public const string FamiliarGroundText =
            "Choose one - • Target a unit adjacent to you. • Target a hero in range who is adjacent " +
            "to 3 or more obstacles. ( You, other heroes, minions, tokens and terrain are obstacles ).";

        public const string HighGroundText =
            "Target a unit adjacent to you. If you target a hero adjacent to 3 or more obstacles, " +
            "+2 [Attack] attack.";

        /// <summary>Elevated Ambush prints High Ground's sentence exactly; BR-06 records the values.</summary>
        public const string ElevatedAmbushText = HighGroundText;

        public const string OverTheTopText =
            "While you are performing actions all enemy heroes in play count as adjacent to 3 or " +
            "more obstacles.";

        /// <summary>
        /// His faces as this block leaves them: <b>four</b>, not nineteen.
        ///
        /// <para><b>Why the member exists in the first block rather than in the one that owns the
        /// set.</b> Several Edit Mode sweeps reflect over every <c>*CardContent</c> class this assembly
        /// declares and read <c>AllDefinitions</c> off it - so the moment a class of that name exists,
        /// it owes them a list or a whole suite goes silently uncovered. Rowenna's 8.57a and Min's
        /// 8.61a each did exactly this.</para>
        ///
        /// <para><b>What a later block of his does with it.</b> A block composing more of his faces
        /// adds one <c>.Concat(...)</c> line to <see cref="ComposedFaces"/> rather than a second
        /// <c>AllDefinitions</c>, which would not compile beside this one. 8.69f replaces both with the
        /// whole nineteen under one real content revision.</para>
        ///
        /// <para>Lazy, on the convention milestone 8.38d adopted after a static-initialization cycle
        /// cost milestone 8.37 1,423 failing cases: a static field initializer runs in textual order
        /// within one file and in the compiler's order ACROSS the files of a partial class, so an eager
        /// list here would read a sibling partial's ids as undefined.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>
        /// Deliberately EMPTY until 8.69f states his set. <c>DealtCorpusCompositionValidationTests</c>
        /// validates every face this returns against the capabilities the build REGISTERS, and this
        /// block registers nothing at all - so a composed-but-unregistered face inside it would be a
        /// red. The member exists because <c>ConfiguredCardArtworkCatalogTests</c> asks every content
        /// class for one, and an empty list is the honest answer for a hero who deals nothing yet.
        /// </summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        /// <summary>The four faces this block composes, in his shard's inventory order.</summary>
        public static IReadOnlyList<CardDefinition> ObstacleDefinitions => LazyObstacle.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyObstacle =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildFamiliarGround(),
                BuildRaisedAttack(
                    HighGround, "High Ground", CardTier.I, 4, PrintedValueChannel.None,
                    HighGroundText, new[] { ElevatedAmbush }),
                BuildRaisedAttack(
                    ElevatedAmbush, "Elevated Ambush", CardTier.II, 5,
                    PrintedValueChannel.Initiative, ElevatedAmbushText,
                    Array.Empty<CardDefinitionId>()),
                BuildOverTheTop()
            }));

        /// <summary>The blocks' own lists; a sibling adds one <c>.Concat(...)</c> line here.</summary>
        private static IEnumerable<CardDefinition> ComposedFaces() =>
            ObstacleDefinitions.Concat(SwapDefinitions)
                .Concat(TrapAndGritDefinitions);

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(
                () => Array.AsReadOnly(ComposedFaces().ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealtDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(
                () => Array.AsReadOnly(Array.Empty<CardDefinition>()));

        /// <summary>
        /// Familiar Ground. p.9's Gold band with a RANGED Attack primary, so p.13 reads its printed
        /// Range icon; its own text names the targets instead, which is
        /// <see cref="AttackTargetBinding.ByCardText"/> - the action reaches p.14's step 1 with no
        /// target and the modal answers with bullets.
        ///
        /// <para>The text applies BEFORE the attack, because choosing which bullet to take is how the
        /// target is bound at all - Hand Crossbow's timing exactly.</para>
        /// </summary>
        private static CardDefinition BuildFamiliarGround() => new CardDefinition(
            FamiliarGround,
            BrynnComponentContent.PrepRevision,
            "Familiar Ground",
            new InitiativeValue(12),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryRangedAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 2),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 3)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged,
                    default,
                    BrynnEffectContent.FamiliarGround(
                        FamiliarGround, BrynnComponentContent.PrepRevision),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.BeforeTheAttack,
                    PrintedAttackTargetPolicy
                        .ForClassification(AttackRangeKind.Ranged)
                        .BoundByCardText()),

                new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

                new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(FamiliarGroundText));

        /// <summary>
        /// High Ground and Elevated Ambush, which differ in Tier, Attack value, item icon and nothing
        /// else - they print the identical sentence.
        ///
        /// <para><b>They compose no card text at all.</b> "Target a unit adjacent to you" is p.13's own
        /// reading of a non-Ranged Attack, which every Attack definition already carries, and the "+2
        /// attack" is a value on the Attack total rather than a step the page performs - see
        /// <see cref="BrynnEffectContent"/>'s own remarks and BR-Q6.</para>
        /// </summary>
        private static CardDefinition BuildRaisedAttack(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int printedAttack,
            PrintedValueChannel itemIcon,
            string printedText,
            IReadOnlyList<CardDefinitionId> nextTierCandidates) => new CardDefinition(
            id,
            BrynnComponentContent.PrepRevision,
            displayName,
            new InitiativeValue(9),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(
                CardColor.Red, tier, itemIcon, nextTierCandidates),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, printedAttack) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged,
                    default,
                    null,
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.None,
                    null,
                    RaisedAttack()),

                new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

                new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 6) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(printedText));

        /// <summary>
        /// "If you target a hero adjacent to 3 or more obstacles, +2 [Attack] attack" - the printed
        /// plus beside a conditional bonus, as the one shape this build has for one: a contribution
        /// declared on the ACTION and scaled by a reusable board predicate (BR-Q6).
        /// </summary>
        public static IEnumerable<RosterValueContributionDefinition> RaisedAttack() =>
            new[]
            {
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack,
                    RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles,
                    2)
            };

        /// <summary>
        /// Over the Top. p.16's permanent ability: it prints no Initiative, no icons and no action at
        /// all, so it is never played, never discarded to defend, and the whole of its content is the
        /// standing declaration below. It is on no ladder, because p.16 UNLOCKS an Ultimate rather than
        /// upgrading into one.
        /// </summary>
        private static CardDefinition BuildOverTheTop() => new CardDefinition(
            OverTheTop,
            BrynnComponentContent.PrepRevision,
            "Over the Top",
            default,
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.UltimateCard(),
            Array.Empty<ActionDefinition>(),
            UltimateRuleDefinition.PermanentAbility(
                BrynnEffectContent.OverTheTopRuleId,
                BrynnEffectContent.OverTheTopRuleKind,
                BrynnEffectContent.OverTheTopModifications),
            CardSourceContent.Transcribed(OverTheTopText));
    }
}
