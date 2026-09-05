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
    /// The stable identity of one item inside a step that pauses more than once (milestone 6.11).
    ///
    /// A Lane-Push respawn owes a Place to each minion whose Spawn point an obstacle blocks, and how
    /// many of those there are is a fact about the live board rather than about the catalog. So the
    /// step is one declared step and the pauses inside it are items, which is what lets the
    /// continuation record "which minion is this pause about" without the definition pretending to
    /// know how many minions there will be.
    /// </summary>
    public readonly struct SystemTransitionItemId : IEquatable<SystemTransitionItemId>
    {
        public SystemTransitionItemId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A transition item ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        /// <summary>The named absence: this pause is about the step as a whole, not about an item.</summary>
        public static SystemTransitionItemId None => default;

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(SystemTransitionItemId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemTransitionItemId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// Where a step's chooser and options come from.
    ///
    /// Milestone 6.10a had two of these and named the third as owed: the chooser and the options were
    /// declared in the catalog, which is everything a bounded synthetic transition needs and not
    /// everything a rule needs whose chooser depends on the board. Milestone 6.11 is the second
    /// consumer that widening was reserved for.
    /// </summary>
    public enum SystemTransitionStepChoiceSource
    {
        /// <summary>The step resolves itself. It does work, it asks nobody anything, and it never pauses.</summary>
        SelfResolving = 0,

        /// <summary>The catalog declares the chooser and the options (milestone 6.10a).</summary>
        Declared = 1,

        /// <summary>
        /// The chooser and the options are derived when the step is reached, from live state, by the
        /// step rules the authority is composed with - and are then recorded on the continuation
        /// exactly as a declared one is. Such a step may pause once per item, or not at all.
        /// </summary>
        Derived = 2
    }

    /// <summary>
    /// One step of a transition: its stable identity, whether it pauses for a player, and - when the
    /// catalog is what decides that - who chooses and what they may choose.
    ///
    /// A declared step's chooser and options are declared here and *recorded* on the continuation when
    /// the step pauses. Milestone 6.10a fixed that boundary and named its limit in the same breath: a
    /// respawn chooses per minion and per obstacle, so its chooser depends on live board state, and
    /// widening belonged to the milestone with a second consumer to widen it for. Milestone 6.11 is
    /// that milestone, and <see cref="SystemTransitionStepChoiceSource.Derived"/> is the widening. What
    /// did not move is the thing the contract is about: a derived step records what it paused with on
    /// the continuation too, rather than leaving a handler to re-derive it on resume.
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
            : this(
                id,
                choosingSide,
                options,
                (options ?? throw new ArgumentNullException(nameof(options))).Any()
                    ? SystemTransitionStepChoiceSource.Declared
                    : SystemTransitionStepChoiceSource.SelfResolving)
        {
        }

        private SystemTransitionStepDefinition(
            SystemTransitionStepId id,
            TeamSide choosingSide,
            IEnumerable<SystemTransitionOptionId> options,
            SystemTransitionStepChoiceSource choiceSource)
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

            var declares = optionArray.Length > 0;
            if (declares != (choosingSide == TeamSide.Red || choosingSide == TeamSide.Blue))
            {
                throw new ArgumentException(
                    "A step pauses exactly when it names a chooser and offers options; a step that " +
                    "offered options to nobody, or asked a chooser for nothing, could never be answered.");
            }

            if (declares != (choiceSource == SystemTransitionStepChoiceSource.Declared))
            {
                throw new ArgumentException(
                    "Only a declared step carries a catalog chooser and catalog options.",
                    nameof(choiceSource));
            }

            Id = id;
            ChoosingSide = choosingSide;
            Options = Array.AsReadOnly(optionArray);
            ChoiceSource = choiceSource;
        }

        /// <summary>
        /// A step whose chooser and options the step rules derive when it is reached (milestone 6.11).
        ///
        /// It declares nothing about who chooses or how many pauses it will produce, because on the
        /// consumer this exists for - p.8's respawn - both are answers about the board at the moment
        /// the step runs. What it does declare is that this step is allowed to stop, which is the part
        /// the catalog can honestly know.
        /// </summary>
        public static SystemTransitionStepDefinition Derived(SystemTransitionStepId id) =>
            new SystemTransitionStepDefinition(
                id,
                TeamSide.None,
                Array.Empty<SystemTransitionOptionId>(),
                SystemTransitionStepChoiceSource.Derived);

        public SystemTransitionStepId Id { get; }

        /// <summary>The side that answers a declared step, or the named absence otherwise.</summary>
        public TeamSide ChoosingSide { get; }

        public IReadOnlyList<SystemTransitionOptionId> Options { get; }

        public SystemTransitionStepChoiceSource ChoiceSource { get; }

        /// <summary>Whether the catalog itself says this step stops and on what.</summary>
        public bool PausesForChoice => ChoiceSource == SystemTransitionStepChoiceSource.Declared;

        /// <summary>Whether this step may stop, which a derived step decides for itself when it runs.</summary>
        public bool MayPause => ChoiceSource != SystemTransitionStepChoiceSource.SelfResolving;
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
            SystemTransitionItemId itemId,
            TeamSide choosingSide,
            SystemTransitionOptionId optionId)
        {
            InstanceId = instanceId;
            StepId = stepId;
            ItemId = itemId;
            ChoosingSide = choosingSide;
            OptionId = optionId;
        }

        public SystemTransitionInstanceId InstanceId { get; }
        public SystemTransitionStepId StepId { get; }

        /// <summary>
        /// Which pause of a step that pauses more than once this answers, or the named absence when the
        /// step pauses once for itself. An answer that named the right step and the wrong item would
        /// otherwise be indistinguishable from the right one.
        /// </summary>
        public SystemTransitionItemId ItemId { get; }

        public TeamSide ChoosingSide { get; }
        public SystemTransitionOptionId OptionId { get; }

        public bool IsDefined =>
            InstanceId.IsDefined && StepId.IsDefined && OptionId.IsDefined &&
            (ChoosingSide == TeamSide.Red || ChoosingSide == TeamSide.Blue);

        public bool Equals(SystemTransitionChoiceCommand other) =>
            InstanceId.Equals(other.InstanceId) && StepId.Equals(other.StepId) &&
            ItemId.Equals(other.ItemId) &&
            ChoosingSide == other.ChoosingSide && OptionId.Equals(other.OptionId);

        public override bool Equals(object obj) => obj is SystemTransitionChoiceCommand other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = InstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ StepId.GetHashCode();
                hashCode = (hashCode * 397) ^ ItemId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ChoosingSide;
                return (hashCode * 397) ^ OptionId.GetHashCode();
            }
        }

        public override string ToString() => ItemId.IsDefined
            ? $"{InstanceId}/{StepId}/{ItemId} -> {OptionId} ({ChoosingSide})"
            : $"{InstanceId}/{StepId} -> {OptionId} ({ChoosingSide})";
    }

    /// <summary>
    /// Milestone 8.35u. One question a paused step is currently asking: which item of the step it is
    /// about, who chooses, and what they may choose.
    ///
    /// It exists because rulebook p.15 levels every hero at once. A step that stops on several items
    /// simultaneously has several choosers and several option lists, and those three travel together
    /// or not at all - an item whose options belonged to a different item would be answerable by a
    /// command nothing offered. What it deliberately does NOT carry is a resume location: there is
    /// still exactly one of those, on the pause that holds these, which is the whole reason this is a
    /// list inside one pause rather than a list of pauses.
    /// </summary>
    public sealed class SystemTransitionOpenItem : IEquatable<SystemTransitionOpenItem>
    {
        public SystemTransitionOpenItem(
            SystemTransitionItemId itemId,
            TeamSide choosingSide,
            IEnumerable<SystemTransitionOptionId> options)
        {
            if (choosingSide != TeamSide.Red && choosingSide != TeamSide.Blue)
            {
                throw new ArgumentException(
                    "An open item is open on somebody; an item with no chooser never stops.",
                    nameof(choosingSide));
            }

            var optionArray = (options ?? throw new ArgumentNullException(nameof(options))).ToArray();
            if (optionArray.Length == 0 || optionArray.Any(option => !option.IsDefined) ||
                optionArray.Distinct().Count() != optionArray.Length)
            {
                throw new ArgumentException(
                    "An open item offers at least one defined option and never the same one twice.",
                    nameof(options));
            }

            ItemId = itemId;
            ChoosingSide = choosingSide;
            Options = Array.AsReadOnly(optionArray);
        }

        /// <summary>
        /// Which pause of the step this is, or the named absence when the step pauses once for itself.
        /// </summary>
        public SystemTransitionItemId ItemId { get; }

        public TeamSide ChoosingSide { get; }
        public IReadOnlyList<SystemTransitionOptionId> Options { get; }

        public bool Equals(SystemTransitionOpenItem other) =>
            other != null && ItemId.Equals(other.ItemId) && ChoosingSide == other.ChoosingSide &&
            Options.SequenceEqual(other.Options);

        public override bool Equals(object obj) => Equals(obj as SystemTransitionOpenItem);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ItemId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ChoosingSide;
                return Options.Aggregate(hashCode, (current, option) =>
                    (current * 397) ^ option.GetHashCode());
            }
        }

        public override string ToString() =>
            $"{(ItemId.IsDefined ? ItemId.Value : "<step>")} ({ChoosingSide}, {Options.Count} options)";
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
        /// <summary>
        /// The one-item pause, which is every step but p.15's leveling. Kept as its own door because
        /// most callers genuinely have one item and should not have to build a list to say so.
        /// </summary>
        public PendingSystemTransitionStep(
            SystemTransitionInstanceId instanceId,
            SystemTransitionDefinitionId definitionId,
            SystemTransitionStepId stepId,
            SystemTransitionItemId itemId,
            TeamSide choosingSide,
            IEnumerable<SystemTransitionOptionId> options,
            IEnumerable<SystemTransitionStepId> completedStepIds,
            IEnumerable<SystemTransitionItemId> completedItemIds)
            : this(
                instanceId,
                definitionId,
                stepId,
                new[] { new SystemTransitionOpenItem(itemId, choosingSide, options) },
                completedStepIds,
                completedItemIds)
        {
        }

        /// <summary>
        /// Milestone 8.35u. The general pause: one step, one resume location, and one open item per
        /// chooser the step is currently waiting on.
        /// </summary>
        public PendingSystemTransitionStep(
            SystemTransitionInstanceId instanceId,
            SystemTransitionDefinitionId definitionId,
            SystemTransitionStepId stepId,
            IEnumerable<SystemTransitionOpenItem> openItems,
            IEnumerable<SystemTransitionStepId> completedStepIds,
            IEnumerable<SystemTransitionItemId> completedItemIds)
        {
            if (!instanceId.IsDefined || !definitionId.IsDefined || !stepId.IsDefined)
            {
                throw new ArgumentException("A paused transition step requires stable typed identities.");
            }

            var openArray = (openItems ?? throw new ArgumentNullException(nameof(openItems))).ToArray();
            if (openArray.Length == 0 || openArray.Any(item => item == null))
            {
                throw new ArgumentException(
                    "A paused step is waiting on at least one open item; a step waiting on none is " +
                    "not paused.",
                    nameof(openItems));
            }

            if (openArray.Select(item => item.ItemId).Distinct().Count() != openArray.Length)
            {
                throw new ArgumentException(
                    "A paused step never opens the same item twice; two answers for one item would " +
                    "have two right answers.",
                    nameof(openItems));
            }

            if (openArray.Length > 1 && openArray.Any(item => !item.ItemId.IsDefined))
            {
                throw new ArgumentException(
                    "A step that pauses once for itself has exactly one open item, because the item " +
                    "is what a second answer would have to be told apart by.",
                    nameof(openItems));
            }

            var itemId = openArray[0].ItemId;
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

            var completedItemArray =
                (completedItemIds ?? throw new ArgumentNullException(nameof(completedItemIds))).ToArray();
            if (completedItemArray.Any(completed => !completed.IsDefined) ||
                completedItemArray.Distinct().Count() != completedItemArray.Length ||
                completedItemArray.Any(completed =>
                    openArray.Any(open => open.ItemId.Equals(completed))))
            {
                // Milestone 8.35u generalises 6.11's clause from "the item still pending" to "any
                // item still open": an item that was both answered and still being asked would have
                // two truths about the same question, which is the disagreement the single resume
                // location exists to make unrepresentable.
                throw new ArgumentException(
                    "Completed items are unique, defined, and never include an item still open.",
                    nameof(completedItemIds));
            }

            if (!itemId.IsDefined && completedItemArray.Length > 0)
            {
                throw new ArgumentException(
                    "A step that pauses once for itself has no items, so it can have completed none.",
                    nameof(completedItemIds));
            }

            InstanceId = instanceId;
            DefinitionId = definitionId;
            StepId = stepId;
            OpenItems = Array.AsReadOnly(openArray);
            CompletedStepIds = Array.AsReadOnly(completedArray);
            CompletedItemIds = Array.AsReadOnly(completedItemArray);
            Invocations = Array.AsReadOnly(openArray
                .SelectMany(open => open.Options.Select(option => new SystemTransitionChoiceCommand(
                    instanceId, stepId, open.ItemId, open.ChoosingSide, option)))
                .ToArray());
        }

        public SystemTransitionInstanceId InstanceId { get; }
        public SystemTransitionDefinitionId DefinitionId { get; }
        public SystemTransitionStepId StepId { get; }

        /// <summary>
        /// Milestone 8.35u. Every question this step is currently asking, one per chooser it waits on.
        ///
        /// One entry is the ordinary case and every step but p.15's leveling has exactly one. The
        /// order is the producing rules' own and is stable across a re-derivation, which is what lets
        /// a restored pause be compared with a re-derived one at all.
        /// </summary>
        public IReadOnlyList<SystemTransitionOpenItem> OpenItems { get; }

        /// <summary>
        /// Whether this step is waiting on more than one chooser at once - true only for rulebook
        /// p.15's simultaneous leveling in this build.
        /// </summary>
        public bool IsConcurrent => OpenItems.Count > 1;

        /// <summary>
        /// Which pause of this step the transition is stopped on, or the named absence when the step
        /// pauses once for itself. Milestone 6.11's respawn pauses once per minion whose Spawn point an
        /// obstacle blocks, which is what the acceptance check means by "stable current step/minion
        /// identity".
        ///
        /// <para><b>Milestone 8.35u: this throws when several items are open, and that is the point.</b>
        /// Returning the first would answer a question about "the" item that no longer has one answer,
        /// and a silently wrong first element is the failure mode that survives to a playtest. Every
        /// caller is therefore either provably single-item or reads <see cref="OpenItems"/>.</para>
        /// </summary>
        public SystemTransitionItemId ItemId => Single().ItemId;

        /// <inheritdoc cref="ItemId"/>
        public TeamSide ChoosingSide => Single().ChoosingSide;

        /// <inheritdoc cref="ItemId"/>
        public IReadOnlyList<SystemTransitionOptionId> Options => Single().Options;

        /// <summary>
        /// Milestone 8.35u. The one side this step is waiting on, or <see cref="TeamSide.None"/> when it
        /// is waiting on both.
        ///
        /// <para>Unlike <see cref="ChoosingSide"/> this never throws, because it is what a reader that
        /// summarises a pause by side actually wants: rulebook p.15 levels heroes on both teams at the
        /// same moment, and "neither side in particular" is the honest answer for such a pause rather
        /// than an error. A reader that needs to know exactly who is being waited on reads
        /// <see cref="OpenItems"/>, which always says.</para>
        /// </summary>
        public TeamSide AwaitedSide
        {
            get
            {
                var side = OpenItems[0].ChoosingSide;
                return OpenItems.All(open => open.ChoosingSide == side) ? side : TeamSide.None;
            }
        }

        /// <summary>
        /// Milestone 8.35u. Every option this step is offering, across every open item, once each.
        ///
        /// <para>Unlike <see cref="Options"/> this never throws, because it is what a caller that
        /// wants "the options on the table" actually means when several questions stand - the
        /// headless and Director paths that name an option by identity rather than by finding a
        /// control. Option ids are unique per hero (they name that hero's own two cards), so the
        /// union is unambiguous and an answer built from it still reaches exactly one item through
        /// <see cref="Invocations"/>.</para>
        /// </summary>
        public IReadOnlyList<SystemTransitionOptionId> AllOptions => Array.AsReadOnly(
            OpenItems.SelectMany(open => open.Options).Distinct().ToArray());

        private SystemTransitionOpenItem Single()
        {
            if (OpenItems.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Paused step '{StepId}' is waiting on {OpenItems.Count} items at once, so it has " +
                    "no single item, chooser or option list. Read OpenItems, or Invocations, which " +
                    "names its own item on every command.");
            }

            return OpenItems[0];
        }

        /// <summary>The steps that have already happened. The exact resume location, stated positively.</summary>
        public IReadOnlyList<SystemTransitionStepId> CompletedStepIds { get; }

        /// <summary>
        /// The items of the current step that have already been answered - the rest of the resume
        /// location, for a step that stops more than once. Empty for every other step, and for the
        /// first pause of one that stops many times.
        /// </summary>
        public IReadOnlyList<SystemTransitionItemId> CompletedItemIds { get; }

        /// <summary>
        /// The exact commands that answer this step, in the option order the definition declares,
        /// across every open item.
        ///
        /// <para>Milestone 8.35u: this is the one member that is well defined however many items are
        /// open, because every command names the item it answers. That is why the Director's surface
        /// needed no change to be able to answer all of them - it looks an invocation up by the option
        /// the player pressed and submits it, and the item travels with it.</para>
        /// </summary>
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
        IllegalOption = 7,

        /// <summary>
        /// The answer names the paused step and a different pause of it - a milestone 6.11 refusal,
        /// because a step that stops once per minion has a right answer for each one and answering the
        /// wrong minion is exactly as wrong as answering the wrong step.
        /// </summary>
        StaleItem = 8
    }

    public enum SystemTransitionStepResolutionKind
    {
        /// <summary>The step is done; the transition moves on to the next one.</summary>
        Completed = 0,

        /// <summary>The step stops here, on this chooser, with these options.</summary>
        Paused = 1,

        /// <summary>
        /// One item of a step that iterates finished without stopping, and the step is not done.
        ///
        /// p.8's respawn needs this: most Spawn points respawn a minion with nobody choosing anything,
        /// and the ones an obstacle blocks stop on a Place. Both are items of the same step, so both
        /// have to be recorded the same way - otherwise "which Spawn points have been dealt with" would
        /// have to be re-derived from a board the respawn has been changing, and a resumed transition
        /// could respawn into the same point twice.
        /// </summary>
        ItemCompleted = 3,

        /// <summary>
        /// The transition is over and the steps after this one do not run. p.8's final-Wave and
        /// Throne-capture sentences both end the game mid-transition, and "the game ends immediately"
        /// means the removal and respawn that would have followed never happen.
        /// </summary>
        EndedTransition = 2
    }

    /// <summary>What running one step of a transition did.</summary>
    public sealed class SystemTransitionStepResolution
    {
        private SystemTransitionStepResolution(
            SystemTransitionStepResolutionKind kind,
            SystemTransitionItemId completedItemId,
            IReadOnlyList<SystemTransitionOpenItem> openItems)
        {
            Kind = kind;
            CompletedItemId = completedItemId;
            OpenItems = openItems;
        }

        public SystemTransitionStepResolutionKind Kind { get; }

        /// <summary>
        /// Milestone 8.35u. Every item this step is now waiting on, empty unless it paused.
        /// </summary>
        public IReadOnlyList<SystemTransitionOpenItem> OpenItems { get; }

        /// <summary>The item an <see cref="SystemTransitionStepResolutionKind.ItemCompleted"/> finished.</summary>
        private SystemTransitionItemId CompletedItemId { get; }

        /// <summary>
        /// The one item this resolution is about. For a pause of several items it throws, for the
        /// reason <see cref="PendingSystemTransitionStep.ItemId"/> does.
        /// </summary>
        public SystemTransitionItemId ItemId =>
            Kind == SystemTransitionStepResolutionKind.Paused ? Single().ItemId : CompletedItemId;

        /// <inheritdoc cref="ItemId"/>
        public TeamSide ChoosingSide =>
            Kind == SystemTransitionStepResolutionKind.Paused ? Single().ChoosingSide : TeamSide.None;

        /// <inheritdoc cref="ItemId"/>
        public IReadOnlyList<SystemTransitionOptionId> Options =>
            Kind == SystemTransitionStepResolutionKind.Paused
                ? Single().Options
                : Array.Empty<SystemTransitionOptionId>();

        private SystemTransitionOpenItem Single()
        {
            if (OpenItems.Count > 1)
            {
                throw new InvalidOperationException(
                    $"A step resolution paused on {OpenItems.Count} items at once has no single item, " +
                    "chooser or option list. Read OpenItems.");
            }

            return OpenItems[0];
        }

        public static SystemTransitionStepResolution Completed { get; } =
            new SystemTransitionStepResolution(
                SystemTransitionStepResolutionKind.Completed,
                SystemTransitionItemId.None,
                Array.Empty<SystemTransitionOpenItem>());

        public static SystemTransitionStepResolution EndedTransition { get; } =
            new SystemTransitionStepResolution(
                SystemTransitionStepResolutionKind.EndedTransition,
                SystemTransitionItemId.None,
                Array.Empty<SystemTransitionOpenItem>());

        public static SystemTransitionStepResolution ItemCompleted(SystemTransitionItemId itemId)
        {
            if (!itemId.IsDefined)
            {
                throw new ArgumentException(
                    "An item that finished is named, or the step could not tell it had.", nameof(itemId));
            }

            return new SystemTransitionStepResolution(
                SystemTransitionStepResolutionKind.ItemCompleted,
                itemId,
                Array.Empty<SystemTransitionOpenItem>());
        }

        public static SystemTransitionStepResolution Paused(
            SystemTransitionItemId itemId,
            TeamSide choosingSide,
            IEnumerable<SystemTransitionOptionId> options)
        {
            if (!itemId.IsDefined)
            {
                throw new ArgumentException(
                    "A derived pause is about one item of its step; the item is what a later answer " +
                    "has to match.",
                    nameof(itemId));
            }

            return PausedOn(new[]
            {
                new SystemTransitionOpenItem(
                    itemId, choosingSide, options ?? throw new ArgumentNullException(nameof(options)))
            });
        }

        /// <summary>
        /// Milestone 8.35u. A step that stops on several choosers at once - rulebook p.15's leveling,
        /// which asks every hero that bought a level the same question at the same time.
        ///
        /// It is opt-in per step and deliberately not the default: milestone 6.11's blocked respawn
        /// stays one Place at a time, because a placement changes the board the next placement reads
        /// and those items are genuinely sequential. Leveling's are not - a hero's cards, coins and
        /// Level are that hero's own - which is the whole reason this shape is honest there.
        /// </summary>
        public static SystemTransitionStepResolution PausedOn(
            IEnumerable<SystemTransitionOpenItem> openItems)
        {
            var openArray = (openItems ?? throw new ArgumentNullException(nameof(openItems))).ToArray();
            if (openArray.Length == 0 || openArray.Any(item => item == null))
            {
                throw new ArgumentException(
                    "A pause names at least one open item; a step waiting on nothing has completed.",
                    nameof(openItems));
            }

            if (openArray.Select(item => item.ItemId).Distinct().Count() != openArray.Length)
            {
                throw new ArgumentException(
                    "A pause never opens the same item twice.", nameof(openItems));
            }

            return new SystemTransitionStepResolution(
                SystemTransitionStepResolutionKind.Paused,
                SystemTransitionItemId.None,
                Array.AsReadOnly(openArray));
        }
    }

    /// <summary>Where the transition is, handed to the step rules so they can run the step from there.</summary>
    public sealed class SystemTransitionStepContext
    {
        public SystemTransitionStepContext(
            SystemTransitionInstanceId instanceId,
            SystemTransitionDefinition definition,
            SystemTransitionStepDefinition step,
            IEnumerable<SystemTransitionStepId> completedStepIds,
            IEnumerable<SystemTransitionItemId> completedItemIds)
        {
            InstanceId = instanceId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Step = step ?? throw new ArgumentNullException(nameof(step));
            CompletedStepIds = Array.AsReadOnly(
                (completedStepIds ?? throw new ArgumentNullException(nameof(completedStepIds))).ToArray());
            CompletedItemIds = Array.AsReadOnly(
                (completedItemIds ?? throw new ArgumentNullException(nameof(completedItemIds))).ToArray());
        }

        public SystemTransitionInstanceId InstanceId { get; }
        public SystemTransitionDefinition Definition { get; }
        public SystemTransitionStepDefinition Step { get; }
        public IReadOnlyList<SystemTransitionStepId> CompletedStepIds { get; }

        /// <summary>The items of this step already answered, which is how the rules know what is left.</summary>
        public IReadOnlyList<SystemTransitionItemId> CompletedItemIds { get; }
    }

    /// <summary>One accepted answer, handed to the step rules so they can apply what it chose.</summary>
    public sealed class SystemTransitionAnswerContext
    {
        public SystemTransitionAnswerContext(
            SystemTransitionInstanceId instanceId,
            SystemTransitionDefinition definition,
            SystemTransitionStepDefinition step,
            SystemTransitionItemId itemId,
            TeamSide choosingSide,
            SystemTransitionOptionId optionId)
        {
            InstanceId = instanceId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Step = step ?? throw new ArgumentNullException(nameof(step));
            ItemId = itemId;
            ChoosingSide = choosingSide;
            OptionId = optionId;
        }

        public SystemTransitionInstanceId InstanceId { get; }
        public SystemTransitionDefinition Definition { get; }
        public SystemTransitionStepDefinition Step { get; }
        public SystemTransitionItemId ItemId { get; }
        public TeamSide ChoosingSide { get; }
        public SystemTransitionOptionId OptionId { get; }
    }

    /// <summary>
    /// What the steps of a transition actually do (milestone 6.11).
    ///
    /// This is the widening milestone 6.10a reserved, and its shape is the whole point. The rules own
    /// <em>what</em> a step does - flip a counter, move a Zone, derive who may place which minion where
    /// - and the authority owns <em>where</em> the transition is. Neither half can be written without
    /// the other and neither may keep the other's state, which is why this is a collaborator the
    /// authority consults rather than a callback the authority stores: a delegate could close over a
    /// cursor, and 6.10a's conformance rule exists because that is the failure nobody sees at the call
    /// site.
    ///
    /// An implementation therefore holds board and participant references and no step state at all.
    /// </summary>
    public interface ISystemTransitionStepRules
    {
        /// <summary>
        /// Runs the step from where <paramref name="context"/> says the transition is, and reports what
        /// happened. A self-resolving step may only complete or end the transition; a derived step may
        /// also stop.
        /// </summary>
        SystemTransitionStepResolution ResolveStep(SystemTransitionStepContext context);

        /// <summary>
        /// Applies one answer the authority has already validated as legal for the paused step.
        ///
        /// It returns nothing, and that is deliberate: nothing else in the match can run while a
        /// transition is paused, so the board the options were derived from is the board they are
        /// applied to, and an option that could not be applied is an engine defect rather than a rules
        /// outcome. Implementations throw in that case, as the validated Battle-Zone advance does.
        /// </summary>
        void ApplyAnswer(SystemTransitionAnswerContext context);
    }

    /// <summary>
    /// The step rules of a build whose transitions declare everything in the catalog - milestone
    /// 6.10a's behaviour, unchanged and named.
    ///
    /// A self-resolving step completes, because a synthetic step that declares no choice also does no
    /// work; an answered step applies nothing, because the answer was the whole of it. A derived step
    /// is refused rather than guessed at: a build that registers a definition with one owes the rules
    /// that run it.
    /// </summary>
    public sealed class DeclaredSystemTransitionStepRules : ISystemTransitionStepRules
    {
        public static DeclaredSystemTransitionStepRules Instance { get; } =
            new DeclaredSystemTransitionStepRules();

        public SystemTransitionStepResolution ResolveStep(SystemTransitionStepContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.Step.ChoiceSource == SystemTransitionStepChoiceSource.Derived)
            {
                throw new InvalidOperationException(
                    $"Transition '{context.Definition.Id}' step '{context.Step.Id}' derives its " +
                    "chooser, and this build composed no rules that can derive one.");
            }

            return SystemTransitionStepResolution.Completed;
        }

        public void ApplyAnswer(SystemTransitionAnswerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
        }
    }

    /// <summary>
    /// The step rules of a build that runs more than one kind of transition: each definition names its
    /// own, and anything unnamed falls back to the declared ones.
    ///
    /// It is a lookup, not a dispatcher with opinions - the same shape the component and capability
    /// catalogs already use - so a build's set of transitions and its set of rules stay one composition
    /// decision rather than a switch somebody has to remember to extend.
    /// </summary>
    public sealed class SystemTransitionStepRuleSet
        : ISystemTransitionStepRules, IDescribingConcurrentSystemTransitionStepRules
    {
        private readonly IReadOnlyDictionary<SystemTransitionDefinitionId, ISystemTransitionStepRules> rules;

        public SystemTransitionStepRuleSet(
            IEnumerable<KeyValuePair<SystemTransitionDefinitionId, ISystemTransitionStepRules>> rules)
        {
            var pairs = (rules ?? throw new ArgumentNullException(nameof(rules))).ToArray();
            if (pairs.Any(pair => !pair.Key.IsDefined || pair.Value == null))
            {
                throw new ArgumentException(
                    "Every named transition supplies defined rules.", nameof(rules));
            }

            this.rules = pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public SystemTransitionStepResolution ResolveStep(SystemTransitionStepContext context) =>
            For(context?.Definition.Id ?? throw new ArgumentNullException(nameof(context)))
                .ResolveStep(context);

        public void ApplyAnswer(SystemTransitionAnswerContext context) =>
            For(context?.Definition.Id ?? throw new ArgumentNullException(nameof(context)))
                .ApplyAnswer(context);

        /// <summary>
        /// Routes a description to the rules of the transition that is actually paused, and answers null
        /// when those rules cannot describe themselves. It is the same lookup that ran the step, so a
        /// pause is never described by rules that did not produce it.
        /// </summary>
        public SystemTransitionStepDescription Describe(PendingSystemTransitionStep pendingStep) =>
            pendingStep != null &&
            For(pendingStep.DefinitionId) is IDescribingSystemTransitionStepRules describing
                ? describing.Describe(pendingStep)
                : null;

        /// <summary>
        /// Milestone 8.35u. The same routing for one named open item.
        ///
        /// <para>It forwards only to rules that declare simultaneity, and answers null otherwise -
        /// which is not a fallback but the honest answer: a step whose rules describe only "the" pause
        /// has no per-item description to give, and this set must not invent one by describing the
        /// whole pause under one item's name.</para>
        /// </summary>
        public SystemTransitionStepDescription DescribeOpenItem(
            PendingSystemTransitionStep pendingStep, SystemTransitionItemId itemId) =>
            pendingStep != null &&
            For(pendingStep.DefinitionId) is IDescribingConcurrentSystemTransitionStepRules concurrent
                ? concurrent.DescribeOpenItem(pendingStep, itemId)
                : null;

        private ISystemTransitionStepRules For(SystemTransitionDefinitionId definitionId) =>
            rules.TryGetValue(definitionId, out var named)
                ? named
                : (ISystemTransitionStepRules)DeclaredSystemTransitionStepRules.Instance;
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
