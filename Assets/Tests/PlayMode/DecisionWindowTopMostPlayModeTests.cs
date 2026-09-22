using System.Collections;
using GuardsDigital.Presentation.Interaction;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.SabinaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.SabinaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TargetingCuePhotographPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.63. The decision window is drawn above every other panel.
    ///
    /// <para><b>Why this fixture exists.</b> On 2026-09-19 the user sent a screenshot of the DEFEND
    /// AGAINST THIS ATTACK? window with another panel across it and said: "popup should be on top, not
    /// behind stuff :D". Sibling order alone decided that: the HUD composes its surfaces in one fixed
    /// order, and any layer attached to the root AFTER that composition - a lazily attached phase
    /// banner or attack spotlight, a drag proxy or a hover preview that brings itself to the front -
    /// painted above the window. This fixture drives the window a player is actually answering,
    /// opens the one popup that belongs to it, and states where in the live hierarchy the window's
    /// own host stands.</para>
    ///
    /// <para><b>The rule, stated once.</b> The window's host is the LAST SHOWN CHILD of the HUD root
    /// whenever the window is shown. Its own hover breakdown is not an exception to that rule - it is
    /// drawn INSIDE the window's host, so the one panel that has to be readable over the window is
    /// part of it rather than something else covering it.</para>
    /// </summary>
    public sealed class DecisionWindowTopMostPlayModeTests
    {
        private const string CaptureRoot = "tmp/863-captures";

        private static string Phase =>
            string.IsNullOrWhiteSpace(
                System.Environment.GetEnvironmentVariable("GUARDS_CAPTURE_PHASE"))
                ? "current"
                : System.Environment.GetEnvironmentVariable("GUARDS_CAPTURE_PHASE").Trim();

        /// <summary>
        /// The user's own screenshot, driven: a real p.14 Defense response, its window expanded, and
        /// the per-card breakdown open over it because the pointer is on a candidate.
        /// </summary>
        [UnityTest]
        public IEnumerator TheDefenseWindow_IsTopMostWithItsOwnBreakdownOpen_AndIsPhotographed()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                UiPlayer.SetPlayerViewport(1920, 1080, "8.63 defense capture");
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
                var root = Root();
                var runtime = root.Runtime;
                var sabinaId = SabinaSeat(runtime).HeroId;
                var enemyId = OtherHeroId(runtime, sabinaId);

                yield return WalkIntoContact(root, sabinaId, enemyId);
                Commit(runtime, enemyId, WaspCardContent.Shock);
                Commit(runtime, sabinaId, SabinaCardContent.BackToBack);
                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                    "Arrange: Shock's 8 takes the turn, so the attack is the enemy's.");
                Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
                Assert.That(
                    runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(sabinaId)),
                    Is.True);
                yield return null;
                yield return null;

                Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                    "Arrange: the attack reaches p.14's Defense response. " + Remaining(runtime));
                var window = Window(root);
                Assert.That(runtime.DecisionWindowView.Model.Family,
                    Is.EqualTo(DecisionWindowFamily.Defense));
                Assert.That(runtime.DecisionWindowView.Model.State,
                    Is.EqualTo(DecisionWindowState.Expanded));

                yield return UiPlayer.CaptureScreen(
                    CaptureRoot + "/defense-window-" + Phase + "-1920x1080.png");

                foreach (var viewport in new[] { (1920u, 1080u), (1280u, 720u), (2560u, 1440u) })
                {
                    UiPlayer.SetPlayerViewport(
                        viewport.Item1, viewport.Item2, "8.63 breakdown capture");
                    yield return null;
                    yield return null;
                    yield return null;

                    // The pointer on one candidate, which is what opens the breakdown the user's
                    // screenshot has across the window (milestone 8.58 kept it: "keep the old
                    // mouseover when mousing over the defense cards"). It is re-pointed at each
                    // viewport because the anchor it is placed against has moved.
                    var candidate = UiPlayer.Descendants(Window(root))
                        .First(element =>
                            element.name == "gd-defense-card" && UiPlayer.IsShown(element));
                    UiPlayer.PointAt(candidate);
                    yield return null;
                    yield return null;

                    var breakdown = Preview(root);
                    var label = "the live Defense window with its breakdown open at " +
                        viewport.Item1 + "x" + viewport.Item2;
                    Assert.That(UiPlayer.IsShown(breakdown), Is.True,
                        "Arrange: pointing at a candidate opens its breakdown at " + label + ".");

                    yield return UiPlayer.CaptureScreen(
                        CaptureRoot + "/defense-window-with-breakdown-" + Phase + "-" +
                        viewport.Item1 + "x" + viewport.Item2 + ".png");

                    AssertTheWindowIsTopMost(root, label);
                    Assert.That(IsDescendantOf(breakdown, Window(root)), Is.True,
                        "The window's own hover breakdown is drawn INSIDE the window, so the one " +
                        "panel that must stay readable over it is part of it rather than a sibling " +
                        "across it.");
                    AssertTheBreakdownReadsAsLines(breakdown, label);
                }
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.63 restore");
            }
        }

        // -----------------------------------------------------------------------------------------
        // The rule, shared with the 8.58 capture fixture so every window family it stages states it
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.63. The window's host is the last SHOWN child of the HUD root: nothing else on
        /// the panel paints over it while it is open.
        /// </summary>
        internal static void AssertTheWindowIsTopMost(MatchSceneCompositionRoot root, string label)
        {
            var hudRoot = root.Hud.RootElement;
            var window = hudRoot.Children().SingleOrDefault(child => child.name == "gd-decision-window");
            Assert.That(window, Is.Not.Null,
                "The decision window is a direct child of the HUD root at " + label + ".");
            Assert.That(UiPlayer.IsShown(window), Is.True, "and is shown at " + label + ".");

            var shownAfter = new List<string>();
            var reached = false;
            foreach (var child in hudRoot.Children())
            {
                if (ReferenceEquals(child, window))
                {
                    reached = true;
                    continue;
                }

                // The one-line tooltip and the enlarged card peek are BOTH the window's own reading of
                // whatever the pointer rests on, and both are hidden on every frame nothing is being
                // pointed at. They are allowed above the window for the same reason the window's own
                // hover breakdown is allowed inside it (milestone 8.63), the peek added at milestone
                // 8.74 after the user found it painting behind the window on the level-up hand.
                if (reached && UiPlayer.IsShown(child) &&
                    child.name != "gd-runtime-tooltip" &&
                    child.name != "gd-defense-card-hover-preview")
                {
                    shownAfter.Add(string.IsNullOrEmpty(child.name) ? "<unnamed>" : child.name);
                }
            }

            Assert.That(shownAfter, Is.Empty,
                "Nothing shown may be painted after the decision window at " + label +
                "; these are: " + string.Join(", ", shownAfter));
        }

        /// <summary>
        /// Milestone 8.63, at the user's direction on the captured breakdown ("spacing is a bit
        /// off"): EVERY LINE OF IT IS ITS OWN LINE.
        ///
        /// <para>The panel's height was a flat 240 units and its contribution rows kept UI Toolkit's
        /// default <c>flex-shrink: 1</c>, so a breakdown with more lines than that cap held had its
        /// rows squeezed while the labels inside them - built with <c>flex-shrink: 0</c> - kept their
        /// size and painted over the line below. This states the outcome a person can see rather than
        /// the styles behind it: the lines are in order, no two of them overlap, each has a real
        /// height, and none of them leaves the panel that holds them.</para>
        /// </summary>
        internal static void AssertTheBreakdownReadsAsLines(VisualElement breakdown, string label)
        {
            var snap = UiPlayer.PixelSnapping();
            var lineNames = new[]
            {
                "gd-defense-preview-title",
                "gd-defense-section",
                "gd-defense-contribution-row",
                "gd-defense-attack-total",
                "gd-defense-defense-total",
                "gd-defense-result"
            };
            var lines = UiPlayer.Descendants(breakdown)
                .Where(element => element.name != null && lineNames.Contains(element.name))
                .OrderBy(element => element.worldBound.yMin)
                .ToArray();
            Assert.That(lines, Has.Length.GreaterThanOrEqualTo(6),
                "The breakdown draws a title, two headings, two totals and a verdict at " + label +
                "; it drew " + lines.Length + " lines.");
            Assert.That(
                lines.Count(element => element.name == "gd-defense-contribution-row"),
                Is.GreaterThan(0),
                "and at least one contribution line at " + label + ".");

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                Assert.That(line.worldBound.height, Is.GreaterThan(1f),
                    "Every line of the breakdown has a real height at " + label + ": '" +
                    line.name + "' measured " + line.worldBound);
                Assert.That(line.worldBound.yMax, Is.LessThanOrEqualTo(breakdown.worldBound.yMax + snap),
                    "and is inside the panel that holds it at " + label + " - nothing clips off its " +
                    "bottom edge: '" + line.name + "' " + line.worldBound + " against " +
                    breakdown.worldBound);
                Assert.That(line.worldBound.xMax, Is.LessThanOrEqualTo(breakdown.worldBound.xMax + snap),
                    "or off its right edge at " + label + ": '" + line.name + "' " + line.worldBound);

                if (index == 0)
                {
                    continue;
                }

                var above = lines[index - 1];
                Assert.That(line.worldBound.yMin, Is.GreaterThanOrEqualTo(above.worldBound.yMax - snap),
                    "NO TWO LINES OF THE BREAKDOWN OVERLAP at " + label + ": '" + above.name +
                    "' " + above.worldBound + " runs into '" + line.name + "' " + line.worldBound);
            }

            foreach (var row in lines.Where(element => element.name == "gd-defense-contribution-row"))
            {
                var source = UiPlayer.Descendants(row)
                    .Single(element => element.name == "gd-defense-contribution-source");
                var amount = UiPlayer.Descendants(row)
                    .Single(element => element.name == "gd-defense-contribution-amount");
                Assert.That(amount.worldBound.xMin,
                    Is.GreaterThanOrEqualTo(source.worldBound.xMin - snap),
                    "The label is on the left of its own line and the signed value on the right at " +
                    label + ": " + source.worldBound + " against " + amount.worldBound);
                Assert.That(amount.worldBound.xMax,
                    Is.LessThanOrEqualTo(breakdown.worldBound.xMax + snap),
                    "and the value is inside the panel rather than crowding its edge at " + label + ".");
            }
        }

        internal static bool IsDescendantOf(VisualElement candidate, VisualElement ancestor)
        {
            for (var current = candidate; current != null; current = current.hierarchy.parent)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        private static VisualElement Window(MatchSceneCompositionRoot root)
        {
            var found = UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-decision-window")
                .ToArray();
            Assert.That(found, Has.Length.EqualTo(1));
            Assert.That(UiPlayer.IsShown(found[0]), Is.True);
            return found[0];
        }

        private static VisualElement Preview(MatchSceneCompositionRoot root)
        {
            var found = UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-defense-preview")
                .ToArray();
            Assert.That(found, Has.Length.EqualTo(1),
                "Exactly one defense breakdown layer exists on the HUD.");
            return found[0];
        }
    }
}
