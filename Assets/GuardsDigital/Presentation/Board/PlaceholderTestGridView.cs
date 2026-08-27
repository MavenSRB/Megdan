using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// The two pieces of screen space in which whole playable hexes may stand. The upper region is
    /// the ordinary board area between the HUD columns. The lower-right region is the otherwise dead
    /// space beneath the resolved Blue rail and to the right of the five-card hand. Their overlap makes
    /// one L-shaped safe surface; decorative artwork is deliberately not constrained to either piece.
    /// Rectangles use UI Toolkit's top-left panel coordinates.
    /// </summary>
    public readonly struct BoardCameraSafeLayout
    {
        public BoardCameraSafeLayout(Rect upperRegion, Rect lowerRightRegion, Vector2 panelViewport)
        {
            UpperRegion = upperRegion;
            LowerRightRegion = lowerRightRegion;
            PanelViewport = panelViewport;
        }

        public Rect UpperRegion { get; }
        public Rect LowerRightRegion { get; }
        public Vector2 PanelViewport { get; }

        public bool IsValid =>
            PanelViewport.x > 0f &&
            PanelViewport.y > 0f &&
            UpperRegion.width > 0f &&
            UpperRegion.height > 0f;
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PlaceholderTestGridView : MonoBehaviour
    {
        private static readonly Color JungleColor = new Color(0.38f, 0.66f, 0.42f, 1f);

        private static readonly Color[] ZoneColors =
        {
            new Color(0.72f, 0.34f, 0.30f, 1f),
            new Color(0.82f, 0.52f, 0.30f, 1f),
            new Color(0.78f, 0.72f, 0.32f, 1f),
            new Color(0.34f, 0.63f, 0.78f, 1f),
            new Color(0.30f, 0.42f, 0.72f, 1f),
            JungleColor,
            JungleColor
        };

        [SerializeField] private float outerRadius = HexWorldLayout.DefaultOuterRadius;
        [SerializeField] private float cameraPadding = 1.5f;

        /// <summary>
        /// Extra world-space air beyond the exact projected hex vertices in the HUD-aware fit. Visible
        /// UI already keeps a four-unit screen gutter; this smaller allowance protects antialiased
        /// edges without forfeiting the lower-right room the notched layout exists to use.
        /// </summary>
        public const float ReservedPlayfieldCameraPadding = 0.02f;

        [SerializeField] private Camera sceneCamera;
        [SerializeField] private PrimitiveHeroView[] heroViews;

        private readonly Dictionary<ZoneId, Material> zoneMaterials = new Dictionary<ZoneId, Material>();
        private readonly List<HexTileView> currentTiles = new List<HexTileView>();
        private readonly List<StandardMinionView> currentMinionViews = new List<StandardMinionView>();
        private readonly List<PlacedTokenView> currentTokenViews = new List<PlacedTokenView>();
        private readonly List<PlaceholderObstacleView> currentObstacles = new List<PlaceholderObstacleView>();
        private BoardMapDefinition renderedDefinition;
        private QuickGameSetupState renderedSetup;
        private Material terrainMaterial;
        private Material edgeMaterial;
        private Material redSpawnMaterial;
        private Material blueSpawnMaterial;
        private Material suppressedFillMaterial;
        private Mesh generatedTileMesh;
        private MapDefinitionReference materialMapReference;
        private OfficialBoardUnderlayView underlayView;
        private BoardCameraSafeLayout configuredCameraLayout;
        private bool hasConfiguredCameraViewport;
        private Camera cachedFrameCamera;
        private BoardMapDefinition cachedFrameDefinition;
        private BoardCameraSafeLayout cachedFrameLayout;
        private bool cachedFrameUsedConfiguredLayout;
        private bool cachedFrameUsedUnderlay;
        private float cachedFrameOuterRadius;
        private float cachedFramePadding;
        private float cachedFrameAspect;
        private float cachedFrameOrthographicSize;
        private Vector3 cachedFramePosition;
        private Quaternion cachedFrameRotation;
        private bool hasCachedCameraFrame;

        public float OuterRadius => outerRadius;
        public BoardMapDefinition RenderedDefinition => renderedDefinition;
        public QuickGameSetupState RenderedSetup => renderedSetup;
        public IReadOnlyCollection<HexCoordinate> Coordinates => renderedDefinition != null
            ? renderedDefinition.Spaces.Select(space => space.Coordinate).ToArray()
            : System.Array.Empty<HexCoordinate>();
        public int OwnedNativeResourceCount => zoneMaterials.Count +
            (terrainMaterial != null ? 1 : 0) +
            (edgeMaterial != null ? 1 : 0) +
            (redSpawnMaterial != null ? 1 : 0) +
            (blueSpawnMaterial != null ? 1 : 0) +
            (suppressedFillMaterial != null ? 1 : 0) +
            (generatedTileMesh != null ? 1 : 0) +
            (underlayView != null ? underlayView.OwnedNativeResourceCount : 0);

        /// <summary>
        /// Milestone 8.11a. True while the official board face is being drawn underneath, which is the
        /// one condition that suppresses the generated fills and debug edges. It is false whenever the
        /// private artwork is absent, and false for every map that is not the reviewed official one.
        /// </summary>
        public bool IsUnderlayActive => underlayView != null && underlayView.HasArtwork;

        public OfficialBoardUnderlayView UnderlayView => underlayView;

        /// <summary>
        /// Which world direction the framing camera treats as up. The board's own up while the official
        /// face is underneath, and world +Z otherwise, which is what it always was.
        /// </summary>
        public Vector3 ScreenUpInWorld =>
            IsUnderlayActive ? OfficialBoardArtwork.BoardUpInWorld : Vector3.forward;

        /// <summary>
        /// The views this rebuild actually produced, excluding any object it retired. Consumers must
        /// use these instead of GetComponentsInChildren: in Play Mode a retired object is destroyed
        /// with the deferred UnityEngine.Object.Destroy and is still returned by a child search until
        /// the end of the frame, so re-discovery can admit doomed objects into authoritative state.
        /// </summary>
        public IReadOnlyList<HexTileView> CurrentTiles => currentTiles;
        public IReadOnlyList<StandardMinionView> CurrentMinionViews => currentMinionViews;
        public IReadOnlyList<PlacedTokenView> CurrentTokenViews => currentTokenViews;
        public IReadOnlyList<PlaceholderObstacleView> CurrentObstacles => currentObstacles;

        public event System.Action<IReadOnlyList<StandardMinionView>> ConfiguredMinionViewsChanged;

        public void ConfigureSceneBindings(Camera camera, IEnumerable<PrimitiveHeroView> heroes)
        {
            sceneCamera = camera;
            ConfigureHeroViews(heroes);
        }

        public void ConfigureHeroViews(IEnumerable<PrimitiveHeroView> heroes)
        {
            heroViews = (heroes ?? throw new System.ArgumentNullException(nameof(heroes)))
                .Where(value => value != null)
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Gives the full-screen board camera the HUD's L-shaped safe surface. The fit is made from the
        /// actual hex inventory rather than the artwork's square: broad rows remain above the cards,
        /// while the board's tapered lower end may grow into the empty area to their right. Artwork may
        /// extend anywhere behind the HUD; only complete hexes are constrained.
        /// </summary>
        public void ConfigureCameraViewport(BoardCameraSafeLayout layout)
        {
            if (sceneCamera == null || !layout.IsValid)
            {
                return;
            }

            var fullCameraRect = new Rect(0f, 0f, 1f, 1f);

            if (hasConfiguredCameraViewport &&
                Approximately(configuredCameraLayout, layout) &&
                Approximately(sceneCamera.rect, fullCameraRect))
            {
                return;
            }

            configuredCameraLayout = layout;
            hasConfiguredCameraViewport = true;
            sceneCamera.rect = fullCameraRect;
            if (renderedDefinition != null)
            {
                FrameWholeMap(sceneCamera, renderedDefinition);
            }
        }

        public void Rebuild(BoardMapDefinition definition)
        {
            RebuildMap(definition, true);
        }

        private void RebuildMap(BoardMapDefinition definition, bool clearConfiguredMinions)
        {
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            if (outerRadius <= 0f || float.IsNaN(outerRadius) || float.IsInfinity(outerRadius))
            {
                throw new System.InvalidOperationException("The placeholder map requires a finite positive outer radius.");
            }

            renderedDefinition = definition;
            renderedSetup = null;
            // Before the materials, because whether the artwork is underneath is what decides which
            // fill a tile is configured with and whether its debug edge is drawn at all.
            EnsureUnderlay(definition);
            // Every board-facing cue turns to match the framing, so a marker reads level with the
            // screen rather than level with a world axis the camera no longer uses. Set here rather
            // than beside the camera, because a rebuild with no camera bound still places cues.
            BoardChoiceCue.SetScreenUp(ScreenUpInWorld);
            EnsureMaterials(definition);

            var spacesByCoordinate = definition.Spaces.ToDictionary(space => space.Coordinate);
            var tilesByCoordinate = new Dictionary<HexCoordinate, HexTileView>();
            HexTileView templateTile = null;
            foreach (var tile in GetComponentsInChildren<HexTileView>(true))
            {
                templateTile ??= tile;
                if (!spacesByCoordinate.ContainsKey(tile.Coordinate) || tilesByCoordinate.ContainsKey(tile.Coordinate))
                {
                    DestroyObject(tile.gameObject);
                    continue;
                }

                ConfigureTile(tile, spacesByCoordinate[tile.Coordinate]);
                tilesByCoordinate.Add(tile.Coordinate, tile);
            }

            foreach (var space in definition.Spaces.OrderBy(space => space.Coordinate.Q).ThenBy(space => space.Coordinate.R))
            {
                if (!tilesByCoordinate.ContainsKey(space.Coordinate))
                {
                    tilesByCoordinate.Add(space.Coordinate, CreateTile(space, templateTile));
                }
            }

            // Record the surviving tiles only. Retired tiles are excluded even though a deferred
            // Destroy leaves them reachable through a child search for the rest of the frame.
            currentTiles.Clear();
            currentTiles.AddRange(tilesByCoordinate.Values);

            EnsureSpawnMarkers(definition);
            RemoveLegacyObstacles();
            if (clearConfiguredMinions)
            {
                // A token view belongs to a match rather than to a map, so it is retired exactly where
                // a configured minion view is - the map-only rebuild, which runs before any runtime
                // holds a reference to one. The setup path deliberately keeps both.
                ClearTokenViews();
                ClearConfiguredMinions();
            }
            FrameWholeMap(sceneCamera, definition);
        }

        public void Rebuild(BoardMapDefinition definition, QuickGameSetupState setupState)
        {
            RebuildMap(definition, false);
            ApplySetup(setupState);
        }

        public void ApplySetup(QuickGameSetupState setupState)
        {
            if (setupState == null) throw new System.ArgumentNullException(nameof(setupState));
            if (renderedDefinition == null || !renderedDefinition.Reference.Equals(setupState.SelectedMap))
            {
                throw new System.InvalidOperationException(
                    $"Setup map '{setupState.SelectedMap}' does not match the rendered map '{renderedDefinition?.Reference}'.");
            }

            renderedSetup = setupState;
            PlaceConfiguredHeroes(renderedDefinition, setupState);
            EnsureConfiguredMinions(setupState);
        }

        public void EnsureGrid()
        {
            if (renderedDefinition != null)
            {
                Rebuild(renderedDefinition);
            }
        }

        private void ConfigureTile(HexTileView tile, BoardMapSpace space)
        {
            tile.gameObject.name = $"{space.Kind} {space.Id.Value}";
            tile.Configure(space, outerRadius, GetSpaceMaterial(space));
            var edge = tile.GetComponentInChildren<LineRenderer>(true);
            if (edge != null)
            {
                ConfigureEdge(edge);
            }
        }

        /// <summary>
        /// Generated presentation is deliberately left with default hide flags. `HideFlags.DontSave`
        /// would keep it out of the saved scene but also hides it from `Object.FindObjectsByType`,
        /// which the configured presentation and its regressions rely on. Scene cleanliness is
        /// instead guaranteed by not composing in edit mode at all; see `MatchSceneCompositionRoot`.
        /// </summary>
        private static GameObject CreateGeneratedObject(string name, Transform parent)
        {
            var generated = new GameObject(name);
            generated.transform.SetParent(parent, false);
            return generated;
        }

        private HexTileView CreateTile(BoardMapSpace space, HexTileView templateTile)
        {
            var tileObject = CreateGeneratedObject($"{space.Kind} {space.Id.Value}", transform);

            var meshFilter = tileObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = templateTile != null && templateTile.GetComponent<MeshFilter>() != null
                ? templateTile.GetComponent<MeshFilter>().sharedMesh
                : GetOrCreateGeneratedTileMesh();

            tileObject.AddComponent<MeshRenderer>();
            var tile = tileObject.AddComponent<HexTileView>();
            tile.Configure(space, outerRadius, GetSpaceMaterial(space));

            var edgeObject = CreateGeneratedObject("Debug Edge", tileObject.transform);
            ConfigureEdge(edgeObject.AddComponent<LineRenderer>());
            return tile;
        }

        private void ConfigureEdge(LineRenderer lineRenderer)
        {
            // Milestone 8.11a: the debug perimeter is what the artwork replaces most directly, so it is
            // switched off rather than restyled while the face is underneath. The renderer stays on the
            // tile, configured and ready, so removing the artwork restores it on the next rebuild.
            lineRenderer.enabled = !IsUnderlayActive;
            lineRenderer.sharedMaterial = edgeMaterial;
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 6;
            lineRenderer.startWidth = 0.045f;
            lineRenderer.endWidth = 0.045f;
            lineRenderer.numCornerVertices = 2;
            for (var side = 0; side < 6; side++)
            {
                var angle = Mathf.Deg2Rad * (30f + (side * 60f));
                lineRenderer.SetPosition(side, new Vector3(
                    Mathf.Cos(angle) * outerRadius,
                    0.015f,
                    Mathf.Sin(angle) * outerRadius));
            }
        }

        private void EnsureSpawnMarkers(BoardMapDefinition definition)
        {
            var markersById = new Dictionary<SpawnPointId, BoardMapSpawnMarkerView>();
            foreach (var marker in GetComponentsInChildren<BoardMapSpawnMarkerView>(true))
            {
                if (markersById.ContainsKey(marker.SpawnPointId))
                {
                    DestroyObject(marker.gameObject);
                }
                else
                {
                    markersById.Add(marker.SpawnPointId, marker);
                }
            }


            var spacesById = definition.Spaces.ToDictionary(space => space.Id);
            foreach (var spawnPoint in definition.SpawnPoints.OrderBy(spawnPoint => spawnPoint.Id.Value))
            {
                var space = spacesById[spawnPoint.SpaceId];
                if (!markersById.TryGetValue(spawnPoint.Id, out var marker))
                {
                    var markerObject = CreateGeneratedObject($"Spawn {spawnPoint.Id.Value}", transform);
                    marker = markerObject.AddComponent<BoardMapSpawnMarkerView>();
                    markerObject.AddComponent<LineRenderer>();
                }

                var targetObject = marker.gameObject;
                targetObject.name = $"Spawn {spawnPoint.Id.Value}";
                targetObject.transform.SetParent(transform, false);
                targetObject.transform.localPosition = HexWorldLayout.ToWorld(space.Coordinate, outerRadius);
                marker.Configure(
                    spawnPoint,
                    outerRadius,
                    string.Equals(spawnPoint.TeamId, "red", System.StringComparison.OrdinalIgnoreCase)
                        ? redSpawnMaterial
                        : blueSpawnMaterial);

                // Milestone 8.11a, at the user's request: the official face already prints every spawn,
                // hero and minion alike, so the generated ring is a second copy of something the artwork
                // says better. It is hidden the same way the debug edge is - the view stays configured
                // with its reviewed shape and team colour, which is what the presentation tests read and
                // what a rebuild without the artwork puts straight back on screen.
                var markerRenderer = marker.GetComponent<LineRenderer>();
                if (markerRenderer != null)
                {
                    markerRenderer.enabled = !IsUnderlayActive;
                }

                markersById.Remove(spawnPoint.Id);
            }

            foreach (var staleMarker in markersById.Values)
            {
                DestroyObject(staleMarker.gameObject);
            }
        }

        private void RemoveLegacyObstacles()
        {
            foreach (var obstacle in GetComponentsInChildren<PlaceholderObstacleView>(true))
            {
                DestroyObject(obstacle.gameObject);
            }

            // Every legacy obstacle is retired unconditionally, so the surviving set is empty.
            currentObstacles.Clear();
        }

        private void PlaceConfiguredHeroes(BoardMapDefinition definition, QuickGameSetupState setupState)
        {
            var views = heroViews ?? System.Array.Empty<PrimitiveHeroView>();
            foreach (var setupHero in setupState.Heroes)
            {
                var view = views.SingleOrDefault(hero => hero.Occupancy.HeroId.Equals(setupHero.HeroId));
                if (view == null)
                {
                    throw new System.InvalidOperationException(
                        $"No primitive hero view is bound to configured hero '{setupHero.HeroId}'.");
                }

                view.gameObject.SetActive(true);
                view.ConfigureFromSetup(setupHero, definition, outerRadius);
            }

            // Milestone 8.21. A seat this configuration did not deal is not on the board, so its view
            // is not drawn. The scene authors one view per seat it can compose, and a configuration
            // chooses how many of them play - which is what lets the same scene stage a Brogan fixture
            // with an ally beside him and still compose the one-hero-per-team match a player loads,
            // with nothing standing where the second Blue seat would be.
            //
            // It is deactivation rather than destruction for the reason the tiles are rebuilt rather
            // than rebuilt from nothing: the view is authored content the next composition may claim,
            // and a destroyed one could not be. Minion and token views are generated, which is why
            // they are retired instead.
            foreach (var view in views)
            {
                if (view != null &&
                    setupState.Heroes.All(hero => !hero.HeroId.Equals(view.Occupancy.HeroId)))
                {
                    view.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureConfiguredMinions(QuickGameSetupState setupState)
        {
            var viewsById = new Dictionary<MinionId, StandardMinionView>();
            foreach (var view in GetComponentsInChildren<StandardMinionView>(true))
            {
                var boundId = view.BoundMinionId;
                if (!view.enabled || !boundId.IsDefined || viewsById.ContainsKey(boundId))
                {
                    RetireMinionView(view);
                }
                else
                {
                    viewsById.Add(boundId, view);
                }
            }

            var currentViews = new List<StandardMinionView>(setupState.Minions.Count);
            foreach (var setupMinion in setupState.Minions.OrderBy(value => value.MinionId.Value))
            {
                if (!viewsById.TryGetValue(setupMinion.MinionId, out var view))
                {
                    var minionObject = CreateGeneratedObject(string.Empty, transform);
                    view = minionObject.AddComponent<StandardMinionView>();
                }

                view.Configure(GuardsDigital.Core.Units.Minions.MinionOccupancy.FromSetup(setupMinion), outerRadius);
                currentViews.Add(view);
                viewsById.Remove(setupMinion.MinionId);
            }

            foreach (var staleView in viewsById.Values)
            {
                RetireMinionView(staleView);
            }

            currentMinionViews.Clear();
            currentMinionViews.AddRange(currentViews);
            ConfiguredMinionViewsChanged?.Invoke(System.Array.AsReadOnly(currentViews.ToArray()));
        }

        private void ClearConfiguredMinions()
        {
            foreach (var minion in GetComponentsInChildren<StandardMinionView>(true))
            {
                RetireMinionView(minion);
            }

            currentMinionViews.Clear();
            ConfiguredMinionViewsChanged?.Invoke(System.Array.Empty<StandardMinionView>());
        }

        /// <summary>
        /// Milestone 6.2d: builds a view for a token the board has not drawn before, at this grid's own
        /// outer radius, and hands it back already configured for that token.
        ///
        /// A token has no declared roster to build views from, the way <see cref="EnsureConfiguredMinions"/>
        /// builds one view per setup minion. Its identity is minted when an effect places it, so the
        /// board asks for a view the first time it sees one and pools it from then on; this grid keeps
        /// them for the same reason it keeps tiles and minions, so that nothing generated at runtime is
        /// re-discovered through a child search.
        /// </summary>
        public PlacedTokenView CreateTokenView(GuardsDigital.Core.Components.PlacedToken token)
        {
            if (token == null) throw new System.ArgumentNullException(nameof(token));
            var tokenObject = CreateGeneratedObject(string.Empty, transform);
            var view = tokenObject.AddComponent<PlacedTokenView>();
            view.Configure(token, outerRadius);
            currentTokenViews.Add(view);
            return view;
        }

        private void ClearTokenViews()
        {
            foreach (var token in currentTokenViews)
            {
                if (token == null)
                {
                    continue;
                }

                token.gameObject.SetActive(false);
                DestroyObject(token.gameObject);
            }

            currentTokenViews.Clear();
        }

        private static void RetireMinionView(StandardMinionView view)
        {
            if (view == null)
            {
                return;
            }

            view.enabled = false;
            view.gameObject.SetActive(false);
            DestroyObject(view.gameObject);
        }

        /// <summary>
        /// Milestone 8.11a. Puts the official board face under the reviewed official map when a prepared
        /// copy is present, and retires it in every other case.
        /// </summary>
        /// <remarks>
        /// Two conditions, and both are ordinary rather than exceptional. The registration in
        /// <see cref="OfficialBoardArtwork"/> is for one map at one revision, so no other definition may
        /// receive it - a synthetic grid in a test would be drawn under a picture of somewhere else. And
        /// the artwork itself is private and ignored, so a clean checkout resolves nothing and the board
        /// keeps exactly the generated presentation it had before this milestone.
        /// </remarks>
        private void EnsureUnderlay(BoardMapDefinition definition)
        {
            var isOfficialMap = definition.Reference.Equals(OfficialBoardMapCatalog.SelectedMap);
            if (!isOfficialMap || !OfficialBoardArtwork.TryLoadTexture(out var artwork))
            {
                RetireUnderlay();
                return;
            }

            if (underlayView == null)
            {
                underlayView = GetComponentInChildren<OfficialBoardUnderlayView>(true);
            }

            if (underlayView == null)
            {
                var underlayObject = CreateGeneratedObject("Official Board Underlay", transform);
                underlayView = underlayObject.AddComponent<OfficialBoardUnderlayView>();
            }

            underlayView.Configure(artwork, outerRadius);
        }

        private void RetireUnderlay()
        {
            underlayView ??= GetComponentInChildren<OfficialBoardUnderlayView>(true);
            if (underlayView == null) return;
            underlayView.ReleaseOwnedResources();
            DestroyObject(underlayView.gameObject);
            underlayView = null;
        }

        private void EnsureMaterials(BoardMapDefinition definition)
        {
            terrainMaterial ??= CreateMaterial("Terrain Debug", new Color(0.20f, 0.23f, 0.26f, 1f));
            edgeMaterial ??= CreateMaterial("Map Edge Debug", new Color(0.10f, 0.11f, 0.13f, 1f));
            suppressedFillMaterial ??= CreateTransparentMaterial("Suppressed Tile Fill");
            redSpawnMaterial ??= CreateMaterial("Red Spawn Debug", new Color(1f, 0.18f, 0.12f, 1f));
            blueSpawnMaterial ??= CreateMaterial("Blue Spawn Debug", new Color(0.12f, 0.55f, 1f, 1f));
            if (materialMapReference.Equals(definition.Reference) && zoneMaterials.Count == definition.Zones.Count)
            {
                return;
            }

            DestroyZoneMaterials();
            for (var index = 0; index < definition.Zones.Count; index++)
            {
                var zone = definition.Zones[index];
                zoneMaterials.Add(zone.Id, CreateMaterial($"Zone {zone.DisplayName} Debug", ZoneColors[index % ZoneColors.Length]));
            }

            materialMapReference = definition.Reference;
        }

        private Mesh GetOrCreateGeneratedTileMesh()
        {
            if (generatedTileMesh == null)
            {
                generatedTileMesh = FlatHexMeshBuilder.Create(outerRadius);
                generatedTileMesh.name = "Owned Placeholder Grid Hex Mesh";
                generatedTileMesh.hideFlags = HideFlags.DontSave;
            }

            return generatedTileMesh;
        }

        private void DestroyZoneMaterials()
        {
            foreach (var material in zoneMaterials.Values)
            {
                DestroyOwnedResource(material);
            }

            zoneMaterials.Clear();
            materialMapReference = default;
        }

        /// <summary>
        /// Milestone 8.11a. With the official face underneath, every tile is configured with one fully
        /// transparent fill instead of its zone colour: the artwork already says which zone a space is
        /// in, and painting a flat colour over it would hide the thing the milestone is for. The tile
        /// keeps its renderer and its material, so <see cref="HexTileView.SetHighlighted"/> still swaps
        /// to an opaque highlight and back with no knowledge of any of this.
        /// </summary>
        private Material GetSpaceMaterial(BoardMapSpace space) =>
            IsUnderlayActive
                ? suppressedFillMaterial
                : space.Kind == BoardMapSpaceKind.Terrain
                    ? terrainMaterial
                    : zoneMaterials[space.ZoneId.Value];

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new System.InvalidOperationException(
                    $"The generated board material '{name}' requires the Standard shader.");
            }

            var material = new Material(shader) { name = name, color = color };
            material.hideFlags = HideFlags.DontSave;
            return material;
        }

        /// <summary>
        /// Milestone 8.11a. The tile fill while the artwork is underneath: present and pickable, drawn
        /// at nothing at all. <see cref="BoardOverlayMaterial"/> owns the blend state, because the
        /// Standard shader is opaque until it is set and a low alpha alone would do nothing.
        /// </summary>
        private static Material CreateTransparentMaterial(string name) =>
            BoardOverlayMaterial.Create(
                name, new Color(1f, 1f, 1f, BoardOverlayMaterial.SuppressedAlpha));

        private void FrameWholeMap(Camera targetCamera, BoardMapDefinition definition)
        {
            if (targetCamera == null) return;

            // Milestone 8.11a. The framing works in the camera's own axes rather than in the world ones.
            // They are the same pair while nothing is underneath - screen right is world +X and screen up
            // is world +Z, which is exactly what this did before - and they become the board's own axes
            // once the artwork is there, so the square face reads upright. Measuring the map's extent in
            // the same basis is what keeps every space inside the viewport either way.
            var screenRight = IsUnderlayActive ? OfficialBoardArtwork.BoardRightInWorld : Vector3.right;
            var screenUp = ScreenUpInWorld;
            var aspect = hasConfiguredCameraViewport && configuredCameraLayout.PanelViewport.y > 0f
                ? configuredCameraLayout.PanelViewport.x / configuredCameraLayout.PanelViewport.y
                : targetCamera.aspect > 0f ? targetCamera.aspect : 16f / 9f;

            // A map rebuild changes tile views, not the immutable map geometry or the HUD reservation.
            // Re-solving the exact notched fit for the same inputs turns that ordinary refresh into
            // millions of interval intersections. Reapply the already proven camera frame instead;
            // any map instance, viewport, artwork basis, radius, padding or aspect change misses this
            // key and calculates a fresh frame below.
            if (hasCachedCameraFrame &&
                ReferenceEquals(cachedFrameCamera, targetCamera) &&
                ReferenceEquals(cachedFrameDefinition, definition) &&
                cachedFrameUsedConfiguredLayout == hasConfiguredCameraViewport &&
                (!hasConfiguredCameraViewport ||
                    Approximately(cachedFrameLayout, configuredCameraLayout)) &&
                cachedFrameUsedUnderlay == IsUnderlayActive &&
                Mathf.Approximately(cachedFrameOuterRadius, outerRadius) &&
                Mathf.Approximately(cachedFramePadding, cameraPadding) &&
                Mathf.Approximately(cachedFrameAspect, aspect))
            {
                targetCamera.orthographic = true;
                targetCamera.orthographicSize = cachedFrameOrthographicSize;
                targetCamera.farClipPlane = 100f;
                targetCamera.transform.position = cachedFramePosition;
                targetCamera.transform.rotation = cachedFrameRotation;
                return;
            }

            var positions = definition.Spaces
                .Select(space => HexWorldLayout.ToWorld(space.Coordinate, outerRadius))
                .ToArray();
            var playablePositions = definition.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => HexWorldLayout.ToWorld(space.Coordinate, outerRadius))
                .ToArray();
            var minimumX = positions.Min(position => Vector3.Dot(position, screenRight)) - outerRadius;
            var maximumX = positions.Max(position => Vector3.Dot(position, screenRight)) + outerRadius;
            var minimumZ = positions.Min(position => Vector3.Dot(position, screenUp)) - outerRadius;
            var maximumZ = positions.Max(position => Vector3.Dot(position, screenUp)) + outerRadius;
            var center = (screenRight * ((minimumX + maximumX) * 0.5f)) +
                (screenUp * ((minimumZ + maximumZ) * 0.5f)) +
                new Vector3(0f, 60f, 0f);
            var halfWidth = (maximumX - minimumX) * 0.5f;
            var halfDepth = (maximumZ - minimumZ) * 0.5f;
            // Milestone 8.25f2 separates the surface the camera draws from the space hexes may use.
            // A rectangle above the whole hand was safe but left the lower-right visibly empty. The
            // notched fit below uses every inventoried hex and the two safe rectangles the HUD reports:
            // the broad body stays above the cards and the tapered end grows into the open space beside
            // them. The camera remains full-screen, so the square artwork is free to pass behind UI.
            targetCamera.orthographic = true;
            if (hasConfiguredCameraViewport &&
                TryCalculateNotchedFrame(
                    playablePositions,
                    screenRight,
                    screenUp,
                    configuredCameraLayout,
                    outerRadius,
                    out var pixelsPerWorld,
                    out var projectedOrigin))
            {
                var panel = configuredCameraLayout.PanelViewport;
                targetCamera.orthographicSize = panel.y / (2f * pixelsPerWorld);
                var cameraRightCoordinate = ((panel.x * 0.5f) - projectedOrigin.x) /
                    pixelsPerWorld;
                var cameraUpCoordinate = (projectedOrigin.y - (panel.y * 0.5f)) /
                    pixelsPerWorld;
                center = (screenRight * cameraRightCoordinate) +
                    (screenUp * cameraUpCoordinate) +
                    new Vector3(0f, 60f, 0f);
            }
            else
            {
                targetCamera.orthographicSize =
                    Mathf.Max(halfDepth, halfWidth / aspect) + cameraPadding;
            }

            targetCamera.farClipPlane = 100f;
            targetCamera.transform.position = center;
            targetCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, screenUp);

            cachedFrameCamera = targetCamera;
            cachedFrameDefinition = definition;
            cachedFrameLayout = configuredCameraLayout;
            cachedFrameUsedConfiguredLayout = hasConfiguredCameraViewport;
            cachedFrameUsedUnderlay = IsUnderlayActive;
            cachedFrameOuterRadius = outerRadius;
            cachedFramePadding = cameraPadding;
            cachedFrameAspect = aspect;
            cachedFrameOrthographicSize = targetCamera.orthographicSize;
            cachedFramePosition = targetCamera.transform.position;
            cachedFrameRotation = targetCamera.transform.rotation;
            hasCachedCameraFrame = true;
        }

        /// <summary>
        /// Finds the largest uniform screen scale at which every complete hex fits in either half of
        /// the L-shaped safe layout. For a fixed scale and vertical origin, each hex contributes one or
        /// two legal horizontal intervals; intersecting those unions gives an exact placement answer.
        /// Candidate vertical origins come from the interval boundaries themselves, so this does not
        /// depend on a resolution-sensitive sampling grid.
        /// </summary>
        private static bool TryCalculateNotchedFrame(
            IReadOnlyList<Vector3> positions,
            Vector3 screenRight,
            Vector3 screenUp,
            BoardCameraSafeLayout layout,
            float radius,
            out float pixelsPerWorld,
            out Vector2 projectedOrigin)
        {
            pixelsPerWorld = 0f;
            projectedOrigin = default;
            if (positions == null || positions.Count == 0 || !layout.IsValid)
            {
                return false;
            }

            var projected = positions
                .Select(position => new Vector2(
                    Vector3.Dot(position, screenRight),
                    Vector3.Dot(position, screenUp)))
                .ToArray();
            CalculateHexScreenExtents(
                screenRight,
                screenUp,
                radius,
                out var halfRight,
                out var halfUp);

            var rightSpan = (projected.Max(point => point.x) - projected.Min(point => point.x)) +
                (2f * halfRight);
            var upSpan = (projected.Max(point => point.y) - projected.Min(point => point.y)) +
                (2f * halfUp);
            var high = Mathf.Min(
                layout.PanelViewport.x / Mathf.Max(0.001f, rightSpan),
                layout.PanelViewport.y / Mathf.Max(0.001f, upSpan));
            var low = 0f;
            var bestOrigin = default(Vector2);
            for (var iteration = 0; iteration < 20; iteration++)
            {
                var candidate = (low + high) * 0.5f;
                if (TryPlaceAtScale(
                    projected,
                    layout,
                    halfRight * candidate,
                    halfUp * candidate,
                    candidate,
                    out var origin))
                {
                    low = candidate;
                    bestOrigin = origin;
                }
                else
                {
                    high = candidate;
                }
            }

            if (low <= 0f || !TryPlaceAtScale(
                    projected,
                    layout,
                    halfRight * low,
                    halfUp * low,
                    low,
                    out bestOrigin))
            {
                return false;
            }

            pixelsPerWorld = low;
            projectedOrigin = bestOrigin;
            return true;
        }

        private static void CalculateHexScreenExtents(
            Vector3 screenRight,
            Vector3 screenUp,
            float radius,
            out float halfRight,
            out float halfUp)
        {
            halfRight = 0f;
            halfUp = 0f;
            for (var side = 0; side < 6; side++)
            {
                var angle = Mathf.Deg2Rad * (30f + (side * 60f));
                var vertex = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                halfRight = Mathf.Max(halfRight, Mathf.Abs(Vector3.Dot(vertex, screenRight)));
                halfUp = Mathf.Max(halfUp, Mathf.Abs(Vector3.Dot(vertex, screenUp)));
            }

            // The safe rectangles already retain a four-unit screen gutter from visible HUD. This
            // small world-space allowance prevents antialiased hex edges from consuming that boundary.
            halfRight += ReservedPlayfieldCameraPadding * radius;
            halfUp += ReservedPlayfieldCameraPadding * radius;
        }

        private static bool TryPlaceAtScale(
            IReadOnlyList<Vector2> projected,
            BoardCameraSafeLayout layout,
            float halfWidth,
            float halfHeight,
            float scale,
            out Vector2 origin)
        {
            origin = default;
            var verticalCandidates = new List<float>(projected.Count * 4);
            foreach (var point in projected)
            {
                AddVerticalBoundaries(verticalCandidates, layout.UpperRegion, point, halfHeight, scale);
                if (layout.LowerRightRegion.width > 0f && layout.LowerRightRegion.height > 0f)
                {
                    AddVerticalBoundaries(
                        verticalCandidates,
                        layout.LowerRightRegion,
                        point,
                        halfHeight,
                        scale);
                }
            }

            verticalCandidates.Sort();
            var firstIntervals = new Vector2[(projected.Count * 4) + 2];
            var secondIntervals = new Vector2[firstIntervals.Length];
            for (var index = 0; index < verticalCandidates.Count; index++)
            {
                if (TryFindHorizontalOrigin(
                    projected,
                    layout,
                    halfWidth,
                    halfHeight,
                    scale,
                    verticalCandidates[index],
                    firstIntervals,
                    secondIntervals,
                    out var horizontal))
                {
                    origin = new Vector2(horizontal, verticalCandidates[index]);
                    return true;
                }
            }

            return false;
        }

        private static void AddVerticalBoundaries(
            ICollection<float> candidates,
            Rect region,
            Vector2 point,
            float halfHeight,
            float scale)
        {
            candidates.Add(region.yMin + halfHeight + (scale * point.y));
            candidates.Add(region.yMax - halfHeight + (scale * point.y));
        }

        private static bool TryFindHorizontalOrigin(
            IReadOnlyList<Vector2> projected,
            BoardCameraSafeLayout layout,
            float halfWidth,
            float halfHeight,
            float scale,
            float verticalOrigin,
            Vector2[] firstBuffer,
            Vector2[] secondBuffer,
            out float horizontalOrigin)
        {
            var feasible = firstBuffer;
            var intersections = secondBuffer;
            feasible[0] = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
            var feasibleCount = 1;
            foreach (var point in projected)
            {
                var centerY = verticalOrigin - (scale * point.y);
                var hasUpper = TryBuildHorizontalInterval(
                    layout.UpperRegion,
                    point,
                    centerY,
                    halfWidth,
                    halfHeight,
                    scale,
                    out var upper);
                var lower = default(Vector2);
                var hasLower = layout.LowerRightRegion.width > 0f &&
                    layout.LowerRightRegion.height > 0f &&
                    TryBuildHorizontalInterval(
                        layout.LowerRightRegion,
                        point,
                        centerY,
                        halfWidth,
                        halfHeight,
                        scale,
                        out lower);

                if (!hasUpper && !hasLower)
                {
                    horizontalOrigin = 0f;
                    return false;
                }

                var intersectionCount = 0;
                for (var feasibleIndex = 0; feasibleIndex < feasibleCount; feasibleIndex++)
                {
                    if (hasUpper)
                    {
                        AddIntersection(
                            feasible[feasibleIndex], upper, intersections, ref intersectionCount);
                    }

                    if (hasLower)
                    {
                        AddIntersection(
                            feasible[feasibleIndex], lower, intersections, ref intersectionCount);
                    }
                }

                if (intersectionCount == 0)
                {
                    horizontalOrigin = 0f;
                    return false;
                }

                // At most two intervals normally survive (one per arm), but sorting and merging makes
                // the operation exact even when differently shifted gaps briefly create more.
                for (var sortIndex = 1; sortIndex < intersectionCount; sortIndex++)
                {
                    var value = intersections[sortIndex];
                    var before = sortIndex - 1;
                    while (before >= 0 && intersections[before].x > value.x)
                    {
                        intersections[before + 1] = intersections[before];
                        before--;
                    }

                    intersections[before + 1] = value;
                }

                feasibleCount = 0;
                for (var intersectionIndex = 0;
                     intersectionIndex < intersectionCount;
                     intersectionIndex++)
                {
                    var value = intersections[intersectionIndex];
                    if (feasibleCount > 0 && value.x <= feasible[feasibleCount - 1].y)
                    {
                        var previous = feasible[feasibleCount - 1];
                        feasible[feasibleCount - 1] =
                            new Vector2(previous.x, Mathf.Max(previous.y, value.y));
                    }
                    else
                    {
                        feasible[feasibleCount++] = value;
                    }
                }
            }

            var widest = feasible[0];
            for (var index = 1; index < feasibleCount; index++)
            {
                if (feasible[index].y - feasible[index].x > widest.y - widest.x)
                {
                    widest = feasible[index];
                }
            }

            horizontalOrigin = (widest.x + widest.y) * 0.5f;
            return true;
        }

        private static bool TryBuildHorizontalInterval(
            Rect region,
            Vector2 point,
            float centerY,
            float halfWidth,
            float halfHeight,
            float scale,
            out Vector2 interval)
        {
            if (centerY - halfHeight < region.yMin - 0.001f ||
                centerY + halfHeight > region.yMax + 0.001f)
            {
                interval = default;
                return false;
            }

            var minimum = region.xMin + halfWidth - (scale * point.x);
            var maximum = region.xMax - halfWidth - (scale * point.x);
            interval = new Vector2(minimum, maximum);
            return minimum <= maximum;
        }

        private static void AddIntersection(
            Vector2 left,
            Vector2 right,
            Vector2[] intersections,
            ref int count)
        {
            var minimum = Mathf.Max(left.x, right.x);
            var maximum = Mathf.Min(left.y, right.y);
            if (minimum <= maximum)
            {
                intersections[count++] = new Vector2(minimum, maximum);
            }
        }

        private static bool Approximately(Rect left, Rect right) =>
            Mathf.Approximately(left.x, right.x) &&
            Mathf.Approximately(left.y, right.y) &&
            Mathf.Approximately(left.width, right.width) &&
            Mathf.Approximately(left.height, right.height);

        private static bool Approximately(BoardCameraSafeLayout left, BoardCameraSafeLayout right) =>
            Approximately(left.UpperRegion, right.UpperRegion) &&
            Approximately(left.LowerRightRegion, right.LowerRightRegion) &&
            Mathf.Approximately(left.PanelViewport.x, right.PanelViewport.x) &&
            Mathf.Approximately(left.PanelViewport.y, right.PanelViewport.y);

        private static void DestroyObject(GameObject target)
        {
            if (UnityEngine.Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }

        private void OnDestroy()
        {
            DestroyZoneMaterials();
            DestroyOwnedResource(terrainMaterial);
            DestroyOwnedResource(edgeMaterial);
            DestroyOwnedResource(redSpawnMaterial);
            DestroyOwnedResource(blueSpawnMaterial);
            DestroyOwnedResource(suppressedFillMaterial);
            DestroyOwnedResource(generatedTileMesh);
            terrainMaterial = null;
            edgeMaterial = null;
            redSpawnMaterial = null;
            blueSpawnMaterial = null;
            suppressedFillMaterial = null;
            generatedTileMesh = null;
        }

        private static void DestroyOwnedResource(Object resource)
        {
            if (resource == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(resource);
            }
            else
            {
                DestroyImmediate(resource);
            }
        }

    }
}
