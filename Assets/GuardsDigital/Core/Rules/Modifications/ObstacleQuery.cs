using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
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
    /// Milestone 8.7 adds a fourth source, and adds it here rather than beside each caller: a card may
    /// declare a <see cref="ConditionalObstacleOverlay"/> that makes spaces count as obstacles while a
    /// qualifying hero acts. p.18 gives the classification one owner, so movement, push and place
    /// legality all get the same answer by asking the same question, and a caller cannot consume the
    /// overlay for one and forget it for another. Everything conditional about the overlay has already
    /// been resolved by <see cref="RuleModificationQuery"/> - the anchor and who is acting - so what is
    /// left is a space, which is what this query is about.
    ///
    /// The modification set is required on all three questions for the reason milestone 6.8f made it
    /// required elsewhere: a caller that omitted it would get a board whose declared obstacles had
    /// quietly stopped existing. <see cref="RuleModificationSet.Empty"/> is the named none.
    ///
    /// The permission it honours is the other half of p.18: a unit that ignores obstacles "is allowed to
    /// enter a space with an obstacle and leave that space in any direction, while moving. However, a unit
    /// can never end its movement on the same space as an obstacle." Transit and endpoint are therefore
    /// separate questions, and only the first one the permission can answer - for an occupied space, for
    /// terrain and for an overlaid space alike, because p.18 lists them all as obstacles.
    /// </summary>
    public static class ObstacleQuery
    {
        public static bool IsObstacle(
            AdjacentMoveAuthority board, HexCoordinate coordinate, RuleModificationSet modifications)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            return !board.IsPlayable(coordinate) || board.IsOccupied(coordinate) ||
                modifications.AnyOverlayMakesAnObstacleOf(coordinate);
        }

        /// <summary>
        /// Whether <paramref name="mover"/> may pass through <paramref name="coordinate"/> while moving.
        /// A granted <see cref="RulePermissionKind.MoveThroughObstacles"/> lets it pass through an
        /// occupied space, a declared terrain space, or a space an overlay has made an obstacle;
        /// nothing lets it pass outside the map.
        /// </summary>
        public static bool BlocksTransit(
            AdjacentMoveAuthority board,
            HexCoordinate coordinate,
            RuleObject mover,
            RuleModificationSet modifications) =>
            BlocksTransit(board, coordinate, mover, modifications, moverIgnoresObstacles: false);

        /// <summary>
        /// The same question for a movement that crosses obstacles because the card text says so
        /// rather than because the mover was granted p.18's permission.
        ///
        /// Milestone 8.23. Blink Strike moves "2 spaces in a straight line through an enemy unit",
        /// which is the effect's property and not the hero's: nothing about Tigerclaw changes, and
        /// the very next Move he makes is stopped by that unit again. That is the shape
        /// <see cref="GuardsDigital.Core.Rules.Displacement.PushPathQuery"/> already reads for
        /// p.18's Push, and it is read the same way here - the declared crossing and the granted
        /// permission are two sources of one answer, so they are asked together and the transit rule
        /// stays in one place.
        ///
        /// It answers transit only. Off the map is still nothing rather than an obstacle to cross,
        /// and where a movement may END is <see cref="BlocksEndOfMovement"/>'s question, which
        /// neither this flag nor the permission is offered.
        /// </summary>
        /// <param name="permittedActivity">
        /// Milestone 8.35z30, RULING 47. What the mover is doing, so a permission its card
        /// scoped to one activity is read only by that activity. A caller naming nothing gets the
        /// answer it always got: only an unqualified grant lets the mover through.
        /// </param>
        public static bool BlocksTransit(
            AdjacentMoveAuthority board,
            HexCoordinate coordinate,
            RuleObject mover,
            RuleModificationSet modifications,
            bool moverIgnoresObstacles,
            RuleActivity permittedActivity = RuleActivity.None)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (!board.IsPlayable(coordinate))
            {
                if (!board.IsTerrain(coordinate))
                {
                    // Off the map: not an obstacle to ignore, and no permission answers it.
                    return true;
                }

                return !MayCrossObstacles(
                    mover, modifications, moverIgnoresObstacles, permittedActivity);
            }

            if (!board.IsOccupied(coordinate) &&
                !modifications.AnyOverlayMakesAnObstacleOf(coordinate))
            {
                return false;
            }

            return !MayCrossObstacles(
                mover, modifications, moverIgnoresObstacles, permittedActivity);
        }

        private static bool MayCrossObstacles(
            RuleObject mover,
            RuleModificationSet modifications,
            bool moverIgnoresObstacles,
            RuleActivity permittedActivity) =>
            moverIgnoresObstacles ||
            (mover.IsDefined && modifications.HasPermission(
                mover, RulePermissionKind.MoveThroughObstacles, permittedActivity));

        /// <summary>Same obstacle rule with only a simultaneous pair's old occupancy vacated.</summary>
        public static bool BlocksTransitAfterVacating(AdjacentMoveAuthority board, HexCoordinate coordinate,
            RuleObject mover, RuleModificationSet modifications, bool ignoresObstacles,
            RuleActivity activity, IReadOnlyCollection<BoardObjectReference> vacating) =>
            BlocksTransit(board, coordinate, mover, modifications, ignoresObstacles, activity) &&
            !IsOnlyVacatingOccupancy(board, coordinate, modifications, vacating);

        public static bool BlocksEndAfterVacating(AdjacentMoveAuthority board, HexCoordinate coordinate,
            RuleModificationSet modifications, IReadOnlyCollection<BoardObjectReference> vacating) =>
            BlocksEndOfMovement(board, coordinate, modifications) &&
            !IsOnlyVacatingOccupancy(board, coordinate, modifications, vacating);

        private static bool IsOnlyVacatingOccupancy(AdjacentMoveAuthority board, HexCoordinate coordinate,
            RuleModificationSet modifications, IReadOnlyCollection<BoardObjectReference> vacating) =>
            board.IsPlayable(coordinate) && !modifications.AnyOverlayMakesAnObstacleOf(coordinate) &&
            board.TryGetObjectAt(coordinate, out var occupant) && vacating.Contains(occupant);

        /// <summary>
        /// Whether <paramref name="coordinate"/> may be a movement endpoint. Deliberately identical to
        /// <see cref="IsObstacle"/> and deliberately not offered a permission parameter: p.18 allows no
        /// unit to end its movement on an obstacle, so a caller cannot pass one in.
        /// </summary>
        public static bool BlocksEndOfMovement(
            AdjacentMoveAuthority board, HexCoordinate coordinate, RuleModificationSet modifications) =>
            IsObstacle(board, coordinate, modifications);

        /// <summary>
        /// Whether <paramref name="coordinate"/> may receive a placed piece.
        ///
        /// The same classification for a caller holding the narrow placement port rather than the
        /// movement authority. p.18's Place puts an object "into a different space", and p.19 allows
        /// one obstacle per space, so a Place needs a space with none - which is what the port's own
        /// empty-and-playable answer means, plus whatever an overlay has closed. No permission is
        /// offered for the reason <see cref="BlocksEndOfMovement"/> offers none: a Place is not
        /// movement, and p.18's grant is about moving through obstacles rather than resting on them.
        /// </summary>
        public static bool BlocksPlacement(
            ITokenPlacementAuthority spaces,
            HexCoordinate coordinate,
            RuleModificationSet modifications)
        {
            if (spaces == null)
            {
                throw new ArgumentNullException(nameof(spaces));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            return !spaces.IsEmptyPlayableSpace(coordinate) ||
                modifications.AnyOverlayMakesAnObstacleOf(coordinate);
        }
    }
}
