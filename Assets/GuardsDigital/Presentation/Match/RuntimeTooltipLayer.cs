using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z19.
    ///
    /// <para><b>Why this exists.</b> Dozens of call sites across this HUD set <c>VisualElement.tooltip</c>
    /// - the greyed-control reasons in <see cref="ActionUnavailabilityLabels"/>, the TIEBREAKER rail
    /// banner, hero/coin/chip assistive labels, a card's own reading. UI Toolkit's native tooltip is an
    /// EDITOR-ONLY feature: the Unity Manual's own Tooltip Event page states plainly "This is an
    /// Editor-only event", and it has never rendered in a packaged Player build - the live evidence this
    /// milestone exists to answer. This layer is the Player-build replacement: one popup, one root hook,
    /// serving every existing <c>.tooltip</c> string with ZERO changes to any of those call sites.</para>
    ///
    /// <para><b>The disabled-element wrinkle, and how this layer avoids it.</b> A control's blocked
    /// reason lives on a control <see cref="HudElements.MarkUnavailable"/> has already called
    /// <c>SetEnabled(false)</c> on, and Unity narrows what a DISABLED element's OWN registered callbacks
    /// receive (confirmed: only the click/press family - Pointer/MouseDown, Up, Cancel, Click, Wheel -
    /// is withheld from a disabled element starting Unity 2021.2; pointer move/enter/leave are not in
    /// that list, which is also why <see cref="HudElements.ApplyButtonMotion"/>'s own
    /// <c>PointerEnterEvent</c> handler, registered directly on the button it animates, has to guard on
    /// <c>enabledInHierarchy</c> at all - a check that would be dead code if the event never reached a
    /// disabled target). This layer does not depend on that fact holding at every Unity version, though:
    /// it never registers on the (possibly disabled) hovered element at all. It registers ONE
    /// capture-phase (<see cref="TrickleDown.TrickleDown"/>) <c>PointerMoveEvent</c> handler on the HUD
    /// ROOT - the exact pattern the match-log drop-down's click-away handler already ships
    /// (<c>ConfiguredMatchHudRenderer.Build</c>: "a trickled event still walks every ancestor of its real
    /// target on the way down, root included") - and reads <c>.tooltip</c> as a plain property off
    /// whatever <c>evt.target</c> resolves to. Picking (hit-testing, which decides <c>evt.target</c>) is
    /// governed by <c>pickingMode</c> alone and is unaffected by <c>enabledInHierarchy</c>, so a disabled
    /// control is still the resolved target and its tooltip string is still readable - this layer never
    /// asks the disabled element to handle anything, only reads state off it.</para>
    ///
    /// <para><b>Cheap by construction.</b> One popup element, added once, that toggles
    /// <c>DisplayStyle</c> rather than being rebuilt. No per-frame polling anywhere: the show delay is
    /// one scheduled one-shot (<c>schedule.Execute(...).StartingIn(...)</c>, the same idiom
    /// <c>KineticHudMotionLayer</c> already uses), and every other transition is driven by the pointer
    /// events UI Toolkit already dispatches. A stale delayed callback firing after the hovered candidate
    /// moved on is a harmless no-op (<see cref="RuntimeTooltipHoverPlan.TryElapseDelay"/> checks the
    /// candidate identity), so nothing here ever cancels a scheduled item either.</para>
    ///
    /// <para>The hover-timing logic (<see cref="RuntimeTooltipHoverPlan"/>) and the clamped placement
    /// math (<see cref="RuntimeTooltipGeometry"/>) are pure and carry no UI Toolkit dependency, so they
    /// are covered by ordinary Edit Mode cases with no live panel. Only the actual on-screen render - a
    /// packaged Player build genuinely showing the popup - is Unity-owed at integration.</para>
    /// </summary>
    public sealed class RuntimeTooltipLayer
    {
        /// <summary>The popup's own offset from the cursor, in the HUD's reference units (8.25c).</summary>
        internal static readonly Vector2 CursorOffset = new Vector2(14f, 18f);

        /// <summary>
        /// Wide enough for the longest standing tooltip sentence (a BLOCKED BY reason naming a hero
        /// and a card) to wrap onto two or three lines rather than becoming one very long banner.
        /// </summary>
        private const float MaxPopupWidth = 340f;

        private const float PopupPadding = 8f;
        private const float PopupFontSize = 12f;

        private readonly RuntimeTooltipHoverPlan plan = new RuntimeTooltipHoverPlan();
        private readonly VisualElement popup;
        private readonly Label label;
        private VisualElement trackedCandidate;
        private VisualElement hudRoot;

        public RuntimeTooltipLayer()
        {
            popup = HudElements.Panel(MatchHudTheme.PanelBackground);
            popup.name = "gd-runtime-tooltip";
            popup.style.position = Position.Absolute;
            popup.style.display = DisplayStyle.None;
            popup.style.opacity = 0f;
            popup.style.maxWidth = MaxPopupWidth;
            // Never itself a pick target: a tooltip that could be hovered would fight the very control
            // it is explaining, and would be its own (tooltip-less) candidate under the pointer.
            popup.pickingMode = PickingMode.Ignore;
            // Belt-and-suspenders alongside the "already carries its own transition" exemption
            // `HudAmbientLife.Qualifies` already grants this element: opted out by name too, so a
            // change to that exemption's exact logic can never double-animate this popup.
            popup.AddToClassList(HudAmbientLife.OptOutClass);
            HudElements.Padding(popup, PopupPadding);
            HudElements.Border(popup, MatchHudTheme.PanelBorder, 1f, MatchHudTheme.ButtonCornerRadius);

            // The motion language's own "a panel eases into existence" band (KineticMotionLanguage.cs,
            // milestone 8.35z11) - reused rather than restated, and opacity-only: the brief's "ease in,
            // no pop" rules out the settle-pop/scale-drop treatments other panels use.
            popup.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            popup.style.transitionDuration = new List<TimeValue>
            {
                new TimeValue(KineticMotionDurations.PanelEnterSeconds, TimeUnit.Second)
            };
            popup.style.transitionTimingFunction = new List<EasingFunction>
            {
                new EasingFunction(HudAmbientLife.EnterEasing)
            };

            label = HudElements.Text(string.Empty, PopupFontSize, MatchHudTheme.PrimaryText);
            label.style.whiteSpace = WhiteSpace.Normal;
            popup.Add(label);

            // Fires once per Show (display None -> Flex always re-triggers layout) and again if a
            // later Show's text measures a different size - the seam that lets the popup be positioned
            // AFTER its true size is known rather than guessed, which is what "never clipped at edges"
            // needs: clamping against a size of zero would clamp against nothing.
            popup.RegisterCallback<GeometryChangedEvent>(_ => PositionAndReveal());
        }

        /// <summary>The one element this layer adds to the HUD. Add it LAST so it paints above everything.</summary>
        public VisualElement Root => popup;

        /// <summary>
        /// Wires this layer to the HUD it serves. <paramref name="root"/> is the HUD's own root -
        /// never disabled, never itself a pick target (<c>ConfiguredMatchHudRenderer.Build</c> sets
        /// <c>pickingMode = Ignore</c> on it already) - so registering here rather than on any one
        /// control is what makes "every existing tooltip string, zero per-site changes" true.
        /// </summary>
        public void Install(VisualElement root)
        {
            hudRoot = root ?? throw new ArgumentNullException(nameof(root));
            hudRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            hudRoot.RegisterCallback<PointerDownEvent>(_ => ApplyCommand(plan.Hide()), TrickleDown.TrickleDown);
            // Best-effort: root's own pickingMode is Ignore, so this may not fire for every way the
            // pointer can leave the panel. Harmless either way - the next PointerMoveEvent anywhere
            // else in the panel already resolves a fresh candidate (or none) on its own.
            hudRoot.RegisterCallback<PointerLeaveEvent>(_ => ApplyCommand(plan.Hide()));
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            var hovered = evt.target as VisualElement;
            var candidate = RuntimeTooltipCandidateLookup.Find(hovered);
            TrackCandidate(candidate);
            var text = candidate?.tooltip;
            ApplyCommand(plan.SetCandidate(candidate, text, evt.position), candidate);
        }

        /// <summary>
        /// Watches the currently-tracked candidate for removal from the panel - a rebuild that drops
        /// the hovered card or control out from under a standing hover, with no pointer move to report
        /// it. Registered/unregistered as the candidate changes rather than swept every frame.
        /// </summary>
        private void TrackCandidate(VisualElement candidate)
        {
            if (ReferenceEquals(candidate, trackedCandidate))
            {
                return;
            }

            trackedCandidate?.UnregisterCallback<DetachFromPanelEvent>(OnCandidateDetached);
            trackedCandidate = candidate;
            trackedCandidate?.RegisterCallback<DetachFromPanelEvent>(OnCandidateDetached);
        }

        private void OnCandidateDetached(DetachFromPanelEvent evt) => ApplyCommand(plan.Hide());

        private void ApplyCommand(RuntimeTooltipCommand command, object candidateKey = null)
        {
            switch (command)
            {
                case RuntimeTooltipCommand.StartDelay:
                    HideNow();
                    ScheduleShow(candidateKey);
                    break;
                case RuntimeTooltipCommand.Reposition:
                    PositionAndReveal();
                    break;
                case RuntimeTooltipCommand.Hide:
                    HideNow();
                    break;
                case RuntimeTooltipCommand.None:
                default:
                    break;
            }
        }

        private void ScheduleShow(object candidateKey)
        {
            hudRoot.schedule.Execute(() =>
            {
                if (hudRoot.panel == null)
                {
                    return;
                }

                if (plan.TryElapseDelay(candidateKey))
                {
                    Show();
                }
            }).StartingIn(RuntimeTooltipHoverPlan.ShowDelayMilliseconds);
        }

        private void Show()
        {
            label.text = plan.CurrentText;
            // A provisional, unclamped position: it exists only to give the popup somewhere to be
            // while `display: Flex` puts it through one layout pass. `PositionAndReveal` (from the
            // GeometryChangedEvent that pass fires) supplies the real, clamped position and is what
            // actually raises the opacity - so nothing is ever visible at this provisional spot.
            popup.style.left = plan.CurrentPosition.x;
            popup.style.top = plan.CurrentPosition.y;
            popup.style.display = DisplayStyle.Flex;
        }

        private void PositionAndReveal()
        {
            if (plan.Visibility != RuntimeTooltipVisibility.Shown)
            {
                return;
            }

            var size = new Vector2(popup.resolvedStyle.width, popup.resolvedStyle.height);
            if (size.x <= 0f || size.y <= 0f)
            {
                // Not measured yet (e.g. the layout pass that follows `display: Flex` has not run) -
                // another GeometryChangedEvent follows once it has.
                return;
            }

            var viewport = MatchHudPanelScaling.CurrentReferenceViewport();
            var anchored = RuntimeTooltipGeometry.AnchorNearCursor(plan.CurrentPosition, size, viewport);
            popup.style.left = anchored.x;
            popup.style.top = anchored.y;
            popup.style.opacity = 1f;
        }

        private void HideNow()
        {
            popup.style.display = DisplayStyle.None;
            popup.style.opacity = 0f;
        }
    }

    /// <summary>Where the runtime tooltip's hover-timing state machine currently sits.</summary>
    internal enum RuntimeTooltipVisibility
    {
        Hidden,
        Pending,
        Shown
    }

    /// <summary>What <see cref="RuntimeTooltipLayer"/> must do in response to a state transition.</summary>
    internal enum RuntimeTooltipCommand
    {
        /// <summary>Nothing changed that the view need act on.</summary>
        None,

        /// <summary>Ensure the popup is hidden, and (re)start the show-delay timer for the new candidate.</summary>
        StartDelay,

        /// <summary>Already shown; just move it to the latest pointer position.</summary>
        Reposition,

        /// <summary>Hide the popup now.</summary>
        Hide
    }

    /// <summary>
    /// Milestone 8.35z19 (placeholder id). The runtime tooltip's pure hover-timing state
    /// machine - delay, show, move, hide - with no UI Toolkit dependency, so it is testable with no
    /// live panel at all.
    ///
    /// <para>It knows nothing about where a candidate's tooltip string comes from. The adapter resolves
    /// that from the actual hovered <c>VisualElement</c> - including one <c>SetEnabled(false)</c> has
    /// disabled - and hands this class only a plain identity key plus the string, which is what keeps
    /// "a disabled control still hover-reports" a fact this class does not even have to know about.</para>
    /// </summary>
    internal sealed class RuntimeTooltipHoverPlan
    {
        /// <summary>The brief's "~400ms" - how long the pointer must rest on one candidate before it shows.</summary>
        public const int ShowDelayMilliseconds = 400;

        private object currentKey;

        public RuntimeTooltipVisibility Visibility { get; private set; } = RuntimeTooltipVisibility.Hidden;

        /// <summary>The candidate's tooltip text. Meaningful only once <see cref="Visibility"/> is Shown.</summary>
        public string CurrentText { get; private set; }

        /// <summary>The latest pointer position reported for the current candidate, in panel space.</summary>
        public Vector2 CurrentPosition { get; private set; }

        /// <summary>
        /// The pointer moved and now sits over <paramref name="candidateKey"/> - null, or an empty
        /// <paramref name="candidateText"/>, meaning it sits over nothing this layer speaks for.
        /// </summary>
        public RuntimeTooltipCommand SetCandidate(object candidateKey, string candidateText, Vector2 pointerPosition)
        {
            var hasCandidate = candidateKey != null && !string.IsNullOrEmpty(candidateText);
            if (!hasCandidate)
            {
                return HideNow();
            }

            if (Equals(candidateKey, currentKey) && Visibility != RuntimeTooltipVisibility.Hidden)
            {
                // Same candidate as last time: while shown this is the "follows the cursor" case, and
                // while still pending it is not a new hover at all, so the delay is never restarted.
                CurrentPosition = pointerPosition;
                return Visibility == RuntimeTooltipVisibility.Shown
                    ? RuntimeTooltipCommand.Reposition
                    : RuntimeTooltipCommand.None;
            }

            currentKey = candidateKey;
            CurrentText = candidateText;
            CurrentPosition = pointerPosition;
            Visibility = RuntimeTooltipVisibility.Pending;
            return RuntimeTooltipCommand.StartDelay;
        }

        /// <summary>
        /// The delay scheduled by the last <see cref="RuntimeTooltipCommand.StartDelay"/> elapsed.
        /// Returns false (a harmless no-op) when the candidate has since moved on - the only guard a
        /// stale timer needs, since nothing ever cancels the timer itself.
        /// </summary>
        public bool TryElapseDelay(object candidateKey)
        {
            if (Visibility != RuntimeTooltipVisibility.Pending || !Equals(candidateKey, currentKey))
            {
                return false;
            }

            Visibility = RuntimeTooltipVisibility.Shown;
            return true;
        }

        /// <summary>An explicit hide: a press, a leave, or the candidate leaving the panel entirely.</summary>
        public RuntimeTooltipCommand Hide() => HideNow();

        private RuntimeTooltipCommand HideNow()
        {
            if (Visibility == RuntimeTooltipVisibility.Hidden)
            {
                return RuntimeTooltipCommand.None;
            }

            Visibility = RuntimeTooltipVisibility.Hidden;
            currentKey = null;
            CurrentText = null;
            return RuntimeTooltipCommand.Hide;
        }
    }

    /// <summary>
    /// The runtime tooltip's placement math: pure, so every viewport and pointer position is checkable
    /// with no live panel. Milestone 8.35z19 (placeholder id).
    /// </summary>
    internal static class RuntimeTooltipGeometry
    {
        /// <summary>
        /// Where the popup's top-left corner belongs: offset from the cursor, then clamped so the
        /// WHOLE popup stays on-screen - never clipped at an edge, on any side, including the
        /// degenerate case of a popup larger than the viewport, which clamps to the origin rather than
        /// going negative on one side while still overflowing the other.
        /// </summary>
        public static Vector2 AnchorNearCursor(Vector2 pointerPosition, Vector2 popupSize, Vector2 viewportSize) =>
            Clamp(pointerPosition + RuntimeTooltipLayer.CursorOffset, popupSize, viewportSize);

        public static Vector2 Clamp(Vector2 desiredTopLeft, Vector2 popupSize, Vector2 viewportSize)
        {
            var maxX = Mathf.Max(0f, viewportSize.x - popupSize.x);
            var maxY = Mathf.Max(0f, viewportSize.y - popupSize.y);
            return new Vector2(
                Mathf.Clamp(desiredTopLeft.x, 0f, maxX),
                Mathf.Clamp(desiredTopLeft.y, 0f, maxY));
        }
    }

    /// <summary>
    /// Finds which element a hover's tooltip string actually belongs to: the same "closest ancestor
    /// wins" rule the Editor's own default <c>TooltipEvent</c> handling uses, so a leaf glyph inside a
    /// labelled control still surfaces its owner's tooltip. Milestone 8.35z19 (placeholder
    /// id).
    ///
    /// <para>Reads the plain <c>.tooltip</c> string directly off the tree rather than through event
    /// dispatch - the one thing that keeps a disabled control's tooltip reachable:
    /// <c>VisualElement.SetEnabled(false)</c> narrows what THAT element's own registered callbacks are
    /// sent, but never touches this property, and this walk never asks the disabled element to handle
    /// anything - it only reads state off it.</para>
    /// </summary>
    internal static class RuntimeTooltipCandidateLookup
    {
        public static VisualElement Find(VisualElement hovered)
        {
            var node = hovered;
            while (node != null)
            {
                if (!string.IsNullOrEmpty(node.tooltip))
                {
                    return node;
                }

                node = node.parent;
            }

            return null;
        }
    }
}
