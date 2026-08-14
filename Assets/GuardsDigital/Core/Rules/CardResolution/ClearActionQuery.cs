using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.AdjacentMove;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>Authoritative replacement and adjacent-token validation for the general Clear action.</summary>
    public static class ClearActionQuery
    {
        public static IReadOnlyList<TokenInstanceId> FindLegalTokenIds(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            AdjacentMoveAuthority board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            if (!IsCurrentReplacementOffer(
                    replacementOffer,
                    replacedAttackActionDefinitionId,
                    materializedDefinitions,
                    materializedInstances) ||
                !board.TryGetHeroCoordinate(replacementOffer.ActorId, out var actorCoordinate))
            {
                return Array.Empty<TokenInstanceId>();
            }

            var adjacentCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(
                actorCoordinate.GetNeighbors());
            return Array.AsReadOnly(board.PlacedTokens
                .Where(token => adjacentCoordinates.Contains(token.Coordinate))
                .OrderBy(token => token.Coordinate.Q)
                .ThenBy(token => token.Coordinate.R)
                .ThenBy(token => token.Id.Value, StringComparer.Ordinal)
                .Select(token => token.Id)
                .ToArray());
        }

        public static CardActionResolutionRejectionReason EvaluateSelection(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            AdjacentMoveAuthority board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var selected = selectedTokenIds?.ToArray() ??
                throw new ArgumentNullException(nameof(selectedTokenIds));
            if (!IsCurrentReplacementOffer(
                    replacementOffer,
                    replacedAttackActionDefinitionId,
                    materializedDefinitions,
                    materializedInstances))
            {
                return CardActionResolutionRejectionReason.UnavailableOffer;
            }

            if (!board.TryGetHeroCoordinate(replacementOffer.ActorId, out _))
            {
                return CardActionResolutionRejectionReason.ActorUnavailable;
            }

            if (selected.Any(id => !id.IsDefined) || selected.Distinct().Count() != selected.Length)
            {
                return CardActionResolutionRejectionReason.ClearTokenSelectionUnavailable;
            }

            var legal = new HashSet<TokenInstanceId>(FindLegalTokenIds(
                replacementOffer,
                replacedAttackActionDefinitionId,
                materializedDefinitions,
                materializedInstances,
                board));
            return selected.All(legal.Contains)
                ? CardActionResolutionRejectionReason.None
                : CardActionResolutionRejectionReason.ClearTokenSelectionUnavailable;
        }

        private static bool IsCurrentReplacementOffer(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            var expected = GeneralRuleActionOfferQuery.GetAvailableClearOffer(
                replacedAttackActionDefinitionId,
                replacementOffer.SourceCardInstanceId,
                replacementOffer.ActorId,
                definitions,
                instances);
            return expected.HasValue && expected.Value.Equals(replacementOffer);
        }
    }
}
