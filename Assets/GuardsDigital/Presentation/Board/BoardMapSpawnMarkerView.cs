using GuardsDigital.Core.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    [DisallowMultipleComponent]
    public sealed class BoardMapSpawnMarkerView : MonoBehaviour
    {
        [SerializeField] private string spawnPointId;
        [SerializeField] private string spaceId;
        [SerializeField] private string teamId;
        [SerializeField] private string unitType;

        public SpawnPointId SpawnPointId => new SpawnPointId(spawnPointId);
        public SpaceId SpaceId => new SpaceId(spaceId);
        public string TeamId => teamId;
        public string UnitType => unitType;

        public void Configure(BoardMapSpawnPoint spawnPoint, float outerRadius, Material material)
        {
            if (spawnPoint == null) throw new System.ArgumentNullException(nameof(spawnPoint));
            spawnPointId = spawnPoint.Id.Value;
            spaceId = spawnPoint.SpaceId.Value;
            teamId = spawnPoint.TeamId;
            unitType = spawnPoint.UnitType;

            var renderer = GetComponent<LineRenderer>();
            renderer.sharedMaterial = material;
            renderer.useWorldSpace = false;
            renderer.loop = true;
            renderer.positionCount = 4;
            renderer.startWidth = outerRadius * 0.075f;
            renderer.endWidth = renderer.startWidth;
            renderer.numCornerVertices = 2;
            var radius = outerRadius * 0.28f;
            renderer.SetPosition(0, new Vector3(0f, 0.04f, radius));
            renderer.SetPosition(1, new Vector3(radius, 0.04f, 0f));
            renderer.SetPosition(2, new Vector3(0f, 0.04f, -radius));
            renderer.SetPosition(3, new Vector3(-radius, 0.04f, 0f));
        }
    }
}
