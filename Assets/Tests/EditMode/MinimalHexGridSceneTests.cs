using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MinimalHexGridSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Milestone1_2_HexGrid.unity";
        private const int OfficialMapCellCount = 292;
        private const int OfficialPlayableCount = 210;
        private const int OfficialTerrainCount = 82;
        private const float Tolerance = 0.0001f;

        [Test]
        public void Scene_RebuildsCompletePlaceholderMapFromSelectedDefinitionAtDerivedPositions()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var mapView = RebuildOfficialMap();
            var tiles = Object.FindObjectsByType<HexTileView>();
            var definition = mapView.RenderedDefinition;

            Assert.That(definition.Reference, Is.EqualTo(OfficialBoardMapCatalog.SelectedMap));
            Assert.That(tiles, Has.Length.EqualTo(OfficialMapCellCount));
            Assert.That(tiles.Select(tile => tile.Coordinate),
                Is.EquivalentTo(definition.Spaces.Select(space => space.Coordinate)));
            Assert.That(tiles.Count(tile => tile.IsPlayable), Is.EqualTo(OfficialPlayableCount));
            Assert.That(tiles.Count(tile => tile.SpaceKind == BoardMapSpaceKind.Terrain),
                Is.EqualTo(OfficialTerrainCount));

            foreach (var tile in tiles)
            {
                var expectedPosition = HexWorldLayout.ToWorld(tile.Coordinate, tile.OuterRadius);
                Assert.That(
                    Vector3.Distance(tile.transform.localPosition, expectedPosition),
                    Is.LessThan(Tolerance),
                    $"Tile {tile.Coordinate} is not at its derived logical position.");
                Assert.That(tile.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
                Assert.That(tile.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.Null);
            }
        }

        [Test]
        public void Scene_PresentationMatchesSelectedDefinitionClassificationsAndSpawnPoints()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var mapView = RebuildOfficialMap();
            var tiles = Object.FindObjectsByType<HexTileView>();
            var markers = Object.FindObjectsByType<BoardMapSpawnMarkerView>();
            var definition = mapView.RenderedDefinition;
            var tilesByCoordinate = tiles.ToDictionary(tile => tile.Coordinate);

            foreach (var space in definition.Spaces)
            {
                var tile = tiles.Single(candidate => candidate.SpaceId.Equals(space.Id));
                Assert.That(tile.Coordinate, Is.EqualTo(space.Coordinate));
                Assert.That(tile.SpaceKind, Is.EqualTo(space.Kind));
                Assert.That(tile.ZoneId, Is.EqualTo(space.ZoneId));
            }

            Assert.That(markers, Has.Length.EqualTo(40));
            Assert.That(markers.Select(marker => marker.SpawnPointId),
                Is.EquivalentTo(definition.SpawnPoints.Select(spawn => spawn.Id)));
            Assert.That(markers.All(marker => definition.SpawnPoints.Any(spawn =>
                spawn.Id.Equals(marker.SpawnPointId) &&
                spawn.SpaceId.Equals(marker.SpaceId) &&
                spawn.TeamId == marker.TeamId &&
                spawn.UnitType == marker.UnitType)), Is.True);
            Assert.That(markers.GroupBy(marker => (marker.TeamId, marker.UnitType)).Count(), Is.EqualTo(8));
            Assert.That(markers.Where(marker => marker.UnitType == "hero"),
                Has.All.Matches<BoardMapSpawnMarkerView>(marker =>
                    marker.MarkerShape == SpawnMarkerShape.HeroCircle &&
                    marker.GetComponent<LineRenderer>().positionCount == 16));
            Assert.That(markers.Where(marker => marker.UnitType == "melee-minion"),
                Has.All.Matches<BoardMapSpawnMarkerView>(marker =>
                    marker.MarkerShape == SpawnMarkerShape.MeleeDiamond &&
                    marker.GetComponent<LineRenderer>().positionCount == 4));
            var rangedMarkers = markers.Where(marker => marker.UnitType == "ranged-minion").ToArray();
            Assert.That(rangedMarkers,
                Has.All.Matches<BoardMapSpawnMarkerView>(marker =>
                    marker.MarkerShape == SpawnMarkerShape.RangedVerticalLine &&
                    marker.GetComponent<LineRenderer>().positionCount == 2 &&
                    !marker.GetComponent<LineRenderer>().loop));
            foreach (var marker in rangedMarkers)
            {
                var renderer = marker.GetComponent<LineRenderer>();
                var start = renderer.GetPosition(0);
                var end = renderer.GetPosition(1);
                Assert.That(start.x, Is.EqualTo(0f).Within(Tolerance));
                Assert.That(end.x, Is.EqualTo(0f).Within(Tolerance));
                Assert.That(start.z, Is.LessThan(0f));
                Assert.That(end.z, Is.GreaterThan(0f));
                Assert.That(start.z, Is.EqualTo(-end.z).Within(Tolerance));
                Assert.That(start.y, Is.EqualTo(end.y).Within(Tolerance));
            }
            Assert.That(markers.Where(marker => marker.UnitType == "heavy-minion"),
                Has.All.Matches<BoardMapSpawnMarkerView>(marker =>
                    marker.MarkerShape == SpawnMarkerShape.HeavyHexagon &&
                    marker.GetComponent<LineRenderer>().positionCount == 6));
            Assert.That(markers.Where(marker => marker.TeamId == "red"),
                Has.All.Matches<BoardMapSpawnMarkerView>(marker =>
                    marker.GetComponent<LineRenderer>().sharedMaterial.color.r >
                    marker.GetComponent<LineRenderer>().sharedMaterial.color.b));
            Assert.That(markers.Where(marker => marker.TeamId == "blue"),
                Has.All.Matches<BoardMapSpawnMarkerView>(marker =>
                    marker.GetComponent<LineRenderer>().sharedMaterial.color.b >
                    marker.GetComponent<LineRenderer>().sharedMaterial.color.r));
            Assert.That(tiles.Sum(tile => tile.Coordinate.GetNeighbors().Count(tilesByCoordinate.ContainsKey)) / 2,
                Is.EqualTo(809));
            Assert.That(tiles.Where(tile => tile.IsPlayable).Sum(tile => tile.Coordinate.GetNeighbors().Count(
                    coordinate => tilesByCoordinate.TryGetValue(coordinate, out var neighbor) && neighbor.IsPlayable)) / 2,
                Is.EqualTo(530));
        }

        [Test]
        public void Scene_UsesOneFixedTopDownCameraWithPointerInputAndNoCameraController()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cameras = Object.FindObjectsByType<Camera>();

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].name, Is.EqualTo("Fixed Camera"));
            Assert.That(cameras[0].GetComponents<MonoBehaviour>(), Has.All.TypeOf<PrimitiveHeroPointerInput>());
            Assert.That(cameras[0].transform.position.y, Is.GreaterThan(0f));
            RebuildOfficialMap();
            Assert.That(cameras[0].orthographic, Is.True);
            Assert.That(Vector3.Angle(cameras[0].transform.forward, Vector3.down), Is.LessThan(Tolerance));
            var tiles = Object.FindObjectsByType<HexTileView>();
            Assert.That(tiles.All(tile =>
            {
                var viewport = cameras[0].WorldToViewportPoint(tile.transform.position);
                return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            }), Is.True);
        }

        [Test]
        public void Scene_UsesSharedJungleColorAndDistinctNonJungleAndTerrainColors()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var mapView = RebuildOfficialMap();
            var tiles = Object.FindObjectsByType<HexTileView>();
            var playableMaterialsByZone = tiles.Where(tile => tile.IsPlayable)
                .GroupBy(tile => tile.ZoneId.Value)
                .ToDictionary(group => group.Key, group => group.Select(tile =>
                    tile.GetComponent<MeshRenderer>().sharedMaterial).Distinct().Single());
            var terrainMaterials = tiles.Where(tile => !tile.IsPlayable)
                .Select(tile => tile.GetComponent<MeshRenderer>().sharedMaterial).Distinct().ToArray();

            Assert.That(playableMaterialsByZone.Keys,
                Is.EquivalentTo(mapView.RenderedDefinition.Zones.Select(zone => zone.Id)));
            Assert.That(playableMaterialsByZone[new ZoneId("bottom-jungle")].color,
                Is.EqualTo(playableMaterialsByZone[new ZoneId("top-jungle")].color));
            Assert.That(playableMaterialsByZone.Values.Select(material => material.color).Distinct().Count(),
                Is.EqualTo(6));
            Assert.That(terrainMaterials, Has.Length.EqualTo(1));
            Assert.That(playableMaterialsByZone.Values.Select(material => material.color),
                Has.No.Member(terrainMaterials[0].color));
        }

        [Test]
        public void Scene_UsesOneDarkerGrayClosedEdgeForEveryTile()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RebuildOfficialMap();
            var tiles = Object.FindObjectsByType<HexTileView>();
            var darkestFill = tiles.Min(tile => tile.GetComponent<MeshRenderer>().sharedMaterial.color.grayscale);
            var edgeRenderers = tiles.Select(tile => tile.GetComponentInChildren<LineRenderer>()).ToArray();

            Assert.That(edgeRenderers, Has.All.Not.Null);
            Assert.That(edgeRenderers.All(edge => edge.loop && edge.positionCount == 6), Is.True);
            Assert.That(edgeRenderers.Select(edge => edge.sharedMaterial).Distinct().Count(), Is.EqualTo(1));

            var edgeColor = edgeRenderers[0].sharedMaterial.color;
            Assert.That(Mathf.Abs(edgeColor.r - edgeColor.g), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(edgeColor.g - edgeColor.b), Is.LessThan(0.05f));
            Assert.That(edgeColor.grayscale, Is.LessThan(darkestFill - 0.05f));
        }

        [Test]
        public void Scene_ContainsVisibleRedAndBlueHeroesAtConfiguredTeamHeroSpawnPoints()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RebuildOfficialMap();
            var tiles = Object.FindObjectsByType<HexTileView>();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();

            Assert.That(heroes, Has.Length.EqualTo(2));
            Assert.That(
                heroes.Select(hero => hero.Occupancy.HeroId.Value),
                Is.EquivalentTo(new[]
                {
                    PlaceholderOneVsOneParticipants.RedHeroIdValue,
                    PlaceholderOneVsOneParticipants.BlueHeroIdValue
                }));
            Assert.That(heroes.Select(hero => hero.Coordinate).Distinct().Count(), Is.EqualTo(2));

            var redHero = heroes.Single(hero =>
                hero.Occupancy.HeroId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blueHero = heroes.Single(hero =>
                hero.Occupancy.HeroId.Value == PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            Assert.That(redHero.TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(blueHero.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(redHero.HeroContentId, Is.EqualTo(new HeroContentId("wasp")));
            Assert.That(blueHero.HeroContentId, Is.EqualTo(new HeroContentId("wasp")));
            Assert.That(redHero.HeroDisplayName, Is.EqualTo("Wasp"));
            Assert.That(blueHero.HeroDisplayName, Is.EqualTo("Wasp"));
            Assert.That(redHero.ConfiguredSpawnPointId, Is.EqualTo(new SpawnPointId("red-hero-01")));
            Assert.That(blueHero.ConfiguredSpawnPointId, Is.EqualTo(new SpawnPointId("blue-hero-01")));
            var definition = Object.FindAnyObjectByType<PlaceholderTestGridView>().RenderedDefinition;
            Assert.That(redHero.Coordinate, Is.EqualTo(definition.Spaces.Single(space => space.Id.Equals(
                definition.SpawnPoints.Single(spawn => spawn.Id.Equals(redHero.ConfiguredSpawnPointId.Value)).SpaceId)).Coordinate));
            Assert.That(blueHero.Coordinate, Is.EqualTo(definition.Spaces.Single(space => space.Id.Equals(
                definition.SpawnPoints.Single(spawn => spawn.Id.Equals(blueHero.ConfiguredSpawnPointId.Value)).SpaceId)).Coordinate));

            foreach (var hero in heroes)
            {
                hero.EnsurePrimitiveVisual();
                var expectedPosition = HexWorldLayout.ToWorld(hero.Occupancy.Coordinate, hero.OuterRadius) +
                    (Vector3.up * hero.VerticalOffset);
                Assert.That(tiles.Select(tile => tile.Coordinate), Does.Contain(hero.Occupancy.Coordinate));
                Assert.That(Vector3.Distance(hero.transform.localPosition, expectedPosition), Is.LessThan(Tolerance));
                Assert.That(hero.GetComponent<MeshRenderer>().enabled, Is.True);
                Assert.That(hero.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            }

            var redRenderer = redHero.GetComponent<MeshRenderer>();
            var blueRenderer = blueHero.GetComponent<MeshRenderer>();
            Assert.That(redRenderer.sharedMaterial.color.r, Is.GreaterThan(redRenderer.sharedMaterial.color.b));
            Assert.That(blueRenderer.sharedMaterial.color.b, Is.GreaterThan(blueRenderer.sharedMaterial.color.r));
            Assert.That(
                redHero.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.Not.EqualTo(blueHero.GetComponent<MeshFilter>().sharedMesh.vertexCount));
        }

        [Test]
        public void Scene_ProjectsConfiguredQuickGameCountersAndTwelveReviewedStandardMinions()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var mapView = RebuildOfficialMap();
            var setup = mapView.RenderedSetup;
            var minions = Object.FindObjectsByType<StandardMinionView>();

            Assert.That(setup, Is.Not.Null);
            Assert.That(setup.WaveCount, Is.EqualTo(3));
            Assert.That(setup.Teams, Has.All.Matches<QuickGameTeamSetupState>(team => team.LifeCount == 4));
            Assert.That(minions, Has.Length.EqualTo(12));
            Assert.That(minions.Select(view => view.SetupState.MinionId).Distinct().Count(), Is.EqualTo(12));
            Assert.That(minions.Select(view => view.SetupState.OccupiedSpace).Distinct().Count(), Is.EqualTo(12));
            Assert.That(minions, Has.All.Matches<StandardMinionView>(view =>
                view.GetComponent<SpriteRenderer>().sprite != null &&
                mapView.RenderedDefinition.GetZoneForSpace(view.SetupState.OccupiedSpace.SpaceId).IsStartingBattleZone &&
                Vector3.Angle(view.transform.TransformDirection(Vector3.up), Vector3.forward) < Tolerance));
            Assert.That(minions.Where(view => view.SetupState.TeamSide == TeamSide.Red),
                Has.All.Matches<StandardMinionView>(view => view.GetComponent<SpriteRenderer>().sprite.name.StartsWith("red-")));
            Assert.That(minions.Where(view => view.SetupState.TeamSide == TeamSide.Blue),
                Has.All.Matches<StandardMinionView>(view => view.GetComponent<SpriteRenderer>().sprite.name.StartsWith("blue-")));
        }

        [Test]
        public void Scene_RemovesLegacyPlaceholderObstaclesFromOfficialMap()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RebuildOfficialMap();
            var obstacles = Object.FindObjectsByType<PlaceholderObstacleView>();

            Assert.That(obstacles, Is.Empty);
        }





        [Test]
        public void Scene_ContainsInputSystemPointerSelectionPath()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controllers = Object.FindObjectsByType<HeroSelectionController>();
            var selectors = Object.FindObjectsByType<PrimitiveHeroPointerSelector>();
            var inputs = Object.FindObjectsByType<PrimitiveHeroPointerInput>();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();

            Assert.That(controllers, Has.Length.EqualTo(1));
            Assert.That(selectors, Has.Length.EqualTo(2));
            Assert.That(inputs, Has.Length.EqualTo(1));
            Assert.That(controllers[0].MovementAllowance, Is.GreaterThan(1));
            Assert.That(controllers[0].IsCardDrivenMovementEnabled, Is.True);
            Assert.That(controllers[0].VisibleCardInstances.Count, Is.EqualTo(5));
            Assert.That(controllers[0].VisibleCardInstances.All(card =>
                card.OwnerId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue), Is.True);
            Assert.That(controllers[0].VisibleCardInstances.All(card =>
                card.Id.Value.StartsWith("red-")), Is.True);
            Assert.That(controllers[0].OtherHeroViews.Count, Is.EqualTo(1));
            Assert.That(controllers[0].HeroOccupancies.Count, Is.EqualTo(2));
            Assert.That(selectors, Has.All.Matches<PrimitiveHeroPointerSelector>(selector =>
                selector.SelectionController == controllers[0]));
            Assert.That(inputs[0].EventCamera, Is.Not.Null);
            Assert.That(inputs[0].Selectors.Count, Is.EqualTo(2));
            Assert.That(inputs[0].Selectors, Is.EquivalentTo(selectors));
            Assert.That(heroes, Has.All.Matches<PrimitiveHeroView>(hero =>
                hero.GetComponent<CapsuleCollider>() != null &&
                hero.GetComponent<PrimitiveHeroPointerSelector>() != null));
        }

        [Test]
        public void Scene_ContainsTopRightAuthoritativeTieBreakerIndicator()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<HeroSelectionController>();
            var side = controller.CurrentTieBreakerState.RevealedSide;
            var indicatorRect = controller.TieBreakerIndicatorRect;

            Assert.That(controller.IsCardDrivenMovementEnabled, Is.True);
            Assert.That(side, Is.EqualTo(TeamSide.Red).Or.EqualTo(TeamSide.Blue));
            Assert.That(controller.TieBreakerIndicatorText, Does.Contain("TIE BREAKER"));
            Assert.That(
                controller.TieBreakerIndicatorText,
                Does.Contain(side == TeamSide.Red ? "[R] RED" : "[B] BLUE"));
            Assert.That(indicatorRect.xMax, Is.EqualTo(Screen.width - 12f).Within(0.01f));
            Assert.That(indicatorRect.yMin, Is.EqualTo(12f).Within(0.01f));
        }

        [Test]
        public void Scene_SelectingSecondaryMovementOfferHighlightsAuthoritativeCardDestinations()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RebuildOfficialMap();
            var controller = Object.FindAnyObjectByType<HeroSelectionController>();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();
            var hero = heroes.Single(candidate =>
                candidate.Occupancy.HeroId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var tiles = Object.FindObjectsByType<HexTileView>();

            var selectedCard = controller.VisibleCardInstances[0];
            Assert.That(controller.TrySelectCard(selectedCard.Id), Is.True);
            Assert.That(controller.TrySelectAction(
                new ActionDefinitionId("secondary-movement")), Is.True);

            var highlightedCoordinates = tiles
                .Where(tile => tile.IsHighlighted)
                .Select(tile => tile.Coordinate)
                .ToArray();

            Assert.That(controller.IsHeroSelected, Is.True);
            var obstacles = Object.FindObjectsByType<PlaceholderObstacleView>();
            var expectedDestinations = MovementReachabilityQuery.FindReachableDestinations(
                    hero.Occupancy,
                    tiles.Where(tile => tile.IsPlayable).Select(tile => tile.Coordinate),
                    heroes.Select(candidate => candidate.Occupancy),
                    obstacles.Select(obstacle => obstacle.Occupancy),
                    controller.LastCardMovementAllowance)
                .Select(destination => destination.Destination)
                .ToArray();

            Assert.That(controller.HighlightedCoordinates, Is.EquivalentTo(expectedDestinations));
            Assert.That(highlightedCoordinates, Is.EquivalentTo(expectedDestinations));
            Assert.That(tiles.Single(tile => tile.Coordinate.Equals(hero.Coordinate)).IsHighlighted, Is.False);
            Assert.That(
                highlightedCoordinates,
                Has.No.Member(heroes.Single(candidate => candidate != hero).Coordinate));
        }

        private static PlaceholderTestGridView RebuildOfficialMap()
        {
            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            var definition = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                PlaceholderOneVsOneParticipants.Create(),
                definition,
                TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Red));
            mapView.Rebuild(definition, setup);
            return mapView;
        }

        private static int HexDistance(HexCoordinate first, HexCoordinate second)
        {
            var firstS = -first.Q - first.R;
            var secondS = -second.Q - second.R;
            return (Mathf.Abs(first.Q - second.Q) + Mathf.Abs(first.R - second.R) + Mathf.Abs(firstS - secondS)) / 2;
        }
    }
}
