using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// A printed minimum separation between standing tokens of one definition. The limit is content
    /// data, not a property of tokens in general; ordinary placements pass zero.
    /// </summary>
    public static class TokenPlacementSeparationQuery
    {
        public static bool Admits(
            HexCoordinate destination,
            TokenDefinitionReference definition,
            IEnumerable<PlacedToken> placedTokens,
            int minimumHexDistance)
        {
            if (placedTokens == null) throw new ArgumentNullException(nameof(placedTokens));
            if (minimumHexDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumHexDistance));
            return minimumHexDistance == 0 || placedTokens
                .Where(token => token.Definition.Reference.Equals(definition))
                .All(token => HexDistanceQuery.Calculate(destination, token.Coordinate) >= minimumHexDistance);
        }
    }
}
