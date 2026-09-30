using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// What card text applied inside rulebook p.14's order may ask about the Attack it is running
    /// inside, and nothing else.
    ///
    /// <para>Milestone 8.10 carried one of these facts - the hero the Attack took - as a bare field on
    /// the resolution context, for Thunder Boomerang's "If you targeted a hero". Milestone 8.11 needs
    /// two more: Wasp's three Block cards are conditioned on the incoming attack being ranged, and two
    /// of them exclude the attacker from a selection the defender makes. Three loose fields describing
    /// one thing is what this replaces.</para>
    ///
    /// <para>Milestone 8.19a adds the two the roster's Before/After-Attack text reads. The minion the
    /// Attack took joins the hero, because a text that may bind either has to be able to see either -
    /// p.13 lets an Attack take a unit of either kind, and asking only about the hero made a minion
    /// target indistinguishable from no target at all. Beside them is the space the target stood on
    /// <em>before</em> the page's consequences, which is the one fact about a target that stops being
    /// readable from the board: at step 7 a defeated hero has already been removed, and Brogan's
    /// Onslaught moves into exactly that space. It is captured once, when the target is validated, and
    /// carried from there rather than re-read.</para>
    ///
    /// <para><b>It is context rather than a published fact.</b> These are properties of the action the
    /// text is printed inside, not of anything a node in that text did, so they arrive the way
    /// <c>EnclosingActionKind</c> does. Every field is undefined outside card text inside an Attack,
    /// and <see cref="None"/> is that state named rather than a null nobody checked.</para>
    /// </summary>
    public readonly struct EnclosingAttackFacts : IEquatable<EnclosingAttackFacts>
    {
        public EnclosingAttackFacts(HeroId actorId, HeroId heroTargetId, AttackRangeKind range)
            : this(actorId, heroTargetId, default, range, null)
        {
        }

        public EnclosingAttackFacts(
            HeroId actorId,
            HeroId heroTargetId,
            MinionId minionTargetId,
            AttackRangeKind range,
            HexCoordinate? targetSpaceBeforeConsequences,
            HexCoordinate? actorSpaceAtActionStart = null,
            CardInstanceId defenseCardInstanceId = default,
            bool targetWasRemoved = false,
            RepeatedAttackSubjectNarrowing repeatedSubject = default,
            int heroTargetBody = 0)
        {
            HeroTargetBody = heroTargetId.IsDefined ? heroTargetBody : 0;
            DefenseCardInstanceId = defenseCardInstanceId;
            TargetWasRemoved = targetWasRemoved;
            RepeatedSubject = repeatedSubject;
            if (!Enum.IsDefined(typeof(AttackRangeKind), range))
            {
                throw new ArgumentException(
                    "An enclosing Attack states its printed subtype, or None outside one.",
                    nameof(range));
            }

            if (heroTargetId.IsDefined && minionTargetId.IsDefined)
            {
                throw new ArgumentException(
                    "An Attack takes one unit, so its enclosing facts name a hero or a minion and " +
                    "never both.",
                    nameof(minionTargetId));
            }

            ActorId = actorId;
            HeroTargetId = heroTargetId;
            MinionTargetId = minionTargetId;
            Range = range;
            TargetSpaceBeforeConsequences = targetSpaceBeforeConsequences;
            ActorSpaceAtActionStart = actorSpaceAtActionStart;
        }

        /// <summary>No enclosing Attack: every effect that is not card text inside one.</summary>
        public static EnclosingAttackFacts None => default;

        /// <summary>
        /// The attacking hero. Defined wherever there is an enclosing Attack at all, including at
        /// rulebook p.14's step 4, where the text being applied belongs to the defender's card and the
        /// attacker is the other side.
        /// </summary>
        public HeroId ActorId { get; }

        /// <summary>
        /// The hero the Attack took, or the undefined hero when it took a minion. p.13 lets an Attack
        /// target either, so "no hero" is an ordinary answer rather than a missing one.
        /// </summary>
        public HeroId HeroTargetId { get; }

        /// <summary>
        /// Milestone 9.21c. The BODY of <see cref="HeroTargetId"/> the Attack took (RZ-Q3) - 1 for every
        /// hero with one body, 0 where no hero was taken or the facts were built without one. A
        /// derived address like <see cref="Components.BoardObjectReference.HeroBody"/>, so it is not
        /// part of equality; the attack resolution it is read from persists the body.
        /// </summary>
        public int HeroTargetBody { get; }

        /// <summary>
        /// Milestone 9.21c. The Attack's target as a board object: the attacked body where one is
        /// named, so "the target" of card text is read, measured and moved at the body the Attack took
        /// rather than at the hero's acting body (RZ-Q3). The undefined reference where the Attack took
        /// no hero.
        /// </summary>
        public Components.BoardObjectReference HeroTargetReference =>
            !HeroTargetId.IsDefined
                ? default
                : HeroTargetBody > 0
                    ? Components.BoardObjectReference.ForHeroBody(HeroTargetId, HeroTargetBody)
                    : Components.BoardObjectReference.ForHero(HeroTargetId);

        /// <summary>
        /// The minion the Attack took, or the undefined minion when it took a hero. Milestone 8.19a:
        /// the other half of p.13's sentence, needed because a text whose target is bound by the text
        /// itself has to be able to say which unit it bound.
        /// </summary>
        public MinionId MinionTargetId { get; }

        /// <summary>Whether this Attack has taken a unit yet at all.</summary>
        public bool HasTarget => HeroTargetId.IsDefined || MinionTargetId.IsDefined;

        /// <summary>
        /// p.13's printed subtype of the Attack, or <see cref="AttackRangeKind.None"/> outside one.
        /// It is what Wasp's Block cards are conditioned on: "Block a ranged attack."
        /// </summary>
        public AttackRangeKind Range { get; }

        /// <summary>
        /// Milestone 8.19a. Where the target stood when the Attack bound it, before p.14's steps 6 and
        /// 7 could remove it - or nothing at all where the Attack has no target yet.
        ///
        /// <para>It is carried rather than re-read because it is the one target fact the board stops
        /// answering: p.14 removes a defeated hero at step 6 and applies "After the attack:" text at
        /// step 7, so a rider that names the target's space is asking about a piece that is no longer
        /// standing anywhere. Capturing it at validation also makes it a fact about the attack rather
        /// than about the moment the rider happened to run, which is what lets a pause and a restore
        /// stand between the two.</para>
        /// </summary>
        public HexCoordinate? TargetSpaceBeforeConsequences { get; }

        /// <summary>
        /// Milestone 8.23. Where the attacking hero stood when this Attack started, or nothing outside
        /// an Attack. It is what Tigerclaw's Combat Reflexes asks about at p.14's step 7 - "If you did
        /// not move before the attack" - and the reasoning is on
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.PendingAttackResolution.ActorSpaceAtActionStart"/>.
        /// </summary>
        public HexCoordinate? ActorSpaceAtActionStart { get; }

        /// <summary>
        /// Milestone 8.35z33. The card the defender revealed and discarded at rulebook p.14's step 3, or
        /// the undefined card where this Attack has not reached that step or was never defended.
        ///
        /// <para>It is what Whisper's Swift Justice names at step 7 - "that hero performs a movement
        /// action on <em>the card they defended with</em>" - and it is the only fact about a defence
        /// that the board stops being able to answer: by the time the rider runs, that card is one of
        /// several in a discard pile with nothing distinguishing it.</para>
        ///
        /// <para><b>It is read from the Attack's own defence offer rather than stored anywhere new.</b>
        /// The pending Attack has carried the offer the defender answered with since the Defense window
        /// was built, so this field is a projection of state the resolution already holds - which is
        /// why it costs no snapshot bytes and no schema movement. The face's "empty discard"
        /// precondition would make the card identifiable a second way, by being the only one in the
        /// pile; reading the offer instead means the rider does not depend on that precondition having
        /// been enforced.</para>
        /// </summary>
        public CardInstanceId DefenseCardInstanceId { get; }

        /// <summary>
        /// Milestone 8.35z50. Whether rulebook p.14 step 6 removed this Attack's target -
        /// a defeated hero sent to respawn, or a defeated minion taken off the board - or
        /// <c>false</c> where there is no target, the Attack has not reached step 6 yet, or a removal
        /// was prevented. It is what Ursafar's Prey Drive, Prey Abundance and Feeding Frenzy ask with
        /// "and the target was not removed".
        ///
        /// <para><b>Re-derived from live board occupancy, never stored.</b> Nothing on
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.PendingAttackResolution"/> carries this -
        /// it costs no snapshot bytes and moves no schema, the same reason
        /// <see cref="DefenseCardInstanceId"/> is a projection rather than a field. By the moment
        /// "After the attack:" text can ask the question, step 6's removal (if any) has already run
        /// and nothing else has touched the board since, so reading current occupancy for the target's
        /// own identity gives the exact answer step 6 reached - live, on the first pass, and
        /// identically after a restored pause, because the board a restore rebuilds already reflects
        /// whatever step 6 did.</para>
        /// </summary>
        public bool TargetWasRemoved { get; }

        /// <summary>
        /// Milestone 8.75d. What the clause that produced this Attack instance lets it take, where
        /// this instance is a REPEAT and its clause narrows anything -
        /// <see cref="RepeatedAttackSubjectNarrowing.None"/> for every printed instance and every
        /// repeat whose clause says nothing.
        ///
        /// <para>It is here rather than anywhere else because it is a property of the ACTION the text
        /// is printed inside, exactly like <see cref="Range"/>: the text did not produce it, cannot
        /// change it, and has to obey it. What reads it is the one place every selection's offer and
        /// answer are already narrowed together -
        /// <see cref="EffectSelectionEligibilityQuery"/> - so a card-text subject that binds this
        /// Attack's target offers exactly the units rulebook p.13's own question would accept.</para>
        /// </summary>
        public RepeatedAttackSubjectNarrowing RepeatedSubject { get; }

        /// <summary>Whether there is an enclosing Attack to ask about at all.</summary>
        public bool IsDefined => ActorId.IsDefined;

        public bool Equals(EnclosingAttackFacts other) =>
            ActorId.Equals(other.ActorId) &&
            HeroTargetId.Equals(other.HeroTargetId) &&
            MinionTargetId.Equals(other.MinionTargetId) &&
            Range == other.Range &&
            Nullable.Equals(TargetSpaceBeforeConsequences, other.TargetSpaceBeforeConsequences) &&
            TargetWasRemoved == other.TargetWasRemoved &&
            RepeatedSubject.Equals(other.RepeatedSubject);

        public override bool Equals(object obj) => obj is EnclosingAttackFacts other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (ActorId.GetHashCode() * 397) ^ HeroTargetId.GetHashCode();
                hash = (hash * 397) ^ MinionTargetId.GetHashCode();
                hash = (hash * 397) ^ (int)Range;
                hash = (hash * 397) ^ (TargetSpaceBeforeConsequences?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ TargetWasRemoved.GetHashCode();
                return (hash * 397) ^ RepeatedSubject.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (!IsDefined) return "no enclosing attack";
            var target = HeroTargetId.IsDefined ? HeroTargetId.Value
                : MinionTargetId.IsDefined ? MinionTargetId.Value
                : "no target";
            return $"{ActorId.Value} -> {target} ({Range})";
        }
    }
}
