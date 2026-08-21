using System;
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
        /// <summary>
        /// Rulebook p.5's setup coin flip, derived from the match's recorded setup draw.
        ///
        /// Milestone 8.28b, closing `F-ER2-04`. The flip is still random from the player's
        /// perspective - the seed it derives from was drawn from platform entropy at setup - but it is
        /// now reproducible from a fact the match wrote down, which is what deterministic replay and a
        /// future online authority both need.
        /// </summary>
        public static TieBreakerState CreateNormalSetupState(SetupRandomnessSeed seed)
        {
            var draw = SetupRandomnessAuthority.Derive(
                seed, SetupRandomnessPurpose.TieBreakerCoinFlip);
            return new TieBreakerState(
                (draw & 1UL) == 0UL
                    ? TeamSide.Red
                    : TeamSide.Blue);
        }

        public static TieBreakerState CreateInternalTestSetupState(TeamSide explicitSide)
        {
            return new TieBreakerState(explicitSide);
        }
    }
}
