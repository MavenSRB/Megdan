using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Rules.CardResolution
{
    internal sealed class MovementCardActionStrategyBackend
    {
        private readonly AdjacentMoveAuthority adjacentMoveAuthority;
        private readonly IMovementActionStartRestrictionEvaluator actionStartRestrictions;
        private PendingCardActionResolution pendingResolution;
        private PendingMovementChoice pendingChoice;

        internal MovementCardActionStrategyBackend(
            AdjacentMoveAuthority adjacentMoveAuthority,
            IMovementActionStartRestrictionEvaluator actionStartRestrictions = null)
        {
            this.adjacentMoveAuthority = adjacentMoveAuthority ?? throw new ArgumentNullException(nameof(adjacentMoveAuthority));
            this.actionStartRestrictions = actionStartRestrictions ??
                UnrestrictedMovementActionStartEvaluator.Instance;
        }

        internal PendingCardActionResolution PendingResolution => pendingResolution;

        internal PendingMovementChoice PendingChoice => pendingChoice;

        internal bool HasPendingResolution => pendingResolution != null;

        internal MovementBackendRestoreState CaptureRestoreState() =>
            new MovementBackendRestoreState(pendingResolution);

        /// <param name="verifiedChoice">
        /// An independently supplied pending choice to cross-check against the one derived
        /// from the resolution, or <c>null</c> to accept the derived choice. Required
        /// rather than defaulted: skipping the cross-check is a decision the call site
        /// states, not one it can omit.
        /// </param>
        internal static MovementCardActionStrategyBackend Restore(
            AdjacentMoveAuthority adjacentMoveAuthority,
            MovementBackendRestoreState state,
            PendingMovementChoice verifiedChoice)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var pendingResolution = state.PendingResolution;
            var backend = new MovementCardActionStrategyBackend(adjacentMoveAuthority);
            var movementSession = new MovementSession(adjacentMoveAuthority, pendingResolution.MovementSessionState);
            var expectedStepId = CreateStepId(movementSession.CaptureState().CommittedStepCount);
            if (!pendingResolution.CurrentStepId.Equals(expectedStepId))
            {
                throw new ArgumentException("Pending resolution step ID does not match its movement continuation.", nameof(state));
            }

            var expectedChoice = backend.CreatePendingChoice(pendingResolution, movementSession);
            if (verifiedChoice != null && !ChoicesMatch(expectedChoice, verifiedChoice))
            {
                throw new ArgumentException("Pending movement choice does not match the authoritative continuation state.", nameof(verifiedChoice));
            }

            backend.pendingResolution = pendingResolution;
            backend.pendingChoice = verifiedChoice ?? expectedChoice;
            return backend;
        }

        public CardActionResolutionResult StartMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authoritativeOffers = ActionOfferQuery.GetAvailableOffers(
                materializedDefinitions,
                materializedInstances,
                offer.ActorId,
                ActionOfferWindow.NormalPlay);
            return StartMovementResolution(
                resolutionId,
                offer,
                materializedDefinitions,
                materializedInstances,
                authoritativeOffers);
        }

        public CardActionResolutionResult StartTurnMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authoritativeOffers = ActionOfferQuery.GetAvailableTurnActionOffers(
                materializedDefinitions,
                materializedInstances,
                offer.ActorId,
                offer.SourceCardInstanceId);
            return StartMovementResolution(
                resolutionId,
                offer,
                materializedDefinitions,
                materializedInstances,
                authoritativeOffers);
        }

        private CardActionResolutionResult StartMovementResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> materializedDefinitions,
            IReadOnlyList<CardInstance> materializedInstances,
            IReadOnlyList<ActionOffer> authoritativeOffers)
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

            if (!offer.IsAvailable || !authoritativeOffers.Contains(offer))
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnavailableOffer,
                    pendingResolution,
                    pendingChoice);
            }

            var actionDefinition = ActionOfferQuery.FindActionDefinition(offer, materializedDefinitions);
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
            var restriction = actionStartRestrictions.Evaluate(new MovementActionStartContext(
                offer.ActorId,
                offer.SourceCardInstanceId,
                offer.ActionDefinitionId,
                offer.InvocationContext,
                movementValue.Total));
            if (restriction.PreventsAction)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.MovementActionPrevented,
                    pendingResolution,
                    pendingChoice);
            }

            var movementSession = new MovementSession(
                adjacentMoveAuthority,
                offer.ActorId,
                restriction.ApplyTo(movementValue.Total));
            pendingResolution = CreatePendingResolution(resolutionId, offer, movementSession.CaptureState());
            pendingChoice = CreatePendingChoice(pendingResolution, movementSession);

            return CardActionResolutionResult.Accept(
                false,
                pendingResolution,
                pendingChoice,
                new[]
                {
                    ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted),
                    ResolutionStep.FromFact(CardActionResolutionEvent.MovementActionStarted(pendingResolution)),
                    ResolutionStep.FromFact(CardActionResolutionEvent.MovementChoiceRequested(pendingResolution))
                });
        }

        public CardActionResolutionResult AnswerMovement(MovementChoiceCommand command)
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
            var authoritativeChoice = CreatePendingChoice(pendingResolution, movementSession);
            if (!ChoicesMatch(authoritativeChoice, pendingChoice))
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.InvalidChoice,
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

                return CompleteResolution(movementSession);
            }

            if (command.ChoiceKind != MovementChoiceKind.Destination)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.InvalidChoice,
                    pendingResolution,
                    pendingChoice);
            }

            var matchingDestinations = authoritativeChoice.LegalDestinations
                .Where(candidate => candidate.Destination.Equals(command.Destination))
                .ToArray();
            if (matchingDestinations.Length != 1)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.InvalidChoice,
                    pendingResolution,
                    pendingChoice);
            }

            var selectedDestination = matchingDestinations[0];
            if (!TryValidateDestinationRoute(
                    pendingResolution.MovementSessionState,
                    selectedDestination,
                    out var rejectedStep))
            {
                return CardActionResolutionResult.RejectMovementStep(
                    rejectedStep,
                    pendingResolution,
                    pendingChoice);
            }

            var resolutionEvents = new List<ResolutionStep>();
            var stepResolution = pendingResolution;
            for (var routeIndex = 1; routeIndex < selectedDestination.Route.Count; routeIndex++)
            {
                var stepResult = movementSession.TryCommitStep(selectedDestination.Route[routeIndex]);
                if (!stepResult.Accepted)
                {
                    throw new InvalidOperationException(
                        "An backend-projected movement route failed after side-effect-free validation.");
                }

                resolutionEvents.Add(ResolutionStep.FromFact(CardActionResolutionEvent.MovementStepCommitted(
                    stepResolution,
                    stepResult.NewEvents.Single())));
                stepResolution = CreatePendingResolution(stepResolution, movementSession.CaptureState());
            }

            if (movementSession.IsComplete)
            {
                movementSession.Complete();
                var completedResolution = CreatePendingResolution(stepResolution, movementSession.CaptureState());
                resolutionEvents.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.MovementActionCompleted(completedResolution)));
                resolutionEvents.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionEnded));
                pendingResolution = null;
                pendingChoice = null;

                return CardActionResolutionResult.Accept(
                    true,
                    null,
                    null,
                    resolutionEvents);
            }

            var nextResolution = CreatePendingResolution(stepResolution, movementSession.CaptureState());
            var nextChoice = CreatePendingChoice(nextResolution, movementSession);
            if (nextChoice.LegalDestinations.Count == 0)
            {
                movementSession.Complete();
                var completedResolution = CreatePendingResolution(nextResolution, movementSession.CaptureState());
                resolutionEvents.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.MovementActionCompleted(completedResolution)));
                resolutionEvents.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionEnded));
                pendingResolution = null;
                pendingChoice = null;

                return CardActionResolutionResult.Accept(
                    true,
                    null,
                    null,
                    resolutionEvents);
            }

            pendingResolution = nextResolution;
            pendingChoice = nextChoice;
            resolutionEvents.Add(ResolutionStep.FromFact(
                CardActionResolutionEvent.MovementChoiceRequested(nextResolution)));

            return CardActionResolutionResult.Accept(
                false,
                pendingResolution,
                pendingChoice,
                resolutionEvents);
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
                new[]
                {
                    ResolutionStep.FromFact(completedEvent),
                    ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionEnded)
                });
        }

        private PendingMovementChoice CreatePendingChoice(
            PendingCardActionResolution resolution,
            MovementSession movementSession)
        {
            var currentOccupancy = movementSession.CurrentOccupancy;
            var legalDestinations = MovementReachabilityQuery.FindReachableDestinations(
                currentOccupancy,
                adjacentMoveAuthority,
                movementSession.RemainingAllowance);

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

        private bool TryValidateDestinationRoute(
            MovementSessionState movementState,
            ReachableMovementDestination destination,
            out MovementSessionStepResult rejectedStep)
        {
            // The fork clones already-validated board state without re-running the constructor's
            // full occupancy validation on every accepted movement step.
            var validationAuthority = adjacentMoveAuthority.CreateValidationFork();
            var validationSession = new MovementSession(validationAuthority, movementState);
            for (var routeIndex = 1; routeIndex < destination.Route.Count; routeIndex++)
            {
                var stepResult = validationSession.TryCommitStep(destination.Route[routeIndex]);
                if (!stepResult.Accepted)
                {
                    rejectedStep = stepResult;
                    return false;
                }
            }

            rejectedStep = default;
            return true;
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
                DestinationChoicesMatch(expected.LegalDestinations, actual.LegalDestinations);
        }

        private static bool DestinationChoicesMatch(
            IReadOnlyList<ReachableMovementDestination> expected,
            IReadOnlyList<ReachableMovementDestination> actual)
        {
            if (expected.Count != actual.Count)
            {
                return false;
            }

            for (var index = 0; index < expected.Count; index++)
            {
                if (!expected[index].Destination.Equals(actual[index].Destination) ||
                    expected[index].Cost != actual[index].Cost ||
                    !expected[index].Route.SequenceEqual(actual[index].Route))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
