using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Movement
{
    public static class MovementReachabilityQuery
    {
        // Authority-backed entry: the board's occupancy index supplies the blocked set directly,
        // so every occupant kind blocks transit without this query enumerating per-kind lists.
        public static IReadOnlyList<ReachableMovementDestination> FindReachableDestinations(
            HeroOccupancy selectedHero,
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
            int movementAllowance)
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

            var playable = new HashSet<HexCoordinate>(board.PlayableCoordinateSet);
            var blocked = new HashSet<HexCoordinate>(board.OccupiedCoordinates);
            return FindReachableCore(selectedHero, playable, blocked, movementAllowance);
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
                Enumerable.Empty<QuickGameMinionSetupState>(),
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
                Enumerable.Empty<QuickGameMinionSetupState>(),
                movementAllowance);
        }

        public static IReadOnlyList<ReachableMovementDestination> FindReachableDestinations(
            HeroOccupancy selectedHero,
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> occupancies,
            IEnumerable<ObstacleOccupancy> obstacleOccupancies,
            IEnumerable<QuickGameMinionSetupState> minions,
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

            return FindReachableCore(selectedHero, board, blocked, movementAllowance);
        }

        private static IReadOnlyList<ReachableMovementDestination> FindReachableCore(
            HeroOccupancy selectedHero,
            HashSet<HexCoordinate> board,
            HashSet<HexCoordinate> blocked,
            int movementAllowance)
        {
            if (!board.Contains(selectedHero.Coordinate))
            {
                return Array.Empty<ReachableMovementDestination>();
            }

            blocked.Remove(selectedHero.Coordinate);

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
                        blocked.Contains(neighbor) ||
                        costs.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    var cost = currentCost + 1;
                    costs[neighbor] = cost;
                    parents[neighbor] = current;
                    queue.Enqueue(neighbor);
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
