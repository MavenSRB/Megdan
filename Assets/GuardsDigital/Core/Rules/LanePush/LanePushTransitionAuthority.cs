using System;
using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.LanePush
{
    /// <summary>
    /// Owns rulebook p.8's shared Wave counters and drives the ordered Lane-Push prefix (milestone
    /// 6.10).
    ///
    /// The Wave counters are one pool rather than a stock per team. p.4: "Whenever a team Pushes the
    /// Lane, a Wave counter is removed. When the last counter is gone, the game ends immediately, and
    /// the team that won the last Push wins the game!" The Quick Game places three.
    ///
    /// This is the one seam through which the prefix can be reached, and reaching it is not something a
    /// live match can do at this milestone. Both Lane-Push requests - the end-of-round one milestone 6.1
    /// records and the mid-turn one milestone 6.9 emits - still stop at their own consumption
    /// checkpoints and say <c>UnimplementedLanePushConsumption</c>, because p.8's third step does not
    /// exist yet and a match that ran the first two would have moved its Battle Zone away from minions
    /// that were never removed or respawned. That is what milestone 6.10's first acceptance check asks
    /// for, and it is held structurally rather than by inspection: <see cref="ResolveOrderedPrefix"/>
    /// names the winning and losing sides, so a request cannot be handed to it, and a conformance rule
    /// proves nothing in the engine calls it at all.
    ///
    /// It is therefore not composed into the match and its Wave counters are not persisted. The current
    /// Battle Zone is, because four live questions read it every time they are asked; nothing a live
    /// match can reach reads a Wave counter, so a persisted one would be a wire field no restore could
    /// ever disagree about. Milestone 6.11 completes the transition, composes this authority, and
    /// persists its state with it.
    /// </summary>
    public sealed class LanePushTransitionAuthority
    {
        private readonly ILaneAdvanceAuthority board;

        public LanePushTransitionAuthority(ILaneAdvanceAuthority board, int waveCounters)
        {
            if (waveCounters <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(waveCounters), waveCounters, "A match starts with at least one Wave counter.");
            }

            this.board = board ?? throw new ArgumentNullException(nameof(board));
            RemainingWaveCounters = waveCounters;
            GameEndingCause = LanePushGameEndingCause.None;
            WinningSide = TeamSide.None;
        }

        /// <summary>Wave counters still face-up.</summary>
        public int RemainingWaveCounters { get; private set; }

        /// <summary>Which of p.8's two victory sentences ended the game, if either has.</summary>
        public LanePushGameEndingCause GameEndingCause { get; private set; }

        /// <summary>The side that won, and only once the game has ended.</summary>
        public TeamSide WinningSide { get; private set; }

        public bool HasEnded => GameEndingCause != LanePushGameEndingCause.None;

        /// <summary>
        /// Runs p.8's first two numbered steps in the page's order and reports what each did.
        ///
        /// The sides are supplied rather than read, because p.8 decides them before the transition
        /// begins - "The team with one or more minions still remaining in the Battle Zone wins the
        /// push" - and milestone 6.9 already built the request that carries them. Supplying them here
        /// rather than accepting that request is what keeps the incomplete prefix out of a live match.
        ///
        /// Nothing is applied until the whole prefix has been decided, so a refusal leaves the Wave
        /// counters and the Battle Zone exactly as they were.
        /// </summary>
        public LanePushPrefixResult ResolveOrderedPrefix(TeamSide winningSide, TeamSide losingSide)
        {
            if (HasEnded)
            {
                return LanePushPrefixResult.Reject(
                    LanePushPrefixRejectionReason.GameAlreadyEnded,
                    RemainingWaveCounters,
                    board.CurrentBattleZoneId);
            }

            var plan = LanePushPrefixRules.Plan(board, RemainingWaveCounters, winningSide, losingSide);
            if (!plan.Accepted)
            {
                return LanePushPrefixResult.Reject(
                    plan.RejectionReason, RemainingWaveCounters, board.CurrentBattleZoneId);
            }

            var outcomes = new List<LanePushStageOutcome>();
            RemainingWaveCounters = plan.RemainingWaveCounters;
            if (plan.FinalWaveFlipped)
            {
                // "the game ends immediately". The Zone stage is absent from the outcomes because it
                // did not run, and the Battle Zone below is the one the match already had.
                GameEndingCause = LanePushGameEndingCause.FinalWaveFlipped;
                WinningSide = winningSide;
                outcomes.Add(LanePushStageOutcome.GameEnded(
                    LanePushTransitionStage.FlipWaveCounter, GameEndingCause));
                return LanePushPrefixResult.Accept(
                    outcomes, RemainingWaveCounters, board.CurrentBattleZoneId,
                    GameEndingCause, WinningSide);
            }

            outcomes.Add(LanePushStageOutcome.Completed(LanePushTransitionStage.FlipWaveCounter));
            if (!board.CommitValidatedBattleZoneAdvance(plan.AdvanceTo.Value))
            {
                throw new InvalidOperationException(
                    "A planned Battle-Zone advance could not be committed; the Lane-Push rules and the " +
                    "board disagree about the same map.");
            }

            if (plan.ThroneCaptured)
            {
                GameEndingCause = LanePushGameEndingCause.ThroneCaptured;
                WinningSide = winningSide;
                outcomes.Add(LanePushStageOutcome.GameEnded(
                    LanePushTransitionStage.AdvanceBattleZone, GameEndingCause));
                return LanePushPrefixResult.Accept(
                    outcomes, RemainingWaveCounters, board.CurrentBattleZoneId,
                    GameEndingCause, WinningSide);
            }

            outcomes.Add(LanePushStageOutcome.Completed(LanePushTransitionStage.AdvanceBattleZone));

            // p.8's third step, named and not run. Reporting it as unimplemented is the honest end of an
            // ordered prefix; reporting the push as complete here would say the minions had been removed
            // and respawned when the board still holds every one of them where it was.
            outcomes.Add(LanePushStageOutcome.NotImplemented(
                LanePushTransitionStage.RemoveAndRespawnMinions));
            return LanePushPrefixResult.Accept(
                outcomes, RemainingWaveCounters, board.CurrentBattleZoneId,
                LanePushGameEndingCause.None, TeamSide.None);
        }
    }
}
