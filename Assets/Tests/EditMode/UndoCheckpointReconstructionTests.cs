using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Snapshots;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.9b's Undo reconstruction: replaying from the latest retained point instead of
    /// from the start, without the result being distinguishable in any way from a full-prefix
    /// replay.
    /// </summary>
    /// <remarks>
    /// `F-ER2-08` records that <c>Undo</c> and <c>Restart</c> reconstruct by full-prefix replay, so
    /// their cost grows with match length; milestone 7.9 measured 429 ms on Unity's Mono at
    /// complete-match length. The fix retains the aggregate's pre-image every
    /// <see cref="MatchSession.UndoCheckpointInterval"/> accepted commands and reconstructs from
    /// the nearest one, so the replayed segment is bounded by the interval.
    ///
    /// What these tests pin is that the retained point is a derived cache and nothing more: every
    /// reconstruction below is compared byte-for-byte against a fresh session that replayed the
    /// whole prefix through the public command path, at the three positions the mechanism
    /// distinguishes - above the latest retained point, exactly on one, and below one, where an
    /// earlier point must serve. The complete-loop gate keeps proving the redo half: an undone
    /// match accepts the same command again into the same ending, byte for byte.
    /// </remarks>
    public sealed class UndoCheckpointReconstructionTests
    {
        /// <summary>
        /// The Wave-counter seed <c>CompleteCoreMatchLoopTests</c> reports, re-measured at milestone
        /// 8.8b when refusing a Clear that can remove nothing changed every walk's trajectory. The two
        /// must stay the same number: these cases are about the match that gate proves correct.
        /// </summary>
        // Milestone 8.35g re-derived this: the turn release is drawn from a one-command set and
        // `Next(1)` still advances the stream, so every seeded walk is a different match. Seed 32
        // no longer reaches an ending inside any reasonable budget; seed 11 is the lowest that
        // does, ending on p.13's last Life counter after 44 rounds and 1,407 commands.
        private const int Seed = 11;

        private const int Budget = 2400;

        [Test]
        public void UndoAboveTheLatestRetainedPoint_MatchesAFullPrefixReplayExactly()
        {
            var record = PlayCompleteMatch();
            var session = ConfiguredMatchFixtures.CreateEngagedSession(2);
            DeterministicMatchLoop.Replay(session, record.Commands);

            var undone = session.Undo();

            Assert.That(undone.Accepted, Is.True, undone.Detail);
            AssertMatchesAFullPrefixReplay(session, record, record.Commands.Count - 1);
        }

        [Test]
        public void UndoDownToExactlyARetainedPoint_MatchesAFullPrefixReplayExactly()
        {
            var record = PlayCompleteMatch();
            var session = ConfiguredMatchFixtures.CreateEngagedSession(2);
            DeterministicMatchLoop.Replay(session, record.Commands);

            // The largest multiple of the interval inside the match: undoing to exactly here means
            // the reconstruction replays nothing at all beyond the retained state.
            var retained = record.Commands.Count / MatchSession.UndoCheckpointInterval *
                MatchSession.UndoCheckpointInterval;
            Assert.That(retained, Is.GreaterThan(0),
                "The complete match must be long enough to have retained a point.");
            for (var length = record.Commands.Count; length > retained; length--)
            {
                var undone = session.Undo();
                Assert.That(undone.Accepted, Is.True, undone.Detail);
            }

            AssertMatchesAFullPrefixReplay(session, record, retained);
        }

        [Test]
        public void UndoBelowTheLatestRetainedPoint_ReconstructsFromAnEarlierOne()
        {
            var record = PlayCompleteMatch();
            var session = ConfiguredMatchFixtures.CreateEngagedSession(2);
            DeterministicMatchLoop.Replay(session, record.Commands);

            // One below the last retained point: that point no longer covers the surviving prefix,
            // so the reconstruction must fall back to the one before it - and, having adopted the
            // result, must have discarded the point that no longer describes a prefix of the match.
            var retained = record.Commands.Count / MatchSession.UndoCheckpointInterval *
                MatchSession.UndoCheckpointInterval;
            Assert.That(retained, Is.GreaterThanOrEqualTo(MatchSession.UndoCheckpointInterval * 2),
                "The complete match must span at least two retained points, or falling back to an " +
                "earlier one is not what this exercises.");
            for (var length = record.Commands.Count; length > retained - 1; length--)
            {
                var undone = session.Undo();
                Assert.That(undone.Accepted, Is.True, undone.Detail);
            }

            AssertMatchesAFullPrefixReplay(session, record, retained - 1);
        }

        [Test]
        public void TheCapturedPayload_IsByteIdenticalToAOnePassSerialization()
        {
            // The capture shares the completed-round history's bytes across commands; a one-pass
            // serialization re-encodes everything with no cache at all. Mid-match, with rounds in
            // the history and a round in flight, the two must be the same string - which is what
            // makes the capture a cost change rather than a format change.
            var record = PlayCompleteMatch();
            var session = ConfiguredMatchFixtures.CreateEngagedSession(2);
            DeterministicMatchLoop.Replay(
                session, record.Commands.Take(record.Commands.Count / 2).ToArray());

            Assert.That(session.Projection.CompletedRounds, Is.Not.Empty,
                "The comparison must span a real completed-round history, or the shared fragment " +
                "is empty and the case proves nothing.");
            Assert.That(
                session.Projection.AuthoritativeSnapshot,
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(
                    session.aggregate.Round, session.aggregate.Board)));
        }

        private static void AssertMatchesAFullPrefixReplay(
            MatchSession session, MatchLoopRecord record, int prefixLength)
        {
            var replayed = ConfiguredMatchFixtures.CreateEngagedSession(2);
            DeterministicMatchLoop.Replay(replayed, record.Commands.Take(prefixLength).ToArray());

            Assert.That(session.Projection.AuthoritativeSnapshot,
                Is.EqualTo(replayed.Projection.AuthoritativeSnapshot),
                "A reconstruction from a retained point must be byte-identical to replaying the " +
                "whole prefix.");
            Assert.That(session.Projection.AcceptedCommands,
                Is.EqualTo(record.Commands.Take(prefixLength)));
            Assert.That(session.Projection.IsHistoryAvailable, Is.True);
            Assert.That(
                session.Projection.DiagnosticEvents.Select(entry => entry.DisplayText),
                Is.EqualTo(replayed.Projection.DiagnosticEvents.Select(entry => entry.DisplayText)),
                "The whole-match rule-event log is a function of the command prefix, however the " +
                "prefix state was reached.");
            Assert.That(
                session.Projection.RoundEvents.Select(Describe),
                Is.EqualTo(replayed.Projection.RoundEvents.Select(Describe)),
                "The current round's ordered events must survive the retained point exactly; the " +
                "public snapshot-restore path deliberately drops them, and this path must not.");
            Assert.That(session.Projection.Version, Is.GreaterThan(0));
        }

        private static string Describe(GuardsDigital.Core.Cards.RoundResolutionEvent value)
        {
            return $"{value.Kind}/r{value.RoundIndex}/t{value.TurnIndex}";
        }

        private static MatchLoopRecord PlayCompleteMatch()
        {
            var record = DeterministicMatchLoop.Play(
                ConfiguredMatchFixtures.CreateEngagedSession(2), Seed, Budget);
            Assert.That(record.Outcome.HasEnded, Is.True,
                "The reported seed must still play to an ending, or every case above shrinks.");
            return record;
        }
    }
}
