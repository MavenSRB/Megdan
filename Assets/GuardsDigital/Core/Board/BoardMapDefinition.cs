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

        public BoardMapDefinition(MapId id, int contentRevision, string displayName,
            IEnumerable<BoardMapSpace> spaces, IEnumerable<BoardMapZone> zones,
            IEnumerable<BoardMapSpawnPoint> spawnPoints)
        {
            if (contentRevision <= 0) throw new ArgumentOutOfRangeException(nameof(contentRevision));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Map display name must not be blank.", nameof(displayName));
            var spaceArray = spaces?.ToArray() ?? throw new ArgumentNullException(nameof(spaces));
            var zoneArray = zones?.ToArray() ?? throw new ArgumentNullException(nameof(zones));
            var spawnArray = spawnPoints?.ToArray() ?? throw new ArgumentNullException(nameof(spawnPoints));
            if (spaceArray.Length == 0) throw new ArgumentException("A map requires spaces.", nameof(spaces));
            RequireUnique(spaceArray.Select(value => value.Id), "space IDs");
            RequireUnique(spaceArray.Select(value => value.Coordinate), "space coordinates");
            RequireUnique(zoneArray.Select(value => value.Id), "zone IDs");
            RequireUnique(spawnArray.Select(value => value.Id), "spawn-point IDs");
            var zoneIds = new HashSet<ZoneId>(zoneArray.Select(value => value.Id));
            if (spaceArray.Any(value => value.ZoneId.HasValue && !zoneIds.Contains(value.ZoneId.Value))) throw new ArgumentException("A space references an unknown zone.", nameof(spaces));
            var playableIds = new HashSet<SpaceId>(spaceArray.Where(value => value.Kind == BoardMapSpaceKind.Playable).Select(value => value.Id));
            if (spawnArray.Any(value => !playableIds.Contains(value.SpaceId))) throw new ArgumentException("A spawn point must reference a playable space.", nameof(spawnPoints));
            Id = id; ContentRevision = contentRevision; DisplayName = displayName;
            this.spaces = Array.AsReadOnly(spaceArray); this.zones = Array.AsReadOnly(zoneArray); this.spawnPoints = Array.AsReadOnly(spawnArray);
            playableCoordinates = Array.AsReadOnly(spaceArray.Where(value => value.Kind == BoardMapSpaceKind.Playable).Select(value => value.Coordinate).ToArray());
        }

        public MapId Id { get; }
        public int ContentRevision { get; }
        public string DisplayName { get; }
        public MapDefinitionReference Reference => new MapDefinitionReference(Id, ContentRevision);
        public IReadOnlyList<BoardMapSpace> Spaces => spaces;
        public IReadOnlyList<BoardMapZone> Zones => zones;
        public IReadOnlyList<BoardMapSpawnPoint> SpawnPoints => spawnPoints;
        public IReadOnlyList<HexCoordinate> PlayableCoordinates => playableCoordinates;

        public static BoardMapDefinition CreateSynthetic(MapId id, int revision, IEnumerable<HexCoordinate> playableCoordinates)
        {
            var zone = new BoardMapZone(new ZoneId("synthetic-zone"), "Synthetic Zone", "synthetic", null, false, false);
            var spaces = (playableCoordinates ?? throw new ArgumentNullException(nameof(playableCoordinates))).Select(value =>
                new BoardMapSpace(new SpaceId($"q{value.Q}_r{value.R}"), value, BoardMapSpaceKind.Playable, zone.Id));
            return new BoardMapDefinition(id, revision, id.Value, spaces, new[] { zone }, Array.Empty<BoardMapSpawnPoint>());
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
