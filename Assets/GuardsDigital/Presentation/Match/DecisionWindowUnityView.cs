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
    /// Milestone 8.46c. The decision window's UI Toolkit surface: one expanded host that covers the
    /// board only, one docked bar in the round dock's own rectangle, and nothing else.
    ///
    /// <para><b>Element names, because a journey addresses this surface by them.</b> The root is
    /// <c>gd-decision-window</c>; its two hosts are <c>gd-decision-window-expanded</c> and
    /// <c>gd-decision-window-docked</c>. Inside them: <c>gd-decision-window-eyebrow</c>,
    /// <c>-title</c>, <c>-question</c>, <c>-count</c>, <c>-statements</c>, <c>-context</c> (the cards
    /// the question is about but does not offer), <c>-controls</c> (the answers), <c>-corner</c> (the
    /// small answer that is not a choice), <c>-minimise</c> and <c>-expand</c>. A worded control is
    /// <c>gd-decision-control</c> and carries its label as the button's own <c>text</c>, so
    /// <c>UiPlayer.Button(hud, label)</c> finds it exactly as it found the strip's controls. A
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
        /// How large a complete card face is drawn inside the window, as a share of
        /// <see cref="CardFootprint"/>.
        ///
        /// <para><b>Computed against the narrowest expanded window this HUD can produce, not chosen by
        /// eye.</b> The expanded window is <c>CalculatePlayfieldRect</c> inset by the standing 12-unit
        /// margin, and the playfield's width is bounded by its own height at 16:9 and wider - so the
        /// narrowest it ever is is the reference one: 1044 wide, 1020 after the inset, 988 inside this
        /// window's own 16-unit padding. The user's rule is that a Defense of FOUR candidates fits, and
        /// the widest such window draws five faces at once (the attacker's card plus four answers) with
        /// <see cref="CardGap"/> between the answers and <see cref="ContextGap"/> after the attacker:
        /// 5 x 300.4615 x 0.6 = 901.4, plus 24 + 3 x 8 = 48 of gutter, is 949.4 against 988. It fits at
        /// the reference with 38.6 units to spare, and every wider or taller panel has more.</para>
        /// </summary>
        public const float CardFaceScale = 0.6f;

        public const float CardFaceWidth = CardFootprint.Width * CardFaceScale;
        public const float CardFaceHeight = CardFootprint.Height * CardFaceScale;

        /// <summary>The space between two answer faces.</summary>
        public const float CardGap = 8f;

        /// <summary>The space between the cards the question is ABOUT and the answers to it.</summary>
        public const float ContextGap = 24f;

        /// <summary>The window's own inside padding, inside the rectangle the renderer hands down.</summary>
        public const float WindowPadding = 16f;

        /// <summary>The docked bar's own inside padding, which is tighter because the bar is slim.</summary>
        public const float DockedPadding = 8f;

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

            var headerText = new VisualElement();
            headerText.style.flexDirection = FlexDirection.Column;
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
            Wrapping(expandedQuestion);
            expandedHost.Add(expandedQuestion);

            expandedCount = HudElements.Text(
                string.Empty, CountFontSize, MatchHudTheme.MutedText, FontStyle.Bold);
            expandedCount.name = "gd-decision-window-count";
            expandedCount.style.marginTop = 2f;
            Wrapping(expandedCount);
            expandedHost.Add(expandedCount);

            expandedStatements = new VisualElement { name = "gd-decision-window-statements" };
            expandedStatements.style.flexDirection = FlexDirection.Column;
            expandedStatements.style.marginTop = 2f;
            expandedStatements.style.flexShrink = 0f;
            expandedHost.Add(expandedStatements);

            var body = new VisualElement { name = "gd-decision-window-body" };
            body.style.flexDirection = FlexDirection.Row;
            body.style.alignItems = Align.FlexStart;
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

            dockedHost = HudElements.Panel(MatchHudTheme.PanelBackground);
            dockedHost.name = "gd-decision-window-docked";
            dockedHost.style.flexDirection = FlexDirection.Row;
            dockedHost.style.alignItems = Align.Center;
            dockedHost.style.flexGrow = 1f;
            dockedHost.style.display = DisplayStyle.None;
            HudElements.Padding(dockedHost, DockedPadding);
            HudElements.Border(dockedHost, MatchHudTheme.PanelBorder, 1f, 4f);
            Root.Add(dockedHost);

            var dockedText = new VisualElement();
            dockedText.style.flexDirection = FlexDirection.Column;
            dockedText.style.flexGrow = 1f;
            dockedText.style.flexShrink = 1f;
            dockedHost.Add(dockedText);

            dockedTitle = HudElements.Text(
                string.Empty, QuestionFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            dockedTitle.name = "gd-decision-window-docked-title";
            dockedTitle.style.letterSpacing = 0.4f;
            Wrapping(dockedTitle);
            dockedText.Add(dockedTitle);

            dockedQuestion = HudElements.Text(
                string.Empty, CountFontSize, MatchHudTheme.MutedText);
            dockedQuestion.name = "gd-decision-window-docked-question";
            Wrapping(dockedQuestion);
            dockedText.Add(dockedQuestion);

            dockedCount = HudElements.Text(
                string.Empty, CountFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            dockedCount.name = "gd-decision-window-docked-count";
            dockedCount.style.flexShrink = 0f;
            dockedCount.style.marginLeft = 10f;
            dockedHost.Add(dockedCount);

            dockedControls = new VisualElement { name = "gd-decision-window-docked-controls" };
            dockedControls.style.flexDirection = FlexDirection.Row;
            dockedControls.style.alignItems = Align.Center;
            dockedControls.style.flexShrink = 0f;
            dockedControls.style.marginLeft = 10f;
            dockedHost.Add(dockedControls);

            dockedCorner = new VisualElement { name = "gd-decision-window-docked-corner" };
            dockedCorner.style.flexDirection = FlexDirection.Row;
            dockedCorner.style.alignItems = Align.Center;
            dockedCorner.style.flexShrink = 0f;
            dockedCorner.style.marginLeft = 8f;
            dockedCorner.style.display = DisplayStyle.None;
            dockedHost.Add(dockedCorner);

            expandControl = HudElements.Control(
                ExpandLabel, () => StateToggleRequested?.Invoke(), 26f);
            expandControl.name = "gd-decision-window-expand";
            expandControl.style.fontSize = CornerFontSize;
            expandControl.style.flexShrink = 0f;
            expandControl.style.marginLeft = 8f;
            dockedHost.Add(expandControl);
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
                    : BuildWordedControl(control);
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
                var element = BuildWordedControl(verb);
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

            if (control.IsPrimary)
            {
                element.style.backgroundColor = MatchHudTheme.ConfirmAccent;
                element.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
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
        /// One card-faced answer: the whole printed face at the declared window scale, its consequence
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
        /// The tray's own complete face, drawn at <see cref="CardFaceScale"/>.
        /// </summary>
        /// <remarks>
        /// The face is built at its full pinned footprint and SCALED, rather than built smaller: the
        /// footprint is pinned on both ends by <see cref="CardFootprint"/> precisely so no container
        /// can resize a card, and a transform is how milestone 8.35z58's level-up hand preview already
        /// draws the same face smaller without arguing with that. The whole subtree is made
        /// pointer-transparent for milestone 8.35z62's measured reason: UI Toolkit picks the DEEPEST
        /// element under the pointer, so a face child that kept the default picking mode would take the
        /// click from the control it is drawn inside.
        /// </remarks>
        private static VisualElement BuildFace(TrayCardViewModel card, CardShineSlot shineSlot)
        {
            var slot = new VisualElement { name = "gd-decision-window-face" };
            slot.style.width = CardFaceWidth;
            slot.style.height = CardFaceHeight;
            slot.style.minWidth = CardFaceWidth;
            slot.style.minHeight = CardFaceHeight;
            slot.style.maxWidth = CardFaceWidth;
            slot.style.maxHeight = CardFaceHeight;
            slot.style.flexShrink = 0f;
            slot.style.overflow = Overflow.Hidden;
            slot.pickingMode = PickingMode.Ignore;

            var face = DirectorHandsUnityView.BuildStandaloneCard(card);
            if (shineSlot != CardShineSlot.None)
            {
                DirectorHandsUnityView.AddFaceShine(face, card, shineSlot);
            }

            face.style.position = Position.Absolute;
            face.style.left = 0f;
            face.style.top = 0f;
            face.style.width = CardFootprint.Width;
            face.style.height = CardFootprint.Height;
            face.style.scale = new Scale(new Vector3(CardFaceScale, CardFaceScale, 1f));
            face.style.transformOrigin = new TransformOrigin(new Length(0f), new Length(0f));
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
