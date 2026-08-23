using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Victory;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.16's gate: the verified Wasp mirror, played rather than staged, from rulebook p.5's
    /// setup to each of the three endings p.8 and p.13 name.
    ///
    /// <para>Milestone 7.9 built this gate's driver and ran it over the mechanics-showcase hands, whose
    /// values and text were invented to exercise machinery. Everything below runs it over
    /// <see cref="WaspCardContent"/>, where every value and word is transcribed from a printed face and
    /// every sentence is composed from a typed capability - so a failure here is a statement about the
    /// hero this project is shipping rather than about a fixture.</para>
    ///
    /// <para>Nothing here scripts a match. <see cref="DeterministicMatchLoop"/> answers whichever
    /// decision the authoritative projection is actually waiting on, choosing among that decision's own
    /// projected options, so a run can only ever do what the rules were already offering. A run that
    /// stalls has found a decision nothing can answer; a run whose command is refused has found an
    /// offer the rules do not honour. Both fail here rather than being steered around, and both are
    /// what this gate found: see the two <c>MatchVictoryPathTests</c> cases the milestone added, which
    /// close the defect these seeds walked into - p.8's Lane Push, pushed in the middle of a turn, could
    /// end a match without the phase recording it, and the payload of such a match could not be
    /// restored.</para>
    ///
    /// <para>The seeds are constants because a seed that is not reported is not evidence. The two
    /// engaged seeds were chosen by measurement at this revision under one stated rule, so a
    /// re-measurement makes the same choice rather than a convenient one: <b>the lowest seed that
    /// reaches this ending inside half the budget, having crossed at least ten round boundaries</b>.
    /// The second clause is what keeps an ending from being claimed by an accident in the first round -
    /// with one Life counter a side, a mirror can lose a hero on its opening Attack, and such a run
    /// proves the ending fires rather than that a match plays. The uncontested-push scenario needs no
    /// such rule, because every seed reaches the Throne in the same two rounds; the lowest is taken.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.</para>
    /// </summary>
    public sealed class WaspMirrorIntegrationGateTests
    {
        // -----------------------------------------------------------------------------------------
        // Reported seeds and budgets, measured at this revision
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Ends by p.13's last Life counter after 19 rounds and 455 commands, on the engaged mirror
        /// carrying one Life counter a side.
        /// </summary>
        private const int LifeCounterMatchSeed = 40;

        /// <summary>
        /// Ends by p.8's last Wave counter after 20 rounds and 511 commands, on the engaged mirror
        /// carrying two Life counters a side - enough that the Wave runs out before the heroes do.
        /// </summary>
        private const int FinalWaveMatchSeed = 16;

        /// <summary>
        /// Ends by p.8's Throne capture after 2 rounds and 50 commands, on the uncontested-push mirror.
        /// This one is short by construction rather than by luck: Blue holds one minion, so every
        /// round's Minion Battle awards Red the push and the official Lane puts the Blue Throne two
        /// Zones away. It is the end-of-round arm of that ending; the mid-turn arm is
        /// <see cref="MidTurnPushMatchSeed"/>, asserted structurally below.
        /// </summary>
        private const int ThroneCaptureMatchSeed = 1;

        /// <summary>
        /// Ends by p.8's Throne capture after 30 rounds and 693 commands, reached by a Lane Push in the
        /// middle of a turn rather than at an end-of-round stage.
        ///
        /// Exactly two seeds in 1-60 end that way, 44 and 59, and both were seeds this gate could not
        /// play to their end at all before the milestone's fix; the cheaper of the two is taken.
        /// </summary>
        private const int MidTurnPushMatchSeed = 59;

        /// <summary>
        /// Generous enough that every complete match above finishes well inside it, and small enough
        /// that a run which reaches no ending is reported as a failure rather than hanging.
        /// </summary>
        private const int CompleteMatchBudget = 1200;

        /// <summary>Life counters for the two engaged scenarios, named so the cases read.</summary>
        private const int OneLifeCounter = 1;

        private const int TwoLifeCounters = 2;

        /// <summary>
        /// The soak seeds, run over the ordinary verified mirror - the composition the scene itself
        /// builds, with both Wasps on their own spawn points and no counter count touched. It has no
        /// natural ending inside this budget, which is what makes it the right soak subject: every run
        /// is bounded by its budget rather than by an outcome, so all of it is ordinary play.
        /// </summary>
        private static readonly int[] SoakSeeds = { 201, 202, 203, 204 };

        private const int SoakBudget = 300;

        // -----------------------------------------------------------------------------------------
        // Complete matches
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AWaspMirrorMatch_PlaysFromSetupToAnEndingWithoutStallingOrRefusal()
        {
            foreach (var run in PlayTheFourCompleteMatches())
            {
                Assert.That(run.Stalled, Is.False,
                    $"Seed {run.Seed} reached a live Wasp mirror the projection offered no legal " +
                    $"command for, after {run.Commands.Count} commands in round {run.RoundsReached}.");
                Assert.That(run.ExhaustedItsBudget, Is.False,
                    $"Seed {run.Seed} played {CompleteMatchBudget} commands without reaching an ending.");
                Assert.That(run.Outcome.HasEnded, Is.True);
                Assert.That(run.Outcome.WinningTeamId.IsDefined, Is.True);
                Assert.That(run.Outcome.LosingTeamId.IsDefined, Is.True);
                Assert.That(run.Outcome.WinningTeamId, Is.Not.EqualTo(run.Outcome.LosingTeamId));
            }

            // Every command the loop accepted was one the projection had offered - the loop asserts
            // that on each command - so what is left to state here is that the runs were matches
            // rather than openings.
            Assert.That(
                PlayTheFourCompleteMatches().Sum(run => run.RoundsReached),
                Is.GreaterThan(40),
                "Between them these four matches play tens of rounds of p.7's turn inside p.8's round.");
        }

        [Test]
        public void TheWaspMirror_EndsThroughAllThreeOfTheRulebooksVictoryPaths()
        {
            var causes = PlayTheFourCompleteMatches()
                .Select(run => run.Outcome.Cause)
                .Distinct()
                .OrderBy(cause => (int)cause)
                .ToArray();

            Assert.That(causes, Is.EquivalentTo(new[]
            {
                MatchVictoryCause.LifeCountersExhausted,
                MatchVictoryCause.FinalWaveFlipped,
                MatchVictoryCause.ThroneCaptured
            }),
                "p.8 names two victory sentences and p.13 names the third, and the mirror reaches all " +
                "three by being played rather than by a fixture arranging each one.");
        }

        [Test]
        public void AMidTurnLanePush_EndsOneOfTheseMatchesWhereItHappened()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters);
            var run = DeterministicMatchLoop.Play(session, MidTurnPushMatchSeed, CompleteMatchBudget);

            Assert.That(run.Outcome.Cause, Is.EqualTo(MatchVictoryCause.ThroneCaptured));

            var ended = session.Projection.RoundEvents
                .Single(value => value.Kind == RoundResolutionEventKind.GameEnded);
            Assert.That(ended.EndOfRoundStage, Is.Null,
                "An end-of-round push names the stage it ended at, so a push ending that names none " +
                "is p.8's mid-turn one - which is what makes this run a different path through the " +
                "same sentence rather than a second copy of the seed above.");
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(session.Projection.TeamLifeCounters.All(entry => entry.Value > 0), Is.True,
                "and it owes nothing to p.13: both teams still hold every Life counter.");
        }

        // -----------------------------------------------------------------------------------------
        // The content these matches were played with
        // -----------------------------------------------------------------------------------------

        [Test]
        public void EveryCardTheseMatchesCommitted_IsOneOfWaspsOwnVerifiedPrintedFaces()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters);
            var run = DeterministicMatchLoop.Play(session, FinalWaveMatchSeed, CompleteMatchBudget);

            var byInstance = session.Projection.Cards.ToDictionary(card => card.Id, card => card);
            var committed = run.Commands
                .Where(command => command.Kind == DirectorCommandKind.CommitCard)
                .Select(command => byInstance[command.CardInstanceId].DefinitionReference.DefinitionId)
                .Distinct()
                .ToArray();

            Assert.That(committed, Is.Not.Empty);
            Assert.That(
                committed.All(id => WaspCardContent.DealtDefinitions.Any(definition =>
                    definition.Id.Equals(id))),
                Is.True,
                "Every card this match played is one of the eighteen faces p.5 deals from Wasp's " +
                "printed deck, so the gate is a statement about verified official content.");
            Assert.That(
                committed.All(id =>
                    WaspCardContent.Definition(id).ContentAuthority == CardContentAuthority.VerifiedOfficial),
                Is.True,
                "and each of them carries that authority on the definition rather than in a comment.");
            Assert.That(
                committed.Any(id => id.Equals(WaspCardContent.CracklingDagger)), Is.False,
                "p.5 returns the Handicap card to the box, so no match holds an instance to commit.");
            Assert.That(
                WaspCardContent.StartingHand.All(id => committed.Any(played => played.Equals(id))),
                Is.True,
                "and all five of p.5's starting cards were played, so the run reached the whole hand " +
                "rather than the first card of it.");
        }

        // -----------------------------------------------------------------------------------------
        // Determinism
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheSameSeed_ProducesTheSameCommandsAndTheSameAuthoritativeState()
        {
            var first = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(OneLifeCounter),
                LifeCounterMatchSeed, CompleteMatchBudget);
            var second = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(OneLifeCounter),
                LifeCounterMatchSeed, CompleteMatchBudget);

            Assert.That(second.Commands, Is.EqualTo(first.Commands),
                "One seed names one command sequence, or a reported seed reports nothing.");
            Assert.That(second.FinalSnapshot, Is.EqualTo(first.FinalSnapshot));
            Assert.That(second.FinalVersion, Is.EqualTo(first.FinalVersion));
            Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
        }

        [Test]
        public void TwoSeeds_ProduceTwoDifferentWaspMirrorMatches()
        {
            // The vacuity guard on the case above it: a driver that ignored its seed would satisfy
            // "the same seed produces the same match" perfectly and prove nothing at all.
            var first = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters),
                FinalWaveMatchSeed, CompleteMatchBudget);
            var second = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters),
                MidTurnPushMatchSeed, CompleteMatchBudget);

            Assert.That(second.Commands.SequenceEqual(first.Commands), Is.False);
            Assert.That(second.FinalSnapshot, Is.Not.EqualTo(first.FinalSnapshot));
            Assert.That(second.Outcome.Cause, Is.Not.EqualTo(first.Outcome.Cause));
        }

        // -----------------------------------------------------------------------------------------
        // Undo, restart, snapshots and the event log, on the mirror rather than on a fixture
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AReplayedWaspMirrorMatch_ReproducesItsCommandEventAndSnapshotEvidence()
        {
            var live = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(OneLifeCounter);
            var record = DeterministicMatchLoop.Play(live, LifeCounterMatchSeed, CompleteMatchBudget);
            Assert.That(record.Outcome.HasEnded, Is.True);

            var replayed = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(OneLifeCounter);
            DeterministicMatchLoop.Replay(replayed, record.Commands);

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot));
            Assert.That(replayed.Projection.Outcome, Is.EqualTo(record.Outcome));
            Assert.That(replayed.Projection.RoundIndex, Is.EqualTo(record.RoundsReached));
            Assert.That(replayed.Projection.AcceptedCommands, Is.EqualTo(record.Commands));
            Assert.That(
                replayed.Projection.DiagnosticEvents.Select(entry => entry.DisplayText),
                Is.EqualTo(live.Projection.DiagnosticEvents.Select(entry => entry.DisplayText)),
                "The whole-match rule-event log is a function of the command prefix, so the internal " +
                "event log a director reads is reproducible rather than incidental.");
            Assert.That(
                live.Projection.DiagnosticEvents.Any(entry =>
                    entry.DisplayText.Contains("Round.GameEnded")),
                Is.True,
                "and it records the ending itself, which is the entry a director looks for.");
        }

        [Test]
        public void AWaspMirrorMatchRestoredHalfwayThrough_ContinuesIntoTheSameEnding()
        {
            var live = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters);
            var record = DeterministicMatchLoop.Play(live, FinalWaveMatchSeed, CompleteMatchBudget);
            Assert.That(record.Outcome.HasEnded, Is.True);

            var half = record.Commands.Count / 2;
            var prefix = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters);
            DeterministicMatchLoop.Replay(prefix, record.Commands.Take(half).ToArray());
            var midpoint = prefix.CaptureSnapshot();

            var restored = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters);
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
        public void AnUndoAtTheEndOfAWaspMirrorMatch_ReturnsALiveMatchThatReplaysBackIntoTheSameEnding()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters);
            var record = DeterministicMatchLoop.Play(session, MidTurnPushMatchSeed, CompleteMatchBudget);
            Assert.That(record.Outcome.HasEnded, Is.True);

            var undone = session.Undo();

            Assert.That(undone.Accepted, Is.True, undone.Detail);
            Assert.That(session.Projection.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded),
                "Truncating the command that ended the match puts the match back inside itself - and " +
                "this is the mid-turn Lane-Push ending, the one shape an Undo had never crossed.");
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
        public void AWaspMirrorMatchRestarted_ReplaysFromSetupIntoTheSameEnding()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedVerifiedPushSession();
            var record = DeterministicMatchLoop.Play(
                session, ThroneCaptureMatchSeed, CompleteMatchBudget);
            Assert.That(record.Outcome.Cause, Is.EqualTo(MatchVictoryCause.ThroneCaptured));

            var restart = session.Restart();

            Assert.That(restart.Accepted, Is.True, restart.Detail);
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(1));
            Assert.That(session.Projection.AcceptedCommands, Is.Empty);
            Assert.That(session.Projection.Outcome.HasEnded, Is.False);

            DeterministicMatchLoop.Replay(session, record.Commands);

            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot));
            Assert.That(session.Projection.Outcome, Is.EqualTo(record.Outcome));
        }

        [Test]
        public void TheTieBreakerOverride_DecidesWhichMirroredWaspActsFirst()
        {
            // `DEV-005`. p.5 flips a coin for the initial Tie Breaker side; the internal override
            // selects it instead, which is the whole reason every fixture in this suite is repeatable.
            // A mirror is what makes the override observable at all: both seats hold the same printed
            // cards at the same Initiative, so which of them acts first is decided by nothing else.
            var red = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var blue = CreateVerifiedMirrorWithTheTieBreakerOn(TeamSide.Blue);

            Assert.That(red.Projection.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));
            Assert.That(blue.Projection.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));

            var redFirst = FirstActorSideOfTheFirstTurn(red);
            var blueFirst = FirstActorSideOfTheFirstTurn(blue);

            Assert.That(redFirst, Is.EqualTo(TeamSide.Red));
            Assert.That(blueFirst, Is.EqualTo(TeamSide.Blue),
                "p.7 breaks the Initiative tie on the Tie Breaker side, and in a mirror every tie is " +
                "one, so the override is the only thing that can have moved the answer.");
        }

        // -----------------------------------------------------------------------------------------
        // Bounded multi-round soak
        // -----------------------------------------------------------------------------------------

        [Test]
        public void ABoundedSeededSoak_PlaysManyRoundsOfTheWaspMirrorWithoutStallingOrRefusal()
        {
            var rounds = new List<int>();
            foreach (var seed in SoakSeeds)
            {
                var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
                var record = DeterministicMatchLoop.Play(session, seed, SoakBudget);

                Assert.That(record.Stalled, Is.False,
                    $"Soak seed {seed} reached a live Wasp mirror the projection offered no legal " +
                    $"command for, after {record.Commands.Count} commands in round {record.RoundsReached}.");
                Assert.That(record.RoundsReached, Is.GreaterThan(1),
                    $"Soak seed {seed} never crossed a round boundary.");

                var restored = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
                var restoration = restored.RestoreSnapshot(session.CaptureSnapshot());
                Assert.That(restoration.Accepted, Is.True, $"Soak seed {seed}: {restoration.Detail}");
                Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot),
                    $"Soak seed {seed} did not restore from its bundled payload to the state it reached.");
                Assert.That(
                    restored.Projection.DiagnosticEvents.Select(entry => entry.DisplayText),
                    Is.EqualTo(session.Projection.DiagnosticEvents.Select(entry => entry.DisplayText)),
                    $"Soak seed {seed} lost its rule-event log across the round trip.");
                rounds.Add(record.RoundsReached);
            }

            Assert.That(rounds.Sum(), Is.GreaterThan(SoakSeeds.Length * 5),
                "The soak is meant to be many rounds of ordinary play rather than one long turn.");
        }

        // -----------------------------------------------------------------------------------------
        // Scenarios
        // -----------------------------------------------------------------------------------------

        private static IReadOnlyList<MatchLoopRecord> PlayTheFourCompleteMatches() => new[]
        {
            DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(OneLifeCounter),
                LifeCounterMatchSeed, CompleteMatchBudget),
            DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters),
                FinalWaveMatchSeed, CompleteMatchBudget),
            DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateUncontestedVerifiedPushSession(),
                ThroneCaptureMatchSeed, CompleteMatchBudget),
            DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession(TwoLifeCounters),
                MidTurnPushMatchSeed, CompleteMatchBudget)
        };

        private static MatchSession CreateVerifiedMirrorWithTheTieBreakerOn(TeamSide side) =>
            ConfiguredMatchFixtures.CreateVerifiedMirrorSession(side);

        /// <summary>
        /// Commits one card for each seat and reports whose card the Initiative order selected first.
        /// </summary>
        private static TeamSide FirstActorSideOfTheFirstTurn(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.First(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand);
                ConfiguredMatchFixtures.Accept(
                    session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            Assert.That(session.Projection.SelectedActor.IsDefined, Is.True,
                "A 1v1 cross-team tie is resolved without a pause: one candidate a side leaves the " +
                "Tie Breaker's own card selected.");
            return session.Projection.Participants
                .GetTeamForHero(session.Projection.SelectedActor.ActorId).Side;
        }
    }
}
