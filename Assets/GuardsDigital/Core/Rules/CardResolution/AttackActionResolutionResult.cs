using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public sealed class AttackActionResolutionResult
    {
        private AttackActionResolutionResult(
            bool accepted,
            CardActionResolutionRejectionReason rejectionReason,
            PendingAttackResolution pendingResolution,
            PendingAttackTargetChoice pendingTargetChoice,
            PendingDefenseResponseChoice pendingDefenseChoice,
            AttackResolutionOutcome outcome,
            IEnumerable<CardActionResolutionEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            PendingResolution = pendingResolution;
            PendingTargetChoice = pendingTargetChoice;
            PendingDefenseChoice = pendingDefenseChoice;
            Outcome = outcome;
            Events = Array.AsReadOnly((events ?? throw new ArgumentNullException(nameof(events))).ToArray());
        }

        public bool Accepted { get; }
        public CardActionResolutionRejectionReason RejectionReason { get; }
        public PendingAttackResolution PendingResolution { get; }
        public PendingAttackTargetChoice PendingTargetChoice { get; }
        public PendingDefenseResponseChoice PendingDefenseChoice { get; }
        public AttackResolutionOutcome Outcome { get; }
        public IReadOnlyList<CardActionResolutionEvent> Events { get; }

        public static AttackActionResolutionResult Accept(
            PendingAttackResolution pendingResolution,
            PendingAttackTargetChoice pendingTargetChoice,
            PendingDefenseResponseChoice pendingDefenseChoice,
            IEnumerable<CardActionResolutionEvent> events,
            AttackResolutionOutcome outcome = AttackResolutionOutcome.None)
        {
            return new AttackActionResolutionResult(
                true,
                CardActionResolutionRejectionReason.None,
                pendingResolution,
                pendingTargetChoice,
                pendingDefenseChoice,
                outcome,
                events);
        }

        public static AttackActionResolutionResult Reject(
            CardActionResolutionRejectionReason rejectionReason,
            PendingAttackResolution pendingResolution,
            PendingAttackTargetChoice pendingTargetChoice,
            PendingDefenseResponseChoice pendingDefenseChoice)
        {
            if (rejectionReason == CardActionResolutionRejectionReason.None)
            {
                throw new ArgumentException("A rejected Attack resolution requires a rejection reason.", nameof(rejectionReason));
            }

            return new AttackActionResolutionResult(
                false,
                rejectionReason,
                pendingResolution,
                pendingTargetChoice,
                pendingDefenseChoice,
                AttackResolutionOutcome.None,
                Array.Empty<CardActionResolutionEvent>());
        }
    }
}
