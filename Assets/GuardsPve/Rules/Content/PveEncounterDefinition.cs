using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Pve.Content
{
    /// <summary>PVE-D04/D17. A friendly creep the players defend.</summary>
    public sealed class PveCreepPlacement
    {
        public PveCreepPlacement(HexCoordinate coordinate, StandardMinionType minionType)
        {
            Coordinate = coordinate;
            MinionType = minionType;
        }

        public HexCoordinate Coordinate { get; }
        public StandardMinionType MinionType { get; }
    }

    /// <summary>PVE-D12/D17. One enemy of a type entering on a space at a round's start.</summary>
    public sealed class PveSpawn
    {
        public PveSpawn(int round, HexCoordinate coordinate, string enemyTypeId)
        {
            Round = round;
            Coordinate = coordinate;
            EnemyTypeId = enemyTypeId;
        }

        public int Round { get; }
        public HexCoordinate Coordinate { get; }
        public string EnemyTypeId { get; }
    }

    /// <summary>PVE-D03/D04/D12/D17. The encounter layout on one map revision.</summary>
    public sealed class PveEncounterDefinition
    {
        public PveEncounterDefinition(
            string mapId,
            int mapRevision,
            IEnumerable<PveCreepPlacement> creeps,
            IEnumerable<HexCoordinate> heroPlacement,
            IEnumerable<PveSpawn> spawns)
        {
            MapId = mapId;
            MapRevision = mapRevision;
            Creeps = Array.AsReadOnly((creeps ?? Enumerable.Empty<PveCreepPlacement>()).ToArray());
            HeroPlacement = Array.AsReadOnly((heroPlacement ?? Enumerable.Empty<HexCoordinate>()).ToArray());
            Spawns = Array.AsReadOnly((spawns ?? Enumerable.Empty<PveSpawn>()).ToArray());
        }

        public string MapId { get; }
        public int MapRevision { get; }
        public IReadOnlyList<PveCreepPlacement> Creeps { get; }

        /// <summary>The spaces heroes are placed on; their count is the hero limit (PVE-D02).</summary>
        public IReadOnlyList<HexCoordinate> HeroPlacement { get; }

        public IReadOnlyList<PveSpawn> Spawns { get; }

        public MapDefinitionReference MapReference => new MapDefinitionReference(new MapId(MapId), MapRevision);
    }
}
