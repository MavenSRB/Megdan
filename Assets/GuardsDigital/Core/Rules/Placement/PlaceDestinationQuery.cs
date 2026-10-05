using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;

namespace GuardsDigital.Core.Rules.Placement
{
    /// <summary>
    /// The destinations a declared <see cref="PlacePolicy"/> admits, in canonical axial order.
    ///
    /// Nothing here walks a path. Rulebook p.18 states that placing "does not count as movement", so a
    /// Place asks a geometric or set-membership question and never a reachability one - which is also
    /// why a piece can be placed somewhere it could not walk to. Whether a space is available is
    /// <see cref="ObstacleQuery"/>'s answer since milestone 8.7, and it was the board's own
    /// <see cref="ITokenPlacementAuthority.IsEmptyPlayableSpace"/> before that; either way this query
    /// never re-derives p.19's one-obstacle-per-space rule or milestone 6.8e's terrain classification.
    ///
    /// The move to the shared query is what closes 8.7's third acceptance line. A conditional obstacle
    /// overlay makes spaces count as obstacles, and p.18 lists obstacles once for every rule that
    /// consults them - so a space a barrier closed has to be closed to a Place as much as to a walk or
    /// a push. Asking one query is what makes that true by construction rather than by three
    /// algorithms remembering to agree.
    ///
    /// The "different space" requirement is p.18's own wording - "put it into a different space" - and
    /// it is enforced here rather than at the commit, because a space the piece already occupies is
    /// not a legal candidate to offer in the first place.
    /// </summary>
    public static class PlaceDestinationQuery
    {
        /// <summary>
        /// The legal destinations for one Place.
        /// </summary>
        /// <param name="policy">The declared operation. Its scope selects the question asked.</param>
        /// <param name="spaces">The board's empty-and-playable answer.</param>
        /// <param name="origin">
        /// The space the question is measured from: the piece's own space for a board Place, and the
        /// space the invoking rule measures from for a supply Place.
        /// </param>
        /// <param name="scopedCoordinates">
        /// The coordinate set a <see cref="PlaceDestinationScope.NearestEmptySpacesInScope"/> is
        /// measured over - for the after-action correction, the starting Battle Zone's own spaces.
        /// A radius scope derives its own candidates and ignores this.
        /// </param>
        /// <param name="spawnPoints">
        /// The board's Spawn port, required exactly when the policy declares a
        /// <see cref="PlaceSpawnPointExclusion"/> and ignored otherwise. It is a required argument for
        /// such a policy rather than an optional one that silently widens the offer: a Place that
        /// carved out Spawn points and was asked without them would admit the very spaces Arien's
        /// three Green cards refuse, and nothing downstream could tell.
        /// </param>
        /// <param name="board">
        /// The board, required exactly when the policy declares one of the two Battle-Zone clauses
        /// milestone 8.35z33 added and ignored otherwise. It is required for the same reason
        /// <paramref name="spawnPoints"/> is: those clauses ask which Spawn points are <em>minion</em>
        /// points and which lie in the <em>current</em> Battle Zone, and both questions are the map's
        /// and the Lane's rather than the token port's.
        /// </param>
        public static IReadOnlyList<HexCoordinate> FindDestinations(
            PlacePolicy policy,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            IReadOnlyList<HexCoordinate> scopedCoordinates,
            RuleModificationSet modifications,
            IUnitSpawnAuthority spawnPoints = null,
            IBoardDisplacementAuthority board = null,
            RuleObject placedPiece = default)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (NamesTheSpawnPort(policy.SpawnPointExclusion) && spawnPoints == null)
            {
                throw new ArgumentNullException(
                    nameof(spawnPoints),
                    "A Place that excludes Spawn-point spaces is asked with the port that knows " +
                    "where they are.");
            }

            if (NamesTheBattleZone(policy.SpawnPointExclusion) && board == null)
            {
                throw new ArgumentNullException(
                    nameof(board),
                    "A Place that admits only the Battle Zone's empty minion Spawn points is asked " +
                    "with the board that knows which points those are.");
            }

            switch (policy.DestinationScope)
            {
                case PlaceDestinationScope.EmptySpacesWithinRadius:
                    return ExcludingSpawnPoints(
                        VisibleToActingUnit(
                            WithinRadius(policy.DestinationRadius, spaces, origin, modifications, placedPiece),
                            modifications),
                        policy.SpawnPointExclusion,
                        spaces,
                        spawnPoints,
                        board);

                case PlaceDestinationScope.EmptySpacesAtExactlyRadius:
                    return ExcludingSpawnPoints(
                        VisibleToActingUnit(
                            AtExactlyRadius(policy.DestinationRadius, spaces, origin, modifications, placedPiece),
                            modifications),
                        policy.SpawnPointExclusion,
                        spaces,
                        spawnPoints,
                        board);

                case PlaceDestinationScope.EmptySpacesWithinRadiusAndInScope:
                    if (scopedCoordinates == null)
                    {
                        throw new ArgumentNullException(
                            nameof(scopedCoordinates),
                            "A qualified Place is intersected with the coordinate set its caller " +
                            "derived; an absent set is a missing question rather than an empty one.");
                    }

                    return ExcludingSpawnPoints(
                        WithinRadius(policy.DestinationRadius, spaces, origin, modifications, placedPiece)
                            .Where(candidate => scopedCoordinates.Contains(candidate))
                            .Where(candidate => !modifications.BlocksCurrentActorSpaceInteraction(candidate))
                            .ToArray(),
                        policy.SpawnPointExclusion,
                        spaces,
                        spawnPoints,
                        board);

                case PlaceDestinationScope.NearestEmptySpacesInScope:
                    if (scopedCoordinates == null)
                    {
                        throw new ArgumentNullException(
                            nameof(scopedCoordinates),
                            "A nearest-in-scope Place is measured over a declared coordinate set.");
                    }

                    return NearestEmptyInScope(spaces, origin,
                        VisibleToActingUnit(scopedCoordinates, modifications),
                        modifications, placedPiece);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(policy),
                        policy.DestinationScope,
                        "A Place destination scope must be one this query answers.");
            }
        }

        private static IReadOnlyList<HexCoordinate> VisibleToActingUnit(
            IEnumerable<HexCoordinate> candidates, RuleModificationSet modifications) =>
            modifications.BoardPartitions.Count == 0
                ? candidates.ToArray()
                : candidates.Where(candidate =>
                    !modifications.BlocksCurrentActorSpaceInteraction(candidate)).ToArray();

        /// <summary>
        /// Every empty playable space at an inclusive rulebook distance of the origin, minus the origin
        /// itself. The disk is enumerated in axial coordinates the way <c>TargetQuery</c> enumerates
        /// its own, because both ask p.18's distance question rather than a route one.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> WithinRadius(
            int radius,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            RuleModificationSet modifications,
            RuleObject placedPiece)
        {
            var found = new List<HexCoordinate>();
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    var coordinate = new HexCoordinate(origin.Q + dq, origin.R + dr);
                    if (coordinate.Equals(origin) ||
                        ObstacleQuery.BlocksPlacement(spaces, coordinate, modifications, placedPiece))
                    {
                        continue;
                    }

                    found.Add(coordinate);
                }
            }

            return found
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R)
                .ToArray();
        }

        /// <summary>
        /// Milestone 8.35z29. Every empty playable space at <em>exactly</em> the declared distance -
        /// the ring the disk above draws the interior of.
        ///
        /// <para>It is written as the same enumeration with one predicate changed rather than as a
        /// walk around the ring, so that the two scopes cannot come to disagree about what p.18's
        /// distance is or about which spaces an obstacle overlay closes. The origin is excluded by
        /// arithmetic here rather than by a test: a radius of zero is refused by
        /// <see cref="PlacePolicy"/>, so the origin is never at the declared distance.</para>
        /// </summary>
        private static IReadOnlyList<HexCoordinate> AtExactlyRadius(
            int radius,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            RuleModificationSet modifications,
            RuleObject placedPiece)
        {
            var found = new List<HexCoordinate>();
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    var coordinate = new HexCoordinate(origin.Q + dq, origin.R + dr);
                    if (HexDistanceQuery.Calculate(origin, coordinate) != radius ||
                        ObstacleQuery.BlocksPlacement(spaces, coordinate, modifications, placedPiece))
                    {
                        continue;
                    }

                    found.Add(coordinate);
                }
            }

            return found
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R)
                .ToArray();
        }

        /// <summary>
        /// The declared Spawn-point clauses applied to a derived candidate set. Milestone 8.20.
        ///
        /// <para>Both clauses are map questions and neither is a reachability one, so they filter the
        /// disk rather than changing how it was drawn - which is what keeps p.18's "does not count as
        /// movement" true of a card that names Spawn points. "Empty" is asked of the Spawn point's own
        /// space through the same <see cref="ITokenPlacementAuthority.IsEmptyPlayableSpace"/> every
        /// other emptiness question in this build goes through, so a Spawn point with a minion standing
        /// on it stops excluding its neighbours the moment the minion leaves - which is the live
        /// reading, the card naming no timing.</para>
        /// </summary>
        private static IReadOnlyList<HexCoordinate> ExcludingSpawnPoints(
            IReadOnlyList<HexCoordinate> candidates,
            PlaceSpawnPointExclusion exclusion,
            ITokenPlacementAuthority spaces,
            IUnitSpawnAuthority spawnPoints,
            IBoardDisplacementAuthority board)
        {
            if (exclusion == PlaceSpawnPointExclusion.None)
            {
                return candidates;
            }

            if (NamesTheBattleZone(exclusion))
            {
                return AdmittingOnlyBattleZoneMinionSpawnPoints(candidates, exclusion, spaces, board);
            }

            var points = spawnPoints.FindSpawnPointSpaces();
            var empty = exclusion == PlaceSpawnPointExclusion.SpacesWithASpawnPointOrAdjacentToAnEmptyOne
                ? points.Where(spaces.IsEmptyPlayableSpace).ToArray()
                : Array.Empty<HexCoordinate>();

            return candidates
                .Where(candidate => !points.Any(point => point.Equals(candidate)))
                .Where(candidate => empty.Length == 0 ||
                    !empty.Any(point => HexDistanceQuery.Calculate(point, candidate) == 1))
                .ToArray();
        }

        /// <summary>
        /// Whether an exclusion needs the Spawn port to be answered. Milestone 8.35z33's two clauses do
        /// not: they ask about <em>minion</em> Spawn points in the current Battle Zone, which is a
        /// narrower question than the port's whole-board list and is derived from the map and the Lane
        /// instead.
        /// </summary>
        private static bool NamesTheSpawnPort(PlaceSpawnPointExclusion exclusion) =>
            exclusion == PlaceSpawnPointExclusion.SpacesWithASpawnPoint ||
            exclusion == PlaceSpawnPointExclusion.SpacesWithASpawnPointOrAdjacentToAnEmptyOne;

        /// <summary>Whether an exclusion is one of milestone 8.35z33's Battle-Zone clauses.</summary>
        private static bool NamesTheBattleZone(PlaceSpawnPointExclusion exclusion) =>
            exclusion == PlaceSpawnPointExclusion
                .SpacesThatAreNotAnEmptyMinionSpawnPointInTheBattleZone ||
            exclusion == PlaceSpawnPointExclusion.SpacesThatAreNeitherSuchAPointNorAdjacentToOne;

        /// <summary>
        /// Milestone 8.35z33. The disk narrowed to the Battle Zone's empty minion Spawn points -
        /// Whisper's Shadow Step and Shadow Walk - and, one clause further, to those points and their
        /// neighbours, which is Creeping Shadow.
        ///
        /// <para><b>The qualifying set is asked of the whole board and then intersected with the
        /// disk</b>, rather than the disk being tested point by point, because that is the order the
        /// sentence reads: the points exist independently of where Whisper is standing, and the Range
        /// icon says which of them she can reach. It is also why a hero outside the Battle Zone still
        /// sees the points inside it - the zone is asked of the point and never of her, which is the
        /// designer's own 2026-07-25 answer built in rather than restated.</para>
        ///
        /// <para><b>A dilated candidate need not itself be a Spawn point, and need not be in the
        /// Battle Zone.</b> Creeping Shadow's second arm says "a space in range adjacent to such spawn
        /// point" and asks nothing else of that space beyond what every Place asks - that it be empty,
        /// which <see cref="WithinRadius"/> has already decided. The first arm's own points stay
        /// admitted, because "or" adds to a set rather than replacing it.</para>
        /// </summary>
        private static IReadOnlyList<HexCoordinate> AdmittingOnlyBattleZoneMinionSpawnPoints(
            IReadOnlyList<HexCoordinate> candidates,
            PlaceSpawnPointExclusion exclusion,
            ITokenPlacementAuthority spaces,
            IBoardDisplacementAuthority board)
        {
            var qualifying = EmptyMinionSpawnPointQuery.FindInCurrentBattleZone(board, spaces);
            if (qualifying.Count == 0)
            {
                return Array.Empty<HexCoordinate>();
            }

            var dilates = exclusion ==
                PlaceSpawnPointExclusion.SpacesThatAreNeitherSuchAPointNorAdjacentToOne;

            return candidates
                .Where(candidate => qualifying.Any(point =>
                    point.Equals(candidate) ||
                    (dilates && HexDistanceQuery.Calculate(point, candidate) == 1)))
                .ToArray();
        }

        /// <summary>
        /// The equally-nearest empty spaces of the declared set. Ties are returned rather than broken:
        /// p.18 hands the choice between them to the piece's own team, and
        /// <see cref="NearestQuery"/> already refuses to break them for exactly that reason.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> NearestEmptyInScope(
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            IReadOnlyList<HexCoordinate> scopedCoordinates,
            RuleModificationSet modifications,
            RuleObject placedPiece)
        {
            var empty = scopedCoordinates
                .Where(candidate => !candidate.Equals(origin) &&
                    !ObstacleQuery.BlocksPlacement(spaces, candidate, modifications, placedPiece))
                .ToArray();
            return empty.Length == 0
                ? Array.Empty<HexCoordinate>()
                : NearestQuery.FindNearest(origin, empty);
        }
    }
}
