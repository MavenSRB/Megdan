using System;
using System.Security.Cryptography;

namespace GuardsDigital.Core.Setup
{
    /// <summary>
    /// What a draw from a match's recorded setup randomness is for.
    ///
    /// The purpose is mixed into the derivation, so two consumers reading the same recorded seed get
    /// independent draws rather than the same bit. That is the whole reason this enum exists: without
    /// it, the second consumer to join the seam would silently correlate with the first, and a
    /// correlated draw is a rules defect that no test would think to look for.
    /// </summary>
    public enum SetupRandomnessPurpose
    {
        /// <summary>The named absence. Derivation refuses it rather than deriving a "default" draw.</summary>
        None = 0,

        /// <summary>Rulebook p.5's setup coin flip, which reveals the starting Tie Breaker side.</summary>
        TieBreakerCoinFlip = 1
    }

    /// <summary>
    /// One match's recorded setup draw: the single number every setup decision that needs randomness
    /// derives from, recorded in authoritative setup state so the decision can be made again and reach
    /// the same answer.
    ///
    /// Milestone 8.28b, closing `F-ER2-04`. Before it, `TieBreakerSetupAuthority.CreateNormalSetupState`
    /// drew a byte straight from <see cref="RandomNumberGenerator"/> and returned only the side. The
    /// outcome was carried everywhere it needed to go, so replay and restore both worked - but the
    /// draw itself was nowhere, so setup could not be *performed* again, only *remembered*. A future
    /// online authority cannot hand a client a memory; it hands it a seed.
    /// </summary>
    public readonly struct SetupRandomnessSeed : IEquatable<SetupRandomnessSeed>
    {
        public SetupRandomnessSeed(ulong value)
        {
            Value = value;
        }

        /// <summary>
        /// The named absence: this match's setup made no random draw. That is the honest description
        /// of a `DEV-005` setup, whose Tie Breaker side is explicitly selected rather than flipped, and
        /// of every focused fixture that names the side it wants.
        /// </summary>
        public static SetupRandomnessSeed Unrecorded => default;

        public ulong Value { get; }

        public bool IsRecorded => Value != 0UL;

        public bool Equals(SetupRandomnessSeed other) => Value == other.Value;

        public override bool Equals(object obj) => obj is SetupRandomnessSeed other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() =>
            IsRecorded
                ? "seed 0x" + Value.ToString("X16", System.Globalization.CultureInfo.InvariantCulture)
                : "no recorded seed";
    }

    /// <summary>
    /// The one place unseeded entropy enters a match, and the one derivation every consumer of a
    /// recorded seed uses.
    ///
    /// `SetupRandomnessConformanceTests` holds this claim mechanically: no other production type may
    /// reference the platform entropy source at all. That rule is what makes "future randomness
    /// consumers have one seam to join" a fact rather than an intention - the second consumer cannot
    /// quietly open its own.
    /// </summary>
    public static class SetupRandomnessAuthority
    {
        /// <summary>
        /// Draws the seed a normal match records. This is the only unseeded draw in the engine.
        /// </summary>
        /// <remarks>
        /// Zero is <see cref="SetupRandomnessSeed.Unrecorded"/>, so a drawn zero would describe itself
        /// as never having happened. It is redrawn rather than adjusted, because adjusting it would
        /// make one value twice as likely and this is a coin flip's source.
        /// </remarks>
        public static SetupRandomnessSeed DrawSeed()
        {
            using (var random = RandomNumberGenerator.Create())
            {
                var bytes = new byte[sizeof(ulong)];
                while (true)
                {
                    random.GetBytes(bytes);
                    var value = BitConverter.ToUInt64(bytes, 0);
                    if (value != 0UL)
                    {
                        return new SetupRandomnessSeed(value);
                    }
                }
            }
        }

        /// <summary>
        /// The recorded seed's draw for one named purpose. Same seed and purpose, same answer, on
        /// every runtime and every build.
        /// </summary>
        /// <remarks>
        /// The mix is written out here rather than taken from a framework type for the reason milestone
        /// 7.9's match loop hand-wrote its own generator: <c>System.Random</c> differs between Unity's
        /// Mono and .NET, and <c>HashCode.Combine</c> is randomized per process, so either would make a
        /// recorded seed mean different things in different places - which is precisely the property
        /// this type exists to remove. This is SplitMix64's finalizer over the seed and purpose; it is
        /// arithmetic, so it cannot drift.
        /// </remarks>
        public static ulong Derive(SetupRandomnessSeed seed, SetupRandomnessPurpose purpose)
        {
            if (!seed.IsRecorded)
            {
                throw new ArgumentException(
                    "A setup draw requires a recorded seed. An explicitly selected setup value has no " +
                    "draw to derive and must not ask for one.",
                    nameof(seed));
            }

            if (purpose == SetupRandomnessPurpose.None ||
                !Enum.IsDefined(typeof(SetupRandomnessPurpose), purpose))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(purpose), purpose, "A setup draw states which named purpose it is for.");
            }

            unchecked
            {
                var z = seed.Value + (0x9E3779B97F4A7C15UL * (ulong)purpose);
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
