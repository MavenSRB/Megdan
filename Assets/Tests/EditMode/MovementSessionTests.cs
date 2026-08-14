using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MovementSessionTests
    {
        [Test]
        public void Complete_WithoutSteps_AllowsZeroMovement()
        {
            var heroId = new HeroId("mover");
            var origin = new HexCoordinate(0, 0);
            var session = CreateSession(heroId, origin, 3);

            session.Complete();

            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.RemainingAllowance, Is.EqualTo(3));
            Assert.That(session.Route, Is.EqualTo(new[] { origin }));
            Assert.That(session.StepEvents, Is.Empty);
            Assert.That(session.CurrentOccupancy.Coordinate, Is.EqualTo(origin));
        }

        [Test]
        public void Complete_AfterOneStep_StopsBeforeAllowanceIsExhausted()
        {
            var heroId = new HeroId("mover");
            var origin = new HexCoordinate(0, 0);
            var destination = origin.GetNeighbor(HexDirection.East);
            var session = CreateSession(heroId, origin, 3);

            var step = session.TryCommitStep(destination);
            session.Complete();

            Assert.That(step.Accepted, Is.True);
            Assert.That(step.RemainingAllowance, Is.EqualTo(2));
            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.RemainingAllowance, Is.EqualTo(2));
            Assert.That(session.Route, Is.EqualTo(new[] { origin, destination }));
            Assert.That(session.StepEvents.Count, Is.EqualTo(1));
            Assert.That(session.StepEvents[0].Origin, Is.EqualTo(origin));
            Assert.That(session.StepEvents[0].Destination, Is.EqualTo(destination));
            Assert.That(session.CurrentOccupancy.Coordinate, Is.EqualTo(destination));
        }

        [Test]
        public void TryCommitStep_ExactAllowance_CompletesWithExplicitRouteAndAdjacentEvents()
        {
            var heroId = new HeroId("mover");
            var origin = new HexCoordinate(0, 0);
            var firstDestination = origin.GetNeighbor(HexDirection.East);
            var secondDestination = firstDestination.GetNeighbor(HexDirection.East);
            var session = CreateSession(heroId, origin, 2);

            var firstStep = session.TryCommitStep(firstDestination);
            var secondStep = session.TryCommitStep(secondDestination);

            Assert.That(firstStep.Accepted, Is.True);
            Assert.That(firstStep.SessionComplete, Is.False);
            Assert.That(secondStep.Accepted, Is.True);
            Assert.That(secondStep.SessionComplete, Is.True);
            Assert.That(secondStep.RemainingAllowance, Is.EqualTo(0));
            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.Route, Is.EqualTo(new[] { origin, firstDestination, secondDestination }));
            Assert.That(session.StepEvents.Select(movementEvent => movementEvent.Origin), Is.EqualTo(new[] { origin, firstDestination }));
            Assert.That(session.StepEvents.Select(movementEvent => movementEvent.Destination), Is.EqualTo(new[] { firstDestination, secondDestination }));
            Assert.That(session.CurrentOccupancy.Coordinate, Is.EqualTo(secondDestination));
        }

        [Test]
        public void TryCommitStep_AfterAllowanceIsSpent_IsRejectedWithoutMoving()
        {
            var heroId = new HeroId("mover");
            var origin = new HexCoordinate(0, 0);
            var firstDestination = origin.GetNeighbor(HexDirection.East);
            var blockedOverMoveDestination = firstDestination.GetNeighbor(HexDirection.East);
            var session = CreateSession(heroId, origin, 1);

            var firstStep = session.TryCommitStep(firstDestination);
            var rejectedStep = session.TryCommitStep(blockedOverMoveDestination);

            Assert.That(firstStep.Accepted, Is.True);
            Assert.That(rejectedStep.Accepted, Is.False);
            Assert.That(rejectedStep.RejectionReason, Is.EqualTo(MovementSessionStepRejectionReason.NoRemainingAllowance));
            Assert.That(rejectedStep.NewEvents, Is.Empty);
            Assert.That(rejectedStep.CommittedEvents.Count, Is.EqualTo(1));
            Assert.That(session.Route, Is.EqualTo(new[] { origin, firstDestination }));
            Assert.That(session.CurrentOccupancy.Coordinate, Is.EqualTo(firstDestination));
        }

        private static MovementSession CreateSession(HeroId heroId, HexCoordinate origin, int allowance)
        {
            var authority = new AdjacentMoveAuthority(
                CreateRadiusTwoCoordinates(),
                new[] { new HeroOccupancy(heroId, origin) });
            return new MovementSession(authority, heroId, allowance);
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
    }
}
