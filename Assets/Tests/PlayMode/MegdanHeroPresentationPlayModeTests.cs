using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class MegdanHeroPresentationPlayModeTests
    {
        private static readonly IReadOnlyDictionary<string, CardDefinition> Representatives =
            new Dictionary<string, CardDefinition>
            {
                ["brogan"] = BroganCardContent.DealtDefinitions.First(definition =>
                    definition.Progression.Color == CardColor.Red),
                ["misa"] = MisaCardContent.DealtDefinitions.First(definition =>
                    definition.Progression.Color == CardColor.Blue),
                ["rowenna"] = RowennaCardContent.DealtDefinitions.First(definition =>
                    definition.Progression.Color == CardColor.Green),
                ["tigerclaw"] = TigerclawCardContent.DealtDefinitions.First(definition =>
                    definition.Progression.Color == CardColor.Silver),
                ["gydion"] = GydionCardContent.DealtDefinitions.First(definition =>
                    definition.Progression.Color == CardColor.Gold),
                ["tali"] = TaliCardContent.DealtDefinitions.First(definition =>
                    definition.Progression.Color == CardColor.None)
            };

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<CardDefinition>> AllMapped =
            new Dictionary<string, IReadOnlyList<CardDefinition>>
            {
                ["Brogan"] = BroganCardContent.DealtDefinitions,
                ["Riki"] = MisaCardContent.DealtDefinitions,
                ["Rowenna"] = RowennaCardContent.DealtDefinitions,
                ["Tigerclaw"] = TigerclawCardContent.DealtDefinitions,
                ["Gydion"] = GydionCardContent.DealtDefinitions,
                ["Tali"] = TaliCardContent.DealtDefinitions
            };

        [UnityTest]
        public IEnumerator CaptureSixMappedHeroesInTheLiveMatch()
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            var red = new[] { "brogan", "misa", "rowenna" };
            var blue = new[] { "tigerclaw", "gydion", "tali" };
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                red.Select(id => roster.Single(hero => hero.ContentId.Value == id)),
                blue.Select(id => roster.Single(hero => hero.ContentId.Value == id)));
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(
                SceneCardContent.ConfiguredMatch);
            UiPlayer.SetPlayerViewport(1920u, 1080u, "Six mapped Megdan heroes in a local 3v3 match");
            var root = BroganUiJourneyPlayModeTests.Root();
            root.Runtime.SelectViewedHero(new GuardsDigital.Core.Units.Heroes.HeroId("hero-misa"));
            root.Hud.Layout();
            yield return new WaitForSecondsRealtime(1f);
            var heroes = root.Runtime.BoardView.HeroViews.Where(view =>
                view.HeroContentId.HasValue && view.gameObject.activeInHierarchy).ToArray();
            Assert.That(heroes.Select(view => view.HeroContentId.Value.Value),
                Is.EquivalentTo(red.Concat(blue)), "The screenshot must contain this exact six-hero lineup.");
            Assert.That(heroes.All(view => view.HasTokenVisual), Is.True);
            yield return UiPlayer.CaptureScreen("tmp/w168-six-heroes-live-match.png");
        }

        [UnityTest]
        public IEnumerator All108MappedFaces_KeepTheStandardFootprintAndTheirTypedTextVisible()
        {
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(
                SceneCardContent.ConfiguredMatch);
            var root = BroganUiJourneyPlayModeTests.Root();
            foreach (var pair in AllMapped)
            {
                yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                    pair.Key, pair.Value, root.Hud);
            }
        }

        [UnityTest]
        public IEnumerator LivePanel_DrawsAllSixMegdanPortraitCardsAndPreparedRuntimePortraits()
        {
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(
                SceneCardContent.ConfiguredMatch);
            UiPlayer.SetPlayerViewport(1920u, 1080u, "W-168 approved Megdan card design");
            yield return null;
            yield return null;
            var root = BroganUiJourneyPlayModeTests.Root();

            foreach (var pair in Representatives)
            {
                var content = new HeroContentId(pair.Key);
                var cardArt = Resources.Load<Texture2D>("Private/Megdan/HeroPortraits/" + pair.Key);
                Assert.That(cardArt, Is.Not.Null, pair.Key + " card art");
                Assert.That(cardArt.width, Is.EqualTo(1024), pair.Key + " panel aspect");
                Assert.That(cardArt.height, Is.EqualTo(840), pair.Key + " panel aspect");
                Assert.That(Resources.Load<Texture2D>(
                    "Private/HeroSelection/hero-" + pair.Key), Is.Not.Null, pair.Key + " selector portrait");
                Assert.That(Resources.Load<Texture2D>(
                    HeroTokenArtwork.NeutralResourcePath(content)), Is.Not.Null, pair.Key + " token portrait");
                foreach (var team in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var presentation = HeroTokenArtwork.ResolveToken(content, team, out var resolved);
                    Assert.That(resolved, Is.Not.Null, pair.Key + " " + team);
                    Assert.That(presentation.FaceKind, Is.EqualTo(TokenFaceKind.TeamNeutral),
                        "The mapped portrait must keep the runtime team-colour rim.");
                }
            }

            var gallery = new VisualElement { name = "gd-megdan-card-gallery" };
            gallery.style.position = Position.Absolute;
            gallery.style.left = 0f;
            gallery.style.top = 0f;
            gallery.style.right = 0f;
            gallery.style.bottom = 0f;
            gallery.style.flexDirection = FlexDirection.Row;
            gallery.style.alignItems = Align.Center;
            gallery.style.justifyContent = Justify.SpaceAround;
            gallery.style.backgroundColor = new Color(0.055f, 0.07f, 0.055f, 0.98f);
            gallery.style.paddingLeft = 10f;
            gallery.style.paddingRight = 10f;
            root.Hud.RootElement.Add(gallery);

            foreach (var pair in Representatives)
            {
                var column = new VisualElement();
                column.style.alignItems = Align.Center;
                var heroName = MegdanHeroPresentationCatalog.DisplayName(
                    new HeroContentId(pair.Key), pair.Key);
                var heading = new Label(heroName.ToUpperInvariant());
                heading.style.fontSize = 18f;
                heading.style.unityFontStyleAndWeight = FontStyle.Bold;
                heading.style.color = new Color(0.93f, 0.82f, 0.54f, 1f);
                heading.style.marginBottom = 8f;
                column.Add(heading);
                column.Add(DirectorHandsUnityView.BuildStandaloneCard(
                    CurrentRoundCards.DeckCard(
                        new CardInstanceId("w161-gallery-" + pair.Key), pair.Value)));
                gallery.Add(column);
            }

            yield return null;
            yield return null;
            Assert.That(gallery.Query<VisualElement>("gd-megdan-card-frame").ToList(), Has.Count.EqualTo(6));
            Assert.That(gallery.Query<Image>("gd-card-artwork").ToList(), Has.Count.EqualTo(6));
            Assert.That(gallery.Query<VisualElement>("gd-card-movement-arrow").ToList(), Has.Count.EqualTo(2),
                "Only Mad Dash and Sudden Breeze have secondary Movement in this gallery.");
            var madDash = gallery.Query<Label>("gd-card-name").ToList().Single(label => label.text == "Mad Dash");
            Assert.That(madDash.parent.parent.parent.Q<Label>("gd-card-printed-text").resolvedStyle.fontSize,
                Is.GreaterThanOrEqualTo(11f), "Ordinary rules text should use the larger approved hierarchy.");
            yield return UiPlayer.CaptureScreen("tmp/w168-megdan-card-gallery.png");
            // The same live vector faces enlarged for visual inspection, including a two-digit
            // initiative and a missing secondary Movement. The six-colour capture stays at game size.
            while (gallery.childCount > 3)
                gallery.RemoveAt(gallery.childCount - 1);
            foreach (var column in gallery.Children())
                column.style.scale = new Scale(new Vector3(1.65f, 1.65f, 1f));
            yield return null;
            yield return null;
            yield return UiPlayer.CaptureScreen("tmp/w168-card-icons-detail.png");
            gallery.RemoveFromHierarchy();
            yield return null;

            foreach (var hero in root.Runtime.BoardView.HeroViews.Where(view =>
                         view.HeroContentId.HasValue &&
                         (view.HeroContentId.Value.Value == "brogan" ||
                          view.HeroContentId.Value.Value == "tigerclaw")))
            {
                Assert.That(hero.HasTokenVisual, Is.True, hero.HeroContentId.Value.Value);
            }

            yield return UiPlayer.CaptureScreen("tmp/w168-megdan-board-tokens.png");
        }
    }
}
