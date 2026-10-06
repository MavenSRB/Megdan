using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>One complete decoded frame: what kind it is and the bytes it carries.</summary>
    public readonly struct SessionFrame
    {
        public SessionFrame(SessionMessageKind kind, byte[] payload)
        {
            Kind = kind;
            Payload = payload ?? Array.Empty<byte>();
        }

        public SessionMessageKind Kind { get; }
        public byte[] Payload { get; }
    }

    /// <summary>
    /// Milestone 8.31. Encodes and decodes protocol version 1's session frames and payloads.
    /// </summary>
    /// <remarks>
    /// <para>Hand-written over <see cref="BinaryWriter"/>/<see cref="BinaryReader"/>, the same
    /// idiom the snapshot serializer uses and for the same reason: every field is written
    /// deliberately and read back bounded, and an integer read into an enum is validated where it
    /// is read rather than cast through.</para>
    /// <para>Every <c>TryDecode</c> refuses rather than throws on wire input: a truncated payload,
    /// an undefined enum value, an over-long list, or trailing bytes after the last field all
    /// return <c>false</c>. The caller answers with <see cref="SessionRejectionReason.MalformedFrame"/>;
    /// nothing about the input is trusted first.</para>
    /// </remarks>
    public static class PrivateSessionFrameCodec
    {
        public static byte[] EncodeFrame(SessionMessageKind kind, byte[] payload)
        {
            payload = payload ?? Array.Empty<byte>();
            if (kind == SessionMessageKind.None)
            {
                throw new ArgumentException("A frame requires a kind.", nameof(kind));
            }

            if (payload.Length > PrivateSessionProtocol.MaxPayloadBytes)
            {
                throw new ArgumentException("A payload is bounded.", nameof(payload));
            }

            var frame = new byte[PrivateSessionProtocol.FrameHeaderBytes + payload.Length];
            frame[0] = PrivateSessionProtocol.FrameMagic0;
            frame[1] = PrivateSessionProtocol.FrameMagic1;
            frame[2] = PrivateSessionProtocol.FrameMagic2;
            frame[3] = PrivateSessionProtocol.FrameMagic3;
            WriteUInt16(frame, 4, (ushort)PrivateSessionProtocol.Version);
            WriteUInt16(frame, 6, (ushort)kind);
            WriteInt32(frame, 8, payload.Length);
            Array.Copy(payload, 0, frame, PrivateSessionProtocol.FrameHeaderBytes, payload.Length);
            return frame;
        }

        public static byte[] ChallengeFrame(SessionChallengeMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(message.Nonce.Length);
                writer.Write(message.Nonce);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.Challenge, stream.ToArray());
            }
        }

        public static bool TryDecodeChallenge(byte[] payload, out SessionChallengeMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var length = reader.ReadInt32();
                    if (length < 0 || length > PrivateSessionProtocol.MaxNonceBytes)
                    {
                        return false;
                    }

                    var nonce = reader.ReadBytes(length);
                    if (nonce.Length != length || stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SessionChallengeMessage(nonce);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] HelloFrame(SessionHelloMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(message.Identity.ProtocolFamily);
                writer.Write(message.Identity.ProtocolVersion);
                writer.Write(message.Identity.BuildIdentity);
                writer.Write(message.Identity.SnapshotSchemaVersion);
                writer.Write(message.Identity.MapId);
                writer.Write(message.Identity.MapContentRevision);
                // Protocol version 9. Written in the element order FirstMismatch checks, so the
                // payload reads the way the refusal ladder does.
                writer.Write(message.Identity.MatchFormatId);
                writer.Write(message.Identity.ContentDigest);
                writer.Write(message.KeyProof);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.Hello, stream.ToArray());
            }
        }

        public static bool TryDecodeHello(byte[] payload, out SessionHelloMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var family = reader.ReadString();
                    var version = reader.ReadInt32();
                    var build = reader.ReadString();
                    var schema = reader.ReadInt32();
                    var mapId = reader.ReadString();
                    var mapRevision = reader.ReadInt32();
                    var matchFormatId = reader.ReadString();
                    var digest = reader.ReadString();
                    var proof = reader.ReadString();
                    if (stream.Position != stream.Length)
                    {
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(family) || string.IsNullOrWhiteSpace(build) ||
                        string.IsNullOrWhiteSpace(mapId) || string.IsNullOrWhiteSpace(digest) ||
                        string.IsNullOrWhiteSpace(matchFormatId))
                    {
                        return false;
                    }

                    message = new SessionHelloMessage(
                        new PrivateSessionIdentity(
                            family, version, build, schema, mapId, mapRevision, matchFormatId, digest),
                        proof);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] WelcomeFrame(SessionWelcomeMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(message.SessionCorrelation);
                WriteRoster(writer, message.Roster, message.FirstPickingTeam);
                writer.Write(message.MatchStarted);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.Welcome, stream.ToArray());
            }
        }

        public static bool TryDecodeWelcome(byte[] payload, out SessionWelcomeMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var correlation = reader.ReadString();
                    if (string.IsNullOrWhiteSpace(correlation) ||
                        !TryReadRoster(reader, out var roster, out var firstPickingTeam))
                    {
                        return false;
                    }

                    var matchStarted = reader.ReadBoolean();
                    if (stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SessionWelcomeMessage(
                        correlation, roster, matchStarted, firstPickingTeam);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] RejectFrame(SessionRejectMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write((int)message.Reason);
                writer.Write(message.Detail);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.Reject, stream.ToArray());
            }
        }

        public static bool TryDecodeReject(byte[] payload, out SessionRejectMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var reasonValue = reader.ReadInt32();
                    var detail = reader.ReadString();
                    if (!TryReadRejectionReason(reasonValue, out var reason) ||
                        detail.Length > PrivateSessionProtocol.MaxRejectDetailChars ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SessionRejectMessage(reason, detail);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] ClaimSeatFrame(SeatClaimMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(message.Seat.Value);
                // Milestone 8.108 (wire /24). The player's name, already cleaned.
                writer.Write(message.PlayerName);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.ClaimSeat, stream.ToArray());
            }
        }

        public static bool TryDecodeClaimSeat(byte[] payload, out SeatClaimMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var seat = reader.ReadString();
                    var playerName = reader.ReadString();
                    if (string.IsNullOrWhiteSpace(seat) || stream.Position != stream.Length ||
                        playerName.Length > PrivateSessionProtocol.MaxPlayerNameWireChars)
                    {
                        return false;
                    }

                    // The constructor cleans the name, so the host never stores what was sent.
                    message = new SeatClaimMessage(new SeatId(seat), playerName);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <summary>
        /// Milestone 8.34. The returning client's reclaim: the seat and the capability, in the
        /// same bounded shape as a claim.
        /// </summary>
        public static byte[] ReclaimSeatFrame(SeatReclaimMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(message.Seat.Value);
                writer.Write(message.Capability);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.ReclaimSeat, stream.ToArray());
            }
        }

        public static bool TryDecodeReclaimSeat(byte[] payload, out SeatReclaimMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var seat = reader.ReadString();
                    var capability = reader.ReadString();
                    if (string.IsNullOrWhiteSpace(seat) ||
                        capability.Length > PrivateSessionProtocol.MaxReconnectCapabilityChars ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SeatReclaimMessage(new SeatId(seat), capability);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] SeatGrantedFrame(SeatGrantedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteRosterEntry(writer, message.Seat);
                writer.Write(message.ClaimedSeats);
                writer.Write(message.TotalSeats);
                writer.Write(message.ConnectionEpoch);
                writer.Write(message.ReconnectCapability);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.SeatGranted, stream.ToArray());
            }
        }

        public static bool TryDecodeSeatGranted(byte[] payload, out SeatGrantedMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadRosterEntry(reader, out var entry))
                    {
                        return false;
                    }

                    var claimed = reader.ReadInt32();
                    var total = reader.ReadInt32();
                    var epoch = reader.ReadInt32();
                    var capability = reader.ReadString();
                    if (claimed < 0 || total < 0 || claimed > total ||
                        total > PrivateSessionProtocol.MaxSeats ||
                        epoch < 1 ||
                        capability.Length > PrivateSessionProtocol.MaxReconnectCapabilityChars ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SeatGrantedMessage(entry, claimed, total, epoch, capability);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] RosterUpdateFrame(SeatRosterMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteRoster(writer, message.Roster, message.FirstPickingTeam);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.RosterUpdate, stream.ToArray());
            }
        }

        public static bool TryDecodeRosterUpdate(byte[] payload, out SeatRosterMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadRoster(reader, out var roster, out var firstPickingTeam) ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SeatRosterMessage(roster, firstPickingTeam);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <summary>
        /// Milestone 8.35a. One seated connection's readiness: a single bool, and nothing that
        /// names a seat.
        /// </summary>
        public static byte[] SetReadinessFrame(SeatReadinessMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(message.IsReady);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.SetReadiness, stream.ToArray());
            }
        }

        public static bool TryDecodeSetReadiness(byte[] payload, out SeatReadinessMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var isReady = reader.ReadBoolean();
                    if (stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SeatReadinessMessage(isReady);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <summary>
        /// Milestone 8.35z37. One seated connection's hero pick: a single bounded id, and
        /// nothing that names a seat - the connection is what says whose pick it is.
        /// </summary>
        /// <remarks>
        /// Measured: for <c>hero-wasp</c> this is a 12-byte frame header plus a length-prefixed
        /// nine-character string, which is the ~12-byte payload cost the selection milestone's own
        /// wire survey predicted before either half was written.
        /// </remarks>
        public static byte[] ChooseHeroFrame(HeroChoiceMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(message.HeroIdValue);
                writer.Flush();
                return EncodeFrame(SessionMessageKind.ChooseHero, stream.ToArray());
            }
        }

        public static bool TryDecodeChooseHero(byte[] payload, out HeroChoiceMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var heroId = reader.ReadString();
                    if (string.IsNullOrWhiteSpace(heroId) ||
                        heroId.Length > PrivateSessionProtocol.MaxHeroIdChars ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new HeroChoiceMessage(heroId);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <summary>
        /// Milestone 8.35a. The host's one-way announcement that the lobby is over. It carries no
        /// payload: the roster the clients already hold says which seats are in it, and a phase
        /// change that needed arguments would be a second way to say what the roster says.
        /// </summary>
        public static byte[] MatchStartedFrame() =>
            EncodeFrame(SessionMessageKind.MatchStarted, Array.Empty<byte>());

        /// <summary>
        /// Refuses a <see cref="SessionMessageKind.MatchStarted"/> frame carrying anything at all -
        /// the same "no trailing bytes" discipline every other payload is read under, stated for a
        /// payload that is meant to be empty.
        /// </summary>
        public static bool TryDecodeMatchStarted(byte[] payload) =>
            (payload ?? Array.Empty<byte>()).Length == 0;

        public static byte[] GoodbyeFrame() => EncodeFrame(SessionMessageKind.Goodbye, Array.Empty<byte>());

        /// <remarks>
        /// Milestone 8.35z37 writes rulebook p.22's coin ahead of the rows, so the Welcome
        /// and every RosterUpdate carry the pick order a selection screen draws from - see
        /// <see cref="SeatRosterMessage.FirstPickingTeam"/> for why a client cannot derive it before
        /// the first pick has been made.
        /// </remarks>
        private static void WriteRoster(
            BinaryWriter writer, IReadOnlyList<SeatRosterEntry> roster, TeamSide firstPickingTeam)
        {
            writer.Write((int)firstPickingTeam);
            writer.Write(roster.Count);
            foreach (var entry in roster)
            {
                WriteRosterEntry(writer, entry);
            }
        }

        private static bool TryReadRoster(
            BinaryReader reader,
            out IReadOnlyList<SeatRosterEntry> roster,
            out TeamSide firstPickingTeam)
        {
            roster = null;
            if (!TryReadTeamSide(reader.ReadInt32(), out firstPickingTeam))
            {
                return false;
            }

            var count = reader.ReadInt32();
            if (count < 0 || count > PrivateSessionProtocol.MaxSeats)
            {
                return false;
            }

            var entries = new List<SeatRosterEntry>(count);
            for (var index = 0; index < count; index++)
            {
                if (!TryReadRosterEntry(reader, out var entry))
                {
                    return false;
                }

                entries.Add(entry);
            }

            roster = entries;
            return true;
        }

        private static void WriteRosterEntry(BinaryWriter writer, SeatRosterEntry entry)
        {
            writer.Write(entry.Seat.Value);
            writer.Write(entry.Hero.Value);
            writer.Write((int)entry.TeamSide);
            writer.Write((int)entry.Occupancy);
            // Milestone 8.35a. Readiness is public seat state, so it travels with the rest of the
            // row rather than in a message of its own.
            writer.Write(entry.IsReady);
            // Milestone 8.35z37. Whether that hero was PICKED, which is public seat state
            // in the same way readiness is, and which a client cannot infer - see
            // SeatRosterEntry.HasChosenHero for why the hero column alone cannot say it.
            writer.Write(entry.HasChosenHero);
            // Milestone 8.108 (wire /24). The seat's player name, public like the rest of the row.
            writer.Write(entry.PlayerName);
        }

        private static bool TryReadRosterEntry(BinaryReader reader, out SeatRosterEntry entry)
        {
            entry = null;
            var seat = reader.ReadString();
            var hero = reader.ReadString();
            var teamValue = reader.ReadInt32();
            var occupancyValue = reader.ReadInt32();
            var isReady = reader.ReadBoolean();
            var hasChosenHero = reader.ReadBoolean();
            var playerName = reader.ReadString();
            if (string.IsNullOrWhiteSpace(seat) || string.IsNullOrWhiteSpace(hero) ||
                playerName.Length > PrivateSessionProtocol.MaxPlayerNameWireChars ||
                !TryReadTeamSide(teamValue, out var team) ||
                !TryReadOccupancy(occupancyValue, out var occupancy))
            {
                return false;
            }

            entry = new SeatRosterEntry(
                new SeatId(seat), new HeroId(hero), team, occupancy, isReady, hasChosenHero, playerName);
            return true;
        }

        private static bool TryReadTeamSide(int value, out TeamSide side)
        {
            switch (value)
            {
                case (int)TeamSide.None: side = TeamSide.None; return true;
                case (int)TeamSide.Red: side = TeamSide.Red; return true;
                case (int)TeamSide.Blue: side = TeamSide.Blue; return true;
                default: side = TeamSide.None; return false;
            }
        }

        private static bool TryReadOccupancy(int value, out SeatOccupancyState occupancy)
        {
            switch (value)
            {
                case (int)SeatOccupancyState.Open: occupancy = SeatOccupancyState.Open; return true;
                case (int)SeatOccupancyState.Claimed: occupancy = SeatOccupancyState.Claimed; return true;
                case (int)SeatOccupancyState.Reserved: occupancy = SeatOccupancyState.Reserved; return true;
                default: occupancy = SeatOccupancyState.Open; return false;
            }
        }

        /// <remarks>
        /// <b>The upper bound names the enum's LAST member and has to be moved with it.</b> Milestone
        /// 8.35z37 added <see cref="SessionMessageKind.ChooseHero"/> and every frame carrying
        /// it was refused as malformed until this line moved - the whole fixture went red on one
        /// stale constant. Worth the remark because nothing else in the build points at it: a new
        /// member compiles, encodes, and is then rejected on the way back in.
        /// </remarks>
        internal static bool TryReadMessageKind(int value, out SessionMessageKind kind)
        {
            if (value >= (int)SessionMessageKind.Challenge &&
                value <= (int)SessionMessageKind.CoordinationUpdate)
            {
                kind = (SessionMessageKind)value;
                return true;
            }

            kind = SessionMessageKind.None;
            return false;
        }

        /// <remarks>
        /// <para>Same stale-tail shape as <see cref="TryReadMessageKind"/> above, and it had ALREADY
        /// BITTEN before this milestone touched it: the bound read
        /// <c>ReconnectNotAuthorized</c> (21) while the ELEVENTH amendment's
        /// <see cref="SessionRejectionReason.MatchFormatMismatch"/> (22) had been appended past it.
        /// A peer refused for composing a different printed setup therefore received a reject frame
        /// its own decoder threw away as malformed - so instead of "The host is playing a different
        /// match format" it was told the host had sent something unreadable. Found while adding
        /// <see cref="SessionRejectionReason.HeroSelectionRefused"/> (23) directly behind it, and
        /// fixed for both.</para>
        /// <para>It is a decode bound rather than a rule, so widening it refuses nothing that used
        /// to be accepted; it only stops discarding reasons the build already sends.</para>
        /// </remarks>
        private static bool TryReadRejectionReason(int value, out SessionRejectionReason reason)
        {
            if (value >= (int)SessionRejectionReason.ProtocolFamilyMismatch &&
                value <= (int)SessionRejectionReason.HeroSelectionRefused)
            {
                reason = (SessionRejectionReason)value;
                return true;
            }

            reason = SessionRejectionReason.None;
            return false;
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        internal static ushort ReadUInt16(byte[] buffer, int offset) =>
            (ushort)(buffer[offset] | (buffer[offset + 1] << 8));

        internal static int ReadInt32(byte[] buffer, int offset) =>
            buffer[offset] | (buffer[offset + 1] << 8) |
            (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24);
    }

    /// <summary>
    /// Milestone 8.31. Reassembles one connection's byte stream into complete frames, refusing the
    /// whole connection at the first violation.
    /// </summary>
    /// <remarks>
    /// <para>This is where the record's "length-checked before allocation-heavy decode" clause is
    /// enforced: the twelve header bytes are validated - magic, protocol version, kind, declared
    /// length - before one payload byte is buffered, so a hostile length field never buys an
    /// allocation past <see cref="PrivateSessionProtocol.MaxPayloadBytes"/>.</para>
    /// <para>One assembler serves one connection and is not shared: a violation poisons it, every
    /// buffered byte is dropped, and the caller closes that connection and no other.</para>
    /// </remarks>
    public sealed class PrivateSessionFrameAssembler
    {
        private readonly byte[] header = new byte[PrivateSessionProtocol.FrameHeaderBytes];
        private readonly Queue<SessionFrame> ready = new Queue<SessionFrame>();
        private int headerFilled;
        private byte[] payload;
        private int payloadFilled;
        private SessionMessageKind pendingKind;

        /// <summary>Why this connection's stream was refused, or <c>None</c> while it is healthy.</summary>
        public SessionRejectionReason Rejection { get; private set; }

        /// <summary>
        /// Consumes received bytes. Returns <c>false</c> once the stream is refused; the bytes of a
        /// refused stream are dropped, not parsed.
        /// </summary>
        public bool Add(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0 || count < 0 || offset + count > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (Rejection != SessionRejectionReason.None)
            {
                return false;
            }

            var position = offset;
            var end = offset + count;
            while (position < end)
            {
                if (payload == null)
                {
                    var headerTake = Math.Min(header.Length - headerFilled, end - position);
                    Array.Copy(data, position, header, headerFilled, headerTake);
                    headerFilled += headerTake;
                    position += headerTake;
                    if (headerFilled < header.Length)
                    {
                        return true;
                    }

                    if (!TryOpenPayload())
                    {
                        return false;
                    }

                    continue;
                }

                var payloadTake = Math.Min(payload.Length - payloadFilled, end - position);
                Array.Copy(data, position, payload, payloadFilled, payloadTake);
                payloadFilled += payloadTake;
                position += payloadTake;
                if (payloadFilled == payload.Length)
                {
                    ready.Enqueue(new SessionFrame(pendingKind, payload));
                    payload = null;
                    payloadFilled = 0;
                    headerFilled = 0;
                    pendingKind = SessionMessageKind.None;
                }
            }

            return true;
        }

        public bool TryDequeue(out SessionFrame frame)
        {
            if (ready.Count > 0)
            {
                frame = ready.Dequeue();
                return true;
            }

            frame = default;
            return false;
        }

        private bool TryOpenPayload()
        {
            if (header[0] != PrivateSessionProtocol.FrameMagic0 ||
                header[1] != PrivateSessionProtocol.FrameMagic1 ||
                header[2] != PrivateSessionProtocol.FrameMagic2 ||
                header[3] != PrivateSessionProtocol.FrameMagic3)
            {
                return Refuse(SessionRejectionReason.MalformedFrame);
            }

            var version = PrivateSessionFrameCodec.ReadUInt16(header, 4);
            if (version != PrivateSessionProtocol.Version)
            {
                return Refuse(SessionRejectionReason.ProtocolVersionMismatch);
            }

            var kindValue = PrivateSessionFrameCodec.ReadUInt16(header, 6);
            if (!PrivateSessionFrameCodec.TryReadMessageKind(kindValue, out var kind))
            {
                return Refuse(SessionRejectionReason.MalformedFrame);
            }

            var length = PrivateSessionFrameCodec.ReadInt32(header, 8);
            if (length < 0)
            {
                return Refuse(SessionRejectionReason.MalformedFrame);
            }

            if (length > PrivateSessionProtocol.MaxPayloadBytes)
            {
                return Refuse(SessionRejectionReason.OversizedFrame);
            }

            pendingKind = kind;
            if (length == 0)
            {
                ready.Enqueue(new SessionFrame(kind, Array.Empty<byte>()));
                headerFilled = 0;
                pendingKind = SessionMessageKind.None;
                return true;
            }

            payload = new byte[length];
            payloadFilled = 0;
            return true;
        }

        private bool Refuse(SessionRejectionReason reason)
        {
            Rejection = reason;
            payload = null;
            payloadFilled = 0;
            headerFilled = 0;
            ready.Clear();
            return false;
        }
    }
}
