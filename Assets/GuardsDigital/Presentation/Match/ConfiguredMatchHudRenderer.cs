using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// The standing status the top bar shows: round and turn, both teams' Life counters, the Wave
    /// counter, and the Tie Breaker.
    ///
    /// Milestone 7.8a folds the Tie Breaker into this one strip, retiring the separate top-right box.
    /// <see cref="Text"/> is unchanged and still carries every one of those facts as one line, which is
    /// what the chip's own assistive reading uses: relocating the indicator must not lose anything the
    /// old surface guaranteed.
    /// </summary>
    public sealed class MatchHudHeaderViewModel
    {
        /// <param name="teamLifeCounters">
        /// Milestone 7.4: rulebook p.13's Life counters as the match now has them. Before this
        /// milestone nothing could spend one, so the header read the setup's starting count; a Defeat
        /// spends them now, and a header still reading the setup would show a pile that never empties.
        /// An empty or absent list falls back to the setup's count, which is what a composition with no
        /// round authority behind it honestly knows.
        /// </param>
        public MatchHudHeaderViewModel(
            TieBreakerState tieBreaker,
            QuickGameSetupState setup,
            IReadOnlyList<KeyValuePair<TeamId, int>> teamLifeCounters = null,
            int roundIndex = 0,
            int turnIndex = 0,
            bool isAwaitingViewerInput = false)
        {
            IsAwaitingViewerInput = isAwaitingViewerInput;
            TieBreaker = tieBreaker;
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            Text = Format(tieBreaker, setup, teamLifeCounters);
            Color = tieBreaker.IsDefined
                ? tieBreaker.RevealedSide == TeamSide.Red
                    ? new Color(0.72f, 0.16f, 0.12f)
                    : new Color(0.12f, 0.32f, 0.72f)
                : new Color(0.3f, 0.3f, 0.3f);
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            RoundTurnLabel = roundIndex <= 0
                ? "ROUND -"
                : $"ROUND {roundIndex} · TURN {turnIndex}/{RoundResolutionAuthority.TurnsPerRound}";
            RedLifeLabel = "RED " + Counters(setup, teamLifeCounters, TeamSide.Red)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            BlueLifeLabel = "BLUE " + Counters(setup, teamLifeCounters, TeamSide.Blue)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            WaveLabel = "WAVES " +
                setup.WaveCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TieBreakerLabel = "TIEBREAKER";
        }

        public TieBreakerState TieBreaker { get; }
        public QuickGameSetupState Setup { get; }

        /// <summary>
        /// Whether the match is waiting on the viewing player, from <c>AwaitedInputQuery</c>. The
        /// whole strip carries the gold treatment while it is true - the user's requirement is the
        /// bar, not only the narration lane inside it - and the round view model's warm voice reads
        /// the same derivation, so the two halves of the strip cannot disagree.
        /// </summary>
        public bool IsAwaitingViewerInput { get; }

        /// <summary>Every fact in this bar as one line, and the Tie Breaker chip's assistive reading.</summary>
        public string Text { get; }

        public Color Color { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public string RoundTurnLabel { get; }
        public string RedLifeLabel { get; }
        public string BlueLifeLabel { get; }
        public string WaveLabel { get; }
        public string TieBreakerLabel { get; }

        private static int Counters(
            QuickGameSetupState setup,
            IReadOnlyList<KeyValuePair<TeamId, int>> counters,
            TeamSide side)
        {
            var team = setup.Teams.Single(value => value.TeamSide == side);
            if (counters != null)
            {
                foreach (var entry in counters)
                {
                    if (entry.Key.Equals(team.TeamId))
                    {
                        return entry.Value;
                    }
                }
            }

            return team.LifeCount;
        }

        private static string Format(
            TieBreakerState state,
            QuickGameSetupState setup,
            IReadOnlyList<KeyValuePair<TeamId, int>> counters)
        {
            var redLives = Counters(setup, counters, TeamSide.Red);
            var blueLives = Counters(setup, counters, TeamSide.Blue);
            var tieBreaker = !state.IsDefined
                ? "TIE BREAKER  [?] UNKNOWN"
                : state.RevealedSide == TeamSide.Red
                    ? "TIE BREAKER  [R] RED"
                    : "TIE BREAKER  [B] BLUE";
            return $"QUICK GAME  WAVES {setup.WaveCount}\nLIVES  [R] {redLives}  [B] {blueLives}\n{tieBreaker}";
        }
    }

    /// <summary>
    /// The configured match HUD, hosted on one runtime UI Toolkit panel.
    ///
    /// Milestone 7.8a replaced this component's immediate-mode drawing with a retained visual tree, by
    /// the recorded user decision. What did not change is what it is: a passive host. It holds no
    /// session, decides no legality, and owns no match state - it lays out the surfaces the presenters
    /// render into, and answers where the pointer is blocked.
    ///
    /// The panel and its layout are created here rather than authored as assets, for the reason the
    /// rest of the configured scene composes in code: the saved scene carries this component and nothing
    /// else about the HUD, so there is no asset for an import to lose and no second place the layout
    /// lives. Its settings object is the one exception, and milestone 7.9 made it one: a build with no
    /// `PanelSettings` asset in it ships no ICU text data, which is invisible in the Editor and fatal
    /// in a built player, so the panel is copied from a committed asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConfiguredMatchHudRenderer : MonoBehaviour
    {
        // Milestone 8.25c. EVERY NUMBER IN THIS CLASS IS A REFERENCE UNIT, not a device pixel. One
        // reference unit is one pixel on a 2560x1440 screen - the size this layout fits at, which is
        // what makes it the reference - and the panel scales them with the viewport, so each region is
        // the same share of every screen instead of the same count of its pixels.
        // `MatchHudPanelScaling` is the declaration and says why that size and not a smaller one.
        private const float Margin = 12f;
        // The ordinary tray is visually only its cards. Redistributing its standing 24-unit vertical
        // inset puts the unused space above the faces instead of below them, so the faces sit closer
        // to the screen edge and the board can use the newly exposed strip above them.
        public const float TrayHorizontalPadding = 0f;
        public const float TrayCardTopPadding = 0f;
        public const float TrayBottomPadding = 0f;
        // The card row is relatively lowered without increasing its layout height. The 470-unit tray
        // already contained this unused space beneath a 420-unit face; spending it this way exposes a
        // larger strip above the cards without creating vertical scrolling.
        public const float OrdinaryCardDownshift = 20f;
        // Complete playable hexes retain this small visible moat from HUD content. The old general
        // 12-unit page margin was unnecessarily conservative here: artwork may pass behind the HUD,
        // and the fitter separately protects the antialiased edge of every playable hex.
        public const float BoardHexUiGap = 4f;
        // A card already has five units of reserved Active-effect outline outside its printed edge.
        // Two more units keep a visible separation without also reserving the fifth slot's unused
        // trailing gutter.
        public const float BoardCardHexUiGap = 2f;
        // Milestone 8.25f2. The resting hand uses five full source-ratio faces overlapped like a
        // physical hand; hovering or focusing one redraws that same complete face above its siblings.
        // Mandatory card decisions continue to use the separately calculated tall tray.
        public const float CardPanelHeight = DirectorHandsUnityView.PeekCardHeight + 40f;
        // One horizontal Level Up choice row: a complete card, its centred item box, and the prompt
        // line. It may extend a little above the ordinary hand, but never changes the hero
        // columns or the camera-safe board surface around the legacy near-full-height context.
        public const float UpgradeComparisonBandHeight = CardFootprint.Height + 130f;
        // Keeps UI Toolkit border/pixel snapping from turning two mathematically adjacent rectangles
        // into a live one-pixel overlap at non-identity panel scales.
        private const float BoardTraySeparation = 2f;
        public const float TieBreakerDiameter = 160f;
        private const float EventLogPreferredWidth = 720f;
        private const float EventLogPreferredHeight = 360f;

        /// <summary>
        /// The identity row of the standing status strip: the build name, the round and turn, both
        /// Life counters, the Wave count, and the Tie Breaker chip. It is what the whole strip was
        /// before milestone 8.30b, and the chip's rectangle is still measured against it, so widening
        /// the strip below moved nothing that was already in it.
        /// </summary>
        public const float StatusBarIdentityRowHeight = 56f;

        /// <summary>
        /// The narration lane beneath the identity row, added by milestone 8.30b's user-approved
        /// amendment. The contextual narration used to be the first row of the round dock down beside
        /// the cards, which is the corner of the screen a player looks at least while the board is
        /// what they are reading; it is now the widest line on the panel and sits directly under the
        /// round and Life counters it qualifies.
        ///
        /// <para>Forty units is one 12-unit line plus its padding at every validated viewport - the
        /// longest narration this build can produce is the respawn sentence, which measures well
        /// inside a single line at the narrowest panel this HUD lays out into.</para>
        /// </summary>
        public const float StatusBarNarrationHeight = 40f;

        /// <summary>
        /// The standing status strip's height, in reference units: the identity row plus the narration
        /// lane below it. Every region under the strip is placed from this, so milestone 8.30b's
        /// amendment re-measured the hero rail column, the playfield and the tray against the taller
        /// value rather than assuming they still fitted.
        /// </summary>
        public const float StatusBarHeight =
            StatusBarIdentityRowHeight + StatusBarNarrationHeight;

        /// <summary>
        /// The fixed height of the ordinary round/hand status dock below `DIRECTOR`. It is the height
        /// required by the revealed action state, so awaiting-card and confirmation states keep the
        /// same footprint instead of resizing the HUD as the player clicks.
        /// </summary>
        public const float StatusDockHeight = 128f;

        /// <summary>
        /// The fixed width of the ordinary round/hand status dock. The widest routine action row fits
        /// this surface, and every smaller state deliberately retains the same width.
        /// </summary>
        public const float StatusDockWidth = 720f;

        /// <summary>
        /// The right edge shared by the compact bottom-left hand and the Director-side information
        /// lane. The upper hex-safe arm begins <see cref="BoardHexUiGap"/> units after it.
        /// </summary>
        public const float CompactHandRightEdge = Margin + StatusDockWidth;

        /// <summary>The gap between the optional-step submit banners and the retained hand.</summary>
        public const float OptionalStepHandGap = 8f;

        /// <summary>Room for the pewter banner's outer shine and rounded stroke at the HUD edge.</summary>
        public const float OptionalStepEdgeClearance = 20f;

        /// <summary>
        /// End Turn's own width, in reference units. Milestone 8.30b's user-approved amendment took it
        /// out of the action row; milestone 8.30l's final layout places it in the status bar's
        /// top-right slot.
        ///
        /// <para><b>Why it left the row.</b> It was the last control on an unlabelled line it shared
        /// with every offered action, and milestone 8.29b1b then moved the printed actions onto the
        /// resolving card - so the row it was keeping company with is empty in an ordinary turn, and
        /// End Turn was a small button alone in the middle of a dock about the hand. It is the one
        /// control that ends what the player is doing, so it is now the size and the position that
        /// says so.</para>
        /// </summary>
        public const float EndTurnControlWidth = HeroRailWidth;
        private const float CornerActionControlWidth = 240f;

        /// <summary>
        /// End Turn fills the compact status bar from top to bottom at its right edge.
        /// </summary>
        public const float EndTurnControlHeight = StatusBarHeight;
        public const float EndTurnControlTopInset = 0f;

        /// <summary>
        /// Milestone 8.35f. The seat's own UNDO control: a square immediately to the left of End
        /// Turn, on the same top edge of the status strip.
        /// </summary>
        /// <remarks>
        /// The user asked for a square whose side is End Turn's width. End Turn is
        /// <see cref="EndTurnControlWidth"/> (the hero rail's 264) wide and
        /// <see cref="EndTurnControlHeight"/> (the status strip's 96) tall, and a 264-unit square
        /// cannot stand in a 96-unit strip without hanging 168 units over the board. The square is
        /// therefore the largest one the strip admits - the strip's own height - which keeps the
        /// shape, the edge and the neighbour the user named. Recorded as the one deviation.
        /// </remarks>
        public const float SeatUndoControlSide = StatusBarHeight;

        /// <summary>
        /// The compact board camera's width-to-height ratio. The official playable footprint is wider
        /// than it is tall; 6:5 keeps its original whole-map scale while avoiding the much wider empty
        /// camera surface that previously centred it across the entire playfield band.
        /// </summary>
        public const float BoardViewportAspect = 6f / 5f;

        /// <summary>
        /// A hero overview rail's width.
        ///
        /// <para>Milestone 8.23a widened it from 172, and the trade is deliberate rather than a matter
        /// of taste. Rulebook p.6 puts three heroes on a side, so a rail's real budget is a third of
        /// this column's height - and every row in a narrow rail buys its narrowness back in wrapped
        /// lines: the discard pile wraps by the pile's own width, the item strip wraps at a fourth
        /// channel, and a held marker's three value chips would wrap at 172 before the row existed.
        /// Trading horizontal room the board does not need for vertical room three rails do need is
        /// what makes the same readings fit in a third of the height.</para>
        ///
        /// <para>Milestone 8.25 found the rest of that room without touching this number:
        /// <see cref="CalculateHeroRailRect"/> gives the column the screen edge down to the reserved
        /// tray. Three rails of the height 8.23a achieved need about 733 and the compact band now leaves
        /// about 878 reference units, so the recomposition and the taller column together are what
        /// hold p.6's three a side.</para>
        ///
        /// <para><b>Milestone 8.29a.</b> 264 reference units gives a character plaque enough room for
        /// its portrait, full written name, and current-card marker to read as one header. It is a
        /// preference, not permission to crowd the map: <see cref="CalculateHeroRailWidth"/> narrows
        /// both sides symmetrically before their combined footprint reaches half a short viewport.
        /// Supported desktop layouts receive the full 264; the extreme-shape geometry gates exercise
        /// the yielding branch.</para>
        /// </summary>
        public const float HeroRailWidth = 264f;

        private const float DirectorToggleWidth = 122f;
        private const float DirectorToggleHeight = 30f;
        // The fixed largest-state dock needs these compact seams to fit below three Red rails without
        // taking height from the cards. Both remain visible at the reference scale, and `DIRECTOR`
        // still reads as the rail's next control rather than as a detached panel.
        private const float DirectorRailGap = 2f;
        private const float DirectorStatusGap = 3f;
        /// <summary>
        /// The small visual breath between DIRECTOR and the commitment banner. It is deliberately
        /// larger than the frameless status seam so the banner's flared pewter edge does not crowd
        /// the compact toggle above it.
        /// </summary>
        public const float ConfirmDirectorGap = 18f;
        private const float DirectorDrawerWidth = 134f;
        private const float DirectorDrawerHeight = 112f;
        private const int OrdinaryHandCardCount = 5;

        private UIDocument document;
        private PanelSettings panelSettings;
        private VisualElement root;
        private VisualElement trayRegion;
        private ScrollView trayScroll;
        private VisualElement trayFlow;
        private VisualElement traySidebar;
        private VisualElement statusDock;
        private bool contextUsesTray;

        /// <summary>
        /// Whether the tray is hosting p.15's one-row comparison band. It selects the bounded overlay,
        /// side-rail clearance, and vertical-scroll fallback without changing standing HUD geometry.
        /// </summary>
        private bool trayShowsUpgradeComparison;
        private VisualElement directorDrawer;
        private Button directorToggle;
        private MatchStatusBarUnityView statusBar;
        private DirectorHandsUnityView hands;
        private RoundActionUnityView round;
        private UndoRestartUnityView undoRestart;
        private DiagnosticLogUnityView diagnostics;
        private MinionChoiceUnityView minionChoice;
        private HeroOverviewsUnityView heroOverviews;
        private MatchHudHeaderViewModel header;

        /// <summary>
        /// Raised whenever layout assigns the playable hexes their safe L-shaped surface. The board
        /// camera renders full-screen behind the HUD and constrains only complete hexes; decorative
        /// artwork may extend outside both regions into otherwise dead space.
        /// </summary>
        public event Action<BoardCameraSafeLayout> PlayfieldLayoutChanged;

        /// <summary>
        /// The rectangle this HUD lays out into, in the reference units milestone 8.25c made every
        /// number in this file. It is the panel's own size, which is the screen's size scaled by
        /// <see cref="MatchHudPanelScaling"/> - and it is the reference resolution itself on every
        /// screen of the reference's shape, whatever that screen's pixel count.
        ///
        /// <para>Every rectangle below is measured in it. Reading <c>Screen.width</c> here instead
        /// would put a device pixel where a reference unit belongs, which is the defect this milestone
        /// removed.</para>
        /// </summary>
        public static Vector2 PanelViewport => MatchHudPanelScaling.CurrentReferenceViewport();

        public MatchHudHeaderViewModel Header => header;
        public Rect CardPanelRect => GetCardPanelRect();
        public Rect UnavailableCardGroupRect => CalculateUnavailableCardGroupRect(
            PanelViewport.x, PanelViewport.y);
        public Rect TieBreakerIndicatorRect => GetTieBreakerIndicatorRect();
        public Rect RuleEventLogPanelRect => GetRuleEventLogRect();
        public Rect StatusBarRect => CalculateStatusBarRect(PanelViewport.x, PanelViewport.y);

        /// <summary>Where End Turn's own control stands. Milestone 8.30b.</summary>
        public Rect EndTurnControlRect => GetEndTurnControlRect();

        /// <summary>The commitment confirmation lane immediately below the Director toggle.</summary>
        public Rect ConfirmFacedownControlRect => GetConfirmFacedownControlRect();
        public Rect OptionalStepControlRect => CalculateOptionalStepControlRect(
            PanelViewport.x, PanelViewport.y);
        public Rect RedHeroRailRect =>
            CalculateHeroRailRect(PanelViewport.x, PanelViewport.y, TeamSide.Red);
        public Rect BlueHeroRailRect =>
            CalculateHeroRailRect(PanelViewport.x, PanelViewport.y, TeamSide.Blue);
        public BoardCameraSafeLayout CameraSafeLayout { get; private set; }

        /// <summary>Whether the compact `DIRECTOR` drawer is open. Local state; it changes no match state.</summary>
        public bool IsDirectorDrawerOpen { get; private set; }

        /// <summary>The panel this HUD draws on, for a test that needs to look at the built tree.</summary>
        public VisualElement RootElement => root;

        /// <summary>
        /// Whether a panel-space point is on the board surface accepted by the commitment drag.
        /// The two regions are the exact L-shaped safe surface already published to the board camera;
        /// HUD chrome, the hand, and space outside the panel are therefore excluded without inventing
        /// a second map rectangle.
        /// </summary>
        public bool IsMapDropPoint(Vector2 panelPosition) =>
            CameraSafeLayout.IsValid &&
            (CameraSafeLayout.UpperRegion.Contains(panelPosition) ||
             CameraSafeLayout.LowerRightRegion.Contains(panelPosition));

        public void Configure(
            DirectorHandsUnityView handsView,
            RoundActionUnityView roundView,
            UndoRestartUnityView undoRestartView,
            DiagnosticLogUnityView diagnosticView,
            MinionChoiceUnityView minionChoiceView,
            HeroOverviewsUnityView heroOverviewsView = null)
        {
            hands = handsView ?? throw new ArgumentNullException(nameof(handsView));
            hands.SetCompactPeekMode(true);
            round = roundView ?? throw new ArgumentNullException(nameof(roundView));
            undoRestart = undoRestartView ?? throw new ArgumentNullException(nameof(undoRestartView));
            diagnostics = diagnosticView ?? throw new ArgumentNullException(nameof(diagnosticView));
            minionChoice = minionChoiceView ?? throw new ArgumentNullException(nameof(minionChoiceView));
            if (heroOverviews != null)
            {
                heroOverviews.ColumnGeometryChanged -= LayoutResolvedHudGeometry;
            }

            heroOverviews = heroOverviewsView;
            if (heroOverviews != null)
            {
                heroOverviews.ColumnGeometryChanged += LayoutResolvedHudGeometry;
            }
            Build();
        }

        public void RenderHeader(MatchHudHeaderViewModel model)
        {
            header = model ?? throw new ArgumentNullException(nameof(model));
            statusBar?.Render(header);
            Layout();
        }

        /// <summary>
        /// Opens or closes the `DIRECTOR` drawer. It submits nothing: the tools inside it forward the
        /// existing Undo, Restart, and event-log operations exactly as they always have, and opening or
        /// closing the drawer itself changes no match state at all.
        /// </summary>
        public void ToggleDirectorDrawer()
        {
            IsDirectorDrawerOpen = !IsDirectorDrawerOpen;
            ApplyDirectorDrawerState();
        }

        /// <summary>Removes Director-only tools when the lobby hands this HUD an online seat.</summary>
        public void SetDirectorToolsVisible(bool visible)
        {
            if (directorToggle != null)
            {
                directorToggle.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                directorToggle.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
            }

            if (!visible)
            {
                IsDirectorDrawerOpen = false;
                ApplyDirectorDrawerState();
            }
        }

        /// <summary>
        /// Whether the HUD consumes the pointer at this screen position, so the board does not also act
        /// on it. Live element bounds are preferred where the panel has laid out, because a rail's
        /// height follows its content; the deterministic rects are the fallback and are what an Edit Mode
        /// fixture with no live panel reads.
        ///
        /// <para><b>Milestone 8.25c: the screen position is converted before anything is compared.</b>
        /// The argument is a device pixel with its origin at the screen's bottom-left corner - what
        /// <c>PrimitiveHeroPointerInput</c> reads off the pointer - and everything it is measured
        /// against, live bounds included, is a reference unit with its origin at the panel's top-left
        /// one. Until this milestone the conversion was a bare <c>Screen.height - y</c> flip, which was
        /// the whole mapping only while the panel drew one point per pixel. A wrong conversion throws
        /// nothing: the HUD simply blocks the pointer somewhere other than where it is drawn, and every
        /// board click in the game passes through here.</para>
        /// </summary>
        public bool BlocksPointerAt(Vector2 screenPosition)
        {
            var viewport = PanelViewport;
            var panelPosition = MatchHudPanelScaling.ScreenToPanelPoint(
                screenPosition, Screen.width, Screen.height);
            var tray = TryLiveRect(trayRegion, out var liveTray) ? liveTray : GetCardPanelRect();
            var trayBlocksPointer = trayShowsUpgradeComparison
                ? tray.Contains(panelPosition)
                : CalculateCompactHandPointerRect(viewport.x, viewport.y).Contains(panelPosition);
            if (trayBlocksPointer ||
                (TryLiveRect(statusDock, out var liveStatusDock) &&
                    liveStatusDock.Contains(panelPosition)) ||
                CalculateStatusBarRect(viewport.x, viewport.y).Contains(panelPosition) ||
                UnavailableCardsBlockPointerAt(panelPosition) ||
                // Milestone 8.30b. End Turn's corner sits inside the tray band, and the band blocks
                // the pointer only across the compact hand's own footprint at the opposite edge - so
                // without this the board would act on the same click the control consumed. It is asked
                // only while the control is drawn, because the corner is genuinely board when it
                // is not.
                EndTurnBlocksPointerAt(panelPosition) ||
                GetDirectorToggleRect().Contains(panelPosition) ||
                (IsDirectorDrawerOpen && GetDirectorDrawerRect().Contains(panelPosition)) ||
                (diagnostics?.Model?.IsExpanded == true && GetRuleEventLogRect().Contains(panelPosition)))
            {
                return true;
            }

            foreach (var region in HeroRailRegions())
            {
                if (region.Contains(panelPosition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether the HUD's standing regions consume the pointer at a screen position, stated for a
        /// device viewport and answerable with no live panel.
        ///
        /// <para>Milestone 8.25c. It is the same question <see cref="BlocksPointerAt"/> answers, for
        /// the five regions that are drawn at every viewport whatever the match is doing - the status
        /// strip, the Tie Breaker chip, the compact hand and both hero-rail columns - so that the mapping
        /// from a screen pixel to the panel can be checked at a scale other than 1 without a running
        /// scene. <see cref="BlocksPointerAt"/> adds what depends on live state: the tray's own laid-out
        /// box, the rails' laid-out boxes, the `DIRECTOR` toggle and drawer, and an expanded event
        /// log.</para>
        /// </summary>
        public static bool StandingRegionsBlockPointerAt(
            Vector2 screenPosition, float viewportWidth, float viewportHeight)
        {
            var panelPosition = MatchHudPanelScaling.ScreenToPanelPoint(
                screenPosition, viewportWidth, viewportHeight);
            var panel = MatchHudPanelScaling.ReferenceViewport(viewportWidth, viewportHeight);
            return CalculateStatusBarRect(panel.x, panel.y).Contains(panelPosition) ||
                CalculateCompactHandPointerRect(panel.x, panel.y).Contains(panelPosition) ||
                CalculateStatusDockRect(panel.x, panel.y).Contains(panelPosition) ||
                CalculateHeroRailRect(panel.x, panel.y, TeamSide.Red).Contains(panelPosition) ||
                CalculateHeroRailRect(panel.x, panel.y, TeamSide.Blue).Contains(panelPosition);
        }

        private IEnumerable<Rect> HeroRailRegions()
        {
            if (heroOverviews != null)
            {
                foreach (var element in heroOverviews.BlockingRegions)
                {
                    if (TryLiveRect(element, out var live))
                    {
                        yield return live;
                    }
                }

                yield break;
            }

            var viewport = PanelViewport;
            yield return CalculateHeroRailRect(viewport.x, viewport.y, TeamSide.Red);
            yield return CalculateHeroRailRect(viewport.x, viewport.y, TeamSide.Blue);
        }

        private bool UnavailableCardsBlockPointerAt(Vector2 panelPosition)
        {
            var host = hands?.UnavailableCardsRoot;
            if (host == null || host.style.display.value == DisplayStyle.None)
            {
                return false;
            }

            return (TryLiveRect(host, out var live)
                ? live
                : UnavailableCardGroupRect).Contains(panelPosition);
        }

        /// <summary>
        /// Whether End Turn's own corner consumes the pointer here. Milestone 8.30b: the laid-out box
        /// where the panel has one, and the stated rectangle otherwise, which is the preference every
        /// other live-or-stated region in this file makes.
        /// </summary>
        private bool EndTurnBlocksPointerAt(Vector2 panelPosition)
        {
            var host = round?.EndTurnRoot;
            if (host == null || host.style.display.value == DisplayStyle.None)
            {
                return false;
            }

            if (TryLiveRect(host, out var live)
                ? live.Contains(panelPosition)
                : GetEndTurnControlRect().Contains(panelPosition))
            {
                return true;
            }

            // Milestone 8.35f. The seat undo square shares this slot's pointer claim: it stands on
            // the same strip edge, so a press that lands on it is a press on a control rather than
            // on the map behind it.
            var undo = round?.SeatUndoRoot;
            if (undo == null || undo.style.display.value == DisplayStyle.None)
            {
                return false;
            }

            return TryLiveRect(undo, out var liveUndo)
                ? liveUndo.Contains(panelPosition)
                : GetSeatUndoControlRect().Contains(panelPosition);
        }

        private static bool TryLiveRect(VisualElement element, out Rect rect)
        {
            rect = default;
            if (element == null || element.panel == null ||
                element.resolvedStyle.display == DisplayStyle.None)
            {
                return false;
            }

            var bounds = element.worldBound;
            if (float.IsNaN(bounds.width) || float.IsNaN(bounds.height) ||
                bounds.width <= 0f || bounds.height <= 0f)
            {
                return false;
            }

            rect = bounds;
            return true;
        }

        private void Build()
        {
            if (root != null)
            {
                return;
            }

            document = GetComponent<UIDocument>();
            if (document == null)
            {
                document = gameObject.AddComponent<UIDocument>();
            }

            // Milestone 7.9. A copy of a project asset where one exists, and a bare instance only when
            // it does not.
            //
            // The player-build smoke gate found why, and it is the exact class of defect milestone
            // 6.1d built that gate for: something no Editor test can observe. The ICU data Unity's
            // text shaper needs ships only when the build contains at least one `PanelSettings`
            // asset, and a HUD that built its settings entirely with `CreateInstance` gave the build
            // none - which in a built player is a `NullReferenceException` out of
            // `UITKTextHandle.ShapeText` for every label the HUD redraws. The player says so itself:
            // "It will not be present on PanelSettings created at runtime, so make sure the build
            // contains at least one PanelSettings asset". The Editor never showed it, because the
            // Editor has the data loaded either way.
            //
            // Two things to know before editing the asset. Its own `m_ICUDataAsset` field is empty on
            // disk and stays empty; it is not what carries the data, its presence in the build is. And
            // it is instantiated rather than used directly, so the per-panel values set below stay
            // runtime state and never dirty a tracked asset. The comment above about composing in code
            // still holds for the layout, which is the part that has no asset.
            var authored = Resources.Load<PanelSettings>(RuntimePanelSettingsResourcePath);
            panelSettings = authored != null
                ? Instantiate(authored)
                : ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "Guards Configured Match Panel";
            panelSettings.hideFlags = HideFlags.HideAndDontSave;

            // Milestone 8.25c. The coordinate space is declared once, in `MatchHudPanelScaling`, and
            // the committed asset above already carries the same four values - so this agrees with the
            // asset rather than overriding it, and exists for the one path that has no asset to copy.
            // Before this milestone the two lines here said `ConstantPixelSize` at `scale = 1` while
            // the asset carried an inert 1200x800 reference resolution, which is how every layout
            // number in this file came to be a literal device pixel.
            MatchHudPanelScaling.Apply(panelSettings);
            panelSettings.clearColor = false;
            panelSettings.sortingOrder = 100f;
            var theme = Resources.Load<ThemeStyleSheet>(RuntimeThemeResourcePath);
            if (theme != null)
            {
                panelSettings.themeStyleSheet = theme;
            }

            document.panelSettings = panelSettings;
            root = document.rootVisualElement;
            if (root == null)
            {
                // A component added while its GameObject was already enabled can miss the panel's own
                // enable pass. Cycling it is the supported way to ask for one, and it happens once.
                document.enabled = false;
                document.enabled = true;
                root = document.rootVisualElement;
            }

            if (root == null)
            {
                return;
            }

            root.Clear();
            root.name = "gd-root";
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = 0f;
            root.style.top = 0f;
            root.style.right = 0f;
            root.style.bottom = 0f;

            statusBar = new MatchStatusBarUnityView();
            root.Add(statusBar.Root);

            if (heroOverviews != null)
            {
                root.Add(heroOverviews.Root);
            }

            // Milestone 8.30d, the user's final call: the dock draws NO chrome and reserves NO
            // height - "kill this empty box above the cards". Every routine tenant has moved
            // (narration to the top bar, printed actions onto the resolving card, End Turn and
            // Confirm to the corner banners, the hand-status bar retired outright), so an
            // empty frame was all that remained. Anything that still lands here in an ordinary
            // state renders frameless at its own height.
            statusDock = new VisualElement { name = "gd-status-dock" };
            statusDock.style.position = Position.Absolute;
            statusDock.style.flexDirection = FlexDirection.Column;
            statusDock.style.alignItems = Align.FlexStart;
            statusDock.style.width = StatusDockWidth;
            statusDock.style.minWidth = StatusDockWidth;
            statusDock.style.maxWidth = StatusDockWidth;
            root.Add(statusDock);

            directorToggle = HudElements.Control("DIRECTOR", ToggleDirectorDrawer, DirectorToggleHeight);
            directorToggle.name = "gd-director-toggle";
            directorToggle.style.position = Position.Absolute;
            root.Add(directorToggle);

            directorDrawer = HudElements.Panel(MatchHudTheme.RailBackground);
            directorDrawer.name = "gd-director-drawer";
            directorDrawer.style.position = Position.Absolute;
            directorDrawer.style.flexDirection = FlexDirection.Column;
            HudElements.Padding(directorDrawer, 6f);
            HudElements.Border(directorDrawer, MatchHudTheme.PanelBorder, 1f, 4f);
            directorDrawer.Add(undoRestart.Root);
            diagnostics.ToggleRoot.style.marginTop = 4f;
            directorDrawer.Add(diagnostics.ToggleRoot);
            root.Add(directorDrawer);
            root.Add(diagnostics.PanelRoot);

            // The tray is a layout reservation, not a visible card frame. The imported cards already
            // carry their complete printed edges; drawing a panel around the whole hand adds a second
            // outline the user explicitly rejected.
            trayRegion = new VisualElement();
            trayRegion.name = "gd-tray-region";
            trayRegion.style.position = Position.Absolute;
            trayRegion.style.flexDirection = FlexDirection.Column;
            trayRegion.style.paddingLeft = TrayHorizontalPadding;
            trayRegion.style.paddingRight = TrayHorizontalPadding;
            trayRegion.style.paddingTop = TrayCardTopPadding;
            trayRegion.style.paddingBottom = TrayBottomPadding;
            trayScroll = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "gd-tray-scroll"
            };
            trayScroll.style.flexGrow = 1f;
            trayScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            trayScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;

            // Milestone 8.25f2's compact ordinary composition. Five native-ratio cards overlap like
            // a physical hand inside the bottom-left of this transparent full-width band. Round status, actions, hand identity and
            // commitment controls live in the open dock beside the Red rail, above the band. When a
            // mandatory decision hides the hand, those contextual views return here and receive the
            // full tray width, preserving the pre-existing decision layouts.
            trayFlow = new VisualElement { name = "gd-tray-flow" };
            trayFlow.style.flexDirection = FlexDirection.Row;
            trayFlow.style.flexWrap = Wrap.NoWrap;
            trayFlow.style.alignItems = Align.FlexEnd;
            trayFlow.style.width = Length.Percent(100f);

            traySidebar = new VisualElement { name = "gd-tray-sidebar" };
            traySidebar.style.flexDirection = FlexDirection.Column;
            traySidebar.style.flexGrow = 1f;
            traySidebar.style.flexShrink = 1f;
            traySidebar.style.minWidth = 0f;
            traySidebar.style.marginLeft = 0f;

            hands.HeaderRoot.RemoveFromHierarchy();
            hands.Root.style.flexShrink = 0f;
            hands.Root.style.top = OrdinaryCardDownshift;
            round.Root.style.flexShrink = 0f;
            hands.HeaderRoot.style.flexShrink = 0f;
            hands.HeaderRoot.style.marginTop = 6f;
            hands.HeaderRoot.style.marginBottom = 0f;
            trayFlow.Add(hands.Root);
            trayFlow.Add(traySidebar);
            HostContextViewsInTray(false);
            trayScroll.Add(trayFlow);
            trayRegion.Add(trayScroll);
            root.Add(trayRegion);
            hands.UnavailableCardsRoot.RemoveFromHierarchy();
            hands.UnavailableCardsRoot.style.position = Position.Absolute;
            root.Add(hands.UnavailableCardsRoot);
            hands.HoverPreviewRoot.RemoveFromHierarchy();
            root.Add(hands.HoverPreviewRoot);
            hands.DragProxyRoot.RemoveFromHierarchy();
            root.Add(hands.DragProxyRoot);
            hands.SetMapDropPredicate(IsMapDropPoint);
            root.Add(round.PreviewRoot);
            round.DefenseCardHoverPreviewRoot.RemoveFromHierarchy();
            root.Add(round.DefenseCardHoverPreviewRoot);
            round.DefenseDragProxyRoot.RemoveFromHierarchy();
            root.Add(round.DefenseDragProxyRoot);
            round.SetMapDropPredicate(IsMapDropPoint);

            // Milestone 8.30b, by the user-approved amendment. Two of this view's surfaces leave the
            // contextual region: the narration goes into the top status bar, which is the widest line
            // on the panel and directly under the round and Life counters it qualifies, and End Turn
            // takes the status bar's top-right slot as a control of its own. Both are re-parented
            // rather than rebuilt, so the round presenter remains the only thing that derives either.
            statusBar.HostNarration(round.NarrationRoot);
            round.EndTurnRoot.RemoveFromHierarchy();
            round.EndTurnRoot.style.position = Position.Absolute;
            root.Add(round.EndTurnRoot);
            // Milestone 8.35f. The seat's own undo square takes the slot immediately left of End
            // Turn on the same edge, re-parented on the same terms and for the same reason.
            round.SeatUndoRoot.RemoveFromHierarchy();
            round.SeatUndoRoot.style.position = Position.Absolute;
            root.Add(round.SeatUndoRoot);
            // Empty-hand Pass and End Turn are mutually exclusive projections of the same top-right
            // status slot. Keeping separate passive views preserves their existing presenters and
            // commands; the pointer-transparent empty host cannot cover whichever control is live.
            // Commitment confirmation alone uses the lane immediately below DIRECTOR.
            hands.CornerActionsRoot.RemoveFromHierarchy();
            hands.CornerActionsRoot.style.position = Position.Absolute;
            root.Add(hands.CornerActionsRoot);
            hands.ConfirmActionRoot.RemoveFromHierarchy();
            hands.ConfirmActionRoot.style.position = Position.Absolute;
            hands.SetConfirmLayoutReady(false);
            root.Add(hands.ConfirmActionRoot);
            round.DeclineDefenseRoot.RemoveFromHierarchy();
            round.DeclineDefenseRoot.style.position = Position.Absolute;
            root.Add(round.DeclineDefenseRoot);
            round.OptionalStepActionsRoot.RemoveFromHierarchy();
            round.OptionalStepActionsRoot.style.position = Position.Absolute;
            root.Add(round.OptionalStepActionsRoot);
            if (heroOverviews != null)
            {
                // Only the revealed face leaves the rail layer. Keeping the rail columns where they
                // are while putting this one pointer-transparent card last in paint order prevents
                // Director and the status dock from cutting across the face being read.
                heroOverviews.HoverPreviewRoot.RemoveFromHierarchy();
                root.Add(heroOverviews.HoverPreviewRoot);
            }

            ApplyDirectorDrawerState();
            Layout();
        }

        /// <summary>
        /// Where the runtime theme lives. Its absence is tolerated - every element in this HUD sets its
        /// own colours, sizes, and borders - so a build that cannot resolve it still draws a readable,
        /// if plainer, panel rather than nothing.
        /// </summary>
        public const string RuntimeThemeResourcePath = "UI/GuardsRuntimeTheme";

        /// <summary>
        /// The imported panel-settings asset the runtime panel is copied from. Milestone 7.9 added it
        /// so a built player has the text data Unity only attaches at import; see <see cref="Build"/>.
        /// </summary>
        public const string RuntimePanelSettingsResourcePath = "UI/GuardsRuntimePanelSettings";

        /// <summary>
        /// Applies the current viewport to every absolutely-placed region. Called on every header
        /// render, which is every projection refresh, so a resized Game View is followed.
        /// </summary>
        public void Layout()
        {
            if (root == null)
            {
                return;
            }

            // Milestone 8.25c: the panel's own rectangle rather than the screen's. Every style write
            // below lands on a panel whose coordinate space is the reference one, so a device pixel
            // here would be laid out as though it were a reference unit.
            var viewport = PanelViewport;
            var width = viewport.x;
            var height = viewport.y;
            var bar = CalculateStatusBarRect(width, height);
            statusBar?.Layout(bar, GetTieBreakerIndicatorRect());
            // Milestone 8.25f2. The tray is a transparent full-width layout band. Its fixed ordinary
            // height fits one 420-unit face plus only card chrome, while the five faces overlap inside
            // the compact left footprint. Metadata sits in the separate status dock to the map's left.
            trayShowsUpgradeComparison = round?.HasUpgradeComparisonBand == true;
            if (trayScroll != null)
            {
                trayScroll.verticalScrollerVisibility = trayShowsUpgradeComparison
                    ? ScrollerVisibility.Auto
                    : ScrollerVisibility.Hidden;
            }
            if (hands?.Root != null)
            {
                hands.Root.style.top = trayShowsUpgradeComparison ? 0f : OrdinaryCardDownshift;
            }
            var ordinaryTray = GetCardPanelRect();
            var tray = trayShowsUpgradeComparison
                ? CalculateUpgradeComparisonPanelRect(width, height)
                : GetCardPanelRect();
            if (traySidebar != null)
            {
                var sideClearance = trayShowsUpgradeComparison
                    ? CalculateHeroRailWidth(width) + (Margin * 2f)
                    : 0f;
                traySidebar.style.marginLeft = sideClearance;
                traySidebar.style.marginRight = sideClearance;
            }
            trayRegion.style.left = tray.x;
            trayRegion.style.top = tray.y;
            trayRegion.style.right = StyleKeyword.Auto;
            trayRegion.style.bottom = StyleKeyword.Auto;
            trayRegion.style.width = tray.width;
            trayRegion.style.height = tray.height;
            trayRegion.style.maxWidth = tray.width;
            // Milestone 8.30j: Upgrade keeps the bounded layout reservation and short-view scroll
            // fallback but paints no comparison band or enclosing edge around its six cards.
            trayRegion.style.backgroundColor = Color.clear;
            trayRegion.style.borderTopWidth = 0f;
            trayRegion.style.borderTopColor = Color.clear;
            // Milestone 8.29b: the upgrade surface is only one card row. Its own bounded rectangle may
            // be a little taller than the ordinary hand, while every standing surface continues to use
            // ordinaryTray. Opening Level Up therefore never moves a plaque or reframes the board.
            trayRegion.style.maxHeight = tray.height;
            var unavailableRect = CalculateUnavailableCardGroupRect(width, height);
            var unavailableHost = hands?.UnavailableCardsRoot;
            if (unavailableHost != null)
            {
                unavailableHost.style.left = unavailableRect.x;
                unavailableHost.style.top = unavailableRect.y;
                unavailableHost.style.right = StyleKeyword.Auto;
                unavailableHost.style.bottom = StyleKeyword.Auto;
                unavailableHost.style.width = unavailableRect.width;
                unavailableHost.style.height = unavailableRect.height;
            }
            var playfield = CalculatePlayfieldRect(width, height, ordinaryTray);
            heroOverviews?.Layout(
                CalculateHeroRailRect(width, height, TeamSide.Red, ordinaryTray.yMin),
                CalculateHeroRailRect(width, height, TeamSide.Blue, ordinaryTray.yMin),
                playfield);
            LayoutDirectorControls();
            LayoutEndTurnControl();
            LayoutOptionalStepControls();
            diagnostics?.Layout(GetRuleEventLogRect());
            PublishCameraSafeLayout(ordinaryTray, playfield, viewport);
        }

        /// <summary>
        /// Keeps exactly one interaction context usable: while a mandatory decision stands, the ordinary
        /// card tray is not merely inert but absent, so there is nothing on screen that looks choosable
        /// and is not.
        /// </summary>
        public void ApplyExclusiveContext()
        {
            if (hands == null || round == null)
            {
                return;
            }

            hands.SetPrintedActionRegions(round.Model?.PrintedActionRegions);
            var retainsOrdinaryHand = round.RetainsOrdinaryHand;
            var usesTray = (round.HasExclusiveDecision && !retainsOrdinaryHand) ||
                minionChoice?.Model != null;
            var hostChanged = usesTray != contextUsesTray;
            if (hostChanged)
            {
                HostContextViewsInTray(usesTray);
            }

            hands.SetContextOnly(retainsOrdinaryHand);
            hands.SetHidden(usesTray);
            // Milestone 8.8a. The tray's own bound depends on what the contextual region is drawing, and
            // that can change without the header changing - a paused prompt is rendered into the region
            // by its own presenter. Re-laying out only on the transition keeps this a two-state answer
            // rather than twenty style writes a frame.
            if (hostChanged || round.HasUpgradeComparisonBand != trayShowsUpgradeComparison)
            {
                Layout();
            }
        }

        private void HostContextViewsInTray(bool useTray)
        {
            if (traySidebar == null || statusDock == null)
            {
                return;
            }

            var host = useTray ? traySidebar : statusDock;
            round.Root.RemoveFromHierarchy();
            minionChoice.Root.RemoveFromHierarchy();
            hands.HeaderRoot.RemoveFromHierarchy();
            host.Add(round.Root);
            host.Add(minionChoice.Root);
            host.Add(hands.HeaderRoot);
            traySidebar.style.display = useTray ? DisplayStyle.Flex : DisplayStyle.None;
            statusDock.style.display = useTray ? DisplayStyle.None : DisplayStyle.Flex;
            contextUsesTray = useTray;
        }

        private void ApplyDirectorDrawerState()
        {
            if (directorDrawer == null || directorToggle == null)
            {
                return;
            }

            directorDrawer.style.display = IsDirectorDrawerOpen ? DisplayStyle.Flex : DisplayStyle.None;
            directorToggle.text = IsDirectorDrawerOpen ? "DIRECTOR ▾" : "DIRECTOR";
        }

        private void LateUpdate()
        {
            ApplyExclusiveContext();
            round?.UpdateDefensePreviewDismissal(Time.realtimeSinceStartupAsDouble);
        }

        private void OnDestroy()
        {
            if (heroOverviews != null)
            {
                heroOverviews.ColumnGeometryChanged -= LayoutResolvedHudGeometry;
            }

            if (panelSettings != null)
            {
                Destroy(panelSettings);
                panelSettings = null;
            }
        }

        public static Rect CalculateStatusBarRect(float panelWidth, float panelHeight) =>
            new Rect(0f, 0f, Mathf.Max(0f, panelWidth), Mathf.Min(StatusBarHeight, Mathf.Max(0f, panelHeight)));

        /// <summary>
        /// The tray's full-width reserved bottom band.
        ///
        /// <para>Milestone 8.25f2 replaces the 1280-wide overlay after the enlarged native-ratio cards
        /// exposed both of its costs: five cards required a horizontal scrollbar and its right corner
        /// covered the map. The band now spans the viewport inside the standing margins, while the board
        /// camera and rail columns stop at its top edge. The metadata lane beside the five-card row keeps
        /// the band to 470 units, leaving a 902-unit playfield and enough height for the measured
        /// three-rail stack on each side at the 2560-by-1440 reference.</para>
        /// </summary>
        public static Rect CalculateCardPanelRect(float panelWidth, float panelHeight)
        {
            return CalculateCardPanelRect(panelWidth, panelHeight, CardPanelHeight);
        }

        /// <summary>
        /// The ordinary hand's actual pointer-owning footprint inside the full-width layout band.
        /// The rest of that transparent band belongs to the board: treating the layout reservation as
        /// a hit surface made lower Blue heroes visibly unobstructed but impossible to click.
        /// Mandatory decisions deliberately continue to block their complete live tray instead.
        /// </summary>
        public static Rect CalculateCompactHandPointerRect(float panelWidth, float panelHeight)
        {
            var tray = CalculateCardPanelRect(panelWidth, panelHeight);
            return new Rect(
                tray.x,
                tray.y,
                Mathf.Min(tray.width, CompactHandRightEdge),
                tray.height);
        }

        /// <summary>
        /// Milestone 8.30l's fixed right-side reservation. Five complete faces overlap vertically
        /// beneath the live Blue rail, leaving the broad map region untouched. The host draws no
        /// chrome and becomes absent when empty; the fixed reservation
        /// keeps card lifecycle refreshes from moving the map or hero rails as groups exchange members.
        /// </summary>
        public static Rect CalculateUnavailableCardGroupRect(float panelWidth, float panelHeight)
        {
            var width = Mathf.Min(panelWidth, DirectorHandsUnityView.PeekCardOuterWidth);
            var height = Mathf.Min(
                Mathf.Max(0f, panelHeight - StatusBarHeight),
                DirectorHandsUnityView.CompactUnavailableStackHeight(OrdinaryHandCardCount));
            var bottom = Mathf.Max(StatusBarHeight + height, panelHeight - Margin);
            return new Rect(
                Mathf.Max(0f, panelWidth - Margin - width),
                Mathf.Max(StatusBarHeight, bottom - height),
                width,
                height);
        }

        private static Rect CalculateCardPanelRect(
            float panelWidth,
            float panelHeight,
            float preferredHeight)
        {
            // The ordinary strip is visually only its cards, so its horizontal reservation reaches
            // the screen edge. This removes the last invisible inset on the left and gives the same
            // width back to the lower board arm on the right.
            var left = 0f;
            var width = Mathf.Max(0f, panelWidth);
            var height = Mathf.Min(
                Mathf.Max(0f, preferredHeight),
                Mathf.Max(0f, panelHeight - (Margin * 2f) - StatusBarHeight));
            return new Rect(
                left,
                Mathf.Max(Margin, panelHeight - height - Margin),
                width,
                height);
        }

        public static Rect CalculateUpgradeComparisonPanelRect(
            float panelWidth, float panelHeight) =>
            CalculateCardPanelRect(panelWidth, panelHeight, UpgradeComparisonBandHeight);

        public static Rect CalculatePlayfieldRect(float panelWidth, float panelHeight)
        {
            var tray = CalculateCardPanelRect(panelWidth, panelHeight);
            return CalculatePlayfieldRect(panelWidth, panelHeight, tray);
        }

        private static Rect CalculatePlayfieldRect(
            float panelWidth,
            float panelHeight,
            Rect tray)
        {
            var availableHeight = Mathf.Max(
                0f,
                tray.yMin - StatusBarHeight - BoardTraySeparation);
            var red = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Red, tray.yMin);
            var blue = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Blue, tray.yMin);
            var right = Mathf.Max(0f, blue.xMin - Margin);
            var availableWidth = Mathf.Max(0f, right - red.xMax - Margin);
            var height = Mathf.Min(availableHeight, availableWidth / BoardViewportAspect);
            var width = height * BoardViewportAspect;
            return new Rect(
                Mathf.Max(0f, right - width),
                StatusBarHeight,
                width,
                height);
        }

        /// <summary>
        /// The ordinary round/hand information block in the open area beside the Red rail. Its fixed
        /// largest-state right edge is the same boundary the upper hex-safe arm starts beyond, and its
        /// bottom edge stops before the card band, so state changes move neither map nor cards.
        /// </summary>
        public static Rect CalculateStatusDockRect(float panelWidth, float panelHeight)
        {
            var tray = CalculateCardPanelRect(panelWidth, panelHeight);
            var playfield = CalculatePlayfieldRect(panelWidth, panelHeight, tray);
            return CalculateStatusDockRect(panelWidth, panelHeight, tray, playfield);
        }

        private static Rect CalculateStatusDockRect(
            float panelWidth,
            float panelHeight,
            Rect tray,
            Rect playfield)
        {
            var red = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Red, tray.yMin);
            var left = Mathf.Min(Mathf.Max(0f, panelWidth), red.xMin);
            var right = Mathf.Min(
                Mathf.Max(left, panelWidth - Margin),
                left + StatusDockWidth);
            var height = Mathf.Min(StatusDockHeight, playfield.height);
            return new Rect(
                left,
                Mathf.Max(playfield.yMin, playfield.yMax - height),
                Mathf.Max(0f, right - left),
                height);
        }

        /// <summary>
        /// One team's rail column: the screen edge under the status strip and above the reserved card
        /// band. Milestone 8.25c's 2560-by-1440 reference makes this about 878 units high with the
        /// enlarged ordinary tray, still above the measured 733 required by three complete rails.
        /// Milestone 8.25f2 stops the column at the tray instead of relying on z-order and hidden bottom
        /// padding, while the unchanged width rule keeps both rails together under half the viewport.
        /// </summary>
        public static Rect CalculateHeroRailRect(float panelWidth, float panelHeight, TeamSide side)
        {
            var tray = CalculateCardPanelRect(panelWidth, panelHeight);
            return CalculateHeroRailRect(panelWidth, panelHeight, side, tray.yMin);
        }

        private static Rect CalculateHeroRailRect(
            float panelWidth,
            float panelHeight,
            TeamSide side,
            float bottomBoundary)
        {
            var width = CalculateHeroRailWidth(panelWidth);
            var x = side == TeamSide.Red
                ? Margin
                : Mathf.Max(Margin, panelWidth - width - Margin);
            var top = StatusBarHeight + Margin;
            return new Rect(x, top, width, Mathf.Max(0f, bottomBoundary - top - Margin));
        }

        private static float CalculateHeroRailWidth(float panelWidth) =>
            // One plaque gets at most a quarter of the panel minus its outside margin. Two plaques
            // therefore remain strictly below half even when the preferred 264 cannot fit.
            Mathf.Min(HeroRailWidth, Mathf.Max(0f, (panelWidth / 4f) - Margin));

        private static Rect GetCardPanelRect() =>
            CalculateCardPanelRect(PanelViewport.x, PanelViewport.y);

        /// <summary>
        /// The Tie Breaker badge's own rectangle in the centre of the status strip. Milestone 8.25c made it a
        /// stated function of the viewport rather than a reader of the current screen, so a check can
        /// ask where it is at a viewport that is not the one the fixture is running at.
        /// </summary>
        public static Rect CalculateTieBreakerIndicatorRect(float panelWidth, float panelHeight)
        {
            var diameter = Mathf.Min(TieBreakerDiameter, Mathf.Max(0f, panelWidth));
            return new Rect(
                Mathf.Max(0f, (panelWidth - diameter) * 0.5f),
                (Mathf.Min(StatusBarHeight, Mathf.Max(0f, panelHeight)) - diameter) * 0.5f,
                diameter,
                diameter);
        }

        private static Rect GetTieBreakerIndicatorRect() =>
            CalculateTieBreakerIndicatorRect(PanelViewport.x, PanelViewport.y);

        /// <summary>
        /// End Turn's own rectangle in the status bar's top-right slot, stated as a function of the
        /// viewport for the reason <see cref="CalculateTieBreakerIndicatorRect"/> is: a fixture with no
        /// live panel has to be able to ask where it lands at a viewport it is not running at.
        ///
        /// <para>The slot replaces the Tie Breaker's former right-side position while the new round
        /// badge occupies the centre. It is fixed independently of the bottom-right unavailable-card
        /// fan, so card lifecycle refreshes cannot move the control.</para>
        /// </summary>
        public static Rect CalculateEndTurnControlRect(float panelWidth, float panelHeight)
        {
            var blueRail = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Blue);
            var width = blueRail.width;
            var height = Mathf.Min(EndTurnControlHeight, Mathf.Max(0f, panelHeight));
            return new Rect(
                blueRail.x,
                Mathf.Min(EndTurnControlTopInset, Mathf.Max(0f, panelHeight - height)),
                width,
                height);
        }

        private static Rect GetEndTurnControlRect() =>
            CalculateEndTurnControlRect(PanelViewport.x, PanelViewport.y);

        /// <summary>
        /// Milestone 8.35f. The seat undo square, sharing End Turn's top edge and standing
        /// immediately to its left. It is clamped to the panel like every other control here, so a
        /// narrow viewport shrinks it rather than pushing it off the left edge.
        /// </summary>
        public static Rect CalculateSeatUndoControlRect(float panelWidth, float panelHeight)
        {
            var endTurn = CalculateEndTurnControlRect(panelWidth, panelHeight);
            var side = Mathf.Min(
                SeatUndoControlSide,
                Mathf.Max(0f, Mathf.Min(panelHeight, endTurn.x)));
            return new Rect(endTurn.x - side, endTurn.y, side, side);
        }

        public Rect SeatUndoControlRect => GetSeatUndoControlRect();

        private static Rect GetSeatUndoControlRect() =>
            CalculateSeatUndoControlRect(PanelViewport.x, PanelViewport.y);

        /// <summary>
        /// Where the `DIRECTOR` toggle stands: directly below the live third Red hero. Level Up no
        /// longer relocates it because the comparison surface is one bounded row between the rails.
        /// </summary>
        private Rect CalculateDirectorToggleRect()
        {
            var viewport = PanelViewport;
            var tray = CalculateCardPanelRect(viewport.x, viewport.y);
            var rail = CalculateHeroRailRect(
                viewport.x, viewport.y, TeamSide.Red, tray.yMin);
            var playfield = CalculatePlayfieldRect(viewport.x, viewport.y, tray);
            var lowestY = rail.yMin;
            if (heroOverviews?.TryGetRedColumnBounds(out var liveRed) == true)
            {
                lowestY = Mathf.Max(lowestY, liveRed.yMax + DirectorRailGap);
            }

            return new Rect(
                rail.xMin,
                Mathf.Min(
                    lowestY,
                    Mathf.Max(rail.yMin, playfield.yMax - DirectorToggleHeight - Margin)),
                Mathf.Min(DirectorToggleWidth, rail.width),
                DirectorToggleHeight);
        }

        private Rect GetDirectorToggleRect() =>
            TryLiveRect(directorToggle, out var live) ? live : CalculateDirectorToggleRect();

        private Rect GetConfirmFacedownControlRect()
        {
            var control = hands?.ConfirmActionRoot?.Q<VisualElement>("gd-confirm-control");
            if (TryLiveRect(control, out var live))
            {
                return live;
            }

            var decline = round?.DeclineDefenseRoot?.Q<VisualElement>(
                "gd-decline-defense-control");
            if (TryLiveRect(decline, out live))
            {
                return live;
            }

            return CalculateConfirmFacedownControlRect(CalculateDirectorToggleRect());
        }

        private Rect CalculateConfirmFacedownControlRect(Rect toggle)
        {
            // This runs immediately after LayoutDirectorControls writes the toggle's new style.
            // worldBound still describes the preceding frame at that moment, which put Confirm
            // beside an old top-left position when a tall/narrow viewport moved DIRECTOR below the
            // resolved hero column. Recompute from the same current inputs instead of reading that
            // one-frame-stale bound.
            var viewport = PanelViewport;
            var tray = CalculateCardPanelRect(viewport.x, viewport.y);
            var top = toggle.yMax + ConfirmDirectorGap;
            return new Rect(
                toggle.x,
                top,
                Mathf.Min(CornerActionControlWidth, Mathf.Max(0f, viewport.x - Margin - toggle.x)),
                Mathf.Min(
                    DirectorHandsUnityView.CornerBannerHeight,
                    Mathf.Max(0f, tray.yMin - top)));
        }

        private void LayoutResolvedHudGeometry()
        {
            if (root == null || trayRegion == null)
            {
                return;
            }

            LayoutDirectorControls();
            // The resolved hero column can move DIRECTOR after the ordinary Layout pass. Confirm
            // must follow in the same callback; otherwise it retains the preceding frame's top and
            // separates from DIRECTOR after a resize or first live geometry resolution.
            LayoutEndTurnControl();
            LayoutOptionalStepControls();
            var viewport = PanelViewport;
            var tray = CalculateCardPanelRect(viewport.x, viewport.y);
            var playfield = CalculatePlayfieldRect(viewport.x, viewport.y, tray);
            PublishCameraSafeLayout(tray, playfield, viewport);
        }

        private void PublishCameraSafeLayout(Rect tray, Rect playfield, Vector2 viewport)
        {
            var red = CalculateHeroRailRect(viewport.x, viewport.y, TeamSide.Red, tray.yMin);
            var blue = CalculateHeroRailRect(viewport.x, viewport.y, TeamSide.Blue, tray.yMin);
            // Status and Director live on the left; Blue's rail owns the right edge. Between them the
            // broad part of the map may use the whole remaining screen height. Compact hand previews
            // and their full hover reveals stay wholly left of this arm.
            var upperLeft = Mathf.Max(
                red.xMax + BoardHexUiGap,
                red.xMin + StatusDockWidth + BoardHexUiGap);
            var unavailableRect = CalculateUnavailableCardGroupRect(viewport.x, viewport.y);
            var upperRight = Mathf.Max(
                upperLeft,
                Mathf.Min(blue.xMin, unavailableRect.xMin) - BoardHexUiGap);
            var upperTop = StatusBarHeight + BoardHexUiGap;
            var upperBottom = Mathf.Max(upperTop, viewport.y - BoardHexUiGap);
            var upper = new Rect(
                upperLeft,
                upperTop,
                upperRight - upperLeft,
                upperBottom - upperTop);

            // Five cards occupy only the lower-left. Once the resolved Blue stack has ended, the area
            // to their right is empty and can receive the board's tapered end. Using the live stack
            // edge is the difference between genuinely free space and the old conservative rail rect.
            var lowerTop = upperBottom;
            if (heroOverviews?.TryGetBlueColumnBounds(out var liveBlue) == true)
            {
                lowerTop = Mathf.Min(lowerTop, liveBlue.yMax + BoardHexUiGap);
            }

            // The lower arm begins after the furthest possible full-size hover reveal: the fifth
            // compact slot's left edge plus one standard card width. This keeps a revealed card clear
            // of every playable hex while reclaiming the width the other four full faces used to hold.
            var lowerLeft = CompactHandRightEdge + BoardCardHexUiGap;
            var lowerRight = Mathf.Max(
                lowerLeft,
                unavailableRect.xMin - BoardCardHexUiGap);
            var lowerBottom = Mathf.Max(lowerTop, viewport.y - BoardHexUiGap);
            var lower = new Rect(
                lowerLeft,
                lowerTop,
                lowerRight - lowerLeft,
                lowerBottom - lowerTop);

            CameraSafeLayout = new BoardCameraSafeLayout(upper, lower, viewport);
            PlayfieldLayoutChanged?.Invoke(CameraSafeLayout);
        }

        /// <summary>
        /// Places End Turn in the status bar's top-right slot. Milestone 8.30l.
        /// </summary>
        private void LayoutEndTurnControl()
        {
            var rect = GetEndTurnControlRect();
            var host = round?.EndTurnRoot;
            if (host != null)
            {
                host.style.left = rect.x;
                host.style.top = rect.y;
                host.style.width = rect.width;
                host.style.height = rect.height;
            }

            var undoRect = GetSeatUndoControlRect();
            var undoHost = round?.SeatUndoRoot;
            if (undoHost != null)
            {
                undoHost.style.left = undoRect.x;
                undoHost.style.top = undoRect.y;
                undoHost.style.width = undoRect.width;
                undoHost.style.height = undoRect.height;
            }

            var corner = hands?.CornerActionsRoot;
            if (corner != null)
            {
                corner.style.left = rect.x;
                corner.style.top = rect.y;
                corner.style.width = rect.width;
                corner.style.height = rect.height;
            }

            var confirm = hands?.ConfirmActionRoot;
            if (confirm != null)
            {
                var confirmRect = CalculateConfirmFacedownControlRect(
                    CalculateDirectorToggleRect());
                confirm.style.left = confirmRect.x;
                confirm.style.top = confirmRect.y;
                confirm.style.width = confirmRect.width;
                confirm.style.height = confirmRect.height;
                hands.SetConfirmLayoutReady(true);
            }

            var decline = round?.DeclineDefenseRoot;
            if (decline != null)
            {
                var declineRect = CalculateConfirmFacedownControlRect(
                    CalculateDirectorToggleRect());
                decline.style.left = declineRect.x;
                decline.style.top = declineRect.y;
                decline.style.width = declineRect.width;
                decline.style.height = declineRect.height;
            }
        }

        /// <summary>
        /// Places a compact optional-step row at the compact hand's left edge, inset only enough to
        /// retain the banner's complete outline and shine. It occupies no board-safe surface.
        /// </summary>
        private void LayoutOptionalStepControls()
        {
            var host = round?.OptionalStepActionsRoot;
            if (host == null)
            {
                return;
            }

            var rect = round.HasTeamAgreementDecision
                ? CalculateTeamAgreementControlRect(PanelViewport.x, PanelViewport.y)
                : CalculateOptionalStepControlRect(PanelViewport.x, PanelViewport.y);
            host.style.left = rect.x;
            host.style.top = rect.y;
            host.style.width = rect.width;
            host.style.height = rect.height;
        }

        public static Rect CalculateOptionalStepControlRect(float panelWidth, float panelHeight)
        {
            var tray = CalculateCardPanelRect(panelWidth, panelHeight);
            var left = tray.xMin + OptionalStepEdgeClearance;
            var right = Mathf.Min(
                CompactHandRightEdge - OptionalStepEdgeClearance,
                Mathf.Max(left, panelWidth - Margin));
            var desiredWidth = RoundActionUnityView.OptionalStepActionWidth * 2f +
                RoundActionUnityView.OptionalStepActionGap;
            var height = Mathf.Min(
                RoundActionUnityView.OptionalStepActionHeight,
                Mathf.Max(0f, tray.yMin - StatusBarHeight - Margin));
            return new Rect(
                left,
                Mathf.Max(StatusBarHeight + Margin, tray.yMin - height - OptionalStepHandGap),
                Mathf.Min(desiredWidth, Mathf.Max(0f, right - left)),
                height);
        }

        public static Rect CalculateTeamAgreementControlRect(float panelWidth, float panelHeight)
        {
            var tray = CalculateCardPanelRect(panelWidth, panelHeight);
            var left = tray.xMin + OptionalStepEdgeClearance;
            var right = Mathf.Min(
                CompactHandRightEdge - OptionalStepEdgeClearance,
                Mathf.Max(left, panelWidth - Margin));
            var height = Mathf.Min(
                RoundActionUnityView.TeamAgreementHeight,
                Mathf.Max(0f, tray.yMin - StatusBarHeight - Margin));
            return new Rect(
                left,
                Mathf.Max(StatusBarHeight + Margin, tray.yMin - height - OptionalStepHandGap),
                Mathf.Max(0f, right - left),
                height);
        }

        private void LayoutDirectorControls()
        {
            if (root == null || directorToggle == null || directorDrawer == null)
            {
                return;
            }

            var toggle = CalculateDirectorToggleRect();
            directorToggle.style.left = toggle.x;
            directorToggle.style.top = toggle.y;
            directorToggle.style.width = toggle.width;
            directorToggle.style.height = toggle.height;

            var viewport = PanelViewport;
            var tray = CalculateCardPanelRect(viewport.x, viewport.y);
            var statusTop = toggle.yMax + DirectorStatusGap;
            var statusWidth = Mathf.Min(
                StatusDockWidth,
                Mathf.Max(0f, viewport.x - Margin - toggle.x));
            statusDock.style.left = toggle.x;
            statusDock.style.top = statusTop;
            // Width pinned; height is the content's own - the dock reserves nothing and draws
            // nothing when empty (the user's final 8.30d call).
            statusDock.style.width = statusWidth;
            statusDock.style.minWidth = statusWidth;
            statusDock.style.maxWidth = statusWidth;
            statusDock.style.maxHeight = Mathf.Max(0f, tray.yMin - statusTop);
            var drawer = CalculateDirectorDrawerRect(toggle);
            directorDrawer.style.left = drawer.x;
            directorDrawer.style.top = drawer.y;
            directorDrawer.style.width = drawer.width;
            directorDrawer.style.height = drawer.height;
        }

        /// <summary>
        /// The drawer opens upward from the toggle, and downward from it when the toggle has moved to the
        /// top of its column, because there is nothing above it there to open into.
        /// </summary>
        private Rect GetDirectorDrawerRect()
        {
            var toggle = GetDirectorToggleRect();
            return CalculateDirectorDrawerRect(toggle);
        }

        private Rect CalculateDirectorDrawerRect(Rect toggle)
        {
            return new Rect(
                toggle.x,
                Mathf.Max(Margin, toggle.yMin - DirectorDrawerHeight - 6f),
                DirectorDrawerWidth,
                DirectorDrawerHeight);
        }

        /// <summary>
        /// The log takes the column to the right of the tray when there is one wide enough, and the
        /// band above the tray otherwise. Milestone 8.25 changed neither branch and moved both: the
        /// right-hand column now stops short of the Blue rail, which owns the whole edge, and the
        /// above-the-tray branch starts clear of the `DIRECTOR` column.
        /// </summary>
        private static Rect GetRuleEventLogRect()
        {
            var viewport = PanelViewport;
            var cardPanel = CalculateCardPanelRect(viewport.x, viewport.y);
            var railWidth = CalculateHeroRailWidth(viewport.x) + Margin;
            var availableRightWidth = viewport.x - cardPanel.xMax - railWidth - (Margin * 2f);
            if (availableRightWidth >= 420f)
            {
                var width = Mathf.Min(EventLogPreferredWidth, availableRightWidth);
                var height = Mathf.Min(
                    EventLogPreferredHeight,
                    Mathf.Max(160f, viewport.y - StatusBarHeight - (Margin * 3f)));
                return new Rect(
                    viewport.x - width - railWidth - Margin,
                    StatusBarHeight + (Margin * 2f),
                    width,
                    height);
            }

            var availableAboveHeight = Mathf.Max(140f, cardPanel.y - StatusBarHeight - (Margin * 2f));
            var aboveHeight = Mathf.Min(EventLogPreferredHeight, availableAboveHeight);

            // The band above the tray is also the `DIRECTOR` toggle's and its drawer's, and this log is
            // tall enough to cover both. It starts clear of that column, for the reason
            // the drawer is the only route to Undo, and a log the player opened is not a reason to lose
            // it. Latent until milestone 8.25
            // moved the tray left, which put the two on the same x and left `RESET` behind the log's
            // own scroll view. Measured from the drawer's column rather than from the tray's edge,
            // because since the rails took the screen edge those are no longer the same x.
            var aboveLeft = Mathf.Max(
                cardPanel.x,
                CalculateHeroRailRect(viewport.x, viewport.y, TeamSide.Red).xMax +
                    Margin + DirectorDrawerWidth + Margin);
            return new Rect(
                aboveLeft,
                Mathf.Max(StatusBarHeight + Margin, cardPanel.y - aboveHeight - Margin),
                Mathf.Max(0f, cardPanel.xMax - aboveLeft),
                aboveHeight);
        }
    }
}
