using System.Collections;
using GuardsDigital.Presentation.Interaction;
using System.Linq;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.DecisionWindowMockupCapturePlayModeTests;
using static GuardsDigital.Tests.PlayMode.SabinaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TargetingCuePhotographPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.64. The victory screen's two variants, photographed, with nothing under the tables
    /// explaining their columns.
    ///
    /// <para><b>Why.</b> On 2026-09-19 the user looked at the shipped victory screen and said of the
    /// band of phrases under the two tables - "Level p.15's Hero Level as the match ended · Kills
    /// enemy heroes this hero defeated · ..." - "Remove this from victory screen." The headers and the
    /// numbers stay; the glossary goes, and the space goes back to the tables.</para>
    /// </summary>
    public sealed class VictoryScreenPhotographPlayModeTests
    {
        private const string CaptureRoot = "tmp/864-captures";

        private static string Phase =>
            string.IsNullOrWhiteSpace(
                System.Environment.GetEnvironmentVariable("GUARDS_CAPTURE_PHASE"))
                ? "current"
                : System.Environment.GetEnvironmentVariable("GUARDS_CAPTURE_PHASE").Trim();

        [UnityTest]
        public IEnumerator TheVictoryScreen_ExplainsNoColumns_AndIsPhotographed()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadEngagedMirror();
                var root = Root();
                var runtime = root.Runtime;
                UiPlayer.SetPlayerViewport(1920, 1080, "8.64 victory captures");
                yield return null;
                yield return null;

                // Both variants are PHOTOGRAPHED first and checked afterwards, so a run against a
                // build that fails the check still leaves the full pair of frames for a person to
                // look at - which is the whole point of a photograph fixture.
                var variants = new[]
                {
                    ("victory-team-win", TeamWin(runtime)),
                    ("victory-solo-win", SoloWin(runtime))
                };

                foreach (var variant in variants)
                {
                    runtime.VictoryScreenView.Render(variant.Item2);
                    // The renderer sizes the scrim and hides the HUD behind it on its own pass.
                    yield return null;
                    yield return null;
                    yield return null;

                    yield return UiPlayer.CaptureScreen(
                        CaptureRoot + "/" + variant.Item1 + "-" + Phase + "-1920x1080.png");
                }

                foreach (var variant in variants)
                {
                    runtime.VictoryScreenView.Render(variant.Item2);
                    yield return null;
                    yield return null;
                    yield return null;

                    var label = variant.Item1 + " at 1920x1080";
                    VictoryScreenUiJourneyPlayModeTests.AssertFits(root.Hud, label);
                    AssertNoColumnLegend(root, label);
                }

                runtime.VictoryScreenView.Render(null);
                yield return null;
                yield return null;
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.64 restore");
            }
        }

        /// <summary>
        /// Nothing on the screen explains a column: no legend band, and none of the glossary's own
        /// phrases is drawn anywhere on it. Milestone 8.64, at the user's direction.
        /// </summary>
        internal static void AssertNoColumnLegend(MatchSceneCompositionRoot root, string label)
        {
            var screen = UiPlayer.Descendants(root.Hud.RootElement)
                .Single(element => element.name == "gd-victory-screen");
            Assert.That(
                UiPlayer.Descendants(screen).Where(element =>
                    element.name != null && element.name.StartsWith("gd-victory-legend")),
                Is.Empty,
                "The victory screen draws no legend band at " + label + ".");

            var phrases = UiPlayer.Descendants(screen).OfType<Label>()
                .Select(element => element.text ?? string.Empty)
                .Where(text => text.Contains("p.15's Hero Level") ||
                    text.Contains("enemy heroes this hero defeated") ||
                    text.Contains("what defeating this hero pays and costs"))
                .ToArray();
            Assert.That(phrases, Is.Empty,
                "and none of the retired glossary's phrases is printed at " + label +
                "; found: " + string.Join(" | ", phrases));
        }
    }
}
