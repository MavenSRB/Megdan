using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The one statement of "an empty spawn point in the battle zone" - rulebook p.19's term, read as
    /// the user's 2026-09-06 ruling settles it (Dodger D-01): <b>minion</b> spawn points only.
    ///
    /// <para><b>Why one query and not a clause per card.</b> Nine of Dodger the Warlock's nineteen
    /// printed faces condition on this quantity, and they consume it three different ways - a value
    /// contribution on three Blue Defense primaries, a counted gate inside three Green rituals, an
    /// adjacency test on her Gold basics, and a candidate narrowing on her Silver basic. Four
    /// derivations of one printed phrase is precisely the two-locally-correct-readings defect milestone
    /// 6.5ad was written to correct, so the phrase is derived here and asked from there.</para>
    ///
    /// <para><b>What makes a point qualify, in the three parts the sentence has.</b>
    /// <list type="number">
    /// <item><description><b>A minion spawn point.</b> p.19 defines a Spawn Point as "Spaces on the
    /// board used to spawn minions <em>and heroes</em>", so the term is broader than the ruling. The
    /// rulebook's own worked example of one of these very cards counts the narrower set - printed p.14,
    /// on Vampiric Shield: "giving Dodger +2 Defense for the 2 empty <em>minion</em> spawn points within
    /// a radius of 2 spaces from her space" - and the user ruled for that reading. Which map unit types
    /// are minion spawn points is <see cref="LanePushSystemTransition.TryReadMinionType"/>'s existing
    /// answer, asked rather than restated: p.8's Lane-Push respawn already had to know, and two lists
    /// could come to disagree about a map that added a type.</description></item>
    /// <item><description><b>In the current Battle Zone.</b> <see cref="BattleZoneQuery"/>'s zone,
    /// which p.8 moves along the Lane, and never the map's starting flag - a reading that kept the flag
    /// would answer about the Zone the match began in for the rest of the match.</description></item>
    /// <item><description><b>Empty.</b> p.19: "An empty Spawn point is a Spawn point in an empty space",
    /// and p.18 makes any occupant an obstacle. That question is
    /// <see cref="ITokenPlacementAuthority.IsEmptyPlayableSpace"/>, which is the same emptiness Arien's
    /// three Green cards already ask about the identical printed phrase ("not adjacent to an empty spawn
    /// point", milestone 8.20). One phrase, one derivation.</description></item>
    /// </list></para>
    ///
    /// <para><b>The zone is asked of the spawn point and never of the hero measuring.</b> This is the
    /// designer's own ruling, captured 2026-07-25 in the official "rules-questions-goa2" Discord and
    /// recorded in the roster-expansion rulings: asked whether Dodger can be outside the Battle Zone and
    /// still get Aegis of Doom's +4 as long as the empty spawns are in the Battle Zone, the designer
    /// answered "Sure, why not." So the condition is <em>zone-anchored</em>, not self-anchored, and no
    /// method here takes or tests the measuring hero's own membership. The disk is still measured from
    /// wherever they stand, which is what p.14's worked example means by "from her space".</para>
    ///
    /// <para><b>Where the overlay will go.</b> Her Ultimate, Tide of Darkness, prints "all spaces count
    /// as if they were in the battle zone and had a friendly minion spawn point", and whether its grant
    /// satisfies <em>empty</em> is open question D-03. It is not composed here and no overlay kind is
    /// invented for it. What this query owes that milestone is structural rather than speculative: every
    /// one of the three parts above is answered in exactly one place inside
    /// <see cref="Qualifies"/>, so the Ultimate widens one method rather than four call sites.</para>
    /// </summary>
    public static class EmptyMinionSpawnPointQuery
    {
        /// <summary>
        /// Every qualifying point on the board right now, in canonical axial order.
        ///
        /// <para>Derived on every call rather than stored, exactly as
        /// <see cref="IUnitSpawnAuthority.FindSpawnPointSpaces"/> is: emptiness is live board state and
        /// a remembered set is a second thing that can disagree with the board. A minion removed from a
        /// spawn point makes it qualify on the very next question, which is the live reading the cards'
        /// silence about timing asks for.</para>
        /// </summary>
        public static IReadOnlyList<HexCoordinate> FindInCurrentBattleZone(
            IBoardDisplacementAuthority board, ITokenPlacementAuthority spaces) =>
            FindInCurrentBattleZone(board, spaces, SpawnPointOverlay.None);

        /// <summary>
        /// The same set, with Dodger's Ultimate optionally rewriting what the board counts as.
        /// Milestone 8.35z28.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> FindInCurrentBattleZone(
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            SpawnPointOverlay overlay) =>
            FindFactsInCurrentBattleZone(board, spaces, overlay)
                .Select(fact => fact.At)
                .Distinct()
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R)
                .ToArray();

        /// <summary>
        /// Every qualifying point with the two facts a Respawn needs and a count does not - whose side
        /// it belongs to and which minion type it spawns. Milestone 8.35z28.
        ///
        /// <para><b>This is the one derivation, and the coordinate form above is a projection of it.</b>
        /// Necromancy and Necromastery ask the same printed question the nine counting faces ask and
        /// then ask two more of the point they picked; a second traversal that re-decided which points
        /// qualify would be the two-readings defect this class was written to prevent.</para>
        ///
        /// <para><b>How the Ultimate widens it, in the two places it touches.</b> Tide of Darkness makes
        /// every space count as being in the Battle Zone, so the zone test passes everywhere; and it
        /// grants every space a friendly minion Spawn point, so a space the map prints no point on
        /// still yields a fact. A space the map <em>does</em> print a point on keeps that point's own
        /// side and type - the card adds a Spawn point where there was none and has no reason to
        /// overwrite one that exists, and keeping the printed one is what lets a Respawn under the
        /// Ultimate still know what comes back.</para>
        /// </summary>
        public static IReadOnlyList<EmptyMinionSpawnPointFact> FindFactsInCurrentBattleZone(
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            SpawnPointOverlay overlay)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));

            if (!board.CurrentBattleZoneId.HasValue && !overlay.IsActive)
            {
                // A board with no Battle Zone - every synthetic map - has no space that could be "in
                // the battle zone", so the answer is none rather than everything. Refusing to throw
                // matters: a Defense total is computed on whatever board the match is running on, and
                // a card whose condition cannot be satisfied simply is not satisfied. The Ultimate is
                // the one thing that makes the question answerable anyway, because it stops asking the
                // board where its Battle Zone is.
                return Array.Empty<EmptyMinionSpawnPointFact>();
            }

            var printedPoints = board.MapDefinition.SpawnPoints
                .Where(point => LanePushSystemTransition.TryReadMinionType(point.UnitType, out _))
                .ToLookup(point => point.SpaceId);

            var found = new List<EmptyMinionSpawnPointFact>();
            foreach (var space in board.MapDefinition.Spaces)
            {
                if (space.Kind != BoardMapSpaceKind.Playable)
                {
                    continue;
                }

                // Part three, hoisted: p.19's "An empty Spawn point is a Spawn point in an empty
                // space", and p.18 makes any occupant an obstacle. Asked first because it is the one
                // part of the sentence the Ultimate never touches - the card supplies Spawn points and
                // Battle-Zone membership, and says nothing whatever about emptiness.
                if (!spaces.IsEmptyPlayableSpace(space.Coordinate))
                {
                    continue;
                }

                // Part two: in the current Battle Zone, asked of the point and never of whoever is
                // measuring - the designer's own 2026-07-25 ruling. The Ultimate makes it true
                // everywhere.
                if (!overlay.IsActive &&
                    !BattleZoneQuery.IsInCurrentBattleZone(board, space.Id))
                {
                    continue;
                }

                // Part one: a minion Spawn point, printed by the map or granted by the Ultimate.
                var printed = printedPoints[space.Id].ToArray();
                if (printed.Length > 0)
                {
                    foreach (var point in printed)
                    {
                        LanePushSystemTransition.TryReadMinionType(point.UnitType, out var minionType);
                        found.Add(new EmptyMinionSpawnPointFact(
                            point.Id,
                            space.Coordinate,
                            LanePushSystemTransition.ReadTeamSide(point.TeamId),
                            minionType,
                            isPrinted: true));
                    }

                    continue;
                }

                if (overlay.IsActive)
                {
                    found.Add(new EmptyMinionSpawnPointFact(
                        default,
                        space.Coordinate,
                        overlay.FriendlySide,
                        StandardMinionType.None,
                        isPrinted: false));
                }
            }

            return found
                .OrderBy(fact => fact.At.Q)
                .ThenBy(fact => fact.At.R)
                .ThenBy(fact => fact.SpawnPointId.Value ?? string.Empty, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// How many qualifying points lie within <paramref name="distance"/> of
        /// <paramref name="origin"/>, counting p.9's disk inclusively.
        ///
        /// <para>This is what "2 or more empty spawn points in radius in the battle zone" is measured
        /// against on the three Blue Defense primaries and the three Green rituals. The number counted
        /// is <em>points</em> rather than spaces: two spawn points can never share a space on a valid
        /// map, so the two counts coincide, and counting points is what the sentence says.</para>
        /// </summary>
        /// <param name="distance">
        /// p.9's radius, already composed with rulebook p.16's matching item by the caller that holds
        /// the printed icon. A distance of one is the innermost ring, which is what "adjacent to" means.
        /// </param>
        public static int CountWithin(
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            int distance) =>
            CountWithin(board, spaces, origin, distance, SpawnPointOverlay.None);

        /// <summary>
        /// The same count, with Dodger's Ultimate optionally up. Milestone 8.35z28: with it up,
        /// the user's D-03 ruling makes "2 or more empty spawn points in radius in the battle zone"
        /// read "2 or more empty spaces in radius", which is the master switch behind nine faces.
        /// </summary>
        public static int CountWithin(
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            int distance,
            SpawnPointOverlay overlay)
        {
            if (distance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(distance),
                    distance,
                    "p.9's disk is inclusive and cannot have a negative radius.");
            }

            return FindInCurrentBattleZone(board, spaces, overlay)
                .Count(point => HexDistanceQuery.Calculate(origin, point) <= distance);
        }

        /// <summary>
        /// Whether at least one qualifying point lies within <paramref name="distance"/> - the same
        /// question as <see cref="CountWithin"/> reaching one, stated separately because that is the
        /// whole of what "If you are adjacent to an empty spawn point in the battlezone" asks and a
        /// consumer that wanted a boolean should not have to know a threshold vocabulary.
        /// </summary>
        public static bool AnyWithin(
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            int distance) =>
            CountWithin(board, spaces, origin, distance) > 0;

        /// <summary>The same question with the Ultimate optionally up. Milestone 8.35z28.</summary>
        public static bool AnyWithin(
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            int distance,
            SpawnPointOverlay overlay) =>
            CountWithin(board, spaces, origin, distance, overlay) > 0;
    }
}
