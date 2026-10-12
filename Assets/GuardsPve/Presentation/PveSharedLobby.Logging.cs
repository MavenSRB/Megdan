using System;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Pve.Play;
using UnityEngine;

namespace GuardsDigital.Pve.Presentation
{
    public sealed partial class PveSharedLobby
    {
        private GameplayNarrationRecorder narration;
        private string narrationPath;
        private PrivateOnlineEvidenceFile hostEvidence;
        private bool hostEvidenceUnavailable;
        private bool endingNarrated;

        private void AttachSavedNarration()
        {
            if (server == null) return; // Like PvP, only the authority saves full gameplay narration.
            narration = GameplayNarrationFile.TryOpen(out narrationPath);
            server.AttachGameplayNarration(narration, identity.BuildIdentity, identity.MapId);
        }

        private void CaptureSavedLogs()
        {
            var directory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "..", UnityEngine.Application.isEditor ? "tmp" : "Logs", "online-runs"));
            if (server != null)
                EnsureCapture(ref hostEvidence, ref hostEvidenceUnavailable, directory,
                    server.SessionCorrelation, PrivateOnlineEvidenceRole.Host, "");
            foreach (var connection in connections)
            {
                var client = connection.Client;
                if (client.State != PrivateSessionClientState.Seated || client.LatestProjection == null) continue;
                EnsureCapture(ref connection.Evidence, ref connection.EvidenceUnavailable, directory,
                    client.SessionCorrelation, PrivateOnlineEvidenceRole.Client, client.TargetSeat.Value);
            }

            var projection = server != null ? match.Session.Projection : runtime.ReadModel;
            if (!PveEnemyController.IsLost(projection)) return;
            var seats = runtime.Host.Seats.Seats.Where(seat => seat.TeamSide == TeamSide.Blue)
                .Select(seat => seat.Id.Value).ToArray();
            if (server != null && !endingNarrated)
            {
                endingNarrated = true;
                narration?.WriteOutcome(projection.Version,
                    projection.Outcome.HasEnded ? projection.Outcome.Cause.ToString() : "All friendly creeps were defeated",
                    projection.Outcome.HasEnded ? projection.Outcome.HasSoloWinner
                        ? projection.Outcome.WinningHeroId.Value : projection.Outcome.WinningTeamId.Value : "Blue defeated");
            }
            CompleteCapture(hostEvidence, projection, seats);
            foreach (var connection in connections)
                if (connection.Client.LatestProjection?.Version == projection.Version)
                    CompleteCapture(connection.Evidence, projection, seats);
        }

        private void EnsureCapture(ref PrivateOnlineEvidenceFile capture, ref bool unavailable,
            string directory, string correlation, PrivateOnlineEvidenceRole role, string seat)
        {
            if (capture != null || unavailable) return;
            try { capture = new PrivateOnlineEvidenceFile(directory, identity, correlation, role, seat); }
            catch (IOException) { unavailable = true; CaptureUnavailable(); }
            catch (UnauthorizedAccessException) { unavailable = true; CaptureUnavailable(); }
        }

        private static void CompleteCapture(PrivateOnlineEvidenceFile capture, IMatchReadModel projection, string[] seats)
        {
            if (capture == null) return;
            try
            {
                if (projection.Outcome.HasEnded) capture.Complete(projection.Version, projection.Outcome, seats);
                else capture.CompleteModeEnding(projection.Version, seats);
            }
            catch (IOException) { CaptureUnavailable(); }
            catch (UnauthorizedAccessException) { CaptureUnavailable(); }
        }

        private static void CloseCapture(IDisposable capture)
        {
            try { capture?.Dispose(); }
            catch (IOException) { CaptureUnavailable(); }
            catch (UnauthorizedAccessException) { CaptureUnavailable(); }
        }

        private static void CaptureUnavailable() =>
            Debug.LogWarning("GUARDS_ONLINE: EVIDENCE_UNAVAILABLE (local capture failed)");

        private void CloseSavedLogs()
        {
            server?.AttachGameplayNarration(null);
            CloseCapture(narration);
            narration = null;
            CloseCapture(hostEvidence);
            hostEvidence = null;
        }
    }
}
