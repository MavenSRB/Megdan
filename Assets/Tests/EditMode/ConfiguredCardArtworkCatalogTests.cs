using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ConfiguredCardArtworkCatalogTests
    {
        [Test]
        public void ConfiguredCatalog_IsA108EntryBijectionWithOnePlayableFrontPerDefinition()
        {
            var catalog = ConfiguredCardArtworkCatalog.Catalog;
            Assert.DoesNotThrow(() =>
                catalog.ValidateBijection(ConfiguredCardArtworkCatalog.ConfiguredReferences));
            Assert.That(catalog.Entries, Has.Count.EqualTo(108));
            Assert.That(ConfiguredCardArtworkCatalog.ConfiguredReferences, Has.Count.EqualTo(108));
            Assert.That(
                catalog.Entries.Select(entry => entry.Reference),
                Is.EquivalentTo(ConfiguredCardArtworkCatalog.ConfiguredReferences));
            Assert.That(
                catalog.Entries.Select(entry => entry.SourcePath).Distinct(
                    StringComparer.OrdinalIgnoreCase).Count(),
                Is.EqualTo(108),
                "One private front cannot illustrate two stable definitions.");
            Assert.That(
                catalog.Entries.GroupBy(entry => entry.ContentSetId)
                    .Select(group => (Hero: group.Key, Count: group.Count()))
                    .OrderBy(group => group.Hero),
                Is.EqualTo(new[]
                {
                    ("arien", 18), ("brogan", 18), ("sabina", 18),
                    ("tigerclaw", 18), ("wasp", 18), ("xargatha", 18)
                }));

            foreach (var entry in catalog.Entries)
            {
                Assert.That(entry.SourcePath, Does.EndWith("__front.png"));
                Assert.That(entry.SourcePath, Does.Not.Contain("/_dashboards/"));
                Assert.That(entry.SourcePath, Does.Not.Contain("__back.png"));
                Assert.That(entry.ResourcePath,
                    Is.EqualTo(ExpectedResourcePath(entry.Reference, entry.ContentSetId)));
                Assert.That(catalog.TryGet(entry.Reference, out var found), Is.True);
                Assert.That(found, Is.SameAs(entry));
            }
        }

        [Test]
        public void Catalog_RejectsAnOmittedConfiguredDefinition()
        {
            var original = ConfiguredCardArtworkCatalog.Catalog.Entries;
            var omitted = original[0];
            var catalog = new CardArtworkCatalog(original.Skip(1));
            var exception = Assert.Throws<InvalidOperationException>(() =>
                catalog.ValidateBijection(ConfiguredCardArtworkCatalog.ConfiguredReferences));
            Assert.That(exception.Message,
                Does.Contain(omitted.Reference.ToString()).And.Contain("no stable source path"));
        }

        [Test]
        public void Catalog_RejectsADuplicateDefinitionAndNamesBothStableAddresses()
        {
            var entries = ConfiguredCardArtworkCatalog.Catalog.Entries.ToList();
            var duplicate = entries[0];
            entries.Add(new CardArtworkCatalogEntry(duplicate.Reference, duplicate.SourcePath));
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new CardArtworkCatalog(entries));
            Assert.That(exception.Message,
                Does.Contain(duplicate.Reference.ToString()).And.Contain(duplicate.SourcePath));
        }

        [Test]
        public void Catalog_RejectsAFrontMappedToTwoDefinitions()
        {
            var entries = ConfiguredCardArtworkCatalog.Catalog.Entries.ToList();
            var first = entries[0];
            var second = entries[1];
            entries[1] = new CardArtworkCatalogEntry(second.Reference, first.SourcePath);
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new CardArtworkCatalog(entries));
            Assert.That(exception.Message,
                Does.Contain(second.Reference.ToString())
                    .And.Contain(first.SourcePath)
                    .And.Contain(first.Reference.ToString()));
        }

        [Test]
        public void Catalog_RejectsAnUnknownHero()
        {
            var entry = new CardArtworkCatalogEntry(
                new CardDefinitionReference(new CardDefinitionId("unknown-card"), "unknown-rev"),
                "active-hero-cards/Unknown/unknown__1__front.png");
            AssertEntryFailure(entry, "unknown configured hero");
        }

        [Test]
        public void Catalog_RejectsADashboardSource()
        {
            var original = ConfiguredCardArtworkCatalog.Catalog.Entries[0];
            var entry = new CardArtworkCatalogEntry(
                original.Reference,
                "active-hero-cards/_dashboards/Wasp__front.png");
            AssertEntryFailure(entry, "action-card folder");
        }

        [Test]
        public void Catalog_RejectsABackFaceSource()
        {
            var original = ConfiguredCardArtworkCatalog.Catalog.Entries[0];
            var entry = new CardArtworkCatalogEntry(
                original.Reference,
                original.SourcePath.Replace("__front.png", "__back.png"));
            AssertEntryFailure(entry, "playable __front.png");
        }

        [Test]
        public void Catalog_RejectsARevisionMismatchAndReportsDefinitionAndSource()
        {
            var entries = ConfiguredCardArtworkCatalog.Catalog.Entries.ToList();
            var original = entries[0];
            var mismatched = new CardArtworkCatalogEntry(
                new CardDefinitionReference(
                    original.Reference.DefinitionId,
                    original.Reference.ContentRevision + "-mismatch"),
                original.SourcePath);
            entries[0] = mismatched;
            var catalog = new CardArtworkCatalog(entries);
            var exception = Assert.Throws<InvalidOperationException>(() =>
                catalog.ValidateBijection(ConfiguredCardArtworkCatalog.ConfiguredReferences));
            Assert.That(exception.Message,
                Does.Contain(mismatched.Reference.ToString())
                    .And.Contain(mismatched.SourcePath)
                    .And.Contain(original.Reference.ContentRevision));
        }

        [Test]
        public void ImmutableDefinitionReference_SelectsThePreparedResourceWithoutNameParsing()
        {
            var reference = WaspCardContent.Definition(WaspCardContent.Shock).Reference;
            Assert.That(
                ConfiguredCardArtwork.TryGetResourcePath(reference, out var resourcePath),
                Is.True);
            Assert.That(resourcePath, Is.EqualTo("Private/Cards/wasp/shock"));

            var stale = new CardDefinitionReference(
                reference.DefinitionId,
                reference.ContentRevision + "-stale");
            Assert.That(ConfiguredCardArtwork.TryGetResourcePath(stale, out _), Is.False,
                "A display name or filename match cannot rescue the wrong immutable revision.");
        }

        [Test]
        public void CompleteFace_UsesInjectedArtworkAtTheStandardFootprintAndKeepsStateReading()
        {
            var definition = WaspCardContent.Definition(WaspCardContent.Shock);
            var card = new TrayCardViewModel(
                new CardInstanceId("artwork-shock"),
                definition.Reference,
                definition.DisplayName,
                definition.Progression.Color,
                CurrentRoundCards.ReadFace(definition),
                TrayCardGroup.Available,
                0,
                true,
                false,
                true);
            var texture = new Texture2D(372, 520, TextureFormat.RGBA32, false);
            try
            {
                var requested = new List<string>();
                VisualElement face;
                using (ConfiguredCardArtwork.UseTextureLoader(path =>
                       {
                           requested.Add(path);
                           return texture;
                       }))
                {
                    face = DirectorHandsUnityView.BuildStandaloneCard(card);
                }

                Assert.That(requested, Is.EqualTo(new[] { "Private/Cards/wasp/shock" }));
                AssertStandardFootprint(face);
                var artwork = Descendants(face).Single(element =>
                    element.name == "gd-card-artwork");
                Assert.That(artwork.userData, Is.EqualTo(definition.Reference));
                Assert.That(artwork.pickingMode, Is.EqualTo(PickingMode.Ignore));
                Assert.That(artwork.focusable, Is.False,
                    "The picture adds no pointer or keyboard stop inside the existing surface.");
                Assert.That(
                    artwork.style.backgroundSize.value.sizeType,
                    Is.EqualTo(BackgroundSizeType.Contain),
                    "A verified 372x520 front is aspect-contained rather than cropped or stretched.");
                Assert.That(face.style.overflow.value, Is.EqualTo(Overflow.Hidden),
                    "The fixed shared footprint clips the artwork layer at the card boundary.");
                Assert.That(
                    Descendants(face).Single(element => element.name == "gd-card-history")
                        .style.display.value,
                    Is.Not.EqualTo(DisplayStyle.None),
                    "The lifecycle remains visible below the aspect-contained front.");
                Assert.That(
                    Descendants(face).Count(element => element.name == "gd-card-active"),
                    Is.EqualTo(1),
                    "The typed ACTIVE overlay remains readable on a prepared front.");
                foreach (var generatedRegion in new[]
                         {
                             "gd-card-name-band", "gd-card-secondary-values",
                             "gd-card-primary-action", "gd-card-rules"
                         })
                {
                    Assert.That(
                        Descendants(face).Single(element => element.name == generatedRegion)
                            .style.display.value,
                        Is.EqualTo(DisplayStyle.None),
                        "Prepared artwork replaces the generated visual without deleting its typed " +
                        "fallback construction.");
                }

                Assert.That(face.tooltip, Is.EqualTo(card.AssistiveLabel));
                Assert.That(face.tooltip, Does.Contain("ACTIVE EFFECT IN EFFECT"));
                Assert.That(face.tooltip, Does.Not.Contain("Private/Cards"),
                    "No resource address reaches player-facing hover text.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void CompleteFace_FallsBackPerCardWhenArtworkIsAbsentOrUnloadable()
        {
            var definition = WaspCardContent.Definition(WaspCardContent.Shock);
            var card = CurrentRoundCards.DeckCard(
                new CardInstanceId("fallback-shock"), definition);

            foreach (var loader in new Func<string, Texture2D>[]
                     {
                         _ => null,
                         _ => throw new InvalidOperationException("injected unloadable texture")
                     })
            {
                VisualElement face;
                using (ConfiguredCardArtwork.UseTextureLoader(loader))
                {
                    face = DirectorHandsUnityView.BuildStandaloneCard(card);
                }

                AssertStandardFootprint(face);
                Assert.That(
                    Descendants(face).Any(element => element.name == "gd-card-artwork"),
                    Is.False);
                foreach (var generatedRegion in new[]
                         {
                             "gd-card-name-band", "gd-card-secondary-values",
                             "gd-card-primary-action", "gd-card-rules", "gd-card-history"
                         })
                {
                    Assert.That(
                        Descendants(face).Single(element => element.name == generatedRegion)
                            .style.display.value,
                        Is.Not.EqualTo(DisplayStyle.None));
                }

                Assert.That(face.tooltip, Is.EqualTo(card.AssistiveLabel));
            }
        }

        [Test]
        public void MissingCatalogMapping_StillBuildsAReachableTypedFace()
        {
            var definition = WaspCardContent.Definition(WaspCardContent.Shock);
            var card = new TrayCardViewModel(
                new CardInstanceId("unknown-artwork-card"),
                new CardDefinitionReference(
                    new CardDefinitionId("synthetic-unmapped-card"), "synthetic-rev"),
                definition.DisplayName,
                definition.Progression.Color,
                CurrentRoundCards.ReadFace(definition),
                TrayCardGroup.Available,
                0,
                true,
                false);
            var loads = 0;
            VisualElement face;
            using (ConfiguredCardArtwork.UseTextureLoader(_ =>
                   {
                       loads++;
                       return null;
                   }))
            {
                face = DirectorHandsUnityView.BuildStandaloneCard(card);
            }

            Assert.That(loads, Is.Zero,
                "An unknown stable identity never falls through to filename or display-name matching.");
            Assert.That(Descendants(face).Any(element => element.name == "gd-card-artwork"), Is.False);
            Assert.That(Descendants(face).Any(element => element.name == "gd-card-rules"), Is.True);
            Assert.That(face.tooltip, Is.EqualTo(card.AssistiveLabel));
            AssertStandardFootprint(face);
        }

        private static void AssertEntryFailure(CardArtworkCatalogEntry entry, string reason)
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new CardArtworkCatalog(new[] { entry }));
            Assert.That(exception.Message,
                Does.Contain(entry.Reference.ToString())
                    .And.Contain(entry.SourcePath)
                    .And.Contain(reason));
        }

        private static string ExpectedResourcePath(
            CardDefinitionReference reference,
            string hero) =>
            $"Private/Cards/{hero}/{reference.DefinitionId.Value.Substring(hero.Length + 1)}";

        private static void AssertStandardFootprint(VisualElement face)
        {
            Assert.That(face.style.width.value.value, Is.EqualTo(CardFootprint.StandardWidth));
            Assert.That(face.style.minWidth.value.value, Is.EqualTo(CardFootprint.StandardWidth));
            Assert.That(face.style.maxWidth.value.value, Is.EqualTo(CardFootprint.StandardWidth));
            Assert.That(face.style.height.value.value, Is.EqualTo(CardFootprint.StandardHeight));
            Assert.That(face.style.minHeight.value.value, Is.EqualTo(CardFootprint.StandardHeight));
            Assert.That(face.style.maxHeight.value.value, Is.EqualTo(CardFootprint.StandardHeight));
        }

        private static IEnumerable<VisualElement> Descendants(VisualElement element)
        {
            yield return element;
            foreach (var child in element.Children())
            {
                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
