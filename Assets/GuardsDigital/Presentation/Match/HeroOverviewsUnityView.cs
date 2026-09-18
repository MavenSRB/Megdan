using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{

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
        public const float HeroIdentityEdgeWidth = 2f;

        // Milestone 8.35z39 retired `AwaitedRailShineInset = 6f` with the travelling mark
        // whose overhang it existed to prevent. Milestone 8.35z9 sized it to exceed half a 10px
        // mark centred on its host's border, because at zero inset half that mark hung outside the
        // rail's own column and `MinimalHexGridPlayModeTests` refused it by measurement. The breath
        // that replaced the mark writes the rail's own border colour and draws no element at all, so
        // there is nothing left that can hang outside a column and no inset left to size.

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
        private readonly Dictionary<HeroId, VisualElement> heroRails =
            new Dictionary<HeroId, VisualElement>();
        private readonly Dictionary<HeroId, Color> heroRailRestingTopColors =
            new Dictionary<HeroId, Color>();

        private object highlighted;
        private HeroId highlightedHero;

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
            Root.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (highlighted != null) PlacePreview(highlighted);
            });
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
        public event Action<HeroId> HeroHighlighted;
        public event Action<HeroId> HeroSelected;

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

        public void SetHighlightedHero(HeroId heroId) => SetHighlightedHero(heroId, false);

        /// <summary>Reports plaque hover for pointerless drivers and the configured input bridge.</summary>
        public void ForwardHeroHighlight(HeroId heroId)
        {
            SetHighlightedHero(heroId, false);
            HeroHighlighted?.Invoke(heroId);
        }

        private void SetHighlightedHero(HeroId heroId, bool notify)
        {
            if (highlightedHero.Equals(heroId)) return;
            if (highlightedHero.IsDefined && heroRails.TryGetValue(highlightedHero, out var previous))
            {
                previous.style.borderTopWidth = HeroIdentityEdgeWidth;
                previous.style.borderTopColor = heroRailRestingTopColors[highlightedHero];
            }

            highlightedHero = heroId;
            if (highlightedHero.IsDefined && heroRails.TryGetValue(highlightedHero, out var current))
            {
                // Corrective 8.30n2: the identity cue is colour in permanently reserved geometry.
                // Expanding this edge moved every child, including the hero face, on hover.
                current.style.borderTopWidth = HeroIdentityEdgeWidth;
                current.style.borderTopColor = HexCornerHighlightTemplate.AllyColor;
            }

            if (notify)
                HeroHighlighted?.Invoke(heroId);
        }

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
            var face = model is HeroOverviewViewModel hero
                ? BuildHeroProfile(hero)
                : card == null ? null : DirectorHandsUnityView.BuildStandaloneCard(card);
            if (face == null)
            {
                highlighted = null;
                preview.style.display = DisplayStyle.None;
            }
            else
            {
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

        private static VisualElement BuildHeroProfile(HeroOverviewViewModel hero)
        {
            // A staged/mirrored hero's instance id can differ from its registered content identity.
            var registered = RegisteredHeroRoster.ForThisBuild.FirstOrDefault(value =>
                value.ContentId.Equals(hero.ContentId));
            if (string.IsNullOrEmpty(registered.HeroIdValue)) return null;
            var texture = Resources.Load<Texture2D>("Private/HeroSelection/" + registered.HeroIdValue);
            if (texture == null) return null; // Private artwork is optional in a source-only checkout.
            return new Image
            {
                name = "gd-hero-profile-preview",
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                tooltip = hero.PortraitAssistiveLabel,
                pickingMode = PickingMode.Ignore
            };
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
            if (face is Image profile && model is HeroOverviewViewModel && profile.image != null)
            {
                var aspect = (float)profile.image.width / profile.image.height;
                var profileWidth = Mathf.Max(0f,
                    Mathf.Min(744f, width - 16f, (height - 16f) * aspect));
                profile.style.width = profileWidth;
                profile.style.height = profileWidth / aspect;
            }
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
            preview.style.left = Mathf.Clamp(left, 0f, Mathf.Max(0f, width - faceWidth));
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
            heroRails.Clear();
            heroRailRestingTopColors.Clear();
            redColumn.Clear();
            blueColumn.Clear();
            foreach (var hero in Model.Heroes)
            {
                var column = hero.TeamSide == TeamSide.Red ? redColumn : blueColumn;
                var rail = BuildOverview(hero);
                heroRails.Add(hero.HeroId, rail);
                heroRailRestingTopColors.Add(
                    hero.HeroId,
                    hero.IsViewed ? MatchHudTheme.SelectedOutline : new Color(0f, 0f, 0f, 0f));
                HudElements.OnPointedAt(rail, pointedAt =>
                    ForwardHeroHighlight(pointedAt ? hero.HeroId : default));
                rail.AddManipulator(new Clickable(() => HeroSelected?.Invoke(hero.HeroId)));
                column.Add(rail);
            }

            if (highlightedHero.IsDefined)
            {
                if (heroRails.TryGetValue(highlightedHero, out var highlightedRail))
                {
                    highlightedRail.style.borderTopWidth = HeroIdentityEdgeWidth;
                    highlightedRail.style.borderTopColor = HexCornerHighlightTemplate.AllyColor;
                }
                else
                {
                    highlightedHero = default;
                }
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
            // Milestone 8.35z11. THE ONE LINE the motion pass adds to this file. Six rails share
            // the name `gd-hero-rail`, so a card flying to a PARTICULAR hero's commitment slot, or a
            // coin landing in a particular tray, has no way to tell them apart by query alone. This
            // stamp is that identity; `KineticFlightAnchors.HeroRail` reads it and returns null when it
            // finds none, so a build without this line loses the flourish and nothing else. It changes
            // no layout, no style and no behaviour - `userData` is unused on this element otherwise.
            rail.userData = hero.HeroId;
            rail.style.marginBottom = 8f;
            HudElements.Padding(rail, 9f);
            rail.style.paddingTop = 8f;
            rail.style.paddingBottom = 8f;
            var accent = MatchHudTheme.TeamFace(hero.TeamSide);
            // Milestone 8.35m: the same warm-gold family the top bar wears while the match waits on
            // the viewer (`MatchHudTheme.ShineGlow`/`NarrationWarmBackground`), read here per hero from
            // `AwaitedInputQuery.IsHeroAwaited` rather than a second "whose turn" guess. The team accent
            // keeps a faint hand in the tint even while shining, so the rail never stops reading as this
            // hero's own - the reserved top edge and the left/right accent border below still carry
            // that identity untouched.
            var frame = hero.IsAwaited
                ? MatchHudTheme.ShineGlow
                : Color.Lerp(MatchHudTheme.PanelBorder, accent, 0.34f);
            var body = hero.IsAwaited
                ? Color.Lerp(MatchHudTheme.NarrationWarmBackground, accent, 0.14f)
                : Color.Lerp(MatchHudTheme.PanelBackground, accent, 0.055f);
            body.a = 0.97f;
            // Milestone 8.35z39. THE RAIL ITSELF wore the marker, and the inset child element
            // that used to wear it went with the treatment that needed it.
            //
            // Milestone 8.35z9 added `gd-rail-awaited-shine` for one reason it stated plainly: the
            // rail wears its gold as its own border, with no child element to find, so the CIRCLING
            // SWEEP needed something here to travel - and it needed that something inset, because a
            // 10px mark centred on the rail's own border put half of itself outside the rail's column
            // and the complete Play Mode run caught it at 280.0 against a 276.5 bound. Both the
            // element and its inset existed to carry a travelling dot.
            //
            // The user retired the travelling dot ("i dont like the little light that goes around"),
            // and the breath that replaced it lights an element's OWN outline in place. The rail's own
            // outline is exactly the gold `frame` written just below, so z39 put the marker on the
            // rail; a child that draws nothing would be a host with no outline to light.
            //
            // Only the edges actually painted with the awaited gold breathe. The reserved top edge and
            // the team accent on the screen-edge side are written after this and are never gold, so
            // the hero's own identity keeps its colour while the frame breathes - the same division of
            // labour milestone 8.35m set up here.
            //
            // Milestone 8.35z47 MOVED THE MARKER OFF THE RAIL, and the paragraph directly
            // above is why it had to move. Counting what that division of labour actually leaves gold:
            // the screen-edge side is the five-unit team accent, the top is the reserved identity
            // edge, and the rail's own border is ONE unit wide - so the breath's whole carrier on a
            // hero tray was a single-unit bottom edge, half a pixel on a real screen. The class now
            // rides `gd-rail-awaited-glow` (see AwaitedGlowBand), a band with enough surface to show a
            // light. The rail still paints its own gold border at rest, unchanged.

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
            rail.style.borderTopWidth = HeroIdentityEdgeWidth;
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
            // Milestone 8.49. The level and, beside it, what defeating the hero is worth - the
            // half-coin, half-heart badge the user asked for at the table: one digit, 1, 2 or 3,
            // that is both the assist coins a Defeat pays and the Life counters it costs.
            var levelRow = HudElements.Row(4f);
            levelRow.name = "gd-hero-level-row";
            levelRow.style.flexWrap = Wrap.NoWrap;
            levelRow.style.alignItems = Align.Center;
            levelRow.style.flexShrink = 1f;
            levelRow.style.minWidth = 0f;
            if (mirrored)
            {
                if (hero.Worth > 0) levelRow.Add(BuildWorthBadge(hero));
                levelRow.Add(level);
            }
            else
            {
                levelRow.Add(level);
                if (hero.Worth > 0) levelRow.Add(BuildWorthBadge(hero));
            }

            identity.Add(levelRow);
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

            // Milestone 8.35z47. THE TRAY'S BREATH NEEDS A SURFACE, and until now it had a
            // sub-pixel one. Added LAST so it draws over the rail's own content rather than under it.
            if (hero.IsAwaited)
            {
                rail.Add(AwaitedGlowBand());
            }

            return rail;
        }

        /// <summary>
        /// Milestone 8.35z47. The awaited hero tray's breathing light: a gold band drawn
        /// just inside the rail's own edge, which the one conductor lights and dims exactly as it
        /// lights a card's ring.
        ///
        /// <para><b>Why this element exists when milestone 8.35z39 deliberately removed one.</b> z39
        /// retired <c>gd-rail-awaited-shine</c> and put the marker class on the RAIL, reasoning that
        /// the rail already wears its gold as its own border so a child that draws nothing would be a
        /// host with no outline to light. That reasoning is sound and the code did exactly what it
        /// said. What it did not account for is HOW MUCH of that outline is left to light: the rail's
        /// border is ONE reference unit, the team accent overwrites the whole screen-edge side at five
        /// units, and the reserved identity edge takes the top - so the breath's entire carrier on a
        /// hero tray was a single-unit bottom edge. A reference unit is one pixel at 2560x1440, so
        /// live that is 0.75 real pixels at 1920x1080 and 0.5 in a typical Editor Game View.
        ///
        /// <para>The breath itself was never broken. Its clock advances every frame, its curve is
        /// right, and its 2.22x luminance swing is real and was measured live. It was simply being
        /// written across half a pixel, which is why the user looked at a lit tray and reported
        /// "Saw no breathing, just solid colors" - a correct report of a treatment with no surface.</para>
        ///
        /// <para><b>It costs no host budget and no layout.</b> The marker class moves here from the
        /// rail rather than being added alongside it, so the number of elements the breath lights at
        /// once is exactly what it was and <see cref="KineticCueUnityView.MaxBreathingShineElements"/>
        /// still bounds the same set. The band is absolutely positioned and ignores picking, so it
        /// takes part in no layout, changes no rail measurement, and can never intercept a click meant
        /// for the tray. The rail keeps its own gold border painted at rest, so a tray that this never
        /// touches looks exactly as it always has.</para>
        /// </summary>
        private static VisualElement AwaitedGlowBand()
        {
            var glow = new VisualElement
            {
                name = "gd-rail-awaited-glow",
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            glow.AddToClassList(MatchHudTheme.AwaitedShineClass);
            glow.style.position = Position.Absolute;
            glow.style.left = 0f;
            glow.style.top = 0f;
            glow.style.right = 0f;
            glow.style.bottom = 0f;
            glow.style.backgroundColor = Color.clear;
            HudElements.Border(glow, MatchHudTheme.ShineGlow, AwaitedGlowBandWidth, 12f);
            return glow;
        }

        /// <summary>
        /// How wide the awaited tray's breathing band is drawn, in reference units. Wide enough that
        /// the breath's swing is carried on pixels a player can actually see at every viewport this
        /// build supports, and narrow enough to sit inside the rail's own nine-unit padding rather
        /// than over its portrait, its name or its counters.
        /// </summary>
        internal const float AwaitedGlowBandWidth = 7f;

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
        /// <summary>
        /// Milestone 8.49. Half a coin and half a heart with the hero's worth in the middle: rulebook
        /// p.13 pays the attacker's friends that many assist coins and takes that many Life counters
        /// from the defeated hero's team, and one number drawn on both halves says both at once. The
        /// left half is the coin's own face colour and the right half the Life counter's red; the two
        /// halves are one rounded disc, so the badge reads as a single token beside the level.
        /// </summary>
        private static VisualElement BuildWorthBadge(HeroOverviewViewModel hero)
        {
            const float size = 16f;
            var badge = new VisualElement { name = "gd-hero-worth", tooltip = hero.WorthAssistiveLabel };
            badge.style.width = size;
            badge.style.height = size;
            badge.style.minWidth = size;
            badge.style.flexShrink = 0f;
            badge.style.flexDirection = FlexDirection.Row;
            badge.style.overflow = Overflow.Hidden;
            HudElements.Border(badge, MatchHudTheme.CoinEdge, 1f, size / 2f);

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

            var digit = HudElements.Text(hero.WorthLabel, 10f, MatchHudTheme.CoinInk, FontStyle.Bold);
            digit.name = "gd-hero-worth-value";
            digit.pickingMode = PickingMode.Ignore;
            digit.style.position = Position.Absolute;
            digit.style.left = 0f;
            digit.style.top = 0f;
            digit.style.width = size;
            digit.style.height = size;
            digit.style.unityTextAlign = TextAnchor.MiddleCenter;
            digit.style.marginLeft = 0f;
            digit.style.marginRight = 0f;
            digit.style.marginTop = 0f;
            digit.style.marginBottom = 0f;
            badge.Add(digit);
            return badge;
        }

        /// <summary>The Life counter's red, for the badge's heart half.</summary>
        private static readonly Color WorthHeartFace = new Color(0.78f, 0.16f, 0.2f, 1f);

        private VisualElement BuildHeroPortrait(HeroOverviewViewModel hero, Color accent)
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

            if (hero.ContentId.IsDefined)
            {
                var token = HeroTokenArtwork.ResolveToken(hero.ContentId, hero.TeamSide, out var portrait);
                if (portrait != null)
                {
                    // Match the board's baked-first/neutral-second decision and wider neutral rim.
                    var faceSize = HeroPortraitSize * token.FaceRadius / RuntimeTeamIdentity.TeamBakedFaceRadius;
                    icon.style.width = faceSize;
                    icon.style.height = faceSize;
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
            revealAnchors.Add(hero, medallion);
            HudElements.OnPointedAt(medallion, pointedAt => SetHighlight(pointedAt ? hero : null));
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
}
