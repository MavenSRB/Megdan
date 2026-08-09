using System;
using System.Collections.Generic;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Board
{
    public static class AdjacentDestinationQuery
    {
        public static IReadOnlyList<HexCoordinate> FindEmptyAdjacentDestinations(
            HeroOccupancy selectedHero,
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> occupancies)
        {
            if (boardCoordinates == null)
            {
                throw new ArgumentNullException(nameof(boardCoordinates));
            }

            if (occupancies == null)
            {
                throw new ArgumentNullException(nameof(occupancies));
            }

            var board = new HashSet<HexCoordinate>(boardCoordinates);
            if (!board.Contains(selectedHero.Coordinate))
            {
                return Array.Empty<HexCoordinate>();
            }

            var occupied = new HashSet<HexCoordinate>();
            foreach (var occupancy in occupancies)
            {
                occupied.Add(occupancy.Coordinate);
            }

            var destinations = new List<HexCoordinate>();
            foreach (var neighbor in selectedHero.Coordinate.GetNeighbors())
            {
                if (board.Contains(neighbor) && !occupied.Contains(neighbor))
                {
                    destinations.Add(neighbor);
                }
            }

            return destinations;
        }
    }
}
