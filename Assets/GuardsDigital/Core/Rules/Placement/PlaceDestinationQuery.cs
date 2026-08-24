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
        public static IReadOnlyList<HexCoordinate> FindDestinations(
            PlacePolicy policy,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            IReadOnlyList<HexCoordinate> scopedCoordinates,
            RuleModificationSet modifications,
            IUnitSpawnAuthority spawnPoints = null)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (policy.SpawnPointExclusion != PlaceSpawnPointExclusion.None && spawnPoints == null)
            {
                throw new ArgumentNullException(
                    nameof(spawnPoints),
                    "A Place that excludes Spawn-point spaces is asked with the port that knows " +
                    "where they are.");
            }

            switch (policy.DestinationScope)
            {
                case PlaceDestinationScope.EmptySpacesWithinRadius:
                    return ExcludingSpawnPoints(
                        WithinRadius(policy.DestinationRadius, spaces, origin, modifications),
                        policy.SpawnPointExclusion,
                        spaces,
                        spawnPoints);

                case PlaceDestinationScope.NearestEmptySpacesInScope:
                    if (scopedCoordinates == null)
                    {
                        throw new ArgumentNullException(
                            nameof(scopedCoordinates),
                            "A nearest-in-scope Place is measured over a declared coordinate set.");
                    }

                    return NearestEmptyInScope(spaces, origin, scopedCoordinates, modifications);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(policy),
                        policy.DestinationScope,
                        "A Place destination scope must be one this query answers.");
            }
        }

        /// <summary>
        /// Every empty playable space at an inclusive rulebook distance of the origin, minus the origin
        /// itself. The disk is enumerated in axial coordinates the way <c>TargetQuery</c> enumerates
        /// its own, because both ask p.18's distance question rather than a route one.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> WithinRadius(
            int radius,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            RuleModificationSet modifications)
        {
            var found = new List<HexCoordinate>();
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    var coordinate = new HexCoordinate(origin.Q + dq, origin.R + dr);
                    if (coordinate.Equals(origin) ||
                        ObstacleQuery.BlocksPlacement(spaces, coordinate, modifications))
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
            IUnitSpawnAuthority spawnPoints)
        {
            if (exclusion == PlaceSpawnPointExclusion.None)
            {
                return candidates;
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
        /// The equally-nearest empty spaces of the declared set. Ties are returned rather than broken:
        /// p.18 hands the choice between them to the piece's own team, and
        /// <see cref="NearestQuery"/> already refuses to break them for exactly that reason.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> NearestEmptyInScope(
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            IReadOnlyList<HexCoordinate> scopedCoordinates,
            RuleModificationSet modifications)
        {
            var empty = scopedCoordinates
                .Where(candidate => !candidate.Equals(origin) &&
                    !ObstacleQuery.BlocksPlacement(spaces, candidate, modifications))
                .ToArray();
            return empty.Length == 0
                ? Array.Empty<HexCoordinate>()
                : NearestQuery.FindNearest(origin, empty);
        }
    }
}
