using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 6.8l: which of a target selection's candidates the step consuming them can act on.
    ///
    /// Milestone 6.5ac refuses an option it can prove applies nothing and offers one it cannot read.
    /// Milestone 6.8k then made every non-hero subject arrive through a selection node, so the shape
    /// the walk cannot read became the ordinary one: it reaches the selection, sees that a selection
    /// can always pause, and offers the branch without the step after it ever being judged. On the
    /// synthetic Skill that is four of seven branches, and the Replace one needs an adjacent Hazard
    /// token that is absent for most of a match.
    ///
    /// The missing information was never in the walk. A selection declared a range and whether the
    /// actor is included, and nothing about what the chosen object was <em>for</em>, so a legal target
    /// and a useful one were the same thing to it. Once the definition says which node consumes the
    /// choice, the question has an owner that already answers it: the consumer's own
    /// <see cref="IEffectCapabilityHandler.Anticipate"/>, asked once per candidate with that candidate
    /// standing in for the output it binds. Nothing is performed to find out, and no second reading of
    /// any rule is written here - the consumer's refusal is the consumer's.
    ///
    /// Everything that needs the answer asks this one query: the feasibility walk, the live pause that
    /// builds the candidate list, the answer that re-reads eligibility rather than trusting the offer,
    /// and the snapshot restore that re-derives both. What it will not do is guess. A selection that
    /// names no consumer, a consumer this build cannot load, and a consumer whose other required input
    /// a later step publishes all come back unnarrowed, because milestone 6.8l narrows what can be read
    /// and does not start disabling what merely might fail.
    /// </summary>
    public static class EffectSelectionEligibilityQuery
    {
        /// <summary>
        /// The offered values whose consumer would accept them, or every one of them where the consumer
        /// cannot be read. Never larger than what was offered, and never reordered: a candidate list is
        /// what a player sees, and the definition's own order is the order it was authored in.
        /// </summary>
        public static IReadOnlyList<EffectValue> EligibleCandidates(
            EffectNodeDefinition selectionNode,
            IReadOnlyList<EffectValue> offered,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (selectionNode == null) throw new ArgumentNullException(nameof(selectionNode));
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var candidates = offered ?? Array.Empty<EffectValue>();
            if (candidates.Count == 0 ||
                !TryReadConsumer(selectionNode, frame, outcomes, context, out var consumer))
            {
                return candidates;
            }

            return candidates.Where(candidate => consumer.Accepts(candidate)).ToArray();
        }

        /// <summary>
        /// What a selection node reports once its consumer has been consulted, given the kind its own
        /// probe already reported.
        ///
        /// Only <see cref="EffectExecutionResultKind.NeedsChoice"/> can change here, and only to
        /// <see cref="EffectExecutionResultKind.ImpossibleMandatory"/>: a selection with candidates
        /// none of which its consumer accepts has nothing to name, which is the same position as
        /// having no candidates at all. Every other answer is the handler's own and passes through, so
        /// this can narrow a selection and can never widen one.
        /// </summary>
        public static EffectExecutionResultKind JudgeSelection(
            EffectExecutionResultKind anticipated,
            IEffectCapabilityHandler handler,
            EffectNodeDefinition selectionNode,
            EffectCapabilityContext selectionContext,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (anticipated != EffectExecutionResultKind.NeedsChoice ||
                !(handler is IEffectSelectionCapabilityHandler selection))
            {
                return anticipated;
            }

            var eligible = EligibleCandidates(
                selectionNode,
                selection.AnticipateCandidates(selectionContext),
                frame,
                outcomes,
                context);
            return eligible.Count == 0
                ? EffectExecutionResultKind.ImpossibleMandatory
                : EffectExecutionResultKind.NeedsChoice;
        }

        /// <summary>
        /// Whether every one of these values is still one the consumer accepts.
        ///
        /// Asked when an answer arrives rather than only when the offer was built. A candidate list is
        /// authoritative frame state and cannot be edited from outside, so this is not a second guard
        /// against a forged command - the command is already checked against the pre-built invocations.
        /// It is the guard against a stale one: the same reason the selection handler re-derives its own
        /// targets in <c>Resume</c> instead of trusting what it offered when the choice opened.
        /// </summary>
        public static bool AreStillEligible(
            EffectNodeDefinition selectionNode,
            IReadOnlyList<EffectValue> selected,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            var chosen = selected ?? Array.Empty<EffectValue>();
            return chosen.Count == 0 ||
                EligibleCandidates(selectionNode, chosen, frame, outcomes, context).Count == chosen.Count;
        }

        /// <summary>
        /// The consumer this selection declares, bound to everything needed to put a candidate to it,
        /// or nothing where the pairing cannot be read yet.
        /// </summary>
        private static bool TryReadConsumer(
            EffectNodeDefinition selectionNode,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            out ConsumerQuestion question)
        {
            question = null;
            if (!EffectSelectionDeclaration.TryReadConsumer(selectionNode, out var consumerNodeId) ||
                !context.Definition.TryGetNode(consumerNodeId, out var consumer) ||
                !consumer.Capability.IsDefined ||
                !context.Catalog.TryLoad(consumer.Capability, out var handler))
            {
                // An unresolvable consumer is a definition fault the validator refuses at content load
                // and the resolver reports where it walks; treating it as a refusal here would hide
                // one behind a greyed control.
                return false;
            }

            var consumed = EffectSelectionDeclaration.ConsumedParameters(consumer, selectionNode.Id);
            if (consumed.Count == 0)
            {
                return false;
            }

            var inputs = EffectResolutionSession.ResolveInputBindings(
                consumer, frame, outcomes, context.OpposingHeroId);
            var substituted = new HashSet<string>(consumed, StringComparer.Ordinal);
            var undecidable = handler.Schema.Parameters
                .Where(parameter => parameter.IsRequired)
                .Where(parameter => !substituted.Contains(parameter.Name))
                .Any(parameter => !inputs.TryGetValue(parameter.Name, out var value) || !value.IsDefined);
            if (undecidable)
            {
                // The consumer needs something a step between this selection and it will publish - the
                // synthetic Skill's Swap reads the actor's own space that way. Asking now would put a
                // question nobody has posed yet, and answering it would refuse candidates for a reason
                // that is not about them.
                return false;
            }

            question = new ConsumerQuestion(handler, consumer, consumed, inputs, frame, outcomes, context);
            return true;
        }

        /// <summary>
        /// One consumer, ready to be asked about a candidate. Holds only what the capability context
        /// needs rebuilding from; nothing here survives the call.
        /// </summary>
        private sealed class ConsumerQuestion
        {
            private readonly IEffectCapabilityHandler handler;
            private readonly EffectNodeDefinition consumer;
            private readonly IReadOnlyList<string> consumedParameters;
            private readonly IReadOnlyDictionary<string, EffectValue> resolvedInputs;
            private readonly PendingResolution frame;
            private readonly IReadOnlyList<EffectNodeOutcome> outcomes;
            private readonly EffectResolutionContext context;

            public ConsumerQuestion(
                IEffectCapabilityHandler handler,
                EffectNodeDefinition consumer,
                IReadOnlyList<string> consumedParameters,
                IReadOnlyDictionary<string, EffectValue> resolvedInputs,
                PendingResolution frame,
                IReadOnlyList<EffectNodeOutcome> outcomes,
                EffectResolutionContext context)
            {
                this.handler = handler;
                this.consumer = consumer;
                this.consumedParameters = consumedParameters;
                this.resolvedInputs = resolvedInputs;
                this.frame = frame;
                this.outcomes = outcomes;
                this.context = context;
            }

            /// <summary>
            /// Whether the consumer could do something with this candidate, read exactly as the
            /// feasibility walk reads any capability node: applying and pausing are both worth
            /// offering, and every other answer means this candidate leads nowhere.
            /// </summary>
            public bool Accepts(EffectValue candidate)
            {
                var substituted = consumedParameters.ToDictionary(
                    parameter => parameter, _ => candidate, StringComparer.Ordinal);
                var kind = handler.Anticipate(EffectResolutionSession.BuildCapabilityContext(
                    frame, consumer, outcomes, context, resolvedInputs, substituted));
                return kind == EffectExecutionResultKind.Applied ||
                    kind == EffectExecutionResultKind.NeedsChoice;
            }
        }
    }
}
