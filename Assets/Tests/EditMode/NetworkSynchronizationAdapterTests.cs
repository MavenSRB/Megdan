using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.33. The synchronization slice over real sockets: the complete projection -
    /// acting surface plus its presentation-fidelity detail record - crosses the sealed adapter
    /// per seat, a stale submission is refused by name and healed by the resync the host offers
    /// unasked, and a seat that asks for a resync is answered with exactly one complete
    /// projection over the plaintext loopback session.
    /// </summary>
    /// <remarks>
    /// The in-memory <c>PrivateOnlineWireHarness</c> fixtures prove the same rules against every
    /// fault the plan names; this fixture is the socket-level restatement, so nothing the adapter
    /// does to real byte streams - segmentation, buffering, the seal - can hide a divergence the
    /// harness would have caught.
    /// </remarks>
    public sealed class NetworkSynchronizationAdapterTests
    {
        private const string SessionKey = "adapter-sync-key";
        private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);

        private sealed class AdapterHost : IDisposable
        {
            public TcpSessionListener Listener { get; }
            public PrivateSessionHost Session { get; }
            public IMatchHost MatchHost { get; }
            public PrivateSessionEndpoint DialEndpoint { get; }
            public PrivateSessionIdentity Identity { get; }

            public AdapterHost(string sessionKey)
            {
                var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                Identity = PrivateSessionIdentity.ForComposition(
                    map, configuration,
                    OwnerScopedProjectionTests.CreateOfficialInitialState().Setup,
                    "adapter-sync-build");
                MatchHost = GuardsDigital.Application.MatchHost.Compose(
                    OwnerScopedProjectionTests.CreateOfficialInitialState());
                Listener = new TcpSessionListener();
                Listener.Start(Ephemeral());
                Session = new PrivateSessionHost(
                    MatchHost, Identity, sessionKey, Listener, new CryptoNonceSource());
                Assert.That(PrivateSessionEndpoint.TryParseAddress(
                    "127.0.0.1:" + Listener.BoundPort, out var dial, out _), Is.True);
                DialEndpoint = dial;
            }

            /// <summary>Every session event the host has recorded, in order, across all pumps.</summary>
            public List<PrivateSessionEventRecord> Events { get; } =
                new List<PrivateSessionEventRecord>();

            private static PrivateSessionEndpoint Ephemeral()
            {
                var probe = new TcpListener(IPAddress.Loopback, 0);
                probe.Start();
                var port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();
                Assert.That(PrivateSessionEndpoint.TryCreate("127.0.0.1", port, out var endpoint, out _),
                    Is.True);
                return endpoint;
            }

            public void Pump()
            {
                while (Listener.TryDequeue(out var transportEvent))
                {
                    switch (transportEvent.Kind)
                    {
                        case TransportEventKind.Connected:
                            Session.OnConnected(transportEvent.Ordinal, DateTime.UtcNow.Ticks);
                            break;
                        case TransportEventKind.Frame:
                            Session.OnFrame(transportEvent.Ordinal, transportEvent.Frame);
                            break;
                        case TransportEventKind.StreamRejected:
                            Session.OnStreamRejected(transportEvent.Ordinal, transportEvent.Reason);
                            break;
                        case TransportEventKind.Disconnected:
                            Session.OnDisconnected(transportEvent.Ordinal);
                            break;
                    }
                }

                Session.Sweep(DateTime.UtcNow.Ticks);

                // DrainEvents empties the host's buffer, so a fixture that reads it more than once
                // must accumulate rather than re-read.
                Events.AddRange(Session.DrainEvents());
            }

            public void Dispose() => Listener.Dispose();
        }

        private sealed class AdapterClient : IDisposable
        {
            public TcpSessionClientTransport Transport { get; } = new TcpSessionClientTransport();
            public PrivateSessionClient Machine { get; }

            /// <summary>What the transport actually delivered, before any unsealing.</summary>
            public List<SessionMessageKind> WireKinds { get; } = new List<SessionMessageKind>();

            public AdapterClient(PrivateSessionIdentity identity, string seat, string sessionKey)
            {
                Machine = new PrivateSessionClient(
                    identity, new SeatId(seat), sessionKey, Transport, new CryptoNonceSource());
            }

            public bool Connect(PrivateSessionEndpoint endpoint, out string error) =>
                Transport.Connect(endpoint, 5, out error);

            public void Pump()
            {
                while (Transport.TryDequeue(out var transportEvent))
                {
                    switch (transportEvent.Kind)
                    {
                        case TransportEventKind.Connected:
                            Machine.OnConnected();
                            break;
                        case TransportEventKind.Frame:
                            WireKinds.Add(transportEvent.Frame.Kind);
                            Machine.OnFrame(transportEvent.Frame);
                            break;
                        case TransportEventKind.StreamRejected:
                            Machine.OnStreamRejected(transportEvent.Reason);
                            break;
                        case TransportEventKind.Disconnected:
                            Machine.OnDisconnected();
                            break;
                    }
                }
            }

            public void Dispose() => Transport.Dispose();
        }

        private static void PumpUntil(AdapterHost host, IEnumerable<AdapterClient> clients, Func<bool> done)
        {
            var stopwatch = Stopwatch.StartNew();
            var all = clients.ToArray();
            while (!done())
            {
                Assert.That(stopwatch.Elapsed, Is.LessThan(Deadline),
                    "The loopback exchange missed its deadline.");
                host.Pump();
                foreach (var client in all)
                {
                    client.Pump();
                }

                Thread.Sleep(5);
            }
        }

        /// <summary>
        /// The complete projection crosses the sealed adapter whole: each seat's delivered record
        /// carries its presentation-fidelity detail beside its acting surface, scoped to that seat,
        /// and nothing after the challenge crosses the keyed link in plaintext.
        /// </summary>
        [Test]
        public void TheCompleteProjection_CrossesTheSealedAdapter_WithItsDetailRecord()
        {
            using (var host = new AdapterHost(SessionKey))
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var first = new AdapterClient(host.Identity, seats[0], SessionKey))
                using (var second = new AdapterClient(host.Identity, seats[1], SessionKey))
                {
                    Assert.That(first.Connect(host.DialEndpoint, out var firstError), Is.True, firstError);
                    Assert.That(second.Connect(host.DialEndpoint, out var secondError), Is.True,
                        secondError);
                    PumpUntil(host, new[] { first, second }, () =>
                        first.Machine.LatestProjection != null &&
                        second.Machine.LatestProjection != null);

                    var expectedSeats = new[] { seats[0], seats[1] };
                    var clients = new[] { first, second };
                    for (var index = 0; index < clients.Length; index++)
                    {
                        var projection = clients[index].Machine.LatestProjection;
                        Assert.That(projection.SeatId, Is.EqualTo(expectedSeats[index]),
                            "Each client's delivered view is its own seat's.");
                        Assert.That(projection.Detail, Is.Not.Null,
                            "The complete projection carries its detail record over the adapter.");
                        Assert.That(projection.Detail.CompletedRounds, Is.Empty,
                            "The initial state has completed no round, and the wire says so.");
                        Assert.That(projection.Detail.TeamAgreement, Is.Null,
                            "No agreement proposal stands at the initial state.");
                        Assert.That(projection.AuthorizedCommands, Is.Not.Empty);

                        // On the wire itself, the challenge is the only plaintext frame the host
                        // ever sent this admitted connection.
                        Assert.That(clients[index].WireKinds.First(),
                            Is.EqualTo(SessionMessageKind.Challenge));
                        Assert.That(clients[index].WireKinds.Skip(1),
                            Has.All.EqualTo(SessionMessageKind.Sealed),
                            "A post-challenge frame crossed the keyed link in plaintext.");
                    }
                }
            }
        }

        /// <summary>
        /// The failure policy's stale row over real sockets: a seat that answers against a view the
        /// authority has already moved past is refused by name with nothing mutated, and the resync
        /// the host offers beside that refusal converges it without the client asking.
        /// </summary>
        [Test]
        public void AStaleSubmission_OverTheSealedAdapter_IsRefusedByNameAndHealed()
        {
            using (var host = new AdapterHost(SessionKey))
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var actor = new AdapterClient(host.Identity, seats[0], SessionKey))
                using (var observer = new AdapterClient(host.Identity, seats[1], SessionKey))
                {
                    Assert.That(actor.Connect(host.DialEndpoint, out var actorError), Is.True, actorError);
                    Assert.That(observer.Connect(host.DialEndpoint, out var observerError), Is.True,
                        observerError);
                    PumpUntil(host, new[] { actor, observer }, () =>
                        actor.Machine.LatestProjection != null &&
                        observer.Machine.LatestProjection != null);

                    var staleVersion = observer.Machine.LatestProjection.Version;
                    var observerCommand = observer.Machine.LatestProjection.AuthorizedCommands[0];

                    // Only the host and the actor are pumped, so the observer's machine never reads
                    // the projection the acceptance pushed it: its view is genuinely behind, the way
                    // a delayed delivery leaves it.
                    Assert.That(actor.Machine.TrySubmitCommand(
                        actor.Machine.LatestProjection.AuthorizedCommands[0], out _), Is.True);
                    PumpUntil(host, new[] { actor }, () => actor.Machine.LastSubmissionResult != null);
                    Assert.That(actor.Machine.LastSubmissionResult.Accepted, Is.True,
                        actor.Machine.LastSubmissionResult.Detail);
                    Assert.That(observer.Machine.LatestProjection.Version, Is.EqualTo(staleVersion),
                        "The observer must still hold the older view for the refusal to be stale.");

                    Assert.That(observer.Machine.TrySubmitCommand(observerCommand, out _), Is.True);
                    PumpUntil(host, new[] { actor, observer }, () =>
                        observer.Machine.LastSubmissionResult != null);
                    Assert.That(observer.Machine.LastSubmissionResult.Accepted, Is.False);
                    Assert.That(observer.Machine.LastSubmissionResult.RejectionReason,
                        Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
                    Assert.That(observer.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated),
                        "A stale answer is an ordinary gameplay moment, not a disconnect.");

                    PumpUntil(host, new[] { actor, observer }, () =>
                        observer.Machine.LatestProjection.Version == host.MatchHost.Version);
                    Assert.That(observer.Machine.ResyncRequestCount, Is.Zero,
                        "The offered resync converged the stale seat without a request.");
                    Assert.That(observer.Machine.LatestProjection.SeatId, Is.EqualTo(seats[1]),
                        "The healed view is still its own seat's.");
                }
            }
        }

        /// <summary>
        /// A seat that asks is answered: one bounded resync request over the plaintext loopback
        /// session brings back exactly one complete projection, and the asking seat stays seated.
        /// </summary>
        [Test]
        public void AResyncRequest_OverTheAdapter_IsAnsweredWithOneCompleteProjection()
        {
            using (var host = new AdapterHost(""))
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var first = new AdapterClient(host.Identity, seats[0], ""))
                using (var asking = new AdapterClient(host.Identity, seats[1], ""))
                {
                    Assert.That(first.Connect(host.DialEndpoint, out var firstError), Is.True, firstError);
                    Assert.That(asking.Connect(host.DialEndpoint, out var askingError), Is.True,
                        askingError);
                    PumpUntil(host, new[] { first, asking }, () =>
                        first.Machine.LatestProjection != null &&
                        asking.Machine.LatestProjection != null);
                    Assert.That(host.Events.Any(record =>
                        record.Kind == PrivateSessionEventKind.ResyncServed), Is.False,
                        "Nothing has been resynchronized before the request.");

                    // The request is sent raw, as a client that detected a gap would send it: the
                    // asking machine's own send sequence is spent by this frame, so it submits
                    // nothing afterwards.
                    asking.Transport.Send(NetworkMessageCodec.RequestResyncFrame(
                        new NetworkResyncRequestMessage(
                            new NetworkEnvelope(host.Identity.Correlation, 1, 1, 7))));

                    PumpUntil(host, new[] { first, asking }, () => host.Events.Any(record =>
                        record.Kind == PrivateSessionEventKind.ResyncServed &&
                        record.Seat.Value == seats[1]));
                    Assert.That(host.Events.Count(record =>
                        record.Kind == PrivateSessionEventKind.ResyncServed), Is.EqualTo(1),
                        "One request is answered with one bounded projection, never a chain.");
                    Assert.That(asking.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated));
                    Assert.That(asking.Machine.LatestProjection, Is.Not.Null);
                    Assert.That(asking.Machine.LatestProjection.SeatId, Is.EqualTo(seats[1]));
                    Assert.That(asking.Machine.LatestProjection.Detail, Is.Not.Null,
                        "The answer is the complete projection, detail record and all.");
                    Assert.That(first.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated),
                        "One seat's recovery is nobody else's business.");
                }
            }
        }
    }
}
