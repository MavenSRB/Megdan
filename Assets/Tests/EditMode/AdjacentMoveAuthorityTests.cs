using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class AdjacentMoveAuthorityTests
    {
        [Test]
        public void PersistentObstacle_PlaceRepositionAndRemove_KeepOneIdentityAndFreeOldSpace()
        {
            var origin = new HexCoordinate(0, 0);
            var first = new HexCoordinate(1, 0);
            var second = new HexCoordinate(0, 1);
            var obstacle = new ObstacleId("turret-test");
            var board = new AdjacentMoveAuthority(
                new[] { origin, first, second },
                new[] { new HeroOccupancy(new HeroId("actor"), origin) });

            Assert.That(board.TryPlaceOrRepositionObstacle(obstacle, first), Is.True);
            Assert.That(board.TryPlaceOrRepositionObstacle(obstacle, second), Is.True);
            Assert.That(board.ObstacleOccupancies, Has.Count.EqualTo(1));
            Assert.That(board.TryGetObstacleCoordinate(obstacle, out var at), Is.True);
            Assert.That(at, Is.EqualTo(second));
            Assert.That(board.IsEmptyPlayableSpace(first), Is.True);
            Assert.That(board.IsEmptyPlayableSpace(second), Is.False);
            Assert.That(board.TryRemoveObstacle(obstacle), Is.True);
            Assert.That(board.TryRemoveObstacle(obstacle), Is.False);
            Assert.That(board.ObstacleOccupancies, Is.Empty);
            Assert.That(board.IsEmptyPlayableSpace(second), Is.True);
        }

        [Test]
        public void PersistentObstacle_IllegalOrStalePlacement_IsAtomic()
        {
            var origin = new HexCoordinate(0, 0);
            var first = new HexCoordinate(1, 0);
            var second = new HexCoordinate(0, 1);
            var obstacle = new ObstacleId("turret-test");
            var board = new AdjacentMoveAuthority(
                new[] { origin, first, second },
                new[] { new HeroOccupancy(new HeroId("actor"), origin) });

            Assert.That(board.TryPlaceOrRepositionObstacle(obstacle, first), Is.True);
            Assert.That(board.TryPlaceOrRepositionObstacle(obstacle, first), Is.False);
            Assert.That(board.TryPlaceOrRepositionObstacle(obstacle, origin), Is.False);
            Assert.That(board.TryPlaceOrRepositionObstacle(obstacle, new HexCoordinate(9, 9)), Is.False);
            Assert.That(board.ObstacleOccupancies.Single().Coordinate, Is.EqualTo(first));
            Assert.That(board.IsEmptyPlayableSpace(second), Is.True);
        }

        [Test]
        public void PersistentObstacle_BoardRestoreState_PreservesObstacleWithoutMakingTerrain()
        {
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(1, 0);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("turret-test-map"), 1, new[] { origin, destination });
            var obstacle = new ObstacleId("turret-test");
            var board = new AdjacentMoveAuthority(
                map, new[] { new HeroOccupancy(new HeroId("actor"), origin) },
                Array.Empty<ObstacleOccupancy>());
            Assert.That(board.TryPlaceOrRepositionObstacle(obstacle, destination), Is.True);

            var restored = AdjacentMoveAuthority.Restore(
                board.CaptureRestoreState(), map,
                Array.Empty<GuardsDigital.Core.Setup.QuickGameMinionSetupState>());

            Assert.That(restored.ObstacleOccupancies.Single().ObstacleId, Is.EqualTo(obstacle));
            Assert.That(restored.ObstacleOccupancies.Single().Coordinate, Is.EqualTo(destination));
            Assert.That(restored.IsEmptyPlayableSpace(destination), Is.False);
            Assert.That(restored.IsEmptyPlayableSpace(origin), Is.False);
        }

        [Test]
        public void TryCommit_LegalAdjacentMove_UpdatesOccupancyAndReturnsMovementEvent()
        {
            var heroId = new HeroId("mover");
            var origin = new HexCoordinate(0, 0);
            var destination = origin.GetNeighbor(HexDirection.East);
            var authority = CreateAuthority(
                new HeroOccupancy(heroId, origin),
                origin,
                destination);

            var result = authority.TryCommit(new AdjacentMoveCommand(heroId, origin, destination));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RejectionReason, Is.EqualTo(AdjacentMoveRejectionReason.None));
            Assert.That(result.Events.Count, Is.EqualTo(1));
            Assert.That(result.Events[0].HeroId, Is.EqualTo(heroId));
            Assert.That(result.Events[0].Origin, Is.EqualTo(origin));
            Assert.That(result.Events[0].Destination, Is.EqualTo(destination));
            Assert.That(authority.Occupancies.Single().Coordinate, Is.EqualTo(destination));
            Assert.That(authority.Occupancies.Select(occupancy => occupancy.Coordinate), Has.No.Member(origin));
        }

        [Test]
        public void TryCommit_NonAdjacentMove_IsRejectedWithoutChangingState()
        {
            var heroId = new HeroId("mover");
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(2, 0);
            var authority = CreateAuthority(
                new HeroOccupancy(heroId, origin),
                origin,
                destination);

            var result = authority.TryCommit(new AdjacentMoveCommand(heroId, origin, destination));

            AssertRejected(result, AdjacentMoveRejectionReason.DestinationNotAdjacent);
            Assert.That(authority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
        }

        [Test]
        public void TryCommit_OccupiedDestination_IsRejectedWithoutChangingState()
        {
            var heroId = new HeroId("mover");
            var blockerId = new HeroId("blocker");
            var origin = new HexCoordinate(0, 0);
            var destination = origin.GetNeighbor(HexDirection.East);
            var authority = CreateAuthority(
                new[]
                {
                    new HeroOccupancy(heroId, origin),
                    new HeroOccupancy(blockerId, destination)
                },
                new[] { origin, destination });

            var result = authority.TryCommit(new AdjacentMoveCommand(heroId, origin, destination));

            AssertRejected(result, AdjacentMoveRejectionReason.DestinationOccupied);
            Assert.That(authority.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate, Is.EqualTo(origin));
            Assert.That(authority.Occupancies.Single(occupancy => occupancy.HeroId.Equals(blockerId)).Coordinate, Is.EqualTo(destination));
        }

        [Test]
        public void TryCommit_ObstacleDestination_IsRejectedWithoutChangingState()
        {
            var heroId = new HeroId("mover");
            var origin = new HexCoordinate(0, 0);
            var destination = origin.GetNeighbor(HexDirection.East);
            var authority = new AdjacentMoveAuthority(
                new[] { origin, destination },
                new[] { new HeroOccupancy(heroId, origin) },
                new[] { new ObstacleOccupancy(new ObstacleId("blocker"), destination) });

            var result = authority.TryCommit(new AdjacentMoveCommand(heroId, origin, destination));

            AssertRejected(result, AdjacentMoveRejectionReason.DestinationOccupied);
            Assert.That(authority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
            Assert.That(authority.ObstacleOccupancies.Single().Coordinate, Is.EqualTo(destination));
        }

        [Test]
        public void Constructor_ObstacleSharingUnitCoordinate_IsRejected()
        {
            var heroId = new HeroId("mover");
            var origin = new HexCoordinate(0, 0);

            Assert.Throws<ArgumentException>(() => new AdjacentMoveAuthority(
                new[] { origin },
                new[] { new HeroOccupancy(heroId, origin) },
                new[] { new ObstacleOccupancy(new ObstacleId("blocker"), origin) }));
        }

        [Test]
        public void Constructor_DuplicateObstacleCoordinate_IsRejected()
        {
            var origin = new HexCoordinate(0, 0);
            var obstacleCoordinate = origin.GetNeighbor(HexDirection.East);

            Assert.Throws<ArgumentException>(() => new AdjacentMoveAuthority(
                new[] { origin, obstacleCoordinate },
                new[] { new HeroOccupancy(new HeroId("mover"), origin) },
                new[]
                {
                    new ObstacleOccupancy(new ObstacleId("first"), obstacleCoordinate),
                    new ObstacleOccupancy(new ObstacleId("second"), obstacleCoordinate)
                }));
        }

        [Test]
        public void TryCommit_WrongOrigin_IsRejectedAsStaleCommandWithoutChangingState()
        {
            var heroId = new HeroId("mover");
            var actualOrigin = new HexCoordinate(0, 0);
            var staleOrigin = actualOrigin.GetNeighbor(HexDirection.West);
            var destination = actualOrigin.GetNeighbor(HexDirection.East);
            var authority = CreateAuthority(
                new HeroOccupancy(heroId, actualOrigin),
                actualOrigin,
                staleOrigin,
                destination);

            var result = authority.TryCommit(new AdjacentMoveCommand(heroId, staleOrigin, destination));

            AssertRejected(result, AdjacentMoveRejectionReason.WrongOrigin);
            Assert.That(authority.Occupancies.Single().Coordinate, Is.EqualTo(actualOrigin));
        }

        [Test]
        public void TryCommit_UnknownHero_IsRejectedWithoutChangingState()
        {
            var heroId = new HeroId("mover");
            var unknownHeroId = new HeroId("unknown");
            var origin = new HexCoordinate(0, 0);
            var destination = origin.GetNeighbor(HexDirection.East);
            var authority = CreateAuthority(
                new HeroOccupancy(heroId, origin),
                origin,
                destination);

            var result = authority.TryCommit(new AdjacentMoveCommand(unknownHeroId, origin, destination));

            AssertRejected(result, AdjacentMoveRejectionReason.UnknownHero);
            Assert.That(authority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
        }

        [Test]
        public void TryCommit_DestinationOffBoard_IsRejectedWithoutChangingState()
        {
            var heroId = new HeroId("mover");
            var origin = new HexCoordinate(0, 0);
            var destination = origin.GetNeighbor(HexDirection.East);
            var authority = CreateAuthority(
                new HeroOccupancy(heroId, origin),
                origin);

            var result = authority.TryCommit(new AdjacentMoveCommand(heroId, origin, destination));

            AssertRejected(result, AdjacentMoveRejectionReason.DestinationOffBoard);
            Assert.That(authority.Occupancies.Single().Coordinate, Is.EqualTo(origin));
        }

        private static AdjacentMoveAuthority CreateAuthority(
            HeroOccupancy occupancy,
            params HexCoordinate[] boardCoordinates)
        {
            return CreateAuthority(new[] { occupancy }, boardCoordinates);
        }

        private static AdjacentMoveAuthority CreateAuthority(
            IEnumerable<HeroOccupancy> occupancies,
            IEnumerable<HexCoordinate> boardCoordinates)
        {
            return new AdjacentMoveAuthority(boardCoordinates, occupancies);
        }

        private static void AssertRejected(
            AdjacentMoveResult result,
            AdjacentMoveRejectionReason expectedReason)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(expectedReason));
            Assert.That(result.Events, Is.Empty);
        }
    }
}
