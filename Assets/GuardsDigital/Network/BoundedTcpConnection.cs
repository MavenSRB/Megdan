using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using GuardsDigital.Application;

namespace GuardsDigital.Network
{
    /// <summary>Local transport resource limits; overflow is a connection loss, never forfeiture.</summary>
    public static class TcpSessionTransportLimits
    {
        public const int PumpEventBudget = 64;
        public const int PendingFramesPerConnection = 64;
        public const int PendingPayloadBytesPerConnection = 512 * 1024;
        public const int PendingSendBytesPerConnection = 1024 * 1024;
        public const int SendTimeoutMilliseconds = 1000;
        public const int GracefulCloseMilliseconds = 200;
        public const int DisposeMilliseconds = 750;
    }

    /// <summary>
    /// Frames have per-connection count AND byte budgets. Control events have at most three
    /// slots per retained connection (Connected, optional rejection, Disconnected). A listener
    /// retains its connection slot until Disconnected is consumed, so churn cannot grow controls.
    /// </summary>
    internal sealed class TcpTransportEventBuffer
    {
        private readonly object gate = new object();
        private readonly Queue<TransportEvent> queue = new Queue<TransportEvent>();
        private readonly Dictionary<int, (int Frames, int Bytes)> pending = new Dictionary<int, (int, int)>();
        private bool stopped;
        private long payloadBytes;
        public long PendingPayloadBytes { get { lock (gate) return payloadBytes; } }

        public bool Frame(int ordinal, SessionFrame frame)
        {
            lock (gate)
            {
                if (stopped) return false;
                pending.TryGetValue(ordinal, out var size);
                if (size.Frames >= TcpSessionTransportLimits.PendingFramesPerConnection ||
                    size.Bytes + frame.Payload.Length > TcpSessionTransportLimits.PendingPayloadBytesPerConnection)
                    return false;
                pending[ordinal] = (size.Frames + 1, size.Bytes + frame.Payload.Length);
                payloadBytes += frame.Payload.Length;
                queue.Enqueue(new TransportEvent(TransportEventKind.Frame, ordinal, frame, SessionRejectionReason.None));
                return true;
            }
        }

        public void Control(TransportEventKind kind, int ordinal, SessionRejectionReason reason = SessionRejectionReason.None)
        {
            lock (gate)
                if (!stopped) queue.Enqueue(new TransportEvent(kind, ordinal, default, reason));
        }

        public bool TryDequeue(out TransportEvent value)
        {
            lock (gate)
            {
                if (queue.Count == 0) { value = default; return false; }
                value = queue.Dequeue();
                if (value.Kind == TransportEventKind.Frame)
                {
                    var size = pending[value.Ordinal];
                    pending[value.Ordinal] = (size.Frames - 1, size.Bytes - value.Frame.Payload.Length);
                    payloadBytes -= value.Frame.Payload.Length;
                }
                else if (value.Kind == TransportEventKind.Disconnected) pending.Remove(value.Ordinal);
                return true;
            }
        }

        public void Stop(int? terminalConnection = null)
        {
            lock (gate)
            {
                stopped = true;
                queue.Clear();
                pending.Clear();
                payloadBytes = 0;
                // A surviving client state machine must observe its disposed transport's loss
                // before it can arm a new connection. Retain one notice, not old payloads.
                if (terminalConnection.HasValue)
                    queue.Enqueue(new TransportEvent(TransportEventKind.Disconnected, terminalConnection.Value,
                        default, SessionRejectionReason.None));
            }
        }
    }

    /// <summary>
    /// One bounded ordered writer and receiver. No socket I/O or thread join holds SendGate.
    /// The pump only enqueues owned bytes; abort can close a socket while Write is blocked.
    /// </summary>
    internal sealed class BoundedTcpConnection
    {
        private readonly TcpTransportEventBuffer events;
        private readonly NetworkStream stream;
        private readonly object sendGate = new object();
        private readonly Queue<byte[]> outgoing = new Queue<byte[]>();
        private int pendingSendBytes;
        private bool finishSending;
        private int closed;
        public int Ordinal { get; }
        public TcpClient Client { get; }
        public Thread ReceiveThread { get; }
        public Thread SendThread { get; }
        public bool DisconnectedConsumed { get; set; }

        public BoundedTcpConnection(int ordinal, TcpClient client, TcpTransportEventBuffer events)
        {
            Ordinal = ordinal;
            Client = client;
            this.events = events;
            client.NoDelay = true;
            client.SendTimeout = TcpSessionTransportLimits.SendTimeoutMilliseconds;
            stream = client.GetStream();
            SendThread = new Thread(WriteLoop) { IsBackground = true, Name = "guards-online-send-" + ordinal };
            ReceiveThread = new Thread(ReadLoop) { IsBackground = true, Name = "guards-online-receive-" + ordinal };
        }

        public void Start()
        {
            events.Control(TransportEventKind.Connected, Ordinal);
            SendThread.Start();
            ReceiveThread.Start();
        }

        public void Send(byte[] frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (frame.Length > PrivateSessionProtocol.FrameHeaderBytes + PrivateSessionProtocol.MaxPayloadBytes)
                throw new ArgumentException("An outbound frame exceeds the protocol bound.", nameof(frame));
            lock (sendGate)
            {
                if (closed != 0 || finishSending) return;
                if (outgoing.Count < TcpSessionTransportLimits.PendingFramesPerConnection &&
                    pendingSendBytes + frame.Length <= TcpSessionTransportLimits.PendingSendBytesPerConnection)
                {
                    outgoing.Enqueue((byte[])frame.Clone());
                    pendingSendBytes += frame.Length; // Includes the writer's in-flight frame.
                    Monitor.PulseAll(sendGate);
                    return;
                }
            }
            Abort();
        }

        public void RequestClose()
        {
            lock (sendGate) { finishSending = true; Monitor.PulseAll(sendGate); }
        }

        public void Abort()
        {
            if (Interlocked.Exchange(ref closed, 1) != 0) return;
            lock (sendGate) { outgoing.Clear(); pendingSendBytes = 0; Monitor.PulseAll(sendGate); }
            try { Client.Close(); }
            catch (Exception error) when (ExpectedFault(error)) { }
        }

        private void WriteLoop()
        {
            try
            {
                while (true)
                {
                    byte[] frame;
                    lock (sendGate)
                    {
                        while (closed == 0 && outgoing.Count == 0 && !finishSending) Monitor.Wait(sendGate);
                        if (closed != 0 || outgoing.Count == 0) return;
                        frame = outgoing.Dequeue();
                    }
                    stream.Write(frame, 0, frame.Length);
                    lock (sendGate) if (closed == 0) pendingSendBytes -= frame.Length;
                }
            }
            catch (Exception error) when (ExpectedFault(error)) { }
            finally { Abort(); }
        }

        private void ReadLoop()
        {
            var assembler = new PrivateSessionFrameAssembler();
            var buffer = new byte[4096];
            try
            {
                while (Volatile.Read(ref closed) == 0)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read == 0) return;
                    if (!assembler.Add(buffer, 0, read))
                    {
                        events.Control(TransportEventKind.StreamRejected, Ordinal, assembler.Rejection);
                        return;
                    }
                    while (assembler.TryDequeue(out var frame))
                        if (!events.Frame(Ordinal, frame)) return;
                }
            }
            catch (Exception error) when (ExpectedFault(error)) { }
            finally
            {
                Abort();
                // Keep the listener slot until its writer has stopped; a flood of short-lived
                // sockets cannot leave detached writer threads behind the admission cap.
                SendThread.Join();
                events.Control(TransportEventKind.Disconnected, Ordinal);
            }
        }

        public static void CloseAll(IReadOnlyList<BoundedTcpConnection> connections, Thread acceptThread = null)
        {
            var timer = Stopwatch.StartNew();
            foreach (var connection in connections) connection.RequestClose();
            foreach (var connection in connections)
                JoinUntil(connection.SendThread, timer, TcpSessionTransportLimits.GracefulCloseMilliseconds);
            foreach (var connection in connections) connection.Abort();
            JoinUntil(acceptThread, timer, TcpSessionTransportLimits.DisposeMilliseconds);
            foreach (var connection in connections)
            {
                JoinUntil(connection.SendThread, timer, TcpSessionTransportLimits.DisposeMilliseconds);
                JoinUntil(connection.ReceiveThread, timer, TcpSessionTransportLimits.DisposeMilliseconds);
            }
            foreach (var connection in connections)
                if (connection.SendThread.IsAlive || connection.ReceiveThread.IsAlive)
                    throw new TimeoutException("Transport workers did not stop within the disposal budget.");
            if (acceptThread != null && acceptThread.IsAlive)
                throw new TimeoutException("Transport listener did not stop within the disposal budget.");
        }

        private static void JoinUntil(Thread thread, Stopwatch timer, int deadline)
        {
            if (thread != null && thread.IsAlive)
                thread.Join(Math.Max(0, deadline - (int)timer.ElapsedMilliseconds));
        }

        public static bool ExpectedFault(Exception error) => error is SocketException ||
            error is ObjectDisposedException || error is IOException || error is InvalidOperationException;
    }
}
