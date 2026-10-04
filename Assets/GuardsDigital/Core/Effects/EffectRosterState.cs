using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects
{
    /// <summary>A narrow round-owned edit of a recorded, resolved turn-card slot.</summary>
    public interface IResolvedTurnSlotSwap
    {
        bool TrySwapResolvedWithHand(
            HeroId owner, CardInstanceId resolvedCard, CardInstanceId handCard);
    }

    public interface IItemCardSwap
    {
        bool TrySwapItemWithCard(
            HeroId owner, CardInstanceId itemCard, CardInstanceId replacementCard);
    }

    /// <summary>
    /// Milestone 9.32a. A round-owned p.10 card swap with the owner's deck (Takahide's gold cards and
    /// Bushido): both cards' states, the turn slot that names the card, and - when the card is the one
    /// resolving its own action - the Initiative's selected card, in one operation (lesson 28).
    /// </summary>
    public interface IDeckCardSwap
    {
        bool TrySwapWithDeckCard(
            HeroId owner, CardInstanceId card, CardInstanceId deckCard, bool restingResultFacedown);
    }

    /// <summary>
    /// Milestone 9.32b. A round-owned p.10 swap of a hero's unresolved card (Takahide's Set an Example:
    /// "A friendly hero in radius may swap their unresolved card with a card in their hand"): both
    /// cards' states, this turn's slot and the revealed record in one operation (lesson 28). The turn
    /// order reads the new card because it reads unresolved cards live (p.7).
    /// </summary>
    public interface IUnresolvedCardSwap
    {
        bool TrySwapUnresolvedCard(HeroId owner, CardInstanceId unresolvedCard, CardInstanceId partnerCard);
    }

    public interface ICurrentTurnCardHistory
    {
        bool HasResolvedCardThisTurn(HeroId heroId);
    }

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
        private readonly GuardsDigital.Core.Rules.Victory.ISoloVictoryClaim soloVictory;
        private readonly IResolvedTurnSlotSwap resolvedTurnSlotSwap;
        private readonly IItemCardSwap itemCardSwap;
        private readonly GuardsDigital.Core.Rules.Victory.ITeamVictoryTally teamVictory;
        private readonly TeamLifeState teamLives;
        private readonly IDeckCardSwap deckCardSwap;
        private readonly IUnresolvedCardSwap unresolvedCardSwap;
        private readonly ICurrentTurnCardHistory currentTurnCardHistory;

        private EffectRosterState()
        {
        }

        private EffectRosterState(
            IReadOnlyList<RoundTurnSlot> turnSlots,
            RoundHeroDefeatHistory heroDefeats,
            HeroCoinLedger coins,
            GuardsDigital.Core.Rules.Victory.ISoloVictoryClaim soloVictory,
            IResolvedTurnSlotSwap resolvedTurnSlotSwap,
            IItemCardSwap itemCardSwap,
            GuardsDigital.Core.Rules.Victory.ITeamVictoryTally teamVictory,
            TeamLifeState teamLives,
            IDeckCardSwap deckCardSwap,
            IUnresolvedCardSwap unresolvedCardSwap,
            ICurrentTurnCardHistory currentTurnCardHistory)
        {
            this.turnSlots = turnSlots;
            this.heroDefeats = heroDefeats;
            this.coins = coins;
            this.soloVictory = soloVictory;
            this.resolvedTurnSlotSwap = resolvedTurnSlotSwap;
            this.itemCardSwap = itemCardSwap;
            this.teamVictory = teamVictory;
            this.teamLives = teamLives;
            this.deckCardSwap = deckCardSwap;
            this.unresolvedCardSwap = unresolvedCardSwap;
            this.currentTurnCardHistory = currentTurnCardHistory;
        }

        /// <summary>
        /// A stated absence. Reading any authority from it throws so missing state cannot masquerade as
        /// an empty history or a zero balance.
        /// </summary>
        public static EffectRosterState Absent => AbsentState;

        /// <summary>
        /// Milestone 8.39g1 added <paramref name="soloVictory"/>, and it is optional for the reason
        /// every optional parameter in this build is: a caller written before it keeps exactly the
        /// state it had. Absence is a match no printed card can claim outright - a fixture composing
        /// this state without one, or a restore reading a payload that never carried the authority -
        /// and <see cref="SoloVictory"/> reports it as the named absence rather than as a claim that
        /// silently does nothing.
        /// </summary>
        public static EffectRosterState Of(
            IReadOnlyList<RoundTurnSlot> turnSlots,
            RoundHeroDefeatHistory heroDefeats,
            HeroCoinLedger coins,
            GuardsDigital.Core.Rules.Victory.ISoloVictoryClaim soloVictory = null,
            IResolvedTurnSlotSwap resolvedTurnSlotSwap = null,
            IItemCardSwap itemCardSwap = null,
            GuardsDigital.Core.Rules.Victory.ITeamVictoryTally teamVictory = null,
            TeamLifeState teamLives = null,
            IDeckCardSwap deckCardSwap = null,
            IUnresolvedCardSwap unresolvedCardSwap = null,
            ICurrentTurnCardHistory currentTurnCardHistory = null) =>
            new EffectRosterState(
                turnSlots ?? throw new ArgumentNullException(nameof(turnSlots)),
                heroDefeats ?? throw new ArgumentNullException(nameof(heroDefeats)),
                coins ?? throw new ArgumentNullException(nameof(coins)),
                soloVictory, resolvedTurnSlotSwap, itemCardSwap, teamVictory, teamLives, deckCardSwap,
                unresolvedCardSwap, currentTurnCardHistory);

        public bool IsAvailable => turnSlots != null;

        public IReadOnlyList<RoundTurnSlot> TurnSlots => Available(turnSlots);
        public ICurrentTurnCardHistory CurrentTurnCardHistory =>
            IsAvailable ? currentTurnCardHistory : null;

        public RoundHeroDefeatHistory HeroDefeats => Available(heroDefeats);

        public HeroCoinLedger Coins => Available(coins);

        public IResolvedTurnSlotSwap ResolvedTurnSlotSwap =>
            IsAvailable ? resolvedTurnSlotSwap : null;

        public IItemCardSwap ItemCardSwap =>
            IsAvailable ? itemCardSwap : null;

        /// <summary>
        /// Milestone 9.30f. The match's team Life counters, which Gydion's Energy Drain raises ("Your
        /// team regains 1 spent Life counter"), or <c>null</c> where this state was composed without
        /// them - the same legitimate absence <see cref="SoloVictory"/> has, answered by the capability
        /// regaining nothing rather than by throwing. The live state, not a copy: the round hands over
        /// the one <c>TeamLives</c> p.13's spends already write.
        /// </summary>
        public TeamLifeState TeamLives =>
            IsAvailable ? teamLives : null;

        /// <summary>Milestone 9.32a. The round's p.10 swap with the deck, or <c>null</c>.</summary>
        public IDeckCardSwap DeckCardSwap =>
            IsAvailable ? deckCardSwap : null;

        /// <summary>Milestone 9.32b. The round's swap of an unresolved card, or <c>null</c>.</summary>
        public IUnresolvedCardSwap UnresolvedCardSwap =>
            IsAvailable ? unresolvedCardSwap : null;

        /// <summary>
        /// Milestone 8.39g1. The round's solo-victory claim, or <c>null</c> where this state was
        /// composed without one.
        ///
        /// <para>It is the one authority here that may legitimately be absent while the rest are
        /// present, which is why it does not go through <see cref="Available{T}"/>: the other three are
        /// state every round has, and a missing one means a caller forgot to supply the round. A
        /// missing claim means this composition has no round to claim through, which a capability
        /// answers by reporting itself infeasible rather than by throwing at a card that did nothing
        /// wrong.</para>
        /// </summary>
        public GuardsDigital.Core.Rules.Victory.ISoloVictoryClaim SoloVictory =>
            IsAvailable ? soloVictory : null;

        /// <summary>
        /// Milestone 9.30g. The round's team-victory tally and claim (Gydion's Wish), or <c>null</c>
        /// where this state was composed without one - optional on the terms
        /// <see cref="SoloVictory"/> states.
        /// </summary>
        public GuardsDigital.Core.Rules.Victory.ITeamVictoryTally TeamVictory =>
            IsAvailable ? teamVictory : null;

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
