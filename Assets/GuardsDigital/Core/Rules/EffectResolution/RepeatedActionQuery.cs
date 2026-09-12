using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// What one completed instance asked for, and what the instance it asks for inherits.
    ///
    /// It is one value rather than a boolean beside a coordinate because the two are never
    /// independently true: an unrequested repeat has nothing to inherit, and a requested one inherits
    /// exactly what its own node declared. Milestone 8.6.
    /// </summary>
    public readonly struct RepeatRequest : IEquatable<RepeatRequest>
    {
        private RepeatRequest(
            bool isRequested, RepeatContinuation continuation, RepeatTargetBinding binding)
        {
            IsRequested = isRequested;
            Continuation = continuation;
            Binding = binding;
        }

        /// <summary>No repeat was asked for - the default, and what every non-repeating action reports.</summary>
        public static RepeatRequest None => default;

        public bool IsRequested { get; }

        /// <summary>The state the requested instance starts in. Meaningful only when requested.</summary>
        public RepeatContinuation Continuation { get; }

        /// <summary>
        /// What the requesting node declared about the repeated instance.s target. Milestone 8.10
        /// carries it here so a dispatcher can honour "on a different target" without reading the
        /// effect nodes itself, which is a seam the architecture conformance rules hold closed.
        /// </summary>
        public RepeatTargetBinding Binding { get; }

        public static RepeatRequest For(
            RepeatContinuation continuation,
            RepeatTargetBinding binding = RepeatTargetBinding.Unbound) =>
            new RepeatRequest(true, continuation, binding);

        public bool Equals(RepeatRequest other) =>
            IsRequested == other.IsRequested && Continuation.Equals(other.Continuation) &&
            Binding == other.Binding;

        public override bool Equals(object obj) => obj is RepeatRequest other && Equals(other);

        public override int GetHashCode() =>
            (IsRequested ? 1 : 0) ^ (Continuation.GetHashCode() * 397);

        public override string ToString() => IsRequested ? Continuation.ToString() : "no repeat";
    }

    /// <summary>
    /// Milestone 6.7: the two questions a bounded Repeat asks of a resolution, stated once.
    ///
    /// Whether this instance may still ask to repeat, and whether it did. Both are read from the frame
    /// and the trusted definition rather than stored anywhere of their own: the invocation context is
    /// already part of every <see cref="PendingResolution"/>, and a request is already an ordinary
    /// applied node outcome. That is what makes a requested repeat survive a pause, a snapshot restore,
    /// a replay, and a retained-prefix Undo without a wire field inside the effect frame.
    ///
    /// Milestone 8.6 added the third question - what the next instance inherits - and moved the bound
    /// from "one repeat, by construction" to "the maximum the node declares". The two are the same
    /// answer for every definition written before it: a node that declares no maximum declares one.
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
        /// The whole bound is here: a chain may ask as many times as the node declares, and no more.
        /// Because the node kind cannot be mandatory, an ineligible one is skipped in silence rather
        /// than stopping the action, so the last instance never exposes the decision at all.
        /// </summary>
        public static bool CanRepeat(PendingResolution frame, EffectNodeDefinition node)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (node == null) throw new ArgumentNullException(nameof(node));
            return frame.Repeat.CompletedRepeats < node.MaximumRepeats;
        }

        /// <summary>
        /// What this resolution's completed outcomes say it asked for, and what the instance it asked
        /// for inherits.
        ///
        /// Asked of the trusted definition rather than of a node-name convention, so only a node the
        /// content author declared as a Repeat can request one. Two applied requests still mean one
        /// repeated instance: the answer is a fact about the action, not a counter - the counter is the
        /// frame's own <see cref="PendingResolution.Repeat"/>, advanced by one here.
        ///
        /// A declared <see cref="RepeatTargetBinding.SameTarget"/> whose selection named no piece
        /// reports no request at all. The definition validator makes that unreachable by requiring the
        /// bound selection to have certainly run; if it were reached anyway, refusing the repeat is the
        /// conservative answer and letting it run unbound would be the card saying something it does
        /// not say.
        /// </summary>
        public static RepeatRequest Requested(
            EffectDefinition definition,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (outcomes == null)
            {
                return RepeatRequest.None;
            }

            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(outcome.NodeId, out var node) ||
                    node.Kind != EffectNodeKind.RepeatAction)
                {
                    continue;
                }

                var completedRepeats = frame.Repeat.CompletedRepeats + 1;

                // Milestone 8.39d1. Two arms carry a piece into the next instance, for opposite
                // reasons: `on the same target` so it can be supplied, and this one so it can be
                // taken out of the offer. Everything else advances unbound.
                if (node.RepeatTargetBinding != RepeatTargetBinding.SameTarget &&
                    node.RepeatTargetBinding != RepeatTargetBinding.DifferentSelectionSubject)
                {
                    return RepeatRequest.For(
                        RepeatContinuation.Next(completedRepeats), node.RepeatTargetBinding);
                }

                // The piece was recorded onto the frame when the selection named it, because that is
                // the last moment it is still standing where the selection saw it.
                if (!frame.Repeat.HasBoundTarget)
                {
                    // `on the same target` with no piece is a repeat that would run unbound, which is
                    // the card saying something it does not say. An EXCLUDING repeat with no piece has
                    // nothing to exclude, and a repeat of an unnarrowed set is exactly what p.10's
                    // default already is - so it advances rather than refusing.
                    return node.RepeatTargetBinding == RepeatTargetBinding.DifferentSelectionSubject
                        ? RepeatRequest.For(
                            RepeatContinuation.Next(completedRepeats), node.RepeatTargetBinding)
                        : RepeatRequest.None;
                }

                // `on the same target` carried no binding before milestone 8.39d1 and carries none
                // now: `RepeatRequest.Binding` exists so a dispatcher can honour "on a different
                // target" without reading nodes, and putting a value where Wasp and Dodger have
                // always seen `Unbound` would be a behaviour change they never asked for.
                return node.RepeatTargetBinding == RepeatTargetBinding.SameTarget
                    ? RepeatRequest.For(RepeatContinuation.NextBoundTo(
                        completedRepeats, frame.Repeat.BoundTarget))
                    : RepeatRequest.For(
                        RepeatContinuation.NextBoundTo(completedRepeats, frame.Repeat.BoundTarget),
                        node.RepeatTargetBinding);
            }

            return RepeatRequest.None;
        }

        /// <summary>
        /// Whether this node is the selection an `on the same target` repeat in this definition binds.
        ///
        /// Asked of the trusted definition on every ask rather than carried in the continuation, which
        /// is what keeps the inherited piece data and the binding a rule. The definition validator
        /// admits at most one binding per definition, so the answer is unambiguous.
        /// </summary>
        public static bool BindsSelection(EffectDefinition definition, EffectNodeDefinition node)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (node == null) throw new ArgumentNullException(nameof(node));
            return BoundBy(definition, node, RepeatTargetBinding.SameTarget) ||
                BoundBy(definition, node, RepeatTargetBinding.DifferentSelectionSubject);
        }

        /// <summary>
        /// Whether this node is the selection a repeat of the named binding declares against it.
        ///
        /// <para>Milestone 8.39d1 split this out of <see cref="BindsSelection"/> because two arms now
        /// bind a selection and they do OPPOSITE things with the piece: one supplies it back, one takes
        /// it out. A single "is bound" answer is right for arming the capture - both need the piece
        /// recorded - and wrong for every reader after that.</para>
        /// </summary>
        public static bool BoundBy(
            EffectDefinition definition, EffectNodeDefinition node, RepeatTargetBinding binding)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (node == null) throw new ArgumentNullException(nameof(node));
            return definition.Nodes.Any(candidate =>
                candidate.RepeatTargetBinding == binding &&
                candidate.BoundSelectionNodeId.Equals(node.Id));
        }

        /// <summary>
        /// The piece a selection just named, for a definition whose repeat will bind it.
        ///
        /// The published space is resolved to its occupant here and now, in the one moment the two
        /// certainly agree - the selection has applied and nothing has moved since. Returns
        /// <c>false</c> for a node that binds nothing, a result that published no space, and a space
        /// the board reports empty, and in each case the frame keeps whatever it already held.
        /// </summary>
        public static bool TryCaptureBoundTarget(
            EffectDefinition definition,
            EffectNodeDefinition node,
            EffectExecutionResult result,
            MatchComponents components,
            out BoardObjectReference boundTarget)
        {
            boundTarget = default;
            if (result == null || result.Kind != EffectExecutionResultKind.Applied ||
                components == null || !components.CanDisplace ||
                !BindsSelection(definition, node))
            {
                return false;
            }

            foreach (var fact in result.Outcomes)
            {
                if (fact.Value.Kind == EffectValueKind.Coordinate)
                {
                    return components.Board.TryGetObjectAt(fact.Value.Coordinate, out boundTarget);
                }
            }

            return false;
        }

        /// <summary>
        /// The answer a repeated instance must give at this node, when this node is the one its card's
        /// `on the same target` binds: the space the inherited piece is standing on now.
        ///
        /// The space is looked up rather than remembered, which is the whole reason the continuation
        /// carries a piece. A bound piece that has left the board reports <c>false</c>, and the node
        /// then executes normally - which for a mandatory selection means it offers its ordinary
        /// candidates. That case cannot arise from an orbit, whose subject is still standing wherever
        /// it was moved to; it is the honest answer for a chain whose target was removed between
        /// instances, and the selection's own eligibility then decides what may happen next.
        /// </summary>
        public static bool TryReadBoundTarget(
            EffectDefinition definition,
            EffectNodeDefinition node,
            PendingResolution frame,
            MatchComponents components,
            out EffectValue boundTarget)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            boundTarget = default;

            // Milestone 8.39d1. The SUPPLY path is `on the same target` alone: an excluding repeat
            // records the identical piece by the identical route and hands it to nobody, so widening
            // this would make the repeated instance take the subject it is meant to avoid.
            if (!frame.Repeat.HasBoundTarget || components == null || !components.CanDisplace ||
                !BoundBy(definition, node, RepeatTargetBinding.SameTarget))
            {
                return false;
            }

            if (!components.Board.TryGetObjectCoordinate(frame.Repeat.BoundTarget, out var at))
            {
                return false;
            }

            boundTarget = EffectValue.Space(at);
            return true;
        }
    }
}
