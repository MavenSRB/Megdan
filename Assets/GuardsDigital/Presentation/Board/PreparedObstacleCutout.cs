using System.Collections.Generic;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 9.16. Cuts a prepared circular standee face down to the figure it prints.
    ///
    /// <para>The Turret's prepared resource is NOT a cutout: <c>Prepare-HeroTokens.ps1 -Companion
    /// -Neutral</c> wrote it through the neutral hero-standee route, which whitens the source's team
    /// ring and field. The file is therefore a white disc (about a third of its pixels are opaque
    /// white) with the turret painted inside it and fully transparent black corners outside it. This
    /// keys out the pale ground connected to the disc's edge, feathers the one-pixel boundary, and
    /// crops to what is left - at runtime, from the untouched private file, so no prepared art is
    /// rewritten - and then draws <see cref="BoardPieceOutline"/>'s thin black line around the
    /// figure by dilating its alpha, as the user asked on the 9.16 captures.</para>
    ///
    /// <para>The resource is imported non-readable, so its pixels are read back through the GPU with
    /// a blit rather than by changing the private file's import settings. The project renders in
    /// gamma space, so the blit and read-back move the stored bytes unchanged.</para>
    /// </summary>
    public static class PreparedObstacleCutout
    {
        /// <summary>A pixel at least this bright in every channel counts as the whitened ground.</summary>
        public const byte PaleFloor = 190;

        /// <summary>...and only while it stays this close to grey; a pale yellow highlight is art.</summary>
        public const int PaleChromaCeiling = 40;

        /// <summary>The boundary feather: a kept edge pixel keeps (255 - darkest channel) / this of its alpha.</summary>
        public const float FeatherRange = 90f;

        /// <summary>Below this alpha a pixel connected to the disc's edge is ground.</summary>
        public const byte TranslucentCeiling = 240;

        private const int CropPadding = 2;
        private const int BleedPasses = 4;

        private static readonly Dictionary<(Texture2D, int), Texture2D> Cache =
            new Dictionary<(Texture2D, int), Texture2D>();

        /// <summary>
        /// The cut figure with <see cref="BoardPieceOutline"/>'s line around it, built once per texture
        /// and line width and shared. <paramref name="outlineShare"/> is the line's width as a share of
        /// the finished texture's width - the line's world width over the width the caller draws the
        /// texture at - so the line comes out at the board's one outline width whatever the art's
        /// resolution.
        /// </summary>
        public static Texture2D CutWithOutline(Texture2D source, float outlineShare)
        {
            if (source == null) throw new System.ArgumentNullException(nameof(source));
            if (!(outlineShare >= 0f && outlineShare < 0.25f))
                throw new System.ArgumentOutOfRangeException(nameof(outlineShare), outlineShare,
                    "An outline share must be a finite share of the texture's width below a quarter.");
            var key = (source, Mathf.RoundToInt(outlineShare * 100000f));
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var figure = CutFigure(source, out var figureWidth, out var figureHeight);
            var radius = outlineShare * figureWidth / (1f - (2f * outlineShare));
            var margin = Mathf.CeilToInt(radius) + 1;
            var width = figureWidth + (2 * margin);
            var height = figureHeight + (2 * margin);
            var figureAlpha = new float[width * height];
            var placed = new Color32[width * height];
            for (var y = 0; y < figureHeight; y++)
            {
                for (var x = 0; x < figureWidth; x++)
                {
                    var target = ((y + margin) * width) + x + margin;
                    placed[target] = figure[(y * figureWidth) + x];
                    figureAlpha[target] = placed[target].a / 255f;
                }
            }

            // The line: the figure's alpha dilated by a disc of the outline's radius, with a
            // one-pixel soft rim so it stays a line rather than a staircase when minified.
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

            var ink = BoardPieceOutline.Colour;
            var output = new Color32[width * height];
            var cleared = new bool[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var line = 0f;
                    foreach (var (dx, dy, weight) in offsets)
                    {
                        var nx = x + dx;
                        var ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                        line = Mathf.Max(line, figureAlpha[(ny * width) + nx] * weight);
                        if (line >= 1f) break;
                    }

                    var index = (y * width) + x;
                    var own = figureAlpha[index];
                    var alpha = own + (line * (1f - own));
                    if (alpha <= 0f)
                    {
                        cleared[index] = true;
                        continue;
                    }

                    var pixel = placed[index];
                    var lineShare = line * (1f - own);
                    output[index] = new Color32(
                        (byte)Mathf.RoundToInt(((pixel.r * own) + (ink.r * lineShare)) / alpha),
                        (byte)Mathf.RoundToInt(((pixel.g * own) + (ink.g * lineShare)) / alpha),
                        (byte)Mathf.RoundToInt(((pixel.b * own) + (ink.b * lineShare)) / alpha),
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            // A cleared pixel keeps the colour of its nearest kept neighbour, so bilinear filtering
            // and the mip chain never pull anything but the line's own black in at the edge.
            BleedColour(output, cleared, width, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = source.name + " (cut, outlined)",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(output);
            texture.Apply(true, false);
            Cache[key] = texture;
            return texture;
        }

        /// <summary>
        /// Keys out the pale ground connected to the disc's edge, feathers the boundary, and crops to
        /// the figure with a small transparent margin.
        /// </summary>
        private static Color32[] CutFigure(Texture2D source, out int cropWidth, out int cropHeight)
        {
            var width = source.width;
            var height = source.height;
            var pixels = ReadBack(source);
            var removed = new bool[pixels.Length];
            var stack = new Stack<int>();
            for (var x = 0; x < width; x++)
            {
                stack.Push(x);
                stack.Push(((height - 1) * width) + x);
            }

            for (var y = 0; y < height; y++)
            {
                stack.Push(y * width);
                stack.Push((y * width) + width - 1);
            }

            while (stack.Count != 0)
            {
                var index = stack.Pop();
                if (removed[index] || !IsGround(pixels[index])) continue;
                removed[index] = true;
                var x = index % width;
                var y = index / width;
                if (x > 0) stack.Push(index - 1);
                if (x < width - 1) stack.Push(index + 1);
                if (y > 0) stack.Push(index - width);
                if (y < height - 1) stack.Push(index + width);
            }

            var output = new Color32[pixels.Length];
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = (y * width) + x;
                    if (removed[index]) continue;
                    var pixel = pixels[index];
                    if (Touches(removed, width, height, x, y))
                    {
                        var darkest = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
                        pixel.a = (byte)Mathf.RoundToInt(
                            pixel.a * Mathf.Clamp01((255f - darkest) / FeatherRange));
                    }

                    output[index] = pixel;
                    if (pixel.a == 0) continue;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < 0) throw new System.InvalidOperationException(
                "The prepared obstacle face keyed out entirely; nothing is left to draw.");

            minX = Mathf.Max(0, minX - CropPadding);
            minY = Mathf.Max(0, minY - CropPadding);
            maxX = Mathf.Min(width - 1, maxX + CropPadding);
            maxY = Mathf.Min(height - 1, maxY + CropPadding);
            cropWidth = maxX - minX + 1;
            cropHeight = maxY - minY + 1;
            var cropped = new Color32[cropWidth * cropHeight];
            for (var y = 0; y < cropHeight; y++)
                System.Array.Copy(output, ((y + minY) * width) + minX, cropped, y * cropWidth, cropWidth);
            return cropped;
        }

        private static bool IsGround(Color32 pixel)
        {
            // Every partially transparent pixel in the prepared file is the anti-aliased rim of the
            // whitened disc (the figure itself is fully opaque), and block compression greys those
            // rim pixels toward the black corners, so they are ground whatever their colour now is.
            if (pixel.a < TranslucentCeiling) return true;
            var darkest = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
            var brightest = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
            return darkest >= PaleFloor && brightest - darkest <= PaleChromaCeiling;
        }

        private static bool Touches(bool[] removed, int width, int height, int x, int y) =>
            (x > 0 && removed[(y * width) + x - 1]) ||
            (x < width - 1 && removed[(y * width) + x + 1]) ||
            (y > 0 && removed[((y - 1) * width) + x]) ||
            (y < height - 1 && removed[((y + 1) * width) + x]);

        private static void BleedColour(Color32[] pixels, bool[] removed, int width, int height)
        {
            var coloured = new bool[pixels.Length];
            for (var index = 0; index < pixels.Length; index++) coloured[index] = !removed[index];
            for (var pass = 0; pass < BleedPasses; pass++)
            {
                var next = (bool[])coloured.Clone();
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var index = (y * width) + x;
                        if (coloured[index]) continue;
                        for (var dy = -1; dy <= 1 && !next[index]; dy++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var nx = x + dx;
                                var ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                                var neighbour = (ny * width) + nx;
                                if (!coloured[neighbour]) continue;
                                var colour = pixels[neighbour];
                                pixels[index] = new Color32(colour.r, colour.g, colour.b, 0);
                                next[index] = true;
                                break;
                            }
                        }
                    }
                }

                coloured = next;
            }
        }

        private static Color32[] ReadBack(Texture2D source)
        {
            if (source.isReadable) return source.GetPixels32();
            var target = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            var previous = RenderTexture.active;
            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply(false);
                return readable.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (UnityEngine.Application.isPlaying) Object.Destroy(readable);
                else Object.DestroyImmediate(readable);
            }
        }
    }
}
