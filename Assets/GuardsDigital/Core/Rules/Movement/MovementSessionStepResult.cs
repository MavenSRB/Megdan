using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.AdjacentMove;

namespace GuardsDigital.Core.Rules.Movement
{
    public readonly struct MovementSessionStepResult
    {
        private MovementSessionStepResult(
            bool accepted,
            bool sessionComplete,
            int remainingAllowance,
            MovementSessionStepRejectionReason rejectionReason,
            AdjacentMoveRejectionReason adjacentMoveRejectionReason,
            IReadOnlyList<AdjacentMoveEvent> newEvents,
            IReadOnlyList<AdjacentMoveEvent> committedEvents)
        {
            Accepted = accepted;
            SessionComplete = sessionComplete;
            RemainingAllowance = remainingAllowance;
            RejectionReason = rejectionReason;
            AdjacentMoveRejectionReason = adjacentMoveRejectionReason;
            NewEvents = newEvents;
            CommittedEvents = committedEvents;
        }

        public bool Accepted { get; }

        public bool SessionComplete { get; }

        public int RemainingAllowance { get; }

        public MovementSessionStepRejectionReason RejectionReason { get; }

        public AdjacentMoveRejectionReason AdjacentMoveRejectionReason { get; }

        public IReadOnlyList<AdjacentMoveEvent> NewEvents { get; }

        public IReadOnlyList<AdjacentMoveEvent> CommittedEvents { get; }

        public static MovementSessionStepResult Accept(
            AdjacentMoveEvent movementEvent,
            int remainingAllowance,
            bool sessionComplete,
            IEnumerable<AdjacentMoveEvent> committedEvents)
        {
            return Accept(
                new[] { movementEvent }, remainingAllowance, sessionComplete, committedEvents);
        }

        /// <summary>
        /// A whole route's spaces, added by milestone 6.8c. <see cref="NewEvents"/> carries one event
        /// per space entered rather than one per accepted call, so a caller that narrates per space
        /// reads them all from a single commit.
        /// </summary>
        public static MovementSessionStepResult Accept(
            IReadOnlyList<AdjacentMoveEvent> movementEvents,
            int remainingAllowance,
            bool sessionComplete,
            IEnumerable<AdjacentMoveEvent> committedEvents)
        {
            if (movementEvents == null)
            {
                throw new ArgumentNullException(nameof(movementEvents));
            }

            if (movementEvents.Count == 0)
            {
                throw new ArgumentException(
                    "An accepted movement entered at least one space.", nameof(movementEvents));
            }

            return new MovementSessionStepResult(
                true,
                sessionComplete,
                remainingAllowance,
                MovementSessionStepRejectionReason.None,
                AdjacentMoveRejectionReason.None,
                movementEvents,
                committedEvents.ToArray());
        }

        public static MovementSessionStepResult Reject(
            MovementSessionStepRejectionReason reason,
            AdjacentMoveRejectionReason adjacentMoveRejectionReason,
            int remainingAllowance,
            IEnumerable<AdjacentMoveEvent> committedEvents)
        {
            if (reason == MovementSessionStepRejectionReason.None)
            {
                throw new ArgumentException("A rejected movement session step requires a rejection reason.", nameof(reason));
            }

            return new MovementSessionStepResult(
                false,
                reason == MovementSessionStepRejectionReason.NoRemainingAllowance,
                remainingAllowance,
                reason,
                adjacentMoveRejectionReason,
                Array.Empty<AdjacentMoveEvent>(),
                committedEvents.ToArray());
        }
    }
}
