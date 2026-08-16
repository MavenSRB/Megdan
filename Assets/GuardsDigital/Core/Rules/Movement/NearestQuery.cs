using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Rules.Movement
{
    /// <summary>
    /// The rulebook's "nearest" over candidate spaces (milestone 6.4a): every candidate at the minimal
    /// geometric distance from the origin, in canonical axial order. Distance is
    /// <see cref="HexDistanceQuery"/> - p.18 counts spaces including spaces with obstacles - so this is
    /// never a route length; a caller that needs path-based closeness walks a path query instead. Ties
    /// are returned rather than broken, because whoever the rules make choose between equally near
    /// spaces is a chooser, not this query.
    /// </summary>
    public static class NearestQuery
    {
        public static IReadOnlyList<HexCoordinate> FindNearest(
            HexCoordinate origin, IEnumerable<HexCoordinate> candidates)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            var materialized = candidates.ToArray();
            if (materialized.Length == 0)
            {
                return Array.Empty<HexCoordinate>();
            }

            var minimum = materialized.Min(candidate => HexDistanceQuery.Calculate(origin, candidate));
            return materialized
                .Where(candidate => HexDistanceQuery.Calculate(origin, candidate) == minimum)
                .OrderBy(candidate => candidate.Q)
                .ThenBy(candidate => candidate.R)
                .ToArray();
        }
    }
}
