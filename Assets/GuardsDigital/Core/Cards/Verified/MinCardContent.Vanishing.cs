using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.61b. Min the Dragonmonk's three Block faces as printed definitions - her Green Tier I
    /// Poof!, the Tier II it upgrades into, Vanish, and the Tier III above that, Ruse.
    ///
    /// <para><b>The values are the shard's table, transcribed.</b> Poof! (<c>min13__7582935</c>):
    /// Initiative 2, secondary Movement 3, primary BLOCK DEFENSE with a Range 3 icon, no Item icon.
    /// Vanish (<c>min14__7582936</c>): Initiative 2, Movement 3, Range 4, Item icon Defense. Ruse
    /// (<c>min16__7602331</c>): Initiative 1, Movement 3, Range 4, Item icon Radius. See
    /// <c>Docs/AI/Rules/HERO_MIN.md</c>, MI-01 and MI-05.</para>
    ///
    /// <para><b>None of the three prints a Defense value, and that absence is the card.</b> MI-05:
    /// "Poof!, Vanish and Ruse print only Movement 3 and no Defense value at all: they are Block cards
    /// (the exclamation shield) whose only defense is the Block." So there is no Defense ribbon action
    /// here at all - not a zero - and p.14's other half applies in full: "If the Block condition is
    /// fulfilled, you successfully defend, regardless of the attack value. If not, you are defeated."
    /// Tigerclaw's five Blocks are the precedent for every part of that shape but the condition.</para>
    ///
    /// <para><b>The upgrade ladder is stated only as far as this block owns it.</b> p.15 makes Poof!
    /// upgrade into either face of Green Tier II - Vanish or Inner Strength - and Vanish into either
    /// Tier III, Ruse or Perfect Self. Inner Strength and Perfect Self are 8.61h's faces, and naming
    /// their definition IDs from here would be this block reaching into another's; 8.61i composes the
    /// whole set and completes both pairs. Nothing about the three definitions below changes when it
    /// does.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <c>MatchContentCatalogs</c> names no Min content, so no
    /// configured match can deal these three cards and nothing reachable can start their swap -
    /// milestone 7.9's gate, not an oversight.</para>
    /// </summary>
    public static partial class MinCardContent
    {
        /// <summary>
        /// The per-block placeholder revision, deliberately not a value that could be mistaken for a
        /// real content revision. 8.61i mints the one real string the whole set carries.
        /// </summary>
        public const string VanishingRevision = "min-8-61b-vanishing";

        /// <summary>Green Tier I. "Swap with a [Smoke bomb] Smoke bomb token in range; if you do, block the attack."</summary>
        public static CardDefinitionId Poof { get; } = new CardDefinitionId("min-poof");

        /// <summary>Green Tier II. Poof!'s sentence at Range 4, with a Defense Item icon.</summary>
        public static CardDefinitionId Vanish { get; } = new CardDefinitionId("min-vanish");

        /// <summary>Green Tier III. The same, and "You may place the Smoke bomb into a space in range."</summary>
        public static CardDefinitionId Ruse { get; } = new CardDefinitionId("min-ruse");

        /// <summary>
        /// The Block primary all three faces print. The identity string is every earlier set's - an
        /// action identity is local to its card - and only the C# member name is this block's, so five
        /// partial files over one class never collide on it.
        /// </summary>
        public static ActionDefinitionId VanishingPrimaryBlock { get; } =
            new ActionDefinitionId("primary-block");

        /// <summary>The Movement ribbon all three print, whose card text p.11 suppresses.</summary>
        public static ActionDefinitionId VanishingSecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        /// <summary>Poof!'s printed text, transcribed. Vanish prints the identical sentence.</summary>
        public const string PoofText =
            "Swap with a [Smoke bomb] Smoke bomb token in range; if you do, block the attack.";

        /// <summary>Vanish's printed text - word for word Poof!'s, at a different printed Range.</summary>
        public const string VanishText = PoofText;

        /// <summary>Ruse's printed text, transcribed whole.</summary>
        public const string RuseText =
            "Swap with a [Smoke bomb] Smoke bomb in range; if you do, block the attack. You may place " +
            "the Smoke bomb into a space in range.";

        /// <summary>The three faces this block composes.</summary>
        public static IEnumerable<CardDefinitionId> VanishingCards() =>
            new[] { Poof, Vanish, Ruse };

        // MERGE NOTE (8.61b -> main): this block's faces join the class aggregate declared in
        // MinCardContent.SmokeBomb.cs (LazyAllDefinitions concatenates VanishingDefinitions) until
        // 8.61i mints the real set; this partial declares no AllDefinitions of its own.

        /// <summary>The three printed definitions, built once so two readers compare the same object.</summary>
        public static IReadOnlyList<CardDefinition> VanishingDefinitions => LazyVanishing.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyVanishing =
            new Lazy<IReadOnlyList<CardDefinition>>(
                () => Array.AsReadOnly(VanishingCards().Select(Build).ToArray()));

        /// <summary>One of the three printed definitions, by ID.</summary>
        public static CardDefinition VanishingDefinition(CardDefinitionId cardId) =>
            VanishingDefinitions.SingleOrDefault(definition => definition.Id.Equals(cardId)) ??
            throw new ArgumentOutOfRangeException(
                nameof(cardId), cardId,
                "Milestone 8.61b composes Poof!, Vanish and Ruse and no other face of hers.");

        private static CardDefinition Build(CardDefinitionId cardId)
        {
            if (cardId.Equals(Poof))
            {
                return Compose(
                    Poof, "Poof!", initiative: 2, range: 3, printedText: PoofText,
                    mayRePlaceTheBomb: false,
                    // 8.61i adds Inner Strength, the sibling Tier II this block does not own.
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Green, CardTier.I, PrintedValueChannel.None, Vanish));
            }

            if (cardId.Equals(Vanish))
            {
                return Compose(
                    Vanish, "Vanish", initiative: 2, range: 4, printedText: VanishText,
                    mayRePlaceTheBomb: false,
                    // 8.61i adds Perfect Self, the sibling Tier III this block does not own.
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Green, CardTier.II, PrintedValueChannel.Defense, Ruse));
            }

            if (cardId.Equals(Ruse))
            {
                return Compose(
                    Ruse, "Ruse", initiative: 1, range: 4, printedText: RuseText,
                    mayRePlaceTheBomb: true,
                    progression: CardProgressionProfile.NonBasicCard(
                        CardColor.Green, CardTier.III, PrintedValueChannel.Radius));
            }

            throw new ArgumentOutOfRangeException(
                nameof(cardId), cardId,
                "Milestone 8.61b composes Poof!, Vanish and Ruse and no other face of hers.");
        }

        /// <summary>
        /// All three faces print the same two icons and differ only in the numbers and in Ruse's second
        /// sentence, so one composition writes all three: p.14's Block as the card's printed PRIMARY
        /// action, carrying p.9's Range icon and the swap its condition is the precondition of, and
        /// p.11's Movement ribbon whose card text is suppressed.
        ///
        /// <para><b>There is no Defense action on any of them.</b> A Block is performed as a reaction
        /// but printed as the primary, which is why the state transition p.14 spends it by - Hand to
        /// discard pile - sits on the primary here and there is no second Defense declaration to carry
        /// it. <c>ActionDefinition</c> refuses a Block printed anywhere but the primary slot, and
        /// refuses one carrying any printed value but Range.</para>
        /// </summary>
        private static CardDefinition Compose(
            CardDefinitionId cardId,
            string displayName,
            int initiative,
            int range,
            string printedText,
            bool mayRePlaceTheBomb,
            CardProgressionProfile progression) =>
            new CardDefinition(
                cardId,
                VanishingRevision,
                displayName,
                new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                progression,
                new[]
                {
                    new ActionDefinition(
                        VanishingPrimaryBlock,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, range) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                        MinEffectContent.VanishingBlock(cardId, VanishingRevision, mayRePlaceTheBomb),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.Block,
                        MinEffectContent.VanishingDefenseTextTimingFor(cardId),
                        blockCondition: PrintedBlockCondition
                            .TheActorsTokenNamedByTheDefenseTextIsWithinTheCardsRange),
                    new ActionDefinition(
                        VanishingSecondaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Movement, VanishingSecondaryMovementValue)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                },
                CardSourceContent.Transcribed(printedText));

        /// <summary>All three faces print a Movement ribbon of 3.</summary>
        private const int VanishingSecondaryMovementValue = 3;
    }
}
