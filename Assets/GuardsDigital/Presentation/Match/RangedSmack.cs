using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z12 (ranged pass). How a strike that has to CROSS GROUND reads.
    ///
    /// <para><b>The finding this is built on.</b> The obvious worry - "a full-body lunge across N
    /// hexes reads wrong" - is already prevented: <c>KineticCueUnityView.Displacement</c> caps a
    /// lunge at <c>min(distance * KineticPlayback.LungeReachFraction, MaxLungeWorldDistance)</c>,
    /// which is 0.60 board units, about a third of a hex. A cross-board attacker therefore already
    /// leans rather than charging. <see cref="JabReach"/> restates that cap so it is asserted rather
    /// than assumed, and tightens it further for a genuinely ranged strike: a bow is drawn, not
    /// thrown, so the jab gets SHORTER as the target gets further away rather than staying at the
    /// melee lean.</para>
    ///
    /// <para><b>What was actually missing is the middle.</b> Nothing travelled between the two pieces,
    /// so a ranged attack was a shrug at one end and a flash at the other with an unexplained gap
    /// between them. The tracer is that gap, and its arrival is the impact the flash belongs on.</para>
    ///
    /// <para>Every member here is arithmetic on a normalized clock, so the whole shape is proven with
    /// no Unity runtime; only the drawing of it needs one.</para>
    /// </summary>
    public static class RangedSmackProfile
    {
        /// <summary>
        /// Beyond this separation a strike is drawn as ranged. It is one hex plus a little: two
        /// pieces on neighbouring spaces stand exactly <see cref="AttackSpotlightTiming.HexStepWorld"/>
        /// apart, and everything further had to cross ground to land.
        ///
        /// <para>The treatment keys on the SEPARATION and not on the card's rules category, and that
        /// is deliberate. A Ranged Attack resolved against an adjacent target crossed nothing and
        /// should read as a hit, not as a shot; a card's <c>AttackRangeKind</c> would say otherwise.
        /// The rules fact is carried alongside on <see cref="AttackSpotlightScene.IsRangedAttack"/>
        /// for anything that wants to name it, but what the eye needs is the distance.</para>
        /// </summary>
        public const float RangedSeparationThreshold = AttackSpotlightTiming.HexStepWorld * 1.35f;

        /// <summary>The melee lean, restating <c>KineticCueUnityView.MaxLungeWorldDistance</c>.</summary>
        public const float MaxJabReach = 0.60f;

        /// <summary>
        /// The shortest a jab may become however far the target is. A strike that displaced the
        /// attacker by nothing at all would read as the attacker doing nothing.
        /// </summary>
        public const float MinJabReach = 0.18f;

        /// <summary>How fast a tracer crosses the ground, in board units per second.</summary>
        public const float TracerSpeed = 26f;

        /// <summary>
        /// The floor and ceiling on a tracer's flight. The floor keeps a two-hex shot from being a
        /// single frame; the ceiling keeps the longest legal shot inside the kinetic pass's own
        /// "short" promise, so the spotlight is never waiting on a projectile.
        /// </summary>
        public const float MinTracerSeconds = 0.10f;

        /// <summary>See <see cref="MinTracerSeconds"/>.</summary>
        public const float MaxTracerSeconds = 0.34f;

        /// <summary>How long the impact mark stays readable after the tracer lands.</summary>
        public const float ImpactSeconds = 0.22f;

        /// <summary>Whether a strike across this much ground is drawn as a ranged one.</summary>
        public static bool IsRangedSeparation(float separationWorld) =>
            separationWorld > RangedSeparationThreshold;

        /// <summary>
        /// How far the attacker leans, in board units.
        ///
        /// <para>At melee separation this is the cap the kinetic pass already applies. Past the ranged
        /// threshold it falls away toward <see cref="MinJabReach"/>: the further the target, the less
        /// a lean toward it means, and the more the tracer is carrying the story.</para>
        /// </summary>
        public static float JabReach(float separationWorld)
        {
            if (!IsRangedSeparation(separationWorld))
            {
                return Mathf.Min(
                    separationWorld * 0.42f,
                    MaxJabReach);
            }

            var beyond = Mathf.InverseLerp(
                RangedSeparationThreshold,
                AttackSpotlightTiming.MaximumFramedSeparation,
                separationWorld);
            return Mathf.Lerp(MaxJabReach, MinJabReach, Mathf.Clamp01(beyond));
        }

        /// <summary>How long a tracer takes to cross <paramref name="separationWorld"/>.</summary>
        public static float TracerSeconds(float separationWorld) =>
            Mathf.Clamp(
                Mathf.Max(0f, separationWorld) / TracerSpeed,
                MinTracerSeconds,
                MaxTracerSeconds);

        /// <summary>The whole ranged smack's length: the flight plus the impact it leaves.</summary>
        public static float TotalSeconds(float separationWorld) =>
            TracerSeconds(separationWorld) + ImpactSeconds;

        /// <summary>
        /// How far along its flight the tracer is at <paramref name="elapsedSeconds"/>, from 0 at the
        /// attacker to 1 at the defender. Eased OUT rather than in: a projectile leaves fast and is
        /// already travelling, so the acceleration an ease-in would draw is the one thing it does not
        /// do.
        /// </summary>
        public static float TracerProgress(float elapsedSeconds, float separationWorld)
        {
            var flight = TracerSeconds(separationWorld);
            if (flight <= 0f)
            {
                return 1f;
            }

            var linear = Mathf.Clamp01(elapsedSeconds / flight);
            return 1f - ((1f - linear) * (1f - linear));
        }

        /// <summary>Whether the tracer has arrived - the moment the flash belongs on.</summary>
        public static bool HasImpacted(float elapsedSeconds, float separationWorld) =>
            elapsedSeconds >= TracerSeconds(separationWorld);

        /// <summary>
        /// How strongly the impact mark shows, 0 before the tracer lands and fading to 0 again over
        /// <see cref="ImpactSeconds"/>.
        /// </summary>
        public static float ImpactStrength(float elapsedSeconds, float separationWorld)
        {
            var flight = TracerSeconds(separationWorld);
            if (elapsedSeconds < flight || ImpactSeconds <= 0f)
            {
                return 0f;
            }

            var since = (elapsedSeconds - flight) / ImpactSeconds;
            return since >= 1f ? 0f : 1f - AttackSpotlightTiming.Ease(since);
        }

        /// <summary>
        /// How long the tracer should be drawn, as a fraction of the whole flight path. A short dash
        /// rather than a full-length beam: a line joining the two pieces for a third of a second reads
        /// as a wall between them, which is the opposite of something travelling.
        /// </summary>
        public const float TracerLengthFraction = 0.22f;
    }
}
