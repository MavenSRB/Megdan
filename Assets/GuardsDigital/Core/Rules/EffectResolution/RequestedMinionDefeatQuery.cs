using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Reads staged minion-Defeat requests from a completed ordinary effect frame. The effect records
    /// the target; card/turn authority owns p.13's coins, removal, and p.8 Lane-Push consequence.
    /// </summary>
    public static class RequestedMinionDefeatQuery
    {
        public static IReadOnlyList<MinionId> Requested(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null) return Array.Empty<MinionId>();

            var requested = new List<MinionId>();
            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(outcome.NodeId, out _))
                    continue;

                foreach (var fact in outcome.Outputs)
                {
                    if (string.Equals(
                            fact.OutputName,
                            SyntheticConsequenceCapabilities.DefeatedMinionOutput,
                            StringComparison.Ordinal) &&
                        fact.Value.Kind == EffectValueKind.Minion &&
                        fact.Value.MinionId.IsDefined)
                    {
                        requested.Add(fact.Value.MinionId);
                    }
                }
            }

            return requested;
        }
    }
}
