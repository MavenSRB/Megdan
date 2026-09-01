using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.31. The in-memory transport stand-ins the session fixtures drive the state
    /// machines through: every sent frame is decoded and recorded, every close is remembered, and
    /// no socket exists anywhere in the fixture.
    /// </summary>
    internal sealed class RecordingSessionOutbox : IPrivateSessionOutbox
    {
        private readonly List<KeyValuePair<int, SessionFrame>> sent =
            new List<KeyValuePair<int, SessionFrame>>();
        private readonly List<int> closed = new List<int>();

        public IReadOnlyList<int> ClosedOrdinals => closed;

        public void Send(int connectionOrdinal, byte[] frame)
        {
            var assembler = new PrivateSessionFrameAssembler();
            Assert.That(assembler.Add(frame, 0, frame.Length), Is.True,
                "The host sent a frame its own assembler refuses.");
            Assert.That(assembler.TryDequeue(out var decoded), Is.True);
            sent.Add(new KeyValuePair<int, SessionFrame>(connectionOrdinal, decoded));
        }

        public void Close(int connectionOrdinal) => closed.Add(connectionOrdinal);

        public IReadOnlyList<SessionFrame> FramesFor(int ordinal) =>
            sent.Where(pair => pair.Key == ordinal).Select(pair => pair.Value).ToArray();

        public SessionFrame LastFrameFor(int ordinal)
        {
            var frames = FramesFor(ordinal);
            Assert.That(frames, Is.Not.Empty, "Nothing was sent to ordinal " + ordinal + ".");
            return frames[frames.Count - 1];
        }

        public SessionRejectMessage RejectFor(int ordinal)
        {
            var frame = LastFrameFor(ordinal);
            Assert.That(frame.Kind, Is.EqualTo(SessionMessageKind.Reject),
                "Ordinal " + ordinal + "'s last frame is a " + frame.Kind + ", not a rejection.");
            Assert.That(PrivateSessionFrameCodec.TryDecodeReject(frame.Payload, out var reject), Is.True);
            return reject;
        }

        public bool WasClosed(int ordinal) => closed.Contains(ordinal);
    }

    /// <summary>A deterministic nonce source, so a fixture can answer its own challenge.</summary>
    internal sealed class RecordedNonceSource : IPrivateSessionNonceSource
    {
        public byte[] Nonce { get; } =
            Enumerable.Range(10, 16).Select(value => (byte)value).ToArray();

        public byte[] NextNonce() => (byte[])Nonce.Clone();
    }

    /// <summary>One composed session host with everything a fixture needs to drive it.</summary>
    internal sealed class PrivateSessionTestDriver
    {
        private PrivateSessionTestDriver(
            PrivateSessionHost session,
            RecordingSessionOutbox outbox,
            RecordedNonceSource nonces,
            PrivateSessionIdentity identity,
            string sessionKey)
        {
            Session = session;
            Outbox = outbox;
            Nonces = nonces;
            Identity = identity;
            SessionKey = sessionKey;
        }

        public PrivateSessionHost Session { get; }
        public RecordingSessionOutbox Outbox { get; }
        public RecordedNonceSource Nonces { get; }
        public PrivateSessionIdentity Identity { get; }
        public string SessionKey { get; }

        public static PrivateSessionTestDriver Compose(string sessionKey = "")
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var identity = PrivateSessionIdentity.ForComposition(map, configuration, "test-build");
            var matchHost = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var outbox = new RecordingSessionOutbox();
            var nonces = new RecordedNonceSource();
            var session = new PrivateSessionHost(matchHost, identity, sessionKey, outbox, nonces);
            return new PrivateSessionTestDriver(session, outbox, nonces, identity, sessionKey);
        }

        /// <summary>Connects and completes a compatible handshake for that ordinal.</summary>
        public void Admit(int ordinal, long nowTicks = 0)
        {
            Session.OnConnected(ordinal, nowTicks);
            Hello(ordinal, Identity, PrivateSessionKeyProof.Compute(Nonces.Nonce, SessionKey));
            var last = Outbox.LastFrameFor(ordinal);
            Assert.That(last.Kind, Is.EqualTo(SessionMessageKind.Welcome),
                "Admission was expected for ordinal " + ordinal + " but the host answered " + last.Kind + ".");
        }

        /// <summary>Sends a Hello with that tuple and proof, however the host answers.</summary>
        public void Hello(int ordinal, PrivateSessionIdentity identity, string keyProof)
        {
            var frame = Decode(PrivateSessionFrameCodec.HelloFrame(
                new SessionHelloMessage(identity, keyProof)));
            Session.OnFrame(ordinal, frame);
        }

        /// <summary>Sends a seat claim, however the host answers.</summary>
        public void Claim(int ordinal, string seat)
        {
            var frame = Decode(PrivateSessionFrameCodec.ClaimSeatFrame(
                new SeatClaimMessage(new SeatId(seat))));
            Session.OnFrame(ordinal, frame);
        }

        /// <summary>Admits and seats one ordinal on one seat, asserting the grant.</summary>
        public void Seat(int ordinal, string seat)
        {
            Admit(ordinal);
            Claim(ordinal, seat);
            var last = Outbox.FramesFor(ordinal)
                .Last(frame => frame.Kind != SessionMessageKind.RosterUpdate);
            Assert.That(last.Kind, Is.EqualTo(SessionMessageKind.SeatGranted),
                "Seat " + seat + " was expected granted to ordinal " + ordinal + ".");
        }

        public static SessionFrame Decode(byte[] encoded)
        {
            var assembler = new PrivateSessionFrameAssembler();
            Assert.That(assembler.Add(encoded, 0, encoded.Length), Is.True);
            Assert.That(assembler.TryDequeue(out var frame), Is.True);
            return frame;
        }

        /// <summary>A copy of the identity with exactly one element replaced.</summary>
        public PrivateSessionIdentity IdentityWith(
            string family = null,
            int? version = null,
            string build = null,
            int? schema = null,
            string mapId = null,
            int? mapRevision = null,
            string digest = null) =>
            new PrivateSessionIdentity(
                family ?? Identity.ProtocolFamily,
                version ?? Identity.ProtocolVersion,
                build ?? Identity.BuildIdentity,
                schema ?? Identity.SnapshotSchemaVersion,
                mapId ?? Identity.MapId,
                mapRevision ?? Identity.MapContentRevision,
                digest ?? Identity.ContentDigest);
    }
}
