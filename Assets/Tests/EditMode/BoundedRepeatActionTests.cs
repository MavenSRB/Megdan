using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
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
            Assert.That(result.RepeatRequested.IsRequested, Is.False,
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
            Assert.That(confirmed.RepeatRequested.IsRequested, Is.True);
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
            Assert.That(declined.RepeatRequested.IsRequested, Is.False);
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
            Assert.That(result.RepeatRequested.IsRequested, Is.True);
        }

        [Test]
        public void ARepeatedInstance_NeitherOffersNorRequestsAnotherRepeat()
        {
            var definition = RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Actor);

            var result = Start(definition, ActionInvocationContext.Repeated);

            Assert.That(result.IsComplete, Is.True,
                "Nothing pauses: the decision is never put to the player at all.");
            Assert.That(result.RepeatRequested.IsRequested, Is.False);
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

            Assert.That(result.RepeatRequested.IsRequested, Is.False,
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
            Assert.That(confirmed.RepeatRequested.IsRequested, Is.True);

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
                RepeatedActionQuery.Requested(definition, rebuilt, rebuilt.CompletedOutcomes).IsRequested,
                Is.True);
            Assert.That(
                RepeatedActionQuery.Requested(
                    definition, paused.Frame, paused.Frame.CompletedOutcomes).IsRequested,
                Is.False,
                "The frame that had not yet been confirmed records no request.");
        }

        [Test]
        public void ADeclinedOutcomeOnTheSameNode_IsNotARequest()
        {
            var definition = RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Actor);

            var frame = Start(definition).Frame;
            Assert.That(
                RepeatedActionQuery.Requested(
                    definition,
                    frame,
                    new[]
                    {
                        new EffectNodeOutcome(
                            new EffectNodeId("repeat"), EffectExecutionResultKind.DeclinedOptional)
                    }).IsRequested,
                Is.False);
            Assert.That(
                RepeatedActionQuery.Requested(
                    definition,
                    frame,
                    new[]
                    {
                        new EffectNodeOutcome(
                            new EffectNodeId("leaf"), EffectExecutionResultKind.Applied)
                    }).IsRequested,
                Is.False,
                "The question is asked of the trusted definition's node kinds, so only a declared " +
                "Repeat can ask for one.");
        }

        /// <summary>
        /// The walk's verdict on a repeat has not moved: a repeat re-runs the very action being judged,
        /// so it adds nothing of its own to that judgement and is read as empty rather than performable.
        ///
        /// <para><b>What the verdict now decides is narrower.</b> Milestone 8.35z35 (Ruling 17)
        /// stopped <see cref="EffectOptionFeasibilityQuery.FindUnperformableOptions"/> greying an empty
        /// option: only a branch a mandatory step blocks is refused, and a repeat is blocked by nothing.
        /// So this option is offered, and the concern the empty verdict exists for is answered where it
        /// was always the real question - <see cref="EffectOptionFeasibilityQuery.CanApplyAnything"/>,
        /// which still refuses to offer an ACTION that can do nothing but repeat itself. Both halves are
        /// asserted here so the pair cannot drift into one.</para>
        /// </summary>
        [Test]
        public void TheFeasibilityWalk_JudgesARepeatEmptyRatherThanPerformable()
        {
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
                StartFrame(definition),
                Array.Empty<EffectNodeOutcome>(),
                Context(definition));

            Assert.That(
                refused, Is.Empty,
                "Nothing stops the repeat arm, so Ruling 17 offers it: an option is greyed for being " +
                "blocked, never for being empty.");

            // The other half. A definition that is nothing but a repeat still proves empty, which is
            // the verdict itself - had the walk called a repeat performable this would read true.
            var repeatOnly = Definition(
                new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("repeat") }),
                RepeatNode(EffectFailurePolicy.IfAble, EffectChooserRole.None));

            Assert.That(
                EffectOptionFeasibilityQuery.CanApplyAnything(
                    StartFrame(repeatOnly), Context(repeatOnly)),
                Is.False,
                "What a repeat re-runs is the action being judged, so an action whose whole text is a " +
                "repeat is still proven empty and still not offered.");
        }

        private static PendingResolution StartFrame(EffectDefinition definition) =>
            EffectResolutionSession.BuildStartFrame(
                new EffectResolutionId("feasibility-fixture"),
                new CardInstanceId("session-card"),
                default,
                new ActionDefinitionId("session-action"),
                Actor,
                Controller,
                ActionInvocationContext.PrintedPrimary,
                Context(definition));

        /// <summary>
        /// Starts one instance. Since milestone 8.6 the two ways of saying "this is a repeat" travel
        /// together - the invocation context and the continuation carrying the count - so this derives
        /// the second from the first rather than letting a fixture state one without the other.
        /// </summary>
        private static EffectResolutionStepResult Start(
            EffectDefinition definition,
            ActionInvocationContext invocationContext = ActionInvocationContext.PrintedPrimary,
            int completedRepeats = 1) =>
            EffectResolutionSession.Start(
                new EffectResolutionId("repeat-fixture"),
                new CardInstanceId("repeat-card"),
                default,
                new ActionDefinitionId("repeat-action"),
                Actor,
                Controller,
                invocationContext,
                Context(definition),
                invocationContext == ActionInvocationContext.Repeated
                    ? RepeatContinuation.Next(completedRepeats)
                    : RepeatContinuation.FirstInstance);

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

        // ---------------------------------------------------------------------------------------
        // Milestone 8.6: the declared maximum and the same-target binding
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ADeclaredMaximum_BoundsTheChainAndDefaultsToTheOneItAlwaysWas()
        {
            var once = RepeatNode(EffectFailurePolicy.May, EffectChooserRole.Actor);
            Assert.That(once.MaximumRepeats, Is.EqualTo(1),
                "Every repeat written before milestone 8.6 declared nothing, and what it meant was " +
                "one - so one is what declaring nothing still means.");

            var frame = Start(RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Actor)).Frame;
            Assert.That(RepeatedActionQuery.CanRepeat(frame, once), Is.True,
                "A printed instance has run no repeats, so it may ask.");

            var twice = new EffectNodeDefinition(
                new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                chooser: EffectChooserRole.Actor, maximumRepeats: 2);
            var afterOne = RepeatedFrame(frame, 1);
            var afterTwo = RepeatedFrame(frame, 2);

            Assert.That(RepeatedActionQuery.CanRepeat(afterOne, once), Is.False,
                "One repeat exhausts a maximum of one, which is 6.7's structural bound restated.");
            Assert.That(RepeatedActionQuery.CanRepeat(afterOne, twice), Is.True,
                "and does not exhaust a maximum of two - the case no card could reach before 8.6.");
            Assert.That(RepeatedActionQuery.CanRepeat(afterTwo, twice), Is.False);
        }

        /// <summary>
        /// The same frame as an instance that is the <paramref name="completedRepeats"/>th repeat of
        /// its chain. Built rather than driven, because what is under test is the bound the count
        /// produces and not the dispatcher that advances it.
        /// </summary>
        private static PendingResolution RepeatedFrame(PendingResolution frame, int completedRepeats) =>
            new PendingResolution(
                frame.ResolutionId,
                frame.SourceCardInstanceId,
                frame.SourceDefinitionReference,
                frame.ActionDefinitionId,
                frame.EffectDefinitionReference,
                frame.ActorId,
                frame.ControllerId,
                ActionInvocationContext.Repeated,
                frame.Continuation,
                repeat: RepeatContinuation.Next(completedRepeats));

        [Test]
        public void ARepeatMaximumBelowOne_AndARepeatDeclarationOnAnyOtherKind_AreRefused()
        {
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                    maximumRepeats: 0),
                Throws.ArgumentException,
                "A repeat that may never ask is the node not being there.");
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("leaf"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.NoteActorV1, maximumRepeats: 2),
                Throws.ArgumentException,
                "A capability leaf is not a repeat, so a maximum on it states a rule nothing reads.");
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("leaf") },
                    repeatTargetBinding: RepeatTargetBinding.SameTarget,
                    boundSelectionNodeId: new EffectNodeId("leaf")),
                Throws.ArgumentException);
        }

        [Test]
        public void ASameTargetRepeat_MustNameASelectionAndAnUnboundOneMustNameNone()
        {
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                    repeatTargetBinding: RepeatTargetBinding.SameTarget),
                Throws.ArgumentException, "A same-target repeat names the selection it binds.");
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                    boundSelectionNodeId: new EffectNodeId("select")),
                Throws.ArgumentException, "and an unbound one names none.");
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                    repeatTargetBinding: RepeatTargetBinding.SameTarget,
                    boundSelectionNodeId: new EffectNodeId("repeat")),
                Throws.ArgumentException, "and neither binds itself.");
        }

        [Test]
        public void ARepeatContinuation_KeepsItsCountAndItsInheritedPieceApart()
        {
            var piece = BoardObjectReference.ForMinion(new MinionId("bound-minion"));

            Assert.That(RepeatContinuation.FirstInstance.IsRepeatedInstance, Is.False);
            Assert.That(RepeatContinuation.FirstInstance.HasBoundTarget, Is.False);
            Assert.That(RepeatContinuation.Next(2).CompletedRepeats, Is.EqualTo(2));
            Assert.That(RepeatContinuation.NextBoundTo(1, piece).BoundTarget, Is.EqualTo(piece));
            Assert.That(
                RepeatContinuation.FirstInstance.Binding(piece).IsRepeatedInstance, Is.False,
                "The piece is recorded while the printed instance is still running - that is the " +
                "moment the selection names it - so recording one is not becoming a repeat.");
            Assert.That(() => RepeatContinuation.Next(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => RepeatContinuation.NextBoundTo(1, default), Throws.ArgumentException,
                "A bound repeat names the piece it binds.");
        }

        [Test]
        public void AFrameAndItsInvocationContext_CannotDisagreeAboutBeingARepeat()
        {
            var printed = Start(RepeatAfterLeaf(EffectFailurePolicy.May, EffectChooserRole.Actor)).Frame;

            Assert.That(
                () => new PendingResolution(
                    printed.ResolutionId,
                    printed.SourceCardInstanceId,
                    printed.SourceDefinitionReference,
                    printed.ActionDefinitionId,
                    printed.EffectDefinitionReference,
                    printed.ActorId,
                    printed.ControllerId,
                    ActionInvocationContext.PrintedPrimary,
                    printed.Continuation,
                    repeat: RepeatContinuation.Next(1)),
                Throws.ArgumentException,
                "The dispatcher's start gate reads the invocation context and the bound reads the " +
                "count; nothing else could catch the two disagreeing.");
        }
    }
}
