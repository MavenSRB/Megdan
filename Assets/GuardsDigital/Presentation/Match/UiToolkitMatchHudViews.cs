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
            button.style.backgroundColor = MatchHudTheme.ButtonIdleBackground;
            Border(button, MatchHudTheme.ButtonBorder, 1f, MatchHudTheme.ButtonCornerRadius);
            ApplyButtonMotion(button, tintBackground: true);
            return button;
        }

        /// <summary>
        /// Milestone 8.35z8 (the EIGHTEENTH amendment, the button-system pass), redesigned by
        /// 8.35z58. One shared press/hover feel for every clickable HUD or lobby
        /// control - idle, a slight hover lift, a quick ~100ms press dip, and disabled (left
        /// untouched here: <see cref="MarkUnavailable"/>
        /// already states that state through opacity and <c>SetEnabled(false)</c>, and this method
        /// itself never reacts to a pointer over a disabled control).
        /// </summary>
        /// <remarks>
        /// <para>A transform scale rather than a redraw of whatever the control paints, so the same
        /// call reads on a flat chip's rectangle, a full card face acting as its own button, and a
        /// custom-painted banner alike - each keeps its own face and gains only the lift and dip.
        /// <paramref name="tintBackground"/> layers a second, optional ease over the control's own
        /// background colour for the ordinary flat chip family (<see cref="Control"/> and the
        /// lobby's own controls): the idle colour is read back lazily, on the control's own first
        /// hover, rather than captured at this call - so a caller that colours a specific button
        /// after this method runs (Confirm's orange, Clear's accent) still eases toward and from
        /// its own colour rather than a stale generic one captured before that override landed.</para>
        /// </remarks>
        internal static void ApplyButtonMotion(VisualElement element, bool tintBackground = false)
        {
            element.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("scale") };
            element.style.transitionTimingFunction =
                new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutSine) };
            element.style.scale = new Scale(Vector3.one);

            Color? idleBackground = null;
            var hovered = false;
            var pressed = false;

            void Refresh()
            {
                var scale = pressed
                    ? MatchHudTheme.ButtonPressedScale
                    : hovered ? MatchHudTheme.ButtonHoverScale : 1f;
                var ms = pressed
                    ? MatchHudTheme.ButtonPressTransitionMs
                    : MatchHudTheme.ButtonHoverTransitionMs;
                element.style.transitionDuration =
                    new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond) };
                element.style.scale = new Scale(new Vector3(scale, scale, 1f));

                if (!tintBackground)
                {
                    return;
                }

                if (!idleBackground.HasValue)
                {
                    idleBackground = element.style.backgroundColor.value;
                }

                element.style.backgroundColor = pressed
                    ? MatchHudTheme.Darken(idleBackground.Value, MatchHudTheme.ButtonPressDarkenAmount)
                    : hovered
                        ? MatchHudTheme.Lighten(idleBackground.Value, MatchHudTheme.ButtonHoverLightenAmount)
                        : idleBackground.Value;
            }

            element.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (!element.enabledInHierarchy)
                {
                    return;
                }

                hovered = true;
                Refresh();
            });
            element.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                hovered = false;
                pressed = false;
                Refresh();
            });
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || !element.enabledInHierarchy)
                {
                    return;
                }

                pressed = true;
                Refresh();
            });
            element.RegisterCallback<PointerUpEvent>(_ =>
            {
                pressed = false;
                Refresh();
            });
            element.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                pressed = false;
                Refresh();
            });
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

        /// <summary>The Life counter's red, for the worth badge's heart half.</summary>
        internal static readonly Color WorthHeartFace = new Color(0.78f, 0.16f, 0.2f, 1f);

        /// <summary>
        /// Milestone 8.49's worth badge: half a coin and half a heart with the hero's worth in the
        /// middle. Rulebook p.13 pays the attacker's friends that many assist coins and takes that many
        /// Life counters from the defeated hero's team, and one number drawn on both halves says both
        /// at once. The left half is the coin's own face colour and the right half the Life counter's
        /// red; the two halves are one rounded disc, so the badge reads as a single token.
        ///
        /// <para>Milestone 8.53 moved it here from <c>HeroOverviewsUnityView</c> so the hero rail and
        /// the victory screen draw ONE badge rather than two that have to be kept looking alike. The
        /// element names are the rail's own - <c>gd-hero-worth</c>, <c>-coin</c>, <c>-heart</c>,
        /// <c>-value</c> - because fixtures address them by those names.</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.58. The badge's own diameter, in reference units.
        ///
        /// <para>The user, on a crop of the rail badge on 2026-09-19: "Hard to see this number one
        /// here." It was 16 units with a 10-unit digit painted straight onto two saturated halves - a
        /// dark ink on gold and the same dark ink on red - and at a hero rail's own scale that digit was
        /// smaller than the <c>LVL n</c> beside it. It is 22 now, and the digit is white over a dark
        /// disc of its own, so it reads the same on either half rather than on the gold alone.</para>
        /// </summary>
        internal const float WorthBadgeSize = 22f;

        /// <summary>
        /// Milestone 8.58. The digit's own black outline, which is what lets it read on the gold half
        /// and on the red one without a ground of its own.
        ///
        /// <para>It was a dark disc first, and the user struck that on the capture: "This is better, but
        /// not the way I want the shape to be ... we can barely see red color now behind it. So a bit
        /// better please, maybe no circle background for number, just black outline?" The disc was the
        /// half of the badge that stopped being visible; an outline costs the halves nothing.</para>
        /// </summary>
        internal static readonly Color WorthDigitOutline = new Color(0f, 0f, 0f, 1f);

        internal const float WorthDigitOutlineWidth = 1.5f;

        /// <summary>
        /// The digit's em as a fraction of the badge. The user: "number needs to be bigger I think.
        /// Make it just barely fit the circle." A digit's cap height is about 0.72 of its em, so 1.05
        /// badges puts the printed glyph at roughly 85% of the disc's inner diameter.
        /// </summary>
        internal const float WorthDigitScale = 1.05f;

        /// <summary>
        /// How far the digit's label is moved so its INK centres on the disc rather than its line box
        /// doing so - the user, twice: "just center the number in the circle", then "number is not
        /// vertically centered still".
        ///
        /// <para><b>Measured, not reasoned.</b> The first attempt pushed the label DOWN half a
        /// descender, on the usual argument that a digit reserves descender room it never uses. The
        /// 2560x1440 capture says this face does the opposite: with the label centred and pushed down
        /// one unit, the rail badge's disc ran y 143-164 (centre 153.5) while the printed glyph's white
        /// ink ran y 149-162 (centre 155.5). The ink sits one unit BELOW its own line box's centre at a
        /// 23.1-unit em - this face carries more room above the cap line than under the baseline - so
        /// the label is pulled UP by that unit: -1 / 23.1 of the em. It is a fraction of the em rather
        /// than a pixel count, so the badge centres at every size the rail and the victory screen draw
        /// it at.</para>
        /// </summary>
        internal const float WorthDigitInkNudge = -0.043f;

        internal static VisualElement WorthBadge(
            string digit, string assistiveLabel, float size = WorthBadgeSize)
        {
            var badge = new VisualElement { name = "gd-hero-worth", tooltip = assistiveLabel };
            badge.style.width = size;
            badge.style.height = size;
            badge.style.minWidth = size;
            badge.style.flexShrink = 0f;
            badge.style.flexDirection = FlexDirection.Row;
            badge.style.overflow = Overflow.Hidden;
            Border(badge, MatchHudTheme.CoinEdge, 1f, size / 2f);

            var coinHalf = new VisualElement { name = "gd-hero-worth-coin", pickingMode = PickingMode.Ignore };
            coinHalf.style.width = size / 2f;
            coinHalf.style.height = size;
            coinHalf.style.backgroundColor = MatchHudTheme.CoinFace;
            badge.Add(coinHalf);

            var heartHalf = new VisualElement { name = "gd-hero-worth-heart", pickingMode = PickingMode.Ignore };
            heartHalf.style.width = size / 2f;
            heartHalf.style.height = size;
            heartHalf.style.backgroundColor = WorthHeartFace;
            badge.Add(heartHalf);

            // Milestone 8.58, the user's last word on it: "number needs to be bigger I think. Make it
            // just barely fit the circle." A digit's cap height is about 0.72 of its em, so an em of
            // 1.05 badges puts the printed glyph at roughly 85% of the disc's inner diameter - which is
            // as large as it goes before the disc stops reading as a disc.
            var digitSize = size * WorthDigitScale;
            var value = Text(digit, digitSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            value.name = "gd-hero-worth-value";
            value.pickingMode = PickingMode.Ignore;
            // Milestone 8.58: the digit carries its OWN black outline rather than sitting on a disc,
            // so both coloured halves stay fully visible behind it.
            value.style.unityTextOutlineWidth = WorthDigitOutlineWidth;
            value.style.unityTextOutlineColor = WorthDigitOutline;
            value.style.position = Position.Absolute;
            value.style.left = 0f;
            // Milestone 8.58, the user twice: "this is good, just center the number in the circle",
            // then "number is not vertically centered still." `MiddleCenter` centres the LINE BOX, and
            // a line box is not the ink. WHERE the ink sits inside it was measured off the 2560x1440
            // capture rather than argued from the usual descender reasoning, which had this face
            // backwards - see `WorthDigitInkNudge`.
            value.style.top = digitSize * WorthDigitInkNudge;
            value.style.width = size;
            value.style.height = size;
            value.style.unityTextAlign = TextAnchor.MiddleCenter;
            value.style.marginLeft = 0f;
            value.style.marginRight = 0f;
            value.style.marginTop = 0f;
            value.style.marginBottom = 0f;
            badge.Add(value);
            return badge;
        }

        /// <summary>
        /// Milestone 8.35z7. Whether <paramref name="candidate"/> is <paramref name="ancestor"/> itself
        /// or is nested inside it - the match-log drop-down's click-away test, so a click on the
        /// drop-down or its own toggle is never also read as "outside" it.
        /// </summary>
        internal static bool IsSelfOrDescendant(VisualElement ancestor, VisualElement candidate)
        {
            var node = candidate;
            while (node != null)
            {
                if (ReferenceEquals(node, ancestor))
                {
                    return true;
                }

                node = node.parent;
            }

            return false;
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
}
