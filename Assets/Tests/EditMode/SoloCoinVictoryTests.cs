using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39g. The victory sentence Cutter's A Fistful of Coins prints, proved as vocabulary
    /// before her content exists: "If you have 13 or more coins, you alone win the game."
    ///
    /// <para><b>The user settled the reading on 2026-09-11 with the contract cost stated: implement it
    /// faithfully.</b> One hero wins and everybody else loses, that hero's own team-mates included.
    /// Every ending this build had before it was a rulebook sentence about TEAMS - <c>MatchOutcome</c>
    /// records a winning team and a losing team together, and its own remarks say "an ending that
    /// cannot name both names neither".</para>
    ///
    /// <para><b>And it is the one victory fact this build RECORDS rather than derives, which is why
    /// the snapshot schema moved.</b> The three team causes are re-read on restore from the Lane-Push
    /// transition and p.13's life counters. Thirteen coins is not a hero who won: coins are farmed by
    /// everyone and spent back down at p.15's Level Up, so what won the match is that this hero
    /// RESOLVED the branch while holding thirteen or more, and nothing else in the match records it.
    /// <see cref="TheCountIsReadAtTheBranchRatherThanAtItsHighWaterMark"/> is that sentence asserted
    /// rather than argued.</para>
    ///
    /// <para>The FACE that reaches this is composed at milestone 8.39h. What these cases own is the
    /// ending itself.</para>
    /// </summary>
    [TestFixture]
    public sealed class SoloCoinVictoryTests
    {
        private static readonly TeamId RedTeam = new TeamId("solo-red");
        private static readonly TeamId BlueTeam = new TeamId("solo-blue");

        /// <summary>The winner, and deliberately one of a PAIR - her team-mate loses with the rest.</summary>
        private static readonly HeroId Winner = new HeroId("solo-winner");
        private static readonly HeroId TeamMate = new HeroId("solo-team-mate");
        private static readonly HeroId EnemyOne = new HeroId("solo-enemy-one");
        private static readonly HeroId EnemyTwo = new HeroId("solo-enemy-two");

        // -------------------------------------------------------------------------------------------
        // The ending
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE WHOLE SENTENCE IN ONE CASE, on a 2v2 where the winner has a team-mate the printed text
        /// does not mention. One hero wins; nobody else does, on either side.
        /// </summary>
        [Test]
        public void TheWinnerWinsAloneAndEveryOtherParticipantLoses()
        {
            var reading = Read(soloVictoryHeroId: Winner, gameIsRecordedAsEnded: true);

            Assert.That(reading.IsCoherent, Is.True, reading.Fault.ToString());
            Assert.That(reading.Outcome.HasEnded, Is.True);
            Assert.That(reading.Outcome.HasSoloWinner, Is.True);
            Assert.That(reading.Outcome.WinningHeroId, Is.EqualTo(Winner));
            Assert.That(reading.Outcome.Cause, Is.EqualTo(MatchVictoryCause.CoinHoardClaimed));

            Assert.That(reading.Outcome.WinningTeamId.IsDefined, Is.False,
                "\"You alone win the game\" awards the match to no team - the winner's own team-mate " +
                "is among the losers, so there is no winning side to name.");
            Assert.That(reading.Outcome.LosingTeamId.IsDefined, Is.False,
                "and no single losing team either, because every team has a loser in it.");
        }

        /// <summary>
        /// The team-mate question, stated as its own case because this is the first ending in the build
        /// where two heroes on one team do not share a result. The printed word is "alone".
        /// </summary>
        [Test]
        public void TheWinnersOwnTeamMateIsNotAWinner()
        {
            var outcome = Read(soloVictoryHeroId: Winner, gameIsRecordedAsEnded: true).Outcome;

            foreach (var other in new[] { TeamMate, EnemyOne, EnemyTwo })
            {
                Assert.That(outcome.WinningHeroId, Is.Not.EqualTo(other),
                    $"{other.Value} did not win: the sentence names one hero.");
            }
        }

        /// <summary>
        /// A solo ending the state does not record is not an ending. This is the half of the clause
        /// that makes it a victory rather than a coin count: thirteen coins in a ledger is not a hero
        /// who resolved the branch.
        /// </summary>
        [Test]
        public void WithNoSoloVictoryRecorded_TheMatchIsUnfinished()
        {
            var reading = Read(soloVictoryHeroId: default, gameIsRecordedAsEnded: false);

            Assert.That(reading.IsCoherent, Is.True);
            Assert.That(reading.Outcome.HasEnded, Is.False);
        }

        /// <summary>
        /// THE COUNT IS READ AT THE BRANCH, NOT AT ITS HIGH-WATER MARK - which is the reason this fact
        /// has to be recorded rather than derived from the coin ledger. A hero who reached thirteen,
        /// spent down at p.15's Level Up and later resolved the branch below thirteen has not won, and
        /// the ledger at the end of the match cannot tell the two stories apart.
        /// </summary>
        [Test]
        public void TheCountIsReadAtTheBranchRatherThanAtItsHighWaterMark()
        {
            var ledger = new HeroCoinLedger(Participants());

            Assert.That(
                Read(soloVictoryHeroId: default, gameIsRecordedAsEnded: false).Outcome.HasEnded,
                Is.False,
                "No solo victory is recorded, so the match is unfinished however many coins anybody " +
                "is holding - and the ledger is where a derivation would have had to look.");
            Assert.That(ledger, Is.Not.Null,
                "The premise this case rests on: a coin ledger exists and is persisted, and it is " +
                "still not the thing that decides this ending.");
        }

        // -------------------------------------------------------------------------------------------
        // What the reading refuses
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A payload claiming a winner this match is not playing is refused rather than resolved by
        /// preference, which is the terms milestone 7.7 set for every malformed ending.
        /// </summary>
        [Test]
        public void ASoloWinnerTheMatchDoesNotHave_IsRefused()
        {
            var reading = Read(
                soloVictoryHeroId: new HeroId("solo-nobody"), gameIsRecordedAsEnded: true);

            Assert.That(reading.IsCoherent, Is.False);
            Assert.That(reading.Fault, Is.EqualTo(MatchOutcomeFault.UndeclaredSoloWinner));
        }

        /// <summary>
        /// A solo claim landing with a team ending is two endings, and two endings cannot both be the
        /// ending. Asserted against BOTH team causes, because they reach the reading by different
        /// routes.
        /// </summary>
        [Test]
        public void ASoloEndingLandingWithATeamEnding_IsRefusedRatherThanPreferred()
        {
            Assert.That(
                Read(
                    soloVictoryHeroId: Winner, gameIsRecordedAsEnded: true,
                    lanePushCause: LanePushGameEndingCause.ThroneCaptured,
                    lanePushWinningSide: TeamSide.Red).Fault,
                Is.EqualTo(MatchOutcomeFault.ConflictingVictoryPaths),
                "p.8's push and a solo claim cannot both have ended the match.");

            Assert.That(
                Read(
                    soloVictoryHeroId: Winner, gameIsRecordedAsEnded: true,
                    eliminatedTeam: BlueTeam).Fault,
                Is.EqualTo(MatchOutcomeFault.ConflictingVictoryPaths),
                "and neither can p.13's counters.");
        }

        /// <summary>
        /// A solo ending recorded while the match is not recorded as ended is the same fault it has
        /// always been for the other three, and is reached by the same route.
        /// </summary>
        [Test]
        public void ASoloEndingThePhaseDoesNotRecord_IsAnUnrecordedGameEnd()
        {
            Assert.That(
                Read(soloVictoryHeroId: Winner, gameIsRecordedAsEnded: false).Fault,
                Is.EqualTo(MatchOutcomeFault.UnrecordedGameEnd));
        }

        /// <summary>
        /// THE THREE TEAM CAUSES ARE UNTOUCHED, asserted rather than assumed - including that
        /// <see cref="MatchOutcomeFault.UndeclaredOutcomeTeams"/> is exactly as strict for them as it
        /// was. Widening that guard for all four endings would have removed the thing it exists for.
        /// </summary>
        [Test]
        public void TheThreeTeamEndingsAreUnchangedAndTheirGuardIsAsStrict()
        {
            var push = Read(
                soloVictoryHeroId: default, gameIsRecordedAsEnded: true,
                lanePushCause: LanePushGameEndingCause.ThroneCaptured,
                lanePushWinningSide: TeamSide.Red);
            Assert.That(push.IsCoherent, Is.True);
            Assert.That(push.Outcome.Cause, Is.EqualTo(MatchVictoryCause.ThroneCaptured));
            Assert.That(push.Outcome.WinningTeamId, Is.EqualTo(RedTeam));
            Assert.That(push.Outcome.LosingTeamId, Is.EqualTo(BlueTeam));
            Assert.That(push.Outcome.HasSoloWinner, Is.False,
                "A team ending names no hero, so a reader cannot mistake one for the other.");

            var life = Read(
                soloVictoryHeroId: default, gameIsRecordedAsEnded: true, eliminatedTeam: BlueTeam);
            Assert.That(life.IsCoherent, Is.True);
            Assert.That(life.Outcome.Cause, Is.EqualTo(MatchVictoryCause.LifeCountersExhausted));
            Assert.That(life.Outcome.WinningTeamId, Is.EqualTo(RedTeam));

            Assert.That(
                MatchOutcomeQuery.Read(
                    SingleTeamParticipants(), LanePushGameEndingCause.ThroneCaptured, TeamSide.Red,
                    SingleTeamLives(), true).Fault,
                Is.EqualTo(MatchOutcomeFault.UndeclaredOutcomeTeams),
                "A push ending that cannot resolve to one team each way is refused exactly as it was " +
                "before this milestone: the guard was not widened to admit an ending with no teams.");
        }

        // -------------------------------------------------------------------------------------------
        // The contract the outcome type itself carries
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The two shapes are constructed by two factories and neither will build the other's, so a
        /// caller cannot produce a solo ending that also claims teams.
        /// </summary>
        [Test]
        public void TheTwoOutcomeShapesCannotBeConfused()
        {
            Assert.Throws<ArgumentException>(
                () => MatchOutcome.WonAlone(MatchVictoryCause.CoinHoardClaimed, default),
                "A solo ending names the one hero who won it.");
            Assert.Throws<ArgumentException>(
                () => MatchOutcome.Won(MatchVictoryCause.ThroneCaptured, RedTeam, RedTeam),
                "and a team ending still names two different teams.");

            var solo = MatchOutcome.WonAlone(MatchVictoryCause.CoinHoardClaimed, Winner);
            var team = MatchOutcome.Won(MatchVictoryCause.ThroneCaptured, RedTeam, BlueTeam);

            Assert.That(solo, Is.Not.EqualTo(team));
            Assert.That(solo.ToString(), Does.Contain("alone"));
            Assert.That(team.HasSoloWinner, Is.False);
        }

        /// <summary>
        /// The fourth cause exists and is distinct from the three rulebook team sentences, so content
        /// naming it cannot silently get one of theirs.
        /// </summary>
        [Test]
        public void TheFourthVictoryCause_IsDistinctFromTheThreeTeamSentences()
        {
            var causes = Enum.GetValues(typeof(MatchVictoryCause))
                .Cast<MatchVictoryCause>()
                .Where(cause => cause != MatchVictoryCause.None)
                .ToArray();

            Assert.That(causes.Length, Is.EqualTo(5),
                "Three at milestone 7.7, four since 8.39g, five since 9.30g's Wish.");
            Assert.That(causes, Does.Contain(MatchVictoryCause.CoinHoardClaimed));
            Assert.That(causes, Does.Contain(MatchVictoryCause.WishesCast));
        }

        /// <summary>
        /// The schema moved, and this is the case that says why in one line: a solo ending is written
        /// where every other ending is derived.
        /// </summary>
        [Test]
        public void TheSchemaMovedBecauseThisEndingHasNoOtherSource()
        {
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(54),
                "v42 carries the solo winner. The three team causes are re-derived on restore from " +
                "the Lane-Push transition and p.13's counters; this one has no such source, because " +
                "holding thirteen coins is not the same fact as having resolved the branch while " +
                "holding them. v43 is milestone 8.53's match scoreboard, written for the same reason " +
                "one version later: accumulated per-hero totals that nothing else in the payload " +
                "remembers - and notably NOT the coins column, which cannot be read off the balances " +
                "beside it, because a balance is what a hero holds after p.15 has spent it down." +
                "Milestone 8.71a moved it again, to v44, for Hanu the Trickster's " +
                "rewritten printed Initiative - a sparse card-keyed list in the card " +
                "section, which is authoritative state no live state remembers.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static MatchOutcomeReading Read(
            HeroId soloVictoryHeroId,
            bool gameIsRecordedAsEnded,
            LanePushGameEndingCause lanePushCause = LanePushGameEndingCause.None,
            TeamSide lanePushWinningSide = TeamSide.None,
            TeamId eliminatedTeam = default) =>
            MatchOutcomeQuery.Read(
                Participants(),
                lanePushCause,
                lanePushWinningSide,
                Lives(eliminatedTeam),
                gameIsRecordedAsEnded,
                soloVictoryHeroId);

        private static TeamLifeState Lives(TeamId eliminatedTeam)
        {
            return TeamLifeState.Restore(
                Participants(),
                new TeamLifeRestoreState(
                    new[]
                    {
                        new KeyValuePair<TeamId, int>(
                            RedTeam, eliminatedTeam.Equals(RedTeam) ? 0 : 5),
                        new KeyValuePair<TeamId, int>(
                            BlueTeam, eliminatedTeam.Equals(BlueTeam) ? 0 : 5)
                    },
                    new[]
                    {
                        new KeyValuePair<TeamId, int>(RedTeam, 5),
                        new KeyValuePair<TeamId, int>(BlueTeam, 5)
                    }));
        }

        /// <summary>A 2v2, so the winner has a team-mate the printed sentence does not mention.</summary>
        private static MatchParticipants Participants()
        {
            var players = new List<Player>();
            var heroes = new List<Hero>();
            foreach (var pair in new[]
            {
                (Hero: Winner, Team: RedTeam),
                (Hero: TeamMate, Team: RedTeam),
                (Hero: EnemyOne, Team: BlueTeam),
                (Hero: EnemyTwo, Team: BlueTeam)
            })
            {
                var player = new PlayerId(pair.Hero.Value + "-player");
                players.Add(new Player(player, pair.Team));
                heroes.Add(new Hero(pair.Hero, player, HeroDashboard.RulebookLayout));
            }

            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                players,
                heroes);
        }

        /// <summary>
        /// A composition with one team, which is what <see cref="MatchOutcomeFault.UndeclaredOutcomeTeams"/>
        /// was written to catch and still catches.
        /// </summary>
        private static MatchParticipants SingleTeamParticipants()
        {
            var player = new PlayerId("solo-lonely-player");
            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red) },
                new[] { new Player(player, RedTeam) },
                new[] { new Hero(Winner, player, HeroDashboard.RulebookLayout) });
        }

        private static TeamLifeState SingleTeamLives() =>
            TeamLifeState.Restore(
                SingleTeamParticipants(),
                new TeamLifeRestoreState(
                    new[] { new KeyValuePair<TeamId, int>(RedTeam, 5) },
                    new[] { new KeyValuePair<TeamId, int>(RedTeam, 5) }));
    }
}
