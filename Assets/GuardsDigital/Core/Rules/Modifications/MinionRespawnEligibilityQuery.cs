using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Rulebook p.19's Respawn eligibility, stated as printed. Milestone 8.35z28.
    ///
    /// <para>p.19 <em>Respawn</em>: "place the minion miniature on the board into an empty space. You
    /// can only respawn a minion if there are <b>more minion spawn points of that type and color (empty
    /// or not), than there are minion miniatures of that type and color present in that Battle
    /// Zone.</b> (To put it simply, a minion must first be removed, before it can be respawned)."</para>
    ///
    /// <para><b>Why this exists now.</b> <see cref="IUnitSpawnAuthority"/> has said since milestone 6.5
    /// that this rule "stays unimplemented and belongs with hero and minion Respawn, so nothing here
    /// may be read as supplying it". Dodger the Warlock's Necromancy and Necromastery are the first
    /// verified faces that respawn from card text, so the rule lands here rather than inside either
    /// card - it is one page's rule and two cards read it.</para>
    ///
    /// <para><b>The Heavy exclusion is derived, not written down.</b> The user's 2026-09-06 ruling on
    /// D-07 records the derivation rather than an added restriction: p.13 makes a Heavy minion immune
    /// while any other friendly minion stands, so a Heavy is removed only as the last minion of its
    /// side in the Zone; that removal is p.8's Lane-Push cause, which moves the Battle Zone and
    /// respawns the whole wave. The count below therefore never favours a Heavy, and the ruling is
    /// explicit that the exclusion must fall out of this arithmetic rather than be hard-coded.
    /// <c>DodgerNecromancyTests</c> proves it against the shipped configuration instead of asserting a
    /// special case here - a special case would make the test tautological.</para>
    ///
    /// <para><b>"That Battle Zone" is the current one, and Dodger's Ultimate can widen it.</b> Both
    /// counts are taken over the same zone, so an overlay that makes every space count as being in the
    /// Battle Zone widens the points and the miniatures together. That is the honest consequence of
    /// Tide of Darkness rather than a special rule: the page's comparison is between two populations of
    /// one zone, and the card changes which spaces that zone contains.</para>
    /// </summary>
    public static class MinionRespawnEligibilityQuery
    {
        /// <summary>
        /// Whether p.19 lets a minion of this type and colour come back right now: strictly more Spawn
        /// points of that type and colour in the Battle Zone, empty or not, than miniatures of that
        /// type and colour standing in it.
        /// </summary>
        public static bool IsTypeEligible(
            IBoardDisplacementAuthority board,
            TeamSide side,
            StandardMinionType minionType,
            SpawnPointOverlay overlay)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            if (side == TeamSide.None || minionType == StandardMinionType.None)
            {
                // A point that names no side or no type - which is every point Tide of Darkness grants
                // - poses p.19 no question it can answer. See EmptyMinionSpawnPointFact.
                return false;
            }

            return CountSpawnPoints(board, side, minionType, overlay) >
                CountMiniaturesPresent(board, side, minionType, overlay);
        }

        /// <summary>
        /// "an empty friendly spawn point [adjacent to you / in radius] in the battle zone" that a
        /// minion may actually be respawned into, given p.19's count.
        ///
        /// <para>The qualifying set comes from <see cref="EmptyMinionSpawnPointQuery"/> and is not
        /// re-derived: the printed phrase these two cards share with the other nine has one reading,
        /// and this method adds only the two words those nine do not print - "friendly", which p.19
        /// makes a statement about sides, and the eligibility p.19 attaches to a Respawn.</para>
        ///
        /// <para><b>A point the Ultimate granted IS a destination - RULING 48 (2026-09-06), which
        /// overrules the reading milestone 8.35z28 shipped.</b> The user settled it in one sentence,
        /// "Respawn anywhere in reach": post-Ultimate she may respawn into any empty space in the
        /// card's reach, because the Ultimate's whole content is that every space has a friendly minion
        /// Spawn point and p.19 asks nothing more of a destination than that. What the earlier reading
        /// got right is that a granted point names no type; what it got wrong is concluding that the
        /// question is therefore unanswerable. p.19 answers it from the other side: the page's own
        /// eligibility count already says which types may come back at all, so a typeless destination
        /// takes its type from <see cref="EligibleTypesFor"/> instead of from the point. Heavies stay
        /// structurally excluded because that count never favours one - RULING 12's derivation is
        /// untouched - and where more than one type is eligible, p.10's default gives the choice to the
        /// actor, which the handler asks as its second question. At a REAL printed point, RULING 12's
        /// destination-fixes-type still governs and this method still filters on the point's own
        /// type.</para>
        /// </summary>
        public static IReadOnlyList<EmptyMinionSpawnPointFact> FindRespawnDestinationsWithin(
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            TeamSide side,
            HexCoordinate origin,
            int distance,
            SpawnPointOverlay overlay)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));
            if (distance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(distance), distance,
                    "p.9's disk is inclusive and cannot have a negative radius.");
            }

            if (side == TeamSide.None)
            {
                return Array.Empty<EmptyMinionSpawnPointFact>();
            }

            // Computed once rather than per candidate space: the count is a fact about the Zone, and
            // every granted point in it reads the same answer.
            var eligibleAtATypelessPoint = EligibleTypesFor(board, side, overlay);

            var found = new List<EmptyMinionSpawnPointFact>();
            foreach (var fact in EmptyMinionSpawnPointQuery
                         .FindFactsInCurrentBattleZone(board, spaces, overlay))
            {
                if (HexDistanceQuery.Calculate(origin, fact.At) > distance)
                {
                    continue;
                }

                // A real printed friendly point, which RULING 48 leaves exactly as RULING 12 left it:
                // the destination fixes what comes back, so the point is a destination only while its
                // own type may come back at all.
                if (fact.IsPrinted && fact.TeamSide == side)
                {
                    if (IsTypeEligible(board, side, fact.MinionType, overlay))
                    {
                        found.Add(fact);
                    }

                    continue;
                }

                // Everything else is a destination only through the Ultimate's grant, and is typeless
                // whatever the map prints on it. Two spaces reach here: one the map prints nothing on,
                // and one printing an ENEMY point - and the second is a destination for the same reason
                // the first is, because the card grants a friendly point on ALL spaces and says nothing
                // about what a space already carries. Excluding it would be an unprinted restriction.
                //
                // Note what does NOT reach here: an empty friendly point whose own type is ineligible.
                // RULING 48 names the two readings as alternatives - "at an untyped granted point ...
                // at a REAL printed spawn point" - so a space with a printed friendly point is governed
                // by that point, which is also what keeps one space to one destination and the offer
                // unambiguous.
                if (!overlay.IsActive || eligibleAtATypelessPoint.Count == 0)
                {
                    continue;
                }

                found.Add(new EmptyMinionSpawnPointFact(
                    default, fact.At, side, StandardMinionType.None, isPrinted: false));
            }

            return found;
        }

        /// <summary>
        /// The minion types p.19 lets this side bring back into the Battle Zone right now - what a
        /// destination that names no type of its own respawns. RULING 48.
        ///
        /// <para>It is the same comparison <see cref="IsTypeEligible"/> makes, asked of every type
        /// instead of one, and it is deliberately stated that way rather than as a list of "types
        /// Dodger may respawn": the page's count is the only rule in play, so a Heavy is absent from
        /// this list exactly when the arithmetic says so and never because this method was told to leave
        /// it out. <c>DodgerNecromancyTests</c> proves the Heavy's absence against the shipped
        /// configuration, which is only evidence while the exclusion is derived here rather than
        /// written.</para>
        ///
        /// <para>Ordered by the enum's own order so that the candidate list a player is offered is
        /// stable across runs, restores and replays - the same reason
        /// <see cref="EmptyMinionSpawnPointQuery.FindFactsInCurrentBattleZone"/> sorts its facts.</para>
        /// </summary>
        public static IReadOnlyList<StandardMinionType> EligibleTypesFor(
            IBoardDisplacementAuthority board,
            TeamSide side,
            SpawnPointOverlay overlay)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            if (side == TeamSide.None)
            {
                return Array.Empty<StandardMinionType>();
            }

            return new[] { StandardMinionType.Melee, StandardMinionType.Ranged, StandardMinionType.Heavy }
                .Where(type => IsTypeEligible(board, side, type, overlay))
                .ToArray();
        }

        /// <summary>
        /// The Spawn points of one type and colour that the Battle Zone holds, "empty or not" - p.19's
        /// own parenthesis, which is why this counts the map and never the board.
        /// </summary>
        private static int CountSpawnPoints(
            IBoardDisplacementAuthority board,
            TeamSide side,
            StandardMinionType minionType,
            SpawnPointOverlay overlay) =>
            board.MapDefinition.SpawnPoints.Count(point =>
                LanePush.LanePushSystemTransition.TryReadMinionType(point.UnitType, out var type) &&
                type == minionType &&
                LanePush.LanePushSystemTransition.ReadTeamSide(point.TeamId) == side &&
                (overlay.IsActive || BattleZoneQuery.IsInCurrentBattleZone(board, point.SpaceId)));

        /// <summary>
        /// The miniatures of one type and colour "present in that Battle Zone" - the board, and the
        /// intrinsic miniature set rather than any effective-type reading, because p.19 is counting
        /// miniatures against the Spawn points that hold them.
        /// </summary>
        private static int CountMiniaturesPresent(
            IBoardDisplacementAuthority board,
            TeamSide side,
            StandardMinionType minionType,
            SpawnPointOverlay overlay) =>
            board.Minions.Count(minion =>
                minion.MinionType == minionType &&
                minion.TeamSide == side &&
                (overlay.IsActive ||
                    BattleZoneQuery.IsInCurrentBattleZone(board, minion.OccupiedSpace.SpaceId)));
    }
}
