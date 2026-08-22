using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.Consequences;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// What rulebook p.14's Block did to an attack, or that no Block was involved at all.
    ///
    /// <para>Milestone 5.8 reserved the first two members and milestone 8.11 evaluates them. The third
    /// is 8.11's own, and it is a named outcome rather than a fall-through to <see cref="None"/> for
    /// the reason milestone 6.6b gave the prevented removal one: a Block whose condition was not
    /// fulfilled and a Defense that was never a Block are different states, and only the first of them
    /// legitimately reaches the comparison with no Defense Total at all. Collapsing them would make a
    /// missing total indistinguishable from a malformed one.</para>
    /// </summary>
    public enum DefenseBlockResult
    {
        /// <summary>p.14's ordinary numeric Defense, or no Defense action at all.</summary>
        None = 0,

        /// <summary>
        /// p.14: "If the Block condition is fulfilled, you successfully defend, regardless of the
        /// attack value."
        /// </summary>
        Blocked = 1,

        /// <summary>p.14, the same sentence's other half: "If not, you are defeated."</summary>
        ConditionUnfulfilled = 2
    }

    public sealed class AttackOutcomeComparison
    {
        public AttackOutcomeComparison(
            int attackTotal,
            int? defenseTotal,
            DefenseBlockResult blockResult,
            AttackResolutionOutcome outcome)
        {
            if (!Enum.IsDefined(typeof(DefenseBlockResult), blockResult))
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
