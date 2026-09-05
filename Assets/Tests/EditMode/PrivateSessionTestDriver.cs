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

        /// <summary>
        /// Milestone 8.32. The per-ordinal client channels of a keyed driver, so sealed host
        /// frames are recorded as their inner meaning. Sealing itself is asserted through
        /// <see cref="SealedFrameCounts"/>.
        /// </summary>
        public Dictionary<int, PrivateSessionSecureChannel> ClientChannels { get; } =
            new Dictionary<int, PrivateSessionSecureChannel>();

        /// <summary>How many sealed frames the host sent each ordinal.</summary>
        public Dictionary<int, int> SealedFrameCounts { get; } = new Dictionary<int, int>();

        public void Send(int connectionOrdinal, byte[] frame)
        {
            var assembler = new PrivateSessionFrameAssembler();
            Assert.That(assembler.Add(frame, 0, frame.Length), Is.True,
                "The host sent a frame its own assembler refuses.");
            Assert.That(assembler.TryDequeue(out var decoded), Is.True);
            if (decoded.Kind == SessionMessageKind.Sealed)
            {
                SealedFrameCounts.TryGetValue(connectionOrdinal, out var count);
                SealedFrameCounts[connectionOrdinal] = count + 1;
                Assert.That(ClientChannels.TryGetValue(connectionOrdinal, out var channel), Is.True,
                    "The host sealed a frame for ordinal " + connectionOrdinal +
                    " but the fixture holds no channel for it.");
                Assert.That(channel.TryUnseal(decoded.Payload, out decoded), Is.True,
                    "The host sent a sealed frame the client channel refuses.");
            }

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
            IMatchHost matchHost,
            RecordingSessionOutbox outbox,
            RecordedNonceSource nonces,
            PrivateSessionIdentity identity,
            string sessionKey)
        {
            Session = session;
            MatchHost = matchHost;
            Outbox = outbox;
            Nonces = nonces;
            Identity = identity;
            SessionKey = sessionKey;
        }

        public PrivateSessionHost Session { get; }

        /// <summary>
        /// Milestone 8.32. The authority behind the session host, for fixtures that must read
        /// omnisciently what an attacker must not. The production path never exposes it.
        /// </summary>
        public IMatchHost MatchHost { get; }

        public RecordingSessionOutbox Outbox { get; }
        public RecordedNonceSource Nonces { get; }
        public PrivateSessionIdentity Identity { get; }
        public string SessionKey { get; }

        public static PrivateSessionTestDriver Compose(string sessionKey = "")
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var initialState = OwnerScopedProjectionTests.CreateOfficialInitialState();
            var identity = PrivateSessionIdentity.ForComposition(
                map, configuration, initialState.Setup, "test-build");
            var matchHost = GuardsDigital.Application.MatchHost.Compose(initialState);
            var outbox = new RecordingSessionOutbox();
            var nonces = new RecordedNonceSource();
            var session = new PrivateSessionHost(matchHost, identity, sessionKey, outbox, nonces);
            return new PrivateSessionTestDriver(session, matchHost, outbox, nonces, identity, sessionKey);
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
            // A keyed driver arms the ordinal's client channel exactly where a real client does:
            // as it answers the challenge, before it can know whether the host will admit it.
            if (SessionKey.Length > 0 && !Outbox.ClientChannels.ContainsKey(ordinal))
            {
                Outbox.ClientChannels[ordinal] =
                    PrivateSessionSecureChannel.ForClient(SessionKey, Nonces.Nonce);
            }

            var frame = Decode(PrivateSessionFrameCodec.HelloFrame(
                new SessionHelloMessage(identity, keyProof)));
            Session.OnFrame(ordinal, frame);
        }

        /// <summary>Sends a seat claim, however the host answers.</summary>
        public void Claim(int ordinal, string seat) =>
            Submit(ordinal, PrivateSessionFrameCodec.ClaimSeatFrame(
                new SeatClaimMessage(new SeatId(seat))));

        /// <summary>
        /// Milestone 8.32. Hands one composed frame to the host as this ordinal's client would
        /// send it: sealed through its channel on a keyed session, plaintext otherwise.
        /// </summary>
        public void Submit(int ordinal, byte[] frame)
        {
            var decoded = Decode(frame);
            if (Outbox.ClientChannels.TryGetValue(ordinal, out var channel))
            {
                decoded = Decode(channel.Seal(decoded.Kind, decoded.Payload));
            }

            Session.OnFrame(ordinal, decoded);
        }

        /// <summary>Admits and seats one ordinal on one seat, asserting the grant.</summary>
        public void Seat(int ordinal, string seat)
        {
            Admit(ordinal);
            Claim(ordinal, seat);
            var last = Outbox.FramesFor(ordinal)
                .Last(frame => frame.Kind != SessionMessageKind.RosterUpdate &&
                    frame.Kind != SessionMessageKind.SeatProjection);
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
            string digest = null,
            string matchFormatId = null) =>
            new PrivateSessionIdentity(
                family ?? Identity.ProtocolFamily,
                version ?? Identity.ProtocolVersion,
                build ?? Identity.BuildIdentity,
                schema ?? Identity.SnapshotSchemaVersion,
                mapId ?? Identity.MapId,
                mapRevision ?? Identity.MapContentRevision,
                matchFormatId ?? Identity.MatchFormatId,
                digest ?? Identity.ContentDigest);
    }
}
