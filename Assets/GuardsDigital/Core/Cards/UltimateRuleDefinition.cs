using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards
{
    /// <summary>The typed completed-action fact that may awaken a permanent Ultimate ability.</summary>
    public enum UltimateTriggerCondition
    {
        None = 0,
        AfterControllerPerformsBasicSkill = 1
    }

    /// <summary>
    /// The permanent ability one Ultimate card prints, as immutable definition content.
    ///
    /// Rulebook p.16 states the whole of its lifetime in one sentence: "It provides you with a powerful
    /// permanent ability that is always in effect (as long as your hero is on the board)." Everything
    /// this type does follows from that sentence rather than from a modelling preference.
    ///
    /// <para><b>The applicability is not content.</b> A token property declares its own applicability
    /// because different tokens can have different ones; p.16 states one applicability for every
    /// Ultimate there is, so <see cref="ApplicabilityKind"/> is a constant rather than a constructor
    /// argument. A per-definition field here would be a place for a future card to state something the
    /// page does not allow it to state.</para>
    ///
    /// <para><b>The rule is a declaration, not a lifetime.</b> Like
    /// <c>TokenPropertyDefinition</c>, this carries a typed <see cref="RuleModification"/> and nothing
    /// about when it applies. <c>UltimateUnlockQuery</c> binds it to the hero it affects and the board
    /// question that decides whether it applies right now, and nothing is stored: an unlocked Ultimate
    /// supplies its rule again on restore because the card is still beside the dashboard and the
    /// trusted definition still declares this.</para>
    /// </summary>
    public sealed class UltimateRuleDefinition
    {
        /// <summary>
        /// p.16's one applicability, shared by every Ultimate: the rule applies exactly while its
        /// controlling hero is on the board. It is the string a derived
        /// <c>ContinuousRuleSource</c> carries, so a payload records which question was asked rather
        /// than leaving the answer implicit in the source kind.
        /// </summary>
        public const string ApplicabilityKind = "WhileControllerHeroOnBoard";

        public UltimateRuleDefinition(string ruleId, string ruleKind, RuleModification modification)
            : this(
                ruleId,
                ruleKind,
                modification ??
                    throw new ArgumentNullException(
                        nameof(modification),
                        "An Ultimate's whole printed content is its permanent ability (rulebook p.16); " +
                        "a declaration with no rule would be a card that does nothing."),
                PrintedTextComposition.None,
                Array.Empty<PrintedValueDefinition>())
        {
        }

        private UltimateRuleDefinition(
            string ruleId,
            string ruleKind,
            RuleModification modification,
            PrintedTextComposition textComposition,
            IEnumerable<PrintedValueDefinition> printedValues,
            EffectDefinitionReference? triggeredEffect = null,
            IEnumerable<TimingCheckpoint> triggerCheckpoints = null,
            UltimateTriggerCondition triggerCondition = UltimateTriggerCondition.None,
            BeforeActionTrigger beforeAction = default)
        {
            BeforeAction = beforeAction;
            if (string.IsNullOrWhiteSpace(ruleId) || string.IsNullOrWhiteSpace(ruleKind))
            {
                throw new ArgumentException("An Ultimate's ability requires stable identity and a rule kind.");
            }

            RuleId = ruleId;
            RuleKind = ruleKind;
            Modification = modification;
            TextComposition = textComposition;
            TriggeredEffect = triggeredEffect;
            TriggerCondition = triggerCondition;
            TriggerCheckpoints = Array.AsReadOnly(
                (triggerCheckpoints ?? Enumerable.Empty<TimingCheckpoint>()).ToArray());

            // Milestone 8.19a. A before-action trigger runs an effect too, and it runs it at a moment
            // the lifecycle has no checkpoint for, so it satisfies the pairing below by declaring a
            // trigger rather than a checkpoint. The two shapes stay mutually exclusive: one Ultimate
            // prints one ability, and an ability that both preceded an action and followed one would be
            // two sentences the page does not print.
            var isCheckpointTriggered = TriggerCheckpoints.Count > 0;
            if (beforeAction.IsDeclared && isCheckpointTriggered)
            {
                throw new ArgumentException(
                    "An Ultimate declares one trigger: a checkpoint after an action, or a start " +
                    "before one.", nameof(beforeAction));
            }

            if (!Enum.IsDefined(typeof(UltimateTriggerCondition), triggerCondition) ||
                triggeredEffect.HasValue != (isCheckpointTriggered || beforeAction.IsDeclared) ||
                isCheckpointTriggered != (triggerCondition != UltimateTriggerCondition.None) ||
                (triggeredEffect.HasValue && !triggeredEffect.Value.IsDefined) ||
                TriggerCheckpoints.Any(value => value != TimingCheckpoint.ActionEnded) ||
                TriggerCheckpoints.Distinct().Count() != TriggerCheckpoints.Count)
            {
                throw new ArgumentException(
                    "A triggered Ultimate requires one defined effect and unique supported checkpoints.");
            }

            var values = (printedValues ?? throw new ArgumentNullException(nameof(printedValues)))
                .ToArray();

            // p.9's one icon, on the one card that is not an action. High Voltage prints a Radius its
            // ability reads, and 2026-08-21's approved reading of ledger question 12 makes that icon an
            // item's target - so the value has to be printed content here rather than a number written
            // into whatever composes the ability later.
            if (values.Length > 1 ||
                values.Any(value =>
                    value.Channel != PrintedValueChannel.Radius &&
                    value.Channel != PrintedValueChannel.Range))
            {
                throw new ArgumentException(
                    "An Ultimate prints at most one of rulebook p.9's Range or Radius icons.",
                    nameof(printedValues));
            }

            if (values.Any(value => value.Amount < 1))
            {
                throw new ArgumentException(
                    "A printed Range or Radius icon states a positive value.", nameof(printedValues));
            }

            PrintedValues = Array.AsReadOnly(values);
        }

        /// <summary>
        /// Milestone 8.3's Ultimate whose printed ability is transcribed but not yet composed, naming
        /// the milestone that owes the composition.
        ///
        /// The invariant the ordinary constructor states is unchanged in substance - an Ultimate whose
        /// whole printed content is an ability may not print no ability - and this is the one way to
        /// satisfy it without a typed rule: by saying out loud which milestone owes one. Wasp's High
        /// Voltage needs it because its ability is a triggered one ("Each time after you perform a basic
        /// skill...") and none of <see cref="RuleModification"/>'s five families can express a trigger.
        /// Milestone 8.12 gives the continuous-source declaration a typed effect reference and 8.13
        /// composes this card onto it; until then the card carries its printed Radius, its transcribed
        /// words on the definition, and an explicit debt - never a stand-in rule chosen to fill the
        /// argument.
        /// </summary>
        public static UltimateRuleDefinition AwaitingComposition(
            string ruleId,
            string ruleKind,
            PrintedTextComposition textComposition,
            IEnumerable<PrintedValueDefinition> printedValues)
        {
            if (!textComposition.IsAwaited)
            {
                throw new ArgumentException(
                    "An Ultimate with no typed rule states the milestone that owes its composition.",
                    nameof(textComposition));
            }

            return new UltimateRuleDefinition(ruleId, ruleKind, null, textComposition, printedValues);
        }

        /// <summary>
        /// A permanent Ultimate ability whose trusted declaration enters the ordinary effect frame at
        /// its named checkpoint. It is a continuous source rather than an Active effect, so its
        /// lifetime remains derived from the unlocked card and the controller being on the board.
        /// </summary>
        public static UltimateRuleDefinition TriggeredAbility(
            string ruleId,
            string ruleKind,
            EffectDefinitionReference triggeredEffect,
            UltimateTriggerCondition triggerCondition,
            IEnumerable<TimingCheckpoint> triggerCheckpoints,
            IEnumerable<PrintedValueDefinition> printedValues) =>
            new UltimateRuleDefinition(
                ruleId, ruleKind, null, PrintedTextComposition.None, printedValues,
                triggeredEffect, triggerCheckpoints, triggerCondition);

        /// <summary>
        /// Milestone 8.19a. A permanent Ultimate ability that runs before its controller performs an
        /// action of a named kind, which is Living Tsunami's shape: "Once per turn, before performing
        /// an Attack action, you may move 1 space."
        ///
        /// <para>It is a third factory rather than a parameter on <see cref="TriggeredAbility"/>
        /// because the two answer different questions. That one names a lifecycle checkpoint and a
        /// completed-action condition, both of which describe something that has already happened; this
        /// one names an action that has not started and must start unchanged afterwards, so what it
        /// declares is a kind and a frequency and nothing about checkpoints at all.</para>
        /// </summary>
        public static UltimateRuleDefinition BeforeActionAbility(
            string ruleId,
            string ruleKind,
            EffectDefinitionReference triggeredEffect,
            BeforeActionTrigger beforeAction,
            IEnumerable<PrintedValueDefinition> printedValues)
        {
            if (!beforeAction.IsDeclared)
            {
                throw new ArgumentException(
                    "A before-action Ultimate declares the action kind it precedes.", nameof(beforeAction));
            }

            return new UltimateRuleDefinition(
                ruleId, ruleKind, null, PrintedTextComposition.None, printedValues,
                triggeredEffect, null, UltimateTriggerCondition.None, beforeAction);
        }

        /// <summary>This ability's stable identity inside its card, and the derived source's identity suffix.</summary>
        public string RuleId { get; }

        public string RuleKind { get; }

        /// <summary>
        /// The typed rule this Ultimate supplies while its hero is on the board, or null when
        /// <see cref="TextComposition"/> names the milestone that owes it.
        /// </summary>
        public RuleModification Modification { get; }

        public EffectDefinitionReference? TriggeredEffect { get; }

        public UltimateTriggerCondition TriggerCondition { get; }

        public IReadOnlyList<TimingCheckpoint> TriggerCheckpoints { get; }

        /// <summary>
        /// Milestone 8.19a. `before-action-trigger@1`, or <see cref="BeforeActionTrigger.None"/> on
        /// every Ultimate that declares no such trigger - which is every one in this build before
        /// Arien's.
        /// </summary>
        public BeforeActionTrigger BeforeAction { get; }

        /// <summary>
        /// Stable effect-frame identity for a permanent ability. An Ultimate is not an action, but the
        /// shared frame addresses its source text through the same typed slot used by action text.
        /// </summary>
        public ActionDefinitionId EffectSourceId => new ActionDefinitionId(RuleId);

        /// <summary>
        /// Milestone 8.3. Whether this Ultimate's printed ability still owes its typed composition, and
        /// to which milestone. <see cref="PrintedTextComposition.None"/> whenever
        /// <see cref="Modification"/> is present.
        /// </summary>
        public PrintedTextComposition TextComposition { get; }

        /// <summary>
        /// Milestone 8.3. The p.9 Range or Radius icon this Ultimate prints, or empty. Empty for every
        /// Ultimate built before 8.3, which printed none.
        /// </summary>
        public IReadOnlyList<PrintedValueDefinition> PrintedValues { get; }

        public override string ToString() =>
            $"{RuleId} ({RuleKind}): {(Modification == null ? TextComposition.ToString() : Modification.ToString())}";
    }
}
