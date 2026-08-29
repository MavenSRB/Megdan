using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Snapshots;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestones 7.9 and 8.27's complete-match runtime capture, and the mechanisms each was scheduled
    /// to measure rather than reason about.
    /// </summary>
    /// <remarks>
    /// `F-ER2-08` records that <c>Undo</c> and <c>Restart</c> reconstruct by full-prefix replay, so
    /// their cost grows with match length, and `RISK-PERF-001` records that projection construction
    /// copies growing command and current-round histories. Neither has ever been measured past the
    /// two-round director flow the Phase 4 baseline captures, because until this milestone nothing in
    /// the project reached a complete match. The roadmap's standing instruction is explicit that this
    /// measures and records; a corrective milestone is proposed only if a standing threshold is
    /// crossed, and never by inspection.
    ///
    /// It is a Core/Application capture rather than a Play Mode one on purpose. What both mechanisms
    /// grow with is the accepted-command history, which is authoritative state; the Play Mode baseline
    /// in <c>Phase4RuntimeBaselinePlayModeTests</c> measures the same accepted-command refresh through
    /// the presenter and the scene, and keeps its own thresholds. Measuring here as well means the
    /// growth curve is sampled at history lengths a scene flow cannot reach, on both tiers, with the
    /// scene's own cost excluded so what is left is the mechanism under investigation.
    ///
    /// Wall-clock assertions are deliberately only against the standing investigation thresholds from
    /// `Testing/FUTURE_GATES.md` - 25 ms per accepted-command refresh, 2 MiB of managed allocation per
    /// sample, 150 ms for an Undo. Those are noise-tolerant by design and sit far above the measured
    /// figures, so this fixture reports a real number on every run and fails only on a real regression
    /// rather than on a busy machine. Milestone 7.9b closed both crossings this fixture was written to
    /// measure, and the growth and Undo lines now guard the two mechanisms it removed.
    /// </remarks>
    public sealed class CompleteMatchRuntimeCaptureTests
    {
        /// <summary>Standing, unchanged, and met with three orders of magnitude to spare.</summary>
        private const double MaximumAcceptedCommandRefreshMilliseconds = 25.0;

        /// <summary>
        /// `F-ER2-08`'s Undo bound, tightened at milestone 7.9b from the 600 ms line milestone 7.9
        /// widened it to.
        /// </summary>
        /// <remarks>
        /// The history of this line is three numbers. 250 ms was set at milestone 4.4d from a real
        /// figure - 28.203 ms for an Undo over the two-round director flow. Milestone 7.9 measured the
        /// complete match `F-ER2-08` predicted would outgrow it - a full-prefix replay of 502 commands
        /// at 429 ms on Unity's Mono, 171 ms on .NET - and re-baselined to 600 ms against the slower
        /// runtime, with the fix separately approved rather than taken inside the gate milestone.
        ///
        /// Milestone 7.9b is that fix: the session retains the aggregate's pre-image every
        /// <see cref="MatchSession.UndoCheckpointInterval"/> accepted commands, and an Undo replays
        /// from the latest retained point instead of from the start, so the replayed segment is
        /// bounded by the interval however long the match is. Measured at complete-match length:
        /// 30.9 ms on Unity's Mono and 56.4 ms on .NET - complete-match Undo back at what the
        /// two-round Undo cost. 150 ms is roughly 2.7x the slower measured tier, noise-tolerant on a
        /// busy machine, and well under what any full-prefix replay of this match measures on either
        /// runtime, so the line fails if the reconstruction ever replays the whole prefix again.
        ///
        /// This bounds the Undo control alone. Ordinary play is unaffected - an accepted command
        /// refresh is 0.36 ms on the same runtime - and `RISK-SCALE-001` owns what six heroes do to it.
        /// </remarks>
        private const double MaximumUndoReplayMilliseconds = 150.0;

        /// <summary>
        /// Milestones 7.9 and 8.27's measured scale-sensitive baseline.
        /// </summary>
        /// <remarks>
        /// `Testing/FUTURE_GATES.md` carried "Accepted-command refresh managed allocation | Unmeasured
        /// at complete-match history length | 64 KiB/sample". The 64 KiB was the per-*frame* steady-state
        /// figure from the row above it, reused for a per-*command* metric that nothing had ever sampled;
        /// the first measurement puts an accepted command between 101 KB and 736 KB at every history
        /// length including an empty one, so the declared number was never a description of this build.
        ///
        /// This is a rationale-preserving re-baseline, which is one of the three dispositions the
        /// roadmap allows a measured crossing - not a widened threshold. It is set an order of magnitude
        /// above nothing: 1 MiB is the worst measured sample plus roughly 40% headroom, which leaves the
        /// figure able to catch a real regression while stating a number the build actually meets. The
        /// user was shown the measurement and approved this disposition on 2026-08-20.
        ///
        /// Milestone 8.27 then moved this exact capture from the staged two-seat scenario to the
        /// official six-player match. The worst accepted command measured 1,592,960 B, so the standing
        /// line is re-derived at 2 MiB: about 32% above that observed maximum, preserving the same
        /// rationale and sensitivity at the scale the gate now owns. No allocation mechanism changed.
        /// </remarks>
        private const long MaximumAcceptedCommandAllocationBytes = 2 * 1024 * 1024;

        /// <summary>
        /// How far one command kind's median allocation may rise between the start of a complete match
        /// and its end.
        /// </summary>
        /// <remarks>
        /// This is the growth term `RISK-PERF-001` predicted, measured at milestone 7.9 - 281,424 B
        /// over history lengths 0-13 against 387,008 B over 487-496, about +210 bytes per command of
        /// retained history - and removed at milestone 7.9b, which took every mechanism that copied
        /// or re-encoded a growing history off the accepted-command path: the projection now shares
        /// the append-only logs' snapshots instead of copying them, and the pre-image capture writes
        /// the current state only, sharing each completed round's serialized fragment from the round
        /// it froze. What remains measures +21,068 B between the same two windows, and it is not a
        /// history-length term: matched samples one round apart differ by state size - items held,
        /// levels bought, the round in flight - while the median also straddles the two `CommitCard`
        /// shapes, whose mix is what most of the 21 KB is. 64 KiB is roughly three times that
        /// residual, and a third of the way to the removed term, so the line fails on the growth
        /// mechanism returning rather than on the match being late-game-shaped.
        /// </remarks>
        private const long MaximumAllocationGrowthAcrossACompleteMatch = 64 * 1024;

        /// <summary>
        /// How much more one command kind's median authoritative serialization may write at
        /// complete-match history length than at an empty one.
        /// </summary>
        /// <remarks>
        /// Milestone 7.9d, finding F-CORE-02. The allocation line above is the sharp instrument and it
        /// reads on the cross-platform harness alone, because `GC.GetAllocatedBytesForCurrentThread`
        /// returns a constant zero on Unity's Mono. That left the two Windows tiers guarding
        /// `RISK-PERF-001`'s mechanism with an absolute 25 ms ceiling, and the recorded pre-7.9b runs
        /// show what that was worth: with full-history copying still in place the late median measured
        /// 2.64-2.72 ms, so the exact regression this case exists to catch was present and this fixture
        /// passed on both Unity tiers.
        ///
        /// A median-*time* ratio was the obvious repair and was measured before being adopted, on two
        /// deliberately reintroduced mechanisms in a throwaway tree. It does not work, and the figures
        /// are worth keeping so nobody proposes it again: the time signature is unchanged across the
        /// fix - 1.33-1.47 before 7.9b, 1.33-1.46 after - because what 7.9b removed was almost entirely
        /// allocation rather than elapsed time, and on the harness tier JIT warm-up makes the early
        /// median the *slower* one, so the ratio there reads below 1 whatever the mechanism does.
        ///
        /// What does read the same on every tier is the serializer's own counter. `CapturedByteCount`
        /// is incremented by the writer rather than by the runtime, and re-encoding a growing
        /// completed-round history is precisely a write that grows with the match.
        ///
        /// Both sides of this line are measured rather than declared, which is what the 7.9 re-baseline
        /// of the sibling allocation row exists to warn about. At the closing revision one command
        /// kind's median capture moves 6,852 -> 7,552 bytes between history 0-13 and 487-496: +700, and
        /// it is state size rather than history length. Against a deliberately reintroduced re-encode -
        /// a fresh `CompletedRoundsSerializationCache` per capture, so every completed round is written
        /// again every command - the same medians measure 6,852 -> 15,082: +8,230 at twenty rounds, and
        /// unbounded in a longer match. 4 KiB sits about six times above the residual and half of what
        /// the mechanism costs at this length, so it discriminates between them with room on both
        /// sides.
        ///
        /// Milestone 8.27 repeats that experiment at official 3v3 scale. The retained-cache build
        /// moves 19,006 -> 24,277 bytes (+5,271), because six heroes' current cards, levels, items and
        /// in-flight round state are larger. Deliberately replacing the aggregate's retained cache
        /// with a fresh cache per capture moves the same medians 19,006 -> 36,458 (+17,452). Twelve
        /// KiB is more than twice the legitimate residual and still only about seven tenths of the
        /// deliberately reintroduced mechanism, preserving discrimination on both sides.
        ///
        /// Its limits, stated rather than left to be discovered: a copy that never reaches the
        /// serializer - the projection's own `ToArray`, about a fifth of the original term - is
        /// invisible to this counter, and a deliberately reintroduced one was observed passing this
        /// line while failing the allocation line on the harness tier. This closes the larger half of
        /// the gap on the tiers that could measure nothing at all; it does not close all of it.
        /// </remarks>
        private const long MaximumCapturedByteGrowthAcrossACompleteMatch = 12 * 1024;

        /// <summary>
        /// The history lengths each sample is taken at: empty, short, and then powers of two up to the
        /// complete match, which is appended by <see cref="SampleLengths"/> because it is whatever the
        /// seeded run turns out to be.
        /// </summary>
        private static readonly int[] FixedSampleLengths = { 0, 1, 4, 16, 64, 128 };

        /// <summary>
        /// Milestone 8.27, closing F-CORE-10. The capture must measure the same official six-player
        /// match the gate proves, rather than retaining milestone 7.9's internal two-seat scenario
        /// behind a neutral factory name.
        /// </summary>
        [Test]
        public void MeasuresTheOfficialSixHeroCompleteMatch()
        {
            var session = CreateCompleteMatchSession();

            Assert.That(session.Projection.Heroes, Has.Count.EqualTo(6),
                "The runtime capture must start from all six official hero seats.");
            Assert.That(session.Projection.Cards, Has.Count.EqualTo(108),
                "The six official eighteen-card sets must be the capture's content boundary.");
            Assert.That(session.Projection.Setup.Teams.Select(team => team.LifeCount),
                Is.EqualTo(new[] { 5, 5 }),
                "Rulebook p.6 gives a six-player Quick Game five Lives per team.");
            Assert.That(session.Projection.Setup.WaveCount, Is.EqualTo(3),
                "Rulebook p.6 gives a Quick Game three Waves.");

            var record = DeterministicMatchLoop.Play(session, CaptureSeed, CaptureBudget);
            Assert.That(record.Stalled, Is.False,
                "The official six-hero match reached a live decision with no projected command.");
            Assert.That(record.ExhaustedItsBudget, Is.False,
                $"The official six-hero match did not end inside {CaptureBudget} commands.");
            Assert.That(record.Outcome.HasEnded, Is.True,
                "The capture must measure a complete official six-hero match.");
        }

        [Test]
        public void TheAcceptedCommandRefresh_IsSampledAcrossHistoryLengthsUpToACompleteMatch()
        {
            var record = PlayTheCompleteMatch();
            var samples = new List<Sample>();

            foreach (var length in SampleLengths(record))
            {
                samples.Add(MeasureCommandAt(record, length));
            }

            foreach (var sample in samples)
            {
                TestContext.WriteLine(
                    "COMPLETE_MATCH_CAPTURE " +
                    $"historyLength={sample.HistoryLength} " +
                    $"command={sample.Kind} " +
                    $"refreshMs={sample.Milliseconds:F4} " +
                    $"allocatedBytes={sample.AllocatedBytes}");
            }

            var complete = samples[samples.Count - 1];
            var shortest = samples[0];
            TestContext.WriteLine(
                "COMPLETE_MATCH_CAPTURE_GROWTH " +
                $"seed={record.Seed} " +
                $"completeMatchCommands={record.Commands.Count} " +
                $"completeMatchRounds={record.RoundsReached} " +
                $"emptyHistoryMs={shortest.Milliseconds:F4} " +
                $"completeHistoryMs={complete.Milliseconds:F4} " +
                $"emptyHistoryBytes={shortest.AllocatedBytes} " +
                $"completeHistoryBytes={complete.AllocatedBytes}");

            Assert.That(samples, Has.Count.GreaterThanOrEqualTo(FixedSampleLengths.Length),
                "Every fixed history length below the complete match must be sampled.");
            Assert.That(complete.HistoryLength, Is.EqualTo(record.Commands.Count - 1),
                "The last sample is the last command of the complete match.");
            Assert.That(complete.Milliseconds,
                Is.LessThanOrEqualTo(MaximumAcceptedCommandRefreshMilliseconds),
                "`RISK-PERF-001`: an accepted command at complete-match history length crossed the " +
                "standing 25 ms investigation threshold.");
        }

        [Test]
        public void TheAcceptedCommandAllocation_StaysWithinItsStandingInvestigationThreshold()
        {
            // The row this compares against carried no baseline before this milestone: `FUTURE_GATES.md`
            // reads "Accepted-command refresh managed allocation | Unmeasured at complete-match history
            // length | 64 KiB/sample". The threshold was therefore declared rather than measured, and
            // this is the first run that puts a number beside it.
            var record = PlayTheCompleteMatch();
            var samples = SampleLengths(record)
                .Concat(SpreadSampleIndices(record, DirectorCommandKind.CommitCard, GrowthSampleCount))
                .Distinct()
                .OrderBy(length => length)
                .Select(length => MeasureCommandAt(record, length))
                .ToArray();
            var worst = samples.OrderByDescending(sample => sample.AllocatedBytes).First();

            TestContext.WriteLine(
                "COMPLETE_MATCH_ALLOCATION " +
                $"worstHistoryLength={worst.HistoryLength} worstCommand={worst.Kind} " +
                $"worstAllocatedBytes={worst.AllocatedBytes} " +
                $"thresholdBytes={MaximumAcceptedCommandAllocationBytes} " +
                $"runtimeReportsAllocation={RuntimeReportsManagedAllocation()}");

            if (!RuntimeReportsManagedAllocation())
            {
                Assert.That(samples.All(sample => sample.AllocatedBytes == 0), Is.True,
                    "A runtime that reports no allocation must report none, not some: a partial counter " +
                    "would make every figure here half a measurement.");
                return;
            }

            Assert.That(worst.AllocatedBytes,
                Is.LessThanOrEqualTo(MaximumAcceptedCommandAllocationBytes),
                $"A {worst.Kind} at history length {worst.HistoryLength} allocated " +
                $"{worst.AllocatedBytes} bytes, against milestone 7.9's measured re-baseline. The " +
                "figure this replaced was never measured; this one was, so a crossing here is a real " +
                "regression rather than a threshold nobody had checked.");
        }

        [Test]
        public void AnUndoAtCompleteMatchLength_StaysWithinItsStandingReplayThreshold()
        {
            var record = PlayTheCompleteMatch();
            var session = CreateCompleteMatchSession();
            DeterministicMatchLoop.Replay(session, record.Commands);

            // `F-ER2-08`'s fix: an Undo reconstructs from the latest retained point, so even the most
            // expensive Undo this build can be asked for - the last command of a complete match -
            // replays a bounded segment rather than the whole prefix.
            var timer = Stopwatch.StartNew();
            var undone = session.Undo();
            timer.Stop();

            Assert.That(undone.Accepted, Is.True, undone.Detail);
            TestContext.WriteLine(
                "COMPLETE_MATCH_UNDO " +
                $"seed={record.Seed} historyLength={record.Commands.Count} " +
                $"undoReplayMs={timer.Elapsed.TotalMilliseconds:F3}");
            Assert.That(timer.Elapsed.TotalMilliseconds,
                Is.LessThanOrEqualTo(MaximumUndoReplayMilliseconds),
                "`F-ER2-08`: an Undo at complete-match length crossed milestone 7.9b's retained-point " +
                "bound. A full-prefix replay of this match measures far above this line on every " +
                "runtime, so crossing it means the reconstruction has stopped using a retained point.");
        }

        [Test]
        public void TheProjectionRefresh_DoesNotGrowWithTheCommandHistoryItCopies()
        {
            // `RISK-PERF-001`'s actual claim is that projection construction copies growing command and
            // current-round histories, so the question it asks is whether an accepted command costs more
            // when the history behind it is longer. Answering that means holding the command kind fixed:
            // the samples above vary by an order of magnitude between kinds, which says what a Skill
            // resolution costs relative to a commitment and says nothing at all about history length.
            //
            // Holding the kind fixed is still not enough on its own, and the reason is worth writing
            // down: the second commitment of a turn reveals both cards and starts the turn, so half the
            // `CommitCard` samples do several times the work of the other half whatever the history
            // behind them is. Comparing one early sample with one late one therefore measures which of
            // the two shapes each happened to be. This compares the *median* of many early samples with
            // the median of many late ones, which is the same population on both sides.
            var record = PlayTheCompleteMatch();
            var samples = SpreadSampleIndices(record, DirectorCommandKind.CommitCard, GrowthSampleCount)
                .Select(length => MeasureCommandAt(record, length))
                .ToArray();

            foreach (var sample in samples)
            {
                TestContext.WriteLine(
                    "COMPLETE_MATCH_GROWTH_SAMPLE " +
                    $"historyLength={sample.HistoryLength} command={sample.Kind} " +
                    $"refreshMs={sample.Milliseconds:F4} allocatedBytes={sample.AllocatedBytes}");
            }

            Assert.That(samples, Has.Length.GreaterThanOrEqualTo(GrowthSampleCount),
                "The growth curve needs its whole sample population to mean anything.");
            var half = samples.Length / 2;
            var early = samples.Take(half).ToArray();
            var late = samples.Skip(samples.Length - half).ToArray();
            var earlyBytes = Median(early.Select(sample => (double)sample.AllocatedBytes));
            var lateBytes = Median(late.Select(sample => (double)sample.AllocatedBytes));
            var earlyMilliseconds = Median(early.Select(sample => sample.Milliseconds));
            var lateMilliseconds = Median(late.Select(sample => sample.Milliseconds));
            var earlyCaptured = Median(early.Select(sample => (double)sample.CapturedBytes));
            var lateCaptured = Median(late.Select(sample => (double)sample.CapturedBytes));

            TestContext.WriteLine(
                "COMPLETE_MATCH_GROWTH " +
                $"earlyHistory={early[0].HistoryLength}-{early[early.Length - 1].HistoryLength} " +
                $"lateHistory={late[0].HistoryLength}-{late[late.Length - 1].HistoryLength} " +
                $"medianEarlyBytes={earlyBytes:F0} medianLateBytes={lateBytes:F0} " +
                $"medianEarlyMs={earlyMilliseconds:F4} medianLateMs={lateMilliseconds:F4} " +
                $"medianEarlyCapturedBytes={earlyCaptured:F0} medianLateCapturedBytes={lateCaptured:F0}");

            Assert.That(late[0].HistoryLength, Is.GreaterThan(early[early.Length - 1].HistoryLength),
                "The two halves must not overlap, or the comparison is between a set and itself.");
            Assert.That(lateMilliseconds,
                Is.LessThanOrEqualTo(MaximumAcceptedCommandRefreshMilliseconds),
                "The late half must not cross the standing refresh-time threshold. This half of the " +
                "case is measured on every runtime, because time is reported everywhere.");
            Assert.That(earlyCaptured, Is.GreaterThan(0.0),
                "The measurement must observe real serialization work, or the growth line below is " +
                "comparing nothing with nothing.");
            Assert.That(lateCaptured,
                Is.LessThanOrEqualTo(earlyCaptured + MaximumCapturedByteGrowthAcrossACompleteMatch),
                $"One command kind's median authoritative serialization moved from {earlyCaptured:F0} " +
                $"bytes over history lengths {early[0].HistoryLength}-{early[early.Length - 1].HistoryLength} " +
                $"to {lateCaptured:F0} over {late[0].HistoryLength}-{late[late.Length - 1].HistoryLength}. " +
                "Re-encoding the growing completed-round history every command is exactly what this " +
                "catches coming back, and unlike the allocation line above it reads on every runtime, " +
                "which is what the two Windows tiers had no growth guard without.");

            if (!RuntimeReportsManagedAllocation())
            {
                // Recorded rather than skipped. The time comparison above is the whole of what this
                // tier can say about growth, and saying so is what stops a zero being read as a pass.
                TestContext.WriteLine(
                    "COMPLETE_MATCH_GROWTH_UNMEASURED runtime reports no per-thread managed " +
                    "allocation; the allocation half of this case is measured on the cross-platform " +
                    "harness tier and recorded in Testing/FUTURE_GATES.md.");
                return;
            }

            Assert.That(lateBytes,
                Is.LessThanOrEqualTo(earlyBytes + MaximumAllocationGrowthAcrossACompleteMatch),
                $"One command kind's median allocation moved from {earlyBytes:F0} bytes over history " +
                $"lengths {early[0].HistoryLength}-{early[early.Length - 1].HistoryLength} to " +
                $"{lateBytes:F0} over {late[0].HistoryLength}-{late[late.Length - 1].HistoryLength}. " +
                "`RISK-PERF-001`'s growth term is real and measured; milestone 7.9b owns reducing it, " +
                "and this line is what catches it getting worse in the meantime.");
        }

        // -----------------------------------------------------------------------------------------
        // Measurement
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Whether this runtime reports per-thread managed allocation at all.
        /// </summary>
        /// <remarks>
        /// It does not on every runtime this suite runs on. Unity's Mono returns a constant zero from
        /// <see cref="GC.GetAllocatedBytesForCurrentThread"/>, so on the Windows Editor tiers every
        /// allocation figure below would be zero and every allocation assertion would pass by measuring
        /// nothing - which is the exact vacuity this project refuses to let a check have. The .NET
        /// runtime the cross-platform harness executes on reports it, so the figures in
        /// `Testing/FUTURE_GATES.md` are that tier's, recorded as such.
        ///
        /// Detected rather than assumed, by allocating a known array and asking whether the counter
        /// noticed. A runtime that starts reporting will therefore start being asserted against without
        /// anything here having to be told about it.
        /// </remarks>
        private static bool RuntimeReportsManagedAllocation()
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var probe = new byte[64 * 1024];
            probe[probe.Length - 1] = 1;
            return GC.GetAllocatedBytesForCurrentThread() - before >= probe.Length;
        }

        private sealed class Sample
        {
            public Sample(
                int historyLength,
                DirectorCommandKind kind,
                double milliseconds,
                long allocatedBytes,
                long capturedBytes)
            {
                HistoryLength = historyLength;
                Kind = kind;
                Milliseconds = milliseconds;
                AllocatedBytes = allocatedBytes;
                CapturedBytes = capturedBytes;
            }

            public int HistoryLength { get; }
            public DirectorCommandKind Kind { get; }
            public double Milliseconds { get; }
            public long AllocatedBytes { get; }

            /// <summary>
            /// What the authoritative serialization wrote for this one command, counted by the
            /// serializer itself rather than by the runtime. Milestone 7.9d: this is the only growth
            /// instrument that reads the same on every tier, which is what the Windows tiers needed.
            /// </summary>
            public long CapturedBytes { get; }
        }

        /// <summary>
        /// Replays the recorded match to <paramref name="historyLength"/> accepted commands and measures
        /// the one command after it - the accepted-command refresh at exactly that history length.
        /// </summary>
        /// <remarks>
        /// A fresh session per sample, replayed rather than continued, because the alternative is to
        /// measure one long run and attribute each command's cost to the history it happened to have.
        /// That would fold every earlier command's garbage into the sample. Here the measured region is
        /// one <see cref="MatchSession.Execute"/> call and the projection it builds, with the replay
        /// that set up the history outside it.
        /// </remarks>
        private static Sample MeasureCommandAt(MatchLoopRecord record, int historyLength)
        {
            var session = CreateCompleteMatchSession();
            DeterministicMatchLoop.Replay(session, record.Commands.Take(historyLength).ToArray());
            var command = record.Commands[historyLength];

            // The projection is lazy about its diagnostic bundle, so read the live one first: an
            // unforced Lazy would otherwise be charged to whichever sample happened to touch it.
            var _ = session.Projection.Version;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            AuthoritativeSnapshotSerializer.ResetSerializeCallCount();
            var before = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            var result = session.Execute(command);
            timer.Stop();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            var captured = AuthoritativeSnapshotSerializer.CapturedByteCount;

            Assert.That(result.Accepted, Is.True, result.Detail);
            return new Sample(
                historyLength, command.Kind, timer.Elapsed.TotalMilliseconds, allocated, captured);
        }

        /// <summary>
        /// The first <paramref name="count"/> occurrences of one command kind and the last
        /// <paramref name="count"/>, so the two halves the growth case compares sit at opposite ends of
        /// the match and hold the same mix of that kind's shapes.
        /// </summary>
        private static IReadOnlyList<int> SpreadSampleIndices(
            MatchLoopRecord record, DirectorCommandKind kind, int count)
        {
            var indices = Enumerable.Range(0, record.Commands.Count)
                .Where(index => record.Commands[index].Kind == kind)
                .ToArray();
            Assert.That(indices.Length, Is.GreaterThanOrEqualTo(count * 2),
                $"The complete match must contain at least {count * 2} {kind} commands to sample.");
            var half = count / 2;
            return indices.Take(half).Concat(indices.Skip(indices.Length - half)).ToArray();
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(value => value).ToArray();
            return sorted.Length % 2 == 1
                ? sorted[sorted.Length / 2]
                : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2d;
        }

        private static IEnumerable<int> SampleLengths(MatchLoopRecord record)
        {
            foreach (var length in FixedSampleLengths.Where(value => value < record.Commands.Count - 1))
            {
                yield return length;
            }

            // The complete-match history: the state just before the command that ended the match.
            yield return record.Commands.Count - 1;
        }

        /// <summary>
        /// The same complete match the gate suite plays, under the same reported seed, so the figures
        /// recorded here describe a match whose correctness is proved next door rather than a synthetic
        /// command stream built to be long.
        /// </summary>
        private static MatchLoopRecord PlayTheCompleteMatch()
        {
            var record = DeterministicMatchLoop.Play(CreateCompleteMatchSession(), CaptureSeed, CaptureBudget);
            Assert.That(record.Outcome.HasEnded, Is.True,
                "The capture measures a complete match, so the run has to have reached an ending.");
            Assert.That(record.Commands.Count,
                Is.GreaterThan(FixedSampleLengths[FixedSampleLengths.Length - 1]),
                "The complete match must be longer than the longest fixed sample length, or the last " +
                "sample would not be the complete-match one.");
            return record;
        }

        /// <summary>
        /// Rulebook p.5's official six-player Quick Game. Milestone 8.27 moves the capture off the
        /// internal two-seat engaged scenario so the runtime, allocation, serialization-growth, and
        /// Undo figures describe the same roster and scale as the deterministic 3v3 hard gate.
        /// </summary>
        private static MatchSession CreateCompleteMatchSession() =>
            ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();

        /// <summary>Milestone 8.27's reported unmodified official-roster Wave-ending seed.</summary>
        private const int CaptureSeed = 21;
        private const int CaptureBudget = 1200;

        /// <summary>Twelve samples of one command kind: six at the start of the match and six at the end.</summary>
        private const int GrowthSampleCount = 12;
    }
}
