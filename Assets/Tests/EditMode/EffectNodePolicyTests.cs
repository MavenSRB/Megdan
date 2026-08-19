using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
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
            Assert.That(Kinds(result), Does.Contain("blocked=DeclinedOptional"),
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
                definition, SyntheticEffectCapabilities.CreateCatalog(), null, null, 1, 1, RuleModificationSet.Empty, EffectCardState.Absent);

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
