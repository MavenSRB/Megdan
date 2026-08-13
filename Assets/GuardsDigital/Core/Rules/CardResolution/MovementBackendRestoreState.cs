using System;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The complete persisted state of a pending Movement continuation.
    /// </summary>
    /// <remarks>
    /// Only the pending resolution is persisted. The pending movement choice is *derived*
    /// from it during restore and never written to the snapshot, so it is not state and
    /// does not belong here. Callers that want the derived choice cross-checked against an
    /// independently supplied one pass it as the explicit verification argument on
    /// <see cref="MovementCardActionStrategyBackend.Restore"/>, which is required rather
    /// than defaulted so that skipping verification is a stated intent, not an omission.
    /// </remarks>
    internal sealed class MovementBackendRestoreState
    {
        internal MovementBackendRestoreState(PendingCardActionResolution pendingResolution)
        {
            PendingResolution = pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution));
            if (pendingResolution.MovementSessionState.IsComplete)
            {
                throw new ArgumentException(
                    "A restored pending resolution cannot contain a completed movement session.",
                    nameof(pendingResolution));
            }
        }

        internal PendingCardActionResolution PendingResolution { get; }

        internal void Deconstruct(out PendingCardActionResolution pendingResolution)
        {
            pendingResolution = PendingResolution;
        }
    }
}
