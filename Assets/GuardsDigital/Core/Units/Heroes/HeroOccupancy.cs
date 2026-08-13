using System;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Units.Heroes
{
    public readonly struct HeroOccupancy : IEquatable<HeroOccupancy>
    {
        public HeroOccupancy(HeroId heroId, HexCoordinate coordinate)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("Hero occupancy requires a defined hero ID.", nameof(heroId));
            }

            HeroId = heroId;
            Coordinate = coordinate;
        }

        public HeroId HeroId { get; }

        public HexCoordinate Coordinate { get; }

        public bool Equals(HeroOccupancy other)
        {
            return HeroId.Equals(other.HeroId) && Coordinate.Equals(other.Coordinate);
        }

        public override bool Equals(object obj)
        {
            return obj is HeroOccupancy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (HeroId.GetHashCode() * 397) ^ Coordinate.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{HeroId} at {Coordinate}";
        }
    }
}
