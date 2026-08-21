using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.Movement;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.6's orbit reading: "without moving it away from you or closer to you", which is
    /// rulebook p.18's `Closer (Space)` negated in both directions. Purely geometric, exactly as
    /// <c>CloserQueryTests</c> is: no board, no occupancy, no reachability.
    /// </summary>
    public sealed class EquidistantQueryTests
    {
        private static readonly HexCoordinate Source = new HexCoordinate(0, 0);

        [Test]
        public void ASpaceIsEquidistant_ExactlyWhenItIsNeitherCloserNorFarther()
        {
            var origin = new HexCoordinate(2, 0);
            var sameRing = new HexCoordinate(1, 1);
            var closer = new HexCoordinate(1, 0);
            var farther = new HexCoordinate(3, 0);

            Assert.That(HexDistanceQuery.Calculate(Source, sameRing), Is.EqualTo(2),
                "The fixture's own arithmetic, stated so the case is readable.");
            Assert.That(EquidistantQuery.IsEquidistantFrom(Source, sameRing, origin), Is.True);
            Assert.That(EquidistantQuery.IsEquidistantFrom(Source, closer, origin), Is.False,
                "p.18's Closer is one of the two things the card excludes.");
            Assert.That(EquidistantQuery.IsEquidistantFrom(Source, farther, origin), Is.False,
                "and Farther is the other.");
        }

        [Test]
        public void TheOriginItself_IsEquidistantWithItself()
        {
            var origin = new HexCoordinate(-3, 1);

            Assert.That(EquidistantQuery.IsEquidistantFrom(Source, origin, origin), Is.True,
                "A space is exactly as far from the source as itself. Whether standing still is a " +
                "legal result is the node's failure policy's decision (p.11), not this query's, so " +
                "the honest geometric answer is given and nothing is quietly removed.");
        }

        [Test]
        public void TheDistanceCountsThroughObstacles_LikeEveryOtherRulebookDistance()
        {
            // p.18 counts spaces "including spaces with obstacles", so this query cannot be a route
            // length - and it is not, because it never sees a board at all. Two spaces on opposite
            // sides of the source are equidistant however impassable everything between them is.
            var origin = new HexCoordinate(3, 0);
            var acrossTheSource = new HexCoordinate(-3, 0);

            Assert.That(EquidistantQuery.IsEquidistantFrom(Source, acrossTheSource, origin), Is.True);
        }

        [Test]
        public void TheFilter_KeepsOnlyTheRingAndOrdersItCanonically()
        {
            var origin = new HexCoordinate(2, 0);
            var candidates = new[]
            {
                new HexCoordinate(1, 1),
                new HexCoordinate(1, 0),
                new HexCoordinate(3, 0),
                new HexCoordinate(2, -2),
                new HexCoordinate(0, 2)
            };

            var kept = EquidistantQuery.FilterEquidistantTo(Source, origin, candidates);

            Assert.That(
                kept.All(space => HexDistanceQuery.Calculate(Source, space) == 2), Is.True,
                "Everything kept is on the ring the mover already stood on.");
            Assert.That(kept, Is.EqualTo(kept.OrderBy(space => space.Q).ThenBy(space => space.R)),
                "and the order is canonical, because a projected candidate list has to be " +
                "deterministic across replay.");
            Assert.That(kept, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 2),
                new HexCoordinate(1, 1),
                new HexCoordinate(2, -2)
            }));
        }
    }
}
