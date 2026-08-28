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
            int turnIndex = 0)
        {
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
            TieBreakerLabel = !tieBreaker.IsDefined
                ? "TIE ?"
                : tieBreaker.RevealedSide == TeamSide.Red ? "TIE [R] RED" : "TIE [B] BLUE";
        }

        public TieBreakerState TieBreaker { get; }
        public QuickGameSetupState Setup { get; }

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
        // One horizontal Level Up comparison: a complete card, its option facts, the pair label and
        // the prompt line. It may extend a little above the ordinary hand, but never changes the hero
        // columns or the camera-safe board surface around the legacy near-full-height context.
        public const float UpgradeComparisonBandHeight = CardFootprint.Height + 130f;
        // Keeps UI Toolkit border/pixel snapping from turning two mathematically adjacent rectangles
        // into a live one-pixel overlap at non-identity panel scales.
        private const float BoardTraySeparation = 2f;
        private const float TieBreakerWidth = 200f;
        private const float EventLogPreferredWidth = 720f;
        private const float EventLogPreferredHeight = 360f;

        /// <summary>
        /// The standing status strip's height, in reference units. The Tie Breaker chip sits inside it.
        /// </summary>
        public const float StatusBarHeight = 56f;

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
        public Rect TieBreakerIndicatorRect => GetTieBreakerIndicatorRect();
        public Rect RuleEventLogPanelRect => GetRuleEventLogRect();
        public Rect StatusBarRect => CalculateStatusBarRect(PanelViewport.x, PanelViewport.y);
        public Rect RedHeroRailRect =>
            CalculateHeroRailRect(PanelViewport.x, PanelViewport.y, TeamSide.Red);
        public Rect BlueHeroRailRect =>
            CalculateHeroRailRect(PanelViewport.x, PanelViewport.y, TeamSide.Blue);
        public BoardCameraSafeLayout CameraSafeLayout { get; private set; }

        /// <summary>Whether the compact `DIRECTOR` drawer is open. Local state; it changes no match state.</summary>
        public bool IsDirectorDrawerOpen { get; private set; }

        /// <summary>The panel this HUD draws on, for a test that needs to look at the built tree.</summary>
        public VisualElement RootElement => root;

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
                GetTieBreakerIndicatorRect().Contains(panelPosition) ||
                CalculateStatusBarRect(viewport.x, viewport.y).Contains(panelPosition) ||
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
                CalculateTieBreakerIndicatorRect(panel.x, panel.y).Contains(panelPosition) ||
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

            statusDock = HudElements.Panel(MatchHudTheme.PanelBackground);
            statusDock.name = "gd-status-dock";
            statusDock.style.position = Position.Absolute;
            statusDock.style.flexDirection = FlexDirection.Column;
            statusDock.style.alignItems = Align.FlexStart;
            statusDock.style.width = StatusDockWidth;
            statusDock.style.minWidth = StatusDockWidth;
            statusDock.style.maxWidth = StatusDockWidth;
            statusDock.style.height = StatusDockHeight;
            statusDock.style.minHeight = StatusDockHeight;
            statusDock.style.maxHeight = StatusDockHeight;
            // This is one stable label/control dock, not another broad HUD band. Eight units keep the
            // largest action state off its edge while the smaller commitment states retain its frame.
            HudElements.Padding(statusDock, 8f);
            HudElements.Border(statusDock, MatchHudTheme.PanelBorder, 1f, 4f);
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
            hands.HoverPreviewRoot.RemoveFromHierarchy();
            root.Add(hands.HoverPreviewRoot);
            root.Add(round.PreviewRoot);
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
            trayRegion.style.backgroundColor = trayShowsUpgradeComparison
                ? MatchHudTheme.PanelBackground
                : new Color(0f, 0f, 0f, 0f);
            trayRegion.style.borderTopWidth = trayShowsUpgradeComparison ? 1f : 0f;
            trayRegion.style.borderTopColor = trayShowsUpgradeComparison
                ? MatchHudTheme.PanelBorder
                : new Color(0f, 0f, 0f, 0f);
            // Milestone 8.29b: the comparison band is only one card row. Its own bounded rectangle may
            // be a little taller than the ordinary hand, while every standing surface continues to use
            // ordinaryTray. Opening Level Up therefore never moves a plaque or reframes the board.
            trayRegion.style.maxHeight = tray.height;
            var playfield = CalculatePlayfieldRect(width, height, ordinaryTray);
            heroOverviews?.Layout(
                CalculateHeroRailRect(width, height, TeamSide.Red, ordinaryTray.yMin),
                CalculateHeroRailRect(width, height, TeamSide.Blue, ordinaryTray.yMin),
                playfield);
            LayoutDirectorControls();
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

            var usesTray = round.HasExclusiveDecision || minionChoice?.Model != null;
            var hostChanged = usesTray != contextUsesTray;
            if (hostChanged)
            {
                HostContextViewsInTray(usesTray);
            }

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
        /// The Tie Breaker chip's own rectangle inside the status strip. Milestone 8.25c made it a
        /// stated function of the viewport rather than a reader of the current screen, so a check can
        /// ask where it is at a viewport that is not the one the fixture is running at.
        /// </summary>
        public static Rect CalculateTieBreakerIndicatorRect(float panelWidth, float panelHeight)
        {
            var width = Mathf.Min(TieBreakerWidth, Mathf.Max(120f, panelWidth - (Margin * 2f)));
            return new Rect(
                Mathf.Max(Margin, panelWidth - width - Margin),
                Margin,
                width,
                Mathf.Max(0f, Mathf.Min(StatusBarHeight, Mathf.Max(0f, panelHeight)) - (Margin * 2f)));
        }

        private static Rect GetTieBreakerIndicatorRect() =>
            CalculateTieBreakerIndicatorRect(PanelViewport.x, PanelViewport.y);

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

        private void LayoutResolvedHudGeometry()
        {
            if (root == null || trayRegion == null)
            {
                return;
            }

            LayoutDirectorControls();
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
            var upperRight = Mathf.Max(upperLeft, blue.xMin - BoardHexUiGap);
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
            var lowerRight = Mathf.Max(lowerLeft, viewport.x - BoardHexUiGap);
            var lowerBottom = Mathf.Max(lowerTop, viewport.y - BoardHexUiGap);
            var lower = new Rect(
                lowerLeft,
                lowerTop,
                lowerRight - lowerLeft,
                lowerBottom - lowerTop);

            CameraSafeLayout = new BoardCameraSafeLayout(upper, lower, viewport);
            PlayfieldLayoutChanged?.Invoke(CameraSafeLayout);
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
            var statusHeight = Mathf.Min(
                StatusDockHeight,
                Mathf.Max(0f, tray.yMin - statusTop));
            statusDock.style.left = toggle.x;
            statusDock.style.top = statusTop;
            // Pin all six bounds. Width/height alone can still be negotiated by flex layout; matching
            // minimum and maximum values makes the largest ordinary state the invariant footprint.
            // A genuinely smaller viewport is the only reason the declared size is bounded here.
            statusDock.style.width = statusWidth;
            statusDock.style.minWidth = statusWidth;
            statusDock.style.maxWidth = statusWidth;
            statusDock.style.height = statusHeight;
            statusDock.style.minHeight = statusHeight;
            statusDock.style.maxHeight = statusHeight;
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
