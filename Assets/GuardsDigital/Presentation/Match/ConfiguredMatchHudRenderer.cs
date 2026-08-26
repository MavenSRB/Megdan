using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
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
        private const float CardPanelWidth = 1280f;
        private const float CardPanelHeight = 360f;
        private const float TieBreakerWidth = 200f;
        private const float EventLogPreferredWidth = 720f;
        private const float EventLogPreferredHeight = 360f;

        /// <summary>
        /// The standing status strip's height, in reference units. The Tie Breaker chip sits inside it.
        /// </summary>
        public const float StatusBarHeight = 56f;

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
        /// <see cref="CalculateHeroRailRect"/> gives the column the whole screen edge instead of the
        /// band above the tray. Three rails of the height 8.23a achieved need about 733 and the column
        /// has about 1000 at 1920x1080, so the recomposition and the taller column together are what
        /// hold p.6's three a side.</para>
        ///
        /// <para>192 rather than more, because the widening is bounded by a standing product rule
        /// rather than by taste: two rails together must stay under half the narrowest validated
        /// viewport so the board remains the dominant surface, which
        /// <c>MatchHudPresentationTests.TheStandingLayout_KeepsEveryRegionInsideBothValidatedViewports</c>
        /// measures. 208 broke it at 416 against a 389 limit and was reduced rather than the rule
        /// being loosened.</para>
        ///
        /// <para><b>Milestone 8.25c restates it rather than re-measuring it.</b> 192 is now 192
        /// <em>reference units</em> - the same panel points 8.23a measured, since a rail's rows are
        /// laid out in points and not in pixels - so the number keeps exactly the meaning that
        /// milestone gave it, and the product rule above becomes a fact about every screen rather than
        /// about whichever one was narrowest. What changes is that a rail is 192 pixels on a
        /// 2560-wide screen and 144 on a 1920-wide one, which is the same share of both.</para>
        /// </summary>
        public const float HeroRailWidth = 192f;

        private const float DirectorToggleWidth = 122f;
        private const float DirectorToggleHeight = 30f;
        private const float DirectorDrawerWidth = 134f;
        private const float DirectorDrawerHeight = 112f;

        private UIDocument document;
        private PanelSettings panelSettings;
        private VisualElement root;
        private VisualElement trayRegion;

        /// <summary>
        /// Whether the last layout gave the tray region the taller bound p.15's upgrade grid needs.
        /// Milestone 8.8a: it is what makes the extra layout happen once per transition instead of once
        /// per frame.
        /// </summary>
        private bool trayUsesTallContext;
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
            round = roundView ?? throw new ArgumentNullException(nameof(roundView));
            undoRestart = undoRestartView ?? throw new ArgumentNullException(nameof(undoRestartView));
            diagnostics = diagnosticView ?? throw new ArgumentNullException(nameof(diagnosticView));
            minionChoice = minionChoiceView ?? throw new ArgumentNullException(nameof(minionChoiceView));
            heroOverviews = heroOverviewsView;
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
            if (tray.Contains(panelPosition) ||
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
        /// strip, the Tie Breaker chip, the card tray and both hero-rail columns - so that the mapping
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
                CalculateCardPanelRect(panel.x, panel.y).Contains(panelPosition) ||
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

            trayRegion = HudElements.Panel(MatchHudTheme.PanelBackground);
            trayRegion.name = "gd-tray-region";
            trayRegion.style.position = Position.Absolute;
            trayRegion.style.flexDirection = FlexDirection.Column;
            HudElements.Padding(trayRegion, 12f);
            HudElements.Border(trayRegion, MatchHudTheme.PanelBorder, 1f, 4f);
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            scroll.Add(round.Root);
            scroll.Add(minionChoice.Root);
            scroll.Add(hands.Root);
            trayRegion.Add(scroll);
            root.Add(trayRegion);
            root.Add(round.PreviewRoot);

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
            // The tray is anchored to the bottom edge and grows upward only as far as its content
            // needs, bounded by the region it may occupy. A fixed height would reserve the whole
            // 360 pixels for a commitment row that uses two hundred of them, and every pixel it did
            // not use would be board it covered for nothing.
            // The tray is anchored to the bottom-left corner of the region it may occupy and sized to
            // its own content in both directions, bounded by that region. A fixed 1280-by-360 box was
            // reserving the whole of it for a commitment row that needs a third of the width and half
            // the height, and every pixel it did not use was board it covered for nothing. The bound is
            // what keeps milestone 5.1c's guarantee: four content-sized Defense choices plus Decline
            // still have 1280 pixels to lay out in before anything has to scroll.
            var tray = GetCardPanelRect();
            trayRegion.style.left = tray.x;
            trayRegion.style.top = StyleKeyword.Auto;
            trayRegion.style.right = StyleKeyword.Auto;
            trayRegion.style.width = StyleKeyword.Auto;
            trayRegion.style.height = StyleKeyword.Auto;
            trayRegion.style.bottom = Mathf.Max(0f, height - tray.yMax);
            trayRegion.style.maxWidth = tray.width;
            // Milestone 8.8a: p.15's upgrade prompt draws whole card faces in two rows, which is taller
            // than the share a tray of one row may take. It is allowed the whole space under the status
            // strip while it stands, and nothing competes for it: the ordinary tray is hidden behind a
            // mandatory decision, and the board's own framing is computed from the ordinary bound below,
            // so the map does not move when the prompt opens and closes. The region still sizes to its
            // own content, so a prompt that fits the ordinary bound occupies no more than it did.
            trayUsesTallContext = round?.HasUpgradeCardGrid == true;
            trayRegion.style.maxHeight = trayUsesTallContext
                ? Mathf.Max(tray.height, CalculateTallContextHeight(height))
                : tray.height;
            var playfield = CalculatePlayfieldRect(width, height);
            heroOverviews?.Layout(
                CalculateHeroRailRect(width, height, TeamSide.Red),
                CalculateHeroRailRect(width, height, TeamSide.Blue),
                playfield);
            var toggle = GetDirectorToggleRect();
            directorToggle.style.left = toggle.x;
            directorToggle.style.top = toggle.y;
            directorToggle.style.width = toggle.width;
            var drawer = GetDirectorDrawerRect();
            directorDrawer.style.left = drawer.x;
            directorDrawer.style.top = drawer.y;
            directorDrawer.style.width = drawer.width;
            diagnostics?.Layout(GetRuleEventLogRect());
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

            hands.SetHidden(round.HasExclusiveDecision || minionChoice?.Model != null);

            // Milestone 8.8a. The tray's own bound depends on what the contextual region is drawing, and
            // that can change without the header changing - a paused prompt is rendered into the region
            // by its own presenter. Re-laying out only on the transition keeps this a two-state answer
            // rather than twenty style writes a frame.
            if (round.HasUpgradeCardGrid != trayUsesTallContext)
            {
                Layout();
            }
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
            if (panelSettings != null)
            {
                Destroy(panelSettings);
                panelSettings = null;
            }
        }

        public static Rect CalculateStatusBarRect(float panelWidth, float panelHeight) =>
            new Rect(0f, 0f, Mathf.Max(0f, panelWidth), Mathf.Min(StatusBarHeight, Mathf.Max(0f, panelHeight)));

        /// <summary>
        /// The share of a tall viewport the tray may use when its content needs it.
        ///
        /// <see cref="CardPanelHeight"/> alone is milestone 5.1c's number, chosen so a Defense row of
        /// four cards plus Decline fits. p.15's Upgrade a Card is a different shape - up to two options
        /// per upgradable card - and six of them ran past 360 on a 1440-pixel screen, putting a whole
        /// row of a mandatory choice behind a scrollbar with most of the screen empty above it. So 360
        /// is the floor rather than the ceiling, and the ceiling follows the viewport. The tray still
        /// sizes to its own content, so nothing here makes a small tray larger.
        ///
        /// Milestone 8.5a raised the share from a third to 0.46. A tray card now carries its face's
        /// whole printed text, and the longest of Wasp's starting five - Static Barrier's two sentences
        /// about radius - needs six wrapped lines that a third of a 1080-line viewport could not hold
        /// without putting a row of five cards behind a scrollbar. The board keeps well over half the
        /// screen at every validated viewport, and the small-viewport case is unaffected: below roughly
        /// 860 lines the floor and the space under the status strip still decide the height.
        /// </summary>
        private const float CardPanelTallViewportShare = 0.46f;

        /// <summary>
        /// The tray's band: everything between the two hero-rail columns.
        ///
        /// <para>Milestone 8.25. The tray used to start at the left margin and run under both rails,
        /// because a rail stopped where the tray began and the corner was free. Rulebook p.5's six
        /// seats need three rails a side, and three do not stack in the band above the tray - the
        /// measurement was 733 needed against 491 available at 1920x1080 - so the rails take the whole
        /// column from the status strip to the bottom margin and the tray takes what is between them.
        /// That is where the vertical room came from: the rails stopped competing with the tray for the
        /// same corner.</para>
        ///
        /// <para><b>It starts at the window's own left margin.</b> The user's 2026-08-26 decision, taken
        /// twice on seeing the six-player HUD: first that the tray should leave the centre of the band,
        /// then that "all the way left" means the window edge rather than the edge of the band. The rail
        /// column keeps the whole screen edge - that is where rulebook p.6's three rails a side came
        /// from - so the two meet in the bottom-left corner, and the tray is drawn over the rail there.
        /// At the reference resolution that overlap is the third rail's bottom padding and nothing a
        /// player reads; <see cref="CalculateHeroRailRect"/> states the measurement and
        /// <c>MinimalHexGridPlayModeTests</c> asserts that every row of every rail stays clear of the
        /// tray.</para>
        /// </summary>
        public static Rect CalculateCardPanelRect(float panelWidth, float panelHeight)
        {
            var left = Margin;
            var width = Mathf.Min(CardPanelWidth, Mathf.Max(0f, panelWidth - (Margin * 2f)));
            var height = Mathf.Min(
                Mathf.Max(CardPanelHeight, panelHeight * CardPanelTallViewportShare),
                Mathf.Max(0f, panelHeight - (Margin * 2f) - StatusBarHeight));
            return new Rect(
                left,
                Mathf.Max(Margin, panelHeight - height - Margin),
                width,
                height);
        }

        /// <summary>
        /// The tallest the tray region may be when the contextual region is drawing whole card faces in
        /// more than one row: everything under the status strip, less the band the `DIRECTOR` toggle
        /// moves into, inside the same margin every region keeps.
        ///
        /// Milestone 8.8a. `CardPanelTallViewportShare` exists to stop an ordinary one-row tray covering
        /// board nobody asked it to; two rows of whole card faces are taller than that share allows, and
        /// they are drawn only while a mandatory decision has already hidden the tray, so the bound is
        /// stated separately here rather than by raising the share for every tray. The reserved toggle
        /// band is what keeps Undo reachable at the one moment this region is at its largest.
        /// </summary>
        public static float CalculateTallContextHeight(float panelHeight) =>
            Mathf.Max(
                0f,
                panelHeight - (Margin * 3f) - StatusBarHeight - DirectorToggleHeight);

        public static Rect CalculatePlayfieldRect(float panelWidth, float panelHeight)
        {
            var tray = CalculateCardPanelRect(panelWidth, panelHeight);
            return new Rect(
                0f,
                StatusBarHeight,
                Mathf.Max(0f, panelWidth),
                Mathf.Max(0f, tray.yMin - StatusBarHeight));
        }

        /// <summary>
        /// One team's rail column: the whole screen edge from under the status strip to the bottom
        /// margin.
        ///
        /// <para>Milestone 8.25. It used to stop where the tray began, which left about 491 lines at
        /// 1920x1080 - a third of a rail short of holding rulebook p.6's three heroes a side, measured
        /// at 8.23a. A column that runs the whole height has about 1000 there, which holds three rails
        /// of the height 8.23a's recomposition achieved with room over.</para>
        ///
        /// <para><b>The column and the tray meet in the bottom corners, and the tray is drawn over the
        /// column there.</b> That is the user's 2026-08-26 decision. At the reference resolution the
        /// measurement is: three rails draw 733 from 68, so the third ends at 801 against a tray top
        /// edge at 765.6 - an overlap of about 35, which is the third rail's bottom padding and border
        /// rather than any row it draws. `gd-hero-rails` is added to the panel before `gd-tray-region`,
        /// which is what decides the z-order, so a later change reordering those two silently reverses
        /// it.</para>
        ///
        /// <para><b>Milestone 8.25c is why that overlap is now the same on every screen, and why it is
        /// small.</b> Before it, the rails were a fixed count of device pixels and the tray's height a
        /// share of the viewport, so the overlap was about 35 at 2560x1440 and about 230 at 1920x1080 -
        /// which buried the whole third rail on the smaller screen. The layout is written in reference
        /// units now and the reference is 2560x1440, so every screen gets the 35.</para>
        ///
        /// <para>The width bound is unchanged and is still the standing product rule - both rails
        /// together stay under half the narrowest validated viewport, so the board remains the dominant
        /// surface.</para>
        ///
        /// <para><b>Milestone 8.25c re-derived both of 8.25's accepted geometry facts.</b> Three rails
        /// still stack a side - measured at 733 drawn into this 1360-unit column, with room over - and
        /// the tray still meets the third one. What the re-derivation changed is that the meeting is
        /// one number everywhere instead of a different number per screen, and that the number is the
        /// small one. See <see cref="MatchHudPanelScaling"/> for why the reference is 2560x1440 and not
        /// the screen the older numbers were quoted at.</para>
        /// </summary>
        public static Rect CalculateHeroRailRect(float panelWidth, float panelHeight, TeamSide side)
        {
            var width = CalculateHeroRailWidth(panelWidth);
            var x = side == TeamSide.Red
                ? Margin
                : Mathf.Max(Margin, panelWidth - width - Margin);
            var top = StatusBarHeight + Margin;
            return new Rect(x, top, width, Mathf.Max(0f, panelHeight - top - Margin));
        }

        private static float CalculateHeroRailWidth(float panelWidth) =>
            Mathf.Min(HeroRailWidth, Mathf.Max(0f, (panelWidth / 2f) - (Margin * 2f)));

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
        /// Where the `DIRECTOR` toggle stands: just above the tray, or - milestone 8.8a - just under the
        /// status strip while p.15's upgrade grid has the tray region taller than its ordinary bound.
        ///
        /// The move exists because the toggle is the only way to Undo, and Undo is exactly as legitimate
        /// while a mandatory decision stands as at any other moment; leaving it under the enlarged region
        /// would have covered it. <see cref="CalculateTallContextHeight"/> reserves this band, so the
        /// toggle stays clear even when the grid takes the whole bound.
        /// </summary>
        private Rect GetDirectorToggleRect()
        {
            // Beside the Red rail rather than on it: the rail owns the left edge from the status strip
            // to the bottom margin, and the band between it and the board is the only part of this
            // column nothing else draws in. Milestone 8.8a put the tall-context toggle here; milestone
            // 8.25 gave the rail the rest of that column, so the ordinary toggle moves in beside it too
            // and only the height it sits at still depends on what the contextual region is drawing.
            var viewport = PanelViewport;
            var rail = CalculateHeroRailRect(viewport.x, viewport.y, TeamSide.Red);
            if (trayUsesTallContext)
            {
                return new Rect(
                    rail.xMax + Margin,
                    StatusBarHeight + Margin,
                    DirectorToggleWidth,
                    DirectorToggleHeight);
            }

            var playfield = CalculatePlayfieldRect(viewport.x, viewport.y);
            return new Rect(
                rail.xMax + Margin,
                Mathf.Max(Margin, playfield.yMax - DirectorToggleHeight - Margin),
                DirectorToggleWidth,
                DirectorToggleHeight);
        }

        /// <summary>
        /// The drawer opens upward from the toggle, and downward from it when the toggle has moved to the
        /// top of its column, because there is nothing above it there to open into.
        /// </summary>
        private Rect GetDirectorDrawerRect()
        {
            var toggle = GetDirectorToggleRect();
            return new Rect(
                toggle.x,
                trayUsesTallContext
                    ? toggle.yMax + 6f
                    : Mathf.Max(Margin, toggle.yMin - DirectorDrawerHeight - 6f),
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
            // `CalculateTallContextHeight` reserves the same strip: the drawer is the only route to
            // Undo, and a log the player opened is not a reason to lose it. Latent until milestone 8.25
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
