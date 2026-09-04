using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.7. The rulebook's three victory sentences, ending one match through one contract.
    ///
    /// p.8: "If the last remaining Wave counter is flipped, the game ends immediately and the team who
    /// won this final push wins the game", and "If the enemy Throne Zone itself becomes the new Battle
    /// Zone, the team who won the push wins the game." p.13: "If the last counter is spent, the game
    /// ends and the team with no Life counters left, loses the game."
    ///
    /// All three already ended the match before this milestone. What none of them did was answer the
    /// same question: p.8's two recorded a cause and a winning side on the Lane-Push transition and
    /// reported a stage boundary that named no team, while p.13's recorded a team at zero counters and
    /// reported the loser and no stage. Every scenario here therefore reads the ending the same way, off
    /// <c>MatchSessionProjection.Outcome</c> and off the one event, whichever rule produced it.
    ///
    /// The endings are reached rather than staged: each fixture is the configured match on the official
    /// map, driven by director commands until the rule that ends it fires. That is what makes the
    /// persistence half worth anything - the payloads under test are the payloads the match actually
    /// writes when it ends.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class MatchVictoryPathTests
    {
        private static readonly TeamId RedTeam = new TeamId(StagedBoardParticipants.RedTeamIdValue);
        private static readonly TeamId BlueTeam = new TeamId(StagedBoardParticipants.BlueTeamIdValue);
        private static readonly ZoneId BlueThrone = new ZoneId("blue-throne");

        // -----------------------------------------------------------------------------------------
        // p.13's third victory path: the last Life counter
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheLastLifeCounter_EndsTheMatchNamingItsWinnerAndItsReason()
        {
            var session = CreateAdjacentHeroSession(lifeCountersPerTeam: 1);

            var ending = DefeatTheBlueHero(session);

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(session.Projection.Outcome.Cause,
                Is.EqualTo(MatchVictoryCause.LifeCountersExhausted));
            Assert.That(session.Projection.Outcome.LosingTeamId, Is.EqualTo(BlueTeam),
                "p.13 names the team with no Life counters left.");
            Assert.That(session.Projection.Outcome.WinningTeamId, Is.EqualTo(RedTeam),
                "Milestone 7.7: and the contract names the other one, which p.13 leaves implied. " +
                "Before this milestone nothing in an ended match said who had won it.");

            var ended = ending.Events.Single(value => value.Kind == RoundResolutionEventKind.GameEnded);
            Assert.That(ended.MatchOutcome, Is.EqualTo(session.Projection.Outcome),
                "One event reports the ending, and it carries the whole outcome rather than the half " +
                "the rule that produced it happened to know.");
            Assert.That(ended.EndOfRoundStage, Is.Null,
                "p.13's ending happens inside an Attack, so it names no end-of-round stage.");
            Assert.That(session.Projection.TurnIndex, Is.EqualTo(1),
                "\"The game ends\" is literal: the turn is not completed and the round does not " +
                "advance past it.");
        }

        [Test]
        public void AMatchEndedByTheLastLifeCounter_StopsTheDecisionsItsTurnWasWaitingOn()
        {
            var session = CreateAdjacentHeroSession(lifeCountersPerTeam: 1);

            DefeatTheBlueHero(session);

            AssertNothingIsPending(session.Projection);
        }

        [Test]
        public void AMatchEndedByTheLastLifeCounter_RefusesEveryLaterCommandAsGameAlreadyEnded()
        {
            var session = CreateAdjacentHeroSession(lifeCountersPerTeam: 1);
            var beforeCommands = DefeatTheBlueHero(session);
            var payload = session.Projection.AuthoritativeSnapshot;

            AssertEveryLaterCommandIsRefused(session);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(payload),
                "A refused command leaves the ended match exactly as it was.");
            Assert.That(beforeCommands.Accepted, Is.True);
        }

        [Test]
        public void AMatchEndedByTheLastLifeCounter_RestoresToTheSameOutcomeAndStaysEnded()
        {
            var session = CreateAdjacentHeroSession(lifeCountersPerTeam: 1);
            DefeatTheBlueHero(session);
            var payload = session.CaptureSnapshot();
            var expected = session.Projection.AuthoritativeSnapshot;

            var restored = CreateAdjacentHeroSession(lifeCountersPerTeam: 1);
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(expected),
                "The ended match round-trips byte for byte, so nothing about the ending was " +
                "reconstructed differently from the way it was written.");
            Assert.That(restored.Projection.Outcome, Is.EqualTo(session.Projection.Outcome),
                "The outcome is derived from restored authority rather than carried in the payload, " +
                "so it has to come back the same without having been written down.");
            Assert.That(restored.Projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            AssertNothingIsPending(restored.Projection);
            AssertEveryLaterCommandIsRefused(restored);
        }

        [Test]
        public void AnUndoAcrossTheLastLifeCounter_ReturnsALiveMatchAndReplaysBackIntoTheSameEnding()
        {
            var session = CreateAdjacentHeroSession(lifeCountersPerTeam: 1);
            var ending = LastCommandOfTheDefeat(session, out var beforeTheEnding);
            var ended = session.Projection.AuthoritativeSnapshot;

            Assert.That(session.Undo().Accepted, Is.True);

            Assert.That(session.Projection.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded),
                "Truncating the retained prefix before the ending gives back a match that is still " +
                "being played, which is only possible because the ending discarded nothing.");
            Assert.That(session.Projection.Outcome.HasEnded, Is.False);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(beforeTheEnding));
            Assert.That(session.Projection.TeamLifeCounters.Single(entry =>
                entry.Key.Equals(BlueTeam)).Value, Is.EqualTo(1),
                "The Life counter the Defeat spent was re-spent by the replayed prefix rather than " +
                "left spent.");

            var again = session.Execute(ending.Command);
            Assert.That(again.Accepted, Is.True, again.Detail);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(ended),
                "Replaying the same command reproduces the same ended match byte for byte.");
            Assert.That(session.Projection.Outcome.Cause,
                Is.EqualTo(MatchVictoryCause.LifeCountersExhausted));
        }

        // -----------------------------------------------------------------------------------------
        // p.8's first victory path: the last Wave counter
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheFinalWaveFlip_EndsTheMatchForTheTeamThatWonThePush()
        {
            var session = CreateUncontestedPushSession(waveCount: 1);

            var ending = PushAtTheEndOfTheRound(session);

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(session.Projection.Outcome.Cause,
                Is.EqualTo(MatchVictoryCause.FinalWaveFlipped));
            Assert.That(session.Projection.Outcome.WinningTeamId, Is.EqualTo(RedTeam),
                "p.8 awards the game to \"the team who won this final push\", and Red won it: Blue " +
                "had no minion left in the Battle Zone.");
            Assert.That(session.Projection.Outcome.LosingTeamId, Is.EqualTo(BlueTeam));
            Assert.That(CurrentBattleZone(session), Is.EqualTo(new ZoneId("starting-battle-zone")),
                "p.8 flips the Wave counter before the Zone moves, so the final flip ends the match " +
                "with the Battle Zone where it was.");

            var ended = ending.Events.Single(value => value.Kind == RoundResolutionEventKind.GameEnded);
            Assert.That(ended.MatchOutcome, Is.EqualTo(session.Projection.Outcome));
            Assert.That(ended.EndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.ResolveRequestedLanePush),
                "An end-of-round push does name the stage it ended at.");
            AssertNothingIsPending(session.Projection);
            AssertEveryLaterCommandIsRefused(session);
        }

        [Test]
        public void AMatchEndedByTheFinalWave_RestoresToTheSameOutcome()
        {
            var session = CreateUncontestedPushSession(waveCount: 1);
            PushAtTheEndOfTheRound(session);
            var payload = session.CaptureSnapshot();
            var expected = session.Projection.AuthoritativeSnapshot;

            var restored = CreateUncontestedPushSession(waveCount: 1);
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(expected));
            Assert.That(restored.Projection.Outcome, Is.EqualTo(session.Projection.Outcome));
            AssertEveryLaterCommandIsRefused(restored);
        }

        // -----------------------------------------------------------------------------------------
        // p.8's second victory path: the enemy Throne
        // -----------------------------------------------------------------------------------------

        [Test]
        public void ArrivingAtTheEnemyThrone_EndsTheMatchForTheTeamThatWonThePush()
        {
            var session = CreateUncontestedPushSession(waveCount: 5);

            PushAtTheEndOfTheRound(session);
            Assert.That(session.Projection.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded),
                "The official Lane runs red-throne, red-approach, starting-battle-zone, " +
                "blue-approach, blue-throne, so the first push is one Zone short of the capture.");
            var ending = PushAtTheEndOfTheRound(session);

            Assert.That(CurrentBattleZone(session), Is.EqualTo(BlueThrone));
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(session.Projection.Outcome.Cause, Is.EqualTo(MatchVictoryCause.ThroneCaptured));
            Assert.That(session.Projection.Outcome.WinningTeamId, Is.EqualTo(RedTeam));
            Assert.That(session.Projection.Outcome.LosingTeamId, Is.EqualTo(BlueTeam));
            Assert.That(session.Projection.TeamLifeCounters.All(entry => entry.Value > 0), Is.True,
                "This ending owes nothing to p.13: both teams still hold every Life counter, and the " +
                "match is over anyway.");

            var ended = ending.Events.Single(value => value.Kind == RoundResolutionEventKind.GameEnded);
            Assert.That(ended.MatchOutcome, Is.EqualTo(session.Projection.Outcome));
            AssertEveryLaterCommandIsRefused(session);
        }

        [Test]
        public void AMatchEndedByAThroneCapture_RestoresToTheSameOutcome()
        {
            var session = CreateUncontestedPushSession(waveCount: 5);
            PushAtTheEndOfTheRound(session);
            PushAtTheEndOfTheRound(session);
            var payload = session.CaptureSnapshot();
            var expected = session.Projection.AuthoritativeSnapshot;

            var restored = CreateUncontestedPushSession(waveCount: 5);
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(expected));
            Assert.That(restored.Projection.Outcome, Is.EqualTo(session.Projection.Outcome));
            Assert.That(restored.Projection.Outcome.Cause, Is.EqualTo(MatchVictoryCause.ThroneCaptured),
                "The cause survives the round trip although nothing writes it down: the push " +
                "transition's own recorded cause and winning side are what it is read from.");
            AssertEveryLaterCommandIsRefused(restored);
        }

        // -----------------------------------------------------------------------------------------
        // Milestone 8.16: p.8's endings reached in the middle of a turn
        //
        // Both cases above push the Lane at the end of a round, and until this milestone that was the
        // only way either of p.8's sentences had ever been reached in a live match. p.8 also pushes
        // "in the middle of a turn ... if you defeat, or remove, the last remaining enemy minion in
        // the Battle Zone", and such a push is consumed inside the turn authority's own Initiative
        // advance rather than at an end-of-round stage - so it never crossed `EndGame`, the phase
        // never recorded the ending, and the next projection this match built threw
        // `UnrecordedGameEnd`. The two cases below are the same board, one command apart from the
        // ending, and they are what the milestone-8.16 gate found by playing the Wasp mirror.
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AMidTurnLanePushThatFlipsTheFinalWave_EndsTheMatchInsideTheTurnItHappenedIn()
        {
            var session = CreateSessionWithTheLastEnemyMinionBesideTheRedHero(waveCount: 1);

            var ending = DefeatTheLastEnemyMinion(session);

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded),
                "p.8: \"the game ends immediately\". The push that flipped the last Wave happened " +
                "inside an Attack, and the match has to record that it ended there.");
            Assert.That(session.Projection.Outcome.Cause,
                Is.EqualTo(MatchVictoryCause.FinalWaveFlipped));
            Assert.That(session.Projection.Outcome.WinningTeamId, Is.EqualTo(RedTeam),
                "Red won the push: Blue had no minion left in the Battle Zone once this one fell.");
            Assert.That(session.Projection.Outcome.LosingTeamId, Is.EqualTo(BlueTeam));
            Assert.That(session.Projection.TeamLifeCounters.All(entry => entry.Value > 0), Is.True,
                "This ending owes nothing to p.13, so the Life-counter question the turn boundary " +
                "used to ask was the wrong one to be asking on its own.");

            var ended = ending.Events.Single(value => value.Kind == RoundResolutionEventKind.GameEnded);
            Assert.That(ended.MatchOutcome, Is.EqualTo(session.Projection.Outcome));
            Assert.That(ended.EndOfRoundStage, Is.Null,
                "A mid-turn push names no stage, which is what distinguishes this ending from the " +
                "end-of-round push above rather than anything a fixture asserts about itself.");
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(1),
                "and it ended in the first round, before any end-of-round stage had run at all.");
            AssertNothingIsPending(session.Projection);
            AssertEveryLaterCommandIsRefused(session);
        }

        [Test]
        public void AMatchEndedByAMidTurnLanePush_RestoresToTheSameOutcome()
        {
            var session = CreateSessionWithTheLastEnemyMinionBesideTheRedHero(waveCount: 1);
            DefeatTheLastEnemyMinion(session);
            var payload = session.CaptureSnapshot();
            var expected = session.Projection.AuthoritativeSnapshot;

            var restored = CreateSessionWithTheLastEnemyMinionBesideTheRedHero(waveCount: 1);
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(expected));
            Assert.That(restored.Projection.Outcome, Is.EqualTo(session.Projection.Outcome));
            AssertEveryLaterCommandIsRefused(restored);
        }

        // -----------------------------------------------------------------------------------------
        // The three paths answer in one vocabulary
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheThreePaths_ProduceThreeCausesAndOneEventShape()
        {
            var byLife = CreateAdjacentHeroSession(lifeCountersPerTeam: 1);
            DefeatTheBlueHero(byLife);
            var byWave = CreateUncontestedPushSession(waveCount: 1);
            PushAtTheEndOfTheRound(byWave);
            var byThrone = CreateUncontestedPushSession(waveCount: 5);
            PushAtTheEndOfTheRound(byThrone);
            PushAtTheEndOfTheRound(byThrone);

            var causes = new[] { byLife, byWave, byThrone }
                .Select(session => session.Projection.Outcome.Cause)
                .ToArray();

            Assert.That(causes, Is.EqualTo(new[]
            {
                MatchVictoryCause.LifeCountersExhausted,
                MatchVictoryCause.FinalWaveFlipped,
                MatchVictoryCause.ThroneCaptured
            }), "Three sentences, three causes, told apart by the reason rather than by which rule " +
                "the reader had to know produced them.");
            foreach (var session in new[] { byLife, byWave, byThrone })
            {
                var ending = session.Projection.RoundEvents
                    .Single(value => value.Kind == RoundResolutionEventKind.GameEnded);
                Assert.That(ending.MatchOutcome.HasEnded, Is.True);
                Assert.That(ending.MatchOutcome.WinningTeamId, Is.EqualTo(RedTeam));
                Assert.That(ending.MatchOutcome.LosingTeamId, Is.EqualTo(BlueTeam));
                Assert.That(session.Projection.DiagnosticEvents.Any(entry =>
                        entry.EventType == "Round.GameEnded" &&
                        entry.Detail.Contains(session.Projection.Outcome.Cause.ToString())),
                    Is.True,
                    "The diagnostic log reports the same reason the projection does.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Duplicate and conflicting winners
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TwoVictoryPathsClaimingTheSameMatch_AreRejectedRatherThanRanked()
        {
            var participants = StagedBoardParticipants.Create();
            var lives = new TeamLifeState(participants, 1);
            Assert.That(lives.TrySpend(BlueTeam, 1).Accepted, Is.True);

            var reading = MatchOutcomeQuery.Read(
                participants, LanePushGameEndingCause.FinalWaveFlipped, TeamSide.Red, lives, true);

            Assert.That(reading.Fault, Is.EqualTo(MatchOutcomeFault.ConflictingVictoryPaths),
                "Both paths name Red the winner, and it is still refused: the second ending had no " +
                "match left to end, so a payload carrying both was not written by a match that was " +
                "played.");
        }

        [Test]
        public void TwoTeamsWithNoLifeCountersLeft_AreTheDuplicateWinnerCaseAndAreRejected()
        {
            var participants = StagedBoardParticipants.Create();
            var lives = new TeamLifeState(participants, 1);
            Assert.That(lives.TrySpend(BlueTeam, 1).Accepted, Is.True);
            Assert.That(lives.TrySpend(RedTeam, 1).Accepted, Is.True);

            var reading = MatchOutcomeQuery.Read(
                participants, LanePushGameEndingCause.None, TeamSide.None, lives, true);

            Assert.That(reading.Fault, Is.EqualTo(MatchOutcomeFault.ConflictingVictoryPaths),
                "p.13's own state can carry the duplicate: two teams at zero counters is two losers " +
                "and therefore no winner. `TeamLifeState.EliminatedTeamId` answers with the first of " +
                "them, which is why this contract counts them instead of asking it.");
        }

        [Test]
        public void APushWonByASideThisMatchDoesNotHave_NamesNoWinnerAndIsRejected()
        {
            var participants = StagedBoardParticipants.Create();

            var reading = MatchOutcomeQuery.Read(
                participants,
                LanePushGameEndingCause.ThroneCaptured,
                TeamSide.None,
                new TeamLifeState(participants, 3),
                true);

            Assert.That(reading.Fault, Is.EqualTo(MatchOutcomeFault.UndeclaredOutcomeTeams));
            Assert.That(reading.Outcome.HasEnded, Is.False,
                "A faulted reading carries no outcome at all, so no caller can act on half of one.");
        }

        [Test]
        public void APhaseThatDisagreesWithEveryVictoryPath_IsRejectedInBothDirections()
        {
            var participants = StagedBoardParticipants.Create();
            var untouched = new TeamLifeState(participants, 3);
            var spent = new TeamLifeState(participants, 1);
            Assert.That(spent.TrySpend(BlueTeam, 1).Accepted, Is.True);

            Assert.That(
                MatchOutcomeQuery.Read(
                    participants, LanePushGameEndingCause.None, TeamSide.None, untouched, true).Fault,
                Is.EqualTo(MatchOutcomeFault.UnexplainedGameEnd),
                "A match recorded as over that no sentence ended has a winner nobody can name.");
            Assert.That(
                MatchOutcomeQuery.Read(
                    participants, LanePushGameEndingCause.None, TeamSide.None, spent, false).Fault,
                Is.EqualTo(MatchOutcomeFault.UnrecordedGameEnd),
                "And a sentence that fired while the match is still being played is the same " +
                "disagreement from the other side.");
            Assert.That(
                MatchOutcomeQuery.Read(
                    participants, LanePushGameEndingCause.None, TeamSide.None, untouched, false)
                    .Outcome.HasEnded,
                Is.False,
                "An ordinary match in progress is neither.");
        }

        [Test]
        public void APayloadClaimingTwoEndings_IsRefusedWithIncoherentGameOutcome()
        {
            var session = CreateUncontestedPushSession(waveCount: 5);
            PushAtTheEndOfTheRound(session);
            PushAtTheEndOfTheRound(session);
            var tampered = SpendEveryLifeCounterOf(
                session.Projection.AuthoritativeSnapshot,
                BlueTeam,
                session.Projection.TeamLifeCounters.Single(entry => entry.Key.Equals(BlueTeam)).Value);

            var rejection = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    tampered,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap },
                    session.Projection.Setup.Minions));

            Assert.That(rejection.Reason,
                Is.EqualTo(SnapshotLoadRejectionReason.IncoherentGameOutcome),
                "The ending is derived from one record rather than written down beside it, so the " +
                "reader owes the check that record would have carried - milestone 7.3's bargain, on " +
                "milestone 7.7's subject.");
            Assert.That(rejection.Message, Does.Contain(
                MatchOutcomeFault.ConflictingVictoryPaths.ToString()),
                "Which disagreement it was is a diagnostic, so it is in the message rather than in a " +
                "second rejection reason.");
        }

        // -----------------------------------------------------------------------------------------
        // Shared assertions
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 7.7's "stops pending resolution", read off the one surface every caller has.
        ///
        /// The turn these matches end in is genuinely mid-resolution - p.13's ending fires inside an
        /// Attack - so before this milestone a hero-respawn choice could still be offered by a match
        /// that had already finished, and nothing could answer it.
        /// </summary>
        private static void AssertNothingIsPending(MatchSessionProjection projection)
        {
            Assert.That(projection.AvailableTurnActions, Is.Empty);
            Assert.That(projection.AvailableFastTravelOptions, Is.Empty);
            Assert.That(projection.AvailableClearOptions, Is.Empty);
            Assert.That(projection.PendingInitiativeChoice, Is.Null);
            Assert.That(projection.PendingHeroRespawnChoice, Is.Null);
            Assert.That(projection.PendingMovementResolution, Is.Null);
            Assert.That(projection.PendingMovementChoice, Is.Null);
            Assert.That(projection.PendingAttackResolution, Is.Null);
            Assert.That(projection.PendingAttackTargetChoice, Is.Null);
            Assert.That(projection.PendingDefenseChoice, Is.Null);
            Assert.That(projection.PendingDefensePreview, Is.Null);
            Assert.That(projection.PendingSkillResolution, Is.Null);
            Assert.That(projection.PendingEffectChoice, Is.Null);
            Assert.That(projection.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(projection.PendingImmediateLanePush, Is.Null);
            Assert.That(projection.PendingMinionBattleChoice, Is.Null);
            Assert.That(projection.PendingLanePushRequest, Is.Null);
            Assert.That(projection.PendingSystemTransitionStep, Is.Null);
        }

        /// <summary>
        /// Milestone 7.7's "prevents later rule steps". Every command family the match accepts is
        /// offered to a match that has ended, and every one of them is refused with the reason the two
        /// pages give rather than with whichever cursor check happened to catch it first.
        /// </summary>
        private static void AssertEveryLaterCommandIsRefused(MatchSession session)
        {
            var hero = session.Projection.Participants.Heroes[0].Id;
            var card = session.Projection.Cards[0].Id;
            var commands = new[]
            {
                DirectorCommand.CommitCard(hero, card),
                DirectorCommand.Pass(hero),
                DirectorCommand.PerformHold(card),
                DirectorCommand.AdvanceEndOfRound()
            };

            foreach (var command in commands)
            {
                var result = session.Execute(command);
                Assert.That(result.Accepted, Is.False, $"{command.Kind} was accepted after the match ended.");
                Assert.That(result.RoundRejectionReason,
                    Is.EqualTo(RoundResolutionRejectionReason.GameAlreadyEnded),
                    $"{command.Kind} was refused for the wrong reason.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Fixtures
        // -----------------------------------------------------------------------------------------

        private static MatchCommandResult Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
            return result;
        }

        private static ZoneId CurrentBattleZone(MatchSession session) =>
            session.Projection.SelectedMap.Zones
                .Single(zone => zone.Id.Equals(BattleZoneIdOf(session))).Id;

        private static ZoneId BattleZoneIdOf(MatchSession session)
        {
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
            return restored.Board.CurrentBattleZoneId.Value;
        }

        /// <summary>Drives the Red hero's Attack into the adjacent Blue hero and returns its last command's result.</summary>
        private static MatchCommandResult DefeatTheBlueHero(MatchSession session) =>
            LastCommandOfTheDefeat(session, out _);

        private static MatchCommandResult LastCommandOfTheDefeat(
            MatchSession session, out string beforeTheEnding) =>
            session.Execute(StageTheDefeat(session, out beforeTheEnding)) is var result && result.Accepted
                ? result
                : throw new InvalidOperationException(result.Detail);

        /// <summary>
        /// Commits both cards, starts Red Pursuit's Attack and chooses the Blue hero, stopping on the
        /// one command that ends the match so an Undo has something to truncate.
        /// </summary>
        private static DirectorCommand StageTheDefeat(MatchSession session, out string beforeTheEnding)
        {
            var red = session.Projection.Cards.Single(card => card.Id.Value == "red-pursuit-card");
            var blue = session.Projection.Cards.Single(card => card.Id.Value == "blue-hammer-card");
            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "Red Pursuit's Initiative 7 acts before Blue Hammer's 6.");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            var target = session.Projection.PendingAttackTargetChoice;
            Assert.That(target.LegalTargetHeroIds.Count, Is.EqualTo(1),
                "The Blue hero stands beside the Red one, so it is the Attack's only hero target.");
            Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, target.LegalTargetHeroIds[0]));

            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "p.14 gives the defender its response before the Attack resolves.");
            beforeTheEnding = session.Projection.AuthoritativeSnapshot;
            return DirectorCommand.DeclineDefense();
        }

        /// <summary>
        /// Plays one round out with Holds and resolves the Lane Push p.8's Minion Battle awards at the
        /// end of it, returning the result of the command that resolved the push.
        /// </summary>
        private static MatchCommandResult PushAtTheEndOfTheRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments ||
                session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                CommitAndHoldOneTurn(session);
            }

            MatchCommandResult push = null;
            while (session.Projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                var stage = session.Projection.CurrentEndOfRoundStage;
                if (session.Projection.PendingMinionBattleChoice != null)
                {
                    while (session.Projection.PendingMinionBattleChoice.SelectInvocations.Count > 0)
                    {
                        Accept(session, DirectorCommand.SelectMinionBattleRemoval(
                            session.Projection.PendingMinionBattleChoice.SelectInvocations[0]));
                    }

                    Accept(session, DirectorCommand.ConfirmMinionBattleRemovals(
                        session.Projection.PendingMinionBattleChoice.ConfirmInvocation));
                    continue;
                }

                var result = Accept(session, DirectorCommand.AdvanceEndOfRound());
                if (stage == EndOfRoundStage.ResolveRequestedLanePush)
                {
                    push = result;
                }
            }

            Assert.That(push, Is.Not.Null, "p.8's Minion Battle awarded no push this round.");
            return push;
        }

        private static void CommitAndHoldOneTurn(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.FirstOrDefault(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand);
                if (card == null)
                {
                    Accept(session, DirectorCommand.Pass(
                        session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId));
                    continue;
                }

                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }

            var turnIndex = session.Projection.TurnIndex;
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                session.Projection.TurnIndex == turnIndex)
            {
                // Milestone 8.35g. A finished resolution waits for its own player to hand the
                // turn on, so this loop presses the release exactly as that player does.
                if (session.Projection.PendingTurnRelease != null)
                {
                    Accept(session, DirectorCommand.ReleaseTurn());
                    continue;
                }

                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }
        }

        /// <summary>
        /// The configured match with the Blue hero standing beside the Red one, so the first Attack of
        /// the first turn can reach p.13's Defeat, and the stated number of Life counters per team.
        /// </summary>
        private static MatchSession CreateAdjacentHeroSession(int lifeCountersPerTeam)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var beside = redHero.Coordinate.GetNeighbors()
                .First(coordinate => !occupied.Contains(coordinate) &&
                    map.Spaces.Any(space =>
                        space.Kind == BoardMapSpaceKind.Playable &&
                        space.Coordinate.Equals(coordinate)));
            var besideSpaceId = map.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                space.Coordinate.Equals(beside)).Id;

            var heroes = setup.Heroes
                .Select(hero => hero.TeamSide == TeamSide.Blue
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, besideSpaceId), beside)
                    : hero)
                .ToArray();
            return CreateSession(
                configuration,
                map,
                new QuickGameSetupState(
                    setup.SelectedMap,
                    setup.WaveCount,
                    setup.TieBreakerState,
                    WithLifeCounters(setup.Teams, lifeCountersPerTeam),
                    heroes,
                    setup.Minions));
        }

        /// <summary>
        /// The configured match with exactly one Blue minion, standing in the starting Battle Zone, so
        /// p.8's Minion Battle removes it and awards Red the push at the end of every round.
        ///
        /// One rather than none: the engine requests a push only when the battle actually removed a
        /// unit, which is p.8's "The team with one or more minions still remaining in the Battle Zone
        /// wins the push" read as a battle that happened. One rather than six: p.8's third step
        /// respawns "one minion of the matching type in each Spawn point in the new Battle Zone" from
        /// the supply, and a supply holding the single minion the battle removed puts exactly one Blue
        /// minion in the next Zone - which is what lets the next round push again, and that is what
        /// reaches the Throne, since the official Lane puts it two Zones away.
        /// </summary>
        private static MatchSession CreateUncontestedPushSession(int waveCount)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var survivor = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue &&
                minion.MinionType != StandardMinionType.Heavy &&
                GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInStartingBattleZone(
                    map, minion.Coordinate));
            return CreateSession(
                configuration,
                map,
                new QuickGameSetupState(
                    setup.SelectedMap,
                    waveCount,
                    setup.TieBreakerState,
                    setup.Teams,
                    setup.Heroes,
                    setup.Minions
                        .Where(minion => minion.TeamSide != TeamSide.Blue ||
                            minion.MinionId.Equals(survivor.MinionId))
                        .ToArray()));
        }

        /// <summary>
        /// Milestone 8.16. The configured match with Blue holding exactly one minion - the melee one
        /// standing in the starting Battle Zone - and the Red hero moved beside it.
        ///
        /// The hero is what moves, not the minion: p.8's mid-turn push asks for "the last remaining
        /// enemy minion in the Battle Zone", so a fixture that stood the target outside the Zone to
        /// reach it would be testing a different sentence. Red keeps its own minions, which is what
        /// gives the resulting push a winner - p.8 awards it to "the team with one or more minions
        /// still remaining in the Battle Zone".
        /// </summary>
        private static MatchSession CreateSessionWithTheLastEnemyMinionBesideTheRedHero(int waveCount)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var survivor = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue &&
                minion.MinionType == StandardMinionType.Melee &&
                GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInStartingBattleZone(
                    map, minion.Coordinate));
            var minions = setup.Minions
                .Where(minion => minion.TeamSide != TeamSide.Blue ||
                    minion.MinionId.Equals(survivor.MinionId))
                .ToArray();

            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(minions.Select(minion => minion.Coordinate)));
            var beside = survivor.Coordinate.GetNeighbors()
                .First(coordinate => !occupied.Contains(coordinate) &&
                    map.Spaces.Any(space =>
                        space.Kind == BoardMapSpaceKind.Playable &&
                        space.Coordinate.Equals(coordinate)));
            var besideSpaceId = map.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                space.Coordinate.Equals(beside)).Id;

            var heroes = setup.Heroes
                .Select(hero => hero.TeamSide == TeamSide.Red
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, besideSpaceId), beside)
                    : hero)
                .ToArray();

            return CreateSession(
                configuration,
                map,
                new QuickGameSetupState(
                    setup.SelectedMap,
                    waveCount,
                    setup.TieBreakerState,
                    setup.Teams,
                    heroes,
                    minions));
        }

        /// <summary>
        /// Commits both cards, starts the Red hero's Attack and takes the one minion it can reach,
        /// returning the result of the command that took it - which is the command p.8's mid-turn push
        /// runs inside, and therefore the command that ends the match.
        /// </summary>
        private static MatchCommandResult DefeatTheLastEnemyMinion(MatchSession session)
        {
            var red = session.Projection.Cards.Single(card => card.Id.Value == "red-pursuit-card");
            var blue = session.Projection.Cards.Single(card => card.Id.Value == "blue-hammer-card");
            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "Red Pursuit's Initiative 7 acts before Blue Hammer's 6.");

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            var target = session.Projection.PendingAttackTargetChoice;
            Assert.That(target.LegalTargetMinionIds.Count, Is.EqualTo(1),
                "Blue holds one minion and it stands beside the Red hero, so it is the Attack's only " +
                "minion target.");
            var minionId = target.LegalTargetMinionIds[0];
            var result = Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, minionId));

            Assert.That(
                session.Projection.Minions.Any(minion =>
                    minion.MinionId.Equals(minionId) &&
                    GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInStartingBattleZone(
                        session.Projection.SelectedMap, minion.Coordinate)),
                Is.False,
                "The Attack has to actually take the minion out of the Battle Zone, or there is no " +
                "push for the ending to be reached through.");
            return result;
        }

        private static IReadOnlyList<QuickGameTeamSetupState> WithLifeCounters(
            IEnumerable<QuickGameTeamSetupState> teams, int lifeCount) =>
            teams.Select(team => new QuickGameTeamSetupState(
                team.TeamId, team.TeamSide, lifeCount, team.ThroneZoneId)).ToArray();

        private static MatchSession CreateSession(
            MatchConfiguration configuration,
            BoardMapDefinition map,
            QuickGameSetupState setup)
        {
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// Rewrites one team's remaining Life counters to zero, so a payload that already carries one
        /// of p.8's endings carries p.13's as well.
        ///
        /// The section is located by its own written shape - the two team ids in the order
        /// <c>WriteTeamLives</c> orders them, each followed by the count this match holds - and the
        /// search asserts it found exactly one, so a later section that happened to write the same
        /// bytes would fail this fixture rather than silently move it onto the wrong field. That is the
        /// same principle as the serializer fixture's prefix walk, taken from the other end because
        /// this field sits behind the whole round section rather than in front of it.
        /// </summary>
        private static string SpendEveryLifeCounterOf(string payload, TeamId teamId, int counters)
        {
            var bytes = Convert.FromBase64String(payload);
            var signature = TeamLifeSectionSignature(counters);
            var matches = Enumerable.Range(0, bytes.Length - signature.Length + 1)
                .Where(offset => signature
                    .Select((value, index) => bytes[offset + index] == value)
                    .All(equal => equal))
                .ToArray();
            Assert.That(matches.Length, Is.EqualTo(1),
                "The Life-counter section must be found exactly once for this rewrite to mean anything.");

            var target = matches[0] + LengthPrefixed(teamId.Value).Length;
            if (!teamId.Equals(BlueTeam))
            {
                target += sizeof(int) + LengthPrefixed(BlueTeam.Value).Length;
            }

            using (var stream = new MemoryStream(bytes, true))
            {
                stream.Position = target;
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                {
                    writer.Write(0);
                }
            }

            return Convert.ToBase64String(bytes);
        }

        private static byte[] TeamLifeSectionSignature(int counters) =>
            LengthPrefixed(BlueTeam.Value)
                .Concat(BitConverter.GetBytes(counters))
                .Concat(LengthPrefixed(RedTeam.Value))
                .Concat(BitConverter.GetBytes(counters))
                .ToArray();

        private static byte[] LengthPrefixed(string value) =>
            new byte[] { (byte)value.Length }
                .Concat(System.Text.Encoding.UTF8.GetBytes(value))
                .ToArray();
    }
}
