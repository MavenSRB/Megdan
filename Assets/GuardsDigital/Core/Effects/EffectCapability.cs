using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// The four results a capability handler may return, verbatim from
    /// <c>CARD_EFFECT_CAPABILITIES.md</c>. They stay distinct because they mean different things to the
    /// enclosing control node: a legal rules no-op is <see cref="Applied"/>, not a failure, and a
    /// declined option is not an impossible one.
    /// </summary>
    public enum EffectExecutionResultKind
    {
        None = 0,

        /// <summary>Done, including a legal rules no-op.</summary>
        Applied = 1,

        /// <summary>Paused on an authoritative pending choice.</summary>
        NeedsChoice = 2,

        /// <summary>Cannot be done. Stops the enclosing sequence; prior applied nodes stay committed.</summary>
        ImpossibleMandatory = 3,

        /// <summary>Declined or skipped under its optionality policy; execution continues.</summary>
        DeclinedOptional = 4
    }

    /// <summary>
    /// What a capability accepts and produces. The definition validator type-checks every node binding
    /// against this before a match starts, so an incompatible definition is refused at content load
    /// rather than mid-resolution.
    /// </summary>
    public sealed class EffectCapabilityParameterSchema
    {
        public EffectCapabilityParameterSchema(
            IEnumerable<EffectCapabilityParameter> parameters = null,
            IEnumerable<EffectOutputBinding> outputs = null)
        {
            var declaredParameters = (parameters ?? Enumerable.Empty<EffectCapabilityParameter>()).ToArray();
            if (declaredParameters.Select(parameter => parameter.Name)
                .Distinct(StringComparer.Ordinal).Count() != declaredParameters.Length)
            {
                throw new ArgumentException("Capability parameter names must be unique.", nameof(parameters));
            }

            var declaredOutputs = (outputs ?? Enumerable.Empty<EffectOutputBinding>()).ToArray();
            if (declaredOutputs.Select(output => output.OutputName)
                .Distinct(StringComparer.Ordinal).Count() != declaredOutputs.Length)
            {
                throw new ArgumentException("Capability output names must be unique.", nameof(outputs));
            }

            Parameters = new ReadOnlyCollection<EffectCapabilityParameter>(declaredParameters);
            Outputs = new ReadOnlyCollection<EffectOutputBinding>(declaredOutputs);
        }

        public IReadOnlyList<EffectCapabilityParameter> Parameters { get; }
        public IReadOnlyList<EffectOutputBinding> Outputs { get; }
    }

    public readonly struct EffectCapabilityParameter
    {
        public EffectCapabilityParameter(string name, EffectValueKind valueKind, bool isRequired = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A capability parameter requires a name.", nameof(name));
            }

            if (valueKind == EffectValueKind.None)
            {
                throw new ArgumentException(
                    "A capability parameter requires a typed value kind.", nameof(valueKind));
            }

            Name = name;
            ValueKind = valueKind;
            IsRequired = isRequired;
        }

        public string Name { get; }
        public EffectValueKind ValueKind { get; }
        public bool IsRequired { get; }
    }

    /// <summary>
    /// Everything a handler is allowed to see. Immutable, and carries no Unity object, delegate, or
    /// continuation: the frame owns where execution is, the handler only decides what happens here.
    /// </summary>
    public sealed class EffectCapabilityContext
    {
        public EffectCapabilityContext(
            EffectNodeId nodeId,
            EffectFailurePolicy failurePolicy,
            HeroId actorId,
            HeroId controllerId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            IReadOnlyDictionary<string, EffectValue> inputs,
            int roundIndex,
            int turnIndex,
            MatchParticipants participants,
            ActiveEffectLifecycle timing)
        {
            if (!nodeId.IsDefined)
            {
                throw new ArgumentException("An effect capability context requires a node.", nameof(nodeId));
            }

            if (!actorId.IsDefined)
            {
                throw new ArgumentException("An effect capability context requires an actor.", nameof(actorId));
            }

            NodeId = nodeId;
            FailurePolicy = failurePolicy;
            ActorId = actorId;
            ControllerId = controllerId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            Inputs = inputs ?? new Dictionary<string, EffectValue>();
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Participants = participants;
            Timing = timing;
        }

        public EffectNodeId NodeId { get; }
        public EffectFailurePolicy FailurePolicy { get; }
        public HeroId ActorId { get; }
        public HeroId ControllerId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public IReadOnlyDictionary<string, EffectValue> Inputs { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public MatchParticipants Participants { get; }
        public ActiveEffectLifecycle Timing { get; }

        public EffectValue Input(string parameterName) =>
            Inputs.TryGetValue(parameterName, out var value) ? value : default;
    }

    /// <summary>
    /// One handler outcome: its kind, the typed facts it published, and the timing checkpoints it
    /// declares around itself. The checkpoints become resolver steps, so an effect raises timing the
    /// same way milestone 6.1ap made every other action raise it.
    /// </summary>
    public sealed class EffectExecutionResult
    {
        private EffectExecutionResult(
            EffectExecutionResultKind kind,
            IEnumerable<EffectOutcomeFact> outcomes,
            IEnumerable<TimingCheckpoint> checkpointsBefore,
            IEnumerable<TimingCheckpoint> checkpointsAfter,
            EffectChoiceRequest choice)
        {
            if (kind == EffectExecutionResultKind.None)
            {
                throw new ArgumentException("An effect execution result requires a kind.", nameof(kind));
            }

            var facts = (outcomes ?? Enumerable.Empty<EffectOutcomeFact>()).ToArray();
            if (facts.Select(fact => fact.OutputName)
                .Distinct(StringComparer.Ordinal).Count() != facts.Length)
            {
                throw new ArgumentException("Effect outcome names must be unique.", nameof(outcomes));
            }

            var before = (checkpointsBefore ?? Enumerable.Empty<TimingCheckpoint>()).ToArray();
            var after = (checkpointsAfter ?? Enumerable.Empty<TimingCheckpoint>()).ToArray();
            if (before.Concat(after).Any(checkpoint => checkpoint == TimingCheckpoint.None))
            {
                throw new ArgumentException("A declared timing checkpoint must be defined.");
            }

            if ((kind == EffectExecutionResultKind.NeedsChoice) != (choice != null))
            {
                throw new ArgumentException(
                    "A NeedsChoice result carries exactly one choice request, and no other kind carries one.");
            }

            Kind = kind;
            Outcomes = new ReadOnlyCollection<EffectOutcomeFact>(facts);
            CheckpointsBefore = new ReadOnlyCollection<TimingCheckpoint>(before);
            CheckpointsAfter = new ReadOnlyCollection<TimingCheckpoint>(after);
            Choice = choice;
        }

        public EffectExecutionResultKind Kind { get; }
        public IReadOnlyList<EffectOutcomeFact> Outcomes { get; }
        public IReadOnlyList<TimingCheckpoint> CheckpointsBefore { get; }
        public IReadOnlyList<TimingCheckpoint> CheckpointsAfter { get; }
        public EffectChoiceRequest Choice { get; }

        public static EffectExecutionResult Applied(
            IEnumerable<EffectOutcomeFact> outcomes = null,
            IEnumerable<TimingCheckpoint> checkpointsBefore = null,
            IEnumerable<TimingCheckpoint> checkpointsAfter = null) =>
            new EffectExecutionResult(
                EffectExecutionResultKind.Applied, outcomes, checkpointsBefore, checkpointsAfter, null);

        public static EffectExecutionResult NeedsChoice(EffectChoiceRequest choice) =>
            new EffectExecutionResult(
                EffectExecutionResultKind.NeedsChoice, null, null, null,
                choice ?? throw new ArgumentNullException(nameof(choice)));

        public static EffectExecutionResult ImpossibleMandatory() =>
            new EffectExecutionResult(
                EffectExecutionResultKind.ImpossibleMandatory, null, null, null, null);

        public static EffectExecutionResult DeclinedOptional() =>
            new EffectExecutionResult(
                EffectExecutionResultKind.DeclinedOptional, null, null, null, null);
    }

    /// <summary>
    /// A handler's request for input: who decides, and the ordered candidate values they choose among.
    /// The frame turns this into the authoritative pending choice; the handler never holds it.
    /// </summary>
    public sealed class EffectChoiceRequest
    {
        public EffectChoiceRequest(
            EffectChooserRole chooser,
            IEnumerable<EffectValue> candidates,
            int minimumSelections,
            int maximumSelections)
        {
            if (chooser == EffectChooserRole.None)
            {
                throw new ArgumentException("An effect choice requires a chooser.", nameof(chooser));
            }

            var options = (candidates ?? throw new ArgumentNullException(nameof(candidates))).ToArray();
            if (options.Any(candidate => !candidate.IsDefined))
            {
                throw new ArgumentException("Effect choice candidates must be defined.", nameof(candidates));
            }

            if (options.Distinct().Count() != options.Length)
            {
                throw new ArgumentException("Effect choice candidates must be unique.", nameof(candidates));
            }

            if (minimumSelections < 0 || maximumSelections < minimumSelections)
            {
                throw new ArgumentException("Effect choice bounds must be a non-negative range.");
            }

            if (maximumSelections > options.Length)
            {
                throw new ArgumentException("An effect choice cannot allow more selections than candidates.");
            }

            Chooser = chooser;
            Candidates = new ReadOnlyCollection<EffectValue>(options);
            MinimumSelections = minimumSelections;
            MaximumSelections = maximumSelections;
        }

        public EffectChooserRole Chooser { get; }
        public IReadOnlyList<EffectValue> Candidates { get; }
        public int MinimumSelections { get; }
        public int MaximumSelections { get; }
    }

    /// <summary>
    /// A pure rules executor for one capability revision. Stateless: everything it needs arrives in the
    /// context, and everything it decides leaves in the result. <see cref="Resume"/> is separate from
    /// <see cref="Execute"/> so the frame, not the handler, owns whether this is a resumption.
    /// </summary>
    public interface IEffectCapabilityHandler
    {
        EffectCapabilityReference Reference { get; }

        EffectCapabilityParameterSchema Schema { get; }

        /// <summary>
        /// Every Active-effect definition this capability can create. These are the trusted semantics a
        /// restore rebinds an instance to, so a snapshot never has to carry them.
        /// </summary>
        IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; }

        EffectExecutionResult Execute(EffectCapabilityContext context);

        EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected);
    }
}
