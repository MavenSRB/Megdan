using System;
using System.Collections.Generic;
namespace GuardsDigital.Application
{
    public sealed partial class MatchHost
    {
        private readonly TeamCoordination coordination = new TeamCoordination();
        public ExperimentalMatchOptions Experiments { get; }
        private IReadOnlyList<TeammateMarker> CoordinationFor(MatchSeat seat)
        {
            if (!Experiments.TeammateCoordination) return Array.Empty<TeammateMarker>();
            coordination.Revalidate(session.Projection, CurrentSeats());
            return coordination.ForSeat(seat, CurrentSeats());
        }
        public bool SubmitCoordinationForSeat(SeatId seat, CoordinationCommand command)
        {
            lock (gate)
            {
                if (executing || !Experiments.TeammateCoordination) return false;
                return coordination.Submit(CurrentSeats().Find(seat), command, session.Projection, CurrentSeats());
            }
        }
        private const string ExperimentalSnapshotPrefix = "guards-experimental/1:";
        private string CaptureExperimentalSnapshot()
        {
            lock (gate)
            {
                var payload = session.CaptureSnapshot();
                if (!Experiments.TeammateCoordination && !Experiments.TurnTimer) return payload;
                using (var stream = new System.IO.MemoryStream())
                using (var writer = new System.IO.BinaryWriter(stream, System.Text.Encoding.UTF8))
                {
                    ExperimentalWireCodec.WriteOptions(writer, Experiments);
                    writer.Write(payload); writer.Flush();
                    return ExperimentalSnapshotPrefix + Convert.ToBase64String(stream.ToArray());
                }
            }
        }
        private string UnwrapExperimentalSnapshot(string payload)
        {
            if (payload == null || !payload.StartsWith(ExperimentalSnapshotPrefix, StringComparison.Ordinal))
                return Experiments.TeammateCoordination || Experiments.TurnTimer ? "Experimental settings are missing from this snapshot." : payload;
            try
            {
                using (var stream = new System.IO.MemoryStream(Convert.FromBase64String(payload.Substring(ExperimentalSnapshotPrefix.Length))))
                using (var reader = new System.IO.BinaryReader(stream, System.Text.Encoding.UTF8))
                {
                    var options = ExperimentalWireCodec.ReadOptions(reader);
                    var inner = reader.ReadString();
                    return options.Equals(Experiments) && stream.Position == stream.Length
                        ? inner : "The snapshot belongs to different experimental settings.";
                }
            }
            catch (FormatException) { return "Invalid experimental snapshot."; }
            catch (System.IO.IOException) { return "Invalid experimental snapshot."; }
            catch (ArgumentException) { return "Invalid experimental snapshot."; }
        }
        public void ClearCoordinationForSeat(SeatId seat)
        {
            lock (gate)
            {
                var bound = CurrentSeats().Find(seat);
                if (bound != null) coordination.Remove(bound.HeroId);
            }
        }
    }
}
