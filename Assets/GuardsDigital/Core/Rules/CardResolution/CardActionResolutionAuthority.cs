using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public sealed class CardActionResolutionAuthority
    {
        private readonly AdjacentMoveAuthority adjacentMoveAuthority;
        private PendingCardActionResolution pendingResolution;
        private PendingMovementChoice pendingChoice;
        private PendingAttackResolution pendingAttackResolution;
        private PendingAttackTargetChoice pendingAttackTargetChoice;
        private PendingDefenseResponseChoice pendingDefenseChoice;

        public CardActionResolutionAuthority(AdjacentMoveAuthority adjacentMoveAuthority)
        {
            this.adjacentMoveAuthority = adjacentMoveAuthority ?? throw new ArgumentNullException(nameof(adjacentMoveAuthority));
        }

        public PendingCardActionResolution PendingResolution => pendingResolution;

        public PendingMovementChoice PendingChoice => pendingChoice;

        public PendingAttackResolution PendingAttackResolution => pendingAttackResolution;

        public PendingAttackTargetChoice PendingAttackTargetChoice => pendingAttackTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice => pendingDefenseChoice;

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

            if (pendingResolution != null || pendingAttackResolution != null)
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

            var resolutionEvents = new List<CardActionResolutionEvent>();
            var stepResolution = pendingResolution;
            for (var routeIndex = 1; routeIndex < selectedDestination.Route.Count; routeIndex++)
            {
                var stepResult = movementSession.TryCommitStep(selectedDestination.Route[routeIndex]);
                if (!stepResult.Accepted)
                {
                    throw new InvalidOperationException(
                        "An authority-projected movement route failed after side-effect-free validation.");
                }

                resolutionEvents.Add(CardActionResolutionEvent.MovementStepCommitted(
                    stepResolution,
                    stepResult.NewEvents.Single()));
                stepResolution = CreatePendingResolution(stepResolution, movementSession.CaptureState());
            }

            if (movementSession.IsComplete)
            {
                movementSession.Complete();
                var completedResolution = CreatePendingResolution(stepResolution, movementSession.CaptureState());
                resolutionEvents.Add(CardActionResolutionEvent.MovementActionCompleted(completedResolution));
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
                resolutionEvents.Add(CardActionResolutionEvent.MovementActionCompleted(completedResolution));
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
            resolutionEvents.Add(CardActionResolutionEvent.MovementChoiceRequested(nextResolution));

            return CardActionResolutionResult.Accept(
                false,
                pendingResolution,
                pendingChoice,
                resolutionEvents);
        }

        public AttackActionResolutionResult StartAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Starting an Attack resolution requires a resolution ID.", nameof(resolutionId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (pendingResolution != null || pendingAttackResolution != null)
            {
                return RejectAttack(CardActionResolutionRejectionReason.ResolutionAlreadyPending);
            }

            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary &&
                offer.InvocationContext != ActionInvocationContext.PrintedSecondary)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnsupportedInvocationContext);
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
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var actionDefinition = FindActionDefinition(offer, materializedDefinitions);
            if (actionDefinition.Kind != ActionKind.Attack)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnsupportedActionKind);
            }

            if (offer.AttackRange != AttackRangeKind.NonRanged &&
                offer.AttackRange != AttackRangeKind.Ranged)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnsupportedAttackRange);
            }

            try
            {
                participants.GetHero(offer.ActorId);
            }
            catch (KeyNotFoundException)
            {
                return RejectAttack(CardActionResolutionRejectionReason.ActorUnavailable);
            }

            if (!adjacentMoveAuthority.Occupancies.Any(occupancy => occupancy.HeroId.Equals(offer.ActorId)))
            {
                return RejectAttack(CardActionResolutionRejectionReason.ActorUnavailable);
            }

            pendingAttackResolution = new PendingAttackResolution(
                resolutionId,
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                offer.ActorId,
                offer.InvocationContext,
                new ActionResolutionStepId("attack-target"),
                AttackResolutionPhase.ChooseTarget,
                default,
                null);
            pendingAttackTargetChoice = CreateAttackTargetChoice(
                pendingAttackResolution,
                offer,
                materializedDefinitions,
                participants);
            pendingDefenseChoice = null;

            if (pendingAttackTargetChoice.LegalTargetHeroIds.Count == 0)
            {
                pendingAttackResolution = null;
                pendingAttackTargetChoice = null;
                return RejectAttack(CardActionResolutionRejectionReason.TargetUnavailable);
            }

            return AttackActionResolutionResult.Accept(
                pendingAttackResolution,
                pendingAttackTargetChoice,
                null,
                new[] { CardActionResolutionEvent.AttackTargetChoiceRequested(pendingAttackResolution) });
        }

        public AttackActionResolutionResult AnswerAttackTarget(
            AttackTargetChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (pendingAttackResolution == null || pendingAttackTargetChoice == null ||
                pendingAttackResolution.Phase != AttackResolutionPhase.ChooseTarget)
            {
                return RejectAttack(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            if (!command.ResolutionId.Equals(pendingAttackTargetChoice.ResolutionId) ||
                !command.ChoiceId.Equals(pendingAttackTargetChoice.ChoiceId))
            {
                return RejectAttack(CardActionResolutionRejectionReason.StaleChoice);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var offer = FindPendingAttackOffer(
                pendingAttackResolution,
                materializedDefinitions,
                materializedInstances);
            if (!offer.HasValue)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var authoritativeChoice = CreateAttackTargetChoice(
                pendingAttackResolution,
                offer.Value,
                materializedDefinitions,
                participants);
            if (!AttackTargetChoicesMatch(authoritativeChoice, pendingAttackTargetChoice))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            if (!authoritativeChoice.LegalTargetHeroIds.Contains(command.TargetHeroId))
            {
                return RejectAttack(CardActionResolutionRejectionReason.TargetUnavailable);
            }

            var attackValue = PrintedValueQuery.CalculatePrintedValue(
                offer.Value,
                materializedDefinitions,
                PrintedValueChannel.Attack);
            var defenseResolution = new PendingAttackResolution(
                pendingAttackResolution.ResolutionId,
                pendingAttackResolution.SourceCardInstanceId,
                pendingAttackResolution.SourceDefinitionReference,
                pendingAttackResolution.ActionDefinitionId,
                pendingAttackResolution.ActorId,
                pendingAttackResolution.InvocationContext,
                new ActionResolutionStepId("defense-response"),
                AttackResolutionPhase.DefenseResponse,
                command.TargetHeroId,
                attackValue);
            var defenseOffers = ActionOfferQuery.GetAvailableOffers(
                materializedDefinitions,
                materializedInstances,
                command.TargetHeroId,
                ActionOfferWindow.Reaction)
                .Where(candidate => FindActionDefinition(candidate, materializedDefinitions).Kind == ActionKind.Defense)
                .ToArray();
            var defenseChoice = new PendingDefenseResponseChoice(
                defenseResolution.ResolutionId,
                defenseResolution.CurrentStepId,
                command.TargetHeroId,
                defenseResolution.ActorId,
                defenseResolution.AttackTotal,
                defenseOffers,
                true);

            pendingAttackResolution = defenseResolution;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = defenseChoice;

            return AttackActionResolutionResult.Accept(
                pendingAttackResolution,
                null,
                pendingDefenseChoice,
                new[]
                {
                    CardActionResolutionEvent.AttackStarted(pendingAttackResolution),
                    CardActionResolutionEvent.AttackTotalCalculated(pendingAttackResolution),
                    CardActionResolutionEvent.DefenseResponseChoiceRequested(pendingAttackResolution)
                });
        }

        public AttackActionResolutionResult AnswerDefenseResponse(
            DefenseResponseChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (pendingAttackResolution == null || pendingDefenseChoice == null ||
                pendingAttackResolution.Phase != AttackResolutionPhase.DefenseResponse)
            {
                return RejectAttack(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            if (!command.ResolutionId.Equals(pendingDefenseChoice.ResolutionId) ||
                !command.ChoiceId.Equals(pendingDefenseChoice.ChoiceId))
            {
                return RejectAttack(CardActionResolutionRejectionReason.StaleChoice);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var attackOffer = FindPendingAttackOffer(
                pendingAttackResolution,
                materializedDefinitions,
                materializedInstances);
            if (!attackOffer.HasValue)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var legalTargets = FindLegalAttackTargets(
                pendingAttackResolution.ActorId,
                attackOffer.Value,
                materializedDefinitions,
                participants);
            var expectedAttackValue = PrintedValueQuery.CalculatePrintedValue(
                attackOffer.Value,
                materializedDefinitions,
                PrintedValueChannel.Attack);
            if (!legalTargets.Contains(pendingAttackResolution.TargetHeroId) ||
                !PrintedValuesMatch(pendingAttackResolution.AttackValue, expectedAttackValue))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var currentDefenseOffers = ActionOfferQuery.GetAvailableOffers(
                    materializedDefinitions,
                    materializedInstances,
                    pendingAttackResolution.TargetHeroId,
                    ActionOfferWindow.Reaction)
                .Where(candidate => FindActionDefinition(candidate, materializedDefinitions).Kind == ActionKind.Defense)
                .ToArray();
            if (!DefenseChoicesMatch(pendingAttackResolution, currentDefenseOffers, pendingDefenseChoice))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            if (command.ChoiceKind == DefenseResponseChoiceKind.Decline)
            {
                if (!pendingDefenseChoice.CanDecline || command.SelectedCardInstanceIds.Count != 0)
                {
                    return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
                }

                var resumedWithoutDefense = CreateOutcomeComparisonResolution(null, null);
                pendingAttackResolution = resumedWithoutDefense;
                pendingDefenseChoice = null;
                return AttackActionResolutionResult.Accept(
                    pendingAttackResolution,
                    null,
                    null,
                    new[]
                    {
                        CardActionResolutionEvent.DefenseDeclined(pendingAttackResolution),
                        CardActionResolutionEvent.DefenseResponseCompleted(pendingAttackResolution)
                    });
            }

            if (command.ChoiceKind != DefenseResponseChoiceKind.SelectCards ||
                command.SelectedCardInstanceIds.Count != 1 ||
                !command.SelectedCardInstanceIds[0].IsDefined)
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var selectedCardId = command.SelectedCardInstanceIds[0];
            var matchingOffers = currentDefenseOffers
                .Where(candidate => candidate.SourceCardInstanceId.Equals(selectedCardId))
                .ToArray();
            var matchingInstances = materializedInstances
                .Where(candidate => candidate != null && candidate.Id.Equals(selectedCardId))
                .ToArray();
            if (matchingOffers.Length != 1 || matchingInstances.Length != 1 ||
                selectedCardId.Equals(pendingAttackResolution.SourceCardInstanceId))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var defenseOffer = matchingOffers[0];
            var defenseCard = matchingInstances[0];
            var transition = defenseOffer.SourceCardStateTransition;
            if (!defenseCard.OwnerId.Equals(pendingAttackResolution.TargetHeroId) ||
                !defenseCard.DefinitionReference.Equals(defenseOffer.SourceDefinitionReference) ||
                defenseCard.State.Location != transition.RequiredLocation ||
                transition.RequiredLocation != CardLocation.Hand ||
                transition.DestinationLocation != CardLocation.DiscardPile)
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var defenseValue = PrintedValueQuery.CalculatePrintedValue(
                defenseOffer,
                materializedDefinitions,
                PrintedValueChannel.Defense);
            var resumedWithDefense = CreateOutcomeComparisonResolution(defenseOffer, defenseValue);

            if (!defenseCard.TryApplyLocationTransition(transition))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            pendingAttackResolution = resumedWithDefense;
            pendingDefenseChoice = null;
            return AttackActionResolutionResult.Accept(
                pendingAttackResolution,
                null,
                null,
                new[]
                {
                    CardActionResolutionEvent.DefenseCardDiscarded(pendingAttackResolution),
                    CardActionResolutionEvent.DefenseTotalCalculated(pendingAttackResolution),
                    CardActionResolutionEvent.DefenseResponseCompleted(pendingAttackResolution)
                });
        }

        public AttackActionResolutionResult ResolveAttackOutcome(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (pendingAttackResolution == null ||
                pendingAttackResolution.Phase != AttackResolutionPhase.CompareOutcome ||
                pendingAttackTargetChoice != null ||
                pendingDefenseChoice != null)
            {
                return RejectAttack(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var attackOffer = FindPendingAttackOffer(
                pendingAttackResolution,
                materializedDefinitions,
                materializedInstances);
            if (!attackOffer.HasValue)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var legalTargets = FindLegalAttackTargets(
                pendingAttackResolution.ActorId,
                attackOffer.Value,
                materializedDefinitions,
                participants);
            var expectedAttackValue = PrintedValueQuery.CalculatePrintedValue(
                attackOffer.Value,
                materializedDefinitions,
                PrintedValueChannel.Attack);
            if (!pendingAttackResolution.CurrentStepId.Equals(
                    new ActionResolutionStepId("attack-outcome-comparison")) ||
                !legalTargets.Contains(pendingAttackResolution.TargetHeroId) ||
                !PrintedValuesMatch(pendingAttackResolution.AttackValue, expectedAttackValue) ||
                !CompletedDefenseMatches(
                    pendingAttackResolution,
                    materializedDefinitions,
                    materializedInstances))
            {
                return RejectAttack(CardActionResolutionRejectionReason.InvalidChoice);
            }

            var completedResolution = pendingAttackResolution;
            var outcome = completedResolution.HasDefenseTotal &&
                completedResolution.DefenseTotal.Value >= completedResolution.AttackTotal
                    ? AttackResolutionOutcome.SuccessfullyDefended
                    : AttackResolutionOutcome.AttackSuccessful;
            var events = new[]
            {
                CardActionResolutionEvent.AttackOutcomeDetermined(completedResolution, outcome),
                CardActionResolutionEvent.AttackResolutionCompleted(completedResolution, outcome)
            };

            pendingAttackResolution = null;
            pendingAttackTargetChoice = null;
            pendingDefenseChoice = null;
            return AttackActionResolutionResult.Accept(
                null,
                null,
                null,
                events,
                outcome);
        }

        public static CardActionResolutionAuthority RestoreAttack(
            AdjacentMoveAuthority adjacentMoveAuthority,
            PendingAttackResolution pendingResolution,
            PendingAttackTargetChoice pendingTargetChoice,
            PendingDefenseResponseChoice pendingDefenseChoice,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (pendingResolution == null)
            {
                throw new ArgumentNullException(nameof(pendingResolution));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authority = new CardActionResolutionAuthority(adjacentMoveAuthority);
            var offer = FindPendingAttackOffer(pendingResolution, materializedDefinitions, materializedInstances);
            if (!offer.HasValue)
            {
                throw new ArgumentException("Pending Attack resolution no longer references an available offer.", nameof(pendingResolution));
            }

            if (pendingResolution.Phase == AttackResolutionPhase.ChooseTarget)
            {
                var expectedChoice = authority.CreateAttackTargetChoice(
                    pendingResolution,
                    offer.Value,
                    materializedDefinitions,
                    participants);
                if (pendingTargetChoice == null || pendingDefenseChoice != null ||
                    !AttackTargetChoicesMatch(expectedChoice, pendingTargetChoice))
                {
                    throw new ArgumentException("Pending Attack target choice does not match authoritative state.", nameof(pendingTargetChoice));
                }
            }
            else
            {
                var legalTargets = authority.FindLegalAttackTargets(
                    pendingResolution.ActorId,
                    offer.Value,
                    materializedDefinitions,
                    participants);
                if (!legalTargets.Contains(pendingResolution.TargetHeroId))
                {
                    throw new ArgumentException("Pending Attack target is no longer legal.", nameof(pendingResolution));
                }

                var expectedAttackValue = PrintedValueQuery.CalculatePrintedValue(
                    offer.Value,
                    materializedDefinitions,
                    PrintedValueChannel.Attack);
                if (pendingTargetChoice != null ||
                    !PrintedValuesMatch(pendingResolution.AttackValue, expectedAttackValue))
                {
                    throw new ArgumentException("Pending Attack continuation does not match authoritative state.", nameof(pendingResolution));
                }

                if (pendingResolution.Phase == AttackResolutionPhase.DefenseResponse)
                {
                    var expectedDefenseOffers = ActionOfferQuery.GetAvailableOffers(
                        materializedDefinitions,
                        materializedInstances,
                        pendingResolution.TargetHeroId,
                        ActionOfferWindow.Reaction)
                        .Where(candidate => FindActionDefinition(candidate, materializedDefinitions).Kind == ActionKind.Defense)
                        .ToArray();
                    if (pendingDefenseChoice == null ||
                        !DefenseChoicesMatch(pendingResolution, expectedDefenseOffers, pendingDefenseChoice))
                    {
                        throw new ArgumentException("Pending Defense response choice does not match authoritative Attack state.", nameof(pendingDefenseChoice));
                    }
                }
                else if (pendingResolution.Phase == AttackResolutionPhase.CompareOutcome)
                {
                    if (pendingDefenseChoice != null ||
                        !pendingResolution.CurrentStepId.Equals(new ActionResolutionStepId("attack-outcome-comparison")) ||
                        !CompletedDefenseMatches(pendingResolution, materializedDefinitions, materializedInstances))
                    {
                        throw new ArgumentException("Completed Defense response does not match authoritative Attack state.", nameof(pendingResolution));
                    }
                }
                else
                {
                    throw new ArgumentException("Pending Attack resolution has an unsupported phase.", nameof(pendingResolution));
                }
            }

            authority.pendingAttackResolution = pendingResolution;
            authority.pendingAttackTargetChoice = pendingTargetChoice;
            authority.pendingDefenseChoice = pendingDefenseChoice;
            return authority;
        }

        private AttackActionResolutionResult RejectAttack(CardActionResolutionRejectionReason reason)
        {
            return AttackActionResolutionResult.Reject(
                reason,
                pendingAttackResolution,
                pendingAttackTargetChoice,
                pendingDefenseChoice);
        }

        private PendingAttackResolution CreateOutcomeComparisonResolution(
            ActionOffer? defenseOffer,
            PrintedValueResult defenseValue)
        {
            return new PendingAttackResolution(
                pendingAttackResolution.ResolutionId,
                pendingAttackResolution.SourceCardInstanceId,
                pendingAttackResolution.SourceDefinitionReference,
                pendingAttackResolution.ActionDefinitionId,
                pendingAttackResolution.ActorId,
                pendingAttackResolution.InvocationContext,
                new ActionResolutionStepId("attack-outcome-comparison"),
                AttackResolutionPhase.CompareOutcome,
                pendingAttackResolution.TargetHeroId,
                pendingAttackResolution.AttackValue,
                defenseOffer,
                defenseValue);
        }

        private PendingAttackTargetChoice CreateAttackTargetChoice(
            PendingAttackResolution resolution,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants)
        {
            return new PendingAttackTargetChoice(
                resolution.ResolutionId,
                resolution.CurrentStepId,
                resolution.ActorId,
                FindLegalAttackTargets(
                    resolution.ActorId,
                    offer,
                    definitions,
                    participants));
        }

        private IReadOnlyList<HeroId> FindLegalAttackTargets(
            HeroId attackerId,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions,
            MatchParticipants participants)
        {
            var rangeTotal = offer.AttackRange == AttackRangeKind.Ranged
                ? PrintedValueQuery.CalculatePrintedValue(
                    offer,
                    definitions,
                    PrintedValueChannel.Range).Total
                : 1;
            return AttackTargetQuery.FindLegalEnemyHeroes(
                attackerId,
                offer.AttackRange,
                rangeTotal,
                participants,
                adjacentMoveAuthority.Occupancies);
        }

        private static ActionOffer? FindPendingAttackOffer(
            PendingAttackResolution resolution,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            return ActionOfferQuery.GetAvailableOffers(
                    definitions,
                    instances,
                    resolution.ActorId,
                    ActionOfferWindow.NormalPlay)
                .Cast<ActionOffer?>()
                .SingleOrDefault(candidate =>
                    candidate.Value.SourceCardInstanceId.Equals(resolution.SourceCardInstanceId) &&
                    candidate.Value.SourceDefinitionReference.Equals(resolution.SourceDefinitionReference) &&
                    candidate.Value.ActionDefinitionId.Equals(resolution.ActionDefinitionId) &&
                    candidate.Value.InvocationContext == resolution.InvocationContext);
        }

        private static bool AttackTargetChoicesMatch(
            PendingAttackTargetChoice expected,
            PendingAttackTargetChoice actual)
        {
            return expected.ResolutionId.Equals(actual.ResolutionId) &&
                expected.ChoiceId.Equals(actual.ChoiceId) &&
                expected.ChooserId.Equals(actual.ChooserId) &&
                expected.LegalTargetHeroIds.SequenceEqual(actual.LegalTargetHeroIds);
        }

        private static bool DefenseChoicesMatch(
            PendingAttackResolution resolution,
            IReadOnlyList<ActionOffer> expectedOffers,
            PendingDefenseResponseChoice actual)
        {
            return actual.ResolutionId.Equals(resolution.ResolutionId) &&
                actual.ChoiceId.Equals(resolution.CurrentStepId) &&
                actual.ChooserId.Equals(resolution.TargetHeroId) &&
                actual.AttackerId.Equals(resolution.ActorId) &&
                actual.AttackTotal == resolution.AttackTotal &&
                actual.CanDecline &&
                actual.LegalDefenseOffers.SequenceEqual(expectedOffers);
        }

        private static bool PrintedValuesMatch(PrintedValueResult expected, PrintedValueResult actual)
        {
            if (expected == null || actual == null ||
                expected.Channel != actual.Channel ||
                expected.Total != actual.Total ||
                expected.Contributions.Count != actual.Contributions.Count)
            {
                return false;
            }

            for (var index = 0; index < expected.Contributions.Count; index++)
            {
                if (!expected.Contributions[index].Equals(actual.Contributions[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompletedDefenseMatches(
            PendingAttackResolution resolution,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            if (!resolution.DefenseOffer.HasValue)
            {
                return resolution.DefenseValue == null;
            }

            var offer = resolution.DefenseOffer.Value;
            var matchingInstances = instances
                .Where(candidate => candidate != null && candidate.Id.Equals(offer.SourceCardInstanceId))
                .ToArray();
            if (matchingInstances.Length != 1)
            {
                return false;
            }

            var instance = matchingInstances[0];
            var action = FindActionDefinition(offer, definitions);
            if (!instance.OwnerId.Equals(resolution.TargetHeroId) ||
                !instance.DefinitionReference.Equals(offer.SourceDefinitionReference) ||
                instance.State.Location != CardLocation.DiscardPile ||
                action.Kind != ActionKind.Defense ||
                action.Origin != ActionDefinitionOrigin.Reaction ||
                !offer.SourceCardStateTransition.Equals(action.SourceCardStateTransition) ||
                offer.SourceCardStateTransition.RequiredLocation != CardLocation.Hand ||
                offer.SourceCardStateTransition.DestinationLocation != CardLocation.DiscardPile)
            {
                return false;
            }

            var expectedDefenseValue = PrintedValueQuery.CalculatePrintedValue(
                offer,
                definitions,
                PrintedValueChannel.Defense);
            return PrintedValuesMatch(resolution.DefenseValue, expectedDefenseValue);
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
            var validationAuthority = new AdjacentMoveAuthority(
                adjacentMoveAuthority.BoardCoordinates,
                adjacentMoveAuthority.Occupancies,
                adjacentMoveAuthority.ObstacleOccupancies);
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
