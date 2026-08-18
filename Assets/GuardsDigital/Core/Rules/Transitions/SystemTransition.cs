using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.Transitions
{
    /// <summary>The stable identity of one catalog-owned transition definition.</summary>
    public readonly struct SystemTransitionDefinitionId : IEquatable<SystemTransitionDefinitionId>
    {
        public SystemTransitionDefinitionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A transition definition ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(SystemTransitionDefinitionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemTransitionDefinitionId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// One running transition. Distinct from the definition ID for the reason every definition/instance
    /// pair in this engine is distinct: the same transition can run twice in a match, and a continuation
    /// that named only the definition could be answered by a command meant for the previous run.
    /// </summary>
    public readonly struct SystemTransitionInstanceId : IEquatable<SystemTransitionInstanceId>
    {
        public SystemTransitionInstanceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A transition instance ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(SystemTransitionInstanceId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemTransitionInstanceId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// The stable identity of one step inside a transition definition.
    ///
    /// Stable rather than positional on purpose: the resume location is a set of completed step
    /// identities, and an index would renumber under a definition change while claiming the same steps
    /// had happened.
    /// </summary>
    public readonly struct SystemTransitionStepId : IEquatable<SystemTransitionStepId>
    {
        public SystemTransitionStepId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A transition step ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(SystemTransitionStepId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemTransitionStepId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>One legal answer to a paused step.</summary>
    public readonly struct SystemTransitionOptionId : IEquatable<SystemTransitionOptionId>
    {
        public SystemTransitionOptionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A transition option ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(SystemTransitionOptionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemTransitionOptionId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// One step of a transition: its stable identity, whether it pauses for a player, and - when it
    /// does - who chooses and what they may choose.
    ///
    /// The chooser and the options are declared here and *recorded* on the continuation when the step
    /// pauses, which is a deliberate boundary rather than an oversight. It is everything this
    /// milestone's checks need and it is not everything milestone 6.11 will need: a respawn chooses per
    /// minion and per obstacle, so its chooser depends on live board state. Widening the definition
    /// side belongs to the milestone that has a second consumer to widen it for; what is fixed here is
    /// that the continuation records what it paused with rather than re-deriving it from a handler.
    /// </summary>
    public sealed class SystemTransitionStepDefinition
    {
        public SystemTransitionStepDefinition(SystemTransitionStepId id)
            : this(id, TeamSide.None, Array.Empty<SystemTransitionOptionId>())
        {
        }

        public SystemTransitionStepDefinition(
            SystemTransitionStepId id,
            TeamSide choosingSide,
            IEnumerable<SystemTransitionOptionId> options)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("A transition step requires a stable identity.", nameof(id));
            }

            var optionArray = (options ?? throw new ArgumentNullException(nameof(options))).ToArray();
            if (optionArray.Any(option => !option.IsDefined) ||
                optionArray.Distinct().Count() != optionArray.Length)
            {
                throw new ArgumentException("Transition step options must be defined and unique.", nameof(options));
            }

            var pauses = optionArray.Length > 0;
            if (pauses != (choosingSide == TeamSide.Red || choosingSide == TeamSide.Blue))
            {
                throw new ArgumentException(
                    "A step pauses exactly when it names a chooser and offers options; a step that " +
                    "offered options to nobody, or asked a chooser for nothing, could never be answered.");
            }

            Id = id;
            ChoosingSide = choosingSide;
            Options = Array.AsReadOnly(optionArray);
        }

        public SystemTransitionStepId Id { get; }

        /// <summary>The side that answers this step, or the named absence when it resolves itself.</summary>
        public TeamSide ChoosingSide { get; }

        public IReadOnlyList<SystemTransitionOptionId> Options { get; }

        public bool PausesForChoice => Options.Count > 0;
    }

    /// <summary>
    /// An immutable, catalog-owned multi-step transition: an ordered, non-empty list of stable steps.
    ///
    /// It is content rather than behaviour, exactly as an <c>EffectDefinition</c> is, so a continuation
    /// can name it and a restore can rebind it from the trusted catalog instead of the payload carrying
    /// what the steps mean.
    /// </summary>
    public sealed class SystemTransitionDefinition
    {
        public SystemTransitionDefinition(
            SystemTransitionDefinitionId id,
            string displayName,
            IEnumerable<SystemTransitionStepDefinition> steps)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("A transition definition requires a stable identity.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A transition definition requires a display name.", nameof(displayName));
            }

            var stepArray = (steps ?? throw new ArgumentNullException(nameof(steps))).ToArray();
            if (stepArray.Length == 0 || stepArray.Any(step => step == null))
            {
                throw new ArgumentException("A transition requires at least one non-null step.", nameof(steps));
            }

            if (stepArray.Select(step => step.Id).Distinct().Count() != stepArray.Length)
            {
                throw new ArgumentException("Transition step identities must be unique.", nameof(steps));
            }

            Id = id;
            DisplayName = displayName;
            Steps = Array.AsReadOnly(stepArray);
        }

        public SystemTransitionDefinitionId Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<SystemTransitionStepDefinition> Steps { get; }

        public bool TryGetStep(SystemTransitionStepId stepId, out SystemTransitionStepDefinition step)
        {
            step = Steps.FirstOrDefault(value => value.Id.Equals(stepId));
            return step != null;
        }

        public int IndexOf(SystemTransitionStepId stepId)
        {
            for (var index = 0; index < Steps.Count; index++)
            {
                if (Steps[index].Id.Equals(stepId)) return index;
            }

            return -1;
        }
    }

    public enum SystemTransitionDefinitionLoadRejectionReason
    {
        None = 0,
        MissingDefinition = 1
    }

    public sealed class SystemTransitionDefinitionLoadException : Exception
    {
        public SystemTransitionDefinitionLoadException(
            SystemTransitionDefinitionLoadRejectionReason reason, string message)
            : base(message)
        {
            Reason = reason;
        }

        public SystemTransitionDefinitionLoadRejectionReason Reason { get; }
    }

    /// <summary>
    /// The trusted transition definitions this build supplies, in the shape
    /// <c>ComponentDefinitionCatalog</c> and <c>EffectCapabilityCatalog</c> already use.
    ///
    /// A restored continuation names its definition and is rebound here, and refused where this build
    /// no longer supplies it - which is why no registration list joins the wire: a payload naming a
    /// definition the catalog does not have is already refusable without one.
    /// </summary>
    public sealed class SystemTransitionCatalog
    {
        private readonly IReadOnlyList<SystemTransitionDefinition> definitions;

        public SystemTransitionCatalog(IEnumerable<SystemTransitionDefinition> definitions)
        {
            var array = (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray();
            if (array.Any(value => value == null))
            {
                throw new ArgumentException("Transition definitions must not be null.", nameof(definitions));
            }

            if (array.Select(value => value.Id).Distinct().Count() != array.Length)
            {
                throw new ArgumentException("Transition definition identities must be unique.", nameof(definitions));
            }

            this.definitions = Array.AsReadOnly(array);
        }

        public static SystemTransitionCatalog Empty { get; } =
            new SystemTransitionCatalog(Array.Empty<SystemTransitionDefinition>());

        public IReadOnlyList<SystemTransitionDefinition> Definitions => definitions;

        public SystemTransitionDefinition Load(SystemTransitionDefinitionId id)
        {
            var definition = definitions.FirstOrDefault(value => value.Id.Equals(id));
            if (definition == null)
            {
                throw new SystemTransitionDefinitionLoadException(
                    SystemTransitionDefinitionLoadRejectionReason.MissingDefinition,
                    $"Transition definition '{id}' is unavailable in this build.");
            }

            return definition;
        }

        public bool Contains(SystemTransitionDefinitionId id) =>
            definitions.Any(value => value.Id.Equals(id));
    }

    /// <summary>One answer to a paused transition step, as the pending continuation projected it.</summary>
    public readonly struct SystemTransitionChoiceCommand : IEquatable<SystemTransitionChoiceCommand>
    {
        public SystemTransitionChoiceCommand(
            SystemTransitionInstanceId instanceId,
            SystemTransitionStepId stepId,
            TeamSide choosingSide,
            SystemTransitionOptionId optionId)
        {
            InstanceId = instanceId;
            StepId = stepId;
            ChoosingSide = choosingSide;
            OptionId = optionId;
        }

        public SystemTransitionInstanceId InstanceId { get; }
        public SystemTransitionStepId StepId { get; }
        public TeamSide ChoosingSide { get; }
        public SystemTransitionOptionId OptionId { get; }

        public bool IsDefined =>
            InstanceId.IsDefined && StepId.IsDefined && OptionId.IsDefined &&
            (ChoosingSide == TeamSide.Red || ChoosingSide == TeamSide.Blue);

        public bool Equals(SystemTransitionChoiceCommand other) =>
            InstanceId.Equals(other.InstanceId) && StepId.Equals(other.StepId) &&
            ChoosingSide == other.ChoosingSide && OptionId.Equals(other.OptionId);

        public override bool Equals(object obj) => obj is SystemTransitionChoiceCommand other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = InstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ StepId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ChoosingSide;
                return (hashCode * 397) ^ OptionId.GetHashCode();
            }
        }

        public override string ToString() => $"{InstanceId}/{StepId} -> {OptionId} ({ChoosingSide})";
    }

    /// <summary>
    /// The intra-stage continuation itself: a multi-step transition stopped on one step, holding
    /// everything needed to resume exactly where it stopped and nothing that would let it resume
    /// anywhere else.
    ///
    /// Each field is one clause of milestone 6.10a's first acceptance check. The owning transition is
    /// <see cref="InstanceId"/> with the <see cref="DefinitionId"/> it runs; the stable current step is
    /// <see cref="StepId"/>; the chooser is <see cref="ChoosingSide"/>; the legal choice context is
    /// <see cref="Options"/> with the exact <see cref="Invocations"/> that answer them; and the exact
    /// resume location is <see cref="CompletedStepIds"/> - the steps that have actually happened,
    /// rather than an index into a list a definition change could renumber underneath it.
    ///
    /// It is a value the authority holds, not a callback: there is no delegate, no handler reference
    /// and no closure anywhere in it, which is what lets it cross the snapshot boundary at all and is
    /// what the fourth acceptance check's conformance rule exists to keep true.
    /// </summary>
    public sealed class PendingSystemTransitionStep
    {
        public PendingSystemTransitionStep(
            SystemTransitionInstanceId instanceId,
            SystemTransitionDefinitionId definitionId,
            SystemTransitionStepId stepId,
            TeamSide choosingSide,
            IEnumerable<SystemTransitionOptionId> options,
            IEnumerable<SystemTransitionStepId> completedStepIds)
        {
            if (!instanceId.IsDefined || !definitionId.IsDefined || !stepId.IsDefined)
            {
                throw new ArgumentException("A paused transition step requires stable typed identities.");
            }

            if (choosingSide != TeamSide.Red && choosingSide != TeamSide.Blue)
            {
                throw new ArgumentException(
                    "A paused step is paused on somebody; a step with no chooser never stops.",
                    nameof(choosingSide));
            }

            var optionArray = (options ?? throw new ArgumentNullException(nameof(options))).ToArray();
            if (optionArray.Length == 0 || optionArray.Any(option => !option.IsDefined) ||
                optionArray.Distinct().Count() != optionArray.Length)
            {
                throw new ArgumentException(
                    "A paused step offers at least one defined option and never the same one twice.",
                    nameof(options));
            }

            var completedArray = (completedStepIds ?? throw new ArgumentNullException(nameof(completedStepIds)))
                .ToArray();
            if (completedArray.Any(completed => !completed.IsDefined) ||
                completedArray.Distinct().Count() != completedArray.Length ||
                completedArray.Any(completed => completed.Equals(stepId)))
            {
                throw new ArgumentException(
                    "Completed steps are unique, defined, and never include the step still pending.",
                    nameof(completedStepIds));
            }

            InstanceId = instanceId;
            DefinitionId = definitionId;
            StepId = stepId;
            ChoosingSide = choosingSide;
            Options = Array.AsReadOnly(optionArray);
            CompletedStepIds = Array.AsReadOnly(completedArray);
            Invocations = Array.AsReadOnly(optionArray
                .Select(option => new SystemTransitionChoiceCommand(instanceId, stepId, choosingSide, option))
                .ToArray());
        }

        public SystemTransitionInstanceId InstanceId { get; }
        public SystemTransitionDefinitionId DefinitionId { get; }
        public SystemTransitionStepId StepId { get; }
        public TeamSide ChoosingSide { get; }
        public IReadOnlyList<SystemTransitionOptionId> Options { get; }

        /// <summary>The steps that have already happened. The exact resume location, stated positively.</summary>
        public IReadOnlyList<SystemTransitionStepId> CompletedStepIds { get; }

        /// <summary>The exact commands that answer this step, in the option order the definition declares.</summary>
        public IReadOnlyList<SystemTransitionChoiceCommand> Invocations { get; }
    }

    public enum SystemTransitionRejectionReason
    {
        None = 0,

        /// <summary>The build supplies no definition by that name.</summary>
        UnknownDefinition = 1,

        /// <summary>A transition is already running; two at once would have two resume locations.</summary>
        TransitionAlreadyRunning = 2,

        /// <summary>Nothing is paused, so there is nothing to answer.</summary>
        NoPendingStep = 3,

        /// <summary>The answer names a different running transition.</summary>
        WrongTransitionInstance = 4,

        /// <summary>The answer names a step that is not the one paused - a completed one, or a later one.</summary>
        StaleStep = 5,

        /// <summary>The answer comes from a side that is not the chooser.</summary>
        WrongChooser = 6,

        /// <summary>The option is not one this step offered.</summary>
        IllegalOption = 7
    }

    /// <summary>What one attempted begin or resumption did.</summary>
    public sealed class SystemTransitionResult
    {
        private SystemTransitionResult(
            bool accepted,
            SystemTransitionRejectionReason rejectionReason,
            PendingSystemTransitionStep pendingStep,
            IEnumerable<SystemTransitionStepId> completedStepIds,
            bool transitionCompleted)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            PendingStep = pendingStep;
            CompletedStepIds = Array.AsReadOnly(completedStepIds.ToArray());
            TransitionCompleted = transitionCompleted;
        }

        public bool Accepted { get; }
        public SystemTransitionRejectionReason RejectionReason { get; }

        /// <summary>Where the transition is now stopped, or <c>null</c> when it is not stopped at all.</summary>
        public PendingSystemTransitionStep PendingStep { get; }

        public IReadOnlyList<SystemTransitionStepId> CompletedStepIds { get; }

        public bool TransitionCompleted { get; }

        internal static SystemTransitionResult Reject(SystemTransitionRejectionReason reason)
        {
            if (reason == SystemTransitionRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(nameof(reason));
            }

            return new SystemTransitionResult(
                false, reason, null, Array.Empty<SystemTransitionStepId>(), false);
        }

        internal static SystemTransitionResult Accept(
            PendingSystemTransitionStep pendingStep,
            IEnumerable<SystemTransitionStepId> completedStepIds,
            bool transitionCompleted) =>
            new SystemTransitionResult(
                true, SystemTransitionRejectionReason.None, pendingStep, completedStepIds, transitionCompleted);
    }
}
