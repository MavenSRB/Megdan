using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.4a end to end: direct Move and Object Push both exercise the one after-action
    /// correction authority through <see cref="IMatchSession"/> commands, with snapshots, restore,
    /// replay, Undo, projections, and ordered events.
    ///
    /// The geography is real official-map ground. The acting Red hero stands at (0,-1) beside the
    /// Battle Zone's northern minions: a two-space push sends red-melee-01 from (-1,-1) across the
    /// zone boundary to (-3,-1), whose two cost-1 returns make a real route choice, while every other
    /// pushed object ends inside the zone. Standing at (1,1) instead pushes blue-melee-02 out to
    /// (1,3), whose correction belongs to the acting hero's opponent.
    /// </summary>
    public sealed class MatchSessionCorrectionDispatchTests
    {
        private static readonly EffectNodeId MoveBranch = new EffectNodeId("displacement-move-branch");
        private static readonly EffectNodeId PushBranch = new EffectNodeId("displacement-object-push");

        private static readonly ActionDefinitionId DisplacementSkill =
            new ActionDefinitionId("primary-displacement-skill");

        private static readonly HexCoordinate RedPusherAt = new HexCoordinate(0, -1);
        private static readonly HexCoordinate DisplacedTo = new HexCoordinate(-3, -1);

        [Test]
        public void ACompletedPush_PausesOnTheDisplacedMinionsRouteChoice()
        {
            var session = StartDisplacementSkill(RedPusherAt);

            ChooseOption(session, PushBranch);

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null,
                "The push displaced red-melee-01 outside the zone, so the after-action correction " +
                "pauses on its team's route choice.");
            Assert.That(pending.Stage, Is.EqualTo(DisplacedMinionCorrectionStage.SelectDestination));
            Assert.That(pending.SubjectMinionId.Value, Is.EqualTo("red-melee-01"));
            Assert.That(pending.ChoosingSide, Is.EqualTo(TeamSide.Red));
            Assert.That(pending.DestinationCandidates.Count, Is.EqualTo(2),
                "Two equally short returns exist, so the team really chooses.");
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(DisplacedTo));
            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "The effect nodes all finished; only the correction holds the action open.");

            var destination = pending.DestinationCandidates[0].Destination;
            Accept(session, DirectorCommand.ChooseCorrectionDestination(pending.SelectInvocations[0]));

            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(destination));
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            AssertCardResolved(session);
        }

        [Test]
        public void ADirectMoveOfAMinionOutOfTheZone_InvokesTheSameCorrectionAuthority()
        {
            // The check 6.8k unblocked: direct Move names the adjacent minion by its space, moves it
            // outside the zone, and the same correction authority answers - here with one shortest
            // return, so the correction is automatic and no unnecessary prompt appears.
            var session = StartDisplacementSkill(RedPusherAt);
            ChooseOption(session, MoveBranch);
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            SelectValue(session, new HexCoordinate(-1, -1));
            SelectValue(session, new HexCoordinate(-1, -3));

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null,
                "One shortest return is no choice, so the correction auto-continues.");
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(new HexCoordinate(-1, -2)),
                "The minion was moved out to (-1,-3) and corrected straight back in through its one " +
                "cost-1 entry.");
            Assert.That(
                CorrectionEntries(session),
                Does.Contain("Action.DisplacedMinionCorrectionMoved"),
                "Direct Move reached the same correction authority the push reaches.");
            AssertCardResolved(session);
        }

        [Test]
        public void APushDisplacingAnOpponentsMinion_HandsTheChoiceToThatTeam()
        {
            var session = StartDisplacementSkill(new HexCoordinate(1, 1));

            ChooseOption(session, PushBranch);

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.SubjectMinionId.Value, Is.EqualTo("blue-melee-02"));
            Assert.That(pending.ChoosingSide, Is.EqualTo(TeamSide.Blue),
                "p.18 hands the decision to the displaced minion's team, which is the acting Red " +
                "hero's opponent here.");

            Accept(session, DirectorCommand.ChooseCorrectionDestination(pending.SelectInvocations[0]));
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            AssertCardResolved(session);
        }

        [Test]
        public void APausedCorrection_SurvivesSnapshotRestoreWithRederivedCandidates()
        {
            var session = StartDisplacementSkill(RedPusherAt);
            ChooseOption(session, PushBranch);
            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null);

            var byteIdentical = Restore(session);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(byteIdentical.Round, byteIdentical.Board),
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A paused correction round-trips byte-identically at schema v10.");

            var payload = session.CaptureSnapshot();
            var restored = StartDisplacementSkill(RedPusherAt);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);

            var revived = restored.Projection.PendingDisplacedMinionCorrection;
            Assert.That(revived, Is.Not.Null);
            Assert.That(
                revived.DestinationCandidates.Select(candidate => candidate.Destination),
                Is.EqualTo(pending.DestinationCandidates.Select(candidate => candidate.Destination)),
                "Candidate endpoints are validated on the wire and the routes re-derived, so the " +
                "restored choice offers exactly what the live one did.");
            Assert.That(
                revived.DestinationCandidates.Select(candidate => candidate.Route.Count),
                Is.EqualTo(pending.DestinationCandidates.Select(candidate => candidate.Route.Count)));

            Accept(restored, DirectorCommand.ChooseCorrectionDestination(revived.SelectInvocations[0]));
            Assert.That(restored.Projection.PendingDisplacedMinionCorrection, Is.Null,
                "The restored correction continues to the same completion the live one would.");
            AssertCardResolved(restored);
        }

        [Test]
        public void ACorrectionAnswer_ReplaysAndUndoesDeterministically()
        {
            var session = StartDisplacementSkill(RedPusherAt);
            ChooseOption(session, PushBranch);
            var beforeAnswer = session.Projection.DiagnosticSnapshot;
            var pending = session.Projection.PendingDisplacedMinionCorrection;

            Accept(session, DirectorCommand.ChooseCorrectionDestination(pending.SelectInvocations[1]));
            var afterAnswer = session.Projection.DiagnosticSnapshot;
            var corrected = Coordinate(session, "red-melee-01");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeAnswer),
                "Retained-prefix Undo reproduces the paused correction exactly.");
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(DisplacedTo),
                "The corrected minion is back outside, because the correction is authoritative " +
                "board state the replay reaches.");

            Accept(session, DirectorCommand.ChooseCorrectionDestination(
                session.Projection.PendingDisplacedMinionCorrection.SelectInvocations[1]));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterAnswer),
                "Replaying the same answer reproduces the same occupancy and ordered facts.");
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(corrected));
        }

        [Test]
        public void TheCorrectionFacts_FollowTheActionsClosingCheckpointInOrder()
        {
            var session = StartDisplacementSkill(RedPusherAt);
            ChooseOption(session, PushBranch);
            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Accept(session, DirectorCommand.ChooseCorrectionDestination(pending.SelectInvocations[0]));

            var entries = session.Projection.DiagnosticEvents
                .Select(entry => entry.EventType)
                .Where(type =>
                    type == "Action.SkillActionCompleted" ||
                    type.StartsWith("Action.DisplacedMinionCorrection", StringComparison.Ordinal))
                .ToArray();

            Assert.That(entries, Is.EqualTo(new[]
            {
                "Action.SkillActionCompleted",
                "Action.DisplacedMinionCorrectionStarted",
                "Action.DisplacedMinionCorrectionChoiceRequested",
                "Action.DisplacedMinionCorrectionMoved",
                "Action.DisplacedMinionCorrectionCompleted"
            }), "p.18's 'after you perform an action': the correction runs once, after every effect " +
                "node and batch of the action has finished, and completes before the card resolves.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static void AssertCardResolved(MatchSession session)
        {
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Value.Contains("red-shove"))
                    .State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "The source card resolves only when the whole action, correction included, completes.");
        }

        private static HexCoordinate Coordinate(MatchSession session, string minionId) =>
            session.Projection.Minions.Single(minion => minion.MinionId.Value == minionId).Coordinate;

        private static string[] CorrectionEntries(MatchSession session) =>
            session.Projection.DiagnosticEvents
                .Select(entry => entry.EventType)
                .Where(type => type.StartsWith("Action.DisplacedMinionCorrection", StringComparison.Ordinal))
                .ToArray();

        private static void ChooseOption(MatchSession session, EffectNodeId option)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Equals(option));
            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void SelectValue(MatchSession session, HexCoordinate coordinate)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.Value.Coordinate.Equals(coordinate));
            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static MatchSession StartDisplacementSkill(HexCoordinate redAt)
        {
            var session = CreateDisplacementSession(redAt);
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

            var offer = session.Projection.AvailableTurnActions
                .SingleOrDefault(candidate => candidate.ActionDefinitionId.Equals(DisplacementSkill));
            Assert.That(offer.ActionDefinitionId, Is.EqualTo(DisplacementSkill),
                "The displacement Skill must be the current actor's offer.");
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static MatchSession CreateDisplacementSession(HexCoordinate redAt)
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
                    new HeroOccupancy(redHeroId, redAt),
                    new HeroOccupancy(blueHeroId, new HexCoordinate(-1, -5))
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
