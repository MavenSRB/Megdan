using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class DoubleLaneSetupTests
    {
        [TestCase(false, 4, WaveCounterAllocation.PerLane)]
        [TestCase(false, 5, WaveCounterAllocation.SharedTotal)]
        [TestCase(true, 4, WaveCounterAllocation.SharedTotal)]
        [TestCase(true, 5, WaveCounterAllocation.PerLane)]
        public void BothLargeMaps_ComposeFourAndFiveHeroesPerTeamWithSelectedWaveMode(
            bool lava,
            int heroesPerTeam,
            WaveCounterAllocation allocation)
        {
            var reference = lava
                ? OfficialBoardMapCatalog.LavaBridgeDoubleLane
                : OfficialBoardMapCatalog.GreenDoubleLane;
            var map = OfficialBoardMapCatalog.Load().Load(reference);
            var format = MatchFormat.DefaultForTeamSize(heroesPerTeam);
            var lineup = RegisteredHeroRoster.DefaultLineup(heroesPerTeam);
            var participants = HeroLineupComposition.ComposeParticipants(lineup);
            var configuration = HeroLineupComposition.Compose(
                format, lineup, map, allocation);

            var setup = QuickGameSetupAuthority.Create(
                configuration, participants, map, new TieBreakerState(TeamSide.Red));

            Assert.That(setup.Heroes, Has.Count.EqualTo(heroesPerTeam * 2));
            Assert.That(setup.Minions, Has.Count.EqualTo(16));
            Assert.That(setup.WaveCount, Is.EqualTo(7));
            Assert.That(setup.WaveCounterAllocation, Is.EqualTo(allocation));
            Assert.That(setup.Teams.Select(team => team.LifeCount),
                Has.All.EqualTo(heroesPerTeam == 4 ? 6 : 7));
            AssertExtraHeroesUsePlayableThroneHexesBesideOccupiedPrintedSpawns(
                map, setup, heroesPerTeam);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BothLargeMaps_BindTwoMeleeOneRangedAndOneHeavyMiniatureToEachTeamInEachLane(
            bool lava)
        {
            var map = OfficialBoardMapCatalog.Load().Load(lava
                ? OfficialBoardMapCatalog.LavaBridgeDoubleLane
                : OfficialBoardMapCatalog.GreenDoubleLane);
            var format = MatchFormat.DefaultForTeamSize(5);
            var lineup = RegisteredHeroRoster.DefaultLineup(5);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(format, lineup, map, WaveCounterAllocation.PerLane),
                HeroLineupComposition.ComposeParticipants(lineup),
                map,
                new TieBreakerState(TeamSide.Red));

            foreach (var lane in map.OrderedLanes)
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var laneMinions = setup.Minions.Where(minion =>
                {
                    var spawn = map.SpawnPoints.Single(point =>
                        point.Id.Equals(minion.SpawnPoint.SpawnPointId));
                    var zone = map.GetZoneForSpace(spawn.SpaceId).Id;
                    return minion.TeamSide == side && lane.Contains(zone);
                }).ToArray();

                Assert.That(laneMinions.Count(minion =>
                    minion.MinionType == StandardMinionType.Melee), Is.EqualTo(2));
                Assert.That(laneMinions.Count(minion =>
                    minion.MinionType == StandardMinionType.Ranged), Is.EqualTo(1));
                Assert.That(laneMinions.Count(minion =>
                    minion.MinionType == StandardMinionType.Heavy), Is.EqualTo(1));
            }
        }

        [TestCase(false, true)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        [TestCase(true, false)]
        public void HeavyImmunity_IsScopedToTheHeavysOwnActiveBattleZone(bool lava, bool firstLane)
        {
            var map = OfficialBoardMapCatalog.Load().Load(lava
                ? OfficialBoardMapCatalog.LavaBridgeDoubleLane
                : OfficialBoardMapCatalog.GreenDoubleLane);
            var lineup = RegisteredHeroRoster.DefaultLineup(5);
            var participants = HeroLineupComposition.ComposeParticipants(lineup);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(MatchFormat.DefaultForTeamSize(5), lineup, map),
                participants, map, new TieBreakerState(TeamSide.Red));
            var board = new AdjacentMoveAuthority(map, Array.Empty<HeroOccupancy>(),
                Array.Empty<ObstacleOccupancy>(), setup.Minions);
            var targetBattleZone = board.CurrentBattleZoneIds.Single(zone =>
                zone.Value.StartsWith(firstLane ? "top-" : "bottom-", StringComparison.Ordinal));
            var otherBattleZone = board.CurrentBattleZoneIds.Single(zone =>
                zone.Value.StartsWith(firstLane ? "bottom-" : "top-", StringComparison.Ordinal));
            var heavy = board.Minions.Single(minion =>
                minion.TeamSide == TeamSide.Red &&
                minion.MinionType == StandardMinionType.Heavy &&
                map.GetZoneForSpace(minion.OccupiedSpace.SpaceId).Id.Equals(targetBattleZone));
            var target = RuleObject.ForMinion(heavy.MinionId);

            Assert.That(ImmunityQuery.IsImmuneTo(
                target, RuleActivity.Attack, RuleObject.None,
                board, participants, RuleModificationSet.Empty), Is.True,
                "A friendly non-Heavy in the same active Battle Zone protects the Heavy.");

            foreach (var escort in board.Minions.Where(minion =>
                minion.TeamSide == heavy.TeamSide &&
                minion.MinionType != StandardMinionType.Heavy &&
                map.GetZoneForSpace(minion.OccupiedSpace.SpaceId).Id.Equals(targetBattleZone)).ToArray())
            {
                Assert.That(board.CommitValidatedRemoval(
                    BoardObjectReference.ForMinion(escort.MinionId)).Accepted, Is.True);
            }

            Assert.That(board.Minions.Any(minion =>
                minion.TeamSide == heavy.TeamSide &&
                minion.MinionType != StandardMinionType.Heavy &&
                map.GetZoneForSpace(minion.OccupiedSpace.SpaceId).Id.Equals(otherBattleZone)), Is.True,
                "The other lane deliberately retains the escort that exposed the regression.");
            Assert.That(ImmunityQuery.IsImmuneTo(
                target, RuleActivity.Attack, RuleObject.None,
                board, participants, RuleModificationSet.Empty), Is.False,
                "A non-Heavy in the other lane cannot protect a lone Heavy in this Battle Zone.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EveryLargeMapLaneZone_FillsEveryPrintedMinionGlyphThroughPushAndRestore(bool lava)
        {
            // W-078: the user requires every printed glyph to spawn a minion. In particular,
            // lava's bottom Red Approach must fill its third Red melee point at (-13,16).
            var map = OfficialBoardMapCatalog.Load().Load(lava
                ? OfficialBoardMapCatalog.LavaBridgeDoubleLane
                : OfficialBoardMapCatalog.GreenDoubleLane);
            var lineup = RegisteredHeroRoster.DefaultLineup(5);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(MatchFormat.DefaultForTeamSize(5), lineup, map),
                HeroLineupComposition.ComposeParticipants(lineup), map, new TieBreakerState(TeamSide.Red));

            foreach (var lane in map.OrderedLanes)
            {
                var board = new AdjacentMoveAuthority(map, Array.Empty<HeroOccupancy>(),
                    Array.Empty<ObstacleOccupancy>(), setup.Minions);
                var otherLaneMinions = board.Minions.Where(minion =>
                    !lane.Contains(map.GetZoneForSpace(minion.OccupiedSpace.SpaceId).Id)).ToArray();
                AssertAllActiveGlyphs(board);
                // Visit both ends, then return to setup: this also exercises the smaller wave
                // retaining its unused reserve and reusing it on a later defending wave.
                var destinations = lane.Where(zone => !map.GetZone(zone).IsThrone)
                    .Concat(lane.Reverse().Where(zone => !map.GetZone(zone).IsThrone))
                    .Concat(lane.Where(zone => map.GetZone(zone).IsStartingBattleZone));
                foreach (var destination in destinations)
                {
                    while (!board.CurrentBattleZoneIds.Single(lane.Contains).Equals(destination))
                    {
                        var current = board.CurrentBattleZoneIds.Single(lane.Contains);
                        var ordered = lane.ToArray();
                        var towardStart = Array.IndexOf(ordered, destination) < Array.IndexOf(ordered, current);
                        var losing = map.GetZone(towardStart ? ordered.First() : ordered.Last()).TeamId == "red"
                            ? TeamSide.Red : TeamSide.Blue;
                        var winning = losing == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;
                        var authority = new LanePushTransitionAuthority(board, 50, WaveCounterAllocation.PerLane);
                        var transitions = new SystemTransitionAuthority(MatchSystemTransitions.CreateCatalog(),
                            MatchSystemTransitions.CreateRules(authority, null));
                        var result = new LanePushConsumption(authority, transitions).TryConsume(
                            new LanePushRequestId("all-glyphs-" + current + "-" + destination), current,
                            winning, losing, new TieBreakerState(TeamSide.Red));
                        Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                        Assert.That(result.IsPaused, Is.False, "Empty glyphs need no placement choice.");
                        AssertAllActiveGlyphs(board);
                        foreach (var untouched in otherLaneMinions)
                            Assert.That(board.Minions.Single(minion => minion.MinionId.Equals(untouched.MinionId)),
                                Is.SameAs(untouched), "A push must not borrow the other lane's miniature.");

                        var restored = AdjacentMoveAuthority.Restore(board.CaptureRestoreState(), map, setup.Minions);
                        Assert.That(MinionInventory(restored), Is.EqualTo(MinionInventory(board)),
                            "Restore must preserve both placed and reserve identities.");
                        board = restored;
                        otherLaneMinions = board.Minions.Where(minion =>
                            !lane.Contains(map.GetZoneForSpace(minion.OccupiedSpace.SpaceId).Id)).ToArray();
                    }
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefendingReserveMinion_SurvivesSnapshotAndJoinedProjection(bool lava)
        {
            var maps = OfficialBoardMapCatalog.Load();
            var map = maps.Load(lava ? OfficialBoardMapCatalog.LavaBridgeDoubleLane : OfficialBoardMapCatalog.GreenDoubleLane);
            var format = MatchFormat.DefaultForTeamSize(5);
            var lineup = RegisteredHeroRoster.DefaultLineup(5);
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var setup = QuickGameSetupAuthority.Create(HeroLineupComposition.Compose(format, lineup, map),
                configuration.Participants, map, configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(configuration.Participants, configuration.CardDefinitions,
                configuration.CardInstances, configuration.TieBreakerState, configuration.SetupSeed, map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)), Array.Empty<ObstacleOccupancy>());
            var host = MatchHost.Compose(new MatchSessionInitialState(scenario, setup, maps.Definitions, configuration));
            var trustedSetup = host.Projection;
            var payload = host.CaptureSnapshot();
            var authoritative = RuleEventLogSnapshotEnvelope.TryDeserialize(payload, out var envelope)
                ? envelope.AuthoritativePayload : payload;
            var restored = AuthoritativeSnapshotSerializer.Deserialize(MatchContentCatalogs.ForThisBuild(),
                authoritative, configuration.CardDefinitions, maps.Definitions, setup.Minions);
            var push = restored.Round.LanePush.TryConsume(new LanePushRequestId("w078-network-bottom"),
                new ZoneId("bottom-starting-battle-zone"), TeamSide.Blue, TeamSide.Red, new TieBreakerState(TeamSide.Red));
            Assert.That(push.Accepted, Is.True);
            var saved = AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board);
            Assert.That(host.Submit(MatchSubmission.ForSnapshotRestore(saved)).Accepted, Is.True);
            Assert.That(NetworkSeatProjectionCodec.TryDecode(NetworkSeatProjectionCodec.Encode(
                host.ProjectionForSeat(host.Seats.Seats[0].Id)), out var record), Is.True);
            var joined = new NetworkMatchReadModel(trustedSetup, MatchContentCatalogs.ForThisBuild());
            joined.Replace(record, true);
            Assert.That(joined.Minions.Select(minion => (minion.MinionId, minion.TeamSide, minion.MinionType, minion.Coordinate)),
                Is.EqualTo(host.Projection.Minions.Select(minion => (minion.MinionId, minion.TeamSide, minion.MinionType, minion.Coordinate))));
            Assert.That(joined.Minions.Count, Is.EqualTo(17));
            if (lava)
                Assert.That(joined.Minions.Any(minion => minion.Coordinate.Equals(new HexCoordinate(-13, 16)) &&
                    minion.TeamSide == TeamSide.Red && minion.MinionType == StandardMinionType.Melee), Is.True);
        }

        private static string[] MinionInventory(AdjacentMoveAuthority board) => board.Minions
            .Select(minion => "board:" + minion.MinionId + ":" + minion.Coordinate)
            .Concat(board.FindMinionSupply().Select(minion => "supply:" + minion.MinionId))
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        private static void AssertAllActiveGlyphs(AdjacentMoveAuthority board)
        {
            foreach (var zone in board.CurrentBattleZoneIds)
            {
                var spawns = board.MapDefinition.SpawnPoints.Where(spawn =>
                    spawn.UnitType.EndsWith("-minion", StringComparison.Ordinal) &&
                    board.MapDefinition.GetZoneForSpace(spawn.SpaceId).Id.Equals(zone)).ToArray();
                Assert.That(board.FindMinionsInZone(zone).Count, Is.EqualTo(spawns.Length),
                    $"{board.MapDefinition.Id} {zone}: every printed glyph must spawn a minion.");
                foreach (var spawn in spawns)
                {
                    var minion = board.Minions.SingleOrDefault(value => value.OccupiedSpace.SpaceId.Equals(spawn.SpaceId));
                    Assert.That(minion, Is.Not.Null, $"{zone} {spawn.Id}: missing printed minion.");
                    Assert.That(minion.TeamSide, Is.EqualTo(spawn.TeamId == "red" ? TeamSide.Red : TeamSide.Blue));
                    Assert.That(minion.MinionType.ToString().ToLowerInvariant() + "-minion", Is.EqualTo(spawn.UnitType));
                }
            }
        }

        private static void AssertExtraHeroesUsePlayableThroneHexesBesideOccupiedPrintedSpawns(
            BoardMapDefinition map,
            QuickGameSetupState setup,
            int heroesPerTeam)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var teamId = side == TeamSide.Red ? "red" : "blue";
                var printedSpawnSpaces = map.SpawnPoints
                    .Where(spawn => spawn.TeamId == teamId && spawn.UnitType == "hero")
                    .Select(spawn => map.Spaces.Single(space => space.Id.Equals(spawn.SpaceId)))
                    .ToArray();
                Assert.That(printedSpawnSpaces, Has.Length.EqualTo(3));

                var teamHeroes = setup.Heroes.Where(hero => hero.TeamSide == side).ToArray();
                Assert.That(teamHeroes, Has.Length.EqualTo(heroesPerTeam));
                foreach (var extra in teamHeroes.Skip(3))
                {
                    var occupied = map.Spaces.Single(space =>
                        space.Id.Equals(extra.OccupiedSpace.SpaceId));
                    Assert.That(occupied.Kind, Is.EqualTo(BoardMapSpaceKind.Playable));
                    Assert.That(map.GetZoneForSpace(occupied.Id).IsThrone, Is.True);
                    Assert.That(map.GetZoneForSpace(occupied.Id).TeamId, Is.EqualTo(teamId));
                    Assert.That(printedSpawnSpaces.Any(spawn =>
                        spawn.Coordinate.GetNeighbors().Contains(occupied.Coordinate)), Is.True,
                        $"{map.Id} {side} extra hero at {occupied.Id} must be adjacent to a printed hero spawn.");
                    Assert.That(printedSpawnSpaces.Any(spawn => spawn.Id.Equals(occupied.Id)), Is.False,
                        "Heroes four and five use empty adjacent playable hexes, not an occupied glyph.");
                }
            }
        }
    }
}
