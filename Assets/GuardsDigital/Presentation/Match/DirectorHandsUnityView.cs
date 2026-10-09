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
        /// Milestone 9.32g (approved mockup A). The step between two cards of a compact row of
        /// <paramref name="cardCount"/>: five or fewer step as they always have; more fit into the same
        /// footprint, each covered card showing a narrower column - Takahide's six after Ready for War.
        /// </summary>
        public static float PeekSlotFootprintFor(int cardCount) => cardCount <= 5
            ? PeekCardSlotFootprint
            : (ConfiguredMatchHudRenderer.CompactHandRightEdge - PeekCardOuterWidth) / (cardCount - 1);

        /// <summary>Milestone 9.32g. The overlap that pairs with <see cref="PeekSlotFootprintFor"/>.</summary>
        public static float PeekOverlapFor(int cardCount) => PeekCardOuterWidth - PeekSlotFootprintFor(cardCount);

        // The overlap of the compact row being built, set from its card count before its cards are.
        private float rowPeekOverlap = PeekCardOverlap;

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

        /// <summary>Milestone 8.72. The drawn "deciding for X" slot, kept by the rebuild that made it.</summary>
        private VisualElement decidingForSlot;

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
            // The host itself must never take the pointer: it shares End Turn's top-right rect,
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
        /// The hand's Pass banner. It begins inside <see cref="Root"/> so this view remains complete
        /// in isolation; the configured HUD reparents it into End Turn's mutually exclusive
        /// top-right status-bar slot.
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
            contextBlocker.style.display = contextOnly && !CoordinationEnabled ? DisplayStyle.Flex : DisplayStyle.None;
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
        public event Action<CardInstanceId> CardSuggested;
        public event Action MyCardsRequested;
        public Func<CardInstanceId, System.Collections.Generic.IReadOnlyList<HeroOverviewViewModel>> SuggestedBy { get; set; }
        public bool CoordinationEnabled { get; set; }
        public event Action<CardInstanceId> CardDroppedOnMap;
        public event Action ConfirmRequested;
        public event Action<HeroId> PassRequested;
        public event Action<TypedInvocationViewModel> PrintedActionSubmitted;
        public event Action<FastTravelOfferViewModel> FastTravelSelected;
        public event Action<ClearOfferViewModel> ClearSelected;

        public IReadOnlyList<PrintedActionRegionViewModel> PrintedActionRegions => printedActionRegions;

        /// <summary>
        /// Milestone 8.72. The drawn "deciding for X" slot, or <c>null</c> when this viewer is
        /// deciding nobody else's action - which is every board no delegation stands on.
        ///
        /// <para>It is exposed because the slot is the one tray card that reaches PAST the compact
        /// hand's own stated footprint, and the HUD blocks the board pointer across that footprint
        /// rather than across whatever the tray happens to have drawn. Without this the click that
        /// started a delegated Attack would also reach the hex underneath it, which is the exact
        /// duplicate-input defect milestone 8.30b recorded for End Turn's corner.</para>
        /// </summary>
        public VisualElement DecidingForSlotRoot => decidingForSlot;

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
            hidden = hidden && !(CoordinationEnabled && Model?.ViewedHand?.IsReadOnly == true);
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
            decidingForSlot = null;
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

            // Milestone 8.72. The one card this viewer is deciding FOR somebody else, if any: it sits
            // beside the hand in a slot of its own rather than inside it, because it is not a card
            // this player holds and a hand is what a player commits from.
            var decidedFor = Model.DecidedForHand?.PlayableTrayCards.FirstOrDefault(
                card => card.IsResolving);
            var rowWidth = compactPeekMode
                ? CompactCardRowWidth(drawnCards.Length)
                : drawnCards.Length * CardSlotFootprint;
            if (decidedFor != null)
            {
                rowWidth += DecidingForSlotGap +
                    (compactPeekMode ? PeekCardOuterWidth : CardSlotFootprint);
            }

            if (hand.IsReadOnly)
            {
                var inspection = new VisualElement { name = "allied-hand-inspection" };
                inspection.style.position = Position.Absolute;
                inspection.style.top = -72;
                inspection.style.left = 140;
                inspection.style.flexDirection = FlexDirection.Row;
                var ownerLabel = new Label(hand.HeroDisplayName + " — READ ONLY");
                ownerLabel.style.color = MatchHudTheme.PrimaryText;
                ownerLabel.style.fontSize = 22;
                ownerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                ownerLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                ownerLabel.style.marginRight = 12;
                inspection.Add(ownerLabel);
                var returnButton = new Button(() => MyCardsRequested?.Invoke()) { text = "MY CARDS", name = "my-cards" };
                returnButton.style.fontSize = 20;
                returnButton.style.height = 32;
                returnButton.style.minWidth = 110;
                returnButton.style.color = MatchHudTheme.PrimaryText;
                returnButton.style.backgroundColor = MatchHudTheme.PanelBackground;
                HudElements.Border(returnButton, MatchHudTheme.PanelBorder, 1, 6);
                inspection.Add(returnButton);
                cards.Add(inspection);
            }
            cards.style.width = rowWidth;
            cards.style.minWidth = rowWidth;
            cards.style.maxWidth = rowWidth;

            // No hand-status bar. Pass shares End Turn's mutually exclusive top-right status slot,
            // while commitment Confirm has its dedicated lane below DIRECTOR.
            // Pass is the empty-hand player's only move, so it uses End Turn's rectangular full-shine
            // treatment rather than the older swallowtail submit-card treatment.
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
                    }, true,
                    RoundActionUnityView.EndTurnControlFontSize, "gd-pass-banner-label",
                    rectangular: true)
                {
                    name = "gd-pass-control",
                    text = "PASS - NO CARD"
                };
                pass.SetVisibleLabel("PASS - NO CARD");
                pass.style.minHeight = ConfiguredMatchHudRenderer.EndTurnControlHeight;
                pass.style.fontSize = RoundActionUnityView.EndTurnControlFontSize;
                pass.style.unityFontStyleAndWeight = FontStyle.Bold;
                pass.style.unityTextAlign = TextAnchor.MiddleCenter;
                pass.style.flexGrow = 1f;
                pass.style.flexShrink = 0f;
                pass.style.marginBottom = 0f;
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
                    text = hand.SelectedSecondCardInstanceId.HasValue
                        ? "CONFIRM TWO FACEDOWN"
                        : "CONFIRM FACEDOWN"
                };
                confirm.SetVisibleLabel(confirm.text);
                confirm.style.minHeight = CornerBannerHeight;
                confirmAction.Add(confirm);
            }

            rowPeekOverlap = PeekOverlapFor(drawnCards.Length);
            for (var index = 0; index < drawnCards.Length; index++)
            {
                cards.Add(BuildCard(
                    drawnCards[index],
                    compactPeekMode && index == drawnCards.Length - 1,
                    false,
                    Model.ViewedHandDrawsActionControls));
            }

            rowPeekOverlap = PeekCardOverlap;

            if (decidedFor != null)
            {
                decidingForSlot = BuildDecidedForSlot(
                    decidedFor, Model.DecidedForHand.HeroDisplayName);
                cards.Add(decidingForSlot);
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
            stackOverlap = PeekCardOuterHeight - UnavailableSlotFootprintFor(unavailable.Length);
            for (var index = 0; index < unavailable.Length; index++)
            {
                unavailableCards.Add(BuildCard(
                    unavailable[index],
                    compactPeekMode && index == unavailable.Length - 1,
                    true));
            }

            stackOverlap = UnavailableCardOverlap;
            unavailableGroup.style.display = !trayHidden && unavailable.Length > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        /// <summary>
        /// Milestone 8.72. The gap that separates the "deciding for X" slot from the viewer's own
        /// hand. It is wider than <see cref="CardGap"/> on purpose: the hand's own cards overlap like
        /// a physical hand, and this card must read as standing apart from them rather than as the
        /// sixth one in the fan.
        /// </summary>
        public const float DecidingForSlotGap = 24f;

        /// <summary>The "DECIDING FOR X" ribbon's height, drawn inside the card's own footprint.</summary>
        public const float DecidingForLabelHeight = 24f;

        /// <summary>
        /// Milestone 8.72. Another hero's revealed resolving card, drawn on the seat that decides it.
        ///
        /// <para>The card itself is built by the SAME builder every tray card goes through, so its
        /// face, its printed-action overlay, its hit targets, its tooltips and its assistive reading
        /// are the ordinary ones - what would be a different card here is a different card, and this
        /// is not one. What the slot adds is the two things that keep it from reading as a hand card:
        /// a ribbon naming whose card it is, and an accent border of its own. It carries no
        /// commitment command, which is the card's own <c>IsSelectable</c> and not a rule stated
        /// here.</para>
        /// </summary>
        private VisualElement BuildDecidedForSlot(TrayCardViewModel card, string heroDisplayName)
        {
            var slot = BuildCard(card, true, false, true);
            slot.name = "gd-deciding-for-slot";
            slot.style.marginLeft = DecidingForSlotGap;
            slot.style.marginRight = 0f;
            HudElements.Border(slot, MatchHudTheme.ActiveEffectAccent, ActiveOutlineWidth, 11f);

            var ribbon = new Label(DelegatedActionPresentation.DecidingForLabel(heroDisplayName))
            {
                name = "gd-deciding-for-label",
                pickingMode = PickingMode.Ignore
            };
            // Inside the card's own footprint, over the top strip no printed region reaches (the
            // secondary badges start a hundred units down), so the ribbon costs the tray no height
            // and can cover no control.
            ribbon.style.position = Position.Absolute;
            ribbon.style.left = 0f;
            ribbon.style.right = 0f;
            ribbon.style.top = 0f;
            ribbon.style.height = DecidingForLabelHeight;
            ribbon.style.unityTextAlign = TextAnchor.MiddleCenter;
            ribbon.style.unityFontStyleAndWeight = FontStyle.Bold;
            ribbon.style.fontSize = 13f;
            ribbon.style.color = MatchHudTheme.ActiveEffectAccent;
            ribbon.style.backgroundColor = new Color(0f, 0f, 0f, 0.82f);
            slot.Add(ribbon);
            return slot;
        }

        private static float CompactCardRowWidth(int cardCount) => cardCount <= 0
            ? 0f
            : PeekCardOuterWidth + ((cardCount - 1) * PeekSlotFootprintFor(cardCount));

        public static float CompactUnavailableStackHeight(int cardCount) => cardCount <= 0
            ? 0f
            : PeekCardOuterHeight + ((cardCount - 1) * UnavailableSlotFootprintFor(cardCount));

        /// <summary>
        /// Milestone 9.32g (approved mockup A's rule, applied to the stack beneath the rail): five or
        /// fewer unavailable cards step as they always have; more fit into the five-card height the HUD
        /// reserves, so a six-card hand never pushes the stack into the rail above it.
        /// </summary>
        public static float UnavailableSlotFootprintFor(int cardCount) => cardCount <= 5
            ? UnavailableCardSlotFootprint
            : 4f * UnavailableCardSlotFootprint / (cardCount - 1);

        // The vertical overlap of the unavailable stack being built, set from its count first.
        private float stackOverlap = UnavailableCardOverlap;

        private VisualElement BuildCard(
            TrayCardViewModel card,
            bool isLastPeekCard = false,
            bool isUnavailableGroup = false,
            bool drawsPrintedActions = true)
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
            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 1) return;
                evt.StopImmediatePropagation();
                CardSuggested?.Invoke(card.CardInstanceId);
            }, TrickleDown.TrickleDown);
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

            // Milestone 8.72: a resolving card draws its printed controls only where the answer
            // belongs. While Hanu's The Ultimate Trick stands, the targeted player's own tray holds
            // the card and his seat holds the commands, so the overlay is built on his slot and not
            // on theirs - a control drawn where the host refuses it is worse than no control.
            if (card.IsResolving && drawsPrintedActions)
            {
                AddPrintedActionOverlay(button, card);
            }

            // Milestone 8.5b: the card sits in a slot, and the slot is what carries the Active-effect
            // outline. It has to be outside the card because the card's own border is the shared
            // selection treatment, and a card can be selected and active at the same time - two
            // meanings on one border would be one meaning lost. The slot is always drawn, so a card
            // does not change size when an effect starts or ends; only the outline's colour does.
            var slot = new VisualElement { name = "gd-card-slot", userData = card.CardInstanceId };
            slot.style.flexDirection = FlexDirection.Column;
            slot.style.alignItems = Align.Stretch;
            slot.style.marginRight = CardGap;
            if (compactPeekMode)
            {
                if (isUnavailableGroup)
                {
                    slot.style.marginRight = 0f;
                    slot.style.marginBottom = isLastPeekCard ? 0f : -stackOverlap;
                }
                else
                {
                    slot.style.marginRight = isLastPeekCard ? 0f : -rowPeekOverlap;
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
                // Full-strength faces remain commandless; spent cards retain keyboard inspection.
                HudElements.MarkInPlay(button);
                if (!card.IsResolving && compactPeekMode && isUnavailableGroup)
                    button.focusable = !contextOnly;
                else if (!card.IsResolving)
                    button.SetEnabled(false);
            }

            // The complete face already carries its printed words. Keep the tooltip to the concise
            // assistive identity/lifecycle reading instead of showing those same rules a second time.
            button.tooltip = card.AssistiveLabel;
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
            else if (!card.IsResolving && card.CastableSpells.Count > 0)
            {
                // At wide layouts the complete card is already present, so there is deliberately no
                // detached peek. The casting companion belongs to that visible face itself instead.
                HudElements.OnPointedAt(button, pointedAt =>
                {
                    if (pointedAt)
                    {
                        CastableSpellHoverView.Attach(button, card.CastableSpells);
                        if (isUnavailableGroup || Model?.ViewedHand?.IsReadOnly == true)
                            CastableSpellHoverView.PlaceBeside(button);
                    }
                    else
                        CastableSpellHoverView.Detach(button);
                });
            }
            AddTeammateSignal(slot, SuggestedBy?.Invoke(card.CardInstanceId));
            return slot;
        }

        /// <summary>Refresh decorations without replacing card faces, input state or an open preview.</summary>
        public void RefreshTeammateSignals()
        {
            foreach (var slot in Root.Query<VisualElement>("gd-card-slot").ToList())
                if (slot.userData is CardInstanceId id)
                    AddTeammateSignal(slot, SuggestedBy?.Invoke(id));
            if (highlightedPeekCard != null && hoverPreview.childCount > 0)
                AddTeammateSignal(hoverPreview[0], SuggestedBy?.Invoke(highlightedPeekCard.CardInstanceId));
        }

        private static void AddTeammateSignal(VisualElement anchor,
            System.Collections.Generic.IReadOnlyList<HeroOverviewViewModel> suggestions)
        {
            anchor.Q("teammate-card-signal")?.RemoveFromHierarchy();
            if (suggestions != null && suggestions.Count > 0)
            {
                var signal = new VisualElement { name = "teammate-card-signal", pickingMode = PickingMode.Ignore };
                signal.style.position = Position.Absolute;
                signal.style.left = -5; signal.style.right = -5; signal.style.top = -5; signal.style.bottom = -5;
                HudElements.Border(signal, Color.white, 2f, 12f);
                var badges = new VisualElement { name = "teammate-card-owners", pickingMode = PickingMode.Ignore };
                badges.style.position = Position.Absolute; badges.style.top = -28; badges.style.left = 8;
                badges.style.flexDirection = FlexDirection.Row;
                foreach (var hero in suggestions)
                {
                    var badge = HeroOverviewsUnityView.BuildSharedHeroPortrait(hero, MatchHudTheme.TeamFace(hero.TeamSide), 0.85f);
                    badge.pickingMode = PickingMode.Ignore;
                    foreach (var child in badge.Children()) child.pickingMode = PickingMode.Ignore;
                    badges.Add(badge);
                }
                signal.Add(badges); IgnorePickingRecursively(signal); anchor.Add(signal);
            }
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

            // MILESTONE 8.35z11-z14 (wave one), and it closes the LAST gold path that milestone
            // 8.35z9's grammar pass did not reach.
            //
            // 8.35z9 stated the rule - GOLD MEANS "THIS IS YOURS TO ANSWER" - and narrowed
            // `IsAwaitedAnswer` so a committed card stops claiming it the moment somebody else is
            // acting. Every gold treatment on this surface was moved onto that predicate EXCEPT this
            // one, which is still reached by `card.IsResolving` alone at the call site. And
            // `IsResolving` is the WIDE window: it is true from the instant a hero's card is REVEALED
            // until it finishes resolving, which spans up to five other heroes' turns on a six-player
            // board. So the printed-action shine on a viewer's own committed card could stay lit while
            // another player resolved theirs - one card, exactly as the user reported it, and a
            // different element from the card-scope ring 8.35z9 fixed.
            //
            // `card.IsAwaitedAnswer` is the right predicate and not a second guess at the same
            // question: this method only runs for a resolving card, and a resolving card is never in
            // the Available group, so within this method the flag reduces to exactly "this is the
            // viewer's own resolving card AND its turn is actually under way".
            //
            // The card KEEPS everything else. The overlay is still built, so the regions, their hit
            // targets, their tooltips, the assistive reading and the layout are untouched; the card
            // still stands at full strength alongside the other hand cards; being the committed card is a
            // true and wanted thing to say. It simply stops wearing the colour that means "act now"
            // while it is not the viewer's turn to act.
            var lit = card.IsAwaitedAnswer;

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
                    tooltip = region.Invocation == null ? region.AssistiveLabel : string.Empty,
                    userData = region,
                    focusable = !contextOnly
                };
                if (region.Invocation != null)
                {
                    // The complete face already prints this action's rules. Stop the runtime
                    // tooltip walk here as well as leaving this hotspot's own tooltip empty, so it
                    // cannot fall through to the resolving card's parent tooltip.
                    control.AddToClassList(RuntimeTooltipCandidateLookup.SuppressTooltipClass);
                }
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
                if (lit &&
                    usesPreparedArtwork &&
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
                else if (lit && isPrimary && region.Invocation != null)
                {
                    // A prepared front without its ring overlay, and every generated-face
                    // primary: the drawn approximation of the same statement.
                    HudElements.Border(control, Color.clear, 0f, 0f);
                    control.style.overflow = Overflow.Visible;
                    // Named for the same reason milestone 8.35z9 gave the other gold treatments a
                    // shared class: a gold element a test cannot find is a gold element no state
                    // matrix can prove absent.
                    primaryGlow = new PrimaryOfferGlow { name = "gd-primary-offer-glow" };
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
                        // Milestone 8.85: only a rail's SECONDARY badge keeps the circle; a primary
                        // Movement now owns the panel box like every other primary. Milestone 8.112a:
                        // a secondary Attack is a rail badge too.
                        circle: !isPrimary &&
                            (region.ActionKind == ActionKind.Movement ||
                                region.ActionKind == ActionKind.Attack),
                        available: lit && region.Invocation != null);
                }
                else
                {
                    HudElements.Border(control, Color.clear, 0f, 0f);
                    SetGeneratedChipShine(
                        cardSurface, ShineSlotFor(region), lit && region.Invocation != null, false);
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
            // Milestone 8.112a: CLEAR attaches above a PRIMARY Attack's title; a secondary Attack's
            // CLEAR sits beside its rail badge, exactly as FAST TRAVEL sits beside the boot.
            var clearAbovePreparedAttack = usesPreparedArtwork &&
                region.ActionKind == ActionKind.Attack &&
                region.InvocationContext == ActionInvocationContext.PrintedPrimary &&
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
                // Milestone 8.35s: CLEAR is the one replacement chip whose unavailable state has a
                // single, provable cause - ClearOptionProjection stamps its own replacement offer
                // unavailable only when no token stands adjacent to the actor to Clear, never for any
                // other reason - so NO TARGET is safe here without re-deriving anything. FAST TRAVEL's
                // view model requires at least one legal destination to exist at all
                // (FastTravelOfferViewModel's own constructor), so it is never drawn unavailable in
                // practice; the generic word is what a change to that shape would fall back to rather
                // than a guessed one.
                tooltip = available
                    ? $"{label} replaces {region.ActionKind}"
                    : $"{label} replaces {region.ActionKind} - " +
                        (string.Equals(label, "CLEAR", StringComparison.Ordinal)
                            ? ActionUnavailabilityLabels.NoTarget
                            : ActionUnavailabilityLabels.Generic),
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
            // Overlay-alignment corrective (post-8.35z9, user screenshots at hand and enlarged-
            // preview scale). The FAST TRAVEL "beside" placement - with, since milestone 8.112a, a
            // secondary Attack's CLEAR, the only paths this reaches - used to anchor against the padded highlight ring
            // (PreparedActionSourceBounds' generous 80x82 glow box around the boot badge), which
            // measured about 15-20 reference units clear of the actual printed boot/value ink -
            // PreparedActionSourceBounds' own comment names that ink as roughly
            // x=21..69, y=203..249 on the shared 372x520 source. Small at hand scale (the ribbon
            // reads as "near" the badge), the same fixed gap reads as a floating, disconnected
            // flag once the identical geometry is seen at the enlarged reveal - both draw from
            // this one rect, so tightening it fixes both at once. The ring/glow geometry itself
            // is untouched; only the ribbon's own anchor moves onto the ink it names.
            var ribbonAnchorBounds =
                fastTravelAbovePreparedMovement && usesPreparedArtwork && !attachedToPreparedPrimaryTitle
                    ? ScalePreparedActionBounds(new Rect(21f, 203f, 48f, 46f))
                    : actionBounds;
            var titleBandTop = actionBounds.y +
                ((region.ActionKind == ActionKind.Attack ? 40f : 36f) *
                    CardFootprint.ArtworkHeight / CardFootprint.ArtworkSourceHeight);
            if (attachedToPreparedPrimaryTitle && ConfiguredCardShine.TryGetPrimaryTitleTop(
                    region.Offer.SourceDefinitionReference, out var sourceTitleTop))
                titleBandTop = sourceTitleTop * CardFootprint.ArtworkHeight / CardFootprint.ArtworkSourceHeight;
            var attachmentCenterX = attachedToPreparedPrimaryTitle
                ? CardFootprint.ArtworkWidth * 0.5f
                : ribbonAnchorBounds.center.x;
            var placeBesidePreparedRegion = usesPreparedArtwork &&
                !clearAbovePreparedAttack &&
                ribbonAnchorBounds.xMax + 96f <= CardFootprint.ArtworkWidth;
            chip.style.left = attachedAbovePreparedAction
                ? Mathf.Clamp(
                    attachmentCenterX - 46f,
                    0f,
                    CardFootprint.ArtworkWidth - 92f)
                : placeBesidePreparedRegion
                    ? ribbonAnchorBounds.xMax - 2f
                    : ribbonAnchorBounds.xMax - 96f;
            chip.style.top = attachedAbovePreparedAction
                ? (attachedToPreparedPrimaryTitle ? titleBandTop : actionBounds.y) - 22f
                : placeBesidePreparedRegion
                    ? ribbonAnchorBounds.y + Mathf.Max(0f, (ribbonAnchorBounds.height - 26f) * 0.5f)
                    : usesPreparedArtwork
                        ? ribbonAnchorBounds.y + 4f
                        : ribbonAnchorBounds.yMax - 26f;
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

        internal static Rect PreparedActionSourceBounds(PrintedActionRegionViewModel region)
        {
            // Prepared fronts share one verified 372x520 anatomy. A SECONDARY Movement owns a compact
            // circular treatment around its printed boot/value badge on the left rail. Every PRIMARY -
            // Movement included - owns the broad lower action panel, so its hit surface is one large
            // box around that complete printed section. These are shared template coordinates, not
            // card-name rules.
            //
            // Milestone 8.85. A primary Movement used to take the 80x82 badge circle too (the same
            // treatment as the rail boot), which left a Movement-primary card with a hit target about
            // one ninth the area an Attack or Skill primary offers: a player who pressed the printed
            // action panel - where every other primary answers - pressed nothing. The user, from the
            // live playtest: "cards that have primary movement action. Players seem to have trouble
            // clicking them" / "They have trouble selecting primary movement action". The panel is
            // the hit surface now; the lit ring overlay already traced the panel's printed lines.
            if (region.InvocationContext == ActionInvocationContext.PrintedPrimary)
            {
                switch (region.ActionKind)
                {
                    case ActionKind.Movement:
                        return new Rect(6f, 318f, 360f, 174f);
                    case ActionKind.Attack:
                        return new Rect(6f, 308f, 360f, 184f);
                    case ActionKind.Skill:
                        return new Rect(6f, 328f, 360f, 164f);
                    default:
                        return new Rect(6f, 318f, 360f, 174f);
                }
            }

            // Milestone 8.112a. A rail printing THREE badges - Defense, Movement and Attack, Mrak's
            // Red Skills - packs them tighter and higher than the ordinary two: measured on the
            // shared source, the boot's value sits at y=179..207 and the swords' at y=241..269,
            // against y=210..238 for an ordinary boot. A card offering two secondary actions is
            // exactly that rail, so the count selects the layout; no card is named.
            var threeBadgeRail = region.SecondaryCount >= 2;
            if (region.ActionKind == ActionKind.Movement)
            {
                // The visible boot/value composite spans roughly x=21..69 and y=203..249 on the
                // shared source. This footprint centres the circle on that ink rather than the rail.
                return threeBadgeRail
                    ? new Rect(5f, 162f, 80f, 62f)
                    : new Rect(5f, 184f, 80f, 82f);
            }

            if (region.ActionKind == ActionKind.Attack)
            {
                // The crossed swords under the boot on a three-badge rail, or directly under the
                // shield where no boot is printed (Stone Carapace and Rock Solid, whose Movement is
                // their primary): value ink measured at y=206..223 there.
                return threeBadgeRail
                    ? new Rect(5f, 224f, 80f, 62f)
                    : new Rect(5f, 178f, 80f, 70f);
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
            bool circle,
            bool available)
        {
            // The control remains the typed hit/focus surface but contributes no fill or text. The
            // temporary prepared-card treatment is deliberately simple: a circle around the rail's
            // secondary Movement boot/value badge, or a broad box around the complete primary panel
            // (Attack, Skill and - since milestone 8.85 - Movement alike).
            HudElements.Border(control, Color.clear, 0f, 0f);
            control.style.overflow = Overflow.Visible;
            if (!available)
            {
                return;
            }

            var glow = new PreparedActionGlow(circle)
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
        internal static CardShineSlot ShineSlotFor(PrintedActionRegionViewModel region)
        {
            if (region.InvocationContext == ActionInvocationContext.PrintedPrimary)
            {
                return CardShineSlot.PrimaryRing;
            }

            switch (region.ActionKind)
            {
                case ActionKind.Defense:
                    return CardShineSlot.SecondaryDefense;
                case ActionKind.Attack:
                    return CardShineSlot.SecondaryAttack;
                default:
                    return CardShineSlot.SecondaryMovement;
            }
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
                case CardShineSlot.SecondaryAttack:
                    return cardSurface.Q<VisualElement>("gd-card-secondary-attack");
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
            // The full-face preview keeps the unavailable veil while the resting anchor is hidden;
            // otherwise hovering a committed card briefly presents it as selectable again.
            PopulatePeekPreview(
                hoverPreview,
                card,
                anchor,
                isUnavailableGroup,
                dimUnavailable: true,
                sideSpells: isUnavailableGroup || Model?.ViewedHand?.IsReadOnly == true);
            AddTeammateSignal(hoverPreview[0], SuggestedBy?.Invoke(card.CardInstanceId));
        }

        /// <summary>
        /// Draws the ordinary hand's complete pointer-transparent peek at its shared bounded position.
        /// Defense offers reuse this exact presentation rather than maintaining a second card fan.
        /// </summary>
        internal static void PopulatePeekPreview(
            VisualElement preview,
            TrayCardViewModel card,
            VisualElement anchor,
            bool isRightSideGroup = false,
            bool dimUnavailable = true, // Compatibility argument; W-016 keeps every peek fully bright.
            bool sideSpells = false)
        {
            preview.Clear();
            var face = BuildStandaloneCard(card);
            face.style.opacity = 1f;
            var spellShelfHeight = CastableSpellHoverView.HeightFor(card.CastableSpells);
            CastableSpellHoverView.Attach(face, card.CastableSpells);
            if (sideSpells) CastableSpellHoverView.PlaceBeside(face);
            var previewWidth = CardFootprint.Width + (sideSpells && spellShelfHeight > 0f
                ? CastableSpellHoverView.Gap + CastableSpellHoverView.WidthFor(card.CastableSpells) : 0f);
            var previewHeight = sideSpells ? Mathf.Max(CardFootprint.Height, spellShelfHeight) : CardFootprint.Height;
            IgnorePickingRecursively(face);
            preview.Add(face);
            preview.style.display = DisplayStyle.Flex;
            preview.BringToFront();
            var viewport = ConfiguredMatchHudRenderer.PanelViewport;
            var bounds = anchor.worldBound;
            var rightLimit = isRightSideGroup
                ? Mathf.Max(
                    CardFootprint.ExteriorOutlineWidth,
                    viewport.x - previewWidth - CardFootprint.ExteriorOutlineWidth)
                : Mathf.Max(
                    CardFootprint.ExteriorOutlineWidth,
                    Mathf.Min(
                        viewport.x,
                        ConfiguredMatchHudRenderer.CompactHandRightEdge) -
                    previewWidth - CardFootprint.ExteriorOutlineWidth);
            preview.style.left = Mathf.Clamp(
                bounds.xMin,
                CardFootprint.ExteriorOutlineWidth,
                rightLimit);
            var topOverhang = !sideSpells && spellShelfHeight > 0f
                ? spellShelfHeight + CastableSpellHoverView.Gap
                : 0f;
            preview.style.top = Mathf.Clamp(
                bounds.yMin,
                ConfiguredMatchHudRenderer.StatusBarHeight + 4f + topOverhang,
                Mathf.Max(
                    ConfiguredMatchHudRenderer.StatusBarHeight + 4f + topOverhang,
                    viewport.y - previewHeight - CardFootprint.ExteriorOutlineWidth));
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
            flag.style.flexDirection = FlexDirection.Row;
            CardValueOverlay.AddInline(flag, card.ValueBonuses, PrintedValueChannel.Initiative);
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
                        : value.Channel == PrintedValueChannel.Attack
                            ? "gd-card-secondary-attack"
                            : "gd-card-secondary-value";
                label.tooltip = value.AssistiveLabel;
                if (!first)
                {
                    label.style.marginLeft = 16f;
                }

                first = false;
                band.Add(label);
                CardValueOverlay.AddInline(band, card.ValueBonuses, value.Channel);
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
                CardValueOverlay.AddInline(plate, card.ValueBonuses, value.Channel);
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
                CardValueOverlay.AddInline(badge, card.ValueBonuses, spatial.Channel);
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
}
