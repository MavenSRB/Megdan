using System;

namespace GuardsDigital.Pve.Composition
{
    /// <summary>
    /// PVE-D13. The encounter's only source of randomness: an explicit SplitMix64 mix of the setup
    /// seed and stable identifiers, so every runtime (Unity's Mono and the .NET harness alike)
    /// derives the same value and nothing has to be stored to replay it.
    /// </summary>
    public static class PveSeed
    {
        /// <summary>A seed of zero is reserved for "unrecorded"; any other value is used as given.</summary>
        public static ulong Normalize(ulong seed) => seed == 0UL ? 0x5EEDUL : seed;

        /// <summary>A fresh seed for an ordinary setup, from the clock and a process-local draw.</summary>
        public static ulong Draw()
        {
            var bytes = Guid.NewGuid().ToByteArray();
            return Normalize(BitConverter.ToUInt64(bytes, 0) ^ (ulong)DateTime.UtcNow.Ticks);
        }

        public static ulong Mix(ulong value)
        {
            unchecked
            {
                value += 0x9E3779B97F4A7C15UL;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }

        /// <summary>FNV-1a over UTF-16 code units: stable across runtimes, unlike string.GetHashCode.</summary>
        public static ulong StableHash(string text)
        {
            unchecked
            {
                var hash = 0xCBF29CE484222325UL;
                foreach (var character in text ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 0x100000001B3UL;
                }

                return hash;
            }
        }

        /// <summary>
        /// A uniform index in [0, count) for one enemy's plan in one round. The modulo bias is below
        /// 2^-60 for any plan list a type can declare.
        /// </summary>
        public static int PlanIndex(ulong seed, int round, string enemyId, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            unchecked
            {
                var value = Mix(Mix(Normalize(seed) ^ StableHash(enemyId)) + (ulong)round);
                return (int)(value % (ulong)count);
            }
        }
    }
}
