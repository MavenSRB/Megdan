using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Application;

using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z61. The host's on-disk gameplay narration: that it exists, that the
    /// game is obvious from it, and that it says so when the engine passes a seat over.
    /// </summary>
    /// <remarks>
    /// The acceptance bar these cases are written to is the user's: a person holding ONLY the log
    /// file can reconstruct the match. The 2026-09-06 playtest is the counter-example the bar was set
    /// against - the host wrote lobby and wire markers and nothing about the game, so when a player
    /// reported being passed over there was no record on disk of what anyone had played.
    /// </remarks>
    public sealed class GameplayNarrationLogTests
    {
        private static readonly DateTimeOffset Noon =
            new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// THE LITMUS CASE, stated as the question the playtest could not answer: "did Brogan's card
        /// resolve, and if not, why". A reader holding only the file must be able to answer it.
        /// </summary>
        [Test]
        public void TheLog_AnswersWhyASeatWasPassedOver()
        {
            var brogan = new HeroId("hero-brogan");
            var line = GameplayNarrationLog.Row(
                Noon, 276,
                new MatchLogEntry(
                    41, MatchLogEntryKind.HeroCardResolvedWithoutAction, 4, 2,
                    subjectHeroId: brogan));

            Assert.That(line, Does.Contain("hero-brogan"),
                "The row must name the player it is about.");
            Assert.That(line, Does.Contain("NO ACTION"),
                "The row must say the card resolved without an action - the fact.");
            Assert.That(line, Does.Contain("DEFEATED"),
                "The row must say WHY, which is the half the playtest needed and no file had.");
            Assert.That(line, Does.Contain("v276").And.Contain("r4"),
                "The row is tied to the projection version and round it happened in, so it can be " +
                "lined up against the wire log beside it.");
        }

        /// <summary>
        /// Milestone 8.51. The 2026-09-18 report, stated as the question the host file could not
        /// answer that night: "did Silverarrow's card resolve, and who resolved it". A Hold row
        /// names the hero, the card, that no action was taken, and that the seat's own press did it.
        /// </summary>
        [Test]
        public void TheLog_SaysWhenASeatHeldItsOwnCard()
        {
            var line = GameplayNarrationLog.Row(
                Noon, 730,
                new MatchLogEntry(
                    2473, MatchLogEntryKind.CardHeld, 6, 2,
                    subjectHeroId: new HeroId("hero-silverarrow"),
                    subjectCardInstanceId: new CardInstanceId("red-02-silverarrow-warning-shot")));

            Assert.That(line, Does.Contain("hero-silverarrow"), "The row names the player it is about.");
            Assert.That(line, Does.Contain("red-02-silverarrow-warning-shot"), "and the card they held.");
            Assert.That(line, Does.Contain("HELD").And.Contain("NO ACTION"), "and the fact.");
            Assert.That(line, Does.Contain("END TURN"),
                "and WHOSE doing it was - the seat's own press, never the engine's.");
            Assert.That(line, Does.Contain("v730").And.Contain("r6").And.Contain("t2"));
        }

        /// <summary>
        /// Milestone 9.19. A card-text discard (Charged Runestone, the Seismic faces, Trample) had no
        /// words at all; the row now says who discarded which card out of their hand.
        /// </summary>
        [Test]
        public void TheLog_SaysWhenCardTextMadeAHeroDiscard()
        {
            var line = GameplayNarrationLog.Row(
                Noon, 731,
                new MatchLogEntry(
                    2474, MatchLogEntryKind.CardDiscardedByCardText, 6, 2,
                    subjectHeroId: new HeroId("hero-bain"),
                    subjectCardInstanceId: new CardInstanceId("blue-01-bain-drinking-buddies")));

            Assert.That(line, Does.Contain("hero-bain"), "The row names the card's owner.");
            Assert.That(line, Does.Contain("blue-01-bain-drinking-buddies"), "and the card that left his hand.");
            Assert.That(line, Does.Contain("DISCARDED").IgnoreCase.And.Contain("FROM THEIR HAND").IgnoreCase,
                "and the fact.");
            Assert.That(line, Does.Not.Contain("REVEALED").IgnoreCase,
                "A discard is not a reveal: the card changed state, it was not merely shown.");
        }

        [Test]
        public void TheLog_NamesTheAuthoritativeCoinAwardAmount()
        {
            var line = GameplayNarrationLog.Row(
                Noon,
                731,
                new MatchLogEntry(
                    2474,
                    MatchLogEntryKind.CoinAwarded,
                    6,
                    2,
                    subjectHeroId: new HeroId("hero-brogan"),
                    amount: 2));

            Assert.That(line, Does.Contain("hero-brogan"));
            Assert.That(line, Does.Contain("2 coins"));
        }

        /// <summary>
        /// Milestone 8.51. A row keeps the rule-event sequence it was derived from, and most rule
        /// events derive no row, so consecutive rows are rarely consecutive numbers: the
        /// 2026-09-18 host file reported 218 such gaps as losses, and none was. A gap is a loss
        /// only when the window's oldest retained row was itself never written.
        /// </summary>
        [Test]
        public void TheRecorder_DoesNotCallAnInherentSequenceGapALoss()
        {
            var sink = new StringWriter();
            var recorder = new GameplayNarrationRecorder(sink, () => Noon, ownsWriter: false);
            var first = new MatchLogEntry(1, MatchLogEntryKind.RoundStarted, 1, 0);
            var third = new MatchLogEntry(3, MatchLogEntryKind.TurnStarted, 1, 1);
            var ninth = new MatchLogEntry(9, MatchLogEntryKind.TurnEnded, 1, 1);

            Assert.That(recorder.Observe(1, new[] { first, third }), Is.EqualTo(2));
            Assert.That(recorder.Observe(2, new[] { first, third, ninth }), Is.EqualTo(1));

            Assert.That(sink.ToString(), Does.Not.Contain("NARRATION_GAP"),
                "Sequences 2 and 4..8 were rule events that derived no row; the window still holds " +
                "row 1, so nothing left it unwritten.");
            Assert.That(recorder.RowsWritten, Is.EqualTo(3));
        }

        /// <summary>
        /// Every row the contract can publish has words. A kind this sink had no sentence for would
        /// be a hole in the file of exactly the kind the sink exists to close, so the default arm is
        /// asserted to be unreachable across the whole vocabulary.
        /// </summary>
        [Test]
        public void EveryKindInTheVocabulary_HasASentence()
        {
            var unnarrated = new List<string>();
            foreach (MatchLogEntryKind kind in Enum.GetValues(typeof(MatchLogEntryKind)))
            {
                if (kind == MatchLogEntryKind.None)
                {
                    continue;
                }

                var described = GameplayNarrationLog.Describe(
                    new MatchLogEntry(1, kind, 1, 1, subjectHeroId: new HeroId("hero-brogan")));
                if (described.IndexOf("unnarrated", StringComparison.Ordinal) >= 0)
                {
                    unnarrated.Add(kind.ToString());
                }
            }

            Assert.That(unnarrated, Is.Empty,
                "These rows would reach the file with no words: " + string.Join(", ", unnarrated));
        }

        /// <summary>The header names the cast, so a reader never infers who was playing whom.</summary>
        [Test]
        public void TheHeader_NamesTheBuildTheMapAndEverySeat()
        {
            var header = GameplayNarrationLog.Header(
                Noon, "Playtest-8.35z14-wave", "official-single-lane",
                new[]
                {
                    new GameplaySeatIntroduction("blue-player-02", "hero-brogan", true),
                    new GameplaySeatIntroduction("red-player-02", "hero-xargatha", false)
                });

            var text = string.Join("\n", header);
            Assert.That(text, Does.Contain(GameplayNarrationLog.FormatVersion));
            Assert.That(text, Does.Contain("Playtest-8.35z14-wave"));
            Assert.That(text, Does.Contain("official-single-lane"));
            Assert.That(text, Does.Contain("blue-player-02").And.Contain("hero-brogan"));
            Assert.That(text, Does.Contain("occupancy=held"), "A seat a connection holds is named as held.");
            Assert.That(text, Does.Contain("occupancy=open"), "So is one nobody holds.");
        }

        /// <summary>
        /// The recorder writes each row once and flushes as it goes, so a match that crashes keeps
        /// everything up to the crash. Re-observing the same log - which a resync causes - adds
        /// nothing.
        /// </summary>
        [Test]
        public void TheRecorder_WritesEachRowOnceAndFlushesAsItGoes()
        {
            var sink = new StringWriter();
            var recorder = new GameplayNarrationRecorder(sink, () => Noon, ownsWriter: false);

            var log = new[]
            {
                new MatchLogEntry(1, MatchLogEntryKind.RoundStarted, 1, 0),
                new MatchLogEntry(2, MatchLogEntryKind.HeroDefeated, 1, 1,
                    subjectHeroId: new HeroId("hero-brogan"))
            };

            Assert.That(recorder.Observe(10, log), Is.EqualTo(2));
            Assert.That(sink.ToString(), Does.Contain("hero-brogan"),
                "Flushed on write, so the row is readable before the match ends.");

            Assert.That(recorder.Observe(10, log), Is.EqualTo(0),
                "A re-served projection - what a resync produces - must not duplicate rows.");
            Assert.That(recorder.RowsWritten, Is.EqualTo(2));
        }

        /// <summary>
        /// The retained match log is a bounded window. A recorder that stepped over rows says so in
        /// the file rather than closing the gap silently.
        /// </summary>
        [Test]
        public void TheRecorder_ReportsRowsItLostRatherThanHidingThem()
        {
            var sink = new StringWriter();
            var recorder = new GameplayNarrationRecorder(sink, () => Noon, ownsWriter: false);

            recorder.Observe(1, new[] { new MatchLogEntry(1, MatchLogEntryKind.RoundStarted, 1, 0) });
            recorder.Observe(9, new[] { new MatchLogEntry(7, MatchLogEntryKind.TurnEnded, 1, 3) });

            Assert.That(sink.ToString(), Does.Contain("NARRATION_GAP"),
                "A narration that quietly omits rows is the failure this sink was built to end.");
            Assert.That(sink.ToString(), Does.Contain("2..6"), "The lost range is named.");
        }

        /// <summary>
        /// A real driven match produces a file, and it carries one row per match-log row - the sink
        /// and the in-game tray read the same seat-public rows, so they cannot disagree about what
        /// happened.
        /// </summary>
        [Test]
        public void ADrivenMatch_NarratesEveryRowTheTrayWouldShow()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 260);

            var projection = host.Projection;
            var sink = new StringWriter();
            var recorder = new GameplayNarrationRecorder(sink, () => Noon, ownsWriter: false);
            recorder.WriteHeader("test-build", "official-single-lane",
                Array.Empty<GameplaySeatIntroduction>());
            recorder.Observe(projection.Version, projection.MatchLog);

            Assert.That(projection.MatchLog.Count, Is.GreaterThan(0),
                "The driven match produced no log at all, so this case proves nothing.");
            Assert.That(recorder.RowsWritten, Is.EqualTo(projection.MatchLog.Count),
                "Every row the tray would show reaches the file.");

            var lines = sink.ToString()
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
                .ToArray();
            Assert.That(lines.Length, Is.EqualTo(projection.MatchLog.Count));
            Assert.That(lines.All(line => line.Contains("v") && line.Contains("2026-09-07")), Is.True,
                "Every row carries its wall clock and the projection version it was observed at.");
        }

        [Test]
        public void DecisionRowsAreReadableMachineParseableAndWrittenOnce()
        {
            var measurement=new DecisionWindowMeasurement(
                1,"commitment:1:0:hero-wuk",MatchClockKind.Commitment,"Card commitment",
                new SeatId("blue-player-01"),new HeroId("hero-wuk"),TeamSide.Blue,
                TimeSpan.FromMilliseconds(8420).Ticks,17,18,
                DecisionWindowCloseReason.PlayerCommand,DirectorCommandKind.CommitCard);
            var line=GameplayNarrationLog.DecisionWindow(Noon,18,measurement);
            Assert.That(line,Does.Contain("hero-wuk took 8.420s for Card commitment"));
            Assert.That(line,Does.Contain("durationTicks=84200000"));
            Assert.That(line,Does.Contain("context=Card_commitment"));
            Assert.That(line,Does.Contain("close=PlayerCommand"));
            Assert.That(line,Does.Contain("command=CommitCard"));

            var sink=new StringWriter();
            var recorder=new GameplayNarrationRecorder(sink,()=>Noon,ownsWriter:false);
            Assert.That(recorder.ObserveDecisionWindows(18,new[]{measurement}),Is.EqualTo(1));
            Assert.That(recorder.ObserveDecisionWindows(18,new[]{measurement}),Is.Zero);
            Assert.That(recorder.DecisionRowsWritten,Is.EqualTo(1));
        }
        [Test]
        public void PrivateHostNarrationIncludesTheCompletedDecisionRows()
        {
            var host=MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var wire=PrivateOnlineWireHarness.Compose(host);
            var sink=new StringWriter();
            var recorder=new GameplayNarrationRecorder(sink,()=>Noon,ownsWriter:false);
            wire.Session.AttachGameplayNarration(recorder,"test-build","official-single-lane");
            host.SetTimerStarted(true);
            foreach(var seat in host.Seats.Seats)
                host.SetClockReadiness(seat.Id,host.Version,true);
            host.AdvanceClock(TimeSpan.FromMilliseconds(750).Ticks);
            var acting=host.Seats.Seats[0];
            var command=host.ProjectionForSeat(acting.Id).AuthorizedCommands
                .First(value=>value.Kind==DirectorCommandKind.CommitCard);
            Assert.That(host.Submit(MatchSubmission.ForCommand(command)).Accepted,Is.True);
            wire.Session.RepublishProjections();

            Assert.That(sink.ToString(),Does.Contain("-- DECISION #"));
            Assert.That(sink.ToString(),Does.Contain("durationTicks=7500000"));
            Assert.That(recorder.DecisionRowsWritten,Is.EqualTo(1));
        }
    }
}
