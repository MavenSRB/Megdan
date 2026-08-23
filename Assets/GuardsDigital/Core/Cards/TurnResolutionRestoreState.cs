using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The complete persisted state of a <see cref="TurnResolutionAuthority"/>.
    /// </summary>
    /// <remarks>
    /// Before milestone 6.1g this authority's restore defaulted <c>timing</c> to
    /// <c>null</c> and both cursor indices to <c>1</c>. Omitting the cursor did not fail:
    /// it silently restored the turn as round 1, turn 1, so every Active effect keyed to
    /// the real cursor then evaluated against the wrong window.
    /// </remarks>
    public sealed class TurnResolutionRestoreState
    {
        public TurnResolutionRestoreState(
            bool isStarted,
            bool isEnded,
            TieBreakerState tieBreakerState,
            InitiativeResolutionRestoreState initiative,
            int roundIndex,
            int turnIndex,
            IEnumerable<string> consumedBeforeActionTriggers = null)
        {
            IsStarted = isStarted;
            IsEnded = isEnded;
            TieBreakerState = tieBreakerState;
            Initiative = initiative ?? throw new ArgumentNullException(nameof(initiative));
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            ConsumedBeforeActionTriggers = Array.AsReadOnly(
                (consumedBeforeActionTriggers ?? Enumerable.Empty<string>()).ToArray());
        }

        public bool IsStarted { get; }
        public bool IsEnded { get; }
        public TieBreakerState TieBreakerState { get; }
        public InitiativeResolutionRestoreState Initiative { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }

        /// <summary>
        /// Milestone 8.19a. The `before-action-trigger@1` sources that have already fired this turn, by
        /// stable source identity, in the order they fired. Empty on every turn in which none has -
        /// which is every turn in this build before a hero unlocks one.
        /// </summary>
        public IReadOnlyList<string> ConsumedBeforeActionTriggers { get; }

        public void Deconstruct(
            out bool isStarted,
            out bool isEnded,
            out TieBreakerState tieBreakerState,
            out InitiativeResolutionRestoreState initiative)
        {
            isStarted = IsStarted;
            isEnded = IsEnded;
            tieBreakerState = TieBreakerState;
            initiative = Initiative;
        }

        /// <summary>
        /// The round/turn cursor, which the owning round writes rather than the turn block.
        /// Deconstructed separately so each member still corresponds to exactly one writer.
        /// </summary>
        public void DeconstructCursor(out int roundIndex, out int turnIndex)
        {
            roundIndex = RoundIndex;
            turnIndex = TurnIndex;
        }
    }
}
