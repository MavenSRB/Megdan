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
    (verified over all 108 fronts). Pixels only decide geometry. Rail badges are traced from each
    card directly; the primary badge - whose surroundings vary from checkered transparency to
    near-black art - is placed from a curated high-contrast donor template per action kind,
    anchored to the card's own panel top, with the Attack value digit re-traced per card because
    it is the one part of a badge silhouette that varies. Each overlay bakes the approved steady
    shine (silhouette rim, close radiance, wide halo, mild ink lift). The target directory is
    ignored by Git and the runtime degrades gracefully when a sprite is absent. The command does
    not invoke Git, Unity, a build, or a network client.

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

        public sealed class DonorTemplate
        {
            public string Kind;
            public byte[] Solid;     // full-card mask
            public int PanelTop;
            public int BoxX0, BoxY0, BoxX1, BoxY1;
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
            }

            // Tolerant sustained run: the panel body is light for a long stretch, but a dense bold
            // text row can dip under the per-row line, so up to four of twenty-five rows may miss.
            for (int y = PrimY0; y <= PrimY1; y++)
            {
                int hits = 0;
                for (int k = 0; k < 25 && y + k < CardHeight; k++)
                {
                    if (lightRows[y + k])
                    {
                        hits++;
                    }
                }

                if (hits >= 21 && lightRows[y])
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

        // bannerDistance -1 selects the outline-and-digit fallback mask.

        private static SlotTrace TraceRail(byte[] rgba, bool upper, int bannerDistance)
        {
            int y0 = upper ? UpperY0 : LowerY0;
            int y1 = upper ? UpperY1 : LowerY1;
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
                return new SlotTrace { Slot = upper ? "rail-upper" : "rail-lower", Status = "none" };
            }

            FillHoles(component, w, h);
            return Finish(rgba, upper ? "rail-upper" : "rail-lower", component, w, h, RailX0, y0, 250, 9000);
        }

        // ---- Donor templates (curated per action kind, hue-separable from their own cards). ----

        // Ink classes a donor mask may allow. Each is deliberately tight: a donor is a curated
        // card whose badge inks separate cleanly from its own art, and a loose class is exactly
        // what turned every donor into its whole search window once.
        public const int InkDark = 1, InkWhite = 2, InkMetal = 4, InkVividBlue = 8, InkVividGreen = 16;

        // A donor badge is traced by flood-growth from hand-placed seeds inside its cartoon
        // regions - each grown region stops at the badge's own dark outline - then wrapped in
        // that outline and closed. No global colour class survives contact with 108 different
        // arts; a seeded donor never has to.
        public static DonorTemplate BuildDonor(
            string kind, string donorPath, int wx0, int wy0, int wx1, int wy1, int[] seedXs, int[] seedYs, int outlineMax, int tolerance)
        {
            var rgba = LoadCard(donorPath);
            int w = wx1 - wx0 + 1;
            int h = wy1 - wy0 + 1;
            var union = new byte[w * h];
            for (int s = 0; s < seedXs.Length; s++)
            {
                GrowRegion(rgba, union, wx0, wy0, w, h, seedXs[s], seedYs[s], outlineMax, tolerance);
            }

            // Wrap the grown fills in their own dark outline, then close the composite.
            var outline = new byte[w * h];
            var grownDilated = Morph(union, w, h, 2, true);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (grownDilated[y * w + x] != 1)
                    {
                        continue;
                    }

                    int r, g, b, lum, sat, max, min;
                    Classify(rgba, x + wx0, y + wy0, out r, out g, out b, out lum, out sat, out max, out min);
                    if (max < 95)
                    {
                        outline[y * w + x] = 1;
                    }
                }
            }

            for (int i = 0; i < w * h; i++)
            {
                if (outline[i] == 1)
                {
                    union[i] = 1;
                }
            }

            var closed = Morph(union, w, h, 2, true);
            closed = Morph(closed, w, h, 2, false);
            FillHoles(closed, w, h);
            var component = LargestComponent(closed, w, h, int.MaxValue, 20, 92, 20, 92);
            if (component == null)
            {
                throw new InvalidOperationException("Donor trace failed for " + kind + " at " + donorPath);
            }

            FillHoles(component, w, h);

            var solid = new byte[CardWidth * CardHeight];
            int minX = CardWidth, minY = CardHeight, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (component[y * w + x] != 1)
                    {
                        continue;
                    }

                    int gx = x + wx0, gy = y + wy0;
                    solid[gy * CardWidth + gx] = 1;
                    if (gx < minX) { minX = gx; }
                    if (gx > maxX) { maxX = gx; }
                    if (gy < minY) { minY = gy; }
                    if (gy > maxY) { maxY = gy; }
                }
            }

            int panelTop = FindPanelTop(rgba);
            if (panelTop < 0)
            {
                throw new InvalidOperationException("Donor panel top not found for " + kind);
            }

            return new DonorTemplate
            {
                Kind = kind,
                Solid = solid,
                PanelTop = panelTop,
                BoxX0 = minX, BoxY0 = minY, BoxX1 = maxX, BoxY1 = maxY
            };
        }

        private static void GrowRegion(
            byte[] rgba, byte[] union, int wx0, int wy0, int w, int h, int seedX, int seedY, int outlineMax, int tolerance)
        {
            int sx = seedX - wx0, sy = seedY - wy0;
            if (sx < 0 || sx >= w || sy < 0 || sy >= h)
            {
                throw new InvalidOperationException("Donor seed outside its window.");
            }

            int sr, sg, sb, slum, ssat, smax, smin;
            Classify(rgba, seedX, seedY, out sr, out sg, out sb, out slum, out ssat, out smax, out smin);
            var visited = new byte[w * h];
            var queue = new Queue<int>();
            visited[sy * w + sx] = 1;
            queue.Enqueue(sy * w + sx);
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                union[index] = 1;
                int y = index / w, x = index % w;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int ny = y + dy, nx = x + dx;
                        if (ny < 0 || ny >= h || nx < 0 || nx >= w || visited[ny * w + nx] == 1)
                        {
                            continue;
                        }

                        int r, g, b, lum, sat, max, min;
                        Classify(rgba, nx + wx0, ny + wy0, out r, out g, out b, out lum, out sat, out max, out min);
                        if (max < outlineMax)
                        {
                            continue; // the outline bounds every grown region
                        }

                        int distance = Math.Abs(r - sr) + Math.Abs(g - sg) + Math.Abs(b - sb);
                        if (distance >= tolerance)
                        {
                            continue;
                        }

                        visited[ny * w + nx] = 1;
                        queue.Enqueue(ny * w + nx);
                    }
                }
            }
        }

        // ---- Primary badge: donor placement anchored to this card's panel top. ----

        public static SlotTrace TracePrimary(byte[] rgba, DonorTemplate donor, bool adaptAttackDigit)
        {
            int panelTop = FindPanelTop(rgba);
            if (panelTop < 0)
            {
                return new SlotTrace { Slot = "primary", Status = "no-panel" };
            }

            // The badge rides its card's panel top; the anchor IS the placement. An earlier
            // dark-overlap "refinement" was worse than none: it scored checkered art above the
            // panel higher than the badge's own position and dragged every template up-left,
            // including on the donor's own card.
            int anchorDy = panelTop - donor.PanelTop;
            int bestDx = 0, bestDy = anchorDy;

            var placed = new byte[CardWidth * CardHeight];
            for (int y = donor.BoxY0; y <= donor.BoxY1; y++)
            {
                for (int x = donor.BoxX0; x <= donor.BoxX1; x++)
                {
                    if (donor.Solid[y * CardWidth + x] != 1)
                    {
                        continue;
                    }

                    int cx = x + bestDx, cy = y + bestDy;
                    if (cx >= 0 && cx < CardWidth && cy >= 0 && cy < CardHeight)
                    {
                        placed[cy * CardWidth + cx] = 1;
                    }
                }
            }

            if (adaptAttackDigit)
            {
                // The Attack value digit (and its +) is the one badge part that varies per card.
                // Re-trace it in the digit zone: the badge box's upper-right, extended past it.
                int zx0 = donor.BoxX0 + bestDx + (donor.BoxX1 - donor.BoxX0) / 3;
                int zx1 = Math.Min(CardWidth - 1, donor.BoxX1 + bestDx + 18);
                int zy0 = Math.Max(0, donor.BoxY0 + bestDy - 18);
                // A value digit is printed above the frame, never on the panel: clamping the zone
                // to the card's own panel top is what keeps the frame's white out of the digit.
                int zy1 = Math.Min(
                    donor.BoxY0 + bestDy + (2 * (donor.BoxY1 - donor.BoxY0)) / 3,
                    panelTop - 1);
                if (zy1 <= zy0)
                {
                    zy1 = zy0;
                }

                int zw = zx1 - zx0 + 1, zh = zy1 - zy0 + 1;
                var digit = new byte[zw * zh];
                for (int y = 0; y < zh; y++)
                {
                    for (int x = 0; x < zw; x++)
                    {
                        int r, g, b, lum, sat, max, min;
                        Classify(rgba, x + zx0, y + zy0, out r, out g, out b, out lum, out sat, out max, out min);
                        if ((min > 195 && sat < 22) || max < 75)
                        {
                            digit[y * zw + x] = 1;
                        }
                    }
                }

                RemoveSmallFourConnected(digit, zw, zh, 30);
                var closedDigit = Morph(digit, zw, zh, 2, true);
                closedDigit = Morph(closedDigit, zw, zh, 2, false);
                for (int y = 0; y < zh; y++)
                {
                    for (int x = 0; x < zw; x++)
                    {
                        if (closedDigit[y * zw + x] == 1)
                        {
                            placed[(y + zy0) * CardWidth + (x + zx0)] = 1;
                        }
                    }
                }

                FillHoles(placed, CardWidth, CardHeight);
            }

            return Finish(rgba, "primary", placed, CardWidth, CardHeight, 0, 0, 700, 14000);
        }

        private static byte[] Erode2(byte[] mask)
        {
            var eroded = Morph(mask, CardWidth, CardHeight, 2, false);
            return eroded;
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
            int offsetX, int offsetY, int minArea, int maxArea)
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

            var sprite = Compose(solid, w, h, offsetX, offsetY);
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

        private static byte[] Compose(byte[] solid, int w, int h, int offsetX, int offsetY)
        {
            var rim = Subtract(Morph(solid, w, h, 3, true), solid, w, h);
            var rimA = Blur(ToFloat(rim, w, h), w, h, 1);
            var glowA = Blur(ToFloat(Morph(solid, w, h, 5, true), w, h), w, h, 3);
            var haloA = Blur(ToFloat(Morph(solid, w, h, 9, true), w, h), w, h, 6);

            var sprite = new byte[CardWidth * CardHeight * 4];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int local = y * w + x;
                    float a = 0f, r = 0f, g = 0f, b = 0f;
                    // Hotter than the source mock on purpose: at HUD card scale (roughly 46% of
                    // the 372x520 source) the softer numbers read as nothing, and the user's bar
                    // is "really POP, obvious to the player".
                    Over(ref a, ref r, ref g, ref b, Math.Min(0.58f, haloA[local] * 0.58f), 255f, 225f, 150f);
                    Over(ref a, ref r, ref g, ref b, Math.Min(0.78f, glowA[local] * 0.78f), 255f, 236f, 170f);
                    Over(ref a, ref r, ref g, ref b, Math.Min(1.00f, rimA[local] * 1.60f), 255f, 252f, 232f);
                    if (solid[local] == 1)
                    {
                        a = 0.18f; r = 255f; g = 248f; b = 225f;
                    }

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

# Curated donors: one clean source per primary action kind, traced by flood-growth from
# hand-placed seeds inside the badge's own cartoon regions. Windows stay badge-tight and the
# seeds sit on the badge's fills (blade, digit, cup, shield body, rim), never on art.
$donorSpecs = @(
    @{ kind = 'Attack';   card = 'wasp/magnetic-dagger';   x0 = 2;  y0 = 322; x1 = 84; y1 = 388; om = 75
       sx = [int[]]@(16, 64, 38, 12, 68, 40); sy = [int[]]@(338, 340, 342, 370, 372, 362) },
    @{ kind = 'Skill';    card = 'brogan/bolster';         x0 = 3;  y0 = 316; x1 = 58; y1 = 380; om = 75
       sx = [int[]]@(27, 27, 14, 40); sy = [int[]]@(335, 369, 345, 345) },
    @{ kind = 'Movement'; card = 'xargatha/control';       x0 = 4;  y0 = 308; x1 = 64; y1 = 378; om = 42; tol = 95
       sx = [int[]]@(32, 16, 30, 46, 12, 22, 40); sy = [int[]]@(332, 346, 358, 320, 362, 364, 366) },
    @{ kind = 'Defense';  card = 'arien/aspiring-duelist'; x0 = 24; y0 = 352; x1 = 68; y1 = 414; om = 75
       sx = [int[]]@(44, 45, 45, 44, 30, 58); sy = [int[]]@(362, 375, 385, 404, 372, 372) },
    @{ kind = 'Block';    card = 'tigerclaw/dodge';        x0 = 24; y0 = 346; x1 = 70; y1 = 412; om = 60
       sx = [int[]]@(46, 46, 47, 46, 32, 60); sy = [int[]]@(356, 368, 380, 402, 370, 370) }
)

$donors = @{}
foreach ($spec in $donorSpecs) {
    $path = Join-Path $cardsRoot ($spec.card.Replace('/', [System.IO.Path]::DirectorySeparatorChar) + '.png')
    $tolerance = 130
    if ($spec.ContainsKey('tol')) { $tolerance = $spec.tol }
    $donors[$spec.kind] = [GuardsDigital.Tools.ShineTracer]::BuildDonor(
        $spec.kind, $path, $spec.x0, $spec.y0, $spec.x1, $spec.y1, $spec.sx, $spec.sy, $spec.om, $tolerance)
}

# Typed manifest: which slots each card owns.
$manifest = @{}
Import-Csv -LiteralPath $ContentManifest | ForEach-Object {
    $manifest[$_.hero + '/' + $_.card] = $_
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
        if ($hasDefense -and $hasMovement) {
            $slots.Add(@{ name = 'secondary-defense'; trace = [GuardsDigital.Tools.ShineTracer]::TraceRail($rgba, $true) })
            $slots.Add(@{ name = 'secondary-movement'; trace = [GuardsDigital.Tools.ShineTracer]::TraceRail($rgba, $false) })
        }
        elseif ($hasDefense) {
            $slots.Add(@{ name = 'secondary-defense'; trace = [GuardsDigital.Tools.ShineTracer]::TraceRail($rgba, $false) })
        }
        elseif ($hasMovement) {
            $slots.Add(@{ name = 'secondary-movement'; trace = [GuardsDigital.Tools.ShineTracer]::TraceRail($rgba, $false) })
        }

        if ($typed.primaryKind -ne 'None') {
            $donorKind = $typed.primaryKind
            if ($typed.primaryKind -eq 'Defense' -and $typed.primaryIsBlock -eq '1') {
                $donorKind = 'Block'
            }

            if (-not $donors.ContainsKey($donorKind)) {
                Write-Output "NO-DONOR $key kind=$donorKind"
                $failed++
            }
            else {
                # Attack and Movement print a value digit whose shape varies per card; the digit
                # zone is re-traced on each card so a 7 shines as a 7. A Defense primary's digit
                # sits INSIDE its shield outline, so the template already covers it.
                $adapt = $typed.primaryKind -eq 'Attack' -or $typed.primaryKind -eq 'Movement'
                $slots.Add(@{ name = 'primary'; trace = [GuardsDigital.Tools.ShineTracer]::TracePrimary($rgba, $donors[$donorKind], $adapt) })
            }
        }

        foreach ($slot in $slots) {
            $traced++
            $trace = $slot.trace
            $fileName = "$card--$($slot.name).png"
            $target = Join-Path $shineDir $fileName
            $row = "$hero,$card,$($slot.name),$($trace.Status),$($trace.X0),$($trace.Y0),$($trace.X1),$($trace.Y1),$($trace.Area),"
            if ($trace.Status -ne 'ok') {
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
}
