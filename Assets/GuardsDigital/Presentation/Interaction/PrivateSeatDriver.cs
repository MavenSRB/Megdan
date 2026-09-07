using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
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
        /// 8.35t corrective. Optional, and not validated the way the other three are: a manually
        /// launched driver (outside any host's own spawn, e.g. this session's own diagnostic
        /// reproduction) has no parent worth watching, and an absent or unparsable value simply
        /// leaves the watchdog off rather than refusing the whole launch over it.
        /// </summary>
        public const string ParentProcessIdEnvironmentVariable = "GUARDS_TESTDRIVER_PARENT_PID";

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

        /// <summary>Zero when absent or unparsable: no parent process to watch.</summary>
        public int ParentProcessId { get; private set; }

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

            // 8.35t corrective. This used to require IsLoopback here too, on the assumption a host
            // ever worth spawning a practice bot onto would always be loopback-bound - the exact
            // assumption the live defect disproved: the tester instructions' own solo-practice
            // paragraph, and a host rehearsing the real port-forwarded setup even alone, both bind
            // to this machine's own real address, not loopback, and a bot confined to loopback then
            // dials an address nothing is listening on. Dropping the restriction grants no new
            // capability - PrivateClientDriver's own "-guardsPrivateClient" role has always been
            // free to dial any address with any key, and the session key is what actually gates
            // entry, exactly as for that role. Only a numeric, well-formed endpoint is still
            // required.
            if (!GuardsDigital.Network.PrivateSessionEndpoint.TryCreate(address, port, out var endpoint, out _))
            {
                error = "a test driver requires a valid " + AddressEnvironmentVariable + " in its environment";
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

            // 8.35t corrective. Best-effort only, per the field's own doc comment: an absent or
            // unparsable value leaves ParentProcessId at 0, which the driver reads as "no parent to
            // watch" rather than a reason to refuse the whole launch.
            var parentProcessIdText = Environment.GetEnvironmentVariable(ParentProcessIdEnvironmentVariable);
            int.TryParse(
                parentProcessIdText, NumberStyles.None, CultureInfo.InvariantCulture,
                out var parentProcessId);

            result = new PrivateSeatDriverOptions
            {
                Seat = new SeatId(seat),
                Address = address,
                Port = port,
                SessionKey = key,
                ThinkSeconds = thinkSeconds,
                Seed = seed,
                ParentProcessId = parentProcessId
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
    /// <para>NOT an AI opponent, and this milestone (the tier-0.5 greedy-nudge pass) does not change
    /// that: there is still no board evaluation, no simulation, no lookahead. What changed is that
    /// three of the game's own already-published facts - a legal Attack target existing, a
    /// destination's hex distance to the nearest fight, and whether a standing Defense preview
    /// already says an unanswered hit is lethal - now narrow the random pick to the subset of
    /// legal answers those facts prefer, with the die still deciding among ties. Card SELECTION at
    /// a commitment (which card to play), leveling picks, and which candidate a team agreement
    /// converges on are untouched: durable non-goals, not oversights. See
    /// <see cref="SelectGreedyCommand"/> for the three preferences themselves.</para>
    /// <para>An ordinary keyed client in every way the host or another player's client can observe:
    /// it dials in, presents the session key, and claims its seat exactly as
    /// <see cref="PrivateClientDriver"/>'s command-line role does, just through the lobby rather
    /// than around it. No new authority, no protocol change.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PrivateSeatDriver : MonoBehaviour
    {
        private const double OutcomeLingerSeconds = 2.0;

        /// <summary>
        /// 8.35t corrective. Cheap enough to poll often, but no reason to check every frame: the
        /// one condition this guards against - the spawning host process dying without a clean
        /// <c>Leave()</c> - does not need sub-second reaction time to still be an enormous
        /// improvement over "never", which was the only outcome the game's own two lifecycle hooks
        /// (<c>Leave()</c>, <c>OnDestroy()</c>) already covered.
        /// </summary>
        private const double ParentCheckIntervalSeconds = 3.0;

        private MatchSceneCompositionRoot compositionRoot;
        private PrivateLobbyDriver lobby;
        private PrivateSeatDriverOptions options;
        private System.Random random;

        /// <summary>
        /// Every GAMEPLAY draw comes from here, never from <see cref="random"/>, which is left to the
        /// think-delay jitter alone. The split is the point: <see cref="DeterministicBotRandom"/> is
        /// the same four lines on Mono and on .NET, so one seed names one match in the real build and
        /// in the harness that measured this bot - which is what makes a measured win rate a
        /// statement about the shipped bot rather than about a harness-only twin.
        /// </summary>
        private IBotRandom botRandom;

        /// <summary>
        /// Commands this seat was OFFERED and the rules then REFUSED, at the projection version they
        /// were refused on. Milestone 8.35z22 added it, and the Solo cohort is what forced it.
        /// </summary>
        /// <remarks>
        /// <para><b>A deterministic brain cannot escape an infeasible offer by luck, and a random one
        /// could.</b> <see cref="Submit"/> does nothing on a refusal, so the driver simply re-ticks;
        /// under random-legal (and under the tier-0.5 pass, which falls back to it) the next tick drew
        /// a different candidate and the seat moved on. Tier-1 answers the same unchanged projection
        /// with the same command every time, so a single offered-then-refused command becomes a hot
        /// infinite loop: the first live Solo cohort spun 11,000+ refusals on one seat at a frozen
        /// version 1022 and the match never advanced.</para>
        /// <para>The cause is not the brain. It is the offer-feasibility defect this milestone
        /// records as corrective debt - the projection offering a Defense card the rules refuse -
        /// and the brain only made a latent hazard permanent. Dropping the refused candidate and
        /// re-choosing from what remains is exactly what <c>BotSelfPlay</c> does, and doing it here is
        /// what makes the SHIPPED driver behave the way the measured configuration behaved. It never
        /// widens the candidate set, so every answer stays one the seat was already offered.</para>
        /// </remarks>
        private readonly HashSet<DirectorCommand> refusedThisVersion = new HashSet<DirectorCommand>();
        private long refusedVersion = -1;
        private bool exhaustionReported;
        private DateTime nextActionUtc;
        private DateTime nextParentCheckUtc;
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
            botRandom = new DeterministicBotRandom(
                options != null && options.Seed != 0 ? options.Seed : Environment.TickCount);
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

            // 8.35t corrective. A watchdog on the spawning host's own PID, independent of any
            // network state: the host that spawned this child could die before this child ever
            // reaches Seated, and per-seat join timeouts on the host's own side are exactly that -
            // on the HOST's side, no help to a child that has already lost its host and now has no
            // one left to time it out.
            if (options.ParentProcessId > 0 && DateTime.UtcNow >= nextParentCheckUtc)
            {
                nextParentCheckUtc = DateTime.UtcNow.AddSeconds(ParentCheckIntervalSeconds);
                if (!IsProcessAlive(options.ParentProcessId))
                {
                    Marker("parent-lost");
                    Quit(0);
                    return;
                }
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

                // 8.35z54. "Bots need to pick heroes as well." A filled seat now takes its
                // own hero WHEN THE ORDER REACHES IT, through the same presenter press a player
                // makes, so the pick appears in rulebook p.22's sequence on every screen exactly
                // like anybody else's rather than arriving as a silent assignment at the
                // transition. First available is the brain, which is deterministic and therefore
                // replayable; a seeded filler is still 8.28b's business rather than this one's.
                //
                // The ORDER is not decided here. The model offers ChooseHero only while the order
                // is on this seat, and the host refuses an out-of-turn pick regardless, so a bot
                // that presses is a bot whose turn it is.
                PickHeroWhenOffered();
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

            // 8.35t corrective. Press(Join, ...) returns true the instant the control was OFFERED -
            // it says nothing about whether the join it triggered actually reached anyone.
            // BeginJoining runs synchronously inside that same call, so by the time Press returns,
            // a failed dial has already recorded its reason here: the interim-4 live defect was
            // exactly this - a driver that pressed successfully, by this same measure, then sat
            // doing nothing forever because nothing ever told it the join underneath had failed.
            if (lobby.LastJoinFailure != null)
            {
                Fail("test-driver-join-failed " + lobby.LastJoinFailure);
                return;
            }

            entryPressed = true;
            Marker("begin");
        }

        private static bool IsProcessAlive(int processId)
        {
            try
            {
                using (var process = System.Diagnostics.Process.GetProcessById(processId))
                {
                    return !process.HasExited;
                }
            }
            catch (ArgumentException)
            {
                // No process with that id - it already exited, and something else may even have
                // reused the id by now. Either way, its watcher is not still there.
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
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

            // See refusedThisVersion. The refusal memory is scoped to one projection version,
            // because a version move is the only thing that can make a refused command legal again.
            var version = machine.LatestProjection.Version;
            if (version != refusedVersion)
            {
                refusedThisVersion.Clear();
                refusedVersion = version;
                exhaustionReported = false;
            }

            var candidates = commands;
            if (refusedThisVersion.Count > 0)
            {
                candidates = commands.Where(value => !refusedThisVersion.Contains(value)).ToList();
                if (candidates.Count == 0)
                {
                    // Every offer this seat holds has been refused at this version. Nothing legal is
                    // left to try, so waiting for the version to move is the only honest answer - but
                    // it is said out loud once, because a silent wait and a live deadlock look
                    // identical in a log and the previous behaviour at least made noise.
                    if (!exhaustionReported)
                    {
                        exhaustionReported = true;
                        Marker("every offered command refused at version " +
                            version.ToString(CultureInfo.InvariantCulture));
                    }

                    return;
                }
            }

            // FOURTEENTH amendment, tier-1. The lookahead argument is deliberately absent: a bot
            // holding a network seat has no live session to try a move on, because its whole view is
            // the seat projection the host sends it. That is the 8.32 privacy law holding, not a gap
            // - see IBotLookahead's remarks. The policy judges by BotEvaluation and by the previews
            // the authority already publishes, which is exactly the configuration the harness
            // measured.
            var selected = BotPolicy.SelectTierOne(
                candidates, runtime.ReadModel, runtime.Seat.HeroId, botRandom)
                ?? candidates[botRandom.Next(candidates.Count)];
            var accepted = runtime.SubmitProjectedCommand(selected);
            if (!accepted)
            {
                refusedThisVersion.Add(selected);
            }

            Submit(accepted);
        }

        /// <summary>
        /// The tier-0.5 greedy-nudge layer (see the class remarks): three deterministic preferences,
        /// tried in the order below, each reading only facts already published on this seat's own
        /// <paramref name="readModel"/> and each returning <c>null</c> - "defer to random-legal" -
        /// the instant its own facts are absent. Never widens <paramref name="commands"/>, so a
        /// result here is always a member of it and therefore always as legal as random-legal was.
        /// </summary>
        /// <remarks>
        /// The order does not matter for correctness - the four sub-selectors key off disjoint
        /// <see cref="DirectorCommandKind"/> subsets of <paramref name="commands"/>, because exactly
        /// one pending decision family is ever standing at a time (<see
        /// cref="ConfiguredMatchSceneRuntime.EnumerateProjectedCommands"/>'s own shape) - but it is
        /// written defense-first, since a lethal, undefended hit is the one mistake this pass exists
        /// to prevent a practice opponent from making by pure bad luck.
        /// </remarks>
        internal static DirectorCommand SelectGreedyCommand(
            IReadOnlyList<DirectorCommand> commands,
            IMatchReadModel readModel,
            HeroId ownHeroId,
            System.Random random)
        {
            if (commands == null || commands.Count == 0 || readModel == null)
            {
                return null;
            }

            // The tier-0.5 preferences now live in Application beside the tier-1 evaluation, so both
            // tiers are one door and the harness can pit them against each other. This delegation is
            // what keeps this milestone's move honest: the tier-0.5 fixture still enters here.
            return BotPolicy.SelectGreedy(
                commands, readModel, ownHeroId, new SystemBotRandom(random));
        }

        /// <summary>
        /// The four tier-0.5 preferences, kept here as the door the tier-0.5 fixture already knocks
        /// on and delegating to <see cref="BotPolicy"/>, where they now live beside the tier-1
        /// evaluation. Deliberately thin: the fixture drives these signatures, so an unchanged
        /// fixture passing against a delegating body is the evidence that moving them changed no
        /// behaviour.
        /// </summary>
        internal static DirectorCommand PreferAttackStart(
            IReadOnlyList<DirectorCommand> commands, IMatchReadModel readModel, System.Random random) =>
            BotPolicy.PreferAttackStart(commands, readModel, new SystemBotRandom(random));

        internal static DirectorCommand PreferKillableAttackTarget(
            IReadOnlyList<DirectorCommand> commands, IMatchReadModel readModel, System.Random random) =>
            BotPolicy.PreferKillableAttackTarget(commands, readModel, new SystemBotRandom(random));

        internal static DirectorCommand PreferAdvancingMovement(
            IReadOnlyList<DirectorCommand> commands, IMatchReadModel readModel, HeroId ownHeroId,
            System.Random random) =>
            BotPolicy.PreferAdvancingMovement(commands, readModel, ownHeroId, new SystemBotRandom(random));

        internal static DirectorCommand PreferSufficientDefense(
            IReadOnlyList<DirectorCommand> commands, IMatchReadModel readModel, System.Random random) =>
            BotPolicy.PreferSufficientDefense(commands, readModel, new SystemBotRandom(random));

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

            Submit(compositionRoot.Runtime.SubmitProjectedCommand(commands[botRandom.Next(commands.Length)]));
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

        /// <summary>
        /// 8.35z54. Takes this seat's hero when rulebook p.22's order reaches it.
        /// </summary>
        /// <remarks>
        /// <para>Presses exactly what a player presses: it highlights one of the options the LOBBY
        /// MODEL is offering and then presses PICK HERO, so a bot cannot pick a hero the surface
        /// would not offer - the one-copy rule and the pick order both reach it as the greying the
        /// model already computes. This is milestone 8.35c's reusable finding applied to the lobby:
        /// an automated seat must choose from what its own surface authorizes rather than from
        /// what it can name.</para>
        /// <para>It re-presses on every frame the order is still on this seat, which costs nothing
        /// and is what makes it robust to a refusal: if the host says no - a hero taken between
        /// the model's render and the press - the next frame reads a fresh model and takes the
        /// next available one.</para>
        /// </remarks>
        private void PickHeroWhenOffered()
        {
            var model = lobby.Presenter.Model;
            if (model == null || !model.Offers(PrivateLobbyControl.ChooseHero))
            {
                return;
            }

            foreach (var option in model.Selection.Heroes)
            {
                if (!option.IsAvailable)
                {
                    continue;
                }

                lobby.Presenter.SelectedHeroIdValue = option.HeroIdValue;
                if (lobby.Presenter.Press(PrivateLobbyControl.ChooseHero, null))
                {
                    Marker("hero-picked " + option.HeroIdValue);
                }

                return;
            }
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
