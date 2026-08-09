using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HexCoordinateTests
    {
        private static IEnumerable<TestCaseData> NeighborDirections
        {
            get
            {
                yield return new TestCaseData(HexDirection.East, 3, -1);
                yield return new TestCaseData(HexDirection.NorthEast, 3, -2);
                yield return new TestCaseData(HexDirection.NorthWest, 2, -2);
                yield return new TestCaseData(HexDirection.West, 1, -1);
                yield return new TestCaseData(HexDirection.SouthWest, 1, 0);
                yield return new TestCaseData(HexDirection.SouthEast, 2, 0);
            }
        }

        [TestCaseSource(nameof(NeighborDirections))]
        public void GetNeighbor_ReturnsCoordinateInRequestedDirection(
            HexDirection direction,
            int expectedQ,
            int expectedR)
        {
            var origin = new HexCoordinate(2, -1);

            var neighbor = origin.GetNeighbor(direction);

            Assert.That(neighbor, Is.EqualTo(new HexCoordinate(expectedQ, expectedR)));
        }

        [Test]
        public void GetNeighbors_ReturnsAllSixUniqueAdjacentCoordinates()
        {
            var origin = new HexCoordinate(2, -1);

            var neighbors = origin.GetNeighbors();

            Assert.That(neighbors.Count, Is.EqualTo(6));
            Assert.That(neighbors.Distinct().Count(), Is.EqualTo(6));
            Assert.That(neighbors, Is.EquivalentTo(new[]
            {
                new HexCoordinate(3, -1),
                new HexCoordinate(3, -2),
                new HexCoordinate(2, -2),
                new HexCoordinate(1, -1),
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0)
            }));
        }

        [Test]
        public void GetNeighbor_RejectsUnknownDirection()
        {
            var origin = new HexCoordinate(0, 0);

            Assert.That(
                () => origin.GetNeighbor((HexDirection)99),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
