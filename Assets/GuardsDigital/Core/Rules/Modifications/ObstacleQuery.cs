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
    /// it rather than re-deriving obstacles from per-kind lists; a space off the playable set is terrain
    /// or off-board and is an obstacle either way.
    ///
    /// The permission it honours is the other half of p.18: a unit that ignores obstacles "is allowed to
    /// enter a space with an obstacle and leave that space in any direction, while moving. However, a unit
    /// can never end its movement on the same space as an obstacle." Transit and endpoint are therefore
    /// separate questions, and only the first one the permission can answer.
    ///
    /// Milestone 6.8 owns this decision point and the permission it reads; the Movement action does not
    /// consult it yet. Committing a route that crosses an occupied space needs the move to assert
    /// occupancy only at its endpoint, which is a change to route commitment rather than to obstacle
    /// classification, and it belongs with the milestones that own movement and displacement. Wiring the
    /// permission into today's one-space-at-a-time commit would offer destinations the commit path must
    /// then refuse, which is exactly the shortcut a later milestone would have to replace.
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
        /// occupied space; nothing lets it pass outside the playable board.
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
                return true;
            }

            if (!board.IsOccupied(coordinate))
            {
                return false;
            }

            return !(mover.IsDefined && modifications != null &&
                modifications.HasPermission(mover, RulePermissionKind.MoveThroughObstacles));
        }

        /// <summary>
        /// Whether <paramref name="coordinate"/> may be a movement endpoint. Deliberately identical to
        /// <see cref="IsObstacle"/> and deliberately not offered a permission parameter: p.18 allows no
        /// unit to end its movement on an obstacle, so a caller cannot pass one in.
        /// </summary>
        public static bool BlocksEndOfMovement(AdjacentMoveAuthority board, HexCoordinate coordinate) =>
            IsObstacle(board, coordinate);
    }
}
