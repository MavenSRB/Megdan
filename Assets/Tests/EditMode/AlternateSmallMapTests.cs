using System;
using System.Linq;
using System.Collections.Generic;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using NUnit.Framework;
namespace GuardsDigital.Tests.EditMode
{
    public sealed class AlternateSmallMapTests
    {
        public static MatchHost Create(string formatId, bool alternate, ExperimentalMatchOptions options = null)
        {
            var format=MatchFormat.All.Single(f=>f.Id==formatId);
            var catalog=OfficialBoardMapCatalog.Load();
            var map=catalog.Load(alternate?OfficialBoardMapCatalog.AlternateSmall:OfficialBoardMapCatalog.SelectedMap);
            var config=MatchConfiguration.CreateOfficialForInternalTest(format,TeamSide.Red);
            var setup=QuickGameSetupAuthority.Create(HeroLineupComposition.Compose(format,RegisteredHeroRoster.PrintedLineup(format.HeroesPerTeam),map),
                config.Participants,map,config.TieBreakerState);
            var scenario=new InitialScenarioDescription(config.Participants,config.CardDefinitions,config.CardInstances,config.TieBreakerState,
                config.SetupSeed,map.Reference,setup.Heroes.Select(h=>new HeroOccupancy(h.HeroId,h.Coordinate)),Array.Empty<ObstacleOccupancy>());
            return MatchHost.Compose(new MatchSessionInitialState(scenario,setup,catalog.Definitions,config,experiments:options));
        }
        [Test] public void ReviewedGeometryIncludesEverySpawnBorderAndSymmetricLane()
        {
            var map=OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.AlternateSmall);
            Assert.That(map.Spaces.Count,Is.EqualTo(258));
            Assert.That(map.Spaces.Count(s=>s.Kind==BoardMapSpaceKind.Playable),Is.EqualTo(156));
            Assert.That(map.SpawnPoints.Count,Is.EqualTo(40));
            Assert.That(map.Zones.Select(z=>map.Spaces.Count(s=>s.ZoneId.Equals(z.Id))),Is.EqualTo(new[]{17,35,52,35,17}));
            Assert.That(map.Zones.Any(z=>z.Kind=="jungle"),Is.False);
            var cells=map.Spaces.ToDictionary(s=>s.Coordinate);
            foreach(var cell in map.Spaces)
            {
                var mirror=new HexCoordinate(-cell.Coordinate.Q-1,1-cell.Coordinate.R);
                Assert.That(cells.ContainsKey(mirror),Is.True,"Missing symmetric cell "+mirror);
                Assert.That(cells[mirror].Kind,Is.EqualTo(cell.Kind));
                if(cell.Kind==BoardMapSpaceKind.Playable)
                    foreach(var neighbor in cell.Coordinate.GetNeighbors()) Assert.That(cells.ContainsKey(neighbor),Is.True,"Missing terrain border "+neighbor);
            }
            foreach(var spawn in map.SpawnPoints)
            {
                var coord=map.Spaces.Single(s=>s.Id.Equals(spawn.SpaceId)).Coordinate;
                var mirror=cells[new HexCoordinate(-coord.Q-1,1-coord.R)];
                Assert.That(map.SpawnPoints.Any(s=>s.SpaceId.Equals(mirror.Id)&&s.UnitType==spawn.UnitType&&s.TeamId!=spawn.TeamId),Is.True);
            }
            var remaining=new HashSet<HexCoordinate>(map.Spaces.Where(s=>s.Kind==BoardMapSpaceKind.Playable).Select(s=>s.Coordinate));
            var todo=new Queue<HexCoordinate>();todo.Enqueue(remaining.First());remaining.Remove(todo.Peek());
            while(todo.Count>0) foreach(var neighbor in todo.Dequeue().GetNeighbors()) if(remaining.Remove(neighbor))todo.Enqueue(neighbor);
            Assert.That(remaining,Is.Empty);
            foreach(var team in new[]{"red","blue"})
                Assert.That(map.SpawnPoints.Count(s=>s.TeamId==team && s.UnitType=="ranged-minion"),Is.EqualTo(3));
        }
        [TestCase("2v2-quick")] [TestCase("2v2-long")] [TestCase("3v3-quick")] [TestCase("3v3-long")]
        public void SelectedMapSetupAndOfficialCompatibilityArePreserved(string id)
        {
            var format=MatchFormat.All.Single(f=>f.Id==id);var catalog=OfficialBoardMapCatalog.Load();
            var lineup=RegisteredHeroRoster.PrintedLineup(format.HeroesPerTeam);
            var config=MatchConfiguration.CreateOfficialForInternalTest(format,TeamSide.Red);
            var official=catalog.Load(OfficialBoardMapCatalog.SelectedMap);
            var legacy=QuickGameSetupAuthority.Create(HeroLineupComposition.Compose(format,lineup),config.Participants,official,config.TieBreakerState);
            var modern=QuickGameSetupAuthority.Create(HeroLineupComposition.Compose(format,lineup,official),config.Participants,official,config.TieBreakerState);
            Assert.That(modern.Heroes.Select(h=>(h.HeroId,h.Coordinate)),Is.EqualTo(legacy.Heroes.Select(h=>(h.HeroId,h.Coordinate))));
            Assert.That(modern.Minions.Select(m=>(m.MinionId,m.Coordinate)),Is.EqualTo(legacy.Minions.Select(m=>(m.MinionId,m.Coordinate))));
            var map=catalog.Load(OfficialBoardMapCatalog.AlternateSmall);
            var setup=QuickGameSetupAuthority.Create(HeroLineupComposition.Compose(format,lineup,map),config.Participants,map,config.TieBreakerState);
            Assert.That(setup.Heroes.Count,Is.EqualTo(format.HeroesPerTeam*2));Assert.That(setup.Minions.Count,Is.EqualTo(12));
            Assert.That(setup.WaveCount,Is.EqualTo(format.WaveCount));Assert.That(setup.Teams.All(t=>t.LifeCount==format.LifeCountPerTeam),Is.True);
            Assert.That(setup.Heroes.Select(h=>h.Coordinate).Take(2),Is.EqualTo(new[]{new HexCoordinate(2,8),new HexCoordinate(0,9)}));
            Assert.That(new MatchCompositionPlan(format,map:map.Reference).WithLineup(lineup).Map,Is.EqualTo(map.Reference));
            var host=Create(id,true);var snapshot=host.CaptureSnapshot();
            Assert.That(host.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted,Is.True);
            var other=Create(id,false);var before=other.CaptureSnapshot();
            Assert.That(other.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted,Is.False);
            Assert.That(other.CaptureSnapshot(),Is.EqualTo(before));
        }
        [TestCase("2v2-quick")] [TestCase("3v3-long")]
        public void JoinedMapSurvivesCommandsUndoRestartAndReconnect(string id)
        {
            var host=Create(id,true,new ExperimentalMatchOptions(teammateCoordination:true));
            var configuration=MatchConfiguration.CreateOfficialForInternalTest(MatchFormat.All.Single(f=>f.Id==id),TeamSide.Red);
            var identity=PrivateSessionIdentity.ForComposition(host.Projection.SelectedMap,configuration,host.Projection.Setup,"map-test-build");
            var wire=PrivateOnlineWireHarness.Compose(host,"map-test",identity);wire.SeatAll();wire.ReadyAndPickEverySeat();
            var machine=wire.Client(1);var view=machine.LatestProjection;
            Assert.That(host.Projection.SelectedMap.Reference,Is.EqualTo(OfficialBoardMapCatalog.AlternateSmall));
            Assert.That(machine.TrySubmitCommand(view.AuthorizedCommands.First(),out _),Is.True);wire.Pump();
            Assert.That(host.Submit(MatchSubmission.ForUndo()).Accepted,Is.True);
            wire.Session.RepublishProjections();wire.Pump();
            wire.DisconnectClient(1);var replacement=wire.Client(wire.ReconnectClient(1));
            Assert.That(replacement.LatestProjection,Is.Not.Null);
            Assert.That(replacement.TrySubmitCommand(replacement.LatestProjection.AuthorizedCommands.First(),out _),Is.True);wire.Pump();
            var restart=host.Submit(MatchSubmission.ForRestart());Assert.That(restart.Accepted,Is.True,restart.Detail);
            Assert.That(host.Projection.SelectedMap.Reference,Is.EqualTo(OfficialBoardMapCatalog.AlternateSmall));
        }
        [TestCase("2v2-quick")] [TestCase("3v3-long")]
        public void AlternateMapAdvancesRoundsAndRestoresEverySeat(string id)
        {
            var host=Create(id,true);
            var count=OwnerScopedProjectionTests.DriveThroughSeats(host,1600,p=>p.RoundIndex>=3 ||
                p.Phase==RoundResolutionPhase.GameEnded);
            Assert.That(host.Projection.RoundIndex>=3 || host.Projection.Outcome.HasEnded,Is.True);
            Assert.That(count,Is.LessThan(1600),"The alternate map must not strand the normal match loop.");
            var copy=Create(id,true);
            Assert.That(copy.Submit(MatchSubmission.ForSnapshotRestore(host.CaptureSnapshot())).Accepted,Is.True);
            foreach(var seat in host.Seats.Seats)
                Assert.That(OwnerScopedProjectionTests.Describe(copy.ProjectionForSeat(seat.Id)),
                    Is.EqualTo(OwnerScopedProjectionTests.Describe(host.ProjectionForSeat(seat.Id))));
        }
        [TestCase("2v2-quick")] [TestCase("3v3-long")]
        public void EmptyBankTimeoutsCannotStrandTheAlternateMap(string id)
        {
            var host = Create(id, true, new ExperimentalMatchOptions(turnTimer: true,
                cardCommitmentSeconds: 1, cardResolutionSeconds: 1, responseSeconds: 1,
                cardUpgradeSeconds: 1, teamBankSeconds: 0));
            int steps = 0;
            while (host.Projection.RoundIndex < 3 && !host.Projection.Outcome.HasEnded && steps++ < 500)
            {
                host.SetTimerStarted(true);
                foreach (var seat in host.Seats.Seats) host.SetClockReadiness(seat.Id, host.Version, true);
                var version = host.Version;
                host.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);
                Assert.That(host.TimeoutFailure, Is.Empty);
                Assert.That(host.Version, Is.GreaterThan(version), "Expired choice: " + host.Projection.Phase);
            }
            Assert.That(host.Projection.RoundIndex >= 3 || host.Projection.Outcome.HasEnded, Is.True);
            Assert.That(steps, Is.LessThan(500));
        }
        [TestCase(TeamSide.Red)] [TestCase(TeamSide.Blue)]
        public void LanePushRespawnsMatchingMinionsAndCapturesEitherThrone(TeamSide winner)
        {
            var host = Create("3v3-long", true);
            var map = host.Projection.SelectedMap;
            // No occupying heroes: isolate the printed lane progression and matching spawn types.
            var board = new AdjacentMoveAuthority(map, Array.Empty<HeroOccupancy>(),
                Array.Empty<ObstacleOccupancy>(), host.Projection.Setup.Minions);
            var lane = new LanePushTransitionAuthority(board, 5);
            var transitions = new SystemTransitionAuthority(MatchSystemTransitions.CreateCatalog(),
                MatchSystemTransitions.CreateRules(lane, null));
            var consumption = new LanePushConsumption(lane, transitions);
            var loser = winner == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;
            var losingId = loser == TeamSide.Red ? "red" : "blue";
            var first = consumption.TryConsume(new LanePushRequestId("alternate-first"), winner, loser,
                new TieBreakerState(TeamSide.Red));
            Assert.That(first.Accepted && !first.IsPaused, Is.True);
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(new ZoneId(losingId + "-approach")));
            var spawns = map.SpawnPoints.Where(s => s.UnitType.EndsWith("-minion", StringComparison.Ordinal) &&
                map.Spaces.Single(c => c.Id.Equals(s.SpaceId)).ZoneId.Equals(board.CurrentBattleZoneId)).ToArray();
            Assert.That(board.Minions.Count, Is.EqualTo(spawns.Length));
            foreach (var spawn in spawns)
            {
                var coordinate = map.Spaces.Single(c => c.Id.Equals(spawn.SpaceId)).Coordinate;
                var minion = board.Minions.Single(m => m.Coordinate.Equals(coordinate));
                Assert.That(minion.MinionType.ToString().ToLowerInvariant() + "-minion", Is.EqualTo(spawn.UnitType));
            }
            var second = consumption.TryConsume(new LanePushRequestId("alternate-second"), winner, loser,
                new TieBreakerState(TeamSide.Red));
            Assert.That(second.Accepted && !second.IsPaused, Is.True);
            Assert.That(lane.HasEnded, Is.True);
            Assert.That(lane.GameEndingCause, Is.EqualTo(LanePushGameEndingCause.ThroneCaptured));
            Assert.That(lane.WinningSide, Is.EqualTo(winner));
        }
        [TestCase(TeamSide.Red)] [TestCase(TeamSide.Blue)]
        public void MovementTravelAndRespawnRespectTheAlternateGeometry(TeamSide side)
        {
            var host = Create("3v3-quick", true); var map = host.Projection.SelectedMap;
            var participants = host.Projection.Participants;
            var actor = host.Projection.Setup.Heroes.First(h => participants.GetTeamForHero(h.HeroId).Side == side);
            var occupant = new HeroOccupancy(actor.HeroId, actor.Coordinate);
            var board = new AdjacentMoveAuthority(map, new[] { occupant }, Array.Empty<ObstacleOccupancy>());
            var movement = MovementReachabilityQuery.FindReachableDestinations(occupant, board, 3, RuleModificationSet.Empty);
            Assert.That(movement, Is.Not.Empty);
            Assert.That(movement.All(d => map.PlayableCoordinates.Contains(d.Destination)), Is.True);
            var travel = FastTravelActionQuery.FindDestinationsForActor(actor.HeroId, board, board, participants,
                board.Minions, FastTravelZoneScope.SameOrAdjacentZone, RuleModificationSet.Empty);
            Assert.That(travel, Is.Not.Empty);
            var prefix = side == TeamSide.Red ? "red" : "blue";
            var expected = map.Spaces.Where(s => s.Kind == BoardMapSpaceKind.Playable &&
                (s.ZoneId.Equals(new ZoneId(prefix + "-throne")) || s.ZoneId.Equals(new ZoneId(prefix + "-approach"))))
                .Select(s => s.Coordinate).Where(c => !c.Equals(actor.Coordinate));
            Assert.That(travel, Is.EquivalentTo(expected));
            var emptyBoard = new AdjacentMoveAuthority(map, Array.Empty<HeroOccupancy>(), Array.Empty<ObstacleOccupancy>());
            var respawn = HeroRespawnRules.CreateChoice(actor.HeroId, new CardInstanceId("alternate-respawn"),
                participants, emptyBoard, RuleModificationSet.Empty);
            Assert.That(respawn.Destinations.Select(d => d.Coordinate), Is.EquivalentTo(map.SpawnPoints
                .Where(s => s.TeamId == prefix && s.UnitType == "hero")
                .Select(s => map.Spaces.Single(c => c.Id.Equals(s.SpaceId)).Coordinate)));
        }
        [Test] public void RejectionCarriesBothMapAndFormatAsTypedPublicSetup()
        {
            var message=new SessionRejectMessage(SessionRejectionReason.MapRevisionMismatch,"Map differs.",
                OfficialBoardMapCatalog.AlternateSmall.MapId.Value,1,"2v2-long");
            var bytes=PrivateSessionFrameCodec.RejectFrame(message).Skip(PrivateSessionProtocol.FrameHeaderBytes).ToArray();
            Assert.That(PrivateSessionFrameCodec.TryDecodeReject(bytes,out var decoded),Is.True);
            Assert.That(decoded.HostMapId,Is.EqualTo(message.HostMapId));Assert.That(decoded.HostMapRevision,Is.EqualTo(1));
            Assert.That(decoded.HostFormatId,Is.EqualTo("2v2-long"));
            var catalog=OfficialBoardMapCatalog.Load();
            Assert.Throws<BoardMapLoadException>(()=>catalog.Load(new MapDefinitionReference(new MapId("unknown"),1)));
            Assert.Throws<BoardMapLoadException>(()=>catalog.Load(new MapDefinitionReference(OfficialBoardMapCatalog.AlternateSmall.MapId,2)));
        }
    }
}
