using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
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

        /// <summary>One space, which is the one-element case of <see cref="TryCommitRoute"/>.</summary>
        public MovementSessionStepResult TryCommitStep(HexCoordinate destination) =>
            TryCommitRoute(new[] { destination }, null, out _);

        public MovementSessionStepResult TryCommitRoute(
            IReadOnlyList<HexCoordinate> routeSpaces,
            RuleModificationSet modifications) =>
            TryCommitRoute(routeSpaces, modifications, out _);

        /// <summary>
        /// Commits every space of a chosen route in one transaction.
        ///
        /// Milestone 6.8c replaced the per-space walk this method used to be. A unit holding rulebook
        /// p.18's Move-Through permission passes through an occupied space without ever occupying it,
        /// and a walk that commits each space separately has to occupy each one in turn, so the route
        /// had to become the unit of commitment for the permission to be expressible at all. The
        /// allowance is spent per space entered either way, and a route that crosses nothing occupied
        /// produces exactly the events and state the walk did.
        /// </summary>
        /// <param name="committedStepStates">
        /// The session state after each space, in order, so a caller that narrates one fact per space
        /// keeps the exact per-space identities the walk produced. Empty on refusal.
        /// </param>
        public MovementSessionStepResult TryCommitRoute(
            IReadOnlyList<HexCoordinate> routeSpaces,
            RuleModificationSet modifications,
            out IReadOnlyList<MovementSessionState> committedStepStates)
        {
            committedStepStates = Array.Empty<MovementSessionState>();
            if (routeSpaces == null)
            {
                throw new ArgumentNullException(nameof(routeSpaces));
            }

            if (IsComplete || RemainingAllowance < routeSpaces.Count || routeSpaces.Count == 0)
            {
                if (IsComplete || RemainingAllowance == 0)
                {
                    IsComplete = true;
                }

                return MovementSessionStepResult.Reject(
                    MovementSessionStepRejectionReason.NoRemainingAllowance,
                    AdjacentMoveRejectionReason.None,
                    RemainingAllowance,
                    stepEvents);
            }

            var routeResult = adjacentMoveAuthority.TryCommitRoute(
                CurrentOccupancy.HeroId, routeSpaces, modifications);
            if (!routeResult.Accepted)
            {
                return MovementSessionStepResult.Reject(
                    MovementSessionStepRejectionReason.AdjacentMoveRejected,
                    routeResult.RejectionReason,
                    RemainingAllowance,
                    stepEvents);
            }

            var stepStates = new List<MovementSessionState>(routeResult.Events.Count);
            foreach (var movementEvent in routeResult.Events)
            {
                RemainingAllowance--;
                stepEvents.Add(movementEvent);
                route.Add(movementEvent.Destination);
                stepStates.Add(CaptureState());
            }

            committedStepStates = stepStates;
            if (RemainingAllowance == 0)
            {
                IsComplete = true;
            }

            return MovementSessionStepResult.Accept(
                routeResult.Events,
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
