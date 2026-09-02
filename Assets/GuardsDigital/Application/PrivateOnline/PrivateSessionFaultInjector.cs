using System;
using System.Collections.Generic;
using System.Globalization;

namespace GuardsDigital.Application
{
    /// <summary>What one fault step does to the frame it addresses.</summary>
    public enum SessionFaultKind
    {
        /// <summary>Pass the frame through untouched - the default for unaddressed frames.</summary>
        Deliver = 0,

        /// <summary>The frame is never delivered.</summary>
        Drop = 1,

        /// <summary>The frame is delivered twice, back to back.</summary>
        Duplicate = 2,

        /// <summary>
        /// The frame is held until <c>Argument</c> later frames have passed, then delivered -
        /// which is a bounded delay, and with an argument of one is exactly a reorder of two.
        /// </summary>
        Delay = 3,

        /// <summary>One bit of the byte at <c>Argument</c> is flipped.</summary>
        Corrupt = 4,

        /// <summary>The frame is cut to <c>Argument</c> bytes.</summary>
        Truncate = 5
    }

    /// <summary>One deterministic fault: which frame, what happens to it.</summary>
    public sealed class SessionFaultStep
    {
        public SessionFaultStep(int frameIndex, SessionFaultKind kind, int argument = 0)
        {
            if (frameIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frameIndex), frameIndex, "A fault addresses a frame by its index from 0.");
            }

            if (!Enum.IsDefined(typeof(SessionFaultKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (kind == SessionFaultKind.Delay && argument < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(argument), argument, "A delay holds past at least one later frame.");
            }

            if ((kind == SessionFaultKind.Corrupt || kind == SessionFaultKind.Truncate) && argument < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(argument));
            }

            FrameIndex = frameIndex;
            Kind = kind;
            Argument = argument;
        }

        public int FrameIndex { get; }
        public SessionFaultKind Kind { get; }
        public int Argument { get; }
    }

    /// <summary>
    /// A named, fully written-out fault plan. The plan is the seed: two runs of the same plan over
    /// the same frames do exactly the same thing, which is what makes a fault scenario repeatable
    /// evidence rather than an anecdote.
    /// </summary>
    public sealed class PrivateSessionFaultPlan
    {
        public PrivateSessionFaultPlan(string name, IEnumerable<SessionFaultStep> steps)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A fault plan is named, so evidence can cite it.", nameof(name));
            }

            Name = name;
            var list = new List<SessionFaultStep>(steps ?? throw new ArgumentNullException(nameof(steps)));
            var addressed = new HashSet<int>();
            foreach (var step in list)
            {
                if (step == null || !addressed.Add(step.FrameIndex))
                {
                    throw new ArgumentException(
                        "A plan addresses each frame at most once, with no null steps.", nameof(steps));
                }
            }

            Steps = list.AsReadOnly();
        }

        public string Name { get; }
        public IReadOnlyList<SessionFaultStep> Steps { get; }
    }

    /// <summary>
    /// Milestone 8.33. The deterministic local fault injector: it transforms one direction's
    /// ordered stream of already-encoded frames, above whatever transport carries them and below
    /// the protocol codec that reads them - so the same plan replays identically over the
    /// in-memory pump today and the socket adapter at the 8.35c rehearsal.
    /// </summary>
    /// <remarks>
    /// It holds bytes and counts, never a socket, a clock, or a protocol type, and it records what
    /// it actually did: <see cref="AppliedActions"/> is the evidence line a fault case cites
    /// beside its plan.
    /// </remarks>
    public sealed class PrivateSessionFaultInjector
    {
        private sealed class HeldFrame
        {
            public HeldFrame(byte[] frame, int remaining)
            {
                Frame = frame;
                Remaining = remaining;
            }

            public byte[] Frame { get; }
            public int Remaining { get; set; }
        }

        private readonly PrivateSessionFaultPlan plan;
        private readonly List<HeldFrame> held = new List<HeldFrame>();
        private readonly List<string> applied = new List<string>();
        private int frameIndex;

        public PrivateSessionFaultInjector(PrivateSessionFaultPlan plan)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        public string PlanName => plan.Name;

        /// <summary>What the injector actually did, one line per addressed frame, in order.</summary>
        public IReadOnlyList<string> AppliedActions => applied;

        public int ProcessedFrameCount => frameIndex;

        /// <summary>
        /// Takes the next frame of the stream and returns what actually goes onto the wire now,
        /// in delivery order - possibly nothing, possibly this frame twice, possibly an earlier
        /// held frame whose delay has elapsed.
        /// </summary>
        public IReadOnlyList<byte[]> Process(byte[] frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            var index = frameIndex++;
            var step = FindStep(index);
            var heldBefore = held.Count;
            var deliveries = new List<byte[]>();

            switch (step?.Kind ?? SessionFaultKind.Deliver)
            {
                case SessionFaultKind.Drop:
                    Note(index, "dropped");
                    break;
                case SessionFaultKind.Duplicate:
                    Note(index, "duplicated");
                    deliveries.Add(frame);
                    deliveries.Add(frame);
                    break;
                case SessionFaultKind.Delay:
                    Note(index, "delayed past " + step.Argument.ToString(CultureInfo.InvariantCulture));
                    held.Add(new HeldFrame(frame, step.Argument));
                    break;
                case SessionFaultKind.Corrupt:
                    var corrupted = (byte[])frame.Clone();
                    corrupted[Math.Min(step.Argument, corrupted.Length - 1)] ^= 0x40;
                    Note(index, "corrupted at byte " +
                        step.Argument.ToString(CultureInfo.InvariantCulture));
                    deliveries.Add(corrupted);
                    break;
                case SessionFaultKind.Truncate:
                    var truncated = new byte[Math.Min(step.Argument, frame.Length)];
                    Array.Copy(frame, truncated, truncated.Length);
                    Note(index, "truncated to " +
                        truncated.Length.ToString(CultureInfo.InvariantCulture) + " bytes");
                    deliveries.Add(truncated);
                    break;
                default:
                    deliveries.Add(frame);
                    break;
            }

            // A held frame's countdown runs only on frames that passed after it - the frame this
            // very call held is not its own later frame - and one whose delay has elapsed is
            // delivered after the current frame, late and out of order, exactly as promised.
            var stillHeld = new List<HeldFrame>(held.Count);
            for (var heldIndex = 0; heldIndex < held.Count; heldIndex++)
            {
                var waiting = held[heldIndex];
                if (heldIndex < heldBefore)
                {
                    waiting.Remaining--;
                    if (waiting.Remaining <= 0)
                    {
                        deliveries.Add(waiting.Frame);
                        continue;
                    }
                }

                stillHeld.Add(waiting);
            }

            held.Clear();
            held.AddRange(stillHeld);
            return deliveries;
        }

        /// <summary>Releases every still-held frame, oldest first - the end of the bounded window.</summary>
        public IReadOnlyList<byte[]> Flush()
        {
            var deliveries = new List<byte[]>(held.Count);
            foreach (var waiting in held)
            {
                deliveries.Add(waiting.Frame);
            }

            held.Clear();
            return deliveries;
        }

        private SessionFaultStep FindStep(int index)
        {
            foreach (var step in plan.Steps)
            {
                if (step.FrameIndex == index)
                {
                    return step;
                }
            }

            return null;
        }

        private void Note(int index, string action) =>
            applied.Add(plan.Name + ": frame " + index.ToString(CultureInfo.InvariantCulture) +
                " " + action);
    }
}
