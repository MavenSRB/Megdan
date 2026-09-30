using System.Collections.Generic;
using GuardsDigital.Presentation.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 9.20. Cuts a printed glyph off the card stock it was prepared on, for the user's
    /// "just have like a picture of hammer sigil anvil hook".
    ///
    /// <para>A turn-slot marker's prepared resource (<c>Tools/Prepare-MarkerArtwork.ps1</c>) is a
    /// small opaque tile of the card that prints the glyph: dark ink on pale card stock, with slivers
    /// of the neighbouring glyphs where the printed column packs them tight. This keys the stock out
    /// by brightness, drops any small ink island that enters from the tile's edge (a neighbour, not
    /// the glyph), crops to what is left, and redraws it larger as a pale picture with a transparent
    /// ground: the print's darkest ink becomes the brightest face and its lighter engraving lines
    /// read as shade, so the printed detail survives the inversion a dark HUD needs. It then draws
    /// <see cref="BoardPieceOutline"/>'s black line around the figure by dilating its alpha. It works
    /// at runtime from the untouched private file, read back through the GPU as
    /// <see cref="PreparedObstacleCutout"/> does, and names no hero and no glyph.</para>
    /// </summary>
    public static class PrintedGlyphCutout
    {
        /// <summary>The finished texture's pixels per prepared source pixel.</summary>
        public const int Magnification = 6;

        /// <summary>
        /// The outline's radius in prepared source pixels: just under one screen pixel at play
        /// scale, where the tray draws a source pixel at about half a screen pixel at 1920x1080.
        /// </summary>
        public const float OutlineSourcePixels = 1.6f;

        /// <summary>The face colour: the card stock's own ivory.</summary>
        public static readonly Color32 Face = new Color32(236, 228, 206, 255);

        // Ink coverage: where a pixel falls between the tile's darkest ink and its stock.
        private const float InkPercentile = 0.04f;
        private const float StockPercentile = 0.70f;
        private const float LevelMargin = 10f;

        // An ink island this solid is part of a figure; an island touching the tile's edge that is
        // smaller than this share of the largest is a neighbouring glyph's sliver.
        private const float IslandCoverage = 0.4f;
        private const float NeighbourShare = 0.3f;

        // The silhouette's edge and the face's shading, as coverage ramps.
        private const float EdgeLow = 0.2f;
        private const float EdgeHigh = 0.45f;
        private const float ToneLow = 0.45f;
        private const float ToneHigh = 0.9f;
        private const float Shade = 0.6f;

        private static readonly Dictionary<Texture2D, Texture2D> Cache =
            new Dictionary<Texture2D, Texture2D>();

        /// <summary>
        /// The glyph cut from <paramref name="tile"/>, built once per tile and shared, and named after
        /// the tile with <c>" (glyph)"</c>. Null when nothing inked is left to draw.
        /// </summary>
        public static Texture2D Cut(Texture2D tile)
        {
            if (tile == null) throw new System.ArgumentNullException(nameof(tile));
            if (Cache.TryGetValue(tile, out var cached) && cached != null) return cached;
            var pixels = PreparedObstacleCutout.ReadBack(tile);
            var output = Cut(pixels, tile.width, tile.height, out var width, out var height);
            if (output == null) return null;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = tile.name + " (glyph)",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(output);
            texture.Apply(true, false);
            Cache[tile] = texture;
            return texture;
        }

        /// <summary>The cut over raw pixels, rows either way up. Null when no ink is found.</summary>
        public static Color32[] Cut(
            Color32[] tile, int width, int height, out int outWidth, out int outHeight)
        {
            if (tile == null) throw new System.ArgumentNullException(nameof(tile));
            outWidth = 0;
            outHeight = 0;
            var count = width * height;
            if (count <= 0 || tile.Length != count) return null;
            var luma = new float[count];
            var sorted = new float[count];
            for (var index = 0; index < count; index++)
            {
                var pixel = tile[index];
                luma[index] = (0.299f * pixel.r) + (0.587f * pixel.g) + (0.114f * pixel.b);
                sorted[index] = luma[index];
            }

            System.Array.Sort(sorted);
            var ink = sorted[(int)(count * InkPercentile)] + LevelMargin;
            var stock = sorted[(int)(count * StockPercentile)] - LevelMargin;
            if (stock - ink < 1f) return null;
            var cover = new float[count];
            for (var index = 0; index < count; index++)
                cover[index] = Mathf.Clamp01((stock - luma[index]) / (stock - ink));

            var kept = KeepFigure(cover, width, height);
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (kept[(y * width) + x] < EdgeLow) continue;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < 0) return null;
            const int scale = Magnification;
            const float radius = OutlineSourcePixels * scale;
            var margin = Mathf.CeilToInt(radius) + 2;
            var cropWidth = (maxX - minX + 1) * scale;
            var cropHeight = (maxY - minY + 1) * scale;
            outWidth = cropWidth + (2 * margin);
            outHeight = cropHeight + (2 * margin);
            var alpha = new float[outWidth * outHeight];
            var tone = new float[outWidth * outHeight];
            for (var y = 0; y < cropHeight; y++)
            {
                for (var x = 0; x < cropWidth; x++)
                {
                    var sx = ((x + 0.5f) / scale) - 0.5f + minX;
                    var sy = ((y + 0.5f) / scale) - 0.5f + minY;
                    var value = Bilinear(kept, width, height, sx, sy);
                    var target = ((y + margin) * outWidth) + x + margin;
                    alpha[target] = Smooth(EdgeLow, EdgeHigh, value);
                    tone[target] = Smooth(ToneLow, ToneHigh, value);
                }
            }

            // The line: the figure's alpha dilated by a disc of the outline's radius, with a
            // one-pixel soft rim, as the Turret's cutout draws it.
            var reach = Mathf.CeilToInt(radius + 0.5f);
            var offsets = new List<(int dx, int dy, float weight)>();
            for (var dy = -reach; dy <= reach; dy++)
            {
                for (var dx = -reach; dx <= reach; dx++)
                {
                    var weight = Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt((dx * dx) + (dy * dy)));
                    if (weight > 0f) offsets.Add((dx, dy, weight));
                }
            }

            var line = BoardPieceOutline.Colour;
            var output = new Color32[outWidth * outHeight];
            for (var y = 0; y < outHeight; y++)
            {
                for (var x = 0; x < outWidth; x++)
                {
                    var index = (y * outWidth) + x;
                    var own = alpha[index];
                    var ring = 0f;
                    if (own < 1f)
                    {
                        foreach (var (dx, dy, weight) in offsets)
                        {
                            var nx = x + dx;
                            var ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= outWidth || ny >= outHeight) continue;
                            ring = Mathf.Max(ring, alpha[(ny * outWidth) + nx] * weight);
                            if (ring >= 1f) break;
                        }
                    }

                    var share = ring * (1f - own);
                    var total = own + share;
                    if (total <= 0f) continue;
                    var face = own * (Shade + ((1f - Shade) * tone[index]));
                    output[index] = new Color32(
                        (byte)Mathf.RoundToInt(((Face.r * face) + (line.r * share)) / total),
                        (byte)Mathf.RoundToInt(((Face.g * face) + (line.g * share)) / total),
                        (byte)Mathf.RoundToInt(((Face.b * face) + (line.b * share)) / total),
                        (byte)Mathf.RoundToInt(total * 255f));
                }
            }

            return output;
        }

        /// <summary>
        /// Coverage with every ink island dropped that enters from the tile's edge and is small beside
        /// the largest: a neighbouring glyph's sliver. A kept island keeps its one-pixel soft rim.
        /// </summary>
        private static float[] KeepFigure(float[] cover, int width, int height)
        {
            var count = width * height;
            var label = new int[count];
            var sizes = new List<int> { 0 };
            var touchesEdge = new List<bool> { false };
            var stack = new Stack<int>();
            for (var start = 0; start < count; start++)
            {
                if (cover[start] < IslandCoverage || label[start] != 0) continue;
                var id = sizes.Count;
                sizes.Add(0);
                touchesEdge.Add(false);
                label[start] = id;
                stack.Push(start);
                while (stack.Count != 0)
                {
                    var index = stack.Pop();
                    sizes[id]++;
                    var x = index % width;
                    var y = index / width;
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1) touchesEdge[id] = true;
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            var nx = x + dx;
                            var ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                            var next = (ny * width) + nx;
                            if (label[next] != 0 || cover[next] < IslandCoverage) continue;
                            label[next] = id;
                            stack.Push(next);
                        }
                    }
                }
            }

            var largest = 0;
            for (var id = 1; id < sizes.Count; id++) largest = Mathf.Max(largest, sizes[id]);
            var keep = new bool[count];
            for (var index = 0; index < count; index++)
            {
                var id = label[index];
                keep[index] = id != 0 && !(touchesEdge[id] && sizes[id] < largest * NeighbourShare);
            }

            var kept = new float[count];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var near = false;
                    for (var dy = -1; dy <= 1 && !near; dy++)
                    {
                        for (var dx = -1; dx <= 1 && !near; dx++)
                        {
                            var nx = x + dx;
                            var ny = y + dy;
                            near = nx >= 0 && ny >= 0 && nx < width && ny < height &&
                                keep[(ny * width) + nx];
                        }
                    }

                    kept[(y * width) + x] = near ? cover[(y * width) + x] : 0f;
                }
            }

            return kept;
        }

        private static float Bilinear(float[] values, int width, int height, float x, float y)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var fx = x - x0;
            var fy = y - y0;
            return (Sample(values, width, height, x0, y0) * (1f - fx) * (1f - fy)) +
                (Sample(values, width, height, x0 + 1, y0) * fx * (1f - fy)) +
                (Sample(values, width, height, x0, y0 + 1) * (1f - fx) * fy) +
                (Sample(values, width, height, x0 + 1, y0 + 1) * fx * fy);
        }

        private static float Sample(float[] values, int width, int height, int x, int y) =>
            x < 0 || y < 0 || x >= width || y >= height ? 0f : values[(y * width) + x];

        private static float Smooth(float low, float high, float value)
        {
            var t = Mathf.Clamp01((value - low) / (high - low));
            return t * t * (3f - (2f * t));
        }
    }
}
