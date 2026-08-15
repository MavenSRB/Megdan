using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public static class FastTravelActionQuery
    {
        public static IReadOnlyList<HexCoordinate> FindLegalDestinations(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            IEnumerable<MinionOccupancy> minions,
            RuleModificationSet modifications)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var materializedMinions = minions?.ToArray() ?? throw new ArgumentNullException(nameof(minions));
            if (!IsCurrentReplacementOffer(
                    replacementOffer,
                    replacedMovementActionDefinitionId,
                    materializedDefinitions,
                    materializedInstances,
                    modifications))
            {
                return Array.Empty<HexCoordinate>();
            }

            return Array.AsReadOnly(board.MapDefinition.PlayableCoordinates
                .Where(destination => EvaluateDestinationCore(
                    replacementOffer,
                    replacedMovementActionDefinitionId,
                    destination,
                    materializedDefinitions,
                    materializedInstances,
                    board,
                    participants,
                    materializedMinions,
                    true,
                    modifications) == CardActionResolutionRejectionReason.None)
                .OrderBy(destination => destination.Q)
                .ThenBy(destination => destination.R)
                .ToArray());
        }

        public static CardActionResolutionRejectionReason EvaluateDestination(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            HexCoordinate destination,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            IEnumerable<MinionOccupancy> minions,
            RuleModificationSet modifications)
        {
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var materializedMinions = minions?.ToArray() ?? throw new ArgumentNullException(nameof(minions));
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            return EvaluateDestinationCore(
                replacementOffer,
                replacedMovementActionDefinitionId,
                destination,
                materializedDefinitions,
                materializedInstances,
                board,
                participants,
                materializedMinions,
                false,
                modifications);
        }

        private static CardActionResolutionRejectionReason EvaluateDestinationCore(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            HexCoordinate destination,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            IReadOnlyList<MinionOccupancy> minions,
            bool offerAlreadyValidated,
            RuleModificationSet modifications)
        {
            if (!offerAlreadyValidated && !IsCurrentReplacementOffer(
                    replacementOffer,
                    replacedMovementActionDefinitionId,
                    definitions,
                    instances,
                    modifications))
            {
                return CardActionResolutionRejectionReason.UnavailableOffer;
            }

            var actorOccupancies = board.Occupancies
                .Where(occupancy => occupancy.HeroId.Equals(replacementOffer.ActorId))
                .ToArray();
            if (actorOccupancies.Length != 1)
            {
                return CardActionResolutionRejectionReason.ActorUnavailable;
            }

            var destinationSpaces = board.MapDefinition.Spaces
                .Where(space =>
                    space.Kind == BoardMapSpaceKind.Playable &&
                    space.Coordinate.Equals(destination))
                .ToArray();
            if (destinationSpaces.Length != 1 || board.IsOccupied(destination))
            {
                return CardActionResolutionRejectionReason.FastTravelDestinationUnavailable;
            }

            var origin = actorOccupancies[0].Coordinate;
            var originZone = ZoneFor(board.MapDefinition, origin);
            var destinationZone = ZoneFor(board.MapDefinition, destination);
            var relationship = board.MapDefinition.GetZoneRelationship(originZone, destinationZone);
            if (relationship != ZoneRelationship.Same && relationship != ZoneRelationship.Adjacent)
            {
                return CardActionResolutionRejectionReason.FastTravelZoneNotEligible;
            }

            var actorSide = participants.GetTeamForHero(replacementOffer.ActorId).Side;
            if (ContainsEnemy(
                originZone,
                replacementOffer.ActorId,
                actorSide,
                board,
                participants,
                minions))
            {
                return CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInOriginZone;
            }

            return ContainsEnemy(
                    destinationZone,
                    replacementOffer.ActorId,
                    actorSide,
                    board,
                    participants,
                    minions)
                ? CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInDestinationZone
                : CardActionResolutionRejectionReason.None;
        }

        private static bool IsCurrentReplacementOffer(
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            RuleModificationSet modifications)
        {
            var expectedOffer = GeneralRuleActionOfferQuery.GetAvailableFastTravelOffer(
                replacedMovementActionDefinitionId,
                replacementOffer.SourceCardInstanceId,
                replacementOffer.ActorId,
                definitions,
                instances,
                modifications);
            return expectedOffer.HasValue && expectedOffer.Value.Equals(replacementOffer);
        }

        private static bool ContainsEnemy(
            ZoneId zoneId,
            HeroId actorId,
            TeamSide actorSide,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            IEnumerable<MinionOccupancy> minions)
        {
            return board.Occupancies.Any(occupancy =>
                    !occupancy.HeroId.Equals(actorId) &&
                    participants.GetTeamForHero(occupancy.HeroId).Side != actorSide &&
                    ZoneFor(board.MapDefinition, occupancy.Coordinate).Equals(zoneId)) ||
                minions.Any(minion =>
                    minion.TeamSide != actorSide &&
                    ZoneFor(board.MapDefinition, minion.Coordinate).Equals(zoneId));
        }

        private static ZoneId ZoneFor(BoardMapDefinition map, HexCoordinate coordinate)
        {
            var space = map.Spaces.Single(candidate =>
                candidate.Kind == BoardMapSpaceKind.Playable && candidate.Coordinate.Equals(coordinate));
            return map.GetZoneForSpace(space.Id).Id;
        }
    }
}
