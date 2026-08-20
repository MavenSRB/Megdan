using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
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
        private readonly VisualElement content;

        public MatchStatusBarUnityView()
        {
            Root = HudElements.Panel(MatchHudTheme.TopBarBackground);
            Root.name = "gd-topbar";
            Root.style.position = Position.Absolute;
            Root.style.flexDirection = FlexDirection.Row;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.SpaceBetween;
            Root.style.paddingLeft = 14f;
            Root.style.paddingRight = 12f;
            Root.style.borderBottomWidth = 1f;
            Root.style.borderBottomColor = MatchHudTheme.PanelBorder;
            content = HudElements.Row(18f);
            content.style.flexShrink = 1f;
            Root.Add(content);
            tieBreakerChip = HudElements.Panel(MatchHudTheme.EmptySlot);
            tieBreakerChip.name = "gd-tie-breaker";
            tieBreakerChip.style.position = Position.Absolute;
            tieBreakerChip.style.flexDirection = FlexDirection.Row;
            tieBreakerChip.style.alignItems = Align.Center;
            tieBreakerChip.style.justifyContent = Justify.Center;
            HudElements.Border(tieBreakerChip, MatchHudTheme.PanelBorder, 1f, 4f);
            Root.Add(tieBreakerChip);
        }

        public VisualElement Root { get; }

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
        }

        public void Render(MatchHudHeaderViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            content.Clear();
            tieBreakerChip.Clear();

            content.Add(HudElements.Text(
                "GUARDS DIGITAL", 13f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            content.Add(HudElements.Text(model.RoundTurnLabel, 12f, MatchHudTheme.MutedText));
            content.Add(TeamChip(TeamSide.Red, model.RedLifeLabel));
            content.Add(HudElements.Text(model.WaveLabel, 12f, MatchHudTheme.MutedText));
            content.Add(TeamChip(TeamSide.Blue, model.BlueLifeLabel));

            tieBreakerChip.style.backgroundColor = model.Color;
            var chipLabel = HudElements.Text(
                model.TieBreakerLabel, 11f, Color.white, FontStyle.Bold);
            chipLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            chipLabel.style.flexGrow = 1f;
            tieBreakerChip.Add(chipLabel);
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

        public HeroOverviewsUnityView()
        {
            Root = new VisualElement { name = "gd-hero-rails" };
            Root.style.position = Position.Absolute;
            Root.pickingMode = PickingMode.Ignore;
            redColumn = Column("gd-hero-rail-red");
            blueColumn = Column("gd-hero-rail-blue");
            Root.Add(redColumn);
            Root.Add(blueColumn);
        }

        public VisualElement Root { get; }
        public HeroOverviewsViewModel Model { get; private set; }

        /// <summary>The two rails' current rects, for the pointer-blocking decision.</summary>
        public IEnumerable<VisualElement> BlockingRegions => new[] { redColumn, blueColumn };

        public void Layout(Rect red, Rect blue, Rect playfield)
        {
            Root.style.left = 0f;
            Root.style.top = 0f;
            Root.style.width = playfield.width + playfield.x;
            Root.style.height = playfield.height + playfield.y;
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

        private static VisualElement BuildOverview(HeroOverviewViewModel hero)
        {
            var rail = HudElements.Panel(MatchHudTheme.RailBackground);
            rail.name = "gd-hero-rail";
            rail.style.marginBottom = 8f;
            HudElements.Padding(rail, 9f);
            var accent = MatchHudTheme.TeamFace(hero.TeamSide);
            if (hero.TeamSide == TeamSide.Red)
            {
                rail.style.borderLeftWidth = 4f;
                rail.style.borderLeftColor = accent;
            }
            else
            {
                rail.style.borderRightWidth = 4f;
                rail.style.borderRightColor = accent;
            }

            if (hero.IsViewed)
            {
                rail.style.borderTopWidth = 2f;
                rail.style.borderTopColor = MatchHudTheme.SelectedOutline;
            }

            var head = HudElements.Row(7f);
            var icon = new VisualElement { name = "gd-hero-icon" };
            icon.style.width = 30f;
            icon.style.height = 30f;
            icon.style.backgroundColor = accent;
            icon.style.justifyContent = Justify.Center;
            HudElements.Border(icon, accent, 1f, 15f);
            var initial = HudElements.Text(hero.IconInitial, 14f, Color.white, FontStyle.Bold);
            initial.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.Add(initial);
            var identity = HudElements.Column(0f);
            identity.Add(HudElements.Text(hero.TeamLabel, 9f, MatchHudTheme.MutedText));
            identity.Add(HudElements.Text(hero.HeroDisplayName, 12f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            head.Add(icon);
            head.Add(identity);
            var level = HudElements.Text(hero.LevelLabel, 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            level.style.marginLeft = 6f;
            head.Add(level);
            rail.Add(head);

            var bonuses = HudElements.Row(6f);
            bonuses.name = "gd-bonus-strip";
            bonuses.style.marginTop = 7f;
            foreach (var bonus in hero.ItemBonuses)
            {
                var chip = HudElements.Row(3f);
                chip.tooltip = bonus.AssistiveLabel;
                chip.Add(HudElements.Text(bonus.Token, 10f, MatchHudTheme.MutedText, FontStyle.Bold));
                chip.Add(HudElements.Text(bonus.AmountLabel, 11f, MatchHudTheme.PrimaryText, FontStyle.Bold));
                bonuses.Add(chip);
            }

            if (hero.ItemBonuses.Count == 0)
            {
                bonuses.Add(HudElements.Text("NO ITEMS", 10f, MatchHudTheme.MutedText));
            }

            rail.Add(bonuses);

            var slots = HudElements.Row(4f);
            slots.name = "gd-turn-slots";
            slots.style.marginTop = 8f;
            slots.style.flexWrap = Wrap.NoWrap;
            foreach (var slot in hero.TurnSlots)
            {
                var element = new VisualElement();
                element.style.height = 40f;
                element.style.flexGrow = 1f;
                element.style.flexBasis = 0f;
                element.style.backgroundColor = slot.IsOccupied
                    ? MatchHudTheme.Face(slot.Color)
                    : MatchHudTheme.EmptySlot;
                HudElements.Border(
                    element,
                    slot.IsOccupied ? MatchHudTheme.PanelBorder : MatchHudTheme.MutedText,
                    slot.IsOccupied ? 1f : 1f,
                    4f);
                element.tooltip = slot.AssistiveLabel;
                slots.Add(element);
            }

            rail.Add(slots);

            var discard = HudElements.Row(6f);
            discard.name = "gd-discard";
            discard.style.marginTop = 6f;
            discard.style.justifyContent = Justify.SpaceBetween;
            discard.style.flexWrap = Wrap.NoWrap;
            HudElements.Padding(discard, 5f);
            discard.style.backgroundColor = new Color(0.129f, 0.149f, 0.161f, 1f);
            HudElements.Border(discard, MatchHudTheme.PanelBorder, 1f, 4f);
            discard.Add(HudElements.Text(hero.DiscardLabel, 10f, MatchHudTheme.MutedText, FontStyle.Bold));
            var pips = HudElements.Row(3f);
            pips.style.flexWrap = Wrap.NoWrap;
            foreach (var color in hero.DiscardColors)
            {
                var pip = new VisualElement();
                pip.style.width = 8f;
                pip.style.height = 8f;
                pip.style.backgroundColor = MatchHudTheme.Face(color);
                pip.tooltip = MatchHudTheme.ColorName(color) + " in the discard pile";
                HudElements.Border(pip, MatchHudTheme.PanelBorder, 1f, 4f);
                pips.Add(pip);
            }

            discard.Add(pips);
            discard.Add(HudElements.Text(
                hero.DiscardCountLabel, 12f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            rail.Add(discard);
            return rail;
        }
    }

    /// <summary>
    /// The card tray: one hero's whole current round, colour-coded, in the order the milestone fixes -
    /// already played in ascending turn order, then available, then discarded.
    ///
    /// Which hero it shows is the presenter's, and it is changed only from the map. This view draws no
    /// hero-selection control of its own.
    /// </summary>
    public sealed class DirectorHandsUnityView : IDirectorHandsView
    {
        /// <summary>Every card in the tray is this wide, so a hand reads as one row of equal cards.</summary>
        public const float CardWidth = 158f;

        /// <summary>
        /// The floor a card is never shorter than. Its actual height comes from the row, which stretches
        /// every card to the tallest one's content, so the cards match without a fixed height that a
        /// longer name could overflow.
        /// </summary>
        public const float CardMinimumHeight = 96f;

        /// <summary>How far a still-playable card sits above the ones that are spent.</summary>
        public const float AvailableCardRaise = 10f;

        private readonly VisualElement header;
        private readonly VisualElement cards;
        private readonly VisualElement footer;

        public DirectorHandsUnityView()
        {
            Root = new VisualElement { name = "gd-card-tray" };
            Root.style.flexDirection = FlexDirection.Column;
            header = HudElements.Row(12f);
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 8f;
            cards = HudElements.Row(8f);
            // Stretch, so the line's tallest card sets the height of every card on it.
            cards.style.alignItems = Align.Stretch;
            // Room for the raise, so a still-playable card lifts into space of its own rather than into
            // the label above it.
            cards.style.marginTop = AvailableCardRaise;
            cards.name = "gd-card-buttons";
            footer = HudElements.Row(8f);
            footer.style.marginTop = 8f;
            Root.Add(header);
            Root.Add(cards);
            Root.Add(footer);
        }

        public VisualElement Root { get; }

        public DirectorHandsViewModel Model { get; private set; }

        public event Action<CardInstanceId> CardSelected;
        public event Action ConfirmRequested;
        public event Action<HeroId> PassRequested;

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
            Root.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void Rebuild()
        {
            header.Clear();
            cards.Clear();
            footer.Clear();
            var hand = Model.ViewedHand;
            if (hand == null)
            {
                return;
            }

            header.Add(HudElements.Text(
                $"{hand.HeaderLabel} · {hand.CommitmentLabel.Replace("\n", " · ")}",
                12f,
                MatchHudTheme.PrimaryText,
                FontStyle.Bold));
            header.Add(HudElements.Text(
                "SELECT A HERO ON THE MAP TO CHANGE HANDS", 10f, MatchHudTheme.MutedText));

            foreach (var card in hand.TrayCards)
            {
                cards.Add(BuildCard(card));
            }

            if (hand.CanPass)
            {
                var pass = HudElements.Control("PASS - NO CARD", () => PassRequested?.Invoke(hand.HeroId), 32f);
                footer.Add(pass);
            }

            if (hand.SelectedCardInstanceId.HasValue)
            {
                var confirm = HudElements.Control("CONFIRM FACEDOWN", () => ConfirmRequested?.Invoke(), 36f);
                confirm.style.backgroundColor = MatchHudTheme.ConfirmAccent;
                confirm.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                footer.Add(confirm);
            }
        }

        private VisualElement BuildCard(TrayCardViewModel card)
        {
            var button = new Button(() =>
            {
                if (card.IsSelectable)
                {
                    CardSelected?.Invoke(card.CardInstanceId);
                }
            })
            {
                name = "gd-card-choice"
            };
            button.text = string.Empty;

            // One size for every card in the tray, without a fixed height. A minimum alone let each
            // card settle at whatever its own wrapped name and value line needed, so five cards of one
            // hand came out five different shapes; a fixed height instead pushed the history line out
            // through the bottom of any card whose content was taller. The row stretches every card to
            // the tallest one's content, so they match and none of them can overflow.
            button.style.width = CardWidth;
            button.style.minHeight = CardMinimumHeight;
            button.style.flexDirection = FlexDirection.Column;
            button.style.alignItems = Align.FlexStart;
            button.style.justifyContent = Justify.SpaceBetween;
            HudElements.Padding(button, 8f);
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.backgroundColor = MatchHudTheme.Face(card.Color);
            HudElements.Border(button, MatchHudTheme.PanelBorder, 1f, 6f);
            HudElements.MarkSelected(button, card.IsSelected);

            var text = MatchHudTheme.FaceText(card.Color);
            button.Add(HudElements.Text(card.ColorName, 9f, text, FontStyle.Bold));
            button.Add(HudElements.Text(card.DisplayName, 13f, text, FontStyle.Bold));
            button.Add(HudElements.Text(card.ValueLine, 10f, text));
            var history = HudElements.Text(card.HistoryLabel, 10f, text, FontStyle.Bold);
            history.style.marginTop = 4f;
            history.style.borderTopWidth = 1f;
            history.style.borderTopColor = new Color(0f, 0f, 0f, 0.22f);
            history.style.width = Length.Percent(100f);
            button.Add(history);

            // The mockup's raised treatment: a card that can still be played this round sits visibly
            // higher than the ones that cannot. It is a relative offset rather than a margin, because a
            // margin on a stretched card would shorten it instead of moving it. The card keeps its size,
            // its hit target moves with it, and its keyboard order and legality are untouched - the
            // group it belongs to is already in the label either way.
            button.style.top = card.Group == TrayCardGroup.Available ? -AvailableCardRaise : 0f;
            if (!card.IsSelectable)
            {
                HudElements.MarkUnavailable(button);
            }

            button.tooltip = $"{card.DisplayName} - {card.ColorName} - {card.HistoryLabel}";
            return button;
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

        public static readonly Color DefensePreviewBackgroundColor =
            new Color(0.025f, 0.035f, 0.045f, 1f);

        private readonly VisualElement status;
        private readonly VisualElement rows;
        private readonly VisualElement preview;
        private DefenseBreakdownViewModel activeDefensePreview;
        private Rect defensePreviewAnchor;
        private double dismissPreviewAt = -1d;

        public RoundActionUnityView()
        {
            Root = new VisualElement { name = "gd-context" };
            Root.style.flexDirection = FlexDirection.Column;
            status = HudElements.Row(8f);
            status.style.marginBottom = 6f;
            rows = HudElements.Column(6f);
            Root.Add(status);
            Root.Add(rows);
            preview = HudElements.Panel(DefensePreviewBackgroundColor);
            preview.name = "gd-defense-preview";
            preview.style.position = Position.Absolute;
            preview.style.display = DisplayStyle.None;
            preview.pickingMode = PickingMode.Ignore;
            HudElements.Padding(preview, 10f);
            HudElements.Border(preview, MatchHudTheme.PanelBorder, 1f, 4f);
        }

        public VisualElement Root { get; }

        /// <summary>The Defense breakdown popup, placed on the HUD root so it can leave the tray.</summary>
        public VisualElement PreviewRoot => preview;

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

        /// <summary>One offered token control was activated, added to or taken out of the subset.</summary>
        public event Action<ClearTokenCandidateViewModel> ClearTokenToggled;
        public event Action ClearConfirmed;
        public event Action ClearCancelled;

        /// <summary>
        /// The Clear token control the pointer or keyboard focus is on, or null when none is. The
        /// board reads this to mark that exact piece.
        /// </summary>
        public event Action<ClearTokenCandidateViewModel> ClearTokenHighlighted;

        public EffectChoiceTargetViewModel HighlightedTarget { get; private set; }
        public HeroRespawnOptionViewModel HighlightedRespawn { get; private set; }
        public ClearTokenCandidateViewModel HighlightedClearToken { get; private set; }

        /// <summary>
        /// Whether the current model carries a decision that must be answered before anything else can
        /// happen. The tray is hidden while one stands, which is how "at most one usable interaction
        /// context" is kept structurally rather than by discipline.
        /// </summary>
        public bool HasExclusiveDecision =>
            Model != null &&
            (Model.SystemTransitionPrompt != null ||
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

            // Milestone 6.3a: on the same terms. A rebuilt model - a new projection, or a change to
            // the selection itself - drops the pointed-at token, so the board never keeps a cue for a
            // control that is no longer drawn.
            if (HighlightedClearToken != null &&
                Model.ClearSelection?.Candidates.Any(value =>
                    ReferenceEquals(value, HighlightedClearToken)) != true)
            {
                SetClearTokenHighlight(null);
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

        /// <summary>Reports a pointed-at Clear token control directly, for a driver with no pointer.</summary>
        public void ForwardClearTokenHighlight(ClearTokenCandidateViewModel candidate) =>
            SetClearTokenHighlight(candidate);

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

        private void SetClearTokenHighlight(ClearTokenCandidateViewModel candidate)
        {
            if (ReferenceEquals(HighlightedClearToken, candidate))
            {
                return;
            }

            HighlightedClearToken = candidate;
            ClearTokenHighlighted?.Invoke(candidate);
        }

        private void Rebuild()
        {
            status.Clear();
            rows.Clear();
            status.Add(HudElements.Text(Model.Status, 12f, MatchHudTheme.PrimaryText));

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

            BuildActionRow();
            HideEmptySections();
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
                section.style.display = section.childCount > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private VisualElement Section(string gutter)
        {
            var section = HudElements.Row(6f);
            var label = HudElements.Text(gutter, 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            label.style.width = 96f;
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
            var section = Section("CLEAR");
            foreach (var candidate in selection.Candidates)
            {
                var control = HudElements.Control(
                    candidate.Label, () => ClearTokenToggled?.Invoke(candidate));
                HudElements.MarkSelected(control, candidate.IsSelected);
                HudElements.OnPointedAt(control, pointedAt =>
                    SetClearTokenHighlight(pointedAt ? candidate : null));
                section.Add(control);
            }

            if (selection.Candidates.Count == 0)
            {
                section.Add(HudElements.Text(
                    "NO ADJACENT TOKEN - CLEARING NONE STILL RESOLVES THE CARD",
                    11f,
                    MatchHudTheme.MutedText));
            }

            var confirm = HudElements.Control(
                selection.Confirm.Label, () => ClearConfirmed?.Invoke());
            confirm.style.backgroundColor = MatchHudTheme.ConfirmAccent;
            confirm.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
            section.Add(confirm);
            section.Add(HudElements.Control(selection.CancelLabel, () => ClearCancelled?.Invoke()));
        }

        private void BuildDefense()
        {
            var section = Section("DEFENSE");
            foreach (var choice in Model.DefenseChoices)
            {
                var control = HudElements.Control(choice.Label, () => ForwardInvocation(choice));
                if (choice.DefenseBreakdown != null)
                {
                    HudElements.OnPointedAt(control, pointedAt =>
                    {
                        if (pointedAt)
                        {
                            ShowDefensePreview(choice.DefenseBreakdown, control.worldBound);
                        }
                        else
                        {
                            ScheduleDefensePreviewDismissal(Time.realtimeSinceStartupAsDouble);
                        }
                    });
                }

                section.Add(control);
            }
        }

        private void BuildActionRow()
        {
            var section = Section("ACTIONS");
            AddInvocation(section, Model.ContinueEndOfRoundInvocation);
            foreach (var choice in Model.InitiativeChoices)
            {
                AddInvocation(section, choice);
            }

            foreach (var action in Model.Actions)
            {
                AddInvocation(section, action);
            }

            // Milestone 6.5af: an action the authority judged unstartable is drawn beside the ones that
            // can be started, disabled, so a player reads that the action exists and cannot be taken.
            foreach (var unavailable in Model.UnavailableActions)
            {
                var control = HudElements.Control(unavailable.Label, null);
                HudElements.MarkUnavailable(control);
                section.Add(control);
            }

            foreach (var fastTravel in Model.FastTravelOffers)
            {
                section.Add(HudElements.Control(
                    fastTravel.Label, () => ForwardFastTravelSelection(fastTravel)));
            }

            foreach (var clear in Model.ClearOffers)
            {
                section.Add(HudElements.Control(clear.Label, () => ClearSelected?.Invoke(clear)));
            }

            foreach (var effectChoice in Model.EffectChoices)
            {
                AddInvocation(section, effectChoice);
            }

            // Milestone 6.5ac: a mode the actor cannot perform keeps its label and its ordinal and
            // carries no command at all.
            foreach (var unavailable in Model.UnavailableEffectChoices)
            {
                var control = HudElements.Control(unavailable.Label, null);
                HudElements.MarkUnavailable(control);
                section.Add(control);
            }

            foreach (var option in Model.HeroRespawnOptions)
            {
                var control = HudElements.Control(
                    option.Label,
                    () => ForwardInvocation(new TypedInvocationViewModel(option.Label, option.Command)));
                HudElements.OnPointedAt(control, pointedAt =>
                    SetRespawnHighlight(pointedAt ? option : null));
                section.Add(control);
            }

            AddInvocation(section, Model.ResolveWithoutRespawn);

            // Milestone 6.5c: one control per target the choice names. Nothing is drawn for a
            // destination candidate - the lit board is its control.
            foreach (var target in Model.EffectChoiceTargets)
            {
                var control = HudElements.Control(target.Label, () => ForwardTarget(target));
                HudElements.OnPointedAt(control, pointedAt =>
                    SetTargetHighlight(pointedAt ? target : null));
                section.Add(control);
            }

            if (Model.EndTurnInvocation != null)
            {
                var endTurn = HudElements.Control(
                    Model.EndTurnInvocation.Label, () => ForwardInvocation(Model.EndTurnInvocation));
                endTurn.style.marginLeft = 18f;
                section.Add(endTurn);
            }
        }

        private void AddInvocation(VisualElement section, TypedInvocationViewModel invocation)
        {
            if (invocation != null)
            {
                section.Add(HudElements.Control(invocation.Label, () => ForwardInvocation(invocation)));
            }
        }

        public void ShowDefensePreview(DefenseBreakdownViewModel model, Rect anchorScreenRect)
        {
            activeDefensePreview = model ?? throw new ArgumentNullException(nameof(model));
            defensePreviewAnchor = anchorScreenRect;
            dismissPreviewAt = -1d;
            preview.Clear();
            var text = HudElements.Text(model.Text, 11f, MatchHudTheme.PrimaryText);
            text.style.whiteSpace = WhiteSpace.Normal;
            preview.Add(text);
            preview.style.display = DisplayStyle.Flex;
            LayoutPreview();
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
            var viewport = new Vector2(Screen.width, Screen.height);
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
        /// Where the breakdown sits: clear of the control it belongs to, and inside the viewport.
        /// Unchanged geometry from milestone 5.8b - only the surface it is drawn on has changed.
        /// </summary>
        public static Rect CalculateDefensePreviewRect(Rect anchor, Vector2 viewport, Vector2 size)
        {
            var width = Mathf.Min(size.x, Mathf.Max(0f, viewport.x));
            var height = Mathf.Min(size.y, Mathf.Max(0f, viewport.y));
            var above = anchor.yMin - height - 8f;
            var below = anchor.yMax + 8f;
            var y = above >= 0f ? above : Mathf.Min(below, Mathf.Max(0f, viewport.y - height));
            var x = Mathf.Clamp(anchor.center.x - (width / 2f), 0f, Mathf.Max(0f, viewport.x - width));
            return new Rect(x, Mathf.Max(0f, y), width, height);
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
            foreach (var candidate in model.Candidates)
            {
                var control = HudElements.Control(candidate.Label, () => ForwardCandidate(candidate));
                if (candidate.IsSelected)
                {
                    control.style.backgroundColor = MatchHudTheme.ConfirmAccent;
                    control.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                }

                HudElements.OnPointedAt(control, pointedAt =>
                    SetHighlight(pointedAt ? candidate : null));
                controls.Add(control);
            }

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
