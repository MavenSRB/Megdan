using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace GuardsDigital.Tests.PlayMode
{
    public sealed class HeroCardInspectionPlayModeTests
    {
        [UnityTest] public IEnumerator PortraitFiltersAndCloseInspectWithoutChangingGameplayAtBothSizes()
        {
            var oldWidth = (uint)Screen.width; var oldHeight = (uint)Screen.height;
            try
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
                var root = ArienUiJourneyPlayModeTests.Root();
                root.ConfigureExperiments(new ExperimentalMatchOptions(teammateCoordination: true, enemyCardInformation: true));
                var roster = RegisteredHeroRoster.ForThisBuild;
                var printed = RegisteredHeroRoster.PrintedLineup(3);
                var gydion = roster.Single(h => h.ContentId.Value == "gydion");
                var takahide = roster.Single(h => h.ContentId.Value == "takahide");
                MatchSceneCompositionRoot.NextComposedSeat = new SeatId("red-player-01");
                root.ComposeForLineup(new HeroLineup(new[] { gydion, takahide, printed.Red.Last() }, printed.Blue));
                yield return null; yield return null;
                var runtime = root.Runtime; var host = runtime.Host; var view = runtime.HeroOverviewsView;
                var own = host.Seats.Seats.First(s => s.Id.Value == "red-player-01");
                var ally = host.Projection.Setup.Heroes.Single(h => h.ContentId.Value == "takahide").HeroId;
                var enemy = host.Seats.Seats.First(s => s.TeamSide != own.TeamSide).HeroId;
                var version = host.Version;
                foreach (var width in new[] { 1280u, 1920u })
                {
                    UiPlayer.SetPlayerViewport(width, width * 9 / 16, "current card inspection");
                    root.Hud.Layout(); yield return null; yield return null;
                    foreach (var hero in new[] { own.HeroId, ally, enemy })
                    {
                        var rail = UiPlayer.Descendants(root.Hud.RootElement).Single(e => e.name == "gd-hero-rail" && e.userData is HeroId id && id.Equals(hero));
                        UiPlayer.Click(rail.Q("gd-hero-medallion"));
                        yield return null; yield return null;
                        var window = view.CardInspection.Root;
                        Assert.That(UiPlayer.IsShown(window), Is.True);
                        Assert.That(view.CardInspection.Hero, Is.EqualTo(hero));
                        Assert.That(window.Query<VisualElement>("gd-inspected-card").ToList().Count,
                            Is.EqualTo(hero.Equals(ally) ? 7 : 5));
                        var first = window.Q("gd-inspected-card");
                        var original = first.worldBound;
                        UiPlayer.PointAt(first); yield return new WaitForSecondsRealtime(0.25f);
                        Assert.That(first.worldBound.width, Is.EqualTo(original.width * MatchHudTheme.ButtonHoverScale).Within(0.6f));
                        UiPlayer.Click(first); yield return null;
                        Assert.That(host.Version, Is.EqualTo(version), "Card faces have no gameplay actions.");
                        UiPlayer.PointAway(first); yield return new WaitForSecondsRealtime(0.25f);
                        if (hero.Equals(ally))
                        {
                            yield return Choose(window.Q<DropdownField>("gd-hero-card-color"), "GOLD");
                            Assert.That(window.Query<VisualElement>("gd-inspected-card").ToList().Count, Is.EqualTo(3));
                            yield return Choose(window.Q<DropdownField>("gd-hero-card-tier"), "I");
                            Assert.That(window.Query<VisualElement>("gd-inspected-card").ToList(), Is.Empty);
                            yield return Choose(window.Q<DropdownField>("gd-hero-card-tier"), "All");
                            yield return Choose(window.Q<DropdownField>("gd-hero-card-color"), "All");
                            Assert.That(window.Query<VisualElement>("gd-inspected-card").ToList().Count, Is.EqualTo(7));
                            var lastFace=window.Query<VisualElement>("gd-inspected-card").ToList().Last();
                            var lowerPoint=new Vector2(lastFace.worldBound.center.x,
                                Mathf.Min(lastFace.worldBound.yMax-10,window.Q<ScrollView>("gd-hero-card-scroll").worldBound.yMax-10));
                            Assert.That(window.Contains(window.panel.Pick(lowerPoint)),Is.True,
                                "Ordinary hand cards must not cover or intercept the inspected card's visible bottom edge.");
                            Assert.That(root.Hud.BlocksPointerAt(MatchHudPanelScaling.PanelPointToScreen(window.worldBound.center,Screen.width,Screen.height)),Is.True,
                                "Clicks in the inspection window cannot fall through to the board.");
                            var retained = window.Q("gd-inspected-card");
                            var beforeMarker = ((MatchHost)host).ProjectionForSeat(own.Id);
                            var previous = beforeMarker.TeammateMarkers.FirstOrDefault(m=>m.Sender.Equals(own.HeroId));
                            Assert.That(((MatchHost)host).SubmitCoordinationForSeat(own.Id,new CoordinationCommand(previous?.Revision??0,
                                CoordinationTargetKind.Hex,false,runtime.ReadModel.SelectedMap.Reference,
                                runtime.ReadModel.SelectedMap.Spaces.First().Coordinate,generation:beforeMarker.CoordinationGeneration)),Is.True);
                            yield return null;yield return null;
                            Assert.That(view.CardInspection.IsOpen,Is.True);
                            Assert.That(window.Q("gd-inspected-card"),Is.SameAs(retained),"A marker update must preserve the open inspection window and its card faces.");
                            runtime.RefreshClockDisplay(); yield return null;
                            Assert.That(window.Q("gd-inspected-card"), Is.SameAs(retained), "Clock ticks cannot rebuild the window.");
                            var scroll = window.Q<ScrollView>("gd-hero-card-scroll");
                            Assert.That(scroll.contentContainer.worldBound.width, Is.LessThanOrEqualTo(scroll.worldBound.width + 1));
                            Assert.That(scroll.horizontalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
                            yield return UiPlayer.CaptureScreen("tmp/964e-current-cards-" + width + ".png");
                        }
                        UiPlayer.Click(window.Q<Button>("gd-hero-card-close")); yield return null;
                        Assert.That(view.CardInspection.IsOpen, Is.False);
                    }
                    var menu = new PrivateLobbyUnityView(); menu.ShowPage(PrivateLobbyEntryPage.Host);
                    root.Hud.RootElement.Add(menu.Root);
                    menu.Root.style.position = Position.Absolute; menu.Root.style.left = 0; menu.Root.style.top = 0;
                    menu.Root.style.width = Length.Percent(100); menu.Root.style.height = Length.Percent(100);
                    yield return null; yield return null;
                    UiPlayer.Click(menu.Root.Q<Button>("experimental-button")); yield return null; yield return null;
                    UiPlayer.Click(menu.Root.Q<Toggle>("experimental-enemy-info"));
                    Assert.That(menu.CurrentEntry().Experiments.EnemyCardInformation, Is.True);
                    Assert.That(menu.CurrentEntry().Experiments.TeammateCoordination, Is.False);
                    UiPlayer.Click(menu.Root.Q<Toggle>("experimental-timer"));
                    yield return null; yield return null;
                    Assert.That(UiPlayer.IsShown(menu.Root.Q<IntegerField>("experimental-duration-4")), Is.True);
                    yield return UiPlayer.CaptureScreen("tmp/964e-host-menu-" + width + ".png");
                    menu.Root.RemoveFromHierarchy();
                }
                Assert.That(host.Version, Is.EqualTo(version));
                root.ConfigureExperiments(ExperimentalMatchOptions.Disabled);
            }
            finally
            {
                MatchSceneCompositionRoot.NextComposedSeat = null;
                UiPlayer.SetPlayerViewport(oldWidth, oldHeight, "restore after inspection");
            }
        }
        private static IEnumerator Choose(DropdownField field, string value)
        {
            UiPlayer.Click(field); yield return null; yield return null;
            var item = field.panel.visualTree.Query<Label>().ToList().Last(label => label.text == value && UiPlayer.IsShown(label));
            AlternateSmallMapPlayModeTests.ClickDropdownItem(item); yield return null; yield return null;
            Assert.That(field.value, Is.EqualTo(value));
        }
    }
}
