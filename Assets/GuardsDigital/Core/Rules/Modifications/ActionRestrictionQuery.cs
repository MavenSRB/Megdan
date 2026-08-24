using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Whether a hero is currently barred from performing an action of one kind, as every offer query
    /// asks it.
    ///
    /// Milestone 8.7 states it once. Before it, one activity of the nine had a consumer - milestone
    /// 6.8c's Fast Travel offer - and the other eight were declarable and inert, which is the half of
    /// the 7.9a review's F-CARD-03 that is about restrictions. The fix is not eight consumers but one:
    /// <see cref="RuleActivityQuery.ForAction"/> already maps every action kind to its activity, so a
    /// single predicate asked with the kind covers the whole family, and a card that first declares
    /// "cannot Attack" needs no new code to be obeyed.
    ///
    /// It answers the undirected question - can this hero do this - because that is the only one an
    /// offer has: the hero is both the restricted object and the actor. A restriction that names other
    /// heroes therefore leaves the offer standing, and <see cref="RestrictionQuery"/> is what the
    /// operations consult when the two differ.
    ///
    /// Rulebook p.12 is what makes removing an offer costless rather than a way to strand a player: a
    /// hero may Hold rather than act, so a card whose primary is barred is still spent and the turn
    /// still passes.
    /// </summary>
    public static class ActionRestrictionQuery
    {
        /// <summary>
        /// Whether <paramref name="actorId"/> may not perform <paramref name="actionKind"/> right now,
        /// from a card of <paramref name="sourceCardColor"/>.
        ///
        /// An undefined hero is not restricted rather than faulting, because an offer query asks this
        /// while assembling a set that may have no actor yet, and "nobody is barred" is the honest
        /// answer to a question about nobody.
        ///
        /// <para>Milestone 8.20 makes the colour a required argument rather than an optional one, for
        /// the reason milestone 8.7 required the actor scope on the declaration: Arien's Spell Break
        /// bars an activity on some of a hero's cards and not on others, so a caller that omitted the
        /// card would apply the prohibition to the very card the sentence carves out. Rulebook
        /// p.11-p.13's Hold, Clear and Fast Travel are performed from no card and pass
        /// <see cref="CardColor.None"/>, which is a statement rather than a default - and which excepts
        /// nothing, because a carve-out naming a colour cannot be met by an action that has none.</para>
        /// </summary>
        public static bool IsRestrictedFrom(
            HeroId actorId,
            ActionKind actionKind,
            RuleModificationSet modifications,
            CardColor sourceCardColor) =>
            TryGetRestriction(actorId, actionKind, modifications, sourceCardColor, out _);

        /// <summary>
        /// The restriction that bars it, for a caller that wants to name the source rather than only
        /// refuse.
        /// </summary>
        public static bool TryGetRestriction(
            HeroId actorId,
            ActionKind actionKind,
            RuleModificationSet modifications,
            CardColor sourceCardColor,
            out AppliedRuleModification restriction)
        {
            restriction = null;
            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (!actorId.IsDefined)
            {
                return false;
            }

            // The undirected question - the actor and the restricted object are one hero - filtered by
            // the two qualifications a declaration may carry. The actor half is
            // RestrictedActorScope.CoversTheAffectedObject, which is what RuleModificationSet's own
            // TryGetRestriction asks; the card half is milestone 8.20's, and it is applied here rather
            // than inside the set because this is the only asker that holds a card.
            foreach (var applied in modifications.CoveringRestrictions(
                RuleObject.ForHero(actorId), RuleActivityQuery.ForAction(actionKind)))
            {
                if (!applied.Modification.RestrictedActors.CoversTheAffectedObject) continue;
                if (applied.Modification.RestrictedSourceCards.Excepts(sourceCardColor)) continue;
                restriction = applied;
                return true;
            }

            return false;
        }
    }
}
