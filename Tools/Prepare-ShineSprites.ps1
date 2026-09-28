<#
.SYNOPSIS
    Prepares per-card shine-overlay sprites for milestone 8.30d as ignored Unity resources.

.DESCRIPTION
    Milestone 8.30d. For every prepared private card front (372x520, see Prepare-CardArtwork.ps1)
    this command derives one full-card-sized transparent RGBA overlay per printed badge - the
    rail's secondary Defense and Movement badges and the primary action's icon badge - and writes
    them beneath:

        Assets/Resources/Private/Cards/<content-id>/_shine/<definition-local-id>--<slot>.png

    with slot one of primary, secondary-defense, secondary-movement. Which slots a card owns
    comes from the typed content manifest, never from pixels: rulebook p.16's Ultimates are
    passive and get no sprite at all, and a single rail badge sits in the rail's LOWER position
    (verified over all 108 fronts). Pixels only decide geometry. The shared Movement boot uses one
    audited canonical rail silhouette (`sabina/point-blank-shot`) at the shared lower-rail anchor;
    per-card colour classification cannot collapse it around the numeral or merge it into banner
    cloth. Defense rails remain traced from their own fronts. The primary badge glyphs are the same artwork on every card - only position,
    ink colour and the value digit vary - so each glyph family's solid mask is VOTED from
    hand-audited clean per-card traces, anchored per card to its own panel geometry by a
    bounded evidence search, and the card's own printed digit is unioned back in so a 7 shines
    as a 7. Each overlay bakes the approved steady shine (silhouette rim, close radiance, wide
    halo, mild ink lift). The target directory is ignored by Git and the runtime degrades
    gracefully when a sprite is absent. The command does not invoke Git, Unity, a build, or a
    network client.

.PARAMETER ContentManifest
    CSV exported from the verified content (hero,card,ultimate,primaryKind,primaryIsBlock,
    railChannels). Regenerate it with the Core-compiled manifest probe whenever verified content
    changes; the Edit Mode suite independently cross-checks sprites against typed content.

.PARAMETER Check
    Read-only verification: re-traces every front in memory and fails on any missing, stale, or
    stray sprite.

.PARAMETER Force
    Regenerate and overwrite every sprite even when all of them already match.

.PARAMETER Report
    Path for the per-slot trace report CSV. Defaults to tmp/shine-sprites-report.csv.
#>
[CmdletBinding()]
param(
    [string] $ContentManifest,
    [switch] $Check,
    [switch] $Force,
    [string] $Report
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$cardsRoot = [System.IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'Private', 'Cards')
if (-not $Report) {
    $Report = [System.IO.Path]::Combine($projectRoot, 'tmp', 'shine-sprites-report.csv')
}

if (-not $ContentManifest) {
    $ContentManifest = [System.IO.Path]::Combine($projectRoot, 'tmp', 'shine-content-manifest.csv')
}

if (-not (Test-Path -LiteralPath $cardsRoot -PathType Container)) {
    throw "The prepared private card fronts are absent; run Tools/Prepare-CardArtwork.ps1 first."
}

if (-not (Test-Path -LiteralPath $ContentManifest -PathType Leaf)) {
    throw "The typed content manifest '$ContentManifest' is absent; export it from the verified content first."
}

Add-Type -AssemblyName System.Drawing

$tracerSource = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GuardsDigital.Tools
{
    public static class ShineTracer
    {
        public const int CardWidth = 372;
        public const int CardHeight = 520;

        // Rail badge windows in the shared anatomy (verified over all 108 fronts).
        private const int RailX0 = 10, RailX1 = 82;
        private const int UpperY0 = 118, UpperY1 = 200;
        private const int LowerY0 = 192, LowerY1 = 266;

        // The primary badge search band and the panel-top detector's scan range.
        private const int PrimX0 = 0, PrimX1 = 112;
        private const int PrimY0 = 298, PrimY1 = 478;

        public sealed class SlotTrace
        {
            public string Slot;
            public string Status;
            public int X0, Y0, X1, Y1, Area;
            public byte[] Sprite;
        }

        public sealed class GlyphTemplate
        {
            public string Group;
            public byte[] Solid;   // trimmed W*H solid mask
            public int W, H;
            public int AnchorX;    // median absolute badge x0 across members
            public int AnchorDy;   // median (badge y0 - panel top) across members
            public int Members;
        }

        private sealed class TemplateMember
        {
            public byte[] Solid;
            public int W, H;
            public int X0, Y0, X1, Y1;
            public int AbsX0, AbsY0, AbsY1;
            public int PanelTop;
        }

        private static readonly Dictionary<string, List<TemplateMember>> PendingMembers =
            new Dictionary<string, List<TemplateMember>>();
        private static readonly Dictionary<string, GlyphTemplate> Templates =
            new Dictionary<string, GlyphTemplate>();
        private static SlotTrace movementRailTemplate;

        public static void ResetTemplates()
        {
            PendingMembers.Clear();
            Templates.Clear();
            movementRailTemplate = null;
        }

        public static byte[] LoadCard(string inputPath)
        {
            using (var bitmap = new Bitmap(inputPath))
            {
                if (bitmap.Width != CardWidth || bitmap.Height != CardHeight)
                {
                    throw new InvalidOperationException(
                        "Prepared front is not 372x520: " + inputPath);
                }

                var data = bitmap.LockBits(
                    new Rectangle(0, 0, CardWidth, CardHeight),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var rgba = new byte[CardWidth * CardHeight * 4];
                Marshal.Copy(data.Scan0, rgba, 0, rgba.Length);
                bitmap.UnlockBits(data);
                return rgba;
            }
        }

        private static void Classify(byte[] rgba, int x, int y, out int r, out int g, out int b, out int lum, out int sat, out int max, out int min)
        {
            int i = (y * CardWidth + x) * 4;
            b = rgba[i]; g = rgba[i + 1]; r = rgba[i + 2];
            max = Math.Max(r, Math.Max(g, b));
            min = Math.Min(r, Math.Min(g, b));
            lum = (r + g + b) / 3;
            sat = max - min;
        }

        // The card's panel top: the first row that starts a SUSTAINED run of light-neutral rows -
        // the rules panel's body. A single bright art row can fake one row but not twenty-one,
        // and the vivid ribbon above the frame never qualifies at all.
        public static int FindPanelTop(byte[] rgba)
        {
            var lightRows = new bool[CardHeight];
            var lightCounts = new int[CardHeight];
            for (int y = PrimY0; y <= Math.Min(CardHeight - 1, PrimY1 + 30); y++)
            {
                int light = 0;
                for (int x = 6; x <= 106; x++)
                {
                    int r, g, b, lum, sat, max, min;
                    Classify(rgba, x, y, out r, out g, out b, out lum, out sat, out max, out min);
                    if (min > 170 && sat < 30)
                    {
                        light++;
                    }
                }

                lightRows[y] = light >= 45;
                lightCounts[y] = light;
            }

            // Tolerant sustained run: the panel body is light for a long stretch, but a dense bold
            // text row can dip under the per-row line, so up to four of twenty-five rows may miss.
            // The window's MEDIAN must still be near-solid parchment: a pale checkered art row
            // reaches ~50-55 light pixels and can fake the per-row bar (mad-dash's art anchored a
            // sprite 25px high), but a run of them can never reach a parchment window's median,
            // because the panel's blank margins and interline gaps hold ~90.
            for (int y = PrimY0; y <= PrimY1; y++)
            {
                int hits = 0;
                var window = new List<int>();
                for (int k = 0; k < 25 && y + k < CardHeight; k++)
                {
                    if (lightRows[y + k])
                    {
                        hits++;
                    }

                    window.Add(lightCounts[y + k]);
                }

                if (hits >= 21 && lightRows[y] && Median(window) >= 72)
                {
                    return y;
                }
            }

            return -1;
        }

        // ---- Rail badges (traced per card; the banner sample separates icon from cloth). ----

        public static SlotTrace TraceRail(byte[] rgba, bool upper)
        {
            var strict = TraceRail(rgba, upper, 90);
            if (strict.Status == "ok")
            {
                return strict;
            }

            // Four red-banner cards print a red-tinted boot whose leather sits inside the strict
            // banner distance. Its black outline and white digit are pristine, though - so the
            // fallback traces those alone and closes the outline curve into the badge solid.
            return TraceRail(rgba, upper, -1);
        }

        public static string SetMovementRailTemplate(byte[] rgba)
        {
            var trace = TraceRail(rgba, false);
            if (trace.Status != "ok")
            {
                return trace.Status;
            }

            movementRailTemplate = trace;
            return "ok rail-movement-template " + trace.Area;
        }

        public static SlotTrace TraceMovementRail()
        {
            if (movementRailTemplate == null)
            {
                return new SlotTrace { Slot = "rail-lower", Status = "missing-movement-template" };
            }

            return new SlotTrace
            {
                Slot = "rail-lower",
                Status = "ok-template",
                X0 = movementRailTemplate.X0,
                Y0 = movementRailTemplate.Y0,
                X1 = movementRailTemplate.X1,
                Y1 = movementRailTemplate.Y1,
                Area = movementRailTemplate.Area,
                Sprite = (byte[])movementRailTemplate.Sprite.Clone()
            };
        }

        // bannerDistance -1 selects the outline-and-digit fallback mask.

        private static SlotTrace TraceRail(byte[] rgba, bool upper, int bannerDistance)
        {
            return TraceRailWindow(rgba, upper ? UpperY0 : LowerY0, upper ? UpperY1 : LowerY1,
                upper ? "rail-upper" : "rail-lower", bannerDistance);
        }

        // Milestone 8.112g. Mrak's Red faces print a THIRD rail badge - the crossed swords of his
        // printed secondary Attack - below the boot, packed tighter and higher than an ordinary pair
        // (8.112a measured the boot's value at y=179..207 and the swords' at y=241..269 against
        // y=210..238 for an ordinary boot). The swords are traced from each card like a Defense
        // shield, in the band that holds them; Stone Carapace and Rock Solid print the swords in the
        // ordinary LOWER window, where no boot is.
        public const int ThreeBadgeSwordsY0 = 224, ThreeBadgeSwordsY1 = 292;
        public const int ThreeBadgeBootShift = -31;

        // Outline-and-digit only: on these rails the banner-colour sample (y=108..120) lands on the
        // shield that three badges push upward, and the swords' band runs past the banner's point
        // into the artwork - either makes "far from the banner" select the whole window. The black
        // outline and the white digit are what every printed badge shares.
        public static SlotTrace TraceRailOutline(byte[] rgba, bool upper)
        {
            return TraceRail(rgba, upper, -1);
        }

        // The three-badge rail's shield and swords are the same glyphs on all three Seismic faces,
        // so - exactly as the boot is - each is traced ONCE from an audited reference face and reused:
        // per-card tracing measured wrong on two of them (Seismic Slam prints a grey shield whose
        // outline window also holds the boot's top; Epicentre's swords traced as a sliver).
        private static SlotTrace threeBadgeShieldTemplate;
        private static SlotTrace threeBadgeSwordsTemplate;

        public static string SetThreeBadgeTemplates(byte[] rgba)
        {
            var shield = TraceRail(rgba, true, -1);
            var swords = TraceRailWindow(rgba, ThreeBadgeSwordsY0, ThreeBadgeSwordsY1, "rail-band", -1);
            if (shield.Status != "ok" || swords.Status != "ok")
            {
                return "three-badge templates: shield " + shield.Status + ", swords " + swords.Status;
            }

            threeBadgeShieldTemplate = shield;
            threeBadgeSwordsTemplate = swords;
            return "ok three-badge templates " + shield.Area + "/" + swords.Area;
        }

        public static SlotTrace TraceThreeBadgeShield()
        {
            return CloneTemplate(threeBadgeShieldTemplate, "rail-three-badge-shield");
        }

        public static SlotTrace TraceThreeBadgeSwords()
        {
            return CloneTemplate(threeBadgeSwordsTemplate, "rail-three-badge-swords");
        }

        private static SlotTrace CloneTemplate(SlotTrace template, string slot)
        {
            if (template == null)
            {
                return new SlotTrace { Slot = slot, Status = "missing-three-badge-template" };
            }

            return new SlotTrace
            {
                Slot = slot,
                Status = "ok-template",
                X0 = template.X0, Y0 = template.Y0, X1 = template.X1, Y1 = template.Y1,
                Area = template.Area,
                Sprite = (byte[])template.Sprite.Clone()
            };
        }

        // The audited boot, moved to where a three-badge rail prints it: the same glyph and the same
        // bytes, translated whole, so the canonical-silhouette check still holds for the shape.
        public static SlotTrace TraceMovementRailShifted(int dy)
        {
            var template = TraceMovementRail();
            if (template.Status != "ok-template")
            {
                return template;
            }

            var shifted = new byte[template.Sprite.Length];
            int stride = CardWidth * 4;
            for (int y = 0; y < CardHeight; y++)
            {
                int from = y - dy;
                if (from < 0 || from >= CardHeight)
                {
                    continue;
                }

                Buffer.BlockCopy(template.Sprite, from * stride, shifted, y * stride, stride);
            }

            template.Sprite = shifted;
            template.Y0 += dy;
            template.Y1 += dy;
            template.Slot = "rail-three-badge-boot";
            return template;
        }

        private static SlotTrace TraceRailWindow(byte[] rgba, int y0, int y1, string slotName, int bannerDistance)
        {
            int bannerR, bannerG, bannerB;
            SampleAverage(rgba, 22, 108, 66, 120, out bannerR, out bannerG, out bannerB);
            int bannerSat = Math.Max(bannerR, Math.Max(bannerG, bannerB)) -
                Math.Min(bannerR, Math.Min(bannerG, bannerB));
            int bannerLum = (bannerR + bannerG + bannerB) / 3;

            int w = RailX1 - RailX0 + 1;
            int h = y1 - y0 + 1;
            var mask = new byte[w * h];
            bool greyBanner = bannerSat < 25;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // The banner's own pale left-edge line reads as far from the cloth as icon
                    // ink does and traced into a bracket riding every gold rail badge - the box
                    // and vertical glow line the user circled on screen. No badge ink lives in
                    // the first columns, so they are simply out of bounds.
                    if (x < 9)
                    {
                        continue;
                    }

                    int r, g, b, lum, sat, max, min;
                    Classify(rgba, x + RailX0, y + y0, out r, out g, out b, out lum, out sat, out max, out min);
                    bool icon;
                    if (bannerDistance < 0)
                    {
                        icon = max < 80 || (min > 195 && sat < 25);
                    }
                    else if (greyBanner)
                    {
                        icon = Math.Abs(lum - bannerLum) > 55 || max < 70;
                    }
                    else
                    {
                        int dr = r - bannerR, dg = g - bannerG, db = b - bannerB;
                        icon = Math.Sqrt(dr * dr + dg * dg + db * db) > bannerDistance || max < 80;
                    }

                    if (icon)
                    {
                        mask[y * w + x] = 1;
                    }
                }
            }

            RemoveSmallFourConnected(mask, w, h, 40);
            var closed = Morph(mask, w, h, 2, true);
            closed = Morph(closed, w, h, 2, false);
            var component = LargestComponent(closed, w, h, int.MaxValue, 20, 64, 20, 80);
            if (component == null)
            {
                return new SlotTrace { Slot = slotName, Status = "none" };
            }

            FillHoles(component, w, h);
            return Finish(rgba, slotName, component, w, h, RailX0, y0, 250, 9000);
        }

        // ---- Glyph family templates (voted from hand-audited clean per-card traces). ----
        //
        // The badge glyphs are the same artwork on every card - only position, ink colour and
        // the value digit vary - so the badge solid is NOT re-derived per card. Clean per-card
        // traces of one family (audited on a sprite-only contact sheet) vote a canonical family
        // mask; each card then anchors that mask to its own panel geometry and unions its own
        // printed digit back in. Pixel classification still finds evidence, but it can no
        // longer invent a shape: the banner arc that shares the glyph's hue merged into roughly
        // a third of the pure per-card traces, and on dark or same-hue art the trace collapsed
        // into the boxes and blobs the user rejected on screen.

        private static byte[] TryTraceComponent(
            byte[] rgba, string color, int panelTop, int bottomReach, int deepX0, int deepX1,
            int hueMaxX, int darkCapX, bool deepMetal,
            out int zoneX0, out int zoneY0, out int zoneW, out int zoneH)
        {
            int zx1 = 104;
            zoneX0 = 2;
            int zy1 = Math.Min(CardHeight - 1, panelTop + bottomReach);
            zoneY0 = Math.Max(264, panelTop - 92);
            zoneW = zx1 - zoneX0 + 1;
            zoneH = zy1 - zoneY0 + 1;

            // A reach past the panel line needs the relaxed metal class: the shield's point,
            // the book's tail and the swords' handles are silver AT OR INSIDE the panel line,
            // where the positional metal block would erase them (that block is why templates
            // used to end in the flat bar or guessed bottom the user rejected). The deep
            // window is per family: handles splay wide and shallow, points and tails sit
            // narrow and deep, and the panel's own corner curve and frame shadow live just
            // outside each window.
            bool relaxedMetal = bottomReach > 6;

            // Pass A reads all ink classes; on dark art the dark class can flood, which the
            // density and edge checks reject, and pass B retries without it.
            var component = TraceComponentPass(
                rgba, zoneX0, zoneY0, zoneW, zoneH, panelTop, true, 3, color, relaxedMetal,
                deepX0, deepX1, hueMaxX, darkCapX, deepMetal);
            if (component == null)
            {
                component = TraceComponentPass(
                    rgba, zoneX0, zoneY0, zoneW, zoneH, panelTop, false, 4, color, relaxedMetal,
                    deepX0, deepX1, hueMaxX, darkCapX, deepMetal);
            }

            return component;
        }

        private static byte[] TraceComponentPass(
            byte[] rgba, int zx0, int zy0, int zw, int zh, int panelTop,
            bool includeDark, int closePasses, string color, bool relaxedMetal,
            int deepX0, int deepX1, int hueMaxX, int darkCapX, bool deepMetal)
        {
            var mask = BuildPrimaryInkMask(
                rgba, zx0, zy0, zw, zh, panelTop, includeDark, color, relaxedMetal,
                deepX0, deepX1, hueMaxX, darkCapX, deepMetal);

            // The transparency checker's squares touch only diagonally, so the 4-connected
            // cleanup removes the lattice while the badge's genuinely connected ink survives.
            RemoveSmallFourConnected(mask, zw, zh, 45);
            var closed = Morph(mask, zw, zh, closePasses, true);
            closed = Morph(closed, zw, zh, closePasses, false);
            FillHoles(closed, zw, zh);
            var component = LargestComponent(closed, zw, zh, int.MaxValue, 20, 96, 20, 84);
            if (component == null)
            {
                return null;
            }

            FillHoles(component, zw, zh);
            int area = 0, minX = zw, minY = zh, maxX = -1, maxY = -1;
            for (int y = 0; y < zh; y++)
            {
                for (int x = 0; x < zw; x++)
                {
                    if (component[y * zw + x] != 1)
                    {
                        continue;
                    }

                    area++;
                    if (x < minX) { minX = x; }
                    if (x > maxX) { maxX = x; }
                    if (y < minY) { minY = y; }
                    if (y > maxY) { maxY = y; }
                }
            }

            var density = (float)area / ((maxX - minX + 1) * (maxY - minY + 1));
            if (area < 550 || area > 5600 || density > 0.86f)
            {
                return null;
            }

            // A dark-art blob is a piece of the zone, so it reaches the zone's edges; a real
            // badge floats inside them.
            var touches = 0;
            if (minX <= 1) { touches++; }
            if (maxX >= zw - 2) { touches++; }
            if (minY <= 1) { touches++; }
            if (maxY >= zh - 2) { touches++; }
            if (touches >= 3 || maxX - minX + 1 >= zw - 6 || maxY - minY + 1 >= zh - 6)
            {
                return null;
            }

            return component;
        }

        // The panel's banner arc bottom-aligns with the glyphs and merges into their traces at
        // the panel corner, so it reaches a template as a wedge on the right. The profile of
        // topmost set pixels dips deepest at the glyph-arc junction - the valley - and the cut
        // clears everything from the deepest valley rightward. A template without a wedge has
        // no deep valley and is left whole.
        private static byte[] CutWingColumns(byte[] solid, int tw, int th)
        {
            int valleyX = -1, valleyDepth = -1;
            for (int x = tw / 2; x < tw; x++)
            {
                int topmost = -1;
                for (int y = 0; y < th; y++)
                {
                    if (solid[y * tw + x] == 1)
                    {
                        topmost = y;
                        break;
                    }
                }

                if (topmost >= 0 && topmost > valleyDepth)
                {
                    valleyDepth = topmost;
                    valleyX = x;
                }
            }

            if (valleyX < 0 || valleyDepth < (int)(th * 0.55))
            {
                return solid;
            }

            var cut = (byte[])solid.Clone();
            for (int x = valleyX; x < tw; x++)
            {
                for (int y = 0; y < th; y++)
                {
                    cut[y * tw + x] = 0;
                }
            }

            var main = LargestComponent(cut, tw, th, int.MaxValue, 12, 110, 12, 100);
            if (main != null)
            {
                cut = main;
            }

            FillHoles(cut, tw, th);
            return cut;
        }

        // Pure printed white inside a window: value digits, a digit's +, a Block shield's
        // exclamation mark. The ribbon's white edging is the same white but thin, and the
        // parchment and checker lights are tinted; the strict threshold plus the thinness cut
        // keeps digits and drops both.
        private static byte[] BuildDigitWhite(byte[] rgba, int wx0, int wy0, int ww, int wh, int panelTop)
        {
            var white = new byte[ww * wh];
            for (int y = 0; y < wh; y++)
            {
                if (wy0 + y > panelTop + 1)
                {
                    continue;
                }

                for (int x = 0; x < ww; x++)
                {
                    int r, g, b, lum, sat, max, min;
                    Classify(rgba, x + wx0, y + wy0, out r, out g, out b, out lum, out sat, out max, out min);
                    if (min > 200 && sat < 18)
                    {
                        white[y * ww + x] = 1;
                    }
                }
            }

            RemoveThinComponents(white, ww, wh, 3);
            return white;
        }

        // Keeps 4-connected components that lie mostly within (a slightly dilated) reference
        // mask and inside the size band - the digit test: digits sit on their glyph, checker
        // cells and border runs do not.
        private static byte[] KeepComponentsMostlyInside(
            byte[] candidates, byte[] reference, int w, int h,
            double minFraction, int minArea, int maxArea, int maxWidth)
        {
            var refWide = Morph(reference, w, h, 2, true);
            var labels = new int[w * h];
            var queue = new Queue<int>();
            var keep = new byte[w * h];
            int next = 0;
            for (int start = 0; start < w * h; start++)
            {
                if (candidates[start] != 1 || labels[start] != 0)
                {
                    continue;
                }

                next++;
                labels[start] = next;
                queue.Enqueue(start);
                var pixels = new List<int>();
                int inside = 0, minX = w, maxX = -1;
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    pixels.Add(index);
                    if (refWide[index] == 1)
                    {
                        inside++;
                    }

                    int y = index / w, x = index % w;
                    if (x < minX) { minX = x; }
                    if (x > maxX) { maxX = x; }
                    if (x > 0) { TryLabel(candidates, labels, queue, index - 1, next); }
                    if (x < w - 1) { TryLabel(candidates, labels, queue, index + 1, next); }
                    if (y > 0) { TryLabel(candidates, labels, queue, index - w, next); }
                    if (y < h - 1) { TryLabel(candidates, labels, queue, index + w, next); }
                }

                if (pixels.Count >= minArea && pixels.Count <= maxArea &&
                    maxX - minX + 1 <= maxWidth &&
                    inside >= (int)Math.Ceiling(pixels.Count * minFraction))
                {
                    for (int i = 0; i < pixels.Count; i++)
                    {
                        keep[pixels[i]] = 1;
                    }
                }
            }

            return keep;
        }

        private static void TryLabel(byte[] mask, int[] labels, Queue<int> queue, int index, int label)
        {
            if (mask[index] == 1 && labels[index] == 0)
            {
                labels[index] = label;
                queue.Enqueue(index);
            }
        }

        public static string AddTemplateMember(byte[] rgba, string group, string color)
        {
            int panelTop = FindPanelTop(rgba);
            if (panelTop < 0)
            {
                return "no-panel";
            }

            // Only the shield's point, the book's tail and the swords' handles genuinely
            // cross the panel line, each over clean parchment or banner; the fan and boot
            // reaching that deep only find printed shadow greys, so their zones stop where
            // they used to. Measured anatomy (tmp/8.30d-shine-design/shock-zoom.png): handles
            // splay x8..94 riding ON the red banner arc - grey on red, so metal is the
            // discriminator, the hue class stops at x=70 (blades never pass it; everything
            // red beyond IS banner) and dark caps at 74. Point and tail sit x32..66 and deep.
            // The boot's sole arcs below the panel line, and the user's markup also claims
            // the heel spur at the far left and the toe tip at the right - so the boot's deep
            // window spans heel spur to toe tip.
            int bottomReach = group == "Book" ? 24 : group == "Shield" ? 20 : group == "Attack" ? 10 :
                group == "Boot" ? 14 : 4;
            int deepX0 = group == "Attack" ? 8 : group == "Boot" ? 14 : 32;
            int deepX1 = group == "Attack" ? 94 : group == "Boot" ? 77 : 66;
            int hueMaxX = group == "Attack" ? 70 : 999;
            int darkCapX = group == "Attack" ? 74 : 999;
            bool deepMetal = group != "Boot";
            int zx0, zy0, zw, zh;
            var component = TryTraceComponent(
                rgba, color, panelTop, bottomReach, deepX0, deepX1, hueMaxX, darkCapX, deepMetal,
                out zx0, out zy0, out zw, out zh);
            if (component == null)
            {
                return "no-trace";
            }

            // The swords' handles are silver so BRIGHT (lum 184-253, probed on wasp/shock)
            // that they cross out of the metal band into the positionally-blocked white class,
            // which is why no general pass ever caught them and the template bottom looked
            // guessed. They get their own sweep: grey of ANY brightness inside the measured
            // handle band (x8..94, above the panel line), thinness-filtered so the banner's
            // white edging drops, closed into the badge.
            if (group == "Attack")
            {
                var handles = new byte[zw * zh];
                var darkHandle = new byte[zw * zh];
                for (int y = 0; y < zh; y++)
                {
                    int cardY = zy0 + y;
                    if (cardY < panelTop - 38 || cardY > panelTop - 1)
                    {
                        continue;
                    }

                    for (int x = 0; x < zw; x++)
                    {
                        int cardX = zx0 + x;
                        if (cardX < 8 || cardX > 94)
                        {
                            continue;
                        }

                        int r, g, b, lum, sat, max, min;
                        Classify(rgba, cardX, cardY, out r, out g, out b, out lum, out sat, out max, out min);
                        // The left handle is lit silver; the right one is shaded nearly black
                        // against the red banner, so on the right dark ink is the handle - in
                        // a band tight enough to exclude the arc's curl above it and its
                        // under-shadow line below.
                        if (sat < 25 && lum >= 95)
                        {
                            handles[y * zw + x] = 1;
                        }
                        else if (cardX >= 70 && cardX <= 90 && max < 60 &&
                            cardY >= panelTop - 30 && cardY <= panelTop - 6)
                        {
                            darkHandle[y * zw + x] = 1;
                        }
                    }
                }

                RemoveThinComponents(handles, zw, zh, 3);
                RemoveThinComponents(darkHandle, zw, zh, 4);
                for (int i = 0; i < zw * zh; i++)
                {
                    if (darkHandle[i] == 1)
                    {
                        handles[i] = 1;
                    }
                }
                int handleCount = 0;
                for (int i = 0; i < zw * zh; i++)
                {
                    if (handles[i] == 1)
                    {
                        handleCount++;
                    }

                    if (component[i] == 1)
                    {
                        handles[i] = 1;
                    }
                }

                Console.Error.WriteLine("attack handle sweep: panelTop=" + panelTop + " greyPixels=" + handleCount);

                handles = Morph(handles, zw, zh, 2, true);
                handles = Morph(handles, zw, zh, 2, false);
                FillHoles(handles, zw, zh);
                var handled = LargestComponent(handles, zw, zh, int.MaxValue, 12, 100, 12, 90);
                if (handled != null)
                {
                    FillHoles(handled, zw, zh);
                    component = handled;
                }
            }

            // Where a glyph meets the banner arc with no dip in the silhouette, no profile
            // finds the boundary; where its sheen approaches the cloth colour, neither does
            // colour. Those boundaries are curated anatomy constants instead, measured at 4x
            // zoom on the prepared 372x520 fronts (tmp/8.30d-shine-design/*-zoom.png): the
            // boot artwork ends at card x=75 (xargatha/charm - 74 clipped the toe tip the
            // user marked, 77 overshot in front of it), the book's right pages at x=86
            // (wasp/static-barrier - the user caught the last page tip missing at 84).
            int rightEdge = group == "Boot" ? 75 : group == "Book" ? 86 : 0;
            if (rightEdge > 0)
            {
                // The boot also gets a hard floor at the panel line: the border's own dark
                // line runs beneath the sole, and once both are traced the hole-fill floods
                // the parchment pocket between them - the below-the-heel spill the user
                // marked. The sole's curve rides the border, so panelTop+3 keeps all of it.
                int bottomEdge = group == "Boot" ? panelTop + 3 : CardHeight;
                for (int y = 0; y < zh; y++)
                {
                    bool belowFloor = zy0 + y > bottomEdge;
                    for (int x = 0; x < zw; x++)
                    {
                        if (zx0 + x > rightEdge || belowFloor)
                        {
                            component[y * zw + x] = 0;
                        }
                    }
                }

                var edgeMain = LargestComponent(component, zw, zh, int.MaxValue, 12, 96, 12, 90);
                if (edgeMain != null)
                {
                    component = edgeMain;
                }

                FillHoles(component, zw, zh);
            }

            // A family whose glyph carries a value digit votes WITHOUT the digits: each card's
            // own digit is re-unioned at emit time, so a 7 shines as a 7 and a 5+ keeps its +.
            if (group == "Attack" || group == "Boot")
            {
                var white = BuildDigitWhite(rgba, zx0, zy0, zw, zh, panelTop);
                var digits = KeepComponentsMostlyInside(white, component, zw, zh, 0.40, 12, 460, 36);
                var digitsWide = Morph(digits, zw, zh, 1, true);
                for (int i = 0; i < zw * zh; i++)
                {
                    if (digitsWide[i] == 1)
                    {
                        component[i] = 0;
                    }
                }

                // A gentle close: just enough to heal the digit cavity, not enough to smear
                // the blades and handles around it.
                component = Morph(component, zw, zh, 2, true);
                component = Morph(component, zw, zh, 2, false);
                FillHoles(component, zw, zh);
                var main = LargestComponent(component, zw, zh, int.MaxValue, 12, 96, 12, 90);
                if (main != null)
                {
                    FillHoles(main, zw, zh);
                    component = main;
                }
            }

            int minX = zw, minY = zh, maxX = -1, maxY = -1;
            for (int y = 0; y < zh; y++)
            {
                for (int x = 0; x < zw; x++)
                {
                    if (component[y * zw + x] != 1)
                    {
                        continue;
                    }

                    if (x < minX) { minX = x; }
                    if (x > maxX) { maxX = x; }
                    if (y < minY) { minY = y; }
                    if (y > maxY) { maxY = y; }
                }
            }

            if (maxX < 0)
            {
                return "empty";
            }

            List<TemplateMember> members;
            if (!PendingMembers.TryGetValue(group, out members))
            {
                members = new List<TemplateMember>();
                PendingMembers[group] = members;
            }

            members.Add(new TemplateMember
            {
                Solid = component,
                W = zw, H = zh,
                X0 = minX, Y0 = minY, X1 = maxX, Y1 = maxY,
                AbsX0 = zx0 + minX, AbsY0 = zy0 + minY, AbsY1 = zy0 + maxY,
                PanelTop = panelTop
            });

            return string.Format(
                "ok w={0} h={1} x0={2} dy={3}",
                maxX - minX + 1, maxY - minY + 1, zx0 + minX, zy0 + minY - panelTop);
        }

        public static string FinalizeTemplates()
        {
            Templates.Clear();
            var lines = new List<string>();
            foreach (var pair in PendingMembers)
            {
                var members = pair.Value;
                const int canvasW = 132, canvasH = 120, margin = 10;
                // Members align at bottom-left: every family's bottom edge is solid artwork
                // (hilt, book base, fan cup, sole, shield point) while the tops are thin
                // spikes whose traced tips jitter per card - aligning there loses them.
                //
                // Only the smooth shield genuinely denoises under a majority vote. Detailed
                // families (book burst, fan rays, boot, the swords' thin blades and handles -
                // the vote mushed those into the guessed-looking bottom the user rejected)
                // take their shape from the FIRST whitelisted member alone, one good example
                // moved around, and the other members still contribute position statistics.
                bool seeded = pair.Key != "Shield";
                var voters = seeded ? members.GetRange(0, 1) : members;
                var votes = new int[canvasW * canvasH];
                for (int m = 0; m < voters.Count; m++)
                {
                    var member = voters[m];
                    for (int y = member.Y0; y <= member.Y1; y++)
                    {
                        for (int x = member.X0; x <= member.X1; x++)
                        {
                            if (member.Solid[y * member.W + x] != 1)
                            {
                                continue;
                            }

                            int cx = margin + x - member.X0;
                            int cy = (canvasH - margin) - (member.Y1 - y);
                            if (cx < canvasW && cy >= 0 && cy < canvasH)
                            {
                                votes[cy * canvasW + cx]++;
                            }
                        }
                    }
                }

                int threshold = (voters.Count + 1) / 2;
                var voted = new byte[canvasW * canvasH];
                for (int i = 0; i < votes.Length; i++)
                {
                    voted[i] = (byte)(votes[i] >= threshold ? 1 : 0);
                }

                var main = LargestComponent(voted, canvasW, canvasH, int.MaxValue, 12, 110, 12, 100);
                if (main == null)
                {
                    lines.Add(pair.Key + ": DEGENERATE VOTE from " + members.Count + " members");
                    continue;
                }

                FillHoles(main, canvasW, canvasH);
                int minX = canvasW, minY = canvasH, maxX = -1, maxY = -1;
                for (int y = 0; y < canvasH; y++)
                {
                    for (int x = 0; x < canvasW; x++)
                    {
                        if (main[y * canvasW + x] != 1)
                        {
                            continue;
                        }

                        if (x < minX) { minX = x; }
                        if (x > maxX) { maxX = x; }
                        if (y < minY) { minY = y; }
                        if (y > maxY) { maxY = y; }
                    }
                }

                int tw = maxX - minX + 1, th = maxY - minY + 1;
                var solid = new byte[tw * th];
                for (int y = 0; y < th; y++)
                {
                    for (int x = 0; x < tw; x++)
                    {
                        solid[y * tw + x] = main[(y + minY) * canvasW + (x + minX)];
                    }
                }

                // The fan and shield reach the panel's banner arc and get the profile cut
                // (self-gating: no deep valley, no cut); the boot and the book were already
                // cut per member at their measured edges (their own low structures - toe and
                // tail - are what the profile rule would amputate), and the swords float
                // clear of the arc.
                if (pair.Key == "Fan" || pair.Key == "Shield")
                {
                    solid = CutWingColumns(solid, tw, th);
                }

                int minX2 = tw, minY2 = th, maxX2 = -1, maxY2 = -1;
                for (int y = 0; y < th; y++)
                {
                    for (int x = 0; x < tw; x++)
                    {
                        if (solid[y * tw + x] != 1)
                        {
                            continue;
                        }

                        if (x < minX2) { minX2 = x; }
                        if (x > maxX2) { maxX2 = x; }
                        if (y < minY2) { minY2 = y; }
                        if (y > maxY2) { maxY2 = y; }
                    }
                }

                if (maxX2 < 0)
                {
                    lines.Add(pair.Key + ": EMPTY AFTER WING CUT");
                    continue;
                }

                int tw2 = maxX2 - minX2 + 1, th2 = maxY2 - minY2 + 1;
                var trimmed = new byte[tw2 * th2];
                for (int y = 0; y < th2; y++)
                {
                    for (int x = 0; x < tw2; x++)
                    {
                        trimmed[y * tw2 + x] = solid[(y + minY2) * tw + (x + minX2)];
                    }
                }

                solid = trimmed;
                tw = tw2;
                th = th2;

                // Anchors follow the trimmed origin. Each member's bbox bottom row sat at
                // canvas row (canvasH - margin) and its bbox left at column margin, so the
                // template origin corresponds to member card coordinates shifted by the trims.
                var xs = new List<int>();
                var dys = new List<int>();
                for (int m = 0; m < members.Count; m++)
                {
                    xs.Add(members[m].AbsX0 + (minX + minX2 - margin));
                    dys.Add(members[m].AbsY1 - (canvasH - margin) + (minY + minY2) - members[m].PanelTop);
                }

                var template = new GlyphTemplate
                {
                    Group = pair.Key,
                    Solid = solid,
                    W = tw, H = th,
                    AnchorX = Median(xs),
                    AnchorDy = Median(dys),
                    Members = members.Count
                };
                Templates[pair.Key] = template;
                lines.Add(string.Format(
                    "{0}: members={1} template={2}x{3} anchorX={4} anchorDy={5}",
                    pair.Key, members.Count, tw, th, template.AnchorX, template.AnchorDy));
            }

            return string.Join(Environment.NewLine, lines.ToArray());
        }

        private static int Median(List<int> values)
        {
            var sorted = new List<int>(values);
            sorted.Sort();
            return sorted[sorted.Count / 2];
        }

        public static byte[] TemplatePng(string group)
        {
            GlyphTemplate template;
            if (!Templates.TryGetValue(group, out template))
            {
                return null;
            }

            var rgba = new byte[template.W * template.H * 4];
            for (int i = 0; i < template.W * template.H; i++)
            {
                if (template.Solid[i] != 1)
                {
                    continue;
                }

                rgba[i * 4] = 255;
                rgba[i * 4 + 1] = 255;
                rgba[i * 4 + 2] = 255;
                rgba[i * 4 + 3] = 255;
            }

            using (var bitmap = new Bitmap(template.W, template.H, PixelFormat.Format32bppArgb))
            {
                var data = bitmap.LockBits(
                    new Rectangle(0, 0, template.W, template.H),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(rgba, 0, data.Scan0, rgba.Length);
                bitmap.UnlockBits(data);
                using (var stream = new System.IO.MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }

        // ---- Primary badge: the family template anchored to this card's panel geometry. ----

        public static SlotTrace TracePrimaryTemplated(byte[] rgba, string group, string color)
        {
            int panelTop = FindPanelTop(rgba);
            if (panelTop < 0)
            {
                return new SlotTrace { Slot = "primary", Status = "no-panel" };
            }

            GlyphTemplate template;
            if (!Templates.TryGetValue(group, out template))
            {
                return new SlotTrace { Slot = "primary", Status = "no-template-" + group };
            }

            // Layout anchor first, evidence second: a bounded local search may only refine the
            // panel-derived position, never leave it - the unbounded "refinement" of an earlier
            // iteration dragged stamps onto checkered art.
            int anchorX = template.AnchorX;
            int anchorY = panelTop + template.AnchorDy;
            int searchX = 14;
            int searchY = group == "Shield" ? 12 : 6;

            int wx0 = Math.Max(0, anchorX - searchX - 2);
            int wy0 = Math.Max(0, anchorY - searchY - 2);
            int wx1 = Math.Min(CardWidth - 1, anchorX + template.W - 1 + searchX + 2);
            int wy1 = Math.Min(CardHeight - 1, anchorY + template.H - 1 + searchY + 2);
            int ww = wx1 - wx0 + 1, wh = wy1 - wy0 + 1;
            var digitWhite = BuildDigitWhite(rgba, wx0, wy0, ww, wh, panelTop);
            var hueLoose = new byte[ww * wh];
            var outlineDark = new byte[ww * wh];
            var parchment = new byte[ww * wh];
            for (int y = 0; y < wh; y++)
            {
                int cardY = wy0 + y;
                for (int x = 0; x < ww; x++)
                {
                    int r, g, b, lum, sat, max, min;
                    Classify(rgba, x + wx0, cardY, out r, out g, out b, out lum, out sat, out max, out min);
                    // Loose by design: this only SCORES candidate offsets, it never traces.
                    bool hue;
                    switch (color)
                    {
                        case "Gold": hue = r > g + 8 && g > b + 10 && r > 95; break;
                        case "Red": hue = r > g + 25 && r > b + 25 && r > 85; break;
                        case "Green": hue = g > r + 8 && g > b + 8 && g > 55; break;
                        case "Blue": hue = b > r + 8 && b > g + 8 && b > 60; break;
                        case "Silver": hue = sat < 28 && lum >= 85 && lum <= 205; break;
                        default: hue = sat >= 90 && max >= 120; break;
                    }

                    if (hue) { hueLoose[y * ww + x] = 1; }
                    if (max < 95 && cardY <= panelTop + 2) { outlineDark[y * ww + x] = 1; }
                    if (min > 170 && sat < 30 && cardY > panelTop - 3) { parchment[y * ww + x] = 1; }
                }
            }

            var bandOuter = Morph(template.Solid, template.W, template.H, 1, true);
            var bandInner = Morph(template.Solid, template.W, template.H, 2, false);

            // Score = digit white and glyph hue under the solid, dark outline under the solid's
            // edge band, minus parchment under the solid (the badge overlaps the panel corner
            // only barely; sinking into the panel is always wrong).
            int bestScore = int.MinValue, bestDx = 0, bestDy = 0;
            for (int dy = -searchY; dy <= searchY; dy++)
            {
                for (int dx = -searchX; dx <= searchX; dx++)
                {
                    int px0 = anchorX + dx, py0 = anchorY + dy;
                    if (px0 < 0 || py0 < 0 ||
                        px0 + template.W > CardWidth || py0 + template.H > CardHeight)
                    {
                        continue;
                    }

                    int score = 0;
                    for (int y = 0; y < template.H; y++)
                    {
                        int ey = py0 + y - wy0;
                        if (ey < 0 || ey >= wh)
                        {
                            continue;
                        }

                        for (int x = 0; x < template.W; x++)
                        {
                            int ex = px0 + x - wx0;
                            if (ex < 0 || ex >= ww)
                            {
                                continue;
                            }

                            int ti = y * template.W + x, ei = ey * ww + ex;
                            if (template.Solid[ti] == 1)
                            {
                                if (digitWhite[ei] == 1) { score += 3; }
                                if (hueLoose[ei] == 1) { score += 1; }
                                if (parchment[ei] == 1) { score -= 1; }
                            }

                            if (bandOuter[ti] == 1 && bandInner[ti] == 0 && outlineDark[ei] == 1)
                            {
                                score += 2;
                            }
                        }
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDx = dx;
                        bestDy = dy;
                    }
                }
            }

            int fx0 = anchorX + bestDx, fy0 = anchorY + bestDy;
            var placed = new byte[CardWidth * CardHeight];
            for (int y = 0; y < template.H; y++)
            {
                for (int x = 0; x < template.W; x++)
                {
                    if (template.Solid[y * template.W + x] == 1)
                    {
                        placed[(fy0 + y) * CardWidth + (fx0 + x)] = 1;
                    }
                }
            }

            // This card's own digit joins the family solid, clipped to the badge's close
            // surroundings so checker lights and border runs stay out.
            if (group == "Attack" || group == "Boot" || group == "Shield")
            {
                var placedLocal = new byte[ww * wh];
                for (int y = 0; y < wh; y++)
                {
                    for (int x = 0; x < ww; x++)
                    {
                        if (placed[(wy0 + y) * CardWidth + (wx0 + x)] == 1)
                        {
                            placedLocal[y * ww + x] = 1;
                        }
                    }
                }

                var clip = new byte[ww * wh];
                int cx0 = Math.Max(0, fx0 - wx0 - 6);
                int cx1 = Math.Min(ww - 1, fx0 - wx0 + template.W - 1 + 18);
                int cy0 = Math.Max(0, fy0 - wy0 - 16);
                int cy1 = Math.Min(wh - 1, fy0 - wy0 + template.H - 1 + 4);
                for (int y = cy0; y <= cy1; y++)
                {
                    for (int x = cx0; x <= cx1; x++)
                    {
                        clip[y * ww + x] = digitWhite[y * ww + x];
                    }
                }

                var digits = KeepComponentsMostlyInside(clip, placedLocal, ww, wh, 0.25, 12, 460, 36);
                for (int y = 0; y < wh; y++)
                {
                    for (int x = 0; x < ww; x++)
                    {
                        if (digits[y * ww + x] == 1)
                        {
                            placed[(wy0 + y) * CardWidth + (wx0 + x)] = 1;
                        }
                    }
                }
            }

            var closed = Morph(placed, CardWidth, CardHeight, 2, true);
            closed = Morph(closed, CardWidth, CardHeight, 2, false);
            FillHoles(closed, CardWidth, CardHeight);
            var main = LargestComponent(closed, CardWidth, CardHeight, int.MaxValue, 16, 112, 16, 100);
            if (main != null)
            {
                FillHoles(main, CardWidth, CardHeight);
                closed = main;
            }

            var finished = Finish(rgba, "primary", closed, CardWidth, CardHeight, 0, 0, 550, 7000);
            if (finished.Status == "ok")
            {
                finished.Status = "ok-template";
            }

            return finished;
        }

        // ---- The primary-ring sprite: the card's OWN printed lines, lit. ----
        //
        // The user's final direction for an offered primary: invent no lines - glow the
        // existing ones, "the box around the skill type and around the text". The ring is the
        // union of two printed outlines, each traced from this card: the parchment panel's
        // border, and the type ribbon's border.

        public static SlotTrace TracePrimaryRing(byte[] rgba)
        {
            int panelTop = FindPanelTop(rgba);
            if (panelTop < 0)
            {
                return new SlotTrace { Slot = "primary-ring", Status = "no-panel" };
            }

            var parchment = BuildParchmentSolid(rgba, panelTop);
            if (parchment == null)
            {
                return new SlotTrace { Slot = "primary-ring", Status = "no-parchment" };
            }

            var mask = BandOf(parchment);
            var ribbon = TraceTypeRibbon(rgba, panelTop, parchment);
            if (ribbon != null)
            {
                var ribbonBand = BandOf(ribbon, 1);
                for (int i = 0; i < mask.Length; i++)
                {
                    if (ribbonBand[i] == 1)
                    {
                        mask[i] = 1;
                    }
                }
            }

            var finished = Finish(
                rgba, "primary-ring", mask, CardWidth, CardHeight, 0, 0,
                900, 16000, primaryRing: true);
            if (finished.Status == "ok")
            {
                finished.Status = "ok-ring";
            }

            return finished;
        }

        // The parchment panel, traced whole (text and headers become interior holes and are
        // filled). Returns null when the panel cannot be isolated.
        private static byte[] BuildParchmentSolid(byte[] rgba, int panelTop)
        {
            var panel = new byte[CardWidth * CardHeight];
            int py0 = Math.Max(0, panelTop - 28), py1 = CardHeight - 12;
            for (int y = py0; y <= py1; y++)
            {
                for (int x = 8; x <= 364; x++)
                {
                    int r, g, b, lum, sat, max, min;
                    Classify(rgba, x, y, out r, out g, out b, out lum, out sat, out max, out min);
                    if (min > 165 && sat < 32)
                    {
                        panel[y * CardWidth + x] = 1;
                    }
                }
            }

            var main = LargestComponent(panel, CardWidth, CardHeight, int.MaxValue, 200, 372, 60, 240);
            if (main == null)
            {
                return null;
            }

            FillHoles(main, CardWidth, CardHeight);
            // A tinted strip under a tall ribbon can fail the parchment class and pull the
            // region top down onto the first text line; the closing swallows such gaps so the
            // border band follows the printed box, not the print inside it.
            main = Morph(main, CardWidth, CardHeight, 4, true);
            main = Morph(main, CardWidth, CardHeight, 4, false);
            FillHoles(main, CardWidth, CardHeight);
            return main;
        }

        // The type ribbon, found by flooding from the panel-top centre - a point that is
        // inside the ribbon's cloth on every prepared front - out to its printed dark border,
        // which is then worn as part of the solid so the band straddles the printed line.
        private static byte[] TraceTypeRibbon(byte[] rgba, int panelTop, byte[] parchment)
        {
            int wx0 = 36, wx1 = 336;
            int wy0 = Math.Max(0, panelTop - 26), wy1 = Math.Min(CardHeight - 1, panelTop + 20);
            int ww = wx1 - wx0 + 1, wh = wy1 - wy0 + 1;

            // On silver cards the ribbon cloth is itself parchment-coloured, so colour alone
            // lets the flood leak into the panel. The parchment region's own top boundary IS
            // the ribbon's printed lower border, so the flood may never descend into it.
            var parchmentTop = new int[CardWidth];
            for (int x = 0; x < CardWidth; x++)
            {
                parchmentTop[x] = CardHeight;
                for (int y = 0; y < CardHeight; y++)
                {
                    if (parchment[y * CardWidth + x] == 1)
                    {
                        parchmentTop[x] = y;
                        break;
                    }
                }
            }

            var solid = new byte[ww * wh];
            var seeds = new[] { 150, 186, 222 };
            foreach (var seedX in seeds)
            {
                int sr, sg, sb, slum, ssat, smax, smin;
                Classify(rgba, seedX, panelTop - 4, out sr, out sg, out sb, out slum, out ssat, out smax, out smin);
                if (smax < 70)
                {
                    continue; // the seed hit border ink or art; another seed will land on cloth
                }

                var queue = new Queue<int>();
                int start = (panelTop - 4 - wy0) * ww + (seedX - wx0);
                if (solid[start] == 1)
                {
                    continue;
                }

                solid[start] = 1;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    int y = index / ww, x = index % ww;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int ny = y + dy, nx = x + dx;
                            if (ny < 0 || ny >= wh || nx < 0 || nx >= ww)
                            {
                                continue;
                            }

                            int ni = ny * ww + nx;
                            if (solid[ni] == 1)
                            {
                                continue;
                            }

                            if (ny + wy0 >= parchmentTop[nx + wx0] + 1)
                            {
                                continue; // never descend into the panel the ribbon sits on
                            }

                            int r, g, b, lum, sat, max, min;
                            Classify(rgba, nx + wx0, ny + wy0, out r, out g, out b, out lum, out sat, out max, out min);
                            if (max < 70)
                            {
                                continue; // the printed border bounds the flood
                            }

                            int dr = r - sr, dg = g - sg, db = b - sb;
                            if (Math.Sqrt(dr * dr + dg * dg + db * db) >= 60)
                            {
                                continue;
                            }

                            solid[ni] = 1;
                            queue.Enqueue(ni);
                        }
                    }
                }
            }

            // Wear the printed dark border, then close and fill so the label text and the
            // border become part of one ribbon solid.
            var wide = Morph(solid, ww, wh, 2, true);
            for (int y = 0; y < wh; y++)
            {
                for (int x = 0; x < ww; x++)
                {
                    if (wide[y * ww + x] != 1 || solid[y * ww + x] == 1)
                    {
                        continue;
                    }

                    int r, g, b, lum, sat, max, min;
                    Classify(rgba, x + wx0, y + wy0, out r, out g, out b, out lum, out sat, out max, out min);
                    if (max < 70)
                    {
                        solid[y * ww + x] = 1;
                    }
                }
            }

            var closed = Morph(solid, ww, wh, 2, true);
            closed = Morph(closed, ww, wh, 2, false);
            FillHoles(closed, ww, wh);
            var main = LargestComponent(closed, ww, wh, int.MaxValue, 80, 300, 14, 46);
            if (main == null)
            {
                return null;
            }

            FillHoles(main, ww, wh);
            int area = 0;
            var card = new byte[CardWidth * CardHeight];
            for (int y = 0; y < wh; y++)
            {
                for (int x = 0; x < ww; x++)
                {
                    if (main[y * ww + x] == 1)
                    {
                        card[(y + wy0) * CardWidth + (x + wx0)] = 1;
                        area++;
                    }
                }
            }

            return area >= 900 && area <= 9000 ? card : null;
        }

        // A thin band straddling a solid's outline: its printed line, ready to light.
        private static byte[] BandOf(byte[] solid, int passes = 2)
        {
            var outer = Morph(solid, CardWidth, CardHeight, passes, true);
            var inner = Morph(solid, CardWidth, CardHeight, passes, false);
            var band = new byte[CardWidth * CardHeight];
            for (int i = 0; i < band.Length; i++)
            {
                band[i] = (byte)(outer[i] == 1 && inner[i] == 0 ? 1 : 0);
            }

            return band;
        }

        // The primary badge's ink classes over one zone: dark outline, white digit, vivid glyph
        // fill, silver metalwork. White and metal are blocked beside the panel frame, whose light
        // line would merge into the badge's bottom edge - and white THIN structures are dropped
        // entirely, because the printed ribbon's white edging is the same white as a digit but
        // never as thick: every "ghost frame" artifact was that edging closed into the badge.
        private static byte[] BuildPrimaryInkMask(
            byte[] rgba, int zx0, int zy0, int zw, int zh, int panelTop, bool includeDark, string color,
            bool relaxedMetal, int deepX0, int deepX1, int hueMaxX, int darkCapX, bool deepMetal)
        {
            var mask = new byte[zw * zh];
            var white = new byte[zw * zh];
            var metalDeep = new byte[zw * zh];
            for (int y = 0; y < zh; y++)
            {
                bool nearFrame = (zy0 + y) > panelTop - 4;
                for (int x = 0; x < zw; x++)
                {
                    // The frame's rounded corner climbs well above the panel line on the right,
                    // so the white/metal block extends higher there; a digit's + suffix sits
                    // higher still and is never lost to it.
                    bool nearCorner = nearFrame ||
                        (x + zx0 > 66 && (zy0 + y) > panelTop - 20);
                    int r, g, b, lum, sat, max, min;
                    Classify(rgba, x + zx0, y + zy0, out r, out g, out b, out lum, out sat, out max, out min);
                    // The frame's dark shadow line runs right of the glyph's cup, and its
                    // rounded corner curls dark at the far left; the glyph's own deep ink
                    // (shield point, book tail, sword handles) lives between the group's deep
                    // bounds - outside a relaxed trace, the classic 26..60 window stands. The
                    // all-rows cap keeps the banner arc's dark shadow line out where a glyph
                    // (the swords) legitimately overlaps the arc itself.
                    int darkLo = relaxedMetal ? deepX0 : 26;
                    int darkHi = relaxedMetal ? Math.Min(deepX1, darkCapX) : 60;
                    bool dark = includeDark && max < 75 && x + zx0 <= darkCapX &&
                        !(nearFrame && (x + zx0 > darkHi || x + zx0 < darkLo));
                    // The glyph's fill is inked in the card's own printed colour - a typed fact
                    // the manifest carries - so the fill class is keyed to that hue by channel
                    // dominance, which separates even a dark blue burst from neutral black art.
                    bool hue;
                    switch (color)
                    {
                        case "Gold": hue = r > g + 15 && g > b + 25 && r > 120; break;
                        case "Red": hue = r > g + 45 && r > b + 45 && r > 110; break;
                        case "Green": hue = g > r + 12 && g > b + 12 && g > 70; break;
                        case "Blue": hue = b > r + 12 && b > g + 14 && b > 70; break;
                        default: hue = sat >= 110 && max >= 140; break;
                    }

                    bool metalBand = sat < 22 && lum >= 95 && lum <= 195;
                    // Where a glyph's grey handles ride ON the banner arc (the swords), the
                    // arc shares the card's printed hue - so the hue class stops where the
                    // glyph's own coloured ink verifiably ends and metal takes over.
                    if (x + zx0 > hueMaxX)
                    {
                        hue = false;
                    }

                    bool metal = metalBand && !nearCorner;
                    if (dark || hue || metal)
                    {
                        mask[y * zw + x] = 1;
                    }

                    // Relaxed: metal near and below the panel line goes through a thinness
                    // filter instead of the positional block - the panel border's shadow is a
                    // 2-3px line and drops, while a shield's point, a book's tail and a
                    // sword's handles are fat. Bounded to the group's deep window: the panel's
                    // rounded corner curve is fat too, but it lives outside it. The boot skips
                    // this class entirely - its deep ink is a dark-outlined enclosed sole, and
                    // the grey this class would add there is the boot's printed drop SHADOW,
                    // which pushed the highlight below the heel and past the toe.
                    if (deepMetal && relaxedMetal && metalBand && nearCorner &&
                        x + zx0 >= deepX0 && x + zx0 <= deepX1)
                    {
                        metalDeep[y * zw + x] = 1;
                    }

                    if (min > 195 && sat < 22 && !nearCorner)
                    {
                        white[y * zw + x] = 1;
                    }
                }
            }

            RemoveThinComponents(white, zw, zh, 4);
            for (int i = 0; i < zw * zh; i++)
            {
                if (white[i] == 1)
                {
                    mask[i] = 1;
                }
            }

            if (relaxedMetal)
            {
                RemoveThinComponents(metalDeep, zw, zh, 4);
                for (int i = 0; i < zw * zh; i++)
                {
                    if (metalDeep[i] == 1)
                    {
                        mask[i] = 1;
                    }
                }
            }

            return mask;
        }

        // Drops 4-connected components whose thickness never reaches the minimum - lines, not
        // glyph strokes. Thickness is approximated per component as the largest square of set
        // pixels any member anchors, which a 2px ribbon edge cannot reach and a digit stroke can.
        private static void RemoveThinComponents(byte[] mask, int w, int h, int minThickness)
        {
            var labels = new int[w * h];
            var queue = new Queue<int>();
            int nextLabel = 0;
            for (int start = 0; start < w * h; start++)
            {
                if (mask[start] != 1 || labels[start] != 0)
                {
                    continue;
                }

                nextLabel++;
                labels[start] = nextLabel;
                queue.Enqueue(start);
                var members = new List<int>();
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    members.Add(index);
                    int y = index / w, x = index % w;
                    int[] nys = { y - 1, y + 1, y, y };
                    int[] nxs = { x, x, x - 1, x + 1 };
                    for (int k = 0; k < 4; k++)
                    {
                        int ny = nys[k], nx = nxs[k];
                        if (ny < 0 || ny >= h || nx < 0 || nx >= w)
                        {
                            continue;
                        }

                        int ni = ny * w + nx;
                        if (mask[ni] == 1 && labels[ni] == 0)
                        {
                            labels[ni] = nextLabel;
                            queue.Enqueue(ni);
                        }
                    }
                }

                var thick = false;
                foreach (int member in members)
                {
                    int y = member / w, x = member % w;
                    if (y + minThickness > h || x + minThickness > w)
                    {
                        continue;
                    }

                    var square = true;
                    for (int dy = 0; dy < minThickness && square; dy++)
                    {
                        for (int dx = 0; dx < minThickness; dx++)
                        {
                            if (mask[(y + dy) * w + (x + dx)] != 1)
                            {
                                square = false;
                                break;
                            }
                        }
                    }

                    if (square)
                    {
                        thick = true;
                        break;
                    }
                }

                if (!thick)
                {
                    foreach (int member in members)
                    {
                        mask[member] = 0;
                    }
                }
            }
        }

        // ---- Shared morphology and composition. ----

        private static void SampleAverage(
            byte[] rgba, int x0, int y0, int x1, int y1, out int r, out int g, out int b)
        {
            long sr = 0, sg = 0, sb = 0;
            int n = 0;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int i = (y * CardWidth + x) * 4;
                    sb += rgba[i]; sg += rgba[i + 1]; sr += rgba[i + 2];
                    n++;
                }
            }

            r = (int)(sr / n); g = (int)(sg / n); b = (int)(sb / n);
        }

        private static SlotTrace Finish(
            byte[] rgba, string slot, byte[] solid, int w, int h,
            int offsetX, int offsetY, int minArea, int maxArea,
            bool thin = false, bool primaryRing = false)
        {
            int area = 0, minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (solid[y * w + x] != 1)
                    {
                        continue;
                    }

                    area++;
                    if (x < minX) { minX = x; }
                    if (x > maxX) { maxX = x; }
                    if (y < minY) { minY = y; }
                    if (y > maxY) { maxY = y; }
                }
            }

            if (area < minArea || area > maxArea)
            {
                return new SlotTrace
                {
                    Slot = slot,
                    Status = "degenerate-" + area,
                    X0 = minX + offsetX, Y0 = minY + offsetY,
                    X1 = maxX + offsetX, Y1 = maxY + offsetY,
                    Area = area
                };
            }

            var sprite = primaryRing
                ? ComposePrimaryRing(solid, w, h, offsetX, offsetY)
                : thin
                    ? ComposeThin(solid, w, h, offsetX, offsetY)
                    : Compose(solid, w, h, offsetX, offsetY);
            return new SlotTrace
            {
                Slot = slot,
                Status = "ok",
                X0 = minX + offsetX, Y0 = minY + offsetY,
                X1 = maxX + offsetX, Y1 = maxY + offsetY,
                Area = area,
                Sprite = sprite
            };
        }

        private static byte[] Morph(byte[] mask, int w, int h, int passes, bool dilate)
        {
            var current = (byte[])mask.Clone();
            var next = new byte[w * h];
            for (int p = 0; p < passes; p++)
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        bool any = false, all = true;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int ny = y + dy, nx = x + dx;
                                bool value = ny >= 0 && ny < h && nx >= 0 && nx < w &&
                                    current[ny * w + nx] == 1;
                                if (value) { any = true; } else { all = false; }
                            }
                        }

                        next[y * w + x] = (byte)((dilate ? any : all) ? 1 : 0);
                    }
                }

                var swap = current; current = next; next = swap;
            }

            return current;
        }

        private static void RemoveSmallFourConnected(byte[] mask, int w, int h, int minArea)
        {
            var labels = new int[w * h];
            var queue = new Queue<int>();
            int nextLabel = 0;
            for (int start = 0; start < w * h; start++)
            {
                if (mask[start] != 1 || labels[start] != 0)
                {
                    continue;
                }

                nextLabel++;
                labels[start] = nextLabel;
                queue.Enqueue(start);
                var members = new List<int>();
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    members.Add(index);
                    int y = index / w, x = index % w;
                    int[] nys = { y - 1, y + 1, y, y };
                    int[] nxs = { x, x, x - 1, x + 1 };
                    for (int k = 0; k < 4; k++)
                    {
                        int ny = nys[k], nx = nxs[k];
                        if (ny < 0 || ny >= h || nx < 0 || nx >= w)
                        {
                            continue;
                        }

                        int ni = ny * w + nx;
                        if (mask[ni] == 1 && labels[ni] == 0)
                        {
                            labels[ni] = nextLabel;
                            queue.Enqueue(ni);
                        }
                    }
                }

                if (members.Count < minArea)
                {
                    foreach (int member in members)
                    {
                        mask[member] = 0;
                    }
                }
            }
        }

        private static byte[] LargestComponent(
            byte[] mask, int w, int h, int mustTouchBeforeX,
            int minWidth, int maxWidth, int minHeight, int maxHeight)
        {
            var labels = new int[w * h];
            var queue = new Queue<int>();
            int best = 0, bestArea = 0, nextLabel = 0;
            for (int start = 0; start < w * h; start++)
            {
                if (mask[start] != 1 || labels[start] != 0)
                {
                    continue;
                }

                nextLabel++;
                labels[start] = nextLabel;
                queue.Enqueue(start);
                int area = 0, minX = w, maxX = -1, minY = h, maxY = -1;
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    int y = index / w, x = index % w;
                    area++;
                    if (x < minX) { minX = x; }
                    if (x > maxX) { maxX = x; }
                    if (y < minY) { minY = y; }
                    if (y > maxY) { maxY = y; }
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int ny = y + dy, nx = x + dx;
                            if (ny < 0 || ny >= h || nx < 0 || nx >= w)
                            {
                                continue;
                            }

                            int ni = ny * w + nx;
                            if (mask[ni] == 1 && labels[ni] == 0)
                            {
                                labels[ni] = nextLabel;
                                queue.Enqueue(ni);
                            }
                        }
                    }
                }

                bool touches = minX <= mustTouchBeforeX;
                int width = maxX - minX + 1, height = maxY - minY + 1;
                bool sized = width >= minWidth && width <= maxWidth &&
                    height >= minHeight && height <= maxHeight;
                if (touches && sized && area > bestArea)
                {
                    bestArea = area;
                    best = nextLabel;
                }
            }

            if (best == 0)
            {
                return null;
            }

            var result = new byte[w * h];
            for (int i = 0; i < w * h; i++)
            {
                result[i] = (byte)(labels[i] == best ? 1 : 0);
            }

            return result;
        }

        private static void FillHoles(byte[] mask, int w, int h)
        {
            var outside = new byte[w * h];
            var queue = new Queue<int>();
            for (int x = 0; x < w; x++)
            {
                TryFlood(mask, outside, queue, x);
                TryFlood(mask, outside, queue, (h - 1) * w + x);
            }

            for (int y = 0; y < h; y++)
            {
                TryFlood(mask, outside, queue, y * w);
                TryFlood(mask, outside, queue, y * w + (w - 1));
            }

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int y = index / w, x = index % w;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int ny = y + dy, nx = x + dx;
                        if (ny < 0 || ny >= h || nx < 0 || nx >= w)
                        {
                            continue;
                        }

                        TryFlood(mask, outside, queue, ny * w + nx);
                    }
                }
            }

            for (int i = 0; i < w * h; i++)
            {
                if (mask[i] == 0 && outside[i] == 0)
                {
                    mask[i] = 1;
                }
            }
        }

        private static void TryFlood(byte[] mask, byte[] outside, Queue<int> queue, int index)
        {
            if (index < 0 || index >= mask.Length || mask[index] == 1 || outside[index] == 1)
            {
                return;
            }

            outside[index] = 1;
            queue.Enqueue(index);
        }

        private static byte[] Subtract(byte[] left, byte[] right, int w, int h)
        {
            var result = new byte[w * h];
            for (int i = 0; i < w * h; i++)
            {
                result[i] = (byte)(left[i] == 1 && right[i] == 0 ? 1 : 0);
            }

            return result;
        }

        private static float[] ToFloat(byte[] mask, int w, int h)
        {
            var result = new float[w * h];
            for (int i = 0; i < w * h; i++)
            {
                result[i] = mask[i];
            }

            return result;
        }

        private static float[] Blur(float[] values, int w, int h, int passes)
        {
            var current = values;
            for (int p = 0; p < passes; p++)
            {
                var next = new float[w * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float sum = 0f;
                        int n = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int ny = y + dy, nx = x + dx;
                                if (ny < 0 || ny >= h || nx < 0 || nx >= w)
                                {
                                    continue;
                                }

                                sum += current[ny * w + nx];
                                n++;
                            }
                        }

                        next[y * w + x] = sum / n;
                    }
                }

                current = next;
            }

            return current;
        }

        // The thin bake, for line work: the band itself is the lit line, with only a tight
        // soft edge around it - the badge bake's wide halo would drown the print an existing
        // line sits beside.
        private static byte[] ComposeThin(byte[] solid, int w, int h, int offsetX, int offsetY)
        {
            var coreA = Blur(ToFloat(solid, w, h), w, h, 1);
            var glowA = Blur(ToFloat(Morph(solid, w, h, 2, true), w, h), w, h, 2);

            var sprite = new byte[CardWidth * CardHeight * 4];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int local = y * w + x;
                    float a = 0f, r = 0f, g = 0f, b = 0f;
                    Over(ref a, ref r, ref g, ref b, Math.Min(0.45f, glowA[local] * 0.45f), 255f, 230f, 155f);
                    Over(ref a, ref r, ref g, ref b, Math.Min(1.00f, coreA[local] * 1.55f), 255f, 250f, 226f);
                    if (a <= 0.004f)
                    {
                        continue;
                    }

                    int gx = x + offsetX, gy = y + offsetY;
                    if (gx < 0 || gx >= CardWidth || gy < 0 || gy >= CardHeight)
                    {
                        continue;
                    }

                    int i = (gy * CardWidth + gx) * 4;
                    sprite[i] = (byte)Math.Min(255f, b);
                    sprite[i + 1] = (byte)Math.Min(255f, g);
                    sprite[i + 2] = (byte)Math.Min(255f, r);
                    sprite[i + 3] = (byte)Math.Min(255f, a * 255f);
                }
            }

            return sprite;
        }

        // The Primary's printed-line bake: a crisp core on the existing boxes plus a spatially wider,
        // softer halo. Runtime tint decides the approved graphite-and-steel colour; this sprite owns
        // only light falloff, so increasing its glow cannot invent or move a line.
        private static byte[] ComposePrimaryRing(
            byte[] solid, int w, int h, int offsetX, int offsetY)
        {
            var coreA = Blur(ToFloat(solid, w, h), w, h, 1);
            var glowA = Blur(ToFloat(Morph(solid, w, h, 4, true), w, h), w, h, 4);
            var haloA = Blur(ToFloat(Morph(solid, w, h, 10, true), w, h), w, h, 8);

            var sprite = new byte[CardWidth * CardHeight * 4];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int local = y * w + x;
                    float a = 0f, r = 0f, g = 0f, b = 0f;
                    Over(ref a, ref r, ref g, ref b,
                        Math.Min(0.34f, haloA[local] * 0.34f), 230f, 240f, 255f);
                    Over(ref a, ref r, ref g, ref b,
                        Math.Min(0.62f, glowA[local] * 0.62f), 238f, 246f, 255f);
                    Over(ref a, ref r, ref g, ref b,
                        Math.Min(1.00f, coreA[local] * 1.50f), 252f, 254f, 255f);
                    if (a <= 0.004f)
                    {
                        continue;
                    }

                    int gx = x + offsetX, gy = y + offsetY;
                    if (gx < 0 || gx >= CardWidth || gy < 0 || gy >= CardHeight)
                    {
                        continue;
                    }

                    int i = (gy * CardWidth + gx) * 4;
                    sprite[i] = (byte)Math.Min(255f, b);
                    sprite[i + 1] = (byte)Math.Min(255f, g);
                    sprite[i + 2] = (byte)Math.Min(255f, r);
                    sprite[i + 3] = (byte)Math.Min(255f, a * 255f);
                }
            }

            return sprite;
        }

        private static byte[] Compose(byte[] solid, int w, int h, int offsetX, int offsetY)
        {
            // Secondary badges use the same three-stage light hierarchy as Primary, but their
            // compact printed numbers cannot carry Primary's opaque core without washing into a
            // white blob. Preserve the exact-shape core at half strength and let the warm outer
            // falloff carry the emphasis.
            var coreA = Blur(ToFloat(solid, w, h), w, h, 1);
            var glowA = Blur(ToFloat(Morph(solid, w, h, 4, true), w, h), w, h, 4);
            var haloA = Blur(ToFloat(Morph(solid, w, h, 10, true), w, h), w, h, 8);

            var sprite = new byte[CardWidth * CardHeight * 4];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int local = y * w + x;
                    float a = 0f, r = 0f, g = 0f, b = 0f;
                    Over(ref a, ref r, ref g, ref b,
                        Math.Min(0.24f, haloA[local] * 0.24f), 255f, 225f, 150f);
                    Over(ref a, ref r, ref g, ref b,
                        Math.Min(0.46f, glowA[local] * 0.46f), 255f, 236f, 170f);
                    Over(ref a, ref r, ref g, ref b,
                        Math.Min(0.52f, coreA[local] * 0.68f), 255f, 252f, 232f);

                    if (a <= 0.004f)
                    {
                        continue;
                    }

                    int gx = x + offsetX, gy = y + offsetY;
                    if (gx < 0 || gx >= CardWidth || gy < 0 || gy >= CardHeight)
                    {
                        continue;
                    }

                    int i = (gy * CardWidth + gx) * 4;
                    sprite[i] = (byte)Math.Min(255f, b);
                    sprite[i + 1] = (byte)Math.Min(255f, g);
                    sprite[i + 2] = (byte)Math.Min(255f, r);
                    sprite[i + 3] = (byte)Math.Min(255f, a * 255f);
                }
            }

            return sprite;
        }

        private static void Over(ref float a, ref float r, ref float g, ref float b, float layerA, float lr, float lg, float lb)
        {
            if (layerA <= 0f)
            {
                return;
            }

            float outA = layerA + a * (1f - layerA);
            if (outA <= 0f)
            {
                return;
            }

            r = (lr * layerA + r * a * (1f - layerA)) / outA;
            g = (lg * layerA + g * a * (1f - layerA)) / outA;
            b = (lb * layerA + b * a * (1f - layerA)) / outA;
            a = outA;
        }

        public static void SaveSprite(byte[] rgba, string path)
        {
            using (var bitmap = new Bitmap(CardWidth, CardHeight, PixelFormat.Format32bppArgb))
            {
                var data = bitmap.LockBits(
                    new Rectangle(0, 0, CardWidth, CardHeight),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(rgba, 0, data.Scan0, rgba.Length);
                bitmap.UnlockBits(data);
                bitmap.Save(path, ImageFormat.Png);
            }
        }

        public static byte[] ToBytesPng(byte[] rgba)
        {
            using (var bitmap = new Bitmap(CardWidth, CardHeight, PixelFormat.Format32bppArgb))
            {
                var data = bitmap.LockBits(
                    new Rectangle(0, 0, CardWidth, CardHeight),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(rgba, 0, data.Scan0, rgba.Length);
                bitmap.UnlockBits(data);
                using (var stream = new System.IO.MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }
    }
}
'@

Add-Type -TypeDefinition $tracerSource -ReferencedAssemblies @('System.Drawing')

# Typed manifest: which slots each card owns.
$manifest = @{}
Import-Csv -LiteralPath $ContentManifest | ForEach-Object {
    $manifest[$_.hero + '/' + $_.card] = $_
}

# The glyph family templates. The members are the per-card traces that were audited CLEAN on
# the sprite-only contact sheet (tmp/8.30d-shine-design): they vote the canonical family solid,
# digits removed, and every card's primary is then that solid anchored to its own panel
# geometry with its own digit unioned back - one good example moved around, with voting across
# the good examples only to smooth per-card trace noise.
$templateWhitelist = [ordered]@{
    Attack = @('wasp/shock', 'wasp/thunder-boomerang', 'xargatha/cleave',
        'tigerclaw/blink-strike', 'brogan/onslaught', 'sabina/point-blank-shot')
    Book = @('wasp/static-barrier', 'xargatha/sirens-call', 'tigerclaw/blend-into-shadows',
        'arien/spell-break', 'brogan/bulwark', 'sabina/back-to-back')
    Fan = @('sabina/steady-advance', 'wasp/lift-up', 'wasp/mass-telekinesis',
        'xargatha/petrifying-stare', 'xargatha/devoted-followers', 'xargatha/final-embrace',
        'tigerclaw/pick-pocket', 'tigerclaw/poisoned-dagger', 'tigerclaw/master-thief',
        'tigerclaw/poisoned-dart', 'arien/liquid-leap', 'brogan/mighty-punch',
        'brogan/shield-bash', 'brogan/counterattack', 'brogan/war-drummer', 'sabina/listen-up',
        'sabina/roger-roger', 'sabina/ready-and-waiting', 'sabina/troop-movement',
        'sabina/marching-orders', 'sabina/path-to-victory', 'sabina/covering-fire')
    Boot = @('xargatha/charm')
    Shield = @('tigerclaw/dodge', 'tigerclaw/sidestep', 'tigerclaw/parry', 'tigerclaw/evade',
        'tigerclaw/riposte', 'arien/aspiring-duelist', 'arien/expert-duelist',
        'arien/master-duelist', 'wasp/stop-projectiles')
}

[GuardsDigital.Tools.ShineTracer]::ResetTemplates()
foreach ($templateGroup in $templateWhitelist.Keys) {
    foreach ($member in $templateWhitelist[$templateGroup]) {
        if (-not $manifest.ContainsKey($member)) {
            throw "Template member $member is not in the content manifest."
        }

        $memberPath = Join-Path $cardsRoot ($member.Replace('/', [System.IO.Path]::DirectorySeparatorChar) + '.png')
        $memberRgba = [GuardsDigital.Tools.ShineTracer]::LoadCard($memberPath)
        $memberStatus = [GuardsDigital.Tools.ShineTracer]::AddTemplateMember(
            $memberRgba, $templateGroup, $manifest[$member].color)
        if ($memberStatus -notlike 'ok*') {
            throw "Template member $member ($templateGroup) failed: $memberStatus"
        }
    }
}

Write-Output ([GuardsDigital.Tools.ShineTracer]::FinalizeTemplates())
foreach ($templateGroup in $templateWhitelist.Keys) {
    $templatePng = [GuardsDigital.Tools.ShineTracer]::TemplatePng($templateGroup)
    if ($null -ne $templatePng) {
        $templatePath = [System.IO.Path]::Combine(
            $projectRoot, 'tmp', 'shine-template-' + $templateGroup.ToString().ToLower() + '.png')
        [System.IO.File]::WriteAllBytes($templatePath, $templatePng)
    }
}

$movementRailReference = [System.IO.Path]::Combine(
    $cardsRoot, 'sabina', 'point-blank-shot.png')
$movementRailStatus = [GuardsDigital.Tools.ShineTracer]::SetMovementRailTemplate(
    [GuardsDigital.Tools.ShineTracer]::LoadCard($movementRailReference))
if ($movementRailStatus -notlike 'ok*') {
    throw "Movement rail template failed: $movementRailStatus"
}
Write-Output $movementRailStatus

# Milestone 8.112g. The three-badge rail's shield and swords, from the audited Seismic Assault face.
$threeBadgeReference = [System.IO.Path]::Combine($cardsRoot, 'mrak', 'seismic-assault.png')
if (Test-Path -LiteralPath $threeBadgeReference) {
    $threeBadgeStatus = [GuardsDigital.Tools.ShineTracer]::SetThreeBadgeTemplates(
        [GuardsDigital.Tools.ShineTracer]::LoadCard($threeBadgeReference))
    if ($threeBadgeStatus -notlike 'ok*') {
        throw "Three-badge rail templates failed: $threeBadgeStatus"
    }

    Write-Output $threeBadgeStatus
}

$reportRows = New-Object System.Collections.Generic.List[string]
$reportRows.Add('hero,card,slot,status,x0,y0,x1,y1,area,file')
$written = 0; $skipped = 0; $missing = 0; $stale = 0; $stray = 0; $traced = 0; $failed = 0

Get-ChildItem -Path $cardsRoot -Directory | Sort-Object Name | ForEach-Object {
    $heroDir = $_
    $hero = $heroDir.Name
    $shineDir = Join-Path $heroDir.FullName '_shine'
    $expected = @{}
    Get-ChildItem -Path $heroDir.FullName -Filter '*.png' | Sort-Object Name | ForEach-Object {
        $card = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
        $key = "$hero/$card"
        if (-not $manifest.ContainsKey($key)) {
            Write-Output "UNMANIFESTED $key"
            $failed++
            return
        }

        $typed = $manifest[$key]
        if ($typed.ultimate -eq '1') {
            return
        }

        $rgba = [GuardsDigital.Tools.ShineTracer]::LoadCard($_.FullName)
        $slots = New-Object System.Collections.Generic.List[object]

        $hasDefense = $typed.railChannels -match 'Defense'
        $hasMovement = $typed.railChannels -match 'Movement'
        $hasAttack = $typed.railChannels -match 'Attack'
        if ($hasAttack -and $hasDefense -and $hasMovement) {
            # Milestone 8.112g. The three-badge rail (Mrak's Seismic faces): shield traced from the
            # card, the audited boot moved up to its tighter position, the swords traced in the band
            # below it.
            $slots.Add(@{ name = 'secondary-defense'; trace = [GuardsDigital.Tools.ShineTracer]::TraceThreeBadgeShield() })
            $slots.Add(@{ name = 'secondary-movement'; trace = [GuardsDigital.Tools.ShineTracer]::TraceMovementRailShifted([GuardsDigital.Tools.ShineTracer]::ThreeBadgeBootShift) })
            $slots.Add(@{ name = 'secondary-attack'; trace = [GuardsDigital.Tools.ShineTracer]::TraceThreeBadgeSwords() })
        }
        elseif ($hasAttack -and $hasDefense) {
            # Milestone 8.112g. Shield above swords where no boot is printed (Stone Carapace, Rock
            # Solid): both traced from the card, the swords in the ordinary lower window.
            $slots.Add(@{ name = 'secondary-defense'; trace = [GuardsDigital.Tools.ShineTracer]::TraceRailOutline($rgba, $true) })
            $slots.Add(@{ name = 'secondary-attack'; trace = [GuardsDigital.Tools.ShineTracer]::TraceRailOutline($rgba, $false) })
        }
        elseif ($hasDefense -and $hasMovement) {
            $slots.Add(@{ name = 'secondary-defense'; trace = [GuardsDigital.Tools.ShineTracer]::TraceRail($rgba, $true) })
            $slots.Add(@{ name = 'secondary-movement'; trace = [GuardsDigital.Tools.ShineTracer]::TraceMovementRail() })
        }
        elseif ($hasDefense) {
            $slots.Add(@{ name = 'secondary-defense'; trace = [GuardsDigital.Tools.ShineTracer]::TraceRail($rgba, $false) })
        }
        elseif ($hasMovement) {
            $slots.Add(@{ name = 'secondary-movement'; trace = [GuardsDigital.Tools.ShineTracer]::TraceMovementRail() })
        }

        if ($typed.primaryKind -ne 'None') {
            # Printed title position follows this card's measured parchment-panel top.
            # The title's upper centre edge is 32 source pixels above the body anchor
            # on the shared 372x520 print template. Export geometry, never runtime pixels.
            $titleTop = [GuardsDigital.Tools.ShineTracer]::FindPanelTop($rgba) - 32
            if ($titleTop -lt 260 -or $titleTop -gt 450) {
                throw "Invalid primary title anchor for $key`: $titleTop"
            }
            $titlePath = Join-Path $shineDir "$card--primary-title.txt"
            $titleText = $titleTop.ToString([Globalization.CultureInfo]::InvariantCulture)
            if ($Check) {
                if (-not (Test-Path -LiteralPath $titlePath)) { $missing++; Write-Output "MISSING $titlePath" }
                elseif ([IO.File]::ReadAllText($titlePath).Trim() -ne $titleText) {
                    $stale++; Write-Output "STALE $titlePath"
                }
            }
            else {
                if (-not (Test-Path -LiteralPath $shineDir)) { New-Item -ItemType Directory -Path $shineDir | Out-Null }
                [IO.File]::WriteAllText($titlePath, $titleText)
            }
            # Typed kind + colour pick the glyph family: silver Skills print the book burst,
            # coloured Skills the fan, Movement the boot, and every Defense primary - Block or
            # valued - the same shield artwork.
            $group = $typed.primaryKind
            if ($group -eq 'Skill') {
                if ($typed.color -eq 'Silver') { $group = 'Book' } else { $group = 'Fan' }
            }
            elseif ($group -eq 'Movement') { $group = 'Boot' }
            elseif ($group -eq 'Defense') { $group = 'Shield' }
            $slots.Add(@{ name = 'primary'; trace = [GuardsDigital.Tools.ShineTracer]::TracePrimaryTemplated($rgba, $group, $typed.color) })
            # The user's final primary treatment: the card's own printed lines - the type
            # ribbon's box and the text panel's box - lit as one ring sprite. The badge sprite
            # above stays for the Defense prompt, which lights the shield itself.
            $slots.Add(@{ name = 'primary-ring'; trace = [GuardsDigital.Tools.ShineTracer]::TracePrimaryRing($rgba) })
        }

        foreach ($slot in $slots) {
            $traced++
            $trace = $slot.trace
            $fileName = "$card--$($slot.name).png"
            $target = Join-Path $shineDir $fileName
            $row = "$hero,$card,$($slot.name),$($trace.Status),$($trace.X0),$($trace.Y0),$($trace.X1),$($trace.Y1),$($trace.Area),"
            if (@('ok', 'ok-template', 'ok-ring') -notcontains $trace.Status) {
                $reportRows.Add($row)
                Write-Output "FAILED $key/$($slot.name): $($trace.Status)"
                $failed++
                continue
            }

            $expected[$fileName] = $true
            $reportRows.Add($row + $fileName)
            if ($Check) {
                if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
                    Write-Output "MISSING $hero/_shine/$fileName"
                    $missing++
                }
                else {
                    $fresh = [GuardsDigital.Tools.ShineTracer]::ToBytesPng($trace.Sprite)
                    $existing = [System.IO.File]::ReadAllBytes($target)
                    if ($fresh.Length -ne $existing.Length -or
                        [System.Linq.Enumerable]::SequenceEqual($fresh, $existing) -eq $false) {
                        Write-Output "STALE $hero/_shine/$fileName"
                        $stale++
                    }
                }
                continue
            }

            if (-not (Test-Path -LiteralPath $shineDir -PathType Container)) {
                New-Item -ItemType Directory -Path $shineDir | Out-Null
            }

            if ((Test-Path -LiteralPath $target -PathType Leaf) -and -not $Force) {
                $skipped++
                continue
            }

            [GuardsDigital.Tools.ShineTracer]::SaveSprite($trace.Sprite, $target)
            $written++
        }
    }

    if (Test-Path -LiteralPath $shineDir -PathType Container) {
        Get-ChildItem -Path $shineDir -Filter '*.png' | ForEach-Object {
            if (-not $expected.ContainsKey($_.Name)) {
                if ($Check) {
                    Write-Output "STRAY $hero/_shine/$($_.Name)"
                    $stray++
                }
                else {
                    Remove-Item -LiteralPath $_.FullName -Force
                    Write-Output "REMOVED stray $hero/_shine/$($_.Name)"
                }
            }
        }
    }
}

$reportDir = [System.IO.Path]::GetDirectoryName($Report)
if (-not (Test-Path -LiteralPath $reportDir -PathType Container)) {
    New-Item -ItemType Directory -Path $reportDir | Out-Null
}

[System.IO.File]::WriteAllLines($Report, $reportRows)
if ($Check) {
    Write-Output ("Check complete: {0} slots traced, {1} failed, {2} missing, {3} stale, {4} stray. Report: {5}" -f $traced, $failed, $missing, $stale, $stray, $Report)
    if ($failed -gt 0 -or $missing -gt 0 -or $stale -gt 0 -or $stray -gt 0) {
        throw 'Shine sprite check failed.'
    }
}
else {
    Write-Output ("Prepared shine sprites: {0} written, {1} already present, {2} failed, {3} slots traced. Report: {4}" -f $written, $skipped, $failed, $traced, $Report)
    if ($failed -gt 0) { throw 'Shine sprite preparation failed.' }
}
