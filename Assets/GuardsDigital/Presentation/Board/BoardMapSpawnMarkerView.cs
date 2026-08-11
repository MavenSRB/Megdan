using GuardsDigital.Core.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    public enum SpawnMarkerShape
    {
        HeroCircle = 1,
        MeleeDiamond = 2,
        RangedVerticalLine = 3,
        HeavyHexagon = 4
    }

    [DisallowMultipleComponent]
    public sealed class BoardMapSpawnMarkerView : MonoBehaviour
    {
        [SerializeField] private string spawnPointId;
        [SerializeField] private string spaceId;
        [SerializeField] private string teamId;
        [SerializeField] private string unitType;
        [SerializeField] private SpawnMarkerShape markerShape;

        public SpawnPointId SpawnPointId => new SpawnPointId(spawnPointId);
        public SpaceId SpaceId => new SpaceId(spaceId);
        public string TeamId => teamId;
        public string UnitType => unitType;
        public SpawnMarkerShape MarkerShape => markerShape;

        public void Configure(BoardMapSpawnPoint spawnPoint, float outerRadius, Material material)
        {
            if (spawnPoint == null) throw new System.ArgumentNullException(nameof(spawnPoint));
            spawnPointId = spawnPoint.Id.Value;
            spaceId = spawnPoint.SpaceId.Value;
            teamId = spawnPoint.TeamId;
            unitType = spawnPoint.UnitType;
            markerShape = ResolveShape(unitType);

            var renderer = GetComponent<LineRenderer>();
            renderer.sharedMaterial = material;
            renderer.useWorldSpace = false;
            renderer.startWidth = outerRadius * 0.075f;
            renderer.endWidth = renderer.startWidth;
            renderer.numCornerVertices = 2;
            var radius = outerRadius * 0.28f;
            ConfigureShape(renderer, markerShape, radius);
        }

        private static SpawnMarkerShape ResolveShape(string type)
        {
            switch (type)
            {
                case "hero": return SpawnMarkerShape.HeroCircle;
                case "melee-minion": return SpawnMarkerShape.MeleeDiamond;
                case "ranged-minion": return SpawnMarkerShape.RangedVerticalLine;
                case "heavy-minion": return SpawnMarkerShape.HeavyHexagon;
                default: throw new System.ArgumentException($"Unsupported spawn-point unit type '{type}'.", nameof(type));
            }
        }

        private static void ConfigureShape(LineRenderer renderer, SpawnMarkerShape shape, float radius)
        {
            switch (shape)
            {
                case SpawnMarkerShape.HeroCircle:
                    ConfigureRegularPolygon(renderer, 16, radius, 0f);
                    break;
                case SpawnMarkerShape.MeleeDiamond:
                    ConfigureRegularPolygon(renderer, 4, radius, 0f);
                    break;
                case SpawnMarkerShape.RangedVerticalLine:
                    renderer.loop = false;
                    renderer.positionCount = 2;
                    renderer.SetPosition(0, new Vector3(0f, 0.04f, -radius));
                    renderer.SetPosition(1, new Vector3(0f, 0.04f, radius));
                    break;
                case SpawnMarkerShape.HeavyHexagon:
                    ConfigureRegularPolygon(renderer, 6, radius, 30f);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(shape), shape, "Unknown spawn marker shape.");
            }
        }

        private static void ConfigureRegularPolygon(LineRenderer renderer, int vertexCount, float radius, float rotationDegrees)
        {
            renderer.loop = true;
            renderer.positionCount = vertexCount;
            for (var index = 0; index < vertexCount; index++)
            {
                var angle = Mathf.Deg2Rad * (rotationDegrees + (360f * index / vertexCount));
                renderer.SetPosition(index, new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.04f,
                    Mathf.Sin(angle) * radius));
            }
        }
    }
}
