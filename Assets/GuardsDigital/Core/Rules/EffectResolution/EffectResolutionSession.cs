using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    public enum EffectResolutionRejectionReason
    {
        None = 0,
        NoPendingChoice = 1,
        StaleChoice = 2,
        WrongChooser = 3,
        InvalidChoice = 4,
        IncompleteSelection = 5,
        UnregisteredCapability = 6,
        UnknownNode = 7
    }

    /// <summary>
    /// One node's contribution to the ordered fact stream, with the timing checkpoints its capability
    /// declared around itself. The caller turns these into whatever event shape its layer speaks;
    /// keeping that translation outside the resolver is what lets the frame stay Unity-free and
    /// action-family-agnostic.
    /// </summary>
    public sealed class EffectResolutionEvent
    {
        public EffectResolutionEvent(
            EffectNodeId nodeId,
            EffectExecutionResultKind resultKind,
            IEnumerable<EffectOutcomeFact> outputs = null,
            IEnumerable<TimingCheckpoint> checkpointsBefore = null,
            IEnumerable<TimingCheckpoint> checkpointsAfter = null)
        {
            NodeId = nodeId;
            ResultKind = resultKind;
            Outputs = new ReadOnlyCollection<EffectOutcomeFact>(
                (outputs ?? Enumerable.Empty<EffectOutcomeFact>()).ToArray());
            CheckpointsBefore = new ReadOnlyCollection<TimingCheckpoint>(
                (checkpointsBefore ?? Enumerable.Empty<TimingCheckpoint>()).ToArray());
            CheckpointsAfter = new ReadOnlyCollection<TimingCheckpoint>(
                (checkpointsAfter ?? Enumerable.Empty<TimingCheckpoint>()).ToArray());
        }

        public EffectNodeId NodeId { get; }
        public EffectExecutionResultKind ResultKind { get; }
        public IReadOnlyList<EffectOutcomeFact> Outputs { get; }
        public IReadOnlyList<TimingCheckpoint> CheckpointsBefore { get; }
        public IReadOnlyList<TimingCheckpoint> CheckpointsAfter { get; }
    }

    public sealed class EffectResolutionStepResult
    {
        private EffectResolutionStepResult(
            bool accepted,
            EffectResolutionRejectionReason rejectionReason,
            PendingResolution frame,
            bool isComplete,
            IEnumerable<EffectResolutionEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Frame = frame;
            IsComplete = isComplete;
            Events = new ReadOnlyCollection<EffectResolutionEvent>(
                (events ?? Enumerable.Empty<EffectResolutionEvent>()).ToArray());
        }

        public bool Accepted { get; }
        public EffectResolutionRejectionReason RejectionReason { get; }

        /// <summary>The frame to keep, or null once the resolution has completed.</summary>
        public PendingResolution Frame { get; }

        public bool IsComplete { get; }
        public IReadOnlyList<EffectResolutionEvent> Events { get; }

        public static EffectResolutionStepResult Accept(
            PendingResolution frame,
            bool isComplete,
            IEnumerable<EffectResolutionEvent> events) =>
            new EffectResolutionStepResult(
                true, EffectResolutionRejectionReason.None, isComplete ? null : frame, isComplete, events);

        public static EffectResolutionStepResult Reject(
            EffectResolutionRejectionReason reason,
            PendingResolution frame)
        {
            if (reason == EffectResolutionRejectionReason.None)
            {
                throw new ArgumentException("A rejected effect step requires a reason.", nameof(reason));
            }

            return new EffectResolutionStepResult(false, reason, frame, false, null);
        }
    }

    /// <summary>Everything the resolver needs that is not part of the frame's own state.</summary>
    public sealed class EffectResolutionContext
    {
        public EffectResolutionContext(
            EffectDefinition definition,
            EffectCapabilityCatalog catalog,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            int roundIndex,
            int turnIndex,
            RuleModificationSet modifications,
            HeroId opposingHeroId = default,
            MatchComponents components = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Participants = participants;
            Timing = timing;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Modifications = modifications ?? throw new ArgumentNullException(nameof(modifications));
            OpposingHeroId = opposingHeroId;
            Components = components;
        }

        public EffectDefinition Definition { get; }
        public EffectCapabilityCatalog Catalog { get; }
        public MatchParticipants Participants { get; }
        public ActiveEffectLifecycle Timing { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public HeroId OpposingHeroId { get; }

        /// <summary>
        /// The modifications in effect for this resolution. Required since milestone 6.4, which added
        /// the first capabilities that read rules rather than only supplying them: a Push consults
        /// p.18's immunity and the Move-Through permission, and a direct Move consults the same
        /// obstacle contract Movement does. <see cref="RuleModificationSet.Empty"/> is the named
        /// no-modification value, so a genuinely rule-free caller states it rather than inheriting it.
        /// </summary>
        public RuleModificationSet Modifications { get; }

        /// <summary>Milestone 6.2's token and marker collaborators, or <c>null</c> outside a match.</summary>
        public MatchComponents Components { get; }
    }

    /// <summary>
    /// Walks an effect definition's node table, pausing on authoritative pending choices and resuming
    /// from the frame alone.
    ///
    /// Static and frame-in / frame-out by construction. That is not a style preference: it is how
    /// <c>CORE_MODEL.md</c>'s sixth invariant - any resolution awaiting a choice is serializable data,
    /// never a delegate, iterator, coroutine, closure, or UI callback - is made structurally true
    /// rather than merely observed. There is no instance to hold a continuation in.
    /// </summary>
    public static class EffectResolutionSession
    {
        /// <summary>
        /// Bounds a single Advance pass. A definition that cycles is already refused at content load,
        /// so exceeding this means the walker itself is wrong; failing loudly beats hanging a match.
        /// </summary>
        private const int MaximumStepsPerPass = 1024;

        public static EffectResolutionStepResult Start(
            EffectResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            HeroId controllerId,
            ActionInvocationContext invocationContext,
            EffectResolutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var frame = new PendingResolution(
                resolutionId,
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                context.Definition.Reference,
                actorId,
                controllerId,
                invocationContext,
                new[] { new EffectContinuationFrame(context.Definition.RootNodeId, 0) });

            return Advance(frame, context, new List<EffectResolutionEvent>());
        }

        public static EffectResolutionStepResult Answer(
            PendingResolution frame,
            EffectChoiceCommand command,
            EffectResolutionContext context)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var choice = frame.PendingChoice;
            if (choice == null)
            {
                return EffectResolutionStepResult.Reject(
                    EffectResolutionRejectionReason.NoPendingChoice, frame);
            }

            if (!command.ResolutionId.Equals(choice.ResolutionId) ||
                !command.ChoiceId.Equals(choice.ChoiceId))
            {
                return EffectResolutionStepResult.Reject(
                    EffectResolutionRejectionReason.StaleChoice, frame);
            }

            switch (command.Kind)
            {
                case EffectChoiceCommandKind.Select:
                    if (!choice.SelectInvocations.Any(invocation => invocation.Equals(command)))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    return Pause(frame, choice.WithSelected(
                        choice.SelectedCandidates.Concat(new[] { command.Candidate })));

                case EffectChoiceCommandKind.Deselect:
                    if (!choice.DeselectInvocations.Any(invocation => invocation.Equals(command)))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    return Pause(frame, choice.WithSelected(
                        choice.SelectedCandidates.Where(candidate => !candidate.Equals(command.Candidate))));

                case EffectChoiceCommandKind.Confirm:
                    if (choice.ConfirmInvocation == null || !choice.ConfirmInvocation.Equals(command))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.IncompleteSelection, frame);
                    }

                    return Resume(frame, choice, EffectChoiceCommandKind.Confirm, context);

                case EffectChoiceCommandKind.Decline:
                    if (choice.DeclineInvocation == null || !choice.DeclineInvocation.Equals(command))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    return Resume(frame, choice, EffectChoiceCommandKind.Decline, context);

                default:
                    return EffectResolutionStepResult.Reject(
                        EffectResolutionRejectionReason.InvalidChoice, frame);
            }
        }

        private static EffectResolutionStepResult Pause(
            PendingResolution frame,
            PendingEffectChoice choice) =>
            EffectResolutionStepResult.Accept(
                frame.With(frame.Continuation, frame.CompletedOutcomes, frame.AcceptedChoices, choice),
                false,
                null);

        private static EffectResolutionStepResult Resume(
            PendingResolution frame,
            PendingEffectChoice choice,
            EffectChoiceCommandKind resolvedBy,
            EffectResolutionContext context)
        {
            var node = context.Definition.GetNode(choice.NodeId);
            var continuation = frame.Continuation.ToList();
            var outcomes = frame.CompletedOutcomes.ToList();
            var choices = frame.AcceptedChoices.ToList();
            var events = new List<EffectResolutionEvent>();

            choices.Add(new EffectChoiceRecord(
                choice.NodeId, choice.ChoiceId, resolvedBy, choice.SelectedCandidates));

            if (resolvedBy == EffectChoiceCommandKind.Decline)
            {
                Pop(continuation);
                Record(outcomes, events, node.Id, EffectExecutionResultKind.DeclinedOptional);
                return Advance(Rebuild(frame, continuation, outcomes, choices), context, events);
            }

            switch (node.Kind)
            {
                case EffectNodeKind.Optional:
                    AdvanceTop(continuation);
                    continuation.Add(new EffectContinuationFrame(node.ChildNodeIds[0], 0));
                    break;

                case EffectNodeKind.Choose:
                    AdvanceTop(continuation);
                    // Pushed in reverse so the selected options execute in the definition's own order.
                    foreach (var candidate in node.ChildNodeIds
                        .Where(child => choice.SelectedCandidates.Any(selected =>
                            selected.Kind == EffectChoiceCandidateKind.Option &&
                            selected.OptionNodeId.Equals(child)))
                        .Reverse())
                    {
                        continuation.Add(new EffectContinuationFrame(candidate, 0));
                    }

                    break;

                case EffectNodeKind.Capability:
                    var handler = context.Catalog.Load(node.Capability);
                    var resumed = handler.Resume(
                        BuildContext(frame, node, outcomes, context),
                        choice.SelectedCandidates.Select(candidate => candidate.Value).ToArray());
                    var terminal = ApplyCapabilityResult(
                        frame, node, resumed, continuation, outcomes, choices, events, context);
                    if (terminal != null)
                    {
                        return terminal;
                    }

                    // A resumed capability that simply applied leaves the rest of the effect to run.
                    break;

                default:
                    return EffectResolutionStepResult.Reject(
                        EffectResolutionRejectionReason.InvalidChoice, frame);
            }

            return Advance(Rebuild(frame, continuation, outcomes, choices), context, events);
        }

        private static EffectResolutionStepResult Advance(
            PendingResolution frame,
            EffectResolutionContext context,
            List<EffectResolutionEvent> events)
        {
            var continuation = frame.Continuation.ToList();
            var outcomes = frame.CompletedOutcomes.ToList();
            var choices = frame.AcceptedChoices.ToList();

            for (var step = 0; step < MaximumStepsPerPass; step++)
            {
                if (continuation.Count == 0)
                {
                    return EffectResolutionStepResult.Accept(
                        Rebuild(frame, continuation, outcomes, choices), true, events);
                }

                var top = continuation[continuation.Count - 1];
                if (!context.Definition.TryGetNode(top.NodeId, out var node))
                {
                    return EffectResolutionStepResult.Reject(
                        EffectResolutionRejectionReason.UnknownNode, frame);
                }

                switch (node.Kind)
                {
                    case EffectNodeKind.Sequence:
                        if (top.ChildIndex < node.ChildNodeIds.Count)
                        {
                            AdvanceTop(continuation);
                            continuation.Add(new EffectContinuationFrame(
                                node.ChildNodeIds[top.ChildIndex], 0));
                            continue;
                        }

                        Pop(continuation);
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                        continue;

                    case EffectNodeKind.Optional:
                        if (top.ChildIndex == 0)
                        {
                            if (node.FailurePolicy == EffectFailurePolicy.IfAble)
                            {
                                // `if able` never asks. It attempts, and a refusal is simply skipped.
                                AdvanceTop(continuation);
                                continuation.Add(new EffectContinuationFrame(node.ChildNodeIds[0], 0));
                                continue;
                            }

                            return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                                frame, node, context, EffectChoiceKind.ConfirmOrDecline,
                                Array.Empty<EffectChoiceCandidate>(), 0, 0));
                        }

                        Pop(continuation);
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                        continue;

                    case EffectNodeKind.Choose:
                        if (top.ChildIndex == 0)
                        {
                            return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                                frame, node, context, EffectChoiceKind.SelectOption,
                                node.ChildNodeIds.Select(EffectChoiceCandidate.Option).ToArray(),
                                node.MinimumSelections, node.MaximumSelections));
                        }

                        Pop(continuation);
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                        continue;

                    case EffectNodeKind.Conditional:
                        if (top.ChildIndex == 0)
                        {
                            var predicate = Evaluate(frame, node, outcomes, context);
                            var branch = predicate
                                ? node.ChildNodeIds[0]
                                : node.ChildNodeIds.Count > 1 ? node.ChildNodeIds[1] : default;
                            AdvanceTop(continuation);
                            if (branch.IsDefined)
                            {
                                continuation.Add(new EffectContinuationFrame(branch, 0));
                            }

                            continue;
                        }

                        Pop(continuation);
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                        continue;

                    case EffectNodeKind.Capability:
                        var handler = context.Catalog.Load(node.Capability);
                        var result = handler.Execute(BuildContext(frame, node, outcomes, context));
                        var applied = ApplyCapabilityResult(
                            frame, node, result, continuation, outcomes, choices, events, context);
                        if (applied != null)
                        {
                            return applied;
                        }

                        continue;

                    default:
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.UnknownNode, frame);
                }
            }

            throw new InvalidOperationException(
                $"Effect definition '{context.Definition.Id}' did not settle within {MaximumStepsPerPass} steps.");
        }

        /// <summary>
        /// Returns a terminal result when the capability paused or stopped the action, and null when
        /// execution should simply carry on to the next node.
        /// </summary>
        private static EffectResolutionStepResult ApplyCapabilityResult(
            PendingResolution frame,
            EffectNodeDefinition node,
            EffectExecutionResult result,
            List<EffectContinuationFrame> continuation,
            List<EffectNodeOutcome> outcomes,
            List<EffectChoiceRecord> choices,
            List<EffectResolutionEvent> events,
            EffectResolutionContext context)
        {
            switch (result.Kind)
            {
                case EffectExecutionResultKind.NeedsChoice:
                    return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                        frame, node, context, EffectChoiceKind.SelectTarget,
                        result.Choice.Candidates.Select(EffectChoiceCandidate.Value_).ToArray(),
                        result.Choice.MinimumSelections,
                        result.Choice.MaximumSelections,
                        result.Choice.Chooser));

                case EffectExecutionResultKind.ImpossibleMandatory:
                    Pop(continuation);
                    if (node.FailurePolicy == EffectFailurePolicy.Mandatory)
                    {
                        // The action stops here. Everything already applied stays applied.
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.ImpossibleMandatory, result);
                        continuation.Clear();
                        return EffectResolutionStepResult.Accept(
                            Rebuild(frame, continuation, outcomes, choices), true, events);
                    }

                    // `if able`, `may`, and `up to` all treat an impossible step as simply not taken.
                    Record(outcomes, events, node.Id, EffectExecutionResultKind.DeclinedOptional, result);
                    return null;

                case EffectExecutionResultKind.DeclinedOptional:
                    Pop(continuation);
                    Record(outcomes, events, node.Id, EffectExecutionResultKind.DeclinedOptional, result);
                    return null;

                default:
                    Pop(continuation);
                    Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied, result);
                    return null;
            }
        }

        private static EffectResolutionStepResult PauseOn(
            PendingResolution frame,
            List<EffectContinuationFrame> continuation,
            List<EffectNodeOutcome> outcomes,
            List<EffectChoiceRecord> choices,
            List<EffectResolutionEvent> events,
            PendingEffectChoice choice) =>
            EffectResolutionStepResult.Accept(
                frame.With(continuation, outcomes, choices, choice), false, events);

        private static PendingEffectChoice BuildChoice(
            PendingResolution frame,
            EffectNodeDefinition node,
            EffectResolutionContext context,
            EffectChoiceKind kind,
            IReadOnlyList<EffectChoiceCandidate> candidates,
            int minimumSelections,
            int maximumSelections,
            EffectChooserRole chooserOverride = EffectChooserRole.None) =>
            new PendingEffectChoice(
                frame.ResolutionId,
                new EffectChoiceId($"node-{node.Id.Value}"),
                node.Id,
                ResolveChooser(
                    chooserOverride != EffectChooserRole.None ? chooserOverride : node.Chooser,
                    frame,
                    context),
                kind,
                candidates,
                minimumSelections,
                maximumSelections);

        private static HeroId ResolveChooser(
            EffectChooserRole role,
            PendingResolution frame,
            EffectResolutionContext context)
        {
            switch (role)
            {
                case EffectChooserRole.Controller:
                    return frame.ControllerId;
                case EffectChooserRole.OpposingHero:
                    if (!context.OpposingHeroId.IsDefined)
                    {
                        throw new InvalidOperationException(
                            "An effect node names the opposing hero as chooser, but none was supplied.");
                    }

                    return context.OpposingHeroId;
                default:
                    return frame.ActorId;
            }
        }

        private static bool Evaluate(
            PendingResolution frame,
            EffectNodeDefinition node,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            var handler = context.Catalog.Load(node.Capability);
            var result = handler.Execute(BuildContext(frame, node, outcomes, context));
            return result.Outcomes.Any(outcome =>
                outcome.Value.Kind == EffectValueKind.Boolean && outcome.Value.BooleanValue);
        }

        private static EffectCapabilityContext BuildContext(
            PendingResolution frame,
            EffectNodeDefinition node,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            return new EffectCapabilityContext(
                node.Id,
                node.FailurePolicy,
                frame.ActorId,
                frame.ControllerId,
                frame.SourceCardInstanceId,
                frame.ActionDefinitionId,
                ResolveInputBindings(node, frame, outcomes),
                context.RoundIndex,
                context.TurnIndex,
                context.Participants,
                context.Timing,
                context.Modifications,
                context.Components);
        }

        /// <summary>
        /// The exact input dictionary live execution hands a capability. Internal so that snapshot
        /// restore re-derives a paused choice's candidates through this same path, rebuilding the
        /// bindings from the persisted completed outcomes rather than re-asking with none.
        /// </summary>
        internal static IReadOnlyDictionary<string, EffectValue> ResolveInputBindings(
            EffectNodeDefinition node,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            var inputs = new Dictionary<string, EffectValue>(StringComparer.Ordinal);
            foreach (var binding in node.Inputs)
            {
                inputs[binding.ParameterName] = ResolveInput(binding, frame, outcomes);
            }

            return inputs;
        }

        private static EffectValue ResolveInput(
            EffectInputBinding binding,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            switch (binding.SourceKind)
            {
                case EffectBindingSourceKind.Literal:
                    return binding.Literal;

                case EffectBindingSourceKind.Role:
                    switch (binding.Role)
                    {
                        case EffectRoleKind.Actor:
                        case EffectRoleKind.Chooser:
                            return EffectValue.Hero(frame.ActorId);
                        case EffectRoleKind.Controller:
                            return EffectValue.Hero(frame.ControllerId);
                        case EffectRoleKind.SourceCard:
                            return EffectValue.Card(frame.SourceCardInstanceId);
                        default:
                            return default;
                    }

                case EffectBindingSourceKind.NodeOutput:
                    var source = outcomes.LastOrDefault(outcome =>
                        outcome.NodeId.Equals(binding.SourceNodeId));
                    if (source == null)
                    {
                        return default;
                    }

                    foreach (var output in source.Outputs)
                    {
                        if (string.Equals(output.OutputName, binding.SourceOutputName, StringComparison.Ordinal))
                        {
                            return output.Value;
                        }
                    }

                    return default;

                default:
                    return default;
            }
        }

        private static void Record(
            ICollection<EffectNodeOutcome> outcomes,
            ICollection<EffectResolutionEvent> events,
            EffectNodeId nodeId,
            EffectExecutionResultKind kind,
            EffectExecutionResult result = null)
        {
            outcomes.Add(new EffectNodeOutcome(nodeId, kind, result?.Outcomes));
            events.Add(new EffectResolutionEvent(
                nodeId, kind, result?.Outcomes, result?.CheckpointsBefore, result?.CheckpointsAfter));
        }

        private static void AdvanceTop(IList<EffectContinuationFrame> continuation) =>
            continuation[continuation.Count - 1] = continuation[continuation.Count - 1].Advanced();

        private static void Pop(IList<EffectContinuationFrame> continuation) =>
            continuation.RemoveAt(continuation.Count - 1);

        private static PendingResolution Rebuild(
            PendingResolution frame,
            IEnumerable<EffectContinuationFrame> continuation,
            IEnumerable<EffectNodeOutcome> outcomes,
            IEnumerable<EffectChoiceRecord> choices) =>
            frame.With(continuation, outcomes, choices, null);
    }
}
