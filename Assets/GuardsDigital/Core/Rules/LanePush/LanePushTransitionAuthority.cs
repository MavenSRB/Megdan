using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.LanePush
{
    /// <summary>
    /// The facts one running push carries between its own steps: who won it, who lost it, and which
    /// Zone the Battle Zone left.
    ///
    /// They are here rather than on the continuation because they are rules state rather than resume
    /// location - the continuation says where the transition is, and this says what the transition is
    /// about. The previous Battle Zone in particular cannot be re-derived once the Zone stage has run,
    /// which is exactly why the removal stage has to be able to read it after the fact.
    /// </summary>
    public sealed class RunningLanePush
    {
        public RunningLanePush(
            TeamSide winningSide,
            TeamSide losingSide,
            TeamSide tieBreakerSide,
            ZoneId? previousBattleZoneId,
            IEnumerable<LanePushStageOutcome> outcomes)
        {
            if (winningSide == losingSide ||
                (winningSide != TeamSide.Red && winningSide != TeamSide.Blue) ||
                (losingSide != TeamSide.Red && losingSide != TeamSide.Blue))
            {
                throw new ArgumentException("A running push has two distinct declared sides.");
            }

            WinningSide = winningSide;
            LosingSide = losingSide;
            TieBreakerSide = tieBreakerSide;
            PreviousBattleZoneId = previousBattleZoneId;
            Outcomes = Array.AsReadOnly(
                (outcomes ?? throw new ArgumentNullException(nameof(outcomes))).ToArray());
        }

        public TeamSide WinningSide { get; }
        public TeamSide LosingSide { get; }

        /// <summary>
        /// The side the Tie Breaker coin was showing when this push began - p.8's "starting with the
        /// team whose symbol is on the tie breaker coin".
        ///
        /// It is recorded on the push rather than read when a placement is derived, for two reasons
        /// that agree. p.8 does not flip the coin for a Lane Push, so the face that decides the order
        /// is the one the push started under; and a push can be paused across a snapshot, so an order
        /// that re-read a coin some later Initiative tie had flipped would resume in a different order
        /// than it began in.
        /// </summary>
        public TeamSide TieBreakerSide { get; }

        /// <summary>The Zone the Battle Zone left, or absent until the Zone stage has run.</summary>
        public ZoneId? PreviousBattleZoneId { get; }

        /// <summary>p.8's stages, in the page's order, and only the ones that have run.</summary>
        public IReadOnlyList<LanePushStageOutcome> Outcomes { get; }

        public RunningLanePush With(ZoneId? previousBattleZoneId, LanePushStageOutcome outcome) =>
            new RunningLanePush(
                WinningSide, LosingSide, TieBreakerSide, previousBattleZoneId ?? PreviousBattleZoneId,
                Outcomes.Concat(new[] { outcome }));
    }

    /// <summary>
    /// The persisted state of a <see cref="LanePushTransitionAuthority"/>, in the arity-disciplined
    /// shape milestone 6.1g gave every restore state.
    ///
    /// Milestone 6.10 declined to persist any of this and said why: nothing a live match could reach
    /// read a Wave counter, so a persisted one would have been a wire field no restore could ever
    /// disagree about. Milestone 6.11 makes the transition reachable, so all of it is live state now.
    /// </summary>
    public sealed class LanePushRestoreState
    {
        public LanePushRestoreState(
            int remainingWaveCounters,
            LanePushGameEndingCause gameEndingCause,
            TeamSide winningSide,
            RunningLanePush running)
        {
            RemainingWaveCounters = remainingWaveCounters;
            GameEndingCause = gameEndingCause;
            WinningSide = winningSide;
            Running = running;
        }

        public int RemainingWaveCounters { get; }
        public LanePushGameEndingCause GameEndingCause { get; }
        public TeamSide WinningSide { get; }
        public RunningLanePush Running { get; }

        public void Deconstruct(
            out int remainingWaveCounters,
            out LanePushGameEndingCause gameEndingCause,
            out TeamSide winningSide,
            out RunningLanePush running)
        {
            remainingWaveCounters = RemainingWaveCounters;
            gameEndingCause = GameEndingCause;
            winningSide = WinningSide;
            running = Running;
        }
    }

    /// <summary>
    /// Owns rulebook p.8's shared Wave counters and performs the Lane-Push transition's stages
    /// (milestones 6.10 and 6.11).
    ///
    /// The Wave counters are one pool rather than a stock per team. p.4: "Whenever a team Pushes the
    /// Lane, a Wave counter is removed. When the last counter is gone, the game ends immediately, and
    /// the team that won the last Push wins the game!" The Quick Game places three.
    ///
    /// This owns what p.8's steps <em>do</em>, and it deliberately does not own where the transition
    /// <em>is</em>: that is the continuation, held by <c>SystemTransitionAuthority</c>, and the two are
    /// joined by <c>LanePushStepRules</c>. A second record of the position here is precisely what
    /// milestone 6.10a's conformance rule forbids, and the reason it forbids it is that this type is
    /// snapshotted and a step cursor beside the continuation could disagree with it after a restore.
    ///
    /// Milestone 6.10 held this out of the match entirely, because p.8's third step did not exist and a
    /// match that ran the first two would have moved its Battle Zone away from minions that were never
    /// removed or respawned. Milestone 6.11 implements the third step, composes this into the round and
    /// persists it, and both Lane-Push requests now reach it.
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

        /// <summary>The push now running, or <c>null</c> when none is.</summary>
        public RunningLanePush Running { get; private set; }

        public ILaneAdvanceAuthority Board => board;

        /// <summary>
        /// Whether this push may begin, decided before p.8's first step spends anything.
        ///
        /// The sides are supplied rather than read, because p.8 decides them before the transition
        /// begins - "The team with one or more minions still remaining in the Battle Zone wins the
        /// push" - and milestones 6.1 and 6.9 already built the requests that carry them.
        /// </summary>
        public LanePushPrefixRejectionReason TryBeginPush(
            TeamSide winningSide, TeamSide losingSide, TieBreakerState tieBreakerState)
        {
            var tieBreakerSide = tieBreakerState.RevealedSide;
            if (HasEnded)
            {
                return LanePushPrefixRejectionReason.GameAlreadyEnded;
            }

            var rejection = LanePushPrefixRules.Validate(board, winningSide, losingSide);
            if (rejection != LanePushPrefixRejectionReason.None)
            {
                return rejection;
            }

            Running = new RunningLanePush(
                winningSide, losingSide, tieBreakerSide, null, Array.Empty<LanePushStageOutcome>());
            return LanePushPrefixRejectionReason.None;
        }

        /// <summary>
        /// p.8 step 1. "Flip one Wave counter face-down. If the last remaining Wave counter is flipped,
        /// the game ends immediately and the team who won this final push wins the game."
        /// </summary>
        public LanePushStageOutcome ResolveWaveStage()
        {
            var running = RequireRunning();
            RemainingWaveCounters = LanePushPrefixRules.PlanWaveFlip(RemainingWaveCounters);
            if (RemainingWaveCounters > 0)
            {
                return Record(LanePushStageOutcome.Completed(LanePushTransitionStage.FlipWaveCounter));
            }

            GameEndingCause = LanePushGameEndingCause.FinalWaveFlipped;
            WinningSide = running.WinningSide;
            return Record(LanePushStageOutcome.GameEnded(
                LanePushTransitionStage.FlipWaveCounter, GameEndingCause));
        }

        /// <summary>
        /// p.8 step 2. "The next Zone towards the losing team's Throne becomes the new Battle Zone. If
        /// the enemy Throne Zone itself becomes the new Battle Zone, the team who won the push wins the
        /// game."
        /// </summary>
        public LanePushStageOutcome ResolveZoneStage()
        {
            var running = RequireRunning();
            var previous = board.CurrentBattleZoneId;
            var plan = LanePushPrefixRules.PlanZoneAdvance(board, running.LosingSide);
            if (!board.CommitValidatedBattleZoneAdvance(plan.AdvanceTo))
            {
                throw new InvalidOperationException(
                    "A planned Battle-Zone advance could not be committed; the Lane-Push rules and the " +
                    "board disagree about the same map.");
            }

            if (!plan.ThroneCaptured)
            {
                return Record(
                    LanePushStageOutcome.Completed(LanePushTransitionStage.AdvanceBattleZone), previous);
            }

            GameEndingCause = LanePushGameEndingCause.ThroneCaptured;
            WinningSide = running.WinningSide;
            return Record(
                LanePushStageOutcome.GameEnded(
                    LanePushTransitionStage.AdvanceBattleZone, GameEndingCause),
                previous);
        }

        /// <summary>
        /// The first half of p.8 step 3: "Remove all minions in the previous Battle Zone."
        ///
        /// Every miniature standing there, of either team, and no outcome of its own - a removal by a
        /// Lane Push is not a Defeat, awards nothing and is not p.13's removal stage. The transition
        /// reports the page's third step once, when the respawn that completes it has finished.
        /// </summary>
        public IReadOnlyList<MinionId> ResolvePreviousZoneRemoval()
        {
            var running = RequireRunning();
            if (!running.PreviousBattleZoneId.HasValue)
            {
                throw new InvalidOperationException(
                    "p.8 removes the minions of the previous Battle Zone, and the Zone stage has not " +
                    "said which Zone that was.");
            }

            var removed = new List<MinionId>();
            foreach (var minion in board.FindMinionsInZone(running.PreviousBattleZoneId.Value))
            {
                if (!board.CommitValidatedLanePushRemoval(minion.MinionId))
                {
                    throw new InvalidOperationException(
                        "A minion the previous Battle Zone was holding could not be removed.");
                }

                removed.Add(minion.MinionId);
            }

            return Array.AsReadOnly(removed.ToArray());
        }

        /// <summary>Records that p.8's third step has finished, which is the last thing a push does.</summary>
        public LanePushStageOutcome CompleteRespawnStage() =>
            Record(LanePushStageOutcome.Completed(LanePushTransitionStage.RemoveAndRespawnMinions));

        /// <summary>The result of the push that has just stopped running, and the end of its record.</summary>
        public LanePushPrefixResult CompletePush()
        {
            var running = RequireRunning();
            Running = null;
            return LanePushPrefixResult.Accept(
                running.Outcomes, RemainingWaveCounters, board.CurrentBattleZoneId,
                GameEndingCause, WinningSide);
        }

        public LanePushRestoreState CaptureRestoreState() =>
            new LanePushRestoreState(RemainingWaveCounters, GameEndingCause, WinningSide, Running);

        public static LanePushTransitionAuthority Restore(
            LanePushRestoreState state, ILaneAdvanceAuthority board)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.Deconstruct(
                out var remainingWaveCounters, out var gameEndingCause, out var winningSide,
                out var running);

            // A pool that has been spent to nothing is a match that has ended, so the constructor's
            // "at least one counter" invariant is about a match starting rather than about a match
            // restored; the restore rebuilds the ended state rather than refusing it.
            var authority = new LanePushTransitionAuthority(
                board, Math.Max(remainingWaveCounters, 1));
            authority.RemainingWaveCounters = remainingWaveCounters;
            authority.GameEndingCause = gameEndingCause;
            authority.WinningSide = winningSide;
            authority.Running = running;
            if (gameEndingCause == LanePushGameEndingCause.None != (winningSide == TeamSide.None))
            {
                throw new ArgumentException(
                    "p.8 awards the game to the team that won the push that ended it, so a winner and " +
                    "an ending cause are one fact and are present together.",
                    nameof(state));
            }

            return authority;
        }

        private RunningLanePush RequireRunning()
        {
            if (Running == null)
            {
                throw new InvalidOperationException(
                    "A Lane-Push stage ran with no push running; p.8's steps belong to a push that was " +
                    "begun by a request.");
            }

            return Running;
        }

        private LanePushStageOutcome Record(
            LanePushStageOutcome outcome, ZoneId? previousBattleZoneId = null)
        {
            Running = Running.With(previousBattleZoneId, outcome);
            return outcome;
        }
    }
}
