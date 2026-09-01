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
    /// Milestone 8.31. The whole stack over real sockets: one seatless host on an ephemeral
    /// loopback port, six clients claiming six seats, a straggler refused, a hostile frame
    /// disconnecting only its own connection, and a disposal that frees everything it held.
    /// </summary>
    /// <remarks>
    /// These are the repository's first socket tests. They bind only the loopback interface and
    /// only port 0 - the operating system picks a free ephemeral port - so nothing here prompts a
    /// firewall or collides with another run. Every wait is a bounded poll against a deadline, and
    /// the teardown assertion is the insurance the milestone plan named: <c>Dispose</c> joins
    /// every thread and releases the port, or the suite would flake for whoever runs next.
    /// </remarks>
    public sealed class PrivateSessionLoopbackTests
    {
        private const string SessionKey = "loopback-key";
        private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);

        private sealed class LoopbackHost : IDisposable
        {
            public TcpSessionListener Listener { get; }
            public PrivateSessionHost Session { get; }
            public PrivateSessionEndpoint DialEndpoint { get; }

            public LoopbackHost(string sessionKey)
            {
                var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                Identity = PrivateSessionIdentity.ForComposition(map, configuration, "loopback-build");
                var matchHost = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
                Listener = new TcpSessionListener();
                Listener.Start(Ephemeral());
                Session = new PrivateSessionHost(
                    matchHost, Identity, sessionKey, Listener, new CryptoNonceSource());
                Assert.That(PrivateSessionEndpoint.TryParseAddress(
                    "127.0.0.1:" + Listener.BoundPort, out var dial, out _), Is.True);
                DialEndpoint = dial;
            }

            public PrivateSessionIdentity Identity { get; }

            /// <summary>
            /// The endpoint type refuses port 0 by design - an operator names a real port - so a
            /// throwaway bind learns a free ephemeral port for the fixture first.
            /// </summary>
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

        private sealed class LoopbackClient : IDisposable
        {
            public TcpSessionClientTransport Transport { get; } = new TcpSessionClientTransport();
            public PrivateSessionClient Machine { get; }

            public LoopbackClient(PrivateSessionIdentity identity, string seat, string sessionKey)
            {
                Machine = new PrivateSessionClient(identity, new SeatId(seat), sessionKey, Transport);
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

        private static void PumpUntil(LoopbackHost host, IEnumerable<LoopbackClient> clients, Func<bool> done)
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
        public void SixClients_ClaimSixSeats_AndTheStragglersAreRefusedByName()
        {
            using (var host = new LoopbackHost(SessionKey))
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                var clients = seats
                    .Select(seat => new LoopbackClient(host.Identity, seat, SessionKey))
                    .ToArray();
                try
                {
                    // One deterministic duplicate first: seat the first client, then a second
                    // connection claims the same seat while five stay open.
                    Assert.That(clients[0].Connect(host.DialEndpoint, out var error), Is.True, error);
                    PumpUntil(host, new[] { clients[0] },
                        () => clients[0].Machine.State == PrivateSessionClientState.Seated);

                    using (var duplicate = new LoopbackClient(host.Identity, seats[0], SessionKey))
                    {
                        Assert.That(duplicate.Connect(host.DialEndpoint, out var dupError), Is.True, dupError);
                        PumpUntil(host, new[] { duplicate },
                            () => duplicate.Machine.State == PrivateSessionClientState.Rejected);
                        Assert.That(duplicate.Machine.RejectionReason,
                            Is.EqualTo(SessionRejectionReason.SeatAlreadyClaimed));
                    }

                    // A wrong key never reaches the roster.
                    using (var uninvited = new LoopbackClient(host.Identity, seats[1], "wrong-key"))
                    {
                        Assert.That(uninvited.Connect(host.DialEndpoint, out var keyError), Is.True, keyError);
                        PumpUntil(host, new[] { uninvited },
                            () => uninvited.Machine.State == PrivateSessionClientState.Rejected);
                        Assert.That(uninvited.Machine.RejectionReason,
                            Is.EqualTo(SessionRejectionReason.SessionKeyMismatch));
                    }

                    // The remaining five fill the session.
                    for (var index = 1; index < clients.Length; index++)
                    {
                        Assert.That(clients[index].Connect(host.DialEndpoint, out var laterError),
                            Is.True, laterError);
                    }

                    PumpUntil(host, clients, () => clients.All(client =>
                        client.Machine.State == PrivateSessionClientState.Seated));
                    Assert.That(host.Session.IsComplete, Is.True);
                    Assert.That(clients.Select(client => client.Machine.GrantedSeat.Seat.Value),
                        Is.EquivalentTo(seats));

                    // A compatible seventh player finds no seat left, by name.
                    using (var straggler = new LoopbackClient(host.Identity, seats[2], SessionKey))
                    {
                        Assert.That(straggler.Connect(host.DialEndpoint, out var lateError), Is.True, lateError);
                        PumpUntil(host, clients.Concat(new[] { straggler }),
                            () => straggler.Machine.State == PrivateSessionClientState.Rejected);
                        Assert.That(straggler.Machine.RejectionReason,
                            Is.EqualTo(SessionRejectionReason.SessionFull));
                    }

                    // Everyone seated is still seated.
                    Assert.That(clients.All(client =>
                        client.Machine.State == PrivateSessionClientState.Seated), Is.True);
                }
                finally
                {
                    foreach (var client in clients)
                    {
                        client.Dispose();
                    }
                }
            }
        }

        [Test]
        public void AnOversizedFrame_DisconnectsOnlyItsOwnConnection()
        {
            using (var host = new LoopbackHost(SessionKey))
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var seated = new LoopbackClient(host.Identity, seats[0], SessionKey))
                using (var hostile = new LoopbackClient(host.Identity, seats[1], SessionKey))
                {
                    Assert.That(seated.Connect(host.DialEndpoint, out var seatedError), Is.True, seatedError);
                    PumpUntil(host, new[] { seated },
                        () => seated.Machine.State == PrivateSessionClientState.Seated);

                    Assert.That(hostile.Connect(host.DialEndpoint, out var hostileError), Is.True, hostileError);
                    PumpUntil(host, new[] { seated, hostile },
                        () => hostile.Machine.State == PrivateSessionClientState.AwaitingWelcome);

                    // Twelve header bytes declaring a payload past the bound - nothing more is sent,
                    // and nothing more is needed for the refusal.
                    var header = new byte[PrivateSessionProtocol.FrameHeaderBytes];
                    header[0] = PrivateSessionProtocol.FrameMagic0;
                    header[1] = PrivateSessionProtocol.FrameMagic1;
                    header[2] = PrivateSessionProtocol.FrameMagic2;
                    header[3] = PrivateSessionProtocol.FrameMagic3;
                    header[4] = (byte)PrivateSessionProtocol.Version;
                    header[6] = (byte)SessionMessageKind.Hello;
                    var huge = PrivateSessionProtocol.MaxPayloadBytes + 1;
                    header[8] = (byte)(huge & 0xFF);
                    header[9] = (byte)((huge >> 8) & 0xFF);
                    header[10] = (byte)((huge >> 16) & 0xFF);
                    header[11] = (byte)((huge >> 24) & 0xFF);
                    hostile.Transport.Send(header);

                    PumpUntil(host, new[] { seated, hostile }, () =>
                        hostile.Machine.State == PrivateSessionClientState.Rejected ||
                        hostile.Machine.State == PrivateSessionClientState.Disconnected ||
                        hostile.Machine.State == PrivateSessionClientState.Faulted);
                    Assert.That(seated.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated),
                        "The hostile connection is refused alone; its neighbor keeps its seat.");
                    Assert.That(host.Session.ClaimedSeatCount, Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void Dispose_JoinsItsThreadsAndFreesThePort()
        {
            int port;
            using (var host = new LoopbackHost(SessionKey))
            {
                port = host.Listener.BoundPort;
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var client = new LoopbackClient(host.Identity, seats[0], SessionKey))
                {
                    Assert.That(client.Connect(host.DialEndpoint, out var error), Is.True, error);
                    PumpUntil(host, new[] { client },
                        () => client.Machine.State == PrivateSessionClientState.Seated);
                }
            }

            var rebind = new TcpListener(IPAddress.Loopback, port);
            rebind.Start();
            rebind.Stop();
        }
    }
}
