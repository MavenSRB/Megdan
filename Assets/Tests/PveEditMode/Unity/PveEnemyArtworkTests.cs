using System.IO;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Pve.Composition;
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
    }
}
