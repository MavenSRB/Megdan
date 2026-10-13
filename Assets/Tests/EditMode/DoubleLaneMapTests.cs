using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// W-072's reviewed transcription pins. Exact playable and terrain totals are intentional: a
    /// full-grid-outline hex must not silently change category in either direction.
    /// </summary>
    public sealed class DoubleLaneMapTests
    {
        [Test]
        public void GreenMap_PreservesReviewedPlayableTerrainAndThreeMiddleZones()
        {
            var map = Load(OfficialBoardMapCatalog.GreenDoubleLane);

            Assert.That(map.Spaces, Has.Count.EqualTo(456));
            Assert.That(map.Spaces.Count(space => space.Kind == BoardMapSpaceKind.Playable), Is.EqualTo(336));
            Assert.That(map.Spaces.Count(space => space.Kind == BoardMapSpaceKind.Terrain), Is.EqualTo(120));
            Assert.That(map.SpawnPoints, Has.Count.EqualTo(74));
            Assert.That(ZoneSize(map, "middle-snow"), Is.EqualTo(21));
            Assert.That(ZoneSize(map, "middle-water"), Is.EqualTo(20));
            Assert.That(ZoneSize(map, "middle-forest"), Is.EqualTo(21));
            Assert.That(map.OrderedLanes, Has.Count.EqualTo(2));
            Assert.That(map.OrderedLanes.All(lane =>
                lane.All(zone => !zone.Value.StartsWith("middle-"))), Is.True,
                "The three middle movement zones are playable but are not Lane-push zones.");
            AssertStartingBattleZoneSpawns(map);
        }

        [Test]
        public void LavaMap_PreservesReviewedPlayableTerrainAndMiddleBridge()
        {
            var map = Load(OfficialBoardMapCatalog.LavaBridgeDoubleLane);

            Assert.That(map.Spaces, Has.Count.EqualTo(386));
            Assert.That(map.Spaces.Count(space => space.Kind == BoardMapSpaceKind.Playable), Is.EqualTo(288));
            Assert.That(map.Spaces.Count(space => space.Kind == BoardMapSpaceKind.Terrain), Is.EqualTo(98));
            Assert.That(map.SpawnPoints, Has.Count.EqualTo(58));
            Assert.That(ZoneSize(map, "middle-bridge"), Is.EqualTo(20));
            Assert.That(map.OrderedLanes, Has.Count.EqualTo(2));
            Assert.That(map.OrderedLanes.All(lane =>
                lane.All(zone => !zone.Value.Equals("middle-bridge"))), Is.True,
                "The bridge is a playable movement zone but is not a Lane-push zone.");
            AssertStartingBattleZoneSpawns(map);
        }

        private static BoardMapDefinition Load(MapDefinitionReference reference) =>
            OfficialBoardMapCatalog.Load().Load(reference);

        private static int ZoneSize(BoardMapDefinition map, string zoneId) =>
            map.Spaces.Count(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                space.ZoneId.Value.Equals(new ZoneId(zoneId)));

        private static void AssertStartingBattleZoneSpawns(BoardMapDefinition map)
        {
            foreach (var zone in map.Zones.Where(candidate => candidate.IsStartingBattleZone))
            {
                foreach (var team in new[] { "red", "blue" })
                {
                    var unitTypes = map.SpawnPoints
                        .Where(spawn => spawn.TeamId == team &&
                            map.GetZoneForSpace(spawn.SpaceId).Id.Equals(zone.Id))
                        .Select(spawn => spawn.UnitType)
                        .OrderBy(value => value)
                        .ToArray();

                    Assert.That(unitTypes, Is.EqualTo(new[]
                    {
                        "heavy-minion", "melee-minion", "melee-minion", "ranged-minion"
                    }), $"{map.Id} {zone.Id} {team} starting minions");
                }
            }
        }
    }
}
