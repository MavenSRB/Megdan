using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public readonly struct AdjacentMoveEvent
    {
        public AdjacentMoveEvent(HeroId heroId, HexCoordinate origin, HexCoordinate destination)
        {
            HeroId = heroId;
            Origin = origin;
            Destination = destination;
        }

        public HeroId HeroId { get; }

        public HexCoordinate Origin { get; }

        public HexCoordinate Destination { get; }
    }
}
