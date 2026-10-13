using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
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
