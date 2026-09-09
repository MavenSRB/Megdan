using System;
using System.Linq;
using GuardsDigital.Application;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.31. Admission by tuple and key: an identical tuple is welcomed, and every
    /// element that can differ is refused by its own name - before the client learns anything
    /// else about the session.
    /// </summary>
    public sealed class PrivateSessionAdmissionTests
    {
        [Test]
        public void AnIdenticalTuple_IsChallengedThenWelcomed()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.OnConnected(1, 0);

            var challenge = driver.Outbox.LastFrameFor(1);
            Assert.That(challenge.Kind, Is.EqualTo(SessionMessageKind.Challenge),
                "The host speaks first, with the challenge.");

            driver.Hello(1, driver.Identity, string.Empty);
            var welcome = driver.Outbox.LastFrameFor(1);
            Assert.That(welcome.Kind, Is.EqualTo(SessionMessageKind.Welcome));
            Assert.That(PrivateSessionFrameCodec.TryDecodeWelcome(welcome.Payload, out var decoded), Is.True);
            Assert.That(decoded.SessionCorrelation, Is.EqualTo(driver.Session.SessionCorrelation));
            Assert.That(decoded.Roster.Count, Is.EqualTo(6));
            Assert.That(decoded.Roster.All(entry => entry.Occupancy == SeatOccupancyState.Open), Is.True);
            Assert.That(driver.Outbox.WasClosed(1), Is.False);
        }

        [Test]
        public void EachTupleElement_IsRefusedByItsOwnName()
        {
            var cases = new (Func<PrivateSessionTestDriver, PrivateSessionIdentity> Variant,
                SessionRejectionReason Expected)[]
            {
                (driver => driver.IdentityWith(family: "guards-other"),
                    SessionRejectionReason.ProtocolFamilyMismatch),
                (driver => driver.IdentityWith(version: driver.Identity.ProtocolVersion + 1),
                    SessionRejectionReason.ProtocolVersionMismatch),
                (driver => driver.IdentityWith(build: "other-build"),
                    SessionRejectionReason.BuildIdentityMismatch),
                (driver => driver.IdentityWith(schema: 33),
                    SessionRejectionReason.SnapshotSchemaMismatch),
                (driver => driver.IdentityWith(mapId: "other-map"),
                    SessionRejectionReason.MapRevisionMismatch),
                (driver => driver.IdentityWith(mapRevision: 999),
                    SessionRejectionReason.MapRevisionMismatch),
                (driver => driver.IdentityWith(digest: "other-digest"),
                    SessionRejectionReason.ContentDigestMismatch)
            };

            foreach (var (variant, expected) in cases)
            {
                var driver = PrivateSessionTestDriver.Compose();
                driver.Session.OnConnected(1, 0);
                driver.Hello(1, variant(driver), string.Empty);

                var reject = driver.Outbox.RejectFor(1);
                Assert.That(reject.Reason, Is.EqualTo(expected));
                Assert.That(driver.Outbox.WasClosed(1), Is.True);
                Assert.That(driver.Outbox.FramesFor(1)
                        .Count(frame => frame.Kind == SessionMessageKind.Welcome),
                    Is.Zero, "A refused client is told the reason and nothing else.");
                Assert.That(driver.Session.ClaimedSeatCount, Is.Zero);
            }
        }

        [Test]
        public void ARefusal_NamesNoSeatAndNoRoster()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.OnConnected(1, 0);
            driver.Hello(1, driver.IdentityWith(build: "other-build"), string.Empty);

            var reject = driver.Outbox.RejectFor(1);
            foreach (var entry in driver.Session.Roster)
            {
                Assert.That(reject.Detail, Does.Not.Contain(entry.Seat.Value));
                Assert.That(reject.Detail, Does.Not.Contain(entry.Hero.Value));
            }
        }

        [Test]
        public void AKeyedSession_RefusesAWrongProofAndAdmitsTheRightOne()
        {
            var wrong = PrivateSessionTestDriver.Compose("attic-key");
            wrong.Session.OnConnected(1, 0);
            wrong.Hello(1, wrong.Identity,
                PrivateSessionKeyProof.Compute(wrong.Nonces.Nonce, "not-the-key"));
            Assert.That(wrong.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.SessionKeyMismatch));
            Assert.That(wrong.Outbox.WasClosed(1), Is.True);

            var missing = PrivateSessionTestDriver.Compose("attic-key");
            missing.Session.OnConnected(1, 0);
            missing.Hello(1, missing.Identity, string.Empty);
            Assert.That(missing.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.SessionKeyMismatch));

            var right = PrivateSessionTestDriver.Compose("attic-key");
            right.Admit(1);
        }

        [Test]
        public void AKeylessSession_IgnoresWhateverProofIsOffered()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.OnConnected(1, 0);
            driver.Hello(1, driver.Identity, "unsolicited-proof");

            Assert.That(driver.Outbox.LastFrameFor(1).Kind, Is.EqualTo(SessionMessageKind.Welcome));
        }

        [Test]
        public void AHandshake_ThatMissesItsDeadline_IsClosedByTheSweep()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.OnConnected(1, 0);

            var justInside = PrivateSessionProtocol.HandshakeTimeoutSeconds * TimeSpan.TicksPerSecond;
            driver.Session.Sweep(justInside);
            Assert.That(driver.Outbox.WasClosed(1), Is.False,
                "The deadline itself is still inside the handshake window.");

            driver.Session.Sweep(justInside + 1);
            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.HandshakeTimeout));
            Assert.That(driver.Outbox.WasClosed(1), Is.True);
        }

        [Test]
        public void AnAdmittedConnection_IsNotSweptByTheHandshakeDeadline()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Admit(1);

            driver.Session.Sweep(long.MaxValue);
            Assert.That(driver.Outbox.WasClosed(1), Is.False);
        }

        [Test]
        public void ConnectionsPastTheBound_AreRefusedAtAccept()
        {
            var driver = PrivateSessionTestDriver.Compose();
            for (var ordinal = 1; ordinal <= PrivateSessionProtocol.MaxConcurrentConnections; ordinal++)
            {
                driver.Session.OnConnected(ordinal, 0);
            }

            var straggler = PrivateSessionProtocol.MaxConcurrentConnections + 1;
            driver.Session.OnConnected(straggler, 0);
            Assert.That(driver.Outbox.RejectFor(straggler).Reason,
                Is.EqualTo(SessionRejectionReason.TooManyConnections));
            Assert.That(driver.Outbox.WasClosed(straggler), Is.True);
        }

        [Test]
        public void AMalformedHello_IsRefusedAsMalformed()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.OnConnected(1, 0);
            driver.Session.OnFrame(1, new SessionFrame(SessionMessageKind.Hello, new byte[] { 0x01 }));

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.MalformedFrame));
        }

        [Test]
        public void AClaimBeforeAdmission_IsUnexpected()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.OnConnected(1, 0);
            driver.Claim(1, "red-player-01");

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.UnexpectedMessage));
            Assert.That(driver.Session.ClaimedSeatCount, Is.Zero);
        }

        [Test]
        public void AStreamViolation_IsRefusedWithTheTransportsReason()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.OnConnected(1, 0);
            driver.Session.OnStreamRejected(1, SessionRejectionReason.OversizedFrame);

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.OversizedFrame));
            Assert.That(driver.Outbox.WasClosed(1), Is.True);
        }

        [Test]
        public void ACompatibleConnection_ArrivingWithNoOpenSeat_IsRefusedAsFull()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var seats = driver.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
            for (var index = 0; index < seats.Length; index++)
            {
                driver.Seat(index + 1, seats[index]);
            }

            driver.Session.OnConnected(7, 0);
            driver.Hello(7, driver.Identity, string.Empty);
            Assert.That(driver.Outbox.RejectFor(7).Reason,
                Is.EqualTo(SessionRejectionReason.SessionFull));
            Assert.That(driver.Session.ClaimedSeatCount, Is.EqualTo(6));
        }
    }
}
