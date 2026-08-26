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
            element.SetEnabled(false);
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
        }

        public VisualElement Root { get; }
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
                preview.Add(DirectorHandsUnityView.BuildStandaloneCard(card));
                preview.style.display = DisplayStyle.Flex;
                PlacePreview(highlighted);
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

            var anchor = element.worldBound;
            var height = Root.resolvedStyle.height;
            var width = Root.resolvedStyle.width;
            var onLeft = anchor.center.x < width / 2f;
            var left = onLeft
                ? anchor.xMax + 10f
                : anchor.xMin - CardFootprint.Width - 10f;
            preview.style.left = Mathf.Max(0f, left);
            preview.style.top = Mathf.Max(
                0f,
                Mathf.Min(
                    anchor.yMin - 40f,
                    Mathf.Max(0f, height - CardFootprint.Height - 8f)));
        }

        public void Layout(Rect red, Rect blue, Rect playfield)
        {
            Root.style.left = 0f;
            Root.style.top = 0f;
            Root.style.width = Mathf.Max(playfield.width + playfield.x, Mathf.Max(red.xMax, blue.xMax));
            // Milestone 8.25: a rail column now runs past the playfield's bottom edge, because rulebook
            // p.6's three heroes a side need the whole screen edge rather than the band above the tray.
            // The container has to cover them, or the reveal below places a card face against a height
            // the rails are no longer inside.
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
            head.style.flexWrap = Wrap.NoWrap;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.FlexStart;
            var icon = new VisualElement { name = "gd-hero-icon" };
            icon.style.width = 30f;
            icon.style.height = 30f;
            icon.style.flexShrink = 0f;
            icon.style.backgroundColor = accent;
            icon.style.justifyContent = Justify.Center;
            HudElements.Border(icon, accent, 1f, 15f);
            var initial = HudElements.Text(hero.IconInitial, 14f, Color.white, FontStyle.Bold);
            initial.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.Add(initial);
            // The team is on the disc's own reading rather than spelled under the hero's name - the
            // user's 2026-08-21 decision. Two rails, two accents and two edges already say which side a
            // hero is on, and the word took a third line of the header to repeat it. It is not lost:
            // this is the same treatment milestone 8.5a gives a card's printed colour, which is named in
            // the hover and assistive reading rather than written on the face.
            icon.tooltip = hero.TeamLabel;
            var identity = HudElements.Column(0f);
            identity.style.flexShrink = 1f;
            identity.style.overflow = Overflow.Hidden;
            identity.style.alignItems = mirrored ? Align.FlexEnd : Align.FlexStart;
            var level = HudElements.Text(hero.LevelLabel, 9f, MatchHudTheme.MutedText, FontStyle.Bold);
            level.style.unityTextAlign = outward;
            identity.Add(level);
            var displayName = HudElements.Text(
                hero.HeroDisplayName, 12f, MatchHudTheme.PrimaryText, FontStyle.Bold);
            displayName.style.unityTextAlign = outward;
            identity.Add(displayName);
            var who = HudElements.Row(0f);
            who.style.flexWrap = Wrap.NoWrap;
            who.style.alignItems = Align.Center;
            who.style.flexShrink = 1f;
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

            var itemChips = HudElements.Row(6f);
            itemChips.name = "gd-item-chips";
            itemChips.style.flexWrap = Wrap.Wrap;
            itemChips.style.alignItems = Align.Center;
            itemChips.style.justifyContent = mirrored ? Justify.FlexEnd : Justify.FlexStart;
            foreach (var bonus in hero.ItemBonuses)
            {
                itemChips.Add(BuildValueChip(bonus.Token, bonus.AmountLabel, bonus.AssistiveLabel));
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
            discard.style.backgroundColor = new Color(0.129f, 0.149f, 0.161f, 1f);
            HudElements.Border(discard, MatchHudTheme.PanelBorder, 1f, 4f);

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
        /// One channel-and-amount chip: the three-letter token in muted ink and the signed amount in
        /// primary ink, as close together as a pair that is read as one thing should be.
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
        private const float CoinDiscSize = 18f;

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
        /// centred label, from the same three primitives the 30px hero disc in the header is built
        /// from. That is what keeps it inside this milestone's own non-goal against imported art, and
        /// what keeps it looking like this HUD on every platform the project declares. The word
        /// `COINS` is not lost - it is in the tooltip and the assistive reading as `3 coins`, which is
        /// where this HUD names everything it draws as a shape.</para>
        /// </summary>
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
                hero.CoinAmountLabel, 9f, MatchHudTheme.CoinInk, FontStyle.Bold);
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
    /// already played in ascending turn order, then available, then discarded.
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
        public const float CardGap = 12f;

        /// <summary>
        /// Milestone 8.5b: the Active-effect outline's width, and the gap between it and the card's own
        /// edge. Both are always drawn - the outline is transparent when nothing is active - so a card
        /// never changes size because an effect started or ended.
        /// </summary>
        public const float ActiveOutlineWidth = 2f;

        public const float ActiveOutlineGap = 3f;

        private readonly VisualElement header;
        private readonly VisualElement cards;

        public DirectorHandsUnityView()
        {
            Root = new VisualElement { name = "gd-card-tray" };
            Root.style.flexDirection = FlexDirection.Column;
            header = HudElements.Row(12f);
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 8f;
            cards = HudElements.Row(8f);
            // Milestone 8.20c: the row no longer sizes the cards. It used to stretch, so the tallest
            // card's content set the height of every card on the line - which is exactly the
            // content-driven sizing the standard footprint replaces.
            cards.style.alignItems = Align.FlexStart;
            // Room for the raise, so a still-playable card lifts into space of its own rather than into
            // the label above it.
            cards.style.marginTop = AvailableCardRaise;
            cards.name = "gd-card-buttons";
            Root.Add(header);
            Root.Add(cards);
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

            // The tray's own actions sit at the top right of its frame, beside the hint, rather than in
            // a row under the cards - the user's 2026-08-21 decision. A card is tall now, so a control
            // below the row sat near the bottom of the screen and a long way from the card that had
            // just been picked up; the header is one place, always in view, and it does not move when a
            // card is selected. Nothing about the controls themselves changed.
            var actions = HudElements.Row(8f);
            actions.name = "gd-tray-actions";
            actions.style.justifyContent = Justify.FlexEnd;
            actions.style.flexShrink = 1f;
            actions.Add(HudElements.Text(
                "SELECT A HERO ON THE MAP TO CHANGE HANDS", 10f, MatchHudTheme.MutedText));

            if (hand.CanPass)
            {
                var pass = HudElements.Control("PASS - NO CARD", () => PassRequested?.Invoke(hand.HeroId), 30f);
                pass.style.marginLeft = 10f;
                actions.Add(pass);
            }

            if (hand.SelectedCardInstanceId.HasValue)
            {
                var confirm = HudElements.Control("CONFIRM FACEDOWN", () => ConfirmRequested?.Invoke(), 30f);
                confirm.style.backgroundColor = MatchHudTheme.ConfirmAccent;
                confirm.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                confirm.style.marginLeft = 10f;
                actions.Add(confirm);
            }

            header.Add(actions);

            foreach (var card in hand.TrayCards)
            {
                cards.Add(BuildCard(card));
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

            // Milestone 8.20c: one size for every card in the game, stated once and pinned at both
            // ends. What stood here before was a width, a minimum height and a flex-grow, which made a
            // card's height whatever the tallest content in its own row needed - so five cards matched
            // each other and matched nothing else, and a hand of longer sentences was a taller hand.
            // The rules panel takes the slack inside the box instead, which is what keeps the bands of
            // five cards lined up without the box itself moving.
            CardFootprint.Apply(button);
            button.style.flexDirection = FlexDirection.Column;
            button.style.alignItems = Align.Stretch;
            button.style.justifyContent = Justify.FlexStart;
            // Milestone 8.5a: the bands run to the card's own edge exactly as a printed card's do, so
            // the padding that used to inset every line is gone and each band states its own.
            HudElements.Padding(button, 0f);
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.backgroundColor = MatchHudTheme.Face(card.Color);
            HudElements.Border(button, MatchHudTheme.PanelBorder, 1f, 8f);
            HudElements.MarkSelectedWithoutResizing(button, card.IsSelected);

            button.Add(BuildCardFrame(card));

            // Milestone 8.5b: the card sits in a slot, and the slot is what carries the Active-effect
            // outline. It has to be outside the card because the card's own border is the shared
            // selection treatment, and a card can be selected and active at the same time - two
            // meanings on one border would be one meaning lost. The slot is always drawn, so a card
            // does not change size when an effect starts or ends; only the outline's colour does.
            var slot = new VisualElement { name = "gd-card-slot" };
            slot.style.flexDirection = FlexDirection.Column;
            slot.style.alignItems = Align.Stretch;
            slot.style.marginRight = CardGap;
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
            if (!card.IsSelectable)
            {
                if (card.IsResolving)
                {
                    HudElements.MarkInPlay(button);
                }
                else
                {
                    HudElements.MarkUnavailable(button);
                }
            }

            var printed = card.Face.PrintedTextKind == CardSourceContentKind.Transcribed
                ? card.Face.PrintedText
                : card.Face.EmptyTextLabel;
            button.tooltip = $"{card.AssistiveLabel} - {printed}";
            slot.Add(button);
            return slot;
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

            var element = new VisualElement { name = "gd-card-preview" };
            CardFootprint.Apply(element);
            element.style.flexDirection = FlexDirection.Column;
            element.style.alignItems = Align.Stretch;
            element.style.backgroundColor = MatchHudTheme.Face(card.Color);
            // The same three-pixel edge a tray card wears, so one card does not read as two.
            HudElements.Border(element, MatchHudTheme.PanelBorder, 3f, 8f);
            element.Add(BuildCardFrame(card));
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
        private static VisualElement BuildCardFrame(TrayCardViewModel card)
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

        /// <summary>Where the card is in this hero's round: `PLAYED · TURN n`, `AVAILABLE`, `DISCARDED`.</summary>
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
        /// The frame an upgrade option's card sits in: enough for the shared selection outline and a
        /// little air, on every side. Milestone 8.8a. Three of these plus their gutters and the section's
        /// own label are well inside the tray's 1280 bound, which is the constraint the grid has to
        /// respect - two rows of full-height cards are taller than the tray, and the tray already scrolls.
        /// </summary>
        private const float UpgradeOptionInset = 6f;

        /// <summary>
        /// The frame a legal Defense card sits in, on the same terms as
        /// <see cref="UpgradeOptionInset"/>. Milestone 8.11b. Four of these plus `DECLINE DEFENSE`, the
        /// section's own gutter label and their spacing come to well under the tray's 1280 bound, which
        /// is milestone 5.1c's constraint on this row and the one this milestone must not break.
        /// </summary>
        private const float DefenseCardInset = 6f;

        /// <summary>
        /// The width one legal Defense offer occupies: a whole tray card plus the frame its shared
        /// selection outline is drawn in. Every offer is exactly this wide, so choosing one cannot
        /// change another's width, re-wrap its rules text, or move the row.
        /// </summary>
        public const float DefenseCardFootprint =
            CardFootprint.Width + (DefenseCardInset * 2f);

        public static readonly Color DefensePreviewBackgroundColor =
            new Color(0.025f, 0.035f, 0.045f, 1f);

        private readonly VisualElement status;
        private readonly VisualElement rows;
        private readonly VisualElement preview;

        /// <summary>
        /// The upgrade-option controls this rebuild drew, in the order they were drawn. Milestone 8.8a:
        /// the selection outline belongs to exactly one of them at a time, so marking one means unmarking
        /// the rest, and a driver with no pointer has to be able to reach them by option.
        /// </summary>
        /// <summary>
        /// The Defense-card controls this rebuild drew. Milestone 8.11b, for milestone 8.8a's reason:
        /// a driver with no pointer has to reach an offer the way its own control does, and an offer
        /// this rebuild did not draw must reach nothing.
        /// </summary>
        private readonly List<KeyValuePair<TypedInvocationViewModel, VisualElement>>
            defenseCardControls =
                new List<KeyValuePair<TypedInvocationViewModel, VisualElement>>();

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
        /// Whether the region is currently drawing p.15's upgrade choices as card faces, which is the one
        /// thing this region draws that is taller than one row of cards.
        ///
        /// Milestone 8.8a. The renderer asks it so the paused prompt can use the whole height under the
        /// status strip; every other state this region can be in fits the tray's ordinary bound.
        /// </summary>
        public bool HasUpgradeCardGrid =>
            Model?.SystemTransitionPrompt != null &&
            Model.SystemTransitionPrompt.Options.All(option => option.Card != null);

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
            status.Clear();
            rows.Clear();
            // Milestone 8.8a: the controls a rebuild is about to discard, and the selection that belonged
            // to them. A highlight belongs to the model it was read from, exactly as every other one here
            // does.
            upgradeOptionControls.Clear();
            defenseCardControls.Clear();
            HighlightedUpgradeOption = null;
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
                section.style.display = section.childCount > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>The gutter every section's family label reserves down this region's left edge.</summary>
        public const float SectionGutterWidth = 96f;

        /// <summary>The space between two controls in one section.</summary>
        public const float SectionGap = 6f;

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

            var section = Section(prompt.SectionLabel);
            var grid = HudElements.Column(6f);
            grid.style.flexGrow = 1f;
            section.Add(grid);

            // Milestone 8.8a: p.15's decision is a choice between cards, so it is drawn as cards. Every
            // option of an upgrade carries the candidate's own face.
            if (prompt.Options.All(option => option.Card != null))
            {
                // The gutter names the family beside the first row rather than halfway down two rows of
                // cards, which is where a row's own vertical centring would leave it.
                section.style.alignItems = Align.FlexStart;
                BuildUpgradeOptionGrid(grid, prompt);
                return;
            }

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
        /// p.15's options as a grid of card faces: one column per upgradable card, one row per candidate.
        ///
        /// <para>Milestone 8.8a. The columns are the pairs the pause itself grouped - options for one
        /// upgraded card arrive together, so grouping them is reading the projection rather than sorting
        /// it - and the candidates inside a column keep the order the pause offered them, which is what
        /// decides which of p.15's two cards is drawn above the other.</para>
        ///
        /// <para>What this milestone does decide is the left-to-right order of the columns, and it takes
        /// it from <see cref="MatchHudTheme.ColorReadingOrder"/> rather than from the pause. The pause
        /// orders its pairs by card instance identity, which puts the same three colours in a different
        /// order for every hero and in no order a player could learn; the colours are the one thing about
        /// this decision that is the same every time it is asked, so they are what the columns are read
        /// by. It reorders columns and nothing else: no option is added, removed, or moved out of its own
        /// pair.</para>
        /// </summary>
        private void BuildUpgradeOptionGrid(
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
            var rows = ordered.Max(column => column.Count);
            for (var rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                var row = HudElements.Row(DirectorHandsUnityView.CardGap);
                row.name = "gd-upgrade-row";
                row.style.alignItems = Align.FlexStart;
                row.style.justifyContent = Justify.FlexStart;
                row.style.flexWrap = Wrap.NoWrap;
                grid.Add(row);
                foreach (var column in ordered)
                {
                    row.Add(rowIndex < column.Count
                        ? BuildUpgradeOption(column[rowIndex])
                        : BuildUpgradeOptionGap());
                }
            }
        }

        /// <summary>
        /// One upgrade option: the candidate's whole printed face, and beside it the facts that belong to
        /// taking it rather than to the card.
        ///
        /// <para>The face is <see cref="DirectorHandsUnityView.BuildStandaloneCard"/> - the tray's own
        /// builder - so this prompt introduces no second, abbreviated card design and cannot decode a
        /// printed sentence. Everything the decision costs is drawn under it instead of on it: p.16's
        /// item is the icon printed on the <em>other</em> candidate, so a player choosing between two
        /// cards of one colour can see which item each choice leaves them holding, and the card returned
        /// to the deck is named because p.15 takes it out of the hand.</para>
        ///
        /// <para>The whole control is the option. Its outline is the HUD's shared selection treatment,
        /// always drawn and only ever changing colour, so focusing or pointing at a candidate cannot
        /// change a card's width, re-wrap its rules text, or move the row.</para>
        /// </summary>
        private Button BuildUpgradeOption(SystemTransitionOptionViewModel option)
        {
            var control = new Button(() => ForwardUpgradeOption(option))
            {
                name = "gd-upgrade-option",
                text = string.Empty
            };
            control.style.flexDirection = FlexDirection.Column;
            control.style.alignItems = Align.Stretch;
            control.style.flexShrink = 0f;
            control.style.width = CardFootprint.Width + (UpgradeOptionInset * 2f);
            control.style.marginLeft = 0f;
            control.style.marginRight = 0f;
            control.style.marginTop = 0f;
            control.style.marginBottom = 0f;
            HudElements.Padding(control, UpgradeOptionInset);
            control.style.backgroundColor = MatchHudTheme.PanelBackground;
            HudElements.MarkSelectedWithoutResizing(control, false);
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
        /// What taking this option does, under the card it would take: p.16's item, which is the icon
        /// printed on the candidate this option does <em>not</em> take.
        /// </summary>
        private static VisualElement BuildUpgradeOptionFacts(SystemTransitionOptionViewModel option)
        {
            var facts = HudElements.Column(3f);
            facts.name = "gd-upgrade-facts";
            facts.style.marginTop = 6f;

            var gained = HudElements.Row(4f);
            gained.name = "gd-upgrade-item";
            gained.style.flexWrap = Wrap.NoWrap;
            gained.Add(HudElements.Text("GAINS", 9f, MatchHudTheme.MutedText, FontStyle.Bold));
            if (option.ItemIcon == PrintedValueChannel.None)
            {
                // Two absences p.15 and p.16 produce between them - a deck with no second candidate, and
                // a dashboard with no slot for the icon that one prints - and `Detail` already states
                // which of the two this is. The chip is not drawn at all rather than drawn empty.
                gained.Add(HudElements.Text("NO ITEM", 10f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            }
            else
            {
                var chip = HudElements.Row(3f);
                chip.style.flexWrap = Wrap.NoWrap;
                chip.style.paddingLeft = 5f;
                chip.style.paddingRight = 5f;
                chip.style.paddingTop = 1f;
                chip.style.paddingBottom = 1f;
                chip.style.backgroundColor = MatchHudTheme.RailBackground;
                HudElements.Border(chip, MatchHudTheme.PanelBorder, 1f, 3f);
                chip.Add(HudElements.Text(
                    MatchHudTheme.ChannelToken(option.ItemIcon), 9f, MatchHudTheme.MutedText,
                    FontStyle.Bold));
                chip.Add(HudElements.Text(
                    MatchHudTheme.Signed(1), 11f, MatchHudTheme.PrimaryText, FontStyle.Bold));
                chip.tooltip = option.Detail;
                gained.Add(chip);
            }

            // The user's 2026-08-22 Game View decision: the item is the only one of the three facts
            // written under the card. Which card is set aside and which is returned to the deck are
            // both already on the screen - the sibling is the other card in this column, and the
            // returned card is the one whose colour the column is - so spelling them out six times
            // said what the grid says. Both stay in the option's hover and assistive reading, where
            // this HUD already keeps a fact a surface stops drawing.
            facts.Add(gained);
            return facts;
        }

        /// <summary>
        /// A column with no candidate on this row. It reserves the option's own footprint so the columns
        /// stay aligned, and takes no focus stop and no pointer, so an absent candidate is absent rather
        /// than being an empty control a player can reach.
        /// </summary>
        private static VisualElement BuildUpgradeOptionGap()
        {
            var gap = new VisualElement { name = "gd-upgrade-gap" };
            gap.style.width = CardFootprint.Width + (UpgradeOptionInset * 2f);
            gap.style.flexShrink = 0f;
            gap.pickingMode = PickingMode.Ignore;
            return gap;
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
                HudElements.MarkSelectedWithoutResizing(
                    entry.Value, ReferenceEquals(entry.Key, HighlightedUpgradeOption));
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
        /// <para>Nothing about the decision moves onto the card. The authority-computed
        /// Attack/Defense/result preview is still what hover and keyboard focus raise, still read from
        /// the offer's own <see cref="DefenseBreakdownViewModel"/>, and the visible order is the
        /// projected response order, which is the keyboard order too because the controls are added in
        /// it. <c>DECLINE DEFENSE</c> is about no card, so it stays the compact control it was.</para>
        /// </summary>
        private void BuildDefense()
        {
            var section = Section("DEFENSE");
            foreach (var choice in Model.DefenseChoices)
            {
                var control = choice.Card == null
                    ? HudElements.Control(choice.Label, () => ForwardInvocation(choice))
                    : BuildDefenseCard(choice);
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

        /// <summary>
        /// One legal Defense card: the whole card is the control, and it forwards exactly the
        /// invocation the authority projected for it.
        ///
        /// The selection outline is the HUD's shared treatment, always drawn and only ever changing
        /// colour, so pointing at or focusing an offer cannot change a card's width, re-wrap its rules
        /// text, or move the row - the same rule milestone 8.8a's upgrade grid keeps.
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
            control.style.width = DefenseCardFootprint;
            control.style.marginLeft = 0f;
            control.style.marginRight = 0f;
            control.style.marginTop = 0f;
            control.style.marginBottom = 0f;
            HudElements.Padding(control, DefenseCardInset);
            control.style.backgroundColor = MatchHudTheme.PanelBackground;
            HudElements.MarkSelectedWithoutResizing(control, false);

            control.Add(DirectorHandsUnityView.BuildStandaloneCard(choice.Card));
            control.tooltip = choice.Label.Replace('\n', ' ');
            return control;
        }

        private void BuildActionRow()
        {
            // Milestone 8.20d: a standing decision gets its own labelled section, and it is built
            // before the action row rather than appended into it. Everything below is what the match
            // is offering; a pause is what the match is asking, and the two had been one row under
            // one heading that said `ACTIONS`.
            BuildEffectChoiceSection();

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
                // Milestone 8.8b: a Clear with nothing adjacent to remove keeps its label and its place
                // and reads as unavailable through opacity as well as through being inert, which is the
                // treatment milestones 6.5ac and 6.5af give every other refused control. It carries no
                // command at all rather than one the authority would reject.
                var control = HudElements.Control(
                    clear.Label,
                    clear.IsAvailable ? () => ClearSelected?.Invoke(clear) : (Action)null);
                control.name = "gd-clear-offer";
                control.tooltip = clear.IsAvailable
                    ? "Clear replaces this card's Attack"
                    : "No token stands adjacent to this hero, so a Clear could remove nothing";
                if (!clear.IsAvailable)
                {
                    HudElements.MarkUnavailable(control);
                }

                section.Add(control);
            }

            // Milestone 8.11a, at the user's request: each Respawn destination is a space, so it is
            // marked on the board and chosen by clicking it. The controls that used to name them are
            // gone; `HeroRespawnOptions` stays on the model because the board resolves a click through
            // it, and because the prompt still counts them to know it has a question to ask.

            // Milestone 8.11a: declining to respawn has no space on the board, so it stays a control
            // even though every destination it sits beside has become a click on the map.
            AddInvocation(section, Model.ResolveWithoutRespawn);

            // Milestone 6.5c drew one control per target the choice names, while a destination
            // candidate had none - the lit board was its control.
            //
            // Milestone 8.11a, at the user's request, makes the board the control for both. A candidate
            // that names a hero, a minion or a token is marked where it stands and answered by clicking
            // it, exactly as an Attack target has always been, so a second list of the same choices in
            // the prompt would be two ways to say one thing. The prompt keeps the question and the
            // choices that have no board surface at all; `EffectChoiceTargets` stays on the model
            // because the presenter still resolves a click through it.

            if (Model.EndTurnInvocation != null)
            {
                var endTurn = HudElements.Control(
                    Model.EndTurnInvocation.Label, () => ForwardInvocation(Model.EndTurnInvocation));
                endTurn.style.marginLeft = 18f;
                section.Add(endTurn);
            }
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

            var section = Section(prompt.SectionLabel);
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

            control.Add(DirectorHandsUnityView.BuildStandaloneCard(choice.Card));
            control.tooltip = choice.Label.Replace('\n', ' ');
            return control;
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
