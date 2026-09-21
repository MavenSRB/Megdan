using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// The control structures milestone 6.2a implements, plus the capability leaf they compose and
    /// milestone 6.7's <c>RepeatAction</c>. <c>PerformAction</c>, <c>ReplaceAction</c>, and
    /// <c>SimultaneousBatch</c> are not implemented here.
    /// </summary>
    public enum EffectNodeKind
    {
        None = 0,

        /// <summary>Ordered children in exact printed order.</summary>
        Sequence = 1,

        /// <summary>One child governed by an optionality policy.</summary>
        Optional = 2,

        /// <summary>A predicate capability selecting between a then-child and an optional else-child.</summary>
        Conditional = 3,

        /// <summary>A chooser-owned selection among the children.</summary>
        Choose = 4,

        /// <summary>A leaf that invokes one registered capability handler.</summary>
        Capability = 5,

        /// <summary>
        /// Milestone 6.7. A leaf that asks for this action to happen again as a separate instance.
        ///
        /// It performs nothing itself and invokes no capability: it records the request on the frame,
        /// and the card-action dispatcher creates the second instance once this one's action boundary -
        /// including milestone 6.4a's after-action displaced-minion correction - is complete. That is
        /// why repetition is a node kind rather than a loop in the node table, which
        /// <see cref="EffectDefinitionValidator"/> refuses.
        /// </summary>
        RepeatAction = 6
    }

    /// <summary>
    /// How a node behaves when its work cannot be done. These stay four distinct behaviors rather than
    /// one failure path, because the rulebook distinguishes them: `may` and `up to` open a decision,
    /// `if able` does not, and a mandatory step that cannot happen stops the rest of the action.
    /// </summary>
    public enum EffectFailurePolicy
    {
        None = 0,

        /// <summary>Opens no choice. Impossible stops the enclosing sequence, keeping prior work.</summary>
        Mandatory = 1,

        /// <summary>`may`. Always opens a confirm-or-decline choice; declining continues the action.</summary>
        May = 2,

        /// <summary>`up to`. The count is the choice; selecting fewer is a legal result, not a failure.</summary>
        UpTo = 3,

        /// <summary>`if able`. Opens no choice; impossible is skipped silently and the action continues.</summary>
        IfAble = 4
    }

    /// <summary>
    /// One node in an effect definition's flat node table.
    ///
    /// The table is flat with explicit child-ID lists rather than a nested tree. Node IDs are the stable
    /// identity a paused resolution serializes, a flat table writes as a count-prefixed array with no
    /// recursion, the continuation is a directly serializable stack of node/child positions, and
    /// reachability and acyclicity become mechanical checks the validator can run at content load.
    /// </summary>
    public sealed class EffectNodeDefinition
    {
        public EffectNodeDefinition(
            EffectNodeId id,
            EffectNodeKind kind,
            EffectFailurePolicy failurePolicy,
            EffectCapabilityReference capability = default,
            IEnumerable<EffectNodeId> childNodeIds = null,
            IEnumerable<EffectInputBinding> inputs = null,
            IEnumerable<EffectOutputBinding> outputs = null,
            EffectChooserRole chooser = EffectChooserRole.None,
            int minimumSelections = 0,
            int maximumSelections = 0,
            int maximumRepeats = 1,
            RepeatTargetBinding repeatTargetBinding = RepeatTargetBinding.Unbound,
            EffectNodeId boundSelectionNodeId = default,
            IEnumerable<ImmunityOverrideDeclaration> immunityOverrides = null)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("An effect node requires a stable ID.", nameof(id));
            }

            if (kind == EffectNodeKind.None)
            {
                throw new ArgumentException("An effect node requires a kind.", nameof(kind));
            }

            if (failurePolicy == EffectFailurePolicy.None)
            {
                throw new ArgumentException(
                    "An effect node requires an explicit failure policy.", nameof(failurePolicy));
            }

            var children = (childNodeIds ?? Enumerable.Empty<EffectNodeId>()).ToArray();
            if (children.Any(child => !child.IsDefined))
            {
                throw new ArgumentException("Effect node children must be defined.", nameof(childNodeIds));
            }

            if (children.Distinct().Count() != children.Length)
            {
                throw new ArgumentException("Effect node children must be unique.", nameof(childNodeIds));
            }

            var boundInputs = (inputs ?? Enumerable.Empty<EffectInputBinding>()).ToArray();
            if (boundInputs.Select(input => input.ParameterName)
                .Distinct(StringComparer.Ordinal).Count() != boundInputs.Length)
            {
                throw new ArgumentException("Effect node input parameters must be unique.", nameof(inputs));
            }

            var boundOutputs = (outputs ?? Enumerable.Empty<EffectOutputBinding>()).ToArray();
            if (boundOutputs.Select(output => output.OutputName)
                .Distinct(StringComparer.Ordinal).Count() != boundOutputs.Length)
            {
                throw new ArgumentException("Effect node output names must be unique.", nameof(outputs));
            }

            var declaredOverrides = (immunityOverrides ??
                Enumerable.Empty<ImmunityOverrideDeclaration>()).ToArray();
            if (declaredOverrides.Any(value => !value.IsDefined) ||
                declaredOverrides.Distinct().Count() != declaredOverrides.Length)
            {
                throw new ArgumentException(
                    "Effect-node immunity overrides must be defined and distinct.",
                    nameof(immunityOverrides));
            }

            Id = id;
            Kind = kind;
            FailurePolicy = failurePolicy;
            Capability = capability;
            ChildNodeIds = new ReadOnlyCollection<EffectNodeId>(children);
            Inputs = new ReadOnlyCollection<EffectInputBinding>(boundInputs);
            Outputs = new ReadOnlyCollection<EffectOutputBinding>(boundOutputs);
            Chooser = chooser;
            MinimumSelections = minimumSelections;
            MaximumSelections = maximumSelections;
            MaximumRepeats = maximumRepeats;
            RepeatTargetBinding = repeatTargetBinding;
            BoundSelectionNodeId = boundSelectionNodeId;
            ImmunityOverrides = new ReadOnlyCollection<ImmunityOverrideDeclaration>(declaredOverrides);

            ValidateNodeShape();
        }

        /// <summary>
        /// Milestone 8.35z33. The input a <see cref="EffectChooserRole.BoundHero"/> node names its
        /// chooser through, spelled once so a definition cannot mistype it and so the validator and
        /// the resolver read the same string.
        /// </summary>
        public const string ChooserParameterName = "chooser";

        public EffectNodeId Id { get; }
        public EffectNodeKind Kind { get; }
        public EffectFailurePolicy FailurePolicy { get; }
        public EffectCapabilityReference Capability { get; }
        public IReadOnlyList<EffectNodeId> ChildNodeIds { get; }
        public IReadOnlyList<EffectInputBinding> Inputs { get; }
        public IReadOnlyList<EffectOutputBinding> Outputs { get; }
        public EffectChooserRole Chooser { get; }
        public int MinimumSelections { get; }
        public int MaximumSelections { get; }

        /// <summary>
        /// How many times a <see cref="EffectNodeKind.RepeatAction"/> node may ask, counted across the
        /// whole chain of instances rather than per instance. One for every repeat built before
        /// milestone 8.6, which is exactly what 6.7's structural bound already meant, and two for
        /// Center of Mass's "may repeat up to two times" - the first verified count above one.
        ///
        /// It is a maximum and never a number of repeats to perform: each instance still reaches the
        /// node's own `may` or `if able` and may stop the chain there, which is what "up to" means on
        /// p.10.
        /// </summary>
        public int MaximumRepeats { get; }

        /// <summary>
        /// What a repeated instance does about the target the instance before it chose. Milestone 8.6.
        /// </summary>
        public RepeatTargetBinding RepeatTargetBinding { get; }

        /// <summary>
        /// The selection node whose published target a <see cref="RepeatTargetBinding.SameTarget"/>
        /// repeat binds. Undefined for an unbound repeat and for every other node kind.
        /// </summary>
        public EffectNodeId BoundSelectionNodeId { get; }

        /// <summary>
        /// Exact trusted immunity exceptions this capability may consume. They are definition semantics,
        /// so a snapshot carries none and restore rebinds them from this node.
        /// </summary>
        public IReadOnlyList<ImmunityOverrideDeclaration> ImmunityOverrides { get; }

        /// <summary>
        /// Per-kind legality that depends only on this node. Cross-node concerns - unresolved children,
        /// cycles, unreachable nodes, binding types, unregistered capabilities - belong to
        /// <see cref="EffectDefinitionValidator"/>, which can see the whole table.
        /// </summary>
        private void ValidateNodeShape()
        {
            // Milestone 8.6. The three repeat declarations belong to one node kind, so every other kind
            // is refused them here rather than ignoring them - a Sequence carrying a bound selection
            // would be stating a rule nothing reads.
            if (Kind != EffectNodeKind.RepeatAction &&
                (MaximumRepeats != 1 || RepeatTargetBinding != RepeatTargetBinding.Unbound ||
                    BoundSelectionNodeId.IsDefined))
            {
                throw new ArgumentException(
                    $"A {Kind} node is not a repeat, so it declares no repeat maximum and no target binding.");
            }

            if (ImmunityOverrides.Count > 0 &&
                Kind != EffectNodeKind.Capability && Kind != EffectNodeKind.Conditional)
            {
                throw new ArgumentException(
                    "Only a node that invokes a capability declares an immunity override.");
            }

            switch (Kind)
            {
                case EffectNodeKind.Sequence:
                    RequireNoCapability();
                    if (ChildNodeIds.Count == 0)
                        throw new ArgumentException("A Sequence node requires at least one child.");
                    if (FailurePolicy != EffectFailurePolicy.Mandatory)
                        throw new ArgumentException(
                            "A Sequence node is mandatory; optionality belongs to the children it orders.");
                    RequireNoChooser();
                    RequireNoSelectionBounds();
                    break;

                case EffectNodeKind.Optional:
                    RequireNoCapability();
                    if (ChildNodeIds.Count != 1)
                        throw new ArgumentException("An Optional node governs exactly one child.");
                    if (FailurePolicy == EffectFailurePolicy.Mandatory)
                        throw new ArgumentException(
                            "An Optional node cannot be mandatory; use the child directly instead.");
                    if (Chooser == EffectChooserRole.None && FailurePolicy != EffectFailurePolicy.IfAble)
                        throw new ArgumentException(
                            "A may or up-to Optional node requires a chooser, because it opens a decision.");
                    if (FailurePolicy == EffectFailurePolicy.IfAble && Chooser != EffectChooserRole.None)
                        throw new ArgumentException(
                            "An if-able Optional node opens no decision, so it must name no chooser.");
                    break;

                case EffectNodeKind.Conditional:
                    if (!Capability.IsDefined)
                        throw new ArgumentException("A Conditional node requires a predicate capability.");
                    if (ChildNodeIds.Count < 1 || ChildNodeIds.Count > 2)
                        throw new ArgumentException(
                            "A Conditional node has a then-child and an optional else-child.");
                    if (FailurePolicy != EffectFailurePolicy.Mandatory)
                        throw new ArgumentException(
                            "A Conditional node is mandatory; its branches carry their own policies.");
                    RequireNoChooser();
                    RequireNoSelectionBounds();
                    break;

                case EffectNodeKind.Choose:
                    RequireNoCapability();
                    if (ChildNodeIds.Count < 2)
                        throw new ArgumentException("A Choose node requires at least two options.");
                    if (Chooser == EffectChooserRole.None)
                        throw new ArgumentException("A Choose node requires a chooser.");

                    // Milestone 8.35z33. A modal whose chooser is a hero this text named has to say
                    // which node named them, or it is claiming a decision belongs to somebody it
                    // cannot identify - and the resolver would have nowhere to send the question.
                    if (Chooser == EffectChooserRole.BoundHero &&
                        !Inputs.Any(input =>
                            string.Equals(input.ParameterName, ChooserParameterName, StringComparison.Ordinal) &&
                            input.ValueKind == EffectValueKind.Hero))
                        throw new ArgumentException(
                            $"A Choose node whose chooser is a bound hero binds them as a Hero input " +
                            $"named '{ChooserParameterName}'.");
                    if (MinimumSelections < 0 || MaximumSelections < MinimumSelections)
                        throw new ArgumentException("Choose selection bounds must be a non-negative range.");
                    if (MaximumSelections > ChildNodeIds.Count)
                        throw new ArgumentException(
                            "A Choose node cannot allow more selections than it has options.");
                    if (MinimumSelections == 0 && FailurePolicy == EffectFailurePolicy.Mandatory)
                        throw new ArgumentException(
                            "A mandatory Choose node requires at least one selection; a zero minimum is up-to.");
                    break;

                case EffectNodeKind.Capability:
                    if (!Capability.IsDefined)
                        throw new ArgumentException("A Capability node requires a capability reference.");
                    if (ChildNodeIds.Count != 0)
                        throw new ArgumentException("A Capability node is a leaf and takes no children.");
                    RequireNoChooser();
                    RequireNoSelectionBounds();
                    break;

                case EffectNodeKind.RepeatAction:
                    RequireNoCapability();
                    if (ChildNodeIds.Count != 0)
                        throw new ArgumentException("A RepeatAction node is a leaf and takes no children.");
                    if (Inputs.Count != 0 || Outputs.Count != 0)
                        throw new ArgumentException(
                            "A RepeatAction node reads and publishes nothing; the repeated instance " +
                            "runs the same definition from its own start.");

                    // A repeat that must happen is unbounded by construction. Admitting `may` and `if
                    // able` only is what makes the bound structural rather than a count to trust: a
                    // Repeat that has already run its declared maximum is ineligible, and an ineligible
                    // non-mandatory step is p.10's silent skip rather than a stop, so nothing is
                    // exposed and nothing halts. Milestone 6.7 stated the maximum as the constant one;
                    // milestone 8.6 let a card declare it, and changed nothing else about this.
                    if (FailurePolicy != EffectFailurePolicy.May &&
                        FailurePolicy != EffectFailurePolicy.IfAble)
                        throw new ArgumentException(
                            "A RepeatAction node is `may` or `if able`; a mandatory repeat is unbounded.");
                    if (MaximumRepeats < 1)
                        throw new ArgumentException(
                            "A RepeatAction node asks at least once; a node that may never ask is the " +
                            "node not being there.");
                    if (RepeatTargetBinding == RepeatTargetBinding.SameTarget &&
                        !BoundSelectionNodeId.IsDefined)
                        throw new ArgumentException(
                            "A same-target repeat names the selection node whose target it binds.");
                    // Milestone 8.39d1. The one Different arm that is about a SELECTION rather than
                    // about the action's own target names its node for the same reason the same-target
                    // arm does: the piece it excludes is that node's published subject.
                    if (RepeatTargetBinding == RepeatTargetBinding.DifferentSelectionSubject &&
                        !BoundSelectionNodeId.IsDefined)
                        throw new ArgumentException(
                            "A different-selection-subject repeat names the selection node whose " +
                            "subject it excludes.");
                    if (RepeatTargetBinding == RepeatTargetBinding.Unbound &&
                        BoundSelectionNodeId.IsDefined)
                        throw new ArgumentException(
                            "An unbound repeat binds no selection, so it must name none.");
                    // Milestone 8.10. A different-target repeat names no selection either: what it
                    // excludes is the target the action itself took, which is the action's own state
                    // and not a node's published output.
                    // Milestone 8.24. A repeat narrowed to a different enemy hero excludes the same
                    // thing and narrows what is left, so it names no selection node either.
                    // Milestone 8.35z46. A repeat narrowed to a minion excludes the same
                    // thing and narrows what is left, so it names no selection node either.
                    // Milestone 8.69b. A repeat narrowed to a different unit adjacent to the actor
                    // excludes the same thing and narrows how far what is left may stand, so it names
                    // no selection node either.
                    if ((RepeatTargetBinding == RepeatTargetBinding.DifferentTarget ||
                            RepeatTargetBinding == RepeatTargetBinding.DifferentEnemyHero ||
                            RepeatTargetBinding == RepeatTargetBinding.DifferentEnemyMinion ||
                            RepeatTargetBinding ==
                                RepeatTargetBinding.DifferentUnitAdjacentToActor) &&
                        BoundSelectionNodeId.IsDefined)
                        throw new ArgumentException(
                            "A different-target repeat excludes the action's own target, so it names " +
                            "no selection node.");
                    if (BoundSelectionNodeId.Equals(Id))
                        throw new ArgumentException(
                            "A repeat cannot bind its own target; the selection it binds is another node.");
                    if (FailurePolicy == EffectFailurePolicy.May && Chooser == EffectChooserRole.None)
                        throw new ArgumentException(
                            "A may RepeatAction node requires a chooser, because it opens a decision.");
                    if (FailurePolicy == EffectFailurePolicy.IfAble && Chooser != EffectChooserRole.None)
                        throw new ArgumentException(
                            "An if-able RepeatAction node opens no decision, so it must name no chooser.");
                    RequireNoSelectionBounds();
                    break;
            }
        }

        private void RequireNoCapability()
        {
            if (Capability.IsDefined)
            {
                throw new ArgumentException(
                    $"A {Kind} node composes other nodes and invokes no capability of its own.");
            }
        }

        private void RequireNoChooser()
        {
            if (Chooser != EffectChooserRole.None)
            {
                throw new ArgumentException($"A {Kind} node opens no decision, so it must name no chooser.");
            }
        }

        private void RequireNoSelectionBounds()
        {
            if (MinimumSelections != 0 || MaximumSelections != 0)
            {
                throw new ArgumentException($"A {Kind} node has no selection bounds.");
            }
        }
    }
}
