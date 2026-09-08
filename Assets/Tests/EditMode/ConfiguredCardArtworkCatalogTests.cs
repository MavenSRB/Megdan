using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public void PrivatePreparationScript_CoversEveryConfiguredHeroFolder()
        {
            var script = File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "..", "Tools", "Prepare-CardArtwork.ps1"));
            var folders = Regex.Matches(script, @"(?m)^\s*([a-z]+)\s*=\s*'([A-Za-z]+)'\s*$")
                .Cast<Match>().Select(match => match.Groups[1].Value + ":" + match.Groups[2].Value);
            var expected = ConfiguredCardArtworkCatalog.Catalog.Entries
                .Select(entry => entry.ContentSetId + ":" + entry.SourcePath.Split('/')[1]).Distinct();
            Assert.That(folders, Is.EquivalentTo(expected),
                "A newly configured hero must not be omitted by the private importer.");
        }

        [Test]
        public void WhenPrivateCorpusIsPresent_EveryConfiguredFrontIsPreparedAndLoadable()
        {
            var corpus = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "..", "Docs", "Guards of Atlantis II - Tabletopia Cards"));
            // A clean checkout deliberately has no private art; separate tests prove its fallback.
            if (!Directory.Exists(corpus)) return;
            foreach (var entry in ConfiguredCardArtworkCatalog.Catalog.Entries)
            {
                Assert.That(File.Exists(Path.Combine(corpus, entry.SourcePath)), Is.True, entry.SourcePath);
                var texture = Resources.Load<Texture2D>(entry.ResourcePath);
                Assert.That(texture, Is.Not.Null, entry.Reference + " requires its private prepared front.");
                Assert.That(texture.width, Is.EqualTo(372), entry.ResourcePath);
                Assert.That(texture.height, Is.EqualTo(520), entry.ResourcePath);
            }
        }

        [Test]
        public void ConfiguredCatalog_IsA198EntryBijectionWithOnePlayableFrontPerDefinition()
        {
            var catalog = ConfiguredCardArtworkCatalog.Catalog;
            Assert.DoesNotThrow(() =>
                catalog.ValidateBijection(ConfiguredCardArtworkCatalog.ConfiguredReferences));
            // Milestone 8.35z34 took this from 108 to 126: dealing Dodger the Warlock adds her eighteen
            // dealt definitions, and the catalog names DEALT heroes rather than composed ones. It is a
            // Unity-only fixture - the dotnet harness excludes it, because the catalog is Presentation -
            // so this is exactly the pin a green harness is structurally blind to, which is milestone
            // 8.35l's recorded trap arriving on schedule.
            //
            // MILESTONE 8.35z38 TOOK IT 126 TO 144 AND WAS CAUGHT BY IT AGAIN, which is what makes the
            // paragraph above a rule rather than one milestone's bad luck. The complete dotnet harness
            // was 2,535 passed / 0 failed with this pin reading 126 against a 144-entry catalog: the
            // harness cannot see this file at all, so the FIRST complete Edit Mode launch is the only
            // thing in the build that can, and a dealing milestone that skips that tier ships a catalog
            // whose own count is wrong. Two consecutive dealings, two identical catches.
            //
            // Milestone 8.35z46 takes it 144 to 162: dealing Silverarrow the Pathfinder adds her
            // eighteen dealt definitions, on the same terms - Keep the Distance is p.20's Handicap
            // card, excluded exactly as Dodger's Rusty Razor and Misa's Leaf on the Wind are.
            //
            // MILESTONE 8.35z50 TAKES IT 162 TO 180 AND WAS CAUGHT A THIRD TIME - and the third catch
            // was a WIDER failure than this pin, which is why it is worth its own sentence. Ursafar's
            // dealing appended all eighteen rows to the catalog JSON and validated that file, but
            // never added his folder to `ConfiguredCardArtworkCatalog.HeroFolders`, so the catalog
            // could not address a single one of them and SIXTEEN Edit Mode cases failed at load -
            // this fixture's ten and six of MatchHudPresentationTests'. Both halves of the omission
            // are invisible offline for the same reason: the map lives in Presentation, which the
            // Unity-free harness does not compile and the one-assembly probe can only compile rather
            // than run. Three consecutive dealings, three identical catches, and the first Edit Mode
            // launch is still the only thing in the build that can make them.
            //
            // MILESTONE 8.35z53 TAKES IT 180 TO 198, AND IS THE FIRST DEALING THE WIDER HALF DID NOT
            // CATCH. Whisper's dealing wrote `HeroFolders`'s `{ "whisper", "Whisper" }` and
            // `Configured`'s `.Concat(WhisperCardContent.DealtDefinitions)` in the same edit as the
            // eighteen JSON rows, because the comment above told it to - so the first Edit Mode launch
            // failed on this COUNT alone rather than on sixteen cases failing at load. That is the
            // whole value of the three paragraphs above being written down: the trap is still
            // structurally invisible to every offline tier, and what changed is that the map was
            // taught the hero at the same moment as the rows. Vile Accusation is p.20's Handicap card
            // and is excluded exactly as the four Handicap cards named above are.
            Assert.That(catalog.Entries, Has.Count.EqualTo(198));
            Assert.That(ConfiguredCardArtworkCatalog.ConfiguredReferences, Has.Count.EqualTo(198));
            Assert.That(
                catalog.Entries.Select(entry => entry.Reference),
                Is.EquivalentTo(ConfiguredCardArtworkCatalog.ConfiguredReferences));
            Assert.That(
                catalog.Entries.Select(entry => entry.SourcePath).Distinct(
                    StringComparer.OrdinalIgnoreCase).Count(),
                Is.EqualTo(198),
                "One private front cannot illustrate two stable definitions.");
            Assert.That(
                catalog.Entries.GroupBy(entry => entry.ContentSetId)
                    .Select(group => (Hero: group.Key, Count: group.Count()))
                    .OrderBy(group => group.Hero),
                Is.EqualTo(new[]
                {
                    ("arien", 18), ("brogan", 18), ("dodger", 18), ("misa", 18),
                    ("sabina", 18), ("silverarrow", 18), ("tigerclaw", 18), ("ursafar", 18),
                    ("wasp", 18), ("whisper", 18), ("xargatha", 18)
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
                Assert.That(face.style.overflow.value, Is.EqualTo(Overflow.Visible),
                    "The outer surface leaves room for the perimeter beyond the printed boundary.");
                var contentClip = Descendants(face).Single(element =>
                    element.name == "gd-card-content-clip");
                Assert.That(contentClip.style.overflow.value, Is.EqualTo(Overflow.Hidden),
                    "The dedicated content box still clips exactly at the printed card boundary.");
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
                // Milestone 8.35m: `CurrentRoundCards.DeckCard` sets `IsAwaitedAnswer`
                // unconditionally true (see its own doc comment - "a surface that offers it"
                // is card-scope's leveling-upgrade case by construction), so the shared
                // `ConfiguredCardFaceView.Build` conditionally adds its "gd-card-awaited-shine"
                // ring here on purpose. That ring is neither the generated shell
                // ("gd-card-frame") nor the lifecycle strip ("gd-card-history") this case
                // guards against - it is the SEVENTH amendment's legitimate resolution-focus
                // overlay, present only while `IsAwaitedAnswer` holds, exactly like
                // "gd-card-selection-outline" is present only while a card is selected. This
                // fixture stages a deck-offered card, which is always an awaited answer, so the
                // shine belongs in the expected set rather than being a chrome regression.
                Assert.That(Descendants(face).Select(element => element.name),
                    Is.EquivalentTo(new[]
                    {
                        "gd-card-preview",
                        "gd-card-exterior-outline",
                        "gd-card-content-clip",
                        "gd-card-artwork",
                        "gd-card-awaited-shine"
                    }));
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
