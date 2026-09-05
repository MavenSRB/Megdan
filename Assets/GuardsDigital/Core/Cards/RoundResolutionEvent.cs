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
        SystemTransitionProgressed = 17,

        /// <summary>Milestone 6.11: one of rulebook p.8's numbered Lane-Push steps ran.</summary>
        LanePushStageResolved = 18,

        /// <summary>Milestone 6.12: p.8's end-of-round cleanup took one token off the board.</summary>
        TokenRemovedAtEndOfRound = 19,

        /// <summary>Milestone 6.12: p.8's end-of-round cleanup returned one marker to its supply.</summary>
        MarkerReturnedAtEndOfRound = 20,

        /// <summary>
        /// Milestone 7.5: one part of p.8's Level Up step happened - a level bought, a card upgraded,
        /// an item gained, or an Ultimate unlocked.
        ///
        /// One kind rather than four, because the four are one page's one transaction and what
        /// distinguishes them is typed on the outcome the event carries. A reader that wants only the
        /// purchases asks the outcome, and nothing has to keep four enum members in agreement with the
        /// four the rules can report.
        /// </summary>
        HeroLevelUpProgressed = 21,

        /// <summary>
        /// Milestone 7.6: one hero collected rulebook p.8's Pity Coin, because it did not level up this
        /// round. A kind of its own rather than a fifth <c>HeroLevelUpOutcomeKind</c>, because this is
        /// the step p.8 places *after* Level Up and its subject is a hero that did not level.
        /// </summary>
        PityCoinCollected = 22,

        /// <summary>
        /// Milestone 8.19c: a declared Minion-Battle rule replaced its own unit's removal, and that
        /// unit's team lost p.8's push instead - Brogan's One Man Army.
        ///
        /// A kind of its own rather than a removal reported with a flag, because the two are opposite
        /// facts: <see cref="MinionBattleMinionRemoved"/> says a miniature left the board, and this
        /// says one did not. A reader counting removals must not have to know which flag meant "no".
        /// </summary>
        MinionBattlePushLost = 23
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
            bool systemTransitionCompleted = false,
            GuardsDigital.Core.Rules.LanePush.LanePushStageOutcome lanePushStageOutcome = default,
            GuardsDigital.Core.Components.PlacedToken removedToken = null,
            GuardsDigital.Core.Components.MarkerAttachment returnedMarker = null,
            GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcome heroLevelUpOutcome = null,
            GuardsDigital.Core.Rules.Consequences.CoinGrantOperation pityCoinGrant = default,
            GuardsDigital.Core.Rules.Victory.MatchOutcome matchOutcome = default)
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
            LanePushStageOutcome = lanePushStageOutcome;
            RemovedToken = removedToken;
            ReturnedMarker = returnedMarker;
            HeroLevelUpOutcome = heroLevelUpOutcome;
            PityCoinGrant = pityCoinGrant;
            MatchOutcome = matchOutcome;
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

        /// <summary>Milestone 6.11: which p.8 Lane-Push step this event reports, and how it ended.</summary>
        public GuardsDigital.Core.Rules.LanePush.LanePushStageOutcome LanePushStageOutcome { get; }

        /// <summary>Milestone 6.12: the token p.8's cleanup took off the board, or <c>null</c>.</summary>
        public GuardsDigital.Core.Components.PlacedToken RemovedToken { get; }

        /// <summary>Milestone 6.12: the attachment p.8's cleanup ended, or <c>null</c>.</summary>
        public GuardsDigital.Core.Components.MarkerAttachment ReturnedMarker { get; }

        /// <summary>Milestone 7.5: what p.15's and p.16's leveling did, or <c>null</c>.</summary>
        public GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcome HeroLevelUpOutcome { get; }

        /// <summary>Milestone 7.6: the p.8 Pity Coin one hero collected, with its balance transition.</summary>
        public GuardsDigital.Core.Rules.Consequences.CoinGrantOperation PityCoinGrant { get; }

        /// <summary>
        /// Milestone 7.7: who won the match and which sentence ended it, on the one event that reports
        /// an ending. <see cref="GuardsDigital.Core.Rules.Victory.MatchOutcome.Unfinished"/> on every
        /// other event, because no other event is about the match ending.
        /// </summary>
        public GuardsDigital.Core.Rules.Victory.MatchOutcome MatchOutcome { get; }

        /// <summary>
        /// Milestone 7.8: whether this event opens a new round's stream of events.
        ///
        /// Exactly one event does - the boundary event that reports the next round beginning, and the
        /// first event of that round that any observer sees - so a reader keeping "this round's
        /// events" has one thing to ask rather than a rule of its own about which kinds end a round.
        /// It is asked at the commit boundary, which is the only place events are appended.
        ///
        /// A round the match ended inside opens no new one, which is why this is the beginning rather
        /// than <see cref="RoundResolutionEventKind.RoundCompleted"/>, the end: the completed round's
        /// own events belong to the round that produced them right up to the moment the next begins.
        /// </summary>
        public bool BeginsCurrentRoundEvents => Kind == RoundResolutionEventKind.NextRoundStarted;

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
            if (kind != RoundResolutionEventKind.ActiveEffectCancellationCheckpoint)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new RoundResolutionEvent(
                kind,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: stage);
        }

        /// <summary>
        /// Milestone 7.7. The match ended, by whichever of the rulebook's three victory sentences ended
        /// it, and this is the one event that says so.
        ///
        /// It replaces two: milestone 6.10's stage boundary, which named the stage the push ended at and
        /// no team at all, and milestone 7.4's <c>GameEndedInTurn</c>, which named the losing team and no
        /// stage. The two shapes were the whole difficulty - a reader had to know which rule had ended
        /// the match before it could ask who won - so both facts are carried here and the stage is the
        /// optional one, because p.13's ending has none: its counters run out inside an Attack.
        /// </summary>
        public static RoundResolutionEvent GameEnded(
            int roundIndex,
            int turnIndex,
            GuardsDigital.Core.Rules.Victory.MatchOutcome outcome,
            EndOfRoundStage? stage)
        {
            if (!outcome.HasEnded)
            {
                throw new ArgumentException(
                    "A game-ended event names the sentence that ended the match.", nameof(outcome));
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.GameEnded,
                roundIndex,
                turnIndex,
                endOfRoundStage: stage,
                teamId: outcome.LosingTeamId,
                matchOutcome: outcome);
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

        /// <summary>
        /// Milestone 8.19c. p.8's removal replaced by its team losing the push. The unit is named
        /// because the rule is that unit's own - "if <em>you</em> would be removed" - and the losing
        /// team is named because that is who the push goes against.
        /// </summary>
        public static RoundResolutionEvent MinionBattlePushLost(
            int roundIndex,
            EndOfRoundResolutionId resolutionId,
            TeamId losingTeamId,
            TeamSide losingSide,
            RuleObject unit)
        {
            return new RoundResolutionEvent(
                RoundResolutionEventKind.MinionBattlePushLost,
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
                teamSide: result.PendingStep?.AwaitedSide ?? TeamSide.None,
                systemTransitionStep: result.PendingStep,
                systemTransitionCompleted: result.TransitionCompleted);
        }

        /// <summary>
        /// Milestone 6.11. The same fact reported by a transition its own rules began: where it is now.
        /// A push begun by a Lane-Push request has no <c>SystemTransitionResult</c> to hand a reader,
        /// because nothing commanded it into existence.
        /// </summary>
        public static RoundResolutionEvent SystemTransitionPaused(
            int roundIndex,
            int turnIndex,
            GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep pendingStep)
        {
            if (pendingStep == null)
            {
                throw new ArgumentNullException(nameof(pendingStep));
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.SystemTransitionProgressed,
                roundIndex,
                turnIndex,
                teamSide: pendingStep.AwaitedSide,
                systemTransitionStep: pendingStep,
                systemTransitionCompleted: false);
        }

        /// <summary>
        /// Milestone 6.11: one of rulebook p.8's numbered Lane-Push steps, and how it ended.
        ///
        /// Reported per stage rather than once per push, because the page numbers them and because an
        /// early game-ending sentence leaves the later ones absent - which is what says they did not
        /// run, rather than that they ran quietly.
        /// </summary>
        public static RoundResolutionEvent LanePushStageResolved(
            int roundIndex,
            int turnIndex,
            GuardsDigital.Core.Rules.LanePush.LanePushStageOutcome outcome)
        {
            return new RoundResolutionEvent(
                RoundResolutionEventKind.LanePushStageResolved,
                roundIndex,
                turnIndex,
                lanePushStageOutcome: outcome);
        }

        /// <summary>
        /// Milestone 6.12: one token left the board at p.8's cleanup. It names the instance and where it
        /// stood, because after the stage the board no longer holds either and the event is the only
        /// record of what was cleared.
        /// </summary>
        public static RoundResolutionEvent TokenRemovedAtEndOfRound(
            int roundIndex,
            GuardsDigital.Core.Components.PlacedToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            return new RoundResolutionEvent(
                RoundResolutionEventKind.TokenRemovedAtEndOfRound,
                roundIndex,
                0,
                endOfRoundStage: Cards.EndOfRoundStage.RemoveTokensAndReturnMarkers,
                removedToken: token);
        }

        /// <summary>
        /// Milestone 6.12: one marker returned to its supply at p.8's cleanup, naming the attachment
        /// that ended so a reader can say which marker left which holder.
        /// </summary>
        public static RoundResolutionEvent MarkerReturnedAtEndOfRound(
            int roundIndex,
            GuardsDigital.Core.Components.MarkerAttachment attachment)
        {
            if (attachment == null) throw new ArgumentNullException(nameof(attachment));
            return new RoundResolutionEvent(
                RoundResolutionEventKind.MarkerReturnedAtEndOfRound,
                roundIndex,
                0,
                endOfRoundStage: Cards.EndOfRoundStage.RemoveTokensAndReturnMarkers,
                returnedMarker: attachment);
        }

        /// <summary>
        /// Milestone 7.5: one part of p.8's Level Up step. It names the stage as well as the outcome,
        /// because the whole of leveling happens inside one end-of-round stage and a reader following
        /// the round's order needs no second lookup to place it.
        /// </summary>
        public static RoundResolutionEvent HeroLevelUpProgressed(
            int roundIndex,
            int turnIndex,
            GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcome outcome)
        {
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            return new RoundResolutionEvent(
                RoundResolutionEventKind.HeroLevelUpProgressed,
                roundIndex,
                turnIndex,
                endOfRoundStage: Cards.EndOfRoundStage.LevelUpAndUpgrade,
                heroLevelUpOutcome: outcome);
        }

        /// <summary>
        /// Milestone 7.6: one hero collected p.8's Pity Coin. It carries the whole balance transition
        /// rather than only the hero, because the page's own consequence is about what that coin can and
        /// cannot then buy, and a reader that had to look the balance up somewhere else could not see it.
        /// </summary>
        public static RoundResolutionEvent PityCoinCollected(
            int roundIndex,
            GuardsDigital.Core.Rules.Consequences.CoinGrantOperation grant)
        {
            return new RoundResolutionEvent(
                RoundResolutionEventKind.PityCoinCollected,
                roundIndex,
                0,
                endOfRoundStage: Cards.EndOfRoundStage.CollectPityCoins,
                pityCoinGrant: grant);
        }
    }
}
