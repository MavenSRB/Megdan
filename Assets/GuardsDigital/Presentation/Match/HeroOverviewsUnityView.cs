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
        // Milestone 9.32g: the hero whose card back a withheld facedown card's hover draws.
        private readonly Dictionary<object, HeroContentId> withheldOwners = new Dictionary<object, HeroContentId>();

        private readonly Dictionary<object, VisualElement> revealAnchors =
            new Dictionary<object, VisualElement>();
        private readonly Dictionary<HeroId, VisualElement> heroRails =
            new Dictionary<HeroId, VisualElement>();
        private readonly Dictionary<HeroId, Color> heroRailRestingTopColors =
            new Dictionary<HeroId, Color>();

        private object highlighted;
        private HeroId highlightedHero;

        // Milestone 9.30h. The SPELLBOOK drawer (approved mockups B and C): a host spanning the open
        // area between the two rails, holding one panel sized to its content. Which hero's book is
        // open is a reading preference of this surface, like the hover - no command, no projection.
        private readonly VisualElement spellbookHost;
        private readonly VisualElement spellbookPanel;
        private HeroId openSpellbookHero;

        private readonly Func<IMatchReadModel> colourRead;

        public HeroCardInspectionUnityView CardInspection { get; }

        public HeroOverviewsUnityView(Func<IMatchReadModel> read = null)
        {
            colourRead = read;
            Root = new VisualElement { name = "gd-hero-rails" };
            Root.style.position = Position.Absolute;
            Root.pickingMode = PickingMode.Ignore;
            redColumn = Column("gd-hero-rail-red");
            blueColumn = Column("gd-hero-rail-blue");
            preview = new VisualElement { name = "gd-turn-slot-preview" };
            preview.style.position = Position.Absolute;
            preview.style.display = DisplayStyle.None;
            preview.pickingMode = PickingMode.Ignore;
            spellbookHost = new VisualElement { name = "gd-spellbook-drawer-host", pickingMode = PickingMode.Ignore };
            spellbookHost.style.position = Position.Absolute;
            spellbookHost.style.display = DisplayStyle.None;
            spellbookHost.style.flexDirection = FlexDirection.Column;
            spellbookHost.style.alignItems = Align.Center;
            spellbookPanel = HudElements.Panel(MatchHudTheme.PanelBackground);
            spellbookPanel.name = "gd-spellbook-drawer";
            spellbookPanel.style.flexDirection = FlexDirection.Column;
            // Milestone 9.30h1: the whole room between the rails, so full-size faces wrap into as
            // many to a row as fit, and never taller than that room - the body scrolls instead.
            spellbookPanel.style.width = Length.Percent(100f);
            spellbookPanel.style.maxWidth = Length.Percent(100f);
            spellbookPanel.style.maxHeight = Length.Percent(100f);
            HudElements.Padding(spellbookPanel, SpellbookDrawerPadding);
            HudElements.Border(spellbookPanel, MatchHudTheme.PanelBorder, 2f, 12f);
            spellbookHost.Add(spellbookPanel);
            Root.Add(redColumn);
            Root.Add(blueColumn);
            Root.Add(spellbookHost);
            Root.Add(preview);
            CardInspection = new HeroCardInspectionUnityView(read);
            Root.Add(CardInspection.Root);
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

        /// <summary>Milestone 9.30h. The hero whose SPELLBOOK drawer is open, or undefined when none is.</summary>
        public HeroId OpenSpellbookHeroId => openSpellbookHero;

        /// <summary>Milestone 9.30h. The drawer's panel - what blocks the board while it is open.</summary>
        public VisualElement SpellbookDrawer => spellbookPanel;

        /// <summary>Milestone 9.30h. Whether the SPELLBOOK drawer is drawn.</summary>
        public bool IsSpellbookOpen => openSpellbookHero.IsDefined;

        /// <summary>
        /// Milestone 9.30h. Opens <paramref name="heroId"/>'s SPELLBOOK drawer - what pressing the
        /// plaque's chip does, for a driver with no pointer - or refuses a hero who owns no book.
        /// Opening submits nothing and changes no match state.
        /// </summary>
        public bool OpenSpellbook(HeroId heroId)
        {
            var hero = Model?.Heroes.FirstOrDefault(value => value.HeroId.Equals(heroId));
            if (hero?.Spellbook == null)
            {
                return false;
            }

            CardInspection.Close();
            openSpellbookHero = heroId;
            RebuildSpellbook();
            return true;
        }

        /// <summary>Opens the one inspection surface used by both a board token and its rail.</summary>
        public bool OpenCardInspection(HeroId heroId)
        {
            var hero = Model?.Heroes.FirstOrDefault(value => value.HeroId.Equals(heroId));
            if (hero == null || !CardInspection.Open(hero.HeroId, hero.HeroDisplayName))
            {
                return false;
            }

            CloseSpellbook();
            SetHighlight(null);
            return true;
        }

        /// <summary>Milestone 9.30h. `CLOSE`: the drawer goes away; nothing else changes.</summary>
        public void CloseSpellbook()
        {
            openSpellbookHero = default;
            RebuildSpellbook();
        }

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
                : model != null && withheldOwners.TryGetValue(model, out var owner)
                    ? BuildCardBack(owner)
                    : card == null ? null : BuildFace(card);
            if (face == null)
            {
                highlighted = null;
                preview.style.display = DisplayStyle.None;
            }
            else
            {
                if (card != null && revealAnchors.TryGetValue(model, out var anchor))
                {
                    CastableSpellHoverView.Attach(face, card.CastableSpells);
                }
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

        /// <summary>
        /// Milestone 9.32g. A card's hover face; a facedown card the seat may read is its own face,
        /// dimmed and hatched (approved mockup B).
        /// </summary>
        private static VisualElement BuildFace(TrayCardViewModel card)
        {
            var face = DirectorHandsUnityView.BuildStandaloneCard(card);
            if (card.IsFacedown)
            {
                var dim = new VisualElement { name = "gd-facedown-dim" };
                dim.pickingMode = PickingMode.Ignore;
                dim.style.position = Position.Absolute;
                dim.style.left = 0f;
                dim.style.top = 0f;
                dim.style.right = 0f;
                dim.style.bottom = 0f;
                dim.style.backgroundColor = new Color(0.04f, 0.04f, 0.05f, 0.42f);
                dim.Add(new FacedownHatch(FacedownHatch.Weight.Full));
                face.Add(dim);
            }

            return face;
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
            // Milestone 9.36. A rail mini - a turn slot, the current card, a discard - sits inside its
            // column, so a face placed off the mini's own edge drew over the owner's tray and the plaque
            // below it. Its face is placed off the rail's edge instead. The hero profile keeps 8.42b's
            // placement beside the portrait.
            var beside = model is HeroOverviewViewModel
                ? anchor
                : RailColumnOf(element)?.worldBound ?? anchor;
            var onLeft = beside.center.x < width / 2f;
            var card = CardOf(model);
            var spellShelfHeight = CastableSpellHoverView.HeightFor(card?.CastableSpells);
            var topOverhang = spellShelfHeight > 0f
                ? spellShelfHeight + CastableSpellHoverView.Gap
                : 0f;
            var left = onLeft
                ? beside.xMax + 10f
                : beside.xMin - faceWidth - 10f;
            preview.style.left = Mathf.Clamp(
                left,
                0f,
                Mathf.Max(0f, width - faceWidth));
            preview.style.top = Mathf.Clamp(
                anchor.yMin - 40f,
                topOverhang,
                Mathf.Max(topOverhang, height - faceHeight - 8f));
        }

        /// <summary>Milestone 9.36: the rail column an element is drawn in, or null outside both.</summary>
        private VisualElement RailColumnOf(VisualElement element)
        {
            for (var ancestor = element; ancestor != null; ancestor = ancestor.parent)
            {
                if (ReferenceEquals(ancestor, redColumn) || ReferenceEquals(ancestor, blueColumn))
                {
                    return ancestor;
                }
            }

            return null;
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

            // Milestone 9.30h. The drawer opens over the board between the two rails, below the
            // status strip the rails themselves start under, as the decision window does.
            var left = red.xMax + SpellbookDrawerGap;
            spellbookHost.style.left = left;
            spellbookHost.style.top = Mathf.Min(red.y, blue.y);
            spellbookHost.style.width = Mathf.Max(0f, blue.xMin - SpellbookDrawerGap - left);
            // Milestone 9.30h1: as tall as the rails' own columns - down to the hand's band, which the
            // drawer never covers - so a book of full-size faces scrolls rather than running under it.
            spellbookHost.style.height = Mathf.Max(
                0f, Mathf.Min(red.yMax, blue.yMax) - Mathf.Min(red.y, blue.y));
            Place(CardInspection.Root, new Rect(left, Mathf.Min(red.y, blue.y),
                Mathf.Max(0f, blue.xMin - SpellbookDrawerGap - left),
                Mathf.Max(0f, Mathf.Min(red.yMax, blue.yMax) - Mathf.Min(red.y, blue.y))));
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
            withheldOwners.Clear();
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
                rail.AddManipulator(new Clickable(() =>
                {
                    if (!OpenCardInspection(hero.HeroId))
                        HeroSelected?.Invoke(hero.HeroId);
                }));
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

            // Milestone 9.30h. An open drawer follows the projection it was opened over: redrawn from
            // the new model, and closed if that hero no longer has a book to show.
            RebuildSpellbook();
            CardInspection.Refresh();
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
            var accent = colourRead == null ? MatchHudTheme.TeamFace(hero.TeamSide)
                : PlayerAccent.For(colourRead().Participants, hero.HeroId);
            // Milestone 8.35m: the same warm-gold family the top bar wears while the match waits on
            // the viewer (`MatchHudTheme.ShineGlow`/`NarrationWarmBackground`), read here per hero from
            // `AwaitedInputQuery.IsHeroAwaited` rather than a second "whose turn" guess. The team accent
            // keeps a faint hand in the tint even while shining, so the rail never stops reading as this
            // hero's own - the reserved top edge and the left/right accent border below still carry
            // that identity untouched.
            var frame = colourRead != null ? accent : hero.IsAwaited
                ? MatchHudTheme.ShineGlow
                : Color.Lerp(MatchHudTheme.PanelBorder, accent, 0.34f);
            var body = colourRead != null ? Color.Lerp(MatchHudTheme.PanelBackground, accent, hero.IsAwaited ? 0.18f : 0.08f) : hero.IsAwaited
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
            // Milestone 9.30h1, measured at 2560x540: the chips row TAKES the strip's width beside the
            // coin (basis 0, grow 1) instead of sizing itself to its chips on one line and then
            // shrinking. Sized that way it was measured on one line and drawn wrapped onto two once
            // "NO ITEMS" and the SPELLBOOK chip just failed to fit (the strip was 32 tall around a
            // two-line row), so the chip hung out of the strip. Taking the width, it wraps exactly once,
            // at the width it is drawn at, and the strip's line is the row it holds. It still reads
            // from the screen edge, the coin still stands inward of it, and nothing moves on a line.
            itemChips.style.flexBasis = 0f;
            itemChips.style.flexGrow = 1f;
            itemChips.style.flexShrink = 1f;
            itemChips.style.minWidth = 0f;
            foreach (var bonus in hero.ItemBonuses)
            {
                itemChips.Add(BuildPrintedItemBonus(bonus));
            }

            if (hero.ItemBonuses.Count == 0)
            {
                // Milestone 9.30h1: one reading on one line, standing on the strip's own line height
                // (the coin's), so a line it starts - the line above a SPELLBOOK chip that wrapped, at
                // 2560x540 - is exactly one strip line. It was the one word in the strip free to wrap.
                var noItems = HudElements.Text("NO ITEMS", 10f, MatchHudTheme.MutedText);
                noItems.name = "gd-no-items";
                noItems.style.whiteSpace = WhiteSpace.NoWrap;
                noItems.style.height = StripCardHeight;
                noItems.style.marginTop = 0f;
                noItems.style.marginBottom = 0f;
                noItems.style.unityTextAlign = TextAnchor.MiddleCenter;
                itemChips.Add(noItems);
            }

            // Milestone 8.15: the unlocked Ultimate, faceup beside the dashboard on the table and
            // therefore on the plaque here - the one card a hero owns that is neither in the round the
            // tray draws nor in the pile below. It is built by the same mini-card builder every other
            // rail card is, revealing the same full face on a pointer rest, and it is only drawn while
            // a card actually stands in rulebook p.16's area.
            //
            // Milestone 9.30a, by the user's 2026-10-01 decision (approved mockup D): it is a small
            // card sitting in the items strip, for every hero, rather than a captioned row of its own.
            // It joins the chips rather than the strip, so it wraps with them and can never push the
            // coin off the line, and it is drawn no taller than the coin that already sets the strip's
            // line. It keeps the coin's side of the item chips, so the mirrored rail mirrors it.
            if (hero.UltimateCard != null)
            {
                var ultimate = BuildMiniCard(
                    "gd-ultimate-card",
                    hero.UltimateCard,
                    hero.UltimateCard.AssistiveLabel,
                    hero.UltimateCard);
                ultimate.style.width = StripCardWidth;
                ultimate.style.height = StripCardHeight;
                // The user's "just purple card sitting there": the profile states no colour for an
                // Ultimate, so the plaque draws the printed Ultimate purple rather than an empty slot.
                ultimate.style.backgroundColor = MatchHudTheme.UltimateFace;

                // Milestone 9.30g (approved mockup D): "1/3" beside the purple card - the team's public
                // count toward a printed team victory (Gydion's Wish). It stays on the card's outer
                // side, so the mirrored rail mirrors it with the card.
                Label tally = null;
                if (hero.UltimateTallyLabel != null)
                {
                    tally = HudElements.Text(hero.UltimateTallyLabel, 10f, MatchHudTheme.PrimaryText);
                    tally.name = "gd-ultimate-tally";
                    // One reading on one line, as every word in the strip is (9.30h1).
                    tally.style.whiteSpace = WhiteSpace.NoWrap;
                    tally.tooltip = hero.UltimateTallyAssistiveLabel;
                    tally.style.unityTextAlign = TextAnchor.MiddleCenter;
                }

                if (mirrored)
                {
                    ultimate.style.marginRight = StripCardGap;
                    itemChips.Insert(0, ultimate);
                    if (tally != null)
                    {
                        tally.style.marginRight = StripCardGap;
                        itemChips.Insert(0, tally);
                    }
                }
                else
                {
                    ultimate.style.marginLeft = StripCardGap;
                    itemChips.Add(ultimate);
                    if (tally != null)
                    {
                        tally.style.marginLeft = StripCardGap;
                        itemChips.Add(tally);
                    }
                }
            }

            // Milestone 9.30h, approved mockup D: "SPELLBOOK n | m" (in the book | removed), the last of
            // the strip's chips - after the Ultimate card - so it wraps onto its own line when the
            // items fill one; mirrored, it is the first. Drawn only for a hero who owns a book.
            if (hero.Spellbook != null)
            {
                var chip = BuildSpellbookChip(hero.HeroId, hero.Spellbook);
                if (mirrored)
                {
                    chip.style.marginRight = StripCardGap;
                    itemChips.Insert(0, chip);
                }
                else
                {
                    chip.style.marginLeft = StripCardGap;
                    itemChips.Add(chip);
                }
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
            // Drawn only while something is held, on the terms the strip's Ultimate card is: p.8 returns
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
                // numbers read inward, the way the identity block does.
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
                var element = slot.IsWithheld
                    ? BuildWithheldMiniCard("gd-turn-slot", slot.Color, slot.AssistiveLabel, slot, hero.ContentId)
                    : slot.IsOccupied
                    ? BuildMiniCard("gd-turn-slot", slot.PlayedCard, slot.AssistiveLabel, slot)
                    : BuildEmptyMiniCard("gd-turn-slot", slot.AssistiveLabel);

                outline.Add(element);
                var turnMarker = BuildTurnSlotMarker(
                    slot.TurnSlotMarker, "gd-turn-slot-marker");
                if (turnMarker != null)
                {
                    // Milestone 9.20: centred on the slot, across and down, over the card that may
                    // stand in it - the user's "center them on card slots". The marker covers the
                    // card itself and draws its glyph centred inside, so the glyph's centre is the
                    // card's wherever layout snaps the two to screen pixels; a marker sized to its
                    // glyph and centred by margins snapped differently from the card and stood
                    // half a pixel off it (the axe, which the user saw).
                    turnMarker.style.position = Position.Absolute;
                    turnMarker.style.left = 0f;
                    turnMarker.style.top = 0f;
                    turnMarker.style.right = 0f;
                    turnMarker.style.bottom = 0f;
                    element.Add(turnMarker);
                }
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
            // decision, and it is the idiom the Ultimate row (removed at 9.30a) used: a short fixed word
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

            // Milestone 9.32g: a facedown card this seat may not name, by its known colour (mockup B).
            foreach (var withheld in hero.WithheldDiscards)
            {
                pile.Add(BuildWithheldMiniCard(
                    "gd-discard-card", withheld.Color, withheld.AssistiveLabel, withheld, withheld.OwnerContentId));
            }

            if (hero.DiscardCount == 0)
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
                rail.Add(AwaitedGlowBand(colourRead != null ? (Color?)accent : null));
            }

            return rail;
        }

        /// <summary>The SPELLBOOK drawer's inside padding and its distance from each rail.</summary>
        private const float SpellbookDrawerPadding = 18f;

        private const float SpellbookDrawerGap = 16f;

        /// <summary>The space between two spell faces in the drawer.</summary>
        private const float SpellbookFaceGap = 10f;

        /// <summary>A stack of backs is drawn at the drawer's own face size (9.30h1).</summary>
        private const float SpellbookBackScale = SpellCardFootprint.DrawerScale;

        /// <summary>
        /// Milestone 9.30h, approved mockup D. The strip's "SPELLBOOK n | m" chip: gold words on the
        /// coin's dark gold, the mockup's chip, and a control - pressing it opens the drawer. Named
        /// <c>gd-spellbook-chip</c>.
        /// </summary>
        private Button BuildSpellbookChip(HeroId heroId, SpellbookViewModel spellbook)
        {
            var chip = new Button(() => OpenSpellbook(heroId))
            {
                name = "gd-spellbook-chip",
                text = spellbook.ChipLabel,
                tooltip = spellbook.ChipAssistiveLabel
            };
            ApplySpellbookChipStyle(chip, StripCardHeight - 4f, inItemsStrip: true);
            return chip;
        }

        /// <summary>
        /// Milestone 9.30k. The one visual treatment for a spellbook control, shared by the hero-tray
        /// chip and the viewed-hand shortcut so the second control is not a generic grey text button.
        /// </summary>
        internal static void ApplySpellbookChipStyle(Button chip, float height, bool inItemsStrip)
        {
            if (chip == null) throw new ArgumentNullException(nameof(chip));
            chip.style.fontSize = 10f;
            chip.style.unityFontStyleAndWeight = FontStyle.Bold;
            chip.style.color = MatchHudTheme.CoinFace;
            chip.style.backgroundColor = new Color(0.184f, 0.141f, 0.063f, 1f);
            chip.style.whiteSpace = WhiteSpace.NoWrap;
            chip.style.flexShrink = 0f;
            chip.style.height = height;
            chip.style.marginTop = inItemsStrip ? 2f : 0f;
            chip.style.marginBottom = inItemsStrip ? 2f : 0f;
            chip.style.paddingLeft = 7f;
            chip.style.paddingRight = 7f;
            chip.style.paddingTop = 0f;
            chip.style.paddingBottom = 0f;
            chip.style.letterSpacing = 0.6f;
            chip.style.unityTextAlign = TextAnchor.MiddleCenter;
            HudElements.Border(chip, new Color(0.753f, 0.706f, 0.522f, 1f), 1f, 4f);
            HudElements.ApplyButtonMotion(chip);
        }

        /// <summary>
        /// Milestone 9.30h, approved mockups B and C. Redraws the open SPELLBOOK drawer from the
        /// current model, or hides it: SPELLBOOK over the hero's name and the seat's sentence, CLOSE,
        /// "IN THE BOOK - n" as the faces this seat's projection names or the backs it only counts,
        /// and "REMOVED, FACEUP - n" as faces for everyone.
        /// </summary>
        private void RebuildSpellbook()
        {
            spellbookPanel.Clear();
            var hero = openSpellbookHero.IsDefined
                ? Model?.Heroes.FirstOrDefault(value => value.HeroId.Equals(openSpellbookHero))
                : null;
            if (hero?.Spellbook == null)
            {
                openSpellbookHero = default;
                spellbookHost.style.display = DisplayStyle.None;
                return;
            }

            var book = hero.Spellbook;
            spellbookHost.style.display = DisplayStyle.Flex;
            spellbookHost.BringToFront();

            var header = new VisualElement { name = "gd-spellbook-header" };
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.FlexStart;
            header.style.flexShrink = 0f;
            var titles = HudElements.Column(2f);
            titles.style.flexGrow = 1f;
            titles.style.alignItems = Align.Center;
            var eyebrow = HudElements.Text(book.Eyebrow, 11f, MatchHudTheme.MutedText, FontStyle.Bold);
            eyebrow.name = "gd-spellbook-eyebrow";
            eyebrow.style.letterSpacing = 0.8f;
            var title = HudElements.Text(book.Title, 22f, MatchHudTheme.PrimaryText, FontStyle.Bold);
            title.name = "gd-spellbook-title";
            var subtitle = HudElements.Text(book.Subtitle, 13f, MatchHudTheme.PrimaryText);
            subtitle.name = "gd-spellbook-subtitle";
            titles.Add(eyebrow);
            titles.Add(title);
            titles.Add(subtitle);
            var close = HudElements.Control(book.CloseLabel, CloseSpellbook, 28f);
            close.name = "gd-spellbook-close";
            close.style.fontSize = 11f;
            header.Add(titles);
            header.Add(close);
            spellbookPanel.Add(header);

            // Milestone 9.30h1. The sections scroll under a fixed header when the book is taller than
            // the room the drawer has.
            var body = new ScrollView(ScrollViewMode.Vertical) { name = "gd-spellbook-body" };
            body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            body.style.flexShrink = 1f;
            body.style.flexGrow = 0f;
            body.style.minHeight = 0f;
            spellbookPanel.Add(body);

            body.Add(SpellbookSectionLabel("gd-spellbook-in-book-label", book.InBookLabel));
            var inBook = SpellbookRow("gd-spellbook-in-book");
            foreach (var spell in book.InBookFaces)
            {
                inBook.Add(SpellbookFace(spell));
            }

            foreach (var stack in book.InBookBacks)
            {
                inBook.Add(SpellbookBackStack(stack));
            }

            body.Add(inBook);
            body.Add(SpellbookSectionLabel("gd-spellbook-removed-label", book.RemovedLabel));
            var removed = SpellbookRow("gd-spellbook-removed");
            foreach (var spell in book.RemovedFaces)
            {
                removed.Add(SpellbookFace(spell));
            }

            body.Add(removed);
        }

        private static Label SpellbookSectionLabel(string name, string text)
        {
            var label = HudElements.Text(text, 11f, MatchHudTheme.MutedText, FontStyle.Bold);
            label.name = name;
            label.style.letterSpacing = 0.8f;
            label.style.marginTop = 14f;
            label.style.marginBottom = 8f;
            return label;
        }

        private static VisualElement SpellbookRow(string name)
        {
            var row = new VisualElement { name = name };
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.FlexStart;
            return row;
        }

        /// <summary>One readable drawer face; spellbook cards do not open hover previews.</summary>
        private VisualElement SpellbookFace(SpellCardViewModel spell)
        {
            var host = new VisualElement { name = "gd-spellbook-spell", tooltip = spell.AssistiveLabel };
            host.style.marginRight = SpellbookFaceGap;
            host.style.marginBottom = SpellbookFaceGap;
            host.Add(SpellCardFaceView.Build(spell, SpellCardFootprint.DrawerScale));
            HudElements.ApplyButtonMotion(host);
            return host;
        }

        /// <summary>One stack of facedown backs with its count - mockup C's `x7`. It reveals nothing.</summary>
        private static VisualElement SpellbookBackStack(SpellBackStackViewModel stack)
        {
            var host = new VisualElement { name = "gd-spellbook-back-stack", tooltip = stack.AssistiveLabel };
            host.style.marginRight = SpellbookFaceGap * 2f;
            host.style.marginBottom = SpellbookFaceGap;
            host.userData = stack.Back;
            // Two offset edges under the top back read as a pile, as the mockup draws it.
            for (var layer = Mathf.Min(2, stack.Count - 1); layer >= 1; layer--)
            {
                var under = SpellCardFaceView.BuildBack(stack.Back, SpellbookBackScale);
                under.name = "gd-spell-back-under";
                under.style.position = Position.Absolute;
                under.style.left = 4f * layer;
                under.style.top = 4f * layer;
                host.Add(under);
            }

            host.Add(SpellCardFaceView.BuildBack(stack.Back, SpellbookBackScale));
            var count = HudElements.Text(stack.CountLabel, 12f, MatchHudTheme.CoinFace, FontStyle.Bold);
            count.name = "gd-spellbook-back-count";
            count.style.position = Position.Absolute;
            count.style.right = -8f;
            count.style.top = -8f;
            count.style.paddingLeft = 6f;
            count.style.paddingRight = 6f;
            count.style.backgroundColor = new Color(0.184f, 0.141f, 0.063f, 1f);
            HudElements.Border(count, new Color(0.753f, 0.706f, 0.522f, 1f), 1f, 10f);
            host.Add(count);
            return host;
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
        private static VisualElement AwaitedGlowBand(Color? accent = null)
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
            if (accent.HasValue) glow.userData = accent.Value;
            HudElements.Border(glow, accent ?? MatchHudTheme.ShineGlow, AwaitedGlowBandWidth, 12f);
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
        /// <remarks>
        /// Milestone 8.53 moved the badge itself to <see cref="HudElements.WorthBadge"/>, where the
        /// victory screen's own rows draw the same one. Two builders for one token would be two things
        /// to keep looking alike; this stays as the rail's own call into the shared one.
        /// </remarks>
        private static VisualElement BuildWorthBadge(HeroOverviewViewModel hero) =>
            HudElements.WorthBadge(
                hero.WorthLabel,
                hero.WorthAssistiveLabel,
                coinOnly: hero.WorthRepresentsAssistCoinsOnly);

        private VisualElement BuildHeroPortrait(HeroOverviewViewModel hero, Color accent)
        {
            var medallion = BuildSharedHeroPortrait(hero, accent);
            revealAnchors.Add(hero, medallion);
            HudElements.OnPointedAt(medallion, pointedAt => SetHighlight(pointedAt ? hero : null));
            medallion.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0) evt.StopPropagation();
            });
            medallion.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != 0) return;
                evt.StopPropagation();
                if (!OpenCardInspection(hero.HeroId)) HeroSelected?.Invoke(hero.HeroId);
            });
            return medallion;
        }

        internal static VisualElement BuildSharedHeroPortrait(HeroOverviewViewModel hero, Color accent, float scale = 1f)
        {
            var medallion = new VisualElement { name = "gd-hero-medallion" };
            medallion.style.width = (HeroMedallionSize * scale);
            medallion.style.height = (HeroMedallionSize * scale);
            medallion.style.flexShrink = 0f;
            medallion.style.alignItems = Align.Center;
            medallion.style.justifyContent = Justify.Center;
            medallion.style.backgroundColor = Color.Lerp(MatchHudTheme.PanelBackground, accent, 0.30f);
            HudElements.Border(medallion, accent, 2f, (HeroMedallionSize * scale) / 2f);

            var icon = new VisualElement { name = "gd-hero-icon" };
            icon.style.width = (HeroPortraitSize * scale);
            icon.style.height = (HeroPortraitSize * scale);
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
                (HeroPortraitSize * scale) / 2f);

            if (hero.ContentId.IsDefined)
            {
                var token = HeroTokenArtwork.ResolveToken(hero.ContentId, hero.TeamSide, out var portrait);
                if (portrait != null)
                {
                    // Match the board's baked-first/neutral-second decision and wider neutral rim.
                    var faceSize = (HeroPortraitSize * scale) * token.FaceRadius / RuntimeTeamIdentity.TeamBakedFaceRadius;
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
        internal const float MiniCardWidth = 24f;

        internal const float MiniCardHeight = 34f;

        private const float MiniCardGap = 4f;

        /// <summary>
        /// Milestone 9.30a: the unlocked Ultimate's footprint inside the items strip - the mini card's
        /// proportions at the coin's height, so the card never makes the strip's line taller than the
        /// coin already does - and its distance from the item chip beside it, which is the chips row's.
        /// </summary>
        private const float StripCardHeight = CoinDiscSize;

        private const float StripCardWidth = 17f;

        private const float StripCardGap = 6f;

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
            if (card.IsFacedown)
            {
                element.Add(new FacedownHatch(FacedownHatch.Weight.Mini));
            }

            return element;
        }

        /// <summary>
        /// Milestone 9.32g. A facedown card this seat may not name: the same hatched mini every seat
        /// sees (approved mockup B, amended 2026-10-02 - "show them the same thing that his seat sees"),
        /// in its known colour. Its hover draws the owner's card back, never a face.
        /// </summary>
        private VisualElement BuildWithheldMiniCard(
            string name, CardColor color, string assistiveLabel, object revealed, HeroContentId owner)
        {
            var element = BuildMiniCardShape(name);
            element.style.backgroundColor = MatchHudTheme.Face(color);
            HudElements.Border(element, MatchHudTheme.PanelBorder, 1f, 3f);
            ConfiguredCardFaceView.AddExteriorOutline(element, 1.5f, 4.5f);
            var band = new VisualElement { name = "gd-mini-card-band" };
            band.style.height = 4f;
            band.style.marginLeft = 3f;
            band.style.marginRight = 3f;
            band.style.marginTop = 3f;
            var ink = MatchHudTheme.FaceText(color);
            band.style.backgroundColor = new Color(ink.r, ink.g, ink.b, 0.30f);
            element.Add(band);
            element.Add(new FacedownHatch(FacedownHatch.Weight.Mini));
            element.tooltip = assistiveLabel;
            revealAnchors[revealed] = element;
            withheldOwners[revealed] = owner;
            HudElements.OnPointedAt(element, entered => SetHighlight(entered ? revealed : null));
            return element;
        }

        /// <summary>
        /// Milestone 9.32g. The hover of a withheld facedown card: the owner's card back at the size a
        /// face would draw, the prepared back art where the checkout has it (private, optional - the
        /// same rule a hero profile follows), the HUD's own card-back colour where it does not.
        /// </summary>
        internal static VisualElement BuildCardBack(HeroContentId owner)
        {
            var element = new VisualElement { name = "gd-card-back-preview" };
            element.style.width = CardFootprint.StandardWidth;
            element.style.height = CardFootprint.StandardHeight;
            element.style.backgroundColor = MatchHudTheme.CardBack;
            HudElements.Border(element, MatchHudTheme.PanelBorder, 2f, 8f);
            var registered = RegisteredHeroRoster.ForThisBuild.FirstOrDefault(value => value.ContentId.Equals(owner));
            var texture = string.IsNullOrEmpty(registered.HeroIdValue)
                ? null
                : Resources.Load<Texture2D>("Private/CardBacks/" + registered.HeroIdValue);
            if (texture != null)
            {
                element.Add(new Image
                {
                    name = "gd-card-back-art",
                    image = texture,
                    scaleMode = ScaleMode.StretchToFill,
                    pickingMode = PickingMode.Ignore,
                    style = { flexGrow = 1f }
                });
            }

            element.tooltip = "Facedown card";
            return element;
        }

        /// <summary>
        /// Milestone 9.32g. The diagonal hatch a facedown card wears (approved mockup B): dark lines over
        /// the card's own colour, drawn rather than imaged so it scales with the card and takes no
        /// artwork.
        ///
        /// <para>Milestone 9.36. Two weights. On a mini (a turn slot or the discard pile) the fine
        /// hatch read as plain gold at normal viewing, so a mini wears a bolder diagonal hatch with
        /// fewer, thicker strokes (the user, 2026-10-02: "a bolder diagonal hatch with fewer, thicker
        /// strokes. approved."); the full-size face a hover reveals keeps the fine hatch.</para>
        /// </summary>
        internal sealed class FacedownHatch : VisualElement
        {
            internal enum Weight
            {
                /// <summary>A rail mini: few, thick, dark strokes.</summary>
                Mini,

                /// <summary>A full-size face: the fine hatch 9.32g drew everywhere.</summary>
                Full
            }

            internal FacedownHatch(Weight weight)
            {
                HatchWeight = weight;
                name = "gd-facedown-hatch";
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0f;
                style.top = 0f;
                style.right = 0f;
                style.bottom = 0f;
                generateVisualContent += OnGenerateVisualContent;
            }

            internal Weight HatchWeight { get; }

            /// <summary>How far apart successive strokes start along the card's edge.</summary>
            internal static float SpacingOf(Weight weight) => weight == Weight.Mini ? 10f : 4f;

            internal static float LineWidthOf(Weight weight) => weight == Weight.Mini ? 3f : 1f;

            internal static Color StrokeColorOf(Weight weight) =>
                new Color(0.10f, 0.085f, 0.07f, weight == Weight.Mini ? 0.85f : 0.62f);

            /// <summary>
            /// The strokes the hatch draws over <paramref name="rect"/>: lines rising left to right at
            /// 45 degrees, each clipped to the card. Exposed so the drawn geometry is testable without
            /// a renderer.
            /// </summary>
            internal static IReadOnlyList<KeyValuePair<Vector2, Vector2>> Strokes(Rect rect, Weight weight)
            {
                var strokes = new List<KeyValuePair<Vector2, Vector2>>();
                if (rect.width <= 1f || rect.height <= 1f)
                {
                    return strokes;
                }

                var spacing = SpacingOf(weight);
                for (var start = -rect.height; start < rect.width; start += spacing)
                {
                    var x0 = rect.xMin + start;
                    var y0 = rect.yMax;
                    var x1 = x0 + rect.height;
                    var y1 = rect.yMin;
                    if (x0 < rect.xMin)
                    {
                        y0 -= rect.xMin - x0;
                        x0 = rect.xMin;
                    }

                    if (x1 > rect.xMax)
                    {
                        y1 += x1 - rect.xMax;
                        x1 = rect.xMax;
                    }

                    if (x1 <= x0)
                    {
                        continue;
                    }

                    strokes.Add(new KeyValuePair<Vector2, Vector2>(new Vector2(x0, y0), new Vector2(x1, y1)));
                }

                return strokes;
            }

            private void OnGenerateVisualContent(MeshGenerationContext context)
            {
                var strokes = Strokes(contentRect, HatchWeight);
                if (strokes.Count == 0)
                {
                    return;
                }

                var painter = context.painter2D;
                painter.strokeColor = StrokeColorOf(HatchWeight);
                painter.lineWidth = LineWidthOf(HatchWeight);
                foreach (var stroke in strokes)
                {
                    painter.BeginPath();
                    painter.MoveTo(stroke.Key);
                    painter.LineTo(stroke.Value);
                    painter.Stroke();
                }
            }
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

        /// <summary>
        /// The marker's fixed footprint in the rail header's upper right. Public since milestone 8.55
        /// so a live check can assert the badge is still DRAWN at the footprint it reserves: what 8.55
        /// changes is the reading inside the marker, and the one thing it may not change is this.
        /// </summary>
        public const float CurrentCardWidth = 46f;

        public const float CurrentCardHeight = 52f;

        /// <summary>
        /// The badge's type size for a one- or two-character reading - `6`, `13` - unchanged since
        /// milestone 8.7a, and still what every card with no Initiative item draws.
        /// </summary>
        public const float InitiativeBadgeFontSize = 20f;

        /// <summary>
        /// Milestone 8.55: the size the three-character split `6+1` draws at.
        ///
        /// <para>The marker may not grow for it - 8.42a's acceptance keeps the badge's geometry, and a
        /// wider marker would move the hero's name and the item strip beside it - so the one thing
        /// that gives is the type. The step is measured on the live rail rather than guessed: at 20 a
        /// three-character reading draws 40 of the 42 units inside the marker's border, and as little
        /// as 40 of 40 at a squat viewport, which is a badge with no margin at all rather than one
        /// that fits. At 18 it draws 36.</para>
        /// </summary>
        public const float SplitInitiativeBadgeFontSize = 18f;

        /// <summary>
        /// The same for a FOUR-character reading - `11+1`, `13+2`: a two-digit printed Initiative with
        /// items, which p.7's Initiatives make an ordinary face rather than a defensive case (the live
        /// rail check reads exactly `11+1`). Measured: 50 units at 20, 42 at 16, 36.75 at 14.
        /// </summary>
        public const float WideSplitInitiativeBadgeFontSize = 14f;

        /// <summary>
        /// The type size the badge draws <paramref name="label"/> at, by how long a reading it is. One
        /// place decides it so a check can read the rule rather than a laid-out label's pixels.
        /// </summary>
        public static float InitiativeBadgeFontSizeFor(string label) =>
            string.IsNullOrEmpty(label) || label.Length <= 2 ? InitiativeBadgeFontSize :
            label.Length == 3 ? SplitInitiativeBadgeFontSize : WideSplitInitiativeBadgeFontSize;

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
                return AddCurrentTurnMarker(slot, hero);
            }

            var ink = current.IsFacedown
                ? MatchHudTheme.PrimaryText
                : MatchHudTheme.FaceText(current.Color);
            var marker = new VisualElement { name = "gd-current-card" };
            marker.style.flexGrow = 1f;
            marker.style.flexDirection = FlexDirection.Column;
            marker.style.alignItems = Align.Center;
            marker.style.justifyContent = Justify.Center;
            // Milestone 8.93f: with a live-turn rune standing in the slot, the card's Initiative
            // sits below it rather than under it.
            marker.style.paddingTop = hero.CurrentTurnMarker != null ? TurnSlotMarkerHeight : 2f;
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
                var reading = current.InitiativeLabel.Length == 0 ? "-" : current.InitiativeLabel;
                var initiative = HudElements.Text(
                    reading,
                    InitiativeBadgeFontSizeFor(reading),
                    ink,
                    FontStyle.Bold);
                initiative.name = "gd-current-card-initiative";
                // Never wrapped, for the reason the card's own flag is not: p.7 prints two-digit
                // Initiatives, and a marker breaking "13" across two lines would show a value the card
                // does not have. Milestone 8.55's `6+1` is the same sentence one character longer: it
                // is one reading of one card's Initiative and breaks nowhere.
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
            return AddCurrentTurnMarker(slot, hero);
        }

        /// <summary>
        /// Milestone 8.93f. The rune under the live turn's slot, drawn over the top edge of the
        /// current-card marker - above its Initiative - and over the empty slot before commitment.
        /// Added last so the card never paints over it.
        /// </summary>
        private static VisualElement AddCurrentTurnMarker(
            VisualElement slot, HeroOverviewViewModel hero)
        {
            var currentTurnMarker = BuildTurnSlotMarker(
                hero.CurrentTurnMarker, "gd-current-turn-marker");
            if (currentTurnMarker != null)
            {
                currentTurnMarker.style.position = Position.Absolute;
                // Inside the slot's top edge: the header clips anything drawn above it. Milestone
                // 9.20: the band the card reserves across the slot's width, with the glyph centred
                // in it at the tray's own scale.
                currentTurnMarker.style.top = 0f;
                currentTurnMarker.style.left = 0f;
                currentTurnMarker.style.right = 0f;
                currentTurnMarker.style.height = TurnSlotMarkerHeight;
                slot.Add(currentTurnMarker);
            }

            return slot;
        }

        // Milestone 8.93f. The band over the live turn's card that its rune stands in.
        private const float TurnSlotMarkerHeight = 24f;

        // Milestone 9.20. A tray glyph's size: the UI units one prepared source pixel is drawn at,
        // the same for every glyph so the four read at their printed relative sizes. The user's
        // "a tad bit smaller so they fit the card": at 0.75 the widest glyph (the anvil, 22 source
        // pixels plus its outline) stands about 18 units wide on the 24-unit mini card, a clear
        // margin each side; the live-turn rune in the header uses the same scale.
        private const float TrayGlyphUnitsPerSourcePixel = 0.75f;

        /// <summary>
        /// A turn-slot marker as its printed glyph alone (milestone 9.20): cut off the prepared
        /// tile's card stock by <see cref="PrintedGlyphCutout"/>, transparent around it and outlined.
        /// The caller places the element over the area the rune stands on; the glyph is drawn
        /// centred in it at the tray's one scale, in the tray and over the live turn's card alike, so
        /// the glyph's centre is the area's and the four keep their printed relative sizes. The
        /// cutout's ink is centred in its own texture, so centring the picture centres the ink. When
        /// no glyph can be cut, the prepared tile, and failing that the slot's number, stand in,
        /// fitted to the area. The marker takes no pointer, so the card under it keeps its hover.
        /// </summary>
        private static VisualElement BuildTurnSlotMarker(
            TurnSlotMarkerViewModel marker, string elementName)
        {
            if (marker == null) return null;
            var icon = new VisualElement { name = elementName, pickingMode = PickingMode.Ignore };
            icon.style.flexShrink = 0f;
            icon.style.justifyContent = Justify.Center;
            icon.tooltip = marker.AssistiveLabel;
            var tile = Resources.Load<Texture2D>(marker.ResourcePath);
            var glyph = tile == null ? null : PrintedGlyphCutout.Cut(tile);
            if (glyph != null)
            {
                const float unitsPerTexel =
                    TrayGlyphUnitsPerSourcePixel / PrintedGlyphCutout.Magnification;
                icon.style.backgroundImage = new StyleBackground(glyph);
                icon.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(
                    glyph.width * unitsPerTexel, glyph.height * unitsPerTexel));
                icon.style.backgroundPositionX =
                    new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
                icon.style.backgroundPositionY =
                    new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
                icon.style.backgroundRepeat =
                    new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
            }
            else if (tile != null)
            {
                icon.style.backgroundImage = new StyleBackground(tile);
                icon.style.backgroundSize =
                    new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
            }
            else
            {
                var fallback = HudElements.Text(
                    marker.TurnSlotIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    9f, MatchHudTheme.PrimaryText, FontStyle.Bold);
                fallback.style.unityTextAlign = TextAnchor.MiddleCenter;
                icon.Add(fallback);
            }
            return icon;
        }
    }
}
