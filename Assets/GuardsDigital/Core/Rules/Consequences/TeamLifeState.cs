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

    /// <summary>
    /// The counters a restore hands back, in the shape <c>DefeatRewardRestoreState</c> established for
    /// the coin balances beside them.
    /// </summary>
    public sealed class TeamLifeRestoreState
    {
        public TeamLifeRestoreState(IEnumerable<KeyValuePair<TeamId, int>> counters)
        {
            Counters = Array.AsReadOnly((counters ?? throw new ArgumentNullException(nameof(counters)))
                .ToArray());
        }

        public IReadOnlyList<KeyValuePair<TeamId, int>> Counters { get; }
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
    /// Shaped after <c>DefeatRewardAccounting</c>'s coin balances and <c>HeroProgressionState</c>'s
    /// Levels, which are the two closest existing things: per-participant authoritative numbers,
    /// covered exactly once, ordered by id so the wire is stable.
    /// </summary>
    public sealed class TeamLifeState
    {
        private readonly Dictionary<TeamId, int> counters;

        /// <summary>Every team in the match at the same starting count.</summary>
        public TeamLifeState(MatchParticipants participants, int startingCountersPerTeam)
            : this(participants, RepeatForEveryTeam(participants, startingCountersPerTeam), false)
        {
        }

        /// <summary>
        /// Every team at its own starting count, which is what a composed setup states - the team
        /// states it produces carry one <c>LifeCount</c> each rather than one shared number.
        /// </summary>
        public TeamLifeState(
            MatchParticipants participants,
            IEnumerable<KeyValuePair<TeamId, int>> startingCounters)
            : this(participants, startingCounters, false)
        {
        }

        private TeamLifeState(
            MatchParticipants participants,
            IEnumerable<KeyValuePair<TeamId, int>> supplied,
            bool restoring)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            var declared = (supplied ?? throw new ArgumentNullException(nameof(supplied))).ToArray();
            var teamIds = participants.Teams.Select(team => team.Id).ToArray();
            if (declared.Length != teamIds.Length ||
                declared.Select(entry => entry.Key).Distinct().Count() != declared.Length ||
                declared.Any(entry => !teamIds.Any(id => id.Equals(entry.Key))))
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
        }

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

        public TeamLifeRestoreState CaptureRestoreState() => new TeamLifeRestoreState(Counters);

        public static TeamLifeState Restore(MatchParticipants participants, TeamLifeRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new TeamLifeState(participants, state.Counters, true);
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
