using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using GuardsDigital.Application;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35o. The dedicated host's typed console command: <c>release &lt;seat&gt;</c>,
    /// the human-reachable surface milestone 8.35k's <see cref="PrivateSessionHost.ReleaseSeat"/>
    /// was left without.
    /// </summary>
    /// <remarks>
    /// <para>Runs at the host-object level rather than through a live process's real OS-attached
    /// stdin: <see cref="HostConsoleReader"/> takes an injectable <see cref="TextReader"/> in place
    /// of <see cref="Console.In"/> for exactly this reason, and
    /// <see cref="ConsoleLine_ReadThroughTheBackgroundReader_ReleasesTheNamedSeat"/> drives the
    /// whole background-thread-read-then-dispatch path a live serve loop's <c>Update</c> exercises
    /// every frame. The one piece this cannot prove is a real OS console attached to a real
    /// process's standard input; the wiring into <see cref="PrivateHostDriver"/> and
    /// <see cref="PrivateLobbyDriver"/> that would receive that is compile-proven only, by building
    /// against the checked-in production sources.</para>
    /// </remarks>
    public sealed class HostConsoleCommandsTests
    {
        private static SeatOccupancyState Occupancy(PrivateSessionHost session, string seat) =>
            session.Roster.Single(entry => entry.Seat.Value == seat).Occupancy;

        private static List<string> CaptureLogs(Action action)
        {
            var messages = new List<string>();
            void Handler(string message, string stackTrace, LogType type) => messages.Add(message);
            UnityEngine.Application.logMessageReceived += Handler;
            try
            {
                action();
            }
            finally
            {
                UnityEngine.Application.logMessageReceived -= Handler;
            }

            return messages;
        }

        private static string WaitForLine(HostConsoleReader console, int timeoutMilliseconds = 2000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
            while (DateTime.UtcNow < deadline)
            {
                if (console.TryDequeue(out var line))
                {
                    return line;
                }

                Thread.Sleep(5);
            }

            return null;
        }

        /// <summary>A stand-in for a live console: one line, released only when the test says so.</summary>
        private sealed class GatedLineReader : TextReader
        {
            private readonly ManualResetEventSlim gate = new ManualResetEventSlim(false);
            private readonly string line;
            private bool served;

            public GatedLineReader(string line)
            {
                this.line = line;
            }

            public void Release() => gate.Set();

            public override string ReadLine()
            {
                gate.Wait();
                if (served)
                {
                    return null;
                }

                served = true;
                return line;
            }
        }

        [Test]
        public void ReleaseCommand_ReleasesAReservedSeat_AndTheDrainedEventNamesTheManualReason()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            var awayOrdinal = harness.OrdinalOf("red-player-02");
            var awaySeat = harness.SeatOf(awayOrdinal);
            harness.DisconnectClient(awayOrdinal);
            Assert.That(Occupancy(harness.Session, awaySeat), Is.EqualTo(SeatOccupancyState.Reserved),
                "A dropped connection reserves the seat rather than releasing it - the state this " +
                "command's one legal target must be composed from.");

            var messages = CaptureLogs(() =>
            {
                HostConsoleCommands.Dispatch("release " + awaySeat, harness.Session);
                // Exactly what the serve loop's own Update does every frame after draining the
                // console: read whatever the operation just recorded and log it. The success
                // marker is deliberately this drain's job, not the command's own - see the comment
                // at HostConsoleCommands.ReleaseSeat's tail.
                foreach (var record in harness.Session.DrainEvents())
                {
                    PrivateOnlineMarkers.LogHostEvent(record, harness.Session);
                }
            });

            Assert.That(Occupancy(harness.Session, awaySeat), Is.EqualTo(SeatOccupancyState.Open),
                "A console-released seat is plainly claimable again, exactly as the operation it " +
                "calls.");
            Assert.That(messages,
                Has.Some.EqualTo("GUARDS_ONLINE: SEAT_RELEASED seat=" + awaySeat + " reason=manual"),
                "The one signal that already existed to tell a manual release apart from a LEAVE's " +
                "forfeit - a connection ordinal of zero - must read as reason=manual in the log a " +
                "host operator actually watches.");
        }

        [Test]
        public void ReleaseCommand_RefusesAnOccupiedSeat_WithALoggedReason()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            var messages = CaptureLogs(() =>
                HostConsoleCommands.Dispatch("release red-player-01", harness.Session));

            Assert.That(Occupancy(harness.Session, "red-player-01"), Is.EqualTo(SeatOccupancyState.Claimed),
                "A live player is never displaced by this command - not a kick.");
            Assert.That(messages,
                Has.Some.EqualTo("GUARDS_ONLINE: RELEASE_REFUSED seat=red-player-01 reason=SeatOccupied"));
        }

        [Test]
        public void ReleaseCommand_RefusesAnOpenSeat_WithALoggedReason()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            Assert.That(Occupancy(harness.Session, "red-player-01"), Is.EqualTo(SeatOccupancyState.Open),
                "Nobody has claimed anything yet in a freshly composed session.");

            var messages = CaptureLogs(() =>
                HostConsoleCommands.Dispatch("release red-player-01", harness.Session));

            Assert.That(Occupancy(harness.Session, "red-player-01"), Is.EqualTo(SeatOccupancyState.Open));
            Assert.That(messages,
                Has.Some.EqualTo("GUARDS_ONLINE: RELEASE_REFUSED seat=red-player-01 reason=SeatOpen"));
        }

        [Test]
        public void ReleaseCommand_RefusesAnUnknownSeatName_WithALoggedReason()
        {
            var harness = PrivateOnlineWireHarness.Compose();

            var messages = CaptureLogs(() =>
                HostConsoleCommands.Dispatch("release not-a-real-seat", harness.Session));

            Assert.That(messages,
                Has.Some.EqualTo("GUARDS_ONLINE: RELEASE_REFUSED seat=not-a-real-seat reason=UnknownSeat"));
        }

        [TestCase("release")]
        [TestCase("release   ")]
        public void ReleaseCommand_RefusesAMissingSeatArgument_WithALoggedReason(string line)
        {
            var harness = PrivateOnlineWireHarness.Compose();

            var messages = CaptureLogs(() => HostConsoleCommands.Dispatch(line, harness.Session));

            Assert.That(messages,
                Has.Some.EqualTo("GUARDS_ONLINE: RELEASE_REFUSED reason=NoSeatNamed"));
        }

        [Test]
        public void UnrecognizedCommand_IsLoggedRatherThanSilentlyDropped()
        {
            var harness = PrivateOnlineWireHarness.Compose();

            var messages = CaptureLogs(() => HostConsoleCommands.Dispatch("kick red-player-01", harness.Session));

            Assert.That(messages,
                Has.Some.EqualTo("GUARDS_ONLINE: CONSOLE_COMMAND_UNKNOWN input=kick red-player-01"),
                "A typo or an unsupported command at the keyboard must be visible in the log rather " +
                "than silently doing nothing.");
        }

        [Test]
        public void Dispatch_WithNoSession_DoesNothingAndNeverThrows()
        {
            Assert.DoesNotThrow(() => HostConsoleCommands.Dispatch("release red-player-01", null));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Dispatch_WithNoLine_DoesNothingAndNeverThrows(string line)
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var messages = CaptureLogs(() => HostConsoleCommands.Dispatch(line, harness.Session));
            Assert.That(messages, Is.Empty);
        }

        [Test]
        public void HostConsoleReader_StartReturnsImmediately_TheBlockingReadRunsOnItsOwnThread()
        {
            var reader = new GatedLineReader("release red-player-02");
            var console = new HostConsoleReader(reader);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            console.Start();
            stopwatch.Stop();

            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500),
                "Start must return at once; the blocking ReadLine call belongs to a background " +
                "thread rather than to whichever thread calls Start - that is the whole point of " +
                "a serve loop polling this every frame without ever blocking on it.");
            Assert.That(console.TryDequeue(out _), Is.False,
                "Nothing has been typed yet - the gate is still closed - so nothing should be queued.");

            reader.Release();
            Assert.That(WaitForLine(console), Is.EqualTo("release red-player-02"),
                "Once the (simulated) keystroke arrives, the background thread's read unblocks and " +
                "the line reaches the queue a poller can drain.");
        }

        [Test]
        public void HostConsoleReader_QueuesEachLineInOrder()
        {
            var console = new HostConsoleReader(
                new StringReader("release red-player-01\nrelease red-player-02\n"));
            console.Start();

            Assert.That(WaitForLine(console), Is.EqualTo("release red-player-01"));
            Assert.That(WaitForLine(console), Is.EqualTo("release red-player-02"));
        }

        [Test]
        public void HostConsoleReader_WithNoConsoleAttached_ExitsQuietlyWithoutSpinningOrThrowing()
        {
            // An empty reader is this test's stand-in for "no console attached": ReadLine returns
            // null at once, exactly as it does for a process nobody opened a terminal for.
            var console = new HostConsoleReader(new StringReader(string.Empty));
            console.Start();

            Thread.Sleep(200);
            Assert.That(console.TryDequeue(out _), Is.False,
                "No line was ever typed, so nothing should be queued and nothing should have thrown.");
        }

        [Test]
        public void ConsoleLine_ReadThroughTheBackgroundReader_ReleasesTheNamedSeat()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            var awayOrdinal = harness.OrdinalOf("red-player-02");
            var awaySeat = harness.SeatOf(awayOrdinal);
            harness.DisconnectClient(awayOrdinal);

            // The real stdin path a live serve loop drives every frame: a background thread reads
            // the operator's keystrokes into a queue, and Update drains and dispatches whatever is
            // there without ever blocking on the read itself.
            var console = new HostConsoleReader(new StringReader("release " + awaySeat + "\n"));
            console.Start();

            var line = WaitForLine(console);
            Assert.That(line, Is.Not.Null, "The typed line must reach the serve loop.");
            HostConsoleCommands.Dispatch(line, harness.Session);

            Assert.That(Occupancy(harness.Session, awaySeat), Is.EqualTo(SeatOccupancyState.Open));
        }
    }
}
