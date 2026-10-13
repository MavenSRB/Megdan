using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    public static class BoardMapJsonLoader
    {
        public static BoardMapDefinition Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Map JSON must not be blank.", nameof(json));
            var source = JsonUtility.FromJson<MapDto>(json) ?? throw new ArgumentException("Map JSON is malformed.", nameof(json));
            var zones = (source.zones ?? Array.Empty<ZoneDto>()).Select(value => new BoardMapZone(
                new ZoneId(value.zoneId), value.displayName, value.kind, value.teamId, value.isThrone, value.isStartingBattleZone));
            var spaces = (source.spaces ?? Array.Empty<SpaceDto>()).Select(value => new BoardMapSpace(
                new SpaceId(value.spaceId), new HexCoordinate(value.q, value.r),
                string.Equals(value.kind, "playable", StringComparison.Ordinal)
                    ? BoardMapSpaceKind.Playable
                    : string.Equals(value.kind, "terrain", StringComparison.Ordinal)
                        ? BoardMapSpaceKind.Terrain
                        : throw new ArgumentException($"Unknown map space kind '{value.kind}'.", nameof(json)),
                string.IsNullOrWhiteSpace(value.zoneId) ? (ZoneId?)null : new ZoneId(value.zoneId)));
            var spawns = (source.spawnPoints ?? Array.Empty<SpawnDto>()).Select(value => new BoardMapSpawnPoint(
                new SpawnPointId(value.spawnPointId), new SpaceId(value.spaceId), value.teamId, value.unitType));
            var orderedLane = (source.orderedLaneZoneIds ?? Array.Empty<string>()).Select(value => new ZoneId(value));
            var orderedLanes = (source.orderedLanes ?? Array.Empty<LaneDto>())
                .Select(lane => (IEnumerable<ZoneId>)(lane.zoneIds ?? Array.Empty<string>())
                    .Select(value => new ZoneId(value)).ToArray())
                .ToArray();
            return new BoardMapDefinition(new MapId(source.mapId), source.revision, source.displayName,
                spaces, zones, spawns,
                orderedLanes.Length == 0
                    ? (orderedLane.Any()
                        ? new[] { orderedLane }
                        : Array.Empty<IEnumerable<ZoneId>>())
                    : orderedLanes);
        }

        [Serializable] private sealed class MapDto { public string mapId; public int revision; public string displayName; public ZoneDto[] zones; public SpaceDto[] spaces; public SpawnDto[] spawnPoints; public string[] orderedLaneZoneIds; public LaneDto[] orderedLanes; }
        [Serializable] private sealed class LaneDto { public string laneId; public string[] zoneIds; }
        [Serializable] private sealed class ZoneDto { public string zoneId; public string displayName; public string kind; public string teamId; public bool isThrone; public bool isStartingBattleZone; }
        [Serializable] private sealed class SpaceDto { public string spaceId; public int q; public int r; public string kind; public string zoneId; }
        [Serializable] private sealed class SpawnDto { public string spawnPointId; public string spaceId; public string teamId; public string unitType; }
    }

    public static class OfficialBoardMapCatalog
    {
        public static readonly MapDefinitionReference SelectedMap =
            new MapDefinitionReference(new MapId("official-single-lane"), 1);

        public static readonly MapDefinitionReference AlternateSmall =
            new MapDefinitionReference(new MapId("alternate-small-single-lane"), 2);
        public static readonly MapDefinitionReference GreenDoubleLane =
            new MapDefinitionReference(new MapId("green-water-double-lane"), 1);
        public static readonly MapDefinitionReference LavaBridgeDoubleLane =
            new MapDefinitionReference(new MapId("lava-bridge-double-lane"), 1);
        public static System.Collections.Generic.IReadOnlyList<MapDefinitionReference> SupportedMaps { get; } =
            Array.AsReadOnly(new[] { SelectedMap, AlternateSmall, GreenDoubleLane, LavaBridgeDoubleLane });

        public static BoardMapCatalog Load()
        {
            return new BoardMapCatalog(SupportedMaps.Select(reference =>
            {
                var asset = Resources.Load<TextAsset>($"Maps/{reference.MapId.Value}");
                if (asset == null) throw new InvalidOperationException($"Map resource '{reference}' is unavailable.");
                return BoardMapJsonLoader.Load(asset.text);
            }));
        }
    }
}
