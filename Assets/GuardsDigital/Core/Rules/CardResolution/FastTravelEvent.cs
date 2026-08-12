using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public readonly struct FastTravelEvent : IEquatable<FastTravelEvent>
    {
        public FastTravelEvent(
            HeroId heroId,
            HexCoordinate origin,
            HexCoordinate destination,
            ZoneId originZoneId,
            ZoneId destinationZoneId)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("Fast Travel requires a defined hero ID.", nameof(heroId));
            }

            HeroId = heroId;
            Origin = origin;
            Destination = destination;
            OriginZoneId = originZoneId;
            DestinationZoneId = destinationZoneId;
        }

        public HeroId HeroId { get; }
        public HexCoordinate Origin { get; }
        public HexCoordinate Destination { get; }
        public ZoneId OriginZoneId { get; }
        public ZoneId DestinationZoneId { get; }

        public bool Equals(FastTravelEvent other) =>
            HeroId.Equals(other.HeroId) &&
            Origin.Equals(other.Origin) &&
            Destination.Equals(other.Destination) &&
            OriginZoneId.Equals(other.OriginZoneId) &&
            DestinationZoneId.Equals(other.DestinationZoneId);

        public override bool Equals(object obj) => obj is FastTravelEvent other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HeroId.GetHashCode();
                hash = (hash * 397) ^ Origin.GetHashCode();
                hash = (hash * 397) ^ Destination.GetHashCode();
                hash = (hash * 397) ^ OriginZoneId.GetHashCode();
                return (hash * 397) ^ DestinationZoneId.GetHashCode();
            }
        }
    }
}
