using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.Movement;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.4a's nearest-space query: every candidate at the minimal geometric distance, in
    /// canonical axial order. Ties are returned rather than broken, because whoever the rules make
    /// choose between equally near spaces is a chooser, not this query.
    /// </summary>
    public sealed class NearestQueryTests
    {
        [Test]
        public void EveryEquallyNearestCandidate_IsReturnedInCanonicalOrder()
        {
            var nearest = NearestQuery.FindNearest(
                new HexCoordinate(0, 0),
                new[]
                {
                    new HexCoordinate(2, 0),
                    new HexCoordinate(0, 2),
                    new HexCoordinate(-3, 0),
                    new HexCoordinate(1, 1)
                });

            Assert.That(nearest, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 2), new HexCoordinate(1, 1), new HexCoordinate(2, 0)
            }), "The three candidates at distance 2 tie; the one at 3 is dropped; axial order.");
        }

        [Test]
        public void DistanceIsGeometric_CountingThroughAnythingInTheWay()
        {
            // p.18's Near/Far counts spaces including spaces with obstacles, so this is
            // HexDistanceQuery and never a route length; a caller needing path closeness walks a path
            // query instead.
            var nearest = NearestQuery.FindNearest(
                new HexCoordinate(0, 0),
                new[] { new HexCoordinate(1, 0), new HexCoordinate(5, 0) });

            Assert.That(nearest, Is.EqualTo(new[] { new HexCoordinate(1, 0) }));
        }

        [Test]
        public void NoCandidates_ReturnsEmptyRatherThanThrowing()
        {
            Assert.That(
                NearestQuery.FindNearest(new HexCoordinate(0, 0), Array.Empty<HexCoordinate>()),
                Is.Empty);
        }
    }
}
