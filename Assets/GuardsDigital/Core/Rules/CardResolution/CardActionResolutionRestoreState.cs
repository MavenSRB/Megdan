using System;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Displacement;
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
        Skill = 3,

        /// <summary>
        /// Milestone 6.4a: the action's effect nodes have all finished and the after-action
        /// displaced-minion correction is paused on a team choice. Resuming it resumes the same
        /// authoritative continuation - the source card resolves and Initiative advances only when
        /// the correction completes.
        /// </summary>
        Correction = 4
    }

    /// <summary>
    /// The complete persisted state of a <see cref="CardActionResolutionAuthority"/>.
    /// </summary>
    /// <remarks>
    /// This replaces five separate restore entry points - two <c>Restore</c> overloads, two
    /// <c>RestoreAttack</c> overloads, and <c>RestoreWithoutPendingAction</c> - four of
    /// which defaulted <c>coinLedger</c> to <c>null</c> and so silently discarded every
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
            HeroCoinLedgerRestoreStateHolder coinLedger,
            PendingDisplacedMinionCorrection pendingCorrection = null,
            PendingRepeatedAction pendingRepeat = null,
            PendingImmediateLanePushRequest pendingLanePush = null)
        {
            ContinuationKind = continuationKind;
            PendingSkillResolution = pendingSkillResolution;
            PendingMovementResolution = pendingMovementResolution;
            PendingAttackResolution = pendingAttackResolution;
            CoinLedger = coinLedger;
            PendingCorrection = pendingCorrection;
            PendingRepeat = pendingRepeat;
            PendingImmediateLanePush = pendingLanePush;
        }

        public CardActionContinuationKind ContinuationKind { get; }

        /// <summary>Set only when <see cref="ContinuationKind"/> is Movement.</summary>
        public PendingCardActionResolution PendingMovementResolution { get; }

        /// <summary>Set only when <see cref="ContinuationKind"/> is Attack.</summary>
        public PendingAttackResolution PendingAttackResolution { get; }

        /// <summary>Set only when <see cref="ContinuationKind"/> is Skill.</summary>
        public PendingResolution PendingSkillResolution { get; }

        /// <summary>Set only when <see cref="ContinuationKind"/> is Correction.</summary>
        public PendingDisplacedMinionCorrection PendingCorrection { get; }

        /// <summary>
        /// Milestone 6.7's owed second action instance, or <c>null</c>. It rides the Correction
        /// continuation rather than becoming a kind of its own, because a repeat is not a kind of
        /// pause: it is a fact about the action boundary the correction is holding open, and the only
        /// state in which one survives a command is a correction that stopped for a team choice.
        /// </summary>
        public PendingRepeatedAction PendingRepeat { get; }

        /// <summary>
        /// Milestone 6.9's mid-turn Lane-Push request, or <c>null</c>. It rides every continuation kind
        /// rather than becoming one of its own, for the reason the repeat rides the correction: it is
        /// not a kind of pause but a fact about the board the action left behind, and a Skill can record
        /// p.8's cause at a node and then stop on a later choice, so the two genuinely coexist.
        ///
        /// It is also the only one of these that outlives its action. A turn holding one is stopped at
        /// the mid-turn checkpoint, so a snapshot taken there must restore a turn that is stopped too -
        /// which is why it is persisted rather than re-derived: nothing about the restored board says
        /// whether the side was emptied by a Defeat, a Remove, or a push p.8 excludes.
        /// </summary>
        public PendingImmediateLanePushRequest PendingImmediateLanePush { get; }

        /// <summary>
        /// The coin ledger to restore. Never omitted: an authority with no recorded
        /// balances carries an explicitly absent holder rather than a missing argument.
        /// </summary>
        public HeroCoinLedgerRestoreStateHolder CoinLedger { get; }

        public static CardActionResolutionRestoreState WithoutPendingAction(
            HeroCoinLedgerRestoreStateHolder coinLedger,
            PendingImmediateLanePushRequest pendingLanePush = null) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.None,
                null,
                null,
                null,
                coinLedger ?? throw new ArgumentNullException(nameof(coinLedger)),
                null,
                null,
                pendingLanePush);

        public static CardActionResolutionRestoreState WithPendingMovement(
            PendingCardActionResolution pendingResolution,
            HeroCoinLedgerRestoreStateHolder coinLedger,
            PendingImmediateLanePushRequest pendingLanePush = null) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.Movement,
                pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution)),
                null,
                null,
                coinLedger ?? throw new ArgumentNullException(nameof(coinLedger)),
                null,
                null,
                pendingLanePush);

        public static CardActionResolutionRestoreState WithPendingAttack(
            PendingAttackResolution pendingResolution,
            HeroCoinLedgerRestoreStateHolder coinLedger,
            PendingImmediateLanePushRequest pendingLanePush = null) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.Attack,
                null,
                pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution)),
                null,
                coinLedger ?? throw new ArgumentNullException(nameof(coinLedger)),
                null,
                null,
                pendingLanePush);

        public void Deconstruct(
            out CardActionContinuationKind continuationKind,
            out PendingCardActionResolution pendingMovementResolution,
            out PendingAttackResolution pendingAttackResolution,
            out PendingResolution pendingSkillResolution,
            out HeroCoinLedgerRestoreStateHolder coinLedger)
        {
            continuationKind = ContinuationKind;
            pendingMovementResolution = PendingMovementResolution;
            pendingAttackResolution = PendingAttackResolution;
            pendingSkillResolution = PendingSkillResolution;
            coinLedger = CoinLedger;
        }

        public static CardActionResolutionRestoreState WithPendingSkill(
            PendingResolution pendingResolution,
            HeroCoinLedgerRestoreStateHolder coinLedger,
            PendingImmediateLanePushRequest pendingLanePush = null) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.Skill,
                null,
                null,
                pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution)),
                coinLedger ?? throw new ArgumentNullException(nameof(coinLedger)),
                null,
                null,
                pendingLanePush);

        public static CardActionResolutionRestoreState WithPendingCorrection(
            PendingDisplacedMinionCorrection pendingCorrection,
            HeroCoinLedgerRestoreStateHolder coinLedger,
            PendingRepeatedAction pendingRepeat = null,
            PendingImmediateLanePushRequest pendingLanePush = null) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.Correction,
                null,
                null,
                null,
                coinLedger ?? throw new ArgumentNullException(nameof(coinLedger)),
                pendingCorrection ?? throw new ArgumentNullException(nameof(pendingCorrection)),
                pendingRepeat,
                pendingLanePush);
    }
}
