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
