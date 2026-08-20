using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.Transitions
{
    /// <summary>
    /// The persisted state of a <see cref="SystemTransitionAuthority"/>, in the arity-disciplined shape
    /// milestone 6.1g gave every restore state: adding a persisted field changes this type's
    /// <c>Deconstruct</c>, so capture, write, read and restore stop compiling together rather than
    /// silently dropping it from the stream.
    ///
    /// A transition that is running but not paused is not a state this contract has: every step either
    /// resolves itself immediately or stops, so between commands a transition is either absent or
    /// stopped on a step. That is why one nullable continuation is the whole of it.
    /// </summary>
    public sealed class SystemTransitionRestoreState
    {
        public SystemTransitionRestoreState(PendingSystemTransitionStep pendingStep)
        {
            PendingStep = pendingStep;
        }

        public PendingSystemTransitionStep PendingStep { get; }

        public void Deconstruct(out PendingSystemTransitionStep pendingStep)
        {
            pendingStep = PendingStep;
        }
    }

    /// <summary>
    /// Owns the intra-stage continuation of a multi-step authoritative system transition (milestone
    /// 6.10a).
    ///
    /// The contract this establishes is deliberately not a transition engine. It answers one question -
    /// where is this transition stopped, and what may happen next - and it answers it as authority-owned
    /// serializable state rather than as a callback a handler is holding, because a callback cannot
    /// cross a snapshot boundary, cannot be replayed, and cannot be truncated by an Undo. The two named
    /// production consumers are milestone 6.11's Lane-Push respawn and milestone 7.5's progression;
    /// neither is implemented here, and the first consumer is the bounded synthetic transition this
    /// build registers.
    ///
    /// The step cursor is derived from the completed steps rather than stored beside them. One record of
    /// where a transition is is the whole point: a cursor and a completed set that could disagree is the
    /// defect this contract exists to make unrepresentable, and it is what "cannot skip, repeat, or
    /// restart a completed step" means once it is a structural property instead of a check.
    /// </summary>
    public sealed class SystemTransitionAuthority
    {
        private readonly SystemTransitionCatalog catalog;
        private readonly ISystemTransitionStepRules rules;
        private readonly List<SystemTransitionStepId> completedStepIds = new List<SystemTransitionStepId>();
        private readonly List<SystemTransitionItemId> completedItemIds = new List<SystemTransitionItemId>();

        public SystemTransitionAuthority(SystemTransitionCatalog catalog)
            : this(catalog, DeclaredSystemTransitionStepRules.Instance)
        {
        }

        /// <summary>
        /// Milestone 6.11. The rules are a composed collaborator rather than a stored callback: they
        /// own what a step does and this type owns where the transition is, so there is still exactly
        /// one record of the position and it is still the one the snapshot carries.
        /// </summary>
        public SystemTransitionAuthority(
            SystemTransitionCatalog catalog, ISystemTransitionStepRules rules)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>Where the running transition is stopped, or <c>null</c> when none is running.</summary>
        public PendingSystemTransitionStep PendingStep { get; private set; }

        public bool IsRunning => PendingStep != null;

        /// <summary>The steps this transition has completed, in the order they happened.</summary>
        public IReadOnlyList<SystemTransitionStepId> CompletedStepIds => completedStepIds.AsReadOnly();

        /// <summary>
        /// The items of the current step already answered, for a step that stops more than once
        /// (milestone 6.11). It is cleared when the step itself completes, because it is part of where
        /// the transition is inside a step rather than a second history of the transition.
        /// </summary>
        public IReadOnlyList<SystemTransitionItemId> CompletedItemIds => completedItemIds.AsReadOnly();

        public SystemTransitionCatalog Catalog => catalog;

        /// <summary>
        /// What the current pause means, described by the rules that produced it, or <c>null</c> when
        /// nothing is paused or those rules do not describe themselves (milestone 7.8a).
        ///
        /// A read. It asks the composed rules a question about state they already hold and changes
        /// nothing, so calling it never moves the transition and calling it twice answers twice.
        /// </summary>
        public SystemTransitionStepDescription DescribePendingStep() =>
            PendingStep != null && rules is IDescribingSystemTransitionStepRules describing
                ? describing.Describe(PendingStep)
                : null;

        /// <summary>
        /// Begins a declared transition and runs it until it stops on a step or finishes.
        ///
        /// The instance identity is composed from the definition and the caller's occasion rather than
        /// minted from a counter, so a replay produces the same identity as the original run; a counter
        /// would produce an equal match with different names and break the byte-identity the aggregate
        /// invariance fixture pins.
        /// </summary>
        public SystemTransitionResult TryBegin(
            SystemTransitionDefinitionId definitionId,
            SystemTransitionInstanceId instanceId)
        {
            if (IsRunning)
            {
                return SystemTransitionResult.Reject(
                    SystemTransitionRejectionReason.TransitionAlreadyRunning);
            }

            if (!definitionId.IsDefined || !instanceId.IsDefined || !catalog.Contains(definitionId))
            {
                return SystemTransitionResult.Reject(SystemTransitionRejectionReason.UnknownDefinition);
            }

            completedStepIds.Clear();
            completedItemIds.Clear();
            return Advance(catalog.Load(definitionId), instanceId);
        }

        /// <summary>
        /// Answers the paused step and runs on until the transition stops again or finishes.
        ///
        /// Every refusal leaves the continuation exactly as it was, which is what makes a rejected or
        /// stale answer indistinguishable from one that never arrived. The four refusals are the four
        /// ways an answer can fail to be about the step that is actually paused: a different running
        /// transition, a step that is not the pending one, somebody other than the chooser, and an
        /// option this step never offered.
        /// </summary>
        public SystemTransitionResult TryAnswer(SystemTransitionChoiceCommand command)
        {
            var pending = PendingStep;
            if (pending == null)
            {
                return SystemTransitionResult.Reject(SystemTransitionRejectionReason.NoPendingStep);
            }

            if (!command.InstanceId.Equals(pending.InstanceId))
            {
                return SystemTransitionResult.Reject(
                    SystemTransitionRejectionReason.WrongTransitionInstance);
            }

            if (!command.StepId.Equals(pending.StepId))
            {
                // Covers both directions at once, which is the point: a completed step and a step the
                // transition has not reached are equally not the step that is paused.
                return SystemTransitionResult.Reject(SystemTransitionRejectionReason.StaleStep);
            }

            if (!command.ItemId.Equals(pending.ItemId))
            {
                // The same statement one level down, for a step that stops once per minion: an answer
                // about an already-placed minion, or one not yet reached, is not about this pause.
                return SystemTransitionResult.Reject(SystemTransitionRejectionReason.StaleItem);
            }

            if (command.ChoosingSide != pending.ChoosingSide)
            {
                return SystemTransitionResult.Reject(SystemTransitionRejectionReason.WrongChooser);
            }

            if (!pending.Options.Any(option => option.Equals(command.OptionId)))
            {
                return SystemTransitionResult.Reject(SystemTransitionRejectionReason.IllegalOption);
            }

            var definition = catalog.Load(pending.DefinitionId);
            var step = definition.Steps.Single(candidate => candidate.Id.Equals(pending.StepId));
            rules.ApplyAnswer(new SystemTransitionAnswerContext(
                pending.InstanceId, definition, step, pending.ItemId, pending.ChoosingSide,
                command.OptionId));

            // A declared step is finished by its one answer. A derived step is finished when its rules
            // say so, because only they know whether another minion is owed a Place, so answering one
            // of its items completes the item and leaves the step where it is.
            if (pending.ItemId.IsDefined)
            {
                completedItemIds.Add(pending.ItemId);
            }
            else
            {
                completedStepIds.Add(pending.StepId);
                completedItemIds.Clear();
            }

            PendingStep = null;
            return Advance(definition, pending.InstanceId);
        }

        public SystemTransitionRestoreState CaptureRestoreState() =>
            new SystemTransitionRestoreState(PendingStep);

        /// <summary>
        /// Rebuilds an authority from a persisted continuation, refusing any payload the trusted
        /// definitions do not reproduce.
        ///
        /// What is validated is the shape rather than the choice: the definition must still exist, the
        /// paused step must be one it declares, that step must still pause and still offer exactly the
        /// options the payload recorded, its chooser must be the one it declares, and the completed
        /// steps must be exactly the definition's steps before it, in the definition's order. A payload
        /// that disagrees on any of those describes a transition this build cannot run, so it is refused
        /// rather than repaired - the same decision the paused after-action correction makes when the
        /// board does not reproduce it.
        /// </summary>
        public static SystemTransitionAuthority Restore(
            SystemTransitionRestoreState state,
            SystemTransitionCatalog catalog)
        {
            return Restore(state, catalog, DeclaredSystemTransitionStepRules.Instance);
        }

        /// <summary>
        /// The same restore against a build that supplies real step rules (milestone 6.11).
        ///
        /// A derived step's options are not in the catalog to be compared with, so they are checked
        /// against the only other thing that can produce them: the rules, re-run over the restored
        /// state. That is a stronger check than the declared one rather than a weaker substitute for
        /// it - a payload whose paused respawn disagrees with the board it was taken from describes a
        /// transition this build would not have produced, so it is refused.
        /// </summary>
        public static SystemTransitionAuthority Restore(
            SystemTransitionRestoreState state,
            SystemTransitionCatalog catalog,
            ISystemTransitionStepRules rules)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            var authority = new SystemTransitionAuthority(catalog, rules);
            var pending = state.PendingStep;
            if (pending == null)
            {
                return authority;
            }

            if (!catalog.Contains(pending.DefinitionId))
            {
                throw new ArgumentException(
                    $"Restored transition '{pending.DefinitionId}' is unavailable in this build.",
                    nameof(state));
            }

            var definition = catalog.Load(pending.DefinitionId);
            if (!definition.TryGetStep(pending.StepId, out var step) || !step.MayPause)
            {
                throw new ArgumentException(
                    $"Restored transition step '{pending.StepId}' does not match the definition this build supplies.",
                    nameof(state));
            }

            var expectedCompleted = definition.Steps
                .Take(definition.IndexOf(pending.StepId))
                .Select(value => value.Id)
                .ToArray();
            if (!expectedCompleted.SequenceEqual(pending.CompletedStepIds))
            {
                throw new ArgumentException(
                    "Restored transition completed steps do not reproduce the definition's order.",
                    nameof(state));
            }

            if (step.ChoiceSource == SystemTransitionStepChoiceSource.Declared)
            {
                if (pending.ItemId.IsDefined || step.ChoosingSide != pending.ChoosingSide ||
                    !step.Options.SequenceEqual(pending.Options))
                {
                    throw new ArgumentException(
                        $"Restored transition step '{pending.StepId}' does not match the definition this build supplies.",
                        nameof(state));
                }
            }
            else
            {
                var derived = rules.ResolveStep(new SystemTransitionStepContext(
                    pending.InstanceId, definition, step, pending.CompletedStepIds,
                    pending.CompletedItemIds));
                if (derived.Kind != SystemTransitionStepResolutionKind.Paused ||
                    !derived.ItemId.Equals(pending.ItemId) ||
                    derived.ChoosingSide != pending.ChoosingSide ||
                    !derived.Options.SequenceEqual(pending.Options))
                {
                    throw new ArgumentException(
                        $"Restored transition step '{pending.StepId}' does not reproduce from the " +
                        "state this build restored.",
                        nameof(state));
                }
            }

            authority.completedStepIds.AddRange(pending.CompletedStepIds);
            authority.completedItemIds.AddRange(pending.CompletedItemIds);
            authority.PendingStep = pending;
            return authority;
        }

        /// <summary>
        /// Runs from the current cursor to the next pause or to the end. Steps that resolve themselves
        /// are completed here rather than being offered and auto-answered, so nothing that never paused
        /// can be answered by a command.
        /// </summary>
        private SystemTransitionResult Advance(
            SystemTransitionDefinition definition,
            SystemTransitionInstanceId instanceId)
        {
            while (completedStepIds.Count < definition.Steps.Count)
            {
                var step = definition.Steps[completedStepIds.Count];
                if (step.ChoiceSource == SystemTransitionStepChoiceSource.Declared)
                {
                    PendingStep = new PendingSystemTransitionStep(
                        instanceId,
                        definition.Id,
                        step.Id,
                        SystemTransitionItemId.None,
                        step.ChoosingSide,
                        step.Options,
                        completedStepIds,
                        Array.Empty<SystemTransitionItemId>());
                    return SystemTransitionResult.Accept(PendingStep, completedStepIds, false);
                }

                var resolution = rules.ResolveStep(new SystemTransitionStepContext(
                    instanceId, definition, step, completedStepIds, completedItemIds));
                if (resolution.Kind == SystemTransitionStepResolutionKind.Paused)
                {
                    if (step.ChoiceSource == SystemTransitionStepChoiceSource.SelfResolving)
                    {
                        throw new InvalidOperationException(
                            $"Transition '{definition.Id}' step '{step.Id}' declares that it resolves " +
                            "itself and its rules stopped on a chooser; a step that could pause " +
                            "without saying so would be answerable by a command nothing offered.");
                    }

                    PendingStep = new PendingSystemTransitionStep(
                        instanceId,
                        definition.Id,
                        step.Id,
                        resolution.ItemId,
                        resolution.ChoosingSide,
                        resolution.Options,
                        completedStepIds,
                        completedItemIds);
                    return SystemTransitionResult.Accept(PendingStep, completedStepIds, false);
                }

                if (resolution.Kind == SystemTransitionStepResolutionKind.EndedTransition)
                {
                    // p.8's "the game ends immediately": the steps after this one do not run, and the
                    // completed record says which ones did rather than claiming the rest happened.
                    return Finish();
                }

                if (resolution.Kind == SystemTransitionStepResolutionKind.ItemCompleted)
                {
                    if (completedItemIds.Any(completed => completed.Equals(resolution.ItemId)))
                    {
                        throw new InvalidOperationException(
                            $"Transition '{definition.Id}' step '{step.Id}' reported item " +
                            $"'{resolution.ItemId}' finished twice; an iterating step that could revisit " +
                            "an item would never terminate and would repeat what it had already done.");
                    }

                    completedItemIds.Add(resolution.ItemId);
                    continue;
                }

                completedStepIds.Add(step.Id);
                completedItemIds.Clear();
            }

            return Finish();
        }

        private SystemTransitionResult Finish()
        {
            PendingStep = null;
            var completed = completedStepIds.ToArray();
            completedStepIds.Clear();
            completedItemIds.Clear();
            return SystemTransitionResult.Accept(null, completed, true);
        }
    }
}
