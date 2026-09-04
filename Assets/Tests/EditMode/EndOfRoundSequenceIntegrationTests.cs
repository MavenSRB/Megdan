using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.8: rulebook p.8's end of round, run as one sequence rather than as six separately
    /// proved steps.
    ///
    /// Milestone 3.8c built the cursor and gave it a vocabulary for stages no system existed for yet -
    /// <c>NotApplicable</c> - and milestones 6.11, 6.12, 7.5 and 7.6 filled those systems in one at a
    /// time. This fixture is the integration the roadmap held back for last: a configured match now
    /// declares every one of p.8's ordered steps, runs each at its own checkpoint, and arrives at the
    /// next round or at a finished match without ever declaring a step unavailable.
    ///
    /// The two-round case at the end is not an ordering proof. It is the current-round projection's own
    /// contract - the stream that <c>MatchAggregate</c> calls "the current round's ordered events" and
    /// that `RISK-PERF-001` reassesses here - which nothing reset at a round boundary before this
    /// milestone.
    ///
    /// Every scenario is the configured match on the official map, driven by director commands, so what
    /// is under test is the sequence the match actually runs rather than a staged cursor.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class EndOfRoundSequenceIntegrationTests
    {
        private static readonly EndOfRoundStage[] OrderedStages =
        {
            EndOfRoundStage.MinionBattle,
            EndOfRoundStage.RemoveTokensAndReturnMarkers,
            EndOfRoundStage.ResolveRequestedLanePush,
            EndOfRoundStage.RetrieveCardsAndCancelActiveEffects,
            EndOfRoundStage.LevelUpAndUpgrade,
            EndOfRoundStage.CollectPityCoins
        };

        // -----------------------------------------------------------------------------------------
        // The ordered sequence itself
        // -----------------------------------------------------------------------------------------

        [Test]
        public void ThePage8Sequence_RunsEveryOrderedStepAndDeclaresNoneOfThemUnavailable()
        {
            var session = CreateConfiguredSession();
            PlayTurnsUntilEndOfRound(session);

            var events = RunEndOfRound(session);
            var resolved = StageOutcomesIn(events);

            Assert.That(resolved.Select(outcome => outcome.Stage), Is.EqualTo(OrderedStages),
                "p.8 lists its steps in one order and the cursor runs exactly that order, once each.");
            Assert.That(resolved.Select(outcome => outcome.Kind),
                Is.All.EqualTo(EndOfRoundStageOutcomeKind.Completed),
                "Every system p.8 names exists now, so a configured match has no step left to " +
                "declare unavailable - which is what milestone 3.8c's NotApplicable vocabulary was " +
                "for.");

            var completed = session.Projection.CompletedRounds.Single();
            Assert.That(completed.RoundIndex, Is.EqualTo(1));
            Assert.That(completed.EndOfRoundOutcomes.Select(outcome => outcome.Stage),
                Is.EqualTo(OrderedStages));
            Assert.That(completed.EndOfRoundOutcomes.Select(outcome => outcome.Kind),
                Is.All.EqualTo(EndOfRoundStageOutcomeKind.Completed),
                "The frozen history records the same six answers the stream reported.");

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2));
            Assert.That(session.Projection.CurrentEndOfRoundStage, Is.Null,
                "The cursor arrived at a valid next round rather than at a stage.");
        }

        [Test]
        public void ARoundThatAwardsNoPush_StillRunsThePushStepAndCompletesItHavingPushedNothing()
        {
            var session = CreateConfiguredSession();
            PlayTurnsUntilEndOfRound(session);

            var events = RunEndOfRound(session);

            Assert.That(events.Any(value => value.Kind == RoundResolutionEventKind.LanePushRequested),
                Is.False,
                "The mirrored setup's Minion Battle removes nothing, so p.8 awards no push.");
            Assert.That(
                events.Any(value => value.Kind == RoundResolutionEventKind.LanePushStageResolved),
                Is.False,
                "Nothing was pushed.");
            Assert.That(
                StageOutcomesIn(events)
                    .Single(outcome => outcome.Stage == EndOfRoundStage.ResolveRequestedLanePush).Kind,
                Is.EqualTo(EndOfRoundStageOutcomeKind.Completed),
                "\"Was there a push to resolve\" is a question this match asks and answers every " +
                "round; the answer being no is not the step being unavailable.");
        }

        [Test]
        public void ARequestedPush_RunsAtItsOwnCheckpointAndTheStepsAfterItStillRun()
        {
            var session = CreateUncontestedPushSession(waveCount: 5);
            PlayTurnsUntilEndOfRound(session);

            var events = RunEndOfRound(session);

            Assert.That(events.Any(value => value.Kind == RoundResolutionEventKind.LanePushRequested),
                Is.True,
                "The Minion Battle removed the only Blue minion in the Battle Zone, so Red won the " +
                "push.");
            Assert.That(StageAt(events, RoundResolutionEventKind.LanePushStageResolved),
                Is.EqualTo(EndOfRoundStage.ResolveRequestedLanePush),
                "p.8's numbered push steps run at the push stage and nowhere else.");

            var resolved = StageOutcomesIn(events);
            Assert.That(resolved.Select(outcome => outcome.Stage), Is.EqualTo(OrderedStages));
            Assert.That(resolved.Select(outcome => outcome.Kind),
                Is.All.EqualTo(EndOfRoundStageOutcomeKind.Completed));
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2),
                "A push that did not end the game leaves the round to finish its remaining steps.");
        }

        [Test]
        public void AGameEndingPush_StopsEveryLaterStepImmediately()
        {
            var session = CreateUncontestedPushSession(waveCount: 1);
            PlayTurnsUntilEndOfRound(session);

            var events = RunEndOfRound(session);

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(session.Projection.Outcome.Cause, Is.EqualTo(MatchVictoryCause.FinalWaveFlipped));

            var resolved = StageOutcomesIn(events);
            Assert.That(resolved.Select(outcome => outcome.Stage), Is.EqualTo(new[]
            {
                EndOfRoundStage.MinionBattle,
                EndOfRoundStage.RemoveTokensAndReturnMarkers,
                EndOfRoundStage.ResolveRequestedLanePush
            }), "The three steps before the ending ran; the three after it never started.");
            Assert.That(
                events.Any(value => value.Kind == RoundResolutionEventKind.CardRetrieved ||
                    value.Kind == RoundResolutionEventKind.HeroLevelUpProgressed ||
                    value.Kind == RoundResolutionEventKind.PityCoinCollected),
                Is.False,
                "\"The game ends immediately\" is about what may still happen, and retrieval, " +
                "leveling and the Pity Coin are three things that may not.");
            Assert.That(events.Any(value => value.Kind == RoundResolutionEventKind.RoundCompleted),
                Is.False);
            Assert.That(session.Projection.CompletedRounds, Is.Empty,
                "A round the match ended inside never completed, so nothing freezes it as a " +
                "completed round.");

            var later = session.Execute(DirectorCommand.AdvanceEndOfRound());
            Assert.That(later.Accepted, Is.False);
            Assert.That(later.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.GameAlreadyEnded));
        }

        // -----------------------------------------------------------------------------------------
        // What the individual steps do inside the sequence
        // -----------------------------------------------------------------------------------------

        [Test]
        public void RetrievalAndItsCancellationBoundary_HappenInsideTheStepsOwnCommand()
        {
            var session = CreateConfiguredSession();
            PlayTurnsUntilEndOfRound(session);
            var playedThisRound = session.Projection.Cards
                .Where(card => card.State.Location != CardLocation.Hand &&
                    card.State.Location != CardLocation.Deck)
                .Select(card => card.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(playedThisRound, Is.Not.Empty, "The round played cards, or retrieval proves nothing.");

            var events = RunEndOfRound(session);

            Assert.That(StageAt(events, RoundResolutionEventKind.CardRetrieved),
                Is.EqualTo(EndOfRoundStage.RetrieveCardsAndCancelActiveEffects));
            Assert.That(
                events.Where(value => value.Kind == RoundResolutionEventKind.CardRetrieved)
                    .Select(value => value.CardInstanceId.Value.Value)
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(playedThisRound),
                "p.8 returns every card the round resolved or discarded, and only those.");

            // p.17's cancellation boundary is a rules step of the round, reported by the stage that
            // reaches it rather than by whoever draws the round afterwards.
            Assert.That(
                events.Count(value =>
                    value.Kind == RoundResolutionEventKind.ActiveEffectCancellationCheckpoint),
                Is.EqualTo(1));
            Assert.That(StageAt(events, RoundResolutionEventKind.ActiveEffectCancellationCheckpoint),
                Is.EqualTo(EndOfRoundStage.RetrieveCardsAndCancelActiveEffects));
            Assert.That(
                IndexOfKind(events, RoundResolutionEventKind.ActiveEffectCancellationCheckpoint),
                Is.GreaterThan(IndexOfKind(events, RoundResolutionEventKind.CardRetrieved)),
                "The cards change state first; the effects their state change cancels follow.");
        }

        [Test]
        public void LevelingAndThePityCoin_PayExactlyTheHeroesPage8Names()
        {
            var session = CreateSessionWithACoinEarningRound(out var redHeroId);
            var blueHeroId = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;

            var events = RunEndOfRound(session);

            var purchases = events
                .Where(value => value.Kind == RoundResolutionEventKind.HeroLevelUpProgressed &&
                    value.HeroLevelUpOutcome.Kind == HeroLevelUpOutcomeKind.LevelPurchased)
                .Select(value => value.HeroLevelUpOutcome)
                .ToArray();
            Assert.That(purchases.Select(outcome => outcome.HeroId), Is.EqualTo(new[] { redHeroId }),
                "p.15 makes the purchase mandatory for the hero that can afford one, and only that " +
                "hero defeated anything this round.");

            var coins = events
                .Where(value => value.Kind == RoundResolutionEventKind.PityCoinCollected)
                .Select(value => value.PityCoinGrant.RecipientHeroId)
                .ToArray();
            Assert.That(coins, Is.EqualTo(new[] { blueHeroId }),
                "p.8 pays the coin to everyone who has NOT leveled up this round, which after the " +
                "step before it is exactly the hero that did not.");

            Assert.That(IndexOfKind(events, RoundResolutionEventKind.PityCoinCollected),
                Is.GreaterThan(IndexOfKind(events, RoundResolutionEventKind.HeroLevelUpProgressed)),
                "The coin's own question is about the step before it, so it is asked after that " +
                "step has answered.");
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2),
                "A round that paused on p.15's upgrade still reaches the next round once answered.");
        }

        // -----------------------------------------------------------------------------------------
        // Determinism
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheSameStateAndChoices_ProduceTheSameStateAndTheSameOrderedEvents()
        {
            var first = CreateConfiguredSession();
            PlayTurnsUntilEndOfRound(first);
            var firstEvents = RunEndOfRound(first);

            var second = CreateConfiguredSession();
            PlayTurnsUntilEndOfRound(second);
            var secondEvents = RunEndOfRound(second);

            Assert.That(secondEvents.Select(Describe), Is.EqualTo(firstEvents.Select(Describe)));
            Assert.That(second.Projection.AuthoritativeSnapshot,
                Is.EqualTo(first.Projection.AuthoritativeSnapshot));
            Assert.That(
                second.Projection.DiagnosticEvents.Select(entry => entry.DisplayText),
                Is.EqualTo(first.Projection.DiagnosticEvents.Select(entry => entry.DisplayText)));
        }

        // -----------------------------------------------------------------------------------------
        // The round boundary and the current-round projection
        // -----------------------------------------------------------------------------------------

        [Test]
        public void CompletingARound_StartsAFreshCurrentRoundSequenceAndKeepsTheOldOneInTheMatchLog()
        {
            var session = CreateConfiguredSession();
            PlayTurnsUntilEndOfRound(session);
            var firstRound = RunEndOfRound(session);

            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2));
            Assert.That(session.Projection.RoundEvents.Select(value => value.Kind),
                Is.EqualTo(new[] { RoundResolutionEventKind.NextRoundStarted }),
                "Completing a round takes that round's events out of the current-round projection, " +
                "and the event that started the new round is the new sequence's first entry.");
            Assert.That(session.Projection.RoundEvents.Select(value => value.RoundIndex),
                Is.All.EqualTo(2));

            // Retained, twice, by the two authorities that own the match's history.
            var retained = session.Projection.DiagnosticEvents
                .Where(entry => entry.StableReferences.StartsWith("round=1;", StringComparison.Ordinal))
                .ToArray();
            Assert.That(retained.Length, Is.GreaterThanOrEqualTo(firstRound.Count - 1),
                "Nothing the round produced is lost: the whole-match log keeps every event of it " +
                "except the boundary event that belongs to the round it started.");
            Assert.That(retained.Count(entry => entry.EventType == "Round.EndOfRoundStageResolved"),
                Is.EqualTo(OrderedStages.Length),
                "Including the six answers the end of round gave.");
            Assert.That(session.Projection.CompletedRounds.Single().RoundIndex, Is.EqualTo(1));

            // The new round appends to the fresh sequence rather than to the old one.
            CommitAndHoldOneTurn(session);
            Assert.That(session.Projection.RoundEvents.Count, Is.GreaterThan(1));
            Assert.That(session.Projection.RoundEvents.Select(value => value.RoundIndex),
                Is.All.EqualTo(2));
            Assert.That(session.Projection.RoundEvents[0].Kind,
                Is.EqualTo(RoundResolutionEventKind.NextRoundStarted));
        }

        [Test]
        public void TheRoundBoundary_IsReproducedByRestoreReplayAndRetainedPrefixUndo()
        {
            var session = CreateConfiguredSession();
            PlayTurnsUntilEndOfRound(session);
            RunEndOfRoundUpToItsLastStep(session);

            var beforeTheBoundary = session.Projection.AuthoritativeSnapshot;
            Accept(session, DirectorCommand.AdvanceEndOfRound());
            var afterTheBoundary = session.Projection.AuthoritativeSnapshot;
            var freshSequence = session.Projection.RoundEvents.Select(Describe).ToArray();
            Assert.That(freshSequence.Length, Is.EqualTo(1));

            // Replay: the same accepted prefix reaches the same boundary and the same fresh sequence.
            var replayed = CreateConfiguredSession();
            foreach (var command in session.Projection.AcceptedCommands)
            {
                Accept(replayed, command);
            }

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(afterTheBoundary));
            Assert.That(replayed.Projection.RoundEvents.Select(Describe), Is.EqualTo(freshSequence));

            // Retained-prefix Undo: truncating the command that closed the round puts the match back
            // inside it, with that round's events back in the current-round projection - which is the
            // statement that the boundary is a function of the command prefix rather than of an
            // accumulated stream.
            var undone = session.Undo();
            Assert.That(undone.Accepted, Is.True, undone.Detail);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(beforeTheBoundary));
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(1));
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.CollectPityCoins));
            Assert.That(session.Projection.RoundEvents.Select(value => value.RoundIndex),
                Is.All.EqualTo(1));
            Assert.That(
                session.Projection.RoundEvents.Any(value =>
                    value.Kind == RoundResolutionEventKind.NextRoundStarted),
                Is.False);

            Accept(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(session.Projection.RoundEvents.Select(Describe), Is.EqualTo(freshSequence),
                "Crossing the boundary a second time produces the same fresh sequence it did the " +
                "first time.");

            // Restore: a payload names a round position rather than a stream, so a restored match
            // starts the round it was in with no events yet and appends the next one to that sequence.
            var restored = CreateConfiguredSession();
            var result = restored.RestoreSnapshot(session.CaptureSnapshot());
            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.Projection.RoundIndex, Is.EqualTo(2));
            Assert.That(restored.Projection.RoundEvents, Is.Empty);

            CommitAndHoldOneTurn(restored);
            Assert.That(restored.Projection.RoundEvents, Is.Not.Empty);
            Assert.That(restored.Projection.RoundEvents.Select(value => value.RoundIndex),
                Is.All.EqualTo(2));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static MatchCommandResult Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
            return result;
        }

        /// <summary>Every stage outcome the stream reported, in the order it reported them.</summary>
        private static IReadOnlyList<EndOfRoundStageOutcome> StageOutcomesIn(
            IEnumerable<RoundResolutionEvent> events) =>
            events
                .Where(value => value.Kind == RoundResolutionEventKind.EndOfRoundStageResolved)
                .Select(value => new EndOfRoundStageOutcome(
                    value.EndOfRoundStage.Value, value.EndOfRoundOutcomeKind.Value))
                .ToArray();

        /// <summary>
        /// The stage the next reported outcome belongs to after the first event of the given kind -
        /// that is, the stage whose command produced it.
        /// </summary>
        private static EndOfRoundStage StageAt(
            IReadOnlyList<RoundResolutionEvent> events, RoundResolutionEventKind kind)
        {
            var index = IndexOfKind(events, kind);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), $"No {kind} event was reported.");
            for (var next = index; next < events.Count; next++)
            {
                if (events[next].Kind == RoundResolutionEventKind.EndOfRoundStageResolved)
                {
                    return events[next].EndOfRoundStage.Value;
                }
            }

            throw new InvalidOperationException($"No stage outcome followed the {kind} event.");
        }

        private static int IndexOfKind(
            IReadOnlyList<RoundResolutionEvent> events, RoundResolutionEventKind kind)
        {
            for (var index = 0; index < events.Count; index++)
            {
                if (events[index].Kind == kind)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>The comparable identity of one event, for the ordering comparisons above.</summary>
        private static string Describe(RoundResolutionEvent value) =>
            $"{value.Kind}|r{value.RoundIndex}t{value.TurnIndex}|" +
            $"{value.EndOfRoundStage}|{value.EndOfRoundOutcomeKind}|{value.CardInstanceId}";

        private static IReadOnlyList<RoundResolutionEvent> RunEndOfRound(MatchSession session)
        {
            var events = new List<RoundResolutionEvent>();
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            while (session.Projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                events.AddRange(AdvanceOneEndOfRoundCommand(session).Events);
            }

            return events;
        }

        /// <summary>Runs the end of round up to but not including the command that closes it.</summary>
        private static void RunEndOfRoundUpToItsLastStep(MatchSession session)
        {
            while (session.Projection.CurrentEndOfRoundStage != EndOfRoundStage.CollectPityCoins ||
                session.Projection.PendingSystemTransitionStep != null)
            {
                Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
                AdvanceOneEndOfRoundCommand(session);
            }
        }

        /// <summary>
        /// One accepted command of the end of round: the answer a stopped step is waiting for, or the
        /// advance that runs the next one.
        /// </summary>
        private static MatchCommandResult AdvanceOneEndOfRoundCommand(MatchSession session)
        {
            var minionBattle = session.Projection.PendingMinionBattleChoice;
            if (minionBattle != null)
            {
                if (minionBattle.SelectInvocations.Count > 0)
                {
                    return Accept(session,
                        DirectorCommand.SelectMinionBattleRemoval(minionBattle.SelectInvocations[0]));
                }

                return Accept(session,
                    DirectorCommand.ConfirmMinionBattleRemovals(minionBattle.ConfirmInvocation));
            }

            var pendingStep = session.Projection.PendingSystemTransitionStep;
            if (pendingStep != null)
            {
                return Accept(session,
                    DirectorCommand.AnswerSystemTransitionStep(pendingStep.Invocations[0]));
            }

            return Accept(session, DirectorCommand.AdvanceEndOfRound());
        }

        private static void PlayTurnsUntilEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments ||
                session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                CommitAndHoldOneTurn(session);
            }

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
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

        // ---------------------------------------------------------------------------------------
        // Sessions
        // ---------------------------------------------------------------------------------------

        /// <summary>The ordinary configured match, whose mirrored Minion Battle awards no push.</summary>
        private static MatchSession CreateConfiguredSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            return CreateSession(configuration, map, setup);
        }

        /// <summary>
        /// Milestone 7.7's push fixture: the configured match with exactly one Blue minion, standing in
        /// the starting Battle Zone, so p.8's Minion Battle removes it and awards Red the push.
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
        /// Milestone 7.5's fixture: the Red hero defeats one adjacent enemy melee minion, so it reaches
        /// p.8's Level Up step holding p.13's coins and the Blue hero reaches it holding none.
        /// </summary>
        private static MatchSession CreateSessionWithACoinEarningRound(out HeroId redHeroId)
        {
            var session = CreateSessionWithAnAdjacentEnemyMinion(out redHeroId, out var target);
            var redCard = session.Projection.Cards.Single(card => card.Id.Value == "red-pursuit-card");
            var blueCard = session.Projection.Cards.Single(card => card.Id.Value == "blue-longbow-card");
            Accept(session, DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id));
            Accept(session, DirectorCommand.CommitCard(blueCard.OwnerId, blueCard.Id));

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            var choice = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, target));
            Assert.That(session.Projection.Minions.Any(minion => minion.MinionId.Equals(target)),
                Is.False, "The Attack has to defeat the minion, or the round earns no coin.");

            // The Attack left this turn part-way through, so it is finished on its own terms before
            // the ordinary commit-and-hold loop takes the remaining turns.
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
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

            PlayTurnsUntilEndOfRound(session);
            return session;
        }

        /// <summary>
        /// The official-map configured match with one Blue melee minion beside the Red hero's spawn,
        /// relocated out of the Battle Zone so defeating it triggers no immediate Lane Push.
        /// </summary>
        private static MatchSession CreateSessionWithAnAdjacentEnemyMinion(
            out HeroId redHeroId, out MinionId target)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            redHeroId = redHero.HeroId;
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var beside = redHero.Coordinate.GetNeighbors()
                .First(coordinate => !occupied.Contains(coordinate) &&
                    map.Spaces.Any(space =>
                        space.Kind == BoardMapSpaceKind.Playable &&
                        space.Coordinate.Equals(coordinate)));

            var moved = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue && minion.MinionType == StandardMinionType.Melee);
            target = moved.MinionId;
            var movedId = target;
            var destination = beside;
            var minions = setup.Minions
                .Select(minion => minion.MinionId.Equals(movedId)
                    ? new QuickGameMinionSetupState(
                        minion.MinionId,
                        minion.TeamId,
                        minion.TeamSide,
                        minion.MinionType,
                        minion.SpawnPoint,
                        new ConfiguredSpaceReference(
                            map.Id,
                            map.Spaces.Single(space =>
                                space.Kind == BoardMapSpaceKind.Playable &&
                                space.Coordinate.Equals(destination)).Id),
                        destination)
                    : minion)
                .ToArray();
            return CreateSession(
                configuration,
                map,
                new QuickGameSetupState(
                    setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams,
                    setup.Heroes, minions));
        }

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
    }
}
