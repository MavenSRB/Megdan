using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application.Bots;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// FOURTEENTH amendment, tier-2. The self-play sweeps that PRODUCED
    /// <see cref="BotEvaluationWeights.Default"/> and <see cref="BotPolicy.NearEqualBand"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Every case here is <c>[Explicit]</c> and none of them is a gate.</b> A sweep is an
    /// instrument, run by hand when the numbers need re-deriving; the gate that certifies its output
    /// lives in <see cref="BotTierTwoTests"/>, runs on seeds no sweep here may read, and is what the
    /// suite enforces. Checked in rather than thrown away for the reason phase A's single tuned
    /// weight makes obvious: a number nobody can reproduce is a number nobody can revisit when the
    /// roster expansion moves the game underneath it.</para>
    /// <para><b>THE COST DECISION, stated rather than buried, because it is the one thing here a
    /// reader should challenge.</b> The weight sweep plays with the LOOKAHEAD OFF. Measured: one
    /// tier-2 match with the ply on costs about 15-21 s and the same match with it off costs about
    /// 0.3 s, so a nine-term sweep with the ply on would run for hours and one with it off runs in
    /// minutes. The justification is that the weights ARE the evaluation, and the evaluation is the
    /// single thing both halves of tier-2's score are denominated in - the static predictions and
    /// the measured ply alike - so a vector that judges positions better judges them better in both
    /// halves. <b>That is an argument, not a proof, which is exactly why it does not decide
    /// anything:</b> the sweep only proposes a vector, and the vector is accepted or rejected by a
    /// gate that plays the FULL shipped configuration, ply and all, on held-out seeds. If the
    /// argument were wrong the gate would fail, which is the honest place for it to be caught.</para>
    /// <para><b>The no-chasing-noise rule.</b> Forty matches put roughly +/-8 points of noise on a
    /// win rate, and coordinate descent over nine terms will happily walk into that noise and call it
    /// a discovery. So a term only moves when the candidate beats the incumbent by
    /// <see cref="MeaningfulMargin"/> or more; anything smaller keeps the incumbent. This costs real
    /// improvements that happen to be small and it refuses far more fake ones, which on a
    /// twenty-seed instrument is the right trade.</para>
    /// </remarks>
    public sealed class BotTierTwoTuning
    {
        /// <summary>
        /// How much better a candidate weight must measure before it displaces the incumbent. See
        /// the no-chasing-noise rule in the class remarks.
        /// </summary>
        private const double MeaningfulMargin = 0.05;

        /// <summary>
        /// The multipliers each term is tried at, around its incumbent value. Wide enough to find a
        /// term that is an order of magnitude wrong (which is what phase A's Initiative weight turned
        /// out to be) and coarse enough to finish.
        /// </summary>
        private static readonly double[] Multipliers = { 0.0, 0.5, 1.0, 2.0, 4.0 };

        /// <summary>
        /// THE WEIGHT SENSITIVITY SCREEN, one term per case. For each candidate value of one term,
        /// with every other term at the phase-A vector, plays the result against the shipped tier-1
        /// brain over the tuning seeds and reports the win rate.
        /// </summary>
        /// <remarks>
        /// <para><b>One term per test case, and that shape is a scar.</b> The first version walked all
        /// nine terms twice inside ONE case - roughly 2,900 matches - and the NUnit test host
        /// CRASHED partway through ("Test host process crashed"), twice, taking every measurement
        /// with it. Splitting the work so each process plays a few hundred matches and prints its
        /// result before the next one starts is what makes a long sweep survivable; it also means a
        /// crash costs one term rather than the whole run. Run them with
        /// <c>--filter "TheWeightSensitivityScreen"</c> to do all nine, or name one case to redo a
        /// single term.</para>
        /// <para><b>A screen, not a descent.</b> Each term is varied against the SAME base rather
        /// than against a moving incumbent, so the numbers are comparable to each other and the
        /// result is a sensitivity table a reader can audit. Coordinate descent would carry an
        /// accepted move into every later measurement, which on a +/-8-point instrument is how a
        /// sweep talks itself into a vector nobody can reproduce. Assembling the winning terms into
        /// one vector and CONFIRMING that vector - first on the tuning seeds, then on held-out ones -
        /// is the second stage, and it is where a term that only looked good in isolation is caught.</para>
        /// </remarks>
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [Explicit("Tuning instrument: one evaluation term's sensitivity. Minutes, not seconds.")]
        public void TheWeightSensitivityScreen(int term)
        {
            var baseline = BotEvaluationWeights.PhaseA;
            TestContext.WriteLine(
                "TERM " + BotEvaluationWeights.TermNames[term] + " (base " + baseline[term] + ")");
            foreach (var value in CandidateValues(baseline, term))
            {
                var rate = RateOf(baseline.WithTerm(term, value));
                TestContext.WriteLine(
                    "  " + BotEvaluationWeights.TermNames[term] + "=" +
                    value.ToString(CultureInfo.InvariantCulture) + " -> " + Percent(rate));
                GC.Collect();
            }
        }

        /// <summary>
        /// THE POLICY SENSITIVITY SCREEN - the one that finds anything. Same shape as
        /// <see cref="TheWeightSensitivityScreen"/>, over <see cref="BotPolicyWeights"/> instead.
        /// </summary>
        /// <remarks>
        /// The evaluation screen exists to be complete; this one exists because it is where the
        /// leverage is. A candidate's score is <c>standing + delta</c> and <c>standing</c> is the same
        /// for every candidate of a decision, so the evaluation cancels and only these weights rank
        /// anything. Phase A's single successful tuning - <c>CommitInitiative</c>, worth 31 points -
        /// is term 5 of this vector, and its ten untouched neighbours are the rest.
        /// </remarks>
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [Explicit("Tuning instrument: one policy weight's sensitivity. Minutes, not seconds.")]
        public void ThePolicySensitivityScreen(int term)
        {
            var baseline = BotPolicyWeights.PhaseA;
            TestContext.WriteLine(
                "POLICY " + BotPolicyWeights.TermNames[term] + " (base " + baseline[term] + ")");
            foreach (var value in PolicyCandidateValues(baseline, term))
            {
                var rate = BotTierTwoBench.Measure(
                    BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.TuningSeeds,
                    BotEvaluationWeights.PhaseA, BotPolicy.ExactTiesOnly,
                    BotPolicy.LookaheadCandidateCap, false, baseline.WithTerm(term, value)).WinRate;
                TestContext.WriteLine(
                    "  " + BotPolicyWeights.TermNames[term] + "=" +
                    value.ToString(CultureInfo.InvariantCulture) + " -> " + Percent(rate));
                GC.Collect();
            }
        }

        /// <summary>
        /// The values a policy term is tried at. Zero is INCLUDED here, unlike the evaluation screen:
        /// a policy weight at zero means "this preference is switched off", which is a legitimate and
        /// interesting answer - it is the ablation the families flag already supports, at one weight's
        /// resolution. A sign flip is included for the same reason phase A's Initiative weight needed
        /// one: the direction of a preference is exactly the thing self-play can falsify.
        /// </summary>
        private static IEnumerable<int> PolicyCandidateValues(BotPolicyWeights incumbent, int term)
        {
            var current = incumbent[term];
            var seen = new HashSet<int> { current };
            foreach (var multiplier in new[] { -1.0, 0.0, 0.5, 2.0, 4.0 })
            {
                var value = (int)Math.Round(current * multiplier, MidpointRounding.AwayFromZero);
                if (seen.Add(value))
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// STAGE TWO, THE ONE THAT DECIDES. Every candidate vector the screen suggested, measured on
        /// the tuning seeds AND on the held-out ones side by side.
        /// </summary>
        /// <remarks>
        /// <b>This is the case that stopped tier-2 shipping an overfitted vector.</b> The screen's two
        /// winners assembled together measured 66.7% on the seeds they were chosen on and 45.0% on
        /// seeds they had never seen - a 21.7-point collapse, which is what fitting twenty matches
        /// looks like. Printing both columns for every candidate is the whole method; a single-column
        /// sweep would have shipped it.
        /// </remarks>
        [Test]
        [Explicit("Tuning instrument: tuning-vs-held-out for every candidate vector. Minutes.")]
        public void TheCandidateVectorHeldOutScreen()
        {
            var candidates = new List<KeyValuePair<string, BotPolicyWeights>>
            {
                new KeyValuePair<string, BotPolicyWeights>("phaseA (control)", BotPolicyWeights.PhaseA),
                new KeyValuePair<string, BotPolicyWeights>(
                    "CommitInitiative=-40", BotPolicyWeights.PhaseA.WithTerm(5, -40)),
                new KeyValuePair<string, BotPolicyWeights>(
                    "CommitInitiative=-20", BotPolicyWeights.PhaseA.WithTerm(5, -20)),
                new KeyValuePair<string, BotPolicyWeights>(
                    "CommitPrintedValue=0", BotPolicyWeights.PhaseA.WithTerm(8, 0)),
                new KeyValuePair<string, BotPolicyWeights>(
                    "CommitReachableAttack=0", BotPolicyWeights.PhaseA.WithTerm(6, 0)),
                new KeyValuePair<string, BotPolicyWeights>(
                    "both winners", BotPolicyWeights.PhaseA.WithTerm(5, -40).WithTerm(8, 0))
            };

            foreach (var candidate in candidates)
            {
                var tuning = BotTierTwoBench.Measure(
                    BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.TuningSeeds,
                    BotEvaluationWeights.PhaseA, BotPolicy.ExactTiesOnly,
                    BotPolicy.LookaheadCandidateCap, false, candidate.Value).WinRate;
                var heldOut = BotTierTwoBench.Measure(
                    BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.HeldOutSeeds,
                    BotEvaluationWeights.PhaseA, BotPolicy.ExactTiesOnly,
                    BotPolicy.LookaheadCandidateCap, false, candidate.Value).WinRate;
                TestContext.WriteLine(
                    candidate.Key.PadRight(26) + " tuning " + Percent(tuning) +
                    "   held-out " + Percent(heldOut));
                GC.Collect();
            }
        }

        /// <summary>
        /// Confirms whatever is currently in <see cref="BotPolicyWeights.Default"/>, on the tuning
        /// seeds and then on the held-out ones.
        /// </summary>
        [Test]
        [Explicit("Tuning instrument: confirms the assembled vector on tuning and held-out seeds.")]
        public void TheAssembledVectorConfirmation()
        {
            var evaluation = BotEvaluationWeights.Default;
            var policy = BotPolicyWeights.Default;
            TestContext.WriteLine("evaluation: " + evaluation.ToVectorString());
            TestContext.WriteLine("policy    : " + policy.ToVectorString());
            TestContext.WriteLine(
                "tuning    : " + Percent(BotTierTwoBench.Measure(
                    BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.TuningSeeds,
                    evaluation, BotPolicy.ExactTiesOnly,
                    BotPolicy.LookaheadCandidateCap, false, policy).WinRate));
            TestContext.WriteLine(
                "held-out  : " + Percent(BotTierTwoBench.Measure(
                    BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.HeldOutSeeds,
                    evaluation, BotPolicy.ExactTiesOnly,
                    BotPolicy.LookaheadCandidateCap, false, policy).WinRate));
        }

        /// <summary>
        /// SIZES <see cref="BotPolicy.NearEqualBand"/>. Ruling 41 asked for variety among near-equal
        /// moves; this asks what each band costs in strength, which is the only thing that decides
        /// how wide "near-equal" may be.
        /// </summary>
        [Test]
        [Explicit("Tuning instrument: sizes the Ruling 41 near-equal band.")]
        public void TheNearEqualBandSweep()
        {
            foreach (var band in new[] { 0, 8, 16, 24, 32, 64, 128 })
            {
                var report = BotTierTwoBench.Measure(
                    BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.TuningSeeds,
                    BotEvaluationWeights.Default, band, BotPolicy.LookaheadCandidateCap, false);
                TestContext.WriteLine("band " + band + "  " + report.Summary());
            }
        }

        /// <summary>
        /// THE OTHER HALF OF SIZING THE BAND. <see cref="TheNearEqualBandSweep"/> says what each band
        /// COSTS; this says what each band BUYS - how many different outcomes a mirror match produces
        /// across twelve seeds. A band is worth having only where those two curves cross.
        /// </summary>
        [Test]
        [Explicit("Tuning instrument: what variety each near-equal band actually buys.")]
        public void TheMirrorVarietyDiagnostic()
        {
            foreach (var band in new[] { 0, 2, 4, 8, 12, 16 })
            {
                var spread = BotTierTwoTests.MirrorSpread(band);
                TestContext.WriteLine(
                    "band " + band + ": red " + spread.Item1 + " blue " + spread.Item2 +
                    " undecided " + spread.Item3);
            }
        }

        /// <summary>
        /// The vector's win rate against the shipped tier-1 brain on the TUNING seeds, with the ply
        /// off. The sweep's objective function and nothing else.
        /// </summary>
        /// <remarks>
        /// AT BAND ZERO, deliberately. The band is a deliberate handicap - it trades strength for
        /// variety - and measuring a weight vector through one measures the vector plus the handicap.
        /// The first tier-2 measurement made exactly this mistake and it cost a whole sweep: at band
        /// 24 the phase-A vector scored 18.8% against its own twin, so every candidate vector would
        /// have been ranked through a 31-point hole. The band is sized separately, afterwards, by
        /// <see cref="TheNearEqualBandSweep"/>.
        /// </remarks>
        private static double RateOf(BotEvaluationWeights weights) =>
            BotTierTwoBench.Measure(
                BotTier.TierTwo, BotTier.TierOne, BotTierTwoBench.TuningSeeds,
                weights, BotPolicy.ExactTiesOnly, BotPolicy.LookaheadCandidateCap, false).WinRate;

        /// <summary>
        /// The values a term is tried at: its incumbent scaled by <see cref="Multipliers"/>, deduped,
        /// with the incumbent itself dropped (it is already measured) and zero dropped for
        /// <see cref="BotEvaluationWeights.TeamLife"/>.
        /// </summary>
        /// <remarks>
        /// Life counters are the win condition, so a sweep that zeroed them would be searching a
        /// space containing "do not try to win". <see cref="BotEvaluation"/>'s remarks call the
        /// ordering of magnitudes the design; this is where that survives contact with a tuner.
        /// </remarks>
        private static IEnumerable<int> CandidateValues(BotEvaluationWeights incumbent, int term)
        {
            var current = incumbent[term];
            var seen = new HashSet<int> { current };
            foreach (var multiplier in Multipliers)
            {
                var value = (int)Math.Round(current * multiplier, MidpointRounding.AwayFromZero);
                if (value == 0 && term == 0)
                {
                    continue;
                }

                if (seen.Add(value))
                {
                    yield return value;
                }
            }
        }

        private static string Percent(double rate) =>
            (rate * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
    }
}
