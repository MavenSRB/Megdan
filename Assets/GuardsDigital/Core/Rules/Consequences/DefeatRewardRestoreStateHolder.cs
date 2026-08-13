using System;

namespace GuardsDigital.Core.Rules.Consequences
{
    /// <summary>
    /// Either the Defeat-reward state to restore, or an explicit statement that this
    /// snapshot carries none.
    /// </summary>
    /// <remarks>
    /// The snapshot writes a presence flag ahead of the Defeat-reward block, so absence is
    /// a real persisted case rather than a missing value. Modelling it as a holder means a
    /// caller must say which case applies. Before milestone 6.1g the same distinction was
    /// carried by a <c>DefeatRewardAccounting defeatRewards = null</c> default across four
    /// restore entry points, where "no rewards recorded" and "I forgot to pass the rewards"
    /// were the same expression and produced the same silent coin-balance loss.
    /// </remarks>
    public sealed class DefeatRewardRestoreStateHolder
    {
        private static readonly DefeatRewardRestoreStateHolder AbsentHolder =
            new DefeatRewardRestoreStateHolder(null);

        private DefeatRewardRestoreStateHolder(DefeatRewardRestoreState state)
        {
            State = state;
        }

        public bool IsPresent => State != null;

        /// <summary>The reward state, or <c>null</c> when <see cref="IsPresent"/> is false.</summary>
        public DefeatRewardRestoreState State { get; }

        public static DefeatRewardRestoreStateHolder Absent() => AbsentHolder;

        public static DefeatRewardRestoreStateHolder Present(DefeatRewardRestoreState state) =>
            new DefeatRewardRestoreStateHolder(state ?? throw new ArgumentNullException(nameof(state)));

        public void Deconstruct(out bool isPresent, out DefeatRewardRestoreState state)
        {
            isPresent = IsPresent;
            state = State;
        }
    }
}
