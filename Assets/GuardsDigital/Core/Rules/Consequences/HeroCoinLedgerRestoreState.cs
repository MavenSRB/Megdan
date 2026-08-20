using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Consequences
{
    /// <summary>
    /// The complete persisted state of a <see cref="HeroCoinLedger"/>.
    /// </summary>
    /// <remarks>
    /// This carries exactly the fields the snapshot writer emits for the coin ledger.
    /// Reconstruction context that the snapshot stores elsewhere - the participants -
    /// stays a separate required argument on <see cref="HeroCoinLedger.Restore"/>,
    /// so every member deconstructed here corresponds to one written field and nothing
    /// the writer must ignore.
    /// </remarks>
    public sealed class HeroCoinLedgerRestoreState
    {
        public HeroCoinLedgerRestoreState(
            IEnumerable<KeyValuePair<HeroId, int>> coinBalances,
            IEnumerable<DefeatOutcomeId> awardedOutcomeIds)
        {
            CoinBalances = Array.AsReadOnly(
                (coinBalances ?? throw new ArgumentNullException(nameof(coinBalances))).ToArray());
            AwardedOutcomeIds = Array.AsReadOnly(
                (awardedOutcomeIds ?? throw new ArgumentNullException(nameof(awardedOutcomeIds))).ToArray());
        }

        public IReadOnlyList<KeyValuePair<HeroId, int>> CoinBalances { get; }
        public IReadOnlyList<DefeatOutcomeId> AwardedOutcomeIds { get; }

        public void Deconstruct(
            out IReadOnlyList<KeyValuePair<HeroId, int>> coinBalances,
            out IReadOnlyList<DefeatOutcomeId> awardedOutcomeIds)
        {
            coinBalances = CoinBalances;
            awardedOutcomeIds = AwardedOutcomeIds;
        }
    }
}
