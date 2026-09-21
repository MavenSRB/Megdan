using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's five GREEN faces - the whole colour, composed at milestone 8.69e because one
    /// block owns every clause on it: Bear Trap, Log Trap, True Grit, Deadfall Trap and Die Hard.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_BRYNN.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.69: Bear Trap (<c>brynn13__7715988</c>)
    /// Green Tier I, Initiative <b>3</b>, Defense <b>3</b>, Movement <b>2</b>, Skill with Radius
    /// <b>3</b>, no item; Log Trap (<c>brynn14__7715987</c>) Tier II, 2/3/2, Radius <b>4</b>, Initiative
    /// item; True Grit (<c>brynn15__7418476</c>) Tier II, 2/3/2, Radius 4, Attack item; Deadfall Trap
    /// (<c>brynn16__7715986</c>) Tier III, 2/4/2, Radius 4, Initiative item; Die Hard
    /// (<c>brynn17__7418486</c>) Tier III, 2/4/2, Radius 4, Movement item.</para>
    ///
    /// <para><b>The Green ladder is stated WHOLE here, where the Red one was not.</b> p.15 forks each
    /// colour twice, and the reason 8.69a left High Ground naming half its fork was that the other half
    /// is a sibling branch's face - naming an id another worktree owns is what Min's series refused
    /// four times over. Green has no such problem: all five faces are this block's, so Bear Trap names
    /// both Tier IIs and each Tier II names both Tier IIIs, exactly as the shard's progression
    /// paragraph prints it. 8.69f states every ladder again as one set; this one will not change.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b> - milestone 7.9's gate, the same
    /// one 8.69a's remarks explain. The revision is
    /// <see cref="BrynnComponentContent.TrapPrepRevision"/>, a deliberately malformed placeholder that
    /// 8.69f replaces with the one real string every face of his carries.</para>
    /// </summary>
    public static partial class BrynnCardContent
    {
        // -----------------------------------------------------------------------------------------
        // Identities
        // -----------------------------------------------------------------------------------------

        // MERGE NOTE (8.69e landed after 8.69d): `PrimarySkill` ("primary-skill") is declared once,
        // in BrynnCardContent.Swaps.cs; the copy this block carried was dropped at the merge.

        /// <summary>His Green Tier I: the trap modal, at Radius 3 and with no item.</summary>
        public static CardDefinitionId BearTrap { get; } =
            new CardDefinitionId("brynn-bear-trap");

        /// <summary>His Green Tier II trap: Bear Trap's sentence at Radius 4.</summary>
        public static CardDefinitionId LogTrap { get; } = new CardDefinitionId("brynn-log-trap");

        /// <summary>His Green Tier II grit: the retrieval and a move of up to 3.</summary>
        public static CardDefinitionId TrueGrit { get; } = new CardDefinitionId("brynn-true-grit");

        /// <summary>His Green Tier III trap: the same modal, escalating to a Defeat.</summary>
        public static CardDefinitionId DeadfallTrap { get; } =
            new CardDefinitionId("brynn-deadfall-trap");

        /// <summary>His Green Tier III grit: the retrieval and a move of up to 4.</summary>
        public static CardDefinitionId DieHard { get; } = new CardDefinitionId("brynn-die-hard");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed
        // -----------------------------------------------------------------------------------------

        public const string BearTrapText =
            "Choose one - • An enemy hero adjacent to you discards a card, if able. • An enemy hero " +
            "in radius who is adjacent to 3 or more obstacles discards a card, if able.";

        /// <summary>Log Trap prints Bear Trap's sentence exactly; the shard records the values.</summary>
        public const string LogTrapText = BearTrapText;

        public const string DeadfallTrapText =
            "Choose one - • An enemy hero adjacent to you discards a card, or is defeated. • An enemy " +
            "hero in radius who is adjacent to 3 or more obstacles discards a card, or is defeated.";

        public const string TrueGritText =
            "You may retrieve a discarded attack card. If an enemy hero in radius is adjacent to 3 or " +
            "more obstacles, move up to 3 spaces.";

        public const string DieHardText =
            "You may retrieve a discarded attack card. If an enemy hero in radius is adjacent to 3 or " +
            "more obstacles, move up to 4 spaces.";

        // -----------------------------------------------------------------------------------------
        // The five definitions
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The five Green faces this block composes, in his shard's inventory order - which is the
        /// order the printed-inventory table prints them in, Tier II's two faces before Tier III's two.
        /// </summary>
        public static IReadOnlyList<CardDefinition> TrapAndGritDefinitions => LazyTrapAndGrit.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyTrapAndGrit =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildTrap(
                    BearTrap, "Bear Trap", 3, CardTier.I, radius: 3, defense: 3,
                    itemIcon: PrintedValueChannel.None, printedText: BearTrapText,
                    escalatesToDefeat: false,
                    nextTierCandidates: new[] { LogTrap, TrueGrit }),
                BuildTrap(
                    LogTrap, "Log Trap", 2, CardTier.II, radius: 4, defense: 3,
                    itemIcon: PrintedValueChannel.Initiative, printedText: LogTrapText,
                    escalatesToDefeat: false,
                    nextTierCandidates: new[] { DeadfallTrap, DieHard }),
                BuildGrit(
                    TrueGrit, "True Grit", CardTier.II, defense: 3,
                    itemIcon: PrintedValueChannel.Attack, distance: 3, printedText: TrueGritText,
                    nextTierCandidates: new[] { DeadfallTrap, DieHard }),
                BuildTrap(
                    DeadfallTrap, "Deadfall Trap", 2, CardTier.III, radius: 4, defense: 4,
                    itemIcon: PrintedValueChannel.Initiative, printedText: DeadfallTrapText,
                    escalatesToDefeat: true,
                    nextTierCandidates: Array.Empty<CardDefinitionId>()),
                BuildGrit(
                    DieHard, "Die Hard", CardTier.III, defense: 4,
                    itemIcon: PrintedValueChannel.Movement, distance: 4, printedText: DieHardText,
                    nextTierCandidates: Array.Empty<CardDefinitionId>())
            }));

        /// <summary>
        /// Bear Trap, Log Trap and Deadfall Trap. All three print a Skill primary over a Radius icon,
        /// p.11's suppressed Movement ribbon at 2 and p.14's Defense ribbon; what separates them is the
        /// Initiative, the Radius, the Defense, the item icon and whether the sentence escalates.
        /// </summary>
        private static CardDefinition BuildTrap(
            CardDefinitionId id,
            string displayName,
            int initiative,
            CardTier tier,
            int radius,
            int defense,
            PrintedValueChannel itemIcon,
            string printedText,
            bool escalatesToDefeat,
            IReadOnlyList<CardDefinitionId> nextTierCandidates) => GreenSkill(
            id, displayName, initiative, tier, radius, defense, itemIcon, printedText,
            nextTierCandidates,
            BrynnEffectContent.TrapModal(
                id, BrynnComponentContent.TrapPrepRevision, escalatesToDefeat));

        /// <summary>
        /// True Grit and Die Hard, which print one Initiative, one Radius, one Defense apiece and
        /// differ in the item icon and in how far the second sentence moves him.
        /// </summary>
        private static CardDefinition BuildGrit(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int defense,
            PrintedValueChannel itemIcon,
            int distance,
            string printedText,
            IReadOnlyList<CardDefinitionId> nextTierCandidates) => GreenSkill(
            id, displayName, initiative: 2, tier: tier, radius: 4, defense: defense,
            itemIcon: itemIcon, printedText: printedText, nextTierCandidates: nextTierCandidates,
            body: BrynnEffectContent.Grit(
                id, BrynnComponentContent.TrapPrepRevision, distance));

        /// <summary>
        /// The shape all five share: a Skill primary carrying p.9's Radius icon and the card's own
        /// text, p.11's Movement ribbon whose card text the page suppresses, and p.14's Defense ribbon
        /// spent by discarding the card out of hand. Every Green face of his prints a Movement 2.
        /// </summary>
        private static CardDefinition GreenSkill(
            CardDefinitionId id,
            string displayName,
            int initiative,
            CardTier tier,
            int radius,
            int defense,
            PrintedValueChannel itemIcon,
            string printedText,
            IReadOnlyList<CardDefinitionId> nextTierCandidates,
            EffectDefinition body) => new CardDefinition(
            id,
            BrynnComponentContent.TrapPrepRevision,
            displayName,
            new InitiativeValue(initiative),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(
                CardColor.Green, tier, itemIcon, nextTierCandidates),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimarySkill,
                    ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    body),

                new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

                new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, defense) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(printedText));
    }
}
