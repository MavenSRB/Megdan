using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's five Red faces - her whole Stance ladder. Milestone 8.61c.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_MIN.md</c>'s printed
    /// inventory, read off the extracted fronts <c>min3</c> through <c>min7</c> at milestone 8.61:
    /// Crane Stance (Red I, Initiative 9, Defense 5, Movement 5, Attack 3, no item), Tiger Stance
    /// (Red II, 9/5/5, Attack 4, Initiative item), Viper Stance (Red II, 9/5/5, Attack 5, Radius 4,
    /// Defense item), Dragon Stance (Red III, 10/6/5, Attack 4, Movement item) and Cobra Stance (Red
    /// III, 10/6/5, Attack 5, Radius 4, Defense item). MI-05 is the rule the absent icons follow - "A
    /// missing icon is absent, never zero" - which is why the three faces that print no Radius declare
    /// none rather than a zero, and why "adjacent to you" on those three is a WORD rather than a
    /// number p.16's items could raise.</para>
    ///
    /// <para><b>The ladder is p.15's, and it forks twice.</b> Crane upgrades to Tiger or Viper, and
    /// either of those to Dragon or Cobra - so the Tier III pair is named by both Tier II faces, which
    /// is the shape <c>CardProgressionProfile.NonBasicCard</c> already carries.</para>
    ///
    /// <para><b>Every one of them prints an ordinary adjacent Attack.</b> "Target a unit adjacent to
    /// you" is rulebook p.13's own reach for a non-Ranged Attack - the page says so "even if the card
    /// has a radius icon" - so none of the five declares a target policy of its own and Viper's and
    /// Cobra's Radius 4 is read by their card TEXT alone. What each face composes is only the rider
    /// after that sentence; see <see cref="MinEffectContent.StanceTextFor"/>.</para>
    ///
    /// <para><b>Nothing here is registered or dealt</b>, and the revision is
    /// <see cref="MinComponentContent.PrepRevision"/> - see <see cref="MinComponentContent"/>'s own
    /// remarks for why the gate is the point. Milestone 8.61i states her whole set of nineteen under
    /// one real content revision and mints <c>DealtDefinitions</c>.</para>
    /// </summary>
    public static partial class MinCardContent
    {
        /// <summary>"Target a unit adjacent to you. After the attack: Push an enemy unit adjacent to you up to 3 spaces."</summary>
        public static CardDefinitionId CraneStance { get; } = new CardDefinitionId("min-crane-stance");

        /// <summary>Crane's first upgrade: the push with an optional step of one space before it.</summary>
        public static CardDefinitionId TigerStance { get; } = new CardDefinitionId("min-tiger-stance");

        /// <summary>Crane's other upgrade: the hardest hit of the three, trading the push for the bomb.</summary>
        public static CardDefinitionId ViperStance { get; } = new CardDefinitionId("min-viper-stance");

        /// <summary>Tier III over Tiger's clause: one space becomes one or two.</summary>
        public static CardDefinitionId DragonStance { get; } = new CardDefinitionId("min-dragon-stance");

        /// <summary>Tier III over Viper's clause: the swap gains a re-place.</summary>
        public static CardDefinitionId CobraStance { get; } = new CardDefinitionId("min-cobra-stance");

        /// <summary>p.9's Red band with an Attack primary.</summary>
        public static ActionDefinitionId PrimaryAttack { get; } =
            new ActionDefinitionId("primary-attack");

        /// <summary>p.9's Movement ribbon icon, which every face printing one declares as a secondary.</summary>
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed.
        // -----------------------------------------------------------------------------------------

        /// <summary>The bracketed glyph is the printed [Smoke bomb] icon, as every transcription writes one.</summary>
        public const string CraneStanceText =
            "Target a unit adjacent to you. After the attack: Push an enemy unit adjacent to you up " +
            "to 3 spaces.";

        public const string TigerStanceText =
            "Target a unit adjacent to you. After the attack: You may move 1 space to a space " +
            "adjacent to the target. Push an enemy unit adjacent to you up to 3 spaces.";

        public const string ViperStanceText =
            "Target a unit adjacent to you. After the attack: You may swap with a [Smoke bomb] Smoke " +
            "bomb in radius.";

        public const string DragonStanceText =
            "Target a unit adjacent to you. After the attack: You may move 1 or 2 spaces to a space " +
            "adjacent to the target. Push an enemy unit adjacent to you up to 3 spaces.";

        public const string CobraStanceText =
            "Target a unit adjacent to you. After the attack: You may swap with a [Smoke bomb] Smoke " +
            "bomb in radius; if you do, you may place the Smoke bomb into a space in radius.";

        /// <summary>
        /// The five faces this block composes, in printed order - Tier I, the two Tier IIs, the two
        /// Tier IIIs.
        ///
        /// <para>A list of its own rather than a second <c>AllDefinitions</c>, which would not compile
        /// beside milestone 8.61a's: that member is owed to seven assembly sweeps the moment a
        /// <c>MinCardContent</c> exists, so the block that first declared the class owns it and every
        /// later block of hers adds its faces to it. Milestone 8.61i replaces both with the whole
        /// nineteen under one real content revision.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> StanceDefinitions => LazyStances.Value;

        /// <summary>One Stance by id, for a fixture or a builder that names the face it wants.</summary>
        public static CardDefinition StanceDefinition(CardDefinitionId cardId) =>
            StanceDefinitions.Single(definition => definition.Id.Equals(cardId));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyStances =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildStance(
                    CraneStance, "Crane Stance", initiative: 9, defense: 5, movement: 5, attack: 3,
                    radius: 0,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Red, CardTier.I, PrintedValueChannel.None,
                        TigerStance, ViperStance),
                    printedText: CraneStanceText),

                BuildStance(
                    TigerStance, "Tiger Stance", initiative: 9, defense: 5, movement: 5, attack: 4,
                    radius: 0,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                        DragonStance, CobraStance),
                    printedText: TigerStanceText),

                BuildStance(
                    ViperStance, "Viper Stance", initiative: 9, defense: 5, movement: 5, attack: 5,
                    radius: 4,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                        DragonStance, CobraStance),
                    printedText: ViperStanceText),

                BuildStance(
                    DragonStance, "Dragon Stance", initiative: 10, defense: 6, movement: 5, attack: 4,
                    radius: 0,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                    printedText: DragonStanceText),

                BuildStance(
                    CobraStance, "Cobra Stance", initiative: 10, defense: 6, movement: 5, attack: 5,
                    radius: 4,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Red, CardTier.III, PrintedValueChannel.Defense),
                    printedText: CobraStanceText)
            }));

        /// <summary>
        /// One Red face: p.9's Attack primary with its rider, the Movement ribbon, and p.14's Defense
        /// reaction. All five print all three, which is why this is one builder and not five.
        /// </summary>
        private static CardDefinition BuildStance(
            CardDefinitionId cardId,
            string displayName,
            int initiative,
            int defense,
            int movement,
            int attack,
            int radius,
            CardProgressionProfile progression,
            string printedText) =>
            new CardDefinition(
                cardId,
                MinComponentContent.PrepRevision,
                displayName,
                new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                progression,
                new List<ActionDefinition>
                {
                    new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        radius > 0
                            ? new[]
                            {
                                new PrintedValueDefinition(PrintedValueChannel.Attack, attack),
                                new PrintedValueDefinition(PrintedValueChannel.Radius, radius)
                            }
                            : new[]
                            {
                                new PrintedValueDefinition(PrintedValueChannel.Attack, attack)
                            },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),

                        // p.13's plain Attack. The target policy is the page's own default for the
                        // classification - heroes and minions, adjacent - supplied by ActionDefinition
                        // rather than restated here, which is what "Target a unit adjacent to you"
                        // means when the card narrows nothing.
                        AttackRangeKind.NonRanged,
                        default,
                        MinEffectContent.StanceTextFor(cardId, MinComponentContent.PrepRevision),
                        default,
                        PrintedDefenseResolution.None,
                        MinEffectContent.StanceTextTimingFor(cardId)),

                    new ActionDefinition(
                        SecondaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Movement, movement)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

                    // p.14's Defense action: the ribbon icon, suppressed card text, and the p.13
                    // discard every reaction performs.
                    new ActionDefinition(
                        SecondaryDefense,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Defense, defense)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                },
                null,
                CardSourceContent.Transcribed(printedText));
    }
}
