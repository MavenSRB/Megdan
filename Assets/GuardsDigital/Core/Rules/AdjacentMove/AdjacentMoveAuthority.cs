using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public sealed class AdjacentMoveAuthority
    {
        private readonly HashSet<HexCoordinate> boardCoordinates;
        private readonly List<HeroOccupancy> occupancies;
        private readonly List<ObstacleOccupancy> obstacleOccupancies;

        public AdjacentMoveAuthority(
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> initialOccupancies)
            : this(boardCoordinates, initialOccupancies, Enumerable.Empty<ObstacleOccupancy>())
        {
        }

        public AdjacentMoveAuthority(
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies)
        {
            if (boardCoordinates == null)
            {
                throw new ArgumentNullException(nameof(boardCoordinates));
            }

            if (initialOccupancies == null)
            {
                throw new ArgumentNullException(nameof(initialOccupancies));
            }

            if (initialObstacleOccupancies == null)
            {
                throw new ArgumentNullException(nameof(initialObstacleOccupancies));
            }

            this.boardCoordinates = new HashSet<HexCoordinate>(boardCoordinates);
            occupancies = new List<HeroOccupancy>();
            obstacleOccupancies = new List<ObstacleOccupancy>();

            foreach (var occupancy in initialOccupancies)
            {
                if (!this.boardCoordinates.Contains(occupancy.Coordinate))
                {
                    throw new ArgumentException("Initial occupancy must be on the board.", nameof(initialOccupancies));
                }

                if (occupancies.Any(existing => existing.HeroId.Equals(occupancy.HeroId)))
                {
                    throw new ArgumentException("Initial occupancies must have unique hero IDs.", nameof(initialOccupancies));
                }

                if (occupancies.Any(existing => existing.Coordinate.Equals(occupancy.Coordinate)))
                {
                    throw new ArgumentException("Initial occupancies must have unique coordinates.", nameof(initialOccupancies));
                }

                occupancies.Add(occupancy);
            }

            foreach (var obstacleOccupancy in initialObstacleOccupancies)
            {
                if (!this.boardCoordinates.Contains(obstacleOccupancy.Coordinate))
                {
                    throw new ArgumentException("Initial obstacle occupancy must be on the board.", nameof(initialObstacleOccupancies));
                }

                if (obstacleOccupancies.Any(existing => existing.ObstacleId.Equals(obstacleOccupancy.ObstacleId)))
                {
                    throw new ArgumentException("Initial obstacle occupancies must have unique obstacle IDs.", nameof(initialObstacleOccupancies));
                }

                if (obstacleOccupancies.Any(existing => existing.Coordinate.Equals(obstacleOccupancy.Coordinate)) ||
                    occupancies.Any(existing => existing.Coordinate.Equals(obstacleOccupancy.Coordinate)))
                {
                    throw new ArgumentException("Initial obstacle occupancies must not share coordinates with another obstacle or unit.", nameof(initialObstacleOccupancies));
                }

                obstacleOccupancies.Add(obstacleOccupancy);
            }
        }

        public IReadOnlyList<HeroOccupancy> Occupancies => occupancies.ToArray();

        public IReadOnlyList<ObstacleOccupancy> ObstacleOccupancies => obstacleOccupancies.ToArray();

        public AdjacentMoveResult TryCommit(AdjacentMoveCommand command)
        {
            var heroIndex = occupancies.FindIndex(occupancy => occupancy.HeroId.Equals(command.HeroId));
            if (heroIndex < 0)
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.UnknownHero);
            }

            var currentOccupancy = occupancies[heroIndex];
            if (!currentOccupancy.Coordinate.Equals(command.Origin))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.WrongOrigin);
            }

            if (!boardCoordinates.Contains(command.Destination))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.DestinationOffBoard);
            }

            if (!currentOccupancy.Coordinate.GetNeighbors().Contains(command.Destination))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.DestinationNotAdjacent);
            }

            if (occupancies.Any(occupancy => occupancy.Coordinate.Equals(command.Destination)) ||
                obstacleOccupancies.Any(occupancy => occupancy.Coordinate.Equals(command.Destination)))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.DestinationOccupied);
            }

            occupancies[heroIndex] = new HeroOccupancy(command.HeroId, command.Destination);
            return AdjacentMoveResult.Accept(new AdjacentMoveEvent(
                command.HeroId,
                command.Origin,
                command.Destination));
        }
    }
}
