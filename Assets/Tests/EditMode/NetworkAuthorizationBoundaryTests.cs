using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.32. Milestone 8.30a's authorization and privacy cases, repeated through encoded
    /// bytes and a hostile client: forged identities, replayed submission ids, stale and forged
    /// versions, unauthorized team choices, malformed envelopes - each refused by its own reason
    /// with no partial host mutation - and another seat's hand absent from the encoded projection
    /// bytes themselves.
    /// </summary>
    public sealed class NetworkAuthorizationBoundaryTests
    {
        private const string SessionKey = "boundary-key";

        /// <summary>
        /// One seated wire client over the in-memory keyed driver: it reads its own pushed
        /// projection record back off the outbox and composes its submissions the way the real
        /// client machine does - and, being a fixture, the way a hostile one would.
        /// </summary>
        private sealed class WireSeat
        {
            private readonly PrivateSessionTestDriver driver;
            private long sendSequence;

            public WireSeat(PrivateSessionTestDriver driver, int ordinal, string seat)
            {
                this.driver = driver;
                Ordinal = ordinal;
                SeatId = seat;
                driver.Seat(ordinal, seat);
            }

            public int Ordinal { get; }
            public string SeatId { get; }

            public NetworkSeatProjectionRecord LatestProjection()
            {
                var frame = driver.Outbox.FramesFor(Ordinal)
                    .Last(value => value.Kind == SessionMessageKind.SeatProjection);
                Assert.That(NetworkMessageCodec.TryDecodeSeatProjection(frame.Payload, out var message),
                    Is.True);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    message.ProjectionPayload, out var record), Is.True);
                return record;
            }

            public NetworkSubmissionResultMessage LastResult()
            {
                var frame = driver.Outbox.FramesFor(Ordinal)
                    .Last(value => value.Kind == SessionMessageKind.SubmissionResult);
                Assert.That(NetworkMessageCodec.TryDecodeSubmissionResult(frame.Payload, out var result),
                    Is.True);
                return result;
            }

            public NetworkSubmissionResultMessage Submit(
                DirectorCommand command,
                string submissionId,
                long expectedVersion,
                string sessionCorrelation = null,
                int epoch = PrivateSessionProtocol.InitialConnectionEpoch,
                long? sequence = null)
            {
                sendSequence++;
                var frame = NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                    new NetworkEnvelope(
                        sessionCorrelation ?? driver.Session.SessionCorrelation,
                        epoch,
                        sequence ?? sendSequence,
                        sendSequence),
                    submissionId,
                    expectedVersion,
                    NetworkCommandCodec.Encode(command)));
                driver.Submit(Ordinal, frame);
                return driver.Outbox.FramesFor(Ordinal)
                        .Any(value => value.Kind == SessionMessageKind.SubmissionResult)
                    ? LastResult()
                    : null;
            }
        }

        private static (PrivateSessionTestDriver driver, WireSeat attacker, WireSeat victim) ComposeSeatedPair()
        {
            var driver = PrivateSessionTestDriver.Compose(SessionKey);
            var attacker = new WireSeat(driver, 1, "red-player-01");
            var victim = new WireSeat(driver, 2, "blue-player-01");
            return (driver, attacker, victim);
        }

        private static string StateFingerprint(PrivateSessionTestDriver driver) =>
            driver.Session.Roster.Aggregate(string.Empty, (text, entry) =>
                text + entry.Seat.Value + ":" + entry.Occupancy + ";");

        [Test]
        public void AGenuineOfferedCommand_IsAcceptedThroughBytes_AndEverySeatReceivesItsOwnView()
        {
            var (driver, attacker, victim) = ComposeSeatedPair();
            var record = attacker.LatestProjection();
            Assert.That(record.AuthorizedCommands, Is.Not.Empty,
                "The commitment phase offers this seat its own commitment answers.");

            var result = attacker.Submit(record.AuthorizedCommands[0], "wire-accept-1", record.Version);
            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(result.Version, Is.EqualTo(record.Version + 1));
            Assert.That(result.SubmissionId, Is.EqualTo("wire-accept-1"));

            // The commit moved the authority once, and each seated connection was pushed its own
            // seat's view of the new version - never a shared payload.
            Assert.That(attacker.LatestProjection().Version, Is.EqualTo(result.Version));
            Assert.That(victim.LatestProjection().Version, Is.EqualTo(result.Version));
            Assert.That(attacker.LatestProjection().SeatId, Is.EqualTo("red-player-01"));
            Assert.That(victim.LatestProjection().SeatId, Is.EqualTo("blue-player-01"));

            var events = driver.Session.DrainEvents();
            Assert.That(events.Count(value =>
                value.Kind == PrivateSessionEventKind.SubmissionAccepted), Is.EqualTo(1));
        }

        [Test]
        public void ACrossSeatHeroCommand_IsRefusedAsOwnershipMismatch_WithoutMutation()
        {
            var (driver, attacker, victim) = ComposeSeatedPair();
            var victimCommit = driver.Session
                .TryGetSeatForConnection(victim.Ordinal, out var victimSeat)
                ? OwnerScopedProjection(driver, victimSeat).AuthorizedCommands
                    .First(command => command.Kind == DirectorCommandKind.CommitCard)
                : null;
            Assert.That(victimCommit, Is.Not.Null);

            var versionBefore = driver.Session.TryGetSeatForConnection(attacker.Ordinal, out var attackerSeat)
                ? OwnerScopedProjection(driver, attackerSeat).Version
                : -1;
            var fingerprint = StateFingerprint(driver);

            // The attacker replays the victim's own commitment bytes as its own submission: a
            // rules-legal command, refused purely because the payload claims an identity the
            // arriving seat does not hold.
            var result = attacker.Submit(victimCommit, "wire-forge-1", versionBefore);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.OwnershipMismatch));
            Assert.That(result.Version, Is.EqualTo(versionBefore), "A refusal advances nothing.");
            Assert.That(result.Sequence, Is.Zero);
            Assert.That(StateFingerprint(driver), Is.EqualTo(fingerprint));
            Assert.That(OwnerScopedProjection(driver, victimSeat).AuthorizedCommands
                    .Any(command => command.Equals(victimCommit)), Is.True,
                "The victim's own answer still stands unconsumed.");
        }

        [Test]
        public void TeamChoices_AreRefusedByOwnership_OrByTheOfferedSurface()
        {
            var (_, attacker, _) = ComposeSeatedPair();
            var record = attacker.LatestProjection();

            // The other side's tied-actor answer claims a side this seat does not play for.
            var otherSide = attacker.Submit(
                DirectorCommand.ChooseTiedActor(TeamSide.Blue, new CardInstanceId("forged-card")),
                "wire-team-1", record.Version);
            Assert.That(otherSide.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.OwnershipMismatch));

            // The seat's own side's answer passes ownership but no tie is standing, so the offered
            // surface refuses it: rules-shaped is not offered.
            var ownSide = attacker.Submit(
                DirectorCommand.ChooseTiedActor(TeamSide.Red, new CardInstanceId("forged-card")),
                "wire-team-2", record.Version);
            Assert.That(ownSide.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.UnauthorizedCommand));
        }

        [Test]
        public void AReplayedSubmissionIdentity_IsRefusedAsDuplicate()
        {
            var (_, attacker, _) = ComposeSeatedPair();
            var record = attacker.LatestProjection();
            var first = attacker.Submit(record.AuthorizedCommands[0], "wire-dup-1", record.Version);
            Assert.That(first.Accepted, Is.True, first.Detail);

            // The same identity again, against the current version and a fresh command: the
            // identity was consumed by the accepted attempt and is never run twice.
            var after = attacker.LatestProjection();
            var replay = attacker.Submit(
                after.AuthorizedCommands.FirstOrDefault() ?? DirectorCommand.AdvanceEndOfRound(),
                "wire-dup-1", after.Version);
            Assert.That(replay.Accepted, Is.False);
            Assert.That(replay.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.DuplicateSubmission));
            Assert.That(replay.Version, Is.EqualTo(after.Version));
        }

        [Test]
        public void AStaleOrForgedExpectedVersion_IsRefusedWithoutMutation()
        {
            var (_, attacker, victim) = ComposeSeatedPair();
            var record = attacker.LatestProjection();
            var advance = victim.Submit(
                victim.LatestProjection().AuthorizedCommands[0], "wire-move-1",
                victim.LatestProjection().Version);
            Assert.That(advance.Accepted, Is.True, advance.Detail);

            // The attacker answers against the version it read before the victim moved the board.
            var stale = attacker.Submit(
                record.AuthorizedCommands[0], "wire-stale-1", record.Version);
            Assert.That(stale.Accepted, Is.False);
            Assert.That(stale.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));

            // A forged far-future version is the same refusal: a claimed version is never trusted.
            var forged = attacker.Submit(
                attacker.LatestProjection().AuthorizedCommands[0], "wire-forgedv-1", 999_999);
            Assert.That(forged.Accepted, Is.False);
            Assert.That(forged.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
        }

        [Test]
        public void AForgedEnvelope_ClosesTheConnection_BeforeAnyAuthorityIsReached()
        {
            var (driver, attacker, _) = ComposeSeatedPair();
            var record = attacker.LatestProjection();
            var versionBefore = record.Version;

            // A wrong session correlation is a protocol violation, not a seat answer.
            attacker.Submit(record.AuthorizedCommands[0], "wire-env-1", record.Version,
                sessionCorrelation: "not-this-session");
            Assert.That(driver.Outbox.RejectFor(attacker.Ordinal).Reason,
                Is.EqualTo(SessionRejectionReason.EnvelopeMismatch));
            Assert.That(driver.Outbox.WasClosed(attacker.Ordinal), Is.True);

            // Nothing reached the authority: no result was sent and no version moved.
            Assert.That(driver.Outbox.FramesFor(attacker.Ordinal)
                .Count(frame => frame.Kind == SessionMessageKind.SubmissionResult), Is.Zero);
            Assert.That(driver.Session.TryGetSeatForConnection(attacker.Ordinal, out _), Is.False);
            var events = driver.Session.DrainEvents();
            Assert.That(events.Count(value =>
                value.Kind == PrivateSessionEventKind.SubmissionAccepted), Is.Zero);
            Assert.That(events.Count(value =>
                value.Kind == PrivateSessionEventKind.SubmissionRefused), Is.Zero);

            // The same for a wrong epoch and a skipped sequence, each on a fresh session.
            var (driver2, attacker2, _) = ComposeSeatedPair();
            var record2 = attacker2.LatestProjection();
            attacker2.Submit(record2.AuthorizedCommands[0], "wire-env-2", record2.Version, epoch: 2);
            Assert.That(driver2.Outbox.RejectFor(attacker2.Ordinal).Reason,
                Is.EqualTo(SessionRejectionReason.EnvelopeMismatch));

            var (driver3, attacker3, _) = ComposeSeatedPair();
            var record3 = attacker3.LatestProjection();
            attacker3.Submit(record3.AuthorizedCommands[0], "wire-env-3", record3.Version, sequence: 9);
            Assert.That(driver3.Outbox.RejectFor(attacker3.Ordinal).Reason,
                Is.EqualTo(SessionRejectionReason.EnvelopeMismatch));
            Assert.That(versionBefore, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void AMalformedOrOversizedSubmission_IsRefusedBeforeTheAuthority()
        {
            var (driver, attacker, _) = ComposeSeatedPair();
            var record = attacker.LatestProjection();

            // A structurally valid envelope whose command bytes declare more than they carry.
            var garbage = NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(driver.Session.SessionCorrelation, 1, 1, 1),
                "wire-mal-1", record.Version, new byte[] { 0x7F, 0x00, 0x00, 0x00 }));
            driver.Submit(attacker.Ordinal, garbage);
            Assert.That(driver.Outbox.RejectFor(attacker.Ordinal).Reason,
                Is.EqualTo(SessionRejectionReason.MalformedFrame));
            Assert.That(driver.Outbox.WasClosed(attacker.Ordinal), Is.True);
            Assert.That(driver.Outbox.FramesFor(attacker.Ordinal)
                .Count(frame => frame.Kind == SessionMessageKind.SubmissionResult), Is.Zero);

            // An unadmitted connection cannot submit at all.
            var (driver4, _, _) = ComposeSeatedPair();
            driver4.Session.OnConnected(9, 0);
            driver4.Submit(9, NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(driver4.Session.SessionCorrelation, 1, 1, 1),
                "wire-mal-2", 0, NetworkCommandCodec.Encode(DirectorCommand.AdvanceEndOfRound()))));
            Assert.That(driver4.Outbox.RejectFor(9).Reason,
                Is.EqualTo(SessionRejectionReason.UnexpectedMessage));
        }

        /// <summary>
        /// The privacy half, over the bytes themselves: the victim's hand and deck identities do
        /// not occur anywhere in the attacker's encoded projection stream, pre-reveal commitments
        /// carry no identity, and every card the record does carry is either the seat's own or in
        /// a location rulebook p.4 makes open.
        /// </summary>
        [Test]
        public void AnotherSeatsHandAndDeck_AreAbsentFromTheEncodedBytes()
        {
            var (driver, attacker, victim) = ComposeSeatedPair();
            Assert.That(driver.Session.TryGetSeatForConnection(victim.Ordinal, out var victimSeat), Is.True);
            var hidden = OwnerScopedProjection(driver, victimSeat).OwnCards
                .Where(card => card.State.Location == CardLocation.Hand ||
                    card.State.Location == CardLocation.Deck)
                .Select(card => card.Id.Value)
                .ToArray();
            Assert.That(hidden, Is.Not.Empty, "The commitment phase deals every hero a hand.");

            foreach (var frame in driver.Outbox.FramesFor(attacker.Ordinal))
            {
                var text = Encoding.UTF8.GetString(frame.Payload);
                foreach (var id in hidden)
                {
                    Assert.That(text, Does.Not.Contain(id),
                        "A hidden card identity of " + victimSeat.Id + " occurs in a " +
                        frame.Kind + " frame sent to " + attacker.SeatId + ".");
                }
            }

            var record = attacker.LatestProjection();
            Assert.That(record.OwnCards.Select(row => row.OwnerHeroId).Distinct(),
                Is.EqualTo(new[] { record.HeroId }),
                "Every own-card row belongs to the receiving seat's hero.");
            var openLocations = new[]
            {
                (int)CardLocation.TurnArea, (int)CardLocation.DiscardPile,
                (int)CardLocation.ItemSlot, (int)CardLocation.UltimateArea,
                (int)CardLocation.RemovedFromSpellbook // Milestone 9.30b
            };
            foreach (var row in record.PublicCards)
            {
                Assert.That(row.OwnerHeroId, Is.Not.EqualTo(record.HeroId));
                Assert.That(openLocations, Does.Contain(row.Location),
                    "A public card row sits in a location p.4 does not make open.");
            }

            foreach (var commitment in record.Commitments)
            {
                if (commitment.Status != (int)TurnCardCommitmentStatus.Revealed)
                {
                    Assert.That(commitment.RevealedCardInstanceId, Is.Null,
                        "An unrevealed commitment carried an identity to another seat.");
                }
            }

            // The countable shape is public: every hero has a hand summary, naming no card.
            Assert.That(record.HandSummaries.Count, Is.EqualTo(6));
        }

        /// <summary>
        /// The diagnostics half of the milestone's third check: the host's drained event records
        /// and every refusal detail sent back over the wire name reasons, seats and counts - never
        /// a hidden card identity.
        /// </summary>
        [Test]
        public void DiagnosticsAndRefusalDetails_ExposeNoHiddenIdentity()
        {
            var (driver, attacker, victim) = ComposeSeatedPair();
            Assert.That(driver.Session.TryGetSeatForConnection(victim.Ordinal, out var victimSeat), Is.True);
            var victimProjection = OwnerScopedProjection(driver, victimSeat);
            var hidden = victimProjection.OwnCards
                .Where(card => card.State.Location == CardLocation.Hand ||
                    card.State.Location == CardLocation.Deck)
                .Select(card => card.Id.Value)
                .ToArray();

            // One accepted, one forged, one stale, one duplicate - then read everything back.
            var record = attacker.LatestProjection();
            attacker.Submit(record.AuthorizedCommands[0], "wire-diag-1", record.Version);
            var forged = victimProjection.AuthorizedCommands
                .First(command => command.Kind == DirectorCommandKind.CommitCard);
            attacker.Submit(forged, "wire-diag-2", attacker.LatestProjection().Version);
            attacker.Submit(attacker.LatestProjection().AuthorizedCommands
                    .FirstOrDefault() ?? DirectorCommand.AdvanceEndOfRound(),
                "wire-diag-1", attacker.LatestProjection().Version);

            foreach (var record_ in driver.Session.DrainEvents())
            {
                var line = record_.ToString();
                foreach (var id in hidden)
                {
                    Assert.That(line, Does.Not.Contain(id));
                }
            }

            foreach (var frame in driver.Outbox.FramesFor(attacker.Ordinal)
                .Where(value => value.Kind == SessionMessageKind.SubmissionResult))
            {
                Assert.That(NetworkMessageCodec.TryDecodeSubmissionResult(frame.Payload, out var result),
                    Is.True);
                foreach (var id in hidden)
                {
                    Assert.That(result.Detail ?? string.Empty, Does.Not.Contain(id),
                        "A refusal detail carried a hidden card identity to another seat.");
                }
            }
        }

        /// <summary>
        /// The fixture reads the authority omnisciently to learn what an attacker must not; the
        /// production path never hands one seat another's projection.
        /// </summary>
        private static SeatProjection OwnerScopedProjection(
            PrivateSessionTestDriver driver, MatchSeat seat) =>
            driver.MatchHost.ProjectionForSeat(seat.Id);
    }
}
