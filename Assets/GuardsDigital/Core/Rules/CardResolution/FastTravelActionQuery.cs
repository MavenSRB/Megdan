using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Which Zones a Fast Travel may finish in. Milestone 8.35z30.
    ///
    /// <para>Rulebook p.11 admits the hero's own Zone and any Zone adjacent to it, which is what every
    /// Fast Travel taken as p.12's replacement action does. Silverarrow's two Gold basics print a
    /// narrower sentence - "you may fast travel <b>to an adjacent zone</b>" - and the narrowing is the
    /// card's own words, so it is declared by the text that prints it rather than assumed by the rule
    /// underneath.</para>
    /// </summary>
    public enum FastTravelZoneScope
    {
        /// <summary>p.11's own reach, and the only one the replacement action has ever used.</summary>
        SameOrAdjacentZone = 0,

        /// <summary>
        /// The narrowing Shoot and Scoot and Keep the Distance print. A hero whose only legal Fast
        /// Travel would be inside their current Zone has no destination under this scope, which is the
        /// case the cards' own "if able" answers.
        /// </summary>
        AdjacentZoneOnly = 1
    }

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

            return EvaluateDestinationForActor(
                replacementOffer.ActorId,
                destination,
                board,
                board,
                participants,
                minions,
                FastTravelZoneScope.SameOrAdjacentZone);
        }

        /// <summary>
        /// Rulebook p.11's Fast Travel destination rule, stated once and asked of a hero rather than of
        /// an offer. Milestone 8.35z30 moved it here so that Silverarrow's card-text Fast
        /// Travel and p.12's replacement action cannot disagree about where a Fast Travel may finish -
        /// the same reason <see cref="Rules.Displacement.DirectMoveDestinations"/> is one statement.
        ///
        /// <para>Everything the page names is here and nothing else is: an empty playable space, a Zone
        /// the <paramref name="zoneScope"/> admits, no enemy hero or minion in the Zone being left, and
        /// none in the Zone being entered. Whether the hero is <em>permitted</em> to Fast Travel at all
        /// is a different question - p.18's restriction - and it is asked by whoever offers the Fast
        /// Travel, because a restriction bars the activity rather than narrowing its destinations.</para>
        /// </summary>
        /// <param name="spaces">
        /// The board's own empty-and-playable answer. It is a second port rather than a second walk of
        /// the map because that predicate is the board's to state - the two arguments are one object at
        /// every call site, so the destination this admits and the destination the commit accepts
        /// cannot come apart.
        /// </param>
        public static CardActionResolutionRejectionReason EvaluateDestinationForActor(
            HeroId actorId,
            HexCoordinate destination,
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            MatchParticipants participants,
            IEnumerable<MinionOccupancy> minions,
            FastTravelZoneScope zoneScope)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            var materializedMinions = minions?.ToArray() ?? throw new ArgumentNullException(nameof(minions));

            var actorOccupancies = board.Occupancies
                .Where(occupancy => occupancy.HeroId.Equals(actorId))
                .ToArray();
            if (actorOccupancies.Length != 1)
            {
                return CardActionResolutionRejectionReason.ActorUnavailable;
            }

            if (!spaces.IsEmptyPlayableSpace(destination))
            {
                return CardActionResolutionRejectionReason.FastTravelDestinationUnavailable;
            }

            var origin = actorOccupancies[0].Coordinate;
            var originZone = ZoneFor(board.MapDefinition, origin);
            var destinationZone = ZoneFor(board.MapDefinition, destination);
            var relationship = board.MapDefinition.GetZoneRelationship(originZone, destinationZone);
            var admitted = zoneScope == FastTravelZoneScope.AdjacentZoneOnly
                ? relationship == ZoneRelationship.Adjacent
                : relationship == ZoneRelationship.Same || relationship == ZoneRelationship.Adjacent;
            if (!admitted)
            {
                return CardActionResolutionRejectionReason.FastTravelZoneNotEligible;
            }

            var actorSide = participants.GetTeamForHero(actorId).Side;
            if (ContainsEnemy(
                originZone,
                actorId,
                actorSide,
                board,
                participants,
                materializedMinions))
            {
                return CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInOriginZone;
            }

            return ContainsEnemy(
                    destinationZone,
                    actorId,
                    actorSide,
                    board,
                    participants,
                    materializedMinions)
                ? CardActionResolutionRejectionReason.FastTravelBlockedByEnemyInDestinationZone
                : CardActionResolutionRejectionReason.None;
        }

        /// <summary>
        /// The spaces one hero could Fast Travel into right now, in canonical order - the destination
        /// rule above asked of every playable space, which is what a card-text Fast Travel offers.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> FindDestinationsForActor(
            HeroId actorId,
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            MatchParticipants participants,
            IEnumerable<MinionOccupancy> minions,
            FastTravelZoneScope zoneScope)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var materializedMinions = minions?.ToArray() ?? throw new ArgumentNullException(nameof(minions));
            return Array.AsReadOnly(board.MapDefinition.PlayableCoordinates
                .Where(destination => EvaluateDestinationForActor(
                    actorId, destination, board, spaces, participants, materializedMinions, zoneScope) ==
                    CardActionResolutionRejectionReason.None)
                .OrderBy(destination => destination.Q)
                .ThenBy(destination => destination.R)
                .ToArray());
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

        /// <summary>
        /// Unit-type question (6.8g ledger): none. p.11 requires "no enemy heroes or minions" in the
        /// origin and destination Zones - the type-insensitive union of all units - so a `Counts As`
        /// declaration cannot move an object into or out of this check and register enumeration is
        /// the declared read.
        /// </summary>
        private static bool ContainsEnemy(
            ZoneId zoneId,
            HeroId actorId,
            TeamSide actorSide,
            IBoardDisplacementAuthority board,
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
