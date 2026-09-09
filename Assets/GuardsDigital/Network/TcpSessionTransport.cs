using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using GuardsDigital.Application;

namespace GuardsDigital.Network
{
    /// <summary>The adapter's challenge nonces: platform entropy, which only this assembly may draw.</summary>
    /// <remarks>
    /// The setup-randomness conformance rule keeps entropy out of Core and Application so the game
    /// stays a function of its recorded seed. A session nonce is not game state - it exists to be
    /// unpredictable - so it lives here, beneath the transport-neutral port, in the assembly the
    /// scan does not govern.
    /// </remarks>
    public sealed class CryptoNonceSource : IPrivateSessionNonceSource
    {
        public byte[] NextNonce()
        {
            var nonce = new byte[16];
            using (var generator = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                generator.GetBytes(nonce);
            }

            return nonce;
        }
    }

    /// <summary>What a transport reports upward, drained on the pump thread.</summary>
    public enum TransportEventKind
    {
        None = 0,
        Connected = 1,
        Frame = 2,
        StreamRejected = 3,
        Disconnected = 4
    }

    /// <summary>
    /// One transport event. The ordinal is the connection's opaque host-side name; a client
    /// transport, having exactly one connection, reports ordinal zero.
    /// </summary>
    public readonly struct TransportEvent
    {
        public TransportEvent(TransportEventKind kind, int ordinal, SessionFrame frame, SessionRejectionReason reason)
        {
            Kind = kind;
            Ordinal = ordinal;
            Frame = frame;
            Reason = reason;
        }

        public TransportEventKind Kind { get; }
        public int Ordinal { get; }
        public SessionFrame Frame { get; }
        public SessionRejectionReason Reason { get; }
    }

    /// <summary>
    /// Direct TCP listener beneath the single-threaded session executor. Each retained connection
    /// owns one bounded reader/writer pair. Remote endpoints never enter events or diagnostics.
    /// </summary>
    public sealed class TcpSessionListener : IDisposable, IPrivateSessionOutbox
    {
        private readonly TcpTransportEventBuffer events = new TcpTransportEventBuffer();
        private readonly Dictionary<int, BoundedTcpConnection> connections = new Dictionary<int, BoundedTcpConnection>();
        private readonly object connectionsGate = new object();
        private TcpListener listener;
        private Thread acceptThread;
        private int nextOrdinal;
        private volatile bool disposed;
        public int BoundPort { get; private set; }
        public long PendingPayloadBytes => events.PendingPayloadBytes;
        public int RetainedConnectionCount { get { lock (connectionsGate) return connections.Count; } }

        public void Start(PrivateSessionEndpoint bind)
        {
            if (bind == null) throw new ArgumentNullException(nameof(bind));
            if (disposed) throw new ObjectDisposedException(nameof(TcpSessionListener));
            if (listener != null) throw new InvalidOperationException("The listener is already started.");
            listener = new TcpListener(bind.ToIPEndPoint());
            listener.Start(PrivateSessionProtocol.ListenBacklog);
            BoundPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "guards-online-accept" };
            acceptThread.Start();
        }

        public bool TryDequeue(out TransportEvent value)
        {
            lock (connectionsGate) ReapStoppedConnections();
            if (!events.TryDequeue(out value)) return false;
            if (value.Kind == TransportEventKind.Disconnected)
                lock (connectionsGate)
                {
                    if (connections.TryGetValue(value.Ordinal, out var connection)) connection.DisconnectedConsumed = true;
                    ReapStoppedConnections();
                }
            return true;
        }

        private void ReapStoppedConnections()
        {
            // Called under connectionsGate. The final event can be consumed just before its
            // receiver returns; retain that slot until both OS threads have actually exited.
            List<int> retired = null;
            foreach (var pair in connections)
                if (pair.Value.DisconnectedConsumed && !pair.Value.ReceiveThread.IsAlive && !pair.Value.SendThread.IsAlive)
                {
                    if (retired == null) retired = new List<int>();
                    retired.Add(pair.Key);
                }
            if (retired != null) foreach (var ordinal in retired) connections.Remove(ordinal);
        }

        /// <summary>One finite pump batch, leaving authority sweeps and UI updates time to run.</summary>
        public IEnumerable<TransportEvent> Drain()
        {
            for (var count = 0; count < TcpSessionTransportLimits.PumpEventBudget && TryDequeue(out var value); count++)
                yield return value;
        }

        public void Send(int connectionOrdinal, byte[] frame)
        {
            BoundedTcpConnection connection;
            lock (connectionsGate) connections.TryGetValue(connectionOrdinal, out connection);
            connection?.Send(frame);
        }

        public void Close(int connectionOrdinal)
        {
            BoundedTcpConnection connection;
            lock (connectionsGate) connections.TryGetValue(connectionOrdinal, out connection);
            // Preserve an already queued terminal Reject/Goodbye before closing, without waiting
            // on the caller/pump thread. Overflow, I/O failure and disposal have bounded abort paths.
            connection?.RequestClose();
        }

        public void Dispose()
        {
            List<BoundedTcpConnection> open;
            lock (connectionsGate)
            {
                if (disposed) return;
                disposed = true;
                open = new List<BoundedTcpConnection>(connections.Values);
            }
            events.Stop();
            try { listener?.Stop(); }
            catch (SocketException) { }
            BoundedTcpConnection.CloseAll(open, acceptThread);
            lock (connectionsGate) connections.Clear();
        }

        private void AcceptLoop()
        {
            while (!disposed)
            {
                TcpClient client;
                try { client = listener.AcceptTcpClient(); }
                catch (Exception error) when (BoundedTcpConnection.ExpectedFault(error)) { return; }
                lock (connectionsGate)
                {
                    ReapStoppedConnections();
                    // Retired connections keep their slot until their final event is consumed.
                    // This bounds sockets, workers AND queued control events before admission.
                    if (disposed || connections.Count >= PrivateSessionProtocol.MaxConcurrentConnections)
                    {
                        client.Close();
                        continue;
                    }
                    try
                    {
                        var connection = new BoundedTcpConnection(++nextOrdinal, client, events);
                        connections.Add(connection.Ordinal, connection);
                        connection.Start();
                    }
                    catch (Exception error) when (BoundedTcpConnection.ExpectedFault(error)) { client.Close(); }
                }
            }
        }
    }

    /// <summary>One bounded client connection, using the same ordered writer/receiver as the host.</summary>
    public sealed class TcpSessionClientTransport : IDisposable, IPrivateSessionClientOutbox
    {
        private readonly TcpTransportEventBuffer events = new TcpTransportEventBuffer();
        private BoundedTcpConnection connection;
        private bool disposed;
        public long PendingPayloadBytes => events.PendingPayloadBytes;

        public bool Connect(PrivateSessionEndpoint target, int timeoutSeconds, out string error)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (disposed) throw new ObjectDisposedException(nameof(TcpSessionClientTransport));
            if (connection != null) throw new InvalidOperationException("The transport is already connected.");
            error = string.Empty;
            var candidate = new TcpClient(target.Address.AddressFamily);
            try
            {
                var pending = candidate.BeginConnect(target.Address, target.Port, null, null);
                using (var wait = pending.AsyncWaitHandle)
                {
                    if (!wait.WaitOne(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        candidate.Close();
                        error = "The host did not answer within " + timeoutSeconds + " seconds.";
                        return false;
                    }
                }
                candidate.EndConnect(pending);
                connection = new BoundedTcpConnection(0, candidate, events);
                connection.Start();
                return true;
            }
            catch (Exception failure) when (BoundedTcpConnection.ExpectedFault(failure))
            {
                candidate.Close();
                error = "The connection was refused or the address is unreachable.";
                return false;
            }
        }

        public bool TryDequeue(out TransportEvent value) => events.TryDequeue(out value);
        public IEnumerable<TransportEvent> Drain()
        {
            for (var count = 0; count < TcpSessionTransportLimits.PumpEventBudget && TryDequeue(out var value); count++)
                yield return value;
        }

        public void Send(byte[] frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            connection?.Send(frame);
        }

        public void Close() => connection?.RequestClose();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            events.Stop();
            try
            {
                if (connection != null) BoundedTcpConnection.CloseAll(new[] { connection });
            }
            finally { events.Stop(connection != null ? (int?)0 : null); }
        }
    }
}
