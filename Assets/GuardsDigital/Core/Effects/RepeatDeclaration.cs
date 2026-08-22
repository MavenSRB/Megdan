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
        DifferentTarget = 2
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
