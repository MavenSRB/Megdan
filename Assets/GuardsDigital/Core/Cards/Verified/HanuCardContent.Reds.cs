using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's six faces whose clauses milestone 8.71c owns: his Gold basic Fight and
    /// Flight and the five Reds - Helping Hand, Even the Odds, Outnumber, Trusted Sidekick and Pile On.
    /// The other thirteen belong to the blocks that own their clauses, and 8.71g states the whole set
    /// under one real content revision.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_HANU.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.71: Fight and Flight
    /// (<c>hanu__7635525</c>) Gold, Basic, Initiative <b>13</b>, Defense <b>1</b>, Movement <b>2</b>,
    /// primary BASIC ATTACK with Attack <b>2</b> and NO Range icon, no item; Helping Hand
    /// (<c>hanu3__7635516</c>) Red Tier I, 9/4/5, ATTACK - RANGED, Attack <b>3</b>, Range <b>3</b>, no
    /// item; Even the Odds (<c>hanu4__7635517</c>) Red Tier II, 9/4/5, Attack <b>4</b>, Range <b>4</b>,
    /// Initiative item; Outnumber (<c>hanu5__7635520</c>) Red Tier II, 9/5/5, Attack <b>5</b>, Range
    /// <b>4</b>, Defense item; Trusted Sidekick (<c>hanu6__7635518</c>) Red Tier III, 10/5/5, Attack
    /// <b>4</b>, Range <b>4</b>, Movement item; Pile On (<c>hanu7__7635512</c>) Red Tier III, 10/6/5,
    /// Attack <b>5</b>, Range <b>4</b>, Radius item. HA-06 is the rule the absent icons follow - "A
    /// missing icon is absent, never zero" - so Fight and Flight declares no Range at all and is
    /// <see cref="AttackRangeKind.NonRanged"/> rather than a Ranged Attack of one.</para>
    ///
    /// <para><b>The ribbon, stated the way the shard's table states it.</b> Every dealt face except the
    /// Ultimate prints Defense, and every face except Hurry up! and the Ultimate prints Movement, so
    /// all six here carry both secondaries. p.11 suppresses each Movement ribbon's own card text -
    /// the sentence belongs to the primary on all six - which is what
    /// <see cref="CardTextApplicationPolicy.Suppressed"/> says on the two secondaries.</para>
    ///
    /// <para><b>Only this block's progression rows are stated</b>, and each names only successors this
    /// block composes: Helping Hand climbs to Even the Odds or Outnumber, and either of those to
    /// Trusted Sidekick or Pile On. 8.71g states the three ladders whole, beside the Blues' and the
    /// Greens'.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's gate
    /// rather than an oversight - see <see cref="HanuComponentContent"/>. The revision is
    /// <see cref="RedPrepRevision"/>, a placeholder that is deliberately not a valid content-revision
    /// id: 8.71g replaces it with the one real string his whole set is stated under.</para>
    /// </summary>
    public static partial class HanuCardContent
    {
        /// <summary>
        /// This block's placeholder revision. It is not a valid real id, on the wave-one rule a
        /// placeholder that could be mistaken for one broke: 8.71g states his set under
        /// <c>hanu-8-71g-rev-1</c> and this string exists only so the six definitions below compile and
        /// can be measured on their own branch.
        /// </summary>
        private const string RedPrepRevision = "hanu-8-71c-prep";

        // -----------------------------------------------------------------------------------------
        // Identities
        // -----------------------------------------------------------------------------------------

        /// <summary>His Gold basic: "Target a unit adjacent to you. If the target is not defeated,
        /// After the attack: If able, move 3 spaces in a straight line."</summary>
        public static CardDefinitionId FightAndFlight { get; } =
            new CardDefinitionId("hanu-fight-and-flight");

        /// <summary>His Red Tier I, the modal at one bullet with a hero second bullet.</summary>
        public static CardDefinitionId HelpingHand { get; } =
            new CardDefinitionId("hanu-helping-hand");

        /// <summary>His Red Tier II, printing Helping Hand's sentence over higher values.</summary>
        public static CardDefinitionId EvenTheOdds { get; } =
            new CardDefinitionId("hanu-even-the-odds");

        /// <summary>His other Red Tier II, whose second bullet names a MINION.</summary>
        public static CardDefinitionId Outnumber { get; } =
            new CardDefinitionId("hanu-outnumber");

        /// <summary>His Red Tier III at one or both bullets, the second excluding his own ring.</summary>
        public static CardDefinitionId TrustedSidekick { get; } =
            new CardDefinitionId("hanu-trusted-sidekick");

        /// <summary>His other Red Tier III at one or both bullets, on minions, RULING 19's face.</summary>
        public static CardDefinitionId PileOn { get; } = new CardDefinitionId("hanu-pile-on");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed
        // -----------------------------------------------------------------------------------------

        public const string FightAndFlightText =
            "Target a unit adjacent to you. If the target is not defeated, After the attack: If able, " +
            "move 3 spaces in a straight line.";

        public const string HelpingHandText =
            "Choose one - • Target a unit adjacent to you. • Target a hero in range, adjacent to your " +
            "friendly hero.";

        /// <summary>Even the Odds prints Helping Hand's sentence exactly; HA-06 records the values.</summary>
        public const string EvenTheOddsText = HelpingHandText;

        public const string OutnumberText =
            "Choose one - • Target a unit adjacent to you. • Target a minion in range, adjacent to " +
            "your friendly hero.";

        public const string TrustedSidekickText =
            "Choose one, or both, in any order - • Target a unit adjacent to you. • Target a hero in " +
            "range, adjacent to your friendly hero and not adjacent to you.";

        public const string PileOnText =
            "Choose one, or both, in any order - • Target a unit adjacent to you. • Target a minion " +
            "in range, adjacent to your friendly hero.";

        // -----------------------------------------------------------------------------------------
        // The six definitions
        // -----------------------------------------------------------------------------------------

        /// <summary>The six faces this block composes, in his shard's inventory order.</summary>
        public static IEnumerable<CardDefinition> RedDefinitions() => LazyReds.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyReds =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildFightAndFlight(),
                BuildRangedModal(
                    HelpingHand, "Helping Hand", CardTier.I, initiative: 9, printedDefense: 4,
                    printedMovement: 5, printedAttack: 3, printedRange: 3,
                    itemIcon: PrintedValueChannel.None, printedText: HelpingHandText,
                    nextTierCandidates: new[] { EvenTheOdds, Outnumber },
                    body: HanuEffectContent.HeroModalChoosingOne(HelpingHand, RedPrepRevision)),
                BuildRangedModal(
                    EvenTheOdds, "Even the Odds", CardTier.II, initiative: 9, printedDefense: 4,
                    printedMovement: 5, printedAttack: 4, printedRange: 4,
                    itemIcon: PrintedValueChannel.Initiative, printedText: EvenTheOddsText,
                    nextTierCandidates: new[] { TrustedSidekick, PileOn },
                    body: HanuEffectContent.HeroModalChoosingOne(EvenTheOdds, RedPrepRevision)),
                BuildRangedModal(
                    Outnumber, "Outnumber", CardTier.II, initiative: 9, printedDefense: 5,
                    printedMovement: 5, printedAttack: 5, printedRange: 4,
                    itemIcon: PrintedValueChannel.Defense, printedText: OutnumberText,
                    nextTierCandidates: new[] { TrustedSidekick, PileOn },
                    body: HanuEffectContent.MinionModalChoosingOne(Outnumber, RedPrepRevision)),
                BuildRangedModal(
                    TrustedSidekick, "Trusted Sidekick", CardTier.III, initiative: 10,
                    printedDefense: 5, printedMovement: 5, printedAttack: 4, printedRange: 4,
                    itemIcon: PrintedValueChannel.Movement, printedText: TrustedSidekickText,
                    nextTierCandidates: Array.Empty<CardDefinitionId>(),
                    body: HanuEffectContent.HeroModalChoosingBoth(TrustedSidekick, RedPrepRevision)),
                BuildRangedModal(
                    PileOn, "Pile On", CardTier.III, initiative: 10, printedDefense: 6,
                    printedMovement: 5, printedAttack: 5, printedRange: 4,
                    itemIcon: PrintedValueChannel.Radius, printedText: PileOnText,
                    nextTierCandidates: Array.Empty<CardDefinitionId>(),
                    body: HanuEffectContent.MinionModalChoosingBoth(PileOn, RedPrepRevision))
            }));

        /// <summary>
        /// Fight and Flight. p.9's Gold band with a BASIC ATTACK primary and no Range icon, so p.13
        /// reads it as adjacency by rule rather than as a Range of one.
        ///
        /// <para><b>Its first sentence composes nothing</b>, which is Brynn's High Ground reading of
        /// the identical words: "Target a unit adjacent to you" is what p.13 already says about a
        /// non-Ranged Attack, so the action declares no target policy of its own and the text is not
        /// what binds the target. That is why the timing is
        /// <see cref="CardTextTiming.AfterTheAttack"/> and not the modals' before-the-attack: what the
        /// text does is at p.14's step 7, once the page knows whether step 6 removed anybody.</para>
        /// </summary>
        private static CardDefinition BuildFightAndFlight() => new CardDefinition(
            FightAndFlight,
            RedPrepRevision,
            "Fight and Flight",
            new InitiativeValue(13),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryBasicAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged,
                    default,
                    HanuEffectContent.FightAndFlight(FightAndFlight, RedPrepRevision),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheAttack),

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
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(FightAndFlightText));

        /// <summary>
        /// The five graded Reds, which differ in Tier, values, item icon, printed sentence and which
        /// modal body they name - and in nothing else.
        ///
        /// <para><b>The text is BEFORE the attack and the target is bound by it</b>, which is Bain's
        /// Hand Crossbow timing and Brynn's Familiar Ground's: the action reaches p.14's step 1 with no
        /// target, the modal answers with bullets, and each chosen bullet's own p.14 then binds its own
        /// unit against the board as that sequence finds it.
        /// <see cref="PrintedAttackTargetPolicy.BoundByCardText"/> is how the definition says so.</para>
        /// </summary>
        private static CardDefinition BuildRangedModal(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int initiative,
            int printedDefense,
            int printedMovement,
            int printedAttack,
            int printedRange,
            PrintedValueChannel itemIcon,
            string printedText,
            IReadOnlyList<CardDefinitionId> nextTierCandidates,
            Effects.EffectDefinition body) => new CardDefinition(
            id,
            RedPrepRevision,
            displayName,
            new InitiativeValue(initiative),
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
                        new PrintedValueDefinition(PrintedValueChannel.Attack, printedAttack),
                        new PrintedValueDefinition(PrintedValueChannel.Range, printedRange)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged,
                    default,
                    body,
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
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, printedMovement) },
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

        /// <summary>One of this block's six faces by id, for the fixtures that name a card.</summary>
        internal static CardDefinition Definition(CardDefinitionId id) =>
            RedDefinitions().Single(definition => definition.Id.Equals(id));

        // MERGE NOTE: this partial's MERGE BLOCK was dropped at the squash-merge; the surviving
        // aggregate and the shared action identities live in HanuCardContent.CoMoves.cs.
    }
}
