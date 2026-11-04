using GuardsDigital.Core.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Private, presentation-only grass art prepared from Veljko's four supplied hexes.
    /// Missing private assets are an ordinary clean-checkout condition; callers retain the
    /// generated synthetic fill in that case.
    /// </summary>
    public static class MegdanGrassTileCatalog
    {
        public const int VariantCount = 4;

        private static readonly string[] ResourcePaths =
        {
            "Private/Megdan/Grass/grass-1",
            "Private/Megdan/Grass/grass-2",
            "Private/Megdan/Grass/grass-3",
            "Private/Megdan/Grass/grass-4"
        };

        private const string WaterResourcePath = "Private/Megdan/Biomes/water";
        private const string LavaResourcePath = "Private/Megdan/Biomes/lava";
        private const string DesertResourcePath = "Private/Megdan/Biomes/desert";

        public static bool TryLoadTextures(out Texture2D[] textures)
        {
            textures = new Texture2D[VariantCount];
            for (var index = 0; index < textures.Length; index++)
            {
                textures[index] = Resources.Load<Texture2D>(ResourcePaths[index]);
                if (textures[index] == null)
                {
                    textures = System.Array.Empty<Texture2D>();
                    return false;
                }
            }

            return true;
        }

        public static bool TryLoadBiomeTextures(
            out Texture2D water,
            out Texture2D lava,
            out Texture2D desert)
        {
            water = Resources.Load<Texture2D>(WaterResourcePath);
            lava = Resources.Load<Texture2D>(LavaResourcePath);
            desert = Resources.Load<Texture2D>(DesertResourcePath);
            return water != null && lava != null && desert != null;
        }

        /// <summary>
        /// Stable presentation variety: the same map coordinate receives the same image on every
        /// rebuild, client and launch. This is deliberately independent of rule/session randomness.
        /// </summary>
        public static int VariantIndex(MapDefinitionReference map, HexCoordinate coordinate)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in map.MapId.Value)
                {
                    hash = (hash ^ character) * 16777619u;
                }

                hash = Mix(hash, map.ContentRevision);
                hash = Mix(hash, coordinate.Q);
                hash = Mix(hash, coordinate.R);
                return (int)(hash % VariantCount);
            }
        }

        private static uint Mix(uint hash, int value)
        {
            unchecked
            {
                hash = (hash ^ (byte)value) * 16777619u;
                hash = (hash ^ (byte)(value >> 8)) * 16777619u;
                hash = (hash ^ (byte)(value >> 16)) * 16777619u;
                return (hash ^ (byte)(value >> 24)) * 16777619u;
            }
        }
    }
}
