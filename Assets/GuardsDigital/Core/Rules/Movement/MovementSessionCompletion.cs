using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Movement
{
    public readonly struct MovementSessionCompletion
    {
        public MovementSessionCompletion(
            HeroId heroId,
            IEnumerable<HexCoordinate> route,
            IEnumerable<AdjacentMoveEvent> stepEvents,
            int movementAllowance,
            int remainingAllowance)
        {
            HeroId = heroId;
            Route = route.ToArray();
            StepEvents = stepEvents.ToArray();
            MovementAllowance = movementAllowance;
            RemainingAllowance = remainingAllowance;
        }

        public HeroId HeroId { get; }

        public IReadOnlyList<HexCoordinate> Route { get; }

        public IReadOnlyList<AdjacentMoveEvent> StepEvents { get; }

        public int MovementAllowance { get; }

        public int RemainingAllowance { get; }
    }
}
