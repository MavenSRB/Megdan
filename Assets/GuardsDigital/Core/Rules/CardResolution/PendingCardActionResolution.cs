using System;
using GuardsDigital.Core.Cards;
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
            int movementAllowance,
            int remainingAllowance)
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

            if (invocationContext == ActionInvocationContext.None)
            {
                throw new ArgumentException("Pending resolution requires an invocation context.", nameof(invocationContext));
            }

            if (!currentStepId.IsDefined)
            {
                throw new ArgumentException("Pending resolution requires a current step ID.", nameof(currentStepId));
            }

            if (movementAllowance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movementAllowance),
                    movementAllowance,
                    "Movement allowance must not be negative.");
            }

            if (remainingAllowance < 0 || remainingAllowance > movementAllowance)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(remainingAllowance),
                    remainingAllowance,
                    "Remaining movement allowance must be between zero and the starting allowance.");
            }

            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            InvocationContext = invocationContext;
            CurrentStepId = currentStepId;
            MovementAllowance = movementAllowance;
            RemainingAllowance = remainingAllowance;
        }

        public ActionResolutionId ResolutionId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public CardDefinitionReference SourceDefinitionReference { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public HeroId ActorId { get; }

        public ActionInvocationContext InvocationContext { get; }

        public ActionResolutionStepId CurrentStepId { get; }

        public int MovementAllowance { get; }

        public int RemainingAllowance { get; }
    }
}
