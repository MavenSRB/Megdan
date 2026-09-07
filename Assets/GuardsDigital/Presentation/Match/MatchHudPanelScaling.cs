using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// The one declaration of the coordinate space every HUD number is written in.
    ///
    /// <para><b>Milestone 8.25c.</b> Until this milestone the panel was
    /// <see cref="PanelScaleMode.ConstantPixelSize"/> at <c>scale = 1</c>, so every layout number in
    /// this namespace was a literal device pixel: a hero rail was 192 pixels and a card face roughly
    /// 300.46 by 420 on a 1280-wide screen and on a 2560-wide one alike, and the HUD took half its designed
    /// share of the second. The panel now scales with the viewport, and every number here - a rail's
    /// width, a card's footprint, the status strip's height, every margin - is a <em>reference unit</em>
    /// rather than a pixel. One reference unit is one device pixel at
    /// <see cref="ReferenceWidth"/>x<see cref="ReferenceHeight"/> and grows or shrinks with the
    /// screen.</para>
    ///
    /// <para><b>Why 2560x1440, and why picking 1920x1080 first was the wrong answer.</b> The reference
    /// has to be the size the layout actually fits at, and this one does not fit at 1920x1080. Rulebook
    /// p.6's three rails a side draw 733 from the top of the column, so the third rail ends at 801; the
    /// tray takes 0.46 of the height off the bottom, which puts its top edge at 571 on a 1080-tall
    /// panel and at 766 on a 1440-tall one. On the smaller panel the tray covers all but nine units of
    /// the third rail - two of six configured heroes with no readable rail - and the tray's own region
    /// is a few units too short for a whole 420 card face, so the hand is guillotined too. The layout
    /// needs about 1506 units of height before the third rail clears, and 1440 leaves the 35 units of
    /// bottom padding this build has always drawn under the tray.</para>
    ///
    /// <para>This mattered more than a reference resolution normally would, because before scaling a
    /// bigger screen bought real extra room: the rails were a fixed count of pixels while the tray was
    /// a share of the viewport, so a 2560x1440 screen had 1360 lines of column and the whole HUD fitted
    /// there. Declaring 1920x1080 the reference would have taken that room away from every screen at
    /// once - it made the 2560 screen render the 1080 layout, magnified. That was observed rather than
    /// reasoned about: the arrangement was captured at both resolutions under both references.</para>
    ///
    /// <para><b>THE ONE RULE, and why halfway was the wrong answer.</b> The panel is never smaller
    /// than the reference in either dimension: <see cref="PanelScreenMatchMode.Expand"/> resolves
    /// the scale from whichever axis demands more, so a wider screen buys reference WIDTH at a
    /// full-height panel and a narrower or taller one buys reference HEIGHT at a full-width panel.
    /// On a 16:9 screen the two axes normalize identically, so the minimum of them is that same
    /// number the old halfway match produced - 2560x1440 and 1920x1080 are pixel-identical across
    /// this change. See <see cref="ScreenMatchMode"/> for the two measured defects that rule
    /// closes.</para>
    ///
    /// <para><b>Milestone 8.35z59 corrects this constant.</b> It was 0.5, and the comment
    /// here claimed splitting the difference "keeps an ultrawide window from shortening the panel".
    /// That is inverted, and a live playtest found it: UI Toolkit resolves
    /// <c>MatchWidthOrHeight</c> as a LINEAR lerp of the two normalized axes (unlike UGUI's
    /// <c>CanvasScaler</c>, which lerps in log space), so at 0.5 an aspect ratio wider than 16:9
    /// pulls the denominator UP with its width term and the panel comes out SHORTER than
    /// <see cref="ReferenceHeight"/>. Splitting the difference is precisely what shortened it.
    /// Every vertical number in this HUD - the status strip, the standing margin, the rail banner
    /// and its gap, the reserved card band - is a fixed reference constant pinned to the top or the
    /// bottom edge, so the hero rail column is the only "whatever is left" region and absorbed the
    /// whole loss: <c>railHeight = panelHeight - 612</c>. It carries just 9.33 units of slack over
    /// the live-measured 818.6667 three-plaque stack at the reference, so it went short at about
    /// 16:9.06 and collapsed from there - 616.8 at 3440x1440 (201.87 short) and 348 at 5120x1440
    /// (470.67 short). The plaques overflowed their column into the full-width tray band and the
    /// hand was drawn over them, which is exactly what the ultrawide playtester reported.</para>
    ///
    /// <para><b>And its mirror image, which the first pass at this milestone missed.</b> Correcting
    /// the constant to 1 (follow the height) closed the ultrawide case and left the narrow one wide
    /// open: below 16:9 the panel keeps its height and loses reference WIDTH instead, and the board
    /// - right-anchored, its width derived from its height - slid its left edge into the round dock
    /// (measured 264x128 at 5:4) and the action rows beside the hand. Two regions were each
    /// "whatever is left" on a DIFFERENT axis, and only one had ever been bounded. That is why the
    /// mode rather than the constant is the answer here, and why
    /// <c>ConfiguredMatchHudRenderer.CalculatePlayfieldRect</c> now bounds the board against the
    /// dock as well: <see cref="PanelScreenMatchMode.Expand"/> stops the panel losing either
    /// dimension, and the bound stops the extra HEIGHT it grants at a narrow aspect from widening
    /// the board into the dock. <c>MatchHudAspectRatioFitTests</c> gates both halves across an
    /// eleven-entry matrix offline.</para>
    ///
    /// <para><b>The scale is stated here rather than read from the live panel.</b> An Edit Mode
    /// fixture has no panel and the pointer boundary has to answer at every viewport, so the formula
    /// is reproduced - and <c>MinimalHexGridPlayModeTests</c> measures the live panel against it at a
    /// viewport that is not the reference, so "the formula is Unity's" is a measurement rather than a
    /// belief.</para>
    /// </summary>
    public static class MatchHudPanelScaling
    {
        /// <summary>
        /// The reference viewport's width in device pixels. One reference unit is one device pixel at
        /// this width on a 16:9 screen.
        /// </summary>
        public const float ReferenceWidth = 2560f;

        /// <summary>The reference viewport's height in device pixels.</summary>
        public const float ReferenceHeight = 1440f;

        /// <summary>
        /// How the panel resolves its scale. Stated here and applied to the runtime panel by
        /// <see cref="Apply"/>; the committed panel-settings asset carries the same three values, and
        /// <c>MatchHudPresentationTests</c> asserts that it does rather than letting the code quietly
        /// override it.
        /// </summary>
        public const PanelScaleMode ScaleMode = PanelScaleMode.ScaleWithScreenSize;

        /// <summary>
        /// How a screen that is not the reference's shape is matched to it.
        ///
        /// <para><b>Milestone 8.35z59 settles this on EXPAND</b>, which resolves the scale
        /// from whichever axis is the more demanding - <c>min(w/refW, h/refH)</c> - and therefore
        /// guarantees the panel is <em>never smaller than the reference in either dimension</em>.
        /// A wider screen buys reference width at a full-height panel; a narrower or taller one buys
        /// reference height at a full-width panel. That is the exact invariant this layout needs,
        /// because it has a hard minimum on BOTH axes: three rulebook p.6 rails a side plus a whole
        /// 420-unit card face vertically, and two 264-unit rail columns either side of a board and a
        /// round dock horizontally.</para>
        ///
        /// <para>The first pass at this milestone used <c>MatchWidthOrHeight</c> at
        /// <see cref="Match"/> 1 (follow the height), which fixed the reported ultrawide defect but
        /// left the mirror-image one intact: at an aspect NARROWER than 16:9 the panel kept its 1440
        /// height and lost reference WIDTH, and the board - right-anchored, its width derived from
        /// its height - slid its left edge into the round dock and the action rows beside the hand
        /// (measured: 264x128 into the dock at 5:4). The user put it exactly right - "I don't think
        /// just aspect ratio is the issue". Two different regions were each "whatever is left" on a
        /// different axis, and only one of them had been bounded.</para>
        ///
        /// <para>At 16:9 the two normalized axes are equal, so the minimum of them is that same
        /// number and this mode resolves precisely what every match value did - the accepted
        /// baselines are pixel-identical across both passes.</para>
        /// </summary>
        public const PanelScreenMatchMode ScreenMatchMode = PanelScreenMatchMode.Expand;

        /// <summary>
        /// 0 follows the width, 1 follows the height, and 0.5 splits the difference.
        ///
        /// <para><b>INERT while <see cref="ScreenMatchMode"/> is
        /// <see cref="PanelScreenMatchMode.Expand"/></b>, which reads neither axis's weight. It is
        /// still declared because the panel-settings asset carries a value for it either way, and it
        /// is deliberately left at 1 rather than restored to the 0.5 this milestone found defective:
        /// anyone who flips the mode back to <see cref="PanelScreenMatchMode.MatchWidthOrHeight"/>
        /// should land on "follow the height", which at least keeps the reported ultrawide defect
        /// closed, rather than on the halfway value that caused it.</para>
        /// </summary>
        public const float Match = 1f;

        /// <summary>The reference resolution as the panel-settings asset carries it.</summary>
        public static Vector2Int ReferenceResolution =>
            new Vector2Int(Mathf.RoundToInt(ReferenceWidth), Mathf.RoundToInt(ReferenceHeight));

        /// <summary>
        /// The panel's own scale factor at a device viewport: how many reference units one device
        /// pixel is worth. It is 1 at the reference viewport, below 1 on a larger screen - where the
        /// same reference unit is drawn across more pixels - and above 1 on a smaller one.
        ///
        /// <para>This is Unity's own <c>ScaleWithScreenSize</c> resolution, reproduced so that a
        /// fixture with no live panel can ask the same question a running one answers.</para>
        /// </summary>
        public static float PanelPointsPerScreenPixel(float viewportWidth, float viewportHeight)
        {
            if (viewportWidth <= 0f || viewportHeight <= 0f)
            {
                return 1f;
            }

            // Unity's own PanelSettings resolution for the mode this panel declares. EXPAND takes the
            // MINIMUM of the two normalized axes, so the panel covers the screen without ever being
            // smaller than the reference in either dimension; MatchWidthOrHeight lerps between them
            // LINEARLY (unlike UGUI's CanvasScaler, which lerps in log space - a real trap, since the
            // two agree only at the reference and at 16:9).
            var normalized = new Vector2(viewportWidth / ReferenceWidth, viewportHeight / ReferenceHeight);
            var denominator = ScreenMatchMode switch
            {
                PanelScreenMatchMode.Expand => Mathf.Min(normalized.x, normalized.y),
                PanelScreenMatchMode.Shrink => Mathf.Max(normalized.x, normalized.y),
                _ => Mathf.Lerp(normalized.x, normalized.y, Mathf.Clamp01(Match))
            };
            return denominator <= 0f ? 1f : 1f / denominator;
        }

        /// <summary>
        /// How many device pixels one reference unit is drawn across - Unity's own
        /// <c>scaledPixelsPerPoint</c>, and the inverse of <see cref="PanelPointsPerScreenPixel"/>.
        /// </summary>
        public static float ScreenPixelsPerPanelPoint(float viewportWidth, float viewportHeight)
        {
            var scale = PanelPointsPerScreenPixel(viewportWidth, viewportHeight);
            return scale <= 0f ? 1f : 1f / scale;
        }

        /// <summary>
        /// The panel's size in reference units at a device viewport. This is the "viewport" every
        /// <c>Calculate*Rect</c> in <see cref="ConfiguredMatchHudRenderer"/> lays out into: on a 16:9
        /// screen it is exactly the reference resolution whatever the screen's pixel count, which is
        /// what makes the HUD the same share of every one of them.
        /// </summary>
        public static Vector2 ReferenceViewport(float viewportWidth, float viewportHeight)
        {
            var scale = PanelPointsPerScreenPixel(viewportWidth, viewportHeight);
            return new Vector2(Mathf.Max(0f, viewportWidth) * scale, Mathf.Max(0f, viewportHeight) * scale);
        }

        /// <summary>The panel's size in reference units at the current screen.</summary>
        public static Vector2 CurrentReferenceViewport() =>
            ReferenceViewport(Screen.width, Screen.height);

        /// <summary>
        /// Where a screen position lands on the panel: the origin moves from the screen's bottom-left
        /// corner to the panel's top-left one, and the device pixel becomes a reference unit.
        ///
        /// <para><b>This is the conversion milestone 8.25c exists to add.</b> Under
        /// <c>ConstantPixelSize</c> at <c>scale = 1</c> the flip alone was the whole mapping, because
        /// a device pixel and a panel point were the same thing; at any other scale a flip alone
        /// blocks the pointer in the wrong places, throwing nothing while it does it. Every board
        /// click in the game passes through
        /// <see cref="ConfiguredMatchHudRenderer.BlocksPointerAt"/> and therefore through here.</para>
        /// </summary>
        public static Vector2 ScreenToPanelPoint(
            Vector2 screenPosition, float viewportWidth, float viewportHeight)
        {
            var scale = PanelPointsPerScreenPixel(viewportWidth, viewportHeight);
            return new Vector2(
                screenPosition.x * scale,
                (Mathf.Max(0f, viewportHeight) - screenPosition.y) * scale);
        }

        /// <summary>
        /// The inverse of <see cref="ScreenToPanelPoint"/>: where on the screen a point on the panel
        /// is drawn. It is what lets a check state a position in the units the HUD is laid out in and
        /// then ask the pointer boundary about the pixel a player would actually click.
        /// </summary>
        public static Vector2 PanelPointToScreen(
            Vector2 panelPoint, float viewportWidth, float viewportHeight)
        {
            var pixels = ScreenPixelsPerPanelPoint(viewportWidth, viewportHeight);
            return new Vector2(
                panelPoint.x * pixels,
                Mathf.Max(0f, viewportHeight) - (panelPoint.y * pixels));
        }

        /// <summary>
        /// Puts this declaration onto a runtime panel.
        ///
        /// <para>The committed asset already carries all four values, so this changes nothing on a
        /// copy of it. It exists for the one path that has no asset to copy - milestone 7.9's bare
        /// <c>CreateInstance</c> fallback - so that a build which cannot resolve the asset still lays
        /// out in the same coordinate space rather than silently reverting to device pixels.</para>
        /// </summary>
        public static void Apply(PanelSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.scaleMode = ScaleMode;
            settings.referenceResolution = ReferenceResolution;
            settings.screenMatchMode = ScreenMatchMode;
            settings.match = Match;
        }
    }
}
