using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{

    /// <summary>
    /// Milestone 8.35z7 (the TENTH amendment's revised placement). The seat-facing match log: no
    /// standing panel, a chevron toggle riding beside the narration text, and a scrollable overlay
    /// that unrolls beneath the top strip when open.
    /// </summary>
    /// <remarks>
    /// Built the same way <see cref="DiagnosticLogUnityView"/> is - a toggle, a panel that starts
    /// hidden, a <see cref="ScrollView"/> rebuilt on every render while open - because that Director-
    /// only control already proved the shape a bounded, scrollable log needs. The differences are
    /// what make this one seat-facing: the toggle is a bare chevron riding in the narration lane
    /// rather than a labelled button of its own, and closing is exposed as its own
    /// <see cref="DismissRequested"/> event so a click-away can ask for "closed" without risking a
    /// "toggle" reopening it.
    /// </remarks>
    public sealed class MatchLogUnityView : IMatchLogView
    {
        private readonly Label toggle;
        private readonly Label panelTitle;
        private readonly ScrollView entries;

        public MatchLogUnityView()
        {
            toggle = HudElements.Text("▾", 13f, MatchHudTheme.MutedText, FontStyle.Bold);
            toggle.name = "gd-match-log-toggle";
            toggle.pickingMode = PickingMode.Position;
            toggle.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.StopPropagation();
                ToggleRequested?.Invoke();
            });
            ToggleRoot = toggle;

            // Item 1 of the 2026-09-06 log drop-down refinements, the user's own words: "make the
            // dropdown match the top, not to be different like this". Every value below is READ FROM
            // the top strip rather than restated, so the two cannot drift again: the band's own
            // background (this drew MatchHudTheme.PanelBackground - a lighter, different dark - which
            // is the "foreign dark box floating under the bar" the item reports, in one line), the
            // strip's border colour and weight, its corner radius and its two horizontal insets.
            //
            // Milestone 8.35z47 extends that reading to the one value item 1 could NOT share: the
            // ground. The strip's ground is not a constant - it turns warm olive whenever the match
            // is waiting on the viewer - so a tray that took the resting colour once, here, went on
            // drawing near-black under a warm band. The user looked at that and ruled (2026-09-07,
            // item 3): "MATCH THE WARM TONE". The tray now names NO ground of its own: it opens at
            // the strip's published resting one and follows the strip's own painted value through
            // `SetBandGround`.
            PanelRoot = HudElements.Panel(MatchStatusBarUnityView.RestingBandGround);
            PanelRoot.name = "gd-match-log";
            PanelRoot.style.position = Position.Absolute;
            PanelRoot.style.display = DisplayStyle.None;
            PanelRoot.style.flexDirection = FlexDirection.Column;
            PanelRoot.style.paddingLeft = MatchStatusBarUnityView.PaddingLeft;
            PanelRoot.style.paddingRight = MatchStatusBarUnityView.PaddingRight;
            PanelRoot.style.paddingTop = TrayVerticalPadding;
            PanelRoot.style.paddingBottom = TrayVerticalPadding;
            HudElements.Border(
                PanelRoot,
                MatchHudTheme.PanelBorder,
                MatchStatusBarUnityView.BorderWidth,
                MatchStatusBarUnityView.CornerRadius);
            // The tray hangs FLUSH from the strip's bottom edge (see
            // ConfiguredMatchHudRenderer.CalculateMatchLogDropdownRect), so the seam where the two
            // meet is squared and unbordered - the strip's own bottom hairline is already drawn
            // there, and a second one plus two rounded corners is what made this read as a separate
            // box rather than as the band unrolling. The three free edges keep the strip's radius.
            PanelRoot.style.borderTopWidth = 0f;
            PanelRoot.style.borderTopLeftRadius = 0f;
            PanelRoot.style.borderTopRightRadius = 0f;

            // The strip's own label type - MutedText at the identity row's size, bold - closed with
            // the same hairline the strip closes its band with, so the tray's header row echoes the
            // seam above it instead of inventing a second heading treatment.
            panelTitle = HudElements.Text(
                "MATCH LOG", TrayLabelFontSize, MatchHudTheme.MutedText, FontStyle.Bold);
            panelTitle.style.paddingBottom = 6f;
            panelTitle.style.marginBottom = 6f;
            panelTitle.style.borderBottomWidth = MatchStatusBarUnityView.BorderWidth;
            panelTitle.style.borderBottomColor = MatchHudTheme.PanelBorder;

            entries = new ScrollView(ScrollViewMode.Vertical);
            entries.style.flexGrow = 1f;
            PanelRoot.Add(panelTitle);
            PanelRoot.Add(entries);
        }

        /// <summary>
        /// The tray's own top and bottom inset. The strip states only its horizontal ones (it sizes
        /// its two rows by height instead), so this is the one padding the tray has to name itself;
        /// it is the strip's smaller horizontal inset, so the box still reads as evenly inset.
        /// </summary>
        private const float TrayVerticalPadding = 10f;

        /// <summary>
        /// The type size the strip's own identity-row labels are drawn at (ROUND/TURN, the Wave
        /// count), which the tray's title and lines now share - the "panel language" half of item 1.
        /// </summary>
        private const float TrayLabelFontSize = 12f;

        public VisualElement ToggleRoot { get; }

        public VisualElement PanelRoot { get; }

        public MatchLogViewModel Model { get; private set; }

        /// <summary>Raised by the chevron, or by the narration lane click it rides in.</summary>
        public event Action ToggleRequested;

        /// <summary>Raised by a click-away, or by anything else that wants the drop-down CLOSED.</summary>
        public event Action DismissRequested;

        /// <summary>
        /// Milestone 8.35z47. Wears the ground the top strip just painted, so the tray that unrolls
        /// from the band is the same colour as the band.
        ///
        /// <para>Called from <see cref="ConfiguredMatchHudRenderer.RenderHeader"/> - the one place the
        /// strip renders - with <see cref="MatchStatusBarUnityView.BandGround"/>, which is the colour
        /// the strip WROTE rather than a second evaluation of the condition that chooses it. That is
        /// deliberate: the defect this closes is two independent styles that agreed until one of them
        /// moved, and copying the rule would have recreated it one level up.</para>
        ///
        /// <para>The tray's own border stays the panel hairline. The strip's border turning gold is
        /// the strip's awaited signal, and the user's ruling moved the GROUND ("tint the tray's
        /// ground to the top strip's olive warmth"); a second gold edge would repeat a signal nobody
        /// asked to move, on a surface that hangs flush beneath the one already carrying it.</para>
        /// </summary>
        public void SetBandGround(Color ground)
        {
            PanelRoot.style.backgroundColor = ground;
        }

        /// <summary>What the chevron does, for a driver that has no pointer.</summary>
        public void ForwardToggle() => ToggleRequested?.Invoke();

        /// <summary>What a click-away does, for a driver that has no pointer.</summary>
        public void ForwardDismiss() => DismissRequested?.Invoke();

        public void Render(MatchLogViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            var wasOpen = Model?.IsOpen == true;
            Model = model ?? throw new ArgumentNullException(nameof(model));
            toggle.text = Model.ToggleLabel;
            PanelRoot.style.display = Model.IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            entries.Clear();
            if (!Model.IsOpen)
            {
                return;
            }

            foreach (var line in Model.Lines)
            {
                entries.Add(HudElements.Text(
                    line.Text,
                    TrayLabelFontSize,
                    line.IsHeader ? MatchHudTheme.MutedText : MatchHudTheme.PrimaryText,
                    line.IsHeader ? FontStyle.Bold : FontStyle.Normal));
            }

            // Item 2's last clause: "scroll position rests at top showing the latest". The list is
            // newest-first, so the top IS the latest and an opening tray only has to start there.
            // Only on the closed -> open transition, never on a later refresh: a new entry arriving
            // while somebody is reading back through the history must not yank them to the top.
            if (!wasOpen)
            {
                entries.scrollOffset = Vector2.zero;
            }
        }

        public void Layout(Rect panel)
        {
            PanelRoot.style.left = panel.x;
            PanelRoot.style.top = panel.y;
            PanelRoot.style.width = panel.width;
            PanelRoot.style.height = panel.height;
        }

        /// <summary>
        /// Whether <paramref name="candidate"/> is this drop-down's own toggle or panel (or nested
        /// inside either) - the click-away test a host renders against everything else on screen.
        /// </summary>
        public bool Contains(VisualElement candidate) =>
            HudElements.IsSelfOrDescendant(PanelRoot, candidate) ||
            HudElements.IsSelfOrDescendant(ToggleRoot, candidate);
    }
}
