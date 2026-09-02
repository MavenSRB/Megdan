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
    /// Milestone 8.34. The reconnect slice over real sockets: a seat whose connection is lost
    /// comes back on a second one under its own capability, with a rotated connection epoch and a
    /// complete owner-scoped projection; a connection presenting anything else is refused before
    /// it learns a private byte; and a client whose host disappears reaches the terminal
    /// host-loss state rather than waiting on an authority that is not there.
    /// </summary>
    /// <remarks>
    /// The in-memory harness fixtures prove the same rules against every cut point the matrix
    /// names; this is the socket-level restatement, so nothing the adapter does to real byte
    /// streams - a second accept, segmentation, the seal - can hide what the harness would catch.
    /// Host events are matched by seat rather than by connection ordinal: two accepts race, and
    /// the ordinal a reconnect is given is exactly what this fixture must not assume.
    /// </remarks>
    public sealed class PrivateSessionReconnectAdapterTests
    {
        private const string SessionKey = "adapter-reconnect-key";
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
                    map, configuration, "adapter-reconnect-build");
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
                Events.AddRange(Session.DrainEvents());
            }

            public void Dispose() => Listener.Dispose();
        }

        /// <summary>
        /// One client machine that may be dialled more than once, because that is the whole point:
        /// the machine is the player, and the transport is what it loses and replaces.
        /// </summary>
        /// <summary>
        /// The outbox a client machine keeps across a lost connection: it forwards to whichever
        /// transport is live now.
        /// </summary>
        /// <remarks>
        /// A machine's outbox is fixed at construction, and the socket it was built over is
        /// exactly what a reconnect replaces - so a returning client needs this indirection or it
        /// answers the second challenge down the first, dead socket. Recorded here because the
        /// shipped <c>PrivateClientDriver</c> holds one transport for its process and will need
        /// the same seam when milestone 8.35a gives a player a way to ask for a reconnect.
        /// </remarks>
        private sealed class SwappableOutbox : IPrivateSessionClientOutbox
        {
            public TcpSessionClientTransport Live { get; set; }

            public void Send(byte[] frame) => Live?.Send(frame);

            public void Close() => Live?.Close();
        }

        private sealed class AdapterClient : IDisposable
        {
            private readonly List<TcpSessionClientTransport> transports =
                new List<TcpSessionClientTransport>();
            private readonly SwappableOutbox outbox = new SwappableOutbox();

            public PrivateSessionClient Machine { get; }

            /// <summary>What the live transport actually delivered, before any unsealing.</summary>
            public List<SessionMessageKind> WireKinds { get; } = new List<SessionMessageKind>();

            public TcpSessionClientTransport Transport => transports[transports.Count - 1];

            public AdapterClient(PrivateSessionIdentity identity, string seat, string sessionKey)
            {
                var transport = new TcpSessionClientTransport();
                transports.Add(transport);
                outbox.Live = transport;
                Machine = new PrivateSessionClient(
                    identity, new SeatId(seat), sessionKey, outbox, new CryptoNonceSource());
            }

            public bool Connect(PrivateSessionEndpoint endpoint, out string error) =>
                Transport.Connect(endpoint, 5, out error);

            /// <summary>Drops this client's socket the way a lost link does: no Goodbye either way.</summary>
            public void DropTheLink() => Transport.Dispose();

            /// <summary>
            /// Arms the machine's return and dials again on a fresh socket, which the host sees as
            /// a new connection with a new ordinal.
            /// </summary>
            public bool Redial(PrivateSessionEndpoint endpoint, out string error)
            {
                error = null;
                // Whatever the lost link last said - up to and including that it was lost - is
                // read before the machine is asked to come back, because the state it is in is
                // what decides whether it may.
                Pump();
                if (!Machine.BeginReconnect())
                {
                    return false;
                }

                var transport = new TcpSessionClientTransport();
                transports.Add(transport);
                outbox.Live = transport;
                return transport.Connect(endpoint, 5, out error);
            }

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

            public void Dispose()
            {
                foreach (var transport in transports)
                {
                    transport.Dispose();
                }
            }
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
        /// The reconnect row over the sealed adapter: the seat is reserved when its socket goes,
        /// the same player returns on a second socket, the epoch rotates, and a complete
        /// owner-scoped projection comes back sealed.
        /// </summary>
        [Test]
        public void AReservedSeat_ReturnsOverTheSealedAdapter_WithARotatedEpochAndACompleteProjection()
        {
            using (var host = new AdapterHost(SessionKey))
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var away = new AdapterClient(host.Identity, seats[0], SessionKey))
                using (var staying = new AdapterClient(host.Identity, seats[1], SessionKey))
                {
                    Assert.That(away.Connect(host.DialEndpoint, out var awayError), Is.True, awayError);
                    Assert.That(staying.Connect(host.DialEndpoint, out var stayError), Is.True, stayError);
                    PumpUntil(host, new[] { away, staying }, () =>
                        away.Machine.LatestProjection != null &&
                        staying.Machine.LatestProjection != null);

                    var seat = new SeatId(seats[0]);
                    Assert.That(host.Session.EpochForSeat(seat),
                        Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch));
                    var version = host.MatchHost.Version;

                    away.DropTheLink();
                    PumpUntil(host, new[] { staying }, () =>
                        host.Session.Roster.Single(row => row.Seat.Equals(seat)).Occupancy ==
                        SeatOccupancyState.Reserved);
                    away.Pump();
                    Assert.That(away.Machine.State, Is.EqualTo(PrivateSessionClientState.HostLost),
                        "A socket that ends with nothing said is the host-loss row, not an " +
                        "orderly close.");
                    Assert.That(away.Machine.CanAttemptReconnect, Is.True);
                    Assert.That(host.Session.IsPaused, Is.True);

                    Assert.That(away.Redial(host.DialEndpoint, out var redialError), Is.True,
                        redialError);
                    PumpUntil(host, new[] { away, staying }, () =>
                        away.Machine.State == PrivateSessionClientState.Seated &&
                        away.Machine.LatestProjection != null &&
                        away.Machine.LatestProjection.Version == host.MatchHost.Version);

                    Assert.That(host.Session.EpochForSeat(seat),
                        Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch + 1),
                        "The returning seat speaks under a rotated epoch.");
                    Assert.That(away.Machine.ConnectionEpoch,
                        Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch + 1));
                    Assert.That(away.Machine.GrantedSeat.Seat, Is.EqualTo(seat));
                    Assert.That(away.Machine.LatestProjection.SeatId, Is.EqualTo(seats[0]));
                    Assert.That(away.Machine.LatestProjection.Detail, Is.Not.Null,
                        "What comes back is the complete projection, detail record and all.");
                    Assert.That(host.MatchHost.Version, Is.EqualTo(version),
                        "Nothing moved while the seat was away.");
                    Assert.That(host.Session.IsPaused, Is.False);

                    // Matched by seat, never by ordinal: two accepts race.
                    Assert.That(host.Events.Count(record =>
                        record.Kind == PrivateSessionEventKind.SeatReclaimed &&
                        record.Seat.Equals(seat)), Is.EqualTo(1));
                    Assert.That(staying.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated),
                        "One seat's return is nobody else's business.");

                    // And on the wire itself nothing after the second challenge was plaintext.
                    Assert.That(away.WireKinds.Count(kind => kind == SessionMessageKind.Challenge),
                        Is.EqualTo(2), "Each connection is challenged once.");
                    Assert.That(away.WireKinds.Where(kind => kind != SessionMessageKind.Challenge),
                        Has.All.EqualTo(SessionMessageKind.Sealed),
                        "A post-challenge frame crossed the keyed link in plaintext.");
                }
            }
        }

        /// <summary>
        /// Seat control never transfers over the adapter either: a connection that reclaims a
        /// reserved seat without its capability is refused and closed, and no owner-private byte
        /// crosses to it first.
        /// </summary>
        [Test]
        public void AReclaimWithoutTheCapability_OverTheAdapter_IsRefusedAndTheSeatStaysReserved()
        {
            using (var host = new AdapterHost(""))
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var away = new AdapterClient(host.Identity, seats[0], ""))
                using (var impostor = new AdapterClient(host.Identity, seats[0], ""))
                {
                    Assert.That(away.Connect(host.DialEndpoint, out var awayError), Is.True, awayError);
                    PumpUntil(host, new[] { away }, () => away.Machine.LatestProjection != null);

                    var seat = new SeatId(seats[0]);
                    away.DropTheLink();
                    PumpUntil(host, Array.Empty<AdapterClient>(), () =>
                        host.Session.Roster.Single(row => row.Seat.Equals(seat)).Occupancy ==
                        SeatOccupancyState.Reserved);

                    // A second machine for the same seat holds no capability, so its ordinary
                    // claim is what reaches the host - and a reserved seat refuses one.
                    Assert.That(impostor.Connect(host.DialEndpoint, out var impostorError), Is.True,
                        impostorError);
                    PumpUntil(host, new[] { impostor }, () =>
                        impostor.Machine.State == PrivateSessionClientState.Rejected);

                    Assert.That(impostor.Machine.RejectionReason,
                        Is.EqualTo(SessionRejectionReason.SeatReserved));
                    Assert.That(impostor.WireKinds,
                        Has.None.EqualTo(SessionMessageKind.SeatGranted)
                            .And.None.EqualTo(SessionMessageKind.SeatProjection),
                        "A refused connection is told the refusal and nothing owner-private.");
                    Assert.That(host.Session.Roster.Single(row => row.Seat.Equals(seat)).Occupancy,
                        Is.EqualTo(SeatOccupancyState.Reserved),
                        "The refused claim left the reservation for the player it belongs to.");
                    Assert.That(host.Session.EpochForSeat(seat),
                        Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch),
                        "A refused claim rotates nothing.");

                    // And the player who does hold it still gets the seat back afterwards.
                    Assert.That(away.Redial(host.DialEndpoint, out var redialError), Is.True,
                        redialError);
                    PumpUntil(host, new[] { away, impostor }, () =>
                        away.Machine.State == PrivateSessionClientState.Seated);
                    Assert.That(host.Session.Roster.Single(row => row.Seat.Equals(seat)).Occupancy,
                        Is.EqualTo(SeatOccupancyState.Claimed));
                }
            }
        }

        /// <summary>
        /// Host loss over the adapter: the listener goes and every seated client reaches the
        /// terminal state rather than waiting, holding what it last saw and able to submit none
        /// of it.
        /// </summary>
        [Test]
        public void HostLoss_OverTheAdapter_IsTerminalForEverySeatedClient()
        {
            var host = new AdapterHost("");
            try
            {
                var seats = host.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
                using (var first = new AdapterClient(host.Identity, seats[0], ""))
                using (var second = new AdapterClient(host.Identity, seats[1], ""))
                {
                    Assert.That(first.Connect(host.DialEndpoint, out var firstError), Is.True,
                        firstError);
                    Assert.That(second.Connect(host.DialEndpoint, out var secondError), Is.True,
                        secondError);
                    PumpUntil(host, new[] { first, second }, () =>
                        first.Machine.LatestProjection != null &&
                        second.Machine.LatestProjection != null);
                    var seen = first.Machine.LatestProjection;

                    // The listener and every connection under it disappear, with nothing said.
                    host.Dispose();

                    var stopwatch = Stopwatch.StartNew();
                    while (first.Machine.State != PrivateSessionClientState.HostLost ||
                        second.Machine.State != PrivateSessionClientState.HostLost)
                    {
                        Assert.That(stopwatch.Elapsed, Is.LessThan(Deadline),
                            "A seated client did not notice its host was gone: " +
                            first.Machine.State + " / " + second.Machine.State + ".");
                        first.Pump();
                        second.Pump();
                        Thread.Sleep(5);
                    }

                    Assert.That(first.Machine.LatestProjection, Is.SameAs(seen),
                        "A lost host takes the ability to act, not the position on the board.");
                    Assert.That(first.Machine.TrySubmitCommand(
                            first.Machine.LatestProjection.AuthorizedCommands[0], out _), Is.False,
                        "Nothing is submitted from a terminal state.");
                    Assert.That(first.Machine.RejectionReason,
                        Is.EqualTo(SessionRejectionReason.None),
                        "Nobody refused anything; the authority simply is not there.");
                }
            }
            finally
            {
                host.Dispose();
            }
        }
    }
}
