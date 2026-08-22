using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Immediate Clear replacement execution. Selection is one atomic command, so Clear introduces no
    /// pending continuation; the generic ReplaceAction effect node remains deferred to its named trigger.
    /// </summary>
    internal sealed class ClearCardActionStrategyBackend
    {
        private readonly AdjacentMoveAuthority board;

        internal ClearCardActionStrategyBackend(AdjacentMoveAuthority board)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
        }

        /// <summary>
        /// Milestone 8.8b: whether this Clear could remove anything, asked of the board this backend
        /// owns. The counterpart of the Attack backend's target question, and the reason the turn
        /// authority can refuse a Clear it cannot itself see the board for.
        /// </summary>
        public bool CanRemoveAnything(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications) =>
            ClearActionQuery.CanRemoveAnything(
                replacementOffer,
                replacedAttackActionDefinitionId,
                definitions,
                instances,
                board,
                modifications);

        public CardActionResolutionResult PerformTurnClear(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Clear requires a resolution ID.", nameof(resolutionId));
            }

            var materializedDefinitions = definitions?.ToArray() ??
                throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ??
                throw new ArgumentNullException(nameof(instances));
            var selected = selectedTokenIds?.ToArray() ??
                throw new ArgumentNullException(nameof(selectedTokenIds));
            var rejection = ClearActionQuery.EvaluateSelection(
                replacementOffer,
                replacedAttackActionDefinitionId,
                selected,
                materializedDefinitions,
                materializedInstances,
                board,
                modifications);
            if (rejection != CardActionResolutionRejectionReason.None)
            {
                return CardActionResolutionResult.Reject(rejection, null, null);
            }

            var selectedSet = new HashSet<TokenInstanceId>(selected);
            var removals = board.PlacedTokens
                .Where(token => selectedSet.Contains(token.Id))
                .OrderBy(token => token.Coordinate.Q)
                .ThenBy(token => token.Coordinate.R)
                .ThenBy(token => token.Id.Value, StringComparer.Ordinal)
                .ToArray();
            var steps = new List<ResolutionStep>
            {
                ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted),
                ResolutionStep.FromFact(CardActionResolutionEvent.ClearActionStarted(
                    resolutionId, replacementOffer, replacedAttackActionDefinitionId))
            };
            foreach (var token in removals)
            {
                var removed = board.CommitValidatedTokenReclamation(token.Id);
                steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.TokenCleared(
                    resolutionId,
                    replacementOffer,
                    replacedAttackActionDefinitionId,
                    removed)));
            }

            steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.ClearActionCompleted(
                resolutionId, replacementOffer, replacedAttackActionDefinitionId)));
            steps.Add(ResolutionStep.AtActionEnded(
                replacementOffer.SourceCardInstanceId,
                replacementOffer.ActionDefinitionId,
                replacementOffer.ActorId,
                ActionKind.Clear));
            return CardActionResolutionResult.Accept(true, null, null, steps);
        }
    }
}
