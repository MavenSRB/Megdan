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

        /// <summary>The attacker leans into the defender and returns. Carries the floating number.</summary>
        Lunge = 2,

        /// <summary>The struck piece registers the hit.</summary>
        Flash = 3,

        /// <summary>A defeated piece shrinks and fades rather than vanishing.</summary>
        Defeat = 4,

        /// <summary>A returning piece pops in.</summary>
        Respawn = 5
    }

    /// <summary>Which kind of piece a cue is about. Heroes and minions are the two the board draws.</summary>
    public enum KineticSubjectKind
    {
        None = 0,
        Hero = 1,
        Minion = 2
    }

    /// <summary>
    /// What a floating number means. The two the SIXTEENTH AMENDMENT names: the damage an attack
    /// dealt, and the value a defense card blocked with.
    /// </summary>
    public enum KineticNumberRole
    {
        None = 0,
        Damage = 1,
        Block = 2
    }

    /// <summary>
    /// The piece a cue is about, by the same stable identity the board already binds its views to.
    /// A cue never carries a coordinate for its subject: where a piece stands is the projection's
    /// answer, asked fresh every frame, which is what makes an interrupted kinetic snap to truth.
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
    /// A floating number and the printed colour it is tinted with.
    ///
    /// <para><b>The colour is a card's own printed colour, never a palette choice.</b> The user's
    /// 2026-09-05 direction: the damage number wears the ATTACKING card's colour and the block number
    /// the DEFENSE card's, "read from the card definitions ... never a presentation-side palette
    /// guess". So this struct carries <see cref="CardColor"/> - the rules-facing characteristic every
    /// <see cref="CardDefinition"/> states through its own <see cref="CardProgressionProfile"/> - and
    /// not a rendering colour. A view turns it into one through <see cref="MatchHudTheme.Face"/>,
    /// which is the same mapping every card face on the HUD already draws itself with, so a number and
    /// the card it came off are the same colour by construction rather than by coincidence.</para>
    ///
    /// <para><b>Why the amount is the card's printed value.</b> The seat-public log's Attack and
    /// Defense rows carry an outcome and the card that resolved, and no amount - <c>MatchLogEntry.
    /// Amount</c> is documented for the Level bought, the coins collected and the Life counters spent,
    /// and the attack rows pass none. The printed value on the named card is therefore the only figure
    /// derivable from the public stream, and it is public on every client for the same reason the
    /// colour is: both are read off the definition catalog every client holds. It is labelled as the
    /// printed value rather than as a computed total precisely so nothing downstream mistakes it for
    /// one.</para>
    /// </summary>
    public readonly struct KineticNumber : IEquatable<KineticNumber>
    {
        public KineticNumber(int printedAmount, KineticNumberRole role, CardColor color)
        {
            if (role == KineticNumberRole.None)
            {
                throw new ArgumentException(
                    "A floating number states what it means. The absence of a number is " +
                    nameof(None) + ", never a number with no role.",
                    nameof(role));
            }

            if (printedAmount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(printedAmount),
                    "A printed attack or defense value is never negative.");
            }

            PrintedAmount = printedAmount;
            Role = role;
            Color = color;
        }

        public static readonly KineticNumber None = default;

        /// <summary>The value printed on the card the log named. Never a computed damage total.</summary>
        public int PrintedAmount { get; }

        public KineticNumberRole Role { get; }

        /// <summary>The printed colour of the card this number came off, or
        /// <see cref="CardColor.None"/> where the log named no card the catalog holds.</summary>
        public CardColor Color { get; }

        public bool HasValue => Role != KineticNumberRole.None;

        public bool Equals(KineticNumber other) =>
            PrintedAmount == other.PrintedAmount && Role == other.Role && Color == other.Color;

        public override bool Equals(object obj) => obj is KineticNumber other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (((int)Role * 397) ^ PrintedAmount) * 397 ^ (int)Color;
            }
        }

        public override string ToString() => HasValue ? Role + ":" + PrintedAmount + ":" + Color : "none";
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
            KineticNumber number = default)
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

            // The two cues that carry a number are the two halves of a resolution the amendment names:
            // the attacker's lunge shows the damage dealt, and the defender's flash shows the value a
            // defense card blocked with. No other cue states a figure, because no other cue has one.
            if (number.HasValue &&
                kind != KineticCueKind.Lunge && kind != KineticCueKind.Flash)
            {
                throw new ArgumentException(
                    "Only a lunge (damage) or a flash (block) carries a floating number.",
                    nameof(number));
            }

            if (number.Role == KineticNumberRole.Damage && kind != KineticCueKind.Lunge)
            {
                throw new ArgumentException(
                    "Damage is what the attacker's lunge states.", nameof(number));
            }

            if (number.Role == KineticNumberRole.Block && kind != KineticCueKind.Flash)
            {
                throw new ArgumentException(
                    "A block is what the defender's flash states.", nameof(number));
            }

            Sequence = sequence;
            Kind = kind;
            Subject = subject;
            Target = target;
            Route = new ReadOnlyCollection<HexCoordinate>(waypoints);
            Number = number;
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
        /// <para><b>Two waypoints is the honest maximum today, and that is a recorded finding rather
        /// than a shortcut.</b> The SIXTEENTH AMENDMENT assumed "the events carry the steps"; they do
        /// not reach a client. <c>MatchLogEntry</c>'s own contract excludes "a per-hex movement step"
        /// by name, and <c>MatchLogProjection</c> folds a whole move into ONE row keeping only the
        /// first origin and the last destination. Carrying the true per-hex route to a seat would be a
        /// change to that selection contract and to the wire schema - Core, Application and Network
        /// territory, not Presentation's. So a slide travels from the space the piece left to the
        /// space it reached, which is a true statement about the move and an enormous improvement on
        /// teleporting. This list is shaped for N waypoints so that a later milestone which does carry
        /// the route drops it in here without a view changing at all.</para>
        /// </summary>
        public IReadOnlyList<HexCoordinate> Route { get; }

        /// <summary>The tinted floating number a lunge carries. <see cref="KineticNumber.None"/> otherwise.</summary>
        public KineticNumber Number { get; }

        public float DurationSeconds { get; }

        public override string ToString() =>
            Sequence.ToString("0000") + " | " + Kind + " | " + Subject +
            (Target.IsDefined ? " -> " + Target : string.Empty) +
            (Number.HasValue ? " | " + Number : string.Empty);
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
        /// How long a floating damage or block number stays up, measured from the moment it APPEARS -
        /// which milestone 8.35x moved to the striking cue's own impact (see
        /// <c>KineticPlayback.ImpactMomentFor</c>), not the cue's start.
        ///
        /// <para><b>A recorded, deliberate departure from the 200-400ms band, with its reason.</b> That
        /// band bounds how long a PIECE is displaced from where the projection says it stands - the
        /// property that keeps kinetics from being mistaken for state. A number floating above the
        /// board displaces nothing and gates nothing; it has to be READ, and a figure that leaves in a
        /// third of a second cannot be. It stays concurrent with input, is interrupted by the next
        /// event exactly as every other kinetic is, and holds no authority, so every constraint the
        /// amendment actually placed on kinetics still holds.</para>
        ///
        /// <para><b>Milestone 8.35x raised this from 0.90s, on the first live internet playtest's
        /// finding that the figures "show up too fast".</b> Two things made them hard to read, and the
        /// duration was only the second. The first was that a figure appeared at the cue's START, ~95ms
        /// BEFORE the lunge it describes had even reached the piece it struck - so the number preceded
        /// its own cause and the eye had nothing to follow. With the appearance moved onto the impact,
        /// this value now buys what it was always meant to: with <c>FadeOutTail</c> holding full opacity
        /// through the first 65%, a figure now sits fully legible for just under a second
        /// (1.5 x 0.65 = 0.975s) before it begins to leave.</para>
        /// </summary>
        public const float FloatingNumberSeconds = 1.5f;

        /// <summary>The circling light's loop on a gold tray or card shine. The amendment's ~2-3s.</summary>
        public const float ShineSweepLoopSeconds = 2.4f;

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
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
