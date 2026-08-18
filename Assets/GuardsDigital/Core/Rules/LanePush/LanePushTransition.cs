using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.LanePush
{
    /// <summary>
    /// Rulebook p.8's three numbered Lane-Push steps, in the order the page numbers them.
    ///
    /// The third is declared and not executable at milestone 6.10, which is what makes this an ordered
    /// prefix rather than a transition: naming the step the prefix stops before is how the result can
    /// say so, rather than reporting a completed transition that quietly did two thirds of the work.
    /// Milestone 6.11 implements it.
    /// </summary>
    public enum LanePushTransitionStage
    {
        FlipWaveCounter = 0,
        AdvanceBattleZone = 1,
        RemoveAndRespawnMinions = 2
    }

    /// <summary>How one p.8 step ended.</summary>
    public enum LanePushStageOutcomeKind
    {
        /// <summary>The step did what the page says and the transition continues.</summary>
        Completed = 0,

        /// <summary>The step ended the game, so no later step runs.</summary>
        GameEnded = 1,

        /// <summary>The step exists in the rules and not yet in the engine (milestone 6.11).</summary>
        NotImplemented = 2
    }

    /// <summary>Which of p.8's two victory sentences ended the game.</summary>
    public enum LanePushGameEndingCause
    {
        None = 0,

        /// <summary>"If the last remaining Wave counter is flipped, the game ends immediately."</summary>
        FinalWaveFlipped = 1,

        /// <summary>"If the enemy Throne Zone itself becomes the new Battle Zone."</summary>
        ThroneCaptured = 2
    }

    /// <summary>Why a push could not be resolved at all. Every one leaves the match exactly as it was.</summary>
    public enum LanePushPrefixRejectionReason
    {
        None = 0,

        /// <summary>p.8 gives the push a winner and a loser, and they are two different sides.</summary>
        UndeclaredSides = 1,

        /// <summary>The board has no Battle Zone, so there is no Lane to push.</summary>
        NoCurrentBattleZone = 2,

        /// <summary>The map declares no ordered Lane, or the Battle Zone is not on the one it declares.</summary>
        BattleZoneOffTheLane = 3,

        /// <summary>The map declares no Throne for the losing side, so "towards" has no direction.</summary>
        UnavailableLosingThrone = 4,

        /// <summary>The Battle Zone is already the losing team's Throne, so the game ended on its capture.</summary>
        BattleZoneAlreadyCaptured = 5,

        /// <summary>An earlier push ended the game.</summary>
        GameAlreadyEnded = 6
    }

    /// <summary>One p.8 step and how it ended.</summary>
    public readonly struct LanePushStageOutcome : IEquatable<LanePushStageOutcome>
    {
        public LanePushStageOutcome(
            LanePushTransitionStage stage,
            LanePushStageOutcomeKind kind,
            LanePushGameEndingCause gameEndingCause)
        {
            if (kind == LanePushStageOutcomeKind.GameEnded && gameEndingCause == LanePushGameEndingCause.None)
            {
                throw new ArgumentException(
                    "A game-ending outcome names which of p.8's two victory sentences ended it.",
                    nameof(gameEndingCause));
            }

            if (kind != LanePushStageOutcomeKind.GameEnded && gameEndingCause != LanePushGameEndingCause.None)
            {
                throw new ArgumentException(
                    "Only a game-ending outcome carries a victory cause.", nameof(gameEndingCause));
            }

            Stage = stage;
            Kind = kind;
            GameEndingCause = gameEndingCause;
        }

        public LanePushTransitionStage Stage { get; }
        public LanePushStageOutcomeKind Kind { get; }
        public LanePushGameEndingCause GameEndingCause { get; }

        public static LanePushStageOutcome Completed(LanePushTransitionStage stage) =>
            new LanePushStageOutcome(stage, LanePushStageOutcomeKind.Completed, LanePushGameEndingCause.None);

        public static LanePushStageOutcome GameEnded(
            LanePushTransitionStage stage, LanePushGameEndingCause cause) =>
            new LanePushStageOutcome(stage, LanePushStageOutcomeKind.GameEnded, cause);

        public static LanePushStageOutcome NotImplemented(LanePushTransitionStage stage) =>
            new LanePushStageOutcome(
                stage, LanePushStageOutcomeKind.NotImplemented, LanePushGameEndingCause.None);

        public bool Equals(LanePushStageOutcome other) =>
            Stage == other.Stage && Kind == other.Kind && GameEndingCause == other.GameEndingCause;

        public override bool Equals(object obj) => obj is LanePushStageOutcome other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (((int)Stage * 397) ^ (int)Kind) * 397 ^ (int)GameEndingCause;
            }
        }

        public override string ToString() => GameEndingCause == LanePushGameEndingCause.None
            ? $"{Stage}: {Kind}"
            : $"{Stage}: {Kind} ({GameEndingCause})";
    }

    /// <summary>
    /// What one attempted push did, as an ordered list of p.8's steps and the state they left behind.
    ///
    /// The ordered list is the whole of milestone 6.10's fourth acceptance check: the same board, wave
    /// count and sides produce the same sequence every time, and the sequence itself is what says which
    /// steps ran. A push that ends the game at the Wave stage has one entry, which is the fifth
    /// acceptance check read directly - the later steps are absent because they did not happen, not
    /// because they happened quietly.
    /// </summary>
    public sealed class LanePushPrefixResult
    {
        private LanePushPrefixResult(
            bool accepted,
            LanePushPrefixRejectionReason rejectionReason,
            IEnumerable<LanePushStageOutcome> outcomes,
            int remainingWaveCounters,
            ZoneId? battleZoneId,
            LanePushGameEndingCause gameEndingCause,
            TeamSide winningSide)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Outcomes = Array.AsReadOnly(outcomes.ToArray());
            RemainingWaveCounters = remainingWaveCounters;
            BattleZoneId = battleZoneId;
            GameEndingCause = gameEndingCause;
            WinningSide = winningSide;
        }

        public bool Accepted { get; }

        public LanePushPrefixRejectionReason RejectionReason { get; }

        /// <summary>p.8's steps, in the page's order, and only the ones that ran.</summary>
        public IReadOnlyList<LanePushStageOutcome> Outcomes { get; }

        /// <summary>Wave counters still face-up after this push.</summary>
        public int RemainingWaveCounters { get; }

        /// <summary>The Battle Zone after this push, unchanged when the Zone stage did not run.</summary>
        public ZoneId? BattleZoneId { get; }

        public LanePushGameEndingCause GameEndingCause { get; }

        /// <summary>The side p.8 awards the game to, and only when this push ended it.</summary>
        public TeamSide WinningSide { get; }

        public bool EndedTheGame => GameEndingCause != LanePushGameEndingCause.None;

        internal static LanePushPrefixResult Reject(
            LanePushPrefixRejectionReason reason,
            int remainingWaveCounters,
            ZoneId? battleZoneId)
        {
            if (reason == LanePushPrefixRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(nameof(reason));
            }

            return new LanePushPrefixResult(
                false, reason, Array.Empty<LanePushStageOutcome>(), remainingWaveCounters,
                battleZoneId, LanePushGameEndingCause.None, TeamSide.None);
        }

        internal static LanePushPrefixResult Accept(
            IEnumerable<LanePushStageOutcome> outcomes,
            int remainingWaveCounters,
            ZoneId? battleZoneId,
            LanePushGameEndingCause gameEndingCause,
            TeamSide winningSide) =>
            new LanePushPrefixResult(
                true, LanePushPrefixRejectionReason.None, outcomes, remainingWaveCounters,
                battleZoneId, gameEndingCause,
                gameEndingCause == LanePushGameEndingCause.None ? TeamSide.None : winningSide);
    }

    /// <summary>What the prefix decided, before any of it is applied.</summary>
    internal sealed class LanePushPrefixPlan
    {
        private LanePushPrefixPlan(
            LanePushPrefixRejectionReason rejectionReason,
            int remainingWaveCounters,
            bool finalWaveFlipped,
            ZoneId? advanceTo,
            bool throneCaptured)
        {
            RejectionReason = rejectionReason;
            RemainingWaveCounters = remainingWaveCounters;
            FinalWaveFlipped = finalWaveFlipped;
            AdvanceTo = advanceTo;
            ThroneCaptured = throneCaptured;
        }

        public LanePushPrefixRejectionReason RejectionReason { get; }
        public bool Accepted => RejectionReason == LanePushPrefixRejectionReason.None;
        public int RemainingWaveCounters { get; }
        public bool FinalWaveFlipped { get; }

        /// <summary>The next Zone, or absent because the final Wave ended the game before the Zone stage.</summary>
        public ZoneId? AdvanceTo { get; }

        public bool ThroneCaptured { get; }

        public static LanePushPrefixPlan Reject(LanePushPrefixRejectionReason reason) =>
            new LanePushPrefixPlan(reason, 0, false, null, false);

        public static LanePushPrefixPlan FinalWave(int remainingWaveCounters) =>
            new LanePushPrefixPlan(
                LanePushPrefixRejectionReason.None, remainingWaveCounters, true, null, false);

        public static LanePushPrefixPlan Advance(
            int remainingWaveCounters, ZoneId advanceTo, bool throneCaptured) =>
            new LanePushPrefixPlan(
                LanePushPrefixRejectionReason.None, remainingWaveCounters, false, advanceTo, throneCaptured);
    }

    /// <summary>
    /// Rulebook p.8's ordered Lane-Push prefix, decided and not applied.
    ///
    /// "1. Flip one Wave counter face-down. If the last remaining Wave counter is flipped, the game ends
    /// immediately and the team who won this final push wins the game. 2. The next Zone towards the
    /// losing team's Throne becomes the new Battle Zone. If the enemy Throne Zone itself becomes the new
    /// Battle Zone, the team who won the push wins the game."
    ///
    /// It is internal, beside <c>MinionBattleRules</c> and for the reason milestone 6.1k recorded there:
    /// a rule this incomplete has no business being a public engine surface, and the one public seam is
    /// the authority that owns the Wave counters and drives it. Milestone 6.11 adds p.8's third step
    /// behind that same seam.
    ///
    /// Deciding and applying are separate here because the two steps are not independent: the Zone stage
    /// runs only if the Wave stage did not end the game, so a plan that computed both and let a caller
    /// apply either would be a partial transition with extra steps. The plan says exactly what will
    /// happen; the authority makes it happen in the page's order.
    /// </summary>
    internal static class LanePushPrefixRules
    {
        public static LanePushPrefixPlan Plan(
            ILaneAdvanceAuthority board,
            int remainingWaveCounters,
            TeamSide winningSide,
            TeamSide losingSide)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            if (winningSide == losingSide ||
                (winningSide != TeamSide.Red && winningSide != TeamSide.Blue) ||
                (losingSide != TeamSide.Red && losingSide != TeamSide.Blue))
            {
                return LanePushPrefixPlan.Reject(LanePushPrefixRejectionReason.UndeclaredSides);
            }

            if (!board.CurrentBattleZoneId.HasValue)
            {
                return LanePushPrefixPlan.Reject(LanePushPrefixRejectionReason.NoCurrentBattleZone);
            }

            var lane = board.MapDefinition.OrderedLaneZoneIds;
            var currentIndex = IndexOf(lane, board.CurrentBattleZoneId.Value);
            if (currentIndex < 0)
            {
                return LanePushPrefixPlan.Reject(LanePushPrefixRejectionReason.BattleZoneOffTheLane);
            }

            if (!ThroneZoneQuery.TryGetThroneZone(board.MapDefinition, losingSide, out var losingThrone))
            {
                return LanePushPrefixPlan.Reject(LanePushPrefixRejectionReason.UnavailableLosingThrone);
            }

            var throneIndex = IndexOf(lane, losingThrone.Id);
            if (throneIndex < 0)
            {
                return LanePushPrefixPlan.Reject(LanePushPrefixRejectionReason.UnavailableLosingThrone);
            }

            if (throneIndex == currentIndex)
            {
                return LanePushPrefixPlan.Reject(LanePushPrefixRejectionReason.BattleZoneAlreadyCaptured);
            }

            // Step 1 first, and its result decides whether step 2 is reached at all. Nothing about the
            // Zone is inspected before the counter is accounted for, because p.8 numbers them and ends
            // the game "immediately" on the last one.
            //
            // An exhausted pool is an engine defect rather than a rules outcome, so it throws instead of
            // taking a rejection member no caller could produce: the only caller starts with at least
            // one counter and stops the moment the last one is flipped, so a zero here means the
            // authority let a match carry on past its own ending.
            if (remainingWaveCounters <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(remainingWaveCounters), remainingWaveCounters,
                    "A push with no Wave counter left to flip is a match that already ended.");
            }

            var remaining = remainingWaveCounters - 1;
            if (remaining == 0)
            {
                return LanePushPrefixPlan.FinalWave(remaining);
            }

            // "The next Zone towards the losing team's Throne": one step along the ordered Lane in the
            // direction that Throne lies, which the Lane's own order supplies. The Throne is at one end
            // of the sequence and the Battle Zone is somewhere inside it, so the direction is a sign.
            var nextIndex = currentIndex + (throneIndex > currentIndex ? 1 : -1);
            var next = lane[nextIndex];
            return LanePushPrefixPlan.Advance(remaining, next, next.Equals(losingThrone.Id));
        }

        private static int IndexOf(IReadOnlyList<ZoneId> lane, ZoneId zoneId)
        {
            for (var index = 0; index < lane.Count; index++)
            {
                if (lane[index].Equals(zoneId)) return index;
            }

            return -1;
        }
    }
}
