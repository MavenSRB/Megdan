using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;

namespace GuardsDigital.Application.Bots
{
    /// <summary>What one conductor tick did, for a caller that wants to log or pace it.</summary>
    public sealed class BotSeatAction
    {
        public BotSeatAction(SeatId seat, string description, bool accepted, long version)
        {
            Seat = seat;
            Description = description;
            Accepted = accepted;
            Version = version;
        }

        public SeatId Seat { get; }
        public string Description { get; }
        public bool Accepted { get; }
        public long Version { get; }

        public override string ToString() =>
            Seat.Value + " " + Description + (Accepted ? " accepted" : " REFUSED") +
            " @" + Version.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// FOURTEENTH amendment (2026-09-06 amendment), tier-2. Plays one or more HOST-OWNED seats from
    /// inside the process that holds the authority.
    /// </summary>
    /// <remarks>
    /// <para><b>"Host-owned" is the whole design, and it is what makes a real ply legal here.</b> The
    /// practice bots milestones 8.35z13 and 8.35z22 shipped are spawned OS processes holding ordinary
    /// keyed NETWORK seats: their entire view is the seat projection the host sends them, other
    /// heroes' hands arrive as counts, and no command log arrives at all - so they have nothing to
    /// simulate on, and giving them something would be giving a bot six hands. That is the 8.32
    /// privacy law and this milestone does not touch it: <b>the network seat driver's behaviour is
    /// byte-for-byte unchanged, still tier-1, still without a lookahead.</b></para>
    /// <para>This conductor is a different vehicle for a different configuration. It runs in the
    /// process that already owns the authoritative <see cref="IMatchSession"/> - so the information a
    /// ply needs is information this process legitimately has, and no wire carries anything extra.
    /// No new authority is taken either: a seat this conductor plays chooses only among the commands
    /// <see cref="SeatProjection.AuthorizedCommands"/> already authorizes for that seat, and submits
    /// them through <see cref="IMatchHost.SubmitForSeat"/> exactly as a remote seat's envelope does.
    /// The privacy boundary for ACTIONS is unchanged; only the boundary for THINKING moved, and it
    /// moved to a place that was already inside it.</para>
    /// <para><b>The refusal memory is not optional and the reason is live-fire evidence.</b> A
    /// deterministic brain answers an unchanged projection with the same command every time, so a
    /// single offered-then-refused command becomes a hot infinite loop rather than a hiccup - the
    /// first Solo cohort run spun 11,000+ consecutive refusals at one frozen version and the match
    /// never advanced. The cure is not to widen the candidate set (that would submit something the
    /// seat was never offered); it is to drop the refused command FOR THAT VERSION and re-choose from
    /// what remains, because a version move is the only thing that can make a refused command legal
    /// again. The engine defect that makes this reachable - a projection offering a Defense card the
    /// rules refuse - is being corrected separately; this containment stands regardless, because a
    /// deterministic driver must not depend on every offer being feasible.</para>
    /// <para><b>Determinism.</b> One seeded <see cref="DeterministicBotRandom"/> per match, drawn by
    /// every seat in a fixed seat order, and submission identities minted from a counter rather than
    /// a GUID. So one seed names one match: replay it and the same commands land in the same order.
    /// <c>BotTierTwoTests.OneSeedReplaysByteStable</c> is the proof.</para>
    /// </remarks>
    public sealed class BotSeatConductor
    {
        private sealed class SeatState
        {
            public SeatState(SeatId seat)
            {
                Seat = seat;
            }

            public SeatId Seat { get; }
            public readonly HashSet<DirectorCommand> RefusedThisVersion = new HashSet<DirectorCommand>();
            public long RefusedVersion = -1;
            public bool ExhaustionReported;
        }

        private readonly IMatchHost host;
        private readonly List<SeatState> seats;
        private readonly IBotRandom random;
        private readonly IBotLookahead lookahead;
        private readonly BotEvaluationWeights weights;
        private readonly int nearEqualBand;
        private readonly Stopwatch stopwatch = new Stopwatch();
        private int submissionOrdinal;

        public BotSeatConductor(
            IMatchHost host,
            IEnumerable<SeatId> ownedSeats,
            int seed,
            IBotLookahead lookahead = null,
            BotEvaluationWeights weights = null,
            int nearEqualBand = BotPolicy.NearEqualBand)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            if (ownedSeats == null)
            {
                throw new ArgumentNullException(nameof(ownedSeats));
            }

            this.lookahead = lookahead;
            this.weights = weights ?? BotEvaluationWeights.Default;
            this.nearEqualBand = nearEqualBand;
            random = new DeterministicBotRandom(seed);
            seats = ownedSeats
                .Where(seat => seat.IsDefined)
                .Distinct()
                .Select(seat => new SeatState(seat))
                .ToList();
            if (seats.Count == 0)
            {
                throw new ArgumentException("A conductor needs at least one seat to play.", nameof(ownedSeats));
            }
        }

        /// <summary>The seats this conductor answers for, in the order it polls them.</summary>
        public IReadOnlyList<SeatId> Seats =>
            Array.AsReadOnly(seats.Select(state => state.Seat).ToArray());

        /// <summary>How many decisions were actually answered. Measurement, not logic.</summary>
        public int Decisions { get; private set; }

        /// <summary>Stopwatch ticks spent inside the policy. Divided by <see cref="Decisions"/>, the latency.</summary>
        public long DecisionTicks { get; private set; }

        public double MillisecondsPerDecision =>
            Decisions > 0 ? DecisionTicks * 1000.0 / Stopwatch.Frequency / Decisions : 0.0;

        /// <summary>
        /// Answers ONE decision for the first owned seat that has one, and reports what it did.
        /// Returns <c>false</c> when no owned seat is being asked anything right now, which is the
        /// ordinary state while a human seat is thinking.
        /// </summary>
        /// <remarks>
        /// One decision per call, deliberately: the caller owns the pace. A solo player watches the
        /// gap between one visible board change and the next, so the Unity driver above this calls it
        /// on a think-delay, while the headless harness calls it in a loop as fast as it likes. Neither
        /// pace changes what is chosen.
        /// </remarks>
        public bool TryPlayOne(out BotSeatAction action)
        {
            action = null;
            foreach (var state in seats)
            {
                var client = host.ClientForSeat(state.Seat);
                var projection = client?.Projection;
                if (projection == null || projection.Outcome.HasEnded || projection.IsWaitingForAnotherSeat)
                {
                    continue;
                }

                if (TryAnswerAgreement(client, projection, out action) ||
                    TryAnswerCommand(client, state, projection, out action))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A team agreement, settled by the deterministic first-candidate rule rather than by the
        /// brain.
        /// </summary>
        /// <remarks>
        /// UNCHANGED FROM TIER-1 AND DELIBERATELY SO. Every seat runs its own die, and a "preference"
        /// at a team agreement is a disagreement nobody is scripted to resolve; the first Solo cohort
        /// run hung forever proving it. Convergence, not cleverness. The near-equal band Ruling 41
        /// added is a variety lever for INDIVIDUAL choices and must never reach here - variety at a
        /// consensus point is just a deadlock with a seed.
        /// </remarks>
        private bool TryAnswerAgreement(ISeatClient client, SeatProjection projection, out BotSeatAction action)
        {
            action = null;
            var agreement = projection.TeamAgreement;
            if (agreement?.AuthorizedCommands == null || agreement.AuthorizedCommands.Count == 0)
            {
                return false;
            }

            var ownIndex = -1;
            for (var index = 0; index < agreement.Participants.Count; index++)
            {
                if (agreement.Participants[index].HeroId.Equals(projection.Seat.HeroId))
                {
                    ownIndex = index;
                    break;
                }
            }

            if (ownIndex < 0)
            {
                return false;
            }

            TeamAgreementCommand statement;
            if (agreement.Kind == TeamAgreementKind.Initiative)
            {
                if (agreement.Participants[ownIndex].HasStatement ||
                    agreement.Candidates == null || agreement.Candidates.Count == 0)
                {
                    return false;
                }

                var preferred = agreement.Candidates[0].CardInstanceId;
                statement = agreement.AuthorizedCommands.FirstOrDefault(value =>
                    value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                    value.PreferredCardInstanceId.Equals(preferred));
            }
            else if (agreement.Kind == TeamAgreementKind.MinionBattle)
            {
                // Milestone 9.29. A seat this conductor owns is a bot; every other choosing seat is
                // a human, whose picks the bots copy.
                var ownedHeroes = new HashSet<Core.Units.Heroes.HeroId>(seats
                    .Select(owned => host.ClientForSeat(owned.Seat)?.Projection)
                    .Where(owned => owned != null)
                    .Select(owned => owned.Seat.HeroId));
                statement = MinionBattlePickPolicy.NextStatement(
                    agreement, projection.Seat.HeroId, ownedHeroes.Contains);
            }
            else
            {
                return false;
            }

            if (statement == null)
            {
                return false;
            }

            var result = client.SubmitAgreement(
                new TeamAgreementSubmission(NextSubmissionId(projection.Seat.Id), projection.Version, statement));
            action = new BotSeatAction(
                projection.Seat.Id, "agreement " + statement.Kind, result != null && result.Accepted,
                projection.Version);
            return true;
        }

        private bool TryAnswerCommand(
            ISeatClient client, SeatState state, SeatProjection projection, out BotSeatAction action)
        {
            action = null;
            var authorized = projection.AuthorizedCommands;
            if (authorized == null || authorized.Count == 0)
            {
                return false;
            }

            // Scoped to one projection version, because a version move is the only thing that can
            // make a refused command legal again. See the class remarks.
            if (projection.Version != state.RefusedVersion)
            {
                state.RefusedThisVersion.Clear();
                state.RefusedVersion = projection.Version;
                state.ExhaustionReported = false;
            }

            var candidates = state.RefusedThisVersion.Count == 0
                ? authorized
                : authorized.Where(value => !state.RefusedThisVersion.Contains(value)).ToList();
            if (candidates.Count == 0)
            {
                // Every offer this seat holds has been refused at this version. Waiting for the
                // version to move is the only honest answer, and it is said once - a silent wait and
                // a live deadlock look identical in a log.
                if (!state.ExhaustionReported)
                {
                    state.ExhaustionReported = true;
                    action = new BotSeatAction(
                        state.Seat, "every offered command refused", false, projection.Version);
                    return true;
                }

                return false;
            }

            stopwatch.Restart();
            var selected = BotPolicy.SelectTierTwo(
                candidates, projection, projection.Seat.HeroId, random,
                lookahead, BotPolicyFamilies.All, weights, nearEqualBand) ??
                candidates[random.Next(candidates.Count)];
            stopwatch.Stop();
            DecisionTicks += stopwatch.ElapsedTicks;
            Decisions++;

            var result = client.Submit(
                new SeatSubmission(NextSubmissionId(state.Seat), projection.Version, selected));
            var accepted = result != null && result.Accepted;
            if (!accepted)
            {
                state.RefusedThisVersion.Add(selected);
            }

            action = new BotSeatAction(state.Seat, selected.Kind.ToString(), accepted, projection.Version);
            return true;
        }

        /// <summary>
        /// Minted from a counter, never from a GUID: a submission identity that varies between two
        /// runs of the same seed would make an otherwise identical replay diverge in the host's own
        /// idempotency record.
        /// </summary>
        private SubmissionId NextSubmissionId(SeatId seat)
        {
            submissionOrdinal++;
            return new SubmissionId(
                "bot-" + seat.Value + "-" + submissionOrdinal.ToString(CultureInfo.InvariantCulture));
        }
    }
}
