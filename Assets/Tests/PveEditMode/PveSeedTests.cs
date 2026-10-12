using System.Linq;
using GuardsDigital.Pve.Play;
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
        public void ShuffledPlansReachAllOrdersAndPreserveEveryCopyWithoutDefense()
        {
            var encounter = PveTestDriver.Start().Composed;
            var enemy = encounter.Enemies.First(value => value.Type.Id == "brute");
            var orders = Enumerable.Range(1, 2400).Select(round =>
            {
                var plan = PvePlans.PlanFor(encounter, enemy, round);
                Assert.That(plan, Is.EquivalentTo(enemy.Type.OrangeHand));
                Assert.That(plan, Is.EqualTo(PvePlans.PlanFor(encounter, enemy, round)));
                return string.Join(" ", plan);
            }).GroupBy(value => value).ToArray();
            Assert.That(orders.Length, Is.EqualTo(24), "Not restricted to legacy plans or movement first.");
            Assert.That(orders.All(group => group.Count() > 60 && group.Count() < 140), Is.True);
            var repeated = encounter.Enemies.First(value => value.Type.Id == "raider");
            for (var round = 1; round <= 100; round++)
                Assert.That(PvePlans.PlanFor(encounter, repeated, round), Is.EquivalentTo(repeated.Type.OrangeHand));
        }

        [Test]
        public void ShuffleDependsOnSeedRoundAndEnemyButNotReadOrder()
        {
            var encounter = PveTestDriver.Start().Composed;
            var clone = PveTestDriver.Start().Composed;
            var differentSeed = PveTestDriver.Start(seed: 4321).Composed;
            var enemy = encounter.Enemies[0];
            var other = encounter.Enemies.First(value => value.Type.Id == enemy.Type.Id && value != enemy);
            var orders = Enumerable.Range(1, 30).Select(round => string.Join(" ", PvePlans.PlanFor(encounter, enemy, round))).ToArray();
            Assert.That(orders.Distinct().Count(), Is.GreaterThan(1));
            Assert.That(orders, Is.EqualTo(Enumerable.Range(1, 30).Select(round => string.Join(" ", PvePlans.PlanFor(clone, clone.Enemies[0], round)))));
            Assert.That(orders, Is.Not.EqualTo(Enumerable.Range(1, 30).Select(round => string.Join(" ", PvePlans.PlanFor(differentSeed, differentSeed.Enemies[0], round)))));
            Assert.That(orders, Is.Not.EqualTo(Enumerable.Range(1, 30).Select(round => string.Join(" ", PvePlans.PlanFor(encounter, other, round)))));
        }

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
