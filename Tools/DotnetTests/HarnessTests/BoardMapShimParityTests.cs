using System.Linq;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.Harness
{
    // Guards the shim boundary: Shims/BoardMapCatalogShim.cs stands in for the
    // Unity loader in Presentation/Board/BoardMapJsonLoader.cs, so this fixture
    // proves the shim yields the same authoritative facts about the official map
    // that the Unity-side BoardMapDefinitionTests prove through Resources.Load.
    // If either loader or the map changes, one of the two fixtures goes red.
    [TestFixture]
    public sealed class BoardMapShimParityTests
    {
        [Test]
        public void ShimLoadedOfficialMap_MatchesAuthoritativeInventory()
        {
            var catalog = OfficialBoardMapCatalog.Load();
            var map = catalog.Load(OfficialBoardMapCatalog.SelectedMap);

            Assert.That(map.Reference.ContentRevision, Is.EqualTo(1));
            Assert.That(map.Spaces.Count, Is.EqualTo(292));
            Assert.That(map.Spaces.Count(space => space.Kind == GuardsDigital.Core.Board.BoardMapSpaceKind.Playable), Is.EqualTo(210));
            Assert.That(map.Spaces.Count(space => space.Kind == GuardsDigital.Core.Board.BoardMapSpaceKind.Terrain), Is.EqualTo(82));
            Assert.That(map.Zones.Count, Is.EqualTo(7));
            Assert.That(map.Zones.Count(zone => zone.IsStartingBattleZone), Is.EqualTo(1));
            Assert.That(map.SpawnPoints.Count, Is.EqualTo(40));
            Assert.That(map.SpawnPoints.Count(point => point.TeamId == "red"), Is.EqualTo(20));
            Assert.That(map.SpawnPoints.Count(point => point.TeamId == "blue"), Is.EqualTo(20));
            Assert.That(map.OrderedLaneZoneIds.Select(value => value.Value), Is.EqualTo(new[]
            {
                "red-throne", "red-approach", "starting-battle-zone", "blue-approach", "blue-throne"
            }));
        }
    }
}
