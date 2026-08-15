using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.4 end to end through <see cref="IMatchSession"/>: direct Move and Object Push as
    /// authoritative commands, with restore, replay, and retained-prefix Undo.
    ///
    /// The fixture line is real official-map geography rather than a synthetic board, which is what
    /// makes the interesting case free. The Red hero stands on its own spawn point <c>q-2_r-5</c>; the
    /// Blue hero is placed adjacent to its East at <c>q-1_r-5</c>; <c>q0_r-5</c> is playable, and the
    /// next space along that file, <c>q1_r-5</c>, is a declared terrain cell with nothing but off-map
    /// beyond it. So a push of 2 directly away from the Red hero can only travel 1, which is p.18's
    /// "move it as far as possible" against milestone 6.8e's terrain classification, proved on the map
    /// the game is actually played on.
    ///
    /// Board state is read back by deserializing the authoritative snapshot, as milestone 6.2's
    /// component fixture does, because 6.4's non-goals bar a displacement-specific projection property.
    /// </summary>
    public sealed class MatchSessionDisplacementDispatchTests
    {
        private static readonly EffectNodeId MoveBranch = new EffectNodeId("displacement-move-branch");
        private static readonly EffectNodeId PushBranch = new EffectNodeId("displacement-object-push");

        private static readonly ActionDefinitionId DisplacementSkill =
            new ActionDefinitionId("primary-displacement-skill");

        private static readonly HexCoordinate RedAt = new HexCoordinate(-2, -5);
        private static readonly HexCoordinate BlueAt = new HexCoordinate(-1, -5);
        private static readonly HexCoordinate BeyondBlue = new HexCoordinate(0, -5);
        private static readonly HexCoordinate TerrainBeyond = new HexCoordinate(1, -5);

        [Test]
        public void TheDisplacementSkill_OffersItsTwoBranchesInDefinitionOrder()
        {
            var session = StartDisplacementSkill();

            var choice = session.Projection.PendingEffectChoice;

            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.SelectOption));
            Assert.That(choice.ChooserId, Is.EqualTo(session.Projection.SelectedActor.ActorId),
                "The acting hero owns the decision; the resolver never picks for a player.");
            Assert.That(
                choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { MoveBranch, PushBranch }));
        }

        [Test]
        public void TheFixtureLine_IsTheOfficialGeographyTheseCasesAssume()
        {
            // The cases below are only meaningful if the map really is shaped this way, so the shape
            // is asserted rather than trusted.
            var board = RestoreBoard(StartDisplacementSkill());

            Assert.That(board.IsEmptyPlayableSpace(BeyondBlue), Is.True);
            Assert.That(board.IsEmptyPlayableSpace(TerrainBeyond), Is.False,
                "q1_r-5 is a declared terrain cell, so nothing may end up there.");
            Assert.That(
                board.MapDefinition.Spaces.Any(space =>
                    space.Coordinate.Equals(TerrainBeyond) && space.Kind == BoardMapSpaceKind.Terrain),
                Is.True);
            Assert.That(
                board.MapDefinition.Spaces.Any(space => space.Coordinate.Equals(new HexCoordinate(2, -5))),
                Is.False,
                "Nothing exists beyond the terrain on this file, so a permitted push has nowhere further.");
        }

        [Test]
        public void TheObjectPushBranch_MovesTheAdjacentHeroAsFarAsTerrainAllows()
        {
            var session = StartDisplacementSkill();
            var blueId = BlueHero(session);

            ChooseOption(session, PushBranch);

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, blueId), Is.EqualTo(BeyondBlue),
                "A push of 2 travels 1: the second space is terrain, and p.18 moves the target as far " +
                "as possible rather than failing.");
            Assert.That(board.IsEmptyPlayableSpace(BlueAt), Is.True,
                "The pushed hero's former space is free for every occupancy consumer.");
            Assert.That(Coordinate(board, session.Projection.Participants.Heroes
                .Single(hero => !hero.Id.Equals(blueId)).Id), Is.EqualTo(RedAt),
                "The pushing unit does not move.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void APushWithNothingAdjacent_StopsTheActionRatherThanMovingNothingSilently()
        {
            // p.18's zero-space push is a target that cannot travel. No target at all is a different
            // thing, and a mandatory step that cannot happen stops the action (p.10).
            var session = StartDisplacementSkill(blueAt: new HexCoordinate(-3, 10));

            ChooseOption(session, PushBranch);

            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, BlueHero(session)), Is.EqualTo(new HexCoordinate(-3, 10)),
                "Nothing moved.");
        }

        [Test]
        public void TheDirectMoveBranch_RelocatesTheActorThroughItsOwnUpToChoice()
        {
            var session = StartDisplacementSkill();
            var actorId = session.Projection.SelectedActor.ActorId;

            ChooseOption(session, MoveBranch);

            // The `up to` Optional node asks whether to move at all before the capability offers spaces.
            var confirm = session.Projection.PendingEffectChoice;
            Assert.That(confirm, Is.Not.Null);
            Assert.That(confirm.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Accept(session, DirectorCommand.AnswerEffectChoice(confirm.ConfirmInvocation));

            var spaces = session.Projection.PendingEffectChoice;
            Assert.That(spaces, Is.Not.Null, "Which destination is the move's one decision.");
            Assert.That(spaces.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            var chosen = spaces.Candidates[0].Value.Coordinate;
            Assert.That(
                spaces.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.No.Member(RedAt),
                "A direct Move relocates; the actor's own space is not a destination.");

            Accept(session, DirectorCommand.AnswerEffectChoice(spaces.SelectInvocations[0]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, actorId), Is.EqualTo(chosen));
            Assert.That(board.IsEmptyPlayableSpace(RedAt), Is.True);
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void DecliningTheUpToMove_CompletesTheSkillWithoutMoving()
        {
            var session = StartDisplacementSkill();
            var actorId = session.Projection.SelectedActor.ActorId;

            ChooseOption(session, MoveBranch);
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.DeclineInvocation));

            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            Assert.That(Coordinate(RestoreBoard(session), actorId), Is.EqualTo(RedAt),
                "`up to` includes not moving, which is a legal result rather than a failure.");
        }

        [Test]
        public void APush_ReplaysAndUndoesDeterministically()
        {
            var session = StartDisplacementSkill();
            var blueId = BlueHero(session);
            var selectInvocation = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(candidate => candidate.Candidate.OptionNodeId.Equals(PushBranch));
            var beforePush = session.Projection.DiagnosticSnapshot;

            Accept(session, DirectorCommand.AnswerEffectChoice(selectInvocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterPush = session.Projection.DiagnosticSnapshot;

            Assert.That(afterPush, Is.Not.EqualTo(beforePush));
            Assert.That(Coordinate(RestoreBoard(session), blueId), Is.EqualTo(BeyondBlue));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(Coordinate(RestoreBoard(session), blueId), Is.EqualTo(BlueAt),
                "Retained-prefix Undo puts the pushed hero back, because the displacement is " +
                "authoritative board state and not a side effect the replay cannot reach.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterPush),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void APush_SurvivesSnapshotRestoreByteIdentically()
        {
            var session = StartDisplacementSkill();
            ChooseOption(session, PushBranch);

            var snapshot = session.Projection.AuthoritativeSnapshot;
            var restored = Restore(session);

            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(snapshot),
                "A committed displacement round-trips with no field the writer and reader disagree on.");
        }

        [Test]
        public void ADisplacement_NeverRecordsALanePushRequest()
        {
            // p.18: "Displacing a minion miniature (by moving, pushing, or swapping it) has no impact
            // on the Minion Battle." The request family exists and is recorded elsewhere, so this is a
            // real absence rather than an untested one.
            var session = StartDisplacementSkill();
            ChooseOption(session, PushBranch);

            Assert.That(Restore(session).Round.PendingLanePushRequest, Is.Null);
        }

        [Test]
        public void NoOrdinaryConfiguredCard_ExposesADisplacementCapability()
        {
            // Milestone 6.4's non-goals bar configured live exposure before 6.4a, and its fifth
            // acceptance check requires that no live action expose an uncorrected displaced-minion
            // endpoint. The ordinary hand is the one a player holds outside this fixture.
            var ordinary = PlaceholderOneVsOneTestConfiguration.Create();

            var displacementNodes = ordinary.CardDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Where(node =>
                    node.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities.DirectMoveV1) ||
                    node.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities.ObjectPushV1))
                .ToArray();

            Assert.That(displacementNodes, Is.Empty,
                "Direct Move and Object Push reach a player only through the Skill fixture until " +
                "milestone 6.4a's after-action correction exists.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static HeroId BlueHero(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero => session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                .Id;

        private static HexCoordinate Coordinate(AdjacentMoveAuthority board, HeroId heroId) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        private static MatchSession StartDisplacementSkill(HexCoordinate? blueAt = null)
        {
            var session = CreateDisplacementSession(blueAt ?? BlueAt);
            CommitDisplacementCards(session);
            var offer = session.Projection.AvailableTurnActions
                .SingleOrDefault(candidate => candidate.ActionDefinitionId.Equals(DisplacementSkill));
            Assert.That(offer.ActionDefinitionId, Is.EqualTo(DisplacementSkill),
                "The displacement Skill must be the current actor's offer.");
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        private static void ChooseOption(MatchSession session, EffectNodeId option)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Equals(option));
            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        /// <summary>
        /// Commits each side's displacement Skill card. Red's carries the highest Initiative in the
        /// fixture hand and Blue's the lowest, so the Red hero reliably acts first.
        /// </summary>
        private static void CommitDisplacementCards(MatchSession session)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));

            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("shove") &&
                    candidate.State.Location == CardLocation.Hand);
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static AdjacentMoveAuthority RestoreBoard(MatchSession session) => Restore(session).Board;

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static MatchSession CreateDisplacementSession(HexCoordinate blueAt)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
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
                    new HeroOccupancy(redHeroId, RedAt),
                    new HeroOccupancy(blueHeroId, blueAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
