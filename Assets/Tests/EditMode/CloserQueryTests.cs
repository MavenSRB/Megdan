using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.Movement;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Rulebook p.18's `Closer (Space)`, which milestone 6.4's direct Move consumes as its
    /// source-relative destination policy. The rule counts spaces "including spaces with obstacles",
    /// so these cases pin that it is geometric separation and never a reachable-route length.
    /// </summary>
    public sealed class CloserQueryTests
    {
        [Test]
        public void ASpaceFewerSpacesFromTheSource_IsCloser()
        {
            var source = new HexCoordinate(0, 0);
            var origin = new HexCoordinate(3, 0);
            var candidate = new HexCoordinate(2, 0);

            Assert.That(CloserQuery.IsCloserTo(source, candidate, origin), Is.True);
        }

        [Test]
        public void EqualSeparation_IsNotCloser()
        {
            var source = new HexCoordinate(0, 0);
            var origin = new HexCoordinate(2, 0);
            var sideways = new HexCoordinate(2, -2);

            Assert.That(
                HexDistanceQuery.Calculate(source, sideways),
                Is.EqualTo(HexDistanceQuery.Calculate(source, origin)),
                "The fixture needs a genuinely equal-distance space, or the case proves nothing.");
            Assert.That(
                CloserQuery.IsCloserTo(source, sideways, origin),
                Is.False,
                "A sideways step around the source is refused by a `towards` policy exactly as an " +
                "outward one is; p.18 requires fewer spaces, not the same number.");
        }

        [Test]
        public void AFartherSpace_IsNotCloser()
        {
            var source = new HexCoordinate(0, 0);
            var origin = new HexCoordinate(2, 0);
            var farther = new HexCoordinate(3, 0);

            Assert.That(CloserQuery.IsCloserTo(source, farther, origin), Is.False);
        }

        [Test]
        public void ClosenessCountsThroughObstacles_NotAlongALegalRoute()
        {
            // The straight separation from the source is two spaces whether or not anything stands
            // between them. p.18 says the count includes spaces with obstacles, so this query never
            // consults the board - which is why it takes no board at all.
            var source = new HexCoordinate(0, 0);
            var origin = new HexCoordinate(4, 0);
            var behindAnObstacle = new HexCoordinate(2, 0);

            Assert.That(CloserQuery.IsCloserTo(source, behindAnObstacle, origin), Is.True);
        }

        [Test]
        public void FilterCloserTo_KeepsOnlyStrictlyCloserCandidatesInDeterministicOrder()
        {
            var source = new HexCoordinate(0, 0);
            var origin = new HexCoordinate(3, 0);
            var candidates = new[]
            {
                new HexCoordinate(4, 0),  // farther
                new HexCoordinate(1, 0),  // closer
                new HexCoordinate(3, -3), // equal separation of 3
                new HexCoordinate(2, 0)   // closer
            };

            var filtered = CloserQuery.FilterCloserTo(source, origin, candidates);

            Assert.That(
                filtered.ToArray(),
                Is.EqualTo(new[] { new HexCoordinate(1, 0), new HexCoordinate(2, 0) }),
                "Ordered by axial coordinate so a projected candidate list is stable under replay.");
        }

        [Test]
        public void FilterCloserTo_WithNoCloserCandidate_IsEmpty()
        {
            var source = new HexCoordinate(0, 0);
            var origin = new HexCoordinate(1, 0);

            var filtered = CloserQuery.FilterCloserTo(
                source, origin, new[] { new HexCoordinate(2, 0), new HexCoordinate(3, 0) });

            Assert.That(filtered, Is.Empty);
        }
    }
}
