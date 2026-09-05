using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Setup
{
    /// <summary>
    /// The Levels and coin balances a match begins from, when a composition wants any but the ones
    /// a dealt match begins from. Milestone 8.35z17.
    ///
    /// <para><b>Why this exists at all.</b> p.15 opens with "Your hero starts the game at level 1"
    /// and a match deals an empty purse, so until this milestone there was nothing to say and no
    /// place to say it: <c>RoundResolutionAuthority</c> created both states itself. A staged
    /// situation whose whole subject is a p.15 purchase - can this hero afford the next Level, does
    /// the Upgrade a Card step offer what it should at Level 4 - cannot be reached by composing a
    /// board, because neither number is a fact about a board. It is reached either by playing the
    /// rounds that produce it, which is the thing a test environment exists to avoid, or by saying
    /// what the match starts from.</para>
    ///
    /// <para><b>Why it is a setup decision rather than a rules one.</b> This is the argument
    /// <c>RoundResolutionComposition.TeamLives</c> already makes for rulebook p.13's Life counters,
    /// and it is made here unchanged: a starting count travels in the composition "rather than being
    /// created by the authority because the starting count is a setup decision - the prototype
    /// baseline records five per team, the staged 1v1 fixture composes four - and the composed setup
    /// is the one record of it". Levels and coins are the same kind of fact seen again. Nothing here
    /// decides when a Level may move, what it costs, or what it upgrades; those remain entirely
    /// <c>HeroLevelUpStepRules</c>' and the single transition remains
    /// <c>HeroProgressionState.TryRaiseLevel</c>.</para>
    ///
    /// <para><b>What it is not.</b> It is not a second persistence format. Both halves are the
    /// existing restore-state types the snapshot writer already emits, so a staged match and a
    /// restored one install a starting position through exactly one code path each - and because it
    /// is composition rather than restoration, <c>MatchSession</c>'s accepted-command history is
    /// untouched, which is what keeps Undo, restart and deterministic replay working over a staged
    /// board. A snapshot restore clears that history by design; this does not.</para>
    /// </summary>
    public sealed class MatchStagedProgression
    {
        private readonly IReadOnlyList<KeyValuePair<HeroId, HeroLevel>> levels;
        private readonly IReadOnlyList<KeyValuePair<HeroId, int>> coins;

        /// <summary>
        /// Every hero's Level and every hero's coin balance. Both cover the participating heroes
        /// exactly once - which is not checked here but by the two authorities' own
        /// <c>Restore</c> entry points, deliberately: they already refuse a partial cover with the
        /// message that names the rule, and a second check here would be a second answer to what a
        /// legal starting position is.
        /// </summary>
        public MatchStagedProgression(
            IEnumerable<KeyValuePair<HeroId, HeroLevel>> levels,
            IEnumerable<KeyValuePair<HeroId, int>> coins)
        {
            this.levels = Array.AsReadOnly(
                (levels ?? throw new ArgumentNullException(nameof(levels))).ToArray());
            this.coins = Array.AsReadOnly(
                (coins ?? throw new ArgumentNullException(nameof(coins))).ToArray());
        }

        public IReadOnlyList<KeyValuePair<HeroId, HeroLevel>> Levels => levels;

        public IReadOnlyList<KeyValuePair<HeroId, int>> Coins => coins;

        /// <summary>
        /// The Levels as the round authority's own composition vocabulary, or null when this
        /// states none - so an empty staging composes the ordinary Level 1 rather than an empty
        /// cover the authority would refuse.
        /// </summary>
        public HeroProgressionRestoreState CreateProgressionState() =>
            levels.Count == 0 ? null : new HeroProgressionRestoreState(levels);

        /// <summary>
        /// The balances as the coin ledger's own composition vocabulary, or null when this states
        /// none. No Defeat has been awarded in a staged situation - the coins are simply there - so
        /// the awarded-outcome set is empty, which is what stops a staged balance from being
        /// mistaken for a paid-out Defeat and double-awarded later.
        /// </summary>
        public HeroCoinLedgerRestoreState CreateCoinLedgerState() =>
            coins.Count == 0
                ? null
                : new HeroCoinLedgerRestoreState(coins, Array.Empty<DefeatOutcomeId>());

        /// <summary>Nothing staged, which composes exactly what a dealt match composes.</summary>
        public static MatchStagedProgression None { get; } = new MatchStagedProgression(
            Array.Empty<KeyValuePair<HeroId, HeroLevel>>(),
            Array.Empty<KeyValuePair<HeroId, int>>());

        /// <summary>True when this states nothing, so a caller can skip composing it entirely.</summary>
        public bool IsEmpty => levels.Count == 0 && coins.Count == 0;
    }
}
