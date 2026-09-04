using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestones 7.9 and 8.27's T3 gate: the official six-hero roster playing complete matches from
    /// setup to rulebook p.8's and p.13's three endings under fixed reported seeds.
    ///
    /// Nothing here scripts a match. <see cref="DeterministicMatchLoop"/> answers whichever decision the
    /// authoritative projection is actually waiting on, choosing among that decision's own projected
    /// options, so every case below is a statement about what the rules do when they are simply played
    /// rather than about a path a fixture walked on purpose. That distinction is the whole reason this
    /// gate exists: every earlier fixture in this project reaches its subject by naming the exact
    /// commands that get there, and a loop that only ever runs the sequences its tests name has never
    /// been asked whether it works.
    ///
    /// It found one defect on its first run, which is recorded here because the case that closes it is
    /// below. The Defense preview projected three of the four typed contribution sources and threw
    /// <c>InvalidOperationException</c> on the fourth, so attacking a hero who controlled a placed
    /// Hazard - whose property has granted its controller Defense +1 since milestone 6.8 - made the
    /// match unplayable. No earlier fixture had ever attacked such a hero. See
    /// <see cref="ConfiguredCardMatrixTests.ADefenderStandingOnItsOwnHazard_ReadsTheContinuousSourceInItsDefensePreview"/>.
    ///
    /// The seeds are constants because a seed that is not reported is not evidence. They were chosen by
    /// measurement at this revision - scanned for runs that reach an ending inside a bounded budget -
    /// and what the cases assert about them is deliberately a property rather than a transcript: that
    /// the run ends, that it never stalls, that no offered command is refused, and which of the three
    /// endings it reaches. A later milestone that changes what these matches do will still pass here as
    /// long as the loop still works, and will fail here the moment it does not.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class CompleteCoreMatchLoopTests
    {
        // -----------------------------------------------------------------------------------------
        // Reported seeds and budgets, measured at this revision
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.27: the lowest scanned seed that ends the engaged official-roster scenario by
        /// p.13's last Life counter. Milestone 8.35g re-derived it from 3 to 8, after 22 rounds and
        /// 1,836 commands: the turn release is drawn from a one-command set and `Next(1)` still
        /// advances the stream, so every seeded walk became a different match rather than the old
        /// one with a command inserted per turn, and seed 3 now ends on the final Wave instead.
        /// </summary>
        private const int LifeCounterMatchSeed = 8;

        /// <summary>
        /// Milestone 8.27: the lowest scanned seed that takes the unmodified official six-player setup
        /// to p.8's last Wave counter. Milestone 8.35g re-derived it from 21 to 14, after 10 rounds
        /// and 896 commands - close to the shape seed 21 used to have. Seed 21 itself still ends the
        /// same way but now takes 43 rounds and 3,613 commands, which is the clearest measure of how
        /// far the release moved every trajectory.
        /// </summary>
        private const int FinalWaveMatchSeed = 14;

        /// <summary>
        /// Milestone 8.27: the lowest scanned seed that takes the official-roster uncontested-push
        /// scenario to p.8's Throne capture, after 2 rounds and - since milestone 8.35g's release -
        /// 181 commands rather than 116. This is the one recorded seed the release did not move:
        /// the uncontested push ends before the trajectory has room to diverge.
        /// </summary>
        private const int ThroneCaptureMatchSeed = 1;

        /// <summary>
        /// A seed on which the engaged walk takes a Clear that milestone 8.8b still offers - one with
        /// a token standing beside the actor. It is used for reachability; see
        /// <see cref="PlayTheClearReachabilityRun"/>.
        /// </summary>
        /// <remarks>
        /// Milestone 8.35g re-derived it, 58 -> 20, and the search is the interesting part rather
        /// than the number. The release moves every trajectory, and a Clear is rare on this fixture
        /// for a reason 8.8b makes correct: the offer only stands where a token is actually beside
        /// the actor, so a walk that places none is right to take none. Scanning seeds 1-120 offline
        /// found exactly TWO that take one - 20 and 51 - and seed 51 exhausts the budget without an
        /// ending, so 20 is the only one that reaches a Clear by playing a match through to its end:
        /// 2,337 commands over 73 rounds. Nothing about the p.13 page-coverage assertion was
        /// weakened to fit; a narrow search is what the property costs.
        /// </remarks>
        private const int ClearReachabilitySeed = 20;

        /// <summary>
        /// Generous enough that the three complete matches finish well inside it, and small enough that
        /// a run which stops having reached no ending is reported as a failure rather than hanging.
        /// </summary>
        private const int CompleteMatchBudget = 2400;

        /// <summary>
        /// The official six-player soak seeds. At this revision each remains live through the bounded
        /// 300-command window and crosses multiple round boundaries, so the fixture exercises ordinary
        /// 3v3 play without making its value depend on which ending a longer random walk eventually finds.
        /// </summary>
        private static readonly int[] SoakSeeds = { 101, 102, 103, 104, 105, 106, 107, 108 };

        // Milestone 8.35g raised this from 300. The release is one more command per turn, so 300
        // commands now buy four rounds a seed where they bought five; 450 buys six, which keeps the
        // soak what its own floor says it is - many rounds of ordinary play - rather than lowering
        // the floor to fit a shorter walk. Measured across all eight seeds: 32 rounds at 300,
        // 40 at 400, 48 at 450.
        private const int SoakBudget = 450;

        // -----------------------------------------------------------------------------------------
        // Complete matches
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AGeneratedMatch_PlaysFromSetupToAnEndingWithoutStallingOrRefusal()
        {
            foreach (var run in PlayTheThreeCompleteMatches())
            {
                Assert.That(run.Stalled, Is.False,
                    $"Seed {run.Seed} reached a live match the projection offered no legal command for.");
                Assert.That(run.ExhaustedItsBudget, Is.False,
                    $"Seed {run.Seed} ran out of budget after {run.Commands.Count} commands and " +
                    $"{run.RoundsReached} rounds without reaching an ending.");
                Assert.That(run.Outcome.HasEnded, Is.True,
                    $"Seed {run.Seed} stopped without an outcome.");
                Assert.That(run.Outcome.WinningTeamId.IsDefined, Is.True);
                Assert.That(run.Outcome.LosingTeamId.IsDefined, Is.True);
                Assert.That(run.Outcome.WinningTeamId.Equals(run.Outcome.LosingTeamId), Is.False);
            }
        }

        [Test]
        public void ThreeGeneratedMatches_EndThroughAllThreeOfTheRulebooksVictoryPaths()
        {
            var causes = PlayTheThreeCompleteMatches().Select(run => run.Outcome.Cause).ToArray();

            Assert.That(causes.Distinct().OrderBy(cause => (int)cause), Is.EqualTo(new[]
            {
                MatchVictoryCause.LifeCountersExhausted,
                MatchVictoryCause.FinalWaveFlipped,
                MatchVictoryCause.ThroneCaptured
            }.OrderBy(cause => (int)cause)),
                "p.13's last Life counter and p.8's two push endings are each reached by playing, not " +
                "by a fixture staging the state that ends the match.");
        }

        [Test]
        public void TheCompleteLoop_ReachesEveryPartOfThePage7TurnAndThePage8Round()
        {
            var runs = PlayTheThreeCompleteMatches()
                .Concat(new[] { PlayTheClearReachabilityRun() })
                .ToArray();
            var commandKinds = runs.SelectMany(run => run.ObservedCommandKinds).Distinct().ToArray();
            var eventKinds = runs.SelectMany(run => run.ObservedEventKinds).Distinct().ToArray();

            // p.7's turn: a card is committed and revealed, an action is taken or the turn is Held, a
            // Movement is walked and stopped, an Attack picks a target and is answered or declined, and
            // both general replacements are taken.
            AssertReached(commandKinds, DirectorCommandKind.CommitCard);
            AssertReached(commandKinds, DirectorCommandKind.PerformHold);
            AssertReached(commandKinds, DirectorCommandKind.StartAction);
            AssertReached(commandKinds, DirectorCommandKind.ChooseMovementDestination);
            AssertReached(commandKinds, DirectorCommandKind.StopMovement);
            AssertReached(commandKinds, DirectorCommandKind.ChooseAttackTarget);
            AssertReached(commandKinds, DirectorCommandKind.ChooseDefenseCard);
            AssertReached(commandKinds, DirectorCommandKind.PerformFastTravel);
            AssertReached(commandKinds, DirectorCommandKind.PerformClear);
            AssertReached(commandKinds, DirectorCommandKind.AnswerEffectChoice);

            // p.13's Defeat and the respawn it leads to.
            AssertReached(commandKinds, DirectorCommandKind.RespawnHero);

            // p.8's round: the Minion Battle asks and is answered, the ordered steps run, cards are
            // retrieved, Active effects reach their cancellation checkpoint, leveling happens and the
            // heroes that did not level collect their Pity Coin, and the match ends.
            AssertReached(commandKinds, DirectorCommandKind.SelectMinionBattleRemoval);
            AssertReached(commandKinds, DirectorCommandKind.ConfirmMinionBattleRemovals);
            AssertReached(commandKinds, DirectorCommandKind.AdvanceEndOfRound);
            AssertReached(commandKinds, DirectorCommandKind.AnswerSystemTransitionStep);
            AssertReached(eventKinds, RoundResolutionEventKind.EndOfRoundStageResolved);
            AssertReached(eventKinds, RoundResolutionEventKind.CardRetrieved);
            AssertReached(eventKinds, RoundResolutionEventKind.ActiveEffectCancellationCheckpoint);
            AssertReached(eventKinds, RoundResolutionEventKind.HeroLevelUpProgressed);
            AssertReached(eventKinds, RoundResolutionEventKind.PityCoinCollected);
            AssertReached(eventKinds, RoundResolutionEventKind.RoundCompleted);
            AssertReached(eventKinds, RoundResolutionEventKind.NextRoundStarted);
            AssertReached(eventKinds, RoundResolutionEventKind.LanePushRequested);
            AssertReached(eventKinds, RoundResolutionEventKind.LanePushStageResolved);
            AssertReached(eventKinds, RoundResolutionEventKind.SystemTransitionProgressed);
            AssertReached(eventKinds, RoundResolutionEventKind.GameEnded);
        }

        /// <summary>
        /// `RISK-SCALE-001`'s previously unmeasured half. Rulebook p.15 says players level
        /// simultaneously; the durable continuation deliberately presents their independent choices
        /// one at a time. This records the actual wait shape and the cost of each authoritative step on
        /// the same six-hero complete match, rather than estimating either from the staged 1v1 graph.
        /// </summary>
        [Test]
        public void TheOfficialMatch_RecordsTheSerializedSixHeroLevelingWaitAndItsRuntimeBound()
        {
            const double maximumLevelingStepMilliseconds = 25.0;
            var record = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateOfficialSixPlayerSession(),
                FinalWaveMatchSeed, CompleteMatchBudget);
            var session = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
            var promptedHeroes = new HashSet<HeroId>();
            var levelingCommands = 0;
            var maximumMilliseconds = 0.0;

            Assert.That(session.Projection.Participants.Heroes, Has.Count.EqualTo(6));
            Assert.That(session.Projection.Participants.Heroes
                .GroupBy(hero => session.Projection.Participants.GetTeamForHero(hero.Id).Side)
                .Select(group => group.Count()).OrderBy(count => count), Is.EqualTo(new[] { 3, 3 }),
                "The measurement must be the two official teams of three, not six objects on a staged board.");

            foreach (var command in record.Commands)
            {
                var projection = session.Projection;
                var isLeveling = projection.Phase == RoundResolutionPhase.EndOfRound &&
                    projection.CurrentEndOfRoundStage == EndOfRoundStage.LevelUpAndUpgrade;
                var prompt = projection.PendingSystemTransitionDescription;
                if (isLeveling && prompt != null &&
                    prompt.Kind == SystemTransitionPromptKind.LevelUpUpgrade)
                {
                    promptedHeroes.Add(prompt.HeroId);
                }

                if (!isLeveling)
                {
                    var ordinary = session.Execute(command);
                    Assert.That(ordinary.Accepted, Is.True, ordinary.Detail);
                    continue;
                }

                var timer = Stopwatch.StartNew();
                var measured = session.Execute(command);
                timer.Stop();
                Assert.That(measured.Accepted, Is.True, measured.Detail);
                levelingCommands++;
                maximumMilliseconds = Math.Max(maximumMilliseconds, timer.Elapsed.TotalMilliseconds);
            }

            TestContext.WriteLine(
                "SIX_HERO_LEVELING " +
                $"seed={record.Seed} rounds={record.RoundsReached} " +
                $"measuredCommands={levelingCommands} promptedHeroes={promptedHeroes.Count} " +
                $"maximumStepMs={maximumMilliseconds:F4} thresholdMs={maximumLevelingStepMilliseconds:F1}");
            Assert.That(levelingCommands, Is.GreaterThan(0),
                "The official match must actually execute the six-hero leveling stage.");
            Assert.That(promptedHeroes, Has.Count.GreaterThan(1),
                "The run must observe the serialized one-hero-at-a-time wait across more than one seat.");
            Assert.That(maximumMilliseconds, Is.LessThanOrEqualTo(maximumLevelingStepMilliseconds),
                "A six-hero leveling step crossed the standing 25 ms investigation threshold.");
        }

        // -----------------------------------------------------------------------------------------
        // Determinism
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheSameSeed_ProducesTheSameCommandsAndTheSameAuthoritativeState()
        {
            var first = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateOfficialSixPlayerSession(),
                FinalWaveMatchSeed, CompleteMatchBudget);
            var second = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateOfficialSixPlayerSession(),
                FinalWaveMatchSeed, CompleteMatchBudget);

            Assert.That(second.Commands, Is.EqualTo(first.Commands),
                "One seed names one command sequence, or a reported seed reports nothing.");
            Assert.That(second.FinalSnapshot, Is.EqualTo(first.FinalSnapshot));
            Assert.That(second.FinalVersion, Is.EqualTo(first.FinalVersion));
            Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
        }

        [Test]
        public void TwoSeeds_ProduceTwoDifferentMatches()
        {
            var first = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateOfficialSixPlayerSession(),
                FinalWaveMatchSeed, CompleteMatchBudget);
            var second = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateOfficialSixPlayerSession(), 32, CompleteMatchBudget);

            // The vacuity guard on the case above it. A driver that ignored its seed would satisfy
            // "the same seed produces the same match" perfectly and prove nothing at all.
            Assert.That(second.Commands.SequenceEqual(first.Commands), Is.False);
            Assert.That(second.FinalSnapshot, Is.Not.EqualTo(first.FinalSnapshot));
        }

        // -----------------------------------------------------------------------------------------
        // The replay oracle: live, replayed, restored-and-continued, and Undo-truncated
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AReplayedCompleteMatch_ReproducesTheLiveCommandEventAndSnapshotEvidence()
        {
            var live = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
            var record = DeterministicMatchLoop.Play(live, FinalWaveMatchSeed, CompleteMatchBudget);
            Assert.That(record.Outcome.HasEnded, Is.True);

            var replayed = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
            DeterministicMatchLoop.Replay(replayed, record.Commands);

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot));
            Assert.That(replayed.Projection.Outcome, Is.EqualTo(record.Outcome));
            Assert.That(replayed.Projection.RoundIndex, Is.EqualTo(record.RoundsReached));
            Assert.That(replayed.Projection.AcceptedCommands, Is.EqualTo(record.Commands));
            Assert.That(
                replayed.Projection.DiagnosticEvents.Select(entry => entry.DisplayText),
                Is.EqualTo(live.Projection.DiagnosticEvents.Select(entry => entry.DisplayText)),
                "The whole-match rule-event log is a function of the command prefix.");
            Assert.That(
                replayed.Projection.CompletedRounds.Select(round => round.RoundIndex),
                Is.EqualTo(live.Projection.CompletedRounds.Select(round => round.RoundIndex)));
        }

        [Test]
        public void ACompleteMatchRestoredHalfwayThrough_ContinuesIntoTheSameEnding()
        {
            var live = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
            var record = DeterministicMatchLoop.Play(live, FinalWaveMatchSeed, CompleteMatchBudget);
            Assert.That(record.Outcome.HasEnded, Is.True);

            // Half the match, replayed into a session of its own so the snapshot is taken from a state
            // reached the same way the live one reached it.
            var half = record.Commands.Count / 2;
            var prefix = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
            DeterministicMatchLoop.Replay(prefix, record.Commands.Take(half).ToArray());
            var midpoint = prefix.CaptureSnapshot();

            var restored = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
            var restoration = restored.RestoreSnapshot(midpoint);
            Assert.That(restoration.Accepted, Is.True, restoration.Detail);
            Assert.That(restored.Projection.IsHistoryAvailable, Is.False,
                "A restored session records no history, which is what makes continuing it a real test " +
                "rather than a second replay.");

            DeterministicMatchLoop.Replay(restored, record.Commands.Skip(half).ToArray());

            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot));
            Assert.That(restored.Projection.Outcome, Is.EqualTo(record.Outcome));
        }

        [Test]
        public void AnUndoAtTheEndOfACompleteMatch_ReturnsALiveMatchThatReplaysBackIntoTheSameEnding()
        {
            var session = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
            var record = DeterministicMatchLoop.Play(session, FinalWaveMatchSeed, CompleteMatchBudget);
            Assert.That(record.Outcome.HasEnded, Is.True);

            var undone = session.Undo();
            Assert.That(undone.Accepted, Is.True, undone.Detail);
            Assert.That(session.Projection.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded),
                "Truncating the command that ended the match puts the match back inside itself.");
            Assert.That(session.Projection.Outcome.HasEnded, Is.False);
            Assert.That(session.Projection.AcceptedCommands,
                Is.EqualTo(record.Commands.Take(record.Commands.Count - 1)));

            var again = session.Execute(record.Commands[record.Commands.Count - 1]);
            Assert.That(again.Accepted, Is.True, again.Detail);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot),
                "A retained-prefix Undo followed by the same command is the same state, byte for byte.");
            Assert.That(session.Projection.Outcome, Is.EqualTo(record.Outcome));
        }

        [Test]
        public void ACompleteMatchRestarted_ReplaysFromSetupIntoTheSameEnding()
        {
            var session = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
            var record = DeterministicMatchLoop.Play(session, FinalWaveMatchSeed, CompleteMatchBudget);

            var restart = session.Restart();
            Assert.That(restart.Accepted, Is.True, restart.Detail);
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(1));
            Assert.That(session.Projection.AcceptedCommands, Is.Empty);
            Assert.That(session.Projection.Outcome.HasEnded, Is.False);

            DeterministicMatchLoop.Replay(session, record.Commands);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot));
        }

        // -----------------------------------------------------------------------------------------
        // Bounded multi-round soak
        // -----------------------------------------------------------------------------------------

        [Test]
        public void ABoundedSeededSoak_PlaysManyRoundsOfTheConfiguredMatchWithoutStallingOrRefusal()
        {
            var rounds = new List<int>();
            foreach (var seed in SoakSeeds)
            {
                var record = DeterministicMatchLoop.Play(
                    ConfiguredMatchFixtures.CreateOfficialSixPlayerSession(), seed, SoakBudget);

                Assert.That(record.Stalled, Is.False,
                    $"Soak seed {seed} reached a live match the projection offered no legal command for, " +
                    $"after {record.Commands.Count} commands in round {record.RoundsReached}.");
                Assert.That(record.Commands, Has.Count.EqualTo(SoakBudget),
                    $"Soak seed {seed} stopped early without an ending.");
                Assert.That(record.RoundsReached, Is.GreaterThan(1),
                    $"Soak seed {seed} never crossed a round boundary.");
                rounds.Add(record.RoundsReached);
            }

            Assert.That(rounds.Sum(), Is.GreaterThan(SoakSeeds.Length * 5),
                "The soak is meant to be many rounds of ordinary play rather than one long turn.");
        }

        [Test]
        public void EverySoakRun_RoundTripsItsFinalStateThroughTheWire()
        {
            foreach (var seed in SoakSeeds)
            {
                var session = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
                var record = DeterministicMatchLoop.Play(session, seed, SoakBudget);

                var restored = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
                var restoration = restored.RestoreSnapshot(record.FinalSnapshot);
                Assert.That(restoration.Accepted, Is.True, $"Soak seed {seed}: {restoration.Detail}");
                Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot),
                    $"Soak seed {seed} did not restore to the state it captured.");

                // The other wire form as well. `CaptureSnapshot` bundles the authoritative state with
                // the rule-event log, and both are payloads a caller can be handed, so a soak state
                // that round-trips as one and not as the other is a state this suite must not report
                // as durable.
                var bundled = ConfiguredMatchFixtures.CreateOfficialSixPlayerSession();
                var bundledRestoration = bundled.RestoreSnapshot(session.CaptureSnapshot());
                Assert.That(bundledRestoration.Accepted, Is.True,
                    $"Soak seed {seed}: {bundledRestoration.Detail}");
                Assert.That(bundled.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot),
                    $"Soak seed {seed} did not restore from its bundled payload to the same state.");
                Assert.That(
                    bundled.Projection.DiagnosticEvents.Select(entry => entry.DisplayText),
                    Is.EqualTo(session.Projection.DiagnosticEvents.Select(entry => entry.DisplayText)),
                    $"Soak seed {seed} lost its rule-event log across the bundled round trip.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Scenarios
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// One bounded run whose job is reachability rather than an ending: the seed on which the walk
        /// puts a token beside a hero who then commits a card with a replaceable Attack, which since
        /// milestone 8.8b is the only board position p.13's Clear is offered in at all.
        ///
        /// <para>It exists because 8.8b showed what the gate's `PerformClear` coverage had been: before
        /// it, every Clear the walk ever took was the empty one offered beside nothing, so the gate was
        /// reaching the command through the one case the rules now refuse. Rather than drop the claim,
        /// the corpus gains the run that makes it true - and the claim is stronger than it was, because
        /// the Clear observed here is one offered over a real candidate.</para>
        ///
        /// <para>It is not part of <see cref="PlayTheThreeCompleteMatches"/>, because those three assert
        /// that they end and this one deliberately does not; it is used only by the reachability case.</para>
        /// </summary>
        private static MatchLoopRecord PlayTheClearReachabilityRun() =>
            DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedSession(2), ClearReachabilitySeed, CompleteMatchBudget);

        private IReadOnlyList<MatchLoopRecord> PlayTheThreeCompleteMatches() => new[]
        {
            DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedOfficialSixPlayerSession(2),
                LifeCounterMatchSeed, CompleteMatchBudget),
            DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateOfficialSixPlayerSession(),
                FinalWaveMatchSeed, CompleteMatchBudget),
            DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession(),
                ThroneCaptureMatchSeed, CompleteMatchBudget)
        };

        private static void AssertReached<T>(IEnumerable<T> observed, T expected) =>
            Assert.That(observed.Any(value => Equals(value, expected)), Is.True,
                $"No complete match reached {typeof(T).Name}.{expected}.");
    }
}
