using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>Only the successful resolved-discard operation publishes this transition.
    /// The last completed node is also the snapshot authority for an interrupted discard.</summary>
    public static class ResolvedCardDiscardQuery
    {
        public static CardInstanceId Requested(EffectDefinition definition, EffectNodeOutcome outcome)
        {
            if (definition == null || outcome == null ||
                outcome.ResultKind != EffectExecutionResultKind.Applied ||
                !definition.TryGetNode(outcome.NodeId, out var node) ||
                node.Kind != EffectNodeKind.Capability ||
                !node.Capability.Equals(SyntheticResolvedDiscardCapabilities.DiscardResolvedCardV1))
                return default;
            var fact = outcome.Outputs.SingleOrDefault(value =>
                value.OutputName == SyntheticResolvedDiscardCapabilities.DiscardedOutput &&
                value.Value.Kind == EffectValueKind.CardInstance);
            return fact.Value.Kind == EffectValueKind.CardInstance ? fact.Value.CardInstanceId : default;
        }
    }
}
