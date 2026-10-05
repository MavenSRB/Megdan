using System;
using System.Collections.Generic;
using System.IO;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
namespace GuardsDigital.Application
{
    internal static class ExperimentalWireCodec
    {
        internal static void WriteOptions(BinaryWriter w, ExperimentalMatchOptions o)
        {
            w.Write(o.TeammateCoordination); w.Write(o.TurnTimer);
            w.Write(o.CardCommitmentSeconds); w.Write(o.CardResolutionSeconds); w.Write(o.ResponseSeconds);
            w.Write(o.CardUpgradeSeconds); w.Write(o.TeamBankSeconds);
        }
        internal static ExperimentalMatchOptions ReadOptions(BinaryReader r) => new ExperimentalMatchOptions(
            r.ReadBoolean(), r.ReadBoolean(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
        internal static void WriteMarkers(BinaryWriter w, IReadOnlyList<TeammateMarker> markers)
        {
            w.Write(markers.Count);
            foreach (var m in markers)
            {
                w.Write(m.Sender.Value); w.Write(m.Revision); w.Write(m.Hex.HasValue);
                if (m.Hex.HasValue) { w.Write(m.Hex.Value.Q); w.Write(m.Hex.Value.R); }
                w.Write(m.CardOwner.Value ?? string.Empty); w.Write(m.Card.Value ?? string.Empty);
            }
        }
        internal static IReadOnlyList<TeammateMarker> ReadMarkers(BinaryReader r)
        {
            int count = r.ReadInt32();
            if (count < 0 || count > 10) throw new ArgumentException("Invalid marker count.");
            var result = new List<TeammateMarker>();
            var senders = new HashSet<HeroId>();
            for (int i = 0; i < count; i++)
            {
                var sender = new HeroId(ReadId(r)); var revision = r.ReadInt64();
                if (revision < 0 || !senders.Add(sender)) throw new ArgumentException("Invalid marker revision/sender.");
                HexCoordinate? hex = r.ReadBoolean() ? new HexCoordinate(r.ReadInt32(), r.ReadInt32()) : (HexCoordinate?)null;
                var owner = ReadId(r); var card = ReadId(r);
                if ((owner.Length == 0) != (card.Length == 0)) throw new ArgumentException("Incomplete card marker.");
                result.Add(new TeammateMarker(sender, revision, hex,
                    owner.Length == 0 ? default : new HeroId(owner), card.Length == 0 ? default : new CardInstanceId(card)));
            }
            return result.AsReadOnly();
        }
        internal static string ReadId(BinaryReader r)
        {
            var value = r.ReadString();
            if (value.Length > 256) throw new ArgumentException("Unbounded identifier.");
            return value;
        }
    }
    public static partial class NetworkMessageCodec
    {
        public static byte[] SubmitCoordinationFrame(NetworkEnvelope envelope, CoordinationCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            using (var s = new MemoryStream())
            using (var w = new BinaryWriter(s, System.Text.Encoding.UTF8))
            {
                WriteEnvelope(w, envelope); w.Write(command.ExpectedRevision); w.Write(command.Generation); w.Write((int)command.Kind);
                w.Write(command.Clear); w.Write(command.Map.MapId.Value); w.Write(command.Map.ContentRevision);
                w.Write(command.Hex.Q); w.Write(command.Hex.R);
                w.Write(command.CardOwner.Value ?? string.Empty); w.Write(command.Card.Value ?? string.Empty);
                w.Flush(); return PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.SubmitCoordination, s.ToArray());
            }
        }
        public static bool TryDecodeSubmitCoordination(byte[] payload, out NetworkEnvelope envelope, out CoordinationCommand command)
        {
            envelope = default; command = null;
            try
            {
                using (var s = new MemoryStream(payload ?? Array.Empty<byte>(), false))
                using (var r = new BinaryReader(s, System.Text.Encoding.UTF8))
                {
                    if (!TryReadEnvelope(r, out envelope)) return false;
                    var revision = r.ReadInt64(); var generation = r.ReadInt32(); var kind = (CoordinationTargetKind)r.ReadInt32(); var clear = r.ReadBoolean();
                    var map = new MapDefinitionReference(new MapId(ExperimentalWireCodec.ReadId(r)), r.ReadInt32());
                    var hex = new HexCoordinate(r.ReadInt32(), r.ReadInt32());
                    var owner = ExperimentalWireCodec.ReadId(r); var card = ExperimentalWireCodec.ReadId(r);
                    if (s.Position != s.Length) return false;
                    command = new CoordinationCommand(revision, kind, clear, map, hex,
                        owner.Length == 0 ? default : new HeroId(owner), card.Length == 0 ? default : new CardInstanceId(card), generation);
                    return true;
                }
            }
            catch (IOException) { return false; }
            catch (FormatException) { return false; }
            catch (ArgumentException) { return false; }
        }
    }
}
