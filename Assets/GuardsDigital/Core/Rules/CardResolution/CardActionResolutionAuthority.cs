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
        private PendingCardActionResolution pendingResolution;
        private PendingMovementChoice pendingChoice;

        public CardActionResolutionAuthority(AdjacentMoveAuthority adjacentMoveAuthority)
        {
            this.adjacentMoveAuthority = adjacentMoveAuthority ?? throw new ArgumentNullException(nameof(adjacentMoveAuthority));
        }

        public PendingCardActionResolution PendingResolution => pendingResolution;

        public PendingMovementChoice PendingChoice => pendingChoice;

        public static CardActionResolutionAuthority Restore(
            AdjacentMoveAuthority adjacentMoveAuthority,
            PendingCardActionResolution pendingResolution,
            PendingMovementChoice pendingChoice)
        {
            if (pendingResolution == null)
            {
                throw new ArgumentNullException(nameof(pendingResolution));
            }

            if (pendingChoice == null)
            {
                throw new ArgumentNullException(nameof(pendingChoice));
            }

            if (pendingResolution.MovementSessionState.IsComplete)
            {
                throw new ArgumentException("A restored pending resolution cannot contain a completed movement session.", nameof(pendingResolution));
            }

            var authority = new CardActionResolutionAuthority(adjacentMoveAuthority);
            var movementSession = new MovementSession(adjacentMoveAuthority, pendingResolution.MovementSessionState);
            var expectedStepId = CreateStepId(movementSession.CaptureState().CommittedStepCount);
            if (!pendingResolution.CurrentStepId.Equals(expectedStepId))
            {
                throw new ArgumentException("Pending resolution step ID does not match its movement continuation.", nameof(pendingResolution));
            }

            var expectedChoice = authority.CreatePendingChoice(pendingResolution, movementSession);
            if (!ChoicesMatch(expectedChoice, pendingChoice))
            {
                throw new ArgumentException("Pending movement choice does not match the authoritative continuation state.", nameof(pendingChoice));
            }

            authority.pendingResolution = pendingResolution;
            authority.pendingChoice = pendingChoice;
            return authority;
        }

        public CardActionResolutionResult StartMovementResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
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

            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary &&
                offer.InvocationContext != ActionInvocationContext.PrintedSecondary)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnsupportedInvocationContext,
                    pendingResolution,
                    pendingChoice);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authoritativeOffers = ActionOfferQuery.GetAvailableOffers(
                materializedDefinitions,
                materializedInstances,
                offer.ActorId,
                ActionOfferWindow.NormalPlay);
            if (!offer.IsAvailable || !authoritativeOffers.Contains(offer))
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer,
                    pendingResolution,
                    pendingChoice);
            }

            var actionDefinition = FindActionDefinition(offer, materializedDefinitions);
            if (actionDefinition.Kind != ActionKind.Movement)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnsupportedActionKind,
                    pendingResolution,
                    pendingChoice);
            }

            if (!adjacentMoveAuthority.Occupancies.Any(occupancy => occupancy.HeroId.Equals(offer.ActorId)))
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.ActorUnavailable,
                    pendingResolution,
                    pendingChoice);
            }

            var movementValue = PrintedValueQuery.CalculatePrintedValue(
                offer,
                materializedDefinitions,
                PrintedValueChannel.Movement);
            var movementSession = new MovementSession(adjacentMoveAuthority, offer.ActorId, movementValue.Total);
            pendingResolution = CreatePendingResolution(resolutionId, offer, movementSession.CaptureState());
            pendingChoice = CreatePendingChoice(pendingResolution, movementSession);

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

            var movementSession = new MovementSession(adjacentMoveAuthority, pendingResolution.MovementSessionState);
            if (command.ChoiceKind == MovementChoiceKind.Stop)
            {
                if (!pendingChoice.CanStop)
                {
                    return CardActionResolutionResult.Reject(
                        CardActionResolutionRejectionReason.InvalidChoice,
                        pendingResolution,
                        pendingChoice);
                }

                return CompleteResolution(movementSession);
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
            var movementEvent = stepResult.NewEvents.Single();
            var nextResolution = CreatePendingResolution(completedStepResolution, movementSession.CaptureState());
            if (movementSession.IsComplete)
            {
                var stepEvent = CardActionResolutionEvent.MovementStepCommitted(completedStepResolution, movementEvent);
                var completedEvent = CardActionResolutionEvent.MovementActionCompleted(nextResolution);
                pendingResolution = null;
                pendingChoice = null;

                return CardActionResolutionResult.Accept(
                    true,
                    null,
                    null,
                    new[] { stepEvent, completedEvent });
            }

            pendingResolution = nextResolution;
            pendingChoice = CreatePendingChoice(pendingResolution, movementSession);
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

        private CardActionResolutionResult CompleteResolution(MovementSession movementSession)
        {
            movementSession.Complete();
            var completedResolution = CreatePendingResolution(pendingResolution, movementSession.CaptureState());
            var completedEvent = CardActionResolutionEvent.MovementActionCompleted(completedResolution);
            pendingResolution = null;
            pendingChoice = null;

            return CardActionResolutionResult.Accept(
                true,
                null,
                null,
                new[] { completedEvent });
        }

        private PendingMovementChoice CreatePendingChoice(
            PendingCardActionResolution resolution,
            MovementSession movementSession)
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

        private static PendingCardActionResolution CreatePendingResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            MovementSessionState movementState)
        {
            return new PendingCardActionResolution(
                resolutionId,
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                offer.ActorId,
                offer.InvocationContext,
                CreateStepId(movementState.CommittedStepCount),
                movementState);
        }

        private static PendingCardActionResolution CreatePendingResolution(
            PendingCardActionResolution previous,
            MovementSessionState movementState)
        {
            return new PendingCardActionResolution(
                previous.ResolutionId,
                previous.SourceCardInstanceId,
                previous.SourceDefinitionReference,
                previous.ActionDefinitionId,
                previous.ActorId,
                previous.InvocationContext,
                CreateStepId(movementState.CommittedStepCount),
                movementState);
        }

        private static ActionResolutionStepId CreateStepId(int committedStepCount)
        {
            return new ActionResolutionStepId($"movement-step-{committedStepCount}");
        }

        private static bool ChoicesMatch(PendingMovementChoice expected, PendingMovementChoice actual)
        {
            return expected.ResolutionId.Equals(actual.ResolutionId) &&
                expected.ChoiceId.Equals(actual.ChoiceId) &&
                expected.ChooserId.Equals(actual.ChooserId) &&
                expected.MovingHeroId.Equals(actual.MovingHeroId) &&
                expected.Origin.Equals(actual.Origin) &&
                expected.RemainingAllowance == actual.RemainingAllowance &&
                expected.CanStop == actual.CanStop &&
                expected.LegalStepDestinations.Count == actual.LegalStepDestinations.Count &&
                !expected.LegalStepDestinations.Except(actual.LegalStepDestinations).Any();
        }

        private static ActionDefinition FindActionDefinition(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions)
        {
            var definitionsByReference = ActionOfferQuery.BuildDefinitionLookup(definitions);
            var definition = definitionsByReference[offer.SourceDefinitionReference];
            return definition.ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));
        }
    }
}
