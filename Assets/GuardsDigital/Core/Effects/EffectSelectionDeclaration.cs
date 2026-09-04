using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// Milestone 6.8l: how a target selection says what it selects for, stated once.
    ///
    /// Milestone 6.8k made every non-hero subject arrive through a selection node, because a role
    /// resolves without board access and so no role can supply a coordinate. That left the selection
    /// knowing a range and whether the actor is included, and nothing about what the chosen object is
    /// <em>for</em> - so a legal target and a useful one were indistinguishable to both the feasibility
    /// walk and the runtime candidate list, and a branch behind a selection was offered without the step
    /// consuming the choice ever being judged.
    ///
    /// The missing information is one node reference, and it is carried as an ordinary literal input
    /// binding rather than as anything new. A capability declares the parameter, a definition binds it,
    /// the parameter type-match the definition validator already runs checks its kind, and the two
    /// things that match cannot state - that the named node exists in this definition, and that it
    /// actually reads this selection's output - are the validator's two rules. Nothing about the
    /// consumer is a capability name, a display name, a string, or a predicate expression.
    ///
    /// The parameter is optional wherever it is declared. A selection that binds none is exactly the
    /// conservative position milestone 6.5ac chose - unreadable means offered - and this milestone
    /// narrows what can be read rather than disabling what merely might fail.
    /// </summary>
    public static class EffectSelectionDeclaration
    {
        /// <summary>
        /// The one parameter name a selection capability declares to accept a consumer. A single
        /// constant rather than a literal at each site: the catalog, the definition validator, the
        /// feasibility walk, the resolver, and the snapshot restore all have to agree on it, and a
        /// second spelling would silently mean "this selection declares nothing".
        /// </summary>
        public const string ConsumerParameterName = "selectsFor";

        /// <summary>
        /// Whether a capability accepts a consumer at all. A schema declaring this parameter is what
        /// obliges its handler to enumerate candidates without opening a choice; see
        /// <see cref="IEffectSelectionCapabilityHandler"/>.
        /// </summary>
        public static bool DeclaresConsumerParameter(EffectCapabilityParameterSchema schema) =>
            schema != null &&
            schema.Parameters.Any(parameter =>
                string.Equals(parameter.Name, ConsumerParameterName, StringComparison.Ordinal) &&
                parameter.ValueKind == EffectValueKind.Node);

        /// <summary>
        /// The node this one selects for, when it names one. False for every node that binds no
        /// consumer, which is every node the milestone deliberately leaves alone.
        /// </summary>
        public static bool TryReadConsumer(EffectNodeDefinition node, out EffectNodeId consumerNodeId)
        {
            consumerNodeId = default;
            if (node == null)
            {
                return false;
            }

            foreach (var input in node.Inputs)
            {
                if (!string.Equals(input.ParameterName, ConsumerParameterName, StringComparison.Ordinal) ||
                    input.Literal.Kind != EffectValueKind.Node)
                {
                    continue;
                }

                consumerNodeId = input.Literal.NodeId;
                return consumerNodeId.IsDefined;
            }

            return false;
        }

        /// <summary>
        /// The consumer's parameters fed by this selection's published outputs, in the consumer's own
        /// binding order, each beside the output it reads. Empty means the named node reads nothing this
        /// selection publishes, which the definition validator refuses at content load - so at runtime it
        /// can only mean a definition that never passed through the validator, and the caller treats it
        /// as unreadable rather than as a refusal.
        ///
        /// <para>Milestone 8.35l added the second half of each pair, and the absence of it is the whole
        /// reason milestone 6.8l's narrowing could not be declared on half the roster. Knowing only
        /// <em>which parameters</em> a selection feeds, the eligibility query had no choice but to
        /// substitute the raw candidate into all of them - so a selection publishing a minion and the
        /// space it stands on, consumed by a Swap that binds the space, put a minion where a coordinate
        /// belonged and refused every candidate. The binding always knew which output it read; this
        /// stopped discarding it.</para>
        /// </summary>
        public static IReadOnlyList<ConsumedBinding> ConsumedParameters(
            EffectNodeDefinition consumer,
            EffectNodeId selectionNodeId) =>
            consumer == null
                ? Array.Empty<ConsumedBinding>()
                : consumer.Inputs
                    .Where(input => input.SourceKind == EffectBindingSourceKind.NodeOutput)
                    .Where(input => input.SourceNodeId.Equals(selectionNodeId))
                    .Select(input => new ConsumedBinding(input.ParameterName, input.SourceOutputName))
                    .ToArray();

        /// <summary>
        /// One consumer parameter and the selection output that feeds it. A pair rather than a name,
        /// because putting a candidate to a consumer means putting it under the output that consumer
        /// binds, and the two are not the same thing wherever a selection publishes more than one.
        /// </summary>
        public readonly struct ConsumedBinding
        {
            public ConsumedBinding(string parameterName, string sourceOutputName)
            {
                ParameterName = parameterName;
                SourceOutputName = sourceOutputName;
            }

            public string ParameterName { get; }

            public string SourceOutputName { get; }
        }
    }
}
