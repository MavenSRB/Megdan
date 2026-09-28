using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2a acceptance check 2: mandatory failure, `may`, `up to`, and `if able` stay four
    /// explicit behaviors rather than collapsing into one failure path.
    ///
    /// Each policy is asserted independently and against the same impossible step, so merging any two
    /// of them fails here rather than silently reinterpreting a card. The two axes that keep them
    /// apart are whether the policy opens a decision at all, and what an impossible step means.
    /// </summary>
    public sealed class EffectNodePolicyTests
    {
        private static readonly HeroId Actor = new HeroId("red-hero-1");
        private static readonly HeroId Controller = new HeroId("red-hero-1");

        [Test]
        public void MandatoryImpossibleStep_StopsTheActionAndKeepsEarlierWork()
        {
            var definition = SequenceOf(
                Leaf("first", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                Leaf("blocked", SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf("never", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));

            var result = Start(definition);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.IsComplete, Is.True, "A mandatory impossible step ends the action.");
            Assert.That(Kinds(result), Is.EqualTo(new[]
            {
                "first=Applied",
                "blocked=ImpossibleMandatory"
            }), "The step after the impossible one must not run, and the one before it must stay applied.");
        }

        [Test]
        public void IfAbleImpossibleStep_IsSkippedSilentlyAndOpensNoChoice()
        {
            var definition = SequenceOf(
                Optional("maybe", "blocked", EffectFailurePolicy.IfAble, EffectChooserRole.None),
                Leaf("after", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));
            definition = WithNode(definition,
                Leaf("blocked", SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.IfAble));

            var result = Start(definition);

            Assert.That(result.IsComplete, Is.True, "`if able` never pauses for a decision.");
            // Milestone 9.1: a branch that cannot be performed is skipped at the Optional, before it
            // is entered, so the record sits on the Optional rather than on the step inside it.
            Assert.That(Kinds(result), Does.Contain("maybe=DeclinedOptional"),
                "An `if able` step that cannot happen is skipped, not a failure.");
            Assert.That(Kinds(result), Does.Contain("after=Applied"),
                "`if able` never stops the rest of the action.");
        }

        [Test]
        public void MayStep_AlwaysOpensAConfirmOrDeclineDecision()
        {
            var definition = SequenceOf(
                Optional("maybe", "inner", EffectFailurePolicy.May, EffectChooserRole.Actor));
            definition = WithNode(definition,
                Leaf("inner", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));

            var result = Start(definition);

            Assert.That(result.IsComplete, Is.False, "`may` always asks, even when the step is possible.");
            var choice = result.Frame.PendingChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Assert.That(choice.DeclineInvocation, Is.Not.Null, "`may` must be declinable.");
            Assert.That(choice.ChooserId, Is.EqualTo(Actor));
        }

        [Test]
        public void IfAbleStep_OpensNoDecisionWhereMayWould()
        {
            var mayDefinition = SequenceOf(
                Optional("maybe", "inner", EffectFailurePolicy.May, EffectChooserRole.Actor));
            mayDefinition = WithNode(mayDefinition,
                Leaf("inner", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));

            var ifAbleDefinition = SequenceOf(
                Optional("maybe", "inner", EffectFailurePolicy.IfAble, EffectChooserRole.None));
            ifAbleDefinition = WithNode(ifAbleDefinition,
                Leaf("inner", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));

            Assert.That(Start(mayDefinition).IsComplete, Is.False);
            Assert.That(Start(ifAbleDefinition).IsComplete, Is.True,
                "This is the pair that collapses if anyone merges the policies: one always asks, the " +
                "other never does, on identical inner work.");
        }

        [Test]
        public void DecliningAMayStep_RecordsTheDecisionAndContinues()
        {
            var definition = SequenceOf(
                Optional("maybe", "inner", EffectFailurePolicy.May, EffectChooserRole.Actor),
                Leaf("after", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));
            definition = WithNode(definition,
                Leaf("inner", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));

            var paused = Start(definition);
            var declined = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.DeclineInvocation, Context(definition));

            Assert.That(declined.Accepted, Is.True);
            Assert.That(declined.IsComplete, Is.True);
            Assert.That(Kinds(declined), Does.Contain("maybe=DeclinedOptional"));
            Assert.That(Kinds(declined), Does.Contain("after=Applied"),
                "Declining an optional step continues the action.");
        }

        [Test]
        public void UpToChoose_AcceptsFewerSelectionsThanItsMaximum()
        {
            var definition = new EffectDefinition(
                new EffectDefinitionId("up-to-fixture"), "rev-1", new EffectNodeId("root"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("root"), EffectNodeKind.Choose, EffectFailurePolicy.UpTo,
                        childNodeIds: new[] { new EffectNodeId("a"), new EffectNodeId("b") },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 0,
                        maximumSelections: 2),
                    Leaf("a", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                    Leaf("b", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)
                });

            var paused = Start(definition);
            Assert.That(paused.Frame.PendingChoice.ConfirmInvocation, Is.Not.Null,
                "`up to` permits selecting nothing, so Confirm is legal before anything is selected.");

            var confirmed = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.ConfirmInvocation, Context(definition));

            Assert.That(confirmed.Accepted, Is.True);
            Assert.That(confirmed.IsComplete, Is.True);
            Assert.That(Kinds(confirmed), Has.None.Contains("a="),
                "An unselected option does not execute, and selecting fewer is not a failure.");
        }

        [Test]
        public void MandatoryChoose_RequiresAtLeastOneSelection()
        {
            var definition = new EffectDefinition(
                new EffectDefinitionId("mandatory-choose-fixture"), "rev-1", new EffectNodeId("root"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("root"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { new EffectNodeId("a"), new EffectNodeId("b") },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    Leaf("a", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                    Leaf("b", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)
                });

            var paused = Start(definition);

            Assert.That(paused.Frame.PendingChoice.ConfirmInvocation, Is.Null,
                "A mandatory Choose cannot be confirmed empty.");
            Assert.That(paused.Frame.PendingChoice.DeclineInvocation, Is.Null,
                "A mandatory Choose is not declinable.");
        }


        /// <summary>
        /// Milestone 8.5, closing the 7.9a review's F-CARD-07.
        ///
        /// A `Conditional` used to execute its predicate for one boolean and throw the rest away. Two
        /// things were lost with it. The typed facts the predicate published never reached the outcome
        /// list, so no later node could bind to what it had established - which is exactly what Wasp's
        /// Thunder Boomerang needs when it asks whether the attack targeted a hero. And a predicate
        /// that paused, refused, or declined arrived here indistinguishable from one that had answered
        /// no, so the else branch of a rule the card printed was taken because the question could not
        /// be asked.
        ///
        /// Both halves are asserted here against the same node kind.
        /// </summary>
        [Test]
        public void AConditionalPredicate_PublishesItsFactsAndCannotSilentlyReportFalse()
        {
            // The facts half: a later node binds the predicate's own published output.
            var publishing = ConditionalDefinition(
                SyntheticEffectCapabilities.IsFlaggedV1,
                new[] { EffectInputBinding.FromLiteral("flag", EffectValue.Boolean(true)) },
                thenChild: new EffectNodeDefinition(
                    new EffectNodeId("then"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    PredicateReaderCapability.Reference,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", new EffectNodeId("root"), "value", EffectValueKind.Boolean)
                    }));

            var published = Start(publishing, PredicateCatalog());

            Assert.That(published.Accepted, Is.True);
            Assert.That(Kinds(published), Does.Contain("root=Applied"),
                "The predicate's own result is recorded, because that is where its facts live.");
            Assert.That(Kinds(published), Does.Contain("then=Applied"),
                "and the branch node could bind the value the predicate published - which is the " +
                "whole point of publishing it, and which resolved to nothing before this milestone.");

            // The silence half: a predicate that cannot be asked is not an answer of no.
            var refusing = ConditionalDefinition(
                RefusingPredicateCapability.Reference,
                System.Array.Empty<EffectInputBinding>(),
                thenChild: Leaf("then", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));

            var refused = Start(refusing, PredicateCatalog());

            Assert.That(refused.Accepted, Is.True);
            Assert.That(Kinds(refused), Does.Contain("root=ImpossibleMandatory"),
                "A mandatory predicate that cannot be asked stops the action, exactly as any other " +
                "mandatory step that cannot be taken does.");
            Assert.That(Kinds(refused), Has.None.EqualTo("then=Applied"),
                "and above all it does not quietly take a branch: the else arm of a rule the card " +
                "printed must not be reached because the question was unanswerable.");
            Assert.That(Kinds(refused), Has.None.EqualTo("else=Applied"));
        }

        /// <summary>
        /// A `Conditional` root over one predicate, with a `then` branch supplied by the caller and an
        /// `else` branch that would be visible if it were ever taken. Milestone 8.5.
        /// </summary>
        private static EffectDefinition ConditionalDefinition(
            EffectCapabilityReference predicate,
            IReadOnlyList<EffectInputBinding> predicateInputs,
            EffectNodeDefinition thenChild)
        {
            var elseChild = Leaf(
                "else", SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory);
            var root = new EffectNodeDefinition(
                new EffectNodeId("root"), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                predicate,
                childNodeIds: new[] { thenChild.Id, elseChild.Id },
                inputs: predicateInputs);
            return new EffectDefinition(
                new EffectDefinitionId("predicate-fixture-definition"), "rev-1", root.Id,
                new[] { root, thenChild, elseChild });
        }

        /// <summary>
        /// The registered set plus this fixture's two predicate handlers. Built by loading each
        /// registered reference back out of the synthetic catalog rather than by listing handlers, so a
        /// capability added there arrives here without this fixture being edited.
        /// </summary>
        private static EffectCapabilityCatalog PredicateCatalog()
        {
            var synthetic = SyntheticEffectCapabilities.CreateCatalog();
            return new EffectCapabilityCatalog(synthetic.Registered
                .Select(synthetic.Load)
                .Concat(new IEffectCapabilityHandler[]
                {
                    new PredicateReaderCapability(),
                    new RefusingPredicateCapability()
                }));
        }

        /// <summary>Consumes a Boolean a prior node published, so a binding has somewhere to land.</summary>
        private sealed class PredicateReaderCapability : IEffectCapabilityHandler
        {
            public static EffectCapabilityReference Reference { get; } =
                new EffectCapabilityReference(new EffectCapabilityId("fixture-predicate-reader"), 1);

            EffectCapabilityReference IEffectCapabilityHandler.Reference => Reference;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter("flag", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                System.Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                context.Input("flag").Kind == EffectValueKind.Boolean
                    ? EffectExecutionResult.Applied()
                    : EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new System.InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// A predicate that cannot be asked at all. It is the shape the old reading collapsed into
        /// "false": there is no boolean in the result, so the branch selection had nothing to read.
        /// </summary>
        private sealed class RefusingPredicateCapability : IEffectCapabilityHandler
        {
            public static EffectCapabilityReference Reference { get; } =
                new EffectCapabilityReference(new EffectCapabilityId("fixture-refusing-predicate"), 1);

            EffectCapabilityReference IEffectCapabilityHandler.Reference => Reference;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                System.Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new System.InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.ImpossibleMandatory;
        }

        private static EffectResolutionStepResult Start(
            EffectDefinition definition, EffectCapabilityCatalog catalog) =>
            EffectResolutionSession.Start(
                new EffectResolutionId("policy-fixture"),
                new CardInstanceId("policy-card"),
                default,
                new ActionDefinitionId("policy-action"),
                Actor,
                Controller,
                ActionInvocationContext.PrintedPrimary,
                new EffectResolutionContext(
                    definition, catalog, null, null, 1, 1, RuleModificationSet.Empty,
                    EffectCardState.Absent, ActionKind.Skill));
        private static EffectResolutionStepResult Start(EffectDefinition definition) =>
            EffectResolutionSession.Start(
                new EffectResolutionId("policy-fixture"),
                new CardInstanceId("policy-card"),
                default,
                new ActionDefinitionId("policy-action"),
                Actor,
                Controller,
                ActionInvocationContext.PrintedPrimary,
                Context(definition));

        private static EffectResolutionContext Context(EffectDefinition definition) =>
            new EffectResolutionContext(
                definition, SyntheticEffectCapabilities.CreateCatalog(), null, null, 1, 1, RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill);

        private static IReadOnlyList<string> Kinds(EffectResolutionStepResult result) =>
            result.Events.Select(value => $"{value.NodeId}={value.ResultKind}").ToArray();

        private static EffectNodeDefinition Leaf(
            string id,
            EffectCapabilityReference capability,
            EffectFailurePolicy policy) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, policy, capability);

        private static EffectNodeDefinition Optional(
            string id,
            string childId,
            EffectFailurePolicy policy,
            EffectChooserRole chooser) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Optional, policy,
                childNodeIds: new[] { new EffectNodeId(childId) },
                chooser: chooser);

        private static EffectDefinition SequenceOf(params EffectNodeDefinition[] children)
        {
            var root = new EffectNodeDefinition(
                new EffectNodeId("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: children.Select(child => child.Id));
            return new EffectDefinition(
                new EffectDefinitionId("policy-fixture-definition"), "rev-1", root.Id,
                new[] { root }.Concat(children));
        }

        private static EffectDefinition WithNode(EffectDefinition definition, EffectNodeDefinition node) =>
            new EffectDefinition(
                definition.Id, definition.ContentRevision, definition.RootNodeId,
                definition.Nodes.Concat(new[] { node }));
    }
}
