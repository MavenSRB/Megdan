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
    /// Milestone 8.32. The gameplay slice over real sockets: on a keyed loopback session every
    /// frame after the Hello travels sealed, each seated client receives its own owner-scoped
    /// projection, a submission from that projection's own offered surface crosses the wire and
    /// advances every view, and a plaintext gameplay frame injected onto a sealed connection
    /// disconnects that connection alone.
    /// </summary>
    public sealed class NetworkSessionAdapterTests
    {
        private const string SessionKey = "adapter-key";
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
                Identity = PrivateSessionIdentity.ForComposition(map, configuration, "adapter-build");
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

        [Test]
        public void AKeyedSession_SealsEverythingAfterTheChallenge_AndDeliversEachSeatItsOwnView()
        {
            using (var host = new AdapterHost(SessionKey))
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var first = new AdapterClient(host.Identity, seats[0], SessionKey))
                using (var second = new AdapterClient(host.Identity, seats[1], SessionKey))
                {
                    Assert.That(first.Connect(host.DialEndpoint, out var firstError), Is.True, firstError);
                    Assert.That(second.Connect(host.DialEndpoint, out var secondError), Is.True, secondError);
                    PumpUntil(host, new[] { first, second }, () =>
                        first.Machine.LatestProjection != null &&
                        second.Machine.LatestProjection != null);

                    foreach (var client in new[] { first, second })
                    {
                        // On the wire itself, the challenge is the only plaintext frame the host
                        // ever sent this admitted connection.
                        Assert.That(client.WireKinds.First(), Is.EqualTo(SessionMessageKind.Challenge));
                        Assert.That(client.WireKinds.Skip(1),
                            Has.All.EqualTo(SessionMessageKind.Sealed),
                            "A post-challenge frame crossed the keyed link in plaintext.");
                    }

                    Assert.That(first.Machine.LatestProjection.SeatId, Is.EqualTo(seats[0]));
                    Assert.That(second.Machine.LatestProjection.SeatId, Is.EqualTo(seats[1]));
                    Assert.That(first.Machine.LatestProjection.OwnCards, Is.Not.Empty);
                    Assert.That(first.Machine.LatestProjection.AuthorizedCommands, Is.Not.Empty);
                }
            }
        }

        [Test]
        public void AWireSubmissionFromTheOfferedSurface_IsAccepted_AndAdvancesEveryView()
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

                    var before = actor.Machine.LatestProjection.Version;
                    Assert.That(actor.Machine.TrySubmitCommand(
                        actor.Machine.LatestProjection.AuthorizedCommands[0], out var submissionId),
                        Is.True);
                    Assert.That(submissionId, Does.StartWith("wire-"));

                    PumpUntil(host, new[] { actor, observer }, () =>
                        actor.Machine.LastSubmissionResult != null);
                    Assert.That(actor.Machine.LastSubmissionResult.Accepted, Is.True,
                        actor.Machine.LastSubmissionResult.Detail);
                    Assert.That(actor.Machine.LastSubmissionResult.SubmissionId, Is.EqualTo(submissionId));
                    Assert.That(actor.Machine.LastSubmissionResult.Version, Is.EqualTo(before + 1));

                    PumpUntil(host, new[] { actor, observer }, () =>
                        actor.Machine.LatestProjection.Version == before + 1 &&
                        observer.Machine.LatestProjection.Version == before + 1);
                    Assert.That(observer.Machine.LatestProjection.SeatId, Is.EqualTo(seats[1]),
                        "The observer's advanced view is still its own seat's.");
                }
            }
        }

        [Test]
        public void APlaintextGameplayFrameOnAKeyedSession_DisconnectsThatConnectionAlone()
        {
            using (var host = new AdapterHost(SessionKey))
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var seated = new AdapterClient(host.Identity, seats[0], SessionKey))
                using (var hostile = new AdapterClient(host.Identity, seats[1], SessionKey))
                {
                    Assert.That(seated.Connect(host.DialEndpoint, out var seatedError), Is.True, seatedError);
                    Assert.That(hostile.Connect(host.DialEndpoint, out var hostileError), Is.True,
                        hostileError);
                    PumpUntil(host, new[] { seated, hostile }, () =>
                        seated.Machine.LatestProjection != null &&
                        hostile.Machine.LatestProjection != null);

                    // The hostile client bypasses its machine and injects a plaintext gameplay
                    // frame onto its own sealed connection.
                    hostile.Transport.Send(NetworkMessageCodec.SubmitCommandFrame(
                        new NetworkSubmissionMessage(
                            new NetworkEnvelope(host.Identity.Correlation, 1, 1, 1),
                            "wire-plain-1",
                            hostile.Machine.LatestProjection.Version,
                            NetworkCommandCodec.Encode(
                                hostile.Machine.LatestProjection.AuthorizedCommands[0]))));

                    PumpUntil(host, new[] { seated, hostile }, () =>
                        hostile.Machine.State == PrivateSessionClientState.Rejected ||
                        hostile.Machine.State == PrivateSessionClientState.Disconnected ||
                        hostile.Machine.State == PrivateSessionClientState.Faulted);
                    Assert.That(seated.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated),
                        "The injection closed only its own connection.");
                    Assert.That(host.MatchHost.Version,
                        Is.EqualTo(seated.Machine.LatestProjection.Version),
                        "The plaintext submission never reached the authority.");
                }
            }
        }
    }
}
