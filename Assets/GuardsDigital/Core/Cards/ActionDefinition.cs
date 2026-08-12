using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    public sealed class ActionDefinition
    {
        public ActionDefinition(
            ActionDefinitionId id,
            ActionKind kind,
            ActionDefinitionOrigin origin,
            IEnumerable<PrintedValueDefinition> baseValues,
            ActionDefinitionPolicy defaultPolicy)
            : this(
                id,
                kind,
                origin,
                baseValues,
                defaultPolicy,
                AttackRangeKind.None,
                default)
        {
        }

        public ActionDefinition(
            ActionDefinitionId id,
            ActionKind kind,
            ActionDefinitionOrigin origin,
            IEnumerable<PrintedValueDefinition> baseValues,
            ActionDefinitionPolicy defaultPolicy,
            AttackRangeKind attackRange,
            ActionCardStateTransition sourceCardStateTransition)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("Action definition requires a defined ID.", nameof(id));
            }

            if (!Enum.IsDefined(typeof(ActionKind), kind) || kind == ActionKind.None)
            {
                throw new ArgumentException("Action definition requires an action kind.", nameof(kind));
            }

            if (!Enum.IsDefined(typeof(ActionDefinitionOrigin), origin) || origin == ActionDefinitionOrigin.None)
            {
                throw new ArgumentException("Action definition requires an origin.", nameof(origin));
            }

            if (baseValues == null)
            {
                throw new ArgumentNullException(nameof(baseValues));
            }

            if (!Enum.IsDefined(typeof(AttackRangeKind), attackRange))
            {
                throw new ArgumentException("Action definition requires a valid Attack range classification.", nameof(attackRange));
            }

            ValidatePolicy(origin, defaultPolicy);

            Id = id;
            Kind = kind;
            Origin = origin;
            var materializedBaseValues = baseValues.ToArray();
            BaseValues = Array.AsReadOnly(materializedBaseValues);
            DefaultPolicy = defaultPolicy;
            AttackRange = attackRange;
            SourceCardStateTransition = sourceCardStateTransition;

            if (BaseValues.Select(value => value.Channel).Distinct().Count() != BaseValues.Count)
            {
                throw new ArgumentException("Action definition base values must have unique channels.", nameof(baseValues));
            }

            ValidateActionShape(baseValues);
        }

        public ActionDefinitionId Id { get; }

        public ActionKind Kind { get; }

        public ActionDefinitionOrigin Origin { get; }

        public IReadOnlyList<PrintedValueDefinition> BaseValues { get; }

        public ActionDefinitionPolicy DefaultPolicy { get; }

        public AttackRangeKind AttackRange { get; }

        public ActionCardStateTransition SourceCardStateTransition { get; }

        private void ValidateActionShape(IEnumerable<PrintedValueDefinition> baseValues)
        {
            if (Kind == ActionKind.Movement)
            {
                ValidateSinglePrintedValue(PrintedValueChannel.Movement, baseValues);
            }
            else if (Kind == ActionKind.Attack)
            {
                if (AttackRange == AttackRangeKind.None)
                {
                    throw new ArgumentException("Attack action definitions require a range classification.", nameof(AttackRange));
                }

                var expectedChannels = AttackRange == AttackRangeKind.Ranged
                    ? new[] { PrintedValueChannel.Attack, PrintedValueChannel.Range }
                    : new[] { PrintedValueChannel.Attack };
                if (BaseValues.Count != expectedChannels.Length ||
                    expectedChannels.Any(channel => BaseValues.All(value => value.Channel != channel)))
                {
                    throw new ArgumentException(
                        AttackRange == AttackRangeKind.Ranged
                            ? "Ranged Attack action definitions require exactly one Attack and one Range printed value."
                            : "Non-Ranged Attack action definitions require exactly one Attack printed value.",
                        nameof(baseValues));
                }

                if (AttackRange == AttackRangeKind.Ranged &&
                    BaseValues.Single(value => value.Channel == PrintedValueChannel.Range).Amount < 1)
                {
                    throw new ArgumentException(
                        "Ranged Attack action definitions require a positive printed Range value.",
                        nameof(baseValues));
                }
            }
            else if (Kind == ActionKind.Defense)
            {
                ValidateSinglePrintedValue(PrintedValueChannel.Defense, baseValues);
                if (Origin != ActionDefinitionOrigin.Reaction)
                {
                    throw new ArgumentException("Defense action definitions must be reactions.", nameof(Origin));
                }

                if (!SourceCardStateTransition.Equals(new ActionCardStateTransition(
                        CardLocation.Hand,
                        CardLocation.DiscardPile)))
                {
                    throw new ArgumentException(
                        "Defense action definitions must require an in-hand card and declare discard as their cost/state transition.",
                        nameof(SourceCardStateTransition));
                }
            }
            else if (Kind == ActionKind.FastTravel)
            {
                if (Origin != ActionDefinitionOrigin.Replacement || BaseValues.Count != 0)
                {
                    throw new ArgumentException(
                        "Fast Travel must be a value-free replacement action definition.",
                        nameof(baseValues));
                }
            }

            if (Kind != ActionKind.Attack && AttackRange != AttackRangeKind.None)
            {
                throw new ArgumentException("Only Attack action definitions may declare an Attack range classification.", nameof(AttackRange));
            }
        }

        private void ValidateSinglePrintedValue(
            PrintedValueChannel expectedChannel,
            IEnumerable<PrintedValueDefinition> baseValues)
        {
            if (BaseValues.Count != 1 || BaseValues[0].Channel != expectedChannel)
            {
                throw new ArgumentException(
                    $"{Kind} action definitions currently support exactly one {expectedChannel} printed value.",
                    nameof(baseValues));
            }
        }

        private static void ValidatePolicy(ActionDefinitionOrigin origin, ActionDefinitionPolicy defaultPolicy)
        {
            if (!defaultPolicy.IsDefined)
            {
                throw new ArgumentException("Action definitions require a defined default policy.", nameof(defaultPolicy));
            }

            if (origin == ActionDefinitionOrigin.PrintedPrimary &&
                defaultPolicy.CardTextApplication != CardTextApplicationPolicy.Applies)
            {
                throw new ArgumentException("Printed Primary actions must default to applying card text.", nameof(defaultPolicy));
            }

            if ((origin == ActionDefinitionOrigin.PrintedSecondary ||
                    origin == ActionDefinitionOrigin.GeneralRuleSecondary ||
                    origin == ActionDefinitionOrigin.Replacement) &&
                defaultPolicy.CardTextApplication != CardTextApplicationPolicy.Suppressed)
            {
                throw new ArgumentException(
                    "Printed Secondary, General Rule Secondary, and replacement actions must default to suppressing source-card text.",
                    nameof(defaultPolicy));
            }

            if ((origin == ActionDefinitionOrigin.Replacement ||
                    origin == ActionDefinitionOrigin.Reaction) &&
                defaultPolicy.CanBeReplaced)
            {
                throw new ArgumentException("Replacement and reaction actions cannot default to being replaced.", nameof(defaultPolicy));
            }
        }
    }
}
