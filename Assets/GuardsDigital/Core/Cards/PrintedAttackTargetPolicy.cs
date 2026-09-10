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
        HeroesOnly = 1,

        /// <summary>
        /// Milestone 8.35z30. The candidate set Silverarrow's three Red modals print:
        /// "<b>Choose one —</b> • Target a unit in range, which is not adjacent to any other unit.
        /// • Target a unit adjacent to you." Snap Shot, Opportunity Shot and Clear Shot print it word
        /// for word and differ only in their Attack and Range values.
        ///
        /// <para><b>Why one member and not a modal.</b> The two bullets differ in nothing but which
        /// unit becomes the target - no other consequence hangs off either arm - so the set of units
        /// this Attack may take is the union of the two arms, and naming a unit in that union names
        /// the arm that admits it. A modal that asked which bullet first and then offered that
        /// bullet's units would put a question in front of the player whose every answer is already
        /// visible in the candidate list, and would have to keep the two answers in agreement with
        /// this query for the offer, the choice and the resolution to read alike.</para>
        ///
        /// <para><b>The union is invariant to the one thing the face leaves open</b>, which is why no
        /// ruling was needed for it. "any other unit" either counts the attacker or does not. If it
        /// counts her, the first arm can never admit a unit beside her - she is adjacent to it - so
        /// the arm holds units at distance 2 or more with no neighbour at all. If it does not count
        /// her, the arm additionally holds units beside her whose only neighbour is her, and the
        /// second arm already holds every unit beside her. Both readings therefore give the same
        /// union: every enemy unit within the printed Range that no unit stands beside, plus every
        /// enemy unit adjacent to the attacker. <c>SilverarrowPrintedSetTests</c> asserts that
        /// equality on a board built to separate the two readings.</para>
        ///
        /// <para><b>Ruling 39 needs nothing here.</b> The user settled that the adjacent arm is still
        /// a ranged attack - the printed band is the action's type whichever arm is taken - and under
        /// this shape there is no arm to carry a type: the action is
        /// <see cref="AttackRangeKind.Ranged"/> and the policy only says which units it reaches.
        /// A "Block a ranged attack" therefore answers a point-blank Snap Shot by construction.</para>
        /// </summary>
        IsolatedUnitsOrUnitsAdjacentToYou = 2
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
        PrintedRange = 1,

        /// <summary>
        /// Milestone 8.35z30, and RULING 1 (user, 2026-09-06: "A is the number, it says AT
        /// MAXIMUM RANGE"). The reach Silverarrow's four "at maximum range" faces print - Shoot and
        /// Scoot, Keep the Distance, Long Shot and Rain of Arrows: the target stands at a distance
        /// <em>exactly equal</em> to the Range Total, and a unit any nearer is not a legal target of
        /// it at all.
        ///
        /// <para>It is a distinct reach rather than a filter over <see cref="PrintedRange"/> because
        /// the page composes one number and these cards read it differently. Every other targeting
        /// sentence in the verified roster says "in range", which is p.13's own ceiling; these four
        /// deliberately say something else, and reading them as the ceiling would leave the changed
        /// words meaning nothing. The number itself is the same one - p.16's Range item raises it, so
        /// an item moves the band outward rather than widening it, which is the observable difference
        /// between a band and a ceiling.</para>
        ///
        /// <para>The Range is still read off the card, so this reach carries
        /// <see cref="PrintedRange"/>'s own requirement that the Attack print a Range icon.</para>
        /// </summary>
        ExactlyPrintedRange = 2,

        /// <summary>
        /// Milestone 8.38a. The reach Swift's Prepared Shot and Killshot print - "Target a unit in
        /// range, in a straight line, and <em>not adjacent to you</em>": the same printed-Range
        /// ceiling <see cref="PrintedRange"/> reads, with a floor of two spaces under it.
        ///
        /// <para><b>It is a reach rather than a fourth dimension on this policy, and that is the
        /// honest reading rather than the cheap one.</b> This enum already carries three bands over
        /// one number - adjacency alone, everything up to the ceiling, and the ceiling exactly - so
        /// "in range and not adjacent" is the fourth band in the same family and not a separate
        /// question hanging beside it. Its lower bound is a constant rather than a printed value,
        /// which is what keeps it a member here instead of a parameter: p.19's separation-of-one is
        /// the only distance any printed face excludes this way.</para>
        ///
        /// <para>It reads the card's Range exactly as <see cref="PrintedRange"/> does and therefore
        /// carries the same requirement that the Attack print a Range icon. A sharpshooter who cannot
        /// answer an enemy that has closed with her is what both cards' names describe, and it is the
        /// same shape of drawback <see cref="ExactlyPrintedRange"/> states more strongly.</para>
        /// </summary>
        PrintedRangeBeyondAdjacent = 3
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
        NotInStraightLine = 1,

        /// <summary>
        /// Milestone 8.38a. The complement of the member above, and the narrowing three of Swift's
        /// five Red Attacks print: Snipe's "at maximum range, and <em>in a straight line</em>" and
        /// Prepared Shot's and Killshot's "in range, <em>in a straight line</em>, and not adjacent to
        /// you". Measured from the attacker's own space, by the same
        /// <see cref="GuardsDigital.Core.Board.StraightLineQuery"/> membership question its
        /// complement uses, so the two cannot disagree about what a file is.
        ///
        /// <para><b>p.19 makes this member and <see cref="AttackTargetReach.AdjacentOnly"/> a
        /// tautology together, and the constructor refuses the pair for that reason rather than for
        /// impossibility.</b> Every space beside the attacker is in a straight line with it, so an
        /// adjacent-only Attack restricted to the straight line admits exactly what it would have
        /// admitted anyway - and a policy that declares a narrowing while narrowing nothing would
        /// make <see cref="PrintedAttackTargetPolicy.NarrowsTheDefault"/> report a narrowing that is
        /// not there. No printed face states the pair; refusing it is the alternative to this file
        /// deciding what it would have meant.</para>
        /// </summary>
        InStraightLine = 2
    }

    /// <summary>
    /// When a printed Attack's target is bound. Milestone 8.19a, and the difference between the
    /// roster matrix's `attack-target@1` and `attack-target@2`.
    /// </summary>
    public enum AttackTargetBinding
    {
        /// <summary>
        /// Rulebook p.14's ordinary reading and every Attack in this build before Brogan: the target
        /// is chosen when the action starts, and p.14's step 1 text is applied against it.
        /// </summary>
        AtActionStart = 0,

        /// <summary>
        /// The target is named by the Attack's own "Before the attack:" text rather than chosen before
        /// it. Brogan's Mad Dash charges two spaces in a straight line and attacks the enemy it ends
        /// beside; Tigerclaw's Blink Strike moves through a unit and attacks the unit it crossed. In
        /// both, the target is a consequence of the move, so there is nothing to choose at the moment
        /// p.14 would ordinarily ask - and asking anyway would offer units the charge cannot reach.
        ///
        /// <para>An Attack declaring this reaches step 1 with no target and no Attack Total, and its
        /// text binds one through the `set-attack-target@1` capability. Text that binds none stops the
        /// action there under p.10's mandatory-step rule, keeping everything the text already did.</para>
        /// </summary>
        ByCardText = 1
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
            AttackTargetLine line = AttackTargetLine.AnyLine,
            AttackTargetBinding binding = AttackTargetBinding.AtActionStart)
        {
            if (!Enum.IsDefined(typeof(AttackTargetBinding), binding))
            {
                throw new ArgumentException(
                    "A printed Attack target policy states when its target is bound.", nameof(binding));
            }

            Binding = binding;
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

            // Milestone 8.38a. p.19 counts an adjacent space as a straight line, so the complement
            // of the refusal above is a tautology rather than an impossibility: an adjacent-only
            // Attack restricted TO the straight line admits exactly the set it would have admitted
            // with no line declared. It is refused all the same, and for a reason of its own -
            // NarrowsTheDefault would report a narrowing that is not there, and no printed face
            // states the pair, so accepting it would be this type deciding what it meant.
            if (line == AttackTargetLine.InStraightLine && reach == AttackTargetReach.AdjacentOnly)
            {
                throw new ArgumentException(
                    "Rulebook p.19 counts an adjacent space as a straight line, so an Attack " +
                    "reaching only adjacent units narrows nothing by restricting itself to the " +
                    "straight line.",
                    nameof(line));
            }

            // Milestone 8.35z30. The modal's first bullet says "a unit in range", so the
            // union is only writable over p.13's ceiling. Paired with AdjacentOnly it would be an
            // Attack whose first arm has no range to be in; paired with the exact band it would be a
            // sentence no face prints, and this file would be deciding what it meant.
            if (targetableUnits == AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou &&
                reach != AttackTargetReach.PrintedRange)
            {
                throw new ArgumentException(
                    "Silverarrow's Red modal prints \"a unit in range\" beside \"a unit adjacent to " +
                    "you\", so its union is declared over rulebook p.13's printed-Range ceiling.",
                    nameof(targetableUnits));
            }

            TargetableUnits = targetableUnits;
            Reach = reach;
            Line = line;
        }

        public AttackTargetableUnits TargetableUnits { get; }

        public AttackTargetReach Reach { get; }

        public AttackTargetLine Line { get; }

        /// <summary>
        /// Milestone 8.19a. When this Attack's target is bound - at the action's start, or by its own
        /// "Before the attack:" text.
        /// </summary>
        public AttackTargetBinding Binding { get; }

        /// <summary>Whether this Attack's own text names its target rather than the action's start.</summary>
        public bool TargetIsBoundByCardText => Binding == AttackTargetBinding.ByCardText;

        /// <summary>
        /// The same policy whose target its own "Before the attack:" text names, which is Mad Dash's
        /// and Blink Strike's shape. Milestone 8.19a.
        /// </summary>
        public PrintedAttackTargetPolicy BoundByCardText() =>
            new PrintedAttackTargetPolicy(
                TargetableUnits, Reach, Line, AttackTargetBinding.ByCardText);

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

        /// <summary>
        /// Milestone 8.35z30. The same policy read as a band rather than a ceiling, which
        /// is what Silverarrow's four "at maximum range" faces print. RULING 1.
        /// </summary>
        public PrintedAttackTargetPolicy AtExactlyThePrintedRange() =>
            new PrintedAttackTargetPolicy(
                TargetableUnits, AttackTargetReach.ExactlyPrintedRange, Line, Binding);

        /// <summary>
        /// Milestone 8.35z30. The union Silverarrow's three Red modals admit - see
        /// <see cref="AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou"/> for why one set
        /// states both printed bullets.
        /// </summary>
        public PrintedAttackTargetPolicy NarrowedToIsolatedUnitsAndThoseAdjacentToYou() =>
            new PrintedAttackTargetPolicy(
                AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou, Reach, Line, Binding);

        /// <summary>The same policy narrowed to p.13's heroes alone, which is Crackling Dagger's shape.</summary>
        public PrintedAttackTargetPolicy NarrowedToHeroes() =>
            new PrintedAttackTargetPolicy(AttackTargetableUnits.HeroesOnly, Reach, Line, Binding);

        /// <summary>
        /// The same policy with p.19's straight line taken out of it, which is both Boomerangs' shape.
        /// Milestone 8.10.
        /// </summary>
        public PrintedAttackTargetPolicy ExcludingTheStraightLine() =>
            new PrintedAttackTargetPolicy(
                TargetableUnits, Reach, AttackTargetLine.NotInStraightLine, Binding);

        /// <summary>
        /// Milestone 8.38a. The same policy narrowed TO p.19's straight line, which is what three of
        /// Swift's five Red Attacks print.
        /// </summary>
        public PrintedAttackTargetPolicy RestrictedToTheStraightLine() =>
            new PrintedAttackTargetPolicy(
                TargetableUnits, Reach, AttackTargetLine.InStraightLine, Binding);

        /// <summary>
        /// Milestone 8.38a. The same printed-Range ceiling with p.19's separation-of-one taken out
        /// from under it, which is Prepared Shot's and Killshot's "not adjacent to you".
        /// </summary>
        public PrintedAttackTargetPolicy BeyondAdjacentWithinThePrintedRange() =>
            new PrintedAttackTargetPolicy(
                TargetableUnits, AttackTargetReach.PrintedRangeBeyondAdjacent, Line, Binding);

        /// <summary>Whether this policy says anything the subtype's default does not.</summary>
        public bool NarrowsTheDefault(AttackRangeKind attackRange) =>
            !Equals(ForClassification(attackRange));

        public bool Equals(PrintedAttackTargetPolicy other) =>
            TargetableUnits == other.TargetableUnits && Reach == other.Reach && Line == other.Line &&
            Binding == other.Binding;

        public override bool Equals(object obj) =>
            obj is PrintedAttackTargetPolicy other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((((int)TargetableUnits * 397) ^ (int)Reach) * 397 ^ (int)Line) * 397 ^
                    (int)Binding;
            }
        }

        public override string ToString() => $"{TargetableUnits}, {Reach}, {Line}, {Binding}";
    }
}
