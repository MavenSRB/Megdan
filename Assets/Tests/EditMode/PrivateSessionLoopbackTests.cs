using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Reflection;
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
        // Regression probes intentionally read the old adapter's private counters too, so the
        // exact same checks can demonstrate failure before the bounded adapter exists.
        private static IDictionary Connections(TcpSessionListener listener) =>
            (IDictionary)typeof(TcpSessionListener).GetField("connections", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(listener);

        private static TcpClient ServerSocket(TcpSessionListener listener, int ordinal)
        {
            var connection = Connections(listener)[ordinal];
            return (TcpClient)connection.GetType().GetProperty("Client").GetValue(connection);
        }

        private static long PendingBytes(object transport)
        {
            var property = transport.GetType().GetProperty("PendingPayloadBytes");
            if (property != null) return Convert.ToInt64(property.GetValue(transport));
            var queue = (IEnumerable)transport.GetType().GetField("events", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(transport);
            return queue.Cast<TransportEvent>().Sum(value => (long)(value.Frame.Payload?.Length ?? 0));
        }

        private static void Until(Func<bool> done, Action pump = null, int milliseconds = 5000)
        {
            var timer = Stopwatch.StartNew();
            while (!done() && timer.ElapsedMilliseconds < milliseconds) { pump?.Invoke(); Thread.Sleep(5); }
            Assert.That(done(), Is.True, "The bounded socket condition was not reached.");
        }

        private sealed class ManuallyReadClient : IDisposable, IPrivateSessionClientOutbox
        {
            public TcpClient Socket { get; } = new TcpClient();
            public PrivateSessionClient Machine { get; }
            private readonly PrivateSessionFrameAssembler assembler = new PrivateSessionFrameAssembler();
            private readonly byte[] buffer = new byte[4096];
            public ManuallyReadClient(LoopbackHost host, string seat)
            {
                Socket.ReceiveBufferSize = 4096;
                Socket.NoDelay = true;
                Socket.Connect(IPAddress.Loopback, host.Listener.BoundPort);
                Machine = new PrivateSessionClient(host.Identity, new SeatId(seat), "", this);
                Machine.OnConnected();
            }
            public void Send(byte[] frame) => Socket.GetStream().Write(frame, 0, frame.Length);
            public void Close() => Socket.Close();
            public void Dispose() => Close();
            public void Pump()
            {
                var stream = Socket.GetStream();
                for (var count = 0; count < 64 && stream.DataAvailable; count++)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    Assert.That(assembler.Add(buffer, 0, read), Is.True);
                    while (assembler.TryDequeue(out var frame)) Machine.OnFrame(frame);
                }
            }
        }

        [Test]
        public void NonReadingPeerCannotBlockHostPumpOrDispose()
        {
            using (var host = new LoopbackHost(""))
            using (var stalled = new ManuallyReadClient(host, "blue-player-01"))
            using (var healthy = new ManuallyReadClient(host, "blue-player-02"))
            using (var continueSending = new ManualResetEventSlim())
            {
                Until(() => stalled.Machine.State == PrivateSessionClientState.Seated &&
                    healthy.Machine.State == PrivateSessionClientState.Seated,
                    () => { host.Pump(); stalled.Pump(); healthy.Pump(); });
                var ordinal = Connections(host.Listener).Keys.Cast<int>().Single(value =>
                    host.Session.TryGetSeatForConnection(value, out var seat) && seat.Id.Value == "blue-player-01");
                var socket = ServerSocket(host.Listener, ordinal);
                socket.SendBufferSize = 4096;
                var largeFrame = PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.Sealed,
                    new byte[PrivateSessionProtocol.MaxPayloadBytes]);
                Exception workerError = null;
                var worker = new Thread(() =>
                {
                    try
                    {
                        host.Listener.Send(ordinal, largeFrame);
                        continueSending.Wait(5000);
                        for (var count = 0; count < 256; count++) host.Listener.Send(ordinal, largeFrame);
                        host.Pump();
                    }
                    catch (Exception error) { workerError = error; }
                }) { IsBackground = true };
                worker.Start();
                try
                {
                    // This is actual kernel backpressure, not just a large API call or an
                    // artificially full application queue. The peer remains connected/unread.
                    Until(() => !socket.Client.Poll(0, SelectMode.SelectWrite));
                    Assert.That(stalled.Socket.Available, Is.GreaterThan(0));
                    continueSending.Set();
                    Assert.That(worker.Join(4000), Is.True, "A non-reader blocked host publication/control.");
                    Assert.That(workerError, Is.Null);
                    Until(() => healthy.Machine.Roster.Any(row => row.Seat.Value == "blue-player-01" &&
                        row.Occupancy == SeatOccupancyState.Reserved), () => { host.Pump(); healthy.Pump(); });
                    Assert.That(host.Session.IsPaused, Is.True);
                    Assert.That(host.Session.TryGetSeatForConnection(ordinal, out _), Is.False);
                    var closing = Stopwatch.StartNew();
                    host.Listener.Dispose();
                    Assert.That(closing.ElapsedMilliseconds, Is.LessThan(1000), "Disposal must not wait on a blocked writer.");
                }
                finally
                {
                    continueSending.Set();
                    stalled.Close(); // Unblock the known-bad baseline without leaking a worker.
                    worker.Join(5000);
                }
            }
        }

        [Test]
        public void TransportIngressRemainsBoundedBeforeAdmission()
        {
            using (var host = new LoopbackHost(""))
            {
                var peers = new List<TcpClient>();
                try
                {
                    // No host pump/admission occurs during this burst.
                    for (var count = 0; count < PrivateSessionProtocol.MaxConcurrentConnections + 4; count++)
                    {
                        var peer = new TcpClient { SendTimeout = 1000 };
                        peer.Connect(IPAddress.Loopback, host.Listener.BoundPort);
                        peers.Add(peer);
                    }
                    Thread.Sleep(100);
                    var accepted = Connections(host.Listener).Count;
                    var frame = PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.Sealed,
                        new byte[PrivateSessionProtocol.MaxPayloadBytes]);
                    try
                    {
                        for (var count = 0; count < 128; count++) peers[0].GetStream().Write(frame, 0, frame.Length);
                    }
                    catch (System.IO.IOException) { } // The bounded implementation closes overflow.
                    catch (SocketException) { }
                    Thread.Sleep(100);
                    Assert.That(accepted, Is.LessThanOrEqualTo(12), "Accepted sockets must be capped before admission.");
                    Assert.That(PendingBytes(host.Listener), Is.LessThanOrEqualTo(12L * 512 * 1024),
                        "Complete frames must not accumulate unbounded payloads before the pump.");
                    foreach (var peer in peers) peer.Close();
                    Until(() => Connections(host.Listener).Count == 0, () =>
                    { while (host.Listener.TryDequeue(out _)) { } });
                    using (var healthy = new ManuallyReadClient(host, "blue-player-01"))
                    {
                        Until(() => healthy.Machine.State == PrivateSessionClientState.Seated,
                            () => { host.Pump(); healthy.Pump(); });
                    }
                    host.Listener.Dispose();
                    Assert.That(PendingBytes(host.Listener), Is.Zero, "Disposal releases queued payloads.");
                }
                finally { foreach (var peer in peers) peer.Dispose(); }
            }
        }

        private const string SessionKey = "loopback-key";
        [TestCase("")]
        [TestCase(SessionKey)]
        public void ImmediateClientDisposeFlushesDeliberateGoodbye(string key)
        {
            using (var host = new LoopbackHost(key))
            using (var client = new LoopbackClient(host.Identity, "blue-player-01", key))
            {
                Assert.That(client.Connect(host.DialEndpoint, out _), Is.True);
                PumpUntil(host, new[] { client }, () => client.Machine.State == PrivateSessionClientState.Seated);
                client.Machine.SendGoodbye();
                client.Transport.Dispose();
                Until(() => host.Session.Roster.Single(row => row.Seat.Value == "blue-player-01").Occupancy ==
                    SeatOccupancyState.Open, host.Pump);
            }
        }

        [Test]
        public void ClientCloseCancelsABackpressuredWriter()
        {
            var server = new TcpListener(IPAddress.Loopback, 0);
            server.Server.ReceiveBufferSize = 4096;
            server.Start();
            try
            {
                Assert.That(PrivateSessionEndpoint.TryCreate("127.0.0.1", ((IPEndPoint)server.LocalEndpoint).Port,
                    out var endpoint, out _), Is.True);
                using (var transport = new TcpSessionClientTransport())
                {
                    Assert.That(transport.Connect(endpoint, 2, out _), Is.True);
                    using (var peer = server.AcceptTcpClient())
                    {
                        var connection = typeof(TcpSessionClientTransport).GetField("connection",
                            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(transport);
                        var socket = (TcpClient)connection.GetType().GetProperty("Client").GetValue(connection);
                        socket.SendBufferSize = 4096;
                        transport.Send(PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.Sealed,
                            new byte[PrivateSessionProtocol.MaxPayloadBytes]));
                        Until(() => !socket.Client.Poll(0, SelectMode.SelectWrite));
                        Assert.That(peer.Available, Is.GreaterThan(0));
                        var timer = Stopwatch.StartNew();
                        transport.Close();
                        transport.Dispose();
                        Assert.That(timer.ElapsedMilliseconds, Is.LessThan(1000));
                    }
                }
            }
            finally { server.Stop(); }
        }

        [Test]
        public void ClientIngressAndBothPumpBatchesStayBounded()
        {
            using (var host = new LoopbackHost(""))
            using (var first = new TcpClient())
            using (var second = new TcpClient())
            {
                first.Connect(IPAddress.Loopback, host.Listener.BoundPort);
                second.Connect(IPAddress.Loopback, host.Listener.BoundPort);
                var small = PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.Sealed, new byte[1]);
                for (var i = 0; i < 40; i++)
                {
                    first.GetStream().Write(small, 0, small.Length);
                    second.GetStream().Write(small, 0, small.Length);
                }
                Until(() => host.Listener.PendingPayloadBytes == 80);
                Assert.That(host.Listener.Drain().Count(), Is.EqualTo(TcpSessionTransportLimits.PumpEventBudget));
                Assert.That(host.Listener.PendingPayloadBytes, Is.GreaterThan(0), "A pump must leave later work for the next update.");
            }
            var server = new TcpListener(IPAddress.Loopback, 0);
            server.Start();
            try
            {
                Assert.That(PrivateSessionEndpoint.TryCreate("127.0.0.1", ((IPEndPoint)server.LocalEndpoint).Port,
                    out var endpoint, out _), Is.True);
                using (var transport = new TcpSessionClientTransport())
                {
                    Assert.That(transport.Connect(endpoint, 2, out _), Is.True);
                    using (var peer = server.AcceptTcpClient())
                    {
                        peer.SendTimeout = 1000;
                        var small = PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.Sealed, new byte[1]);
                        for (var i = 0; i < 64; i++) peer.GetStream().Write(small, 0, small.Length);
                        Until(() => transport.PendingPayloadBytes == 64);
                        Assert.That(transport.Drain().Count(), Is.EqualTo(TcpSessionTransportLimits.PumpEventBudget));
                        Assert.That(transport.TryDequeue(out _), Is.True, "Client pumping is also bounded.");
                        var large = PrivateSessionFrameCodec.EncodeFrame(SessionMessageKind.Sealed,
                            new byte[PrivateSessionProtocol.MaxPayloadBytes]);
                        try { for (var i = 0; i < 128; i++) peer.GetStream().Write(large, 0, large.Length); }
                        catch (System.IO.IOException) { }
                        catch (SocketException) { }
                        Thread.Sleep(100);
                        Assert.That(transport.PendingPayloadBytes, Is.LessThanOrEqualTo(512 * 1024));
                        transport.Dispose();
                        Assert.That(transport.PendingPayloadBytes, Is.Zero);
                    }
                }
            }
            finally { server.Stop(); }
        }

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
                Identity = PrivateSessionIdentity.ForComposition(
                    map, configuration,
                    OwnerScopedProjectionTests.CreateOfficialInitialState().Setup,
                    "loopback-build");
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
