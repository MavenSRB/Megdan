using System;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The one mechanical answer to whether a named actor may do a named thing to an object, which is
    /// the restriction family's directed question.
    ///
    /// <see cref="RuleModificationSet.IsRestricted"/> answers the undirected one - may this object
    /// perform this activity at all - and an offer query needs no more, because there the actor and the
    /// restricted object are the same hero. Milestone 8.7 adds this because Wasp's Magnetic Dagger is
    /// the first verified text where they differ: the restriction lands on enemy units and names the
    /// heroes who may not reposition them, so the answer depends on who is asking.
    ///
    /// It sits beside <see cref="ImmunityQuery"/> and is deliberately not folded into it. p.18 gives
    /// immunity a fixed actor rule - "You are never immune to your own actions" - while a restriction's
    /// actors are declared content; and immunity is about being targeted while a restriction is about an
    /// activity happening. A card can print either, and a consumer that asked one query for both would
    /// have to guess which of the two exceptions applied.
    /// </summary>
    public static class RestrictionQuery
    {
        /// <summary>
        /// Whether <paramref name="actor"/> is barred from <paramref name="activity"/> directed at
        /// <paramref name="affectedObject"/>.
        ///
        /// An undefined actor asks the question with nobody performing it, which is what p.18's own
        /// corrections do - a Place the rules carry out has no acting hero - and only a restriction
        /// naming <see cref="RestrictedActorRelation.AnyActor"/> answers that one, because a
        /// qualification about who is acting cannot be met when nobody is.
        /// </summary>
        public static bool IsRestricted(
            RuleObject affectedObject,
            RuleActivity activity,
            RuleObject actor,
            MatchParticipants participants,
            RuleModificationSet modifications) =>
            TryGetRestriction(affectedObject, activity, actor, participants, modifications, out _);

        /// <summary>
        /// The first restriction in canonical order that bars this actor, which is the one whose source
        /// explains the refusal.
        /// </summary>
        public static bool TryGetRestriction(
            RuleObject affectedObject,
            RuleActivity activity,
            RuleObject actor,
            MatchParticipants participants,
            RuleModificationSet modifications,
            out AppliedRuleModification restriction)
        {
            restriction = null;
            if (!affectedObject.IsDefined)
            {
                return false;
            }

            if (activity == RuleActivity.None)
            {
                throw new ArgumentException("A restriction question requires an activity.", nameof(activity));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            var covering = modifications.CoveringRestrictions(affectedObject, activity);
            for (var index = 0; index < covering.Count; index++)
            {
                if (Bars(covering[index], affectedObject, actor, participants))
                {
                    restriction = covering[index];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether one declaration's named actors include this one. The team relations are resolved
        /// against the source's controlling side, which
        /// <see cref="AppliedRuleModification.ControllerTeamSide"/> already carries for p.17's ordering
        /// - so a restriction knows whose enemies it means without the declaration naming a team.
        /// </summary>
        private static bool Bars(
            AppliedRuleModification applied,
            RuleObject affectedObject,
            RuleObject actor,
            MatchParticipants participants)
        {
            var scope = applied.Modification.RestrictedActors;
            if (scope.Covers(RestrictedActorRelation.AnyActor))
            {
                return true;
            }

            if (!actor.IsDefined)
            {
                return false;
            }

            if (scope.Covers(RestrictedActorRelation.AffectedObjectItself) && actor.Equals(affectedObject))
            {
                return true;
            }

            var opposing = scope.Covers(RestrictedActorRelation.HeroesOpposingSource);
            var allied = scope.Covers(RestrictedActorRelation.HeroesAlliedWithSource);
            if (!opposing && !allied)
            {
                return false;
            }

            // Both remaining classes name heroes, so a minion actor meets neither. The intrinsic
            // question is the right one: p.17's Counts As replaces an object's rules and qualities,
            // and which player is acting is identity rather than a quality - the hero taking the turn
            // is still the hero taking the turn while it counts as a minion.
            if (participants == null || IntrinsicKindQuery.IntrinsicUnitKind(actor) != EffectiveUnitKind.Hero)
            {
                return false;
            }

            var actingSide = participants.GetTeamForHero(actor.HeroId).Side;
            return actingSide == applied.ControllerTeamSide ? allied : opposing;
        }
    }
}
