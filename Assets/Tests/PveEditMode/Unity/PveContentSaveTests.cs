using System.Linq;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Presentation;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>W-023. The enemy card editor's save writes JSON that loads back to the same content.</summary>
    public sealed class PveContentSaveTests
    {
        [Test]
        public void SavedEnemyContentLoadsBackUnchanged()
        {
            var stated = PveTestContent.Catalog();
            var reloaded = PveContentLoader.LoadCatalog(PveContentLoader.SaveCatalog(stated));

            Assert.That(PveContentValidator.ValidateCatalog(reloaded), Is.Empty);
            Assert.That(reloaded.Types.Select(type => type.Id), Is.EqualTo(stated.Types.Select(type => type.Id)));
            foreach (var type in stated.Types)
            {
                var loaded = reloaded.Find(type.Id);
                Assert.That(loaded.OrangeCards.Select(card =>
                        (card.Key, card.Initiative, card.Copies, card.AttackValue, card.AttackRange, card.MoveValue, card.IsIdle,
                            string.Join(",", card.Options.Select(option => option.Kind + ":" + option.Target + ":" + option.StopDistance)))),
                    Is.EqualTo(type.OrangeCards.Select(card =>
                        (card.Key, card.Initiative, card.Copies, card.AttackValue, card.AttackRange, card.MoveValue, card.IsIdle,
                            string.Join(",", card.Options.Select(option => option.Kind + ":" + option.Target + ":" + option.StopDistance))))));
                Assert.That(loaded.PurpleCards.Select(card => (card.Key, card.Initiative, card.Defense)),
                    Is.EqualTo(type.PurpleCards.Select(card => (card.Key, card.Initiative, card.Defense))));
                Assert.That(loaded.Plans.Select(plan => string.Join(" ", plan)),
                    Is.EqualTo(type.Plans.Select(plan => string.Join(" ", plan))));
            }
        }
    }
}
