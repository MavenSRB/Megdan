using System;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2a. Definition validation happens once at content load, never during a match, so a
    /// malformed effect is refused before it can strand a resolution half-executed.
    ///
    /// Per-node shape is enforced by the node constructor and asserted here through it; the validator
    /// owns everything that needs the whole table.
    /// </summary>
    public sealed class EffectDefinitionValidatorTests
    {
        [Test]
        public void ValidDefinition_IsAccepted()
        {
            Assert.That(Validate(Sequence("root", "a", "b"), Leaf("a"), Leaf("b")), Is.Empty);
        }

        [Test]
        public void UnresolvedChild_IsRejected()
        {
            var rejections = Validate(Sequence("root", "a", "ghost"), Leaf("a"));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.UnresolvedChildNode));
        }

        [Test]
        public void UnreachableNode_IsRejected()
        {
            var rejections = Validate(Sequence("root", "a"), Leaf("a"), Leaf("orphan"));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.UnreachableNode),
                "A node nothing reaches is dead content, and dead content in a rules definition is a " +
                "sign the author meant to wire it somewhere.");
        }

        [Test]
        public void UnregisteredCapability_IsRejected()
        {
            var rejections = Validate(
                Sequence("root", "a"),
                new EffectNodeDefinition(
                    new EffectNodeId("a"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    new EffectCapabilityReference(new EffectCapabilityId("not-registered"), 1)));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.UnregisteredCapability));
        }

        [Test]
        public void BindingTypeMismatch_IsRejected()
        {
            var rejections = Validate(
                Sequence("root", "a"),
                new EffectNodeDefinition(
                    new EffectNodeId("a"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("flag", EffectValue.Integer(1))
                    }));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.IncompatibleBindingType));
        }

        [Test]
        public void MissingRequiredParameter_IsRejected()
        {
            var rejections = Validate(
                Sequence("root", "a"),
                new EffectNodeDefinition(
                    new EffectNodeId("a"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.MissingRequiredParameter));
        }

        /// <summary>
        /// The rule that makes typed result binding safe without an expression language: a node may
        /// only read an output from a node that has certainly already run on every path reaching it.
        /// </summary>
        [Test]
        public void NodeOutputReadFromASkippableBranch_IsRejected()
        {
            var rejections = Validate(
                new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("maybe"), new EffectNodeId("reader") }),
                new EffectNodeDefinition(
                    new EffectNodeId("maybe"), EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { new EffectNodeId("producer") },
                    chooser: EffectChooserRole.Actor),
                Producer("producer"),
                Reader("reader", "producer"));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.NodeOutputNotGuaranteed),
                "The producer sits behind a `may`, so it may never have run when the reader binds it.");
        }

        [Test]
        public void NodeOutputReadFromAnEarlierMandatorySequenceStep_IsAccepted()
        {
            var rejections = Validate(
                Sequence("root", "producer", "reader"),
                Producer("producer"),
                Reader("reader", "producer"));

            Assert.That(rejections, Is.Empty,
                "An earlier mandatory step in the same Sequence has certainly run, which is exactly " +
                "the case typed result binding exists for.");
        }

        /// <summary>
        /// Milestone 6.8k. The resolver reads roles from the execution frame alone, with no board
        /// access, so a role can supply a hero or the source card and nothing else. A binding that
        /// declares anything else would pass the parameter type-match and fault mid-resolution; the
        /// acceptance check requires the refusal at content load instead.
        /// </summary>
        [Test]
        public void ARoleBindingDeclaringACoordinate_IsRejectedAtContentLoad()
        {
            var rejections = Validate(
                Sequence("root", "a"),
                new EffectNodeDefinition(
                    new EffectNodeId("a"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticDisplacementCapabilities.DirectMoveV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            "mover", EffectRoleKind.Actor, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral("towardActor", EffectValue.Boolean(false))
                    }));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.RoleCannotSupplyValueKind),
                "No role can supply a coordinate: the frame holds no board reference.");
        }

        [Test]
        public void AChosenSpaceRoleBinding_IsRejectedBecauseNoFrameResolvesIt()
        {
            var rejections = Validate(
                Sequence("root", "a"),
                new EffectNodeDefinition(
                    new EffectNodeId("a"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticDisplacementCapabilities.DirectMoveV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            "mover", EffectRoleKind.ChosenSpace, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral("towardActor", EffectValue.Boolean(false))
                    }));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.RoleCannotSupplyValueKind),
                "ChosenSpace is declared vocabulary no frame resolves; a selection reaches a node " +
                "through a prior target-selection node's published output.");
        }

        [Test]
        public void ARoleBindingDeclaringWhatTheRoleSupplies_StaysAccepted()
        {
            var rejections = Validate(
                Sequence("root", "a"),
                new EffectNodeDefinition(
                    new EffectNodeId("a"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.AcceptHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole("hero", EffectRoleKind.Actor, EffectValueKind.Hero)
                    }));

            Assert.That(rejections, Is.Empty,
                "The vacuity guard: the rule refuses impossible bindings, not role bindings.");
        }

        [Test]
        public void ASubjectBoundFromASelectionNodesOutput_IsAcceptedExactlyWhenGuaranteed()
        {
            // 6.8k's positive shape: the selection publishes a coordinate and a later step in the same
            // Sequence binds it. The negative twin behind a `may` is NodeOutputReadFromASkippableBranch.
            var rejections = Validate(
                Sequence("root", "select", "move"),
                new EffectNodeDefinition(
                    new EffectNodeId("select"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.ChooseTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(true))
                    },
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    new EffectNodeId("move"), EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.DirectMoveV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", new EffectNodeId("select"), "chosen", EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral("towardActor", EffectValue.Boolean(false))
                    }));

            Assert.That(rejections, Is.Empty);
        }

        [Test]
        public void ConditionalWithoutABooleanPredicateOutput_IsRejected()
        {
            var rejections = Validate(
                new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.NoteActorV1,
                    childNodeIds: new[] { new EffectNodeId("a") }),
                Leaf("a"));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.PredicateMustReturnBoolean));
        }

        [Test]
        public void NodeConstructor_RejectsEachIllegalPerKindShape()
        {
            Assert.Throws<ArgumentException>(() => new EffectNodeDefinition(
                    new EffectNodeId("n"), EffectNodeKind.Optional, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("c") }),
                "An Optional node cannot be mandatory.");

            Assert.Throws<ArgumentException>(() => new EffectNodeDefinition(
                    new EffectNodeId("n"), EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                    childNodeIds: new[] { new EffectNodeId("c") },
                    chooser: EffectChooserRole.Actor),
                "An `if able` node opens no decision, so naming a chooser is contradictory.");

            Assert.Throws<ArgumentException>(() => new EffectNodeDefinition(
                    new EffectNodeId("n"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("a"), new EffectNodeId("b") },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 0,
                    maximumSelections: 2),
                "A mandatory Choose with a zero minimum is really an up-to.");

            Assert.Throws<ArgumentException>(() => new EffectNodeDefinition(
                    new EffectNodeId("n"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.NoteActorV1,
                    childNodeIds: new[] { new EffectNodeId("c") }),
                "A Capability node is a leaf.");

            Assert.Throws<ArgumentException>(() => new EffectNodeDefinition(
                    new EffectNodeId("n"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory),
                "A Sequence orders at least one child.");
        }

        private static System.Collections.Generic.IReadOnlyList<EffectDefinitionRejection> Validate(
            params EffectNodeDefinition[] nodes) =>
            EffectDefinitionValidator.Validate(
                new EffectDefinition(
                    new EffectDefinitionId("validator-fixture"), "rev-1", nodes[0].Id, nodes),
                SyntheticEffectCapabilities.CreateCatalog());

        private static EffectNodeDefinition Sequence(string id, params string[] children) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: children.Select(child => new EffectNodeId(child)));

        private static EffectNodeDefinition Leaf(string id) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.NoteActorV1);

        private static EffectNodeDefinition Producer(string id) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.NoteActorV1,
                outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) });

        private static EffectNodeDefinition Reader(string id, string sourceId) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.AcceptHeroV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "hero", new EffectNodeId(sourceId), "hero", EffectValueKind.Hero)
                });
    }
}
