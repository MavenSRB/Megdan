using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// The board port rulebook p.8's Lane-Push transition reaches (milestone 6.10, widened at 6.11),
    /// beside the displacement, token-placement, unit-spawn and exchange ports and for the same reason
    /// each of those exists: the rule needs the authoritative questions it actually asks and its
    /// validated commits, not the whole board authority.
    ///
    /// Milestone 6.10 recorded that anything about occupancy was deliberately absent, because the stage
    /// that removes and respawns minions was 6.11's and a port that could reach them would have let that
    /// milestone expose half a transition. This is that milestone, so the occupancy half joins here
    /// rather than on <see cref="IUnitSpawnAuthority"/>: a card capability reaches that port through
    /// <c>MatchComponents</c>, and p.8's respawn is a system transition's business rather than a card's.
    /// </summary>
    public interface ILaneAdvanceAuthority
    {
        BoardMapDefinition MapDefinition { get; }

        /// <summary>The Zone the Lane has reached, or the named absence on a map that declares none.</summary>
        ZoneId? CurrentBattleZoneId { get; }

        /// <summary>
        /// Moves the Battle Zone one verified step along the ordered Lane, or refuses and leaves it
        /// where it is. Which Zone is next is the caller's rules decision; that the answer is a Zone
        /// one step away on this map's Lane is verified here.
        /// </summary>
        bool CommitValidatedBattleZoneAdvance(ZoneId destination);

        /// <summary>
        /// The minion miniatures standing in one Zone, in canonical identity order (milestone 6.11).
        ///
        /// Miniatures rather than p.13's effective minions: "Remove all minions in the previous Battle
        /// Zone" is answered by what is standing there, and the respawn that follows puts miniatures
        /// back on Spawn points. A hero that counts as a minion is not a miniature and a Lane Push does
        /// not sweep it off the board.
        /// </summary>
        IReadOnlyList<MinionOccupancy> FindMinionsInZone(ZoneId zoneId);

        /// <summary>The minion miniatures in the supply, in canonical identity order.</summary>
        IReadOnlyList<MinionOccupancy> FindMinionSupply();

        /// <summary>
        /// Takes one standing miniature off the board and back into the supply, which is the first half
        /// of p.8's third step. Refuses rather than throws when the miniature is not standing, because
        /// the transition derives its subjects from the same board it then mutates and a disagreement
        /// would be a rules answer this rule cannot give.
        /// </summary>
        bool CommitValidatedLanePushRemoval(MinionId minionId);

        /// <summary>
        /// Puts a supply miniature onto a named Spawn point, clearing p.19's covering token first.
        ///
        /// Distinct from <see cref="IUnitSpawnAuthority.CommitValidatedUnitSpawn"/>, which puts a
        /// miniature back on the Spawn point it belongs to: p.8's respawn is about the Spawn points of
        /// the new Battle Zone, which are not the ones the miniatures started on.
        /// </summary>
        UnitSpawnCommitResult CommitValidatedRespawnAtSpawnPoint(
            MinionId minionId, SpawnPointId spawnPointId);

        /// <summary>
        /// Places a supply miniature on a chosen empty space - p.8's sidebar answer for a Spawn point an
        /// obstacle other than a token is standing on.
        /// </summary>
        bool CommitValidatedLanePushPlacement(MinionId minionId, HexCoordinate destination);

        /// <summary>Whether a coordinate is a playable space of this map with nothing standing on it.</summary>
        bool IsEmptyPlayableSpace(HexCoordinate coordinate);

        /// <summary>
        /// Whether a token is what is standing on a coordinate. p.8 clears a token out of a Spawn point
        /// and sends the miniature elsewhere for every other occupant, so the push has to be able to
        /// tell the one case from the other before it commits anything.
        /// </summary>
        bool HasTokenAt(HexCoordinate coordinate);
    }
}
