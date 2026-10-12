using System.IO;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.Pve
{
    public sealed class PveEnemyArtworkTests
    {
        private static string Json => File.ReadAllText(Path.Combine(PveDataPaths.Directory, PveDataPaths.EnemiesFileName));

        [Test]
        public void AllFourteenApprovedFacesMapToTheirExactDefinitions()
        {
            var catalog = PveContentLoader.LoadCatalog(Json);
            var paths = PveEnemyArtwork.ResourcesFor(catalog);
            Assert.That(paths.Count, Is.EqualTo(14));
            Assert.That(paths.Keys, Is.EquivalentTo(catalog.Types.SelectMany(PveCardFactory.CreateDefinitions)
                .Select(definition => definition.Reference)));
            Assert.That(paths[new CardDefinitionReference(new CardDefinitionId("pve-ranged-x"), PveCardFactory.ContentRevision)],
                Is.EqualTo("Private/PveCards/ranged-aim"));
            PveEnemyArtwork.Configure(catalog);
            try
            {
                foreach (var pair in paths)
                {
                    Assert.That(ConfiguredCardArtwork.TryGetResourcePath(pair.Key, out var path), Is.True);
                    Assert.That(path, Is.EqualTo(pair.Value));
                    // Private resource presence is exercised in the player-view fixture; clean source checkouts retain fallback.
                    using (ConfiguredCardArtwork.UseTextureLoader(_ => null))
                        Assert.That(ConfiguredCardArtwork.TryLoadTexture(pair.Key, out _, out _), Is.False);
                }
            }
            finally { ConfiguredCardArtwork.SetModeResources(null); }
        }

        [Test]
        public void EditedValuesOrPrioritiesDoNotDisplayStaleBakedFaces()
        {
            var changed = PveContentLoader.LoadCatalog(Json.Replace("\"initiative\": 8", "\"initiative\": 10"));
            Assert.That(PveEnemyArtwork.ResourcesFor(changed).Count, Is.EqualTo(13));
            var stopChanged = PveContentLoader.LoadCatalog(Json.Replace("\"stopDistance\": 2", "\"stopDistance\": 3"));
            Assert.That(PveEnemyArtwork.ResourcesFor(stopChanged).Count, Is.EqualTo(12));
        }

        [Test]
        public void EnemyCardCategoriesUseTheFiveApprovedPresentationColours()
        {
            var catalog = PveContentLoader.LoadCatalog(Json);
            foreach (var type in catalog.Types)
            {
                Assert.That(PveEnemyArtwork.DisplayColor(type,
                    type.OrangeCards.Single(card => !card.AttackValue.HasValue && card.MoveValue.HasValue)),
                    Is.EqualTo(CardColor.Green), type.Id + " movement");
                Assert.That(PveEnemyArtwork.DisplayColor(type,
                    type.OrangeCards.Single(card => card.Skill != PveSkillKind.None)),
                    Is.EqualTo(CardColor.Silver), type.Id + " skill");
                var attacks = type.OrangeCards.Where(card => card.AttackValue.HasValue)
                    .OrderBy(card => card.AttackValue.Value).ToArray();
                Assert.That(PveEnemyArtwork.DisplayColor(type, attacks[0]), Is.EqualTo(CardColor.Gold),
                    type.Id + " weaker attack");
                Assert.That(PveEnemyArtwork.DisplayColor(type, attacks[1]), Is.EqualTo(CardColor.Red),
                    type.Id + " stronger attack");
                Assert.That(type.PurpleCards.All(card => PveEnemyArtwork.DisplayColor(card) == CardColor.Blue),
                    Is.True, type.Id + " defenses");
            }
        }
    }
}
