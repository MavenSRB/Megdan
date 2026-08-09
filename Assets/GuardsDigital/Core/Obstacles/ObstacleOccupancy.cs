using System;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Obstacles
{
    public readonly struct ObstacleOccupancy : IEquatable<ObstacleOccupancy>
    {
        public ObstacleOccupancy(ObstacleId obstacleId, HexCoordinate coordinate)
        {
            if (!obstacleId.IsDefined)
            {
                throw new ArgumentException("Obstacle occupancy requires a defined obstacle ID.", nameof(obstacleId));
            }

            ObstacleId = obstacleId;
            Coordinate = coordinate;
        }

        public ObstacleId ObstacleId { get; }

        public HexCoordinate Coordinate { get; }

        public bool Equals(ObstacleOccupancy other)
        {
            return ObstacleId.Equals(other.ObstacleId) && Coordinate.Equals(other.Coordinate);
        }

        public override bool Equals(object obj)
        {
            return obj is ObstacleOccupancy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ObstacleId.GetHashCode() * 397) ^ Coordinate.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{ObstacleId} at {Coordinate}";
        }
    }
}
