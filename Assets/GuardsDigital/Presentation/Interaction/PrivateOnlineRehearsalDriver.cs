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
using GuardsDigital.Presentation.Board;
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
        RejoinReturn = 3,

        /// <summary>
        /// The THIRTEENTH-amendment bot-seat regression: one scripted "human" seat (this driver, in
        /// this mode, playing ordinarily - no forced disagreement, no forced proposal change, since
        /// there is no second scripted teammate to force it against) plus the host's own
        /// <see cref="PrivateLobbyControl.FillWithTestDrivers"/> filling every other seat with real
        /// spawned <see cref="PrivateSeatDriver"/> processes. Proves the shipped feature end to end
        /// through its real control, not a shortcut.
        /// </summary>
        Solo = 4,

        /// <summary>
        /// Milestone <c>8.35z54</c>. THE PACKAGE CEREMONY'S REQUIRED MULTI-CLIENT STEP, and the
        /// distillation of the eleven-hero six-client acceptance into something cheap enough to run on
        /// every cut.
        ///
        /// <para>It is the acceptance's shape with the match removed: a host and two real joiner
        /// processes walk the user's own flow - join naming no seat, take a seat, READY, pick a hero
        /// in p.22's order, the remaining seats filled by the host's own FILL WITH BOTS control - and
        /// every process stops at the FIRST COMMITMENT WINDOW, which is exactly where all three
        /// questions this exists to ask are already answerable. It plays no match, forces no fault and
        /// injects no disconnect, because <see cref="Match"/> already owns those and a gate nobody can
        /// afford to run is a gate that gets skipped.</para>
        ///
        /// <para>WHY THIS TIER AT ALL, stated so it is not "cheaper is better": the z51 package
        /// shipped a lobby whose picked-line-up match was unplayable on all six seats, and every
        /// journey was green because every journey drives the HOST's own view. Only two real
        /// processes disagreeing about a seat can catch that class, so the package ceremony now owes
        /// at least two.</para>
        /// </summary>
        Smoke = 5
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
            else if (string.Equals(mode, "solo", StringComparison.OrdinalIgnoreCase))
                parsedMode = PrivateOnlineRehearsalMode.Solo;
            else if (string.Equals(mode, "smoke", StringComparison.OrdinalIgnoreCase))
                parsedMode = PrivateOnlineRehearsalMode.Smoke;
            else
            {
                error = "the rehearsal mode must be match, host-loss, rejoin, rejoin-return, solo or smoke";
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

            if (parsedRole == PrivateOnlineRehearsalRole.Host && parsedSeat.IsDefined &&
                parsedMode != PrivateOnlineRehearsalMode.Smoke)
            {
                error = "the dedicated rehearsal host is seatless outside playing-host smoke";
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
        private bool testDriversFillAttempted;
        private bool testDriversRefillAttempted;
        private bool hudReported;
        private bool hostHudReported;

        /// <summary>
        /// Milestone <c>8.35z54</c>. One-shot, because <c>Quit</c> does not stop the frame it is called
        /// on and this block would otherwise log its pass line again on the way out.
        /// </summary>
        private bool smokeReported;
        private bool stopped;
        private bool privacyFailed;
        private long auditedRevision = -1;

        /// <summary>
        /// PLAYTEST FAILURE #2 gate. The hero THIS seat actually drafted, remembered so the smoke
        /// assertion can prove the composed match fields the DRAFT rather than the printed default -
        /// the degeneracy that hid the shipped bug is that "composed the draft" and "kept the default"
        /// are the same board unless the draft is provably different, so the smoke draft is
        /// deliberately NON-DEFAULT (last-available) and each seat asserts its own pick landed.
        /// </summary>
        private string smokePickedHeroId = string.Empty;
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
            // The Solo cohort's one scripted seat plays ordinarily for the same reason a relaunched
            // process does: there is no second scripted teammate here to force a disagreement or a
            // changed proposal against, so pre-observing these skips straight to the "ordinary"
            // branch every agreement-driving method already has.
            if (options != null && (options.Mode == PrivateOnlineRehearsalMode.RejoinReturn ||
                    options.Mode == PrivateOnlineRehearsalMode.Solo))
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
                // 8.36h's package smoke uses the shipped playing-host topology. Its authority
                // and its ordinary loopback player client both live in this process, so drive the
                // owner-scoped client through the same claim/ready/pick/HUD path as a joiner.
                if (!stopped && options.Mode == PrivateOnlineRehearsalMode.Smoke &&
                    options.Seat.IsDefined)
                {
                    UpdateClient();
                }
                return;
            }

            UpdateClient();
        }

        private void PressEntryWhenReady()
        {
            if (lobby.Presenter?.Model == null)
                return;

            // Milestone 8.35z54. THE SMOKE COHORT JOINS NAMING NO SEAT, which is the user's flow spec
            // read literally - "they join the server, see open seats, they pick a seat" - and it is a
            // different code path from the one every other cohort takes. The older cohorts carry their
            // seat in the entry, which the host still accepts and which is what lets them be
            // deterministic about who sits where; that path cannot observe the seatless join at all,
            // and the seatless join is exactly what the z51 playtest was doing when it broke. So this
            // one arrives as a stranger and takes a chair from inside the session, through the same
            // control a player presses.
            var joinsSeatless = options.Mode == PrivateOnlineRehearsalMode.Smoke &&
                options.Role == PrivateOnlineRehearsalRole.Client;
            var entry = new PrivateLobbyEntry(
                options.Address,
                options.Port.ToString(CultureInfo.InvariantCulture),
                options.SessionKey,
                joinsSeatless ? default : options.Seat);
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

            // The Solo cohort's own host: presses the shipped feature's real lobby control exactly
            // once, unattended, so the regression proves that control rather than a shortcut around
            // it. One attempt only - a refusal (nothing left open, or the control briefly not
            // offered) is not retried into a spin.
            //
            // Waits for the scripted seat's OWN claim first - real solo play's actual order, and
            // load-bearing here: this host is seatless and cannot name which seat is "the human's"
            // to protect, so pressing the moment hosting begins (nobody claimed yet) fills every
            // seat including the one the scripted client is about to ask for, and its own join is
            // then refused SessionFull. ClaimedSeatCount >= 1 is an exact signal for "the scripted
            // client got there first" precisely because nothing else can claim a seat before this
            // control is pressed - the bots this control itself spawns are the only other claimants
            // that will ever exist in this cohort.
            // Milestone 8.35z54's smoke host fills for the same reason and with the same one-shot
            // discipline, but waits for BOTH scripted joiners to be seated rather than one: this
            // cohort's whole point is that two real processes are in the session at once, and filling
            // while only the first has arrived would take the second one's chair.
            // The current package smoke has three humans: this process's playing-host seat and two
            // joiners. Waiting for all three keeps FILL from taking the second joiner's chair.
            var smokeHumanCount = options.Seat.IsDefined ? 3 : 2;
            if (options.Mode == PrivateOnlineRehearsalMode.Smoke && !testDriversFillAttempted &&
                !host.HasMatchStarted && host.ClaimedSeatCount >= smokeHumanCount)
            {
                if (lobby.Presenter.Model?.Offers(PrivateLobbyControl.FillWithTestDrivers) == true)
                {
                    testDriversFillAttempted = true;
                    if (!lobby.Presenter.Press(PrivateLobbyControl.FillWithTestDrivers, null))
                    {
                        Fail("smoke-fill-control-refused");
                        return;
                    }

                    PrivateOnlineMarkers.Log("REHEARSAL_SMOKE_FILLED claimed=" +
                        host.ClaimedSeatCount.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (options.Mode == PrivateOnlineRehearsalMode.Solo && !testDriversFillAttempted &&
                !host.HasMatchStarted && host.ClaimedSeatCount >= 1)
            {
                if (lobby.Presenter.Model?.Offers(PrivateLobbyControl.FillWithTestDrivers) == true)
                {
                    testDriversFillAttempted = true;
                    if (!lobby.Presenter.Press(PrivateLobbyControl.FillWithTestDrivers, null))
                    {
                        Fail("solo-fill-control-refused");
                        return;
                    }

                    PrivateOnlineMarkers.Log("REHEARSAL_SOLO_FILLED");
                }
            }

            // 8.35t corrective, "must not stack". Pressed a second time immediately after the
            // first, still unattended: every seat FILL just sent a driver to is still Open at this
            // exact instant (a fresh child has not had time to dial in yet), so a naive repeat
            // would spawn a second, redundant process onto each of those same five seats if the
            // dedupe fix were not in place. The regression's own log-count assertion is what
            // actually proves it - this press only creates the condition to prove.
            if (options.Mode == PrivateOnlineRehearsalMode.Solo && testDriversFillAttempted &&
                !testDriversRefillAttempted && !host.HasMatchStarted)
            {
                testDriversRefillAttempted = true;
                lobby.Presenter.Press(PrivateLobbyControl.FillWithTestDrivers, null);
                PrivateOnlineMarkers.Log("REHEARSAL_SOLO_REFILL_ATTEMPTED");
            }

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
                if (!hostHudReported)
                {
                    hostHudReported = true;
                    PrivateOnlineMarkers.Log("REHEARSAL_HOSTLOSS_READY version=" +
                        projection.Version.ToString(CultureInfo.InvariantCulture));
                }
                return;
            }

            // Milestone 8.35z54. The smoke host's whole job ends at the first commitment window: the
            // match it just composed from a chosen line-up is the thing under test, and the state of
            // it that answers every question this cohort asks is the state before anybody acts. The
            // version is reported rather than assumed so the runner asserts it is 0 - a host that had
            // already accepted a command would be the fourth playtest finding, seen from the
            // authority's own side.
            if (options.Mode == PrivateOnlineRehearsalMode.Smoke)
            {
                if (!hostHudReported)
                {
                    hostHudReported = true;
                    PrivateOnlineMarkers.Log(
                        "REHEARSAL_SMOKE_HOST_READY version=" +
                        projection.Version.ToString(CultureInfo.InvariantCulture) +
                        " phase=" + compositionRoot.Runtime.Phase +
                        " seats=" + host.ClaimedSeatCount.ToString(CultureInfo.InvariantCulture));
                    if (!options.Seat.IsDefined)
                    {
                        lobby.Presenter.Press(PrivateLobbyControl.Leave, null);
                        Quit(0);
                    }
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

            // Milestone 8.35z54. A smoke client arrived naming no seat, so it is ADMITTED rather than
            // Seated and its first job is to take the chair the runner assigned it - through the same
            // TAKE SEAT control a player presses, against the host's own roster. It re-presses on
            // every frame the control is still offered, which costs nothing and is what makes it
            // robust to the one real race here: two joiners reaching for chairs at the same instant.
            if (machine.State == PrivateSessionClientState.Admitted)
            {
                ClaimOwnSeatWhenOffered();
                return;
            }

            if (machine.State == PrivateSessionClientState.Seated && !machine.HasMatchStarted)
            {
                if (!readinessPressed &&
                    lobby.Presenter.Model?.Offers(PrivateLobbyControl.Ready) == true)
                    readinessPressed = lobby.Presenter.Press(PrivateLobbyControl.Ready, null);
                // 8.35z54. The picks run after readiness now and EVERY seat takes one, so a
                // cohort that only pressed READY would wait at an open selection forever. It picks
                // the way a player does - out of the options its own model offers, while the order
                // is on this seat - which also means the rehearsal exercises the shipped pick path
                // on every run instead of leaving it to the one tier nobody re-ran.
                PickHeroWhenOffered();
                return;
            }

            if (!lobby.RehearsalHudAttached || machine.LatestProjection == null)
                return;
            if (!hudReported)
            {
                hudReported = true;
                ClientMarker("hud", machine.LatestProjection.Version);
                ReportFirstMatchState(machine);
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

            // Milestone 8.35z54. The smoke cohort stops HERE, one line after the audit rather than one
            // line before it: `AuditProjection` is the per-seat authorization proof - it fails the run
            // if any offered command is not held by this seat - and it is the first playtest finding's
            // direct negative, so passing through it is the point of coming this far. Everything below
            // is the full match's business.
            if (options.Mode == PrivateOnlineRehearsalMode.Smoke)
            {
                if (!smokeReported)
                {
                    smokeReported = true;
                    // PLAYTEST FAILURE #2 gate. AuditProjection above proved this seat's authority is
                    // scoped to its own hero; this proves the hero it is scoped ABOUT is the one this
                    // seat DRAFTED rather than the printed default, and that the MAP, the HANDS/tray
                    // and the roster all field it. A failure here is the shipped bug caught, and it
                    // fails the run (Quit 1) rather than reporting a pass.
                    if (!AssertSmokeDraftLanded(machine))
                        return;
                    PrivateOnlineMarkers.Log(
                        "REHEARSAL_SMOKE_PASS seat=" + options.Seat.Value +
                        " hero=" + compositionRoot.Runtime.Seat.HeroId.Value +
                        " authorized=" + machine.LatestProjection.AuthorizedCommands.Count
                            .ToString(CultureInfo.InvariantCulture) +
                        " version=" + machine.LatestProjection.Version
                            .ToString(CultureInfo.InvariantCulture));
                    Quit(0);
                }

                return;
            }

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
        /// <summary>
        /// Shared with <see cref="PrivateSeatDriver"/>: the whole point of the intersection - see
        /// the remarks above - is one answer to "what may this seat legally press right now", so a
        /// second driver reads the same one rather than re-deriving it.
        /// </summary>
        internal static IReadOnlyList<DirectorCommand> AuthorizedProjectedCommands(
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

        /// <summary>
        /// Milestone <c>8.35z54</c>. Takes THIS process's assigned seat from inside the session, which
        /// is the half of the user's flow spec no other cohort walks.
        /// </summary>
        /// <remarks>
        /// The seat is named on the command line as before - the runner still decides who sits where,
        /// because a smoke whose seats varied run to run could not assert anything about them - but it
        /// is claimed through <see cref="PrivateLobbyControl.ClaimSeat"/> rather than carried in the
        /// join. If the roster does not offer this seat the driver says so by name instead of silently
        /// taking a different chair: a smoke that quietly reseats itself would pass while proving the
        /// wrong thing.
        /// </remarks>
        private void ClaimOwnSeatWhenOffered()
        {
            var model = lobby.Presenter.Model;
            if (model == null || !model.Offers(PrivateLobbyControl.ClaimSeat))
                return;

            var mine = model.Seats.FirstOrDefault(row =>
                string.Equals(row.Seat.Value, options.Seat.Value, StringComparison.Ordinal));
            if (mine == null)
            {
                Fail("smoke-seat-not-on-roster-" + options.Seat.Value);
                return;
            }

            // The seat rides the entry exactly as the view's own seat-row button sends it: that button
            // picks the row and then presses ClaimSeat with the current entry, and this is that pair
            // written out, so the press the host receives is byte-for-byte a player's.
            var claim = new PrivateLobbyEntry(
                options.Address,
                options.Port.ToString(CultureInfo.InvariantCulture),
                options.SessionKey,
                mine.Seat);
            if (lobby.Presenter.Press(PrivateLobbyControl.ClaimSeat, claim))
                ClientMarker("seat-claimed-" + options.Seat.Value, -1);
        }

        /// <summary>
        /// Milestone <c>8.35z54</c>. The two lines the eleven-hero acceptance reads, written ONCE at
        /// the first frame this seat holds a match HUD - which is the earliest moment either question
        /// can be asked and the latest at which the answer is still about a fresh match.
        ///
        /// <para><b>PRECOMMIT is the fourth playtest finding's direct negative</b> ("wasp made a move
        /// before even selecting cards"). A match that has accepted no command stands at projection
        /// version 0 in <c>AwaitingCommitments</c>; anything else means something acted before the
        /// commitment window, and the version says how much. Reporting BOTH the version and the phase
        /// is deliberate - a wrong phase at version 0 and a right phase at version 3 are different
        /// defects, and a single boolean would hide which one happened.</para>
        ///
        /// <para><b>BOARDRECT is the fifth finding's direct negative</b> ("map sizing broke a bit
        /// somehow on player side"). <c>configured=False</c> is the defect itself - a board still
        /// wearing the framing it was built with, because the layout event never fired a second time
        /// for a client that re-composed. The size and viewport let the runner compare every process
        /// against every other rather than merely check a flag, so a future regression that configures
        /// the viewport WRONG is caught by the same line that catches one that never configures it.</para>
        /// </summary>
        private void ReportFirstMatchState(PrivateSessionClient machine)
        {
            var runtime = compositionRoot.Runtime;
            PrivateOnlineMarkers.Log(
                "REHEARSAL_PRECOMMIT seat=" + options.Seat.Value +
                " version=" + runtime.ReadModel.Version.ToString(CultureInfo.InvariantCulture) +
                " phase=" + runtime.Phase +
                " wireversion=" + machine.LatestProjection.Version.ToString(CultureInfo.InvariantCulture));

            var board = compositionRoot.MapView;
            if (board == null)
            {
                PrivateOnlineMarkers.Log("REHEARSAL_BOARDRECT seat=" + options.Seat.Value + " board=none");
                return;
            }

            var layout = board.ConfiguredCameraLayout;
            PrivateOnlineMarkers.Log(
                "REHEARSAL_BOARDRECT seat=" + options.Seat.Value +
                " configured=" + board.HasConfiguredCameraViewport +
                " ortho=" + board.CameraOrthographicSize.ToString("F3", CultureInfo.InvariantCulture) +
                " panel=" + layout.PanelViewport.x.ToString("F0", CultureInfo.InvariantCulture) +
                "x" + layout.PanelViewport.y.ToString("F0", CultureInfo.InvariantCulture) +
                " upper=" + layout.UpperRegion.x.ToString("F0", CultureInfo.InvariantCulture) +
                "," + layout.UpperRegion.y.ToString("F0", CultureInfo.InvariantCulture) +
                "," + layout.UpperRegion.width.ToString("F0", CultureInfo.InvariantCulture) +
                "," + layout.UpperRegion.height.ToString("F0", CultureInfo.InvariantCulture) +
                " px=" + board.CameraPixelRect.width.ToString("F0", CultureInfo.InvariantCulture) +
                "x" + board.CameraPixelRect.height.ToString("F0", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 8.35z54. Takes this seat's hero when rulebook p.22's order reaches it, out of the
        /// options this seat's own lobby model offers - the same rule
        /// <c>PrivateSeatDriver.PickHeroWhenOffered</c> follows, and the same reason: a driver
        /// choosing from what it can NAME rather than from what it is OFFERED is milestone 8.35c's
        /// first live-found defect.
        /// </summary>
        private void PickHeroWhenOffered()
        {
            var model = lobby.Presenter.Model;
            if (model == null || !model.Offers(PrivateLobbyControl.ChooseHero))
                return;

            // PLAYTEST FAILURE #2 gate. In Smoke mode this seat takes the LAST available hero rather
            // than the first, so the drafted line-up differs from the printed default and "composed
            // the draft" is distinguishable from "kept the default" - the exact degeneracy that let
            // the shipped bug pass every green tier. Every other cohort keeps first-available, which
            // is deterministic and reproduces the shipped board.
            string chosenHeroId = null;
            foreach (var option in model.Selection.Heroes)
            {
                if (!option.IsAvailable)
                    continue;
                chosenHeroId = option.HeroIdValue;
                if (options.Mode != PrivateOnlineRehearsalMode.Smoke)
                    break;
                // Smoke: keep scanning so the LAST available option wins.
            }

            if (chosenHeroId == null)
                return;

            lobby.Presenter.SelectedHeroIdValue = chosenHeroId;
            if (lobby.Presenter.Press(PrivateLobbyControl.ChooseHero, null))
            {
                smokePickedHeroId = chosenHeroId;
                ClientMarker("hero-picked-" + chosenHeroId, -1);
            }
        }

        /// <summary>
        /// PLAYTEST FAILURE #2 gate, the enshrined board-vs-tray-vs-draft check the shipped bug slipped
        /// past twice. <see cref="AuditProjection"/> already proves this seat's projection is
        /// internally consistent and its authority scoped to its own hero; what it cannot prove is
        /// that the hero it is consistent ABOUT is the one this seat DRAFTED rather than the printed
        /// default - "composed the draft" and "kept the default" are internally identical, the exact
        /// degeneracy the coordinator named as why every green tier missed this. This asserts the
        /// seat's DRAFTED hero (deliberately non-default in Smoke mode) is the hero its ONLINE
        /// projection fields, its RECOMPOSED local board seats, and its TRAY draws - the three tiers
        /// the user saw disagree ("heroes spawned on the map" but "hero trays did not change; can play
        /// all six original heroes"). It fails the run rather than reporting a pass.
        /// </summary>
        private bool AssertSmokeDraftLanded(PrivateSessionClient machine)
        {
            var rt = compositionRoot.Runtime;
            var projection = machine.LatestProjection;

            // board = the recomposed local host's seats (what spawns on the MAP).
            var board = rt?.Host == null
                ? new List<string>()
                : rt.Host.Seats.Seats
                    .Select(seat => seat.TeamSide + ":" + seat.HeroId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList();
            // tray = the online seat projection's setup heroes (what the hands / hero rail draw).
            var tray = rt?.ReadModel?.Setup?.Heroes == null
                ? new List<string>()
                : rt.ReadModel.Setup.Heroes
                    .Select(hero => hero.TeamSide + ":" + hero.HeroId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList();
            // roster = the drafted line-up the roster names.
            var lineup = machine.ComposedLineup();
            var roster = lineup == null
                ? new List<string>()
                : lineup.Red.Select(hero => TeamSide.Red + ":" + hero.HeroIdValue)
                    .Concat(lineup.Blue.Select(hero => TeamSide.Blue + ":" + hero.HeroIdValue))
                    .OrderBy(value => value, StringComparer.Ordinal).ToList();
            var home = rt?.Seat == null ? "none" : rt.Seat.HeroId.Value;

            PrivateOnlineMarkers.Log(
                "REHEARSAL_SPLITCHECK seat=" + options.Seat.Value +
                " picked=" + (smokePickedHeroId.Length == 0 ? "none" : smokePickedHeroId) +
                " home=" + home +
                " projection=" + (projection == null ? "none" : projection.HeroId) +
                " board=[" + string.Join(",", board) + "]" +
                " tray=[" + string.Join(",", tray) + "]" +
                " roster=[" + string.Join(",", roster) + "]");

            // A Smoke client always drafts a hero; a seat that reached here without one means the
            // pick control was never truly reachable, which is itself a finding.
            if (smokePickedHeroId.Length == 0)
            {
                Fail("smoke-split-no-pick seat=" + options.Seat.Value);
                return false;
            }

            // The ONLINE projection - the authority the hands and tray read - must field THIS seat's
            // drafted hero. A default here is the split-brain the user hit: the board recomposed to
            // the draft while the projection kept the printed line-up.
            if (projection == null ||
                !string.Equals(projection.HeroId, smokePickedHeroId, StringComparison.Ordinal))
            {
                Fail("smoke-split-projection-not-draft picked=" + smokePickedHeroId +
                    " projection=" + (projection == null ? "none" : projection.HeroId));
                return false;
            }

            // The recomposed local board (the MAP) must seat the same drafted hero for this seat.
            if (rt?.Seat == null ||
                !string.Equals(rt.Seat.HeroId.Value, smokePickedHeroId, StringComparison.Ordinal))
            {
                Fail("smoke-split-board-not-draft picked=" + smokePickedHeroId + " home=" + home);
                return false;
            }

            // No split between the map and the hands/tray the presenters draw.
            if (!board.SequenceEqual(tray))
            {
                Fail("smoke-split-board-vs-tray board=[" + string.Join(",", board) +
                    "] tray=[" + string.Join(",", tray) + "]");
                return false;
            }

            // And the composed board is the DRAFT itself, not merely some valid set of the right size.
            if (roster.Count == 0 || !board.SequenceEqual(roster))
            {
                Fail("smoke-split-board-vs-draft board=[" + string.Join(",", board) +
                    "] draft=[" + string.Join(",", roster) + "]");
                return false;
            }

            return true;
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
                case PrivateOnlineRehearsalMode.Solo: return "solo";
                case PrivateOnlineRehearsalMode.Smoke: return "smoke";
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
