using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.53. The end-of-match screen's derivation: who the banner names, which sentence it
    /// stands on, which block each hero is in, and which number in each column is the leader.
    /// </summary>
    /// <remarks>
    /// <para><b>Two kinds of case, and both are needed.</b> The ENDING is staged, because there is no
    /// offline way to make one configured match end four different ways and every attempt to would be
    /// a test about how to reach an ending rather than about what the screen says. The MATCH is real
    /// in every case - a composed six-hero session with its own participants, setup order, definitions
    /// and cards - so the rows are built from the same shapes the live HUD builds them from. The last
    /// case drops the staging entirely and derives the screen from a genuinely played-out match.</para>
    /// <para><b>This fixture reaches Presentation</b>, so it is excluded from the offline harness by
    /// <c>Tools/DotnetTests/UnityOnlyTests.props</c> exactly as <c>HeroRailPortraitTests</c> is, and
    /// runs in Unity's Edit Mode.</para>
    /// </remarks>
    public sealed class VictoryScreenPresenterTests
    {
        // -----------------------------------------------------------------------------------------
        // Nothing is drawn while the match is being played
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AMatchThatHasNotEnded_DrawsNoScreenAtAll()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();

            Assert.That(session.Projection.Outcome.HasEnded, Is.False, "Arrange: the match is running.");
            Assert.That(VictoryScreenPresenter.Derive(session.Projection), Is.Null,
                "A null model is what tells the HUD it may not hide itself: the screen and the rule " +
                "that everything else disappears are one fact, not two.");
        }

        [Test]
        public void ThePresenterDrawsNothingUntilTheMatchEnds_AndThenDrawsTheScreen()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var view = new RecordingView();
            var client = new StubMatchClient(session.Projection);
            var presenter = new VictoryScreenPresenter(client, view);

            presenter.Refresh();
            Assert.That(presenter.Model, Is.Null);
            Assert.That(view.Rendered, Is.Null, "The view is told there is no screen, not left stale.");

            client.Projection = EndedProjection.TeamWin(
                session.Projection, MatchVictoryCause.ThroneCaptured, TeamSide.Blue);
            presenter.Refresh();

            Assert.That(presenter.Model, Is.Not.Null);
            Assert.That(view.Rendered, Is.SameAs(presenter.Model),
                "and the one model the presenter holds is the one the view was handed.");
            Assert.That(view.RenderCount, Is.EqualTo(2));
        }

        // -----------------------------------------------------------------------------------------
        // A team win, on every cause the rulebook has
        // -----------------------------------------------------------------------------------------

        [TestCase(MatchVictoryCause.FinalWaveFlipped, TeamSide.Red)]
        [TestCase(MatchVictoryCause.ThroneCaptured, TeamSide.Blue)]
        [TestCase(MatchVictoryCause.LifeCountersExhausted, TeamSide.Red)]
        [TestCase(MatchVictoryCause.CoinHoardClaimed, TeamSide.Blue)]
        public void ATeamWin_NamesTheSideItsColourAndTheSentenceThatEndedIt(
            MatchVictoryCause cause, TeamSide winningSide)
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var model = VictoryScreenPresenter.Derive(
                EndedProjection.TeamWin(session.Projection, cause, winningSide));

            Assert.That(model, Is.Not.Null);
            Assert.That(model.BannerText,
                Is.EqualTo(winningSide == TeamSide.Red ? "RED WINS" : "BLUE WINS"),
                "The user asked for this line by name: \"above it its which team won RED WINS\".");
            Assert.That(model.BannerColor, Is.EqualTo(winningSide == TeamSide.Red
                ? MatchHudTheme.TeamRed
                : MatchHudTheme.TeamBlue),
                "in the team's own colour, which is the HUD's one Red and one Blue.");
            Assert.That(model.CauseText,
                Is.EqualTo(VictoryCauseLabels.Describe(cause).ToUpperInvariant()),
                "and the sentence under it is the SAME formatter the status line has been saying " +
                "all match - one source, so the two surfaces cannot disagree about why it ended.");
            Assert.That(model.Cause, Is.EqualTo(cause));
            Assert.That(model.HasSoloWinner, Is.False);
        }

        [Test]
        public void ATeamWin_PutsTheWinnersBlockAboveTheLosersAndRowsInSetupOrder()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var projection = EndedProjection.TeamWin(
                session.Projection, MatchVictoryCause.FinalWaveFlipped, TeamSide.Blue);
            var model = VictoryScreenPresenter.Derive(projection);

            Assert.That(model.Blocks.Select(block => block.Kind),
                Is.EqualTo(new[] { VictoryScreenBlockKind.Winners, VictoryScreenBlockKind.Defeated }),
                "Winners above the defeated - the user's own layout: \"scoreboard after a game, and " +
                "above it its which team won\".");
            Assert.That(model.Blocks[0].TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(model.Blocks[0].Header, Is.EqualTo("BLUE TEAM"));
            Assert.That(model.Blocks[0].Tag, Is.EqualTo(VictoryScreenLabels.WinnersTag));
            Assert.That(model.Blocks[1].TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(model.Blocks[1].Header, Is.EqualTo("RED TEAM"));
            Assert.That(model.Blocks[1].Tag, Is.EqualTo(VictoryScreenLabels.DefeatedTag));

            foreach (var block in model.Blocks)
            {
                Assert.That(
                    block.Rows.Select(row => row.HeroId),
                    Is.EqualTo(projection.Setup.Heroes
                        .Where(hero => hero.TeamSide == block.TeamSide)
                        .Select(hero => hero.HeroId)),
                    "A block's rows are that side's heroes in the order the match was set up in, " +
                    "which is the order every other surface draws them in too.");
            }

            Assert.That(model.Rows.Select(row => row.HeroId).Distinct().Count(),
                Is.EqualTo(projection.Setup.Heroes.Count),
                "and every hero in the match is on the screen exactly once - the user asked for the " +
                "losing team's numbers as well as the winners'.");
        }

        [Test]
        public void EveryRowCarriesTheScoreboardsOwnNumbersAndTheProgressionsOwnLevel()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var heroes = session.Projection.Setup.Heroes.Select(hero => hero.HeroId).ToArray();
            var board = heroes
                .Select((heroId, index) => new HeroScoreboardEntry(
                    heroId, index, index + 1, index + 2, index + 3, index + 4, index + 5))
                .ToArray();
            var levels = heroes.ToDictionary(heroId => heroId, heroId => 2 + heroes.ToList().IndexOf(heroId));
            var projection = EndedProjection.TeamWin(
                session.Projection, MatchVictoryCause.ThroneCaptured, TeamSide.Red, board, levels);

            var model = VictoryScreenPresenter.Derive(projection);

            foreach (var row in model.Rows)
            {
                var entry = board.Single(value => value.HeroId.Equals(row.HeroId));
                Assert.That(row.ValueOf(VictoryScreenColumn.Kills), Is.EqualTo(entry.Kills));
                Assert.That(row.ValueOf(VictoryScreenColumn.Deaths), Is.EqualTo(entry.Deaths));
                Assert.That(row.ValueOf(VictoryScreenColumn.AttacksAtHeroes),
                    Is.EqualTo(entry.AttacksAtHeroes));
                Assert.That(row.ValueOf(VictoryScreenColumn.TimesDefended),
                    Is.EqualTo(entry.TimesDefended));
                Assert.That(row.ValueOf(VictoryScreenColumn.MinionsKilled),
                    Is.EqualTo(entry.MinionsKilled));
                Assert.That(row.ValueOf(VictoryScreenColumn.CoinsEarned),
                    Is.EqualTo(entry.CoinsEarned));
                Assert.That(row.ValueOf(VictoryScreenColumn.Level), Is.EqualTo(levels[row.HeroId]),
                    "Level is the one column that is NOT on the scoreboard: it is read where p.15's " +
                    "own Level already lives, exactly as the hero rail reads it.");
                Assert.That(row.Cells.Select(cell => cell.Column),
                    Is.EqualTo(VictoryScreenLabels.Columns),
                    "and every row prints the seven columns in the one settled order.");
            }

            Assert.That(
                model.Rows.First().Cells.Select(cell => cell.Header),
                Is.EqualTo(new[]
                {
                    "Level", "Kills", "Deaths", "Attacks at heroes", "Times defended",
                    "Minions Killed", "Coins earned"
                }),
                "The headers are the user's own words - \"It should say Minions Killed\", \"add one " +
                "more column: times defended\" - and \"Cards played\" is not among them.");
        }

        [Test]
        public void EveryRowCarriesTheHerosOwnIdentityAndTheRailsOwnWorthBadge()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var projection = EndedProjection.TeamWin(
                session.Projection, MatchVictoryCause.ThroneCaptured, TeamSide.Red);
            var model = VictoryScreenPresenter.Derive(projection);

            foreach (var hero in projection.Setup.Heroes)
            {
                var row = model.Rows.Single(value => value.HeroId.Equals(hero.HeroId));
                Assert.That(row.DisplayName, Is.EqualTo(hero.DisplayName));
                Assert.That(row.TeamSide, Is.EqualTo(hero.TeamSide));
                Assert.That(row.ContentId, Is.EqualTo(hero.ContentId),
                    "The token disc is addressed by the projected content identity, like the rail's " +
                    "medallion and the piece on the board - never by the name printed beside it.");
                Assert.That(row.Worth, Is.EqualTo(
                    GuardsDigital.Core.Rules.Consequences.HeroDefeatCostQuery.Worth(
                        hero.HeroId, projection.Definitions, projection.Cards)),
                    "and the 8.49 badge prices the hero by the same reading the Defeat itself does.");
                if (row.Worth > 0)
                {
                    Assert.That(row.WorthLabel, Is.EqualTo(row.Worth.ToString()));
                    Assert.That(row.WorthAssistiveLabel, Does.Contain("assist coin"));
                }
            }
        }

        // -----------------------------------------------------------------------------------------
        // The solo ending
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The user: "if Cutter solo wins, split him up above the others, and represent all the same
        /// stuff". Her own team-mates are not winners - "you alone win the game" leaves them with
        /// everybody else - so they are the SECOND block and tagged Defeated.
        /// </summary>
        [Test]
        public void ASoloWin_PutsTheWinnerAloneOnTopThenTheirOwnTeamThenTheEnemy()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var winner = session.Projection.Setup.Heroes.First(hero => hero.TeamSide == TeamSide.Red);
            var projection = EndedProjection.SoloWin(
                session.Projection, MatchVictoryCause.CoinHoardClaimed, winner.HeroId);

            var model = VictoryScreenPresenter.Derive(projection);

            Assert.That(model.HasSoloWinner, Is.True);
            Assert.That(model.BannerText,
                Is.EqualTo(winner.DisplayName.ToUpperInvariant() + " WINS"),
                "The banner names the hero, because a solo ending has no winning team to name.");
            Assert.That(model.BannerColor, Is.EqualTo(MatchHudTheme.CoinFace),
                "in the coin's own gold - it is the coin hoard that won it.");
            Assert.That(model.CauseText,
                Is.EqualTo(VictoryCauseLabels.Describe(MatchVictoryCause.CoinHoardClaimed)
                    .ToUpperInvariant()));

            Assert.That(model.Blocks.Select(block => block.Kind), Is.EqualTo(new[]
            {
                VictoryScreenBlockKind.SoloWinner,
                VictoryScreenBlockKind.Defeated,
                VictoryScreenBlockKind.Defeated
            }));
            Assert.That(model.Blocks[0].Rows.Select(row => row.HeroId),
                Is.EqualTo(new[] { winner.HeroId }),
                "The first block is the winner and nobody else.");
            Assert.That(model.Blocks[0].Header,
                Is.EqualTo(winner.DisplayName.ToUpperInvariant()));
            Assert.That(model.Blocks[0].Tag, Is.EqualTo(VictoryScreenLabels.SoloTag));

            Assert.That(model.Blocks[1].TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(model.Blocks[1].Tag, Is.EqualTo(VictoryScreenLabels.DefeatedTag),
                "Her OWN team is defeated with everybody else, which is the first ending in this " +
                "build where two heroes on one team do not share a result.");
            Assert.That(model.Blocks[1].Rows.Select(row => row.HeroId),
                Is.EqualTo(projection.Setup.Heroes
                    .Where(hero => hero.TeamSide == TeamSide.Red && !hero.HeroId.Equals(winner.HeroId))
                    .Select(hero => hero.HeroId)));

            Assert.That(model.Blocks[2].TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(model.Blocks[2].Tag, Is.EqualTo(VictoryScreenLabels.DefeatedTag));

            Assert.That(model.Rows.Select(row => row.HeroId).Distinct().Count(),
                Is.EqualTo(projection.Setup.Heroes.Count),
                "and the split costs nobody their line: every hero is still on the screen once, " +
                "with \"all the same stuff\" in the same seven columns.");
            Assert.That(model.Blocks.All(block =>
                    block.Rows.All(row => row.Cells.Select(cell => cell.Column)
                        .SequenceEqual(VictoryScreenLabels.Columns))),
                Is.True);
        }

        /// <summary>
        /// The staged two-hero boards every dealing's journeys run on have no team-mate to demote. The
        /// winner's own side then has no second block at all rather than an empty table under it.
        /// </summary>
        [Test]
        public void ASoloWinWithNoTeamMates_DrawsNoEmptyTableUnderTheWinner()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var winner = session.Projection.Setup.Heroes.First();
            var projection = EndedProjection.SoloWin(
                session.Projection, MatchVictoryCause.CoinHoardClaimed, winner.HeroId);

            var model = VictoryScreenPresenter.Derive(projection);

            Assert.That(model.Blocks.Select(block => block.Kind), Is.EqualTo(new[]
            {
                VictoryScreenBlockKind.SoloWinner, VictoryScreenBlockKind.Defeated
            }));
            Assert.That(model.Blocks[1].TeamSide, Is.Not.EqualTo(winner.TeamSide));
            Assert.That(model.Blocks.All(block => block.Rows.Count > 0), Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // The mockup's `best` class
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheLeaderOfEachColumnIsMarked_TiesAllOfThemAndAnEmptyColumnNobody()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var heroes = session.Projection.Setup.Heroes.Select(hero => hero.HeroId).ToArray();
            Assert.That(heroes.Length, Is.EqualTo(6), "Arrange: rulebook p.6's three a side.");

            // Kills: a single leader. Attacks at heroes: a two-way tie at the top. Deaths: a two-way
            // tie at the top that must mark NOBODY, because being defeated most often leads nothing.
            // Minions: nothing happened to anybody. Coins: every hero the same, which is a six-way tie
            // rather than an empty column.
            var kills = new[] { 0, 4, 1, 2, 0, 3 };
            var deaths = new[] { 2, 2, 1, 0, 0, 0 };
            var attacks = new[] { 3, 3, 1, 0, 2, 0 };
            var board = heroes
                .Select((heroId, index) => new HeroScoreboardEntry(
                    heroId, kills[index], deaths[index], attacks[index], 0, 0, 7))
                .ToArray();
            var model = VictoryScreenPresenter.Derive(EndedProjection.TeamWin(
                session.Projection, MatchVictoryCause.FinalWaveFlipped, TeamSide.Red, board,
                heroes.ToDictionary(heroId => heroId, _ => 3)));

            var rows = model.Rows.ToDictionary(row => row.HeroId);
            Assert.That(
                heroes.Where(heroId => rows[heroId].Cell(VictoryScreenColumn.Kills).IsBest),
                Is.EqualTo(new[] { heroes[1] }),
                "One hero led the column, so one cell is marked.");
            Assert.That(
                heroes.Where(heroId => rows[heroId].Cell(VictoryScreenColumn.AttacksAtHeroes).IsBest),
                Is.EqualTo(new[] { heroes[0], heroes[1] }),
                "A tie marks every hero that tied - there is no first-listed tie-break here.");
            Assert.That(
                heroes.Any(heroId => rows[heroId].Cell(VictoryScreenColumn.Deaths).IsBest),
                Is.False,
                "Deaths marks nobody, tie or not: the mark says \"led the column\", and the hero " +
                "defeated most often led nothing. The approved mockup marks no Deaths cell.");
            Assert.That(
                heroes.Any(heroId => rows[heroId].Cell(VictoryScreenColumn.MinionsKilled).IsBest),
                Is.False,
                "A column nothing happened in marks nobody: \"joint best at nothing\" is a fact " +
                "about the match rather than about a player.");
            Assert.That(
                heroes.All(heroId => rows[heroId].Cell(VictoryScreenColumn.CoinsEarned).IsBest),
                Is.True,
                "but a column where everybody did the SAME non-zero thing is a six-way tie, which " +
                "is not the same statement as an empty one.");
            Assert.That(
                heroes.All(heroId => rows[heroId].Cell(VictoryScreenColumn.Level).IsBest),
                Is.True,
                "and Level is marked on the same terms as every other column.");
        }

        [Test]
        public void TheMarkingIsDecidedAcrossTheWholeScreen_NotOneBlockAtATime()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var heroes = session.Projection.Setup.Heroes;
            var best = heroes.Last(hero => hero.TeamSide != heroes[0].TeamSide);
            var board = heroes
                .Select(hero => new HeroScoreboardEntry(
                    hero.HeroId, hero.HeroId.Equals(best.HeroId) ? 9 : 1, 0, 0, 0, 0, 0))
                .ToArray();

            var model = VictoryScreenPresenter.Derive(EndedProjection.TeamWin(
                session.Projection, MatchVictoryCause.ThroneCaptured, heroes[0].TeamSide, board));

            Assert.That(
                model.Rows.Where(row => row.Cell(VictoryScreenColumn.Kills).IsBest)
                    .Select(row => row.HeroId),
                Is.EqualTo(new[] { best.HeroId }),
                "The leader of a column is the leader of the SCREEN. A defeated hero who did the " +
                "most of something still gets the mark for it - that is what a scoreboard is.");
        }

        // -----------------------------------------------------------------------------------------
        // ... and the same derivation against a match that actually played itself out
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Nothing staged: the same six-hero session <c>SeatScopedMatchHudTests</c> plays to its
        /// terminal projection, with the scoreboard the authority actually accumulated while it ran.
        /// </summary>
        [Test]
        public void ARealPlayedOutMatch_DrawsTheBoardTheAuthorityActuallyAccumulated()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var record = DeterministicMatchLoop.Play(session, seed: 1, maximumCommands: 1200);
            Assert.That(record.Outcome.HasEnded, Is.True,
                "Arrange: the recorded local scenario must reach its terminal projection.");

            var projection = session.Projection;
            var model = VictoryScreenPresenter.Derive(projection);

            Assert.That(model, Is.Not.Null);
            Assert.That(projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded),
                "The phase and the outcome are the same instant, which is why the screen may gate " +
                "on either one.");
            Assert.That(model.Cause, Is.EqualTo(projection.Outcome.Cause));
            Assert.That(model.BannerText, Is.EqualTo(VictoryScreenLabels.Banner(
                VictoryScreenLabels.TeamName(
                    projection.Participants.GetTeam(projection.Outcome.WinningTeamId).Side))));

            foreach (var entry in projection.Scoreboard)
            {
                var row = model.Rows.Single(value => value.HeroId.Equals(entry.HeroId));
                Assert.That(row.ValueOf(VictoryScreenColumn.Kills), Is.EqualTo(entry.Kills));
                Assert.That(row.ValueOf(VictoryScreenColumn.Deaths), Is.EqualTo(entry.Deaths));
                Assert.That(row.ValueOf(VictoryScreenColumn.AttacksAtHeroes),
                    Is.EqualTo(entry.AttacksAtHeroes));
                Assert.That(row.ValueOf(VictoryScreenColumn.TimesDefended),
                    Is.EqualTo(entry.TimesDefended));
                Assert.That(row.ValueOf(VictoryScreenColumn.MinionsKilled),
                    Is.EqualTo(entry.MinionsKilled));
                Assert.That(row.ValueOf(VictoryScreenColumn.CoinsEarned),
                    Is.EqualTo(entry.CoinsEarned));
                Assert.That(row.ValueOf(VictoryScreenColumn.Level), Is.EqualTo(projection.HeroProgression
                    .Single(value => value.HeroId.Equals(entry.HeroId)).Level));
            }

            Assert.That(model.Rows.Count(), Is.EqualTo(projection.Scoreboard.Count),
                "one row per hero, and the board has a line for every hero from the first command.");
        }

        /// <summary>
        /// Every seat reads the same screen, because the outcome and the board are public information.
        /// </summary>
        [Test]
        public void EverySeatDrawsTheSameScreenAsTheDirector()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var record = DeterministicMatchLoop.Play(session, seed: 1, maximumCommands: 1200);
            Assert.That(record.Outcome.HasEnded, Is.True);

            var director = VictoryScreenPresenter.Derive(session.Projection);
            var host = new MatchHost(session);
            foreach (var seat in host.Seats.Seats)
            {
                var client = new SeatMatchClient(host.ClientForSeat(seat.Id));
                var seatModel = VictoryScreenPresenter.Derive(client.Projection);
                Assert.That(seatModel, Is.Not.Null, "Seat " + seat.Id + " draws the screen too.");
                Assert.That(seatModel.BannerText, Is.EqualTo(director.BannerText));
                Assert.That(seatModel.CauseText, Is.EqualTo(director.CauseText));
                Assert.That(
                    seatModel.Rows.Select(row =>
                        row.HeroId.Value + ":" + string.Join(
                            ",", row.Cells.Select(cell => cell.Text))),
                    Is.EqualTo(director.Rows.Select(row =>
                        row.HeroId.Value + ":" + string.Join(
                            ",", row.Cells.Select(cell => cell.Text)))),
                    "and every number on it, for every hero, including the ones on the other team.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Fixtures
        // -----------------------------------------------------------------------------------------

        private sealed class RecordingView : IVictoryScreenView
        {
            public VictoryScreenViewModel Rendered { get; private set; }
            public int RenderCount { get; private set; }

            public void Render(VictoryScreenViewModel model)
            {
                Rendered = model;
                RenderCount++;
            }
        }

        private sealed class StubMatchClient : IMatchClient
        {
            public StubMatchClient(IMatchReadModel projection) => Projection = projection;

            public IMatchReadModel Projection { get; set; }

            public bool IsDirector => true;

            public MatchClientResult Submit(DirectorCommand command) =>
                throw new NotSupportedException("This stub answers Refresh() reads only.");

            public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
                throw new NotSupportedException("This stub answers Refresh() reads only.");

            public MatchClientResult SubmitUndo() =>
                throw new NotSupportedException("This stub answers Refresh() reads only.");
        }

        /// <summary>
        /// A REAL composed match, read as though it had ended a chosen way. Everything the screen is
        /// built from but the ending itself - the participants, the setup order, the definitions, the
        /// cards the worth badge is priced from - is the composed session's own; only the outcome, the
        /// phase, the board and the levels are staged, because no offline seam can make one match end
        /// four different ways.
        /// </summary>
        private sealed class EndedProjection : IMatchReadModel
        {
            private readonly IMatchReadModel inner;
            private readonly IReadOnlyList<HeroScoreboardEntry> board;
            private readonly IReadOnlyList<HeroProgressionProjection> progression;

            private EndedProjection(
                IMatchReadModel inner,
                MatchOutcome outcome,
                IReadOnlyList<HeroScoreboardEntry> board,
                IReadOnlyDictionary<HeroId, int> levels)
            {
                this.inner = inner;
                Outcome = outcome;
                this.board = board ?? inner.Scoreboard;
                progression = levels == null
                    ? inner.HeroProgression
                    : inner.HeroProgression
                        .Select(entry => new HeroProgressionProjection(
                            entry.HeroId,
                            levels.TryGetValue(entry.HeroId, out var level) ? level : entry.Level,
                            entry.Coins,
                            entry.LeveledThisRound,
                            entry.ItemBonuses))
                        .ToArray();
            }

            internal static EndedProjection TeamWin(
                IMatchReadModel inner,
                MatchVictoryCause cause,
                TeamSide winningSide,
                IReadOnlyList<HeroScoreboardEntry> board = null,
                IReadOnlyDictionary<HeroId, int> levels = null)
            {
                var winning = inner.Participants.Teams.Single(team => team.Side == winningSide);
                var losing = inner.Participants.Teams.Single(team => team.Side != winningSide);
                return new EndedProjection(
                    inner, MatchOutcome.Won(cause, winning.Id, losing.Id), board, levels);
            }

            internal static EndedProjection SoloWin(
                IMatchReadModel inner,
                MatchVictoryCause cause,
                HeroId winner,
                IReadOnlyList<HeroScoreboardEntry> board = null,
                IReadOnlyDictionary<HeroId, int> levels = null) =>
                new EndedProjection(inner, MatchOutcome.WonAlone(cause, winner), board, levels);

            public MatchOutcome Outcome { get; }

            public RoundResolutionPhase Phase => RoundResolutionPhase.GameEnded;

            public IReadOnlyList<HeroScoreboardEntry> Scoreboard => board;

            public IReadOnlyList<HeroProgressionProjection> HeroProgression => progression;

            // Everything else is the composed match's own reading, forwarded unchanged.
            public long Version => inner.Version;
            public bool IsInputEnabled => inner.IsInputEnabled;
            public bool IsSeatUndoAvailable => inner.IsSeatUndoAvailable;
            public QuickGameSetupState Setup => inner.Setup;
            public BoardMapDefinition SelectedMap => inner.SelectedMap;
            public MatchParticipants Participants => inner.Participants;
            public IReadOnlyList<CardDefinition> Definitions => inner.Definitions;
            public IReadOnlyList<HeroOccupancy> Heroes => inner.Heroes;
            public IReadOnlyList<MinionOccupancy> Minions => inner.Minions;
            public IReadOnlyList<ObstacleOccupancy> Obstacles => inner.Obstacles;
            public IReadOnlyList<PlacedToken> PlacedTokens => inner.PlacedTokens;
            public IReadOnlyList<MarkerAttachment> HeldMarkers => inner.HeldMarkers;

            public IEnumerable<MarkerAttachment> MarkersHeldBy(HeroId holderHeroId) =>
                inner.MarkersHeldBy(holderHeroId);

            public int RoundIndex => inner.RoundIndex;
            public int TurnIndex => inner.TurnIndex;
            public TieBreakerState TieBreakerState => inner.TieBreakerState;
            public IReadOnlyList<RoundTurnSlot> TurnSlots => inner.TurnSlots;
            public IReadOnlyList<CompletedRoundRecord> CompletedRounds => inner.CompletedRounds;
            public EndOfRoundStage? CurrentEndOfRoundStage => inner.CurrentEndOfRoundStage;
            public IReadOnlyList<MatchCommitmentProjection> Commitments => inner.Commitments;
            public MatchCommitmentProjection GetCommitment(HeroId heroId) => inner.GetCommitment(heroId);
            public SelectedInitiativeActor SelectedActor => inner.SelectedActor;
            public bool IsTurnActive => inner.IsTurnActive;

            public IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters => inner.TeamLifeCounters;

            public int RemainingWaveCounters => inner.RemainingWaveCounters;

            public IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds =>
                inner.ActiveEffectSourceCardInstanceIds;

            public bool IsActiveEffectSource(CardInstanceId cardInstanceId) =>
                inner.IsActiveEffectSource(cardInstanceId);

            public TeamAgreementProjection TeamAgreement => inner.TeamAgreement;
            public IReadOnlyList<CardInstance> Cards => inner.Cards;
            public PendingDecisionContract DecisionContract => inner.DecisionContract;
            public PendingDecisionOwner DecisionOwner => inner.DecisionOwner;

            public IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> MatchLog =>
                inner.MatchLog;

            public MatchUndoMarker UndoMarker => inner.UndoMarker;
            public PendingInitiativeChoice PendingInitiativeChoice => inner.PendingInitiativeChoice;

            public PendingCardActionResolution PendingMovementResolution =>
                inner.PendingMovementResolution;

            public bool HasPendingMovementResolution => inner.HasPendingMovementResolution;
            public PendingMovementChoice PendingMovementChoice => inner.PendingMovementChoice;
            public PendingAttackResolution PendingAttackResolution => inner.PendingAttackResolution;
            public bool HasPendingAttackResolution => inner.HasPendingAttackResolution;

            public PendingAttackTargetChoice PendingAttackTargetChoice =>
                inner.PendingAttackTargetChoice;

            public IReadOnlyList<AttackTargetDefensePreviewProjection>
                PendingAttackTargetDefensePreviews => inner.PendingAttackTargetDefensePreviews;

            public PendingDefenseResponseChoice PendingDefenseChoice => inner.PendingDefenseChoice;
            public PendingDefensePreviewProjection PendingDefensePreview => inner.PendingDefensePreview;
            public PendingResolution PendingSkillResolution => inner.PendingSkillResolution;
            public bool HasPendingSkillResolution => inner.HasPendingSkillResolution;
            public CardInstanceId ResolvingSourceCardInstanceId =>
                inner.ResolvingSourceCardInstanceId;
            public PendingEffectChoice PendingEffectChoice => inner.PendingEffectChoice;

            public IReadOnlyList<EffectChoiceCandidatePreview> PendingEffectChoicePreview =>
                inner.PendingEffectChoicePreview;

            public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
                PendingDisplacedMinionCorrection => inner.PendingDisplacedMinionCorrection;

            public PendingMinionBattleChoice PendingMinionBattleChoice =>
                inner.PendingMinionBattleChoice;

            public PendingHeroRespawnChoice PendingHeroRespawnChoice =>
                inner.PendingHeroRespawnChoice;
            public PendingActingBodyChoice PendingActingBodyChoice =>
                inner.PendingActingBodyChoice;

            public GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
                PendingSystemTransitionStep => inner.PendingSystemTransitionStep;

            public GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
                PendingSystemTransitionDescription => inner.PendingSystemTransitionDescription;

            public RemovalPreventionPrompt PendingRemovalPrevention => inner.PendingRemovalPrevention;
            public PendingTurnRelease PendingTurnRelease => inner.PendingTurnRelease;
            public IReadOnlyList<ActionOffer> AvailableTurnActions => inner.AvailableTurnActions;

            public IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions =>
                inner.AvailableFastTravelOptions;

            public IReadOnlyList<ClearOptionProjection> AvailableClearOptions =>
                inner.AvailableClearOptions;

            public ActionOffer? AvailableHoldOffer => inner.AvailableHoldOffer;
        }
    }
}
