using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;
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

        /// <summary>
        /// The whole walk the Movement action that just finished took, and the hero who took it, or a
        /// default pair when nothing has finished since it was last taken. Milestone 8.61e.
        ///
        /// <para><b>Why the route and not the board's own record of it.</b> Rulebook p.18's token
        /// effects apply after the WHOLE move, and a Movement action can be several route commits with
        /// a decision - and therefore a snapshot - between them. The session's route is persisted state
        /// that a restore rebuilds; an in-memory note of what was walked between two commands is not.
        /// So the action's consequences are computed from the one thing that survives a restore, which
        /// is the same reasoning milestone 6.8c used to make the route the unit of commitment.</para>
        /// </summary>
        private IReadOnlyList<HexCoordinate> completedWalkRoute;
        private HeroId completedWalkHeroId;

        internal PendingCardActionResolution PendingResolution => pendingResolution;

        /// <summary>
        /// Hands over the finished walk and clears it, so one Movement action's consequences are run
        /// exactly once. Milestone 8.61e.
        /// </summary>
        internal IReadOnlyList<HexCoordinate> TakeCompletedWalk(out HeroId mover)
        {
            var route = completedWalkRoute;
            mover = completedWalkHeroId;
            completedWalkRoute = null;
            completedWalkHeroId = default;
            return route;
        }

        private void RecordCompletedWalk(MovementSession movementSession)
        {
            // Milestone 8.114b1. Every completed walk is handed over, a walk of one space or none
            // included: p.18's token effects read only its middle (empty for these), and the board's
            // record of the last completed walk must not keep an earlier action's route.
            completedWalkRoute = movementSession.Route;
            completedWalkHeroId = movementSession.CurrentOccupancy.HeroId;
        }

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
            RuleModificationSet modifications,
            bool authorizedTextGrant = false)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authoritativeOffers = authorizedTextGrant
                ? new[] { offer }
                : ActionOfferQuery.GetAvailableTurnActionOffers(
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

        /// <summary>
        /// Milestone 8.23. Starts the second instance of a Movement a permanent after-action ability
        /// asked to repeat - Tigerclaw's Cloak and Daggers over a Movement performed on a basic card.
        ///
        /// <para>It takes the identities rather than an offer for the reason
        /// <see cref="SkillCardActionStrategyBackend.StartRepeatedSkill"/> does: a repeated action is
        /// created by a rule and is never offered, so the offer gate that admits only the two printed
        /// invocation contexts cannot be the way in. Everything past that gate is the ordinary start -
        /// the same Movement Total composed from the same card, the same item bonus, the same declared
        /// caps and the same start restrictions, read from the board as it now stands, which is
        /// rulebook p.10's "the entire action again (with the same range, restrictions etc.)".</para>
        /// </summary>
        public CardActionResolutionResult StartRepeatedMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            if (offer.InvocationContext != ActionInvocationContext.Repeated)
            {
                throw new ArgumentException(
                    "A repeated Movement runs under the repeated invocation context.", nameof(offer));
            }

            return StartMovementResolution(
                resolutionId,
                offer,
                definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions)),
                instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances)),
                authoritativeOffers: null,
                modifications);
        }

        /// <summary>
        /// Milestone 8.25e. Whether a repeated Movement could start from the board as it now stands,
        /// without starting one.
        ///
        /// <para>Rulebook p.10's repeat is an offer, so the ability that would make it has to know
        /// whether there is an instance to offer, and the commonest reason there is not is a live
        /// restriction: p.11 says effects which prevent you from performing a Movement action are
        /// checked at the start of the movement action, and a second instance is a second start. The
        /// gate is <see cref="StartMovementResolution"/>'s own, read here without committing the
        /// session it would build.</para>
        /// </summary>
        public bool CanStartRepeatedMovement(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            if (pendingResolution != null || definitions == null || instances == null ||
                !offer.IsAvailable)
            {
                return false;
            }

            var materializedDefinitions = definitions.ToArray();
            var actionDefinition = ActionOfferQuery.FindActionDefinition(
                offer, materializedDefinitions);
            if (actionDefinition.Kind != ActionKind.Movement ||
                !adjacentMoveAuthority.Occupancies.Any(
                    occupancy => occupancy.HeroId.Equals(offer.ActorId)))
            {
                return false;
            }

            return !MovementStartRestrictionFor(offer, materializedDefinitions, modifications)
                .PreventsAction;
        }

        /// <summary>
        /// The one restriction a Movement action starts under, composed from both families that can
        /// declare one. Milestone 8.25e extracted it so the start and
        /// <see cref="CanStartRepeatedMovement"/> ask the same question.
        ///
        /// <para>Milestone 7.2a. p.16's item bonus is composed onto the printed Movement before
        /// anything reads it, so the value the start restriction judges and the value the session
        /// spends are the one Movement Total this card has. Composing after the restriction would let a
        /// prevented action be re-enabled by an item, which is a different rule than the page states.
        /// Milestone 8.19c moved the composition into <c>MovementValueQuery</c> when a source became a
        /// second contributor to it; the ordering this describes is stated there and is
        /// unchanged.</para>
        ///
        /// <para>Milestone 8.20. Two families can now answer this: the composed evaluator seam, and a
        /// declared <c>limit-activity@1</c> - Arien's Slippery Ground and Deluge, "cannot ... move more
        /// than 1 space with a movement action". They are combined into the one restriction the session
        /// is built from rather than consulted in turn, so nothing downstream has two answers to choose
        /// between. The cap is read after the Movement Total is composed, which is what makes it a cap
        /// rather than a contribution: p.16's item raises the total and the sentence still holds it to
        /// the number it names.</para>
        /// </summary>
        private MovementActionStartRestriction MovementStartRestrictionFor(
            ActionOffer offer,
            IReadOnlyList<CardDefinition> materializedDefinitions,
            RuleModificationSet modifications) =>
            MovementActionStartRestriction.Tightest(
                actionStartRestrictions.Evaluate(new MovementActionStartContext(
                    offer.ActorId,
                    offer.SourceCardInstanceId,
                    offer.ActionDefinitionId,
                    offer.InvocationContext,
                    MovementValueQuery.Calculate(
                        offer, materializedDefinitions, heroItems, modifications).Total)),
                modifications != null &&
                modifications.TryGetActivityLimit(
                    RuleObject.ForHero(offer.ActorId), RuleActivity.Movement, out var declaredMaximum)
                    ? MovementActionStartRestriction.LimitTo(declaredMaximum)
                    : MovementActionStartRestriction.None);

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

            // Milestone 8.23. A null offer list is a repeated instance, which p.10 creates by rule and
            // never offers; the two printed contexts still come through the offers that produced them.
            // Milestone 8.35z50 admits `TextGranted`, the stamp a borrowed primary carries, so that
            // borrowing a card whose printed primary is a Movement performs that Movement rather than
            // being refused at the door. It is admitted BESIDE the two printed contexts rather than
            // through the repeated-instance arm above, because a borrow arrives with a real offer -
            // one naming the borrowed card - where a p.10 repeat arrives with none.
            var isRepeatedInstance = authoritativeOffers == null;
            if (!isRepeatedInstance &&
                offer.InvocationContext != ActionInvocationContext.PrintedPrimary &&
                offer.InvocationContext != ActionInvocationContext.PrintedSecondary &&
                offer.InvocationContext != ActionInvocationContext.TextGranted &&
                offer.InvocationContext != ActionInvocationContext.TextGrantedSecondary)
            {
                return CardActionResolutionResult.Reject(
                    CardActionResolutionRejectionReason.UnsupportedInvocationContext,
                    pendingResolution,
                    pendingChoice);
            }

            if (!offer.IsAvailable || (!isRepeatedInstance && !authoritativeOffers.Contains(offer)))
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

            var movementValue = MovementValueQuery.Calculate(
                offer, materializedDefinitions, heroItems, modifications);
            var restriction = MovementStartRestrictionFor(
                offer, materializedDefinitions, modifications);
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
                routeSpaces, modifications, out var committedStepStates,
                CrossesInAStraightLine(movementSession, selectedDestination, modifications));
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
                RecordCompletedWalk(movementSession);
                var completedResolution = CreatePendingResolution(stepResolution, movementSession.CaptureState());
                resolutionEvents.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.MovementActionCompleted(completedResolution)));
                resolutionEvents.Add(ResolutionStep.AtActionEnded(
                    completedResolution.SourceCardInstanceId,
                    completedResolution.ActionDefinitionId,
                    completedResolution.ActorId,
                    ActionKind.Movement, completedResolution.InvocationContext));
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
                RecordCompletedWalk(movementSession);
                var completedResolution = CreatePendingResolution(nextResolution, movementSession.CaptureState());
                resolutionEvents.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.MovementActionCompleted(completedResolution)));
                resolutionEvents.Add(ResolutionStep.AtActionEnded(
                    completedResolution.SourceCardInstanceId,
                    completedResolution.ActionDefinitionId,
                    completedResolution.ActorId,
                    ActionKind.Movement, completedResolution.InvocationContext));
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
            RecordCompletedWalk(movementSession);
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
                        ActionKind.Movement, completedResolution.InvocationContext)
                });
        }

        private PendingMovementChoice CreatePendingChoice(
            PendingCardActionResolution resolution,
            MovementSession movementSession,
            RuleModificationSet modifications)
        {
            var currentOccupancy = movementSession.CurrentOccupancy;
            // Milestone 9.30d. Expeditious Retreat's "Move only in a straight line" narrows what the walk
            // offers to its own file, after Trample's permission has widened it - so the offered choice
            // and the answer validated against it (AnswerMovement re-derives this list) read one set.
            var legalDestinations = StraightLineCrossingQuery.Narrow(
                StraightLineCrossingQuery.Widen(
                    MovementReachabilityQuery.FindReachableDestinations(
                        currentOccupancy,
                        adjacentMoveAuthority,
                        movementSession.RemainingAllowance,
                        modifications),
                    movementSession.Route,
                    currentOccupancy.HeroId,
                    adjacentMoveAuthority,
                    movementSession.RemainingAllowance,
                    modifications),
                movementSession.Route,
                currentOccupancy.HeroId,
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

        /// <summary>
        /// Milestone 8.114b1. Whether this destination's route continues the walk along one straight
        /// line under the Trample permission - the one case its transit may pass through obstacles.
        /// </summary>
        private static bool CrossesInAStraightLine(
            MovementSession session,
            ReachableMovementDestination destination,
            RuleModificationSet modifications) =>
            StraightLineCrossingQuery.Permits(session.CurrentOccupancy.HeroId, modifications) &&
            StraightLineCrossingQuery.IsStraightWalk(session.Route, destination.Route);

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
                destination.Route.Skip(1).ToArray(), modifications, out _,
                CrossesInAStraightLine(validationSession, destination, modifications));
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
