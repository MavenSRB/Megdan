using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z59. THE ASPECT-RATIO GATE: every standing HUD panel, at every shape of
    /// screen this build expects to meet, rather than only at the reference's own 16:9.
    ///
    /// <para><b>Why this suite exists.</b> A live playtester on a 3440x1440 ultrawide reported his
    /// hand drawn on top of the hero rails. Every viewport this project had ever swept -
    /// <c>MatchHudPanelScalingTests.ReferenceShapedResolutions</c> is six entries and all six are
    /// 16:9 - resolves to exactly the reference viewport, which is precisely the family of screens
    /// on which <see cref="MatchHudPanelScaling.Match"/> cannot matter: at 16:9 the two normalized
    /// axes are equal, so every match value produces the same scale. The one constant that governs
    /// off-16:9 behaviour therefore had no test at all, and it was wrong.</para>
    ///
    /// <para><b>The mechanism it now pins.</b> UI Toolkit resolves
    /// <see cref="UnityEngine.UIElements.PanelScreenMatchMode.MatchWidthOrHeight"/> as a LINEAR lerp
    /// of the two normalized axes. At the old <c>match = 0.5</c> a screen wider than 16:9 pulled the
    /// denominator up with its width term, so the panel came out SHORTER than
    /// <see cref="MatchHudPanelScaling.ReferenceHeight"/> in reference units. Every vertical number
    /// in this HUD is a fixed reference constant pinned to the top or the bottom edge, so the hero
    /// rail column - the only "whatever is left between them" region - absorbed the entire loss
    /// (<c>railHeight = panelHeight - 612</c>). With 9.33 units of slack over the live-measured
    /// three-plaque stack it went short at about 16:9.06: 616.8 at 3440x1440 and 348 at 5120x1440,
    /// against 818.6667 required. The plaques overflowed their column into the full-width tray band
    /// and the hand was painted over them.</para>
    ///
    /// <para><b>And the mirror-image defect, which the user predicted.</b> Following the height
    /// (<c>match = 1</c>) closed the ultrawide case but left its twin open: at an aspect NARROWER
    /// than 16:9 the panel keeps its 1440 height and loses reference WIDTH instead, and the board -
    /// right-anchored, its width derived from its height - slides its left edge into the round dock
    /// and the two action rows beside the hand. Measured at 5:4: <c>Playfield x StatusDock</c> by
    /// 264x128, plus 244x36 and 244x78 into those rows. Told that the ultrawide case was fixed the
    /// user answered "What happens when someone plays in 1080p. I dont thik just aspect ratio is the
    /// issue", which is exactly right: TWO regions were each "whatever is left" on a DIFFERENT axis
    /// - the rail column vertically, the board's left edge horizontally - and only one of them had
    /// ever been bounded.</para>
    ///
    /// <para><b>THE RULE, in one sentence: the panel is never smaller than the reference in either
    /// dimension</b> (<see cref="UnityEngine.UIElements.PanelScreenMatchMode.Expand"/>), and every
    /// panel that could grow into a neighbour is bounded against that neighbour rather than against
    /// the panel edge. Expand stops the panel losing reference width or height; the board's new
    /// bound against the round dock stops extra HEIGHT growing it into the dock, which Expand alone
    /// would have allowed at 5:4. 16:9 is untouched by both halves - at 16:9 the two axes normalize
    /// equally so Expand resolves what any match value did, and at the reference the board's height
    /// is the binding constraint (870 against the 1273 the new width bound would allow), so its
    /// rectangle does not move.</para>
    /// </summary>
    public sealed class MatchHudAspectRatioFitTests
    {
        /// <summary>
        /// The device resolutions this gate answers at: the two 16:9 baselines the user has accepted
        /// live, a small laptop, 16:10, both 21:9 shapes (the reported playtester's class), and 32:9.
        /// </summary>
        private static readonly (string Name, float Width, float Height)[] Matrix =
        {
            ("1366x768 (16:9, small laptop)", 1366f, 768f),
            ("1920x1080 (16:9, accepted baseline)", 1920f, 1080f),
            ("2560x1440 (16:9, the reference)", 2560f, 1440f),
            ("1920x1200 (16:10)", 1920f, 1200f),
            ("2560x1080 (21:9)", 2560f, 1080f),
            ("3440x1440 (21:9, the reported playtester)", 3440f, 1440f),
            ("5120x1440 (32:9)", 5120f, 1440f),
            ("1920x1010 (a maximized window on a 1080p monitor)", 1920f, 1010f),
            ("1440x1080 (4:3)", 1440f, 1080f),
            ("1280x1024 (5:4)", 1280f, 1024f),
            ("1024x768 (4:3, small)", 1024f, 768f)
        };

        /// <summary>
        /// What three stacked rulebook p.6 plaques actually draw, measured on a live panel by
        /// <c>MinimalHexGridPlayModeTests.Scene_DrawsAllSixOfficialRailsInsideTheirOwnColumnsWithTheTrayBetweenThem</c>
        /// (its <c>gd-hero-rail-red</c> column <c>worldBound</c>), not an estimate. The reserved
        /// column has to be at least this tall or the third plaque is drawn below its own bottom
        /// edge, which is where the tray band and the hand are.
        /// </summary>
        private const float MeasuredThreeRailStackHeight = 818.6667f;

        /// <summary>
        /// Pairs of standing rectangles that are allowed to intersect, each for a stated reason.
        /// Nothing here is a shrug: a pair is exempt only because it is a parent and its own child,
        /// a layout reservation and something deliberately placed inside it, or two states of one
        /// region that are never on screen together.
        /// </summary>
        private static readonly Dictionary<string, string> PermittedIntersections =
            new Dictionary<string, string>
            {
                // The status strip is the container the three top-row controls are drawn inside.
                ["StatusBar|NarrationBar"] = "The narration bar is drawn inside the strip.",
                ["StatusBar|EndTurn"] = "End Turn fills the strip's top-right slot.",
                ["StatusBar|SeatUndo"] = "The undo square shares End Turn's slot row.",

                // The full-width tray band is a transparent layout reservation, not a drawn surface;
                // CalculateCompactHandPointerRect is the hand's real pointer-owning footprint.
                ["TrayBand|HandFan"] = "The hand is placed inside its own band.",
                ["TrayBand|UnavailableFan"] = "The unavailable fan is placed inside the band.",
                ["TrayBand|UpgradeBand"] = "The Level Up row replaces the band.",
                ["TrayBand|ConnectionBanner"] = "The banner sits under the band's open extent.",
                ["TrayBand|OptionalStep"] = "The optional-step row hangs above the band.",
                ["TrayBand|TeamAgreement"] = "The agreement row hangs above the band.",

                // The Level Up comparison row REPLACES the ordinary tray; the two never coexist, and
                // its documented 550-unit height deliberately rises above the ordinary hand.
                ["UpgradeBand|HandFan"] = "Never on screen together.",
                ["UpgradeBand|UnavailableFan"] = "Never on screen together.",
                ["UpgradeBand|StatusDock"] = "Never on screen together.",
                ["UpgradeBand|OptionalStep"] = "Never on screen together.",
                ["UpgradeBand|TeamAgreement"] = "Never on screen together.",
                ["UpgradeBand|ConnectionBanner"] = "Never on screen together.",
                ["UpgradeBand|Playfield"] = "The taller Level Up row is documented to rise above " +
                    "the ordinary hand into the board's lower band.",
                ["UpgradeBand|RedRail"] = "As above, over the rail columns' reserved bottoms.",
                ["UpgradeBand|BlueRail"] = "As above, over the rail columns' reserved bottoms.",
                ["UpgradeBand|RedRailContent"] = "As above, over the drawn plaques.",
                ["UpgradeBand|BlueRailContent"] = "As above, over the drawn plaques.",

                // Two states of one slot beside the hand.
                ["OptionalStep|TeamAgreement"] = "Mutually exclusive states of one region.",

                // The log drop-down is a transient overlay hung off the narration bar.
                ["LogDropdown|Playfield"] = "A transient overlay may cover the board.",
                ["LogDropdown|StatusDock"] = "A transient overlay may cover the dock.",
                ["LogDropdown|ConnectionBanner"] = "The banner is placed under the open drop-down.",
                ["LogDropdown|OptionalStep"] = "A transient overlay may cover the row.",
                ["LogDropdown|TeamAgreement"] = "A transient overlay may cover the row.",
                ["LogDropdown|UpgradeBand"] = "A transient overlay may cover the row.",
                ["LogDropdown|TrayBand"] = "A transient overlay may cover the band.",
                ["LogDropdown|HandFan"] = "A transient overlay may cover the hand.",
                ["ConnectionBanner|Playfield"] = "The centred notice sits over the board.",
                ["ConnectionBanner|StatusDock"] = "The centred notice may reach the dock.",

                // A rail's reserved column, the plaques drawn in it, and its own banner are one region.
                ["RedRail|RedRailContent"] = "A column and its own content.",
                ["BlueRail|BlueRailContent"] = "A column and its own content.",
                ["RedRail|TieBannerRed"] = "A column and its own banner slot.",
                ["BlueRail|TieBannerBlue"] = "A column and its own banner slot.",
                ["RedRailContent|TieBannerRed"] = "A column's content and its own banner slot.",
                ["BlueRailContent|TieBannerBlue"] = "A column's content and its own banner slot.",

                // The unavailable fan is documented to overlap vertically BENEATH the live Blue rail.
                ["BlueRail|UnavailableFan"] = "CalculateUnavailableCardGroupRect places the fan " +
                    "deliberately beneath the live Blue rail.",
                ["UnavailableFan|BlueRailContent"] = "As above, against the drawn plaques.",

                // PRE-EXISTING AT THE ACCEPTED REFERENCE, and therefore not this milestone's to move:
                // the round dock's left edge is the Red rail's own left edge rather than its right,
                // so the dock reaches under the column. It is identical at 16:9 before and after this
                // change - see TheTwoAcceptedSixteenByNineBaselines_AreUnmovedByTheCorrection - and
                // the user has accepted the 16:9 layout live. Recorded here rather than silently
                // tolerated, and reported to the coordinator as its own separate question.
                ["RedRail|StatusDock"] = "Pre-existing at the accepted 16:9 reference.",
                ["StatusDock|RedRailContent"] = "Pre-existing at the accepted 16:9 reference.",
                ["StatusDock|OptionalStep"] = "Pre-existing at the accepted 16:9 reference.",
                ["StatusDock|TeamAgreement"] = "Pre-existing at the accepted 16:9 reference.",
                ["Playfield|UnavailableFan"] = "Pre-existing at the accepted 16:9 reference."
            };

        /// <summary>
        /// THE CASE THE PLAYTESTER REPORTED. His hand was drawn over the hero rails; this states that
        /// it cannot be, at any shape of screen, against the plaques as DRAWN rather than against the
        /// column the renderer merely reserves. The reserved column is clamped to the tray's top edge
        /// by construction, so asserting on it alone would have passed all the way through the defect.
        /// </summary>
        [Test]
        public void TheHandAndTrayBand_ClearBothDrawnRailStacksAtEveryAspectRatio()
        {
            foreach (var entry in Matrix)
            {
                var panel = MatchHudPanelScaling.ReferenceViewport(entry.Width, entry.Height);
                var hand = ConfiguredMatchHudRenderer.CalculateCompactHandPointerRect(panel.x, panel.y);
                var tray = ConfiguredMatchHudRenderer.CalculateCardPanelRect(panel.x, panel.y);

                foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var drawn = DrawnRailStack(panel, side);
                    Assert.That(hand.Overlaps(drawn), Is.False,
                        $"The hand is drawn over the {side} rail stack at {entry.Name}: hand " +
                        $"{Describe(hand)} against plaques {Describe(drawn)}.");
                    Assert.That(drawn.yMax, Is.LessThanOrEqualTo(tray.yMin + 0.01f),
                        $"The {side} rail stack overflows its column into the reserved card band at " +
                        $"{entry.Name}: plaques end at {drawn.yMax}, the band starts at {tray.yMin}.");
                }
            }
        }

        /// <summary>
        /// THE ONE RULE, stated as an invariant: <b>the panel is never smaller than the reference in
        /// either dimension.</b> A wider screen buys reference width at a full-height panel; a
        /// narrower or taller one buys reference height at a full-width panel. Both of this
        /// milestone's defects are the same failure of that rule on different axes - the ultrawide
        /// one lost reference height, the 4:3/5:4 one lost reference width - so this is the single
        /// assertion that fails for either.
        /// </summary>
        [Test]
        public void EveryAspectRatio_ResolvesAPanelNoSmallerThanTheReferenceInEitherDimension()
        {
            Assert.That(MatchHudPanelScaling.ScreenMatchMode,
                Is.EqualTo(PanelScreenMatchMode.Expand),
                "Expand resolves the scale from whichever axis demands more, which is the only mode " +
                "that honours a layout with a hard minimum on BOTH axes.");

            foreach (var entry in Matrix)
            {
                var panel = MatchHudPanelScaling.ReferenceViewport(entry.Width, entry.Height);
                Assert.That(panel.x,
                    Is.GreaterThanOrEqualTo(MatchHudPanelScaling.ReferenceWidth - 0.01f),
                    $"The panel is narrower than the reference at {entry.Name}; the board would " +
                    "slide left into the round dock.");
                Assert.That(panel.y,
                    Is.GreaterThanOrEqualTo(MatchHudPanelScaling.ReferenceHeight - 0.01f),
                    $"The panel is shorter than the reference at {entry.Name}; the rail column " +
                    "would lose the height three plaques need.");

                // And it is never needlessly larger either: exactly the more demanding axis sits ON
                // the reference, so the HUD keeps the same share of every screen rather than
                // shrinking away from one.
                var onReference =
                    Mathf.Abs(panel.x - MatchHudPanelScaling.ReferenceWidth) < 0.01f ||
                    Mathf.Abs(panel.y - MatchHudPanelScaling.ReferenceHeight) < 0.01f;
                Assert.That(onReference, Is.True,
                    $"Neither axis sits on the reference at {entry.Name}: {panel}. The panel has " +
                    "been scaled past what the screen's shape demands.");
            }
        }

        /// <summary>
        /// The SECOND defect this milestone found, and the one the user predicted when they said
        /// "I don't think just aspect ratio is the issue".
        ///
        /// <para>The board is right-anchored and its width is derived from its height, so its LEFT
        /// edge is whatever is left over - the horizontal twin of the rail column's vertical
        /// elasticity. The round dock stands between the Red rail and the board, and nothing bounded
        /// the board against it: at a panel narrower than about 2064 units the board's left edge
        /// crossed the dock (measured 264x128 at 5:4) and the two action rows beside the hand. Both
        /// halves of the fix are needed - Expand stops the panel losing reference width, and
        /// <c>CalculatePlayfieldRect</c> now bounds the board against the dock so extra HEIGHT
        /// cannot grow it into the dock either.</para>
        /// </summary>
        [Test]
        public void TheBoard_StopsShortOfTheRoundDockAtEveryAspectRatio()
        {
            foreach (var entry in Matrix)
            {
                var panel = MatchHudPanelScaling.ReferenceViewport(entry.Width, entry.Height);
                var board = ConfiguredMatchHudRenderer.CalculatePlayfieldRect(panel.x, panel.y);
                var dock = ConfiguredMatchHudRenderer.CalculateStatusDockRect(panel.x, panel.y);
                var optional = ConfiguredMatchHudRenderer.CalculateOptionalStepControlRect(
                    panel.x, panel.y);
                var agreement = ConfiguredMatchHudRenderer.CalculateTeamAgreementControlRect(
                    panel.x, panel.y);

                Assert.That(board.xMin, Is.GreaterThanOrEqualTo(dock.xMax),
                    $"The board reaches the round dock at {entry.Name}: board starts at " +
                    $"{board.xMin}, the dock ends at {dock.xMax}.");
                Assert.That(board.Overlaps(optional), Is.False,
                    $"The board reaches the optional-step row at {entry.Name}.");
                Assert.That(board.Overlaps(agreement), Is.False,
                    $"The board reaches the team-agreement row at {entry.Name}.");
                Assert.That(board.width, Is.GreaterThan(0f).And.EqualTo(
                        board.height * ConfiguredMatchHudRenderer.BoardViewportAspect).Within(0.01f),
                    $"The board keeps its native framing at {entry.Name} rather than being " +
                    "squared off by the new bound.");
            }
        }

        /// <summary>
        /// The three-rail fit, at every aspect ratio rather than at the reference alone. The budget is
        /// the live-measured stack, so this is the gate that goes red if a later milestone spends the
        /// column's remaining 9.33 units of slack.
        /// </summary>
        [Test]
        public void EveryAspectRatio_ReservesTheLiveMeasuredThreeRailStack()
        {
            foreach (var entry in Matrix)
            {
                var panel = MatchHudPanelScaling.ReferenceViewport(entry.Width, entry.Height);
                foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var column = ConfiguredMatchHudRenderer.CalculateHeroRailRect(
                        panel.x, panel.y, side);
                    Assert.That(column.height,
                        Is.GreaterThanOrEqualTo(MeasuredThreeRailStackHeight),
                        $"The {side} column reserves {column.height} at {entry.Name}, short of the " +
                        $"live-measured {MeasuredThreeRailStackHeight} three plaques draw.");
                }
            }
        }

        /// <summary>
        /// THE CLASS RATHER THAN THE INSTANCE. Every standing rectangle against every other one, at
        /// every entry in the matrix: the set of intersecting pairs must be exactly the set the
        /// accepted 2560x1440 reference already carries, and no pair may intersect more deeply
        /// VERTICALLY than it does there. A future panel that collides at 21:9 but not at 16:9 fails
        /// here, offline, without a Unity launch.
        /// </summary>
        [Test]
        public void EveryAspectRatio_IntersectsExactlyWhatTheAcceptedReferenceAlreadyDoes()
        {
            var reference = MatchHudPanelScaling.ReferenceViewport(2560f, 1440f);
            var accepted = Intersections(reference)
                .ToDictionary(found => found.Pair, found => found.Vertical);

            foreach (var entry in Matrix)
            {
                var panel = MatchHudPanelScaling.ReferenceViewport(entry.Width, entry.Height);
                var found = Intersections(panel).ToList();

                var introduced = found
                    .Where(candidate => !accepted.ContainsKey(candidate.Pair))
                    .Select(candidate => candidate.Describe())
                    .ToArray();
                Assert.That(introduced, Is.Empty,
                    $"{entry.Name} introduces intersections the accepted 16:9 reference does not " +
                    $"have: {string.Join("; ", introduced)}.");

                foreach (var candidate in found)
                {
                    Assert.That(candidate.Vertical,
                        Is.LessThanOrEqualTo(accepted[candidate.Pair] + 0.01f),
                        $"{candidate.Pair} intersects {candidate.Vertical} deep at {entry.Name}, " +
                        $"more than the {accepted[candidate.Pair]} the reference accepts.");
                }
            }
        }

        /// <summary>
        /// THE REGRESSION PROOF the user's accepted screens are owed: at 16:9 the two normalized axes
        /// are equal, so <see cref="MatchHudPanelScaling.Match"/> cannot change the resolved scale -
        /// and every rectangle is therefore identical to the numbers milestone 8.30b and the
        /// 2026-09-06/08 refinements pinned. Stated against those literals rather than against a
        /// recomputation of the same formula, so a change to the formula fails here too.
        /// </summary>
        [Test]
        public void TheTwoAcceptedSixteenByNineBaselines_AreUnmovedByTheCorrection()
        {
            foreach (var device in new[] { new Vector2(1920f, 1080f), new Vector2(2560f, 1440f) })
            {
                var panel = MatchHudPanelScaling.ReferenceViewport(device.x, device.y);
                Assert.That(panel, Is.EqualTo(new Vector2(2560f, 1440f)),
                    $"{device} still resolves exactly the reference viewport.");

                var w = panel.x;
                var h = panel.y;
                Assert.That(ConfiguredMatchHudRenderer.CalculateStatusBarRect(w, h),
                    Is.EqualTo(new Rect(0f, 0f, 2560f, 96f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateNarrationBarRect(w, h),
                    Is.EqualTo(new Rect(830f, 56f, 900f, 40f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateHeroRailRect(w, h, TeamSide.Red),
                    Is.EqualTo(new Rect(12f, 128f, 264f, 828f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateHeroRailRect(w, h, TeamSide.Blue),
                    Is.EqualTo(new Rect(2284f, 128f, 264f, 828f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculatePlayfieldRect(w, h),
                    Is.EqualTo(new Rect(1228f, 96f, 1044f, 870f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateStatusDockRect(w, h),
                    Is.EqualTo(new Rect(12f, 838f, 720f, 128f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateCardPanelRect(w, h),
                    Is.EqualTo(new Rect(0f, 968f, 2560f, 460f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateCompactHandPointerRect(w, h),
                    Is.EqualTo(new Rect(0f, 968f, 732f, 460f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateUpgradeComparisonPanelRect(w, h),
                    Is.EqualTo(new Rect(0f, 878f, 2560f, 550f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateEndTurnControlRect(w, h),
                    Is.EqualTo(new Rect(2284f, 0f, 264f, 96f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateSeatUndoControlRect(w, h),
                    Is.EqualTo(new Rect(2188f, 0f, 96f, 96f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateMatchLogDropdownRect(w, h),
                    Is.EqualTo(new Rect(830f, 96f, 900f, 320f)));
                Assert.That(ConfiguredMatchHudRenderer.CalculateConnectionBannerRect(w, h),
                    Is.EqualTo(new Rect(1020f, 428f, 520f, 40f)));
                Assert.That(
                    ConfiguredMatchHudRenderer.CalculateTieBreakerRailBannerRect(w, h, TeamSide.Red),
                    Is.EqualTo(new Rect(12f, 108f, 264f, 16f)));
                Assert.That(
                    ConfiguredMatchHudRenderer.CalculateTieBreakerRailBannerRect(w, h, TeamSide.Blue),
                    Is.EqualTo(new Rect(2284f, 108f, 264f, 16f)));
            }
        }

        /// <summary>
        /// The plaques as DRAWN: the stack starts at the column's own top and is the live-measured
        /// height whatever the column reserves, which is exactly why it could be drawn outside it.
        /// </summary>
        private static Rect DrawnRailStack(Vector2 panel, TeamSide side)
        {
            var column = ConfiguredMatchHudRenderer.CalculateHeroRailRect(panel.x, panel.y, side);
            return new Rect(column.x, column.y, column.width, MeasuredThreeRailStackHeight);
        }

        private static IEnumerable<(string Pair, float Vertical, float Horizontal)> Intersections(
            Vector2 panel)
        {
            var rects = StandingRects(panel);
            for (var i = 0; i < rects.Count; i++)
            {
                for (var j = i + 1; j < rects.Count; j++)
                {
                    var a = rects[i];
                    var b = rects[j];
                    if (a.Rect.width <= 0f || a.Rect.height <= 0f ||
                        b.Rect.width <= 0f || b.Rect.height <= 0f)
                    {
                        continue;
                    }

                    if (PermittedIntersections.ContainsKey($"{a.Name}|{b.Name}") ||
                        PermittedIntersections.ContainsKey($"{b.Name}|{a.Name}"))
                    {
                        continue;
                    }

                    if (!a.Rect.Overlaps(b.Rect))
                    {
                        continue;
                    }

                    yield return (
                        $"{a.Name} x {b.Name}",
                        Mathf.Min(a.Rect.yMax, b.Rect.yMax) - Mathf.Max(a.Rect.yMin, b.Rect.yMin),
                        Mathf.Min(a.Rect.xMax, b.Rect.xMax) - Mathf.Max(a.Rect.xMin, b.Rect.xMin));
                }
            }
        }

        /// <summary>
        /// Every rectangle the HUD stands up, named. The two rail entries are deliberately doubled:
        /// the reserved column AND the stack actually drawn into it, because the defect this suite
        /// exists for lived entirely in the gap between those two.
        /// </summary>
        private static List<(string Name, Rect Rect)> StandingRects(Vector2 panel)
        {
            var w = panel.x;
            var h = panel.y;
            return new List<(string, Rect)>
            {
                ("StatusBar", ConfiguredMatchHudRenderer.CalculateStatusBarRect(w, h)),
                ("NarrationBar", ConfiguredMatchHudRenderer.CalculateNarrationBarRect(w, h)),
                ("EndTurn", ConfiguredMatchHudRenderer.CalculateEndTurnControlRect(w, h)),
                ("SeatUndo", ConfiguredMatchHudRenderer.CalculateSeatUndoControlRect(w, h)),
                ("TieBannerRed", ConfiguredMatchHudRenderer.CalculateTieBreakerRailBannerRect(
                    w, h, TeamSide.Red)),
                ("TieBannerBlue", ConfiguredMatchHudRenderer.CalculateTieBreakerRailBannerRect(
                    w, h, TeamSide.Blue)),
                ("RedRail", ConfiguredMatchHudRenderer.CalculateHeroRailRect(w, h, TeamSide.Red)),
                ("BlueRail", ConfiguredMatchHudRenderer.CalculateHeroRailRect(w, h, TeamSide.Blue)),
                ("RedRailContent", DrawnRailStack(panel, TeamSide.Red)),
                ("BlueRailContent", DrawnRailStack(panel, TeamSide.Blue)),
                ("Playfield", ConfiguredMatchHudRenderer.CalculatePlayfieldRect(w, h)),
                ("StatusDock", ConfiguredMatchHudRenderer.CalculateStatusDockRect(w, h)),
                ("HandFan", ConfiguredMatchHudRenderer.CalculateCompactHandPointerRect(w, h)),
                ("TrayBand", ConfiguredMatchHudRenderer.CalculateCardPanelRect(w, h)),
                ("UnavailableFan", ConfiguredMatchHudRenderer.CalculateUnavailableCardGroupRect(w, h)),
                ("LogDropdown", ConfiguredMatchHudRenderer.CalculateMatchLogDropdownRect(w, h)),
                ("ConnectionBanner", ConfiguredMatchHudRenderer.CalculateConnectionBannerRect(w, h)),
                ("OptionalStep", ConfiguredMatchHudRenderer.CalculateOptionalStepControlRect(w, h)),
                ("TeamAgreement", ConfiguredMatchHudRenderer.CalculateTeamAgreementControlRect(w, h)),
                ("UpgradeBand", ConfiguredMatchHudRenderer.CalculateUpgradeComparisonPanelRect(w, h))
            };
        }

        private static string Describe(Rect rect) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "[x {0:0.##}..{1:0.##}, y {2:0.##}..{3:0.##}]",
                rect.xMin, rect.xMax, rect.yMin, rect.yMax);
    }

    internal static class AspectRatioIntersectionExtensions
    {
        public static string Describe(this (string Pair, float Vertical, float Horizontal) found) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0} by {1:0.##} x {2:0.##}",
                found.Pair, found.Horizontal, found.Vertical);
    }
}
