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

            var hero = new HeroId("map-test-hero");
            var playable = definition.PlayableCoordinates.First();
            var authority = new AdjacentMoveAuthority(definition,
                new[] { new HeroOccupancy(hero, playable) }, Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>());
            Assert.That(authority.SelectedMap, Is.EqualTo(definition.Reference));
            Assert.That(authority.BoardCoordinates, Is.EquivalentTo(definition.PlayableCoordinates));
            Assert.That(authority.BoardCoordinates, Has.None.EqualTo(
                definition.Spaces.First(value => value.Kind == BoardMapSpaceKind.Terrain).Coordinate));
        }

        private static BoardMapDefinition CreateMap(string mapId, int revision)
        {
            var zone = new BoardMapZone(new ZoneId("shared-zone"), "Shared", "lane", null, false, true);
            var space = new BoardMapSpace(new SpaceId("q0_r0"), new HexCoordinate(0, 0),
                BoardMapSpaceKind.Playable, zone.Id);
            var spawn = new BoardMapSpawnPoint(new SpawnPointId("shared-spawn"), space.Id, "red", "hero");
            return new BoardMapDefinition(new MapId(mapId), revision, mapId,
                new[] { space }, new[] { zone }, new[] { spawn });
        }
    }
}
