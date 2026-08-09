using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Movement
{
    public sealed class MovementSession
    {
        private readonly AdjacentMoveAuthority adjacentMoveAuthority;
        private readonly HeroId heroId;
        private readonly List<HexCoordinate> route;
        private readonly List<AdjacentMoveEvent> stepEvents = new List<AdjacentMoveEvent>();

        public MovementSession(
            AdjacentMoveAuthority adjacentMoveAuthority,
            HeroId heroId,
            int movementAllowance)
        {
            if (adjacentMoveAuthority == null)
            {
                throw new ArgumentNullException(nameof(adjacentMoveAuthority));
            }

            if (!heroId.IsDefined)
            {
                throw new ArgumentException("Movement session requires a defined hero ID.", nameof(heroId));
            }

            if (movementAllowance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movementAllowance),
                    movementAllowance,
                    "Movement allowance must not be negative.");
            }

            this.adjacentMoveAuthority = adjacentMoveAuthority;
            this.heroId = heroId;
            MovementAllowance = movementAllowance;
            RemainingAllowance = movementAllowance;
            route = new List<HexCoordinate> { CurrentOccupancy.Coordinate };
        }

        public int MovementAllowance { get; }

        public int RemainingAllowance { get; private set; }

        public bool IsComplete { get; private set; }

        public IReadOnlyList<HexCoordinate> Route => route.ToArray();

        public IReadOnlyList<AdjacentMoveEvent> StepEvents => stepEvents.ToArray();

        public HeroOccupancy CurrentOccupancy =>
            adjacentMoveAuthority.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId));

        public MovementSessionStepResult TryCommitStep(HexCoordinate destination)
        {
            if (IsComplete || RemainingAllowance == 0)
            {
                IsComplete = true;
                return MovementSessionStepResult.Reject(
                    MovementSessionStepRejectionReason.NoRemainingAllowance,
                    AdjacentMoveRejectionReason.None,
                    RemainingAllowance,
                    stepEvents);
            }

            var currentOccupancy = CurrentOccupancy;
            var adjacentResult = adjacentMoveAuthority.TryCommit(new AdjacentMoveCommand(
                currentOccupancy.HeroId,
                currentOccupancy.Coordinate,
                destination));

            if (!adjacentResult.Accepted)
            {
                return MovementSessionStepResult.Reject(
                    MovementSessionStepRejectionReason.AdjacentMoveRejected,
                    adjacentResult.RejectionReason,
                    RemainingAllowance,
                    stepEvents);
            }

            RemainingAllowance--;
            var movementEvent = adjacentResult.Events.Single();
            stepEvents.Add(movementEvent);
            route.Add(movementEvent.Destination);

            if (RemainingAllowance == 0)
            {
                IsComplete = true;
            }

            return MovementSessionStepResult.Accept(
                movementEvent,
                RemainingAllowance,
                IsComplete,
                stepEvents);
        }

        public MovementSessionCompletion Complete()
        {
            IsComplete = true;
            return new MovementSessionCompletion(
                heroId,
                route,
                stepEvents,
                MovementAllowance,
                RemainingAllowance);
        }
    }
}
