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

        public MovementSession(
            AdjacentMoveAuthority adjacentMoveAuthority,
            MovementSessionState state)
        {
            this.adjacentMoveAuthority = adjacentMoveAuthority ?? throw new ArgumentNullException(nameof(adjacentMoveAuthority));
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var matchingOccupancies = adjacentMoveAuthority.Occupancies
                .Where(occupancy => occupancy.HeroId.Equals(state.HeroId))
                .ToArray();
            if (matchingOccupancies.Length != 1 ||
                !matchingOccupancies[0].Coordinate.Equals(state.Route[state.Route.Count - 1]))
            {
                throw new ArgumentException(
                    "Restored movement state must end at the moving hero's authoritative coordinate.",
                    nameof(state));
            }

            heroId = state.HeroId;
            MovementAllowance = state.MovementAllowance;
            RemainingAllowance = state.RemainingAllowance;
            IsComplete = state.IsComplete;
            route = new List<HexCoordinate>(state.Route);
            stepEvents.AddRange(state.StepEvents);
        }

        public int MovementAllowance { get; }

        public int RemainingAllowance { get; private set; }

        public bool IsComplete { get; private set; }

        public IReadOnlyList<HexCoordinate> Route => route.ToArray();

        public IReadOnlyList<AdjacentMoveEvent> StepEvents => stepEvents.ToArray();

        public MovementSessionState CaptureState()
        {
            return new MovementSessionState(
                heroId,
                MovementAllowance,
                RemainingAllowance,
                IsComplete,
                route,
                stepEvents);
        }

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

        /// <summary>Stops the session short of its allowance. The completed route, step events, and
        /// remaining allowance stay readable on the session itself, which is where every caller
        /// reads them from.</summary>
        public void Complete()
        {
            IsComplete = true;
        }
    }
}
