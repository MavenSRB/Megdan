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
        private readonly BoardOccupancyIndex occupancyIndex;
        private readonly IReadOnlyList<HeroOccupancy> occupanciesView;
        private readonly IReadOnlyList<ObstacleOccupancy> obstacleOccupanciesView;
        private readonly IReadOnlyList<QuickGameMinionSetupState> minionsView;
        private readonly IReadOnlyList<HexCoordinate> boardCoordinatesView;

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
            occupancyIndex = new BoardOccupancyIndex();
            var materializedMinions = initialMinions.ToArray();

            // Per-kind identity validation stays with each kind; the shared occupancy index owns
            // the one-obstacle-per-space rule for every kind through its single Add.
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

                occupancyIndex.Add(occupancy.Coordinate, BoardOccupant.ForHero(occupancy.HeroId), nameof(initialOccupancies));
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

                occupancyIndex.Add(obstacleOccupancy.Coordinate, BoardOccupant.ForObstacle(obstacleOccupancy.ObstacleId), nameof(initialObstacleOccupancies));
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

                occupancyIndex.Add(minion.Coordinate, BoardOccupant.ForMinion(minion.MinionId), nameof(initialMinions));
            }

            minions = new List<QuickGameMinionSetupState>(materializedMinions);
            occupanciesView = occupancies.AsReadOnly();
            obstacleOccupanciesView = obstacleOccupancies.AsReadOnly();
            minionsView = minions.AsReadOnly();
            boardCoordinatesView = this.boardCoordinates.ToArray();
        }

        // Already-validated copy constructor for CreateValidationFork: the source authority's
        // state was validated when it was built and mutated only through validated commits, so a
        // fork clones that state without re-running the constructor validation.
        private AdjacentMoveAuthority(AdjacentMoveAuthority source)
        {
            MapDefinition = source.MapDefinition;
            boardCoordinates = source.boardCoordinates;
            occupancies = new List<HeroOccupancy>(source.occupancies);
            obstacleOccupancies = new List<ObstacleOccupancy>(source.obstacleOccupancies);
            minions = new List<QuickGameMinionSetupState>(source.minions);
            occupancyIndex = source.occupancyIndex.Clone();
            occupanciesView = occupancies.AsReadOnly();
            obstacleOccupanciesView = obstacleOccupancies.AsReadOnly();
            minionsView = minions.AsReadOnly();
            boardCoordinatesView = source.boardCoordinatesView;
        }

        public IReadOnlyList<HeroOccupancy> Occupancies => occupanciesView;

        public BoardMapDefinition MapDefinition { get; }

        public MapDefinitionReference SelectedMap => MapDefinition.Reference;

        public IReadOnlyList<ObstacleOccupancy> ObstacleOccupancies => obstacleOccupanciesView;

        public IReadOnlyList<QuickGameMinionSetupState> Minions => minionsView;

        public IReadOnlyList<HexCoordinate> BoardCoordinates => boardCoordinatesView;

        internal bool IsOccupied(HexCoordinate coordinate) => occupancyIndex.IsOccupied(coordinate);

        internal IReadOnlyCollection<HexCoordinate> OccupiedCoordinates => occupancyIndex.OccupiedCoordinates;

        internal IReadOnlyCollection<HexCoordinate> PlayableCoordinateSet => boardCoordinates;

        internal AdjacentMoveAuthority CreateValidationFork() => new AdjacentMoveAuthority(this);

        public BoardRestoreState CaptureRestoreState() =>
            new BoardRestoreState(occupancies, obstacleOccupancies, minions.Select(minion => minion.MinionId));

        public static AdjacentMoveAuthority Restore(
            BoardRestoreState state,
            BoardMapDefinition mapDefinition,
            IEnumerable<QuickGameMinionSetupState> configuredMinions)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (mapDefinition == null) throw new ArgumentNullException(nameof(mapDefinition));
            if (configuredMinions == null) throw new ArgumentNullException(nameof(configuredMinions));

            var available = configuredMinions.ToArray();
            if (state.RemainingMinionIds.Any(id => available.Count(minion => minion.MinionId.Equals(id)) != 1))
            {
                throw new ArgumentException(
                    "Restored remaining-minion identities must match configured minion content.",
                    nameof(state));
            }

            var remaining = available.Where(minion => state.RemainingMinionIds.Contains(minion.MinionId));
            return new AdjacentMoveAuthority(mapDefinition, state.HeroOccupancies, state.ObstacleOccupancies, remaining);
        }

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

            if (occupancyIndex.IsOccupied(command.Destination))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.DestinationOccupied);
            }

            occupancyIndex.Move(command.Origin, command.Destination);
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

            if (!boardCoordinates.Contains(destination) || occupancyIndex.IsOccupied(destination))
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
            occupancyIndex.Move(origin, destination);
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
            occupancyIndex.Remove(removed.Coordinate);
            return removed;
        }
    }
}
