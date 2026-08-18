using System;
using System.Collections.Generic;
using System.Linq;
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
            TieBreakerState tieBreakerState)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Phase = phase;
            TieBreakerState = tieBreakerState;
        }

        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public TieBreakerState TieBreakerState { get; }

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

    /// <summary>The round's ordered turn slots and its completed-round history.</summary>
    public sealed class RoundHistoryRestoreState
    {
        public RoundHistoryRestoreState(
            IEnumerable<RoundTurnSlot> turnSlots,
            IEnumerable<CompletedRoundRecord> completedRounds)
        {
            TurnSlots = Array.AsReadOnly(
                (turnSlots ?? throw new ArgumentNullException(nameof(turnSlots))).ToArray());
            CompletedRounds = Array.AsReadOnly(
                (completedRounds ?? throw new ArgumentNullException(nameof(completedRounds))).ToArray());
        }

        public IReadOnlyList<RoundTurnSlot> TurnSlots { get; }
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds { get; }

        public void Deconstruct(
            out IReadOnlyList<RoundTurnSlot> turnSlots,
            out IReadOnlyList<CompletedRoundRecord> completedRounds)
        {
            turnSlots = TurnSlots;
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
            PendingLanePushRequest pendingLanePushRequest)
        {
            CurrentStage = currentStage;
            Outcomes = Array.AsReadOnly(
                (outcomes ?? throw new ArgumentNullException(nameof(outcomes))).ToArray());
            PendingMinionBattleChoice = pendingMinionBattleChoice;
            PendingLanePushRequest = pendingLanePushRequest;
        }

        public EndOfRoundStage? CurrentStage { get; }
        public IReadOnlyList<EndOfRoundStageOutcome> Outcomes { get; }
        public PendingMinionBattleChoice PendingMinionBattleChoice { get; }
        public PendingLanePushRequest PendingLanePushRequest { get; }

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
    /// at all, and the Defeat rewards recorded against it.
    /// </summary>
    public sealed class RoundCardActionsRestoreState
    {
        public RoundCardActionsRestoreState(
            bool hasCardActions,
            DefeatRewardRestoreStateHolder defeatRewards)
        {
            HasCardActions = hasCardActions;
            DefeatRewards = defeatRewards ?? throw new ArgumentNullException(nameof(defeatRewards));
        }

        public bool HasCardActions { get; }
        public DefeatRewardRestoreStateHolder DefeatRewards { get; }

        public void Deconstruct(
            out bool hasCardActions,
            out DefeatRewardRestoreStateHolder defeatRewards)
        {
            hasCardActions = HasCardActions;
            defeatRewards = DefeatRewards;
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
            GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState systemTransitions)
        {
            Cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
            History = history ?? throw new ArgumentNullException(nameof(history));
            EndOfRound = endOfRound ?? throw new ArgumentNullException(nameof(endOfRound));
            CardActions = cardActions ?? throw new ArgumentNullException(nameof(cardActions));
            SystemTransitions = systemTransitions ?? throw new ArgumentNullException(nameof(systemTransitions));
        }

        public RoundCursorRestoreState Cursor { get; }
        public RoundHistoryRestoreState History { get; }
        public EndOfRoundRestoreState EndOfRound { get; }
        public RoundCardActionsRestoreState CardActions { get; }

        /// <summary>Milestone 6.10a's paused system transition, or the absence of one.</summary>
        public GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState SystemTransitions { get; }

        public void Deconstruct(
            out RoundCursorRestoreState cursor,
            out RoundHistoryRestoreState history,
            out EndOfRoundRestoreState endOfRound,
            out RoundCardActionsRestoreState cardActions,
            out GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState systemTransitions)
        {
            cursor = Cursor;
            history = History;
            endOfRound = EndOfRound;
            cardActions = CardActions;
            systemTransitions = SystemTransitions;
        }
    }
}
