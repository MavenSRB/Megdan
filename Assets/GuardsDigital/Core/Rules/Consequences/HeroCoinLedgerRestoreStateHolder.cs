using System;

namespace GuardsDigital.Core.Rules.Consequences
{
    /// <summary>
    /// Either the coin-ledger state to restore, or an explicit statement that this
    /// snapshot carries none.
    /// </summary>
    /// <remarks>
    /// The snapshot writes a presence flag ahead of the coin-ledger block, so absence is
    /// a real persisted case rather than a missing value. Modelling it as a holder means a
    /// caller must say which case applies. Before milestone 6.1g the same distinction was
    /// carried by a <c>HeroCoinLedger coinLedger = null</c> default across four
    /// restore entry points, where "no ledger recorded" and "I forgot to pass the ledger"
    /// were the same expression and produced the same silent coin-balance loss.
    /// </remarks>
    public sealed class HeroCoinLedgerRestoreStateHolder
    {
        private static readonly HeroCoinLedgerRestoreStateHolder AbsentHolder =
            new HeroCoinLedgerRestoreStateHolder(null);

        private HeroCoinLedgerRestoreStateHolder(HeroCoinLedgerRestoreState state)
        {
            State = state;
        }

        public bool IsPresent => State != null;

        /// <summary>The ledger state, or <c>null</c> when <see cref="IsPresent"/> is false.</summary>
        public HeroCoinLedgerRestoreState State { get; }

        public static HeroCoinLedgerRestoreStateHolder Absent() => AbsentHolder;

        public static HeroCoinLedgerRestoreStateHolder Present(HeroCoinLedgerRestoreState state) =>
            new HeroCoinLedgerRestoreStateHolder(state ?? throw new ArgumentNullException(nameof(state)));

        public void Deconstruct(out bool isPresent, out HeroCoinLedgerRestoreState state)
        {
            isPresent = IsPresent;
            state = State;
        }
    }
}
