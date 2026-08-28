using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HeroRespawnTests
    {
        [Test]
        public void OccupiedThroneSpawn_OffersEveryNearestEmptyAdjacentFallback()
        {
            var participants = StagedBoardParticipants.Create();
            var redHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var center = new HexCoordinate(0, 0);
            var coordinates = new[] { center }.Concat(center.GetNeighbors()).ToArray();
            var zone = new BoardMapZone(
                new ZoneId("blue-throne"), "Blue Throne", "throne", "blue", true, false);
            var spaces = coordinates.Select(coordinate => new BoardMapSpace(
                new SpaceId($"q{coordinate.Q}_r{coordinate.R}"),
                coordinate,
                BoardMapSpaceKind.Playable,
                zone.Id)).ToArray();
            var map = new BoardMapDefinition(
                new MapId("hero-respawn-fallback"),
                1,
                "Hero Respawn Fallback",
                spaces,
                new[] { zone },
                new[]
                {
                    new BoardMapSpawnPoint(
                        new SpawnPointId("blue-hero-spawn"), spaces[0].Id, "blue", "hero")
                },
                Array.Empty<ZoneId>());
            var board = new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(redHero, center) },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

            var choice = HeroRespawnRules.CreateChoice(
                blueHero,
                new CardInstanceId("hero-respawn-card"),
                participants,
                board);

            Assert.That(choice.Destinations, Has.Count.EqualTo(6));
            Assert.That(choice.Destinations.Select(value => value.Coordinate),
                Is.EquivalentTo(center.GetNeighbors()));
            Assert.That(choice.Destinations, Has.All.Matches<HeroRespawnDestination>(value =>
                value.IsFallback && value.AdjacentSpawnPointId.Equals(
                    new SpawnPointId("blue-hero-spawn"))));
        }

        [Test]
        public void OffBoardHero_RespawnsFromProjectedChoiceAcrossRestoreReplayAndUndo()
        {
            var session = CreateSessionWithBlueHeroOffBoard();
            CommitBlueFirstTurn(session);

            var pending = session.Projection.PendingHeroRespawnChoice;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.Destinations, Is.Not.Empty);
            Assert.That(pending.Destinations, Has.All.Matches<HeroRespawnDestination>(destination =>
                !destination.IsFallback));
            Assert.That(session.Projection.AvailableTurnActions, Is.Empty,
                "No ordinary action control is exposed before the respawn decision.");

            var pausedSnapshot = session.CaptureSnapshot();
            var restored = CreateSessionWithBlueHeroOffBoard();
            Assert.That(restored.RestoreSnapshot(pausedSnapshot).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(pausedSnapshot));
            Assert.That(restored.Projection.PendingHeroRespawnChoice.Destinations,
                Is.EqualTo(pending.Destinations),
                "Respawn choices are re-derived from restored board state, not trusted from UI data.");

            var beforeForged = session.CaptureSnapshot();
            var forged = session.Execute(DirectorCommand.RespawnHero(new HeroRespawnCommand(
                HeroRespawnCommandKind.Respawn,
                pending.HeroId,
                pending.CardInstanceId,
                new HexCoordinate(99, 99))));
            Assert.That(forged.Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeForged),
                "A forged destination rejects atomically.");

            Accept(session, DirectorCommand.RespawnHero(pending.RespawnInvocations[0]));
            Accept(session, DirectorCommand.PerformHold(pending.CardInstanceId));
            var completedSnapshot = session.CaptureSnapshot();
            var commands = session.Projection.AcceptedCommands.ToArray();

            var replayed = CreateSessionWithBlueHeroOffBoard();
            foreach (var command in commands)
            {
                Accept(replayed, command);
            }

            Assert.That(replayed.CaptureSnapshot(), Is.EqualTo(completedSnapshot));
            Assert.That(session.Undo().Accepted, Is.True);
            Accept(session, commands.Last());
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(completedSnapshot),
                "Retained-prefix Undo reconstructs the same respawned occupancy and card state.");
        }

        [Test]
        public void OffBoardHero_MayResolveItsCardWithoutRespawningOrActing()
        {
            var session = CreateSessionWithBlueHeroOffBoard();
            CommitBlueFirstTurn(session);
            var pending = session.Projection.PendingHeroRespawnChoice;

            Accept(session, DirectorCommand.ResolveCardWithoutRespawn(pending.DeclineInvocation));

            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
            Assert.That(restored.Board.TryGetHeroCoordinate(pending.HeroId, out _), Is.False);
            Assert.That(session.Projection.Cards.Single(card =>
                    card.Id.Equals(pending.CardInstanceId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(session.Projection.SelectedActor.CardInstanceId,
                Is.Not.EqualTo(pending.CardInstanceId),
                "Declining consumes that card's turn without fabricating an action.");
        }

        private static MatchSession CreateSessionWithBlueHeroOffBoard()
        {
            var configuration = MatchConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var occupiedHeroes = setup.Heroes
                .Where(hero => hero.TeamSide == TeamSide.Red)
                .Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate));
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                occupiedHeroes,
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        internal static MatchSession CreatePendingRespawnSession()
        {
            var session = CreateSessionWithBlueHeroOffBoard();
            CommitBlueFirstTurn(session);
            Assert.That(session.Projection.PendingHeroRespawnChoice, Is.Not.Null);
            return session;
        }

        private static void CommitBlueFirstTurn(MatchSession session)
        {
            var red = session.Projection.Cards.Single(card =>
                card.Id.Value.Contains("bombard") && card.State.Location == CardLocation.Hand);
            var blue = session.Projection.Cards.Single(card =>
                card.Id.Value.Contains("feint") && card.State.Location == CardLocation.Hand);
            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(blue.Id));
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }
    }
}
