using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's three Red faces whose clauses milestone 8.69b owns: Split Attack, Split Throw
    /// and Peak Precision. The other sixteen belong to the blocks that own their clauses, and 8.69f
    /// states the whole set under one real content revision.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_BRYNN.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.69: Split Attack
    /// (<c>brynn5__7690722</c>) Red Tier II, Initiative <b>9</b>, Defense <b>3</b>, Movement <b>4</b>,
    /// primary ATTACK - RANGED with Attack <b>3</b> and Range <b>2</b>, Defense item; Peak Precision
    /// (<c>brynn6__7696234</c>) Red Tier III, 10/7/4, primary ATTACK with Attack <b>5+</b> and no reach
    /// icon at all, Radius item; Split Throw (<c>brynn7__7690723</c>) Red Tier III, 10/4/4, primary
    /// ATTACK - RANGED with Attack <b>3</b> and Range <b>2</b>, Defense item. BR-06 is the rule the
    /// absent icons follow - "A missing icon is absent, never zero" - so Peak Precision declares no
    /// Range and no Radius on its action even though its ITEM slot is a Radius icon, which p.16 makes a
    /// fact about the card somebody else tucks under a dashboard rather than about this action.</para>
    ///
    /// <para><b>Peak Precision's printed "+" is the contribution and not a second number</b>, exactly
    /// as milestone 8.69a read High Ground's: p.14 says "The '+' sign signifies that the value can be
    /// increased by the card text", so the face prints Attack 5 and carries
    /// <see cref="RaisedAttack"/> - 8.69a's member, reused here rather than restated, so that the three
    /// faces printing "+2 [Attack] attack" cannot come to disagree about when it is read (BR-Q6).</para>
    ///
    /// <para><b>Only this block's own progression rows are stated.</b> p.15 forks Red twice, and the
    /// halves of both forks cross block boundaries: High Ground and Elevated Ambush are 8.69a's. So
    /// Split Attack names the two Tier III faces this block composes, and both of those name none -
    /// 8.69f states the ladders whole. Naming an id a sibling branch owns is what Min's series refused
    /// four times over.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's gate:
    /// the revision is <see cref="BrynnComponentContent.SplitAttackPrepRevision"/>, a deliberately
    /// malformed placeholder that 8.69f replaces, and <c>DealtDefinitions</c> stays empty.</para>
    /// </summary>
    public static partial class BrynnCardContent
    {
        /// <summary>His Red Tier II: "Target a unit in range. If you target a hero who is adjacent to 3
        /// or more obstacles, may repeat once on a different unit adjacent to you."</summary>
        public static CardDefinitionId SplitAttack { get; } =
            new CardDefinitionId("brynn-split-attack");

        /// <summary>His Red Tier III: "... repeat once on a different target unit." - no "may".</summary>
        public static CardDefinitionId SplitThrow { get; } =
            new CardDefinitionId("brynn-split-throw");

        /// <summary>His Red Tier III: "+2 [Attack] attack and After the attack: You may retrieve this
        /// card."</summary>
        public static CardDefinitionId PeakPrecision { get; } =
            new CardDefinitionId("brynn-peak-precision");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed.
        // -----------------------------------------------------------------------------------------

        public const string SplitAttackText =
            "Target a unit in range. If you target a hero who is adjacent to 3 or more obstacles, " +
            "may repeat once on a different unit adjacent to you.";

        public const string SplitThrowText =
            "Target a unit in range. If you target a hero who is adjacent to 3 or more obstacles, " +
            "repeat once on a different target unit.";

        /// <summary>The doubled "obstacles" is a printing slip transcribed as printed (BR-06's note);
        /// it carries no rule.</summary>
        public const string PeakPrecisionText =
            "Target a unit adjacent to you. If you target a hero adjacent to 3 or more obstacles " +
            "obstacles, +2 [Attack] attack and After the attack: You may retrieve this card.";

        /// <summary>The three faces this block composes, in his shard's inventory order.</summary>
        public static IReadOnlyList<CardDefinition> SplitAttackDefinitions => LazySplitAttack.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazySplitAttack =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildSplitFace(
                    SplitAttack, "Split Attack", CardTier.II, new InitiativeValue(9), 3,
                    PrintedValueChannel.Defense, SplitAttackText,
                    RepeatTargetBinding.DifferentUnitAdjacentToActor, printsMay: true,
                    nextTierCandidates: new[] { PeakPrecision, SplitThrow }),
                BuildPeakPrecision(),
                BuildSplitFace(
                    SplitThrow, "Split Throw", CardTier.III, new InitiativeValue(10), 4,
                    PrintedValueChannel.Defense, SplitThrowText,
                    RepeatTargetBinding.DifferentTarget, printsMay: false,
                    nextTierCandidates: Array.Empty<CardDefinitionId>())
            }));

        /// <summary>
        /// This block's faces, appended to milestone 8.69a's aggregate by the one
        /// <c>.Concat(...)</c> line its remarks reserve for a wave block.
        /// </summary>
        private static IEnumerable<CardDefinition> SplitAttackFaces() => SplitAttackDefinitions;

        /// <summary>
        /// Split Attack and Split Throw, which differ in Tier, Initiative, printed Defense, repeat
        /// binding and the printed "may" - and in nothing else. Both print ATTACK - RANGED with Attack
        /// 3 and Range 2, both print a Defense item, and both put their whole text at p.14's step 1.
        ///
        /// <para><b>The Movement ribbon's own text is suppressed</b>, which is p.11's rule for a
        /// secondary and not a gap: the printed sentence belongs to the primary.</para>
        /// </summary>
        private static CardDefinition BuildSplitFace(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            InitiativeValue initiative,
            int printedDefense,
            PrintedValueChannel itemIcon,
            string printedText,
            RepeatTargetBinding binding,
            bool printsMay,
            IReadOnlyList<CardDefinitionId> nextTierCandidates) => new CardDefinition(
            id,
            BrynnComponentContent.SplitAttackPrepRevision,
            displayName,
            initiative,
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(
                CardColor.Red, tier, itemIcon, nextTierCandidates),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryRangedAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 3),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 2)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged,
                    default,
                    BrynnEffectContent.SplitRepeat(
                        id, BrynnComponentContent.SplitAttackPrepRevision, binding, printsMay),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,

                    // p.14's step 1. "Target a unit in range" is the action's own printed reach, so
                    // the text that follows it is reached with the target already bound - which is
                    // the moment BR-Q5 reads the condition at, and Arien's Violent Torrent's own.
                    CardTextTiming.BeforeTheAttack),

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
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, printedDefense) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(printedText));

        /// <summary>
        /// Peak Precision. A non-Ranged Attack, so "Target a unit adjacent to you" is p.13's own
        /// reading of the band and composes nothing - milestone 8.69a's reading of High Ground's
        /// identical first sentence.
        ///
        /// <para>Its second sentence composes in two places at once, which is what the printed "and"
        /// joins: the +2 is <see cref="RaisedAttack"/> on the action, read when p.14 computes the
        /// Attack total, and the retrieval is card text at p.14's step 7. Both are gated on the same
        /// condition, BR-Q7, and each is stated where this build states that kind of rule.</para>
        /// </summary>
        private static CardDefinition BuildPeakPrecision() => new CardDefinition(
            PeakPrecision,
            BrynnComponentContent.SplitAttackPrepRevision,
            "Peak Precision",
            new InitiativeValue(10),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(
                CardColor.Red, CardTier.III, PrintedValueChannel.Radius,
                Array.Empty<CardDefinitionId>()),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 5) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged,
                    default,
                    BrynnEffectContent.PeakPrecisionRetrieval(
                        PeakPrecision, BrynnComponentContent.SplitAttackPrepRevision),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheAttack,
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
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 7) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(PeakPrecisionText));
    }
}
