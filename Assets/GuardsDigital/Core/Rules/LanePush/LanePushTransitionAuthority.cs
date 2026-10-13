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
            : this(
                winningSide, losingSide, tieBreakerSide,
                previousBattleZoneId ?? default,
                previousBattleZoneId, outcomes)
        {
        }

        public RunningLanePush(
            TeamSide winningSide,
            TeamSide losingSide,
            TeamSide tieBreakerSide,
            ZoneId battleZoneId,
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
            BattleZoneId = battleZoneId;
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
        public ZoneId BattleZoneId { get; }

        /// <summary>The Zone the Battle Zone left, or absent until the Zone stage has run.</summary>
        public ZoneId? PreviousBattleZoneId { get; }

        /// <summary>p.8's stages, in the page's order, and only the ones that have run.</summary>
        public IReadOnlyList<LanePushStageOutcome> Outcomes { get; }

        public RunningLanePush With(
            ZoneId battleZoneId,
            ZoneId? previousBattleZoneId,
            LanePushStageOutcome outcome) =>
            new RunningLanePush(
                WinningSide, LosingSide, TieBreakerSide, battleZoneId,
                previousBattleZoneId ?? PreviousBattleZoneId,
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
            : this(
                new[] { remainingWaveCounters }, WaveCounterAllocation.SharedTotal,
                gameEndingCause, winningSide, running)
        {
        }

        public LanePushRestoreState(
            IEnumerable<int> remainingWaveCountersByLane,
            WaveCounterAllocation waveCounterAllocation,
            LanePushGameEndingCause gameEndingCause,
            TeamSide winningSide,
            RunningLanePush running,
            bool isSuddenDeath = false)
        {
            RemainingWaveCountersByLane = Array.AsReadOnly(
                (remainingWaveCountersByLane ?? throw new ArgumentNullException(nameof(remainingWaveCountersByLane)))
                .ToArray());
            if (RemainingWaveCountersByLane.Count == 0 || RemainingWaveCountersByLane.Any(value => value < 0))
                throw new ArgumentException("Restored Wave-counter pools must be present and nonnegative.", nameof(remainingWaveCountersByLane));
            WaveCounterAllocation = waveCounterAllocation;
            RemainingWaveCounters = waveCounterAllocation == WaveCounterAllocation.PerLane
                ? RemainingWaveCountersByLane.Min()
                : RemainingWaveCountersByLane[0];
            GameEndingCause = gameEndingCause;
            WinningSide = winningSide;
            Running = running;
            IsSuddenDeath = isSuddenDeath;
        }

        public int RemainingWaveCounters { get; }
        public IReadOnlyList<int> RemainingWaveCountersByLane { get; }
        public WaveCounterAllocation WaveCounterAllocation { get; }
        public LanePushGameEndingCause GameEndingCause { get; }
        public TeamSide WinningSide { get; }
        public RunningLanePush Running { get; }
        public bool IsSuddenDeath { get; }

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
        private readonly WaveCounterAllocation waveCounterAllocation;
        private readonly int[] remainingWaveCountersByLane;
        private bool deferDoubleLaneFinalWaveDecision;

        public LanePushTransitionAuthority(ILaneAdvanceAuthority board, int waveCounters)
            : this(board, waveCounters, WaveCounterAllocation.SharedTotal)
        {
        }

        public LanePushTransitionAuthority(
            ILaneAdvanceAuthority board,
            int waveCounters,
            WaveCounterAllocation waveCounterAllocation)
        {
            if (waveCounters <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(waveCounters), waveCounters, "A match starts with at least one Wave counter.");
            }

            this.board = board ?? throw new ArgumentNullException(nameof(board));
            if (waveCounterAllocation != WaveCounterAllocation.SharedTotal &&
                waveCounterAllocation != WaveCounterAllocation.PerLane)
                throw new ArgumentOutOfRangeException(nameof(waveCounterAllocation));
            this.waveCounterAllocation = board.MapDefinition.OrderedLanes.Count > 1
                ? waveCounterAllocation
                : WaveCounterAllocation.SharedTotal;
            remainingWaveCountersByLane = this.waveCounterAllocation == WaveCounterAllocation.PerLane
                ? Enumerable.Repeat(waveCounters, board.MapDefinition.OrderedLanes.Count).ToArray()
                : new[] { waveCounters };
            RemainingWaveCounters = waveCounters;
            GameEndingCause = LanePushGameEndingCause.None;
            WinningSide = TeamSide.None;
        }

        /// <summary>Wave counters still face-up.</summary>
        public int RemainingWaveCounters { get; private set; }
        public IReadOnlyList<int> RemainingWaveCountersByLane =>
            Array.AsReadOnly(remainingWaveCountersByLane);
        public WaveCounterAllocation WaveCounterAllocation => waveCounterAllocation;

        /// <summary>Which of p.8's two victory sentences ended the game, if either has.</summary>
        public LanePushGameEndingCause GameEndingCause { get; private set; }

        /// <summary>The side that won, and only once the game has ended.</summary>
        public TeamSide WinningSide { get; private set; }

        public bool HasEnded => GameEndingCause != LanePushGameEndingCause.None;
        public bool IsSuddenDeath { get; private set; }

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
            return !board.CurrentBattleZoneId.HasValue
                ? LanePushPrefixRejectionReason.NoCurrentBattleZone
                : TryBeginPush(
                    board.CurrentBattleZoneId.Value, winningSide, losingSide, tieBreakerState);
        }

        public LanePushPrefixRejectionReason TryBeginPush(
            ZoneId battleZoneId,
            TeamSide winningSide, TeamSide losingSide, TieBreakerState tieBreakerState,
            bool deferDoubleLaneFinalWaveDecision = false)
        {
            var tieBreakerSide = tieBreakerState.RevealedSide;
            if (HasEnded)
            {
                return LanePushPrefixRejectionReason.GameAlreadyEnded;
            }

            var rejection = LanePushPrefixRules.Validate(
                board, battleZoneId, winningSide, losingSide);
            if (rejection != LanePushPrefixRejectionReason.None)
            {
                return rejection;
            }

            Running = new RunningLanePush(
                winningSide, losingSide, tieBreakerSide, battleZoneId, null,
                Array.Empty<LanePushStageOutcome>());
            this.deferDoubleLaneFinalWaveDecision = deferDoubleLaneFinalWaveDecision;
            return LanePushPrefixRejectionReason.None;
        }

        /// <summary>
        /// p.8 step 1. "Flip one Wave counter face-down. If the last remaining Wave counter is flipped,
        /// the game ends immediately and the team who won this final push wins the game."
        /// </summary>
        public LanePushStageOutcome ResolveWaveStage()
        {
            var running = RequireRunning();
            var laneIndex = LaneIndexOf(running.BattleZoneId);
            var counterIndex = waveCounterAllocation == WaveCounterAllocation.PerLane ? laneIndex : 0;

            if (remainingWaveCountersByLane[counterIndex] == 0)
            {
                if (board.MapDefinition.OrderedLanes.Count == 1)
                    throw new InvalidOperationException(
                        "A spent Wave pool cannot begin another single-lane push.");

                // The two Lane Battles are simultaneous. With a shared pool, the first queued push
                // can spend the last counter and the other Lane must still advance before the final
                // position is compared. Sudden death likewise keeps advancing without more counters.
                return Record(LanePushStageOutcome.Completed(LanePushTransitionStage.FlipWaveCounter));
            }

            remainingWaveCountersByLane[counterIndex] = LanePushPrefixRules.PlanWaveFlip(
                remainingWaveCountersByLane[counterIndex]);
            RemainingWaveCounters = waveCounterAllocation == WaveCounterAllocation.PerLane
                ? remainingWaveCountersByLane.Min()
                : remainingWaveCountersByLane[0];
            if (remainingWaveCountersByLane[counterIndex] > 0)
            {
                return Record(LanePushStageOutcome.Completed(LanePushTransitionStage.FlipWaveCounter));
            }

            if (board.MapDefinition.OrderedLanes.Count > 1)
            {
                // A two-Lane winner is decided after the current Battle Zone moves, and after the
                // other Lane's simultaneous push when this push belongs to an end-of-round pair.
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
            var previous = (ZoneId?)running.BattleZoneId;
            var plan = LanePushPrefixRules.PlanZoneAdvance(
                board, running.BattleZoneId, running.LosingSide);
            if (!board.CommitValidatedBattleZoneAdvance(running.BattleZoneId, plan.AdvanceTo))
            {
                throw new InvalidOperationException(
                    "A planned Battle-Zone advance could not be committed; the Lane-Push rules and the " +
                    "board disagree about the same map.");
            }

            if (!plan.ThroneCaptured)
            {
                if (board.MapDefinition.OrderedLanes.Count > 1 &&
                    !deferDoubleLaneFinalWaveDecision &&
                    (IsSuddenDeath || remainingWaveCountersByLane.Any(value => value == 0)))
                {
                    var finalWinner = CompareDoubleLanePosition();
                    if (finalWinner == TeamSide.None)
                    {
                        IsSuddenDeath = true;
                        return Record(
                            LanePushStageOutcome.Completed(LanePushTransitionStage.AdvanceBattleZone),
                            plan.AdvanceTo, previous);
                    }

                    GameEndingCause = LanePushGameEndingCause.FinalWaveFlipped;
                    WinningSide = finalWinner;
                    return Record(
                        LanePushStageOutcome.GameEnded(
                            LanePushTransitionStage.AdvanceBattleZone, GameEndingCause),
                        plan.AdvanceTo, previous);
                }

                return Record(
                    LanePushStageOutcome.Completed(LanePushTransitionStage.AdvanceBattleZone),
                    plan.AdvanceTo, previous);
            }

            GameEndingCause = LanePushGameEndingCause.ThroneCaptured;
            WinningSide = running.WinningSide;
            return Record(
                LanePushStageOutcome.GameEnded(
                    LanePushTransitionStage.AdvanceBattleZone, GameEndingCause),
                plan.AdvanceTo, previous);
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
            deferDoubleLaneFinalWaveDecision = false;
            return LanePushPrefixResult.Accept(
                running.Outcomes, RemainingWaveCounters, board.CurrentBattleZoneId,
                GameEndingCause, WinningSide);
        }

        public LanePushRestoreState CaptureRestoreState() =>
            new LanePushRestoreState(
                remainingWaveCountersByLane, waveCounterAllocation,
                GameEndingCause, WinningSide, Running, IsSuddenDeath);

        public static LanePushTransitionAuthority Restore(
            LanePushRestoreState state, ILaneAdvanceAuthority board)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var remainingWaveCounters = state.RemainingWaveCounters;
            var gameEndingCause = state.GameEndingCause;
            var winningSide = state.WinningSide;
            var running = state.Running;

            // A pool that has been spent to nothing is a match that has ended, so the constructor's
            // "at least one counter" invariant is about a match starting rather than about a match
            // restored; the restore rebuilds the ended state rather than refusing it.
            var authority = new LanePushTransitionAuthority(
                board, Math.Max(state.RemainingWaveCountersByLane.Max(), 1),
                state.WaveCounterAllocation);
            if (authority.remainingWaveCountersByLane.Length != state.RemainingWaveCountersByLane.Count)
                throw new ArgumentException(
                    "Restored Wave-counter pools must match the selected allocation and Lane count.", nameof(state));
            for (var index = 0; index < authority.remainingWaveCountersByLane.Length; index++)
                authority.remainingWaveCountersByLane[index] = state.RemainingWaveCountersByLane[index];
            authority.RemainingWaveCounters = remainingWaveCounters;
            authority.GameEndingCause = gameEndingCause;
            authority.WinningSide = winningSide;
            authority.Running = running;
            authority.IsSuddenDeath = state.IsSuddenDeath;
            if (gameEndingCause == LanePushGameEndingCause.None != (winningSide == TeamSide.None))
            {
                throw new ArgumentException(
                    "p.8 awards the game to the team that won the push that ended it, so a winner and " +
                    "an ending cause are one fact and are present together.",
                    nameof(state));
            }

            return authority;
        }

        private TeamSide CompareDoubleLanePosition()
        {
            var red = 0;
            var blue = 0;
            for (var laneIndex = 0; laneIndex < board.MapDefinition.OrderedLanes.Count; laneIndex++)
            {
                var lane = board.MapDefinition.OrderedLanes[laneIndex];
                var current = board.CurrentBattleZoneIds[laneIndex];
                var position = -1;
                for (var zoneIndex = 0; zoneIndex < lane.Count; zoneIndex++)
                    if (lane[zoneIndex].Equals(current)) position = zoneIndex;
                if (position < 0)
                    throw new InvalidOperationException(
                        $"Current Battle Zone '{current}' is not on its recorded Lane.");
                red += Math.Max(position - 1, 0);
                blue += Math.Max(lane.Count - position - 2, 0);
            }

            return red == blue ? TeamSide.None : red > blue ? TeamSide.Red : TeamSide.Blue;
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

        private int LaneIndexOf(ZoneId battleZoneId)
        {
            if (!board.MapDefinition.TryGetLaneContaining(battleZoneId, out var laneIndex, out _))
                throw new InvalidOperationException(
                    $"Battle Zone '{battleZoneId}' is not on an ordered Lane.");
            return laneIndex;
        }

        private LanePushStageOutcome Record(
            LanePushStageOutcome outcome,
            ZoneId? battleZoneId = null,
            ZoneId? previousBattleZoneId = null)
        {
            Running = Running.With(
                battleZoneId ?? Running.BattleZoneId,
                previousBattleZoneId,
                outcome);
            return outcome;
        }
    }
}
