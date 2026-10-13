using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
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
