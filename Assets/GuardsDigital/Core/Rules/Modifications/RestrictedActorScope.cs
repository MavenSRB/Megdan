using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// One class of actor a restriction names, relative to the affected object and to the source that
    /// declared the restriction.
    ///
    /// Milestone 8.7 adds the vocabulary because Wasp's Magnetic Dagger is the first verified text that
    /// says who: "Enemy units in radius cannot be swapped or placed by themselves, or by enemy heroes."
    /// Two of the three sentences the page needs are about the actor rather than about the activity -
    /// the affected unit doing it to itself, and a hero standing on a declared side of the source - and
    /// the restriction family carried neither, so a card that printed them would have had its
    /// qualification dropped and become a blanket prohibition on everyone.
    ///
    /// The team relations are stated relative to <em>the source</em>, not to the affected object,
    /// because that is the perspective card text is written from: Magnetic Dagger's "enemy heroes" are
    /// Wasp's enemies, which is why her own teammates may still reposition the units she pinned.
    /// </summary>
    public enum RestrictedActorRelation
    {
        None = 0,

        /// <summary>
        /// The affected object acting on itself - Magnetic Dagger's "by themselves". This is also the
        /// half of <see cref="AnyActor"/> that an offer query asks about, because there the actor and
        /// the restricted object are the same hero.
        /// </summary>
        AffectedObjectItself = 1,

        /// <summary>Heroes on the team opposing the source's controller.</summary>
        HeroesOpposingSource = 2,

        /// <summary>Heroes on the source controller's own team.</summary>
        HeroesAlliedWithSource = 3,

        /// <summary>
        /// Anyone at all, which is what a restriction that names no actor has always meant: "you
        /// cannot Fast Travel" bars the activity however it is reached.
        /// </summary>
        AnyActor = 4
    }

    /// <summary>
    /// The actors one restriction names, as definition content.
    ///
    /// It is a value with a named unqualified form rather than a nullable list, for the reason milestone
    /// 6.8f gave the modification set itself: a declaration that says nothing about who must say so out
    /// loud, and <see cref="AnyActor"/> is where that is written. An empty scope is refused - a
    /// restriction naming no actor at all would forbid nothing while reading as a prohibition.
    /// </summary>
    public sealed class RestrictedActorScope
    {
        private RestrictedActorScope(
            IReadOnlyList<RestrictedActorRelation> relations, bool exceptsTheBoundActor)
        {
            Relations = relations;
            ExceptsTheBoundActor = exceptsTheBoundActor;
        }

        /// <summary>The unqualified reading: every actor, which is what a bare restriction declares.</summary>
        public static RestrictedActorScope AnyActor { get; } = new RestrictedActorScope(
            new ReadOnlyCollection<RestrictedActorRelation>(
                new[] { RestrictedActorRelation.AnyActor }),
            false);

        /// <summary>
        /// A restriction qualified to the named actor classes. Order is the declaration's own and is
        /// preserved so a refusal can name the class that produced it in the order the card prints.
        /// </summary>
        public static RestrictedActorScope Of(params RestrictedActorRelation[] relations)
        {
            if (relations == null)
            {
                throw new ArgumentNullException(nameof(relations));
            }

            if (relations.Length == 0)
            {
                throw new ArgumentException(
                    "A restricted-actor scope names at least one class of actor; state " +
                    "RestrictedActorScope.AnyActor when the restriction qualifies none.",
                    nameof(relations));
            }

            if (relations.Any(relation => relation == RestrictedActorRelation.None) ||
                !relations.All(relation =>
                    Enum.IsDefined(typeof(RestrictedActorRelation), relation)))
            {
                throw new ArgumentException(
                    "A restricted-actor scope names defined actor classes.", nameof(relations));
            }

            if (relations.Distinct().Count() != relations.Length)
            {
                throw new ArgumentException(
                    "A restricted-actor scope names each actor class once.", nameof(relations));
            }

            return new RestrictedActorScope(
                new ReadOnlyCollection<RestrictedActorRelation>(relations.ToArray()),
                false);
        }

        /// <summary>
        /// Milestone 8.20. The same classes, minus the one hero the occurrence was created against -
        /// Arien's Expert and Master Duelist, "immune to attack actions of all enemy heroes,
        /// <b>except this attacker</b>".
        ///
        /// <para><b>Why the exception is not a sixth <see cref="RestrictedActorRelation"/>.</b> The
        /// five members are classes: an object, a side, everyone. "This attacker" is none of them - it
        /// is a hero picked out by the moment the card was played, and two occurrences of the same
        /// definition on the same hero in the same turn can except different heroes. So the definition
        /// declares <em>that</em> it excepts the bound actor and
        /// <see cref="Timing.ActiveEffectDefinition.BoundActorSource"/> declares <em>where the binding
        /// comes from</em>; which hero it turned out to be is the occurrence's, and
        /// <see cref="AppliedRuleModification.BoundActorHeroId"/> is what carries it to the query.</para>
        ///
        /// <para>An occurrence that bound nobody excepts nobody, which is the honest reading of a
        /// declaration whose "this" never got a referent: the scope's named classes stand unchanged.
        /// That is also what makes the exception safe on a restored payload that carried no bound
        /// actor - it widens the immunity to what the classes already said rather than inventing a
        /// hero.</para>
        /// </summary>
        public static RestrictedActorScope OfExceptingTheBoundActor(
            params RestrictedActorRelation[] relations)
        {
            var named = Of(relations);
            return new RestrictedActorScope(named.Relations, true);
        }

        public IReadOnlyList<RestrictedActorRelation> Relations { get; }

        /// <summary>
        /// Milestone 8.20. Whether this declaration carves out the hero its occurrence was bound to.
        /// False on every scope declared before it, and on every scope that carves nobody out.
        /// </summary>
        public bool ExceptsTheBoundActor { get; }

        /// <summary>Whether this scope states no qualification at all.</summary>
        public bool IsUnqualified =>
            !ExceptsTheBoundActor &&
            Relations.Count == 1 && Relations[0] == RestrictedActorRelation.AnyActor;

        /// <summary>
        /// Whether the affected object performing the activity itself is one of the named actors.
        /// This is the question an offer query asks, where the actor and the restricted object are one
        /// hero, and it is true for the unqualified scope because "anyone" includes them.
        /// </summary>
        public bool CoversTheAffectedObject =>
            Relations.Contains(RestrictedActorRelation.AffectedObjectItself) ||
            Relations.Contains(RestrictedActorRelation.AnyActor);

        public bool Covers(RestrictedActorRelation relation)
        {
            if (relation == RestrictedActorRelation.None)
            {
                throw new ArgumentException(
                    "Coverage requires a named actor class.", nameof(relation));
            }

            return Relations.Contains(relation) ||
                Relations.Contains(RestrictedActorRelation.AnyActor);
        }

        public override string ToString()
        {
            var named = IsUnqualified || (!ExceptsTheBoundActor &&
                Relations.Count == 1 && Relations[0] == RestrictedActorRelation.AnyActor)
                ? "by anyone"
                : "by " + string.Join(" or ", Relations);
            return ExceptsTheBoundActor ? named + ", except the bound actor" : named;
        }
    }
}
