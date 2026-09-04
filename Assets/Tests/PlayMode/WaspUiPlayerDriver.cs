using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Drives the configured match the way a player does. Milestone 8.15.
    ///
    /// <para>Every existing Play Mode fixture reaches the match through
    /// <see cref="ConfiguredMatchSceneRuntime"/>'s headless command surface, which looks an invocation
    /// up in the rendered model and submits it to the presenter directly. That surface deliberately
    /// bypasses the live panel: the control's own click binding, the element's enabled and displayed
    /// state, whatever is drawn on top of it, and the HUD's pointer-blocking answer are all outside
    /// it - and defects in exactly that band were caught by hand rather than by a suite. This driver
    /// enters where the player does: it finds the real element on the live UI Toolkit panel, proves a
    /// pointer at its centre would reach it, and dispatches genuine pointer events through the panel;
    /// board decisions go through the camera's own screen-point ray after the HUD has been asked
    /// whether it blocks that position, which is the exact sequence
    /// <c>PrimitiveHeroPointerInput</c> runs for a hardware click.</para>
    /// </summary>
    internal static class UiPlayer
    {
        /// <summary>
        /// Milestone 8.35g. Hands a finished turn on when one is waiting for its own player, and
        /// says whether it had to.
        /// </summary>
        /// <remarks>
        /// <para>Every headless filler step in these journeys used to be able to read a selection or
        /// a commitment phase straight after a resolution completed, because completing the
        /// resolution WAS the hand-off. It no longer is: the turn is held for the player whose card
        /// resolved, and the acknowledgement that raised the pause is what emptied the selection. So
        /// the filler presses the release exactly where a player does - above the read, never
        /// beside it.</para>
        /// <para>It goes through the projected command rather than the END TURN button on purpose.
        /// The button is the subject of its own journey; here the release is filler, and the same
        /// split milestone 8.15 established for the commitment applies.</para>
        /// </remarks>
        public static bool ReleaseFinishedTurn(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.PendingTurnRelease == null)
            {
                return false;
            }

            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ReleaseTurn()), Is.True,
                "A turn waiting for its own player is always answerable by its own release.");
            return true;
        }

        /// <summary>
        /// Milestone 8.35g. Finishes whatever turn is still in progress, so a filler step that wants
        /// the commitment phase actually finds it open.
        /// </summary>
        /// <remarks>
        /// <para>One release is not enough on its own. A turn holds several actors in Initiative
        /// order and each of them now costs two presses of the same control - the Hold or stop that
        /// finishes their resolution, then the release that hands on - so a fixture that pressed
        /// once per actor arrives at its next commitment with the round still resolving.</para>
        /// <para>It presses only while END TURN is actually offered, so a decision the filler cannot
        /// answer stops it rather than being swallowed: the caller then fails on the decision it can
        /// see instead of on a phase it cannot explain.</para>
        /// </remarks>
        public static void EndAnyTurnInProgress(ConfiguredMatchSceneRuntime runtime)
        {
            for (var budget = 0; budget < 16; budget++)
            {
                if (runtime.Phase != RoundResolutionPhase.ResolvingTurn ||
                    runtime.RoundView.Model.EndTurnInvocation == null)
                {
                    return;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "A turn ending this filler was offered must be takeable.");
            }
        }

        /// <summary>
        /// Sizes the Play Mode view to a realistic desktop viewport before a journey begins.
        ///
        /// <para>The batch test runner's default play view is 640 by 480, a viewport no player uses
        /// and one the HUD is honestly unplayable at: the five-card tray wraps to two rows that
        /// overflow their scroll, and the 52-pixel board band sits entirely behind the tray, so every
        /// journey assertion about what a player can reach would fail against geometry no player
        /// meets. 1920 by 1080 is the smallest common desktop viewport and comfortably above
        /// the configured HUD's supported desktop viewport. The resize goes through
        /// <c>UnityEditor.PlayModeWindow</c> by reflection because a Play Mode test assembly cannot
        /// reference the Editor assembly; in a built player the type is absent and the viewport is
        /// whatever the player's window is, which is the honest answer there.</para>
        /// </summary>
        public static void EnsurePlayerViewport()
        {
            if (Screen.width >= 1280 && Screen.height >= 720)
            {
                return;
            }

            SetPlayerViewport(1920, 1080, "8.15 journey");
        }

        /// <summary>
        /// Sizes the Play Mode view to an exact viewport, for a journey whose subject <em>is</em> the
        /// viewport. Milestone 8.20c, which needs a short supported desktop screen to show that the
        /// region holding the cards scrolls rather than the cards shrinking.
        ///
        /// <para>Unlike <see cref="EnsurePlayerViewport"/> this always applies, so a caller that
        /// shrinks the view owns putting it back: every other journey in the suite reads its geometry
        /// from whatever size is left behind, and <see cref="EnsurePlayerViewport"/> would accept a
        /// leaked short one as already good enough.</para>
        /// </summary>
        public static void SetPlayerViewport(uint width, uint height, string reason)
        {
            var playModeWindow = Type.GetType(
                "UnityEditor.PlayModeWindow,UnityEditor" /* absence-intended: Editor-only view sizing */);
            Assert.That(playModeWindow, Is.Not.Null,
                $"The {Screen.width}x{Screen.height} viewport cannot be set to {width}x{height}, and " +
                "the Editor play-view sizing type is unavailable to correct it.");

            var custom = playModeWindow.GetMethod(
                "SetCustomRenderingResolution",
                new[] { typeof(uint), typeof(uint), typeof(string) });
            if (custom != null)
            {
                custom.Invoke(null, new object[] { width, height, reason });
                return;
            }

            var setViewSize = playModeWindow.GetMethod(
                "SetViewSize", new[] { typeof(uint), typeof(uint) });
            if (setViewSize != null)
            {
                setViewSize.Invoke(null, new object[] { width, height });
                return;
            }

            Assert.Fail(
                "No known play-view sizing method exists on UnityEditor.PlayModeWindow; its static " +
                "methods are: " + string.Join(", ", playModeWindow
                    .GetMethods(System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static)
                    .Select(method => method.Name)
                    .Distinct()));
        }

        /// <summary>
        /// Captures the live Game View to <c>tmp/</c> for the targeted non-batch inspection the
        /// permanent hero player-path gate asks of every visible addition. Milestone 8.20b lifted it
        /// here from the per-hero fixture that first needed it, because two heroes now need it and a
        /// second copy would drift from the first.
        ///
        /// <para>A batch run has no Game View to photograph, so it captures nothing rather than
        /// failing: the gate's own sentence is that screenshot evidence cannot substitute for the
        /// real-input journey, and the journey is what runs in both tiers.</para>
        /// </summary>
        public static System.Collections.IEnumerator CaptureGameView(string milestone, string name)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                yield break;
            }

            var path = System.IO.Path.GetFullPath($"tmp/{milestone}-{name}.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(System.IO.File.Exists(path), Is.True,
                $"The targeted Game View evidence must be captured under tmp/ as {name}.png.");
        }

        /// <summary>
        /// Scrolls the control into its enclosing scroll view's visible area, as a player would before
        /// clicking something the tray has scrolled away. A control in no scroll view is left alone.
        /// The caller yields a frame afterwards so the scroll's layout resolves before the click.
        /// </summary>
        public static void EnsureVisible(VisualElement element)
        {
            for (var current = element?.hierarchy.parent; current != null;
                current = current.hierarchy.parent)
            {
                if (current is ScrollView scrollView)
                {
                    scrollView.ScrollTo(element);
                    return;
                }
            }
        }

        /// <summary>
        /// Every element under <paramref name="root"/>, in depth-first document order, so a caller can
        /// address a control the way a reader finds it.
        /// </summary>
        public static IEnumerable<VisualElement> Descendants(VisualElement root)
        {
            if (root == null)
            {
                yield break;
            }

            foreach (var child in root.Children())
            {
                yield return child;
                foreach (var grandchild in Descendants(child))
                {
                    yield return grandchild;
                }
            }
        }

        /// <summary>Whether the element and its whole ancestry are displayed.</summary>
        public static bool IsShown(VisualElement element)
        {
            for (var current = element; current != null; current = current.hierarchy.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The one shown button whose visible label is exactly <paramref name="label"/>, or whose
        /// silent printed-action region carries that exact projected label as typed user data. The
        /// latter is how the player driver addresses the artwork-native boot, Skill book, or swords
        /// without forcing redundant words back onto the prepared card face. Zero or several is a
        /// failure with the visible candidates named, because a journey that cannot find its control
        /// must say what the player was actually looking at.
        /// </summary>
        public static Button Button(ConfiguredMatchHudRenderer hud, string label)
        {
            var buttons = Descendants(hud.RootElement)
                .OfType<Button>()
                .Where(candidate => IsShown(candidate) &&
                    (candidate.text == label ||
                     candidate.name == "gd-printed-action-region" &&
                     candidate.userData is PrintedActionRegionViewModel region &&
                     region.ActionLabel == label))
                .ToArray();
            Assert.That(buttons, Has.Length.EqualTo(1),
                $"Expected exactly one shown control labelled '{label}'; the shown controls are: " +
                Describe(hud));
            return buttons[0];
        }

        /// <summary>Whether exactly one shown button carries this label, for a journey asserting absence.</summary>
        public static bool HasButton(ConfiguredMatchHudRenderer hud, string label) =>
            Descendants(hud.RootElement)
                .OfType<Button>()
                .Count(candidate => IsShown(candidate) && candidate.text == label) == 1;

        /// <summary>Every shown button label, for a failure message that names what was on screen.</summary>
        public static string Describe(ConfiguredMatchHudRenderer hud) =>
            string.Join(" | ", Descendants(hud.RootElement)
                .OfType<Button>()
                .Where(IsShown)
                .Select(button => button.text.Length == 0
                    ? $"<{button.name}>"
                    : button.text.Replace('\n', ' ')));

        /// <summary>
        /// The shown element named <paramref name="name"/> whose face or assistive reading carries
        /// <paramref name="text"/> - how a card-shaped control (a tray card, a Defense offer, an
        /// upgrade candidate) is addressed, since the face carries no single button label. A generated
        /// face exposes labels; prepared artwork deliberately exposes the same identity through its
        /// tooltip instead, so the driver reads either representation without preferring one.
        /// </summary>
        public static VisualElement NamedElementWithText(
            ConfiguredMatchHudRenderer hud, string name, string text)
        {
            var matches = Descendants(hud.RootElement)
                .Where(candidate => candidate.name == name && IsShown(candidate) &&
                    CarriesText(candidate, text))
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1),
                $"Expected exactly one shown '{name}' element carrying the text '{text}'.");
            return matches[0];
        }

        /// <summary>
        /// Clicks one live control exactly as a pointer does: the element must be shown, enabled, and
        /// laid out; the panel must pick this element (or a child of it) at a painted point inside its
        /// bounds, so a completely covered control fails here rather than passing by direct dispatch.
        /// The centre is tried first; an overlapped physical-hand card then uses its exposed strip,
        /// exactly where a player can point. The pointer-down/pointer-up pair is sent through the panel
        /// targeted at that picked leaf, bubbling through the control's own callbacks.
        /// </summary>
        public static void Click(VisualElement element)
        {
            Assert.That(element, Is.Not.Null, "The journey asked to click nothing.");
            Assert.That(element.panel, Is.Not.Null,
                "The control is not on any panel, so no player could click it.");
            Assert.That(IsShown(element), Is.True,
                "The control is display:none somewhere in its ancestry.");
            Assert.That(element.enabledInHierarchy, Is.True,
                "The control is disabled, so a player's click would do nothing.");

            var bounds = element.worldBound;
            Assert.That(float.IsNaN(bounds.width) || float.IsNaN(bounds.height), Is.False,
                "The control has no resolved layout, so it is not actually drawn.");
            Assert.That(bounds.width, Is.GreaterThan(0f));
            Assert.That(bounds.height, Is.GreaterThan(0f));

            if (!TryFindReachablePoint(element, out var point, out var picked))
            {
                var obstruction = element.panel.Pick(bounds.center);
                Assert.Fail(
                    $"No sampled painted point inside '{DescribeElement(element)}' reaches that " +
                    "control. Its centre reaches '" +
                    (obstruction == null ? "nothing" : DescribeElement(obstruction)) + "'.");
            }

            SendPointerClick(picked, point);
        }

        /// <summary>
        /// Rests the pointer on a control - the hover half of the pipeline, which is what raises the
        /// Defense breakdown, the board target cue, and every other pointed-at reading.
        /// </summary>
        public static void PointAt(VisualElement element)
        {
            Assert.That(element?.panel, Is.Not.Null);
            Assert.That(TryFindReachablePoint(element, out _, out _), Is.True,
                $"No painted point remains exposed on '{DescribeElement(element)}'.");
            using (var enter = PointerEnterEvent.GetPooled())
            {
                enter.target = element;
                element.SendEvent(enter);
            }
        }

        /// <summary>
        /// How far a laid-out size may honestly sit from the size that was declared for it, in the
        /// panel's own reference units.
        ///
        /// <para><b>Milestone 8.25c, and it is a property of a scaled panel rather than a fudge.</b>
        /// UI Toolkit snaps every laid-out edge to the device pixel grid. While the panel drew one
        /// point per pixel that was invisible, but at any other scale a declared size lands between two
        /// pixels and the layout takes the nearer one. So a live measurement in reference units is its declared
        /// value to within one device pixel per edge, and a column of stacked boxes to within one per
        /// box.</para>
        ///
        /// <para>Pass the number of independently snapped edges the measurement accumulates: 1 for a
        /// single box's width or height, and the number of stacked rows for a column's total.</para>
        /// </summary>
        public static float PixelSnapping(int snappedEdges = 1) =>
            Mathf.Max(
                0.01f,
                Mathf.Max(1, snappedEdges) *
                    MatchHudPanelScaling.PanelPointsPerScreenPixel(Screen.width, Screen.height));

        /// <summary>The pointer leaves a control it was resting on.</summary>
        public static void PointAway(VisualElement element)
        {
            Assert.That(element?.panel, Is.Not.Null);
            using (var leave = PointerLeaveEvent.GetPooled())
            {
                leave.target = element;
                element.SendEvent(leave);
            }
        }

        /// <summary>
        /// Clicks the board where a player would: the world position is projected through the scene's
        /// own event camera to a screen point, the HUD is asked whether it blocks that point - the
        /// same question <c>PrimitiveHeroPointerInput</c> asks before every hardware click - and the
        /// camera's screen-point ray is then handled by the same selector chain. A target the HUD
        /// covers fails here, because no player could click it either.
        /// </summary>
        public static bool ClickBoard(
            PrimitiveHeroPointerInput pointerInput,
            ConfiguredMatchHudRenderer hud,
            Vector3 worldPosition)
        {
            var camera = pointerInput.EventCamera;
            Assert.That(camera, Is.Not.Null, "The pointer input has no event camera configured.");
            var screen = camera.WorldToScreenPoint(worldPosition);
            Assert.That(screen.z, Is.GreaterThan(0f),
                "The board target is behind the camera, so no player could see it.");
            Assert.That(screen.x, Is.InRange(0f, Screen.width),
                $"The board target at screen x={screen.x} is outside the {Screen.width}-wide viewport.");
            Assert.That(screen.y, Is.InRange(0f, Screen.height),
                $"The board target at screen y={screen.y} is outside the {Screen.height}-tall viewport.");
            var screenPosition = new Vector2(screen.x, screen.y);
            Assert.That(hud.BlocksPointerAt(screenPosition), Is.False,
                $"The HUD blocks the pointer at {screenPosition} " +
                $"(viewport {Screen.width}x{Screen.height}), so a player could not click this " +
                "board target where it is drawn.");
            return pointerInput.TryHandlePointerRay(camera.ScreenPointToRay(screenPosition));
        }

        private static bool IsSelfOrDescendantOf(VisualElement candidate, VisualElement ancestor)
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

        private static bool CarriesText(VisualElement element, string text)
        {
            var comparison = System.StringComparison.OrdinalIgnoreCase;
            return Descendants(element).OfType<Label>()
                    .Any(label => string.Equals(label.text, text, comparison)) ||
                Descendants(element).Prepend(element)
                    .Any(candidate => !string.IsNullOrEmpty(candidate.tooltip) &&
                        candidate.tooltip.IndexOf(text, comparison) >= 0);
        }

        /// <summary>
        /// Finds a real pointer target inside the part of a control that is still painted. The compact
        /// hand exposes roughly the left third of every non-final card, while ordinary controls remain
        /// reachable at their centre; sampling both proves the requested control is not fully covered.
        /// </summary>
        private static bool TryFindReachablePoint(
            VisualElement element,
            out Vector2 point,
            out VisualElement picked)
        {
            var bounds = element.worldBound;
            var horizontal = new[] { 0.5f, 0.18f, 0.32f, 0.68f, 0.82f, 0.06f, 0.94f };
            var vertical = new[] { 0.5f, 0.25f, 0.75f, 0.1f, 0.9f };
            foreach (var y in vertical)
            {
                foreach (var x in horizontal)
                {
                    point = new Vector2(
                        Mathf.Lerp(bounds.xMin, bounds.xMax, x),
                        Mathf.Lerp(bounds.yMin, bounds.yMax, y));
                    picked = element.panel.Pick(point);
                    if (picked != null && IsSelfOrDescendantOf(picked, element))
                    {
                        return true;
                    }
                }
            }

            point = bounds.center;
            picked = null;
            return false;
        }

        private static string DescribeElement(VisualElement element)
        {
            var text = element is TextElement textElement ? textElement.text : null;
            return string.IsNullOrEmpty(text)
                ? $"{element.GetType().Name} '{element.name}'"
                : $"{element.GetType().Name} '{element.name}' (\"{text}\")";
        }

        /// <summary>
        /// One complete click: pointer down then pointer up at the same panel position, targeted at
        /// the picked leaf and bubbling to whatever registered for them - which is how
        /// <c>Clickable</c> hears a hardware mouse.
        /// </summary>
        private static void SendPointerClick(VisualElement target, Vector2 panelPosition)
        {
            using (var down = PointerDownEvent.GetPooled(new Event
            {
                type = EventType.MouseDown,
                mousePosition = panelPosition,
                button = 0,
                clickCount = 1
            }))
            {
                down.target = target;
                target.SendEvent(down);
            }

            using (var up = PointerUpEvent.GetPooled(new Event
            {
                type = EventType.MouseUp,
                mousePosition = panelPosition,
                button = 0,
                clickCount = 1
            }))
            {
                up.target = target;
                target.SendEvent(up);
            }
        }
    }
}
