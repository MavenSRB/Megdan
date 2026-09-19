using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Setup;
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
        public void ConfiguredCatalog_IsA324EntryBijectionWithOnePlayableFrontPerDefinition()
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
            //
            // MILESTONE 8.37 TAKES IT 198 TO 216, AND IS THE LAST TIME IT MOVES FOR A DEALING: Tali
            // is the twelfth and last hero of the package arc, so the catalog now illustrates every
            // hero this build can put on a board. Her dealing wrote `HeroFolders`'s
            // `{ "tali", "Tali" }` and `Configured`'s `.Concat(TaliCardContent.DealtDefinitions)` in
            // the same edit as the eighteen JSON rows, on the instruction the paragraphs above give.
            // Ice Shard is p.20's Handicap card and is excluded exactly as the five above it are.
            //
            // MILESTONE 8.38e TAKES IT 216 TO 234, AND THE SENTENCE ABOVE IT - "the last time it moves
            // for a dealing" - WAS ABOUT THE PACKAGE ARC RATHER THAN ABOUT THIS COUNT. The user
            // re-opened the roster on 2026-09-11 and Swift is the thirteenth hero; what has to keep
            // being true is that the catalog illustrates every hero this build can put on a board, and
            // that is kept true by this line moving rather than by the earlier one having been right.
            // Backup is p.20's Handicap card and is excluded exactly as the six above it are.
            //
            // AND THIS IS THE RED THAT PROVES THE THREE PARAGRAPHS ABOVE ARE NOT DECORATION. Milestone
            // 8.38e wrote HeroFolders' { "swift", "Swift" }, Configured's
            // .Concat(SwiftCardContent.DealtDefinitions) and the eighteen JSON rows in one edit, as
            // instructed - and still shipped with this count unmoved, because this fixture is on the
            // harness's UnityOnlyTest list and NO dotnet run can execute it. It was found by the
            // first Unity Edit Mode run after that dealing, which is the only tier that can find it.
            //
            // AND IT TOOK TWO RUNS, BECAUSE THIS FIXTURE CARRIES TWO PINS AND ASSERTIONS SURFACE ONE
            // AT A TIME. The count below failed first; the per-hero grouping further down failed only
            // once the count was fixed. A Unity-only fixture that pins more than one thing is not
            // green until it is RE-RUN green - the same shape milestone 8.38e's scenario digests
            // record, where the failure message reported five of nine.
            //
            // MILESTONE 8.39i TAKES IT 234 TO 252, and it is the first dealing since 8.35z53 that the
            // wider half did not catch AND that this count did not catch either: the instruction the
            // seven paragraphs above leave was followed in one edit - `HeroFolders`'s
            // { "cutter", "Cutter" }, `Configured`'s .Concat(CutterCardContent.DealtDefinitions), the
            // eighteen JSON rows, this count and the grouping below - and the offline check those
            // paragraphs do NOT name was run beside it: the catalog's `contentRevision` set against
            // every `public const string ContentRevision` in the verified content, which is a two-
            // second comparison and the one thing that makes the revision half of this file provable
            // without a Unity launch. Few More Steps is p.20's Handicap card and is excluded exactly
            // as the seven Handicap cards named above are.
            //
            // MILESTONE 8.40j TAKES IT 252 TO 270, and it ran the offline revision check the
            // paragraph above names BEFORE launching an Editor rather than after: the catalog's
            // fifteen `contentRevision` values equal the fifteen `ContentRevision` constants as sets,
            // which is the half of this file a green harness is otherwise structurally blind to.
            // Side Quest is p.20's Handicap card and is excluded exactly as the eight named above are.
            // 8.41e adds Garrus's eighteen playable fronts; Angry Swipe stays boxed.
            // MILESTONE 8.57g TAKES IT 306 TO 324 with Rowenna the Vanguard's eighteen, and ran the
            // same offline revision check the paragraph above names BEFORE launching an Editor: the
            // catalog's eighteen `contentRevision` values equal the eighteen `ContentRevision`
            // constants as sets. Earn the Spurs is p.20's Handicap card and is excluded exactly as the
            // nine named above are.
            Assert.That(catalog.Entries, Has.Count.EqualTo(324));
            Assert.That(ConfiguredCardArtworkCatalog.ConfiguredReferences, Has.Count.EqualTo(324));
            Assert.That(
                catalog.Entries.Select(entry => entry.Reference),
                Is.EquivalentTo(ConfiguredCardArtworkCatalog.ConfiguredReferences));
            Assert.That(
                catalog.Entries.Select(entry => entry.SourcePath).Distinct(
                    StringComparer.OrdinalIgnoreCase).Count(),
                Is.EqualTo(324),
                "One private front cannot illustrate two stable definitions.");
            Assert.That(
                catalog.Entries.GroupBy(entry => entry.ContentSetId)
                    .Select(group => (Hero: group.Key, Count: group.Count()))
                    .OrderBy(group => group.Hero),
                Is.EqualTo(new[]
                {
                    ("arien", 18), ("bain", 18), ("brogan", 18), ("cutter", 18), ("dodger", 18),
                    ("garrus", 18), ("misa", 18), ("rowenna", 18), ("sabina", 18),
                    ("silverarrow", 18), ("swift", 18), ("tali", 18),
                    ("tigerclaw", 18), ("ursafar", 18), ("wasp", 18), ("whisper", 18),
                    ("widget", 18), ("xargatha", 18)
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

        /// <summary>
        /// Milestone 8.57g's own read of the artwork half of a dealing: every dealt face of Rowenna's
        /// has a catalog row, the rows point at her own extraction directory, and every one carries
        /// her content revision. Milestone 8.35l's trap in one case - a revision bump owes the catalog
        /// row - asserted for the hero this milestone deals rather than left to the aggregate counts
        /// above, which would still pass if eighteen rows of hers carried somebody else's revision.
        /// </summary>
        [Test]
        public void EveryDealtFaceOfRowennas_HasAnArtworkRowAtHerOwnRevision()
        {
            var entries = ConfiguredCardArtworkCatalog.Catalog.Entries
                .Where(entry => entry.ContentSetId == RowennaCardContent.ContentSetId)
                .ToArray();

            Assert.That(entries, Has.Length.EqualTo(18));
            Assert.That(
                entries.Select(entry => entry.Reference),
                Is.EquivalentTo(RowennaCardContent.DealtDefinitions
                    .Select(definition => definition.Reference)),
                "A bijection onto her dealt definitions, so an extracted face nobody deals and a " +
                "dealt face nobody illustrated both fail here.");
            Assert.That(
                entries.Select(entry => entry.Reference.ContentRevision).Distinct(),
                Is.EqualTo(new[] { RowennaCardContent.ContentRevision }),
                "and every row carries the one revision her content does - milestone 8.35l's trap: " +
                "a revision bump owes the catalog row, and a row on the old string simply stops " +
                "addressing the definition.");
            foreach (var entry in entries)
            {
                Assert.That(entry.SourcePath,
                    Does.StartWith("active-hero-cards/Rowenna/rowenna")
                        .And.EndWith("__front.png"));
            }

            Assert.That(
                entries.Select(entry => entry.SourcePath).Distinct().Count(), Is.EqualTo(18),
                "One private front cannot illustrate two of her definitions.");
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

        /// <summary>
        /// Milestone 8.39i. A checkout WITHOUT the private corpus still draws every configured hero,
        /// proved rather than assumed, and proved for all fourteen rather than for the one face the
        /// case above happens to use.
        /// </summary>
        /// <remarks>
        /// <para><b>Why it is derived over the catalog.</b> The clean-checkout claim is made by every
        /// dealing's acceptance checks and has been carried since milestone 8.25f by one Wasp card. A
        /// hero whose rows are in the catalog and whose face cannot be built without artwork would fail
        /// for a player with no `Docs/` folder and pass for every author who has one - which is exactly
        /// the asymmetry a private corpus creates. Walking the catalog's own content sets means a
        /// fifteenth hero is covered the moment her rows are added.</para>
        /// <para>The absent corpus is simulated the way the case above simulates it - a texture loader
        /// that returns null for every request - because that is precisely what `Resources.Load` does
        /// when the ignored folder is not there.</para>
        /// </remarks>
        [Test]
        public void WithoutThePrivateCorpus_EveryConfiguredHeroStillDrawsAGeneratedFace()
        {
            var byHero = ConfiguredCardArtworkCatalog.Catalog.Entries
                .GroupBy(entry => entry.ContentSetId)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToArray();
            Assert.That(byHero, Is.Not.Empty, "The catalog must name the heroes this case walks.");

            var definitions = ConfiguredCardArtworkCatalog.ConfiguredReferences.ToArray();
            using (ConfiguredCardArtwork.UseTextureLoader(_ => null))
            {
                foreach (var hero in byHero)
                {
                    var reference = hero.First().Reference;
                    var definition = EveryDealtDefinition()
                        .Single(candidate => candidate.Reference.Equals(reference));
                    var card = CurrentRoundCards.DeckCard(
                        new CardInstanceId("clean-checkout-" + hero.Key), definition);

                    var face = DirectorHandsUnityView.BuildStandaloneCard(card);

                    AssertStandardFootprint(face);
                    Assert.That(
                        Descendants(face).Any(element => element.name == "gd-card-artwork"),
                        Is.False,
                        hero.Key + " must draw no artwork element when the private corpus is absent.");
                    Assert.That(
                        Descendants(face).Any(element => element.name == "gd-card-rules"),
                        Is.True,
                        hero.Key + " must still draw its generated typed face.");
                    Assert.That(face.tooltip, Is.EqualTo(card.AssistiveLabel));
                }
            }

            Assert.That(definitions, Has.Length.EqualTo(
                ConfiguredCardArtworkCatalog.Catalog.Entries.Count),
                "and the walk above covered one face per configured hero out of that same set.");
        }

        /// <summary>
        /// Milestone 8.39i. The hover pick card resolves for EVERY hero the selector offers, and the
        /// two halves that have to agree for it to are asserted against each other rather than either
        /// alone.
        /// </summary>
        /// <remarks>
        /// <para><b>An absent private resource is tolerated by design everywhere else in this build,
        /// which is exactly what would let this fail quietly.</b> `PrivateLobbyViews.ShowHeroOverview`
        /// loads `Private/HeroSelection/&lt;heroIdValue&gt;` and HIDES the overview when the load
        /// returns null - correct behaviour for a clean checkout, and indistinguishable from a hero
        /// whose front was never prepared. So the check is that the two sides cannot drift: the
        /// preparation tool is keyed by CONTENT id and writes `hero-&lt;contentId&gt;.png`, while the
        /// runtime addresses by HERO id, and every roster row must satisfy
        /// `heroIdValue == "hero-" + contentId` for those to be the same file.</para>
        /// <para>Derived over `RegisteredHeroRoster.ForThisBuild`, so a fifteenth hero is covered by
        /// her roster row alone. Milestone 8.36e built the hover; what a dealing owes it is the
        /// prepared resource under the key the runtime composes, and this is the offline half of
        /// proving that - the live half is the lobby Play Mode case, which asserts a non-null texture
        /// per offered hero on a checkout that HAS the corpus.</para>
        /// </remarks>
        [Test]
        public void EveryHeroTheSelectorOffers_HasAPreparedHoverFrontUnderTheKeyTheRuntimeComposes()
        {
            var script = File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "..", "Tools", "Prepare-HeroSelectionArtwork.ps1"));
            var declared = Regex.Matches(script, @"(?m)^\s*([a-z]+)\s*=\s*'([^']+\.png)'\s*$")
                .Cast<Match>().Select(match => match.Groups[1].Value).ToArray();

            Assert.That(declared, Is.Not.Empty,
                "The preparation script's own declared rows are this check's source; finding none " +
                "means its shape moved and this assertion stopped asking anything.");
            Assert.That(
                declared, Is.EquivalentTo(RegisteredHeroRoster.ForThisBuild
                    .Select(hero => hero.ContentId.Value)),
                "Every hero the selector offers needs a prepared hover front, and nothing is prepared " +
                "for a hero no selector offers.");

            foreach (var hero in RegisteredHeroRoster.ForThisBuild)
            {
                Assert.That(hero.HeroIdValue, Is.EqualTo("hero-" + hero.ContentId.Value),
                    "The tool writes hero-<contentId>.png and the runtime loads " +
                    "Private/HeroSelection/<heroIdValue>; a row where those disagree prepares a file " +
                    "the hover never asks for and falls back silently.");
            }
        }

        /// <summary>
        /// Every dealt definition every verified content class in the build contributes, derived by the
        /// same reflection milestone 8.38d's corpus sweep uses rather than listed by hand.
        /// </summary>
        private static IEnumerable<CardDefinition> EveryDealtDefinition()
        {
            var contentClasses = typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == typeof(WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .ToArray();
            Assert.That(contentClasses, Is.Not.Empty,
                "The reflection must find the verified content classes at all.");

            var everyDealtDefinition = new List<CardDefinition>();
            foreach (var contentClass in contentClasses)
            {
                var property = contentClass.GetProperty(
                    "DealtDefinitions",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                Assert.That(property, Is.Not.Null, contentClass.Name);
                everyDealtDefinition.AddRange((IReadOnlyList<CardDefinition>)property.GetValue(null));
            }

            return everyDealtDefinition;
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
