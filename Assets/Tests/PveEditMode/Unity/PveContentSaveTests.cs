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
                Assert.That(loaded.OrangeCards.Select(card => card.Skill), Is.EqualTo(type.OrangeCards.Select(card => card.Skill)));
                Assert.That(loaded.OrangeCards.Select(card => card.RepeatOnDifferentTarget), Is.EqualTo(type.OrangeCards.Select(card => card.RepeatOnDifferentTarget)));
                Assert.That(loaded.PurpleCards.Select(card => card.Counterattack), Is.EqualTo(type.PurpleCards.Select(card => card.Counterattack)));
                Assert.That(loaded.MinionCategory, Is.EqualTo(type.MinionCategory));
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
        public void SkillContentRefusesUnknownOrMixedActions()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out _);
            var json = PveContentLoader.SaveCatalog(catalog);
            Assert.Throws<System.FormatException>(() => PveContentLoader.LoadCatalog(
                json.Replace("immune-this-turn", "unknown-skill")));
            var melee = catalog.Find("melee");
            foreach (var badCard in new[]
            {
                new PveOrangeCard("X", "Invalid", 9, 1, null, 0, null, null, true, PveSkillKind.ImmuneThisTurn),
                new PveOrangeCard("X", "Invalid", 9, 1, 3, 0, null,
                    new[] { new PveCardOption(PveOptionKind.Attack, PveTargetKind.Hero) }, skill: PveSkillKind.ImmuneThisTurn),
                new PveOrangeCard("X", "Invalid", 9, 1, null, 0, null, null, skill: (PveSkillKind)99)
            })
            {
                var invalid = new PveEnemyType(melee.Id, melee.DisplayName, melee.Level,
                    melee.OrangeCards.Select(card => card.Key == "X" ? badCard : card), melee.PurpleCards,
                    melee.Plans, melee.TokenType, melee.MinionCategory);
                Assert.That(PveContentValidator.ValidateCatalog(new PveEnemyCatalog("placeholder-test", new[] { invalid })),
                    Is.Not.Empty);
            }
        }

        [Test]
        public void CombinedUnitTargetIsMovementOnly()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out _);
            var json = PveContentLoader.SaveCatalog(catalog);
            Assert.That(PveContentValidator.ValidateCatalog(PveContentLoader.LoadCatalog(
                json.Replace("\"hero\"", "\"unit\""))), Has.Some.Contains("unit is supported only for movement"));
        }

        [Test]
        public void CardEditorPreservesTokenIdentityAndUnknownTokenIsRefused()
        {
            PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out _);
            var editor = new PveCardEditor(() => catalog, null);
            editor.Build();
            Assert.That(editor.Edited().Find("heavy").FindOrange("C").RepeatOnDifferentTarget, Is.True);
            Assert.That(editor.Edited().Find("heavy").FindOrange("X").Skill, Is.EqualTo(PveSkillKind.HookClosestBlueUnit));
            Assert.That(editor.Edited().Find("heavy").PurpleCards.First().Counterattack, Is.EqualTo(1));
            Assert.That(editor.Edited().Find("melee").FindOrange("X").Skill, Is.EqualTo(PveSkillKind.ImmuneThisTurn));
            Assert.That(editor.Edited().Find("ranged").FindOrange("X").Skill, Is.EqualTo(PveSkillKind.RangeNextTurn));
            Assert.That(editor.Edited().Find("ranged").FindOrange("A").Name, Is.EqualTo("Take Position"));
            Assert.That(editor.Edited().Types.Select(type => type.MinionCategory),
                Is.EqualTo(catalog.Types.Select(type => type.MinionCategory)));
            Assert.That(editor.Edited().Types.Select(type => type.TokenType),
                Is.EqualTo(catalog.Types.Select(type => type.TokenType)));
            Assert.That(editor.Edited().Types.Select(type => type.PurpleCards.Count),
                Is.EqualTo(new[] { 0, 0, 2 }));
            Assert.Throws<System.FormatException>(() => PveContentLoader.LoadCatalog(
                PveContentLoader.SaveCatalog(catalog).Replace("\"tokenType\": \"melee\"", "\"tokenType\": \"unknown\"")));
        }
    }
}
