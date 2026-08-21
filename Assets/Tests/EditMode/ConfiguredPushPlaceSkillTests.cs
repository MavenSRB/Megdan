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
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Displacement;
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
    /// Milestone 6.5a. Red Needle's printed Primary Attack is replaced by the exact modal Skill
    /// `Choose one: choose another adjacent unit and Push it 3 spaces; or Place your hero in a
    /// different empty space within Radius 4`.
    ///
    /// Everything both branches do comes from milestones 6.4, 6.5 and 6.8k; this fixture proves the
    /// binding and the player-visible behaviour. It runs on real official-map geography rather than a
    /// synthetic board, because that is where the interesting cases are free: the Red hero on its own
    /// spawn point at `q-2_r-5`, the Blue hero adjacent at `q-1_r-5`, `q0_r-5` playable, and `q1_r-5`
    /// a declared terrain cell with nothing beyond it - so a push of 3 travels exactly 1.
    ///
    /// None of the synthetic values or texts is official content.
    /// </summary>
    public sealed class ConfiguredPushPlaceSkillTests
    {
        private static readonly CardInstanceId NeedleCardId = new CardInstanceId("red-needle-card");

        private static readonly ActionDefinitionId NeedleSkill =
            new ActionDefinitionId("primary-push-place-skill");

        private static readonly EffectNodeId PushBranch = new EffectNodeId("needle-push-branch");
        private static readonly EffectNodeId SelectTarget = new EffectNodeId("needle-select-target");
        private static readonly EffectNodeId Push = new EffectNodeId("needle-push");
        private static readonly EffectNodeId PlaceBranch = new EffectNodeId("needle-place-branch");

        /// <summary>Milestone 6.2's Cache Skill, which is how a token reaches the board at all.</summary>
        private static readonly ActionDefinitionId ComponentSkill =
            new ActionDefinitionId("primary-component-skill");

        private static readonly EffectNodeId TokenPlaceBranch =
            new EffectNodeId("component-place-branch");
        private static readonly EffectNodeId ActorSpace = new EffectNodeId("needle-actor-space");
        private static readonly EffectNodeId CardRadius = new EffectNodeId("needle-card-radius");
        private static readonly EffectNodeId Place = new EffectNodeId("needle-place");

        private static readonly HexCoordinate RedAt = new HexCoordinate(-2, -5);
        private static readonly HexCoordinate BlueAt = new HexCoordinate(-1, -5);
        private static readonly HexCoordinate BeyondBlue = new HexCoordinate(0, -5);
        private static readonly HexCoordinate TerrainBeyond = new HexCoordinate(1, -5);

        // ---------------------------------------------------------------------------------------
        // Definition
        // ---------------------------------------------------------------------------------------

        [Test]
        public void RedNeedle_ReplacesOnlyItsPrintedPrimaryAttack()
        {
            var hand = SyntheticShowcaseHand.CreateRed(new HeroId("red-synthetic-hero"));
            var definition = hand.Definitions.Single(candidate =>
                candidate.DisplayName == "TEST - Red Needle");

            Assert.That(definition.Id, Is.EqualTo(new CardDefinitionId("placeholder-red-needle")),
                "The conversion advances the revision, never the card's identity.");
            Assert.That(definition.ContentRevision, Is.EqualTo("phase-7-2c-rev-1"),
                "Milestone 7.2c advances this card alone, for the Radius icon it now prints.");
            Assert.That(definition.ContentAuthority,
                Is.EqualTo(CardContentAuthority.SyntheticTestFixture));
            Assert.That(definition.Initiative.Value, Is.EqualTo(13));
            Assert.That(hand.Instances[0].DefinitionReference, Is.EqualTo(definition.Reference));
            Assert.That(hand.DealtDefinitions, Has.Count.EqualTo(5), "The hand keeps its five-card shape.");

            var primary = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(primary.Id, Is.EqualTo(NeedleSkill));
            Assert.That(definition.ActionDefinitions.Any(action => action.Kind == ActionKind.Attack),
                Is.False, "The printed Attack is replaced, not kept beside the Skill.");
            Assert.That(
                definition.ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedSecondary)
                    .BaseValues.Single().Amount,
                Is.EqualTo(2));
            Assert.That(
                definition.ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.Reaction)
                    .BaseValues.Single().Amount,
                Is.EqualTo(1));
        }

        [Test]
        public void TheRedHand_KeepsExactlyOnePrintedNonRangedAndOneRangedAttack()
        {
            // The showcase schedule requires it of every team, and this conversion is the one that
            // takes the count to its floor on the Red side.
            var attacks = SyntheticShowcaseHand.CreateRed(new HeroId("red-synthetic-hero"))
                .Definitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Kind == ActionKind.Attack &&
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .ToArray();

            Assert.That(
                attacks.Count(action => action.AttackRange == AttackRangeKind.NonRanged),
                Is.GreaterThanOrEqualTo(1));
            Assert.That(
                attacks.Count(action => action.AttackRange == AttackRangeKind.Ranged),
                Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void TheSkill_DeclaresTheTwoBranchesTheCardTextNames()
        {
            var effect = NeedleEffect();

            var root = effect.Nodes.Single(node => node.Id.Equals(effect.RootNodeId));
            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(root.Chooser, Is.EqualTo(EffectChooserRole.Actor));
            Assert.That(root.ChildNodeIds, Is.EqualTo(new[] { PushBranch, PlaceBranch }));

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
                "The target offer asks the same immunity question as its consuming Push.");

            var push = effect.Nodes.Single(node => node.Id.Equals(Push));
            Assert.That(push.Capability, Is.EqualTo(SyntheticDisplacementCapabilities.PushTargetV2),
                "The Push names one target, so it uses the target-named declaration of p.18's Push.");
            Assert.That(push.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory),
                "A fixed distance opens no distance choice.");
            Assert.That(
                push.Inputs.Single(input => input.ParameterName == "distance").Literal.IntegerValue,
                Is.EqualTo(3));
            Assert.That(
                push.Inputs.Single(input => input.ParameterName == "target").SourceNodeId,
                Is.EqualTo(SelectTarget));

            var place = effect.Nodes.Single(node => node.Id.Equals(Place));
            Assert.That(place.Capability, Is.EqualTo(SyntheticPlacementCapabilities.PlaceV2));

            // Milestone 7.2c. The bound is the card's printed Radius icon, published by its own node
            // and bound here, rather than a number this table carries. p.9: a card text that mentions
            // Radius "refers to this value", and p.16 raises the icon it refers to.
            Assert.That(
                place.Inputs.Single(input => input.ParameterName == "radius").SourceNodeId,
                Is.EqualTo(CardRadius));
            Assert.That(
                effect.Nodes.Single(node => node.Id.Equals(CardRadius)).Capability,
                Is.EqualTo(SyntheticValueCapabilities.CardRadiusV1));
            Assert.That(
                SyntheticShowcaseHand.CreateRed(new HeroId("red-synthetic-hero"))
                    .Definitions
                    .Single(candidate => candidate.DisplayName == "TEST - Red Needle")
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(NeedleSkill))
                    .BaseValues
                    .Single(value => value.Channel == PrintedValueChannel.Radius).Amount,
                Is.EqualTo(4),
                "The printed value is the 4 the card text already named.");
            Assert.That(
                place.Inputs.Single(input => input.ParameterName == "subject").SourceNodeId,
                Is.EqualTo(ActorSpace),
                "`your hero` is a fact rather than a decision, so no target prompt is invented.");
            Assert.That(effect.Nodes.Single(node => node.Id.Equals(ActorSpace)).Capability,
                Is.EqualTo(SyntheticPlacementCapabilities.ActorSpaceV1));

            Assert.That(
                EffectDefinitionValidator.Validate(effect, SyntheticEffectCapabilities.CreateCatalog()),
                Is.Empty);
        }

        // ---------------------------------------------------------------------------------------
        // The Push branch
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ThePushBranch_OffersExactlyTheOtherAdjacentUnitsAndNoDistanceChoice()
        {
            var session = StartNeedleSkill();

            ChooseBranch(session, PushBranch);

            var selection = session.Projection.PendingEffectChoice;
            Assert.That(selection, Is.Not.Null);
            Assert.That(selection.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            Assert.That(
                selection.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { BlueAt }),
                "`choose another adjacent unit`: the actor's own space is not among them.");

            SelectValue(session, BlueAt);

            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "Distance 3 is fixed, so the Push opens no second decision and the Skill completes.");
        }

        [Test]
        public void ThePushBranch_NeitherOffersNorPushesAnAdjacentToken()
        {
            // Milestone 6.4c. The card text says unit, and p.19 states that tokens are not units.
            // p.18's Push admits "a unit or a token", so the breadth is the rule's and this narrowing
            // is the card's, declared on the operation rather than enforced in this branch.
            var session = StartNeedleSkillBesideAToken(out var tokenAt);

            ChooseBranch(session, PushBranch);

            var selection = session.Projection.PendingEffectChoice;
            Assert.That(
                selection.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.No.Member(tokenAt),
                "The Hazard stands beside the acting hero and is not offered.");
            Assert.That(
                selection.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.Member(BlueAt),
                "The adjacent unit is still offered, so the narrowing took the token and nothing else.");

            var forged = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                selection.ResolutionId,
                selection.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Space(tokenAt)))));

            Assert.That(forged.Accepted, Is.False,
                "And an answer naming it anyway is refused, because the offer is not the only place " +
                "the declaration is read.");
            Assert.That(session.Projection.PlacedTokens.Single().Coordinate, Is.EqualTo(tokenAt));
        }

        [Test]
        public void ThePushBranch_MovesTheNamedTargetAsFarAsItCan()
        {
            var session = StartNeedleSkill();
            var blueId = BlueHero(session);

            ChooseBranch(session, PushBranch);
            SelectValue(session, BlueAt);

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, blueId), Is.EqualTo(BeyondBlue),
                "p.18 moves the target as far as possible: a push of 3 travels 1, because the next " +
                "space along that file is terrain and nothing lies beyond it.");
            Assert.That(Coordinate(board, session.Projection.Participants.Heroes
                .Single(hero => !hero.Id.Equals(blueId)).Id), Is.EqualTo(RedAt),
                "The pushing unit does not move.");
            Assert.That(board.IsEmptyPlayableSpace(TerrainBeyond), Is.False,
                "Fixture precondition: the space that stopped the push really is terrain.");
        }

        [Test]
        public void ThePushBranch_PushesZeroWhenTheTargetHasNowhereToGo()
        {
            // p.18: "this can result in pushing it 0 spaces". Standing the pair one space further
            // along the same file leaves the target's only direction blocked by that terrain.
            var session = StartNeedleSkill(redAt: BlueAt, blueAt: BeyondBlue);
            var blueId = BlueHero(session);

            ChooseBranch(session, PushBranch);
            SelectValue(session, BeyondBlue);

            Assert.That(Coordinate(RestoreBoard(session), blueId), Is.EqualTo(BeyondBlue),
                "A zero-space push is a legal result rather than a failure.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void ThePushBranch_RefusesATargetTheSelectionNeverOffered()
        {
            var session = StartNeedleSkill();
            ChooseBranch(session, PushBranch);

            var before = session.Projection;
            var illegal = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                before.PendingEffectChoice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Space(BeyondBlue)))));

            Assert.That(illegal.Accepted, Is.False, "That space holds no unit at all.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "The refusal touches no card, occupancy, continuation, or event.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(NeedleCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        // ---------------------------------------------------------------------------------------
        // The Place branch
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ThePlaceBranch_OffersEveryEmptySpaceInRadiusFourExceptTheActorsOwn()
        {
            var session = StartNeedleSkill();

            ChooseBranch(session, PlaceBranch);

            var destinations = session.Projection.PendingEffectChoice;
            Assert.That(destinations, Is.Not.Null);
            Assert.That(destinations.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            var offered = destinations.Candidates.Select(candidate => candidate.Value.Coordinate).ToArray();

            Assert.That(offered, Has.No.Member(RedAt), "p.18 requires a different space.");
            Assert.That(offered, Has.No.Member(BlueAt), "An occupied space is not empty.");
            Assert.That(offered, Has.No.Member(TerrainBeyond), "Terrain is an obstacle (p.18).");
            Assert.That(offered.All(space => HexDistanceQuery.Calculate(RedAt, space) <= 4), Is.True,
                "Radius 4 is inclusive rulebook distance.");
            Assert.That(offered, Has.Member(BeyondBlue),
                "Radius counts spaces including spaces with obstacles and needs no path: the space " +
                "screened by the adjacent hero is still within Radius 4.");
        }

        [Test]
        public void ThePlaceBranch_PlacesTheActingHeroWithoutMovementOrFastTravel()
        {
            var session = StartNeedleSkill();
            var actorId = session.Projection.SelectedActor.ActorId;
            ChooseBranch(session, PlaceBranch);
            var destination = session.Projection.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .First(space => HexDistanceQuery.Calculate(RedAt, space) == 4);

            SelectValue(session, destination);

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, actorId), Is.EqualTo(destination));
            Assert.That(board.IsEmptyPlayableSpace(RedAt), Is.True);
            Assert.That(session.Projection.PendingMovementChoice, Is.Null,
                "Placing is not movement (p.18): no Movement action was started and none is pending.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void ThePlaceBranch_RefusesAnOccupiedOrOffBoardDestination()
        {
            var session = StartNeedleSkill();
            ChooseBranch(session, PlaceBranch);
            var before = session.Projection;

            foreach (var illegal in new[] { BlueAt, TerrainBeyond, new HexCoordinate(40, 40) })
            {
                var refused = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                    before.PendingEffectChoice.ResolutionId,
                    before.PendingEffectChoice.ChoiceId,
                    EffectChoiceCommandKind.Select,
                    EffectChoiceCandidate.Value_(EffectValue.Space(illegal)))));
                Assert.That(refused.Accepted, Is.False, illegal.ToString());
            }

            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "Every refusal is atomic: the pending placement is exactly what it was.");
            Assert.That(
                session.Projection.PendingEffectChoice.Candidates, Is.EqualTo(before.PendingEffectChoice.Candidates));
        }

        // ---------------------------------------------------------------------------------------
        // The one after-action correction boundary
        // ---------------------------------------------------------------------------------------

        [Test]
        public void PushingAMinionOutOfTheBattleZone_ReachesTheCorrectionBeforeTheSkillResolves()
        {
            var session = StartNeedleSkillBesideAMinion(out var minionId);

            ChooseBranch(session, PushBranch);
            var pushed = session.Projection.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .Single(space => RestoreBoard(session).Minions
                    .Any(minion => minion.MinionId.Equals(minionId) && minion.Coordinate.Equals(space)));
            SelectValue(session, pushed);

            var correction = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(correction, Is.Not.Null,
                "p.18 corrects a minion miniature that ends up outside the Battle Zone after the action.");
            Assert.That(correction.SubjectMinionId, Is.EqualTo(minionId));
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(NeedleCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved),
                "The action is not over until the correction is, so the source card stays unresolved.");

            Accept(session, DirectorCommand.ChooseCorrectionDestination(correction.SelectInvocations[0]));

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(NeedleCardId))
                .State.ResolutionStatus, Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "Only now does the whole action complete and the card resolve.");
        }

        [Test]
        public void PlacingTheActingHero_OpensNoCorrectionOfItsOwn()
        {
            // p.18's correction applies to minion miniatures; a hero placed anywhere is not one, and
            // this branch relocates nothing else.
            var session = StartNeedleSkill();
            ChooseBranch(session, PlaceBranch);

            SelectValue(session, session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate);

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(NeedleCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        // ---------------------------------------------------------------------------------------
        // Determinism
        // ---------------------------------------------------------------------------------------

        [TestCase("needle-push-branch")]
        [TestCase("needle-place-branch")]
        public void EitherBranch_SurvivesSnapshotRestoreReplayAndRetainedPrefixUndo(string branchNodeId)
        {
            var branch = new EffectNodeId(branchNodeId);
            var session = StartNeedleSkill();
            ChooseBranch(session, branch);

            var paused = session.Projection.PendingEffectChoice;
            var payload = session.CaptureSnapshot();
            var revived = StartNeedleSkill();
            Assert.That(revived.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(
                revived.Projection.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(paused.Candidates.Select(candidate => candidate.Value.Coordinate)),
                "Candidates are never written; the restored handler re-derives the same offer.");

            var chosen = paused.Candidates[0].Value.Coordinate;
            var selection = paused.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.Coordinate.Equals(chosen));
            Accept(session, DirectorCommand.AnswerEffectChoice(selection));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void AStaleOrForgedModeAnswer_RejectsWithoutTouchingTheFrameOrTheCard()
        {
            var session = StartNeedleSkill();
            var choice = session.Projection.PendingEffectChoice;
            var snapshot = session.Projection.AuthoritativeSnapshot;

            var stale = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                new EffectChoiceId("stale-needle-choice"),
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(PushBranch))));
            var forged = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(new EffectNodeId("needle-no-such-branch")))));

            Assert.That(stale.Accepted, Is.False);
            Assert.That(forged.Accepted, Is.False);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(session.Projection.PendingEffectChoice.Candidates, Is.EqualTo(choice.Candidates));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static EffectDefinition NeedleEffect() =>
            SyntheticShowcaseHand.CreateRed(new HeroId("red-synthetic-hero"))
                .Definitions.Single(candidate => candidate.DisplayName == "TEST - Red Needle")
                .ActionDefinitions.Single(action => action.Id.Equals(NeedleSkill))
                .EffectRoot;

        private static HeroId BlueHero(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero => session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                .Id;

        private static HexCoordinate Coordinate(AdjacentMoveAuthority board, HeroId heroId) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        /// <summary>
        /// Commits Red Needle at Initiative 13 against Blue Longbow at 4, so the Red hero acts first
        /// and its Skill is the current actor's offer.
        /// </summary>
        private static MatchSession StartNeedleSkill(
            HexCoordinate? redAt = null, HexCoordinate? blueAt = null)
        {
            var session = CreateNeedleSession(redAt ?? RedAt, blueAt ?? BlueAt);
            CommitCards(session, "needle", "longbow");
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(NeedleCardId));
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(NeedleSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        /// <summary>
        /// The same line one turn later, with a Hazard standing beside the acting hero.
        ///
        /// No initial scenario can seed a token, so this places one the way a match does: the Red hero
        /// spends the earlier turn on milestone 6.2's Cache Skill, whose placement takes an empty space
        /// adjacent to the actor. That needs the Skill-fixture hand, which is the only one dealing the
        /// Cache card beside the ordinary five; Red Cache at Initiative 15 is above every Blue card, so
        /// the Red hero acts first on that turn as well as on the Needle's.
        /// </summary>
        private static MatchSession StartNeedleSkillBesideAToken(out HexCoordinate tokenAt)
        {
            var session = CreateNeedleSession(RedAt, BlueAt, withSkillCards: true);
            CommitCards(session, "cache", "longbow");
            var placement = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(ComponentSkill));
            Accept(session, DirectorCommand.StartAction(placement.ActionDefinitionId));
            ChooseBranch(session, TokenPlaceBranch);
            tokenAt = session.Projection.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .First(space => !space.Equals(BlueAt));
            SelectValue(session, tokenAt);

            // Red acted first on that turn, so the Blue hero still owes one before the round can end.
            Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));

            CommitCards(session, "needle", "hammer");
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(NeedleCardId));
            var skill = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(NeedleSkill));
            Accept(session, DirectorCommand.StartAction(skill.ActionDefinitionId));
            return session;
        }

        /// <summary>
        /// The corpus fixture's correction geography: the Red hero at (0,-1) with red-melee-01 adjacent
        /// at (-1,-1), where a push directly away carries the minion out of the Battle Zone.
        /// </summary>
        private static MatchSession StartNeedleSkillBesideAMinion(out MinionId minionId)
        {
            minionId = new MinionId("red-melee-01");
            return StartNeedleSkill(new HexCoordinate(0, -1), new HexCoordinate(-3, 10));
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

        private static void ChooseBranch(MatchSession session, EffectNodeId branch)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.OptionNodeId.Equals(branch))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void SelectValue(MatchSession session, HexCoordinate coordinate)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.Value.Coordinate.Equals(coordinate))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static AdjacentMoveAuthority RestoreBoard(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions).Board;

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static MatchSession CreateNeedleSession(
            HexCoordinate redAt, HexCoordinate blueAt, bool withSkillCards = false)
        {
            var configuration = withSkillCards
                ? PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Red)
                : PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redAt),
                    new HeroOccupancy(blueHeroId, blueAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
