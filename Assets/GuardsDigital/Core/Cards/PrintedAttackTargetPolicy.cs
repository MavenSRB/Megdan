using System;

namespace GuardsDigital.Core.Cards
{
    /// <summary>Which enemy units a printed Attack's card text lets it target.</summary>
    public enum AttackTargetableUnits
    {
        /// <summary>
        /// Rulebook p.13's default: "You may attack enemy heroes and minions by performing an Attack
        /// action."
        /// </summary>
        HeroesAndMinions = 0,

        /// <summary>
        /// The narrowing a card states in its own words - Wasp's Crackling Dagger prints "Target a
        /// hero adjacent to you", and a minion beside her is not a legal target of it.
        /// </summary>
        HeroesOnly = 1
    }

    /// <summary>How far a printed Attack reaches, and what says so.</summary>
    public enum AttackTargetReach
    {
        /// <summary>
        /// p.13's own sentence for a non-Ranged Attack: "Non-Ranged Attacks can only target units
        /// adjacent to you (even if the card has a radius icon)."
        /// </summary>
        AdjacentOnly = 0,

        /// <summary>
        /// p.13's sentence for a Ranged one: "Ranged Attacks can target units based on the Range value
        /// on the card (See page 9)." The value comes from the action's printed
        /// <see cref="PrintedValueChannel.Range"/>, composed with p.16's items exactly as the Attack
        /// value is.
        /// </summary>
        PrintedRange = 1
    }

    /// <summary>Whether a printed Attack's own words exclude rulebook p.19's straight line.</summary>
    public enum AttackTargetLine
    {
        /// <summary>
        /// p.13's default. The page says nothing about files, so every enemy unit within the reach
        /// above is a legal target of an ordinary Attack.
        /// </summary>
        AnyLine = 0,

        /// <summary>
        /// The narrowing both of Wasp's Boomerangs print - "Target a unit in range and not in a
        /// straight line" - measured from the attacker's own space.
        ///
        /// <para>Charged Boomerang's face adds "( Adjacent units are in a straight line as well. )",
        /// which p.9 makes clarification rather than a second rule, and it is not modelled as one:
        /// <see cref="GuardsDigital.Core.Board.StraightLineQuery"/> already answers adjacency as
        /// p.19's separation-of-one case, so the reminder is describing the one query rather than
        /// adding to it.</para>
        /// </summary>
        NotInStraightLine = 1
    }

    /// <summary>
    /// What a printed Attack may target, as typed content rather than as a sentence something reads at
    /// runtime.
    ///
    /// <para>Milestone 8.5. Two things decided this shape, and both are p.13's. First, "Most Attacks
    /// are Primary actions. When you perform a primary action, apply the card text" - so an Attack's
    /// targeting is card text, and card text is content. Second, the page states the reach twice, once
    /// per subtype, and states it as a rule about the card rather than about the classification label:
    /// a non-Ranged Attack reaches adjacent units "even if the card has a radius icon", and a Ranged
    /// one reaches "based on the Range value on the card". Before this milestone the reach was inferred
    /// from <see cref="AttackRangeKind"/> inside the target query, which made the classification the
    /// source of a rule the page attributes to the card's own text.</para>
    ///
    /// <para><see cref="ForClassification"/> reproduces p.13 exactly for a card that narrows nothing,
    /// which is every Attack in this build until a verified face says otherwise. A definition that
    /// declares something else has to declare it wholly, and the two refusals below are the page's own
    /// sentences: a reach of <see cref="AttackTargetReach.PrintedRange"/> needs a printed Range to
    /// read, and a non-Ranged Attack may not have one, because p.13 gives its adjacency no
    /// exception.</para>
    ///
    /// <para>Milestone 8.10 added the third dimension for the same reason the first two are here.
    /// Both Boomerangs print "Target a unit in range and not in a straight line", which is a sentence
    /// about what the Attack may target rather than a rider hanging off it, so it belongs to the
    /// Attack's own target set and not to milestone 8.5's card-text seam. Declaring it here is what
    /// keeps the offer, the target choice and the resolution reading one answer.</para>
    /// </summary>
    public readonly struct PrintedAttackTargetPolicy : IEquatable<PrintedAttackTargetPolicy>
    {
        public PrintedAttackTargetPolicy(
            AttackTargetableUnits targetableUnits,
            AttackTargetReach reach,
            AttackTargetLine line = AttackTargetLine.AnyLine)
        {
            if (!Enum.IsDefined(typeof(AttackTargetableUnits), targetableUnits))
            {
                throw new ArgumentException(
                    "A printed Attack target policy states which units it may target.",
                    nameof(targetableUnits));
            }

            if (!Enum.IsDefined(typeof(AttackTargetReach), reach))
            {
                throw new ArgumentException(
                    "A printed Attack target policy states how far it reaches.", nameof(reach));
            }

            if (!Enum.IsDefined(typeof(AttackTargetLine), line))
            {
                throw new ArgumentException(
                    "A printed Attack target policy states whether it excludes the straight line.",
                    nameof(line));
            }

            // Milestone 8.10. p.19 makes adjacency the separation-of-one case of one file, so every
            // space beside the attacker is in a straight line with it. A policy pairing the line
            // complement with p.13's adjacent-only reach would therefore be a printed Attack that can
            // never have a target, which no face states and no match could resolve; it is refused here
            // rather than left to produce an empty offer that reads like a board position.
            if (line == AttackTargetLine.NotInStraightLine && reach == AttackTargetReach.AdjacentOnly)
            {
                throw new ArgumentException(
                    "Rulebook p.19 counts an adjacent space as a straight line, so an Attack reaching " +
                    "only adjacent units and excluding the straight line could never target anything.",
                    nameof(line));
            }

            TargetableUnits = targetableUnits;
            Reach = reach;
            Line = line;
        }

        public AttackTargetableUnits TargetableUnits { get; }

        public AttackTargetReach Reach { get; }

        public AttackTargetLine Line { get; }

        /// <summary>
        /// p.13's default for an Attack of this subtype: every enemy unit, adjacent for a non-Ranged
        /// Attack and within the printed Range for a Ranged one.
        /// </summary>
        public static PrintedAttackTargetPolicy ForClassification(AttackRangeKind attackRange)
        {
            switch (attackRange)
            {
                case AttackRangeKind.Ranged:
                    return new PrintedAttackTargetPolicy(
                        AttackTargetableUnits.HeroesAndMinions, AttackTargetReach.PrintedRange);
                case AttackRangeKind.NonRanged:
                    return new PrintedAttackTargetPolicy(
                        AttackTargetableUnits.HeroesAndMinions, AttackTargetReach.AdjacentOnly);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(attackRange), attackRange,
                        "Only an Attack subtype has a printed target policy.");
            }
        }

        /// <summary>The same policy narrowed to p.13's heroes alone, which is Crackling Dagger's shape.</summary>
        public PrintedAttackTargetPolicy NarrowedToHeroes() =>
            new PrintedAttackTargetPolicy(AttackTargetableUnits.HeroesOnly, Reach, Line);

        /// <summary>
        /// The same policy with p.19's straight line taken out of it, which is both Boomerangs' shape.
        /// Milestone 8.10.
        /// </summary>
        public PrintedAttackTargetPolicy ExcludingTheStraightLine() =>
            new PrintedAttackTargetPolicy(
                TargetableUnits, Reach, AttackTargetLine.NotInStraightLine);

        /// <summary>Whether this policy says anything the subtype's default does not.</summary>
        public bool NarrowsTheDefault(AttackRangeKind attackRange) =>
            !Equals(ForClassification(attackRange));

        public bool Equals(PrintedAttackTargetPolicy other) =>
            TargetableUnits == other.TargetableUnits && Reach == other.Reach && Line == other.Line;

        public override bool Equals(object obj) =>
            obj is PrintedAttackTargetPolicy other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (((int)TargetableUnits * 397) ^ (int)Reach) * 397 ^ (int)Line;
            }
        }

        public override string ToString() => $"{TargetableUnits}, {Reach}, {Line}";
    }
}
