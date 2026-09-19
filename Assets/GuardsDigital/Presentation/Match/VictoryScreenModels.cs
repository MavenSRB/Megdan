using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.53. The one place a <see cref="MatchVictoryCause"/> becomes words.
    ///
    /// <para>It was <c>RoundActionPresenter.FormatVictoryCause</c>'s private switch until the victory
    /// screen needed the same sentence under its banner. Two copies of these four strings would be two
    /// answers to "why did the match end", and the one the player read on the screen could disagree
    /// with the one the status line had been saying for the whole match. The strip's own formatter is
    /// KEPT, as a private static that forwards here: <c>SeatScopedMatchHudTests
    /// .EveryVictoryCause_HasAWordedSentenceTheStatusLineCanSay</c> reflects on it by name to walk the
    /// enum, and that guard exists because milestone 8.39g added a cause without a sentence and the
    /// HUD threw at the moment somebody won.</para>
    /// </summary>
    internal static class VictoryCauseLabels
    {
        internal static string Describe(MatchVictoryCause cause)
        {
            switch (cause)
            {
                case MatchVictoryCause.FinalWaveFlipped:
                    return "the final Wave counter was flipped";
                case MatchVictoryCause.ThroneCaptured:
                    return "the enemy Throne was captured";
                case MatchVictoryCause.LifeCountersExhausted:
                    return "the opposing team ran out of Life counters";

                // Milestone 8.39j. A card sentence rather than a rulebook one - Cutter's A Fistful of
                // Coins - which is why it reads as the card rather than as a page.
                case MatchVictoryCause.CoinHoardClaimed:
                    return "thirteen coins were hoarded";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(cause), cause, "An ended match requires one rulebook victory cause.");
            }
        }
    }

    /// <summary>
    /// Milestone 8.53. The seven columns the scoreboard prints, in the order the user settled them.
    ///
    /// <para><see cref="Level"/> is the one column that is NOT on
    /// <see cref="GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry"/>: p.15's Level is already
    /// <c>HeroProgressionProjection.Level</c> and the screen reads it there, exactly as the hero rail
    /// does. "Cards played" is absent because the user removed it ("Cards played is unnecessary"), and
    /// <see cref="MinionsKilled"/> carries the user's own capitalisation of it.</para>
    /// </summary>
    public enum VictoryScreenColumn
    {
        Level = 0,
        Kills = 1,
        Deaths = 2,
        AttacksAtHeroes = 3,
        TimesDefended = 4,
        MinionsKilled = 5,
        CoinsEarned = 6
    }

    /// <summary>What kind of block a table of heroes is - which decides its tag and its border.</summary>
    public enum VictoryScreenBlockKind
    {
        /// <summary>The team that won.</summary>
        Winners = 0,

        /// <summary>The one hero who won alone - rulebook-wise, Cutter's A Fistful of Coins.</summary>
        SoloWinner = 1,

        /// <summary>
        /// Everybody else. On a solo ending that includes the winner's OWN team-mates, which is what
        /// "you alone win the game" means and is the first ending in this build where two heroes on
        /// one team do not share a result.
        /// </summary>
        Defeated = 2
    }

    /// <summary>
    /// Milestone 8.53. Every word the victory screen prints that is not a hero's name or a number,
    /// stated once so the presenter, the view and the journeys that read them cannot drift apart.
    /// </summary>
    public static class VictoryScreenLabels
    {
        public const string WinnersTag = "WINNERS";
        public const string DefeatedTag = "DEFEATED";
        public const string SoloTag = "SOLO VICTORY";

        /// <summary>The columns, in the one order every block draws them.</summary>
        public static readonly IReadOnlyList<VictoryScreenColumn> Columns = Array.AsReadOnly(new[]
        {
            VictoryScreenColumn.Level,
            VictoryScreenColumn.Kills,
            VictoryScreenColumn.Deaths,
            VictoryScreenColumn.AttacksAtHeroes,
            VictoryScreenColumn.TimesDefended,
            VictoryScreenColumn.MinionsKilled,
            VictoryScreenColumn.CoinsEarned
        });

        /// <summary>`RED`, `BLUE` - the side's own name, with none of the strip's bracket prefix.</summary>
        public static string TeamName(TeamSide side) =>
            side == TeamSide.Red ? "RED" : "BLUE";

        /// <summary>The header over a team's own table.</summary>
        public static string TeamHeader(TeamSide side) => TeamName(side) + " TEAM";

        /// <summary>`RED WINS`, `CUTTER WINS` - the one line the whole screen is about.</summary>
        public static string Banner(string winner) =>
            (winner ?? string.Empty).ToUpperInvariant() + " WINS";

        /// <summary>The column's printed header, in the user's own words.</summary>
        public static string Header(VictoryScreenColumn column)
        {
            switch (column)
            {
                case VictoryScreenColumn.Level: return "Level";
                case VictoryScreenColumn.Kills: return "Kills";
                case VictoryScreenColumn.Deaths: return "Deaths";
                case VictoryScreenColumn.AttacksAtHeroes: return "Attacks at heroes";
                case VictoryScreenColumn.TimesDefended: return "Times defended";
                case VictoryScreenColumn.MinionsKilled: return "Minions Killed";
                case VictoryScreenColumn.CoinsEarned: return "Coins earned";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(column), column, "Every scoreboard column prints a header.");
            }
        }

        /// <summary>
        /// The column's own element-name suffix, so a journey addresses a cell by
        /// <c>gd-victory-cell-&lt;column&gt;</c> rather than by its position in a row.
        /// </summary>
        public static string ElementSuffix(VictoryScreenColumn column)
        {
            switch (column)
            {
                case VictoryScreenColumn.Level: return "level";
                case VictoryScreenColumn.Kills: return "kills";
                case VictoryScreenColumn.Deaths: return "deaths";
                case VictoryScreenColumn.AttacksAtHeroes: return "attacks-at-heroes";
                case VictoryScreenColumn.TimesDefended: return "times-defended";
                case VictoryScreenColumn.MinionsKilled: return "minions-killed";
                case VictoryScreenColumn.CoinsEarned: return "coins-earned";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(column), column, "Every scoreboard column is addressable by name.");
            }
        }

        /// <summary>The cell's own element name for one column.</summary>
        public static string CellName(VictoryScreenColumn column) =>
            "gd-victory-cell-" + ElementSuffix(column);

        /// <summary>The header cell's own element name for one column.</summary>
        public static string HeaderCellName(VictoryScreenColumn column) =>
            "gd-victory-head-" + ElementSuffix(column);

        /// <summary>
        /// One short phrase per column, plus the worth badge beside the name.
        ///
        /// <para><b>Two of the mockup's phrases are corrected against the authority that counts
        /// them.</b> "Minions Killed" is not "minions defeated by this hero's attacks" - milestone
        /// 8.53's accumulator counts a minion Defeat asked for by card text too. And "Times defended"
        /// is not narrowed to enemy HERO attacks: what is counted is every
        /// <c>AttackSuccessfullyDefended</c> whose target is this hero. The screen says what the
        /// number is, not what a mockup guessed it was.</para>
        /// </summary>
        public static IReadOnlyList<VictoryScreenLegendEntry> Legend { get; } = Array.AsReadOnly(new[]
        {
            new VictoryScreenLegendEntry("Level", "p.15's Hero Level as the match ended"),
            new VictoryScreenLegendEntry("Kills", "enemy heroes this hero defeated"),
            new VictoryScreenLegendEntry("Deaths", "times this hero was defeated"),
            new VictoryScreenLegendEntry(
                "Attacks at heroes", "attack actions that targeted an enemy hero, defended or not"),
            new VictoryScreenLegendEntry(
                "Times defended", "attacks on this hero that were successfully defended"),
            new VictoryScreenLegendEntry(
                "Minions Killed", "minions this hero defeated, by attack or by card text"),
            new VictoryScreenLegendEntry(
                "Coins earned", "every coin collected over the match, not what is held now"),
            new VictoryScreenLegendEntry("Worth", "what defeating this hero pays and costs")
        });
    }

    /// <summary>One term of the legend under the tables, and the phrase that explains it.</summary>
    public sealed class VictoryScreenLegendEntry
    {
        public VictoryScreenLegendEntry(string term, string description)
        {
            Term = string.IsNullOrWhiteSpace(term)
                ? throw new ArgumentException("A legend entry names a column.", nameof(term))
                : term;
            Description = string.IsNullOrWhiteSpace(description)
                ? throw new ArgumentException("A legend entry says what the column is.", nameof(description))
                : description;
        }

        public string Term { get; }
        public string Description { get; }
    }

    /// <summary>
    /// Milestone 8.53. One hero's number in one column.
    ///
    /// <para><see cref="IsBest"/> is the mockup's `best` class: the HIGHEST value in this column
    /// across EVERY hero on the screen, winners and defeated alike, with ties all marked. A column in
    /// which nothing happened to anybody - every value zero - marks nobody, because "joint best at
    /// nothing" is a statement about the match rather than about a player. Deaths is never marked:
    /// the hero defeated most often led nothing, and the approved mockup marks no Deaths cell.</para>
    /// </summary>
    public sealed class VictoryScreenCellViewModel
    {
        public VictoryScreenCellViewModel(VictoryScreenColumn column, int value, bool isBest)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Every scoreboard column counts occurrences.");
            }

            Column = column;
            Value = value;
            IsBest = isBest;
            Header = VictoryScreenLabels.Header(column);
            ElementName = VictoryScreenLabels.CellName(column);
            Text = value.ToString(CultureInfo.InvariantCulture);
        }

        public VictoryScreenColumn Column { get; }
        public string Header { get; }

        /// <summary>`gd-victory-cell-kills` - how a journey finds this cell.</summary>
        public string ElementName { get; }

        public int Value { get; }
        public string Text { get; }
        public bool IsBest { get; }
    }

    /// <summary>Milestone 8.53. One hero's line of the printed scoreboard.</summary>
    public sealed class VictoryScreenRowViewModel
    {
        public VictoryScreenRowViewModel(
            HeroId heroId,
            TeamSide teamSide,
            HeroContentId contentId,
            string displayName,
            int worth,
            IEnumerable<VictoryScreenCellViewModel> cells)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A scoreboard row names its hero.", nameof(heroId));
            }

            if (worth < 0 || worth > 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worth), worth, "p.13's table prices a hero at 1, 2 or 3, or not at all.");
            }

            HeroId = heroId;
            TeamSide = teamSide;
            ContentId = contentId;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Cells = Array.AsReadOnly((cells ?? throw new ArgumentNullException(nameof(cells))).ToArray());
            if (Cells.Select(cell => cell.Column).SequenceEqual(VictoryScreenLabels.Columns) == false)
            {
                throw new ArgumentException(
                    "A row draws every column once, in the one order the headers are printed in.",
                    nameof(cells));
            }

            Worth = worth;
            // Milestone 8.49's badge, unchanged: one digit that is both the assist coins a Defeat pays
            // and the Life counters it costs. Its words are the rail's words, because it is the rail's
            // badge.
            WorthLabel = worth == 0
                ? string.Empty
                : worth.ToString(CultureInfo.InvariantCulture);
            WorthAssistiveLabel = worth == 0
                ? string.Empty
                : $"Worth {worth}: defeating this hero pays {worth} assist coin" +
                    (worth == 1 ? string.Empty : "s") +
                    $" and costs their team {worth} life counter" + (worth == 1 ? string.Empty : "s");
            PortraitAssistiveLabel = DisplayName + ", " +
                (teamSide == TeamSide.Red ? "red hero" : "blue hero");
        }

        public HeroId HeroId { get; }
        public TeamSide TeamSide { get; }

        /// <summary>The printed set this hero is, which is what the token disc is addressed by.</summary>
        public HeroContentId ContentId { get; }

        public string DisplayName { get; }
        public int Worth { get; }
        public string WorthLabel { get; }
        public string WorthAssistiveLabel { get; }
        public string PortraitAssistiveLabel { get; }
        public IReadOnlyList<VictoryScreenCellViewModel> Cells { get; }

        public VictoryScreenCellViewModel Cell(VictoryScreenColumn column) =>
            Cells.First(cell => cell.Column == column);

        public int ValueOf(VictoryScreenColumn column) => Cell(column).Value;
    }

    /// <summary>Milestone 8.53. One table on the screen: whose it is, what it is tagged, and its rows.</summary>
    public sealed class VictoryScreenBlockViewModel
    {
        public VictoryScreenBlockViewModel(
            VictoryScreenBlockKind kind,
            TeamSide teamSide,
            string header,
            string tag,
            IEnumerable<VictoryScreenRowViewModel> rows)
        {
            Kind = kind;
            TeamSide = teamSide;
            Header = string.IsNullOrWhiteSpace(header)
                ? throw new ArgumentException("A block says whose table it is.", nameof(header))
                : header;
            Tag = string.IsNullOrWhiteSpace(tag)
                ? throw new ArgumentException("A block says how its side finished.", nameof(tag))
                : tag;
            Rows = Array.AsReadOnly((rows ?? throw new ArgumentNullException(nameof(rows))).ToArray());
            if (Rows.Count == 0)
            {
                throw new ArgumentException(
                    "A block with no heroes in it is not drawn at all.", nameof(rows));
            }
        }

        public VictoryScreenBlockKind Kind { get; }

        /// <summary>The side this block's heroes are on - the winner's own side for a solo block.</summary>
        public TeamSide TeamSide { get; }

        /// <summary>`RED TEAM`, or the solo winner's own name.</summary>
        public string Header { get; }

        /// <summary>`WINNERS`, `DEFEATED`, `SOLO VICTORY`.</summary>
        public string Tag { get; }

        public IReadOnlyList<VictoryScreenRowViewModel> Rows { get; }
    }

    /// <summary>
    /// Milestone 8.53. The whole end-of-match screen: who won, on what, and what everybody did.
    ///
    /// <para>Built identically for every seat and for the Director - the outcome and the scoreboard are
    /// public information in every projection - so there is no home-hero special-casing anywhere in it.
    /// A null model means the match has not ended and no screen may be shown.</para>
    /// </summary>
    public sealed class VictoryScreenViewModel
    {
        public VictoryScreenViewModel(
            long version,
            MatchVictoryCause cause,
            bool hasSoloWinner,
            string bannerText,
            Color bannerColor,
            string causeText,
            IEnumerable<VictoryScreenBlockViewModel> blocks)
        {
            if (cause == MatchVictoryCause.None)
            {
                throw new ArgumentException(
                    "A victory screen is built for a match that has actually ended.", nameof(cause));
            }

            Version = version;
            Cause = cause;
            HasSoloWinner = hasSoloWinner;
            BannerText = string.IsNullOrWhiteSpace(bannerText)
                ? throw new ArgumentException("The screen says who won.", nameof(bannerText))
                : bannerText;
            BannerColor = bannerColor;
            CauseText = string.IsNullOrWhiteSpace(causeText)
                ? throw new ArgumentException("The screen says what won it.", nameof(causeText))
                : causeText;
            Blocks = Array.AsReadOnly((blocks ?? throw new ArgumentNullException(nameof(blocks))).ToArray());
            if (Blocks.Count == 0)
            {
                throw new ArgumentException(
                    "An ended match has at least the block that won it.", nameof(blocks));
            }

            Legend = VictoryScreenLabels.Legend;
        }

        public long Version { get; }
        public MatchVictoryCause Cause { get; }

        /// <summary>Whether one hero won alone, in which case the first block is theirs and nobody else's.</summary>
        public bool HasSoloWinner { get; }

        /// <summary>`RED WINS` / `BLUE WINS` / `CUTTER WINS`.</summary>
        public string BannerText { get; }

        /// <summary>The team's own colour, or the coin's gold for a solo ending.</summary>
        public Color BannerColor { get; }

        /// <summary>The rulebook sentence that ended it, in the words the status line has been saying.</summary>
        public string CauseText { get; }

        /// <summary>The winners' block first, then everybody else.</summary>
        public IReadOnlyList<VictoryScreenBlockViewModel> Blocks { get; }

        public IReadOnlyList<VictoryScreenLegendEntry> Legend { get; }

        /// <summary>Every row on the screen, in the order the blocks print them.</summary>
        public IEnumerable<VictoryScreenRowViewModel> Rows => Blocks.SelectMany(block => block.Rows);
    }

    /// <summary>
    /// Milestone 8.53. The passive port for the victory screen. A null model means the match is still
    /// being played, and therefore that no screen may be shown and nothing of the HUD may be hidden.
    /// </summary>
    public interface IVictoryScreenView
    {
        void Render(VictoryScreenViewModel model);
    }
}
