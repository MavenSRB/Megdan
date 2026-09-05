using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z11. What a flying thing looks like at one instant: everything a view needs
    /// to place it, and nothing about what it is.
    ///
    /// <para>Every field is an identity at <c>progress == 1</c> except <see cref="Position"/>, which is
    /// the arrival point - so a flight that is interrupted, dropped or completed leaves nothing behind
    /// to unwind. That is the same discipline <see cref="KineticPieceMotion"/> follows on the board,
    /// restated for the panel.</para>
    /// </summary>
    public readonly struct KineticFlightSample
    {
        internal KineticFlightSample(
            Vector2 position,
            float rotationDegrees,
            float scale,
            float faceScaleX,
            bool showsDestinationFace)
        {
            Position = position;
            RotationDegrees = rotationDegrees;
            Scale = scale;
            FaceScaleX = faceScaleX;
            ShowsDestinationFace = showsDestinationFace;
        }

        /// <summary>Where the centre of the flying thing is, in panel points.</summary>
        public Vector2 Position { get; }

        /// <summary>The slight lean a card carries while it travels. Zero at both ends.</summary>
        public float RotationDegrees { get; }

        /// <summary>Uniform scale, carrying the settle-pop at the tail. Exactly 1 at arrival.</summary>
        public float Scale { get; }

        /// <summary>
        /// Horizontal scale, for a flip: 1 face-on, 0 edge-on. Always 1 for a flight that does not
        /// turn anything over.
        /// </summary>
        public float FaceScaleX { get; }

        /// <summary>Whether a flip has passed its edge-on instant and should be drawing its new face.</summary>
        public bool ShowsDestinationFace { get; }
    }

    /// <summary>
    /// Milestone 8.35z11. The card-and-coin flight path: eased travel, a slight arc, a slight
    /// rotation, and a settle-pop on landing - the amendment's principle 2, as arithmetic.
    ///
    /// <para><b>It is deliberately free of the camera, the HUD and the board.</b> It takes two points in
    /// one space and a progress, and returns where the thing is. That is what lets the whole motion
    /// language's geometry be tested in a plain .NET process while the piece that genuinely needs Unity
    /// - turning a hex or a panel rectangle into one of those two points - stays a thin, separate
    /// bridge (<see cref="KineticSpaceBridge"/>).</para>
    /// </summary>
    public static class KineticFlightPath
    {
        /// <summary>How high a flight arcs, as a fraction of the distance it covers.</summary>
        public const float ArcHeightFraction = 0.20f;

        /// <summary>The most a flight arcs, in panel points, however far it travels.</summary>
        public const float MaxArcHeightPoints = 150f;

        /// <summary>How far a flying card leans at the middle of its journey.</summary>
        public const float FlightRotationDegrees = 7f;

        /// <summary>
        /// Where a flight is at <paramref name="progress"/>.
        ///
        /// <para><b>The arc rides on the EASED progress rather than the raw one</b>, so the apex sits at
        /// the spatial midpoint of the journey instead of at its temporal midpoint. Lifted by raw
        /// progress, a card eased slowly out of the hand would already be near its apex before it had
        /// gone anywhere, which reads as a hop rather than a throw.</para>
        ///
        /// <para><b>Up is negative Y</b>, because this returns panel points and UI Toolkit's Y grows
        /// downward. A flight computed in world space and handed here would arc into the floor; the
        /// bridge converts before this is called, never after.</para>
        /// </summary>
        public static KineticFlightSample Sample(
            Vector2 from, Vector2 to, float progress, KineticCueKind kind)
        {
            var raw = KineticEasing.Clamp01(progress);

            if (kind == KineticCueKind.CardReveal)
            {
                // A reveal turns over where it lies. No travel, no arc, no lean - a flip that also
                // slid would read as a card being replaced rather than as the same card turning.
                return new KineticFlightSample(
                    to,
                    0f,
                    KineticEasing.SettlePop(raw, KineticAmbientAmplitudes.SettlePopFraction),
                    KineticEasing.FlipScaleX(raw),
                    KineticEasing.FlipHasTurned(raw));
            }

            if (kind == KineticCueKind.LevelPop)
            {
                // A pop happens in place: the rail's new Level swells and settles. It is the one
                // "flight" with no journey, and it is here rather than in a view so the whole family
                // shares one clock and one curve.
                var swell = 1f + (KineticAmbientAmplitudes.SettlePopFraction * 2f *
                    KineticEasing.Arc(raw));
                return new KineticFlightSample(to, 0f, swell, 1f, true);
            }

            var eased = KineticEasing.EaseInOut(raw);
            var travel = Vector2.Lerp(from, to, eased);
            var lift = ArcHeightFor(from, to) * KineticEasing.Arc(eased);
            var position = new Vector2(travel.x, travel.y - lift);

            var lean = kind == KineticCueKind.CoinFly
                ? 0f
                : FlightRotationDegrees * KineticEasing.Arc(eased);

            return new KineticFlightSample(
                position,
                lean,
                KineticEasing.SettlePop(raw, KineticAmbientAmplitudes.SettlePopFraction),
                1f,
                true);
        }

        /// <summary>How high this particular journey arcs.</summary>
        public static float ArcHeightFor(Vector2 from, Vector2 to)
        {
            var distance = (to - from).magnitude;
            var height = distance * ArcHeightFraction;
            return height > MaxArcHeightPoints ? MaxArcHeightPoints : height;
        }
    }
}
