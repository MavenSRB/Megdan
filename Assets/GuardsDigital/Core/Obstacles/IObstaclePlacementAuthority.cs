using System.Collections.Generic;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Obstacles
{
    /// <summary>
    /// The board-owned operations for placing a persistent obstacle miniature. Obstacles here are
    /// neither terrain nor tokens: their identity and lifetime belong to the board occupancy state.
    /// </summary>
    public interface IObstaclePlacementAuthority
    {
        IReadOnlyList<ObstacleOccupancy> ObstacleOccupancies { get; }

        bool TryGetObstacleCoordinate(ObstacleId obstacleId, out HexCoordinate coordinate);

        /// <summary>
        /// Places an absent obstacle or repositions the same existing piece. Refuses occupied,
        /// non-playable and unchanged destinations without mutating either occupancy record.
        /// </summary>
        bool TryPlaceOrRepositionObstacle(ObstacleId obstacleId, HexCoordinate destination);

        /// <summary>Removes a standing obstacle; an absent identity is a rejected stale operation.</summary>
        bool TryRemoveObstacle(ObstacleId obstacleId);
    }
}
