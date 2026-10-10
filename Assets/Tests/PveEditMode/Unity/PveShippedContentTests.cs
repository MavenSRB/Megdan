using System.Linq;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Presentation;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-021, PVE-D15/D17. The JSON shipped beside the executable loads, validates against the
    /// official map, carries the approved placement layout and is the same placeholder content the
    /// engine-free tests state in code.
    /// </summary>
    public sealed class PveShippedContentTests
    {
        [Test]
        public void TheShippedContentLoadsAndValidates()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var encounter);

            Assert.That(PveContentValidator.Validate(catalog, encounter, PveTestContent.OfficialMap()), Is.Empty);
            Assert.That(catalog.ContentStatus, Is.EqualTo("placeholder-test"));
        }

        [Test]
        public void TheShippedLayoutIsTheApprovedOne()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out _, out var shipped);
            var approved = PveTestContent.Encounter();

            Assert.That(shipped.MapReference, Is.EqualTo(approved.MapReference));
            Assert.That(shipped.Creeps.Select(creep => (creep.Coordinate, creep.MinionType)),
                Is.EqualTo(approved.Creeps.Select(creep => (creep.Coordinate, creep.MinionType))));
            Assert.That(shipped.HeroPlacement, Is.EqualTo(approved.HeroPlacement));
            Assert.That(shipped.Spawns.Select(spawn => (spawn.Round, spawn.Coordinate, spawn.EnemyTypeId)),
                Is.EqualTo(approved.Spawns.Select(spawn => (spawn.Round, spawn.Coordinate, spawn.EnemyTypeId))));
        }

        [Test]
        public void TheShippedEnemiesAreThePlaceholderTypes()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var shipped, out _);
            var stated = PveTestContent.Catalog();

            Assert.That(shipped.Types.Select(type => type.Id), Is.EqualTo(stated.Types.Select(type => type.Id)));
            foreach (var type in stated.Types)
            {
                var loaded = shipped.Find(type.Id);
                Assert.That(loaded.Plans.Select(plan => string.Join(" ", plan)),
                    Is.EqualTo(type.Plans.Select(plan => string.Join(" ", plan))), type.Id);
                Assert.That(loaded.OrangeCards.Select(Describe), Is.EqualTo(type.OrangeCards.Select(Describe)), type.Id);
                Assert.That(loaded.PurpleCards.Select(card => (card.Key, card.Initiative, card.Defense)),
                    Is.EqualTo(type.PurpleCards.Select(card => (card.Key, card.Initiative, card.Defense))), type.Id);
            }
        }

        private static string Describe(PveOrangeCard card) =>
            $"{card.Key}/{card.Initiative}/{card.Copies}/{card.AttackValue}/{card.AttackRange}/{card.MoveValue}/" +
            $"{card.IsIdle}/" + string.Join(",", card.Options.Select(option =>
                option.Kind + ":" + option.Target + ":" + option.StopDistance));
    }
}
