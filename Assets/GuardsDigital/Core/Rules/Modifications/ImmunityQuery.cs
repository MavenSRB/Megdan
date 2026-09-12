using System;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The one mechanical answer to whether an action or effect may target or affect a unit.
    /// It combines source-declared immunity with standing rulebook immunity, so offer generation and
    /// final command resolution cannot disagree and no card has to reimplement a protected unit rule.
    /// </summary>
    public static class ImmunityQuery
    {
        public static bool IsImmuneTo(
            RuleObject target,
            RuleActivity activity,
            RuleObject actingUnit,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications) =>
            IsImmuneTo(
                target,
                activity,
                actingUnit,
                board,
                participants,
                modifications,
                Array.Empty<ImmunityOverrideDeclaration>());

        /// <summary>
        /// The same immunity question with trusted, exact node declarations. A Heavy override suppresses
        /// only standing Heavy immunity for its named activity; a source-declared immunity still wins.
        /// </summary>
        public static bool IsImmuneTo(
            RuleObject target,
            RuleActivity activity,
            RuleObject actingUnit,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications,
            System.Collections.Generic.IReadOnlyList<ImmunityOverrideDeclaration> overrides) =>
            IsImmuneTo(
                target,
                activity,
                actingUnit,
                board,
                participants,
                modifications,
                overrides,
                activity);

        /// <summary>
        /// The same query when an operation asks both its enclosing activity and its own family. The
        /// override stays addressed to the exact operation while source-declared immunity for either
        /// queried activity still wins.
        /// </summary>
        public static bool IsImmuneTo(
            RuleObject target,
            RuleActivity activity,
            RuleObject actingUnit,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications,
            System.Collections.Generic.IReadOnlyList<ImmunityOverrideDeclaration> overrides,
            RuleActivity overrideActivity)
        {
            if (!target.IsDefined)
            {
                return false;
            }

            if (activity == RuleActivity.None)
            {
                throw new ArgumentException("An immunity question requires an activity.", nameof(activity));
            }

            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (overrideActivity == RuleActivity.None ||
                !Enum.IsDefined(typeof(RuleActivity), overrideActivity))
            {
                throw new ArgumentException(
                    "An immunity override query requires the exact affected activity.",
                    nameof(overrideActivity));
            }

            // Milestone 8.39b. p.18's Immune keyword is written to be overridden - "you cannot
            // target that unit ... unless the card text says otherwise" - and a card whose text says
            // so without qualifying it says so about both protections below. Cutter's Grappling Bolt
            // prints exactly that, because p.18's obstacles include units.
            var unqualified = Declares(
                overrides, overrideActivity, ImmunityOverrideReason.PrintedUnqualifiedImmunity);

            if (HasDeclaredImmunity(target, activity, actingUnit, participants, modifications))
            {
                return !unqualified;
            }

            var standingHeavy = HasStandingHeavyImmunity(
                target, board, participants, modifications);
            if (!standingHeavy)
            {
                return false;
            }

            // The Heavy arm is unchanged: a card naming p.22's rule bypasses p.22's rule and nothing
            // else, which is what its own reason has always meant.
            return !(unqualified || Declares(
                overrides, overrideActivity, ImmunityOverrideReason.PrintedHeavyMinionImmunity));
        }

        private static bool Declares(
            System.Collections.Generic.IReadOnlyList<ImmunityOverrideDeclaration> overrides,
            RuleActivity overrideActivity,
            ImmunityOverrideReason reason) =>
            (overrides ?? Array.Empty<ImmunityOverrideDeclaration>()).Any(value =>
                value.Activity == overrideActivity && value.Reason == reason);

        /// <summary>
        /// Whether a source has declared this target immune to this actor's activity.
        ///
        /// Milestone 8.19c's `immunity@2` lets that declaration name the actors it protects against -
        /// Brogan's Bulwark protects against "enemy heroes" and against nobody else - so the answer
        /// stopped being a property of the target alone. The actor classes are read by
        /// <see cref="RestrictedActorResolution"/>, the same reading a restriction's actors get, and
        /// p.18's fixed exception is applied first: an actor acting on itself is never immune to
        /// itself, whatever any declaration says.
        /// </summary>
        private static bool HasDeclaredImmunity(
            RuleObject target,
            RuleActivity activity,
            RuleObject actingUnit,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (actingUnit.IsDefined && actingUnit.Equals(target))
            {
                return false;
            }

            var covering = modifications.CoveringImmunities(target, activity);
            for (var index = 0; index < covering.Count; index++)
            {
                if (RestrictedActorResolution.Names(covering[index], target, actingUnit, participants))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasStandingHeavyImmunity(
            RuleObject target,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (!IntrinsicKindQuery.IsMinionMiniature(target))
            {
                return false;
            }

            var heavy = board.Minions.SingleOrDefault(minion => minion.MinionId.Equals(target.MinionId));
            if (heavy == null || heavy.MinionType != StandardMinionType.Heavy ||
                !BattleZoneQuery.IsInCurrentBattleZone(board, heavy.OccupiedSpace.SpaceId))
            {
                return false;
            }

            return BattleZoneQuery.FindEffectiveMinionsInside(board, participants, modifications)
                .Any(candidate => Accompanies(candidate, target, heavy.TeamSide));
        }

        /// <summary>
        /// Whether one member of the Battle Zone's effective minion set accompanies the Heavy.
        ///
        /// Milestone 8.7 closes the 7.9a review's F-BOARD-04 here. The set itself now comes from
        /// <see cref="BattleZoneQuery.FindEffectiveMinionsInside"/> - the one derivation the Minion
        /// Battle and p.8's last-minion Lane-Push cause already count - rather than from a second
        /// zone walk of this query's own. The three conditions below are all that was ever local to
        /// p.13's clause: not the Heavy itself, not another Heavy, and standing on the Heavy's side.
        ///
        /// The side comparison replaces a <c>TeamId</c> one. The shared set reports the side each
        /// unit belongs to, and a match has exactly one team per side, so the two are the same
        /// question asked of the discriminator the shared set publishes.
        ///
        /// One reading moved with the derivation and is recorded rather than smuggled: the old local
        /// walk asked the effective-type question of minion miniatures too, so a miniature counting
        /// as a hero stopped accompanying, while the shared set takes every miniature in the zone as
        /// intrinsic. Adopting the shared reading makes that answer wrong in one place instead of
        /// disagreeing in two, which is the whole of the finding; no source declares
        /// <see cref="EffectiveUnitKind.Hero"/> today, so nothing observable changes.
        /// </summary>
        private static bool Accompanies(
            EffectiveBattleZoneMinion candidate, RuleObject heavy, TeamSide heavySide) =>
            !candidate.Unit.Equals(heavy) && !candidate.IsHeavy && candidate.TeamSide == heavySide;
    }
}
