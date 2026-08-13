using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Units.Minions
{
    // A minion's authoritative runtime board state, distinct from the immutable
    // QuickGameMinionSetupState that records only its configured setup placement. The instance is
    // immutable and replaced through WithPosition when the minion moves: reference identity of a
    // given instance therefore means "unchanged", which the presentation refresh path uses as its
    // change detector, while identity across positions is the stable MinionId.
    public sealed class MinionOccupancy
    {
        private MinionOccupancy(
            MinionId minionId,
            TeamId teamId,
            TeamSide teamSide,
            StandardMinionType minionType,
            ConfiguredSpawnPointReference spawnPoint,
            ConfiguredSpaceReference occupiedSpace,
            HexCoordinate coordinate)
        {
            if (!minionId.IsDefined) throw new ArgumentException("Minion occupancy requires a defined minion ID.", nameof(minionId));
            if (!teamId.IsDefined) throw new ArgumentException("Minion occupancy requires a defined team ID.", nameof(teamId));
            if (teamSide != TeamSide.Red && teamSide != TeamSide.Blue)
                throw new ArgumentException("Minion occupancy requires a Red or Blue team side.", nameof(teamSide));
            if (minionType != StandardMinionType.Melee &&
                minionType != StandardMinionType.Ranged &&
                minionType != StandardMinionType.Heavy)
                throw new ArgumentException("Minion occupancy requires a standard intrinsic minion type.", nameof(minionType));

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

        public static MinionOccupancy FromSetup(QuickGameMinionSetupState setup)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            return new MinionOccupancy(
                setup.MinionId,
                setup.TeamId,
                setup.TeamSide,
                setup.MinionType,
                setup.SpawnPoint,
                setup.OccupiedSpace,
                setup.Coordinate);
        }

        public MinionOccupancy WithPosition(ConfiguredSpaceReference occupiedSpace, HexCoordinate coordinate) =>
            new MinionOccupancy(MinionId, TeamId, TeamSide, MinionType, SpawnPoint, occupiedSpace, coordinate);
    }
}
