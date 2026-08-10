using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TurnCardCommitmentAuthorityTests
    {
        [Test]
        public void CommitOneCardPerSide_PlaysFacedownThenRevealsTogether()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = new TurnCardCommitmentAuthority();
            var redCard = configuration.RedHand.Instances[0];
            var blueCard = configuration.BlueHand.Instances[1];

            var redResult = authority.TryCommitCard(
                TeamSide.Red,
                redCard.Id,
                configuration.CardInstances,
                configuration.Participants);

            Assert.That(redResult.Accepted, Is.True);
            Assert.That(redResult.Events.Select(ruleEvent => ruleEvent.Kind),
                Is.EqualTo(new[] { TurnCardCommitmentEventKind.CardCommittedFacedown }));
            Assert.That(redResult.Events, Has.All.Matches<TurnCardCommitmentEvent>(ruleEvent =>
                !ruleEvent.HasCardInstanceId));
            Assert.That(authority.GetStatus(TeamSide.Red),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(authority.GetRevealedCardInstanceId(TeamSide.Red), Is.Null);
            Assert.That(redCard.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));
            Assert.That(blueCard.State, Is.EqualTo(CardStateFacts.InitialInHand()));

            var blueResult = authority.TryCommitCard(
                TeamSide.Blue,
                blueCard.Id,
                configuration.CardInstances,
                configuration.Participants);

            Assert.That(blueResult.Accepted, Is.True);
            Assert.That(blueResult.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                TurnCardCommitmentEventKind.CardCommittedFacedown,
                TurnCardCommitmentEventKind.CardRevealed,
                TurnCardCommitmentEventKind.CardRevealed,
                TurnCardCommitmentEventKind.RevealCompleted
            }));
            Assert.That(blueResult.Events.Where(ruleEvent =>
                    ruleEvent.Kind == TurnCardCommitmentEventKind.CardRevealed)
                .Select(ruleEvent => ruleEvent.TeamSide),
                Is.EqualTo(new[] { TeamSide.Red, TeamSide.Blue }));
            Assert.That(authority.IsRevealed, Is.True);
            Assert.That(authority.GetRevealedCardInstanceId(TeamSide.Red), Is.EqualTo(redCard.Id));
            Assert.That(authority.GetRevealedCardInstanceId(TeamSide.Blue), Is.EqualTo(blueCard.Id));
            Assert.That(redCard.State, Is.EqualTo(PlayedUnresolved(CardFaceOrientation.FaceUp)));
            Assert.That(blueCard.State, Is.EqualTo(PlayedUnresolved(CardFaceOrientation.FaceUp)));
        }

        [Test]
        public void Commit_RejectsCrossSideIneligibleMissingAndRepeatedWithoutMutation()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = new TurnCardCommitmentAuthority();
            var redCard = configuration.RedHand.Instances[0];
            var blueCard = configuration.BlueHand.Instances[0];
            Assert.That(CardLifecycleAuthority.TryDiscardDefenseFromHand(configuration.RedHand.Instances[1]), Is.True);
            var statesBeforeRejections = configuration.CardInstances.ToDictionary(card => card.Id, card => card.State);

            AssertRejected(
                authority.TryCommitCard(
                    TeamSide.Red,
                    blueCard.Id,
                    configuration.CardInstances,
                    configuration.Participants),
                TurnCardCommitmentRejectionReason.CardNotOwnedBySide);
            AssertRejected(
                authority.TryCommitCard(
                    TeamSide.Red,
                    configuration.RedHand.Instances[1].Id,
                    configuration.CardInstances,
                    configuration.Participants),
                TurnCardCommitmentRejectionReason.CardNotEligible);
            AssertRejected(
                authority.TryCommitCard(
                    TeamSide.Red,
                    new CardInstanceId("missing-card"),
                    configuration.CardInstances,
                    configuration.Participants),
                TurnCardCommitmentRejectionReason.CardNotFound);
            Assert.That(configuration.CardInstances, Has.All.Matches<CardInstance>(card =>
                card.State.Equals(statesBeforeRejections[card.Id])));

            Assert.That(authority.TryCommitCard(
                TeamSide.Red,
                redCard.Id,
                configuration.CardInstances,
                configuration.Participants).Accepted, Is.True);
            var statesAfterCommit = configuration.CardInstances.ToDictionary(card => card.Id, card => card.State);
            AssertRejected(
                authority.TryCommitCard(
                    TeamSide.Red,
                    configuration.RedHand.Instances[1].Id,
                    configuration.CardInstances,
                    configuration.Participants),
                TurnCardCommitmentRejectionReason.SideAlreadyResponded);
            Assert.That(configuration.CardInstances, Has.All.Matches<CardInstance>(card =>
                card.State.Equals(statesAfterCommit[card.Id])));
        }

        [Test]
        public void Pass_IsAllowedOnlyWhenSideHasNoEligibleCardAndRevealsOtherCommitment()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = new TurnCardCommitmentAuthority();
            var blueCard = configuration.BlueHand.Instances[0];

            AssertRejected(
                authority.TryPass(
                    TeamSide.Red,
                    configuration.CardInstances,
                    configuration.Participants),
                TurnCardCommitmentRejectionReason.PassNotAllowed);

            foreach (var redCard in configuration.RedHand.Instances)
            {
                Assert.That(CardLifecycleAuthority.TryDiscardDefenseFromHand(redCard), Is.True);
            }

            Assert.That(authority.CanPass(
                TeamSide.Red,
                configuration.CardInstances,
                configuration.Participants), Is.True);
            var redPass = authority.TryPass(
                TeamSide.Red,
                configuration.CardInstances,
                configuration.Participants);
            Assert.That(redPass.Accepted, Is.True);
            Assert.That(authority.GetStatus(TeamSide.Red), Is.EqualTo(TurnCardCommitmentStatus.Passed));
            Assert.That(authority.IsRevealed, Is.False);

            var blueCommit = authority.TryCommitCard(
                TeamSide.Blue,
                blueCard.Id,
                configuration.CardInstances,
                configuration.Participants);
            Assert.That(blueCommit.Accepted, Is.True);
            Assert.That(blueCommit.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                TurnCardCommitmentEventKind.CardCommittedFacedown,
                TurnCardCommitmentEventKind.CardRevealed,
                TurnCardCommitmentEventKind.RevealCompleted
            }));
            Assert.That(authority.IsRevealed, Is.True);
            Assert.That(authority.GetStatus(TeamSide.Red), Is.EqualTo(TurnCardCommitmentStatus.Passed));
            Assert.That(authority.GetRevealedCardInstanceId(TeamSide.Red), Is.Null);
            Assert.That(blueCard.State, Is.EqualTo(PlayedUnresolved(CardFaceOrientation.FaceUp)));
        }

        [Test]
        public void ChangedCommittedCard_RejectsOtherResponseWithoutAdditionalMutation()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = new TurnCardCommitmentAuthority();
            var redCard = configuration.RedHand.Instances[0];
            var blueCard = configuration.BlueHand.Instances[0];

            Assert.That(authority.TryCommitCard(
                TeamSide.Red,
                redCard.Id,
                configuration.CardInstances,
                configuration.Participants).Accepted, Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(redCard), Is.True);
            var statesBeforeRejection = configuration.CardInstances.ToDictionary(card => card.Id, card => card.State);

            var result = authority.TryCommitCard(
                TeamSide.Blue,
                blueCard.Id,
                configuration.CardInstances,
                configuration.Participants);

            AssertRejected(result, TurnCardCommitmentRejectionReason.CommittedCardStateChanged);
            Assert.That(authority.CanPass(
                TeamSide.Blue,
                configuration.CardInstances,
                configuration.Participants), Is.False);
            Assert.That(configuration.CardInstances, Has.All.Matches<CardInstance>(card =>
                card.State.Equals(statesBeforeRejection[card.Id])));
        }

        [Test]
        public void SameCommitSequence_ReplaysWithIdenticalStatesAndOrderedEvents()
        {
            var first = RunCommitSequence();
            var second = RunCommitSequence();

            Assert.That(second.States, Is.EqualTo(first.States));
            Assert.That(second.EventKinds, Is.EqualTo(first.EventKinds));
            Assert.That(second.EventSides, Is.EqualTo(first.EventSides));
            Assert.That(second.EventCardIds, Is.EqualTo(first.EventCardIds));
        }

        private static CardStateFacts PlayedUnresolved(CardFaceOrientation orientation)
        {
            return new CardStateFacts(
                CardLocation.TurnArea,
                true,
                CardResolutionStatus.Unresolved,
                orientation);
        }

        private static void AssertRejected(
            TurnCardCommitmentResult result,
            TurnCardCommitmentRejectionReason reason)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(reason));
            Assert.That(result.Events, Is.Empty);
        }

        private static ReplayFacts RunCommitSequence()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var authority = new TurnCardCommitmentAuthority();
            var events = authority.TryCommitCard(
                    TeamSide.Blue,
                    configuration.BlueHand.Instances[1].Id,
                    configuration.CardInstances,
                    configuration.Participants)
                .Events
                .Concat(authority.TryCommitCard(
                    TeamSide.Red,
                    configuration.RedHand.Instances[0].Id,
                    configuration.CardInstances,
                    configuration.Participants).Events)
                .ToArray();
            return new ReplayFacts(
                configuration.CardInstances
                    .OrderBy(card => card.Id.Value)
                    .Select(card => card.State)
                    .ToArray(),
                events.Select(ruleEvent => ruleEvent.Kind).ToArray(),
                events.Select(ruleEvent => ruleEvent.TeamSide).ToArray(),
                events.Select(ruleEvent => ruleEvent.CardInstanceId).ToArray());
        }

        private sealed class ReplayFacts
        {
            public ReplayFacts(
                CardStateFacts[] states,
                TurnCardCommitmentEventKind[] eventKinds,
                TeamSide[] eventSides,
                CardInstanceId[] eventCardIds)
            {
                States = states;
                EventKinds = eventKinds;
                EventSides = eventSides;
                EventCardIds = eventCardIds;
            }

            public CardStateFacts[] States { get; }

            public TurnCardCommitmentEventKind[] EventKinds { get; }

            public TeamSide[] EventSides { get; }

            public CardInstanceId[] EventCardIds { get; }
        }
    }
}
