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
            RuleModificationSet modifications) =>
            FindReachableDestinations(
                selectedHero.Coordinate,
                RuleObject.ForHero(selectedHero.HeroId),
                board,
                movementAllowance,
                modifications,
                moverIgnoresObstacles: false,
                // Milestone 8.35z30, RULING 47. This overload is the Movement action's own
                // entry point - it is asked with the hero Initiative selected and the allowance that
                // action composed - so it is the one ask that a permission scoped to movement actions
                // answers. Silverarrow's Trailblazer reaches its allies here and nowhere else.
                RuleActivity.Movement);

        /// <summary>
        /// The same reachability for any mover, named by where it stands and what it is.
        ///
        /// Milestone 6.4a widened the entry point rather than the algorithm: rulebook p.11 lets card
        /// text move "another unit or a token", and the walk below never cared which kind of object it
        /// was walking - only the transit predicate does, and that already takes a
        /// <see cref="RuleObject"/>. A token passes <see cref="RuleObject.None"/>, which is the honest
        /// answer rather than a missing one, because p.19 states tokens are not units and so no
        /// Move-Through permission is ever addressed to one.
        /// </summary>
        public static IReadOnlyList<ReachableMovementDestination> FindReachableDestinations(
            HexCoordinate origin,
            RuleObject mover,
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
            int movementAllowance,
            RuleModificationSet modifications) =>
            FindReachableDestinations(
                origin, mover, board, movementAllowance, modifications, moverIgnoresObstacles: false);

        /// <summary>
        /// The same walk for a movement whose own text crosses obstacles - milestone 8.23's Blink
        /// Strike, "Move 2 spaces in a straight line through an enemy unit".
        ///
        /// <para>The flag reaches the transit predicate and nothing else. It is not a permission
        /// granted to the mover: it belongs to this movement, so the hero's next Move is stopped by
        /// the same unit, and p.18's endpoint rule is untouched - a crossing movement still may not
        /// stop on an obstacle, which is why the space beyond has to be empty for the line to exist
        /// at all.</para>
        /// </summary>
        public static IReadOnlyList<ReachableMovementDestination> FindReachableDestinations(
            HexCoordinate origin,
            RuleObject mover,
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
            int movementAllowance,
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

            // The walkable universe is every space that exists on the map - playable and terrain
            // alike - because milestone 6.8e classifies terrain as an obstacle a permitted mover may
            // cross, not as off-board. The transit and endpoint predicates then decide per mover:
            // terrain blocks transit without the permission and is never an endpoint for anyone.
            var onMap = new HashSet<HexCoordinate>(board.PlayableCoordinateSet);
            onMap.UnionWith(board.TerrainCoordinateSet);
            return FindReachableCore(
                origin,
                onMap,
                coordinate => ObstacleQuery.BlocksTransit(
                    board, coordinate, mover, modifications, moverIgnoresObstacles,
                    permittedActivity),
                coordinate => ObstacleQuery.BlocksEndOfMovement(board, coordinate, modifications, mover),
                movementAllowance);
        }

        /// <summary>
        /// Reachability over a caller-supplied notion of a passable space, for milestone 6.4a's
        /// displaced-minion correction: p.18 returns a minion "by the shortest path of empty spaces",
        /// which is a stricter walk than movement's, since a correcting minion may not pass through
        /// anything at all. Sharing this core rather than writing a second traversal is what keeps the
        /// two from disagreeing about what a shortest route is.
        /// </summary>
        public static IReadOnlyList<ReachableMovementDestination> FindReachableThroughEmptySpaces(
            HexCoordinate origin,
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
            int maximumDistance)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (maximumDistance <= 0)
            {
                return Array.Empty<ReachableMovementDestination>();
            }

            return FindReachableCore(
                origin,
                new HashSet<HexCoordinate>(board.PlayableCoordinateSet),
                coordinate => !board.IsEmptyPlayableSpace(coordinate),
                coordinate => !board.IsEmptyPlayableSpace(coordinate),
                maximumDistance);
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
                selectedHero.Coordinate,
                board,
                blocked.Contains,
                coordinate => !board.Contains(coordinate) || blocked.Contains(coordinate),
                movementAllowance);
        }

        private static IReadOnlyList<ReachableMovementDestination> FindReachableCore(
            HexCoordinate origin,
            HashSet<HexCoordinate> board,
            Func<HexCoordinate, bool> blocksTransit,
            Func<HexCoordinate, bool> blocksEndpoint,
            int movementAllowance)
        {
            if (!board.Contains(origin))
            {
                return Array.Empty<ReachableMovementDestination>();
            }

            var costs = new Dictionary<HexCoordinate, int>
            {
                [origin] = 0
            };

            // Milestone 8.61e, MI-Q3. How many spaces the best route to each coordinate only PASSES
            // THROUGH - a space the mover may enter but may not stop on, which is p.18's obstacle
            // crossed under a permission, under a card's own "through", or because the piece standing
            // there says units may move through it. Min the Dragonmonk's mines are the third, and the
            // reason this number exists: several routes of one length can reach one space, and where
            // one of them walks over a mine and another does not, a one-click destination should take
            // the one that does not. Nothing about the rules forces that choice - p.10 leaves it to
            // the mover - so this is the default a mover who said nothing is taken to have meant, and
            // it costs nothing anywhere else because a board with no such space gives every route a
            // zero and leaves the first-found parent exactly where it was.
            var crossings = new Dictionary<HexCoordinate, int>
            {
                [origin] = 0
            };
            var parents = new Dictionary<HexCoordinate, HexCoordinate>();
            var queue = new Queue<HexCoordinate>();
            var offered = new List<KeyValuePair<HexCoordinate, int>>();

            queue.Enqueue(origin);
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
                    if (!board.Contains(neighbor) || blocksTransit(neighbor))
                    {
                        continue;
                    }

                    // Traversable and legal-to-stop-on are different questions. A space the mover may
                    // only pass through is walked but never offered, which is p.18's rule that no unit
                    // may end its movement on an obstacle.
                    var onlyPassedThrough = blocksEndpoint(neighbor);
                    var crossed = crossings[current] + (onlyPassedThrough ? 1 : 0);

                    // Entering a space costs the same whether or not something occupies it, so a
                    // permitted pass-through changes which routes exist rather than what they cost.
                    var cost = currentCost + 1;
                    if (costs.TryGetValue(neighbor, out var known))
                    {
                        // A second way in. It is only ever taken when it is the same length and walks
                        // over strictly fewer pieces, so the offered set, its costs and its order are
                        // all untouched - what changes is which of two equal routes is handed over.
                        // The queue is in cost order, so every way in at this length has been seen by
                        // the time this space is itself dequeued, and its count is final before
                        // anything is built from it.
                        if (known == cost && crossed < crossings[neighbor])
                        {
                            crossings[neighbor] = crossed;
                            parents[neighbor] = current;
                        }

                        continue;
                    }

                    costs[neighbor] = cost;
                    crossings[neighbor] = crossed;
                    parents[neighbor] = current;
                    queue.Enqueue(neighbor);
                    if (!onlyPassedThrough)
                    {
                        offered.Add(new KeyValuePair<HexCoordinate, int>(neighbor, cost));
                    }
                }
            }

            // Routes are built once the walk is over, because a parent may still be re-pointed while it
            // is running. The offered spaces, their order and their costs are the ones the walk found.
            var destinations = new List<ReachableMovementDestination>(offered.Count);
            foreach (var entry in offered)
            {
                destinations.Add(new ReachableMovementDestination(
                    entry.Key, BuildRoute(origin, entry.Key, parents), entry.Value));
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
