using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class DoubleLaneFivePlayerPresentationTests
    {
        [UnityTest]
        public IEnumerator BothLargeMaps_ShowFiveRailsPerTeamWithoutCrossingTheHandBoundary()
        {
            foreach (var map in new[]
            {
                OfficialBoardMapCatalog.GreenDoubleLane,
                OfficialBoardMapCatalog.LavaBridgeDoubleLane
            })
            {
                MatchSceneCompositionRoot.NextComposedSeat = new SeatId("red-player-01");
                MatchSceneCompositionRoot.NextComposedMap = map;
                MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.DefaultForTeamSize(5);
                MatchSceneCompositionRoot.NextComposedLineup = RegisteredHeroRoster.DefaultLineup(5);
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(
                    SceneCardContent.ConfiguredMatch);

                var root = ArienUiJourneyPlayModeTests.Root();
                root.Recompose(new MatchCompositionPlan(
                    MatchFormat.DefaultForTeamSize(5),
                    RegisteredHeroRoster.DefaultLineup(5),
                    map,
                    WaveCounterAllocation.PerLane));
                yield return null;
                yield return null;
                Assert.That(Object.FindAnyObjectByType<PlaceholderTestGridView>()
                    .RenderedDefinition.Reference, Is.EqualTo(map));
                Assert.That(root.Runtime.ReadModel.Setup.Heroes, Has.Count.EqualTo(10));
                Assert.That(root.Runtime.ReadModel.Setup.WaveCounterAllocation,
                    Is.EqualTo(WaveCounterAllocation.PerLane));
                Assert.That(root.Hud.RootElement.Q<Label>("gd-waves-remaining").text,
                    Is.EqualTo("WAVES REMAINING  LANE 1 7  ·  LANE 2 7"));

                foreach (var width in new[] { 1920u, 2560u })
                {
                    UiPlayer.SetPlayerViewport(width, width * 9 / 16, "W-072 five-player rails");
                    root.Hud.Layout();
                    yield return null;
                    yield return null;

                    var rails = UiPlayer.Descendants(root.Hud.RootElement)
                        .Where(element => element.name == "gd-hero-rail")
                        .ToArray();
                    Assert.That(rails, Has.Length.EqualTo(10));
                    Assert.That(rails.Count(rail =>
                        rail.ClassListContains("gd-hero-rail-compact")), Is.EqualTo(8),
                        "At the opening commitment, one awaited hero per team is expanded.");

                    var compactRail = rails.First(rail =>
                        rail.ClassListContains("gd-hero-rail-compact"));
                    var hoveredId = (HeroId)compactRail.userData;
                    var hoveredColumn = compactRail.parent;
                    var compactCurrent = UiPlayer.Descendants(rails.First(rail =>
                            rail.userData is HeroId id && id.Equals(hoveredId)))
                        .Single(element => element.name == "gd-current-card-slot");
                    Assert.That(
                        compactCurrent.resolvedStyle.width / compactCurrent.resolvedStyle.height,
                        Is.EqualTo(
                            HeroOverviewsUnityView.CurrentCardWidth /
                            HeroOverviewsUnityView.CurrentCardHeight).Within(0.03f),
                        "The compact current-card marker keeps its original proportions.");
                    var compactSlots = UiPlayer.Descendants(rails.First(rail =>
                            rail.userData is HeroId id && id.Equals(hoveredId)))
                        .Single(element => element.name == "gd-turn-slots");
                    Assert.That(
                        rails.First(rail => rail.userData is HeroId id && id.Equals(hoveredId))
                            .resolvedStyle.height - compactSlots.resolvedStyle.height,
                        Is.LessThanOrEqualTo(10f),
                        "The compact tray must hug the outlined turn-slot row without an empty label strip below it.");

                    UiPlayer.PointAt(rails.First(rail =>
                        rail.userData is HeroId id && id.Equals(hoveredId)));
                    yield return null;
                    yield return null;
                    rails = UiPlayer.Descendants(root.Hud.RootElement)
                        .Where(element => element.name == "gd-hero-rail")
                        .ToArray();
                    var hovered = rails.Single(rail =>
                        rail.userData is HeroId id && id.Equals(hoveredId));
                    Assert.That(hovered.ClassListContains("gd-hero-rail-compact"), Is.False,
                        "A compact hero tray expands while the mouse is over it.");
                    Assert.That(UiPlayer.Descendants(hovered).Any(element =>
                        element.name == "gd-hero-identity-plate"), Is.True,
                        "Hover expansion presents the complete tray, not only a larger compact row.");
                    Assert.That(rails.Count(rail =>
                        rail.ClassListContains("gd-hero-rail-compact")), Is.EqualTo(7));

                    if (width == 2560u && map.Equals(OfficialBoardMapCatalog.LavaBridgeDoubleLane))
                        yield return UiPlayer.CaptureScreen("tmp/w076-five-player-hover-expanded.png");

                    UiPlayer.PointAway(hoveredColumn);
                    yield return null;
                    yield return null;
                    rails = UiPlayer.Descendants(root.Hud.RootElement)
                        .Where(element => element.name == "gd-hero-rail")
                        .ToArray();
                    Assert.That(rails.Count(rail =>
                        rail.ClassListContains("gd-hero-rail-compact")), Is.EqualTo(8),
                        "Leaving the tray restores the compact layout.");

                    foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                    {
                        var column = root.Hud.RootElement.Q<VisualElement>(
                            side == TeamSide.Red ? "gd-hero-rail-red" : "gd-hero-rail-blue");
                        var teamRails = column.Children()
                            .Where(element => element.name == "gd-hero-rail")
                            .ToArray();
                        Assert.That(teamRails, Has.Length.EqualTo(5));
                        Assert.That(teamRails.Max(rail => rail.worldBound.yMax),
                            Is.LessThanOrEqualTo(column.worldBound.yMax + UiPlayer.PixelSnapping(2)),
                            $"{map} {side} rails cross the card-hand boundary at {width}x{width * 9 / 16}.");
                    }

                    if (width == 2560u)
                    {
                        yield return UiPlayer.CaptureScreen(
                            "tmp/w072-" + map.MapId.Value + "-5v5.png");
                    }
                }
            }

            UiPlayer.SetPlayerViewport(1920, 1080, "restore W-072 presentation viewport");
        }
    }
}
