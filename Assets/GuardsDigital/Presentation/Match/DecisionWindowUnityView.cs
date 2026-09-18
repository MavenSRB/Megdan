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

        /// <summary>The docked bar's own inside padding, which is tighter because the bar is slim.</summary>
        public const float DockedPadding = 8f;

        /// <summary>
        /// The verb banner's footprint - the optional step's own two numbers, because that pair IS the
        /// banner language this window inherits and two sizes for one control would be two languages.
        /// </summary>
        public const float VerbWidth = RoundActionUnityView.OptionalStepActionWidth;

        public const float VerbHeight = RoundActionUnityView.OptionalStepActionHeight;

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

        /// <summary>The words on the control that puts the window out of the way, and brings it back.</summary>
        public const string MinimiseLabel = "MINIMISE";

        public const string ExpandLabel = "EXPAND";

        private readonly VisualElement expandedHost;
        private readonly VisualElement dockedHost;
        private readonly Label expandedEyebrow;
        private readonly Label expandedTitle;
        private readonly Label expandedQuestion;
        private readonly Label expandedCount;
        private readonly VisualElement expandedStatements;
        private readonly VisualElement expandedContext;
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

        public DecisionWindowUnityView()
        {
            Root = new VisualElement { name = "gd-decision-window" };
            Root.style.position = Position.Absolute;
            Root.style.flexDirection = FlexDirection.Column;
            Root.style.display = DisplayStyle.None;

            expandedHost = HudElements.Panel(MatchHudTheme.PanelBackground);
            expandedHost.name = "gd-decision-window-expanded";
            expandedHost.style.flexDirection = FlexDirection.Column;
            expandedHost.style.flexGrow = 1f;
            expandedHost.style.display = DisplayStyle.None;
            HudElements.Padding(expandedHost, WindowPadding);
            HudElements.Border(
                expandedHost, MatchHudTheme.PanelBorder, 2f, MatchHudTheme.BandCornerRadius);
            Root.Add(expandedHost);

            var header = new VisualElement { name = "gd-decision-window-header" };
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.FlexStart;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.flexShrink = 0f;
            expandedHost.Add(header);

            // The expanded rectangle is ONE shape for every family - the whole open area between the
            // rails - so the content is centred inside it rather than hugging a corner of a window
            // that is nearly two thousand units wide whatever the question is.
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
            header.Add(minimiseControl);

            expandedQuestion = HudElements.Text(
                string.Empty, QuestionFontSize, MatchHudTheme.PrimaryText);
            expandedQuestion.name = "gd-decision-window-question";
            expandedQuestion.style.marginTop = 6f;
            expandedQuestion.style.unityTextAlign = TextAnchor.MiddleCenter;
            Wrapping(expandedQuestion);
            expandedHost.Add(expandedQuestion);

            expandedCount = HudElements.Text(
                string.Empty, CountFontSize, MatchHudTheme.MutedText, FontStyle.Bold);
            expandedCount.name = "gd-decision-window-count";
            expandedCount.style.marginTop = 2f;
            Wrapping(expandedCount);
            expandedCount.style.unityTextAlign = TextAnchor.MiddleCenter;
            expandedHost.Add(expandedCount);

            expandedStatements = new VisualElement { name = "gd-decision-window-statements" };
            expandedStatements.style.flexDirection = FlexDirection.Column;
            expandedStatements.style.marginTop = 2f;
            expandedStatements.style.flexShrink = 0f;
            expandedStatements.style.alignItems = Align.Center;
            expandedHost.Add(expandedStatements);

            var body = new VisualElement { name = "gd-decision-window-body" };
            body.style.flexDirection = FlexDirection.Row;
            body.style.alignItems = Align.FlexStart;
            body.style.justifyContent = Justify.Center;
            body.style.flexWrap = Wrap.NoWrap;
            body.style.marginTop = 10f;
            body.style.flexGrow = 1f;
            expandedHost.Add(body);

            expandedContext = new VisualElement { name = "gd-decision-window-context" };
            expandedContext.style.flexDirection = FlexDirection.Row;
            expandedContext.style.alignItems = Align.FlexStart;
            expandedContext.style.flexShrink = 0f;
            expandedContext.style.marginRight = ContextGap;
            expandedContext.style.display = DisplayStyle.None;
            body.Add(expandedContext);

            expandedControls = new VisualElement { name = "gd-decision-window-controls" };
            expandedControls.style.flexDirection = FlexDirection.Row;
            expandedControls.style.alignItems = Align.FlexStart;
            expandedControls.style.justifyContent = Justify.Center;
            expandedControls.style.flexWrap = Wrap.Wrap;
            expandedControls.style.flexGrow = 1f;
            expandedControls.style.flexShrink = 1f;
            body.Add(expandedControls);

            expandedCorner = new VisualElement { name = "gd-decision-window-corner" };
            expandedCorner.style.flexDirection = FlexDirection.Row;
            expandedCorner.style.justifyContent = Justify.FlexEnd;
            expandedCorner.style.alignItems = Align.Center;
            expandedCorner.style.flexShrink = 0f;
            expandedCorner.style.marginTop = 8f;
            expandedCorner.style.display = DisplayStyle.None;
            expandedHost.Add(expandedCorner);

            // The docked bar is TWO ROWS rather than one, which is a width decision and not a taste
            // one: it is bounded by the round dock's 720 units and yields its left edge to the Red
            // rail, so at the reference it is 444 wide. One row would leave a wrapped question a few
            // dozen units beside a verb, a count and EXPAND. Two rows give each line the bar's whole
            // width, and the host clips rather than spilling past the rectangle the renderer states.
            dockedHost = HudElements.Panel(MatchHudTheme.PanelBackground);
            dockedHost.name = "gd-decision-window-docked";
            dockedHost.style.flexDirection = FlexDirection.Column;
            dockedHost.style.justifyContent = Justify.Center;
            dockedHost.style.flexGrow = 1f;
            dockedHost.style.overflow = Overflow.Hidden;
            dockedHost.style.display = DisplayStyle.None;
            HudElements.Padding(dockedHost, DockedPadding);
            HudElements.Border(dockedHost, MatchHudTheme.PanelBorder, 1f, 4f);
            Root.Add(dockedHost);

            var dockedTopRow = new VisualElement { name = "gd-decision-window-docked-top" };
            dockedTopRow.style.flexDirection = FlexDirection.Row;
            dockedTopRow.style.alignItems = Align.Center;
            dockedTopRow.style.flexShrink = 0f;
            dockedHost.Add(dockedTopRow);

            dockedTitle = HudElements.Text(
                string.Empty, QuestionFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            dockedTitle.name = "gd-decision-window-docked-title";
            dockedTitle.style.letterSpacing = 0.4f;
            dockedTitle.style.flexGrow = 1f;
            Wrapping(dockedTitle);
            dockedTopRow.Add(dockedTitle);

            dockedCount = HudElements.Text(
                string.Empty, CountFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            dockedCount.name = "gd-decision-window-docked-count";
            dockedCount.style.flexShrink = 0f;
            dockedCount.style.marginLeft = 8f;
            dockedTopRow.Add(dockedCount);

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
            dockedBottomRow.style.marginTop = 4f;
            dockedHost.Add(dockedBottomRow);

            dockedQuestion = HudElements.Text(
                string.Empty, CountFontSize, MatchHudTheme.MutedText);
            dockedQuestion.name = "gd-decision-window-docked-question";
            dockedQuestion.style.flexGrow = 1f;
            Wrapping(dockedQuestion);
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
                var line = HudElements.Text(statement, CaptionFontSize, MatchHudTheme.MutedText);
                line.name = "gd-decision-window-statement";
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

            // Milestone 8.46g. p.15's question is not a row of independent answers: it is the seat's
            // own hand above three PAIRS, and each pair is one card the hero may upgrade. It is laid
            // out as the one shape it is rather than fed through the generic control loop.
            if (Model.Family == DecisionWindowFamily.LevelUp)
            {
                // A DEFINITE height for the offer to be bounded against. The arithmetic in
                // `LevelUpHandPreviewHeight` is what makes the offer fit; this is what keeps a
                // candidate face inside the rectangle even if it one day did not - the preview row
                // gives its height up first, and the row of faces the player is choosing between is
                // the last thing in this window that may move.
                expandedControls.style.height = Length.Percent(100f);
                expandedControls.Add(BuildLevelUpOffer());
            }
            else
            {
                expandedControls.style.height = StyleKeyword.Auto;
                for (var index = 0; index < Model.Controls.Count; index++)
                {
                    var control = Model.Controls[index];
                    var element = control.Card != null
                        ? BuildCardControl(control)
                        : control.IsPrimary
                            ? BuildVerbBanner(control)
                            : (VisualElement)BuildWordedControl(control);
                    if (control.Hero.IsDefined)
                    {
                        var hero = control.Hero;
                        HudElements.OnPointedAt(element, pointedAt =>
                            HeroHighlighted?.Invoke(pointedAt ? hero : default));
                    }

                    element.style.marginRight = index == Model.Controls.Count - 1 ? 0f : CardGap;
                    element.style.marginBottom = CardGap;
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
            dockedQuestion.text = Model.Question;
            dockedQuestion.style.display = string.IsNullOrEmpty(Model.Question)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            dockedCount.text = Model.Count ?? string.Empty;
            dockedCount.style.display = string.IsNullOrEmpty(Model.Count)
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            // "at most one verb" - the docked bar's own rule. A docked decision that offers several
            // answers is answered on the board; the one verb here is the ratification.
            var verb = Model.Controls.FirstOrDefault(control => control.Card == null);
            if (verb != null)
            {
                var element = verb.IsPrimary
                    ? BuildVerbBanner(verb, compact: true)
                    : (VisualElement)BuildWordedControl(verb);
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
        /// The one control that moves the game on, in the HUD's own pewter-banner language - the
        /// swallowtailed silver cloth END TURN, CONFIRM FACEDOWN, PASS and the optional step's two
        /// answers already wear. It is the same class, not a copy of it.
        /// </summary>
        /// <remarks>
        /// <para>The shine follows the rule that language already carries: a banner standing ALONE in
        /// the window is the player's one move and takes the full warm glow, exactly as END TURN does
        /// when nothing else is offered; two banners beside each other (a printed `may`'s two answers)
        /// withhold their shine until the pointer enters, as those two do today; and a single banner
        /// beside other controls keeps the modest steady glow.</para>
        /// <para>The Button's own <c>text</c> stays set under the cloth, because it is the selector a
        /// journey finds this control by - the same reason <c>PewterBanner</c>'s own remarks give for
        /// END TURN.</para>
        /// </remarks>
        private RoundActionUnityView.PewterBanner BuildVerbBanner(
            DecisionWindowControlViewModel control, bool compact = false)
        {
            var captured = control;
            var banners = Model.Controls.Count(value => value.IsPrimary && value.Card == null);
            var alone = Model.Controls.Count == 1;
            var banner = new RoundActionUnityView.PewterBanner(
                () =>
                {
                    if (captured.IsAnswerable)
                    {
                        ControlActivated?.Invoke(captured);
                    }
                },
                alone && banners == 1,
                DirectorHandsUnityView.CornerBannerFontSize,
                "gd-decision-verb-label",
                hoverOnlyShine: banners > 1)
            {
                name = "gd-decision-verb",
                text = control.Label
            };
            banner.SetVisibleLabel(control.Label);
            banner.style.minHeight = VerbHeight;
            banner.style.height = VerbHeight;
            banner.style.whiteSpace = WhiteSpace.Normal;
            if (compact)
            {
                // The slim bar is bounded by the round dock's own width and shares its lower row with
                // the question and the corner answer, so the verb is content-sized there rather than
                // taking the expanded window's fixed 240. It keeps the same cloth, height and motion.
                banner.style.minWidth = 0f;
                banner.style.flexShrink = 1f;
            }
            else
            {
                banner.style.width = VerbWidth;
                banner.style.flexShrink = 0f;
            }
            if (control.UnavailableReason != null)
            {
                banner.tooltip = control.UnavailableReason;
                HudElements.MarkUnavailable(banner);
            }

            return banner;
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
            element.Add(caption);

            if (!string.IsNullOrEmpty(control.ValueChip))
            {
                var chip = HudElements.Text(
                    control.ValueChip, ChipFontSize, MatchHudTheme.ConfirmAccent, FontStyle.Bold);
                chip.name = "gd-decision-window-card-chip";
                chip.style.unityTextAlign = TextAnchor.MiddleCenter;
                chip.style.width = CardFaceWidth;
                Wrapping(chip);
                element.Add(chip);
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
        /// The whole level-up offer: the seat's own hand above it, and p.15's three source-card pairs
        /// of complete candidate faces below.
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

            var preview = BuildLevelUpHandPreviewRow();
            if (preview != null)
            {
                offer.Add(preview);
            }

            offer.Add(BuildUpgradeComparisonBand());
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
            row.style.marginBottom = LevelUpRowGap;
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

        private VisualElement BuildContextCard(DecisionWindowCardViewModel card)
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
                var caption = HudElements.Text(
                    card.Caption, ChipFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
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
