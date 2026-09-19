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
    /// <para><b>The upgrade ladder is stated at milestone 8.61i, not here.</b> Inner Strength is a
    /// Green Tier II that upgrades to Ruse or Perfect Self; Ruse is composed by a sibling block, and
    /// naming an id this file does not declare would be a compile dependency between two partials
    /// being written at the same time. Its row therefore names no next-Tier candidate, exactly as a
    /// Tier III's does, and 8.61i - which composes the whole set under one revision - restates both
    /// Green pairs whole.</para>
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
        /// The revision this block's three definitions carry - a deliberate placeholder, and not a
        /// value that could be mistaken for a real content revision, on the project's standing
        /// placeholder rule. Milestone 8.61i mints the one real string every one of her nineteen
        /// definitions and every effect body they reach will carry.
        /// </summary>
        public const string DisciplineRevision = MinComponentContent.DisciplineRevision;

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

        // -----------------------------------------------------------------------------------------
        // Local action identities. The STRINGS are the per-card-local ones every earlier set uses;
        // the C# members are named for this block, because five parallel partials of one class are
        // being written at once and one shared member name would be a compile error rather than a
        // merge. Milestone 8.61i folds them onto the set's own trio.
        // -----------------------------------------------------------------------------------------

        /// <summary>The Skill primary both of this block's composed faces print.</summary>
        public static ActionDefinitionId DisciplinePrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");

        /// <summary>p.9's green Movement ribbon, whose text is suppressed (p.11).</summary>
        public static ActionDefinitionId DisciplineSecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        /// <summary>p.14's Defense reaction, spent by discarding the card from hand.</summary>
        public static ActionDefinitionId DisciplineSecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// The three definitions this block composes, in the shard's own row order. Milestone 8.61i is
        /// what assembles the nineteen; this list exists so a fixture can reach exactly these.
        /// </summary>
        public static IReadOnlyList<CardDefinition> DisciplineDefinitions { get; } =
            Array.AsReadOnly(new[]
            {
                // min15 - Green II, Initiative 2, Defense 2, Movement 3, Skill with no reach icon,
                // Attack item.
                BuildDisciplineSkill(
                    InnerStrength,
                    "Inner Strength",
                    initiative: 2,
                    secondaryDefense: 2,
                    secondaryMovement: 3,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Green, CardTier.II, PrintedValueChannel.Attack),
                    printedText: "This round: Double your item bonuses.",
                    skillEffect: MinEffectContent.InnerStrengthSkillEffect(DisciplineRevision)),

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
                    skillEffect: MinEffectContent.PerfectSelfSkillEffect(DisciplineRevision)),

                // min18 - Purple Tier IV. No Initiative, no icons, no action: the whole card is the
                // permanent ability.
                new CardDefinition(
                    FlurryOfBlows,
                    DisciplineRevision,
                    "Flurry of Blows",
                    default,
                    CardContentAuthority.VerifiedOfficial,
                    CardProgressionProfile.UltimateCard(),
                    Array.Empty<ActionDefinition>(),
                    MinEffectContent.FlurryOfBlowsRule(DisciplineRevision),
                    CardSourceContent.Transcribed(
                        "Each time after you perform an attack action you may repeat it once on a " +
                        "different target."))
            });

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
                DisciplineRevision,
                displayName,
                new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                progression,
                new[]
                {
                    // MI-05: neither face prints a Range or a Radius icon, so the primary carries no
                    // printed value at all. A missing icon is absent, never zero.
                    new ActionDefinition(
                        DisciplinePrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        skillEffect),

                    new ActionDefinition(
                        DisciplineSecondaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Movement, secondaryMovement)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

                    new ActionDefinition(
                        DisciplineSecondaryDefense,
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
        /// block of the series uses, written locally so this partial depends on no member a sibling
        /// block owns.
        /// </summary>
        private static CardDefinitionId DisciplineId(string cardId) =>
            new CardDefinitionId("min-" + cardId);
    }
}
