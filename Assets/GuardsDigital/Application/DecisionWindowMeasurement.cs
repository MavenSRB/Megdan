using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    public enum DecisionWindowCloseReason
    {
        StateAdvanced = 1,
        PlayerCommand = 2,
        TimeoutCommand = 3,
        Undo = 4,
        DirectorUndo = 5,
        Restart = 6,
        SnapshotRestore = 7
    }

    /// <summary>
    /// One completed player decision window, measured only while the host says its owner can act.
    /// It is diagnostic output: never rules state, replay ancestry or snapshot content.
    /// </summary>
    public sealed class DecisionWindowMeasurement
    {
        internal DecisionWindowMeasurement(
            int sequence,
            string id,
            MatchClockKind kind,
            string context,
            SeatId seat,
            HeroId hero,
            TeamSide team,
            long activeTicks,
            long openedVersion,
            long closedVersion,
            DecisionWindowCloseReason closeReason,
            DirectorCommandKind? closingCommand)
        {
            Sequence = sequence;
            Id = id ?? string.Empty;
            Kind = kind;
            Context = context ?? string.Empty;
            Seat = seat;
            Hero = hero;
            Team = team;
            ActiveTicks = activeTicks;
            OpenedVersion = openedVersion;
            ClosedVersion = closedVersion;
            CloseReason = closeReason;
            ClosingCommand = closingCommand;
        }

        public int Sequence { get; }
        public string Id { get; }
        public MatchClockKind Kind { get; }
        public string Context { get; }
        public SeatId Seat { get; }
        public HeroId Hero { get; }
        public TeamSide Team { get; }
        public long ActiveTicks { get; }
        public long OpenedVersion { get; }
        public long ClosedVersion { get; }
        public DecisionWindowCloseReason CloseReason { get; }
        public DirectorCommandKind? ClosingCommand { get; }
        public double ActiveSeconds => ActiveTicks / (double)TimeSpan.TicksPerSecond;
    }

    internal sealed class DecisionWindowDescriptor
    {
        public string Id;
        public MatchClockKind Kind;
        public string Context;
        public MatchSeat Seat;
    }

    internal sealed class DecisionWindowClosure
    {
        public DecisionWindowCloseReason Reason;
        public DirectorCommandKind? Command;
    }

    /// <summary>
    /// Passive accounting over the same window identities and readiness decisions as MatchClock.
    /// Unlike MatchClock it is always active, so timer-off playtests still produce tuning evidence.
    /// </summary>
    internal sealed class DecisionWindowTracker
    {
        private sealed class OpenWindow
        {
            public DecisionWindowDescriptor Descriptor;
            public long ActiveTicks;
            public long OpenedVersion;
            public bool Running;
        }

        private readonly Dictionary<string, OpenWindow> open =
            new Dictionary<string, OpenWindow>(StringComparer.Ordinal);
        private readonly List<DecisionWindowMeasurement> completed =
            new List<DecisionWindowMeasurement>();
        private int sequence;

        public IReadOnlyList<DecisionWindowMeasurement> Completed => completed;

        public void Advance(long elapsedTicks)
        {
            if (elapsedTicks < 0) throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
            if (elapsedTicks == 0) return;
            foreach (var window in open.Values.Where(value => value.Running))
            {
                window.ActiveTicks += elapsedTicks;
            }
        }

        public void CloseAll(long version, DecisionWindowClosure closure)
        {
            Reconcile(Array.Empty<DecisionWindowDescriptor>(), new HashSet<string>(), version, closure);
        }

        public void Reconcile(
            IEnumerable<DecisionWindowDescriptor> descriptors,
            ISet<string> running,
            long version,
            DecisionWindowClosure closure = null,
            ISet<string> retainedClockIds = null)
        {
            var desired = (descriptors ?? Enumerable.Empty<DecisionWindowDescriptor>())
                .ToDictionary(value => value.Id, StringComparer.Ordinal);
            foreach (var pair in open.Where(pair => !desired.ContainsKey(pair.Key)).ToArray())
            {
                var separator = pair.Key.LastIndexOf("|seat:", StringComparison.Ordinal);
                var clockId = separator < 0 ? pair.Key : pair.Key.Substring(0, separator);
                if (retainedClockIds != null && retainedClockIds.Contains(clockId))
                    desired.Add(pair.Key, pair.Value.Descriptor);
            }

            foreach (var removed in open.Keys.Where(id => !desired.ContainsKey(id)).ToArray())
            {
                var value = open[removed];
                completed.Add(new DecisionWindowMeasurement(
                    ++sequence,
                    value.Descriptor.Id,
                    value.Descriptor.Kind,
                    value.Descriptor.Context,
                    value.Descriptor.Seat.Id,
                    value.Descriptor.Seat.HeroId,
                    value.Descriptor.Seat.TeamSide,
                    value.ActiveTicks,
                    value.OpenedVersion,
                    version,
                    closure?.Reason ?? DecisionWindowCloseReason.StateAdvanced,
                    closure?.Command));
                open.Remove(removed);
            }

            foreach (var pair in desired)
            {
                if (!open.TryGetValue(pair.Key, out var value))
                {
                    value = new OpenWindow
                    {
                        Descriptor = pair.Value,
                        OpenedVersion = version
                    };
                    open.Add(pair.Key, value);
                }
                else
                {
                    value.Descriptor = pair.Value;
                }
                value.Running = running != null && running.Contains(pair.Key);
            }
        }
    }
}