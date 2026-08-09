using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public sealed class PendingCardActionResolution
    {
        public PendingCardActionResolution(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionInvocationContext invocationContext,
            ActionResolutionStepId currentStepId,
            MovementSessionState movementSessionState)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Pending resolution requires a resolution ID.", nameof(resolutionId));
            }

            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException("Pending resolution requires a source card instance ID.", nameof(sourceCardInstanceId));
            }

            if (!sourceDefinitionReference.IsDefined)
            {
                throw new ArgumentException("Pending resolution requires a source definition reference.", nameof(sourceDefinitionReference));
            }

            if (!actionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Pending resolution requires an action definition ID.", nameof(actionDefinitionId));
            }

            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Pending resolution requires an actor ID.", nameof(actorId));
            }

            if (!Enum.IsDefined(typeof(ActionInvocationContext), invocationContext) ||
                invocationContext == ActionInvocationContext.None)
            {
                throw new ArgumentException("Pending resolution requires an invocation context.", nameof(invocationContext));
            }

            if (!currentStepId.IsDefined)
            {
                throw new ArgumentException("Pending resolution requires a current step ID.", nameof(currentStepId));
            }

            if (movementSessionState == null)
            {
                throw new ArgumentNullException(nameof(movementSessionState));
            }

            if (!movementSessionState.HeroId.Equals(actorId))
            {
                throw new ArgumentException("Pending resolution movement state must belong to its actor.", nameof(movementSessionState));
            }

            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            InvocationContext = invocationContext;
            CurrentStepId = currentStepId;
            MovementSessionState = movementSessionState;
        }

        public ActionResolutionId ResolutionId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public CardDefinitionReference SourceDefinitionReference { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public HeroId ActorId { get; }

        public ActionInvocationContext InvocationContext { get; }

        public ActionResolutionStepId CurrentStepId { get; }

        public MovementSessionState MovementSessionState { get; }

        public int MovementAllowance => MovementSessionState.MovementAllowance;

        public int RemainingAllowance => MovementSessionState.RemainingAllowance;
    }
}
