using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.53. The screen the match ends on.
    ///
    /// <para><b>The user's brief, 2026-09-19:</b> "Can you add a Victory Screen of sorts?! Like show RED
    /// WIN, and winning hero tokens. Like stats or something? Kills/deaths, no of attacks at enemy
    /// heroes, creep kills? Also, add similar thing for a losing team as well? Maybe like scoreboard
    /// after a game, and above it its which team won RED WINS, and then under it the win condition they
    /// won on. And if Cutter solo wins, split him up above the others" - then, reading the mockup back:
    /// "Cards played is unnecessary", "It should say Minions Killed", "add one more column: times
    /// defended", and "Everything else should disappear and this pops up."</para>
    ///
    /// <para><b>It derives nothing the match did not already publish.</b> Six of the seven columns are
    /// <see cref="IMatchReadModel.Scoreboard"/> read straight off, which the authority accumulated at
    /// its own commit boundary; the seventh is <c>HeroProgressionProjection.Level</c>, where p.15's
    /// Level already lives. Counting anything here would be a second reading of the match that could
    /// disagree with the first - and would disagree, because the seat-public match log a client holds
    /// is a bounded window.</para>
    ///
    /// <para><b>Seat-agnostic, deliberately.</b> The outcome and the board are public information in
    /// every projection, so this presenter has no home hero, implements no
    /// <see cref="ISeatScopedPresenter"/> and needs no rebinding: six seats and the Director all draw
    /// the same screen.</para>
    /// </summary>
    public sealed class VictoryScreenPresenter
    {
        private readonly IMatchClient session;
        private readonly IVictoryScreenView view;
        private IMatchReadModel lastProjection;
        private VictoryScreenViewModel lastViewModel;

        /// <summary>The Director-shaped constructor, composing the Director's own client.</summary>
        public VictoryScreenPresenter(IMatchSession session, IVictoryScreenView view)
            : this(new DirectorMatchClient(session), view)
        {
        }

        public VictoryScreenPresenter(IMatchClient session, IVictoryScreenView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        /// <summary>The screen currently drawn, or null while the match is still being played.</summary>
        public VictoryScreenViewModel Model => lastViewModel;

        public void Refresh() => view.Render(GetOrCreateViewModel(session.Projection));

        private VictoryScreenViewModel GetOrCreateViewModel(IMatchReadModel projection)
        {
            if (ReferenceEquals(projection, lastProjection))
            {
                return lastViewModel;
            }

            lastProjection = projection;
            lastViewModel = Derive(projection);
            return lastViewModel;
        }

        /// <summary>
        /// The whole screen from one projection, or null while the match is unfinished.
        ///
        /// <para>The gate is <see cref="MatchOutcome.HasEnded"/> rather than the phase, because the
        /// outcome is the fact that NAMES the winner and the cause the screen prints - the two are the
        /// same instant (<c>RoundResolutionPhase.GameEnded</c> is recorded with the outcome and stops
        /// every later command), and gating on the one that carries the content is what makes a screen
        /// with nothing to say unconstructible rather than merely undrawn.</para>
        /// </summary>
        public static VictoryScreenViewModel Derive(IMatchReadModel projection)
        {
            var outcome = projection?.Outcome ?? default;
            if (projection == null || !outcome.HasEnded)
            {
                return null;
            }

            var setupHeroes = projection.Setup?.Heroes;
            if (setupHeroes == null || setupHeroes.Count == 0)
            {
                return null;
            }

            // Every row first, in the setup's own order, so the "best" marking below is decided over
            // the whole table rather than one block at a time - the mockup highlights the leader of a
            // column across the screen, not the leader of each side.
            var lines = setupHeroes
                .Select(hero => new Line(projection, hero.HeroId, hero.TeamSide, hero.ContentId, hero.DisplayName))
                .ToArray();
            var best = VictoryScreenLabels.Columns.ToDictionary(
                column => column,
                column =>
                {
                    var highest = lines.Max(line => line.Value(column));
                    // A column nothing happened in has no leader: "joint best at nothing" is a fact
                    // about the match rather than about a player.
                    return highest > 0 ? highest : int.MinValue;
                });

            var rows = lines.ToDictionary(
                line => line.HeroId,
                line => new VictoryScreenRowViewModel(
                    line.HeroId,
                    line.TeamSide,
                    line.ContentId,
                    line.DisplayName,
                    line.Worth,
                    VictoryScreenLabels.Columns.Select(column => new VictoryScreenCellViewModel(
                        column, line.Value(column), line.Value(column) == best[column]))));

            var blocks = new List<VictoryScreenBlockViewModel>();
            string bannerName;
            UnityEngine.Color bannerColor;

            if (outcome.HasSoloWinner)
            {
                // Milestone 8.39g's ending, drawn the way the user asked for it: "if Cutter solo wins,
                // split him up above the others". Her own team-mates are in the SECOND block and tagged
                // Defeated, because "you alone win the game" leaves them on the losing side of it.
                var winner = lines.FirstOrDefault(line => line.HeroId.Equals(outcome.WinningHeroId));
                if (winner == null)
                {
                    return null;
                }

                bannerName = winner.DisplayName;
                bannerColor = MatchHudTheme.CoinFace;
                blocks.Add(new VictoryScreenBlockViewModel(
                    VictoryScreenBlockKind.SoloWinner,
                    winner.TeamSide,
                    winner.DisplayName.ToUpperInvariant(),
                    VictoryScreenLabels.SoloTag,
                    new[] { rows[winner.HeroId] }));
                AddDefeatedBlock(blocks, lines, rows, winner.TeamSide, except: winner.HeroId);
                foreach (var side in OtherSides(lines, winner.TeamSide))
                {
                    AddDefeatedBlock(blocks, lines, rows, side, except: default);
                }
            }
            else
            {
                var winningSide = projection.Participants.GetTeam(outcome.WinningTeamId).Side;
                bannerName = VictoryScreenLabels.TeamName(winningSide);
                bannerColor = winningSide == TeamSide.Red
                    ? MatchHudTheme.TeamRed
                    : MatchHudTheme.TeamBlue;
                var winners = RowsOn(lines, rows, winningSide, except: default);
                if (winners.Count == 0)
                {
                    return null;
                }

                blocks.Add(new VictoryScreenBlockViewModel(
                    VictoryScreenBlockKind.Winners,
                    winningSide,
                    VictoryScreenLabels.TeamHeader(winningSide),
                    VictoryScreenLabels.WinnersTag,
                    winners));
                foreach (var side in OtherSides(lines, winningSide))
                {
                    AddDefeatedBlock(blocks, lines, rows, side, except: default);
                }
            }

            return new VictoryScreenViewModel(
                projection.Version,
                outcome.Cause,
                outcome.HasSoloWinner,
                VictoryScreenLabels.Banner(bannerName),
                bannerColor,
                // Upper case because the whole screen is: the words are the strip's own, read from the
                // one formatter both surfaces call, so the sentence under the banner and the sentence
                // in the status line cannot be two different readings of the same ending.
                VictoryCauseLabels.Describe(outcome.Cause).ToUpperInvariant(),
                blocks);
        }

        /// <summary>The sides that have heroes on them, other than <paramref name="side"/>, in setup order.</summary>
        private static IEnumerable<TeamSide> OtherSides(IEnumerable<Line> lines, TeamSide side) =>
            lines.Select(line => line.TeamSide).Where(value => value != side).Distinct();

        private static void AddDefeatedBlock(
            ICollection<VictoryScreenBlockViewModel> blocks,
            IReadOnlyList<Line> lines,
            IReadOnlyDictionary<HeroId, VictoryScreenRowViewModel> rows,
            TeamSide side,
            HeroId except)
        {
            var members = RowsOn(lines, rows, side, except);
            if (members.Count == 0)
            {
                // A solo winner with no team-mates - the staged two-hero boards every dealing's
                // journeys run on - draws no empty table under their own name.
                return;
            }

            blocks.Add(new VictoryScreenBlockViewModel(
                VictoryScreenBlockKind.Defeated,
                side,
                VictoryScreenLabels.TeamHeader(side),
                VictoryScreenLabels.DefeatedTag,
                members));
        }

        private static IReadOnlyList<VictoryScreenRowViewModel> RowsOn(
            IReadOnlyList<Line> lines,
            IReadOnlyDictionary<HeroId, VictoryScreenRowViewModel> rows,
            TeamSide side,
            HeroId except) =>
            lines
                .Where(line => line.TeamSide == side && !line.HeroId.Equals(except))
                .Select(line => rows[line.HeroId])
                .ToArray();

        /// <summary>
        /// One hero's seven numbers, read once from the two places they live: the authority's own
        /// scoreboard, and p.15's progression for the Level that is deliberately not on it.
        /// </summary>
        private sealed class Line
        {
            private readonly HeroScoreboardEntry entry;
            private readonly int level;

            internal Line(
                IMatchReadModel projection,
                HeroId heroId,
                TeamSide teamSide,
                HeroContentId contentId,
                string displayName)
            {
                HeroId = heroId;
                TeamSide = teamSide;
                ContentId = contentId;
                DisplayName = displayName ?? heroId.Value;
                entry = projection.Scoreboard?.FirstOrDefault(row => row.HeroId.Equals(heroId)) ??
                    new HeroScoreboardEntry(heroId, 0, 0, 0, 0, 0, 0);
                level = projection.HeroProgression?
                    .FirstOrDefault(row => row.HeroId.Equals(heroId))?.Level ?? 0;
                // Milestone 8.49's badge, priced by the same reading the Defeat itself is priced by -
                // the rail's own call, so the two surfaces cannot print different numbers for one hero.
                Worth = HeroDefeatCostQuery.Worth(heroId, projection.Definitions, projection.Cards);
            }

            internal HeroId HeroId { get; }
            internal TeamSide TeamSide { get; }
            internal HeroContentId ContentId { get; }
            internal string DisplayName { get; }
            internal int Worth { get; }

            internal int Value(VictoryScreenColumn column)
            {
                switch (column)
                {
                    case VictoryScreenColumn.Level: return level;
                    case VictoryScreenColumn.Kills: return entry.Kills;
                    case VictoryScreenColumn.Deaths: return entry.Deaths;
                    case VictoryScreenColumn.AttacksAtHeroes: return entry.AttacksAtHeroes;
                    case VictoryScreenColumn.TimesDefended: return entry.TimesDefended;
                    case VictoryScreenColumn.MinionsKilled: return entry.MinionsKilled;
                    case VictoryScreenColumn.CoinsEarned: return entry.CoinsEarned;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(column), column, "Every scoreboard column has a number.");
                }
            }
        }
    }
}
