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
        private Rect lastBar, lastNarration;
        private readonly VisualElement clockLayer;
        private readonly VisualElement lifeLayer;
        private MatchClockState displayedClock;

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
            lifeLayer=new VisualElement {name="gd-life-layer",pickingMode=PickingMode.Ignore};
            lifeLayer.style.position=Position.Absolute; lifeLayer.style.left=0; lifeLayer.style.top=0;
            lifeLayer.style.right=0; lifeLayer.style.bottom=0; Root.Add(lifeLayer);
            clockLayer=new VisualElement {name="gd-clock-layer",pickingMode=PickingMode.Ignore};
            clockLayer.style.position=Position.Absolute; clockLayer.style.left=0; clockLayer.style.top=0;
            clockLayer.style.right=0; clockLayer.style.bottom=0; Root.Add(clockLayer);
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
            lastBar=bar; lastNarration=narrationBar;
            if(Model!=null) RenderClocks(Model,displayedClock);
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
            content.style.flexDirection=FlexDirection.Column; content.style.position=Position.Absolute;
            content.style.left=14; content.style.top=0; content.style.width=380; content.style.height=ConfiguredMatchHudRenderer.StatusBarHeight;
            content.style.justifyContent=Justify.Center; content.style.alignItems=Align.FlexStart; content.style.flexWrap=Wrap.NoWrap;
            var roundLabel=HudElements.Text(model.RoundTurnLabel,26f,MatchHudTheme.PrimaryText,FontStyle.Bold);
            var waveLabel=HudElements.Text(model.WaveLabel,23f,MatchHudTheme.PrimaryText,FontStyle.Bold);
            roundLabel.name="gd-round-turn"; waveLabel.name="gd-waves-remaining";
            foreach(var label in new[]{roundLabel,waveLabel})
            { label.style.whiteSpace=WhiteSpace.NoWrap; label.style.height=27; label.style.minHeight=27; label.style.flexShrink=0; label.style.marginTop=0; label.style.marginBottom=0; content.Add(label); }
            displayedClock=model.Clock; RenderClocks(model,displayedClock);
        }

        public void RenderClock(MatchClockState clock)
        {
            displayedClock=clock;
            if(Model!=null) RenderClocks(Model,clock,true);
        }
        private void RenderClocks(MatchHudHeaderViewModel model,MatchClockState clock,bool clocksOnly=false)
        {
            clockLayer.Clear();
            var current=clock.Entries.Where(e=>e.IsCurrent).ToArray();
            var banks=current.Where(e=>e.UsesBank).Select(e=>e.Team).Distinct().ToArray();
            float left=lastNarration.x-lastBar.x, right=left+lastNarration.width;
            float baseline=lastNarration.y-lastBar.y;
            if(!clocksOnly)
            {
            lifeLayer.Clear();
            var redHeart=TeamChip(TeamSide.Red,model.RedLifeLabel); var blueHeart=TeamChip(TeamSide.Blue,model.BlueLifeLabel);
            PlaceClock(redHeart,Mathf.Max(298,left-180),baseline-8,52,52);
            PlaceClock(blueHeart,Mathf.Min(lastBar.width-64,right+126),baseline-8,52,52);
            lifeLayer.Add(redHeart); lifeLayer.Add(blueHeart);
            }
            if(!clock.Enabled) return;
            if(!banks.Contains(TeamSide.Red))
            { var preview=ClockWindow(clock.RedBankTicks,ClockColor(TeamSide.Red),"Red team time bank",26); preview.name="gd-red-bank-preview"; PlaceClock(preview,left-122,baseline+2,114,34); clockLayer.Add(preview); }
            if(!banks.Contains(TeamSide.Blue))
            { var preview=ClockWindow(clock.BlueBankTicks,ClockColor(TeamSide.Blue),"Blue team time bank",26); preview.name="gd-blue-bank-preview"; PlaceClock(preview,right+8,baseline+2,114,34); clockLayer.Add(preview); }
            var displays=new List<(long Ticks,Color Color,string Description)>();
            var normal=current.Where(e=>!e.UsesBank).ToArray();
            foreach(var group in normal.GroupBy(e=>(long)Math.Ceiling(TimeSpan.FromTicks(e.RemainingTicks).TotalSeconds)))
            {
                var sides=group.Select(e=>e.Team).Distinct().ToArray();
                var color=sides.Length>1 ? MatchHudTheme.ShineGlow : ClockColor(sides[0]);
                displays.Add((group.Key*TimeSpan.TicksPerSecond,color,string.Join(", ",group.Select(e=>e.Owner.Value+" "+e.Kind))));
            }
            foreach(var side in banks) displays.Add((clock.Bank(side),ClockColor(side),side+" team time bank"));
            float width=displays.Count<=2?164:Mathf.Min(150,520f/Mathf.Max(1,displays.Count));
            float start=lastBar.width/2-(displays.Count*(width+6)-6)/2;
            for(int i=0;i<displays.Count;i++)
            {
                var display=displays[i]; var window=ClockWindow(display.Ticks,display.Color,display.Description,displays.Count<=2?40:24);
                window.name="gd-main-clock"; PlaceClock(window,start+i*(width+6),3,width,48); clockLayer.Add(window);
            }
        }
        private static void PlaceClock(VisualElement e,float left,float top,float width,float height)
        { e.style.position=Position.Absolute; e.style.left=left; e.style.top=top; e.style.width=width; e.style.height=height; }
        private static Color ClockColor(TeamSide side) => side==TeamSide.Red ? new Color(.96f,.28f,.32f) : new Color(.3f,.73f,1f);
        private static VisualElement ClockWindow(long ticks,Color color,string description,int size)
        {
            var box=new VisualElement {tooltip=description,pickingMode=PickingMode.Ignore};
            box.style.flexDirection=FlexDirection.Row; box.style.alignItems=Align.Center; box.style.justifyContent=Justify.Center;
            box.style.backgroundColor=new Color(.025f,.04f,.045f,.96f); HudElements.Border(box,color,2,9);
            var icon=new VisualElement {name="gd-clock-icon",pickingMode=PickingMode.Ignore}; icon.style.width=22; icon.style.height=22; icon.style.marginRight=5; icon.style.flexShrink=0;
            icon.generateVisualContent+=ctx=>{var p=ctx.painter2D;p.strokeColor=color;p.lineWidth=2;p.BeginPath();p.Arc(new Vector2(11,11),8,0,360);p.Stroke();p.BeginPath();p.MoveTo(new Vector2(11,5));p.LineTo(new Vector2(11,11));p.LineTo(new Vector2(15,14));p.Stroke();};
            box.Add(icon); var seconds=(long)Math.Ceiling(TimeSpan.FromTicks(ticks).TotalSeconds);
            var digits=HudElements.Text((seconds/60).ToString("00")+":"+(seconds%60).ToString("00"),seconds>=3600?size*.7f:size,color,FontStyle.Bold); digits.name="gd-clock-digits";
            digits.style.whiteSpace=WhiteSpace.NoWrap;
            digits.style.height=Length.Percent(100); digits.style.unityTextAlign=TextAnchor.MiddleCenter;
            digits.style.marginTop=0; digits.style.marginBottom=0;
            digits.style.paddingTop=0; digits.style.paddingBottom=0;
            box.Add(digits);
            return box;
        }

        /// <summary>
        /// Milestone 8.58. One team's Life counters as a HEART in that team's colour with the number
        /// inside it.
        ///
        /// <para>The user, on the strip: "these dots moved up for some reason? Maybe just lose red and
        /// blue words, and have a red and blue heart with a number in it to symbolise the lives left."
        /// The word and the drifting dot are gone; what is left is the counter itself, drawn the way
        /// p.13 talks about it. The digit carries the same black outline 8.49's worth badge digit does,
        /// so it reads on either colour, and the glyph and the digit are stacked in one fixed box that
        /// is centred on the strip's own row rather than laid out beside the text.</para>
        /// </summary>
        private static VisualElement TeamChip(TeamSide side, string label)
        {
            var chip = new VisualElement { name = "gd-life-counter" };
            chip.style.width = LifeCounterSize;
            chip.style.height = LifeCounterSize;
            chip.style.minWidth = LifeCounterSize;
            chip.style.flexShrink = 0f;
            chip.style.alignSelf = Align.Center;
            chip.tooltip = (side == TeamSide.Red ? "Red" : "Blue") + " team life counters: " + label;

            var heart = HudElements.Text(
                "\u2665", LifeCounterSize, MatchHudTheme.TeamFace(side), FontStyle.Bold);
            heart.name = "gd-life-counter-heart";
            heart.pickingMode = PickingMode.Ignore;
            heart.style.position = Position.Absolute;
            heart.style.left = 0f;
            // Milestone 8.58, the user: "center them a bit up, in line with text, no need for this
            // offset that is lowering them." A heart glyph's ink sits low in its em box - it hangs off
            // the baseline rather than centring on it - so the BOX is nudged up by the difference,
            // which is what puts the drawn heart on the same line as the words beside it.
            heart.style.top = -LifeCounterSize * HeartInkOffset;
            heart.style.width = LifeCounterSize;
            heart.style.height = LifeCounterSize;
            heart.style.unityTextAlign = TextAnchor.MiddleCenter;
            heart.style.unityTextOutlineWidth = 1f;
            heart.style.unityTextOutlineColor = HudElements.WorthDigitOutline;
            chip.Add(heart);

            var value = HudElements.Text(
                label, LifeCounterDigitSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            value.name = "gd-life-counter-value";
            value.pickingMode = PickingMode.Ignore;
            value.style.position = Position.Absolute;
            value.style.left = 0f;
            // A heart is widest above its middle, so the number sits a little high inside it - and it
            // follows the glyph's own upward nudge, or it would slide off the ink.
            value.style.top = -LifeCounterSize * (HeartInkOffset + 0.06f);
            value.style.width = LifeCounterSize;
            value.style.height = LifeCounterSize;
            value.style.unityTextAlign = TextAnchor.MiddleCenter;
            value.style.unityTextOutlineWidth = HudElements.WorthDigitOutlineWidth;
            value.style.unityTextOutlineColor = HudElements.WorthDigitOutline;
            chip.Add(value);
            return chip;
        }

        /// <summary>
        /// The heart's own box, sized to the strip's 56-unit identity row rather than to its text - the
        /// counter is one of the three facts this strip exists to carry, and the dot it replaces was
        /// ten units tall and drifting above the words beside it.
        /// </summary>
        public const float LifeCounterSize = 52f;

        /// <summary>
        /// The number inside the heart. It is a third of the glyph rather than a half, because a heart's
        /// ink does not fill its em box and a digit sized to the box would spill over the lobes.
        /// </summary>
        public const float LifeCounterDigitSize = 24f;

        /// <summary>
        /// How far the heart GLYPH sits below its own em box's centre, as a fraction of the box. The
        /// box is drawn that much higher so the ink lands on the strip's own text line.
        /// </summary>
        private const float HeartInkOffset = 0.12f;
    }
}
