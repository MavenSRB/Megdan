using GuardsDigital.Core.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Rulebook p.19's straight line, which milestone 6.4's Object Push consumes to find the direction
    /// "directly away from the pushing unit". Purely geometric: no board, no occupancy, no permission.
    /// </summary>
    public sealed class StraightLineQueryTests
    {
        private static readonly HexDirection[] AllDirections =
        {
            HexDirection.East,
            HexDirection.NorthEast,
            HexDirection.NorthWest,
            HexDirection.West,
            HexDirection.SouthWest,
            HexDirection.SouthEast
        };

        [Test]
        public void AdjacentSpaces_AreInAStraightLineInEveryDirection()
        {
            var origin = new HexCoordinate(0, 0);

            foreach (var direction in AllDirections)
            {
                var neighbor = origin.GetNeighbor(direction);

                Assert.That(
                    StraightLineQuery.TryGetDirection(origin, neighbor, out var found, out var distance),
                    Is.True,
                    $"p.19: two units are in a straight line if they are adjacent ({direction}).");
                Assert.That(found, Is.EqualTo(direction));
                Assert.That(distance, Is.EqualTo(1));
            }
        }

        [Test]
        public void DistantSpacesOnOneFile_ReportThatFileAndItsSpaceCount()
        {
            var origin = new HexCoordinate(0, 0);

            foreach (var direction in AllDirections)
            {
                var far = StraightLineQuery.Step(origin, direction, 4);

                Assert.That(
                    StraightLineQuery.TryGetDirection(origin, far, out var found, out var distance),
                    Is.True,
                    "p.19: a straight line of spaces can be drawn through both.");
                Assert.That(found, Is.EqualTo(direction));
                Assert.That(distance, Is.EqualTo(4));
            }
        }

        [Test]
        public void ASpaceOffEveryFile_IsNotInAStraightLine()
        {
            var origin = new HexCoordinate(0, 0);

            // Two steps East then one NorthEast: reachable, but on no single file from the origin.
            var offAxis = new HexCoordinate(3, -1);

            Assert.That(StraightLineQuery.AreInStraightLine(origin, offAxis), Is.False);
            Assert.That(
                StraightLineQuery.TryGetDirection(origin, offAxis, out _, out var distance), Is.False);
            Assert.That(distance, Is.EqualTo(0), "A refusal reports no usable distance.");
        }

        [Test]
        public void TheSameSpace_HasNoDirection()
        {
            var origin = new HexCoordinate(2, -3);

            Assert.That(
                StraightLineQuery.TryGetDirection(origin, origin, out _, out _),
                Is.False,
                "p.18 admits no second obstacle in a space, so two objects never share one and " +
                "a caller asking for the direction between an object and itself has none to be given.");
        }

        [Test]
        public void StepZero_IsTheOriginItself()
        {
            var origin = new HexCoordinate(-1, 4);

            Assert.That(
                StraightLineQuery.Step(origin, HexDirection.SouthWest, 0),
                Is.EqualTo(origin),
                "A zero-space push (p.18) resolves to the space the target already stands on.");
        }

        [Test]
        public void StepIsTheInverseOfTheDirectionItReports()
        {
            var origin = new HexCoordinate(5, -2);

            foreach (var direction in AllDirections)
            {
                for (var steps = 1; steps <= 3; steps++)
                {
                    var stepped = StraightLineQuery.Step(origin, direction, steps);

                    Assert.That(
                        StraightLineQuery.TryGetDirection(origin, stepped, out var found, out var distance),
                        Is.True);
                    Assert.That(found, Is.EqualTo(direction));
                    Assert.That(distance, Is.EqualTo(steps));
                }
            }
        }
    }
}
