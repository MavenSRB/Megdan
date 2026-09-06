using System;

namespace GuardsDigital.Application.Bots
{
    /// <summary>
    /// The one source of chance a practice bot draws from, behind an interface so the tiebreak is the
    /// same code whichever runtime is asking.
    /// </summary>
    public interface IBotRandom
    {
        /// <summary>A value in <c>[0, exclusiveUpperBound)</c>.</summary>
        int Next(int exclusiveUpperBound);
    }

    /// <summary>
    /// xorshift32, written out rather than taken from the framework, so that one seed names one
    /// sequence everywhere.
    /// </summary>
    /// <remarks>
    /// <para><b>Not <see cref="System.Random"/>, and the reason is load-bearing.</b> Its algorithm is
    /// not part of its contract and is known to differ between the Mono runtime Unity's tiers execute
    /// on and the .NET runtime the cross-platform harness executes on. A bot seeded with
    /// <c>System.Random</c> therefore plays one match in the harness and a different one in the real
    /// build from the same seed - which makes "deterministic per seed" a claim that cannot be
    /// verified where it matters, and makes a harness-measured win rate a measurement of a bot that
    /// is not the shipped bot. <c>Assets/Tests/EditMode/DeterministicMatchLoop.cs</c> reached this
    /// conclusion first, for the same reason; this is the same four lines, so a seeded self-play run
    /// and a seeded live bot draw the same numbers.</para>
    /// <para>The modulo bias in <see cref="Next"/> is real and deliberately accepted, exactly as in
    /// that driver: the property needed here is that one seed names one sequence, not that the
    /// sequence is uniform. Nothing here samples a distribution.</para>
    /// </remarks>
    public sealed class DeterministicBotRandom : IBotRandom
    {
        private uint state;

        public DeterministicBotRandom(int seed)
        {
            // xorshift32 is fixed at zero, so a zero seed would produce one constant forever.
            state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
        }

        public int Next(int exclusiveUpperBound)
        {
            if (exclusiveUpperBound <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveUpperBound), exclusiveUpperBound, "An empty range has nothing to draw.");
            }

            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (int)(state % (uint)exclusiveUpperBound);
        }
    }

    /// <summary>
    /// Adapts a <see cref="System.Random"/> so the tier-0.5 preferences keep the exact source of
    /// chance they were written and tested against. New code should prefer
    /// <see cref="DeterministicBotRandom"/> for the reason its remarks give.
    /// </summary>
    public sealed class SystemBotRandom : IBotRandom
    {
        private readonly Random random;

        public SystemBotRandom(Random random) =>
            this.random = random ?? throw new ArgumentNullException(nameof(random));

        public int Next(int exclusiveUpperBound) => random.Next(exclusiveUpperBound);
    }
}
