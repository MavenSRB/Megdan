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
        /// <para><b>A point the Ultimate granted is not a destination</b>, and that is the printed
        /// silence rather than a restriction. Tide of Darkness grants "a friendly minion spawn point"
        /// and names no type; the user's D-07 ruling makes the chosen point's type decide what comes
        /// back, and p.19's count is stated per type. A typeless point answers neither question, so it
        /// counts towards every face that asks whether an empty Spawn point is there and hosts no
        /// Respawn. The alternative would be choosing a type the card does not print.</para>
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

            return EmptyMinionSpawnPointQuery
                .FindFactsInCurrentBattleZone(board, spaces, overlay)
                .Where(fact => fact.IsPrinted)
                .Where(fact => fact.TeamSide == side)
                .Where(fact => HexDistanceQuery.Calculate(origin, fact.At) <= distance)
                .Where(fact => IsTypeEligible(board, side, fact.MinionType, overlay))
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
