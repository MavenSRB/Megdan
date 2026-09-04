using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// THIRTEENTH amendment. The command line a host-spawned test-driver child reads. Distinct from
    /// <see cref="PrivateOnlineRehearsalOptions"/> deliberately: that vocabulary is CI-only
    /// choreography (forced disagreements, scripted cuts) for a process nobody ships, and adding a
    /// mode to it would let a shipped feature and a CI probe answer to the same flag by accident.
    /// This one is small on purpose - a seat and how patient to be - because the play itself is
    /// "whatever is legal, at random".
    /// </summary>
    /// <remarks>
    /// Endpoint and key travel only through this process's inherited environment block, exactly as
    /// the rehearsal driver's do, so Unity's command-line banner cannot print an invitation secret -
    /// the same privacy rule, independently satisfied rather than borrowed by sharing the flag.
    /// </remarks>
    internal sealed class PrivateSeatDriverOptions
    {
        public const string RoleFlag = "-guardsTestDriver";
        public const string SeatFlag = "-guardsTestDriverSeat";
        public const string ThinkSecondsFlag = "-guardsTestDriverThinkSeconds";
        public const string SeedFlag = "-guardsTestDriverSeed";
        public const string AddressEnvironmentVariable = "GUARDS_TESTDRIVER_ADDRESS";
        public const string PortEnvironmentVariable = "GUARDS_TESTDRIVER_PORT";
        public const string KeyEnvironmentVariable = "GUARDS_TESTDRIVER_SESSION_KEY";

        /// <summary>
        /// The default think-delay: long enough for a solo player watching their own screen to
        /// follow what just happened before the next bot acts, short enough that six bots do not
        /// make a full round take minutes. The rehearsal drivers' own 0.02 s pace is the thing the
        /// amendment calls out as unwatchable; this is deliberately about a hundred times slower.
        /// </summary>
        public const double DefaultThinkSeconds = 2.0;

        public SeatId Seat { get; private set; }
        public string Address { get; private set; }
        public int Port { get; private set; }
        public string SessionKey { get; private set; }
        public double ThinkSeconds { get; private set; }
        public int Seed { get; private set; }

        public static bool TryParse(string[] args, out PrivateSeatDriverOptions result, out string error)
        {
            result = null;
            error = string.Empty;
            args = args ?? Array.Empty<string>();
            if (Array.IndexOf(args, RoleFlag) < 0)
            {
                return false;
            }

            string seat = null;
            var thinkSecondsText = DefaultThinkSeconds.ToString(CultureInfo.InvariantCulture);
            var seedText = "0";
            for (var index = 0; index < args.Length; index++)
            {
                var argument = args[index] ?? string.Empty;
                if (argument == SeatFlag)
                {
                    seat = Take(args, ref index);
                }
                else if (argument == ThinkSecondsFlag)
                {
                    thinkSecondsText = Take(args, ref index);
                }
                else if (argument == SeedFlag)
                {
                    seedText = Take(args, ref index);
                }
            }

            if (Array.IndexOf(args, GuardsDigital.Network.PrivateSessionLaunchOptions.LobbyFlag) < 0)
            {
                error = "a test driver requires the real private-lobby launch flag";
                return true;
            }

            if (string.IsNullOrWhiteSpace(seat))
            {
                error = "a test driver requires " + SeatFlag + " with the seat to claim";
                return true;
            }

            var address = Environment.GetEnvironmentVariable(AddressEnvironmentVariable);
            var portText = Environment.GetEnvironmentVariable(PortEnvironmentVariable);
            var key = Environment.GetEnvironmentVariable(KeyEnvironmentVariable);
            if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var port) ||
                port < 1 || port > 65535)
            {
                error = "a test driver requires a valid " + PortEnvironmentVariable + " in its environment";
                return true;
            }

            if (!GuardsDigital.Network.PrivateSessionEndpoint.TryCreate(
                    address, port, out var endpoint, out _) || !endpoint.IsLoopback)
            {
                error = "a spawned test driver is confined to a numeric loopback address";
                return true;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                error = "a test driver requires a non-empty " + KeyEnvironmentVariable + " in its environment";
                return true;
            }

            if (!double.TryParse(thinkSecondsText, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var thinkSeconds) || thinkSeconds < 0)
            {
                error = ThinkSecondsFlag + " must be a non-negative number of seconds";
                return true;
            }

            int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed);

            result = new PrivateSeatDriverOptions
            {
                Seat = new SeatId(seat),
                Address = address,
                Port = port,
                SessionKey = key,
                ThinkSeconds = thinkSeconds,
                Seed = seed
            };
            return true;
        }

        private static string Take(string[] args, ref int index)
        {
            if (index + 1 >= args.Length)
            {
                return null;
            }

            index++;
            return args[index];
        }
    }

    /// <summary>
    /// THIRTEENTH amendment. A spawned QA practice opponent: enters through the real primitive
    /// lobby, joins its one assigned seat, and from then on answers whatever the retained HUD path
    /// offers - <see cref="PrivateOnlineRehearsalDriver.AuthorizedProjectedCommands"/> is the same
    /// intersection the CI rehearsal proved, reused rather than re-derived - picking a random legal
    /// answer at a human-followable pace instead of racing to the end of the match.
    /// </summary>
    /// <remarks>
    /// <para>NOT an AI opponent: there is no board evaluation, no difficulty, no notion of a good
    /// move versus a bad one - every legal answer is equally likely, which is the whole of the
    /// design. That is a durable non-goal, not a limitation this milestone works around.</para>
    /// <para>An ordinary keyed client in every way the host or another player's client can observe:
    /// it dials in, presents the session key, and claims its seat exactly as
    /// <see cref="PrivateClientDriver"/>'s command-line role does, just through the lobby rather
    /// than around it. No new authority, no protocol change.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PrivateSeatDriver : MonoBehaviour
    {
        private const double OutcomeLingerSeconds = 2.0;

        private MatchSceneCompositionRoot compositionRoot;
        private PrivateLobbyDriver lobby;
        private PrivateSeatDriverOptions options;
        private System.Random random;
        private DateTime nextActionUtc;
        private DateTime? outcomeSeenUtc;
        private bool entryPressed;
        private bool readinessPressed;
        private bool hudReported;
        private bool stopped;

        internal static bool TryParseRequested(
            string[] args, out PrivateSeatDriverOptions parsed, out string error)
        {
            var requested = args != null && Array.IndexOf(args, PrivateSeatDriverOptions.RoleFlag) >= 0;
            if (!requested)
            {
                parsed = null;
                error = string.Empty;
                return false;
            }

            if (!PrivateSeatDriverOptions.TryParse(args, out parsed, out error))
            {
                parsed = null;
            }

            return true;
        }

        internal void Configure(
            MatchSceneCompositionRoot root, PrivateLobbyDriver lobbyDriver, PrivateSeatDriverOptions driverOptions)
        {
            compositionRoot = root;
            lobby = lobbyDriver;
            options = driverOptions;
            // A minimized/unfocused practice bot must keep playing - it is the other five seats of
            // the solo player's own match, not a window they will ever click into.
            UnityEngine.Application.runInBackground = true;
            random = options != null && options.Seed != 0
                ? new System.Random(options.Seed)
                : new System.Random();
        }

        private void Update()
        {
            if (stopped)
            {
                return;
            }

            if (compositionRoot?.Runtime == null || lobby == null || options == null)
            {
                Fail("test-driver-configuration");
                return;
            }

            if (!entryPressed)
            {
                PressJoinWhenReady();
                return;
            }

            var machine = lobby.RehearsalClient;
            if (machine == null)
            {
                return;
            }

            if (machine.State == PrivateSessionClientState.Rejected ||
                machine.State == PrivateSessionClientState.Faulted)
            {
                Fail("test-driver-terminal-" + machine.State.ToString().ToLowerInvariant());
                return;
            }

            if (machine.State == PrivateSessionClientState.HostLost)
            {
                // The host that spawned this process is gone; there is nothing left to play with
                // and nobody left to answer to. Quiet exit, not a failure - this is exactly the
                // shape a host-loss cohort's own clients already treat as a pass.
                Marker("host-lost");
                Quit(0);
                return;
            }

            if (machine.State == PrivateSessionClientState.Seated && !machine.HasMatchStarted)
            {
                if (!readinessPressed &&
                    lobby.Presenter.Model?.Offers(PrivateLobbyControl.Ready) == true)
                {
                    readinessPressed = lobby.Presenter.Press(PrivateLobbyControl.Ready, null);
                }

                return;
            }

            if (!lobby.RehearsalHudAttached || machine.LatestProjection == null)
            {
                return;
            }

            if (!hudReported)
            {
                hudReported = true;
                Marker("hud version=" + machine.LatestProjection.Version.ToString(CultureInfo.InvariantCulture));
            }

            var outcome = compositionRoot.Runtime.ReadModel.Outcome;
            if (outcome.HasEnded)
            {
                if (!outcomeSeenUtc.HasValue)
                {
                    outcomeSeenUtc = DateTime.UtcNow;
                    Marker("outcome cause=" + outcome.Cause + " winner=" + outcome.WinningTeamId.Value);
                }

                if (machine.State == PrivateSessionClientState.Disconnected ||
                    (DateTime.UtcNow - outcomeSeenUtc.Value).TotalSeconds >= OutcomeLingerSeconds)
                {
                    Quit(0);
                }

                return;
            }

            if (machine.State != PrivateSessionClientState.Seated || machine.IsWaitingForHost ||
                DateTime.UtcNow < nextActionUtc)
            {
                return;
            }

            TryPlayOneAnswer(machine);
        }

        private void PressJoinWhenReady()
        {
            if (lobby.Presenter?.Model == null)
            {
                return;
            }

            var entry = new PrivateLobbyEntry(
                options.Address, options.Port.ToString(CultureInfo.InvariantCulture),
                options.SessionKey, options.Seat);
            if (!lobby.Presenter.Press(PrivateLobbyControl.Join, entry))
            {
                Fail("test-driver-join-control-not-offered");
                return;
            }

            entryPressed = true;
            Marker("begin");
        }

        /// <summary>
        /// One answer, chosen uniformly at random from whatever this seat is both offered and
        /// authorized to press right now - never a strategy, never a preference. Team agreements
        /// (Initiative's tie, the Minion Battle's confirmation) are the two families with their own
        /// ordered multi-step shape rather than a single command, so they get their own small
        /// "ordinary" driving: state or agree the moment it is this hero's turn in the standing
        /// order, choosing the first candidate rather than none, because "no preference at all" is
        /// not itself a legal answer.
        /// </summary>
        private void TryPlayOneAnswer(PrivateSessionClient machine)
        {
            var runtime = compositionRoot.Runtime;

            // Live-fire evidence (offline reproduction in 9s via a throwaway harness probe): a
            // pending minion-battle choice and a standing MinionBattle team agreement can be
            // non-null AT THE SAME TIME - the agreement is the standing PROPOSAL participants are
            // confirming, and it exists even while more removals are still legally selectable.
            // Checking the agreement first (as this method originally did) makes every bot on the
            // choosing side wait forever on a "your turn to answer" condition that can only become
            // true once the choice stops changing - which nothing was left to make happen, since
            // this was the one branch that could still add a selection. The proven rehearsal
            // driver's own priority order settles it: an unconfirmable pending choice is served
            // before the agreement, every time.
            var pendingMinionChoice = runtime.ReadModel.PendingMinionBattleChoice;
            if (pendingMinionChoice != null && !pendingMinionChoice.CanConfirm)
            {
                PlayMinionSelection(machine);
                return;
            }

            var agreement = runtime.ReadModel.TeamAgreement;
            if (agreement != null)
            {
                if (agreement.Kind == TeamAgreementKind.Initiative)
                {
                    PlayInitiativeAgreement(machine, agreement);
                }
                else if (agreement.Kind == TeamAgreementKind.MinionBattle)
                {
                    PlayMinionAgreement(machine, agreement);
                }

                return;
            }

            if (pendingMinionChoice != null)
            {
                PlayMinionSelection(machine);
                return;
            }

            var commands = PrivateOnlineRehearsalDriver.AuthorizedProjectedCommands(machine, runtime);
            if (commands.Count == 0)
            {
                return;
            }

            var selected = commands[random.Next(commands.Count)];
            Submit(runtime.SubmitProjectedCommand(selected));
        }

        /// <summary>
        /// Live-fire evidence, not a guess: the first Solo-cohort run hung here indefinitely.
        /// Reported: each bot runs its own independent <see cref="System.Random"/>, so a naively
        /// "random-legal" pick here has each teammate proposing a DIFFERENT tied card with no
        /// revision logic to converge them - a disagreement no one is scripted to resolve, forever.
        /// A team AGREEMENT is the one family where "legal" and "converges" are not the same
        /// property: every participant must pick <see cref="TeamAgreementProjection.Candidates"/>'s
        /// first entry, deterministically, exactly as the proven rehearsal driver's own
        /// "ordinary" branch already does - not a strategy preference, a consensus requirement.
        /// Individual command selection below has no such constraint and stays genuinely random.
        /// </summary>
        private void PlayInitiativeAgreement(PrivateSessionClient machine, TeamAgreementProjection agreement)
        {
            var ownIndex = IndexOfParticipant(agreement, compositionRoot.Runtime.Seat.HeroId);
            if (ownIndex < 0 || machine.IsWaitingForHost)
            {
                return;
            }

            var own = agreement.Participants[ownIndex];
            if (own.HasStatement || !agreement.Participants.Take(ownIndex).All(value => value.HasStatement))
            {
                return;
            }

            var preferred = agreement.Candidates[0].CardInstanceId;
            var statement = agreement.AuthorizedCommands.FirstOrDefault(value =>
                value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                value.PreferredCardInstanceId.Equals(preferred));
            Submit(statement != null && compositionRoot.Runtime.SubmitProjectedAgreement(statement));
        }

        private void PlayMinionAgreement(PrivateSessionClient machine, TeamAgreementProjection agreement)
        {
            var ownIndex = IndexOfParticipant(agreement, compositionRoot.Runtime.Seat.HeroId);
            if (ownIndex < 0 || machine.IsWaitingForHost)
            {
                return;
            }

            var own = agreement.Participants[ownIndex];
            if (own.Agreed || !agreement.Participants.Take(ownIndex).All(value => value.Agreed))
            {
                return;
            }

            var statement = agreement.AuthorizedCommands.FirstOrDefault(value =>
                value.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection);
            Submit(statement != null && compositionRoot.Runtime.SubmitProjectedAgreement(statement));
        }

        /// <summary>
        /// Whichever seat on the choosing side gets there first submits; a teammate racing the same
        /// tick is simply refused as stale and tries again next think-tick, exactly as two humans
        /// reaching for the same control would be - no coordination is attempted, matching the
        /// "no strategy" rule at team scope as well as at the single-command one.
        /// </summary>
        private void PlayMinionSelection(PrivateSessionClient machine)
        {
            if (machine.IsWaitingForHost)
            {
                return;
            }

            // No seat ever submits the final confirmation - milestone 8.30n reserves it for the
            // host's own unanimous-agreement flow, and this seat's authorized set already says so.
            var commands = PrivateOnlineRehearsalDriver.AuthorizedProjectedCommands(
                    machine, compositionRoot.Runtime)
                .Where(value => value.Kind == DirectorCommandKind.SelectMinionBattleRemoval)
                .ToArray();
            if (commands.Length == 0)
            {
                return;
            }

            Submit(compositionRoot.Runtime.SubmitProjectedCommand(commands[random.Next(commands.Length)]));
        }

        private void Submit(bool accepted)
        {
            if (accepted)
            {
                // The delay lands AFTER a submission, not before it: the pace a solo player watches
                // is the gap between one visible board change and the next, and a driver that also
                // paused before an accepted-but-then-refused-by-a-teammate retry would just look
                // slower without being any more watchable.
                var jitter = 0.5 + random.NextDouble();
                nextActionUtc = DateTime.UtcNow.AddSeconds(options.ThinkSeconds * jitter);
            }
        }

        private static int IndexOfParticipant(TeamAgreementProjection agreement, HeroId hero)
        {
            for (var index = 0; index < agreement.Participants.Count; index++)
            {
                if (agreement.Participants[index].HeroId.Equals(hero))
                {
                    return index;
                }
            }

            return -1;
        }

        private void Marker(string detail) =>
            PrivateOnlineMarkers.Log(
                "TESTDRIVER seat=" + options.Seat.Value +
                (string.IsNullOrEmpty(detail) ? string.Empty : " " + detail));

        private void Fail(string code)
        {
            PrivateOnlineMarkers.Fail(
                code + (options?.Seat.IsDefined == true ? " seat=" + options.Seat.Value : string.Empty));
            Quit(1);
        }

        private void Quit(int code)
        {
            if (stopped)
            {
                return;
            }

            stopped = true;
            PrivateOnlineMarkers.Log("TESTDRIVER seat=" + (options?.Seat.Value ?? "?") + " EXIT " +
                code.ToString(CultureInfo.InvariantCulture));
            UnityEngine.Application.Quit(code);
        }
    }
}
