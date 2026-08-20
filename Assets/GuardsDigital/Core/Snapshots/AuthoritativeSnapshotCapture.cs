using System;
using System.Collections.Generic;
using System.IO;

namespace GuardsDigital.Core.Snapshots
{
    /// <summary>
    /// One authoritative serialization, held as the parts it was written from rather than as the
    /// encoded string: the bytes before the completed-round history, the history's cached fragment,
    /// and the bytes after it. The encoded payload is assembled and memoized on first read.
    /// </summary>
    /// <remarks>
    /// Milestone 7.9b. Every command captures a rollback pre-image, and the pre-image is a full
    /// authoritative serialization, so anything the serialization costs is a per-command cost. The
    /// current state the two byte spans cover is bounded, but the completed-round history grows
    /// every round, and `RISK-PERF-001`'s measured growth term was dominated by re-encoding it -
    /// buffers, copy, and Base64 are all proportional to the whole payload. A capture writes only
    /// the current state and shares the history's bytes with every other capture through
    /// <see cref="CompletedRoundsSerializationCache"/>, so what a command pays stops depending on
    /// how many rounds the match has completed.
    ///
    /// A capture is immutable: its spans are private copies and the fragment's visible range is
    /// never overwritten, so <see cref="Payload"/> answers the same bytes however late it is read.
    /// The pre-image's consumers keep their exact semantics - a rollback deserializes
    /// <see cref="Payload"/>, and the rejected-command atomicity comparison asks
    /// <see cref="ContentEquals"/>, which compares the same serialized content the payload
    /// comparison compared, without either side materializing a string.
    /// </remarks>
    public sealed class AuthoritativeSnapshotCapture
    {
        private readonly byte[] prefix;
        private readonly byte[] suffix;
        private readonly CompletedRoundsFragment completedRounds;
        private string memoizedPayload;

        internal AuthoritativeSnapshotCapture(
            byte[] prefix,
            CompletedRoundsFragment completedRounds,
            byte[] suffix)
        {
            this.prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
            this.completedRounds = completedRounds;
            this.suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
        }

        /// <summary>
        /// The exact payload <see cref="AuthoritativeSnapshotSerializer.Serialize"/> describes,
        /// assembled on first read and then held. Byte-identical to writing the whole state in one
        /// pass, because both paths run the same section writers.
        /// </summary>
        public string Payload => memoizedPayload ?? (memoizedPayload = MaterializePayload());

        private string MaterializePayload()
        {
            var fragmentOffset = prefix.Length + sizeof(int);
            var bytes = new byte[fragmentOffset + completedRounds.ByteLength + suffix.Length];
            Buffer.BlockCopy(prefix, 0, bytes, 0, prefix.Length);

            // The count `BinaryWriter` would have written where the history block begins.
            var count = completedRounds.RecordCount;
            bytes[prefix.Length] = (byte)count;
            bytes[prefix.Length + 1] = (byte)(count >> 8);
            bytes[prefix.Length + 2] = (byte)(count >> 16);
            bytes[prefix.Length + 3] = (byte)(count >> 24);

            completedRounds.CopyTo(bytes, fragmentOffset);
            Buffer.BlockCopy(suffix, 0, bytes, fragmentOffset + completedRounds.ByteLength, suffix.Length);
            var encoded = Convert.ToBase64String(bytes);
            AuthoritativeSnapshotSerializer.CountMaterializedPayload(encoded.Length);
            return encoded;
        }

        /// <summary>
        /// Whether this capture and <paramref name="other"/> describe the same serialized state -
        /// the comparison the rejected-command atomicity check makes, equal exactly when the two
        /// payload strings would be equal.
        /// </summary>
        public bool ContentEquals(AuthoritativeSnapshotCapture other)
        {
            return other != null &&
                BytesEqual(prefix, other.prefix) &&
                BytesEqual(suffix, other.suffix) &&
                completedRounds.ContentEquals(other.completedRounds);
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left.Length != right.Length) return false;
            for (var index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index]) return false;
            }

            return true;
        }
    }

    /// <summary>
    /// An immutable view of the cache's buffer: the serialized bytes of the first
    /// <see cref="RecordCount"/> completed rounds. Appends land past every view's visible range and
    /// growth or truncation replaces the buffer, so a view stays correct forever - the same terms
    /// the append-only event logs' snapshots state.
    /// </summary>
    internal readonly struct CompletedRoundsFragment
    {
        private readonly byte[] buffer;

        internal CompletedRoundsFragment(byte[] buffer, int byteLength, int recordCount)
        {
            this.buffer = buffer;
            ByteLength = byteLength;
            RecordCount = recordCount;
        }

        public int ByteLength { get; }
        public int RecordCount { get; }

        public void CopyTo(byte[] destination, int destinationOffset)
        {
            if (ByteLength > 0)
            {
                Buffer.BlockCopy(buffer, 0, destination, destinationOffset, ByteLength);
            }
        }

        public bool ContentEquals(CompletedRoundsFragment other)
        {
            if (RecordCount != other.RecordCount || ByteLength != other.ByteLength)
            {
                return false;
            }

            // Two views over one cache with equal counts are one value; different buffers - a
            // rebuilt cache, or a rollback's - are compared by content, which is what "the refusal
            // moved nothing" means.
            if (ReferenceEquals(buffer, other.buffer)) return true;
            for (var index = 0; index < ByteLength; index++)
            {
                if (buffer[index] != other.buffer[index]) return false;
            }

            return true;
        }
    }

    /// <summary>
    /// The serialized bytes of every completed round, written once when the round freezes and
    /// shared by every capture after it.
    /// </summary>
    /// <remarks>
    /// A derived cache in the acceptance checks' sense: it holds no answer of its own to what the
    /// match is. Each call re-validates by reference against the authority's own record list - the
    /// records are deeply immutable, so reference identity is content identity - and any
    /// divergence, as a rollback or a restore produces, truncates onto a fresh buffer and
    /// re-serializes from the trusted records. Discarding the whole cache would change nothing but
    /// the cost of the next capture.
    /// </remarks>
    public sealed class CompletedRoundsSerializationCache
    {
        private readonly List<Cards.CompletedRoundRecord> cachedRecords =
            new List<Cards.CompletedRoundRecord>();
        private readonly List<int> recordEndOffsets = new List<int>();
        private byte[] buffer = Array.Empty<byte>();
        private int length;

        internal CompletedRoundsFragment GetFragment(
            IReadOnlyList<Cards.CompletedRoundRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));

            var comparable = Math.Min(cachedRecords.Count, records.Count);
            var matched = 0;
            while (matched < comparable && ReferenceEquals(cachedRecords[matched], records[matched]))
            {
                matched++;
            }

            if (matched < cachedRecords.Count)
            {
                // Copy-on-truncate: outstanding fragments keep reading the buffer they were given.
                var keptLength = matched == 0 ? 0 : recordEndOffsets[matched - 1];
                var replacement = new byte[keptLength];
                Array.Copy(buffer, replacement, keptLength);
                buffer = replacement;
                length = keptLength;
                cachedRecords.RemoveRange(matched, cachedRecords.Count - matched);
                recordEndOffsets.RemoveRange(matched, recordEndOffsets.Count - matched);
            }

            for (var index = cachedRecords.Count; index < records.Count; index++)
            {
                Append(records[index]);
            }

            return new CompletedRoundsFragment(buffer, length, records.Count);
        }

        private void Append(Cards.CompletedRoundRecord record)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                AuthoritativeSnapshotSerializer.WriteCompletedRound(writer, record);
                writer.Flush();
                var appended = checked((int)stream.Length);
                if (length + appended > buffer.Length)
                {
                    // A new buffer rather than an in-place resize, so every outstanding fragment
                    // keeps the buffer it was handed.
                    var grown = new byte[Math.Max(buffer.Length * 2, length + appended)];
                    Array.Copy(buffer, grown, length);
                    buffer = grown;
                }

                Array.Copy(stream.GetBuffer(), 0, buffer, length, appended);
                length += appended;
                cachedRecords.Add(record);
                recordEndOffsets.Add(length);
                AuthoritativeSnapshotSerializer.CountCapturedBytes(appended);
            }
        }
    }
}
