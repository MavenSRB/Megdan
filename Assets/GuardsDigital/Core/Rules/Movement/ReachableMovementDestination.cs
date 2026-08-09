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

            var materializedRoute = route.ToArray();
            if (materializedRoute.Length - 1 != cost)
            {
                throw new ArgumentException(
                    "A reachable movement route must contain its origin and exactly one coordinate per movement point.",
                    nameof(route));
            }

            if (!materializedRoute[materializedRoute.Length - 1].Equals(destination))
            {
                throw new ArgumentException(
                    "A reachable movement route must end at its destination.",
                    nameof(route));
            }

            for (var index = 1; index < materializedRoute.Length; index++)
            {
                if (!materializedRoute[index - 1].GetNeighbors().Contains(materializedRoute[index]))
                {
                    throw new ArgumentException(
                        "A reachable movement route must contain only adjacent steps.",
                        nameof(route));
                }
            }

            Destination = destination;
            Route = Array.AsReadOnly(materializedRoute);
            Cost = cost;
        }

        public HexCoordinate Destination { get; }

        public IReadOnlyList<HexCoordinate> Route { get; }

        public int Cost { get; }
    }
}
