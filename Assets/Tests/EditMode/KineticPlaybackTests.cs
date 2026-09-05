using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35y. The kinetic clock: the recorded timing bounds, the interruption rule,
    /// and the curves - all of it without an Editor, because none of it needs one.
    /// </summary>
    public sealed class KineticPlaybackTests
    {
        private static readonly KineticSubject Attacker =
            KineticSubject.ForHero(new HeroId("red-hero-01"));
        private static readonly KineticSubject Defender =
            KineticSubject.ForHero(new HeroId("blue-hero-01"));
        private static readonly KineticSubject Minion =
            KineticSubject.ForMinion(new MinionId("blue-minion-03"));

        /// <summary>
        /// Advances the clock the way frames do, in small steps.
        ///
        /// <para>This is not decoration. <see cref="KineticPlayback.Advance"/> deliberately clamps any
        /// single delta to a quarter of a second so a hitched frame cannot skip a 280ms lunge past its
        /// own peak - so a case that hands it one large delta is not testing what it thinks it is.
        /// Two cases here were written that way and failed until this helper existed, which is the
        /// clamp proving itself.</para>
        /// </summary>
        private static void AdvanceBy(KineticPlayback playback, float seconds)
        {
            const float frame = 1f / 60f;
            for (var elapsed = 0f; elapsed < seconds; elapsed += frame)
            {
                playback.Advance(frame);
            }
        }

        private static KineticCue Slide(int sequence = 1) => new KineticCue(
            sequence, KineticCueKind.Slide, Attacker,
            route: new[] { new HexCoordinate(0, 0), new HexCoordinate(3, 0) });

        private static KineticCue Lunge(int sequence = 2) => new KineticCue(
            sequence, KineticCueKind.Lunge, Attacker, Defender);

        /// <summary>
        /// The amendment's own bound on a piece kinetic: short, 200-400ms. Asserted over the whole
        /// vocabulary so a kind added later cannot quietly sit outside it.
        /// </summary>
        [Test]
        public void EveryPieceKinetic_IsShort()
        {
            foreach (KineticCueKind kind in Enum.GetValues(typeof(KineticCueKind)))
            {
                if (kind == KineticCueKind.None)
                {
                    continue;
                }

                var duration = KineticTiming.DurationFor(kind);
                Assert.That(duration, Is.GreaterThanOrEqualTo(KineticTiming.ShortestPieceKineticSeconds),
                    kind + " is shorter than the recorded floor.");
                Assert.That(duration, Is.LessThanOrEqualTo(KineticTiming.LongestPieceKineticSeconds),
                    kind + " is longer than the recorded ceiling of 400ms.");
            }
        }

        /// <summary>The amendment's bounds for the two standing animations, and its low amplitude.</summary>
        [Test]
        public void TheStandingHighlights_BreatheWithinTheirRecordedBounds()
        {
            Assert.That(KineticTiming.ShineSweepLoopSeconds, Is.InRange(2f, 3f),
                "The circling light was asked for at a slow 2-3s loop.");
            Assert.That(KineticTiming.MarkPulseLoopSeconds, Is.InRange(1f, 1.5f),
                "The board marks were asked to breathe at 1-1.5s.");
            Assert.That(KineticTiming.MarkPulseAmplitude, Is.LessThanOrEqualTo(0.10f),
                "Low amplitude: alive, not alarming.");
            Assert.That(KineticTiming.MaxConcurrentCues, Is.LessThanOrEqualTo(8),
                "Few animated elements at once - the recorded perf thresholds gate this pass.");
        }

        [Test]
        public void APieceWithNothingPlaying_IsExactlyStill()
        {
            var playback = new KineticPlayback();
            Assert.That(playback.IsIdle, Is.True);
            Assert.That(playback.MotionFor(Attacker).IsStill, Is.True);

            playback.Begin(new[] { Slide() });
            playback.Advance(0.1f);

            Assert.That(playback.MotionFor(Defender).IsStill, Is.True,
                "A kinetic on one piece must not move another.");
            Assert.That(playback.MotionFor(Minion).IsStill, Is.True);
            Assert.That(playback.MotionFor(KineticSubject.None).IsStill, Is.True);
        }

        [Test]
        public void ASlide_TravelsItsRouteAndThenEnds()
        {
            var playback = new KineticPlayback();
            playback.Begin(new[] { Slide() });

            var start = playback.MotionFor(Attacker);
            Assert.That(start.HasRouteTravel, Is.True);
            Assert.That(start.RouteProgress, Is.EqualTo(0f).Within(0.0001f));

            playback.Advance(KineticTiming.SlideSeconds / 2f);
            var middle = playback.MotionFor(Attacker);
            Assert.That(middle.RouteProgress, Is.GreaterThan(0.2f));
            Assert.That(middle.RouteProgress, Is.LessThan(0.8f));
            Assert.That(playback.RouteFor(Attacker), Is.Not.Null);

            AdvanceBy(playback, KineticTiming.SlideSeconds);
            Assert.That(playback.MotionFor(Attacker).IsStill, Is.True,
                "A finished slide leaves the piece exactly where the projection says it stands.");
            Assert.That(playback.IsIdle, Is.True);
        }

        [Test]
        public void ALunge_LeansOutAndComesBack()
        {
            var playback = new KineticPlayback();
            playback.Begin(new[] { Lunge() });

            Assert.That(playback.LungeTargetFor(Attacker), Is.EqualTo(Defender));

            var peak = 0f;
            for (var step = 0; step < 40; step++)
            {
                playback.Advance(KineticTiming.LungeSeconds / 40f);
                var lean = playback.MotionFor(Attacker).LungeLean;
                if (lean > peak)
                {
                    peak = lean;
                }
            }

            Assert.That(peak, Is.GreaterThan(0.9f), "A lunge reaches its full lean.");
            Assert.That(playback.MotionFor(Attacker).IsStill, Is.True,
                "And returns to where the projection stands it.");
        }

        /// <summary>
        /// The interruption rule, which is what lets the whole pass sit on an authoritative surface:
        /// the next projection's cues end every motion the previous one started.
        /// </summary>
        [Test]
        public void TheNextProjectionsCues_EndEveryMotionBeforeThem()
        {
            var playback = new KineticPlayback();
            playback.Begin(new[] { Slide() });
            playback.Advance(0.05f);
            Assert.That(playback.MotionFor(Attacker).IsStill, Is.False);

            playback.Begin(new[] { new KineticCue(9, KineticCueKind.Respawn, Defender) });

            Assert.That(playback.MotionFor(Attacker).IsStill, Is.True,
                "A half-finished slide does not outlive the projection change after it.");
            Assert.That(playback.MotionFor(Defender).IsStill, Is.False);
        }

        [Test]
        public void ADefeatHoldsThePieceDrawnOnlyWhileItFades()
        {
            var playback = new KineticPlayback();
            playback.Begin(new[] { new KineticCue(5, KineticCueKind.Defeat, Minion) });
            playback.Advance(KineticTiming.DefeatSeconds / 2f);

            var midway = playback.MotionFor(Minion);
            Assert.That(midway.HoldVisible, Is.True, "The fade has to cover the piece leaving the board.");
            Assert.That(midway.Scale, Is.LessThan(1f));
            Assert.That(midway.Opacity, Is.LessThan(1f));

            AdvanceBy(playback, KineticTiming.DefeatSeconds);
            Assert.That(playback.MotionFor(Minion).HoldVisible, Is.False,
                "And it must never hold a removed piece on the board a moment longer than that.");
        }

        [Test]
        public void ARespawnGrowsIn()
        {
            var playback = new KineticPlayback();
            playback.Begin(new[] { new KineticCue(5, KineticCueKind.Respawn, Defender) });

            var start = playback.MotionFor(Defender);
            Assert.That(start.Scale, Is.LessThan(1f));
            Assert.That(start.Opacity, Is.LessThan(1f));

            AdvanceBy(playback, KineticTiming.RespawnSeconds + 0.02f);
            Assert.That(playback.MotionFor(Defender).IsStill, Is.True);
        }

        /// <summary>
        /// A frame that took a quarter of a second - a scene load, an Editor pause - must not skip a
        /// 280ms lunge past its own peak and make the smack invisible.
        /// </summary>
        [Test]
        public void AHitchedFrame_DoesNotSwallowAKinetic()
        {
            var playback = new KineticPlayback();
            playback.Begin(new[] { Lunge() });
            playback.Advance(4f);

            Assert.That(playback.MotionFor(Attacker).IsStill, Is.False,
                "A four-second frame must still leave the lunge something to show.");

            foreach (var ignored in new[] { 0f, -1f, float.NaN })
            {
                var before = playback.MotionFor(Attacker).LungeLean;
                playback.Advance(ignored);
                Assert.That(playback.MotionFor(Attacker).LungeLean, Is.EqualTo(before),
                    "A non-advancing delta advances nothing.");
            }
        }

        [Test]
        public void TheBoardMarksBreatheGently_AndTheShineSweepWraps()
        {
            var playback = new KineticPlayback();
            var lowest = float.MaxValue;
            var highest = float.MinValue;
            var sawEarlyPhase = false;
            var sawLatePhase = false;

            for (var step = 0; step < 400; step++)
            {
                playback.Advance(0.02f);
                var scale = playback.MarkPulseScale;
                lowest = Math.Min(lowest, scale);
                highest = Math.Max(highest, scale);

                var phase = playback.ShineSweepPhase;
                Assert.That(phase, Is.InRange(0f, 1f), "A loop phase is always a fraction of its loop.");
                sawEarlyPhase |= phase < 0.2f;
                sawLatePhase |= phase > 0.8f;
            }

            Assert.That(highest, Is.LessThanOrEqualTo(1f + KineticTiming.MarkPulseAmplitude + 0.0001f));
            Assert.That(lowest, Is.GreaterThanOrEqualTo(1f - KineticTiming.MarkPulseAmplitude - 0.0001f));
            Assert.That(highest - lowest, Is.GreaterThan(0.01f), "It does have to visibly breathe.");
            Assert.That(sawEarlyPhase && sawLatePhase, Is.True, "The sweep travels the whole border.");
        }

        [Test]
        public void TheCurvesAreWellFormed()
        {
            Assert.That(KineticPlayback.SmoothStep(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(KineticPlayback.SmoothStep(1f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(KineticPlayback.SmoothStep(-5f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(KineticPlayback.SmoothStep(5f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(KineticPlayback.SmoothStep(float.NaN), Is.EqualTo(0f).Within(0.0001f));

            Assert.That(KineticPlayback.Strike(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(KineticPlayback.Strike(1f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(KineticPlayback.Strike(0.34f), Is.EqualTo(1f).Within(0.0001f));

            Assert.That(KineticPlayback.PingPong(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(KineticPlayback.PingPong(0.5f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(KineticPlayback.PingPong(1f), Is.EqualTo(0f).Within(0.0001f));

            Assert.That(KineticPlayback.Repeat(2.25f), Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(KineticPlayback.Repeat(-0.25f), Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(KineticPlayback.Repeat(float.NaN), Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
