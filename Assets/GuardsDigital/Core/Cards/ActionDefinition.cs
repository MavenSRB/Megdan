using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

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
            ActionCardStateTransition sourceCardStateTransition,
            EffectDefinition effectRoot = null)
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
            EffectRoot = effectRoot;

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

        /// <summary>
        /// The effect this action executes, for kinds that resolve through the typed effect frame.
        /// Null for Movement, Attack, Defense, Hold, Fast Travel, and Clear, which have their own algorithms.
        /// </summary>
        public EffectDefinition EffectRoot { get; }

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
            else if (Kind == ActionKind.FastTravel || Kind == ActionKind.Clear)
            {
                if (Origin != ActionDefinitionOrigin.Replacement || BaseValues.Count != 0)
                {
                    throw new ArgumentException(
                        $"{Kind} must be a value-free replacement action definition.",
                        nameof(baseValues));
                }
            }

            else if (Kind == ActionKind.Skill)
            {
                if (EffectRoot == null)
                {
                    throw new ArgumentException(
                        "Skill action definitions require an effect root.", nameof(EffectRoot));
                }

                if (Origin != ActionDefinitionOrigin.PrintedPrimary &&
                    Origin != ActionDefinitionOrigin.PrintedSecondary)
                {
                    throw new ArgumentException(
                        "Skill action definitions are printed on their card.", nameof(Origin));
                }

                // A Skill carries no printed combat value: what it does is its effect, and the effect
                // is data the resolver walks rather than a number an algorithm consumes. Milestone
                // 7.2b admits the one exception rulebook p.9 prints, and only that one: "Some primary
                // actions have a Range or Radius icon and a value on the right side of the card", and
                // "the value on the Range/Radius icon determines the maximum Range/Radius of your
                // action". A Radius is not a combat value the Skill's algorithm consumes - a Skill has
                // none - it is the scope its effect asks about, so it is printed content the effect
                // reads rather than a number written into the effect's text.
                if (BaseValues.Any(value => value.Channel != PrintedValueChannel.Radius))
                {
                    throw new ArgumentException(
                        "A Skill action definition prints no value other than rulebook p.9's Radius.",
                        nameof(baseValues));
                }

                if (BaseValues.Count > 0)
                {
                    // p.9 prints the icon for a primary action, and the same page makes the Ranged
                    // subtype the Range icon's own: "All other actions are non-ranged, including those
                    // with a Radius icon." A Skill declares no Attack range classification at all,
                    // which the whole-definition rule below already enforces, so a Radius-printing
                    // Skill is non-Ranged by construction.
                    if (Origin != ActionDefinitionOrigin.PrintedPrimary)
                    {
                        throw new ArgumentException(
                            "Rulebook p.9 prints a Radius icon for a primary action.", nameof(Origin));
                    }

                    if (BaseValues.Single().Amount < 1)
                    {
                        throw new ArgumentException(
                            "A printed Radius icon states a positive value; p.9's smallest is 1, which " +
                            "reaches adjacent targets only.",
                            nameof(baseValues));
                    }
                }
            }

            if (Kind != ActionKind.Skill && EffectRoot != null)
            {
                throw new ArgumentException(
                    "Only Skill action definitions execute through the typed effect frame.", nameof(EffectRoot));
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
