using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// FOURTEENTH amendment, tier-1 phase A. The gate: a heuristic practice opponent is only worth
    /// shipping if it measurably beats the tier-0.5 pass it replaces, so the win rate is asserted
    /// here rather than asserted in prose.
    /// </summary>
    public sealed class BotBrainTests
    {
        /// <summary>
        /// Life counters low enough that p.13's ending is reachable inside a bounded run. A self-play
        /// win rate needs DECIDED matches: a budget-exhausted run has no winner and is counted for
        /// nobody, so a fixture that never ends would measure nothing at all.
        /// </summary>
        private const int LifeCountersPerTeam = 2;

        private const int CommandBudget = 2400;

        /// <summary>
        /// Twenty seeds, each played TWICE with the sides swapped. The swap is not padding: the
        /// official configuration hands one side the opening Initiative, so a brain measured only on
        /// Red would be measured together with whatever that opening is worth. Forty decided matches
        /// per comparison, and no side bias in the total.
        /// </summary>
        private static readonly int[] MeasurementSeeds =
            Enumerable.Range(1, 20).ToArray();

        /// <summary>
        /// The seed set for the LOPSIDED comparisons - anything measured against random-legal, which
        /// both brains beat by 30 to 45 points.
        /// </summary>
        /// <remarks>
        /// Sample size sized to the effect, deliberately. Twenty seeds are there to separate tier-1
        /// from the tier-0.5 pass, which is the close comparison and the one the gate turns on;
        /// spending the same twenty on a 95%-versus-5% question buys no confidence and costs real
        /// suite time. Measured: <c>TheMovedGreedyPassStillBeatsRandomLegal</c> ran 1 m 24 s at twenty
        /// seeds, longer than the actual gate, because a random-legal side drags most of its matches
        /// out to the full command budget.
        /// </remarks>
        private static readonly int[] LopsidedSeeds =
            Enumerable.Range(1, 8).ToArray();

        [Test]
        public void TierOneBeatsTheGreedyPass_OverTwentySeededSelfPlayMatches()
        {
            var report = MeasureAgainst(BotTier.Greedy);
            TestContext.WriteLine(report.ToString());

            Assert.That(report.Decided, Is.GreaterThanOrEqualTo(20),
                "A win rate needs decided matches; this comparison produced too few to mean anything.");
            Assert.That(report.WinRate, Is.GreaterThan(0.60),
                "Tier-1 phase A ships only if it measurably beats the tier-0.5 greedy pass. " + report);
        }

        /// <summary>
        /// INSTRUMENT CHECK, and it must run before any win rate is believed. Both sides play the
        /// SAME brain, so every win is the fixture's doing and none of it is the brain's. A side that
        /// wins far more than half of these is a side whose advantage would swamp any difference
        /// between two brains measured on it.
        /// </summary>
        [Test]
        [Explicit("Diagnostic: reports the fixture's own side bias rather than asserting a bot property.")]
        public void SideBiasDiagnostic()
        {
            foreach (var tier in new[] { BotTier.RandomLegal, BotTier.Greedy, BotTier.TierOne })
            {
                foreach (var lives in new[] { 2, 4, 6 })
                {
                    int red = 0, blue = 0, undecided = 0, commands = 0, rounds = 0, refused = 0;
                    foreach (var seed in MeasurementSeeds)
                    {
                        var session = BotSelfPlay.CreateSymmetricBench(lives);
                        var result = BotSelfPlay.Play(session, seed, CommandBudget, _ => tier);
                        commands += result.Commands;
                        rounds += result.RoundsReached;
                        refused += result.Refusals.Count;
                        foreach (var refusal in result.Refusals.Take(1))
                        {
                            TestContext.WriteLine("    OFFER-FEASIBILITY: " + refusal);
                        }
                        if (!result.HasWinner)
                        {
                            undecided++;
                            continue;
                        }

                        var side = session.Projection.Participants.Teams
                            .First(team => team.Id.Equals(result.Outcome.WinningTeamId)).Side;
                        if (side == TeamSide.Red)
                        {
                            red++;
                        }
                        else
                        {
                            blue++;
                        }
                    }

                    TestContext.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0,-11} lives={1}  red={2,2} blue={3,2} undecided={4,2}  " +
                        "avg commands={5,5:F0} avg rounds={6,4:F1}  refusals={7}",
                        tier, lives, red, blue, undecided,
                        commands / (double)MeasurementSeeds.Length,
                        rounds / (double)MeasurementSeeds.Length, refused));
                }
            }
        }

        /// <summary>
        /// ABLATION. Runs the same head-to-head with one policy family at a time returned to the die,
        /// so the win rate can be attributed to a family instead of to "the heuristics". A family
        /// whose REMOVAL raises the win rate is a heuristic that is wrong.
        /// </summary>
        [Test]
        [Explicit("Diagnostic: attributes the measured win rate to individual policy families.")]
        public void AblationDiagnostic()
        {
            var families = new[]
            {
                ("full", BotPolicyFamilies.All),
                ("-commitment", BotPolicyFamilies.All & ~BotPolicyFamilies.Commitment),
                ("-turnEconomy", BotPolicyFamilies.All & ~BotPolicyFamilies.TurnEconomy),
                ("-defense", BotPolicyFamilies.All & ~BotPolicyFamilies.Defense),
                ("-attackTarget", BotPolicyFamilies.All & ~BotPolicyFamilies.AttackTarget),
                ("-geometry", BotPolicyFamilies.All & ~BotPolicyFamilies.Geometry),
                ("-actionStart", BotPolicyFamilies.All & ~BotPolicyFamilies.ActionStart),
                ("commitmentOnly", BotPolicyFamilies.Commitment),
                ("none", BotPolicyFamilies.None)
            };

            foreach (var (label, enabled) in families)
            {
                int wins = 0, losses = 0, undecided = 0, redWins = 0;
                foreach (var seed in MeasurementSeeds)
                {
                    foreach (var tierOneOnRed in new[] { true, false })
                    {
                        var session = BotSelfPlay.CreateSymmetricBench(LifeCountersPerTeam);
                        var participants = session.Projection.Participants;
                        var tierOneTeam = participants.Teams
                            .First(team => team.Side == (tierOneOnRed ? TeamSide.Red : TeamSide.Blue)).Id;
                        var result = BotSelfPlay.Play(
                            session, seed, CommandBudget,
                            BotSelfPlay.ByTeam(participants, tierOneTeam, BotTier.Greedy),
                            useLookahead: false, families: enabled);
                        if (!result.HasWinner)
                        {
                            undecided++;
                        }
                        else if (result.Outcome.WinningTeamId.Equals(tierOneTeam))
                        {
                            wins++;
                            if (tierOneOnRed)
                            {
                                redWins++;
                            }
                        }
                        else
                        {
                            losses++;
                        }
                    }
                }

                TestContext.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0,-16} wins={1,2} losses={2,2} undecided={3,2}  winRate={4,6:P1}  (as red {5}/{6})",
                    label, wins, losses, undecided,
                    wins + losses == 0 ? 0.0 : (double)wins / (wins + losses),
                    redWins, MeasurementSeeds.Length));
            }
        }

        /// <summary>
        /// HELD-OUT VALIDATION. The measurement seeds above were used to choose a weight, which means
        /// a win rate on them is partly a measurement of that choice. These seeds were not, so this is
        /// the number that says whether the tuning generalises or was fitted to twenty matches.
        /// </summary>
        [Test]
        public void TierOneBeatsTheGreedyPass_OnSeedsItWasNotTunedOn()
        {
            var report = MeasureAgainst(BotTier.Greedy, Enumerable.Range(101, 20).ToArray());
            TestContext.WriteLine(report.ToString());

            Assert.That(report.Decided, Is.GreaterThanOrEqualTo(20));
            Assert.That(report.WinRate, Is.GreaterThan(0.60),
                "The tier-1 weights do not generalise past the seeds they were chosen on. " + report);
        }

        /// <summary>
        /// LOOKAHEAD COST AND VALUE. The brief commissioned a one-ply lookahead over snapshot/restore;
        /// a seat cannot have one (see <see cref="IBotLookahead"/>), but a full-information
        /// composition can, so this measures what it would be worth if it could - both in win rate
        /// and in the decision latency the 2 s watchable pace has to absorb.
        /// </summary>
        [Test]
        [Explicit("Diagnostic: prices the one-ply lookahead the shipped seat cannot have.")]
        public void LookaheadCostDiagnostic()
        {
            foreach (var useLookahead in new[] { false, true })
            {
                int wins = 0, losses = 0, undecided = 0, plies = 0;
                double ticks = 0;
                var decisions = 0;
                foreach (var seed in MeasurementSeeds)
                {
                    foreach (var tierOneOnRed in new[] { true, false })
                    {
                        var session = BotSelfPlay.CreateSymmetricBench(LifeCountersPerTeam);
                        var participants = session.Projection.Participants;
                        var tierOneTeam = participants.Teams
                            .First(team => team.Side == (tierOneOnRed ? TeamSide.Red : TeamSide.Blue)).Id;
                        var result = BotSelfPlay.Play(
                            session, seed, CommandBudget,
                            BotSelfPlay.ByTeam(participants, tierOneTeam, BotTier.Greedy),
                            useLookahead);
                        plies += result.SimulatedPlies;
                        ticks += result.DecisionTicks[BotTier.TierOne];
                        decisions += result.DecisionCounts[BotTier.TierOne];
                        if (!result.HasWinner)
                        {
                            undecided++;
                        }
                        else if (result.Outcome.WinningTeamId.Equals(tierOneTeam))
                        {
                            wins++;
                        }
                        else
                        {
                            losses++;
                        }
                    }
                }

                TestContext.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "lookahead={0,-5} wins={1,2} losses={2,2} undecided={3,2} winRate={4,6:P1}  " +
                    "plies={5,6}  {6:F3} ms/decision",
                    useLookahead, wins, losses, undecided,
                    wins + losses == 0 ? 0.0 : (double)wins / (wins + losses),
                    plies,
                    decisions == 0 ? 0.0 : ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency / decisions));
            }
        }

        /// <summary>
        /// SNAPSHOT ROUND-TRIP REPRO, deliberately with no bot in it at all. Drives the bench with the
        /// plain random-legal loop and, at every step, captures a snapshot and restores it into the
        /// same live session. A capture the same session cannot read back is a defect in
        /// capture/restore, and this fixture exists to say so without a policy anywhere near it.
        /// </summary>
        [Test]
        [Explicit("Diagnostic: reproduces the snapshot capture/restore asymmetry the lookahead met.")]
        public void SnapshotRoundTripDiagnostic()
        {
            var failures = 0;
            var captureTicks = 0L;
            var restoreTicks = 0L;
            var payloadBytes = 0L;
            var samples = 0;
            var clock = new System.Diagnostics.Stopwatch();
            foreach (var seed in MeasurementSeeds)
            {
                var session = BotSelfPlay.CreateSymmetricBench(LifeCountersPerTeam);
                var random = new DeterministicBotRandom(seed);
                for (var step = 0; step < 600 &&
                    session.Projection.Phase != RoundResolutionPhase.GameEnded; step++)
                {
                    string payload;
                    try
                    {
                        clock.Restart();
                        payload = session.CaptureSnapshot();
                        clock.Stop();
                        captureTicks += clock.ElapsedTicks;
                        payloadBytes += payload?.Length ?? 0;
                    }
                    catch (Exception capture)
                    {
                        TestContext.WriteLine("seed " + seed + " step " + step + " CAPTURE threw " +
                            capture.GetType().Name + ": " + capture.Message);
                        failures++;
                        break;
                    }

                    try
                    {
                        clock.Restart();
                        var restored = session.RestoreSnapshot(payload);
                        clock.Stop();
                        restoreTicks += clock.ElapsedTicks;
                        samples++;
                        if (!restored.Accepted)
                        {
                            TestContext.WriteLine("seed " + seed + " step " + step +
                                " RESTORE refused: " + restored.Detail);
                            failures++;
                            break;
                        }
                    }
                    catch (Exception restore)
                    {
                        TestContext.WriteLine("seed " + seed + " step " + step + " RESTORE threw " +
                            restore.GetType().Name + ": " + restore.Message);
                        failures++;
                        break;
                    }

                    var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                    if (candidates.Count == 0 ||
                        !session.Execute(candidates[random.Next(candidates.Count)]).Accepted)
                    {
                        break;
                    }
                }
            }

            TestContext.WriteLine("seeds with a capture/restore failure: " + failures +
                " of " + MeasurementSeeds.Length);
            var frequency = System.Diagnostics.Stopwatch.Frequency;
            TestContext.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "CLONE COST over {0} round trips: capture {1:F3} ms, restore {2:F3} ms, " +
                "one ply = {3:F3} ms, payload {4:F0} chars",
                samples,
                samples == 0 ? 0 : captureTicks * 1000.0 / frequency / samples,
                samples == 0 ? 0 : restoreTicks * 1000.0 / frequency / samples,
                samples == 0 ? 0 : (captureTicks + restoreTicks) * 1000.0 / frequency / samples,
                samples == 0 ? 0 : payloadBytes / (double)samples));
        }

        [Test]
        public void TierOneBeatsRandomLegal_OverSeededSelfPlayMatches()
        {
            var report = MeasureAgainst(BotTier.RandomLegal, LopsidedSeeds);
            TestContext.WriteLine(report.ToString());

            Assert.That(report.Decided, Is.GreaterThanOrEqualTo(LopsidedSeeds.Length));
            Assert.That(report.WinRate, Is.GreaterThan(0.60),
                "Tier-1 must beat the THIRTEENTH amendment's random-legal driver by a wide margin. " + report);
        }

        /// <summary>
        /// THE MOVE WAS FAITHFUL. The tier-0.5 preferences left <c>PrivateSeatDriver</c> for
        /// <c>BotPolicy</c>, and its own fixture still drives the driver's now-delegating signatures -
        /// but that fixture is harness-excluded (it references Presentation), so this is the harness's
        /// own behavioural net: the moved code, driven through <c>BotPolicy.SelectGreedy</c> over
        /// whole matches, must still play the way the greedy pass played - decisively better than the
        /// random-legal driver it replaced.
        /// </summary>
        [Test]
        public void TheMovedGreedyPassStillBeatsRandomLegal()
        {
            int greedyWins = 0, randomWins = 0, undecided = 0;
            foreach (var seed in LopsidedSeeds)
            {
                foreach (var greedyOnRed in new[] { true, false })
                {
                    var session = BotSelfPlay.CreateSymmetricBench(LifeCountersPerTeam);
                    var participants = session.Projection.Participants;
                    var greedyTeam = participants.Teams
                        .First(team => team.Side == (greedyOnRed ? TeamSide.Red : TeamSide.Blue)).Id;
                    var result = BotSelfPlay.Play(
                        session, seed, CommandBudget,
                        heroId =>
                        {
                            try
                            {
                                return participants.GetTeamForHero(heroId).Id.Equals(greedyTeam)
                                    ? BotTier.Greedy
                                    : BotTier.RandomLegal;
                            }
                            catch (Exception)
                            {
                                return BotTier.RandomLegal;
                            }
                        });
                    if (!result.HasWinner)
                    {
                        undecided++;
                    }
                    else if (result.Outcome.WinningTeamId.Equals(greedyTeam))
                    {
                        greedyWins++;
                    }
                    else
                    {
                        randomWins++;
                    }
                }
            }

            var rate = greedyWins + randomWins == 0 ? 0.0 : (double)greedyWins / (greedyWins + randomWins);
            TestContext.WriteLine("moved greedy vs random-legal: wins=" + greedyWins +
                " losses=" + randomWins + " undecided=" + undecided +
                " winRate=" + rate.ToString("P1", CultureInfo.InvariantCulture));
            Assert.That(rate, Is.GreaterThan(0.60),
                "The tier-0.5 preferences stopped playing like themselves after moving to BotPolicy.");
        }

        /// <summary>
        /// Determinism per seed, proven the only way it can be: run the same seed twice and compare
        /// what the two runs actually did. Every preference is computed BEFORE the seeded tiebreak, so
        /// two runs of one seed must agree command for command.
        /// </summary>
        [Test]
        public void OneSeedNamesOneMatch_ForTheTierOneBrain()
        {
            var first = PlayOne(7, BotTier.Greedy, tierOneOnRed: true);
            var second = PlayOne(7, BotTier.Greedy, tierOneOnRed: true);

            Assert.That(second.Commands, Is.EqualTo(first.Commands),
                "The same seed produced a different number of commands the second time.");
            Assert.That(second.RoundsReached, Is.EqualTo(first.RoundsReached));
            Assert.That(second.Outcome.HasEnded, Is.EqualTo(first.Outcome.HasEnded));
            Assert.That(second.Outcome.WinningTeamId.Value, Is.EqualTo(first.Outcome.WinningTeamId.Value));
        }

        /// <summary>
        /// The policy may narrow what it was offered and must never widen it. A chosen command that
        /// was not in the candidate list would be a command the rules never offered.
        /// </summary>
        [Test]
        public void TierOneOnlyEverAnswersWithACommandItWasOffered()
        {
            var session = BotSelfPlay.CreateSymmetricBench(LifeCountersPerTeam);
            var random = new DeterministicBotRandom(11);
            var inspected = 0;

            for (var step = 0; step < 400 && session.Projection.Phase != RoundResolutionPhase.GameEnded; step++)
            {
                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                if (candidates.Count == 0)
                {
                    break;
                }

                var hero = BotSelfPlay.DecidingHero(session.Projection);
                var chosen = BotPolicy.SelectTierOne(candidates, session.Projection, hero, random) ??
                    candidates[random.Next(candidates.Count)];
                Assert.That(candidates, Contains.Item(chosen),
                    "Tier-1 answered with a command that was not offered.");
                inspected++;

                Assert.That(session.Execute(chosen).Accepted, Is.True,
                    "A command tier-1 chose from the offered set was refused by the rules.");
            }

            Assert.That(inspected, Is.GreaterThan(50), "Too few decisions inspected to mean anything.");
        }

        /// <summary>
        /// THE PRIVACY CONFORMANCE CASE, and the whole self-play measurement leans on it.
        /// </summary>
        /// <remarks>
        /// The bots measured in this file read the DIRECTOR's projection, because that is what a
        /// headless session has. The bot that ships reads its own seat's, which hides other hands
        /// entirely. Those two agree only if every term the evaluation reads is public - so this
        /// scores the same live position through both and demands the same number. A failure here
        /// does not merely fail a test: it says the measurement above stopped describing the shipped
        /// bot, and it names the position where they diverged.
        /// </remarks>
        [Test]
        public void TheEvaluationReadsOnlyWhatASeatCanSee()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var director = new LocalDirectorMatchClient(host);
            var random = new DeterministicBotRandom(23);
            var compared = 0;

            for (var step = 0; step < 300 && director.Projection.Phase != RoundResolutionPhase.GameEnded; step++)
            {
                foreach (var seat in host.Seats.Seats)
                {
                    var seatProjection = host.ClientForSeat(seat.Id).Projection;
                    var throughSeat = BotEvaluation.Explain(seatProjection, seat.HeroId);
                    var throughDirector = BotEvaluation.Explain(director.Projection, seat.HeroId);

                    Assert.That(throughSeat.Total, Is.EqualTo(throughDirector.Total),
                        "Seat " + seat.Id + " scores this position differently from the Director at step " +
                        step + ". The evaluation is reading something a seat cannot see. Seat: " +
                        Describe(throughSeat) + " Director: " + Describe(throughDirector));
                    compared++;
                }

                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(director.Projection);
                if (candidates.Count == 0)
                {
                    break;
                }

                if (!director.Execute(candidates[random.Next(candidates.Count)]).Accepted)
                {
                    break;
                }
            }

            Assert.That(compared, Is.GreaterThan(100), "Too few positions compared to mean anything.");
        }

        private static string Describe(BotEvaluationBreakdown value) =>
            "life=" + value.TeamLifeAdvantage + " presence=" + value.BoardPresence +
            " level=" + value.LevelAdvantage + " coins=" + value.OwnCoins +
            " minions=" + value.MinionPressure + " proximity=" + value.EngagementProximity +
            " threats=" + value.AdjacentThreats + " hand=" + value.OwnHandSize;

        private static BotSelfPlayResult PlayOne(int seed, BotTier opponent, bool tierOneOnRed)
        {
            var session = BotSelfPlay.CreateSymmetricBench(LifeCountersPerTeam);
            var participants = session.Projection.Participants;
            var tierOneTeam = participants.Teams
                .First(team => team.Side == (tierOneOnRed ? TeamSide.Red : TeamSide.Blue)).Id;
            return BotSelfPlay.Play(
                session, seed, CommandBudget,
                BotSelfPlay.ByTeam(participants, tierOneTeam, opponent));
        }

        private static ComparisonReport MeasureAgainst(BotTier opponent, int[] seeds = null)
        {
            seeds = seeds ?? MeasurementSeeds;
            var report = new ComparisonReport(opponent, seeds.Length);
            foreach (var seed in seeds)
            {
                foreach (var tierOneOnRed in new[] { true, false })
                {
                    var session = BotSelfPlay.CreateSymmetricBench(LifeCountersPerTeam);
                    var participants = session.Projection.Participants;
                    var tierOneTeam = participants.Teams
                        .First(team => team.Side == (tierOneOnRed ? TeamSide.Red : TeamSide.Blue)).Id;
                    var result = BotSelfPlay.Play(
                        session, seed, CommandBudget,
                        BotSelfPlay.ByTeam(participants, tierOneTeam, opponent));
                    report.Add(result, tierOneTeam, tierOneOnRed);
                }
            }

            return report;
        }

        private sealed class ComparisonReport
        {
            private readonly BotTier opponent;
            private readonly List<string> rows = new List<string>();

            private readonly int seedCount;

            public ComparisonReport(BotTier opponent, int seedCount)
            {
                this.opponent = opponent;
                this.seedCount = seedCount;
            }

            public int Wins { get; private set; }
            public int Losses { get; private set; }
            public int Undecided { get; private set; }

            /// <summary>
            /// Wins split by which side tier-1 held. Reported because a single total hides the one
            /// thing that matters when the game itself is not side-neutral: whether the brain wins
            /// the HARD seat more often than the opponent's own mirror does.
            /// </summary>
            public int WinsAsRed { get; private set; }

            public int WinsAsBlue { get; private set; }
            public int Decided => Wins + Losses;
            public double WinRate => Decided == 0 ? 0.0 : (double)Wins / Decided;
            public double TierOneMillisecondsPerDecision { get; private set; }
            public double OpponentMillisecondsPerDecision { get; private set; }

            private double tierOneTicks;
            private int tierOneDecisions;
            private double opponentTicks;
            private int opponentDecisions;

            public void Add(BotSelfPlayResult result, TeamId tierOneTeam, bool tierOneOnRed)
            {
                string verdict;
                if (!result.HasWinner)
                {
                    Undecided++;
                    verdict = result.Stalled ? "STALLED" : "budget";
                }
                else if (result.Outcome.WinningTeamId.Equals(tierOneTeam))
                {
                    Wins++;
                    if (tierOneOnRed)
                    {
                        WinsAsRed++;
                    }
                    else
                    {
                        WinsAsBlue++;
                    }

                    verdict = "tier1";
                }
                else
                {
                    Losses++;
                    verdict = "opponent";
                }

                tierOneTicks += result.DecisionTicks[BotTier.TierOne];
                tierOneDecisions += result.DecisionCounts[BotTier.TierOne];
                opponentTicks += result.DecisionTicks[opponent];
                opponentDecisions += result.DecisionCounts[opponent];
                TierOneMillisecondsPerDecision = tierOneDecisions == 0
                    ? 0.0
                    : tierOneTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency / tierOneDecisions;
                OpponentMillisecondsPerDecision = opponentDecisions == 0
                    ? 0.0
                    : opponentTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency / opponentDecisions;

                rows.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "  seed {0,3}  tier1={1,-4}  {2,-8}  commands={3,5}  rounds={4,3}  cause={5}",
                    result.Seed, tierOneOnRed ? "red" : "blue", verdict, result.Commands,
                    result.RoundsReached, result.Outcome.HasEnded ? result.Outcome.Cause.ToString() : "-"));
            }

            public override string ToString()
            {
                var text = new StringBuilder();
                text.AppendLine("tier-1 vs " + opponent + " - " + seedCount +
                    " seeds x 2 side assignments");
                foreach (var row in rows)
                {
                    text.AppendLine(row);
                }

                text.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "  TOTAL wins={0} losses={1} undecided={2} winRate={3:P1} " +
                    "[as red {4}/{5}, as blue {6}/{5}] " +
                    "(tier1 {7:F3} ms/decision, {8} {9:F3} ms/decision)",
                    Wins, Losses, Undecided, WinRate,
                    WinsAsRed, seedCount, WinsAsBlue,
                    TierOneMillisecondsPerDecision,
                    opponent, OpponentMillisecondsPerDecision));
                return text.ToString();
            }
        }
    }
}
