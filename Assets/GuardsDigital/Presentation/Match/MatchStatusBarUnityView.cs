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
    /// The standing status strip along the top: round and turn, both teams' Life counters, the Wave
    /// counter, and the Tie Breaker.
    ///
    /// Milestone 7.8a folds the Tie Breaker in here rather than leaving the separate top-right box beside
    /// it, by the recorded user decision. Nothing the old box guaranteed is dropped - the same counters
    /// and the same revealed side are shown, and <see cref="MatchHudHeaderViewModel.Text"/> still carries
    /// all of it as one line for the assistive reading.
    /// </summary>
    public sealed class MatchStatusBarUnityView
    {
        /// <summary>
        /// The corner radius the strip's own narration pill is drawn at.
        /// </summary>
        /// <remarks>
        /// Named and exposed by item 1 of the 2026-09-06 log drop-down refinements ("make the
        /// dropdown match the top, not to be different like this"). The match-log drop-down reads
        /// THIS rather than repeating the number, so "the tray matches the strip" is one shared
        /// source instead of two literals that happen to agree today - which is the whole failure
        /// the item reports, in the small. Milestone 8.35z58 reads it the OTHER
        /// direction too: this now forwards to <see cref="MatchHudTheme.BandCornerRadius"/>, the
        /// same one number the whole push-button family reuses, so the strip stays the single
        /// place this radius is chosen rather than becoming a second competing source.
        /// </remarks>
        public const float CornerRadius = MatchHudTheme.BandCornerRadius;

        /// <summary>The strip's own left inset, shared with the drop-down that hangs from it.</summary>
        public const float PaddingLeft = 14f;

        /// <summary>The strip's own right inset, shared with the drop-down that hangs from it.</summary>
        public const float PaddingRight = 12f;

        /// <summary>
        /// The hairline the strip closes its band with, and the weight every border in this
        /// family is drawn at.
        /// </summary>
        public const float BorderWidth = 1f;

        /// <summary>
        /// The band's ground while nothing is being asked of the viewer - the near-black the strip
        /// rests at. Milestone 8.35z47 names it here, beside the strip's other published band values,
        /// rather than leaving each host to reach for the theme colour itself.
        /// </summary>
        public static Color RestingBandGround => MatchHudTheme.TopBarBackground;

        /// <summary>
        /// The band's ground while the match is waiting on the viewer - the warm olive the whole
        /// strip turns, at the user's own requirement ("on players turn, the whole top bar needs to
        /// be gold, not just naration bar").
        /// </summary>
        public static Color AwaitedBandGround => MatchHudTheme.NarrationWarmBackground;

        /// <summary>
        /// The ground this strip most recently PAINTED, published so anything that hangs off the
        /// band can wear the same one.
        ///
        /// <para><b>Why the painted colour and not the rule.</b> Item 1 of the 2026-09-06 refinements
        /// already made the tray read the strip's insets, radius and weight from this class rather
        /// than restate them; what it could not read was the ground, because the ground is not a
        /// constant - it changes with <see cref="MatchHudHeaderViewModel.IsAwaitingViewerInput"/>.
        /// So the tray took the resting colour once, at construction, and stayed near-black while the
        /// strip above it went warm. The user saw exactly that and ruled on it (2026-09-07, visual
        /// acceptance decisions, item 3): "MATCH THE WARM TONE". Publishing the value this view just
        /// wrote - rather than a second copy of the condition that chooses it - is what makes the two
        /// unable to drift again, in the same spirit as the shared insets above.</para>
        /// </summary>
        public Color BandGround { get; private set; } = RestingBandGround;

        private readonly VisualElement content;
        private readonly VisualElement identityRow;
        private readonly VisualElement narrationLane;

        public MatchStatusBarUnityView()
        {
            Root = HudElements.Panel(MatchHudTheme.TopBarBackground);
            Root.name = "gd-topbar";
            Root.style.position = Position.Absolute;
            // Milestone 8.30b: two stacked rows rather than one. The identity row keeps every fact it
            // carried and the measurements taken of it. Item 2 of the post-8.35z9 top-strip redesign
            // (user-ruled 2026-09-05) re-anchors the narration lane below it as an absolutely-placed,
            // width-capped, centred bar rather than a full-width flex row - see Layout/Render below -
            // so Root itself no longer needs a column flex layout to stack the two.
            Root.style.overflow = Overflow.Hidden;
            Root.style.paddingLeft = PaddingLeft;
            Root.style.paddingRight = PaddingRight;
            Root.style.borderBottomWidth = BorderWidth;
            Root.style.borderBottomColor = MatchHudTheme.PanelBorder;

            identityRow = new VisualElement { name = "gd-topbar-identity" };
            identityRow.style.position = Position.Absolute;
            identityRow.style.left = 0f;
            identityRow.style.top = 0f;
            identityRow.style.right = 0f;
            identityRow.style.flexDirection = FlexDirection.Row;
            identityRow.style.alignItems = Align.Center;
            identityRow.style.justifyContent = Justify.FlexStart;
            identityRow.style.paddingLeft = 14f;
            identityRow.style.height = ConfiguredMatchHudRenderer.StatusBarIdentityRowHeight;
            Root.Add(identityRow);

            content = HudElements.Row(18f);
            content.style.flexShrink = 1f;
            identityRow.Add(content);

            // Item 2 (user-ruled 2026-09-05): "CENTER the narration bar at top-center - the game's
            // most important line takes the prime spot." The lane the round view's own narration
            // element is re-parented into is now its own centred, width-capped, rounded bar rather
            // than the retired full-width lane - a host and never a second renderer: the sentence is
            // still derived by the round presenter from the projection, exactly as before, so there
            // is still one place that decides what the match is doing and one place that says it.
            narrationLane = new VisualElement { name = "gd-narration-lane" };
            narrationLane.style.position = Position.Absolute;
            narrationLane.style.flexDirection = FlexDirection.Row;
            narrationLane.style.alignItems = Align.Center;
            narrationLane.style.justifyContent = Justify.Center;
            narrationLane.style.overflow = Overflow.Hidden;
            narrationLane.style.paddingLeft = 14f;
            narrationLane.style.paddingRight = 10f;
            HudElements.Border(narrationLane, MatchHudTheme.PanelBorder, BorderWidth, CornerRadius);
            Root.Add(narrationLane);
        }

        public VisualElement Root { get; }

        /// <summary>
        /// Moves the round view's narration element into this bar's lane. Milestone 8.30b's amendment.
        ///
        /// <para>It is exposed rather than filled here because the narration belongs to the surface
        /// that derives it. A builder of its own in this class would be a second answer to "what is
        /// the match doing", and two answers is the one thing this lane must not have - it is exactly
        /// the shape milestone 7.9c's finding F-PRES-01 was, where two copies of one derivation had
        /// drifted apart by a single pending family.</para>
        /// </summary>
        public void HostNarration(VisualElement narration)
        {
            if (narration == null)
            {
                throw new ArgumentNullException(nameof(narration));
            }

            narration.RemoveFromHierarchy();
            narration.style.flexGrow = 1f;
            narration.style.flexShrink = 1f;
            narration.style.marginTop = 0f;
            narration.style.marginBottom = 0f;
            narrationLane.Add(narration);
        }

        /// <summary>
        /// Milestone 8.35z7 (the TENTH amendment's revised placement). Hosts the match-log drop-down's
        /// own chevron toggle in the same lane the narration text lives in, beside it - so the whole
        /// narration bar reads as the log's own newest-entry-plus-toggle, per the amendment's words.
        /// </summary>
        public void HostMatchLogToggle(VisualElement toggle)
        {
            if (toggle == null)
            {
                throw new ArgumentNullException(nameof(toggle));
            }

            toggle.RemoveFromHierarchy();
            toggle.style.flexShrink = 0f;
            toggle.style.marginLeft = 6f;
            narrationLane.Add(toggle);
        }

        public MatchHudHeaderViewModel Model { get; private set; }

        /// <summary>
        /// Lays out the strip and, separately, the centred narration bar within it. Item 2 of the
        /// post-8.35z9 top-strip redesign (user-ruled 2026-09-05): the retired Tie Breaker chip
        /// rect this method used to also place is gone - that fact now lives on the rail banner
        /// <see cref="ConfiguredMatchHudRenderer"/> positions directly, above the holding column.
        /// </summary>
        public void Layout(Rect bar, Rect narrationBar)
        {
            Root.style.left = bar.x;
            Root.style.top = bar.y;
            Root.style.width = bar.width;
            Root.style.height = bar.height;
            narrationLane.style.left = narrationBar.x - bar.x;
            narrationLane.style.top = narrationBar.y - bar.y;
            narrationLane.style.width = narrationBar.width;
            narrationLane.style.height = narrationBar.height;
        }

        public void Render(MatchHudHeaderViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));

            // The user's requirement is the whole strip and not the lane inside it: "on players turn,
            // the whole top bar needs to be gold, not just naration bar". The condition is the shared
            // `AwaitedInputQuery` answer rather than "your turn" - a commitment, a defense, a level-up
            // choice and the turn release are all moments the match is waiting on you. Item 2 keeps
            // that guarantee: the narration bar is now its OWN bordered pill rather than a lane
            // painted by Root showing through, so it mirrors the same two colours explicitly instead
            // of inheriting them.
            var background = model.IsAwaitingViewerInput
                ? AwaitedBandGround
                : RestingBandGround;
            var borderColor = model.IsAwaitingViewerInput
                ? MatchHudTheme.ShineGlow
                : MatchHudTheme.PanelBorder;
            BandGround = background;
            Root.style.backgroundColor = background;
            Root.style.borderBottomColor = borderColor;
            narrationLane.style.backgroundColor = background;
            HudElements.Border(narrationLane, borderColor, BorderWidth, CornerRadius);

            content.Clear();
            content.Add(HudElements.Text(
                "GUARDS DIGITAL", 13f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            content.Add(HudElements.Text(model.RoundTurnLabel, 12f, MatchHudTheme.MutedText));
            content.Add(TeamChip(TeamSide.Red, model.RedLifeLabel));
            content.Add(HudElements.Text(model.WaveLabel, 12f, MatchHudTheme.MutedText));
            content.Add(TeamChip(TeamSide.Blue, model.BlueLifeLabel));
        }

        private static VisualElement TeamChip(TeamSide side, string label)
        {
            var chip = HudElements.Row(6f);
            var swatch = new VisualElement();
            swatch.style.width = 10f;
            swatch.style.height = 10f;
            swatch.style.backgroundColor = MatchHudTheme.TeamFace(side);
            HudElements.Border(swatch, MatchHudTheme.PanelBorder, 1f, 5f);
            chip.Add(swatch);
            chip.Add(HudElements.Text(label, 12f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            return chip;
        }
    }
}
