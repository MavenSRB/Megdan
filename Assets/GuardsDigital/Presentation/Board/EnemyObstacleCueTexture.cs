using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 9.32g. The texture of the enemy-obstacle cue (approved mockup E): a faint fill,
    /// diagonal hatching, the hex's own outline and a barred sign at the centre, all in white so the
    /// material's red tints them. It is mapped by <see cref="FlatHexMeshBuilder"/>'s UVs, which place
    /// the hex's corners on a circle of radius one half around the square's centre.
    /// </summary>
    public static class EnemyObstacleCueTexture
    {
        public static Texture2D Create(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Enemy Obstacle Cue",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = (x + 0.5f) / size - 0.5f;
                    var v = (y + 0.5f) / size - 0.5f;
                    pixels[(y * size) + x] = new Color32(255, 255, 255, (byte)(255f * Alpha(u, v)));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>The cue's coverage at a point, measured from the centre in UV units.</summary>
        private static float Alpha(float u, float v)
        {
            // The pointy-top-to-the-side hex the mesh builds: corners at 30 + 60k degrees, radius 0.5,
            // so its edge sits at the apothem 0.433 along the six normals at 0, 60, ... degrees.
            var edge = 0f;
            for (var side = 0; side < 6; side++)
            {
                var angle = Mathf.Deg2Rad * (side * 60f);
                edge = Mathf.Max(edge, (u * Mathf.Cos(angle)) + (v * Mathf.Sin(angle)));
            }

            const float apothem = 0.433f;
            if (edge > apothem)
            {
                return 0f;
            }

            if (edge > apothem - 0.035f)
            {
                return 0.95f; // the hex outline
            }

            var radius = Mathf.Sqrt((u * u) + (v * v));
            if (radius < 0.11f && radius > 0.075f)
            {
                return 1f; // the barred sign's ring
            }

            if (radius <= 0.075f && Mathf.Abs(u + v) < 0.025f)
            {
                return 1f; // its bar
            }

            if (radius <= 0.075f)
            {
                return 0.55f; // the sign's dark heart reads through the red tint
            }

            var stripe = Mathf.Repeat((u - v) * 9f, 1f);
            return stripe < 0.28f ? 0.62f : 0.22f; // hatching over a faint fill
        }
    }
}
