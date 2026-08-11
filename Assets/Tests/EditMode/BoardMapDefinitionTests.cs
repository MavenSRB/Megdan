using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class BoardMapDefinitionTests
    {
        [Test]
        public void Catalog_ResolvesExactMapRevisionAndKeepsReusedLocalIdentitiesIsolated()
        {
            var alpha = CreateMap("synthetic-alpha", 1);
            var beta = CreateMap("synthetic-beta", 1);
            var catalog = new BoardMapCatalog(new[] { alpha, beta });

            Assert.That(catalog.Load(alpha.Reference), Is.SameAs(alpha));
            Assert.That(catalog.Load(beta.Reference), Is.SameAs(beta));
            Assert.That(alpha.Spaces.Single().Id, Is.EqualTo(beta.Spaces.Single().Id));
            Assert.That(alpha.Zones.Single().Id, Is.EqualTo(beta.Zones.Single().Id));
            Assert.That(alpha.SpawnPoints.Single().Id, Is.EqualTo(beta.SpawnPoints.Single().Id));
            Assert.That(alpha.Reference, Is.Not.EqualTo(beta.Reference));

            var missing = Assert.Throws<BoardMapLoadException>(() => catalog.Load(
                new MapDefinitionReference(new MapId("missing"), 1)));
            Assert.That(missing.Reason, Is.EqualTo(BoardMapLoadRejectionReason.MissingMap));
            var revision = Assert.Throws<BoardMapLoadException>(() => catalog.Load(
                new MapDefinitionReference(alpha.Id, 2)));
            Assert.That(revision.Reason, Is.EqualTo(BoardMapLoadRejectionReason.ContentRevisionMismatch));
        }

        [Test]
        public void OfficialResource_LoadsReviewedImmutableDefinitionAndBoardUsesPlayableCoordinatesOnly()
        {
            var definition = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            Assert.That(definition.Id.Value, Is.EqualTo("official-single-lane"));
            Assert.That(definition.ContentRevision, Is.EqualTo(1));
            Assert.That(definition.Spaces.Count, Is.EqualTo(292));
            Assert.That(definition.Spaces.Count(value => value.Kind == BoardMapSpaceKind.Playable), Is.EqualTo(210));
            Assert.That(definition.Spaces.Count(value => value.Kind == BoardMapSpaceKind.Terrain), Is.EqualTo(82));
            Assert.That(definition.Zones.Count, Is.EqualTo(7));
            Assert.That(definition.SpawnPoints.Count, Is.EqualTo(40));
            Assert.That(definition.Zones.Select(value => value.Id.Value), Is.EquivalentTo(new[]
            {
                "red-throne", "red-approach", "starting-battle-zone", "blue-approach",
                "blue-throne", "top-jungle", "bottom-jungle"
            }));
            Assert.That(definition.Spaces.Where(value => value.Kind == BoardMapSpaceKind.Playable)
                .All(value => value.ZoneId.HasValue), Is.True);
            Assert.That(definition.Spaces.Where(value => value.Kind == BoardMapSpaceKind.Playable)
                .GroupBy(value => value.ZoneId.Value.Value)
                .ToDictionary(group => group.Key, group => group.Count()), Is.EquivalentTo(
                new System.Collections.Generic.Dictionary<string, int>
                {
                    ["red-throne"] = 19,
                    ["red-approach"] = 41,
                    ["starting-battle-zone"] = 60,
                    ["blue-approach"] = 41,
                    ["blue-throne"] = 19,
                    ["top-jungle"] = 15,
                    ["bottom-jungle"] = 15
                }));

            var hero = new HeroId("map-test-hero");
            var playable = definition.PlayableCoordinates.First();
            var authority = new AdjacentMoveAuthority(definition,
                new[] { new HeroOccupancy(hero, playable) }, Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>());
            Assert.That(authority.SelectedMap, Is.EqualTo(definition.Reference));
            Assert.That(authority.BoardCoordinates, Is.EquivalentTo(definition.PlayableCoordinates));
            Assert.That(authority.BoardCoordinates, Has.None.EqualTo(
                definition.Spaces.First(value => value.Kind == BoardMapSpaceKind.Terrain).Coordinate));
        }

        [Test]
        public void OfficialResource_DerivesCompleteSymmetricZoneRelationshipsFromSharedBorders()
        {
            var definition = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var expectedAdjacency = new[]
            {
                "blue-approach|blue-throne",
                "blue-approach|starting-battle-zone",
                "blue-throne|bottom-jungle",
                "bottom-jungle|starting-battle-zone",
                "red-approach|red-throne",
                "red-approach|starting-battle-zone",
                "red-throne|top-jungle",
                "starting-battle-zone|top-jungle"
            };

            var actualAdjacency = definition.Zones
                .SelectMany(zone => definition.GetAdjacentZoneIds(zone.Id)
                    .Where(adjacent => string.CompareOrdinal(zone.Id.Value, adjacent.Value) < 0)
                    .Select(adjacent => $"{zone.Id.Value}|{adjacent.Value}"))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(actualAdjacency, Is.EqualTo(expectedAdjacency));

            var sameZoneSpaces = definition.Spaces
                .Where(value => value.ZoneId.HasValue && value.ZoneId.Value.Equals(new ZoneId("red-throne")))
                .Take(2)
                .ToArray();
            Assert.That(definition.GetZoneRelationship(sameZoneSpaces[0].Id, sameZoneSpaces[1].Id),
                Is.EqualTo(ZoneRelationship.Same));
            Assert.That(definition.GetZoneRelationship(sameZoneSpaces[1].Id, sameZoneSpaces[0].Id),
                Is.EqualTo(ZoneRelationship.Same));

            var redThrone = new ZoneId("red-throne");
            var redApproach = new ZoneId("red-approach");
            var blueThrone = new ZoneId("blue-throne");
            Assert.That(definition.GetZoneRelationship(redThrone, redApproach), Is.EqualTo(ZoneRelationship.Adjacent));
            Assert.That(definition.GetZoneRelationship(redApproach, redThrone), Is.EqualTo(ZoneRelationship.Adjacent));
            Assert.That(definition.GetZoneRelationship(redThrone, blueThrone), Is.EqualTo(ZoneRelationship.NonAdjacent));
            Assert.That(definition.GetZoneRelationship(blueThrone, redThrone), Is.EqualTo(ZoneRelationship.NonAdjacent));
            Assert.Throws<ArgumentException>(() => definition.GetZoneRelationship(new ZoneId("missing"), redThrone));
            Assert.Throws<ArgumentException>(() => definition.GetZoneForSpace(
                definition.Spaces.First(value => value.Kind == BoardMapSpaceKind.Terrain).Id));
        }

        [Test]
        public void OfficialResource_RepresentsValidatedOrderedLaneBetweenThrones()
        {
            var definition = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);

            Assert.That(definition.OrderedLaneZoneIds.Select(value => value.Value), Is.EqualTo(new[]
            {
                "red-throne", "red-approach", "starting-battle-zone", "blue-approach", "blue-throne"
            }));
            Assert.That(definition.GetZone(definition.OrderedLaneZoneIds.First()).IsThrone, Is.True);
            Assert.That(definition.GetZone(definition.OrderedLaneZoneIds.Last()).IsThrone, Is.True);
            Assert.That(definition.OrderedLaneZoneIds.Zip(definition.OrderedLaneZoneIds.Skip(1),
                (first, second) => definition.GetZoneRelationship(first, second))
                .All(value => value == ZoneRelationship.Adjacent), Is.True);
        }

        private static BoardMapDefinition CreateMap(string mapId, int revision)
        {
            var zone = new BoardMapZone(new ZoneId("shared-zone"), "Shared", "lane", null, false, true);
            var space = new BoardMapSpace(new SpaceId("q0_r0"), new HexCoordinate(0, 0),
                BoardMapSpaceKind.Playable, zone.Id);
            var spawn = new BoardMapSpawnPoint(new SpawnPointId("shared-spawn"), space.Id, "red", "hero");
            return new BoardMapDefinition(new MapId(mapId), revision, mapId,
                new[] { space }, new[] { zone }, new[] { spawn }, Array.Empty<ZoneId>());
        }
    }
}
