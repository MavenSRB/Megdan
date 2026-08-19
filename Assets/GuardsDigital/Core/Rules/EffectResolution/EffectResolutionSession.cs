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
            IEnumerable<EffectResolutionEvent> events,
            bool repeatRequested,
            GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind lanePushCause)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Frame = frame;
            IsComplete = isComplete;
            Events = new ReadOnlyCollection<EffectResolutionEvent>(
                (events ?? Enumerable.Empty<EffectResolutionEvent>()).ToArray());
            RepeatRequested = repeatRequested;
            LanePushCause = lanePushCause;
        }

        public bool Accepted { get; }
        public EffectResolutionRejectionReason RejectionReason { get; }

        /// <summary>The frame to keep, or null once the resolution has completed.</summary>
        public PendingResolution Frame { get; }

        public bool IsComplete { get; }
        public IReadOnlyList<EffectResolutionEvent> Events { get; }

        /// <summary>
        /// Milestone 6.7: whether this completed instance asked to be repeated. Meaningful only
        /// alongside <see cref="IsComplete"/>, because the completing frame is the last thing that
        /// carries the outcome the request lives on; while a resolution is still paused the same
        /// question is asked of <see cref="Frame"/> through <see cref="RepeatedActionQuery"/>.
        ///
        /// It says nothing about when the next instance starts. That is the card action's business,
        /// and its answer is "once this instance's after-action correction boundary is clean".
        /// </summary>
        public bool RepeatRequested { get; }

        /// <summary>
        /// Milestone 6.9: which of rulebook p.8's two sentences this completed instance answered, or
        /// the named absence. Meaningful only alongside <see cref="IsComplete"/>, for the reason
        /// <see cref="RepeatRequested"/> is: the completing frame is the last thing that carries the
        /// outcome the recorded cause lives on.
        ///
        /// It says nothing about whether a push is owed. That question needs the board, and the card
        /// action asks it.
        /// </summary>
        public GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind LanePushCause { get; }

        public static EffectResolutionStepResult Accept(
            PendingResolution frame,
            bool isComplete,
            IEnumerable<EffectResolutionEvent> events,
            bool repeatRequested = false,
            GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind lanePushCause =
                GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind.None) =>
            new EffectResolutionStepResult(
                true, EffectResolutionRejectionReason.None, isComplete ? null : frame, isComplete, events,
                repeatRequested, lanePushCause);

        public static EffectResolutionStepResult Reject(
            EffectResolutionRejectionReason reason,
            PendingResolution frame)
        {
            if (reason == EffectResolutionRejectionReason.None)
            {
                throw new ArgumentException("A rejected effect step requires a reason.", nameof(reason));
            }

            return new EffectResolutionStepResult(
                false, reason, frame, false, null, false,
                GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind.None);
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
            EffectCardState cardState,
            HeroId opposingHeroId = default,
            MatchComponents components = null,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Participants = participants;
            Timing = timing;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Modifications = modifications ?? throw new ArgumentNullException(nameof(modifications));
            CardState = cardState ?? throw new ArgumentNullException(nameof(cardState));
            OpposingHeroId = opposingHeroId;
            Components = components;
            HeroItems = heroItems;
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

        /// <summary>
        /// The card definitions and instances this resolution may read. Required since milestone 7.1a,
        /// which added the first capability that reads card state rather than only card identities: a
        /// discard derives its candidates from the chooser's actual hand, and must derive them again
        /// when the answer arrives, when the option walk asks what the node would do, and when a
        /// snapshot restores the paused choice. <see cref="EffectCardState.Absent"/> is the named
        /// no-card-state value, so a caller with none states it rather than inheriting an empty hand.
        /// </summary>
        public EffectCardState CardState { get; }

        /// <summary>Milestone 6.2's token and marker collaborators, or <c>null</c> outside a match.</summary>
        public MatchComponents Components { get; }

        /// <summary>
        /// Milestone 7.2b's items, or <c>null</c> in a composition that holds none. A capability that
        /// reads a printed value composes p.16's Item Bonus onto it, and must compose the identical one
        /// at every point it can be asked - Skill start, repeated-Skill start, the option-feasibility
        /// walk, the choice-answer path, and snapshot restore's candidate re-derivation - or a paused
        /// Radius would come back a ring smaller than the one the player was offered.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.HeroItemState HeroItems { get; }
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

            return Advance(
                BuildStartFrame(
                    resolutionId,
                    sourceCardInstanceId,
                    sourceDefinitionReference,
                    actionDefinitionId,
                    actorId,
                    controllerId,
                    invocationContext,
                    context),
                context,
                new List<EffectResolutionEvent>());
        }

        /// <summary>
        /// The exact frame a start begins from, before a single node has run.
        ///
        /// Extracted at milestone 6.5af so that asking whether an effect could do anything asks it of
        /// the frame a start <em>would</em> create rather than of a hand-built approximation. The two
        /// cannot drift, because <see cref="Start"/> begins from this same method.
        ///
        /// Public because <see cref="EffectOptionFeasibilityQuery.CanApplyAnything"/> is: a caller that
        /// may ask the question must be able to build the frame the question is asked about, and this
        /// is the only sanctioned way to build one.
        /// </summary>
        public static PendingResolution BuildStartFrame(
            EffectResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            HeroId controllerId,
            ActionInvocationContext invocationContext,
            EffectResolutionContext context) =>
            new PendingResolution(
                resolutionId,
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                context.Definition.Reference,
                actorId,
                controllerId,
                invocationContext,
                new[] { new EffectContinuationFrame(context.Definition.RootNodeId, 0) });

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

            // Milestone 7.1a: whose decision this is, asked before what the decision was.
            //
            // Until a chooser could be someone other than the actor there was nobody to mistake, and
            // this reason was declared but unreachable. Now that a Skill can require another hero to
            // discard, the actor submitting that hero's answer is a real command to refuse, and it is
            // refused as the wrong chooser rather than as an invalid choice, because the difference is
            // exactly what a player needs told. The frame is returned untouched, so the choice still
            // stands and the hero whose decision it is can still make it.
            if (!command.AnsweringHeroId.Equals(choice.ChooserId))
            {
                return EffectResolutionStepResult.Reject(
                    EffectResolutionRejectionReason.WrongChooser, frame);
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

                case EffectChoiceCommandKind.Stop:
                    if (choice.StopInvocation == null || !choice.StopInvocation.Equals(command))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    return Stop(frame, choice, context);

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

        /// <summary>
        /// Milestone 6.5ae: closes a <c>Choose</c> whose every option was refused, on the player's
        /// own command.
        ///
        /// This is the stop milestone 6.5ac performed unasked at the node itself, moved rather than
        /// rewritten, so accepting the invocation produces exactly what the unprompted stop produced:
        /// the same node result, the same ordered facts, and therefore the same card resolution and
        /// turn hand-off downstream. What changed is when the action ends, not what ending it means.
        ///
        /// It records no <see cref="EffectChoiceRecord"/>. That type closes on Confirm or Decline,
        /// and the path this replaces recorded nothing either - a stop answers no question, so there
        /// is no answer for a replay to reproduce.
        /// </summary>
        private static EffectResolutionStepResult Stop(
            PendingResolution frame,
            PendingEffectChoice choice,
            EffectResolutionContext context)
        {
            var continuation = frame.Continuation.ToList();
            var outcomes = frame.CompletedOutcomes.ToList();
            var events = new List<EffectResolutionEvent>();

            Record(outcomes, events, choice.NodeId, EffectExecutionResultKind.ImpossibleMandatory);
            continuation.Clear();
            return EffectResolutionStepResult.Accept(
                Rebuild(frame, continuation, outcomes, frame.AcceptedChoices), true, events,
                RepeatedActionQuery.WasRequested(context.Definition, outcomes),
                RecordedLanePushQuery.Recorded(context.Definition, outcomes));
        }

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

                case EffectNodeKind.RepeatAction:
                    // Milestone 6.7. The request is the whole of what confirming does; the second
                    // instance is created by the card action once this one's boundary is clean. The
                    // node is popped here rather than re-entered, because re-entering would reach the
                    // same `may` and ask again.
                    Pop(continuation);
                    Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                    break;

                case EffectNodeKind.Capability:
                    var selected = choice.SelectedCandidates
                        .Select(candidate => candidate.Value)
                        .ToArray();

                    // Milestone 6.8l: eligibility is read again here rather than trusted from the
                    // offer, for the reason the selection handler re-derives its own targets in
                    // Resume - what was true when the choice opened is not what this answer applies
                    // to. Rejecting leaves the frame and its pending choice exactly as they were, so
                    // the decision is still there to be made.
                    if (!EffectSelectionEligibilityQuery.AreStillEligible(
                            node, selected, frame, outcomes, context))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    var handler = context.Catalog.Load(node.Capability);
                    var resumed = handler.Resume(
                        BuildCapabilityContext(frame, node, outcomes, context), selected);
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
                        Rebuild(frame, continuation, outcomes, choices), true, events,
                        RepeatedActionQuery.WasRequested(context.Definition, outcomes),
                        RecordedLanePushQuery.Recorded(context.Definition, outcomes));
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
                            // Milestone 6.5ac: an option the chooser cannot perform is refused rather
                            // than offered, so no card is spent on a mode that was never going to do
                            // anything.
                            var unavailable = EffectOptionFeasibilityQuery.FindUnperformableOptions(
                                node, frame, outcomes, context);
                            var everyOptionRefused = unavailable.Count == node.ChildNodeIds.Count;
                            if (everyOptionRefused &&
                                node.FailurePolicy != EffectFailurePolicy.Mandatory)
                            {
                                // `if able`, `may`, and `up to` treat a node they cannot take as
                                // simply not taken, so there is nothing here for a player to close.
                                Pop(continuation);
                                Record(outcomes, events, node.Id,
                                    EffectExecutionResultKind.DeclinedOptional);
                                continue;
                            }

                            // Milestone 6.5ae: a mandatory node whose every option is refused pauses
                            // with all of them greyed and offers p.10's stop, which the player takes.
                            // Milestone 6.5ac stopped here without asking, because a mandatory Choose
                            // has no decline and every other control is withheld while a choice
                            // stands; the stop is the command that position was missing.
                            return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                                frame, node, context, EffectChoiceKind.SelectOption,
                                node.ChildNodeIds.Select(EffectChoiceCandidate.Option).ToArray(),
                                node.MinimumSelections, node.MaximumSelections,
                                unavailableCandidates: unavailable,
                                canStop: everyOptionRefused));
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

                    case EffectNodeKind.RepeatAction:
                        // Milestone 6.7. Nothing is performed here. An eligible `may` opens p.10's
                        // confirm-or-decline; an eligible `if able` requests without asking; and an
                        // instance that is already a repeat skips in silence, which is the whole bound
                        // and is why this node kind refuses to be mandatory.
                        if (!RepeatedActionQuery.CanRepeat(frame))
                        {
                            Pop(continuation);
                            Record(outcomes, events, node.Id,
                                EffectExecutionResultKind.DeclinedOptional);
                            continue;
                        }

                        if (node.FailurePolicy == EffectFailurePolicy.May)
                        {
                            return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                                frame, node, context, EffectChoiceKind.ConfirmOrDecline,
                                Array.Empty<EffectChoiceCandidate>(), 0, 0));
                        }

                        Pop(continuation);
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                        continue;

                    case EffectNodeKind.Capability:
                        var handler = context.Catalog.Load(node.Capability);
                        var result = handler.Execute(BuildCapabilityContext(frame, node, outcomes, context));
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
                    // Milestone 6.8l: a selection that declares what it selects for offers only the
                    // objects that consumer can act on, so the prompt stops naming targets the effect
                    // would refuse. Every other capability's candidates come back exactly as offered.
                    var offered = EffectSelectionEligibilityQuery.EligibleCandidates(
                        node, result.Choice.Candidates, frame, outcomes, context);
                    if (offered.Count == 0 || offered.Count < result.Choice.MinimumSelections)
                    {
                        // Nothing left to name is the same position as nothing to name in the first
                        // place, which is what the handler itself reports as impossible. The refusal
                        // carries no result, because the one this node produced was a request for a
                        // choice that is not going to be opened.
                        return RefuseCapability(
                            frame, node, null, continuation, outcomes, choices, events, context);
                    }

                    return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                        frame, node, context, EffectChoiceKind.SelectTarget,
                        offered.Select(EffectChoiceCandidate.Value_).ToArray(),
                        result.Choice.MinimumSelections,
                        Math.Min(result.Choice.MaximumSelections, offered.Count),
                        result.Choice.Chooser,
                        chooserHeroOverride: result.Choice.ChooserHeroId));

                case EffectExecutionResultKind.ImpossibleMandatory:
                    return RefuseCapability(
                        frame, node, result, continuation, outcomes, choices, events, context);

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

        /// <summary>
        /// What a capability node that cannot be taken does to the resolution: stops it where the node
        /// is mandatory, and is skipped where p.10's `if able`, `may`, and `up to` say a step nobody
        /// can take is simply not taken. Returns a terminal result only in the first case.
        /// </summary>
        private static EffectResolutionStepResult RefuseCapability(
            PendingResolution frame,
            EffectNodeDefinition node,
            EffectExecutionResult result,
            List<EffectContinuationFrame> continuation,
            List<EffectNodeOutcome> outcomes,
            List<EffectChoiceRecord> choices,
            List<EffectResolutionEvent> events,
            EffectResolutionContext context)
        {
            Pop(continuation);
            if (node.FailurePolicy != EffectFailurePolicy.Mandatory)
            {
                Record(outcomes, events, node.Id, EffectExecutionResultKind.DeclinedOptional, result);
                return null;
            }

            // The action stops here. Everything already applied stays applied - a repeat this
            // instance already asked for included, because asking is applied work like any other.
            Record(outcomes, events, node.Id, EffectExecutionResultKind.ImpossibleMandatory, result);
            continuation.Clear();
            return EffectResolutionStepResult.Accept(
                Rebuild(frame, continuation, outcomes, choices), true, events,
                RepeatedActionQuery.WasRequested(context.Definition, outcomes),
                RecordedLanePushQuery.Recorded(context.Definition, outcomes));
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
            EffectChooserRole chooserOverride = EffectChooserRole.None,
            IReadOnlyList<EffectChoiceCandidate> unavailableCandidates = null,
            bool canStop = false,
            HeroId chooserHeroOverride = default) =>
            new PendingEffectChoice(
                frame.ResolutionId,
                new EffectChoiceId($"node-{node.Id.Value}"),
                node.Id,
                // Milestone 7.1a: a handler that names the exact hero has already answered this, and
                // a role would only re-derive a worse version of the same answer.
                chooserHeroOverride.IsDefined
                    ? chooserHeroOverride
                    : ResolveChooser(
                        chooserOverride != EffectChooserRole.None ? chooserOverride : node.Chooser,
                        frame,
                        context),
                kind,
                candidates,
                minimumSelections,
                maximumSelections,
                null,
                unavailableCandidates,
                canStop);

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
            var result = handler.Execute(BuildCapabilityContext(frame, node, outcomes, context));
            return result.Outcomes.Any(outcome =>
                outcome.Value.Kind == EffectValueKind.Boolean && outcome.Value.BooleanValue);
        }

        internal static EffectCapabilityContext BuildCapabilityContext(
            PendingResolution frame,
            EffectNodeDefinition node,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context) =>
            BuildCapabilityContext(
                frame, node, outcomes, context, ResolveInputBindings(node, frame, outcomes), null);

        /// <summary>
        /// The same context, with some inputs answered rather than resolved.
        ///
        /// Milestone 6.8l asks a node what it would do with a subject a prior selection has not chosen
        /// yet, so it supplies that subject instead of the nothing the binding currently resolves to.
        /// It builds the context here rather than assembling one of its own, because everything else a
        /// handler is allowed to see - the actor, the source card, the round, the modifications, the
        /// components - has to be the same in a probe as in the execution it is asking about, and one
        /// construction site is what keeps it so.
        /// </summary>
        internal static EffectCapabilityContext BuildCapabilityContext(
            PendingResolution frame,
            EffectNodeDefinition node,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            IReadOnlyDictionary<string, EffectValue> resolvedInputs,
            IReadOnlyDictionary<string, EffectValue> substitutedInputs)
        {
            var inputs = resolvedInputs;
            if (substitutedInputs != null && substitutedInputs.Count > 0)
            {
                var merged = new Dictionary<string, EffectValue>(StringComparer.Ordinal);
                foreach (var entry in resolvedInputs)
                {
                    merged[entry.Key] = entry.Value;
                }

                foreach (var entry in substitutedInputs)
                {
                    merged[entry.Key] = entry.Value;
                }

                inputs = merged;
            }

            return new EffectCapabilityContext(
                node.Id,
                node.FailurePolicy,
                frame.ActorId,
                frame.ControllerId,
                frame.SourceCardInstanceId,
                frame.SourceDefinitionReference,
                frame.ActionDefinitionId,
                inputs,
                context.RoundIndex,
                context.TurnIndex,
                context.Participants,
                context.Timing,
                context.Modifications,
                context.CardState,
                context.Components,
                context.HeroItems);
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
