using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35y. The closed set of kinetic feedback a surface may play - the user's
    /// "tokens smacking each other", typed.
    ///
    /// <para><b>Why a cue is not a rendering instruction.</b> Every member below names something that
    /// HAPPENED, never something to draw: a piece travelled, an attacker struck, a defender was hit, a
    /// piece was taken off the board, a piece came back. A view chooses the treatment. That is what
    /// lets the cue layer be unit-tested with no Unity at all, and what keeps a kinetic from becoming
    /// a second, softer authority on what the board contains.</para>
    ///
    /// <para><b>What is absent is the point.</b> There is no cue for a pending choice, a hover, a
    /// legality hint or anything a player has not yet committed to. Kinetics derive from the
    /// seat-public match log alone (<see cref="GuardsDigital.Core.Diagnostics.MatchLogEntry"/>), so
    /// every client plays the same story from the same rows, and a cue can never reveal something the
    /// log's own selection contract excluded.</para>
    /// </summary>
    public enum KineticCueKind
    {
        None = 0,

        /// <summary>A piece travelled its recorded route instead of teleporting.</summary>
        Slide = 1,

        /// <summary>The attacker leans into the defender and returns.</summary>
        Lunge = 2,

        /// <summary>The struck piece registers the hit.</summary>
        Flash = 3,

        /// <summary>A defeated piece shrinks and fades rather than vanishing.</summary>
        Defeat = 4,

        /// <summary>A returning piece pops in.</summary>
        Respawn = 5,

        // ---- Milestone 8.35z11 (the TWENTIETH AMENDMENT's principle 1: NOTHING TELEPORTS).
        // The five above describe pieces on the board. The five below describe the CARDS AND COINS,
        // which until now appeared and disappeared instantly - a hand that was five and is four, a slot
        // that was empty and holds a card back, a coin count that changed while the player was reading
        // something else. Each names a journey the player could not previously see.

        /// <summary>A committed card arcs from the hand to its facedown slot.</summary>
        CardCommit = 6,

        /// <summary>A facedown commitment turns face up.</summary>
        CardReveal = 7,

        /// <summary>A played card flies back to its owner's hand at the end of a round.</summary>
        CardRetrieve = 8,

        /// <summary>Coins travel to the tray they join.</summary>
        CoinFly = 9,

        /// <summary>A hero's new Level pops on their rail.</summary>
        LevelPop = 10
    }

    /// <summary>
    /// Milestone 8.35z11. Which SURFACE a cue plays on - and it is a real partition, not a
    /// label.
    ///
    /// <para><b>Why the distinction had to be made explicit.</b> Until this milestone every cue moved a
    /// piece on the board, so <see cref="KineticPlayback"/> could take all of them and
    /// <c>ActiveMotionCount</c> meant "pieces in motion". The five journeys the motion pass adds happen
    /// on the HUD - a card leaving a hand, coins landing in a tray - and <see cref="KineticPlayback"/>
    /// has no arm for any of them. Handing it one anyway would leave it reporting a motion it was not
    /// animating, which is the sort of quietly false state a Play Mode case reads and believes. So the
    /// view routes each family to the layer that can actually draw it.</para>
    /// </summary>
    public static class KineticCueFamilies
    {
        /// <summary>A cue that displaces, tints or resizes a piece on the board.</summary>
        public static bool IsBoardPiece(KineticCueKind kind)
        {
            switch (kind)
            {
                case KineticCueKind.Slide:
                case KineticCueKind.Lunge:
                case KineticCueKind.Flash:
                case KineticCueKind.Defeat:
                case KineticCueKind.Respawn:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>A cue that moves a card, a coin or a number on the HUD.</summary>
        public static bool IsHudJourney(KineticCueKind kind)
        {
            switch (kind)
            {
                case KineticCueKind.CardCommit:
                case KineticCueKind.CardReveal:
                case KineticCueKind.CardRetrieve:
                case KineticCueKind.CoinFly:
                case KineticCueKind.LevelPop:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Just the board cues of a scene, for the clock that animates pieces.</summary>
        public static IReadOnlyList<KineticCue> BoardCuesOf(KineticScene scene)
        {
            if (scene == null)
            {
                return Array.Empty<KineticCue>();
            }

            List<KineticCue> board = null;
            for (var index = 0; index < scene.Cues.Count; index++)
            {
                if (!IsBoardPiece(scene.Cues[index].Kind))
                {
                    continue;
                }

                board = board ?? new List<KineticCue>(scene.Cues.Count);
                board.Add(scene.Cues[index]);
            }

            return board == null
                ? Array.Empty<KineticCue>()
                : new ReadOnlyCollection<KineticCue>(board.ToArray());
        }
    }

    /// <summary>Which kind of piece a cue is about. Heroes and minions are the two the board draws.</summary>
    public enum KineticSubjectKind
    {
        None = 0,
        Hero = 1,
        Minion = 2
    }


    /// <summary>
    /// The piece a cue is about, by the same stable identity the board already binds its views to.
    /// Standing-state cues use the projection's current coordinate. A resolved Attack may additionally
    /// carry its historical pair on <see cref="KineticCue"/> so identity reuse after Lane Push cannot
    /// redirect that one delayed lunge; interruption still snaps the real piece to projection truth.
    /// </summary>
    public readonly struct KineticSubject : IEquatable<KineticSubject>
    {
        private KineticSubject(KineticSubjectKind kind, HeroId heroId, MinionId minionId)
        {
            Kind = kind;
            HeroId = heroId;
            MinionId = minionId;
        }

        public static readonly KineticSubject None = default;

        public static KineticSubject ForHero(HeroId heroId) =>
            heroId.IsDefined
                ? new KineticSubject(KineticSubjectKind.Hero, heroId, default)
                : None;

        public static KineticSubject ForMinion(MinionId minionId) =>
            minionId.IsDefined
                ? new KineticSubject(KineticSubjectKind.Minion, default, minionId)
                : None;

        public KineticSubjectKind Kind { get; }
        public HeroId HeroId { get; }
        public MinionId MinionId { get; }

        public bool IsDefined => Kind != KineticSubjectKind.None;

        public bool Equals(KineticSubject other) =>
            Kind == other.Kind &&
            HeroId.Equals(other.HeroId) &&
            MinionId.Equals(other.MinionId);

        public override bool Equals(object obj) => obj is KineticSubject other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = (hash * 397) ^ HeroId.GetHashCode();
                hash = (hash * 397) ^ MinionId.GetHashCode();
                return hash;
            }
        }

        public override string ToString() =>
            Kind == KineticSubjectKind.Hero ? "hero:" + HeroId
            : Kind == KineticSubjectKind.Minion ? "minion:" + MinionId
            : "none";
    }

    /// <summary>
    /// One kinetic to play, derived from exactly one seat-public match-log row.
    ///
    /// <para><see cref="Sequence"/> is that row's own sequence, so a cue carries the log's identity
    /// rather than a counter of its own. That is what makes the stream idempotent across a resync and
    /// truncatable across a rewind, and it is why a client that already played a sequence can refuse
    /// to play it twice.</para>
    /// </summary>
    public sealed class KineticCue
    {
        public KineticCue(
            int sequence,
            KineticCueKind kind,
            KineticSubject subject,
            KineticSubject target = default,
            IReadOnlyList<HexCoordinate> route = null,
            CardInstanceId cardInstanceId = default,
            int amount = 0,
            HexCoordinate? subjectSpace = null,
            HexCoordinate? targetSpace = null)
        {
            if (sequence < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequence),
                    "A kinetic cue keeps the match-log sequence it derives from, which is one-based.");
            }

            if (kind == KineticCueKind.None)
            {
                throw new ArgumentException(
                    "A kinetic cue requires a named kind.", nameof(kind));
            }

            if (!subject.IsDefined)
            {
                throw new ArgumentException(
                    "A kinetic cue is about a piece the board can draw, so its subject is required.",
                    nameof(subject));
            }

            var waypoints = route == null
                ? Array.Empty<HexCoordinate>()
                : System.Linq.Enumerable.ToArray(route);
            if (kind == KineticCueKind.Slide && waypoints.Length < 2)
            {
                throw new ArgumentException(
                    "A slide travels a route, which needs at least the space it left and the space it " +
                    "reached. A move whose log row stated neither is not drawn as a slide at all.",
                    nameof(route));
            }

            if (kind != KineticCueKind.Slide && waypoints.Length != 0)
            {
                throw new ArgumentException(
                    "Only a slide carries a route.", nameof(route));
            }

            if (kind == KineticCueKind.Lunge && !target.IsDefined)
            {
                throw new ArgumentException(
                    "A lunge leans toward someone, so it names the piece it struck.", nameof(target));
            }

            // Milestone 8.35z11. THE PRIVACY RULE, ENFORCED BY THE CONSTRUCTOR rather than left
            // to whoever writes the next arm of the translation. A commitment going facedown is the
            // one journey on screen whose cargo is secret, so the type refuses to carry an identity
            // for it; a reveal is public by definition and is required to name what it turned over.
            if (kind == KineticCueKind.CardCommit && cardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "A commitment travels facedown. Naming the card would put a hand into a cue, " +
                    "which is exactly what the match log's selection contract excludes.",
                    nameof(cardInstanceId));
            }

            if (kind == KineticCueKind.CardReveal && !cardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "A reveal turns a specific card face up, so it names the revealed card.",
                    nameof(cardInstanceId));
            }

            if ((kind == KineticCueKind.CoinFly || kind == KineticCueKind.LevelPop) && amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "A cue about a quantity carries that quantity: coins that arrived, or the Level " +
                    "reached. Nothing travelled is not a cue.");
            }

            Sequence = sequence;
            Kind = kind;
            Subject = subject;
            Target = target;
            Route = new ReadOnlyCollection<HexCoordinate>(waypoints);
            CardInstanceId = cardInstanceId;
            Amount = amount;
            SubjectSpace = subjectSpace;
            TargetSpace = targetSpace;
            DurationSeconds = KineticTiming.DurationFor(kind);
        }

        /// <summary>The match-log sequence this cue derives from. Its identity and its merge key.</summary>
        public int Sequence { get; }

        public KineticCueKind Kind { get; }

        /// <summary>The piece that acts: the one that slides, lunges, is defeated, or returns.</summary>
        public KineticSubject Subject { get; }

        /// <summary>The piece on the receiving end, for a lunge and a flash. Undefined otherwise.</summary>
        public KineticSubject Target { get; }

        /// <summary>
        /// The spaces a slide travels, in order, starting where the piece stood.
        ///
        /// <para>Ordinary Movement carries every committed space from the public match log. Fast
        /// Travel remains an endpoint-only rules fact, but Presentation derives a deterministic
        /// adjacent empty-space journey when one exists and uses the endpoint pair as its honest
        /// fallback when the pathless Fast Travel rule reaches somewhere ordinary movement cannot.</para>
        /// </summary>
        public IReadOnlyList<HexCoordinate> Route { get; }

        /// <summary>Historical Attack spaces; null on legacy rows and non-spatial cues.</summary>
        public HexCoordinate? SubjectSpace { get; }
        public HexCoordinate? TargetSpace { get; }


        /// <summary>
        /// The card this cue is about, where it is about one: the commitment being revealed, the card
        /// flying back to a hand.
        ///
        /// <para><b>It is only ever a card the whole table can see.</b> A reveal names the revealed
        /// form, which is what the log's own <c>CardRevealed</c> row carries and nothing more; a commit
        /// names NOTHING, because the card going facedown is a card no one but its owner may know. That
        /// asymmetry is not an oversight to tidy up later - it is the selection contract, and a commit
        /// cue that carried an identity would put a hand on a wire that has never carried one.</para>
        /// </summary>
        public CardInstanceId CardInstanceId { get; }

        /// <summary>
        /// How many, where a cue is about a quantity: the coins that arrived, the Level reached.
        /// Zero for every cue that is not.
        /// </summary>
        public int Amount { get; }

        public float DurationSeconds { get; }

        public override string ToString() =>
            Sequence.ToString("0000") + " | " + Kind + " | " + Subject +
            (Target.IsDefined ? " -> " + Target : string.Empty);
    }

    /// <summary>
    /// Milestone 8.35y. Every duration the kinetic pass uses, in one place, so the recorded
    /// constraints are checkable rather than scattered through views as literals.
    ///
    /// <para>The SIXTEENTH AMENDMENT's own bounds: piece kinetics are SHORT (~200-400ms), the tray and
    /// card shine sweeps slowly (~2-3s), the board marks breathe gently (~1-1.5s) at a low
    /// amplitude.</para>
    /// </summary>
    public static class KineticTiming
    {
        /// <summary>The amendment's stated floor for a piece kinetic.</summary>
        public const float ShortestPieceKineticSeconds = 0.20f;

        /// <summary>The amendment's stated ceiling for a piece kinetic.</summary>
        public const float LongestPieceKineticSeconds = 0.40f;

        public const float SlideSeconds = 0.32f;
        public const float LungeSeconds = 0.28f;
        public const float FlashSeconds = 0.24f;
        public const float DefeatSeconds = 0.38f;
        public const float RespawnSeconds = 0.30f;


        /// <summary>
        /// One full breath of an awaited tray's own outline -
        /// dark, bright, dark. The user asked for "a breathing light around the whole thing", at the
        /// slow 2-3s the original circling light was specified at and this inherits unchanged.
        /// </summary>
        /// <remarks>
        /// It is the same 2.4 the retired <c>ShineSweepLoopSeconds</c> named, kept deliberately: the
        /// cadence was never what the user objected to - "the little light that goes around" was - so
        /// changing the number as well would have made a rejected treatment and an accepted tempo
        /// indistinguishable if this ever needs re-tuning.
        /// </remarks>
        public const float ShineBreatheLoopSeconds = 2.4f;

        /// <summary>
        /// Milestone 8.35z39. How far DOWN an awaited outline dims at the bottom of its
        /// breath, as a multiplier on the resting gold's brightness.
        ///
        /// <para><b>The breath had to travel downward, and an offline measurement is what said so.</b>
        /// The first version of this ran from the resting gold UP toward
        /// <see cref="MatchHudTheme.ShineRim"/> and looked right in source: same hue family, eased
        /// curve, correct cadence. Measured, it moved the outline's luminance by 3.44% - because
        /// <see cref="MatchHudTheme.ShineGlow"/> is ALREADY a near-white at 0.92 luminance, so the gap
        /// to the rim is almost entirely blue channel, which is saturation and not brightness. A 3%
        /// swing on a 1-3px border is not a breathing light; it is nothing, and it would have shipped
        /// looking exactly like the static gold it replaced.</para>
        ///
        /// <para>So the light kindles rather than merely warms: at the bottom of the breath the
        /// outline is a deeper amber at this fraction of the resting gold's brightness, and at the top
        /// it is full light. Multiplying the channels scales brightness while leaving their ratios -
        /// the hue - alone, which is what keeps this "light, not just yellow color" in the direction
        /// the user meant.</para>
        /// </summary>
        public const float ShineBreatheFloor = 0.45f;

        /// <summary>
        /// Milestone 8.35z39. How far the breath actually travels, derived rather than
        /// declared: the outline runs from <see cref="ShineBreatheFloor"/> of its resting brightness
        /// up to all of it, so this IS the amplitude and there is no second number that could
        /// disagree with the first.
        ///
        /// <para>The amendment's bound on this family is "low amplitude ... subtle enough to read as
        /// alive, not alarming", which is about how much the light MOVES; the floor is about how far
        /// down it goes. They are the same fact stated twice, so it is stated once.</para>
        /// </summary>
        public const float ShineBreatheAmplitude = 1f - ShineBreatheFloor;

        /// <summary>The board marks' breathe. The amendment's ~1-1.5s.</summary>
        public const float MarkPulseLoopSeconds = 1.2f;

        /// <summary>
        /// How far the gentle pulse travels, as a fraction. The amendment's "low amplitude ... subtle
        /// enough to read as alive, not alarming".
        /// </summary>
        public const float MarkPulseAmplitude = 0.06f;

        /// <summary>
        /// The most kinetics a surface plays at once.
        ///
        /// <para>The amendment gates this pass on the recorded perf thresholds and asks for "few
        /// animated elements at once". A burst of log rows - a resync delivering a whole window, an
        /// end-of-round cascade - must therefore not queue a train of animations; the newest cues win
        /// and the rest are dropped, which is also the interruption rule stated from the other side.
        /// The end state is projection-true either way, so a dropped cue costs a flourish and never a
        /// fact.</para>
        /// </summary>
        public const int MaxConcurrentCues = 6;

        public static float DurationFor(KineticCueKind kind)
        {
            switch (kind)
            {
                case KineticCueKind.Slide: return SlideSeconds;
                case KineticCueKind.Lunge: return LungeSeconds;
                case KineticCueKind.Flash: return FlashSeconds;
                case KineticCueKind.Defeat: return DefeatSeconds;
                case KineticCueKind.Respawn: return RespawnSeconds;

                // Milestone 8.35z11. The card and coin journeys take their durations from the
                // motion language's own travel band rather than declaring a second set of numbers
                // here, so one audit case covers every duration the language uses.
                case KineticCueKind.CardCommit: return KineticMotionDurations.CardCommitArcSeconds;
                case KineticCueKind.CardReveal: return KineticMotionDurations.CardRevealFlipSeconds;
                case KineticCueKind.CardRetrieve: return KineticMotionDurations.CardRetrieveSeconds;
                case KineticCueKind.CoinFly: return KineticMotionDurations.CoinFlySeconds;
                case KineticCueKind.LevelPop: return KineticMotionDurations.LevelPopSeconds;

                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
