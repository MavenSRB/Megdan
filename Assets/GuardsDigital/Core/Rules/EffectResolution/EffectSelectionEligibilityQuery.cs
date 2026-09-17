using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;

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

            // Milestone 8.39d1, applied before the consumer narrowing rather than after it: a piece
            // an excluding repeat took is not a candidate at all, so no question about it is put to a
            // consumer. It is also the one narrowing here that applies where no consumer can be read.
            var candidates = WithoutTheExcludedSubject(
                selectionNode, offered ?? Array.Empty<EffectValue>(), frame, context);
            if (candidates.Count == 0 ||
                !TryReadConsumer(selectionNode, frame, outcomes, context, out var consumer))
            {
                return candidates;
            }

            var minimum = consumer.MinimumSelections;
            if (minimum <= 1)
            {
                return candidates
                    .Where(candidate => consumer.Accepts(new[] { candidate }))
                    .ToArray();
            }

            if (minimum > LargestNarrowableAnswer)
            {
                // Milestone 8.35l's declared boundary, and it is milestone 6.5ac's position restated:
                // a selection whose complete answer names more pieces than this walk will enumerate is
                // one the query cannot read, so it is left exactly as offered rather than narrowed on a
                // guess. Nothing in the shipped roster asks for more than two.
                return candidates;
            }

            // A pair - the only floor above one this build declares. A candidate belongs in the offer
            // exactly when some complete answer containing it would be accepted, which is the same
            // rule the single-selection arm states and not a second one: with a floor of one, "some
            // complete answer containing it" is the candidate by itself.
            //
            // Sabina's Blue swap trio is why this is asked as a pair rather than piece by piece. Its
            // consumer takes both halves at once and refuses when either is barred, so a question that
            // put one piece to it and left the other half unanswered would be asking about a Swap
            // nobody proposed. Enumerating partners asks only questions the consumer can answer, and
            // it stops at the first partner that works, so the ordinary all-legal board costs one
            // question per candidate.
            return candidates
                .Where((candidate, index) => candidates
                    .Where((_, partnerIndex) => partnerIndex != index)
                    .Any(partner => consumer.Accepts(new[] { candidate, partner })))
                .ToArray();
        }

        /// <summary>
        /// The largest complete answer this query will enumerate while narrowing. Two, because
        /// <c>choose-target-pair@1</c> is the widest floor any shipped selection declares and because
        /// the enumeration is over answers rather than candidates: a floor of three would ask a cubic
        /// number of questions of a consumer, and buying a narrowing at that price is not obviously
        /// right. Raising it is a deliberate decision with a measurement behind it, not a constant edit.
        /// </summary>
        private const int LargestNarrowableAnswer = 2;

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

            // Milestone 8.35l. Not `eligible.Count == 0` - the selection's own floor, which is what the
            // live pause has always applied and what this offer question never did. A pair selection
            // narrowed to one legal minion has narrowed correctly and still cannot be answered, and
            // reporting it answerable is exactly how Sabina's Listen Up stayed offered on a board with
            // one free minion, spent the card and moved nothing. A floor of zero belongs to a step the
            // card marks `may`, and the resolver reads that word where it belongs, so this only ever
            // raises the bar it already had.
            var minimum = Math.Max(1, selection.AnticipateMinimumSelections(selectionContext));
            return eligible.Count < minimum
                ? EffectExecutionResultKind.ImpossibleMandatory
                : EffectExecutionResultKind.NeedsChoice;
        }

        /// <summary>
        /// Milestone 8.35l: which standing restriction refused this selection's consumer, where the
        /// consumer can say.
        ///
        /// <para>Asked only once the narrowing has already produced the refusal, and asked of the
        /// consumer rather than re-derived here, so the reason a control carries and the reason it is
        /// greyed are one answer read once. It puts the first candidate's question, because what is
        /// being explained is the whole action rather than one target: the player is being told why the
        /// card cannot be played at all, and every candidate came back refused for it.</para>
        /// </summary>
        public static bool TryExplainSelectionRefusal(
            IEffectCapabilityHandler handler,
            EffectNodeDefinition selectionNode,
            EffectCapabilityContext selectionContext,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            out AppliedRuleModification blocking)
        {
            blocking = null;
            if (!(handler is IEffectSelectionCapabilityHandler selection) ||
                !TryReadConsumer(selectionNode, frame, outcomes, context, out var consumer))
            {
                return false;
            }

            var candidates = selection.AnticipateCandidates(selectionContext);
            if (candidates.Count == 0)
            {
                return false;
            }

            var answer = consumer.MinimumSelections >= 2 && candidates.Count >= 2
                ? new[] { candidates[0], candidates[1] }
                : new[] { candidates[0] };
            return consumer.TryExplainRefusal(answer, out blocking);
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

            // Milestone 8.39d1, the answer-side half of the offer narrowing above. The two read one
            // method, so a piece left out of the offer cannot be submitted to it - which is what keeps
            // "on a different target" a rule rather than a presentation.
            if (chosen.Count > 0 && WithoutTheExcludedSubject(
                    selectionNode, chosen, frame, context).Count != chosen.Count)
            {
                return false;
            }

            if (chosen.Count == 0 ||
                !TryReadConsumer(selectionNode, frame, outcomes, context, out var consumer))
            {
                return true;
            }

            // Milestone 8.35l asks about the answer rather than about each piece of it. An answer is
            // what the consumer is going to be given, and for a pair the two halves are only legal
            // together - so checking them one at a time would either refuse a legal pair or admit an
            // illegal one depending on which half was asked about.
            return consumer.Accepts(chosen);
        }


        /// <summary>
        /// Milestone 8.39d1. The piece an excluding repeat took in the instance that asked for the
        /// repeat, removed from what this selection offers - Cutter's Broadside, "May repeat once on a
        /// different target."
        ///
        /// <para><b>Removing a candidate cannot be done the way supplying one is.</b>
        /// <c>RepeatTargetBinding.SameTarget</c> is capability-agnostic because it hands a value to the
        /// bound selection's own <c>Resume</c> and lets that selection judge it; nothing in the session
        /// has to know what a target is. An exclusion has no such route - only a selection knows its
        /// candidates - so it is taken out of the OFFER here, where every other generic narrowing
        /// already happens, rather than pushed into each handler.</para>
        ///
        /// <para><b>Matched by identity where the offer names a piece and by space where it names a
        /// coordinate.</b> A selection publishes whichever of those its own card asked for, and the
        /// frame records the excluded piece as a board object; comparing an identity against a
        /// coordinate would silently exclude nobody, which is the failure this method exists to make
        /// impossible. A piece the board can no longer place is excluded by identity alone, because a
        /// selection that still offers it is offering the thing the clause named.</para>
        /// </summary>
        private static IReadOnlyList<EffectValue> WithoutTheExcludedSubject(
            EffectNodeDefinition selectionNode,
            IReadOnlyList<EffectValue> candidates,
            PendingResolution frame,
            EffectResolutionContext context)
        {
            if (candidates.Count == 0 || !frame.Repeat.HasBoundTarget ||
                !RepeatedActionQuery.BoundBy(
                    context.Definition, selectionNode,
                    RepeatTargetBinding.DifferentSelectionSubject))
            {
                return candidates;
            }

            var excluded = frame.Repeat.BoundTarget;
            var excludedAt = context.Components != null && context.Components.CanDisplace &&
                context.Components.Board.TryGetObjectCoordinate(excluded, out var at)
                    ? (HexCoordinate?)at
                    : null;

            return candidates.Where(candidate => !NamesTheExcluded(candidate)).ToArray();

            bool NamesTheExcluded(EffectValue candidate)
            {
                switch (candidate.Kind)
                {
                    case EffectValueKind.Hero:
                        return excluded.Kind == BoardObjectKind.Hero &&
                            excluded.HeroId.Equals(candidate.HeroId);
                    case EffectValueKind.Minion:
                        return excluded.Kind == BoardObjectKind.Minion &&
                            excluded.MinionId.Equals(candidate.MinionId);
                    case EffectValueKind.Coordinate:
                        return excludedAt.HasValue && excludedAt.Value.Equals(candidate.Coordinate);
                    default:
                        return false;
                }
            }
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
                !context.Catalog.TryLoad(consumer.Capability, out var handler) ||
                !selectionNode.Capability.IsDefined ||
                !context.Catalog.TryLoad(selectionNode.Capability, out var selectionHandler) ||
                !(selectionHandler is IEffectSelectionCapabilityHandler selection) ||
                !(selectionHandler is IEffectAnticipatedOutcomeCapabilityHandler publisher))
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
                consumer, frame, outcomes, context.OpposingHeroId, context.TriggeredCardId);
            var substituted = new HashSet<string>(
                consumed.Select(binding => binding.ParameterName), StringComparer.Ordinal);
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

            var selectionContext =
                EffectResolutionSession.BuildCapabilityContext(frame, selectionNode, outcomes, context);
            question = new ConsumerQuestion(
                handler, consumer, consumed, inputs, frame, outcomes, context,
                publisher, selectionContext,
                Math.Max(1, selection.AnticipateMinimumSelections(selectionContext)));
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
            private readonly IReadOnlyList<EffectSelectionDeclaration.ConsumedBinding> consumedBindings;
            private readonly IReadOnlyDictionary<string, EffectValue> resolvedInputs;
            private readonly PendingResolution frame;
            private readonly IReadOnlyList<EffectNodeOutcome> outcomes;
            private readonly EffectResolutionContext context;
            private readonly IEffectAnticipatedOutcomeCapabilityHandler publisher;
            private readonly EffectCapabilityContext selectionContext;

            public ConsumerQuestion(
                IEffectCapabilityHandler handler,
                EffectNodeDefinition consumer,
                IReadOnlyList<EffectSelectionDeclaration.ConsumedBinding> consumedBindings,
                IReadOnlyDictionary<string, EffectValue> resolvedInputs,
                PendingResolution frame,
                IReadOnlyList<EffectNodeOutcome> outcomes,
                EffectResolutionContext context,
                IEffectAnticipatedOutcomeCapabilityHandler publisher,
                EffectCapabilityContext selectionContext,
                int minimumSelections)
            {
                this.handler = handler;
                this.consumer = consumer;
                this.consumedBindings = consumedBindings;
                this.resolvedInputs = resolvedInputs;
                this.frame = frame;
                this.outcomes = outcomes;
                this.context = context;
                this.publisher = publisher;
                this.selectionContext = selectionContext;
                MinimumSelections = minimumSelections;
            }

            /// <summary>How many candidates a complete answer to this selection names.</summary>
            public int MinimumSelections { get; }

            /// <summary>
            /// Whether the consumer could do something with this prospective answer, read exactly as
            /// the feasibility walk reads any capability node: applying and pausing are both worth
            /// offering, and every other answer means this answer leads nowhere.
            ///
            /// <para>Milestone 8.35l put the selection's own publication between the answer and the
            /// consumer. Before it, the raw candidate was substituted into every parameter the consumer
            /// binds - which is right only where the candidate's kind happens to be the bound output's
            /// kind, and is why the only declared consumers in the roster were three of Wasp's. Now the
            /// selection says what this answer would publish, and each consumed parameter is given the
            /// output it actually reads.</para>
            /// </summary>
            public bool Accepts(IReadOnlyList<EffectValue> answer)
            {
                if (!TrySubstitute(answer, out var substituted))
                {
                    // A required parameter this selection feeds would receive nothing for this answer.
                    // That is a question about the answer that this query cannot pose rather than a
                    // refusal of it, so the conservative reading stands and the candidate is offered.
                    return true;
                }

                var kind = handler.Anticipate(EffectResolutionSession.BuildCapabilityContext(
                    frame, consumer, outcomes, context, resolvedInputs, substituted));
                return kind == EffectExecutionResultKind.Applied ||
                    kind == EffectExecutionResultKind.NeedsChoice;
            }

            /// <summary>
            /// Milestone 8.35l: the standing modification the consumer refused this answer on, where
            /// the consumer can say. Nothing is derived here - the consumer that refused is the one
            /// asked, on the context it was refused in.
            /// </summary>
            public bool TryExplainRefusal(
                IReadOnlyList<EffectValue> answer, out AppliedRuleModification blocking)
            {
                blocking = null;
                return handler is IEffectRefusalExplainingCapabilityHandler explaining &&
                    TrySubstitute(answer, out var substituted) &&
                    explaining.TryExplainRefusal(
                        EffectResolutionSession.BuildCapabilityContext(
                            frame, consumer, outcomes, context, resolvedInputs, substituted),
                        out blocking);
            }

            /// <summary>
            /// The consumer's inputs as this answer would leave them, or nothing where a required one
            /// would be left undefined.
            /// </summary>
            private bool TrySubstitute(
                IReadOnlyList<EffectValue> answer,
                out IReadOnlyDictionary<string, EffectValue> substituted)
            {
                substituted = null;
                var published = publisher.AnticipateOutcomes(selectionContext, answer);
                var byOutput = new Dictionary<string, EffectValue>(StringComparer.Ordinal);
                for (var index = 0; index < published.Count; index++)
                {
                    byOutput[published[index].OutputName] = published[index].Value;
                }

                var bound = new Dictionary<string, EffectValue>(StringComparer.Ordinal);
                foreach (var binding in consumedBindings)
                {
                    if (byOutput.TryGetValue(binding.SourceOutputName, out var value))
                    {
                        bound[binding.ParameterName] = value;
                        continue;
                    }

                    // A selection may publish one of several outputs per answer - the unit selection
                    // names either a hero or a minion - and a consumer binding both reads whichever
                    // resolved. An unpublished OPTIONAL parameter is therefore ordinary and simply
                    // stays undefined; an unpublished REQUIRED one means the question cannot be posed.
                    if (handler.Schema.Parameters.Any(parameter =>
                            string.Equals(
                                parameter.Name, binding.ParameterName, StringComparison.Ordinal) &&
                            parameter.IsRequired))
                    {
                        return false;
                    }

                    bound[binding.ParameterName] = default;
                }

                substituted = bound;
                return true;
            }
        }
    }
}
