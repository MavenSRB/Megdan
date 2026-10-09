using System;
using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class CardValueOverlayPlayModeTests
    {
        [UnityTest]
        public IEnumerator RealCreepModifierReachesHandAndInspection()
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            var wasp = roster.Single(h => h.ContentId.Value == "wasp");
            var owner = new HeroId(wasp.HeroIdValue);
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.TwoVersusTwoQuick;
                var lineup = new HeroLineup(
                    new[] { wasp, roster.Single(h => h.ContentId.Value == "sabina") },
                    new[] { roster.Single(h => h.ContentId.Value == "arien"),
                        roster.Single(h => h.ContentId.Value == "xargatha") });
                MatchSceneCompositionRoot.NextComposedLineup = lineup;
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
                var removed = ArienUiJourneyPlayModeTests.Root().Runtime.Setup.Minions.Select(m => m.MinionId).ToArray();
                MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.TwoVersusTwoQuick;
                MatchSceneCompositionRoot.NextComposedLineup = lineup;
                MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                    heroes: new[] { new SandboxHeroStaging(owner, new HexCoordinate(-3, 1)) },
                    removedMinions: removed,
                    spawnedMinions: new[] { new SandboxMinionStaging(new MinionId("card-preview-creep"),
                        side, StandardMinionType.Melee, new HexCoordinate(-2, 1)) });
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
                var root = ArienUiJourneyPlayModeTests.Root();
                root.Runtime.SelectViewedHero(owner);
                UiPlayer.SetPlayerViewport(1920u, 1080u, "live creep card bonus");
                root.Hud.Layout(); yield return null; yield return null;
                var expected = side == TeamSide.Red ? 1 : -1;
                var card = root.Runtime.HandsView.Model.ViewedHand.TrayCards.Single(c =>
                    c.DefinitionReference.DefinitionId.Equals(WaspCardContent.MagneticDagger));
                Assert.That(card.ValueBonuses.Single(v => v.Channel == PrintedValueChannel.Defense).Bonus,
                    Is.EqualTo(expected));
                Assert.That(root.Runtime.HandsView.Root.Query<Label>("gd-card-bonus-defense").ToList()
                    .Any(label => label.text == CardValueOverlay.Signed(expected)), Is.True);
                yield return UiPlayer.CaptureScreen("tmp/w012-live-creep-" + side + "-hand.png");
                var inspection = root.Runtime.HeroOverviewsView.CardInspection;
                inspection.UseTrustedLocalHeroPerspective = true;
                Assert.That(inspection.Open(owner, "Wasp"), Is.True);
                root.Hud.Layout(); yield return null; yield return null;
                Assert.That(inspection.Root.Query<Label>("gd-card-bonus-defense").ToList()
                    .Any(label => label.text == CardValueOverlay.Signed(expected)), Is.True);
                yield return UiPlayer.CaptureScreen("tmp/w012-live-creep-" + side + "-inspection.png");
                inspection.Close();
            }
        }

        [UnityTest]
        public IEnumerator RenderedBonusesStayRightOfValuesInsideCardAtEveryDigitWidth()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = ArienUiJourneyPlayModeTests.Root();
            UiPlayer.SetPlayerViewport(1920u, 1080u, "card modifier render validation");
            root.Hud.Layout();
            var shelf = new VisualElement { name = "gd-card-value-validation" };
            shelf.style.position = Position.Absolute;
            shelf.style.left = 100; shelf.style.top = 110;
            shelf.style.width = 2360; shelf.style.height = 470;
            shelf.style.backgroundColor = new Color(0.045f, 0.055f, 0.07f);
            shelf.style.flexDirection = FlexDirection.Row;
            shelf.style.flexWrap = Wrap.Wrap;
            shelf.style.alignContent = Align.FlexStart;
            shelf.style.paddingTop = 24; shelf.style.paddingLeft = 24;
            root.Hud.RootElement.Add(shelf);
            var definitions = WaspCardContent.AllDefinitions.Concat(XargathaCardContent.AllDefinitions)
                .Concat(MisaCardContent.AllDefinitions).ToArray();
            var selected = new[] { "wasp-magnetic-dagger", "xargatha-dangerous-slash",
                "misa-watch-how-i-soar", "wasp-static-barrier", "wasp-control-gravity",
                "xargatha-long-thrust" }.Select(id => definitions.Single(d => d.Id.Value == id)).ToArray();
            try
            {
                foreach (var amount in new[] { 1, 12, -12 })
                {
                    shelf.Clear();
                    foreach (var definition in selected)
                    {
                        var face = CurrentRoundCards.ReadFace(definition);
                        var values = new[] { face.Initiative, face.PrimaryValue, face.SpatialValue }
                            .Concat(face.SecondaryValues).Where(v => v != null).Select(v =>
                                new CardValuePreview(new HeroId("validation-owner"),
                                    definition.Reference, v.Channel, amount)).ToArray();
                        var model = new TrayCardViewModel(new CardInstanceId("validation:" + definition.Id.Value),
                            definition.Reference, definition.DisplayName, definition.Progression.Color, face,
                            TrayCardGroup.Available, 0, false, false, valueBonuses: values);
                        var card = ConfiguredCardFaceView.Build(new VisualElement(), model);
                        card.style.marginRight = 70; card.style.marginBottom = 50;
                        shelf.Add(card);
                    }
                    yield return null; yield return null; yield return null;
                    foreach (var card in shelf.Children())
                    {
                        foreach (var label in card.Query<Label>().ToList().Where(l => l.name.StartsWith("gd-card-bonus-")))
                        {
                            Assert.That(label.worldBound.xMin, Is.GreaterThan(card.worldBound.xMin));
                            Assert.That(label.worldBound.xMax, Is.LessThanOrEqualTo(card.worldBound.xMax + 0.1f));
                            Assert.That(label.worldBound.yMin, Is.GreaterThan(card.worldBound.yMin));
                            Assert.That(label.worldBound.yMax, Is.LessThan(card.worldBound.yMax));
                            Assert.That(label.resolvedStyle.color, Is.EqualTo(Color.white));
                            Assert.That(label.text, Is.EqualTo(CardValueOverlay.Signed(amount)));
                        }
                        Assert.That(card.Query<Label>().ToList().Count(l => l.name.StartsWith("gd-card-bonus-")),
                            Is.GreaterThan(0));
                    }
                    yield return UiPlayer.CaptureScreen("tmp/w012-rendered-bonuses-" + amount + ".png");
                }
            }
            finally { shelf.RemoveFromHierarchy(); }
        }
    }
}
