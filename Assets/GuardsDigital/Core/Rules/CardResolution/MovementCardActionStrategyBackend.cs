using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
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

        /// <summary>
        /// Milestone 7.2a: the same item state the round authority owns, bound by the dispatcher for the
        /// reason the Attack and Defense backend's is. A Movement item raises the value the action
        /// starts with, which is what gates the action and bounds its reachability. Absent in a
        /// composition with no items, where the printed Movement is the total.
        /// </summary>
        private GuardsDigital.Core.Rules.Progression.HeroItemState heroItems;

        internal MovementCardActionStrategyBackend(
            AdjacentMoveAuthority adjacentMoveAuthority,
            IMovementActionStartRestrictionEvaluator actionStartRestrictions = null)
        {
            this.adjacentMoveAuthority = adjacentMoveAuthority ?? throw new ArgumentNullException(nameof(adjacentMoveAuthority));
            this.actionStartRestrictions = actionStartRestrictions ??
                UnrestrictedMovementActionStartEvaluator.Instance;
        }

        internal void BindHeroItems(GuardsDigital.Core.Rules.Progression.HeroItemState items)
        {
            heroItems = items;
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
            PendingMovementChoice verifiedChoice,
            RuleModificationSet modifications)
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

            var expectedChoice = backend.CreatePendingChoice(pendingResolution, movementSession, modifications);
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
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
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
                authoritativeOffers,
                modifications);
        }

        public CardActionResolutionResult StartTurnMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
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
                authoritativeOffers,
                modifications);
        }

        private CardActionResolutionResult StartMovementResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> materializedDefinitions,
            IReadOnlyList<CardInstance> materializedInstances,
            IReadOnlyList<ActionOffer> authoritativeOffers,
            RuleModificationSet modifications)
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

            // Milestone 7.2a. p.16's item bonus is composed onto the printed Movement before anything
            // reads it, so the value the start restriction judges and the value the session spends are
            // the one Movement Total this card has. Composing after the restriction would let a
            // prevented action be re-enabled by an item, which is a different rule than the page states.
            // Milestone 8.19c moved the composition into MovementValueQuery when a source became a
            // second contributor to it; the ordering this comment describes is stated there and is
            // unchanged.
            var movementValue = MovementValueQuery.Calculate(
                offer, materializedDefinitions, heroItems, modifications);
            // Milestone 8.20. Two families can now answer this: the composed evaluator seam, and a
            // declared `limit-activity@1` - Arien's Slippery Ground and Deluge, "cannot ... move more
            // than 1 space with a movement action". They are combined into the one restriction the
            // session is built from rather than consulted in turn, so nothing downstream has two
            // answers to choose between. The cap is read after the Movement Total is composed, which
            // is what makes it a cap rather than a contribution: p.16's item raises the total and the
            // sentence still holds it to the number it names.
            var restriction = MovementActionStartRestriction.Tightest(
                actionStartRestrictions.Evaluate(new MovementActionStartContext(
                    offer.ActorId,
                    offer.SourceCardInstanceId,
                    offer.ActionDefinitionId,
                    offer.InvocationContext,
                    movementValue.Total)),
                modifications != null &&
                modifications.TryGetActivityLimit(
                    RuleObject.ForHero(offer.ActorId), RuleActivity.Movement, out var declaredMaximum)
                    ? MovementActionStartRestriction.LimitTo(declaredMaximum)
                    : MovementActionStartRestriction.None);
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
            pendingChoice = CreatePendingChoice(pendingResolution, movementSession, modifications);

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

        public CardActionResolutionResult AnswerMovement(
            MovementChoiceCommand command, RuleModificationSet modifications)
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
            var authoritativeChoice = CreatePendingChoice(pendingResolution, movementSession, modifications);
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
                    modifications,
                    out var rejectedStep))
            {
                return CardActionResolutionResult.RejectMovementStep(
                    rejectedStep,
                    pendingResolution,
                    pendingChoice);
            }

            var resolutionEvents = new List<ResolutionStep>();
            var stepResolution = pendingResolution;

            // One transaction for the whole route, because a space the mover only passes through is
            // never occupied and so cannot be committed on its own. The facts stay per space: the unit
            // really does enter and leave each one (p.18), and a route crossing nothing occupied emits
            // exactly what the former per-space walk emitted.
            var routeSpaces = selectedDestination.Route.Skip(1).ToArray();
            var routeResult = movementSession.TryCommitRoute(
                routeSpaces, modifications, out var committedStepStates);
            if (!routeResult.Accepted)
            {
                throw new InvalidOperationException(
                    "A backend-projected movement route failed after side-effect-free validation.");
            }

            for (var routeIndex = 0; routeIndex < routeResult.NewEvents.Count; routeIndex++)
            {
                resolutionEvents.Add(ResolutionStep.FromFact(CardActionResolutionEvent.MovementStepCommitted(
                    stepResolution,
                    routeResult.NewEvents[routeIndex])));
                stepResolution = CreatePendingResolution(stepResolution, committedStepStates[routeIndex]);
            }

            if (movementSession.IsComplete)
            {
                movementSession.Complete();
                var completedResolution = CreatePendingResolution(stepResolution, movementSession.CaptureState());
                resolutionEvents.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.MovementActionCompleted(completedResolution)));
                resolutionEvents.Add(ResolutionStep.AtActionEnded(
                    completedResolution.SourceCardInstanceId,
                    completedResolution.ActionDefinitionId,
                    completedResolution.ActorId,
                    ActionKind.Movement));
                pendingResolution = null;
                pendingChoice = null;

                return CardActionResolutionResult.Accept(
                    true,
                    null,
                    null,
                    resolutionEvents);
            }

            var nextResolution = CreatePendingResolution(stepResolution, movementSession.CaptureState());
            var nextChoice = CreatePendingChoice(nextResolution, movementSession, modifications);
            if (nextChoice.LegalDestinations.Count == 0)
            {
                movementSession.Complete();
                var completedResolution = CreatePendingResolution(nextResolution, movementSession.CaptureState());
                resolutionEvents.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.MovementActionCompleted(completedResolution)));
                resolutionEvents.Add(ResolutionStep.AtActionEnded(
                    completedResolution.SourceCardInstanceId,
                    completedResolution.ActionDefinitionId,
                    completedResolution.ActorId,
                    ActionKind.Movement));
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
                    ResolutionStep.AtActionEnded(
                        completedResolution.SourceCardInstanceId,
                        completedResolution.ActionDefinitionId,
                        completedResolution.ActorId,
                        ActionKind.Movement)
                });
        }

        private PendingMovementChoice CreatePendingChoice(
            PendingCardActionResolution resolution,
            MovementSession movementSession,
            RuleModificationSet modifications)
        {
            var currentOccupancy = movementSession.CurrentOccupancy;
            var legalDestinations = MovementReachabilityQuery.FindReachableDestinations(
                currentOccupancy,
                adjacentMoveAuthority,
                movementSession.RemainingAllowance,
                modifications);

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
            RuleModificationSet modifications,
            out MovementSessionStepResult rejectedStep)
        {
            // The fork clones already-validated board state without re-running the constructor's
            // full occupancy validation on every accepted movement step. It validates the same whole
            // route the real commit will take, under the same modifications, so a route the fork
            // accepts cannot be one the commit refuses.
            var validationAuthority = adjacentMoveAuthority.CreateValidationFork();
            var validationSession = new MovementSession(validationAuthority, movementState);
            var stepResult = validationSession.TryCommitRoute(
                destination.Route.Skip(1).ToArray(), modifications);
            if (!stepResult.Accepted)
            {
                rejectedStep = stepResult;
                return false;
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
