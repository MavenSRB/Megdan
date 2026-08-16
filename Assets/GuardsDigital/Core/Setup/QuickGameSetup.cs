using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Setup
{
    public readonly struct HeroContentId : IEquatable<HeroContentId>
    {
        public HeroContentId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Hero content ID must not be blank.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(HeroContentId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is HeroContentId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct MinionId : IEquatable<MinionId>
    {
        public MinionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Minion ID must not be blank.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(MinionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MinionId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public enum StandardMinionType
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Heavy = 3
    }

    public readonly struct ConfiguredSpaceReference : IEquatable<ConfiguredSpaceReference>
    {
        public ConfiguredSpaceReference(MapId mapId, SpaceId spaceId)
        {
            if (string.IsNullOrWhiteSpace(mapId.Value)) throw new ArgumentException("Configured space requires a map ID.", nameof(mapId));
            if (string.IsNullOrWhiteSpace(spaceId.Value)) throw new ArgumentException("Configured space requires a space ID.", nameof(spaceId));
            MapId = mapId;
            SpaceId = spaceId;
        }

        public MapId MapId { get; }
        public SpaceId SpaceId { get; }
        public bool Equals(ConfiguredSpaceReference other) => MapId.Equals(other.MapId) && SpaceId.Equals(other.SpaceId);
        public override bool Equals(object obj) => obj is ConfiguredSpaceReference other && Equals(other);
        public override int GetHashCode() => (MapId.GetHashCode() * 397) ^ SpaceId.GetHashCode();
        public override string ToString() => $"{MapId}/{SpaceId}";
    }

    public readonly struct ConfiguredSpawnPointReference : IEquatable<ConfiguredSpawnPointReference>
    {
        public ConfiguredSpawnPointReference(MapId mapId, SpawnPointId spawnPointId)
        {
            if (string.IsNullOrWhiteSpace(mapId.Value)) throw new ArgumentException("Configured spawn point requires a map ID.", nameof(mapId));
            if (string.IsNullOrWhiteSpace(spawnPointId.Value)) throw new ArgumentException("Configured spawn point requires a spawn-point ID.", nameof(spawnPointId));
            MapId = mapId;
            SpawnPointId = spawnPointId;
        }

        public MapId MapId { get; }
        public SpawnPointId SpawnPointId { get; }
        public bool Equals(ConfiguredSpawnPointReference other) =>
            MapId.Equals(other.MapId) && SpawnPointId.Equals(other.SpawnPointId);
        public override bool Equals(object obj) => obj is ConfiguredSpawnPointReference other && Equals(other);
        public override int GetHashCode() => (MapId.GetHashCode() * 397) ^ SpawnPointId.GetHashCode();
        public override string ToString() => $"{MapId}/{SpawnPointId}";
    }

    public sealed class QuickGameHeroPlacementDefinition
    {
        public QuickGameHeroPlacementDefinition(
            HeroId heroId,
            HeroContentId contentId,
            string displayName,
            ConfiguredSpawnPointReference spawnPoint,
            ConfiguredSpaceReference occupiedSpace)
        {
            if (!heroId.IsDefined) throw new ArgumentException("Hero placement requires a hero ID.", nameof(heroId));
            if (!contentId.IsDefined) throw new ArgumentException("Hero placement requires a content ID.", nameof(contentId));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Hero display name must not be blank.", nameof(displayName));
            HeroId = heroId;
            ContentId = contentId;
            DisplayName = displayName;
            SpawnPoint = spawnPoint;
            OccupiedSpace = occupiedSpace;
        }

        public HeroId HeroId { get; }
        public HeroContentId ContentId { get; }
        public string DisplayName { get; }
        public ConfiguredSpawnPointReference SpawnPoint { get; }
        public ConfiguredSpaceReference OccupiedSpace { get; }
    }

    public sealed class QuickGameMinionPlacementDefinition
    {
        public QuickGameMinionPlacementDefinition(
            MinionId minionId,
            TeamSide teamSide,
            StandardMinionType minionType,
            ConfiguredSpawnPointReference spawnPoint,
            ConfiguredSpaceReference occupiedSpace)
        {
            if (!minionId.IsDefined) throw new ArgumentException("Minion placement requires a minion ID.", nameof(minionId));
            if (teamSide != TeamSide.Red && teamSide != TeamSide.Blue) throw new ArgumentOutOfRangeException(nameof(teamSide));
            if (minionType == StandardMinionType.None) throw new ArgumentOutOfRangeException(nameof(minionType));
            MinionId = minionId;
            TeamSide = teamSide;
            MinionType = minionType;
            SpawnPoint = spawnPoint;
            OccupiedSpace = occupiedSpace;
        }

        public MinionId MinionId { get; }
        public TeamSide TeamSide { get; }
        public StandardMinionType MinionType { get; }
        public ConfiguredSpawnPointReference SpawnPoint { get; }
        public ConfiguredSpaceReference OccupiedSpace { get; }
    }

    public sealed class QuickGameSetupConfiguration
    {
        private readonly IReadOnlyList<QuickGameHeroPlacementDefinition> heroes;
        private readonly IReadOnlyList<QuickGameMinionPlacementDefinition> minions;

        public QuickGameSetupConfiguration(
            MapDefinitionReference requiredMap,
            int waveCount,
            int lifeCountPerTeam,
            IEnumerable<QuickGameHeroPlacementDefinition> heroes,
            IEnumerable<QuickGameMinionPlacementDefinition> minions)
        {
            if (string.IsNullOrWhiteSpace(requiredMap.MapId.Value) || requiredMap.ContentRevision <= 0)
                throw new ArgumentException("Quick Game setup requires a map identity and content revision.", nameof(requiredMap));
            if (waveCount <= 0) throw new ArgumentOutOfRangeException(nameof(waveCount));
            if (lifeCountPerTeam <= 0) throw new ArgumentOutOfRangeException(nameof(lifeCountPerTeam));
            var heroArray = heroes?.ToArray() ?? throw new ArgumentNullException(nameof(heroes));
            var minionArray = minions?.ToArray() ?? throw new ArgumentNullException(nameof(minions));
            if (heroArray.Length == 0) throw new ArgumentException("Quick Game setup requires heroes.", nameof(heroes));
            if (minionArray.Length == 0) throw new ArgumentException("Quick Game setup requires minions.", nameof(minions));
            if (heroArray.Any(value => value == null) || minionArray.Any(value => value == null))
                throw new ArgumentException("Quick Game setup placements must not contain null entries.");
            if (heroArray.Select(value => value.HeroId).Distinct().Count() != heroArray.Length)
                throw new ArgumentException("Quick Game hero IDs must be unique.", nameof(heroes));
            if (minionArray.Select(value => value.MinionId).Distinct().Count() != minionArray.Length)
                throw new ArgumentException("Quick Game minion IDs must be unique.", nameof(minions));

            RequiredMap = requiredMap;
            WaveCount = waveCount;
            LifeCountPerTeam = lifeCountPerTeam;
            this.heroes = Array.AsReadOnly(heroArray);
            this.minions = Array.AsReadOnly(minionArray);
        }

        public MapDefinitionReference RequiredMap { get; }
        public int WaveCount { get; }
        public int LifeCountPerTeam { get; }
        public IReadOnlyList<QuickGameHeroPlacementDefinition> Heroes => heroes;
        public IReadOnlyList<QuickGameMinionPlacementDefinition> Minions => minions;

        public static QuickGameSetupConfiguration CreateMirroredWaspOneVsOne()
        {
            var mapId = new MapId("official-single-lane");
            ConfiguredSpawnPointReference Spawn(string value) =>
                new ConfiguredSpawnPointReference(mapId, new SpawnPointId(value));
            ConfiguredSpaceReference Space(string value) =>
                new ConfiguredSpaceReference(mapId, new SpaceId(value));
            QuickGameMinionPlacementDefinition Minion(
                string id, TeamSide side, StandardMinionType type, string spawn, string space) =>
                new QuickGameMinionPlacementDefinition(new MinionId(id), side, type, Spawn(spawn), Space(space));

            return new QuickGameSetupConfiguration(
                new MapDefinitionReference(mapId, 1),
                3,
                4,
                new[]
                {
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue),
                        new HeroContentId("wasp"),
                        "Wasp",
                        Spawn("red-hero-01"),
                        Space("q-2_r-5")),
                    new QuickGameHeroPlacementDefinition(
                        new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue),
                        new HeroContentId("wasp"),
                        "Wasp",
                        Spawn("blue-hero-01"),
                        Space("q-3_r10"))
                },
                new[]
                {
                    Minion("red-melee-01", TeamSide.Red, StandardMinionType.Melee, "red-melee-minion-04", "q-1_r-1"),
                    Minion("red-melee-02", TeamSide.Red, StandardMinionType.Melee, "red-melee-minion-05", "q-1_r0"),
                    Minion("red-melee-03", TeamSide.Red, StandardMinionType.Melee, "red-melee-minion-08", "q-8_r4"),
                    Minion("red-melee-04", TeamSide.Red, StandardMinionType.Melee, "red-melee-minion-09", "q-4_r4"),
                    Minion("red-ranged-01", TeamSide.Red, StandardMinionType.Ranged, "red-ranged-minion-02", "q-4_r2"),
                    Minion("red-heavy-01", TeamSide.Red, StandardMinionType.Heavy, "red-heavy-minion-03", "q-7_r6"),
                    Minion("blue-melee-01", TeamSide.Blue, StandardMinionType.Melee, "blue-melee-minion-03", "q-3_r2"),
                    Minion("blue-melee-02", TeamSide.Blue, StandardMinionType.Melee, "blue-melee-minion-04", "q1_r2"),
                    Minion("blue-melee-03", TeamSide.Blue, StandardMinionType.Melee, "blue-melee-minion-07", "q-6_r6"),
                    Minion("blue-melee-04", TeamSide.Blue, StandardMinionType.Melee, "blue-melee-minion-08", "q-6_r7"),
                    Minion("blue-ranged-01", TeamSide.Blue, StandardMinionType.Ranged, "blue-ranged-minion-02", "q-3_r4"),
                    Minion("blue-heavy-01", TeamSide.Blue, StandardMinionType.Heavy, "blue-heavy-minion-01", "q0_r0")
                });
        }
    }

    public sealed class QuickGameTeamSetupState
    {
        public QuickGameTeamSetupState(TeamId teamId, TeamSide teamSide, int lifeCount, ZoneId throneZoneId)
        {
            TeamId = teamId;
            TeamSide = teamSide;
            LifeCount = lifeCount;
            ThroneZoneId = throneZoneId;
        }

        public TeamId TeamId { get; }
        public TeamSide TeamSide { get; }
        public int LifeCount { get; }
        public ZoneId ThroneZoneId { get; }
    }

    public sealed class QuickGameHeroSetupState
    {
        public QuickGameHeroSetupState(
            HeroId heroId, HeroContentId contentId, string displayName, TeamId teamId, TeamSide teamSide,
            ConfiguredSpawnPointReference spawnPoint, ConfiguredSpaceReference occupiedSpace, HexCoordinate coordinate)
        {
            HeroId = heroId;
            ContentId = contentId;
            DisplayName = displayName;
            TeamId = teamId;
            TeamSide = teamSide;
            SpawnPoint = spawnPoint;
            OccupiedSpace = occupiedSpace;
            Coordinate = coordinate;
        }

        public HeroId HeroId { get; }
        public HeroContentId ContentId { get; }
        public string DisplayName { get; }
        public TeamId TeamId { get; }
        public TeamSide TeamSide { get; }
        public ConfiguredSpawnPointReference SpawnPoint { get; }
        public ConfiguredSpaceReference OccupiedSpace { get; }
        public HexCoordinate Coordinate { get; }
    }

    public sealed class QuickGameMinionSetupState
    {
        public QuickGameMinionSetupState(
            MinionId minionId, TeamId teamId, TeamSide teamSide, StandardMinionType minionType,
            ConfiguredSpawnPointReference spawnPoint, ConfiguredSpaceReference occupiedSpace, HexCoordinate coordinate)
        {
            MinionId = minionId;
            TeamId = teamId;
            TeamSide = teamSide;
            MinionType = minionType;
            SpawnPoint = spawnPoint;
            OccupiedSpace = occupiedSpace;
            Coordinate = coordinate;
        }

        public MinionId MinionId { get; }
        public TeamId TeamId { get; }
        public TeamSide TeamSide { get; }
        public StandardMinionType MinionType { get; }
        public ConfiguredSpawnPointReference SpawnPoint { get; }
        public ConfiguredSpaceReference OccupiedSpace { get; }
        public HexCoordinate Coordinate { get; }
    }

    public sealed class QuickGameSetupState
    {
        public QuickGameSetupState(
            MapDefinitionReference selectedMap,
            int waveCount,
            TieBreakerState tieBreakerState,
            IEnumerable<QuickGameTeamSetupState> teams,
            IEnumerable<QuickGameHeroSetupState> heroes,
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            SelectedMap = selectedMap;
            WaveCount = waveCount;
            TieBreakerState = tieBreakerState;
            Teams = Array.AsReadOnly(teams.ToArray());
            Heroes = Array.AsReadOnly(heroes.ToArray());
            Minions = Array.AsReadOnly(minions.ToArray());
        }

        public MapDefinitionReference SelectedMap { get; }
        public int WaveCount { get; }
        public TieBreakerState TieBreakerState { get; }
        public IReadOnlyList<QuickGameTeamSetupState> Teams { get; }
        public IReadOnlyList<QuickGameHeroSetupState> Heroes { get; }
        public IReadOnlyList<QuickGameMinionSetupState> Minions { get; }
    }

    public enum QuickGameSetupRejectionReason
    {
        None = 0,
        MissingSelectedMap = 1,
        MapIdentityMismatch = 2,
        ContentRevisionMismatch = 3,
        CrossMapReference = 4,
        MissingSpace = 5,
        MissingSpawnPoint = 6,
        SpawnSpaceMismatch = 7,
        SpawnTeamMismatch = 8,
        SpawnUnitTypeMismatch = 9,
        InvalidHeroThrone = 10,
        InvalidMinionZone = 11,
        OccupancyConflict = 12,
        ParticipantMismatch = 13
    }

    public sealed class QuickGameSetupException : Exception
    {
        public QuickGameSetupException(QuickGameSetupRejectionReason reason, string message) : base(message)
        {
            Reason = reason;
        }

        public QuickGameSetupRejectionReason Reason { get; }
    }

    public static class QuickGameSetupAuthority
    {
        public static QuickGameSetupState Create(
            QuickGameSetupConfiguration configuration,
            MatchParticipants participants,
            BoardMapDefinition selectedMap,
            TieBreakerState tieBreakerState)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (selectedMap == null)
                throw Reject(QuickGameSetupRejectionReason.MissingSelectedMap, "Quick Game setup requires the selected map definition.");
            if (!tieBreakerState.IsDefined) throw new ArgumentException("Quick Game setup requires the initial Tie Breaker result.", nameof(tieBreakerState));
            if (participants.Teams.Count != 2 ||
                participants.Teams.Count(value => value.Side == TeamSide.Red) != 1 ||
                participants.Teams.Count(value => value.Side == TeamSide.Blue) != 1)
                throw Reject(QuickGameSetupRejectionReason.ParticipantMismatch,
                    "Mirrored 1v1 Quick Game setup requires exactly one Red team and one Blue team.");
            if (!configuration.RequiredMap.MapId.Equals(selectedMap.Id))
                throw Reject(QuickGameSetupRejectionReason.MapIdentityMismatch,
                    $"Setup requires map '{configuration.RequiredMap.MapId}', not '{selectedMap.Id}'.");
            if (configuration.RequiredMap.ContentRevision != selectedMap.ContentRevision)
                throw Reject(QuickGameSetupRejectionReason.ContentRevisionMismatch,
                    $"Setup requires map revision {configuration.RequiredMap.ContentRevision}, not {selectedMap.ContentRevision}.");

            var spaces = selectedMap.Spaces.ToDictionary(value => value.Id);
            var spawns = selectedMap.SpawnPoints.ToDictionary(value => value.Id);
            var occupiedSpaceIds = new HashSet<SpaceId>();
            var occupiedSpawnIds = new HashSet<SpawnPointId>();
            var heroStates = new List<QuickGameHeroSetupState>();
            var minionStates = new List<QuickGameMinionSetupState>();

            foreach (var definition in configuration.Heroes)
            {
                Hero participantHero;
                Team team;
                try
                {
                    participantHero = participants.GetHero(definition.HeroId);
                    team = participants.GetTeamForHero(participantHero.Id);
                }
                catch (KeyNotFoundException exception)
                {
                    throw Reject(QuickGameSetupRejectionReason.ParticipantMismatch, exception.Message);
                }

                var resolved = ResolvePlacement(
                    selectedMap, definition.SpawnPoint, definition.OccupiedSpace, spaces, spawns, occupiedSpaceIds, occupiedSpawnIds);
                var expectedTeam = GetMapTeamId(team.Side);
                if (!string.Equals(resolved.Spawn.TeamId, expectedTeam, StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.SpawnTeamMismatch, $"Hero '{definition.HeroId}' uses the wrong team spawn point.");
                if (!string.Equals(resolved.Spawn.UnitType, "hero", StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.SpawnUnitTypeMismatch, $"Hero '{definition.HeroId}' requires a hero spawn point.");
                var zone = selectedMap.GetZoneForSpace(resolved.Space.Id);
                if (!zone.IsThrone || !string.Equals(zone.TeamId, expectedTeam, StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.InvalidHeroThrone, $"Hero '{definition.HeroId}' must start in its own Throne zone.");

                heroStates.Add(new QuickGameHeroSetupState(
                    definition.HeroId, definition.ContentId, definition.DisplayName, team.Id, team.Side,
                    definition.SpawnPoint, definition.OccupiedSpace, resolved.Space.Coordinate));
            }

            if (heroStates.Count != participants.Heroes.Count ||
                participants.Heroes.Any(hero => heroStates.All(state => !state.HeroId.Equals(hero.Id))))
                throw Reject(QuickGameSetupRejectionReason.ParticipantMismatch, "Every configured participant hero requires exactly one setup placement.");

            foreach (var definition in configuration.Minions)
            {
                var team = participants.Teams.Single(value => value.Side == definition.TeamSide);
                var resolved = ResolvePlacement(
                    selectedMap, definition.SpawnPoint, definition.OccupiedSpace, spaces, spawns, occupiedSpaceIds, occupiedSpawnIds);
                if (!string.Equals(resolved.Spawn.TeamId, GetMapTeamId(definition.TeamSide), StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.SpawnTeamMismatch, $"Minion '{definition.MinionId}' uses the wrong team spawn point.");
                if (!string.Equals(resolved.Spawn.UnitType, GetMapUnitType(definition.MinionType), StringComparison.Ordinal))
                    throw Reject(QuickGameSetupRejectionReason.SpawnUnitTypeMismatch, $"Minion '{definition.MinionId}' uses the wrong minion-type spawn point.");
                if (!GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInStartingBattleZone(
                        selectedMap, resolved.Space.Id))
                    throw Reject(QuickGameSetupRejectionReason.InvalidMinionZone, $"Minion '{definition.MinionId}' must start in the starting Battle Zone.");

                minionStates.Add(new QuickGameMinionSetupState(
                    definition.MinionId, team.Id, team.Side, definition.MinionType,
                    definition.SpawnPoint, definition.OccupiedSpace, resolved.Space.Coordinate));
            }

            var teamStates = participants.Teams
                .OrderBy(value => value.Side)
                .Select(team =>
                {
                    var mapTeam = GetMapTeamId(team.Side);
                    var throne = selectedMap.Zones.SingleOrDefault(zone => zone.IsThrone && string.Equals(zone.TeamId, mapTeam, StringComparison.Ordinal));
                    if (throne == null)
                        throw Reject(QuickGameSetupRejectionReason.InvalidHeroThrone, $"Map '{selectedMap.Reference}' has no {mapTeam} Throne zone.");
                    return new QuickGameTeamSetupState(team.Id, team.Side, configuration.LifeCountPerTeam, throne.Id);
                })
                .ToArray();

            return new QuickGameSetupState(
                selectedMap.Reference,
                configuration.WaveCount,
                tieBreakerState,
                teamStates,
                heroStates,
                minionStates);
        }

        private static (BoardMapSpawnPoint Spawn, BoardMapSpace Space) ResolvePlacement(
            BoardMapDefinition selectedMap,
            ConfiguredSpawnPointReference spawnReference,
            ConfiguredSpaceReference spaceReference,
            IReadOnlyDictionary<SpaceId, BoardMapSpace> spaces,
            IReadOnlyDictionary<SpawnPointId, BoardMapSpawnPoint> spawns,
            ISet<SpaceId> occupiedSpaceIds,
            ISet<SpawnPointId> occupiedSpawnIds)
        {
            if (!spawnReference.MapId.Equals(selectedMap.Id) || !spaceReference.MapId.Equals(selectedMap.Id))
                throw Reject(QuickGameSetupRejectionReason.CrossMapReference, "Setup child references must belong to the selected map.");
            if (!spawns.TryGetValue(spawnReference.SpawnPointId, out var spawn))
                throw Reject(QuickGameSetupRejectionReason.MissingSpawnPoint, $"Spawn point '{spawnReference}' is unavailable.");
            if (!spaces.TryGetValue(spaceReference.SpaceId, out var space))
                throw Reject(QuickGameSetupRejectionReason.MissingSpace, $"Space '{spaceReference}' is unavailable.");
            if (!spawn.SpaceId.Equals(space.Id))
                throw Reject(QuickGameSetupRejectionReason.SpawnSpaceMismatch, $"Spawn point '{spawnReference}' does not occupy configured space '{spaceReference}'.");
            if (!occupiedSpawnIds.Add(spawn.Id) || !occupiedSpaceIds.Add(space.Id))
                throw Reject(QuickGameSetupRejectionReason.OccupancyConflict, $"Setup placement conflicts at '{space.Id}'.");
            return (spawn, space);
        }

        private static string GetMapTeamId(TeamSide side)
        {
            if (side == TeamSide.Red) return "red";
            if (side == TeamSide.Blue) return "blue";
            throw new ArgumentOutOfRangeException(nameof(side));
        }

        private static string GetMapUnitType(StandardMinionType type)
        {
            switch (type)
            {
                case StandardMinionType.Melee: return "melee-minion";
                case StandardMinionType.Ranged: return "ranged-minion";
                case StandardMinionType.Heavy: return "heavy-minion";
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static QuickGameSetupException Reject(QuickGameSetupRejectionReason reason, string message) =>
            new QuickGameSetupException(reason, message);
    }
}
