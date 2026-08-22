using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
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
        /// The share of the viewport's width the framing keeps clear along the left edge, where the card
        /// tray stands.
        ///
        /// Milestone 8.5a, by the user's 2026-08-21 decision. A tray card carries its whole printed face
        /// now, so the tray is taller than it was, and a map centred in the viewport sat squarely behind
        /// it. The map is framed into the band this share leaves instead: the camera moves left, and it
        /// widens only if the map no longer fits. It usually does not have to - the official map is close
        /// to square and the viewport is 16:9, so the width it was never using is most of what the tray
        /// needs.
        ///
        /// The value is a judgement about where the board should sit rather than the one that would
        /// clear the tray outright, so a little of the map is still behind the cards' corner. Zero
        /// restores the plain centred framing.
        /// </summary>
        [SerializeField] private float reservedLeftViewportShare = 0.32f;
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

                view.ConfigureFromSetup(setupHero, definition, outerRadius);
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

            var positions = definition.Spaces.Select(space => HexWorldLayout.ToWorld(space.Coordinate, outerRadius)).ToArray();
            var minimumX = positions.Min(position => Vector3.Dot(position, screenRight)) - outerRadius;
            var maximumX = positions.Max(position => Vector3.Dot(position, screenRight)) + outerRadius;
            var minimumZ = positions.Min(position => Vector3.Dot(position, screenUp)) - outerRadius;
            var maximumZ = positions.Max(position => Vector3.Dot(position, screenUp)) + outerRadius;
            var center = (screenRight * ((minimumX + maximumX) * 0.5f)) +
                (screenUp * ((minimumZ + maximumZ) * 0.5f)) +
                new Vector3(0f, 60f, 0f);
            var aspect = targetCamera.aspect > 0f ? targetCamera.aspect : 16f / 9f;
            var halfWidth = (maximumX - minimumX) * 0.5f;
            var halfDepth = (maximumZ - minimumZ) * 0.5f;
            // The map is framed into the band left over once the tray's share of the width is reserved,
            // so the size below is what fits that band rather than what fits the whole viewport.
            var reserved = Mathf.Clamp(reservedLeftViewportShare, 0f, 0.6f);
            targetCamera.orthographic = true;
            targetCamera.orthographicSize =
                Mathf.Max(halfDepth, halfWidth / (aspect * (1f - reserved))) + cameraPadding;
            targetCamera.farClipPlane = 100f;

            // The camera looks straight down with world +Z up, so its screen right is world +X: moving
            // it left moves the map right, out from behind the tray. Half the reserved width centres the
            // map in the band that is left, which is what keeps every space inside the viewport - the
            // property `Scene_LoadsCompleteSelectedMapPresentationUnderFramedFixedCamera` measures.
            var shift = reserved * targetCamera.orthographicSize * aspect;
            targetCamera.transform.position = center - (screenRight * shift);
            targetCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, screenUp);
        }

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
