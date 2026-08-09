using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Movement
{
    public sealed class MovementSessionState
    {
        public MovementSessionState(
            HeroId heroId,
            int movementAllowance,
            int remainingAllowance,
            bool isComplete,
            IEnumerable<HexCoordinate> route,
            IEnumerable<AdjacentMoveEvent> stepEvents)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("Movement session state requires a defined hero ID.", nameof(heroId));
            }

            if (movementAllowance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(movementAllowance));
            }

            if (remainingAllowance < 0 || remainingAllowance > movementAllowance)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingAllowance));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (stepEvents == null)
            {
                throw new ArgumentNullException(nameof(stepEvents));
            }

            var materializedRoute = route.ToArray();
            var materializedStepEvents = stepEvents.ToArray();
            if (materializedRoute.Length == 0)
            {
                throw new ArgumentException("Movement session state requires an origin in its route.", nameof(route));
            }

            if (materializedStepEvents.Length != materializedRoute.Length - 1 ||
                movementAllowance - remainingAllowance != materializedStepEvents.Length)
            {
                throw new ArgumentException("Movement session route, events, and allowances must describe the same committed steps.");
            }

            for (var index = 0; index < materializedStepEvents.Length; index++)
            {
                var movementEvent = materializedStepEvents[index];
                if (!movementEvent.HeroId.Equals(heroId) ||
                    !movementEvent.Origin.Equals(materializedRoute[index]) ||
                    !movementEvent.Destination.Equals(materializedRoute[index + 1]) ||
                    !materializedRoute[index].GetNeighbors().Contains(materializedRoute[index + 1]))
                {
                    throw new ArgumentException("Movement session events must form the adjacent route for the moving hero.", nameof(stepEvents));
                }
            }

            HeroId = heroId;
            MovementAllowance = movementAllowance;
            RemainingAllowance = remainingAllowance;
            IsComplete = isComplete;
            Route = Array.AsReadOnly(materializedRoute);
            StepEvents = Array.AsReadOnly(materializedStepEvents);
        }

        public HeroId HeroId { get; }

        public int MovementAllowance { get; }

        public int RemainingAllowance { get; }

        public bool IsComplete { get; }

        public IReadOnlyList<HexCoordinate> Route { get; }

        public IReadOnlyList<AdjacentMoveEvent> StepEvents { get; }

        public int CommittedStepCount => StepEvents.Count;
    }
}
