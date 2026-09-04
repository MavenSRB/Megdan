using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    internal enum PrivateOnlineRehearsalRole
    {
        None = 0,
        Host = 1,
        Client = 2
    }

    internal enum PrivateOnlineRehearsalMode
    {
        Match = 0,
        HostLoss = 1,

        /// <summary>
        /// Milestone 8.35k. The cohort whose cut kills a PROCESS rather than a socket. Everything
        /// the match cohort proves about reconnect was proved with the client object still alive -
        /// <c>InterruptConnectionForRehearsal</c> replaces the transport under a machine that still
        /// holds its capability - and that is exactly the one path a real player never walks. Here
        /// the designated seat's process exits and the runner launches a new one.
        /// </summary>
        Rejoin = 2,

        /// <summary>
        /// Milestone 8.35k. The relaunched process, which is a first join as far as it knows: a new
        /// process, a new client object, and nothing carried across but what it finds on disk.
        /// </summary>
        RejoinReturn = 3
    }

    /// <summary>
    /// The additional command-line data accepted only beside <c>-guardsPrivateLobby</c> for the
    /// disposable local rehearsal. A normal lobby still receives its invitation from the player;
    /// the explicit role flags opt into automation while endpoint and key arrive through inherited
    /// process environment so Unity's command-line banner cannot disclose an invitation secret.
    /// </summary>
    internal sealed class PrivateOnlineRehearsalOptions
    {
        public PrivateOnlineRehearsalRole Role { get; private set; }
        public PrivateOnlineRehearsalMode Mode { get; private set; }
        public string Address { get; private set; }
        public int Port { get; private set; }
        public string SessionKey { get; private set; }
        public SeatId Seat { get; private set; }
        public int Seed { get; private set; }
        public int TimeoutSeconds { get; private set; }

        public static bool TryParse(string[] args, out PrivateOnlineRehearsalOptions result, out string error)
        {
            result = null;
            error = string.Empty;
            args = args ?? Array.Empty<string>();
            var requested = Array.IndexOf(args, PrivateOnlineRehearsalDriver.RoleFlag) >= 0;
            if (!requested)
            {
                return true;
            }

            string role = null;
            string mode = "match";
            var address = Environment.GetEnvironmentVariable(
                PrivateOnlineRehearsalDriver.AddressEnvironmentVariable);
            var port = Environment.GetEnvironmentVariable(
                PrivateOnlineRehearsalDriver.PortEnvironmentVariable);
            var key = Environment.GetEnvironmentVariable(
                PrivateOnlineRehearsalDriver.KeyEnvironmentVariable);
            string seat = null;
            string seed = "21";
            string timeout = GuardsDigital.Network.PrivateSessionLaunchOptions.DefaultTimeoutSeconds
                .ToString(CultureInfo.InvariantCulture);
            for (var index = 0; index < args.Length; index++)
            {
                var argument = args[index] ?? string.Empty;
                if (argument == PrivateOnlineRehearsalDriver.RoleFlag)
                    role = Take(args, ref index);
                else if (argument == PrivateOnlineRehearsalDriver.ModeFlag)
                    mode = Take(args, ref index);
                else if (argument == PrivateOnlineRehearsalDriver.SeatFlag)
                    seat = Take(args, ref index);
                else if (argument == PrivateOnlineRehearsalDriver.SeedFlag)
                    seed = Take(args, ref index);
                else if (argument == GuardsDigital.Network.PrivateSessionLaunchOptions.TimeoutFlag)
                    timeout = Take(args, ref index);
            }

            if (Array.IndexOf(args, GuardsDigital.Network.PrivateSessionLaunchOptions.LobbyFlag) < 0)
            {
                error = "the rehearsal role requires the real private-lobby launch flag";
                return false;
            }

            PrivateOnlineRehearsalRole parsedRole;
            if (string.Equals(role, "host", StringComparison.OrdinalIgnoreCase))
                parsedRole = PrivateOnlineRehearsalRole.Host;
            else if (string.Equals(role, "client", StringComparison.OrdinalIgnoreCase))
                parsedRole = PrivateOnlineRehearsalRole.Client;
            else
            {
                error = "the rehearsal role must be host or client";
                return false;
            }

            PrivateOnlineRehearsalMode parsedMode;
            if (string.Equals(mode, "match", StringComparison.OrdinalIgnoreCase))
                parsedMode = PrivateOnlineRehearsalMode.Match;
            else if (string.Equals(mode, "host-loss", StringComparison.OrdinalIgnoreCase))
                parsedMode = PrivateOnlineRehearsalMode.HostLoss;
            else if (string.Equals(mode, "rejoin", StringComparison.OrdinalIgnoreCase))
                parsedMode = PrivateOnlineRehearsalMode.Rejoin;
            else if (string.Equals(mode, "rejoin-return", StringComparison.OrdinalIgnoreCase))
                parsedMode = PrivateOnlineRehearsalMode.RejoinReturn;
            else
            {
                error = "the rehearsal mode must be match, host-loss, rejoin or rejoin-return";
                return false;
            }

            if (!int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPort) ||
                parsedPort < 1 || parsedPort > 65535)
            {
                error = "the rehearsal port must be between 1 and 65535";
                return false;
            }

            // The adapter's own endpoint type answers "is this a numeric loopback address". Naming
            // the socket namespace here instead would put a transport type in Presentation, which
            // is the one thing the amended record's removal path forbids above the adapter.
            if (!GuardsDigital.Network.PrivateSessionEndpoint.TryCreate(
                    address, parsedPort, out var parsedEndpoint, out _) ||
                !parsedEndpoint.IsLoopback)
            {
                error = "the automated rehearsal is confined to a numeric loopback address";
                return false;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                error = "the automated rehearsal requires a non-empty session key";
                return false;
            }

            if (!int.TryParse(seed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSeed) ||
                parsedSeed == 0)
            {
                error = "the rehearsal seed must be a non-zero whole number";
                return false;
            }

            if (!int.TryParse(timeout, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var parsedTimeout) || parsedTimeout < 1)
            {
                error = "the rehearsal timeout must be a positive whole number of seconds";
                return false;
            }

            var parsedSeat = string.IsNullOrWhiteSpace(seat) ? default : new SeatId(seat);
            if (parsedRole == PrivateOnlineRehearsalRole.Client && !parsedSeat.IsDefined)
            {
                error = "a rehearsal client requires one seat";
                return false;
            }

            if (parsedRole == PrivateOnlineRehearsalRole.Host && parsedSeat.IsDefined)
            {
                error = "the dedicated rehearsal host is seatless";
                return false;
            }

            result = new PrivateOnlineRehearsalOptions
            {
                Role = parsedRole,
                Mode = parsedMode,
                Address = address,
                Port = parsedPort,
                SessionKey = key,
                Seat = parsedSeat,
                Seed = parsedSeed,
                TimeoutSeconds = parsedTimeout
            };
            return true;
        }

        private static string Take(string[] args, ref int index)
        {
            if (index + 1 >= args.Length)
                return null;
            index++;
            return args[index];
        }
    }

    /// <summary>
    /// Milestone 8.35c's CLI-only player-build journey. It enters through the real primitive lobby,
    /// presses its Host/Join/Ready/Reconnect controls, waits for the retained HUD handoff, and then
    /// chooses only commands or agreement choices that the owner-scoped HUD currently projects.
    /// It contains no match authority and is never attached by a normal player launch.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrivateOnlineRehearsalDriver : MonoBehaviour
    {
        public const string RoleFlag = "-guardsRehearsalRole";
        public const string ModeFlag = "-guardsRehearsalMode";
        public const string SeatFlag = "-guardsRehearsalSeat";
        public const string SeedFlag = "-guardsRehearsalSeed";
        public const string AddressEnvironmentVariable = "GUARDS_REHEARSAL_ADDRESS";
        public const string PortEnvironmentVariable = "GUARDS_REHEARSAL_PORT";
        public const string KeyEnvironmentVariable = "GUARDS_REHEARSAL_SESSION_KEY";

        private const string ReconnectSeat = OfficialSixPlayerParticipants.RedSecondPlayerIdValue;
        private const double ActionIntervalSeconds = 0.02;
        private const double PublicObservationSeconds = 2.0;
        private const double OutcomeLingerSeconds = 3.0;

        private MatchSceneCompositionRoot compositionRoot;
        private PrivateLobbyDriver lobby;
        private PrivateOnlineRehearsalOptions options;
        private DateTime startedUtc;
        private DateTime nextActionUtc;
        private DateTime? outcomeSeenUtc;
        private bool entryPressed;
        private bool readinessPressed;
        private bool hudReported;
        private bool stopped;
        private bool privacyFailed;
        private long auditedRevision = -1;
        private long reportedProgressVersion = -1;

        private string initiativeProposal = string.Empty;
        private CardInstanceId initiativeTarget;
        private DateTime? initiativeDisagreementSeenUtc;
        private bool initiativeDisagreementReported;
        private bool initiativeFinalObserved;
        private bool tieBehaviorObserved;
        private TeamSide tieSideAtSelection;
        private CardInstanceId tiedActor;
        private bool tiedActorFlips;
        private long tiedActorVersion;

        private string minionInitialProposal = string.Empty;
        private string minionChangedProposal = string.Empty;
        private IReadOnlyList<RuleObject> initialMinionSelection = Array.Empty<RuleObject>();
        private DateTime? minionPartialSeenUtc;
        private bool minionPartialReported;
        private bool minionChangeSent;
        private bool minionReplacementSent;
        private bool minionInvalidationObserved;
        private bool minionFinalObserved;

        private bool reconnectInterrupted;
        private DateTime reconnectInterruptedUtc;
        private bool reconnectPressed;
        private bool reconnectCompleted;
        private PrivateSessionClient reconnectMachine;
        private int reconnectEpoch;
        private long reconnectRevision;
        private long reconnectVersion;

        internal static bool TryParseRequested(
            string[] args, out PrivateOnlineRehearsalOptions parsed, out string error)
        {
            var requested = args != null && Array.IndexOf(args, RoleFlag) >= 0;
            if (!requested)
            {
                parsed = null;
                error = string.Empty;
                return false;
            }

            if (!PrivateOnlineRehearsalOptions.TryParse(args, out parsed, out error))
            {
                parsed = null;
            }

            return true;
        }

        internal void Configure(
            MatchSceneCompositionRoot root,
            PrivateLobbyDriver lobbyDriver,
            PrivateOnlineRehearsalOptions rehearsalOptions)
        {
            compositionRoot = root;
            lobby = lobbyDriver;
            options = rehearsalOptions;
            UnityEngine.Application.runInBackground = true;

            // Milestone 8.35k. A relaunched process starts with the scripted choreography already
            // BEHIND it, because it is: the forced Initiative disagreement, its revision, and the
            // changed Minion Battle proposal all happened in the rounds before the cut, and this
            // process cannot witness what it was not there for.
            //
            // Marking them observed rather than merely skipping the assertions is the whole point,
            // and the first attempt at skipping them DEADLOCKED THE TABLE. `DriveInitiativeAgreement`
            // branches on `initiativeFinalObserved`: true means "answer ordinarily", false means
            // "force the scripted disagreement". A returned process with the flag still false went
            // on trying to force a disagreement in a later round while the other five answered
            // ordinarily, and the agreement could never complete - six processes waiting on each
            // other at version 347. So the flags are not an assertion to dodge; they are this
            // driver's phase, and a process that joins mid-match is in the later phase.
            if (options != null && options.Mode == PrivateOnlineRehearsalMode.RejoinReturn)
            {
                initiativeFinalObserved = true;
                tieBehaviorObserved = true;
                minionInvalidationObserved = true;
                minionFinalObserved = true;
            }
        }

        private void Start()
        {
            startedUtc = DateTime.UtcNow;
            if (lobby != null)
                lobby.HostEventObserved += OnHostEvent;
        }

        private void Update()
        {
            if (stopped)
                return;
            if (compositionRoot?.Runtime == null || lobby == null || options == null)
            {
                Fail("rehearsal-driver-configuration");
                return;
            }

            if ((DateTime.UtcNow - startedUtc).TotalSeconds >= options.TimeoutSeconds)
            {
                Fail("rehearsal-timeout");
                return;
            }

            if (!entryPressed)
            {
                PressEntryWhenReady();
                return;
            }

            if (options.Role == PrivateOnlineRehearsalRole.Host)
            {
                UpdateHost();
                return;
            }

            UpdateClient();
        }

        private void PressEntryWhenReady()
        {
            if (lobby.Presenter?.Model == null)
                return;

            var entry = new PrivateLobbyEntry(
                options.Address,
                options.Port.ToString(CultureInfo.InvariantCulture),
                options.SessionKey,
                options.Seat);
            var control = options.Role == PrivateOnlineRehearsalRole.Host
                ? PrivateLobbyControl.Host
                : PrivateLobbyControl.Join;
            if (!lobby.Presenter.Press(control, entry))
            {
                Fail("lobby-entry-control-not-offered");
                return;
            }

            entryPressed = true;
            if (options.Role == PrivateOnlineRehearsalRole.Host)
            {
                PrivateOnlineMarkers.Log(
                    "REHEARSAL_HOST seed=" + options.Seed.ToString(CultureInfo.InvariantCulture) +
                    " mode=" + ModeLabel(options.Mode) + " " +
                    PrivateOnlineMarkers.Describe(compositionRoot.Runtime.SessionIdentity));
            }
            else
            {
                PrivateOnlineMarkers.Log(
                    "CLIENT_PROTOCOL version=" +
                    compositionRoot.Runtime.SessionIdentity.ProtocolVersion
                        .ToString(CultureInfo.InvariantCulture));
                ClientMarker("begin", -1);
            }
        }

        private void UpdateHost()
        {
            var host = lobby.RehearsalHost;
            if (host == null)
                return;
            if (!host.HasMatchStarted)
                return;

            var projection = compositionRoot.Runtime.Host.Projection;
            if (reportedProgressVersion < 0 || projection.Version >= reportedProgressVersion + 100)
            {
                reportedProgressVersion = projection.Version;
                PrivateOnlineMarkers.Log(
                    "REHEARSAL_HOST_PROGRESS version=" +
                    projection.Version.ToString(CultureInfo.InvariantCulture));
            }

            if (options.Mode == PrivateOnlineRehearsalMode.HostLoss)
            {
                if (!hudReported)
                {
                    hudReported = true;
                    PrivateOnlineMarkers.Log("REHEARSAL_HOSTLOSS_READY version=" +
                        projection.Version.ToString(CultureInfo.InvariantCulture));
                }
                return;
            }

            if (!projection.Outcome.HasEnded)
                return;
            if (!outcomeSeenUtc.HasValue)
            {
                outcomeSeenUtc = DateTime.UtcNow;
                PrivateOnlineMarkers.Log(
                    "REHEARSAL_OUTCOME version=" +
                    projection.Version.ToString(CultureInfo.InvariantCulture) +
                    " cause=" + projection.Outcome.Cause +
                    " winner=" + projection.Outcome.WinningTeamId.Value);
                return;
            }

            if ((DateTime.UtcNow - outcomeSeenUtc.Value).TotalSeconds < OutcomeLingerSeconds)
                return;
            lobby.Presenter.Press(PrivateLobbyControl.Leave, null);
            PrivateOnlineMarkers.Log("REHEARSAL_HOST_COMPLETE");
            Quit(0);
        }

        private void UpdateClient()
        {
            var machine = lobby.RehearsalClient;
            if (machine == null)
                return;

            if (machine.State == PrivateSessionClientState.Rejected ||
                machine.State == PrivateSessionClientState.Faulted)
            {
                Fail("client-terminal-" + machine.State.ToString().ToLowerInvariant());
                return;
            }

            if (options.Mode == PrivateOnlineRehearsalMode.HostLoss &&
                machine.State == PrivateSessionClientState.HostLost)
            {
                if (compositionRoot.Runtime.ReadModel.IsInputEnabled)
                {
                    Fail("host-loss-left-input-enabled");
                    return;
                }
                ClientMarker("host-loss-pass", machine.LatestProjection?.Version ?? -1);
                Quit(0);
                return;
            }

            if (machine.State == PrivateSessionClientState.Seated && !machine.HasMatchStarted)
            {
                if (!readinessPressed &&
                    lobby.Presenter.Model?.Offers(PrivateLobbyControl.Ready) == true)
                    readinessPressed = lobby.Presenter.Press(PrivateLobbyControl.Ready, null);
                return;
            }

            if (!lobby.RehearsalHudAttached || machine.LatestProjection == null)
                return;
            if (!hudReported)
            {
                hudReported = true;
                ClientMarker("hud", machine.LatestProjection.Version);
                if (options.Mode == PrivateOnlineRehearsalMode.HostLoss)
                    ClientMarker("host-loss-ready", machine.LatestProjection.Version);
                // Milestone 8.35k. The relaunched process is back in the seat it left, holding an
                // owner-scoped HUD, having carried nothing across a process boundary but what it
                // found on disk. Reaching this line at all is what the cohort exists to observe.
                if (options.Mode == PrivateOnlineRehearsalMode.RejoinReturn &&
                    string.Equals(options.Seat.Value, ReconnectSeat, StringComparison.Ordinal))
                    ClientMarker("rejoin-return-pass", machine.LatestProjection.Version);
            }

            AuditProjection(machine);
            if (privacyFailed)
                return;
            ObserveCompletedAgreements();

            if (options.Mode == PrivateOnlineRehearsalMode.HostLoss)
                return;

            // Milestone 8.35k. Two cohorts cut the same seat and they must not both fire. The match
            // cohort replaces a socket under a surviving client; this one ends the process. The
            // relaunched process cuts nothing at all - it is the return.
            if (options.Mode == PrivateOnlineRehearsalMode.Rejoin)
            {
                if (HandleRejoinCut(machine))
                    return;
            }
            else if (options.Mode != PrivateOnlineRehearsalMode.RejoinReturn &&
                HandleReconnect(machine))
            {
                return;
            }

            var outcome = compositionRoot.Runtime.ReadModel.Outcome;
            if (outcome.HasEnded)
            {
                if (!outcomeSeenUtc.HasValue)
                {
                    // Milestone 8.35k. The in-process reconnect is the MATCH cohort's required
                    // observation and only its own. The rejoin cohorts cut the same seat a
                    // different way - the process dies and a new one returns - so requiring
                    // `reconnectCompleted` of them would fail a run that did exactly what it was
                    // asked to. Their own required observation is the marker pair the runner waits
                    // on across two processes, which no single process can assert about itself.
                    var owesInProcessReconnect =
                        options.Mode == PrivateOnlineRehearsalMode.Match &&
                        string.Equals(options.Seat.Value, ReconnectSeat, StringComparison.Ordinal) &&
                        !reconnectCompleted;
                    if (!initiativeFinalObserved || !tieBehaviorObserved ||
                        !minionInvalidationObserved || !minionFinalObserved ||
                        owesInProcessReconnect)
                    {
                        Fail("match-ended-before-required-rehearsal-observations");
                        return;
                    }
                    outcomeSeenUtc = DateTime.UtcNow;
                    ClientMarker("privacy-pass", machine.LatestProjection.Version);
                    PrivateOnlineMarkers.Log(
                        "REHEARSAL_OUTCOME seat=" + options.Seat.Value + " version=" +
                        machine.LatestProjection.Version.ToString(CultureInfo.InvariantCulture) +
                        " cause=" + outcome.Cause + " winner=" + outcome.WinningTeamId.Value);
                }
                if (machine.State == PrivateSessionClientState.Disconnected ||
                    (DateTime.UtcNow - outcomeSeenUtc.Value).TotalSeconds >=
                        OutcomeLingerSeconds + 2.0)
                    Quit(0);
                return;
            }

            if (machine.State == PrivateSessionClientState.HostLost)
            {
                Fail("unexpected-host-loss");
                return;
            }

            if (machine.State != PrivateSessionClientState.Seated || machine.IsWaitingForHost ||
                DateTime.UtcNow < nextActionUtc)
                return;
            TryDriveMatch(machine);
        }

        /// <summary>
        /// Milestone 8.35k. Ends this PROCESS at the same cut point the match cohort replaces a
        /// socket at, so the two differ in exactly one thing: whether the client object survives.
        /// </summary>
        /// <remarks>
        /// <para>That one difference is the whole defect. Every reconnect fixture at every tier -
        /// this driver's own match cohort included - kept the client object alive and replaced only
        /// its transport, which is the one path a real player never walks. A player relaunches.</para>
        /// <para><b>No Goodbye is sent, deliberately.</b> Leaving forfeits the seat and a crash
        /// reserves it, so a polite departure here would hand the seat back as Open and the cohort
        /// would prove the opposite of what it is for. <c>Application.Quit</c> reaches the lobby's
        /// <c>OnDestroy</c>, which disposes the transport and sends nothing - which is what a killed
        /// process looks like from the host's end.</para>
        /// <para>The cut point matches <see cref="HandleReconnect"/>'s exactly - the Initiative
        /// outcome observed, a version past 20, and not mid-request - so the two cohorts cut
        /// comparable states and a difference in result is about the cut and not about when.</para>
        /// </remarks>
        private bool HandleRejoinCut(PrivateSessionClient machine)
        {
            if (!string.Equals(options.Seat.Value, ReconnectSeat, StringComparison.Ordinal))
                return false;
            if (!initiativeFinalObserved || machine.LatestProjection.Version < 20 ||
                machine.IsWaitingForHost)
                return false;

            PrivateOnlineMarkers.Log(
                "REHEARSAL_CLIENT event=rejoin-process-exiting seat=" + options.Seat.Value +
                " version=" +
                machine.LatestProjection.Version.ToString(CultureInfo.InvariantCulture));
            Quit(0);
            return true;
        }

        private bool HandleReconnect(PrivateSessionClient machine)
        {
            if (!string.Equals(options.Seat.Value, ReconnectSeat, StringComparison.Ordinal) ||
                reconnectCompleted)
                return false;

            if (!reconnectInterrupted)
            {
                if (!initiativeFinalObserved || machine.LatestProjection.Version < 20 ||
                    machine.IsWaitingForHost)
                    return false;
                reconnectInterrupted = true;
                reconnectInterruptedUtc = DateTime.UtcNow;
                reconnectMachine = machine;
                reconnectEpoch = machine.ConnectionEpoch;
                reconnectRevision = machine.ProjectionRevision;
                reconnectVersion = machine.LatestProjection.Version;
                if (!lobby.InterruptConnectionForRehearsal())
                {
                    Fail("reconnect-interrupt-refused");
                    return true;
                }
                if (compositionRoot.Runtime.ReadModel.IsInputEnabled)
                {
                    Fail("reconnect-left-input-enabled");
                    return true;
                }
                ClientMarker("reconnect-input-cleared", reconnectVersion);
                return true;
            }

            if (!reconnectPressed)
            {
                if ((DateTime.UtcNow - reconnectInterruptedUtc).TotalSeconds < 1.0)
                    return true;
                if (lobby.Presenter.Model?.Offers(PrivateLobbyControl.Reconnect) != true)
                    return true;
                reconnectPressed = lobby.Presenter.Press(PrivateLobbyControl.Reconnect, null);
                if (!reconnectPressed)
                    Fail("reconnect-control-refused");
                return true;
            }

            machine = lobby.RehearsalClient;
            if (machine == null || machine.State != PrivateSessionClientState.Seated ||
                machine.IsReconnecting || machine.LatestProjection == null)
                return true;
            if (!ReferenceEquals(machine, reconnectMachine) ||
                machine.ConnectionEpoch <= reconnectEpoch ||
                machine.ProjectionRevision <= reconnectRevision ||
                machine.LatestProjection.Version < reconnectVersion ||
                !compositionRoot.Runtime.ReadModel.IsInputEnabled)
            {
                Fail("reconnect-did-not-restore-fresh-owner-projection");
                return true;
            }

            reconnectCompleted = true;
            ClientMarker("reconnect-pass", machine.LatestProjection.Version);
            return false;
        }

        private void TryDriveMatch(PrivateSessionClient machine)
        {
            var runtime = compositionRoot.Runtime;
            var pendingMinionChoice = runtime.ReadModel.PendingMinionBattleChoice;
            if (pendingMinionChoice != null && !pendingMinionChoice.CanConfirm &&
                IsTeamController(pendingMinionChoice.ChoosingSide))
            {
                DriveMinionSelection(machine);
                return;
            }

            var agreement = runtime.ReadModel.TeamAgreement;
            if (agreement != null)
            {
                if (agreement.Kind == TeamAgreementKind.Initiative)
                    DriveInitiativeAgreement(machine, agreement);
                else if (agreement.Kind == TeamAgreementKind.MinionBattle)
                    DriveMinionAgreement(machine, agreement);
                return;
            }

            if (pendingMinionChoice != null)
            {
                DriveMinionSelection(machine);
                return;
            }

            var decisionOwner = machine.LatestProjection.DecisionOwner;
            if (decisionOwner != null && decisionOwner.TeamSide != (int)TeamSide.None &&
                !IsTeamController((TeamSide)decisionOwner.TeamSide))
                return;

            var commands = AuthorizedProjectedCommands(machine, runtime);
            if (commands.Count == 0)
                return;
            if (runtime.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                var first = runtime.ReadModel.Commitments.FirstOrDefault(value =>
                    value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                if (!first.HeroId.Equals(runtime.Seat.HeroId))
                    return;
            }
            else if (runtime.Phase == RoundResolutionPhase.EndOfRound &&
                runtime.ReadModel.DecisionContract == null &&
                !string.Equals(options.Seat.Value,
                    OfficialSixPlayerParticipants.RedFirstPlayerIdValue, StringComparison.Ordinal))
            {
                // End of round advances on a command that carries no choice and is therefore
                // authorized to every seat, so exactly one seat presses it. A decision standing
                // INSIDE end of round has an owner of its own - the round's system transition is
                // team-owned and can fall to either side - and the single-advancer rule must not
                // reach it, or a Blue-owned transition waits on a Red seat that holds nothing.
                return;
            }

            DirectorCommand selected;
            if (!initiativeFinalObserved && runtime.Phase == RoundResolutionPhase.AwaitingCommitments)
                selected = SelectForcedTieCommitment(runtime, commands);
            else
                selected = commands[SelectIndex(options.Seed, runtime.ReadModel.Version, commands.Count)];
            if (runtime.SubmitProjectedCommand(selected))
                nextActionUtc = DateTime.UtcNow.AddSeconds(ActionIntervalSeconds);
        }

        /// <summary>
        /// The commands the retained HUD is currently drawing that this seat's own host-published
        /// projection also authorizes it to submit.
        /// </summary>
        /// <remarks>
        /// The intersection is the whole point. <see cref="ConfiguredMatchSceneRuntime
        /// .EnumerateProjectedCommands"/> is the Director-era surface affordance and derives some
        /// offers - the general Hold, and a standing decision's own contract - from public state
        /// rather than from the authorized set, so every seat can see the acting seat's Hold and
        /// every seat of a deciding team can see that team's final rules command. A rehearsal that
        /// submitted those would be pressing controls it does not hold: the host refuses each one
        /// by name, which is correct and which milestone 8.30a already proves, but a driver that
        /// leans on that refusal is neither playing the game the way a player does nor leaving a
        /// diagnosable log - the first live run spent roughly seven refusals and a served resync
        /// per accepted command and could not have finished a match inside any sane budget.
        /// </remarks>
        private static IReadOnlyList<DirectorCommand> AuthorizedProjectedCommands(
            PrivateSessionClient machine, ConfiguredMatchSceneRuntime runtime)
        {
            var authorized = machine.LatestProjection?.AuthorizedCommands;
            if (authorized == null || authorized.Count == 0)
                return Array.Empty<DirectorCommand>();
            return runtime.EnumerateProjectedCommands()
                .Where(command => authorized.Contains(command)).ToArray();
        }

        private DirectorCommand SelectForcedTieCommitment(
            ConfiguredMatchSceneRuntime runtime, IReadOnlyList<DirectorCommand> commands)
        {
            var commits = commands.Where(value => value.Kind == DirectorCommandKind.CommitCard).ToArray();
            if (commits.Length == 0)
                return commands[0];
            var ranked = commits.Select(command => new
            {
                Command = command,
                Initiative = InitiativeOf(runtime, command.CardInstanceId)
            }).ToArray();
            var forceHighest = runtime.Seat.HeroId.Value ==
                    OfficialSixPlayerParticipants.WaspHeroIdValue ||
                runtime.Seat.HeroId.Value == OfficialSixPlayerParticipants.TigerclawHeroIdValue;
            return (forceHighest
                    ? ranked.OrderByDescending(value => value.Initiative)
                    : ranked.OrderBy(value => value.Initiative))
                .ThenBy(value => value.Command.CardInstanceId.Value, StringComparer.Ordinal)
                .First().Command;
        }

        private static int InitiativeOf(ConfiguredMatchSceneRuntime runtime, CardInstanceId cardId)
        {
            var card = runtime.Cards.Single(value => value.Id.Equals(cardId));
            return runtime.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).Initiative.Value;
        }

        private void DriveInitiativeAgreement(
            PrivateSessionClient machine, TeamAgreementProjection agreement)
        {
            if (initiativeFinalObserved)
            {
                SubmitOrdinaryInitiativeAgreement(machine, agreement);
                return;
            }

            if (initiativeProposal.Length == 0)
            {
                initiativeProposal = agreement.ProposalId.Value;
                initiativeTarget = agreement.Candidates[
                    SelectIndex(options.Seed, machine.LatestProjection.Version,
                        agreement.Candidates.Count)].CardInstanceId;
            }

            var ownHero = compositionRoot.Runtime.Seat.HeroId;
            var participantIndex = IndexOfParticipant(agreement, ownHero);
            if (participantIndex >= 0 && !machine.IsWaitingForHost)
            {
                var own = agreement.Participants[participantIndex];
                var previousHaveSpoken = agreement.Participants.Take(participantIndex)
                    .All(value => value.HasStatement);
                if (!own.HasStatement && previousHaveSpoken)
                {
                    var preferred = participantIndex == 1
                        ? agreement.Candidates.Select(value => value.CardInstanceId)
                            .First(value => !value.Equals(initiativeTarget))
                        : initiativeTarget;
                    SubmitInitiativePreference(agreement, preferred);
                    return;
                }
            }

            if (!agreement.Participants.All(value => value.HasStatement))
                return;
            var disagreed = agreement.Participants.Select(value => value.PreferredCardInstanceId)
                .Distinct().Count() > 1;
            if (disagreed)
            {
                if (!initiativeDisagreementSeenUtc.HasValue)
                    initiativeDisagreementSeenUtc = DateTime.UtcNow;
                if (!initiativeDisagreementReported)
                {
                    initiativeDisagreementReported = true;
                    ClientMarker("initiative-disagreement", machine.LatestProjection.Version);
                }
            }

            if (!initiativeDisagreementSeenUtc.HasValue ||
                (DateTime.UtcNow - initiativeDisagreementSeenUtc.Value).TotalSeconds <
                    PublicObservationSeconds || participantIndex < 0 || machine.IsWaitingForHost)
                return;
            var participant = agreement.Participants[participantIndex];
            if (!participant.PreferredCardInstanceId.Equals(initiativeTarget))
            {
                if (SubmitInitiativePreference(agreement, initiativeTarget))
                {
                    ClientMarker("initiative-revision", machine.LatestProjection.Version);
                }
            }
        }

        private bool SubmitInitiativePreference(
            TeamAgreementProjection agreement, CardInstanceId preferred)
        {
            var statement = agreement.AuthorizedCommands.FirstOrDefault(value =>
                value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                value.PreferredCardInstanceId.Equals(preferred));
            if (statement == null)
                return false;
            var sent = compositionRoot.Runtime.SubmitProjectedAgreement(statement);
            if (sent)
                nextActionUtc = DateTime.UtcNow.AddSeconds(ActionIntervalSeconds);
            return sent;
        }

        private void SubmitOrdinaryInitiativeAgreement(
            PrivateSessionClient machine, TeamAgreementProjection agreement)
        {
            var ownIndex = IndexOfParticipant(agreement, compositionRoot.Runtime.Seat.HeroId);
            if (ownIndex < 0 || machine.IsWaitingForHost)
                return;
            var own = agreement.Participants[ownIndex];
            if (own.HasStatement ||
                !agreement.Participants.Take(ownIndex).All(value => value.HasStatement))
                return;
            SubmitInitiativePreference(agreement, agreement.Candidates[0].CardInstanceId);
        }

        private void DriveMinionAgreement(
            PrivateSessionClient machine, TeamAgreementProjection agreement)
        {
            if (minionFinalObserved)
            {
                SubmitMinionAgreementsInOrder(machine, agreement);
                return;
            }

            // Only a confirmable selection has a proposal worth tracking. The proposal identity
            // carries the authoritative version, so it moves with every ordinary selection the
            // choosing team makes; a seat that started tracking before the selection was complete
            // would report the first ordinary selection as an invalidation and prove nothing. Every
            // seat therefore starts at the same first confirmable version.
            if (minionInitialProposal.Length == 0)
            {
                if (!agreement.SelectionIsConfirmable)
                    return;
                minionInitialProposal = agreement.ProposalId.Value;
                initialMinionSelection = agreement.SelectedUnits.ToArray();
            }

            if (!string.Equals(agreement.ProposalId.Value, minionInitialProposal,
                    StringComparison.Ordinal))
            {
                if (minionChangedProposal.Length == 0)
                {
                    if (agreement.Participants.Any(value => value.HasStatement))
                    {
                        Fail("changed-minion-proposal-retained-old-agreement");
                        return;
                    }
                    minionChangedProposal = agreement.ProposalId.Value;
                    minionInvalidationObserved = true;
                    ClientMarker("minion-proposal-invalidated", machine.LatestProjection.Version);
                }
                SubmitMinionAgreementsInOrder(machine, agreement);
                return;
            }

            var agreedCount = agreement.Participants.Count(value => value.Agreed);
            if (agreedCount == 0)
            {
                var ownIndex = IndexOfParticipant(agreement, compositionRoot.Runtime.Seat.HeroId);
                if (ownIndex == 0 && !machine.IsWaitingForHost)
                    SubmitAgreementOfKind(
                        agreement, TeamAgreementCommandKind.AgreeToMinionBattleSelection);
                return;
            }

            if (agreedCount == 1)
            {
                if (!minionPartialSeenUtc.HasValue)
                    minionPartialSeenUtc = DateTime.UtcNow;
                if (!minionPartialReported)
                {
                    minionPartialReported = true;
                    ClientMarker("minion-partial", machine.LatestProjection.Version);
                }
            }

            if (minionChangeSent || !minionPartialSeenUtc.HasValue ||
                (DateTime.UtcNow - minionPartialSeenUtc.Value).TotalSeconds <
                    PublicObservationSeconds || !IsTeamController(agreement.ChoosingSide) ||
                machine.IsWaitingForHost)
                return;
            var deselect = compositionRoot.Runtime.ReadModel.DecisionContract?.Commands
                .FirstOrDefault(value => value.Kind == DirectorCommandKind.DeselectMinionBattleRemoval);
            if (deselect == null)
            {
                Fail("minion-partial-proposal-had-no-deselect");
                return;
            }
            if (compositionRoot.Runtime.SubmitProjectedCommand(deselect))
            {
                minionChangeSent = true;
                nextActionUtc = DateTime.UtcNow.AddSeconds(ActionIntervalSeconds);
            }
        }

        private void DriveMinionSelection(PrivateSessionClient machine)
        {
            var choice = compositionRoot.Runtime.ReadModel.PendingMinionBattleChoice;
            if (choice == null || !IsTeamController(choice.ChoosingSide) || machine.IsWaitingForHost)
                return;
            // No seat ever submits the final confirmation: milestone 8.30n reserves it for the
            // host's unanimous agreement, and this seat's own authorized set says so.
            var commands = AuthorizedProjectedCommands(machine, compositionRoot.Runtime)
                .Where(value => value.Kind == DirectorCommandKind.SelectMinionBattleRemoval)
                .ToArray();
            if (commands.Length == 0)
                return;
            DirectorCommand selected;
            if (minionChangeSent && !minionReplacementSent)
            {
                selected = commands.FirstOrDefault(command => !initialMinionSelection.Any(unit =>
                    (unit.Kind == RuleObjectKind.Hero && unit.HeroId.Equals(command.HeroId)) ||
                    (unit.Kind == RuleObjectKind.Minion && unit.MinionId.Equals(command.MinionId))));
                if (selected == null)
                {
                    Fail("minion-change-had-no-different-removal-candidate");
                    return;
                }
            }
            else
            {
                selected = commands[SelectIndex(options.Seed, choice.SelectedUnits.Count,
                    commands.Length)];
            }
            if (compositionRoot.Runtime.SubmitProjectedCommand(selected))
            {
                if (minionChangeSent)
                    minionReplacementSent = true;
                nextActionUtc = DateTime.UtcNow.AddSeconds(ActionIntervalSeconds);
            }
        }

        private void SubmitMinionAgreementsInOrder(
            PrivateSessionClient machine, TeamAgreementProjection agreement)
        {
            var ownIndex = IndexOfParticipant(agreement, compositionRoot.Runtime.Seat.HeroId);
            if (ownIndex < 0 || machine.IsWaitingForHost)
                return;
            var own = agreement.Participants[ownIndex];
            if (own.Agreed || !agreement.Participants.Take(ownIndex).All(value => value.Agreed))
                return;
            SubmitAgreementOfKind(
                agreement, TeamAgreementCommandKind.AgreeToMinionBattleSelection);
        }

        private bool SubmitAgreementOfKind(
            TeamAgreementProjection agreement, TeamAgreementCommandKind kind)
        {
            var statement = agreement.AuthorizedCommands.FirstOrDefault(value => value.Kind == kind);
            if (statement == null)
                return false;
            var sent = compositionRoot.Runtime.SubmitProjectedAgreement(statement);
            if (sent)
                nextActionUtc = DateTime.UtcNow.AddSeconds(ActionIntervalSeconds);
            return sent;
        }

        private void ObserveCompletedAgreements()
        {
            var runtime = compositionRoot.Runtime;
            var agreement = runtime.ReadModel.TeamAgreement;
            if (!initiativeFinalObserved && initiativeProposal.Length > 0 && agreement == null &&
                runtime.SelectedActor.IsDefined)
            {
                if (!initiativeDisagreementReported ||
                    !runtime.SelectedActor.CardInstanceId.Equals(initiativeTarget))
                {
                    Fail("initiative-agreement-did-not-resolve-revised-target");
                    return;
                }
                initiativeFinalObserved = true;
                tieSideAtSelection = runtime.TieBreakerState.RevealedSide;
                tiedActor = runtime.SelectedActor.CardInstanceId;
                tiedActorFlips = runtime.SelectedActor.FlipTieBreakerAfterResolution;
                tiedActorVersion = runtime.ReadModel.Version;
                ClientMarker("initiative-pass", runtime.ReadModel.Version);
            }

            if (initiativeFinalObserved && !tieBehaviorObserved &&
                runtime.ReadModel.Version > tiedActorVersion &&
                (!runtime.SelectedActor.IsDefined ||
                    !runtime.SelectedActor.CardInstanceId.Equals(tiedActor)))
            {
                var expected = tiedActorFlips
                    ? tieSideAtSelection == TeamSide.Red ? TeamSide.Blue : TeamSide.Red
                    : tieSideAtSelection;
                if (runtime.TieBreakerState.RevealedSide != expected)
                {
                    Fail("tie-breaker-transition-diverged");
                    return;
                }
                tieBehaviorObserved = true;
                ClientMarker("tie-breaker-pass", runtime.ReadModel.Version);
            }

            if (!minionFinalObserved && minionChangedProposal.Length > 0 && agreement == null &&
                runtime.ReadModel.PendingMinionBattleChoice == null)
            {
                minionFinalObserved = true;
                ClientMarker("minion-pass", runtime.ReadModel.Version);
            }
        }

        private void AuditProjection(PrivateSessionClient machine)
        {
            if (machine.ProjectionRevision == auditedRevision)
                return;
            auditedRevision = machine.ProjectionRevision;
            var projection = machine.LatestProjection;
            var seat = compositionRoot.Runtime.Seat;
            if (!string.Equals(projection.SeatId, seat.Id.Value, StringComparison.Ordinal) ||
                !string.Equals(projection.PlayerId, seat.PlayerId.Value, StringComparison.Ordinal) ||
                !string.Equals(projection.HeroId, seat.HeroId.Value, StringComparison.Ordinal) ||
                projection.TeamSide != (int)seat.TeamSide ||
                projection.OwnCards.Any(value =>
                    !string.Equals(value.OwnerHeroId, projection.HeroId, StringComparison.Ordinal)) ||
                projection.PublicCards.Any(value =>
                    string.Equals(value.OwnerHeroId, projection.HeroId, StringComparison.Ordinal) ||
                    value.Location == (int)CardLocation.Hand ||
                    value.Location == (int)CardLocation.Deck ||
                    value.FaceOrientation != (int)CardFaceOrientation.FaceUp) ||
                projection.AuthorizedCommands.Any(value =>
                    !SeatCommandOwnership.Claim(value).IsHeldBy(seat)) ||
                !AgreementAuthorityIsScoped(projection, seat))
            {
                privacyFailed = true;
                Fail("owner-projection-privacy-or-authority");
            }
        }

        private static bool AgreementAuthorityIsScoped(
            NetworkSeatProjectionRecord projection, MatchSeat seat)
        {
            var agreement = projection.Detail?.TeamAgreement;
            if (agreement == null || agreement.AuthorizedStatements.Count == 0)
                return true;
            if (agreement.ChoosingSide != (int)seat.TeamSide ||
                !agreement.Participants.Any(value =>
                    string.Equals(value.HeroId, seat.HeroId.Value, StringComparison.Ordinal)))
                return false;
            return agreement.AuthorizedStatements.All(statement =>
                string.Equals(statement.ProposalId.Value, agreement.ProposalId,
                    StringComparison.Ordinal) &&
                (statement.Kind != TeamAgreementCommandKind.PreferInitiativeCandidate ||
                 agreement.Candidates.Any(candidate => string.Equals(
                     candidate.CardInstanceId, statement.PreferredCardInstanceId.Value,
                     StringComparison.Ordinal))));
        }

        private bool IsTeamController(TeamSide side) =>
            string.Equals(options.Seat.Value,
                side == TeamSide.Red
                    ? OfficialSixPlayerParticipants.RedFirstPlayerIdValue
                    : OfficialSixPlayerParticipants.BlueFirstPlayerIdValue,
                StringComparison.Ordinal);

        private static int IndexOfParticipant(TeamAgreementProjection agreement, HeroId hero)
        {
            for (var index = 0; index < agreement.Participants.Count; index++)
                if (agreement.Participants[index].HeroId.Equals(hero))
                    return index;
            return -1;
        }

        internal static int SelectIndex(int seed, long version, int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));
            var state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
            var draws = Math.Max(1L, version + 1L);
            for (long index = 0; index < draws; index++)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
            }
            return (int)(state % (uint)count);
        }

        private void OnHostEvent(PrivateSessionEventRecord record)
        {
            if (record.Kind == PrivateSessionEventKind.MatchStarted)
                PrivateOnlineMarkers.Log("REHEARSAL_TOPOLOGY host=1 clients=6");
        }

        private void ClientMarker(string eventName, long version)
        {
            PrivateOnlineMarkers.Log(
                "REHEARSAL_CLIENT event=" + eventName + " seat=" + options.Seat.Value +
                (version < 0
                    ? string.Empty
                    : " version=" + version.ToString(CultureInfo.InvariantCulture)));
        }

        private void Fail(string code)
        {
            PrivateOnlineMarkers.Fail(
                "rehearsal=" + code +
                (options?.Seat.IsDefined == true ? " seat=" + options.Seat.Value : string.Empty));
            Quit(1);
        }

        private void Quit(int code)
        {
            if (stopped)
                return;
            stopped = true;
            PrivateOnlineMarkers.Log("EXIT " + code.ToString(CultureInfo.InvariantCulture));
            UnityEngine.Application.Quit(code);
        }

        // Milestone 8.35k. Named per cohort rather than "everything that is not host-loss is a
        // match", so a log says which of the four a process was running.
        private static string ModeLabel(PrivateOnlineRehearsalMode mode)
        {
            switch (mode)
            {
                case PrivateOnlineRehearsalMode.HostLoss: return "host-loss";
                case PrivateOnlineRehearsalMode.Rejoin: return "rejoin";
                case PrivateOnlineRehearsalMode.RejoinReturn: return "rejoin-return";
                default: return "match";
            }
        }

        private void OnDestroy()
        {
            if (lobby != null)
                lobby.HostEventObserved -= OnHostEvent;
        }
    }
}
