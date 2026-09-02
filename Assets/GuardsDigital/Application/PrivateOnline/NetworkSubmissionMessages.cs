using System;
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

        /// <summary>Echoed by a reply; zero on an unsolicited host push.</summary>
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
        public NetworkSeatProjectionMessage(NetworkEnvelope envelope, byte[] projectionPayload)
        {
            Envelope = envelope;
            ProjectionPayload = projectionPayload ?? throw new ArgumentNullException(nameof(projectionPayload));
        }

        public NetworkEnvelope Envelope { get; }
        public byte[] ProjectionPayload { get; }
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
                writer.Flush();
                return PrivateSessionFrameCodec.EncodeFrame(
                    SessionMessageKind.SeatProjection, stream.ToArray());
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
                    if (projectionPayload.Length != projectionLength ||
                        stream.Position != stream.Length)
                    {
                        return false;
                    }

                    message = new NetworkSeatProjectionMessage(envelope, projectionPayload);
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
