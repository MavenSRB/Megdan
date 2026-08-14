using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.Consequences;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public enum DefenseBlockResult
    {
        None = 0,
        Blocked = 1
    }

    public sealed class AttackOutcomeComparison
    {
        public AttackOutcomeComparison(
            int attackTotal,
            int? defenseTotal,
            DefenseBlockResult blockResult,
            AttackResolutionOutcome outcome)
        {
            if (blockResult != DefenseBlockResult.None && blockResult != DefenseBlockResult.Blocked)
                throw new ArgumentOutOfRangeException(nameof(blockResult));
            var expected = blockResult == DefenseBlockResult.Blocked ||
                (defenseTotal.HasValue && defenseTotal.Value >= attackTotal)
                    ? AttackResolutionOutcome.SuccessfullyDefended
                    : AttackResolutionOutcome.AttackSuccessful;
            if (outcome != expected)
                throw new ArgumentException("Attack outcome must match the typed Block/numeric comparison.", nameof(outcome));

            AttackTotal = attackTotal;
            DefenseTotal = defenseTotal;
            BlockResult = blockResult;
            Outcome = outcome;
        }

        public int AttackTotal { get; }
        public int? DefenseTotal { get; }
        public DefenseBlockResult BlockResult { get; }
        public AttackResolutionOutcome Outcome { get; }

        public static AttackOutcomeComparison Create(
            int attackTotal,
            int? defenseTotal,
            DefenseBlockResult blockResult)
        {
            var outcome = blockResult == DefenseBlockResult.Blocked ||
                (defenseTotal.HasValue && defenseTotal.Value >= attackTotal)
                    ? AttackResolutionOutcome.SuccessfullyDefended
                    : AttackResolutionOutcome.AttackSuccessful;
            return new AttackOutcomeComparison(attackTotal, defenseTotal, blockResult, outcome);
        }
    }

    public sealed class HeroDefeatResolution
    {
        public HeroDefeatResolution(DefeatOutcome defeat)
        {
            Defeat = defeat ?? throw new ArgumentNullException(nameof(defeat));
            if (defeat.HasRewardAmounts)
                throw new ArgumentException("A staged Hero Defeat must defer Level/Tier reward derivation.", nameof(defeat));
        }

        public DefeatOutcome Defeat { get; }
    }

    public sealed class AttackActionResolutionResult
    {
        private AttackActionResolutionResult(
            bool accepted,
            CardActionResolutionRejectionReason rejectionReason,
            PendingAttackResolution pendingResolution,
            PendingAttackTargetChoice pendingTargetChoice,
            PendingDefenseResponseChoice pendingDefenseChoice,
            AttackResolutionOutcome outcome,
            IEnumerable<ResolutionStep> steps,
            MinionDefeatResolution minionDefeat,
            AttackOutcomeComparison outcomeComparison,
            HeroDefeatResolution heroDefeat)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            PendingResolution = pendingResolution;
            PendingTargetChoice = pendingTargetChoice;
            PendingDefenseChoice = pendingDefenseChoice;
            Outcome = outcome;
            MinionDefeat = minionDefeat;
            OutcomeComparison = outcomeComparison;
            HeroDefeat = heroDefeat;
            Steps = Array.AsReadOnly((steps ?? throw new ArgumentNullException(nameof(steps))).ToArray());
            Events = Array.AsReadOnly(Steps
                .Where(step => step.Kind == ResolutionStepKind.Fact)
                .Select(step => step.Fact)
                .ToArray());

            if (minionDefeat != null && heroDefeat != null)
                throw new ArgumentException("An Attack cannot record both a minion and hero Defeat.");
            if (outcomeComparison != null && outcomeComparison.Outcome != outcome)
                throw new ArgumentException("The typed comparison must match the reported Attack outcome.");
            if (outcome == AttackResolutionOutcome.SuccessfullyDefended &&
                (outcomeComparison == null || heroDefeat != null))
                throw new ArgumentException("A defended Hero Attack requires its comparison and no Defeat.");
            if (outcome == AttackResolutionOutcome.AttackSuccessful && minionDefeat == null &&
                (outcomeComparison == null || heroDefeat == null))
                throw new ArgumentException("A successful Hero Attack requires its comparison and staged Defeat.");
        }

        public bool Accepted { get; }
        public CardActionResolutionRejectionReason RejectionReason { get; }
        public PendingAttackResolution PendingResolution { get; }
        public PendingAttackTargetChoice PendingTargetChoice { get; }
        public PendingDefenseResponseChoice PendingDefenseChoice { get; }
        public AttackResolutionOutcome Outcome { get; }
        public MinionDefeatResolution MinionDefeat { get; }
        public AttackOutcomeComparison OutcomeComparison { get; }
        public HeroDefeatResolution HeroDefeat { get; }
        /// <summary>
        /// The ordered stream the resolver emitted, facts and timing checkpoints interleaved at the
        /// positions the resolver raised them (milestone 6.1ap).
        /// </summary>
        public IReadOnlyList<ResolutionStep> Steps { get; }

        /// <summary>The fact subsequence of <see cref="Steps"/>, for callers that want facts alone.</summary>
        public IReadOnlyList<CardActionResolutionEvent> Events { get; }

        public static AttackActionResolutionResult Accept(
            PendingAttackResolution pendingResolution,
            PendingAttackTargetChoice pendingTargetChoice,
            PendingDefenseResponseChoice pendingDefenseChoice,
            IEnumerable<ResolutionStep> steps,
            AttackResolutionOutcome outcome = AttackResolutionOutcome.None,
            MinionDefeatResolution minionDefeat = null,
            AttackOutcomeComparison outcomeComparison = null,
            HeroDefeatResolution heroDefeat = null)
        {
            return new AttackActionResolutionResult(
                true,
                CardActionResolutionRejectionReason.None,
                pendingResolution,
                pendingTargetChoice,
                pendingDefenseChoice,
                outcome,
                steps,
                minionDefeat,
                outcomeComparison,
                heroDefeat);
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
                Array.Empty<ResolutionStep>(),
                null,
                null,
                null);
        }
    }
}
