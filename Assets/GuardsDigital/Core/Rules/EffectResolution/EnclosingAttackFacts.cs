using System;
using GuardsDigital.Core.Cards;
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
    /// <para><b>It is context rather than a published fact.</b> These are properties of the action the
    /// text is printed inside, not of anything a node in that text did, so they arrive the way
    /// <c>EnclosingActionKind</c> does. Every field is undefined outside card text inside an Attack,
    /// and <see cref="None"/> is that state named rather than a null nobody checked.</para>
    /// </summary>
    public readonly struct EnclosingAttackFacts : IEquatable<EnclosingAttackFacts>
    {
        public EnclosingAttackFacts(HeroId actorId, HeroId heroTargetId, AttackRangeKind range)
        {
            if (!Enum.IsDefined(typeof(AttackRangeKind), range))
            {
                throw new ArgumentException(
                    "An enclosing Attack states its printed subtype, or None outside one.",
                    nameof(range));
            }

            ActorId = actorId;
            HeroTargetId = heroTargetId;
            Range = range;
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
        /// p.13's printed subtype of the Attack, or <see cref="AttackRangeKind.None"/> outside one.
        /// It is what Wasp's Block cards are conditioned on: "Block a ranged attack."
        /// </summary>
        public AttackRangeKind Range { get; }

        /// <summary>Whether there is an enclosing Attack to ask about at all.</summary>
        public bool IsDefined => ActorId.IsDefined;

        public bool Equals(EnclosingAttackFacts other) =>
            ActorId.Equals(other.ActorId) &&
            HeroTargetId.Equals(other.HeroTargetId) &&
            Range == other.Range;

        public override bool Equals(object obj) => obj is EnclosingAttackFacts other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ActorId.GetHashCode() * 397) ^ HeroTargetId.GetHashCode()) * 397 ^ (int)Range;
            }
        }

        public override string ToString() =>
            IsDefined ? $"{ActorId.Value} -> {HeroTargetId.Value} ({Range})" : "no enclosing attack";
    }
}
