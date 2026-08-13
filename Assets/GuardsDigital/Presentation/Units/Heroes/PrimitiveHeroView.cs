using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Units.Heroes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PrimitiveHeroView : MonoBehaviour
    {
        public const string DefaultHeroId = "primitive-hero";
        public const float DefaultBaseRadius = 0.32f;
        public const float DefaultHeight = 0.5f;
        public const float DefaultVerticalOffset = 0.03f;

        private const int RedSideCount = 16;
        private const int BlueSideCount = 4;

        [SerializeField]
        private string heroId = DefaultHeroId;

        [SerializeField]
        private TeamSide teamSide = TeamSide.Red;

        [SerializeField]
        private string spawnPointId;

        [SerializeField]
        private string heroContentId;

        [SerializeField]
        private string heroDisplayName;

        [SerializeField]
        private int q;

        [SerializeField]
        private int r;

        [SerializeField]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        [SerializeField]
        private float baseRadius = DefaultBaseRadius;

        [SerializeField]
        private float height = DefaultHeight;

        [SerializeField]
        private float verticalOffset = DefaultVerticalOffset;

        private Mesh ownedMesh;
        private Material ownedMaterial;

        public HeroOccupancy Occupancy => new HeroOccupancy(new HeroId(heroId), Coordinate);

        public TeamSide TeamSide => teamSide;

        public HeroContentId? HeroContentId => string.IsNullOrWhiteSpace(heroContentId)
            ? (HeroContentId?)null
            : new HeroContentId(heroContentId);

        public string HeroDisplayName => heroDisplayName;

        public SpawnPointId? ConfiguredSpawnPointId => string.IsNullOrWhiteSpace(spawnPointId)
            ? (SpawnPointId?)null
            : new SpawnPointId(spawnPointId);

        public HexCoordinate Coordinate => new HexCoordinate(q, r);

        public float OuterRadius => outerRadius;

        public float VerticalOffset => verticalOffset;

        public void Configure(HeroOccupancy occupancy, float radius)
        {
            heroId = occupancy.HeroId.Value;
            q = occupancy.Coordinate.Q;
            r = occupancy.Coordinate.R;
            outerRadius = radius;
            ApplyLayout();
        }

        public void ConfigureFromSetup(QuickGameHeroSetupState setupState, BoardMapDefinition definition, float radius)
        {
            if (setupState == null) throw new System.ArgumentNullException(nameof(setupState));
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            if (!setupState.HeroId.Equals(Occupancy.HeroId))
            {
                throw new System.InvalidOperationException(
                    $"Setup hero '{setupState.HeroId}' does not match view hero '{Occupancy.HeroId}'.");
            }

            var spawn = definition.SpawnPoints.SingleOrDefault(value =>
                value.Id.Equals(setupState.SpawnPoint.SpawnPointId));
            var space = definition.Spaces.SingleOrDefault(value =>
                value.Id.Equals(setupState.OccupiedSpace.SpaceId));
            if (!setupState.SpawnPoint.MapId.Equals(definition.Id) ||
                !setupState.OccupiedSpace.MapId.Equals(definition.Id) ||
                spawn == null || space == null || !spawn.SpaceId.Equals(space.Id) ||
                !space.Coordinate.Equals(setupState.Coordinate))
            {
                throw new System.InvalidOperationException("Hero setup placement is not valid for the rendered map.");
            }

            teamSide = setupState.TeamSide;
            spawnPointId = setupState.SpawnPoint.SpawnPointId.Value;
            heroContentId = setupState.ContentId.Value;
            heroDisplayName = setupState.DisplayName;
            Configure(new HeroOccupancy(setupState.HeroId, setupState.Coordinate), radius);
            gameObject.name = $"{teamSide} {heroDisplayName} ({setupState.HeroId.Value})";
        }

        public bool PlaceAtConfiguredSpawnPoint(BoardMapDefinition definition)
        {
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            if (!ConfiguredSpawnPointId.HasValue) return false;

            var selectedSpawnPoint = definition.SpawnPoints.SingleOrDefault(spawnPoint =>
                spawnPoint.Id.Equals(ConfiguredSpawnPointId.Value));
            if (selectedSpawnPoint == null)
            {
                throw new System.InvalidOperationException(
                    $"Configured spawn point '{ConfiguredSpawnPointId.Value}' is unavailable on map '{definition.Reference}'.");
            }

            var expectedTeam = teamSide == TeamSide.Red ? "red" : "blue";
            if (selectedSpawnPoint.TeamId != expectedTeam || selectedSpawnPoint.UnitType != "hero")
            {
                throw new System.InvalidOperationException(
                    $"Spawn point '{selectedSpawnPoint.Id}' is not a {expectedTeam} hero spawn point.");
            }

            var selectedSpace = definition.Spaces.Single(space => space.Id.Equals(selectedSpawnPoint.SpaceId));
            Configure(new HeroOccupancy(Occupancy.HeroId, selectedSpace.Coordinate), outerRadius);
            return true;
        }

        public void ApplyLayout()
        {
            transform.localPosition = HexWorldLayout.ToWorld(Coordinate, outerRadius) + (Vector3.up * verticalOffset);
        }

        public void EnsurePrimitiveVisual()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (meshFilter.sharedMesh == null)
            {
                ownedMesh = CreatePrismMesh(baseRadius, height, GetSideCount());
                meshFilter.sharedMesh = ownedMesh;
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (meshRenderer.sharedMaterial == null)
            {
                ownedMaterial = CreatePlaceholderMaterial(teamSide);
                meshRenderer.sharedMaterial = ownedMaterial;
            }
        }

        private int GetSideCount()
        {
            return teamSide == TeamSide.Blue ? BlueSideCount : RedSideCount;
        }

        private static Mesh CreatePrismMesh(float radius, float cylinderHeight, int sideCount)
        {
            var vertices = new Vector3[(sideCount * 2) + 2];
            var triangles = new int[sideCount * 12];
            var bottomCenterIndex = sideCount * 2;
            var topCenterIndex = bottomCenterIndex + 1;

            vertices[bottomCenterIndex] = Vector3.zero;
            vertices[topCenterIndex] = Vector3.up * cylinderHeight;

            for (var side = 0; side < sideCount; side++)
            {
                var angle = Mathf.Deg2Rad * ((360f / sideCount) * side);
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                vertices[side] = new Vector3(x, 0f, z);
                vertices[side + sideCount] = new Vector3(x, cylinderHeight, z);
            }

            var triangleIndex = 0;
            for (var side = 0; side < sideCount; side++)
            {
                var next = (side + 1) % sideCount;
                var top = side + sideCount;
                var nextTop = next + sideCount;

                triangles[triangleIndex++] = side;
                triangles[triangleIndex++] = top;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = top;
                triangles[triangleIndex++] = nextTop;

                triangles[triangleIndex++] = topCenterIndex;
                triangles[triangleIndex++] = nextTop;
                triangles[triangleIndex++] = top;

                triangles[triangleIndex++] = bottomCenterIndex;
                triangles[triangleIndex++] = side;
                triangles[triangleIndex++] = next;
            }

            var mesh = new Mesh
            {
                name = $"Primitive {sideCount}-Sided Hero Placeholder",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreatePlaceholderMaterial(TeamSide side)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new System.InvalidOperationException(
                    "The primitive hero placeholder requires the Standard shader.");
            }

            var material = new Material(shader)
            {
                name = $"{side} Primitive Hero Placeholder",
                color = side == TeamSide.Blue
                    ? new Color(0.1f, 0.35f, 0.9f, 1f)
                    : new Color(0.82f, 0.12f, 0.12f, 1f)
            };
            return material;
        }

        private void Awake()
        {
            ApplyLayout();
            EnsurePrimitiveVisual();
        }

        private void OnDestroy()
        {
            DestroyOwnedResource(ownedMesh);
            DestroyOwnedResource(ownedMaterial);
            ownedMesh = null;
            ownedMaterial = null;
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (outerRadius > 0f && !float.IsNaN(outerRadius) && !float.IsInfinity(outerRadius))
            {
                ApplyLayout();
            }
        }
#endif
    }
}
