using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Core.Diagnostics;

namespace GuardsDigital.Core.Snapshots
{
    public sealed class RuleEventLogSnapshotEnvelope
    {
        private const string Magic = "GDRL";
        private const int CurrentVersion = 1;

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
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Magic);
                writer.Write(CurrentVersion);
                writer.Write(snapshot.AuthoritativePayload);
                writer.Write(snapshot.Entries.Count);
                foreach (var entry in snapshot.Entries)
                {
                    writer.Write(entry.Sequence);
                    writer.Write(entry.EventType);
                    writer.Write(entry.StableReferences);
                    writer.Write(entry.Detail);
                }

                writer.Flush();
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static bool TryDeserialize(string payload, out RuleEventLogSnapshotEnvelope envelope)
        {
            envelope = null;
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                return false;
            }

            using (var stream = new MemoryStream(bytes, false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                string magic;
                try
                {
                    magic = reader.ReadString();
                }
                catch (EndOfStreamException)
                {
                    return false;
                }

                if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                {
                    return false;
                }

                var version = reader.ReadInt32();
                if (version != CurrentVersion)
                {
                    throw new InvalidDataException($"Unsupported rule-event log envelope version {version}.");
                }

                var authoritativePayload = reader.ReadString();
                var entryCount = reader.ReadInt32();
                if (entryCount < 1)
                {
                    throw new InvalidDataException("A rule-event log envelope requires at least its setup entry.");
                }

                var entries = new RuleEventLogEntry[entryCount];
                for (var index = 0; index < entryCount; index++)
                {
                    entries[index] = new RuleEventLogEntry(
                        reader.ReadInt32(),
                        reader.ReadString(),
                        reader.ReadString(),
                        reader.ReadString());
                    if (entries[index].Sequence != index + 1)
                    {
                        throw new InvalidDataException(
                            "Rule-event log envelope entries must have contiguous one-based sequence numbers.");
                    }
                }

                if (stream.Position != stream.Length)
                {
                    throw new InvalidDataException("The rule-event log envelope contains trailing data.");
                }

                envelope = new RuleEventLogSnapshotEnvelope(authoritativePayload, entries);
                return true;
            }
        }
    }
}
