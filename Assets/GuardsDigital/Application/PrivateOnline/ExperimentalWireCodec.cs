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
        internal static void WriteClock(BinaryWriter w, MatchClockState state)
        {
            w.Write(state.Enabled); w.Write(state.Revision); w.Write(state.Wave);
            w.Write(state.RedBankTicks); w.Write(state.BlueBankTicks); w.Write(state.Entries.Count);
            foreach(var e in state.Entries)
            { w.Write(e.Id); w.Write((int)e.Kind); w.Write(e.Owner.Value??string.Empty); w.Write((int)e.Team); w.Write(e.RemainingTicks); w.Write(e.Running); w.Write(e.IsCurrent); }
            w.Write(state.BoardVotes.Count);
            foreach(var vote in state.BoardVotes)
            { w.Write(vote.Voter.Value); w.Write(vote.Position.Q);w.Write(vote.Position.R);var payload=NetworkCommandCodec.Encode(vote.Command);w.Write(payload.Length);w.Write(payload); }
        }
        internal static MatchClockState ReadClock(BinaryReader r)
        {
            var enabled=r.ReadBoolean(); var revision=r.ReadInt64(); var wave=r.ReadInt32();
            var red=r.ReadInt64(); var blue=r.ReadInt64(); var count=r.ReadInt32();
            if (count<0 || count>4096) throw new ArgumentException("Invalid clock count.");
            var entries=new List<MatchClockEntry>();
            for(int i=0;i<count;i++)
            {
                var id=ReadId(r); var kind=(MatchClockKind)r.ReadInt32(); var owner=ReadId(r);
                entries.Add(new MatchClockEntry(id,kind,owner.Length==0?default:new HeroId(owner),
                    (GuardsDigital.Core.Participants.TeamSide)r.ReadInt32(),r.ReadInt64(),r.ReadBoolean(),r.ReadBoolean()));
            }
            var voteCount=r.ReadInt32(); if(voteCount<0 || voteCount>10) throw new ArgumentException("Invalid board vote count.");
            var votes=new List<BoardChoiceVote>();
            for(int i=0;i<voteCount;i++)
            {
                var hero=new HeroId(ReadId(r)); var position=new HexCoordinate(r.ReadInt32(),r.ReadInt32());var size=r.ReadInt32();
                if(size<0 || size>65536 || !NetworkCommandCodec.TryDecode(r.ReadBytes(size),out var command)) throw new ArgumentException("Invalid board vote.");
                votes.Add(new BoardChoiceVote(hero,position,command));
            }
            return new MatchClockState(enabled,revision,wave,red,blue,entries,votes);
        }
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
        public static byte[] BoardVoteFrame(NetworkEnvelope envelope,long version,GuardsDigital.Core.Replay.DirectorCommand command)
        {
            using(var s=new MemoryStream()) using(var w=new BinaryWriter(s))
            { WriteEnvelope(w,envelope);w.Write("board-vote");w.Write(version);var payload=NetworkCommandCodec.Encode(command);w.Write(payload.Length);w.Write(payload);w.Flush();return PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.BoardVote,s.ToArray()); }
        }
        public static byte[] TimerReadinessFrame(NetworkEnvelope envelope,long version,bool ready)
        {
            using(var s=new MemoryStream()) using(var w=new BinaryWriter(s,System.Text.Encoding.UTF8))
            { WriteEnvelope(w,envelope); w.Write(version); w.Write(ready); w.Flush(); return PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.TimerReadiness,s.ToArray()); }
        }
        public static bool TryDecodeTimerReadiness(byte[] payload,out NetworkEnvelope envelope,out long version,out bool ready)
        {
            envelope=default; version=0; ready=false;
            try
            {
                using(var s=new MemoryStream(payload??Array.Empty<byte>(),false)) using(var r=new BinaryReader(s))
                { if(!TryReadEnvelope(r,out envelope)) return false; version=r.ReadInt64(); ready=r.ReadBoolean(); return version>=0 && s.Position==s.Length; }
            }
            catch(IOException) { return false; } catch(ArgumentException) { return false; }
        }
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
