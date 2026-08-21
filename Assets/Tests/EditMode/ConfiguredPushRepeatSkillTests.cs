using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.7a. Blue Longbow's printed Primary Attack is replaced by the Skill
    /// `Choose another adjacent unit and Push it 1 space; you may Repeat this action once`.
    ///
    /// Everything the card does comes from milestones 6.5a, 6.8k, 6.8l and 6.7; what this fixture
    /// proves is the binding and the player-visible behaviour of a bounded Repeat on a configured
    /// card. <see cref="MatchSessionRepeatDispatchTests"/> owns the general dispatch contract on the
    /// fixture hand's Repeat card, and <see cref="BoundedRepeatActionTests"/> owns the frame contract;
    /// what is here is what the conversion adds, which is a repeat whose subject is a single named
    /// target rather than a mode choice.
    ///
    /// It runs on real official-map geography, because the Battle Zone the correction between the two
    /// instances depends on is map content and a synthetic coordinate board declares none. Four
    /// positions carry the whole file, each chosen so its case is deterministic:
    ///
    /// - (0,-1) stands beside two pushable Red minions and one Blue Heavy, so the first instance has a
    ///   real choice, the Heavy's standing immunity is visibly filtered out of it, and the repeated
    ///   instance has a different unit left to name.
    /// - (0,-2) stands beside exactly one, whose push carries it out of reach, so the repeated instance
    ///   has nothing legal left at all.
    /// - (-1,-2) stands beside one whose push is blocked by the minion behind it, so p.18's zero-space
    ///   push leaves the subject adjacent and legally nameable a second time.
    /// - (-7,4) stands beside one whose push carries it out of the Battle Zone with two equally short
    ///   returns, so p.18's after-action correction really pauses between the instances.
    ///
    /// Blue Longbow at Initiative 4 sits below every Red card, so the Red hero always acts first; each
    /// fixture spends that turn on p.12's Hold, which is what an ordinary match does with a turn it
    /// does not want to spend.
    ///
    /// None of the synthetic values or texts is official content.
    /// </summary>
    public sealed class ConfiguredPushRepeatSkillTests
    {
        private static readonly CardInstanceId LongbowCardId = new CardInstanceId("blue-longbow-card");

        private static readonly ActionDefinitionId LongbowSkill =
            new ActionDefinitionId("primary-push-repeat-skill");

        private static readonly EffectNodeId SelectTarget = new EffectNodeId("longbow-select-target");
        private static readonly EffectNodeId Push = new EffectNodeId("longbow-push");
        private static readonly EffectNodeId Repeat = new EffectNodeId("longbow-repeat");

        /// <summary>Milestone 6.2's Cache Skill, which is how a token reaches the board at all.</summary>
        private static readonly ActionDefinitionId ComponentSkill =
            new ActionDefinitionId("primary-component-skill");

        private static readonly EffectNodeId TokenPlaceBranch =
            new EffectNodeId("component-place-branch");

        /// <summary>Beside red-melee-01, red-melee-02, and the immune blue-heavy-01.</summary>
        private static readonly HexCoordinate BesideTwo = new HexCoordinate(0, -1);

        /// <summary>Beside red-melee-01 alone, whose push of 1 carries it out of reach.</summary>
        private static readonly HexCoordinate BesideOne = new HexCoordinate(0, -2);

        /// <summary>Beside red-melee-01 alone, with red-melee-02 directly behind it.</summary>
        private static readonly HexCoordinate BesideABlockedOne = new HexCoordinate(-1, -2);

        /// <summary>Beside red-melee-03 alone, whose push of 1 leaves the Battle Zone.</summary>
        private static readonly HexCoordinate BesideAStray = new HexCoordinate(-7, 4);

        /// <summary>The far corner the Blue hero stands in when the case is about standing alone.</summary>
        private static readonly HexCoordinate Alone = new HexCoordinate(-3, 10);

        /// <summary>The Red hero's own spawn point, well away from the Battle Zone and from anything.</summary>
        private static readonly HexCoordinate Elsewhere = new HexCoordinate(-2, -5);

        private static readonly HexCoordinate FirstMinionAt = new HexCoordinate(-1, -1);
        private static readonly HexCoordinate SecondMinionAt = new HexCoordinate(-1, 0);
        private static readonly HexCoordinate HeavyAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate PushedFromBesideTwo = new HexCoordinate(-2, -1);
        private static readonly HexCoordinate PushedFromBesideOne = new HexCoordinate(-2, 0);
        private static readonly HexCoordinate StrayAt = new HexCoordinate(-8, 4);
        private static readonly HexCoordinate StrayPushedTo = new HexCoordinate(-9, 4);

        /// <summary>The correction return that leaves the stray out of the actor's reach.</summary>
        private static readonly HexCoordinate ReturnedAway = new HexCoordinate(-9, 5);

        /// <summary>The correction return that puts the stray back beside the actor.</summary>
        private static readonly HexCoordinate ReturnedBeside = new HexCoordinate(-8, 4);

        // ---------------------------------------------------------------------------------------
        // Definition
        // ---------------------------------------------------------------------------------------

        [Test]
        public void BlueLongbow_ReplacesOnlyItsPrintedPrimaryAttack()
        {
            var hand = SyntheticShowcaseHand.CreateBlue(new HeroId("blue-synthetic-hero"));
            var definition = hand.Definitions.Single(candidate =>
                candidate.DisplayName == "TEST - Blue Longbow");

            Assert.That(definition.Id, Is.EqualTo(new CardDefinitionId("placeholder-blue-longbow")),
                "The conversion advances the revision, never the card's identity.");
            Assert.That(definition.ContentRevision, Is.EqualTo("phase-7-1-rev-1"));
            Assert.That(definition.ContentAuthority,
                Is.EqualTo(CardContentAuthority.SyntheticTestFixture));
            Assert.That(definition.Initiative.Value, Is.EqualTo(4));
            Assert.That(hand.Instances[4].DefinitionReference, Is.EqualTo(definition.Reference),
                "It keeps its five-card-hand slot as well as its identity.");
            Assert.That(hand.DealtDefinitions, Has.Count.EqualTo(5), "The hand keeps its five-card shape.");

            var primary = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(primary.Id, Is.EqualTo(LongbowSkill));
            Assert.That(primary.BaseValues, Is.Empty,
                "A Skill prints no value channel, so the replaced Ranged Attack says nothing - " +
                "neither its Attack value nor its Range.");
            Assert.That(definition.ActionDefinitions.Any(action => action.Kind == ActionKind.Attack),
                Is.False, "The printed Attack is replaced, not kept beside the Skill.");
            Assert.That(
                definition.ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedSecondary)
                    .BaseValues.Single().Amount,
                Is.EqualTo(3));
            Assert.That(
                definition.ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.Reaction)
                    .BaseValues.Single().Amount,
                Is.EqualTo(2),
                "The Defense reaction is untouched, which is what keeps every fixture that commits " +
                "this card to lose the turn or to react on Defense working across the conversion.");
        }

        [Test]
        public void TheBlueHand_KeepsExactlyOnePrintedNonRangedAndOneRangedAttack()
        {
            // This is the last of the six showcase conversions, so both hands now sit at the floor the
            // schedule requires rather than merely above it.
            var attacks = SyntheticShowcaseHand.CreateBlue(new HeroId("blue-synthetic-hero"))
                .Definitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Kind == ActionKind.Attack &&
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .ToArray();

            Assert.That(
                attacks.Count(action => action.AttackRange == AttackRangeKind.NonRanged), Is.EqualTo(1),
                "Blue Hammer is the team's one printed non-Ranged Attack.");
            Assert.That(
                attacks.Count(action => action.AttackRange == AttackRangeKind.Ranged), Is.EqualTo(1),
                "Blue Scout Shot is the team's one printed Ranged Attack.");
        }

        [Test]
        public void TheSkill_DeclaresTheOneThingTheCardTextNamesAndThenTheRepeat()
        {
            var effect = LongbowEffect();

            var root = effect.Nodes.Single(node => node.Id.Equals(effect.RootNodeId));
            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Sequence),
                "This card text names one thing to do rather than alternatives, so there is no " +
                "`Choose` and no mode.");
            Assert.That(root.ChildNodeIds, Is.EqualTo(new[] { SelectTarget, Push, Repeat }),
                "p.10 puts the repeat after the action it repeats, so it is the root's last child.");

            var selectTarget = effect.Nodes.Single(node => node.Id.Equals(SelectTarget));
            Assert.That(selectTarget.Capability,
                Is.EqualTo(SyntheticTargetingCapabilities.ChooseTargetV4));
            Assert.That(
                selectTarget.Inputs.Single(input => input.ParameterName == "range").Literal.IntegerValue,
                Is.EqualTo(1), "`another adjacent unit` is Radius 1.");
            Assert.That(
                selectTarget.Inputs.Single(input => input.ParameterName == "includeSelf")
                    .Literal.BooleanValue,
                Is.False, "`another` excludes the acting hero.");
            Assert.That(
                (RuleActivity)selectTarget.Inputs.Single(input => input.ParameterName == "activity")
                    .Literal.IntegerValue,
                Is.EqualTo(RuleActivity.ObjectPush),
                "Card-text Push has an activity of its own, so the offer asks the same immunity " +
                "question its consuming Push will ask.");
            Assert.That(
                selectTarget.Inputs
                    .Single(input => input.ParameterName == EffectSelectionDeclaration.ConsumerParameterName)
                    .Literal.NodeId,
                Is.EqualTo(Push),
                "The selection declares the node it selects for, so only objects that Push can take " +
                "are offered.");

            var push = effect.Nodes.Single(node => node.Id.Equals(Push));
            Assert.That(push.Capability, Is.EqualTo(SyntheticDisplacementCapabilities.PushTargetV2),
                "The Push names one target, so it uses the target-named declaration of p.18's Push.");
            Assert.That(push.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory),
                "A fixed distance opens no distance choice.");
            Assert.That(
                push.Inputs.Single(input => input.ParameterName == "distance").Literal.IntegerValue,
                Is.EqualTo(1));
            Assert.That(
                push.Inputs.Single(input => input.ParameterName == "ignoresObstacles").Literal.BooleanValue,
                Is.False, "p.18's ordinary transit and endpoint rules apply.");
            Assert.That(
                push.Inputs.Single(input => input.ParameterName == "target").SourceNodeId,
                Is.EqualTo(SelectTarget));

            var repeat = effect.Nodes.Single(node => node.Id.Equals(Repeat));
            Assert.That(repeat.Kind, Is.EqualTo(EffectNodeKind.RepeatAction));
            Assert.That(repeat.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May),
                "`you may Repeat` is the node's own policy; `once` is the node kind's structural " +
                "bound rather than a count this card states.");
            Assert.That(repeat.Chooser, Is.EqualTo(EffectChooserRole.Actor));
            Assert.That(repeat.Capability.IsDefined, Is.False, "A Repeat performs nothing itself.");
            Assert.That(repeat.ChildNodeIds, Is.Empty);

            Assert.That(
                EffectDefinitionValidator.Validate(effect, SyntheticEffectCapabilities.CreateCatalog()),
                Is.Empty);
        }

        // ---------------------------------------------------------------------------------------
        // The ordinary instance
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheOrdinaryInstance_OffersOnlyTheAdjacentUnitsThePushCanTake()
        {
            var session = StartLongbowSkill(BesideTwo);
            var choice = session.Projection.PendingEffectChoice;

            Assert.That(choice.NodeId, Is.EqualTo(SelectTarget));
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EquivalentTo(new[] { FirstMinionAt, SecondMinionAt }),
                "Three objects stand beside the actor and only two are offered: p.18 makes immunity a " +
                "targeting rule, and the Blue Heavy is immune while friendly non-Heavy minions stand " +
                "in the Battle Zone.");
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.No.Member(HeavyAt));
        }

        [Test]
        public void TheOrdinaryInstance_PushesTheNamedUnitOneSpaceAndThenAsksToRepeat()
        {
            var session = StartLongbowSkill(BesideTwo);

            SelectValue(session, FirstMinionAt);

            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(PushedFromBesideTwo),
                "A fixed distance of 1 opens no distance choice: the named unit travels one space " +
                "directly away and the Push resolves in the same command.");
            Assert.That(Coordinate(session, "red-melee-02"), Is.EqualTo(SecondMinionAt),
                "The named declaration of p.18's Push moves the one target it was given.");

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.NodeId, Is.EqualTo(Repeat));
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Assert.That(choice.Candidates, Is.Empty, "A `may` names nothing to pick between.");
            Assert.That(choice.ConfirmInvocation, Is.Not.Null);
            Assert.That(choice.DeclineInvocation, Is.Not.Null);
            Assert.That(choice.ChooserId, Is.EqualTo(BlueHeroId(session)),
                "The acting hero owns the decision the card gives it.");
        }

        [Test]
        public void ADeclinedRepeat_EndsTheActionAtOneInstance()
        {
            var session = StartLongbowSkill(BesideTwo);
            SelectValue(session, FirstMinionAt);

            Decline(session);

            Assert.That(StartedInstances(session), Is.EqualTo(1),
                "Declining is an answer, not a deferral: no second instance is ever created.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            Assert.That(Coordinate(session, "red-melee-02"), Is.EqualTo(SecondMinionAt),
                "Nothing else moved, so the declined repeat cost the board nothing.");
            AssertCardResolved(session);
        }

        // ---------------------------------------------------------------------------------------
        // The repeated instance
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AConfirmedRepeat_StartsASecondInstanceUnderTheRepeatedContext()
        {
            var session = StartLongbowSkill(BesideTwo);
            SelectValue(session, FirstMinionAt);
            var first = session.Projection.PendingSkillResolution.ResolutionId;

            Confirm(session);

            Assert.That(StartedInstances(session), Is.EqualTo(2),
                "Nothing carried this action out of the Battle Zone, so the correction boundary was " +
                "clean and the second instance began inside the same command.");
            var repeated = session.Projection.PendingSkillResolution;
            Assert.That(repeated, Is.Not.Null);
            Assert.That(repeated.InvocationContext, Is.EqualTo(ActionInvocationContext.Repeated),
                "The explicit repeated context is what makes the second instance's own Repeat node " +
                "ineligible; nothing counts instances.");
            Assert.That(repeated.ResolutionId, Is.Not.EqualTo(first),
                "A separate instance carries a separate resolution identity.");
        }

        [Test]
        public void TheRepeatedInstance_NamesADifferentUnitBecauseTheFirstIsOutOfReach()
        {
            var session = StartLongbowSkill(BesideTwo);
            SelectValue(session, FirstMinionAt);
            Confirm(session);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.NodeId, Is.EqualTo(SelectTarget));
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { SecondMinionAt }),
                "The repeated instance derives its candidates from the board as it now stands: a push " +
                "of 1 left the first subject two spaces off, so only the unit the first instance did " +
                "not name is still legal.");

            SelectValue(session, SecondMinionAt);

            Assert.That(Coordinate(session, "red-melee-02"), Is.EqualTo(new HexCoordinate(-2, 1)),
                "The second instance's own Push resolves against current authority.");
            AssertCardResolved(session);
        }

        [Test]
        public void TheRepeatedInstance_AsksEverythingAnOrdinaryOneAsksAndNoMore()
        {
            var session = StartLongbowSkill(BesideTwo);
            SelectValue(session, FirstMinionAt);
            Confirm(session);

            var prompts = new List<string>();
            while (session.Projection.PendingEffectChoice != null)
            {
                var choice = session.Projection.PendingEffectChoice;
                prompts.Add(choice.NodeId.Value);
                SelectValue(session, choice.Candidates[0].Value.Coordinate);
            }

            Assert.That(prompts, Is.EqualTo(new[] { SelectTarget.Value }),
                "The repeated instance opens the one prompt the card names and never its own Repeat: " +
                "an ineligible Repeat is p.10's silent skip rather than a decision offered and then " +
                "refused, which is the whole of `once`.");
            Assert.That(StartedInstances(session), Is.EqualTo(2));
            AssertCardResolved(session);
        }

        [Test]
        public void TheTwoInstances_AreSeparateActionsWithTheirOwnEnvelopes()
        {
            var session = StartLongbowSkill(BesideTwo);
            SelectValue(session, FirstMinionAt);
            Confirm(session);
            SelectValue(session, SecondMinionAt);

            Assert.That(
                session.Projection.DiagnosticEvents
                    .Select(entry => entry.EventType)
                    .Where(type =>
                        type == "Action.SkillActionStarted" || type == "Action.SkillActionCompleted")
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "Action.SkillActionStarted",
                    "Action.SkillActionCompleted",
                    "Action.SkillActionStarted",
                    "Action.SkillActionCompleted"
                }),
                "Two complete action envelopes rather than one action with an extra tail, which is " +
                "p.10's `treat each instance of repeat as a separate action`.");
        }

        [Test]
        public void TheSourceCard_ResolvesOnlyWhenTheLastInstanceCompletes()
        {
            var session = StartLongbowSkill(BesideTwo);
            SelectValue(session, FirstMinionAt);
            AssertCardUnresolved(session);

            Confirm(session);
            AssertCardUnresolved(session);

            SelectValue(session, SecondMinionAt);
            AssertCardResolved(session);
        }

        // ---------------------------------------------------------------------------------------
        // A zero-space push, and a repeated instance with nothing left
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ABlockedPush_LeavesTheSubjectAdjacentAndLegallyNameableAgain()
        {
            var session = StartLongbowSkill(BesideABlockedOne);
            Assert.That(
                session.Projection.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { FirstMinionAt }),
                "p.18 admits a zero-space push as a legal result, so a target that cannot travel is " +
                "still a target and is still offered.");

            SelectValue(session, FirstMinionAt);

            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(FirstMinionAt),
                "The space behind it holds red-melee-02, so the push resolves to zero rather than " +
                "refusing.");

            Confirm(session);

            Assert.That(
                session.Projection.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { FirstMinionAt }),
                "Nothing moved, so the repeated instance may legally name the same subject the first " +
                "one did - the rulebook restricts the repeated instance's target on no card this " +
                "hand prints.");
            SelectValue(session, FirstMinionAt);
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(FirstMinionAt));
            AssertCardResolved(session);
        }

        [Test]
        public void ARepeatedInstanceWithNoLegalTarget_StopsWithoutASecondPush()
        {
            var session = StartLongbowSkill(BesideOne);
            SelectValue(session, FirstMinionAt);
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(PushedFromBesideOne));

            Confirm(session);

            Assert.That(StartedInstances(session), Is.EqualTo(2),
                "The repeat was owed and the boundary was clean, so the instance really began.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "Its one mandatory step has nothing to name, so p.10 stops the action there rather " +
                "than opening a prompt with no answer.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(PushedFromBesideOne),
                "The first instance's work stands; a stopped instance keeps everything already applied.");
            AssertCardResolved(session);
        }

        // ---------------------------------------------------------------------------------------
        // The correction between the instances
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ACorrectionBetweenTheInstances_CompletesBeforeTheSecondBegins()
        {
            var session = StartLongbowSkill(BesideAStray);
            SelectValue(session, StrayAt);
            Assert.That(Coordinate(session, "red-melee-03"), Is.EqualTo(StrayPushedTo));

            Confirm(session);

            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "The instance that asked to repeat has ended; its effect frame is gone.");
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Not.Null,
                "p.18's correction holds the action boundary open, and the repeat waits behind it.");
            Assert.That(StartedInstances(session), Is.EqualTo(1),
                "The next instance has not begun while the correction is unanswered.");

            AnswerCorrection(session, ReturnedAway);

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(StartedInstances(session), Is.EqualTo(2),
                "Once the boundary is clean the second instance begins, inside the same command.");
            Assert.That(
                session.Projection.DiagnosticEvents
                    .Select(entry => entry.EventType)
                    .Where(type =>
                        type == "Action.SkillActionStarted" ||
                        type == "Action.SkillActionCompleted" ||
                        type == "Action.DisplacedMinionCorrectionStarted" ||
                        type == "Action.DisplacedMinionCorrectionCompleted")
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "Action.SkillActionStarted",
                    "Action.SkillActionCompleted",
                    "Action.DisplacedMinionCorrectionStarted",
                    "Action.DisplacedMinionCorrectionCompleted",
                    "Action.SkillActionStarted",
                    "Action.SkillActionCompleted"
                }),
                "The correction of the first instance opens and closes before the second opens. This " +
                "return carried the stray out of the actor's reach, so the second instance opens and " +
                "closes on nothing - which is the whole reason the repeat prompt cannot be answered " +
                "for the player: the board the second instance reads is made by this correction, " +
                "after the prompt was answered.");
            AssertCardResolved(session);
        }

        [Test]
        public void TheCorrectionChoice_DecidesWhatTheRepeatedInstanceFinds()
        {
            var session = StartLongbowSkill(BesideAStray);
            SelectValue(session, StrayAt);
            Confirm(session);

            AnswerCorrection(session, ReturnedBeside);

            Assert.That(Coordinate(session, "red-melee-03"), Is.EqualTo(ReturnedBeside));
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.NodeId, Is.EqualTo(SelectTarget));
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { ReturnedBeside }),
                "The owning team returned the stray to the space it started in, so the repeated " +
                "instance finds it adjacent again and may name it - every target and endpoint read " +
                "from current authority rather than from anything the first instance recorded.");

            SelectValue(session, ReturnedBeside);

            Assert.That(Coordinate(session, "red-melee-03"), Is.EqualTo(StrayPushedTo),
                "The second Push resolves the same geometry against the board as it now stands.");
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Not.Null,
                "The second instance owes no repeat, but it owes its own correction: the boundary " +
                "contract is the same for every instance.");

            AnswerCorrection(session, ReturnedAway);

            Assert.That(StartedInstances(session), Is.EqualTo(2),
                "Two instances and no more: the repeated one is barred from asking again.");
            AssertCardResolved(session);
        }

        [Test]
        public void ACorrectionHoldingThisCardsRepeat_SurvivesSnapshotRestore()
        {
            var session = StartLongbowSkill(BesideAStray);
            SelectValue(session, StrayAt);
            Confirm(session);

            var roundTripped = Restore(session);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(roundTripped.Round, roundTripped.Board),
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A correction holding this card's owed repeat round-trips byte-identically at v12.");

            var payload = session.CaptureSnapshot();
            var restored = StartLongbowSkill(BesideAStray);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(restored.Projection.PendingDisplacedMinionCorrection, Is.Not.Null);
            Assert.That(StartedInstances(restored), Is.EqualTo(1));

            AnswerCorrection(restored, ReturnedBeside);

            Assert.That(StartedInstances(restored), Is.EqualTo(2),
                "The restored boundary still owes the repeat, so answering the correction starts the " +
                "second instance exactly as the live one did.");
            Assert.That(
                restored.Projection.PendingSkillResolution.InvocationContext,
                Is.EqualTo(ActionInvocationContext.Repeated));
        }

        [Test]
        public void ADeclinedRepeatUnderTheSameCorrection_RestoresWithNothingOwed()
        {
            var session = StartLongbowSkill(BesideAStray);
            SelectValue(session, StrayAt);
            Decline(session);

            var payload = session.CaptureSnapshot();
            var restored = StartLongbowSkill(BesideAStray);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            AnswerCorrection(restored, ReturnedBeside);

            Assert.That(StartedInstances(restored), Is.EqualTo(1),
                "The wire distinguishes a correction that owes a repeat from one that does not.");
            AssertCardResolved(restored);
        }

        // ---------------------------------------------------------------------------------------
        // Determinism, refusal, and the offer
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheRepeatedAction_ReplaysAndUndoesDeterministically()
        {
            var session = StartLongbowSkill(BesideAStray);
            SelectValue(session, StrayAt);
            Confirm(session);
            var beforeAnswer = session.Projection.DiagnosticSnapshot;

            AnswerCorrection(session, ReturnedBeside);
            var afterAnswer = session.Projection.DiagnosticSnapshot;
            Assert.That(StartedInstances(session), Is.EqualTo(2));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeAnswer),
                "Retained-prefix Undo reproduces the paused correction with its repeat still owed.");
            Assert.That(StartedInstances(session), Is.EqualTo(1),
                "The second instance is derived work, so undoing the command that started it undoes it.");

            AnswerCorrection(session, ReturnedBeside);

            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterAnswer),
                "Replaying the same answer recreates the same second instance and the same facts.");
        }

        [Test]
        public void AStaleOrForgedRepeatAnswer_RejectsWithoutTouchingTheFrameOrTheCard()
        {
            var session = StartLongbowSkill(BesideTwo);
            SelectValue(session, FirstMinionAt);
            var choice = session.Projection.PendingEffectChoice;
            var snapshot = session.Projection.AuthoritativeSnapshot;

            var stale = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                new EffectChoiceId("stale-longbow-choice"),
                EffectChoiceCommandKind.Confirm)));
            var forged = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(new EffectNodeId("longbow-no-such-node")))));

            Assert.That(stale.Accepted, Is.False);
            Assert.That(forged.Accepted, Is.False);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot),
                "A refused answer leaves the owed decision and the spent card exactly as they stood.");
            Assert.That(session.Projection.PendingEffectChoice.NodeId, Is.EqualTo(Repeat));
            AssertCardUnresolved(session);
        }

        [Test]
        public void WhereNothingIsAdjacent_TheSkillCannotBeStartedAndTheMovementStillCan()
        {
            // Milestone 6.5af. Nothing stands beside the acting hero, so the one step this card names
            // has nothing to name and the Skill is judged before it is offered rather than after it is
            // started. Rulebook p.12: a player is never forced to act, and this card prints a
            // Secondary Movement 3 beside the Skill.
            var session = CreateLongbowTurn(Alone);

            var skill = session.Projection.AvailableTurnActions
                .Single(offer => offer.ActionDefinitionId.Equals(LongbowSkill));
            Assert.That(skill.IsAvailable, Is.False);
            Assert.That(
                session.Projection.AvailableTurnActions
                    .Where(offer => offer.IsAvailable)
                    .Select(offer => offer.ActionDefinitionId),
                Is.Not.Empty,
                "The turn is still spendable; only the Skill is unavailable.");
            Assert.That(session.Execute(DirectorCommand.StartAction(LongbowSkill)).Accepted, Is.False,
                "It cannot be started, so the turn is not spent on it.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Null);
            AssertCardUnresolved(session);
        }

        // ---------------------------------------------------------------------------------------
        // Milestone 6.4c: `another adjacent unit` never names a token
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AnAdjacentToken_IsNeitherOfferedToNorPushedByThisCard()
        {
            // The card text says unit, and p.19 states that tokens are not units. p.18's Push admits
            // "a unit or a token", so the breadth is the rule's and this narrowing is the card's,
            // declared on the operation rather than enforced here.
            var session = StartLongbowSkillBesideAToken(out var tokenAt);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.NodeId, Is.EqualTo(SelectTarget));
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.No.Member(tokenAt),
                "The Hazard stands beside the acting hero and is not offered.");
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EquivalentTo(new[] { FirstMinionAt, SecondMinionAt }),
                "The two adjacent units are still offered, so the narrowing took the token and " +
                "nothing else.");

            var forged = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Space(tokenAt)))));

            Assert.That(forged.Accepted, Is.False,
                "And an answer naming it anyway is refused, because the offer is not the only place " +
                "the declaration is read.");
            Assert.That(
                session.Projection.PlacedTokens.Single().Coordinate, Is.EqualTo(tokenAt),
                "The token has not moved.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static EffectDefinition LongbowEffect() =>
            SyntheticShowcaseHand.CreateBlue(new HeroId("blue-synthetic-hero"))
                .Definitions.Single(candidate => candidate.DisplayName == "TEST - Blue Longbow")
                .ActionDefinitions.Single(action => action.Id.Equals(LongbowSkill))
                .EffectRoot;

        private static HeroId BlueHeroId(MatchSession session) =>
            session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;

        private static HexCoordinate Coordinate(MatchSession session, string minionId) =>
            session.Projection.Minions.Single(minion => minion.MinionId.Value == minionId).Coordinate;

        private static int StartedInstances(MatchSession session) =>
            session.Projection.DiagnosticEvents.Count(entry =>
                entry.EventType == "Action.SkillActionStarted");

        private static CardInstance LongbowCard(MatchSession session) =>
            session.Projection.Cards.Single(card => card.Id.Equals(LongbowCardId));

        private static void AssertCardResolved(MatchSession session) =>
            Assert.That(
                LongbowCard(session).State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "The source card resolves only when the last instance, correction included, completes.");

        private static void AssertCardUnresolved(MatchSession session) =>
            Assert.That(
                LongbowCard(session).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved),
                "One card is spent on the whole action however many instances it runs.");

        /// <summary>
        /// Names a candidate and confirms it where the authority still asks for a confirm. Milestone
        /// 6.5c answers an exactly-one value choice in the select alone, so the confirm is conditional
        /// rather than assumed.
        /// </summary>
        private static void SelectValue(MatchSession session, HexCoordinate coordinate)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                choice.SelectInvocations.Single(candidate =>
                    candidate.Candidate.Value.Coordinate.Equals(coordinate))));
            var confirm = session.Projection.PendingEffectChoice?.ConfirmInvocation;
            if (confirm != null && session.Projection.PendingEffectChoice.NodeId.Equals(choice.NodeId))
            {
                Accept(session, DirectorCommand.AnswerEffectChoice(confirm));
            }
        }

        private static void Confirm(MatchSession session)
        {
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.NodeId, Is.EqualTo(Repeat));
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation));
        }

        private static void Decline(MatchSession session)
        {
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.NodeId, Is.EqualTo(Repeat));
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.DeclineInvocation));
        }

        /// <summary>
        /// Answers the correction with the named return. It is named rather than taken first, because
        /// which return the owning team picks is exactly what decides whether the repeated instance
        /// finds the stray beside the actor or out of reach - the two cases this file separates.
        /// </summary>
        private static void AnswerCorrection(MatchSession session, HexCoordinate destination)
        {
            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null);
            Assert.That(
                pending.SelectInvocations.Select(invocation => invocation.Destination),
                Is.EquivalentTo(new[] { ReturnedAway, ReturnedBeside }),
                "The stray has two equally short returns, which is what makes this correction pause " +
                "for the owning team rather than apply itself.");
            Accept(session, DirectorCommand.ChooseCorrectionDestination(
                pending.SelectInvocations.Single(invocation =>
                    invocation.Destination.Equals(destination))));
        }

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>
        /// The same position one turn later, with a Hazard standing beside the acting hero.
        ///
        /// No initial scenario can seed a token, so this places one the way a match does: the Blue
        /// hero spends the earlier turn on milestone 6.2's Cache Skill, whose placement takes an empty
        /// space adjacent to the actor. That needs the Skill-fixture hand, which is the only one that
        /// deals the Cache card beside the ordinary five.
        /// </summary>
        private static MatchSession StartLongbowSkillBesideAToken(out HexCoordinate tokenAt)
        {
            var session = CreateSession(BesideTwo, withSkillCards: true);
            CommitCards(session, "bombard", "cache");
            Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));

            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(ComponentSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            ChooseMode(session, TokenPlaceBranch);
            tokenAt = session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate;
            SelectValue(session, tokenAt);

            CommitCards(session, "pursuit", "longbow");
            Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(LongbowCardId));
            var skill = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(LongbowSkill));
            Accept(session, DirectorCommand.StartAction(skill.ActionDefinitionId));
            return session;
        }

        /// <summary>A mode keeps milestone 6.8a's explicit confirm, so both invocations are sent.</summary>
        private static void ChooseMode(MatchSession session, EffectNodeId branch)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.OptionNodeId.Equals(branch))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        /// <summary>The Blue hero's turn with Blue Longbow's Skill started and its first prompt open.</summary>
        private static MatchSession StartLongbowSkill(HexCoordinate blueAt)
        {
            var session = CreateLongbowTurn(blueAt);
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(LongbowSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        /// <summary>
        /// The Blue hero's turn, reached without steering anything. Blue Longbow at Initiative 4 sits
        /// below every Red card, so Red Bombard at 5 acts first and spends its turn on p.12's Hold -
        /// which any Action card may do, and which is what a player does with a turn they do not want.
        /// </summary>
        private static MatchSession CreateLongbowTurn(HexCoordinate blueAt)
        {
            var session = CreateSession(blueAt);
            CommitCards(session, "bombard", "longbow");
            Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(LongbowCardId),
                "The hero the board was built around must be the one whose turn this is.");
            return session;
        }

        private static void CommitCards(
            MatchSession session, string redCardFragment, string blueCardFragment)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            foreach (var pair in new[]
            {
                new { Side = TeamSide.Red, Fragment = redCardFragment },
                new { Side = TeamSide.Blue, Fragment = blueCardFragment }
            })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == pair.Side &&
                    candidate.Id.Value.Contains(pair.Fragment) &&
                    candidate.State.Location == CardLocation.Hand);
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "The committed Initiatives differ, so no tie has to be steered.");
        }

        private static MatchSession CreateSession(HexCoordinate blueAt, bool withSkillCards = false)
        {
            var configuration = withSkillCards
                ? PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue)
                : PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var occupancies = setup.Heroes
                .Select(hero => new HeroOccupancy(
                    hero.HeroId, hero.TeamSide == TeamSide.Blue ? blueAt : Elsewhere))
                .ToArray();
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                occupancies,
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
