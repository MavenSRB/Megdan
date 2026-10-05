using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Target eligibility, which rulebook p.19 separates from presence: "Whenever your action has any
    /// effect on another unit in any way, you target that unit." Targeting applies immunity - p.18: "If
    /// a unit is immune, you cannot target that unit" - while <see cref="UnitPresenceQuery"/> is the
    /// other half of the pair and consults none, so a presence question over the same spaces still
    /// counts what this query drops.
    ///
    /// Milestone 6.8k implements the disk-shaped slice the target-selection capability needs: every
    /// board object within a rulebook distance of an origin, immunity filtered per activity. Milestone
    /// 8.8 adds the first relationship arm, <see cref="StandsInRelationTo"/>, because Wasp's two kinetic
    /// Skills print "enemy units" and a card finally needed one. The rest of the vocabulary (friendly,
    /// owner, controller, source) stays specified in <c>BOARD_CAPABILITIES.md</c> until a verified card
    /// needs it; adding it here untested would repeat the no-consumer defect 6.8c and 6.8b were
    /// scheduled to correct.
    ///
    /// Distance counts spaces including spaces with obstacles (p.18, Near/Far), so this is
    /// <see cref="HexDistanceQuery"/> geometry, never a route length. Off-board coordinates hold no
    /// object, so the disk needs no board-membership test of its own.
    /// </summary>
    public static class TargetQuery
    {
        /// <summary>Explicit grant exclusions cannot be bypassed by a printed immunity override.</summary>
        public static bool IsPrevented(RuleObject target, RuleActivity activity, RuleObject actingUnit,
            IBoardDisplacementAuthority board, MatchParticipants participants, RuleModificationSet modifications,
            IReadOnlyList<ImmunityOverrideDeclaration> overrides = null, RuleActivity overrideActivity = RuleActivity.None) =>
            modifications.ExcludesTarget(target, actingUnit) ||
            IsScreened(target, actingUnit, board, participants, modifications) ||
            ImmunityQuery.IsImmuneTo(target, activity, actingUnit,
                board, participants, modifications, overrides ?? Array.Empty<ImmunityOverrideDeclaration>(),
                overrideActivity == RuleActivity.None ? activity : overrideActivity);

        /// <summary>
        /// Milestone 8.61a. Whether a token standing between the two of them stops this hero targeting
        /// this unit - Min the Dragonmonk's Smoke Bomb, "enemy heroes cannot target you or another
        /// unit if the smoke bomb token is on a straight line between that enemy hero and their
        /// target".
        ///
        /// <para><b>It is here, beside immunity and the borrowed-Skill exclusion, because this method
        /// is where p.19's Target already has its one owner.</b> That page defines targeting as broadly
        /// as it can - "Whenever your action has any effect on another unit in any way, you target that
        /// unit" - and every attack target, push subject, swap subject and card-text unit choice in
        /// this engine reaches that definition through this call. Screening them anywhere else would
        /// have meant screening them in one place and forgetting them in the next.</para>
        ///
        /// <para><b>Four conditions, and each is one printed word.</b> "Enemy <em>heroes</em>" makes an
        /// acting minion no screen's business, which is also what leaves p.8's Minion Battle untouched -
        /// nothing there is a hero performing an action. "<em>Enemy</em>" is measured against the side
        /// that placed the token, so Min's own team targets through her smoke freely. "You or another
        /// <em>unit</em>" is p.19's units, heroes and minions, which is why the token itself stays
        /// targetable - a token reaches this method as <see cref="RuleObject.None"/> and a space never
        /// reaches it at all. And "on a straight line <em>between</em>" is
        /// <see cref="StraightLineQuery.LiesStrictlyBetween"/>, under which an adjacent pair has
        /// nothing between it.</para>
        ///
        /// <para><b>Presence is not targeting</b>, and needs no condition here to stay that way:
        /// <see cref="UnitPresenceQuery"/> is the other half of the pair this type's own remarks
        /// describe and consults no modification at all, so "if you are adjacent to a minion" is
        /// unaffected by construction.</para>
        ///
        /// <para>The board and the participants are consulted only once a screen is actually standing,
        /// so a caller that has neither - and there are such callers, asking in the abstract - answers
        /// exactly as it did before this milestone.</para>
        /// </summary>
        private static bool IsScreened(
            RuleObject target,
            RuleObject actingUnit,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            var screens = modifications.TargetingScreens;
            if (screens.Count == 0 ||
                actingUnit.Kind != RuleObjectKind.Hero ||
                board == null ||
                participants == null)
            {
                return false;
            }

            if (!TryFindSpaceOf(target, board, out var targetAt) ||
                !board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(actingUnit.HeroId), out var actorAt))
            {
                return false;
            }

            // A hero the match does not list is on no side, so no token can be its enemy - the same
            // answer StandsInRelationTo gives rather than the exception GetTeamForHero would throw.
            if (!participants.Heroes.Any(hero => hero.Id.Equals(actingUnit.HeroId)))
            {
                return false;
            }

            var actingSide = participants.GetTeamForHero(actingUnit.HeroId).Side;
            for (var index = 0; index < screens.Count; index++)
            {
                var screen = screens[index];
                if (screen.ControllerTeamSide == actingSide ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForToken(screen.AffectedObject.TokenInstanceId),
                        out var tokenAt))
                {
                    continue;
                }

                if (StraightLineQuery.LiesStrictlyBetween(actorAt, tokenAt, targetAt))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Where one p.19 unit is standing, or <c>false</c> for an object that is not one - a token,
        /// which this vocabulary can name since milestone 8.35z32, or the named absence.
        /// </summary>
        private static bool TryFindSpaceOf(
            RuleObject unit, IBoardDisplacementAuthority board, out HexCoordinate coordinate)
        {
            switch (unit.Kind)
            {
                case RuleObjectKind.Hero:
                    return board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(unit.HeroId), out coordinate);
                case RuleObjectKind.Minion:
                    return board.TryGetObjectCoordinate(
                        BoardObjectReference.ForMinion(unit.MinionId), out coordinate);
                default:
                    coordinate = default;
                    return false;
            }
        }

        /// <summary>
        /// Every board object within <paramref name="range"/> spaces of <paramref name="origin"/> that
        /// <paramref name="targetingUnit"/> may target under <paramref name="activity"/>, in canonical
        /// axial order.
        ///
        /// A token is never immunity-filtered: p.19 states tokens are not units, so the rules-object
        /// vocabulary has no token member and no immunity can be addressed to one - its
        /// <see cref="BoardObjectReference.AsRuleObject"/> is the named absence. The object standing at
        /// the origin itself - the selecting unit, when the origin is its own space - is admitted only
        /// when <paramref name="includeSelf"/> says so, because p.19's relationship vocabulary keeps
        /// self and another distinct.
        /// </summary>
        public static IReadOnlyList<BoardObjectReference> FindTargetsWithin(
            HexCoordinate origin,
            int range,
            bool includeSelf,
            RuleObject targetingUnit,
            RuleActivity activity,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications,
            IReadOnlyList<ImmunityOverrideDeclaration> immunityOverrides = null,
            RuleActivity overrideActivity = RuleActivity.None)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (range < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(range), range, "A targeting question cannot use a negative range.");
            }

            if (activity == RuleActivity.None)
            {
                throw new ArgumentException(
                    "A targeting question requires the activity doing the targeting, because p.18 " +
                    "declares immunity per activity.", nameof(activity));
            }

            var found = new List<BoardObjectReference>();
            for (var dq = -range; dq <= range; dq++)
            {
                for (var dr = Math.Max(-range, -dq - range); dr <= Math.Min(range, -dq + range); dr++)
                {
                    var coordinate = new HexCoordinate(origin.Q + dq, origin.R + dr);
                    if (!includeSelf && coordinate.Equals(origin))
                    {
                        continue;
                    }

                    if (!board.TryGetObjectAt(coordinate, out var occupant))
                    {
                        continue;
                    }

                    var asRuleObject = occupant.AsRuleObject();
                    if (asRuleObject.IsDefined && TargetQuery.IsPrevented(
                            asRuleObject,
                            activity,
                            targetingUnit,
                            board,
                            participants,
                            modifications,
                            immunityOverrides ?? Array.Empty<ImmunityOverrideDeclaration>(),
                            overrideActivity == RuleActivity.None ? activity : overrideActivity))
                    {
                        continue;
                    }

                    found.Add(occupant);
                }
            }

            return found;
        }

        /// <summary>
        /// Whether <paramref name="target"/> stands on the side of the board <paramref name="relation"/>
        /// names, measured from <paramref name="sourceHero"/>.
        ///
        /// <para>Milestone 8.8 opens the relationship half of p.19's vocabulary, and opens exactly the
        /// arm a verified card needed: Kinetic Repulse and Kinetic Blast print "up to 2 enemy units
        /// adjacent to you", so a targeting question can now say whose units it means. The wider
        /// vocabulary the capability document specifies - owner, controller, source - still waits for a
        /// card, for the reason the disk above waited: adding it with no consumer repeats the defect
        /// milestones 6.8b and 6.8c were scheduled to correct.</para>
        ///
        /// <para>It reuses <see cref="SourceTeamRelation"/> rather than minting a fourth team vocabulary.
        /// That enum already means "which side of the source a unit stands on", declared from the card's
        /// perspective so the declaration says the same thing whichever team plays it, and the pushing
        /// hero is this question's source.</para>
        ///
        /// <para>A token is on nobody's side. p.19 states that tokens are not units, so a relation that
        /// names a team excludes one, while <see cref="SourceTeamRelation.AnyTeam"/> - the declaration
        /// that names no side at all - admits it along with everything else. Milestone 8.21 adds the
        /// third answer a printed sentence needed:
        /// <see cref="SourceTeamRelation.OpposingSourceOrUnaligned"/> is "an enemy unit or a token",
        /// which admits the side-less object without also admitting the source's own units. A hero the match does not
        /// list, or a source the match does not list, is on no known side either: this answers false
        /// rather than asking <see cref="MatchParticipants.GetTeamForHero"/> a question it throws on.</para>
        /// </summary>
        public static bool StandsInRelationTo(
            SourceTeamRelation relation,
            BoardObjectReference target,
            HeroId sourceHero,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            RuleModificationSet modifications)
        {
            if (relation == SourceTeamRelation.None || !Enum.IsDefined(typeof(SourceTeamRelation), relation))
            {
                throw new ArgumentException(
                    "A targeting relation must name a side, or SourceTeamRelation.AnyTeam when it " +
                    "names none.",
                    nameof(relation));
            }

            if (relation == SourceTeamRelation.AnyTeam)
            {
                return true;
            }

            if (!sourceHero.IsDefined ||
                !TryFindTeam(BoardObjectReference.ForHero(sourceHero), participants, board,
                    modifications, out var source))
            {
                return false;
            }

            if (!TryFindTeam(target, participants, board, modifications, out var side))
            {
                // Milestone 8.21. An object on no side satisfies exactly one relation - the one whose
                // card names it beside an enemy, which is Brogan's "an enemy unit or a token". Every
                // other relation compares two sides and this object has none, which is the false the
                // paragraph above describes.
                return relation == SourceTeamRelation.OpposingSourceOrUnaligned;
            }

            switch (relation)
            {
                case SourceTeamRelation.OpposingSource:
                case SourceTeamRelation.OpposingSourceOrUnaligned:
                    return !side.Equals(source);
                case SourceTeamRelation.AlliedWithSource:
                    return side.Equals(source);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(relation), relation,
                        "A targeting relation names how the target stands to the source.");
            }
        }

        /// <summary>
        /// The team a board object belongs to. A hero's is the match's and a minion carries its own.
        /// A token ordinarily has none; only a live effective-minion overlay gives it the controller's
        /// team for this query. Absence is reported rather than compared as a default team.
        /// </summary>
        private static bool TryFindTeam(
            BoardObjectReference target,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            RuleModificationSet modifications,
            out TeamId teamId)
        {
            teamId = default;
            switch (target.Kind)
            {
                case BoardObjectKind.Hero:
                    if (participants == null ||
                        !participants.Heroes.Any(hero => hero.Id.Equals(target.HeroId)))
                    {
                        return false;
                    }

                    teamId = participants.GetTeamForHero(target.HeroId).Id;
                    return teamId.IsDefined;

                case BoardObjectKind.Minion:
                    if (board == null)
                    {
                        return false;
                    }

                    foreach (var minion in board.Minions)
                    {
                        if (minion.MinionId.Equals(target.MinionId))
                        {
                            teamId = minion.TeamId;
                            return teamId.IsDefined;
                        }
                    }

                    return false;

                case BoardObjectKind.Token:
                    if (!(board is ITokenPlacementAuthority tokens) ||
                        modifications == null ||
                        !EffectiveUnitKindQuery.Is(
                            RuleObject.ForToken(target.TokenInstanceId),
                            EffectiveUnitKind.Minion, modifications))
                        return false;
                    var placed = tokens.PlacedTokens.FirstOrDefault(candidate =>
                        candidate.Id.Equals(target.TokenInstanceId));
                    var effectiveController = EffectiveUnitKindQuery.TokenMinionController(
                        RuleObject.ForToken(target.TokenInstanceId), modifications);
                    if (placed == null || participants == null ||
                        !participants.Heroes.Any(hero =>
                            hero.Id.Equals(effectiveController)))
                        return false;
                    teamId = participants.GetTeamForHero(effectiveController).Id;
                    return teamId.IsDefined;

                default:
                    return false;
            }
        }
    }
}
