using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class MinimalHexGridPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_LoadsCompleteSelectedMapPresentationUnderFramedFixedCamera()
        {
            yield return LoadScene();

            var tiles = Object.FindObjectsByType<HexTileView>();
            var markers = Object.FindObjectsByType<BoardMapSpawnMarkerView>();
            var cameras = Object.FindObjectsByType<Camera>();
            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            var minions = Object.FindObjectsByType<StandardMinionView>();
            var root = Root();

            Assert.That(root.Runtime.Setup.WaveCount, Is.EqualTo(3));
            Assert.That(root.Runtime.Setup.Teams,
                Has.All.Matches<QuickGameTeamSetupState>(team => team.LifeCount == 4));
            Assert.That(mapView.RenderedDefinition.Reference, Is.EqualTo(OfficialBoardMapCatalog.SelectedMap));
            Assert.That(tiles, Has.Length.EqualTo(292));
            Assert.That(tiles.Count(tile => tile.IsPlayable), Is.EqualTo(210));
            Assert.That(tiles.Count(tile => tile.SpaceKind == BoardMapSpaceKind.Terrain), Is.EqualTo(82));
            Assert.That(markers, Has.Length.EqualTo(40));
            Assert.That(minions, Has.Length.EqualTo(12));
            Assert.That(minions, Has.All.Matches<StandardMinionView>(view =>
                view.GetComponent<SpriteRenderer>().sprite != null));
            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].orthographic, Is.True);
            Assert.That(tiles.Where(tile => tile.IsPlayable).All(tile =>
            {
                var viewport = cameras[0].WorldToViewportPoint(tile.transform.position);
                return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            }), Is.True,
                "Every playable hex remains on screen; decorative terrain and artwork may extend " +
                "behind the full-screen HUD.");
        }

        /// <summary>
        /// Milestone 8.4. The scene a player loads deals verified content, which is the whole point of
        /// the milestone and which only a loaded scene can show: every Editor tier composes its own
        /// configuration, so none of them can say what the saved scene's composition root builds.
        ///
        /// It also holds the one-shot property the showcase request depends on. The fixtures above ask
        /// for the mechanics-showcase hands and never clear the request, because the composition clears
        /// it; if it were a mode that stayed on, this case would find showcase cards here.
        ///
        /// <para>Milestone 8.20 made the two seats two heroes. What the case says is unchanged - the
        /// scene deals verified content and nothing synthetic - and what it can no longer say is that
        /// one revision covers both, because a hero's set moves as its own revision.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DealsTheVerifiedStagedHeroesAndKeepsDealingThemAfterAShowcaseRequest()
        {
            yield return LoadShowcaseScene();
            Assert.That(
                Root().Runtime.Cards.Any(card =>
                    card.DefinitionReference.DefinitionId.Value == "placeholder-red-pursuit"),
                Is.True,
                "The requested composition is the showcase one, or the reset below proves nothing.");

            yield return LoadScene();
            var runtime = Root().Runtime;

            Assert.That(runtime.Cards, Has.Count.EqualTo(36),
                "Two eighteen-card decks, which is the smoke gate's pinned cards=36 marker.");
            Assert.That(
                runtime.Definitions.All(definition =>
                    definition.ContentAuthority == CardContentAuthority.VerifiedOfficial),
                Is.True,
                "and every one of them is source-verified official content.");
            Assert.That(
                runtime.Definitions.Select(definition => definition.ContentRevision).Distinct()
                    .OrderBy(value => value, System.StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    ArienCardContent.ContentRevision, WaspCardContent.ContentRevision
                }),
                "at the two revisions the two dealt heroes state.");
            Assert.That(
                runtime.Cards
                    .Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId)
                    .Distinct()
                    .ToArray(),
                Is.EquivalentTo(
                    WaspCardContent.StartingHand.Concat(ArienCardContent.StartingHand).ToArray()),
                "and each seat holds rulebook p.6's starting five off its own printed set.");
        }

        [UnityTest]
        public IEnumerator Scene_RendersHeaderAndBlocksBoardPointerBehindConfiguredHud()
        {
            yield return LoadScene();
            var root = Root();
            var header = root.Hud.Header;
            var side = root.Runtime.TieBreakerState.RevealedSide;
            var indicator = root.Hud.TieBreakerIndicatorRect;

            Assert.That(header.TieBreaker, Is.EqualTo(root.Runtime.Setup.TieBreakerState));
            Assert.That(header.Text, Does.Contain(side == TeamSide.Red ? "[R] RED" : "[B] BLUE"));
            Assert.That(header.Text, Does.Contain("WAVES 3"));
            Assert.That(header.Text, Does.Contain("LIVES  [R] 4  [B] 4"));
            // Milestone 8.25c: every rectangle the HUD reports is in the panel's reference units, so
            // the margin is measured off the panel's own width and a point on it is turned into the
            // screen pixel a player would click by the same declaration the boundary converts with.
            var panel = ConfiguredMatchHudRenderer.PanelViewport;
            Assert.That(indicator.center.x, Is.EqualTo(panel.x / 2f).Within(0.01f));
            Assert.That(indicator.center.y,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarHeight / 2f).Within(0.01f));
            Assert.That(indicator.width, Is.EqualTo(indicator.height).Within(0.01f));
            Assert.That(
                root.Hud.BlocksPointerAt(
                    MatchHudPanelScaling.PanelPointToScreen(
                        indicator.center, Screen.width, Screen.height)),
                Is.True);
        }

        [UnityTest]
        public IEnumerator Scene_KeepsConfiguredCardPanelInsideRuntimeViewport()
        {
            yield return LoadScene();

            // Milestone 8.25c: the tray's rectangle and the viewport it is bounded by are both in the
            // panel's reference units. The screen's pixel count decides how large a reference unit is
            // drawn and nothing else about this arithmetic.
            var viewport = ConfiguredMatchHudRenderer.PanelViewport;
            var panel = Root().Hud.CardPanelRect;
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(viewport.x));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(viewport.y));
            Assert.That(panel.width,
                Is.EqualTo(Mathf.Max(0f, viewport.x)).Within(0.01f));
            // Milestone 8.25f2: the ordinary tray reserves exactly one readable-card band, capped only
            // when a deliberately short panel has less room beneath the status strip.
            Assert.That(panel.height,
                Is.EqualTo(Mathf.Min(
                    ConfiguredMatchHudRenderer.CardPanelHeight,
                    Mathf.Max(0f, viewport.y - 24f - ConfiguredMatchHudRenderer.StatusBarHeight)))
                    .Within(0.01f));
            Assert.That(panel.yMin,
                Is.GreaterThanOrEqualTo(ConfiguredMatchHudRenderer.StatusBarHeight));
        }

        /// <summary>
        /// Milestone 8.25c's targeted Game View evidence: the six-player HUD as a player sees it, at
        /// the reference resolution and at the 2560-wide screen that prompted the milestone.
        ///
        /// <para>Two captures rather than one, because the whole subject is whether the HUD is the same
        /// share of both. It no-ops in batch, like every other capture in this suite.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_CapturesTheSixPlayerHudAtTwoResolutionsForGameViewInspection()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                Assert.That(UnityEngine.Application.isBatchMode, Is.True,
                    "The capture branch is exercised by the dedicated nonbatch recipe.");
                yield break;
            }

            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            UiPlayer.SetPlayerViewport(1920, 1080, "8.25c reference-resolution capture");
            yield return LoadOfficialSixPlayerScene();
            yield return null;
            yield return null;
            var root = Root();
            root.Runtime.RefreshAll();
            yield return null;
            yield return null;
            TestContext.WriteLine(
                "8.25c capture at " + Screen.width + "x" + Screen.height + ": panel " +
                    ConfiguredMatchHudRenderer.PanelViewport + ", tray " + root.Hud.CardPanelRect +
                    ", red column " + root.Hud.RedHeroRailRect + ", red column drawn " +
                    Named(root.Hud.RootElement, "gd-hero-rail-red").worldBound + ".");
            yield return Capture("tmp/milestone-8-25c-six-player-1920x1080.png");

            UiPlayer.SetPlayerViewport(2560, 1440, "8.25c wide-screen capture");
            yield return null;
            root.Runtime.RefreshAll();
            yield return null;
            yield return null;
            TestContext.WriteLine(
                "8.25c capture at " + Screen.width + "x" + Screen.height + ": panel " +
                    ConfiguredMatchHudRenderer.PanelViewport + ", tray " + root.Hud.CardPanelRect +
                    ", red column " + root.Hud.RedHeroRailRect + ", red column drawn " +
                    Named(root.Hud.RootElement, "gd-hero-rail-red").worldBound + ".");
            yield return Capture("tmp/milestone-8-25c-six-player-2560x1440.png");

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.25c restore the viewport this session was running at");
            yield return null;
        }

        /// <summary>
        /// Milestone 8.25c. The live panel resolves the scale <see cref="MatchHudPanelScaling"/>
        /// predicts, at a viewport that is not the reference and is not even the reference's shape.
        ///
        /// <para><b>Why this case exists.</b> Every rectangle in the HUD is laid out into a viewport
        /// this project computes for itself, and the pointer boundary converts a screen pixel with the
        /// same arithmetic - both because an Edit Mode fixture has no panel to ask. That arithmetic is
        /// a reproduction of Unity's own, so it has to be measured against Unity's own or it is a
        /// belief. 1600x1000 is chosen because it is neither the reference resolution nor 16:9: at any
        /// 16:9 viewport the prediction is exactly the reference resolution whatever the formula, so a
        /// wrong one would pass unnoticed.</para>
        ///
        /// <para>The measurements are written out before they are judged, so a formula that turns out
        /// to be the wrong one reports the numbers it should have produced rather than only that it
        /// did not.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_ResolvesTheDeclaredPanelScaleOnTheLiveRuntimePanel()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            UiPlayer.SetPlayerViewport(1600, 1000, "8.25c off-reference panel scale measurement");
            yield return LoadScene();
            yield return null;
            yield return null;

            var hud = Root().Hud;
            Assert.That(hud.RootElement?.panel, Is.Not.Null, "There is no live panel to measure.");

            var deviceWidth = (float)Screen.width;
            var deviceHeight = (float)Screen.height;
            var predicted = MatchHudPanelScaling.ReferenceViewport(deviceWidth, deviceHeight);
            var measured = hud.RootElement.worldBound;
            // The probe is a pointer position as `PrimitiveHeroPointerInput` reads one: device pixels
            // with the origin at the screen's BOTTOM-left, which is where the Input System puts it.
            //
            // `RuntimePanelUtils.ScreenToPanel` scales but does not flip - measured here rather than
            // assumed, and the reason this comparison hands it the flipped position. Unity's answer is
            // therefore the oracle for the scale, and the flip is this project's own, owed to the
            // origin the pointer arrives in.
            var probe = new Vector2(deviceWidth * 0.37f, deviceHeight * 0.61f);
            var predictedPoint = MatchHudPanelScaling.ScreenToPanelPoint(
                probe, deviceWidth, deviceHeight);
            var measuredPoint = UnityEngine.UIElements.RuntimePanelUtils.ScreenToPanel(
                hud.RootElement.panel, new Vector2(probe.x, deviceHeight - probe.y));

            TestContext.WriteLine(
                "8.25c panel scale at " + deviceWidth + "x" + deviceHeight + ": predicted panel " +
                    predicted + " and measured " + measured.size + "; screen " + probe +
                    " predicted as panel point " + predictedPoint + " and measured as " +
                    measuredPoint + ".");

            var restored = false;
            try
            {
                Assert.That(
                    MatchHudPanelScaling.PanelPointsPerScreenPixel(deviceWidth, deviceHeight),
                    Is.Not.EqualTo(1f).Within(0.001f),
                    "The measurement is only worth taking where the scale is not 1.");
                Assert.That(measured.width, Is.EqualTo(predicted.x).Within(1f),
                    "The live panel is a different width from the one every layout number is written " +
                    "into, so the reproduced scale resolution is not Unity's.");
                Assert.That(measured.height, Is.EqualTo(predicted.y).Within(1f));
                Assert.That(measuredPoint.x, Is.EqualTo(predictedPoint.x).Within(0.5f),
                    "The pointer boundary scales a screen pixel differently from the panel it is " +
                    "asking about.");
                Assert.That(measuredPoint.y, Is.EqualTo(predictedPoint.y).Within(0.5f),
                    "and the flip that turns a bottom-left pointer position into a top-left panel " +
                    "one no longer lands where the panel's own conversion does.");

                // And the boundary agrees with the physical hand's actual painted footprint. The
                // tray is a full-width layout reservation, but its transparent right side belongs to
                // the board so lower Blue heroes remain clickable.
                var tray = Named(hud.RootElement, "gd-tray-region").worldBound;
                var compactHand = ConfiguredMatchHudRenderer.CalculateCompactHandPointerRect(
                    predicted.x, predicted.y);
                Assert.That(tray.Contains(compactHand.center), Is.True,
                    "The compact hand must remain inside the live tray reservation.");
                Assert.That(
                    hud.BlocksPointerAt(
                        MatchHudPanelScaling.PanelPointToScreen(
                            compactHand.center, deviceWidth, deviceHeight)),
                    Is.True,
                    "The HUD does not consume the pointer where it draws the compact hand.");
                var transparentTrayPoint = new Vector2(
                    (compactHand.xMax + tray.xMax) * 0.5f,
                    tray.center.y);
                Assert.That(
                    hud.BlocksPointerAt(
                        MatchHudPanelScaling.PanelPointToScreen(
                            transparentTrayPoint, deviceWidth, deviceHeight)),
                    Is.False,
                    "The transparent part of the tray reservation must leave the board clickable.");
            }
            finally
            {
                UiPlayer.SetPlayerViewport(
                    restoreWidth, restoreHeight, "8.25c restore the viewport this session was running at");
                restored = true;
            }

            Assert.That(restored, Is.True);
            yield return null;
        }

        /// <summary>
        /// Milestone 7.8a: the HUD is one live UI Toolkit panel in the running scene, the two hero rails
        /// and the tray are on it, and the pointer is blocked over each of them.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_BuildsTheRuntimeHudPanelWithBothHeroRailsAndBlocksThePointerOverThem()
        {
            yield return LoadScene();
            var root = Root();
            var hud = root.Hud;

            Assert.That(hud.RootElement, Is.Not.Null, "The configured HUD builds a runtime panel.");
            Assert.That(hud.RootElement.panel, Is.Not.Null);
            Assert.That(Named(hud.RootElement, "gd-topbar"), Is.Not.Null);
            Assert.That(Named(hud.RootElement, "gd-tray-region"), Is.Not.Null);
            Assert.That(Named(hud.RootElement, "gd-director-toggle"), Is.Not.Null);

            var rails = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Has.Length.EqualTo(root.Runtime.Setup.Heroes.Count),
                "One overview per projected hero, drawn as a repeated component.");

            var compactHand = ConfiguredMatchHudRenderer.CalculateCompactHandPointerRect(
                ConfiguredMatchHudRenderer.PanelViewport.x,
                ConfiguredMatchHudRenderer.PanelViewport.y);
            foreach (var rect in new[] { compactHand, hud.TieBreakerIndicatorRect })
            {
                Assert.That(
                    hud.BlocksPointerAt(
                        MatchHudPanelScaling.PanelPointToScreen(
                            rect.center, Screen.width, Screen.height)),
                    Is.True,
                    $"The HUD consumes the pointer over {rect}.");
            }

            var openTrayPoint = new Vector2(
                (compactHand.xMax + hud.CardPanelRect.xMax) * 0.5f,
                hud.CardPanelRect.center.y);
            Assert.That(
                hud.BlocksPointerAt(
                    MatchHudPanelScaling.PanelPointToScreen(
                        openTrayPoint, Screen.width, Screen.height)),
                Is.False,
                "The tray's unpainted right side remains available to board clicks.");

            Assert.That(hud.IsDirectorDrawerOpen, Is.False);
            var acceptedBefore = root.Runtime.AcceptedCommands.Count;
            var roundBefore = root.Runtime.RoundIndex;
            root.Runtime.ToggleDirectorDrawer();
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);
            Assert.That(root.Runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBefore),
                "Opening the drawer submits no command.");
            Assert.That(root.Runtime.RoundIndex, Is.EqualTo(roundBefore));
            root.Runtime.ToggleDirectorDrawer();
            Assert.That(hud.IsDirectorDrawerOpen, Is.False);
        }

        /// <summary>
        /// Milestone 8.25: the scene a player loads composes rulebook p.5's six seats, and the HUD
        /// draws all six of them.
        ///
        /// <para>Milestone 8.25f2 makes the tray a reserved full-width bottom band. What this asks is
        /// that the remaining edge columns still hold all six rails: three per side, every one inside
        /// its own rectangle, and both columns ending above the tray.</para>
        ///
        /// <para>It reads the live element bounds rather than the deterministic rects, because a rail's
        /// height follows its content and the question is whether the content fits.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsAllSixOfficialRailsInsideTheirOwnColumnsWithTheTrayBetweenThem()
        {
            // The subject is a measurement, so the viewport is stated rather than inherited: a leaked
            // short one from an earlier journey would measure a different arrangement, and 1920x1080
            // is the reference this milestone's numbers are recorded at.
            UiPlayer.SetPlayerViewport(1920, 1080, "8.25 six-rail measurement");
            yield return LoadOfficialSixPlayerScene();
            yield return null;
            yield return null;
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            Assert.That(runtime.Setup.Heroes, Has.Count.EqualTo(6),
                "p.5's official setup places three heroes a side.");
            Assert.That(
                runtime.Setup.Heroes.Count(hero => hero.TeamSide == TeamSide.Red), Is.EqualTo(3));
            Assert.That(
                runtime.Setup.Heroes.Count(hero => hero.TeamSide == TeamSide.Blue), Is.EqualTo(3));

            var rails = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Has.Length.EqualTo(6), "One overview per projected hero.");

            var columns = new[]
            {
                (Name: "gd-hero-rail-red", Rect: hud.RedHeroRailRect),
                (Name: "gd-hero-rail-blue", Rect: hud.BlueHeroRailRect)
            };
            foreach (var column in columns)
            {
                var element = Named(hud.RootElement, column.Name);
                Assert.That(element, Is.Not.Null, $"The {column.Name} column is on the panel.");
                var railsInColumn = Descendants(element)
                    .Where(candidate => candidate.name == "gd-hero-rail")
                    .ToArray();
                Assert.That(railsInColumn, Has.Length.EqualTo(3),
                    "Three heroes a side means three rails in each column.");

                var bounds = element.worldBound;
                Assert.That(bounds.height, Is.GreaterThan(0f), "The column resolved a real rectangle.");
                Assert.That(bounds.height, Is.LessThanOrEqualTo(column.Rect.height + 0.5f),
                    $"Three rails stack inside the column the renderer reserved: drew {bounds.height} " +
                    $"into {column.Rect.height} at {Screen.width}x{Screen.height}.");
                Assert.That(
                    bounds.yMax,
                    Is.LessThanOrEqualTo(ConfiguredMatchHudRenderer.PanelViewport.y + 0.5f),
                    "and the last rail is on screen rather than under its bottom edge.");
                foreach (var rail in railsInColumn)
                {
                    Assert.That(rail.worldBound.width, Is.LessThanOrEqualTo(column.Rect.width + 0.5f));
                    Assert.That(rail.worldBound.xMin, Is.GreaterThanOrEqualTo(bounds.xMin - 0.5f),
                        "A plaque may not protrude past the outside of its assigned side column.");
                    Assert.That(rail.worldBound.xMax, Is.LessThanOrEqualTo(bounds.xMax + 0.5f),
                        "A plaque may not protrude into the board from its assigned side column.");
                    foreach (var readable in Descendants(rail).Where(element =>
                        element.visible && element.resolvedStyle.display != DisplayStyle.None))
                    {
                        Assert.That(readable.worldBound.xMin,
                            Is.GreaterThanOrEqualTo(rail.worldBound.xMin - 0.5f),
                            readable.name + " protrudes through the plaque's outside edge.");
                        Assert.That(readable.worldBound.xMax,
                            Is.LessThanOrEqualTo(rail.worldBound.xMax + 0.5f),
                            readable.name + " protrudes through the plaque's board-side edge.");
                    }
                }
            }

            // A rail the HUD draws is a rail a player can read. The reserved column itself now ends at
            // the tray, so the geometry and every readable row agree without relying on z-order.
            var tray = hud.CardPanelRect;
            var redColumn = Named(hud.RootElement, "gd-hero-rail-red").worldBound;
            var referencePanel = ConfiguredMatchHudRenderer.PanelViewport;
            TestContext.WriteLine(
                "8.25c layout: the panel is " + referencePanel.x + "x" + referencePanel.y +
                    " reference units at " + Screen.width + "x" + Screen.height + " device pixels " +
                    "(scale " +
                    MatchHudPanelScaling.ScreenPixelsPerPanelPoint(Screen.width, Screen.height) +
                    " pixels per unit), three rails draw " + redColumn.height + " into the " +
                    hud.RedHeroRailRect.height + "-unit column, and the tray is " + tray + ".");

            // Every row and the declared rail column are clear of the tray as drawn.
            var drawnTray = Named(hud.RootElement, "gd-tray-region").worldBound;
            Assert.That(redColumn.yMax, Is.LessThanOrEqualTo(drawnTray.yMin + 0.5f),
                "The rail column ends before the reserved card band.");
            var readableRows = new[]
            {
                "gd-hero-icon", "gd-bonus-strip", "gd-item-chips", "gd-hero-coins",
                "gd-current-card-slot", "gd-turn-slots", "gd-discard", "gd-hero-marker", "gd-ultimate"
            };
            foreach (var rail in Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail"))
            {
                Assert.That(rail.worldBound.yMax, Is.LessThanOrEqualTo(referencePanel.y + 0.5f),
                    "A rail that runs off the bottom of the panel is not drawn: " + rail.worldBound);
                foreach (var row in Descendants(rail)
                    .Where(element => readableRows.Contains(element.name)))
                {
                    Assert.That(row.worldBound.Overlaps(drawnTray), Is.False,
                        "A row a player cannot read is a row that is not drawn: " + row.name + " at " +
                            row.worldBound + " is under the tray at " + drawnTray + ".");
                }
            }

            // The rails still answer the pointer over their whole column, which is what stops a click
            // meant for a rail reaching the board behind it.
            foreach (var column in columns)
            {
                var element = Named(hud.RootElement, column.Name);
                foreach (var probe in new[]
                {
                    element.worldBound.yMin + 4f,
                    element.worldBound.center.y,
                    element.worldBound.yMax - 4f
                })
                {
                    var point = new Vector2(element.worldBound.center.x, probe);
                    Assert.That(
                        hud.BlocksPointerAt(
                            MatchHudPanelScaling.PanelPointToScreen(
                                point, Screen.width, Screen.height)),
                        Is.True,
                        $"The HUD consumes the pointer over {column.Name} at {point}.");
                }
            }
        }

        /// <summary>
        /// Milestone 8.30i1. Every configured board piece and overview rail consumes the same one of
        /// twelve optional prepared faces. The test stays valid in a clean checkout: then both sets
        /// are empty and all six board pieces retain the generated-prism fallback.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_UsesMatchingPreparedWhiteFieldFacesOnAllSixBoardPiecesAndHeroRails()
        {
            UiPlayer.SetPlayerViewport(2560, 1440, "8.30i1 prepared hero tokens");
            yield return LoadOfficialSixPlayerScene();
            yield return null;
            yield return null;

            var root = Root();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroes, Has.Length.EqualTo(6));
            Assert.That(heroes.Select(hero => hero.HasTokenVisual).Distinct().Count(), Is.EqualTo(1),
                "The ignored roster is present as one complete set or absent as one complete set.");

            var boardTextures = heroes
                .Where(hero => hero.HasTokenVisual)
                .Select(hero => hero.GetComponent<MeshRenderer>().sharedMaterials[1].mainTexture)
                .ToArray();
            var railTextures = Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-hero-icon")
                .Select(element => element.style.backgroundImage.value.texture)
                .Where(texture => texture != null)
                .ToArray();
            Assert.That(boardTextures, Has.Length.EqualTo(railTextures.Length));
            Assert.That(boardTextures, Is.EquivalentTo(railTextures),
                "Board pieces and hero rails must consume the same prepared resources, not copies " +
                "or presentation-side recolours.");
            if (boardTextures.Length > 0)
            {
                Assert.That(boardTextures, Has.Length.EqualTo(6));
                Assert.That(boardTextures.Distinct().Count(), Is.EqualTo(6));
            }

            yield return Capture("tmp/8-30i1-white-hero-tokens-2560x1440.png");
            UiPlayer.SetPlayerViewport(2048, 1152, "8.30i1 prepared hero tokens alternate viewport");
            yield return null;
            yield return null;
            yield return Capture("tmp/8-30i1-white-hero-tokens-2048x1152.png");
        }

        /// <summary>
        /// Milestone 7.8a's visible Game View sweep: the standing HUD, the HUD with the `DIRECTOR`
        /// drawer open, and the contextual region drawing a paused leveling prompt.
        ///
        /// The prompt is rendered into the scene's own view from a presentation-shaped model, which is
        /// how milestone 6.1a's removal prompt and milestone 5.8b's Defense preview are captured here
        /// too: the configured symmetric setup reaches neither state in one round, and what a capture is
        /// evidence for is contrast, overlap, and clipping rather than authority. Whether the options are
        /// the authority's own is proved in <c>MatchHudPresentationTests</c> against a live pause.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_CapturesTheStandingHudAndOneContextualStateForGameViewInspection()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            yield return null;

            yield return Capture("tmp/milestone-7-8a-standing-hud.png");

            runtime.ToggleDirectorDrawer();
            yield return null;
            yield return Capture("tmp/milestone-7-8a-director-drawer.png");
            runtime.ToggleDirectorDrawer();
            yield return null;

            var current = runtime.RoundView.Model;
            var upgrade = new SystemTransitionPromptViewModel(
                GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind.LevelUpUpgrade,
                "LEVEL UP",
                "RED WASP REACHED LEVEL 2 - UPGRADE A CARD",
                "PAID 1 COIN - 1 REMAINING",
                // Milestone 8.8a: the six choices the configured Wasp hand actually produces, in the
                // order the pause produces them - the Blue pair, the Red pair, the Green pair, each one
                // upgraded card with p.15's two candidates. The transparent card row reorders those into
                // Blue, Green, Red while leaving the candidates inside each pair untouched.
                UpgradeOptions(
                        WaspCardContent.LiftUp,
                        WaspCardContent.ControlGravity,
                        WaspCardContent.KineticRepulse)
                    .Concat(UpgradeOptions(
                        WaspCardContent.Shock,
                        WaspCardContent.Electrocute,
                        WaspCardContent.ChargedBoomerang))
                    .Concat(UpgradeOptions(
                        WaspCardContent.StopProjectiles,
                        WaspCardContent.DeflectProjectiles,
                        WaspCardContent.Telekinesis)));
            runtime.RoundView.Render(new RoundActionViewModel(
                current.Version + 1,
                current.RoundIndex,
                current.TurnIndex,
                current.Phase,
                "End of round - rulebook p.15 is waiting on an upgrade.",
                System.Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                System.Array.Empty<TypedInvocationViewModel>(),
                System.Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                upgrade));
            root.Hud.ApplyExclusiveContext();
            yield return null;
            Assert.That(runtime.RoundView.HasExclusiveDecision, Is.True,
                "A paused transition is a decision the tray steps aside for.");
            yield return Capture("tmp/milestone-7-8a-leveling-prompt.png");
        }

        [UnityTest]
        public IEnumerator ChangingTheViewedHero_ChangesOnlyItsReservedOutlineWithoutMovingAnyPlaque()
        {
            yield return LoadOfficialSixPlayerScene();
            var runtime = Root().Runtime;
            var hud = Root().Hud;
            yield return null;
            yield return null;

            var before = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToDictionary(
                    rail => Descendants(rail).OfType<UnityEngine.UIElements.Label>()
                        .Single(label => label.name == "gd-hero-name").text,
                    rail => rail.worldBound);
            Assert.That(Descendants(hud.RootElement)
                    .Where(element => element.name == "gd-hero-rail")
                    .Select(rail => rail.style.borderTopWidth.value),
                Has.All.EqualTo(2f).Within(0.01f),
                "Every plaque permanently reserves the selected outline width.");
            var previouslyViewed = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.IsViewed).HeroId;
            var newlyViewed = runtime.HeroOverviewsView.Model.Heroes
                .First(hero => !hero.HeroId.Equals(previouslyViewed)).HeroId;

            Assert.That(runtime.SelectViewedHero(newlyViewed), Is.True);
            yield return null;
            yield return null;

            var afterRails = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(afterRails, Has.Length.EqualTo(6));
            Assert.That(afterRails.Select(rail => rail.style.borderTopWidth.value),
                Has.All.EqualTo(2f).Within(0.01f));
            foreach (var rail in afterRails)
            {
                var name = Descendants(rail).OfType<UnityEngine.UIElements.Label>()
                    .Single(label => label.name == "gd-hero-name").text;
                Assert.That(rail.worldBound, Is.EqualTo(before[name]),
                    $"Changing the viewed hero must not move or resize {name}'s plaque.");
            }

            Assert.That(afterRails.Count(rail =>
                    rail.resolvedStyle.borderTopColor == MatchHudTheme.SelectedOutline),
                Is.EqualTo(1), "Exactly the newly viewed plaque receives the reserved outline colour.");
        }

        [UnityTest]
        public IEnumerator Scene_ProgressedHeroPlaquesUsePrintedItemSymbolsAtBothValidatedViewports()
        {
            MatchSceneCompositionRoot.NextComposedCardContent =
                SceneCardContent.VerifiedTierTwoEngagedMirror;
            yield return LoadComposedScene();
            var root = Root();
            RenderAllChannelItemPlaque(root.Runtime);
            var sizes = new[] { new Vector2Int(2560, 1440), new Vector2Int(2048, 1152) };

            foreach (var size in sizes)
            {
                UiPlayer.SetPlayerViewport(
                    (uint)size.x, (uint)size.y, "8.30k printed item symbols");
                root.Hud.Layout();
                yield return null;
                yield return null;

                var projectedBonuses = root.Runtime.HeroOverviewsView.Model.Heroes
                    .SelectMany(hero => hero.ItemBonuses)
                    .ToArray();
                Assert.That(projectedBonuses, Is.Not.Empty,
                    "The progressed staging must carry real p.16 item bonuses.");
                var itemStrips = Descendants(root.Hud.RootElement)
                    .Where(element => element.name == "gd-item-chips")
                    .ToArray();
                var symbols = itemStrips.SelectMany(Descendants)
                    .Where(element => element.name == "gd-printed-channel-symbol")
                    .ToArray();
                Assert.That(symbols, Has.Length.EqualTo(projectedBonuses.Length));
                Assert.That(symbols.Select(symbol => symbol.viewDataKey),
                    Is.EquivalentTo(projectedBonuses.Select(bonus => bonus.Symbol.ResourceId)),
                    "Every progressed plaque consumes its typed catalog symbol.");
                Assert.That(symbols.Select(symbol => symbol.worldBound.height).Distinct().Count(),
                    Is.EqualTo(1), "Every channel symbol retains one visual height.");

                var populatedStrip = itemStrips.Single(strip => Descendants(strip)
                    .Count(element => element.name == "gd-printed-channel-symbol") ==
                    projectedBonuses.Length);
                var bonusStrip = populatedStrip.parent;
                var coin = Descendants(bonusStrip)
                    .Single(element => element.name == "gd-hero-coins");
                Assert.That(
                    populatedStrip.worldBound.xMax <= coin.worldBound.xMin + 0.5f ||
                    coin.worldBound.xMax <= populatedStrip.worldBound.xMin + 0.5f,
                    Is.True,
                    $"Multiple item channels and the coin stay separate at {size.x}x{size.y}.");

                foreach (var chip in itemStrips.SelectMany(Descendants)
                    .Where(element => element.name == "gd-value-chip"))
                {
                    var symbol = Descendants(chip)
                        .Single(element => element.name == "gd-printed-channel-symbol");
                    var amount = Descendants(chip).OfType<Label>().Single();
                    Assert.That(symbol.worldBound.xMin,
                        Is.GreaterThanOrEqualTo(chip.worldBound.xMin - 0.5f));
                    Assert.That(amount.worldBound.xMax,
                        Is.LessThanOrEqualTo(chip.worldBound.xMax + 0.5f));
                    Assert.That(symbol.worldBound.yMin,
                        Is.GreaterThanOrEqualTo(chip.worldBound.yMin - 0.5f));
                    Assert.That(symbol.worldBound.yMax,
                        Is.LessThanOrEqualTo(chip.worldBound.yMax + 0.5f),
                        $"The symbol and signed value stay inside their plaque chip at {size.x}x{size.y}.");
                    Assert.That(new[] { "INI", "ATK", "DEF", "MOV", "RNG", "RAD" },
                        Does.Not.Contain(amount.text));
                    Assert.That(chip.tooltip, Does.Contain(amount.text),
                        "The full assistive reading retains the signed value.");
                }
            }
        }

        [UnityTest]
        public IEnumerator Scene_LevelUpUsesOneContainedCardRowWithoutMovingTheStandingHud()
        {
            yield return LoadOfficialSixPlayerScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            yield return null;
            yield return null;

            var standingModel = runtime.RoundView.Model;
            var redBefore = Named(hud.RootElement, "gd-hero-rail-red").worldBound;
            var blueBefore = Named(hud.RootElement, "gd-hero-rail-blue").worldBound;
            var plaquesBefore = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .Select(element => element.worldBound)
                .ToArray();
            var safeBefore = hud.CameraSafeLayout;

            RenderArtworkUpgradeChoices(runtime);
            hud.ApplyExclusiveContext();
            yield return null;
            yield return null;

            Assert.That(Named(hud.RootElement, "gd-hero-rail-red").worldBound,
                Is.EqualTo(redBefore));
            Assert.That(Named(hud.RootElement, "gd-hero-rail-blue").worldBound,
                Is.EqualTo(blueBefore));
            Assert.That(Descendants(hud.RootElement)
                    .Where(element => element.name == "gd-hero-rail")
                    .Select(element => element.worldBound),
                Is.EqualTo(plaquesBefore), "Opening Level Up relocates no hero plaque.");
            Assert.That(hud.CameraSafeLayout.UpperRegion, Is.EqualTo(safeBefore.UpperRegion));
            Assert.That(hud.CameraSafeLayout.LowerRightRegion,
                Is.EqualTo(safeBefore.LowerRightRegion),
                "The board keeps its standing safe surface instead of reframing for Level Up.");

            var tray = Named(hud.RootElement, "gd-tray-region").worldBound;
            var trayElement = Named(hud.RootElement, "gd-tray-region");
            Assert.That(trayElement.resolvedStyle.backgroundColor.a,
                Is.EqualTo(0f).Within(0.001f),
                "Upgrade reserves layout but paints no full-width comparison band.");
            Assert.That(trayElement.resolvedStyle.borderTopWidth,
                Is.EqualTo(0f).Within(UiPlayer.PixelSnapping()),
                "The six cards have no enclosing tray outline.");
            var band = Named(hud.RootElement, "gd-upgrade-comparison-band");
            var pairs = Descendants(band)
                .Where(element => element.name == "gd-upgrade-pair")
                .ToArray();
            var cards = Descendants(band)
                .Where(element => element.name == "gd-card-preview")
                .ToArray();
            Assert.That(pairs, Has.Length.EqualTo(3));
            Assert.That(cards, Has.Length.EqualTo(6));
            Assert.That(cards.Select(card => Mathf.Round(card.worldBound.yMin)).Distinct().Count(),
                Is.EqualTo(1), "All six complete faces share one comparison row.");
            Assert.That(pairs.All(pair => pair.worldBound.xMin >= tray.xMin - 0.5f &&
                    pair.worldBound.xMax <= tray.xMax + 0.5f),
                Is.True, "Every Blue, Green, and Red pair stays inside the full-width band.");
            foreach (var option in Descendants(band)
                .Where(element => element.name == "gd-upgrade-option"))
            {
                Assert.That(option.worldBound.yMin, Is.GreaterThanOrEqualTo(tray.yMin - 0.5f));
                Assert.That(option.worldBound.yMax, Is.LessThanOrEqualTo(tray.yMax + 0.5f),
                    $"A complete Level Up option, including its gained-item fact, must fit inside " +
                    $"the comparison band. Option {option.worldBound}; band {tray}.");
                var card = Descendants(option)
                    .Single(element => element.name == "gd-card-preview");
                var item = Descendants(option)
                    .Single(element => element.name == "gd-upgrade-item");
                Assert.That(item.worldBound.center.x,
                    Is.EqualTo(card.worldBound.center.x).Within(UiPlayer.PixelSnapping()),
                    "The gained item is centred over the printed bottom item position.");
                Assert.That(item.worldBound.yMin,
                    Is.GreaterThan(card.worldBound.yMin + (card.worldBound.height * 0.88f)));
                Assert.That(item.worldBound.yMax,
                    Is.LessThanOrEqualTo(card.worldBound.yMax + UiPlayer.PixelSnapping()),
                    "The backed box remains wholly on the face instead of forming a detached row.");
                Assert.That(item.worldBound.height,
                    Is.EqualTo(46f).Within(UiPlayer.PixelSnapping()),
                    "The opaque box is tall enough to mask the candidate's printed item icon.");
                Assert.That(Descendants(item)
                        .Count(element => element.name == "gd-printed-channel-symbol"),
                    Is.EqualTo(1));
                Assert.That(Descendants(item).OfType<Label>().Select(label => label.text),
                    Is.EqualTo(new[] { "+1" }),
                    "Only the signed value remains text in the upgrade box.");
            }
            foreach (var card in cards)
            {
                Assert.That(card.worldBound.width,
                    Is.EqualTo(CardFootprint.Width).Within(UiPlayer.PixelSnapping()));
                Assert.That(card.worldBound.height,
                    Is.EqualTo(CardFootprint.Height).Within(UiPlayer.PixelSnapping()),
                    "No Level Up card is shrunk or cropped into a compact substitute.");
            }
            var scroll = Descendants(hud.RootElement).OfType<ScrollView>()
                .Single(element => element.name == "gd-tray-scroll");
            Assert.That(scroll.horizontalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));

            runtime.RoundView.Render(standingModel);
            hud.ApplyExclusiveContext();
            yield return null;
            yield return null;
            Assert.That(Named(hud.RootElement, "gd-hero-rail-red").worldBound,
                Is.EqualTo(redBefore));
            Assert.That(Named(hud.RootElement, "gd-hero-rail-blue").worldBound,
                Is.EqualTo(blueBefore), "Closing Level Up also leaves both columns standing.");
        }

        [UnityTest]
        public IEnumerator Scene_UpgradeCardsAcceptPointerAndKeyboardActivationWithoutFrames()
        {
            yield return LoadOfficialSixPlayerScene();
            var root = Root();
            RenderArtworkUpgradeChoices(root.Runtime);

            // Isolate the view from the scene presenter so both activations can be observed without an
            // intentionally presentation-shaped option being submitted to the scene's standing match.
            var view = new RoundActionUnityView();
            view.Render(root.Runtime.RoundView.Model);
            view.Root.style.position = Position.Absolute;
            view.Root.style.left = 300f;
            view.Root.style.top = 120f;
            view.Root.style.width = 1400f;
            view.Root.style.height = 560f;
            root.Hud.RootElement.Add(view.Root);
            try
            {
                yield return null;
                yield return null;
                var options = Descendants(view.Root)
                    .Where(element => element.name == "gd-upgrade-option")
                    .ToArray();
                Assert.That(options, Has.Length.EqualTo(6));
                Assert.That(options.Select(option => option.resolvedStyle.borderTopWidth),
                    Has.All.EqualTo(0f).Within(UiPlayer.PixelSnapping()),
                    "Pointer and keyboard controls paint no enclosing option outline.");

                var submitted = new List<TypedInvocationViewModel>();
                view.InvocationSubmitted += submitted.Add;
                UiPlayer.Click(options[0]);
                Assert.That(submitted, Has.Count.EqualTo(1));
                Assert.That(submitted[0].Command,
                    Is.SameAs(view.Model.SystemTransitionPrompt.Options[0].Command));

                options[1].Focus();
                yield return null;
                var focusedFacts = Descendants(options[1])
                    .Single(element => element.name == "gd-upgrade-facts");
                Assert.That(focusedFacts.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.001f),
                    "Keyboard focus keeps the centred item box fully opaque.");
                var focusedItem = Descendants(focusedFacts)
                    .Single(element => element.name == "gd-upgrade-item");
                Assert.That(focusedItem.resolvedStyle.borderTopColor,
                    Is.EqualTo(MatchHudTheme.ShineRim),
                    "Keyboard focus strengthens the box edge without altering the face.");
                using (var submit = NavigationSubmitEvent.GetPooled())
                {
                    submit.target = options[1];
                    options[1].SendEvent(submit);
                }

                Assert.That(submitted, Has.Count.EqualTo(2));
                Assert.That(submitted[1].Command,
                    Is.SameAs(view.Model.SystemTransitionPrompt.Options[1].Command));
            }
            finally
            {
                view.Root.RemoveFromHierarchy();
            }
        }

        [UnityTest]
        public IEnumerator Scene_CapturesPrintedItemSymbolsAtBothValidatedDesktopSizes()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                Assert.That(UnityEngine.Application.isBatchMode, Is.True,
                    "The two-resolution capture branch is exercised by the dedicated nonbatch run.");
                yield break;
            }

            var restoreGameView = SelectFixedGameViewResolution(2560, 1440);
            try
            {
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2560 || Screen.height != 1440);
                     frame++)
                {
                    yield return null;
                }

                Assert.That(Screen.width, Is.EqualTo(2560));
                Assert.That(Screen.height, Is.EqualTo(1440));
                MatchSceneCompositionRoot.NextComposedCardContent =
                    SceneCardContent.VerifiedTierTwoEngagedMirror;
                yield return LoadComposedScene();
                var root = Root();
                var standing = root.Runtime.RoundView.Model;
                RenderAllChannelItemPlaque(root.Runtime);
                yield return null;
                yield return new WaitForEndOfFrame();
                yield return Capture("tmp/milestone-8-30k-plaque-symbols-2560x1440.png");
                RenderArtworkUpgradeChoices(root.Runtime);
                root.Hud.ApplyExclusiveContext();
                yield return null;
                yield return new WaitForEndOfFrame();
                yield return Capture("tmp/milestone-8-30k-level-up-symbols-2560x1440.png");

                SelectFixedGameViewResolution(2048, 1152);
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2048 || Screen.height != 1152);
                     frame++)
                {
                    yield return null;
                }

                Assert.That(Screen.width, Is.EqualTo(2048));
                Assert.That(Screen.height, Is.EqualTo(1152));
                root.Runtime.RoundView.Render(standing);
                root.Hud.ApplyExclusiveContext();
                root.Hud.Layout();
                yield return null;
                yield return new WaitForEndOfFrame();
                yield return Capture("tmp/milestone-8-30k-plaque-symbols-2048x1152.png");
                RenderArtworkUpgradeChoices(root.Runtime);
                root.Hud.ApplyExclusiveContext();
                root.Hud.Layout();
                yield return null;
                yield return new WaitForEndOfFrame();
                var tray = Named(root.Hud.RootElement, "gd-tray-region").worldBound;
                var cards = Descendants(Named(root.Hud.RootElement, "gd-upgrade-comparison-band"))
                    .Where(element => element.name == "gd-card-preview")
                    .ToArray();
                Assert.That(cards, Has.Length.EqualTo(6));
                Assert.That(cards.All(card => card.worldBound.xMin >= tray.xMin - 0.5f &&
                        card.worldBound.xMax <= tray.xMax + 0.5f),
                    Is.True, "No validated desktop size clips a Level Up face horizontally.");
                Assert.That(Descendants(root.Hud.RootElement)
                        .Where(element => element.name == "gd-upgrade-option")
                        .All(option => option.worldBound.yMin >= tray.yMin - 0.5f &&
                            option.worldBound.yMax <= tray.yMax + 0.5f),
                    Is.True, "Every complete option and gained-item fact fits the validated view.");
                yield return Capture("tmp/milestone-8-30k-level-up-symbols-2048x1152.png");
            }
            finally
            {
                restoreGameView?.Invoke();
            }
        }

        /// <summary>
        /// Milestone 8.5a's targeted Game View inspection, and the running-scene proof behind it: the
        /// tray draws Wasp's verified starting five with each face's own words, in the five regions the
        /// approved mockup gives them, and every card lands inside the tray it is drawn in.
        ///
        /// It runs against the saved scene rather than a fixture because the layout is what is being
        /// checked, and only a live panel resolves one. The Editor tier proves the grouping is the
        /// definition's; this proves the cards fit.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_KeepsEveryPlayableHexClearWhileBoardArtworkUsesTheFullScreen()
        {
            yield return LoadOfficialSixPlayerScene();
            yield return null;
            yield return null;

            var hud = Root().Hud;
            var tray = Named(hud.RootElement, "gd-tray-region").worldBound;
            var camera = Object.FindAnyObjectByType<Camera>();
            var panelViewport = ConfiguredMatchHudRenderer.PanelViewport;
            Assert.That(camera.rect.x, Is.Zero.Within(0.0001f));
            Assert.That(camera.rect.y, Is.Zero.Within(0.0001f));
            Assert.That(camera.rect.width, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(camera.rect.height, Is.EqualTo(1f).Within(0.0001f),
                "The camera draws the decorative board across the dead space behind the HUD.");
            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            var safe = hud.CameraSafeLayout;
            Assert.That(safe.IsValid, Is.True);
            Assert.That(safe.LowerRightRegion.xMin, Is.GreaterThan(tray.xMin));
            Assert.That(safe.LowerRightRegion.yMax, Is.GreaterThan(tray.yMin));
            Assert.That(safe.UpperRegion.yMax,
                Is.EqualTo(panelViewport.y - ConfiguredMatchHudRenderer.BoardHexUiGap).Within(0.6f),
                "The broad safe arm uses the complete screen height; only actual hex containment, " +
                "not decorative artwork, is reserved.");
            Assert.That(safe.LowerRightRegion.xMin,
                Is.EqualTo(
                    ConfiguredMatchHudRenderer.CompactHandRightEdge +
                    ConfiguredMatchHudRenderer.BoardCardHexUiGap).Within(0.6f),
                "The lower safe edge follows the furthest full hover face, not five side-by-side " +
                "card slots.");
            var projectedHexes = mapView.CurrentTiles
                .Where(tile => tile.IsPlayable)
                .Select(tile => ProjectedPanelBounds(tile, camera, panelViewport, mapView.OuterRadius))
                .ToArray();
            Assert.That(projectedHexes, Has.Length.EqualTo(210));
            Assert.That(projectedHexes.All(bounds =>
                    ContainsBounds(safe.UpperRegion, bounds) ||
                    ContainsBounds(safe.LowerRightRegion, bounds)),
                Is.True,
                "Every complete hex fits one safe arm; no card, status block or rail touches it.");
            Assert.That(projectedHexes.Any(bounds => bounds.yMax > tray.yMin + 0.5f), Is.True,
                "At least one playable hex uses the reclaimed strip beside the hand; the camera did " +
                "not fall back to the old rectangle above the tray.");
        }

        [UnityTest]
        public IEnumerator Scene_DrawsTheVerifiedCardFacesInTheTrayAndCapturesThemForInspection()
        {
            using var generatedFallback =
                ConfiguredCardArtwork.UseTextureLoader(_ => null);
            yield return LoadScene();
            var root = Root();
            var hud = root.Hud;
            // Two frames, so the panel has laid out and every card has a resolved rectangle.
            yield return null;
            yield return null;

            var cards = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-card-choice")
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(5),
                "The viewed hero's whole current round is rulebook p.5's starting five.");

            foreach (var card in cards)
            {
                foreach (var region in new[]
                    {
                        "gd-card-initiative", "gd-card-name-band", "gd-card-secondary-values",
                        "gd-card-primary-action", "gd-card-rules", "gd-card-history"
                    })
                {
                    Assert.That(
                        Descendants(card).Count(element => element.name == region), Is.EqualTo(1),
                        $"Each tray card draws exactly one {region}.");
                }
            }

            var printed = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-card-printed-text")
                .OfType<UnityEngine.UIElements.Label>()
                .Select(label => label.text)
                .ToArray();
            var transcriptions = root.Runtime.Cards
                .Where(card => card.State.Location == CardLocation.Hand &&
                    card.OwnerId.Equals(root.Runtime.ViewedHeroId))
                .Select(card => root.Runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .SourceContent.PrintedText)
                .ToArray();
            Assert.That(printed, Is.EquivalentTo(transcriptions),
                "and the words on screen are the verified transcriptions, whole and unabbreviated.");

            var tray = Named(hud.RootElement, "gd-tray-region");
            Assert.That(tray.resolvedStyle.backgroundColor.a, Is.LessThanOrEqualTo(0.001f),
                "The layout-only tray draws no panel behind the imported card faces.");
            Assert.That(new[]
                {
                    tray.resolvedStyle.borderTopWidth,
                    tray.resolvedStyle.borderRightWidth,
                    tray.resolvedStyle.borderBottomWidth,
                    tray.resolvedStyle.borderLeftWidth
                }, Has.All.EqualTo(0f).Within(0.01f),
                "The hand has no surrounding tray outline; only the cards remain visible.");
            var trayScroll = Descendants(hud.RootElement).OfType<ScrollView>()
                .Single(element => element.name == "gd-tray-scroll");
            Assert.That(trayScroll.mode, Is.EqualTo(ScrollViewMode.Vertical),
                "The enlarged hand fits horizontally; only short-height overflow may scroll.");
            Assert.That(trayScroll.horizontalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
            Assert.That(trayScroll.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden),
                "The ordinary hand exposes no scrollbar in either direction.");
            // Milestone 8.20c replaced the vertical half of this claim, which used to be made only
            // where the viewport was tall enough to hold a card plus the tray's chrome. A card is now
            // exactly `CardFootprint` tall on every surface and no supported desktop viewport leaves
            // the region room for the contextual region above it *and* a whole card, so the region's
            // own scroll is what keeps the cards reachable - and a card inside the region's rectangle
            // is not something this layout tries to do any more. What replaces it is stronger and
            // always runs: the card is the standard size whatever its words say, which is the property
            // the old bound existed to protect. Reachability is proved by the 8.20c journeys, which
            // scroll to a card and click it.
            foreach (var card in cards)
            {
                var bounds = card.worldBound;
                Assert.That(bounds.width, Is.GreaterThan(0f), "Every card resolved a real rectangle.");
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(tray.worldBound.xMin - 0.5f));
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(tray.worldBound.xMax + 0.5f),
                    "and no card runs out through the side of the tray.");
                Assert.That(bounds.height,
                    Is.EqualTo(CardFootprint.Height).Within(UiPlayer.PixelSnapping()),
                    "and every card in the live tray is the standard footprint, so the tallest " +
                    "verified face's wrapped rules text no longer decides how tall a hand is; the " +
                    "only tolerance is one snapped panel pixel.");
            }

            var cardRow = Named(hud.RootElement, "gd-card-buttons");
            Assert.That(cardRow.style.flexWrap.value, Is.EqualTo(Wrap.NoWrap));
            Assert.That(cardRow.worldBound.xMin, Is.GreaterThanOrEqualTo(tray.worldBound.xMin));
            Assert.That(cardRow.worldBound.xMax, Is.LessThanOrEqualTo(tray.worldBound.xMax),
                "Five readable cards fit simultaneously without sideways scrolling.");
            Assert.That(cards.Select(card => Mathf.Round(card.worldBound.yMin)).Distinct().Count(),
                Is.EqualTo(1),
                "Five cards of one hand remain one ordered row.");
            Assert.That(cards.Max(card => card.worldBound.yMax),
                Is.LessThanOrEqualTo(tray.worldBound.yMax + 0.5f),
                "Lowering the cards spends dead space but never clips a printed face at the screen " +
                "edge.");
            Assert.That(tray.resolvedStyle.paddingTop,
                Is.EqualTo(ConfiguredMatchHudRenderer.TrayCardTopPadding).Within(0.1f));
            Assert.That(tray.resolvedStyle.paddingBottom,
                Is.EqualTo(ConfiguredMatchHudRenderer.TrayBottomPadding).Within(0.1f));
            Assert.That(tray.resolvedStyle.paddingTop,
                Is.EqualTo(tray.resolvedStyle.paddingBottom).Within(0.1f),
                "The layout-only reservation adds no invisible inset around the cards.");
            var handRoot = Named(hud.RootElement, "gd-card-tray");
            Assert.That(handRoot.style.top.value.value,
                Is.EqualTo(ConfiguredMatchHudRenderer.OrdinaryCardDownshift).Within(0.1f),
                "The card row spends the tray's formerly dead bottom strip instead of leaving the " +
                "faces pinned to its top.");
            Assert.That(tray.resolvedStyle.paddingLeft,
                Is.EqualTo(ConfiguredMatchHudRenderer.TrayHorizontalPadding).Within(0.1f),
                "The invisible tray inset does not hold the cards away from the left edge.");
            var statusDockElement = Named(hud.RootElement, "gd-status-dock");
            Assert.That(statusDockElement, Is.Not.Null,
                "Round and hand status have their own dock in the former Director-side area.");
            var statusDock = statusDockElement.worldBound;
            var redHeroes = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail" &&
                    element.parent?.name == "gd-hero-rail-red")
                .ToArray();
            Assert.That(redHeroes, Has.Length.GreaterThanOrEqualTo(1));
            var lastRedHero = redHeroes[redHeroes.Length - 1].worldBound;
            var director = Named(hud.RootElement, "gd-director-toggle").worldBound;
            Assert.That(director.xMin, Is.EqualTo(lastRedHero.xMin).Within(0.5f),
                "DIRECTOR sits in the Red rail column, not in the card tray or status dock.");
            Assert.That(director.xMax, Is.LessThanOrEqualTo(lastRedHero.xMax + 0.5f));
            Assert.That(director.yMin, Is.GreaterThanOrEqualTo(lastRedHero.yMax - 0.5f),
                "DIRECTOR is immediately below the last Red hero (the third in a six-player match).");
            Assert.That(director.yMax, Is.LessThanOrEqualTo(tray.worldBound.yMin + 0.5f));

            // Milestone 8.25f2: only playable hexes are reserved. The full camera is allowed behind
            // transparent HUD space so decorative artwork fills the dead area.
            var camera = Object.FindAnyObjectByType<Camera>();
            var panelViewport = ConfiguredMatchHudRenderer.PanelViewport;
            Assert.That(camera.rect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)),
                "Decorative map artwork renders behind the complete HUD.");
            var safe = hud.CameraSafeLayout;
            Assert.That(safe.IsValid, Is.True);
            Assert.That(statusDock.xMin, Is.EqualTo(director.xMin).Within(0.5f),
                "Round and hand status aligns with DIRECTOR.");
            Assert.That(statusDock.yMin, Is.GreaterThanOrEqualTo(director.yMax - 0.5f),
                "The fixed status block sits below DIRECTOR.");
            Assert.That(statusDock.width,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusDockWidth)
                    .Within(UiPlayer.PixelSnapping()),
                "The status block reserves the largest routine state's width from the first click.");
            // Milestone 8.30d, the user's final call: the dock reserves no height - the empty
            // box above the cards is retired, and content-driven is the invariant now.
            Assert.That(statusDock.height, Is.LessThan(24f),
                "The dock draws nothing in routine states; its empty box is retired.");
            Assert.That(statusDock.xMax,
                Is.LessThanOrEqualTo(safe.UpperRegion.xMin + 0.5f),
                "The relocated status stops before the upper hex-safe arm begins.");
            Assert.That(statusDock.yMax, Is.LessThanOrEqualTo(tray.worldBound.yMin + 0.5f),
                "The status dock uses the former Director-side area above the card band.");
            Assert.That(cardRow.worldBound.xMax,
                Is.LessThanOrEqualTo(safe.LowerRightRegion.xMin + 0.5f),
                "The lower safe arm begins only after the fifth card; artwork may run behind both.");

            yield return Capture("tmp/milestone-8-5a-card-tray.png");

            var chosen = root.Runtime.Cards.First(card =>
                card.State.Location == CardLocation.Hand &&
                card.OwnerId.Equals(root.Runtime.ViewedHeroId));
            Assert.That(root.Runtime.SelectCommitmentCard(chosen.Id), Is.True);
            yield return null;
            yield return Capture("tmp/milestone-8-5a-card-tray-selected.png");
        }

        [UnityTest]
        public IEnumerator Scene_OverlappedHandHoverRevealDoesNotStealTheCardClick()
        {
            yield return LoadScene();
            yield return null;
            yield return null;

            var root = Root();
            var cards = Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-card-choice")
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(5));
            var card = cards[cards.Length - 1];
            Assert.That(card.enabledInHierarchy, Is.True,
                "The opening commitment card must be clickable for this regression to be meaningful.");

            UiPlayer.PointAt(card);
            yield return null;

            var preview = root.Runtime.HandsView.HoverPreviewRoot;
            Assert.That(root.Runtime.HandsView.HighlightedPeekCard, Is.Not.Null,
                "Resting the pointer on the painted card opens its complete face.");
            Assert.That(preview.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(preview.parent, Is.SameAs(root.Hud.RootElement));
            Assert.That(preview.parent.Children().Last(), Is.SameAs(preview),
                "The reveal remains in the original click-safe HUD panel.");
            Assert.That(Named(root.Hud.RootElement, "gd-status-dock").resolvedStyle.visibility,
                Is.EqualTo(Visibility.Visible),
                "Opening a complete hand face leaves the action and status dock visible.");
            Assert.That(Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1),
                "Hover uses the existing HUD document; a second full-screen document would block " +
                "map clicks even if its contents ignored picking.");
            var fullFace = Descendants(preview)
                .Single(element => element.name == "gd-card-preview");
            Assert.That(fullFace.worldBound.width,
                Is.EqualTo(CardFootprint.Width).Within(UiPlayer.PixelSnapping()));
            Assert.That(fullFace.worldBound.height,
                Is.EqualTo(CardFootprint.Height).Within(UiPlayer.PixelSnapping()));
            Assert.That(Descendants(preview).Prepend(preview)
                    .All(element => element.pickingMode == PickingMode.Ignore),
                Is.True,
                "Every layer of the reveal is pointer-transparent, not merely its outer wrapper.");

            var pickedThroughPreview = card.panel.Pick(card.worldBound.center);
            Assert.That(pickedThroughPreview, Is.Not.Null);
            Assert.That(
                pickedThroughPreview == card ||
                pickedThroughPreview.GetFirstAncestorOfType<Button>() == card,
                Is.True,
                "The full reveal draws over the hand but the pointer still reaches the card below.");

            UiPlayer.PointAway(card);
            yield return null;
            Assert.That(Named(root.Hud.RootElement, "gd-status-dock").resolvedStyle.visibility,
                Is.EqualTo(Visibility.Visible),
                "Leaving the card does not need to restore a dock that hover never hid.");

            var blueHero = root.Runtime.Setup.Heroes
                .First(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            var blueView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(blueHero));
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(
                    pointerInput, root.Hud, blueView.transform.position),
                Is.True,
                "The Blue hero remains reachable through the configured board pointer path.");
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(blueHero),
                "Clicking the Blue hero switches the viewed hand.");

            card = Descendants(root.Hud.RootElement)
                .Last(element => element.name == "gd-card-choice");
            UiPlayer.PointAt(card);
            yield return null;
            UiPlayer.Click(card);
            yield return null;
            Assert.That(root.Runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId.HasValue,
                Is.True,
                "A click made while the full hover face is visible still selects the underlying card.");
        }

        [UnityTest]
        public IEnumerator Scene_StacksCurrentActionControlsAboveHandStatusWithoutOverlap()
        {
            yield return LoadScene();
            var root = Root();
            yield return null;
            yield return null;

            var dockElement = Named(root.Hud.RootElement, "gd-status-dock");
            var awaitingDock = dockElement.worldBound;
            yield return Capture("tmp/milestone-8-25f2-status-dock-awaiting.png");

            var red = Hand(root.Runtime, TeamSide.Red).Cards[0];
            var blue = Hand(root.Runtime, TeamSide.Blue).Cards[0];
            Assert.That(root.Runtime.SelectCommitmentCard(red.Id), Is.True);
            yield return null;
            yield return null;
            var confirmationDock = dockElement.worldBound;
            // Milestone 8.30f: the dock's hand-status bar remains retired, but Confirm alone
            // moved out of the old shared corner and into the open lane immediately below DIRECTOR.
            var confirmControl = Descendants(root.Hud.RootElement).OfType<Button>()
                .SingleOrDefault(control => control.text == "CONFIRM FACEDOWN");
            Assert.That(confirmControl, Is.Not.Null,
                "The middle measurement is the selected-card confirmation state.");
            Assert.That(Descendants(dockElement).OfType<Button>()
                    .Any(control => control.text == "CONFIRM FACEDOWN"),
                Is.False,
                "The dock's hand-status bar is retired; Confirm lives below DIRECTOR.");
            var confirmBox = confirmControl.worldBound;
            var confirmRect = root.Hud.ConfirmFacedownControlRect;
            var directorBox = Named(root.Hud.RootElement, "gd-director-toggle").worldBound;
            Assert.That(confirmBox.xMin,
                Is.EqualTo(confirmRect.xMin).Within(UiPlayer.PixelSnapping() * 2f),
                "The confirm banner uses the resolved below-DIRECTOR rectangle.");
            Assert.That(confirmBox.xMax,
                Is.EqualTo(confirmRect.xMax).Within(UiPlayer.PixelSnapping() * 2f));
            Assert.That(confirmBox.yMin,
                Is.EqualTo(confirmRect.yMin).Within(UiPlayer.PixelSnapping() * 2f));
            Assert.That(confirmBox.yMax,
                Is.EqualTo(confirmRect.yMax).Within(UiPlayer.PixelSnapping() * 2f));
            Assert.That(confirmBox.yMin,
                Is.GreaterThanOrEqualTo(directorBox.yMax +
                    ConfiguredMatchHudRenderer.ConfirmDirectorGap - UiPlayer.PixelSnapping() * 2f),
                "Confirm stands immediately below DIRECTOR without covering it.");
            yield return Capture("tmp/milestone-8-25f2-status-dock-confirmation.png");

            Assert.That(root.Runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(root.Runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(root.Runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(root.Runtime);
            yield return null;
            yield return null;

            var dock = dockElement.worldBound;
            var snap = UiPlayer.PixelSnapping();
            Assert.That(confirmationDock.size.x, Is.EqualTo(awaitingDock.size.x).Within(snap));
            Assert.That(confirmationDock.size.y, Is.EqualTo(awaitingDock.size.y).Within(snap));
            Assert.That(dock.size.x, Is.EqualTo(awaitingDock.size.x).Within(snap));
            Assert.That(dock.size.y, Is.EqualTo(awaitingDock.size.y).Within(snap),
                "Awaiting, confirmation and revealed action states keep one footprint.");
            Assert.That(dock.width,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusDockWidth).Within(snap));
            // Milestone 8.30d, the user's final call: the dock reserves no height and draws no
            // chrome - every routine tenant moved elsewhere, so an empty frame was all that
            // remained and it is gone.
            Assert.That(dock.height, Is.LessThan(24f),
                "The dock draws nothing in routine states; its empty box is retired.");
            yield return Capture("tmp/milestone-8-25f2-status-dock-actions.png");

            var context = Named(root.Hud.RootElement, "gd-context");
            var actionSection = Named(context, "gd-action-section");
            Assert.That(actionSection, Is.Not.Null);
            Assert.That(Descendants(actionSection).OfType<Label>()
                    .Select(label => label.text),
                Has.None.EqualTo("ACTIONS"),
                "The status sentence already names these as actions; no gutter repeats it.");
            // Milestone 8.30b's user-approved amendment. Printed actions moved onto the resolving card
            // at 8.29b1b and End Turn moved into its own status-bar slot, so the dock's
            // action row is empty in an ordinary turn and `HideEmptySections` hides it rather than
            // reserving a line for nothing.
            var actionControls = actionSection.Children().OfType<Button>().ToArray();
            Assert.That(actionControls, Is.Empty,
                "Printed actions live on the resolving card and End Turn has its own corner, so this " +
                "dock row holds no external control at all.");
            Assert.That(UiPlayer.IsShown(actionSection), Is.False,
                "A section with nothing in it is not drawn.");

            var endTurnControl = (Button)Named(root.Hud.RootElement, "gd-end-turn-control");
            Assert.That(endTurnControl, Is.Not.Null);
            Assert.That(endTurnControl.text,
                Is.EqualTo(root.Runtime.RoundView.Model.EndTurnInvocation.Label));
            var endTurnBox = endTurnControl.worldBound;
            var endTurnRect = root.Hud.EndTurnControlRect;
            Assert.That(endTurnBox.xMin, Is.EqualTo(endTurnRect.xMin).Within(snap),
                "End Turn stands where the stated corner rectangle puts it.");
            Assert.That(endTurnBox.yMin, Is.EqualTo(endTurnRect.yMin).Within(snap));
            Assert.That(endTurnBox.width, Is.EqualTo(ConfiguredMatchHudRenderer.EndTurnControlWidth)
                .Within(snap * 2f));
            Assert.That(endTurnBox.height, Is.EqualTo(ConfiguredMatchHudRenderer.EndTurnControlHeight)
                .Within(snap * 2f));
            Assert.That(endTurnBox.xMin, Is.GreaterThan(dock.xMax),
                "It is clear of the dock it used to sit inside.");
            Assert.That(endTurnBox.yMin,
                Is.EqualTo(ConfiguredMatchHudRenderer.EndTurnControlTopInset).Within(snap),
                "and anchored at the status bar's upper-right edge.");
            // Milestone 8.30b. This used to assert that wrapped action controls finished with a
            // deliberate gap before the hand-status row. There are no controls left here to wrap:
            // 8.29b1b moved the printed actions onto the resolving card and the amendment moved the
            // narration into the status strip and End Turn into the panel's corner, so an ordinary
            // revealed turn draws nothing in this region at all. That is the assertion now - a later
            // milestone that puts something back fails here and has its spacing judged again rather
            // than inheriting a gap rule about an empty box.
            var contextContent = Descendants(context)
                .Where(element => UiPlayer.IsShown(element) && element.worldBound.height > 0f)
                .ToArray();
            Assert.That(contextContent.Select(element => element.name).ToArray(), Is.Empty,
                "An ordinary revealed turn draws no contextual control.");
            // Milestone 8.30d, the user's follow-up decision: the hand-status bar is retired -
            // never shown in any state - and its commands are the corner banners asserted above.
            Assert.That(UiPlayer.IsShown(Named(root.Hud.RootElement, "gd-hand-header")), Is.False,
                "The hand-status bar is retired outright.");

            var firstCard = Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-card-choice")
                .Min(element => element.worldBound.yMin);
            Assert.That(dock.yMax, Is.LessThanOrEqualTo(firstCard + 0.5f),
                "The fixed status dock finishes before the cards begin.");
        }

        [UnityTest]
        public IEnumerator OrdinaryPrintedActions_AppearOnTheResolvingCardWithoutADuplicateDockRow()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            UiPlayer.SetPlayerViewport(2560, 1440, "8.29b1b reference printed-action surface");
            // Use the project's natural provider here: the local visual route exercises prepared
            // fronts, while a clean checkout still proves the generated fallback without private art.
            {
                yield return LoadScene();
                var root = Root();
                var runtime = root.Runtime;
                CommitNextTurn(runtime);
                yield return null;
                yield return null;

                var resolvingCard = Descendants(root.Hud.RootElement)
                    .Single(element => element.name == "gd-card-choice" &&
                        Descendants(element).Any(child =>
                            child.name == "gd-printed-action-overlay"));
                Assert.That(Descendants(resolvingCard)
                        .Count(element => element.name == "gd-printed-action-region"),
                    Is.EqualTo(runtime.RoundView.Model.Actions.Count +
                        runtime.RoundView.Model.UnavailableActions.Count),
                    "Every projected printed action owns one region on the fully exposed resolving card.");

                var actionSection = Named(root.Hud.RootElement, "gd-action-section");
                Assert.That(actionSection.Children().OfType<Button>().ToArray(), Is.Empty,
                    "Printed actions have no duplicate row, and milestone 8.30b moved End Turn out " +
                    "of this dock entirely.");
                Assert.That(((Button)Named(root.Hud.RootElement, "gd-end-turn-control")).text,
                    Is.EqualTo(runtime.RoundView.Model.EndTurnInvocation.Label),
                    "End Turn carries the projected label in its own corner surface.");

                var regions = Descendants(resolvingCard)
                    .Where(element => element.name == "gd-printed-action-region")
                    .OfType<Button>().ToArray();
                Assert.That(regions.Count(region => region.enabledSelf),
                    Is.EqualTo(runtime.RoundView.Model.PrintedActionRegions.Count(region =>
                        region.Invocation != null)));
                Assert.That(regions.Select(region => region.text), Is.All.Empty,
                    "The prepared or generated face supplies the visible action wording.");

                // Milestone 8.30d, final direction, on the natural providers: whenever the
                // resolving card draws prepared artwork, each offered SECONDARY is lit by its
                // badge overlay, each offered PRIMARY by its printed-lines ring overlay, and a
                // checkout without overlays degrades to the named fallbacks - which is what the
                // Game View capture below is evidence of.
                if (Descendants(resolvingCard).Any(element => element.name == "gd-card-artwork"))
                {
                    var liveShines = Descendants(resolvingCard)
                        .OfType<UnityEngine.UIElements.Image>()
                        .Count(element => element.name == "gd-card-shine");
                    var offeredRegions = runtime.RoundView.Model.PrintedActionRegions
                        .Count(region => region.Invocation != null);
                    var fallbackGlows = Descendants(resolvingCard)
                        .Count(element => element.name == "gd-prepared-action-glow");
                    var drawnPrimaryGlows = Descendants(resolvingCard)
                        .Count(element => element.name == "gd-primary-offer-glow");
                    Assert.That(liveShines + fallbackGlows + drawnPrimaryGlows,
                        Is.EqualTo(offeredRegions),
                        "Every offered region is lit by exactly one treatment - a prepared " +
                        "overlay (badge or printed-lines ring) or its named fallback.");
                }

                AssertPrintedActionControlsStayOnCard(resolvingCard, "2560x1440");
                yield return Capture("tmp/milestone-8-29b1b-printed-actions-2560x1440.png");

                UiPlayer.SetPlayerViewport(2048, 1152, "8.29b1b validated desktop printed-action surface");
                yield return null;
                yield return null;
                resolvingCard = Descendants(root.Hud.RootElement)
                    .Single(element => element.name == "gd-card-choice" &&
                        Descendants(element).Any(child =>
                            child.name == "gd-printed-action-overlay"));
                regions = Descendants(resolvingCard)
                    .Where(element => element.name == "gd-printed-action-region")
                    .OfType<Button>().ToArray();
                AssertPrintedActionControlsStayOnCard(resolvingCard, "2048x1152");
                yield return Capture("tmp/milestone-8-29b1b-printed-actions-2048x1152.png");

                // The hover walk drives a SECONDARY region: primaries wear the section glow
                // (asserted by count above) while secondaries keep the three-way treatment the
                // branches below cover - shine overlay, prepared fallback, or generated chip.
                var available = regions.First(region => region.enabledSelf &&
                    ((PrintedActionRegionViewModel)region.userData).InvocationContext !=
                        ActionInvocationContext.PrintedPrimary);
                var projected = (PrintedActionRegionViewModel)available.userData;
                var expectedSlot = projected.ActionKind == GuardsDigital.Core.Cards.ActionKind.Defense
                    ? CardShineSlot.SecondaryDefense
                    : CardShineSlot.SecondaryMovement;
                var shineImage = Descendants(resolvingCard)
                    .OfType<UnityEngine.UIElements.Image>()
                    .FirstOrDefault(element => element.name == "gd-card-shine" &&
                        expectedSlot.Equals(element.userData));
                var restingShine = shineImage?.style.opacity.value ?? 0f;
                var preparedGlow = Descendants(available)
                    .SingleOrDefault(element => element.name == "gd-prepared-action-glow");
                var restingPreparedGlow = preparedGlow?.style.opacity.value ?? 0f;
                var litChip = Descendants(resolvingCard).FirstOrDefault(element =>
                    element.name == (expectedSlot == CardShineSlot.Primary
                        ? "gd-card-primary-value"
                        : expectedSlot == CardShineSlot.SecondaryDefense
                            ? "gd-card-secondary-defense"
                            : "gd-card-secondary-movement"));
                var restingChipColor = litChip?.style.backgroundColor.value ?? default;
                var bounds = resolvingCard.worldBound;
                UiPlayer.PointAt(available);
                if (shineImage != null)
                {
                    Assert.That(shineImage.style.opacity.value, Is.GreaterThan(restingShine),
                        "Pointer hover raises the printed icon's shine to full.");
                }
                else if (preparedGlow != null)
                {
                    Assert.That(preparedGlow.style.opacity.value,
                        Is.GreaterThan(restingPreparedGlow),
                        "Pointer hover strengthens the same prepared circle or box.");
                }
                else
                {
                    Assert.That(litChip, Is.Not.Null,
                        "A generated face carries the typed chip its offer is about.");
                    Assert.That(litChip.style.backgroundColor.value,
                        Is.Not.EqualTo(restingChipColor),
                        "Pointer hover brightens the lit chip without moving anything.");
                }
                UiPlayer.PointAway(available);
                Assert.That(resolvingCard.worldBound, Is.EqualTo(bounds),
                    "Hovering the overlay changes no card-footprint geometry.");
                var acceptedBefore = runtime.AcceptedCommands.Count;
                UiPlayer.Click(available);
                yield return null;

                Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBefore + 1));
                Assert.That(runtime.AcceptedCommands.Last(), Is.EqualTo(projected.Invocation.Command));
                Assert.That(runtime.AcceptedCommands.Last().Kind,
                    Is.EqualTo(DirectorCommandKind.StartAction));
                Assert.That(runtime.HandsView.ForwardPrintedActionRegion(projected), Is.False,
                    "The old region is stale immediately after its exact offer advances.");
            }

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.29b1b restore the viewport this session was running at");
            yield return null;
        }

        private static void AssertPrintedActionControlsStayOnCard(
            VisualElement resolvingCard, string viewport)
        {
            var regions = Descendants(resolvingCard)
                .Where(element => element.name == "gd-printed-action-region")
                .OfType<Button>().ToArray();
            Assert.That(regions, Is.Not.Empty, $"No printed action regions draw at {viewport}.");
            var card = resolvingCard.worldBound;
            Assert.That(card.width,
                Is.EqualTo(CardFootprint.ArtworkWidth).Within(UiPlayer.PixelSnapping()),
                $"The resolving card keeps the standard artwork width at {viewport}.");
            Assert.That(card.height,
                Is.EqualTo(CardFootprint.ArtworkHeight).Within(UiPlayer.PixelSnapping()),
                $"The resolving card keeps the standard artwork height at {viewport}.");
            var controls = regions.Cast<VisualElement>().Concat(Descendants(resolvingCard)
                .Where(element => element.name == "gd-action-replacement-chip"));
            foreach (var region in controls)
            {
                var bounds = region.worldBound;
                var label = (region as Button)?.text ?? region.name;
                Assert.That(bounds.width, Is.GreaterThan(0f), $"{label} has no width at {viewport}.");
                Assert.That(bounds.height, Is.GreaterThan(0f), $"{label} has no height at {viewport}.");
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(card.xMin - 0.5f));
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(card.xMax + 0.5f));
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(card.yMin - 0.5f));
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(card.yMax + 0.5f));
            }
        }

        [UnityTest]
        public IEnumerator ResolvingCardActionRegions_OverlayPreparedArtworkWithoutChangingItsFootprint()
        {
            var texture = new Texture2D(372, 520, TextureFormat.RGBA32, false);
            try
            {
                using (ConfiguredCardArtwork.UseTextureLoader(_ => texture))
                {
                    yield return LoadScene();
                    var root = Root();
                    CommitNextTurn(root.Runtime);
                    yield return null;
                    yield return null;

                    var resolvingCard = Descendants(root.Hud.RootElement)
                        .Single(element => element.name == "gd-card-choice" &&
                            Descendants(element).Any(child =>
                                child.name == "gd-printed-action-overlay"));
                    Assert.That(Descendants(resolvingCard)
                        .Any(element => element.name == "gd-card-artwork"), Is.True);
                    Assert.That(Descendants(resolvingCard)
                            .Count(element => element.name == "gd-printed-action-region"),
                        Is.EqualTo(root.Runtime.RoundView.Model.PrintedActionRegions.Count));
                    Assert.That(resolvingCard.worldBound.width,
                        Is.EqualTo(CardFootprint.ArtworkWidth).Within(UiPlayer.PixelSnapping()));
                    Assert.That(resolvingCard.worldBound.height,
                        Is.EqualTo(CardFootprint.ArtworkHeight).Within(UiPlayer.PixelSnapping()));
                }
            }
            finally
            {
                Object.Destroy(texture);
            }
        }

        [UnityTest]
        public IEnumerator HoveringTheResolvingCard_PreservesItsLivePrintedActionSurface()
        {
            var texture = new Texture2D(372, 520, TextureFormat.RGBA32, false);
            try
            {
                using (ConfiguredCardArtwork.UseTextureLoader(_ => texture))
                {
                    yield return LoadScene();
                    var root = Root();
                    CommitNextTurn(root.Runtime);
                    yield return null;
                    yield return null;

                    var resolvingCard = Descendants(root.Hud.RootElement)
                        .Single(element => element.name == "gd-card-choice" &&
                            Descendants(element).Any(child =>
                                child.name == "gd-printed-action-overlay"));
                    var overlay = Descendants(resolvingCard)
                        .Single(element => element.name == "gd-printed-action-overlay");
                    var regions = Descendants(overlay)
                        .Where(element => element.name == "gd-printed-action-region")
                        .ToArray();
                    var preview = Named(root.Hud.RootElement, "gd-hand-hover-preview");
                    Assert.That(preview.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

                    UiPlayer.PointAt(resolvingCard);
                    yield return null;

                    Assert.That(preview.resolvedStyle.display, Is.EqualTo(DisplayStyle.None),
                        "The already exposed resolving card must not raise a standalone face over its actions.");
                    Assert.That(overlay.panel, Is.Not.Null);
                    Assert.That(Descendants(resolvingCard)
                            .Where(element => element.name == "gd-printed-action-region"),
                        Is.EqualTo(regions),
                        "Hover preserves the exact live region objects and their typed offers.");
                    Assert.That(regions.OfType<Button>().Select(region => region.text), Is.All.Empty);

                    UiPlayer.PointAway(resolvingCard);
                    yield return null;
                    Assert.That(preview.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                }
            }
            finally
            {
                Object.Destroy(texture);
            }
        }

        /// <summary>
        /// Milestone 8.25f's tracked Play Mode path. One representative injected texture and a forced
        /// absent provider each cross the ordinary hand, a Defense choice, an upgrade choice, and the
        /// rail's shared full-face preview. Private files can be present or absent without deciding the
        /// result.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_ArtworkAwareCompleteFacesUseInjectedTextureAndGeneratedFallbackAcrossPlayerSurfaces()
        {
            var texture = new Texture2D(372, 520, TextureFormat.RGBA32, false);
            try
            {
                var requested = new List<string>();
                using (ConfiguredCardArtwork.UseTextureLoader(path =>
                       {
                           requested.Add(path);
                           return texture;
                       }))
                {
                    yield return LoadScene();
                    var runtime = Root().Runtime;
                    runtime.RefreshAll();
                    yield return null;

                    AssertCompleteFaceArtwork(
                        runtime.HandsView.Root, "gd-card-choice", 5, true, "the ordinary hand");
                    Assert.That(Descendants(runtime.HandsView.Root)
                        .Any(element => element.name == "gd-card-selection-outline"), Is.False,
                        "An unselected prepared hand has no permanent synthetic outline.");
                    var selected = Hand(runtime, TeamSide.Red).TrayCards.First(card => card.IsSelectable);
                    Assert.That(runtime.SelectCommitmentCard(selected.CardInstanceId), Is.True,
                        "The art child leaves the ordinary card control reachable.");
                    AssertCompleteFaceArtwork(
                        runtime.HandsView.Root, "gd-card-choice", 5, true, "the selected hand");
                    Assert.That(Descendants(runtime.HandsView.Root)
                        .Count(element => element.name == "gd-card-selection-outline"),
                        Is.EqualTo(1),
                        "Only the selected prepared card receives the non-layout overlay.");

                    RenderArtworkDefenseChoices(runtime);
                    AssertCompleteFaceArtwork(
                        runtime.RoundView.Root, "gd-card-preview", 4, true, "the Defense choice");
                    var defense = runtime.RoundView.Model.DefenseChoices.First(choice => choice.Card != null);
                    TypedInvocationViewModel submitted = null;
                    runtime.RoundView.InvocationSubmitted += value => submitted = value;
                    runtime.RoundView.ForwardInvocation(defense);
                    Assert.That(submitted, Is.SameAs(defense),
                        "The artwork adds no interception between the card control and its invocation.");

                    RenderArtworkUpgradeChoices(runtime);
                    AssertCompleteFaceArtwork(
                        runtime.RoundView.Root, "gd-card-preview", 6, true, "the upgrade choice");

                    runtime.HeroOverviewsView.ForwardDiscardCardHighlight(
                        Hand(runtime, TeamSide.Red).TrayCards[0]);
                    AssertCompleteFaceArtwork(
                        runtime.HeroOverviewsView.HoverPreviewRoot,
                        "gd-card-preview",
                        1,
                        true,
                        "the rail preview");
                }

                Assert.That(requested, Is.Not.Empty);
                Assert.That(
                    requested.All(path => path.StartsWith(
                        "Private/Cards/", System.StringComparison.Ordinal)),
                    Is.True);

                using (ConfiguredCardArtwork.UseTextureLoader(_ => null))
                {
                    var runtime = Root().Runtime;
                    runtime.RefreshAll();
                    var fallbackSelection = Hand(runtime, TeamSide.Red).TrayCards
                        .Last(card => card.IsSelectable);
                    Assert.That(
                        runtime.SelectCommitmentCard(fallbackSelection.CardInstanceId),
                        Is.True,
                        "A second ordinary selection rebuilds the cached hand through its real " +
                        "player path before the forced fallback is inspected.");
                    yield return null;
                    AssertCompleteFaceArtwork(
                        runtime.HandsView.Root, "gd-card-choice", 5, false, "the fallback hand");

                    RenderArtworkDefenseChoices(runtime);
                    AssertCompleteFaceArtwork(
                        runtime.RoundView.Root,
                        "gd-card-preview",
                        4,
                        false,
                        "the fallback Defense choice");

                    RenderArtworkUpgradeChoices(runtime);
                    AssertCompleteFaceArtwork(
                        runtime.RoundView.Root,
                        "gd-card-preview",
                        6,
                        false,
                        "the fallback upgrade choice");

                    runtime.HeroOverviewsPresenter.Refresh();
                    runtime.HeroOverviewsView.ForwardDiscardCardHighlight(
                        Hand(runtime, TeamSide.Red).TrayCards[0]);
                    AssertCompleteFaceArtwork(
                        runtime.HeroOverviewsView.HoverPreviewRoot,
                        "gd-card-preview",
                        1,
                        false,
                        "the fallback rail preview");
                }
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Milestone 8.25f2's non-batch 2048x1152 inspection. It checks one imported front from every
        /// configured hero, then captures the enlarged ordinary hand in its reserved band rather than
        /// a presentation-only gallery.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_CapturesReadableReservedCardBandAt2048By1152()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                yield break;
            }

            var restoreGameView = SelectFixedGameViewResolution(2048, 1152);
            try
            {
                yield return LoadOfficialSixPlayerScene();
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2048 || Screen.height != 1152);
                     frame++)
                {
                    yield return null;
                }

                Assert.That(Screen.width, Is.EqualTo(2048));
                Assert.That(Screen.height, Is.EqualTo(1152));

                var representatives = new[]
                {
                    WaspCardContent.Definition(WaspCardContent.Shock),
                    XargathaCardContent.Definition(XargathaCardContent.StoneGaze),
                    TigerclawCardContent.Definition(TigerclawCardContent.Dodge),
                    ArienCardContent.Definition(ArienCardContent.LiquidLeap),
                    BroganCardContent.Definition(BroganCardContent.BrutalJab),
                    SabinaCardContent.Definition(SabinaCardContent.Quickdraw)
                };
                foreach (var representative in representatives)
                {
                    Assert.That(ConfiguredCardArtwork.TryLoadTexture(
                        representative.Reference, out var imported, out _), Is.True,
                        $"The prepared representative {representative.Reference} did not load.");
                    Assert.That(imported.width, Is.EqualTo((int)CardFootprint.ArtworkSourceWidth));
                    Assert.That(imported.height, Is.EqualTo((int)CardFootprint.ArtworkSourceHeight),
                        $"Unity resampled {representative.Reference} away from 372x520.");
                }

                var root = Root();
                root.Runtime.RefreshAll();
                yield return null;
                yield return null;
                var faces = Descendants(root.Runtime.HandsView.Root)
                    .Where(element => element.name == "gd-card-choice")
                    .ToArray();
                Assert.That(faces, Has.Length.EqualTo(5),
                    "The ordinary p.6 hand is the view being inspected.");
                Assert.That(
                    faces.All(face => Descendants(face)
                        .Count(element => element.name == "gd-card-artwork") == 1),
                    Is.True,
                    "Every ordinary tray card uses its prepared front.");
                Assert.That(
                    faces.All(face =>
                        Mathf.Abs(face.worldBound.width - CardFootprint.StandardWidth) < 1.1f &&
                        Mathf.Abs(face.worldBound.height - CardFootprint.ArtworkHeight) < 1.1f),
                    Is.True,
                    "Every real front preserves the 372:520 footprint at live panel scale.");
                Assert.That(faces.All(face => !Descendants(face).Any(element =>
                        element.name == "gd-card-frame" || element.name == "gd-card-history")),
                    Is.True, "No generated shell or lifecycle strip surrounds the real fronts.");
                Assert.That(faces.All(face => face.style.borderTopWidth.value == 0f), Is.True,
                    "The ordinary unselected cards have no permanent outer frame.");

                var tray = Named(root.Hud.RootElement, "gd-tray-region").worldBound;
                var trayElement = Named(root.Hud.RootElement, "gd-tray-region");
                Assert.That(trayElement.resolvedStyle.backgroundColor.a,
                    Is.LessThanOrEqualTo(0.001f));
                Assert.That(trayElement.resolvedStyle.borderTopWidth, Is.Zero.Within(0.01f),
                    "The visual capture contains cards, not a frame around the card band.");
                var row = Named(root.Hud.RootElement, "gd-card-buttons").worldBound;
                var scroll = Descendants(root.Hud.RootElement).OfType<ScrollView>()
                    .Single(element => element.name == "gd-tray-scroll");
                Assert.That(scroll.mode, Is.EqualTo(ScrollViewMode.Vertical),
                    "The ordinary readable hand exposes no horizontal scrollbar.");
                Assert.That(row.xMax, Is.LessThanOrEqualTo(tray.xMax + 0.5f),
                    "All five readable cards are simultaneously visible.");
                var orderedFaces = faces.OrderBy(face => face.worldBound.xMin).ToArray();
                for (var index = 1; index < orderedFaces.Length; index++)
                {
                    var gap = orderedFaces[index].worldBound.xMin -
                        orderedFaces[index - 1].worldBound.xMax;
                    Assert.That(gap, Is.LessThanOrEqualTo(
                            DirectorHandsUnityView.CardGap +
                            (2f * (DirectorHandsUnityView.ActiveOutlineGap +
                                DirectorHandsUnityView.ActiveOutlineWidth)) + 0.5f),
                        "Adjacent native-ratio card faces use only the tightened reserved outlines " +
                        "and gutter.");
                }

                var camera = Object.FindAnyObjectByType<Camera>();
                Assert.That(camera.rect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
                var safe = root.Hud.CameraSafeLayout;
                Assert.That(safe.IsValid, Is.True);
                Assert.That(row.xMax,
                    Is.LessThanOrEqualTo(safe.LowerRightRegion.xMin + 0.5f),
                    "The lower-right hex-safe arm begins beyond all five cards.");

                var statusDock = Named(root.Hud.RootElement, "gd-status-dock").worldBound;
                Assert.That(statusDock.xMax,
                    Is.LessThanOrEqualTo(safe.UpperRegion.xMin + 0.5f),
                    "The relocated status block ends before the playable hexes begin.");
                var redHeroes = Descendants(root.Hud.RootElement)
                    .Where(element => element.name == "gd-hero-rail" &&
                        element.parent?.name == "gd-hero-rail-red")
                    .ToArray();
                Assert.That(redHeroes, Has.Length.EqualTo(3));
                var thirdRedHero = redHeroes[2].worldBound;
                var director = Named(root.Hud.RootElement, "gd-director-toggle").worldBound;
                Assert.That(director.xMin, Is.EqualTo(thirdRedHero.xMin).Within(0.5f));
                Assert.That(director.xMax, Is.LessThanOrEqualTo(thirdRedHero.xMax + 0.5f));
                Assert.That(director.yMin, Is.GreaterThanOrEqualTo(thirdRedHero.yMax - 0.5f),
                    "DIRECTOR stands directly below the third Red hero rail.");
                Assert.That(director.yMax, Is.LessThanOrEqualTo(tray.yMin + 0.5f));
                Assert.That(statusDock.xMin, Is.EqualTo(director.xMin).Within(0.5f));
                Assert.That(statusDock.yMin, Is.GreaterThanOrEqualTo(director.yMax - 0.5f),
                    "The compact status block begins below DIRECTOR.");
                Assert.That(statusDock.width,
                    Is.EqualTo(ConfiguredMatchHudRenderer.StatusDockWidth)
                        .Within(UiPlayer.PixelSnapping()),
                    "The awaiting state already reserves the largest routine dock width.");
                // Milestone 8.30d, the user's final call: no reserved dock height - the empty
                // box above the cards is retired, and the dock is only as tall as whatever
                // (rarely) still renders into it.
                Assert.That(statusDock.height, Is.LessThan(24f),
                    "The dock draws nothing in routine states; its empty box is retired.");

                yield return Capture("tmp/milestone-8-25f2-readable-reserved-card-band.png");
            }
            finally
            {
                restoreGameView();
            }
        }

        /// <summary>
        /// Milestone 8.5b in the running scene: the card that sourced a live Active effect is outlined
        /// and labelled, a card can be selected and active at once, and both go when the effect ends.
        ///
        /// It runs on the showcase composition because that is the only content in this build that
        /// creates an Active effect - every Wasp printed primary is still greyed under `DEV-006` until
        /// its own slice composes it - and on the round-scoped mode of the Bulwark Skill, so the effect
        /// outlives the turn that made it and is still live while the next turn's commitment phase can
        /// select a card. The three states the milestone asks to see are captured in order.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_MarksTheSourceCardWhileItsActiveEffectIsLiveAndCapturesEachState()
        {
            yield return LoadShowcaseScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            yield return null;

            var bulwark = runtime.Cards.Single(card => card.Id.Value == "red-bulwark-card");
            var blueHeroId = runtime.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            var blue = runtime.Cards
                .Where(card => card.State.Location == CardLocation.Hand &&
                    card.OwnerId.Equals(blueHeroId))
                .OrderBy(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();
            foreach (var card in new[] { bulwark, blue })
            {
                Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            }

            Assert.That(runtime.SubmitAction(new ActionDefinitionId("primary-modification-skill")),
                Is.True);
            Assert.That(
                runtime.SubmitEffectChoiceOption(new EffectNodeId("bulwark-fast-travel-restriction")),
                Is.True);
            yield return null;

            Assert.That(runtime.ActiveEffectSourceCardInstanceIds,
                Is.EqualTo(new[] { bulwark.Id }),
                "The authority reports exactly the card that sourced the live effect.");
            // The turn may already have moved on to the other hero, and the tray follows whoever the
            // match is waiting on; the indicator is a fact about a card, so point the tray at the hand
            // that holds it before reading it.
            Assert.That(runtime.SelectViewedHero(bulwark.OwnerId), Is.True);
            yield return null;
            AssertOneCardIsMarkedActive(hud, "while the effect is live");
            yield return Capture("tmp/milestone-8-5b-active.png");

            // Play the turn out, so the next turn's commitment phase can select a card while the
            // round-scoped effect is still live - the two states standing together. The Skill may have
            // closed its own turn already, so the advance is offered rather than assumed.
            AdvancePastTheCurrentTurn(runtime);
            yield return null;
            if (runtime.Phase == RoundResolutionPhase.AwaitingCommitments &&
                runtime.IsActiveEffectSource(bulwark.Id))
            {
                var selectable = runtime.Cards.First(card =>
                    card.State.Location == CardLocation.Hand &&
                    card.OwnerId.Equals(bulwark.OwnerId));
                Assert.That(runtime.SelectCommitmentCard(selectable.Id), Is.True);
                Assert.That(runtime.SelectViewedHero(bulwark.OwnerId), Is.True);
                yield return null;
                AssertOneCardIsMarkedActive(hud, "with another card selected beside it");
                yield return Capture("tmp/milestone-8-5b-active-and-selected.png");
            }

            for (var turn = 0; turn < 8 && runtime.Phase != RoundResolutionPhase.EndOfRound; turn++)
            {
                if (runtime.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitNextTurn(runtime);
                }

                AdvancePastTheCurrentTurn(runtime);
            }

            Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound),
                "The round has to reach its end for a round-scoped effect to expire.");

            while (runtime.Phase == RoundResolutionPhase.EndOfRound)
            {
                var options = runtime.PausedSystemTransitionOptions;
                if (options.Count > 0)
                {
                    Assert.That(runtime.AnswerPausedSystemTransition(options[0]), Is.True);
                    continue;
                }

                Assert.That(runtime.ContinueEndOfRound(), Is.True);
            }

            yield return null;
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Empty,
                "Round End expires a round-scoped effect, so nothing is live any more.");
            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-card-active"),
                Is.EqualTo(0),
                "and the indicator is gone with it rather than left behind by presentation.");
            yield return Capture("tmp/milestone-8-5b-ended.png");
        }

        /// <summary>
        /// Ends the current turn, however many projected stops that takes and whether or not the action
        /// already closed it. Nothing here assumes a turn shape - only that the round moves on.
        /// </summary>
        /// <summary>
        /// Milestone 8.7a in the running scene: the hero rail keeps the card being played in its own
        /// header marker and only numbers a turn once that card has resolved.
        ///
        /// It runs against the saved scene rather than a fixture because the three things the milestone
        /// asks to see are all layout - the marker fits the compact rail beside the hero's name, the
        /// mirrored rail puts it on the opposite side, and the whole rail still stands inside the region
        /// it reserved. The Editor tier proves the states themselves against live authority. The three
        /// captures are the targeted Game View inspection against the approved mockup.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_SeparatesTheCardInPlayFromResolvedTurnHistoryOnBothRails()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            yield return null;
            yield return null;

            var rails = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-current-card-slot"),
                Is.EqualTo(rails.Length),
                "Every rail reserves the marker's footprint, so the header never moves.");
            Assert.That(
                Descendants(hud.RootElement).Any(element => element.name == "gd-current-card"),
                Is.False,
                "Nobody has committed, so no rail draws a marker inside its slot.");

            // One hero committed: a facedown back, and nothing about the card.
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            yield return null;
            var facedown = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-current-card")
                .ToArray();
            Assert.That(facedown, Has.Length.EqualTo(1));
            Assert.That(
                Descendants(facedown[0]).Any(element => element.name == "gd-current-card-back"),
                Is.True,
                "A commitment nobody may read yet is a neutral back.");
            Assert.That(
                Descendants(hud.RootElement).OfType<UnityEngine.UIElements.Label>()
                    .Where(label => label.name == "gd-current-card-state")
                    .Select(label => label.text),
                Is.EquivalentTo(new[] { "FACEDOWN" }),
                "A back with nothing on it says nothing, so the facedown marker keeps its word.");
            yield return Capture("tmp/milestone-8-7a-facedown.png");

            // Both committed: p.7's joint reveal, both cards in play, no turn numbered yet.
            var blue = Hand(runtime, TeamSide.Blue).Cards[0];
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            yield return null;
            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-current-card"),
                Is.EqualTo(2),
                "Joint reveal puts both heroes' cards in play, one marker each.");
            Assert.That(
                Descendants(hud.RootElement).Any(element => element.name == "gd-current-card-state"),
                Is.False,
                "and a revealed marker carries its colour and Initiative rather than a caption.");
            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes
                    .SelectMany(hero => hero.TurnSlots).Any(slot => slot.IsOccupied),
                Is.False,
                "The round's history names both cards already; neither turn is over.");

            // Every rail stays inside its own reserved region with the marker in it, and the two rails
            // are mirrored - the marker on the inner corner of each.
            var playfieldViewport = ConfiguredMatchHudRenderer.PanelViewport;
            var playfield = ConfiguredMatchHudRenderer.CalculatePlayfieldRect(
                playfieldViewport.x, playfieldViewport.y);
            foreach (var rail in Descendants(hud.RootElement).Where(element =>
                element.name == "gd-hero-rail"))
            {
                var bounds = rail.worldBound;
                Assert.That(bounds.width, Is.GreaterThan(0f), "Every rail resolved a real rectangle.");
                Assert.That(bounds.width,
                    Is.LessThanOrEqualTo(ConfiguredMatchHudRenderer.HeroRailWidth + 0.5f),
                    "and the marker did not widen the rail past its reserved band.");
                var marker = Descendants(rail).First(element => element.name == "gd-current-card-slot");
                var onLeftEdge = bounds.center.x < playfield.center.x;
                Assert.That(
                    marker.worldBound.center.x > bounds.center.x,
                    Is.EqualTo(onLeftEdge),
                    "The marker sits on each rail's inner corner: right on the left rail, left on the " +
                    "mirrored one.");
            }

            yield return Capture("tmp/milestone-8-7a-resolving.png");

            // The acting hero's card resolves: its marker clears and its turn is numbered history.
            var acting = runtime.SelectedActor.ActorId;
            AdvancePastTheCurrentTurn(runtime);
            yield return null;
            var resolved = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(acting));
            Assert.That(resolved.CurrentCard, Is.Null);
            Assert.That(resolved.TurnSlots.Count(slot => slot.IsOccupied), Is.EqualTo(1),
                "and exactly one numbered turn is now history.");
            // Milestone 8.23a took the `RESOLVED TURNS` caption off the rail to give the row's line
            // back, at the user's 2026-08-26 direction, so what the slots are is carried by their own
            // hover and assistive labels - the treatment milestone 8.7a gave the team label and 8.5a
            // gave a card's printed colour. The claim this case makes is therefore the same one at the
            // surface the fact moved to: the occupied slot names its turn and the card on it, and an
            // empty one names its turn and says so.
            Assert.That(
                Descendants(hud.RootElement).OfType<UnityEngine.UIElements.Label>()
                    .Any(label => label.name == "gd-turn-slots-caption"),
                Is.False,
                "The caption's line is what this milestone gave back to the rail.");
            foreach (var rail in runtime.HeroOverviewsView.Model.Heroes)
            {
                foreach (var slot in rail.TurnSlots)
                {
                    Assert.That(slot.AssistiveLabel, Does.StartWith($"Turn {slot.TurnIndex}:"),
                        "Every slot says which turn it stands for, so the row still means resolved " +
                        "turns without a caption over it.");
                    Assert.That(
                        slot.AssistiveLabel,
                        slot.IsOccupied
                            ? Does.Contain(slot.PlayedCard.DisplayName)
                            : Does.Contain("empty"),
                        "and it names the card that resolved there, or says the turn is empty.");
                }
            }

            // Milestone 8.11b, in the runtime scene: the row is card icons rather than numbers, and
            // resting on one reveals that card's whole face through the rail's single preview. The
            // search is scoped to the rails on purpose - the tray beside them is full of printed
            // numbers, and one of them is legitimately an Initiative of 3.
            var railView = runtime.HeroOverviewsView;
            var railText = Descendants(railView.Root)
                .OfType<UnityEngine.UIElements.Label>()
                .Select(label => label.text)
                .ToArray();
            foreach (var ordinal in new[] { "1", "2", "3", "4" })
            {
                Assert.That(railText, Has.No.Member(ordinal),
                    $"The slot ordinal '{ordinal}' is off the runtime rail too.");
            }

            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-turn-slot"),
                Is.EqualTo(RoundResolutionAuthority.TurnsPerRound * rails.Length));

            var occupied = resolved.TurnSlots.Single(slot => slot.IsOccupied);
            railView.ForwardTurnSlotHighlight(occupied);
            yield return null;
            var preview = railView.HoverPreviewRoot;
            Assert.That(preview.style.display.value,
                Is.EqualTo(UnityEngine.UIElements.DisplayStyle.Flex));
            Assert.That(preview.parent, Is.SameAs(hud.RootElement));
            Assert.That(preview.parent.Children().Last(), Is.SameAs(preview),
                "A rail hover face is the HUD's top painted branch, so Director and status cannot " +
                "cut across it.");
            Assert.That(
                Descendants(preview).Prepend(preview)
                    .All(element => element.pickingMode == PickingMode.Ignore),
                Is.True,
                "The top-layer face reads through the pointer without creating another blocked area.");
            Assert.That(
                Descendants(preview).Count(element => element.name == "gd-card-preview"),
                Is.EqualTo(1),
                "One face, from the tray's own builder.");

            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-discard-card"),
                Is.EqualTo(railView.Model.Heroes.Sum(hero => hero.DiscardCards.Count)),
                "The pile is one icon per projected card, and invents none when it is empty.");

            var pile = railView.Model.Heroes.FirstOrDefault(hero => hero.DiscardCards.Count > 0);
            if (pile != null)
            {
                railView.ForwardDiscardCardHighlight(pile.DiscardCards[0]);
                yield return null;
                Assert.That(railView.HighlightedTurnSlot, Is.Null,
                    "Moving onto a discard icon replaces the face rather than adding one.");
                Assert.That(
                    Descendants(preview).Count(element => element.name == "gd-card-preview"),
                    Is.EqualTo(1));
            }

            railView.ForwardDiscardCardHighlight(null);
            railView.ForwardTurnSlotHighlight(null);
            yield return Capture("tmp/milestone-8-7a-resolved.png");
        }

        private static void AdvancePastTheCurrentTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var turnIndex = runtime.TurnIndex;
            for (var guard = 0; guard < 6; guard++)
            {
                if (runtime.Phase != RoundResolutionPhase.ResolvingTurn ||
                    runtime.TurnIndex != turnIndex)
                {
                    return;
                }

                if (!runtime.SubmitEndTurn())
                {
                    return;
                }
            }
        }

        private static void AssertOneCardIsMarkedActive(
            ConfiguredMatchHudRenderer hud, string because)
        {
            var slots = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-card-slot")
                .ToArray();
            Assert.That(
                slots.Count(slot =>
                    slot.style.borderTopColor.value == MatchHudTheme.ActiveEffectAccent),
                Is.EqualTo(1),
                $"Exactly one card wears the Active-effect outline {because}.");
            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-card-active"),
                Is.EqualTo(1),
                $"and it carries the word ACTIVE {because}, so the state is never colour alone.");
        }

        /// <summary>
        /// p.15's two answers for one upgraded card, both at that card's own colour, from Wasp's printed
        /// content: taking either candidate sets the other aside, and the item that arrives is the icon
        /// printed on the card that was not taken.
        /// </summary>
        private static IEnumerable<SystemTransitionOptionViewModel> UpgradeOptions(
            CardDefinitionId upgradedId,
            CardDefinitionId firstCandidateId,
            CardDefinitionId secondCandidateId)
        {
            var upgraded = WaspCardContent.Definition(upgradedId);
            foreach (var pair in new[]
                {
                    new[] { firstCandidateId, secondCandidateId },
                    new[] { secondCandidateId, firstCandidateId }
                })
            {
                var taken = WaspCardContent.Definition(pair[0]);
                var setAside = WaspCardContent.Definition(pair[1]);
                yield return new SystemTransitionOptionViewModel(
                    $"UPGRADE {upgraded.DisplayName} INTO {taken.DisplayName}",
                    $"SETS {setAside.DisplayName} ASIDE AS A " +
                        $"{MatchHudTheme.ChannelToken(setAside.Progression.ItemIcon)} +1 ITEM",
                    DirectorCommand.AdvanceEndOfRound(),
                    upgraded.Progression.Color,
                    new CardInstanceId($"capture-{upgradedId.Value}"),
                    CurrentRoundCards.DeckCard(
                        new CardInstanceId($"capture-{taken.Id.Value}"), taken),
                    upgraded.DisplayName,
                    setAside.Progression.ItemIcon,
                    setAside.DisplayName);
            }
        }

        private static void RenderArtworkDefenseChoices(ConfiguredMatchSceneRuntime runtime)
        {
            var hand = Hand(runtime, TeamSide.Red);
            var invocations = hand.TrayCards
                .Where(card => hand.Cards.Any(instance =>
                    instance.Id.Equals(card.CardInstanceId)))
                .OrderByDescending(card => card.Face.PrimaryActionLabel.Contains("BLOCK"))
                .Take(4)
                .Select(card => new TypedInvocationViewModel(
                    $"DISCARD {card.DisplayName.ToUpperInvariant()}\n{card.Face.ValueLine}",
                    DirectorCommand.ChooseDefenseCard(card.CardInstanceId),
                    null,
                    card))
                .ToArray();
            Assert.That(invocations, Has.Length.EqualTo(4));
            var model = runtime.RoundView.Model;
            runtime.RoundView.Render(new RoundActionViewModel(
                model.Version + 100,
                model.RoundIndex,
                model.TurnIndex,
                model.Phase,
                "Choose a Defense card.",
                System.Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                System.Array.Empty<TypedInvocationViewModel>(),
                invocations));
        }

        private static void RenderArtworkUpgradeChoices(ConfiguredMatchSceneRuntime runtime)
        {
            var current = runtime.RoundView.Model;
            var upgrade = new SystemTransitionPromptViewModel(
                GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind.LevelUpUpgrade,
                "LEVEL UP",
                "RED WASP REACHED LEVEL 2 - UPGRADE A CARD",
                "PAID 1 COIN - 1 REMAINING",
                UpgradeOptions(
                        WaspCardContent.LiftUp,
                        WaspCardContent.ControlGravity,
                        WaspCardContent.KineticRepulse)
                    .Concat(UpgradeOptions(
                        WaspCardContent.Shock,
                        WaspCardContent.Electrocute,
                        WaspCardContent.ChargedBoomerang))
                    .Concat(UpgradeOptions(
                        WaspCardContent.StopProjectiles,
                        WaspCardContent.DeflectProjectiles,
                        WaspCardContent.Telekinesis)));
            runtime.RoundView.Render(new RoundActionViewModel(
                current.Version + 200,
                current.RoundIndex,
                current.TurnIndex,
                current.Phase,
                "End of round - rulebook p.15 is waiting on an upgrade.",
                System.Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                System.Array.Empty<TypedInvocationViewModel>(),
                System.Array.Empty<TypedInvocationViewModel>(),
                systemTransitionPrompt: upgrade));
        }

        private static void RenderAllChannelItemPlaque(ConfiguredMatchSceneRuntime runtime)
        {
            var sourceModel = runtime.HeroOverviewsView.Model;
            var source = sourceModel.Heroes[0];
            var channels = new[]
            {
                PrintedValueChannel.Initiative,
                PrintedValueChannel.Attack,
                PrintedValueChannel.Defense,
                PrintedValueChannel.Movement,
                PrintedValueChannel.Range,
                PrintedValueChannel.Radius
            };
            var replacement = new HeroOverviewViewModel(
                source.HeroId,
                source.TeamSide,
                source.ContentId,
                source.HeroDisplayName,
                source.Level,
                source.Coins,
                channels.Select((channel, index) =>
                    new HeroItemBonusViewModel(channel, index == 0 ? 10 : 1)),
                source.TurnSlots,
                source.DiscardCards,
                source.IsViewed,
                source.CurrentCard,
                source.UltimateCard,
                source.HeldMarkers);
            runtime.HeroOverviewsView.Render(new HeroOverviewsViewModel(
                sourceModel.Version,
                sourceModel.Heroes.Select(hero =>
                    hero.HeroId.Equals(source.HeroId) ? replacement : hero)));
        }

        private static void AssertCompleteFaceArtwork(
            VisualElement root,
            string faceName,
            int expectedFaces,
            bool expectedArtwork,
            string surface)
        {
            var faces = Descendants(root)
                .Where(element => element.name == faceName)
                .ToArray();
            Assert.That(faces, Has.Length.EqualTo(expectedFaces),
                $"{surface} was not reached at its expected complete-face count.");
            foreach (var face in faces)
            {
                Assert.That(
                    Descendants(face).Count(element => element.name == "gd-card-artwork"),
                    Is.EqualTo(expectedArtwork ? 1 : 0),
                    $"{surface} did not take the selected artwork/fallback arm.");
                Assert.That(
                    Descendants(face).Count(element => element.name == "gd-card-frame"),
                    Is.EqualTo(expectedArtwork ? 0 : 1),
                    $"{surface} did not choose exactly one visible face arm.");
                Assert.That(
                    Descendants(face).Count(element => element.name == "gd-card-rules"),
                    Is.EqualTo(expectedArtwork ? 0 : 1),
                    $"{surface} did not keep the generated rules exclusive to fallback.");
                Assert.That(
                    face.style.width.value.value,
                    Is.EqualTo(CardFootprint.StandardWidth),
                    $"{surface} changed the shared card width.");
                Assert.That(
                    face.style.height.value.value,
                    Is.EqualTo(expectedArtwork
                        ? CardFootprint.ArtworkHeight
                        : CardFootprint.StandardHeight),
                    $"{surface} changed its selected face-arm height.");
                if (expectedArtwork)
                {
                    var artwork = Descendants(face).OfType<Image>()
                        .Single(element => element.name == "gd-card-artwork");
                    Assert.That(artwork.scaleMode, Is.EqualTo(ScaleMode.ScaleToFit));
                    Assert.That(face.style.borderTopWidth.value, Is.Zero,
                        $"{surface} put a permanent frame around prepared artwork.");
                }
                Assert.That(face.focusable,
                    Is.EqualTo(faceName == "gd-card-choice"),
                    "The artwork child adds no focus stop and the outer interaction surface keeps its " +
                    "existing keyboard role.");
            }
        }

        private static IEnumerator Capture(string relativePath)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                yield break;
            }

            var path = Path.GetFullPath(relativePath);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
            yield return null;
            Assert.That(File.Exists(path), Is.True,
                $"The targeted Game View visual evidence must be captured at {relativePath}.");
        }

        private static System.Action SelectFixedGameViewResolution(int width, int height)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var requestedLabel = $"8.11d {width}x{height}";
            var gameViewType = System.Type.GetType(
                "UnityEditor.GameView,UnityEditor" /* absence-intended: Editor-only capture API */) ??
                throw new System.InvalidOperationException("Unity Editor Game View type is unavailable.");
            var sizesType = System.Type.GetType(
                "UnityEditor.GameViewSizes,UnityEditor" /* absence-intended: Editor-only capture API */) ??
                throw new System.InvalidOperationException("Unity Editor Game View sizes are unavailable.");
            var sizeType = System.Type.GetType(
                "UnityEditor.GameViewSize,UnityEditor" /* absence-intended: Editor-only capture API */) ??
                throw new System.InvalidOperationException("Unity Editor Game View size type is unavailable.");
            var sizeKindType = System.Type.GetType(
                "UnityEditor.GameViewSizeType,UnityEditor" /* absence-intended: Editor-only capture API */) ??
                throw new System.InvalidOperationException("Unity Editor Game View size kind is unavailable.");

            var sizes = sizesType.GetProperty("instance", flags)?.GetValue(null) ??
                throw new System.InvalidOperationException("Unity Editor Game View sizes have no instance.");
            var group = sizesType.GetProperty("currentGroup", flags)?.GetValue(sizes) ??
                throw new System.InvalidOperationException("Unity Editor Game View has no current size group.");
            var getBuiltinCount = group.GetType().GetMethod("GetBuiltinCount", flags) ??
                throw new System.InvalidOperationException("Game View built-in size count is unavailable.");
            var getCustomCount = group.GetType().GetMethod("GetCustomCount", flags) ??
                throw new System.InvalidOperationException("Game View custom size count is unavailable.");
            var getSize = group.GetType().GetMethod("GetGameViewSize", flags) ??
                throw new System.InvalidOperationException("Game View size lookup is unavailable.");
            var addCustom = group.GetType().GetMethod("AddCustomSize", flags) ??
                throw new System.InvalidOperationException("Game View custom-size insertion is unavailable.");
            var removeCustom = group.GetType().GetMethod("RemoveCustomSize", flags) ??
                throw new System.InvalidOperationException("Game View custom-size removal is unavailable.");
            var widthProperty = sizeType.GetProperty("width", flags) ??
                throw new System.InvalidOperationException("Game View size width is unavailable.");
            var heightProperty = sizeType.GetProperty("height", flags) ??
                throw new System.InvalidOperationException("Game View size height is unavailable.");
            var kindProperty = sizeType.GetProperty("sizeType", flags) ??
                throw new System.InvalidOperationException("Game View size kind is unavailable.");
            var displayTextProperty = sizeType.GetProperty("displayText", flags) ??
                throw new System.InvalidOperationException("Game View size label is unavailable.");
            var gameViews = Resources.FindObjectsOfTypeAll(gameViewType);
            Assert.That(gameViews, Is.Not.Empty,
                "The dedicated nonbatch recipe must own a Game View to inspect.");
            var selection = gameViewType.GetProperty("selectedSizeIndex", flags) ??
                throw new System.InvalidOperationException("Game View size selection is unavailable.");
            var repaint = gameViewType.GetMethod("Repaint", flags);
            var priorSelections = gameViews
                .Select(gameView => (GameView: gameView, Index: (int)selection.GetValue(gameView)))
                .ToArray();

            // Everything above is read-only pre-state. From this point onward any insertion or
            // partial selection is rolled back here if setup fails before the cleanup is returned.
            var builtinCount = (int)getBuiltinCount.Invoke(group, null);
            var customCount = (int)getCustomCount.Invoke(group, null);
            var selectedIndex = -1;
            var customIndexToRemove = -1;
            var createdThisCall = false;

            void RestorePriorSelections()
            {
                System.Exception restoreFailure = null;
                foreach (var prior in priorSelections)
                {
                    try
                    {
                        if (prior.GameView != null)
                        {
                            selection.SetValue(prior.GameView, prior.Index);
                            repaint?.Invoke(prior.GameView, null);
                        }
                    }
                    catch (System.Exception exception)
                    {
                        restoreFailure ??= exception;
                    }
                }

                if (restoreFailure != null)
                {
                    throw new System.InvalidOperationException(
                        "Failed to restore one or more prior Game View selections.",
                        restoreFailure);
                }
            }

            void RemoveCreatedSize()
            {
                if (!createdThisCall || customIndexToRemove < 0)
                {
                    return;
                }

                var liveCustomCount = (int)getCustomCount.Invoke(group, null);
                if (customIndexToRemove >= liveCustomCount)
                {
                    createdThisCall = false;
                    return;
                }

                var candidate = getSize.Invoke(
                    group, new object[] { builtinCount + customIndexToRemove });
                var isCreatedSize = (int)widthProperty.GetValue(candidate) == width &&
                    (int)heightProperty.GetValue(candidate) == height &&
                    kindProperty.GetValue(candidate).ToString() == "FixedResolution" &&
                    displayTextProperty.GetValue(candidate).ToString() == requestedLabel;
                if (isCreatedSize)
                {
                    removeCustom.Invoke(group, new object[] { customIndexToRemove });
                }

                createdThisCall = false;
            }

            try
            {
                for (var index = 0; index < builtinCount + customCount; index++)
                {
                    var candidate = getSize.Invoke(group, new object[] { index });
                    var candidateWidth = (int)widthProperty.GetValue(candidate);
                    var candidateHeight = (int)heightProperty.GetValue(candidate);
                    var candidateKind = kindProperty.GetValue(candidate).ToString();
                    if (candidateWidth == width && candidateHeight == height &&
                        candidateKind == "FixedResolution")
                    {
                        selectedIndex = index;
                        break;
                    }
                }

                if (selectedIndex < 0)
                {
                    var fixedResolution = System.Enum.Parse(sizeKindType, "FixedResolution");
                    var constructor = sizeType.GetConstructor(
                        flags,
                        null,
                        new[] { sizeKindType, typeof(int), typeof(int), typeof(string) },
                        null) ?? throw new System.InvalidOperationException(
                        "Game View fixed-resolution constructor is unavailable.");
                    var custom = constructor.Invoke(new[]
                    {
                        fixedResolution,
                        (object)width,
                        height,
                        requestedLabel
                    });
                    customIndexToRemove = customCount;
                    // Mark the insertion attempt before reflection invokes Editor code. If that
                    // code mutates and then throws, RemoveCreatedSize can still identify and remove
                    // only the exact appended size owned by this call.
                    createdThisCall = true;
                    addCustom.Invoke(group, new[] { custom });
                    selectedIndex = builtinCount + customIndexToRemove;
                }

                foreach (var gameView in gameViews)
                {
                    selection.SetValue(gameView, selectedIndex);
                    repaint?.Invoke(gameView, null);
                }

                return () =>
                {
                    System.Exception cleanupFailure = null;
                    try
                    {
                        RestorePriorSelections();
                    }
                    catch (System.Exception exception)
                    {
                        cleanupFailure = exception;
                    }

                    try
                    {
                        RemoveCreatedSize();
                    }
                    catch (System.Exception exception)
                    {
                        cleanupFailure ??= exception;
                    }

                    if (cleanupFailure != null)
                    {
                        throw new System.InvalidOperationException(
                            "Failed to restore the Game View resolution after capture.",
                            cleanupFailure);
                    }
                };
            }
            catch
            {
                try
                {
                    RestorePriorSelections();
                }
                catch
                {
                    // Preserve the setup exception after making the best possible rollback.
                }

                try
                {
                    RemoveCreatedSize();
                }
                catch
                {
                    // Preserve the setup exception after making the best possible rollback.
                }

                throw;
            }
        }

        private static UnityEngine.UIElements.VisualElement Named(
            UnityEngine.UIElements.VisualElement root, string name) =>
            Descendants(root).FirstOrDefault(element => element.name == name);

        private static IEnumerable<UnityEngine.UIElements.VisualElement> Descendants(
            UnityEngine.UIElements.VisualElement element)
        {
            yield return element;
            foreach (var child in element.Children())
            {
                foreach (var nested in Descendants(child))
                {
                    yield return nested;
                }
            }
        }

        [UnityTest]
        public IEnumerator Scene_LoadsVisibleConfiguredHeroesAndPassiveBoardCallbacks()
        {
            yield return LoadScene();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();
            var selectors = Object.FindObjectsByType<PrimitiveHeroPointerSelector>();

            Assert.That(heroes, Has.Length.EqualTo(2));
            Assert.That(heroes.Select(hero => hero.Occupancy.HeroId.Value), Is.EquivalentTo(new[]
            {
                StagedBoardParticipants.RedHeroIdValue,
                StagedBoardParticipants.BlueHeroIdValue
            }));
            Assert.That(selectors, Has.Length.EqualTo(2));
            Assert.That(selectors, Has.All.Matches<PrimitiveHeroPointerSelector>(selector =>
                selector.GetComponent<PrimitiveHeroView>() != null &&
                selector.GetComponent<CapsuleCollider>() != null));
            Assert.That(heroes, Has.All.Matches<PrimitiveHeroView>(hero =>
                hero.GetComponent<MeshRenderer>().enabled &&
                hero.GetComponent<MeshFilter>().sharedMesh != null));
        }

        /// <summary>
        /// Milestone 8.11b. Both mirrored Wasps are drawn as the piece the project has art for, and the
        /// pointer target is the piece a player can see.
        ///
        /// <para>It asserts the same things whether or not the private token artwork is present, which
        /// is the point: the tracked project has none, and an absent face is an ordinary state rather
        /// than a fault. What must hold either way is that both heroes are in the <em>same</em> state -
        /// two identical Wasps cannot resolve one token and one prism - that the two teams never share
        /// a face, and that the collider follows whichever piece was built.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsBothMirroredWaspsAsTheSamePieceAndSizesTheirPointerTargetsToIt()
        {
            yield return LoadScene();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroes, Has.Length.EqualTo(2));
            Assert.That(heroes.Select(hero => hero.HasTokenVisual).Distinct().Count(), Is.EqualTo(1),
                "Both seats hold the same hero, so they resolve the same kind of piece or neither does.");

            var tokened = heroes[0].HasTokenVisual;
            foreach (var hero in heroes)
            {
                var filter = hero.GetComponent<MeshFilter>();
                var renderer = hero.GetComponent<MeshRenderer>();
                var collider = hero.GetComponent<CapsuleCollider>();
                Assert.That(filter.sharedMesh, Is.Not.Null);
                Assert.That(renderer.sharedMaterials.All(material => material != null), Is.True,
                    "No hero is left holding a null material by either route.");
                Assert.That(collider.radius, Is.EqualTo(hero.SelectionRadius).Within(0.0001f),
                    "The pointer target is the radius of the piece that was actually built.");

                if (tokened)
                {
                    Assert.That(filter.sharedMesh.subMeshCount, Is.EqualTo(2),
                        "The token is the official face and the team rim, on one renderer.");
                    Assert.That(renderer.sharedMaterials, Has.Length.EqualTo(2));
                    Assert.That(renderer.sharedMaterials[1].mainTexture, Is.Not.Null,
                        "and the face carries this hero's own prepared token.");
                    Assert.That(hero.SelectionRadius,
                        Is.EqualTo(HeroTokenArtwork.TokenOuterRadius).Within(0.0001f));
                }
                else
                {
                    Assert.That(renderer.sharedMaterials, Has.Length.EqualTo(1),
                        "The tracked fallback is exactly the prism it always was.");
                    Assert.That(hero.SelectionRadius,
                        Is.EqualTo(PrimitiveHeroView.DefaultBaseRadius).Within(0.0001f));
                }

                // Either way the team is stated in this project's own colour, from the authoritative
                // side rather than from the picture.
                Assert.That(
                    renderer.sharedMaterials[0].color,
                    Is.EqualTo(HeroTokenArtwork.TeamRim(hero.TeamSide)));
            }

            Assert.That(
                heroes[0].GetComponent<MeshRenderer>().sharedMaterials[0].color,
                Is.Not.EqualTo(heroes[1].GetComponent<MeshRenderer>().sharedMaterials[0].color),
                "The two teams are never the same colour on the board.");
            if (tokened)
            {
                Assert.That(
                    heroes[0].GetComponent<MeshRenderer>().sharedMaterials[1].mainTexture,
                    Is.Not.SameAs(heroes[1].GetComponent<MeshRenderer>().sharedMaterials[1].mainTexture),
                    "and never the same face.");
            }

            yield return Capture("tmp/milestone-8-11b-tokens.png");
        }

        /// <summary>
        /// Milestone 8.11d replaces the hero's downward triangle with the shared fitted base ring.
        /// This keeps the old validation filter name while measuring the new contract through the
        /// configured camera: flat on the board, visible on screen, outside the current footprint,
        /// and cleared in place.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_ShowsTheHeroChoiceCueFacingTheConfiguredCamera()
        {
            yield return LoadScene();
            var hero = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(value => value.Occupancy.HeroId.Value ==
                    StagedBoardParticipants.RedHeroIdValue);
            var camera = Object.FindAnyObjectByType<Camera>();

            hero.SetTargetCue(
                BoardTargetCueState.Eligible,
                BoardTargetConsequenceIntent.Affect);
            yield return null;

            var ring = hero.TargetRing;
            Assert.That(ring, Is.Not.Null);
            Assert.That(ring.IsVisible, Is.True);
            Assert.That(hero.TargetCueState, Is.EqualTo(BoardTargetCueState.Eligible));
            Assert.That(hero.TargetConsequenceIntent,
                Is.EqualTo(BoardTargetConsequenceIntent.Affect));
            Assert.That(Vector3.Dot(ring.transform.up, Vector3.up), Is.GreaterThan(0.99f),
                "The generated ring lies on the board plane rather than standing edge-on.");
            Assert.That(ring.InnerRadius,
                Is.EqualTo(Mathf.Max(
                    hero.GetComponent<Renderer>().bounds.extents.x,
                    hero.GetComponent<Renderer>().bounds.extents.z)).Within(0.001f),
                "Its inner edge is fitted to the hero's current rendered footprint.");
            var onScreen = camera.WorldToViewportPoint(ring.transform.position);
            Assert.That(onScreen.z, Is.GreaterThan(0f));
            Assert.That(onScreen.x, Is.InRange(0f, 1f));
            Assert.That(onScreen.y, Is.InRange(0f, 1f));
            var ringBounds = ring.GetComponent<Renderer>().bounds;
            Assert.That(Mathf.Abs(
                camera.WorldToScreenPoint(ringBounds.max).x -
                camera.WorldToScreenPoint(ringBounds.min).x), Is.GreaterThan(2f));
            Assert.That(Mathf.Abs(
                camera.WorldToScreenPoint(ringBounds.max).y -
                camera.WorldToScreenPoint(ringBounds.min).y), Is.GreaterThan(2f));
            Assert.That(hero.GetComponentsInChildren<TextMesh>(true),
                Has.None.Matches<TextMesh>(value => value.text == "▼"),
                "No triangle survives beside the fitted base ring.");

            hero.SetTargetCue(BoardTargetCueState.None, BoardTargetConsequenceIntent.None);
            yield return null;
            Assert.That(ring.IsVisible, Is.False);
            Assert.That(hero.TargetRing, Is.SameAs(ring),
                "Clearing changes state on the retained cue rather than destroying it.");
        }

        /// <summary>
        /// Milestone 8.11d's Play boundary for the 8.11c identity-shaped effect candidate. The
        /// EditMode presenter fixture proves Reflect Projectiles produces this stable hero identity
        /// and Affect intent from live authority. Here the saved scene's passive board consumes that
        /// authority-shaped model directly: no test-only session or production injection seam is
        /// introduced, and moving the occupancy proves the cue follows identity rather than space.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_IdentityNamedEffectChoiceUsesNeutralHexCorners()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var baseBoard = runtime.BoardView.Model;
            var target = baseBoard.Heroes.Single(value => value.HeroId.Equals(
                runtime.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue).HeroId));
            var targetReference = BoardObjectReference.ForHero(target.HeroId);
            var cue = new PieceTargetCueViewModel(
                targetReference,
                BoardTargetCueState.Eligible,
                BoardTargetConsequenceIntent.Affect);

            RenderBoard(
                runtime,
                baseBoard,
                baseBoard.Version + 1,
                baseBoard.Heroes,
                baseBoard.Minions,
                baseBoard.PlacedTokens,
                new[] { cue });
            yield return null;

            var heroView = Object.FindObjectsByType<PrimitiveHeroView>().Single(value =>
                value.Occupancy.HeroId.Equals(target.HeroId));
            var ring = heroView.TargetRing;
            Assert.That(runtime.BoardView.Model.PieceTargetCues.Single().Target,
                Is.EqualTo(targetReference));
            Assert.That(heroView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(ring.IsVisible, Is.False);
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(target.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect));

            var occupied = new HashSet<HexCoordinate>(baseBoard.Heroes.Select(value => value.Coordinate));
            occupied.UnionWith(baseBoard.Minions.Select(value => value.Coordinate));
            occupied.UnionWith(baseBoard.PlacedTokens.Select(value => value.Coordinate));
            var movedTo = runtime.BoardView.TileViews.First(value =>
                value.IsPlayable && !occupied.Contains(value.Coordinate)).Coordinate;
            var movedHeroes = baseBoard.Heroes.Select(value => value.HeroId.Equals(target.HeroId)
                    ? new HeroOccupancy(value.HeroId, movedTo)
                    : value)
                .ToArray();
            RenderBoard(
                runtime,
                baseBoard,
                baseBoard.Version + 2,
                movedHeroes,
                baseBoard.Minions,
                baseBoard.PlacedTokens,
                new[] { cue });
            yield return null;

            Assert.That(heroView.Occupancy.Coordinate, Is.EqualTo(movedTo));
            Assert.That(heroView.TargetRing, Is.SameAs(ring));
            Assert.That(ring.IsVisible, Is.False);
            Assert.That(heroView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(movedTo)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect),
                "The corner cue follows the stable HeroId to its rebuilt occupancy.");

            runtime.BoardPresenter.Refresh();
            yield return null;
            Assert.That(heroView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(ring.IsVisible, Is.False,
                "Returning to the live authority projection clears the hand-rendered boundary model.");
        }

        /// <summary>
        /// The milestone's dedicated 2048x1152 Game View evidence. It renders an authority-shaped
        /// representative cluster into the saved passive board so all three piece classes and their
        /// current footprints,
        /// both palette families, all three typed intents, eligible and selected construction, neutral
        /// keylines, and a destination fill can be judged in one frame.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_CapturesTargetRingsForGameViewInspection()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                Assert.That(UnityEngine.Application.isBatchMode, Is.True,
                    "The screenshot branch is exercised by the dedicated nonbatch recipe.");
                yield break;
            }

            var restoreGameView = SelectFixedGameViewResolution(2048, 1152);
            try
            {
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2048 || Screen.height != 1152);
                     frame++)
                {
                    yield return null;
                }

                Assert.That(Screen.width, Is.EqualTo(2048));
                Assert.That(Screen.height, Is.EqualTo(1152));
                yield return LoadScene();
                Assert.That(Screen.width, Is.EqualTo(2048));
                Assert.That(Screen.height, Is.EqualTo(1152));

            var root = Root();
            var runtime = root.Runtime;
            var baseBoard = runtime.BoardView.Model;
            var camera = Object.FindAnyObjectByType<Camera>();
            var availableTiles = runtime.BoardView.TileViews
                .Where(value => value.IsPlayable)
                .ToList();

            HexTileView TakeNearest(float viewportX, float viewportY)
            {
                var picked = availableTiles
                    .OrderBy(value =>
                    {
                        var viewport = camera.WorldToViewportPoint(value.transform.position);
                        return Mathf.Pow(viewport.x - viewportX, 2f) +
                            Mathf.Pow(viewport.y - viewportY, 2f);
                    })
                    .First();
                availableTiles.Remove(picked);
                return picked;
            }

            var eligibleHeroAt = TakeNearest(0.47f, 0.72f).Coordinate;
            var selectedHeroAt = TakeNearest(0.61f, 0.72f).Coordinate;
            var eligibleMinionAt = TakeNearest(0.47f, 0.58f).Coordinate;
            var selectedMinionAt = TakeNearest(0.61f, 0.58f).Coordinate;
            var tokenAt = TakeNearest(0.76f, 0.72f).Coordinate;
            var destination = TakeNearest(0.76f, 0.58f).Coordinate;

            var heroes = baseBoard.Heroes.OrderBy(value => value.HeroId.Value).ToArray();
            Assert.That(heroes, Has.Length.EqualTo(2));
            var captureHeroes = new[]
            {
                new HeroOccupancy(heroes[0].HeroId, eligibleHeroAt),
                new HeroOccupancy(heroes[1].HeroId, selectedHeroAt)
            };
            var minions = baseBoard.Minions
                .OrderBy(value => value.MinionId.Value, System.StringComparer.Ordinal)
                .Take(2)
                .ToArray();
            var captureMinions = new[]
            {
                minions[0].WithPosition(default, eligibleMinionAt),
                minions[1].WithPosition(default, selectedMinionAt)
            };
            var token = new PlacedToken(
                new TokenInstanceId("8-11d-capture-token"),
                SyntheticComponentDefinitions.HazardDefinition,
                heroes[0].HeroId,
                heroes[0].HeroId,
                new CardInstanceId("8-11d-capture-source"),
                tokenAt);
            var cues = new[]
            {
                new PieceTargetCueViewModel(
                    BoardObjectReference.ForHero(heroes[0].HeroId),
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Affect),
                new PieceTargetCueViewModel(
                    BoardObjectReference.ForHero(heroes[1].HeroId),
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Attack),
                new PieceTargetCueViewModel(
                    BoardObjectReference.ForMinion(minions[0].MinionId),
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Removal),
                new PieceTargetCueViewModel(
                    BoardObjectReference.ForMinion(minions[1].MinionId),
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Removal),
                new PieceTargetCueViewModel(
                    BoardObjectReference.ForToken(token.Id),
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Affect)
            };

            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 1,
                captureHeroes,
                captureMinions,
                new[] { destination },
                new[] { heroes[1].HeroId },
                new[] { minions[0].MinionId },
                heroes[0].HeroId,
                placedTokens: new[] { token },
                pieceTargetCues: cues));
            yield return null;
            yield return new WaitForEndOfFrame();

            var rings = Object.FindObjectsByType<BoardTargetRing>()
                .Where(value => value.IsVisible)
                .ToArray();
            Assert.That(rings, Is.Empty,
                "Configured play uses no fitted piece ring for any board-choice family.");
            Assert.That(runtime.BoardView.TokenViews.Single(value =>
                value.BoundTokenId.Equals(token.Id)).TargetRing.IsVisible, Is.False);
            var movementCorner = runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(destination)).CornerHighlight;
            Assert.That(movementCorner.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(movementCorner.VisibleMarkCount, Is.EqualTo(6),
                "The same six fixed marks carry every board-choice family.");
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(eligibleHeroAt)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(selectedHeroAt)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(selectedMinionAt)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(tokenAt)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(runtime.BoardView.TileViews.Where(value => value.IsPlayable),
                Has.All.Matches<HexTileView>(value => !value.IsHighlighted));
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(eligibleMinionAt)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.None.Matches<TextMesh>(value => value.text == "▼"));

                yield return Capture("tmp/8-11d-target-rings.png");
            }
            finally
            {
                restoreGameView();
            }
        }

        /// <summary>Milestone 8.30h's two-resolution hands-on palette and strength evidence.</summary>
        [UnityTest]
        public IEnumerator Scene_CapturesMovementAndAttackCornerShineAtBothSupportedResolutions()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                Assert.That(UnityEngine.Application.isBatchMode, Is.True,
                    "The screenshot branch is exercised by the dedicated nonbatch recipe.");
                yield break;
            }

            var restoreFirst = SelectFixedGameViewResolution(2560, 1440);
            System.Action restoreSecond = null;
            try
            {
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2560 || Screen.height != 1440);
                     frame++) yield return null;
                Assert.That(Screen.width, Is.EqualTo(2560));
                Assert.That(Screen.height, Is.EqualTo(1440));
                yield return LoadOfficialSixPlayerScene();

                var root = Root();
                var runtime = root.Runtime;
                var baseBoard = runtime.BoardView.Model;
                var camera = Object.FindAnyObjectByType<Camera>();
                var occupied = new HashSet<HexCoordinate>(
                    baseBoard.Heroes.Select(value => value.Coordinate)
                        .Concat(baseBoard.Minions.Select(value => value.Coordinate)));
                var available = runtime.BoardView.TileViews
                    .Where(value => value.IsPlayable && !occupied.Contains(value.Coordinate))
                    .OrderBy(value => Vector2.Distance(
                        camera.WorldToViewportPoint(value.transform.position),
                        new Vector2(0.58f, 0.58f)))
                    .Take(6)
                    .ToArray();
                Assert.That(available, Has.Length.EqualTo(6));
                var movementAt = available[0].Coordinate;
                var heroAt = available[1].Coordinate;
                var minionAt = available[2].Coordinate;
                var fastTravelAt = available[3].Coordinate;
                var respawnAt = available[4].Coordinate;
                var effectAt = available[5].Coordinate;
                var hero = baseBoard.Heroes[0];
                var minion = baseBoard.Minions[0];
                var heroes = baseBoard.Heroes
                    .Select(value => value.HeroId.Equals(hero.HeroId)
                        ? new HeroOccupancy(value.HeroId, heroAt)
                        : value)
                    .ToArray();
                var minions = baseBoard.Minions
                    .Select(value => value.MinionId.Equals(minion.MinionId)
                        ? value.WithPosition(default, minionAt)
                        : value)
                    .ToArray();
                var attackCues = new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(hero.HeroId),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Attack),
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForMinion(minion.MinionId),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Removal)
                };
                runtime.BoardView.Render(new BoardInteractionViewModel(
                    baseBoard.Version + 1,
                    heroes,
                    minions,
                    new[] { movementAt },
                    new[] { hero.HeroId },
                    System.Array.Empty<MinionId>(),
                    hero.HeroId,
                    fastTravelDestinations: new[] { fastTravelAt },
                    pieceTargetCues: attackCues,
                    effectChoiceDestinations: new[] { effectAt },
                    heroRespawnDestinations: new[] { respawnAt }));
                yield return null;
                yield return new WaitForEndOfFrame();
                runtime.BoardView.SetHoveredCoordinate(heroAt);

                var movement = available[0].CornerHighlight;
                var heroAttack = available[1].CornerHighlight;
                var minionAttack = available[2].CornerHighlight;
                var fastTravel = available[3].CornerHighlight;
                var respawn = available[4].CornerHighlight;
                var effect = available[5].CornerHighlight;
                Assert.That(movement.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
                Assert.That(fastTravel.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
                Assert.That(heroAttack.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
                Assert.That(minionAttack.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
                Assert.That(respawn.Kind, Is.EqualTo(HexCornerHighlightKind.Respawn));
                Assert.That(effect.Kind, Is.EqualTo(HexCornerHighlightKind.Effect));
                Assert.That(new[] { movement, fastTravel, heroAttack, minionAttack, respawn, effect },
                    Has.All.Matches<HexCornerHighlight>(value => value.VisibleMarkCount == 6));
                Assert.That(available, Has.All.Matches<HexTileView>(value => !value.IsHighlighted));
                Assert.That(runtime.BoardView.MinionViews.Single(value =>
                    value.BoundMinionId.Equals(minion.MinionId)).TargetRing.IsVisible, Is.False);
                Assert.That(heroAttack.IsHovered, Is.True);
                Assert.That(movement.IsHovered, Is.False);
                var stableBounds = heroAttack.LocalBounds;
                yield return Capture("tmp/8-30h-corner-shine-2560x1440.png");

                restoreSecond = SelectFixedGameViewResolution(2048, 1152);
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2048 || Screen.height != 1152);
                     frame++) yield return null;
                Assert.That(Screen.width, Is.EqualTo(2048));
                Assert.That(Screen.height, Is.EqualTo(1152));
                runtime.BoardView.SetHoveredCoordinate(heroAt);
                Assert.That(heroAttack.LocalBounds, Is.EqualTo(stableBounds));
                yield return Capture("tmp/8-30h-corner-shine-2048x1152.png");
            }
            finally
            {
                restoreSecond?.Invoke();
                restoreFirst();
            }
        }

        /// <summary>Milestone 8.30i's target-hover contribution coexistence evidence.</summary>
        [UnityTest]
        public IEnumerator Scene_CapturesAttackTargetDefenseContributionsAtBothSupportedResolutions()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                Assert.That(UnityEngine.Application.isBatchMode, Is.True,
                    "The screenshot branch is exercised by the dedicated nonbatch recipe.");
                yield break;
            }

            var restoreFirst = SelectFixedGameViewResolution(2560, 1440);
            System.Action restoreSecond = null;
            PrimitiveHeroPointerInput pointerInput = null;
            var pointerInputWasEnabled = false;
            try
            {
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2560 || Screen.height != 1440);
                     frame++) yield return null;
                Assert.That(Screen.width, Is.EqualTo(2560));
                Assert.That(Screen.height, Is.EqualTo(1440));
                yield return LoadOfficialSixPlayerScene();

                var runtime = Root().Runtime;
                pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
                Assert.That(pointerInput, Is.Not.Null);
                pointerInputWasEnabled = pointerInput.enabled;
                pointerInput.enabled = false;
                var baseBoard = runtime.BoardView.Model;
                var camera = Object.FindAnyObjectByType<Camera>();
                var occupied = new HashSet<HexCoordinate>(
                    baseBoard.Heroes.Select(value => value.Coordinate)
                        .Concat(baseBoard.Minions.Select(value => value.Coordinate)));
                var available = runtime.BoardView.TileViews
                    .Where(value => value.IsPlayable && !occupied.Contains(value.Coordinate))
                    .OrderBy(value => Vector2.Distance(
                        camera.WorldToViewportPoint(value.transform.position),
                        new Vector2(0.58f, 0.58f)))
                    .Take(3)
                    .ToArray();
                Assert.That(available, Has.Length.EqualTo(3));

                var hero = baseBoard.Heroes[0];
                var supporting = baseBoard.Minions[0];
                var hindering = baseBoard.Minions[1];
                var heroAt = available[0].Coordinate;
                var supportingAt = available[1].Coordinate;
                var hinderingAt = available[2].Coordinate;
                var heroes = baseBoard.Heroes.Select(value => value.HeroId.Equals(hero.HeroId)
                    ? new HeroOccupancy(value.HeroId, heroAt)
                    : value).ToArray();
                var minions = baseBoard.Minions.Select(value =>
                    value.MinionId.Equals(supporting.MinionId)
                        ? value.WithPosition(default, supportingAt)
                        : value.MinionId.Equals(hindering.MinionId)
                            ? value.WithPosition(default, hinderingAt)
                            : value).ToArray();
                ValueContributionProjection Contribution(MinionId minionId, int amount) =>
                    new ValueContributionProjection(
                        new ValueContribution(
                            ValueContributionKind.MinionDefenseModifier,
                            minionId,
                            PrintedValueChannel.Defense,
                            amount),
                        minionId.Value);
                var preview = new AttackTargetDefensePreviewProjection(
                    hero.HeroId,
                    new[]
                    {
                        Contribution(supporting.MinionId, 1),
                        Contribution(hindering.MinionId, -1)
                    });

                runtime.BoardView.Render(new BoardInteractionViewModel(
                    baseBoard.Version + 1,
                    heroes,
                    minions,
                    System.Array.Empty<HexCoordinate>(),
                    new[] { hero.HeroId },
                    new[] { supporting.MinionId },
                    hero.HeroId,
                    attackTargetDefensePreviews: new[] { preview }));
                runtime.BoardView.SetHoveredCoordinate(heroAt);
                yield return null;
                yield return new WaitForEndOfFrame();

                var target = available[0].CornerHighlight;
                var overlappingContributor = available[1].CornerHighlight;
                var contributionOnly = available[2].CornerHighlight;
                Assert.That(target.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
                Assert.That(target.IsHovered, Is.True);
                Assert.That(overlappingContributor.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
                Assert.That(overlappingContributor.ContributionKind,
                    Is.EqualTo(HexCornerHighlightKind.DefensePositive));
                Assert.That(overlappingContributor.VisibleCoreMaterialColor,
                    Is.EqualTo(HexCornerHighlightTemplate.DefensePositiveColor));
                Assert.That(contributionOnly.Kind,
                    Is.EqualTo(HexCornerHighlightKind.DefenseNegative));
                Assert.That(new[] { target, overlappingContributor, contributionOnly },
                    Has.All.Matches<HexCornerHighlight>(value => value.VisibleMarkCount == 6));
                yield return Capture("tmp/8-30i-defense-contributions-2560x1440.png");

                restoreSecond = SelectFixedGameViewResolution(2048, 1152);
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2048 || Screen.height != 1152);
                     frame++) yield return null;
                Assert.That(Screen.width, Is.EqualTo(2048));
                Assert.That(Screen.height, Is.EqualTo(1152));
                runtime.BoardView.SetHoveredCoordinate(heroAt);
                yield return new WaitForEndOfFrame();
                Assert.That(overlappingContributor.ContributionKind,
                    Is.EqualTo(HexCornerHighlightKind.DefensePositive));
                Assert.That(contributionOnly.Kind,
                    Is.EqualTo(HexCornerHighlightKind.DefenseNegative));
                yield return Capture("tmp/8-30i-defense-contributions-2048x1152.png");
            }
            finally
            {
                if (pointerInput != null) pointerInput.enabled = pointerInputWasEnabled;
                restoreSecond?.Invoke();
                restoreFirst();
            }
        }

        [UnityTest]
        public IEnumerator Scene_RoutesConfiguredMinionAttackCuePointerAndRemovalThroughFeatureBoundary()
        {
            yield return RunConfiguredRangedAttackPointerScenario(false);
        }

        [UnityTest]
        public IEnumerator Scene_HeroAttackTargetClickWithBlockAndMinionDefenseAdvancesToDefenseWithoutEngineFault()
        {
            yield return RunConfiguredRangedAttackPointerScenario(true);
        }

        private static IEnumerator RunConfiguredRangedAttackPointerScenario(
            bool continueThroughHeroDefenseRegression)
        {
            // Milestone 8.20 named the mirror rather than taking whatever the default arm composes.
            // The hero-Defense half of this scenario ends on p.14's Block beside numeric choices, and
            // the defender has to be holding one; the default arm now deals Arien on Blue, whose three
            // Duelist cards print p.14's other Defense shape and no Block at all.
            yield return continueThroughHeroDefenseRegression ? LoadMirrorScene() : LoadShowcaseScene();
            var root = Root();
            var runtime = root.Runtime;
            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (!continueThroughHeroDefenseRegression)
            {
                var transientSetupField = ReflectedMember.RequiredInstanceField(
                    typeof(StandardMinionView),
                    "occupancy");
                foreach (var configuredMinion in Object.FindObjectsByType<StandardMinionView>())
                {
                    transientSetupField.SetValue(configuredMinion, null);
                }

                mapView.Rebuild(mapView.RenderedDefinition, runtime.Setup);
            }
            yield return null;

            var reboundViews = Object.FindObjectsByType<StandardMinionView>();
            Assert.That(reboundViews, Has.Length.EqualTo(12));
            Assert.That(runtime.BoardView.MinionViews, Is.EquivalentTo(reboundViews));
            Assert.That(pointerInput.MinionSelectors.Select(value =>
                value.GetComponent<StandardMinionView>()), Is.EquivalentTo(reboundViews));
            var redHeroId = new GuardsDigital.Core.Units.Heroes.HeroId(
                StagedBoardParticipants.RedHeroIdValue);

            // Reserved before the approach turns rather than looked for after them. Those turns spend
            // Red's highest cards, and since milestone 6.5b converted Red Bombard the team prints
            // exactly one Ranged Attack - so the card this case exists to play would otherwise be the
            // third card the approach burns.
            var attackCard = Hand(runtime, TeamSide.Red).Cards
                .Where(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).ActionDefinitions.Any(action =>
                        action.Kind == GuardsDigital.Core.Cards.ActionKind.Attack &&
                        (continueThroughHeroDefenseRegression ||
                            action.AttackRange == AttackRangeKind.Ranged)))
                .OrderByDescending(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                .First();
            var reservedNumericDefenseCard = Hand(runtime, TeamSide.Blue).Cards
                .Where(card => runtime.Definitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference))
                    .ActionDefinitions.Any(action =>
                        action.Kind == GuardsDigital.Core.Cards.ActionKind.Defense &&
                        action.DefenseResolution == PrintedDefenseResolution.Value))
                .OrderByDescending(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                .First();

            var approachTurnCount = continueThroughHeroDefenseRegression ? 8 : 3;
            for (var approachTurn = 0; approachTurn < approachTurnCount; approachTurn++)
            {
                if (continueThroughHeroDefenseRegression)
                {
                    while (runtime.Phase == RoundResolutionPhase.EndOfRound)
                    {
                        Assert.That(runtime.ContinueEndOfRound(), Is.True,
                            "Each bounded approach round must close before the next one starts.");
                    }

                    var attackerCoordinate = runtime.Heroes.Single(value =>
                        value.HeroId.Equals(redHeroId)).Coordinate;
                    var defenderCoordinate = runtime.Heroes.Single(value =>
                        !value.HeroId.Equals(redHeroId)).Coordinate;
                    var friendlyMinionDistance = runtime.BoardView.Model.Minions
                        .Where(value => value.TeamSide == TeamSide.Blue &&
                            value.MinionType == StandardMinionType.Melee)
                        .Min(value => HexDistance(defenderCoordinate, value.Coordinate));
                    if (approachTurn >= 4 &&
                        HexDistance(attackerCoordinate, defenderCoordinate) <= 1 &&
                        friendlyMinionDistance <= 1)
                    {
                        break;
                    }
                }

                // The approach card is named by the printed action it has to perform rather than by
                // being the top of the band. Milestone 6.2d needed that, because the scene was dealt
                // milestone 6.2's Cache card as a sixth - its only printed action a Skill, and its
                // Initiative above every ordinary Red card - so "Red's highest" picked a card with no
                // Movement to approach with. The five-card scene deals no such card any more, and the
                // filter stays because naming the action a fixture needs is the right way round: it
                // survives whatever a later conversion does to the top of the band.
                var red = Hand(runtime, TeamSide.Red).Cards
                    .Where(card => (!card.Id.Equals(attackCard.Id) ||
                            (continueThroughHeroDefenseRegression && approachTurn % 4 == 3)) &&
                        runtime.Definitions.Single(definition =>
                            definition.Reference.Equals(card.DefinitionReference))
                        .ActionDefinitions.Any(action =>
                            action.Kind == GuardsDigital.Core.Cards.ActionKind.Movement))
                    .OrderByDescending(card => runtime.Definitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                    .First();
                var blue = Hand(runtime, TeamSide.Blue).Cards
                    .Where(card => !continueThroughHeroDefenseRegression ||
                        runtime.Definitions.Single(definition =>
                                definition.Reference.Equals(card.DefinitionReference))
                            .ActionDefinitions.All(action =>
                                action.Kind != GuardsDigital.Core.Cards.ActionKind.Defense ||
                                action.DefenseResolution != PrintedDefenseResolution.Block))
                    .Where(card => !continueThroughHeroDefenseRegression || approachTurn < 4 ||
                        !card.Id.Equals(reservedNumericDefenseCard.Id))
                    .OrderBy(card => runtime.Definitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                    .First();
                Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
                Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
                if (runtime.PendingInitiativeChoice != null)
                {
                    Assert.That(runtime.SubmitInitiativeChoice(red.Id), Is.True);
                }

                if (!runtime.SelectedActor.ActorId.Equals(redHeroId))
                {
                    CompleteBlueApproachTurn(runtime,
                        continueThroughHeroDefenseRegression && approachTurn >= 4,
                        "The earlier opposing actor should complete before the Red approach action.");
                }

                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
                var movement = runtime.RoundView.Model.Actions.Single(value =>
                    ActionKind(runtime, value.Command.ActionDefinitionId) ==
                    GuardsDigital.Core.Cards.ActionKind.Movement);
                Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
                while (runtime.PendingMovementChoice != null)
                {
                    var blueMinions = runtime.BoardView.Model.Minions
                        .Where(value => value.TeamSide == TeamSide.Blue &&
                            value.MinionType != StandardMinionType.Heavy)
                        .ToArray();
                    var blueHeroCoordinate = runtime.Heroes.Single(value =>
                        runtime.Setup.Heroes.Single(hero =>
                            hero.HeroId.Equals(value.HeroId)).TeamSide == TeamSide.Blue).Coordinate;
                    var destination = runtime.PendingMovementChoice.LegalDestinations
                        .OrderBy(value => continueThroughHeroDefenseRegression
                            ? HexDistance(value.Destination, blueHeroCoordinate)
                            : blueMinions.Min(minion =>
                                HexDistance(value.Destination, minion.Coordinate)))
                        .ThenByDescending(value => value.Cost)
                        .First().Destination;
                    Assert.That(runtime.BoardPresenter.SubmitDestination(destination), Is.True);
                }

                if (runtime.IsTurnActive)
                {
                    Assert.That(runtime.SelectedActor.ActorId, Is.Not.EqualTo(redHeroId));
                    CompleteBlueApproachTurn(runtime,
                        continueThroughHeroDefenseRegression && approachTurn >= 4,
                        "The later opposing actor should complete after the Red approach action.");
                }
            }

            while (runtime.Phase == RoundResolutionPhase.EndOfRound)
            {
                Assert.That(runtime.ContinueEndOfRound(), Is.True,
                    "The fourth approach turn must close its round before the reserved Attack is replayed.");
            }

            var finalBlueCard = Hand(runtime, TeamSide.Blue).Cards
                .Where(card => !continueThroughHeroDefenseRegression ||
                    runtime.Definitions.Single(definition =>
                            definition.Reference.Equals(card.DefinitionReference))
                        .ActionDefinitions.All(action =>
                            action.Kind != GuardsDigital.Core.Cards.ActionKind.Defense ||
                            action.DefenseResolution != PrintedDefenseResolution.Block))
                .OrderBy(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                .First();
            Assert.That(runtime.SelectCommitmentCard(attackCard.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(finalBlueCard.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            if (runtime.PendingInitiativeChoice != null)
            {
                Assert.That(runtime.SubmitInitiativeChoice(attackCard.Id), Is.True);
            }

            if (!runtime.SelectedActor.ActorId.Equals(redHeroId))
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "The earlier opposing actor should complete by Hold before the Red Attack.");
            }

            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
            var attack = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            var pendingTargetSnapshot = runtime.CaptureSnapshot();
            if (continueThroughHeroDefenseRegression)
            {
                goto HeroDefenseRegression;
            }

            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Is.Not.Empty);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.First();
            var minion = Object.FindObjectsByType<StandardMinionView>()
                .Single(value => value.Occupancy.MinionId.Equals(targetId));
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(minion.Occupancy.Coordinate));
            Assert.That(runtime.BoardView.Model.MinionAttackTargets, Does.Contain(targetId));
            var projectedCue = runtime.BoardView.Model.PieceTargetCues.Single(value =>
                value.Target.Equals(BoardObjectReference.ForMinion(targetId)));
            Assert.That(projectedCue.State, Is.EqualTo(BoardTargetCueState.Eligible));
            Assert.That(projectedCue.ConsequenceIntent,
                Is.EqualTo(BoardTargetConsequenceIntent.Attack),
                "Attack carries its typed blood-red intent even though this live attack will defeat " +
                "its target; it is distinct from Removal rather than predicted from the result.");
            var retainedRing = minion.TargetRing;
            Assert.That(minion.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(retainedRing.IsVisible, Is.False,
                "Configured play retires the fitted piece ring for every board choice.");
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(tile.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(minion.GetComponent<StandardMinionPointerSelector>(), Is.Not.Null);
            var commandCount = runtime.AcceptedCommands.Count;
            Assert.That(pointerInput.TryHandlePointerRay(DownRay(minion.transform.position)), Is.True);
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandCount + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseAttackTarget));
            Assert.That(runtime.AcceptedCommands.Last().MinionId, Is.EqualTo(targetId));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(attackCard.Id)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.False);
            Assert.That(minion.TargetCueState, Is.EqualTo(BoardTargetCueState.None),
                "Removal clears the cue before the retained minion view is hidden.");
            Assert.That(retainedRing.IsVisible, Is.False);
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(runtime.DiagnosticEvents.Select(value => value.EventType), Does.Contain(
                "Action.MinionDefeatRecorded"));
            Assert.That(runtime.DiagnosticEvents.Select(value => value.EventType), Does.Contain(
                "Action.MinionRewardAwarded"));
            Assert.That(runtime.DiagnosticEvents.Select(value => value.EventType), Does.Contain(
                "Action.MinionRemovalCompleted"));

            Assert.That(runtime.Undo(), Is.True);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Does.Contain(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.True);
            Assert.That(minion.TargetRing, Is.SameAs(retainedRing));
            Assert.That(retainedRing.IsVisible, Is.False);
            Assert.That(minion.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack),
                "Undo restores the projected Attack through the blood-red corner language.");

            Assert.That(runtime.Restart(), Is.True);
            yield return null;
            Assert.That(runtime.BoardView.Model.Minions, Has.Count.EqualTo(12));
            Assert.That(runtime.BoardView.MinionViews.Count, Is.EqualTo(12));
            Assert.That(pointerInput.MinionSelectors.Count, Is.EqualTo(12));
            Assert.That(runtime.BoardView.Model.PieceTargetCues, Is.Empty);
            Assert.That(runtime.BoardView.MinionViews,
                Has.All.Matches<StandardMinionView>(value =>
                    value.TargetCueState == BoardTargetCueState.None),
                "Restart rebuilds every retained minion without a stale ring.");
            Assert.That(retainedRing.IsVisible, Is.False);

            Assert.That(runtime.RestoreSnapshot(pendingTargetSnapshot), Is.True, runtime.LastOperationDetail);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Does.Contain(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.True);
            Assert.That(minion.TargetRing, Is.SameAs(retainedRing));
            Assert.That(retainedRing.IsVisible, Is.False);
            Assert.That(minion.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack),
                "and snapshot restore restores the blood-red corner cue with the choice.");
            Assert.That(pointerInput.TryHandlePointerRay(DownRay(minion.transform.position)), Is.True);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.False);
            Assert.That(minion.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(retainedRing.IsVisible, Is.False);

            if (!continueThroughHeroDefenseRegression)
            {
                yield break;
            }

        HeroDefenseRegression:
            var redCoordinate = runtime.Heroes.Single(value => value.HeroId.Equals(redHeroId)).Coordinate;
            var blueHero = runtime.Heroes.Single(value => !value.HeroId.Equals(redHeroId));
            var nearestFriendlyMinionDistance = runtime.BoardView.Model.Minions
                .Where(value => value.TeamSide == TeamSide.Blue &&
                    value.MinionType == StandardMinionType.Melee)
                .Min(value => HexDistance(blueHero.Coordinate, value.Coordinate));
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Is.Not.Empty,
                $"The verified Attack must reach the defending hero after the bounded approach; " +
                $"attacker={redCoordinate}, defender={blueHero.Coordinate}, " +
                $"distance={HexDistance(redCoordinate, blueHero.Coordinate)}.");
            var heroTargetId = runtime.PendingAttackTargetChoice.LegalTargetHeroIds.Single();

            // Named rather than found by a bare Single, so a board this scenario has driven into an
            // unexpected shape says which shape rather than throwing "no matching element". Milestone
            // 8.20 needed that once and it stays: the approach turns are long and the two seats now
            // deal different printed sets, so the position they reach is worth reporting.
            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            var hero = heroViews.FirstOrDefault(value => value.Occupancy.HeroId.Equals(heroTargetId));
            Assert.That(hero, Is.Not.Null,
                $"No hero view is bound to the projected target {heroTargetId.Value}. Views: " +
                string.Join(", ", heroViews.Select(value =>
                    $"{value.Occupancy.HeroId.Value}@{value.Occupancy.Coordinate}" +
                    $" active={value.gameObject.activeSelf}")) +
                $"; heroes on board: {string.Join(", ", runtime.Heroes.Select(value => value.HeroId.Value))}.");
            var heroRing = hero.TargetRing;
            Assert.That(hero.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(heroRing.IsVisible, Is.False);
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(hero.Occupancy.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack));
            var beforeHeroTarget = runtime.AcceptedCommands.Count;

            Physics.SyncTransforms();
            var heroPointerRay = new Ray(hero.transform.position + (Vector3.up * 10f), Vector3.down);
            var targetDefensePreview = runtime.ReadModel.PendingAttackTargetDefensePreviews
                .Single(value => value.HeroId.Equals(heroTargetId));
            Assert.That(targetDefensePreview.MinionContributions, Is.Not.Empty,
                "This configured journey must reach a hero with visible minion Defense modifiers.");
            yield return null;
            Assert.That(pointerInput.RefreshHoverFromPointerRay(heroPointerRay), Is.True);
            // The supplied ray redraws synchronously. A yield here would let Update replace it with
            // the test runner's unrelated live pointer position before the contribution is inspected.
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(beforeHeroTarget),
                "Hover previews consequences without selecting the target.");
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                    value.Coordinate.Equals(hero.Occupancy.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Ally),
                "The permanent hero hover link gives the hovered hero's warm ivory ally cue precedence over the resting attack-target cue.");
            foreach (var contribution in targetDefensePreview.MinionContributions)
            {
                var minionState = runtime.BoardView.Model.Minions.Single(value =>
                    value.MinionId.Equals(contribution.SourceMinionId));
                var contributionTile = runtime.BoardView.TileViews.Single(value =>
                    value.Coordinate.Equals(minionState.Coordinate));
                Assert.That(contributionTile.CornerHighlight.ContributionKind,
                    Is.EqualTo(contribution.Amount > 0
                        ? HexCornerHighlightKind.DefensePositive
                        : HexCornerHighlightKind.DefenseNegative));
            }
            runtime.BoardView.SetHoveredCoordinate(null);
            yield return null;
            Assert.That(runtime.BoardView.TileViews.Where(value => value.CornerHighlight != null).All(value =>
                    value.CornerHighlight.ContributionKind == HexCornerHighlightKind.None), Is.True,
                "Leaving the hero clears only the speculative contribution marks.");
            Assert.That(pointerInput.RefreshHoverFromPointerRay(heroPointerRay), Is.True);
            yield return null;
            Assert.That(pointerInput.TryHandlePointerRay(heroPointerRay), Is.True,
                "The configured Input System ray, hero collider, selector and board presenter must " +
                "claim the projected hero target.");
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(beforeHeroTarget + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseAttackTarget));
            Assert.That(runtime.AcceptedCommands.Last().HeroId, Is.EqualTo(heroTargetId));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(hero.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(heroRing.IsVisible, Is.False);
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            Assert.That(runtime.RoundView.Model.Status, Does.Contain("Incoming Attack"));

            var blockCard = runtime.Cards.Single(card =>
                card.OwnerId.Equals(heroTargetId) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles));
            Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Select(value =>
                value.SourceCardInstanceId), Does.Contain(blockCard.Id),
                "The defending Wasp must still hold the legal printed Block.");
            var blockChoice = runtime.RoundView.Model.DefenseChoices.Single(value =>
                value.Command.CardInstanceId.Equals(blockCard.Id));
            Assert.That(blockChoice.DefenseBreakdown, Is.Not.Null);
            Assert.That(blockChoice.DefenseBreakdown.Text, Does.Contain("BLOCK"));
            Assert.That(blockChoice.DefenseBreakdown.Text.ToUpperInvariant(), Does.Not.Contain("MINION"),
                "The Block remains value-free beside numeric choices with minion contributions.");
            Assert.That(runtime.BoardView.Model.MinionDefenseCues, Is.Not.Empty,
                "The player-reported mixed preview requires at least one nearby defending minion; " +
                $"nearest friendly minion distance was {nearestFriendlyMinionDistance}.");
            Assert.That(runtime.RoundView.Model.DefenseChoices.Any(value =>
                value.DefenseBreakdown != null &&
                value.DefenseBreakdown.Text.Contains("DEFENSE TOTAL:")), Is.True,
                "At least one ordinary numeric Defense is published beside the Block; the board's " +
                "nonempty signed minion cues carry their shared contribution.");
        }

        [UnityTest]
        public IEnumerator Scene_CommitsBothHandsAndResolvesHoldThroughFeaturePresenters()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            var blue = Hand(runtime, TeamSide.Blue).Cards[1];

            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.GetCommitmentStatus(red.OwnerId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            yield return null;

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.Setup.Heroes, Has.All.Matches<QuickGameHeroSetupState>(value =>
                runtime.GetCommitmentStatus(value.HeroId) == TurnCardCommitmentStatus.Revealed));
            Assert.That(runtime.RoundView.Model.EndTurnInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.PerformHold));
            var firstActor = runtime.SelectedActor.CardInstanceId;
            Assert.That(runtime.SubmitEndTurn(), Is.True);
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(firstActor)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.Not.EqualTo(firstActor));
        }

        [UnityTest]
        public IEnumerator Scene_RoutesMovementDestinationAndStopThroughBoardViewPresenter()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            CommitNextTurn(runtime);
            var actorId = runtime.SelectedActor.ActorId;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null);
            Assert.That(runtime.RoundView.Model.EndTurnInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.StopMovement));

            var destination = runtime.PendingMovementChoice.LegalDestinations
                .First(value => value.Cost == 1).Destination;
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var commandCount = runtime.AcceptedCommands.Count;
            Assert.That(pointer.TryHandlePointerRay(DownRay(tile.transform.position)), Is.True);
            Assert.That(runtime.AcceptedCommands.Count, Is.EqualTo(commandCount + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseMovementDestination));
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                Assert.That(runtime.AcceptedCommands.Last().Kind,
                    Is.EqualTo(DirectorCommandKind.StopMovement));
            }

            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(runtime.BoardView.Model.MovementDestinations, Is.Empty);
            var fastTravelDestinations = runtime.BoardView.Model.FastTravelDestinations;
            Assert.That(Object.FindObjectsByType<HexTileView>(),
                Has.All.Matches<HexTileView>(value =>
                    !value.IsHighlighted &&
                    (!fastTravelDestinations.Contains(value.Coordinate) ||
                     value.CornerHighlight.Kind == HexCornerHighlightKind.Movement)));
        }

        [UnityTest]
        public IEnumerator Scene_OffersAndRoutesFastTravelThroughConfiguredPassiveViews()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitNextTurn(runtime);
            yield return null;

            var actorId = runtime.SelectedActor.ActorId;
            var sourceCardId = runtime.SelectedActor.CardInstanceId;
            Assert.That(runtime.RoundView.Model.FastTravelOffers, Has.Count.EqualTo(1));
            Assert.That(runtime.RoundView.Model.FastTravelOffers[0].Label,
                Is.EqualTo("FAST TRAVEL"));
            Assert.That(runtime.RoundView.Model.Actions, Is.Not.Empty);
            Assert.That(runtime.BoardView.Model.FastTravelDestinations, Is.Empty,
                "Fast Travel must not be the configured board's default interaction state.");
            Assert.That(Object.FindObjectsByType<HexTileView>(),
                Has.All.Matches<HexTileView>(value =>
                    !value.IsHighlighted &&
                    (value.CornerHighlight == null ||
                     value.CornerHighlight.VisibleMarkCount == 0)));

            var fastTravel = Descendants(Root().Hud.RootElement)
                .OfType<Button>()
                .Single(control => control.name == "gd-action-replacement-chip" &&
                    control.text == "FAST TRAVEL");
            UiPlayer.Click(fastTravel);
            yield return null;

            Assert.That(runtime.BoardView.Model.FastTravelDestinations,
                Has.Count.EqualTo(runtime.RoundView.Model.FastTravelOffers[0].LegalDestinationCount));

            var destination = runtime.BoardView.Model.FastTravelDestinations[0];
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.False,
                "Fast Travel uses Movement's corner shine rather than the old whole-hex fill.");
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(tile.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var commandCount = runtime.AcceptedCommands.Count;

            Assert.That(pointer.TryHandlePointerRay(DownRay(tile.transform.position)), Is.True);
            yield return null;

            Assert.That(runtime.AcceptedCommands.Count, Is.EqualTo(commandCount + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.PerformFastTravel));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(sourceCardId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
        }

        /// <summary>
        /// Milestone 6.8a. Red Bulwark's converted Skill is the first Skill in the configured hand, so
        /// this is the scene actually reaching a paused effect frame and finishing it through the
        /// passive round view. The view forwards the projected invocations and nothing else.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_ResolvesTheConvertedSkillThroughThePassiveEffectChoicePrompt()
        {
            yield return LoadShowcaseScene();
            var runtime = Root().Runtime;
            var red = Hand(runtime, TeamSide.Red).Cards
                .Single(card => card.Id.Value == "red-bulwark-card");
            var blue = Hand(runtime, TeamSide.Blue).Cards
                .Single(card => card.Id.Value == "blue-hammer-card");
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));

            var skill = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Skill);
            Assert.That(runtime.RoundPresenter.Submit(skill), Is.True);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            Assert.That(runtime.RoundView.Model.Actions, Is.Empty,
                "The paused frame withholds the offers the authority would refuse.");
            Assert.That(runtime.RoundView.Model.EffectChoices, Has.Count.EqualTo(3),
                "Every mode of the converted Skill reaches the passive view.");

            var mode = new EffectNodeId("bulwark-defense-bonus");
            Assert.That(runtime.SubmitEffectChoiceOption(mode), Is.True);

            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(runtime.RoundView.Model.EffectChoices, Is.Empty);
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(red.Id)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.AnswerEffectChoice));
        }

        [UnityTest]
        public IEnumerator Scene_RejectsUnavailableAttackWithoutMutationThroughRoundPresenter()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitNextAttackTurn(runtime);

            // Milestone 7.8a1 changed where this Attack is drawn without changing what it does. The
            // configured heroes start far apart, so the authority now judges the Attack unstartable and
            // it keeps its label among the greyed controls instead of sitting in the row of actions that
            // can be taken. What this case has always protected is unchanged and asserted below: a start
            // naming it mutates nothing.
            var unavailable = runtime.RoundView.Model.UnavailableActions.Single(value =>
                ActionKind(runtime, value.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Attack);
            Assert.That(
                runtime.RoundView.Model.Actions.Any(value =>
                    value.Command.ActionDefinitionId.Equals(unavailable.ActionDefinitionId)),
                Is.False,
                "An action the authority judged unstartable carries no command at all.");

            var snapshot = runtime.CaptureSnapshot();
            var commandCount = runtime.AcceptedCommands.Count;

            // The stale invocation a view could still be holding from an older model.
            var stale = new TypedInvocationViewModel(
                unavailable.Label, DirectorCommand.StartAction(unavailable.ActionDefinitionId));
            Assert.That(runtime.RoundPresenter.Submit(stale), Is.False);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandCount));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.PendingDefenseChoice, Is.Null);
        }

        [UnityTest]
        public IEnumerator Scene_PassiveRoundViewPreservesFourPrintedDefenseInvocationsAndDeclineCommands()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            // Milestone 8.20 moved this from the Blue hand to the Red one. The subject is a row of
            // Defense faces that includes p.14's Block, and Arien - who deals Blue now - prints no
            // Block card at all: his three Duelist cards print a Defense value in the primary slot
            // instead, which is p.14's other shape. Wasp still holds the Block, so the row that proves
            // both shapes side by side is hers.
            var hand = Hand(runtime, TeamSide.Red);

            // Milestone 8.11b: every offer carries the card it is about, as the tray draws one, so the
            // runtime region draws whole faces here rather than abbreviated text - and the four are
            // chosen so the Game View inspection this test captures covers p.14's Block as well as an
            // ordinary numeric Defense. The hand holds five and this row is four, so which one is left
            // out matters; taking the first four dropped the only Block Wasp's starting hand holds.
            var offeredFaces = hand.TrayCards
                .Where(card => hand.Cards.Any(instance => instance.Id.Equals(card.CardInstanceId)))
                .OrderByDescending(card => card.Face.PrimaryActionLabel.Contains("BLOCK"))
                .Take(4)
                .ToArray();
            Assert.That(offeredFaces, Has.Length.EqualTo(4));
            Assert.That(offeredFaces.Any(card => card.Face.PrimaryActionLabel.Contains("BLOCK")), Is.True,
                "so the captured row shows a Block face beside the numeric ones.");
            var invocations = offeredFaces
                .Select(card => new TypedInvocationViewModel(
                    // Named from the card rather than from an invented string, so the tooltip and the
                    // face a player sees are the same card - the correction milestone 8.8a made to its
                    // own capture for the same reason.
                    $"DISCARD {card.DisplayName.ToUpperInvariant()}\n{card.Face.ValueLine}",
                    DirectorCommand.ChooseDefenseCard(card.CardInstanceId),
                    null,
                    card))
                .ToArray();
            var declineCommand = DirectorCommand.DeclineDefense();
            var declineInvocation = new TypedInvocationViewModel(
                "DECLINE DEFENSE",
                declineCommand);
            var choices = invocations.Concat(new[] { declineInvocation }).ToArray();
            var model = runtime.RoundView.Model;
            TypedInvocationViewModel submitted = null;
            runtime.RoundView.InvocationSubmitted += value => submitted = value;
            runtime.RoundView.Render(new RoundActionViewModel(
                model.Version,
                model.RoundIndex,
                model.TurnIndex,
                model.Phase,
                model.Status,
                model.Actions,
                model.EndTurnInvocation,
                model.ContinueEndOfRoundInvocation,
                model.InitiativeChoices,
                choices));

            yield return null;

            Assert.That(runtime.RoundView.Model.DefenseChoices, Has.Count.EqualTo(5));
            Assert.That(runtime.RoundView.Model.DefenseChoices[2].Label, Is.EqualTo(invocations[2].Label),
                "Every offer keeps the complete label it was projected with, whole and unabbreviated.");
            Assert.That(runtime.RoundView.Model.DefenseChoices[4].Label, Is.EqualTo("DECLINE DEFENSE"));

            // Milestone 8.11b, in the runtime scene: four whole faces from the tray's own builder, in
            // the projected response order, and DECLINE DEFENSE still a control of its own.
            var faces = Descendants(runtime.RoundView.Root)
                .Where(element => element.name == "gd-defense-card")
                .ToArray();
            Assert.That(faces, Has.Length.EqualTo(4));
            Assert.That(
                faces.All(face =>
                    Descendants(face).Count(element => element.name == "gd-card-preview") == 1),
                Is.True);
            Assert.That(
                invocations.All(invocation => faces.Any(face =>
                    face.tooltip.Contains(invocation.Card.DisplayName.ToUpperInvariant()))),
                Is.True,
                "Each offer's whole-card control identifies its own card whether the visible arm is " +
                "prepared artwork or the generated fallback.");
            Assert.That(
                Descendants(runtime.RoundView.DeclineDefenseRoot)
                    .OfType<UnityEngine.UIElements.Button>()
                    .Single(button => button.text == "DECLINE DEFENSE").name,
                Is.Not.EqualTo("gd-defense-card"));
            yield return Capture("tmp/milestone-8-11b-defense.png");
            foreach (var invocation in invocations)
            {
                runtime.RoundView.ForwardInvocation(invocation);
                Assert.That(submitted, Is.SameAs(invocation));
                Assert.That(submitted.Command.Kind, Is.EqualTo(DirectorCommandKind.ChooseDefenseCard));
                Assert.That(submitted.Command.CardInstanceId, Is.EqualTo(invocation.Command.CardInstanceId));
            }

            runtime.RoundView.ForwardInvocation(declineInvocation);
            Assert.That(submitted, Is.SameAs(declineInvocation));
            Assert.That(submitted.Command, Is.EqualTo(declineCommand));
        }

        [UnityTest]
        public IEnumerator Scene_PassiveDefensePreviewAndSignedMinionCuesUseConfiguredViews()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var supporting = runtime.BoardView.Model.Minions.First(value => value.TeamSide == TeamSide.Blue);
            var hindering = runtime.BoardView.Model.Minions.First(value => value.TeamSide == TeamSide.Red);
            var baseBoard = runtime.BoardView.Model;
            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 1,
                baseBoard.Heroes,
                baseBoard.Minions,
                baseBoard.MovementDestinations,
                baseBoard.AttackTargets,
                System.Array.Empty<MinionId>(),
                baseBoard.FocusedHeroId,
                fastTravelDestinations: baseBoard.FastTravelDestinations,
                minionDefenseCues: new[]
                {
                    new MinionDefenseCueViewModel(supporting.MinionId, 1),
                    new MinionDefenseCueViewModel(hindering.MinionId, -1)
                }));

            var supportingView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(supporting.MinionId));
            var hinderingView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(hindering.MinionId));
            Assert.That(supportingView.GetComponentsInChildren<TextMesh>(true), Is.Empty);
            Assert.That(hinderingView.GetComponentsInChildren<TextMesh>(true), Is.Empty);
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                    value.Coordinate.Equals(supporting.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.DefensePositive));
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                    value.Coordinate.Equals(hindering.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.DefenseNegative));

            var resolutionId = new ActionResolutionId("configured-preview-resolution");
            var choiceId = new ActionResolutionStepId("configured-preview-choice");
            var cardBreakdown = new DefenseBreakdownViewModel(
                12,
                resolutionId,
                choiceId,
                Hand(runtime, TeamSide.Blue).Cards[0].Id,
                "TEST DEFENSE CARD DEFENSE",
                "ATTACK:\n  +3 — Test Attack Card\nATTACK TOTAL: 3\n\n" +
                "DEFENSE:\n  CARD: TEST DEFENSE CARD\n  +3 — Test Defense Card\n" +
                "DEFENSE TOTAL: 3\nRESULT: DEFENDED",
                AttackResolutionOutcome.SuccessfullyDefended,
                new[] { new DefenseContributionRowViewModel(3, "Test Attack Card") },
                3,
                new[] { new DefenseContributionRowViewModel(3, "Test Defense Card") },
                3,
                DefenseBlockResult.None,
                "DEFENDED");
            var declineBreakdown = new DefenseBreakdownViewModel(
                12,
                resolutionId,
                choiceId,
                null,
                "DECLINE PREVIEW",
                "ATTACK:\n  +3 — Test Attack Card\nATTACK TOTAL: 3\n\n" +
                "DEFENSE:\n  NO DEFENSE TOTAL\nRESULT: NO DEFENSE TOTAL - ATTACK SUCCEEDS",
                AttackResolutionOutcome.AttackSuccessful,
                new[] { new DefenseContributionRowViewModel(3, "Test Attack Card") },
                3,
                System.Array.Empty<DefenseContributionRowViewModel>(),
                null,
                DefenseBlockResult.None,
                "NO DEFENSE — ATTACK SUCCEEDS");
            var currentRound = runtime.RoundView.Model;
            var cardInvocation = new TypedInvocationViewModel(
                "DISCARD TEST DEFENSE CARD\nPRINTED DEFENSE 3",
                DirectorCommand.ChooseDefenseCard(Hand(runtime, TeamSide.Blue).Cards[0].Id),
                cardBreakdown);
            var declineInvocation = new TypedInvocationViewModel(
                "DECLINE DEFENSE",
                DirectorCommand.DeclineDefense(),
                declineBreakdown);
            runtime.RoundView.Render(new RoundActionViewModel(
                12,
                currentRound.RoundIndex,
                currentRound.TurnIndex,
                currentRound.Phase,
                "Incoming Attack 3: choose a card to defend with.",
                currentRound.Actions,
                currentRound.EndTurnInvocation,
                currentRound.ContinueEndOfRoundInvocation,
                currentRound.InitiativeChoices,
                new[] { cardInvocation, declineInvocation },
                currentRound.FastTravelOffers));

            runtime.RoundView.ShowDefensePreview(cardBreakdown, new Rect(120f, 220f, 190f, 46f));
            yield return null;
            Assert.That(runtime.RoundView.ActiveDefensePreview.Text, Does.Contain("DEFENSE TOTAL: 3"));
            runtime.RoundView.ShowDefensePreview(declineBreakdown, new Rect(320f, 220f, 190f, 46f));
            yield return null;
            Assert.That(runtime.RoundView.ActiveDefensePreview.Text,
                Does.Contain("NO DEFENSE TOTAL - ATTACK SUCCEEDS"));
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                    value.Coordinate.Equals(supporting.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.DefensePositive),
                "Moving between Defense choices must not rebuild attack-scoped cues.");
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                    value.Coordinate.Equals(hindering.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.DefenseNegative));

            if (!UnityEngine.Application.isBatchMode)
            {
                var screenshotPath = Path.GetFullPath("tmp/milestone-6-1b-defense-preview.png");
                if (File.Exists(screenshotPath))
                {
                    File.Delete(screenshotPath);
                }
                ScreenCapture.CaptureScreenshot(screenshotPath);
                yield return new WaitForEndOfFrame();
                yield return null;
                Assert.That(File.Exists(screenshotPath), Is.True,
                    "The targeted Game View visual evidence must be captured under tmp/.");
            }
        }

        /// <summary>Milestone 8.30i's final structured Defense-panel visual evidence.</summary>
        [UnityTest]
        public IEnumerator Scene_CapturesStructuredDefensePreviewAtBothSupportedResolutions()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                Assert.That(UnityEngine.Application.isBatchMode, Is.True,
                    "The screenshot branch is exercised by the dedicated nonbatch recipe.");
                yield break;
            }

            var restoreFirst = SelectFixedGameViewResolution(2560, 1440);
            System.Action restoreSecond = null;
            try
            {
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2560 || Screen.height != 1440);
                     frame++) yield return null;
                Assert.That(Screen.width, Is.EqualTo(2560));
                Assert.That(Screen.height, Is.EqualTo(1440));
                yield return LoadOfficialSixPlayerScene();

                var runtime = Root().Runtime;
                var card = runtime.HandsView.Model.ViewedHand.TrayCards.First();
                var breakdown = new DefenseBreakdownViewModel(
                    12,
                    new ActionResolutionId("structured-defense-capture"),
                    new ActionResolutionStepId("structured-defense-choice"),
                    card.CardInstanceId,
                    card.DisplayName.ToUpperInvariant() + " DEFENSE",
                    "ATTACK:\n  +3 — Test Attack Card\nATTACK TOTAL: 3\n\n" +
                    "DEFENSE:\n  +2 — Printed Defense\n  +1 — Supporting Minion\n" +
                    "DEFENSE TOTAL: 3\nRESULT: DEFENDED",
                    AttackResolutionOutcome.SuccessfullyDefended,
                    new[] { new DefenseContributionRowViewModel(3, "Test Attack Card") },
                    3,
                    new[]
                    {
                        new DefenseContributionRowViewModel(2, "Printed Defense"),
                        new DefenseContributionRowViewModel(1, "Supporting Minion")
                    },
                    3,
                    DefenseBlockResult.None,
                    "DEFENDED");
                var offer = new TypedInvocationViewModel(
                    "DISCARD " + card.DisplayName.ToUpperInvariant(),
                    DirectorCommand.ChooseDefenseCard(card.CardInstanceId),
                    breakdown,
                    card,
                    CardShineSlot.PrimaryRing);
                var decline = new TypedInvocationViewModel(
                    "DECLINE DEFENSE", DirectorCommand.DeclineDefense());
                var current = runtime.RoundView.Model;
                runtime.RoundView.Render(new RoundActionViewModel(
                    12,
                    current.RoundIndex,
                    current.TurnIndex,
                    current.Phase,
                    "Incoming Attack 3: choose a card to defend with.",
                    current.Actions,
                    current.EndTurnInvocation,
                    current.ContinueEndOfRoundInvocation,
                    current.InitiativeChoices,
                    new[] { offer, decline },
                    current.FastTravelOffers));
                yield return null;
                yield return null;

                var control = runtime.RoundView.Root.Q<Button>("gd-defense-card");
                Assert.That(control, Is.Not.Null);
                Assert.That(control.worldBound.width, Is.GreaterThan(0f));
                runtime.RoundView.ShowDefensePreview(breakdown, control.worldBound);
                yield return new WaitForEndOfFrame();
                yield return Capture("tmp/8-30i-defense-preview-2560x1440.png");

                restoreSecond = SelectFixedGameViewResolution(2048, 1152);
                for (var frame = 0;
                     frame < 120 && (Screen.width != 2048 || Screen.height != 1152);
                     frame++) yield return null;
                Assert.That(Screen.width, Is.EqualTo(2048));
                Assert.That(Screen.height, Is.EqualTo(1152));
                yield return null;
                runtime.RoundView.ShowDefensePreview(breakdown, control.worldBound);
                yield return new WaitForEndOfFrame();
                yield return Capture("tmp/8-30i-defense-preview-2048x1152.png");
            }
            finally
            {
                restoreSecond?.Invoke();
                restoreFirst();
            }
        }

        /// <summary>
        /// Milestone 6.1a. The configured setup is symmetric, so no round of it ever produces a
        /// Minion Battle removal choice; the prompt and its board cues are proven here the same way
        /// the Defense preview is - by rendering an authority-shaped model into the scene's own
        /// views and checking what the configured scene does with it, including that a passive view
        /// forwards controls without reaching authoritative state itself.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_MinionRemovalPromptAndBoardCuesUseConfiguredPassiveViews()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            Assert.That(runtime.PendingMinionBattleChoice, Is.Null);
            Assert.That(runtime.MinionChoiceView.Model, Is.Null,
                "With no pending choice the configured prompt exposes no controls.");

            // Non-Heavy candidates only. The authority withholds a control from a Heavy while any
            // non-Heavy is unselected, so a hand-built model offering one would render a prompt the
            // real projection can never produce.
            var blue = runtime.BoardView.Model.Minions
                .Where(value => value.TeamSide == TeamSide.Blue &&
                    value.MinionType != StandardMinionType.Heavy)
                .OrderBy(value => value.MinionId.Value, System.StringComparer.Ordinal)
                .Take(2)
                .ToArray();
            var blueTeamId = runtime.Setup.Teams.Single(value => value.TeamSide == TeamSide.Blue).TeamId;
            var resolutionId = new EndOfRoundResolutionId("configured-minion-battle");
            var choiceId = new EndOfRoundChoiceId("configured-minion-battle-removals");
            var baseBoard = runtime.BoardView.Model;
            var acceptedBefore = runtime.AcceptedCommands.Count;
            var snapshotBefore = runtime.CaptureSnapshot();

            // Nothing selected yet: both authority-shaped candidates carry static eligible removal
            // rings and there is nothing to confirm. Hover remains reserved for a later milestone.
            var unselected = blue.Select((minion, index) => new MinionChoiceCandidateViewModel(
                    minion.MinionId,
                    $"REMOVE {MinionChoiceLabels.CandidateLabel(TeamSide.Blue, minion.MinionType, index + 1)}",
                    index + 1,
                    false,
                    DirectorCommand.SelectMinionBattleRemoval(new MinionBattleChoiceCommand(
                        MinionBattleChoiceCommandKind.Select, resolutionId, choiceId,
                        TeamSide.Blue, minion.MinionId))))
                .ToArray();
            runtime.MinionChoiceView.Render(new MinionChoiceViewModel(
                baseBoard.Version,
                resolutionId,
                choiceId,
                blueTeamId,
                TeamSide.Blue,
                1,
                0,
                unselected,
                null));
            RenderTargetCues(runtime, baseBoard, baseBoard.Version + 1,
                blue.Select(value => new PieceTargetCueViewModel(
                    BoardObjectReference.ForMinion(value.MinionId),
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Removal)));
            yield return null;

            Assert.That(runtime.MinionChoiceView.Model.PromptLabel,
                Is.EqualTo("MINION BATTLE - [B] BLUE REMOVES 1 MINION"));
            Assert.That(runtime.MinionChoiceView.Model.StatusLabel, Is.EqualTo("SELECTED 0 OF 1"));
            Assert.That(runtime.MinionChoiceView.Model.Candidates, Has.Count.EqualTo(2));
            Assert.That(runtime.MinionChoiceView.Model.ConfirmInvocation, Is.Null,
                "Below the required count the configured prompt offers no confirmation control.");

            var firstView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(blue[0].MinionId));
            var secondView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(blue[1].MinionId));
            var firstRing = firstView.TargetRing;
            var secondRing = secondView.TargetRing;
            Assert.That(firstView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(secondView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(firstRing.IsVisible, Is.False);
            Assert.That(secondRing.IsVisible, Is.False);
            Assert.That(runtime.BoardView.TileViews.Where(value =>
                    value.Coordinate.Equals(blue[0].Coordinate) ||
                    value.Coordinate.Equals(blue[1].Coordinate)),
                Has.All.Matches<HexTileView>(value =>
                    value.CornerHighlight.Kind == HexCornerHighlightKind.Attack &&
                    value.CornerHighlight.VisibleMarkCount == 6));
            Assert.That(firstView.GetComponentsInChildren<TextMesh>(true)
                    .Concat(secondView.GetComponentsInChildren<TextMesh>(true)),
                Has.None.Matches<TextMesh>(value => value.text == "▼"),
                "The former triangle glyph is absent from both candidate pieces.");

            // One selected at the required count: the chosen minion keeps its board cue, its control
            // becomes the projected deselect, and the confirmation appears.
            var selectedCandidate = new MinionChoiceCandidateViewModel(
                blue[0].MinionId,
                $"KEEP {MinionChoiceLabels.CandidateLabel(TeamSide.Blue, blue[0].MinionType, 1)}",
                1,
                true,
                DirectorCommand.DeselectMinionBattleRemoval(new MinionBattleChoiceCommand(
                    MinionBattleChoiceCommandKind.Deselect, resolutionId, choiceId,
                    TeamSide.Blue, blue[0].MinionId)));
            var confirm = new TypedInvocationViewModel(
                "CONFIRM REMOVAL",
                DirectorCommand.ConfirmMinionBattleRemovals(new MinionBattleChoiceCommand(
                    MinionBattleChoiceCommandKind.Confirm, resolutionId, choiceId, TeamSide.Blue)));
            runtime.MinionChoiceView.Render(new MinionChoiceViewModel(
                baseBoard.Version,
                resolutionId,
                choiceId,
                blueTeamId,
                TeamSide.Blue,
                1,
                1,
                new[] { selectedCandidate },
                confirm));
            RenderTargetCues(runtime, baseBoard, baseBoard.Version + 2, new[]
            {
                new PieceTargetCueViewModel(
                    BoardObjectReference.ForMinion(blue[0].MinionId),
                    BoardTargetCueState.Selected,
                    BoardTargetConsequenceIntent.Removal),
                new PieceTargetCueViewModel(
                    BoardObjectReference.ForMinion(blue[1].MinionId),
                    BoardTargetCueState.Eligible,
                    BoardTargetConsequenceIntent.Removal)
            });
            yield return null;

            Assert.That(runtime.MinionChoiceView.Model.StatusLabel, Is.EqualTo("SELECTED 1 OF 1"));
            Assert.That(runtime.MinionChoiceView.Model.Candidates.Single().IsSelected, Is.True);
            Assert.That(runtime.MinionChoiceView.Model.ConfirmInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.ConfirmMinionBattleRemovals));
            Assert.That(firstView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(secondView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(firstView.TargetRing, Is.SameAs(firstRing));
            Assert.That(secondView.TargetRing, Is.SameAs(secondRing));
            Assert.That(firstRing.IsVisible, Is.False);
            Assert.That(secondRing.IsVisible, Is.False);
            Assert.That(runtime.BoardView.TileViews.Where(value =>
                    value.Coordinate.Equals(blue[0].Coordinate) ||
                    value.Coordinate.Equals(blue[1].Coordinate)),
                Has.All.Matches<HexTileView>(value =>
                    value.CornerHighlight.Kind == HexCornerHighlightKind.Attack));

            if (!UnityEngine.Application.isBatchMode)
            {
                var promptScreenshotPath = Path.GetFullPath("tmp/milestone-6-1a-minion-removal-prompt.png");
                if (File.Exists(promptScreenshotPath))
                {
                    File.Delete(promptScreenshotPath);
                }
                ScreenCapture.CaptureScreenshot(promptScreenshotPath);
                yield return new WaitForEndOfFrame();
                yield return null;
                Assert.That(File.Exists(promptScreenshotPath), Is.True,
                    "The targeted Game View visual evidence must be captured under tmp/.");
            }

            MinionChoiceCandidateViewModel submitted = null;
            var confirmed = 0;
            var highlights = new List<MinionChoiceCandidateViewModel>();
            runtime.MinionChoiceView.CandidateSubmitted += value => submitted = value;
            runtime.MinionChoiceView.ConfirmRequested += () => confirmed++;
            runtime.MinionChoiceView.CandidateHighlighted += value => highlights.Add(value);

            runtime.MinionChoiceView.ForwardCandidate(selectedCandidate);
            Assert.That(submitted, Is.SameAs(selectedCandidate));
            runtime.MinionChoiceView.ForwardConfirm();
            Assert.That(confirmed, Is.EqualTo(1));

            // Milestone 6.2d: the prompt reports the whole candidate rather than its unit alone, so a
            // control that shares its unit with every other one is still distinguishable. The unit is
            // still readable off it, which is what the board's minion cue goes on reading.
            runtime.MinionChoiceView.ForwardHighlight(null);
            runtime.MinionChoiceView.ForwardHighlight(selectedCandidate);
            Assert.That(highlights.Last(), Is.SameAs(selectedCandidate));
            Assert.That(runtime.MinionChoiceView.HighlightedCandidate, Is.SameAs(selectedCandidate));
            Assert.That(runtime.MinionChoiceView.HighlightedUnit,
                Is.EqualTo(RuleObject.ForMinion(blue[0].MinionId)));
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBefore),
                "The passive prompt reports intent; it never advances authoritative state itself.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshotBefore));

            // Back to the ordinary no-choice state the configured scene actually sits in.
            runtime.MinionChoicePresenter.Refresh();
            RenderTargetCues(runtime, baseBoard, baseBoard.Version + 3,
                System.Array.Empty<PieceTargetCueViewModel>());
            yield return null;

            Assert.That(runtime.MinionChoiceView.Model, Is.Null);
            Assert.That(firstView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(secondView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(firstRing.IsVisible, Is.False);
            Assert.That(secondRing.IsVisible, Is.False);
        }

        private static void RenderTargetCues(
            ConfiguredMatchSceneRuntime runtime,
            BoardInteractionViewModel baseBoard,
            long version,
            IEnumerable<PieceTargetCueViewModel> cues)
        {
            runtime.BoardView.Render(new BoardInteractionViewModel(
                version,
                baseBoard.Heroes,
                baseBoard.Minions,
                baseBoard.MovementDestinations,
                baseBoard.AttackTargets,
                System.Array.Empty<MinionId>(),
                baseBoard.FocusedHeroId,
                fastTravelDestinations: baseBoard.FastTravelDestinations,
                minionDefenseCues: baseBoard.MinionDefenseCues,
                pieceTargetCues: cues));
        }

        private static void RenderBoard(
            ConfiguredMatchSceneRuntime runtime,
            BoardInteractionViewModel baseBoard,
            long version,
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<MinionOccupancy> minions,
            IEnumerable<PlacedToken> tokens,
            IEnumerable<PieceTargetCueViewModel> cues)
        {
            runtime.BoardView.Render(new BoardInteractionViewModel(
                version,
                heroes,
                minions,
                baseBoard.MovementDestinations,
                baseBoard.AttackTargets,
                baseBoard.MinionAttackTargets,
                baseBoard.FocusedHeroId,
                placedTokens: tokens,
                fastTravelDestinations: baseBoard.FastTravelDestinations,
                minionDefenseCues: baseBoard.MinionDefenseCues,
                pieceTargetCues: cues,
                correctionDestination: baseBoard.CorrectionDestination,
                effectChoiceDestinations: baseBoard.EffectChoiceDestinations,
                effectChoiceTargetSpace: baseBoard.EffectChoiceTargetSpace,
                effectChoiceResultingSpace: baseBoard.EffectChoiceResultingSpace,
                heroRespawnHighlight: baseBoard.HeroRespawnHighlight,
                heroRespawnDestinations: baseBoard.HeroRespawnDestinations));
        }

        private static void CompleteBlueApproachTurn(
            ConfiguredMatchSceneRuntime runtime,
            bool moveTowardFriendlyMinion,
            string failureMessage)
        {
            if (moveTowardFriendlyMinion)
            {
                var movement = runtime.RoundView.Model.Actions.FirstOrDefault(value =>
                    ActionKind(runtime, value.Command.ActionDefinitionId) ==
                    GuardsDigital.Core.Cards.ActionKind.Movement);
                if (movement != null)
                {
                    Assert.That(runtime.RoundPresenter.Submit(movement), Is.True, failureMessage);
                    while (runtime.PendingMovementChoice != null)
                    {
                        var friendlyMinions = runtime.BoardView.Model.Minions
                            .Where(value => value.TeamSide == TeamSide.Blue &&
                                value.MinionType == StandardMinionType.Melee)
                            .ToArray();
                        var destination = runtime.PendingMovementChoice.LegalDestinations
                            .OrderBy(value => friendlyMinions.Min(minion =>
                                HexDistance(value.Destination, minion.Coordinate)))
                            .ThenByDescending(value => value.Cost)
                            .First().Destination;
                        Assert.That(runtime.BoardPresenter.SubmitDestination(destination), Is.True,
                            failureMessage);
                    }

                    return;
                }
            }

            Assert.That(runtime.SubmitEndTurn(), Is.True, failureMessage);
        }

        [UnityTest]
        public IEnumerator Scene_UndoRestartRestoreAndDiagnosticsStayEquivalent()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var initial = runtime.CaptureSnapshot();
            Assert.That(runtime.DiagnosticEvents.Single().EventType,
                Is.EqualTo("Setup.MatchInitialized"));
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            var blue = Hand(runtime, TeamSide.Blue).Cards[1];
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            var afterRed = runtime.CaptureSnapshot();
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            var revealed = runtime.CaptureSnapshot();
            var revealedEvents = runtime.DiagnosticEvents.Select(value => value.DisplayText).ToArray();

            runtime.DiagnosticPresenter.Toggle();
            Assert.That(runtime.DiagnosticView.Model.IsExpanded, Is.True);
            Assert.That(runtime.Undo(), Is.True);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(afterRed));
            Assert.That(runtime.Restart(), Is.True);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(initial));
            Assert.That(runtime.RestoreSnapshot(revealed), Is.True, runtime.LastOperationDetail);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(revealed));
            Assert.That(runtime.DiagnosticEvents.Select(value => value.DisplayText),
                Is.EqualTo(revealedEvents));
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_PlaysTwoCompleteRoundsThroughConfiguredFeatureBoundary()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;

            for (var round = 1; round <= 2; round++)
            {
                for (var turn = 1; turn <= 4; turn++)
                {
                    CommitNextTurn(runtime);
                    Assert.That(runtime.SubmitEndTurn(), Is.True);
                    Assert.That(runtime.SubmitEndTurn(), Is.True);
                }

                Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
                while (runtime.Phase == RoundResolutionPhase.EndOfRound)
                {
                    // Milestone 7.6. p.8's Pity Coin leaves every hero able to afford a level, so from
                    // the second round on this loop meets p.15's Upgrade a Card choice. No presenter
                    // draws a paused system transition yet - the same gap milestone 6.11's Lane-Push
                    // respawn has - so the scene answers it through the runtime's director command
                    // surface, naming the option rather than letting presentation pick one.
                    var options = runtime.PausedSystemTransitionOptions;
                    if (options.Count > 0)
                    {
                        Assert.That(runtime.AnswerPausedSystemTransition(options[0]), Is.True);
                        continue;
                    }

                    Assert.That(runtime.ContinueEndOfRound(), Is.True);
                }

                Assert.That(runtime.RoundIndex, Is.EqualTo(round + 1));
            }

            Assert.That(runtime.CompletedRounds.Select(value => value.RoundIndex),
                Is.EqualTo(new[] { 1, 2 }));
        }

        /// <summary>
        /// Milestone 6.5c added the second board highlight, so the composition root now generates two
        /// materials. What this fixture guards is unchanged and is the point: both are owned by the
        /// root, both are released on reload, and neither accumulates a copy per scene load.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_HidesAnAuthoritativelyAbsentHeroAndShowsItAgainWhenReturned()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var baseBoard = runtime.BoardView.Model;
            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            var blueHeroId = runtime.Setup.Heroes.Single(value => value.TeamSide == TeamSide.Blue).HeroId;
            var blueView = heroViews.Single(value => value.Occupancy.HeroId.Equals(blueHeroId));
            var wasTokened = blueView.HasTokenVisual;
            var meshBefore = blueView.GetComponent<MeshFilter>().sharedMesh;
            var remainingHeroes = baseBoard.Heroes
                .Where(value => !value.HeroId.Equals(blueHeroId))
                .ToArray();
            RenderBoard(
                runtime,
                baseBoard,
                baseBoard.Version + 1,
                baseBoard.Heroes,
                baseBoard.Minions,
                baseBoard.PlacedTokens,
                new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(blueHeroId),
                        BoardTargetCueState.Selected,
                        BoardTargetConsequenceIntent.Removal)
                });
            yield return null;
            var retainedRing = blueView.TargetRing;
            Assert.That(blueView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(retainedRing.IsVisible, Is.False);
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(blueView.Occupancy.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "Minion Battle removal uses the blood-red corner language.");

            RenderBoard(
                runtime,
                baseBoard,
                baseBoard.Version + 2,
                remainingHeroes,
                baseBoard.Minions,
                baseBoard.PlacedTokens,
                System.Array.Empty<PieceTargetCueViewModel>());

            Assert.That(blueView.gameObject.activeSelf, Is.False,
                "A Minion-Battle-removed hero must disappear when its occupancy leaves projection.");
            Assert.That(blueView.TargetCueState, Is.EqualTo(BoardTargetCueState.None),
                "An absent hero cannot retain the choice state it carried before removal.");
            Assert.That(blueView.TargetRing, Is.SameAs(retainedRing));

            var returnedCoordinate = Object.FindObjectsByType<HexTileView>()
                .First(tile => tile.IsPlayable && remainingHeroes.All(hero =>
                    !hero.Coordinate.Equals(tile.Coordinate)))
                .Coordinate;
            var returnedHeroes = remainingHeroes.Concat(new[]
            {
                new HeroOccupancy(blueHeroId, returnedCoordinate)
            });
            RenderBoard(
                runtime,
                baseBoard,
                baseBoard.Version + 3,
                returnedHeroes,
                baseBoard.Minions,
                baseBoard.PlacedTokens,
                new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(blueHeroId),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Affect)
                });

            Assert.That(blueView.gameObject.activeSelf, Is.True);
            Assert.That(blueView.Occupancy.Coordinate, Is.EqualTo(returnedCoordinate),
                "Respawn, Undo, or restore reuses and repositions the configured hero view.");
            Assert.That(blueView.TargetRing, Is.SameAs(retainedRing));
            Assert.That(blueView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(retainedRing.IsVisible, Is.False);
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(returnedCoordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect),
                "The returned identity rebuilds the neutral corner cue from projection state.");
            // Milestone 8.11b: and it comes back as the piece it went away as. The swap is a mesh and
            // a material on the view's own components, so visibility, position and the piece are one
            // object's state rather than three that could disagree after a Defeat.
            Assert.That(blueView.HasTokenVisual, Is.EqualTo(wasTokened));
            Assert.That(blueView.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(meshBefore));
            Assert.That(
                blueView.GetComponent<MeshRenderer>().sharedMaterials[0].color,
                Is.EqualTo(HeroTokenArtwork.TeamRim(TeamSide.Blue)));
            Assert.That(
                Object.FindObjectsByType<PrimitiveHeroView>()
                    .Count(value => value.Occupancy.HeroId.Equals(blueHeroId)),
                Is.EqualTo(1),
                "and no duplicate or stale view was built to bring it back.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneReload_ReleasesAndReusesTheBoundedSharedHighlightResources()
        {
            yield return LoadScene();
            var first = Root();
            Assert.That(first.OwnsGeneratedHighlightMaterial, Is.False);
            // 8.30i owns two fixed meshes and sixteen shared shine materials: 8.30h's six board-cue
            // pairs plus the two signed Defense-contribution pairs. The retired destination fill and
            // rim resources are not allocated by configured composition.
            // The 210 tiles retain renderers only; none owns a material or mesh of its own.
            Assert.That(first.OwnedHighlightNativeResourceCount, Is.EqualTo(18));
            Assert.That(CountHighlightMaterials(), Is.Zero);
            Assert.That(CountResultingSpaceMaterials(), Is.Zero);
            AssertCurrentMinionBindings(first);
            var firstTargetRings = Object.FindObjectsByType<BoardTargetRing>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(firstTargetRings, Is.Not.Empty);
            Assert.That(firstTargetRings,
                Has.All.Matches<BoardTargetRing>(value =>
                    value.OwnedNativeResourceCount == 4 &&
                    value.GetComponent<MeshRenderer>().sharedMaterials.Length == 3));
            var retiredRing = firstTargetRings[0];
            var retiredMesh = retiredRing.GeneratedMesh;
            var retiredMaterials = retiredRing.GetComponent<MeshRenderer>()
                .sharedMaterials.ToArray();

            yield return LoadScene();
            yield return null;
            Assert.That(retiredRing == null, Is.True,
                "Reload destroys the prior scene's retained target-ring component.");
            Assert.That(retiredMesh == null, Is.True,
                "The runtime Destroy branch releases the ring's owned native mesh.");
            Assert.That(retiredMaterials,
                Has.All.Matches<Material>(value => value == null),
                "The runtime Destroy branch releases keyline, consequence, and wash materials.");
            var second = Root();
            Assert.That(second.OwnedHighlightNativeResourceCount, Is.EqualTo(18),
                "and a reload rebuilds the same bounded eighteen rather than accumulating them.");
            Assert.That(CountHighlightMaterials(), Is.Zero);
            Assert.That(CountResultingSpaceMaterials(), Is.Zero);
            AssertCurrentMinionBindings(second);
            var secondTargetRings = Object.FindObjectsByType<BoardTargetRing>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(secondTargetRings, Has.Length.EqualTo(firstTargetRings.Length));
            Assert.That(secondTargetRings,
                Has.All.Matches<BoardTargetRing>(value =>
                    value.OwnedNativeResourceCount == 4 &&
                    value.GetComponent<MeshRenderer>().sharedMaterials.Length == 3),
                "Reload recreates one mesh plus exactly three retained materials per piece ring.");
        }

        [UnityTest]
        public IEnumerator Scene_RespawnButtonHoverUsesCyanSixCornerHighlight()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var baseBoard = runtime.BoardView.Model;
            var tile = Object.FindObjectsByType<HexTileView>()
                .First(candidate => candidate.IsPlayable);
            var destination = tile.Coordinate;

            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 1,
                baseBoard.Heroes,
                baseBoard.Minions,
                System.Array.Empty<HexCoordinate>(),
                baseBoard.AttackTargets,
                baseBoard.MinionAttackTargets,
                baseBoard.FocusedHeroId,
                effectChoiceDestinations: new[] { destination }));
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Effect));

            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 2,
                baseBoard.Heroes,
                baseBoard.Minions,
                System.Array.Empty<HexCoordinate>(),
                baseBoard.AttackTargets,
                baseBoard.MinionAttackTargets,
                baseBoard.FocusedHeroId,
                heroRespawnHighlight: destination));
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Respawn));
            Assert.That(tile.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));

            var hero = runtime.Setup.Heroes.Single(value => value.TeamSide == TeamSide.Blue).HeroId;
            var card = runtime.Cards.First(value => value.OwnerId.Equals(hero) &&
                value.State.Location == CardLocation.Hand);
            var option = new HeroRespawnOptionViewModel(
                "RESPAWN AT SPAWN #1",
                destination,
                DirectorCommand.RespawnHero(new HeroRespawnCommand(
                    HeroRespawnCommandKind.Respawn, hero, card.Id, destination)));
            HeroRespawnOptionViewModel highlighted = null;
            runtime.RoundView.RespawnHighlighted += value => highlighted = value;
            runtime.RoundView.Render(new RoundActionViewModel(
                runtime.RoundView.Model.Version + 1,
                runtime.RoundIndex,
                runtime.RoundView.Model.TurnIndex,
                RoundResolutionPhase.ResolvingTurn,
                "Choose respawn",
                System.Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                System.Array.Empty<TypedInvocationViewModel>(),
                System.Array.Empty<TypedInvocationViewModel>(),
                heroRespawnOptions: new[] { option }));
            runtime.RoundView.ForwardRespawnHighlight(option);
            Assert.That(highlighted, Is.SameAs(option));
            Assert.That(highlighted.Destination, Is.EqualTo(destination));
            yield return null;
        }


        /// <summary>
        /// Milestone 8.4. The scene deals Wasp's verified cards, whose printed primaries are all
        /// refused until their 8.6-8.13 slices compose their text, so the two cases below - which need
        /// a Skill that resolves and an Attack that reaches a minion - ask for the mechanics-showcase
        /// hands instead. The request is consumed and reset by the composition it applies to, so every
        /// other case in this fixture still loads the configured match.
        /// </summary>
        private static IEnumerator LoadShowcaseScene()
        {
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.MechanicsShowcaseFixture;
            return LoadComposedScene();
        }

        /// <summary>
        /// Milestone 8.20. The verified Wasp mirror at p.6's spawn positions, which is what the default
        /// arm composed until this milestone put Arien on the Blue seat. One scenario here needs it by
        /// name: its subject ends on p.14's Block drawn beside numeric Defense choices, and the Block is
        /// a card Wasp prints and Arien does not.
        /// </summary>
        private static IEnumerator LoadMirrorScene()
        {
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.VerifiedMirror;
            return LoadComposedScene();
        }
        /// <summary>
        /// The staged one-hero-per-team board this fixture was written on.
        ///
        /// <para>Milestone 8.25 named it. It was the composition root default from milestone 8.20 until
        /// this one, when the default became rulebook p.5's six seats; every case here that counts
        /// rails, pieces or heroes counts a one-versus-one board, so the board it counts is now asked for
        /// rather than inherited. <see cref="LoadOfficialSixPlayerScene"/> loads what a player loads.</para>
        /// </summary>
        private static IEnumerator LoadScene()
        {
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.VerifiedStagedArien;
            return LoadComposedScene();
        }

        /// <summary>
        /// Milestone 8.25: rulebook p.5's official six-player configuration, which is what the scene
        /// composes when nothing asks for anything else.
        /// </summary>
        private static IEnumerator LoadOfficialSixPlayerScene()
        {
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            return LoadComposedScene();
        }

        private static IEnumerator LoadComposedScene()
        {
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        private static DirectorHandViewModel Hand(ConfiguredMatchSceneRuntime runtime, TeamSide side) =>
            runtime.HandsView.Model.Hands.Single(value => value.TeamSide == side);

        private static void CommitNextTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            var blue = Hand(runtime, TeamSide.Blue).Cards[0];
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            Assert.That(runtime.IsTurnActive, Is.True);
        }

        /// <summary>
        /// Commits the Red hand's highest-Initiative card that still prints an Attack, against the Blue
        /// hand's lowest, so the acting card offers an Attack. The showcase conversions took the printed
        /// Attack off both hands' first card - Red Bulwark at 6.8a, Blue Feint at 6.8b, Red Needle at
        /// 6.5a - so the ordinary <see cref="CommitNextTurn"/> pair no longer offers one.
        /// </summary>
        private static void CommitNextAttackTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var red = Hand(runtime, TeamSide.Red).Cards
                .Where(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .ActionDefinitions.Any(action =>
                        action.Kind == GuardsDigital.Core.Cards.ActionKind.Attack))
                .OrderByDescending(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();
            var blue = Hand(runtime, TeamSide.Blue).Cards
                .OrderBy(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
        }

        private static void ResolveInitiativeChoice(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.PendingInitiativeChoice != null)
            {
                Assert.That(runtime.SubmitInitiativeChoice(
                    runtime.PendingInitiativeChoice.CandidateCardInstanceIds[0]), Is.True);
            }
        }

        private static GuardsDigital.Core.Cards.ActionKind ActionKind(
            ConfiguredMatchSceneRuntime runtime,
            ActionDefinitionId actionId)
        {
            var offer = runtime.AvailableTurnActions.Single(value =>
                value.ActionDefinitionId.Equals(actionId));
            return runtime.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(actionId)).Kind;
        }

        private static int HexDistance(HexCoordinate first, HexCoordinate second)
        {
            var deltaQ = first.Q - second.Q;
            var deltaR = first.R - second.R;
            return (System.Math.Abs(deltaQ) + System.Math.Abs(deltaR) +
                System.Math.Abs(deltaQ + deltaR)) / 2;
        }

        private static Ray DownRay(Vector3 position) =>
            new Ray(position + (Vector3.up * 2f), Vector3.down);

        private static Rect ProjectedPanelBounds(
            HexTileView tile,
            Camera camera,
            Vector2 panelViewport,
            float outerRadius)
        {
            var mesh = tile.GetComponent<MeshFilter>()?.sharedMesh;
            Assert.That(mesh, Is.Not.Null);
            var points = mesh.vertices
                .Skip(1)
                .Select(vertex => tile.transform.TransformPoint(vertex))
                .Select(camera.WorldToScreenPoint)
                .Select(screen => new Vector2(
                    (screen.x / Screen.width) * panelViewport.x,
                    (1f - (screen.y / Screen.height)) * panelViewport.y))
                .ToArray();
            var pixelsPerWorld = panelViewport.y / (2f * camera.orthographicSize);
            var clearance = PlaceholderTestGridView.ReservedPlayfieldCameraPadding *
                outerRadius * pixelsPerWorld;
            return Rect.MinMaxRect(
                points.Min(point => point.x) - clearance,
                points.Min(point => point.y) - clearance,
                points.Max(point => point.x) + clearance,
                points.Max(point => point.y) + clearance);
        }

        private static bool ContainsBounds(Rect region, Rect bounds) =>
            bounds.xMin >= region.xMin - 0.5f &&
            bounds.xMax <= region.xMax + 0.5f &&
            bounds.yMin >= region.yMin - 0.5f &&
            bounds.yMax <= region.yMax + 0.5f;

        private static int CountHighlightMaterials() =>
            Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null && value.name == DestinationHighlightMaterialOwner.GeneratedMaterialName);

        private static int CountResultingSpaceMaterials() =>
            Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null &&
                value.name == DestinationHighlightMaterialOwner.GeneratedResultingSpaceMaterialName);

        private static void AssertWash(
            BoardTargetRing ring,
            Color expectedColor,
            float expectedAlpha)
        {
            Assert.That(ring.VisibleWashColor.r, Is.EqualTo(expectedColor.r).Within(0.0001f));
            Assert.That(ring.VisibleWashColor.g, Is.EqualTo(expectedColor.g).Within(0.0001f));
            Assert.That(ring.VisibleWashColor.b, Is.EqualTo(expectedColor.b).Within(0.0001f));
            Assert.That(ring.VisibleWashColor.a, Is.EqualTo(expectedAlpha).Within(0.0001f));
        }

        private static void AssertCurrentMinionBindings(MatchSceneCompositionRoot root)
        {
            var views = Object.FindObjectsByType<StandardMinionView>();
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(views, Has.Length.EqualTo(12));
            Assert.That(root.Runtime.BoardView.MinionViews, Is.EquivalentTo(views));
            Assert.That(pointerInput.MinionSelectors.Count, Is.EqualTo(12));
            Assert.That(pointerInput.MinionSelectors.Select(value =>
                value.GetComponent<StandardMinionView>()), Is.EquivalentTo(views));
        }
    }
}
