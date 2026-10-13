using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Board
{
    public readonly struct MapId : IEquatable<MapId>
    {
        public MapId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Map ID must not be blank.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool Equals(MapId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MapId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct MapDefinitionReference : IEquatable<MapDefinitionReference>
    {
        public MapDefinitionReference(MapId mapId, int contentRevision)
        {
            if (contentRevision <= 0) throw new ArgumentOutOfRangeException(nameof(contentRevision));
            MapId = mapId;
            ContentRevision = contentRevision;
        }

        public MapId MapId { get; }
        public int ContentRevision { get; }
        public bool Equals(MapDefinitionReference other) => MapId.Equals(other.MapId) && ContentRevision == other.ContentRevision;
        public override bool Equals(object obj) => obj is MapDefinitionReference other && Equals(other);
        public override int GetHashCode() => (MapId.GetHashCode() * 397) ^ ContentRevision;
        public override string ToString() => $"{MapId}@{ContentRevision}";
    }

    public readonly struct SpaceId : IEquatable<SpaceId>
    {
        public SpaceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Space ID must not be blank.", nameof(value));
            Value = value;
        }
        public string Value { get; }
        public bool Equals(SpaceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SpaceId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct ZoneId : IEquatable<ZoneId>
    {
        public ZoneId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Zone ID must not be blank.", nameof(value));
            Value = value;
        }
        public string Value { get; }
        public bool Equals(ZoneId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ZoneId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct SpawnPointId : IEquatable<SpawnPointId>
    {
        public SpawnPointId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Spawn-point ID must not be blank.", nameof(value));
            Value = value;
        }
        public string Value { get; }
        public bool Equals(SpawnPointId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SpawnPointId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public enum BoardMapSpaceKind { Playable = 1, Terrain = 2 }

    public enum ZoneRelationship
    {
        Same = 1,
        Adjacent = 2,
        NonAdjacent = 3
    }

    public sealed class BoardMapSpace
    {
        public BoardMapSpace(SpaceId id, HexCoordinate coordinate, BoardMapSpaceKind kind, ZoneId? zoneId)
        {
            if (kind != BoardMapSpaceKind.Playable && kind != BoardMapSpaceKind.Terrain) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == BoardMapSpaceKind.Playable && !zoneId.HasValue) throw new ArgumentException("Playable spaces require a zone.", nameof(zoneId));
            if (kind == BoardMapSpaceKind.Terrain && zoneId.HasValue) throw new ArgumentException("Terrain spaces cannot belong to a zone.", nameof(zoneId));
            Id = id;
            Coordinate = coordinate;
            Kind = kind;
            ZoneId = zoneId;
        }
        public SpaceId Id { get; }
        public HexCoordinate Coordinate { get; }
        public BoardMapSpaceKind Kind { get; }
        public ZoneId? ZoneId { get; }
    }

    public sealed class BoardMapZone
    {
        public BoardMapZone(ZoneId id, string displayName, string kind, string teamId, bool isThrone, bool isStartingBattleZone)
        {
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Zone display name must not be blank.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Zone kind must not be blank.", nameof(kind));
            Id = id; DisplayName = displayName; Kind = kind; TeamId = teamId; IsThrone = isThrone; IsStartingBattleZone = isStartingBattleZone;
        }
        public ZoneId Id { get; }
        public string DisplayName { get; }
        public string Kind { get; }
        public string TeamId { get; }
        public bool IsThrone { get; }
        public bool IsStartingBattleZone { get; }
    }

    public sealed class BoardMapSpawnPoint
    {
        public BoardMapSpawnPoint(SpawnPointId id, SpaceId spaceId, string teamId, string unitType)
        {
            if (string.IsNullOrWhiteSpace(teamId)) throw new ArgumentException("Spawn team must not be blank.", nameof(teamId));
            if (string.IsNullOrWhiteSpace(unitType)) throw new ArgumentException("Spawn unit type must not be blank.", nameof(unitType));
            Id = id; SpaceId = spaceId; TeamId = teamId; UnitType = unitType;
        }
        public SpawnPointId Id { get; }
        public SpaceId SpaceId { get; }
        public string TeamId { get; }
        public string UnitType { get; }
    }

    public sealed class BoardMapDefinition
    {
        private readonly IReadOnlyList<BoardMapSpace> spaces;
        private readonly IReadOnlyList<BoardMapZone> zones;
        private readonly IReadOnlyList<BoardMapSpawnPoint> spawnPoints;
        private readonly IReadOnlyList<HexCoordinate> playableCoordinates;
        private readonly IReadOnlyList<ZoneId> orderedLaneZoneIds;
        private readonly IReadOnlyList<IReadOnlyList<ZoneId>> orderedLanes;
        private readonly IReadOnlyDictionary<SpaceId, BoardMapSpace> spacesById;
        private readonly IReadOnlyDictionary<ZoneId, BoardMapZone> zonesById;
        private readonly IReadOnlyDictionary<ZoneId, IReadOnlyList<ZoneId>> adjacentZoneIds;

        public BoardMapDefinition(MapId id, int contentRevision, string displayName,
            IEnumerable<BoardMapSpace> spaces, IEnumerable<BoardMapZone> zones,
            IEnumerable<BoardMapSpawnPoint> spawnPoints, IEnumerable<ZoneId> orderedLaneZoneIds)
            : this(id, contentRevision, displayName, spaces, zones, spawnPoints,
                NormalizeLanes(orderedLaneZoneIds))
        {
        }

        public BoardMapDefinition(MapId id, int contentRevision, string displayName,
            IEnumerable<BoardMapSpace> spaces, IEnumerable<BoardMapZone> zones,
            IEnumerable<BoardMapSpawnPoint> spawnPoints,
            IEnumerable<IEnumerable<ZoneId>> orderedLanes)
        {
            if (contentRevision <= 0) throw new ArgumentOutOfRangeException(nameof(contentRevision));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Map display name must not be blank.", nameof(displayName));
            var spaceArray = spaces?.ToArray() ?? throw new ArgumentNullException(nameof(spaces));
            var zoneArray = zones?.ToArray() ?? throw new ArgumentNullException(nameof(zones));
            var spawnArray = spawnPoints?.ToArray() ?? throw new ArgumentNullException(nameof(spawnPoints));
            var laneArrays = (orderedLanes ?? throw new ArgumentNullException(nameof(orderedLanes)))
                .Select(lane => lane?.ToArray() ?? throw new ArgumentException(
                    "An ordered lane cannot be null.", nameof(orderedLanes)))
                .ToArray();
            if (spaceArray.Length == 0) throw new ArgumentException("A map requires spaces.", nameof(spaces));
            RequireUnique(spaceArray.Select(value => value.Id), "space IDs");
            RequireUnique(spaceArray.Select(value => value.Coordinate), "space coordinates");
            RequireUnique(zoneArray.Select(value => value.Id), "zone IDs");
            RequireUnique(spawnArray.Select(value => value.Id), "spawn-point IDs");
            var zoneIds = new HashSet<ZoneId>(zoneArray.Select(value => value.Id));
            if (spaceArray.Any(value => value.ZoneId.HasValue && !zoneIds.Contains(value.ZoneId.Value))) throw new ArgumentException("A space references an unknown zone.", nameof(spaces));
            if (zoneArray.Any(zone => !spaceArray.Any(space => space.ZoneId.HasValue && space.ZoneId.Value.Equals(zone.Id)))) throw new ArgumentException("Every zone requires at least one playable space.", nameof(zones));
            var playableIds = new HashSet<SpaceId>(spaceArray.Where(value => value.Kind == BoardMapSpaceKind.Playable).Select(value => value.Id));
            if (spawnArray.Any(value => !playableIds.Contains(value.SpaceId))) throw new ArgumentException("A spawn point must reference a playable space.", nameof(spawnPoints));

            var zoneAdjacency = zoneArray.ToDictionary(value => value.Id, value => new HashSet<ZoneId>());
            var playableByCoordinate = spaceArray
                .Where(value => value.Kind == BoardMapSpaceKind.Playable)
                .ToDictionary(value => value.Coordinate);
            foreach (var space in playableByCoordinate.Values)
            {
                foreach (var neighborCoordinate in space.Coordinate.GetNeighbors())
                {
                    if (!playableByCoordinate.TryGetValue(neighborCoordinate, out var neighbor) ||
                        neighbor.ZoneId.Value.Equals(space.ZoneId.Value))
                    {
                        continue;
                    }

                    zoneAdjacency[space.ZoneId.Value].Add(neighbor.ZoneId.Value);
                    zoneAdjacency[neighbor.ZoneId.Value].Add(space.ZoneId.Value);
                }
            }

            ValidateOrderedLanes(laneArrays, zoneArray, zoneIds, zoneAdjacency);
            Id = id; ContentRevision = contentRevision; DisplayName = displayName;
            this.spaces = Array.AsReadOnly(spaceArray); this.zones = Array.AsReadOnly(zoneArray); this.spawnPoints = Array.AsReadOnly(spawnArray);
            playableCoordinates = Array.AsReadOnly(spaceArray.Where(value => value.Kind == BoardMapSpaceKind.Playable).Select(value => value.Coordinate).ToArray());
            this.orderedLanes = Array.AsReadOnly(laneArrays
                .Select(lane => (IReadOnlyList<ZoneId>)Array.AsReadOnly(lane)).ToArray());
            orderedLaneZoneIds = this.orderedLanes.Count == 0
                ? Array.Empty<ZoneId>()
                : this.orderedLanes[0];
            spacesById = new Dictionary<SpaceId, BoardMapSpace>(spaceArray.ToDictionary(value => value.Id));
            zonesById = new Dictionary<ZoneId, BoardMapZone>(zoneArray.ToDictionary(value => value.Id));
            adjacentZoneIds = new Dictionary<ZoneId, IReadOnlyList<ZoneId>>(zoneAdjacency.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<ZoneId>)Array.AsReadOnly(pair.Value.OrderBy(value => value.Value, StringComparer.Ordinal).ToArray())));
        }

        public MapId Id { get; }
        public int ContentRevision { get; }
        public string DisplayName { get; }
        public MapDefinitionReference Reference => new MapDefinitionReference(Id, ContentRevision);
        public IReadOnlyList<BoardMapSpace> Spaces => spaces;
        public IReadOnlyList<BoardMapZone> Zones => zones;
        public IReadOnlyList<BoardMapSpawnPoint> SpawnPoints => spawnPoints;
        public IReadOnlyList<HexCoordinate> PlayableCoordinates => playableCoordinates;
        public IReadOnlyList<ZoneId> OrderedLaneZoneIds => orderedLaneZoneIds;
        public IReadOnlyList<IReadOnlyList<ZoneId>> OrderedLanes => orderedLanes;

        public bool TryGetLaneContaining(ZoneId zoneId, out int laneIndex, out IReadOnlyList<ZoneId> lane)
        {
            for (var index = 0; index < orderedLanes.Count; index++)
            {
                if (!orderedLanes[index].Contains(zoneId)) continue;
                laneIndex = index;
                lane = orderedLanes[index];
                return true;
            }
            laneIndex = -1;
            lane = null;
            return false;
        }

        public BoardMapZone GetZone(ZoneId zoneId)
        {
            if (!zonesById.TryGetValue(zoneId, out var zone))
                throw new ArgumentException($"Zone '{zoneId}' is unavailable on map '{Reference}'.", nameof(zoneId));
            return zone;
        }

        public BoardMapZone GetZoneForSpace(SpaceId spaceId)
        {
            if (!spacesById.TryGetValue(spaceId, out var space))
                throw new ArgumentException($"Space '{spaceId}' is unavailable on map '{Reference}'.", nameof(spaceId));
            if (!space.ZoneId.HasValue)
                throw new ArgumentException($"Terrain cell '{spaceId}' does not belong to a zone.", nameof(spaceId));
            return zonesById[space.ZoneId.Value];
        }

        public IReadOnlyList<ZoneId> GetAdjacentZoneIds(ZoneId zoneId)
        {
            GetZone(zoneId);
            return adjacentZoneIds[zoneId];
        }

        public ZoneRelationship GetZoneRelationship(ZoneId first, ZoneId second)
        {
            GetZone(first);
            GetZone(second);
            if (first.Equals(second)) return ZoneRelationship.Same;
            return adjacentZoneIds[first].Contains(second)
                ? ZoneRelationship.Adjacent
                : ZoneRelationship.NonAdjacent;
        }

        public ZoneRelationship GetZoneRelationship(SpaceId first, SpaceId second)
        {
            return GetZoneRelationship(GetZoneForSpace(first).Id, GetZoneForSpace(second).Id);
        }

        public static BoardMapDefinition CreateSynthetic(MapId id, int revision, IEnumerable<HexCoordinate> playableCoordinates) =>
            CreateSynthetic(id, revision, playableCoordinates, Array.Empty<HexCoordinate>());

        /// <summary>
        /// Synthetic board with declared terrain cells, added at milestone 6.8e so a fixture can
        /// express the terrain/off-board distinction the official map carries. Terrain spaces have no
        /// zone, exactly as the map specification requires.
        /// </summary>
        public static BoardMapDefinition CreateSynthetic(
            MapId id,
            int revision,
            IEnumerable<HexCoordinate> playableCoordinates,
            IEnumerable<HexCoordinate> terrainCoordinates)
        {
            var zone = new BoardMapZone(new ZoneId("synthetic-zone"), "Synthetic Zone", "synthetic", null, false, false);
            var playable = (playableCoordinates ?? throw new ArgumentNullException(nameof(playableCoordinates))).Select(value =>
                new BoardMapSpace(new SpaceId($"q{value.Q}_r{value.R}"), value, BoardMapSpaceKind.Playable, zone.Id));
            var terrain = (terrainCoordinates ?? throw new ArgumentNullException(nameof(terrainCoordinates))).Select(value =>
                new BoardMapSpace(new SpaceId($"q{value.Q}_r{value.R}"), value, BoardMapSpaceKind.Terrain, null));
            return new BoardMapDefinition(id, revision, id.Value, playable.Concat(terrain), new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        /// <summary>
        /// Synthetic two-zone board with a flagged starting Battle Zone, added at milestone 6.4a so a
        /// focused fixture can express "outside the Battle Zone" at all - the single-zone synthetic
        /// board makes every playable space a member, so nothing on it is ever displaced. A separate
        /// explicit factory rather than an optional flag, per the recorded no-optional-parameters
        /// doctrine. Composing one flips the round authority's end-of-round probe to the configured
        /// capabilities, so round-level fixtures account for a live Minion Battle or stay below the
        /// round.
        /// </summary>
        public static BoardMapDefinition CreateSyntheticWithStartingBattleZone(
            MapId id,
            int revision,
            IEnumerable<HexCoordinate> battleZoneCoordinates,
            IEnumerable<HexCoordinate> outerZoneCoordinates,
            IEnumerable<HexCoordinate> terrainCoordinates)
        {
            var battleZone = new BoardMapZone(
                new ZoneId("synthetic-battle-zone"), "Synthetic Battle Zone", "synthetic", null, false, true);
            var outerZone = new BoardMapZone(
                new ZoneId("synthetic-outer-zone"), "Synthetic Outer Zone", "synthetic", null, false, false);
            var battle = (battleZoneCoordinates ?? throw new ArgumentNullException(nameof(battleZoneCoordinates)))
                .Select(value => new BoardMapSpace(
                    new SpaceId($"q{value.Q}_r{value.R}"), value, BoardMapSpaceKind.Playable, battleZone.Id));
            var outer = (outerZoneCoordinates ?? throw new ArgumentNullException(nameof(outerZoneCoordinates)))
                .Select(value => new BoardMapSpace(
                    new SpaceId($"q{value.Q}_r{value.R}"), value, BoardMapSpaceKind.Playable, outerZone.Id));
            var terrain = (terrainCoordinates ?? throw new ArgumentNullException(nameof(terrainCoordinates)))
                .Select(value => new BoardMapSpace(
                    new SpaceId($"q{value.Q}_r{value.R}"), value, BoardMapSpaceKind.Terrain, null));
            return new BoardMapDefinition(
                id, revision, id.Value, battle.Concat(outer).Concat(terrain),
                new[] { battleZone, outerZone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        private static void ValidateOrderedLanes(
            ZoneId[][] lanes,
            BoardMapZone[] zones,
            HashSet<ZoneId> zoneIds,
            IReadOnlyDictionary<ZoneId, HashSet<ZoneId>> adjacency)
        {
            if (lanes.Length == 0) return;
            var zonesById = zones.ToDictionary(value => value.Id);
            foreach (var lane in lanes)
            {
                if (lane.Length < 2) throw new ArgumentException("An ordered lane requires at least two zones.", nameof(lanes));
                RequireUnique(lane, "ordered lane zone IDs");
                if (lane.Any(value => !zoneIds.Contains(value))) throw new ArgumentException("An ordered lane references an unknown zone.", nameof(lanes));
                if (!zonesById[lane[0]].IsThrone || !zonesById[lane[lane.Length - 1]].IsThrone)
                    throw new ArgumentException("Every ordered lane must begin and end at Throne zones.", nameof(lanes));
                if (lane.Skip(1).Take(lane.Length - 2).Any(value => zonesById[value].IsThrone))
                    throw new ArgumentException("Only ordered-lane endpoints may be Throne zones.", nameof(lanes));
                if (lane.Count(value => zonesById[value].IsStartingBattleZone) != 1)
                    throw new ArgumentException("Every ordered lane requires exactly one starting Battle Zone.", nameof(lanes));
                for (var index = 0; index < lane.Length - 1; index++)
                    if (!adjacency[lane[index]].Contains(lane[index + 1]))
                        throw new ArgumentException("Consecutive ordered lane zones must share a playable border.", nameof(lanes));
            }

            var expectedLaneZones = new HashSet<ZoneId>(zones
                .Where(value => value.IsThrone || string.Equals(value.Kind, "lane", StringComparison.Ordinal))
                .Select(value => value.Id));
            var actualLaneZones = new HashSet<ZoneId>(lanes.SelectMany(lane => lane));
            if (!expectedLaneZones.SetEquals(actualLaneZones))
                throw new ArgumentException("Ordered lanes must contain every Throne and lane zone.", nameof(lanes));
            var sharedInterior = lanes.SelectMany(lane => lane.Skip(1).Take(lane.Length - 2))
                .GroupBy(value => value).FirstOrDefault(group => group.Count() > 1);
            if (sharedInterior != null)
                throw new ArgumentException("Two ordered lanes cannot share an interior zone.", nameof(lanes));
        }

        private static IEnumerable<IEnumerable<ZoneId>> NormalizeLanes(
            IEnumerable<ZoneId> orderedLaneZoneIds)
        {
            var lane = orderedLaneZoneIds?.ToArray() ??
                throw new ArgumentNullException(nameof(orderedLaneZoneIds));
            return lane.Length == 0
                ? Array.Empty<IEnumerable<ZoneId>>()
                : new[] { (IEnumerable<ZoneId>)lane };
        }

        private static void RequireUnique<T>(IEnumerable<T> values, string label)
        {
            var array = values.ToArray();
            if (array.Distinct().Count() != array.Length) throw new ArgumentException($"Map {label} must be unique.");
        }
    }

    public enum BoardMapLoadRejectionReason { None = 0, MissingMap = 1, ContentRevisionMismatch = 2 }

    public sealed class BoardMapLoadException : Exception
    {
        public BoardMapLoadException(BoardMapLoadRejectionReason reason, string message) : base(message) { Reason = reason; }
        public BoardMapLoadRejectionReason Reason { get; }
    }

    public sealed class BoardMapCatalog
    {
        private readonly IReadOnlyList<BoardMapDefinition> definitions;
        public BoardMapCatalog(IEnumerable<BoardMapDefinition> definitions)
        {
            var array = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            if (array.GroupBy(value => value.Reference).Any(group => group.Count() > 1)) throw new ArgumentException("Map catalog references must be unique.", nameof(definitions));
            this.definitions = Array.AsReadOnly(array);
        }
        public IReadOnlyList<BoardMapDefinition> Definitions => definitions;
        public BoardMapDefinition Load(MapDefinitionReference reference)
        {
            var definition = definitions.SingleOrDefault(value => value.Reference.Equals(reference));
            if (definition != null) return definition;
            if (definitions.Any(value => value.Id.Equals(reference.MapId)))
                throw new BoardMapLoadException(BoardMapLoadRejectionReason.ContentRevisionMismatch, $"Map '{reference.MapId}' revision {reference.ContentRevision} is unavailable.");
            throw new BoardMapLoadException(BoardMapLoadRejectionReason.MissingMap, $"Map '{reference.MapId}' is unavailable.");
        }
    }
}
