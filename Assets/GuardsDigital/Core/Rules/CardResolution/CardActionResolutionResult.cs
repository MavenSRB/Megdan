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
            IEnumerable<ResolutionStep> steps)
        {
            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            Accepted = accepted;
            IsComplete = isComplete;
            RejectionReason = rejectionReason;
            MovementStepRejectionReason = movementStepRejectionReason;
            AdjacentMoveRejectionReason = adjacentMoveRejectionReason;
            PendingResolution = pendingResolution;
            PendingChoice = pendingChoice;
            Steps = steps.ToArray();
            Events = Steps
                .Where(step => step.Kind == ResolutionStepKind.Fact)
                .Select(step => step.Fact)
                .ToArray();
        }

        public bool Accepted { get; }

        public bool IsComplete { get; }

        public CardActionResolutionRejectionReason RejectionReason { get; }

        public MovementSessionStepRejectionReason MovementStepRejectionReason { get; }

        public AdjacentMoveRejectionReason AdjacentMoveRejectionReason { get; }

        public PendingCardActionResolution PendingResolution { get; }

        public PendingMovementChoice PendingChoice { get; }

        /// <summary>
        /// The ordered stream the resolver emitted, facts and timing checkpoints interleaved at the
        /// positions the resolver raised them (milestone 6.1ap).
        /// </summary>
        public IReadOnlyList<ResolutionStep> Steps { get; }

        /// <summary>The fact subsequence of <see cref="Steps"/>, for callers that want facts alone.</summary>
        public IReadOnlyList<CardActionResolutionEvent> Events { get; }

        public static CardActionResolutionResult Accept(
            bool isComplete,
            PendingCardActionResolution pendingResolution,
            PendingMovementChoice pendingChoice,
            IEnumerable<ResolutionStep> steps)
        {
            return new CardActionResolutionResult(
                true,
                isComplete,
                CardActionResolutionRejectionReason.None,
                MovementSessionStepRejectionReason.None,
                AdjacentMoveRejectionReason.None,
                pendingResolution,
                pendingChoice,
                steps);
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
                Array.Empty<ResolutionStep>());
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
                Array.Empty<ResolutionStep>());
        }
    }
}
