using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Cards
{
    /// <summary>The round cursor: where the match currently is.</summary>
    public sealed class RoundCursorRestoreState
    {
        public RoundCursorRestoreState(
            int roundIndex,
            int turnIndex,
            RoundResolutionPhase phase,
            TieBreakerState tieBreakerState,
            GuardsDigital.Core.Units.Heroes.HeroId soloVictoryHeroId = default,
            TeamId teamVictoryTeamId = default,
            IEnumerable<GuardsDigital.Core.Rules.Victory.TeamVictoryTally> teamVictoryTallies = null)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Phase = phase;
            TieBreakerState = tieBreakerState;
            SoloVictoryHeroId = soloVictoryHeroId;
            TeamVictoryTeamId = teamVictoryTeamId;
            TeamVictoryTallies = Array.AsReadOnly((teamVictoryTallies ??
                Enumerable.Empty<GuardsDigital.Core.Rules.Victory.TeamVictoryTally>()).ToArray());
        }

        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public TieBreakerState TieBreakerState { get; }

        /// <summary>
        /// The hero who won the match alone, or the named absence. Milestone 8.39g.
        ///
        /// <para><b>This is the one victory fact in the build that is RECORDED rather than derived,
        /// and it sits beside the phase because the phase is the other half of the same sentence.</b>
        /// The three team endings are re-read on restore from the Lane-Push transition and p.13's life
        /// counters, which is what <c>MatchOutcomeQuery</c>'s own remarks say a fourth record would
        /// only be able to agree with or be wrong about. Cutter's A Fistful of Coins has no such
        /// source: thirteen coins is not a hero who won - coins are farmed by everyone and spent back
        /// down at p.15's Level Up - so what won the match is that this hero RESOLVED that branch while
        /// holding thirteen or more, and nothing else in the match records it.</para>
        /// </summary>
        public GuardsDigital.Core.Units.Heroes.HeroId SoloVictoryHeroId { get; }

        /// <summary>
        /// Milestone 9.30g. The team a printed team victory was claimed for (Gydion's third Wish), or
        /// the named absence - recorded beside <see cref="SoloVictoryHeroId"/> for its reason: the
        /// Wish that won lies faceup only until the next refill, so nothing else records it.
        /// </summary>
        public TeamId TeamVictoryTeamId { get; }

        /// <summary>
        /// Milestone 9.30g. Every team's count toward that victory, ordered by team id - match-scoped,
        /// public, and surviving rounds and Defeats (GY-Q10). Empty until a team first counts.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.Victory.TeamVictoryTally> TeamVictoryTallies { get; }

        public void Deconstruct(
            out int roundIndex,
            out int turnIndex,
            out RoundResolutionPhase phase,
            out TieBreakerState tieBreakerState)
        {
            roundIndex = RoundIndex;
            turnIndex = TurnIndex;
            phase = Phase;
            tieBreakerState = TieBreakerState;
        }
    }

    /// <summary>
    /// The round's ordered turn slots, current hero-Defeat history, and completed-round history.
    /// </summary>
    public sealed class RoundHistoryRestoreState
    {
        public RoundHistoryRestoreState(
            IEnumerable<RoundTurnSlot> turnSlots,
            RoundHeroDefeatHistoryRestoreState heroDefeats,
            IEnumerable<CompletedRoundRecord> completedRounds)
        {
            TurnSlots = Array.AsReadOnly(
                (turnSlots ?? throw new ArgumentNullException(nameof(turnSlots))).ToArray());
            HeroDefeats = heroDefeats ?? throw new ArgumentNullException(nameof(heroDefeats));
            CompletedRounds = Array.AsReadOnly(
                (completedRounds ?? throw new ArgumentNullException(nameof(completedRounds))).ToArray());
        }

        public IReadOnlyList<RoundTurnSlot> TurnSlots { get; }
        public RoundHeroDefeatHistoryRestoreState HeroDefeats { get; }
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds { get; }

        public void Deconstruct(
            out IReadOnlyList<RoundTurnSlot> turnSlots,
            out RoundHeroDefeatHistoryRestoreState heroDefeats,
            out IReadOnlyList<CompletedRoundRecord> completedRounds)
        {
            turnSlots = TurnSlots;
            heroDefeats = HeroDefeats;
            completedRounds = CompletedRounds;
        }
    }

    /// <summary>
    /// The end-of-round stage cursor, its recorded outcomes, and both pending
    /// continuations.
    /// </summary>
    /// <remarks>
    /// <see cref="PendingMinionBattleChoice"/> and <see cref="PendingLanePushRequest"/> are
    /// the two fields finding <c>F613-07</c> was actually about. Milestone 6.1 added them
    /// as defaulted restore parameters, so a caller that omitted them restored a round
    /// that had silently lost its pending Minion Battle choice - the first real
    /// end-of-round player choice in the game.
    /// </remarks>
    public sealed class EndOfRoundRestoreState
    {
        public EndOfRoundRestoreState(
            EndOfRoundStage? currentStage,
            IEnumerable<EndOfRoundStageOutcome> outcomes,
            PendingMinionBattleChoice pendingMinionBattleChoice,
            PendingLanePushRequest pendingLanePushRequest,
            bool preMinionBattleTriggersVisited = false)
        {
            CurrentStage = currentStage;
            Outcomes = Array.AsReadOnly(
                (outcomes ?? throw new ArgumentNullException(nameof(outcomes))).ToArray());
            PendingMinionBattleChoice = pendingMinionBattleChoice;
            PendingLanePushRequest = pendingLanePushRequest;
            PreMinionBattleTriggersVisited = preMinionBattleTriggersVisited;
        }

        public EndOfRoundStage? CurrentStage { get; }
        public IReadOnlyList<EndOfRoundStageOutcome> Outcomes { get; }
        public PendingMinionBattleChoice PendingMinionBattleChoice { get; }
        public PendingLanePushRequest PendingLanePushRequest { get; }

        /// <summary>
        /// Milestone 8.19c1, schema v27. Whether this round has already visited the moment before its
        /// Minion Battle is planned, at which an Active effect declaring
        /// <see cref="GuardsDigital.Core.Timing.TimingCheckpoint.EndOfRoundBeforeMinionBattle"/> runs.
        ///
        /// <para>It is a fact about the round rather than about any effect, which is why it is here and
        /// not on the lifecycle. Nothing else records it: the stage cursor says the round is at its
        /// Minion Battle, and a trigger that stopped on a question leaves that cursor exactly where it
        /// was - so a restore without this would run the moment a second time and fire every card that
        /// declared it again, on a battle it had already interrupted once.</para>
        ///
        /// <para>It is not a stage outcome and not a stage. p.8 lists the stages a round runs and this
        /// is not one of them; it is a moment inside the first, before that stage has counted anything.
        /// A stage would have had to appear in every completed round's record and in the persisted
        /// cursor, which would make a card's trigger window part of the shape of a round.</para>
        /// </summary>
        public bool PreMinionBattleTriggersVisited { get; }

        public void Deconstruct(
            out EndOfRoundStage? currentStage,
            out IReadOnlyList<EndOfRoundStageOutcome> outcomes,
            out PendingMinionBattleChoice pendingMinionBattleChoice,
            out PendingLanePushRequest pendingLanePushRequest)
        {
            currentStage = CurrentStage;
            outcomes = Outcomes;
            pendingMinionBattleChoice = PendingMinionBattleChoice;
            pendingLanePushRequest = PendingLanePushRequest;
        }
    }

    /// <summary>
    /// The round-level card-action block: whether the round owns a card-action authority
    /// at all, and the coin ledger recorded against it.
    /// </summary>
    public sealed class RoundCardActionsRestoreState
    {
        public RoundCardActionsRestoreState(
            bool hasCardActions,
            HeroCoinLedgerRestoreStateHolder coinLedger)
        {
            HasCardActions = hasCardActions;
            CoinLedger = coinLedger ?? throw new ArgumentNullException(nameof(coinLedger));
        }

        public bool HasCardActions { get; }
        public HeroCoinLedgerRestoreStateHolder CoinLedger { get; }

        public void Deconstruct(
            out bool hasCardActions,
            out HeroCoinLedgerRestoreStateHolder coinLedger)
        {
            hasCardActions = HasCardActions;
            coinLedger = CoinLedger;
        }
    }

    /// <summary>
    /// The complete persisted state of a <see cref="RoundResolutionAuthority"/>.
    /// </summary>
    /// <remarks>
    /// Split into nested states along the same seams the snapshot writer already uses, so
    /// that every <c>Deconstruct</c> stays a readable arity while a newly added persisted
    /// field still breaks the writer on arity rather than being silently left unwritten.
    ///
    /// The constructed collaborators a round is restored *with* - the board, the shared
    /// Active-effect lifecycle, and the commitment/turn/card-action authorities - are
    /// required arguments on <see cref="RoundResolutionAuthority.Restore"/> rather than
    /// members here. They are objects assembled from their own restore states, not fields
    /// this round writes.
    /// </remarks>
    public sealed class RoundResolutionRestoreState
    {
        public RoundResolutionRestoreState(
            RoundCursorRestoreState cursor,
            RoundHistoryRestoreState history,
            EndOfRoundRestoreState endOfRound,
            RoundCardActionsRestoreState cardActions,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState systemTransitions,
            GuardsDigital.Core.Rules.Progression.HeroProgressionRestoreState heroProgression,
            GuardsDigital.Core.Rules.Progression.HeroItemRestoreState heroItems,
            GuardsDigital.Core.Rules.Consequences.TeamLifeRestoreState teamLives,
            GuardsDigital.Core.Rules.Progression.RoundLevelingRestoreState roundLeveling,
            GuardsDigital.Core.Rules.Victory.MatchScoreboardRestoreState scoreboard)
        {
            Cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
            History = history ?? throw new ArgumentNullException(nameof(history));
            EndOfRound = endOfRound ?? throw new ArgumentNullException(nameof(endOfRound));
            CardActions = cardActions ?? throw new ArgumentNullException(nameof(cardActions));
            SystemTransitions = systemTransitions ?? throw new ArgumentNullException(nameof(systemTransitions));
            HeroProgression = heroProgression ?? throw new ArgumentNullException(nameof(heroProgression));
            HeroItems = heroItems ?? throw new ArgumentNullException(nameof(heroItems));
            TeamLives = teamLives ?? throw new ArgumentNullException(nameof(teamLives));
            RoundLeveling = roundLeveling ?? throw new ArgumentNullException(nameof(roundLeveling));
            Scoreboard = scoreboard ?? throw new ArgumentNullException(nameof(scoreboard));
        }

        public RoundCursorRestoreState Cursor { get; }
        public RoundHistoryRestoreState History { get; }
        public EndOfRoundRestoreState EndOfRound { get; }
        public RoundCardActionsRestoreState CardActions { get; }

        /// <summary>Milestone 6.10a's paused system transition, or the absence of one.</summary>
        public GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState SystemTransitions { get; }

        /// <summary>
        /// Milestone 7.1's per-hero Level. Required rather than optional and with no absent form,
        /// because every match has heroes and every hero has a Level from p.15's first sentence: an
        /// optional field here would have let a caller restore a round whose heroes had no Level, which
        /// is the shape finding <c>F613-07</c> was about for the two pending end-of-round fields.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.HeroProgressionRestoreState HeroProgression { get; }

        /// <summary>
        /// Milestone 7.2's per-hero items. Required for the reason the Level beside it is, though the
        /// two absences differ: a hero with no Level is impossible, while a hero with no items is the
        /// ordinary case. What is refused here is the payload that carries no item section at all, which
        /// would be a round whose items nobody wrote rather than a round whose heroes have none.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.HeroItemRestoreState HeroItems { get; }

        /// <summary>
        /// Milestone 7.4's per-team Life counters. Required and with no absent form, for the reason the
        /// Level is: every match has teams and p.13 gives every team counters from setup, so a payload
        /// carrying no counter section would be a round whose Life nobody wrote rather than a round
        /// whose teams have none. A team recorded at zero is a legal value and the one p.13 produces.
        /// </summary>
        public GuardsDigital.Core.Rules.Consequences.TeamLifeRestoreState TeamLives { get; }

        /// <summary>
        /// Milestone 7.6's record of which heroes bought a level this round. Required and with no absent
        /// form, for the reason the three states above are: every round has an answer to p.8's question,
        /// and "nobody levelled" is a legal value rather than a missing section. It is the only one of
        /// the four that is round-scoped, so the empty list is both the common case and the one a fresh
        /// round starts from.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.RoundLevelingRestoreState RoundLeveling { get; }

        /// <summary>
        /// Milestone 8.53's per-hero match scoreboard. Required and with no absent form, for the
        /// reason the Level and the Life counters above it are: every match has heroes and every
        /// hero has a line from the first command, so a payload carrying no scoreboard section would
        /// be a round whose totals nobody wrote rather than a round whose heroes have none. It is
        /// the only member here that is match-scoped AND accumulated - the round-scoped leveling
        /// record beside it answers a question about one round, and this one answers a question
        /// about the whole match, which is why the round boundary resets that one and not this.
        /// </summary>
        public GuardsDigital.Core.Rules.Victory.MatchScoreboardRestoreState Scoreboard { get; }

        public void Deconstruct(
            out RoundCursorRestoreState cursor,
            out RoundHistoryRestoreState history,
            out EndOfRoundRestoreState endOfRound,
            out RoundCardActionsRestoreState cardActions,
            out GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState systemTransitions,
            out GuardsDigital.Core.Rules.Progression.HeroProgressionRestoreState heroProgression,
            out GuardsDigital.Core.Rules.Progression.HeroItemRestoreState heroItems,
            out GuardsDigital.Core.Rules.Consequences.TeamLifeRestoreState teamLives,
            out GuardsDigital.Core.Rules.Progression.RoundLevelingRestoreState roundLeveling,
            out GuardsDigital.Core.Rules.Victory.MatchScoreboardRestoreState scoreboard)
        {
            cursor = Cursor;
            history = History;
            endOfRound = EndOfRound;
            cardActions = CardActions;
            systemTransitions = SystemTransitions;
            heroProgression = HeroProgression;
            heroItems = HeroItems;
            teamLives = TeamLives;
            roundLeveling = RoundLeveling;
            scoreboard = Scoreboard;
        }
    }
}
