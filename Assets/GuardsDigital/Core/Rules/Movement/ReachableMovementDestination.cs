using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Rules.Movement
{
    public readonly struct ReachableMovementDestination
    {
        public ReachableMovementDestination(
            HexCoordinate destination,
            IEnumerable<HexCoordinate> route,
            int cost)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (cost <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cost),
                    cost,
                    "A reachable movement destination must cost at least one movement point.");
            }

            Destination = destination;
            Route = route.ToArray();
            Cost = cost;
        }

        public HexCoordinate Destination { get; }

        public IReadOnlyList<HexCoordinate> Route { get; }

        public int Cost { get; }
    }
}
