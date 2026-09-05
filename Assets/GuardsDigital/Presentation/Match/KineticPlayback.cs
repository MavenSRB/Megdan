using System;
using System.Collections.Generic;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// What a kinetic is currently doing to one piece. Every field is a pure number a view applies to
    /// a piece it has ALREADY placed authoritatively, never a position - which is what guarantees the
    /// amendment's "end states always projection-true": with no kinetic running every field below is
    /// its own identity, so applying them is indistinguishable from not applying them.
    /// </summary>
    public readonly struct KineticPieceMotion
    {
        internal KineticPieceMotion(
            float routeProgress,
            bool hasRouteTravel,
            float lungeLean,
            float scale,
            float opacity,
            float flashStrength,
            bool holdVisible)
        {
            RouteProgress = routeProgress;
            HasRouteTravel = hasRouteTravel;
            LungeLean = lungeLean;
            Scale = scale;
            Opacity = opacity;
            FlashStrength = flashStrength;
            HoldVisible = holdVisible;
        }

        /// <summary>The identity motion: a piece with nothing playing on it.</summary>
        public static readonly KineticPieceMotion Still =
            new KineticPieceMotion(1f, false, 0f, 1f, 1f, 0f, false);

        /// <summary>
        /// How far along its route the piece has travelled, 0 at the space it left and 1 at the space
        /// it reached. A view draws the piece at the route point for this progress and offsets by the
        /// difference from the route's END, because the projection already has the piece there.
        /// </summary>
        public float RouteProgress { get; }

        public bool HasRouteTravel { get; }

        /// <summary>How far the attacker leans toward the piece it struck, 0 to 1 and back to 0.</summary>
        public float LungeLean { get; }

        /// <summary>Uniform scale: 1 at rest, shrinking through a defeat, growing into a respawn.</summary>
        public float Scale { get; }

        /// <summary>1 at rest, fading through a defeat, rising through a respawn.</summary>
        public float Opacity { get; }

        /// <summary>How hard the piece is currently lit by a hit, 0 to 1 and back to 0.</summary>
        public float FlashStrength { get; }

        /// <summary>
        /// Whether the view must keep drawing a piece the projection has already removed, so a defeat
        /// shrinks and fades instead of vanishing. False the instant the defeat finishes, and false
        /// for every other cue, so nothing can hold a removed piece on the board.
        /// </summary>
        public bool HoldVisible { get; }

        public bool IsStill =>
            !HasRouteTravel && LungeLean == 0f && Scale == 1f &&
            Opacity == 1f && FlashStrength == 0f && !HoldVisible;
    }

    /// <summary>One tinted figure floating above a piece.</summary>
    public readonly struct KineticFloatingNumber
    {
        internal KineticFloatingNumber(
            int sequence, KineticSubject anchor, KineticNumber number, float rise, float opacity)
        {
            Sequence = sequence;
            Anchor = anchor;
            Number = number;
            Rise = rise;
            Opacity = opacity;
        }

        /// <summary>The match-log sequence the figure came from; its identity for a view's reuse.</summary>
        public int Sequence { get; }

        /// <summary>The piece the figure floats above.</summary>
        public KineticSubject Anchor { get; }

        public KineticNumber Number { get; }

        /// <summary>How far the figure has drifted upward, 0 to 1.</summary>
        public float Rise { get; }

        public float Opacity { get; }
    }

    /// <summary>
    /// Milestone 8.35y. The kinetic clock: turns cues into per-frame numbers, and nothing
    /// else. No Unity type appears here, so every easing curve, every duration bound and the whole
    /// interruption rule are unit-testable without an Editor.
    ///
    /// <para><b>The interruption rule, in one sentence:</b> <see cref="Begin"/> ends every piece
    /// motion that was running before it, so a kinetic can never outlive the projection change that
    /// followed the one it started on. A view that places pieces authoritatively and then applies
    /// <see cref="MotionFor"/> is therefore correct at every instant, including the instant an undo
    /// lands - which is why the amendment's "a rewind/undo snaps cleanly to the authoritative state"
    /// needs no undo-specific code path at all.</para>
    ///
    /// <para><b>Why the standing pulses are phases and not animations.</b> The circling shine and the
    /// board marks' breathe are periodic functions of accumulated time, so they cost one multiply per
    /// animated element per frame and hold no per-element state. Nothing is allocated while they run,
    /// and the element count is bounded by the view model that names them.</para>
    /// </summary>
    public sealed class KineticPlayback
    {
        /// <summary>How far a lunging piece leans, as a fraction of the distance to what it struck.</summary>
        public const float LungeReachFraction = 0.42f;

        /// <summary>What a defeated piece has shrunk to when it finishes leaving.</summary>
        public const float DefeatEndScale = 0.25f;

        /// <summary>What a respawning piece starts at before it pops in.</summary>
        public const float RespawnStartScale = 0.45f;

        private readonly List<Running> running = new List<Running>();
        private readonly List<Running> numbers = new List<Running>();
        private readonly List<KineticFloatingNumber> floatingNumbers = new List<KineticFloatingNumber>();
        private float elapsedTotal;

        private sealed class Running
        {
            public KineticCue Cue;
            public float Elapsed;
            public float Duration;
            public float Progress => Duration <= 0f ? 1f : Clamp01(Elapsed / Duration);
            public bool Finished => Elapsed >= Duration;
        }

        /// <summary>Whether anything at all is playing.</summary>
        public bool IsIdle => running.Count == 0 && numbers.Count == 0;

        /// <summary>The piece motions currently running, for a view deciding whether to do any work.</summary>
        public int ActiveMotionCount => running.Count;

        public IReadOnlyList<KineticFloatingNumber> FloatingNumbers => floatingNumbers;

        /// <summary>
        /// Starts a projection's worth of cues, ending every piece motion already running.
        ///
        /// <para>Floating numbers deliberately survive: a figure above the board displaces no piece and
        /// states no position, so ending it early would cost the one thing it exists for - being read -
        /// without buying any of the correctness that ending a piece motion buys. A rewind still clears
        /// them, through <see cref="CancelAll"/>, because a figure for an event that no longer happened
        /// is a different matter entirely.</para>
        /// </summary>
        public void Begin(IReadOnlyList<KineticCue> cues)
        {
            running.Clear();
            if (cues == null)
            {
                return;
            }

            for (var index = 0; index < cues.Count; index++)
            {
                var cue = cues[index];
                if (cue == null)
                {
                    continue;
                }

                running.Add(new Running
                {
                    Cue = cue,
                    Elapsed = 0f,
                    Duration = cue.DurationSeconds
                });

                if (cue.Number.HasValue)
                {
                    numbers.Add(new Running
                    {
                        Cue = cue,
                        Elapsed = 0f,
                        Duration = KineticTiming.FloatingNumberSeconds
                    });
                }
            }

            RebuildFloatingNumbers();
        }

        /// <summary>
        /// Ends everything, including the floating numbers. The presenter asks for this when it primed
        /// or re-primed its watermark: a first sight of a match, and a log that went backwards.
        /// </summary>
        public void CancelAll()
        {
            running.Clear();
            numbers.Clear();
            floatingNumbers.Clear();
        }

        /// <summary>Advances the clock. Negative and absurd deltas are ignored rather than trusted.</summary>
        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || deltaSeconds <= 0f)
            {
                return;
            }

            // A frame that took a quarter of a second - a scene load, a hitch, an Editor pause - must
            // not teleport a 280 ms lunge past its own midpoint and make the smack invisible.
            var delta = deltaSeconds > 0.25f ? 0.25f : deltaSeconds;
            elapsedTotal += delta;

            for (var index = running.Count - 1; index >= 0; index--)
            {
                running[index].Elapsed += delta;
                if (running[index].Finished)
                {
                    running.RemoveAt(index);
                }
            }

            for (var index = numbers.Count - 1; index >= 0; index--)
            {
                numbers[index].Elapsed += delta;
                if (numbers[index].Finished)
                {
                    numbers.RemoveAt(index);
                }
            }

            RebuildFloatingNumbers();
        }

        /// <summary>
        /// What is currently happening to one piece. <see cref="KineticPieceMotion.Still"/> when
        /// nothing is - which is every piece, most of the time.
        /// </summary>
        public KineticPieceMotion MotionFor(KineticSubject subject)
        {
            if (!subject.IsDefined || running.Count == 0)
            {
                return KineticPieceMotion.Still;
            }

            var routeProgress = 1f;
            var hasRouteTravel = false;
            var lungeLean = 0f;
            var scale = 1f;
            var opacity = 1f;
            var flash = 0f;
            var hold = false;

            for (var index = 0; index < running.Count; index++)
            {
                var active = running[index];
                var cue = active.Cue;
                var progress = active.Progress;

                if (cue.Kind == KineticCueKind.Slide && cue.Subject.Equals(subject))
                {
                    routeProgress = SmoothStep(progress);
                    hasRouteTravel = true;
                    continue;
                }

                if (cue.Kind == KineticCueKind.Lunge && cue.Subject.Equals(subject))
                {
                    lungeLean = Max(lungeLean, Strike(progress));
                    continue;
                }

                if (cue.Kind == KineticCueKind.Flash && cue.Subject.Equals(subject))
                {
                    flash = Max(flash, PingPong(progress));
                    continue;
                }

                if (cue.Kind == KineticCueKind.Defeat && cue.Subject.Equals(subject))
                {
                    var eased = SmoothStep(progress);
                    scale = Lerp(1f, DefeatEndScale, eased);
                    opacity = Lerp(1f, 0f, eased);
                    hold = true;
                    continue;
                }

                if (cue.Kind == KineticCueKind.Respawn && cue.Subject.Equals(subject))
                {
                    var eased = SmoothStep(progress);
                    scale = Lerp(RespawnStartScale, 1f, eased);
                    opacity = eased;
                }
            }

            return new KineticPieceMotion(
                routeProgress, hasRouteTravel, lungeLean, scale, opacity, flash, hold);
        }

        /// <summary>
        /// The piece a running lunge on <paramref name="subject"/> is leaning toward, so a view can
        /// resolve the direction without re-reading the cue list. Undefined when nothing is lunging.
        /// </summary>
        public KineticSubject LungeTargetFor(KineticSubject subject)
        {
            for (var index = 0; index < running.Count; index++)
            {
                var cue = running[index].Cue;
                if (cue.Kind == KineticCueKind.Lunge && cue.Subject.Equals(subject))
                {
                    return cue.Target;
                }
            }

            return KineticSubject.None;
        }

        /// <summary>
        /// The slide route a running slide on <paramref name="subject"/> travels, or null. A view maps
        /// these to world points itself, because hex-to-world is the board's business, not the clock's.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Board.HexCoordinate> RouteFor(KineticSubject subject)
        {
            for (var index = 0; index < running.Count; index++)
            {
                var cue = running[index].Cue;
                if (cue.Kind == KineticCueKind.Slide && cue.Subject.Equals(subject))
                {
                    return cue.Route;
                }
            }

            return null;
        }

        /// <summary>
        /// Where the circling light stands on its loop, 0 to 1. A view sweeps a highlight's border by
        /// this fraction. Free-running rather than started by an event, because the shine it travels is
        /// standing state and a sweep that restarted on every refresh would stutter.
        /// </summary>
        public float ShineSweepPhase =>
            Repeat(elapsedTotal / KineticTiming.ShineSweepLoopSeconds);

        /// <summary>
        /// The board marks' current scale: 1 at the middle of the breathe, and never further from it
        /// than <see cref="KineticTiming.MarkPulseAmplitude"/>.
        /// </summary>
        public float MarkPulseScale =>
            1f + (KineticTiming.MarkPulseAmplitude *
                Sine(Repeat(elapsedTotal / KineticTiming.MarkPulseLoopSeconds)));

        private void RebuildFloatingNumbers()
        {
            floatingNumbers.Clear();
            for (var index = 0; index < numbers.Count; index++)
            {
                var active = numbers[index];
                var progress = active.Progress;
                var cue = active.Cue;

                // Damage floats above the piece that was struck; a block floats above the piece that
                // blocked. Both are "the piece this figure is about", which is the lunge's target and
                // the flash's own subject.
                var anchor = cue.Kind == KineticCueKind.Lunge ? cue.Target : cue.Subject;
                floatingNumbers.Add(new KineticFloatingNumber(
                    cue.Sequence,
                    anchor,
                    cue.Number,
                    SmoothStep(progress),
                    FadeOutTail(progress)));
            }
        }

        // ---- The curves. Small, named, and here rather than in a view, so a test can hold them.

        /// <summary>Eased 0 to 1: slow out of the start, slow into the end.</summary>
        internal static float SmoothStep(float t)
        {
            var clamped = Clamp01(t);
            return clamped * clamped * (3f - (2f * clamped));
        }

        /// <summary>
        /// A strike: out fast, back slower. Peaks at a third of the way through rather than the middle,
        /// because a lunge that returns as slowly as it left reads as a lean rather than a hit.
        /// </summary>
        internal static float Strike(float t)
        {
            var clamped = Clamp01(t);
            const float peak = 0.34f;
            return clamped <= peak
                ? SmoothStep(clamped / peak)
                : 1f - SmoothStep((clamped - peak) / (1f - peak));
        }

        /// <summary>Symmetric 0 to 1 to 0.</summary>
        internal static float PingPong(float t)
        {
            var clamped = Clamp01(t);
            return clamped <= 0.5f
                ? SmoothStep(clamped * 2f)
                : 1f - SmoothStep((clamped - 0.5f) * 2f);
        }

        /// <summary>
        /// Full opacity until the last third, then out. A figure that starts fading immediately is
        /// hardest to read exactly when it first appears.
        /// </summary>
        internal static float FadeOutTail(float t)
        {
            var clamped = Clamp01(t);
            const float holdUntil = 0.65f;
            return clamped <= holdUntil ? 1f : 1f - SmoothStep((clamped - holdUntil) / (1f - holdUntil));
        }

        internal static float Clamp01(float value) =>
            float.IsNaN(value) ? 0f : value < 0f ? 0f : value > 1f ? 1f : value;

        private static float Lerp(float from, float to, float t) => from + ((to - from) * Clamp01(t));

        private static float Max(float a, float b) => a > b ? a : b;

        /// <summary>The fractional part, for a free-running loop phase.</summary>
        internal static float Repeat(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            var wrapped = value - (float)Math.Floor(value);
            return wrapped < 0f ? wrapped + 1f : wrapped;
        }

        /// <summary>A full sine over a 0-to-1 phase, so the breathe is symmetric about 0.</summary>
        internal static float Sine(float phase) => (float)Math.Sin(phase * 2d * Math.PI);
    }
}
