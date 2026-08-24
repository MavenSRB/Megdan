using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Whether one declaration's named actors include the actor now asking.
    ///
    /// Milestone 8.7 wrote this reading privately inside <see cref="RestrictionQuery"/>, where it was
    /// the only family that could name an actor. Milestone 8.19c's `immunity@2` is the second - Brogan's
    /// Bulwark protects against "enemy heroes" and nobody else - and the two must agree: a card that
    /// printed the same qualification on the two families and had it read differently would be two
    /// rules, not one sentence. Lifting the reading out is what makes the agreement structural rather
    /// than a duplicated method two milestones apart.
    ///
    /// The team relations are resolved against the source's controlling side, which
    /// <see cref="AppliedRuleModification.ControllerTeamSide"/> already carries for p.17's ordering, so
    /// a declaration knows whose enemies it means without naming a team.
    /// </summary>
    internal static class RestrictedActorResolution
    {
        /// <summary>
        /// An undefined actor asks the question with nobody performing the activity, which is what
        /// p.18's own corrections do - a Place the rules carry out has no acting hero - and only a
        /// declaration naming <see cref="RestrictedActorRelation.AnyActor"/> answers that one, because
        /// a qualification about who is acting cannot be met when nobody is.
        /// </summary>
        public static bool Names(
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
