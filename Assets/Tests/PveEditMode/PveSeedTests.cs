using System.Linq;
using GuardsDigital.Pve.Composition;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-021, PVE-D13. Plan selection is a pure function of seed, round and enemy, identical on every
    /// runtime: the pinned values below were computed once and must never move.
    /// </summary>
    public sealed class PveSeedTests
    {
        [Test]
        public void StableHashAndPlanIndexArePinned()
        {
            Assert.That(PveSeed.StableHash("pve-enemy-01"), Is.EqualTo(PveSeed.StableHash("pve-enemy-01")));
            Assert.That(PveSeed.StableHash(string.Empty), Is.EqualTo(0xCBF29CE484222325UL));
            Assert.That(PveSeed.Mix(0UL), Is.EqualTo(0xE220A8397B1DCDAFUL));
            Assert.That(
                Enumerable.Range(1, 8).Select(round => PveSeed.PlanIndex(1234, round, "pve-enemy-01", 3)),
                Is.EqualTo(PinnedIndices));
        }

        [Test]
        public void EveryListedPlanIsReachableAndNoneDominates()
        {
            var counts = new int[3];
            for (var enemy = 1; enemy <= 30; enemy++)
            {
                for (var round = 1; round <= 30; round++)
                {
                    counts[PveSeed.PlanIndex(99, round, "pve-enemy-" + enemy.ToString("00"), 3)]++;
                }
            }

            Assert.That(counts.All(count => count > 240 && count < 360), Is.True, string.Join(",", counts));
        }

        [Test]
        public void AZeroSeedIsNormalizedRatherThanUnrecorded()
        {
            Assert.That(PveSeed.Normalize(0UL), Is.Not.EqualTo(0UL));
            Assert.That(PveSeed.Normalize(42UL), Is.EqualTo(42UL));
        }

        private static readonly int[] PinnedIndices = { 2, 2, 1, 0, 0, 2, 0, 1 };
    }
}
