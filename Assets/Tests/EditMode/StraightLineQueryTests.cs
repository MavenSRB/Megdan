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

        // ---------------------------------------------------------------------------------------
        // Milestone 8.6: p.19's sentence stated whole, and read as a complement
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ASingleSpace_IsAStraightLineAndIsInAStraightLineWithItself()
        {
            var origin = new HexCoordinate(2, -3);

            Assert.That(
                StraightLineQuery.IsStraightLine(new[] { origin }), Is.True,
                "p.19 states it outright: a single space counts as a straight line.");
            Assert.That(StraightLineQuery.AreInStraightLine(origin, origin), Is.True,
                "and membership follows from that, which is the reading the complement below needs.");
            Assert.That(
                StraightLineQuery.TryGetDirection(origin, origin, out _, out _), Is.False,
                "while the direction question still has no answer - a line of one space names no " +
                "file to walk along, and p.18's Push is the caller that asks.");
            Assert.That(
                StraightLineQuery.IsStraightLine(new HexCoordinate[0]), Is.False,
                "No spaces at all is not a line: p.19 describes spaces arranged in a file.");
        }

        [Test]
        public void ASequenceIsALine_OnlyWhenEveryStepFollowsTheSameFile()
        {
            var origin = new HexCoordinate(0, 0);

            Assert.That(
                StraightLineQuery.IsStraightLine(new[]
                {
                    origin,
                    StraightLineQuery.Step(origin, HexDirection.East, 1),
                    StraightLineQuery.Step(origin, HexDirection.East, 2)
                }),
                Is.True, "Three spaces arranged in a file.");
            Assert.That(
                StraightLineQuery.IsStraightLine(new[]
                {
                    origin,
                    StraightLineQuery.Step(origin, HexDirection.East, 2)
                }),
                Is.False, "A file with a gap in it is not a sequence of spaces.");
            Assert.That(
                StraightLineQuery.IsStraightLine(new[]
                {
                    origin,
                    StraightLineQuery.Step(origin, HexDirection.East, 1),
                    origin.GetNeighbor(HexDirection.East).GetNeighbor(HexDirection.NorthEast)
                }),
                Is.False, "and a bend is not one file.");
            Assert.That(
                StraightLineQuery.IsStraightLine(new[] { origin, origin }), Is.False,
                "nor is a repeated space, which p.19's one-obstacle-per-space rule forbids anyway.");
        }

        [Test]
        public void TheComplement_DropsEveryFileThroughTheOriginIncludingItsOwnSpace()
        {
            var origin = new HexCoordinate(0, 0);
            var inLine = StraightLineQuery.Step(origin, HexDirection.NorthEast, 2);
            var adjacent = origin.GetNeighbor(HexDirection.West);
            var offAxis = origin.GetNeighbor(HexDirection.East).GetNeighbor(HexDirection.NorthEast);

            var kept = StraightLineQuery.FilterOutStraightLine(
                origin, new[] { origin, inLine, adjacent, offAxis });

            Assert.That(kept, Is.EqualTo(new[] { offAxis }),
                "`not in a straight line` removes the whole file, the adjacent space p.19 names by " +
                "hand, and the origin itself - and keeps exactly what is off every axis.");
        }

        [Test]
        public void TheComplement_KeepsTheTwoOffAxisSpacesOfEveryDistanceTwoRing()
        {
            var origin = new HexCoordinate(-4, 1);
            var ring = new System.Collections.Generic.List<HexCoordinate>();
            for (var dq = -2; dq <= 2; dq++)
            {
                for (var dr = System.Math.Max(-2, -dq - 2); dr <= System.Math.Min(2, -dq + 2); dr++)
                {
                    var candidate = new HexCoordinate(origin.Q + dq, origin.R + dr);
                    if (HexDistanceQuery.Calculate(origin, candidate) == 2)
                    {
                        ring.Add(candidate);
                    }
                }
            }

            Assert.That(ring, Has.Count.EqualTo(12), "A distance-two ring has twelve spaces.");
            Assert.That(
                StraightLineQuery.FilterOutStraightLine(origin, ring), Has.Count.EqualTo(6),
                "Six of them are two steps along one of the six files, and the other six are not - " +
                "which is the shape a `not in a straight line` card is actually offered.");
        }
    }
}
