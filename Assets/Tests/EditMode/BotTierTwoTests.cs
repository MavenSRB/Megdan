using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// FOURTEENTH amendment (the 2026-09-06 amendment + RULING 41), tier-2: a real one-ply lookahead
    /// for HOST-OWNED practice seats, self-play-tuned weights, and the seeded near-equal pick.
    /// </summary>
    /// <remarks>
    /// <para><b>What is asserted here and what is only measured.</b> The suite cases below are the
    /// ones a regression must not be allowed to pass silently: the vehicle works end to end, the
    /// tier-1 control has not moved, a seed replays, the band is inside its own covenant, and tier-2
    /// is not WORSE than the brain it replaces on seeds no sweep saw. The full-strength margins - all
    /// three opponents, twenty held-out seeds, both sides - are in
    /// <see cref="TheHeldOutGateMeasurement"/>, which is <c>[Explicit]</c> because it plays 120
    /// matches with a live one-ply probe in each of them and belongs in a worker's hands rather than
    /// in every suite run. Its numbers are recorded in the milestone notes.</para>
    /// <para><b>Suite cost is a design constraint here, not an afterthought.</b> One ply costs about
    /// 8.6 ms and a decision probes up to <see cref="BotPolicy.LookaheadCandidateCap"/> of them, so a
    /// tier-2 match is two orders of magnitude slower than a tier-1 one. Phase A's own lesson applies
    /// and is applied: sample size sized to the effect, and the expensive comparison kept to the one
    /// question the suite actually needs answered.</para>
    /// </remarks>
    public sealed class BotTierTwoTests
    {
        /// <summary>
        /// The suite gate's seed set: held-out, and DISJOINT from
        /// <see cref="BotTierTwoBench.TuningSeeds"/> by construction.
        /// </summary>
        private static readonly int[] SuiteGateSeeds = { 101, 102, 103, 104 };

        // =====================================================================================
        // THE VEHICLE
        // =====================================================================================

        /// <summary>
        /// THE HOST-OWNED SEAT WORKS: a conductor inside the authority's own process plays real
        /// seats, through the seat envelope, with a live one-ply probe, and the match advances.
        /// </summary>
        /// <remarks>
        /// This is the case that would have caught the deterministic livelock the first Solo cohort
        /// found: a brain that answers an unchanged projection identically turns one infeasible offer
        /// into an infinite loop. Progress is asserted, not assumed.
        /// </remarks>
        [Test]
        public void TheConductorPlaysHostOwnedSeatsWithALivePly()
        {
            var run = PlayHostOwned(seed: 7, maximumDecisions: 60, useLookahead: true);

            TestContext.WriteLine(
                "decisions=" + run.Decisions + " accepted=" + run.Accepted + " refused=" + run.Refused +
                " version=" + run.FinalVersion + " ms/decision=" +
                run.MillisecondsPerDecision.ToString("0.000", CultureInfo.InvariantCulture) +
                " plies=" + run.SimulatedPlies);

            Assert.That(run.Accepted, Is.GreaterThan(30),
                "A host-owned conductor made almost no accepted move: " + run.Describe());
            Assert.That(run.FinalVersion, Is.GreaterThan(30),
                "The match version barely moved, which is what a livelock looks like: " + run.Describe());
            Assert.That(run.SimulatedPlies, Is.GreaterThan(0),
                "No candidate was ever simulated, so the lookahead is not actually wired in.");
            Assert.That(run.Poisoned, Is.False,
                "The shadow probe poisoned itself: " + run.PoisonReason);
        }

        /// <summary>
        /// LATENCY. The whole point of a host-owned bot is that a person watches it play, so the
        /// question is not "is it fast" but "is it far inside the pace it is paced at".
        /// </summary>
        [Test]
        public void OneDecisionCostsFarLessThanTheWatchablePace()
        {
            var run = PlayHostOwned(seed: 11, maximumDecisions: 40, useLookahead: true);
            TestContext.WriteLine(
                "tier-2 " + run.MillisecondsPerDecision.ToString("0.000", CultureInfo.InvariantCulture) +
                " ms/decision over " + run.Decisions + " decisions, " + run.SimulatedPlies + " plies; " +
                "pace " + WatchablePaceMilliseconds + " ms");

            Assert.That(run.MillisecondsPerDecision, Is.LessThan(WatchablePaceMilliseconds / 4.0),
                "A tier-2 decision is eating the think-delay it is supposed to sit inside. " +
                run.Describe());
        }

        private const double WatchablePaceMilliseconds = 2000.0;

        /// <summary>
        /// A ONE-PLY PROBE MUST NOT BE ABLE TO DAMAGE THE MATCH, and the shadow is only safe if two
        /// sessions built from one description are genuinely independent.
        /// </summary>
        /// <remarks>
        /// Asserted rather than assumed because the failure mode is silent and severe: if a shadow
        /// shared any state with the live session, every probe would be mutating the real match
        /// through the back door and the corruption would look like a rules defect.
        /// </remarks>
        [Test]
        public void TwoSessionsFromOneInitialStateDoNotShareState()
        {
            var initialState = BotSelfPlay.CreateSymmetricBenchInitialState(
                BotTierTwoBench.LifeCountersPerTeam);
            var live = new MatchSession(initialState);
            var shadow = new MatchSession(initialState);

            var openingVersion = shadow.Projection.Version;
            var random = new DeterministicBotRandom(3);
            for (var step = 0; step < 40; step++)
            {
                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(live.Projection);
                if (candidates.Count == 0)
                {
                    break;
                }

                live.Execute(candidates[random.Next(candidates.Count)]);
            }

            Assert.That(live.Projection.Version, Is.GreaterThan(openingVersion),
                "The live session did not move, so this case proves nothing.");
            Assert.That(shadow.Projection.Version, Is.EqualTo(openingVersion),
                "A second session built from the same initial state moved when the first one did. " +
                "The shadow probe would be mutating the live match.");
        }

        // =====================================================================================
        // THE PLY ITSELF
        // =====================================================================================

        /// <summary>
        /// WHY GEOMETRY IS SUPPRESSED WHEN A PLY LANDS. A movement command moves the hero at once
        /// (milestone 8.35p), so the position the ply measures differs from the position before it in
        /// the geometry terms AND NOTHING ELSE - which is exactly what
        /// <c>BotPolicy.GeometryDelta</c> predicts, and why counting both would double it.
        /// </summary>
        [Test]
        public void AMovementPlyMovesTheGeometryTermsAndNoOthers()
        {
            var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
            var random = new DeterministicBotRandom(5);
            var examined = 0;

            for (var step = 0; step < 600 && examined < 3; step++)
            {
                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                if (candidates.Count == 0)
                {
                    break;
                }

                var movement = candidates.FirstOrDefault(value =>
                    value.Kind == DirectorCommandKind.ChooseMovementDestination);
                var hero = BotSelfPlay.DecidingHero(session.Projection);
                if (movement != null && hero.IsDefined)
                {
                    var before = BotEvaluation.Explain(session.Projection, hero);
                    var snapshot = session.CaptureSnapshot();
                    if (session.Execute(movement).Accepted)
                    {
                        var after = BotEvaluation.Explain(session.Projection, hero);
                        Assert.That(after.TeamLifeAdvantage, Is.EqualTo(before.TeamLifeAdvantage));
                        Assert.That(after.BoardPresence, Is.EqualTo(before.BoardPresence));
                        Assert.That(after.LevelAdvantage, Is.EqualTo(before.LevelAdvantage));
                        Assert.That(after.OwnLevel, Is.EqualTo(before.OwnLevel));
                        Assert.That(after.MinionPressure, Is.EqualTo(before.MinionPressure));
                        Assert.That(after.OwnHandSize, Is.EqualTo(before.OwnHandSize));
                        examined++;
                    }

                    session.RestoreSnapshot(snapshot);
                }

                var chosen = candidates[random.Next(candidates.Count)];
                if (!session.Execute(chosen).Accepted)
                {
                    break;
                }
            }

            Assert.That(examined, Is.GreaterThan(0),
                "No movement decision was reached, so the geometry claim was never tested.");
        }

        /// <summary>
        /// THE STRUCTURAL FINDING OF TIER-2, pinned as a test because it is the thing a later reader
        /// is most likely to waste a week rediscovering: <b>an evaluation weight that appears only in
        /// the position score cannot change which candidate is chosen.</b>
        /// </summary>
        /// <remarks>
        /// <para>A candidate scores <c>standing + delta</c>. <c>standing</c> is the evaluation of the
        /// position as it currently is, which is <b>the same number for every candidate of one
        /// decision</b> - so it cancels out of the ranking, and out of the near-equal band, which is
        /// measured relative to the best score. Only the deltas rank anything.</para>
        /// <para>Measured before it was understood: the sensitivity screen scaled
        /// <c>TeamLife</c>, <c>BoardPresence</c> and <c>LevelAdvantage</c> to zero, to half, to double
        /// and to quadruple, and every single configuration returned <b>exactly 50.0%</b> against the
        /// unmodified brain over forty matches. This case is why that is not a coincidence.</para>
        /// <para><b>What it means for anyone told to "tune the evaluation".</b> A term that appears
        /// only in the position score cannot rank anything; the terms that CAN only bite inside a
        /// delta. The weights that decide what this bot does are <see cref="BotPolicyWeights"/>. That
        /// is also, in hindsight, why the one weight phase A successfully tuned was a policy weight.</para>
        /// <para><b>Corrected at milestone 8.35z43, by measurement.</b> This remark used to
        /// name <c>OwnCoin</c> and <c>MinionPressure</c> (through the attack-target delta) and
        /// <c>OwnHandSize</c> (through the defense one) as terms that are NOT inert. When those three
        /// were finally play-screened they changed the win rate at no multiplier and changed no
        /// chosen command at all - see
        /// <see cref="TheFiveScreenedEvaluationTerms_MoveExactlyTheDecisionsTheirWinRatesImply"/>,
        /// which holds the numbers. Appearing in a delta's CODE is not the same as moving a decision,
        /// and this remark had inferred the second from the first.</para>
        /// </remarks>
        [Test]
        public void EvaluationTermsOutsideADeltaCannotChangeTheChosenCommand()
        {
            var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
            var walk = new DeterministicBotRandom(23);

            // Three terms that appear ONLY in the position score, scaled hard.
            var scaled = BotEvaluationWeights.PhaseA
                .WithTerm(1, BotEvaluationWeights.PhaseA.BoardPresence * 8)
                .WithTerm(2, BotEvaluationWeights.PhaseA.LevelAdvantage * 8)
                .WithTerm(3, BotEvaluationWeights.PhaseA.OwnLevel * 8);
            var compared = 0;

            for (var step = 0; step < 300; step++)
            {
                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                if (candidates.Count == 0)
                {
                    break;
                }

                var hero = BotSelfPlay.DecidingHero(session.Projection);
                if (hero.IsDefined && candidates.Count > 1)
                {
                    var underBase = BotPolicy.SelectTierTwo(
                        candidates, session.Projection, hero, new DeterministicBotRandom(7), null,
                        BotPolicyFamilies.All, BotEvaluationWeights.PhaseA, BotPolicy.ExactTiesOnly);
                    var underScaled = BotPolicy.SelectTierTwo(
                        candidates, session.Projection, hero, new DeterministicBotRandom(7), null,
                        BotPolicyFamilies.All, scaled, BotPolicy.ExactTiesOnly);
                    Assert.That(underScaled, Is.SameAs(underBase),
                        "Scaling a position-only evaluation term changed the chosen command at step " +
                        step + ". Either a delta now reads that term - in which case this case should " +
                        "name a different one - or the score is no longer standing + delta.");
                    compared++;
                }

                if (!session.Execute(candidates[walk.Next(candidates.Count)]).Accepted)
                {
                    break;
                }
            }

            TestContext.WriteLine("decisions compared: " + compared);
            Assert.That(compared, Is.GreaterThan(50),
                "Too few multi-candidate decisions were reached to make this claim.");
        }

        /// <summary>
        /// MILESTONE 8.35z43. The five evaluation terms milestone 8.35z26 shipped WITHOUT
        /// ever play-screening them, screened - and the result pinned here as a command-level fact
        /// rather than only as a win rate.
        /// </summary>
        /// <remarks>
        /// <para><b>The measurement.</b> <c>BotTierTwoTuning.TheWeightSensitivityScreen</c> was run
        /// for terms 4-8 at 0/half/2x/4x, one process each, plus terms 1 and 2 as controls. The
        /// tuning-seed win rates:</para>
        /// <list type="bullet">
        /// <item><c>OwnCoin</c> (6): 65.0% at 0, 3, 12, 24 - and 65.0% at the incumbent.</item>
        /// <item><c>MinionPressure</c> (12): 65.0% at 0, 6, 24, 48.</item>
        /// <item><c>OwnHandSize</c> (5): 65.0% at 0, 3, 10, 20.</item>
        /// <item><c>EngagementProximity</c> (4): <b>45.0%</b> at 0, 65.0% at 2, <b>42.5%</b> at 8,
        /// 57.5% at 16. The one live term of the five.</item>
        /// <item><c>ThreatAdjacency</c> (15): 67.5% at 0, then 65.0% at 8, 30, 60. The 67.5% is ONE
        /// match in forty, and this case's own decision count for it is zero - so the row is noise,
        /// corroborated by the mechanism, rather than a reason to switch the term off.</item>
        /// <item>Controls <c>BoardPresence</c> and <c>LevelAdvantage</c>: 65.0% at every value, which
        /// reproduces the recorded INVARIANCE exactly.</item>
        /// </list>
        /// <para><b>NOTHING WAS RETUNED, and the numbers are why.</b> EngagementProximity's incumbent
        /// is already the joint best of its row - every alternative ties it or is far worse - so
        /// there is nothing to move to. ThreatAdjacency at zero reads +2.5 points, which is inside
        /// both the instrument's own +/-8-point noise band and its 5-point
        /// <c>MeaningfulMargin</c>, so it is not a result. This was screening, not tuning.</para>
        /// <para><b>The control level moved from the recorded 50.0% to 65.0%, and that is expected
        /// rather than alarming.</b> The screen holds the EVALUATION vector at phase A and lets the
        /// POLICY vector default; when 50.0% was recorded, <c>BotPolicyWeights.Default</c> still
        /// equalled <c>PhaseA</c>, so the screen was measuring a mirror match. Tier-2's tuning then
        /// moved <c>CommitPrintedValue</c> 6 -> 0, so the same screen now measures a genuinely
        /// stronger brain against tier-1. The invariance - the thing the structural finding is
        /// actually about - reproduces exactly.</para>
        /// <para><b>Why this case counts DECISIONS rather than replaying the screen.</b> A win rate
        /// takes minutes and answers "did the outcome move"; this answers "did the CHOICE move",
        /// which is the mechanism, runs in about a second, and is therefore something the standing
        /// suite can keep. A term that changes no chosen command anywhere cannot move a win rate,
        /// which is what makes the three exact 65.0% rows above a consequence rather than a
        /// coincidence.</para>
        /// </remarks>
        [Test]
        public void TheFiveScreenedEvaluationTerms_MoveExactlyTheDecisionsTheirWinRatesImply()
        {
            var counts = new Dictionary<string, int>();
            foreach (var term in new[] { 4, 5, 6, 7, 8 })
            {
                counts[BotEvaluationWeights.TermNames[term]] = DecisionsChangedByScaling(term);
            }

            foreach (var pair in counts)
            {
                TestContext.WriteLine(pair.Key + ": " + pair.Value + " decisions changed");
            }

            Assert.That(counts["EngagementProximity"], Is.GreaterThan(0),
                "EngagementProximity measured 45.0% at zero against 65.0% at its incumbent, so it " +
                "must be changing chosen commands. A zero here would mean the win-rate difference " +
                "came from somewhere this case cannot see, and the screening result would be unsafe " +
                "to act on. (Measured: 61 of this walk's decisions.)");

            foreach (var inert in new[] { "OwnCoin", "MinionPressure", "ThreatAdjacency", "OwnHandSize" })
            {
                Assert.That(counts[inert], Is.EqualTo(0),
                    inert + " changed a chosen command. When this was screened it changed NONE, " +
                    "which is what made its flat 65.0% win-rate row a mechanism rather than a " +
                    "coincidence. A non-zero here means this term now reads a delta - the content " +
                    "underneath moved - and the term needs re-screening before anyone relies on the " +
                    "recorded verdict in this case's remarks.");
            }
        }

        /// <summary>
        /// How many multi-candidate decisions of one deterministic walk pick a different command when
        /// this term alone is scaled hard. Same walk, same seeds and same policy on both sides, so
        /// the term is the only difference.
        /// </summary>
        private static int DecisionsChangedByScaling(int term)
        {
            var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
            var walk = new DeterministicBotRandom(23);
            var baseline = BotEvaluationWeights.PhaseA;
            // Zero and eightfold, so a term that reads a delta at all has every chance to show it.
            var scaled = baseline.WithTerm(term, baseline[term] * 8);
            var zeroed = baseline.WithTerm(term, 0);
            var changed = 0;

            for (var step = 0; step < 300; step++)
            {
                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                if (candidates.Count == 0)
                {
                    break;
                }

                var hero = BotSelfPlay.DecidingHero(session.Projection);
                if (hero.IsDefined && candidates.Count > 1)
                {
                    var underBase = BotPolicy.SelectTierTwo(
                        candidates, session.Projection, hero, new DeterministicBotRandom(7), null,
                        BotPolicyFamilies.All, baseline, BotPolicy.ExactTiesOnly);
                    var underScaled = BotPolicy.SelectTierTwo(
                        candidates, session.Projection, hero, new DeterministicBotRandom(7), null,
                        BotPolicyFamilies.All, scaled, BotPolicy.ExactTiesOnly);
                    var underZero = BotPolicy.SelectTierTwo(
                        candidates, session.Projection, hero, new DeterministicBotRandom(7), null,
                        BotPolicyFamilies.All, zeroed, BotPolicy.ExactTiesOnly);
                    if (!ReferenceEquals(underScaled, underBase) || !ReferenceEquals(underZero, underBase))
                    {
                        changed++;
                    }
                }

                if (!session.Execute(candidates[walk.Next(candidates.Count)]).Accepted)
                {
                    break;
                }
            }

            return changed;
        }

        // =====================================================================================
        // RULING 41 - SEEDED VARIETY
        // =====================================================================================

        /// <summary>
        /// RULING 41's product requirement: the same brain on both sides must not produce the same
        /// match every time. Twelve seeds, mirror match, and BOTH sides must win at least once.
        /// </summary>
        /// <remarks>
        /// <para><b>This case was written to prove the band fixed a one-sidedness, and the
        /// measurement said there was none to fix.</b> Phase A predicted the mirror bench would give
        /// Blue 20 of 20 because the evaluation almost always has a unique argmax. On the current
        /// build it does not: band 0 already gives red 4 / blue 8 over twelve seeds, and no band from
        /// 2 to 16 widens that. The band-0 control is still printed beside the shipped band, because
        /// the honest version of this case is "the seeded stream reaches real decisions", which is
        /// what is asserted - not "the band rescued a deterministic bot", which is what it was
        /// originally going to claim.</para>
        /// <para>Asserted as a property over seeds rather than a particular seed's result: "different
        /// seeds vary" is the claim, and which side any one seed favours is not.</para>
        /// </remarks>
        [Test]
        public void TheSeededStreamProducesBothOutcomesInAMirrorMatch()
        {
            var withBand = MirrorSpread(BotPolicy.NearEqualBand);
            var withoutBand = MirrorSpread(BotPolicy.ExactTiesOnly);

            TestContext.WriteLine(
                "band " + BotPolicy.NearEqualBand + ": red " + withBand.Item1 +
                " blue " + withBand.Item2 + " undecided " + withBand.Item3);
            TestContext.WriteLine(
                "band 0 (control): red " + withoutBand.Item1 +
                " blue " + withoutBand.Item2 + " undecided " + withoutBand.Item3);

            Assert.That(withBand.Item1, Is.GreaterThan(0),
                "The mirror match is entirely one-sided, so the seeded stream decides nothing a " +
                "player would ever notice.");
            Assert.That(withBand.Item2, Is.GreaterThan(0),
                "The mirror match is entirely one-sided, so the seeded stream decides nothing a " +
                "player would ever notice.");
        }

        /// <summary>
        /// The band is a TIEBREAK, so it must stay under every weight that names a material change of
        /// position. A band that could swallow a Life counter would be a handicap wearing a ruling's
        /// name.
        /// </summary>
        [Test]
        public void TheNearEqualBandStaysBelowEveryMaterialWeight()
        {
            var weights = BotEvaluationWeights.Default;
            Assert.That(BotPolicy.NearEqualBand, Is.LessThan(weights.TeamLife));
            Assert.That(BotPolicy.NearEqualBand, Is.LessThan(weights.BoardPresence));
            Assert.That(BotPolicy.NearEqualBand, Is.LessThan(weights.LevelAdvantage));
            Assert.That(BotPolicy.NearEqualBand, Is.LessThan(weights.OwnLevel));
            Assert.That(BotPolicy.NearEqualBand, Is.GreaterThan(0),
                "Ruling 41 asked for variety among near-equal moves; a zero band grants none.");
        }

        // =====================================================================================
        // DETERMINISM
        // =====================================================================================

        /// <summary>
        /// RULING 41's other half: "seeded per match (replays reproducible)". Two runs of one seed
        /// must produce the same match, byte for byte, through the SHIPPED host-owned path and with
        /// the live probe running - a probe that leaked any state into the match would show up here
        /// as a diverging snapshot.
        /// </summary>
        [Test]
        public void OneSeedReplaysByteStable()
        {
            var first = PlayHostOwned(seed: 4242, maximumDecisions: 45, useLookahead: true);
            var second = PlayHostOwned(seed: 4242, maximumDecisions: 45, useLookahead: true);

            Assert.That(second.Decisions, Is.EqualTo(first.Decisions));
            Assert.That(second.FinalVersion, Is.EqualTo(first.FinalVersion));
            Assert.That(second.Log, Is.EqualTo(first.Log),
                "The same seed chose different commands on a second run.");
            Assert.That(second.FinalSnapshot, Is.EqualTo(first.FinalSnapshot),
                "The same seed reached a different position on a second run.");
        }

        /// <summary>
        /// Two DIFFERENT seeds must not produce the same match, or the seeded stream Ruling 41 asked
        /// for is not actually reaching any decision.
        /// </summary>
        [Test]
        public void DifferentSeedsProduceDifferentMatches()
        {
            var first = PlayHostOwned(seed: 4242, maximumDecisions: 45, useLookahead: false);
            var other = PlayHostOwned(seed: 9191, maximumDecisions: 45, useLookahead: false);

            Assert.That(other.Log, Is.Not.EqualTo(first.Log),
                "Two seeds produced an identical command sequence, so the seed decides nothing.");
        }

        // =====================================================================================
        // THE CONTROL MUST NOT MOVE
        // =====================================================================================

        /// <summary>
        /// THE TIER-1 CONTROL IS PINNED. Tier-2 tunes its own vector; the brain that ships to a
        /// spawned NETWORK seat must keep the numbers milestone 8.35z22 measured, or every margin
        /// this milestone quotes is a comparison against something nobody runs - and the shipped
        /// network bot would have silently changed behaviour, which this milestone's scope
        /// explicitly excludes.
        /// </summary>
        [Test]
        public void ThePhaseAVectorIsPinnedSoTheControlCannotDrift()
        {
            var phaseA = BotEvaluationWeights.PhaseA;
            Assert.That(phaseA.TeamLife, Is.EqualTo(1000));
            Assert.That(phaseA.BoardPresence, Is.EqualTo(30));
            Assert.That(phaseA.LevelAdvantage, Is.EqualTo(40));
            Assert.That(phaseA.OwnLevel, Is.EqualTo(25));
            Assert.That(phaseA.OwnCoin, Is.EqualTo(6));
            Assert.That(phaseA.MinionPressure, Is.EqualTo(12));
            Assert.That(phaseA.EngagementProximity, Is.EqualTo(4));
            Assert.That(phaseA.ThreatAdjacency, Is.EqualTo(15));
            Assert.That(phaseA.OwnHandSize, Is.EqualTo(5));
        }

        /// <summary>
        /// A lookahead denominated in a DIFFERENT weight vector must be refused rather than mixed in:
        /// a simulated score and a standing score in different units make their difference a
        /// difference of units. This is also what keeps tier-1 ply-free when a tier-2 probe exists in
        /// the same match.
        /// </summary>
        [Test]
        public void APlyInAnotherVectorsUnitsIsRefused()
        {
            var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
            var otherVector = BotEvaluationWeights.Default.WithTerm(0, 999);
            var probe = new ShadowSessionBotLookahead(
                session, BotTierTwoBench.ShadowFactory, otherVector);

            var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
            var hero = BotSelfPlay.DecidingHero(session.Projection);
            Assume.That(candidates.Count, Is.GreaterThan(0));

            BotPolicy.SelectTierTwo(
                candidates, session.Projection, hero, new DeterministicBotRandom(1), probe,
                BotPolicyFamilies.All, BotEvaluationWeights.Default);

            Assert.That(probe.SimulatedPlies, Is.Zero,
                "A probe measuring in another vector's units was consulted anyway.");
        }

        // =====================================================================================
        // THE GATE
        // =====================================================================================

        /// <summary>
        /// THE SUITE GATE. Tier-2, in the configuration that ships, on seeds no sweep ever read, must
        /// not have broken outright against the shipped tier-1 brain it is meant to improve on.
        /// </summary>
        /// <remarks>
        /// <para><b>Four seeds and an outright-break floor, deliberately, and the reason is honest
        /// rather than convenient.</b> Eight matches cannot establish a margin - they can only catch a
        /// regression that broke tier-2 outright, which is what a standing suite case is for. The
        /// MARGIN is established by <see cref="TheHeldOutGateMeasurement"/> over twenty held-out
        /// seeds, run by hand and recorded, because doing it in every suite run would cost minutes per
        /// run for a number that only changes when someone changes the bot.</para>
        /// <para>Stated the other way round: this case failing means tier-2 broke; this case passing
        /// does not mean tier-2 is good. The Explicit measurement is what says that.</para>
        /// <para><b>THE THRESHOLD WAS CORRECTED AT MILESTONE 8.35z26, and the correction is the point
        /// of this paragraph.</b> This case originally asserted a win rate at or above 0.5 - which is
        /// a MARGIN claim, exactly the thing the paragraph above says eight matches cannot support.
        /// The flaw was latent while the sample happened to fall the right way, and milestone 8.35z25
        /// exposed it: that milestone changed what two of Tigerclaw's Attack faces offer, every
        /// seeded walk that chooses from offers therefore re-routed, and this subsample came back
        /// 3W/5L - 37.5% - while the twenty-seed held-out measurement on the very same tree read
        /// <b>55.0% (22W/18L)</b>. Three wins in eight is an ordinary draw from a true rate of 0.55
        /// (about a one-in-four outcome), so the red was sampling noise reporting itself as a
        /// regression. The seeds are deliberately UNTOUCHED - they are the first four held-out seeds
        /// and re-picking them until the case passed would be selecting on the outcome, which is the
        /// one thing this fixture's whole tuning/held-out methodology exists to forbid. What changed
        /// instead is the assertion, down to what eight matches genuinely support: at least a quarter
        /// of the decided matches. A bot whose true rate is 0.55 falls below that about 1.6% of the
        /// time, so this still trips loudly on an outright break while no longer firing on noise.</para>
        /// </remarks>
        [Test]
        public void TierTwoIsNotWorseThanTierOne_OnSeedsItWasNotTunedOn()
        {
            var report = BotTierTwoBench.Measure(
                BotTier.TierTwo, BotTier.TierOne, SuiteGateSeeds);
            TestContext.WriteLine(report.Summary());

            Assert.That(report.Decided, Is.GreaterThanOrEqualTo(SuiteGateSeeds.Length),
                "Too few matches reached a winner to say anything. " + report.Summary());
            Assert.That(report.WinRate, Is.GreaterThanOrEqualTo(0.25),
                "Tier-2 broke outright against the tier-1 brain it replaces, on seeds it was not " +
                "tuned on. This floor is an outright-break tripwire, not a margin: the margin is " +
                nameof(TheHeldOutGateMeasurement) + "'s twenty held-out seeds. " + report.Summary());
        }

        /// <summary>
        /// THE FULL-STRENGTH GATE, run by hand: all three opponents, twenty held-out seeds, both
        /// sides, in the shipped configuration. The numbers it prints are the ones the milestone
        /// notes quote.
        /// </summary>
        /// <remarks>
        /// <para><b>Two measurements exist and a later reader needs to know which is which.</b> The
        /// tuning-session numbers quoted throughout <see cref="BotPolicyWeights"/> and
        /// <see cref="BotPolicy.NearEqualBand"/> were measured on the tier-2 branch, before milestone
        /// 8.35z25 changed what two of Tigerclaw's Attack faces offer. They are kept as the honest
        /// record of what the sweeps saw when the constants were chosen, and they are not restated
        /// as current claims.</para>
        /// <para><b>Re-measured at integration on the merged tree</b> (milestone 8.35z26, all three
        /// matchups, shipped configuration): tier-1 <b>55.0%</b> (22W/18L over twenty held-out seeds
        /// both sides), greedy <b>81.3%</b> (13W/3L), random-legal <b>93.8%</b> (15W/1L), at
        /// 18.8-22.1 ms per decision. The branch read 57.5% / 73.3% / 100.0% on the same seeds before
        /// the content moved. The margin holds; the individual matchups moved in both directions,
        /// which is what re-routing every seeded walk does and is why the number is re-measured here
        /// rather than carried forward.</para>
        /// </remarks>
        [Test]
        [Explicit("Gate measurement: 120 matches with a live one-ply probe. Minutes, not seconds.")]
        public void TheHeldOutGateMeasurement()
        {
            foreach (var opponent in new[] { BotTier.TierOne, BotTier.Greedy, BotTier.RandomLegal })
            {
                var seeds = opponent == BotTier.TierOne
                    ? BotTierTwoBench.HeldOutSeeds
                    : BotTierTwoBench.HeldOutSeeds.Take(8).ToArray();
                var report = BotTierTwoBench.Measure(BotTier.TierTwo, opponent, seeds);
                TestContext.WriteLine(report.Summary());
            }
        }

        // =====================================================================================
        // DIAGNOSTICS
        // =====================================================================================

        [Test]
        [Explicit("Diagnostic: one seed, to price the ply and prove the shadow survives a whole match.")]
        public void TheShadowProbeDiagnostic()
        {
            var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
            var participants = session.Projection.Participants;
            var redTeam = participants.Teams.First(team => team.Side == TeamSide.Red).Id;

            var result = BotSelfPlay.Play(
                session, 1, BotTierTwoBench.CommandBudget,
                BotSelfPlay.ByTeam(participants, redTeam, BotTier.TierTwo, BotTier.TierOne),
                true, BotPolicyFamilies.All, BotTierTwoBench.ShadowFactory);

            TestContext.WriteLine(
                "commands=" + result.Commands + " rounds=" + result.RoundsReached +
                " ended=" + result.Outcome.HasEnded + " stalled=" + result.Stalled +
                " budget=" + result.ExhaustedItsBudget);
            TestContext.WriteLine("simulated plies=" + result.SimulatedPlies);
            TestContext.WriteLine(
                "tier-2 " + result.MillisecondsPerDecision(BotTier.TierTwo)
                    .ToString("0.0000", CultureInfo.InvariantCulture) + " ms/decision over " +
                result.DecisionCounts[BotTier.TierTwo] + " decisions");
            TestContext.WriteLine(
                "tier-1 " + result.MillisecondsPerDecision(BotTier.TierOne)
                    .ToString("0.0000", CultureInfo.InvariantCulture) + " ms/decision over " +
                result.DecisionCounts[BotTier.TierOne] + " decisions");
            TestContext.WriteLine("refusals=" + result.Refusals.Count);
            foreach (var refusal in result.Refusals.Take(5))
            {
                TestContext.WriteLine("  " + refusal);
            }
        }

        /// <summary>
        /// SIZES <see cref="BotPolicy.LookaheadCandidateCap"/>: win rate and latency at each cap, so
        /// the constant is a measurement and a reader can see what a larger one would have bought.
        /// </summary>
        [Test]
        [Explicit("Diagnostic: sizes the lookahead candidate cap by playing it.")]
        public void TheLookaheadCapDiagnostic()
        {
            foreach (var cap in new[] { 2, 4, 6, 0 })
            {
                var report = BotTierTwoBench.Measure(
                    BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.LopsidedSeeds,
                    BotEvaluationWeights.Default, BotPolicy.NearEqualBand, cap);
                TestContext.WriteLine(
                    "cap " + (cap == 0 ? "unbounded" : cap.ToString(CultureInfo.InvariantCulture)) +
                    "  " + report.Summary());
            }
        }

        /// <summary>
        /// WHAT THE PLY IS WORTH ON ITS OWN, with the weight vector held at phase A's: tier-2 minus
        /// tier-1 with nothing changed but the lookahead.
        /// </summary>
        [Test]
        [Explicit("Diagnostic: isolates the ply from the tuning.")]
        public void ThePlyInIsolationDiagnostic()
        {
            var withPly = BotTierTwoBench.Measure(
                BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.LopsidedSeeds,
                BotEvaluationWeights.PhaseA, BotPolicy.NearEqualBand,
                BotPolicy.LookaheadCandidateCap, true);
            var withoutPly = BotTierTwoBench.Measure(
                BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.LopsidedSeeds,
                BotEvaluationWeights.PhaseA, BotPolicy.NearEqualBand,
                BotPolicy.LookaheadCandidateCap, false);
            TestContext.WriteLine("phase-A vector, ply ON : " + withPly.Summary());
            TestContext.WriteLine("phase-A vector, ply OFF: " + withoutPly.Summary());
        }

        /// <summary>
        /// HOW OFTEN THE PLY ACTUALLY CHANGES THE ANSWER. Not a win rate - a direct count, taken at
        /// every decision of a real walk: score the offered candidates with the probe and without it,
        /// under the same vector and the same seeded die, and see whether the chosen command differs.
        /// </summary>
        /// <remarks>
        /// This exists because the head-to-head said something a win rate alone cannot explain: with
        /// the ply on and the ply off, sixteen matches produced byte-identical results while 17,509
        /// plies were simulated. Either the ply changes nothing, or it changes something that never
        /// reaches the outcome. A count says which.
        /// </remarks>
        [Test]
        [Explicit("Diagnostic: counts the decisions where the one-ply probe changes the chosen command.")]
        public void ThePlyArgmaxDiagnostic()
        {
            var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
            var probe = new ShadowSessionBotLookahead(
                session, BotTierTwoBench.ShadowFactory, BotEvaluationWeights.Default);
            var walk = new DeterministicBotRandom(17);
            var decisions = 0;
            var changed = 0;
            var nonZeroGains = 0;
            var byKind = new Dictionary<DirectorCommandKind, int>();

            for (var step = 0; step < 500; step++)
            {
                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                if (candidates.Count == 0)
                {
                    break;
                }

                var hero = BotSelfPlay.DecidingHero(session.Projection);
                if (hero.IsDefined && candidates.Count > 1)
                {
                    var pliesBefore = probe.SimulatedPlies;
                    var withPly = BotPolicy.SelectTierTwo(
                        candidates, session.Projection, hero, new DeterministicBotRandom(99), probe,
                        BotPolicyFamilies.All, BotEvaluationWeights.Default, BotPolicy.ExactTiesOnly);
                    var withoutPly = BotPolicy.SelectTierTwo(
                        candidates, session.Projection, hero, new DeterministicBotRandom(99), null,
                        BotPolicyFamilies.All, BotEvaluationWeights.Default, BotPolicy.ExactTiesOnly);
                    if (probe.SimulatedPlies > pliesBefore)
                    {
                        decisions++;
                        if (!ReferenceEquals(withPly, withoutPly))
                        {
                            changed++;
                            byKind.TryGetValue(withPly.Kind, out var count);
                            byKind[withPly.Kind] = count + 1;
                        }
                    }
                }

                var standing = hero.IsDefined
                    ? BotEvaluation.Evaluate(session.Projection, hero, BotEvaluationWeights.Default)
                    : 0;
                var chosen = candidates[walk.Next(candidates.Count)];
                if (!session.Execute(chosen).Accepted)
                {
                    break;
                }

                if (hero.IsDefined &&
                    BotEvaluation.Evaluate(session.Projection, hero, BotEvaluationWeights.Default) != standing)
                {
                    nonZeroGains++;
                }
            }

            TestContext.WriteLine(
                "decisions with a ply: " + decisions + "; ply changed the answer in " + changed +
                "; executed commands that moved the evaluation at all: " + nonZeroGains);
            foreach (var entry in byKind.OrderByDescending(value => value.Value))
            {
                TestContext.WriteLine("  changed " + entry.Key + " x" + entry.Value);
            }
        }

        // =====================================================================================
        // HELPERS
        // =====================================================================================

        /// <summary>Red wins, Blue wins, undecided, in a mirror match at one band.</summary>
        internal static Tuple<int, int, int> MirrorSpread(int band)
        {
            var red = 0;
            var blue = 0;
            var undecided = 0;
            foreach (var seed in Enumerable.Range(201, 12))
            {
                var session = BotSelfPlay.CreateSymmetricBench(BotTierTwoBench.LifeCountersPerTeam);
                var participants = session.Projection.Participants;
                var redTeam = participants.Teams.First(team => team.Side == TeamSide.Red).Id;
                var result = BotSelfPlay.Play(
                    session, seed, BotTierTwoBench.CommandBudget,
                    _ => BotTier.TierTwo, false, BotPolicyFamilies.All, null,
                    BotEvaluationWeights.Default, band);
                if (!result.HasWinner)
                {
                    undecided++;
                }
                else if (result.Outcome.WinningTeamId.Equals(redTeam))
                {
                    red++;
                }
                else
                {
                    blue++;
                }
            }

            return Tuple.Create(red, blue, undecided);
        }

        private sealed class HostOwnedRun
        {
            public int Decisions;
            public int Accepted;
            public int Refused;
            public long FinalVersion;
            public double MillisecondsPerDecision;
            public int SimulatedPlies;
            public bool Poisoned;
            public string PoisonReason = string.Empty;
            public string Log = string.Empty;
            public string FinalSnapshot = string.Empty;

            public string Describe() =>
                "decisions=" + Decisions + " accepted=" + Accepted + " refused=" + Refused +
                " version=" + FinalVersion + " plies=" + SimulatedPlies +
                (Poisoned ? " POISONED: " + PoisonReason : string.Empty);
        }

        /// <summary>
        /// Plays the bench through the SHIPPED host-owned path: a real <c>MatchHost</c>, a conductor
        /// owning every seat, submissions through the seat envelope, and the shadow probe.
        /// </summary>
        private static HostOwnedRun PlayHostOwned(int seed, int maximumDecisions, bool useLookahead)
        {
            var initialState = BotSelfPlay.CreateSymmetricBenchInitialState(
                BotTierTwoBench.LifeCountersPerTeam);
            var host = MatchHost.Compose(initialState);
            var session = new LocalDirectorMatchClient(host);
            var probe = useLookahead
                ? new ShadowSessionBotLookahead(
                    session, () => new MatchSession(initialState), BotEvaluationWeights.Default)
                : null;
            var conductor = new BotSeatConductor(
                host, host.Seats.Seats.Select(seat => seat.Id), seed, probe);

            var run = new HostOwnedRun();
            var log = new List<string>();
            while (run.Decisions < maximumDecisions)
            {
                if (!conductor.TryPlayOne(out var action))
                {
                    break;
                }

                run.Decisions++;
                if (action.Accepted)
                {
                    run.Accepted++;
                }
                else
                {
                    run.Refused++;
                }

                log.Add(action.ToString());
            }

            run.FinalVersion = session.Projection.Version;
            run.MillisecondsPerDecision = conductor.MillisecondsPerDecision;
            run.SimulatedPlies = probe?.SimulatedPlies ?? 0;
            run.Poisoned = probe?.IsPoisoned ?? false;
            run.PoisonReason = probe?.PoisonReason ?? string.Empty;
            run.Log = string.Join("\n", log);
            run.FinalSnapshot = session.CaptureSnapshot();
            return run;
        }
    }
}
