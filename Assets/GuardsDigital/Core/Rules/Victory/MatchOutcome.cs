using System;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.LanePush;

namespace GuardsDigital.Core.Rules.Victory
{
    /// <summary>
    /// Which of the rulebook's three victory sentences ended the match.
    ///
    /// The three are printed on two pages and reached by two different rules, and until milestone 7.7
    /// each was recorded only where it happened - p.8's two on the Lane-Push transition, p.13's on the
    /// Life counters. This enum is the one vocabulary all three answer in, so "why did the match end"
    /// has a single answer rather than one answer per rule that can end it.
    /// </summary>
    public enum MatchVictoryCause
    {
        None = 0,

        /// <summary>
        /// p.8: "If the last remaining Wave counter is flipped, the game ends immediately and the team
        /// who won this final push wins the game."
        /// </summary>
        FinalWaveFlipped = 1,

        /// <summary>
        /// p.8: "If the enemy Throne Zone itself becomes the new Battle Zone, the team who won the push
        /// wins the game."
        /// </summary>
        ThroneCaptured = 2,

        /// <summary>
        /// p.13: "If the last counter is spent, the game ends and the team with no Life counters left,
        /// loses the game."
        /// </summary>
        LifeCountersExhausted = 3
    }

    /// <summary>
    /// Why authoritative state does not describe exactly one ending.
    ///
    /// None of these is reachable in a live match: the phase stops every later command the moment an
    /// ending is recorded, so a second ending has nothing left to happen inside. They exist because a
    /// payload can still claim one, and milestone 7.7's third acceptance check asks for duplicate and
    /// conflicting winners to be rejected rather than resolved by preference.
    /// </summary>
    public enum MatchOutcomeFault
    {
        None = 0,

        /// <summary>
        /// More than one victory path claims to have ended the match. Two endings cannot both be the
        /// ending, whether or not they happen to name the same winner: the second one had no match left
        /// to end.
        /// </summary>
        ConflictingVictoryPaths = 1,

        /// <summary>
        /// The ending names a winner or a loser the participants do not resolve to exactly one team.
        /// p.8 names its winner by side and p.13 names its loser by team, and each implies the other,
        /// so an ending that cannot name both names neither.
        /// </summary>
        UndeclaredOutcomeTeams = 2,

        /// <summary>The match is recorded as ended and no victory path accounts for it.</summary>
        UnexplainedGameEnd = 3,

        /// <summary>A victory path ended the match and the match is not recorded as ended.</summary>
        UnrecordedGameEnd = 4
    }

    /// <summary>
    /// Who won, who lost, and which of the rulebook's three sentences decided it.
    ///
    /// The winner and the loser are both recorded although each page names only one of them, because
    /// each page's silence is the other page's subject: p.8 awards the game to the team that won the
    /// push and p.13 takes it from the team whose counters ran out. A reader that had to know which
    /// page produced an outcome before it could tell who won would be reading three contracts again.
    /// </summary>
    public readonly struct MatchOutcome : IEquatable<MatchOutcome>
    {
        private MatchOutcome(MatchVictoryCause cause, TeamId winningTeamId, TeamId losingTeamId)
        {
            Cause = cause;
            WinningTeamId = winningTeamId;
            LosingTeamId = losingTeamId;
        }

        /// <summary>A match that is still being played.</summary>
        public static MatchOutcome Unfinished => default;

        public static MatchOutcome Won(
            MatchVictoryCause cause, TeamId winningTeamId, TeamId losingTeamId)
        {
            if (cause == MatchVictoryCause.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cause), "An ended match names the sentence that ended it.");
            }

            if (!winningTeamId.IsDefined || !losingTeamId.IsDefined ||
                winningTeamId.Equals(losingTeamId))
            {
                throw new ArgumentException(
                    "An ended match names two different teams as its winner and its loser.");
            }

            return new MatchOutcome(cause, winningTeamId, losingTeamId);
        }

        public MatchVictoryCause Cause { get; }

        public TeamId WinningTeamId { get; }

        public TeamId LosingTeamId { get; }

        public bool HasEnded => Cause != MatchVictoryCause.None;

        public bool Equals(MatchOutcome other) =>
            Cause == other.Cause &&
            WinningTeamId.Equals(other.WinningTeamId) &&
            LosingTeamId.Equals(other.LosingTeamId);

        public override bool Equals(object obj) => obj is MatchOutcome other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Cause;
                hashCode = (hashCode * 397) ^ WinningTeamId.GetHashCode();
                return (hashCode * 397) ^ LosingTeamId.GetHashCode();
            }
        }

        public override string ToString() => HasEnded
            ? $"{WinningTeamId.Value} won ({Cause})"
            : "unfinished";
    }

    /// <summary>An outcome and whatever stops it from being one.</summary>
    public readonly struct MatchOutcomeReading
    {
        private MatchOutcomeReading(MatchOutcome outcome, MatchOutcomeFault fault)
        {
            Outcome = outcome;
            Fault = fault;
        }

        internal static MatchOutcomeReading Coherent(MatchOutcome outcome) =>
            new MatchOutcomeReading(outcome, MatchOutcomeFault.None);

        internal static MatchOutcomeReading Faulted(MatchOutcomeFault fault)
        {
            if (fault == MatchOutcomeFault.None)
            {
                throw new ArgumentOutOfRangeException(nameof(fault));
            }

            return new MatchOutcomeReading(MatchOutcome.Unfinished, fault);
        }

        public MatchOutcome Outcome { get; }

        public MatchOutcomeFault Fault { get; }

        public bool IsCoherent => Fault == MatchOutcomeFault.None;
    }

    /// <summary>
    /// The one place the match's ending is decided, milestone 7.7.
    ///
    /// It derives rather than records, for the reason milestone 7.3 gave for the unlocked Ultimate and
    /// milestone 7.4 repeated for p.13's losing team: every fact this needs is already authoritative
    /// and already persisted. The Lane-Push transition holds which of p.8's two sentences fired and the
    /// side that won the push, and the Life counters hold p.13's team at zero. A fourth record beside
    /// them could only agree with those two or be wrong about them, and it would be the copy a restore
    /// had to be trusted about.
    ///
    /// What the single record costs is the same thing it cost milestone 7.3: the reader owes the check
    /// the record would otherwise have carried, which is why this answers with a fault rather than
    /// throwing. <see cref="Snapshots.AuthoritativeSnapshotSerializer"/> turns that fault into a typed
    /// rejection, and the live authorities assert against it instead, because a live match that
    /// produced one would be an engine defect rather than a rules outcome.
    /// </summary>
    public static class MatchOutcomeQuery
    {
        /// <summary>
        /// Reads the ending out of the two authorities that hold its facts.
        ///
        /// <paramref name="lanePush"/> is absent in a composition with no Lane to push, which leaves
        /// p.13's ending as the only one such a match can reach.
        /// </summary>
        public static MatchOutcomeReading Read(
            MatchParticipants participants,
            LanePushTransitionAuthority lanePush,
            TeamLifeState teamLives,
            bool gameIsRecordedAsEnded) =>
            Read(
                participants,
                lanePush?.GameEndingCause ?? LanePushGameEndingCause.None,
                lanePush?.WinningSide ?? TeamSide.None,
                teamLives,
                gameIsRecordedAsEnded);

        /// <summary>
        /// The same reading over the two facts themselves, so a caller holding a restored payload's
        /// values can ask before it has built an authority to hold them.
        /// </summary>
        public static MatchOutcomeReading Read(
            MatchParticipants participants,
            LanePushGameEndingCause lanePushCause,
            TeamSide lanePushWinningSide,
            TeamLifeState teamLives,
            bool gameIsRecordedAsEnded)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (teamLives == null) throw new ArgumentNullException(nameof(teamLives));

            // p.13's ending is asked for as "how many teams have run out", not "has one", because two
            // teams at zero is one of the duplicate winners this milestone rejects and
            // `TeamLifeState.EliminatedTeamId` answers with the first of them.
            var eliminated = teamLives.Counters
                .Where(entry => entry.Value == 0)
                .Select(entry => entry.Key)
                .ToArray();
            var lifeEnding = eliminated.Length > 0;
            var pushEnding = lanePushCause != LanePushGameEndingCause.None;
            if ((pushEnding && lifeEnding) || eliminated.Length > 1)
            {
                return MatchOutcomeReading.Faulted(MatchOutcomeFault.ConflictingVictoryPaths);
            }

            if (!pushEnding && !lifeEnding)
            {
                return gameIsRecordedAsEnded
                    ? MatchOutcomeReading.Faulted(MatchOutcomeFault.UnexplainedGameEnd)
                    : MatchOutcomeReading.Coherent(MatchOutcome.Unfinished);
            }

            if (!gameIsRecordedAsEnded)
            {
                return MatchOutcomeReading.Faulted(MatchOutcomeFault.UnrecordedGameEnd);
            }

            return pushEnding
                ? ReadPushEnding(participants, lanePushCause, lanePushWinningSide)
                : ReadLifeEnding(participants, eliminated[0]);
        }

        private static MatchOutcomeReading ReadPushEnding(
            MatchParticipants participants, LanePushGameEndingCause cause, TeamSide winningSide)
        {
            var winners = participants.Teams
                .Where(team => team.Side == winningSide)
                .Select(team => team.Id)
                .ToArray();
            var losers = participants.Teams
                .Where(team => team.Side != winningSide)
                .Select(team => team.Id)
                .ToArray();
            if (winningSide == TeamSide.None || winners.Length != 1 || losers.Length != 1)
            {
                return MatchOutcomeReading.Faulted(MatchOutcomeFault.UndeclaredOutcomeTeams);
            }

            return MatchOutcomeReading.Coherent(MatchOutcome.Won(
                cause == LanePushGameEndingCause.FinalWaveFlipped
                    ? MatchVictoryCause.FinalWaveFlipped
                    : MatchVictoryCause.ThroneCaptured,
                winners[0],
                losers[0]));
        }

        private static MatchOutcomeReading ReadLifeEnding(
            MatchParticipants participants, TeamId losingTeamId)
        {
            var survivors = participants.Teams
                .Where(team => !team.Id.Equals(losingTeamId))
                .Select(team => team.Id)
                .ToArray();
            if (survivors.Length != 1 ||
                !participants.Teams.Any(team => team.Id.Equals(losingTeamId)))
            {
                return MatchOutcomeReading.Faulted(MatchOutcomeFault.UndeclaredOutcomeTeams);
            }

            return MatchOutcomeReading.Coherent(MatchOutcome.Won(
                MatchVictoryCause.LifeCountersExhausted, survivors[0], losingTeamId));
        }
    }
}
