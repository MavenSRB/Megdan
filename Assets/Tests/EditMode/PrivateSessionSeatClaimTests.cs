using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.31. One stable seat per connection: six distinct claims fill the official
    /// configuration, and every way a claim can be wrong is refused by its own name with the seat
    /// map left exactly as it was.
    /// </summary>
    public sealed class PrivateSessionSeatClaimTests
    {
        [Test]
        public void SixDistinctClaims_FillTheOfficialConfiguration()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var expected = new[]
            {
                ("blue-player-01", "hero-arien", TeamSide.Blue),
                ("blue-player-02", "hero-brogan", TeamSide.Blue),
                ("blue-player-03", "hero-sabina", TeamSide.Blue),
                ("red-player-01", "hero-wasp", TeamSide.Red),
                ("red-player-02", "hero-xargatha", TeamSide.Red),
                ("red-player-03", "hero-tigerclaw", TeamSide.Red)
            };

            Assert.That(driver.Session.Roster.Select(entry => entry.Seat.Value),
                Is.EqualTo(expected.Select(seat => seat.Item1)),
                "Seats are derived from the participant graph in its stable order.");

            for (var index = 0; index < expected.Length; index++)
            {
                driver.Seat(index + 1, expected[index].Item1);
            }

            Assert.That(driver.Session.ClaimedSeatCount, Is.EqualTo(6));
            Assert.That(driver.Session.IsComplete, Is.True);
            foreach (var (seat, hero, side) in expected)
            {
                var entry = driver.Session.Roster.Single(row => row.Seat.Value == seat);
                Assert.That(entry.Hero.Value, Is.EqualTo(hero));
                Assert.That(entry.TeamSide, Is.EqualTo(side));
                Assert.That(entry.Occupancy, Is.EqualTo(SeatOccupancyState.Claimed));
            }
        }

        [Test]
        public void AGrant_NamesTheSeatHeroTeamAndRunningCount()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Admit(1);
            driver.Claim(1, "red-player-01");

            var granted = driver.Outbox.FramesFor(1)
                .Single(frame => frame.Kind == SessionMessageKind.SeatGranted);
            Assert.That(PrivateSessionFrameCodec.TryDecodeSeatGranted(granted.Payload, out var decoded), Is.True);
            Assert.That(decoded.Seat.Seat.Value, Is.EqualTo("red-player-01"));
            Assert.That(decoded.Seat.Hero.Value, Is.EqualTo("hero-wasp"));
            Assert.That(decoded.Seat.TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(decoded.ClaimedSeats, Is.EqualTo(1));
            Assert.That(decoded.TotalSeats, Is.EqualTo(6));
        }

        [Test]
        public void AClaimOfAHeldSeat_IsRefusedAndChangesNothing()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, "red-player-01");
            driver.Admit(2);
            driver.Claim(2, "red-player-01");

            Assert.That(driver.Outbox.RejectFor(2).Reason,
                Is.EqualTo(SessionRejectionReason.SeatAlreadyClaimed));
            Assert.That(driver.Outbox.WasClosed(2), Is.True);
            Assert.That(driver.Outbox.WasClosed(1), Is.False,
                "Only the claiming connection is disconnected.");
            Assert.That(driver.Session.ClaimedSeatCount, Is.EqualTo(1));
            Assert.That(driver.Session.TryGetSeatForConnection(1, out var seat), Is.True);
            Assert.That(seat.Id.Value, Is.EqualTo("red-player-01"));
        }

        [Test]
        public void AClaimOfAnUnknownSeat_IsRefusedByName()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Admit(1);
            driver.Claim(1, "green-player-09");

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.UnknownSeat));
            Assert.That(driver.Session.ClaimedSeatCount, Is.Zero);
        }

        [Test]
        public void ASecondClaimFromASeatedConnection_IsRefusedWithoutUnseatingIt()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, "red-player-01");
            driver.Claim(1, "red-player-02");

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.SeatChangeRefused));
            Assert.That(driver.Outbox.WasClosed(1), Is.False,
                "The refused change leaves the connection seated where it was.");
            Assert.That(driver.Session.ClaimedSeatCount, Is.EqualTo(1));
            Assert.That(driver.Session.TryGetSeatForConnection(1, out var seat), Is.True);
            Assert.That(seat.Id.Value, Is.EqualTo("red-player-01"));
            Assert.That(driver.Session.Roster.Single(row => row.Seat.Value == "red-player-02").Occupancy,
                Is.EqualTo(SeatOccupancyState.Open));
        }

        [Test]
        public void ADisconnectedClaimedSeat_IsReservedNotReleased()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, "red-player-01");
            driver.Session.OnDisconnected(1);

            Assert.That(driver.Session.Roster.Single(row => row.Seat.Value == "red-player-01").Occupancy,
                Is.EqualTo(SeatOccupancyState.Reserved));
            Assert.That(driver.Session.ClaimedSeatCount, Is.Zero);

            driver.Admit(2);
            driver.Claim(2, "red-player-01");
            Assert.That(driver.Outbox.RejectFor(2).Reason,
                Is.EqualTo(SessionRejectionReason.SeatReserved),
                "With no identity to recognize a returning player, the seat is nobody else's.");
            Assert.That(driver.Session.Roster.Single(row => row.Seat.Value == "red-player-01").Occupancy,
                Is.EqualTo(SeatOccupancyState.Reserved));
        }

        /// <summary>
        /// Milestone 8.35k. A Goodbye is the departure the player CHOSE, and it gives the seat up.
        /// </summary>
        /// <remarks>
        /// This case asserted the opposite until this milestone, because until this milestone every
        /// departure reserved the seat. The user's rule replaced that: leaving forfeits, crashing
        /// does not. The wire already told the two apart - a deliberate leave sends
        /// <c>Goodbye</c> and a lost link sends nothing - so the distinction cost no new message
        /// kind and no protocol move, only a decision the session had never been asked to make.
        /// </remarks>
        [Test]
        public void AGoodbyeFromASeatedConnection_ReleasesItsSeat()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, "blue-player-03");
            driver.Session.OnFrame(1, PrivateSessionTestDriver.Decode(
                PrivateSessionFrameCodec.GoodbyeFrame()));

            Assert.That(driver.Outbox.WasClosed(1), Is.True);
            Assert.That(driver.Session.Roster.Single(row => row.Seat.Value == "blue-player-03").Occupancy,
                Is.EqualTo(SeatOccupancyState.Open),
                "The leaver chose the exposure, so the seat is given up rather than held.");
            Assert.That(driver.Session.ClaimedSeatCount, Is.Zero);
            Assert.That(driver.Session.DrainEvents().Count(record =>
                    record.Kind == PrivateSessionEventKind.SeatReleased &&
                    record.Seat.Value == "blue-player-03"),
                Is.EqualTo(1),
                "and the host says which of the two departures happened, because a log that " +
                "called both 'reserved' is what made the playtest's refusal unreadable.");
        }

        /// <summary>
        /// Milestone 8.35k. The two departures diverge where it matters: at the next claim.
        /// </summary>
        /// <remarks>
        /// Stating both halves in one case is deliberate. A build that forfeited every departure
        /// would pass a leave-only case while throwing away the reservation a crashed player comes
        /// back along; a build that reserved every departure would pass a crash-only case while
        /// leaving the leaver's seat stuck - which on the night of the playtest the host could only
        /// escape by tearing the session down and re-hosting.
        /// </remarks>
        [Test]
        public void AReleasedSeat_TakesAnOrdinaryClaim_WhereAReservedOneRefusesIt()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, "blue-player-03");
            driver.Seat(2, "red-player-01");

            driver.Session.OnFrame(1, PrivateSessionTestDriver.Decode(
                PrivateSessionFrameCodec.GoodbyeFrame()));
            driver.Session.OnDisconnected(2);

            driver.Admit(3);
            driver.Claim(3, "blue-player-03");
            Assert.That(driver.Session.TryGetSeatForConnection(3, out var taken), Is.True,
                "A forfeited seat is claimable again by an ordinary keyed claim.");
            Assert.That(taken.Id.Value, Is.EqualTo("blue-player-03"));

            driver.Admit(4);
            driver.Claim(4, "red-player-01");
            Assert.That(driver.Outbox.RejectFor(4).Reason,
                Is.EqualTo(SessionRejectionReason.SeatReserved),
                "while a seat whose player only dropped is nobody else's - every invited player " +
                "holds the session key, and holding it is an invitation, never authority.");
            Assert.That(driver.Session.Roster.Single(row => row.Seat.Value == "red-player-01").Occupancy,
                Is.EqualTo(SeatOccupancyState.Reserved));
        }

        [Test]
        public void AClaim_BroadcastsTheRosterToEveryAdmittedConnection()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Admit(1);
            driver.Admit(2);
            driver.Claim(1, "red-player-01");

            var update = driver.Outbox.FramesFor(2)
                .Single(frame => frame.Kind == SessionMessageKind.RosterUpdate);
            Assert.That(PrivateSessionFrameCodec.TryDecodeRosterUpdate(update.Payload, out var decoded), Is.True);
            Assert.That(decoded.Roster.Single(row => row.Seat.Value == "red-player-01").Occupancy,
                Is.EqualTo(SeatOccupancyState.Claimed));
        }

        [Test]
        public void AnUnadmittedConnection_ReceivesNoRosterBroadcast()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.OnConnected(9, 0);
            driver.Seat(1, "red-player-01");

            Assert.That(driver.Outbox.FramesFor(9)
                    .Count(frame => frame.Kind == SessionMessageKind.RosterUpdate),
                Is.Zero, "Before admission a connection is told nothing about the session.");
        }

        [Test]
        public void TheHostEvents_RecordTheClaimAndTheCompletion()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var seats = driver.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
            for (var index = 0; index < seats.Length; index++)
            {
                driver.Seat(index + 1, seats[index]);
            }

            var events = driver.Session.DrainEvents();
            Assert.That(events.Count(record => record.Kind == PrivateSessionEventKind.SeatClaimed),
                Is.EqualTo(6));
            Assert.That(events.Count(record => record.Kind == PrivateSessionEventKind.SessionComplete),
                Is.EqualTo(1));
            Assert.That(driver.Session.DrainEvents(), Is.Empty, "Draining clears the record.");
        }
    }
}
