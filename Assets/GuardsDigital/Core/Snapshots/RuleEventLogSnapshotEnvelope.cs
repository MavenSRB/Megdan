using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Core.Diagnostics;

namespace GuardsDigital.Core.Snapshots
{
    public enum RuleEventLogEnvelopeRejectionReason
    {
        None = 0,
        BlankPayload = 1,
        InvalidBase64 = 2,
        OversizedPayload = 3,
        TruncatedPayload = 4,
        InvalidHeader = 5,
        UnsupportedVersion = 6,
        InvalidEntryCount = 7,
        InvalidEntry = 8,
        TrailingData = 9,
        MalformedPayload = 10
    }

    public sealed class RuleEventLogSnapshotEnvelope
    {
        private const string Magic = "GDRL";
        private const int CurrentVersion = 1;
        private const int MaximumDecodedBytes = 4 * 1024 * 1024;
        private const int MaximumBase64Characters = ((MaximumDecodedBytes + 2) / 3) * 4;
        private const int MaximumAuthoritativePayloadBytes = 3 * 1024 * 1024;
        private const int MaximumEntryTextBytes = 64 * 1024;
        private const int MaximumEntryCount = 10000;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private RuleEventLogSnapshotEnvelope(
            string authoritativePayload,
            IEnumerable<RuleEventLogEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(authoritativePayload))
            {
                throw new ArgumentException("The envelope requires an authoritative snapshot.", nameof(authoritativePayload));
            }

            AuthoritativePayload = authoritativePayload;
            Entries = Array.AsReadOnly((entries ?? throw new ArgumentNullException(nameof(entries))).ToArray());
        }

        public string AuthoritativePayload { get; }

        public IReadOnlyList<RuleEventLogEntry> Entries { get; }

        public static string Serialize(
            string authoritativePayload,
            IEnumerable<RuleEventLogEntry> entries)
        {
            var snapshot = new RuleEventLogSnapshotEnvelope(authoritativePayload, entries);
            if (StrictUtf8.GetByteCount(snapshot.AuthoritativePayload) > MaximumAuthoritativePayloadBytes)
            {
                throw new ArgumentException("The authoritative snapshot exceeds the envelope size limit.",
                    nameof(authoritativePayload));
            }

            if (snapshot.Entries.Count < 1 || snapshot.Entries.Count > MaximumEntryCount)
            {
                throw new ArgumentException("The rule-event log entry count exceeds the envelope limit.",
                    nameof(entries));
            }

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Magic);
                writer.Write(CurrentVersion);
                writer.Write(snapshot.AuthoritativePayload);
                writer.Write(snapshot.Entries.Count);
                foreach (var entry in snapshot.Entries)
                {
                    ValidateEntryString(entry.EventType, "event type", nameof(entries));
                    ValidateEntryString(entry.StableReferences, "stable references", nameof(entries));
                    ValidateEntryString(entry.Detail, "event detail", nameof(entries));
                    writer.Write(entry.Sequence);
                    writer.Write(entry.EventType);
                    writer.Write(entry.StableReferences);
                    writer.Write(entry.Detail);
                }

                writer.Flush();
                if (stream.Length > MaximumDecodedBytes)
                {
                    throw new ArgumentException("The rule-event log envelope exceeds its total size limit.",
                        nameof(entries));
                }

                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static bool TryDeserialize(string payload, out RuleEventLogSnapshotEnvelope envelope)
        {
            return TryDeserialize(payload, out envelope, out _);
        }

        public static bool TryDeserialize(
            string payload,
            out RuleEventLogSnapshotEnvelope envelope,
            out RuleEventLogEnvelopeRejectionReason rejectionReason)
        {
            envelope = null;
            rejectionReason = RuleEventLogEnvelopeRejectionReason.None;
            if (string.IsNullOrWhiteSpace(payload))
            {
                rejectionReason = RuleEventLogEnvelopeRejectionReason.BlankPayload;
                return false;
            }

            if (payload.Length > MaximumBase64Characters)
            {
                rejectionReason = RuleEventLogEnvelopeRejectionReason.OversizedPayload;
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                rejectionReason = RuleEventLogEnvelopeRejectionReason.InvalidBase64;
                return false;
            }

            if (bytes.Length > MaximumDecodedBytes)
            {
                rejectionReason = RuleEventLogEnvelopeRejectionReason.OversizedPayload;
                return false;
            }

            try
            {
                using (var stream = new MemoryStream(bytes, false))
                using (var reader = new BinaryReader(stream, StrictUtf8, true))
                {
                    var magic = ReadBoundedString(reader, Magic.Length, "header");
                    if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                    {
                        rejectionReason = RuleEventLogEnvelopeRejectionReason.InvalidHeader;
                        return false;
                    }

                    var version = reader.ReadInt32();
                    if (version != CurrentVersion)
                    {
                        rejectionReason = RuleEventLogEnvelopeRejectionReason.UnsupportedVersion;
                        return false;
                    }

                    var authoritativePayload = ReadBoundedString(
                        reader,
                        MaximumAuthoritativePayloadBytes,
                        "authoritative snapshot");
                    if (string.IsNullOrWhiteSpace(authoritativePayload))
                    {
                        rejectionReason = RuleEventLogEnvelopeRejectionReason.MalformedPayload;
                        return false;
                    }

                    var entryCount = reader.ReadInt32();
                    if (entryCount < 1 || entryCount > MaximumEntryCount)
                    {
                        rejectionReason = RuleEventLogEnvelopeRejectionReason.InvalidEntryCount;
                        return false;
                    }

                    var entries = new RuleEventLogEntry[entryCount];
                    for (var index = 0; index < entryCount; index++)
                    {
                        entries[index] = new RuleEventLogEntry(
                            reader.ReadInt32(),
                            ReadBoundedString(reader, MaximumEntryTextBytes, "event type"),
                            ReadBoundedString(reader, MaximumEntryTextBytes, "stable references"),
                            ReadBoundedString(reader, MaximumEntryTextBytes, "event detail"));
                        if (entries[index].Sequence != index + 1)
                        {
                            rejectionReason = RuleEventLogEnvelopeRejectionReason.InvalidEntry;
                            return false;
                        }
                    }

                    if (stream.Position != stream.Length)
                    {
                        rejectionReason = RuleEventLogEnvelopeRejectionReason.TrailingData;
                        return false;
                    }

                    envelope = new RuleEventLogSnapshotEnvelope(authoritativePayload, entries);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                rejectionReason = RuleEventLogEnvelopeRejectionReason.TruncatedPayload;
                return false;
            }
            catch (DecoderFallbackException)
            {
                rejectionReason = RuleEventLogEnvelopeRejectionReason.MalformedPayload;
                return false;
            }
            catch (IOException)
            {
                rejectionReason = RuleEventLogEnvelopeRejectionReason.MalformedPayload;
                return false;
            }
            catch (ArgumentException)
            {
                rejectionReason = RuleEventLogEnvelopeRejectionReason.InvalidEntry;
                return false;
            }
            catch (OverflowException)
            {
                rejectionReason = RuleEventLogEnvelopeRejectionReason.MalformedPayload;
                return false;
            }
        }

        private static string ReadBoundedString(BinaryReader reader, int maximumByteCount, string fieldName)
        {
            var byteCount = Read7BitEncodedInt(reader);
            if (byteCount < 0 || byteCount > maximumByteCount)
            {
                throw new IOException($"The rule-event log {fieldName} exceeds its size limit.");
            }

            var bytes = reader.ReadBytes(byteCount);
            if (bytes.Length != byteCount)
            {
                throw new EndOfStreamException($"The rule-event log {fieldName} is truncated.");
            }

            return StrictUtf8.GetString(bytes);
        }

        private static int Read7BitEncodedInt(BinaryReader reader)
        {
            var value = 0;
            for (var shift = 0; shift < 35; shift += 7)
            {
                var current = reader.ReadByte();
                value |= (current & 0x7f) << shift;
                if ((current & 0x80) == 0)
                {
                    return value;
                }
            }

            throw new IOException("The rule-event log contains an invalid string-length prefix.");
        }

        private static void ValidateEntryString(string value, string fieldName, string parameterName)
        {
            if (StrictUtf8.GetByteCount(value) > MaximumEntryTextBytes)
            {
                throw new ArgumentException(
                    $"The rule-event log {fieldName} exceeds the envelope size limit.",
                    parameterName);
            }
        }
    }
}
