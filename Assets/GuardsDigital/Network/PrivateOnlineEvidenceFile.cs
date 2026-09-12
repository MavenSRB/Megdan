using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GuardsDigital.Application;
using GuardsDigital.Core.Rules.Victory;

namespace GuardsDigital.Network
{
    public enum PrivateOnlineEvidenceRole { Host, Client }

    /// <summary>
    /// A small public-metadata-only capture, independent of the general player log. Each session
    /// and process capture gets a new path; completed files are closed and never reopened.
    /// There is no parameter for payloads, keys, endpoints, hands or reconnect capabilities.
    /// </summary>
    public sealed class PrivateOnlineEvidenceFile : IDisposable
    {
        private readonly StreamWriter writer;
        private readonly string process = Guid.NewGuid().ToString("N");
        private bool closed;
        public string RunId { get; }
        public string SeatId { get; }
        public string Path { get; }
        public PrivateOnlineEvidenceRole Role { get; }

        public PrivateOnlineEvidenceFile(string directory, PrivateSessionIdentity identity, string runId,
            PrivateOnlineEvidenceRole role, string seatId)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            if (!Regex.IsMatch(runId ?? "", "\\A[a-f0-9]{12}-[a-f0-9]{32}\\z"))
                throw new ArgumentException("Evidence requires a host-instance identity.", nameof(runId));
            if (role != PrivateOnlineEvidenceRole.Host && role != PrivateOnlineEvidenceRole.Client)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (role == PrivateOnlineEvidenceRole.Client && string.IsNullOrWhiteSpace(seatId))
                throw new ArgumentException("Client evidence requires its own seat.", nameof(seatId));
            RunId = runId;
            Role = role;
            SeatId = role == PrivateOnlineEvidenceRole.Host ? "-" : seatId;
            var folder = System.IO.Path.Combine(directory, runId);
            Directory.CreateDirectory(folder);
            Path = System.IO.Path.Combine(folder, process + ".log");
            writer = new StreamWriter(new FileStream(Path, FileMode.CreateNew, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(false)) { AutoFlush = true };
            try { writer.WriteLine("GD_EVIDENCE_BEGIN run=" + Encode(runId) + " process=" + process +
                " role=" + role.ToString().ToLowerInvariant() + " seat=" + Encode(SeatId) +
                " protocol=" + Encode(identity.ProtocolFamily + "/" + identity.ProtocolVersion) +
                " schema=" + identity.SnapshotSchemaVersion.ToString(CultureInfo.InvariantCulture) +
                " map=" + Encode(identity.MapId + "@" + identity.MapContentRevision) +
                " format=" + Encode(identity.MatchFormatId) + " content=" + Encode(identity.ContentDigest) +
                " build=" + Encode(identity.BuildIdentity) + " topology=private-lobby-tcp"); }
            catch { writer.Dispose(); throw; }
        }

        public void Complete(long version, MatchOutcome outcome, IEnumerable<string> humanSeats = null,
            IEnumerable<string> botSeats = null)
        {
            if (closed || !outcome.HasEnded) return;
            if (Role == PrivateOnlineEvidenceRole.Host)
                writer.WriteLine("GD_EVIDENCE_PARTICIPANTS human=" + Encode(Seats(humanSeats)) +
                    " bot=" + Encode(Seats(botSeats)));
            writer.WriteLine("GD_EVIDENCE_END run=" + Encode(RunId) + " process=" + process +
                " version=" + version.ToString(CultureInfo.InvariantCulture) +
                " cause=" + Encode(outcome.Cause.ToString()) + " winner=" + Encode(outcome.HasSoloWinner ? outcome.WinningHeroId.Value + " (alone)" : outcome.WinningTeamId.Value));
            closed = true;
            writer.Dispose();
        }

        private static string Seats(IEnumerable<string> seats)
        {
            var values = (seats ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            return values.Length == 0 ? "-" : string.Join(",", values);
        }

        public void Dispose()
        {
            if (closed) return;
            closed = true;
            try { writer.WriteLine("GD_EVIDENCE_ABORT run=" + Encode(RunId) + " process=" + process); }
            finally { writer.Dispose(); }
        }

        private static string Encode(string value) => Uri.EscapeDataString(value ?? "");
    }
}
