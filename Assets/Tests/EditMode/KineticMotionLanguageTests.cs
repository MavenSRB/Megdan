using System;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z11. THE MOTION-LANGUAGE AUDIT: the TWENTIETH AMENDMENT's principle 2
    /// ("ease-in-out only, 200-450ms ... no linear motion anywhere") and principle 5's amplitudes,
    /// asserted rather than reviewed.
    ///
    /// <para><b>Why an audit rather than a case per constant.</b> A rule stated once in an amendment
    /// and applied by hand in twenty places is a rule that decays: the twenty-first place is written by
    /// someone reading the twentieth. Every duration, curve and amplitude the language uses is declared
    /// in one enumerable place, so these cases hold the whole set and a new member is covered the
    /// moment it is added rather than when somebody remembers to write its test.</para>
    /// </summary>
    [TestFixture]
    public sealed class KineticMotionLanguageTests
    {
        // ---- "No linear motion anywhere."

        /// <summary>
        /// The rule, stated as the property it actually is. "Linear" cannot be checked by reading the
        /// source for a word - a curve written as <c>t</c> contains no such word and is exactly what
        /// the rule forbids. So every declared curve is SAMPLED and required to depart from the
        /// straight line between its endpoints by a real margin.
        /// </summary>
        [Test]
        public void EveryEasingCurve_IsActuallyEased_AndNotAStraightLine()
        {
            foreach (var curve in KineticEasingCurves.All)
            {
                var worst = 0f;
                for (var step = 0; step <= 100; step++)
                {
                    var t = step / 100f;
                    var departure = Math.Abs(curve.Value(t) - t);
                    if (departure > worst)
                    {
                        worst = departure;
                    }
                }

                Assert.That(
                    worst, Is.GreaterThan(KineticEasingCurves.MinimumDepartureFromLinear),
                    curve.Key + " is indistinguishable from linear motion, which the motion language " +
                    "forbids everywhere.");
            }
        }

        [Test]
        public void EveryEasingCurve_StartsAtZero_EndsAtOne_AndNeverGoesBackwards()
        {
            foreach (var curve in KineticEasingCurves.All)
            {
                Assert.That(curve.Value(0f), Is.EqualTo(0f).Within(0.0001f), curve.Key + " at 0");
                Assert.That(curve.Value(1f), Is.EqualTo(1f).Within(0.0001f), curve.Key + " at 1");

                var previous = -1f;
                for (var step = 0; step <= 100; step++)
                {
                    var value = curve.Value(step / 100f);
                    Assert.That(value, Is.GreaterThanOrEqualTo(previous - 0.0001f),
                        curve.Key + " must not travel backwards partway through.");
                    previous = value;
                }
            }
        }

        [Test]
        public void EveryShape_RestsAtBothEnds_AndPeaksBetweenThem()
        {
            foreach (var shape in KineticEasingCurves.Shapes)
            {
                Assert.That(shape.Value(0f), Is.EqualTo(0f).Within(0.0001f), shape.Key + " at 0");
                Assert.That(shape.Value(1f), Is.EqualTo(0f).Within(0.0001f), shape.Key + " at 1");

                var peak = 0f;
                for (var step = 0; step <= 100; step++)
                {
                    peak = Math.Max(peak, shape.Value(step / 100f));
                }

                Assert.That(peak, Is.GreaterThan(0.9f),
                    shape.Key + " has to actually reach its extent, or it is not a shape at all.");
            }
        }

        /// <summary>
        /// The UI half of the same rule. UI Toolkit transitions do not take a function - they take an
        /// <see cref="EasingMode"/> - so "no linear" there is the claim that the set the HUD is allowed
        /// to draw from does not contain <see cref="EasingMode.Linear"/>.
        /// </summary>
        [Test]
        public void NoUiTransition_MayBeLinear()
        {
            Assert.That(
                HudAmbientLife.AllowedEasingModes.Contains(EasingMode.Linear), Is.False,
                "Linear is the one easing mode the motion language forbids outright.");
            Assert.That(
                HudAmbientLife.AllowedEasingModes.Contains(HudAmbientLife.EnterEasing), Is.True,
                "A panel eases into existence with a mode from the allowed set.");
            Assert.That(HudAmbientLife.AllowedEasingModes.Count, Is.GreaterThan(0));
        }

        // ---- The bands.

        [Test]
        public void EveryTravelAndAppearanceDuration_IsInsideTheAmendmentsBand()
        {
            foreach (var duration in KineticMotionDurations.All)
            {
                Assert.That(
                    duration.Value,
                    Is.InRange(
                        KineticMotionDurations.ShortestSeconds,
                        KineticMotionDurations.LongestSeconds),
                    duration.Key + " is outside the amendment's stated 200-450ms band.");
            }
        }

        /// <summary>
        /// The interaction band is separate and NARROWER, because principle 5 asks a hover to answer
        /// immediately and 200ms is not immediate on a pointer. Recorded as a deliberate second band
        /// rather than as an exception nobody wrote down.
        /// </summary>
        [Test]
        public void EveryInteractionDuration_AnswersImmediately()
        {
            foreach (var duration in KineticInteractionDurations.All)
            {
                Assert.That(
                    duration.Value,
                    Is.InRange(
                        KineticInteractionDurations.ShortestSeconds,
                        KineticInteractionDurations.LongestSeconds),
                    duration.Key + " is not a pointer answer.");
                Assert.That(
                    duration.Value, Is.LessThan(KineticMotionDurations.ShortestSeconds),
                    duration.Key + " must be quicker than anything that TRAVELS, or the two bands " +
                    "are one band and the distinction is a fiction.");
            }
        }

        /// <summary>
        /// The interaction band is the shipped button pass's own numbers, restated. If the two ever
        /// disagree, the audit is auditing a copy rather than what the buttons do.
        /// </summary>
        [Test]
        public void TheInteractionBand_IsTheShippedButtonSystemsOwnNumbers()
        {
            Assert.That(
                KineticInteractionDurations.HoverSeconds * 1000f,
                Is.EqualTo((float)MatchHudTheme.ButtonHoverTransitionMs).Within(0.001f));
            Assert.That(
                KineticInteractionDurations.PressSeconds * 1000f,
                Is.EqualTo((float)MatchHudTheme.ButtonPressTransitionMs).Within(0.001f));
        }

        [Test]
        public void EveryAmbientAmplitude_IsTiny()
        {
            foreach (var amplitude in KineticAmbientAmplitudes.All)
            {
                Assert.That(
                    amplitude.Value,
                    Is.InRange(
                        KineticAmbientAmplitudes.SmallestFraction,
                        KineticAmbientAmplitudes.LargestFraction),
                    amplitude.Key + " is outside principle 5's 1-3%. The discipline IS the tininess: " +
                    "a screen-wide floor of motion at a larger amplitude is the 'busy' it forbids.");
            }
        }

        /// <summary>
        /// The five journeys principle 1 names take their durations from the language rather than
        /// declaring their own, so the band audit above actually covers them.
        /// </summary>
        [Test]
        public void EveryNewCueKind_TakesItsDurationFromTheLanguage()
        {
            Assert.That(
                KineticTiming.DurationFor(KineticCueKind.CardCommit),
                Is.EqualTo(KineticMotionDurations.CardCommitArcSeconds));
            Assert.That(
                KineticTiming.DurationFor(KineticCueKind.CardReveal),
                Is.EqualTo(KineticMotionDurations.CardRevealFlipSeconds));
            Assert.That(
                KineticTiming.DurationFor(KineticCueKind.CardRetrieve),
                Is.EqualTo(KineticMotionDurations.CardRetrieveSeconds));
            Assert.That(
                KineticTiming.DurationFor(KineticCueKind.CoinFly),
                Is.EqualTo(KineticMotionDurations.CoinFlySeconds));
            Assert.That(
                KineticTiming.DurationFor(KineticCueKind.LevelPop),
                Is.EqualTo(KineticMotionDurations.LevelPopSeconds));
        }

        /// <summary>
        /// Every cue belongs to exactly one surface: the board's pieces, or the HUD's cards and coins.
        ///
        /// <para>This is asserted rather than assumed because the routing depends on it. A kind in
        /// NEITHER family would be derived, queued, staged - and then drawn by nobody, silently. A kind
        /// in BOTH would be animated twice, by two layers with different clocks. Adding a cue kind
        /// therefore fails this case until somebody says which surface it plays on.</para>
        /// </summary>
        [Test]
        public void EveryCueKind_BelongsToExactlyOneSurface()
        {
            foreach (KineticCueKind kind in Enum.GetValues(typeof(KineticCueKind)))
            {
                if (kind == KineticCueKind.None)
                {
                    Assert.That(KineticCueFamilies.IsBoardPiece(kind), Is.False);
                    Assert.That(KineticCueFamilies.IsHudJourney(kind), Is.False);
                    continue;
                }

                var board = KineticCueFamilies.IsBoardPiece(kind);
                var hud = KineticCueFamilies.IsHudJourney(kind);

                Assert.That(board || hud, Is.True,
                    kind + " plays on no surface at all, so it would be derived, queued, staged and " +
                    "then drawn by nobody.");
                Assert.That(board && hud, Is.False,
                    kind + " claims both surfaces, so two layers with different clocks would animate " +
                    "it at once.");
            }
        }

        /// <summary>
        /// The partition, applied: a scene mixing the two families hands the piece clock only what it
        /// can animate. Handing it a card's journey would leave <c>ActiveMotionCount</c> reporting a
        /// motion nothing was drawing - which is what a Play Mode case reads to decide the board is
        /// busy.
        /// </summary>
        [Test]
        public void ASceneOfHudJourneys_GivesThePieceClockNothing()
        {
            var hero = new HeroId("hero-a");
            var mixed = new KineticScene(
                1,
                KineticSceneKind.Commitment,
                new[]
                {
                    new KineticCue(1, KineticCueKind.CardCommit, KineticSubject.ForHero(hero)),
                    new KineticCue(1, KineticCueKind.Flash, KineticSubject.ForHero(hero))
                });

            var board = KineticCueFamilies.BoardCuesOf(mixed);

            Assert.That(board.Count, Is.EqualTo(1));
            Assert.That(board[0].Kind, Is.EqualTo(KineticCueKind.Flash));

            var hudOnly = new KineticScene(
                2,
                KineticSceneKind.Economy,
                new[]
                {
                    new KineticCue(
                        2, KineticCueKind.CoinFly, KineticSubject.ForHero(hero), amount: 3)
                });

            Assert.That(KineticCueFamilies.BoardCuesOf(hudOnly).Count, Is.EqualTo(0));
            Assert.That(KineticCueFamilies.BoardCuesOf(null).Count, Is.EqualTo(0));
        }

        /// <summary>Every kind the vocabulary declares has a duration; none may throw.</summary>
        [Test]
        public void EveryCueKind_HasADuration()
        {
            foreach (KineticCueKind kind in Enum.GetValues(typeof(KineticCueKind)))
            {
                if (kind == KineticCueKind.None)
                {
                    continue;
                }

                Assert.That(KineticTiming.DurationFor(kind), Is.GreaterThan(0f), kind.ToString());
            }
        }

        // ---- The curves' own shapes.

        [Test]
        public void ASettlePop_IsFlatUntilTheTail_AndReturnsExactlyToOne()
        {
            const float amplitude = KineticAmbientAmplitudes.SettlePopFraction;

            Assert.That(KineticEasing.SettlePop(0f, amplitude), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                KineticEasing.SettlePop(KineticEasing.SettlePopStart, amplitude),
                Is.EqualTo(1f).Within(0.0001f),
                "Nothing pops until the thing has nearly landed.");
            Assert.That(
                KineticEasing.SettlePop(1f, amplitude), Is.EqualTo(1f).Within(0.0001f),
                "A placement that ends 3% large is a piece the projection does not describe. Every " +
                "end state in this layer is an identity.");

            var crest = KineticEasing.SettlePop(
                KineticEasing.SettlePopStart + ((1f - KineticEasing.SettlePopStart) / 2f), amplitude);
            Assert.That(crest, Is.EqualTo(1f + amplitude).Within(0.001f));
        }

        [Test]
        public void AFlip_IsEdgeOnAtItsMiddle_AndSwapsFacesExactlyThere()
        {
            Assert.That(KineticEasing.FlipScaleX(0f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(KineticEasing.FlipScaleX(0.5f), Is.EqualTo(0f).Within(0.0001f),
                "The face swaps at the one instant the card has no width, which is the only instant " +
                "at which a swap is invisible.");
            Assert.That(KineticEasing.FlipScaleX(1f), Is.EqualTo(1f).Within(0.0001f));

            Assert.That(KineticEasing.FlipHasTurned(0.49f), Is.False);
            Assert.That(KineticEasing.FlipHasTurned(0.5f), Is.True);
        }

        // ---- The flight path.

        [Test]
        public void AFlight_LeavesItsOriginAndLandsExactlyOnItsDestination()
        {
            var from = new Vector2(100f, 900f);
            var to = new Vector2(1800f, 300f);

            var start = KineticFlightPath.Sample(from, to, 0f, KineticCueKind.CardCommit);
            var end = KineticFlightPath.Sample(from, to, 1f, KineticCueKind.CardCommit);

            Assert.That(start.Position.x, Is.EqualTo(from.x).Within(0.001f));
            Assert.That(start.Position.y, Is.EqualTo(from.y).Within(0.001f));
            Assert.That(end.Position.x, Is.EqualTo(to.x).Within(0.001f));
            Assert.That(end.Position.y, Is.EqualTo(to.y).Within(0.001f),
                "A flight that ends anywhere but its destination leaves a card in mid-air.");
            Assert.That(end.Scale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(end.RotationDegrees, Is.EqualTo(0f).Within(0.0001f));
        }

        /// <summary>
        /// The arc rides the EASED progress, so its apex sits at the spatial midpoint. Lifted by raw
        /// progress it would peak while the card was still near the hand, which reads as a hop.
        /// </summary>
        [Test]
        public void AFlightsApex_IsAtTheSpatialMidpointOfTheJourney()
        {
            var from = new Vector2(0f, 500f);
            var to = new Vector2(1000f, 500f);

            var highest = float.MaxValue;
            var highestX = 0f;
            for (var step = 0; step <= 200; step++)
            {
                var sample = KineticFlightPath.Sample(from, to, step / 200f, KineticCueKind.CardCommit);
                if (sample.Position.y < highest)
                {
                    highest = sample.Position.y;
                    highestX = sample.Position.x;
                }
            }

            Assert.That(highestX, Is.EqualTo(500f).Within(20f),
                "The apex belongs halfway ALONG the journey, not halfway through its clock.");
            Assert.That(
                500f - highest,
                Is.EqualTo(KineticFlightPath.ArcHeightFor(from, to)).Within(1f),
                "Up is negative Y: a panel's origin is its top-left corner.");
        }

        [Test]
        public void AFlightsLean_IsZeroAtBothEnds_AndNeverExtreme()
        {
            var from = new Vector2(0f, 0f);
            var to = new Vector2(600f, 400f);

            for (var step = 0; step <= 100; step++)
            {
                var sample = KineticFlightPath.Sample(from, to, step / 100f, KineticCueKind.CardCommit);
                Assert.That(
                    Math.Abs(sample.RotationDegrees),
                    Is.LessThanOrEqualTo(KineticFlightPath.FlightRotationDegrees + 0.001f),
                    "The amendment asks for a SLIGHT rotation on card flight.");
            }

            Assert.That(
                KineticFlightPath.Sample(from, to, 0f, KineticCueKind.CardCommit).RotationDegrees,
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(
                KineticFlightPath.Sample(from, to, 1f, KineticCueKind.CardCommit).RotationDegrees,
                Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void AnArcIsBounded_HoweverFarTheJourney()
        {
            var near = KineticFlightPath.ArcHeightFor(new Vector2(0f, 0f), new Vector2(100f, 0f));
            var far = KineticFlightPath.ArcHeightFor(new Vector2(0f, 0f), new Vector2(40000f, 0f));

            Assert.That(near, Is.EqualTo(20f).Within(0.001f));
            Assert.That(far, Is.EqualTo(KineticFlightPath.MaxArcHeightPoints).Within(0.001f),
                "A card crossing the whole screen must not loop over the top of it.");
        }

        [Test]
        public void ARevealDoesNotTravel_AndACoinDoesNotLean()
        {
            var from = new Vector2(0f, 0f);
            var to = new Vector2(900f, 200f);

            var reveal = KineticFlightPath.Sample(from, to, 0.5f, KineticCueKind.CardReveal);
            Assert.That(reveal.Position, Is.EqualTo(to),
                "A card that slid while it turned would read as a card being replaced.");
            Assert.That(reveal.FaceScaleX, Is.EqualTo(0f).Within(0.0001f));

            var coin = KineticFlightPath.Sample(from, to, 0.5f, KineticCueKind.CoinFly);
            Assert.That(coin.RotationDegrees, Is.EqualTo(0f).Within(0.0001f),
                "A disc has no lean to read.");
            Assert.That(coin.FaceScaleX, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ALevelPop_HappensInPlace_AndEndsAtItsOwnSize()
        {
            var at = new Vector2(400f, 120f);
            var mid = KineticFlightPath.Sample(at, at, 0.5f, KineticCueKind.LevelPop);
            var end = KineticFlightPath.Sample(at, at, 1f, KineticCueKind.LevelPop);

            Assert.That(mid.Position, Is.EqualTo(at));
            Assert.That(mid.Scale, Is.GreaterThan(1f), "A pop has to actually swell.");
            Assert.That(end.Scale, Is.EqualTo(1f).Within(0.0001f),
                "And has to give the element back exactly as it found it.");
        }

        // ---- Ambient life: the parts that can be asked without a live panel.

        /// <summary>
        /// The cheap rejections, which are also the ones that keep the sweep off things it must not
        /// touch: an unnamed element (no identity to recognise across a rebuild), an opted-out one, a
        /// control the button pass already animates, and this pass's own overlay.
        ///
        /// <para>These are checked in the order the predicate applies them, which is not incidental:
        /// every one of them is answered before anything reads <c>worldBound</c>, so the sweep resolves
        /// layout only for the hundred-odd NAMED elements on the HUD rather than for the whole
        /// tree.</para>
        /// </summary>
        [Test]
        public void TheAmbientSweep_LeavesAloneWhatItMustNotTouch()
        {
            Assert.That(HudAmbientLife.Qualifies(null), Is.False);

            Assert.That(
                HudAmbientLife.Qualifies(new VisualElement()), Is.False,
                "An unnamed element cannot be recognised across a rebuild, which is the whole " +
                "identity mechanism this sweep runs on.");

            var opted = new VisualElement { name = "gd-panel" };
            opted.AddToClassList(HudAmbientLife.OptOutClass);
            Assert.That(HudAmbientLife.Qualifies(opted), Is.False);

            Assert.That(
                HudAmbientLife.Qualifies(new Button { name = "gd-end-turn" }), Is.False,
                "Milestone 8.35x's button pass already animates every control; easing one in would " +
                "overwrite its hover with a fade.");

            Assert.That(
                HudAmbientLife.Qualifies(
                    new VisualElement { name = KineticFlightLayer.LayerName }),
                Is.False,
                "The motion layer must not ease itself into existence.");
            Assert.That(
                HudAmbientLife.Qualifies(new VisualElement { name = KineticFlightLayer.GhostName }),
                Is.False);
        }

        [Test]
        public void SweepingNothing_EasesNothing()
        {
            var ambient = new HudAmbientLife();

            Assert.That(ambient.Sweep(null), Is.EqualTo(0));
            Assert.That(ambient.LastEnterCount, Is.EqualTo(0));

            // An element with no panel is not laid out, so it has no position to ease from.
            var detached = new VisualElement { name = "gd-detached" };
            Assert.That(ambient.Sweep(detached), Is.EqualTo(0));
        }

        [Test]
        public void ThePanelEnterThreshold_IsAPanelRatherThanAChip()
        {
            Assert.That(
                HudAmbientLife.MinimumAreaPoints, Is.GreaterThan(2000f),
                "Easing every label and value chip into existence is motion for its own sake.");
            Assert.That(
                KineticMotionDurations.PanelEnterSeconds,
                Is.GreaterThanOrEqualTo(KineticMotionDurations.PanelExitSeconds),
                "Arriving may take at least as long as leaving; a panel that snaps in and drifts out " +
                "reads backwards.");
        }

        [Test]
        public void EveryCurveSurvivesRubbish()
        {
            foreach (var curve in KineticEasingCurves.All.Concat(KineticEasingCurves.Shapes))
            {
                Assert.That(float.IsNaN(curve.Value(float.NaN)), Is.False, curve.Key + " NaN");
                Assert.That(float.IsNaN(curve.Value(-5f)), Is.False, curve.Key + " below range");
                Assert.That(float.IsNaN(curve.Value(9f)), Is.False, curve.Key + " above range");
            }

            var sample = KineticFlightPath.Sample(
                new Vector2(0f, 0f), new Vector2(10f, 10f), float.NaN, KineticCueKind.CardCommit);
            Assert.That(float.IsNaN(sample.Position.x), Is.False);
        }
    }
}
