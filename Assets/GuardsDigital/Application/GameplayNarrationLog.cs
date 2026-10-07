using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GuardsDigital.Core.Diagnostics;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.35z61. One seat's line in the host's on-disk gameplay narration: who,
    /// when, at which projection version, and what happened in words.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this exists.</b> The 2026-09-06 six-player playtest could not be diagnosed
    /// afterwards. The host wrote lobby and wire markers only - connections, seat claims, opaque
    /// submission ids - and nothing about the game. When a player reported that his card had been
    /// passed over, there was no record anywhere on disk of what any hero had played, what resolved,
    /// or why. The submission ids could not close the gap either: they are hashes of a transport
    /// nonce and carry no command content, so the wire log cannot be decoded into a match.</para>
    /// <para><b>The bar this is written to</b> is that the game is obvious from the log alone: a
    /// reader holding only the file can reconstruct the match, including every moment the engine
    /// resolved something WITHOUT asking a player for it. That last case is the one the playtest
    /// turned on, and it is why <see cref="MatchLogEntryKind.HeroCardResolvedWithoutAction"/> is
    /// narrated with its cause rather than as a bare fact.</para>
    /// </remarks>
    public readonly struct GameplaySeatIntroduction
    {
        public GameplaySeatIntroduction(string seatId, string heroId, bool isOccupied)
        {
            SeatId = seatId ?? throw new ArgumentNullException(nameof(seatId));
            HeroId = heroId ?? throw new ArgumentNullException(nameof(heroId));
            IsOccupied = isOccupied;
        }

        public string SeatId { get; }
        public string HeroId { get; }
        public bool IsOccupied { get; }
    }

    /// <summary>
    /// Formats the host's gameplay narration. Pure text composition over
    /// <see cref="MatchLogEntry"/> - the same seat-public rows the in-game tray renders, so the file
    /// and the tray cannot come to disagree about what happened.
    /// </summary>
    /// <remarks>
    /// The sink states MORE than the tray rather than something different: every row carries its
    /// wall clock, the projection version it was observed at, the round and turn, the row's own kind,
    /// and the structured identities behind the sentence. The tray elides those because a player is
    /// watching the board; a reader holding only this file is not.
    /// </remarks>
    public static class GameplayNarrationLog
    {
        /// <summary>
        /// The format marker, first line of every file. It is versioned because a reader written
        /// against these rows is entitled to know when their shape changed.
        /// </summary>
        public const string FormatVersion = "guards-gameplay-log/2";

        private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        /// <summary>
        /// The file's opening block: what build produced it, on what map, and who was sitting where.
        /// Written once, before any row, so a reader never has to infer the cast from the play.
        /// </summary>
        public static IReadOnlyList<string> Header(
            DateTimeOffset startedAt,
            string buildIdentity,
            string mapIdentity,
            IEnumerable<GameplaySeatIntroduction> seats)
        {
            if (seats == null) throw new ArgumentNullException(nameof(seats));

            var lines = new List<string>
            {
                "# " + FormatVersion,
                "# started=" + Stamp(startedAt),
                "# build=" + Blank(buildIdentity),
                "# map=" + Blank(mapIdentity)
            };

            foreach (var seat in seats.OrderBy(seat => seat.SeatId, StringComparer.Ordinal))
            {
                lines.Add(
                    "# seat " + seat.SeatId + " hero=" + seat.HeroId +
                    " occupancy=" + (seat.IsOccupied ? "held" : "open"));
            }

            lines.Add("# ---- match begins ----");
            return lines;
        }

        /// <summary>
        /// One narrated row. <paramref name="observedVersion"/> is the projection version the row was
        /// read at, which is what ties a line here to a submission in the wire log beside it.
        /// </summary>
        public static string Row(DateTimeOffset at, long observedVersion, MatchLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var builder = new StringBuilder();
            builder.Append(Stamp(at));
            builder.Append(" v").Append(Int(observedVersion));
            builder.Append(" r").Append(Int(entry.RoundIndex));
            builder.Append(" t").Append(Int(entry.TurnIndex));
            builder.Append(" #").Append(Int(entry.Sequence));
            builder.Append(' ').Append(entry.Kind);
            builder.Append(" | ").Append(Describe(entry));

            var facts = Facts(entry).ToArray();
            if (facts.Length > 0)
            {
                builder.Append(" | ").Append(string.Join(" ", facts));
            }

            return builder.ToString();
        }

        /// <summary>
        /// One completed player decision window, using actionable game-clock time rather than the
        /// wall-clock stamp at the start of this row.
        /// </summary>
        public static string DecisionWindow(
            DateTimeOffset at,
            long observedVersion,
            DecisionWindowMeasurement window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            var command = window.ClosingCommand.HasValue
                ? window.ClosingCommand.Value.ToString()
                : "none";
            return Stamp(at) + " v" + Int(observedVersion) + " -- DECISION #" + Int(window.Sequence) +
                " | " + Blank(window.Hero.Value) + " took " +
                window.ActiveSeconds.ToString("0.000", CultureInfo.InvariantCulture) + "s for " +
                Blank(window.Context) + "; closed by " + window.CloseReason + " (" + command + ")" +
                " | seat=" + Blank(window.Seat.Value) +
                " hero=" + Blank(window.Hero.Value) +
                " team=" + window.Team +
                " kind=" + window.Kind +
                " context=" + Token(window.Context) +
                " durationTicks=" + window.ActiveTicks.ToString(CultureInfo.InvariantCulture) +
                " openedVersion=" + window.OpenedVersion.ToString(CultureInfo.InvariantCulture) +
                " closedVersion=" + window.ClosedVersion.ToString(CultureInfo.InvariantCulture) +
                " close=" + window.CloseReason +
                " command=" + command;
        }

        private static string Token(string value) =>
            Blank(value).Replace(" ", "_");

        /// <summary>
        /// A session-layer row - a refusal, a resync, a reconnect - narrated inline among the
        /// gameplay rows rather than only in the wire log.
        /// </summary>
        /// <remarks>
        /// These are invisible to players today, and that invisibility is exactly what made the
        /// 2026-09-06 playtest undiagnosable: a refused submission produces no gameplay row at all,
        /// so a seat whose input was rejected looks identical to a seat that was never asked.
        /// Interleaving them here is what lets a reader tell those two apart.
        /// </remarks>
        public static string SessionRow(
            DateTimeOffset at, long observedVersion, string marker, string detail) =>
            Stamp(at) + " v" + Int(observedVersion) + " -- SESSION " +
            Blank(marker) + (string.IsNullOrEmpty(detail) ? string.Empty : " | " + detail);

        /// <summary>The closing line, naming how the match ended.</summary>
        public static string Outcome(DateTimeOffset at, long observedVersion, string cause, string winner) =>
            Stamp(at) + " v" + Int(observedVersion) + " ==== MATCH OVER cause=" + Blank(cause) +
            " winner=" + Blank(winner) + " ====";

        /// <summary>
        /// The row in player language. Every member of the vocabulary is answered here, and the
        /// default arm names the kind rather than dropping it - a row this method had no words for
        /// would otherwise be a hole in the file of exactly the kind this whole sink exists to close.
        /// </summary>
        public static string Describe(MatchLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var hero = Name(entry.SubjectHeroId.IsDefined ? entry.SubjectHeroId.Value : null);
            var target = Name(entry.TargetHeroId.IsDefined ? entry.TargetHeroId.Value : null);
            var minion = entry.SubjectMinionId.IsDefined ? entry.SubjectMinionId.Value : "a minion";
            var team = entry.SubjectTeamId.IsDefined ? entry.SubjectTeamId.Value : "a team";

            switch (entry.Kind)
            {
                case MatchLogEntryKind.RoundStarted:
                    return "---- ROUND " + Int(entry.RoundIndex) + " ----";
                case MatchLogEntryKind.RoundCompleted:
                    return "---- end of round " + Int(entry.RoundIndex) + " ----";
                case MatchLogEntryKind.TurnStarted:
                    return "-- turn " + Int(entry.TurnIndex) + " begins";
                case MatchLogEntryKind.TurnEnded:
                    return "-- turn " + Int(entry.TurnIndex) + " ends";

                case MatchLogEntryKind.CardRevealed:
                    return hero + " revealed " + Card(entry);
                // Milestone 8.40f. Card text turned a card out of a hand face up for the table.
                case MatchLogEntryKind.CardRevealedByCardText:
                    return hero + " revealed " + Card(entry) + " from their hand";
                // Milestone 9.19. Card text sent a card out of a hand to the discard pile.
                case MatchLogEntryKind.CardDiscardedByCardText:
                    return hero + " discarded " + Card(entry) + " from their hand";
                // Milestone 9.30h. A spellbook's three public facts. A cast and a removal name the
                // spell, which lies faceup as the row is written; a refill names how many, never which.
                case MatchLogEntryKind.SpellCast:
                    return hero + " cast " + Card(entry);
                case MatchLogEntryKind.SpellRemovedFaceup:
                    return hero + " removed " + Card(entry) + " from their spellbook faceup";
                case MatchLogEntryKind.SpellbookRefilled:
                    return hero + " returned " + Int(entry.Amount) +
                        (entry.Amount == 1 ? " spell" : " spells") + " to their spellbook";
                // Milestone 9.32g. A swap, a kept swap and an Ultimate's unlock that moved cards,
                // each move as this seat's row states it: a card it may not name stays unnamed.
                case MatchLogEntryKind.CardSwappedByCardText:
                    return hero + " swapped " + Moves(entry) + " by " + Card(entry);
                case MatchLogEntryKind.CardKeptByCardText:
                    return hero + " kept " + Moves(entry) + " against " + Card(entry);
                case MatchLogEntryKind.UltimateMovedCards:
                    return hero + " unlocked " + Card(entry) + ", moving " + Moves(entry);
                case MatchLogEntryKind.Move:
                    return hero + " moved" + Journey(entry);
                case MatchLogEntryKind.FastTravel:
                    return hero + " fast travelled" + Journey(entry);
                case MatchLogEntryKind.Attack:
                    return hero + " attacked " + target + OutcomeText(entry);
                case MatchLogEntryKind.Defense:
                    return hero + " answered " + target + "'s attack" + OutcomeText(entry);
                case MatchLogEntryKind.SkillUsed:
                    return hero + " used a skill";
                case MatchLogEntryKind.Clear:
                    return hero + " cleared a token" + At(entry);

                // Milestone 8.61e. The reveal, in words: which hero walked over it, where, and which
                // kind it turned out to be. `Amount` is the number of cards the removal costs, which is
                // exactly the blast-or-dud the printed sentence distinguishes.
                case MatchLogEntryKind.MoveThroughTrapSprung:
                    return hero + " moved through " +
                        (entry.Amount > 0 ? "a blast" : "a dud") + At(entry);

                // The blocked action and the silent auto-resolve are the two rows that say why
                // nothing happened. They are the reason a reader can answer "did it resolve, and if
                // not why" - the question the 2026-09-06 playtest could not answer from any file.
                case MatchLogEntryKind.BlockedAction:
                    return hero + "'s action was BLOCKED" +
                        (entry.BlockingHeroId.IsDefined
                            ? " by " + Name(entry.BlockingHeroId.Value)
                            : string.Empty);
                case MatchLogEntryKind.HeroCardResolvedWithoutAction:
                    return hero + "'S CARD RESOLVED WITH NO ACTION - they were DEFEATED before " +
                        "their card came up, so rulebook p.13 resolved it without an action and " +
                        "they were never asked for anything";

                // Milestone 8.51. The third row that says why nothing happened, and the one the
                // 2026-09-18 playtest lacked: the seat's OWN command resolved the card with no
                // action. Stated as a press because that is the only way a seat performs a Hold -
                // END TURN on an untouched card composes exactly this command - so a reader can
                // tell "the player ended their turn" from "the engine passed them over" above.
                case MatchLogEntryKind.CardHeld:
                    return hero + " HELD " + Card(entry) + " - resolved with NO ACTION by the " +
                        "seat's own END TURN press on an untouched card (rulebook p.12's Hold); " +
                        "the engine never holds a card by itself";

                case MatchLogEntryKind.MinionDefeated:
                    return minion + " was defeated";
                case MatchLogEntryKind.MinionRemoved:
                    return minion + " was removed";
                case MatchLogEntryKind.MinionRemovalPrevented:
                    return minion + "'s removal was prevented";
                case MatchLogEntryKind.MinionBattleMinionRemoved:
                    return minion + " was removed by the minion battle";
                case MatchLogEntryKind.DisplacedMinionCorrected:
                    return minion + " was corrected onto a legal space" + Journey(entry);

                case MatchLogEntryKind.HeroDefeated:
                    return hero + " was defeated";
                case MatchLogEntryKind.HeroRemoved:
                    return hero + " was removed from the board";
                case MatchLogEntryKind.HeroRemovalPrevented:
                    return hero + "'s removal was prevented";
                case MatchLogEntryKind.HeroRespawned:
                    return hero + " respawned" + At(entry);

                case MatchLogEntryKind.TeamLifeSpent:
                    return hero + "'s team spent a life";
                case MatchLogEntryKind.TeamEliminated:
                    return hero + "'s team was eliminated";
                case MatchLogEntryKind.LanePush:
                    return "the lane was pushed against " + team;

                case MatchLogEntryKind.LevelPurchased:
                    return hero + " bought level " + Int(entry.Amount);
                case MatchLogEntryKind.CardUpgraded:
                    return hero + " upgraded " + Card(entry);
                case MatchLogEntryKind.ItemGained:
                    return hero + " gained an item";
                case MatchLogEntryKind.PityCoinCollected:
                    return hero + " collected " + Int(entry.Amount) + " coin(s)";
                case MatchLogEntryKind.CoinAwarded:
                    return hero + " gained " + Int(entry.Amount) +
                        (entry.Amount == 1 ? " coin" : " coins");

                case MatchLogEntryKind.GameEnded:
                    return "the match ended; " + team + " won";

                default:
                    return "unnarrated row of kind " + entry.Kind +
                        " (this sink has no words for it yet, which is a gap to close)";
            }
        }

        /// <summary>
        /// The structured identities behind the sentence, so the file is machine-readable as well as
        /// legible and a reader never has to guess which Brogan a sentence meant.
        /// </summary>
        private static IEnumerable<string> Facts(MatchLogEntry entry)
        {
            if (entry.SubjectHeroId.IsDefined) yield return "hero=" + entry.SubjectHeroId.Value;
            if (entry.TargetHeroId.IsDefined) yield return "target=" + entry.TargetHeroId.Value;
            if (entry.SubjectMinionId.IsDefined) yield return "minion=" + entry.SubjectMinionId.Value;
            if (entry.SubjectTeamId.IsDefined) yield return "team=" + entry.SubjectTeamId.Value;
            if (entry.SubjectCardInstanceId.IsDefined)
            {
                yield return "card=" + entry.SubjectCardInstanceId.Value;
            }

            if (entry.BlockingHeroId.IsDefined)
            {
                yield return "blockingHero=" + entry.BlockingHeroId.Value;
            }

            if (entry.BlockingCardInstanceId.IsDefined)
            {
                yield return "blockingCard=" + entry.BlockingCardInstanceId.Value;
            }

            if (entry.HasOrigin) yield return "from=" + entry.Origin;
            if (entry.HasDestination) yield return "to=" + entry.Destination;
            if (entry.Amount != 0) yield return "amount=" + Int(entry.Amount);
            if (entry.Outcome != MatchLogOutcome.None) yield return "outcome=" + entry.Outcome;
        }

        private static string Journey(MatchLogEntry entry)
        {
            if (entry.HasOrigin && entry.HasDestination)
            {
                return " from " + entry.Origin + " to " + entry.Destination;
            }

            return entry.HasDestination ? " to " + entry.Destination : string.Empty;
        }

        private static string At(MatchLogEntry entry) =>
            entry.HasOrigin ? " at " + entry.Origin : string.Empty;

        private static string OutcomeText(MatchLogEntry entry) =>
            entry.Outcome == MatchLogOutcome.None
                ? string.Empty
                : " - " + entry.Outcome;

        private static string Card(MatchLogEntry entry) =>
            entry.SubjectCardInstanceId.IsDefined ? entry.SubjectCardInstanceId.Value : "a card";

        /// <summary>Milestone 9.32g. "definition (From -> To)" per move, "a gold card" where unnamed.</summary>
        private static string Moves(MatchLogEntry entry) =>
            string.Join(" and ", entry.CardMoves.Select(move =>
                (move.Definition.IsDefined
                    ? move.Definition.Value
                    : move.KnownColor != GuardsDigital.Core.Cards.CardColor.None
                        ? "a " + move.KnownColor.ToString().ToLowerInvariant() + " card"
                        : "a card") +
                " (" + move.From + " -> " + move.To +
                (move.ArrivesFacedown ? ", facedown" : string.Empty) + ")"));

        private static string Name(string heroId) =>
            string.IsNullOrEmpty(heroId) ? "an unnamed hero" : heroId;

        private static string Stamp(DateTimeOffset at) =>
            at.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture);

        private static string Int(long value) =>
            value.ToString(CultureInfo.InvariantCulture);

        private static string Blank(string value) =>
            string.IsNullOrWhiteSpace(value) ? "(unstated)" : value;
    }
}
