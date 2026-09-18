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
                    new KeyValuePair<DecisionWindowControlViewModel, VisualElement>(control, element));
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
