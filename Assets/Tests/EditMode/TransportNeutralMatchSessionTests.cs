using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.30. The contract two different consumers of match authority share: the local
    /// director adapter that exists, and the network host milestone 8.31 onward adds.
    /// </summary>
    /// <remarks>
    /// Every case here drives <see cref="IMatchHost"/> and never the session behind it. That is the
    /// point of the fixture - a check that reached past the port would prove the port optional.
    /// </remarks>
    public sealed class TransportNeutralMatchSessionTests
    {
        private const int LoopSeed = 8_30;

        [Test]
        public void SameTypedCommands_ThroughThePortAndDirectly_ReachTheSameAuthoritativeState()
        {
            var initialState = CreateInitialState();
            var direct = new MatchSession(initialState);
            var host = MatchHost.Compose(initialState);

            var commands = Drive(direct, 40);
            Assert.That(commands.Count, Is.GreaterThan(20),
                "The comparison needs a real command sequence behind it.");

            foreach (var command in commands)
            {
                var result = host.Submit(MatchSubmission.ForCommand(command));
                Assert.That(result.Accepted, Is.True, result.Detail);
            }

            Assert.That(host.Version, Is.EqualTo(direct.Projection.Version));
            Assert.That(host.Projection.AuthoritativeSnapshot,
                Is.EqualTo(direct.Projection.AuthoritativeSnapshot),
                "The same typed commands must leave the same authoritative state whichever door " +
                "they came through.");
            Assert.That(host.Projection.DiagnosticSnapshot,
                Is.EqualTo(direct.Projection.DiagnosticSnapshot),
                "Including the ordered rule-event log, which is half of what a client consumes.");
            Assert.That(host.CaptureSnapshot(), Is.EqualTo(direct.CaptureSnapshot()));
            Assert.That(host.Projection.AcceptedCommands, Is.EqualTo(commands));
        }

        [Test]
        public void EachAcceptedSubmission_AppendsOneRecordCarryingItsOwnOrderedEvents()
        {
            var host = MatchHost.Compose(CreateInitialState());
            var expected = new List<MatchHostResult>();

            foreach (var command in NextLegalCommands(host, 12))
            {
                var result = host.Submit(MatchSubmission.ForCommand(command));
                Assert.That(result.Accepted, Is.True, result.Detail);
                expected.Add(result);
            }

            var read = host.ReadResults(0);

            Assert.That(read.IsComplete, Is.True);
            Assert.That(read.NextSequence, Is.EqualTo(expected.Count));
            Assert.That(read.Records.Select(record => record.Sequence),
                Is.EqualTo(Enumerable.Range(1, expected.Count).Select(value => (long)value)),
                "One accepted submission appends exactly one record, and the sequence is contiguous.");

            for (var index = 0; index < expected.Count; index++)
            {
                var record = read.Records[index];
                var result = expected[index];
                Assert.That(record.Sequence, Is.EqualTo(result.Sequence));
                Assert.That(record.Version, Is.EqualTo(result.Version));
                Assert.That(record.Kind, Is.EqualTo(MatchSubmissionKind.Command));
                Assert.That(record.Command, Is.SameAs(result.Submission.Command));
                Assert.That(record.Events, Is.EqualTo(result.Events),
                    "A record carries the events that submission produced, in their order.");
            }

            Assert.That(read.Records.Any(record => record.Events.Count > 0),
                "The sequence must contain at least one submission that produced rule events, or " +
                "the ordering claim above is vacuous.");
        }

        [Test]
        public void TwoIndependentConsumers_ReadTheSameOrderedRecordsFromTheirOwnCursors()
        {
            var host = MatchHost.Compose(CreateInitialState());
            var localAdapter = new StreamConsumer();
            var networkHost = new StreamConsumer();
            var commands = NextLegalCommands(host, 14);

            for (var index = 0; index < commands.Count; index++)
            {
                Assert.That(host.Submit(MatchSubmission.ForCommand(commands[index])).Accepted, Is.True);

                // The local adapter consumes after every submission; the stand-in network host
                // consumes in bursts, exactly as a client that is behind by a few messages would.
                localAdapter.Consume(host);
                if (index % 5 == 4)
                {
                    networkHost.Consume(host);
                }
            }

            networkHost.Consume(host);

            Assert.That(localAdapter.Cursor, Is.EqualTo(host.Sequence));
            Assert.That(networkHost.Cursor, Is.EqualTo(host.Sequence));
            Assert.That(localAdapter.Sequences, Is.EqualTo(networkHost.Sequences),
                "Both consumers read one ordered stream, whatever cadence they read it at.");
            Assert.That(localAdapter.Sequences,
                Is.EqualTo(Enumerable.Range(1, commands.Count).Select(value => (long)value)));
            Assert.That(localAdapter.Versions, Is.EqualTo(networkHost.Versions));
            Assert.That(localAdapter.Versions, Is.Ordered.Ascending);
        }

        [Test]
        public void RefusedSubmission_AdvancesNeitherVersionNorStream()
        {
            var host = MatchHost.Compose(CreateInitialState());
            var accepted = host.Submit(MatchSubmission.ForCommand(NextLegalCommands(host, 1)[0]));
            Assert.That(accepted.Accepted, Is.True, accepted.Detail);
            var version = host.Version;
            var sequence = host.Sequence;
            var snapshot = host.CaptureSnapshot();

            var refused = host.Submit(MatchSubmission.ForCommand(DirectorCommand.StopMovement()));

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason, Is.EqualTo(MatchHostRejectionReason.SessionRefused));
            Assert.That(refused.SessionRejectionReason,
                Is.EqualTo(MatchSessionRejectionReason.CommandUnavailable),
                "A rules refusal reaches the submitter as the authority's own reason.");
            Assert.That(refused.Sequence, Is.EqualTo(0));
            Assert.That(refused.Events, Is.Empty);
            Assert.That(refused.Version, Is.EqualTo(version));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(sequence));
            Assert.That(host.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(host.ReadResults(0).Records.Count, Is.EqualTo((int)sequence),
                "A refusal must leave nothing behind in the stream for a consumer to apply.");
        }

        [Test]
        public void SubmissionMadeFromInsideAnother_IsRefusedBeforeTheAuthority()
        {
            var host = MatchHost.Compose(CreateInitialState());
            var command = NextLegalCommands(host, 1)[0];
            MatchHostResult nested = null;
            host.DuringSubmissionForTest = () =>
            {
                if (nested != null)
                {
                    return;
                }

                nested = host.Submit(MatchSubmission.ForCommand(command));
            };

            var outer = host.Submit(MatchSubmission.ForCommand(command));
            host.DuringSubmissionForTest = null;

            Assert.That(nested, Is.Not.Null, "The hook must have run inside the outer submission.");
            Assert.That(nested.Accepted, Is.False);
            Assert.That(nested.RejectionReason,
                Is.EqualTo(MatchHostRejectionReason.SubmissionInProgress));
            Assert.That(nested.SessionRejectionReason, Is.EqualTo(MatchSessionRejectionReason.None),
                "A host refusal is not a rules answer and must not borrow one.");
            Assert.That(nested.Sequence, Is.EqualTo(0));
            Assert.That(outer.Accepted, Is.True, outer.Detail);
            Assert.That(host.Sequence, Is.EqualTo(1),
                "The outer submission is the only one that reached the authority.");
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(1));
        }

        [Test]
        public void ConcurrentSubmissionsOfOneCommand_AreSerializedIntoOneStateAdvance()
        {
            var host = MatchHost.Compose(CreateInitialState());
            var command = NextLegalCommands(host, 1)[0];
            var results = new MatchHostResult[4];
            var start = new ManualResetEventSlim(false);
            var threads = new Thread[results.Length];

            for (var index = 0; index < threads.Length; index++)
            {
                var slot = index;
                threads[slot] = new Thread(() =>
                {
                    start.Wait();
                    results[slot] = host.Submit(MatchSubmission.ForCommand(command));
                });
                threads[slot].Start();
            }

            start.Set();
            foreach (var thread in threads)
            {
                Assert.That(thread.Join(TimeSpan.FromSeconds(30)), Is.True,
                    "A submission blocked on the host gate must still complete.");
            }

            Assert.That(results.Count(result => result.Accepted), Is.EqualTo(1),
                "One command, submitted four times at once, is one state advance.");
            Assert.That(host.Sequence, Is.EqualTo(1));
            Assert.That(host.Version, Is.EqualTo(results.Single(result => result.Accepted).Version));
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(1));
            foreach (var refused in results.Where(result => !result.Accepted))
            {
                Assert.That(refused.Sequence, Is.EqualTo(0));
                Assert.That(refused.RejectionReason,
                    Is.EqualTo(MatchHostRejectionReason.SessionRefused)
                        .Or.EqualTo(MatchHostRejectionReason.SubmissionInProgress));
            }
        }

        [Test]
        public void UndoRestartAndSnapshotRestore_FlowThroughThePortAndRecordNoRuleEvents()
        {
            var host = MatchHost.Compose(CreateInitialState());
            var commands = NextLegalCommands(host, 6);
            foreach (var command in commands)
            {
                Assert.That(host.Submit(MatchSubmission.ForCommand(command)).Accepted, Is.True);
            }

            var beforeUndo = host.Projection.AuthoritativeSnapshot;
            var payload = host.CaptureSnapshot();

            var undo = host.Submit(MatchSubmission.ForUndo());
            Assert.That(undo.Accepted, Is.True, undo.Detail);
            Assert.That(undo.Events, Is.Empty);
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(commands.Count - 1));

            var restart = host.Submit(MatchSubmission.ForRestart());
            Assert.That(restart.Accepted, Is.True, restart.Detail);
            Assert.That(host.Projection.AcceptedCommands, Is.Empty);

            var restored = host.Submit(MatchSubmission.ForSnapshotRestore(payload));
            Assert.That(restored.Accepted, Is.True, restored.Detail);
            Assert.That(host.Projection.AuthoritativeSnapshot, Is.EqualTo(beforeUndo),
                "A snapshot captured through the port restores through it.");

            var read = host.ReadResults(commands.Count);

            Assert.That(read.IsComplete, Is.True);
            Assert.That(read.Records.Select(record => record.Kind), Is.EqualTo(new[]
            {
                MatchSubmissionKind.Undo,
                MatchSubmissionKind.Restart,
                MatchSubmissionKind.RestoreSnapshot
            }));
            Assert.That(read.Records.All(record => record.Command == null), Is.True);
            Assert.That(read.Records.All(record => record.Events.Count == 0), Is.True,
                "These three reconstruct state rather than resolving rules, so a consumer is told " +
                "that the version moved and nothing about how.");
            Assert.That(read.Records.Select(record => record.Version), Is.Ordered.Ascending);
            Assert.That(read.Records.Last().Version, Is.EqualTo(host.Version));
        }

        [Test]
        public void RefusedOperations_ReachTheSubmitterAsTheAuthoritysOwnReason()
        {
            var host = MatchHost.Compose(CreateInitialState());

            var undo = host.Submit(MatchSubmission.ForUndo());
            Assert.That(undo.Accepted, Is.False);
            Assert.That(undo.RejectionReason, Is.EqualTo(MatchHostRejectionReason.SessionRefused));
            Assert.That(undo.SessionRejectionReason,
                Is.EqualTo(MatchSessionRejectionReason.CommandUnavailable));

            var restore = host.Submit(MatchSubmission.ForSnapshotRestore("not a payload"));
            Assert.That(restore.Accepted, Is.False);
            Assert.That(restore.SessionRejectionReason,
                Is.EqualTo(MatchSessionRejectionReason.SnapshotRejected));
            Assert.That(host.Sequence, Is.EqualTo(0));
            Assert.That(host.ReadResults(0).Records, Is.Empty);
        }

        [Test]
        public void ConsumerBehindTheRetainedWindow_IsToldToResynchronizeRatherThanHandedAGap()
        {
            var host = MatchHost.Compose(CreateInitialState());
            var commands = NextLegalCommands(host, MatchHost.RetainedResultCount + 6);
            Assert.That(commands.Count, Is.GreaterThan(MatchHost.RetainedResultCount),
                "The window has to be exceeded for the claim to be observable.");
            foreach (var command in commands)
            {
                Assert.That(host.Submit(MatchSubmission.ForCommand(command)).Accepted, Is.True);
            }

            var fromTheStart = host.ReadResults(0);
            Assert.That(fromTheStart.IsComplete, Is.False);
            Assert.That(fromTheStart.Records, Is.Empty,
                "An incomplete read carries no records, so it can never be applied as a continuation.");
            Assert.That(fromTheStart.NextSequence, Is.EqualTo(host.Sequence));

            var oldestServable = host.Sequence - MatchHost.RetainedResultCount;
            Assert.That(host.ReadResults(oldestServable).IsComplete, Is.True);
            Assert.That(host.ReadResults(oldestServable).Records.Count,
                Is.EqualTo(MatchHost.RetainedResultCount));
            Assert.That(host.ReadResults(oldestServable - 1).IsComplete, Is.False);
            Assert.That(host.ReadResults(host.Sequence).Records, Is.Empty);
            Assert.That(host.ReadResults(host.Sequence).IsComplete, Is.True);
            Assert.That(host.ReadResults(host.Sequence + 1).IsComplete, Is.False,
                "A cursor ahead of the host is out of synchronization, not merely up to date.");
        }

        [Test]
        public void LocalDirectorClient_SubmitsEverythingThroughThePortAndOwnsNoAuthority()
        {
            var host = new RecordingHost(MatchHost.Compose(CreateInitialState()));
            var client = new LocalDirectorMatchClient(host);

            var command = DeterministicMatchLoop.EnumerateLegalCommands(client.Projection)[0];
            var executed = client.Execute(command);
            Assert.That(executed.Accepted, Is.True, executed.Detail);
            Assert.That(executed.Projection, Is.SameAs(host.Projection));

            var refused = client.Execute(DirectorCommand.StopMovement());
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason,
                Is.EqualTo(MatchSessionRejectionReason.CommandUnavailable),
                "A rules refusal must reach the director unchanged by the port.");

            var payload = client.CaptureSnapshot();
            Assert.That(client.Undo().Accepted, Is.True);
            Assert.That(client.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(client.Restart().Accepted, Is.False,
                "A restored session has no history, and that answer comes from the authority.");

            Assert.That(host.Submitted, Is.EqualTo(new[]
            {
                MatchSubmissionKind.Command,
                MatchSubmissionKind.Command,
                MatchSubmissionKind.Undo,
                MatchSubmissionKind.RestoreSnapshot,
                MatchSubmissionKind.Restart
            }), "Every mutating call the director makes is one submission, in the order it made them.");
            Assert.That(host.SnapshotReads, Is.EqualTo(1));
        }

        /// <summary>A consumer holding its own cursor into the one ordered stream.</summary>
        private sealed class StreamConsumer
        {
            public long Cursor { get; private set; }
            public List<long> Sequences { get; } = new List<long>();
            public List<long> Versions { get; } = new List<long>();

            public void Consume(IMatchHost host)
            {
                var read = host.ReadResults(Cursor);
                Assert.That(read.IsComplete, Is.True, "A consumer inside the window reads completely.");
                foreach (var record in read.Records)
                {
                    Sequences.Add(record.Sequence);
                    Versions.Add(record.Version);
                }

                Cursor = read.NextSequence;
            }
        }

        /// <summary>
        /// A port that records what was asked of it and forwards everything. It stands where a
        /// network host will stand, and is the only way to observe that the director's client makes
        /// one submission per mutating call rather than reaching around it.
        /// </summary>
        private sealed class RecordingHost : IMatchHost
        {
            private readonly IMatchHost inner;

            public RecordingHost(IMatchHost inner)
            {
                this.inner = inner;
            }

            public List<MatchSubmissionKind> Submitted { get; } = new List<MatchSubmissionKind>();
            public int SnapshotReads { get; private set; }

            public long Version => inner.Version;
            public long Sequence => inner.Sequence;
            public MatchSessionProjection Projection => inner.Projection;

            public MatchHostResult Submit(MatchSubmission submission)
            {
                Submitted.Add(submission.Kind);
                return inner.Submit(submission);
            }

            public MatchHostResultStreamRead ReadResults(long afterSequence) =>
                inner.ReadResults(afterSequence);

            public string CaptureSnapshot()
            {
                SnapshotReads++;
                return inner.CaptureSnapshot();
            }

            // Milestone 8.30a's seat surface. This double exists to observe the director client's
            // submissions, so it forwards the seat members unchanged rather than recording them.
            public MatchSeatBinding Seats => inner.Seats;

            public SeatProjection ProjectionForSeat(SeatId seat) => inner.ProjectionForSeat(seat);

            public SeatSubmissionResult SubmitForSeat(SeatId seat, SeatSubmission submission) =>
                inner.SubmitForSeat(seat, submission);

            public TeamAgreementSubmissionResult SubmitAgreementForSeat(
                SeatId seat, TeamAgreementSubmission submission) =>
                inner.SubmitAgreementForSeat(seat, submission);

            public SeatUndoResult SubmitUndoForSeat(SeatId seat, SeatUndoSubmission submission) =>
                inner.SubmitUndoForSeat(seat, submission);

            public ISeatClient ClientForSeat(SeatId seat) => inner.ClientForSeat(seat);
        }

        /// <summary>
        /// The commands the authority currently offers, taken one at a time so that each is legal in
        /// the state the previous one left. It drives the port, never the session.
        /// </summary>
        private static IReadOnlyList<DirectorCommand> NextLegalCommands(IMatchHost host, int count)
        {
            var random = new MatchLoopRandom(LoopSeed);
            var commands = new List<DirectorCommand>();
            var probe = MatchHost.Compose(CreateInitialState());
            for (var index = 0; index < host.Sequence; index++)
            {
                // The probe walks the host's own accepted prefix so that the commands it goes on to
                // offer are legal for the caller too.
                var replay = host.Projection.AcceptedCommands[index];
                Assert.That(probe.Submit(MatchSubmission.ForCommand(replay)).Accepted, Is.True);
            }

            while (commands.Count < count)
            {
                var legal = DeterministicMatchLoop.EnumerateLegalCommands(probe.Projection);
                if (legal.Count == 0)
                {
                    break;
                }

                var command = legal[random.Next(legal.Count)];
                if (!probe.Submit(MatchSubmission.ForCommand(command)).Accepted)
                {
                    break;
                }

                commands.Add(command);
            }

            Assert.That(commands.Count, Is.EqualTo(count),
                "The fixture needs the whole sequence it asked for; a short one would weaken the " +
                "claim silently.");
            return commands;
        }

        private static IReadOnlyList<DirectorCommand> Drive(MatchSession session, int count)
        {
            var random = new MatchLoopRandom(LoopSeed);
            var commands = new List<DirectorCommand>();
            while (commands.Count < count)
            {
                var legal = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                if (legal.Count == 0)
                {
                    break;
                }

                var command = legal[random.Next(legal.Count)];
                if (!session.Execute(command).Accepted)
                {
                    break;
                }

                commands.Add(command);
            }

            return commands;
        }

        private static MatchSessionInitialState CreateInitialState()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSessionInitialState(scenario, setup, new[] { map });
        }
    }
}
