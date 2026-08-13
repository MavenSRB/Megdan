using System;
using GuardsDigital.Core.Rules.Consequences;

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
        Attack = 2
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
            DefeatRewardRestoreStateHolder defeatRewards)
        {
            ContinuationKind = continuationKind;
            PendingMovementResolution = pendingMovementResolution;
            PendingAttackResolution = pendingAttackResolution;
            DefeatRewards = defeatRewards;
        }

        public CardActionContinuationKind ContinuationKind { get; }

        /// <summary>Set only when <see cref="ContinuationKind"/> is Movement.</summary>
        public PendingCardActionResolution PendingMovementResolution { get; }

        /// <summary>Set only when <see cref="ContinuationKind"/> is Attack.</summary>
        public PendingAttackResolution PendingAttackResolution { get; }

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
                defeatRewards ?? throw new ArgumentNullException(nameof(defeatRewards)));

        public static CardActionResolutionRestoreState WithPendingMovement(
            PendingCardActionResolution pendingResolution,
            DefeatRewardRestoreStateHolder defeatRewards) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.Movement,
                pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution)),
                null,
                defeatRewards ?? throw new ArgumentNullException(nameof(defeatRewards)));

        public static CardActionResolutionRestoreState WithPendingAttack(
            PendingAttackResolution pendingResolution,
            DefeatRewardRestoreStateHolder defeatRewards) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.Attack,
                null,
                pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution)),
                defeatRewards ?? throw new ArgumentNullException(nameof(defeatRewards)));

        public void Deconstruct(
            out CardActionContinuationKind continuationKind,
            out PendingCardActionResolution pendingMovementResolution,
            out PendingAttackResolution pendingAttackResolution,
            out DefeatRewardRestoreStateHolder defeatRewards)
        {
            continuationKind = ContinuationKind;
            pendingMovementResolution = PendingMovementResolution;
            pendingAttackResolution = PendingAttackResolution;
            defeatRewards = DefeatRewards;
        }
    }
}
