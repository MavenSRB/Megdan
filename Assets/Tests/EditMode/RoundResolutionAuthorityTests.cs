using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class RoundResolutionAuthorityTests
    {
        [Test]
        public void FourTurns_RecordResolvedCardsInSlotsAndEnterEndOfRound()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = CreateAuthority(configuration);
            var allEvents = new List<RoundResolutionEvent>();

            for (var turnIndex = 1; turnIndex <= RoundResolutionAuthority.TurnsPerRound; turnIndex++)
            {
                var red = configuration.RedHand.Instances[turnIndex - 1];
                var blue = configuration.BlueHand.Instances[turnIndex - 1];
                allEvents.AddRange(authority.TryCommitCard(red.OwnerId, red.Id).Events);
                allEvents.AddRange(authority.TryCommitCard(blue.OwnerId, blue.Id).Events);

                Assert.That(authority.TurnSlots, Has.Count.EqualTo(turnIndex));
                var slot = authority.TurnSlots[turnIndex - 1];
                Assert.That(slot.RoundIndex, Is.EqualTo(1));
                Assert.That(slot.TurnIndex, Is.EqualTo(turnIndex));
                Assert.That(slot.Assignments, Has.Count.EqualTo(2));
                Assert.That(slot.Assignments.Single(assignment => assignment.HeroId.Equals(red.OwnerId))
                    .CardInstanceId, Is.EqualTo(red.Id));
                Assert.That(slot.Assignments.Single(assignment => assignment.HeroId.Equals(blue.OwnerId))
                    .CardInstanceId, Is.EqualTo(blue.Id));
                Assert.That(slot.Assignments, Has.None.Matches<TurnCardAssignment>(assignment =>
                    assignment.PassedWithEmptyHand));

                while (authority.Phase == RoundResolutionPhase.ResolvingTurn)
                {
                    var selectedCardId = authority.CurrentTurnAuthority.SelectedActor.CardInstanceId;
                    var hold = authority.TryPerformHold(selectedCardId);
                    Assert.That(hold.Accepted, Is.True);
                    allEvents.AddRange(hold.Events);
                }

                Assert.That(red.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
                Assert.That(blue.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
                if (turnIndex < RoundResolutionAuthority.TurnsPerRound)
                {
                    Assert.That(authority.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
                    Assert.That(authority.TurnIndex, Is.EqualTo(turnIndex + 1));
                }
            }

            Assert.That(authority.RoundIndex, Is.EqualTo(1));
            Assert.That(authority.TurnIndex, Is.EqualTo(RoundResolutionAuthority.TurnsPerRound));
            Assert.That(authority.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            Assert.That(allEvents.Count(ruleEvent =>
                ruleEvent.Kind == RoundResolutionEventKind.EndOfRoundEntered), Is.EqualTo(1));
            Assert.That(allEvents.Last().Kind, Is.EqualTo(RoundResolutionEventKind.EndOfRoundEntered));
            Assert.That(configuration.RedHand.Instances[4].State, Is.EqualTo(CardStateFacts.InitialInHand()),
                "Entering end of round must not retrieve, play, or otherwise resolve its deferred subsystems.");
            Assert.That(configuration.BlueHand.Instances[4].State, Is.EqualTo(CardStateFacts.InitialInHand()));
        }

        [Test]
        public void ResolvedCard_CannotBeReplayedInALaterTurn()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = CreateAuthority(configuration);
            var red = configuration.RedHand.Instances[0];
            var blue = configuration.BlueHand.Instances[0];
            Assert.That(authority.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(blue.OwnerId, blue.Id).Accepted, Is.True);
            ResolveCurrentTurnWithHold(authority);
            var statesBefore = configuration.CardInstances.ToDictionary(card => card.Id, card => card.State);

            var replay = authority.TryCommitCard(red.OwnerId, red.Id);

            Assert.That(replay.Accepted, Is.False);
            Assert.That(replay.RejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.CommitmentCommandRejected));
            Assert.That(replay.CommitmentRejectionReason,
                Is.EqualTo(TurnCardCommitmentRejectionReason.CardNotEligible));
            Assert.That(replay.Events, Is.Empty);
            Assert.That(authority.TurnIndex, Is.EqualTo(2));
            Assert.That(authority.TurnSlots, Has.Count.EqualTo(1));
            Assert.That(configuration.CardInstances, Has.All.Matches<CardInstance>(card =>
                card.State.Equals(statesBefore[card.Id])));
        }

        [Test]
        public void NoCardsRemaining_RecordsPassesForEveryRemainingTurn()
        {
            var fixture = CreateOneCardPerSideFixture();
            var authority = fixture.Authority;
            Assert.That(authority.TryCommitCard(fixture.Red.OwnerId, fixture.Red.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(fixture.Blue.OwnerId, fixture.Blue.Id).Accepted, Is.True);
            ResolveCurrentTurnWithHold(authority);

            for (var turnIndex = 2; turnIndex <= RoundResolutionAuthority.TurnsPerRound; turnIndex++)
            {
                var redPass = authority.TryPass(fixture.Red.OwnerId);
                Assert.That(redPass.Accepted, Is.True);
                var bluePass = authority.TryPass(fixture.Blue.OwnerId);
                Assert.That(bluePass.Accepted, Is.True);
                var slot = authority.TurnSlots[turnIndex - 1];
                Assert.That(slot.TurnIndex, Is.EqualTo(turnIndex));
                Assert.That(slot.Assignments, Has.Count.EqualTo(2));
                Assert.That(slot.Assignments, Has.All.Matches<TurnCardAssignment>(assignment =>
                    assignment.PassedWithEmptyHand && !assignment.CardInstanceId.HasValue));
            }

            Assert.That(authority.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            Assert.That(authority.TurnSlots, Has.Count.EqualTo(RoundResolutionAuthority.TurnsPerRound));
        }

        [Test]
        public void IndicesSlotsAndBoundaryEvents_ReplayDeterministically()
        {
            var first = RunDeterministicRound();
            var second = RunDeterministicRound();

            Assert.That(second.RoundIndex, Is.EqualTo(first.RoundIndex));
            Assert.That(second.TurnIndex, Is.EqualTo(first.TurnIndex));
            Assert.That(second.Phase, Is.EqualTo(first.Phase));
            Assert.That(second.TieBreakerSide, Is.EqualTo(first.TieBreakerSide));
            Assert.That(second.Slots, Is.EqualTo(first.Slots));
            Assert.That(second.EventKinds, Is.EqualTo(first.EventKinds));
            Assert.That(second.EventRoundIndices, Is.EqualTo(first.EventRoundIndices));
            Assert.That(second.EventTurnIndices, Is.EqualTo(first.EventTurnIndices));
        }

        [Test]
        public void EndOfRound_RejectsFurtherCommitmentOrTurnCommandsWithoutMutation()
        {
            var fixture = CreateOneCardPerSideFixture();
            var authority = fixture.Authority;
            Assert.That(authority.TryCommitCard(fixture.Red.OwnerId, fixture.Red.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(fixture.Blue.OwnerId, fixture.Blue.Id).Accepted, Is.True);
            ResolveCurrentTurnWithHold(authority);
            for (var turn = 2; turn <= RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                Assert.That(authority.TryPass(fixture.Red.OwnerId).Accepted, Is.True);
                Assert.That(authority.TryPass(fixture.Blue.OwnerId).Accepted, Is.True);
            }

            var slots = authority.TurnSlots.ToArray();
            var redState = fixture.Red.State;
            AssertRejectedAfterRound(authority.TryPass(fixture.Red.OwnerId));
            AssertRejectedAfterRound(authority.TryPerformHold(fixture.Red.Id));
            Assert.That(authority.TurnSlots, Is.EqualTo(slots));
            Assert.That(fixture.Red.State, Is.EqualTo(redState));
        }

        [Test]
        public void TurnSlot_RequiresOneOwnedAssignmentPerHeroAndCopiesDeterministicCollection()
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var redHeroId = participants.GetHeroesForTeam(
                new TeamId(PlaceholderOneVsOneParticipants.RedTeamIdValue)).Single().Id;
            var blueHeroId = participants.GetHeroesForTeam(
                new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue)).Single().Id;
            var definition = new CardDefinitionReference(new CardDefinitionId("slot-card"), "rev-1");
            var redCard = CardInstance.CreateInHand(
                new CardInstanceId("red-card"), definition, redHeroId);
            var blueCard = CardInstance.CreateInHand(
                new CardInstanceId("blue-card"), definition, blueHeroId);
            var cards = new[] { redCard, blueCard };
            var mutableAssignments = new[]
            {
                new TurnCardAssignment(redHeroId, redCard.Id, false),
                new TurnCardAssignment(blueHeroId, null, true)
            };

            var slot = new RoundTurnSlot(1, 1, mutableAssignments, cards, participants);
            mutableAssignments[0] = new TurnCardAssignment(redHeroId, null, true);

            Assert.That(slot.Assignments.Select(assignment => assignment.HeroId.Value),
                Is.EqualTo(new[] { redHeroId.Value, blueHeroId.Value }
                    .OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(slot.Assignments.Single(assignment => assignment.HeroId.Equals(redHeroId))
                .CardInstanceId, Is.EqualTo(redCard.Id));
            var redAssignment = slot.Assignments.Single(assignment => assignment.HeroId.Equals(redHeroId));
            Assert.That(redAssignment.GetControllingPlayerId(participants),
                Is.EqualTo(participants.GetPlayerForHero(redHeroId).Id));
            Assert.That(redAssignment.GetTeamSide(participants), Is.EqualTo(TeamSide.Red));
            Assert.That(slot.Assignments, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<TurnCardAssignment>>());

            Assert.Throws<ArgumentException>(() => new RoundTurnSlot(
                1, 1,
                new[] { new TurnCardAssignment(redHeroId, redCard.Id, false) },
                cards,
                participants));
            Assert.Throws<ArgumentException>(() => new RoundTurnSlot(
                1, 1,
                new[]
                {
                    new TurnCardAssignment(redHeroId, redCard.Id, false),
                    new TurnCardAssignment(redHeroId, null, true)
                },
                cards,
                participants));
            Assert.Throws<ArgumentException>(() => new RoundTurnSlot(
                1, 1,
                new[]
                {
                    new TurnCardAssignment(redHeroId, blueCard.Id, false),
                    new TurnCardAssignment(blueHeroId, null, true)
                },
                cards,
                participants));
            Assert.Throws<ArgumentException>(() => new TurnCardAssignment(
                redHeroId, redCard.Id, true));
        }

        [Test]
        public void ExistingMovementAndAttackCommands_ResolveInsideSuccessiveRoundTurns()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var board = new AdjacentMoveAuthority(
                new[]
                {
                    new HexCoordinate(-1, 0),
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    new HexCoordinate(2, 0)
                },
                new[]
                {
                    new HeroOccupancy(configuration.RedHand.Instances[0].OwnerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(configuration.BlueHand.Instances[0].OwnerId, new HexCoordinate(1, 0))
                });
            var authority = new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                configuration.TieBreakerState,
                1,
                RoundResolutionComposition.ForCardActionsOnly(new CardActionResolutionAuthority(board)));

            var redMovementCard = configuration.RedHand.Instances[0];
            var blueFirstCard = configuration.BlueHand.Instances[0];
            Assert.That(authority.TryCommitCard(redMovementCard.OwnerId, redMovementCard.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(blueFirstCard.OwnerId, blueFirstCard.Id).Accepted, Is.True);
            var movementOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    configuration.CardDefinitions,
                configuration.CardInstances,
                redMovementCard.OwnerId,
                redMovementCard.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(
                    new ActionDefinitionId("secondary-movement")));
            Assert.That(authority.TryStartMovement(
                new ActionResolutionId("round-turn-1-movement"),
                movementOffer).Accepted, Is.True);
            var movementChoice = authority.CurrentTurnAuthority.PendingMovementChoice;
            Assert.That(authority.TryAnswerMovementChoice(MovementChoiceCommand.Stop(
                movementChoice.ResolutionId,
                movementChoice.ChoiceId)).Accepted, Is.True);
            Assert.That(authority.TryPerformHold(blueFirstCard.Id).Accepted, Is.True);
            Assert.That(authority.TurnIndex, Is.EqualTo(2));

            // Turn 2 is the hands' one cross-hero Initiative tie, Red Bulwark against Blue Sentinel at
            // 11. Milestone 6.8a replaced Red Bulwark's printed Attack with its showcase Skill and
            // 6.6a replaced Blue Sentinel's, so neither card of that pair prints an Attack and the
            // tied turn resolves through p.12's Hold on both sides. What turn 2 is here for survives
            // that unchanged: the tie, and the deferred flip after the resolving card.
            var redTiedCard = configuration.RedHand.Instances[1];
            var blueTiedCard = configuration.BlueHand.Instances[1];
            Assert.That(authority.TryCommitCard(redTiedCard.OwnerId, redTiedCard.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(blueTiedCard.OwnerId, blueTiedCard.Id).Accepted, Is.True);
            Assert.That(authority.CurrentTurnAuthority.SelectedActor.CardInstanceId,
                Is.EqualTo(redTiedCard.Id),
                "The revealed side acts first on a tie.");
            Assert.That(authority.TryPerformHold(redTiedCard.Id).Accepted, Is.True);
            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue),
                "Resolving the tied card flips the Tie Breaker before the next actor is selected.");
            Assert.That(authority.CurrentTurnAuthority.SelectedActor.CardInstanceId,
                Is.EqualTo(blueTiedCard.Id));
            Assert.That(authority.TryPerformHold(blueTiedCard.Id).Accepted, Is.True);
            Assert.That(authority.TurnIndex, Is.EqualTo(3));

            // Turn 3 carries the Attack that used to sit inside turn 2, from the untied pair that
            // still prints one: Red Pursuit at Initiative 7 against Blue Hammer at 6, so the Red hero
            // acts first and the Blue hero closes the turn by Holding.
            var redAttackCard = configuration.RedHand.Instances[3];
            var blueSecondCard = configuration.BlueHand.Instances[3];
            Assert.That(authority.TryCommitCard(redAttackCard.OwnerId, redAttackCard.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(blueSecondCard.OwnerId, blueSecondCard.Id).Accepted, Is.True);
            Assert.That(authority.CurrentTurnAuthority.SelectedActor.CardInstanceId,
                Is.EqualTo(redAttackCard.Id));
            var attackOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    configuration.CardDefinitions,
                configuration.CardInstances,
                redAttackCard.OwnerId,
                redAttackCard.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(
                    new ActionDefinitionId("primary-attack")));
            Assert.That(authority.TryStartAttack(
                new ActionResolutionId("round-turn-3-attack"),
                attackOffer).Accepted, Is.True);
            var targetChoice = authority.CurrentTurnAuthority.PendingAttackTargetChoice;
            Assert.That(authority.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                blueSecondCard.OwnerId)).Accepted, Is.True);
            var defenseChoice = authority.CurrentTurnAuthority.PendingDefenseChoice;
            Assert.That(authority.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.Decline(
                defenseChoice.ResolutionId,
                defenseChoice.ChoiceId)).Accepted, Is.True);
            Assert.That(authority.TryPerformHold(blueSecondCard.Id).Accepted, Is.True);

            Assert.That(authority.TurnIndex, Is.EqualTo(4));
            Assert.That(authority.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            Assert.That(authority.TurnSlots, Has.Count.EqualTo(3));
            Assert.That(redMovementCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(redTiedCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(blueTiedCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(redAttackCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(blueSecondCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void EndOfRound_UsesOrderedExplicitStagesRetrievesOwnersAndStartsFreshRound()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = CreateAuthority(configuration);
            var firstRoundSlots = new List<RoundTurnSlot>();
            for (var turn = 0; turn < RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                var red = configuration.RedHand.Instances[turn];
                var blue = configuration.BlueHand.Instances[turn];
                Assert.That(authority.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
                Assert.That(authority.TryCommitCard(blue.OwnerId, blue.Id).Accepted, Is.True);
                ResolveCurrentTurnWithHold(authority);
            }

            firstRoundSlots.AddRange(authority.TurnSlots);
            var eventKinds = new List<RoundResolutionEventKind>();
            var stageOutcomes = new List<EndOfRoundStageOutcome>();
            while (authority.Phase == RoundResolutionPhase.EndOfRound)
            {
                var result = authority.TryAdvanceEndOfRound();
                Assert.That(result.Accepted, Is.True);
                eventKinds.AddRange(result.Events.Select(ruleEvent => ruleEvent.Kind));
                stageOutcomes.AddRange(result.Events
                    .Where(ruleEvent => ruleEvent.Kind == RoundResolutionEventKind.EndOfRoundStageResolved)
                    .Select(ruleEvent => new EndOfRoundStageOutcome(
                        ruleEvent.EndOfRoundStage.Value,
                        ruleEvent.EndOfRoundOutcomeKind.Value)));
            }

            var orderedStages = Enum.GetValues(typeof(EndOfRoundStage))
                .Cast<EndOfRoundStage>()
                .Where(stage => stage != EndOfRoundStage.Complete)
                .ToArray();
            Assert.That(stageOutcomes.Select(outcome => outcome.Stage), Is.EqualTo(orderedStages));
            Assert.That(stageOutcomes.Single(outcome =>
                    outcome.Stage == EndOfRoundStage.RetrieveCardsAndCancelActiveEffects).Kind,
                Is.EqualTo(EndOfRoundStageOutcomeKind.Completed));
            Assert.That(stageOutcomes.Where(outcome =>
                    outcome.Stage != EndOfRoundStage.RetrieveCardsAndCancelActiveEffects)
                .Select(outcome => outcome.Kind),
                Is.All.EqualTo(EndOfRoundStageOutcomeKind.NotApplicable));
            Assert.That(eventKinds.Count(kind => kind == RoundResolutionEventKind.CardRetrieved), Is.EqualTo(8));
            Assert.That(eventKinds.Count(kind =>
                kind == RoundResolutionEventKind.ActiveEffectCancellationCheckpoint), Is.EqualTo(1));
            Assert.That(eventKinds.TakeLast(2), Is.EqualTo(new[]
            {
                RoundResolutionEventKind.RoundCompleted,
                RoundResolutionEventKind.NextRoundStarted
            }));
            Assert.That(configuration.RedHand.Instances.Take(4).Select(card => card.State),
                Is.All.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(configuration.BlueHand.Instances.Take(4).Select(card => card.State),
                Is.All.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(authority.RoundIndex, Is.EqualTo(2));
            Assert.That(authority.TurnIndex, Is.EqualTo(1));
            Assert.That(authority.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            Assert.That(authority.TurnSlots, Is.Empty);
            Assert.That(authority.CompletedRounds, Has.Count.EqualTo(1));
            Assert.That(authority.CompletedRounds[0].TurnSlots, Is.EqualTo(firstRoundSlots));
            Assert.That(authority.CompletedRounds[0].EndOfRoundOutcomes.Select(outcome => outcome.Stage),
                Is.EqualTo(orderedStages));

            var frozenAssignments = authority.CompletedRounds[0].TurnSlots[0].Assignments
                .Select(assignment => assignment.CardInstanceId).ToArray();
            Assert.That(authority.TryCommitCard(
                configuration.RedHand.Instances[0].OwnerId,
                configuration.RedHand.Instances[0].Id).Accepted, Is.True);
            Assert.That(authority.CompletedRounds[0].TurnSlots[0].Assignments
                .Select(assignment => assignment.CardInstanceId), Is.EqualTo(frozenAssignments));
        }

        [Test]
        public void EndOfRound_PausedHandlerRetainsCursorAndMissingAvailableHandlerRejectsWithoutMutation()
        {
            var fixture = CreateOneCardPerSideFixture(
                new EndOfRoundStageAvailability(new[] { EndOfRoundStage.MinionBattle }),
                new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>
                {
                    { EndOfRoundStage.MinionBattle, new PauseOnceHandler() }
                });
            CompleteOneCardRound(fixture);

            var paused = fixture.Authority.TryAdvanceEndOfRound();
            Assert.That(paused.Accepted, Is.True);
            Assert.That(paused.Events.Single().EndOfRoundOutcomeKind,
                Is.EqualTo(EndOfRoundStageOutcomeKind.Paused));
            Assert.That(fixture.Authority.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.MinionBattle));
            Assert.That(fixture.Authority.EndOfRoundOutcomes, Is.Empty);

            var resumed = fixture.Authority.TryAdvanceEndOfRound();
            Assert.That(resumed.Accepted, Is.True);
            Assert.That(fixture.Authority.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));
            Assert.That(fixture.Authority.EndOfRoundOutcomes.Single().Kind,
                Is.EqualTo(EndOfRoundStageOutcomeKind.Completed));

            var missingFixture = CreateOneCardPerSideFixture(
                new EndOfRoundStageAvailability(new[] { EndOfRoundStage.MinionBattle }),
                new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>());
            CompleteOneCardRound(missingFixture);
            var slots = missingFixture.Authority.TurnSlots.ToArray();
            var rejected = missingFixture.Authority.TryAdvanceEndOfRound();
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.MissingRequiredEndOfRoundHandler));
            Assert.That(rejected.Events, Is.Empty);
            Assert.That(missingFixture.Authority.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.MinionBattle));
            Assert.That(missingFixture.Authority.TurnSlots, Is.EqualTo(slots));
        }

        [Test]
        public void EndOfRound_GameEndingHandlerStopsCursorAndLaterCommands()
        {
            var fixture = CreateOneCardPerSideFixture(
                new EndOfRoundStageAvailability(new[] { EndOfRoundStage.MinionBattle }),
                new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>
                {
                    { EndOfRoundStage.MinionBattle, new GameEndingHandler() }
                });
            CompleteOneCardRound(fixture);

            var ended = fixture.Authority.TryAdvanceEndOfRound();

            Assert.That(ended.Accepted, Is.True);
            Assert.That(ended.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                RoundResolutionEventKind.EndOfRoundStageResolved,
                RoundResolutionEventKind.GameEnded
            }));
            Assert.That(fixture.Authority.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(fixture.Authority.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.MinionBattle));
            Assert.That(fixture.Authority.CompletedRounds, Is.Empty);
            var rejected = fixture.Authority.TryAdvanceEndOfRound();
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.GameAlreadyEnded));
        }

        /// <summary>
        /// Milestone 6.13. Rulebook p.8 prints Retrieve Cards after the Lane Push, so a push that stops
        /// on a choice must hold the whole round there: no card comes back early, and the one
        /// `CardsRetrieved` checkpoint does not run early either. Resuming then does both exactly once.
        ///
        /// The pause is staged at the Lane-Push stage through a handler rather than through a real push,
        /// because what is under test is the stage cursor's promise rather than p.8's transition, which
        /// <c>LanePushTransitionTests</c> owns.
        /// </summary>
        [Test]
        public void EndOfRound_APauseBeforeRetrieval_RetrievesNothingUntilItResumesAndThenRetrievesOnce()
        {
            var fixture = CreateOneCardPerSideFixture(
                new EndOfRoundStageAvailability(new[]
                {
                    EndOfRoundStage.ResolveRequestedLanePush,
                    EndOfRoundStage.RetrieveCardsAndCancelActiveEffects
                }),
                new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>
                {
                    { EndOfRoundStage.ResolveRequestedLanePush, new PauseOnceHandler() },
                    { EndOfRoundStage.RetrieveCardsAndCancelActiveEffects, new CardRetrievalHandler() }
                });
            CompleteOneCardRound(fixture);
            AdvanceToStage(fixture, EndOfRoundStage.ResolveRequestedLanePush);

            var paused = fixture.Authority.TryAdvanceEndOfRound();

            Assert.That(paused.Accepted, Is.True);
            Assert.That(fixture.Authority.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.ResolveRequestedLanePush));
            Assert.That(paused.Events.Any(ruleEvent =>
                    ruleEvent.Kind == RoundResolutionEventKind.CardRetrieved ||
                    ruleEvent.Kind == RoundResolutionEventKind.ActiveEffectCancellationCheckpoint),
                Is.False,
                "A stopped stage cannot reach past itself into the stage p.8 prints after it.");
            Assert.That(fixture.Red.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(fixture.Blue.State.Location, Is.EqualTo(CardLocation.TurnArea));

            var resumed = fixture.Authority.TryAdvanceEndOfRound();
            Assert.That(resumed.Accepted, Is.True);
            Assert.That(fixture.Authority.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RetrieveCardsAndCancelActiveEffects));
            Assert.That(resumed.Events.Any(ruleEvent =>
                    ruleEvent.Kind == RoundResolutionEventKind.CardRetrieved), Is.False,
                "Resuming finishes the stage that stopped; it does not run the next one with it.");

            var retrieval = fixture.Authority.TryAdvanceEndOfRound();

            Assert.That(retrieval.Accepted, Is.True);
            Assert.That(
                retrieval.Events.Count(ruleEvent => ruleEvent.Kind == RoundResolutionEventKind.CardRetrieved),
                Is.EqualTo(2), "Each of the two played cards comes back exactly once.");
            Assert.That(
                retrieval.Events.Count(ruleEvent =>
                    ruleEvent.Kind == RoundResolutionEventKind.ActiveEffectCancellationCheckpoint),
                Is.EqualTo(1), "and the round crosses its one cancellation boundary exactly once.");
            Assert.That(fixture.Red.State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(fixture.Blue.State, Is.EqualTo(CardStateFacts.InitialInHand()));
        }

        /// <summary>
        /// Milestone 6.13, the other half: p.8's push can end the game, and a game that ended does not
        /// go on to retrieve cards for a round nobody will play.
        /// </summary>
        [Test]
        public void EndOfRound_AGameEndingStageBeforeRetrieval_RetrievesNoCardAtAll()
        {
            var fixture = CreateOneCardPerSideFixture(
                new EndOfRoundStageAvailability(new[]
                {
                    EndOfRoundStage.ResolveRequestedLanePush,
                    EndOfRoundStage.RetrieveCardsAndCancelActiveEffects
                }),
                new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>
                {
                    { EndOfRoundStage.ResolveRequestedLanePush, new GameEndingHandler() },
                    { EndOfRoundStage.RetrieveCardsAndCancelActiveEffects, new CardRetrievalHandler() }
                });
            CompleteOneCardRound(fixture);
            AdvanceToStage(fixture, EndOfRoundStage.ResolveRequestedLanePush);

            var ended = fixture.Authority.TryAdvanceEndOfRound();

            Assert.That(ended.Accepted, Is.True);
            Assert.That(fixture.Authority.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(ended.Events.Any(ruleEvent =>
                    ruleEvent.Kind == RoundResolutionEventKind.CardRetrieved), Is.False);
            Assert.That(fixture.Red.State.Location, Is.EqualTo(CardLocation.TurnArea),
                "The cards stay where the game ended, which is the only honest record of where it ended.");
            Assert.That(fixture.Blue.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(fixture.Authority.TryAdvanceEndOfRound().Accepted, Is.False);
            Assert.That(fixture.Authority.CompletedRounds, Is.Empty);
        }

        [Test]
        public void TwoRounds_ReusesRetrievedCardsAndPreservesTieBreakerDeterministically()
        {
            var first = RunTwoRoundReplay();
            var second = RunTwoRoundReplay();

            Assert.That(second, Is.EqualTo(first));
        }

        private static RoundResolutionAuthority CreateAuthority(
            PlaceholderOneVsOneTestConfiguration configuration)
        {
            return new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                configuration.TieBreakerState,
                1,
                RoundResolutionComposition.ForCardActionsOnly(null));
        }

        private static void ResolveCurrentTurnWithHold(RoundResolutionAuthority authority)
        {
            while (authority.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                Assert.That(authority.CurrentTurnAuthority.SelectedActor.IsDefined, Is.True);
                Assert.That(authority.TryPerformHold(
                    authority.CurrentTurnAuthority.SelectedActor.CardInstanceId).Accepted, Is.True);
            }
        }

        private static OneCardFixture CreateOneCardPerSideFixture(
            EndOfRoundStageAvailability capabilities = null,
            IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> handlers = null)
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var redDefinition = CreateDefinition("red", 7);
            var blueDefinition = CreateDefinition("blue", 4);
            var red = CardInstance.CreateInHand(
                new CardInstanceId("red-card"),
                redDefinition.Reference,
                participants.GetHeroesForTeam(new TeamId(PlaceholderOneVsOneParticipants.RedTeamIdValue)).Single().Id);
            var blue = CardInstance.CreateInHand(
                new CardInstanceId("blue-card"),
                blueDefinition.Reference,
                participants.GetHeroesForTeam(new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue)).Single().Id);
            var authority = new RoundResolutionAuthority(
                new[] { redDefinition, blueDefinition },
                new[] { red, blue },
                participants,
                new TieBreakerState(TeamSide.Red),
                1,
                new RoundResolutionComposition(
                    null, null, null, null, null, null, capabilities, handlers));
            return new OneCardFixture(red, blue, authority);
        }

        /// <summary>
        /// Advances the end-of-round cursor to the named stage, crossing whatever earlier stages this
        /// fixture's availability declares not applicable. Milestone 6.13 needs it because the stage
        /// under test is the one before retrieval rather than the first one.
        /// </summary>
        private static void AdvanceToStage(OneCardFixture fixture, EndOfRoundStage stage)
        {
            while (fixture.Authority.CurrentEndOfRoundStage != stage)
            {
                Assert.That(fixture.Authority.CurrentEndOfRoundStage.HasValue, Is.True,
                    "The round left end of round before reaching " + stage + ".");
                Assert.That(fixture.Authority.TryAdvanceEndOfRound().Accepted, Is.True);
            }
        }

        private static void CompleteOneCardRound(OneCardFixture fixture)
        {
            Assert.That(fixture.Authority.TryCommitCard(fixture.Red.OwnerId, fixture.Red.Id).Accepted, Is.True);
            Assert.That(fixture.Authority.TryCommitCard(fixture.Blue.OwnerId, fixture.Blue.Id).Accepted, Is.True);
            ResolveCurrentTurnWithHold(fixture.Authority);
            for (var turn = 2; turn <= RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                Assert.That(fixture.Authority.TryPass(fixture.Red.OwnerId).Accepted, Is.True);
                Assert.That(fixture.Authority.TryPass(fixture.Blue.OwnerId).Accepted, Is.True);
            }
        }

        private static string RunTwoRoundReplay()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = CreateAuthority(configuration);
            var facts = new List<string>();
            for (var round = 1; round <= 2; round++)
            {
                for (var turn = 0; turn < RoundResolutionAuthority.TurnsPerRound; turn++)
                {
                    var red = configuration.RedHand.Instances[turn];
                    var blue = configuration.BlueHand.Instances[turn];
                    facts.AddRange(authority.TryCommitCard(red.OwnerId, red.Id).Events
                        .Select(ruleEvent => $"{ruleEvent.RoundIndex}:{ruleEvent.TurnIndex}:{ruleEvent.Kind}"));
                    facts.AddRange(authority.TryCommitCard(blue.OwnerId, blue.Id).Events
                        .Select(ruleEvent => $"{ruleEvent.RoundIndex}:{ruleEvent.TurnIndex}:{ruleEvent.Kind}"));
                    while (authority.Phase == RoundResolutionPhase.ResolvingTurn)
                    {
                        facts.AddRange(authority.TryPerformHold(
                                authority.CurrentTurnAuthority.SelectedActor.CardInstanceId).Events
                            .Select(ruleEvent => $"{ruleEvent.RoundIndex}:{ruleEvent.TurnIndex}:{ruleEvent.Kind}"));
                    }
                }

                while (authority.Phase == RoundResolutionPhase.EndOfRound)
                {
                    facts.AddRange(authority.TryAdvanceEndOfRound().Events.Select(ruleEvent =>
                        $"{ruleEvent.RoundIndex}:{ruleEvent.TurnIndex}:{ruleEvent.Kind}:" +
                        $"{ruleEvent.EndOfRoundStage}:{ruleEvent.EndOfRoundOutcomeKind}"));
                }
            }

            return string.Join("|", facts) + $"|tie={authority.TieBreakerState.RevealedSide}";
        }

        private static CardDefinition CreateDefinition(string id, int initiative)
        {
            return new CardDefinition(
                new CardDefinitionId(id),
                "round-test-rev-1",
                $"TEST - {id}",
                new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated);
        }

        private static ReplayFacts RunDeterministicRound()
        {
            var fixture = CreateOneCardPerSideFixture();
            var authority = fixture.Authority;
            var events = new List<RoundResolutionEvent>();
            events.AddRange(authority.TryCommitCard(fixture.Blue.OwnerId, fixture.Blue.Id).Events);
            events.AddRange(authority.TryCommitCard(fixture.Red.OwnerId, fixture.Red.Id).Events);
            while (authority.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                events.AddRange(authority.TryPerformHold(
                    authority.CurrentTurnAuthority.SelectedActor.CardInstanceId).Events);
            }

            for (var turn = 2; turn <= RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                events.AddRange(authority.TryPass(fixture.Blue.OwnerId).Events);
                events.AddRange(authority.TryPass(fixture.Red.OwnerId).Events);
            }

            return new ReplayFacts(
                authority.RoundIndex,
                authority.TurnIndex,
                authority.Phase,
                authority.TieBreakerState.RevealedSide,
                authority.TurnSlots.Select(slot =>
                    $"{slot.RoundIndex}:{slot.TurnIndex}:" +
                    string.Join(":" , slot.Assignments.Select(assignment =>
                        $"{assignment.HeroId.Value}={assignment.CardInstanceId?.Value ?? "PASS"}")))
                    .ToArray(),
                events.Select(ruleEvent => ruleEvent.Kind).ToArray(),
                events.Select(ruleEvent => ruleEvent.RoundIndex).ToArray(),
                events.Select(ruleEvent => ruleEvent.TurnIndex).ToArray());
        }

        private static void AssertRejectedAfterRound(RoundResolutionResult result)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.RoundAlreadyEnded));
            Assert.That(result.Events, Is.Empty);
        }

        private sealed class OneCardFixture
        {
            public OneCardFixture(
                CardInstance red,
                CardInstance blue,
                RoundResolutionAuthority authority)
            {
                Red = red;
                Blue = blue;
                Authority = authority;
            }

            public CardInstance Red { get; }

            public CardInstance Blue { get; }

            public RoundResolutionAuthority Authority { get; }
        }

        private sealed class ReplayFacts
        {
            public ReplayFacts(
                int roundIndex,
                int turnIndex,
                RoundResolutionPhase phase,
                TeamSide tieBreakerSide,
                string[] slots,
                RoundResolutionEventKind[] eventKinds,
                int[] eventRoundIndices,
                int[] eventTurnIndices)
            {
                RoundIndex = roundIndex;
                TurnIndex = turnIndex;
                Phase = phase;
                TieBreakerSide = tieBreakerSide;
                Slots = slots;
                EventKinds = eventKinds;
                EventRoundIndices = eventRoundIndices;
                EventTurnIndices = eventTurnIndices;
            }

            public int RoundIndex { get; }

            public int TurnIndex { get; }

            public RoundResolutionPhase Phase { get; }

            public TeamSide TieBreakerSide { get; }

            public string[] Slots { get; }

            public RoundResolutionEventKind[] EventKinds { get; }

            public int[] EventRoundIndices { get; }

            public int[] EventTurnIndices { get; }
        }

        private sealed class PauseOnceHandler : IEndOfRoundStageHandler
        {
            private bool paused;

            public EndOfRoundStageResolution TryResolve(EndOfRoundStageContext context)
            {
                if (!paused)
                {
                    paused = true;
                    return EndOfRoundStageResolution.Paused();
                }

                return EndOfRoundStageResolution.Completed();
            }
        }

        private sealed class GameEndingHandler : IEndOfRoundStageHandler
        {
            public EndOfRoundStageResolution TryResolve(EndOfRoundStageContext context)
            {
                return EndOfRoundStageResolution.GameEnded();
            }
        }
    }
}
