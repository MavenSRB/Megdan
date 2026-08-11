using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    internal sealed class AttackDefenseCardActionStrategyBackend : IAttackDefenseCardActionStrategyBackend
    {
        private readonly AdjacentMoveAuthority adjacentMoveAuthority;
        private PendingAttackResolution pendingAttackResolution;
        private PendingAttackTargetChoice pendingAttackTargetChoice;
        private PendingDefenseResponseChoice pendingDefenseChoice;

        internal AttackDefenseCardActionStrategyBackend(AdjacentMoveAuthority adjacentMoveAuthority)
        {
            this.adjacentMoveAuthority = adjacentMoveAuthority ?? throw new ArgumentNullException(nameof(adjacentMoveAuthority));
        }

        internal PendingAttackResolution PendingResolution => pendingAttackResolution;

        internal PendingAttackTargetChoice PendingTargetChoice => pendingAttackTargetChoice;

        internal PendingDefenseResponseChoice PendingDefenseChoice => pendingDefenseChoice;

        internal bool HasPendingResolution => pendingAttackResolution != null;

        AttackActionResolutionResult IAttackDefenseCardActionStrategyBackend.StartAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authoritativeOffers = ActionOfferQuery.GetAvailableOffers(
                materializedDefinitions,
                materializedInstances,
                offer.ActorId,
                ActionOfferWindow.NormalPlay);
            return StartAttackResolution(
                resolutionId,
                offer,
                materializedDefinitions,
                materializedInstances,
                participants,
                authoritativeOffers);
        }


        AttackActionResolutionResult IAttackDefenseCardActionStrategyBackend.StartTurnAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var authoritativeOffers = ActionOfferQuery.GetAvailableTurnActionOffers(
                materializedDefinitions,
                materializedInstances,
                offer.ActorId,
                offer.SourceCardInstanceId);
            return StartAttackResolution(
                resolutionId,
                offer,
                materializedDefinitions,
                materializedInstances,
                participants,
                authoritativeOffers);
        }

        private AttackActionResolutionResult StartAttackResolution(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IReadOnlyList<CardDefinition> materializedDefinitions,
            IReadOnlyList<CardInstance> materializedInstances,
            MatchParticipants participants,
            IReadOnlyList<ActionOffer> authoritativeOffers)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Starting an Attack resolution requires a resolution ID.", nameof(resolutionId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (pendingAttackResolution != null)
            {
                return RejectAttack(CardActionResolutionRejectionReason.ResolutionAlreadyPending);
            }

            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary &&
                offer.InvocationContext != ActionInvocationContext.PrintedSecondary)
            {
                return RejectAttack(CardActionResolutionRejectionReason.UnsupportedInvocationContext);
            }

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


        AttackActionResolutionResult IAttackDefenseCardActionStrategyBackend.AnswerAttackTarget(
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


        AttackActionResolutionResult IAttackDefenseCardActionStrategyBackend.AnswerDefense(
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


        AttackActionResolutionResult IAttackDefenseCardActionStrategyBackend.ResolveOutcome(
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


        internal static AttackDefenseCardActionStrategyBackend RestoreAttack(
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
            var backend = new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority);
            var offer = FindPendingAttackOffer(pendingResolution, materializedDefinitions, materializedInstances);
            if (!offer.HasValue)
            {
                throw new ArgumentException("Pending Attack resolution no longer references an available offer.", nameof(pendingResolution));
            }

            if (pendingResolution.Phase == AttackResolutionPhase.ChooseTarget)
            {
                var expectedChoice = backend.CreateAttackTargetChoice(
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
                var legalTargets = backend.FindLegalAttackTargets(
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

            backend.pendingAttackResolution = pendingResolution;
            backend.pendingAttackTargetChoice = pendingTargetChoice;
            backend.pendingDefenseChoice = pendingDefenseChoice;
            return backend;
        }

        internal static AttackDefenseCardActionStrategyBackend RestoreAttack(
            AdjacentMoveAuthority adjacentMoveAuthority,
            PendingAttackResolution pendingResolution,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var backend = new AttackDefenseCardActionStrategyBackend(adjacentMoveAuthority);
            var offer = FindPendingAttackOffer(pendingResolution, materializedDefinitions, materializedInstances);
            if (!offer.HasValue)
            {
                throw new ArgumentException("Pending Attack resolution no longer references an available offer.", nameof(pendingResolution));
            }

            PendingAttackTargetChoice targetChoice = null;
            PendingDefenseResponseChoice defenseChoice = null;
            if (pendingResolution.Phase == AttackResolutionPhase.ChooseTarget)
            {
                targetChoice = backend.CreateAttackTargetChoice(
                    pendingResolution, offer.Value, materializedDefinitions, participants);
            }
            else if (pendingResolution.Phase == AttackResolutionPhase.DefenseResponse)
            {
                var defenseOffers = ActionOfferQuery.GetAvailableOffers(
                        materializedDefinitions,
                        materializedInstances,
                        pendingResolution.TargetHeroId,
                        ActionOfferWindow.Reaction)
                    .Where(candidate => FindActionDefinition(candidate, materializedDefinitions).Kind == ActionKind.Defense)
                    .ToArray();
                defenseChoice = new PendingDefenseResponseChoice(
                    pendingResolution.ResolutionId,
                    pendingResolution.CurrentStepId,
                    pendingResolution.TargetHeroId,
                    pendingResolution.ActorId,
                    pendingResolution.AttackTotal,
                    defenseOffers,
                    true);
            }

            return RestoreAttack(
                adjacentMoveAuthority,
                pendingResolution,
                targetChoice,
                defenseChoice,
                materializedDefinitions,
                materializedInstances,
                participants);
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
            var sourceCard = instances.SingleOrDefault(candidate =>
                candidate.Id.Equals(resolution.SourceCardInstanceId));
            var offers = sourceCard != null &&
                sourceCard.State.Location == CardLocation.TurnArea &&
                sourceCard.State.HasBeenPlayed &&
                sourceCard.State.ResolutionStatus == CardResolutionStatus.Unresolved &&
                sourceCard.State.FaceOrientation == CardFaceOrientation.FaceUp
                    ? ActionOfferQuery.GetAvailableTurnActionOffers(
                        definitions,
                        instances,
                        resolution.ActorId,
                        resolution.SourceCardInstanceId)
                    : ActionOfferQuery.GetAvailableOffers(
                        definitions,
                        instances,
                        resolution.ActorId,
                        ActionOfferWindow.NormalPlay);
            return offers
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
