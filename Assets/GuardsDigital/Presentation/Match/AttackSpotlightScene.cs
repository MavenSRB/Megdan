using System;
using GuardsDigital.Core.Diagnostics;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z12. Where a spotlight is in its own short life.
    /// </summary>
    public enum AttackSpotlightPhase
    {
        /// <summary>Nothing is being shown; the surface is the ordinary board and HUD.</summary>
        None = 0,

        /// <summary>A scene is ready and waiting for the conductor's go-ahead.</summary>
        Requested = 1,

        /// <summary>The HUD is going, the camera is moving in, the smack is playing.</summary>
        Entering = 2,

        /// <summary>The camera has arrived. CONTINUE is armed and the viewer decides when to leave.</summary>
        Held = 3,

        /// <summary>Dismissed. The camera is easing back and the HUD is coming home.</summary>
        Exiting = 4
    }

    /// <summary>
    /// Milestone 8.35z12. One resolved attack, captured at the moment it resolved, and
    /// everything a spotlight needs to show it.
    ///
    /// <para><b>Why the cards are CAPTURED rather than looked up while the scene plays.</b> The
    /// NINETEENTH AMENDMENT's continue button gates the viewer's presentation and not the match, so
    /// a viewer may still be looking at this scene several authoritative commands later - by which
    /// time the attacker's card has left the turn area and a lookup would find a different card, or
    /// none. Both faces were table-public at the instant this scene was built (the attacker's revealed
    /// commitment stands face-up in the turn area; a spent defense card is face-up in a discard pile,
    /// and <c>SeatProjection.IsPublic</c> admits exactly those two locations to every seat), so
    /// capturing them is both the correct reading and a privacy-safe one: nothing is retained that
    /// was not already open information when it was read.</para>
    ///
    /// <para>A scene is a value. It is never mutated, and two clients that derived it from the same
    /// seat-public log row hold the same one.</para>
    /// </summary>
    public sealed class AttackSpotlightScene
    {
        public AttackSpotlightScene(
            int sequence,
            KineticSubject attacker,
            KineticSubject defender,
            MatchLogOutcome outcome,
            string attackerName = null,
            string defenderName = null,
            TrayCardViewModel attackCard = null,
            TrayCardViewModel defenseCard = null,
            bool isRangedAttack = false,
            SpellCardViewModel attackSpell = null)
        {
            if (sequence < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequence),
                    "A spotlight keeps the match-log sequence of the attack it shows, which is one-based.");
            }

            if (!attacker.IsDefined)
            {
                throw new ArgumentException(
                    "A spotlight frames two pieces, so the attacker is required.", nameof(attacker));
            }

            if (!defender.IsDefined)
            {
                throw new ArgumentException(
                    "A spotlight frames two pieces, so the defender is required.", nameof(defender));
            }

            Sequence = sequence;
            Attacker = attacker;
            Defender = defender;
            Outcome = outcome;
            AttackerName = attackerName ?? string.Empty;
            DefenderName = defenderName ?? string.Empty;
            AttackCard = attackSpell == null ? attackCard : null;
            AttackSpell = attackSpell;
            DefenseCard = defenseCard;
            IsRangedAttack = isRangedAttack;
        }

        /// <summary>The Attack row's own sequence. The scene's identity and its merge key.</summary>
        public int Sequence { get; }

        public KineticSubject Attacker { get; }

        public KineticSubject Defender { get; }

        /// <summary>
        /// Whether the attack went through or was defended. It is the one word the spotlight's own
        /// caption says, and it is the log's answer rather than a re-derivation.
        /// </summary>
        public MatchLogOutcome Outcome { get; }

        public string AttackerName { get; }

        public string DefenderName { get; }

        /// <summary>
        /// The attacker's revealed card, or <c>null</c> when the log named none this viewer can see.
        /// A spotlight with no face still frames and still smacks; it simply shows one card, or none.
        /// </summary>
        public TrayCardViewModel AttackCard { get; }

        /// <summary>
        /// Milestone 9.34. The spell the attack was cast from, drawn as its printed spell face rather
        /// than as a hero card, or <c>null</c> when the attacking card is not a spell. When it is set,
        /// <see cref="AttackCard"/> is <c>null</c>: one attack shows one face.
        /// </summary>
        public SpellCardViewModel AttackSpell { get; }

        /// <summary>
        /// The card the defender spent, or <c>null</c> - which is the ordinary answer for an
        /// undefended attack, a declined defense, and every attack on a minion.
        /// </summary>
        public TrayCardViewModel DefenseCard { get; }

        /// <summary>
        /// Whether the card this attack came off prints a RANGED Attack.
        ///
        /// <para><b>A recorded finding about where this fact lives.</b> It is NOT on the match-log
        /// row - <c>MatchLogEntry</c> has no range field, and 8.35r's selection contract names none.
        /// It is on <c>ActionDefinition.AttackRange</c>, reachable from the card the Attack row
        /// already names (row -> <c>CardInstance.DefinitionReference</c> -> <c>CardDefinition</c> ->
        /// <c>ActionDefinitions</c>), so every client derives it from public data with nothing
        /// added to the wire. The one imprecision worth stating: the row names the CARD, not which
        /// of its action definitions resolved, so this answers "this card prints a Ranged Attack"
        /// rather than "this strike was the ranged one". For the cards in the verified corpus those
        /// are the same statement.</para>
        ///
        /// <para><b>It does not drive the treatment.</b> How the smack is drawn keys on the measured
        /// separation instead - see <see cref="RangedSmackProfile.IsRangedSeparation"/> - because a
        /// Ranged Attack that landed on an adjacent target crossed no ground and should not be drawn
        /// as though it did. This is carried for narration, for tests, and for the day something
        /// wants to say the word.</para>
        /// </summary>
        public bool IsRangedAttack { get; }

        public bool WasDefended => Outcome == MatchLogOutcome.AttackSuccessfullyDefended;

        public bool HasAttackCard => AttackCard != null || AttackSpell != null;

        public bool HasDefenseCard => DefenseCard != null;

        public override string ToString() =>
            Sequence.ToString("0000") + " | SPOTLIGHT | " + Attacker + " -> " + Defender +
            " | " + Outcome;
    }

    /// <summary>
    /// Milestone 8.35z12. Every number the spotlight uses, in one place, so the design
    /// direction's own bounds are checkable rather than scattered through a view as literals.
    /// </summary>
    public static class AttackSpotlightTiming
    {
        /// <summary>How long the camera takes to reach the two combatants.</summary>
        public const float EnterSeconds = 0.45f;

        /// <summary>How long it takes to come home once the viewer presses CONTINUE.</summary>
        public const float ExitSeconds = 0.35f;

        /// <summary>
        /// The camera's tilt at full framing, in degrees off straight-down.
        ///
        /// <para>The NINETEENTH AMENDMENT asks for "a modest (~10-20 degree) camera tilt". Sixteen sits
        /// in the middle of that band. It is the number the piece depth in
        /// <see cref="GuardsDigital.Presentation.Board.BoardPieceDepth"/> was chosen against: under an
        /// ORTHOGRAPHIC camera a tilt of t shows exactly <c>depth * sin(t)</c> of a piece's side, so the
        /// two constants are one decision and are asserted together.</para>
        /// </summary>
        public const float TiltDegrees = 16f;

        /// <summary>The amendment's own floor for the tilt.</summary>
        public const float MinimumTiltDegrees = 10f;

        /// <summary>The amendment's own ceiling for the tilt.</summary>
        public const float MaximumTiltDegrees = 20f;

        /// <summary>
        /// How close the camera may get, as an orthographic half-height in board units. A hex's
        /// outer radius is 1, so this frames roughly five hexes of height - two pieces, the ground
        /// between them, and enough around the edge that neither sits against the frame.
        /// </summary>
        public const float MinimumOrthographicSize = 2.4f;

        /// <summary>
        /// The distance between two neighbouring hex centres, in board units, at the default outer
        /// radius of 1. It is <c>sqrt(3)</c> by <see cref="Board.HexWorldLayout.ToWorld"/>'s own
        /// arithmetic, and it is stated here so every separation constant below can be read in
        /// HEXES, which is the unit the rulebook and the cards use.
        /// </summary>
        public const float HexStepWorld = 1.7320508f;

        /// <summary>
        /// The widest separation the spotlight promises to frame WHOLE, in board units.
        ///
        /// <para><b>Measured, and measured against the right thing.</b> The obvious bound is the
        /// board - the official single-lane map's playable spaces reach 32.909 units apart (extent
        /// 27.71 x 27.00, computed from <c>Assets/Resources/Maps/official-single-lane.json</c>). That
        /// is the wrong bound: the spotlight frames an ATTACK, and an attack's separation is bounded
        /// by the attacker's printed RANGE, not by how far apart two pieces could stand. The largest
        /// printed Range in the six-hero verified corpus is <b>4</b>, and Range can be raised
        /// dynamically by card text and items, so this is EIGHT hexes - the printed maximum with its
        /// dynamic headroom doubled.</para>
        ///
        /// <para>Beyond it the shot CROPS rather than widening further. A pair this far apart is not
        /// reachable by any attack this build can make, and a "spotlight" that opened out into the
        /// whole board view would be the opposite of the word - which is exactly what the first
        /// version of this framing did, because its only ceiling was the resting board frame.</para>
        /// </summary>
        public const float MaximumFramedSeparation = 8f * HexStepWorld;

        /// <summary>
        /// The furthest the camera may pull back, as an orthographic half-height in board units.
        /// It is <see cref="MaximumFramedSeparation"/> put through the framing solve, so the two
        /// numbers cannot drift apart - <c>AWidePairAtTheFramedMaximumStillFitsTheShot</c> asserts it.
        /// </summary>
        public const float MaximumOrthographicSize = 11.2f;

        /// <summary>Extra room around the two pieces, as a fraction of the span they need.</summary>
        public const float FramingMargin = 0.35f;

        /// <summary>
        /// Below this separation the tilt is full. One hex apart is the melee case the tilt was
        /// designed for.
        /// </summary>
        public const float TiltTaperStartSeparation = 2f * HexStepWorld;

        /// <summary>
        /// At and beyond this separation the tilt is at the amendment's floor.
        ///
        /// <para><b>Why the wide shot flattens.</b> The tilt earns its place at close range: it shows
        /// <c>depth * sin t</c> of a piece's side, which reads because the piece fills much of the
        /// frame. Wide, it buys nothing - the same 0.044 units of side against eight hexes of framed
        /// board is invisible - and it COSTS framing, because the foreshortening divides the whole
        /// solve by <c>cos t</c>. So the tilt tapers off as the pair separates. It stops at the
        /// amendment's own 10-degree floor rather than going flat, so a spotlight is always visibly a
        /// different camera from the board's.</para>
        /// </summary>
        public const float TiltTaperEndSeparation = MaximumFramedSeparation;

        /// <summary>
        /// The safety valve: the longest a spotlight may hold the viewer's screen without a press.
        ///
        /// <para>The HUD is hidden while a spotlight is up, so a viewer who walks away must not come
        /// back to a client that cannot be played. The authority never waited for them - that is the
        /// amendment's whole point - so releasing on its own costs a flourish and never a decision.
        /// Six seconds is long enough to read two card faces and short enough that nobody loses a
        /// prompt behind it.</para>
        /// </summary>
        public const float MaximumHeldSeconds = 6f;

        /// <summary>
        /// Milestone 8.35z56 (the camera-timing directive, refined same day: "Zoom in, a
        /// small pause, then smack"). The beat held between the camera FINISHING its zoom and the
        /// attack's own cues being allowed to start.
        ///
        /// <para><b>Why a beat and not an instant release.</b> Releasing the deferred cues the
        /// instant <see cref="AttackSpotlightPhase.Held"/> begins landed them on literally the same
        /// tick the camera stopped moving - correct by the letter of "wait for the zoom to complete",
        /// but with nothing to make the two events read as two events, which is
        /// <see cref="KineticStaging.SceneStaggerSeconds"/>'s own justification applied to this seam.
        /// The user's own words name the shape directly: zoom, THEN a pause, THEN the smack.</para>
        ///
        /// <para>Chosen from the user's own 0.2-0.4s band, at its midpoint: long enough to read as a
        /// deliberate beat, short enough that the whole spotlight - already capped by
        /// <see cref="EnterSeconds"/> and <see cref="ExitSeconds"/> - does not start to drag.</para>
        /// </summary>
        public const float PostZoomBeatSeconds = 0.3f;

        /// <summary>
        /// How many scenes may wait behind the one on screen.
        ///
        /// <para>The amendment asks that the cue queue buffer subsequent events until the viewer
        /// dismisses. A viewer who steps away would otherwise return to an unbounded reel, so the
        /// queue keeps the three most recent attacks IN THE ORDER THEY HAPPENED and drops the oldest
        /// on overflow. Dropping the oldest rather than the newest is deliberate: the story a player
        /// most needs is what just happened to them, and the end state is projection-true either way,
        /// so a dropped scene costs a flourish and never a fact.</para>
        /// </summary>
        public const int MaxQueuedScenes = 3;

        /// <summary>Eased, never linear - the TWENTIETH AMENDMENT's principle 2.</summary>
        public static float Ease(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return t * t * (3f - (2f * t));
        }
    }
}
