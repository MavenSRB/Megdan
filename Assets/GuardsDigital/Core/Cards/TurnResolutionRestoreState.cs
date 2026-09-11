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
            IEnumerable<string> consumedTurnScopedKeys = null,
            PendingTurnRelease pendingTurnRelease = null)
        {
            IsStarted = isStarted;
            IsEnded = isEnded;
            TieBreakerState = tieBreakerState;
            Initiative = initiative ?? throw new ArgumentNullException(nameof(initiative));
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            ConsumedTurnScopedKeys = Array.AsReadOnly(
                (consumedTurnScopedKeys ?? Enumerable.Empty<string>()).ToArray());
            PendingTurnRelease = pendingTurnRelease;
        }

        public bool IsStarted { get; }
        public bool IsEnded { get; }
        public TieBreakerState TieBreakerState { get; }
        public InitiativeResolutionRestoreState Initiative { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }

        /// <summary>
        /// The keys a turn has consumed, in the order they were taken. Two kinds of fact ride here,
        /// and both are true of THE TURN rather than of any resolution inside it, which is why one set
        /// holds them.
        ///
        /// <para>Milestone 8.19a wrote the first: the `before-action-trigger@1` sources that have
        /// already fired this turn, by stable source identity. Milestone 8.38d added the second:
        /// <see cref="GuardsDigital.Core.Rules.Progression.GrantedTargetExclusionQuery"/>'s
        /// granted-target exclusions, which namespace their keys so the two readers cannot collide.
        /// Sharing the set is what keeps the second kind format-identical on the wire, and so free of
        /// a snapshot revision.</para>
        ///
        /// <para>Empty on every turn that consumed neither, which is every turn in a build whose
        /// heroes unlock no such ability.</para>
        /// </summary>
        public IReadOnlyList<string> ConsumedTurnScopedKeys { get; }

        /// <summary>
        /// Milestone 8.35g. The finished turn this state was captured waiting to hand on, or
        /// <c>null</c> when nothing was waiting. It is the fifth legitimate shape a started,
        /// unended turn can be captured in, and the first in which nobody is selected and nothing
        /// is pending because the turn is complete rather than interrupted.
        /// </summary>
        public PendingTurnRelease PendingTurnRelease { get; }

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
