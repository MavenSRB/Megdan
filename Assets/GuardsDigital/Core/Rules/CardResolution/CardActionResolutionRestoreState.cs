using System;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Which kind of continuation, if any, a restored card-action authority carries.
    /// </summary>
    /// <remarks>
    /// These values match the discriminator the snapshot writer emits for a card action.
    /// </remarks>
    public enum CardActionContinuationKind
    {
        None = 0,
        Movement = 1,
        Attack = 2,
        Skill = 3
    }

    /// <summary>
    /// The complete persisted state of a <see cref="CardActionResolutionAuthority"/>.
    /// </summary>
    /// <remarks>
    /// This replaces five separate restore entry points - two <c>Restore</c> overloads, two
    /// <c>RestoreAttack</c> overloads, and <c>RestoreWithoutPendingAction</c> - four of
    /// which defaulted <c>defeatRewards</c> to <c>null</c> and so silently discarded every
    /// hero's coin balance and awarded-Defeat record when a caller forgot it.
    ///
    /// The continuation kind is explicit rather than inferred from which field happens to
    /// be non-null, so a restore that carries no pending action is a stated case rather
    /// than an absence.
    /// </remarks>
    public sealed class CardActionResolutionRestoreState
    {
        private CardActionResolutionRestoreState(
            CardActionContinuationKind continuationKind,
            PendingCardActionResolution pendingMovementResolution,
            PendingAttackResolution pendingAttackResolution,
            PendingResolution pendingSkillResolution,
            DefeatRewardRestoreStateHolder defeatRewards)
        {
            ContinuationKind = continuationKind;
            PendingSkillResolution = pendingSkillResolution;
            PendingMovementResolution = pendingMovementResolution;
            PendingAttackResolution = pendingAttackResolution;
            DefeatRewards = defeatRewards;
        }

        public CardActionContinuationKind ContinuationKind { get; }

        /// <summary>Set only when <see cref="ContinuationKind"/> is Movement.</summary>
        public PendingCardActionResolution PendingMovementResolution { get; }

        /// <summary>Set only when <see cref="ContinuationKind"/> is Attack.</summary>
        public PendingAttackResolution PendingAttackResolution { get; }

        /// <summary>Set only when <see cref="ContinuationKind"/> is Skill.</summary>
        public PendingResolution PendingSkillResolution { get; }

        /// <summary>
        /// The Defeat-reward accounting to restore. Never omitted: an authority with no
        /// recorded rewards carries an explicitly absent holder rather than a missing
        /// argument.
        /// </summary>
        public DefeatRewardRestoreStateHolder DefeatRewards { get; }

        public static CardActionResolutionRestoreState WithoutPendingAction(
            DefeatRewardRestoreStateHolder defeatRewards) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.None,
                null,
                null,
                null,
                defeatRewards ?? throw new ArgumentNullException(nameof(defeatRewards)));

        public static CardActionResolutionRestoreState WithPendingMovement(
            PendingCardActionResolution pendingResolution,
            DefeatRewardRestoreStateHolder defeatRewards) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.Movement,
                pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution)),
                null,
                null,
                defeatRewards ?? throw new ArgumentNullException(nameof(defeatRewards)));

        public static CardActionResolutionRestoreState WithPendingAttack(
            PendingAttackResolution pendingResolution,
            DefeatRewardRestoreStateHolder defeatRewards) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.Attack,
                null,
                pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution)),
                null,
                defeatRewards ?? throw new ArgumentNullException(nameof(defeatRewards)));

        public void Deconstruct(
            out CardActionContinuationKind continuationKind,
            out PendingCardActionResolution pendingMovementResolution,
            out PendingAttackResolution pendingAttackResolution,
            out PendingResolution pendingSkillResolution,
            out DefeatRewardRestoreStateHolder defeatRewards)
        {
            continuationKind = ContinuationKind;
            pendingMovementResolution = PendingMovementResolution;
            pendingAttackResolution = PendingAttackResolution;
            pendingSkillResolution = PendingSkillResolution;
            defeatRewards = DefeatRewards;
        }

        public static CardActionResolutionRestoreState WithPendingSkill(
            PendingResolution pendingResolution,
            DefeatRewardRestoreStateHolder defeatRewards) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.Skill,
                null,
                null,
                pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution)),
                defeatRewards ?? throw new ArgumentNullException(nameof(defeatRewards)));
    }
}
