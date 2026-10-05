using System;
using System.Collections.Generic;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// A typed, applied action fact. An Optional parent decides whether the player
    /// actually laughs; this capability is never executed when that choice is declined.
    /// </summary>
    public static class LaughterCapabilities
    {
        public static EffectCapabilityReference LaughV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("laugh-diabolically"), 1);

        public static IEffectCapabilityHandler Handler { get; } = new LaughHandler();

        private sealed class LaughHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => LaughV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema();
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }
    }
}
