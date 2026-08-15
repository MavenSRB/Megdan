using System;
using System.Security.Cryptography;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Setup
{
    public readonly struct TieBreakerState : IEquatable<TieBreakerState>
    {
        public TieBreakerState(TeamSide revealedSide)
        {
            if (revealedSide != TeamSide.Red && revealedSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revealedSide),
                    revealedSide,
                    "The Tie Breaker must reveal Red or Blue.");
            }

            RevealedSide = revealedSide;
        }

        /// <summary>
        /// The named absence of revealed Tie-Breaker state: no side is revealed, so p.17's
        /// tie-breaking clause orders nothing ahead of anything else.
        ///
        /// Milestone 6.8i made the parameter required wherever it decides a rules answer, and this is
        /// what a genuinely order-free call site writes instead of inheriting a parameter default.
        /// The value is the same one the default produced; what changed is that the call site now
        /// says it.
        /// </summary>
        public static TieBreakerState Unrevealed => default;

        public TeamSide RevealedSide { get; }

        public bool IsDefined => RevealedSide == TeamSide.Red || RevealedSide == TeamSide.Blue;

        public bool Equals(TieBreakerState other)
        {
            return RevealedSide == other.RevealedSide;
        }

        public override bool Equals(object obj)
        {
            return obj is TieBreakerState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)RevealedSide;
        }

        public override string ToString()
        {
            return RevealedSide.ToString();
        }
    }

    public static class TieBreakerSetupAuthority
    {
        public static TieBreakerState CreateNormalSetupState()
        {
            using (var random = RandomNumberGenerator.Create())
            {
                var result = new byte[1];
                random.GetBytes(result);
                return new TieBreakerState(
                    (result[0] & 1) == 0
                        ? TeamSide.Red
                        : TeamSide.Blue);
            }
        }

        public static TieBreakerState CreateInternalTestSetupState(TeamSide explicitSide)
        {
            return new TieBreakerState(explicitSide);
        }
    }
}
