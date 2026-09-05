using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.32. The bounded header every gameplay message carries: which session it belongs
    /// to, which connection epoch it was sent under, where it stands in its direction's ordered
    /// stream, and the correlation a reply echoes.
    /// </summary>
    /// <remarks>
    /// <para>These are the envelope fields the decision record lists beyond the frame header's own
    /// slice - session ID, sender connection epoch, monotonic per-direction sequence, and
    /// correlation ID - joining the protocol with the gameplay codec exactly as the record
    /// schedules. The session ID is the correlation the host published at admission; the epoch is
    /// fixed at <see cref="PrivateSessionProtocol.InitialConnectionEpoch"/> until milestone 8.34
    /// rotates it on reconnect.</para>
    /// <para>Everything here is protocol state, not authority state: a wrong value is an
    /// <see cref="SessionRejectionReason.EnvelopeMismatch"/> protocol refusal that closes the
    /// connection, never a rules answer, and no envelope field can name a seat or a hero - the
    /// caller's identity is the connection the message arrived on.</para>
    /// </remarks>
    public readonly struct NetworkEnvelope
    {
        public NetworkEnvelope(string sessionCorrelation, int epoch, long sequence, long correlation)
        {
            if (string.IsNullOrWhiteSpace(sessionCorrelation))
            {
                throw new ArgumentException(
                    "An envelope names its session.", nameof(sessionCorrelation));
            }

            if (epoch < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(epoch), epoch, "An epoch counts from 1.");
            }

            if (sequence < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequence), sequence, "A gameplay sequence counts from 1.");
            }

            SessionCorrelation = sessionCorrelation;
            Epoch = epoch;
            Sequence = sequence;
            Correlation = correlation;
        }

        /// <summary>The opaque session correlation the host published in its Welcome.</summary>
        public string SessionCorrelation { get; }

        /// <summary>The sender's connection epoch; rotation is milestone 8.34's reconnect work.</summary>
        public int Epoch { get; }

        /// <summary>Strictly monotonic per direction per connection, from 1.</summary>
        public long Sequence { get; }

        /// <summary>
        /// Echoed by a result and by the initiating seat's promised follow-up projection; zero on
        /// an unsolicited host push or on the same publication sent to observers.
        /// </summary>
        public long Correlation { get; }
    }

    /// <summary>
    /// Milestone 8.32. One gameplay submission as it crosses the wire: the envelope, the wire's
    /// own submission identity and the expected projection version - both outside the command
    /// payload - and the bounded command DTO bytes.
    /// </summary>
    public sealed class NetworkSubmissionMessage
    {
        public NetworkSubmissionMessage(
            NetworkEnvelope envelope, string submissionId, long expectedVersion, byte[] commandPayload)
        {
            if (string.IsNullOrWhiteSpace(submissionId) ||
                submissionId.Length > NetworkMessageCodec.MaxSubmissionIdChars)
            {
                throw new ArgumentException("A submission id is present and bounded.", nameof(submissionId));
            }

            if (expectedVersion < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedVersion), expectedVersion, "A projection version is not negative.");
            }

            Envelope = envelope;
            SubmissionId = submissionId;
            ExpectedVersion = expectedVersion;
            CommandPayload = commandPayload ?? throw new ArgumentNullException(nameof(commandPayload));
        }

        public NetworkEnvelope Envelope { get; }

        /// <summary>The wire's own random unique identity, never composed by presentation.</summary>
        public string SubmissionId { get; }

        /// <summary>The seat-projection version this answer was decided against.</summary>
        public long ExpectedVersion { get; }

        /// <summary>The bounded command DTO; decoded and mapped by the host, never trusted.</summary>
        public byte[] CommandPayload { get; }
    }

    /// <summary>Milestone 8.32. What the host told one seat about its own wire submission.</summary>
    public sealed class NetworkSubmissionResultMessage
    {
        public NetworkSubmissionResultMessage(
            NetworkEnvelope envelope,
            string submissionId,
            bool accepted,
            SeatSubmissionRejectionReason rejectionReason,
            MatchSessionRejectionReason sessionRejectionReason,
            string detail,
            long version,
            long sequence)
        {
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                throw new ArgumentException("A result names the submission it answers.", nameof(submissionId));
            }

            Envelope = envelope;
            SubmissionId = submissionId;
            Accepted = accepted;
            RejectionReason = rejectionReason;
            SessionRejectionReason = sessionRejectionReason;
            Detail = detail ?? string.Empty;
            if (Detail.Length > PrivateSessionProtocol.MaxRejectDetailChars)
            {
                Detail = Detail.Substring(0, PrivateSessionProtocol.MaxRejectDetailChars);
            }

            Version = version;
            Sequence = sequence;
        }

        public NetworkEnvelope Envelope { get; }
        public string SubmissionId { get; }
        public bool Accepted { get; }
        public SeatSubmissionRejectionReason RejectionReason { get; }
        public MatchSessionRejectionReason SessionRejectionReason { get; }
        public string Detail { get; }

        /// <summary>The authoritative version after the submission; unmoved by a refusal.</summary>
        public long Version { get; }

        /// <summary>The host stream sequence the submission appended, and 0 when refused.</summary>
        public long Sequence { get; }
    }

    /// <summary>
    /// Milestone 8.32. One seat's own owner-scoped projection, pushed by the host at seating and
    /// after every accepted submission. The payload is that seat's encoding and nobody else's.
    /// </summary>
    public sealed class NetworkSeatProjectionMessage
    {
        public NetworkSeatProjectionMessage(
            NetworkEnvelope envelope,
            byte[] projectionPayload,
            IReadOnlyList<NetworkMatchLogRow> matchLog = null)
        {
            Envelope = envelope;
            ProjectionPayload = projectionPayload ?? throw new ArgumentNullException(nameof(projectionPayload));
            MatchLog = matchLog ?? Array.Empty<NetworkMatchLogRow>();
        }

        public NetworkEnvelope Envelope { get; }
        public byte[] ProjectionPayload { get; }

        /// <summary>
        /// Milestone 8.35r. The seat-public match log's whole retained window, beside the
        /// projection rather than inside it.
        /// </summary>
        /// <remarks>
        /// <para><b>Why it travels with the projection and not in its own frame.</b> It was in its
        /// own frame first, and the fault-injection suite falsified that: an extra host frame
        /// consumes a gameplay sequence, so a dropped projection's gap was absorbed by the log frame
        /// behind it and the client's own recovery never fired. Riding inside this message, the log
        /// inherits the ordered-stream discipline already proven for the projection - a stale copy
        /// is discarded, a duplicate is harmless, a gap heals on the next complete view - and adds
        /// no rule of its own.</para>
        /// <para><b>Why the whole window every time, rather than the new lines.</b> An incremental
        /// append needs the host to remember how much of the log each connection holds, and a frame
        /// the wire drops silently invalidates that memory - the host would believe it had delivered
        /// lines the client never saw, and would never send them again. A whole window is absolutely
        /// idempotent instead, which is the same reason the complete projection beside it is re-sent
        /// whole rather than diffed. The log is a minority of a cost this design already pays.</para>
        /// <para><b>Why it is outside <see cref="ProjectionPayload"/>.</b> That payload is one
        /// seat's own owner-scoped encoding; the log is identical for every seat. Keeping them
        /// separate is what lets the synchronization sweep keep measuring the seat record's size
        /// against its own bound without the shared log inflating a per-seat number.</para>
        /// </remarks>
        public IReadOnlyList<NetworkMatchLogRow> MatchLog { get; }
    }

    /// <summary>
    /// Milestone 8.33. One public agreement statement as it crosses the wire, in exactly the
    /// submission shape: the envelope, the wire's own submission identity and the expected
    /// projection version outside the payload, and the typed statement the seat's own projected
    /// agreement surface offered.
    /// </summary>
    public sealed class NetworkAgreementSubmissionMessage
    {
        public NetworkAgreementSubmissionMessage(
            NetworkEnvelope envelope,
            string submissionId,
            long expectedVersion,
            TeamAgreementCommand statement)
        {
            if (string.IsNullOrWhiteSpace(submissionId) ||
                submissionId.Length > NetworkMessageCodec.MaxSubmissionIdChars)
            {
                throw new ArgumentException("A submission id is present and bounded.", nameof(submissionId));
            }

            if (expectedVersion < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedVersion), expectedVersion, "A projection version is not negative.");
            }

            Envelope = envelope;
            SubmissionId = submissionId;
            ExpectedVersion = expectedVersion;
            Statement = statement ?? throw new ArgumentNullException(nameof(statement));
        }

        public NetworkEnvelope Envelope { get; }
        public string SubmissionId { get; }
        public long ExpectedVersion { get; }

        /// <summary>The typed statement; the host maps it to its own offered instance, never trusts it.</summary>
        public TeamAgreementCommand Statement { get; }
    }

    /// <summary>
    /// Milestone 8.35f. One seat's request to step its own turn back by one accepted command.
    /// </summary>
    public sealed class NetworkUndoSubmissionMessage
    {
        public NetworkUndoSubmissionMessage(
            NetworkEnvelope envelope, string submissionId, long expectedVersion)
        {
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                throw new ArgumentException(
                    "A submission carries its own identity.", nameof(submissionId));
            }

            if (expectedVersion < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedVersion), expectedVersion, "A projection version is not negative.");
            }

            Envelope = envelope;
            SubmissionId = submissionId;
            ExpectedVersion = expectedVersion;
        }

        public NetworkEnvelope Envelope { get; }
        public string SubmissionId { get; }
        public long ExpectedVersion { get; }
    }

    /// <summary>Milestone 8.35f. What the host told one seat about its own rewind.</summary>
    public sealed class NetworkUndoResultMessage
    {
        public NetworkUndoResultMessage(
            NetworkEnvelope envelope,
            string submissionId,
            bool accepted,
            SeatSubmissionRejectionReason rejectionReason,
            MatchSessionRejectionReason sessionRejectionReason,
            string detail,
            long version)
        {
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                throw new ArgumentException(
                    "A result names the submission it answers.", nameof(submissionId));
            }

            Envelope = envelope;
            SubmissionId = submissionId;
            Accepted = accepted;
            RejectionReason = rejectionReason;
            SessionRejectionReason = sessionRejectionReason;
            Detail = detail ?? string.Empty;
            if (Detail.Length > PrivateSessionProtocol.MaxRejectDetailChars)
            {
                Detail = Detail.Substring(0, PrivateSessionProtocol.MaxRejectDetailChars);
            }

            Version = version;
        }

        public NetworkEnvelope Envelope { get; }
        public string SubmissionId { get; }
        public bool Accepted { get; }
        public SeatSubmissionRejectionReason RejectionReason { get; }
        public MatchSessionRejectionReason SessionRejectionReason { get; }
        public string Detail { get; }
        public long Version { get; }
    }

    /// <summary>Milestone 8.33. What the host told one seat about its own agreement statement.</summary>
    public sealed class NetworkAgreementResultMessage
    {
        public NetworkAgreementResultMessage(
            NetworkEnvelope envelope,
            string submissionId,
            bool accepted,
            TeamAgreementSubmissionRejectionReason rejectionReason,
            string detail,
            long version)
        {
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                throw new ArgumentException("A result names the submission it answers.", nameof(submissionId));
            }

            Envelope = envelope;
            SubmissionId = submissionId;
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Detail = detail ?? string.Empty;
            if (Detail.Length > PrivateSessionProtocol.MaxRejectDetailChars)
            {
                Detail = Detail.Substring(0, PrivateSessionProtocol.MaxRejectDetailChars);
            }

            Version = version;
        }

        public NetworkEnvelope Envelope { get; }
        public string SubmissionId { get; }
        public bool Accepted { get; }
        public TeamAgreementSubmissionRejectionReason RejectionReason { get; }
        public string Detail { get; }

        /// <summary>
        /// The authoritative version after the statement. A statement short of unanimity moves
        /// nothing; the one that completes it carries the final rules choice's new version.
        /// </summary>
        public long Version { get; }
    }

    /// <summary>
    /// Milestone 8.33. A seat's request for one bounded current-projection resync: the envelope
    /// alone, because the seat asking is the connection it arrived on and the answer is that
    /// seat's ordinary projection push echoing this correlation.
    /// </summary>
    public sealed class NetworkResyncRequestMessage
    {
        public NetworkResyncRequestMessage(NetworkEnvelope envelope)
        {
            Envelope = envelope;
        }

        public NetworkEnvelope Envelope { get; }
    }

    /// <summary>
    /// Milestone 8.32. Encodes and decodes the gameplay messages, in the same refuse-on-read
    /// idiom as <see cref="PrivateSessionFrameCodec"/>.
    /// </summary>
    public static class NetworkMessageCodec
    {
        public const int MaxSubmissionIdChars = 64;

        public static byte[] SubmitCommandFrame(NetworkSubmissionMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Write(message.SubmissionId);
                writer.Write(message.ExpectedVersion);
                writer.Write(message.CommandPayload.Length);
                writer.Write(message.CommandPayload);
                writer.Flush();
                return PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.SubmitCommand, stream.ToArray());
            }
        }

        public static bool TryDecodeSubmitCommand(byte[] payload, out NetworkSubmissionMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadEnvelope(reader, out var envelope))
                    {
                        return false;
                    }

                    var submissionId = reader.ReadString();
                    var expectedVersion = reader.ReadInt64();
                    var commandLength = reader.ReadInt32();
                    if (string.IsNullOrWhiteSpace(submissionId) ||
                        submissionId.Length > MaxSubmissionIdChars ||
                        expectedVersion < 0 ||
                        commandLength < 0 ||
                        commandLength > stream.Length - stream.Position)
                    {
                        return false;
                    }

                    var commandPayload = reader.ReadBytes(commandLength);
                    if (commandPayload.Length != commandLength || stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new NetworkSubmissionMessage(
                        envelope, submissionId, expectedVersion, commandPayload);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] SubmissionResultFrame(NetworkSubmissionResultMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Write(message.SubmissionId);
                writer.Write(message.Accepted);
                writer.Write((int)message.RejectionReason);
                writer.Write((int)message.SessionRejectionReason);
                writer.Write(message.Detail);
                writer.Write(message.Version);
                writer.Write(message.Sequence);
                writer.Flush();
                return PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.SubmissionResult, stream.ToArray());
            }
        }

        public static bool TryDecodeSubmissionResult(
            byte[] payload, out NetworkSubmissionResultMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadEnvelope(reader, out var envelope))
                    {
                        return false;
                    }

                    var submissionId = reader.ReadString();
                    var accepted = reader.ReadBoolean();
                    var reasonValue = reader.ReadInt32();
                    var sessionReasonValue = reader.ReadInt32();
                    var detail = reader.ReadString();
                    var version = reader.ReadInt64();
                    var sequence = reader.ReadInt64();
                    if (string.IsNullOrWhiteSpace(submissionId) ||
                        submissionId.Length > MaxSubmissionIdChars ||
                        !Enum.IsDefined(typeof(SeatSubmissionRejectionReason), reasonValue) ||
                        !Enum.IsDefined(typeof(MatchSessionRejectionReason), sessionReasonValue) ||
                        detail.Length > PrivateSessionProtocol.MaxRejectDetailChars ||
                        version < 0 || sequence < 0 ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new NetworkSubmissionResultMessage(
                        envelope,
                        submissionId,
                        accepted,
                        (SeatSubmissionRejectionReason)reasonValue,
                        (MatchSessionRejectionReason)sessionReasonValue,
                        detail,
                        version,
                        sequence);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] SeatProjectionFrame(NetworkSeatProjectionMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Write(message.ProjectionPayload.Length);
                writer.Write(message.ProjectionPayload);
                writer.Write(message.MatchLog.Count);
                foreach (var row in message.MatchLog)
                {
                    NetworkMatchLogCodec.Write(writer, row);
                }

                writer.Flush();
                return PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.SeatProjection, stream.ToArray());
            }
        }

        /// <summary>
        /// The projection frame composed to FIT: the same layout as
        /// <see cref="SeatProjectionFrame"/>, with the OLDEST match-log rows dropped first when the
        /// whole message would not fit <paramref name="maxPayloadBytes"/>. Returns false - never a
        /// throw - when the envelope and projection payload alone cannot fit, which no amount of
        /// log trimming can mend.
        /// </summary>
        /// <remarks>
        /// <para>This is the send half of the frame-bound corrective whose growth half was 8.35w's
        /// retained-round window. <see cref="PrivateSessionFrameCodec.EncodeFrame"/> throws past
        /// <see cref="PrivateSessionProtocol.MaxPayloadBytes"/>, and a throw out of the host's own
        /// publish path is the recorded table-killer: the send sequence has already advanced, every
        /// resync is served down the same throwing path, and the match stalls for everyone with
        /// nothing on screen. The windows make that state unreachable today; this method is what
        /// keeps any future growth from being silent again.</para>
        /// <para>The log is the one trimmable tenant, and trimming it from the oldest end is the
        /// log's own recorded semantics - "recent history a player scrolls, not an archive" - so a
        /// shortened window is a smaller copy of exactly what the retained window already means.
        /// The projection payload is authoritative state and is never touched.</para>
        /// <para>A message that already fits produces byte-for-byte what
        /// <see cref="SeatProjectionFrame"/> produces, so the byte-level convergence assertions are
        /// not looking at a second encoder.</para>
        /// </remarks>
        public static bool TrySeatProjectionFrameWithin(
            NetworkSeatProjectionMessage message,
            int maxPayloadBytes,
            out byte[] frame,
            out int droppedLogRows)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (maxPayloadBytes < 1 || maxPayloadBytes > PrivateSessionProtocol.MaxPayloadBytes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxPayloadBytes), maxPayloadBytes,
                    "A compose budget is positive and never looser than the frame bound itself.");
            }

            frame = null;
            droppedLogRows = 0;
            int prefixBytes;
            var rowBytes = new int[message.MatchLog.Count];
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Write(message.ProjectionPayload.Length);
                writer.Write(message.ProjectionPayload);
                writer.Flush();
                prefixBytes = checked((int)stream.Length);

                // Each row's own encoded size, measured through the one real row writer, so the
                // fit decision and the bytes actually sent can never disagree.
                for (var index = 0; index < rowBytes.Length; index++)
                {
                    var before = stream.Length;
                    NetworkMatchLogCodec.Write(writer, message.MatchLog[index]);
                    writer.Flush();
                    rowBytes[index] = checked((int)(stream.Length - before));
                }
            }

            if (prefixBytes + sizeof(int) > maxPayloadBytes)
            {
                return false;
            }

            // The largest suffix of rows that fits: newest kept, oldest dropped, exactly the
            // retained window's own idea of which lines matter.
            var budget = maxPayloadBytes - prefixBytes - sizeof(int);
            var firstRetained = rowBytes.Length;
            while (firstRetained > 0 && budget >= rowBytes[firstRetained - 1])
            {
                firstRetained--;
                budget -= rowBytes[firstRetained];
            }

            droppedLogRows = firstRetained;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Write(message.ProjectionPayload.Length);
                writer.Write(message.ProjectionPayload);
                writer.Write(message.MatchLog.Count - firstRetained);
                for (var index = firstRetained; index < message.MatchLog.Count; index++)
                {
                    NetworkMatchLogCodec.Write(writer, message.MatchLog[index]);
                }

                writer.Flush();
                frame = PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.SeatProjection, stream.ToArray());
                return true;
            }
        }

        public static bool TryDecodeSeatProjection(
            byte[] payload, out NetworkSeatProjectionMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadEnvelope(reader, out var envelope))
                    {
                        return false;
                    }

                    var projectionLength = reader.ReadInt32();
                    if (projectionLength < 0 || projectionLength > stream.Length - stream.Position)
                    {
                        return false;
                    }

                    var projectionPayload = reader.ReadBytes(projectionLength);
                    if (projectionPayload.Length != projectionLength)
                    {
                        return false;
                    }

                    var logCount = reader.ReadInt32();
                    if (logCount < 0 || logCount > NetworkMatchLogCodec.MaxRows)
                    {
                        return false;
                    }

                    var log = new List<NetworkMatchLogRow>(logCount);
                    for (var index = 0; index < logCount; index++)
                    {
                        if (!NetworkMatchLogCodec.TryRead(reader, out var row))
                        {
                            return false;
                        }

                        log.Add(row);
                    }

                    if (stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new NetworkSeatProjectionMessage(
                        envelope, projectionPayload, Array.AsReadOnly(log.ToArray()));
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] SubmitAgreementFrame(NetworkAgreementSubmissionMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Write(message.SubmissionId);
                writer.Write(message.ExpectedVersion);
                NetworkProjectionDetailCodec.WriteAgreementStatement(writer, message.Statement);
                writer.Flush();
                return PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.SubmitAgreement, stream.ToArray());
            }
        }

        public static bool TryDecodeSubmitAgreement(
            byte[] payload, out NetworkAgreementSubmissionMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadEnvelope(reader, out var envelope))
                    {
                        return false;
                    }

                    var submissionId = reader.ReadString();
                    var expectedVersion = reader.ReadInt64();
                    if (string.IsNullOrWhiteSpace(submissionId) ||
                        submissionId.Length > MaxSubmissionIdChars ||
                        expectedVersion < 0 ||
                        !NetworkProjectionDetailCodec.TryReadAgreementStatement(reader, out var statement) ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new NetworkAgreementSubmissionMessage(
                        envelope, submissionId, expectedVersion, statement);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] AgreementResultFrame(NetworkAgreementResultMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Write(message.SubmissionId);
                writer.Write(message.Accepted);
                writer.Write((int)message.RejectionReason);
                writer.Write(message.Detail);
                writer.Write(message.Version);
                writer.Flush();
                return PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.AgreementResult, stream.ToArray());
            }
        }

        public static bool TryDecodeAgreementResult(
            byte[] payload, out NetworkAgreementResultMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadEnvelope(reader, out var envelope))
                    {
                        return false;
                    }

                    var submissionId = reader.ReadString();
                    var accepted = reader.ReadBoolean();
                    var reasonValue = reader.ReadInt32();
                    var detail = reader.ReadString();
                    var version = reader.ReadInt64();
                    if (string.IsNullOrWhiteSpace(submissionId) ||
                        submissionId.Length > MaxSubmissionIdChars ||
                        !Enum.IsDefined(typeof(TeamAgreementSubmissionRejectionReason), reasonValue) ||
                        detail.Length > PrivateSessionProtocol.MaxRejectDetailChars ||
                        version < 0 ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new NetworkAgreementResultMessage(
                        envelope,
                        submissionId,
                        accepted,
                        (TeamAgreementSubmissionRejectionReason)reasonValue,
                        detail,
                        version);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <summary>
        /// Milestone 8.35f. One seat's bounded rewind: the envelope, the submission identity and
        /// the version it was asked against. There is nothing else in it, because an undo names no
        /// target and takes exactly one step.
        /// </summary>
        public static byte[] SubmitUndoFrame(NetworkUndoSubmissionMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Write(message.SubmissionId);
                writer.Write(message.ExpectedVersion);
                writer.Flush();
                return PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.SubmitUndo, stream.ToArray());
            }
        }

        public static bool TryDecodeSubmitUndo(byte[] payload, out NetworkUndoSubmissionMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadEnvelope(reader, out var envelope))
                    {
                        return false;
                    }

                    var submissionId = reader.ReadString();
                    var expectedVersion = reader.ReadInt64();
                    if (string.IsNullOrWhiteSpace(submissionId) ||
                        submissionId.Length > MaxSubmissionIdChars ||
                        expectedVersion < 0 ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new NetworkUndoSubmissionMessage(envelope, submissionId, expectedVersion);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] UndoResultFrame(NetworkUndoResultMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Write(message.SubmissionId);
                writer.Write(message.Accepted);
                writer.Write((int)message.RejectionReason);
                writer.Write((int)message.SessionRejectionReason);
                writer.Write(message.Detail);
                writer.Write(message.Version);
                writer.Flush();
                return PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.UndoResult, stream.ToArray());
            }
        }

        public static bool TryDecodeUndoResult(byte[] payload, out NetworkUndoResultMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadEnvelope(reader, out var envelope))
                    {
                        return false;
                    }

                    var submissionId = reader.ReadString();
                    var accepted = reader.ReadBoolean();
                    var reasonValue = reader.ReadInt32();
                    var sessionReasonValue = reader.ReadInt32();
                    var detail = reader.ReadString();
                    var version = reader.ReadInt64();
                    if (string.IsNullOrWhiteSpace(submissionId) ||
                        submissionId.Length > MaxSubmissionIdChars ||
                        !Enum.IsDefined(typeof(SeatSubmissionRejectionReason), reasonValue) ||
                        !Enum.IsDefined(typeof(MatchSessionRejectionReason), sessionReasonValue) ||
                        detail.Length > PrivateSessionProtocol.MaxRejectDetailChars ||
                        version < 0 ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new NetworkUndoResultMessage(
                        envelope,
                        submissionId,
                        accepted,
                        (SeatSubmissionRejectionReason)reasonValue,
                        (MatchSessionRejectionReason)sessionReasonValue,
                        detail,
                        version);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static byte[] RequestResyncFrame(NetworkResyncRequestMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteEnvelope(writer, message.Envelope);
                writer.Flush();
                return PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.RequestResync, stream.ToArray());
            }
        }

        public static bool TryDecodeRequestResync(
            byte[] payload, out NetworkResyncRequestMessage message)
        {
            message = null;
            try
            {
                using (var stream = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!TryReadEnvelope(reader, out var envelope) ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new NetworkResyncRequestMessage(envelope);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        private static void WriteEnvelope(BinaryWriter writer, NetworkEnvelope envelope)
        {
            writer.Write(envelope.SessionCorrelation);
            writer.Write(envelope.Epoch);
            writer.Write(envelope.Sequence);
            writer.Write(envelope.Correlation);
        }

        private static bool TryReadEnvelope(BinaryReader reader, out NetworkEnvelope envelope)
        {
            envelope = default;
            var correlationId = reader.ReadString();
            var epoch = reader.ReadInt32();
            var sequence = reader.ReadInt64();
            var correlation = reader.ReadInt64();
            if (string.IsNullOrWhiteSpace(correlationId) || epoch < 1 || sequence < 1 ||
                correlation < 0)
            {
                return false;
            }

            envelope = new NetworkEnvelope(correlationId, epoch, sequence, correlation);
            return true;
        }
    }
}
