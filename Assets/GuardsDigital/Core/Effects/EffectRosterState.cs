using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Consequences;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// The round-owned rules state roster capabilities may read or mutate, or its named absence.
    ///
    /// <para>The references are live authorities, not copies. Turn slots are appended by the round,
    /// hero-Defeat history is cleared at its boundary, and the coin ledger is the single writer every
    /// reward and purchase already uses. A paused capability therefore re-derives against the same
    /// state on answer, replay and restore without adding a snapshot field.</para>
    /// </summary>
    public sealed class EffectRosterState
    {
        private static readonly EffectRosterState AbsentState = new EffectRosterState();

        private readonly IReadOnlyList<RoundTurnSlot> turnSlots;
        private readonly RoundHeroDefeatHistory heroDefeats;
        private readonly HeroCoinLedger coins;

        private EffectRosterState()
        {
        }

        private EffectRosterState(
            IReadOnlyList<RoundTurnSlot> turnSlots,
            RoundHeroDefeatHistory heroDefeats,
            HeroCoinLedger coins)
        {
            this.turnSlots = turnSlots;
            this.heroDefeats = heroDefeats;
            this.coins = coins;
        }

        /// <summary>
        /// A stated absence. Reading any authority from it throws so missing state cannot masquerade as
        /// an empty history or a zero balance.
        /// </summary>
        public static EffectRosterState Absent => AbsentState;

        public static EffectRosterState Of(
            IReadOnlyList<RoundTurnSlot> turnSlots,
            RoundHeroDefeatHistory heroDefeats,
            HeroCoinLedger coins) =>
            new EffectRosterState(
                turnSlots ?? throw new ArgumentNullException(nameof(turnSlots)),
                heroDefeats ?? throw new ArgumentNullException(nameof(heroDefeats)),
                coins ?? throw new ArgumentNullException(nameof(coins)));

        public bool IsAvailable => turnSlots != null;

        public IReadOnlyList<RoundTurnSlot> TurnSlots => Available(turnSlots);

        public RoundHeroDefeatHistory HeroDefeats => Available(heroDefeats);

        public HeroCoinLedger Coins => Available(coins);

        private T Available<T>(T value)
            where T : class
        {
            if (!IsAvailable)
            {
                throw new InvalidOperationException(
                    "This effect resolution was given no roster rules state. Supply the round's live " +
                    "turn slots, hero-Defeat history and coin ledger instead of treating absence as " +
                    "an empty history or zero balance.");
            }

            return value;
        }
    }
}
