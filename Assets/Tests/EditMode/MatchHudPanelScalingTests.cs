using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25c. The HUD's coordinate space: that the panel declares one, that every layout
    /// number is written in it, and - the check this milestone exists for - that the pointer boundary
    /// converts a screen pixel into it before deciding anything.
    ///
    /// <para>All of it is stated at device viewports the fixture is not running at, because the
    /// question is what happens on a player's screen rather than on the Editor's. Nothing here needs a
    /// live panel: <see cref="MatchHudPanelScaling"/> reproduces the panel's own scale resolution, and
    /// <c>MinimalHexGridPlayModeTests</c> measures a running panel against it so that reproduction is a
    /// measurement rather than a belief.</para>
    /// </summary>
    public sealed class MatchHudPanelScalingTests
    {
        /// <summary>
        /// Device resolutions of the reference's own 16:9 shape, either side of it. Every one of them
        /// resolves to exactly the reference viewport, which is the property that makes the HUD the
        /// same share of all of them.
        /// </summary>
        private static readonly Vector2[] ReferenceShapedResolutions =
        {
            new Vector2(1280f, 720f),
            new Vector2(1600f, 900f),
            new Vector2(1920f, 1080f),
            new Vector2(2048f, 1152f),
            new Vector2(2560f, 1440f),
            new Vector2(3840f, 2160f)
        };

        /// <summary>
        /// THE CLOSING CHECK. A screen position over a region the HUD is <em>drawn</em> in is blocked,
        /// and one over the board between the two rail columns is not - at a panel scale other than 1,
        /// in both directions.
        ///
        /// <para><b>Why this one comes first and why it is stated this way.</b>
        /// <see cref="ConfiguredMatchHudRenderer.BlocksPointerAt"/> is on the path of every board click
        /// in the game. Before this milestone it flipped a screen position with
        /// <c>Screen.height - y</c> and compared the result against rectangles computed from
        /// <c>Screen.width</c>/<c>Screen.height</c> - an identity mapping, and a correct one only while
        /// the panel drew one panel point per device pixel. A scaled panel breaks it silently: nothing
        /// throws, the HUD simply consumes the pointer somewhere other than where it is drawn.</para>
        ///
        /// <para>The two device viewports below sit either side of the reference, so the failure is
        /// exercised in both of its directions. Above the reference the drawn regions are larger than
        /// the unconverted rectangles, so a click on the HUD reaches the board through it; below the
        /// reference they are smaller, so a click on the board is swallowed by a rail that is not
        /// there. Points are sampled on a grid across each region rather than at its centre, because a
        /// centre point lands inside both rectangles for every large region and the case would pass on
        /// luck.</para>
        /// </summary>
        [Test]
        public void ThePointerBoundary_BlocksWhereTheHudIsDrawnAtAPanelScaleOtherThanOne()
        {
            foreach (var device in new[] { new Vector2(3840f, 2160f), new Vector2(1920f, 1080f) })
            {
                var scale = MatchHudPanelScaling.PanelPointsPerScreenPixel(device.x, device.y);
                Assert.That(scale, Is.Not.EqualTo(1f).Within(0.001f),
                    $"{device} is the case only while its panel scale is not 1; it resolved {scale}.");

                var panel = MatchHudPanelScaling.ReferenceViewport(device.x, device.y);
                var red = ConfiguredMatchHudRenderer.CalculateHeroRailRect(
                    panel.x, panel.y, TeamSide.Red);
                var blue = ConfiguredMatchHudRenderer.CalculateHeroRailRect(
                    panel.x, panel.y, TeamSide.Blue);
                var statusDock = ConfiguredMatchHudRenderer.CalculateStatusDockRect(panel.x, panel.y);
                var regions = new (string Name, Rect Rect)[]
                {
                    ("the status strip",
                        ConfiguredMatchHudRenderer.CalculateStatusBarRect(panel.x, panel.y)),
                    ("the compact card hand",
                        ConfiguredMatchHudRenderer.CalculateCompactHandPointerRect(panel.x, panel.y)),
                    ("the fixed status/action dock", statusDock),
                    ("the Red rail column", red),
                    ("the Blue rail column", blue)
                };

                foreach (var region in regions)
                {
                    foreach (var point in Interior(region.Rect))
                    {
                        var screenPosition =
                            MatchHudPanelScaling.PanelPointToScreen(point, device.x, device.y);
                        Assert.That(
                            ConfiguredMatchHudRenderer.StandingRegionsBlockPointerAt(
                                screenPosition, device.x, device.y),
                            Is.True,
                            $"At {device} the HUD draws {region.Name} over the screen position " +
                            $"{screenPosition}, which is panel point {point}, and lets the board have " +
                            "the click anyway.");
                    }
                }

                // The other half: the band of board to the right of the fixed Director/status lane,
                // between the two columns, under the status strip and above the tray, is the player's
                // own surface and nothing on the HUD may take a click there.
                var tray = ConfiguredMatchHudRenderer.CalculateCardPanelRect(panel.x, panel.y);
                var open = Rect.MinMaxRect(
                    Mathf.Max(red.xMax, statusDock.xMax),
                    ConfiguredMatchHudRenderer.StatusBarHeight,
                    blue.xMin,
                    tray.yMin);
                Assert.That(open.width, Is.GreaterThan(0f), $"{device} leaves no board between the rails.");
                Assert.That(open.height, Is.GreaterThan(0f), $"{device} leaves no board under the strip.");
                foreach (var point in Interior(open))
                {
                    var screenPosition =
                        MatchHudPanelScaling.PanelPointToScreen(point, device.x, device.y);
                    Assert.That(
                        ConfiguredMatchHudRenderer.StandingRegionsBlockPointerAt(
                            screenPosition, device.x, device.y),
                        Is.False,
                        $"At {device} the HUD takes the click at screen position {screenPosition}, " +
                        $"which is panel point {point} - open board between the two rail columns.");
                }

                // The ordinary tray reserves the full width only for layout. Its empty right side is
                // visibly map, and therefore must remain map input too. This is the exact lower-screen
                // path through which the Blue heroes became unreachable when the cards moved left.
                var fullTray = ConfiguredMatchHudRenderer.CalculateCardPanelRect(panel.x, panel.y);
                var transparentTray = Rect.MinMaxRect(
                    ConfiguredMatchHudRenderer.CompactHandRightEdge,
                    fullTray.yMin,
                    blue.xMin,
                    fullTray.yMax);
                Assert.That(transparentTray.width, Is.GreaterThan(0f));
                foreach (var point in Interior(transparentTray))
                {
                    var screenPosition =
                        MatchHudPanelScaling.PanelPointToScreen(point, device.x, device.y);
                    Assert.That(
                        ConfiguredMatchHudRenderer.StandingRegionsBlockPointerAt(
                            screenPosition, device.x, device.y),
                        Is.False,
                        $"At {device} the transparent right side of the card layout band swallows " +
                        $"screen position {screenPosition}, panel point {point}.");
                }
            }
        }

        /// <summary>
        /// The panel declares one scale mode, one reference resolution and one match mode, in one
        /// place, and the committed asset carries the same four values.
        ///
        /// <para>Milestone 7.9 committed the asset so a built player would ship Unity's text data, and
        /// until this milestone the code then overrode its scale mode on the copy - so the asset's
        /// 1200x800 reference resolution was inert and the file on disk described a panel the game
        /// never built. Now <see cref="MatchHudPanelScaling"/> is the statement and the asset agrees
        /// with it, which is what this reads.</para>
        /// </summary>
        [Test]
        public void ThePanelSettingsAsset_CarriesTheDeclaredCoordinateSpace()
        {
            var asset = Resources.Load<PanelSettings>(
                ConfiguredMatchHudRenderer.RuntimePanelSettingsResourcePath);
            Assert.That(asset, Is.Not.Null,
                "The committed panel-settings asset is what a built player copies its panel from.");
            Assert.That(asset.scaleMode, Is.EqualTo(MatchHudPanelScaling.ScaleMode));
            Assert.That(asset.referenceResolution,
                Is.EqualTo(MatchHudPanelScaling.ReferenceResolution));
            Assert.That(asset.screenMatchMode, Is.EqualTo(MatchHudPanelScaling.ScreenMatchMode));
            Assert.That(asset.match, Is.EqualTo(MatchHudPanelScaling.Match).Within(0.0001f));

            // And the fallback path applies the same statement, so a build that cannot resolve the
            // asset lays out in the same space rather than reverting to device pixels.
            var bare = ScriptableObject.CreateInstance<PanelSettings>();
            try
            {
                MatchHudPanelScaling.Apply(bare);
                Assert.That(bare.scaleMode, Is.EqualTo(MatchHudPanelScaling.ScaleMode));
                Assert.That(bare.referenceResolution,
                    Is.EqualTo(MatchHudPanelScaling.ReferenceResolution));
                Assert.That(bare.screenMatchMode, Is.EqualTo(MatchHudPanelScaling.ScreenMatchMode));
                Assert.That(bare.match, Is.EqualTo(MatchHudPanelScaling.Match).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(bare);
            }
        }

        /// <summary>
        /// Two device resolutions either side of the reference produce the same layout in reference
        /// units - which is the whole claim: the HUD occupies the same share of every screen of the
        /// reference's shape rather than the same count of its pixels.
        /// </summary>
        [Test]
        public void EveryDeviceResolutionOfTheReferenceShape_LaysOutIdenticallyInReferenceUnits()
        {
            var expected = Regions(
                MatchHudPanelScaling.ReferenceWidth, MatchHudPanelScaling.ReferenceHeight);
            foreach (var device in ReferenceShapedResolutions)
            {
                var panel = MatchHudPanelScaling.ReferenceViewport(device.x, device.y);
                Assert.That(panel.x, Is.EqualTo(MatchHudPanelScaling.ReferenceWidth).Within(0.01f),
                    $"{device} is the reference's own shape, so its panel is the reference viewport.");
                Assert.That(panel.y, Is.EqualTo(MatchHudPanelScaling.ReferenceHeight).Within(0.01f));

                var actual = Regions(panel.x, panel.y);
                foreach (var name in expected.Keys)
                {
                    Assert.That(actual[name].x, Is.EqualTo(expected[name].x).Within(0.01f),
                        $"{name} moved at {device}.");
                    Assert.That(actual[name].y, Is.EqualTo(expected[name].y).Within(0.01f),
                        $"{name} moved at {device}.");
                    Assert.That(actual[name].width, Is.EqualTo(expected[name].width).Within(0.01f),
                        $"{name} changed width at {device}.");
                    Assert.That(actual[name].height, Is.EqualTo(expected[name].height).Within(0.01f),
                        $"{name} changed height at {device}.");
                }

                // One device pixel at the reference, and the screen's own count of them elsewhere.
                var pixels = MatchHudPanelScaling.ScreenPixelsPerPanelPoint(device.x, device.y);
                Assert.That(
                    ConfiguredMatchHudRenderer.HeroRailWidth * pixels,
                    Is.EqualTo(ConfiguredMatchHudRenderer.HeroRailWidth * device.x /
                        MatchHudPanelScaling.ReferenceWidth).Within(0.01f),
                    $"A rail takes the same share of {device} as it does of the reference screen.");
            }
        }

        /// <summary>
        /// The four numbers milestone 8.25c's block names, restated in reference units rather than
        /// re-measured: each still means what its own milestone made it mean, because the reference is
        /// the screen every one of them was measured on.
        /// </summary>
        [Test]
        public void TheStandingLayoutNumbers_KeepTheirOwnMilestonesMeaningInReferenceUnits()
        {
            var width = MatchHudPanelScaling.ReferenceWidth;
            var height = MatchHudPanelScaling.ReferenceHeight;
            var tray = ConfiguredMatchHudRenderer.CalculateCardPanelRect(width, height);
            var red = ConfiguredMatchHudRenderer.CalculateHeroRailRect(width, height, TeamSide.Red);
            var blue = ConfiguredMatchHudRenderer.CalculateHeroRailRect(width, height, TeamSide.Blue);

            // Milestone 8.25f2's readable footprint. One whole card face stands inside the reserved
            // bottom band, and rulebook p.6's five-card hand fits one row without horizontal travel.
            Assert.That(CardFootprint.Height, Is.LessThan(tray.height),
                "A whole card face stands inside the tray at the reference viewport.");
            Assert.That(
                ConfiguredMatchHudRenderer.CompactHandRightEdge,
                Is.LessThanOrEqualTo(tray.width),
                "and the overlapped five-card hand fits inside it without a horizontal scrollbar.");

            var playfield = ConfiguredMatchHudRenderer.CalculatePlayfieldRect(width, height);
            Assert.That(playfield.yMax, Is.LessThanOrEqualTo(tray.yMin),
                "The reserved card band starts only after the board playfield ends.");
            Assert.That(playfield.width,
                Is.EqualTo(playfield.height * ConfiguredMatchHudRenderer.BoardViewportAspect)
                    .Within(0.01f),
                "The map keeps its compact native framing instead of stretching across the whole " +
                "middle band.");
            Assert.That(playfield.xMax, Is.LessThanOrEqualTo(blue.xMin - 12f + 0.01f),
                "The square map is restored to the upper-right corner immediately before the Blue rail.");
            Assert.That(playfield.xMin, Is.GreaterThan(red.xMax + 12f),
                "The map leaves the former Director-side area open for round and hand status.");
            Assert.That(DirectorHandsUnityView.CardGap, Is.LessThanOrEqualTo(6f),
                "The readable hand uses only a tight gutter between adjacent cards.");
            Assert.That(tray.height,
                Is.EqualTo(CardFootprint.Height + 40f).Within(0.01f),
                "The card band spends 40 reference units on chrome; the hand's " +
                "status belongs in the open playfield-side dock rather than in a second tray row.");
            // Milestone 8.30b re-measured this floor rather than assuming it survived. The amendment
            // narration lane took 40 reference units off the top of the middle band, so the measured
            // board went from 1092x910 to 1044x870 - the trade the amendment makes, and one a capture
            // at 2560x1440 and at the short desktop viewport was taken to judge. The floor moves with
            // the measurement and stays a floor: a board that has lost another ten units to some later
            // strip fails here rather than shrinking quietly one milestone at a time.
            Assert.That(playfield.height, Is.GreaterThanOrEqualTo(860f),
                "At the reference viewport the separated hand must leave a materially sized board, " +
                "not merely a positive postage-stamp viewport.");

            // Milestone 8.23a's rail width, and the standing product rule that bounds it: both rails
            // together stay under half the panel, so the board remains the dominant surface. It is a
            // fact about reference units now, and therefore about every screen rather than about the
            // narrowest one that was ever measured.
            Assert.That(red.width, Is.EqualTo(ConfiguredMatchHudRenderer.HeroRailWidth));
            Assert.That(blue.width, Is.EqualTo(ConfiguredMatchHudRenderer.HeroRailWidth));
            foreach (var device in ReferenceShapedResolutions)
            {
                var panel = MatchHudPanelScaling.ReferenceViewport(device.x, device.y);
                Assert.That(
                    ConfiguredMatchHudRenderer.CalculateHeroRailRect(panel.x, panel.y, TeamSide.Red).width +
                    ConfiguredMatchHudRenderer.CalculateHeroRailRect(panel.x, panel.y, TeamSide.Blue).width,
                    Is.LessThan(panel.x / 2f),
                    $"Both rails together take less than half the panel at {device}.");
            }

            // Milestone 8.25f2's rail column: the edge under the status strip and above the reserved
            // card band. The measured three-rail stack needs about 733 reference units.
            Assert.That(red.height,
                Is.EqualTo(tray.yMin - (ConfiguredMatchHudRenderer.StatusBarHeight + 24f))
                    .Within(0.01f));
            Assert.That(red.height, Is.GreaterThan(733f),
                "Three measured rails still fit above the enlarged hand.");
            Assert.That(red.height, Is.EqualTo(blue.height).Within(0.01f));

            // The status strip is a reference unit too, and the tray clears it.
            Assert.That(tray.yMin,
                Is.GreaterThanOrEqualTo(ConfiguredMatchHudRenderer.StatusBarHeight));
        }

        /// <summary>
        /// Milestone 8.30b amendment geometry, measured at the reference panel rather than described.
        ///
        /// <para>The amendment moved two surfaces and said in as many words that the geometry
        /// milestones 8.25c and 8.25f2 pinned is <em>re-measured</em> rather than assumed by it. These
        /// are those measurements: what the widened strip cost, what it did not touch, and where End
        /// Turn corner landed. Every number here was read off the layout before it was written down.
        /// </para>
        /// </summary>
        [Test]
        public void TheAmendedStripAndEndTurnCorner_MeasureAsRecordedAtTheReferencePanel()
        {
            var width = MatchHudPanelScaling.ReferenceWidth;
            var height = MatchHudPanelScaling.ReferenceHeight;

            // The strip is the old bar plus the new lane, and the two are stated separately so the
            // chip below can be measured against the half it has always sat in.
            Assert.That(ConfiguredMatchHudRenderer.StatusBarIdentityRowHeight, Is.EqualTo(56f),
                "The identity row is exactly the strip milestone 8.25c measured.");
            Assert.That(ConfiguredMatchHudRenderer.StatusBarHeight,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarIdentityRowHeight +
                    ConfiguredMatchHudRenderer.StatusBarNarrationHeight));
            Assert.That(
                ConfiguredMatchHudRenderer.CalculateStatusBarRect(width, height),
                Is.EqualTo(new Rect(0f, 0f, 2560f, 96f)));

            // Milestone 8.30l centres a round Tie Breaker badge across the complete strip. The tray,
            // compact hand, Level Up card row and round dock remain placed from the panel bottom edge.
            Assert.That(
                ConfiguredMatchHudRenderer.CalculateTieBreakerIndicatorRect(width, height),
                Is.EqualTo(new Rect(1200f, -32f, 160f, 160f)),
                "The doubled Tie Breaker is centred on the compact strip and cropped by it.");
            Assert.That(
                ConfiguredMatchHudRenderer.CalculateCardPanelRect(width, height),
                Is.EqualTo(new Rect(0f, 968f, 2560f, 460f)));
            Assert.That(
                ConfiguredMatchHudRenderer.CalculateCompactHandPointerRect(width, height),
                Is.EqualTo(new Rect(0f, 968f, 732f, 460f)));
            Assert.That(
                ConfiguredMatchHudRenderer.CalculateUpgradeComparisonPanelRect(width, height),
                Is.EqualTo(new Rect(0f, 878f, 2560f, 550f)));
            Assert.That(
                ConfiguredMatchHudRenderer.CalculateStatusDockRect(width, height),
                Is.EqualTo(new Rect(12f, 838f, 720f, 128f)),
                "The round dock is placed from the playfield bottom edge, which the strip did not " +
                "move, so it kept its footprint exactly.");

            // What it did move, and by how much. The rail column and the board each lost the lane's
            // forty units off the top; the three-rail stack measured at 733 still clears by 115.
            var red = ConfiguredMatchHudRenderer.CalculateHeroRailRect(width, height, TeamSide.Red);
            var blue = ConfiguredMatchHudRenderer.CalculateHeroRailRect(width, height, TeamSide.Blue);
            Assert.That(red, Is.EqualTo(new Rect(12f, 108f, 264f, 848f)));
            Assert.That(blue, Is.EqualTo(new Rect(2284f, 108f, 264f, 848f)));
            Assert.That(red.height - 733f, Is.EqualTo(115f).Within(0.01f),
                "Rulebook p.6 three rails a side clear the compact strip.");
            Assert.That(
                ConfiguredMatchHudRenderer.CalculatePlayfieldRect(width, height),
                Is.EqualTo(new Rect(1228f, 96f, 1044f, 870f)),
                "The board remains directly below the compact clipped strip.");

            // End Turn replaces the Tie Breaker's former top-right slot and fills the status strip.
            var endTurn = ConfiguredMatchHudRenderer.CalculateEndTurnControlRect(width, height);
            Assert.That(endTurn, Is.EqualTo(new Rect(2284f, 0f, 264f, 96f)));
            Assert.That(endTurn.xMax, Is.EqualTo(width - 12f).Within(0.01f),
                "It is inset from the panel right edge by the standing margin and no more.");
            Assert.That(endTurn.yMin, Is.EqualTo(ConfiguredMatchHudRenderer.EndTurnControlTopInset));
            Assert.That(endTurn.yMax, Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarHeight));
            Assert.That(endTurn.xMin, Is.EqualTo(blue.xMin));
            Assert.That(endTurn.width, Is.EqualTo(blue.width),
                "End Turn aligns exactly with the Blue rail column beneath it.");
            Assert.That(endTurn.Overlaps(
                ConfiguredMatchHudRenderer.CalculateTieBreakerIndicatorRect(width, height)), Is.False);
        }

        /// <summary>
        /// Every region stays inside the panel at every device resolution, including shapes that are
        /// not the reference's - where the scale follows neither axis alone and the panel is a
        /// different rectangle from the reference one.
        /// </summary>
        [Test]
        public void EveryRegion_StaysInsideThePanelAtEveryDeviceResolution()
        {
            var resolutions = ReferenceShapedResolutions
                .Concat(new[]
                {
                    new Vector2(1280f, 800f),
                    new Vector2(1600f, 1200f),
                    new Vector2(3440f, 1440f),
                    new Vector2(778f, 300f)
                })
                .ToArray();

            foreach (var device in resolutions)
            {
                var panel = MatchHudPanelScaling.ReferenceViewport(device.x, device.y);
                Assert.That(panel.x, Is.GreaterThan(0f), $"{device} resolved no panel width.");
                Assert.That(panel.y, Is.GreaterThan(0f), $"{device} resolved no panel height.");

                foreach (var region in Regions(panel.x, panel.y))
                {
                    Assert.That(region.Value.xMin, Is.GreaterThanOrEqualTo(-0.01f),
                        $"{region.Key} at {device}");
                    Assert.That(region.Value.yMin, Is.GreaterThanOrEqualTo(-0.01f),
                        $"{region.Key} at {device}");
                    Assert.That(region.Value.xMax, Is.LessThanOrEqualTo(panel.x + 0.01f),
                        $"{region.Key} at {device}");
                    Assert.That(region.Value.yMax, Is.LessThanOrEqualTo(panel.y + 0.01f),
                        $"{region.Key} at {device}");
                }

                var red = ConfiguredMatchHudRenderer.CalculateHeroRailRect(panel.x, panel.y, TeamSide.Red);
                var blue = ConfiguredMatchHudRenderer.CalculateHeroRailRect(panel.x, panel.y, TeamSide.Blue);
                Assert.That(red.xMax, Is.LessThan(blue.xMin),
                    $"The two rails meet at {device}.");
                Assert.That(red.width + blue.width, Is.LessThan(panel.x / 2f),
                    $"Both rails take half the panel or more at {device}.");
            }
        }

        private static Dictionary<string, Rect> Regions(float panelWidth, float panelHeight) =>
            new Dictionary<string, Rect>
            {
                ["the status strip"] =
                    ConfiguredMatchHudRenderer.CalculateStatusBarRect(panelWidth, panelHeight),
                ["the card tray"] =
                    ConfiguredMatchHudRenderer.CalculateCardPanelRect(panelWidth, panelHeight),
                ["the board band"] =
                    ConfiguredMatchHudRenderer.CalculatePlayfieldRect(panelWidth, panelHeight),
                ["the Red rail column"] =
                    ConfiguredMatchHudRenderer.CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Red),
                ["the Blue rail column"] =
                    ConfiguredMatchHudRenderer.CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Blue)
            };

        /// <summary>
        /// A grid of points strictly inside a rectangle. A grid rather than the centre, because a
        /// centre point lands inside both the drawn region and the unconverted one for every region
        /// wider than the error, and a check that can pass on where its one sample fell is not
        /// evidence.
        /// </summary>
        private static IEnumerable<Vector2> Interior(Rect rect)
        {
            const int Steps = 8;
            for (var column = 1; column < Steps; column++)
            {
                for (var row = 1; row < Steps; row++)
                {
                    yield return new Vector2(
                        Mathf.Lerp(rect.xMin, rect.xMax, column / (float)Steps),
                        Mathf.Lerp(rect.yMin, rect.yMax, row / (float)Steps));
                }
            }
        }
    }
}
