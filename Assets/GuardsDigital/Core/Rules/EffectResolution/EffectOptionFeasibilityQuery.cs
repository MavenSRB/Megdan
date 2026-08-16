using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 6.5ac: which of a control node's options the acting player cannot perform.
    ///
    /// A `Choose` used to offer every child unconditionally, so a mode that could do nothing was
    /// selectable, and choosing it ran the branch to a mandatory stop that spent the card, ended the
    /// turn, changed nothing on the board, and explained nothing. This is the question that refuses it,
    /// and it is asked of the branch's own declared contract - the nodes it orders and the capabilities
    /// they name - so every definition of the same shape is judged by this one walk rather than by
    /// anything about a card, a hero, or a node's name.
    ///
    /// Nothing here executes. A capability handler may apply rather than pause - marking an actor,
    /// creating an Active effect, returning a marker, pushing every adjacent object at a fixed distance
    /// - so a probe that called <see cref="IEffectCapabilityHandler.Execute"/> would perform the very
    /// effect it was asked about. Each handler answers
    /// <see cref="IEffectCapabilityHandler.Anticipate"/> instead, from the candidate derivation it
    /// already owns.
    /// </summary>
    public static class EffectOptionFeasibilityQuery
    {
        /// <summary>
        /// Bounds one walk. A definition that cycles is refused at content load, so exceeding this
        /// means the walker itself is wrong; failing loudly beats hanging a match.
        /// </summary>
        private const int MaximumNodesPerOption = 1024;

        /// <summary>
        /// What a walk proves about one node or branch. The three are distinct because the enclosing
        /// sequence treats them differently: only <see cref="Blocked"/> means nothing after this can
        /// run, and only <see cref="Performable"/> means the option is worth offering.
        /// </summary>
        private enum Verdict
        {
            /// <summary>Applies nothing here, and stops nothing either: execution carries on.</summary>
            Empty = 0,

            /// <summary>Something here can apply, pause, or publish. The option is offered.</summary>
            Performable = 1,

            /// <summary>A mandatory step cannot be taken, so the resolution stops at it.</summary>
            Blocked = 2
        }

        /// <summary>
        /// The options of <paramref name="chooseNode"/> the chooser cannot perform, in the definition's
        /// own order. Empty when every option can be performed, and never larger than the option set.
        ///
        /// Feasibility is a reading of the state as it stands, never a stored fact: this is asked again
        /// at every pause, on snapshot restore, on replay, and on retained-prefix Undo, so a mode that
        /// becomes performable inside one frame becomes selectable and one that stops being performable
        /// stops being offered.
        /// </summary>
        public static IReadOnlyList<EffectChoiceCandidate> FindUnperformableOptions(
            EffectNodeDefinition chooseNode,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (chooseNode == null) throw new ArgumentNullException(nameof(chooseNode));
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var completed = outcomes ?? Array.Empty<EffectNodeOutcome>();
            var refused = new List<EffectChoiceCandidate>();
            foreach (var option in chooseNode.ChildNodeIds)
            {
                var budget = MaximumNodesPerOption;
                if (Walk(option, frame, completed, context, ref budget) != Verdict.Performable)
                {
                    refused.Add(EffectChoiceCandidate.Option(option));
                }
            }

            return refused;
        }

        /// <summary>
        /// Milestone 6.5af: whether a whole effect definition could apply anything at all, asked of
        /// the frame a start would begin from rather than of one it began.
        ///
        /// It is the same walk, the same verdicts, and the same <c>Anticipate</c> contract the option
        /// question uses - one root instead of a control node's children - so an action is judged by
        /// exactly the rule its modes are, and a later definition of any shape is covered without
        /// naming it. <c>false</c> is returned only where the walk can prove nothing applies; a branch
        /// it cannot read stays performable, so this refuses an action it knows is empty and never one
        /// it merely cannot judge.
        /// </summary>
        public static bool CanApplyAnything(
            PendingResolution prospectiveFrame,
            EffectResolutionContext context)
        {
            if (prospectiveFrame == null) throw new ArgumentNullException(nameof(prospectiveFrame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var budget = MaximumNodesPerOption;
            return Walk(
                context.Definition.RootNodeId,
                prospectiveFrame,
                Array.Empty<EffectNodeOutcome>(),
                context,
                ref budget) == Verdict.Performable;
        }

        /// <summary>
        /// One node's verdict, and its subtree's, in the order execution would reach them. The walk
        /// short-circuits at the first thing that can apply, because an option only has to do one
        /// thing to be worth offering.
        /// </summary>
        private static Verdict Walk(
            EffectNodeId nodeId,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            ref int budget)
        {
            if (--budget < 0)
            {
                throw new InvalidOperationException(
                    $"Effect definition '{context.Definition.Id}' did not settle within " +
                    $"{MaximumNodesPerOption} nodes while judging an option.");
            }

            if (!context.Definition.TryGetNode(nodeId, out var node))
            {
                // An unresolvable child is a definition fault the resolver reports when it walks
                // there. Judging it unperformable would hide that behind a greyed control.
                return Verdict.Performable;
            }

            switch (node.Kind)
            {
                case EffectNodeKind.Capability:
                    return Judge(node, frame, outcomes, context);

                case EffectNodeKind.Sequence:
                    foreach (var child in node.ChildNodeIds)
                    {
                        var verdict = Walk(child, frame, outcomes, context, ref budget);
                        if (verdict != Verdict.Empty)
                        {
                            // Performable ends the walk; blocked ends the sequence, because the
                            // resolution stops at a mandatory step it cannot take.
                            return verdict;
                        }
                    }

                    return Verdict.Empty;

                case EffectNodeKind.Optional:
                    // Declining is always available and always does nothing, so an optional step is
                    // worth offering exactly when the step it governs is.
                    return Walk(node.ChildNodeIds[0], frame, outcomes, context, ref budget) ==
                        Verdict.Performable
                        ? Verdict.Performable
                        : Verdict.Empty;

                case EffectNodeKind.Choose:
                    foreach (var child in node.ChildNodeIds)
                    {
                        if (Walk(child, frame, outcomes, context, ref budget) == Verdict.Performable)
                        {
                            return Verdict.Performable;
                        }
                    }

                    // Every option of a nested Choose is refused, so the node itself cannot be taken
                    // and its own policy decides what that means - the same reading the resolver makes
                    // when it reaches such a node directly.
                    return node.FailurePolicy == EffectFailurePolicy.Mandatory
                        ? Verdict.Blocked
                        : Verdict.Empty;

                case EffectNodeKind.Conditional:
                    // Which branch runs could only be learned by executing the predicate, which this
                    // walk may not do. Both branches are therefore judged, and the node counts as
                    // performable when either is; when neither is, execution still continues, because
                    // an unevaluated predicate could have taken the harmless path.
                    foreach (var child in node.ChildNodeIds)
                    {
                        if (Walk(child, frame, outcomes, context, ref budget) == Verdict.Performable)
                        {
                            return Verdict.Performable;
                        }
                    }

                    return Verdict.Empty;

                default:
                    return Verdict.Performable;
            }
        }

        /// <summary>
        /// One capability node's verdict: what its handler says it would report, read through the
        /// node's own failure policy, which is where p.10's mandatory-versus-optional distinction
        /// already lives.
        /// </summary>
        private static Verdict Judge(
            EffectNodeDefinition node,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (!context.Catalog.TryLoad(node.Capability, out var handler))
            {
                // An unregistered capability is a load fault the resolver raises when it gets there.
                return Verdict.Performable;
            }

            var capabilityContext =
                EffectResolutionSession.BuildCapabilityContext(frame, node, outcomes, context);
            if (!IsDecidable(handler, capabilityContext))
            {
                return Verdict.Performable;
            }

            // Milestone 6.8l: a selection that says what it selects for is judged by that consumer
            // rather than by its own ability to pause. Without this a branch behind a selection was
            // performable whenever anything was targetable, however little the step after it could do
            // with what was chosen - and since milestone 6.8k every named subject arrives this way.
            var anticipated = EffectSelectionEligibilityQuery.JudgeSelection(
                handler.Anticipate(capabilityContext),
                handler,
                node,
                capabilityContext,
                frame,
                outcomes,
                context);

            switch (anticipated)
            {
                case EffectExecutionResultKind.Applied:
                case EffectExecutionResultKind.NeedsChoice:
                    return Verdict.Performable;

                case EffectExecutionResultKind.ImpossibleMandatory:
                    // `if able`, `may`, and `up to` all treat an impossible step as simply not taken,
                    // so only a mandatory one stops what follows it.
                    return node.FailurePolicy == EffectFailurePolicy.Mandatory
                        ? Verdict.Blocked
                        : Verdict.Empty;

                default:
                    return Verdict.Empty;
            }
        }

        /// <summary>
        /// Whether this node can be judged at all yet.
        ///
        /// A required parameter bound to a later node's output resolves to nothing while the walk runs
        /// - the usual shape being a subject a selection has not made yet - and a handler asked with a
        /// missing subject would answer about a question nobody posed. Such a node is treated as
        /// performable, which is the direction to err in: this milestone refuses an option it can prove
        /// does nothing and never disables one it merely cannot read.
        /// </summary>
        private static bool IsDecidable(
            IEffectCapabilityHandler handler,
            EffectCapabilityContext capabilityContext) =>
            handler.Schema.Parameters
                .Where(parameter => parameter.IsRequired)
                .All(parameter => capabilityContext.Input(parameter.Name).IsDefined);
    }
}
