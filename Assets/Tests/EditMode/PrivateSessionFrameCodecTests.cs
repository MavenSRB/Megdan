using System;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.31. The session frame and payload codecs: every kind round-trips, and every
    /// way a wire byte sequence can lie is refused where it is read.
    /// </summary>
    /// <remarks>
    /// The oversized-frame cases assert the rule the amended record states outright: a declared
    /// length is judged against the bound from the twelve header bytes alone, so the refusal
    /// happens before any payload buffer for it can exist.
    /// </remarks>
    public sealed class PrivateSessionFrameCodecTests
    {
        private static PrivateSessionIdentity Identity() => new PrivateSessionIdentity(
            PrivateSessionProtocol.FamilyName, PrivateSessionProtocol.Version,
            "build-a", 34, "official-single-lane", 1, "3v3-quick", "digest-a");

        private static SessionFrame OneFrame(byte[] encoded)
        {
            var assembler = new PrivateSessionFrameAssembler();
            Assert.That(assembler.Add(encoded, 0, encoded.Length), Is.True);
            Assert.That(assembler.TryDequeue(out var frame), Is.True);
            Assert.That(assembler.TryDequeue(out _), Is.False, "Exactly one frame was encoded.");
            return frame;
        }

        [Test]
        public void Challenge_RoundTrips()
        {
            var nonce = Enumerable.Range(1, 16).Select(value => (byte)value).ToArray();
            var frame = OneFrame(PrivateSessionFrameCodec.ChallengeFrame(new SessionChallengeMessage(nonce)));

            Assert.That(frame.Kind, Is.EqualTo(SessionMessageKind.Challenge));
            Assert.That(PrivateSessionFrameCodec.TryDecodeChallenge(frame.Payload, out var decoded), Is.True);
            Assert.That(decoded.Nonce, Is.EqualTo(nonce));
        }

        [Test]
        public void Hello_RoundTripsTheWholeAdmissionTuple()
        {
            var frame = OneFrame(PrivateSessionFrameCodec.HelloFrame(
                new SessionHelloMessage(Identity(), "proof-1")));

            Assert.That(frame.Kind, Is.EqualTo(SessionMessageKind.Hello));
            Assert.That(PrivateSessionFrameCodec.TryDecodeHello(frame.Payload, out var decoded), Is.True);
            Assert.That(decoded.Identity.FirstMismatch(Identity()), Is.EqualTo(SessionRejectionReason.None));
            Assert.That(decoded.KeyProof, Is.EqualTo("proof-1"));
        }

        [Test]
        public void Welcome_RoundTripsCorrelationAndRoster()
        {
            var roster = new[]
            {
                new SeatRosterEntry(new SeatId("red-player-01"), new HeroId("hero-wasp"),
                    TeamSide.Red, SeatOccupancyState.Claimed),
                new SeatRosterEntry(new SeatId("blue-player-01"), new HeroId("hero-arien"),
                    TeamSide.Blue, SeatOccupancyState.Open)
            };
            var frame = OneFrame(PrivateSessionFrameCodec.WelcomeFrame(
                new SessionWelcomeMessage("correlation-1", roster)));

            Assert.That(PrivateSessionFrameCodec.TryDecodeWelcome(frame.Payload, out var decoded), Is.True);
            Assert.That(decoded.SessionCorrelation, Is.EqualTo("correlation-1"));
            Assert.That(decoded.Roster.Select(entry => entry.Seat.Value),
                Is.EqualTo(new[] { "red-player-01", "blue-player-01" }));
            Assert.That(decoded.Roster.Select(entry => entry.Occupancy),
                Is.EqualTo(new[] { SeatOccupancyState.Claimed, SeatOccupancyState.Open }));
        }

        [Test]
        public void Welcome_RoundTripsTheSupportedTenSeatRoster()
        {
            var roster = Enumerable.Range(1, 10)
                .Select(index => new SeatRosterEntry(
                    new SeatId("seat-" + index),
                    new HeroId("hero-" + index),
                    index <= 5 ? TeamSide.Red : TeamSide.Blue,
                    SeatOccupancyState.Open))
                .ToArray();
            var frame = OneFrame(PrivateSessionFrameCodec.WelcomeFrame(
                new SessionWelcomeMessage("large-map-correlation", roster)));

            Assert.That(PrivateSessionProtocol.MaxSeats, Is.EqualTo(10),
                "The wire bound must cover the supported 5v5 format.");
            Assert.That(PrivateSessionFrameCodec.TryDecodeWelcome(frame.Payload, out var decoded),
                Is.True, "A 5v5 host welcome must not be discarded as a malformed roster.");
            Assert.That(decoded.Roster.Count, Is.EqualTo(10));
        }

        [Test]
        public void Reject_RoundTripsReasonAndBoundedDetail()
        {
            var frame = OneFrame(PrivateSessionFrameCodec.RejectFrame(
                new SessionRejectMessage(SessionRejectionReason.SeatAlreadyClaimed, "held")));

            Assert.That(PrivateSessionFrameCodec.TryDecodeReject(frame.Payload, out var decoded), Is.True);
            Assert.That(decoded.Reason, Is.EqualTo(SessionRejectionReason.SeatAlreadyClaimed));
            Assert.That(decoded.Detail, Is.EqualTo("held"));
        }

        [Test]
        public void ClaimSeatAndSeatGranted_RoundTrip()
        {
            var claim = OneFrame(PrivateSessionFrameCodec.ClaimSeatFrame(
                new SeatClaimMessage(new SeatId("blue-player-02"))));
            Assert.That(PrivateSessionFrameCodec.TryDecodeClaimSeat(claim.Payload, out var decodedClaim), Is.True);
            Assert.That(decodedClaim.Seat.Value, Is.EqualTo("blue-player-02"));

            var granted = OneFrame(PrivateSessionFrameCodec.SeatGrantedFrame(new SeatGrantedMessage(
                new SeatRosterEntry(new SeatId("blue-player-02"), new HeroId("hero-brogan"),
                    TeamSide.Blue, SeatOccupancyState.Claimed),
                3, 6)));
            Assert.That(PrivateSessionFrameCodec.TryDecodeSeatGranted(granted.Payload, out var decodedGrant), Is.True);
            Assert.That(decodedGrant.Seat.Hero.Value, Is.EqualTo("hero-brogan"));
            Assert.That(decodedGrant.ClaimedSeats, Is.EqualTo(3));
            Assert.That(decodedGrant.TotalSeats, Is.EqualTo(6));
        }

        [Test]
        public void RosterUpdateAndGoodbye_RoundTrip()
        {
            var roster = new[]
            {
                new SeatRosterEntry(new SeatId("red-player-02"), new HeroId("hero-xargatha"),
                    TeamSide.Red, SeatOccupancyState.Reserved)
            };
            var update = OneFrame(PrivateSessionFrameCodec.RosterUpdateFrame(new SeatRosterMessage(roster)));
            Assert.That(PrivateSessionFrameCodec.TryDecodeRosterUpdate(update.Payload, out var decoded), Is.True);
            Assert.That(decoded.Roster.Single().Occupancy, Is.EqualTo(SeatOccupancyState.Reserved));

            var goodbye = OneFrame(PrivateSessionFrameCodec.GoodbyeFrame());
            Assert.That(goodbye.Kind, Is.EqualTo(SessionMessageKind.Goodbye));
            Assert.That(goodbye.Payload, Is.Empty);
        }

        [Test]
        public void Assembler_ReassemblesAFrameFromSingleByteChunks()
        {
            var encoded = PrivateSessionFrameCodec.ClaimSeatFrame(
                new SeatClaimMessage(new SeatId("red-player-03")));
            var assembler = new PrivateSessionFrameAssembler();
            foreach (var value in encoded)
            {
                Assert.That(assembler.Add(new[] { value }, 0, 1), Is.True);
            }

            Assert.That(assembler.TryDequeue(out var frame), Is.True);
            Assert.That(frame.Kind, Is.EqualTo(SessionMessageKind.ClaimSeat));
        }

        [Test]
        public void Assembler_RefusesABadMagic()
        {
            var encoded = PrivateSessionFrameCodec.GoodbyeFrame();
            encoded[0] = 0x00;
            var assembler = new PrivateSessionFrameAssembler();

            Assert.That(assembler.Add(encoded, 0, encoded.Length), Is.False);
            Assert.That(assembler.Rejection, Is.EqualTo(SessionRejectionReason.MalformedFrame));
        }

        [Test]
        public void Assembler_RefusesAForeignProtocolVersion()
        {
            var encoded = PrivateSessionFrameCodec.GoodbyeFrame();
            encoded[4] = 0xFF;
            var assembler = new PrivateSessionFrameAssembler();

            Assert.That(assembler.Add(encoded, 0, encoded.Length), Is.False);
            Assert.That(assembler.Rejection, Is.EqualTo(SessionRejectionReason.ProtocolVersionMismatch));
        }

        [Test]
        public void Assembler_RefusesAnUnknownKind()
        {
            var encoded = PrivateSessionFrameCodec.GoodbyeFrame();
            encoded[6] = 0x63;
            var assembler = new PrivateSessionFrameAssembler();

            Assert.That(assembler.Add(encoded, 0, encoded.Length), Is.False);
            Assert.That(assembler.Rejection, Is.EqualTo(SessionRejectionReason.MalformedFrame));
        }

        /// <summary>
        /// The pre-allocation rule itself: the twelve header bytes alone, declaring a payload past
        /// the bound, are refused - there are no payload bytes here to allocate for.
        /// </summary>
        [Test]
        public void Assembler_RefusesAnOversizedDeclaredLengthFromTheHeaderAlone()
        {
            var encoded = PrivateSessionFrameCodec.GoodbyeFrame();
            var header = encoded.Take(PrivateSessionProtocol.FrameHeaderBytes).ToArray();
            var huge = PrivateSessionProtocol.MaxPayloadBytes + 1;
            header[8] = (byte)(huge & 0xFF);
            header[9] = (byte)((huge >> 8) & 0xFF);
            header[10] = (byte)((huge >> 16) & 0xFF);
            header[11] = (byte)((huge >> 24) & 0xFF);
            var assembler = new PrivateSessionFrameAssembler();

            Assert.That(assembler.Add(header, 0, header.Length), Is.False);
            Assert.That(assembler.Rejection, Is.EqualTo(SessionRejectionReason.OversizedFrame));
        }

        [Test]
        public void Assembler_RefusesANegativeDeclaredLength()
        {
            var encoded = PrivateSessionFrameCodec.GoodbyeFrame();
            var header = encoded.Take(PrivateSessionProtocol.FrameHeaderBytes).ToArray();
            header[8] = 0xFF;
            header[9] = 0xFF;
            header[10] = 0xFF;
            header[11] = 0xFF;
            var assembler = new PrivateSessionFrameAssembler();

            Assert.That(assembler.Add(header, 0, header.Length), Is.False);
            Assert.That(assembler.Rejection, Is.EqualTo(SessionRejectionReason.MalformedFrame));
        }

        [Test]
        public void Assembler_HoldsAnIncompleteFrameWithoutInventingOne()
        {
            var encoded = PrivateSessionFrameCodec.ClaimSeatFrame(
                new SeatClaimMessage(new SeatId("red-player-01")));
            var assembler = new PrivateSessionFrameAssembler();

            Assert.That(assembler.Add(encoded, 0, encoded.Length - 1), Is.True);
            Assert.That(assembler.TryDequeue(out _), Is.False,
                "A frame missing its last byte is incomplete, not a frame.");
            Assert.That(assembler.Rejection, Is.EqualTo(SessionRejectionReason.None));
        }

        [Test]
        public void Assembler_RefusedStreamStaysRefusedAndDropsItsBytes()
        {
            var good = PrivateSessionFrameCodec.GoodbyeFrame();
            var bad = PrivateSessionFrameCodec.GoodbyeFrame();
            bad[0] = 0x00;
            var assembler = new PrivateSessionFrameAssembler();

            Assert.That(assembler.Add(bad, 0, bad.Length), Is.False);
            Assert.That(assembler.Add(good, 0, good.Length), Is.False,
                "A refused stream parses nothing further.");
            Assert.That(assembler.TryDequeue(out _), Is.False);
        }

        [Test]
        public void EncodeFrame_RefusesAnOversizedPayloadOutright()
        {
            Assert.Throws<ArgumentException>(() => PrivateSessionFrameCodec.EncodeFrame(
                SessionMessageKind.Hello, new byte[PrivateSessionProtocol.MaxPayloadBytes + 1]));
        }

        [Test]
        public void TryDecodeHello_RefusesATruncatedPayload()
        {
            var encoded = PrivateSessionFrameCodec.HelloFrame(new SessionHelloMessage(Identity(), "p"));
            var frame = OneFrame(encoded);
            var truncated = frame.Payload.Take(frame.Payload.Length - 2).ToArray();

            Assert.That(PrivateSessionFrameCodec.TryDecodeHello(truncated, out _), Is.False);
        }

        [Test]
        public void TryDecodeClaimSeat_RefusesTrailingBytes()
        {
            var frame = OneFrame(PrivateSessionFrameCodec.ClaimSeatFrame(
                new SeatClaimMessage(new SeatId("red-player-01"))));
            var padded = frame.Payload.Concat(new byte[] { 0x01 }).ToArray();

            Assert.That(PrivateSessionFrameCodec.TryDecodeClaimSeat(padded, out _), Is.False);
        }

        [Test]
        public void TryDecodeRosterUpdate_RefusesAnUndefinedOccupancyValue()
        {
            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(1);
                writer.Write("red-player-01");
                writer.Write("hero-wasp");
                writer.Write((int)TeamSide.Red);
                writer.Write(99);
                writer.Flush();
                payload = stream.ToArray();
            }

            Assert.That(PrivateSessionFrameCodec.TryDecodeRosterUpdate(payload, out _), Is.False,
                "An integer that is not a declared occupancy member is refused where it is read.");
        }

        [Test]
        public void TryDecodeRosterUpdate_RefusesAnOverlongRoster()
        {
            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(PrivateSessionProtocol.MaxSeats + 1);
                writer.Flush();
                payload = stream.ToArray();
            }

            Assert.That(PrivateSessionFrameCodec.TryDecodeRosterUpdate(payload, out _), Is.False);
        }

        [Test]
        public void TryDecodeReject_RefusesAnUndefinedReasonValue()
        {
            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(999);
                writer.Write("detail");
                writer.Flush();
                payload = stream.ToArray();
            }

            Assert.That(PrivateSessionFrameCodec.TryDecodeReject(payload, out _), Is.False);
        }
    }
}
