using System;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Units.Heroes;

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
        LifeCountersExhausted = 3,

        /// <summary>
        /// Milestone 8.39g. Cutter's A Fistful of Coins: "If you have 13 or more coins, you alone win
        /// the game."
        ///
        /// <para><b>The first ending in this build that is not a team sentence, and the user settled
        /// that reading on 2026-09-11 with the contract cost stated.</b> The three causes above are
        /// rulebook sentences about teams, and every one of them can be re-derived on restore from
        /// state that was already authoritative - the Lane-Push transition, and p.13's counters. This
        /// one names a HERO, and its winning fact has no other home: thirteen coins is not a hero who
        /// won, because coins are farmed by everyone and spent back down at p.15's Level Up. What won
        /// the match is that this hero RESOLVED that branch while holding thirteen or more, and
        /// nothing else in the match records it. So it is written, and writing it is what moved the
        /// snapshot schema at this milestone.</para>
        ///
        /// <para><b>"Alone" is load-bearing.</b> In 2v2 and 3v3 the winner has team-mates the printed
        /// sentence does not mention, and they lose with everybody else - see
        /// <see cref="MatchOutcome.WinningHeroId"/>. It is the first ending in this build where two
        /// heroes on one team do not share a result.</para>
        /// </summary>
        CoinHoardClaimed = 4,

        /// <summary>
        /// Milestone 9.30g. Gydion's Wish: "After you cast the "Wish" spell three times your team wins
        /// the game." A card sentence that names a TEAM, so it ends the match as the three rulebook
        /// endings do - one winning team, one losing team - while its winning fact, like
        /// <see cref="CoinHoardClaimed"/>'s, has no other home and is recorded
        /// (<c>RoundCursorRestoreState.TeamVictoryTeamId</c>). GY-Q10 (user, 2026-10-01): it is taken
        /// once the third Wish's whole action, its inner spell included, has resolved, and an ending
        /// the inner spell already caused stands.
        /// </summary>
        WishesCast = 5
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
        UnrecordedGameEnd = 4,

        /// <summary>
        /// Milestone 8.39g. A solo ending names no hero, or names a hero this match does not have.
        ///
        /// <para>It is a fifth fault rather than a widening of <see cref="UndeclaredOutcomeTeams"/>,
        /// and deliberately so: that one exists to catch a team ending that does not resolve to one
        /// team each way, and loosening it to admit an ending with no teams at all would have removed
        /// the guard for the three causes it was written for. The two faults are asked of different
        /// endings and neither has been made weaker.</para>
        /// </summary>
        UndeclaredSoloWinner = 5
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
        private MatchOutcome(
            MatchVictoryCause cause, TeamId winningTeamId, TeamId losingTeamId, HeroId winningHeroId)
        {
            Cause = cause;
            WinningTeamId = winningTeamId;
            LosingTeamId = losingTeamId;
            WinningHeroId = winningHeroId;
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

            return new MatchOutcome(cause, winningTeamId, losingTeamId, default);
        }

        /// <summary>
        /// An ending with one hero as its winner and everybody else - team-mates included - as its
        /// losers. Milestone 8.39g, and Cutter's A Fistful of Coins is the only sentence that reaches
        /// it.
        ///
        /// <para><b>It names no teams at all, and that is the reading rather than an omission.</b>
        /// "You alone win the game" leaves the winner's team-mates on the losing side of it, so there
        /// is no winning TEAM to name and no single losing team either - every team has a loser in it.
        /// A reader that wants a side must ask <see cref="HasSoloWinner"/> first;
        /// <see cref="WinningTeamId"/> and <see cref="LosingTeamId"/> are the named absence here, in
        /// the shape this codebase uses everywhere for a fact that does not apply.</para>
        /// </summary>
        public static MatchOutcome WonAlone(MatchVictoryCause cause, HeroId winningHeroId)
        {
            if (cause == MatchVictoryCause.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cause), "An ended match names the sentence that ended it.");
            }

            if (!winningHeroId.IsDefined)
            {
                throw new ArgumentException(
                    "A solo ending names the one hero who won it.", nameof(winningHeroId));
            }

            return new MatchOutcome(cause, default, default, winningHeroId);
        }

        public MatchVictoryCause Cause { get; }

        /// <summary>
        /// The team that won, or the named absence for a solo ending. See
        /// <see cref="HasSoloWinner"/>.
        /// </summary>
        public TeamId WinningTeamId { get; }

        /// <summary>
        /// The team that lost, or the named absence for a solo ending - where every team has a loser
        /// in it. See <see cref="HasSoloWinner"/>.
        /// </summary>
        public TeamId LosingTeamId { get; }

        /// <summary>
        /// The hero who won alone, or the named absence for one of the three team endings. Milestone
        /// 8.39g.
        /// </summary>
        public HeroId WinningHeroId { get; }

        public bool HasEnded => Cause != MatchVictoryCause.None;

        /// <summary>
        /// Whether this ending awards the match to one hero rather than to a team. Every reader that
        /// wants a side must ask this first, because the two team fields are the named absence when it
        /// is true. Milestone 8.39g.
        /// </summary>
        public bool HasSoloWinner => WinningHeroId.IsDefined;

        public bool Equals(MatchOutcome other) =>
            Cause == other.Cause &&
            WinningTeamId.Equals(other.WinningTeamId) &&
            LosingTeamId.Equals(other.LosingTeamId) &&
            WinningHeroId.Equals(other.WinningHeroId);

        public override bool Equals(object obj) => obj is MatchOutcome other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Cause;
                hashCode = (hashCode * 397) ^ WinningTeamId.GetHashCode();
                hashCode = (hashCode * 397) ^ LosingTeamId.GetHashCode();
                return (hashCode * 397) ^ WinningHeroId.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (!HasEnded) return "unfinished";
            return HasSoloWinner
                ? $"{WinningHeroId.Value} won alone ({Cause})"
                : $"{WinningTeamId.Value} won ({Cause})";
        }
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
            bool gameIsRecordedAsEnded,
            HeroId soloVictoryHeroId = default,
            TeamId teamVictoryTeamId = default) =>
            Read(
                participants,
                lanePush?.GameEndingCause ?? LanePushGameEndingCause.None,
                lanePush?.WinningSide ?? TeamSide.None,
                teamLives,
                gameIsRecordedAsEnded,
                soloVictoryHeroId,
                teamVictoryTeamId);

        /// <summary>
        /// The same reading over the two facts themselves, so a caller holding a restored payload's
        /// values can ask before it has built an authority to hold them.
        /// </summary>
        public static MatchOutcomeReading Read(
            MatchParticipants participants,
            LanePushGameEndingCause lanePushCause,
            TeamSide lanePushWinningSide,
            TeamLifeState teamLives,
            bool gameIsRecordedAsEnded,
            HeroId soloVictoryHeroId = default,
            TeamId teamVictoryTeamId = default)
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

            // Milestone 8.39g. The fourth ending is read from the one fact that records it, and it is
            // exactly as much of a second ending as the other two are of each other: two endings
            // cannot both be THE ending, whether or not they happen to agree about who won. p.8's own
            // tie-break paragraph settles two TEAM endings that land together; a solo claim landing
            // with either is an engine defect rather than a rules outcome, and is refused here rather
            // than resolved by preference - which is what ConflictingVictoryPaths has existed for
            // since milestone 7.7.
            var soloEnding = soloVictoryHeroId.IsDefined;

            // Milestone 9.30g. The fifth ending is a card's TEAM sentence, read from the one fact that
            // records it, and it is as much a second ending as the others: the claim is refused while
            // any other ending is describable (GY-Q10), so two at once is an engine defect here too.
            var teamClaimEnding = teamVictoryTeamId.IsDefined;
            if ((pushEnding && lifeEnding) || eliminated.Length > 1 ||
                (soloEnding && (pushEnding || lifeEnding)) ||
                (teamClaimEnding && (pushEnding || lifeEnding || soloEnding)))
            {
                return MatchOutcomeReading.Faulted(MatchOutcomeFault.ConflictingVictoryPaths);
            }

            if (!pushEnding && !lifeEnding && !soloEnding && !teamClaimEnding)
            {
                return gameIsRecordedAsEnded
                    ? MatchOutcomeReading.Faulted(MatchOutcomeFault.UnexplainedGameEnd)
                    : MatchOutcomeReading.Coherent(MatchOutcome.Unfinished);
            }

            if (!gameIsRecordedAsEnded)
            {
                return MatchOutcomeReading.Faulted(MatchOutcomeFault.UnrecordedGameEnd);
            }

            if (soloEnding) return ReadSoloEnding(participants, soloVictoryHeroId);
            if (teamClaimEnding) return ReadTeamClaimEnding(participants, teamVictoryTeamId);

            return pushEnding
                ? ReadPushEnding(participants, lanePushCause, lanePushWinningSide)
                : ReadLifeEnding(participants, eliminated[0]);
        }

        /// <summary>
        /// Milestone 8.39g. The ending Cutter's A Fistful of Coins reaches: one hero wins and everybody
        /// else loses.
        ///
        /// <para><b>The only thing checked here is that the hero is one this match has</b>, because
        /// that is the only thing that can be wrong about a fact the match itself wrote. A payload
        /// claiming a winner nobody is playing is refused rather than resolved by preference, which is
        /// the terms milestone 7.7 set for every other malformed ending - and the fault is its own,
        /// so the three team causes keep exactly the guard they were written with.</para>
        ///
        /// <para>No team is named, and no team is checked: "you alone win the game" is an ending in
        /// which the winner's own team-mates lose, so there is no winning side and no single losing
        /// one.</para>
        /// </summary>
        private static MatchOutcomeReading ReadSoloEnding(
            MatchParticipants participants, HeroId winningHeroId) =>
            participants.Heroes.Any(hero => hero.Id.Equals(winningHeroId))
                ? MatchOutcomeReading.Coherent(
                    MatchOutcome.WonAlone(MatchVictoryCause.CoinHoardClaimed, winningHeroId))
                : MatchOutcomeReading.Faulted(MatchOutcomeFault.UndeclaredSoloWinner);

        /// <summary>
        /// Milestone 9.30g. The ending Gydion's third Wish reaches: "your team wins the game" - one
        /// winning team, and the one team left is the loser, exactly as p.13's ending names its two.
        /// A recorded team this match does not have, or a match of other than two teams, is refused
        /// with the fault the team endings already share.
        /// </summary>
        private static MatchOutcomeReading ReadTeamClaimEnding(
            MatchParticipants participants, TeamId winningTeamId)
        {
            var losers = participants.Teams
                .Where(team => !team.Id.Equals(winningTeamId))
                .Select(team => team.Id)
                .ToArray();
            if (losers.Length != 1 || !participants.Teams.Any(team => team.Id.Equals(winningTeamId)))
            {
                return MatchOutcomeReading.Faulted(MatchOutcomeFault.UndeclaredOutcomeTeams);
            }

            return MatchOutcomeReading.Coherent(
                MatchOutcome.Won(MatchVictoryCause.WishesCast, winningTeamId, losers[0]));
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
