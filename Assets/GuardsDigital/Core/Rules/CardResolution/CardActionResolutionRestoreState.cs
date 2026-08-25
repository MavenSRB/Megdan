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
        Correction = 4,

        /// <summary>
        /// Milestone 8.10: an Attack stopped inside rulebook p.14's card text, because the text applied
        /// at one of the page's moments asked a question. It is a kind of its own rather than a flag on
        /// <see cref="Attack"/> because the two are genuinely different states - the attack's own
        /// resolution may already be finished and cleared while its text is still being applied - and
        /// because what resumes it is an effect-frame answer rather than an attack command.
        /// </summary>
        AttackCardText = 5
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
            PendingImmediateLanePushRequest pendingLanePush = null,
            PendingAttackCardTextResolution pendingCardText = null,
            PendingPreActionResolution pendingPreAction = null,
            PendingEffectInterrupt pendingEffectInterrupt = null,
            PendingRemovalPreventionDecision pendingRemovalPrevention = null,
            PendingAttackCardTextRemainder pendingCardTextRemainder = null)
        {
            PendingCardTextRemainder = pendingCardTextRemainder;
            PendingEffectInterrupt = pendingEffectInterrupt;
            PendingRemovalPrevention = pendingRemovalPrevention;
            PendingPreAction = pendingPreAction;
            PendingCardText = pendingCardText;
            ContinuationKind = continuationKind;
            PendingSkillResolution = pendingSkillResolution;
            PendingMovementResolution = pendingMovementResolution;
            PendingAttackResolution = pendingAttackResolution;
            CoinLedger = coinLedger;
            PendingCorrection = pendingCorrection;
            PendingRepeat = pendingRepeat;
            PendingImmediateLanePush = pendingLanePush;
        }

        /// <summary>
        /// Milestone 8.22. The rest of rulebook p.14, owed while an after-push trigger a Push inside
        /// its card text set off is standing, or <c>null</c>. It rides every continuation kind for the
        /// reason the Lane-Push request does: it is not a kind of pause but a fact about the page the
        /// trigger stopped, and the pause itself is the interrupt beside it.
        /// </summary>
        public PendingAttackCardTextRemainder PendingCardTextRemainder { get; }

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

        /// <summary>Set only when <see cref="ContinuationKind"/> is AttackCardText. Milestone 8.10.</summary>
        public PendingAttackCardTextResolution PendingCardText { get; }

        /// <summary>
        /// Milestone 8.19a's outer pre-action continuation, or <c>null</c>. It rides every continuation
        /// kind for the reason milestone 6.9's Lane-Push request does, and the reason is sharper here:
        /// work the actor placed after a Movement is parked while that Movement runs, so a payload
        /// carrying both is the ordinary case rather than an exception.
        /// </summary>
        public PendingPreActionResolution PendingPreAction { get; }

        /// <summary>
        /// Milestone 8.19c1's after-push trigger frame, or <c>null</c>. It rides every continuation kind
        /// for the reason the pre-action continuation does, and for a sharper version of that reason:
        /// the effect it interrupted is the continuation, standing at the node it reached.
        /// </summary>
        public PendingEffectInterrupt PendingEffectInterrupt { get; }

        /// <summary>
        /// Milestone 8.19c1's Defeat standing at its removal stage, or <c>null</c>. It rides every
        /// continuation kind because the two Defeats that can reach it are in different ones: rulebook
        /// p.14's minion arm leaves no continuation at all, and an effect-invoked Defeat is inside a
        /// triggered Skill's.
        /// </summary>
        public PendingRemovalPreventionDecision PendingRemovalPrevention { get; }

        /// <summary>The same state carrying a pre-action continuation beside whatever it already holds.</summary>
        public CardActionResolutionRestoreState WithPreAction(
            PendingPreActionResolution pendingPreAction) =>
            new CardActionResolutionRestoreState(
                ContinuationKind,
                PendingMovementResolution,
                PendingAttackResolution,
                PendingSkillResolution,
                CoinLedger,
                PendingCorrection,
                PendingRepeat,
                PendingImmediateLanePush,
                PendingCardText,
                pendingPreAction,
                PendingEffectInterrupt,
                PendingRemovalPrevention,
                PendingCardTextRemainder);

        /// <summary>The same state carrying milestone 8.19c1's two pauses beside whatever it holds.</summary>
        public CardActionResolutionRestoreState WithInterruptions(
            PendingEffectInterrupt pendingEffectInterrupt,
            PendingRemovalPreventionDecision pendingRemovalPrevention) =>
            new CardActionResolutionRestoreState(
                ContinuationKind,
                PendingMovementResolution,
                PendingAttackResolution,
                PendingSkillResolution,
                CoinLedger,
                PendingCorrection,
                PendingRepeat,
                PendingImmediateLanePush,
                PendingCardText,
                PendingPreAction,
                pendingEffectInterrupt,
                pendingRemovalPrevention,
                PendingCardTextRemainder);

        /// <summary>The same state carrying milestone 8.22's owed card-text remainder.</summary>
        public CardActionResolutionRestoreState WithCardTextRemainder(
            PendingAttackCardTextRemainder pendingCardTextRemainder) =>
            new CardActionResolutionRestoreState(
                ContinuationKind,
                PendingMovementResolution,
                PendingAttackResolution,
                PendingSkillResolution,
                CoinLedger,
                PendingCorrection,
                PendingRepeat,
                PendingImmediateLanePush,
                PendingCardText,
                PendingPreAction,
                PendingEffectInterrupt,
                PendingRemovalPrevention,
                pendingCardTextRemainder);

        public static CardActionResolutionRestoreState WithPendingCardText(
            PendingAttackCardTextResolution pendingCardText,
            HeroCoinLedgerRestoreStateHolder coinLedger,
            PendingImmediateLanePushRequest pendingLanePush = null) =>
            new CardActionResolutionRestoreState(
                CardActionContinuationKind.AttackCardText,
                null,
                null,
                null,
                coinLedger ?? throw new ArgumentNullException(nameof(coinLedger)),
                null,
                null,
                pendingLanePush,
                pendingCardText ?? throw new ArgumentNullException(nameof(pendingCardText)));

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
