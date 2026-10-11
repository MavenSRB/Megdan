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
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var stated, out _);
            var reloaded = PveContentLoader.LoadCatalog(PveContentLoader.SaveCatalog(stated));

            Assert.That(PveContentValidator.ValidateCatalog(reloaded), Is.Empty);
            Assert.That(reloaded.Types.Select(type => type.Id), Is.EqualTo(stated.Types.Select(type => type.Id)));
            foreach (var type in stated.Types)
            {
                var loaded = reloaded.Find(type.Id);
                Assert.That(loaded.TokenType, Is.EqualTo(type.TokenType));
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

        [Test]
        public void CardEditorPreservesTokenIdentityAndUnknownTokenIsRefused()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out _);
            var editor = new PveCardEditor(() => catalog, null);
            editor.Build();
            Assert.That(editor.Edited().Types.Select(type => type.TokenType),
                Is.EqualTo(catalog.Types.Select(type => type.TokenType)));
            Assert.That(editor.Edited().Types.Select(type => type.PurpleCards.Count),
                Is.EqualTo(new[] { 0, 0, 2 }));
            Assert.Throws<System.FormatException>(() => PveContentLoader.LoadCatalog(
                PveContentLoader.SaveCatalog(catalog).Replace("\"tokenType\": \"melee\"", "\"tokenType\": \"unknown\"")));
        }
    }
}
