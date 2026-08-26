using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
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

        [Test]
        public void ImmunityOverride_IsAcceptedOnlyByACapabilityThatConsumesItsExactActivity()
        {
            var declaration = new ImmunityOverrideDeclaration(
                RuleActivity.DirectMove,
                ImmunityOverrideReason.PrintedHeavyMinionImmunity);
            var ignored = Validate(new EffectNodeDefinition(
                new EffectNodeId("ignored-override"), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.NoteActorV1,
                immunityOverrides: new[] { declaration }));
            var consumed = Validate(new EffectNodeDefinition(
                new EffectNodeId("consumed-override"), EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral(
                        "mover", EffectValue.Space(new GuardsDigital.Core.Board.HexCoordinate(0, 0))),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                    EffectInputBinding.FromLiteral(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        EffectValue.Integer((int)GuardsDigital.Core.Rules.Displacement
                            .DirectMoveDestinationPolicy.Unrestricted))
                },
                immunityOverrides: new[] { declaration }));

            Assert.That(ignored.Select(value => value.Reason),
                Does.Contain(EffectDefinitionRejectionReason.UnsupportedImmunityOverride));
            Assert.That(consumed, Is.Empty,
                "The existing capability identity consumes a trusted node declaration without a " +
                "registry or snapshot revision.");
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
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            "mover", EffectRoleKind.Actor, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        EffectValue.Integer(
                            (int)GuardsDigital.Core.Rules.Displacement
                                .DirectMoveDestinationPolicy.Unrestricted))
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
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            "mover", EffectRoleKind.ChosenSpace, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        EffectValue.Integer(
                            (int)GuardsDigital.Core.Rules.Displacement
                                .DirectMoveDestinationPolicy.Unrestricted))
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
                    SyntheticTargetingCapabilities.ChooseTargetV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(true))
                    },
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    new EffectNodeId("move"), EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", new EffectNodeId("select"), "chosen", EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        EffectValue.Integer(
                            (int)GuardsDigital.Core.Rules.Displacement
                                .DirectMoveDestinationPolicy.Unrestricted))
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

        [Test]
        public void ASelectionNamingAConsumerThisDefinitionDoesNotDeclare_IsRejected()
        {
            // Milestone 6.8l. A definition is one action's node table, so this is also how a selection
            // reaching for a node in another action is refused: there is no address that leaves here.
            var rejections = Validate(
                Sequence("root", "select", "move"),
                Selection("select", "somewhere-else"),
                Mover("move", "select"));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.UnresolvedSelectionConsumer));
        }

        [Test]
        public void ASelectionNamingANodeThatReadsNothingItPublishes_IsRejected()
        {
            // The two nodes are unrelated, so narrowing the selection's candidates by that node's
            // eligibility would be a guess about a pairing the definition never stated.
            var rejections = Validate(
                Sequence("root", "select", "move", "unrelated"),
                Selection("select", "unrelated"),
                Mover("move", "select"),
                Leaf("unrelated"));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.SelectionConsumerReadsNothing));
        }

        [Test]
        public void ASelectionNamingTheNodeThatBindsItsOutput_IsAccepted()
        {
            // The vacuity guard for both rules above, and the shape every converted card now uses.
            Assert.That(
                Validate(
                    Sequence("root", "select", "move"),
                    Selection("select", "move"),
                    Mover("move", "select")),
                Is.Empty);
        }

        [Test]
        public void ANodeReference_CanOnlyEverBeALiteral()
        {
            // Milestone 6.8l states this where the binding is built rather than as a validator rule,
            // because a node reference addresses the definition that is already loaded: a frame
            // resolving one from a role, or a handler publishing one, would be inventing a definition
            // while the match runs. That also keeps it out of every payload by construction.
            Assert.Throws<ArgumentException>(() => EffectInputBinding.FromRole(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectRoleKind.Actor,
                    EffectValueKind.Node),
                "No role supplies a node reference.");
            Assert.Throws<ArgumentException>(() => EffectInputBinding.FromNodeOutput(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    new EffectNodeId("earlier"),
                    "chosen",
                    EffectValueKind.Node),
                "And no node publishes one.");
            Assert.Throws<ArgumentException>(
                () => new EffectOutputBinding("consumer", EffectValueKind.Node),
                "So no capability may declare one as an output either.");
            Assert.Throws<ArgumentException>(
                () => new EffectOutcomeFact("consumer", EffectValue.Node(new EffectNodeId("move"))),
                "Nor publish one as a produced fact.");
        }

        [Test]
        public void AMalformedEffectDefinition_IsRefusedBeforeTheMatchStarts()
        {
            // Milestone 7.9d, finding F-CARD-04 - the 6.2b review's F62B-05. This validator has been
            // complete since 6.2a and, until this milestone, had no production caller: every rule it
            // enforces was a guarantee the docs claimed and no load path asked for, so an unregistered
            // capability or an unguaranteed binding would have surfaced as a mid-resolution fault after
            // the card was spent. The link assertion is the half a behavioral case cannot state: the
            // configuration type itself must reach Validate, so removing the call fails here rather
            // than passing vacuously while the behavioral half tests the validator directly.
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(
                        typeof(GuardsDigital.Core.Participants.MatchConfiguration)),
                    ILConformanceScanner.Method(
                        typeof(EffectDefinitionValidator), nameof(EffectDefinitionValidator.Validate))),
                Is.True,
                "The configuration is where definitions enter a match, so it is the load path that " +
                "must ask; a fixture calling the validator on its own content is not enforcement.");

            // And the refusal itself, through the configuration's own private load-path helper, with a
            // definition whose capability nothing registers - the exact shape that used to load
            // cleanly and fault mid-resolution.
            var malformed = new CardDefinition(
                new CardDefinitionId("validator-load-path-card"),
                "rev-1",
                "TEST - Validator Load Path",
                new InitiativeValue(30),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("validator-load-path-skill"),
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        System.Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        new EffectDefinition(
                            new EffectDefinitionId("validator-load-path-effect"),
                            "rev-1",
                            new EffectNodeId("root"),
                            new[]
                            {
                                new EffectNodeDefinition(
                                    new EffectNodeId("root"),
                                    EffectNodeKind.Capability,
                                    EffectFailurePolicy.Mandatory,
                                    new EffectCapabilityReference(
                                        new EffectCapabilityId("not-registered-anywhere"), 1))
                            }))
                });

            var loadPath = typeof(GuardsDigital.Core.Participants.MatchConfiguration)
                .GetMethod(
                    "ValidateEffectDefinitions",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(loadPath, Is.Not.Null,
                "The load-path helper the link assertion proved reachable must exist to be exercised.");

            var thrown = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
                loadPath.Invoke(
                    null,
                    new object[] { new[] { malformed }, MatchContentCatalogs.ForThisBuild() }));
            Assert.That(thrown.InnerException, Is.InstanceOf<System.ArgumentException>());
            Assert.That(thrown.InnerException.Message,
                Does.Contain(nameof(EffectDefinitionRejectionReason.UnregisteredCapability)),
                "The refusal carries the typed rejection, because that is what a content author acts on.");
            Assert.That(thrown.InnerException.Message, Does.Contain("validator-load-path-card"),
                "And it names the card, because a load failure over 24 definitions is otherwise a hunt.");
        }

        /// <summary>
        /// Milestone 8.3, closing the 7.9a review's F-CARD-06: the node table is a tree, and the
        /// validator used to check only that it was acyclic.
        ///
        /// A diamond passes every rule that existed before this one - no cycle, every node reachable,
        /// every child resolving - and then breaks the dominance analysis that makes typed result
        /// binding safe. That analysis unions each parent's guarantees into its children, which is
        /// right when there is one parent and wrong with two: the shared node inherits guarantees from
        /// the path that did not run. So this is the shape the rule has to refuse, and refusing it at
        /// content load is the difference between a rejected definition and a card spent on a
        /// resolution that faults halfway through.
        /// </summary>
        [Test]
        public void ANodeReachableThroughTwoParents_IsRejected()
        {
            var rejections = Validate(
                Sequence("root", "left", "right"),
                Sequence("left", "shared"),
                Sequence("right", "shared"),
                Leaf("shared"));

            Assert.That(rejections.Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.NodeReachableThroughTwoParents),
                "A node named as a child by two nodes is a graph, and every reader of this table - the " +
                "resolver's single visit, its node-keyed continuations, the dominance analysis - reads " +
                "a tree.");
            Assert.That(rejections.Single().Detail, Does.Contain("shared"),
                "The refusal names the node, because a content author fixes one declaration.");
        }

        /// <summary>The same rule from the other end: the root is where execution starts.</summary>
        [Test]
        public void ARootNamedAsAChild_IsRejected()
        {
            var rejections = Validate(
                Sequence("root", "child"),
                Sequence("child", "root"));

            // A root with a parent is also a cycle, and the cycle rule reports first and returns; what
            // this pins is that reaching the root again is refused either way.
            var graphRules = new[]
            {
                EffectDefinitionRejectionReason.CyclicNodeGraph,
                EffectDefinitionRejectionReason.NodeReachableThroughTwoParents
            };

            Assert.That(
                rejections.Any(rejection => graphRules.Contains(rejection.Reason)),
                Is.True,
                "Reaching the root again is refused, whichever of the two graph rules names it first.");
        }

        /// <summary>
        /// The compliant twin. Sharing is expressed by declaring the node twice under two ids, which is
        /// the shape every configured definition already has - so the rule above bans a shape nothing
        /// in this build authors, rather than one it relies on.
        /// </summary>
        [Test]
        public void TheSameWorkUnderTwoNodeIds_StaysAccepted()
        {
            Assert.That(
                Validate(
                    Sequence("root", "left", "right"),
                    Sequence("left", "shared-a"),
                    Sequence("right", "shared-b"),
                    Leaf("shared-a"),
                    Leaf("shared-b")),
                Is.Empty);
        }


        /// <summary>
        /// Milestone 8.19a, and the retirement of milestone 8.5's opposite rule.
        ///
        /// <para>Card text applied at any of rulebook p.14's three moments may now ask the player a
        /// question. 8.5 refused all three node shapes that open one, because an Attack's remaining
        /// steps were produced in the command that began them; 8.10 held steps 4 and 7, whose
        /// remainders follow from the attack's identity; this milestone holds step 1, whose remainder is
        /// the rest of the page produced against live state. All three shapes are asserted at step 1
        /// specifically, because that is the moment that changed.</para>
        /// </summary>
        [Test]
        public void EveryPausingShape_IsAdmittedAtRulebookPageFourteensFirstStep()
        {
            var choose = new EffectNodeDefinition(
                new EffectNodeId("root"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { new EffectNodeId("a"), new EffectNodeId("b") },
                chooser: EffectChooserRole.Actor,
                minimumSelections: 1,
                maximumSelections: 1);
            var mayStep = new EffectNodeDefinition(
                new EffectNodeId("root"), EffectNodeKind.Optional, EffectFailurePolicy.May,
                childNodeIds: new[] { new EffectNodeId("a") },
                chooser: EffectChooserRole.Actor);
            var repeat = new EffectNodeDefinition(
                new EffectNodeId("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { new EffectNodeId("a"), new EffectNodeId("b") });

            Assert.That(ValidateCardText(choose, Leaf("a"), Leaf("b")), Is.Empty,
                "Rulebook p.10's Choose is what Brogan's Mad Dash asks at step 1.");
            Assert.That(ValidateCardText(mayStep, Leaf("a")), Is.Empty,
                "A `may` step is what Arien's Noble Blade offers at step 1.");
            Assert.That(
                ValidateCardText(
                    repeat,
                    Leaf("a"),
                    new EffectNodeDefinition(
                        new EffectNodeId("b"), EffectNodeKind.RepeatAction,
                        EffectFailurePolicy.IfAble)),
                Is.Empty,
                "A repeat the actor may decline opens the same question the other two do.");

            // The unchanged twin: text that only performs was admitted before and still is.
            Assert.That(ValidateCardText(Sequence("root", "a", "b"), Leaf("a"), Leaf("b")), Is.Empty,
                "Text that only performs is admitted, which is what the seam exists to run.");
        }

        /// <summary>
        /// Milestone 8.19a. Text applied around a primary action is entered twice - once for the
        /// question that places it and once for the work that question placed - so the definition has to
        /// say which node is which. Each way the declaration can fail is asserted beside the compliant
        /// twin.
        /// </summary>
        [Test]
        public void TextAppliedAroundAPrimaryAction_DeclaresItsTwoHalvesOrIsRefused()
        {
            var compliant = new[]
            {
                Sequence("root", "order", "work"),
                new EffectNodeDefinition(
                    new EffectNodeId("order"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("before"), new EffectNodeId("after") },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1),
                Placement("before", SyntheticContinuationCapabilities.PlacementBeforeTheAction),
                Placement("after", SyntheticContinuationCapabilities.PlacementAfterTheAction),
                Leaf("work")
            };
            Assert.That(
                ValidateCardTextAt(GuardsDigital.Core.Cards.CardTextTiming.AroundTheAction, compliant),
                Is.Empty,
                "The declared shape is a root sequence of the ordering question and the work it places.");

            // A root that is not two nodes: nothing says which half is which.
            Assert.That(
                ValidateCardTextAt(
                    GuardsDigital.Core.Cards.CardTextTiming.AroundTheAction,
                    Sequence("root", "work"), Leaf("work"))
                    .Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.AroundActionShapeIsMalformed));

            // A first node that asks nothing: the actor never places the work.
            Assert.That(
                ValidateCardTextAt(
                    GuardsDigital.Core.Cards.CardTextTiming.AroundTheAction,
                    Sequence("root", "order", "work"), Leaf("order"), Leaf("work"))
                    .Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.AroundActionShapeIsMalformed));

            // A branch that publishes no side: the question is asked and its answer says nothing.
            var branchless = new[]
            {
                Sequence("root", "order", "work"),
                new EffectNodeDefinition(
                    new EffectNodeId("order"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("before"), new EffectNodeId("after") },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1),
                Placement("before", SyntheticContinuationCapabilities.PlacementBeforeTheAction),
                Leaf("after"),
                Leaf("work")
            };
            Assert.That(
                ValidateCardTextAt(
                    GuardsDigital.Core.Cards.CardTextTiming.AroundTheAction, branchless)
                    .Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.AroundActionShapeIsMalformed));

            // And the rule belongs to that timing alone: the same table is ordinary text elsewhere.
            Assert.That(
                ValidateCardTextAt(
                    GuardsDigital.Core.Cards.CardTextTiming.AfterTheAttack,
                    Sequence("root", "work"), Leaf("work")),
                Is.Empty);
        }

        private static EffectNodeDefinition Placement(string id, int side) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.AroundPrimaryActionV1,
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral(
                        SyntheticContinuationCapabilities.PlacementParameter,
                        EffectValue.Integer(side))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticContinuationCapabilities.PlacementOutput, EffectValueKind.Integer)
                });

        // ---------------------------------------------------------------------------------------
        // Milestone 8.6: a same-target repeat has to be able to inherit a target
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ASameTargetRepeat_IsAcceptedWhenItsSelectionCertainlyRanAndPublishesOneSpace()
        {
            Assert.That(
                Validate(
                    Sequence("root", "select", "move", "repeat"),
                    Selection("select", "move"),
                    Mover("move", "select"),
                    BoundRepeat("repeat", "select")),
                Is.Empty,
                "The selection is a guaranteed predecessor of the repeat and publishes exactly one " +
                "coordinate, which is everything the inherited target needs.");
        }

        [Test]
        public void ASameTargetRepeat_IsRejectedWhenItCouldNotInheritATarget()
        {
            Assert.That(
                Validate(
                    Sequence("root", "select", "move", "repeat"),
                    Selection("select", "move"),
                    Mover("move", "select"),
                    BoundRepeat("repeat", "absent"))
                    .Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.UnresolvedRepeatTargetBinding),
                "One definition is one action's node table, and there is no address that reaches " +
                "out of it.");

            Assert.That(
                Validate(
                    Sequence("root", "producer", "repeat"),
                    Producer("producer"),
                    BoundRepeat("repeat", "producer"))
                    .Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.UnresolvedRepeatTargetBinding),
                "A node publishing a hero and no space has no target for the repeat to inherit.");

            // The selection is inside one arm of a Choose, so a run that took the other arm reaches the
            // repeat having named nobody - and the card would silently become p.10's free choice.
            Assert.That(
                Validate(
                    Sequence("root", "choice", "repeat"),
                    new EffectNodeDefinition(
                        new EffectNodeId("choice"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { new EffectNodeId("select"), new EffectNodeId("other") },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    Selection("select", "other"),
                    Leaf("other"),
                    BoundRepeat("repeat", "select"))
                    .Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.UnresolvedRepeatTargetBinding),
                "and a selection that has not certainly run cannot be the one a repeat binds.");
        }

        [Test]
        public void TwoRepeatsBindingATarget_AreRejectedBecauseOneInheritedPieceHasOneOwner()
        {
            Assert.That(
                Validate(
                    Sequence("root", "select", "move", "first", "second"),
                    Selection("select", "move"),
                    Mover("move", "select"),
                    BoundRepeat("first", "select"),
                    BoundRepeat("second", "select"))
                    .Select(rejection => rejection.Reason),
                Does.Contain(EffectDefinitionRejectionReason.UnresolvedRepeatTargetBinding),
                "A repeated instance inherits one piece and carries no node identity with it, so two " +
                "bindings would leave it unable to say which selection the piece belongs to.");
        }

        private static EffectNodeDefinition BoundRepeat(string id, string selectionId) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                chooser: EffectChooserRole.Actor,
                repeatTargetBinding: RepeatTargetBinding.SameTarget,
                boundSelectionNodeId: new EffectNodeId(selectionId));

        /// <summary>
        /// Milestone 8.10 made p.14's moment part of the question, and step 1 is the one that still
        /// cannot hold one - so it is the moment these cases ask about.
        /// </summary>
        private static System.Collections.Generic.IReadOnlyList<EffectDefinitionRejection> ValidateCardText(
            params EffectNodeDefinition[] nodes) =>
            ValidateCardTextAt(GuardsDigital.Core.Cards.CardTextTiming.BeforeTheAttack, nodes);

        private static System.Collections.Generic.IReadOnlyList<EffectDefinitionRejection> ValidateCardTextAt(
            GuardsDigital.Core.Cards.CardTextTiming moment,
            params EffectNodeDefinition[] nodes) =>
            EffectDefinitionValidator.ValidateCardText(
                new EffectDefinition(
                    new EffectDefinitionId("validator-fixture"), "rev-1", nodes[0].Id, nodes),
                SyntheticEffectCapabilities.CreateCatalog(),
                moment);
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

        /// <summary>Milestone 6.8l's shape: a selection declaring the node it selects for.</summary>
        private static EffectNodeDefinition Selection(string id, string consumerId) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.ChooseTargetV3,
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                    EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(true)),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName,
                        EffectValue.Node(new EffectNodeId(consumerId)))
                },
                outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });

        private static EffectNodeDefinition Mover(string id, string selectionId) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", new EffectNodeId(selectionId), "chosen", EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                    EffectInputBinding.FromLiteral(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        EffectValue.Integer(
                            (int)GuardsDigital.Core.Rules.Displacement
                                .DirectMoveDestinationPolicy.Unrestricted))
                });

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
