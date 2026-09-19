using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.58. One photograph of every window the user reviewed, at three viewports, beside the
    /// assertions that say what the mockup's own frame holds.
    ///
    /// <para><b>Why this fixture exists.</b> On 2026-09-19 the user looked at five of these windows in
    /// the packaged build and said: "I need you to verify these new windows against their mockups and
    /// make sure they are ok. None of them were good as it stands now." A suite that only measures
    /// rectangles could not have caught any of what they saw - an empty box, the old buttons, a line
    /// printed twice, a table whose numbers were not under their headers - so this one both measures the
    /// mockup's own facts and writes the frame to <c>tmp/858-captures/</c> for a person to look at.</para>
    ///
    /// <para><b>Why the models are staged.</b> The subject here is what the window LOOKS LIKE, at three
    /// viewports, for eleven families at once. Driving eleven real pauses through a live match would
    /// make the photograph depend on which decision a walk reached rather than on the drawing, which is
    /// the same reason <c>DecisionWindowGeometryPlayModeTests</c> stages its two. Every model below is
    /// the shape <c>DecisionWindowPresenter</c> produces - proved against real pauses, in
    /// <c>DecisionWindowPresenterTests</c> - built from the REAL cards the live scene is holding, so the
    /// faces, the artwork and the scaling in these photographs are the build's own.</para>
    /// </summary>
    public sealed class DecisionWindowMockupCapturePlayModeTests
    {
        private const string CaptureRoot = "tmp/858-captures";

        private static IEnumerable<(uint Width, uint Height)> Viewports() =>
            new[] { (1280u, 720u), (1920u, 1080u), (2560u, 1440u) };

        /// <summary>
        /// Every window family the user listed, drawn and photographed. The assertions are the mockup's
        /// own checklist, family by family: the card is there, the controls are the one style, nothing
        /// of the strip shows through, no line is printed twice, and the tray is where it belongs.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryDecisionWindow_HoldsWhatItsMockupFrameHolds_AndIsPhotographed()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadEngagedScene();
                var root = Root();
                var runtime = root.Runtime;

                foreach (var viewport in Viewports())
                {
                    UiPlayer.SetPlayerViewport(
                        viewport.Width, viewport.Height, "8.58 window captures");
                    yield return null;
                    yield return null;

                    foreach (var frame in Frames(runtime))
                    {
                        runtime.DecisionWindowView.Render(frame.Model);
                        yield return null;
                        yield return null;

                        var label = frame.Name + " at " + viewport.Width + "x" + viewport.Height;
                        AssertTheStripAndTheRailReadTheirNumbers(root, label);
                        AssertTheOneControlStyle(root, label);
                        AssertNoStripBannerShowsThrough(root, label);
                        AssertNothingIsPrintedTwice(root, label);
                        AssertTheBodyIsCentredInThePanel(root, label);
                        AssertTheDockedBarFinishesItsLines(root, label);
                        frame.Verify(root, label);

                        yield return UiPlayer.CaptureScreen(
                            CaptureRoot + "/" + frame.Name + "-" +
                            viewport.Width + "x" + viewport.Height + ".png");
                    }
                }
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.58 restore");
            }
        }

        /// <summary>
        /// The victory screen's two variants - the mockup's "a team wins" and "Cutter wins alone" - and
        /// the grid the user's capture showed was not one.
        /// </summary>
        [UnityTest]
        public IEnumerator TheVictoryScreen_IsAGridInBothVariants_AndIsPhotographed()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadEngagedScene();
                var root = Root();
                var runtime = root.Runtime;

                foreach (var viewport in Viewports())
                {
                    UiPlayer.SetPlayerViewport(
                        viewport.Width, viewport.Height, "8.58 victory captures");
                    yield return null;
                    yield return null;

                    foreach (var variant in new[]
                    {
                        ("victory-team-win", TeamWin(runtime)),
                        ("victory-solo-win", SoloWin(runtime))
                    })
                    {
                        runtime.VictoryScreenView.Render(variant.Item2);
                        // The renderer sizes the scrim and hides the HUD behind it on its own pass.
                        yield return null;
                        yield return null;
                        yield return null;

                        var label = variant.Item1 + " at " + viewport.Width + "x" + viewport.Height;
                        VictoryScreenUiJourneyPlayModeTests.AssertFits(root.Hud, label);
                        yield return UiPlayer.CaptureScreen(
                            CaptureRoot + "/" + variant.Item1 + "-" +
                            viewport.Width + "x" + viewport.Height + ".png");
                    }

                    runtime.VictoryScreenView.Render(null);
                    yield return null;
                    yield return null;
                }
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.58 restore");
            }
        }

        // -----------------------------------------------------------------------------------------
        // The mockup's cross-cutting rules, asked of every frame
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.58. The two small numbers the user said they could not read: the rail's worth
        /// badge ("Hard to see this number one here") and the strip's Life counters, which are one
        /// heart per team with the count inside now instead of a dot and a word.
        ///
        /// <para>Measured on the LIVE panel at every viewport, because "big enough" is a fact about
        /// drawn pixels rather than about a declared font size.</para>
        /// </summary>
        private static void AssertTheStripAndTheRailReadTheirNumbers(
            MatchSceneCompositionRoot root, string label)
        {
            var hearts = UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-life-counter" && UiPlayer.IsShown(element))
                .ToArray();
            Assert.That(hearts, Has.Length.EqualTo(2),
                "One heart per team on the strip at " + label + ".");
            foreach (var heart in hearts)
            {
                var glyph = UiPlayer.Descendants(heart).OfType<Label>()
                    .Single(element => element.name == "gd-life-counter-heart");
                Assert.That(glyph.text, Is.EqualTo("♥"));
                Assert.That(glyph.style.color.value,
                    Is.EqualTo(MatchHudTheme.TeamRed).Or.EqualTo(MatchHudTheme.TeamBlue),
                    "filled in its own team's colour at " + label + ".");
                var digit = UiPlayer.Descendants(heart).OfType<Label>()
                    .Single(element => element.name == "gd-life-counter-value");
                Assert.That(digit.text, Does.Not.Contain("RED").And.Not.Contain("BLUE"),
                    "The words are gone; the colour says which team it is. " + label);
                Assert.That(digit.worldBound.height, Is.GreaterThanOrEqualTo(12f),
                    "and the count inside it is readable at " + label + ": " + digit.worldBound);
                Assert.That(
                    Mathf.Abs(digit.worldBound.center.x - glyph.worldBound.center.x),
                    Is.LessThanOrEqualTo(UiPlayer.PixelSnapping(2)),
                    "centred in the heart at " + label + ".");
            }

            foreach (var badge in UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-hero-worth" && UiPlayer.IsShown(element)))
            {
                Assert.That(badge.Children().Any(child => child.name == "gd-hero-worth-ink"),
                    Is.False,
                    "No disc behind the worth digit - both coloured halves stay visible. " + label);
                var digit = UiPlayer.Descendants(badge).OfType<Label>()
                    .Single(element => element.name == "gd-hero-worth-value");
                Assert.That(digit.worldBound.height, Is.GreaterThanOrEqualTo(12f),
                    "The worth badge's digit is readable at " + label + ": " + digit.worldBound);
            }
        }

        /// <summary>
        /// "Old buttons here instead of new ones" - the user, twice. There is one control style in this
        /// window, and the strip's pewter swallowtail is not it.
        /// </summary>
        private static void AssertTheOneControlStyle(MatchSceneCompositionRoot root, string label)
        {
            var window = Window(root);
            Assert.That(
                UiPlayer.Descendants(window)
                    .Where(element => element.Q("gd-banner-cloth") != null)
                    .Select(element => element.name)
                    .ToArray(),
                Is.Empty,
                "A pewter banner is drawn inside the decision window at " + label + ".");

            foreach (var control in UiPlayer.Descendants(window).OfType<Button>()
                .Where(element => element.name == "gd-decision-control" && UiPlayer.IsShown(element)))
            {
                Assert.That(control.style.backgroundColor.value,
                    Is.EqualTo(MatchHudTheme.ButtonIdleBackground),
                    "'" + control.text + "' wears the shared chrome at " + label + ".");
                Assert.That(control.worldBound.width, Is.GreaterThan(0f));
                Assert.That(control.worldBound.height, Is.GreaterThan(0f));
            }
        }

        /// <summary>
        /// "nothing of the strip's optional-step/agreement controls may remain visible while the window
        /// owns the decision" - the pewter banner the user's docked Minion Battle capture showed
        /// peeking out under <c>EXPAND</c>.
        /// </summary>
        private static void AssertNoStripBannerShowsThrough(
            MatchSceneCompositionRoot root, string label)
        {
            foreach (var name in new[]
            {
                "gd-optional-step-accept", "gd-optional-step-decline", "gd-decline-defense",
                "gd-initiative-preference", "gd-agreement-waiting"
            })
            {
                foreach (var element in UiPlayer.Descendants(root.Hud.RootElement)
                    .Where(candidate => candidate.name == name))
                {
                    Assert.That(UiPlayer.IsShown(element), Is.False,
                        "The strip still draws '" + name + "' while the window owns the decision, at " +
                        label + ".");
                }
            }
        }

        /// <summary>
        /// "the expanded Minion Battle window repeating 'Sabina the Gunslinger is ready.' twice" - one
        /// line, never the same sentence as both the question and a status line under it.
        /// </summary>
        private static void AssertNothingIsPrintedTwice(
            MatchSceneCompositionRoot root, string label)
        {
            // The window's own SENTENCES - the eyebrow, the title, the question, the count and any
            // statement under them. A card's caption repeating the title is not what the user saw (a
            // card chooser titled SHOCK legitimately offers Shock among its candidates); what they saw
            // was one sentence drawn as both the question and a status line beneath it.
            var sentences = new[]
            {
                "gd-decision-window-eyebrow", "gd-decision-window-title",
                "gd-decision-window-question", "gd-decision-window-count",
                "gd-decision-window-statement", "gd-decision-window-docked-title",
                "gd-decision-window-docked-question", "gd-decision-window-docked-count"
            };
            var lines = UiPlayer.Descendants(Window(root)).OfType<Label>()
                .Where(element => sentences.Contains(element.name) && UiPlayer.IsShown(element) &&
                    !string.IsNullOrWhiteSpace(element.text))
                .Select(element => element.text.Trim())
                .ToArray();
            var repeated = lines
                .GroupBy(text => text)
                .Where(group => group.Count() > 1)
                .Select(group => "\"" + group.Key + "\" x" + group.Count())
                .ToArray();
            Assert.That(repeated, Is.Empty,
                "The window prints the same sentence more than once at " + label + ": " +
                string.Join(", ", repeated));
        }

        private static void AssertCardFaceIsTheTraysOwn(
            MatchSceneCompositionRoot root, string elementName, string label)
        {
            var hosts = UiPlayer.Descendants(Window(root))
                .Where(element => element.name == elementName && UiPlayer.IsShown(element))
                .ToArray();
            Assert.That(hosts, Is.Not.Empty,
                "The window draws no '" + elementName + "' at " + label + " - the card the user asked " +
                "for by name (\"We said we will show the card\").");
            foreach (var host in hosts)
            {
                var face = UiPlayer.Descendants(host)
                    .First(element => element.name == "gd-decision-window-face");
                Assert.That(face.worldBound.width,
                    Is.EqualTo(CardFootprint.Width).Within(UiPlayer.PixelSnapping()),
                    "and it is the tray's own card at " + label + ": \"make sure you don't change " +
                    "the scaling on the cards\".");
                Assert.That(face.worldBound.height,
                    Is.EqualTo(CardFootprint.Height).Within(UiPlayer.PixelSnapping()));
            }
        }

        /// <summary>The panel is sized to its content and centred inside the rectangle, never both.</summary>
        private static void AssertThePanelIsContentSized(
            MatchSceneCompositionRoot root, string label, float mostOfTheRectangle)
        {
            var bound = Window(root).worldBound;
            var panel = UiPlayer.Descendants(Window(root))
                .Single(element => element.name == "gd-decision-window-expanded");
            Assert.That(UiPlayer.IsShown(panel), Is.True);
            Assert.That(panel.worldBound.width,
                Is.LessThanOrEqualTo((bound.width * mostOfTheRectangle) + UiPlayer.PixelSnapping()),
                "The panel is sized to its content at " + label + " - \"This box is a waste of space " +
                "like this\" - rather than to the whole rectangle: " + panel.worldBound + " in " +
                bound);
            Assert.That(panel.worldBound.xMin, Is.GreaterThanOrEqualTo(bound.xMin - UiPlayer.PixelSnapping()));
            Assert.That(panel.worldBound.xMax, Is.LessThanOrEqualTo(bound.xMax + UiPlayer.PixelSnapping()));
            Assert.That(panel.worldBound.yMin, Is.GreaterThanOrEqualTo(bound.yMin - UiPlayer.PixelSnapping()));
            Assert.That(panel.worldBound.yMax, Is.LessThanOrEqualTo(bound.yMax + UiPlayer.PixelSnapping()));
            Assert.That(
                Mathf.Abs(panel.worldBound.center.x - bound.center.x),
                Is.LessThanOrEqualTo(UiPlayer.PixelSnapping(2)),
                "and centred in it at " + label + ".");
        }

        private static void AssertTheTrayIsOrdinary(MatchSceneCompositionRoot root, string label)
        {
            var tray = root.Runtime.HandsView.Root;
            Assert.That(UiPlayer.IsShown(tray), Is.True,
                "The hand stays visible at " + label + ": \"hand should not disappear on these " +
                "choices, it makes this bar float strangely in the air\".");
            Assert.That(tray.worldBound.height, Is.GreaterThan(0f));
        }

        /// <summary>
        /// Milestone 8.58, the user on the optional-step capture: "This drifted a bit to the right."
        ///
        /// <para>The gap left of the card and the gap right of the answers are ONE gap. They were not:
        /// the panel measured its own width from a header carrying `MINIMISE` on one side only, and a
        /// control row that grew into whatever width was left over put its centre where the leftover
        /// space ended rather than on the panel's centre. Asked of every expanded family, because
        /// every family has that header and that row.</para>
        /// </summary>
        private static void AssertTheBodyIsCentredInThePanel(
            MatchSceneCompositionRoot root, string label)
        {
            var panel = UiPlayer.Descendants(Window(root))
                .Single(element => element.name == "gd-decision-window-expanded");
            if (!UiPlayer.IsShown(panel))
            {
                return;
            }

            var body = UiPlayer.Descendants(panel)
                .Single(element => element.name == "gd-decision-window-body");
            var drawn = body.Children()
                .Where(element => UiPlayer.IsShown(element) && element.worldBound.width > 0f)
                .ToArray();
            Assert.That(drawn, Is.Not.Empty, "The body draws something at " + label + ".");
            var leftGap = drawn.Min(element => element.worldBound.xMin) - panel.worldBound.xMin;
            var rightGap = panel.worldBound.xMax - drawn.Max(element => element.worldBound.xMax);
            Assert.That(Mathf.Abs(leftGap - rightGap),
                Is.LessThanOrEqualTo(UiPlayer.PixelSnapping(3)),
                "The body is centred in the panel at " + label + " - \"This drifted a bit to the " +
                "right\" - but the gap left of it is " + leftGap + " and the gap right of it is " +
                rightGap + ".");
        }

        /// <summary>
        /// Milestone 8.58. Nothing on the docked bar is cut off with an ellipsis.
        ///
        /// <para>The user's capture showed both bars losing the end of their second line - `1 of 2
        /// chosen - Click 2 minions on the...`, `PAID 2 COINS - 5 REMAINING - RED WASP REACHED...`.
        /// The answer was both halves at once: the lines are short by construction now, and the bar
        /// is the mockup's own width. This is what proves neither half was enough on its own, and it
        /// is measured at 1280x720 as well, which is where the bar is tightest.</para>
        /// </summary>
        private static void AssertTheDockedBarFinishesItsLines(
            MatchSceneCompositionRoot root, string label)
        {
            var bar = UiPlayer.Descendants(Window(root))
                .Single(element => element.name == "gd-decision-window-docked");
            if (!UiPlayer.IsShown(bar))
            {
                return;
            }

            foreach (var line in UiPlayer.Descendants(bar).OfType<Label>()
                .Where(element => element.name != null &&
                    element.name.StartsWith("gd-decision-window-docked-") &&
                    UiPlayer.IsShown(element) && !string.IsNullOrEmpty(element.text)))
            {
                var wanted = line.MeasureTextSize(
                    line.text,
                    0f, VisualElement.MeasureMode.Undefined,
                    0f, VisualElement.MeasureMode.Undefined);
                Assert.That(line.worldBound.width,
                    Is.GreaterThanOrEqualTo(wanted.x - UiPlayer.PixelSnapping(2)),
                    "'" + line.text + "' is cut off on the bar at " + label + ": it is drawn " +
                    line.worldBound.width + " wide and needs " + wanted.x + ".");
            }
        }

        // -----------------------------------------------------------------------------------------
        // The frames
        // -----------------------------------------------------------------------------------------

        private sealed class Frame
        {
            public Frame(
                string name,
                DecisionWindowViewModel model,
                System.Action<MatchSceneCompositionRoot, string> verify)
            {
                Name = name;
                Model = model;
                Verify = verify;
            }

            public string Name { get; }
            public DecisionWindowViewModel Model { get; }
            public System.Action<MatchSceneCompositionRoot, string> Verify { get; }
        }

        private static IEnumerable<Frame> Frames(ConfiguredMatchSceneRuntime runtime)
        {
            var hand = runtime.HandsView.Model.ViewedHand.TrayCards.Take(5).ToArray();
            Assert.That(hand, Has.Length.EqualTo(5),
                "The staged hand holds five faces for the defense frame's four candidates.");

            yield return new Frame("mode-choice", ModeChoice(hand), (root, label) =>
            {
                AssertCardFaceIsTheTraysOwn(root, "gd-decision-source-card", label);
                AssertThePanelIsContentSized(root, label, 0.9f);
                AssertTheTrayIsOrdinary(root, label);
                AssertLabels(root, label,
                    DeclaredChosen(SwiftCardContent.Reload, "reload-borrow-arm"),
                    DeclaredChosen(SwiftCardContent.Reload, "reload-defeat-arm"));
            });

            yield return new Frame("optional-step", OptionalStep(hand), (root, label) =>
            {
                AssertCardFaceIsTheTraysOwn(root, "gd-decision-source-card", label);
                // "a yes-or-no window should not pretend to have more to show than it does."
                AssertThePanelIsContentSized(root, label, 0.72f);
                AssertLabels(root, label,
                    DeclaredChosen(WaspCardContent.LiftUp, "orbit-repeat"),
                    DeclaredDeclined(WaspCardContent.LiftUp, "orbit-repeat"));
            });

            yield return new Frame("card-chooser", CardChooser(hand), (root, label) =>
            {
                AssertCardFaceIsTheTraysOwn(root, "gd-decision-source-card", label);
                AssertCardFaceIsTheTraysOwn(root, "gd-effect-card", label);
            });

            yield return new Frame("tied-initiative", TiedInitiative(hand), (root, label) =>
            {
                AssertCardFaceIsTheTraysOwn(root, "gd-decision-window-card", label);
                AssertTheTrayIsOrdinary(root, label);
            });

            yield return new Frame("defense", Defense(hand), (root, label) =>
            {
                AssertCardFaceIsTheTraysOwn(root, "gd-decision-attack-card", label);
                AssertCardFaceIsTheTraysOwn(root, "gd-defense-card", label);
                var divider = UiPlayer.Descendants(Window(root))
                    .Single(element => element.name == "gd-decision-window-divider");
                Assert.That(UiPlayer.IsShown(divider), Is.True,
                    "The attack is kept apart from the offer by the mockup's own rule at " + label + ".");
                var chips = UiPlayer.Descendants(Window(root)).OfType<Label>()
                    .Where(element => element.name == "gd-decision-window-card-chip")
                    .Select(element => element.text)
                    .ToArray();
                Assert.That(chips, Has.Length.EqualTo(4), "one chip per candidate at " + label + ".");
                Assert.That(chips.Count(text => text.Contains("HOLDS")), Is.EqualTo(1),
                    "and the one that would stop the attack says so at " + label + ".");
                // Milestone 8.58, the user's LAST word on the inline arithmetic, after asking for it:
                // "Remove it and keep the old mouseover when mousing over the defense cards." The
                // window states the attack and each candidate's verdict as chips and says no more; the
                // per-contribution reading is the hover breakdown's, on the layer that has drawn it
                // since 8.11b. (That the hover still opens from inside the window is proved on a LIVE
                // defense pause in `WaspEngagedUiJourneyPlayModeTests`, where there is a real
                // `PendingDefensePreview` to break down.)
                Assert.That(
                    UiPlayer.Descendants(Window(root)).OfType<Label>()
                        .Where(element => element.name == "gd-decision-window-statement" &&
                            UiPlayer.IsShown(element)),
                    Is.Empty,
                    "No modifiers section is printed in the panel at " + label + ".");
                var corner = UiPlayer.Descendants(Window(root)).OfType<Button>()
                    .Single(element => element.name == "gd-decision-window-corner-control");
                Assert.That(corner.text, Is.EqualTo(DecisionWindowPresenter.TakeTheHitLabel));
                Assert.That(corner.worldBound.xMax,
                    Is.GreaterThan(Window(root).worldBound.center.x),
                    "Declining is the quiet control in the corner at " + label + ".");
            });

            yield return new Frame("removal-prevention", RemovalPrevention(hand), (root, label) =>
            {
                AssertCardFaceIsTheTraysOwn(root, "gd-decision-attack-card", label);
                AssertCardFaceIsTheTraysOwn(root, "gd-effect-card", label);
            });

            yield return new Frame("minion-battle-docked", MinionBattle(docked: true, ready: false),
                (root, label) =>
                {
                    AssertTheTrayIsOrdinary(root, label);
                    var verb = UiPlayer.Descendants(Window(root)).OfType<Button>()
                        .Single(element => element.name == "gd-decision-control");
                    Assert.That(verb.text,
                        Is.EqualTo(DecisionWindowPresenter.RemoveTheseMinionsLabel));
                    Assert.That(verb.enabledSelf, Is.False,
                        "and it is disabled until the count is met at " + label + ", as the mockup " +
                        "draws it.");
                });

            yield return new Frame("minion-battle-expanded", MinionBattle(docked: false, ready: true),
                (root, label) =>
                {
                    var verb = UiPlayer.Descendants(Window(root)).OfType<Button>()
                        .Single(element => element.name == "gd-decision-control");
                    Assert.That(verb.enabledSelf, Is.True,
                        "The one verb answers once the count is met at " + label + ".");
                });

            yield return new Frame("level-up-expanded", LevelUp(hand, DecisionWindowState.Expanded),
                (root, label) =>
                {
                    Assert.That(
                        UiPlayer.Descendants(Window(root))
                            .Count(element => element.name == "gd-upgrade-option"),
                        Is.EqualTo(6),
                        "p.15's six candidates at " + label + ".");
                    // Milestone 8.58, the user: "hand can be removed from bottom left and shown below
                    // the offer window, like it was before." The offer leads and the hand follows it,
                    // inside the panel - and the tray underneath is taken away while it does, so the
                    // same five cards are not on screen twice.
                    var band = UiPlayer.Descendants(Window(root))
                        .Single(element => element.name == "gd-upgrade-comparison-band");
                    var preview = UiPlayer.Descendants(Window(root))
                        .Where(element => element.name == "gd-levelup-hand-preview-card" &&
                            UiPlayer.IsShown(element))
                        .ToArray();
                    Assert.That(preview, Is.Not.Empty,
                        "The panel draws the levelling hero's hand at " + label + ".");
                    Assert.That(preview.Min(card => card.worldBound.yMin),
                        Is.GreaterThanOrEqualTo(band.worldBound.yMax - UiPlayer.PixelSnapping(2)),
                        "BELOW the six candidates at " + label + ".");
                    Assert.That(UiPlayer.IsShown(root.Runtime.HandsView.Root), Is.False,
                        "and the tray at the bottom left is gone while it is drawn at " + label + ".");
                    // The same content-sizing rule every other family follows: "No reason for this
                    // bigger box when we have smaller one already."
                    AssertThePanelIsContentSized(root, label, 1f);
                    var panel = UiPlayer.Descendants(Window(root))
                        .Single(element => element.name == "gd-decision-window-expanded");
                    Assert.That(
                        panel.worldBound.yMax - preview.Max(card => card.worldBound.yMax),
                        // The panel's own padding and border, plus at most one row gap: what this
                        // measures is that there is no BAND of empty panel under the offer - the
                        // "waste of space" was hundreds of units, not tens.
                        Is.LessThanOrEqualTo(
                            DecisionWindowUnityView.WindowPadding +
                                DecisionWindowUnityView.PanelBorderWidth +
                                DecisionWindowUnityView.LevelUpRowGap +
                                UiPlayer.PixelSnapping(4)),
                        "The panel ends where its content ends at " + label + ": " + panel.worldBound);
                });

            yield return new Frame("level-up-docked", LevelUp(hand, DecisionWindowState.Docked),
                (root, label) => AssertTheTrayIsOrdinary(root, label));

            yield return new Frame("direction-on-the-board", Direction(), (root, label) =>
            {
                AssertTheTrayIsOrdinary(root, label);
                Assert.That(
                    ConfiguredMatchHudRenderer.CalculatePlayfieldRect(
                            ConfiguredMatchHudRenderer.PanelViewport.x,
                            ConfiguredMatchHudRenderer.PanelViewport.y)
                        .Overlaps(Window(root).worldBound),
                    Is.False,
                    "A question the board answers never covers the board at " + label + ".");
            });
        }

        private static void AssertLabels(
            MatchSceneCompositionRoot root, string label, params string[] expected)
        {
            var drawn = UiPlayer.Descendants(Window(root)).OfType<Button>()
                .Where(element => element.name == "gd-decision-control" && UiPlayer.IsShown(element))
                .Select(element => element.text)
                .ToArray();
            Assert.That(drawn, Is.EquivalentTo(expected),
                "The controls say what pressing them does, in the card's own printed words, at " +
                label + ". Drawn: " + string.Join(" | ", drawn));
        }

        // -----------------------------------------------------------------------------------------
        // Staged models, each the shape DecisionWindowPresenter produces for that family
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A card's "Choose one", drawn on the face the staged scene is holding.
        ///
        /// <para>The mirrored staging is Wasp's and she prints no <c>Choose one</c>, so the two CONTROLS
        /// here are Swift's Reload! - the pair the user photographed, and the longest declared label in
        /// the content, which is also what makes this the width case. The same window on Reload!'s own
        /// face, reached by real play, is captured by
        /// <c>SwiftProgressedUiJourneyPlayModeTests.ReloadOffersItsTwoBulletsAndDefeatsAnAdjacentMinion</c>
        /// as <c>mode-choice-reload-live</c>.</para>
        /// </summary>
        private static DecisionWindowViewModel ModeChoice(IReadOnlyList<TrayCardViewModel> hand) =>
            new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.Mode,
                DecisionWindowState.Expanded,
                "8.58-capture-mode",
                hand[1].DisplayName.ToUpperInvariant(),
                "MODE CHOICE",
                "Pick one of the two printed options.",
                contextCards: new[] { new DecisionWindowCardViewModel(hand[1]) },
                controls: new[]
                {
                    new DecisionWindowControlViewModel(
                        DeclaredChosen(SwiftCardContent.Reload, "reload-borrow-arm"),
                        DirectorCommand.ReleaseTurn()),
                    new DecisionWindowControlViewModel(
                        DeclaredChosen(SwiftCardContent.Reload, "reload-defeat-arm"),
                        DirectorCommand.ReleaseTurn())
                });

        private static DecisionWindowViewModel OptionalStep(IReadOnlyList<TrayCardViewModel> hand) =>
            new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.OptionalStep,
                DecisionWindowState.Expanded,
                "8.58-capture-optional",
                hand[0].DisplayName.ToUpperInvariant() + "?",
                "OPTIONAL STEP",
                "The unit moved one space. The card lets you move it once more.",
                contextCards: new[] { new DecisionWindowCardViewModel(hand[0]) },
                // Lift Up's OWN declared pair, read out of the catalog rather than typed: this frame is
                // real content end to end, which is what makes it the one to compare with the mockup's
                // own optional-step frame.
                controls: new[]
                {
                    new DecisionWindowControlViewModel(
                        DeclaredChosen(WaspCardContent.LiftUp, "orbit-repeat"),
                        DirectorCommand.ReleaseTurn(),
                        isPrimary: true),
                    new DecisionWindowControlViewModel(
                        DeclaredDeclined(WaspCardContent.LiftUp, "orbit-repeat"),
                        DirectorCommand.ReleaseTurn())
                });

        private static string DeclaredChosen(CardDefinitionId cardId, string nodeId)
        {
            Assert.That(
                DecisionLabelCatalog.TryGetLabel(cardId, new EffectNodeId(nodeId), out var label),
                Is.True, cardId.Value + " / " + nodeId + " declares its consequence.");
            return label.Chosen.ToUpperInvariant();
        }

        private static string DeclaredDeclined(CardDefinitionId cardId, string nodeId)
        {
            Assert.That(
                DecisionLabelCatalog.TryGetLabel(cardId, new EffectNodeId(nodeId), out var label),
                Is.True);
            Assert.That(label.HasDeclined, Is.True);
            return label.Declined.ToUpperInvariant();
        }

        private static DecisionWindowViewModel CardChooser(IReadOnlyList<TrayCardViewModel> hand) =>
            new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.CardChoice,
                DecisionWindowState.Expanded,
                "8.58-capture-card-chooser",
                hand[0].DisplayName.ToUpperInvariant(),
                "CARD CHOICE",
                "Choose a card to discard.",
                contextCards: new[] { new DecisionWindowCardViewModel(hand[0]) },
                controls: hand.Skip(1).Take(3).Select(card => new DecisionWindowControlViewModel(
                        card.DisplayName.ToUpperInvariant(),
                        DirectorCommand.ReleaseTurn(),
                        card: card))
                    .ToArray());

        private static DecisionWindowViewModel TiedInitiative(IReadOnlyList<TrayCardViewModel> hand) =>
            new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.TiedInitiative,
                DecisionWindowState.Expanded,
                "8.58-capture-initiative",
                "WHO ACTS FIRST?",
                "TIED INITIATIVE · RED TEAM",
                "These cards tie on Initiative. Choose which hero takes the turn first.",
                controls: new[]
                {
                    new DecisionWindowControlViewModel(
                        "SABINA ACTS FIRST", DirectorCommand.ReleaseTurn(), card: hand[0]),
                    new DecisionWindowControlViewModel(
                        "TALI ACTS FIRST", DirectorCommand.ReleaseTurn(), card: hand[1])
                },
                statements: new[] { "Tali prefers Sabina" });

        private static DecisionWindowViewModel Defense(IReadOnlyList<TrayCardViewModel> hand) =>
            new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.Defense,
                DecisionWindowState.Expanded,
                "8.58-capture-defense",
                "DEFEND AGAINST " + hand[0].DisplayName.ToUpperInvariant() + "?",
                "BROGAN ATTACKS SWIFT",
                "Discard a card for its printed defense, or take the hit.",
                contextCards: new[]
                {
                    // Milestone 8.58: the chip and nothing else. "Remove it and keep the old mouseover
                    // when mousing over the defense cards."
                    new DecisionWindowCardViewModel(hand[0], "ATTACK 5 · RANGE 4")
                },
                controls: hand.Skip(1).Select((card, index) => new DecisionWindowControlViewModel(
                        card.DisplayName.ToUpperInvariant(),
                        DirectorCommand.ReleaseTurn(),
                        card: card,
                        valueChip: "DEFENSE " + (index == 3 ? 6 : index + 2) +
                            (index == 3 ? " · HOLDS" : string.Empty),
                        isPrimary: index == 3))
                    .ToArray(),
                cornerControl: new DecisionWindowControlViewModel(
                    DecisionWindowPresenter.TakeTheHitLabel,
                    DirectorCommand.DeclineDefense(),
                    role: DecisionWindowControlRole.Corner));

        private static DecisionWindowViewModel RemovalPrevention(
            IReadOnlyList<TrayCardViewModel> hand) =>
            new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.RemovalPrevention,
                DecisionWindowState.Expanded,
                "8.58-capture-removal-prevention",
                "KEEP RED HEAVY?",
                "REMOVAL PREVENTION",
                "Discard a card to keep this minion on the board.",
                contextCards: new[]
                {
                    new DecisionWindowCardViewModel(hand[0], "WOULD BE REMOVED", "RED HEAVY")
                },
                controls: hand.Skip(1).Take(2).Select(card => new DecisionWindowControlViewModel(
                        card.DisplayName.ToUpperInvariant(),
                        DirectorCommand.ReleaseTurn(),
                        card: card,
                        valueChip: "DISCARD TO KEEP IT"))
                    .ToArray(),
                cornerControl: new DecisionWindowControlViewModel(
                    "LET IT BE REMOVED",
                    DirectorCommand.ReleaseTurn(),
                    role: DecisionWindowControlRole.Corner));

        private static DecisionWindowViewModel MinionBattle(bool docked, bool ready) =>
            new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.MinionBattleRemoval,
                docked ? DecisionWindowState.Docked : DecisionWindowState.Expanded,
                "8.58-capture-minion-battle-" + (docked ? "docked" : "expanded") + "-" + ready,
                "REMOVE 2 MINIONS",
                "MINION BATTLE · " + MinionChoiceLabels.TeamLabel(TeamSide.Blue),
                ready ? "Sabina is ready." : "Click the board.",
                ready ? "2 of 2 chosen" : "1 of 2 chosen",
                controls: new[]
                {
                    ready
                        ? new DecisionWindowControlViewModel(
                            DecisionWindowPresenter.RemoveTheseMinionsLabel,
                            DirectorCommand.ReleaseTurn(),
                            isPrimary: true)
                        : new DecisionWindowControlViewModel(
                            DecisionWindowPresenter.RemoveTheseMinionsLabel,
                            isPrimary: true,
                            unavailableReason: "Click the board.")
                });

        private static DecisionWindowViewModel LevelUp(
            IReadOnlyList<TrayCardViewModel> hand, DecisionWindowState state)
        {
            var controls = new List<DecisionWindowControlViewModel>();
            var colours = new[] { CardColor.Blue, CardColor.Green, CardColor.Red };
            for (var pair = 0; pair < 3; pair++)
            {
                var source = new CardInstanceId("8-58-upgraded-" + pair);
                for (var candidate = 0; candidate < 2; candidate++)
                {
                    var face = hand[((pair * 2) + candidate) % hand.Count];
                    var option = new SystemTransitionOptionViewModel(
                        "UPGRADE " + face.DisplayName.ToUpperInvariant(),
                        "SETS " + face.DisplayName.ToUpperInvariant() + " ASIDE",
                        DirectorCommand.AdvanceEndOfRound(),
                        colours[pair],
                        source,
                        face,
                        face.DisplayName,
                        candidate == 0 ? PrintedValueChannel.Attack : PrintedValueChannel.None,
                        face.DisplayName);
                    controls.Add(new DecisionWindowControlViewModel(
                        option.Label,
                        option.Command,
                        card: option.Card,
                        detail: option.Detail,
                        upgradeOption: option));
                }
            }

            return new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.LevelUp,
                state,
                "8.58-capture-level-up-" + state,
                "UPGRADE A CARD",
                "LEVEL UP",
                "RED WASP REACHED LEVEL 2 - UPGRADE A CARD",
                "PAID 2 COINS - 5 REMAINING",
                controls: controls,
                handPreview: hand);
        }

        private static DecisionWindowViewModel Direction() =>
            new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.Direction,
                DecisionWindowState.Docked,
                "8.58-capture-direction",
                "COLD SNAP",
                "DIRECTION CHOICE",
                "Choose a direction");

        // -----------------------------------------------------------------------------------------
        // The victory screen's two variants
        // -----------------------------------------------------------------------------------------

        private static VictoryScreenViewModel TeamWin(ConfiguredMatchSceneRuntime runtime)
        {
            var heroes = runtime.Setup.Heroes.ToArray();
            var red = heroes.Where(hero => hero.TeamSide == TeamSide.Red).ToArray();
            var blue = heroes.Where(hero => hero.TeamSide == TeamSide.Blue).ToArray();
            Assert.That(red, Is.Not.Empty);
            Assert.That(blue, Is.Not.Empty);
            return new VictoryScreenViewModel(
                1L,
                MatchVictoryCause.LifeCountersExhausted,
                false,
                "RED WINS",
                MatchHudTheme.TeamRed,
                "BLUE RAN OUT OF LIFE COUNTERS",
                new[]
                {
                    Block(VictoryScreenBlockKind.Winners, TeamSide.Red, "RED TEAM - WINNERS",
                        "VICTORY", red),
                    Block(VictoryScreenBlockKind.Defeated, TeamSide.Blue, "BLUE TEAM", "DEFEATED", blue)
                });
        }

        private static VictoryScreenViewModel SoloWin(ConfiguredMatchSceneRuntime runtime)
        {
            var heroes = runtime.Setup.Heroes.ToArray();
            var red = heroes.Where(hero => hero.TeamSide == TeamSide.Red).ToArray();
            var blue = heroes.Where(hero => hero.TeamSide == TeamSide.Blue).ToArray();
            var blocks = new List<VictoryScreenBlockViewModel>
            {
                Block(VictoryScreenBlockKind.SoloWinner, TeamSide.Red,
                    red[0].DisplayName.ToUpperInvariant() + " - SOLO WINNER", "SOLO VICTORY",
                    red.Take(1).ToArray())
            };
            // The mockup's "her crew" block exists only where she HAS one: this scene is a mirrored
            // one-a-side staging, and a block with no heroes in it is not drawn at all.
            if (red.Length > 1)
            {
                blocks.Add(Block(VictoryScreenBlockKind.Defeated, TeamSide.Red,
                    "RED TEAM - HER CREW", "NO SHARE", red.Skip(1).ToArray()));
            }

            blocks.Add(Block(
                VictoryScreenBlockKind.Defeated, TeamSide.Blue, "BLUE TEAM", "DEFEATED", blue));
            return new VictoryScreenViewModel(
                2L,
                MatchVictoryCause.ThroneCaptured,
                true,
                red[0].DisplayName.ToUpperInvariant() + " WINS",
                MatchHudTheme.CoinFace,
                "SOLO VICTORY - THE THRONE WAS CLAIMED ALONE",
                blocks);
        }

        private static VictoryScreenBlockViewModel Block(
            VictoryScreenBlockKind kind,
            TeamSide side,
            string header,
            string tag,
            IReadOnlyList<QuickGameHeroSetupState> heroes)
        {
            Assert.That(heroes, Is.Not.Empty, "A staged block draws at least one hero.");
            var rows = new List<VictoryScreenRowViewModel>();
            for (var index = 0; index < heroes.Count; index++)
            {
                var hero = heroes[index];
                var cells = VictoryScreenLabels.Columns
                    .Select((column, ordinal) => new VictoryScreenCellViewModel(
                        column, ((index + ordinal) % 7) + 1, ordinal == 6 && index == 0))
                    .ToArray();
                rows.Add(new VictoryScreenRowViewModel(
                    hero.HeroId, side, hero.ContentId, hero.DisplayName, 2, cells));
            }

            return new VictoryScreenBlockViewModel(kind, side, header, tag, rows);
        }

        // -----------------------------------------------------------------------------------------
        // Scene
        // -----------------------------------------------------------------------------------------

        private static VisualElement Window(MatchSceneCompositionRoot root)
        {
            var found = UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-decision-window")
                .ToArray();
            Assert.That(found, Has.Length.EqualTo(1));
            Assert.That(UiPlayer.IsShown(found[0]), Is.True);
            return found[0];
        }

        private static IEnumerator LoadEngagedScene()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent =
                SceneCardContent.VerifiedEngagedMirror;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }
    }
}
