using System;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// Milestone 8.19a. The declared shape of text applied around a primary action, and the two nodes
    /// that shape names.
    ///
    /// <para>An effect definition is ordinarily walked once, from its root. This one is walked twice,
    /// because a whole action stands between its two halves: the actor is asked which side the text
    /// belongs to, the action happens, and the text's work happens on the chosen side. Rather than
    /// invent a second effect language for that, the definition declares one root
    /// <see cref="EffectNodeKind.Sequence"/> with exactly two children - the ordering question and the
    /// work - and the two entries are those two children.</para>
    ///
    /// <para><b>Why the shape is declared rather than inferred.</b> Entering a definition at a node
    /// inside it is only meaningful if the definition says which node means what. Stating it as a rule
    /// the definition validator enforces makes a malformed composition a content-load rejection, which
    /// is where every other statement about a definition's structure is caught, instead of a
    /// mid-action fault the player would see as a stuck turn.</para>
    /// </summary>
    public static class AroundActionShape
    {
        /// <summary>The node that asks which side of the action the work belongs to.</summary>
        public static EffectNodeId OrderingNode(EffectDefinition definition) => ChildAt(definition, 0);

        /// <summary>The node holding the work that question places.</summary>
        public static EffectNodeId WorkNode(EffectDefinition definition) => ChildAt(definition, 1);

        /// <summary>
        /// Whether this definition has the declared shape. The validator reports the reason; this
        /// answers the question for a caller that only needs yes or no.
        /// </summary>
        public static bool IsWellFormed(EffectDefinition definition)
        {
            if (definition == null || !definition.TryGetNode(definition.RootNodeId, out var root))
            {
                return false;
            }

            return root.Kind == EffectNodeKind.Sequence && root.ChildNodeIds.Count == 2;
        }

        private static EffectNodeId ChildAt(EffectDefinition definition, int index)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!IsWellFormed(definition))
            {
                throw new InvalidOperationException(
                    $"Effect definition '{definition.Id}' is applied around a primary action, so its " +
                    "root is a sequence of exactly two nodes: the ordering question and the work it " +
                    "places.");
            }

            definition.TryGetNode(definition.RootNodeId, out var root);
            return root.ChildNodeIds[index];
        }
    }
}
