using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.Movement;

namespace GuardsDigital.Core.Rules.Placement
{
    /// <summary>
    /// The destinations a declared <see cref="PlacePolicy"/> admits, in canonical axial order.
    ///
    /// Nothing here walks a path. Rulebook p.18 states that placing "does not count as movement", so a
    /// Place asks a geometric or set-membership question and never a reachability one - which is also
    /// why a piece can be placed somewhere it could not walk to. Emptiness and playability are the
    /// board's own single answer (<see cref="ITokenPlacementAuthority.IsEmptyPlayableSpace"/>), which
    /// already carries p.19's one-obstacle-per-space rule and milestone 6.8e's terrain classification,
    /// so this query never re-derives either.
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
        public static IReadOnlyList<HexCoordinate> FindDestinations(
            PlacePolicy policy,
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            IReadOnlyList<HexCoordinate> scopedCoordinates)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));

            switch (policy.DestinationScope)
            {
                case PlaceDestinationScope.EmptySpacesWithinRadius:
                    return WithinRadius(policy.DestinationRadius, spaces, origin);

                case PlaceDestinationScope.NearestEmptySpacesInScope:
                    if (scopedCoordinates == null)
                    {
                        throw new ArgumentNullException(
                            nameof(scopedCoordinates),
                            "A nearest-in-scope Place is measured over a declared coordinate set.");
                    }

                    return NearestEmptyInScope(spaces, origin, scopedCoordinates);

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
            HexCoordinate origin)
        {
            var found = new List<HexCoordinate>();
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    var coordinate = new HexCoordinate(origin.Q + dq, origin.R + dr);
                    if (coordinate.Equals(origin) || !spaces.IsEmptyPlayableSpace(coordinate))
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
        /// The equally-nearest empty spaces of the declared set. Ties are returned rather than broken:
        /// p.18 hands the choice between them to the piece's own team, and
        /// <see cref="NearestQuery"/> already refuses to break them for exactly that reason.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> NearestEmptyInScope(
            ITokenPlacementAuthority spaces,
            HexCoordinate origin,
            IReadOnlyList<HexCoordinate> scopedCoordinates)
        {
            var empty = scopedCoordinates
                .Where(candidate => !candidate.Equals(origin) && spaces.IsEmptyPlayableSpace(candidate))
                .ToArray();
            return empty.Length == 0
                ? Array.Empty<HexCoordinate>()
                : NearestQuery.FindNearest(origin, empty);
        }
    }
}
