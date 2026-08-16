using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class CardActionResolutionAuthorityTests
    {
        [Test]
        public void Constructor_RejectsMissingMovementActionStartRestrictionEvaluator()
        {
            var actorId = new HeroId("primitive-hero");

            // Cast because the authority now also overloads on a marker register: the null under test
            // is specifically an absent movement-restriction evaluator.
            Assert.Throws<System.ArgumentNullException>(() => new CardActionResolutionAuthority(
                CreateMovementAuthority(actorId, new HexCoordinate(0, 0)),
                (IMovementActionStartRestrictionEvaluator)null));
        }

        [Test]
        public void StartMovementResolution_RecordsPendingResolutionAndChoiceAsPlainData()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(
                CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3),
                CreateMovementAction("secondary-move", ActionDefinitionOrigin.PrintedSecondary, 1));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                actorId);
            var offer = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId)
                .Single(candidate => candidate.InvocationContext == ActionInvocationContext.PrintedPrimary);
            var authority = CreateResolutionAuthority(actorId, new HexCoordinate(0, 0));
            var resolutionId = new ActionResolutionId("resolution-1");

            var result = authority.StartMovementResolution(resolutionId, offer, new[] { definition }, new[] { instance }, RuleModificationSet.Empty);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.PendingResolution.ResolutionId, Is.EqualTo(resolutionId));
            Assert.That(result.PendingResolution.SourceCardInstanceId, Is.EqualTo(instance.Id));
            Assert.That(result.PendingResolution.SourceDefinitionReference, Is.EqualTo(definition.Reference));
            Assert.That(result.PendingResolution.ActionDefinitionId, Is.EqualTo(new ActionDefinitionId("primary-move")));
            Assert.That(result.PendingResolution.ActorId, Is.EqualTo(actorId));
            Assert.That(result.PendingResolution.InvocationContext, Is.EqualTo(ActionInvocationContext.PrintedPrimary));
            Assert.That(result.PendingResolution.CurrentStepId, Is.EqualTo(new ActionResolutionStepId("movement-step-0")));
            Assert.That(result.PendingResolution.MovementAllowance, Is.EqualTo(3));
            Assert.That(result.PendingResolution.RemainingAllowance, Is.EqualTo(3));
            Assert.That(result.PendingChoice.ChoiceId, Is.EqualTo(result.PendingResolution.CurrentStepId));
            Assert.That(result.PendingChoice.ChooserId, Is.EqualTo(actorId));
            Assert.That(result.PendingChoice.MovingHeroId, Is.EqualTo(actorId));
            Assert.That(result.PendingChoice.Origin, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(result.PendingChoice.CanStop, Is.True);
            Assert.That(result.PendingChoice.LegalDestinations, Has.Count.EqualTo(18));
            Assert.That(
                result.PendingChoice.LegalDestinations.Select(candidate => candidate.Destination),
                Has.No.Member(new HexCoordinate(0, 0)));
            var farEast = result.PendingChoice.LegalDestinations
                .Single(candidate => candidate.Destination.Equals(new HexCoordinate(2, 0)));
            Assert.That(farEast.Cost, Is.EqualTo(2));
            Assert.That(farEast.Route, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 0),
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0)
            }));
            Assert.That(result.PendingChoice.LegalDestinations, Is.Not.InstanceOf<ReachableMovementDestination[]>());
            Assert.Throws<System.NotSupportedException>(() =>
                ((IList<ReachableMovementDestination>)result.PendingChoice.LegalDestinations)[0] = farEast);
            Assert.That(farEast.Route, Is.Not.InstanceOf<HexCoordinate[]>());
            Assert.Throws<System.NotSupportedException>(() =>
                ((IList<HexCoordinate>)farEast.Route)[0] = new HexCoordinate(99, 99));
            Assert.That(result.PendingResolution.MovementSessionState.Route, Is.EqualTo(new[] { new HexCoordinate(0, 0) }));
            Assert.That(result.PendingResolution.MovementSessionState.StepEvents, Is.Empty);
            Assert.That(result.Events.Select(resolutionEvent => resolutionEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MovementActionStarted,
                CardActionResolutionEventKind.MovementChoiceRequested
            }));
        }

        // ActionOfferQuery.FindActionDefinition is internal and there is no InternalsVisibleTo, so
        // milestone 6.1u's single shared lookup is exercised through a real caller. A card carrying
        // two actions of different kinds is the case a wrong lookup cannot survive: resolving the
        // offer's own action definition, rather than the definition's first one, is the whole job.
        [Test]
        public void StartMovementResolution_ResolvesTheOffersOwnActionDefinitionAmongSeveral()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(
                CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3),
                new ActionDefinition(
                    new ActionDefinitionId("secondary-attack"),
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 4),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 1)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true),
                    AttackRangeKind.Ranged,
                    default));
            var instance = CreateInstance(actorId, definition);
            var offers = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId);
            var authority = CreateResolutionAuthority(actorId, new HexCoordinate(0, 0));

            var movement = authority.StartMovementResolution(
                new ActionResolutionId("resolution-move"),
                offers.Single(candidate =>
                    candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-move"))),
                new[] { definition },
                new[] { instance }, RuleModificationSet.Empty);

            Assert.That(movement.Accepted, Is.True,
                "The Movement offer resolves to the Movement action definition.");
            Assert.That(movement.PendingResolution.MovementAllowance, Is.EqualTo(3),
                "The allowance comes from the resolved definition, not from the card.");

            var attackThroughMovement = CreateResolutionAuthority(actorId, new HexCoordinate(0, 0))
                .StartMovementResolution(
                    new ActionResolutionId("resolution-attack"),
                    offers.Single(candidate =>
                        candidate.ActionDefinitionId.Equals(new ActionDefinitionId("secondary-attack"))),
                    new[] { definition },
                    new[] { instance }, RuleModificationSet.Empty);

            Assert.That(attackThroughMovement.Accepted, Is.False);
            Assert.That(attackThroughMovement.RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.UnsupportedActionKind),
                "The same lookup reads the Attack action's own kind off the second definition.");
        }

        [Test]
        public void StartMovementResolution_UsesSelectedPrimaryOrSecondaryOfferAllowance()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(
                CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3),
                CreateMovementAction("secondary-move", ActionDefinitionOrigin.PrintedSecondary, 1));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                actorId);
            var secondaryOffer = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId)
                .Single(candidate => candidate.InvocationContext == ActionInvocationContext.PrintedSecondary);
            var authority = CreateResolutionAuthority(actorId, new HexCoordinate(0, 0));

            var result = authority.StartMovementResolution(
                new ActionResolutionId("resolution-1"),
                secondaryOffer,
                new[] { definition },
                new[] { instance }, RuleModificationSet.Empty);

            Assert.That(result.PendingResolution.InvocationContext, Is.EqualTo(ActionInvocationContext.PrintedSecondary));
            Assert.That(result.PendingResolution.ActionDefinitionId, Is.EqualTo(new ActionDefinitionId("secondary-move")));
            Assert.That(result.PendingResolution.MovementAllowance, Is.EqualTo(1));
            Assert.That(result.PendingResolution.RemainingAllowance, Is.EqualTo(1));
        }

        [Test]
        public void StartMovementResolution_RulebookPage11PreventRestrictionRejectsAtActionStart()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var definition = CreateDefinition(
                CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var instance = CreateInstance(actorId, definition);
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var restrictions = new MutableMovementActionStartRestrictionEvaluator(
                MovementActionStartRestriction.Prevent());
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority, restrictions);

            var result = authority.StartMovementResolution(
                new ActionResolutionId("prevented-resolution"),
                offer,
                new[] { definition },
                new[] { instance }, RuleModificationSet.Empty);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(
                CardActionResolutionRejectionReason.MovementActionPrevented));
            Assert.That(result.PendingResolution, Is.Null);
            Assert.That(result.PendingChoice, Is.Null);
            Assert.That(result.Events, Is.Empty);
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
            Assert.That(restrictions.EvaluationCount, Is.EqualTo(1));
            Assert.That(restrictions.LastContext.ActorId, Is.EqualTo(actorId));
            Assert.That(restrictions.LastContext.SourceCardInstanceId, Is.EqualTo(instance.Id));
            Assert.That(restrictions.LastContext.ActionDefinitionId, Is.EqualTo(offer.ActionDefinitionId));
            Assert.That(restrictions.LastContext.InvocationContext, Is.EqualTo(offer.InvocationContext));
            Assert.That(restrictions.LastContext.MovementAllowance, Is.EqualTo(3));
        }

        [Test]
        public void StartMovementResolution_RulebookPage11LimitIsCapturedOnceForAllWaypointChoices()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var firstDestination = origin.GetNeighbor(HexDirection.East);
            var definition = CreateDefinition(
                CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var instance = CreateInstance(actorId, definition);
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var restrictions = new MutableMovementActionStartRestrictionEvaluator(
                MovementActionStartRestriction.LimitTo(2));
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority, restrictions);

            var start = authority.StartMovementResolution(
                new ActionResolutionId("limited-resolution"),
                offer,
                new[] { definition },
                new[] { instance }, RuleModificationSet.Empty);
            restrictions.Restriction = MovementActionStartRestriction.Prevent();
            var waypoint = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                firstDestination), RuleModificationSet.Empty);

            Assert.That(start.Accepted, Is.True);
            Assert.That(start.PendingResolution.MovementAllowance, Is.EqualTo(2));
            Assert.That(start.PendingChoice.RemainingAllowance, Is.EqualTo(2));
            Assert.That(waypoint.Accepted, Is.True);
            Assert.That(waypoint.IsComplete, Is.False);
            Assert.That(waypoint.PendingResolution.MovementAllowance, Is.EqualTo(2));
            Assert.That(waypoint.PendingChoice.RemainingAllowance, Is.EqualTo(1));
            Assert.That(restrictions.EvaluationCount, Is.EqualTo(1),
                "Action-start restrictions must not be re-evaluated for segmented waypoint continuation.");
        }

        [Test]
        public void PendingMovementChoice_RejectsDuplicateUnaffordableOrWrongOriginRoutes()
        {
            var resolutionId = new ActionResolutionId("resolution-1");
            var choiceId = new ActionResolutionStepId("movement-step-0");
            var heroId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(1, 0);
            var candidate = new ReachableMovementDestination(
                destination,
                new[] { origin, destination },
                1);
            var wrongOriginCandidate = new ReachableMovementDestination(
                destination,
                new[] { new HexCoordinate(1, -1), destination },
                1);

            Assert.Throws<System.ArgumentException>(() => new PendingMovementChoice(
                resolutionId,
                choiceId,
                heroId,
                heroId,
                origin,
                1,
                true,
                new[] { candidate, candidate }));
            Assert.Throws<System.ArgumentException>(() => new PendingMovementChoice(
                resolutionId,
                choiceId,
                heroId,
                heroId,
                origin,
                0,
                true,
                new[] { candidate }));
            Assert.Throws<System.ArgumentException>(() => new PendingMovementChoice(
                resolutionId,
                choiceId,
                heroId,
                heroId,
                origin,
                1,
                true,
                new[] { wrongOriginCandidate }));
            Assert.Throws<System.ArgumentException>(() => new PendingMovementChoice(
                resolutionId,
                choiceId,
                heroId,
                heroId,
                origin,
                1,
                true,
                new[] { default(ReachableMovementDestination) }));
        }

        [Test]
        public void AnswerMovementChoice_SelectsPartialDestinationAndRequestsContinuationFromNewPosition()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var destination = origin.GetNeighbor(HexDirection.East);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 2));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition }, new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);

            var movement = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                destination), RuleModificationSet.Empty);

            Assert.That(movement.Accepted, Is.True);
            Assert.That(movement.IsComplete, Is.False);
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(destination));
            Assert.That(movement.PendingResolution, Is.Not.Null);
            Assert.That(movement.PendingResolution.ResolutionId, Is.EqualTo(start.PendingResolution.ResolutionId));
            Assert.That(movement.PendingResolution.CurrentStepId, Is.EqualTo(new ActionResolutionStepId("movement-step-1")));
            Assert.That(movement.PendingResolution.MovementSessionState.Route, Is.EqualTo(new[] { origin, destination }));
            Assert.That(movement.PendingChoice, Is.Not.Null);
            Assert.That(movement.PendingChoice.Origin, Is.EqualTo(destination));
            Assert.That(movement.PendingChoice.RemainingAllowance, Is.EqualTo(1));
            Assert.That(movement.Events.Select(resolutionEvent => resolutionEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MovementStepCommitted,
                CardActionResolutionEventKind.MovementChoiceRequested
            }));
            Assert.That(movement.Events[0].MovementEvent.Origin, Is.EqualTo(origin));
            Assert.That(movement.Events[0].MovementEvent.Destination, Is.EqualTo(destination));
            Assert.That(movement.Events[1].RemainingAllowance, Is.EqualTo(1));
            Assert.That(authority.PendingResolution, Is.SameAs(movement.PendingResolution));
            Assert.That(authority.PendingChoice, Is.SameAs(movement.PendingChoice));
        }

        [Test]
        public void AnswerMovementChoice_StopBeforeAllowanceIsSpent_CompletesLegally()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition }, new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);

            var stop = authority.AnswerMovementChoice(MovementChoiceCommand.Stop(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId), RuleModificationSet.Empty);

            Assert.That(stop.Accepted, Is.True);
            Assert.That(stop.IsComplete, Is.True);
            Assert.That(stop.PendingResolution, Is.Null);
            Assert.That(stop.PendingChoice, Is.Null);
            Assert.That(authority.PendingResolution, Is.Null);
            Assert.That(authority.PendingChoice, Is.Null);
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
            Assert.That(stop.Events.Select(resolutionEvent => resolutionEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MovementActionCompleted
            }));
            Assert.That(stop.Events[0].RemainingAllowance, Is.EqualTo(3));
        }

        [Test]
        public void AnswerMovementChoice_SpendingAllowanceCompletesAutomatically()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var destination = origin.GetNeighbor(HexDirection.East);
            var definition = CreateDefinition(CreateMovementAction("secondary-move", ActionDefinitionOrigin.PrintedSecondary, 1));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedSecondary);
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition }, new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);

            var step = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                destination), RuleModificationSet.Empty);

            Assert.That(step.Accepted, Is.True);
            Assert.That(step.IsComplete, Is.True);
            Assert.That(step.PendingResolution, Is.Null);
            Assert.That(step.PendingChoice, Is.Null);
            Assert.That(authority.PendingResolution, Is.Null);
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(destination));
            Assert.That(step.Events.Select(resolutionEvent => resolutionEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MovementStepCommitted,
                CardActionResolutionEventKind.MovementActionCompleted
            }));
            Assert.That(step.Events[1].RemainingAllowance, Is.EqualTo(0));
        }

        [Test]
        public void AnswerMovementChoice_InvalidOrStaleChoiceDoesNotMutateOrAdvance()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var firstDestination = origin.GetNeighbor(HexDirection.East);
            var invalidDestination = new HexCoordinate(3, 0);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 2));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition }, new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);

            var stale = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                new ActionResolutionStepId("stale-choice"),
                firstDestination), RuleModificationSet.Empty);
            var invalid = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                invalidDestination), RuleModificationSet.Empty);

            Assert.That(stale.Accepted, Is.False);
            Assert.That(stale.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.StaleChoice));
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
            Assert.That(authority.PendingChoice.ChoiceId, Is.EqualTo(start.PendingChoice.ChoiceId));

            var accepted = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                firstDestination), RuleModificationSet.Empty);
            var staleAfterContinuation = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                firstDestination), RuleModificationSet.Empty);

            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.IsComplete, Is.False);
            Assert.That(staleAfterContinuation.Accepted, Is.False);
            Assert.That(staleAfterContinuation.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.StaleChoice));
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(firstDestination));
            Assert.That(authority.PendingChoice, Is.SameAs(accepted.PendingChoice));
        }

        [Test]
        public void AnswerMovementChoice_WaypointsBuildOneCumulativeRouteUntilAllowanceIsSpent()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var east = origin.GetNeighbor(HexDirection.East);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition }, new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);

            var first = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                east), RuleModificationSet.Empty);
            var second = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                first.PendingChoice.ResolutionId,
                first.PendingChoice.ChoiceId,
                origin), RuleModificationSet.Empty);
            var third = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                second.PendingChoice.ResolutionId,
                second.PendingChoice.ChoiceId,
                east), RuleModificationSet.Empty);

            Assert.That(first.IsComplete, Is.False);
            Assert.That(second.IsComplete, Is.False);
            Assert.That(second.PendingResolution.MovementSessionState.Route, Is.EqualTo(new[] { origin, east, origin }));
            Assert.That(second.PendingChoice.Origin, Is.EqualTo(origin));
            Assert.That(second.PendingChoice.RemainingAllowance, Is.EqualTo(1));
            Assert.That(third.Accepted, Is.True);
            Assert.That(third.IsComplete, Is.True);
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(east));
            Assert.That(third.Events.Select(resolutionEvent => resolutionEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MovementStepCommitted,
                CardActionResolutionEventKind.MovementActionCompleted
            }));
        }

        [Test]
        public void AnswerMovementChoice_ObstacleDestinationIsNotALegalPendingDecision()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var blockedDestination = origin.GetNeighbor(HexDirection.East);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 2));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = new AdjacentMoveAuthority(
                CreateRadiusTwoCoordinates(),
                new[] { new HeroOccupancy(actorId, origin) },
                new[] { new ObstacleOccupancy(new ObstacleId("blocker"), blockedDestination) });
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition }, new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);

            var blockedStep = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                blockedDestination), RuleModificationSet.Empty);

            Assert.That(
                start.PendingChoice.LegalDestinations.Select(candidate => candidate.Destination),
                Has.No.Member(blockedDestination));
            Assert.That(blockedStep.Accepted, Is.False);
            Assert.That(blockedStep.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
        }

        [Test]
        public void AnswerMovementChoice_SelectsObstacleDetourAndEmitsEveryCommittedStepInOrder()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(2, 0);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = new AdjacentMoveAuthority(
                CreateRadiusTwoCoordinates(),
                new[] { new HeroOccupancy(actorId, origin) },
                new[]
                {
                    new ObstacleOccupancy(new ObstacleId("east-blocker"), new HexCoordinate(1, 0)),
                    new ObstacleOccupancy(new ObstacleId("south-east-blocker"), new HexCoordinate(0, 1))
                });
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(
                new ActionResolutionId("resolution-1"),
                offer,
                new[] { definition },
                new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);
            var offeredDestination = start.PendingChoice.LegalDestinations
                .Single(candidate => candidate.Destination.Equals(destination));

            var result = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                destination), RuleModificationSet.Empty);

            Assert.That(offeredDestination.Route, Is.EqualTo(new[]
            {
                origin,
                new HexCoordinate(1, -1),
                new HexCoordinate(2, -1),
                destination
            }));
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(destination));
            Assert.That(
                result.Events.Select(resolutionEvent => resolutionEvent.Kind),
                Is.EqualTo(new[]
                {
                    CardActionResolutionEventKind.MovementStepCommitted,
                    CardActionResolutionEventKind.MovementStepCommitted,
                    CardActionResolutionEventKind.MovementStepCommitted,
                    CardActionResolutionEventKind.MovementActionCompleted
                }));
            Assert.That(
                result.Events.Take(3).Select(resolutionEvent => resolutionEvent.MovementEvent.Destination),
                Is.EqualTo(offeredDestination.Route.Skip(1)));
            Assert.That(result.Events[3].RemainingAllowance, Is.EqualTo(0));
        }

        [Test]
        public void AnswerMovementChoice_WhenProjectedRouteBecomesBlocked_RejectsWithoutPartialMovement()
        {
            var actorId = new HeroId("primitive-hero");
            var blockerId = new HeroId("moving-blocker");
            var origin = new HexCoordinate(0, 0);
            var blockerOrigin = new HexCoordinate(1, -1);
            var blockedIntermediate = new HexCoordinate(1, 0);
            var destination = new HexCoordinate(2, 0);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 2));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = new AdjacentMoveAuthority(
                CreateRadiusTwoCoordinates(),
                new[]
                {
                    new HeroOccupancy(actorId, origin),
                    new HeroOccupancy(blockerId, blockerOrigin)
                });
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(
                new ActionResolutionId("resolution-1"),
                offer,
                new[] { definition },
                new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);
            Assert.That(
                start.PendingChoice.LegalDestinations.Select(candidate => candidate.Destination),
                Has.Member(destination));

            var blockerMove = movementAuthority.TryCommit(new AdjacentMoveCommand(
                blockerId,
                blockerOrigin,
                blockedIntermediate));
            var result = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                destination), RuleModificationSet.Empty);

            Assert.That(blockerMove.Accepted, Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
            Assert.That(result.Events, Is.Empty);
            Assert.That(
                movementAuthority.Occupancies.Single(occupancy => occupancy.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(origin));
            Assert.That(
                movementAuthority.Occupancies.Single(occupancy => occupancy.HeroId.Equals(blockerId)).Coordinate,
                Is.EqualTo(blockedIntermediate));
            Assert.That(authority.PendingResolution, Is.SameAs(start.PendingResolution));
            Assert.That(authority.PendingChoice, Is.SameAs(start.PendingChoice));
        }

        [Test]
        public void SameCommandSequence_ReplaysToSameStateAndOrderedEvents()
        {
            var firstReplay = RunTwoStepReplay();
            var secondReplay = RunTwoStepReplay();

            Assert.That(firstReplay.FinalCoordinate, Is.EqualTo(secondReplay.FinalCoordinate));
            Assert.That(firstReplay.Events, Is.EqualTo(secondReplay.Events));
        }

        [Test]
        public void StartMovementResolution_RejectsUnsupportedInvocationContext()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var offer = new ActionOffer(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                new ActionDefinitionId("primary-move"),
                actorId,
                ActionInvocationContext.GeneralRuleSecondary,
                true);
            var authority = CreateResolutionAuthority(actorId, new HexCoordinate(0, 0));

            var result = authority.StartMovementResolution(
                new ActionResolutionId("resolution-1"),
                offer,
                new[] { definition },
                new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.UnsupportedInvocationContext));
            Assert.That(authority.PendingResolution, Is.Null);
        }

        [Test]
        public void StartMovementResolution_RejectsFabricatedOrNoLongerAvailableOffer()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var instance = CreateInstance(actorId, definition);
            var fabricatedOffer = new ActionOffer(
                new CardInstanceId("forged-card"),
                definition.Reference,
                new ActionDefinitionId("primary-move"),
                actorId,
                ActionInvocationContext.PrintedPrimary,
                true);
            var mismatchedContextOffer = new ActionOffer(
                instance.Id,
                definition.Reference,
                new ActionDefinitionId("primary-move"),
                actorId,
                ActionInvocationContext.PrintedSecondary,
                true);
            var playedInstance = new CardInstance(
                instance.Id,
                definition.Reference,
                actorId,
                new CardStateFacts(
                    CardLocation.TurnArea,
                    true,
                    CardResolutionStatus.Unresolved,
                    CardFaceOrientation.FaceUp));

            var forgedResult = CreateResolutionAuthority(actorId, new HexCoordinate(0, 0)).StartMovementResolution(
                new ActionResolutionId("forged-resolution"),
                fabricatedOffer,
                new[] { definition },
                new[] { instance }, RuleModificationSet.Empty);
            var staleResult = CreateResolutionAuthority(actorId, new HexCoordinate(0, 0)).StartMovementResolution(
                new ActionResolutionId("stale-resolution"),
                CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary),
                new[] { definition },
                new[] { playedInstance }, RuleModificationSet.Empty);
            var mismatchedContextResult = CreateResolutionAuthority(actorId, new HexCoordinate(0, 0)).StartMovementResolution(
                new ActionResolutionId("mismatched-context-resolution"),
                mismatchedContextOffer,
                new[] { definition },
                new[] { instance }, RuleModificationSet.Empty);

            Assert.That(forgedResult.Accepted, Is.False);
            Assert.That(forgedResult.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.UnavailableOffer));
            Assert.That(staleResult.Accepted, Is.False);
            Assert.That(staleResult.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.UnavailableOffer));
            Assert.That(mismatchedContextResult.Accepted, Is.False);
            Assert.That(mismatchedContextResult.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.UnavailableOffer));
        }

        /// <summary>
        /// Milestone 6.2c closing check for finding F62B-03: starting a Skill revalidates the
        /// offer against the authoritative offer set inside Core exactly as the shared
        /// Movement/Attack prevalidation does. The live session derives its offers
        /// authoritatively, so what this closes is the trust the seam extended to its caller
        /// before a second Core caller (milestone 6.7's Repeat) could rely on it.
        /// </summary>
        [Test]
        public void StartTurnSkill_RejectsFabricatedOrNoLongerAvailableOffer()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Red);
            var redFocus = configuration.CardInstances.Single(card => card.Id.Value.Contains("red-focus"));
            var forgedOffer = new ActionOffer(
                redFocus.Id,
                redFocus.DefinitionReference,
                new ActionDefinitionId("primary-skill"),
                redFocus.OwnerId,
                ActionInvocationContext.PrintedPrimary,
                true);
            var mismatchedContextOffer = new ActionOffer(
                redFocus.Id,
                redFocus.DefinitionReference,
                new ActionDefinitionId("primary-skill"),
                redFocus.OwnerId,
                ActionInvocationContext.GeneralRuleSecondary,
                true);

            // The card is still in hand, so the authoritative turn offer recompute yields nothing:
            // an accepted start here executes an effect from an offer nobody derived.
            var forgedResult = CreateResolutionAuthority(redFocus.OwnerId, new HexCoordinate(0, 0))
                .StartTurnSkill(
                    new ActionResolutionId("forged-skill"),
                    forgedOffer,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    new ActiveEffectLifecycle(),
                    RuleModificationSet.Empty,
                    TieBreakerState.Unrevealed,
                     1,
                     1);
            var mismatchedResult = CreateResolutionAuthority(redFocus.OwnerId, new HexCoordinate(0, 0))
                .StartTurnSkill(
                    new ActionResolutionId("mismatched-context-skill"),
                    mismatchedContextOffer,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    new ActiveEffectLifecycle(),
                    RuleModificationSet.Empty,
                    TieBreakerState.Unrevealed,
                     1,
                     1);

            Assert.That(forgedResult.Accepted, Is.False);
            Assert.That(forgedResult.RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.UnavailableOffer));
            Assert.That(mismatchedResult.Accepted, Is.False);
            Assert.That(mismatchedResult.RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.UnsupportedInvocationContext));
        }

        [Test]
        public void StartMovementResolution_RejectsActorWithoutAuthoritativeOccupancy()
        {
            var actorId = new HeroId("card-owner");
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var instance = CreateInstance(actorId, definition);
            var offer = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId).Single();
            var authority = CreateResolutionAuthority(new HeroId("other-hero"), new HexCoordinate(0, 0));

            var result = authority.StartMovementResolution(
                new ActionResolutionId("resolution-1"),
                offer,
                new[] { definition },
                new[] { instance }, RuleModificationSet.Empty);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.ActorUnavailable));
            Assert.That(authority.PendingResolution, Is.Null);
        }

        [Test]
        public void PendingMovementResolution_RestoresFromPlainContinuationAndCompletesDeterministically()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(2, 0);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 2));
            var instance = CreateInstance(actorId, definition);
            var offer = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId).Single();
            var originalMovementAuthority = CreateMovementAuthority(actorId, origin);
            var originalAuthority = new CardActionResolutionAuthority(originalMovementAuthority);
            var start = originalAuthority.StartMovementResolution(
                new ActionResolutionId("resolution-1"),
                offer,
                new[] { definition },
                new[] { instance }, RuleModificationSet.Empty);
            var state = start.PendingResolution.MovementSessionState;
            Assert.That(state.Route, Is.EqualTo(new[] { origin }));
            Assert.That(state.StepEvents, Is.Empty);
            Assert.That(state.CommittedStepCount, Is.EqualTo(0));

            var restoredMovementAuthority = CreateMovementAuthority(actorId, origin);
            var tamperedChoice = new PendingMovementChoice(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                start.PendingChoice.ChooserId,
                start.PendingChoice.MovingHeroId,
                start.PendingChoice.Origin,
                start.PendingChoice.RemainingAllowance,
                start.PendingChoice.CanStop,
                System.Array.Empty<ReachableMovementDestination>());
            Assert.Throws<System.ArgumentException>(() => CardActionResolutionAuthority.Restore(
                restoredMovementAuthority,
                CardActionResolutionRestoreState.WithPendingMovement(
                    start.PendingResolution,
                    DefeatRewardRestoreStateHolder.Absent()),
                tamperedChoice,
                null,
                null,
                new[] { definition },
                new[] { instance },
                null,
                new MarkerAttachmentRegister(), RuleModificationSet.Empty));

            var restoredAuthority = CardActionResolutionAuthority.Restore(
                restoredMovementAuthority,
                CardActionResolutionRestoreState.WithPendingMovement(
                    start.PendingResolution,
                    DefeatRewardRestoreStateHolder.Absent()),
                start.PendingChoice,
                null,
                null,
                new[] { definition },
                new[] { instance },
                null,
                new MarkerAttachmentRegister(), RuleModificationSet.Empty);
            var completed = restoredAuthority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                restoredAuthority.PendingChoice.ResolutionId,
                restoredAuthority.PendingChoice.ChoiceId,
                destination), RuleModificationSet.Empty);

            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            Assert.That(restoredMovementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(destination));
            Assert.That(completed.Events.Select(resolutionEvent => resolutionEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MovementStepCommitted,
                CardActionResolutionEventKind.MovementStepCommitted,
                CardActionResolutionEventKind.MovementActionCompleted
            }));
        }

        [Test]
        public void PendingMovementResolution_RestoresCommittedPartialRouteAndContinuesSameAction()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var firstDestination = origin.GetNeighbor(HexDirection.East);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 2));
            var instance = CreateInstance(actorId, definition);
            var offer = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId).Single();
            var originalMovementAuthority = CreateMovementAuthority(actorId, origin);
            var originalAuthority = new CardActionResolutionAuthority(originalMovementAuthority);
            var start = originalAuthority.StartMovementResolution(
                new ActionResolutionId("resolution-1"),
                offer,
                new[] { definition },
                new[] { instance }, RuleModificationSet.Empty);
            var partial = originalAuthority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                firstDestination), RuleModificationSet.Empty);
            var movementAuthority = CreateMovementAuthority(actorId, firstDestination);
            var restored = CardActionResolutionAuthority.Restore(
                movementAuthority,
                CardActionResolutionRestoreState.WithPendingMovement(
                    partial.PendingResolution,
                    DefeatRewardRestoreStateHolder.Absent()),
                partial.PendingChoice,
                null,
                null,
                new[] { definition },
                new[] { instance },
                null,
                new MarkerAttachmentRegister(), RuleModificationSet.Empty);
            var destination = firstDestination.GetNeighbor(HexDirection.NorthEast);
            Assert.That(restored.PendingResolution, Is.SameAs(partial.PendingResolution));

            var completed = restored.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                restored.PendingChoice.ResolutionId,
                restored.PendingChoice.ChoiceId,
                destination), RuleModificationSet.Empty);

            Assert.That(partial.IsComplete, Is.False);
            Assert.That(partial.PendingResolution.MovementSessionState.Route, Is.EqualTo(new[] { origin, firstDestination }));
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(destination));
            Assert.That(completed.Events.Select(resolutionEvent => resolutionEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MovementStepCommitted,
                CardActionResolutionEventKind.MovementActionCompleted
            }));
        }

        private static ReplayResult RunTwoStepReplay()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var firstDestination = origin.GetNeighbor(HexDirection.East);
            var secondDestination = firstDestination.GetNeighbor(HexDirection.NorthEast);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 2));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var events = new List<CardActionResolutionEvent>();

            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition }, new[] { CreateInstance(actorId, definition) }, RuleModificationSet.Empty);
            events.AddRange(start.Events);
            var movement = authority.AnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                secondDestination), RuleModificationSet.Empty);
            events.AddRange(movement.Events);

            return new ReplayResult(movementAuthority.Occupancies.Single().Coordinate, events);
        }

        private static CardActionResolutionAuthority CreateResolutionAuthority(HeroId actorId, HexCoordinate origin)
        {
            return new CardActionResolutionAuthority(CreateMovementAuthority(actorId, origin));
        }

        private static AdjacentMoveAuthority CreateMovementAuthority(HeroId actorId, HexCoordinate origin)
        {
            return new AdjacentMoveAuthority(
                CreateRadiusTwoCoordinates(),
                new[] { new HeroOccupancy(actorId, origin) });
        }

        private static ActionOffer CreateOffer(
            HeroId actorId,
            CardDefinition definition,
            ActionInvocationContext invocationContext)
        {
            var origin = invocationContext == ActionInvocationContext.PrintedPrimary
                ? ActionDefinitionOrigin.PrintedPrimary
                : ActionDefinitionOrigin.PrintedSecondary;
            var instance = CreateInstance(actorId, definition);

            return ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId)
                .Single(candidate => candidate.InvocationContext == invocationContext &&
                    candidate.ActionDefinitionId.Equals(definition.ActionDefinitions.Single(action => action.Origin == origin).Id));
        }

        private static CardInstance CreateInstance(HeroId actorId, CardDefinition definition)
        {
            return CardInstance.CreateInHand(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                actorId);
        }

        private static CardDefinition CreateDefinition(params ActionDefinition[] actionDefinitions)
        {
            return new CardDefinition(
                new CardDefinitionId("placeholder-movement-card"),
                "rev-1",
                "Placeholder Movement Card",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                actionDefinitions);
        }

        private static ActionDefinition CreateMovementAction(
            string id,
            ActionDefinitionOrigin origin,
            int movementValue)
        {
            return new ActionDefinition(
                new ActionDefinitionId(id),
                ActionKind.Movement,
                origin,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movementValue) },
                origin == ActionDefinitionOrigin.PrintedPrimary
                    ? new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)
                    : new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));
        }

        private static IEnumerable<HexCoordinate> CreateRadiusTwoCoordinates()
        {
            const int radius = 2;
            for (var q = -radius; q <= radius; q++)
            {
                var minimumR = System.Math.Max(-radius, -q - radius);
                var maximumR = System.Math.Min(radius, -q + radius);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }

        private readonly struct ReplayResult
        {
            public ReplayResult(
                HexCoordinate finalCoordinate,
                IEnumerable<CardActionResolutionEvent> events)
            {
                FinalCoordinate = finalCoordinate;
                Events = events.ToArray();
            }

            public HexCoordinate FinalCoordinate { get; }

            public IReadOnlyList<CardActionResolutionEvent> Events { get; }
        }

        private sealed class MutableMovementActionStartRestrictionEvaluator :
            IMovementActionStartRestrictionEvaluator
        {
            public MutableMovementActionStartRestrictionEvaluator(
                MovementActionStartRestriction restriction)
            {
                Restriction = restriction;
            }

            public MovementActionStartRestriction Restriction { get; set; }

            public int EvaluationCount { get; private set; }

            public MovementActionStartContext LastContext { get; private set; }

            public MovementActionStartRestriction Evaluate(MovementActionStartContext context)
            {
                EvaluationCount++;
                LastContext = context;
                return Restriction;
            }
        }
    }
}
