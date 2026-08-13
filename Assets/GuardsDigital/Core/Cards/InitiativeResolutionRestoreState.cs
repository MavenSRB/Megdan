using System;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The complete persisted Initiative state carried inside a restored turn.
    /// </summary>
    /// <remarks>
    /// The Tie Breaker state is written by the owning turn rather than by Initiative, so it
    /// stays a separate required argument on <see cref="InitiativeResolutionAuthority.Restore"/>
    /// instead of appearing here twice.
    /// </remarks>
    public sealed class InitiativeResolutionRestoreState
    {
        public InitiativeResolutionRestoreState(
            PendingInitiativeChoice pendingChoice,
            SelectedInitiativeActor selectedActor)
        {
            if (pendingChoice != null && selectedActor.IsDefined)
            {
                throw new ArgumentException(
                    "Restored Initiative state cannot contain both a pending choice and selected actor.");
            }

            PendingChoice = pendingChoice;
            SelectedActor = selectedActor;
        }

        public PendingInitiativeChoice PendingChoice { get; }
        public SelectedInitiativeActor SelectedActor { get; }

        public void Deconstruct(
            out PendingInitiativeChoice pendingChoice,
            out SelectedInitiativeActor selectedActor)
        {
            pendingChoice = PendingChoice;
            selectedActor = SelectedActor;
        }
    }
}
