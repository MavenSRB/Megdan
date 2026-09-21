using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's five Green faces - Monkey Trick, Monkey Twist, Hear Nothing, Monkey
    /// Business and See Nothing - whose two clauses milestone 8.71f owns. The other fourteen faces
    /// belong to the blocks that own their clauses, and milestone 8.71g states the whole set under one
    /// real content revision.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_HANU.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.71: Monkey Trick
    /// (<c>hanu13__7635529</c>) Green Tier I, Initiative <b>2</b>, Defense <b>1</b>, Movement <b>2</b>,
    /// primary SKILL with Radius <b>1</b>, no item; Monkey Twist (<c>hanu14__7635519</c>) Green Tier
    /// II, 2/2/2, Radius <b>2</b>, <b>Defense</b> item; Hear Nothing (<c>hanu15__7635515</c>) Green
    /// Tier II, 2/2/2, Radius <b>3</b>, <b>Attack</b> item; Monkey Business (<c>hanu16__7635521</c>)
    /// Green Tier III, 1/2/2, Radius <b>2</b>, <b>Defense</b> item; See Nothing
    /// (<c>hanu17__7635527</c>) Green Tier III, 1/2/2, Radius <b>3</b>, <b>Range</b> item.</para>
    ///
    /// <para><b>The Green ladder forks twice and both forks are stated here</b>, because both halves of
    /// it are this block's own faces: Monkey Trick climbs to Monkey Twist or Hear Nothing, and either
    /// of those to Monkey Business or See Nothing (p.15). The unchosen face of each pair becomes its
    /// printed item, which is why See Nothing's <b>Range</b> icon sits beside a Radius one - p.16's
    /// items raise the channel they NAME, and the Range item this face hands out on the unchosen branch
    /// is one his Reds spend.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's gate.
    /// The revision is the deliberately malformed <see cref="GreenPrepRevision"/>: a placeholder must
    /// not be a value that could be mistaken for a real one, and 8.71g replaces it with the one string
    /// his whole set is composed under.</para>
    /// </summary>
    public static partial class HanuCardContent
    {
        /// <summary>
        /// The placeholder this block's five faces are composed under until 8.71g states the set. It is
        /// not a well-formed content revision and is not meant to be.
        /// </summary>
        private const string GreenPrepRevision = "hanu-8-71f-prep";

        /// <summary>His Green Tier I: the friendly pair swap, and nothing after it.</summary>
        public static CardDefinitionId MonkeyTrick { get; } =
            new CardDefinitionId("hanu-monkey-trick");

        /// <summary>His Green Tier II: the same sentence at Radius 2.</summary>
        public static CardDefinitionId MonkeyTwist { get; } =
            new CardDefinitionId("hanu-monkey-twist");

        /// <summary>His other Green Tier II: the enemy-hero swap at Radius 3.</summary>
        public static CardDefinitionId HearNothing { get; } =
            new CardDefinitionId("hanu-hear-nothing");

        /// <summary>His Green Tier III: the friendly pair swap, then "Move up to 1 space".</summary>
        public static CardDefinitionId MonkeyBusiness { get; } =
            new CardDefinitionId("hanu-monkey-business");

        /// <summary>His other Green Tier III: the enemy-hero swap, then "Move up to 1 space".</summary>
        public static CardDefinitionId SeeNothing { get; } =
            new CardDefinitionId("hanu-see-nothing");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed.
        // -----------------------------------------------------------------------------------------

        /// <summary>The sentence Monkey Trick, Monkey Twist and Monkey Business print.</summary>
        public const string FriendlySwapSentence = "Swap two friendly units in radius.";

        /// <summary>The sentence Hear Nothing and See Nothing print.</summary>
        public const string EnemySwapSentence = "Swap with an enemy hero in radius.";

        /// <summary>The second sentence the two Tier III faces print, word for word.</summary>
        public const string TrailingMoveSentence = "Move up to 1 space.";

        public const string MonkeyTrickText = FriendlySwapSentence;

        public const string MonkeyTwistText = FriendlySwapSentence;

        public const string HearNothingText = EnemySwapSentence;

        public const string MonkeyBusinessText = FriendlySwapSentence + " " + TrailingMoveSentence;

        public const string SeeNothingText = EnemySwapSentence + " " + TrailingMoveSentence;

        /// <summary>The five faces this block composes, in his shard's inventory order.</summary>
        public static IEnumerable<CardDefinition> GreenDefinitions() => LazyGreens.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyGreens =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildGreen(
                    MonkeyTrick, "Monkey Trick", CardTier.I, 2, 1, 2, 1,
                    PrintedValueChannel.None, MonkeyTrickText, GreenClause.FriendlyPair, 0,
                    new[] { MonkeyTwist, HearNothing }),
                BuildGreen(
                    MonkeyTwist, "Monkey Twist", CardTier.II, 2, 2, 2, 2,
                    PrintedValueChannel.Defense, MonkeyTwistText, GreenClause.FriendlyPair, 0,
                    new[] { MonkeyBusiness, SeeNothing }),
                BuildGreen(
                    HearNothing, "Hear Nothing", CardTier.II, 2, 2, 2, 3,
                    PrintedValueChannel.Attack, HearNothingText, GreenClause.EnemyHero, 0,
                    new[] { MonkeyBusiness, SeeNothing }),
                BuildGreen(
                    MonkeyBusiness, "Monkey Business", CardTier.III, 1, 2, 2, 2,
                    PrintedValueChannel.Defense, MonkeyBusinessText, GreenClause.FriendlyPair, 1,
                    Array.Empty<CardDefinitionId>()),
                BuildGreen(
                    SeeNothing, "See Nothing", CardTier.III, 1, 2, 2, 3,
                    PrintedValueChannel.Range, SeeNothingText, GreenClause.EnemyHero, 1,
                    Array.Empty<CardDefinitionId>())
            }));

        /// <summary>Which of the two printed sentences a face carries.</summary>
        private enum GreenClause
        {
            /// <summary>"Swap two friendly units in radius."</summary>
            FriendlyPair,

            /// <summary>"Swap with an enemy hero in radius."</summary>
            EnemyHero
        }

        /// <summary>
        /// One face of the five. They differ in Initiative, Defense, the printed Radius, the item icon,
        /// which of the two sentences they print and whether a trailing move follows it - and in
        /// nothing else at all.
        ///
        /// <para>The primary is p.9's Skill band, so its card text IS the action (p.12) and the Radius
        /// icon is read by that text rather than by any targeting the page performs. The Movement
        /// ribbon's own card text is suppressed by p.11 and the Defense ribbon is p.14's discard from
        /// hand - every dealt face of his but the Ultimate prints a Defense, and every face but Hurry
        /// up! and the Ultimate prints a Movement.</para>
        /// </summary>
        private static CardDefinition BuildGreen(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int initiative,
            int defense,
            int movement,
            int radius,
            PrintedValueChannel itemIcon,
            string printedText,
            GreenClause clause,
            int moveDistance,
            IReadOnlyList<CardDefinitionId> nextTierCandidates) => new CardDefinition(
            id,
            GreenPrepRevision,
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
                    clause == GreenClause.FriendlyPair
                        ? HanuEffectContent.FriendlySwap(id, GreenPrepRevision, moveDistance)
                        : HanuEffectContent.EnemySwap(id, GreenPrepRevision, moveDistance),
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

        // MERGE NOTE: this partial's MERGE BLOCK was dropped at the squash-merge; the surviving
        // aggregate and the shared action identities live in HanuCardContent.CoMoves.cs.
    }
}
