using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
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
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IReadOnlyDictionary<string, EffectValue> inputs,
            int roundIndex,
            int turnIndex,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            EffectCardState cardState,
            MatchComponents components = null,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null)
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
            SourceDefinitionReference = sourceDefinitionReference;
            ActionDefinitionId = actionDefinitionId;
            Inputs = inputs ?? new Dictionary<string, EffectValue>();
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Participants = participants;
            Timing = timing;
            Modifications = modifications ?? throw new ArgumentNullException(nameof(modifications));
            CardState = cardState ?? throw new ArgumentNullException(nameof(cardState));
            Components = components;
            HeroItems = heroItems;
        }

        public EffectNodeId NodeId { get; }
        public EffectFailurePolicy FailurePolicy { get; }
        public HeroId ActorId { get; }
        public HeroId ControllerId { get; }
        public CardInstanceId SourceCardInstanceId { get; }

        /// <summary>
        /// The definition revision this resolution's source card was played at, recorded by the frame
        /// at start. Milestone 7.2b added it because a capability that reads its own card's printed
        /// icon has to name the content it is reading, and the frame's own reference is what the
        /// resolution began from - re-deriving one from the instance list would let a capability read a
        /// revision the resolution never started under.
        /// </summary>
        public CardDefinitionReference SourceDefinitionReference { get; }

        public ActionDefinitionId ActionDefinitionId { get; }
        public IReadOnlyDictionary<string, EffectValue> Inputs { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public MatchParticipants Participants { get; }
        public ActiveEffectLifecycle Timing { get; }

        /// <summary>
        /// The modifications in effect. Required since milestone 6.4: a handler that displaces
        /// something consults p.18's immunity and the Move-Through permission, so a capability is a
        /// rules-consuming seam like any other and takes the live set rather than an optional one.
        /// <see cref="RuleModificationSet.Empty"/> is the named absence.
        /// </summary>
        public RuleModificationSet Modifications { get; }

        /// <summary>
        /// The card state this handler may read. Required since milestone 7.1a: a capability that
        /// derives candidates from a hand needs the instances, and needs them identically at every
        /// point it can be asked - Skill start, repeated-Skill start, the option-feasibility walk, the
        /// choice-answer path, and snapshot restore's candidate re-derivation.
        ///
        /// <see cref="EffectCardState.Absent"/> is the named absence and throws when read, so a
        /// capability handed no card state fails loudly instead of reporting the empty candidate set
        /// that an ordinary, legal "the chooser holds nothing eligible" also produces. Those two mean
        /// opposite things to the enclosing node, and nothing may quietly turn the first into the
        /// second.
        /// </summary>
        public EffectCardState CardState { get; }

        /// <summary>
        /// Milestone 6.2's token and marker collaborators, or <c>null</c> outside a live match. Like
        /// <see cref="Timing"/>, this is authoritative state the handler may mutate through a named
        /// operation; unlike a Unity object or a delegate, none of it is a continuation, so the frame
        /// still owns where execution is.
        /// </summary>
        public MatchComponents Components { get; }

        /// <summary>
        /// Milestone 7.2b: the match's items, or <c>null</c> in a composition that holds none - the
        /// same absence <see cref="Components"/> expresses. A capability that reads a printed value
        /// composes rulebook p.16's Item Bonus onto it through the one query every other total uses, so
        /// the same state the Defense, Attack, Movement, Range and Initiative totals read reaches the
        /// effect frame too. It is the state the round owns rather than a copy: a Radius derived from
        /// items other than the ones the match records would be a disk no restore or replay could
        /// reproduce, which is the same reason the marker register and the item state are bound rather
        /// than constructed at every seam that needs them.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.HeroItemState HeroItems { get; }

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
            int maximumSelections,
            HeroId chooserHeroId = default)
        {
            if (chooser == EffectChooserRole.None && !chooserHeroId.IsDefined)
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
            ChooserHeroId = chooserHeroId;
            Candidates = new ReadOnlyCollection<EffectValue>(options);
            MinimumSelections = minimumSelections;
            MaximumSelections = maximumSelections;
        }

        public EffectChooserRole Chooser { get; }

        /// <summary>
        /// The exact hero whose decision this is, when the handler already knows. Milestone 7.1a: a
        /// capability told which hero must discard has that hero in a typed input, so naming them is
        /// reporting a fact rather than re-deriving one. It takes precedence over
        /// <see cref="Chooser"/>, which stays the definition-level way to say the same thing when the
        /// node rather than the handler decides.
        ///
        /// This is what keeps "another hero discards" out of the resolution-scoped opposing-hero seam,
        /// which cannot express it: with three opponents on a side there is no single opposing hero,
        /// and the handler's own input is the only place the answer actually exists.
        /// </summary>
        public HeroId ChooserHeroId { get; }
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

        /// <summary>
        /// Which of the four results <see cref="Execute"/> would report in this context, answered
        /// without performing anything.
        ///
        /// Milestone 6.5ac asks this so a control node can refuse an option the actor cannot perform
        /// instead of spending a card on it. Probing by calling <see cref="Execute"/> is exactly what
        /// this member exists to avoid: a handler may apply rather than pause - marking an actor,
        /// creating an Active effect, returning a marker, pushing every adjacent object at a fixed
        /// distance - so asking that way would perform the effect it was asked about.
        ///
        /// The answer is the candidate derivation the handler already owns, without the commit that
        /// follows it. It reports the kind alone rather than an <see cref="EffectExecutionResult"/>,
        /// because a result carries published facts and declared checkpoints, and a probe has neither:
        /// it is a reading of the state, not a step in the resolution. Nothing here may mutate board,
        /// card, effect, component, supply, timing, or identity state, and nothing may mint.
        /// </summary>
        EffectExecutionResultKind Anticipate(EffectCapabilityContext context);
    }

    /// <summary>
    /// Milestone 6.8l: what a capability must add to accept a consumer.
    ///
    /// A selection that declares <see cref="EffectSelectionDeclaration.ConsumerParameterName"/> is asked
    /// to offer only the objects that consumer can act on, and both the feasibility walk and the live
    /// pause therefore need the set it <em>would</em> offer. <see cref="IEffectCapabilityHandler.Anticipate"/>
    /// cannot supply it: it answers with a kind, deliberately, because a result carries facts and
    /// checkpoints a probe has neither of. Running <see cref="IEffectCapabilityHandler.Execute"/> to
    /// find out is what milestone 6.5ac's whole contract exists to avoid.
    ///
    /// So the candidate derivation the handler already owns is declared separately, under the same rule
    /// the probe obeys: nothing here may mutate board, card, effect, component, supply, timing, or
    /// identity state, and nothing may mint. The catalog refuses to register a handler that declares the
    /// parameter without implementing this, so a selection can never accept a consumer it has no way to
    /// answer for.
    /// </summary>
    public interface IEffectSelectionCapabilityHandler
    {
        /// <summary>
        /// The candidate values <see cref="IEffectCapabilityHandler.Execute"/> would offer here, derived
        /// without opening the choice. Empty means the selection has nothing to name, which is the same
        /// state its probe reports as <see cref="EffectExecutionResultKind.ImpossibleMandatory"/>.
        /// </summary>
        IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context);
    }
}
