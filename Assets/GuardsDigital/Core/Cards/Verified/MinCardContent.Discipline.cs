using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Three of Min the Dragonmonk's nineteen printed faces as definition content - Inner Strength,
    /// Perfect Self and Flurry of Blows, milestone 8.61h.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier and item icon below
    /// is transcribed from the extracted card faces at milestone 8.61's inventory pass and recorded in
    /// the <c>Docs/AI/Rules/HERO_MIN.md</c> shard. The table there and the rows here are meant to be
    /// read side by side. Nothing is derived.</para>
    ///
    /// <para><b>MI-05 is why two columns are absent rather than zero.</b> Inner Strength and Perfect
    /// Self print no Range and no Radius icon at all - their Skills reach nothing at a distance,
    /// because neither sentence names a unit - so those actions carry no printed value beside their
    /// kind. Flurry of Blows prints no Initiative, no icons and no action whatsoever, which is what
    /// makes its whole row a statement rather than an omission: it is never played and never discarded
    /// to defend, and the only content it carries is p.16's permanent ability.</para>
    ///
    /// <para><b>The Green ladder is whole here since milestone 8.61i.</b> Inner Strength is a Green
    /// Tier II that upgrades to Ruse or Perfect Self; Ruse is composed by a sibling block, so while
    /// the two branches ran in parallel this row named no next-Tier candidate at all - naming an id
    /// this file does not declare would have been a compile dependency between two partials being
    /// written at the same time - and the block that composed her set stated the pair.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <c>MatchContentCatalogs</c> names no Min content, so no
    /// configured match deals these cards. That is milestone 7.9's gate: the registered content list
    /// is serialized, and registering a card before it can be dealt moves the pinned list, every
    /// snapshot's bytes and every corpus digest while the content never moved. Milestone 8.61j is the
    /// block that flips it.</para>
    /// </summary>
    public static partial class MinCardContent
    {
        /// <summary>
        /// Her printed Green Tier II whose whole text is the doubling - the first face in the corpus
        /// to change rulebook p.16's own arithmetic.
        /// </summary>
        public static CardDefinitionId InnerStrength { get; } = DisciplineId("inner-strength");

        /// <summary>
        /// The Green Tier III that prints the doubling again beside a second bullet, and the only face
        /// in the corpus that takes a card out of its own deck and makes it an item.
        /// </summary>
        public static CardDefinitionId PerfectSelf { get; } = DisciplineId("perfect-self");

        /// <summary>Her Purple Tier IV: p.16's permanent ability, and nothing else.</summary>
        public static CardDefinitionId FlurryOfBlows { get; } = DisciplineId("flurry-of-blows");

        /// <summary>
        /// The three definitions this block composes, in the shard's own row order. Milestone 8.61i is
        /// what assembles the nineteen; this list exists so a fixture can reach exactly these.
        ///
        /// <para><b>Built lazily since 8.61i, and the reason was measured rather than assumed.</b>
        /// Completing p.15's Green ladder made this list name <c>Ruse</c>, an id another partial of
        /// this class declares - and a static field initializer reading one across a partial boundary
        /// reads whatever the compiler had reached, which here was an UNDEFINED definition id and a
        /// <c>TypeInitializationException</c> out of the first case that touched her set.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> DisciplineDefinitions =>
            LazyDisciplineDefinitions.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDisciplineDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                // min15 - Green II, Initiative 2, Defense 2, Movement 3, Skill with no reach icon,
                // Attack item.
                BuildDisciplineSkill(
                    InnerStrength,
                    "Inner Strength",
                    initiative: 2,
                    secondaryDefense: 2,
                    secondaryMovement: 3,
                    // p.15's fork, whole since 8.61i: Ruse is 8.61b's face and Perfect Self is this
                    // block's, and a Tier II names both of its colour's Tier III faces.
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                        Ruse, PerfectSelf),
                    printedText: "This round: Double your item bonuses.",
                    skillEffect: MinEffectContent.InnerStrengthSkillEffect(ContentRevision)),

                // min17 - Green III, Initiative 1, Defense 3, Movement 3, Skill with no reach icon,
                // Range item.
                BuildDisciplineSkill(
                    PerfectSelf,
                    "Perfect Self",
                    initiative: 1,
                    secondaryDefense: 3,
                    secondaryMovement: 3,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Green, CardTier.III, PrintedValueChannel.Range),
                    printedText:
                        "Choose one or both - (1) This round: Double your item bonuses. (2) Take a " +
                        "Tier II card from your deck and add it to your dashboard as a permanent item.",
                    skillEffect: MinEffectContent.PerfectSelfSkillEffect(ContentRevision)),

                // min18 - Purple Tier IV. No Initiative, no icons, no action: the whole card is the
                // permanent ability.
                new CardDefinition(
                    FlurryOfBlows,
                    ContentRevision,
                    "Flurry of Blows",
                    default,
                    CardContentAuthority.VerifiedOfficial,
                    CardProgressionProfile.UltimateCard(),
                    Array.Empty<ActionDefinition>(),
                    MinEffectContent.FlurryOfBlowsRule(ContentRevision),
                    CardSourceContent.Transcribed(
                        "Each time after you perform an attack action you may repeat it once on a " +
                        "different target."))
            }));

        /// <summary>
        /// One Green Skill face, built the one way both of this block's composed cards are built: the
        /// Skill primary that carries the text, the Movement ribbon whose own text is suppressed, and
        /// p.14's Defense reaction spent by discarding the card.
        /// </summary>
        private static CardDefinition BuildDisciplineSkill(
            CardDefinitionId id,
            string displayName,
            int initiative,
            int secondaryDefense,
            int secondaryMovement,
            CardProgressionProfile progression,
            string printedText,
            EffectDefinition skillEffect) =>
            new CardDefinition(
                id,
                ContentRevision,
                displayName,
                new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                progression,
                new[]
                {
                    // MI-05: neither face prints a Range or a Radius icon, so the primary carries no
                    // printed value at all. A missing icon is absent, never zero.
                    new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        skillEffect),

                    new ActionDefinition(
                        SecondaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Movement, secondaryMovement)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

                    new ActionDefinition(
                        SecondaryDefense,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Defense, secondaryDefense)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                },
                null,
                CardSourceContent.Transcribed(printedText));

        /// <summary>
        /// Her content-set identity spelled into a definition id - the same <c>min-</c> prefix every
        /// block of the series uses, which milestone 8.61i states once as
        /// <see cref="ContentSetId"/>.
        /// </summary>
        private static CardDefinitionId DisciplineId(string cardId) => Id(cardId);
    }
}
