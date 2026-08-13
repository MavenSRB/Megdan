using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public sealed class AdjacentMoveAuthority
    {
        private readonly HashSet<HexCoordinate> boardCoordinates;
        private readonly List<HeroOccupancy> occupancies;
        private readonly List<ObstacleOccupancy> obstacleOccupancies;
        private readonly List<MinionOccupancy> minions;
        private readonly BoardOccupancyIndex occupancyIndex;
        private readonly IReadOnlyList<HeroOccupancy> occupanciesView;
        private readonly IReadOnlyList<ObstacleOccupancy> obstacleOccupanciesView;
        private readonly IReadOnlyList<MinionOccupancy> minionsView;
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

        // Boundary conversion: configured setup placements enter as QuickGameMinionSetupState and
        // become runtime MinionOccupancy records here; the setup type itself is setup data only.
        public AdjacentMoveAuthority(
            BoardMapDefinition mapDefinition,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies,
            IEnumerable<QuickGameMinionSetupState> initialMinions)
            : this(
                mapDefinition,
                initialOccupancies,
                initialObstacleOccupancies,
                (initialMinions ?? throw new ArgumentNullException(nameof(initialMinions)))
                    .Select(minion => minion == null
                        ? throw new ArgumentException("Initial minions must be non-null and have unique IDs.", nameof(initialMinions))
                        : MinionOccupancy.FromSetup(minion)))
        {
        }

        public AdjacentMoveAuthority(
            BoardMapDefinition mapDefinition,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies,
            IEnumerable<MinionOccupancy> initialMinions)
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

            minions = new List<MinionOccupancy>(materializedMinions);
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
            minions = new List<MinionOccupancy>(source.minions);
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

        public IReadOnlyList<MinionOccupancy> Minions => minionsView;

        public IReadOnlyList<HexCoordinate> BoardCoordinates => boardCoordinatesView;

        internal bool IsOccupied(HexCoordinate coordinate) => occupancyIndex.IsOccupied(coordinate);

        internal IReadOnlyCollection<HexCoordinate> OccupiedCoordinates => occupancyIndex.OccupiedCoordinates;

        internal IReadOnlyCollection<HexCoordinate> PlayableCoordinateSet => boardCoordinates;

        internal AdjacentMoveAuthority CreateValidationFork() => new AdjacentMoveAuthority(this);

        public BoardRestoreState CaptureRestoreState() =>
            new BoardRestoreState(
                occupancies,
                obstacleOccupancies,
                minions.Select(minion => new RestoredMinionPosition(minion.MinionId, minion.Coordinate)));

        public static AdjacentMoveAuthority Restore(
            BoardRestoreState state,
            BoardMapDefinition mapDefinition,
            IEnumerable<QuickGameMinionSetupState> configuredMinions)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (mapDefinition == null) throw new ArgumentNullException(nameof(mapDefinition));
            if (configuredMinions == null) throw new ArgumentNullException(nameof(configuredMinions));

            var available = configuredMinions.ToArray();
            if (state.RemainingMinions.Any(position => available.Count(minion => minion.MinionId.Equals(position.MinionId)) != 1))
            {
                throw new ArgumentException(
                    "Restored remaining-minion identities must match configured minion content.",
                    nameof(state));
            }

            // Identity, team, type, and spawn come from the configured setup content; the current
            // position comes from the restored state, not from the setup placement.
            var remaining = state.RemainingMinions.Select(position =>
            {
                var setup = available.Single(minion => minion.MinionId.Equals(position.MinionId));
                var space = mapDefinition.Spaces.SingleOrDefault(candidate =>
                    candidate.Kind == BoardMapSpaceKind.Playable &&
                    candidate.Coordinate.Equals(position.Coordinate));
                if (space == null)
                {
                    throw new ArgumentException(
                        "Restored minion positions must resolve to playable spaces on the selected map.",
                        nameof(state));
                }

                return MinionOccupancy.FromSetup(setup).WithPosition(
                    new ConfiguredSpaceReference(mapDefinition.Id, space.Id),
                    position.Coordinate);
            });
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

        internal MinionOccupancy CommitValidatedMinionRemoval(MinionId minionId)
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

        // The validated authority-level minion position mutation required before Object Push
        // (6.4), Lane Push (6.9-6.11), and minion Respawn can move minions. No player-facing
        // command or session exposure exists yet; consumers arrive with those milestones.
        public MinionMoveResult TryCommitMinionMove(MinionId minionId, HexCoordinate destination)
        {
            var index = minions.FindIndex(minion => minion.MinionId.Equals(minionId));
            if (index < 0)
            {
                return MinionMoveResult.Reject(MinionMoveRejectionReason.UnknownMinion);
            }

            var space = MapDefinition.Spaces.SingleOrDefault(candidate =>
                candidate.Kind == BoardMapSpaceKind.Playable &&
                candidate.Coordinate.Equals(destination));
            if (space == null)
            {
                return MinionMoveResult.Reject(MinionMoveRejectionReason.DestinationOffBoard);
            }

            if (occupancyIndex.IsOccupied(destination))
            {
                return MinionMoveResult.Reject(MinionMoveRejectionReason.DestinationOccupied);
            }

            var current = minions[index];
            var moved = current.WithPosition(
                new ConfiguredSpaceReference(MapDefinition.Id, space.Id),
                destination);
            occupancyIndex.Move(current.Coordinate, destination);
            minions[index] = moved;
            return MinionMoveResult.Accept(moved, current.Coordinate);
        }
    }
}
