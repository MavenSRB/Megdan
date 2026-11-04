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
            UiPlayer.SetPlayerViewport(1920u, 1080u, "W-161 Megdan hero presentation");
            yield return null;
            yield return null;
            var root = BroganUiJourneyPlayModeTests.Root();

            foreach (var pair in Representatives)
            {
                var content = new HeroContentId(pair.Key);
                Assert.That(Resources.Load<Texture2D>(
                    "Private/Megdan/HeroPortraits/" + pair.Key), Is.Not.Null, pair.Key + " card art");
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
            yield return UiPlayer.CaptureScreen("tmp/w161-megdan-card-gallery.png");
            gallery.RemoveFromHierarchy();
            yield return null;

            foreach (var hero in root.Runtime.BoardView.HeroViews.Where(view =>
                         view.HeroContentId.HasValue &&
                         (view.HeroContentId.Value.Value == "brogan" ||
                          view.HeroContentId.Value.Value == "tigerclaw")))
            {
                Assert.That(hero.HasTokenVisual, Is.True, hero.HeroContentId.Value.Value);
            }

            yield return UiPlayer.CaptureScreen("tmp/w161-megdan-board-tokens.png");
        }
    }
}
