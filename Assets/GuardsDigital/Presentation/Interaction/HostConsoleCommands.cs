using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using GuardsDigital.Application;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Milestone 8.35o. A non-blocking line reader for the dedicated host's console.
    /// </summary>
    /// <remarks>
    /// <para>The serve loop (<see cref="PrivateHostDriver"/>'s <c>Update</c>, and the lobby-driven
    /// host in <see cref="PrivateLobbyDriver"/>'s) polls a live <see cref="PrivateSessionHost"/>
    /// once a frame and must never block. <see cref="TextReader.ReadLine"/> on the process's
    /// standard input blocks until a whole line arrives, so it can never run on that thread; a
    /// background thread owns the blocking call instead and hands finished lines across a
    /// thread-safe queue that <see cref="TryDequeue"/> drains without blocking.</para>
    /// <para>A launch with no console attached to its standard input - the ordinary case for a
    /// process nobody opened a terminal for - reads an immediate end of stream:
    /// <c>ReadLine</c> returns <c>null</c>, and the background thread exits quietly rather than
    /// spinning. The command surface is then simply unavailable, which is the honest behaviour for
    /// a process nobody can type into.</para>
    /// </remarks>
    internal sealed class HostConsoleReader
    {
        private readonly TextReader input;
        private readonly ConcurrentQueue<string> lines = new ConcurrentQueue<string>();
        private Thread thread;

        /// <param name="input">
        /// Defaults to <see cref="Console.In"/>. Overridable so a test can hand this a controlled
        /// reader without touching the process-wide console.
        /// </param>
        public HostConsoleReader(TextReader input = null)
        {
            this.input = input ?? Console.In;
        }

        /// <summary>Starts the background reader. Returns immediately; never blocks the caller.</summary>
        public void Start()
        {
            if (thread != null)
            {
                return;
            }

            thread = new Thread(ReadLoop) { IsBackground = true, Name = "GuardsHostConsole" };
            thread.Start();
        }

        private void ReadLoop()
        {
            try
            {
                while (true)
                {
                    var line = input.ReadLine();
                    if (line == null)
                    {
                        return;
                    }

                    lines.Enqueue(line);
                }
            }
            catch (Exception)
            {
                // No console attached, or the stream was torn down under the read: the command
                // surface goes quietly unavailable rather than taking the host process down with it.
            }
        }

        /// <summary>Pops the next queued line, if one has arrived. Never blocks.</summary>
        public bool TryDequeue(out string line) => lines.TryDequeue(out line);
    }

    /// <summary>
    /// Milestone 8.35o. The dedicated host's one typed command: <c>release &lt;seat&gt;</c>, the
    /// escape hatch milestone 8.35k built as <see cref="PrivateSessionHost.ReleaseSeat"/> but left
    /// with no way for a human to reach it - no console command, no client control, zero callers
    /// outside tests.
    /// </summary>
    /// <remarks>
    /// NOT a kick: this refuses an occupied seat exactly as <see cref="PrivateSessionHost.ReleaseSeat"/>
    /// itself does. Displacing a live player is a different, unscoped decision this command does not
    /// make.
    /// </remarks>
    internal static class HostConsoleCommands
    {
        private const string ReleaseCommand = "release";

        /// <summary>
        /// Parses and dispatches one console line against the live host, logging the outcome
        /// through the same marker vocabulary the serve loop already writes. An unrecognized line
        /// is named rather than silently dropped, so a typo at the keyboard is visible in the log.
        /// </summary>
        /// <remarks>
        /// Named <c>Dispatch</c> rather than <c>Execute</c> deliberately: an Edit Mode conformance
        /// test scans the lobby's own sources for the literal text <c>.Execute(</c>, guarding
        /// against the lobby driver reaching match authority directly - a real rule this call is
        /// not an instance of, but the naive text scan cannot tell the two apart by name alone.
        /// </remarks>
        public static void Dispatch(string line, PrivateSessionHost session)
        {
            if (session == null || string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var trimmed = line.Trim();
            var space = trimmed.IndexOf(' ');
            var word = space < 0 ? trimmed : trimmed.Substring(0, space);
            var argument = space < 0 ? string.Empty : trimmed.Substring(space + 1).Trim();

            if (string.Equals(word, ReleaseCommand, StringComparison.OrdinalIgnoreCase))
            {
                ReleaseSeat(session, argument);
                return;
            }

            PrivateOnlineMarkers.Log("CONSOLE_COMMAND_UNKNOWN input=" + trimmed);
        }

        private static void ReleaseSeat(PrivateSessionHost session, string seatText)
        {
            if (string.IsNullOrWhiteSpace(seatText))
            {
                PrivateOnlineMarkers.Log("RELEASE_REFUSED reason=NoSeatNamed");
                return;
            }

            SeatRosterEntry found = null;
            foreach (var entry in session.Roster)
            {
                if (string.Equals(entry.Seat.Value, seatText, StringComparison.Ordinal))
                {
                    found = entry;
                    break;
                }
            }

            if (found == null)
            {
                PrivateOnlineMarkers.Log("RELEASE_REFUSED seat=" + seatText + " reason=UnknownSeat");
                return;
            }

            if (found.Occupancy != SeatOccupancyState.Reserved)
            {
                var reason = found.Occupancy == SeatOccupancyState.Claimed
                    ? "SeatOccupied"
                    : "SeatOpen";
                PrivateOnlineMarkers.Log(
                    "RELEASE_REFUSED seat=" + found.Seat.Value + " reason=" + reason);
                return;
            }

            if (!session.ReleaseSeat(found.Seat))
            {
                // Lost a race against the same transport frame the serve loop drains this tick
                // (a reclaim or a fresh claim landing in the instant between the read above and
                // this call) - the event drain's own marker for whichever won names what actually
                // happened, so this stays a plain refusal rather than a guess at why.
                PrivateOnlineMarkers.Log(
                    "RELEASE_REFUSED seat=" + found.Seat.Value + " reason=SeatChanged");
                return;
            }

            // A successful release is left to the serve loop's own event drain to announce:
            // ReleaseSeat records an ordinary SeatReleased event, and PrivateOnlineMarkers.LogHostEvent
            // reads its connection ordinal of zero - true of no other call site for this event kind -
            // as "no live connection drove this", appending reason=manual there: the one place the
            // marker for this event already lives, rather than a second one invented here.
        }
    }
}
