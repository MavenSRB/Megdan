using System;
using System.Globalization;
using GuardsDigital.Application;
using GuardsDigital.Network;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Milestone 8.31. The shared vocabulary of the private-online process drivers: one marker
    /// prefix, mirrored from the smoke gate, because the rehearsal script reads the player logs as
    /// its only channel.
    /// </summary>
    /// <remarks>
    /// No marker ever carries a remote address: connections are named by the host's opaque
    /// ordinals and seats by their ids, which is the whole diagnostic identity the amended
    /// architecture record allows.
    /// </remarks>
    internal static class PrivateOnlineMarkers
    {
        public const string Prefix = "GUARDS_ONLINE:";

        public static void Log(string message) => Debug.Log(Prefix + " " + message);

        public static void Fail(string reason) => Debug.LogError(Prefix + " FAIL " + reason);

        public static string Describe(PrivateSessionIdentity identity) =>
            "protocol=" + identity.ProtocolFamily + "/" +
            identity.ProtocolVersion.ToString(CultureInfo.InvariantCulture) +
            " schema=" + identity.SnapshotSchemaVersion.ToString(CultureInfo.InvariantCulture) +
            " map=" + identity.MapId + "@" +
            identity.MapContentRevision.ToString(CultureInfo.InvariantCulture) +
            " content=" + identity.ContentDigest +
            " build=" + identity.BuildIdentity;

        /// <summary>
        /// Formats the host-only event inventory once for both the direct smoke host and the real
        /// lobby host. The record deliberately contains no payload, remote endpoint, invitation,
        /// reconnect capability, or rejection detail.
        /// </summary>
        public static void LogHostEvent(
            PrivateSessionEventRecord record, PrivateSessionHost session = null)
        {
            var ordinal = record.ConnectionOrdinal.ToString(CultureInfo.InvariantCulture);
            switch (record.Kind)
            {
                case PrivateSessionEventKind.ConnectionOpened:
                    Log("CONNECTION_OPENED ordinal=" + ordinal);
                    return;
                case PrivateSessionEventKind.Admitted:
                    Log("ADMITTED ordinal=" + ordinal);
                    return;
                case PrivateSessionEventKind.SeatClaimed:
                    Log(
                        "SEAT_CLAIMED ordinal=" + ordinal +
                        " seat=" + record.Seat.Value +
                        " hero=" + record.Hero.Value +
                        " team=" + record.TeamSide +
                        " claimed=" + record.ClaimedSeats.ToString(CultureInfo.InvariantCulture) +
                        "/" + record.TotalSeats.ToString(CultureInfo.InvariantCulture));
                    return;
                case PrivateSessionEventKind.SeatReserved:
                    Log("SEAT_RESERVED ordinal=" + ordinal + " seat=" + record.Seat.Value);
                    return;
                // Milestone 8.35k. Beside SEAT_RESERVED because the pair is the point: one seat is
                // being held for its player and the other has been given up, and a host log that
                // called both "reserved" is why the failed playtest could not be read afterwards.
                // The capability has no parameter to arrive through here, exactly as before.
                case PrivateSessionEventKind.SeatReleased:
                    Log("SEAT_RELEASED ordinal=" + ordinal + " seat=" + record.Seat.Value);
                    return;
                case PrivateSessionEventKind.SeatReclaimed:
                    Log(
                        "SEAT_RECLAIMED ordinal=" + ordinal + " seat=" + record.Seat.Value +
                        (session == null
                            ? string.Empty
                            : " epoch=" + session.EpochForSeat(record.Seat)
                                .ToString(CultureInfo.InvariantCulture)));
                    return;
                case PrivateSessionEventKind.Rejected:
                    Log("REJECTED ordinal=" + ordinal + " reason=" + record.Reason);
                    return;
                case PrivateSessionEventKind.Disconnected:
                    Log("CONNECTION_CLOSED ordinal=" + ordinal);
                    return;
                case PrivateSessionEventKind.SessionComplete:
                    Log(
                        "SEATS_COMPLETE claimed=" +
                        record.ClaimedSeats.ToString(CultureInfo.InvariantCulture) + "/" +
                        record.TotalSeats.ToString(CultureInfo.InvariantCulture));
                    return;
                case PrivateSessionEventKind.SubmissionAccepted:
                    Log(
                        "SUBMISSION_ACCEPTED ordinal=" + ordinal + " seat=" + record.Seat.Value +
                        " submission=" + SafeDiagnosticToken(record.SubmissionId) + " version=" +
                        record.Version.ToString(CultureInfo.InvariantCulture));
                    return;
                case PrivateSessionEventKind.SubmissionRefused:
                    Log(
                        "SUBMISSION_REFUSED ordinal=" + ordinal + " seat=" + record.Seat.Value +
                        " submission=" + SafeDiagnosticToken(record.SubmissionId) + " reason=" +
                        record.SubmissionRejection + " version=" +
                        record.Version.ToString(CultureInfo.InvariantCulture));
                    return;
                case PrivateSessionEventKind.AgreementAccepted:
                    Log(
                        "AGREEMENT_ACCEPTED ordinal=" + ordinal + " seat=" + record.Seat.Value +
                        " submission=" + SafeDiagnosticToken(record.SubmissionId) + " version=" +
                        record.Version.ToString(CultureInfo.InvariantCulture));
                    return;
                case PrivateSessionEventKind.AgreementRefused:
                    Log(
                        "AGREEMENT_REFUSED ordinal=" + ordinal + " seat=" + record.Seat.Value +
                        " submission=" + SafeDiagnosticToken(record.SubmissionId) + " reason=" +
                        record.AgreementRejection + " version=" +
                        record.Version.ToString(CultureInfo.InvariantCulture));
                    return;
                case PrivateSessionEventKind.ResyncServed:
                    Log(
                        "RESYNC_SERVED ordinal=" + ordinal + " seat=" + record.Seat.Value +
                        " version=" + record.Version.ToString(CultureInfo.InvariantCulture));
                    return;
                case PrivateSessionEventKind.SeatReadinessChanged:
                    Log("READINESS_CHANGED ordinal=" + ordinal + " seat=" + record.Seat.Value);
                    return;
                case PrivateSessionEventKind.MatchStarted:
                    Log("MATCH_STARTED");
                    return;
            }
        }

        private static string SafeDiagnosticToken(string value)
        {
            value = value ?? string.Empty;
            var length = Math.Min(value.Length, NetworkMessageCodec.MaxSubmissionIdChars);
            var safe = new char[length];
            for (var index = 0; index < length; index++)
            {
                var character = value[index];
                safe[index] = char.IsLetterOrDigit(character) || character == '-' ||
                    character == '_' || character == '.'
                    ? character
                    : '_';
            }
            return new string(safe);
        }
    }

    /// <summary>
    /// Milestone 8.31. The dedicated seatless host process: listens on the configured endpoint,
    /// pumps the transport into the session state machine on the main thread, writes one marker
    /// per session event, and exits on its own.
    /// </summary>
    /// <remarks>
    /// Attached by <see cref="MatchSceneCompositionRoot"/> only when the player was launched with
    /// the host flag, exactly as the smoke driver is - a normally launched build never constructs
    /// it. The composed local match supplies two things: the authoritative six-seat binding, and
    /// the admission tuple. No command and no projection crosses the wire here; that is milestones
    /// 8.32-8.33.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PrivateHostDriver : MonoBehaviour
    {
        /// <summary>How long the completed session lingers for stragglers before an orderly stop.</summary>
        private const double LingerSeconds = 5.0;

        private MatchSceneCompositionRoot compositionRoot;
        private PrivateSessionLaunchOptions options;
        private TcpSessionListener listener;
        private PrivateSessionHost session;
        private DateTime startedUtc;
        private DateTime lastActivityUtc;
        private bool stopped;

        public void Configure(MatchSceneCompositionRoot root, PrivateSessionLaunchOptions launchOptions)
        {
            compositionRoot = root;
            options = launchOptions;
            // Milestone 8.31a. A host that pauses on focus loss stops pumping every connection it
            // holds, so activating the host role opts this one process out of the checked-in
            // pause-on-focus-loss default; a normal launch never reaches this line.
            UnityEngine.Application.runInBackground = true;
        }

        private void Start()
        {
            PrivateOnlineMarkers.Log("HOST_BEGIN");
            if (compositionRoot?.Runtime == null || options == null || !options.IsValid)
            {
                PrivateOnlineMarkers.Fail("the host driver was attached without a runtime or valid options");
                Quit(2);
                return;
            }

            try
            {
                listener = new TcpSessionListener();
                listener.Start(options.Endpoint);
            }
            catch (Exception exception)
            {
                PrivateOnlineMarkers.Fail("the listener could not bind: " + exception.GetType().Name);
                Quit(1);
                return;
            }

            var identity = compositionRoot.Runtime.SessionIdentity;
            session = new PrivateSessionHost(
                compositionRoot.Runtime.Host, identity, options.SessionKey, listener,
                new CryptoNonceSource());
            startedUtc = DateTime.UtcNow;
            lastActivityUtc = startedUtc;
            PrivateOnlineMarkers.Log(
                "HOST_LISTENING port=" + listener.BoundPort.ToString(CultureInfo.InvariantCulture) +
                " keyed=" + (options.SessionKey.Length > 0 ? "true" : "false") +
                " " + PrivateOnlineMarkers.Describe(identity));
        }

        private void Update()
        {
            if (stopped || session == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            while (listener.TryDequeue(out var transportEvent))
            {
                switch (transportEvent.Kind)
                {
                    case TransportEventKind.Connected:
                        session.OnConnected(transportEvent.Ordinal, now.Ticks);
                        break;
                    case TransportEventKind.Frame:
                        session.OnFrame(transportEvent.Ordinal, transportEvent.Frame);
                        break;
                    case TransportEventKind.StreamRejected:
                        session.OnStreamRejected(transportEvent.Ordinal, transportEvent.Reason);
                        break;
                    case TransportEventKind.Disconnected:
                        session.OnDisconnected(transportEvent.Ordinal);
                        break;
                }
            }

            session.Sweep(now.Ticks);
            var events = session.DrainEvents();
            foreach (var record in events)
            {
                lastActivityUtc = now;
                PrivateOnlineMarkers.LogHostEvent(record, session);
            }

            if (session.IsComplete && (now - lastActivityUtc).TotalSeconds >= LingerSeconds)
            {
                session.Shutdown();
                PrivateOnlineMarkers.Log("HOST_STOPPED");
                Quit(0);
                return;
            }

            if ((now - startedUtc).TotalSeconds >= options.TimeoutSeconds)
            {
                PrivateOnlineMarkers.Fail(
                    "the session did not complete inside " +
                    options.TimeoutSeconds.ToString(CultureInfo.InvariantCulture) +
                    " seconds; claimed=" +
                    session.ClaimedSeatCount.ToString(CultureInfo.InvariantCulture) + "/" +
                    session.TotalSeats.ToString(CultureInfo.InvariantCulture));
                Quit(1);
            }
        }

        private void Quit(int exitCode)
        {
            stopped = true;
            PrivateOnlineMarkers.Log("EXIT " + exitCode.ToString(CultureInfo.InvariantCulture));
            listener?.Dispose();
            UnityEngine.Application.Quit(exitCode);
        }

        private void OnDestroy() => listener?.Dispose();
    }

    /// <summary>
    /// Milestone 8.31. One client process: dials the configured host, answers the challenge with
    /// the admission tuple and key proof, claims its one configured seat, and holds it until the
    /// host says Goodbye.
    /// </summary>
    /// <remarks>
    /// Exit codes are the rehearsal's contract: 0 for a seat held to an orderly end, 3 for a
    /// refusal the host named (the expected outcome for the duplicate and uninvited rehearsal
    /// clients), 1 for a fault or timeout, 2 for a malformed launch.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PrivateClientDriver : MonoBehaviour
    {
        public const int SeatedExitCode = 0;
        public const int FaultExitCode = 1;
        public const int OptionsExitCode = 2;
        public const int RejectedExitCode = 3;

        private MatchSceneCompositionRoot compositionRoot;
        private PrivateSessionLaunchOptions options;
        private TcpSessionClientTransport transport;
        private PrivateSessionClient machine;
        private PrivateSessionClientState reportedState;
        private long reportedProjectionVersion = -1;
        private DateTime startedUtc;
        private bool stopped;

        public void Configure(MatchSceneCompositionRoot root, PrivateSessionLaunchOptions launchOptions)
        {
            compositionRoot = root;
            options = launchOptions;
            // Milestone 8.31a. An unfocused client that stops pumping never answers the challenge
            // and never claims its seat; activating the client role opts this one process out of
            // the pause-on-focus-loss default, exactly as the host does.
            UnityEngine.Application.runInBackground = true;
        }

        private void Start()
        {
            if (compositionRoot?.Runtime == null || options == null || !options.IsValid)
            {
                PrivateOnlineMarkers.Fail("the client driver was attached without a runtime or valid options");
                Quit(OptionsExitCode);
                return;
            }

            PrivateOnlineMarkers.Log("CLIENT_BEGIN seat=" + options.Seat.Value);
            var identity = compositionRoot.Runtime.SessionIdentity;
            PrivateOnlineMarkers.Log(
                "CLIENT_PROTOCOL version=" +
                identity.ProtocolVersion.ToString(CultureInfo.InvariantCulture));
            transport = new TcpSessionClientTransport();
            machine = new PrivateSessionClient(identity, options.Seat, options.SessionKey, transport);
            startedUtc = DateTime.UtcNow;

            var connectBudget = Math.Min(10, options.TimeoutSeconds);
            if (!transport.Connect(options.Endpoint, connectBudget, out var error))
            {
                PrivateOnlineMarkers.Fail("the host was unreachable: " + error);
                Quit(FaultExitCode);
            }
        }

        private void Update()
        {
            if (stopped || machine == null)
            {
                return;
            }

            while (transport.TryDequeue(out var transportEvent))
            {
                switch (transportEvent.Kind)
                {
                    case TransportEventKind.Connected:
                        machine.OnConnected();
                        PrivateOnlineMarkers.Log("CLIENT_CONNECTED");
                        break;
                    case TransportEventKind.Frame:
                        machine.OnFrame(transportEvent.Frame);
                        break;
                    case TransportEventKind.StreamRejected:
                        machine.OnStreamRejected(transportEvent.Reason);
                        break;
                    case TransportEventKind.Disconnected:
                        machine.OnDisconnected();
                        break;
                }
            }

            ReportTransitions();
            if (stopped)
            {
                return;
            }

            // One marker per received projection version, containing only the own seat and version
            // allowed by the client-diagnostic contract. Card/command counts looked harmless but
            // were still an undeclared side channel and are deliberately absent.
            if (machine.LatestProjection != null &&
                machine.LatestProjection.Version != reportedProjectionVersion)
            {
                reportedProjectionVersion = machine.LatestProjection.Version;
                PrivateOnlineMarkers.Log(
                    "CLIENT_PROJECTION version=" +
                    reportedProjectionVersion.ToString(CultureInfo.InvariantCulture) +
                    " seat=" + machine.LatestProjection.SeatId);
            }

            if ((DateTime.UtcNow - startedUtc).TotalSeconds >= options.TimeoutSeconds)
            {
                if (machine.State == PrivateSessionClientState.Seated)
                {
                    PrivateOnlineMarkers.Log("CLIENT_BUDGET_ELAPSED");
                    machine.SendGoodbye();
                    Quit(SeatedExitCode);
                    return;
                }

                PrivateOnlineMarkers.Fail("the client did not reach a terminal state inside " +
                    options.TimeoutSeconds.ToString(CultureInfo.InvariantCulture) + " seconds; state=" +
                    machine.State);
                Quit(FaultExitCode);
            }
        }

        private void ReportTransitions()
        {
            if (machine.State == reportedState)
            {
                return;
            }

            reportedState = machine.State;
            switch (machine.State)
            {
                case PrivateSessionClientState.AwaitingSeat:
                    PrivateOnlineMarkers.Log("CLIENT_ADMITTED");
                    return;

                case PrivateSessionClientState.Seated:
                    PrivateOnlineMarkers.Log(
                        "CLIENT_SEATED seat=" + machine.GrantedSeat.Seat.Value);
                    return;

                case PrivateSessionClientState.Rejected:
                    PrivateOnlineMarkers.Log(
                        "CLIENT_REJECTED reason=" + machine.RejectionReason);
                    Quit(RejectedExitCode);
                    return;

                case PrivateSessionClientState.Disconnected:
                    PrivateOnlineMarkers.Log("CLIENT_CLOSED");
                    Quit(machine.GrantedSeat != null ? SeatedExitCode : FaultExitCode);
                    return;

                // Milestone 8.34. The host-loss row: this seat's authority ended without saying
                // so, which is terminal for the session and for anything this process could still
                // do in it. The seat was held, so the exit code stays the seated one - the
                // rehearsal reads a lost host from the marker, not from a failure.
                case PrivateSessionClientState.HostLost:
                    PrivateOnlineMarkers.Log(
                        "CLIENT_HOST_LOST seat=" + machine.GrantedSeat.Seat.Value);
                    Quit(SeatedExitCode);
                    return;

                case PrivateSessionClientState.Faulted:
                    PrivateOnlineMarkers.Fail("the host's stream violated the protocol: " +
                        machine.RejectionReason);
                    Quit(FaultExitCode);
                    return;
            }
        }

        private void Quit(int exitCode)
        {
            stopped = true;
            PrivateOnlineMarkers.Log("EXIT " + exitCode.ToString(CultureInfo.InvariantCulture));
            transport?.Dispose();
            UnityEngine.Application.Quit(exitCode);
        }

        private void OnDestroy() => transport?.Dispose();
    }
}
