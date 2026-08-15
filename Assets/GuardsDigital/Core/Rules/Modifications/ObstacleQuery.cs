using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The p.18 obstacle rule, expressed as a query a movement or displacement algorithm consults.
    ///
    /// "Units, terrain (including printed on the board), tokens, as well as any objects represented by
    /// miniatures, are obstacles. There can never be more than one obstacle in any one space." The board's
    /// occupancy index already enforces the second sentence for every occupant kind, so this query reads
    /// it rather than re-deriving obstacles from per-kind lists. Since milestone 6.8e the classification
    /// is three-way: a playable space may hold an occupant, a declared terrain space exists and is
    /// permanently an obstacle, and a coordinate off the map is not an obstacle at all - it is nothing,
    /// and no permission answers it.
    ///
    /// The permission it honours is the other half of p.18: a unit that ignores obstacles "is allowed to
    /// enter a space with an obstacle and leave that space in any direction, while moving. However, a unit
    /// can never end its movement on the same space as an obstacle." Transit and endpoint are therefore
    /// separate questions, and only the first one the permission can answer - for an occupied space and
    /// for terrain alike, because p.18 lists both as obstacles.
    /// </summary>
    public static class ObstacleQuery
    {
        public static bool IsObstacle(AdjacentMoveAuthority board, HexCoordinate coordinate)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            return !board.IsPlayable(coordinate) || board.IsOccupied(coordinate);
        }

        /// <summary>
        /// Whether <paramref name="mover"/> may pass through <paramref name="coordinate"/> while moving.
        /// A granted <see cref="RulePermissionKind.MoveThroughObstacles"/> lets it pass through an
        /// occupied space or a declared terrain space; nothing lets it pass outside the map.
        /// </summary>
        public static bool BlocksTransit(
            AdjacentMoveAuthority board,
            HexCoordinate coordinate,
            RuleObject mover,
            RuleModificationSet modifications)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!board.IsPlayable(coordinate))
            {
                if (!board.IsTerrain(coordinate))
                {
                    // Off the map: not an obstacle to ignore, and no permission answers it.
                    return true;
                }

                return !HasMoveThroughPermission(mover, modifications);
            }

            if (!board.IsOccupied(coordinate))
            {
                return false;
            }

            return !HasMoveThroughPermission(mover, modifications);
        }

        private static bool HasMoveThroughPermission(RuleObject mover, RuleModificationSet modifications) =>
            mover.IsDefined && modifications != null &&
            modifications.HasPermission(mover, RulePermissionKind.MoveThroughObstacles);

        /// <summary>
        /// Whether <paramref name="coordinate"/> may be a movement endpoint. Deliberately identical to
        /// <see cref="IsObstacle"/> and deliberately not offered a permission parameter: p.18 allows no
        /// unit to end its movement on an obstacle, so a caller cannot pass one in.
        /// </summary>
        public static bool BlocksEndOfMovement(AdjacentMoveAuthority board, HexCoordinate coordinate) =>
            IsObstacle(board, coordinate);
    }
}
