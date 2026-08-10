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
                allEvents.AddRange(authority.TryCommitCard(TeamSide.Red, red.Id).Events);
                allEvents.AddRange(authority.TryCommitCard(TeamSide.Blue, blue.Id).Events);

                Assert.That(authority.TurnSlots, Has.Count.EqualTo(turnIndex));
                var slot = authority.TurnSlots[turnIndex - 1];
                Assert.That(slot.RoundIndex, Is.EqualTo(1));
                Assert.That(slot.TurnIndex, Is.EqualTo(turnIndex));
                Assert.That(slot.RedCardInstanceId, Is.EqualTo(red.Id));
                Assert.That(slot.BlueCardInstanceId, Is.EqualTo(blue.Id));
                Assert.That(slot.RedPassed || slot.BluePassed, Is.False);

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
            Assert.That(authority.TryCommitCard(TeamSide.Red, red.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(TeamSide.Blue, blue.Id).Accepted, Is.True);
            ResolveCurrentTurnWithHold(authority);
            var statesBefore = configuration.CardInstances.ToDictionary(card => card.Id, card => card.State);

            var replay = authority.TryCommitCard(TeamSide.Red, red.Id);

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
            Assert.That(authority.TryCommitCard(TeamSide.Red, fixture.Red.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(TeamSide.Blue, fixture.Blue.Id).Accepted, Is.True);
            ResolveCurrentTurnWithHold(authority);

            for (var turnIndex = 2; turnIndex <= RoundResolutionAuthority.TurnsPerRound; turnIndex++)
            {
                var redPass = authority.TryPass(TeamSide.Red);
                Assert.That(redPass.Accepted, Is.True);
                var bluePass = authority.TryPass(TeamSide.Blue);
                Assert.That(bluePass.Accepted, Is.True);
                var slot = authority.TurnSlots[turnIndex - 1];
                Assert.That(slot.TurnIndex, Is.EqualTo(turnIndex));
                Assert.That(slot.RedPassed, Is.True);
                Assert.That(slot.BluePassed, Is.True);
                Assert.That(slot.RedCardInstanceId, Is.Null);
                Assert.That(slot.BlueCardInstanceId, Is.Null);
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
            Assert.That(authority.TryCommitCard(TeamSide.Red, fixture.Red.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(TeamSide.Blue, fixture.Blue.Id).Accepted, Is.True);
            ResolveCurrentTurnWithHold(authority);
            for (var turn = 2; turn <= RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                Assert.That(authority.TryPass(TeamSide.Red).Accepted, Is.True);
                Assert.That(authority.TryPass(TeamSide.Blue).Accepted, Is.True);
            }

            var slots = authority.TurnSlots.ToArray();
            var redState = fixture.Red.State;
            AssertRejectedAfterRound(authority.TryPass(TeamSide.Red));
            AssertRejectedAfterRound(authority.TryPerformHold(fixture.Red.Id));
            Assert.That(authority.TurnSlots, Is.EqualTo(slots));
            Assert.That(fixture.Red.State, Is.EqualTo(redState));
        }

        [Test]
        public void TurnSlot_RequiresExactlyOneDefinedCardOrPassPerSide()
        {
            var redCardId = new CardInstanceId("red-card");
            var blueCardId = new CardInstanceId("blue-card");

            Assert.Throws<ArgumentException>(() => new RoundTurnSlot(
                1, 1, null, false, blueCardId, false));
            Assert.Throws<ArgumentException>(() => new RoundTurnSlot(
                1, 1, redCardId, true, blueCardId, false));
            Assert.Throws<ArgumentException>(() => new RoundTurnSlot(
                1, 1, redCardId, false, default(CardInstanceId), true));
            Assert.DoesNotThrow(() => new RoundTurnSlot(
                1, 1, redCardId, false, null, true));
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
                cardActions: new CardActionResolutionAuthority(board));

            var redMovementCard = configuration.RedHand.Instances[0];
            var blueFirstCard = configuration.BlueHand.Instances[0];
            Assert.That(authority.TryCommitCard(TeamSide.Red, redMovementCard.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(TeamSide.Blue, blueFirstCard.Id).Accepted, Is.True);
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

            var redAttackCard = configuration.RedHand.Instances[1];
            var blueSecondCard = configuration.BlueHand.Instances[1];
            Assert.That(authority.TryCommitCard(TeamSide.Red, redAttackCard.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(TeamSide.Blue, blueSecondCard.Id).Accepted, Is.True);
            var attackOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    configuration.CardDefinitions,
                configuration.CardInstances,
                redAttackCard.OwnerId,
                redAttackCard.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(
                    new ActionDefinitionId("primary-attack")));
            Assert.That(authority.TryStartAttack(
                new ActionResolutionId("round-turn-2-attack"),
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
            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(authority.TryPerformHold(blueSecondCard.Id).Accepted, Is.True);

            Assert.That(authority.TurnIndex, Is.EqualTo(3));
            Assert.That(authority.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            Assert.That(authority.TurnSlots, Has.Count.EqualTo(2));
            Assert.That(redMovementCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(redAttackCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        private static RoundResolutionAuthority CreateAuthority(
            PlaceholderOneVsOneTestConfiguration configuration)
        {
            return new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                configuration.TieBreakerState);
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

        private static OneCardFixture CreateOneCardPerSideFixture()
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
                new TieBreakerState(TeamSide.Red));
            return new OneCardFixture(red, blue, authority);
        }

        private static CardDefinition CreateDefinition(string id, int initiative)
        {
            return new CardDefinition(
                new CardDefinitionId(id),
                "round-test-rev-1",
                $"TEST - {id}",
                new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture);
        }

        private static ReplayFacts RunDeterministicRound()
        {
            var fixture = CreateOneCardPerSideFixture();
            var authority = fixture.Authority;
            var events = new List<RoundResolutionEvent>();
            events.AddRange(authority.TryCommitCard(TeamSide.Blue, fixture.Blue.Id).Events);
            events.AddRange(authority.TryCommitCard(TeamSide.Red, fixture.Red.Id).Events);
            while (authority.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                events.AddRange(authority.TryPerformHold(
                    authority.CurrentTurnAuthority.SelectedActor.CardInstanceId).Events);
            }

            for (var turn = 2; turn <= RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                events.AddRange(authority.TryPass(TeamSide.Blue).Events);
                events.AddRange(authority.TryPass(TeamSide.Red).Events);
            }

            return new ReplayFacts(
                authority.RoundIndex,
                authority.TurnIndex,
                authority.Phase,
                authority.TieBreakerState.RevealedSide,
                authority.TurnSlots.Select(slot =>
                    $"{slot.RoundIndex}:{slot.TurnIndex}:" +
                    $"{slot.RedCardInstanceId?.Value ?? "PASS"}:" +
                    $"{slot.BlueCardInstanceId?.Value ?? "PASS"}").ToArray(),
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
    }
}
