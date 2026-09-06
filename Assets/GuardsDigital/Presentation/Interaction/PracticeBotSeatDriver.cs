using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Replay;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Milestone 8.35z26 (the 2026-09-06 amendment). The command line that seats HOST-OWNED
    /// practice bots in the local, no-lobby practice composition.
    /// </summary>
    /// <remarks>
    /// <para>Deliberately its own vocabulary rather than a mode on
    /// <see cref="PrivateSeatDriverOptions"/>, for the reason that one is already its own vocabulary
    /// rather than a mode on the rehearsal driver's: two different things answering one flag is how a
    /// CI probe and a shipped feature end up sharing a defect. These seats are not spawned, not
    /// networked, and hold no session key - there is nothing for the two flags to have in common.</para>
    /// <para><b>Opt-in, and absent by default.</b> A build launched normally attaches nothing, so the
    /// Director's own local composition behaves exactly as it did. That is not caution for its own
    /// sake: the Director runtime is the surface every Play Mode test drives, and a driver that
    /// seated itself would change all of them.</para>
    /// </remarks>
    internal sealed class PracticeBotSeatDriverOptions
    {
        public const string RoleFlag = "-guardsPracticeBots";
        public const string SeatsFlag = "-guardsPracticeBotSeats";
        public const string SeedFlag = "-guardsPracticeBotSeed";
        public const string ThinkSecondsFlag = "-guardsPracticeBotThinkSeconds";

        /// <summary>
        /// The same watchable pace the spawned practice drivers use, and for the same reason: a solo
        /// player follows the gap between one visible board change and the next.
        /// </summary>
        public const double DefaultThinkSeconds = PrivateSeatDriverOptions.DefaultThinkSeconds;

        public IReadOnlyList<SeatId> Seats { get; private set; }
        public int Seed { get; private set; }
        public double ThinkSeconds { get; private set; }

        public static bool TryParse(string[] args, out PracticeBotSeatDriverOptions result, out string error)
        {
            result = null;
            error = string.Empty;
            args = args ?? Array.Empty<string>();
            if (Array.IndexOf(args, RoleFlag) < 0)
            {
                return false;
            }

            string seats = null;
            var seedText = "0";
            var thinkSecondsText = DefaultThinkSeconds.ToString(CultureInfo.InvariantCulture);
            for (var index = 0; index < args.Length; index++)
            {
                var argument = args[index] ?? string.Empty;
                if (argument == SeatsFlag)
                {
                    seats = Take(args, ref index);
                }
                else if (argument == SeedFlag)
                {
                    seedText = Take(args, ref index);
                }
                else if (argument == ThinkSecondsFlag)
                {
                    thinkSecondsText = Take(args, ref index);
                }
            }

            // Named seats, never inferred. "Every seat but the player's" would need this driver to
            // know which hero the player intends to run, which nothing here publishes - and guessing
            // it would produce a practice match that plays the player's own hero for them.
            var parsed = (seats ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Select(value => new SeatId(value))
                .ToArray();
            if (parsed.Length == 0)
            {
                error = RoleFlag + " requires " + SeatsFlag + " with a comma-separated list of seats";
                return true;
            }

            if (!double.TryParse(
                    thinkSecondsText, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var thinkSeconds) || thinkSeconds < 0)
            {
                error = ThinkSecondsFlag + " must be a non-negative number of seconds";
                return true;
            }

            int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed);
            result = new PracticeBotSeatDriverOptions
            {
                Seats = parsed,
                Seed = seed,
                ThinkSeconds = thinkSeconds
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
    /// Milestone 8.35z26. Runs <see cref="BotSeatConductor"/> inside the process that holds
    /// the authority, at a pace a person can watch.
    /// </summary>
    /// <remarks>
    /// <para><b>This component holds no game logic at all</b>, deliberately: everything that decides
    /// anything lives in <see cref="BotSeatConductor"/> and <see cref="BotPolicy"/>, in Application,
    /// where the headless harness measures it. What is left here is a clock and a null check, which
    /// is the most a Unity component this milestone cannot launch Unity to test should be trusted
    /// with.</para>
    /// <para>The probe is the SHADOW one. The live session this driver can see is the real match a
    /// person is playing, and a probe that mutated it would be risking that match against a bot's
    /// opinion of a candidate move - see <see cref="ShadowSessionBotLookahead"/> for the measured
    /// hazard that rules out.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PracticeBotSeatDriver : MonoBehaviour
    {
        private MatchSceneCompositionRoot compositionRoot;
        private PracticeBotSeatDriverOptions options;
        private BotSeatConductor conductor;
        private DateTime nextActionUtc;
        private bool stopped;

        internal static bool TryParseRequested(
            string[] args, out PracticeBotSeatDriverOptions parsed, out string error)
        {
            var requested = args != null &&
                Array.IndexOf(args, PracticeBotSeatDriverOptions.RoleFlag) >= 0;
            if (!requested)
            {
                parsed = null;
                error = string.Empty;
                return false;
            }

            if (!PracticeBotSeatDriverOptions.TryParse(args, out parsed, out error))
            {
                parsed = null;
            }

            return true;
        }

        internal void Configure(MatchSceneCompositionRoot root, PracticeBotSeatDriverOptions driverOptions)
        {
            compositionRoot = root;
            options = driverOptions;
            UnityEngine.Application.runInBackground = true;
        }

        private void Update()
        {
            if (stopped || options == null)
            {
                return;
            }

            var runtime = compositionRoot?.Runtime;
            if (runtime == null)
            {
                return;
            }

            // Milestone 8.35z43. Update reads the five things the tick needs off the scene
            // runtime and does nothing else, so everything below this line can be driven without a
            // composed Unity scene. That is not a testing convenience bolted on: this component's own
            // contract is that it holds no game logic, and the seam is where that claim becomes
            // checkable. Milestone 8.35z26 shipped it with no durable coverage at all, and its FIRST
            // EVER launch found an uncaught per-frame throw out of exactly this path.
            Tick(
                runtime.Seat,
                runtime.Host,
                runtime.Session,
                runtime.InitialState,
                runtime.ReadModel.Outcome.HasEnded);
        }

        /// <summary>
        /// One frame's work, given what the scene runtime would have supplied. The whole of the
        /// component's behaviour; <see cref="Update"/> is the adapter that reads these five values.
        /// </summary>
        internal void Tick(
            MatchSeat seat,
            IMatchHost host,
            IMatchSession session,
            MatchSessionInitialState initialState,
            bool matchHasEnded)
        {
            if (stopped || options == null)
            {
                return;
            }

            if (conductor == null && !TryCompose(seat, host, session, initialState))
            {
                return;
            }

            if (matchHasEnded || DateTime.UtcNow < nextActionUtc)
            {
                return;
            }

            if (!conductor.TryPlayOne(out var action))
            {
                return;
            }

            Debug.Log("GUARDS_PRACTICEBOT: " + action);
            if (action.Accepted)
            {
                // After the move, not before it: the pace a person watches is the gap between one
                // visible board change and the next, and pausing before a refused-and-retried
                // submission would only look slower.
                nextActionUtc = DateTime.UtcNow.AddSeconds(options.ThinkSeconds);
            }
        }

        /// <summary>Whether this driver has seated itself. For the fixture, which cannot see a field.</summary>
        internal bool HasSeated => conductor != null;

        /// <summary>Whether a refusal has stopped this driver for good.</summary>
        internal bool HasStopped => stopped;

        private bool TryCompose(
            MatchSeat seat,
            IMatchHost host,
            IMatchSession session,
            MatchSessionInitialState initialState)
        {
            if (seat != null)
            {
                // A seat-scoped composition is a NETWORK client: its host is frozen at the opening
                // position and its view is the projection the real host sends it. Nothing here is
                // host-owned, so there is nothing for this driver to own - and pretending otherwise
                // is exactly the privacy line the amendment kept out of scope.
                Fail("practice bots require the local, seat-less composition");
                return false;
            }

            // Milestone 8.35z26, found by this driver's FIRST EVER launch under Unity and fixed
            // there. A seat this match does not bind is a malformed request, and this component's
            // whole contract is that a malformed request fails loudly rather than quietly playing
            // nobody's seats. It did not: the conductor accepts any non-empty seat id, so an
            // unbound one survived composition, logged "seated", and then threw
            // ArgumentException("No seat '<id>' is bound in this match.") out of Update() on the
            // first tick that asked the host for its projection - once per frame, uncaught, with
            // the honest cause buried under the exception. A person naming the wrong seat on the
            // command line is the expected way to reach this. MatchSeatBinding.Find answers null
            // for an unknown seat precisely so a caller can refuse instead of throwing, so the
            // check is made here, before anything is composed, and it names what IS bound.
            var bound = host.Seats;
            var unknown = options.Seats
                .Where(requested => bound.Find(requested) == null)
                .Select(requested => requested.Value)
                .ToArray();
            if (unknown.Length > 0)
            {
                Fail(
                    "no seat named " + string.Join(", ", unknown) + " is bound in this match; " +
                    "this match binds " +
                    string.Join(", ", bound.Seats.Select(known => known.Id.Value)));
                return false;
            }

            // Built through the Application factory, never by constructing a session here: the
            // authority is composed inside the Application boundary, and a presentation component
            // that could build its own session could build a second authority. The conformance suite
            // enforces this by scanning source TEXT, so even naming the constructor in a comment
            // fails it - which is how this comment came to be worded around it.
            var lookahead = initialState == null
                ? null
                : ShadowSessionBotLookahead.ForMatch(
                    session, initialState, BotEvaluationWeights.Default);
            try
            {
                conductor = new BotSeatConductor(
                    host, options.Seats, options.Seed, lookahead, BotEvaluationWeights.Default);
            }
            catch (ArgumentException failure)
            {
                Fail(failure.Message);
                return false;
            }

            Debug.Log(
                "GUARDS_PRACTICEBOT: seated " +
                string.Join(", ", conductor.Seats.Select(owned => owned.Value)) +
                " seed=" + options.Seed.ToString(CultureInfo.InvariantCulture) +
                " lookahead=" + (lookahead != null));
            return true;
        }

        private void Fail(string reason)
        {
            stopped = true;
            Debug.LogError("GUARDS_PRACTICEBOT: FAIL " + reason);
        }
    }
}
