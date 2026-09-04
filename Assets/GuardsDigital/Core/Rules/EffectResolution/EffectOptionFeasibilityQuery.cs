using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;

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

        private enum BindingVerdict
        {
            Continue = 0,
            Bound = 1,
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
            EffectResolutionContext context) =>
            CanApplyAnything(prospectiveFrame, context, out _);

        /// <summary>
        /// Milestone 8.35l: the same answer, plus the standing modification that produced it where the
        /// walk met one.
        ///
        /// <para>The reason travels out with the verdict rather than being asked for afterwards,
        /// because asking afterwards means asking a second time and a second reading can differ from
        /// the first. What comes back is the applied modification the refusing capability itself named,
        /// and it is null wherever the action is empty for any other reason - a card whose text can
        /// bind no target is greyed exactly as before and says nothing new.</para>
        /// </summary>
        public static bool CanApplyAnything(
            PendingResolution prospectiveFrame,
            EffectResolutionContext context,
            out AppliedRuleModification blocking)
        {
            if (prospectiveFrame == null) throw new ArgumentNullException(nameof(prospectiveFrame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var budget = MaximumNodesPerOption;
            var explanation = new RefusalExplanation();
            var verdict = Walk(
                context.Definition.RootNodeId,
                prospectiveFrame,
                Array.Empty<EffectNodeOutcome>(),
                context,
                ref budget,
                explanation);
            blocking = verdict == Verdict.Performable ? null : explanation.Blocking;
            return verdict == Verdict.Performable;
        }

        /// <summary>
        /// The first standing modification a walk met, kept so the offer can name it.
        ///
        /// <para>First rather than every one, and first in the walk's own order, which is execution
        /// order: what a player is shown is why the action stops where it stops, and a list of every
        /// rule that would also have refused it further along is not that. It is written once and never
        /// overwritten, so a later branch cannot rename the blocker the walk actually stopped at.</para>
        /// </summary>
        private sealed class RefusalExplanation
        {
            public AppliedRuleModification Blocking { get; private set; }

            public void RecordFirst(AppliedRuleModification blocking)
            {
                if (Blocking == null && blocking != null)
                {
                    Blocking = blocking;
                }
            }
        }

        /// <summary>
        /// Milestone 8.29e: whether the one branch an <see cref="EffectNodeKind.Optional"/> node
        /// governs can perform anything from the state as it stands.
        ///
        /// A `Choose` has asked this of each of its options since milestone 6.5ac, and an `Optional`
        /// asked it of nothing: it opened p.10's confirm-or-decline whatever the branch behind it
        /// could do, so "You may move another unit adjacent to the target" was a live prompt with no
        /// unit to move, and the only answer that changed anything was the one the player was going to
        /// give anyway. This is the same walk the option question uses, over the one child the node
        /// governs - <see cref="EffectNodeKind.Optional"/> in <see cref="Walk"/> already reads a
        /// declined branch as applying nothing, so the offer and the walk now read the node the same
        /// way rather than one of them deciding twice.
        ///
        /// It answers about the branch alone. Whether declining is free, whether the enclosing action
        /// continues, and what a skipped node records are the resolver's, and none of them change.
        /// </summary>
        public static bool CanPerformOptionalBranch(
            EffectNodeDefinition optionalNode,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (optionalNode == null) throw new ArgumentNullException(nameof(optionalNode));
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var budget = MaximumNodesPerOption;
            return Walk(
                optionalNode.ChildNodeIds[0],
                frame,
                outcomes ?? Array.Empty<EffectNodeOutcome>(),
                context,
                ref budget) == Verdict.Performable;
        }

        /// <summary>
        /// Whether a mandatory card-text path can reach the typed operation that binds an enclosing
        /// Attack's target. Unlike <see cref="CanApplyAnything"/>, an earlier optional or otherwise
        /// performable step is not sufficient: the walk continues until a mandatory target binding is
        /// reachable, or a mandatory prerequisite proves the path blocked. A modal choice succeeds
        /// when any legal branch can bind, matching the player's ability to choose that branch.
        /// </summary>
        public static bool CanCompleteMandatoryAttackTargetBinding(
            PendingResolution prospectiveFrame,
            EffectResolutionContext context)
        {
            if (prospectiveFrame == null) throw new ArgumentNullException(nameof(prospectiveFrame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var budget = MaximumNodesPerOption;
            return WalkToAttackTargetBinding(
                context.Definition.RootNodeId,
                prospectiveFrame,
                Array.Empty<EffectNodeOutcome>(),
                context,
                ref budget) == BindingVerdict.Bound;
        }

        private static BindingVerdict WalkToAttackTargetBinding(
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
                    $"{MaximumNodesPerOption} nodes while judging an Attack target binding.");
            }

            if (!context.Definition.TryGetNode(nodeId, out var node))
            {
                return BindingVerdict.Continue;
            }

            switch (node.Kind)
            {
                case EffectNodeKind.Capability:
                    if (node.Capability.Equals(
                            GuardsDigital.Core.Effects.Synthetic.SyntheticContinuationCapabilities
                                .SetAttackTargetV1))
                    {
                        return node.FailurePolicy == EffectFailurePolicy.Mandatory
                            ? BindingVerdict.Bound
                            : BindingVerdict.Continue;
                    }

                    return Judge(node, frame, outcomes, context) == Verdict.Blocked
                        ? BindingVerdict.Blocked
                        : BindingVerdict.Continue;

                case EffectNodeKind.Sequence:
                    foreach (var child in node.ChildNodeIds)
                    {
                        var verdict = WalkToAttackTargetBinding(
                            child, frame, outcomes, context, ref budget);
                        if (verdict != BindingVerdict.Continue)
                        {
                            return verdict;
                        }
                    }

                    return BindingVerdict.Continue;

                case EffectNodeKind.Optional:
                    // A player may decline this whole subtree, so it cannot be the mandatory binding
                    // that makes the enclosing Attack legal. Continue to the next ordered step.
                    return BindingVerdict.Continue;

                case EffectNodeKind.Choose:
                case EffectNodeKind.Conditional:
                    foreach (var child in node.ChildNodeIds)
                    {
                        if (WalkToAttackTargetBinding(
                                child, frame, outcomes, context, ref budget) ==
                            BindingVerdict.Bound)
                        {
                            return BindingVerdict.Bound;
                        }
                    }

                    return node.FailurePolicy == EffectFailurePolicy.Mandatory
                        ? BindingVerdict.Blocked
                        : BindingVerdict.Continue;

                default:
                    return BindingVerdict.Continue;
            }
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
            ref int budget,
RefusalExplanation explanation = null)
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
                    return Judge(node, frame, outcomes, context, explanation);

                case EffectNodeKind.Sequence:
                    // Milestone 8.35l. The sequence carries forward what an earlier step would publish,
                    // so a later step bound to it can be judged at all. Nothing is executed to get it:
                    // only a node whose handler declares
                    // `IEffectAnticipatedOutcomeCapabilityHandler` - a claim that deriving its outputs
                    // mutates nothing - contributes, and only when the walk already found it decidable
                    // and performable. `actor-space@1` is the whole reason: a Place reading the actor's
                    // own space was undecidable to this walk, so Blend Into Shadows was offered against
                    // a standing restriction that would refuse it, drew no prompt, and spent the card.
                    var carried = outcomes;
                    foreach (var child in node.ChildNodeIds)
                    {
                        var verdict = Walk(child, frame, carried, context, ref budget, explanation);
                        if (verdict != Verdict.Empty)
                        {
                            // Performable ends the walk; blocked ends the sequence, because the
                            // resolution stops at a mandatory step it cannot take.
                            return verdict;
                        }

                        carried = WithAnticipatedOutputs(child, frame, carried, context);
                    }

                    return Verdict.Empty;

                case EffectNodeKind.Optional:
                    // Declining is always available and always does nothing, so an optional step is
                    // worth offering exactly when the step it governs is.
                    return Walk(node.ChildNodeIds[0], frame, outcomes, context, ref budget, explanation) ==
                        Verdict.Performable
                        ? Verdict.Performable
                        : Verdict.Empty;

                case EffectNodeKind.Choose:
                    foreach (var child in node.ChildNodeIds)
                    {
                        if (Walk(child, frame, outcomes, context, ref budget, explanation) == Verdict.Performable)
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
                        if (Walk(child, frame, outcomes, context, ref budget, explanation) == Verdict.Performable)
                        {
                            return Verdict.Performable;
                        }
                    }

                    return Verdict.Empty;

                case EffectNodeKind.RepeatAction:
                    // Milestone 6.7. A Repeat applies nothing of its own: what it re-runs is the very
                    // action this walk is judging, so calling it performable would let an action that
                    // can do nothing be offered on the strength of being able to do nothing twice. It
                    // never blocks either, because the node kind refuses to be mandatory.
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
            EffectResolutionContext context,
            RefusalExplanation explanation = null)
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
                    // Milestone 8.35l. A node that only publishes a fact has not made the action worth
                    // taking. Reading the acting hero's own coordinate is the case that mattered:
                    // `actor-space@1` applied, the walk short-circuited there, and the Place standing
                    // behind it - which a restriction had already barred - was never judged, so
                    // Tigerclaw's Blend Into Shadows was offered, drew no prompt, and spent the card
                    // for nothing. The sequence carries what it published to the next step instead, so
                    // the step that does the visible thing is the one this answer is about.
                    return handler is IEffectAnticipatedOutcomeCapabilityHandler publisher &&
                        publisher.PublishesWithoutApplying
                        ? Verdict.Empty
                        : Verdict.Performable;

                case EffectExecutionResultKind.NeedsChoice:
                    return Verdict.Performable;

                case EffectExecutionResultKind.ImpossibleMandatory:
                    // Milestone 8.35l. Ask what refused, before reading the failure policy: the node
                    // itself where it can explain its own refusal, and otherwise the consumer this
                    // selection was narrowed by, which is where a selection's refusal actually lives.
                    RecordExplanation(
                        explanation, handler, node, capabilityContext, frame, outcomes, context);

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
        /// Milestone 8.35l: which standing modification refused this node, recorded once.
        ///
        /// <para>Two shapes reach it and both are the refusing party answering for itself. A node that
        /// refused on a restriction of its own - the Place behind Tigerclaw's actor-space bridge - says
        /// so directly. A selection narrowed to nothing was not refused by itself at all: its consumer
        /// refused every candidate, so the consumer is asked. Neither case re-derives anything, and a
        /// refusal with no restriction behind it records nothing rather than inventing a cause.</para>
        /// </summary>
        private static void RecordExplanation(
            RefusalExplanation explanation,
            IEffectCapabilityHandler handler,
            EffectNodeDefinition node,
            EffectCapabilityContext capabilityContext,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (explanation == null)
            {
                return;
            }

            if (handler is IEffectRefusalExplainingCapabilityHandler explaining &&
                explaining.TryExplainRefusal(capabilityContext, out var own))
            {
                explanation.RecordFirst(own);
                return;
            }

            if (EffectSelectionEligibilityQuery.TryExplainSelectionRefusal(
                    handler, node, capabilityContext, frame, outcomes, context, out var consumed))
            {
                explanation.RecordFirst(consumed);
            }
        }

        /// <summary>
        /// Milestone 8.35l: the outcomes so far, plus what this node would publish if it publishes
        /// without applying anything and can be read now.
        ///
        /// <para>Nothing is executed. A handler contributes only by declaring
        /// <see cref="IEffectAnticipatedOutcomeCapabilityHandler.PublishesWithoutApplying"/>, which is
        /// a claim that running it changes no rules state, and only when its required inputs already
        /// resolve - so this widens what the walk can read without widening what it may do. A node it
        /// cannot read contributes nothing and the step after it stays undecidable, which is milestone
        /// 6.5ac's conservative position exactly where it still applies.</para>
        /// </summary>
        private static IReadOnlyList<EffectNodeOutcome> WithAnticipatedOutputs(
            EffectNodeId nodeId,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (!context.Definition.TryGetNode(nodeId, out var node) ||
                node.Kind != EffectNodeKind.Capability ||
                !context.Catalog.TryLoad(node.Capability, out var handler) ||
                !(handler is IEffectAnticipatedOutcomeCapabilityHandler publisher) ||
                !publisher.PublishesWithoutApplying)
            {
                return outcomes;
            }

            var capabilityContext =
                EffectResolutionSession.BuildCapabilityContext(frame, node, outcomes, context);
            if (!IsDecidable(handler, capabilityContext) ||
                handler.Anticipate(capabilityContext) != EffectExecutionResultKind.Applied)
            {
                return outcomes;
            }

            var published = publisher.AnticipateOutcomes(
                capabilityContext, Array.Empty<EffectValue>());
            if (published == null || published.Count == 0)
            {
                return outcomes;
            }

            var carried = new List<EffectNodeOutcome>(outcomes);
            carried.Add(new EffectNodeOutcome(
                node.Id, EffectExecutionResultKind.Applied, published));
            return carried;
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
