using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
    /// Milestone 8.31. The host's TCP listener: accepts connections, reassembles their byte
    /// streams into frames on background threads, and hands everything to one pump thread.
    /// </summary>
    /// <remarks>
    /// <para>Threading contract: sockets are the only threaded thing in the stack. One accept
    /// thread and one receive thread per connection push events onto a queue; the owner drains
    /// that queue on its own single thread and drives <see cref="PrivateSessionHost"/> from there,
    /// which is what keeps the session state machine single-threaded and, from 8.32 on, keeps the
    /// serialized executor's discipline true.</para>
    /// <para>Privacy: a connection is named by an opaque incrementing ordinal from the moment it
    /// is accepted. Its remote endpoint is never stored on any reachable field, never surfaced in
    /// an event, and never given to a log.</para>
    /// </remarks>
    public sealed class TcpSessionListener : IDisposable, IPrivateSessionOutbox
    {
        private sealed class Connection
        {
            public Connection(int ordinal, TcpClient client)
            {
                Ordinal = ordinal;
                Client = client;
            }

            public int Ordinal { get; }
            public TcpClient Client { get; }
            public Thread ReceiveThread { get; set; }
            public object SendGate { get; } = new object();
            public bool Closed { get; set; }
        }

        private readonly ConcurrentQueue<TransportEvent> events = new ConcurrentQueue<TransportEvent>();
        private readonly Dictionary<int, Connection> connections = new Dictionary<int, Connection>();
        private readonly object connectionsGate = new object();
        private TcpListener listener;
        private Thread acceptThread;
        private int nextOrdinal;
        private volatile bool disposed;

        /// <summary>The endpoint actually bound - the answer when port 0 asked for an ephemeral one.</summary>
        public int BoundPort { get; private set; }

        public void Start(PrivateSessionEndpoint bind)
        {
            if (bind == null) throw new ArgumentNullException(nameof(bind));
            if (listener != null)
            {
                throw new InvalidOperationException("The listener is already started.");
            }

            listener = new TcpListener(bind.ToIPEndPoint());
            listener.Start(PrivateSessionProtocol.ListenBacklog);
            BoundPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "guards-online-accept" };
            acceptThread.Start();
        }

        public bool TryDequeue(out TransportEvent transportEvent) => events.TryDequeue(out transportEvent);

        public void Send(int connectionOrdinal, byte[] frame)
        {
            Connection connection;
            lock (connectionsGate)
            {
                if (!connections.TryGetValue(connectionOrdinal, out connection))
                {
                    return;
                }
            }

            try
            {
                lock (connection.SendGate)
                {
                    if (!connection.Closed)
                    {
                        connection.Client.GetStream().Write(frame, 0, frame.Length);
                    }
                }
            }
            catch (Exception exception) when (IsExpectedSocketFault(exception))
            {
                // The receive thread will observe the same failure and report the disconnect.
            }
        }

        public void Close(int connectionOrdinal)
        {
            Connection connection;
            lock (connectionsGate)
            {
                if (!connections.TryGetValue(connectionOrdinal, out connection))
                {
                    return;
                }
            }

            CloseConnection(connection);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            try
            {
                listener?.Stop();
            }
            catch (SocketException)
            {
            }

            List<Connection> open;
            lock (connectionsGate)
            {
                open = new List<Connection>(connections.Values);
            }

            foreach (var connection in open)
            {
                CloseConnection(connection);
            }

            acceptThread?.Join(TimeSpan.FromSeconds(5));
            foreach (var connection in open)
            {
                connection.ReceiveThread?.Join(TimeSpan.FromSeconds(5));
            }
        }

        private void AcceptLoop()
        {
            while (!disposed)
            {
                TcpClient client;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception exception) when (IsExpectedSocketFault(exception))
                {
                    return;
                }

                client.NoDelay = true;
                var connection = new Connection(Interlocked.Increment(ref nextOrdinal), client);
                lock (connectionsGate)
                {
                    connections[connection.Ordinal] = connection;
                }

                events.Enqueue(new TransportEvent(
                    TransportEventKind.Connected, connection.Ordinal, default, SessionRejectionReason.None));
                connection.ReceiveThread = new Thread(() => ReceiveLoop(connection))
                {
                    IsBackground = true,
                    Name = "guards-online-receive-" + connection.Ordinal
                };
                connection.ReceiveThread.Start();
            }
        }

        private void ReceiveLoop(Connection connection)
        {
            var assembler = new PrivateSessionFrameAssembler();
            var buffer = new byte[4096];
            try
            {
                var stream = connection.Client.GetStream();
                while (!disposed && !connection.Closed)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        break;
                    }

                    if (!assembler.Add(buffer, 0, read))
                    {
                        events.Enqueue(new TransportEvent(
                            TransportEventKind.StreamRejected, connection.Ordinal, default,
                            assembler.Rejection));
                        return;
                    }

                    while (assembler.TryDequeue(out var frame))
                    {
                        events.Enqueue(new TransportEvent(
                            TransportEventKind.Frame, connection.Ordinal, frame,
                            SessionRejectionReason.None));
                    }
                }
            }
            catch (Exception exception) when (IsExpectedSocketFault(exception))
            {
            }
            finally
            {
                Forget(connection);
                events.Enqueue(new TransportEvent(
                    TransportEventKind.Disconnected, connection.Ordinal, default,
                    SessionRejectionReason.None));
            }
        }

        private void CloseConnection(Connection connection)
        {
            lock (connection.SendGate)
            {
                if (connection.Closed)
                {
                    return;
                }

                connection.Closed = true;
            }

            try
            {
                connection.Client.Close();
            }
            catch (Exception exception) when (IsExpectedSocketFault(exception))
            {
            }
        }

        private void Forget(Connection connection)
        {
            lock (connectionsGate)
            {
                connections.Remove(connection.Ordinal);
            }

            CloseConnection(connection);
        }

        private static bool IsExpectedSocketFault(Exception exception) =>
            exception is SocketException ||
            exception is ObjectDisposedException ||
            exception is IOException ||
            exception is InvalidOperationException;
    }

    /// <summary>
    /// Milestone 8.31. One client connection to the host: dial with a bounded timeout, then the
    /// same queue-and-pump shape as the listener, with ordinal zero.
    /// </summary>
    public sealed class TcpSessionClientTransport : IDisposable, IPrivateSessionClientOutbox
    {
        private readonly ConcurrentQueue<TransportEvent> events = new ConcurrentQueue<TransportEvent>();
        private readonly object sendGate = new object();
        private TcpClient client;
        private Thread receiveThread;
        private volatile bool closed;

        /// <summary>Dials the host. False, with a bounded error, when the host is unreachable.</summary>
        public bool Connect(PrivateSessionEndpoint target, int timeoutSeconds, out string error)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (client != null)
            {
                throw new InvalidOperationException("The transport is already connected.");
            }

            error = string.Empty;
            var candidate = new TcpClient(target.Address.AddressFamily);
            try
            {
                // TcpClient.Connect has no timeout of its own; a wrong address would otherwise
                // block for the platform's full TCP timeout.
                var pending = candidate.BeginConnect(target.Address, target.Port, null, null);
                if (!pending.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(timeoutSeconds)))
                {
                    candidate.Close();
                    error = "The host did not answer within " + timeoutSeconds + " seconds.";
                    return false;
                }

                candidate.EndConnect(pending);
            }
            catch (Exception exception) when (
                exception is SocketException || exception is ObjectDisposedException)
            {
                candidate.Close();
                error = "The connection was refused or the address is unreachable.";
                return false;
            }

            candidate.NoDelay = true;
            client = candidate;
            events.Enqueue(new TransportEvent(
                TransportEventKind.Connected, 0, default, SessionRejectionReason.None));
            receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "guards-online-client-receive"
            };
            receiveThread.Start();
            return true;
        }

        public bool TryDequeue(out TransportEvent transportEvent) => events.TryDequeue(out transportEvent);

        public void Send(byte[] frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            try
            {
                lock (sendGate)
                {
                    if (!closed && client != null)
                    {
                        client.GetStream().Write(frame, 0, frame.Length);
                    }
                }
            }
            catch (Exception exception) when (IsExpectedSocketFault(exception))
            {
            }
        }

        public void Close()
        {
            lock (sendGate)
            {
                if (closed)
                {
                    return;
                }

                closed = true;
            }

            try
            {
                client?.Close();
            }
            catch (Exception exception) when (IsExpectedSocketFault(exception))
            {
            }
        }

        public void Dispose()
        {
            Close();
            receiveThread?.Join(TimeSpan.FromSeconds(5));
        }

        private void ReceiveLoop()
        {
            var assembler = new PrivateSessionFrameAssembler();
            var buffer = new byte[4096];
            try
            {
                var stream = client.GetStream();
                while (!closed)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        break;
                    }

                    if (!assembler.Add(buffer, 0, read))
                    {
                        events.Enqueue(new TransportEvent(
                            TransportEventKind.StreamRejected, 0, default, assembler.Rejection));
                        return;
                    }

                    while (assembler.TryDequeue(out var frame))
                    {
                        events.Enqueue(new TransportEvent(
                            TransportEventKind.Frame, 0, frame, SessionRejectionReason.None));
                    }
                }
            }
            catch (Exception exception) when (IsExpectedSocketFault(exception))
            {
            }
            finally
            {
                events.Enqueue(new TransportEvent(
                    TransportEventKind.Disconnected, 0, default, SessionRejectionReason.None));
            }
        }

        private static bool IsExpectedSocketFault(Exception exception) =>
            exception is SocketException ||
            exception is ObjectDisposedException ||
            exception is IOException ||
            exception is InvalidOperationException;
    }
}
