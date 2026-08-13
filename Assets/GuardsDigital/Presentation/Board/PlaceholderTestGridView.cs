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
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private PrimitiveHeroView[] heroViews;

        private readonly Dictionary<ZoneId, Material> zoneMaterials = new Dictionary<ZoneId, Material>();
        private readonly List<HexTileView> currentTiles = new List<HexTileView>();
        private readonly List<StandardMinionView> currentMinionViews = new List<StandardMinionView>();
        private readonly List<PlaceholderObstacleView> currentObstacles = new List<PlaceholderObstacleView>();
        private BoardMapDefinition renderedDefinition;
        private QuickGameSetupState renderedSetup;
        private Material terrainMaterial;
        private Material edgeMaterial;
        private Material redSpawnMaterial;
        private Material blueSpawnMaterial;
        private Mesh generatedTileMesh;
        private MapDefinitionReference materialMapReference;

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
            (generatedTileMesh != null ? 1 : 0);

        /// <summary>
        /// The views this rebuild actually produced, excluding any object it retired. Consumers must
        /// use these instead of GetComponentsInChildren: in Play Mode a retired object is destroyed
        /// with the deferred UnityEngine.Object.Destroy and is still returned by a child search until
        /// the end of the frame, so re-discovery can admit doomed objects into authoritative state.
        /// </summary>
        public IReadOnlyList<HexTileView> CurrentTiles => currentTiles;
        public IReadOnlyList<StandardMinionView> CurrentMinionViews => currentMinionViews;
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

        private void EnsureMaterials(BoardMapDefinition definition)
        {
            terrainMaterial ??= CreateMaterial("Terrain Debug", new Color(0.20f, 0.23f, 0.26f, 1f));
            edgeMaterial ??= CreateMaterial("Map Edge Debug", new Color(0.10f, 0.11f, 0.13f, 1f));
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

        private Material GetSpaceMaterial(BoardMapSpace space) =>
            space.Kind == BoardMapSpaceKind.Terrain ? terrainMaterial : zoneMaterials[space.ZoneId.Value];

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

        private void FrameWholeMap(Camera targetCamera, BoardMapDefinition definition)
        {
            if (targetCamera == null) return;
            var positions = definition.Spaces.Select(space => HexWorldLayout.ToWorld(space.Coordinate, outerRadius)).ToArray();
            var minimumX = positions.Min(position => position.x) - outerRadius;
            var maximumX = positions.Max(position => position.x) + outerRadius;
            var minimumZ = positions.Min(position => position.z) - outerRadius;
            var maximumZ = positions.Max(position => position.z) + outerRadius;
            var center = new Vector3((minimumX + maximumX) * 0.5f, 60f, (minimumZ + maximumZ) * 0.5f);
            var aspect = targetCamera.aspect > 0f ? targetCamera.aspect : 16f / 9f;
            var halfWidth = (maximumX - minimumX) * 0.5f;
            var halfDepth = (maximumZ - minimumZ) * 0.5f;
            targetCamera.orthographic = true;
            targetCamera.orthographicSize = Mathf.Max(halfDepth, halfWidth / aspect) + cameraPadding;
            targetCamera.farClipPlane = 100f;
            targetCamera.transform.position = center;
            targetCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
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
            DestroyOwnedResource(generatedTileMesh);
            terrainMaterial = null;
            edgeMaterial = null;
            redSpawnMaterial = null;
            blueSpawnMaterial = null;
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
