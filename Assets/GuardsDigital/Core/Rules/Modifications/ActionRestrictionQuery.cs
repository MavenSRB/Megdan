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
        /// Whether <paramref name="actorId"/> may not perform <paramref name="actionKind"/> right now.
        ///
        /// An undefined hero is not restricted rather than faulting, because an offer query asks this
        /// while assembling a set that may have no actor yet, and "nobody is barred" is the honest
        /// answer to a question about nobody.
        /// </summary>
        public static bool IsRestrictedFrom(
            HeroId actorId, ActionKind actionKind, RuleModificationSet modifications)
        {
            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (!actorId.IsDefined)
            {
                return false;
            }

            return modifications.IsRestricted(
                RuleObject.ForHero(actorId), RuleActivityQuery.ForAction(actionKind));
        }

        /// <summary>
        /// The restriction that bars it, for a caller that wants to name the source rather than only
        /// refuse.
        /// </summary>
        public static bool TryGetRestriction(
            HeroId actorId,
            ActionKind actionKind,
            RuleModificationSet modifications,
            out AppliedRuleModification restriction)
        {
            restriction = null;
            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            return actorId.IsDefined && modifications.TryGetRestriction(
                RuleObject.ForHero(actorId), RuleActivityQuery.ForAction(actionKind), out restriction);
        }
    }
}
