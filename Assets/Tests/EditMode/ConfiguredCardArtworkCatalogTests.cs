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
        public void CompleteFace_UsesOnlyTheInjectedArtworkAtNativeAspectAndKeepsStateReading()
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
                true,
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
                AssertArtworkFootprint(face);
                var artwork = Descendants(face).OfType<Image>().Single(element =>
                    element.name == "gd-card-artwork");
                Assert.That(artwork.userData, Is.EqualTo(definition.Reference));
                Assert.That(artwork.pickingMode, Is.EqualTo(PickingMode.Ignore));
                Assert.That(artwork.focusable, Is.False,
                    "The picture adds no pointer or keyboard stop inside the existing surface.");
                Assert.That(artwork.image, Is.SameAs(texture));
                Assert.That(artwork.scaleMode, Is.EqualTo(ScaleMode.ScaleToFit),
                    "A verified 372x520 front is fitted without cropping or distortion.");
                Assert.That(face.style.overflow.value, Is.EqualTo(Overflow.Hidden),
                    "The exact artwork box clips only at the printed card boundary.");
                Assert.That(Descendants(face).Any(element => element.name == "gd-card-frame"),
                    Is.False, "Prepared artwork does not construct the generated shell.");
                Assert.That(Descendants(face).Any(element => element.name == "gd-card-history"),
                    Is.False, "No generated lifecycle strip is placed around the printed face.");
                Assert.That(face.style.borderTopWidth.value, Is.Zero);
                Assert.That(face.style.paddingTop.value.value, Is.Zero);
                Assert.That(face.style.backgroundColor.value.a, Is.Zero);
                Assert.That(
                    Descendants(face).Count(element =>
                        element.name == "gd-card-selection-outline"),
                    Is.EqualTo(1),
                    "Selection is a conditional overlay and does not become a permanent frame.");

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
        public void UnselectedArtworkFace_HasNoSyntheticFrameOrLifecycleStrip()
        {
            var definition = WaspCardContent.Definition(WaspCardContent.Shock);
            var card = CurrentRoundCards.DeckCard(
                new CardInstanceId("unframed-artwork-shock"), definition);
            var texture = new Texture2D(372, 520, TextureFormat.RGBA32, false);
            try
            {
                VisualElement face;
                using (ConfiguredCardArtwork.UseTextureLoader(_ => texture))
                {
                    face = DirectorHandsUnityView.BuildStandaloneCard(card);
                }

                AssertArtworkFootprint(face);
                Assert.That(Descendants(face).Select(element => element.name),
                    Is.EquivalentTo(new[] { "gd-card-preview", "gd-card-artwork" }));
                Assert.That(face.style.borderTopWidth.value, Is.Zero);
                Assert.That(face.style.borderRightWidth.value, Is.Zero);
                Assert.That(face.style.borderBottomWidth.value, Is.Zero);
                Assert.That(face.style.borderLeftWidth.value, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ArtworkFace_UsesReadableStandardHeightAndDerivesWidthFromSourceAspect()
        {
            var definition = WaspCardContent.Definition(WaspCardContent.Shock);
            var card = CurrentRoundCards.DeckCard(
                new CardInstanceId("readable-artwork-shock"), definition);
            var texture = new Texture2D(372, 520, TextureFormat.RGBA32, false);
            try
            {
                VisualElement face;
                using (ConfiguredCardArtwork.UseTextureLoader(_ => texture))
                {
                    face = DirectorHandsUnityView.BuildStandaloneCard(card);
                }

                var expectedHeight = CardFootprint.StandardHeight;
                var expectedWidth = expectedHeight * CardFootprint.ArtworkSourceWidth /
                    CardFootprint.ArtworkSourceHeight;
                Assert.That(face.style.height.value.value, Is.EqualTo(expectedHeight),
                    "The printed front keeps the established readable card height.");
                Assert.That(face.style.width.value.value, Is.EqualTo(expectedWidth).Within(0.001f),
                    "Its width is enlarged from that height at the exact 372:520 source ratio.");
                Assert.That(face.style.minWidth.value.value, Is.EqualTo(expectedWidth).Within(0.001f));
                Assert.That(face.style.maxWidth.value.value, Is.EqualTo(expectedWidth).Within(0.001f));
                Assert.That(face.style.minHeight.value.value, Is.EqualTo(expectedHeight));
                Assert.That(face.style.maxHeight.value.value, Is.EqualTo(expectedHeight));
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

        private static void AssertArtworkFootprint(VisualElement face)
        {
            Assert.That(CardFootprint.ArtworkHeight, Is.EqualTo(CardFootprint.StandardHeight));
            Assert.That(CardFootprint.ArtworkWidth, Is.EqualTo(
                CardFootprint.StandardHeight * CardFootprint.ArtworkSourceWidth /
                CardFootprint.ArtworkSourceHeight).Within(0.001f));
            Assert.That(
                CardFootprint.ArtworkWidth / CardFootprint.ArtworkHeight,
                Is.EqualTo(CardFootprint.ArtworkSourceWidth /
                    CardFootprint.ArtworkSourceHeight).Within(0.0001f));
            Assert.That(face.style.width.value.value, Is.EqualTo(CardFootprint.ArtworkWidth));
            Assert.That(face.style.minWidth.value.value, Is.EqualTo(CardFootprint.ArtworkWidth));
            Assert.That(face.style.maxWidth.value.value, Is.EqualTo(CardFootprint.ArtworkWidth));
            Assert.That(face.style.height.value.value, Is.EqualTo(CardFootprint.ArtworkHeight));
            Assert.That(face.style.minHeight.value.value, Is.EqualTo(CardFootprint.ArtworkHeight));
            Assert.That(face.style.maxHeight.value.value, Is.EqualTo(CardFootprint.ArtworkHeight));
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
