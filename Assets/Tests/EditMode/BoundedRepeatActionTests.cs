using System;
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
    /// Milestone 6.7 at the frame: what a <see cref="EffectNodeKind.RepeatAction"/> node is, what it
    /// does to a resolution, and why it cannot run away.
    ///
    /// The load-bearing assertions here are
    /// <see cref="ARepeatedInstance_NeitherOffersNorRequestsAnotherRepeat"/> and
    /// <see cref="AReconstructedFrame_StillReportsTheRequestItsOutcomesRecord"/>. The first is the whole
    /// bound, and it is structural rather than counted: the node kind refuses to be mandatory, so an
    /// ineligible one is skipped rather than exposed or stopped on. The second is why the request needs
    /// no wire field of its own inside the effect frame - it is an ordinary applied node outcome, so a
    /// frame rebuilt from its serializable fields alone still reports it.
    ///
    /// Nothing here touches a board. The card-action side of the same milestone -  the correction
    /// boundary between two instances, and the second instance itself - is
    /// <see cref="MatchSessionRepeatDispatchTests"/>.
    /// </summary>
    public sealed class BoundedRepeatActionTests
    {
        private static readonly HeroId Actor = new HeroId("red-hero-1");
        private static readonly HeroId Controller = new HeroId("blue-hero-1");

        [Test]
        public void AMayRepeat_PausesOnAConfirmOrDeclineOwnedByItsDeclaredChooser()
        {
            var definition = RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Controller);

            var result = Start(definition);

            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.RepeatRequested, Is.False,
                "Nothing has been asked for while the decision is still open.");
            var choice = result.Frame.PendingChoice;
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline),
                "`you may Repeat this action` is p.10's confirm-or-decline, not a new prompt family.");
            Assert.That(choice.NodeId, Is.EqualTo(new EffectNodeId("repeat")));
            Assert.That(choice.ChooserId, Is.EqualTo(Controller),
                "Chooser ownership comes from the definition, exactly as every other node's does.");
            Assert.That(choice.Candidates, Is.Empty);
            Assert.That(choice.StopInvocation, Is.Null,
                "A repeat that can be declined is never the position 6.5ae's stop exists for.");
        }

        [Test]
        public void AConfirmedRepeat_CompletesTheInstanceAndReportsTheRequest()
        {
            var definition = RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Actor);
            var paused = Start(definition);

            var confirmed = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.ConfirmInvocation, Context(definition));

            Assert.That(confirmed.IsComplete, Is.True,
                "The instance that asked for the repeat still ends; the second one is a separate action.");
            Assert.That(confirmed.RepeatRequested, Is.True);
            Assert.That(Nodes(confirmed), Is.EqualTo(new[] { "repeat", "root" }),
                "Confirming records the request against the node that asked and lets the rest run.");
        }

        [Test]
        public void ADeclinedRepeat_CompletesWithoutRequestingAnything()
        {
            var definition = RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Actor);
            var paused = Start(definition);

            var declined = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.DeclineInvocation, Context(definition));

            Assert.That(declined.IsComplete, Is.True);
            Assert.That(declined.RepeatRequested, Is.False);
            Assert.That(
                declined.Events.Single(value => value.NodeId.Value == "repeat").ResultKind,
                Is.EqualTo(EffectExecutionResultKind.DeclinedOptional));
        }

        [Test]
        public void AnIfAbleRepeat_RequestsWithoutOpeningADecision()
        {
            var result = Start(RepeatAfterLeaf(EffectFailurePolicy.IfAble, EffectChooserRole.None));

            Assert.That(result.IsComplete, Is.True,
                "`if able` opens no choice, so the whole instance settles in one pass.");
            Assert.That(result.RepeatRequested, Is.True);
        }

        [Test]
        public void ARepeatedInstance_NeitherOffersNorRequestsAnotherRepeat()
        {
            var definition = RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Actor);

            var result = Start(definition, ActionInvocationContext.Repeated);

            Assert.That(result.IsComplete, Is.True,
                "Nothing pauses: the decision is never put to the player at all.");
            Assert.That(result.RepeatRequested, Is.False);
            Assert.That(
                result.Events.Single(value => value.NodeId.Value == "repeat").ResultKind,
                Is.EqualTo(EffectExecutionResultKind.DeclinedOptional),
                "An ineligible optional step is p.10's silent skip, not a stop - so the rest of the " +
                "repeated instance still runs.");
            Assert.That(Nodes(result), Does.Contain("leaf"),
                "The repeated instance performs everything an ordinary one does.");
        }

        [Test]
        public void AnIfAbleRepeatInARepeatedInstance_IsAlsoSkipped()
        {
            var result = Start(
                RepeatAfterLeaf(EffectFailurePolicy.IfAble, EffectChooserRole.None),
                ActionInvocationContext.Repeated);

            Assert.That(result.RepeatRequested, Is.False,
                "The bound does not depend on a player declining; a repeat that asks nobody is " +
                "refused by the same eligibility.");
        }

        [Test]
        public void AMandatoryOrCountedRepeatNode_IsRefusedAtConstruction()
        {
            Assert.That(
                () => RepeatNode(EffectFailurePolicy.Mandatory, EffectChooserRole.None),
                Throws.ArgumentException,
                "A repeat that must happen is unbounded, which is what this milestone exists to " +
                "prevent; refusing it here is what makes non-recursion a silent skip rather than a stop.");
            Assert.That(
                () => RepeatNode(EffectFailurePolicy.UpTo, EffectChooserRole.Actor),
                Throws.ArgumentException,
                "`up to` chooses a count, and this node has none.");
            Assert.That(
                () => RepeatNode(EffectFailurePolicy.May, EffectChooserRole.None),
                Throws.ArgumentException,
                "A may repeat opens a decision, so it names who makes it.");
            Assert.That(
                () => RepeatNode(EffectFailurePolicy.IfAble, EffectChooserRole.Actor),
                Throws.ArgumentException,
                "An if-able repeat opens no decision, so it names nobody.");
        }

        [Test]
        public void ARepeatNode_InvokesNoCapabilityAndBindsNothing()
        {
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                    SyntheticEffectCapabilities.NoteActorV1),
                Throws.ArgumentException);
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                    childNodeIds: new[] { new EffectNodeId("leaf") }),
                Throws.ArgumentException);
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                    outputs: new[] { new EffectOutputBinding("repeated", EffectValueKind.Boolean) }),
                Throws.ArgumentException,
                "A repeat publishes nothing: the second instance runs the same definition from its " +
                "own start, so there is nothing here for a later node to read.");
        }

        [Test]
        public void AReconstructedFrame_StillReportsTheRequestItsOutcomesRecord()
        {
            var definition = RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Actor);
            var paused = Start(definition);
            var confirmed = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.ConfirmInvocation, Context(definition));
            Assert.That(confirmed.RepeatRequested, Is.True);

            // The completing frame is gone, so this rebuilds the state a pause would have persisted and
            // asks the same question of it - which is what a snapshot restore and a replay do.
            var rebuilt = new PendingResolution(
                paused.Frame.ResolutionId,
                paused.Frame.SourceCardInstanceId,
                paused.Frame.SourceDefinitionReference,
                paused.Frame.ActionDefinitionId,
                paused.Frame.EffectDefinitionReference,
                paused.Frame.ActorId,
                paused.Frame.ControllerId,
                paused.Frame.InvocationContext,
                paused.Frame.Continuation,
                new[]
                {
                    new EffectNodeOutcome(
                        new EffectNodeId("repeat"), EffectExecutionResultKind.Applied)
                },
                paused.Frame.AcceptedChoices);

            Assert.That(
                RepeatedActionQuery.WasRequested(definition, rebuilt.CompletedOutcomes), Is.True);
            Assert.That(
                RepeatedActionQuery.WasRequested(definition, paused.Frame.CompletedOutcomes), Is.False,
                "The frame that had not yet been confirmed records no request.");
        }

        [Test]
        public void ADeclinedOutcomeOnTheSameNode_IsNotARequest()
        {
            var definition = RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Actor);

            Assert.That(
                RepeatedActionQuery.WasRequested(
                    definition,
                    new[]
                    {
                        new EffectNodeOutcome(
                            new EffectNodeId("repeat"), EffectExecutionResultKind.DeclinedOptional)
                    }),
                Is.False);
            Assert.That(
                RepeatedActionQuery.WasRequested(
                    definition,
                    new[]
                    {
                        new EffectNodeOutcome(
                            new EffectNodeId("leaf"), EffectExecutionResultKind.Applied)
                    }),
                Is.False,
                "The question is asked of the trusted definition's node kinds, so only a declared " +
                "Repeat can ask for one.");
        }

        [Test]
        public void TheFeasibilityWalk_JudgesARepeatEmptyRatherThanPerformable()
        {
            // A `Choose` between a repeat and an inert branch: if a repeat counted as performable, the
            // option would be offered on the strength of being able to do nothing twice.
            var definition = Definition(
                new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Choose, EffectFailurePolicy.UpTo,
                    childNodeIds: new[] { new EffectNodeId("repeat"), new EffectNodeId("leaf") },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 0,
                    maximumSelections: 1),
                RepeatNode(EffectFailurePolicy.IfAble, EffectChooserRole.None),
                Leaf("leaf"));

            var refused = EffectOptionFeasibilityQuery.FindUnperformableOptions(
                definition.GetNode(new EffectNodeId("root")),
                EffectResolutionSession.BuildStartFrame(
                    new EffectResolutionId("feasibility-fixture"),
                    new CardInstanceId("session-card"),
                    default,
                    new ActionDefinitionId("session-action"),
                    Actor,
                    Controller,
                    ActionInvocationContext.PrintedPrimary,
                    Context(definition)),
                Array.Empty<EffectNodeOutcome>(),
                Context(definition));

            Assert.That(
                refused.Select(candidate => candidate.OptionNodeId.Value),
                Is.EqualTo(new[] { "repeat" }),
                "What a repeat re-runs is the action the walk is judging, so it adds nothing of its " +
                "own to that judgement.");
        }

        private static EffectResolutionStepResult Start(
            EffectDefinition definition,
            ActionInvocationContext invocationContext = ActionInvocationContext.PrintedPrimary) =>
            EffectResolutionSession.Start(
                new EffectResolutionId("repeat-fixture"),
                new CardInstanceId("repeat-card"),
                default,
                new ActionDefinitionId("repeat-action"),
                Actor,
                Controller,
                invocationContext,
                Context(definition));

        private static EffectResolutionContext Context(EffectDefinition definition) =>
            new EffectResolutionContext(
                definition, SyntheticEffectCapabilities.CreateCatalog(), null, null, 1, 1,
                RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill);

        private static string[] Nodes(EffectResolutionStepResult result) =>
            result.Events.Select(value => value.NodeId.Value).ToArray();

        private static EffectDefinition Definition(params EffectNodeDefinition[] nodes) =>
            new EffectDefinition(
                new EffectDefinitionId("repeat-fixture-definition"), "rev-1", nodes[0].Id, nodes);

        /// <summary>An ordinary step, then the repeat - the shape every printed Repeat has.</summary>
        private static EffectDefinition RepeatAfterLeaf(
            EffectFailurePolicy policy,
            EffectChooserRole chooser) =>
            Definition(
                new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("leaf"), new EffectNodeId("repeat") }),
                Leaf("leaf"),
                RepeatNode(policy, chooser));

        private static EffectNodeDefinition RepeatNode(
            EffectFailurePolicy policy,
            EffectChooserRole chooser) =>
            new EffectNodeDefinition(
                new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, policy, chooser: chooser);

        private static EffectNodeDefinition Leaf(string id) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.NoteActorV1);
    }
}
