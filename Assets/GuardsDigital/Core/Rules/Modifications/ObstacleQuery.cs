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
    /// Milestone 8.61d adds the obstacle's own side of the same page, and adds it here for the same
    /// reason: "If the push is 'ignoring obstacles', or the obstacle can be 'moved through', the pushed
    /// target is moved through obstacles as long as it can reach an empty space." A token whose printed
    /// text says units may move through it makes its space transit-legal for every unit of either team
    /// - see <see cref="AnyTokenHereMayBeMovedThrough"/> - and changes nothing about where a movement
    /// may END, because the next sentence of p.18 allows no unit to stop on an obstacle at all.
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

            // Milestone 8.61d. p.18's OTHER clause, and the obstacle's own side of it: "If the push is
            // 'ignoring obstacles', or THE OBSTACLE CAN BE 'MOVED THROUGH', the pushed target is moved
            // through obstacles as long as it can reach an empty space." Min's mines print it -
            // "units may move through them" - and it is not a grant to anybody: it holds for every unit
            // of either team, so it is read off the piece standing in the way rather than off the
            // mover, and no permission, activity scope or card-text crossing flag is consulted.
            //
            // It is asked only after the space has been found to be an obstacle, and it is refused
            // where an overlay is one of the reasons: a milestone 8.7 overlay is a second obstacle
            // declared over the same space by somebody else's card, and a token's own sentence cannot
            // speak for it.
            if (!modifications.AnyOverlayMakesAnObstacleOf(coordinate) &&
                AnyTokenHereMayBeMovedThrough(board, coordinate))
            {
                return false;
            }

            return !MayCrossObstacles(
                mover, modifications, moverIgnoresObstacles, permittedActivity);
        }

        /// <summary>
        /// Whether a token standing on <paramref name="coordinate"/> declares rulebook p.18's
        /// "can be moved through". Milestone 8.61d.
        ///
        /// <para>It is read from the trusted <see cref="TokenDefinition"/> the placed instance carries
        /// rather than from a <see cref="RuleModificationSet"/>, and the difference is a correctness
        /// one rather than a convenience: this question is asked by every route, push and coupled walk
        /// in the engine, several of which are reachable with <see cref="RuleModificationSet.Empty"/>,
        /// and a mine that stopped being walkable because a caller supplied no live rules would be a
        /// silent, board-shaped wrong answer. The declaration is definition content that a snapshot
        /// re-binds from the catalog, so it is available wherever the board is.</para>
        /// </summary>
        public static bool AnyTokenHereMayBeMovedThrough(
            AdjacentMoveAuthority board, HexCoordinate coordinate)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var placed = board.PlacedTokens;
            for (var index = 0; index < placed.Count; index++)
            {
                if (placed[index].Coordinate.Equals(coordinate) &&
                    DeclaresMayBeMovedThrough(placed[index].Definition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether one token definition prints p.18's "units may move through them". Stated once here,
        /// at the classification's one owner, so content declares the rule and nothing has to agree
        /// with a second reader about what the declaration looks like.
        /// </summary>
        public static bool DeclaresMayBeMovedThrough(TokenDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            for (var index = 0; index < definition.Properties.Count; index++)
            {
                var property = definition.Properties[index];
                if (property.AffectedObject == TokenPropertyAffectedObject.TheTokenItself &&
                    property.Modification != null &&
                    property.Modification.Kind == RuleModificationKind.MayBeMovedThrough)
                {
                    return true;
                }
            }

            return false;
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
