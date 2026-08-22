using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;

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
            AdjacentMoveAuthority board,
            RuleModificationSet modifications)
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
                    materializedInstances,
                    modifications) ||
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

        /// <summary>
        /// Whether this Clear could remove anything at all, which is rulebook p.13's "any number of
        /// tokens adjacent to you" asked of the board rather than of the chooser.
        ///
        /// <para>Milestone 8.8b, and it is one statement so the offer set and the start cannot disagree
        /// about it - the same arrangement <c>CanFindLegalTarget</c> has for an Attack. The distinction
        /// it draws is the one milestone 6.5af drew for a Skill: zero is a number the page allows, so a
        /// hero standing beside a token may still clear none, and what this answers false for is a Clear
        /// that <em>cannot</em> remove anything rather than one whose chooser declines to.</para>
        /// </summary>
        public static bool CanRemoveAnything(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications) =>
            FindLegalTokenIds(
                replacementOffer,
                replacedAttackActionDefinitionId,
                definitions,
                instances,
                board,
                modifications).Count > 0;

        public static CardActionResolutionRejectionReason EvaluateSelection(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications)
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
                    materializedInstances,
                    modifications))
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
                board,
                modifications));
            return selected.All(legal.Contains)
                ? CardActionResolutionRejectionReason.None
                : CardActionResolutionRejectionReason.ClearTokenSelectionUnavailable;
        }

        private static bool IsCurrentReplacementOffer(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            var expected = GeneralRuleActionOfferQuery.GetAvailableClearOffer(
                replacedAttackActionDefinitionId,
                replacementOffer.SourceCardInstanceId,
                replacementOffer.ActorId,
                definitions,
                instances,
                modifications);
            return expected.HasValue && expected.Value.Equals(replacementOffer);
        }
    }
}
