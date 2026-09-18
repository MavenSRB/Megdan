using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
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
    ///
    /// Milestone 8.35z60: the Wave counter reads as REMAINING, not the printed starting total.
    /// See <see cref="IMatchReadModel.RemainingWaveCounters"/> for where the number comes from.
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
        /// <param name="remainingWaveCounters">
        /// Milestone 8.35z60, the 2026-09-08 user directive: "Waves up top should show waves
        /// remaining until the end of the game." Rulebook p.8's ONE SHARED Wave-counter pool as it now
        /// stands, rather than <paramref name="setup"/>'s starting total, which never moves. Absent
        /// falls back to the setup's total, the same "no round authority behind it" honesty
        /// <paramref name="teamLifeCounters"/> already gives.
        /// </param>
        public MatchHudHeaderViewModel(
            TieBreakerState tieBreaker,
            QuickGameSetupState setup,
            IReadOnlyList<KeyValuePair<TeamId, int>> teamLifeCounters = null,
            int roundIndex = 0,
            int turnIndex = 0,
            bool isAwaitingViewerInput = false,
            int? remainingWaveCounters = null)
        {
            IsAwaitingViewerInput = isAwaitingViewerInput;
            TieBreaker = tieBreaker;
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            var waveCount = remainingWaveCounters ?? setup.WaveCount;
            Text = Format(tieBreaker, setup, teamLifeCounters, waveCount);
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
            // Milestone 8.35z60. "WAVES 3" read as a fixed printed total and never moved as
            // the pool spent down; the label now says REMAINING explicitly, matching the strip's own
            // compact "LABEL VALUE" convention (RedLifeLabel, BlueLifeLabel) rather than inventing a
            // colon this strip has never used.
            WaveLabel = "WAVES REMAINING " +
                waveCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
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
            IReadOnlyList<KeyValuePair<TeamId, int>> counters,
            int waveCount)
        {
            var redLives = Counters(setup, counters, TeamSide.Red);
            var blueLives = Counters(setup, counters, TeamSide.Blue);
            var tieBreaker = !state.IsDefined
                ? "TIE BREAKER  [?] UNKNOWN"
                : state.RevealedSide == TeamSide.Red
                    ? "TIE BREAKER  [R] RED"
                    : "TIE BREAKER  [B] BLUE";
            return $"QUICK GAME  WAVES REMAINING {waveCount}\nLIVES  [R] {redLives}  [B] {blueLives}\n{tieBreaker}";
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
        public const float UpgradeComparisonBandHeight =
            CardFootprint.Height + RoundActionUnityView.LevelUpHandPreviewHeight + 130f;
        // Keeps UI Toolkit border/pixel snapping from turning two mathematically adjacent rectangles
        // into a live one-pixel overlap at non-identity panel scales.
        private const float BoardTraySeparation = 2f;

        /// <summary>
        /// The retired centre-strip Tie Breaker disc's replacement: a slim banner's height above
        /// the HOLDING team's own rail column. Item 2 of the post-8.35z9 top-strip redesign
        /// (user-ruled 2026-09-05): "Use team trays to show it somehow. Like a line above it, that
        /// says tiebreaker or something." Reserved above BOTH rail columns always, whether or not
        /// that side currently holds it - the standing "reserve the slot, change only its content"
        /// rule this file already applies to the plaque's own selection edge and the current-card
        /// marker - so a handover never grows or shrinks either column.
        ///
        /// <para><b>16, not a rounder number, because the live three-rail fit gate is a hard
        /// budget.</b> <see cref="MinimalHexGridPlayModeTests.Scene_DrawsAllSixOfficialRailsInsideTheirOwnColumnsWithTheTrayBetweenThem"/>
        /// measured the three stacked plaques at 818.6667 reference units - not the 733 an older
        /// note carried forward unverified - against the old column's 848, a slack of only 29.33.
        /// This height plus <see cref="TieBreakerRailBannerGap"/> spends 20 of that, leaving the
        /// column at 828: a real, measured 9.33-unit clearance, not an assumed one.</para>
        /// </summary>
        public const float TieBreakerRailBannerHeight = 16f;

        /// <summary>
        /// The small breathing gap between the TIEBREAKER rail banner and the rail column beneath
        /// it - deliberately smaller than the standing <see cref="Margin"/> because the three-rail
        /// fit gate leaves little room to spend (see <see cref="TieBreakerRailBannerHeight"/>).
        /// </summary>
        private const float TieBreakerRailBannerGap = 4f;

        /// <summary>
        /// The TIEBREAKER rail banner's handover transition, in milliseconds. The TWENTIETH
        /// amendment's motion language ("EASING EVERYWHERE ... 200-450ms") names the band; this
        /// sits in its middle.
        /// </summary>
        private const int TieBreakerRailBannerTransitionMs = 350;

        /// <summary>
        /// The centred narration bar's preferred width, in reference units - wide enough for the
        /// longest sentence this build narrates ("XARGATHA — CHOOSING ATTACK TARGET" and the like)
        /// without spanning the whole strip the way the retired full-width lane did.
        /// </summary>
        private const float NarrationBarPreferredWidth = 900f;

        private const float EventLogPreferredWidth = 720f;
        private const float EventLogPreferredHeight = 360f;

        /// <summary>
        /// Milestone 8.35z7. The match-log drop-down's own HEIGHT, in reference units. It is
        /// deliberately bounded well short of the hero rails' own vertical reach, per the TENTH
        /// amendment's revised placement: a temporary overlay beneath the top strip that "never
        /// blocks an owed input" - which this satisfies structurally, by never covering the rails or
        /// the tray it would otherwise sit above.
        ///
        /// <para>There is no matching <c>MatchLogDropdownWidth</c> constant. Milestone
        /// <c>8.35z55</c>'s 2026-09-08 width
        /// directive ("make it as wide as naration part on the top is") retired the tray's own fixed
        /// width literal: <see cref="CalculateMatchLogDropdownRect"/> now reads its width AND its x
        /// straight off <see cref="CalculateNarrationBarRect"/>'s own rect, the same one-source
        /// pattern the chrome unification (item 1) and the tone pass already used elsewhere in this
        /// file, so the two can never drift apart again.</para>
        /// </summary>
        private const float MatchLogDropdownHeight = 320f;

        /// <summary>
        /// The connection banner's own width, in reference units. Item 3 of the 2026-09-06 log
        /// drop-down refinements demotes this notice - it is narrower than the drop-down it sits
        /// under so that an anomalous connection reads as a note beneath the log rather than as a
        /// second, competing panel across the top of the screen.
        /// </summary>
        private const float ConnectionBannerWidth = 520f;

        /// <summary>The connection banner's own height, in reference units.</summary>
        private const float ConnectionBannerHeight = 40f;

        /// <summary>
        /// The band beneath the match-log drop-down that the connection banner is guaranteed to
        /// find free, in reference units - its own height plus the margin above it.
        ///
        /// <para><b>Why the drop-down's height budget subtracts this rather than the banner's rect
        /// clamping itself into whatever is left.</b> Item 3 asks for a banner "placed clear of the
        /// narration bar and the log drop-down (verify against both open and closed states)". A
        /// clamp would hold at the reference viewport and quietly fail on a short one, which is the
        /// exact shape of the overlap this item exists to remove. Reserving the band inside
        /// <see cref="CalculateMatchLogDropdownRect"/> instead makes the clearance structural at
        /// EVERY viewport - and costs nothing at any supported one, where the drop-down's own
        /// <see cref="MatchLogDropdownHeight"/> is the smaller of the two terms anyway.</para>
        /// </summary>
        private const float ConnectionBannerReservedBand = ConnectionBannerHeight + Margin;

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

        /// <summary>Milestone 8.46c. The decision window, when this composition has one.</summary>
        private DecisionWindowUnityView decisionWindow;

        /// <summary>
        /// Milestone 8.46c. What the window was drawing when it was last laid out, so the four style
        /// writes its rectangle costs happen when the decision or its state changes rather than on
        /// every frame - the same "two-state answer rather than twenty style writes a frame" rule
        /// <see cref="ApplyExclusiveContext"/> already follows for the tray host.
        /// </summary>
        private string laidOutDecisionKey;
        private DecisionWindowState laidOutDecisionState;
        private bool contextUsesTray;

        /// <summary>
        /// Whether the tray is hosting p.15's one-row comparison band. It selects the bounded overlay,
        /// side-rail clearance, and vertical-scroll fallback without changing standing HUD geometry.
        /// </summary>
        private bool trayShowsUpgradeComparison;
        private VisualElement directorDrawer;
        private Button directorToggle;

        // TWELFTH amendment, milestone 8.35z18: the Director-only sandbox banner.
        private VisualElement sandboxBanner;
        private Label sandboxBannerTitle;
        private Label sandboxBannerNote;
        private VisualElement sandboxBannerLines;
        private bool directorToolsVisible = true;
        private bool sandboxStagingComposed;
        private string[] sandboxSummaryLines = Array.Empty<string>();
        private VisualElement tieBreakerRailBanner;
        private Label tieBreakerRailBannerLabel;
        private TeamSide? tieBreakerRailBannerSide;
        private MatchStatusBarUnityView statusBar;
        private MatchLogUnityView matchLog;
        private DirectorHandsUnityView hands;
        private RoundActionUnityView round;
        private UndoRestartUnityView undoRestart;
        private DiagnosticLogUnityView diagnostics;
        private MinionChoiceUnityView minionChoice;
        /// <summary>
        /// Milestone 8.35z62. The root children THIS renderer composed, so a re-composition can
        /// replace its own surfaces without disturbing another owner's on a shared root. See
        /// <see cref="ComposeSurfaces"/> for the fixture that proved the distinction is real.
        /// </summary>
        private readonly List<VisualElement> composedChildren = new List<VisualElement>();

        private HeroOverviewsUnityView heroOverviews;
        private MatchHudHeaderViewModel header;
        private RuntimeTooltipLayer tooltipLayer;

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
        /// <summary>The centred narration bar's own rectangle - the retired full-width lane's replacement.</summary>
        public Rect NarrationBarRect => GetNarrationBarRect();

        /// <summary>Where the TIEBREAKER rail banner stands when Red holds the Tie Breaker.</summary>
        public Rect RedTieBreakerRailBannerRect =>
            CalculateTieBreakerRailBannerRect(PanelViewport.x, PanelViewport.y, TeamSide.Red);

        /// <summary>Where the TIEBREAKER rail banner stands when Blue holds the Tie Breaker.</summary>
        public Rect BlueTieBreakerRailBannerRect =>
            CalculateTieBreakerRailBannerRect(PanelViewport.x, PanelViewport.y, TeamSide.Blue);
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
            HeroOverviewsUnityView heroOverviewsView = null,
            // Milestone 8.35z7. Optional, and appended, so every existing composition and fixture keeps
            // its exact call - the same reason milestone 8.20a's heroOverviews and 8.35y's kinetics
            // were appended rather than inserted.
            MatchLogUnityView matchLogView = null,
            // Milestone 8.46c. Optional, and appended, on the same terms as the two above: a
            // composition that does not pass a window is a composition where the strip keeps drawing
            // the families the window would otherwise take, rather than one where those decisions
            // reach nobody. That is what `YieldedDecisionFamily` below states, and why the strip's own
            // builders are yielded per family rather than deleted.
            DecisionWindowUnityView decisionWindowView = null)
        {
            hands = handsView ?? throw new ArgumentNullException(nameof(handsView));
            hands.SetCompactPeekMode(true);
            round = roundView ?? throw new ArgumentNullException(nameof(roundView));
            undoRestart = undoRestartView ?? throw new ArgumentNullException(nameof(undoRestartView));
            diagnostics = diagnosticView ?? throw new ArgumentNullException(nameof(diagnosticView));
            minionChoice = minionChoiceView ?? throw new ArgumentNullException(nameof(minionChoiceView));
            matchLog = matchLogView;
            decisionWindow = decisionWindowView;
            // Milestone 8.46c. The one signal that moves a family off the strip: the strip yields a
            // decision to the window exactly while the window is DRAWING it. A composition with no
            // window, and a window with no standing model, both read `None` here and the strip draws
            // what it always drew - so no decision can fall between the two surfaces.
            var window = decisionWindow;
            Func<DecisionWindowFamily> yielded = window == null
                ? (Func<DecisionWindowFamily>)null
                : () => window.Model?.Family ?? DecisionWindowFamily.None;
            round.YieldedDecisionFamily = yielded;
            minionChoice.YieldedDecisionFamily = yielded;
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
            // Milestone 8.35z47 (user visual decision, 2026-09-07 item 3: "MATCH THE WARM TONE").
            // The tray that unrolls from the band takes the ground the band just painted. This is
            // the only place the strip renders, so it is the only place the two can be handed the
            // same colour - and the tray is given the PAINTED value rather than the rule, so no
            // second copy of "is the viewer being waited on" exists to fall out of step.
            if (statusBar != null)
            {
                matchLog?.SetBandGround(statusBar.BandGround);
            }

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
            directorToolsVisible = visible;
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

            // The sandbox banner is a Director tool and goes with the rest of them. It should never
            // be reachable from an online seat in the first place - the staging flag is refused
            // beside every networked role - so this is the second of two independent reasons an
            // online seat cannot see one, not the only one.
            ApplySandboxBannerState();
        }

        /// <summary>
        /// TWELFTH amendment, milestone 8.35z18. Shows the Director-only sandbox banner and the
        /// summary of what this match's staging changed. Called once by the composition root when a
        /// staged board was composed, and by nothing else: an ordinary dealt match never calls it
        /// and its banner stays hidden.
        /// </summary>
        /// <remarks>
        /// It takes the already-derived summary lines rather than the staging itself, so this
        /// surface holds no opinion about what a staging means - <see cref="SandboxStagingSummary"/>
        /// owns that, in the engine-free assembly the Unity-free harness covers.
        /// </remarks>
        public void ShowSandboxStaging(IReadOnlyList<string> summaryLines)
        {
            if (summaryLines == null) throw new ArgumentNullException(nameof(summaryLines));

            sandboxStagingComposed = true;
            sandboxSummaryLines = summaryLines.ToArray();
            RenderSandboxBanner();
            Layout();
        }

        /// <summary>
        /// Draws the banner from the retained summary, and is called both by
        /// <see cref="ShowSandboxStaging"/> and at the end of <see cref="Build"/>.
        /// </summary>
        /// <remarks>
        /// Rendering from retained state rather than from the call is what makes this independent of
        /// order. The composition root learns the staging before it configures the HUD, and
        /// <c>Configure</c> rebuilds the whole tree - so a banner drawn only at the moment it was
        /// announced would be discarded by the very next rebuild, which is exactly the shape of a
        /// defect that survives every headless test and shows up as a blank panel in a live run.
        /// </remarks>
        private void RenderSandboxBanner()
        {
            if (sandboxBannerLines == null)
            {
                return;
            }

            sandboxBannerLines.Clear();
            foreach (var line in sandboxSummaryLines)
            {
                var label = HudElements.Text(line, 10f, MatchHudTheme.PrimaryText);
                label.style.marginTop = 1f;
                sandboxBannerLines.Add(label);
            }

            ApplySandboxBannerState();
        }

        /// <summary>
        /// The banner is shown only where BOTH conditions hold: this match was composed from a
        /// staging, and the Director's own tools are visible.
        /// </summary>
        private void ApplySandboxBannerState()
        {
            if (sandboxBanner == null)
            {
                return;
            }

            sandboxBanner.style.display = sandboxStagingComposed && directorToolsVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        /// <summary>Whether the Director-only sandbox banner is currently drawn.</summary>
        public bool IsSandboxBannerVisible =>
            sandboxBanner != null && sandboxBanner.style.display == DisplayStyle.Flex;

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
                // Milestone 8.46c. An EXPANDED decision window is an inset of the board's own
                // rectangle, so without this the click that answered a question would also reach the
                // hex underneath it - the exact duplicate-input defect milestone 8.30b's own note
                // records for End Turn's corner. It is asked only while a window is actually drawn,
                // because the board is genuinely board when it is not.
                (decisionWindow?.HasWindow == true &&
                    TryLiveRect(decisionWindow.Root, out var liveDecisionWindow) &&
                    liveDecisionWindow.Contains(panelPosition)) ||
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
                (diagnostics?.Model?.IsExpanded == true && GetRuleEventLogRect().Contains(panelPosition)) ||
                (matchLog?.Model?.IsOpen == true &&
                    GetMatchLogDropdownRect().Contains(panelPosition)))
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

        /// <summary>
        /// Milestone 8.35z62. THE PANEL IS BUILT ONCE; THE SURFACES ON IT ARE COMPOSED EVERY TIME.
        /// </summary>
        /// <remarks>
        /// <para><b>The defect this split closes, reproduced before it was fixed.</b> This method used
        /// to open with <c>if (root != null) return;</c> - once-only for the whole body. That is
        /// correct for the panel (instantiating <c>PanelSettings</c> again would throw away the live
        /// panel), and WRONG for everything below it, because
        /// <see cref="ConfiguredMatchSceneRuntime"/> constructs a NEW view for every surface it owns
        /// - hands, round, undo/restart, diagnostics, minion choice, hero overviews, match log - and
        /// hands them to <see cref="Configure"/> on every composition. At the one-way lobby
        /// transition the scene recomposes, <see cref="Configure"/> swapped in the new views, called
        /// this, and this RETURNED IMMEDIATELY: the new roots were never added to the tree and the
        /// PREVIOUS composition's views stayed on screen, wired to the previous composition's
        /// presenters.</para>
        ///
        /// <para>That is exactly what the 2026-09-08 playtest reported and what
        /// <c>PrivateLobbyPlayModeTests.TheTransition_DrawsTheRailsOfTheHeroesThatWerePicked</c>
        /// pins: the board rebuilt correctly and spawned the heroes that were picked, while the hero
        /// rails went on drawing the printed demo six and the hand fan stayed on the previous
        /// composition's Director-scoped presenter. The runtime's own read model was RIGHT
        /// throughout - the diagnostic that found this printed the picked line-up from
        /// <c>Runtime().ReadModel</c> in the same breath as <c>viewRootInTree=False</c> - so no
        /// amount of projection or seat-binding correctness could have shown it. Only the rendered
        /// tier could.</para>
        ///
        /// <para><b>Why re-composing is safe.</b> <see cref="ComposeSurfaces"/> begins with
        /// <c>root.Clear()</c> and re-adds every child from the views it currently holds, so it was
        /// already written to be idempotent - it had simply never been reachable a second time. It is
        /// called from exactly one place, <see cref="Configure"/>, which runs once per composition,
        /// so this trades no per-frame work for the fix.</para>
        /// </remarks>
        private void Build()
        {
            EnsurePanelBuilt();
            if (root == null)
            {
                return;
            }

            ComposeSurfaces();
        }

        /// <summary>
        /// The live panel and its settings, which are created once and then kept: re-instantiating
        /// <c>PanelSettings</c> would discard the panel every surface below is drawn on.
        /// </summary>
        private void EnsurePanelBuilt()
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

        }

        /// <summary>
        /// Every surface on the panel, composed from the views this renderer currently holds. Safe to
        /// run again: it clears the root and re-adds each child from the CURRENT views, which is what
        /// makes a re-composition replace the previous composition's surfaces instead of leaving them
        /// on screen. See <see cref="Build"/> for the defect that made this a separate method.
        /// </summary>
        private void ComposeSurfaces()
        {
            // Milestone 8.35z62, SECOND CORRECTION - and the complete Play Mode run is what forced
            // it. Making this method repeatable was right; clearing the WHOLE root to do it was not.
            //
            // `root` is the UIDocument's own root, and this renderer is not always its only owner: a
            // `PrivateLobbyDriver` composed onto THE SAME GameObject adds its own connection banner
            // to that same element, and `OnlineMatchHudPlayModeTests` composes exactly that
            // arrangement. Before this milestone the second `Build()` returned early, so a blanket
            // `root.Clear()` never ran twice and nobody noticed it was too wide a hammer; once the
            // method became repeatable it wiped the lobby's banner and that fixture failed with a
            // null element. A re-composition must replace THIS renderer's surfaces and leave every
            // other owner's alone.
            //
            // Paint order is preserved on purpose rather than incidentally: anything another owner
            // added arrived AFTER this renderer's first build and therefore painted above it, so it
            // is restored last and keeps painting above it.
            var foreign = root.Children().Where(child => !composedChildren.Contains(child)).ToList();
            root.Clear();
            composedChildren.Clear();
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

            // Item 2 of the post-8.35z9 top-strip redesign (user-ruled 2026-09-05): the retired
            // disc's fact moves here, above the HOLDING team's own rail column. One element that
            // eases its own `left` between the two rails on handover, rather than two elements
            // toggling visibility - "moves ... with an eased transition ... no pop" is a
            // TRANSLATION, not a cross-fade. Added after the rails for the same z-order reason the
            // match-log fix above states.
            tieBreakerRailBanner = HudElements.Panel(Color.clear);
            tieBreakerRailBanner.name = "gd-tie-breaker-rail-banner";
            tieBreakerRailBanner.style.position = Position.Absolute;
            tieBreakerRailBanner.style.display = DisplayStyle.None;
            tieBreakerRailBanner.style.alignItems = Align.Center;
            tieBreakerRailBanner.style.justifyContent = Justify.Center;
            tieBreakerRailBanner.pickingMode = PickingMode.Ignore;
            HudElements.Border(tieBreakerRailBanner, Color.clear, 0f, 4f);
            tieBreakerRailBanner.style.transitionProperty =
                new List<StylePropertyName> { new StylePropertyName("left") };
            tieBreakerRailBanner.style.transitionDuration =
                new List<TimeValue> { new TimeValue(TieBreakerRailBannerTransitionMs, TimeUnit.Millisecond) };
            tieBreakerRailBanner.style.transitionTimingFunction =
                new List<EasingFunction> { new EasingFunction(EasingMode.EaseInOutSine) };
            tieBreakerRailBannerLabel = HudElements.Text(
                "TIEBREAKER", 10f, Color.white, FontStyle.Bold);
            tieBreakerRailBannerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            // The requested word stays one line inside the slim 16-unit banner - the same
            // no-wrap statement the retired disc's own label made.
            tieBreakerRailBannerLabel.style.whiteSpace = WhiteSpace.NoWrap;
            tieBreakerRailBanner.Add(tieBreakerRailBannerLabel);
            tieBreakerRailBanner.tooltip = "TIEBREAKER";
            root.Add(tieBreakerRailBanner);

            if (matchLog != null)
            {
                statusBar.HostMatchLogToggle(matchLog.ToggleRoot);
                // Z-order corrective (post-8.35z9). `root.Add` order is UI Toolkit paint order, and the
                // drop-down used to be added ahead of the hero rails - so an open panel whose bounds
                // reached a rail column painted fully BEHIND it, clipping its entries. Added after the
                // rails now, so the log always paints above both columns; its geometry (clear of both
                // rails by construction) is unchanged, this is z-order alone.
                root.Add(matchLog.PanelRoot);
                // Milestone 8.35z7. Click-away: a pointer-down anywhere that is not the drop-down's own
                // toggle or panel asks it closed. Registered in the capture phase on the whole HUD root
                // so it sees every click before whatever it lands on consumes it - `root` itself picks
                // no pointer (see above), but a trickled event still walks every ancestor of its real
                // target on the way down, `root` included, exactly as `pickingMode` leaves untouched.
                root.RegisterCallback<PointerDownEvent>(
                    evt =>
                    {
                        if (!matchLog.Contains(evt.target as VisualElement))
                        {
                            matchLog.ForwardDismiss();
                        }
                    },
                    TrickleDown.TrickleDown);
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

            // TWELFTH amendment, milestone 8.35z18. The Director-only sandbox banner. It is built
            // unconditionally and left hidden, because every other absolutely-positioned element
            // here is too and the layout pass expects to find it; what decides whether it is ever
            // shown is ShowSandboxStaging, which only a staged composition calls.
            //
            // It sits OUTSIDE the drawer deliberately. Inside it, a tester would have to open
            // DIRECTOR to discover which board they are on, and the one thing this surface exists
            // to guarantee is that they never have to ask. It is still Director-only: it is hidden
            // with the rest of the Director tools when the lobby hands this HUD an online seat.
            sandboxBanner = HudElements.Panel(MatchHudTheme.RailBackground);
            sandboxBanner.name = "gd-sandbox-banner";
            sandboxBanner.style.position = Position.Absolute;
            sandboxBanner.style.flexDirection = FlexDirection.Column;
            sandboxBanner.style.display = DisplayStyle.None;
            sandboxBanner.pickingMode = PickingMode.Ignore;
            HudElements.Padding(sandboxBanner, 6f);
            HudElements.Border(sandboxBanner, MatchHudTheme.ConfirmAccent, 1f, 4f);
            sandboxBannerTitle = HudElements.Text(
                SandboxStagingSummary.Title, 13f, MatchHudTheme.ConfirmAccent, FontStyle.Bold);
            sandboxBanner.Add(sandboxBannerTitle);
            sandboxBannerNote = HudElements.Text(
                SandboxStagingSummary.DirectorOnlyNote, 10f, MatchHudTheme.MutedText);
            sandboxBannerNote.style.marginTop = 2f;
            sandboxBanner.Add(sandboxBannerNote);
            sandboxBannerLines = new VisualElement();
            sandboxBannerLines.style.flexDirection = FlexDirection.Column;
            sandboxBannerLines.style.marginTop = 4f;
            sandboxBanner.Add(sandboxBannerLines);
            root.Add(sandboxBanner);
            // Redraw from retained state, so a rebuild after the staging was announced keeps it.
            RenderSandboxBanner();

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
            // Milestone 8.46c. The decision window is placed on the panel like every other standing
            // surface here, and added after the strip's own re-parented hosts so an expanded window
            // paints above the board it insets into. Its two rectangles - an inset of the playfield,
            // and the round dock's own bar - are clear of the rails, the status strip, End Turn, the
            // seat undo square and the tray BY CONSTRUCTION; see the two Calculate methods below.
            if (decisionWindow != null)
            {
                decisionWindow.Root.RemoveFromHierarchy();
                decisionWindow.Root.style.position = Position.Absolute;
                root.Add(decisionWindow.Root);
            }

            if (heroOverviews != null)
            {
                // Only the revealed face leaves the rail layer. Keeping the rail columns where they
                // are while putting this one pointer-transparent card last in paint order prevents
                // Director and the status dock from cutting across the face being read.
                heroOverviews.HoverPreviewRoot.RemoveFromHierarchy();
                root.Add(heroOverviews.HoverPreviewRoot);
            }

            // Milestone 8.35z19 (placeholder id). Added LAST, after every other surface,
            // so its popup always paints above whatever it is explaining - a drop-down, a hover
            // preview, a drag proxy included. One layer serves every `.tooltip` string this whole HUD
            // sets, because UI Toolkit's own tooltip renders only in the Editor.
            tooltipLayer = new RuntimeTooltipLayer();
            root.Add(tooltipLayer.Root);
            tooltipLayer.Install(root);

            // Everything on the root now is this renderer's, so remember it: the next composition
            // removes exactly this set and nothing else. Then every other owner's elements go back
            // on top, in the order they held.
            composedChildren.AddRange(root.Children());
            foreach (var element in foreign)
            {
                root.Add(element);
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
            var narrationBar = CalculateNarrationBarRect(width, height);
            statusBar?.Layout(bar, narrationBar);
            matchLog?.Layout(CalculateMatchLogDropdownRect(width, height));
            LayoutTieBreakerRailBanner(width, height);
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
            LayoutDecisionWindow(force: true);
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
            // Milestone 8.46c. The window's own rectangle follows the decision it is drawing, which
            // can change without the header changing - a pause is rendered into it by its own
            // presenter - so it is placed here, on the same per-frame pass and by the same
            // changed-state rule the tray host above uses.
            LayoutDecisionWindow(force: false);
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
            // Milestone 8.35z59, second finding. The board is RIGHT-anchored and its width is
            // derived from its height, so its LEFT edge is whatever is left over - the horizontal twin
            // of the rail column's vertical elasticity, and unbounded in exactly the same way. Bounding
            // it against the Red rail alone was not enough: the round dock stands between that rail and
            // the board, and at a panel narrower than about 2064 units (or taller than the reference,
            // which buys the board height and therefore width) the board's left edge crossed the dock
            // and the two action rows beside the hand. This is the same "bound it against the neighbour
            // it would collide with" rule milestone 8.35z2 applied to the optional-step and agreement
            // panels. At the reference the HEIGHT is the binding constraint (870 against the 1273 this
            // width would allow), so the board's rectangle is unmoved - see the aspect-ratio gate's
            // 16:9 baseline case.
            var leftBound = Mathf.Max(
                red.xMax + Margin,
                CalculateStatusDockRightEdge(panelWidth) + Margin);
            var availableWidth = Mathf.Max(0f, right - leftBound);
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
            var left = CalculateStatusDockLeftEdge(panelWidth);
            var right = CalculateStatusDockRightEdge(panelWidth);
            var height = Mathf.Min(StatusDockHeight, playfield.height);
            return new Rect(
                left,
                Mathf.Max(playfield.yMin, playfield.yMax - height),
                Mathf.Max(0f, right - left),
                height);
        }

        /// <summary>
        /// Milestone 8.46c. The EXPANDED decision window: <see cref="CalculatePlayfieldRect"/> inset by
        /// the standing 12-unit margin, and nothing else.
        ///
        /// <para><b>It covers the board and only the board, by construction rather than by
        /// measurement.</b> The playfield is already bounded against the status strip above it, the
        /// tray below it, the Blue rail to its right and - since milestone 8.35z59's second finding -
        /// the Red rail and the round dock to its left, so an inset of it cannot reach any of them.
        /// At the 2560x1440 reference the playfield is (1228, 96, 1044, 870) and this window is
        /// therefore (1240, 108, 1020, 846): 14 units clear of the tray's top edge at 968, 24 clear of
        /// the Blue rail's left edge at 2284, 964 clear of the Red rail's right edge at 276, and 12
        /// below the status strip's own 96-unit band - which is where End Turn and the seat undo
        /// square live, so neither is ever covered.</para>
        ///
        /// <para>The inset yields on a panel too small to carry it, so the window is never inverted -
        /// which cannot happen at any viewport this HUD lays out into (the panel is never smaller than
        /// the reference in either dimension), and is stated anyway because every other rectangle
        /// here states it.</para>
        /// </summary>
        public static Rect CalculateDecisionWindowExpandedRect(float panelWidth, float panelHeight)
        {
            var playfield = CalculatePlayfieldRect(panelWidth, panelHeight);
            var inset = Mathf.Min(Margin, Mathf.Min(playfield.width, playfield.height) * 0.25f);
            return new Rect(
                playfield.xMin + inset,
                playfield.yMin + inset,
                Mathf.Max(0f, playfield.width - (inset * 2f)),
                Mathf.Max(0f, playfield.height - (inset * 2f)));
        }

        /// <summary>
        /// Milestone 8.46c. The DOCKED decision window: a slim bar along the TOP of
        /// <see cref="CalculateStatusDockRect"/>, yielding its left edge to the Red rail exactly as
        /// <see cref="CalculateOptionalStepControlRect"/> and
        /// <see cref="CalculateTeamAgreementControlRect"/> already do.
        ///
        /// <para><b>Why it shares the dock rather than replacing it.</b> The declared dock rectangle
        /// starts at the Red rail's own left edge - "the dock and the rail share an outside margin",
        /// per <see cref="CalculateStatusDockLeftEdge"/> - so a bar taking the whole of it would lie
        /// across the rail column at every viewport, which is the structurally identical defect
        /// milestone 8.35z2 and 8.35z6 each had to correct for the two panels above. The bar therefore
        /// begins one margin past the rail. It takes the dock's TOP band because the dock's own live
        /// contents (the round view, the minion prompt and the hand header) are laid out against
        /// `DIRECTOR`'s bottom edge, which is the dock's LOWER band - so the two alternate by band
        /// rather than by visibility, and the `DIRECTOR` toggle's own rectangle stays clear.</para>
        ///
        /// <para>At the reference this is (288, 838, 444, 96): the tray begins at y 968, the board's
        /// left edge is at x 1228 - so a board-answered decision's bar never covers the pieces its own
        /// question is about - and the `DIRECTOR` toggle occupies x 12 to 134, which this bar clears
        /// HORIZONTALLY at every viewport because it begins one margin past the Red rail and the
        /// toggle begins at that rail's own left edge. That is why the toggle's vertical position,
        /// which follows the live hero column and cannot be resolved statically, does not matter
        /// here.</para>
        /// </summary>
        public static Rect CalculateDecisionWindowDockedRect(float panelWidth, float panelHeight)
        {
            var dock = CalculateStatusDockRect(panelWidth, panelHeight);
            var red = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Red);
            var blue = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Blue);
            var left = Mathf.Max(dock.xMin, red.xMax + Margin);
            var right = Mathf.Max(left, Mathf.Min(dock.xMax, blue.xMin - Margin));
            return new Rect(
                left,
                dock.yMin,
                right - left,
                Mathf.Min(DecisionWindowDockedHeight, dock.height));
        }

        /// <summary>
        /// Milestone 8.46c. The docked bar's own height, inside the 128-unit dock: its two rows - the
        /// question's title with its running count, and the question with its one verb - at
        /// <see cref="DecisionWindowUnityView.DockedPadding"/> either side.
        ///
        /// <para>96 rather than the whole 128, so the bar takes the dock's TOP band and the dock's own
        /// live contents keep the lower one. At the reference the bar therefore ends at y 934, clear
        /// of the tray at 968 and of the `DIRECTOR` toggle's lane below the hero column.</para>
        /// </summary>
        public const float DecisionWindowDockedHeight = 96f;

        /// <summary>
        /// The round dock's own left edge. It is the Red rail's left edge - the dock and the rail
        /// share an outside margin - and depends on the panel WIDTH alone, which is what lets
        /// <see cref="CalculatePlayfieldRect(float,float,Rect)"/> bound the board against this dock
        /// without either one having to resolve the other first.
        /// </summary>
        private static float CalculateStatusDockLeftEdge(float panelWidth) =>
            Mathf.Min(Mathf.Max(0f, panelWidth), Margin);

        /// <summary>
        /// The round dock's own right edge, and therefore the leftmost line the board may reach.
        /// </summary>
        private static float CalculateStatusDockRightEdge(float panelWidth)
        {
            var left = CalculateStatusDockLeftEdge(panelWidth);
            return Mathf.Min(
                Mathf.Max(left, panelWidth - Margin),
                left + StatusDockWidth);
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
            // The rail column's top reserves the TIEBREAKER banner's own slot ABOVE it (item 2 of
            // the post-8.35z9 top-strip redesign): the banner sits where the column used to start
            // (the standing gap between the strip and the column is now the gap above the banner),
            // then a small gap of its own before the column. Reserved for BOTH sides always, so a
            // Tie Breaker handover moves only which side reads the banner, never either column's
            // own height. Measured against the live three-rail fit gate, not assumed - see
            // TieBreakerRailBannerHeight's own doc comment for the exact budget.
            var top = StatusBarHeight + Margin + TieBreakerRailBannerHeight + TieBreakerRailBannerGap;
            return new Rect(x, top, width, Mathf.Max(0f, bottomBoundary - top - Margin));
        }

        private static float CalculateHeroRailWidth(float panelWidth) =>
            // One plaque gets at most a quarter of the panel minus its outside margin. Two plaques
            // therefore remain strictly below half even when the preferred 264 cannot fit.
            Mathf.Min(HeroRailWidth, Mathf.Max(0f, (panelWidth / 4f) - Margin));

        private static Rect GetCardPanelRect() =>
            CalculateCardPanelRect(PanelViewport.x, PanelViewport.y);

        /// <summary>
        /// The centred narration bar's own rectangle, in the strip's own coordinate space (so it is
        /// used both as the bar's layout rect and, translated by the strip's own origin, as the
        /// match-log drop-down's anchor). Item 2 of the post-8.35z9 top-strip redesign (user-ruled
        /// 2026-09-05): the narration - "the game's most important line" - moves from the retired
        /// full-width lane to a centred bar, keeping its gold treatment, typography and log
        /// chevron. Bounded against <see cref="CalculateHeroRailWidth"/> so it is clear of both
        /// rail columns BY CONSTRUCTION at any viewport, not only the reference one.
        /// </summary>
        public static Rect CalculateNarrationBarRect(float panelWidth, float panelHeight)
        {
            var railWidth = CalculateHeroRailWidth(panelWidth);
            var maxWidth = Mathf.Max(0f, panelWidth - ((railWidth + (Margin * 2f)) * 2f));
            var width = Mathf.Min(NarrationBarPreferredWidth, maxWidth);
            var height = Mathf.Min(
                StatusBarNarrationHeight,
                Mathf.Max(0f, panelHeight - StatusBarIdentityRowHeight));
            return new Rect(
                Mathf.Max(0f, (panelWidth - width) * 0.5f),
                StatusBarIdentityRowHeight,
                width,
                height);
        }

        private static Rect GetNarrationBarRect() =>
            CalculateNarrationBarRect(PanelViewport.x, PanelViewport.y);

        /// <summary>
        /// The slim TIEBREAKER banner's own rectangle above the named side's rail column - the
        /// retired centre-strip disc's replacement. Always that side's own
        /// <see cref="CalculateHeroRailRect(float,float,TeamSide)"/> x/width, so the live element's
        /// eased transition on handover is a pure horizontal move between two named targets rather
        /// than a re-derivation of anything about the column itself.
        /// </summary>
        public static Rect CalculateTieBreakerRailBannerRect(
            float panelWidth, float panelHeight, TeamSide holdingSide)
        {
            var width = CalculateHeroRailWidth(panelWidth);
            var x = holdingSide == TeamSide.Red
                ? Margin
                : Mathf.Max(Margin, panelWidth - width - Margin);
            return new Rect(x, StatusBarHeight + Margin, width, TieBreakerRailBannerHeight);
        }

        /// <summary>
        /// End Turn's own rectangle in the status bar's top-right slot, stated as a function of the
        /// viewport for the reason <see cref="CalculateNarrationBarRect"/> is: a fixture with no
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
            LayoutDecisionWindow(force: true);
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

        /// <summary>
        /// Milestone 8.46c. Places the decision window in whichever of its two rectangles the standing
        /// decision asked for.
        ///
        /// <para>It is re-laid only when the DECISION or its STATE changed, or when the caller states
        /// that the viewport itself may have - the two-state answer <see cref="ApplyExclusiveContext"/>
        /// already prefers to twenty style writes a frame.</para>
        /// </summary>
        private void LayoutDecisionWindow(bool force)
        {
            if (decisionWindow == null)
            {
                return;
            }

            var model = decisionWindow.Model;
            var key = model?.DecisionKey;
            var state = model?.State ?? DecisionWindowState.Docked;
            if (!force && string.Equals(key, laidOutDecisionKey, StringComparison.Ordinal) &&
                state == laidOutDecisionState)
            {
                return;
            }

            laidOutDecisionKey = key;
            laidOutDecisionState = state;
            var viewport = PanelViewport;
            var rect = state == DecisionWindowState.Docked
                ? CalculateDecisionWindowDockedRect(viewport.x, viewport.y)
                : CalculateDecisionWindowExpandedRect(viewport.x, viewport.y);
            var host = decisionWindow.Root;
            host.style.left = rect.x;
            host.style.top = rect.y;
            host.style.width = rect.width;
            host.style.height = rect.height;
        }

        /// <summary>
        /// Milestone 8.35z6 correction, following 8.35z2's own template exactly
        /// (<see cref="CalculateTeamAgreementControlRect"/>'s doc comment). This rect used to anchor its
        /// left edge at <see cref="CalculateCardPanelRect"/>'s <c>xMin</c> - the screen's own left edge,
        /// because that full-width tray band reserves the whole viewport - rather than the hand's
        /// visible left edge, which put the ACCEPT/DECLINE optional-step banner directly on top of the
        /// Red rail's own column at every viewport, reference included. 8.35z2 recorded this as the
        /// structurally identical defect it fixed for <see cref="CalculateTeamAgreementControlRect"/> and
        /// left unfixed here as out of its own stated territory (follow-up task d3b5c3d2). Fixed the same
        /// way: left yields to the Red rail's measured right edge, and right is bounded against the Blue
        /// rail and the unavailable-card fan too, so the banner cannot land on either rail, the hand
        /// tray, or the fan at a reference, narrow, or wide viewport.
        /// </summary>
        public static Rect CalculateOptionalStepControlRect(float panelWidth, float panelHeight)
        {
            var tray = CalculateCardPanelRect(panelWidth, panelHeight);
            var redRail = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Red);
            var blueRail = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Blue);
            var unavailable = CalculateUnavailableCardGroupRect(panelWidth, panelHeight);
            var left = Mathf.Max(tray.xMin, redRail.xMax) + OptionalStepEdgeClearance;
            var right = Mathf.Min(
                Mathf.Min(CompactHandRightEdge, blueRail.xMin),
                Mathf.Min(unavailable.xMin, panelWidth - Margin)) - OptionalStepEdgeClearance;
            right = Mathf.Max(left, right);
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

        /// <summary>
        /// Milestone 8.35z2 correction. The panel used to start at <c>tray.xMin</c>, which is the
        /// screen's own left edge rather than the hand's visible left edge - the full-width tray band
        /// (<see cref="CalculateCardPanelRect"/>) reserves the whole viewport, so anchoring here at the
        /// bare edge put the box directly on top of the Red rail's own column (its third plaque,
        /// `DIRECTOR` and `CONFIRM FACEDOWN`) at every viewport, not only a narrow one. Left now yields
        /// to the Red rail's measured right edge; right is bounded against the Blue rail and the
        /// unavailable-card fan too, the same "bound it against the neighbour it would collide with"
        /// rule <see cref="GetEndTurnControlRect"/> already applies, so the panel cannot land on either
        /// rail, the hand tray, or the fan at a reference, narrow, or wide viewport.
        /// </summary>
        public static Rect CalculateTeamAgreementControlRect(float panelWidth, float panelHeight)
        {
            var tray = CalculateCardPanelRect(panelWidth, panelHeight);
            var redRail = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Red);
            var blueRail = CalculateHeroRailRect(panelWidth, panelHeight, TeamSide.Blue);
            var unavailable = CalculateUnavailableCardGroupRect(panelWidth, panelHeight);
            var left = Mathf.Max(tray.xMin, redRail.xMax) + OptionalStepEdgeClearance;
            var right = Mathf.Min(
                Mathf.Min(CompactHandRightEdge, blueRail.xMin),
                Mathf.Min(unavailable.xMin, panelWidth - Margin)) - OptionalStepEdgeClearance;
            right = Mathf.Max(left, right);
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

            // TWELFTH amendment. The banner sits in the Director's own column, pinned by its BOTTOM
            // edge so it grows UPWARD as staged lines are added rather than needing a height this
            // layout would have to predict. Its width follows the drawer's, so the Director column
            // stays one column rather than becoming two.
            //
            // It clears the DRAWER rather than the toggle, and unconditionally rather than only
            // while the drawer is open. The drawer opens upward from the toggle (see
            // CalculateDirectorDrawerRect), so clearing only the toggle would put the banner
            // underneath it; and reserving the space whether or not it is open keeps the banner
            // still when a tester opens DIRECTOR, instead of making it jump.
            if (sandboxBanner != null)
            {
                var clearedTop = Mathf.Min(toggle.y, drawer.y);
                sandboxBanner.style.left = toggle.x;
                sandboxBanner.style.width = drawer.width;
                sandboxBanner.style.bottom =
                    Mathf.Max(0f, viewport.y - clearedTop) + DirectorRailGap;
            }
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

        /// <summary>
        /// Milestone 8.35z7, re-anchored by item 2 of the post-8.35z9 top-strip redesign: where the
        /// match-log drop-down unrolls, beneath the narration bar it hangs from - the TENTH
        /// amendment's "opens up as a drop down of a naration bar" read literally now that the bar
        /// itself is centred rather than full-width.
        ///
        /// <para><b>Milestone <c>8.35z55</c>, the 2026-09-08 width directive, user, with screenshot
        /// ("make it as wide as
        /// naration part on the top is"): the tray's WIDTH AND X BOTH READ DIRECTLY OFF
        /// <see cref="CalculateNarrationBarRect"/>'s own rect</b> - not a second literal sized to
        /// agree with the bar today, which is exactly how the two disagreed before (900 wide at
        /// x=830 above a fixed 640 wide at x=960). One source means the pair cannot drift apart
        /// again, at any viewport the bar itself is defined at - including the hero-rail clearance
        /// <see cref="CalculateNarrationBarRect"/> already guarantees BY CONSTRUCTION, which the tray
        /// now inherits for free rather than re-deriving its own margin-based clamp.</para>
        ///
        /// <para><b>Item 1 of the 2026-09-06 log drop-down refinements ("make the dropdown match the
        /// top, not to be different like this") took the 4-unit gap out.</b> The tray still hangs
        /// FLUSH from <c>narrationBar.yMax</c>, which is exactly the strip's own bottom edge, so the
        /// panel reads as the band unrolling rather than as a separate box floating below it - and
        /// the squared top corners <see cref="MatchLogUnityView"/> now draws only read as a seam
        /// because there is no gap for them to hang over.</para>
        ///
        /// <para>Stated as a function of the viewport, and public, for the reason
        /// <see cref="CalculateNarrationBarRect"/> is: a fixture with no live panel has to be able
        /// to ask where this lands at a viewport it is not running at - which is what lets the
        /// connection banner's clearance be PROVED offline rather than eyeballed in a screenshot.</para>
        /// </summary>
        public static Rect CalculateMatchLogDropdownRect(float panelWidth, float panelHeight)
        {
            var narrationBar = CalculateNarrationBarRect(panelWidth, panelHeight);
            var height = Mathf.Min(
                MatchLogDropdownHeight,
                Mathf.Max(
                    0f,
                    panelHeight - narrationBar.yMax - (Margin * 2f) - ConnectionBannerReservedBand));
            return new Rect(narrationBar.xMin, narrationBar.yMax, narrationBar.width, height);
        }

        /// <summary>
        /// Item 3 of the 2026-09-06 log drop-down refinements: where an ANOMALOUS connection state
        /// says so. Centred beneath the match-log drop-down's own full open extent, so it is clear
        /// of both the narration bar and the drop-down whether that drop-down is open or closed -
        /// the "verify against both open and closed states" the item asks for, answered by
        /// construction rather than by a state-dependent position that could be wrong in one of them.
        ///
        /// <para>A HEALTHY connection draws nothing at all; see
        /// <see cref="MatchConnectionBanner.EarnsScreen"/>. This rectangle only says where the
        /// anomalous cases go.</para>
        /// </summary>
        public static Rect CalculateConnectionBannerRect(float panelWidth, float panelHeight)
        {
            var dropdown = CalculateMatchLogDropdownRect(panelWidth, panelHeight);
            var width = Mathf.Min(ConnectionBannerWidth, Mathf.Max(0f, panelWidth - (Margin * 2f)));
            var height = Mathf.Min(ConnectionBannerHeight, Mathf.Max(0f, panelHeight));
            return new Rect(
                Mathf.Max(0f, (panelWidth - width) * 0.5f),
                dropdown.yMax + Margin,
                width,
                height);
        }

        private static Rect GetMatchLogDropdownRect() =>
            CalculateMatchLogDropdownRect(PanelViewport.x, PanelViewport.y);

        /// <summary>
        /// Positions the TIEBREAKER rail banner above the HOLDING team's own rail column, or hides
        /// it while no side has been decided yet (the opening moments of a match). The element's
        /// own USS transition on `left` is what turns a handover into the eased slide the SECOND
        /// item of the post-8.35z9 redesign asks for; this method only ever states the TARGET.
        /// </summary>
        private void LayoutTieBreakerRailBanner(float panelWidth, float panelHeight)
        {
            if (tieBreakerRailBanner == null)
            {
                return;
            }

            if (header == null || !header.TieBreaker.IsDefined)
            {
                tieBreakerRailBannerSide = null;
                tieBreakerRailBanner.style.display = DisplayStyle.None;
                return;
            }

            var side = header.TieBreaker.RevealedSide;
            var target = CalculateTieBreakerRailBannerRect(panelWidth, panelHeight, side);
            tieBreakerRailBanner.style.display = DisplayStyle.Flex;
            tieBreakerRailBanner.style.top = target.y;
            tieBreakerRailBanner.style.width = target.width;
            tieBreakerRailBanner.style.height = target.height;
            if (tieBreakerRailBannerSide != side)
            {
                tieBreakerRailBannerSide = side;
                tieBreakerRailBanner.style.backgroundColor = header.Color;
                if (tieBreakerRailBannerLabel != null)
                {
                    tieBreakerRailBannerLabel.style.color = Color.white;
                }
            }

            tieBreakerRailBanner.style.left = target.x;
            tieBreakerRailBanner.tooltip = header.Text;
        }
    }
}
