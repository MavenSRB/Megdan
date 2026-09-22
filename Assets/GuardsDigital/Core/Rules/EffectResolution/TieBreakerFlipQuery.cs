using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 8.75a. Whether an applied node asked the match to flip its Tie Breaker coin -
    /// Ignatia the Mad's Chaos Incarnate, "you may flip the Tie Breaker coin; if you do, you may
    /// perform that action again, choosing different targets."
    ///
    /// <para><b>The fact is named here and published by content, which is the split every other
    /// staged request in this folder uses.</b> <see cref="RequestedMinionDefeatQuery"/> reads a
    /// Defeat off an output name the engine declares and a capability publishes; this reads a flip
    /// the same way. A handler cannot flip the coin itself - <see cref="EffectCapabilityContext"/>
    /// is "immutable, and carries no Unity object, delegate, or continuation" in as many words - so
    /// it says that a flip is owed and the authority holding the coin performs it.</para>
    ///
    /// <para><b>The flip is applied BEFORE the next node runs, which is why it is read one outcome at
    /// a time.</b> Chaos Incarnate's own next node is the repeat, and whether that repeat has a legal
    /// subject is a question about her card text under the face the coin is NOW showing. So the
    /// resolver stops on the flip exactly as milestone 8.19c1 stops on an applied Push and 8.40h on a
    /// given marker, the authority flips, and the frame carries on with the new face in hand. A
    /// completed-outcome sweep would answer a milestone too late.</para>
    /// </summary>
    public static class TieBreakerFlipQuery
    {
        /// <summary>
        /// The output name a capability publishes to ask for the flip. Declared by the engine rather
        /// than by the hero who prints the sentence, so the resolver's stop reads one string and a
        /// second hero printing the same clause reaches it without a second rule.
        /// </summary>
        public const string FlippedOutput = "tie-breaker-flip";

        /// <summary>
        /// Whether one applied node outcome asked for the flip. The node is confirmed against the
        /// trusted definition before its fact is believed, which is milestone 6.7's discipline: an
        /// outcome naming a node this definition does not declare names nothing.
        /// </summary>
        public static bool Requested(EffectDefinition definition, EffectNodeOutcome outcome)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcome == null ||
                outcome.ResultKind != EffectExecutionResultKind.Applied ||
                !definition.TryGetNode(outcome.NodeId, out _))
            {
                return false;
            }

            foreach (var fact in outcome.Outputs)
            {
                if (string.Equals(fact.OutputName, FlippedOutput, StringComparison.Ordinal) &&
                    fact.Value.Kind == EffectValueKind.Boolean && fact.Value.BooleanValue)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether any of a frame's completed outcomes asked for the flip. Used by a reader that has
        /// the whole frame rather than the node that has just run - a fixture asserting what a
        /// finished body did, and never the resolver, which stops one outcome at a time.
        /// </summary>
        public static bool RequestedAny(
            EffectDefinition definition, IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null) return false;

            foreach (var outcome in outcomes)
            {
                if (Requested(definition, outcome)) return true;
            }

            return false;
        }
    }
}
