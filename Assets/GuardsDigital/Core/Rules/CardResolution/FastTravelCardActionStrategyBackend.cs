using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.CardResolution
{
    internal sealed class FastTravelCardActionStrategyBackend
    {
        private readonly AdjacentMoveAuthority board;

        internal FastTravelCardActionStrategyBackend(AdjacentMoveAuthority board)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public CardActionResolutionResult PerformTurnFastTravel(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            HexCoordinate destination,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            IEnumerable<MinionOccupancy> minions,
            RuleModificationSet modifications)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Fast Travel requires a resolution ID.", nameof(resolutionId));
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var materializedMinions = minions?.ToArray() ?? throw new ArgumentNullException(nameof(minions));
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var rejection = FastTravelActionQuery.EvaluateDestination(
                replacementOffer,
                replacedMovementActionDefinitionId,
                destination,
                materializedDefinitions,
                materializedInstances,
                board,
                participants,
                materializedMinions,
                modifications);
            if (rejection != CardActionResolutionRejectionReason.None)
            {
                return Reject(rejection);
            }

            var travelEvent = board.CommitValidatedFastTravel(replacementOffer.ActorId, destination);
            return CardActionResolutionResult.Accept(
                true,
                null,
                null,
                new[]
                {
                    ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted),
                    ResolutionStep.FromFact(CardActionResolutionEvent.FastTravelActionStarted(
                        resolutionId,
                        replacementOffer,
                        replacedMovementActionDefinitionId,
                        travelEvent)),
                    ResolutionStep.FromFact(CardActionResolutionEvent.FastTravelCommitted(
                        resolutionId,
                        replacementOffer,
                        replacedMovementActionDefinitionId,
                        travelEvent)),
                    ResolutionStep.FromFact(CardActionResolutionEvent.FastTravelActionCompleted(
                        resolutionId,
                        replacementOffer,
                        replacedMovementActionDefinitionId,
                        travelEvent)),
                    ResolutionStep.AtActionEnded(
                        replacementOffer.SourceCardInstanceId,
                        replacementOffer.ActionDefinitionId,
                        replacementOffer.ActorId,
                        ActionKind.FastTravel)
                });
        }

        private static CardActionResolutionResult Reject(CardActionResolutionRejectionReason reason) =>
            CardActionResolutionResult.Reject(reason, null, null);
    }
}
