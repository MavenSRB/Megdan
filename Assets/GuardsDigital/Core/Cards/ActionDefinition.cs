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

            ValidatePolicy(origin, defaultPolicy);

            Id = id;
            Kind = kind;
            Origin = origin;
            var materializedBaseValues = baseValues.ToArray();
            BaseValues = Array.AsReadOnly(materializedBaseValues);
            DefaultPolicy = defaultPolicy;

            if (BaseValues.Select(value => value.Channel).Distinct().Count() != BaseValues.Count)
            {
                throw new ArgumentException("Action definition base values must have unique channels.", nameof(baseValues));
            }

            if (Kind == ActionKind.Movement)
            {
                if (BaseValues.Count != 1 || BaseValues[0].Channel != PrintedValueChannel.Movement)
                {
                    throw new ArgumentException("Movement action definitions currently support exactly one Movement printed value.", nameof(baseValues));
                }
            }
        }

        public ActionDefinitionId Id { get; }

        public ActionKind Kind { get; }

        public ActionDefinitionOrigin Origin { get; }

        public IReadOnlyList<PrintedValueDefinition> BaseValues { get; }

        public ActionDefinitionPolicy DefaultPolicy { get; }

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
