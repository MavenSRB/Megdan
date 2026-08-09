using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Movement;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public sealed class CardActionResolutionResult
    {
        private CardActionResolutionResult(
            bool accepted,
            bool isComplete,
            CardActionResolutionRejectionReason rejectionReason,
            MovementSessionStepRejectionReason movementStepRejectionReason,
            AdjacentMoveRejectionReason adjacentMoveRejectionReason,
            PendingCardActionResolution pendingResolution,
            PendingMovementChoice pendingChoice,
            IEnumerable<CardActionResolutionEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            Accepted = accepted;
            IsComplete = isComplete;
            RejectionReason = rejectionReason;
            MovementStepRejectionReason = movementStepRejectionReason;
            AdjacentMoveRejectionReason = adjacentMoveRejectionReason;
            PendingResolution = pendingResolution;
            PendingChoice = pendingChoice;
            Events = events.ToArray();
        }

        public bool Accepted { get; }

        public bool IsComplete { get; }

        public CardActionResolutionRejectionReason RejectionReason { get; }

        public MovementSessionStepRejectionReason MovementStepRejectionReason { get; }

        public AdjacentMoveRejectionReason AdjacentMoveRejectionReason { get; }

        public PendingCardActionResolution PendingResolution { get; }

        public PendingMovementChoice PendingChoice { get; }

        public IReadOnlyList<CardActionResolutionEvent> Events { get; }

        public static CardActionResolutionResult Accept(
            bool isComplete,
            PendingCardActionResolution pendingResolution,
            PendingMovementChoice pendingChoice,
            IEnumerable<CardActionResolutionEvent> events)
        {
            return new CardActionResolutionResult(
                true,
                isComplete,
                CardActionResolutionRejectionReason.None,
                MovementSessionStepRejectionReason.None,
                AdjacentMoveRejectionReason.None,
                pendingResolution,
                pendingChoice,
                events);
        }

        public static CardActionResolutionResult Reject(
            CardActionResolutionRejectionReason rejectionReason,
            PendingCardActionResolution pendingResolution,
            PendingMovementChoice pendingChoice)
        {
            if (rejectionReason == CardActionResolutionRejectionReason.None)
            {
                throw new ArgumentException("A rejected card action resolution requires a rejection reason.", nameof(rejectionReason));
            }

            return new CardActionResolutionResult(
                false,
                false,
                rejectionReason,
                MovementSessionStepRejectionReason.None,
                AdjacentMoveRejectionReason.None,
                pendingResolution,
                pendingChoice,
                Array.Empty<CardActionResolutionEvent>());
        }

        public static CardActionResolutionResult RejectMovementStep(
            MovementSessionStepResult stepResult,
            PendingCardActionResolution pendingResolution,
            PendingMovementChoice pendingChoice)
        {
            return new CardActionResolutionResult(
                false,
                false,
                CardActionResolutionRejectionReason.MovementStepRejected,
                stepResult.RejectionReason,
                stepResult.AdjacentMoveRejectionReason,
                pendingResolution,
                pendingChoice,
                Array.Empty<CardActionResolutionEvent>());
        }
    }
}
