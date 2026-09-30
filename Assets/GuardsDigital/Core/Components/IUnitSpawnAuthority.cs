using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// One unit miniature currently in the supply: declared match content that is not standing on the
    /// board. Rulebook p.18 defines Spawn as taking a piece that "must be taken from the supply and
    /// cannot be taken from the board", so this is exactly the set a Spawn may draw from.
    ///
    /// It carries the identity-bound content the miniature keeps whether or not it is placed - its
    /// team, its type, and the Spawn point it belongs to - because those are setup content rather than
    /// board state, and the miniature standing back up must be the same miniature.
    /// </summary>
    public readonly struct SpawnableUnit : IEquatable<SpawnableUnit>
    {
        public SpawnableUnit(
            MinionId minionId,
            TeamSide teamSide,
            StandardMinionType minionType,
            ConfiguredSpawnPointReference spawnPoint)
        {
            if (!minionId.IsDefined)
            {
                throw new ArgumentException("A spawnable unit requires a defined minion ID.", nameof(minionId));
            }

            MinionId = minionId;
            TeamSide = teamSide;
            MinionType = minionType;
            SpawnPoint = spawnPoint;
        }

        public MinionId MinionId { get; }
        public TeamSide TeamSide { get; }
        public StandardMinionType MinionType { get; }

        /// <summary>The configured Spawn point this miniature belongs to (p.19: "Spaces on the board used to spawn minions and heroes").</summary>
        public ConfiguredSpawnPointReference SpawnPoint { get; }

        public bool Equals(SpawnableUnit other) =>
            MinionId.Equals(other.MinionId) && TeamSide == other.TeamSide &&
            MinionType == other.MinionType && SpawnPoint.Equals(other.SpawnPoint);

        public override bool Equals(object obj) => obj is SpawnableUnit other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = MinionId.GetHashCode();
                hash = (hash * 397) ^ (int)TeamSide;
                hash = (hash * 397) ^ (int)MinionType;
                return (hash * 397) ^ SpawnPoint.GetHashCode();
            }
        }

        public override string ToString() => $"{MinionId} ({MinionType}) at {SpawnPoint}";
    }

    /// <summary>Why a validated unit Spawn was refused.</summary>
    public enum UnitSpawnRejectionReason
    {
        None = 0,

        /// <summary>No such miniature is declared for this match.</summary>
        UnknownUnit = 1,

        /// <summary>The miniature is already standing on the board, so it is not in the supply.</summary>
        UnitAlreadyOnBoard = 2,

        /// <summary>The miniature's Spawn point is not a playable space of the selected map.</summary>
        UnknownSpawnPoint = 3,

        /// <summary>An obstacle other than a token stands on the Spawn point, so nothing may enter it.</summary>
        SpawnPointOccupied = 4
    }

    /// <summary>
    /// The outcome of one unit Spawn, including whether p.19's token-clearance step actually removed
    /// anything: "If a unit Spawns in a Spawn Point covered by a token, remove the token first, then
    /// spawn the unit."
    /// </summary>
    public readonly struct UnitSpawnCommitResult
    {
        private UnitSpawnCommitResult(
            bool accepted,
            UnitSpawnRejectionReason rejectionReason,
            MinionOccupancy spawned,
            HexCoordinate destination,
            PlacedToken clearedToken)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Spawned = spawned;
            Destination = destination;
            ClearedToken = clearedToken;
        }

        public bool Accepted { get; }
        public UnitSpawnRejectionReason RejectionReason { get; }

        /// <summary>The miniature now standing on the board, or <c>null</c> when the Spawn was refused.</summary>
        public MinionOccupancy Spawned { get; }

        public HexCoordinate Destination { get; }

        /// <summary>The token removed to clear the Spawn point, or <c>null</c> when none covered it.</summary>
        public PlacedToken ClearedToken { get; }

        public static UnitSpawnCommitResult Accept(
            MinionOccupancy spawned, HexCoordinate destination, PlacedToken clearedToken) =>
            new UnitSpawnCommitResult(
                true, UnitSpawnRejectionReason.None, spawned, destination, clearedToken);

        public static UnitSpawnCommitResult Reject(UnitSpawnRejectionReason reason)
        {
            if (reason == UnitSpawnRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "A refused unit Spawn requires a reason.");
            }

            return new UnitSpawnCommitResult(false, reason, null, default, null);
        }
    }

    /// <summary>
    /// The board port a Spawn capability reaches, beside <see cref="ITokenPlacementAuthority"/> and
    /// <see cref="IBoardDisplacementAuthority"/> and for the reason milestone 6.4 recorded when it
    /// added the second: taking a miniature out of the supply is a different authority question from
    /// relocating one that is already standing, and p.18 keeps Place, Push, and Spawn distinct
    /// operations rather than modes of one.
    ///
    /// It is deliberately narrow, and in particular it answers no eligibility question of its own.
    /// p.18's Respawn adds a rule about how many Spawn points of a type and colour exist relative to
    /// miniatures present in the Battle Zone; that specialized rule stays unimplemented and belongs
    /// with hero and minion Respawn, so nothing here may be read as supplying it.
    /// </summary>
    public interface IUnitSpawnAuthority
    {
        /// <summary>
        /// Every declared unit miniature not currently on the board, in canonical identity order.
        ///
        /// Derived from the declared roster and live occupancy on every call, never stored. That is
        /// the same decision milestone 6.2 made for the token supply and for the same reason: a second
        /// count is a second thing that can disagree with the board, and there is nothing here to
        /// serialize, so a restored match cannot carry a stale supply.
        /// </summary>
        IReadOnlyList<SpawnableUnit> FindUnitSupply();

        /// <summary>
        /// The playable coordinate of a configured Spawn point, or <c>false</c> when the selected map
        /// declares no such point.
        /// </summary>
        bool TryGetSpawnPointCoordinate(
            ConfiguredSpawnPointReference spawnPoint, out HexCoordinate coordinate);

        /// <summary>
        /// Every playable space the selected map puts a Spawn point on, in canonical axial order.
        ///
        /// <para>Milestone 8.20. Arien's Liquid Leap, Magical Current and Stranger Tide are the first
        /// verified texts that ask where the Spawn points <em>are</em> rather than resolving one they
        /// already name: "Place yourself into a space in range without a spawn point and not adjacent
        /// to an empty spawn point". Both clauses are questions about the map, so this answers the map
        /// and nothing else - which point, whose team, and what unit type are not asked, because the
        /// sentence does not ask them.</para>
        ///
        /// <para>Derived on every call rather than stored, exactly as <see cref="FindUnitSupply"/> is
        /// and for the same reason: there is nothing here a restore could carry stale. It still adds
        /// no eligibility rule - whether a space with a Spawn point may be entered at all remains
        /// p.18's business, and this only says where they stand.</para>
        /// </summary>
        IReadOnlyList<HexCoordinate> FindSpawnPointSpaces();

        /// <summary>
        /// Puts a supply miniature onto its declared Spawn point as one transaction: p.19's token
        /// clearance first, then the occupant. Refusals are returned rather than thrown, because a
        /// Spawn point taken between derivation and commit is a real race an effect must report.
        /// </summary>
        UnitSpawnCommitResult CommitValidatedUnitSpawn(MinionId minionId);

        /// <summary>
        /// Milestone 9.21d. The bodies of a hero that are not on the board, in body order - the supply
        /// p.19's Spawn takes a body from ("must be taken from the supply and cannot be taken from the
        /// board"; RZ-Q1: the declared supply, with no reclaim). Derived on every call from the
        /// declared supply and the bodies standing, never stored, for the reason
        /// <see cref="FindUnitSupply"/> is. Empty for a hero that declares one body.
        /// </summary>
        IReadOnlyList<int> FindHeroBodySupply(HeroId heroId);

        /// <summary>
        /// Milestone 9.21d. Puts the lowest body of <see cref="FindHeroBodySupply"/> onto an empty
        /// playable space, as one more body of a hero already standing on the board. Refused (false)
        /// when the hero has no body standing - off the board he returns only by p.7's respawn, as one
        /// body - when no body is left in the supply, or when the space is not empty: a space taken
        /// between derivation and commit is a real race the effect must report.
        /// </summary>
        bool CommitValidatedHeroBodySpawn(HeroId heroId, HexCoordinate destination, out HeroOccupancy spawned);
    }
}
