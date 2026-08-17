using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 6.7: the two questions a bounded Repeat asks of a resolution, stated once.
    ///
    /// Whether this instance may still ask to repeat, and whether it did. Both are read from the frame
    /// and the trusted definition rather than stored anywhere of their own: the invocation context is
    /// already part of every <see cref="PendingResolution"/>, and a request is already an ordinary
    /// applied node outcome. That is what makes a requested repeat survive a pause, a snapshot restore,
    /// a replay, and a retained-prefix Undo without a wire field inside the effect frame.
    /// </summary>
    public static class RepeatedActionQuery
    {
        /// <summary>
        /// Whether an action instance running under this context is itself a repeat.
        ///
        /// <see cref="ActionInvocationContext.Repeated"/> is the discriminator, and it is the only
        /// producer of one: no offer carries it, and the card-action dispatcher refuses to start an
        /// ordinary action under any context but the two printed ones.
        /// </summary>
        public static bool IsRepeatedInstance(ActionInvocationContext invocationContext) =>
            invocationContext == ActionInvocationContext.Repeated;

        /// <summary>
        /// Whether a <see cref="EffectNodeKind.RepeatAction"/> node reached in this frame is eligible.
        ///
        /// The whole bound is here: an instance created by a repeat may not ask for another. Because
        /// the node kind cannot be mandatory, an ineligible one is skipped in silence rather than
        /// stopping the action, so a repeated instance never exposes the decision at all.
        /// </summary>
        public static bool CanRepeat(PendingResolution frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            return !IsRepeatedInstance(frame.InvocationContext);
        }

        /// <summary>
        /// Whether this resolution's completed outcomes contain an applied repeat request.
        ///
        /// Asked of the trusted definition rather than of a node-name convention, so only a node the
        /// content author declared as a Repeat can request one. Two applied requests still mean one
        /// repeated instance: the answer is a fact about the action, not a counter.
        /// </summary>
        public static bool WasRequested(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null)
            {
                return false;
            }

            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind == EffectExecutionResultKind.Applied &&
                    definition.TryGetNode(outcome.NodeId, out var node) &&
                    node.Kind == EffectNodeKind.RepeatAction)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
