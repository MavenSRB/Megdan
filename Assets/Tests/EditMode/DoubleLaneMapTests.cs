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

        [Test]
        public void GreenMap_EveryLaneZoneMatchesTheReviewedPrintedMinionInventory()
        {
            var map = Load(OfficialBoardMapCatalog.GreenDoubleLane);

            AssertMinionZone(map, "top-blue-approach", 2, 1, 1, 3, 1, 1);
            AssertMinionZone(map, "top-starting-battle-zone", 2, 1, 1, 2, 1, 1);
            AssertMinionZone(map, "top-red-approach", 2, 1, 1, 2, 1, 1);
            AssertMinionZone(map, "top-red-gate", 3, 1, 1, 2, 1, 1);
            AssertMinionZone(map, "bottom-blue-gate", 2, 1, 1, 3, 1, 1);
            AssertMinionZone(map, "bottom-blue-approach", 2, 1, 1, 2, 1, 1);
            AssertMinionZone(map, "bottom-starting-battle-zone", 2, 1, 1, 2, 1, 1);
            AssertMinionZone(map, "bottom-red-approach", 3, 1, 1, 2, 1, 1);
            AssertOnlyTheseZonesCarryMinionSpawns(map,
                "top-blue-approach", "top-starting-battle-zone", "top-red-approach", "top-red-gate",
                "bottom-blue-gate", "bottom-blue-approach", "bottom-starting-battle-zone", "bottom-red-approach");
        }

        [Test]
        public void LavaMap_EveryLaneZoneMatchesTheReviewedPrintedMinionInventory()
        {
            var map = Load(OfficialBoardMapCatalog.LavaBridgeDoubleLane);

            AssertMinionZone(map, "top-blue-approach", 2, 1, 1, 3, 1, 1);
            AssertMinionZone(map, "top-starting-battle-zone", 2, 1, 1, 2, 1, 1);
            AssertMinionZone(map, "top-red-approach", 3, 1, 1, 2, 1, 1);
            AssertMinionZone(map, "bottom-blue-approach", 2, 1, 1, 3, 1, 1);
            AssertMinionZone(map, "bottom-starting-battle-zone", 2, 1, 1, 2, 1, 1);
            AssertMinionZone(map, "bottom-red-approach", 3, 1, 1, 2, 1, 1);
            AssertOnlyTheseZonesCarryMinionSpawns(map,
                "top-blue-approach", "top-starting-battle-zone", "top-red-approach",
                "bottom-blue-approach", "bottom-starting-battle-zone", "bottom-red-approach");
        }

        [Test]
        public void LavaMap_BottomEdgeMarkerIsTheThirdRedMeleePointInBottomRedApproach()
        {
            var map = Load(OfficialBoardMapCatalog.LavaBridgeDoubleLane);
            var spawn = map.SpawnPoints.Single(point =>
                point.Id.Value == "red-melee-minion-14");
            var space = map.Spaces.Single(candidate => candidate.Id.Equals(spawn.SpaceId));

            Assert.That(spawn.TeamId, Is.EqualTo("red"));
            Assert.That(spawn.UnitType, Is.EqualTo("melee-minion"));
            Assert.That(space.Coordinate, Is.EqualTo(new HexCoordinate(-13, 16)));
            Assert.That(space.ZoneId.Value.Value, Is.EqualTo("bottom-red-approach"));
        }

        private static BoardMapDefinition Load(MapDefinitionReference reference) =>
            OfficialBoardMapCatalog.Load().Load(reference);

        private static int ZoneSize(BoardMapDefinition map, string zoneId) =>
            map.Spaces.Count(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                space.ZoneId.Value.Equals(new ZoneId(zoneId)));

        private static void AssertMinionZone(
            BoardMapDefinition map,
            string zoneId,
            int redMelee,
            int redRanged,
            int redHeavy,
            int blueMelee,
            int blueRanged,
            int blueHeavy)
        {
            var zone = new ZoneId(zoneId);
            Assert.That(Count("red", "melee-minion"), Is.EqualTo(redMelee), zoneId);
            Assert.That(Count("red", "ranged-minion"), Is.EqualTo(redRanged), zoneId);
            Assert.That(Count("red", "heavy-minion"), Is.EqualTo(redHeavy), zoneId);
            Assert.That(Count("blue", "melee-minion"), Is.EqualTo(blueMelee), zoneId);
            Assert.That(Count("blue", "ranged-minion"), Is.EqualTo(blueRanged), zoneId);
            Assert.That(Count("blue", "heavy-minion"), Is.EqualTo(blueHeavy), zoneId);

            int Count(string team, string type) => map.SpawnPoints.Count(spawn =>
                spawn.TeamId == team && spawn.UnitType == type &&
                map.GetZoneForSpace(spawn.SpaceId).Id.Equals(zone));
        }

        private static void AssertOnlyTheseZonesCarryMinionSpawns(
            BoardMapDefinition map, params string[] expectedZoneIds)
        {
            var actual = map.SpawnPoints
                .Where(spawn => spawn.UnitType.EndsWith("-minion"))
                .Select(spawn => map.GetZoneForSpace(spawn.SpaceId).Id.Value)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            Assert.That(actual, Is.EqualTo(expectedZoneIds.OrderBy(value => value).ToArray()));
        }

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
