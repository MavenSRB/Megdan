using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class CardActionResolutionAuthorityTests
    {
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

            var result = authority.StartMovementResolution(resolutionId, offer, new[] { definition });

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
            Assert.That(result.PendingChoice.LegalStepDestinations, Is.EquivalentTo(new HexCoordinate(0, 0).GetNeighbors()));
            Assert.That(result.Events.Select(resolutionEvent => resolutionEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MovementActionStarted,
                CardActionResolutionEventKind.MovementChoiceRequested
            }));
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
                new[] { definition });

            Assert.That(result.PendingResolution.InvocationContext, Is.EqualTo(ActionInvocationContext.PrintedSecondary));
            Assert.That(result.PendingResolution.ActionDefinitionId, Is.EqualTo(new ActionDefinitionId("secondary-move")));
            Assert.That(result.PendingResolution.MovementAllowance, Is.EqualTo(1));
            Assert.That(result.PendingResolution.RemainingAllowance, Is.EqualTo(1));
        }

        [Test]
        public void AnswerMovementChoice_CommitsAdjacentStepThroughMovementAuthorityAndRequestsNextChoice()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var destination = origin.GetNeighbor(HexDirection.East);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 2));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition });

            var step = authority.AnswerMovementChoice(MovementChoiceCommand.Step(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                destination));

            Assert.That(step.Accepted, Is.True);
            Assert.That(step.IsComplete, Is.False);
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(destination));
            Assert.That(step.PendingResolution.CurrentStepId, Is.EqualTo(new ActionResolutionStepId("movement-step-1")));
            Assert.That(step.PendingResolution.RemainingAllowance, Is.EqualTo(1));
            Assert.That(step.PendingChoice.Origin, Is.EqualTo(destination));
            Assert.That(step.Events.Select(resolutionEvent => resolutionEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MovementStepCommitted,
                CardActionResolutionEventKind.MovementChoiceRequested
            }));
            Assert.That(step.Events[0].MovementEvent.Origin, Is.EqualTo(origin));
            Assert.That(step.Events[0].MovementEvent.Destination, Is.EqualTo(destination));
        }

        [Test]
        public void AnswerMovementChoice_StopAtZeroOrBeforeAllowanceIsSpent_CompletesLegally()
        {
            var actorId = new HeroId("primitive-hero");
            var origin = new HexCoordinate(0, 0);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition });

            var stop = authority.AnswerMovementChoice(MovementChoiceCommand.Stop(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId));

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
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition });

            var step = authority.AnswerMovementChoice(MovementChoiceCommand.Step(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                destination));

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
            var invalidDestination = firstDestination.GetNeighbor(HexDirection.East);
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 2));
            var offer = CreateOffer(actorId, definition, ActionInvocationContext.PrintedPrimary);
            var movementAuthority = CreateMovementAuthority(actorId, origin);
            var authority = new CardActionResolutionAuthority(movementAuthority);
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition });

            var invalid = authority.AnswerMovementChoice(MovementChoiceCommand.Step(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                invalidDestination));

            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
            Assert.That(authority.PendingChoice.ChoiceId, Is.EqualTo(start.PendingChoice.ChoiceId));

            var accepted = authority.AnswerMovementChoice(MovementChoiceCommand.Step(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                firstDestination));
            var stale = authority.AnswerMovementChoice(MovementChoiceCommand.Step(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                firstDestination.GetNeighbor(HexDirection.East)));

            Assert.That(accepted.Accepted, Is.True);
            Assert.That(stale.Accepted, Is.False);
            Assert.That(stale.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.StaleChoice));
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(firstDestination));
            Assert.That(authority.PendingChoice.ChoiceId, Is.EqualTo(new ActionResolutionStepId("movement-step-1")));
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
            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition });

            var blockedStep = authority.AnswerMovementChoice(MovementChoiceCommand.Step(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                blockedDestination));

            Assert.That(start.PendingChoice.LegalStepDestinations, Has.No.Member(blockedDestination));
            Assert.That(blockedStep.Accepted, Is.False);
            Assert.That(blockedStep.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
            Assert.That(movementAuthority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
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
                new[] { definition });

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.UnsupportedInvocationContext));
            Assert.That(authority.PendingResolution, Is.Null);
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

            var start = authority.StartMovementResolution(new ActionResolutionId("resolution-1"), offer, new[] { definition });
            events.AddRange(start.Events);
            var firstStep = authority.AnswerMovementChoice(MovementChoiceCommand.Step(
                start.PendingChoice.ResolutionId,
                start.PendingChoice.ChoiceId,
                firstDestination));
            events.AddRange(firstStep.Events);
            var secondStep = authority.AnswerMovementChoice(MovementChoiceCommand.Step(
                firstStep.PendingChoice.ResolutionId,
                firstStep.PendingChoice.ChoiceId,
                secondDestination));
            events.AddRange(secondStep.Events);

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
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                actorId);

            return ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId)
                .Single(candidate => candidate.InvocationContext == invocationContext &&
                    candidate.ActionDefinitionId.Equals(definition.ActionDefinitions.Single(action => action.Origin == origin).Id));
        }

        private static CardDefinition CreateDefinition(params ActionDefinition[] actionDefinitions)
        {
            return new CardDefinition(
                new CardDefinitionId("placeholder-movement-card"),
                "rev-1",
                "Placeholder Movement Card",
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
    }
}
