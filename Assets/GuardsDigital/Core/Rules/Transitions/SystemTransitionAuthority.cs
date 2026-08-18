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
        private readonly List<SystemTransitionStepId> completedStepIds = new List<SystemTransitionStepId>();

        public SystemTransitionAuthority(SystemTransitionCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>Where the running transition is stopped, or <c>null</c> when none is running.</summary>
        public PendingSystemTransitionStep PendingStep { get; private set; }

        public bool IsRunning => PendingStep != null;

        /// <summary>The steps this transition has completed, in the order they happened.</summary>
        public IReadOnlyList<SystemTransitionStepId> CompletedStepIds => completedStepIds.AsReadOnly();

        public SystemTransitionCatalog Catalog => catalog;

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

            if (command.ChoosingSide != pending.ChoosingSide)
            {
                return SystemTransitionResult.Reject(SystemTransitionRejectionReason.WrongChooser);
            }

            if (!pending.Options.Any(option => option.Equals(command.OptionId)))
            {
                return SystemTransitionResult.Reject(SystemTransitionRejectionReason.IllegalOption);
            }

            completedStepIds.Add(pending.StepId);
            PendingStep = null;
            return Advance(catalog.Load(pending.DefinitionId), pending.InstanceId);
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
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var authority = new SystemTransitionAuthority(catalog);
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
            if (!definition.TryGetStep(pending.StepId, out var step) || !step.PausesForChoice ||
                step.ChoosingSide != pending.ChoosingSide ||
                !step.Options.SequenceEqual(pending.Options))
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

            authority.completedStepIds.AddRange(pending.CompletedStepIds);
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
            for (var index = completedStepIds.Count; index < definition.Steps.Count; index++)
            {
                var step = definition.Steps[index];
                if (!step.PausesForChoice)
                {
                    completedStepIds.Add(step.Id);
                    continue;
                }

                PendingStep = new PendingSystemTransitionStep(
                    instanceId,
                    definition.Id,
                    step.Id,
                    step.ChoosingSide,
                    step.Options,
                    completedStepIds);
                return SystemTransitionResult.Accept(PendingStep, completedStepIds, false);
            }

            PendingStep = null;
            var completed = completedStepIds.ToArray();
            completedStepIds.Clear();
            return SystemTransitionResult.Accept(null, completed, true);
        }
    }
}
