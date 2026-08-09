using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HexWorldLayoutTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void ToWorld_MapsOriginToXZPlaneOrigin()
        {
            var worldPosition = HexWorldLayout.ToWorld(new HexCoordinate(0, 0));

            Assert.That(Vector3.Distance(worldPosition, Vector3.zero), Is.LessThan(Tolerance));
        }

        [Test]
        public void ToWorld_MapsNamedDirectionsToExpectedXZPositions()
        {
            var origin = new HexCoordinate(0, 0);
            var horizontalStep = Mathf.Sqrt(3f);
            var expectedPositions = new[]
            {
                new Vector3(horizontalStep, 0f, 0f),
                new Vector3(horizontalStep * 0.5f, 0f, 1.5f),
                new Vector3(horizontalStep * -0.5f, 0f, 1.5f),
                new Vector3(-horizontalStep, 0f, 0f),
                new Vector3(horizontalStep * -0.5f, 0f, -1.5f),
                new Vector3(horizontalStep * 0.5f, 0f, -1.5f)
            };

            for (var directionIndex = 0; directionIndex < expectedPositions.Length; directionIndex++)
            {
                var coordinate = origin.GetNeighbor((HexDirection)directionIndex);
                var worldPosition = HexWorldLayout.ToWorld(coordinate);

                Assert.That(
                    Vector3.Distance(worldPosition, expectedPositions[directionIndex]),
                    Is.LessThan(Tolerance),
                    $"Unexpected position for {(HexDirection)directionIndex}.");
            }
        }

        [Test]
        public void AdjacentCenters_AreSeparatedByTwiceTheMeshApothem()
        {
            const float outerRadius = 2.5f;
            var origin = new HexCoordinate(0, 0);
            var originPosition = HexWorldLayout.ToWorld(origin, outerRadius);
            var expectedCenterDistance = 2f * outerRadius * Mathf.Cos(30f * Mathf.Deg2Rad);

            foreach (var neighbor in origin.GetNeighbors())
            {
                var neighborPosition = HexWorldLayout.ToWorld(neighbor, outerRadius);
                Assert.That(
                    Vector3.Distance(originPosition, neighborPosition),
                    Is.EqualTo(expectedCenterDistance).Within(Tolerance));
            }
        }

        [Test]
        public void NeighborPositions_AreUniqueAndRemainOnXZPlane()
        {
            var origin = new HexCoordinate(0, 0);
            var positions = origin.GetNeighbors()
                .Select(coordinate => HexWorldLayout.ToWorld(coordinate))
                .ToArray();

            Assert.That(positions.Distinct().Count(), Is.EqualTo(6));
            Assert.That(positions.All(position => Mathf.Abs(position.y) < Tolerance), Is.True);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void ToWorld_RejectsNonPositiveOuterRadius(float outerRadius)
        {
            Assert.That(
                () => HexWorldLayout.ToWorld(new HexCoordinate(0, 0), outerRadius),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
