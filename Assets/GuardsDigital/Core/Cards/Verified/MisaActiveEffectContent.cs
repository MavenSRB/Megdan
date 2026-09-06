using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The typed composition of Misa's six persistent texts, at milestone 8.35z29: the three
    /// Defense bonuses her Red melee Attacks raise over herself for a turn, and the three free steps
    /// her Green cards give her at the start of the next one.
    ///
    /// <para><b>These are declarations, not handlers.</b> All six register through
    /// <see cref="ActiveEffectCreationHandler"/>, the shape milestone 6.8 proved and every earlier hero
    /// reuses, and everything each card does is stated in its definition. No card name is read anywhere
    /// at runtime and nothing here asks the board a question.</para>
    ///
    /// <para><b>Six definitions and not two parameterised ones.</b>
    /// <see cref="ActiveEffectDefinition.UniquenessKey"/> is what keeps one card's occurrence from
    /// suppressing another's, and a hero who plays Challenge Accepted on one turn and Worthy Opponent
    /// on a later one has played two cards and is owed two rules - the same argument milestone 8.20
    /// recorded for Expert and Master Duelist and milestone 8.21 for Shield, Bolster and Fortify.</para>
    ///
    /// <para><b>Why the p.7 end-of-round cancellation is not declared and is nevertheless true.</b>
    /// <see cref="ActiveEffectCancellationCondition.CardsRetrieved"/> is on all six, which is p.7's
    /// Retrieve Cards step stated once: "This cancels all your Active effects, including 'Next Turn'
    /// effects." So a Focus, Discipline or Mastery played on the round's fourth turn never takes
    /// effect at all - the round ends before the next turn arrives. That is settled from source rather
    /// than special-cased here, and <c>HERO_TIGERCLAW.md</c> already records it for the one earlier
    /// face that prints the keyword.</para>
    /// </summary>
    public static class MisaActiveEffectContent
    {
        /// <summary>
        /// Rulebook p.17's three cancellations for an Active effect on your card, as every earlier hero
        /// set declares them: the effect stops applying, the controller is defeated, or the card
        /// changes state at the end of the round.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>
        /// The window Misa's three Green cards open, named once. It is not a checkpoint anything
        /// raises: <see cref="GuardsDigital.Core.Rules.Progression.BeforeActionTriggerQuery"/> reads it
        /// as the declaration that this effect wants the same pre-action window Arien's Living Tsunami
        /// already runs in.
        /// </summary>
        private static readonly TimingCheckpoint[] BeforeAPrimaryAction =
        {
            TimingCheckpoint.BeforePrimaryAction
        };

        public static EffectCapabilityId ChallengeAcceptedGuard { get; } =
            new EffectCapabilityId("misa-challenge-accepted-guard");

        public static EffectCapabilityId MatterOfHonorGuard { get; } =
            new EffectCapabilityId("misa-matter-of-honor-guard");

        public static EffectCapabilityId WorthyOpponentGuard { get; } =
            new EffectCapabilityId("misa-worthy-opponent-guard");

        public static EffectCapabilityId FocusStep { get; } =
            new EffectCapabilityId("misa-focus-step");

        public static EffectCapabilityId DisciplineStep { get; } =
            new EffectCapabilityId("misa-discipline-step");

        public static EffectCapabilityId MasteryStep { get; } =
            new EffectCapabilityId("misa-mastery-step");

        public static EffectCapabilityReference ChallengeAcceptedGuardV1 =>
            new EffectCapabilityReference(ChallengeAcceptedGuard, 1);

        public static EffectCapabilityReference MatterOfHonorGuardV1 =>
            new EffectCapabilityReference(MatterOfHonorGuard, 1);

        public static EffectCapabilityReference WorthyOpponentGuardV1 =>
            new EffectCapabilityReference(WorthyOpponentGuard, 1);

        public static EffectCapabilityReference FocusStepV1 =>
            new EffectCapabilityReference(FocusStep, 1);

        public static EffectCapabilityReference DisciplineStepV1 =>
            new EffectCapabilityReference(DisciplineStep, 1);

        public static EffectCapabilityReference MasteryStepV1 =>
            new EffectCapabilityReference(MasteryStep, 1);

        /// <summary>
        /// Challenge Accepted's rider: "After the attack: <b>This turn:</b> Gain +2 [Defense] Defense."
        ///
        /// <para><b>Two printed keywords, two different things, and only the second is here.</b>
        /// "After the attack:" is p.14's step 7 and is declared on the action as its card-text timing;
        /// "This turn:" is p.17's duration and is this definition's. So the effect is raised at step 7
        /// and lasts until the turn ends, which is what the two keywords say read in order.</para>
        ///
        /// <para><b>The bonus is unqualified, and the absence of a qualification is content.</b> Sabina's
        /// Big Sodding Gun prints "your basic attack has +2 Defense..." and needs
        /// <see cref="RuleValueApplicability.ControllerBasicAttackAction"/> to say so; Misa's face
        /// prints "Gain +2 Defense" with no clause after it, so it reaches every Defense value she
        /// composes for the rest of the turn - which is p.14's Defense Total on whichever card she
        /// answers an attack with, and the whole of her Durable role. Declaring a narrowing the card
        /// does not print would be inventing one.</para>
        ///
        /// <para>The scope is <see cref="ActiveEffectAffectedScopeKind.ControllerHero"/> because the
        /// face says "Gain", which p.10's Picking Targets makes a statement about the acting hero: "If
        /// your action is affecting you (your hero), it will say so in the card text."</para>
        /// </summary>
        public static ActiveEffectDefinition ChallengeAcceptedDefinition { get; } =
            Guard("misa-challenge-accepted-effect", "MisaChallengeAccepted",
                "misa-challenge-accepted", 2);

        /// <summary>
        /// Matter of Honor's rider, which is Challenge Accepted's sentence with one number changed:
        /// "After the attack: <b>This turn:</b> Gain +3 [Defense] Defense."
        /// </summary>
        public static ActiveEffectDefinition MatterOfHonorDefinition { get; } =
            Guard("misa-matter-of-honor-effect", "MisaMatterOfHonor", "misa-matter-of-honor", 3);

        /// <summary>
        /// Worthy Opponent's rider, the Tier III of the same sentence: "After the attack:
        /// <b>This turn:</b> Gain +5 [Defense] Defense."
        ///
        /// <para>The three faces differ in that number and in nothing else about this rule, which is
        /// why they compose through one builder. What separates the cards otherwise is their printed
        /// Attack (5, 6, 6) and Initiative, both of which live on the card rather than here.</para>
        /// </summary>
        public static ActiveEffectDefinition WorthyOpponentDefinition { get; } =
            Guard("misa-worthy-opponent-effect", "MisaWorthyOpponent", "misa-worthy-opponent", 5);

        /// <summary>
        /// Focus's whole text: "<b>Next turn:</b> Before you perform a primary action, you may move 1
        /// space."
        ///
        /// <para><b>p.17's keyword is the duration and nothing else.</b> "The text that follows the
        /// 'Next turn' keyword comes into effect at the start of the next turn and remains in effect
        /// for the duration of that entire turn." So this is
        /// <see cref="ActiveEffectDurationKind.NextTurn"/>, raised when the Skill resolves and standing
        /// through the whole of Misa's following turn - or through none of it, when p.7's Retrieve
        /// Cards step arrives first and cancels every Active effect she has.</para>
        ///
        /// <para><b>The window is a declaration, not a raised checkpoint.</b>
        /// <see cref="TimingCheckpoint.BeforePrimaryAction"/> in the trigger list is what tells
        /// <see cref="GuardsDigital.Core.Rules.Progression.BeforeActionTriggerQuery"/> that this effect
        /// wants the pre-action window milestone 8.19a built for Arien's Living Tsunami. Everything
        /// after that is that machinery's: the offer, the pause, the answer, and the spent latch.</para>
        ///
        /// <para><b>It fires once, and the ruling says why.</b> The user's 2026-09-06 answer to Misa's
        /// M-05 is "It says action, not actions. So just the once." p.10's Repeat can put a second
        /// primary action instance in the same turn, and this effect does not re-arm for it: the latch
        /// is keyed on the source, and a source already consumed this turn is not offered again.</para>
        ///
        /// <para>The distance travels on the triggered effect rather than on this declaration, because
        /// what differs between the three cards is how far she may move and not when.</para>
        /// </summary>
        public static ActiveEffectDefinition FocusDefinition { get; } =
            Step("misa-focus-effect", "MisaFocus", "misa-focus",
                MisaEffectContent.FocusStepEffectId);

        /// <summary>
        /// Discipline's whole text - Focus's sentence with "up to 2 spaces" - and the Tier II of the
        /// same clause.
        /// </summary>
        public static ActiveEffectDefinition DisciplineDefinition { get; } =
            Step("misa-discipline-effect", "MisaDiscipline", "misa-discipline",
                MisaEffectContent.DisciplineStepEffectId);

        /// <summary>
        /// Mastery's whole text - "up to 3 spaces" - and the Tier III of the same clause. The three
        /// differ in the distance and in their printed Initiative and item icon, and in nothing
        /// else.
        /// </summary>
        public static ActiveEffectDefinition MasteryDefinition { get; } =
            Step("misa-mastery-effect", "MisaMastery", "misa-mastery",
                MisaEffectContent.MasteryStepEffectId);

        /// <summary>
        /// Every Active-effect definition Misa's set declares, in the order the printed-value table
        /// lists the cards that raise them.
        /// </summary>
        public static IReadOnlyList<ActiveEffectDefinition> AllDefinitions { get; } =
            Array.AsReadOnly(new[]
            {
                ChallengeAcceptedDefinition,
                MatterOfHonorDefinition,
                WorthyOpponentDefinition,
                FocusDefinition,
                DisciplineDefinition,
                MasteryDefinition
            });

        /// <summary>
        /// The six creation handlers Misa's content contributes to the shared catalog.
        ///
        /// <para><b>Not registered by this milestone, deliberately.</b> Misa is not dealt yet, and
        /// milestone 7.9's deferral gate refuses a contract in the pinned registration list that no
        /// dealt card can start. Integration wires this into <c>MatchContentCatalogs</c> in the same
        /// pass that deals her; until then the list exists so that a fixture can build a catalog which
        /// resolves her definitions, which is exactly what her tests do.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(ChallengeAcceptedGuardV1, ChallengeAcceptedDefinition),
            new ActiveEffectCreationHandler(MatterOfHonorGuardV1, MatterOfHonorDefinition),
            new ActiveEffectCreationHandler(WorthyOpponentGuardV1, WorthyOpponentDefinition),
            new ActiveEffectCreationHandler(FocusStepV1, FocusDefinition),
            new ActiveEffectCreationHandler(DisciplineStepV1, DisciplineDefinition),
            new ActiveEffectCreationHandler(MasteryStepV1, MasteryDefinition)
        };

        /// <summary>
        /// The "This turn: Gain +N Defense" rule, written once because the three faces that print it
        /// differ only in N.
        /// </summary>
        private static ActiveEffectDefinition Guard(
            string definitionId, string effectKind, string uniquenessKey, int defense) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                effectKind,
                uniquenessKey,
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForValue(PrintedValueChannel.Defense, defense) });

        /// <summary>
        /// The "Next turn: Before you perform a primary action, you may move up to N spaces" rule,
        /// written once because the three faces that print it differ only in N - and N is on the
        /// triggered effect, not here.
        ///
        /// <para><b>Why it carries across the round boundary and the guards above do not.</b> A
        /// <c>Next turn:</c> effect is raised on one turn and applies on another, so
        /// <see cref="ActiveEffectDefinition"/>'s carry flag is what stops the lifecycle expiring it at
        /// the end of the turn that created it. It still dies at p.7's Retrieve Cards step, which is
        /// the cancellation every Active effect of hers declares, so "carries across the round
        /// boundary" is a statement about turns and not about rounds.</para>
        /// </summary>
        private static ActiveEffectDefinition Step(
            string definitionId,
            string effectKind,
            string uniquenessKey,
            EffectDefinitionId triggeredEffect) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurn,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.TurnStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                true,
                effectKind,
                uniquenessKey,
                BeforeAPrimaryAction,
                null,
                null,
                default,
                SourceTeamRelation.None,
                new EffectDefinitionReference(triggeredEffect, MisaCardContent.ContentRevision));
    }
}
