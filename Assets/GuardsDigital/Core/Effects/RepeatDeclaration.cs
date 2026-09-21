using System;
using GuardsDigital.Core.Components;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// What a repeated instance may do about the target the instance before it chose.
    ///
    /// Rulebook p.10's default is that a repeated action is a new instance which may make different
    /// modal choices, so a card that wants the same target - or a different one - has to say so, and
    /// two of Wasp's sentences do: her three telekinetic Move skills print "may repeat once on the same
    /// target", and Thunder Boomerang prints "may repeat once on a different target".
    ///
    /// This is a declaration on the <see cref="EffectNodeKind.RepeatAction"/> node rather than a
    /// parameter of the selection it binds, because the sentence is the repeat's: the first instance
    /// chose freely, and it is the repetition that is constrained. Only the two readings verified
    /// content prints are here; the different-target exclusion Thunder Boomerang needs lands with that
    /// card at milestone 8.10, and adding its member now would leave a declaration nothing consumes.
    ///
    /// Milestone 8.6.
    /// </summary>
    public enum RepeatTargetBinding
    {
        /// <summary>
        /// p.10's default: the repeated instance chooses for itself. This is what every repeat built
        /// before milestone 8.6 declared by declaring nothing, which is why it is the zero value.
        /// </summary>
        Unbound = 0,

        /// <summary>
        /// `on the same target`: the repeated instance does not choose at all. The piece the bound
        /// selection node named is supplied to that node again, wherever it now stands, and a piece
        /// that is no longer eligible makes the repeated instance's selection impossible rather than
        /// free.
        /// </summary>
        SameTarget = 1,

        /// <summary>
        /// Milestone 8.10. `on a different target`: the repeated instance chooses as freely as an
        /// unbound one does, out of a legal set with one unit taken out of it - the one the instance
        /// that asked for the repeat took.
        ///
        /// <para>It is the complement of <see cref="SameTarget"/> and not a variation on it: that one
        /// supplies a target and asks nothing, this one asks the ordinary question of a smaller set.
        /// Thunder Boomerang is what prints it, and it prints it about the Attack it repeats rather
        /// than about a selection node - which is why the exclusion is carried by the repeated Attack
        /// instance rather than bound to a node id.</para>
        /// </summary>
        DifferentTarget = 2,

        /// <summary>
        /// Milestone 8.24. `on a different enemy hero`: the exclusion <see cref="DifferentTarget"/>
        /// makes, and a narrowing of what the repeated instance may take at all.
        ///
        /// <para>It is a third member rather than a second axis because the cards print it as one
        /// clause. Xargatha's Cleave says "Target a unit adjacent to you. After the attack: May repeat
        /// once on a different enemy hero", and its parenthesis spells out the consequence the two
        /// halves have together - "You may repeat even if the original target was a minion". So the
        /// printed instance admits p.19's units and the repeated one admits enemy heroes, which no
        /// earlier repeat in this build does: <see cref="DifferentTarget"/> takes one unit out of a set
        /// and leaves the set's kinds alone.</para>
        ///
        /// <para><b>The narrowing is derived, never stored.</b> A repeated instance already knows it is
        /// one, and which action it repeats; what that action's card declares here is read from the
        /// trusted definition when the instance builds its candidates, exactly as
        /// <see cref="RepeatContinuation"/> rebinds meaning rather than carrying it. So this member
        /// moves no snapshot field and no schema version, and a tampered payload cannot widen the set
        /// back.</para>
        /// </summary>
        DifferentEnemyHero = 3,

        /// <summary>
        /// Milestone 8.35z46. `on a minion`: <see cref="DifferentEnemyHero"/>'s twin at
        /// the other end of p.19's unit kinds, and the second clause of the only face in the roster
        /// that prints two.
        ///
        /// <para>Silverarrow's Rain of Arrows prints "Target a unit at maximum range. If you target a
        /// hero, repeat once on a different hero; if you do, repeat once on a minion." RULING 38
        /// settles it: the repeats are mandatory where a legal target exists, the minion clause fires
        /// only if the hero repeat happened, every instance inherits the action's own targeting
        /// sentence, and the minion must be a different unit. So this member narrows the repeated
        /// instance to minions and excludes the target the asking instance took, which is exactly the
        /// two halves <see cref="DifferentEnemyHero"/> has - the exclusion is vacuous in the one chain
        /// that reaches it, because the instance before it took a hero, and it is carried anyway
        /// because the ruling says the minion is a different unit rather than because a board makes
        /// it matter.</para>
        ///
        /// <para><b>What this member could not do on its own, and what had to be built beside it.</b>
        /// The narrowing is derived from the card when a repeated instance builds its candidates, and a
        /// card that prints two clauses cannot be told apart that way - which is the seam limit
        /// milestone 8.35z30 measured and wrote up. It is answered by
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.CardTextRepeatChain"/>, which reads the
        /// instance's position in the chain out of the identity the chain itself mints. Nothing is
        /// stored: this member moves no snapshot field and no schema version, exactly as
        /// <see cref="DifferentEnemyHero"/> does not.</para>
        /// </summary>
        DifferentEnemyMinion = 4,

        /// <summary>
        /// Milestone 8.39d1. `on a different target`, where the target is the subject a card-text
        /// SELECTION node chose rather than the action's own - Cutter's Broadside, "An enemy hero
        /// adjacent to another enemy unit and not adjacent to you discards a card, if able. <b>May
        /// repeat once on a different target.</b>"
        ///
        /// <para><b>It is not <see cref="DifferentTarget"/> with a node id, and the difference is which
        /// state carries the exclusion.</b> That member's own remarks say it "prints it about the
        /// Attack it repeats rather than about a selection node - which is why the exclusion is carried
        /// by the repeated Attack instance rather than bound to a node id", and the store is
        /// <c>PendingAttackResolution.ExcludedTargets</c>. Broadside is a Skill. It has no attack
        /// target, so there is nothing there to exclude and nowhere to keep the exclusion; what it has
        /// is a selection node whose subject the repeated instance must not take again.</para>
        ///
        /// <para><b>It is <see cref="SameTarget"/>'s capture with the opposite use, which is why the
        /// two share one binding and one capture.</b> That member records the piece the bound selection
        /// named, onto the frame, at the moment the selection applied - the last moment it is still
        /// standing where the selection saw it - and hands it to the repeated instance's own
        /// <c>Resume</c>. This one records the identical piece by the identical route and hands it to
        /// nobody: the repeated instance asks its ordinary question, of a set with that piece taken
        /// out.</para>
        ///
        /// <para><b>The exclusion is applied where every other generic narrowing is.</b>
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.EffectSelectionEligibilityQuery"/>
        /// already narrows an offer without knowing what a target is, and already has an answer-side
        /// twin, so the offer and the answer cannot come to disagree about who is excluded. Supplying a
        /// value can be capability-agnostic by handing it over; REMOVING one cannot, because only a
        /// selection knows its candidates - so it is removed from the offer rather than from inside the
        /// handler.</para>
        ///
        /// <para><b>The narrowing is derived, never stored</b>, exactly as
        /// <see cref="DifferentEnemyHero"/>'s remarks record: the repeated instance reads this member
        /// off the trusted definition when it builds its candidates, and the piece it reads comes from
        /// the same restorable frame state <see cref="SameTarget"/> has used since milestone 8.6. So
        /// this member moves no snapshot field and no schema version either.</para>
        /// </summary>
        DifferentSelectionSubject = 5,

        /// <summary>
        /// Milestone 8.69b. `on a different unit adjacent to you` - Brynn the Seeker's Split Attack,
        /// "Target a unit in range. If you target a hero who is adjacent to 3 or more obstacles, may
        /// repeat once on a <b>different unit adjacent to you</b>."
        ///
        /// <para><b>It is <see cref="DifferentTarget"/>'s exclusion with a REACH narrowing beside it,
        /// which is a third thing this enum has not carried.</b> <see cref="DifferentEnemyHero"/> and
        /// <see cref="DifferentEnemyMinion"/> narrow the repeated instance by p.19's unit KIND, and
        /// this one leaves the kinds exactly as the printed instance had them - "a unit" is the same
        /// word the first sentence uses - while shrinking how far the repeat may reach. Split Attack
        /// prints Range 2 with the Ranged band, so the repeated instance reaches p.19's innermost ring
        /// alone: the card's own words override p.10's "the entire action again", which is the only
        /// reading that leaves "adjacent to you" doing anything at all.</para>
        ///
        /// <para><b>It is measured from where the attacker is standing when the repeat builds its
        /// candidates</b>, because that is the space p.19's adjacency is about and no earlier moment is
        /// named by the sentence. Nothing in either split face moves him between the two instances; a
        /// face that did would be asking a question this member does not answer, and would be a
        /// different member here.</para>
        ///
        /// <para><b>The narrowing is derived, never stored</b>, exactly as
        /// <see cref="DifferentEnemyHero"/>'s remarks record: a repeated instance reads this member off
        /// the trusted definition when it builds its candidates, so it moves no snapshot field and no
        /// schema version, and a tampered payload cannot widen the ring back.</para>
        /// </summary>
        DifferentUnitAdjacentToActor = 6
    }

    /// <summary>
    /// How far through a bounded repeat an action instance is, and the target it inherited.
    ///
    /// <para><b>Why this is state rather than a derivation.</b> Milestone 6.7 needed neither: a repeat
    /// was bounded at two instances by construction, so "am I a repeat" - already carried by
    /// <see cref="GuardsDigital.Core.Cards.ActionInvocationContext.Repeated"/> - was the whole answer,
    /// and the page's different-choices default meant nothing crossed from one instance to the next.
    /// Center of Mass's "may repeat up to two times" ends both. A third instance has to know it is the
    /// third, and "on the same target" has to carry an identity across an action boundary that a
    /// snapshot, a replay and a retained-prefix Undo can all fall between.</para>
    ///
    /// <para><b>What it deliberately does not carry.</b> No node identity, no definition, no semantics.
    /// Which node the target binds to is read from the trusted definition when the repeated instance
    /// reaches it, exactly as every other continuation in this engine rebinds meaning rather than
    /// storing it, so a tampered payload can name a different piece but cannot move a rule.</para>
    ///
    /// Milestone 8.6, and the whole of the schema move that milestone owes.
    /// </summary>
    public readonly struct RepeatContinuation : IEquatable<RepeatContinuation>
    {
        private RepeatContinuation(int completedRepeats, BoardObjectReference boundTarget)
        {
            CompletedRepeats = completedRepeats;
            BoundTarget = boundTarget;
        }

        /// <summary>
        /// The state a printed instance starts in: no repeat has happened and nothing is inherited.
        /// It is the default value, so an action family that has never heard of repeats keeps working
        /// without naming this at all.
        /// </summary>
        public static RepeatContinuation FirstInstance => default;

        /// <summary>How many repeated instances have already run in this chain.</summary>
        public int CompletedRepeats { get; }

        /// <summary>
        /// The piece an `on the same target` repeat binds, or the named absence.
        ///
        /// It is the piece and deliberately not the space it was standing on. Everywhere else in this
        /// engine a board subject is named by its coordinate, which p.19's one-obstacle-per-space rule
        /// makes unambiguous - but only for as long as nothing has moved. A repeat is separated from
        /// the instance that chose the target by a whole action boundary, and the commonest thing for
        /// that instance to have done is move the target, so a coordinate here would name whatever
        /// happens to be standing where the target used to be. p.10's "the same target" names the
        /// piece, and Wasp's orbit skills move it every time.
        /// </summary>
        public BoardObjectReference BoundTarget { get; }

        /// <summary>Whether an earlier instance's target is bound onto this one.</summary>
        public bool HasBoundTarget => BoundTarget.IsDefined;

        /// <summary>Whether this instance is itself a repeat.</summary>
        public bool IsRepeatedInstance => CompletedRepeats > 0;

        /// <summary>The next instance in an unbound chain - p.10's default.</summary>
        public static RepeatContinuation Next(int completedRepeats) =>
            new RepeatContinuation(Require(completedRepeats), default);

        /// <summary>The next instance in a chain whose card said `on the same target`.</summary>
        public static RepeatContinuation NextBoundTo(
            int completedRepeats,
            BoardObjectReference boundTarget)
        {
            if (!boundTarget.IsDefined)
            {
                throw new ArgumentException(
                    "A bound repeat names the piece it binds.", nameof(boundTarget));
            }

            return new RepeatContinuation(Require(completedRepeats), boundTarget);
        }

        /// <summary>
        /// This instance carrying the piece its card's `on the same target` will bind, recorded at the
        /// moment the selection names it rather than at the moment the repeat asks - which is the whole
        /// point of binding a piece, because by the time the repeat asks the instance has usually moved
        /// it.
        /// </summary>
        public RepeatContinuation Binding(BoardObjectReference boundTarget) =>
            new RepeatContinuation(CompletedRepeats, boundTarget);

        public bool Equals(RepeatContinuation other) =>
            CompletedRepeats == other.CompletedRepeats && BoundTarget.Equals(other.BoundTarget);

        public override bool Equals(object obj) => obj is RepeatContinuation other && Equals(other);

        public override int GetHashCode() => (CompletedRepeats * 397) ^ BoundTarget.GetHashCode();

        public override string ToString() =>
            HasBoundTarget
                ? $"repeat {CompletedRepeats} bound to {BoundTarget.Kind}"
                : $"repeat {CompletedRepeats}";

        private static int Require(int completedRepeats)
        {
            if (completedRepeats < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedRepeats),
                    completedRepeats,
                    "A repeated instance is at least the first repeat; the printed instance is " +
                    nameof(FirstInstance) + ".");
            }

            return completedRepeats;
        }
    }
}
