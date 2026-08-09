using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public sealed class AdjacentMoveAuthority
    {
        private readonly HashSet<HexCoordinate> boardCoordinates;
        private readonly List<HeroOccupancy> occupancies;

        public AdjacentMoveAuthority(
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> initialOccupancies)
        {
            if (boardCoordinates == null)
            {
                throw new ArgumentNullException(nameof(boardCoordinates));
            }

            if (initialOccupancies == null)
            {
                throw new ArgumentNullException(nameof(initialOccupancies));
            }

            this.boardCoordinates = new HashSet<HexCoordinate>(boardCoordinates);
            occupancies = new List<HeroOccupancy>();

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
        }

        public IReadOnlyList<HeroOccupancy> Occupancies => occupancies.ToArray();

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

            if (occupancies.Any(occupancy => occupancy.Coordinate.Equals(command.Destination)))
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
