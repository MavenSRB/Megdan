using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Diagnostics;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.35z61. Appends the host's gameplay narration to a sink, line by line, as
    /// the match happens.
    /// </summary>
    /// <remarks>
    /// <para><b>It flushes every write.</b> The file exists to explain a session that went wrong, and
    /// the sessions most worth explaining are the ones that ended badly - so a buffered tail lost to
    /// a crash would lose exactly the rows a reader came for. Flushing per row is affordable here:
    /// rows are produced at human speed, a few hundred over a whole match.</para>
    /// <para><b>It observes rather than subscribes.</b> The recorder is handed a projection whenever
    /// the host publishes one and emits whatever rows it has not emitted before, identified by
    /// <see cref="MatchLogEntry.Sequence"/>. That keeps it a pure reader of the authoritative read
    /// model, with no second event path to fall out of step with, and makes it correct under
    /// duplicate publishes and under a resync that re-serves a version already seen.</para>
    /// <para><b>It says so when it loses rows.</b> The projection's match log is a bounded window -
    /// <see cref="MatchLogProjection.RetainedEntries"/> - so a host that observed too rarely could
    /// step over rows that had already scrolled out. A gap between the last sequence written and the
    /// first now offered is reported in the file rather than closed silently, because a narration
    /// that quietly omits rows is the failure this whole sink was built to end.</para>
    /// </remarks>
    public sealed class GameplayNarrationRecorder : IDisposable
    {
        private readonly TextWriter writer;
        private readonly Func<DateTimeOffset> clock;
        private readonly bool ownsWriter;
        private int lastWrittenSequence;
        private int lastDecisionSequence;
        private bool headerWritten;

        public GameplayNarrationRecorder(
            TextWriter writer,
            Func<DateTimeOffset> clock = null,
            bool ownsWriter = true)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.clock = clock ?? (() => DateTimeOffset.UtcNow);
            this.ownsWriter = ownsWriter;
        }

        /// <summary>How many narration rows this recorder has written, header excluded.</summary>
        public int RowsWritten { get; private set; }

        /// <summary>The opening block. Written once; a second call is ignored.</summary>
        public void WriteHeader(
            string buildIdentity,
            string mapIdentity,
            IEnumerable<GameplaySeatIntroduction> seats)
        {
            if (headerWritten)
            {
                return;
            }

            headerWritten = true;
            foreach (var line in GameplayNarrationLog.Header(
                clock(), buildIdentity, mapIdentity, seats))
            {
                WriteLine(line);
            }
        }

        /// <summary>
        /// Emits every row of <paramref name="matchLog"/> this recorder has not written yet.
        /// Returns how many it wrote.
        /// </summary>
        public int Observe(long observedVersion, IReadOnlyList<MatchLogEntry> matchLog)
        {
            if (matchLog == null) throw new ArgumentNullException(nameof(matchLog));

            var fresh = matchLog
                .Where(entry => entry != null && entry.Sequence > lastWrittenSequence)
                .OrderBy(entry => entry.Sequence)
                .ToArray();
            if (fresh.Length == 0)
            {
                return 0;
            }

            // Milestone 8.51. A row keeps the RULE-EVENT sequence it was derived from, and most
            // rule events derive no row, so consecutive rows are almost never consecutive numbers:
            // the 2026-09-18 host file reported 218 "gaps" of that kind and had lost nothing. A
            // loss is possible only when the window's OLDEST retained row is itself one this
            // recorder never wrote - then whatever left the window since the last write is gone.
            // While the oldest retained row is at or behind the last written one, every row that
            // ever left the window was written first, and a numbering gap is just the events in
            // between that derived no row.
            var oldestRetained = matchLog
                .Where(entry => entry != null)
                .Min(entry => entry.Sequence);
            if (lastWrittenSequence > 0 && oldestRetained > lastWrittenSequence + 1)
            {
                WriteLine(GameplayNarrationLog.SessionRow(
                    clock(), observedVersion, "NARRATION_GAP",
                    "rows " + (lastWrittenSequence + 1).ToString(CultureInfo.InvariantCulture) +
                    ".." + (fresh[0].Sequence - 1).ToString(CultureInfo.InvariantCulture) +
                    " left the retained window before they were written (rule-event numbers; " +
                    "any that derived no row were never lines to lose)"));
            }

            var written = 0;
            foreach (var entry in fresh)
            {
                WriteLine(GameplayNarrationLog.Row(clock(), observedVersion, entry));
                lastWrittenSequence = entry.Sequence;
                RowsWritten++;
                written++;
            }

            return written;
        }

        /// <summary>Emits each completed player decision window once.</summary>
        public int ObserveDecisionWindows(
            long observedVersion,
            IReadOnlyList<DecisionWindowMeasurement> windows)
        {
            if (windows == null) throw new ArgumentNullException(nameof(windows));
            var fresh = windows.Where(window => window != null && window.Sequence > lastDecisionSequence)
                .OrderBy(window => window.Sequence).ToArray();
            foreach (var window in fresh)
            {
                WriteLine(GameplayNarrationLog.DecisionWindow(clock(), observedVersion, window));
                lastDecisionSequence = window.Sequence;
                DecisionRowsWritten++;
            }
            return fresh.Length;
        }

        public int DecisionRowsWritten { get; private set; }

        /// <summary>A session-layer event - a refusal, a resync, a reconnect - written inline.</summary>
        public void WriteSessionEvent(long observedVersion, string marker, string detail = null) =>
            WriteLine(GameplayNarrationLog.SessionRow(clock(), observedVersion, marker, detail));

        /// <summary>The closing line.</summary>
        public void WriteOutcome(long observedVersion, string cause, string winner) =>
            WriteLine(GameplayNarrationLog.Outcome(clock(), observedVersion, cause, winner));

        private void WriteLine(string line)
        {
            writer.WriteLine(line);
            writer.Flush();
        }

        public void Dispose()
        {
            if (ownsWriter)
            {
                writer.Dispose();
            }
        }
    }
}
