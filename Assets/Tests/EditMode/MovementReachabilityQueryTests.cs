using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MovementReachabilityQueryTests
    {
        [Test]
        public void FindReachableDestinations_ReturnsSpacesWithinAllowanceWithRoutes()
        {
            var hero = new HeroOccupancy(new HeroId("mover"), new HexCoordinate(0, 0));

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                hero,
                CreateRadiusTwoCoordinates(),
                new[] { hero },
                2);
            var farEast = destinations.Single(destination => destination.Destination.Equals(new HexCoordinate(2, 0)));

            Assert.That(destinations.Select(destination => destination.Destination), Has.No.Member(hero.Coordinate));
            Assert.That(destinations, Has.Count.EqualTo(18));
            Assert.That(farEast.Cost, Is.EqualTo(2));
            Assert.That(farEast.Route, Is.EqualTo(new[]
            {
                hero.Coordinate,
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0)
            }));
        }

        [Test]
        public void FindReachableDestinations_ExcludesSpacesBeyondAllowance()
        {
            var hero = new HeroOccupancy(new HeroId("mover"), new HexCoordinate(0, 0));

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                hero,
                CreateRadiusTwoCoordinates(),
                new[] { hero },
                1);

            Assert.That(destinations.Select(destination => destination.Destination), Is.EquivalentTo(hero.Coordinate.GetNeighbors()));
            Assert.That(destinations.Select(destination => destination.Destination), Has.No.Member(new HexCoordinate(2, 0)));
        }

        [Test]
        public void FindReachableDestinations_UsesDeterministicShortestRouteWhenMultipleRoutesExist()
        {
            var hero = new HeroOccupancy(new HeroId("mover"), new HexCoordinate(0, 0));

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                hero,
                CreateRadiusTwoCoordinates(),
                new[] { hero },
                2);
            var southEastCorner = destinations.Single(destination => destination.Destination.Equals(new HexCoordinate(1, 1)));

            Assert.That(southEastCorner.Cost, Is.EqualTo(2));
            Assert.That(southEastCorner.Route, Is.EqualTo(new[]
            {
                hero.Coordinate,
                new HexCoordinate(1, 0),
                new HexCoordinate(1, 1)
            }));
        }

        [Test]
        public void FindReachableDestinations_DoesNotRouteThroughOccupiedSpaces()
        {
            var hero = new HeroOccupancy(new HeroId("mover"), new HexCoordinate(0, 0));
            var blocker = new HeroOccupancy(new HeroId("blocker"), new HexCoordinate(1, 0));

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                hero,
                new[] { hero.Coordinate, blocker.Coordinate, new HexCoordinate(2, 0) },
                new[] { hero, blocker },
                2);

            Assert.That(destinations, Is.Empty);
        }

        [Test]
        public void FindReachableDestinations_ExcludesObstacleDestinationsAndRoutesAroundObstacle()
        {
            var hero = new HeroOccupancy(new HeroId("mover"), new HexCoordinate(0, 0));
            var obstacles = CreateDetourObstacles();

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                hero,
                CreateRadiusTwoCoordinates(),
                new[] { hero },
                obstacles,
                3);
            var detourDestination = destinations.Single(destination => destination.Destination.Equals(new HexCoordinate(2, 0)));

            Assert.That(destinations.Select(destination => destination.Destination), Has.No.Member(new HexCoordinate(1, 0)));
            Assert.That(destinations.Select(destination => destination.Destination), Has.No.Member(new HexCoordinate(0, 1)));
            Assert.That(detourDestination.Cost, Is.EqualTo(3));
            Assert.That(detourDestination.Route, Is.EqualTo(new[]
            {
                hero.Coordinate,
                new HexCoordinate(1, -1),
                new HexCoordinate(2, -1),
                new HexCoordinate(2, 0)
            }));
        }

        [Test]
        public void FindReachableDestinations_ExcludesDestinationWhenDetourExceedsAllowance()
        {
            var hero = new HeroOccupancy(new HeroId("mover"), new HexCoordinate(0, 0));

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                hero,
                CreateRadiusTwoCoordinates(),
                new[] { hero },
                CreateDetourObstacles(),
                2);

            Assert.That(destinations.Select(destination => destination.Destination), Has.No.Member(new HexCoordinate(2, 0)));
        }

        private static ObstacleOccupancy[] CreateDetourObstacles()
        {
            return new[]
            {
                new ObstacleOccupancy(new ObstacleId("east-blocker"), new HexCoordinate(1, 0)),
                new ObstacleOccupancy(new ObstacleId("south-east-blocker"), new HexCoordinate(0, 1))
            };
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
