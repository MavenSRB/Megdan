using System;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25c. Each hero rail identifies its hero with that hero's own portrait rather than
    /// with a letter inside a circle.
    ///
    /// <para><b>What is actually being checked.</b> Not that a picture is present - the artwork is
    /// private, ignored by Git, and a clean checkout has none, exactly as the board's own face is. What
    /// is checked is everything that must be true whether or not the file is there: that the portrait
    /// is <em>addressed</em> from the hero the projection names, that six configured heroes address six
    /// different portraits, that the address does not come from the words drawn beside it, that nothing
    /// about rules or control ownership is decided by it, and that the letter badge is gone with no
    /// configured fallback to it.</para>
    /// </summary>
    public sealed class HeroRailPortraitTests
    {
        private const int PreparedTokenSize = 302;

        /// <summary>
        /// Milestone 8.30i1 closing check. The ignored private resources are optional as a set, but
        /// when the local preparation route has supplied them all twelve carry the measured white
        /// field, transparent exterior, official team ring, and substantial untouched portrait.
        /// Runtime selection stays the same content-id/team address shared by board and rail.
        /// </summary>
        [Test]
        public void PreparedHeroTokens_UseWhiteInteriorFieldsWhilePreservingPortraitsTeamRingsAndSharedRuntimeAddressing()
        {
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(CreateOfficialSixPlayerSession(), view).Refresh();
            var configured = view.Model.Heroes;
            var resources = configured
                .SelectMany(hero => new[] { TeamSide.Red, TeamSide.Blue }, (hero, team) => new
                {
                    hero.ContentId,
                    Team = team,
                    ResourcePath = HeroTokenArtwork.ResourcePath(hero.ContentId, team),
                    FilePath = Path.Combine(
                        UnityEngine.Application.dataPath,
                        "Resources",
                        HeroTokenArtwork.ResourceRoot,
                        hero.ContentId.Value + "-" + (team == TeamSide.Red ? "red" : "blue") + ".png")
                })
                .ToArray();

            Assert.That(resources, Has.Length.EqualTo(12));
            Assert.That(resources.Select(resource => resource.ResourcePath).Distinct().Count(),
                Is.EqualTo(12), "Six content identities and two teams address twelve distinct faces.");

            var present = resources.Where(resource => File.Exists(resource.FilePath)).ToArray();
            Assert.That(present.Length, Is.EqualTo(0).Or.EqualTo(12),
                "Private token preparation is atomic at roster scale: a checkout has none, or a " +
                "prepared development workspace has both team faces for every configured hero.");
            if (present.Length == 0)
            {
                foreach (var resource in resources)
                {
                    Assert.That(HeroTokenArtwork.TryLoadTexture(
                        resource.ContentId, resource.Team, out var absent), Is.False);
                    Assert.That(absent, Is.Null,
                        "A clean checkout retains the ordinary generated-prism/empty-portrait fallback.");
                }
                Assert.Pass("The private corpus is absent, so the tracked fallback contract applies.");
            }

            foreach (var resource in present)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(texture.LoadImage(File.ReadAllBytes(resource.FilePath)), Is.True);
                    Assert.That(texture.width, Is.EqualTo(PreparedTokenSize));
                    Assert.That(texture.height, Is.EqualTo(PreparedTokenSize));

                    var pixels = texture.GetPixels32();
                    Assert.That(Pixel(pixels, 0, 0).a, Is.Zero,
                        "The exterior remains transparent rather than receiving the white field.");
                    Assert.That(Pixel(pixels, 301, 0).a, Is.Zero);
                    Assert.That(Pixel(pixels, 0, 301).a, Is.Zero);
                    Assert.That(Pixel(pixels, 301, 301).a, Is.Zero);

                    var centre = (PreparedTokenSize - 1) / 2f;
                    var radius = PreparedTokenSize / 2f;
                    var whiteInterior = 0;
                    var retainedInterior = 0;
                    for (var y = 0; y < PreparedTokenSize; y++)
                    {
                        for (var x = 0; x < PreparedTokenSize; x++)
                        {
                            var distance = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre));
                            if (distance >= radius * 0.90f)
                            {
                                continue;
                            }

                            var pixel = Pixel(pixels, x, y);
                            if (pixel.r == 255 && pixel.g == 255 && pixel.b == 255 && pixel.a == 255)
                            {
                                whiteInterior++;
                            }
                            else
                            {
                                retainedInterior++;
                            }
                        }
                    }

                    var protectedInteriorArea = Mathf.PI * Mathf.Pow(radius * 0.90f, 2f);
                    Assert.That(whiteInterior / protectedInteriorArea, Is.GreaterThanOrEqualTo(0.02f),
                        resource.ResourcePath + " has no measured opaque-white source field.");
                    Assert.That(retainedInterior / protectedInteriorArea, Is.GreaterThanOrEqualTo(0.30f),
                        resource.ResourcePath + " erased the portrait while whitening the field.");

                    var ringRed = 0f;
                    var ringBlue = 0f;
                    var ringSamples = 0;
                    for (var angle = 0; angle < 360; angle += 5)
                    {
                        var radians = angle * Mathf.Deg2Rad;
                        var x = Mathf.RoundToInt(centre + radius * 0.96f * Mathf.Cos(radians));
                        var y = Mathf.RoundToInt(centre + radius * 0.96f * Mathf.Sin(radians));
                        var pixel = Pixel(pixels, x, y);
                        ringRed += pixel.r;
                        ringBlue += pixel.b;
                        ringSamples++;
                    }
                    var signedRingDifference = (ringRed - ringBlue) / ringSamples;
                    Assert.That(resource.Team == TeamSide.Red
                            ? signedRingDifference
                            : -signedRingDifference,
                        Is.GreaterThanOrEqualTo(40f),
                        resource.ResourcePath + " no longer retains its official team-coloured ring.");

                    Assert.That(resource.ResourcePath,
                        Is.EqualTo(HeroTokenArtwork.ResourcePath(resource.ContentId, resource.Team)),
                        "The prepared file is still selected by the shared board-and-rail address.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        /// <summary>
        /// All six of rulebook p.5's configured heroes address a distinct portrait, and each one is the
        /// resource the board's own piece for that hero is drawn from.
        /// </summary>
        [Test]
        public void EveryConfiguredHeroRail_AddressesItsOwnHeroPortrait()
        {
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(CreateOfficialSixPlayerSession(), view).Refresh();
            var heroes = view.Model.Heroes;

            Assert.That(heroes, Has.Count.EqualTo(6), "p.5's official setup seats six heroes.");
            foreach (var hero in heroes)
            {
                Assert.That(hero.ContentId.IsDefined, Is.True,
                    $"{hero.HeroDisplayName}'s rail names no content identity to select a portrait by.");
                Assert.That(
                    hero.PortraitResourcePath,
                    Is.EqualTo(HeroTokenArtwork.ResourcePath(hero.ContentId, hero.TeamSide)),
                    "A rail's portrait is the same resource the piece standing on the board is drawn " +
                    "from, so the two are one hero rather than two opinions of one.");
            }

            Assert.That(
                heroes.Select(hero => hero.PortraitResourcePath).Distinct().Count(),
                Is.EqualTo(6),
                "Six configured heroes address six different portraits: " +
                    string.Join(", ", heroes.Select(hero => hero.PortraitResourcePath)));
        }

        /// <summary>
        /// The portrait follows the projected hero identity, not the display name printed beside it.
        ///
        /// <para>Stated as the two halves that make it a real claim rather than a coincidence of this
        /// build's naming: two heroes carrying the same display name still address different portraits,
        /// and one hero renamed keeps the portrait they had. A selector reading the name would fail one
        /// of the two whichever way it was written.</para>
        /// </summary>
        [Test]
        public void ThePortrait_FollowsTheProjectedIdentityRatherThanTheDisplayName()
        {
            var sameName = new[]
            {
                Overview(new HeroContentId("wasp"), "The Same Name", TeamSide.Red),
                Overview(new HeroContentId("brogan"), "The Same Name", TeamSide.Red)
            };
            Assert.That(sameName[0].PortraitResourcePath,
                Is.Not.EqualTo(sameName[1].PortraitResourcePath),
                "Two heroes printing one name are still two heroes, and the picture must say so.");

            var renamed = Overview(new HeroContentId("wasp"), "Something Else Entirely", TeamSide.Red);
            Assert.That(renamed.PortraitResourcePath, Is.EqualTo(sameName[0].PortraitResourcePath),
                "Renaming the seat's label changes no hero, so it changes no portrait.");

            // And the team is the authoritative one rather than anything read out of the image.
            var blue = Overview(new HeroContentId("wasp"), "Wasp the Warmaiden", TeamSide.Blue);
            Assert.That(blue.PortraitResourcePath,
                Is.Not.EqualTo(sameName[0].PortraitResourcePath),
                "One hero on two sides is two prepared faces, addressed by the authoritative team.");
        }

        /// <summary>
        /// The disc the portrait is drawn in carries no text, on any of the six rails - so there is no
        /// configured fallback to the letter badge this milestone replaces - and the hero's written
        /// name is still beside it.
        ///
        /// <para>The absent-artwork case is the tracked project's ordinary one and is what this fixture
        /// actually runs in, so this is exactly the state a letter fallback would have shown itself in.
        /// What identifies the hero there is the name, which never left the header.</para>
        /// </summary>
        [Test]
        public void TheHeroDisc_CarriesNoLetterAndTheWrittenNameStaysBesideIt()
        {
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(CreateOfficialSixPlayerSession(), view).Refresh();

            var rails = Descendants(view.Root)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Has.Length.EqualTo(6));

            foreach (var rail in rails)
            {
                var disc = Descendants(rail).SingleOrDefault(element => element.name == "gd-hero-icon");
                Assert.That(disc, Is.Not.Null, "Every rail draws the hero disc.");
                Assert.That(
                    Descendants(disc).OfType<TextElement>().Select(text => text.text).ToArray(),
                    Is.Empty,
                    "The disc holds a portrait and nothing written; a letter here is the badge this " +
                    "milestone replaced, drawn again as a fallback.");

                Assert.That(disc.tooltip, Is.Not.Null.And.Not.Empty,
                    "and it reads as words to a pointer and to an assistive reader, because a surface " +
                    "whose only statement is a picture states nothing to a reader who cannot use one.");
            }

            var names = Descendants(view.Root).OfType<TextElement>().Select(text => text.text).ToArray();
            foreach (var hero in view.Model.Heroes)
            {
                Assert.That(names, Does.Contain(hero.HeroDisplayName),
                    $"{hero.HeroDisplayName}'s written name is still drawn beside the portrait.");
                Assert.That(hero.PortraitAssistiveLabel, Does.Contain(hero.HeroDisplayName));
                Assert.That(
                    hero.PortraitAssistiveLabel.IndexOf(
                        hero.TeamSide == TeamSide.Red ? "red" : "blue",
                        StringComparison.OrdinalIgnoreCase),
                    Is.GreaterThanOrEqualTo(0),
                    "The team is in the words too, so neither the picture nor the accent colour is " +
                    "the only place it is stated.");
            }
        }

        /// <summary>
        /// Milestone 8.29a's closing identity-hierarchy check. The portrait and written name are the
        /// first reading of every hero plaque, while the same complete secondary match information
        /// remains present beneath them.
        /// </summary>
        [Test]
        public void TheSixHeroPlaques_MakePortraitAndNameThePrimaryIdentity()
        {
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(CreateOfficialSixPlayerSession(), view).Refresh();

            var rails = Descendants(view.Root)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Has.Length.EqualTo(6));

            foreach (var rail in rails)
            {
                var identityPlate = rail.Children()
                    .SingleOrDefault(element => element.name == "gd-hero-identity-plate");
                Assert.That(identityPlate, Is.Not.Null,
                    "The hero identity leads in its own layered plate rather than floating in a box.");

                var medallion = Descendants(identityPlate)
                    .SingleOrDefault(element => element.name == "gd-hero-medallion");
                var portrait = Descendants(identityPlate)
                    .SingleOrDefault(element => element.name == "gd-hero-icon");
                Assert.That(medallion, Is.Not.Null, "The portrait has a team-rimmed medallion.");
                Assert.That(portrait, Is.Not.Null, "Every plaque keeps the official hero portrait.");
                Assert.That(portrait.style.width.value.value, Is.GreaterThanOrEqualTo(48f));
                Assert.That(portrait.style.height.value.value, Is.GreaterThanOrEqualTo(48f));

                var level = Descendants(identityPlate).OfType<Label>()
                    .SingleOrDefault(label => label.name == "gd-hero-level");
                var name = Descendants(identityPlate).OfType<Label>()
                    .SingleOrDefault(label => label.name == "gd-hero-name");
                Assert.That(level, Is.Not.Null, "The level remains in the identity plate.");
                Assert.That(name, Is.Not.Null, "The full written hero name remains in the plaque.");
                Assert.That(name.style.fontSize.value.value, Is.GreaterThanOrEqualTo(15f));
                Assert.That(name.style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Bold));

                foreach (var secondary in new[]
                {
                    "gd-current-card-slot", "gd-bonus-strip", "gd-item-chips", "gd-hero-coins",
                    "gd-turn-slots", "gd-discard"
                })
                {
                    Assert.That(Descendants(rail).Any(element => element.name == secondary), Is.True,
                        $"The artistic hierarchy must not remove {secondary}'s match information.");
                }
            }
        }

        /// <summary>
        /// Milestone 8.49. Beside the level, what defeating the hero is worth: the half-coin,
        /// half-heart badge carrying p.13's number - the assist coins a Defeat pays and the Life
        /// counters it costs, one digit for both. Every hero in the official setup starts at Level 1
        /// holding Tier I cards, so every badge reads 1 here; the number itself is
        /// <c>HeroDefeatCostQuery</c>'s, proven on progressed boards in <c>DefeatCostByTierTests</c>.
        /// </summary>
        [Test]
        public void TheWorthBadge_SitsBesideTheLevelAndReadsWhatDefeatingTheHeroIsWorth()
        {
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(CreateOfficialSixPlayerSession(), view).Refresh();

            Assert.That(view.Model.Heroes.Select(hero => hero.Worth), Has.All.EqualTo(1),
                "Six Level 1 heroes holding Tier I cards are each worth one coin and one counter.");
            var rails = Descendants(view.Root).Where(element => element.name == "gd-hero-rail").ToArray();
            Assert.That(rails, Has.Length.EqualTo(6));
            foreach (var rail in rails)
            {
                var levelRow = Descendants(rail).SingleOrDefault(element => element.name == "gd-hero-level-row");
                Assert.That(levelRow, Is.Not.Null, "The level and the worth share one row.");
                Assert.That(levelRow.Children().Any(child => child.name == "gd-hero-level"), Is.True);
                var badge = levelRow.Children().SingleOrDefault(child => child.name == "gd-hero-worth");
                Assert.That(badge, Is.Not.Null, "The worth badge sits beside the level.");
                Assert.That(badge.Children().Any(child => child.name == "gd-hero-worth-coin"), Is.True);
                Assert.That(badge.Children().Any(child => child.name == "gd-hero-worth-heart"), Is.True);
                var digit = Descendants(badge).OfType<Label>()
                    .SingleOrDefault(label => label.name == "gd-hero-worth-value");
                Assert.That(digit, Is.Not.Null);
                Assert.That(digit.text, Is.EqualTo("1"));
                Assert.That(badge.tooltip, Does.Contain("1 assist coin").And.Contain("1 life counter"));

                // Milestone 8.58. The user, on a crop of this badge: "Hard to see this number one
                // here." It was 16 units with a 10-unit digit painted straight onto the two saturated
                // halves. It is 22 now, the digit is 62% of that, and it stands white on a dark disc
                // of its own so it reads the same over the gold half and over the red one.
                Assert.That(badge.style.width.value.value,
                    Is.EqualTo(HudElements.WorthBadgeSize).Within(0.001f));
                Assert.That(badge.style.height.value.value,
                    Is.EqualTo(HudElements.WorthBadgeSize).Within(0.001f));
                Assert.That(digit.style.fontSize.value.value,
                    Is.GreaterThanOrEqualTo(13f),
                    "The digit is read at a hero rail's own scale, so it is bigger than the LVL " +
                    "label beside it rather than smaller.");
                Assert.That(digit.style.color.value, Is.EqualTo(MatchHudTheme.PrimaryText),
                    "White, not the coin's dark ink, because half the badge behind it is red.");
                // Milestone 8.58, the user on the first attempt: "maybe no circle background for
                // number, just black outline?" - the disc was the half of the badge that stopped
                // being visible.
                Assert.That(badge.Children().Any(child => child.name == "gd-hero-worth-ink"),
                    Is.False,
                    "No disc behind the digit: both coloured halves stay fully visible.");
                Assert.That(digit.style.unityTextOutlineWidth.value,
                    Is.EqualTo(HudElements.WorthDigitOutlineWidth).Within(0.001f),
                    "The digit carries its own black outline instead.");
                Assert.That(digit.style.unityTextOutlineColor.value,
                    Is.EqualTo(HudElements.WorthDigitOutline));
                Assert.That(digit.style.fontSize.value.value,
                    Is.EqualTo(HudElements.WorthBadgeSize * HudElements.WorthDigitScale)
                        .Within(0.001f),
                    "\"Make it just barely fit the circle\" - the digit is larger than the badge's own " +
                    "box, because a cap height is about 0.72 of an em.");
                Assert.That(digit.style.top.value.value,
                    Is.EqualTo(HudElements.WorthBadgeSize * HudElements.WorthDigitScale *
                        HudElements.WorthDigitInkNudge).Within(0.001f),
                    "and it is moved by the MEASURED difference between its line box's centre and its " +
                    "ink's, so the glyph centres on the disc rather than the box doing so: \"just " +
                    "center the number in the circle\", then \"number is not vertically centered " +
                    "still\".");
            }
        }

        [Test]
        public void ExperimentalOneLifeBase_KeepsTierNumbersAndMakesEveryTrayBadgeYellowOnly()
        {
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(CreateOfficialSixPlayerSession(true), view).Refresh();
            Assert.That(view.Model.Heroes.Select(hero => hero.Worth), Has.All.EqualTo(1));
            Assert.That(view.Model.Heroes, Has.All.Matches<HeroOverviewViewModel>(
                hero => hero.WorthRepresentsAssistCoinsOnly));
            foreach (var badge in Descendants(view.Root).Where(element => element.name == "gd-hero-worth"))
            {
                var coin = badge.Q<VisualElement>("gd-hero-worth-coin");
                var heart = badge.Q<VisualElement>("gd-hero-worth-heart");
                Assert.That(heart.style.backgroundColor.value, Is.EqualTo(coin.style.backgroundColor.value));
                Assert.That(badge.tooltip, Does.Contain("assist coin").And.Not.Contain("costs their team"));
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        public void ExperimentalOneLifeBase_ProgressedTrayKeepsItsTierNumber(int tier)
        {
            var board = SabinaBoard.Create(
                tier: tier,
                experiments: new ExperimentalMatchOptions(heroDefeatBaseLifeCostIsOne: true));
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(board.Session, view).Refresh();
            var enemy = view.Model.Heroes.Single(hero => hero.HeroId.Equals(board.Enemy));
            Assert.That(enemy.Worth, Is.EqualTo(tier));
            Assert.That(enemy.WorthRepresentsAssistCoinsOnly, Is.True);
            var rail = Descendants(view.Root).Single(element =>
                element.name == "gd-hero-rail" &&
                element.userData is HeroId id &&
                id.Equals(board.Enemy));
            var badge = Descendants(rail)
                .Single(element => element.name == "gd-hero-worth");
            Assert.That(
                badge.Q<VisualElement>("gd-hero-worth-heart").style.backgroundColor.value,
                Is.EqualTo(
                    badge.Q<VisualElement>("gd-hero-worth-coin").style.backgroundColor.value));
        }

        /// <summary>
        /// A rail with no content identity behind it - which no configured setup produces, and which a
        /// hand-built projection can - addresses no portrait and still draws a rail. The absence is
        /// stated rather than papered over with a letter.
        /// </summary>
        [Test]
        public void ARailWithNoContentIdentity_AddressesNoPortraitAndDrawsNoLetter()
        {
            var hero = Overview(default, "A Hero With No Content", TeamSide.Blue);
            Assert.That(hero.PortraitResourcePath, Is.Null);
            Assert.That(hero.PortraitAssistiveLabel, Does.Contain("A Hero With No Content"));
        }

        [Test]
        public void FiveHeroesPerTeam_KeepEveryRailVisibleAndOnlyTheViewedRailExpanded()
        {
            var heroes = Enumerable.Range(0, 5)
                .Select(index => Overview(
                    "red-" + index, "Red " + index, TeamSide.Red, index == 2))
                .Concat(Enumerable.Range(0, 5).Select(index => Overview(
                    "blue-" + index, "Blue " + index, TeamSide.Blue, false)))
                .ToArray();
            var view = new HeroOverviewsUnityView();

            view.Render(new HeroOverviewsViewModel(1, heroes));

            var rails = Descendants(view.Root)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Has.Length.EqualTo(10));
            Assert.That(rails.Count(rail => rail.ClassListContains("gd-hero-rail-compact")),
                Is.EqualTo(9));
            var expanded = rails.Single(rail =>
                rail.userData is HeroId id && id.Equals(new HeroId("red-2")));
            Assert.That(expanded.ClassListContains("gd-hero-rail-compact"), Is.False);
            Assert.That(Descendants(expanded).Any(element => element.name == "gd-turn-slots"), Is.True);
            Assert.That(rails.Where(rail => rail.ClassListContains("gd-hero-rail-compact"))
                .All(rail => Descendants(rail).Any(element =>
                    element.name == "gd-compact-card-row")), Is.True);
            Assert.That(rails.Where(rail => rail.ClassListContains("gd-hero-rail-compact"))
                .All(rail => Descendants(rail).Any(element =>
                    element.name == "gd-turn-slots") &&
                    Descendants(rail).Any(element =>
                        element.name == "gd-current-card-slot") &&
                    Descendants(rail).Any(element =>
                        element.name == "gd-hero-medallion") &&
                    Descendants(rail).All(element =>
                        element.name != "gd-hero-identity-plate" &&
                        element.name != "gd-turn-slot-active")), Is.True);
        }

        private static HeroOverviewViewModel Overview(
            HeroContentId contentId, string displayName, TeamSide teamSide) =>
            new HeroOverviewViewModel(
                new HeroId("hero-under-test"),
                teamSide,
                contentId,
                displayName,
                1,
                0,
                Array.Empty<HeroItemBonusViewModel>(),
                Array.Empty<HeroTurnSlotViewModel>(),
                Array.Empty<TrayCardViewModel>(),
                false);

        private static HeroOverviewViewModel Overview(
            string heroId, string displayName, TeamSide teamSide, bool isViewed) =>
            new HeroOverviewViewModel(
                new HeroId(heroId),
                teamSide,
                default,
                displayName,
                1,
                0,
                Array.Empty<HeroItemBonusViewModel>(),
                Array.Empty<HeroTurnSlotViewModel>(),
                Array.Empty<TrayCardViewModel>(),
                isViewed);

        private static Color32 Pixel(Color32[] pixels, int x, int y) =>
            pixels[(y * PreparedTokenSize) + x];

        private static System.Collections.Generic.IEnumerable<VisualElement> Descendants(
            VisualElement element)
        {
            foreach (var child in element.Children())
            {
                yield return child;
                foreach (var nested in Descendants(child))
                {
                    yield return nested;
                }
            }
        }

        /// <summary>
        /// Rulebook p.5's six-player Quick Game, composed exactly as
        /// <c>OfficialSixPlayerSetupTests</c> composes it.
        /// </summary>
        private static MatchSession CreateOfficialSixPlayerSession(bool heroDefeatBaseLifeCostIsOne = false)
        {
            var configuration = MatchConfiguration
                .CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficialSixPlayer(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(
                scenario,
                setup,
                new[] { map },
                experiments: new ExperimentalMatchOptions(
                    heroDefeatBaseLifeCostIsOne: heroDefeatBaseLifeCostIsOne)));
        }
    }
}
