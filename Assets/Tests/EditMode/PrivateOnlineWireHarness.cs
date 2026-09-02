using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>A deterministic nonce source whose every draw differs, so submission ids never collide.</summary>
    internal sealed class CountingNonceSource : IPrivateSessionNonceSource
    {
        private readonly byte prefix;
        private long counter;

        public CountingNonceSource(byte prefix)
        {
            this.prefix = prefix;
        }

        public byte[] NextNonce()
        {
            counter++;
            var nonce = new byte[9];
            nonce[0] = prefix;
            for (var index = 0; index < 8; index++)
            {
                nonce[index + 1] = (byte)((counter >> (8 * index)) & 0xFF);
            }

            return nonce;
        }
    }

    /// <summary>
    /// Milestone 8.33. The in-memory wire: one real <see cref="PrivateSessionHost"/> and up to six
    /// real <see cref="PrivateSessionClient"/> machines linked by ordered frame queues, each
    /// direction optionally transformed by a deterministic <see cref="PrivateSessionFaultInjector"/>
    /// sitting exactly where the fault plan puts it - above the transport, below the codec.
    /// </summary>
    /// <remarks>
    /// Every delivered byte run goes through a real <see cref="PrivateSessionFrameAssembler"/>, so
    /// a truncated or misaligned injection is refused by the same stream discipline the socket
    /// adapter applies. The harness composes unkeyed sessions by default: those are the
    /// loopback-legitimate plaintext sessions, and plaintext is what lets a fixture read the
    /// projection payloads it asserts byte-for-byte convergence over.
    /// </remarks>
    internal sealed class PrivateOnlineWireHarness
    {
        private abstract class WireAction
        {
        }

        private sealed class FrameAction : WireAction
        {
            public FrameAction(int ordinal, byte[] bytes)
            {
                Ordinal = ordinal;
                Bytes = bytes;
            }

            public int Ordinal { get; }
            public byte[] Bytes { get; }
        }

        private sealed class CloseAction : WireAction
        {
            public CloseAction(int ordinal)
            {
                Ordinal = ordinal;
            }

            public int Ordinal { get; }
        }

        private sealed class ClientOutbox : IPrivateSessionClientOutbox
        {
            private readonly PrivateOnlineWireHarness harness;
            private readonly int ordinal;

            public ClientOutbox(PrivateOnlineWireHarness harness, int ordinal)
            {
                this.harness = harness;
                this.ordinal = ordinal;
            }

            public void Send(byte[] frame) =>
                harness.clientQueue.Enqueue(new FrameAction(ordinal, frame));

            public void Close() => harness.clientQueue.Enqueue(new CloseAction(ordinal));
        }

        private sealed class ClientLink
        {
            public PrivateSessionClient Machine;
            public PrivateSessionFrameAssembler FromHost = new PrivateSessionFrameAssembler();
            public PrivateSessionFrameAssembler ToHost = new PrivateSessionFrameAssembler();
            public PrivateSessionFaultInjector HostToClientInjector;
            public PrivateSessionFaultInjector ClientToHostInjector;
            public byte[] LastProjectionPayload;
            public string Seat;
            public bool HostSideClosed;
            public bool ClientSideClosed;
        }

        private readonly Queue<WireAction> hostQueue = new Queue<WireAction>();
        private readonly Queue<WireAction> clientQueue = new Queue<WireAction>();
        private readonly Dictionary<int, ClientLink> links = new Dictionary<int, ClientLink>();
        private readonly string sessionKey;

        private PrivateOnlineWireHarness(
            PrivateSessionHost session,
            IMatchHost matchHost,
            PrivateSessionIdentity identity,
            string sessionKey)
        {
            Session = session;
            MatchHost = matchHost;
            Identity = identity;
            this.sessionKey = sessionKey;
        }

        public PrivateSessionHost Session { get; }
        public IMatchHost MatchHost { get; }
        public PrivateSessionIdentity Identity { get; }

        public static PrivateOnlineWireHarness Compose(string sessionKey = "")
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var identity = PrivateSessionIdentity.ForComposition(map, configuration, "sync-build");
            var matchHost = GuardsDigital.Application.MatchHost.Compose(
                OwnerScopedProjectionTests.CreateOfficialInitialState());
            PrivateOnlineWireHarness harness = null;
            var session = new PrivateSessionHost(
                matchHost, identity, sessionKey, new HostOutboxProxy(() => harness),
                new RecordedNonceSource());
            harness = new PrivateOnlineWireHarness(session, matchHost, identity, sessionKey);
            return harness;
        }

        /// <summary>The outbox exists before the harness it forwards to; this closes the loop.</summary>
        private sealed class HostOutboxProxy : IPrivateSessionOutbox
        {
            private readonly Func<PrivateOnlineWireHarness> harness;

            public HostOutboxProxy(Func<PrivateOnlineWireHarness> harness)
            {
                this.harness = harness;
            }

            public void Send(int connectionOrdinal, byte[] frame) =>
                harness().hostQueue.Enqueue(new FrameAction(connectionOrdinal, frame));

            public void Close(int connectionOrdinal) =>
                harness().hostQueue.Enqueue(new CloseAction(connectionOrdinal));
        }

        public PrivateSessionClient Client(int ordinal) => links[ordinal].Machine;

        public string SeatOf(int ordinal) => links[ordinal].Seat;

        /// <summary>The last projection payload delivered to this ordinal, readable on plaintext links.</summary>
        public byte[] LastProjectionPayload(int ordinal) => links[ordinal].LastProjectionPayload;

        public void SetHostToClientInjector(int ordinal, PrivateSessionFaultInjector injector) =>
            links[ordinal].HostToClientInjector = injector;

        public void SetClientToHostInjector(int ordinal, PrivateSessionFaultInjector injector) =>
            links[ordinal].ClientToHostInjector = injector;

        /// <summary>
        /// Connects one real client machine claiming one seat. A caller may hand the client its
        /// own admission identity - the compatibility cases present each mismatched tuple through
        /// exactly this door - and the default is the host's own, which is admitted.
        /// </summary>
        public PrivateSessionClient Connect(int ordinal, string seat, PrivateSessionIdentity identity = null)
        {
            var link = new ClientLink
            {
                Seat = seat,
                Machine = new PrivateSessionClient(
                    identity ?? Identity, new SeatId(seat), sessionKey,
                    new ClientOutbox(this, ordinal), new CountingNonceSource((byte)ordinal))
            };
            links[ordinal] = link;
            link.Machine.OnConnected();
            Session.OnConnected(ordinal, 0);
            return link.Machine;
        }

        /// <summary>Seats every official seat: ordinal 1..6 in the authoritative roster order.</summary>
        public void SeatAll()
        {
            var seats = Session.Roster.Select(entry => entry.Seat.Value).ToArray();
            for (var index = 0; index < seats.Length; index++)
            {
                Connect(index + 1, seats[index]);
            }

            Pump();
            for (var index = 0; index < seats.Length; index++)
            {
                Assert.That(Client(index + 1).State, Is.EqualTo(PrivateSessionClientState.Seated),
                    "Seat " + seats[index] + " did not seat over the wire.");
                Assert.That(Client(index + 1).LatestProjection, Is.Not.Null);
            }
        }

        /// <summary>Delivers everything in flight, in order, until the wire is quiet.</summary>
        public void Pump()
        {
            var progressed = true;
            while (progressed)
            {
                progressed = false;
                if (clientQueue.Count > 0)
                {
                    DeliverToHost(clientQueue.Dequeue());
                    progressed = true;
                    continue;
                }

                if (hostQueue.Count > 0)
                {
                    DeliverToClient(hostQueue.Dequeue());
                    progressed = true;
                }
            }
        }

        /// <summary>Releases every frame a delay injector still holds, then pumps it through.</summary>
        public void FlushInjectors()
        {
            foreach (var pair in links)
            {
                if (pair.Value.HostToClientInjector != null)
                {
                    foreach (var late in pair.Value.HostToClientInjector.Flush())
                    {
                        DeliverBytesToClient(pair.Key, late);
                    }
                }

                if (pair.Value.ClientToHostInjector != null)
                {
                    foreach (var late in pair.Value.ClientToHostInjector.Flush())
                    {
                        DeliverBytesToHost(pair.Key, late);
                    }
                }
            }

            Pump();
        }

        /// <summary>Injects raw client-side bytes onto the wire, the way a hostile process would.</summary>
        public void SendRaw(int ordinal, byte[] frame)
        {
            clientQueue.Enqueue(new FrameAction(ordinal, frame));
            Pump();
        }

        private void DeliverToHost(WireAction action)
        {
            if (action is CloseAction close)
            {
                var link = links[close.Ordinal];
                if (!link.ClientSideClosed)
                {
                    link.ClientSideClosed = true;
                    Session.OnDisconnected(close.Ordinal);
                }

                return;
            }

            var frame = (FrameAction)action;
            var target = links[frame.Ordinal];
            if (target.HostSideClosed)
            {
                return;
            }

            if (target.ClientToHostInjector == null)
            {
                DeliverBytesToHost(frame.Ordinal, frame.Bytes);
                return;
            }

            foreach (var bytes in target.ClientToHostInjector.Process(frame.Bytes))
            {
                DeliverBytesToHost(frame.Ordinal, bytes);
            }
        }

        private void DeliverBytesToHost(int ordinal, byte[] bytes)
        {
            var link = links[ordinal];
            if (link.HostSideClosed)
            {
                return;
            }

            if (!link.ToHost.Add(bytes, 0, bytes.Length))
            {
                Session.OnStreamRejected(ordinal, link.ToHost.Rejection);
                return;
            }

            while (link.ToHost.TryDequeue(out var frame))
            {
                Session.OnFrame(ordinal, frame);
            }
        }

        private void DeliverToClient(WireAction action)
        {
            if (action is CloseAction close)
            {
                var link = links[close.Ordinal];
                if (!link.HostSideClosed)
                {
                    link.HostSideClosed = true;
                    link.Machine.OnDisconnected();
                }

                return;
            }

            var frame = (FrameAction)action;
            var target = links[frame.Ordinal];
            if (target.ClientSideClosed)
            {
                return;
            }

            if (target.HostToClientInjector == null)
            {
                DeliverBytesToClient(frame.Ordinal, frame.Bytes);
                return;
            }

            foreach (var bytes in target.HostToClientInjector.Process(frame.Bytes))
            {
                DeliverBytesToClient(frame.Ordinal, bytes);
            }
        }

        private void DeliverBytesToClient(int ordinal, byte[] bytes)
        {
            var link = links[ordinal];
            if (link.ClientSideClosed)
            {
                return;
            }

            if (!link.FromHost.Add(bytes, 0, bytes.Length))
            {
                link.Machine.OnStreamRejected(link.FromHost.Rejection);
                return;
            }

            while (link.FromHost.TryDequeue(out var frame))
            {
                // On a plaintext link the harness can read what crossed; the byte-for-byte
                // convergence assertions compare exactly this against the host's own encoding.
                // Only a payload the machine actually adopted is recorded - a late or duplicate
                // delivery the client set aside unapplied is not this seat's view of anything.
                byte[] candidate = null;
                if (frame.Kind == SessionMessageKind.SeatProjection &&
                    NetworkMessageCodec.TryDecodeSeatProjection(frame.Payload, out var projection))
                {
                    candidate = projection.ProjectionPayload;
                }

                var before = link.Machine.LatestProjection;
                link.Machine.OnFrame(frame);
                if (candidate != null && !ReferenceEquals(link.Machine.LatestProjection, before))
                {
                    link.LastProjectionPayload = candidate;
                }
            }
        }

        /// <summary>
        /// Asserts that every seated client's view is the host's: the last delivered projection
        /// payload equals the host's current encoding for that seat, byte for byte, and the
        /// decoded version is the authoritative one.
        /// </summary>
        public void AssertConverged()
        {
            foreach (var pair in links.OrderBy(value => value.Key))
            {
                var link = pair.Value;
                if (link.Machine.State != PrivateSessionClientState.Seated)
                {
                    continue;
                }

                var seat = MatchHost.Seats.Seats.Single(value => value.Id.Value == link.Seat);
                var expected = NetworkSeatProjectionCodec.Encode(MatchHost.ProjectionForSeat(seat.Id));
                Assert.That(link.LastProjectionPayload, Is.EqualTo(expected),
                    "Seat " + link.Seat + "'s delivered view diverged from the host's own encoding " +
                    "at version " + MatchHost.Version + ".");
                Assert.That(link.Machine.LatestProjection.Version, Is.EqualTo(MatchHost.Version));
            }
        }

        /// <summary>
        /// Drives the match through the wire itself: each step, the same deterministic policy the
        /// local seat walk uses picks a command, and the owning seat's real client submits it -
        /// agreement finals as per-seat statements, everything else as a command from that
        /// client's own decoded offered surface.
        /// </summary>
        public int DriveThroughWire(
            int commandBudget,
            Func<IMatchReadModel, bool> stopBeforeCommand = null,
            Action afterAcceptedStep = null)
        {
            var random = new MatchLoopRandom(2);
            var submitted = 0;
            while (submitted < commandBudget)
            {
                if (stopBeforeCommand?.Invoke(MatchHost.Projection) == true)
                {
                    break;
                }

                var legal = DeterministicMatchLoop.EnumerateLegalCommands(MatchHost.Projection);
                if (legal.Count == 0)
                {
                    break;
                }

                var command = legal[random.Next(legal.Count)];
                if (TeamAgreementTestDriver.IsFinalAgreementCommand(command))
                {
                    SubmitAgreementThroughWire(command);
                    submitted++;
                    afterAcceptedStep?.Invoke();
                    continue;
                }

                var seat = MatchHost.Seats.Seats.FirstOrDefault(value =>
                    MatchHost.ProjectionForSeat(value.Id).AuthorizedCommands.Contains(command));
                Assert.That(seat, Is.Not.Null,
                    "Every command the walk could take belongs to some seat: " + command);
                var ordinal = OrdinalOf(seat.Id.Value);
                var client = Client(ordinal);
                var offered = client.LatestProjection.AuthorizedCommands
                    .FirstOrDefault(value => value.Equals(command));
                Assert.That(offered, Is.Not.Null,
                    "The wire record for seat " + seat.Id + " does not offer " + command.Kind +
                    " although the host's own projection does - the two surfaces diverged.");
                Assert.That(client.TrySubmitCommand(offered, out _), Is.True);
                Pump();
                Assert.That(client.LastSubmissionResult, Is.Not.Null);
                Assert.That(client.LastSubmissionResult.Accepted, Is.True,
                    client.LastSubmissionResult.Detail);
                submitted++;
                afterAcceptedStep?.Invoke();
            }

            return submitted;
        }

        /// <summary>
        /// Every participant of the standing proposal states, through its own client, the
        /// statement matching the walk's chosen final command; the host's coordinator submits the
        /// final rules choice itself at unanimity.
        /// </summary>
        public void SubmitAgreementThroughWire(DirectorCommand finalCommand)
        {
            var participants = links.Values
                .Where(link => link.Machine.LatestProjection?.Detail.TeamAgreement != null)
                .SelectMany(link =>
                    link.Machine.LatestProjection.Detail.TeamAgreement.Participants
                        .Select(participant => participant.HeroId))
                .Distinct()
                .ToArray();
            Assert.That(participants, Is.Not.Empty,
                "A final team choice must publish an agreement proposal to its participants.");

            foreach (var heroId in participants)
            {
                var seat = MatchHost.Seats.Seats.Single(value => value.HeroId.Value == heroId);
                var client = Client(OrdinalOf(seat.Id.Value));
                var agreement = client.LatestProjection.Detail.TeamAgreement;
                Assert.That(agreement, Is.Not.Null);
                var statement = agreement.AuthorizedStatements.Single(value =>
                    finalCommand.Kind == DirectorCommandKind.ChooseTiedActor
                        ? value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                          value.PreferredCardInstanceId.Value == finalCommand.CardInstanceId.Value
                        : value.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection);
                Assert.That(client.TrySubmitAgreement(statement, out _), Is.True);
                Pump();
                Assert.That(client.LastAgreementResult, Is.Not.Null);
                Assert.That(client.LastAgreementResult.Accepted, Is.True,
                    client.LastAgreementResult.Detail);
            }
        }

        /// <summary>
        /// Submits one accepted command from the lowest-ordinal seated client, other than
        /// <paramref name="excludedOrdinal"/>, whose own decoded surface currently offers one, and
        /// returns that ordinal. The commitment phase answers each seat exactly once, so a
        /// scenario's second step must come from a seat still holding an offer rather than from
        /// whoever stepped first.
        /// </summary>
        public int SubmitOneAcceptedCommand(int excludedOrdinal = 0)
        {
            foreach (var pair in links.OrderBy(value => value.Key))
            {
                if (pair.Key == excludedOrdinal)
                {
                    continue;
                }

                var client = pair.Value.Machine;
                if (client.State != PrivateSessionClientState.Seated ||
                    client.LatestProjection == null ||
                    client.LatestProjection.AuthorizedCommands.Count == 0)
                {
                    continue;
                }

                Assert.That(client.TrySubmitCommand(
                    client.LatestProjection.AuthorizedCommands[0], out _), Is.True);
                Pump();
                Assert.That(client.LastSubmissionResult.Accepted, Is.True,
                    client.LastSubmissionResult.Detail);
                return pair.Key;
            }

            Assert.Fail("No seated client holds an offered command to submit.");
            return 0;
        }

        public int OrdinalOf(string seat) =>
            links.Single(pair => pair.Value.Seat == seat).Key;
    }
}
