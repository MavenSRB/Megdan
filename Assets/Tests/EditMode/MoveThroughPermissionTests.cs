using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8c. The Move-Through permission's Movement consumer.
    ///
    /// Milestone 6.8 built the permission and <see cref="ObstacleQuery"/>'s separation of p.18's two
    /// questions - "may this unit pass through here" and "may any unit stop here" - and left the query
    /// with no caller. This fixture is that caller's proof: reachability asks both questions, and route
    /// commitment asserts occupancy only at the endpoint so a passed-through space is entered and left
    /// without ever being occupied.
    ///
    /// The board is a straight line, hero at (0,0), an obstacle at (1,0), open space beyond, which is
    /// the smallest shape in which passing through and stopping short are different answers.
    /// </summary>
    public sealed class MoveThroughPermissionTests
    {
        private static readonly HeroId Mover = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
        private static readonly HeroId Blocker = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

        // ---------------------------------------------------------------------------------------
        // Reachability
        // ---------------------------------------------------------------------------------------

        [Test]
        public void WithoutThePermission_AnOccupiedSpaceBlocksTheRouteBeyondIt()
        {
            var board = CreateLineBoard();

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                Occupancy(board), board, 3);

            Assert.That(
                destinations.Select(destination => destination.Destination),
                Is.EquivalentTo(new[] { new HexCoordinate(-1, 0), new HexCoordinate(-2, 0) }),
                "An obstacle blocks transit for an ordinary mover, so only the open side is reachable.");
        }

        [Test]
        public void WithThePermission_TheRouteContinuesBeyondTheObstacleButNeverStopsOnIt()
        {
            var board = CreateLineBoard();

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                Occupancy(board), board, 3, MoveThroughFor(Mover));

            Assert.That(
                destinations.Select(destination => destination.Destination),
                Is.EquivalentTo(new[]
                {
                    new HexCoordinate(-1, 0),
                    new HexCoordinate(-2, 0),
                    new HexCoordinate(2, 0),
                    new HexCoordinate(3, 0)
                }),
                "Rulebook p.18: the unit may enter and leave the obstacle's space while moving.");
            Assert.That(
                destinations.Any(destination =>
                    destination.Destination.Equals(new HexCoordinate(1, 0))),
                Is.False,
                "p.18 also says no unit may end its movement on an obstacle, permission or not.");
        }

        [Test]
        public void PassingThrough_CostsTheSameAsEnteringAnyOtherSpace()
        {
            var board = CreateLineBoard();

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                Occupancy(board), board, 3, MoveThroughFor(Mover));
            var beyond = destinations.Single(destination =>
                destination.Destination.Equals(new HexCoordinate(2, 0)));

            Assert.That(beyond.Cost, Is.EqualTo(2),
                "The permission changes which routes exist, not what they cost.");
            Assert.That(beyond.Route, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 0),
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0)
            }), "The route still names every space entered, including the one passed through.");
        }

        [Test]
        public void ThePermissionNeverReachesOffTheBoard()
        {
            var board = CreateLineBoard();

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                Occupancy(board), board, 6, MoveThroughFor(Mover));

            Assert.That(
                destinations.Any(destination => destination.Destination.Equals(new HexCoordinate(4, 0))),
                Is.False,
                "Nothing lets a unit pass outside the playable board.");
        }

        [Test]
        public void ThePermissionAppliesOnlyToTheUnitHoldingIt()
        {
            var board = CreateLineBoard();

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                Occupancy(board), board, 3, MoveThroughFor(Blocker));

            Assert.That(
                destinations.Select(destination => destination.Destination),
                Is.EquivalentTo(new[] { new HexCoordinate(-1, 0), new HexCoordinate(-2, 0) }),
                "A permission on another unit does nothing for this mover.");
        }

        // ---------------------------------------------------------------------------------------
        // Route commitment
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ACommittedThroughRoute_LeavesTheObstacleInPlaceAndTheMoverBeyondIt()
        {
            var board = CreateLineBoard();
            var session = new MovementSession(board, Mover, 3);

            var result = session.TryCommitRoute(
                new[] { new HexCoordinate(1, 0), new HexCoordinate(2, 0) },
                MoveThroughFor(Mover));

            Assert.That(result.Accepted, Is.True);
            Assert.That(board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Mover)).Coordinate,
                Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Blocker)).Coordinate,
                Is.EqualTo(new HexCoordinate(1, 0)),
                "Passing through moves nothing but the mover.");
            Assert.That(board.IsEmptyPlayableSpace(new HexCoordinate(0, 0)), Is.True,
                "The origin is vacated exactly once.");
            Assert.That(result.RemainingAllowance, Is.EqualTo(1),
                "Two spaces entered cost two movement.");
        }

        [Test]
        public void ACommittedThroughRoute_NarratesEverySpaceItEntered()
        {
            var board = CreateLineBoard();
            var session = new MovementSession(board, Mover, 3);

            var result = session.TryCommitRoute(
                new[] { new HexCoordinate(1, 0), new HexCoordinate(2, 0) },
                MoveThroughFor(Mover));

            Assert.That(
                result.NewEvents.Select(value => (value.Origin, value.Destination)),
                Is.EqualTo(new[]
                {
                    (new HexCoordinate(0, 0), new HexCoordinate(1, 0)),
                    (new HexCoordinate(1, 0), new HexCoordinate(2, 0))
                }),
                "p.18 has the unit enter the obstacle's space and leave it, and the facts say so.");
            Assert.That(session.Route, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 0),
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0)
            }));
        }

        [Test]
        public void ARouteEndingOnTheObstacle_IsRefusedWithoutMovingAnything()
        {
            var board = CreateLineBoard();
            var session = new MovementSession(board, Mover, 3);

            var result = session.TryCommitRoute(
                new[] { new HexCoordinate(1, 0) }, MoveThroughFor(Mover));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.AdjacentMoveRejectionReason,
                Is.EqualTo(AdjacentMoveRejectionReason.DestinationOccupied));
            AssertBoardUnchanged(board, session);
        }

        [Test]
        public void AThroughRouteWithoutThePermission_IsRefusedAtTheSpaceItWouldHaveCrossed()
        {
            var board = CreateLineBoard();
            var session = new MovementSession(board, Mover, 3);

            var result = board.TryCommitRoute(
                Mover, new[] { new HexCoordinate(1, 0), new HexCoordinate(2, 0) });

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(AdjacentMoveRejectionReason.DestinationOccupied));
            Assert.That(result.RejectedRouteIndex, Is.EqualTo(0),
                "The refusal names the space that refused it.");
            AssertBoardUnchanged(board, session);
        }

        [Test]
        public void ARouteIsRefusedWholeRatherThanWalkedPartway()
        {
            var board = CreateLineBoard();
            var session = new MovementSession(board, Mover, 3);

            // The first space is free and the second leaves the board, so a walk would have moved the
            // hero once before discovering the refusal.
            var result = session.TryCommitRoute(
                new[] { new HexCoordinate(-1, 0), new HexCoordinate(0, -1) },
                MoveThroughFor(Mover));

            Assert.That(result.Accepted, Is.False);
            AssertBoardUnchanged(board, session);
        }

        [Test]
        public void ARouteBeyondTheRemainingAllowance_IsRefused()
        {
            var board = CreateLineBoard();
            var session = new MovementSession(board, Mover, 1);

            var result = session.TryCommitRoute(
                new[] { new HexCoordinate(1, 0), new HexCoordinate(2, 0) },
                MoveThroughFor(Mover));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(MovementSessionStepRejectionReason.NoRemainingAllowance));
            AssertBoardUnchanged(board, session);
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static void AssertBoardUnchanged(AdjacentMoveAuthority board, MovementSession session)
        {
            Assert.That(board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Mover)).Coordinate,
                Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Blocker)).Coordinate,
                Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(session.Route, Is.EqualTo(new[] { new HexCoordinate(0, 0) }));
            Assert.That(session.StepEvents, Is.Empty);
        }

        private static HeroOccupancy Occupancy(AdjacentMoveAuthority board) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Mover));

        /// <summary>
        /// A live set carrying only the Move-Through permission, built the way the match builds it:
        /// from an Active effect in its active phase, not hand-assembled.
        /// </summary>
        private static RuleModificationSet MoveThroughFor(HeroId heroId)
        {
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"move-through-{heroId.Value}"),
                GuardsDigital.Core.Effects.Synthetic.SyntheticModificationCapabilities
                    .MoveThroughPermissionDefinition,
                heroId,
                TeamSide.Red,
                default,
                new CardInstanceId("move-through-source-card"),
                new ActionDefinitionId("move-through-source-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2),
                new TieBreakerState(TeamSide.Red));

            var participants = PlaceholderOneVsOneParticipants.Create();
            var set = RuleModificationQuery.Build(
                timing, null, participants, new TieBreakerState(TeamSide.Red));
            Assert.That(
                set.HasPermission(RuleObject.ForHero(heroId), RulePermissionKind.MoveThroughObstacles),
                Is.True,
                "The fixture must actually hold the permission, or every case below proves nothing.");
            return set;
        }

        /// <summary>Hero at (0,0), an obstacle unit at (1,0), open board from (-2,0) to (3,0).</summary>
        private static AdjacentMoveAuthority CreateLineBoard()
        {
            var coordinates = new[]
            {
                new HexCoordinate(-2, 0),
                new HexCoordinate(-1, 0),
                new HexCoordinate(0, 0),
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0),
                new HexCoordinate(3, 0)
            };
            return new AdjacentMoveAuthority(
                coordinates,
                new[]
                {
                    new HeroOccupancy(Mover, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Blocker, new HexCoordinate(1, 0))
                });
        }
    }
}
