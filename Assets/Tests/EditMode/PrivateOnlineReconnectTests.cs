using System;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.34. The reconnect protocol itself: a reserved seat comes back to the player who
    /// held it and to nobody else, under a rotated connection epoch, with the whole owner-scoped
    /// projection re-sent rather than any part of it restored; the session holds still while a seat
    /// is away; a replayed submission identity is answered from what it already did instead of
    /// being executed twice; and every refusal on the way says nothing a refused peer may not know.
    /// </summary>
    /// <remarks>
    /// The host-side cases drive <see cref="PrivateSessionTestDriver"/>, because what they are
    /// about is what the host answers a hand-composed frame - including frames a real client of
    /// this build would never send. The end-to-end cases drive
    /// <see cref="PrivateOnlineWireHarness"/>, where real client machines lose their connections
    /// and come back on new ones through the same frame assemblers the socket adapter uses.
    /// </remarks>
    public sealed class PrivateOnlineReconnectTests
    {
        private const string FirstSeat = "red-player-01";
        private const string SecondSeat = "red-player-02";
        private const int WalkBudget = 1500;

        /// <summary>
        /// The reconnect row of the failure policy, end to end: the same player returns to its
        /// reserved seat, the seat's connection epoch is rotated, and what comes back is a
        /// complete owner-scoped projection identical to the host's own current encoding.
        /// </summary>
        [Test]
        public void AReservedSeat_ReturnsUnderItsCapability_WithARotatedEpochAndACompleteProjection()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(12);
            harness.AssertConverged();

            var seat = harness.SeatOf(2);
            var away = harness.Client(2);
            var versionBefore = harness.MatchHost.Version;
            Assert.That(harness.Session.EpochForSeat(new SeatId(seat)),
                Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch));

            harness.DisconnectClient(2);
            Assert.That(harness.Session.Roster.Single(row => row.Seat.Value == seat).Occupancy,
                Is.EqualTo(SeatOccupancyState.Reserved));
            Assert.That(away.State, Is.EqualTo(PrivateSessionClientState.HostLost));
            Assert.That(away.CanAttemptReconnect, Is.True);

            var returned = harness.ReconnectClient(2);
            Assert.That(harness.Client(returned), Is.SameAs(away),
                "The player who came back is the one who left; a new machine would be a new player.");
            Assert.That(away.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(away.IsReconnecting, Is.False);
            Assert.That(away.GrantedSeat.Seat.Value, Is.EqualTo(seat));
            Assert.That(harness.Session.Roster.Single(row => row.Seat.Value == seat).Occupancy,
                Is.EqualTo(SeatOccupancyState.Claimed));

            // The rotation the decision record asks for, on both sides of the wire.
            Assert.That(harness.Session.EpochForSeat(new SeatId(seat)),
                Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch + 1));
            Assert.That(away.ConnectionEpoch,
                Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch + 1));

            // A full compatible owner projection, not a snapshot and not a delta: byte for byte
            // the host's own current encoding for that seat.
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore),
                "Nothing moved while the seat was away, so the returning view is the one it left.");
            harness.AssertConverged();
            Assert.That(away.LatestProjection.SeatId, Is.EqualTo(seat));
            Assert.That(away.LatestProjection.Detail, Is.Not.Null);

            var events = harness.Session.DrainEvents();
            Assert.That(events.Count(record =>
                record.Kind == PrivateSessionEventKind.SeatReclaimed &&
                record.Seat.Value == seat), Is.EqualTo(1));

            // And the returned seat can act again, which the whole exercise is for.
            harness.DriveThroughWire(6);
            harness.AssertConverged();
            Assert.That(harness.MatchHost.Version, Is.GreaterThan(versionBefore));
        }

        /// <summary>
        /// Seat control never transfers. A reclaim presenting a capability that is not that seat's
        /// is refused, the reservation is untouched, and nothing owner-private is sent first.
        /// </summary>
        [Test]
        public void AReclaimWithAnotherCapability_IsRefused_AndTheSeatStaysReserved()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, FirstSeat);
            driver.Seat(2, SecondSeat);
            var mine = CapabilityFor(driver, 1);
            var theirs = CapabilityFor(driver, 2);
            Assert.That(mine, Is.Not.Empty);
            Assert.That(theirs, Is.Not.EqualTo(mine),
                "Two seats never share a capability, or one player's reclaim would be another's.");

            driver.Session.OnDisconnected(1);
            Assert.That(Occupancy(driver, FirstSeat), Is.EqualTo(SeatOccupancyState.Reserved));

            foreach (var offered in new[] { theirs, "", new string('0', 32) })
            {
                var ordinal = 10 + offered.Length;
                driver.Admit(ordinal);
                driver.Submit(ordinal, PrivateSessionFrameCodec.ReclaimSeatFrame(
                    new SeatReclaimMessage(new SeatId(FirstSeat), offered)));

                Assert.That(driver.Outbox.RejectFor(ordinal).Reason,
                    Is.EqualTo(SessionRejectionReason.ReconnectNotAuthorized),
                    "A reclaim without that seat's own capability is refused by one reason, " +
                    "whatever was wrong with it.");
                Assert.That(driver.Outbox.WasClosed(ordinal), Is.True);
                Assert.That(Occupancy(driver, FirstSeat), Is.EqualTo(SeatOccupancyState.Reserved),
                    "The refused reclaim left the reservation exactly where it was.");
                Assert.That(driver.Outbox.FramesFor(ordinal)
                        .Any(frame => frame.Kind == SessionMessageKind.SeatProjection ||
                            frame.Kind == SessionMessageKind.SeatGranted), Is.False,
                    "No owner-private byte crosses to a connection the host is about to refuse.");
            }

            // And the player who does hold it still gets the seat back afterwards.
            driver.Admit(20);
            driver.Submit(20, PrivateSessionFrameCodec.ReclaimSeatFrame(
                new SeatReclaimMessage(new SeatId(FirstSeat), mine)));
            Assert.That(Occupancy(driver, FirstSeat), Is.EqualTo(SeatOccupancyState.Claimed));
            Assert.That(driver.Session.EpochForSeat(new SeatId(FirstSeat)),
                Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch + 1));
        }

        /// <summary>
        /// A reclaim of a seat somebody is sitting in is refused by that seat's own public reason,
        /// and the holder is not disturbed by it.
        /// </summary>
        [Test]
        public void AReclaimOfALiveSeat_IsRefused_WithoutDisturbingItsHolder()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, FirstSeat);
            var capability = CapabilityFor(driver, 1);
            var epoch = driver.Session.EpochForSeat(new SeatId(FirstSeat));

            driver.Admit(9);
            driver.Submit(9, PrivateSessionFrameCodec.ReclaimSeatFrame(
                new SeatReclaimMessage(new SeatId(FirstSeat), capability)));

            Assert.That(driver.Outbox.RejectFor(9).Reason,
                Is.EqualTo(SessionRejectionReason.SeatAlreadyClaimed));
            Assert.That(driver.Outbox.WasClosed(9), Is.True);
            Assert.That(driver.Outbox.WasClosed(1), Is.False,
                "A refused reclaim cannot evict the connection that holds the seat.");
            Assert.That(Occupancy(driver, FirstSeat), Is.EqualTo(SeatOccupancyState.Claimed));
            Assert.That(driver.Session.TryGetSeatForConnection(1, out var held), Is.True);
            Assert.That(held.Id.Value, Is.EqualTo(FirstSeat));
            Assert.That(driver.Session.EpochForSeat(new SeatId(FirstSeat)), Is.EqualTo(epoch),
                "A refused reclaim rotates nothing.");
        }

        /// <summary>
        /// An ordinary claim of a reserved seat stays refused, and a reclaim of a seat nobody has
        /// ever held is refused too: a reservation is the only thing a capability can answer.
        /// </summary>
        [Test]
        public void AnOrdinaryClaimOfAReservedSeat_AndAReclaimOfAnOpenSeat_AreBothRefused()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, FirstSeat);
            var capability = CapabilityFor(driver, 1);
            driver.Session.OnDisconnected(1);

            driver.Admit(2);
            driver.Claim(2, FirstSeat);
            Assert.That(driver.Outbox.RejectFor(2).Reason,
                Is.EqualTo(SessionRejectionReason.SeatReserved),
                "Naming a reserved seat is not the same as proving it is yours.");

            driver.Admit(3);
            driver.Submit(3, PrivateSessionFrameCodec.ReclaimSeatFrame(
                new SeatReclaimMessage(new SeatId(SecondSeat), capability)));
            Assert.That(driver.Outbox.RejectFor(3).Reason,
                Is.EqualTo(SessionRejectionReason.ReconnectNotAuthorized));
            Assert.That(Occupancy(driver, SecondSeat), Is.EqualTo(SeatOccupancyState.Open),
                "A seat nobody has held has nothing to return to.");
        }

        /// <summary>
        /// A connection lost before it claims anything leaves nothing behind, exactly as the
        /// failure policy says: no reservation, no capability, nothing to come back to.
        /// </summary>
        [Test]
        public void AConnectionLostBeforeAClaim_CreatesNoReservation()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Admit(1);
            driver.Session.OnDisconnected(1);

            Assert.That(driver.Session.Roster.Select(row => row.Occupancy),
                Is.All.EqualTo(SeatOccupancyState.Open));
            Assert.That(driver.Session.IsPaused, Is.False,
                "Nothing is being waited for, so nothing is paused.");
            Assert.That(driver.Session.ClaimedSeatCount, Is.Zero);
        }

        /// <summary>
        /// Reconnect eligibility expires with the session that granted it, which is the only
        /// lifetime the architecture record gives a reservation: an orderly end releases every
        /// seat and drops every capability, and a reclaim afterwards is refused.
        /// </summary>
        [Test]
        public void AnOrderlySessionEnd_ReleasesEverySeat_AndEndsReconnectEligibility()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, FirstSeat);
            driver.Seat(2, SecondSeat);
            var capability = CapabilityFor(driver, 1);
            driver.Session.OnDisconnected(1);
            Assert.That(Occupancy(driver, FirstSeat), Is.EqualTo(SeatOccupancyState.Reserved));

            driver.Session.Shutdown();
            Assert.That(driver.Session.HasEnded, Is.True);
            Assert.That(driver.Session.Roster.Select(row => row.Occupancy),
                Is.All.EqualTo(SeatOccupancyState.Open),
                "There is no session left to hold a reservation.");
            Assert.That(driver.Session.IsPaused, Is.False);

            driver.Admit(5);
            driver.Submit(5, PrivateSessionFrameCodec.ReclaimSeatFrame(
                new SeatReclaimMessage(new SeatId(FirstSeat), capability)));
            Assert.That(driver.Outbox.RejectFor(5).Reason,
                Is.EqualTo(SessionRejectionReason.ReconnectNotAuthorized),
                "The capability outlived nothing: the session it belonged to is over.");
        }

        /// <summary>
        /// A reconnect whose admission tuple no longer matches is refused where every other
        /// incompatible connection is - at admission, before the roster or any projection.
        /// </summary>
        [Test]
        public void AnIncompatibleReturn_IsRefusedBeforeAnyPrivateProjection()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, FirstSeat);
            var capability = CapabilityFor(driver, 1);
            driver.Session.OnDisconnected(1);

            driver.Session.OnConnected(4, 0);
            driver.Hello(4, driver.IdentityWith(build: "some-other-build"), string.Empty);

            Assert.That(driver.Outbox.RejectFor(4).Reason,
                Is.EqualTo(SessionRejectionReason.BuildIdentityMismatch));
            Assert.That(driver.Outbox.FramesFor(4).Select(frame => frame.Kind),
                Is.All.EqualTo(SessionMessageKind.Challenge)
                    .Or.EqualTo(SessionMessageKind.Reject),
                "An incompatible peer learns the challenge and the refusal, and nothing else.");
            Assert.That(Occupancy(driver, FirstSeat), Is.EqualTo(SeatOccupancyState.Reserved));
            Assert.That(capability, Is.Not.Empty);
        }

        /// <summary>
        /// Old-connection input is discarded rather than applied: frames for the connection a
        /// return replaced never reach a seat, and a frame stamped with the retired epoch on the
        /// new connection is a protocol refusal before any seat-level answer.
        /// </summary>
        [Test]
        public void OldConnectionInput_IsDiscarded_AndARetiredEpochIsRefusedBeforeAnySeatAnswer()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var seat = harness.SeatOf(2);
            var command = harness.Client(2).LatestProjection.AuthorizedCommands[0];
            var version = harness.MatchHost.Version;

            harness.DisconnectClient(2);
            var returned = harness.ReconnectClient(2);
            var discardedBefore = harness.Session.DiscardedRetiredConnectionFrames;

            // A submission the lost connection had in flight, arriving after the return.
            harness.SendRaw(2, NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(harness.Session.SessionCorrelation,
                    PrivateSessionProtocol.InitialConnectionEpoch, 1, 1),
                "wire-old-connection", version, NetworkCommandCodec.Encode(command))));

            Assert.That(harness.Session.DiscardedRetiredConnectionFrames,
                Is.EqualTo(discardedBefore + 1));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(version),
                "A discarded frame moves nothing; discarding is how the two connections converge.");
            Assert.That(harness.Client(returned).State,
                Is.EqualTo(PrivateSessionClientState.Seated),
                "The returned connection is untouched by its predecessor's leftovers.");
            Assert.That(harness.Session.Roster.Single(row => row.Seat.Value == seat).Occupancy,
                Is.EqualTo(SeatOccupancyState.Claimed));

            // The same submission replayed onto the live connection, still stamped with the epoch
            // the lost connection spoke under.
            harness.SendRaw(returned, NetworkMessageCodec.SubmitCommandFrame(
                new NetworkSubmissionMessage(
                    new NetworkEnvelope(harness.Session.SessionCorrelation,
                        PrivateSessionProtocol.InitialConnectionEpoch, 1, 1),
                    "wire-old-epoch", version, NetworkCommandCodec.Encode(command))));

            Assert.That(harness.Client(returned).RejectionReason,
                Is.EqualTo(SessionRejectionReason.EnvelopeMismatch));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(version));
            Assert.That(harness.Client(1).State, Is.EqualTo(PrivateSessionClientState.Seated),
                "Only the offending connection is closed.");
        }

        /// <summary>
        /// New authoritative progress pauses while a seat is away, and resumes when it returns.
        /// The refusal names the pause, mutates nothing, and leaves the submission identity unused
        /// so the same attempt succeeds afterwards.
        /// </summary>
        [Test]
        public void WhileASeatIsAway_TheSessionHoldsStill_AndResumesOnItsReturn()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var version = harness.MatchHost.Version;

            harness.DisconnectClient(2);
            Assert.That(harness.Session.IsPaused, Is.True);

            var actor = harness.Client(1);
            var command = actor.LatestProjection.AuthorizedCommands[0];
            Assert.That(actor.TrySubmitCommand(command, out var identity), Is.True);
            harness.Pump();

            Assert.That(actor.LastSubmissionResult.Accepted, Is.False);
            Assert.That(actor.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.SessionPaused));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(version),
                "A paused session mutates nothing at all.");
            Assert.That(actor.State, Is.EqualTo(PrivateSessionClientState.Seated),
                "Being told to wait is not a disconnection.");

            harness.ReconnectClient(2);
            Assert.That(harness.Session.IsPaused, Is.False);

            // The identity was never consumed, so the same attempt - identity and all - now runs.
            Assert.That(actor.TryResubmitCommand(command, identity), Is.True);
            harness.Pump();
            Assert.That(actor.LastSubmissionResult.Accepted, Is.True,
                actor.LastSubmissionResult.Detail);
            Assert.That(harness.MatchHost.Version, Is.EqualTo(version + 1));
            harness.AssertConverged();
        }

        /// <summary>
        /// The failure policy's first row, both clauses. An identical retry inside the retained
        /// result window is answered from what that submission already did; the same identity
        /// asking something else is refused; and an identity whose answer has aged out of the
        /// result window is refused rather than run a second time.
        /// </summary>
        [Test]
        public void ARetriedSubmissionIdentity_IsAnsweredFromItsCachedResult_AndNeverExecutesTwice()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(3);
            var command = client.LatestProjection.AuthorizedCommands[0];
            var versionBefore = harness.MatchHost.Version;

            Assert.That(client.TrySubmitCommand(command, out var identity), Is.True);
            harness.Pump();
            var first = client.LastSubmissionResult;
            Assert.That(first.Accepted, Is.True, first.Detail);
            var accepted = harness.MatchHost.Version;
            Assert.That(accepted, Is.EqualTo(versionBefore + 1));

            // The retry a lost answer produces: the same identity, the same question.
            Assert.That(client.TryResubmitCommand(command, identity), Is.True);
            harness.Pump();
            var retried = client.LastSubmissionResult;
            Assert.That(retried.Accepted, Is.True,
                "An identical retry is answered with what the submission did, not refused.");
            Assert.That(retried.RejectionReason, Is.EqualTo(first.RejectionReason));
            Assert.That(retried.Version, Is.EqualTo(first.Version));
            Assert.That(retried.Sequence, Is.EqualTo(first.Sequence));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(accepted),
                "The cached answer executed nothing.");
            Assert.That(harness.MatchHost.Sequence, Is.EqualTo(first.Sequence),
                "and appended nothing to the ordered stream.");

            // The same identity asking a different question is a used name, not a retry.
            var other = harness.Client(4).LatestProjection.AuthorizedCommands[0];
            Assert.That(harness.Client(4).TryResubmitCommand(other, identity), Is.True);
            harness.Pump();
            Assert.That(harness.Client(4).LastSubmissionResult.Accepted, Is.False);
            Assert.That(harness.Client(4).LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.DuplicateSubmission));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(accepted));
            harness.AssertConverged();
        }

        /// <summary>
        /// The second clause on its own: an identity the host still recognizes but whose answer
        /// has left the retained result window is refused as the duplicate it is. It is still
        /// never executed twice - the point of the wider identity window.
        /// </summary>
        [Test]
        public void AnIdentityOlderThanTheResultWindow_IsRefusedAsDuplicate()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(3);
            var command = client.LatestProjection.AuthorizedCommands[0];
            Assert.That(client.TrySubmitCommand(command, out var identity), Is.True);
            harness.Pump();
            Assert.That(client.LastSubmissionResult.Accepted, Is.True);

            var driven = harness.DriveThroughWire(MatchHost.RetainedResultCount + 8);
            Assert.That(driven, Is.EqualTo(MatchHost.RetainedResultCount + 8),
                "The walk must outrun the result window for this case to be about anything.");
            Assert.That(MatchHost.RetainedSubmissionIdCount,
                Is.GreaterThan(MatchHost.RetainedResultCount),
                "The two windows differ on purpose; this case is what the difference is for.");

            var version = harness.MatchHost.Version;
            var sequence = harness.MatchHost.Sequence;
            var replaying = harness.Client(harness.OrdinalOf(harness.SeatOf(3)));
            Assert.That(replaying.TryResubmitCommand(command, identity), Is.True);
            harness.Pump();

            Assert.That(replaying.LastSubmissionResult.Accepted, Is.False);
            Assert.That(replaying.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.DuplicateSubmission));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(version));
            Assert.That(harness.MatchHost.Sequence, Is.EqualTo(sequence));
        }

        /// <summary>
        /// The capability is owner-private. It reaches the one connection that was granted the
        /// seat, and appears in no other seat's frames, in no roster, and in no diagnostic record.
        /// </summary>
        [Test]
        public void TheReconnectCapability_ReachesOneSeatOnly_AndNoDiagnosticRecordsIt()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(10);
            var events = harness.Session.DrainEvents();

            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                var capability = harness.CapabilityDeliveredTo(ordinal);
                Assert.That(capability, Is.Not.Empty,
                    "Seat " + harness.SeatOf(ordinal) + " received no way back.");
                var bytes = Encoding.UTF8.GetBytes(capability);

                foreach (var other in harness.Ordinals.Where(value => value != ordinal))
                {
                    foreach (var frame in harness.DeliveredFrames(other))
                    {
                        Assert.That(Contains(frame.Payload, bytes), Is.False,
                            "Seat " + harness.SeatOf(ordinal) + "'s reconnect capability crossed " +
                            "to seat " + harness.SeatOf(other) + " in a " + frame.Kind + " frame.");
                    }
                }

                foreach (var record in events)
                {
                    Assert.That(record.ToString(), Does.Not.Contain(capability),
                        "A diagnostic line carried a reconnect capability.");
                }

                foreach (var row in harness.Session.Roster)
                {
                    Assert.That(row.Seat.Value, Does.Not.Contain(capability));
                }
            }
        }

        /// <summary>
        /// Host loss is terminal for a seated client: it holds the last position it was shown, it
        /// cannot submit anything from there, and nothing about its state claims a continuation.
        /// An orderly Goodbye is a different thing and stays one.
        /// </summary>
        [Test]
        public void HostLoss_IsTerminalForASeatedClient_AndAnOrderlyEndIsNot()
        {
            var lost = PrivateOnlineWireHarness.Compose();
            lost.SeatAll();
            var machine = lost.Client(2);
            var seen = machine.LatestProjection;
            lost.DisconnectClient(2);

            Assert.That(machine.State, Is.EqualTo(PrivateSessionClientState.HostLost));
            Assert.That(machine.LatestProjection, Is.SameAs(seen),
                "The player still sees the position they were in; what they lose is the ability " +
                "to act on it.");
            Assert.That(machine.TrySubmitCommand(
                DirectorCommand.AdvanceEndOfRound(), out _), Is.False);
            Assert.That(machine.TryResubmitCommand(
                DirectorCommand.AdvanceEndOfRound(), "wire-after-host-loss"), Is.False);
            Assert.That(machine.RejectionReason, Is.EqualTo(SessionRejectionReason.None),
                "Nobody refused anything; the authority simply is not there.");

            var ended = PrivateOnlineWireHarness.Compose();
            ended.SeatAll();
            ended.Session.Shutdown();
            ended.Pump();
            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                Assert.That(ended.Client(ordinal).State,
                    Is.EqualTo(PrivateSessionClientState.Disconnected),
                    "An orderly end is an ending, not a loss.");
            }
        }

        /// <summary>
        /// A returning seat is reprojected, never restored: what the host sends is a fresh
        /// complete encoding of the seat's own view, and it contains no other seat's private card.
        /// </summary>
        [Test]
        public void AReturningSeat_IsSentAFreshCompleteProjection_WithNoOtherSeatsPrivateCards()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(20);

            var seat = harness.SeatOf(2);
            var machine = harness.Client(2);
            var before = harness.LastProjectionPayload(2);
            harness.DisconnectClient(2);
            var returned = harness.ReconnectClient(2);

            var after = harness.LastProjectionPayload(returned);
            Assert.That(after, Is.Not.Null, "The return delivered no projection at all.");
            Assert.That(after, Is.EqualTo(before),
                "Nothing moved while the seat was away, so the reprojection is the same complete " +
                "view - re-encoded from the authority, not replayed from anything the client held.");

            var record = machine.LatestProjection;
            Assert.That(record.SeatId, Is.EqualTo(seat));
            var own = new System.Collections.Generic.HashSet<string>(
                record.OwnCards.Select(card => card.InstanceId));
            var public_ = new System.Collections.Generic.HashSet<string>(
                record.PublicCards.Select(card => card.InstanceId));
            var mine = harness.MatchHost.Seats.Seats.Single(value => value.Id.Value == seat);
            foreach (var card in harness.MatchHost.Projection.Cards)
            {
                if (card.OwnerId.Equals(mine.HeroId))
                {
                    continue;
                }

                if (card.State.Location == GuardsDigital.Core.Cards.CardLocation.Hand ||
                    card.State.Location == GuardsDigital.Core.Cards.CardLocation.Deck)
                {
                    Assert.That(own.Contains(card.Id.Value) || public_.Contains(card.Id.Value),
                        Is.False,
                        "The reprojection carried another hero's hidden card " + card.Id.Value + ".");
                }
            }
        }

        /// <summary>
        /// The agreement half of the reconnect check: a returning eligible player is given the
        /// standing proposal and every public statement made under it, may revise only its own,
        /// and its return converts neither a partial nor a conflicting set of statements into an
        /// agreement.
        /// </summary>
        /// <remarks>
        /// The family is reached by the deterministic wire walk rather than staged, so the
        /// proposal, its candidates and its participants are the ones the rules produced. Every
        /// participant but one states first, so the away player is the only thing standing between
        /// the team and unanimity - which is what makes "the return did not agree for them" mean
        /// something.
        /// </remarks>
        [Test]
        public void AReturningEligiblePlayer_ResumesThePartialAgreement_WithoutManufacturingOne()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var walked = harness.DriveThroughWire(WalkBudget, projection =>
                projection.DecisionContract != null &&
                projection.DecisionContract.FamilyId.Value == "initiative");
            Assert.That(harness.MatchHost.Projection.DecisionContract, Is.Not.Null,
                "No decision stands after " + walked + " steps.");
            Assert.That(harness.MatchHost.Projection.DecisionContract.FamilyId.Value,
                Is.EqualTo("initiative"),
                "The deterministic walk stopped on another family after " + walked + " steps.");

            var eligible = harness.Ordinals
                .Where(ordinal => harness.Client(ordinal).LatestProjection?.Detail?.TeamAgreement !=
                    null && harness.Client(ordinal).LatestProjection.Detail.TeamAgreement
                        .AuthorizedStatements.Count > 0)
                .ToArray();
            Assert.That(eligible.Length, Is.GreaterThanOrEqualTo(2),
                "A same-team tie is between at least two players, or there is no agreement here.");
            var proposal = harness.Client(eligible[0]).LatestProjection.Detail.TeamAgreement;
            var candidates = proposal.Candidates
                .Select(row => row.CardInstanceId).Distinct().ToArray();
            Assert.That(candidates.Length, Is.GreaterThanOrEqualTo(2),
                "A tie stands between at least two cards, or nothing can be disagreed about.");

            // Everyone but one states the same preference, which is short of unanimity by exactly
            // the player who is about to leave.
            var leaves = eligible[eligible.Length - 1];
            var version = harness.MatchHost.Version;
            foreach (var ordinal in eligible.Where(value => value != leaves))
            {
                State(harness, ordinal, candidates[0]);
                Assert.That(harness.MatchHost.Version, Is.EqualTo(version),
                    "A statement short of unanimity moved the match.");
            }

            harness.DisconnectClient(leaves);
            Assert.That(harness.Session.IsPaused, Is.True);
            var returned = harness.ReconnectClient(leaves);
            harness.AssertConverged();

            // What the return delivered: the same standing proposal, and every teammate's current
            // public statement.
            var resumed = harness.Client(returned).LatestProjection.Detail.TeamAgreement;
            Assert.That(resumed, Is.Not.Null,
                "The returning player was reprojected without the proposal it left standing.");
            Assert.That(resumed.ProposalId, Is.EqualTo(proposal.ProposalId),
                "The standing proposal changed across a return that moved nothing.");
            var mine = HeroOf(harness, returned);
            foreach (var ordinal in eligible.Where(value => value != leaves))
            {
                var row = resumed.Participants.Single(value => value.HeroId == HeroOf(harness, ordinal));
                Assert.That(row.HasStatement, Is.True,
                    "The returning player was not told what its teammate had already said.");
                Assert.That(row.PreferredCardInstanceId, Is.EqualTo(candidates[0]));
            }

            Assert.That(resumed.Participants.Single(value => value.HeroId == mine).HasStatement,
                Is.False, "The return stated a preference for a player who never made one.");
            Assert.That(harness.MatchHost.Version, Is.EqualTo(version),
                "The return converted a partial agreement into an agreement.");
            Assert.That(harness.MatchHost.Projection.DecisionContract.FamilyId.Value,
                Is.EqualTo("initiative"),
                "The tie resolved itself while a statement was still missing.");

            // A conflicting statement is not agreement either, and it revises one row only.
            State(harness, returned, candidates[1]);
            Assert.That(harness.MatchHost.Version, Is.EqualTo(version),
                "Two players preferring different candidates is a disagreement, not a choice.");
            var conflicting = harness.Client(returned).LatestProjection.Detail.TeamAgreement;
            foreach (var ordinal in eligible.Where(value => value != leaves))
            {
                Assert.That(conflicting.Participants
                        .Single(value => value.HeroId == HeroOf(harness, ordinal))
                        .PreferredCardInstanceId,
                    Is.EqualTo(candidates[0]),
                    "Stating a preference revised a teammate's public statement.");
            }

            Assert.That(conflicting.Participants.Single(value => value.HeroId == mine)
                .PreferredCardInstanceId, Is.EqualTo(candidates[1]));
            harness.AssertConverged();

            // And revising its own to match is what unanimity is: the host makes the final rules
            // choice itself, exactly once, after the return.
            State(harness, returned, candidates[0]);
            Assert.That(harness.MatchHost.Version, Is.GreaterThan(version),
                "Unanimity reached after a return did not produce the host's own final choice.");
            Assert.That(harness.MatchHost.Projection.DecisionContract?.FamilyId.Value,
                Is.Not.EqualTo("initiative"), "The answered tie is still standing.");
            harness.AssertConverged();
        }

        /// <summary>
        /// A version a returning connection claims is never trusted, whichever side of the
        /// authority it falls on: a future expected version is refused by name, nothing is
        /// mutated or partially replaced, no seat moves, and the resync offered beside the refusal
        /// converges the seat on the authority's own view.
        /// </summary>
        [Test]
        public void AReturningSeatsClaimedVersion_IsNeverTrusted_AndReplacesNoAuthoritativeState()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(8);
            harness.AssertConverged();

            var seat = harness.SeatOf(2);
            harness.DisconnectClient(2);
            var returned = harness.ReconnectClient(2);
            var version = harness.MatchHost.Version;
            var sequence = harness.MatchHost.Sequence;
            var roster = harness.Session.Roster
                .Select(row => row.Seat.Value + ":" + row.Occupancy).ToArray();
            var command = harness.Client(returned).LatestProjection.AuthorizedCommands
                .FirstOrDefault() ?? DirectorCommand.AdvanceEndOfRound();

            harness.SendSubmissionAtVersion(
                returned, "wire-return-future", version + 500, command);

            Assert.That(harness.Client(returned).LastSubmissionResult.Accepted, Is.False);
            Assert.That(harness.Client(returned).LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection),
                "A claimed version ahead of the authority is answered exactly as one behind it.");
            Assert.That(harness.MatchHost.Version, Is.EqualTo(version));
            Assert.That(harness.MatchHost.Sequence, Is.EqualTo(sequence),
                "A refused submission appended nothing to the ordered stream.");
            Assert.That(harness.Session.Roster.Select(row => row.Seat.Value + ":" + row.Occupancy),
                Is.EqualTo(roster), "A refused submission moved a seat.");
            Assert.That(harness.Client(returned).State,
                Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(harness.Client(returned).LatestProjection.SeatId, Is.EqualTo(seat),
                "The refusal was answered to the seat that asked, with that seat's own view.");
            Assert.That(harness.Client(returned).LatestProjection.Version, Is.EqualTo(version),
                "The resync offered beside the refusal did not converge the seat.");
            harness.AssertConverged();
        }

        private static string HeroOf(PrivateOnlineWireHarness harness, int ordinal) =>
            harness.MatchHost.Seats.Seats
                .Single(value => value.Id.Value == harness.SeatOf(ordinal)).HeroId.Value;

        /// <summary>
        /// States one seat's preference for one candidate through its own client, and reads the
        /// acceptance back off the wire.
        /// </summary>
        private static void State(
            PrivateOnlineWireHarness harness, int ordinal, string cardInstanceId)
        {
            var client = harness.Client(ordinal);
            var agreement = client.LatestProjection.Detail.TeamAgreement;
            Assert.That(agreement, Is.Not.Null,
                "Seat " + harness.SeatOf(ordinal) + " holds no standing proposal.");
            var statement = agreement.AuthorizedStatements.FirstOrDefault(value =>
                value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                value.PreferredCardInstanceId.Value == cardInstanceId);
            Assert.That(statement, Is.Not.Null,
                "Seat " + harness.SeatOf(ordinal) + " is offered no preference for " +
                cardInstanceId + ".");
            Assert.That(client.TrySubmitAgreement(statement, out _), Is.True);
            harness.Pump();
            Assert.That(client.LastAgreementResult.Accepted, Is.True,
                "Seat " + harness.SeatOf(ordinal) + ": " + client.LastAgreementResult.Detail);
        }

        private static string CapabilityFor(PrivateSessionTestDriver driver, int ordinal)
        {
            foreach (var frame in driver.Outbox.FramesFor(ordinal))
            {
                if (frame.Kind == SessionMessageKind.SeatGranted &&
                    PrivateSessionFrameCodec.TryDecodeSeatGranted(frame.Payload, out var granted))
                {
                    return granted.ReconnectCapability;
                }
            }

            Assert.Fail("Ordinal " + ordinal + " was never granted a seat.");
            return string.Empty;
        }

        private static SeatOccupancyState Occupancy(PrivateSessionTestDriver driver, string seat) =>
            driver.Session.Roster.Single(row => row.Seat.Value == seat).Occupancy;

        private static bool Contains(byte[] haystack, byte[] needle)
        {
            if (needle.Length == 0 || haystack.Length < needle.Length)
            {
                return false;
            }

            for (var index = 0; index <= haystack.Length - needle.Length; index++)
            {
                var matched = true;
                for (var offset = 0; offset < needle.Length; offset++)
                {
                    if (haystack[index + offset] != needle[offset])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
