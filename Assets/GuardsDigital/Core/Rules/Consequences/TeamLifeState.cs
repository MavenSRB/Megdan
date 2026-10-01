using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.Consequences
{
    public enum TeamLifeSpendRejectionReason
    {
        None = 0,
        TeamNotInMatch = 1,
        AmountNotPositive = 2,

        /// <summary>
        /// The team already has no counters left. p.13 ends the game the moment the last one is spent,
        /// so a second spend against an eliminated team is a defect in the caller's ordering rather
        /// than a deeper hole to dig.
        /// </summary>
        TeamAlreadyEliminated = 3
    }

    /// <summary>
    /// One p.13 Life spend: what was asked for, what the team actually had to give, and whether that
    /// took its last counter.
    ///
    /// <see cref="Requested"/> and <see cref="Spent"/> differ only when the team held fewer counters
    /// than the defeated hero's Tier asked for. p.13 does not describe that case in words, and the
    /// clamp is the only reading its own next sentence permits: "If the last counter is spent, the game
    /// ends", so the spend that empties the pile is a spend that happened, not one refused for being
    /// too large. Both numbers are recorded rather than only the effective one, because the difference
    /// is exactly the fact a reader would otherwise have to reconstruct.
    /// </summary>
    public readonly struct TeamLifeSpend
    {
        public TeamLifeSpend(
            TeamId teamId,
            int requested,
            int spent,
            int countersBefore,
            int countersAfter)
        {
            if (!teamId.IsDefined)
            {
                throw new ArgumentException("A Life spend names the team that spent.", nameof(teamId));
            }

            if (requested <= 0 || spent <= 0 || spent > requested)
            {
                throw new ArgumentException("A Life spend gives at least one counter and never more than asked.");
            }

            if (countersBefore <= 0 || countersAfter != countersBefore - spent || countersAfter < 0)
            {
                throw new ArgumentException("A Life spend preserves its declared counter transition.");
            }

            TeamId = teamId;
            Requested = requested;
            Spent = spent;
            CountersBefore = countersBefore;
            CountersAfter = countersAfter;
        }

        public TeamId TeamId { get; }

        public int Requested { get; }

        public int Spent { get; }

        public int CountersBefore { get; }

        public int CountersAfter { get; }

        /// <summary>p.13: "If the last counter is spent, the game ends".</summary>
        public bool SpentLastCounter => CountersAfter == 0;
    }

    public sealed class TeamLifeSpendResult
    {
        private TeamLifeSpendResult(bool accepted, TeamLifeSpendRejectionReason reason, TeamLifeSpend spend)
        {
            Accepted = accepted;
            RejectionReason = reason;
            Spend = spend;
        }

        public bool Accepted { get; }

        public TeamLifeSpendRejectionReason RejectionReason { get; }

        public TeamLifeSpend Spend { get; }

        public static TeamLifeSpendResult Accept(TeamLifeSpend spend) =>
            new TeamLifeSpendResult(true, TeamLifeSpendRejectionReason.None, spend);

        public static TeamLifeSpendResult Reject(TeamLifeSpendRejectionReason reason)
        {
            if (reason == TeamLifeSpendRejectionReason.None)
            {
                throw new ArgumentException("A rejected Life spend requires a reason.", nameof(reason));
            }

            return new TeamLifeSpendResult(false, reason, default);
        }
    }

    public enum TeamLifeRegainRejectionReason
    {
        None = 0,
        TeamNotInMatch = 1,
        AmountNotPositive = 2,

        /// <summary>
        /// The team holds every counter it began with. GY-Q11 (user, 2026-10-01): Energy Drain's "Your
        /// team regains 1 <b>spent</b> Life counter" regains only a counter the team has spent, so a
        /// team at its starting Lives regains nothing - never above its starting Lives.
        /// </summary>
        NoSpentCounter = 3,

        /// <summary>
        /// The team has no counters left. p.13 has already ended the game, so nothing regains one.
        /// </summary>
        TeamAlreadyEliminated = 4
    }

    /// <summary>
    /// Milestone 9.30f. One regained Life counter - the inverse of <see cref="TeamLifeSpend"/>, and
    /// the first rule in this build that raises a team's counters (Gydion's Energy Drain).
    /// </summary>
    public readonly struct TeamLifeRegain
    {
        public TeamLifeRegain(TeamId teamId, int requested, int regained, int countersBefore, int countersAfter)
        {
            if (!teamId.IsDefined)
            {
                throw new ArgumentException("A Life regain names the team that regained.", nameof(teamId));
            }

            if (requested <= 0 || regained <= 0 || regained > requested ||
                countersBefore <= 0 || countersAfter != countersBefore + regained)
            {
                throw new ArgumentException("A Life regain takes back at least one spent counter, never more than asked.");
            }

            TeamId = teamId;
            Requested = requested;
            Regained = regained;
            CountersBefore = countersBefore;
            CountersAfter = countersAfter;
        }

        public TeamId TeamId { get; }

        public int Requested { get; }

        public int Regained { get; }

        public int CountersBefore { get; }

        public int CountersAfter { get; }
    }

    public sealed class TeamLifeRegainResult
    {
        private TeamLifeRegainResult(bool accepted, TeamLifeRegainRejectionReason reason, TeamLifeRegain regain)
        {
            Accepted = accepted;
            RejectionReason = reason;
            Regain = regain;
        }

        public bool Accepted { get; }

        public TeamLifeRegainRejectionReason RejectionReason { get; }

        public TeamLifeRegain Regain { get; }

        public static TeamLifeRegainResult Accept(TeamLifeRegain regain) =>
            new TeamLifeRegainResult(true, TeamLifeRegainRejectionReason.None, regain);

        public static TeamLifeRegainResult Reject(TeamLifeRegainRejectionReason reason)
        {
            if (reason == TeamLifeRegainRejectionReason.None)
            {
                throw new ArgumentException("A rejected Life regain requires a reason.", nameof(reason));
            }

            return new TeamLifeRegainResult(false, reason, default);
        }
    }

    /// <summary>
    /// The counters a restore hands back, in the shape <c>HeroCoinLedgerRestoreState</c> established for
    /// the coin balances beside them.
    ///
    /// <para><b>Milestone 9.30f: and the counters each team BEGAN with.</b> A regain takes back only a
    /// SPENT counter (GY-Q11), which is the difference between the two, so a restore that carried only
    /// the remaining counters would come back unable to tell a full team from one that had spent
    /// three. Required rather than defaulted, so every restore site must say what the match began
    /// with (snapshot v52).</para>
    /// </summary>
    public sealed class TeamLifeRestoreState
    {
        public TeamLifeRestoreState(
            IEnumerable<KeyValuePair<TeamId, int>> counters,
            IEnumerable<KeyValuePair<TeamId, int>> startingCounters)
        {
            Counters = Array.AsReadOnly((counters ?? throw new ArgumentNullException(nameof(counters)))
                .ToArray());
            StartingCounters = Array.AsReadOnly(
                (startingCounters ?? throw new ArgumentNullException(nameof(startingCounters))).ToArray());
        }

        public IReadOnlyList<KeyValuePair<TeamId, int>> Counters { get; }

        /// <summary>Milestone 9.30f. The counters each team began the match with.</summary>
        public IReadOnlyList<KeyValuePair<TeamId, int>> StartingCounters { get; }
    }

    /// <summary>
    /// Every team's remaining Life counters, as authoritative match state.
    ///
    /// Rulebook p.13 is the only rule that changes one - "Defeated hero's team spends the number of
    /// Life counters, equal to that hero's lowest card Tier. If the last counter is spent, the game
    /// ends and the team with no Life counters left, loses the game" - and milestone 7.4 is where that
    /// spend first happens, so this is where the number stops being a setup constant. Before it, the
    /// count existed only as <c>QuickGameTeamSetupState.LifeCount</c>, which the HUD renderer printed
    /// and nothing could change.
    ///
    /// The starting count is supplied rather than declared here, because it is a setup decision: the
    /// prototype baseline in <c>RULES_LEDGER.md</c> records five per team for the official pitch setup,
    /// and the staged 1v1 fixture composes four. Whichever it is, exactly one record of it exists - the
    /// composed setup - and this state is initialised from that record rather than from a constant that
    /// could disagree with it.
    ///
    /// Shaped after <c>HeroCoinLedger</c>'s coin balances and <c>HeroProgressionState</c>'s
    /// Levels, which are the two closest existing things: per-participant authoritative numbers,
    /// covered exactly once, ordered by id so the wire is stable.
    /// </summary>
    public sealed class TeamLifeState
    {
        private readonly Dictionary<TeamId, int> counters;

        /// <summary>
        /// Milestone 9.30f. The counters each team began the match with - the composed setup's, or the
        /// restored payload's record of it - so a regain can tell a SPENT counter from one never held.
        /// </summary>
        private readonly Dictionary<TeamId, int> starting;

        /// <summary>Every team in the match at the same starting count.</summary>
        public TeamLifeState(MatchParticipants participants, int startingCountersPerTeam)
            : this(participants, RepeatForEveryTeam(participants, startingCountersPerTeam), null, false)
        {
        }

        /// <summary>
        /// Every team at its own starting count, which is what a composed setup states - the team
        /// states it produces carry one <c>LifeCount</c> each rather than one shared number.
        /// </summary>
        public TeamLifeState(
            MatchParticipants participants,
            IEnumerable<KeyValuePair<TeamId, int>> startingCounters)
            : this(participants, startingCounters, null, false)
        {
        }

        private TeamLifeState(
            MatchParticipants participants,
            IEnumerable<KeyValuePair<TeamId, int>> supplied,
            IEnumerable<KeyValuePair<TeamId, int>> suppliedStarting,
            bool restoring)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            var declared = (supplied ?? throw new ArgumentNullException(nameof(supplied))).ToArray();
            var teamIds = participants.Teams.Select(team => team.Id).ToArray();
            if (!CoversEveryTeamOnce(declared, teamIds))
            {
                throw new ArgumentException(
                    "Life counters must cover every participating team exactly once.", nameof(supplied));
            }

            // A match that begins with a team at zero has already been lost before its first turn, which
            // is a composition defect rather than a legal setup; a restore is the one caller allowed to
            // present a zero, because p.13 produces one and the payload has to be able to say so.
            var floor = restoring ? 0 : 1;
            if (declared.Any(entry => entry.Value < floor))
            {
                throw new ArgumentException(
                    restoring
                        ? "Restored Life counters must not be negative."
                        : "A match begins with at least one Life counter per team.",
                    nameof(supplied));
            }

            counters = declared.ToDictionary(entry => entry.Key, entry => entry.Value);

            // A composed match begins with what it holds; a restore says what it began with, and that
            // record is refused where no legal match could have produced it - a team beginning with
            // nothing, or holding more than it began with (nothing raises a team above its start).
            var start = suppliedStarting?.ToArray() ?? declared;
            if (!CoversEveryTeamOnce(start, teamIds))
            {
                throw new ArgumentException(
                    "Starting Life counters must cover every participating team exactly once.",
                    nameof(suppliedStarting));
            }

            starting = start.ToDictionary(entry => entry.Key, entry => entry.Value);
            if (starting.Values.Any(value => value < 1) ||
                counters.Any(entry => entry.Value > starting[entry.Key]))
            {
                throw new ArgumentException(
                    "A team begins with at least one Life counter and never holds more than it began with.",
                    nameof(suppliedStarting));
            }
        }

        private static bool CoversEveryTeamOnce(
            IReadOnlyList<KeyValuePair<TeamId, int>> entries, IReadOnlyList<TeamId> teamIds) =>
            entries.Count == teamIds.Count &&
            entries.Select(entry => entry.Key).Distinct().Count() == entries.Count &&
            entries.All(entry => teamIds.Any(id => id.Equals(entry.Key)));

        /// <summary>Every team's remaining counters, ordered by team id so the order is a fact.</summary>
        public IReadOnlyList<KeyValuePair<TeamId, int>> Counters => Array.AsReadOnly(
            counters.OrderBy(entry => entry.Key.Value, StringComparer.Ordinal).ToArray());

        /// <summary>
        /// p.13's losing team, or an undefined id when every team still has a counter. It is derived
        /// rather than recorded, for milestone 7.3's reason: a team at zero counters <em>is</em> the
        /// fact, and a second flag beside it could only agree with the counters or be wrong about them.
        /// </summary>
        public TeamId EliminatedTeamId
        {
            get
            {
                foreach (var entry in Counters)
                {
                    if (entry.Value == 0)
                    {
                        return entry.Key;
                    }
                }

                return default;
            }
        }

        public bool HasEliminatedTeam => EliminatedTeamId.IsDefined;

        public int GetCounters(TeamId teamId)
        {
            if (!teamId.IsDefined || !counters.TryGetValue(teamId, out var remaining))
            {
                throw new KeyNotFoundException($"Unknown team ID '{teamId}'.");
            }

            return remaining;
        }

        /// <summary>Milestone 9.30f. Every team's starting counters, ordered by team id.</summary>
        public IReadOnlyList<KeyValuePair<TeamId, int>> StartingCounters => Array.AsReadOnly(
            starting.OrderBy(entry => entry.Key.Value, StringComparer.Ordinal).ToArray());

        /// <summary>Milestone 9.30f. The counters this team began the match with.</summary>
        public int GetStartingCounters(TeamId teamId)
        {
            if (!teamId.IsDefined || !starting.TryGetValue(teamId, out var begun))
            {
                throw new KeyNotFoundException($"Unknown team ID '{teamId}'.");
            }

            return begun;
        }

        /// <summary>
        /// Milestone 9.30f. How many of its counters this team has spent - what a regain may take back.
        /// </summary>
        public int GetSpentCounters(TeamId teamId) => GetStartingCounters(teamId) - GetCounters(teamId);

        public TeamLifeRestoreState CaptureRestoreState() =>
            new TeamLifeRestoreState(Counters, StartingCounters);

        public static TeamLifeState Restore(MatchParticipants participants, TeamLifeRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new TeamLifeState(participants, state.Counters, state.StartingCounters, true);
        }

        /// <summary>
        /// Milestone 9.30f. The team takes back up to <paramref name="amount"/> of the counters it has
        /// SPENT - Gydion's Energy Drain, "Your team regains 1 spent [Life] Life counter".
        ///
        /// <para>GY-Q11 (user, 2026-10-01): only a spent counter is regained, so a team at its starting
        /// Lives regains nothing and a regain never raises a team above where it began. A team at zero
        /// has already lost (p.13), so nothing is regained there either. The regain is clamped to what
        /// was spent, as <see cref="TrySpend"/> is clamped to what is left.</para>
        /// </summary>
        public TeamLifeRegainResult TryRegainSpent(TeamId teamId, int amount)
        {
            if (!teamId.IsDefined || !counters.ContainsKey(teamId))
            {
                return TeamLifeRegainResult.Reject(TeamLifeRegainRejectionReason.TeamNotInMatch);
            }

            if (amount <= 0)
            {
                return TeamLifeRegainResult.Reject(TeamLifeRegainRejectionReason.AmountNotPositive);
            }

            var before = counters[teamId];
            if (before == 0)
            {
                return TeamLifeRegainResult.Reject(TeamLifeRegainRejectionReason.TeamAlreadyEliminated);
            }

            var spent = starting[teamId] - before;
            if (spent <= 0)
            {
                return TeamLifeRegainResult.Reject(TeamLifeRegainRejectionReason.NoSpentCounter);
            }

            var regained = Math.Min(amount, spent);
            counters[teamId] = before + regained;
            return TeamLifeRegainResult.Accept(
                new TeamLifeRegain(teamId, amount, regained, before, before + regained));
        }

        public TeamLifeSpendResult TrySpend(TeamId teamId, int amount)
        {
            if (!teamId.IsDefined || !counters.ContainsKey(teamId))
            {
                return TeamLifeSpendResult.Reject(TeamLifeSpendRejectionReason.TeamNotInMatch);
            }

            if (amount <= 0)
            {
                return TeamLifeSpendResult.Reject(TeamLifeSpendRejectionReason.AmountNotPositive);
            }

            var before = counters[teamId];
            if (before == 0)
            {
                return TeamLifeSpendResult.Reject(TeamLifeSpendRejectionReason.TeamAlreadyEliminated);
            }

            var spent = Math.Min(amount, before);
            counters[teamId] = before - spent;
            return TeamLifeSpendResult.Accept(
                new TeamLifeSpend(teamId, amount, spent, before, before - spent));
        }

        private static IEnumerable<KeyValuePair<TeamId, int>> RepeatForEveryTeam(
            MatchParticipants participants,
            int startingCountersPerTeam)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            return participants.Teams
                .Select(team => new KeyValuePair<TeamId, int>(team.Id, startingCountersPerTeam))
                .ToArray();
        }
    }
}
