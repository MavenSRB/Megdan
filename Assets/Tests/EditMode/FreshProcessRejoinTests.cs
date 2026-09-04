using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35k. A player whose process died has a road back to their own seat, and a player
    /// who deliberately left does not.
    ///
    /// <para>Milestone 8.34 built the whole return mechanism and it worked. What it never had was a
    /// road to it: the host-minted capability lived in one private field of one live
    /// <c>PrivateSessionClient</c>, and three ordinary player actions end that object - the process
    /// exiting, the process crashing, and pressing LEAVE. After any of them the lobby could only
    /// offer a FIRST join, which the host refuses by name because the seat is reserved. That is the
    /// 2026-09-03 playtest report verbatim: "rejoin does not work, if user disconnects, its seat
    /// stays away, and connection is refused."</para>
    ///
    /// <para><b>Why 8.35c's rehearsal missed it.</b> Its reconnect cohort cut the SOCKET and kept
    /// the client object, so it exercised the one path that already worked. These cases construct a
    /// second, distinct client - which is what a relaunched process is - and that single difference
    /// is the whole defect.</para>
    /// </summary>
    public sealed class FreshProcessRejoinTests
    {
        private const string ReturningSeat = "red-player-02";

        /// <summary>
        /// The reported defect, as a fixture: a brand-new client for the same seat gets back in.
        /// </summary>
        /// <remarks>
        /// Before this milestone the third assertion below was the bug - admission succeeded, the
        /// Welcome arrived with the roster and the match phase, and the player was refused at the
        /// seat claim with `SeatReserved` and the connection closed. Everything upstream of the
        /// claim already worked for a returning player; only the claim/reclaim fork had nothing to
        /// go on, because nothing had survived the process.
        /// </remarks>
        [Test]
        public void ABrandNewClientForAReservedSeat_ReclaimsIt_WithARotatedEpoch()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(12);
            harness.AssertConverged();

            var ordinal = harness.OrdinalOf(ReturningSeat);
            var seat = harness.SeatOf(ordinal);
            var original = harness.Client(ordinal);
            var versionBefore = harness.MatchHost.Version;

            // The process dies: the link drops and the object is discarded. Nothing is copied out
            // of it - that is the whole point, and copying anything would be the fixture cheating.
            harness.DisconnectClient(ordinal);
            Assert.That(Occupancy(harness, seat), Is.EqualTo(SeatOccupancyState.Reserved),
                "A dropped connection reserves the seat; that half always worked.");

            var relaunched = harness.Connect(harness.NextOrdinal(), seat);
            Assert.That(relaunched, Is.Not.SameAs(original),
                "This is a NEW client, which is what a relaunched process brings; reusing the old " +
                "object would exercise the path that was never broken.");
            harness.Pump();

            Assert.That(relaunched.State, Is.EqualTo(PrivateSessionClientState.Seated),
                "The relaunched process is back in its own seat.");
            Assert.That(relaunched.GrantedSeat.Seat.Value, Is.EqualTo(seat));
            Assert.That(Occupancy(harness, seat), Is.EqualTo(SeatOccupancyState.Claimed));
            Assert.That(relaunched.ConnectionEpoch,
                Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch + 1),
                "It returned by reclaiming, not by claiming: only a reclaim rotates the epoch, so " +
                "this is also what says the retired connection's frames are unacceptable now.");

            var events = harness.Session.DrainEvents();
            Assert.That(events.Count(record =>
                    record.Kind == PrivateSessionEventKind.SeatReclaimed &&
                    record.Seat.Value == seat),
                Is.EqualTo(1),
                "and the host recorded it as a reclaim rather than a first claim.");

            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore),
                "Nothing moved while the seat was away.");
            harness.AssertConverged();
            harness.DriveThroughWire(6);
            harness.AssertConverged();
            Assert.That(harness.MatchHost.Version, Is.GreaterThan(versionBefore),
                "and the returned seat can act again, which the whole exercise is for.");
        }

        /// <summary>
        /// The user's own rule, both ways in one case: LEAVE forfeits the seat and a crash does not.
        /// </summary>
        /// <remarks>
        /// <para>Stating both halves together is deliberate. A build that forfeited every departure
        /// would pass a leave-only case while throwing away exactly the protection this milestone
        /// exists to give, and a build that reserved every departure would pass a crash-only case
        /// while leaving the leaver's seat stuck - which is the behaviour the host had to re-host to
        /// escape on the night of the playtest.</para>
        /// <para>The wire already told the two apart: a deliberate leave sends `Goodbye` and a lost
        /// link sends nothing. No message kind was added for this.</para>
        /// </remarks>
        [Test]
        public void LeavingForfeitsTheSeat_WhileACrashKeepsItReservedForItsPlayer()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(8);

            // The leaver: a deliberate departure, which gives the seat up.
            var leaverOrdinal = harness.OrdinalOf(ReturningSeat);
            var leaverSeat = harness.SeatOf(leaverOrdinal);
            harness.Client(leaverOrdinal).SendGoodbye();
            harness.Pump();
            harness.DisconnectClient(leaverOrdinal);

            Assert.That(Occupancy(harness, leaverSeat), Is.EqualTo(SeatOccupancyState.Open),
                "Leaving is a choice, and the seat it gives up is claimable again - the leaver " +
                "chose the exposure.");

            // The crashed player: no Goodbye, so the seat is held for them.
            var crashedOrdinal = harness.OrdinalOf("blue-player-01");
            var crashedSeat = harness.SeatOf(crashedOrdinal);
            harness.DisconnectClient(crashedOrdinal);
            Assert.That(Occupancy(harness, crashedSeat), Is.EqualTo(SeatOccupancyState.Reserved),
                "A dropped link is not a choice, so the seat stays held.");

            // The forfeited seat takes an ordinary first claim from a client that has never held
            // it - which is what "claimable again" has to mean to be worth anything.
            var newcomer = harness.Connect(harness.NextOrdinal(), leaverSeat);
            harness.Pump();
            Assert.That(newcomer.State, Is.EqualTo(PrivateSessionClientState.Seated),
                "A forfeited seat is claimable by an ordinary keyed claim.");
            Assert.That(Occupancy(harness, leaverSeat), Is.EqualTo(SeatOccupancyState.Claimed));

            // And the crashed player's seat is NOT claimable by anyone else, which is the half the
            // capability exists to protect: every invited player holds the session key.
            var opportunist = harness.Connect(
                harness.NextOrdinal(), crashedSeat,
                machineStore: new InMemoryRetainedSeatCapabilityStore());
            harness.Pump();
            Assert.That(opportunist.State, Is.Not.EqualTo(PrivateSessionClientState.Seated),
                "Holding the session key is an invitation, never authority over somebody's seat.");
            Assert.That(Occupancy(harness, crashedSeat), Is.EqualTo(SeatOccupancyState.Reserved),
                "and the refusal left the reservation exactly as it was.");
        }

        /// <summary>
        /// The host's deliberate escape hatch: a reserved seat handed back by hand, for the evening
        /// where a player's machine is gone and no stored capability can exist.
        /// </summary>
        /// <remarks>
        /// It is the one road back that proves nothing about who is taking the seat, so what makes
        /// it safe is that a person with the authority chose it. It is never automatic and never on
        /// a timer: the reservation's "no clock anywhere in it" property is a feature, and an
        /// auto-release would hand a dropped player's seat to whoever reconnected first.
        /// </remarks>
        [Test]
        public void TheHostCanReleaseAReservedSeatByHand_ButNeverALiveOne()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(8);

            var liveSeat = new SeatId(harness.SeatOf(harness.OrdinalOf("red-player-01")));
            Assert.That(harness.Session.ReleaseSeat(liveSeat), Is.False,
                "A live player is never displaced by this control, any more than by a returning " +
                "one: the host wanting the seat back is not a reason to take it from somebody in it.");

            var awayOrdinal = harness.OrdinalOf(ReturningSeat);
            var awaySeat = harness.SeatOf(awayOrdinal);
            harness.DisconnectClient(awayOrdinal);
            Assert.That(Occupancy(harness, awaySeat), Is.EqualTo(SeatOccupancyState.Reserved));

            Assert.That(harness.Session.ReleaseSeat(new SeatId(awaySeat)), Is.True);
            Assert.That(Occupancy(harness, awaySeat), Is.EqualTo(SeatOccupancyState.Open),
                "The released seat is plainly claimable again.");
            Assert.That(harness.Session.ReleaseSeat(new SeatId(awaySeat)), Is.False,
                "and releasing an already-open seat reports that it released nothing.");

            var newcomer = harness.Connect(
                harness.NextOrdinal(), awaySeat,
                machineStore: new InMemoryRetainedSeatCapabilityStore());
            harness.Pump();
            Assert.That(newcomer.State, Is.EqualTo(PrivateSessionClientState.Seated),
                "which is the point of the control: somebody can sit down again tonight.");
        }

        /// <summary>
        /// Milestone 8.35k. A capability left over from a FINISHED session must not stop its owner
        /// joining the next one.
        /// </summary>
        /// <remarks>
        /// <para>Found by the six-client rehearsal's new cohort and reproduced here, because it is
        /// the defect this milestone could most easily have shipped: the automatic road back, armed
        /// on the stored entry alone, made every SECOND session on a machine fail. All six clients
        /// of the rehearsal's host-loss cohort were refused <c>ReconnectNotAuthorized</c> and closed
        /// — a player who finished one game could never join another, which is worse than the bug
        /// being fixed.</para>
        /// <para>The cause is that <c>PrivateSessionIdentity.Correlation</c> is deterministic over
        /// the content tuple - its own record says two processes composed from the same content
        /// derive the same value and draw no entropy - so it names a BUILD, not a session, and an
        /// entry keyed by it outlives the session that wrote it. The roster the Welcome already
        /// carries is the evidence that is about THIS session, so a reclaim is armed only against a
        /// seat the host says it is holding.</para>
        /// <para>No tier below this one could have found it: the wire harness gives each seat a
        /// fresh store per harness, so every fixture is a machine's first session.</para>
        /// </remarks>
        [Test]
        public void AStoredCapabilityFromAnEndedSession_DoesNotBlockJoiningTheNextOne()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var seat = new SeatId(ReturningSeat);

            // Exactly what a previous session on this machine left behind: the correlation is a
            // function of the content, so a finished session keyed its entry at this very address.
            const string stale = "a-capability-a-finished-session-minted";
            var leftOver = new InMemoryRetainedSeatCapabilityStore();
            leftOver.Retain(harness.Identity.Correlation, seat, stale);

            var joining = harness.Connect(harness.NextOrdinal(), ReturningSeat, machineStore: leftOver);
            harness.Pump();

            Assert.That(joining.State, Is.EqualTo(PrivateSessionClientState.Seated),
                "The seat is Open, so this is a first join and the leftover capability is about a " +
                "session that no longer exists. Presenting it would be refused and the connection " +
                "closed, with no road back at all.");
            Assert.That(joining.IsReconnecting, Is.False,
                "and it must not even have claimed to be returning.");
            Assert.That(Occupancy(harness, ReturningSeat), Is.EqualTo(SeatOccupancyState.Claimed));
            Assert.That(leftOver.Find(harness.Identity.Correlation, seat), Is.Not.EqualTo(stale),
                "The stale entry is gone rather than left on disk for the life of the build - " +
                "replaced, here, by the capability this new session's own grant issued.");
        }

        /// <summary>
        /// Milestone 8.35k. Two names for one event value is a defect C# accepts in silence, and
        /// this milestone shipped one for a while: <c>SeatReleased</c> was first written as a second
        /// <c>15</c> beside <c>MatchStarted</c>.
        /// </summary>
        /// <remarks>
        /// <para>What that cost is worth stating, because it is why a structural check earns its
        /// place over a behavioural one. Every consumer of this enum compares values, not names, so
        /// a released seat WAS a match starting: <c>PrivateOnlineDrivers.LogHostEvent</c> would have
        /// written <c>MATCH_STARTED</c> when the host released a reservation, and the rehearsal
        /// driver's convergence test reads that same value to decide the match has begun. A
        /// milestone whose whole purpose was to make the host log readable would have made it lie.
        /// </para>
        /// <para>No behavioural case catches this cheaply, because the collision makes the two
        /// indistinguishable to the assertion as well: a fixture asserting "a release records
        /// SeatReleased" passes on a build where that value also means something else. The
        /// distinctness has to be asserted about the type itself.</para>
        /// </remarks>
        [Test]
        public void TheSessionsEventKinds_AreDistinct_SoAReleaseIsNeverReadAsAMatchStarting()
        {
            var collisions = Enum.GetNames(typeof(PrivateSessionEventKind))
                .GroupBy(name => (int)Enum.Parse(typeof(PrivateSessionEventKind), name))
                .Where(group => group.Count() > 1)
                .Select(group => group.Key + " is " + string.Join(" and ", group.OrderBy(x => x)))
                .ToArray();

            Assert.That(collisions, Is.Empty,
                "Every consumer compares values, not names, so two names for one value are one " +
                "event: " + string.Join("; ", collisions));
            Assert.That((int)PrivateSessionEventKind.SeatReleased,
                Is.Not.EqualTo((int)PrivateSessionEventKind.MatchStarted),
                "and these two in particular, because a seat being given up and a match beginning " +
                "are the pair this milestone made the host log tell apart.");
        }

        /// <summary>
        /// Milestone 8.35k. A retained capability answers for exactly one session and one seat.
        /// </summary>
        /// <remarks>
        /// The session half is the security property: an entry scoped to one session correlation
        /// cannot be replayed into a different session. The seat half is what keeps the store from
        /// handing one seat's secret to another. Both rest on the key separating its two parts,
        /// which until this milestone it did not - the parts were concatenated bare, so
        /// <c>("AB", "C")</c> and <c>("A", "BC")</c> named one entry.
        /// </remarks>
        [Test]
        public void ARetainedCapability_AnswersForItsOwnSessionAndSeatOnly()
        {
            var store = new InMemoryRetainedSeatCapabilityStore();
            store.Retain("AB", new SeatId("C"), "first");
            store.Retain("A", new SeatId("BC"), "second");

            Assert.That(store.Count, Is.EqualTo(2),
                "The two parts are keyed separately, so these are two entries and not one.");
            Assert.That(store.Find("AB", new SeatId("C")), Is.EqualTo("first"));
            Assert.That(store.Find("A", new SeatId("BC")), Is.EqualTo("second"));

            Assert.That(store.Find("a-different-session", new SeatId("C")), Is.Empty,
                "An entry cannot be replayed into another session, which is what scoping it to " +
                "the correlation is for.");
            Assert.That(store.Find("AB", new SeatId("a-different-seat")), Is.Empty,
                "and one seat's secret is never answered for another.");

            store.Forget("AB", new SeatId("C"));
            Assert.That(store.Find("AB", new SeatId("C")), Is.Empty);
            Assert.That(store.Find("A", new SeatId("BC")), Is.EqualTo("second"),
                "and forgetting one entry leaves its neighbour alone.");
        }

        private static SeatOccupancyState Occupancy(PrivateOnlineWireHarness harness, string seat) =>
            harness.Session.Roster.Single(row => row.Seat.Value == seat).Occupancy;
    }
}
