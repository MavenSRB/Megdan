using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>One head-to-head, both sides, over a named seed set.</summary>
    public sealed class BotMatchupReport
    {
        private readonly List<string> rows = new List<string>();

        public BotMatchupReport(BotTier subject, BotTier opponent, int seedCount)
        {
            Subject = subject;
            Opponent = opponent;
            SeedCount = seedCount;
        }

        public BotTier Subject { get; }
        public BotTier Opponent { get; }
        public int SeedCount { get; }
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int Undecided { get; private set; }

        /// <summary>
        /// Wins split by the side the subject held. Reported because one total hides the thing that
        /// matters when the game is not side-neutral - phase A's Initiative finding turned entirely
        /// on this split moving from 3/20 to 13/20 while the total moved 31 points.
        /// </summary>
        public int WinsAsRed { get; private set; }

        public int WinsAsBlue { get; private set; }
        public int SimulatedPlies { get; private set; }
        public int Decided => Wins + Losses;
        public double WinRate => Decided == 0 ? 0.0 : (double)Wins / Decided;
        public double SubjectMillisecondsPerDecision => SafeMean(subjectTicksMs, subjectDecisions);
        public double OpponentMillisecondsPerDecision => SafeMean(opponentTicksMs, opponentDecisions);

        private double subjectTicksMs;
        private int subjectDecisions;
        private double opponentTicksMs;
        private int opponentDecisions;

        private static double SafeMean(double total, int count) => count > 0 ? total / count : 0.0;

        public void Add(BotSelfPlayResult result, TeamId subjectTeam, bool subjectOnRed)
        {
            string verdict;
            if (!result.HasWinner)
            {
                Undecided++;
                verdict = result.Stalled ? "STALLED" : "budget";
            }
            else if (result.Outcome.WinningTeamId.Equals(subjectTeam))
            {
                Wins++;
                if (subjectOnRed)
                {
                    WinsAsRed++;
                }
                else
                {
                    WinsAsBlue++;
                }

                verdict = "WIN";
            }
            else
            {
                Losses++;
                verdict = "loss";
            }

            SimulatedPlies += result.SimulatedPlies;
            subjectTicksMs += result.MillisecondsPerDecision(Subject) *
                result.DecisionCounts[Subject];
            subjectDecisions += result.DecisionCounts[Subject];
            opponentTicksMs += result.MillisecondsPerDecision(Opponent) *
                result.DecisionCounts[Opponent];
            opponentDecisions += result.DecisionCounts[Opponent];
            rows.Add("seed " + result.Seed + (subjectOnRed ? " red " : " blue ") + verdict +
                " commands=" + result.Commands + " rounds=" + result.RoundsReached);
        }

        public string Summary() =>
            Subject + " vs " + Opponent + "  " + Wins + "W " + Losses + "L " + Undecided + "U   " +
            (WinRate * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%   [red " +
            WinsAsRed + "/" + SeedCount + ", blue " + WinsAsBlue + "/" + SeedCount + "]   " +
            "subject " + SubjectMillisecondsPerDecision.ToString("0.000", CultureInfo.InvariantCulture) +
            " ms/decision, opponent " +
            OpponentMillisecondsPerDecision.ToString("0.000", CultureInfo.InvariantCulture) +
            " ms, plies " + SimulatedPlies;

        public override string ToString() =>
            Summary() + Environment.NewLine + string.Join(Environment.NewLine, rows);
    }

    /// <summary>
    /// FOURTEENTH amendment, tier-2. The measurement bench, kept apart from phase A's
    /// <c>BotBrainTests</c> so that the tier-1 gate those cases assert stays exactly the control it
    /// was.
    /// </summary>
    /// <remarks>
    /// <para><b>Tuning seeds and gate seeds are DISJOINT, which is the whole methodology.</b> A
    /// weight vector chosen on a seed set can fit that seed set; phase A proved its one weight
    /// generalised by re-measuring on seeds 101-120 that the choice never saw, and tier-2 tunes nine
    /// weights, so the risk of fitting is nine times larger and the discipline matters nine times
    /// more. <see cref="TuningSeeds"/> is what the sweep is allowed to look at.
    /// <see cref="HeldOutSeeds"/> is what the gate turns on, and no sweep may read it.</para>
    /// <para>Both benches are the SYMMETRIC one, for the reason phase A recorded at length: the
    /// engaged fixture hands Blue 72-85% of decided matches with the same brain on both sides, which
    /// is several times the effect being looked for. Every comparison is still played from both
    /// sides on top of that.</para>
    /// </remarks>
    public static class BotTierTwoBench
    {
        public const int LifeCountersPerTeam = 2;
        public const int CommandBudget = 2400;

        /// <summary>What the weight sweep and the band sweep are allowed to see.</summary>
        public static readonly int[] TuningSeeds = Enumerable.Range(1, 20).ToArray();

        /// <summary>What the gate turns on. No tuning run may read these.</summary>
        public static readonly int[] HeldOutSeeds = Enumerable.Range(101, 20).ToArray();

        /// <summary>
        /// Sample size sized to the effect, phase A's own lesson: a 30-45 point gap needs far fewer
        /// seeds than the close comparison, and spending twenty on it costs suite time for no
        /// confidence.
        /// </summary>
        public static readonly int[] LopsidedSeeds = Enumerable.Range(1, 8).ToArray();

        /// <summary>
        /// The shadow probe's session factory: the SAME bench, freshly composed. It is restored to
        /// the live position before every candidate, so what matters is that it is the same match
        /// definition - see <see cref="ShadowSessionBotLookahead"/>.
        /// </summary>
        public static Func<IMatchSession> ShadowFactory =>
            () => BotSelfPlay.CreateSymmetricBench(LifeCountersPerTeam);

        public static BotMatchupReport Measure(
            BotTier subject,
            BotTier opponent,
            IReadOnlyList<int> seeds,
            BotEvaluationWeights weights = null,
            int nearEqualBand = BotPolicy.NearEqualBand,
            int lookaheadCandidateCap = BotPolicy.LookaheadCandidateCap,
            bool useLookahead = true,
            BotPolicyWeights policy = null)
        {
            if (seeds == null) throw new ArgumentNullException(nameof(seeds));
            var report = new BotMatchupReport(subject, opponent, seeds.Count);
            foreach (var seed in seeds)
            {
                foreach (var subjectOnRed in new[] { true, false })
                {
                    report.Add(
                        PlayOne(
                            subject, opponent, seed, subjectOnRed, weights, nearEqualBand,
                            lookaheadCandidateCap, useLookahead, policy, out var team),
                        team, subjectOnRed);
                }
            }

            return report;
        }

        public static BotSelfPlayResult PlayOne(
            BotTier subject,
            BotTier opponent,
            int seed,
            bool subjectOnRed,
            BotEvaluationWeights weights,
            int nearEqualBand,
            int lookaheadCandidateCap,
            bool useLookahead,
            BotPolicyWeights policy,
            out TeamId subjectTeam)
        {
            var session = BotSelfPlay.CreateSymmetricBench(LifeCountersPerTeam);
            var participants = session.Projection.Participants;
            subjectTeam = participants.Teams
                .First(team => team.Side == (subjectOnRed ? TeamSide.Red : TeamSide.Blue)).Id;

            // A ply is composed only when somebody in this match can use one. Tier-1 is handed none
            // even when the object exists (see BotSelfPlay.Choose), so composing one for a tier-1
            // subject would pay the cost and change nothing.
            var wantsLookahead = useLookahead &&
                (subject == BotTier.TierTwo || opponent == BotTier.TierTwo);
            return BotSelfPlay.Play(
                session,
                seed,
                CommandBudget,
                BotSelfPlay.ByTeam(participants, subjectTeam, subject, opponent),
                wantsLookahead,
                BotPolicyFamilies.All,
                wantsLookahead ? ShadowFactory : null,
                weights,
                nearEqualBand,
                lookaheadCandidateCap,
                policy);
        }
    }
}
