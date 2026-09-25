using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.46c. The decision window's UI Toolkit surface: one expanded host filling the open
    /// area between the two hero rails, one docked bar in the round dock's own rectangle, and nothing
    /// else.
    ///
    /// <para><b>Element names, because a journey addresses this surface by them.</b> The root is
    /// <c>gd-decision-window</c>; its two hosts are <c>gd-decision-window-expanded</c> and
    /// <c>gd-decision-window-docked</c>. Inside them: <c>gd-decision-window-eyebrow</c>,
    /// <c>-title</c>, <c>-question</c>, <c>-count</c>, <c>-statements</c>, <c>-context</c> (the cards
    /// the question is about but does not offer), <c>-controls</c> (the answers), <c>-corner</c> (the
    /// small answer that is not a choice), <c>-minimise</c> and <c>-expand</c>. A worded control is
    /// <c>gd-decision-control</c> and carries its label as the button's own <c>text</c>, so
    /// <c>UiPlayer.Button(hud, label)</c> finds it exactly as it found the strip's controls; the one
    /// control that moves the game on is <c>gd-decision-verb</c> in the shared pewter-banner language,
    /// and carries its label the same way (with the visible words on <c>gd-decision-verb-label</c>). A
    /// card-faced control keeps the name the strip gave it - <c>gd-defense-card</c> for rulebook
    /// p.11's Defense, <c>gd-effect-card</c> for a card-valued choice and a removal-prevention payment
    /// - and carries its identity in a caption label and its tooltip, which is how every existing
    /// journey addresses a face and why those journeys did not have to move with the controls.</para>
    ///
    /// <para><b>Every size is a reference unit</b> under <see cref="MatchHudPanelScaling"/>. The
    /// rectangle itself is handed down by <see cref="ConfiguredMatchHudRenderer"/>; what is declared
    /// here is only what is drawn inside it.</para>
    /// </summary>
    public sealed class DecisionWindowUnityView : IDecisionWindowView
    {
        /// <summary>
        /// A complete card face in this window is drawn at <see cref="CardFootprint"/> and nothing
        /// else - the same 300.46-by-420 the tray, the Defense row and the level-up offer all draw.
        ///
        /// <para><b>The user's instruction, 2026-09-18: "make sure you don't change the scaling on the
        /// cards."</b> This window declares no scale of its own, expanded or docked. That is why the
        /// expanded rectangle is the whole open area BETWEEN THE RAILS rather than an inset of the
        /// board - see <c>ConfiguredMatchHudRenderer.CalculateDecisionWindowExpandedRect</c>, which
        /// carries the five-face arithmetic. A card is the same size on every surface in this game.</para>
        /// </summary>
        public const float CardFaceWidth = CardFootprint.Width;

        public const float CardFaceHeight = CardFootprint.Height;

        /// <summary>The space between two answer faces.</summary>
        public const float CardGap = 8f;

        /// <summary>The space between the cards the question is ABOUT and the answers to it.</summary>
        public const float ContextGap = 24f;

        /// <summary>The window's own inside padding, inside the rectangle the renderer hands down.</summary>
        public const float WindowPadding = 16f;

        /// <summary>
        /// The expanded panel's own border, which a measurement of where its content ends has to allow
        /// for: `worldBound` is the BORDER box, and the padding is inside it.
        /// </summary>
        public const float PanelBorderWidth = 2f;

        /// <summary>The docked bar's own inside padding, which is tighter because the bar is slim.</summary>
        public const float DockedPadding = 8f;

        /// <summary>
        /// Milestone 8.58. One worded ANSWER's own footprint - the mockup's `.btn.tall`. It is a target
        /// the size of a decision rather than a chip the size of its text, and it is the same shape for
        /// a modal arm, an optional step's two answers and the Minion Battle's ratification alike.
        /// </summary>
        public const float AnswerHeight = RoundActionUnityView.OptionalStepActionHeight;

        public const float AnswerMinimumWidth = RoundActionUnityView.OptionalStepActionWidth;

        public const float AnswerFontSize = 14f;

        /// <summary>The same control at the slim bar's own height, which is what the bar has room for.</summary>
        public const float DockedControlHeight = 26f;

        // -------------------------------------------------------------------------------------
        // Milestone 8.46g. Rulebook p.15's Upgrade a Card, drawn inside the FIXED expanded
        // rectangle - and the one place in this window where a card is drawn at anything other
        // than the tray's footprint, because the hand preview is a REMINDER rather than an answer.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The expanded rectangle's own height, which
        /// <see cref="ConfiguredMatchHudRenderer.CalculateDecisionWindowExpandedRect"/> derives as 848
        /// at the 2560x1440 reference - and which is that rectangle's MINIMUM at every viewport,
        /// because the panel's <c>Expand</c> match mode guarantees it is never smaller than the
        /// reference in either dimension (the panel is 1440 units tall at 2560x1440 and TALLER at
        /// every narrower or shorter screen, never shorter).
        /// </summary>
        public const float ExpandedReferenceHeight = 848f;

        /// <summary>The height a level-up has to lay itself out inside, once the window's padding is spent.</summary>
        public const float LevelUpContentHeight = ExpandedReferenceHeight - (WindowPadding * 2f);

        /// <summary>
        /// What the window's own header spends before the offer begins: the eyebrow (11), the title
        /// (22), the question (14) and the count (12) with their margins, plus the body's own 10-unit
        /// top margin.
        ///
        /// <para><b>160 rather than the ~96 those four lines nominally measure, and the difference is
        /// pixel snapping rather than padding.</b> UI Toolkit snaps every laid-out edge to the DEVICE
        /// pixel grid, and one device pixel is <em>1/scale</em> reference units - 2.67 of them at
        /// 2560x540, the shortest viewport this project's journeys drive. A column of a dozen stacked
        /// boxes therefore measures up to a dozen of those units taller than its declared sum. The
        /// first Play Mode run of this block measured the real stack at about 136 units there and
        /// overflowed the rectangle by 16; this is that measurement with 24 units of margin over
        /// it.</para>
        /// </summary>
        public const float LevelUpHeaderAllowance = 160f;

        /// <summary>The gap between the hand the seat holds and the offer it is choosing from.</summary>
        public const float LevelUpRowGap = 8f;

        /// <summary>
        /// One upgrade candidate's full footprint: the tray's own 420-unit face plus the exterior
        /// outline the strip's own option control reserves above and below it.
        ///
        /// <para><b>Milestone 8.58 moved p.16's `GAINS:` box below the face and moved it back.</b> The
        /// user saw it over the printed text on a capture ("Gain symbols are clipping the text"), then
        /// withdrew that on seeing which faces the capture held: "Might be problem cuz you shown me
        /// tier one cards. I think it might be fine on T2 and higher and that is ok then." A real
        /// upgrade candidate is always a Tier II or III face, whose bottom band is the item-icon zone
        /// the box is drawn over on purpose (8.30j). The overlay stays.</para>
        /// </summary>
        public const float LevelUpOptionHeight =
            CardFootprint.Height + (CardFootprint.ExteriorOutlineWidth * 2f);

        /// <summary>
        /// How tall the hand preview may be, and therefore - since a face keeps its printed
        /// proportions - the scale it is drawn at.
        ///
        /// <para><b>The arithmetic, and why the strip's own 0.85 could not come with it.</b> The
        /// expanded rectangle is FIXED at 1984 by 848 (the user's rule: the window spans rail to rail
        /// so that a card never has to shrink to fit). Of its 848:</para>
        /// <list type="bullet">
        /// <item>32 is the window's own padding, leaving 816 of content;</item>
        /// <item>160 is the header allowance - eyebrow, title, question, count and the body's margin,
        /// at the measured cost of laying them out on a pixel grid coarser than a reference unit (see
        /// <see cref="LevelUpHeaderAllowance"/>);</item>
        /// <item>428 is one row of candidate faces at the TRAY'S OWN FOOTPRINT, which is the one thing
        /// in this window that may not be scaled: "make sure you don't change the scaling on the
        /// cards";</item>
        /// <item>8 separates the two rows.</item>
        /// </list>
        /// <para>816 - 160 - 428 - 8 leaves 220 units for the preview, which is 220/420 = 0.524 of a
        /// face. The strip's 0.85 would need 357 and overflow the rectangle by 137 units - the strip
        /// could afford it because it reserved its own taller tray band
        /// (<c>ConfiguredMatchHudRenderer.UpgradeComparisonBandHeight</c>) and fell back to a
        /// scroller on a short view; this window has one fixed rectangle and neither. A preview card
        /// is a REMINDER of what the seat holds, and resting the pointer on one still opens it at the
        /// tray's full size on the HUD's shared inspection layer.</para>
        /// <para>Widthwise there is room to spare either way: five preview faces at this scale are
        /// 811 units and the six candidates plus their pair gaps are 1879, against 1952 of content.</para>
        /// </summary>
        public const float LevelUpHandPreviewHeight =
            LevelUpContentHeight - LevelUpHeaderAllowance - LevelUpOptionHeight - LevelUpRowGap;

        public const float LevelUpHandPreviewScale =
            LevelUpHandPreviewHeight / CardFootprint.Height;

        /// <summary>The space between two cards of the hand preview.</summary>
        public const float LevelUpHandPreviewGap = 6f;

        /// <summary>The space between two of p.15's three source-card pairs.</summary>
        public const float LevelUpPairGap = 8f;

        public const float TitleFontSize = 22f;
        public const float EyebrowFontSize = 11f;
        public const float QuestionFontSize = 14f;
        public const float CountFontSize = 12f;
        public const float CaptionFontSize = 11f;
        public const float ChipFontSize = 12f;
        public const float CornerFontSize = 12f;

        /// <summary>
        /// Milestone 8.58. The narrowest the content-sized panel may be: wide enough for a header, a
        /// card face at the tray's own footprint, and a control row beside it, so a one-card yes-or-no
        /// still reads as a window rather than as a tooltip. It is a MINIMUM, not a width: a defense of
        /// four candidates plus the attacker grows past it and is bounded only by the rectangle.
        /// </summary>
        public const float MinimumPanelWidth = 720f;

        /// <summary>
        /// Milestone 8.58. The rectangle behind the expanded panel is a CLICK SHIELD and nothing else:
        /// fully transparent, no border, no dimming.
        ///
        /// <para>The user, on the first captures: "No reason for this bigger box when we have smaller
        /// one already." A painted scrim read as a second, larger box with its own edge - which is the
        /// same complaint the whole block began with, one layer out. The mockup has no scrim: the panel
        /// floats over the board it covers. The shield stays because the renderer has blocked the
        /// pointer over this rectangle since 8.46c, and a press outside the panel must not reach a piece
        /// underneath; it just stops being visible.</para>
        /// </summary>
        public static readonly Color ScrimColor = Color.clear;

        /// <summary>The words on the control that puts the window out of the way, and brings it back.</summary>
        public const string MinimiseLabel = "MINIMISE";

        /// <summary>
        /// Milestone 8.58. The width `MINIMISE` is given in the header, and the width of the empty
        /// column standing opposite it.
        ///
        /// <para>The user, on the optional-step capture: "This drifted a bit to the right." A control
        /// on ONE side of a centred column pushes that column off the panel's centre by half its own
        /// width, and every family drew the same drift because every family has this header. The
        /// answer is a matching column on the other side rather than an absolute corner, so the
        /// header still MEASURES what it occupies and the panel that sizes itself to its content
        /// cannot be surprised by it.</para>
        /// </summary>
        public const float MinimiseAllowance = 96f;

        public const string ExpandLabel = "EXPAND";

        private readonly VisualElement expandedHost;
        private readonly VisualElement dockedHost;
        private readonly Label expandedEyebrow;
        private readonly Label expandedTitle;
        private readonly Label expandedQuestion;
        private readonly Label expandedCount;
        private readonly VisualElement expandedStatements;
        private readonly VisualElement expandedContext;
        private VisualElement expandedBody;
        private VisualElement expandedDivider;
        private readonly VisualElement expandedControls;
        private readonly VisualElement expandedCorner;
        private readonly Button minimiseControl;
        private readonly Label dockedTitle;
        private readonly Label dockedQuestion;
        private readonly Label dockedCount;
        private readonly VisualElement dockedControls;
        private readonly VisualElement dockedCorner;
        private readonly Button expandControl;

        private readonly List<KeyValuePair<DecisionWindowControlViewModel, VisualElement>> drawnControls =
            new List<KeyValuePair<DecisionWindowControlViewModel, VisualElement>>();

        private int selectedRuneForSlots;
        private string selectedRuneDecisionKey;

        public DecisionWindowUnityView()
        {
            Root = new VisualElement { name = "gd-decision-window" };
            Root.style.position = Position.Absolute;
            Root.style.flexDirection = FlexDirection.Column;
            // Milestone 8.58. The ROOT is the BOUND the renderer states, and the panel inside it is
            // sized to its own content and centred in it. The user's words on the shipped window:
            // "This box is a waste of space like this." A yes-or-no question used to be drawn on a
            // panel nineteen hundred units wide because the panel WAS the bound.
            Root.style.justifyContent = Justify.Center;
            Root.style.alignItems = Align.Center;
            Root.style.display = DisplayStyle.None;

            expandedHost = HudElements.Panel(MatchHudTheme.PanelBackground);
            expandedHost.name = "gd-decision-window-expanded";
            expandedHost.style.flexDirection = FlexDirection.Column;
            expandedHost.style.flexGrow = 0f;
            expandedHost.style.flexShrink = 1f;
            expandedHost.style.alignItems = Align.Center;
            expandedHost.style.maxWidth = Length.Percent(100f);
            expandedHost.style.maxHeight = Length.Percent(100f);
            expandedHost.style.minWidth = MinimumPanelWidth;
            expandedHost.style.display = DisplayStyle.None;
            HudElements.Padding(expandedHost, WindowPadding);
            HudElements.Border(
                expandedHost,
                MatchHudTheme.PanelBorder,
                PanelBorderWidth,
                MatchHudTheme.BandCornerRadius);
            Root.Add(expandedHost);

            var header = new VisualElement { name = "gd-decision-window-header" };
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.FlexStart;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.flexShrink = 0f;
            // Milestone 8.58: stretched rather than sized. The panel's width is decided by its BODY -
            // the card and the controls - and the header, the question, the count and the corner each
            // take whatever that turned out to be.
            header.style.alignSelf = Align.Stretch;
            expandedHost.Add(header);

            // The expanded rectangle is ONE shape for every family - the whole open area between the
            // rails - so the content is centred inside it rather than hugging a corner of a window
            // that is nearly two thousand units wide whatever the question is.
            // Milestone 8.58: the column that balances `MINIMISE`. Without it the header's centred text
            // - and, through the header's own measured width, the whole content-sized panel - sits half
            // a control to the left of where the eye puts the panel's centre.
            var headerBalance = new VisualElement { name = "gd-decision-window-header-balance" };
            headerBalance.style.width = MinimiseAllowance + 12f;
            headerBalance.style.flexShrink = 0f;
            headerBalance.pickingMode = PickingMode.Ignore;
            header.Add(headerBalance);

            var headerText = new VisualElement();
            headerText.style.flexDirection = FlexDirection.Column;
            headerText.style.alignItems = Align.Center;
            headerText.style.flexGrow = 1f;
            headerText.style.flexShrink = 1f;
            header.Add(headerText);

            expandedEyebrow = HudElements.Text(
                string.Empty, EyebrowFontSize, MatchHudTheme.MutedText, FontStyle.Bold);
            expandedEyebrow.name = "gd-decision-window-eyebrow";
            expandedEyebrow.style.letterSpacing = 1.2f;
            Wrapping(expandedEyebrow);
            headerText.Add(expandedEyebrow);

            expandedTitle = HudElements.Text(
                string.Empty, TitleFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            expandedTitle.name = "gd-decision-window-title";
            expandedTitle.style.letterSpacing = 0.6f;
            expandedTitle.style.marginTop = 2f;
            Wrapping(expandedTitle);
            headerText.Add(expandedTitle);

            minimiseControl = HudElements.Control(
                MinimiseLabel, () => StateToggleRequested?.Invoke(), 26f);
            minimiseControl.name = "gd-decision-window-minimise";
            minimiseControl.style.fontSize = CornerFontSize;
            minimiseControl.style.flexShrink = 0f;
            minimiseControl.style.marginLeft = 12f;
            minimiseControl.style.minWidth = MinimiseAllowance;
            header.Add(minimiseControl);

            expandedQuestion = HudElements.Text(
                string.Empty, QuestionFontSize, MatchHudTheme.PrimaryText);
            expandedQuestion.name = "gd-decision-window-question";
            expandedQuestion.style.marginTop = 6f;
            expandedQuestion.style.unityTextAlign = TextAnchor.MiddleCenter;
            expandedQuestion.style.alignSelf = Align.Stretch;
            Wrapping(expandedQuestion);
            expandedHost.Add(expandedQuestion);

            expandedCount = HudElements.Text(
                string.Empty, CountFontSize, MatchHudTheme.MutedText, FontStyle.Bold);
            expandedCount.name = "gd-decision-window-count";
            expandedCount.style.marginTop = 2f;
            Wrapping(expandedCount);
            expandedCount.style.unityTextAlign = TextAnchor.MiddleCenter;
            expandedCount.style.alignSelf = Align.Stretch;
            expandedHost.Add(expandedCount);

            expandedStatements = new VisualElement { name = "gd-decision-window-statements" };
            expandedStatements.style.flexDirection = FlexDirection.Column;
            expandedStatements.style.marginTop = 2f;
            expandedStatements.style.flexShrink = 0f;
            expandedStatements.style.alignItems = Align.Center;
            expandedHost.Add(expandedStatements);

            var body = expandedBody = new VisualElement { name = "gd-decision-window-body" };
            body.style.flexDirection = FlexDirection.Row;
            body.style.alignItems = Align.FlexStart;
            body.style.justifyContent = Justify.Center;
            body.style.flexWrap = Wrap.NoWrap;
            body.style.marginTop = 10f;
            body.style.flexGrow = 1f;
            // Milestone 8.58, the user: "This drifted a bit to the right." The body spans the panel's
            // whole content box and centres its own row inside it, so the gap left of the card and the
            // gap right of the answers are the SAME gap by construction rather than by whatever the
            // header, the question and the controls happened to measure.
            body.style.alignSelf = Align.Stretch;
            expandedHost.Add(body);

            expandedContext = new VisualElement { name = "gd-decision-window-context" };
            expandedContext.style.flexDirection = FlexDirection.Row;
            expandedContext.style.alignItems = Align.FlexStart;
            expandedContext.style.flexShrink = 0f;
            expandedContext.style.marginRight = ContextGap;
            expandedContext.style.display = DisplayStyle.None;
            body.Add(expandedContext);

            expandedDivider = new VisualElement { name = "gd-decision-window-divider" };
            expandedDivider.style.width = 2f;
            expandedDivider.style.alignSelf = Align.Stretch;
            expandedDivider.style.marginRight = ContextGap;
            expandedDivider.style.flexShrink = 0f;
            expandedDivider.style.backgroundColor = MatchHudTheme.PanelBorder;
            expandedDivider.style.display = DisplayStyle.None;
            expandedDivider.pickingMode = PickingMode.Ignore;
            body.Add(expandedDivider);

            expandedControls = new VisualElement { name = "gd-decision-window-controls" };
            expandedControls.style.flexDirection = FlexDirection.Row;
            expandedControls.style.alignItems = Align.FlexStart;
            expandedControls.style.justifyContent = Justify.Center;
            expandedControls.style.flexWrap = Wrap.Wrap;
            // Never GROWS into the body's spare width: a control row that took the leftover space put
            // its own centre where the space ended rather than where the panel's centre is, which is
            // the drift the user saw. The body centres this row; this row centres its controls.
            expandedControls.style.flexGrow = 0f;
            expandedControls.style.flexShrink = 1f;
            body.Add(expandedControls);

            expandedCorner = new VisualElement { name = "gd-decision-window-corner" };
            expandedCorner.style.flexDirection = FlexDirection.Row;
            expandedCorner.style.justifyContent = Justify.FlexEnd;
            expandedCorner.style.alignItems = Align.Center;
            expandedCorner.style.flexShrink = 0f;
            expandedCorner.style.marginTop = 8f;
            expandedCorner.style.alignSelf = Align.Stretch;
            expandedCorner.style.display = DisplayStyle.None;
            expandedHost.Add(expandedCorner);

            // Milestone 8.58, the mockup's `.dock`: TWO lines, and the question owns the first of them
            // outright. It used to share that line with the count and `EXPAND`, which left
            // `UPGRADE A CARD` wrapping to two lines inside a two-line bar (the user's own capture);
            // the count moved down beside the verb, so the question has the bar's whole width minus
            // `EXPAND` and never wraps or ellipsises at the reference. Whatever does give ground gives
            // it by ellipsis rather than by taking the other line.
            dockedHost = HudElements.Panel(MatchHudTheme.PanelBackground);
            dockedHost.name = "gd-decision-window-docked";
            dockedHost.style.flexDirection = FlexDirection.Column;
            dockedHost.style.justifyContent = Justify.Center;
            dockedHost.style.flexGrow = 1f;
            // The docked bar is the WHOLE of its own rectangle - the root centres the expanded panel,
            // which would otherwise shrink this bar to its text too.
            dockedHost.style.width = Length.Percent(100f);
            dockedHost.style.height = Length.Percent(100f);
            dockedHost.style.overflow = Overflow.Hidden;
            dockedHost.style.display = DisplayStyle.None;
            HudElements.Padding(dockedHost, DockedPadding);
            HudElements.Border(dockedHost, MatchHudTheme.PanelBorder, 1f, 4f);
            Root.Add(dockedHost);

            var dockedTopRow = new VisualElement { name = "gd-decision-window-docked-top" };
            dockedTopRow.style.flexDirection = FlexDirection.Row;
            dockedTopRow.style.alignItems = Align.Center;
            dockedTopRow.style.flexShrink = 0f;
            dockedTopRow.style.overflow = Overflow.Hidden;
            dockedHost.Add(dockedTopRow);

            dockedTitle = HudElements.Text(
                string.Empty, QuestionFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            dockedTitle.name = "gd-decision-window-docked-title";
            dockedTitle.style.letterSpacing = 0.4f;
            OneLine(dockedTitle);
            dockedTitle.style.flexGrow = 1f;
            dockedTopRow.Add(dockedTitle);

            expandControl = HudElements.Control(
                ExpandLabel, () => StateToggleRequested?.Invoke(), 22f);
            expandControl.name = "gd-decision-window-expand";
            expandControl.style.fontSize = CornerFontSize;
            expandControl.style.flexShrink = 0f;
            expandControl.style.marginLeft = 8f;
            dockedTopRow.Add(expandControl);

            var dockedBottomRow = new VisualElement { name = "gd-decision-window-docked-bottom" };
            dockedBottomRow.style.flexDirection = FlexDirection.Row;
            dockedBottomRow.style.alignItems = Align.Center;
            dockedBottomRow.style.flexShrink = 0f;
            dockedBottomRow.style.marginTop = 2f;
            dockedBottomRow.style.overflow = Overflow.Hidden;
            dockedHost.Add(dockedBottomRow);

            dockedCount = HudElements.Text(
                string.Empty, CountFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            dockedCount.name = "gd-decision-window-docked-count";
            OneLine(dockedCount);
            // The count is the fact the bar is COUNTING and is never the half that gives ground: the
            // question beside it is what shortens. (After `OneLine`, which sets a shrink of its own.)
            dockedCount.style.flexShrink = 0f;
            dockedBottomRow.Add(dockedCount);

            dockedQuestion = HudElements.Text(
                string.Empty, CountFontSize, MatchHudTheme.MutedText);
            dockedQuestion.name = "gd-decision-window-docked-question";
            OneLine(dockedQuestion);
            dockedQuestion.style.flexGrow = 1f;
            dockedQuestion.style.minWidth = 0f;
            dockedBottomRow.Add(dockedQuestion);

            dockedControls = new VisualElement { name = "gd-decision-window-docked-controls" };
            dockedControls.style.flexDirection = FlexDirection.Row;
            dockedControls.style.alignItems = Align.Center;
            dockedControls.style.flexShrink = 0f;
            dockedControls.style.marginLeft = 8f;
            dockedBottomRow.Add(dockedControls);

            dockedCorner = new VisualElement { name = "gd-decision-window-docked-corner" };
            dockedCorner.style.flexDirection = FlexDirection.Row;
            dockedCorner.style.alignItems = Align.Center;
            dockedCorner.style.flexShrink = 0f;
            dockedCorner.style.marginLeft = 6f;
            dockedCorner.style.display = DisplayStyle.None;
            dockedBottomRow.Add(dockedCorner);
        }

        /// <summary>
        /// Milestone 8.58. A line of the docked bar: one line, clipped with an ellipsis rather than
        /// wrapped, because the bar has exactly two lines and a wrapped question takes the other one.
        /// </summary>
        private static void OneLine(VisualElement element)
        {
            element.style.whiteSpace = WhiteSpace.NoWrap;
            element.style.overflow = Overflow.Hidden;
            element.style.textOverflow = TextOverflow.Ellipsis;
            element.style.flexShrink = 1f;
            element.style.maxWidth = Length.Percent(100f);
        }

        public VisualElement Root { get; }

        public DecisionWindowViewModel Model { get; private set; }

        /// <summary>The viewer pressed one drawn control; the presenter decides what that submits.</summary>
        public event Action<DecisionWindowControlViewModel> ControlActivated;

        /// <summary>The viewer asked to minimise or expand. It answers nothing and submits nothing.</summary>
        public event Action StateToggleRequested;

        /// <summary>
        /// The hero the pointer or keyboard focus is on, or the undefined hero when none is. A tied
        /// Initiative's controls are about heroes, and pointing at one cues that hero where they
        /// stand - the affordance the strip's own preference row had, inherited rather than lost.
        /// </summary>
        public event Action<HeroId> HeroHighlighted;

        /// <summary>
        /// Milestone 8.46g. The pointer entered one card this window SHOWS but does not offer - a card
        /// of the level-up hand preview - or left it, which is the null card.
        ///
        /// <para>It is raised rather than answered here because the HUD has exactly one read-only
        /// inspection layer, on <see cref="RoundActionUnityView"/>, and the hand preview has used it
        /// since milestone 8.35z58: "Defense and leveling are exclusive decisions, sharing one
        /// unclipped, pointer-transparent inspection layer without adding playable controls". Moving
        /// the preview into this window did not make that two layers.</para>
        /// </summary>
        public event Action<TrayCardViewModel, VisualElement> CardInspected;

        /// <summary>
        /// Milestone 8.58. A Defense offer was pointed at, or the pointer left it (a null control).
        ///
        /// <para>The user, after seeing the same arithmetic printed inline: "Remove it and keep the old
        /// mouseover when mousing over the defense cards." The window raises the pointer and the HUD
        /// opens the breakdown on <c>RoundActionUnityView</c>'s own layer - the layer that has drawn it
        /// since 8.11b - so the window did not have to grow a second copy of the popup when it took
        /// the question off the strip.</para>
        /// </summary>
        public event Action<DecisionWindowControlViewModel, VisualElement> DefenseBreakdownPointedAt;

        /// <summary>Whether a window is currently drawn at all, for a driver with no pointer.</summary>
        public bool HasWindow => Model != null;

        public void Render(DecisionWindowViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model;
            Rebuild();
        }

        /// <summary>
        /// Activates one drawn control without a pointer, on milestone 8.8a's terms: a control this
        /// rebuild did not draw forwards nothing at all, so a stale control cannot answer a question
        /// the authority has moved past.
        /// </summary>
        public bool ForwardControl(DecisionWindowControlViewModel control)
        {
            if (control == null ||
                !drawnControls.Any(entry => ReferenceEquals(entry.Key, control)))
            {
                return false;
            }

            ControlActivated?.Invoke(control);
            return true;
        }

        /// <summary>Asks for the minimise or expand a player presses, for a driver with no pointer.</summary>
        public void ForwardStateToggle() => StateToggleRequested?.Invoke();

        /// <summary>The element drawn for one control, so a check can measure where it landed.</summary>
        public VisualElement ElementFor(DecisionWindowControlViewModel control) =>
            drawnControls
                .Where(entry => ReferenceEquals(entry.Key, control))
                .Select(entry => entry.Value)
                .FirstOrDefault();

        private void Rebuild()
        {
            if (Model == null || selectedRuneDecisionKey != Model.DecisionKey)
            {
                selectedRuneForSlots = 0;
                selectedRuneDecisionKey = Model?.DecisionKey;
            }
            drawnControls.Clear();
            expandedStatements.Clear();
            expandedContext.Clear();
            expandedControls.Clear();
            expandedCorner.Clear();
            dockedControls.Clear();
            dockedCorner.Clear();

            if (Model == null)
            {
                Root.style.display = DisplayStyle.None;
                expandedHost.style.display = DisplayStyle.None;
                dockedHost.style.display = DisplayStyle.None;
                return;
            }

            Root.style.display = DisplayStyle.Flex;
            var expanded = Model.State == DecisionWindowState.Expanded;
            // Milestone 8.58: the rectangle is a transparent click shield while the window is expanded
            // and paints nothing at all - see `ScrimColor`.
            Root.style.backgroundColor = expanded ? ScrimColor : Color.clear;
            expandedHost.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            dockedHost.style.display = expanded ? DisplayStyle.None : DisplayStyle.Flex;
            minimiseControl.style.display = Model.IsMinimisable ? DisplayStyle.Flex : DisplayStyle.None;
            expandControl.style.display = Model.IsMinimisable ? DisplayStyle.Flex : DisplayStyle.None;

            if (expanded)
            {
                BuildExpanded();
            }
            else
            {
                BuildDocked();
            }
        }

        private void BuildExpanded()
        {
            // Milestone 8.58. EVERY family is sized to what it has to show, p.15's included. The
            // level-up offer's arithmetic is absolute rather than proportional (a 220-unit preview
            // row, an 8-unit gap, a 428-unit band of faces), so a host that shrinks to its content
            // still hands the offer every unit it declared. The expanded rectangle is the BOUND -
            // `maxWidth`/`maxHeight` are 100% of it - and the root centres whatever is smaller.
            expandedHost.style.width = StyleKeyword.Auto;
            expandedHost.style.height = StyleKeyword.Auto;
            expandedEyebrow.text = Model.Eyebrow;
            expandedEyebrow.style.display = string.IsNullOrEmpty(Model.Eyebrow)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            expandedTitle.text = Model.Title;
            expandedQuestion.text = Model.Question;
            expandedQuestion.style.display = string.IsNullOrEmpty(Model.Question)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            expandedCount.text = Model.Count ?? string.Empty;
            expandedCount.style.display = string.IsNullOrEmpty(Model.Count)
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            foreach (var statement in Model.Statements)
            {
                // Milestone 8.58: bold, because this row is a SECTION now as well as a ledger of what
                // teammates said - a Defense states its modifiers here ("ATTACK 5 = 4 PRINTED + 1
                // ITEM", "MINIONS BESIDE: 1 MINION"), which is a reading a player compares cards by.
                var line = HudElements.Text(
                    statement, CaptionFontSize, MatchHudTheme.MutedText, FontStyle.Bold);
                line.name = "gd-decision-window-statement";
                line.style.letterSpacing = 0.6f;
                Wrapping(line);
                expandedStatements.Add(line);
            }

            expandedStatements.style.display = Model.Statements.Count == 0
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            foreach (var card in Model.ContextCards)
            {
                expandedContext.Add(BuildContextCard(card));
            }

            expandedContext.style.display = Model.ContextCards.Count == 0
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            // Milestone 8.58, the mockup's defense frame: the incoming attack sits on its own side of a
            // rule, and the cards that could answer it sit on the other. Nowhere else needs a divider,
            // because nowhere else shows a card the player is being asked ABOUT beside cards they are
            // being asked to choose BETWEEN.
            expandedDivider.style.display =
                Model.ContextCards.Count > 0 &&
                (Model.Family == DecisionWindowFamily.Defense ||
                    Model.Family == DecisionWindowFamily.RemovalPrevention ||
                    Model.Family == DecisionWindowFamily.CardChoice)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            // Milestone 8.46g. p.15's question is not a row of independent answers: it is the seat's
            // own hand above three PAIRS, and each pair is one card the hero may upgrade. It is laid
            // out as the one shape it is rather than fed through the generic control loop.
            if (Model.Family == DecisionWindowFamily.RuneSlots)
            {
                expandedControls.style.height = StyleKeyword.Auto;
                expandedControls.style.flexDirection = FlexDirection.Column;
                expandedControls.style.alignItems = Align.Center;
                expandedControls.style.alignSelf = Align.Auto;
                expandedControls.style.flexWrap = Wrap.NoWrap;
                expandedBody.style.alignItems = Align.Center;
                expandedControls.Add(BuildRuneSlotOffer());
            }
            else if (Model.Family == DecisionWindowFamily.LevelUp)
            {
                // The offer's own arithmetic is ABSOLUTE - a 220-unit preview row, an 8-unit gap and
                // a 428-unit band of candidate faces at the tray's own footprint - so the row that
                // carries it asks for exactly what those three declare and not one unit more. The
                // rectangle BOUNDS the offer (`maxHeight` above) rather than being filled by it,
                // which is what stops the empty lower half the user struck on the mode choice: "No
                // reason for this bigger box when we have smaller one already."
                expandedControls.style.height = StyleKeyword.Auto;
                expandedControls.style.flexDirection = FlexDirection.Row;
                expandedControls.style.alignItems = Align.FlexStart;
                expandedControls.style.alignSelf = Align.Auto;
                expandedControls.style.flexWrap = Wrap.NoWrap;
                expandedBody.style.alignItems = Align.FlexStart;
                expandedControls.Add(BuildLevelUpOffer());
            }
            else
            {
                expandedControls.style.height = StyleKeyword.Auto;
                // Milestone 8.58, the mockup's `.stack`: a card's printed bullets are a LIST, read top
                // to bottom beside the card they are printed on, so a modal's worded arms stack. Faces,
                // values and an optional step's two answers stay a row.
                var worded = Model.Controls.Count > 0 &&
                    Model.Controls.All(control => control.Card == null);
                // The mockup's `.stack` of `.btn.tall{flex:1}`: a card's printed answers are STACKED
                // beside the card and together fill its height. The user settled the optional step onto
                // the same shape after seeing both - "Arrange the buttons like on Swift's gold in this
                // scenario. It's better looking." - so a printed `may`'s two answers read exactly like
                // a `Choose one`'s two bullets, the accepting one on top.
                var stacked = worded &&
                    (Model.Family == DecisionWindowFamily.Mode ||
                        Model.Family == DecisionWindowFamily.OptionalStep);
                var fillsTheCardsHeight = stacked;
                expandedControls.style.flexDirection = stacked
                    ? FlexDirection.Column
                    : FlexDirection.Row;
                expandedControls.style.alignItems =
                    stacked || fillsTheCardsHeight ? Align.Stretch : Align.FlexStart;
                // Milestone 8.58, from the first capture of this block: worded answers beside a card
                // face left a block of empty panel under them, which is the same "waste of space" the
                // user struck at full size. A card's printed bullets FILL the height of the card they
                // are printed on (the mockup's `.btn.tall{flex:1}` inside its `.stack`); every other
                // worded row is centred against the face instead of hanging from its top edge.
                expandedBody.style.alignItems = fillsTheCardsHeight
                    ? Align.Stretch
                    : worded ? Align.Center : Align.FlexStart;
                expandedControls.style.alignSelf =
                    fillsTheCardsHeight ? Align.Stretch : Align.Auto;
                // A printed `may`'s TWO answers stand SIDE BY SIDE, always - it is the one row whose
                // order is part of the reading ("accept, then decline"), and a wrap would put the
                // second answer under the first. They shrink and wrap their own text instead; every
                // other row keeps the wrap it has had since 8.46c, because six value buttons on one
                // line would run out of window before they ran out of words.
                expandedControls.style.flexWrap = worded && !stacked && Model.Controls.Count <= 2
                    ? Wrap.NoWrap
                    : Wrap.Wrap;
                for (var index = 0; index < Model.Controls.Count; index++)
                {
                    var control = Model.Controls[index];
                    // Milestone 8.58, the user on the shipped optional step and again on the Minion
                    // Battle bar: "Old buttons here instead of new ones." There is ONE control style in
                    // this window now - the shared `HudElements.Control` the mockup draws - and the
                    // leading answer is told apart by a gold border rather than by being a different
                    // object. The pewter swallowtail keeps END TURN, CONFIRM FACEDOWN and PASS, which
                    // are the strip's own controls and not this window's.
                    var element = control.Card != null
                        ? BuildCardControl(control)
                        : (VisualElement)BuildWordedControl(control);
                    if (control.Hero.IsDefined)
                    {
                        var hero = control.Hero;
                        HudElements.OnPointedAt(element, pointedAt =>
                            HeroHighlighted?.Invoke(pointedAt ? hero : default));
                    }

                    var last = index == Model.Controls.Count - 1;
                    element.style.marginRight = stacked || last ? 0f : CardGap;
                    element.style.marginBottom = stacked && last ? 0f : CardGap;
                    if (fillsTheCardsHeight)
                    {
                        element.style.flexGrow = 1f;
                    }

                    if (!stacked && expandedControls.style.flexWrap == Wrap.NoWrap)
                    {
                        // A row that may not wrap has to be able to give ground instead, so the two
                        // answers share the width they have and wrap their own words inside it.
                        element.style.minWidth = 0f;
                        element.style.flexShrink = 1f;
                    }
                    expandedControls.Add(element);
                    drawnControls.Add(
                        new KeyValuePair<DecisionWindowControlViewModel, VisualElement>(
                            control, element));
                }
            }

            if (Model.CornerControl != null)
            {
                var corner = BuildWordedControl(Model.CornerControl);
                corner.style.fontSize = CornerFontSize;
                expandedCorner.Add(corner);
                expandedCorner.style.display = DisplayStyle.Flex;
                drawnControls.Add(
                    new KeyValuePair<DecisionWindowControlViewModel, VisualElement>(
                        Model.CornerControl, corner));
            }
        }

        private void BuildDocked()
        {
            dockedTitle.text = Model.Title;
            // The mockup's second line: the count, then the question, on ONE line with a separator
            // between them - `1 of 2 chosen · click the board`.
            dockedCount.text = Model.Count ?? string.Empty;
            dockedCount.style.display = string.IsNullOrEmpty(Model.Count)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            // Milestone 8.58, the mockup's `.count` and the user's capture of the shipped bar. That line
            // is short BY CONSTRUCTION: the Minion Battle bar reads `1 of 2 chosen - click the board`,
            // and p.15's reads `PAID 2 COINS - 5 REMAINING` and stops there. The line above it already
            // says UPGRADE A CARD and names the hero, so `RED WASP REACHED LEVEL 2 - ...` was the one
            // thing on this bar that said nothing new - and it was what pushed both bars into an
            // ellipsis. It is still the expanded panel's question, where there is room to read it.
            var saysTheQuestion = Model.Family != DecisionWindowFamily.LevelUp &&
                !string.IsNullOrEmpty(Model.Question);
            dockedQuestion.text = !saysTheQuestion
                ? string.Empty
                : string.IsNullOrEmpty(Model.Count)
                    ? Model.Question
                    : " · " + Model.Question;
            dockedQuestion.style.display = saysTheQuestion
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            // "at most one verb" - the docked bar's own rule. A docked decision that offers several
            // answers is answered on the board; the one verb here is the ratification.
            var verb = Model.Controls.FirstOrDefault(control => control.Card == null);
            if (verb != null)
            {
                var element = (VisualElement)BuildWordedControl(verb);
                // The slim bar is bounded by the round dock's own width and shares its lower row with
                // the question and the corner answer, so the verb is content-sized there rather than
                // taking the expanded window's own target size. It keeps the same chrome and motion -
                // it is the same control, at the size the bar has.
                element.style.minWidth = 0f;
                element.style.minHeight = DockedControlHeight;
                element.style.fontSize = CountFontSize;
                dockedControls.Add(element);
                drawnControls.Add(
                    new KeyValuePair<DecisionWindowControlViewModel, VisualElement>(verb, element));
            }

            if (Model.CornerControl != null)
            {
                var corner = BuildWordedControl(Model.CornerControl);
                corner.style.fontSize = CornerFontSize;
                dockedCorner.Add(corner);
                dockedCorner.style.display = DisplayStyle.Flex;
                drawnControls.Add(
                    new KeyValuePair<DecisionWindowControlViewModel, VisualElement>(
                        Model.CornerControl, corner));
            }
        }

        private Button BuildWordedControl(DecisionWindowControlViewModel control)
        {
            var captured = control;
            var element = HudElements.Control(
                control.Label,
                control.IsAnswerable ? () => ControlActivated?.Invoke(captured) : (Action)null,
                34f);
            element.name = control.Role == DecisionWindowControlRole.Corner
                ? "gd-decision-window-corner-control"
                : "gd-decision-control";
            element.style.flexShrink = 1f;
            element.style.maxWidth = Length.Percent(100f);
            if (control.Role == DecisionWindowControlRole.Corner)
            {
                // The mockup's `.btn.quiet`: transparent, muted, smaller - a control that is not one of
                // the choices should not look like one.
                element.style.fontSize = CornerFontSize;
                element.style.backgroundColor = Color.clear;
                element.style.color = MatchHudTheme.MutedText;
                HudElements.Border(
                    element, MatchHudTheme.PanelBorder, 1f, MatchHudTheme.ButtonCornerRadius);
            }
            else
            {
                // The mockup's `.btn.tall`: an answer is a target, not a chip.
                element.style.minHeight = AnswerHeight;
                element.style.minWidth = AnswerMinimumWidth;
                element.style.fontSize = AnswerFontSize;
                element.style.unityTextAlign = TextAnchor.MiddleCenter;
            }

            if (control.IsPrimary && control.Role != DecisionWindowControlRole.Corner)
            {
                // The mockup's `.btn.primary`: the leading answer wears the gold border and stays the
                // same control underneath. That is the whole of "one control style, everywhere" - an
                // emphasis, never a second object with a look of its own.
                HudElements.Border(
                    element, MatchHudTheme.CoinFace, 2f, MatchHudTheme.ButtonCornerRadius);
            }

            if (control.Swatch != CardColor.None)
            {
                // Milestone 8.37's rule, unchanged: a control that NAMES a printed colour wears it,
                // and the word stays beside the tint rather than being replaced by it.
                element.style.backgroundColor = MatchHudTheme.Face(control.Swatch);
                element.style.color = MatchHudTheme.FaceText(control.Swatch);
            }

            if (control.IsChosen)
            {
                HudElements.MarkSelected(element, true);
            }

            if (control.UnavailableReason != null)
            {
                element.tooltip = control.UnavailableReason;
                HudElements.MarkUnavailable(element);
            }
            else if (control.Detail != null)
            {
                element.tooltip = control.Detail;
            }

            return element;
        }

        /// <summary>
        /// The four physical rune pieces and the four numbered turn-card slots, in the existing
        /// decision pop-up. A drop selects the pending typed pair command; the pop-up never mutates
        /// marker state. Clicking a rune then a slot is the keyboard/focus path to the same command.
        /// An occupied slot can be clicked to deselect its pair before another rune is dropped.
        /// </summary>
        private VisualElement BuildRuneSlotOffer()
        {
            var offer = new VisualElement { name = "gd-rune-slot-offer" };
            offer.style.flexDirection = FlexDirection.Column;
            offer.style.alignItems = Align.Center;
            offer.style.maxWidth = Length.Percent(100f);

            var bank = new VisualElement { name = "gd-rune-bank" };
            bank.style.flexDirection = FlexDirection.Row;
            bank.style.justifyContent = Justify.Center;
            bank.style.flexWrap = Wrap.Wrap;
            offer.Add(bank);

            var slots = new VisualElement { name = "gd-rune-turn-slots" };
            slots.style.flexDirection = FlexDirection.Row;
            slots.style.justifyContent = Justify.Center;
            slots.style.flexWrap = Wrap.Wrap;
            slots.style.marginTop = 18f;
            offer.Add(slots);

            var slotButtons = new Button[5];
            for (var slot = 1; slot <= 4; slot++)
            {
                var slotIndex = slot;
                var placed = Model.Controls.FirstOrDefault(control =>
                    control.IsChosen && control.TurnSlotIndex == slotIndex);
                var button = HudElements.Control(
                    placed == null
                        ? "TURN " + slotIndex + "\nDROP RUNE"
                        : "TURN " + slotIndex + "\nRUNE " + placed.RuneOrdinal,
                    () =>
                    {
                        if (placed != null)
                        {
                            ControlActivated?.Invoke(placed);
                        }
                        else if (selectedRuneForSlots > 0)
                        {
                            ActivateRuneSlotPair(selectedRuneForSlots, slotIndex);
                        }
                    },
                    96f);
                button.name = "gd-rune-turn-slot-" + slotIndex;
                button.style.width = 142f;
                button.style.marginRight = slot == 4 ? 0f : 9f;
                button.style.unityTextAlign = TextAnchor.MiddleCenter;
                HudElements.Border(button,
                    placed == null ? MatchHudTheme.ButtonBorder : MatchHudTheme.CoinFace,
                    placed == null ? 1f : 2f,
                    MatchHudTheme.ButtonCornerRadius);
                slots.Add(button);
                slotButtons[slotIndex] = button;
                if (placed != null)
                {
                    drawnControls.Add(
                        new KeyValuePair<DecisionWindowControlViewModel, VisualElement>(
                            placed, button));
                }
            }

            for (var rune = 1; rune <= 4; rune++)
            {
                var runeIndex = rune;
                var assigned = Model.Controls.Any(control =>
                    control.IsChosen && control.RuneOrdinal == runeIndex);
                var button = HudElements.Control(
                    assigned ? "RUNE " + runeIndex + " · PLACED" : "RUNE " + runeIndex,
                    assigned ? (Action)null : () =>
                    {
                        selectedRuneForSlots = runeIndex;
                        Rebuild();
                    },
                    64f);
                button.name = "gd-rune-piece-" + runeIndex;
                button.style.width = 142f;
                button.style.marginRight = rune == 4 ? 0f : 9f;
                button.style.unityTextAlign = TextAnchor.MiddleCenter;
                if (selectedRuneForSlots == runeIndex && !assigned)
                {
                    HudElements.MarkSelected(button, true);
                }
                if (assigned)
                {
                    HudElements.MarkUnavailable(button);
                }

                bank.Add(button);
                if (!assigned)
                {
                    BindRuneDrag(button, runeIndex, slotButtons, offer);
                }
            }

            var confirm = Model.Controls.FirstOrDefault(control =>
                control.Role == DecisionWindowControlRole.Confirm);
            if (confirm != null)
            {
                var done = BuildWordedControl(confirm);
                done.name = "gd-rune-slot-done";
                done.style.marginTop = 22f;
                offer.Add(done);
                drawnControls.Add(
                    new KeyValuePair<DecisionWindowControlViewModel, VisualElement>(
                        confirm, done));
            }

            return offer;
        }

        private void ActivateRuneSlotPair(int runeOrdinal, int slotIndex)
        {
            if (Model == null || Model.Family != DecisionWindowFamily.RuneSlots ||
                Model.Controls.Any(control =>
                    control.IsChosen &&
                    (control.RuneOrdinal == runeOrdinal || control.TurnSlotIndex == slotIndex)))
            {
                return;
            }

            var pair = Model.Controls.FirstOrDefault(control =>
                control.RuneOrdinal == runeOrdinal &&
                control.TurnSlotIndex == slotIndex &&
                control.IsAnswerable);
            if (pair != null)
            {
                selectedRuneForSlots = 0;
                ControlActivated?.Invoke(pair);
            }
        }

        private void BindRuneDrag(
            Button piece, int runeOrdinal, IReadOnlyList<Button> slots, VisualElement offer)
        {
            var pointer = -1;
            var start = Vector3.zero;
            var wasDrag = false;
            Label proxy = null;

            void EndDrag()
            {
                if (pointer >= 0 && piece.HasPointerCapture(pointer))
                {
                    piece.ReleasePointer(pointer);
                }
                pointer = -1;
                if (proxy != null)
                {
                    proxy.RemoveFromHierarchy();
                    proxy = null;
                }
            }

            piece.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || pointer >= 0)
                {
                    return;
                }
                pointer = evt.pointerId;
                start = evt.position;
                wasDrag = false;
                piece.CapturePointer(pointer);
            }, TrickleDown.TrickleDown);

            piece.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (evt.pointerId != pointer || pointer < 0)
                {
                    return;
                }
                if (!wasDrag && (evt.position - start).sqrMagnitude < 36f)
                {
                    return;
                }
                wasDrag = true;
                if (proxy == null)
                {
                    proxy = HudElements.Text(
                        "RUNE " + runeOrdinal, 17f,
                        MatchHudTheme.CoinFace, FontStyle.Bold);
                    proxy.name = "gd-rune-drag-proxy";
                    proxy.style.position = Position.Absolute;
                    proxy.pickingMode = PickingMode.Ignore;
                    offer.Add(proxy);
                }
                proxy.style.left = evt.position.x - offer.worldBound.x + 12f;
                proxy.style.top = evt.position.y - offer.worldBound.y + 12f;
            }, TrickleDown.TrickleDown);

            piece.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.pointerId != pointer)
                {
                    return;
                }
                var dragged = wasDrag;
                EndDrag();
                if (!dragged)
                {
                    return;
                }
                evt.StopImmediatePropagation();
                for (var slot = 1; slot <= 4; slot++)
                {
                    if (slots[slot].worldBound.Contains(evt.position))
                    {
                        ActivateRuneSlotPair(runeOrdinal, slot);
                        break;
                    }
                }
            }, TrickleDown.TrickleDown);

            piece.RegisterCallback<PointerCancelEvent>(_ => EndDrag(),
                TrickleDown.TrickleDown);
        }

        /// <summary>
        /// One card-faced answer: the whole printed face at the tray's own footprint, its consequence
        /// caption beneath it, and the per-card fact the question turns on - a printed Defense, a
        /// Block's verdict, a price.
        /// </summary>
        private VisualElement BuildCardControl(DecisionWindowControlViewModel control)
        {
            var captured = control;
            var element = new Button(() =>
            {
                if (captured.IsAnswerable)
                {
                    ControlActivated?.Invoke(captured);
                }
            })
            {
                name = NameForCardControl(Model.Family),
                text = string.Empty
            };
            element.style.flexDirection = FlexDirection.Column;
            element.style.alignItems = Align.Center;
            element.style.flexShrink = 0f;
            element.style.width = CardFaceWidth;
            element.style.backgroundColor = Color.clear;
            HudElements.Padding(element, 0f);
            HudElements.Border(element, Color.clear, 0f, 0f);
            HudElements.ApplyButtonMotion(element);
            element.tooltip = control.ValueChip == null
                ? control.Label
                : control.Label + " - " + control.ValueChip;
            element.Add(BuildFace(control.Card, control.ShineSlot));

            var caption = HudElements.Text(
                control.Label, CaptionFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            caption.name = "gd-decision-window-card-caption";
            caption.style.unityTextAlign = TextAnchor.MiddleCenter;
            caption.style.marginTop = 4f;
            caption.style.width = CardFaceWidth;
            Wrapping(caption);
            if (DrawsItsAnswerAsAControl(Model.Family))
            {
                // Milestone 8.58. The mockup puts a `.btn` under each tied card ("Sabina acts first");
                // the caption alone read as a label rather than as the answer. It is the control's own
                // chrome painted on the caption - the same background, border and radius
                // `HudElements.Control` uses - rather than a Button inside a Button, because the FACE
                // is the control and a nested one would take its click.
                caption.style.fontSize = CaptionFontSize + 1f;
                caption.style.paddingLeft = 10f;
                caption.style.paddingRight = 10f;
                caption.style.paddingTop = 6f;
                caption.style.paddingBottom = 6f;
                caption.style.marginTop = 8f;
                caption.style.backgroundColor = MatchHudTheme.ButtonIdleBackground;
                caption.pickingMode = PickingMode.Ignore;
                HudElements.Border(
                    caption,
                    control.IsChosen ? MatchHudTheme.CoinFace : MatchHudTheme.ButtonBorder,
                    control.IsChosen ? 2f : 1f,
                    MatchHudTheme.ButtonCornerRadius);
            }

            element.Add(caption);

            if (!string.IsNullOrEmpty(control.ValueChip))
            {
                // Milestone 8.58: the mockup's `.chip.gold` - the candidate the AUTHORITY says would
                // stop this attack is the one the eye lands on, in the coin's own gold, and the rest
                // keep the ordinary accent.
                var chip = HudElements.Text(
                    control.ValueChip,
                    ChipFontSize,
                    control.IsPrimary ? MatchHudTheme.CoinFace : MatchHudTheme.ConfirmAccent,
                    FontStyle.Bold);
                chip.name = "gd-decision-window-card-chip";
                chip.style.unityTextAlign = TextAnchor.MiddleCenter;
                chip.style.width = CardFaceWidth;
                Wrapping(chip);
                element.Add(chip);
            }

            if (control.DefenseBreakdown != null)
            {
                var hovered = control;
                var anchor = element;
                HudElements.OnPointedAt(element, pointedAt =>
                    DefenseBreakdownPointedAt?.Invoke(pointedAt ? hovered : null, anchor));
            }

            if (control.IsChosen)
            {
                HudElements.MarkSelectedWithoutResizing(element, true);
            }

            return element;
        }

        // -------------------------------------------------------------------------------------
        // Milestone 8.46g. Rulebook p.15's Upgrade a Card.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The whole level-up offer: p.15's three source-card pairs of complete candidate faces, and
        /// the seat's own hand beneath them.
        ///
        /// <para>Everything a player reads here is what the round-action strip drew before this block
        /// moved the question - the user's own condition, "show all information that we show now" -
        /// including the element names, which is what lets every journey that pressed
        /// <c>gd-upgrade-option</c> keep pressing it.</para>
        /// </summary>
        private VisualElement BuildLevelUpOffer()
        {
            var offer = new VisualElement { name = "gd-decision-window-levelup" };
            offer.style.flexDirection = FlexDirection.Column;
            offer.style.alignItems = Align.Center;
            offer.style.flexGrow = 0f;
            offer.style.flexShrink = 1f;
            offer.style.maxHeight = Length.Percent(100f);

            // Milestone 8.58, the user: "hand can be removed from bottom left and shown below the offer
            // window, like it was before." The OFFER leads - the six faces the seat is choosing between
            // are the question - and the hand they already hold is the reminder underneath it, where
            // the tray itself used to sit. The tray is hidden while this row is drawn
            // (`ConfiguredMatchHudRenderer.ApplyExclusiveContext`), so the hand is on screen once.
            offer.Add(BuildUpgradeComparisonBand());
            var preview = BuildLevelUpHandPreviewRow();
            if (preview != null)
            {
                offer.Add(preview);
            }

            return offer;
        }

        /// <summary>
        /// The seat's current hand above its own upgrade offer - the user's directive of 2026-09-07,
        /// "when leveling. Show current hand (all 5 cards) above the offer" - at the one reduced scale
        /// this window declares and the arithmetic in <see cref="LevelUpHandPreviewHeight"/> derives.
        ///
        /// <para>Read-only by construction rather than by omission, exactly as the strip's own row
        /// was: the face is <see cref="DirectorHandsUnityView.BuildStandaloneCard"/>, which is a plain
        /// element with no click handler at all, and the WHOLE subtree under a slot ignores the
        /// pointer - UI Toolkit picks the DEEPEST element under it, so a face child that kept the
        /// default picking mode would take a click from the offer below (milestone 8.35z62's measured
        /// defect). Only the slot itself receives hover, and hovering opens the HUD's one shared
        /// inspection layer at full size.</para>
        /// </summary>
        private VisualElement BuildLevelUpHandPreviewRow()
        {
            if (Model.HandPreview.Count == 0)
            {
                return null;
            }

            var row = new VisualElement { name = "gd-levelup-hand-preview" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexStart;
            row.style.justifyContent = Justify.Center;
            row.style.flexWrap = Wrap.NoWrap;
            row.style.height = LevelUpHandPreviewHeight;
            row.style.minHeight = 0f;
            row.style.flexShrink = 1f;
            row.style.overflow = Overflow.Hidden;
            row.style.marginTop = LevelUpRowGap;
            row.pickingMode = PickingMode.Ignore;

            for (var index = 0; index < Model.HandPreview.Count; index++)
            {
                var card = Model.HandPreview[index];
                var slot = new VisualElement { name = "gd-levelup-hand-preview-card" };
                slot.style.width = CardFootprint.Width * LevelUpHandPreviewScale;
                slot.style.height = CardFootprint.Height * LevelUpHandPreviewScale;
                slot.style.flexShrink = 0f;
                slot.style.marginLeft = index == 0 ? 0f : LevelUpHandPreviewGap;
                slot.style.overflow = Overflow.Hidden;
                slot.pickingMode = PickingMode.Position;
                slot.focusable = false;

                var face = DirectorHandsUnityView.BuildStandaloneCard(card);
                // Never let the map bleed through a card the player has to read while choosing an
                // upgrade: comparison is read-only, not an unavailable in-play action.
                face.style.opacity = 1f;
                face.style.position = Position.Absolute;
                face.style.left = 0f;
                face.style.top = 0f;
                face.style.width = CardFootprint.Width;
                face.style.height = CardFootprint.Height;
                face.style.scale = new Scale(
                    new Vector3(LevelUpHandPreviewScale, LevelUpHandPreviewScale, 1f));
                face.style.transformOrigin = new TransformOrigin(new Length(0f), new Length(0f));
                IgnorePointerThroughout(face);
                slot.Add(face);

                var inspected = card;
                var anchor = slot;
                slot.RegisterCallback<PointerEnterEvent>(_ => CardInspected?.Invoke(inspected, anchor));
                slot.RegisterCallback<PointerLeaveEvent>(_ => CardInspected?.Invoke(null, anchor));
                row.Add(slot);
            }

            return row;
        }

        /// <summary>
        /// p.15's six complete candidate faces, in the three pairs the pause itself supplied.
        ///
        /// <para>The pairs are the projection's own grouping - options for one upgraded card arrive
        /// together, so grouping them is reading the projection rather than sorting it - and the
        /// candidates inside a pair keep the order the pause offered them. Presentation decides only
        /// the left-to-right order of the pairs and takes it from
        /// <see cref="MatchHudTheme.ColorReadingRank"/>, which is the one thing about this decision
        /// that is the same every time it is asked; the pause orders its pairs by card instance
        /// identity, which is no order a player could learn. This is the same reading
        /// <c>RoundActionUnityView.BuildUpgradeCardRow</c> makes, because it is the same question.</para>
        /// </summary>
        private VisualElement BuildUpgradeComparisonBand()
        {
            var band = new VisualElement { name = "gd-upgrade-comparison-band" };
            band.style.flexDirection = FlexDirection.Row;
            band.style.alignItems = Align.FlexStart;
            band.style.justifyContent = Justify.Center;
            band.style.flexWrap = Wrap.NoWrap;
            band.style.flexShrink = 0f;
            band.style.height = LevelUpOptionHeight;
            band.style.backgroundColor = Color.clear;

            var columns = new List<List<DecisionWindowControlViewModel>>();
            foreach (var control in Model.Controls)
            {
                if (control.UpgradeOption == null)
                {
                    continue;
                }

                var column = columns.FirstOrDefault(candidate =>
                    candidate[0].UpgradeOption.SourceCardInstanceId.Equals(
                        control.UpgradeOption.SourceCardInstanceId));
                if (column == null)
                {
                    column = new List<DecisionWindowControlViewModel>();
                    columns.Add(column);
                }

                column.Add(control);
            }

            var ordered = columns
                .OrderBy(column => MatchHudTheme.ColorReadingRank(column[0].UpgradeOption.Color))
                .ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                // Pairing is the authority's grouping and focus order, and stays a transparent layout
                // fact: no caption, plate, border or enclosing outline (milestone 8.30j).
                var pair = new VisualElement { name = "gd-upgrade-pair" };
                pair.style.flexDirection = FlexDirection.Row;
                pair.style.width = RoundActionUnityView.UpgradePairWidth;
                pair.style.minWidth = RoundActionUnityView.UpgradePairWidth;
                pair.style.maxWidth = RoundActionUnityView.UpgradePairWidth;
                pair.style.flexShrink = 0f;
                pair.style.alignItems = Align.FlexStart;
                pair.style.justifyContent = Justify.Center;
                pair.style.flexWrap = Wrap.NoWrap;
                pair.style.marginLeft = index == 0 ? 0f : LevelUpPairGap;
                pair.style.backgroundColor = Color.clear;
                foreach (var control in ordered[index])
                {
                    var element = BuildUpgradeControl(control);
                    pair.Add(element);
                    drawnControls.Add(
                        new KeyValuePair<DecisionWindowControlViewModel, VisualElement>(
                            control, element));
                }

                band.Add(pair);
            }

            return band;
        }

        /// <summary>
        /// One upgrade option: the candidate's whole printed face at the tray's own footprint, with
        /// the fact that belongs to taking it - p.16's gained item - centred over the face's bottom
        /// item position by the SHARED overlay builder both this window and the strip call.
        ///
        /// <para>The element keeps the name and the hover reading the strip's own control has carried
        /// since milestone 8.8a, which is how every journey that presses an upgrade addresses it. The
        /// card returned to the deck and the card set aside stay in that reading, where this HUD keeps
        /// a fact a surface stops drawing.</para>
        /// </summary>
        private VisualElement BuildUpgradeControl(DecisionWindowControlViewModel control)
        {
            var captured = control;
            var element = new Button(() =>
            {
                if (captured.IsAnswerable)
                {
                    ControlActivated?.Invoke(captured);
                }
            })
            {
                name = "gd-upgrade-option",
                text = string.Empty
            };
            element.style.flexDirection = FlexDirection.Column;
            element.style.alignItems = Align.Center;
            element.style.flexShrink = 0f;
            element.style.width = CardFootprint.Width;
            element.style.height = CardFootprint.Height;
            element.style.marginLeft = CardFootprint.ExteriorOutlineWidth;
            element.style.marginRight = CardFootprint.ExteriorOutlineWidth;
            element.style.marginTop = CardFootprint.ExteriorOutlineWidth;
            element.style.marginBottom = CardFootprint.ExteriorOutlineWidth;
            element.style.backgroundColor = Color.clear;
            HudElements.Padding(element, 0f);
            HudElements.Border(element, Color.clear, 0f, 0f);
            HudElements.ApplyButtonMotion(element);
            element.tooltip = string.IsNullOrEmpty(control.Detail)
                ? control.Label
                : control.Label + " - " + control.Detail;
            element.Add(BuildFace(control.Card, CardShineSlot.None));
            element.Add(RoundActionUnityView.BuildUpgradeOptionFacts(control.UpgradeOption));
            return element;
        }

        /// <summary>
        /// Makes an element and everything beneath it pointer-transparent and unfocusable, so a
        /// read-only copy of a card can never take a click - or a keyboard focus - from the controls
        /// it is drawn above. Milestone 8.35z62's measured rule: UI Toolkit picks the DEEPEST element
        /// under the pointer, so setting the root alone does not hold.
        /// </summary>
        private static void IgnorePointerThroughout(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.pickingMode = PickingMode.Ignore;
            element.focusable = false;
            foreach (var child in element.Children())
            {
                IgnorePointerThroughout(child);
            }
        }

        /// <summary>
        /// A card the window SHOWS but does not offer, named for what it is doing there.
        ///
        /// <para>Milestone 8.58's two names, because a journey addresses them and the two say different
        /// things: <c>gd-decision-attack-card</c> is the attack a Defense is being asked about, and
        /// <c>gd-decision-source-card</c> is the card whose own printed text raised the question - the
        /// face the user asked for by name ("We said we will show the card"). Both keep
        /// <c>gd-decision-window-context-card</c> as well, so the 8.46 geometry gate's own descendant
        /// walk is untouched.</para>
        /// </summary>
        private VisualElement BuildContextCard(DecisionWindowCardViewModel card)
        {
            var host = new VisualElement
            {
                name = Model.Family == DecisionWindowFamily.Defense ||
                    Model.Family == DecisionWindowFamily.RemovalPrevention
                    ? "gd-decision-attack-card"
                    : "gd-decision-source-card"
            };
            host.style.flexDirection = FlexDirection.Column;
            host.style.alignItems = Align.Center;
            host.style.flexShrink = 0f;
            host.pickingMode = PickingMode.Ignore;
            host.Add(BuildContextCardBody(card));
            return host;
        }

        private VisualElement BuildContextCardBody(DecisionWindowCardViewModel card)
        {
            var slot = new VisualElement { name = "gd-decision-window-context-card" };
            slot.style.flexDirection = FlexDirection.Column;
            slot.style.alignItems = Align.Center;
            slot.style.flexShrink = 0f;
            slot.style.width = CardFaceWidth;
            slot.pickingMode = PickingMode.Ignore;
            slot.Add(BuildFace(card.Card, CardShineSlot.None));
            if (!string.IsNullOrEmpty(card.Caption))
            {
                // Milestone 8.58: the mockup's `.chip.gold`. The incoming attack's own total is the
                // fact the whole question turns on, so it is drawn as a chip rather than as a line.
                var caption = HudElements.Text(
                    card.Caption, ChipFontSize, MatchHudTheme.CoinFace, FontStyle.Bold);
                caption.name = "gd-decision-window-context-caption";
                caption.style.unityTextAlign = TextAnchor.MiddleCenter;
                caption.style.marginTop = 4f;
                caption.style.width = CardFaceWidth;
                Wrapping(caption);
                slot.Add(caption);
            }

            if (!string.IsNullOrEmpty(card.Detail))
            {
                var detail = HudElements.Text(
                    card.Detail, CaptionFontSize, MatchHudTheme.MutedText, FontStyle.Bold);
                detail.name = "gd-decision-window-context-detail";
                detail.style.unityTextAlign = TextAnchor.MiddleCenter;
                detail.style.width = CardFaceWidth;
                Wrapping(detail);
                slot.Add(detail);
            }

            return slot;
        }

        /// <summary>
        /// The tray's own complete face, at the tray's own footprint.
        /// </summary>
        /// <remarks>
        /// <para>It is <see cref="DirectorHandsUnityView.BuildStandaloneCard"/> and nothing composed
        /// beside it - the builder every other card surface in this HUD uses - with no transform, no
        /// declared scale and no size of its own, so the card in the window and the same card in the
        /// hand are ONE card. <see cref="CardFootprint.Apply"/> pins that size at both ends inside the
        /// builder, which is what keeps this window from resizing a face however it lays it out.</para>
        /// <para>The whole subtree is made pointer-transparent for milestone 8.35z62's measured
        /// reason: UI Toolkit picks the DEEPEST element under the pointer, so a face child that kept
        /// the default picking mode would take the click from the control it is drawn inside.</para>
        /// </remarks>
        private static VisualElement BuildFace(TrayCardViewModel card, CardShineSlot shineSlot)
        {
            var slot = new VisualElement { name = "gd-decision-window-face" };
            slot.style.flexDirection = FlexDirection.Column;
            slot.style.alignItems = Align.Center;
            slot.style.flexShrink = 0f;
            slot.pickingMode = PickingMode.Ignore;

            var face = DirectorHandsUnityView.BuildStandaloneCard(card);
            if (shineSlot != CardShineSlot.None)
            {
                DirectorHandsUnityView.AddFaceShine(face, card, shineSlot);
            }

            DirectorHandsUnityView.IgnorePickingRecursively(face);
            slot.Add(face);
            return slot;
        }

        /// <summary>
        /// The element name a card-faced control keeps. Rulebook p.11's Defense and a card-valued
        /// choice have been addressed by these two names since milestones 8.11b and 8.15a, and every
        /// journey that clicks a face finds it by one of them - so the family decides the name and the
        /// journeys did not have to move with the control.
        /// </summary>
        /// <summary>
        /// Milestone 8.58. Whether a card-faced control's caption is drawn as the ANSWER - the
        /// mockup's `.btn` under each face - rather than as a fact about the card.
        ///
        /// <para>A tied Initiative's caption is a sentence about what pressing does ("Sabina acts
        /// first"), which is a control's words. A Defense candidate's caption is the card's own NAME
        /// with its printed Defense chip under it, which is a reading of the card; painting a button
        /// around a card's name would say the name is the answer.</para>
        /// </summary>
        private static bool DrawsItsAnswerAsAControl(DecisionWindowFamily family) =>
            family == DecisionWindowFamily.TiedInitiative;

        private static string NameForCardControl(DecisionWindowFamily family)
        {
            switch (family)
            {
                case DecisionWindowFamily.Defense:
                    return "gd-defense-card";
                case DecisionWindowFamily.CardChoice:
                case DecisionWindowFamily.RemovalPrevention:
                    return "gd-effect-card";
                default:
                    return "gd-decision-window-card";
            }
        }

        /// <summary>
        /// Every label in this window wraps rather than clipping. <c>HudElements.Text</c> already sets
        /// <c>WhiteSpace.Normal</c>, but it also sets <c>flexShrink: 0</c>, which keeps a long line
        /// from ever being asked to wrap inside a bounded row - so the shrink is restored here and the
        /// element is bounded to its parent's width.
        /// </summary>
        private static void Wrapping(VisualElement element)
        {
            element.style.whiteSpace = WhiteSpace.Normal;
            element.style.flexShrink = 1f;
            element.style.maxWidth = Length.Percent(100f);
        }
    }
}
