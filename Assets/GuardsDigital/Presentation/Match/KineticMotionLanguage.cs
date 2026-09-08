using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z11 (the TWENTIETH AMENDMENT). Every curve the motion language is allowed to
    /// move anything along, in one place, with no Unity type anywhere near them.
    ///
    /// <para><b>The user's rule is "ease-in-out only ... no linear motion anywhere", and this file is
    /// where that stops being a style note and becomes a checkable property.</b> A curve that lives in
    /// a view as an inline expression cannot be audited; a curve that lives here can be enumerated
    /// (<see cref="KineticEasingCurves.All"/>) and asserted against the identity line. The audit case
    /// does exactly that, so a future edit that reaches for <c>t</c> where it wanted <c>EaseInOut(t)</c>
    /// fails a test rather than shipping a mechanical-looking flight.</para>
    ///
    /// <para><b>Why <see cref="KineticPlayback.SmoothStep"/> is not duplicated here.</b> The shipped
    /// board kinetics (8.35y) already ease every piece motion through a smoothstep, which IS an
    /// ease-in-out - symmetric, zero-derivative at both ends. This file adds the language's standard
    /// cubic beside it rather than replacing it, so no shipped board motion changes and the audit can
    /// hold both to the same non-linearity bar.</para>
    /// </summary>
    public static class KineticEasing
    {
        /// <summary>
        /// The motion language's standard curve: cubic ease-in-out. Slower out of the start and into
        /// the end than <see cref="KineticPlayback.SmoothStep"/>, which is what gives a card flight its
        /// weight.
        /// </summary>
        public static float EaseInOut(float t)
        {
            var clamped = Clamp01(t);
            return clamped < 0.5f
                ? 4f * clamped * clamped * clamped
                : 1f - (Pow3((-2f * clamped) + 2f) / 2f);
        }

        /// <summary>
        /// Cubic ease-out: immediate at the start, settling into the end.
        ///
        /// <para><b>The one documented exception to "ease-in-out only", and it is the user's own
        /// rule.</b> Principle 5 asks that "every hover answers IMMEDIATELY". An ease-in on a hover
        /// spends its first frames barely moving, which is precisely the lag the principle forbids, so
        /// interaction feedback - the hover lift and the press dip milestone 8.35z8 already ships - eases
        /// OUT. Travel and appearance, where the rule was aimed, stay ease-in-out. The audit holds this
        /// curve to the same non-linearity bar as the rest.</para>
        /// </summary>
        public static float EaseOut(float t)
        {
            var clamped = Clamp01(t);
            return 1f - Pow3(1f - clamped);
        }

        /// <summary>
        /// The height factor of an arced flight: 0 at both ends, 1 at the middle.
        ///
        /// <para>It is a SHAPE rather than a timing, which is why it is a parabola and not an easing
        /// curve - a card lifted by an eased height would rise and fall at the same rate it travels,
        /// and the lift would vanish. Progress along the flight is eased; the lift rides on top of the
        /// eased progress.</para>
        /// </summary>
        public static float Arc(float t)
        {
            var clamped = Clamp01(t);
            return 4f * clamped * (1f - clamped);
        }

        /// <summary>Where <see cref="SettlePop"/> begins, as a fraction of a motion's duration.</summary>
        public const float SettlePopStart = 0.82f;

        /// <summary>
        /// A small overshoot at the very end of a placement, as a SCALE MULTIPLIER: exactly 1 until
        /// <see cref="SettlePopStart"/>, up to <c>1 + amplitude</c>, and back to exactly 1 at t=1.
        ///
        /// <para>The amendment's "small settle-pop on placement". It returns to 1 rather than near 1
        /// because a placement that ends 2% large is a piece the projection does not describe; the
        /// whole discipline of this layer is that its end states are identities.</para>
        /// </summary>
        public static float SettlePop(float t, float amplitude)
        {
            var clamped = Clamp01(t);
            if (clamped <= SettlePopStart)
            {
                return 1f;
            }

            var within = (clamped - SettlePopStart) / (1f - SettlePopStart);
            // A half sine over the tail: 0 at the start of the pop, 1 at its crest, 0 again at arrival.
            return 1f + (amplitude * (float)Math.Sin(within * Math.PI));
        }

        /// <summary>
        /// A card flip's horizontal scale: 1 edge-on to the viewer at the start, 0 at the halfway
        /// instant the face changes, 1 again at the end.
        /// </summary>
        public static float FlipScaleX(float t) =>
            (float)Math.Abs(Math.Cos(Clamp01(t) * Math.PI));

        /// <summary>
        /// Whether a flip is showing its destination face yet. The face swaps at the instant the card
        /// is edge-on, which is the only instant at which a swap is invisible.
        /// </summary>
        public static bool FlipHasTurned(float t) => Clamp01(t) >= 0.5f;

        internal static float Clamp01(float value) =>
            float.IsNaN(value) ? 0f : value < 0f ? 0f : value > 1f ? 1f : value;

        private static float Pow3(float value) => value * value * value;
    }

    /// <summary>
    /// Milestone 8.35z11. Every easing curve the motion language declares, by name, so the
    /// "no linear anywhere" rule can be ASSERTED rather than reviewed.
    ///
    /// <para>A registry exists because the alternative - reading the source for the token
    /// <c>linear</c> - tests the spelling of the code rather than the behaviour of the curves. A curve
    /// in this list is checked for what actually matters: that it departs from the straight line
    /// between its endpoints by a real margin, and that it starts and ends where a normalised curve
    /// must.</para>
    /// </summary>
    public static class KineticEasingCurves
    {
        /// <summary>
        /// How far from the identity line a curve must stray, at its worst sampled point, to count as
        /// eased. A straight line scores 0; the language's own cubic scores about 0.19.
        /// </summary>
        public const float MinimumDepartureFromLinear = 0.05f;

        /// <summary>
        /// The normalised 0-to-1 curves: each must start at 0, end at 1, and be visibly not a line.
        /// <see cref="KineticPlayback.SmoothStep"/> is included deliberately - the shipped board
        /// kinetics travel on it, and the audit should cover what ships rather than only what is new.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Func<float, float>> All =
            new ReadOnlyDictionary<string, Func<float, float>>(
                new Dictionary<string, Func<float, float>>(StringComparer.Ordinal)
                {
                    { "EaseInOut", KineticEasing.EaseInOut },
                    { "EaseOut", KineticEasing.EaseOut },
                    { "SmoothStep", KineticPlayback.SmoothStep }
                });

        /// <summary>
        /// The shape functions, which are 0 at both ends by design and so cannot be held to the
        /// endpoint rule above. They are audited for their own property: a real peak in the middle.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Func<float, float>> Shapes =
            new ReadOnlyDictionary<string, Func<float, float>>(
                new Dictionary<string, Func<float, float>>(StringComparer.Ordinal)
                {
                    { "Arc", KineticEasing.Arc },
                    { "Strike", KineticPlayback.Strike },
                    { "PingPong", KineticPlayback.PingPong }
                });
    }

    /// <summary>
    /// Milestone 8.35z11. The TRAVEL AND APPEARANCE band: every duration for something that
    /// moves across the screen or comes into existence.
    ///
    /// <para>The amendment's stated bound is 200-450ms, and <see cref="All"/> exists so that bound is
    /// enforced on every member by one case rather than trusted per constant.</para>
    /// </summary>
    public static class KineticMotionDurations
    {
        /// <summary>The amendment's floor for a travel or appearance motion.</summary>
        public const float ShortestSeconds = 0.20f;

        /// <summary>The amendment's ceiling for a travel or appearance motion.</summary>
        public const float LongestSeconds = 0.45f;

        /// <summary>A committed card arcing from the hand to the facedown slot.</summary>
        public const float CardCommitArcSeconds = 0.38f;

        /// <summary>A commitment turning face up.</summary>
        public const float CardRevealFlipSeconds = 0.34f;

        /// <summary>A played card flying back to its owner's hand at the end of a round.</summary>
        public const float CardRetrieveSeconds = 0.30f;

        /// <summary>A coin travelling to the tray it joins.</summary>
        public const float CoinFlySeconds = 0.26f;

        /// <summary>A level-up popping on the hero's rail.</summary>
        public const float LevelPopSeconds = 0.28f;

        /// <summary>A panel, prompt or control easing into existence. Principle 5's floor of motion.</summary>
        public const float PanelEnterSeconds = 0.22f;

        /// <summary>The same, leaving.</summary>
        public const float PanelExitSeconds = 0.20f;

        public static readonly IReadOnlyDictionary<string, float> All =
            new ReadOnlyDictionary<string, float>(
                new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { nameof(CardCommitArcSeconds), CardCommitArcSeconds },
                    { nameof(CardRevealFlipSeconds), CardRevealFlipSeconds },
                    { nameof(CardRetrieveSeconds), CardRetrieveSeconds },
                    { nameof(CoinFlySeconds), CoinFlySeconds },
                    { nameof(LevelPopSeconds), LevelPopSeconds },
                    { nameof(PanelEnterSeconds), PanelEnterSeconds },
                    { nameof(PanelExitSeconds), PanelExitSeconds }
                });
    }

    /// <summary>
    /// Milestone 8.35z11. The INTERACTION band: a control answering a pointer.
    ///
    /// <para><b>Why this is a separate band and not a violation of the 200-450ms rule.</b> Principle 2's
    /// band governs how long something TRAVELS or takes to APPEAR. Principle 5 governs a different
    /// thing and states its own requirement in the same breath: "every hover answers IMMEDIATELY". A
    /// 200ms hover does not answer immediately - it answers a fifth of a second late, which on a
    /// pointer is the difference between a live control and a laggy one. So interaction feedback lives
    /// under 200ms by rule, and the audit asserts THAT bound on these rather than pretending they
    /// belong to the other band.</para>
    ///
    /// <para>The two values are milestone 8.35z8's shipped button-system constants
    /// (<c>MatchHudTheme.ButtonHoverTransitionMs</c>, <c>ButtonPressTransitionMs</c>) restated in
    /// seconds. They are restated rather than redefined so the audit covers the buttons that already
    /// ship, and the audit asserts the two sources agree.</para>
    /// </summary>
    public static class KineticInteractionDurations
    {
        /// <summary>Below this a transition is a jump rather than a motion.</summary>
        public const float ShortestSeconds = 0.04f;

        /// <summary>At or above this a pointer answer stops feeling immediate.</summary>
        public const float LongestSeconds = 0.20f;

        /// <summary>A control taking or losing a hover.</summary>
        public const float HoverSeconds = MatchHudTheme.ButtonHoverTransitionMs / 1000f;

        /// <summary>
        /// A control dipping under a press.
        ///
        /// <para><b>Milestone 8.35z62 made these two DERIVED rather than restated, which is what the
        /// audit beside them always meant.</b> They were literals - 0.12 and 0.08 - copied from the
        /// 8.35z8 button constants, and milestone 8.35z58 raised the press to the "~100ms" the user
        /// asked for twice without sweeping this file, so the copy said 80 while the buttons did 100
        /// and <c>KineticMotionLanguageTests.TheInteractionBand_IsTheShippedButtonSystemsOwnNumbers</c>
        /// failed on the merged tree exactly as it was written to. That case's own sentence is "if the
        /// two ever disagree, the audit is auditing a copy rather than what the buttons do" - so the
        /// fix is to stop keeping a copy. Reading <see cref="MatchHudTheme"/> const-to-const makes the
        /// two sources one source, and the audit now proves a conversion rather than a coincidence.</para>
        /// </summary>
        public const float PressSeconds = MatchHudTheme.ButtonPressTransitionMs / 1000f;

        public static readonly IReadOnlyDictionary<string, float> All =
            new ReadOnlyDictionary<string, float>(
                new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { nameof(HoverSeconds), HoverSeconds },
                    { nameof(PressSeconds), PressSeconds }
                });
    }

    /// <summary>
    /// Milestone 8.35z11. Principle 5's AMBIENT amplitudes - "1-3% scale ... alive but never
    /// busy".
    ///
    /// <para><b>The discipline IS the tininess</b>, in the user's own words, so the amplitudes get a
    /// declared band and an audit exactly as the durations do. The board marks' 6% breathe
    /// (<see cref="KineticTiming.MarkPulseAmplitude"/>) is deliberately NOT in this family: it is one
    /// mark on a board, sized to be seen across a table-sized playfield, and milestone 8.35y recorded
    /// its own bound for it. This family is the screen-wide floor, where six simultaneous 6% motions
    /// would be the "busy" the principle forbids.</para>
    /// </summary>
    public static class KineticAmbientAmplitudes
    {
        public const float SmallestFraction = 0.01f;
        public const float LargestFraction = 0.03f;

        /// <summary>How far under full size a panel starts before it eases into existence.</summary>
        public const float PanelEnterScaleDrop = 0.02f;

        /// <summary>The settle-pop's overshoot when something lands.</summary>
        public const float SettlePopFraction = 0.03f;

        public static readonly IReadOnlyDictionary<string, float> All =
            new ReadOnlyDictionary<string, float>(
                new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { nameof(PanelEnterScaleDrop), PanelEnterScaleDrop },
                    { nameof(SettlePopFraction), SettlePopFraction }
                });
    }
}
