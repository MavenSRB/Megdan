using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Presentation-only state for one possible card drag. It knows pointer distance and identity, but
    /// neither map geometry nor card-choice legality; those are rechecked by their existing owners.
    /// </summary>
    internal sealed class CardCommitmentDragState
    {
        private int pointerId = -1;
        private CardInstanceId cardInstanceId;
        private Vector2 start;

        public bool IsTracking => pointerId >= 0;
        public int PointerId => pointerId;
        public bool IsDragging { get; private set; }
        public CardInstanceId CardInstanceId => cardInstanceId;

        public bool Begin(int nextPointerId, CardInstanceId nextCardInstanceId, Vector2 position)
        {
            if (IsTracking || nextPointerId < 0 || !nextCardInstanceId.IsDefined)
            {
                return false;
            }

            pointerId = nextPointerId;
            cardInstanceId = nextCardInstanceId;
            start = position;
            IsDragging = false;
            return true;
        }

        public bool Move(int movedPointerId, Vector2 position, float threshold)
        {
            if (!IsTracking || movedPointerId != pointerId)
            {
                return false;
            }

            if (!IsDragging && (position - start).sqrMagnitude >= threshold * threshold)
            {
                IsDragging = true;
            }

            return IsDragging;
        }

        public bool Release(int releasedPointerId, out CardInstanceId releasedCard, out bool wasDrag)
        {
            releasedCard = default;
            wasDrag = false;
            if (!IsTracking || releasedPointerId != pointerId)
            {
                return false;
            }

            releasedCard = cardInstanceId;
            wasDrag = IsDragging;
            Reset();
            return true;
        }

        public void Cancel() => Reset();

        private void Reset()
        {
            pointerId = -1;
            cardInstanceId = default;
            start = default;
            IsDragging = false;
        }
    }

    /// <summary>
    /// The element vocabulary every configured HUD surface is built from.
    ///
    /// Milestone 7.8a. Styles are set here in C# rather than in a stylesheet asset, deliberately: the
    /// configured scene composes its HUD in code and owns no UI assets, so a stylesheet would be a second
    /// place the look lived and a second thing an import could lose. Pseudo-state - pointed at, focused,
    /// selected, disabled - is applied through the same explicit callbacks that already tell the board
    /// which piece a control means, so there is exactly one path from "the pointer is on this control" to
    /// everything that follows from it.
    /// </summary>
    internal static class HudElements
    {
        internal const float ControlHeight = 46f;

        internal static VisualElement Row(float gap = 6f)
        {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = Align.Center;
            element.style.flexWrap = Wrap.Wrap;
            Gap(element, gap);
            return element;
        }

        internal static VisualElement Column(float gap = 4f)
        {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Column;
            Gap(element, gap);
            return element;
        }

        private static void Gap(VisualElement element, float gap)
        {
            // `row-gap`/`column-gap` are not available on every supported runtime, so spacing is a
            // margin on the container's own children, applied when they are added.
            element.userData = gap;
        }

        internal static T Add<T>(this VisualElement parent, T child) where T : VisualElement
        {
            if (parent.childCount > 0 && parent.userData is float gap)
            {
                if (parent.resolvedStyle.flexDirection == FlexDirection.Row ||
                    parent.style.flexDirection.value == FlexDirection.Row)
                {
                    child.style.marginLeft = gap;
                }
                else
                {
                    child.style.marginTop = gap;
                }
            }

            parent.Add(child);
            return child;
        }

        internal static Label Text(
            string value,
            float size = 12f,
            Color? color = null,
            FontStyle style = FontStyle.Normal)
        {
            var label = new Label(value ?? string.Empty);
            label.style.fontSize = size;
            label.style.color = color ?? MatchHudTheme.PrimaryText;
            label.style.unityFontStyleAndWeight = style;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexShrink = 0f;
            return label;
        }

        /// <summary>One code-native copy of a catalogued printed channel symbol.</summary>
        internal static VisualElement PrintedChannelSymbol(
            PrintedValueChannel channel,
            float size = 15f,
            Color? color = null) =>
            new PrintedChannelSymbolElement(
                MatchHudTheme.ChannelSymbol(channel), size, color ?? MatchHudTheme.MutedText);

        /// <summary>
        /// One control. It sizes to its own complete label - no width is ever set - which is what makes
        /// milestone 5.1c's clipping guarantee structural here rather than a measurement: a label cannot
        /// be cut off by a width nothing assigned.
        /// </summary>
        internal static Button Control(string label, Action action, float minHeight = ControlHeight)
        {
            var button = new Button(action ?? (() => { }))
            {
                text = label ?? string.Empty
            };
            button.style.minHeight = minHeight;
            button.style.paddingLeft = 10f;
            button.style.paddingRight = 10f;
            button.style.paddingTop = 6f;
            button.style.paddingBottom = 6f;
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.flexShrink = 0f;
            button.style.fontSize = 12f;
            button.style.color = MatchHudTheme.PrimaryText;
            button.style.backgroundColor = new Color(0.145f, 0.169f, 0.184f, 1f);
            Border(button, MatchHudTheme.PanelBorder, 1f, 4f);
            return button;
        }

        internal static void Border(VisualElement element, Color color, float width, float radius)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        internal static void Padding(VisualElement element, float value)
        {
            element.style.paddingLeft = value;
            element.style.paddingRight = value;
            element.style.paddingTop = value;
            element.style.paddingBottom = value;
        }

        /// <summary>
        /// Marks one control as selected, using the one selection treatment the whole HUD shares: an
        /// outline, never a colour swap, so a card keeps its own printed colour while it is chosen.
        /// </summary>
        internal static void MarkSelected(VisualElement element, bool selected)
        {
            element.style.borderTopColor = selected ? MatchHudTheme.SelectedOutline : MatchHudTheme.PanelBorder;
            element.style.borderBottomColor = element.style.borderTopColor;
            element.style.borderLeftColor = element.style.borderTopColor;
            element.style.borderRightColor = element.style.borderTopColor;
            var width = selected ? 3f : 1f;
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
        }

        /// <summary>
        /// The same selection treatment, on a control whose size must not move when it is chosen.
        ///
        /// Milestone 8.5a. <see cref="MarkSelected"/> draws the outline by widening the border from one
        /// pixel to three, which is invisible on a control sized by its own label and wrong on a tray
        /// card: the card's width is fixed and its height comes from its content, so four pixels of
        /// border re-wrapped its rules text, made that card taller, and - because the row stretches every
        /// card to the tallest one - grew the whole tray each time a player picked a card up. Here the
        /// outline is always drawn and only its colour changes, so choosing a card moves nothing.
        /// </summary>
        internal static void MarkSelectedWithoutResizing(VisualElement element, bool selected)
        {
            var color = selected ? MatchHudTheme.SelectedOutline : MatchHudTheme.PanelBorder;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopWidth = 3f;
            element.style.borderBottomWidth = 3f;
            element.style.borderLeftWidth = 3f;
            element.style.borderRightWidth = 3f;
        }

        /// <summary>
        /// A control the authority offers no command for. It keeps its label and its place - milestone
        /// 6.5ac's and 6.5af's decision - and reads as unavailable through opacity as well as through
        /// being inert, so the state is never carried by colour alone.
        /// </summary>
        internal static void MarkUnavailable(VisualElement element)
        {
            element.SetEnabled(false);
            element.style.opacity = 0.45f;
        }

        /// <summary>
        /// An unavailable complete card in an overlapped hand. The card remains an inert, visibly
        /// dimmed control, but its own surface stays opaque: lowering the whole element's opacity lets
        /// every overlapping sibling show through it and turns two spent cards into one muddled image.
        /// A veil over an opaque face produces the same hierarchy without exposing the card below.
        /// </summary>
        internal static void MarkUnavailableOpaque(VisualElement element)
        {
            element.SetEnabled(false);
            element.style.opacity = 1f;

            AddUnavailableVeil(element);
        }

        /// <summary>
        /// Milestone 8.30l's unavailable-card group remains inspectable by pointer and keyboard while
        /// carrying no command. The view model and every callback still refuse selection/drag; unlike
        /// the old mixed-hand treatment, the outer face therefore stays enabled so hover and focus can
        /// expose it without weakening the same opaque veil.
        /// </summary>
        internal static void MarkUnavailableOpaqueInspectable(VisualElement element)
        {
            element.SetEnabled(true);
            element.focusable = true;
            element.style.opacity = 1f;

            AddUnavailableVeil(element);
        }

        private static void AddUnavailableVeil(VisualElement element)
        {

            var veil = new VisualElement { name = "gd-card-unavailable-veil" };
            veil.style.position = Position.Absolute;
            veil.style.left = 0f;
            veil.style.top = 0f;
            veil.style.right = 0f;
            veil.style.bottom = 0f;
            veil.style.backgroundColor = new Color(0.01f, 0.014f, 0.018f, 0.55f);
            veil.pickingMode = PickingMode.Ignore;
            element.Add(veil);
        }

        /// <summary>
        /// A control that carries no command but is not spent: inert, and drawn at full strength.
        ///
        /// Milestone 8.5b, for the card the current turn is being resolved from. It is the same
        /// inertness <see cref="MarkUnavailable"/> applies - no command, no focus stop, no keyboard-order
        /// change - without the dimming, because the card in play is the opposite of unavailable to look
        /// at. The opacity is set explicitly rather than left alone so that a disabled control's own
        /// theme dimming cannot reintroduce it.
        /// </summary>
        internal static void MarkInPlay(VisualElement element)
        {
            // A resolving card is not a commitment control, but its printed descendants can now be
            // action controls. Keep the inert outer face enabled and remove its focus stop instead of
            // disabling the whole subtree.
            element.SetEnabled(true);
            element.focusable = false;
            element.style.opacity = 1f;
        }

        /// <summary>
        /// Reports when the pointer enters or leaves a control, and when keyboard focus does, as one
        /// event. Every board cue in this HUD is driven from here, so a pointed-at control and a focused
        /// control mean the same thing to the board.
        /// </summary>
        internal static void OnPointedAt(VisualElement element, Action<bool> pointedAt)
        {
            element.RegisterCallback<PointerEnterEvent>(_ => pointedAt(true));
            element.RegisterCallback<PointerLeaveEvent>(_ => pointedAt(false));
            element.RegisterCallback<FocusInEvent>(_ => pointedAt(true));
            element.RegisterCallback<FocusOutEvent>(_ => pointedAt(false));
        }

        internal static VisualElement Panel(Color background)
        {
            var element = new VisualElement();
            element.style.backgroundColor = background;
            return element;
        }
    }

    /// <summary>
    /// A small code-native rendering of one established printed channel symbol. Card fronts and the
    /// rulebook are reference sources only: this element reads the catalogued typed identity and never
    /// samples, crops, parses or substitutes pixels from either source.
    /// </summary>
    internal sealed class PrintedChannelSymbolElement : VisualElement
    {
        private readonly PrintedChannelSymbolDefinition definition;
        private readonly Color ink;

        internal PrintedChannelSymbolElement(
            PrintedChannelSymbolDefinition definition,
            float size,
            Color ink)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.ink = ink;
            name = "gd-printed-channel-symbol";
            viewDataKey = definition.ResourceId;
            pickingMode = PickingMode.Ignore;
            focusable = false;
            style.width = size;
            style.height = size;
            style.minWidth = size;
            style.minHeight = size;
            style.maxWidth = size;
            style.maxHeight = size;
            style.flexShrink = 0f;
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 4f || rect.height < 4f)
            {
                return;
            }

            var painter = context.painter2D;
            switch (definition.Kind)
            {
                case PrintedChannelSymbolKind.InitiativeLightning:
                    FillPolygon(painter, rect, new[]
                    {
                        new Vector2(0.58f, 0.04f), new Vector2(0.20f, 0.56f),
                        new Vector2(0.47f, 0.56f), new Vector2(0.36f, 0.96f),
                        new Vector2(0.82f, 0.40f), new Vector2(0.55f, 0.40f)
                    });
                    break;
                case PrintedChannelSymbolKind.AttackCrossedSwords:
                    DrawSword(painter, rect, new Vector2(0.18f, 0.14f), new Vector2(0.80f, 0.86f));
                    DrawSword(painter, rect, new Vector2(0.82f, 0.14f), new Vector2(0.20f, 0.86f));
                    break;
                case PrintedChannelSymbolKind.DefenseShield:
                    FillAndStrokePolygon(painter, rect, new[]
                    {
                        new Vector2(0.50f, 0.05f), new Vector2(0.86f, 0.20f),
                        new Vector2(0.80f, 0.67f), new Vector2(0.50f, 0.94f),
                        new Vector2(0.20f, 0.67f), new Vector2(0.14f, 0.20f)
                    });
                    break;
                case PrintedChannelSymbolKind.MovementBoot:
                    FillPolygon(painter, rect, new[]
                    {
                        new Vector2(0.22f, 0.08f), new Vector2(0.55f, 0.08f),
                        new Vector2(0.55f, 0.48f), new Vector2(0.72f, 0.65f),
                        new Vector2(0.92f, 0.68f), new Vector2(0.92f, 0.88f),
                        new Vector2(0.16f, 0.88f), new Vector2(0.10f, 0.66f),
                        new Vector2(0.28f, 0.55f)
                    });
                    break;
                case PrintedChannelSymbolKind.RangeArrow:
                    StrokeLine(painter, rect, new Vector2(0.08f, 0.50f), new Vector2(0.80f, 0.50f), 2.1f);
                    FillPolygon(painter, rect, new[]
                    {
                        new Vector2(0.92f, 0.50f), new Vector2(0.66f, 0.25f),
                        new Vector2(0.66f, 0.75f)
                    });
                    StrokeLine(painter, rect, new Vector2(0.18f, 0.28f), new Vector2(0.18f, 0.72f), 1.7f);
                    break;
                case PrintedChannelSymbolKind.RadiusBurst:
                    StrokePolygon(painter, rect, new[]
                    {
                        new Vector2(0.50f, 0.07f), new Vector2(0.80f, 0.20f),
                        new Vector2(0.93f, 0.50f), new Vector2(0.80f, 0.80f),
                        new Vector2(0.50f, 0.93f), new Vector2(0.20f, 0.80f),
                        new Vector2(0.07f, 0.50f), new Vector2(0.20f, 0.20f)
                    }, 1.8f);
                    FillPolygon(painter, rect, new[]
                    {
                        new Vector2(0.50f, 0.34f), new Vector2(0.66f, 0.50f),
                        new Vector2(0.50f, 0.66f), new Vector2(0.34f, 0.50f)
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawSword(Painter2D painter, Rect rect, Vector2 start, Vector2 end)
        {
            StrokeLine(painter, rect, start, end, 1.7f);
            var direction = (end - start).normalized;
            var normal = new Vector2(-direction.y, direction.x) * 0.13f;
            var guard = start + direction * 0.20f;
            StrokeLine(painter, rect, guard - normal, guard + normal, 1.5f);
        }

        private void FillPolygon(Painter2D painter, Rect rect, IReadOnlyList<Vector2> points)
        {
            BeginPolygon(painter, rect, points);
            painter.fillColor = ink;
            painter.Fill();
        }

        private void FillAndStrokePolygon(
            Painter2D painter, Rect rect, IReadOnlyList<Vector2> points)
        {
            BeginPolygon(painter, rect, points);
            painter.fillColor = new Color(ink.r, ink.g, ink.b, ink.a * 0.28f);
            painter.Fill();
            BeginPolygon(painter, rect, points);
            painter.strokeColor = ink;
            painter.lineWidth = 1.5f;
            painter.lineJoin = LineJoin.Round;
            painter.Stroke();
        }

        private void StrokePolygon(
            Painter2D painter, Rect rect, IReadOnlyList<Vector2> points, float width)
        {
            BeginPolygon(painter, rect, points);
            painter.strokeColor = ink;
            painter.lineWidth = width;
            painter.lineJoin = LineJoin.Round;
            painter.Stroke();
        }

        private static void BeginPolygon(
            Painter2D painter, Rect rect, IReadOnlyList<Vector2> points)
        {
            painter.BeginPath();
            painter.MoveTo(ToRect(rect, points[0]));
            for (var index = 1; index < points.Count; index++)
            {
                painter.LineTo(ToRect(rect, points[index]));
            }

            painter.ClosePath();
        }

        private void StrokeLine(
            Painter2D painter, Rect rect, Vector2 start, Vector2 end, float width)
        {
            painter.BeginPath();
            painter.MoveTo(ToRect(rect, start));
            painter.LineTo(ToRect(rect, end));
            painter.strokeColor = ink;
            painter.lineWidth = width;
            painter.lineJoin = LineJoin.Round;
            painter.Stroke();
        }

        private static Vector2 ToRect(Rect rect, Vector2 normalized) =>
            new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
    }

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
        private readonly VisualElement tieBreakerChip;
        private readonly VisualElement tieBreakerDisc;
        private readonly VisualElement content;
        private readonly VisualElement identityRow;
        private readonly VisualElement narrationLane;

        public MatchStatusBarUnityView()
        {
            Root = HudElements.Panel(MatchHudTheme.TopBarBackground);
            Root.name = "gd-topbar";
            Root.style.position = Position.Absolute;
            // Milestone 8.30b: two stacked rows rather than one. The identity row keeps every fact it
            // carried and the measurements taken of it; the narration lane beneath it is what the
            // user-approved amendment added.
            Root.style.flexDirection = FlexDirection.Column;
            Root.style.alignItems = Align.Stretch;
            Root.style.justifyContent = Justify.FlexStart;
            Root.style.overflow = Overflow.Hidden;
            Root.style.paddingLeft = 14f;
            Root.style.paddingRight = 12f;
            Root.style.borderBottomWidth = 1f;
            Root.style.borderBottomColor = MatchHudTheme.PanelBorder;

            identityRow = new VisualElement { name = "gd-topbar-identity" };
            identityRow.style.flexDirection = FlexDirection.Row;
            identityRow.style.alignItems = Align.Center;
            identityRow.style.justifyContent = Justify.SpaceBetween;
            identityRow.style.flexShrink = 0f;
            identityRow.style.height = ConfiguredMatchHudRenderer.StatusBarIdentityRowHeight;
            Root.Add(identityRow);

            content = HudElements.Row(18f);
            content.style.flexShrink = 1f;
            identityRow.Add(content);

            // The lane the round view's own narration element is re-parented into. This is a host and
            // never a second renderer: the sentence is derived by the round presenter from the
            // projection, exactly as it was when it was drawn beside the cards, so there is still one
            // place that decides what the match is doing and one place that says it.
            narrationLane = new VisualElement { name = "gd-narration-lane" };
            narrationLane.style.flexDirection = FlexDirection.Row;
            narrationLane.style.alignItems = Align.Center;
            narrationLane.style.flexGrow = 0f;
            narrationLane.style.flexShrink = 0f;
            narrationLane.style.height = ConfiguredMatchHudRenderer.StatusBarNarrationHeight;
            narrationLane.style.overflow = Overflow.Hidden;
            narrationLane.style.paddingBottom = 4f;
            Root.Add(narrationLane);

            tieBreakerChip = HudElements.Panel(MatchHudTheme.EmptySlot);
            tieBreakerChip.name = "gd-tie-breaker";
            tieBreakerChip.style.position = Position.Absolute;
            tieBreakerChip.style.flexDirection = FlexDirection.Row;
            tieBreakerChip.style.alignItems = Align.Center;
            tieBreakerChip.style.justifyContent = Justify.Center;
            tieBreakerChip.style.backgroundColor = Color.clear;
            HudElements.Border(tieBreakerChip, Color.clear, 0f, 999f);

            tieBreakerDisc = new VisualElement { name = "gd-tie-breaker-disc" };
            tieBreakerDisc.style.width = ConfiguredMatchHudRenderer.TieBreakerDiameter;
            tieBreakerDisc.style.height = ConfiguredMatchHudRenderer.TieBreakerDiameter;
            tieBreakerDisc.style.flexShrink = 0f;
            tieBreakerDisc.style.alignItems = Align.Center;
            tieBreakerDisc.style.justifyContent = Justify.Center;
            HudElements.Border(tieBreakerDisc, Color.clear, 0f, 999f);
            tieBreakerChip.Add(tieBreakerDisc);
            Root.Add(tieBreakerChip);
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

        public MatchHudHeaderViewModel Model { get; private set; }

        /// <summary>Where the Tie Breaker chip sits, in the panel's own top-left-origin pixels.</summary>
        public Rect ChipRect { get; private set; }

        public void Layout(Rect bar, Rect chip)
        {
            Root.style.left = bar.x;
            Root.style.top = bar.y;
            Root.style.width = bar.width;
            Root.style.height = bar.height;
            ChipRect = chip;
            tieBreakerChip.style.left = chip.x - bar.x;
            tieBreakerChip.style.top = chip.y - bar.y;
            tieBreakerChip.style.width = chip.width;
            tieBreakerChip.style.height = chip.height;
            tieBreakerDisc.style.width = chip.width;
            tieBreakerDisc.style.height = chip.height;
            var leftLaneWidth = Mathf.Max(0f, chip.xMin - bar.xMin - 18f);
            content.style.width = leftLaneWidth;
            content.style.maxWidth = leftLaneWidth;
            // The narration remains one uninterrupted status line. End Turn is intentionally layered
            // over its right end instead of shortening the gold turn cue.
            var goldLaneWidth = Mathf.Max(0f, bar.width - 26f);
            narrationLane.style.width = goldLaneWidth;
            narrationLane.style.maxWidth = goldLaneWidth;
        }

        public void Render(MatchHudHeaderViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            content.Clear();
            tieBreakerDisc.Clear();

            content.Add(HudElements.Text(
                "GUARDS DIGITAL", 13f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            content.Add(HudElements.Text(model.RoundTurnLabel, 12f, MatchHudTheme.MutedText));
            content.Add(TeamChip(TeamSide.Red, model.RedLifeLabel));
            content.Add(HudElements.Text(model.WaveLabel, 12f, MatchHudTheme.MutedText));
            content.Add(TeamChip(TeamSide.Blue, model.BlueLifeLabel));

            tieBreakerChip.style.backgroundColor = Color.clear;
            tieBreakerDisc.style.backgroundColor = model.Color;
            var chipLabel = HudElements.Text(
                model.TieBreakerLabel, 14f, Color.white, FontStyle.Bold);
            chipLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            chipLabel.style.whiteSpace = WhiteSpace.NoWrap;
            chipLabel.style.flexGrow = 1f;
            tieBreakerDisc.Add(chipLabel);
            tieBreakerChip.tooltip = model.Text;
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

    /// <summary>
    /// The two edge-aligned hero overviews: identity, item bonuses, four turn slots, and the discard
    /// pile.
    ///
    /// It is one repeated component per projected hero, laid out by team side, so the configured
    /// one-hero-per-team scene draws one on each edge and a six-hero scene would draw six without this
    /// view learning anything new.
    /// </summary>
    public sealed class HeroOverviewsUnityView : IHeroOverviewsView
    {
        private readonly VisualElement redColumn;
        private readonly VisualElement blueColumn;
        private readonly VisualElement preview;

        /// <summary>
        /// The rail element each revealable model was drawn as, so a revealed face can be placed beside
        /// the thing the pointer is resting on.
        ///
        /// Keyed by object because two kinds of model are revealable since milestone 8.7a - a resolved
        /// turn slot and the current-card marker - and the one preview element belongs to whichever of
        /// them the pointer is on. Making it two dictionaries and two previews would have allowed two
        /// faces on screen at once, which is the one thing a hover reveal must not do.
        /// </summary>
        private readonly Dictionary<object, VisualElement> revealAnchors =
            new Dictionary<object, VisualElement>();

        private object highlighted;

        public HeroOverviewsUnityView()
        {
            Root = new VisualElement { name = "gd-hero-rails" };
            Root.style.position = Position.Absolute;
            Root.pickingMode = PickingMode.Ignore;
            redColumn = Column("gd-hero-rail-red");
            blueColumn = Column("gd-hero-rail-blue");
            preview = new VisualElement { name = "gd-turn-slot-preview" };
            preview.style.position = Position.Absolute;
            preview.style.display = DisplayStyle.None;
            preview.pickingMode = PickingMode.Ignore;
            Root.Add(redColumn);
            Root.Add(blueColumn);
            Root.Add(preview);
            redColumn.RegisterCallback<GeometryChangedEvent>(_ =>
                ColumnGeometryChanged?.Invoke());
            blueColumn.RegisterCallback<GeometryChangedEvent>(_ =>
                ColumnGeometryChanged?.Invoke());
        }

        public VisualElement Root { get; }

        /// <summary>
        /// The one complete card face revealed from a resolved turn, current card, Ultimate, or
        /// discard icon. It begins inside <see cref="Root"/> so the view is complete in isolation;
        /// the configured HUD reparents only this preview to its top layer so unrelated status and
        /// Director controls cannot paint over a card while it is being read.
        /// </summary>
        public VisualElement HoverPreviewRoot => preview;

        public HeroOverviewsViewModel Model { get; private set; }

        /// <summary>The two rails' current rects, for the pointer-blocking decision.</summary>
        public IEnumerable<VisualElement> BlockingRegions => new[] { redColumn, blueColumn };

        /// <summary>
        /// The turn slot the pointer or keyboard focus is resting on, or null when none is.
        ///
        /// Milestone 8.5a, by the user's 2026-08-21 decision. A round's earlier turns leave the tray as
        /// they are played, and the four slots are the only place they remain; resting on one shows that
        /// card's whole printed face, drawn by the same builder the tray uses. It is a reading of the
        /// model the rail was already rendered from - no command, no projection query, no new authority.
        /// </summary>
        public HeroTurnSlotViewModel HighlightedTurnSlot => highlighted as HeroTurnSlotViewModel;

        /// <summary>
        /// The current-card marker the pointer or keyboard focus is resting on, or null when none is.
        ///
        /// Milestone 8.7a. A revealed unresolved card is readable exactly the way a resolved one is -
        /// the same complete face, from the same builder - and a facedown marker reveals nothing,
        /// because there is nothing about it presentation is allowed to know.
        /// </summary>
        public HeroCurrentCardViewModel HighlightedCurrentCard => highlighted as HeroCurrentCardViewModel;

        /// <summary>
        /// The discard-pile icon the pointer is resting on, or null when none is.
        ///
        /// Milestone 8.11b. A discarded card is now drawn as its own icon rather than as a share of a
        /// count, so it reveals its own face - the same one preview, through the same builder, on the
        /// same terms as a resolved turn.
        /// </summary>
        public TrayCardViewModel HighlightedDiscardCard => highlighted as TrayCardViewModel;

        public event Action<HeroTurnSlotViewModel> TurnSlotHighlighted;

        public event Action<HeroCurrentCardViewModel> CurrentCardHighlighted;

        /// <summary>Milestone 8.11b: the pointed-at discard icon, on the terms of the two above.</summary>
        public event Action<TrayCardViewModel> DiscardCardHighlighted;

        /// <summary>
        /// Raised after either rail column resolves a new live bound. The configured HUD uses Red's
        /// edge to place `DIRECTOR` immediately after the third hero, and Blue's edge to expose only
        /// the genuinely empty lower-right area to the board fitter.
        /// </summary>
        public event Action ColumnGeometryChanged;

        /// <summary>The live content bound of the Red hero stack, when UI Toolkit has resolved it.</summary>
        public bool TryGetRedColumnBounds(out Rect bounds)
        {
            return TryGetColumnBounds(redColumn, out bounds);
        }

        /// <summary>
        /// The live Blue stack ends well above the cards. The board fitter uses that resolved edge to
        /// admit only the lower-right dead space beneath it, while keeping every hex clear of the rail.
        /// </summary>
        public bool TryGetBlueColumnBounds(out Rect bounds)
        {
            return TryGetColumnBounds(blueColumn, out bounds);
        }

        private static bool TryGetColumnBounds(VisualElement column, out Rect bounds)
        {
            bounds = default;
            if (column.panel == null ||
                column.resolvedStyle.display == DisplayStyle.None ||
                column.childCount == 0)
            {
                return false;
            }

            bounds = column.worldBound;
            return !float.IsNaN(bounds.width) && !float.IsNaN(bounds.height) &&
                bounds.width > 0f && bounds.height > 0f;
        }

        /// <summary>Reports a pointed-at slot directly, for a driver with no pointer.</summary>
        public void ForwardTurnSlotHighlight(HeroTurnSlotViewModel slot) =>
            // An empty slot has no card to show, so resting on one is the same as resting on nothing.
            SetHighlight(slot != null && slot.IsOccupied ? slot : null);

        /// <summary>Reports a pointed-at current-card marker directly, for a driver with no pointer.</summary>
        public void ForwardCurrentCardHighlight(HeroCurrentCardViewModel current) =>
            SetHighlight(current != null && current.Card != null ? current : null);

        /// <summary>Milestone 8.11b: reports a pointed-at discard icon, for a driver with no pointer.</summary>
        public void ForwardDiscardCardHighlight(TrayCardViewModel card) => SetHighlight(card);

        private void SetHighlight(object model)
        {
            if (ReferenceEquals(highlighted, model))
            {
                return;
            }

            var slotBefore = HighlightedTurnSlot;
            var currentBefore = HighlightedCurrentCard;
            var discardBefore = HighlightedDiscardCard;
            highlighted = model;
            preview.Clear();
            var card = CardOf(model);
            if (card == null)
            {
                highlighted = null;
                preview.style.display = DisplayStyle.None;
            }
            else
            {
                var face = DirectorHandsUnityView.BuildStandaloneCard(card);
                DirectorHandsUnityView.IgnorePickingRecursively(face);
                preview.Add(face);
                preview.style.display = DisplayStyle.Flex;
                PlacePreview(highlighted);
                preview.BringToFront();
            }

            if (!ReferenceEquals(slotBefore, HighlightedTurnSlot))
            {
                TurnSlotHighlighted?.Invoke(HighlightedTurnSlot);
            }

            if (!ReferenceEquals(currentBefore, HighlightedCurrentCard))
            {
                CurrentCardHighlighted?.Invoke(HighlightedCurrentCard);
            }

            if (!ReferenceEquals(discardBefore, HighlightedDiscardCard))
            {
                DiscardCardHighlighted?.Invoke(HighlightedDiscardCard);
            }
        }

        private static TrayCardViewModel CardOf(object model)
        {
            switch (model)
            {
                case HeroTurnSlotViewModel slot: return slot.PlayedCard;
                case HeroCurrentCardViewModel current: return current.Card;
                // Milestone 8.11b: a discarded card is drawn as its own icon and is its own revealable
                // model, so it is already the card rather than something holding one.
                case TrayCardViewModel card: return card;
                default: return null;
            }
        }

        /// <summary>
        /// Puts the revealed card beside the rail it belongs to and inside the region the rails occupy:
        /// out from the left rail, in from the right one, and never off the top or bottom.
        /// </summary>
        private void PlacePreview(object model)
        {
            if (!revealAnchors.TryGetValue(model, out var element))
            {
                return;
            }

            var face = preview.Children().SingleOrDefault();
            var anchor = element.worldBound;
            var height = Root.resolvedStyle.height;
            var width = Root.resolvedStyle.width;
            var faceWidth = face == null
                ? CardFootprint.Width
                : face.style.width.value.value;
            var faceHeight = face == null
                ? CardFootprint.Height
                : face.style.height.value.value;
            var onLeft = anchor.center.x < width / 2f;
            var left = onLeft
                ? anchor.xMax + 10f
                : anchor.xMin - faceWidth - 10f;
            preview.style.left = Mathf.Max(0f, left);
            preview.style.top = Mathf.Max(
                0f,
                Mathf.Min(
                    anchor.yMin - 40f,
                    Mathf.Max(0f, height - faceHeight - 8f)));
        }

        public void Layout(Rect red, Rect blue, Rect playfield)
        {
            Root.style.left = 0f;
            Root.style.top = 0f;
            Root.style.width = Mathf.Max(playfield.width + playfield.x, Mathf.Max(red.xMax, blue.xMax));
            // Milestone 8.25f2: the playfield and both rail columns end at the reserved card band. The
            // container follows the furthest of those declared edges so a reveal is clamped to the same
            // non-overlapping space.
            Root.style.height = Mathf.Max(playfield.height + playfield.y, Mathf.Max(red.yMax, blue.yMax));
            Place(redColumn, red);
            Place(blueColumn, blue);
        }

        private static void Place(VisualElement element, Rect rect)
        {
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
        }

        public void Render(HeroOverviewsViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            // A revealed card belongs to the model it was read from, exactly as milestone 6.5c's board
            // cues do: a rebuilt model drops it, so the rail never keeps a face for a slot that is no
            // longer drawn.
            SetHighlight(null);
            revealAnchors.Clear();
            redColumn.Clear();
            blueColumn.Clear();
            foreach (var hero in Model.Heroes)
            {
                var column = hero.TeamSide == TeamSide.Red ? redColumn : blueColumn;
                column.Add(BuildOverview(hero));
            }

            redColumn.style.display = redColumn.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            blueColumn.style.display = blueColumn.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static VisualElement Column(string name)
        {
            var column = new VisualElement { name = name };
            column.style.position = Position.Absolute;
            column.style.flexDirection = FlexDirection.Column;
            return column;
        }

        private VisualElement BuildOverview(HeroOverviewViewModel hero)
        {
            var rail = HudElements.Panel(MatchHudTheme.RailBackground);
            rail.name = "gd-hero-rail";
            rail.style.marginBottom = 8f;
            HudElements.Padding(rail, 9f);
            rail.style.paddingTop = 8f;
            rail.style.paddingBottom = 8f;
            var accent = MatchHudTheme.TeamFace(hero.TeamSide);
            var frame = Color.Lerp(MatchHudTheme.PanelBorder, accent, 0.34f);
            var body = Color.Lerp(MatchHudTheme.PanelBackground, accent, 0.055f);
            body.a = 0.97f;
            rail.style.backgroundColor = body;
            rail.style.overflow = Overflow.Hidden;
            rail.style.flexShrink = 0f;
            HudElements.Border(rail, frame, 1f, 12f);
            if (hero.TeamSide == TeamSide.Red)
            {
                rail.style.borderLeftWidth = 5f;
                rail.style.borderLeftColor = accent;
            }
            else
            {
                rail.style.borderRightWidth = 5f;
                rail.style.borderRightColor = accent;
            }

            // Milestone 8.29b. Selection is a colour change inside permanently reserved geometry.
            // Adding this width only to the viewed plaque made Yoga renegotiate that plaque's height
            // and shifted the plaques below it whenever the viewed hero changed.
            rail.style.borderTopWidth = 2f;
            rail.style.borderTopColor = hero.IsViewed
                ? MatchHudTheme.SelectedOutline
                : new Color(0f, 0f, 0f, 0f);

            // Milestone 8.7a, by the user's 2026-08-21 decision: the rail on the right edge of the
            // screen is the one on the left mirrored, rather than the same rail repeated. Everything in
            // it reads outward from the board - the hero's identity, the item strip, the caption and
            // the discard pile all sit against the screen edge, and the current-card marker sits at the
            // rail's inner corner - so the two rails frame the board instead of both leaning the same
            // way. The turn slots are the one row that is not mirrored: they fill the rail's width
            // edge to edge, and 1-2-3-4 is a sequence rather than a decoration.
            var mirrored = hero.TeamSide != TeamSide.Red;
            var outward = mirrored ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

            // The header: who this hero is on the outer side, and the card they have in play on the
            // inner one. `LVL n` moves above the name rather than standing beside it, because the space
            // it used to take at the row's edge is the space the marker now occupies.
            var head = HudElements.Row(0f);
            head.name = "gd-hero-identity-plate";
            head.style.flexWrap = Wrap.NoWrap;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            head.style.minWidth = 0f;
            head.style.paddingLeft = 5f;
            head.style.paddingRight = 5f;
            head.style.paddingTop = 0f;
            head.style.paddingBottom = 0f;
            var identityFace = Color.Lerp(MatchHudTheme.RailBackground, accent, 0.18f);
            identityFace.a = 0.98f;
            head.style.backgroundColor = identityFace;
            HudElements.Border(head, frame, 1f, 9f);
            head.style.borderTopWidth = 2f;
            head.style.borderTopColor = accent;
            var icon = BuildHeroPortrait(hero, accent);
            var identity = HudElements.Column(0f);
            identity.style.flexShrink = 1f;
            identity.style.flexGrow = 1f;
            identity.style.minWidth = 0f;
            identity.style.overflow = Overflow.Hidden;
            identity.style.alignItems = mirrored ? Align.FlexEnd : Align.FlexStart;
            var level = HudElements.Text(hero.LevelLabel, 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            level.name = "gd-hero-level";
            level.style.unityTextAlign = outward;
            level.style.letterSpacing = 0.8f;
            identity.Add(level);
            var displayName = HudElements.Text(
                hero.HeroDisplayName, 16f, MatchHudTheme.PrimaryText, FontStyle.Bold);
            displayName.name = "gd-hero-name";
            displayName.style.unityTextAlign = outward;
            displayName.style.flexShrink = 1f;
            displayName.style.marginTop = 1f;
            identity.Add(displayName);
            var who = HudElements.Row(0f);
            who.style.flexWrap = Wrap.NoWrap;
            who.style.alignItems = Align.Center;
            who.style.flexShrink = 1f;
            who.style.flexGrow = 1f;
            who.style.minWidth = 0f;
            if (mirrored)
            {
                identity.style.marginRight = 7f;
                who.Add(identity);
                who.Add(icon);
                head.Add(BuildCurrentCard(hero, true));
                head.Add(who);
            }
            else
            {
                identity.style.marginLeft = 7f;
                who.Add(icon);
                who.Add(identity);
                head.Add(who);
                head.Add(BuildCurrentCard(hero, false));
            }

            rail.Add(head);

            // The strip of standing numbers this hero carries into every turn: p.16's item bonuses on
            // the outer end and p.15's coin balance on the inner one.
            //
            // Milestone 8.23a moved the balance here, by the user's 2026-08-26 decision, and the row
            // it came from is gone entirely. Milestone 8.21a had put it on the `RESOLVED TURNS`
            // caption's line because that was the one line in the rail with provably unused space -
            // a sound argument for an addition that could not cost height, and the wrong shape once
            // the rail itself is being made shorter. Deleting the caption gives back a whole line
            // instead of sharing one, and the balance joins the strip that was already the rail's
            // home for "a number that is true of this hero right now".
            var bonuses = HudElements.Row(0f);
            bonuses.name = "gd-bonus-strip";
            bonuses.style.marginTop = 7f;
            bonuses.style.flexWrap = Wrap.NoWrap;
            bonuses.style.alignItems = Align.Center;
            bonuses.style.justifyContent = Justify.SpaceBetween;
            bonuses.style.paddingLeft = 5f;
            bonuses.style.paddingRight = 5f;
            bonuses.style.backgroundColor = new Color(0.115f, 0.132f, 0.145f, 0.96f);
            HudElements.Border(bonuses, new Color(frame.r, frame.g, frame.b, 0.72f), 1f, 7f);

            var itemChips = HudElements.Row(6f);
            itemChips.name = "gd-item-chips";
            itemChips.style.flexWrap = Wrap.Wrap;
            itemChips.style.alignItems = Align.Center;
            itemChips.style.justifyContent = mirrored ? Justify.FlexEnd : Justify.FlexStart;
            foreach (var bonus in hero.ItemBonuses)
            {
                itemChips.Add(BuildPrintedItemBonus(bonus));
            }

            if (hero.ItemBonuses.Count == 0)
            {
                itemChips.Add(HudElements.Text("NO ITEMS", 10f, MatchHudTheme.MutedText));
            }

            var coins = BuildCoinDisc(hero);
            if (mirrored)
            {
                bonuses.Add(coins);
                bonuses.Add(itemChips);
            }
            else
            {
                bonuses.Add(itemChips);
                bonuses.Add(coins);
            }

            rail.Add(bonuses);

            // Milestone 8.23a: rulebook p.18's markers this hero is holding, and what each does to
            // them - the debt milestone 8.23 recorded on its own gate row, where Poisoned Dagger and
            // Poisoned Dart lowered three of a hero's values with nothing on screen saying so.
            //
            // It sits directly under the item strip because the two are the same kind of fact about
            // this hero and are drawn in the same chip: a named component shifting named channels by
            // named amounts. p.16's item raises and p.18's marker lowers, and the sign is on the chip.
            // Putting them anywhere else in the rail would have made a player learn two readings of
            // one thing.
            //
            // Drawn only while something is held, on the terms the Ultimate row below is: p.8 returns
            // every marker at the end of every round and p.13 returns a defeated holder's, so an
            // always-present row would be an empty caption for almost all of a match - and the rail
            // this milestone is compacting has no height to spend on one.
            foreach (var marker in hero.HeldMarkers)
            {
                var held = HudElements.Row(6f);
                held.name = "gd-hero-marker";
                held.style.marginTop = 5f;
                held.style.flexWrap = Wrap.NoWrap;
                held.style.alignItems = Align.Center;
                held.style.justifyContent = mirrored ? Justify.FlexEnd : Justify.FlexStart;
                held.tooltip = marker.AssistiveLabel;

                var markerName = HudElements.Text(
                    marker.NameLabel, 9f, MatchHudTheme.MarkerInk, FontStyle.Bold);
                markerName.name = "gd-hero-marker-name";
                markerName.style.whiteSpace = WhiteSpace.NoWrap;
                markerName.style.marginTop = 0f;
                markerName.style.marginBottom = 0f;

                var changes = HudElements.Row(5f);
                changes.name = "gd-hero-marker-values";
                changes.style.flexWrap = Wrap.NoWrap;
                changes.style.alignItems = Align.Center;
                foreach (var change in marker.ValueChanges)
                {
                    changes.Add(BuildValueChip(change.Token, change.AmountLabel, change.AssistiveLabel));
                }

                // Mirrored with the rest of the rail: the marker's name keeps the screen edge and its
                // numbers read inward, the way the Ultimate caption and the identity block do.
                if (mirrored)
                {
                    held.Add(changes);
                    held.Add(markerName);
                }
                else
                {
                    held.Add(markerName);
                    held.Add(changes);
                }

                rail.Add(held);
            }

            // Milestone 8.15: the unlocked Ultimate, faceup beside the dashboard on the table and
            // therefore on the rail here - the one card a hero owns that is neither in the round the
            // tray draws nor in the pile below. It is the same mini icon every other rail card is,
            // revealing the same full face on a pointer rest, and it is only drawn while a card
            // actually stands in rulebook p.16's area, so most of a match has no row here at all.
            if (hero.UltimateCard != null)
            {
                var ultimate = HudElements.Row(4f);
                ultimate.name = "gd-ultimate";
                ultimate.style.marginTop = 6f;
                ultimate.style.flexWrap = Wrap.NoWrap;
                ultimate.style.alignItems = Align.Center;
                ultimate.style.justifyContent = mirrored ? Justify.FlexEnd : Justify.FlexStart;
                var ultimateCaption = HudElements.Text(
                    hero.UltimateLabel, 9f, MatchHudTheme.MutedText, FontStyle.Bold);
                var ultimateIcon = BuildMiniCard(
                    "gd-ultimate-card",
                    hero.UltimateCard,
                    hero.UltimateCard.AssistiveLabel,
                    hero.UltimateCard);
                if (mirrored)
                {
                    ultimate.Add(ultimateIcon);
                    ultimate.Add(ultimateCaption);
                }
                else
                {
                    ultimate.Add(ultimateCaption);
                    ultimate.Add(ultimateIcon);
                }

                rail.Add(ultimate);
            }

            // The four resolved-turn slots, which milestone 8.23a draws with no caption over them.
            //
            // Milestone 8.7a had added `RESOLVED TURNS` for a real reason - without it the row is four
            // card icons whose meaning a player has to infer - and the user's 2026-08-26 decision
            // takes the line back for the rail's height, on the terms this HUD has settled twice
            // before. The team label came off the header at 8.7a and lives on the hero disc's own
            // reading; a card's printed colour has been named in its hover since 8.5a. This is the
            // same move: every slot's tooltip and assistive label already read `Turn 1: Blue card -
            // Static Barrier` or `Turn 3: empty`, built from the slot's own TurnIndex, so what the
            // row means is one pointer rest away and is spoken in full to a reader with no pointer.
            // Nothing here means a position by its position alone.
            var slots = HudElements.Row(0f);
            slots.name = "gd-turn-slots";
            slots.style.marginTop = 7f;
            slots.style.flexWrap = Wrap.NoWrap;
            slots.style.alignItems = Align.Stretch;
            slots.style.paddingLeft = 4f;
            slots.style.paddingRight = 4f;
            slots.style.backgroundColor = new Color(0.065f, 0.076f, 0.085f, 0.92f);
            HudElements.Border(slots, new Color(frame.r, frame.g, frame.b, 0.58f), 1f, 7f);
            var first = true;
            foreach (var slot in hero.TurnSlots)
            {
                var column = new VisualElement();
                column.style.flexDirection = FlexDirection.Column;
                column.style.alignItems = Align.Stretch;
                column.style.flexGrow = 1f;
                column.style.flexBasis = 0f;
                column.style.marginLeft = first ? 0f : 4f;
                first = false;

                // Milestone 8.7a: milestone 8.5b's outline, on the slot the resolved card occupies. It
                // is the same treatment the tray gives the same card for the same reason - drawn around
                // the swatch rather than on it, and always drawn so nothing moves when an effect starts
                // or ends - so one card wears one mark wherever it is shown.
                var outline = new VisualElement { name = "gd-turn-slot-outline" };
                HudElements.Padding(outline, 2f);
                HudElements.Border(
                    outline,
                    slot.IsActiveEffectSource
                        ? MatchHudTheme.ActiveEffectAccent
                        : new Color(0f, 0f, 0f, 0f),
                    DirectorHandsUnityView.ActiveOutlineWidth,
                    7f);

                // Milestone 8.11b: the card that resolved on this turn, as its own icon, or an empty
                // card outline for a turn with none. The slot is named the same and sits in the same
                // fixed position, so the row still reads left to right as turns one to four.
                var element = slot.IsOccupied
                    ? BuildMiniCard("gd-turn-slot", slot.PlayedCard, slot.AssistiveLabel, slot)
                    : BuildEmptyMiniCard("gd-turn-slot", slot.AssistiveLabel);

                outline.Add(element);
                column.Add(outline);

                // Always drawn, empty when nothing is live, so a slot's height does not change with an
                // effect and the four of them stay on one line.
                var activeLabel = HudElements.Text(
                    slot.ActiveLabel, 7f, MatchHudTheme.ActiveEffectAccent, FontStyle.Bold);
                activeLabel.name = "gd-turn-slot-active";
                activeLabel.style.height = 10f;
                activeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                activeLabel.style.whiteSpace = WhiteSpace.NoWrap;
                column.Add(activeLabel);
                slots.Add(column);
            }

            rail.Add(slots);

            // Milestone 8.11b: the pile itself rather than a description of it.
            //
            // It carried a `DISCARD` label, a strip of one pip per colour present, and a count. A pip
            // strip cannot say that two of a colour are in there, and a count cannot say what they are,
            // so a player who wanted to know what an opponent had spent could read neither off the
            // rail. One icon per card says both, in the same icon a resolved turn is drawn as, and the
            // count goes because the icons are the count.
            var discard = HudElements.Column(4f);
            discard.name = "gd-discard";
            discard.style.marginTop = 6f;
            HudElements.Padding(discard, 5f);
            discard.style.backgroundColor = new Color(0.115f, 0.132f, 0.145f, 0.96f);
            HudElements.Border(discard, new Color(frame.r, frame.g, frame.b, 0.72f), 1f, 7f);

            // Milestone 8.23a put the caption on the pile's own line, by the user's 2026-08-26
            // decision, and it is the idiom the Ultimate row above already uses: a short fixed word
            // beside the cards it names, rather than stacked above them. The caption is a literal at a
            // fixed size and the pile is 34 tall, so the row is exactly as tall as the pile was and
            // the caption's whole line is what the rail gets back.
            //
            // The pile keeps the one exception the mirror does not touch, by the user's 2026-08-21
            // decision: it reads left to right whichever rail it is on, so two piles are compared side
            // by side rather than being each other's reflection. Only the caption takes a side.
            discard.style.flexDirection = FlexDirection.Row;
            discard.style.alignItems = Align.Center;
            var discardCaption = HudElements.Text(
                hero.DiscardLabel, 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            discardCaption.name = "gd-discard-caption";
            discardCaption.style.whiteSpace = WhiteSpace.NoWrap;
            discardCaption.style.flexShrink = 0f;
            var pile = HudElements.Row(MiniCardGap);
            pile.name = "gd-discard-pile";
            pile.style.flexWrap = Wrap.Wrap;
            pile.style.flexGrow = 1f;
            pile.style.flexShrink = 1f;
            foreach (var card in hero.DiscardCards)
            {
                pile.Add(BuildMiniCard("gd-discard-card", card, card.AssistiveLabel, card));
            }

            if (hero.DiscardCards.Count == 0)
            {
                // An empty pile says so in words rather than with a nought, for the reason the count
                // went: nothing here is a number any more.
                pile.Add(HudElements.Text("EMPTY", 10f, MatchHudTheme.MutedText));
            }

            if (mirrored)
            {
                discardCaption.style.marginLeft = 6f;
                discard.Add(pile);
                discard.Add(discardCaption);
            }
            else
            {
                discardCaption.style.marginRight = 6f;
                discard.Add(discardCaption);
                discard.Add(pile);
            }
            rail.Add(discard);
            return rail;
        }

        /// <summary>
        /// One item bonus: the established printed symbol and signed projected amount, optically centred
        /// as one compact reading. Its tooltip keeps the full channel name in words.
        /// </summary>
        private static VisualElement BuildPrintedItemBonus(HeroItemBonusViewModel bonus)
        {
            var chip = HudElements.Row(2f);
            chip.name = "gd-value-chip";
            chip.style.flexWrap = Wrap.NoWrap;
            chip.style.alignItems = Align.Center;
            chip.tooltip = bonus.AssistiveLabel;

            var symbol = HudElements.PrintedChannelSymbol(bonus.Channel, 15f);
            symbol.style.marginTop = 0f;
            symbol.style.marginBottom = 0f;
            var amount = HudElements.Text(
                bonus.AmountLabel, 11f, MatchHudTheme.PrimaryText, FontStyle.Bold);
            amount.style.whiteSpace = WhiteSpace.NoWrap;
            amount.style.marginLeft = 2f;
            amount.style.marginTop = 0f;
            amount.style.marginBottom = 0f;
            chip.Add(symbol);
            chip.Add(amount);
            return chip;
        }

        /// <summary>
        /// One marker channel-and-amount chip: the three-letter token in muted ink and the signed amount
        /// in primary ink. Milestone 8.30k changes item bonuses only; p.18 marker readings retain the
        /// established textual chip rather than silently expanding the requested surface.
        ///
        /// <para>Milestone 8.23a pulled it out of the item strip so p.18's marker draws the identical
        /// chip rather than a lookalike, and closed the gap between the token and its number from 3 to
        /// 1 by the user's 2026-08-26 decision. At 3 the two halves read as two words; the amount
        /// belongs to the token beside it, and the gap between chips - which stayed at 6 - is what
        /// separates one pair from the next.</para>
        /// </summary>
        private static VisualElement BuildValueChip(string token, string amountLabel, string assistiveLabel)
        {
            var chip = HudElements.Row(1f);
            chip.name = "gd-value-chip";
            chip.style.flexWrap = Wrap.NoWrap;
            chip.style.alignItems = Align.Center;
            chip.tooltip = assistiveLabel;
            var tokenLabel = HudElements.Text(token, 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            tokenLabel.style.whiteSpace = WhiteSpace.NoWrap;
            tokenLabel.style.marginTop = 0f;
            tokenLabel.style.marginBottom = 0f;
            var amount = HudElements.Text(amountLabel, 11f, MatchHudTheme.PrimaryText, FontStyle.Bold);
            amount.style.whiteSpace = WhiteSpace.NoWrap;
            amount.style.marginTop = 0f;
            amount.style.marginBottom = 0f;
            chip.Add(tokenLabel);
            chip.Add(amount);
            return chip;
        }

        /// <summary>The coin disc's diameter, which is what keeps it inside the item strip's line.</summary>
        private const float CoinDiscSize = 24f;

        /// <summary>
        /// p.15's balance as the coin it counts: a struck disc with the number on its face.
        ///
        /// <para>Milestone 8.23a, by the user's 2026-08-26 decision. `COINS 0` was a caption
        /// explaining a number, and a coin with the number on it is the same fact as one object -
        /// which is the argument milestone 8.11b already made against the discard row's count and the
        /// turn slots' ordinals, applied to the last number in the rail that still had a word in front
        /// of it.</para>
        ///
        /// <para>It is composed rather than imported: a filled circle, a rim a shade darker, and a
        /// centred label, from the same primitives the hero medallion in the header is built
        /// from. That is what keeps it inside this milestone's own non-goal against imported art, and
        /// what keeps it looking like this HUD on every platform the project declares. The word
        /// `COINS` is not lost - it is in the tooltip and the assistive reading as `3 coins`, which is
        /// where this HUD names everything it draws as a shape.</para>
        /// </summary>
        /// <summary>Milestone 8.29a's readable official hero face inside its team medallion.</summary>
        private const float HeroPortraitSize = 50f;

        private const float HeroMedallionSize = 56f;

        /// <summary>
        /// The rail's hero disc: this hero's own official token face, in a disc rimmed in their team's
        /// accent.
        ///
        /// <para><b>Milestone 8.25c.</b> Until this milestone the disc carried the first letter of the
        /// hero's display name - the placeholder milestone 7.8a's decision left a slot for. Six
        /// configured heroes tell each other apart about as well as their initials happen to differ,
        /// and the fix was already in the project: the same private token face the piece on the board
        /// is drawn from, addressed the same way. So the rail and the map now show one hero rather than
        /// a picture and an initial that have to be matched up.</para>
        ///
        /// <para><b>The face is selected, never read.</b>
        /// <see cref="HeroOverviewViewModel.PortraitResourcePath"/> is built from the projected content
        /// identity and the authoritative team; nothing here asks the display name, and nothing asks a
        /// pixel. No rule and no control ownership is decided by the image - this is a background on a
        /// disc that answers no pointer.</para>
        ///
        /// <para><b>Milestone 8.29a makes the face the anchor.</b> The official token face grows to 50
        /// reference units inside a 56-unit team medallion. The reserved column already has the
        /// measured vertical headroom for three such plaques; the live six-rail gate verifies that the
        /// larger identity does not escape that budget.</para>
        ///
        /// <para><b>There is no letter fallback.</b> The private artwork is ignored by Git and a clean
        /// checkout resolves none, exactly as the board's own face does; the disc is then the team
        /// accent with nothing written in it, and the hero's name beside it - which never left - is
        /// what carries the identity. Falling back to the initial would keep the surface this milestone
        /// exists to replace alive in the tracked project.</para>
        /// </summary>
        private static VisualElement BuildHeroPortrait(HeroOverviewViewModel hero, Color accent)
        {
            var medallion = new VisualElement { name = "gd-hero-medallion" };
            medallion.style.width = HeroMedallionSize;
            medallion.style.height = HeroMedallionSize;
            medallion.style.flexShrink = 0f;
            medallion.style.alignItems = Align.Center;
            medallion.style.justifyContent = Justify.Center;
            medallion.style.backgroundColor = Color.Lerp(MatchHudTheme.PanelBackground, accent, 0.30f);
            HudElements.Border(medallion, accent, 2f, HeroMedallionSize / 2f);

            var icon = new VisualElement { name = "gd-hero-icon" };
            icon.style.width = HeroPortraitSize;
            icon.style.height = HeroPortraitSize;
            icon.style.flexShrink = 0f;
            icon.style.justifyContent = Justify.Center;
            icon.style.backgroundColor = accent;
            icon.style.overflow = Overflow.Hidden;
            HudElements.Border(
                icon,
                new Color(
                    MatchHudTheme.PrimaryText.r,
                    MatchHudTheme.PrimaryText.g,
                    MatchHudTheme.PrimaryText.b,
                    0.72f),
                1f,
                HeroPortraitSize / 2f);

            if (!string.IsNullOrEmpty(hero.PortraitResourcePath))
            {
                var portrait = Resources.Load<Texture2D>(hero.PortraitResourcePath);
                if (portrait != null)
                {
                    // Contained rather than covered, and centred: the source is a circular token
                    // inscribed in a square, so containing it fills the disc exactly while a cover
                    // would crop a face that is already as wide as it is tall. Aspect is the
                    // background's own and is never set from the element's.
                    icon.style.backgroundImage = new StyleBackground(portrait);
                    icon.style.backgroundSize =
                        new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
                    icon.style.backgroundPositionX =
                        new StyleBackgroundPosition(
                            new BackgroundPosition(BackgroundPositionKeyword.Center));
                    icon.style.backgroundPositionY =
                        new StyleBackgroundPosition(
                            new BackgroundPosition(BackgroundPositionKeyword.Center));
                }
            }

            // Who and which side, in words. Milestone 8.7a took the team off the rail's face by the
            // user's 2026-08-21 decision and left it on this disc's own hover and assistive reading;
            // milestone 8.25c adds the hero's name to it for the same reason it is on the tooltip at
            // all - the disc's whole statement is now a picture, and a picture states nothing to a
            // reader who cannot use one.
            icon.tooltip = hero.PortraitAssistiveLabel;
            medallion.tooltip = hero.PortraitAssistiveLabel;
            medallion.Add(icon);
            return medallion;
        }

        private static VisualElement BuildCoinDisc(HeroOverviewViewModel hero)
        {
            var coins = HudElements.Row(0f);
            coins.name = "gd-hero-coins";
            coins.style.flexWrap = Wrap.NoWrap;
            coins.style.alignItems = Align.Center;
            coins.style.flexShrink = 0f;
            // The words for the same fact, on the terms every other compact rail reading is named by:
            // the pointer and an assistive reader get `3 coins`.
            coins.tooltip = hero.CoinAssistiveLabel;

            var coinDisc = new VisualElement { name = "gd-hero-coin-disc" };
            coinDisc.style.width = CoinDiscSize;
            coinDisc.style.height = CoinDiscSize;
            coinDisc.style.flexShrink = 0f;
            coinDisc.style.justifyContent = Justify.Center;
            coinDisc.style.backgroundColor = MatchHudTheme.CoinFace;
            HudElements.Border(coinDisc, MatchHudTheme.CoinEdge, 1f, CoinDiscSize / 2f);
            var coinAmount = HudElements.Text(
                hero.CoinAmountLabel, 13f, MatchHudTheme.CoinInk, FontStyle.Bold);
            coinAmount.name = "gd-hero-coins-amount";
            coinAmount.style.whiteSpace = WhiteSpace.NoWrap;
            coinAmount.style.unityTextAlign = TextAnchor.MiddleCenter;
            coinAmount.style.marginTop = 0f;
            coinAmount.style.marginBottom = 0f;
            coinAmount.style.marginLeft = 0f;
            coinAmount.style.marginRight = 0f;
            coinDisc.Add(coinAmount);
            coins.Add(coinDisc);
            return coins;
        }

        /// <summary>The mini card's footprint, and the gap between two of them.</summary>
        private const float MiniCardWidth = 24f;

        private const float MiniCardHeight = 34f;

        private const float MiniCardGap = 4f;

        /// <summary>
        /// One card as a compact card-shaped icon: the shape and the printed colour, and nothing
        /// abbreviated onto it.
        ///
        /// <para>Milestone 8.11b. It is deliberately not a small card face - at this size a name band
        /// and a value line would be unreadable smudges, and a smudge that looks like text is worse
        /// than none. What it carries is what reads at 24 pixels: a card's proportions, its printed
        /// colour, and a hairline band at the top so it is a card rather than a swatch. Everything else
        /// about the card is one pointer rest away, at full size, through the same preview the tray's
        /// own card faces are drawn by - and is in <paramref name="assistiveLabel"/> for a reader that
        /// has no pointer.</para>
        ///
        /// <para>Like the slots before it, an icon takes no focus stop. The rail is informational, and
        /// a hero's whole discard pile as tab stops in front of the tray would be a keyboard-order
        /// change this milestone has no reason to make.</para>
        /// </summary>
        private VisualElement BuildMiniCard(
            string name, TrayCardViewModel card, string assistiveLabel, object revealed)
        {
            var element = BuildMiniCardShape(name);
            element.style.backgroundColor = MatchHudTheme.Face(card.Color);
            HudElements.Border(element, MatchHudTheme.PanelBorder, 1f, 3f);
            ConfiguredCardFaceView.AddExteriorOutline(element, 1.5f, 4.5f);

            var band = new VisualElement { name = "gd-mini-card-band" };
            band.style.height = 4f;
            band.style.marginLeft = 3f;
            band.style.marginRight = 3f;
            band.style.marginTop = 3f;
            var ink = MatchHudTheme.FaceText(card.Color);
            band.style.backgroundColor = new Color(ink.r, ink.g, ink.b, 0.30f);
            element.Add(band);

            element.tooltip = assistiveLabel;
            revealAnchors[revealed] = element;
            HudElements.OnPointedAt(element, entered => SetHighlight(entered ? revealed : null));
            return element;
        }

        /// <summary>
        /// A reserved position with no card in it: the same footprint, drawn as an outline.
        ///
        /// It reveals nothing and answers no pointer, because there is nothing to reveal - an empty
        /// resolved-turn slot is a turn this hero has not finished, and inventing a card or a number
        /// for it would be the presentation saying something the round has not.
        /// </summary>
        private static VisualElement BuildEmptyMiniCard(string name, string assistiveLabel)
        {
            var element = BuildMiniCardShape(name);
            element.style.backgroundColor = MatchHudTheme.EmptySlot;
            HudElements.Border(element, MatchHudTheme.MutedText, 1f, 3f);
            element.tooltip = assistiveLabel;
            return element;
        }

        private static VisualElement BuildMiniCardShape(string name)
        {
            var element = new VisualElement { name = name };
            element.style.width = MiniCardWidth;
            element.style.height = MiniCardHeight;
            element.style.flexShrink = 0f;
            element.style.flexDirection = FlexDirection.Column;
            element.style.alignSelf = Align.Center;
            return element;
        }

        /// <summary>The marker's fixed footprint in the rail header's upper right.</summary>
        private const float CurrentCardWidth = 46f;

        private const float CurrentCardHeight = 52f;

        /// <summary>
        /// The marker in the hero header's inner top corner: the card this hero has in play and has not
        /// resolved. Upper right on the left-hand rail, upper left on the mirrored one.
        ///
        /// Milestone 8.7a. The slot around it is always drawn at a fixed size and the marker itself only
        /// appears inside it, for the reason milestone 8.5b's Active outline is always drawn: a header
        /// that grew and shrank as a turn was committed and resolved would move the hero's name, the
        /// item strip and the four slots under it four times a round. Nothing here reads a card
        /// location, a definition or a hand - the whole marker is built from
        /// <see cref="HeroCurrentCardViewModel"/>, and a facedown one carries no card to build from.
        /// </summary>
        private VisualElement BuildCurrentCard(HeroOverviewViewModel hero, bool mirrored)
        {
            var slot = new VisualElement { name = "gd-current-card-slot" };
            slot.style.width = CurrentCardWidth;
            slot.style.height = CurrentCardHeight;
            slot.style.flexShrink = 0f;
            if (mirrored)
            {
                slot.style.marginRight = 6f;
            }
            else
            {
                slot.style.marginLeft = 6f;
            }

            var current = hero.CurrentCard;
            if (current == null)
            {
                return slot;
            }

            var ink = current.IsFacedown
                ? MatchHudTheme.PrimaryText
                : MatchHudTheme.FaceText(current.Color);
            var marker = new VisualElement { name = "gd-current-card" };
            marker.style.flexGrow = 1f;
            marker.style.flexDirection = FlexDirection.Column;
            marker.style.alignItems = Align.Center;
            marker.style.justifyContent = Justify.Center;
            marker.style.paddingTop = 2f;
            marker.style.paddingBottom = 2f;
            marker.style.backgroundColor = current.IsFacedown
                ? MatchHudTheme.CardBack
                : MatchHudTheme.FaceFlag(current.Color);
            HudElements.Border(marker, new Color(ink.r, ink.g, ink.b, 0.45f), 2f, 6f);
            ConfiguredCardFaceView.AddExteriorOutline(marker, 1.5f, 7.5f);
            marker.tooltip = current.AssistiveLabel;

            if (current.IsFacedown)
            {
                // A back, and only a back: no name, no colour, no Initiative, no words, no hover. The
                // motif is a plain lozenge in the marker's own ink rather than anything a card prints,
                // so there is nothing on it a player could read a commitment from.
                var back = new VisualElement { name = "gd-current-card-back" };
                back.style.width = 18f;
                back.style.height = 18f;
                back.style.backgroundColor = new Color(ink.r, ink.g, ink.b, 0.16f);
                HudElements.Border(back, new Color(ink.r, ink.g, ink.b, 0.32f), 1f, 9f);
                marker.Add(back);

                // The one word the facedown marker cannot do without: a neutral back with nothing on it
                // says nothing, and this is the only place the state can be read. The revealed marker
                // does not carry its word - the user's 2026-08-21 decision - because a card's own
                // colour and Initiative in the header's inner corner already say it is the card in
                // play, and `RESOLVING` under it was a caption on something not in doubt. It stays in
                // the marker's hover and assistive reading and on the revealed face's own lifecycle
                // band, exactly where milestone 8.5a keeps a card's printed colour.
                var state = HudElements.Text(current.StateLabel, 7f, ink, FontStyle.BoldAndItalic);
                state.name = "gd-current-card-state";
                state.style.whiteSpace = WhiteSpace.NoWrap;
                state.style.unityTextAlign = TextAnchor.MiddleCenter;
                state.style.marginTop = 3f;
                marker.Add(state);
            }
            else
            {
                var initiative = HudElements.Text(
                    current.InitiativeLabel.Length == 0 ? "-" : current.InitiativeLabel,
                    20f,
                    ink,
                    FontStyle.Bold);
                // Never wrapped, for the reason the card's own flag is not: p.7 prints two-digit
                // Initiatives, and a marker breaking "13" across two lines would show a value the card
                // does not have.
                initiative.style.whiteSpace = WhiteSpace.NoWrap;
                initiative.style.unityTextAlign = TextAnchor.MiddleCenter;
                initiative.style.position = Position.Absolute;
                initiative.style.left = 0f;
                initiative.style.top = 0f;
                initiative.style.right = 0f;
                initiative.style.bottom = 0f;
                initiative.style.marginTop = 0f;
                initiative.style.marginBottom = 0f;
                initiative.style.marginLeft = 0f;
                initiative.style.marginRight = 0f;
                marker.Add(initiative);

                // The same reveal the four resolved slots offer, on the card that has not resolved yet:
                // one builder, one preview, no focus stop, and no command.
                var pointed = current;
                revealAnchors[pointed] = marker;
                HudElements.OnPointedAt(marker, entered => SetHighlight(entered ? pointed : null));
            }

            slot.Add(marker);
            return slot;
        }
    }

    /// <summary>
    /// The card tray: one hero's whole current round, colour-coded, in the order the milestone fixes -
    /// already played in ascending turn order, then discarded/unavailable, then available.
    ///
    /// Which hero it shows is the presenter's, and it is changed only from the map. This view draws no
    /// hero-selection control of its own.
    /// </summary>
    public sealed class DirectorHandsUnityView : IDirectorHandsView
    {
        // Milestone 8.20c: a card's own size is CardFootprint's, and this class no longer states one.
        // Milestone 8.5a's `CardWidth` and its `CardMinimumHeight` floor lived here, and the floor is
        // what the milestone removed - the row used to stretch every card to the tallest one's
        // *content*, so a hand's card size was decided by whichever sentence in it ran longest and
        // switching to the other hero resized every card on the screen.

        /// <summary>
        /// The Initiative flag's width. It is a fixed column so that five flags line up down the left
        /// edge of a hand and the number a player is comparing is always in the same place.
        /// </summary>
        private const float InitiativeFlagWidth = 38f;

        /// <summary>How far the Initiative banner hangs from the card's top edge into the name band.</summary>
        private const float InitiativeFlagHeight = 46f;

        /// <summary>
        /// The heights the four bands above the rules panel are never shorter than.
        ///
        /// They exist so that five cards of one hand line their bands up with each other. Without them a
        /// two-line card name pushes that card's values, action and rules panel a row lower than its
        /// neighbours', and a player comparing five Initiatives or five Defense values has to find each
        /// one at a different height. The rules panel takes whatever is left, which is what makes the
        /// bottoms line up too.
        /// </summary>
        /// <remarks>
        /// Milestone 8.7a lowered this from 70 to the banner's own height plus a little. The name used
        /// to sit on a line the band opened <em>below</em> the banner, which left the whole strip beside
        /// the banner empty on every card in the game; the approved current-card mockup puts the title
        /// in that strip, and the band is then as tall as the thing hanging in it.
        /// </remarks>
        private const float NameBandMinimumHeight = InitiativeFlagHeight + 8f;

        /// <summary>
        /// The name band's own inset. The title reserves the banner's width measured from the band's
        /// edge, so the reservation on each side is this much less than the banner itself - reserving a
        /// whole banner width *inside* the inset took another six pixels off each side of the title and
        /// was enough to break "PROJECTILES" mid-word.
        /// </summary>
        private const float NameBandHorizontalPadding = 6f;

        private const float SecondaryBandMinimumHeight = 26f;
        private const float PrimaryBandMinimumHeight = 40f;
        private const float HistoryBandMinimumHeight = 28f;

        /// <summary>How far a still-playable card sits above the ones that are spent.</summary>
        public const float AvailableCardRaise = 10f;

        /// <summary>
        /// The gutter between two cards. It is a margin on the card rather than a gap on the row,
        /// because the row's own gap helper only applies through <c>HudElements.Add</c> and a plain
        /// <c>Add</c> resolves to the instance method - so a gutter written there is a gutter that is
        /// never drawn.
        /// </summary>
        public const float CardGap = 6f;

        /// <summary>
        /// Milestone 8.5b: the Active-effect outline's width, and the gap between it and the card's own
        /// edge. Both are always drawn - the outline is transparent when nothing is active - so a card
        /// never changes size because an effect started or ended.
        /// </summary>
        public const float ActiveOutlineWidth = 2f;

        public const float ActiveOutlineGap = 3f + CardFootprint.ExteriorOutlineWidth;

        /// <summary>
        /// The configured match's resting-hand footprint. The screenshot-approved safe area is tall
        /// enough for the standard card, so the resting cards keep that full source-ratio size. Five
        /// cards fit its width by overlapping horizontally, like a physical hand; hover/focus draws
        /// the pointed card on top so every part of its face is readable.
        /// </summary>
        public const float PeekCardWidth = CardFootprint.StandardWidth;
        public const float PeekCardHeight = CardFootprint.StandardHeight;

        /// <summary>
        /// One complete tray slot including the always-reserved Active outline and the inter-card
        /// gutter. Milestone 8.25f2 uses five of these to size the full-width reserved tray band, so
        /// the complete hand stays one row without wrapping, shrinking, or sideways scrolling.
        /// </summary>
        public const float CardSlotFootprint = CardFootprint.Width + CardGap +
            (2f * (ActiveOutlineGap + ActiveOutlineWidth));

        public const float PeekCardOuterWidth = PeekCardWidth +
            (2f * (ActiveOutlineGap + ActiveOutlineWidth));

        public const float PeekCardOuterHeight = PeekCardHeight +
            (2f * (ActiveOutlineGap + ActiveOutlineWidth));

        /// <summary>The left-to-left step of two cards in the overlapped resting hand.</summary>
        public const float PeekCardSlotFootprint =
            (ConfiguredMatchHudRenderer.CompactHandRightEdge - PeekCardOuterWidth) / 4f;

        public const float PeekCardOverlap = PeekCardOuterWidth - PeekCardSlotFootprint;

        /// <summary>
        /// The exposed top strip of each unavailable card in the right-side vertical fan. Five
        /// complete faces remain individually targetable while the final face stays fully visible.
        /// </summary>
        public const float UnavailableCardSlotFootprint = 20f;

        public const float UnavailableCardOverlap =
            PeekCardOuterHeight - UnavailableCardSlotFootprint;

        private readonly VisualElement header;
        private readonly VisualElement cards;
        private readonly VisualElement unavailableGroup;
        private readonly VisualElement unavailableCards;
        private readonly VisualElement hoverPreview;
        private bool compactPeekMode;
        private TrayCardViewModel highlightedPeekCard;
        private IReadOnlyList<PrintedActionRegionViewModel> printedActionRegions =
            Array.Empty<PrintedActionRegionViewModel>();

        public DirectorHandsUnityView()
        {
            Root = new VisualElement { name = "gd-card-tray" };
            Root.style.flexDirection = FlexDirection.Column;
            header = HudElements.Row(12f);
            header.name = "gd-hand-header";
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 8f;
            cards = HudElements.Row(8f);
            // Milestone 8.20c: the row no longer sizes the cards. It used to stretch, so the tallest
            // card's content set the height of every card on the line - which is exactly the
            // content-driven sizing the standard footprint replaces.
            cards.style.alignItems = Align.FlexStart;
            cards.style.flexWrap = Wrap.NoWrap;
            cards.style.flexGrow = 0f;
            cards.style.flexShrink = 0f;
            // Room for the raise, so a still-playable card lifts into space of its own rather than into
            // the label above it.
            cards.style.marginTop = AvailableCardRaise;
            cards.name = "gd-card-buttons";
            unavailableGroup = new VisualElement { name = "gd-unavailable-card-group" };
            unavailableGroup.style.flexDirection = FlexDirection.Column;
            unavailableGroup.style.justifyContent = Justify.FlexStart;
            unavailableGroup.style.alignItems = Align.FlexStart;
            unavailableGroup.style.display = DisplayStyle.None;
            unavailableGroup.pickingMode = PickingMode.Ignore;
            unavailableCards = new VisualElement();
            unavailableCards.name = "gd-unavailable-card-buttons";
            unavailableCards.style.flexDirection = FlexDirection.Column;
            unavailableCards.style.alignItems = Align.Stretch;
            unavailableCards.style.flexWrap = Wrap.NoWrap;
            unavailableCards.style.flexGrow = 0f;
            unavailableCards.style.flexShrink = 0f;
            unavailableCards.pickingMode = PickingMode.Ignore;
            unavailableGroup.Add(unavailableCards);
            hoverPreview = new VisualElement { name = "gd-hand-hover-preview" };
            hoverPreview.style.position = Position.Absolute;
            hoverPreview.style.display = DisplayStyle.None;
            hoverPreview.pickingMode = PickingMode.Ignore;
            dragProxy = new VisualElement { name = "gd-card-drag-proxy" };
            dragProxy.style.position = Position.Absolute;
            dragProxy.style.display = DisplayStyle.None;
            dragProxy.style.width = CardFootprint.Width;
            dragProxy.style.height = CardFootprint.Height;
            dragProxy.pickingMode = PickingMode.Ignore;
            // Milestone 8.30d, the user's follow-up decision: the hand-status bar is dead. Its
            // two commands live on as corner banners instead, and the identity/hint line it
            // carried is not rebuilt - the narration and the rails already say it.
            header.style.display = DisplayStyle.None;
            cornerActions = new VisualElement { name = "gd-hand-corner-actions" };
            // The host itself must never take the pointer: it shares End Turn's corner rect,
            // and a pickable empty host swallowed every End Turn click (three journeys caught
            // it). Its banner children pick for themselves.
            cornerActions.pickingMode = PickingMode.Ignore;
            cornerActions.style.flexDirection = FlexDirection.Column;
            cornerActions.style.justifyContent = Justify.FlexEnd;
            cornerActions.style.alignItems = Align.Stretch;
            confirmAction = new VisualElement { name = "gd-hand-confirm-action" };
            confirmAction.pickingMode = PickingMode.Ignore;
            confirmAction.style.flexDirection = FlexDirection.Column;
            confirmAction.style.justifyContent = Justify.FlexStart;
            confirmAction.style.alignItems = Align.Stretch;
            confirmAction.style.display = DisplayStyle.None;
            Root.Add(header);
            Root.Add(cards);
            Root.Add(unavailableGroup);
            Root.Add(cornerActions);
            Root.Add(confirmAction);
            contextBlocker = new VisualElement { name = "gd-context-only-hand-blocker" };
            contextBlocker.style.position = Position.Absolute;
            contextBlocker.style.left = 0f;
            contextBlocker.style.top = 0f;
            contextBlocker.style.right = 0f;
            contextBlocker.style.bottom = 0f;
            contextBlocker.style.display = DisplayStyle.None;
            contextBlocker.pickingMode = PickingMode.Position;
            Root.Add(contextBlocker);
        }

        private readonly VisualElement cornerActions;
        private readonly VisualElement confirmAction;
        private readonly VisualElement contextBlocker;
        private readonly VisualElement dragProxy;
        private readonly CardCommitmentDragState dragState = new CardCommitmentDragState();
        private VisualElement dragCapture;
        private VisualElement draggedCardSlot;
        private Func<Vector2, bool> isMapDropPoint;
        private bool confirmLayoutReady;
        private bool trayHidden;
        private bool contextOnly;

        public const float CardDragThreshold = 12f;

        /// <summary>One bottom-corner Pass banner's height.</summary>
        public const float CornerBannerHeight = 44f;
        public const float CornerBannerFontSize = 16f;

        /// <summary>
        /// The hand's Pass and Confirm banners. It begins inside <see cref="Root"/> so this view
        /// remains complete in isolation; the configured HUD reparents it into the panel's
        /// bottom-right corner. End Turn now occupies the separate top-right status-bar slot.
        /// </summary>
        public VisualElement CornerActionsRoot => cornerActions;

        /// <summary>The facedown commitment confirmation, hosted below DIRECTOR by 8.30f.</summary>
        public VisualElement ConfirmActionRoot => confirmAction;

        /// <summary>
        /// Prevents the absolute confirmation host from painting at UI Toolkit's default origin for
        /// the frame before the configured HUD publishes its first real rectangle.
        /// </summary>
        public void SetConfirmLayoutReady(bool ready)
        {
            confirmLayoutReady = ready;
            confirmAction.style.display = ready && !trayHidden && !contextOnly
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        /// <summary>
        /// Keeps the ordinary hand visible as context while a separately hosted decision owns input.
        /// The cards retain their complete-face presentation, but every command path from this view
        /// is refused until the exclusive decision leaves.
        /// </summary>
        public void SetContextOnly(bool value)
        {
            if (contextOnly == value)
            {
                return;
            }

            contextOnly = value;
            CancelCardDrag();
            ClearPeekPreview();
            contextBlocker.style.display = contextOnly ? DisplayStyle.Flex : DisplayStyle.None;
            confirmAction.style.display = confirmLayoutReady && !trayHidden && !contextOnly
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            if (Model != null)
            {
                Rebuild();
            }
        }

        public bool IsContextOnly => contextOnly;

        public VisualElement Root { get; }

        /// <summary>
        /// The viewed hand's identity, commitment state and controls. It begins inside
        /// <see cref="Root"/> so this view remains complete in isolation; the configured HUD reparents
        /// it into the open status dock beside the Red rail, keeping the ordinary card band compact.
        /// </summary>
        public VisualElement HeaderRoot => header;

        /// <summary>
        /// The right-side unavailable-card group. It begins in <see cref="Root"/> so isolated view
        /// tests see one complete presentation; the configured HUD reparents and anchors it inside
        /// the reserved card band without giving the empty host pointer ownership or visible chrome.
        /// </summary>
        public VisualElement UnavailableCardsRoot => unavailableGroup;

        /// <summary>
        /// The complete face opened from a compact resting-hand card. The configured HUD reparents it
        /// to the main panel root so neither the short hand band nor its scroller can clip it, and the
        /// preview brings itself to the front without hiding the status or action controls beneath it.
        /// </summary>
        public VisualElement HoverPreviewRoot => hoverPreview;

        /// <summary>The pointer-transparent card proxy reparented to the configured panel root.</summary>
        public VisualElement DragProxyRoot => dragProxy;

        public TrayCardViewModel HighlightedPeekCard => highlightedPeekCard;

        public DirectorHandsViewModel Model { get; private set; }

        public event Action<CardInstanceId> CardSelected;
        public event Action<CardInstanceId> CardDroppedOnMap;
        public event Action ConfirmRequested;
        public event Action<HeroId> PassRequested;
        public event Action<TypedInvocationViewModel> PrintedActionSubmitted;
        public event Action<FastTravelOfferViewModel> FastTravelSelected;
        public event Action<ClearOfferViewModel> ClearSelected;

        public IReadOnlyList<PrintedActionRegionViewModel> PrintedActionRegions => printedActionRegions;

        /// <summary>Supplies the HUD-owned L-shaped live map boundary for drag release.</summary>
        public void SetMapDropPredicate(Func<Vector2, bool> predicate)
        {
            isMapDropPoint = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <summary>
        /// Binds the ordinary action projection to this hand without teaching the hand legality. The
        /// resolving card filters only by its typed instance identity; every command still follows the
        /// round presenter's existing validation and session path.
        /// </summary>
        public void SetPrintedActionRegions(IEnumerable<PrintedActionRegionViewModel> regions)
        {
            var next = (regions ?? Enumerable.Empty<PrintedActionRegionViewModel>()).ToArray();
            if (printedActionRegions.SequenceEqual(next))
            {
                return;
            }

            printedActionRegions = Array.AsReadOnly(next);
            if (Model != null)
            {
                Rebuild();
            }
        }

        public bool ForwardPrintedActionRegion(PrintedActionRegionViewModel region)
        {
            if (contextOnly || region?.Invocation == null || !printedActionRegions.Any(value =>
                    ReferenceEquals(value, region) &&
                    value.SourceCardInstanceId.Equals(region.SourceCardInstanceId) &&
                    value.ActionDefinitionId.Equals(region.ActionDefinitionId)))
            {
                return false;
            }

            PrintedActionSubmitted?.Invoke(region.Invocation);
            return true;
        }

        /// <summary>
        /// Overlaps five full source-ratio faces into the configured hand's compact horizontal
        /// footprint. Isolated/shared view users retain the standard side-by-side row.
        /// </summary>
        public void SetCompactPeekMode(bool enabled)
        {
            if (compactPeekMode == enabled)
            {
                return;
            }

            compactPeekMode = enabled;
            ClearPeekPreview();
            if (Model != null)
            {
                Rebuild();
            }
        }

        public void Render(DirectorHandsViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            Rebuild();
        }

        /// <summary>
        /// Hides the tray while a mandatory decision stands, so exactly one interaction context is
        /// usable. The tray is not merely inert: a hand a card cannot be committed from is not a choice
        /// with nothing selected, it is not the choice being asked.
        /// </summary>
        public void SetHidden(bool hidden)
        {
            trayHidden = hidden;
            if (hidden)
            {
                ClearPeekPreview();
                CancelCardDrag();
            }

            var display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
            Root.style.display = display;
            unavailableGroup.style.display = hidden || unavailableCards.childCount == 0
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            // The header stays dead either way (the user's follow-up decision retired the
            // hand-status bar); the corner banners are the surface exclusivity must hide.
            HeaderRoot.style.display = DisplayStyle.None;
            cornerActions.style.display = display;
            confirmAction.style.display = confirmLayoutReady && !hidden && !contextOnly
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void Rebuild()
        {
            CancelCardDrag();
            ClearPeekPreview();
            header.Clear();
            cards.Clear();
            unavailableCards.Clear();
            unavailableGroup.style.display = DisplayStyle.None;
            var hand = Model.ViewedHand;
            if (hand == null)
            {
                return;
            }

            // In the overlapped configured hand, the final card is the only one whose complete face
            // is visible at rest. The card currently being resolved belongs there: putting it first
            // buried the action the turn is actually performing under all four siblings. `OrderBy` is
            // stable, so moving that one state to the end does not disturb any other card's order.
            var drawnCards = compactPeekMode
                ? hand.PlayableTrayCards.OrderBy(card => card.IsResolving ? 1 : 0).ToArray()
                : hand.PlayableTrayCards.ToArray();
            var rowWidth = compactPeekMode
                ? CompactCardRowWidth(drawnCards.Length)
                : drawnCards.Length * CardSlotFootprint;
            cards.style.width = rowWidth;
            cards.style.minWidth = rowWidth;
            cards.style.maxWidth = rowWidth;

            // Milestone 8.30d, the user's follow-up decision: no hand-status bar. Pass and
            // Pass stands in the panel's bottom-right corner as a pewter banner, while commitment
            // Confirm has its dedicated lane below DIRECTOR and End Turn has the status-bar slot.
            // The shared clothes retain one banner language for the button that moves the game on.
            // The Button texts stay the exact selector strings every
            // existing check and driver finds these commands by.
            cornerActions.Clear();
            confirmAction.Clear();
            if (hand.CanPass)
            {
                var pass = new RoundActionUnityView.PewterBanner(
                    () =>
                    {
                        if (!contextOnly)
                        {
                            PassRequested?.Invoke(hand.HeroId);
                        }
                    }, false,
                    CornerBannerFontSize, "gd-pass-banner-label")
                {
                    name = "gd-pass-control",
                    text = "PASS - NO CARD"
                };
                pass.SetVisibleLabel("PASS - NO CARD");
                pass.style.minHeight = CornerBannerHeight;
                pass.style.marginBottom = 8f;
                cornerActions.Add(pass);
            }

            if (hand.SelectedCardInstanceId.HasValue)
            {
                var confirm = new RoundActionUnityView.PewterBanner(
                    () =>
                    {
                        if (!contextOnly)
                        {
                            ConfirmRequested?.Invoke();
                        }
                    }, true,
                    CornerBannerFontSize, "gd-confirm-banner-label")
                {
                    name = "gd-confirm-control",
                    text = "CONFIRM FACEDOWN"
                };
                confirm.SetVisibleLabel("CONFIRM FACEDOWN");
                confirm.style.minHeight = CornerBannerHeight;
                confirmAction.Add(confirm);
            }

            for (var index = 0; index < drawnCards.Length; index++)
            {
                cards.Add(BuildCard(
                    drawnCards[index],
                    compactPeekMode && index == drawnCards.Length - 1,
                    false));
            }

            var unavailable = hand.UnavailableTrayCards.ToArray();
            var unavailableWidth = compactPeekMode
                ? PeekCardOuterWidth
                : unavailable.Length * CardSlotFootprint;
            unavailableCards.style.width = unavailableWidth;
            unavailableCards.style.minWidth = unavailableWidth;
            unavailableCards.style.maxWidth = unavailableWidth;
            var unavailableHeight = compactPeekMode
                ? CompactUnavailableStackHeight(unavailable.Length)
                : PeekCardOuterHeight;
            unavailableCards.style.height = unavailableHeight;
            unavailableCards.style.minHeight = unavailableHeight;
            unavailableCards.style.maxHeight = unavailableHeight;
            for (var index = 0; index < unavailable.Length; index++)
            {
                unavailableCards.Add(BuildCard(
                    unavailable[index],
                    compactPeekMode && index == unavailable.Length - 1,
                    true));
            }
            unavailableGroup.style.display = !trayHidden && unavailable.Length > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private static float CompactCardRowWidth(int cardCount) => cardCount <= 0
            ? 0f
            : PeekCardOuterWidth + ((cardCount - 1) * PeekCardSlotFootprint);

        public static float CompactUnavailableStackHeight(int cardCount) => cardCount <= 0
            ? 0f
            : PeekCardOuterHeight + ((cardCount - 1) * UnavailableCardSlotFootprint);

        private VisualElement BuildCard(
            TrayCardViewModel card,
            bool isLastPeekCard = false,
            bool isUnavailableGroup = false)
        {
            var button = new Button(() =>
            {
                if (!contextOnly && card.IsSelectable)
                {
                    CardSelected?.Invoke(card.CardInstanceId);
                }
            })
            {
                name = "gd-card-choice"
            };
            button.focusable = !contextOnly;
            button.text = string.Empty;
            // The shared builder owns both complete-face shapes: a prepared printed front at its
            // verified source aspect, or the taller typed generated fallback when no resource exists.
            ConfiguredCardFaceView.Build(button, card);
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            if (compactPeekMode)
            {
                ApplyPeekFootprint(button);
            }

            if (card.IsResolving)
            {
                AddPrintedActionOverlay(button, card);
            }

            // Milestone 8.5b: the card sits in a slot, and the slot is what carries the Active-effect
            // outline. It has to be outside the card because the card's own border is the shared
            // selection treatment, and a card can be selected and active at the same time - two
            // meanings on one border would be one meaning lost. The slot is always drawn, so a card
            // does not change size when an effect starts or ends; only the outline's colour does.
            var slot = new VisualElement { name = "gd-card-slot" };
            slot.style.flexDirection = FlexDirection.Column;
            slot.style.alignItems = Align.Stretch;
            slot.style.marginRight = CardGap;
            if (compactPeekMode)
            {
                if (isUnavailableGroup)
                {
                    slot.style.marginRight = 0f;
                    slot.style.marginBottom = isLastPeekCard ? 0f : -UnavailableCardOverlap;
                }
                else
                {
                    slot.style.marginRight = isLastPeekCard ? 0f : -PeekCardOverlap;
                }
                slot.style.flexShrink = 0f;
            }
            HudElements.Padding(slot, ActiveOutlineGap);
            HudElements.Border(
                slot,
                card.IsActiveEffectSource
                    ? MatchHudTheme.ActiveEffectAccent
                    : new Color(0f, 0f, 0f, 0f),
                ActiveOutlineWidth,
                11f);

            // The mockup's raised treatment: a card that can still be played this round sits visibly
            // higher than the ones that cannot. It is a relative offset rather than a margin, because a
            // margin on a stretched card would shorten it instead of moving it. The card keeps its size,
            // its hit target moves with it, and its keyboard order and legality are untouched - the
            // group it belongs to is already in the label either way.
            slot.style.top = card.Group == TrayCardGroup.Available ? -AvailableCardRaise : 0f;

            // Milestone 8.5b's correction. A card carries no commitment command once the round is past
            // choosing, but the card the current turn is being resolved from is the card in play and
            // reads at full strength - "carries no command" and "is spent" are different facts, and the
            // shared unavailable treatment was saying the second about the first.
            if (!card.IsSelectable && card.Group != TrayCardGroup.Available)
            {
                if (card.IsResolving)
                {
                    HudElements.MarkInPlay(button);
                }
                else
                {
                    if (compactPeekMode && isUnavailableGroup)
                    {
                        HudElements.MarkUnavailableOpaqueInspectable(button);
                        button.focusable = !contextOnly;
                    }
                    else if (compactPeekMode)
                    {
                        HudElements.MarkUnavailableOpaque(button);
                    }
                    else
                    {
                        HudElements.MarkUnavailable(button);
                    }
                }
            }

            var printed = card.Face.PrintedTextKind == CardSourceContentKind.Transcribed
                ? card.Face.PrintedText
                : card.Face.EmptyTextLabel;
            button.tooltip = $"{card.AssistiveLabel} - {printed}";
            slot.Add(button);
            BindCardDrag(button, slot, card);
            if (compactPeekMode && !card.IsResolving)
            {
                // The button, not its overlapping layout slot, owns the pointer. Enter/leave events
                // are targeted at the painted card and an overlapped slot is not the hit surface a
                // player sees. The reveal itself is recursively pointer-transparent below, so it can
                // draw on top without taking either this hover or the subsequent click away.
                // The resolving card is already fully exposed and owns live printed-action children;
                // raising a standalone copy over it would hide those children precisely on hover.
                HudElements.OnPointedAt(
                    button,
                    pointedAt => SetPeekPreview(
                        pointedAt ? card : null,
                        slot,
                        isUnavailableGroup));
            }
            return slot;
        }

        private void BindCardDrag(Button button, VisualElement slot, TrayCardViewModel card)
        {
            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || contextOnly || !card.IsSelectable ||
                    !dragState.Begin(evt.pointerId, card.CardInstanceId, evt.position))
                {
                    return;
                }

                dragCapture = button;
                button.CapturePointer(evt.pointerId);
            }, TrickleDown.TrickleDown);

            button.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!dragState.Move(evt.pointerId, evt.position, CardDragThreshold))
                {
                    return;
                }

                ShowDragProxy(card, slot, evt.position);
            }, TrickleDown.TrickleDown);

            button.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!dragState.Release(evt.pointerId, out var cardId, out var wasDrag))
                {
                    return;
                }

                ReleaseDragCapture(evt.pointerId);
                HideDragProxy();
                if (!wasDrag)
                {
                    return;
                }

                // Suppress Button.Clickable's pointer-up path: a drag is one input path and cannot
                // also select the card or reveal the confirmation banner beneath it.
                evt.StopImmediatePropagation();
                if (!contextOnly && isMapDropPoint?.Invoke(evt.position) == true)
                {
                    CardDroppedOnMap?.Invoke(cardId);
                }
            }, TrickleDown.TrickleDown);

            button.RegisterCallback<PointerCancelEvent>(evt =>
            {
                if (dragState.IsTracking)
                {
                    CancelCardDrag();
                }
            });
            button.RegisterCallback<DetachFromPanelEvent>(_ => CancelCardDrag());
        }

        private void ShowDragProxy(
            TrayCardViewModel card,
            VisualElement sourceSlot,
            Vector2 pointerPosition)
        {
            if (dragProxy.childCount == 0)
            {
                var face = BuildStandaloneCard(card);
                IgnorePickingRecursively(face);
                dragProxy.Add(face);
                dragProxy.tooltip = "Commit " + card.DisplayName + " facedown";
            }

            // Keep the hand geometry stable while making it visually unambiguous that the player
            // picked this one card up. `visibility` removes the painted face without letting the
            // overlapped siblings close over its slot; cancellation restores exactly that slot.
            if (draggedCardSlot == null)
            {
                draggedCardSlot = sourceSlot;
                draggedCardSlot.style.visibility = Visibility.Hidden;
            }

            dragProxy.style.left = pointerPosition.x - (CardFootprint.Width * 0.5f);
            dragProxy.style.top = pointerPosition.y - (CardFootprint.Height * 0.5f);
            dragProxy.style.display = DisplayStyle.Flex;
            dragProxy.BringToFront();
        }

        private void CancelCardDrag()
        {
            if (dragCapture != null && dragState.IsTracking)
            {
                ReleaseDragCapture(dragState.PointerId);
            }

            dragState.Cancel();
            dragCapture = null;
            HideDragProxy();
        }

        private void ReleaseDragCapture(int pointerId)
        {
            if (dragCapture != null && dragCapture.HasPointerCapture(pointerId))
            {
                dragCapture.ReleasePointer(pointerId);
            }

            dragCapture = null;
        }

        private void HideDragProxy()
        {
            if (draggedCardSlot != null)
            {
                draggedCardSlot.style.visibility = Visibility.Visible;
                draggedCardSlot = null;
            }

            dragProxy.style.display = DisplayStyle.None;
            dragProxy.Clear();
            dragProxy.tooltip = string.Empty;
        }

        private void AddPrintedActionOverlay(VisualElement cardSurface, TrayCardViewModel card)
        {
            var regions = printedActionRegions
                .Where(value => value.SourceCardInstanceId.Equals(card.CardInstanceId))
                .ToArray();
            if (regions.Length == 0)
            {
                return;
            }

            var overlay = new VisualElement { name = "gd-printed-action-overlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.top = 0f;
            overlay.style.width = Length.Percent(100f);
            overlay.style.height = Length.Percent(100f);
            overlay.pickingMode = PickingMode.Ignore;
            var preparedArtwork = cardSurface.Q<Image>("gd-card-artwork")?.image as Texture2D;
            var usesPreparedArtwork = preparedArtwork != null;
            foreach (var region in regions)
            {
                var bounds = PrintedActionBounds(region, usesPreparedArtwork);
                var control = new Button(() => ForwardPrintedActionRegion(region))
                {
                    name = "gd-printed-action-region",
                    text = string.Empty,
                    tooltip = region.AssistiveLabel,
                    userData = region,
                    focusable = !contextOnly
                };
                ApplyActionBounds(control, bounds, usesPreparedArtwork);
                control.style.backgroundColor = Color.clear;
                control.style.backgroundImage = StyleKeyword.None;
                control.style.color = Color.clear;
                HudElements.Padding(control, 0f);

                // Milestone 8.30d, per the user's final direction: an offered SECONDARY action's
                // own printed icon shines (the per-card overlay tracing that badge's silhouette,
                // degrading to the prior treatment when the overlay is absent), while an offered
                // PRIMARY lights the card's own printed lines - the per-card ring overlay
                // tracing the type ribbon's box and the rules-text box, with no icon shine and
                // no invented lines. On the generated fallback face, which prints no such lines,
                // the primary statement is the drawn glowing box over the typed band, and a
                // secondary is its chip lighting up. An unavailable region carries no shine at
                // all - nothing offered, nothing lit.
                var isPrimary = region.InvocationContext == ActionInvocationContext.PrintedPrimary;
                Image shine = null;
                Image primaryContrast = null;
                PrimaryOfferGlow primaryGlow = null;
                if (usesPreparedArtwork &&
                    region.Invocation != null &&
                    ConfiguredCardShine.TryLoadTexture(
                        card.DefinitionReference, ShineSlotFor(region), out var shineTexture))
                {
                    // The hit surface is invisible: clearing the Button's default theme border is
                    // what the prior treatment did first, and skipping it drew a stock button box
                    // around every lit icon. For a primary the typed slot is the printed-lines
                    // ring; for a secondary it is the badge silhouette.
                    HudElements.Border(control, Color.clear, 0f, 0f);
                    control.style.overflow = Overflow.Visible;
                    shine = BuildShineOverlay(shineTexture, ShineSlotFor(region));
                    if (ShineSlotFor(region) == CardShineSlot.PrimaryRing)
                    {
                        var slot = ShineSlotFor(region);
                        primaryContrast = BuildShineOverlay(shineTexture, slot);
                        primaryContrast.name = "gd-card-shine-primary-contrast";
                        primaryContrast.tintColor = MatchHudTheme.PrimaryShineContrast;
                        SetPrimaryContrastHighlighted(primaryContrast, false);
                        overlay.Add(primaryContrast);
                    }
                    overlay.Add(shine);
                }
                else if (isPrimary && region.Invocation != null)
                {
                    // A prepared front without its ring overlay, and every generated-face
                    // primary: the drawn approximation of the same statement.
                    HudElements.Border(control, Color.clear, 0f, 0f);
                    control.style.overflow = Overflow.Visible;
                    primaryGlow = new PrimaryOfferGlow();
                    ApplyActionBounds(
                        primaryGlow,
                        new Rect(
                            bounds.x - PrimaryOfferGlow.Margin,
                            bounds.y - PrimaryOfferGlow.Margin,
                            bounds.width + PrimaryOfferGlow.Margin * 2f,
                            bounds.height + PrimaryOfferGlow.Margin * 2f),
                        true);
                    overlay.Add(primaryGlow);
                }
                else if (usesPreparedArtwork)
                {
                    AddPreparedArtworkHighlight(
                        control,
                        region.ActionKind,
                        region.Invocation != null);
                }
                else
                {
                    HudElements.Border(control, Color.clear, 0f, 0f);
                    SetGeneratedChipShine(
                        cardSurface, ShineSlotFor(region), region.Invocation != null, false);
                }

                if (region.Invocation == null)
                {
                    HudElements.MarkUnavailable(control);
                }
                else
                {
                    HudElements.OnPointedAt(
                        control,
                        highlighted =>
                        {
                            if (primaryGlow != null)
                            {
                                primaryGlow.SetHovered(highlighted);
                            }
                            else if (shine != null)
                            {
                                SetPreparedShineHighlighted(shine, highlighted);
                                if (primaryContrast != null)
                                {
                                    SetPrimaryContrastHighlighted(primaryContrast, highlighted);
                                }
                            }
                            else if (usesPreparedArtwork)
                            {
                                SetPreparedArtworkGlow(control, highlighted);
                            }
                            else
                            {
                                SetGeneratedChipShine(
                                    cardSurface, ShineSlotFor(region), true, highlighted);
                            }
                        });
                }

                overlay.Add(control);
                if (!contextOnly && region.FastTravelOffer != null)
                {
                    overlay.Add(BuildReplacementChip(
                        region,
                        "FAST TRAVEL",
                        bounds,
                        () =>
                        {
                            if (printedActionRegions.Any(value => ReferenceEquals(value, region)))
                            {
                                FastTravelSelected?.Invoke(region.FastTravelOffer);
                            }
                        },
                        true,
                        usesPreparedArtwork));
                }

                if (!contextOnly && region.ClearOffer != null)
                {
                    overlay.Add(BuildReplacementChip(
                        region,
                        "CLEAR",
                        bounds,
                        () =>
                        {
                            if (region.ClearOffer.IsAvailable &&
                                printedActionRegions.Any(value => ReferenceEquals(value, region)))
                            {
                                ClearSelected?.Invoke(region.ClearOffer);
                            }
                        },
                        region.ClearOffer.IsAvailable,
                        usesPreparedArtwork));
                }
            }

            cardSurface.Add(overlay);
        }

        private static Button BuildReplacementChip(
            PrintedActionRegionViewModel region,
            string label,
            Rect actionBounds,
            Action selected,
            bool available,
            bool usesPreparedArtwork)
        {
            // The replacement controls wear the card's own banner language instead of the dark
            // utility chips that sat beside the art belonging to nothing. A prepared Primary uses
            // the symmetric title tab approved at 8.30f2; Secondary Fast Travel keeps 8.30d's
            // side-mounted swallowtail. The cloth follows the CARD's printed colour, and remains
            // text-first by the standing rule: this HUD invents no action glyph.
            var clearAbovePreparedAttack = usesPreparedArtwork &&
                region.ActionKind == ActionKind.Attack &&
                string.Equals(label, "CLEAR", StringComparison.Ordinal);
            var fastTravelAbovePreparedMovement = usesPreparedArtwork &&
                region.ActionKind == ActionKind.Movement &&
                string.Equals(label, "FAST TRAVEL", StringComparison.Ordinal);
            var attachedToPreparedPrimaryTitle =
                (clearAbovePreparedAttack || fastTravelAbovePreparedMovement) &&
                region.InvocationContext == ActionInvocationContext.PrintedPrimary;
            var attachedAbovePreparedAction = attachedToPreparedPrimaryTitle;
            var chip = new ReplacementRibbon(
                available ? selected : null,
                available,
                region.CardColor,
                attachedToPreparedPrimaryTitle)
            {
                name = "gd-action-replacement-chip",
                text = label,
                tooltip = available
                    ? $"{label} replaces {region.ActionKind}"
                    : $"{label} replaces {region.ActionKind} - unavailable",
                userData = region
            };
            if (attachedToPreparedPrimaryTitle)
            {
                chip.AddToClassList("gd-replacement-primary-tab");
            }
            chip.SetVisibleLabel(label);
            if (string.Equals(label, "FAST TRAVEL", StringComparison.Ordinal))
            {
                chip.SetTextColor(Color.black);
            }
            chip.style.position = Position.Absolute;
            var titleBandTop = actionBounds.y +
                ((region.ActionKind == ActionKind.Attack ? 40f : 36f) *
                    CardFootprint.ArtworkHeight / CardFootprint.ArtworkSourceHeight);
            var attachmentCenterX = attachedToPreparedPrimaryTitle
                ? CardFootprint.ArtworkWidth * 0.5f
                : actionBounds.center.x;
            var placeBesidePreparedRegion = usesPreparedArtwork &&
                !clearAbovePreparedAttack &&
                actionBounds.xMax + 96f <= CardFootprint.ArtworkWidth;
            chip.style.left = attachedAbovePreparedAction
                ? Mathf.Clamp(
                    attachmentCenterX - 46f,
                    0f,
                    CardFootprint.ArtworkWidth - 92f)
                : placeBesidePreparedRegion
                    ? actionBounds.xMax - 2f
                    : actionBounds.xMax - 96f;
            chip.style.top = attachedAbovePreparedAction
                ? (attachedToPreparedPrimaryTitle ? titleBandTop : actionBounds.y) - 22f
                : placeBesidePreparedRegion
                    ? actionBounds.y + Mathf.Max(0f, (actionBounds.height - 26f) * 0.5f)
                    : usesPreparedArtwork
                        ? actionBounds.y + 4f
                        : actionBounds.yMax - 26f;
            chip.style.width = 92f;
            chip.style.height = 22f;
            if (!available)
            {
                HudElements.MarkUnavailable(chip);
            }
            else
            {
                HudElements.OnPointedAt(chip, chip.SetShineHovered);
            }

            return chip;
        }

        /// <summary>
        /// An offered PRIMARY action's treatment, per the user's final 8.30d direction: the whole
        /// printed primary section glows - the same warm halo the rail badges wear, drawn as
        /// widening strokes around the section's box because Painter2D has no blur - and the icon
        /// itself carries no shine. Steady by the approved rule; pointing at it brightens it.
        /// </summary>
        private sealed class PrimaryOfferGlow : VisualElement
        {
            internal const float Margin = 12f;

            private bool hovered;

            internal PrimaryOfferGlow()
            {
                name = "gd-primary-offer-glow";
                pickingMode = PickingMode.Ignore;
                focusable = false;
                style.overflow = Overflow.Visible;
                generateVisualContent += OnGenerateVisualContent;
            }

            internal void SetHovered(bool value)
            {
                if (hovered == value)
                {
                    return;
                }

                hovered = value;
                MarkDirtyRepaint();
            }

            private void OnGenerateVisualContent(MeshGenerationContext context)
            {
                var painter = context.painter2D;
                var rect = contentRect;
                if (rect.width <= Margin * 2f + 4f || rect.height <= Margin * 2f + 4f)
                {
                    return;
                }

                var box = new Rect(
                    rect.x + Margin, rect.y + Margin,
                    rect.width - Margin * 2f, rect.height - Margin * 2f);
                var boost = hovered ? 1.3f : 1f;
                StrokeBox(painter, box,
                    WithAlpha(MatchHudTheme.PrimaryShineHalo, 0.22f * boost), 9f);
                StrokeBox(painter, box,
                    WithAlpha(MatchHudTheme.PrimaryShineGlow, 0.48f * boost), 5f);
                StrokeBox(painter, box,
                    WithAlpha(MatchHudTheme.PrimaryShineRim,
                        Mathf.Min(1f, 0.96f * boost)), 2f);
            }

            private static Color WithAlpha(Color color, float alpha) =>
                new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));

            private static void StrokeBox(Painter2D painter, Rect box, Color color, float width)
            {
                painter.strokeColor = color;
                painter.lineWidth = width;
                painter.lineJoin = LineJoin.Round;
                painter.BeginPath();
                painter.MoveTo(new Vector2(box.xMin, box.yMin));
                painter.LineTo(new Vector2(box.xMax, box.yMin));
                painter.LineTo(new Vector2(box.xMax, box.yMax));
                painter.LineTo(new Vector2(box.xMin, box.yMax));
                painter.ClosePath();
                painter.Stroke();
            }
        }

        /// <summary>
        /// A replacement offer in the cloth of its card: a symmetric trapezoid when attached to a
        /// prepared Primary title, or the established side-mounted swallowtail for Secondary Fast
        /// Travel. It remains exactly the projected button: same forwarding and availability
        /// semantics. Pointing at it gives the cloth the shared warm shine.
        /// </summary>
        private sealed class ReplacementRibbon : Button
        {
            private const float NotchDepth = 10f;

            private readonly bool available;
            private readonly CardColor cardColor;
            private readonly bool symmetricTab;
            private bool shineHovered;

            private readonly VisualElement cloth;

            internal ReplacementRibbon(
                Action selected,
                bool available,
                CardColor cardColor,
                bool symmetricTab)
                : base(selected)
            {
                this.available = available;
                this.cardColor = cardColor;
                this.symmetricTab = symmetricTab;
                style.backgroundColor = Color.clear;
                HudElements.Border(this, Color.clear, 0f, 0f);
                HudElements.Padding(this, 0f);
                style.paddingLeft = symmetricTab ? NotchDepth : 0f;
                style.paddingRight = NotchDepth;
                style.fontSize = 9f;
                style.unityFontStyleAndWeight = FontStyle.Bold;
                style.unityTextAlign = TextAnchor.MiddleCenter;
                style.color = available
                    ? MatchHudTheme.RibbonInk(cardColor)
                    : MatchHudTheme.MutedText;
                style.overflow = Overflow.Visible;

                // A TextElement paints its own words FIRST - both a generateVisualContent mesh and
                // any child render above them, so there is no order in which a self-painted cloth
                // sits under the built-in label. The cloth is therefore a child, and the VISIBLE
                // words are a second child above it; the Button's own text stays set (buried under
                // the cloth) because it is the typed selector every existing check finds a
                // replacement control by.
                cloth = new VisualElement
                {
                    name = "gd-ribbon-cloth",
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                cloth.style.position = Position.Absolute;
                cloth.style.left = 0f;
                cloth.style.top = 0f;
                cloth.style.right = 0f;
                cloth.style.bottom = 0f;
                cloth.style.overflow = Overflow.Visible;
                cloth.generateVisualContent += OnGenerateVisualContent;
                Add(cloth);
                cloth.SendToBack();

                visibleLabel = new Label
                {
                    name = "gd-ribbon-label",
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                visibleLabel.style.position = Position.Absolute;
                visibleLabel.style.left = symmetricTab ? NotchDepth : 0f;
                visibleLabel.style.top = 0f;
                visibleLabel.style.right = NotchDepth;
                visibleLabel.style.bottom = 0f;
                visibleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                visibleLabel.style.fontSize = 9f;
                visibleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                visibleLabel.style.color = available
                    ? MatchHudTheme.RibbonInk(cardColor)
                    : MatchHudTheme.MutedText;
                Add(visibleLabel);
            }

            private readonly Label visibleLabel;

            /// <summary>The words the ribbon shows; the Button's own text is the selector only.</summary>
            internal void SetVisibleLabel(string label)
            {
                visibleLabel.text = label ?? string.Empty;
            }

            internal void SetTextColor(Color color)
            {
                style.color = color;
                visibleLabel.style.color = color;
            }

            internal void SetShineHovered(bool hovered)
            {
                if (shineHovered == hovered)
                {
                    return;
                }

                shineHovered = hovered;
                cloth.MarkDirtyRepaint();
            }

            private void OnGenerateVisualContent(MeshGenerationContext context)
            {
                var painter = context.painter2D;
                var rect = cloth.contentRect;
                if (rect.width <= NotchDepth + 2f || rect.height <= 4f)
                {
                    return;
                }

                var right = rect.xMax;
                var mid = rect.center.y;

                if (available && shineHovered)
                {
                    // The warm shine, painted as widening strokes because Painter2D has no blur.
                    DrawBanner(painter, rect, right, mid, Color.clear,
                        new Color(MatchHudTheme.ShineGlow.r, MatchHudTheme.ShineGlow.g,
                            MatchHudTheme.ShineGlow.b, 0.20f), 8f);
                    DrawBanner(painter, rect, right, mid, Color.clear,
                        new Color(MatchHudTheme.ShineRim.r, MatchHudTheme.ShineRim.g,
                            MatchHudTheme.ShineRim.b, 0.45f), 4f);
                }

                var fabric = available
                    ? MatchHudTheme.RibbonCloth(cardColor)
                    : new Color(0.42f, 0.40f, 0.36f, 1f);
                DrawBanner(painter, rect, right, mid, fabric, Color.clear, 0f);

                // The darker lower fold keeps it reading as cloth rather than plastic. Primary
                // title tabs use the same inset on both sides; the established Secondary ribbon
                // keeps its right-hand swallowtail unchanged.
                painter.fillColor = new Color(0f, 0f, 0f, available ? 0.14f : 0.10f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(
                    symmetricTab ? rect.xMin + (NotchDepth * 0.5f) : rect.xMin,
                    mid));
                painter.LineTo(new Vector2(
                    symmetricTab ? right - (NotchDepth * 0.5f) : right - NotchDepth,
                    mid));
                painter.LineTo(new Vector2(right, rect.yMax));
                painter.LineTo(new Vector2(rect.xMin, rect.yMax));
                painter.ClosePath();
                painter.Fill();

                DrawBanner(painter, rect, right, mid, Color.clear,
                    available
                        ? Color.Lerp(fabric, Color.black, 0.62f)
                        : new Color(0.25f, 0.25f, 0.25f, 1f), 2f);
            }

            private void DrawBanner(
                Painter2D painter, Rect rect, float right, float mid,
                Color fill, Color stroke, float strokeWidth)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(
                    symmetricTab ? rect.xMin + NotchDepth : rect.xMin,
                    rect.yMin));
                painter.LineTo(new Vector2(
                    symmetricTab ? right - NotchDepth : right,
                    rect.yMin));
                if (!symmetricTab)
                {
                    painter.LineTo(new Vector2(right - NotchDepth, mid));
                }
                painter.LineTo(new Vector2(right, rect.yMax));
                painter.LineTo(new Vector2(rect.xMin, rect.yMax));
                painter.ClosePath();
                if (fill.a > 0f)
                {
                    painter.fillColor = fill;
                    painter.Fill();
                }

                if (stroke.a > 0f && strokeWidth > 0f)
                {
                    painter.strokeColor = stroke;
                    painter.lineWidth = strokeWidth;
                    painter.lineJoin = LineJoin.Round;
                    painter.Stroke();
                }
            }
        }

        private static void ApplyActionBounds(
            VisualElement element, Rect bounds, bool usesPreparedArtwork)
        {
            const float maximumGlowWidth = 4f;
            element.style.position = Position.Absolute;
            element.style.left = bounds.x;
            element.style.top = bounds.y;
            element.style.width = usesPreparedArtwork
                ? bounds.width
                : bounds.width - (maximumGlowWidth * 2f);
            element.style.height = usesPreparedArtwork
                ? bounds.height
                : bounds.height - (maximumGlowWidth * 2f);
        }

        private static Rect PrintedActionBounds(
            PrintedActionRegionViewModel region, bool usesPreparedArtwork)
        {
            if (usesPreparedArtwork)
            {
                return ScalePreparedActionBounds(PreparedActionSourceBounds(region));
            }

            // The generated fallback owns a different anatomy and keeps its established upper bands.
            if (region.InvocationContext == ActionInvocationContext.PrintedPrimary)
            {
                return new Rect(4f, 78f, CardFootprint.Width - 8f, 50f);
            }

            var count = Mathf.Max(1, region.SecondaryCount);
            var width = (CardFootprint.Width - 8f) / count;
            return new Rect(4f + (width * region.SecondaryIndex), 47f, width, 31f);
        }

        private static Rect PreparedActionSourceBounds(PrintedActionRegionViewModel region)
        {
            // Prepared fronts share one verified 372x520 anatomy. Movement owns a compact circular
            // treatment around its printed boot/value badge. Primary Attack and Skill own the broad
            // lower action panel, so their temporary treatment is a single large box around that
            // complete printed section. These are shared template coordinates, not card-name rules.
            if (region.InvocationContext == ActionInvocationContext.PrintedPrimary)
            {
                switch (region.ActionKind)
                {
                    case ActionKind.Movement:
                        return new Rect(8f, 330f, 80f, 82f);
                    case ActionKind.Attack:
                        return new Rect(6f, 308f, 360f, 184f);
                    case ActionKind.Skill:
                        return new Rect(6f, 328f, 360f, 164f);
                    default:
                        return new Rect(6f, 318f, 360f, 174f);
                }
            }

            if (region.ActionKind == ActionKind.Movement)
            {
                // The visible boot/value composite spans roughly x=21..69 and y=203..249 on the
                // shared source. This footprint centres the circle on that ink rather than the rail.
                return new Rect(5f, 184f, 80f, 82f);
            }

            // Future typed prepared Secondary actions retain a bounded left-rail badge without
            // changing the current Movement mapping or introducing identity-specific geometry.
            var index = Mathf.Max(0, region.SecondaryIndex);
            return new Rect(12f, 124f + (70f * index), 70f, 70f);
        }

        private static Rect ScalePreparedActionBounds(Rect sourceBounds)
        {
            var scaleX = CardFootprint.ArtworkWidth / CardFootprint.ArtworkSourceWidth;
            var scaleY = CardFootprint.ArtworkHeight / CardFootprint.ArtworkSourceHeight;
            return new Rect(
                sourceBounds.x * scaleX,
                sourceBounds.y * scaleY,
                sourceBounds.width * scaleX,
                sourceBounds.height * scaleY);
        }

        private static void AddPreparedArtworkHighlight(
            VisualElement control,
            ActionKind actionKind,
            bool available)
        {
            // The control remains the typed hit/focus surface but contributes no fill or text. The
            // temporary prepared-card treatment is deliberately simple: a circle around Movement's
            // printed boot/value badge, or a broad box around the complete Primary Skill/Attack panel.
            HudElements.Border(control, Color.clear, 0f, 0f);
            control.style.overflow = Overflow.Visible;
            if (!available)
            {
                return;
            }

            var glow = new PreparedActionGlow(actionKind == ActionKind.Movement)
            {
                name = "gd-prepared-action-glow",
                pickingMode = PickingMode.Ignore,
                focusable = false,
                userData = actionKind
            };
            glow.style.position = Position.Absolute;
            glow.style.left = 0f;
            glow.style.top = 0f;
            glow.style.width = Length.Percent(100f);
            glow.style.height = Length.Percent(100f);
            control.Add(glow);
            SetPreparedArtworkGlow(control, false);
        }

        private static void SetPreparedArtworkGlow(VisualElement control, bool highlighted)
        {
            var glow = control.Q<PreparedActionGlow>("gd-prepared-action-glow");
            if (glow == null)
            {
                return;
            }

            glow.SetHighlighted(highlighted);
        }

        /// <summary>
        /// The steady strength of a lit icon; pointer or focus raises it to full. Strong enough
        /// to be obvious at rest (the user's bar), but far enough below full that the hover
        /// response is visible too - at 0.95 the raise to 1.0 read as nothing, which the user
        /// called out on the live build.
        /// </summary>
        public const float SteadyShineOpacity = 0.90f;

        /// <summary>
        /// A Primary's traced printed-lines ring needs a stronger core than the compact Secondary
        /// badges because it crosses both pale rules text and busy art at card scale.
        /// </summary>
        public const float PrimaryRingSteadyOpacity = 0.96f;

        /// <summary>The aligned graphite pass that keys the Primary ring over any card colour.</summary>
        public const float PrimaryRingContrastOpacity = 0.52f;

        /// <summary>
        /// Which prepared shine slot an offered printed region lights: the printed-lines ring for
        /// a printed primary (the user's final direction - the existing boxes glow, not the icon),
        /// and the rail badge of the region's own kind for a secondary. Milestone 8.30d - the
        /// mapping is typed, exactly like the sprite files it selects between.
        /// </summary>
        private static CardShineSlot ShineSlotFor(PrintedActionRegionViewModel region)
        {
            if (region.InvocationContext == ActionInvocationContext.PrintedPrimary)
            {
                return CardShineSlot.PrimaryRing;
            }

            return region.ActionKind == ActionKind.Defense
                ? CardShineSlot.SecondaryDefense
                : CardShineSlot.SecondaryMovement;
        }

        /// <summary>
        /// One prepared shine overlay as a picking-ignored image spanning the whole card, so it
        /// aligns with the artwork it was traced from and no geometry lives in code.
        /// </summary>
        internal static Image BuildShineOverlay(Texture2D texture, CardShineSlot slot)
        {
            var shine = new Image
            {
                name = "gd-card-shine",
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                tintColor = slot == CardShineSlot.PrimaryRing
                    ? MatchHudTheme.PrimaryShineTint
                    : Color.white,
                pickingMode = PickingMode.Ignore,
                focusable = false,
                userData = slot
            };
            shine.style.position = Position.Absolute;
            shine.style.left = 0f;
            shine.style.top = 0f;
            shine.style.right = 0f;
            shine.style.bottom = 0f;
            SetPreparedShineHighlighted(shine, false);
            return shine;
        }

        /// <summary>
        /// Strengthens one prepared overlay without changing any of its resolved geometry. Hover and
        /// keyboard focus share this path so neither can make the traced artwork jump on the card.
        /// </summary>
        internal static void SetPreparedShineHighlighted(Image shine, bool highlighted)
        {
            if (shine == null)
            {
                throw new ArgumentNullException(nameof(shine));
            }

            var primary = shine.userData is CardShineSlot slot &&
                slot == CardShineSlot.PrimaryRing;
            shine.style.opacity = highlighted
                ? 1f
                : primary ? PrimaryRingSteadyOpacity : SteadyShineOpacity;
        }

        private static void SetPrimaryContrastHighlighted(Image shine, bool highlighted)
        {
            shine.style.opacity = highlighted ? 0.68f : PrimaryRingContrastOpacity;
        }


        /// <summary>
        /// The generated fallback face's version of the same statement: the typed chip that prints
        /// the slot's value lights up. Recolour only - a border or size change would move the very
        /// layout the shared selection treatment promises never to move.
        /// </summary>
        internal static void SetGeneratedChipShine(
            VisualElement cardSurface, CardShineSlot slot, bool available, bool highlighted)
        {
            if (!available)
            {
                return;
            }

            var chip = FindGeneratedChip(cardSurface, slot);
            if (chip == null)
            {
                return;
            }

            var ink = new Color(0.16f, 0.12f, 0.05f, 1f);
            chip.style.backgroundColor = highlighted
                ? MatchHudTheme.ShineRim
                : MatchHudTheme.ShineGlow;
            chip.style.color = ink;
            if (chip.name == "gd-card-primary-value")
            {
                // The plate already carries a 2-unit border; recolouring it changes no layout.
                var rim = highlighted ? Color.white : MatchHudTheme.ShineRim;
                chip.style.borderLeftColor = rim;
                chip.style.borderRightColor = rim;
                chip.style.borderTopColor = rim;
                chip.style.borderBottomColor = rim;
                foreach (var child in chip.Children())
                {
                    child.style.color = ink;
                }
            }
        }

        private static VisualElement FindGeneratedChip(VisualElement cardSurface, CardShineSlot slot)
        {
            switch (slot)
            {
                case CardShineSlot.Primary:
                case CardShineSlot.PrimaryRing:
                    return cardSurface.Q<VisualElement>("gd-card-primary-value") ??
                        cardSurface.Q<VisualElement>("gd-card-primary-kind");
                case CardShineSlot.SecondaryDefense:
                    return cardSurface.Q<VisualElement>("gd-card-secondary-defense");
                case CardShineSlot.SecondaryMovement:
                    return cardSurface.Q<VisualElement>("gd-card-secondary-movement");
                default:
                    return null;
            }
        }

        /// <summary>
        /// Lights one badge of a standalone card face - the Defense prompt's use, where every legal
        /// card shines exactly the printed region it would defend with. Prepared faces get the traced
        /// overlay; generated faces light the typed chip; a missing overlay quietly lights nothing
        /// beyond the loader's one-time diagnostic, because the face itself is already the offer.
        /// </summary>
        public static void AddFaceShine(
            VisualElement faceElement, TrayCardViewModel card, CardShineSlot slot)
        {
            if (faceElement == null || card == null || slot == CardShineSlot.None)
            {
                return;
            }

            if (faceElement.Q<Image>("gd-card-artwork") != null)
            {
                if (ConfiguredCardShine.TryLoadTexture(card.DefinitionReference, slot, out var texture))
                {
                    if (slot == CardShineSlot.PrimaryRing)
                    {
                        // Match the ordinary printed-action surface exactly. The cool ring alone
                        // disappears into Blue and pale rules panels (Dodge exposed this); its
                        // aligned graphite keyline is what makes the same traced lines legible.
                        var contrast = BuildShineOverlay(texture, slot);
                        contrast.name = "gd-card-shine-primary-contrast";
                        contrast.tintColor = MatchHudTheme.PrimaryShineContrast;
                        SetPrimaryContrastHighlighted(contrast, false);
                        faceElement.Add(contrast);
                    }

                    faceElement.Add(BuildShineOverlay(texture, slot));
                }

                return;
            }

            SetGeneratedChipShine(faceElement, slot, true, false);
        }

        /// <summary>
        /// A stable three-layer treatment drawn over one prepared action region. It never copies card
        /// pixels or invents an action glyph: Movement receives a circular ring around the existing
        /// boot/value badge, while Primary Skill and Attack receive a broad rounded rectangle around
        /// their complete printed action panel.
        /// </summary>
        private sealed class PreparedActionGlow : VisualElement
        {
            private readonly VisualElement halo;
            private readonly VisualElement bloom;
            private readonly VisualElement core;

            internal PreparedActionGlow(bool circular)
            {
                AddToClassList(circular
                    ? "gd-prepared-action-glow-circle"
                    : "gd-prepared-action-glow-box");
                var radius = circular ? 999f : 8f;
                halo = BuildLayer(
                    "gd-prepared-action-glow-halo",
                    new Color(1f, 0.52f, 0.04f, 0.16f),
                    10f,
                    radius,
                    -4f);
                bloom = BuildLayer(
                    "gd-prepared-action-glow-bloom",
                    new Color(1f, 0.72f, 0.10f, 0.38f),
                    6f,
                    radius,
                    -2f);
                bloom.style.backgroundColor = circular
                    ? new Color(1f, 0.72f, 0.10f, 0.25f)
                    : new Color(1f, 0.72f, 0.10f, 0.09f);
                core = BuildLayer(
                    "gd-prepared-action-glow-core",
                    new Color(1f, 0.99f, 0.82f, 1f),
                    2f,
                    radius,
                    1f);
                Add(halo);
                Add(bloom);
                Add(core);
                SetHighlighted(false);
            }

            internal void SetHighlighted(bool value)
            {
                // Hover/focus strengthens the same rings without resizing or relocating either one.
                style.opacity = value ? 1f : 0.92f;
            }

            private static VisualElement BuildLayer(
                string name, Color color, float width, float radius, float inset)
            {
                var layer = new VisualElement
                {
                    name = name,
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                layer.style.position = Position.Absolute;
                layer.style.left = inset;
                layer.style.top = inset;
                layer.style.right = inset;
                layer.style.bottom = inset;
                layer.style.backgroundColor = Color.clear;
                HudElements.Border(layer, color, width, radius);
                return layer;
            }
        }


        internal static void ApplyPeekFootprint(VisualElement card)
        {
            card.style.width = PeekCardWidth;
            card.style.minWidth = PeekCardWidth;
            card.style.maxWidth = PeekCardWidth;
            card.style.height = PeekCardHeight;
            card.style.minHeight = PeekCardHeight;
            card.style.maxHeight = PeekCardHeight;
        }

        /// <summary>
        /// The compact card a peek preview currently stands in for. While the full face is up,
        /// the resting card underneath goes fully transparent - the user's correction: the pop
        /// REPLACES the card, it does not float over a visible copy of it. Opacity, not
        /// visibility, so the anchor keeps receiving the very pointer events that hold the
        /// preview open.
        /// </summary>
        private VisualElement peekAnchor;

        private void SetPeekPreview(
            TrayCardViewModel card,
            VisualElement anchor,
            bool isUnavailableGroup)
        {
            if (!compactPeekMode || card == null)
            {
                ClearPeekPreview();
                return;
            }

            if (peekAnchor != null && peekAnchor != anchor)
            {
                peekAnchor.style.opacity = 1f;
            }

            peekAnchor = anchor;
            if (peekAnchor != null)
            {
                peekAnchor.style.opacity = 0f;
            }

            highlightedPeekCard = card;
            PopulatePeekPreview(hoverPreview, card, anchor, isUnavailableGroup);
        }

        /// <summary>
        /// Draws the ordinary hand's complete pointer-transparent peek at its shared bounded position.
        /// Defense offers reuse this exact presentation rather than maintaining a second card fan.
        /// </summary>
        internal static void PopulatePeekPreview(
            VisualElement preview,
            TrayCardViewModel card,
            VisualElement anchor,
            bool isRightSideGroup = false)
        {
            preview.Clear();
            var face = BuildStandaloneCard(card);
            if (isRightSideGroup)
            {
                HudElements.MarkUnavailableOpaqueInspectable(face);
                face.focusable = false;
            }
            IgnorePickingRecursively(face);
            preview.Add(face);
            preview.style.display = DisplayStyle.Flex;
            preview.BringToFront();
            var viewport = ConfiguredMatchHudRenderer.PanelViewport;
            var bounds = anchor.worldBound;
            var rightLimit = isRightSideGroup
                ? Mathf.Max(
                    CardFootprint.ExteriorOutlineWidth,
                    viewport.x - CardFootprint.Width - CardFootprint.ExteriorOutlineWidth)
                : Mathf.Max(
                    CardFootprint.ExteriorOutlineWidth,
                    Mathf.Min(
                        viewport.x,
                        ConfiguredMatchHudRenderer.CompactHandRightEdge) -
                    CardFootprint.Width - CardFootprint.ExteriorOutlineWidth);
            preview.style.left = Mathf.Clamp(
                bounds.xMin,
                CardFootprint.ExteriorOutlineWidth,
                rightLimit);
            preview.style.top = Mathf.Clamp(
                bounds.yMin,
                ConfiguredMatchHudRenderer.StatusBarHeight + 4f,
                Mathf.Max(
                    CardFootprint.ExteriorOutlineWidth,
                    viewport.y - CardFootprint.Height - CardFootprint.ExteriorOutlineWidth));
        }

        private void ClearPeekPreview()
        {
            if (peekAnchor != null)
            {
                peekAnchor.style.opacity = 1f;
                peekAnchor = null;
            }

            highlightedPeekCard = null;
            hoverPreview.Clear();
            hoverPreview.style.display = DisplayStyle.None;
        }

        internal static void IgnorePickingRecursively(VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;
            foreach (var child in element.Children())
            {
                IgnorePickingRecursively(child);
            }
        }

        /// <summary>
        /// One card's whole printed face as a standalone element, with no control around it.
        ///
        /// Milestone 8.5a. It is the same face the tray draws - one builder, so a card cannot look like
        /// two different cards depending on which surface shows it - and it exists because the hero
        /// rail's turn slots reveal the card played on a turn when the pointer rests on one.
        /// </summary>
        public static VisualElement BuildStandaloneCard(TrayCardViewModel card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            var element = ConfiguredCardFaceView.Build(
                new VisualElement { name = "gd-card-preview" }, card);
            element.tooltip = card.AssistiveLabel;
            return element;
        }

        /// <summary>
        /// The printed card's own bevel and the five bands inside it: a hairline of the card's ink just
        /// inside its dark edge.
        ///
        /// It is a wrapper rather than a second border on the card itself, because on a tray card that
        /// border is the shared selection treatment and must stay free to become the selected outline.
        /// </summary>
        internal static VisualElement BuildGeneratedCardFrame(TrayCardViewModel card)
        {
            var ink = MatchHudTheme.FaceText(card.Color);
            var frame = new VisualElement { name = "gd-card-frame" };
            frame.style.flexDirection = FlexDirection.Column;
            frame.style.flexGrow = 1f;
            frame.style.marginLeft = 3f;
            frame.style.marginRight = 3f;
            frame.style.marginTop = 3f;
            frame.style.marginBottom = 3f;
            frame.style.overflow = Overflow.Hidden;
            HudElements.Border(frame, new Color(ink.r, ink.g, ink.b, 0.30f), 1f, 5f);
            frame.Add(BuildNameBand(card, ink));
            frame.Add(BuildSecondaryValueBand(card, ink));
            frame.Add(BuildPrimaryActionBand(card, ink));
            frame.Add(BuildRulesPanel(card, ink));
            frame.Add(BuildHistoryBand(card, ink));
            return frame;
        }

        /// <summary>
        /// The card's upper band: rulebook p.7's Initiative on a flag of its own, and the card's name
        /// beside it.
        ///
        /// The flag is a fixed column so five of them line up down a hand's left edge. p.16's Ultimate
        /// prints no Initiative at all, and says so with an em dash rather than by leaving the column out
        /// and shifting its own name away from every other card's.
        /// </summary>
        private static VisualElement BuildNameBand(TrayCardViewModel card, Color ink)
        {
            var band = new VisualElement { name = "gd-card-name-band" };
            band.style.flexDirection = FlexDirection.Column;
            band.style.alignItems = Align.Stretch;
            band.style.justifyContent = Justify.Center;
            band.style.backgroundColor = MatchHudTheme.FaceBand(card.Color);
            band.style.minHeight = NameBandMinimumHeight;
            // Symmetric horizontally, so the name's centre is the card's.
            band.style.paddingLeft = NameBandHorizontalPadding;
            band.style.paddingRight = NameBandHorizontalPadding;
            band.style.paddingTop = 4f;
            band.style.paddingBottom = 4f;

            // A banner in the card's own corner rather than a column in a row: square along the top,
            // where it meets the card's edge, and rounded where it hangs into the card. Taking it out of
            // the flow is what lets the name centre on the card itself - laid out beside the flag, the
            // name was either pushed off-centre by it or centred in a box with a dead gap opposite.
            var flag = new VisualElement { name = "gd-card-initiative" };
            flag.style.position = Position.Absolute;
            flag.style.left = 0f;
            flag.style.top = 0f;
            flag.style.width = InitiativeFlagWidth;
            flag.style.height = InitiativeFlagHeight;
            flag.style.alignItems = Align.Center;
            flag.style.justifyContent = Justify.Center;
            flag.style.backgroundColor = MatchHudTheme.FaceFlag(card.Color);
            flag.style.borderLeftWidth = 2f;
            flag.style.borderRightWidth = 2f;
            flag.style.borderBottomWidth = 2f;
            flag.style.borderTopWidth = 0f;
            var flagEdge = new Color(ink.r, ink.g, ink.b, 0.45f);
            flag.style.borderLeftColor = flagEdge;
            flag.style.borderRightColor = flagEdge;
            flag.style.borderBottomColor = flagEdge;
            flag.style.borderBottomLeftRadius = 14f;
            flag.style.borderBottomRightRadius = 14f;
            var initiative = card.Face.Initiative;
            var initiativeLabel = HudElements.Text(
                initiative?.AmountLabel ?? "-", initiative == null ? 14f : 19f, ink, FontStyle.Bold);
            // Never wrapped: p.7's Initiative is a two-digit number on most of Wasp's faces, and a flag
            // that broke "13" across two lines would be showing a value the card does not print.
            initiativeLabel.style.whiteSpace = WhiteSpace.NoWrap;
            flag.Add(initiativeLabel);
            flag.tooltip = initiative?.AssistiveLabel ?? "Prints no Initiative";
            band.Add(flag);

            // The name sits on the card's own centre line, in the header strip the banner hangs beside.
            //
            // Milestone 8.7a, following the approved current-card mockup. Both of the earlier
            // arrangements traded one property against the other: laid out beside the banner in a row
            // the name was pushed off the card's centre, and centred in a box narrow enough to start
            // clear of the banner it broke "PROJECTILES" mid-word with the rest of the band empty. This
            // takes the banner out of the flow and reserves its width on *both* sides, so the text box
            // is narrower than the card but still concentric with it: the title reads as centred on the
            // card, and a long one wraps inside a box that cannot reach the banner. The band's floor
            // height keeps the bands below it in step with its neighbours' whether a name takes one
            // line or two.
            //
            // The printed colour is not written on the face. By the user's 2026-08-21 decision the word
            // came off the card - a card wearing its colour and then also spelling it is saying the same
            // thing twice, five times over, in the space the printed text needs. It is not lost: the
            // colour's name stays in this card's hover and assistive reading, which is exactly where
            // milestone 7.8a already put the colour of a hero rail's turn slot, so nothing that means
            // something by colour here means it by colour alone.
            var name = HudElements.Text(card.DisplayName.ToUpperInvariant(), 10.5f, ink, FontStyle.Bold);
            name.name = "gd-card-name";
            name.style.flexShrink = 1f;
            name.style.paddingLeft = InitiativeFlagWidth - NameBandHorizontalPadding;
            name.style.paddingRight = InitiativeFlagWidth - NameBandHorizontalPadding;
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            band.Add(name);
            return band;
        }

        /// <summary>
        /// Every printed value belonging to an action that is not the card's primary - p.9's secondary
        /// Movement and p.14's secondary Defense - kept in a region of their own so a number a player
        /// might spend a whole turn on is never read as the primary action's.
        ///
        /// The region is drawn even on a face that prints none, saying so, because five cards whose
        /// bands line up are what makes a hand comparable at a glance.
        /// </summary>
        private static VisualElement BuildSecondaryValueBand(TrayCardViewModel card, Color ink)
        {
            var band = new VisualElement { name = "gd-card-secondary-values" };
            band.style.flexDirection = FlexDirection.Row;
            band.style.alignItems = Align.Center;
            band.style.justifyContent = Justify.Center;
            band.style.minHeight = SecondaryBandMinimumHeight;
            band.style.backgroundColor = MatchHudTheme.FaceBand(card.Color);
            band.style.paddingTop = 4f;
            band.style.paddingBottom = 5f;
            band.style.paddingLeft = 6f;
            band.style.paddingRight = 6f;
            band.style.borderTopWidth = 1f;
            band.style.borderTopColor = new Color(ink.r, ink.g, ink.b, 0.22f);

            if (card.Face.SecondaryValues.Count == 0)
            {
                var none = HudElements.Text("-", 11f, new Color(ink.r, ink.g, ink.b, 0.6f), FontStyle.Bold);
                none.tooltip = "No secondary action values";
                band.Add(none);
                return band;
            }

            var first = true;
            foreach (var value in card.Face.SecondaryValues)
            {
                var label = HudElements.Text(value.Label, 11.5f, ink, FontStyle.Bold);
                // Milestone 8.30d: the chip is named by its typed channel so the shine treatment
                // can light exactly the value a Defense or Movement offer is about.
                label.name = value.Channel == PrintedValueChannel.Defense
                    ? "gd-card-secondary-defense"
                    : value.Channel == PrintedValueChannel.Movement
                        ? "gd-card-secondary-movement"
                        : "gd-card-secondary-value";
                label.tooltip = value.AssistiveLabel;
                if (!first)
                {
                    label.style.marginLeft = 16f;
                }

                first = false;
                band.Add(label);
            }

            return band;
        }

        /// <summary>
        /// The primary-action band: what the card's primary action is, with its own numeric value
        /// attached to it rather than pooled with the secondary ones.
        ///
        /// Both come from typed content - <see cref="TrayCardFaceViewModel"/> owns that reading - so the
        /// band never says anything about a card that its definition does not.
        /// </summary>
        private static VisualElement BuildPrimaryActionBand(TrayCardViewModel card, Color ink)
        {
            var band = new VisualElement { name = "gd-card-primary-action" };
            band.style.flexDirection = FlexDirection.Row;
            band.style.alignItems = Align.Center;
            band.style.minHeight = PrimaryBandMinimumHeight;
            band.style.backgroundColor = MatchHudTheme.FaceFlag(card.Color);
            band.style.paddingLeft = 6f;
            band.style.paddingRight = 6f;
            band.style.paddingTop = 6f;
            band.style.paddingBottom = 6f;
            band.style.borderTopWidth = 1f;
            band.style.borderTopColor = new Color(ink.r, ink.g, ink.b, 0.30f);

            var value = card.Face.PrimaryValue;
            if (value != null)
            {
                var plate = new VisualElement { name = "gd-card-primary-value" };
                plate.style.flexDirection = FlexDirection.Row;
                plate.style.alignItems = Align.Center;
                plate.style.justifyContent = Justify.Center;
                plate.style.flexShrink = 0f;
                plate.style.minWidth = 44f;
                plate.style.paddingLeft = 5f;
                plate.style.paddingRight = 5f;
                plate.style.paddingTop = 1f;
                plate.style.paddingBottom = 1f;
                plate.style.backgroundColor = MatchHudTheme.Face(card.Color);
                HudElements.Border(plate, new Color(ink.r, ink.g, ink.b, 0.45f), 2f, 4f);
                plate.Add(HudElements.Text(value.Token, 8.5f, ink, FontStyle.Bold));
                var amount = HudElements.Text(value.AmountLabel, 18f, ink, FontStyle.Bold);
                amount.style.marginLeft = 3f;
                plate.Add(amount);
                plate.tooltip = value.AssistiveLabel;
                band.Add(plate);
            }

            var kind = HudElements.Text(card.Face.PrimaryActionLabel, 11f, ink, FontStyle.Bold);
            kind.name = "gd-card-primary-kind";
            kind.style.flexGrow = 1f;
            kind.style.flexShrink = 1f;
            kind.style.marginLeft = value == null ? 0f : 6f;
            kind.style.unityTextAlign = TextAnchor.MiddleCenter;
            band.Add(kind);
            return band;
        }

        /// <summary>
        /// The rules panel: the exact words the source face prints, on the one light surface every card
        /// shares, with p.9's Range or Radius icon on a badge at its lower left.
        ///
        /// The words are never colour-coded - a printed card's are not either - and a face that prints
        /// none says so in muted type rather than being given copy it does not have.
        /// </summary>
        private static VisualElement BuildRulesPanel(TrayCardViewModel card, Color ink)
        {
            var panel = new VisualElement { name = "gd-card-rules" };
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.flexGrow = 1f;
            panel.style.backgroundColor = MatchHudTheme.RulesPanelBackground;
            panel.style.marginLeft = 7f;
            panel.style.marginRight = 7f;
            panel.style.marginTop = 7f;
            panel.style.marginBottom = 7f;
            HudElements.Padding(panel, 9f);
            HudElements.Border(panel, new Color(ink.r, ink.g, ink.b, 0.28f), 1f, 4f);

            var transcribed = card.Face.PrintedTextKind == CardSourceContentKind.Transcribed;
            var words = HudElements.Text(
                transcribed ? card.Face.PrintedText : card.Face.EmptyTextLabel,
                transcribed ? 11.5f : 10f,
                transcribed ? MatchHudTheme.RulesPanelText : MatchHudTheme.RulesPanelMutedText,
                transcribed ? FontStyle.Normal : FontStyle.Italic);
            words.name = "gd-card-printed-text";
            words.style.flexGrow = 1f;
            words.style.flexShrink = 1f;
            // Centred in the panel the way a printed card sets its rules text, rather than pinned to
            // the top with the leftover space below it.
            words.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(words);

            var spatial = card.Face.SpatialValue;
            if (spatial != null)
            {
                var accent = MatchHudTheme.RulesPanelAccent(card.Color);
                var badge = new VisualElement { name = "gd-card-spatial-value" };
                badge.style.flexDirection = FlexDirection.Row;
                badge.style.alignItems = Align.Center;
                badge.style.alignSelf = Align.FlexStart;
                badge.style.flexShrink = 0f;
                badge.style.marginTop = 7f;
                badge.style.paddingLeft = 5f;
                badge.style.paddingRight = 8f;
                badge.style.paddingTop = 2f;
                badge.style.paddingBottom = 2f;
                badge.style.backgroundColor = accent;
                HudElements.Border(badge, accent, 1f, 9f);
                badge.Add(BuildSpatialMark(spatial.Channel));
                var label = HudElements.Text(
                    spatial.Label, 9.5f, MatchHudTheme.RulesPanelBackground, FontStyle.Bold);
                label.style.marginLeft = 5f;
                badge.Add(label);
                badge.tooltip = spatial.AssistiveLabel;
                panel.Add(badge);
            }

            return panel;
        }

        /// <summary>
        /// The mark the p.9 icon badge wears: a ring for a Radius, which is measured out from the actor
        /// in every direction, and a solid disc for a Range, which is a reach.
        ///
        /// It is drawn from a rounded element rather than taken from the font, for the reason
        /// <see cref="MatchHudTheme.ChannelToken"/> records about tokens: a glyph borrowed from whatever
        /// font a platform resolves is a different picture on every platform. The three-letter token is
        /// beside it either way, so nothing here is the only thing saying which icon this is.
        /// </summary>
        private static VisualElement BuildSpatialMark(PrintedValueChannel channel)
        {
            var mark = new VisualElement { name = "gd-card-spatial-mark" };
            mark.style.width = 11f;
            mark.style.height = 11f;
            mark.style.flexShrink = 0f;
            var light = MatchHudTheme.RulesPanelBackground;
            HudElements.Border(mark, light, channel == PrintedValueChannel.Radius ? 2f : 5.5f, 5.5f);
            mark.style.backgroundColor = new Color(light.r, light.g, light.b, 0f);
            return mark;
        }

        /// <summary>Where the card is in this hero's round: `PLAYED Â· TURN n`, `AVAILABLE`, `DISCARDED`.</summary>
        private static VisualElement BuildHistoryBand(TrayCardViewModel card, Color ink)
        {
            var band = new VisualElement { name = "gd-card-history" };
            band.style.flexDirection = FlexDirection.Row;
            band.style.alignItems = Align.Center;
            band.style.justifyContent = Justify.Center;
            band.style.minHeight = HistoryBandMinimumHeight;
            band.style.backgroundColor = MatchHudTheme.FaceFlag(card.Color);
            band.style.paddingTop = 5f;
            band.style.paddingBottom = 5f;
            band.style.paddingLeft = 6f;
            band.style.paddingRight = 6f;
            band.style.borderTopWidth = 1f;
            band.style.borderTopColor = new Color(ink.r, ink.g, ink.b, 0.30f);
            var label = HudElements.Text(card.HistoryLabel, 11f, ink, FontStyle.Bold);
            label.style.flexShrink = 1f;
            band.Add(label);

            // Milestone 8.5b: the word beside the outline. The outline alone would be a meaning carried
            // by colour, and the two states can stand together - a played card can still be sourcing a
            // live effect - so `ACTIVE` sits beside the lifecycle label rather than replacing it.
            if (card.IsActiveEffectSource)
            {
                var active = new VisualElement { name = "gd-card-active" };
                active.style.flexDirection = FlexDirection.Row;
                active.style.alignItems = Align.Center;
                active.style.flexShrink = 0f;
                active.style.marginLeft = 7f;
                active.style.paddingLeft = 5f;
                active.style.paddingRight = 5f;
                active.style.paddingTop = 1f;
                active.style.paddingBottom = 1f;
                active.style.backgroundColor = MatchHudTheme.ActiveEffectAccent;
                HudElements.Border(active, MatchHudTheme.ActiveEffectAccent, 1f, 3f);
                active.Add(HudElements.Text(
                    card.ActiveLabel, 9.5f, new Color(0.09f, 0.094f, 0.102f, 1f), FontStyle.Bold));
                active.tooltip = "An Active effect this card created is still in effect";
                band.Add(active);
            }

            return band;
        }
    }

    /// <summary>
    /// The one shared contextual region, and the tray's own action row.
    ///
    /// Every decision the match can ask is drawn here: commitment Initiative choices, the selected card's
    /// action offers, Defense responses, effect modes and targets, Clear, Hero Respawn, and - milestone
    /// 7.8a - a paused system transition. They share one region because the match asks exactly one thing
    /// at a time, and because a second region would be a second place a player has to look for the
    /// question.
    /// </summary>
    public sealed class RoundActionUnityView : IRoundActionView
    {
        public const float OptionalStepActionWidth = 240f;
        public const float OptionalStepActionHeight = 36f;
        public const float OptionalStepActionGap = 44f;

        private const double PreviewDismissDelaySeconds = 0.12d;

        /// <summary>
        /// How wide every transition option is. Without one width the longest label sets the width of
        /// its own row, and p.15's options carry two card names each, so six of them came out as three
        /// ragged rows that did not line up with each other.
        ///
        /// Wide enough that a two-card label stays on one line: at 336 every option wrapped, which made
        /// each one half as tall again and pushed six of them past the tray's height bound into a
        /// scrollbar. Two of these plus the gutter still fit the tray's width.
        /// </summary>
        private const float OptionWidth = 430f;

        /// <summary>
        /// The gap between p.15's two candidates for one source card. Milestone 8.30j removes the
        /// option and pair frames, so the pair width is now only two standard card faces and this gap.
        /// </summary>
        private const float UpgradePairGap = 4f;
        public const float UpgradePairWidth =
            (CardFootprint.Width * 2f) + UpgradePairGap +
            (CardFootprint.ExteriorOutlineWidth * 4f);

        /// <summary>
        /// The printed item sits at the centre of the card's bottom band. The projected sibling item
        /// covers only that conflicting symbol and remains wholly inside the standard face.
        /// </summary>
        private const float UpgradeItemOverlayHeight = 46f;
        private const float UpgradeItemOverlayTop =
            CardFootprint.Height - UpgradeItemOverlayHeight;
        private static readonly Color UpgradeItemOverlayBackground =
            new Color(0.216f, 0.216f, 0.216f, 1f);

        /// <summary>
        /// The legacy framed footprint retained by other card-valued choice families. Defense itself
        /// became frameless during milestone 8.30i and uses <see cref="CardFootprint.Width"/> directly.
        /// </summary>
        private const float DefenseCardInset = 6f;
        public const float DefenseCardFootprint =
            CardFootprint.Width + (DefenseCardInset * 2f);

        public static readonly Color DefensePreviewBackgroundColor =
            new Color(0.025f, 0.035f, 0.045f, 1f);

        private readonly VisualElement status;
        private readonly VisualElement rows;
        private readonly VisualElement preview;
        private readonly VisualElement declineDefenseHost;
        private readonly VisualElement optionalStepActionsHost;
        private readonly VisualElement defenseCardHoverPreview;
        private readonly VisualElement defenseDragProxy;
        private readonly CardCommitmentDragState defenseDragState = new CardCommitmentDragState();
        private VisualElement defenseDragCapture;
        private VisualElement draggedDefenseCard;
        private Func<Vector2, bool> isMapDropPoint;

        /// <summary>
        /// End Turn's own surface. Milestone 8.30b's user-approved amendment took the control out of
        /// the shared action row and gave it a larger control of its own. Milestone 8.30l moves that
        /// control into the status bar's top-right slot; the renderer moves this element out of the contextual region onto the panel and
        /// places it, exactly as it already does with this view's Defense preview.
        ///
        /// <para>It stays a child of <see cref="Root"/> until something moves it, so a fixture that
        /// renders this view alone still finds the control where every other one it draws is.</para>
        /// </summary>
        private readonly VisualElement endTurnHost;

        /// <summary>
        /// The Defense-card controls this rebuild drew. Milestone 8.11b, for milestone 8.8a's reason:
        /// a driver with no pointer has to reach an offer the way its own control does, and an offer
        /// this rebuild did not draw must reach nothing.
        /// </summary>
        private readonly List<KeyValuePair<TypedInvocationViewModel, VisualElement>>
            defenseCardControls =
                new List<KeyValuePair<TypedInvocationViewModel, VisualElement>>();

        /// <summary>
        /// The upgrade-option controls this rebuild drew, in their rendered and focus order. The list
        /// refuses stale option forwarding and lets pointer or keyboard focus strengthen exactly one
        /// centred item box without painting an enclosing option frame.
        /// </summary>
        private readonly List<KeyValuePair<SystemTransitionOptionViewModel, VisualElement>>
            upgradeOptionControls =
                new List<KeyValuePair<SystemTransitionOptionViewModel, VisualElement>>();

        private DefenseBreakdownViewModel activeDefensePreview;
        private Rect defensePreviewAnchor;
        private double dismissPreviewAt = -1d;

        public RoundActionUnityView()
        {
            Root = new VisualElement { name = "gd-context" };
            Root.style.flexDirection = FlexDirection.Column;
            // Milestone 8.30b: the narration is named, because the renderer now moves it into the top
            // status bar and a check has to be able to say where it ended up.
            status = HudElements.Row(8f);
            status.name = "gd-narration";
            status.style.marginBottom = 6f;
            rows = HudElements.Column(6f);
            endTurnHost = new VisualElement { name = "gd-end-turn" };
            endTurnHost.style.flexDirection = FlexDirection.Column;
            endTurnHost.style.alignItems = Align.Stretch;
            endTurnHost.style.justifyContent = Justify.Center;
            endTurnHost.style.display = DisplayStyle.None;
            Root.Add(status);
            Root.Add(rows);
            Root.Add(endTurnHost);
            preview = HudElements.Panel(DefensePreviewBackgroundColor);
            preview.name = "gd-defense-preview";
            preview.style.position = Position.Absolute;
            preview.style.display = DisplayStyle.None;
            preview.pickingMode = PickingMode.Ignore;
            HudElements.Padding(preview, 10f);
            HudElements.Border(preview, MatchHudTheme.PanelBorder, 1f, 4f);
            declineDefenseHost = new VisualElement { name = "gd-decline-defense-host" };
            declineDefenseHost.style.flexDirection = FlexDirection.Column;
            declineDefenseHost.style.alignItems = Align.Stretch;
            declineDefenseHost.style.justifyContent = Justify.FlexStart;
            declineDefenseHost.style.display = DisplayStyle.None;
            Root.Add(declineDefenseHost);
            optionalStepActionsHost = new VisualElement { name = "gd-optional-step-actions" };
            optionalStepActionsHost.pickingMode = PickingMode.Ignore;
            optionalStepActionsHost.style.flexDirection = FlexDirection.Row;
            optionalStepActionsHost.style.alignItems = Align.Stretch;
            optionalStepActionsHost.style.justifyContent = Justify.FlexStart;
            optionalStepActionsHost.style.overflow = Overflow.Visible;
            optionalStepActionsHost.style.display = DisplayStyle.None;
            Root.Add(optionalStepActionsHost);
            defenseCardHoverPreview = new VisualElement { name = "gd-defense-card-hover-preview" };
            defenseCardHoverPreview.style.position = Position.Absolute;
            defenseCardHoverPreview.style.display = DisplayStyle.None;
            defenseCardHoverPreview.pickingMode = PickingMode.Ignore;
            defenseDragProxy = new VisualElement { name = "gd-defense-card-drag-proxy" };
            defenseDragProxy.style.position = Position.Absolute;
            defenseDragProxy.style.display = DisplayStyle.None;
            defenseDragProxy.style.width = CardFootprint.Width;
            defenseDragProxy.style.height = CardFootprint.Height;
            defenseDragProxy.pickingMode = PickingMode.Ignore;
        }

        public VisualElement Root { get; }

        /// <summary>The Defense breakdown popup, placed on the HUD root so it can leave the tray.</summary>
        public VisualElement PreviewRoot => preview;

        /// <summary>The Defense decline banner hosted at the facedown confirmation rectangle.</summary>
        public VisualElement DeclineDefenseRoot => declineDefenseHost;

        /// <summary>The two optional-step answers hosted above the retained ordinary hand.</summary>
        public VisualElement OptionalStepActionsRoot => optionalStepActionsHost;

        /// <summary>The ordinary hand-style complete face raised from an overlapped Defense card.</summary>
        public VisualElement DefenseCardHoverPreviewRoot => defenseCardHoverPreview;

        /// <summary>The pointer-transparent complete Defense card reparented to the HUD root.</summary>
        public VisualElement DefenseDragProxyRoot => defenseDragProxy;

        /// <summary>Uses the commitment drag's exact live board boundary for Defense-card drops.</summary>
        public void SetMapDropPredicate(Func<Vector2, bool> predicate)
        {
            isMapDropPoint = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <summary>
        /// The narration element, so the renderer can host it in the top status bar. Milestone 8.30b.
        /// </summary>
        public VisualElement NarrationRoot => status;

        /// <summary>
        /// End Turn's own element, so the renderer can place it in the status bar's top-right slot.
        /// Milestones 8.30b and 8.30l. It is hidden whenever the projection offers no End Turn, which is every
        /// state in which the turn authority would refuse one.
        /// </summary>
        public VisualElement EndTurnRoot => endTurnHost;

        /// <summary>
        /// Whether the drawn End Turn banner is in its full-shine state - ending the turn is the
        /// only offer left. Milestone 8.30d, exposed as a fact a test can read rather than a colour
        /// only a screenshot carries; false whenever the control is hidden.
        /// </summary>
        public bool EndTurnShinesFully { get; private set; }

        public RoundActionViewModel Model { get; private set; }
        public DefenseBreakdownViewModel ActiveDefensePreview => activeDefensePreview;
        public Rect DefensePreviewAnchor => defensePreviewAnchor;

        public event Action<TypedInvocationViewModel> InvocationSubmitted;
        public event Action<FastTravelOfferViewModel> FastTravelSelected;

        /// <summary>Milestone 6.5c: the named target control that was activated.</summary>
        public event Action<EffectChoiceTargetViewModel> TargetSubmitted;

        /// <summary>
        /// The target control the pointer or keyboard focus is on, or null when none is. The board
        /// reads this to mark that object and light where the projected operation would leave it.
        /// </summary>
        public event Action<EffectChoiceTargetViewModel> TargetHighlighted;
        public event Action<HeroRespawnOptionViewModel> RespawnHighlighted;

        /// <summary>Milestone 6.3a: the Clear replacement whose control was activated.</summary>
        public event Action<ClearOfferViewModel> ClearSelected;

        public event Action ClearConfirmed;
        public event Action ClearCancelled;

        public EffectChoiceTargetViewModel HighlightedTarget { get; private set; }
        public HeroRespawnOptionViewModel HighlightedRespawn { get; private set; }

        /// <summary>
        /// The upgrade option the pointer or keyboard focus is on, or null when none is.
        ///
        /// Milestone 8.8a. It is view-local and drives nothing outside this region - p.15's choice is
        /// about two cards rather than about a space, so there is no board cue to raise - and it exists
        /// so the selection a player sees on the card is a fact a test can read rather than a colour
        /// only a screenshot carries.
        /// </summary>
        public SystemTransitionOptionViewModel HighlightedUpgradeOption { get; private set; }

        /// <summary>
        /// Whether the current model carries a decision that must be answered before anything else can
        /// happen. The tray is hidden while one stands, which is how "at most one usable interaction
        /// context" is kept structurally rather than by discipline.
        /// </summary>
        /// <summary>
        /// Whether the region is drawing p.15's six card-faced upgrade choices. The renderer uses this
        /// to select the contained six-card overlay and short-view vertical fallback while keeping
        /// every standing HUD and board-safe rectangle on its ordinary geometry.
        /// </summary>
        public bool HasUpgradeComparisonBand =>
            Model?.SystemTransitionPrompt != null &&
            Model.SystemTransitionPrompt.Options.All(option => option.Card != null);

        public bool HasOptionalStepDecision =>
            Model?.EffectChoicePrompt?.Subject == EffectChoiceSubject.OptionalStep &&
            Model.EffectChoices.Count > 0;

        public bool HasExclusiveDecision =>
            Model != null &&
            (Model.SystemTransitionPrompt != null ||
             Model.RemovalPreventionPrompt != null ||
             Model.DefenseChoices.Count > 0 ||
             Model.InitiativeChoices.Count > 0 ||
             Model.EffectChoices.Count > 0 ||
             Model.EffectChoiceTargets.Count > 0 ||
             Model.UnavailableEffectChoices.Count > 0 ||
             Model.HeroRespawnOptions.Count > 0 ||
             Model.ResolveWithoutRespawn != null ||
             Model.ClearSelection != null);

        public void Render(RoundActionViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            if (activeDefensePreview != null &&
                (activeDefensePreview.Version != Model.Version ||
                 !Model.DefenseChoices.Any(value =>
                     ReferenceEquals(value.DefenseBreakdown, activeDefensePreview))))
            {
                ClearDefensePreview();
            }

            // A highlight belongs to the model it was read from. A rebuilt model drops it, so the
            // board never keeps a cue for a control that is no longer offered.
            if (HighlightedTarget != null &&
                !Model.EffectChoiceTargets.Any(value => ReferenceEquals(value, HighlightedTarget)))
            {
                SetTargetHighlight(null);
            }

            if (HighlightedRespawn != null &&
                !Model.HeroRespawnOptions.Any(value => ReferenceEquals(value, HighlightedRespawn)))
            {
                SetRespawnHighlight(null);
            }

            Rebuild();
        }

        /// <summary>Forwards one named target control, for a driver that has no pointer.</summary>
        public void ForwardTarget(EffectChoiceTargetViewModel target)
        {
            if (target != null)
            {
                TargetSubmitted?.Invoke(target);
            }
        }

        /// <summary>Reports a pointed-at target directly, for a driver that has no pointer.</summary>
        public void ForwardTargetHighlight(EffectChoiceTargetViewModel target) =>
            SetTargetHighlight(target);

        /// <summary>Reports a hovered or keyboard-focused respawn control without a pointer driver.</summary>
        public void ForwardRespawnHighlight(HeroRespawnOptionViewModel option) =>
            SetRespawnHighlight(option);

        public void ForwardInvocation(TypedInvocationViewModel invocation)
        {
            if (invocation != null)
            {
                InvocationSubmitted?.Invoke(invocation);
            }
        }

        public void ForwardFastTravelSelection(FastTravelOfferViewModel selection)
        {
            if (selection != null)
            {
                FastTravelSelected?.Invoke(selection);
            }
        }

        private void SetTargetHighlight(EffectChoiceTargetViewModel target)
        {
            if (ReferenceEquals(HighlightedTarget, target))
            {
                return;
            }

            HighlightedTarget = target;
            TargetHighlighted?.Invoke(target);
        }

        private void SetRespawnHighlight(HeroRespawnOptionViewModel option)
        {
            if (ReferenceEquals(HighlightedRespawn, option))
            {
                return;
            }

            HighlightedRespawn = option;
            RespawnHighlighted?.Invoke(option);
        }

        private void Rebuild()
        {
            CancelDefenseCardDrag();
            ClearDefenseCardPeekPreview();
            declineDefenseHost.Clear();
            declineDefenseHost.style.display = DisplayStyle.None;
            optionalStepActionsHost.Clear();
            optionalStepActionsHost.style.display = DisplayStyle.None;
            status.Clear();
            rows.Clear();
            // Milestone 8.30b: End Turn is rebuilt before the branches below, because four of them
            // return early and the control is no longer inside the region any of them draw. Every one
            // of those four is a state the turn authority refuses an End Turn in, so the model carries
            // none and this leaves the corner empty rather than leaving a stale control standing.
            RebuildEndTurn();
            // Milestone 8.8a: the controls a rebuild is about to discard, and the selection that belonged
            // to them. A highlight belongs to the model it was read from, exactly as every other one here
            // does.
            upgradeOptionControls.Clear();
            defenseCardControls.Clear();
            HighlightedUpgradeOption = null;
            // Milestone 8.30d: the narration speaks in two voices. When the sentence is addressed
            // to the viewing seat - the model carries something to act on - the lane lights warm
            // with the shared shine accent; every other state stays calm and quiet. The word is
            // never carried by colour alone: the sentence itself says who acts.
            var warm = Model.Voice == NarrationVoice.YourMove;
            status.style.backgroundColor = warm
                ? MatchHudTheme.NarrationWarmBackground
                : Color.clear;
            var accent = new VisualElement { name = "gd-narration-accent" };
            accent.style.width = 5f;
            accent.style.alignSelf = Align.Stretch;
            accent.style.flexShrink = 0f;
            accent.style.backgroundColor = MatchHudTheme.ShineGlow;
            accent.style.display = warm ? DisplayStyle.Flex : DisplayStyle.None;
            status.Add(accent);
            var narration = HudElements.Text(
                Model.Status,
                warm ? 14f : 12f,
                warm ? MatchHudTheme.NarrationWarmText : MatchHudTheme.MutedText,
                warm ? FontStyle.Bold : FontStyle.Normal);
            narration.name = "gd-narration-text";
            narration.style.marginLeft = warm ? 8f : 0f;
            status.Add(narration);

            // Milestone 7.8a: a paused system transition is the whole of the region while it stands.
            // Nothing else the model carries can be acted on - the authority refuses every other
            // command - so drawing anything beside it would be drawing refusals.
            if (Model.SystemTransitionPrompt != null)
            {
                BuildTransitionPrompt(Model.SystemTransitionPrompt);
                return;
            }

            if (Model.ClearSelection != null)
            {
                BuildClearSelection(Model.ClearSelection);
                return;
            }

            if (Model.DefenseChoices.Count > 0)
            {
                BuildDefense();
                HideEmptySections();
                return;
            }

            // Milestone 8.21: a standing removal prevention is the whole of the region while it
            // stands, for the reason a paused system transition is. The Defeat it stopped has already
            // recorded its outcome and paid its reward; nothing else the model carries can be acted on
            // until the removal stage is answered, so drawing anything beside it would be drawing
            // refusals - and rulebook p.13's own ordering is why the tray is hidden with it.
            if (Model.RemovalPreventionPrompt != null)
            {
                BuildRemovalPrevention(Model.RemovalPreventionPrompt);
                HideEmptySections();
                return;
            }

            BuildActionRow();
            HideEmptySections();
        }

        /// <summary>
        /// The standing removal-prevention decision, under a gutter that names what the price buys.
        ///
        /// <para>Milestone 8.21, and it is milestone 8.15a's direction applied to a second family:
        /// each payment is the whole printed face of the card that would pay, drawn by the tray's own
        /// standalone builder at the one fixed width, with the card's name kept as the control's hover
        /// and assistive reading. The decline is about no card and keeps its labelled control.</para>
        ///
        /// <para>The minion is not named here. It is cued on the board where it is standing, which is
        /// the one place a player can see which miniature the price would keep.</para>
        /// </summary>
        private void BuildRemovalPrevention(RemovalPreventionPromptViewModel prompt)
        {
            var section = Section(prompt.SectionLabel);
            foreach (var payment in prompt.Payments)
            {
                // The same branch the effect-choice section takes, and for the same reason: a choice
                // that is about a card is drawn as the card, and one carrying no face keeps its
                // labelled control rather than being drawn as an empty frame. A projection always
                // supplies the face, so the second arm is what a surface composed without one gets.
                if (payment.Card != null)
                {
                    section.Add(BuildEffectCardChoice(payment));
                }
                else
                {
                    AddInvocation(section, payment);
                }
            }

            AddInvocation(section, prompt.Decline);
        }

        /// <summary>
        /// A section whose gutter names a family with nothing in it is not drawn. During card
        /// commitment the match offers no action at all, and an `ACTIONS` label over an empty row says
        /// only that this surface expected something it did not get.
        /// </summary>
        private void HideEmptySections()
        {
            foreach (var section in rows.Children())
            {
                var contentCount = section.name == "gd-action-section" ||
                                   section.name == "gd-defense-section"
                    ? section.childCount
                    : section.childCount - 1;
                section.style.display = contentCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>The gutter every section's family label reserves down this region's left edge.</summary>
        public const float SectionGutterWidth = 96f;

        /// <summary>The space between two controls in one section.</summary>
        public const float SectionGap = 6f;

        /// <summary>
        /// The tighter spacing used only by the unlabelled standing action row. Five ordinary offers
        /// are the widest routine set; spending less chrome between them keeps End Turn on that same
        /// line without abbreviating any rule-facing label.
        /// </summary>
        public const float ActionControlGap = 3f;

        public const float ActionControlHorizontalPadding = 6f;

        public const float ActionControlFontSize = 11f;

        private VisualElement Section(string gutter)
        {
            var section = HudElements.Row(SectionGap);
            var label = HudElements.Text(gutter, 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            label.style.width = SectionGutterWidth;
            section.Add(label);
            rows.Add(section);
            return section;
        }

        private void BuildTransitionPrompt(SystemTransitionPromptViewModel prompt)
        {
            status.Add(HudElements.Text(prompt.PromptLabel, 12f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            if (prompt.StatusLabel.Length > 0)
            {
                status.Add(HudElements.Text(prompt.StatusLabel, 11f, MatchHudTheme.MutedText));
            }

            // Milestone 8.16c: p.8's blocked-Spawn-point Place is answered by clicking one of the
            // standing highlighted spaces. The contextual region still explains who chooses and why,
            // but drawing a PLACE AT control here would restore the duplicate input surface this
            // milestone closes. Upgrade a Card continues below as card-faced controls.
            if (prompt.Kind == GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind
                    .LanePushRespawnPlace)
            {
                return;
            }

            // Milestone 8.29b: Level Up uses the whole width between the standing side rails. Its
            // family label joins the prompt line instead of consuming the ordinary 96-unit section
            // gutter, leaving six unchanged CardFootprints enough room in one comparison row.
            if (prompt.Options.All(option => option.Card != null))
            {
                status.Add(HudElements.Text(
                    prompt.SectionLabel, 10f, MatchHudTheme.MutedText, FontStyle.Bold));
                var upgradeGrid = HudElements.Column(6f);
                upgradeGrid.style.flexGrow = 1f;
                rows.Add(upgradeGrid);
                BuildUpgradeCardRow(upgradeGrid, prompt);
                return;
            }

            var section = Section(prompt.SectionLabel);
            var grid = HudElements.Column(6f);
            grid.style.flexGrow = 1f;
            section.Add(grid);

            // One row per card the options are about, which p.15's own option order already groups them
            // by: the page asks which card to upgrade and which of its two next-Tier cards to take, so
            // the answers for one card arrive together. Breaking the line where that card changes is a
            // line break, not a sort - the options keep the order the pause offered them, and every row
            // ends up one colour wide because the two cards p.15 finds share the chosen card's colour.
            VisualElement row = null;
            var group = default(CardInstanceId);
            foreach (var option in prompt.Options)
            {
                if (row == null || !option.SourceCardInstanceId.Equals(group))
                {
                    row = HudElements.Row(8f);
                    row.style.alignItems = Align.Stretch;
                    row.style.justifyContent = Justify.FlexStart;
                    grid.Add(row);
                    group = option.SourceCardInstanceId;
                }

                row.Add(BuildTransitionOption(option));
            }
        }

        /// <summary>
        /// p.15's six complete candidate cards in the stable Blue, Green, Red reading order.
        ///
        /// <para>The groups are the pairs the pause itself supplied - options for one
        /// upgraded card arrive together, so grouping them is reading the projection rather than sorting
        /// it - and the candidates inside a pair keep the order the pause offered them.</para>
        ///
        /// <para>Presentation decides only the left-to-right order of the pairs, and takes
        /// it from <see cref="MatchHudTheme.ColorReadingOrder"/> rather than from the pause. The pause
        /// orders its pairs by card instance identity, which puts the same three colours in a different
        /// order for every hero and in no order a player could learn; the colours are the one thing about
        /// this decision that is the same every time it is asked, so they are what the columns are read
        /// by. It reorders pairs and nothing else: no option is added, removed, or moved out of its own
        /// pair.</para>
        /// </summary>
        private void BuildUpgradeCardRow(
            VisualElement grid, SystemTransitionPromptViewModel prompt)
        {
            var columns = new List<List<SystemTransitionOptionViewModel>>();
            foreach (var option in prompt.Options)
            {
                var column = columns.FirstOrDefault(candidate =>
                    candidate[0].SourceCardInstanceId.Equals(option.SourceCardInstanceId));
                if (column == null)
                {
                    column = new List<SystemTransitionOptionViewModel>();
                    columns.Add(column);
                }

                column.Add(option);
            }

            // A stable order, so two pairs of one colour - which p.15's "1 card of each color" makes
            // unreachable, but which nothing in this method needs to assume - keep the pause's order.
            var ordered = columns
                .OrderBy(column => MatchHudTheme.ColorReadingRank(column[0].Color))
                .ToArray();
            var band = HudElements.Row(DirectorHandsUnityView.CardGap);
            band.name = "gd-upgrade-comparison-band";
            band.style.alignItems = Align.FlexStart;
            band.style.justifyContent = Justify.FlexStart;
            band.style.flexWrap = Wrap.NoWrap;
            band.style.backgroundColor = Color.clear;
            grid.Add(band);
            foreach (var column in ordered)
            {
                // Pairing remains the authority's grouping and focus order, but 8.30j makes the
                // group a transparent layout fact: no caption, plate, border, or enclosing outline.
                var pair = HudElements.Row(UpgradePairGap);
                pair.name = "gd-upgrade-pair";
                pair.style.width = UpgradePairWidth;
                pair.style.minWidth = UpgradePairWidth;
                pair.style.maxWidth = UpgradePairWidth;
                pair.style.flexShrink = 0f;
                pair.style.alignItems = Align.FlexStart;
                pair.style.flexWrap = Wrap.NoWrap;
                pair.style.backgroundColor = Color.clear;
                foreach (var option in column)
                {
                    pair.Add(BuildUpgradeOption(option));
                }

                band.Add(pair);
            }
        }

        /// <summary>
        /// One upgrade option: the candidate's whole printed face with the fact that belongs to taking
        /// it centred over the face's bottom item position.
        ///
        /// <para>The face is <see cref="DirectorHandsUnityView.BuildStandaloneCard"/> - the tray's own
        /// builder - so this prompt introduces no second, abbreviated card design and cannot decode a
        /// printed sentence. p.16's projected item is the icon printed on the <em>other</em> candidate,
        /// so the compact backed reading covers this candidate's conflicting bottom item symbol and a
        /// player sees exactly which item this choice leaves them holding. The card returned to the deck
        /// remains named in the option's assistive reading.</para>
        ///
        /// <para>The whole control is the option. It paints no plate or outline; focus and pointer
        /// strengthen only the already-visible item box, so the face and every layout measurement stay
        /// unchanged.</para>
        /// </summary>
        private Button BuildUpgradeOption(SystemTransitionOptionViewModel option)
        {
            var control = new Button(() => ForwardUpgradeOption(option))
            {
                name = "gd-upgrade-option",
                text = string.Empty
            };
            control.style.flexDirection = FlexDirection.Column;
            control.style.alignItems = Align.Center;
            control.style.flexShrink = 0f;
            control.style.width = CardFootprint.Width;
            control.style.height = CardFootprint.Height;
            control.style.marginLeft = CardFootprint.ExteriorOutlineWidth;
            control.style.marginRight = CardFootprint.ExteriorOutlineWidth;
            control.style.marginTop = CardFootprint.ExteriorOutlineWidth;
            control.style.marginBottom = CardFootprint.ExteriorOutlineWidth;
            HudElements.Padding(control, 0f);
            HudElements.Border(control, Color.clear, 0f, 0f);
            control.style.backgroundColor = Color.clear;
            upgradeOptionControls.Add(
                new KeyValuePair<SystemTransitionOptionViewModel, VisualElement>(option, control));
            HudElements.OnPointedAt(control, pointedAt =>
                ForwardUpgradeOptionHighlight(pointedAt ? option : null));

            control.Add(DirectorHandsUnityView.BuildStandaloneCard(option.Card));
            control.Add(BuildUpgradeOptionFacts(option));
            control.tooltip = option.Detail.Length > 0
                ? $"{option.Label} - {option.Detail}"
                : option.Label;
            return control;
        }

        /// <summary>
        /// What taking this option does, over the candidate's bottom printed item position: p.16's item,
        /// which is the icon printed on the candidate this option does <em>not</em> take.
        /// </summary>
        private static VisualElement BuildUpgradeOptionFacts(SystemTransitionOptionViewModel option)
        {
            var facts = HudElements.Column(3f);
            facts.name = "gd-upgrade-facts";
            facts.style.position = Position.Absolute;
            facts.style.left = 0f;
            facts.style.top = UpgradeItemOverlayTop;
            facts.style.marginTop = 0f;
            facts.style.width = CardFootprint.Width;
            facts.style.alignItems = Align.Center;
            facts.style.opacity = 1f;
            facts.pickingMode = PickingMode.Ignore;

            var itemBox = HudElements.Row(3f);
            itemBox.name = "gd-upgrade-item";
            itemBox.style.flexWrap = Wrap.NoWrap;
            itemBox.style.minWidth = 86f;
            itemBox.style.height = UpgradeItemOverlayHeight;
            itemBox.style.minHeight = UpgradeItemOverlayHeight;
            itemBox.style.maxHeight = UpgradeItemOverlayHeight;
            itemBox.style.justifyContent = Justify.Center;
            itemBox.style.alignItems = Align.Center;
            itemBox.style.paddingLeft = 7f;
            itemBox.style.paddingRight = 7f;
            itemBox.style.paddingTop = 3f;
            itemBox.style.paddingBottom = 3f;
            itemBox.style.backgroundColor = UpgradeItemOverlayBackground;
            itemBox.pickingMode = PickingMode.Ignore;
            HudElements.Border(itemBox, UpgradeItemOverlayBackground, 2f, 3f);
            if (option.ItemIcon == PrintedValueChannel.None)
            {
                // Two absences p.15 and p.16 produce between them - a deck with no second candidate, and
                // a dashboard with no slot for the icon that one prints - and `Detail` already states
                // which of the two this is. The chip is not drawn at all rather than drawn empty.
                itemBox.Add(HudElements.Text(
                    "NO ITEM", 10f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            }
            else
            {
                itemBox.Add(HudElements.PrintedChannelSymbol(
                    option.ItemIcon, 26f, MatchHudTheme.PrimaryText));
                var amount = HudElements.Text(
                    MatchHudTheme.Signed(1), 13f, MatchHudTheme.PrimaryText, FontStyle.Bold);
                amount.style.marginLeft = 3f;
                itemBox.Add(amount);
            }
            itemBox.tooltip = option.Detail;

            // The user's 2026-08-22 Game View decision: the item is the only one of the three facts
            // written under the card. Which card is set aside and which is returned to the deck are
            // both already on the screen - the sibling is the other card in this column, and the
            // returned card is the one whose colour the column is - so spelling them out six times
            // said what the grid says. Both stay in the option's hover and assistive reading, where
            // this HUD already keeps a fact a surface stops drawing.
            facts.Add(itemBox);
            return facts;
        }

        /// <summary>
        /// Activates one drawn upgrade option: the same forwarding its own control does, for a driver
        /// that has no pointer.
        ///
        /// Milestone 8.8a. It forwards the invocation the continuation itself projected and nothing
        /// composed here, and an option this rebuild did not draw forwards nothing at all - so a stale
        /// option cannot be answered through the surface any more than a forged command can be submitted
        /// through the presenter.
        /// </summary>
        /// <summary>
        /// Activates one drawn Defense card: the same forwarding its own control does, for a driver
        /// that has no pointer.
        ///
        /// Milestone 8.11b, on milestone 8.8a's terms. The card's <see cref="Button"/> calls this, so
        /// there is one path rather than two that could drift, and an offer this rebuild did not draw
        /// forwards nothing at all - a stale face cannot answer a question the authority has moved on
        /// from. What it forwards is the offer's own projected invocation, unchanged.
        /// </summary>
        public bool ForwardDefenseCard(TypedInvocationViewModel choice)
        {
            if (choice == null ||
                !defenseCardControls.Any(entry => ReferenceEquals(entry.Key, choice)))
            {
                return false;
            }

            ForwardInvocation(choice);
            return true;
        }

        public bool ForwardUpgradeOption(SystemTransitionOptionViewModel option)
        {
            if (option == null ||
                !upgradeOptionControls.Any(entry => ReferenceEquals(entry.Key, option)))
            {
                return false;
            }

            ForwardInvocation(new TypedInvocationViewModel(option.Label, option.Command));
            return true;
        }

        /// <summary>
        /// Marks one upgrade option as the pointed-at or focused one, and every other as not.
        ///
        /// Milestone 8.8a. It is public for the same reason every other highlight here is: a driver with
        /// no pointer - the build-smoke path, and every fixture that reaches this pause without one -
        /// has to be able to report the same thing a pointer reports. An option this prompt did not draw
        /// selects nothing, which is the same refusal <see cref="Submit"/> makes for a forged command.
        /// </summary>
        public void ForwardUpgradeOptionHighlight(SystemTransitionOptionViewModel option)
        {
            HighlightedUpgradeOption =
                option != null &&
                upgradeOptionControls.Any(entry => ReferenceEquals(entry.Key, option))
                    ? option
                    : null;
            foreach (var entry in upgradeOptionControls)
            {
                var facts = entry.Value.Children()
                    .FirstOrDefault(child => child.name == "gd-upgrade-facts");
                if (facts != null)
                {
                    var highlighted = ReferenceEquals(entry.Key, HighlightedUpgradeOption);
                    var item = facts.Children()
                        .FirstOrDefault(child => child.name == "gd-upgrade-item");
                    if (item != null)
                    {
                        item.style.backgroundColor = highlighted
                            ? Color.Lerp(UpgradeItemOverlayBackground, MatchHudTheme.ShineGlow, 0.16f)
                            : UpgradeItemOverlayBackground;
                        item.style.borderTopColor = highlighted
                            ? MatchHudTheme.ShineRim
                            : UpgradeItemOverlayBackground;
                        item.style.borderRightColor = item.style.borderTopColor;
                        item.style.borderBottomColor = item.style.borderTopColor;
                        item.style.borderLeftColor = item.style.borderTopColor;
                    }
                }
            }
        }

        private Button BuildTransitionOption(SystemTransitionOptionViewModel option)
        {
            var control = HudElements.Control(
                option.Label,
                () => ForwardInvocation(new TypedInvocationViewModel(option.Label, option.Command)),
                58f);
            control.style.flexDirection = FlexDirection.Column;
            control.style.alignItems = Align.FlexStart;
            control.style.justifyContent = Justify.SpaceBetween;

            // One width for every option, so the rows line up as a grid instead of each row being as
            // wide as its own longest pair of card names.
            control.style.width = OptionWidth;
            control.text = string.Empty;

            // An option about a card wears that card's own colour, exactly as the tray's cards do, so a
            // player matches the option to the card in their hand by the swatch rather than by reading
            // two names. p.15 puts the taken card at the same colour as the upgraded one, so one colour
            // is right for the whole option. The colour name is drawn beside it, because nothing in this
            // HUD means something by colour alone.
            var body = option.Color == CardColor.None
                ? MatchHudTheme.PrimaryText
                : MatchHudTheme.FaceText(option.Color);
            var muted = option.Color == CardColor.None
                ? MatchHudTheme.MutedText
                : new Color(body.r, body.g, body.b, 0.78f);
            if (option.Color != CardColor.None)
            {
                control.style.backgroundColor = MatchHudTheme.Face(option.Color);
                control.Add(HudElements.Text(option.ColorName, 9f, muted, FontStyle.Bold));
            }

            control.Add(HudElements.Text(option.Label, 12f, body, FontStyle.Bold));
            if (option.Detail.Length > 0)
            {
                control.Add(HudElements.Text(option.Detail, 10f, muted));
            }

            return control;
        }

        private void BuildClearSelection(ClearSelectionViewModel selection)
        {
            // Milestone 8.15a, at the user's direction: every offered token is a piece standing on
            // the board, so the board is where it is chosen - marked Eligible or Selected where it
            // stands and toggled by clicking it, exactly as the Minion Battle's candidates have been
            // since 8.11a. The controls that used to name them are gone; what stays is what has no
            // board surface: agreeing to the chosen subset, and closing the selection.
            var section = Section("CLEAR");
            var confirm = HudElements.Control(
                selection.Confirm.Label, () => ClearConfirmed?.Invoke());
            confirm.style.backgroundColor = MatchHudTheme.ConfirmAccent;
            confirm.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
            section.Add(confirm);
            section.Add(HudElements.Control(selection.CancelLabel, () => ClearCancelled?.Invoke()));
        }

        /// <summary>
        /// p.11's Defense response: every legal card as its own whole printed face, then the one
        /// control that is not a card.
        ///
        /// <para>Milestone 8.11b. A card was offered here as two lines of abbreviated text while the
        /// same cards stood on the tray as complete faces, so a defender was comparing a summary
        /// against the thing it summarised. The face is
        /// <see cref="DirectorHandsUnityView.BuildStandaloneCard"/> - the tray's own builder, and the
        /// same one p.15's upgrade prompt uses - so this region introduces no second card design and
        /// decodes no printed sentence.</para>
        ///
        /// <para>The authority-computed
        /// Attack/Defense/result preview is still what hover and keyboard focus raise, still read from
        /// the offer's own <see cref="DefenseBreakdownViewModel"/>, and the visible order is the
        /// projected response order, which is the keyboard order too because the controls are added in
        /// it. A card may be clicked or dragged onto the same live board surface used by facedown
        /// commitment; both paths forward that card's exact projected invocation once. The redundant
        /// DEFENSE gutter and card frames are absent, and <c>DECLINE DEFENSE</c> uses the same pewter
        /// banner language as <c>CONFIRM FACEDOWN</c>.</para>
        /// </summary>
        private void BuildDefense()
        {
            var section = HudElements.Row(SectionGap);
            section.name = "gd-defense-section";
            section.style.flexWrap = Wrap.NoWrap;
            section.style.alignItems = Align.FlexStart;
            section.style.flexGrow = 0f;
            section.style.flexShrink = 0f;
            section.style.width = ConfiguredMatchHudRenderer.CompactHandRightEdge;
            section.style.minWidth = ConfiguredMatchHudRenderer.CompactHandRightEdge;
            section.style.maxWidth = ConfiguredMatchHudRenderer.CompactHandRightEdge;
            section.style.marginTop = DirectorHandsUnityView.AvailableCardRaise;
            rows.Add(section);
            var cardChoices = Model.DefenseChoices.Where(choice => choice.Card != null).ToArray();
            for (var index = 0; index < cardChoices.Length; index++)
            {
                var choice = cardChoices[index];
                var control = BuildDefenseCard(choice);
                var slot = new VisualElement { name = "gd-defense-card-slot" };
                slot.style.flexDirection = FlexDirection.Column;
                slot.style.alignItems = Align.Stretch;
                slot.style.flexShrink = 0f;
                slot.style.marginRight = index == cardChoices.Length - 1
                    ? 0f
                    : -DirectorHandsUnityView.PeekCardOverlap;
                slot.style.top = -DirectorHandsUnityView.AvailableCardRaise;
                HudElements.Padding(slot, DirectorHandsUnityView.ActiveOutlineGap);
                HudElements.Border(slot, Color.clear,
                    DirectorHandsUnityView.ActiveOutlineWidth, 11f);
                slot.Add(control);
                BindDefenseCardDrag(control, slot, choice);
                HudElements.OnPointedAt(control, pointedAt =>
                    SetDefenseCardPeekPreview(pointedAt ? choice : null, slot));
                if (choice.DefenseBreakdown != null)
                {
                    HudElements.OnPointedAt(control, pointedAt =>
                    {
                        if (pointedAt)
                        {
                            ShowDefensePreview(
                                choice.DefenseBreakdown,
                                CalculateDefenseCardFanWorldBound(control.worldBound));
                        }
                        else
                        {
                            ScheduleDefensePreviewDismissal(Time.realtimeSinceStartupAsDouble);
                        }
                    });
                }

                section.Add(slot);
            }

            var declineChoice = Model.DefenseChoices.FirstOrDefault(choice => choice.Card == null);
            if (declineChoice != null)
            {
                var decline = new PewterBanner(
                    () => ForwardInvocation(declineChoice), true,
                    DirectorHandsUnityView.CornerBannerFontSize,
                    "gd-decline-defense-banner-label")
                {
                    name = "gd-decline-defense-control",
                    text = declineChoice.Label
                };
                decline.SetVisibleLabel(declineChoice.Label);
                decline.style.minHeight = DirectorHandsUnityView.CornerBannerHeight;
                decline.style.whiteSpace = WhiteSpace.Normal;
                declineDefenseHost.Add(decline);
                declineDefenseHost.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// The breakdown belongs to the complete set of available Defense cards, not to the one
        /// overlapped face currently under the pointer. Anchor it to the visible fan so its right
        /// edge is stable while the pointer crosses cards and the popup never sits over that hand.
        /// </summary>
        private Rect CalculateDefenseCardFanWorldBound(Rect fallback)
        {
            var hasBounds = false;
            var xMin = 0f;
            var yMin = 0f;
            var xMax = 0f;
            var yMax = 0f;
            foreach (var pair in defenseCardControls)
            {
                var bounds = pair.Value.worldBound;
                if (bounds.width <= 0f || bounds.height <= 0f)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    xMin = bounds.xMin;
                    yMin = bounds.yMin;
                    xMax = bounds.xMax;
                    yMax = bounds.yMax;
                    hasBounds = true;
                    continue;
                }

                xMin = Mathf.Min(xMin, bounds.xMin);
                yMin = Mathf.Min(yMin, bounds.yMin);
                xMax = Mathf.Max(xMax, bounds.xMax);
                yMax = Mathf.Max(yMax, bounds.yMax);
            }

            return hasBounds ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : fallback;
        }

        /// <summary>
        /// One legal Defense card: the whole card is the control, and it forwards exactly the
        /// invocation the authority projected for it.
        ///
        /// The control adds no surrounding plate, inset, or selection box, so the shared card face is
        /// the complete visible offer and pointing at it cannot move or resize the row.
        /// </summary>
        private Button BuildDefenseCard(TypedInvocationViewModel choice)
        {
            var control = new Button(() => ForwardDefenseCard(choice))
            {
                name = "gd-defense-card",
                text = string.Empty
            };
            defenseCardControls.Add(
                new KeyValuePair<TypedInvocationViewModel, VisualElement>(choice, control));
            control.style.flexDirection = FlexDirection.Column;
            control.style.alignItems = Align.Stretch;
            control.style.flexShrink = 0f;
            // Milestone 8.20c: the offers line up because every face is the standard footprint, not
            // because the row stretches them. A Block prints no Defense value and an Attack's rules
            // text runs longer, so content-sized offers used to come out four different heights at
            // four different baselines; stretching them to the row fixed that inside this prompt and
            // left the same four cards a different size everywhere else.
            control.style.alignSelf = Align.FlexStart;
            control.style.width = CardFootprint.Width;
            control.style.marginLeft = 0f;
            control.style.marginRight = 0f;
            control.style.marginTop = 0f;
            control.style.marginBottom = 0f;
            HudElements.Padding(control, 0f);
            control.style.backgroundColor = Color.clear;
            HudElements.Border(control, Color.clear, 0f, 0f);

            var face = DirectorHandsUnityView.BuildStandaloneCard(choice.Card);
            // Milestone 8.30d: a card Defense offer shines exactly the shield it would defend
            // with, at the typed placement the presenter stated - never one the view inferred
            // from the face. Every other card-faced invocation carries None and lights nothing.
            DirectorHandsUnityView.AddFaceShine(face, choice.Card, choice.DefenseShineSlot);
            control.Add(face);
            control.tooltip = choice.Label.Replace('\n', ' ');
            return control;
        }

        private void BindDefenseCardDrag(
            Button control,
            VisualElement sourceSlot,
            TypedInvocationViewModel choice)
        {
            control.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || choice.Card == null ||
                    !defenseDragState.Begin(
                        evt.pointerId, choice.Card.CardInstanceId, evt.position))
                {
                    return;
                }

                defenseDragCapture = control;
                control.CapturePointer(evt.pointerId);
            }, TrickleDown.TrickleDown);

            control.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!defenseDragState.Move(
                        evt.pointerId, evt.position, DirectorHandsUnityView.CardDragThreshold))
                {
                    return;
                }

                ShowDefenseDragProxy(choice, sourceSlot, evt.position);
            }, TrickleDown.TrickleDown);

            control.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!defenseDragState.Release(evt.pointerId, out var cardId, out var wasDrag))
                {
                    return;
                }

                ReleaseDefenseDragCapture(evt.pointerId);
                HideDefenseDragProxy();
                if (!wasDrag)
                {
                    return;
                }

                evt.StopImmediatePropagation();
                if (choice.Card.CardInstanceId.Equals(cardId) &&
                    isMapDropPoint?.Invoke(evt.position) == true)
                {
                    ForwardDefenseCard(choice);
                }
            }, TrickleDown.TrickleDown);

            control.RegisterCallback<PointerCancelEvent>(_ => CancelDefenseCardDrag());
            control.RegisterCallback<DetachFromPanelEvent>(_ => CancelDefenseCardDrag());
        }

        private void ShowDefenseDragProxy(
            TypedInvocationViewModel choice,
            VisualElement source,
            Vector2 pointerPosition)
        {
            ClearDefenseCardPeekPreview();
            if (defenseDragProxy.childCount == 0)
            {
                var face = DirectorHandsUnityView.BuildStandaloneCard(choice.Card);
                DirectorHandsUnityView.AddFaceShine(
                    face, choice.Card, choice.DefenseShineSlot);
                DirectorHandsUnityView.IgnorePickingRecursively(face);
                defenseDragProxy.Add(face);
                defenseDragProxy.tooltip = choice.Label.Replace('\n', ' ');
            }

            if (draggedDefenseCard == null)
            {
                draggedDefenseCard = source;
                draggedDefenseCard.style.visibility = Visibility.Hidden;
            }

            defenseDragProxy.style.left = pointerPosition.x - (CardFootprint.Width * 0.5f);
            defenseDragProxy.style.top = pointerPosition.y - (CardFootprint.Height * 0.5f);
            defenseDragProxy.style.display = DisplayStyle.Flex;
            defenseDragProxy.BringToFront();
        }

        private void CancelDefenseCardDrag()
        {
            if (defenseDragCapture != null && defenseDragState.IsTracking)
            {
                ReleaseDefenseDragCapture(defenseDragState.PointerId);
            }

            defenseDragState.Cancel();
            defenseDragCapture = null;
            HideDefenseDragProxy();
        }

        private void ReleaseDefenseDragCapture(int pointerId)
        {
            if (defenseDragCapture != null && defenseDragCapture.HasPointerCapture(pointerId))
            {
                defenseDragCapture.ReleasePointer(pointerId);
            }

            defenseDragCapture = null;
        }

        private void HideDefenseDragProxy()
        {
            if (draggedDefenseCard != null)
            {
                draggedDefenseCard.style.visibility = Visibility.Visible;
                draggedDefenseCard = null;
            }

            defenseDragProxy.style.display = DisplayStyle.None;
            defenseDragProxy.Clear();
        }

        private VisualElement defenseCardPeekAnchor;

        private void SetDefenseCardPeekPreview(
            TypedInvocationViewModel choice,
            VisualElement anchor)
        {
            if (choice?.Card == null)
            {
                ClearDefenseCardPeekPreview();
                return;
            }

            if (defenseCardPeekAnchor != null && defenseCardPeekAnchor != anchor)
            {
                defenseCardPeekAnchor.style.opacity = 1f;
            }

            defenseCardPeekAnchor = anchor;
            defenseCardPeekAnchor.style.opacity = 0f;
            DirectorHandsUnityView.PopulatePeekPreview(
                defenseCardHoverPreview, choice.Card, anchor);
            DirectorHandsUnityView.AddFaceShine(
                defenseCardHoverPreview.ElementAt(0),
                choice.Card,
                choice.DefenseShineSlot);
        }

        private void ClearDefenseCardPeekPreview()
        {
            if (defenseCardPeekAnchor != null)
            {
                defenseCardPeekAnchor.style.opacity = 1f;
                defenseCardPeekAnchor = null;
            }

            defenseCardHoverPreview.Clear();
            defenseCardHoverPreview.style.display = DisplayStyle.None;
        }

        private void BuildActionRow()
        {
            // Milestone 8.20d: a standing decision gets its own labelled section, and it is built
            // before the action row rather than appended into it. Everything below is what the match
            // is offering; a pause is what the match is asking, and the two had been one row under
            // one heading that said `ACTIONS`.
            BuildEffectChoiceSection();

            // The status line already says that the controls are actions. Keeping a 96-unit ACTIONS
            // gutter repeated that fact and forced the final controls onto a staggered second line in
            // the compact Director-side dock. This unlabelled row spends its whole width on the actual
            // choices. The controls may flex narrower and wrap their own words, but the action row
            // itself never wraps: End Turn belongs beside the actions, not alone beneath them.
            var section = HudElements.Row(ActionControlGap);
            section.name = "gd-action-section";
            section.style.alignItems = Align.Stretch;
            section.style.flexWrap = Wrap.NoWrap;
            section.style.flexShrink = 1f;
            section.style.width = Length.Percent(100f);
            rows.Add(section);
            AddActionInvocation(section, Model.ContinueEndOfRoundInvocation);
            foreach (var choice in Model.InitiativeChoices)
            {
                AddActionInvocation(section, choice);
            }

            // Printed starts and the Clear/Fast Travel replacements attached to them are drawn on the
            // fully exposed resolving card. This dock retains only controls with no printed region.

            // Milestone 8.11a, at the user's request: each Respawn destination is a space, so it is
            // marked on the board and chosen by clicking it. The controls that used to name them are
            // gone; `HeroRespawnOptions` stays on the model because the board resolves a click through
            // it, and because the prompt still counts them to know it has a question to ask.

            // Milestone 8.11a: declining to respawn has no space on the board, so it stays a control
            // even though every destination it sits beside has become a click on the map.
            AddActionInvocation(section, Model.ResolveWithoutRespawn);

            // Milestone 6.5c drew one control per target the choice names, while a destination
            // candidate had none - the lit board was its control.
            //
            // Milestone 8.11a, at the user's request, makes the board the control for both. A candidate
            // that names a hero, a minion or a token is marked where it stands and answered by clicking
            // it, exactly as an Attack target has always been, so a second list of the same choices in
            // the prompt would be two ways to say one thing. The prompt keeps the question and the
            // choices that have no board surface at all; `EffectChoiceTargets` stays on the model
            // because the presenter still resolves a click through it.

            // Milestone 8.30b, by the user-approved amendment: End Turn is no longer one of these.
            // It is built by `RebuildEndTurn` into its own surface, which the renderer places in the
            // status bar's top-right slot.

            // A trailing gap is useful only between controls, and the row is content-sized: the last
            // control keeps none.
            var last = section.Children().LastOrDefault();
            if (last != null)
            {
                last.style.marginRight = 0f;
            }
        }

        /// <summary>
        /// End Turn, as its own control in its own surface. Milestone 8.30b's user-approved amendment.
        ///
        /// <para>Nothing about what it submits changed: it forwards the same projected invocation the
        /// action row forwarded, through the same <c>ForwardInvocation</c>, so the authority sees the
        /// identical command. What changed is that it is no longer the last item of a row it shared
        /// with the offered actions - milestone 8.29b1b had already moved those onto the resolving
        /// card, leaving End Turn a small button alone in a dock about the hand.</para>
        /// </summary>
        private void RebuildEndTurn()
        {
            endTurnHost.Clear();
            var invocation = Model?.EndTurnInvocation;

            // `HasExclusiveDecision` and not a second list, for milestone 7.9c's finding F-PRES-01
            // reason: two copies of "is the region asking something" drift by one entry and the entry
            // they drift by is a control standing where it must not.
            //
            // **This is what the complete Edit Mode run caught.** Nine of the ten states that
            // predicate covers are authority pauses, and the projection offers no End Turn in any of
            // them - but the tenth, an open Clear selection, is a presentation context: the authority
            // has not paused, so it still offers an End Turn, and the player is part-way through
            // choosing tokens on the board. Until this milestone the region hid the control by the
            // accident of returning early before the action row was built. Moving the control out of
            // that row would have made ending the turn mid-selection reachable again, which is
            // exactly the one-usable-context rule the tray hiding exists to keep.
            if (invocation == null || HasExclusiveDecision)
            {
                EndTurnShinesFully = false;
                endTurnHost.style.display = DisplayStyle.None;
                return;
            }

            endTurnHost.style.display = DisplayStyle.Flex;

            // Milestone 8.30d: whether ending the turn is the only thing left to do. Everything
            // else this region could offer alongside an End Turn is enumerable from the model the
            // view is already rendering: a startable printed region, a live replacement, a tied
            // Initiative, or the end-of-round continue. When none stands, the banner shines fully -
            // the game itself points at the player's one move.
            var onlyOfferLeft =
                Model.InitiativeChoices.Count == 0 &&
                Model.ContinueEndOfRoundInvocation == null &&
                Model.PrintedActionRegions.All(region =>
                    region.Invocation == null &&
                    region.FastTravelOffer == null &&
                    (region.ClearOffer == null || !region.ClearOffer.IsAvailable));

            EndTurnShinesFully = onlyOfferLeft;
            var control = new PewterBanner(
                () => ForwardInvocation(invocation), onlyOfferLeft,
                EndTurnControlFontSize, "gd-end-turn-label", rectangular: true)
            {
                name = "gd-end-turn-control",
                text = invocation.Label
            };
            control.SetVisibleLabel(invocation.Label);
            control.style.minHeight = ConfiguredMatchHudRenderer.EndTurnControlHeight;
            control.style.fontSize = EndTurnControlFontSize;
            control.style.unityFontStyleAndWeight = FontStyle.Bold;
            control.style.unityTextAlign = TextAnchor.MiddleCenter;
            control.style.flexGrow = 1f;
            control.style.flexShrink = 0f;
            endTurnHost.Add(control);
        }

        /// <summary>
        /// The neutral pewter banner - swallowtailed at both ends in the cards' own cloth
        /// language, but in the silver of their printed icons rather than a fifth patch of gold,
        /// by the user's 8.30d decision. Born as End Turn's clothes, and shared with the hand's
        /// Confirm and Pass controls by the user's follow-up: one banner language for "the button
        /// that moves the game on". A strong-shine banner carries the full warm glow (End Turn as
        /// the only move left, or a confirm that IS the move); otherwise a modest steady one,
        /// raised while pointed at.
        /// </summary>
        internal sealed class PewterBanner : Button
        {
            private const float NotchDepth = 14f;

            private readonly bool strongShine;
            private bool hovered;

            private readonly VisualElement cloth;

            internal PewterBanner(
                Action selected,
                bool strongShine,
                float fontSize,
                string labelName,
                bool rectangular = false)
                : base(selected)
            {
                this.strongShine = strongShine;
                this.rectangular = rectangular;
                style.backgroundColor = Color.clear;
                HudElements.Border(this, Color.clear, 0f, 0f);
                style.marginLeft = 0f;
                style.marginRight = 0f;
                style.marginTop = 0f;
                style.marginBottom = 0f;
                style.paddingLeft = rectangular ? 12f : NotchDepth + 8f;
                style.paddingRight = rectangular ? 12f : NotchDepth + 8f;
                style.paddingTop = 6f;
                style.paddingBottom = 6f;
                style.whiteSpace = WhiteSpace.NoWrap;
                style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                style.overflow = Overflow.Visible;

                // A TextElement paints its own words FIRST - both a generateVisualContent mesh and
                // any child render above them - so the cloth is a child, and the VISIBLE words are
                // a second child above it. The Button's own text stays set, buried under the
                // pewter, because it is the selector every existing check finds End Turn by.
                cloth = new VisualElement
                {
                    name = "gd-banner-cloth",
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                cloth.style.position = Position.Absolute;
                cloth.style.left = 0f;
                cloth.style.top = 0f;
                cloth.style.right = 0f;
                cloth.style.bottom = 0f;
                cloth.style.overflow = Overflow.Visible;
                cloth.generateVisualContent += OnGenerateVisualContent;
                Add(cloth);
                cloth.SendToBack();

                visibleLabel = new Label
                {
                    name = labelName,
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                visibleLabel.style.position = Position.Absolute;
                visibleLabel.style.left = rectangular ? 0f : NotchDepth;
                visibleLabel.style.top = 0f;
                visibleLabel.style.right = rectangular ? 0f : NotchDepth;
                visibleLabel.style.bottom = 0f;
                visibleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                visibleLabel.style.fontSize = fontSize;
                visibleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                visibleLabel.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                Add(visibleLabel);
                RegisterCallback<PointerEnterEvent>(_ => SetHovered(true));
                RegisterCallback<PointerLeaveEvent>(_ => SetHovered(false));
            }

            private readonly Label visibleLabel;
            private readonly bool rectangular;

            /// <summary>The words the banner shows; the Button's own text is the selector only.</summary>
            internal void SetVisibleLabel(string label)
            {
                visibleLabel.text = label ?? string.Empty;
            }

            /// <summary>Whether this banner is in its full-shine "your one move" state.</summary>
            internal bool ShinesFully => strongShine;

            /// <summary>Whether this instance uses the End Turn rectangle instead of swallowtails.</summary>
            internal bool IsRectangular => rectangular;

            private void SetHovered(bool value)
            {
                if (hovered == value)
                {
                    return;
                }

                hovered = value;
                cloth.MarkDirtyRepaint();
            }

            private void OnGenerateVisualContent(MeshGenerationContext context)
            {
                var painter = context.painter2D;
                var rect = cloth.contentRect;
                var left = rect.xMin;
                var right = rect.xMax;
                var mid = rect.center.y;
                if (right - left <= NotchDepth * 2f + 4f || rect.height <= 8f)
                {
                    return;
                }

                var strong = strongShine || hovered;
                DrawShape(painter, left, right, rect.yMin, rect.yMax, mid, Color.clear,
                    new Color(MatchHudTheme.ShineHalo.r, MatchHudTheme.ShineHalo.g,
                        MatchHudTheme.ShineHalo.b, strong ? 0.30f : 0.10f), strong ? 18f : 8f);
                DrawShape(painter, left, right, rect.yMin, rect.yMax, mid, Color.clear,
                    new Color(MatchHudTheme.ShineGlow.r, MatchHudTheme.ShineGlow.g,
                        MatchHudTheme.ShineGlow.b, strong ? 0.50f : 0.16f), strong ? 10f : 5f);
                if (strong)
                {
                    DrawShape(painter, left, right, rect.yMin, rect.yMax, mid, Color.clear,
                        new Color(MatchHudTheme.ShineRim.r, MatchHudTheme.ShineRim.g,
                            MatchHudTheme.ShineRim.b, 0.85f), 4f);
                }

                DrawShape(painter, left, right, rect.yMin, rect.yMax, mid,
                    MatchHudTheme.PewterBanner, Color.clear, 0f);

                painter.fillColor = new Color(0f, 0f, 0f, 0.12f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(rectangular ? left : left + NotchDepth, mid));
                painter.LineTo(new Vector2(rectangular ? right : right - NotchDepth, mid));
                painter.LineTo(new Vector2(right, rect.yMax));
                painter.LineTo(new Vector2(left, rect.yMax));
                painter.ClosePath();
                painter.Fill();

                DrawShape(painter, left, right, rect.yMin, rect.yMax, mid, Color.clear,
                    MatchHudTheme.PewterBannerEdge, 3f);
            }

            private void DrawShape(
                Painter2D painter, float left, float right, float top, float bottom, float mid,
                Color fill, Color stroke, float strokeWidth)
            {
                if (!rectangular)
                {
                    DrawBanner(painter, left, right, top, bottom, mid, fill, stroke, strokeWidth);
                    return;
                }

                painter.BeginPath();
                painter.MoveTo(new Vector2(left, top));
                painter.LineTo(new Vector2(right, top));
                painter.LineTo(new Vector2(right, bottom));
                painter.LineTo(new Vector2(left, bottom));
                painter.ClosePath();
                if (fill.a > 0f)
                {
                    painter.fillColor = fill;
                    painter.Fill();
                }

                if (stroke.a > 0f && strokeWidth > 0f)
                {
                    painter.strokeColor = stroke;
                    painter.lineWidth = strokeWidth;
                    painter.lineJoin = LineJoin.Round;
                    painter.Stroke();
                }
            }

            private static void DrawBanner(
                Painter2D painter, float left, float right, float top, float bottom, float mid,
                Color fill, Color stroke, float strokeWidth)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(left, top));
                painter.LineTo(new Vector2(right, top));
                painter.LineTo(new Vector2(right - NotchDepth, mid));
                painter.LineTo(new Vector2(right, bottom));
                painter.LineTo(new Vector2(left, bottom));
                painter.LineTo(new Vector2(left + NotchDepth, mid));
                painter.ClosePath();
                if (fill.a > 0f)
                {
                    painter.fillColor = fill;
                    painter.Fill();
                }

                if (stroke.a > 0f && strokeWidth > 0f)
                {
                    painter.strokeColor = stroke;
                    painter.lineWidth = strokeWidth;
                    painter.lineJoin = LineJoin.Round;
                    painter.Stroke();
                }
            }
        }

        /// <summary>
        /// End Turn's type size. Milestone 8.30b: it is the largest label on the HUD, which is the
        /// point - it is the one control that ends what the player is doing.
        /// </summary>
        public const float EndTurnControlFontSize = 18f;

        private void AddActionInvocation(
            VisualElement section,
            TypedInvocationViewModel invocation)
        {
            if (invocation != null)
            {
                AddActionControl(
                    section,
                    HudElements.Control(invocation.Label, () => ForwardInvocation(invocation)));
            }
        }

        private static void AddActionControl(VisualElement section, VisualElement control)
        {
            control.style.marginLeft = 0f;
            control.style.marginRight = ActionControlGap;
            control.style.paddingLeft = ActionControlHorizontalPadding;
            control.style.paddingRight = ActionControlHorizontalPadding;
            control.style.fontSize = ActionControlFontSize;
            control.style.flexShrink = 1f;
            control.style.minWidth = 56f;
            section.Add(control);
        }

        /// <summary>
        /// The standing effect choice, under a gutter that names the decision it is.
        ///
        /// <para>Milestone 8.20d. These controls used to be appended to the action row, so rulebook
        /// p.10's discard chooser reached the table as four card faces under a heading reading
        /// <c>ACTIONS</c>, beneath a status line reading "choose an action or End Turn" - the shape
        /// milestone 8.20b's Wasp journey met and recorded. The controls, their order and their
        /// invocations are exactly what they were; what changed is the heading they arrive under,
        /// which the model now carries because the pause projected it.</para>
        ///
        /// <para>The section is empty, and therefore hidden, for a choice answered entirely on the
        /// board - milestone 8.11a moved every candidate naming a piece or a space out of this region
        /// - and the status line is then the whole of what names the pause.</para>
        /// </summary>
        private void BuildEffectChoiceSection()
        {
            var prompt = Model.EffectChoicePrompt;
            if (prompt == null)
            {
                return;
            }

            if (prompt.Subject == EffectChoiceSubject.OptionalStep)
            {
                BuildOptionalStepActions();
                return;
            }

            var section = Section(prompt.SectionLabel);
            if (Model.EffectChoices.Any(choice => choice.Card != null))
            {
                section.style.flexWrap = Wrap.NoWrap;
            }
            foreach (var effectChoice in Model.EffectChoices)
            {
                // Milestone 8.15a, at the user's direction: a choice that is about a card is drawn
                // as the card - rulebook p.10's discard chooser above all - through the same shared
                // face a Defense offer wears. A choice about no card keeps its labelled control.
                if (effectChoice.Card != null)
                {
                    section.Add(BuildEffectCardChoice(effectChoice));
                }
                else
                {
                    AddInvocation(section, effectChoice);
                }
            }

            // Milestone 6.5ac: a mode the actor cannot perform keeps its label and its ordinal and
            // carries no command at all.
            foreach (var unavailable in Model.UnavailableEffectChoices)
            {
                var control = HudElements.Control(unavailable.Label, null);
                HudElements.MarkUnavailable(control);
                section.Add(control);
            }
        }

        private void BuildOptionalStepActions()
        {
            optionalStepActionsHost.style.display = DisplayStyle.Flex;
            for (var index = 0; index < Model.EffectChoices.Count; index++)
            {
                var invocation = Model.EffectChoices[index];
                var accepts = invocation.Label == EffectChoiceLabels.AcceptLabel;
                var control = new PewterBanner(
                    () => ForwardInvocation(invocation), true,
                    DirectorHandsUnityView.CornerBannerFontSize,
                    accepts
                        ? "gd-optional-step-accept-label"
                        : "gd-optional-step-decline-label")
                {
                    name = accepts ? "gd-optional-step-accept" : "gd-optional-step-decline",
                    text = invocation.Label
                };
                control.SetVisibleLabel(invocation.Label);
                control.style.width = OptionalStepActionWidth;
                control.style.minHeight = OptionalStepActionHeight;
                control.style.height = OptionalStepActionHeight;
                control.style.flexGrow = 0f;
                control.style.flexBasis = OptionalStepActionWidth;
                control.style.flexShrink = 1f;
                control.style.marginRight = index < Model.EffectChoices.Count - 1
                    ? OptionalStepActionGap
                    : 0f;
                optionalStepActionsHost.Add(control);
            }
        }

        private void AddInvocation(VisualElement section, TypedInvocationViewModel invocation)
        {
            if (invocation != null)
            {
                section.Add(HudElements.Control(invocation.Label, () => ForwardInvocation(invocation)));
            }
        }

        /// <summary>
        /// One card-valued effect choice as the whole printed face, on the terms milestone 8.11b gave
        /// a legal Defense offer: the tray's own standalone builder inside a fixed-width frame with
        /// the shared always-drawn selection outline, the whole face forwarding exactly the projected
        /// invocation, and the card's name in the control's hover and assistive reading.
        /// </summary>
        private Button BuildEffectCardChoice(TypedInvocationViewModel choice)
        {
            var control = new Button(() => ForwardInvocation(choice))
            {
                name = "gd-effect-card",
                text = string.Empty
            };
            control.style.flexDirection = FlexDirection.Column;
            control.style.alignItems = Align.Stretch;
            control.style.flexShrink = 0f;
            control.style.alignSelf = Align.FlexStart;
            control.style.width = DefenseCardFootprint;
            control.style.marginLeft = 0f;
            control.style.marginRight = 0f;
            control.style.marginTop = 0f;
            control.style.marginBottom = 0f;
            HudElements.Padding(control, DefenseCardInset);
            control.style.backgroundColor = MatchHudTheme.PanelBackground;
            HudElements.MarkSelectedWithoutResizing(control, false);

            var face = DirectorHandsUnityView.BuildStandaloneCard(choice.Card);
            // Milestone 8.30d: a card Defense offer shines exactly the shield it would defend
            // with, at the typed placement the presenter stated - never one the view inferred
            // from the face. Every other card-faced invocation carries None and lights nothing.
            DirectorHandsUnityView.AddFaceShine(face, choice.Card, choice.DefenseShineSlot);
            control.Add(face);
            control.tooltip = choice.Label.Replace('\n', ' ');
            return control;
        }

        public void ShowDefensePreview(DefenseBreakdownViewModel model, Rect anchorScreenRect)
        {
            activeDefensePreview = model ?? throw new ArgumentNullException(nameof(model));
            defensePreviewAnchor = anchorScreenRect;
            dismissPreviewAt = -1d;
            preview.Clear();
            var title = HudElements.Text(model.Title, 13f, MatchHudTheme.PrimaryText);
            title.name = "gd-defense-preview-title";
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6f;
            preview.Add(title);

            AddDefenseSection("ATTACK", model.AttackRows);
            var attackTotal = HudElements.Text(
                $"ATTACK TOTAL  {model.AttackTotal}", 13f, MatchHudTheme.PrimaryText);
            attackTotal.name = "gd-defense-attack-total";
            attackTotal.style.unityFontStyleAndWeight = FontStyle.Bold;
            attackTotal.style.marginTop = 3f;
            attackTotal.style.marginBottom = 7f;
            preview.Add(attackTotal);

            AddDefenseSection("DEFENSE", model.DefenseRows);
            var defenseTotalText = model.DefenseTotal.HasValue
                ? $"DEFENSE TOTAL  {model.DefenseTotal.Value}"
                : model.BlockResult == DefenseBlockResult.Blocked
                    ? "BLOCK"
                    : model.BlockResult == DefenseBlockResult.ConditionUnfulfilled
                        ? "BLOCK CONDITION NOT MET"
                        : "NO DEFENSE TOTAL";
            var defenseTotal = HudElements.Text(
                defenseTotalText, 13f, MatchHudTheme.PrimaryText);
            defenseTotal.name = "gd-defense-defense-total";
            defenseTotal.style.unityFontStyleAndWeight = FontStyle.Bold;
            defenseTotal.style.marginTop = 3f;
            defenseTotal.style.marginBottom = 7f;
            preview.Add(defenseTotal);

            var resultColor = model.Outcome == AttackResolutionOutcome.SuccessfullyDefended
                ? new Color(0.30f, 0.88f, 0.48f, 1f)
                : model.BlockResult == DefenseBlockResult.ConditionUnfulfilled
                    ? new Color(1f, 0.24f, 0.16f, 1f)
                    : new Color(1f, 0.63f, 0.18f, 1f);
            var result = HudElements.Text(model.ResultStatement, 14f, resultColor);
            result.name = "gd-defense-result";
            result.style.unityFontStyleAndWeight = FontStyle.Bold;
            result.style.unityTextAlign = TextAnchor.MiddleCenter;
            result.style.backgroundColor = new Color(
                resultColor.r * 0.12f,
                resultColor.g * 0.12f,
                resultColor.b * 0.12f,
                1f);
            HudElements.Padding(result, 6f);
            HudElements.Border(result, resultColor, 1f, 3f);
            preview.Add(result);
            preview.style.display = DisplayStyle.Flex;
            LayoutPreview();
        }

        private void AddDefenseSection(
            string heading,
            IReadOnlyList<DefenseContributionRowViewModel> contributionRows)
        {
            var section = HudElements.Text(heading, 10f, MatchHudTheme.MutedText);
            section.name = "gd-defense-section";
            section.style.unityFontStyleAndWeight = FontStyle.Bold;
            preview.Add(section);
            foreach (var contribution in contributionRows)
            {
                var row = HudElements.Row(8f);
                row.name = "gd-defense-contribution-row";
                row.style.justifyContent = Justify.SpaceBetween;
                var source = HudElements.Text(
                    contribution.SourceLabel, 11f, MatchHudTheme.PrimaryText);
                source.name = "gd-defense-contribution-source";
                source.style.flexGrow = 1f;
                var amountColor = contribution.Amount >= 0
                    ? new Color(0.30f, 0.90f, 0.48f, 1f)
                    : new Color(1f, 0.30f, 0.20f, 1f);
                var amount = HudElements.Text(contribution.SignedAmount, 12f, amountColor);
                amount.name = "gd-defense-contribution-amount";
                amount.style.unityFontStyleAndWeight = FontStyle.Bold;
                row.Add(source);
                row.Add(amount);
                preview.Add(row);
            }
        }

        public void ScheduleDefensePreviewDismissal(double now)
        {
            if (activeDefensePreview != null && dismissPreviewAt < 0d)
            {
                dismissPreviewAt = now + PreviewDismissDelaySeconds;
            }
        }

        public void UpdateDefensePreviewDismissal(double now)
        {
            if (activeDefensePreview != null && dismissPreviewAt >= 0d && now >= dismissPreviewAt)
            {
                ClearDefensePreview();
            }
        }

        public void ClearDefensePreview()
        {
            activeDefensePreview = null;
            dismissPreviewAt = -1d;
            preview.Clear();
            preview.style.display = DisplayStyle.None;
        }

        private void LayoutPreview()
        {
            // Milestone 8.25c: the panel's rectangle in reference units, not the screen's in pixels.
            // The three style writes below are panel coordinates, so a device pixel here would put the
            // breakdown outside the panel on any screen that is not the reference one.
            var viewport = ConfiguredMatchHudRenderer.PanelViewport;
            var size = new Vector2(
                Mathf.Min(440f, Mathf.Max(240f, viewport.x - 24f)),
                Mathf.Min(240f, Mathf.Max(120f, viewport.y - 24f)));
            var rect = CalculateDefensePreviewRect(defensePreviewAnchor, viewport, size);
            preview.style.left = rect.x;
            preview.style.top = rect.y;
            preview.style.width = rect.width;
            preview.style.maxHeight = rect.height;
        }

        /// <summary>
        /// Where the breakdown sits: immediately right of the complete available-card fan and
        /// inside the viewport. A constrained right edge falls back to the fan's left.
        /// </summary>
        public static Rect CalculateDefensePreviewRect(Rect anchor, Vector2 viewport, Vector2 size)
        {
            var width = Mathf.Min(size.x, Mathf.Max(0f, viewport.x));
            var height = Mathf.Min(size.y, Mathf.Max(0f, viewport.y));
            var maximumX = Mathf.Max(0f, viewport.x - width);
            var right = anchor.xMax + 8f;
            var left = anchor.xMin - width - 8f;
            var x = right + width <= viewport.x
                ? right
                : left >= 0f
                    ? left
                    : Mathf.Clamp(right, 0f, maximumX);
            var y = Mathf.Clamp(anchor.yMin, 0f, Mathf.Max(0f, viewport.y - height));
            return new Rect(x, y, width, height);
        }
    }

    /// <summary>
    /// Milestone 6.1a's Minion Battle removal prompt and milestone 6.4b's displaced-minion correction,
    /// which are one prompt with a family rather than two prompts.
    /// </summary>
    public sealed class MinionChoiceUnityView : IMinionChoiceView
    {
        private readonly VisualElement header;
        private readonly VisualElement controls;

        public MinionChoiceUnityView()
        {
            Root = new VisualElement { name = "gd-minion-choice" };
            Root.style.flexDirection = FlexDirection.Column;
            header = HudElements.Row(8f);
            header.style.marginBottom = 6f;
            controls = HudElements.Row(6f);
            Root.Add(header);
            Root.Add(controls);
        }

        public VisualElement Root { get; }

        public MinionChoiceViewModel Model { get; private set; }

        /// <summary>
        /// The candidate the pointer or keyboard focus is currently on, or <c>null</c> when none is.
        /// </summary>
        public MinionChoiceCandidateViewModel HighlightedCandidate { get; private set; }

        /// <summary>The unit of the candidate being pointed at, undefined when none is.</summary>
        public RuleObject HighlightedUnit => HighlightedCandidate?.Unit ?? RuleObject.None;

        public event Action<MinionChoiceCandidateViewModel> CandidateSubmitted;
        public event Action ConfirmRequested;
        public event Action<MinionChoiceCandidateViewModel> CandidateHighlighted;

        public void Render(MinionChoiceViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model;
            // A highlight belongs to the model it was read from, exactly as the round view's target and
            // respawn highlights do; a rebuilt prompt drops it rather than keeping a cue for a control
            // that is no longer drawn.
            if (HighlightedCandidate != null &&
                Model?.Candidates.Any(value => ReferenceEquals(value, HighlightedCandidate)) != true)
            {
                SetHighlight(null);
            }

            Rebuild();
        }

        public void ForwardCandidate(MinionChoiceCandidateViewModel candidate)
        {
            if (candidate != null)
            {
                CandidateSubmitted?.Invoke(candidate);
            }
        }

        public void ForwardConfirm() => ConfirmRequested?.Invoke();

        /// <summary>Reports a hovered or keyboard-focused candidate directly, for a driver with no pointer.</summary>
        public void ForwardHighlight(MinionChoiceCandidateViewModel candidate) => SetHighlight(candidate);

        private void Rebuild()
        {
            header.Clear();
            controls.Clear();
            var model = Model;
            Root.style.display = model == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (model == null)
            {
                return;
            }

            var gutter = HudElements.Text(model.SectionLabel, 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            gutter.style.width = 96f;
            header.Add(gutter);
            header.Add(HudElements.Text(model.PromptLabel, 12f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            header.Add(HudElements.Text(model.StatusLabel, 11f, MatchHudTheme.MutedText));

            var spacer = new VisualElement();
            spacer.style.width = 96f;
            spacer.style.flexShrink = 0f;
            controls.Add(spacer);
            // Milestone 8.11a, at the user's request: every candidate is a piece standing on the board,
            // so the board is where it is chosen. Each one is marked where it stands - and only while
            // the authority says it may be selected, which is how rulebook p.8's Heavy-last ordering
            // stays honest - and clicking it selects or deselects it. The controls that used to name
            // them are gone; the confirm below stays, because agreeing to a completed selection is not
            // a thing anywhere on the map.
            if (model.ConfirmInvocation != null)
            {
                var confirm = HudElements.Control(
                    model.ConfirmInvocation.Label, () => ConfirmRequested?.Invoke());
                confirm.style.backgroundColor = MatchHudTheme.ConfirmAccent;
                confirm.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                controls.Add(confirm);
            }
        }

        private void SetHighlight(MinionChoiceCandidateViewModel candidate)
        {
            if (ReferenceEquals(HighlightedCandidate, candidate))
            {
                return;
            }

            HighlightedCandidate = candidate;
            CandidateHighlighted?.Invoke(candidate);
        }
    }

    /// <summary>
    /// The `DIRECTOR` drawer's Undo and Reset. `RESET` is presentation terminology for the existing
    /// match Restart operation and forwards exactly that; nothing about what either does has changed.
    /// </summary>
    public sealed class UndoRestartUnityView : IUndoRestartView
    {
        private readonly Button undo;
        private readonly Button restart;

        public UndoRestartUnityView()
        {
            Root = new VisualElement { name = "gd-director-tools" };
            Root.style.flexDirection = FlexDirection.Column;
            undo = HudElements.Control("UNDO", ForwardUndo, 28f);
            restart = HudElements.Control("RESET", ForwardRestart, 28f);
            restart.tooltip = "Restart the match from its initial state.";
            undo.style.marginBottom = 4f;
            Root.Add(undo);
            Root.Add(restart);
        }

        public VisualElement Root { get; }

        public UndoRestartViewModel Model { get; private set; }

        public event Action UndoRequested;
        public event Action RestartRequested;

        /// <summary>
        /// What the `UNDO` control does, callable by a driver that has no pointer - the same idiom the
        /// minion prompt's <c>ForwardCandidate</c> and the round view's <c>ForwardInvocation</c> use, and
        /// for the same reason: a fixture and a build-smoke driver reach these surfaces without one.
        /// </summary>
        public void ForwardUndo() => UndoRequested?.Invoke();

        /// <summary>What the `RESET` control does. It is the existing Restart, under its HUD name.</summary>
        public void ForwardRestart() => RestartRequested?.Invoke();

        public void Render(UndoRestartViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            undo.SetEnabled(Model.CanUndo);
            undo.style.opacity = Model.CanUndo ? 1f : 0.45f;
            restart.SetEnabled(Model.CanRestart);
            restart.style.opacity = Model.CanRestart ? 1f : 0.45f;
        }
    }

    /// <summary>The collapsible diagnostic log, toggled from the same `DIRECTOR` drawer.</summary>
    public sealed class DiagnosticLogUnityView : IDiagnosticLogView
    {
        private readonly Button toggle;
        private readonly Label panelLabel;
        private readonly ScrollView entries;

        public DiagnosticLogUnityView()
        {
            toggle = HudElements.Control("EVENT LOG", ForwardToggle, 28f);
            ToggleRoot = toggle;
            PanelRoot = HudElements.Panel(MatchHudTheme.PanelBackground);
            PanelRoot.name = "gd-event-log";
            PanelRoot.style.position = Position.Absolute;
            PanelRoot.style.display = DisplayStyle.None;
            PanelRoot.style.flexDirection = FlexDirection.Column;
            HudElements.Padding(PanelRoot, 10f);
            HudElements.Border(PanelRoot, MatchHudTheme.PanelBorder, 1f, 4f);
            panelLabel = HudElements.Text(string.Empty, 11f, MatchHudTheme.MutedText, FontStyle.Bold);
            entries = new ScrollView(ScrollViewMode.Vertical);
            entries.style.flexGrow = 1f;
            PanelRoot.Add(panelLabel);
            PanelRoot.Add(entries);
        }

        public VisualElement ToggleRoot { get; }
        public VisualElement PanelRoot { get; }

        public DiagnosticLogViewModel Model { get; private set; }

        public event Action ToggleRequested;

        /// <summary>What the `EVENT LOG` control does, for a driver that has no pointer.</summary>
        public void ForwardToggle() => ToggleRequested?.Invoke();

        public void Render(DiagnosticLogViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            toggle.text = Model.ToggleLabel;
            panelLabel.text = Model.PanelLabel;
            PanelRoot.style.display = Model.IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            entries.Clear();
            if (!Model.IsExpanded)
            {
                return;
            }

            foreach (var entry in Model.Entries)
            {
                entries.Add(HudElements.Text(entry.DisplayText, 11f, MatchHudTheme.PrimaryText));
            }
        }

        public void Layout(Rect panel)
        {
            PanelRoot.style.left = panel.x;
            PanelRoot.style.top = panel.y;
            PanelRoot.style.width = panel.width;
            PanelRoot.style.height = panel.height;
        }
    }
}
