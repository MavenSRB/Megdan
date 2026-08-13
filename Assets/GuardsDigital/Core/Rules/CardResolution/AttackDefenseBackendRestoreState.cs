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
        {
            PendingResolution = pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution));
        }

        internal PendingAttackResolution PendingResolution { get; }

        internal void Deconstruct(out PendingAttackResolution pendingResolution)
        {
            pendingResolution = PendingResolution;
        }
    }
}
