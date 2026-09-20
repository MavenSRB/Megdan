using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.UrsafarEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.UrsafarUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.53. The end-of-match screen on a live panel, reached by actually ending a match.
    ///
    /// <para><b>Why this cannot be an Edit Mode case.</b> The presenter fixture proves what the screen
    /// SAYS. What needs a panel is the user's own sentence for this milestone - "Everything else
    /// should disappear and this pops up" - which is a question about what is drawn on a composed HUD
    /// at the moment an ending is recorded, and about where the screen lands on a scaled panel. The
    /// hiding covers two layers that are not even composed by the renderer (the phase banner and the
    /// kinetic layer attach themselves to the HUD root lazily), so only a rendered tier can see it.
    /// </para>
    ///
    /// <para>The ending is rulebook p.13's: Ursafar's Tear spends its Tier + 1 counters on an enemy
    /// team staged down to exactly that many, which is the same staging
    /// <c>UrsafarProgressedUiJourneyPlayModeTests.Tear_SpendsTierPlusOneOnce_AndEndsTheGameOnTheLastCounter</c>
    /// already drives - deliberately, so this journey's subject is the screen rather than a second
    /// opinion about how a match ends.</para>
    /// </summary>
    public sealed class VictoryScreenUiJourneyPlayModeTests
    {
        /// <summary>
        /// The HUD surfaces the user named. Addressed by element name, because a journey must fail
        /// saying what the player could still see rather than which field was non-null.
        /// </summary>
        private static readonly string[] EverythingElse =
        {
            "gd-topbar", "gd-hero-rails", "gd-tray-region", "gd-status-dock", "gd-end-turn",
            "gd-seat-undo", "gd-decision-window", "gd-director-toggle", "gd-director-drawer"
        };

        [UnityTest]
        public IEnumerator TheLastLifeCounterIsSpent_AndTheVictoryScreenReplacesTheWholeHud()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return UrsafarAuditStaging.Load(
                    SceneCardContent.VerifiedTierThreeAlternateEngagedStagedUrsafar,
                    enemyDistance: 1,
                    enemyLife: 2);
                var root = Root();
                var runtime = root.Runtime;
                var hud = root.Hud;
                var own = UrsafarSeat(runtime).HeroId;
                var enemy = OtherHeroId(runtime, own);
                var winningSide = runtime.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(own)).TeamSide;

                Assert.That(runtime.VictoryScreenView.Model, Is.Null,
                    "Arrange: nothing of the screen is drawn while the match is being played.");
                Assert.That(UiPlayer.IsShown(Screen_(hud)), Is.False);
                foreach (var name in new[] { "gd-topbar", "gd-hero-rails", "gd-tray-region" })
                {
                    Assert.That(UiPlayer.IsShown(Named(hud, name)), Is.True,
                        "and the ordinary HUD is on screen: " + name);
                }

                CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.Tear);
                yield return YieldUntilTheNamedSeatActs(runtime, own);
                yield return PointTrayAt(root, own);
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        hud,
                        HeroView(enemy).transform.position),
                    Is.True);
                yield return null;
                yield return null;
                yield return UrsafarAuditStaging.AnswerDefense(false);
                yield return null;

                Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded),
                    Remaining(runtime));
                Assert.That(runtime.ReadModel.Outcome.Cause,
                    Is.EqualTo(MatchVictoryCause.LifeCountersExhausted));

                // ---------------------------------------------------------------------------------
                // The screen
                // ---------------------------------------------------------------------------------
                var model = runtime.VictoryScreenView.Model;
                Assert.That(model, Is.Not.Null,
                    "The match ended, so the screen is drawn. " + UiPlayer.Describe(hud));
                var screen = Screen_(hud);
                Assert.That(UiPlayer.IsShown(screen), Is.True);

                Assert.That(Text(hud, "gd-victory-banner"),
                    Is.EqualTo(winningSide == TeamSide.Red ? "RED WINS" : "BLUE WINS"),
                    "The banner is the user's own line: \"above it its which team won RED WINS\".");
                Assert.That(Text(hud, "gd-victory-cause"),
                    Is.EqualTo("THE OPPOSING TEAM RAN OUT OF LIFE COUNTERS"),
                    "and under it \"the win condition they won on\", in the same words the status " +
                    "line has been saying all match.");

                Assert.That(model.Rows.Count(), Is.EqualTo(runtime.Setup.Heroes.Count),
                    "Every hero in the match has a row - the losing side too, which is what the " +
                    "user asked for: \"add similar thing for a losing team as well\".");
                Assert.That(
                    UiPlayer.Descendants(screen).Count(element => element.name == "gd-victory-row"),
                    Is.EqualTo(runtime.Setup.Heroes.Count),
                    "and each one is actually drawn.");
                foreach (var column in VictoryScreenLabels.Columns)
                {
                    Assert.That(
                        UiPlayer.Descendants(screen)
                            .Count(element => element.name == VictoryScreenLabels.CellName(column)),
                        Is.EqualTo(runtime.Setup.Heroes.Count),
                        "Every row draws the " + VictoryScreenLabels.Header(column) + " column.");
                }

                Assert.That(
                    UiPlayer.Descendants(screen).Count(element => element.name == "gd-victory-portrait"),
                    Is.EqualTo(runtime.Setup.Heroes.Count),
                    "with the hero token disc the user asked for beside each name.");

                // ---------------------------------------------------------------------------------
                // "Everything else should disappear"
                // ---------------------------------------------------------------------------------
                foreach (var name in EverythingElse)
                {
                    foreach (var element in UiPlayer.Descendants(hud.RootElement)
                        .Where(candidate => candidate.name == name))
                    {
                        Assert.That(UiPlayer.IsShown(element), Is.False,
                            "'" + name + "' is off the screen while the match is over.");
                    }
                }

                Assert.That(
                    UiPlayer.Descendants(hud.RootElement)
                        .Where(UiPlayer.IsShown)
                        .Where(element => !IsInsideTheScreen(element, screen))
                        .Select(element => element.name + " <" + element.GetType().Name + ">")
                        .ToArray(),
                    Is.Empty,
                    "and NOTHING at all is drawn outside the screen: the user's rule for this " +
                    "milestone is \"Everything else should disappear and this pops up\", which " +
                    "covers the two layers that attach to this root lazily as well as the " +
                    "composed ones.");
                Assert.That(UiPlayer.Describe(hud), Is.Empty,
                    "There is no control left to press: the screen offers none, and everything " +
                    "that did offer one is hidden.");

                // ---------------------------------------------------------------------------------
                // and it fits, at the smallest supported desktop viewport and at the reference
                // ---------------------------------------------------------------------------------
                foreach (var viewport in new[] { (1280u, 720u), (1920u, 1080u), (2560u, 1440u) })
                {
                    UiPlayer.SetPlayerViewport(
                        viewport.Item1, viewport.Item2, "8.53 victory screen fit");
                    yield return null;
                    yield return null;
                    AssertFits(hud, viewport.Item1 + "x" + viewport.Item2);
                }

                Assert.That(runtime.DiagnosticEvents.Any(entry =>
                    entry.EventType.Contains("EngineFault")), Is.False);
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.53 restore");
            }
        }

        /// <summary>
        /// The screen's own panel is inside the viewport, and the LAST row of the LAST block is inside
        /// the panel - which is what "no clipping" means for a surface whose height is the sum of its
        /// blocks.
        /// </summary>
        internal static void AssertFits(ConfiguredMatchHudRenderer hud, string label)
        {
            var snap = UiPlayer.PixelSnapping();
            var panelSize = ConfiguredMatchHudRenderer.PanelViewport;
            var screen = Screen_(hud);
            var panel = UiPlayer.Descendants(screen)
                .Single(element => element.name == "gd-victory-panel");

            Assert.That(screen.worldBound.width, Is.EqualTo(panelSize.x).Within(snap),
                "The scrim is the whole viewport at " + label + ".");
            Assert.That(screen.worldBound.height, Is.EqualTo(panelSize.y).Within(snap));
            Assert.That(panel.worldBound.xMin, Is.GreaterThanOrEqualTo(-snap),
                "and the panel is inside it at " + label + ": " + panel.worldBound);
            Assert.That(panel.worldBound.yMin, Is.GreaterThanOrEqualTo(-snap),
                "and the panel is inside it at " + label + ": " + panel.worldBound);
            Assert.That(panel.worldBound.xMax, Is.LessThanOrEqualTo(panelSize.x + snap),
                "and the panel is inside it at " + label + ": " + panel.worldBound);
            Assert.That(panel.worldBound.yMax, Is.LessThanOrEqualTo(panelSize.y + snap),
                "and the panel is inside it at " + label + ": " + panel.worldBound);

            var rows = UiPlayer.Descendants(panel)
                .Where(element => element.name == "gd-victory-row")
                .ToArray();
            Assert.That(rows, Is.Not.Empty);
            foreach (var row in rows)
            {
                Assert.That(row.worldBound.yMax, Is.LessThanOrEqualTo(panel.worldBound.yMax + snap),
                    "Every hero row is inside the panel at " + label + " - the last one included, " +
                    "which is the one a screen that outgrew its box would lose: " + row.worldBound +
                    " against " + panel.worldBound);
                Assert.That(row.worldBound.xMax, Is.LessThanOrEqualTo(panel.worldBound.xMax + snap),
                    "and no column is cut off the right-hand edge at " + label + ".");
            }

            // Milestone 8.64, at the user's direction: "Remove this from victory screen." The band of
            // phrases that used to explain every column ("Level p.15's Hero Level as the match
            // ended", and seven more) is gone, and this fixture - which used to measure that it fitted
            // inside the panel - now states that nothing of it is drawn at all.
            Assert.That(
                UiPlayer.Descendants(panel).Where(element =>
                    element.name != null && element.name.StartsWith("gd-victory-legend")),
                Is.Empty,
                "Nothing under the tables explains their columns at " + label + ".");

            AssertTheTableIsAGrid(panel, label, snap);
        }

        /// <summary>
        /// Milestone 8.58. The user's capture of RED WINS: "the numbers do not sit under their headers"
        /// and "Coins earned runs past the panel's right edge".
        ///
        /// <para>Both were one arithmetic mistake. The hero cell was the only element in a row that
        /// could shrink and the header's could not, so every row's numbers slid out from under their own
        /// headers by exactly the eight units the table overflowed the panel by. The check is therefore
        /// the two facts together: every cell shares its header's left and right edge, and the last
        /// column is inside the panel - at every viewport, because the overflow was invisible at some of
        /// them.</para>
        /// </summary>
        private static void AssertTheTableIsAGrid(VisualElement panel, string label, float snap)
        {
            var blocks = UiPlayer.Descendants(panel)
                .Where(element => element.name == "gd-victory-block")
                .ToArray();
            Assert.That(blocks, Is.Not.Empty, "The screen draws at least one table at " + label + ".");

            foreach (var block in blocks)
            {
                var headers = UiPlayer.Descendants(block)
                    .Where(element => element.name == "gd-victory-head")
                    .ToArray();
                Assert.That(headers, Has.Length.EqualTo(1));
                var rows = UiPlayer.Descendants(block)
                    .Where(element => element.name == "gd-victory-row")
                    .ToArray();
                Assert.That(rows, Is.Not.Empty);

                foreach (var column in VictoryScreenLabels.Columns)
                {
                    var header = UiPlayer.Descendants(headers[0])
                        .Single(element => element.name == VictoryScreenLabels.HeaderCellName(column));
                    Assert.That(header.worldBound.xMax,
                        Is.LessThanOrEqualTo(panel.worldBound.xMax + snap),
                        "The " + VictoryScreenLabels.Header(column) + " header is inside the panel " +
                        "at " + label + ": " + header.worldBound + " against " + panel.worldBound);

                    // Milestone 8.64, at the user's direction on the shipped capture ("I think
                    // spacing is a bit off here"): one column means one BOX and one ANCHOR. The boxes
                    // already matched; both rows were anchored middle-right, so a one-digit number
                    // sat at the right edge of a box its long heading filled, and read as drifting.
                    Assert.That(header.resolvedStyle.unityTextAlign,
                        Is.EqualTo(TextAnchor.MiddleCenter),
                        "The " + VictoryScreenLabels.Header(column) + " heading is centred in its " +
                        "column at " + label + ".");

                    foreach (var row in rows)
                    {
                        var cell = UiPlayer.Descendants(row)
                            .Single(element => element.name == VictoryScreenLabels.CellName(column));
                        Assert.That(cell.resolvedStyle.unityTextAlign,
                            Is.EqualTo(header.resolvedStyle.unityTextAlign),
                            "and every number under it is anchored the same way, which is what puts " +
                            "it under its own heading at " + label + ".");
                        Assert.That(cell.worldBound.xMin,
                            Is.EqualTo(header.worldBound.xMin).Within(snap),
                            "The " + VictoryScreenLabels.Header(column) + " cell sits under its own " +
                            "header at " + label + ": " + cell.worldBound + " against " +
                            header.worldBound);
                        Assert.That(cell.worldBound.xMax,
                            Is.EqualTo(header.worldBound.xMax).Within(snap),
                            "on both edges, which is what a shared column width means.");
                        Assert.That(cell.worldBound.xMax,
                            Is.LessThanOrEqualTo(panel.worldBound.xMax + snap),
                            "and the last column is never cut off the panel's right edge at " +
                            label + ".");
                    }
                }
            }
        }

        internal static VisualElement Screen_(ConfiguredMatchHudRenderer hud)
        {
            var found = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-victory-screen")
                .ToArray();
            Assert.That(found, Has.Length.EqualTo(1),
                "The composed HUD holds exactly one victory screen.");
            return found[0];
        }

        internal static string Text(ConfiguredMatchHudRenderer hud, string name)
        {
            var labels = UiPlayer.Descendants(hud.RootElement)
                .OfType<Label>()
                .Where(element => element.name == name && UiPlayer.IsShown(element))
                .ToArray();
            Assert.That(labels, Has.Length.EqualTo(1),
                "Expected exactly one shown '" + name + "'.");
            return labels[0].text;
        }

        private static VisualElement Named(ConfiguredMatchHudRenderer hud, string name)
        {
            var found = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == name)
                .ToArray();
            Assert.That(found, Is.Not.Empty, "The composed HUD has a '" + name + "'.");
            return found[0];
        }

        private static bool IsInsideTheScreen(VisualElement element, VisualElement screen)
        {
            for (var current = element; current != null; current = current.hierarchy.parent)
            {
                if (ReferenceEquals(current, screen))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
