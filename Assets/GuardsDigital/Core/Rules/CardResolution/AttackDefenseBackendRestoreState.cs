using System;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The complete persisted state of a pending Attack continuation.
    /// </summary>
    /// <remarks>
    /// Only the pending Attack resolution is persisted. Both the target choice and the
    /// Defense response choice are *derived* from it and the current offers during restore
    /// and are never written to the snapshot, so neither is state. Callers that hold an
    /// independently obtained choice pass it as an explicit verification argument on
    /// <see cref="AttackDefenseCardActionStrategyBackend.RestoreAttack"/>; passing
    /// <c>null</c> accepts the derived choice. Those arguments are required rather than
    /// defaulted, so declining verification is stated at the call site.
    /// </remarks>
    internal sealed class AttackDefenseBackendRestoreState
    {
        internal AttackDefenseBackendRestoreState(PendingAttackResolution pendingResolution)
            : this(pendingResolution, null)
        {
        }

        internal AttackDefenseBackendRestoreState(
            PendingAttackResolution pendingResolution,
            PendingAttackSequenceChain sequenceChain)
        {
            PendingResolution = pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution));
            SequenceChain = sequenceChain;
        }

        internal PendingAttackResolution PendingResolution { get; }

        /// <summary>
        /// Milestone 8.35z27. The modal Attack plan this pending resolution is a sequence
        /// of, or <c>null</c>.
        ///
        /// <para>It is the one exception to the remark above: a Attack target choice really is derived
        /// on restore, and for a sequence of a modal Attack it cannot be derived from the pending
        /// resolution alone - the bullet's own targeting sentence and the units earlier sequences took
        /// are the chain's, and a restore without them would offer the wrong set.</para>
        /// </summary>
        internal PendingAttackSequenceChain SequenceChain { get; }

        internal void Deconstruct(out PendingAttackResolution pendingResolution)
        {
            pendingResolution = PendingResolution;
        }
    }
}
