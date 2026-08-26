using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
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
            OpenConfiguredScene();
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
            OpenConfiguredScene();
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
            OpenConfiguredScene();
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
            OpenConfiguredScene();
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

            // Milestone 8.11a. The generated colouring is what a board with no artwork under it says,
            // and it is still exactly what it was. With the official face present there is nothing for
            // it to say: every zone the colours stood for is painted on the board itself, so each tile
            // is drawn with one fully transparent fill instead. Both readings are asserted, because a
            // clean checkout has no artwork and this project's own does.
            if (mapView.IsUnderlayActive)
            {
                Assert.That(
                    playableMaterialsByZone.Values.Concat(terrainMaterials).Distinct().Count(),
                    Is.EqualTo(1),
                    "One suppressed fill serves every space while the artwork is underneath.");
                Assert.That(terrainMaterials.Single().color.a, Is.EqualTo(0f).Within(1f / 255f),
                    "and it is fully transparent, so the zone a player reads is the painted one.");
                return;
            }

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
            OpenConfiguredScene();
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
            OpenConfiguredScene();
            RebuildOfficialMap();
            var tiles = Object.FindObjectsByType<HexTileView>();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();

            // Milestone 8.25: six, not two, and all different. This case used to see two mirrored
            // Wasps because the rebuild helper built its own mirrored setup; it rebuilds from what the
            // scene composed now, which is rulebook p.5's six-player line-up.
            Assert.That(heroes, Has.Length.EqualTo(6));
            Assert.That(
                heroes.Select(hero => hero.HeroContentId?.Value),
                Is.EquivalentTo(new[] { "wasp", "xargatha", "tigerclaw", "arien", "brogan", "sabina" }),
                "Six different heroes, and Wasp exactly once.");
            Assert.That(heroes.Select(hero => hero.Coordinate).Distinct().Count(), Is.EqualTo(6));
            Assert.That(heroes.Count(hero => hero.TeamSide == TeamSide.Red), Is.EqualTo(3));
            Assert.That(heroes.Count(hero => hero.TeamSide == TeamSide.Blue), Is.EqualTo(3));

            var redHero = heroes.Single(hero => hero.HeroContentId.Equals(new HeroContentId("wasp")));
            var blueHero = heroes.Single(hero => hero.HeroContentId.Equals(new HeroContentId("arien")));
            Assert.That(redHero.TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(blueHero.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(redHero.HeroDisplayName, Is.EqualTo("Wasp the Warmaiden"));
            Assert.That(blueHero.HeroDisplayName, Is.EqualTo("Arien the Tidemaster"));
            Assert.That(redHero.ConfiguredSpawnPointId, Is.EqualTo(new SpawnPointId("red-hero-01")));
            Assert.That(blueHero.ConfiguredSpawnPointId, Is.EqualTo(new SpawnPointId("blue-hero-01")));
            var definition = Object.FindAnyObjectByType<PlaceholderTestGridView>().RenderedDefinition;
            foreach (var hero in heroes)
            {
                // Every seat stands on the space its own configured spawn point occupies, which is what
                // rulebook p.6 setup step 9 places a hero miniature on.
                Assert.That(hero.Coordinate, Is.EqualTo(definition.Spaces.Single(space => space.Id.Equals(
                    definition.SpawnPoints
                        .Single(spawn => spawn.Id.Equals(hero.ConfiguredSpawnPointId.Value)).SpaceId))
                    .Coordinate));
            }

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
            OpenConfiguredScene();
            var mapView = RebuildOfficialMap();
            var setup = mapView.RenderedSetup;
            var minions = Object.FindObjectsByType<StandardMinionView>();

            Assert.That(setup, Is.Not.Null);
            Assert.That(setup.WaveCount, Is.EqualTo(3));
            // Five since milestone 8.25: rulebook p.6 step 5's table gives a Quick game four Life
            // counters per team at four players and five at six, and the composed setup is the
            // six-player one.
            Assert.That(setup.Teams, Has.All.Matches<QuickGameTeamSetupState>(team => team.LifeCount == 5));
            Assert.That(minions, Has.Length.EqualTo(12));
            Assert.That(minions.Select(view => view.Occupancy.MinionId).Distinct().Count(), Is.EqualTo(12));
            Assert.That(minions.Select(view => view.Occupancy.OccupiedSpace).Distinct().Count(), Is.EqualTo(12));
            Assert.That(minions, Has.All.Matches<StandardMinionView>(view =>
                view.GetComponent<SpriteRenderer>().sprite != null &&
                mapView.RenderedDefinition.GetZoneForSpace(view.Occupancy.OccupiedSpace.SpaceId).IsStartingBattleZone &&
                // Milestone 8.11a: up-screen rather than world +Z, which are the same direction until
                // the official artwork puts the framing camera on the board's own axes.
                Vector3.Angle(
                    view.transform.TransformDirection(Vector3.up),
                    BoardChoiceCue.ScreenUpInWorld) < Tolerance));
            Assert.That(minions.Where(view => view.Occupancy.TeamSide == TeamSide.Red),
                Has.All.Matches<StandardMinionView>(view => view.GetComponent<SpriteRenderer>().sprite.name.StartsWith("red-")));
            Assert.That(minions.Where(view => view.Occupancy.TeamSide == TeamSide.Blue),
                Has.All.Matches<StandardMinionView>(view => view.GetComponent<SpriteRenderer>().sprite.name.StartsWith("blue-")));
        }

        [Test]
        public void Scene_RemovesLegacyPlaceholderObstaclesFromOfficialMap()
        {
            OpenConfiguredScene();
            RebuildOfficialMap();
            var obstacles = Object.FindObjectsByType<PlaceholderObstacleView>();

            Assert.That(obstacles, Is.Empty);
        }





        [Test]
        public void Scene_ContainsInputSystemPointerSelectionPath()
        {
            OpenConfiguredScene();
            var selectors = Object.FindObjectsByType<PrimitiveHeroPointerSelector>();
            var minionSelectors = Object.FindObjectsByType<StandardMinionPointerSelector>();
            var inputs = Object.FindObjectsByType<PrimitiveHeroPointerInput>();
            var compositionRoots = Object.FindObjectsByType<MatchSceneCompositionRoot>();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();

            // Six since milestone 8.25: the scene composes rulebook p.5's six-player setup, and a seat
            // the scene authors no view for gets a generated view and a generated selector. The seats
            // are named as well as counted, because a count alone cannot say whether an authored view
            // for a seat this configuration does not deal is still standing on the board.
            Assert.That(
                heroes.Select(hero => hero.Occupancy.HeroId.Value).OrderBy(value => value, System.StringComparer.Ordinal),
                Is.EqualTo(compositionRoots[0].Runtime.Setup.Heroes
                    .Select(hero => hero.HeroId.Value)
                    .OrderBy(value => value, System.StringComparer.Ordinal)),
                "Exactly the configured seats are drawn.");
            Assert.That(
                selectors
                    .Select(selector => selector.GetComponent<PrimitiveHeroView>()?.Occupancy.HeroId.Value)
                    .OrderBy(value => value, System.StringComparer.Ordinal),
                Is.EqualTo(compositionRoots[0].Runtime.Setup.Heroes
                    .Select(hero => hero.HeroId.Value)
                    .OrderBy(value => value, System.StringComparer.Ordinal)),
                "One pointer selector per drawn seat, and none belonging to a seat this configuration " +
                "does not deal.");
            Assert.That(minionSelectors, Has.Length.EqualTo(12));
            Assert.That(minionSelectors, Has.All.Matches<StandardMinionPointerSelector>(selector =>
                selector.GetComponent<StandardMinionView>() != null &&
                selector.GetComponent<SphereCollider>() != null));
            Assert.That(inputs, Has.Length.EqualTo(1));
            Assert.That(compositionRoots, Has.Length.EqualTo(1));
            Assert.That(compositionRoots[0].Runtime, Is.Not.Null);
            Assert.That(compositionRoots[0].Session, Is.Not.Null);
            // One hundred and eight, which is rulebook p.6's eighteen cards per hero across milestone
            // 8.25's six seats. Thirty of them are the five dealt into each hand: milestone 6.2d had
            // made that twelve by dealing milestone 6.2's Cache card beside the five ordinary ones,
            // because nothing else could put a token on this board, and Blue Sentinel and Blue Feint
            // carry those capabilities now. Milestone 7.5 deals the remaining thirteen per hero into
            // the deck, because p.15's Upgrade a Card step searches it.
            Assert.That(compositionRoots[0].Session.Projection.Cards, Has.Count.EqualTo(108));
            Assert.That(
                compositionRoots[0].Session.Projection.Cards.Count(card =>
                    card.State.Location == CardLocation.Hand),
                Is.EqualTo(30));
            Assert.That(compositionRoots[0].Session.Projection.Heroes, Has.Count.EqualTo(6));
            Assert.That(inputs[0].EventCamera, Is.Not.Null);
            // The input is handed every selector the scene authors as well as every one composition
            // generated: an authored seat this configuration does not deal keeps its selector, its
            // GameObject is deactivated, and the pointer therefore reaches nothing through it.
            Assert.That(inputs[0].Selectors, Is.SupersetOf(selectors));
            Assert.That(
                inputs[0].Selectors.Where(selector => selector.gameObject.activeInHierarchy),
                Is.EquivalentTo(selectors));
            Assert.That(heroes, Has.All.Matches<PrimitiveHeroView>(hero =>
                hero.GetComponent<CapsuleCollider>() != null &&
                hero.GetComponent<PrimitiveHeroPointerSelector>() != null));
        }

        [Test]
        public void Scene_RebuildReconcilesReloadedAndDeliberatelyReplacedMinionBindings()
        {
            OpenConfiguredScene();
            var mapView = RebuildOfficialMap();
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var originalViews = Object.FindObjectsByType<StandardMinionView>()
                .OrderBy(value => value.BoundMinionId.Value)
                .ToArray();
            var transientSetupField = ReflectedMember.RequiredInstanceField(
                typeof(StandardMinionView),
                "occupancy");

            Assert.That(root, Is.Not.Null);
            Assert.That(pointerInput, Is.Not.Null);
            Assert.That(originalViews, Has.Length.EqualTo(12));
            foreach (var view in originalViews)
            {
                transientSetupField.SetValue(view, null);
            }

            Assert.That(originalViews, Has.All.Matches<StandardMinionView>(view =>
                view.Occupancy == null && view.BoundMinionId.IsDefined));
            Assert.DoesNotThrow(() => mapView.Rebuild(mapView.RenderedDefinition, root.Runtime.Setup));

            var retainedViews = Object.FindObjectsByType<StandardMinionView>()
                .OrderBy(value => value.BoundMinionId.Value)
                .ToArray();
            Assert.That(retainedViews, Is.EqualTo(originalViews),
                "A backup/domain reload that only loses transient setup objects must retain the stable views.");

            var replacedState = retainedViews[0].Occupancy;
            Object.DestroyImmediate(retainedViews[0].gameObject);
            var replacementObject = new GameObject("Deliberate Bound Minion Replacement");
            replacementObject.transform.SetParent(mapView.transform, false);
            var replacement = replacementObject.AddComponent<StandardMinionView>();
            replacement.Configure(replacedState, mapView.OuterRadius);

            Assert.DoesNotThrow(() => mapView.Rebuild(mapView.RenderedDefinition, root.Runtime.Setup));
            Assert.DoesNotThrow(() => root.Runtime.RefreshAll(),
                "A session refresh after replacement must not touch the destroyed cached component.");

            var currentViews = Object.FindObjectsByType<StandardMinionView>();
            var currentSelectors = Object.FindObjectsByType<StandardMinionPointerSelector>();
            Assert.That(currentViews, Has.Length.EqualTo(12));
            Assert.That(currentViews.Select(value => value.BoundMinionId).Distinct().Count(), Is.EqualTo(12));
            Assert.That(currentSelectors, Has.Length.EqualTo(12));
            Assert.That(currentSelectors.Select(value => value.GetComponent<StandardMinionView>()),
                Is.EquivalentTo(currentViews));
            Assert.That(currentSelectors.Select(value => value.GetComponent<SphereCollider>()).Distinct().Count(),
                Is.EqualTo(12));
            Assert.That(root.Runtime.BoardView.MinionViews, Is.EquivalentTo(currentViews));
            Assert.That(pointerInput.MinionSelectors, Is.EquivalentTo(currentSelectors));

            MinionId submitted = default;
            root.Runtime.BoardView.BindIntentHandlers(
                _ => false,
                id => { submitted = id; return true; },
                _ => false);
            var replacementSelector = replacement.GetComponent<StandardMinionPointerSelector>();
            Assert.That(replacementSelector, Is.Not.Null);
            Assert.That(replacementSelector.TrySelectFromPointerRay(
                new Ray(replacement.transform.position + (Vector3.up * 10f), Vector3.down)), Is.True);
            Assert.That(submitted, Is.EqualTo(replacedState.MinionId));
        }

        [Test]
        public void Scene_ContainsTopRightAuthoritativeTieBreakerIndicator()
        {
            OpenConfiguredScene();
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            var side = root.Session.Projection.TieBreakerState.RevealedSide;
            var indicatorRect = root.Hud.TieBreakerIndicatorRect;

            Assert.That(side, Is.EqualTo(TeamSide.Red).Or.EqualTo(TeamSide.Blue));
            Assert.That(root.Hud.Header.Text, Does.Contain("TIE BREAKER"));
            Assert.That(
                root.Hud.Header.Text,
                Does.Contain(side == TeamSide.Red ? "[R] RED" : "[B] BLUE"));
            Assert.That(indicatorRect.xMax, Is.EqualTo(Screen.width - 12f).Within(0.01f));
            Assert.That(indicatorRect.yMin, Is.EqualTo(12f).Within(0.01f));
        }

        [Test]
        public void Scene_SelectingSecondaryMovementOfferHighlightsAuthoritativeCardDestinations()
        {
            OpenConfiguredScene();
            RebuildOfficialMap();
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            var runtime = root.Runtime;
            var tiles = Object.FindObjectsByType<HexTileView>();

            foreach (var hand in runtime.HandsView.Model.Hands)
            {
                Assert.That(runtime.SelectCommitmentCard(hand.Cards[0].Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            }

            if (runtime.Projection.PendingInitiativeChoice != null)
            {
                Assert.That(runtime.SubmitInitiativeChoice(
                    runtime.Projection.PendingInitiativeChoice.CandidateCardInstanceIds[0]), Is.True);
            }

            // Milestone 8.25: whichever of the six acts first may print both a primary and a secondary
            // Movement, so the offer this case is about is named by its printed origin rather than by
            // being the only one there is.
            ActionDefinition ActionOf(ActionOffer offer) =>
                runtime.Projection.Definitions.Single(definition =>
                        definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));
            var movementOffers = runtime.Projection.AvailableTurnActions
                .Where(offer => ActionOf(offer).Kind == ActionKind.Movement)
                .ToArray();
            Assert.That(movementOffers, Is.Not.Empty,
                "The acting hero's card offers a Movement, or this case has no subject.");
            var secondary = movementOffers
                .Where(offer => ActionOf(offer).Origin == ActionDefinitionOrigin.PrintedSecondary)
                .ToArray();
            var movement = secondary.Length > 0 ? secondary[0] : movementOffers[0];
            Assert.That(runtime.SubmitAction(movement.ActionDefinitionId), Is.True);

            var highlightedCoordinates = tiles
                .Where(tile => tile.IsHighlighted)
                .Select(tile => tile.Coordinate)
                .ToArray();

            var expectedDestinations = runtime.Projection.PendingMovementChoice.LegalDestinations
                .Select(destination => destination.Destination)
                .ToArray();
            Assert.That(highlightedCoordinates, Is.EquivalentTo(expectedDestinations));

            Assert.That(
                tiles.Single(tile => tile.Coordinate.Equals(runtime.Projection.PendingMovementChoice.Origin))
                    .IsHighlighted,
                Is.False);
            // Milestone 8.25: five other heroes stand on this board rather than one, and an occupied
            // space is not a destination whichever of them is standing on it.
            var actor = runtime.Projection.SelectedActor.ActorId;
            foreach (var other in runtime.Projection.Heroes.Where(hero => !hero.HeroId.Equals(actor)))
            {
                Assert.That(highlightedCoordinates, Has.No.Member(other.Coordinate),
                    $"{other.HeroId} is standing on {other.Coordinate}.");
            }
        }

        /// <summary>
        /// Opens the configured scene and explicitly composes it. Since milestone 6.1e the
        /// composition root does not compose on an edit-mode domain reload, so these scene tests
        /// request composition rather than depending on that implicit side effect.
        /// </summary>
        private static void OpenConfiguredScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null, "The configured scene must contain a composition root.");
            root.Compose();
        }

        /// <summary>
        /// Rebuilds the board from the setup the scene actually composed.
        ///
        /// <para>Milestone 8.25. It used to rebuild from a mirrored one-versus-one setup it built
        /// itself, which agreed with the composition only for as long as the composition was that
        /// board. Once the scene composes rulebook p.5's six seats, rebuilding from a different setup
        /// leaves the scene holding both sets of pieces - which is a state no composition produces and
        /// which every later case in the run then measures.</para>
        /// </summary>
        private static PlaceholderTestGridView RebuildOfficialMap()
        {
            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root?.Runtime, Is.Not.Null, "The scene must have composed before a rebuild.");
            var definition = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            mapView.Rebuild(definition, root.Runtime.Setup);
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
