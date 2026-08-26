using System;
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

                // Its footprint is the badge's, to the unit: a hero rail's height is under a measured
                // ceiling and a three-up budget that rulebook p.6's three heroes a side depend on, so
                // the change deliberately buys no height.
                Assert.That(disc.style.width.value.value, Is.EqualTo(30f).Within(0.01f));
                Assert.That(disc.style.height.value.value, Is.EqualTo(30f).Within(0.01f));

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
        private static MatchSession CreateOfficialSixPlayerSession()
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
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
