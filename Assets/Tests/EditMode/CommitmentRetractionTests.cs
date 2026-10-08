using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class CommitmentRetractionTests
    {
        private static void Commit(MatchHost host, MatchSeat seat)
        {
            var command = host.ProjectionForSeat(seat.Id).AuthorizedCommands.First(c => c.Kind == DirectorCommandKind.CommitCard);
            Assert.That(new SeatMatchClient(host.ClientForSeat(seat.Id)).Submit(command).Accepted, Is.True);
        }

        [Test]
        public void OwnCommitmentCanBeWithdrawnWithoutChangingLaterCommitmentsAndCanBeRecommitted()
        {
            var initial = OwnerScopedProjectionTests.CreateOfficialInitialState();
            var host = MatchHost.Compose(initial); var own = host.Seats.Seats[0]; var other = host.Seats.Seats[1];
            var initialOwnStates = host.Projection.Cards.Where(c => c.OwnerId.Equals(own.HeroId)).Select(c => c.State).ToArray();
            Commit(host, own); Commit(host, other);
            var otherCards = host.Projection.Cards.Where(c => c.OwnerId.Equals(other.HeroId)).Select(c => c.State).ToArray();
            var count = host.Projection.AcceptedCommands.Count;
            var request = new SeatUndoSubmission(new SubmissionId("withdraw"), host.Version);
            Assert.That(host.ProjectionForSeat(own.Id).IsSeatUndoAvailable, Is.True);
            Assert.That(host.SubmitUndoForSeat(own.Id, request).Accepted, Is.True);
            Assert.That(host.Projection.GetCommitment(own.HeroId).Status, Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            Assert.That(host.Projection.Cards.Where(c => c.OwnerId.Equals(own.HeroId)).Select(c => c.State), Is.EqualTo(initialOwnStates));
            Assert.That(host.Projection.Cards.Where(c => c.OwnerId.Equals(other.HeroId)).Select(c => c.State), Is.EqualTo(otherCards));
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(count + 1), "Withdrawal is a replayable command, not a rewind beneath other players.");
            Assert.That(host.SubmitUndoForSeat(own.Id, request).Accepted, Is.True, "Identical retry returns its cached result.");
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(count + 1));
            Commit(host, own);
            var replay = MatchHost.Compose(initial);
            foreach (var command in host.Projection.AcceptedCommands)
                Assert.That(replay.Submit(MatchSubmission.ForCommand(command)).Accepted, Is.True);
            Assert.That(replay.CaptureSnapshot(), Is.EqualTo(host.CaptureSnapshot()));
        }

        [TestCase(false)] [TestCase(true)]
        public void FinalCommitmentAndWithdrawalHaveOneAtomicOrdering(bool withdrawalFirst)
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seats = host.Seats.Seats; var own = seats[0];
            foreach (var seat in seats.Take(seats.Count - 1)) Commit(host, seat);
            var undo = new SeatUndoSubmission(new SubmissionId("race"), host.Version);
            if (withdrawalFirst) Assert.That(host.SubmitUndoForSeat(own.Id, undo).Accepted, Is.True);
            Commit(host, seats.Last());
            if (withdrawalFirst)
            {
                Assert.That(host.Projection.GetCommitment(own.HeroId).Status, Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
                Commit(host, own);
            }
            else Assert.That(host.SubmitUndoForSeat(own.Id, undo).Accepted, Is.False, "The old view cannot cross final acceptance.");
            Assert.That(host.Projection.Commitments.All(c => c.Status == TurnCardCommitmentStatus.Revealed), Is.True);
            Assert.That(host.SubmitUndoForSeat(own.Id, new SeatUndoSubmission(new SubmissionId("after-reveal"), host.Version)).Accepted, Is.False);
        }

        [Test]
        public void SnapshotWithoutReplayAncestryStillAllowsOnlyItsOwnersUnrevealedWithdrawal()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var own = host.Seats.Seats[0]; var other = host.Seats.Seats[1]; Commit(host, own);
            var snapshot = host.CaptureSnapshot();
            Assert.That(host.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted, Is.True);
            Assert.That(host.Projection.IsHistoryAvailable, Is.False);
            Assert.That(host.SubmitUndoForSeat(other.Id, new SeatUndoSubmission(new SubmissionId("wrong-owner"), host.Version)).Accepted, Is.False);
            Assert.That(host.SubmitUndoForSeat(own.Id, new SeatUndoSubmission(new SubmissionId("restored-withdraw"), host.Version)).Accepted, Is.True);
            Assert.That(host.Projection.GetCommitment(own.HeroId).Status, Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
        }

        [Test]
        public void TwoCardCommitmentReturnsBothCardsAtomicallyAndItsEventDisclosesNoCardIdentity()
        {
            var config = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = new TurnCardCommitmentAuthority(config.CardInstances, config.Participants);
            var cards = config.RedHand.Instances.Take(2).ToArray(); var hero = cards[0].OwnerId;
            Assert.That(authority.TryCommitTwoCards(hero, cards[0].Id, cards[1].Id).Accepted, Is.True);
            var result = authority.TryRetractCommitment(hero);
            Assert.That(result.Accepted, Is.True);
            Assert.That(cards.All(c => c.State.Equals(CardStateFacts.InitialInHand())), Is.True);
            Assert.That(result.Events.Single().Kind, Is.EqualTo(TurnCardCommitmentEventKind.CommitmentRetracted));
            Assert.That(result.Events.Single().HasCardInstanceId, Is.False);
            Assert.That(authority.TryRetractCommitment(hero).Accepted, Is.False);
            var command = DirectorCommand.RetractCommitment(hero);
            Assert.That(NetworkCommandCodec.TryDecode(NetworkCommandCodec.Encode(command), out var decoded), Is.True);
            Assert.That(decoded, Is.EqualTo(command));
        }

        [Test]
        public void InvalidSecondCardRefusesTheWholeWithdrawalWithoutChangingTheFirst()
        {
            var config = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = new TurnCardCommitmentAuthority(config.CardInstances, config.Participants);
            var cards = config.RedHand.Instances.Take(2).ToArray(); var hero = cards[0].OwnerId;
            Assert.That(authority.TryCommitTwoCards(hero, cards[0].Id, cards[1].Id).Accepted, Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(cards[1]), Is.True);
            var before = cards.Select(c => c.State).ToArray();
            var result = authority.TryRetractCommitment(hero);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(TurnCardCommitmentRejectionReason.CommittedCardStateChanged));
            Assert.That(cards.Select(c => c.State), Is.EqualTo(before));
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void SharedRevealClosesWithdrawalEvenWhileTwoCardRetrievalIsPending()
        {
            var config = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = new TurnCardCommitmentAuthority(config.CardInstances, config.Participants);
            var cards = config.RedHand.Instances.Take(2).ToArray(); var hero = cards[0].OwnerId;
            Assert.That(authority.TryCommitTwoCards(hero, cards[0].Id, cards[1].Id).Accepted, Is.True);
            var other = config.BlueHand.Instances.First();
            Assert.That(authority.TryCommitCard(other.OwnerId, other.Id).Accepted, Is.True);
            Assert.That(authority.IsRevealed, Is.True);
            Assert.That(authority.PendingRetrievalHero.HasValue, Is.True);
            var before = config.CardInstances.Select(c => c.State).ToArray();
            var result = authority.TryRetractCommitment(hero);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(TurnCardCommitmentRejectionReason.TurnAlreadyRevealed));
            Assert.That(config.CardInstances.Select(c => c.State), Is.EqualTo(before));
            Assert.That(result.Events, Is.Empty);
        }

        [TestCase(3)] [TestCase(13)]
        public void WithdrawalResumesSpentCommitmentAllowanceWithoutRefundingEitherBank(int elapsedSeconds)
        {
            var initial = OwnerScopedProjectionTests.CreateOfficialInitialState();
            var host = MatchHost.Compose(new MatchSessionInitialState(initial.Scenario, initial.Setup,
                initial.AvailableMaps, initial.InitialConfiguration, experiments: new ExperimentalMatchOptions(
                    turnTimer: true, cardCommitmentSeconds: 10, teamBankSeconds: 20)));
            var own = host.Seats.Seats[0]; host.SetTimerStarted(true);
            foreach (var seat in host.Seats.Seats) host.SetClockReadiness(seat.Id, host.Version, true);
            host.AdvanceClock(TimeSpan.FromSeconds(elapsedSeconds).Ticks);
            var before = host.ClockState; var remaining = before.Entries.Single(e => e.Owner.Equals(own.HeroId)).RemainingTicks;
            Assert.That(remaining, Is.EqualTo(TimeSpan.FromSeconds(Math.Max(0, 10 - elapsedSeconds)).Ticks));
            if (elapsedSeconds > 10) Assert.That(before.RedBankTicks, Is.LessThan(TimeSpan.FromSeconds(20).Ticks));
            Commit(host, own);
            Assert.That(host.SubmitUndoForSeat(own.Id, new SeatUndoSubmission(new SubmissionId("timed-withdraw"), host.Version)).Accepted, Is.True);
            Assert.That(host.ClockState.Entries.Single(e => e.Owner.Equals(own.HeroId)).RemainingTicks, Is.EqualTo(remaining));
            Assert.That(host.ClockState.RedBankTicks, Is.EqualTo(before.RedBankTicks));
            Assert.That(host.ClockState.BlueBankTicks, Is.EqualTo(before.BlueBankTicks));
        }

        [Test]
        public void ReconnectedSeatCanWithdrawOverTheWireAndOpponentReceivesNoPrivateCard()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var wire = PrivateOnlineWireHarness.Compose(host); wire.SeatAll(); wire.ReadyAndPickEverySeat();
            var own = host.Seats.Seats[0]; var machine = wire.Client(1);
            var command = machine.LatestProjection.AuthorizedCommands.First(c => c.Kind == DirectorCommandKind.CommitCard);
            Assert.That(machine.TrySubmitCommand(command, out _), Is.True); wire.Pump();
            wire.DisconnectClient(1); machine = wire.Client(wire.ReconnectClient(1));
            Assert.That(machine.LatestProjection.IsSeatUndoAvailable, Is.True);
            Assert.That(machine.TrySubmitUndo(out _), Is.True); wire.Pump();
            Assert.That(machine.IsWaitingForHost, Is.False);
            Assert.That(host.Projection.GetCommitment(own.HeroId).Status, Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            var enemy = host.Seats.Seats.First(s => s.TeamSide != own.TeamSide);
            Assert.That(host.ProjectionForSeat(enemy.Id).Cards.Any(c => c.OwnerId.Equals(own.HeroId) && c.State.Location == CardLocation.Hand), Is.False);
        }
    }
}
