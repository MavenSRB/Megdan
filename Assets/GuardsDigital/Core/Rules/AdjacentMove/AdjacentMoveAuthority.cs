using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public sealed class AdjacentMoveAuthority
    {
        private readonly HashSet<HexCoordinate> boardCoordinates;
        private readonly List<HeroOccupancy> occupancies;
        private readonly List<ObstacleOccupancy> obstacleOccupancies;
        private readonly List<QuickGameMinionSetupState> minions;

        public AdjacentMoveAuthority(
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> initialOccupancies)
            : this(BoardMapDefinition.CreateSynthetic(new MapId("synthetic-coordinate-board"), 1, boardCoordinates), initialOccupancies, Enumerable.Empty<ObstacleOccupancy>(), Enumerable.Empty<QuickGameMinionSetupState>())
        {
        }

        public AdjacentMoveAuthority(
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies)
            : this(BoardMapDefinition.CreateSynthetic(new MapId("synthetic-coordinate-board"), 1, boardCoordinates), initialOccupancies, initialObstacleOccupancies, Enumerable.Empty<QuickGameMinionSetupState>())
        {
        }

        public AdjacentMoveAuthority(
            IEnumerable<HexCoordinate> boardCoordinates,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies,
            IEnumerable<QuickGameMinionSetupState> initialMinions)
            : this(BoardMapDefinition.CreateSynthetic(new MapId("synthetic-coordinate-board"), 1, boardCoordinates), initialOccupancies, initialObstacleOccupancies, initialMinions)
        {
        }

        public AdjacentMoveAuthority(
            BoardMapDefinition mapDefinition,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies)
            : this(mapDefinition, initialOccupancies, initialObstacleOccupancies, Enumerable.Empty<QuickGameMinionSetupState>())
        {
        }

        public AdjacentMoveAuthority(
            BoardMapDefinition mapDefinition,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies,
            IEnumerable<QuickGameMinionSetupState> initialMinions)
        {
            MapDefinition = mapDefinition ?? throw new ArgumentNullException(nameof(mapDefinition));

            if (initialOccupancies == null)
            {
                throw new ArgumentNullException(nameof(initialOccupancies));
            }

            if (initialObstacleOccupancies == null)
            {
                throw new ArgumentNullException(nameof(initialObstacleOccupancies));
            }

            if (initialMinions == null)
            {
                throw new ArgumentNullException(nameof(initialMinions));
            }

            this.boardCoordinates = new HashSet<HexCoordinate>(mapDefinition.PlayableCoordinates);
            occupancies = new List<HeroOccupancy>();
            obstacleOccupancies = new List<ObstacleOccupancy>();
            var materializedMinions = initialMinions.ToArray();

            foreach (var occupancy in initialOccupancies)
            {
                if (!this.boardCoordinates.Contains(occupancy.Coordinate))
                {
                    throw new ArgumentException("Initial occupancy must be on the board.", nameof(initialOccupancies));
                }

                if (occupancies.Any(existing => existing.HeroId.Equals(occupancy.HeroId)))
                {
                    throw new ArgumentException("Initial occupancies must have unique hero IDs.", nameof(initialOccupancies));
                }

                if (occupancies.Any(existing => existing.Coordinate.Equals(occupancy.Coordinate)))
                {
                    throw new ArgumentException("Initial occupancies must have unique coordinates.", nameof(initialOccupancies));
                }

                occupancies.Add(occupancy);
            }

            foreach (var obstacleOccupancy in initialObstacleOccupancies)
            {
                if (!this.boardCoordinates.Contains(obstacleOccupancy.Coordinate))
                {
                    throw new ArgumentException("Initial obstacle occupancy must be on the board.", nameof(initialObstacleOccupancies));
                }

                if (obstacleOccupancies.Any(existing => existing.ObstacleId.Equals(obstacleOccupancy.ObstacleId)))
                {
                    throw new ArgumentException("Initial obstacle occupancies must have unique obstacle IDs.", nameof(initialObstacleOccupancies));
                }

                if (obstacleOccupancies.Any(existing => existing.Coordinate.Equals(obstacleOccupancy.Coordinate)) ||
                    occupancies.Any(existing => existing.Coordinate.Equals(obstacleOccupancy.Coordinate)))
                {
                    throw new ArgumentException("Initial obstacle occupancies must not share coordinates with another obstacle or unit.", nameof(initialObstacleOccupancies));
                }

                obstacleOccupancies.Add(obstacleOccupancy);
            }

            if (materializedMinions.Any(minion => minion == null) ||
                materializedMinions.Select(minion => minion.MinionId).Distinct().Count() != materializedMinions.Length)
            {
                throw new ArgumentException("Initial minions must be non-null and have unique IDs.", nameof(initialMinions));
            }

            foreach (var minion in materializedMinions)
            {
                if (!minion.MinionId.IsDefined ||
                    !minion.TeamId.IsDefined ||
                    (minion.TeamSide != TeamSide.Red && minion.TeamSide != TeamSide.Blue) ||
                    (minion.MinionType != StandardMinionType.Melee &&
                        minion.MinionType != StandardMinionType.Ranged &&
                        minion.MinionType != StandardMinionType.Heavy))
                {
                    throw new ArgumentException("Initial minions require defined identity, team, and standard intrinsic type.", nameof(initialMinions));
                }

                if (!minion.OccupiedSpace.MapId.Equals(mapDefinition.Id) ||
                    !mapDefinition.Spaces.Any(space =>
                        space.Id.Equals(minion.OccupiedSpace.SpaceId) &&
                        space.Kind == BoardMapSpaceKind.Playable &&
                        space.Coordinate.Equals(minion.Coordinate)))
                {
                    throw new ArgumentException("Initial minion occupancy must resolve to its exact playable space on the selected map.", nameof(initialMinions));
                }

                if (occupancies.Any(existing => existing.Coordinate.Equals(minion.Coordinate)) ||
                    obstacleOccupancies.Any(existing => existing.Coordinate.Equals(minion.Coordinate)) ||
                    materializedMinions.Count(existing => existing.Coordinate.Equals(minion.Coordinate)) != 1)
                {
                    throw new ArgumentException("Initial minions must not share a coordinate with another obstacle or unit.", nameof(initialMinions));
                }
            }

            minions = new List<QuickGameMinionSetupState>(materializedMinions);
        }

        public IReadOnlyList<HeroOccupancy> Occupancies => occupancies.ToArray();

        public BoardMapDefinition MapDefinition { get; }

        public MapDefinitionReference SelectedMap => MapDefinition.Reference;

        public IReadOnlyList<ObstacleOccupancy> ObstacleOccupancies => obstacleOccupancies.ToArray();

        public IReadOnlyList<QuickGameMinionSetupState> Minions => minions.ToArray();

        public IReadOnlyList<HexCoordinate> BoardCoordinates => boardCoordinates.ToArray();

        public AdjacentMoveResult TryCommit(AdjacentMoveCommand command)
        {
            var heroIndex = occupancies.FindIndex(occupancy => occupancy.HeroId.Equals(command.HeroId));
            if (heroIndex < 0)
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.UnknownHero);
            }

            var currentOccupancy = occupancies[heroIndex];
            if (!currentOccupancy.Coordinate.Equals(command.Origin))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.WrongOrigin);
            }

            if (!boardCoordinates.Contains(command.Destination))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.DestinationOffBoard);
            }

            if (!currentOccupancy.Coordinate.GetNeighbors().Contains(command.Destination))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.DestinationNotAdjacent);
            }

            if (occupancies.Any(occupancy => occupancy.Coordinate.Equals(command.Destination)) ||
                obstacleOccupancies.Any(occupancy => occupancy.Coordinate.Equals(command.Destination)) ||
                minions.Any(minion => minion.Coordinate.Equals(command.Destination)))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.DestinationOccupied);
            }

            occupancies[heroIndex] = new HeroOccupancy(command.HeroId, command.Destination);
            return AdjacentMoveResult.Accept(new AdjacentMoveEvent(
                command.HeroId,
                command.Origin,
                command.Destination));
        }

        internal FastTravelEvent CommitValidatedFastTravel(HeroId heroId, HexCoordinate destination)
        {
            var heroIndex = occupancies.FindIndex(occupancy => occupancy.HeroId.Equals(heroId));
            if (heroIndex < 0)
            {
                throw new InvalidOperationException("A validated Fast Travel actor is no longer on the board.");
            }

            if (!boardCoordinates.Contains(destination) ||
                occupancies.Any(occupancy => occupancy.Coordinate.Equals(destination)) ||
                obstacleOccupancies.Any(occupancy => occupancy.Coordinate.Equals(destination)) ||
                minions.Any(minion => minion.Coordinate.Equals(destination)))
            {
                throw new InvalidOperationException("A validated Fast Travel destination is no longer empty and playable.");
            }

            var origin = occupancies[heroIndex].Coordinate;
            var originSpace = MapDefinition.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable && space.Coordinate.Equals(origin));
            var destinationSpace = MapDefinition.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable && space.Coordinate.Equals(destination));
            var travelEvent = new FastTravelEvent(
                heroId,
                origin,
                destination,
                MapDefinition.GetZoneForSpace(originSpace.Id).Id,
                MapDefinition.GetZoneForSpace(destinationSpace.Id).Id);
            occupancies[heroIndex] = new HeroOccupancy(heroId, destination);
            return travelEvent;
        }

        internal QuickGameMinionSetupState CommitValidatedMinionRemoval(MinionId minionId)
        {
            var index = minions.FindIndex(minion => minion.MinionId.Equals(minionId));
            if (index < 0)
            {
                throw new InvalidOperationException("A validated minion target is no longer on the board.");
            }

            var removed = minions[index];
            minions.RemoveAt(index);
            return removed;
        }
    }
}
