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
                    var digest = reader.ReadString();
                    var proof = reader.ReadString();
                    if (stream.Position != stream.Length)
                    {
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(family) || string.IsNullOrWhiteSpace(build) ||
                        string.IsNullOrWhiteSpace(mapId) || string.IsNullOrWhiteSpace(digest))
                    {
                        return false;
                    }

                    message = new SessionHelloMessage(
                        new PrivateSessionIdentity(family, version, build, schema, mapId, mapRevision, digest),
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
                WriteRoster(writer, message.Roster);
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
                        !TryReadRoster(reader, out var roster) ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SessionWelcomeMessage(correlation, roster);
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
                    if (string.IsNullOrWhiteSpace(seat) || stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SeatClaimMessage(new SeatId(seat));
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
                    if (claimed < 0 || total < 0 || claimed > total ||
                        total > PrivateSessionProtocol.MaxSeats ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SeatGrantedMessage(entry, claimed, total);
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
                WriteRoster(writer, message.Roster);
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
                    if (!TryReadRoster(reader, out var roster) || stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new SeatRosterMessage(roster);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] GoodbyeFrame() => EncodeFrame(SessionMessageKind.Goodbye, Array.Empty<byte>());

        private static void WriteRoster(BinaryWriter writer, IReadOnlyList<SeatRosterEntry> roster)
        {
            writer.Write(roster.Count);
            foreach (var entry in roster)
            {
                WriteRosterEntry(writer, entry);
            }
        }

        private static bool TryReadRoster(BinaryReader reader, out IReadOnlyList<SeatRosterEntry> roster)
        {
            roster = null;
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
        }

        private static bool TryReadRosterEntry(BinaryReader reader, out SeatRosterEntry entry)
        {
            entry = null;
            var seat = reader.ReadString();
            var hero = reader.ReadString();
            var teamValue = reader.ReadInt32();
            var occupancyValue = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(seat) || string.IsNullOrWhiteSpace(hero) ||
                !TryReadTeamSide(teamValue, out var team) ||
                !TryReadOccupancy(occupancyValue, out var occupancy))
            {
                return false;
            }

            entry = new SeatRosterEntry(new SeatId(seat), new HeroId(hero), team, occupancy);
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

        internal static bool TryReadMessageKind(int value, out SessionMessageKind kind)
        {
            if (value >= (int)SessionMessageKind.Challenge &&
                value <= (int)SessionMessageKind.RequestResync)
            {
                kind = (SessionMessageKind)value;
                return true;
            }

            kind = SessionMessageKind.None;
            return false;
        }

        private static bool TryReadRejectionReason(int value, out SessionRejectionReason reason)
        {
            if (value >= (int)SessionRejectionReason.ProtocolFamilyMismatch &&
                value <= (int)SessionRejectionReason.SealViolation)
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
