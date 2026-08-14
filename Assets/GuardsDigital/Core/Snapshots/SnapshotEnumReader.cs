using System;
using System.IO;

namespace GuardsDigital.Core.Snapshots
{
    /// <summary>
    /// The decode boundary for every enum in an authoritative snapshot payload.
    /// </summary>
    /// <remarks>
    /// A raw <c>(SomeEnum)reader.ReadInt32()</c> accepts every integer the payload can hold, so
    /// whether an undefined value was caught depended entirely on which constructor happened to
    /// consume it. That coverage was inconsistent by accident rather than by design: about half the
    /// sites reached an <c>Enum.IsDefined</c> guard and surfaced late as
    /// <see cref="SnapshotLoadRejectionReason.MalformedPayload"/> - indistinguishable from a garbled
    /// byte stream - and the rest constructed semantically invalid state and carried on.
    ///
    /// This guarantees exactly one property: the value is a declared member of its enum. It
    /// deliberately does not reject <c>None</c>, or members that are illegal in a particular
    /// position. Those are rule meaning, and they already belong to the domain constructors that own
    /// them; duplicating them here would put rule meaning in the wire layer.
    ///
    /// The declared members are cached per type rather than routed through <c>Enum.IsDefined</c>,
    /// which is reflection-backed and boxes its argument. This decoder also runs on
    /// <c>MatchAggregate.Rollback</c>, so it executes on every refused or faulted command, not only
    /// on restore. The cache is built once per enum type per process; each read is then an integer
    /// scan over a handful of values, with no allocation and no boxing on the returned value.
    /// </remarks>
    public static class SnapshotEnumReader
    {
        /// <summary>
        /// Reads one int32 and returns it as <typeparamref name="T"/>, rejecting any value that is
        /// not a declared member. <paramref name="fieldName"/> names the field in the rejection
        /// message, matching the convention of the serializer's own <c>ReadCount</c>/<c>ReadMany</c>
        /// helpers.
        /// </summary>
        public static T Read<T>(BinaryReader reader, string fieldName) where T : struct, Enum
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var raw = reader.ReadInt32();
            var ordinals = EnumDomain<T>.Ordinals;
            for (var index = 0; index < ordinals.Length; index++)
            {
                if (ordinals[index] == raw) return EnumDomain<T>.Values[index];
            }

            throw new SnapshotLoadException(
                SnapshotLoadRejectionReason.UnrecognizedEnumValue,
                $"Snapshot field '{fieldName}' holds unrecognized {typeof(T).Name} value {raw}.");
        }

        /// <summary>
        /// Reads a presence boolean and, when set, the enum that follows it. Use only where the
        /// boolean is solely a presence flag; where the writer's boolean means something else as
        /// well, read it separately and call <see cref="Read{T}"/>.
        /// </summary>
        public static T? ReadNullable<T>(BinaryReader reader, string fieldName) where T : struct, Enum
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            return reader.ReadBoolean() ? Read<T>(reader, fieldName) : (T?)null;
        }

        private static class EnumDomain<T> where T : struct, Enum
        {
            internal static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
            internal static readonly int[] Ordinals = BuildOrdinals();

            private static int[] BuildOrdinals()
            {
                // Both of these are wire-format assumptions the writer already makes when it stores
                // an enum as `writer.Write((int)value)`. They are asserted here so a future enum
                // that breaks one fails immediately, naming itself, rather than truncating on write
                // or - for a flags enum - rejecting legitimate combined values in a player's
                // snapshot, which no test would be looking for.
                if (Enum.GetUnderlyingType(typeof(T)) != typeof(int))
                {
                    throw new InvalidOperationException(
                        $"{typeof(T).Name} is not int-backed, but the snapshot wire format stores every enum as an int32.");
                }

                if (typeof(T).IsDefined(typeof(FlagsAttribute), false))
                {
                    throw new InvalidOperationException(
                        $"{typeof(T).Name} is a flags enum, so a combination of its members is legal but is not itself a declared member. Give it a dedicated reader rather than the declared-member check.");
                }

                var ordinals = new int[Values.Length];
                for (var index = 0; index < Values.Length; index++)
                {
                    ordinals[index] = Convert.ToInt32(Values[index]);
                }

                return ordinals;
            }
        }
    }
}
