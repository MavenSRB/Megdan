using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Movement;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public sealed class CardActionResolutionAuthority
    {
        private readonly AdjacentMoveAuthority adjacentMoveAuthority;
        private MovementSession movementSession;
        private PendingCardActionResolution pendingResolution;
        private PendingMovementChoice pendingChoice;
        private int committedStepCount;

        public CardActionResolutionAuthority(AdjacentMoveAuthority adjacentMoveAuthority)
        {
            this.adjacentMoveAuthority = adjacentMoveAuthority ?? throw new ArgumentNullException(nameof(adjacentMoveAuthority));
        }

        public PendingCardActionResolution PendingResolution => pendingResolution;

        public PendingMovementChoice PendingChoice => pendingChoice;

        public CardActionResolutionResult StartMovementResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Starting a card action resolution requires a resolution ID.", nameof(resolutionId));
            }

            if (pendingResolution != null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ResolutionAlreadyPending,
                    pendingResolution,
                    pendingChoice);
            }

            if (!offer.IsAvailable)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer,
                    pendingResolution,
                    pendingChoice);
            }

            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary &&
                offer.InvocationContext != ActionInvocationContext.PrintedSecondary)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnsupportedInvocationContext,
                    pendingResolution,
                    pendingChoice);
            }

            var actionDefinition = FindActionDefinition(offer, definitions);
            if (actionDefinition.Kind != ActionKind.Movement)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnsupportedActionKind,
                    pendingResolution,
                    pendingChoice);
            }

            var movementValue = PrintedValueQuery.CalculatePrintedValue(
                offer,
                definitions,
                PrintedValueChannel.Movement);

            movementSession = new MovementSession(
                adjacentMoveAuthority,
                offer.ActorId,
                movementValue.Total);
            committedStepCount = 0;
            pendingResolution = CreatePendingResolution(resolutionId, offer, movementSession.RemainingAllowance);
            pendingChoice = CreatePendingChoice(pendingResolution);

            return CardActionResolutionResult.Accept(
                false,
                pendingResolution,
                pendingChoice,
                new[]
                {
                    CardActionResolutionEvent.MovementActionStarted(pendingResolution),
                    CardActionResolutionEvent.MovementChoiceRequested(pendingResolution)
                });
        }

        public CardActionResolutionResult AnswerMovementChoice(MovementChoiceCommand command)
        {
            if (pendingResolution == null || pendingChoice == null)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.NoPendingResolution,
                    pendingResolution,
                    pendingChoice);
            }

            if (!command.ResolutionId.Equals(pendingChoice.ResolutionId) ||
                !command.ChoiceId.Equals(pendingChoice.ChoiceId))
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.StaleChoice,
                    pendingResolution,
                    pendingChoice);
            }

            if (command.ChoiceKind == MovementChoiceKind.Stop)
            {
                if (!pendingChoice.CanStop)
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.InvalidChoice,
                        pendingResolution,
                        pendingChoice);
                }

                return CompleteResolution();
            }

            if (command.ChoiceKind != MovementChoiceKind.Step ||
                !pendingChoice.LegalStepDestinations.Contains(command.Destination))
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.InvalidChoice,
                    pendingResolution,
                    pendingChoice);
            }

            var stepResult = movementSession.TryCommitStep(command.Destination);
            if (!stepResult.Accepted)
            {
                return CardActionResolutionResult.RejectMovementStep(
                    stepResult,
                    pendingResolution,
                    pendingChoice);
            }

            var completedStepResolution = pendingResolution;
            committedStepCount++;
            var movementEvent = stepResult.NewEvents.Single();
            var nextResolution = CreatePendingResolution(
                pendingResolution.ResolutionId,
                pendingResolution.SourceCardInstanceId,
                pendingResolution.SourceDefinitionReference,
                pendingResolution.ActionDefinitionId,
                pendingResolution.ActorId,
                pendingResolution.InvocationContext,
                movementSession.RemainingAllowance);

            if (movementSession.IsComplete)
            {
                pendingResolution = nextResolution;
                var completedEvent = CardActionResolutionEvent.MovementActionCompleted(pendingResolution);
                var stepEvent = CardActionResolutionEvent.MovementStepCommitted(completedStepResolution, movementEvent);

                pendingResolution = null;
                pendingChoice = null;
                movementSession = null;

                return CardActionResolutionResult.Accept(
                    true,
                    null,
                    null,
                    new[] { stepEvent, completedEvent });
            }

            pendingResolution = nextResolution;
            pendingChoice = CreatePendingChoice(pendingResolution);

            return CardActionResolutionResult.Accept(
                false,
                pendingResolution,
                pendingChoice,
                new[]
                {
                    CardActionResolutionEvent.MovementStepCommitted(completedStepResolution, movementEvent),
                    CardActionResolutionEvent.MovementChoiceRequested(pendingResolution)
                });
        }

        private CardActionResolutionResult CompleteResolution()
        {
            movementSession.Complete();
            var completedEvent = CardActionResolutionEvent.MovementActionCompleted(pendingResolution);

            pendingResolution = null;
            pendingChoice = null;
            movementSession = null;

            return CardActionResolutionResult.Accept(
                true,
                null,
                null,
                new[] { completedEvent });
        }

        private PendingMovementChoice CreatePendingChoice(PendingCardActionResolution resolution)
        {
            var currentOccupancy = movementSession.CurrentOccupancy;
            var legalDestinations = MovementReachabilityQuery.FindReachableDestinations(
                    currentOccupancy,
                    adjacentMoveAuthority.BoardCoordinates,
                    adjacentMoveAuthority.Occupancies,
                    adjacentMoveAuthority.ObstacleOccupancies,
                    movementSession.RemainingAllowance > 0 ? 1 : 0)
                .Select(destination => destination.Destination);

            return new PendingMovementChoice(
                resolution.ResolutionId,
                resolution.CurrentStepId,
                resolution.ActorId,
                currentOccupancy.HeroId,
                currentOccupancy.Coordinate,
                resolution.RemainingAllowance,
                true,
                legalDestinations);
        }

        private PendingCardActionResolution CreatePendingResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            int remainingAllowance)
        {
            return CreatePendingResolution(
                resolutionId,
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                offer.ActorId,
                offer.InvocationContext,
                remainingAllowance);
        }

        private PendingCardActionResolution CreatePendingResolution(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            GuardsDigital.Core.Units.Heroes.HeroId actorId,
            ActionInvocationContext invocationContext,
            int remainingAllowance)
        {
            return new PendingCardActionResolution(
                resolutionId,
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                actorId,
                invocationContext,
                new ActionResolutionStepId($"movement-step-{committedStepCount}"),
                movementSession.MovementAllowance,
                remainingAllowance);
        }

        private static ActionDefinition FindActionDefinition(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions)
        {
            var definitionsByReference = ActionOfferQuery.BuildDefinitionLookup(definitions);
            if (!definitionsByReference.TryGetValue(offer.SourceDefinitionReference, out var definition))
            {
                throw new ArgumentException("Action offer references an unresolved definition ID or content revision.", nameof(offer));
            }

            var actionDefinition = definition.ActionDefinitions.SingleOrDefault(action => action.Id.Equals(offer.ActionDefinitionId));
            if (actionDefinition == null)
            {
                throw new ArgumentException("Action offer references an unresolved action definition ID.", nameof(offer));
            }

            return actionDefinition;
        }
    }
}
