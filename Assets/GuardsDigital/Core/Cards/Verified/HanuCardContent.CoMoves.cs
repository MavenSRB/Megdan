using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's two Blue co-moves - This Way! and That Way! - whose clause milestone 8.71e
    /// owns. The other seventeen faces belong to the blocks that own their clauses, and 8.71g states
    /// the whole set under one real content revision.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_HANU.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.71: This Way! (<c>hanu10__7635523</c>)
    /// Blue Tier II, Initiative <b>11</b>, Defense <b>4</b>, Movement <b>3</b>, primary SKILL with
    /// Radius <b>3</b>, <b>Initiative</b> item; That Way! (<c>hanu12__7635524</c>) Blue Tier III,
    /// 11/5/3, Radius <b>3</b>, <b>Initiative</b> item.</para>
    ///
    /// <para><b>Only one progression row is stated here.</b> This Way! is a Tier II face of the Blue
    /// ladder whose Tier I is Unexpected Journey and whose other Tier II is There and Back Again -
    /// 8.71d's faces - so this block names only the successor it can see, That Way!. 8.71g states both
    /// forks whole, exactly as 8.69f did for Brynn's Blue pair.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's gate:
    /// the revision is this block's placeholder, and it is not a valid content revision by
    /// construction.</para>
    /// </summary>
    public static partial class HanuCardContent
    {
        /// <summary>
        /// This block's placeholder revision. 8.71g replaces it with the one real string his whole set
        /// is stated under; until then it is deliberately not a revision any dealt content could carry.
        /// </summary>
        private const string CoMovePrepRevision = "hanu-8-71e-prep";

        /// <summary>His Blue Tier II: a friendly hero picks 1 or 2, and both of them travel it.</summary>
        public static CardDefinitionId ThisWay { get; } = new CardDefinitionId("hanu-this-way");

        /// <summary>His Blue Tier III: the same sentence, reaching one space further.</summary>
        public static CardDefinitionId ThatWay { get; } = new CardDefinitionId("hanu-that-way");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// This Way!, word for word - including the parenthesised reminder, which is a statement of
        /// <c>coupled-direct-move@1</c>'s own contract rather than a rule this face alone has.
        /// </summary>
        public const string ThisWayText =
            "A friendly hero in radius chooses a distance of 1 or 2; move both of you that number " +
            "of spaces in the same direction of your choice. ( Both must be moved the full " +
            "distance, or neither one moves ).";

        /// <summary>That Way!, word for word. It prints no reminder and has the same rule.</summary>
        public const string ThatWayText =
            "A friendly hero in radius chooses a distance of 1, 2 or 3; move both of you that " +
            "number of spaces in the same direction of your choice.";

        /// <summary>The two faces this block composes, in his shard's inventory order.</summary>
        public static IEnumerable<CardDefinition> CoMoveDefinitions() => LazyCoMoves.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyCoMoves =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildCoMove(
                    ThisWay, "This Way!", CardTier.II, 11, 4, 3, 2, ThisWayText,
                    new[] { ThatWay }),
                BuildCoMove(
                    ThatWay, "That Way!", CardTier.III, 11, 5, 3, 3, ThatWayText,
                    Array.Empty<CardDefinitionId>())
            }));

        /// <summary>
        /// One face of the two. They differ in Tier, printed Defense, the greatest distance the friend
        /// may name and the progression row - and in nothing else at all, which is what makes one
        /// factory the honest statement of two cards rather than a saving.
        ///
        /// <para>Both print the same Initiative 11, the same Radius 3, the same Movement 3 ribbon and
        /// the same <b>Initiative</b> item. The primary is p.9's Skill band, so its card text IS the
        /// action (p.12) and the Radius icon is read by that text. The Movement ribbon's own card text
        /// is suppressed by p.11 and the Defense ribbon is p.14's discard from hand.</para>
        /// </summary>
        private static CardDefinition BuildCoMove(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int initiative,
            int defense,
            int movement,
            int greatestDistance,
            string printedText,
            IReadOnlyList<CardDefinitionId> nextTierCandidates) => new CardDefinition(
            id,
            CoMovePrepRevision,
            displayName,
            new InitiativeValue(initiative),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(
                CardColor.Blue, tier, PrintedValueChannel.Initiative, nextTierCandidates),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimarySkill,
                    ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    HanuEffectContent.CoMove(id, CoMovePrepRevision, greatestDistance),
                    PrintedTextComposition.None),

                new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movement) },
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

        // NO `Definition(CardDefinitionId)` helper here, deliberately: every 8.71 wave partial would
        // declare the same member and a partial class cannot declare one twice. 8.71g mints it once
        // over the whole set, exactly as `BrynnCardContent.cs` does; until then a fixture reads
        // `AllDefinitions` itself.

        // ===== MERGE BLOCK (identical in every 8.71 wave partial; the coordinator keeps one) =====
        public static ActionDefinitionId PrimaryBasicAttack { get; } = new ActionDefinitionId("primary-basic-attack");
        public static ActionDefinitionId PrimaryBasicSkill { get; } = new ActionDefinitionId("primary-basic-skill");
        public static ActionDefinitionId PrimaryRangedAttack { get; } = new ActionDefinitionId("primary-ranged-attack");
        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryMovement { get; } = new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } = new ActionDefinitionId("secondary-defense");
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;
        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(ComposedFaces().ToArray()));
        private static IEnumerable<CardDefinition> ComposedFaces() =>
            CoMoveDefinitions()
                .Concat(HurryUpDefinitions())
                .Concat(RedDefinitions())
                .Concat(GreenDefinitions())
                .Concat(JourneyDefinitions());
        public static IReadOnlyList<CardDefinition> DealtDefinitions { get; } = Array.Empty<CardDefinition>();
        // ===== END MERGE BLOCK =====
    }
}
