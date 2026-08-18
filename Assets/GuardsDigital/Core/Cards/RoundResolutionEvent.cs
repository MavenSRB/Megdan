using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards
{
    public enum RoundResolutionEventKind
    {
        None = 0,
        CommitmentProgressed = 1,
        TurnSlotRecorded = 2,
        TurnResolutionProgressed = 3,
        TurnAdvanced = 4,
        EndOfRoundEntered = 5,
        EndOfRoundStageResolved = 6,
        CardRetrieved = 7,
        ActiveEffectCancellationCheckpoint = 8,
        RoundCompleted = 9,
        NextRoundStarted = 10,
        GameEnded = 11,
        TimingProgressed = 12,
        MinionBattleChoiceRequested = 13,
        MinionBattleSelectionChanged = 14,
        MinionBattleMinionRemoved = 15,
        LanePushRequested = 16,

        /// <summary>Milestone 6.10a: a system transition began, stopped on a step, or finished.</summary>
        SystemTransitionProgressed = 17
    }

    public readonly struct RoundResolutionEvent
    {
        private RoundResolutionEvent(
            RoundResolutionEventKind kind,
            int roundIndex,
            int turnIndex,
            TurnCardCommitmentEvent commitmentEvent = default,
            TurnResolutionEvent turnEvent = default,
            RoundTurnSlot turnSlot = null,
            EndOfRoundStage? endOfRoundStage = null,
            EndOfRoundStageOutcomeKind? endOfRoundOutcomeKind = null,
            CardInstanceId? cardInstanceId = null,
            HeroId? cardOwnerId = null,
            CompletedRoundRecord completedRound = null,
            ActiveEffectLifecycleEvent activeEffectEvent = default,
            EndOfRoundResolutionId endOfRoundResolutionId = default,
            EndOfRoundChoiceId endOfRoundChoiceId = default,
            TeamId teamId = default,
            TeamSide teamSide = TeamSide.None,
            MinionId minionId = default,
            RuleObject unit = default,
            LanePushRequestId lanePushRequestId = default,
            MinionBattleChoiceCommandKind minionBattleChoiceCommandKind = MinionBattleChoiceCommandKind.None,
            GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep systemTransitionStep = null,
            bool systemTransitionCompleted = false)
        {
            Kind = kind;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            CommitmentEvent = commitmentEvent;
            TurnEvent = turnEvent;
            TurnSlot = turnSlot;
            EndOfRoundStage = endOfRoundStage;
            EndOfRoundOutcomeKind = endOfRoundOutcomeKind;
            CardInstanceId = cardInstanceId;
            CardOwnerId = cardOwnerId;
            CompletedRound = completedRound;
            ActiveEffectEvent = activeEffectEvent;
            EndOfRoundResolutionId = endOfRoundResolutionId;
            EndOfRoundChoiceId = endOfRoundChoiceId;
            TeamId = teamId;
            TeamSide = teamSide;
            Unit = unit.IsDefined
                ? unit
                : minionId.IsDefined ? RuleObject.ForMinion(minionId) : RuleObject.None;
            MinionId = Unit.Kind == RuleObjectKind.Minion ? Unit.MinionId : default;
            LanePushRequestId = lanePushRequestId;
            MinionBattleChoiceCommandKind = minionBattleChoiceCommandKind;
            SystemTransitionStep = systemTransitionStep;
            SystemTransitionCompleted = systemTransitionCompleted;
        }

        public RoundResolutionEventKind Kind { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public TurnCardCommitmentEvent CommitmentEvent { get; }
        public TurnResolutionEvent TurnEvent { get; }
        public RoundTurnSlot TurnSlot { get; }
        public EndOfRoundStage? EndOfRoundStage { get; }
        public EndOfRoundStageOutcomeKind? EndOfRoundOutcomeKind { get; }
        public CardInstanceId? CardInstanceId { get; }
        public HeroId? CardOwnerId { get; }
        public CompletedRoundRecord CompletedRound { get; }
        public ActiveEffectLifecycleEvent ActiveEffectEvent { get; }
        public EndOfRoundResolutionId EndOfRoundResolutionId { get; }
        public EndOfRoundChoiceId EndOfRoundChoiceId { get; }
        public TeamId TeamId { get; }
        public TeamSide TeamSide { get; }
        public MinionId MinionId { get; }
        public HeroId HeroId => Unit.Kind == RuleObjectKind.Hero ? Unit.HeroId : default;
        public RuleObject Unit { get; }
        public LanePushRequestId LanePushRequestId { get; }
        public MinionBattleChoiceCommandKind MinionBattleChoiceCommandKind { get; }

        /// <summary>Where the transition is now stopped, or <c>null</c> when it is not stopped.</summary>
        public GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep SystemTransitionStep { get; }

        public bool SystemTransitionCompleted { get; }

        public static RoundResolutionEvent FromCommitment(
            int roundIndex,
            int turnIndex,
            TurnCardCommitmentEvent commitmentEvent)
        {
            if (commitmentEvent.Kind == TurnCardCommitmentEventKind.None)
            {
                throw new ArgumentException("A round commitment event requires a defined nested event.");
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.CommitmentProgressed,
                roundIndex,
                turnIndex,
                commitmentEvent: commitmentEvent);
        }

        public static RoundResolutionEvent SlotRecorded(RoundTurnSlot turnSlot)
        {
            if (turnSlot == null)
            {
                throw new ArgumentNullException(nameof(turnSlot));
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.TurnSlotRecorded,
                turnSlot.RoundIndex,
                turnSlot.TurnIndex,
                turnSlot: turnSlot);
        }

        public static RoundResolutionEvent FromTurn(
            int roundIndex,
            int turnIndex,
            TurnResolutionEvent turnEvent)
        {
            if (turnEvent.Kind == TurnResolutionEventKind.None)
            {
                throw new ArgumentException("A round turn event requires a defined nested event.");
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.TurnResolutionProgressed,
                roundIndex,
                turnIndex,
                turnEvent: turnEvent);
        }

        public static RoundResolutionEvent Boundary(
            RoundResolutionEventKind kind,
            int roundIndex,
            int turnIndex)
        {
            if (kind != RoundResolutionEventKind.TurnAdvanced &&
                kind != RoundResolutionEventKind.EndOfRoundEntered)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new RoundResolutionEvent(kind, roundIndex, turnIndex);
        }

        public static RoundResolutionEvent EndOfRoundStageOutcome(
            int roundIndex,
            EndOfRoundStage stage,
            EndOfRoundStageOutcomeKind outcomeKind)
        {
            return new RoundResolutionEvent(
                RoundResolutionEventKind.EndOfRoundStageResolved,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: stage,
                endOfRoundOutcomeKind: outcomeKind);
        }

        public static RoundResolutionEvent RetrievedCard(
            int roundIndex,
            EndOfRoundStage stage,
            CardRetrievalFact retrieval)
        {
            return new RoundResolutionEvent(
                RoundResolutionEventKind.CardRetrieved,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: stage,
                endOfRoundOutcomeKind: EndOfRoundStageOutcomeKind.Completed,
                cardInstanceId: retrieval.CardInstanceId,
                cardOwnerId: retrieval.OwnerId);
        }

        public static RoundResolutionEvent EndOfRoundBoundary(
            RoundResolutionEventKind kind,
            int roundIndex,
            EndOfRoundStage stage)
        {
            if (kind != RoundResolutionEventKind.ActiveEffectCancellationCheckpoint &&
                kind != RoundResolutionEventKind.GameEnded)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new RoundResolutionEvent(
                kind,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: stage);
        }

        public static RoundResolutionEvent CompletedRoundBoundary(
            RoundResolutionEventKind kind,
            CompletedRoundRecord completedRound,
            int currentRoundIndex)
        {
            if (completedRound == null)
            {
                throw new ArgumentNullException(nameof(completedRound));
            }

            if (kind != RoundResolutionEventKind.RoundCompleted &&
                kind != RoundResolutionEventKind.NextRoundStarted)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new RoundResolutionEvent(
                kind,
                currentRoundIndex,
                kind == RoundResolutionEventKind.NextRoundStarted
                    ? 1
                    : RoundResolutionAuthority.TurnsPerRound,
                completedRound: completedRound);
        }

        public static RoundResolutionEvent FromTiming(
            int roundIndex,
            int turnIndex,
            ActiveEffectLifecycleEvent activeEffectEvent)
        {
            if (!activeEffectEvent.EffectId.IsDefined || activeEffectEvent.Checkpoint == TimingCheckpoint.None)
                throw new ArgumentException("A round timing event requires a defined effect and checkpoint.", nameof(activeEffectEvent));
            return new RoundResolutionEvent(
                RoundResolutionEventKind.TimingProgressed,
                roundIndex,
                turnIndex,
                activeEffectEvent: activeEffectEvent);
        }

        public static RoundResolutionEvent MinionBattleChoiceRequested(
            int roundIndex,
            PendingMinionBattleChoice choice)
        {
            if (choice == null)
            {
                throw new ArgumentNullException(nameof(choice));
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.MinionBattleChoiceRequested,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: global::GuardsDigital.Core.Cards.EndOfRoundStage.MinionBattle,
                endOfRoundResolutionId: choice.ResolutionId,
                endOfRoundChoiceId: choice.ChoiceId,
                teamId: choice.LosingTeamId,
                teamSide: choice.ChoosingSide);
        }

        public static RoundResolutionEvent MinionBattleSelectionChanged(
            int roundIndex,
            PendingMinionBattleChoice choice,
            RuleObject unit,
            MinionBattleChoiceCommandKind commandKind)
        {
            if (choice == null || !unit.IsDefined ||
                (commandKind != MinionBattleChoiceCommandKind.Select &&
                    commandKind != MinionBattleChoiceCommandKind.Deselect))
            {
                throw new ArgumentException("A Minion Battle selection event requires choice and unit identity.");
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.MinionBattleSelectionChanged,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: global::GuardsDigital.Core.Cards.EndOfRoundStage.MinionBattle,
                endOfRoundResolutionId: choice.ResolutionId,
                endOfRoundChoiceId: choice.ChoiceId,
                teamId: choice.LosingTeamId,
                teamSide: choice.ChoosingSide,
                unit: unit,
                minionBattleChoiceCommandKind: commandKind);
        }

        public static RoundResolutionEvent MinionBattleUnitRemoved(
            int roundIndex,
            EndOfRoundResolutionId resolutionId,
            TeamId losingTeamId,
            TeamSide losingSide,
            RuleObject unit)
        {
            return new RoundResolutionEvent(
                RoundResolutionEventKind.MinionBattleMinionRemoved,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: global::GuardsDigital.Core.Cards.EndOfRoundStage.MinionBattle,
                endOfRoundResolutionId: resolutionId,
                teamId: losingTeamId,
                teamSide: losingSide,
                unit: unit);
        }

        public static RoundResolutionEvent LanePushRequested(
            int roundIndex,
            PendingLanePushRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.LanePushRequested,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: global::GuardsDigital.Core.Cards.EndOfRoundStage.MinionBattle,
                endOfRoundResolutionId: request.CauseResolutionId,
                teamId: request.LosingTeamId,
                teamSide: request.LosingSide,
                lanePushRequestId: request.RequestId);
        }

        /// <summary>
        /// Milestone 6.10a. One event kind covers began, stopped and finished, because the fact a
        /// reader needs is the same in all three: where the transition is now. A paused transition
        /// names its step and its chooser; a finished one names neither, which is what "finished" is.
        /// </summary>
        public static RoundResolutionEvent SystemTransitionProgressed(
            int roundIndex,
            int turnIndex,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Accepted)
            {
                throw new ArgumentException(
                    "A refused transition command produces no event; it left nothing to report.",
                    nameof(result));
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.SystemTransitionProgressed,
                roundIndex,
                turnIndex,
                teamSide: result.PendingStep?.ChoosingSide ?? TeamSide.None,
                systemTransitionStep: result.PendingStep,
                systemTransitionCompleted: result.TransitionCompleted);
        }
    }
}
