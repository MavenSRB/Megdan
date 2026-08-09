using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class AdjacentDestinationQueryTests
    {
        [Test]
        public void FindEmptyAdjacentDestinations_ReturnsOnlyAdjacentBoardSpaces()
        {
            var hero = new HeroOccupancy(new HeroId("selected"), new HexCoordinate(0, 0));
            var board = new List<HexCoordinate>(hero.Coordinate.GetNeighbors())
            {
                hero.Coordinate,
                new HexCoordinate(2, 0)
            };

            var destinations = AdjacentDestinationQuery.FindEmptyAdjacentDestinations(
                hero,
                board,
                new[] { hero });

            Assert.That(destinations, Is.EquivalentTo(hero.Coordinate.GetNeighbors()));
            Assert.That(destinations, Has.No.Member(hero.Coordinate));
            Assert.That(destinations, Has.No.Member(new HexCoordinate(2, 0)));
        }

        [Test]
        public void FindEmptyAdjacentDestinations_ExcludesOccupiedAdjacentSpaces()
        {
            var hero = new HeroOccupancy(new HeroId("selected"), new HexCoordinate(0, 0));
            var occupiedNeighbor = hero.Coordinate.GetNeighbor(HexDirection.East);
            var blocker = new HeroOccupancy(new HeroId("blocker"), occupiedNeighbor);
            var board = new List<HexCoordinate>(hero.Coordinate.GetNeighbors())
            {
                hero.Coordinate
            };

            var destinations = AdjacentDestinationQuery.FindEmptyAdjacentDestinations(
                hero,
                board,
                new[] { hero, blocker });

            Assert.That(destinations, Has.No.Member(occupiedNeighbor));
            Assert.That(destinations, Has.Count.EqualTo(5));
        }

        [Test]
        public void FindEmptyAdjacentDestinations_ReturnsNoneWhenSelectedHeroIsOffBoard()
        {
            var hero = new HeroOccupancy(new HeroId("selected"), new HexCoordinate(0, 0));

            var destinations = AdjacentDestinationQuery.FindEmptyAdjacentDestinations(
                hero,
                new[] { new HexCoordinate(1, 0) },
                new[] { hero });

            Assert.That(destinations, Is.Empty);
        }
    }
}
