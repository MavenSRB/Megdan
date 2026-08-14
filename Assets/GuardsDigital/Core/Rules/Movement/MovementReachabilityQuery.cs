using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.Movement
{
    public static class MovementReachabilityQuery
    {
        /// <summary>
        /// Authority-backed entry: the board's occupancy index supplies the blocked set directly, so
        /// every occupant kind blocks without this query enumerating per-kind lists.
        ///
        /// It asks <see cref="ObstacleQuery"/> rulebook p.18's two separate questions rather than one.
        /// Whether the mover may pass through an occupied space depends on the Move-Through permission
        /// in <paramref name="modifications"/>; whether a space may be a movement endpoint never does,
        /// because p.18 lets no unit end its movement on an obstacle. Milestone 6.8c added this: the
        /// permission and the query existed from 6.8, and this was the caller neither had.
        /// </summary>
        public static IReadOnlyList<ReachableMovementDestination> FindReachableDestinations(
            HeroOccupancy selectedHero,
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
            int movementAllowance,
            RuleModificationSet modifications = null)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (movementAllowance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movementAllowance),
                    movementAllowance,
                    "Movement allowance must not be negative.");
            }

            if (movementAllowance == 0)
            {
                return Array.Empty<ReachableMovementDestination>();
            }

            var mover = RuleObject.ForHero(selectedHero.HeroId);
            var playable = new HashSet<HexCoordinate>(board.PlayableCoordinateSet);
            return FindReachableCore(
                selectedHero,
                playable,
                coordinate => ObstacleQuery.BlocksTransit(board, coordinate, mover, modifications),
                coordinate => ObstacleQuery.BlocksEndOfMovement(board, coordinate),
                movementAllowance);
        }

        public static IReadOnlyList<ReachableMovementDestination> FindReachableDestinations(
            HeroOccupancy selectedHero,
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> occupancies,
            int movementAllowance)
        {
            return FindReachableDestinations(
                selectedHero,
                boardCoordinates,
                occupancies,
                Enumerable.Empty<ObstacleOccupancy>(),
                Enumerable.Empty<MinionOccupancy>(),
                movementAllowance);
        }

        public static IReadOnlyList<ReachableMovementDestination> FindReachableDestinations(
            HeroOccupancy selectedHero,
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> occupancies,
            IEnumerable<ObstacleOccupancy> obstacleOccupancies,
            int movementAllowance)
        {
            return FindReachableDestinations(
                selectedHero,
                boardCoordinates,
                occupancies,
                obstacleOccupancies,
                Enumerable.Empty<MinionOccupancy>(),
                movementAllowance);
        }

        public static IReadOnlyList<ReachableMovementDestination> FindReachableDestinations(
            HeroOccupancy selectedHero,
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> occupancies,
            IEnumerable<ObstacleOccupancy> obstacleOccupancies,
            IEnumerable<MinionOccupancy> minions,
            int movementAllowance)
        {
            if (boardCoordinates == null)
            {
                throw new ArgumentNullException(nameof(boardCoordinates));
            }

            if (occupancies == null)
            {
                throw new ArgumentNullException(nameof(occupancies));
            }

            if (obstacleOccupancies == null)
            {
                throw new ArgumentNullException(nameof(obstacleOccupancies));
            }

            if (minions == null)
            {
                throw new ArgumentNullException(nameof(minions));
            }

            if (movementAllowance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movementAllowance),
                    movementAllowance,
                    "Movement allowance must not be negative.");
            }

            if (movementAllowance == 0)
            {
                return Array.Empty<ReachableMovementDestination>();
            }

            var board = new HashSet<HexCoordinate>(boardCoordinates);
            var blocked = new HashSet<HexCoordinate>(occupancies.Select(occupancy => occupancy.Coordinate));
            foreach (var obstacleOccupancy in obstacleOccupancies)
            {
                blocked.Add(obstacleOccupancy.Coordinate);
            }
            foreach (var minion in minions)
            {
                blocked.Add(minion.Coordinate);
            }

            // These overloads take raw collections rather than the board authority, so no permission
            // can reach them; an occupied space blocks both transit and endpoint, as it always has.
            blocked.Remove(selectedHero.Coordinate);
            return FindReachableCore(
                selectedHero,
                board,
                blocked.Contains,
                coordinate => !board.Contains(coordinate) || blocked.Contains(coordinate),
                movementAllowance);
        }

        private static IReadOnlyList<ReachableMovementDestination> FindReachableCore(
            HeroOccupancy selectedHero,
            HashSet<HexCoordinate> board,
            Func<HexCoordinate, bool> blocksTransit,
            Func<HexCoordinate, bool> blocksEndpoint,
            int movementAllowance)
        {
            if (!board.Contains(selectedHero.Coordinate))
            {
                return Array.Empty<ReachableMovementDestination>();
            }

            var costs = new Dictionary<HexCoordinate, int>
            {
                [selectedHero.Coordinate] = 0
            };
            var parents = new Dictionary<HexCoordinate, HexCoordinate>();
            var queue = new Queue<HexCoordinate>();
            var destinations = new List<ReachableMovementDestination>();

            queue.Enqueue(selectedHero.Coordinate);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentCost = costs[current];
                if (currentCost >= movementAllowance)
                {
                    continue;
                }

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (!board.Contains(neighbor) ||
                        blocksTransit(neighbor) ||
                        costs.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    // Entering a space costs the same whether or not something occupies it, so a
                    // permitted pass-through changes which routes exist rather than what they cost.
                    var cost = currentCost + 1;
                    costs[neighbor] = cost;
                    parents[neighbor] = current;
                    queue.Enqueue(neighbor);

                    // Traversable and legal-to-stop-on are different questions. A space the mover may
                    // only pass through is walked but never offered, which is p.18's rule that no unit
                    // may end its movement on an obstacle.
                    if (blocksEndpoint(neighbor))
                    {
                        continue;
                    }

                    destinations.Add(new ReachableMovementDestination(
                        neighbor,
                        BuildRoute(selectedHero.Coordinate, neighbor, parents),
                        cost));
                }
            }

            return destinations.AsReadOnly();
        }

        private static IReadOnlyList<HexCoordinate> BuildRoute(
            HexCoordinate origin,
            HexCoordinate destination,
            IReadOnlyDictionary<HexCoordinate, HexCoordinate> parents)
        {
            var route = new List<HexCoordinate> { destination };
            var current = destination;
            while (!current.Equals(origin))
            {
                current = parents[current];
                route.Add(current);
            }

            route.Reverse();
            return route;
        }
    }
}
