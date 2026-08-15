using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public sealed class AdjacentMoveAuthority : ITokenPlacementAuthority, IBoardDisplacementAuthority
    {
        private readonly HashSet<HexCoordinate> boardCoordinates;
        private readonly HashSet<HexCoordinate> terrainCoordinates;
        private readonly List<HeroOccupancy> occupancies;
        private readonly List<ObstacleOccupancy> obstacleOccupancies;
        private readonly List<MinionOccupancy> minions;
        private readonly List<PlacedToken> tokens;
        private readonly BoardOccupancyIndex occupancyIndex;
        private readonly IReadOnlyList<HeroOccupancy> occupanciesView;
        private readonly IReadOnlyList<ObstacleOccupancy> obstacleOccupanciesView;
        private readonly IReadOnlyList<MinionOccupancy> minionsView;
        private readonly IReadOnlyList<PlacedToken> tokensView;
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
            : this(mapDefinition, initialOccupancies, initialObstacleOccupancies, initialMinions,
                Enumerable.Empty<PlacedToken>())
        {
        }

        // Tokens enter only through a restore. No configured setup places one: a token reaches the
        // board when an effect places it, which is why this arm is not part of the public setup
        // surface the other constructors form.
        private AdjacentMoveAuthority(
            BoardMapDefinition mapDefinition,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies,
            IEnumerable<MinionOccupancy> initialMinions,
            IEnumerable<PlacedToken> initialTokens)
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

            if (initialTokens == null)
            {
                throw new ArgumentNullException(nameof(initialTokens));
            }

            this.boardCoordinates = new HashSet<HexCoordinate>(mapDefinition.PlayableCoordinates);
            terrainCoordinates = new HashSet<HexCoordinate>(mapDefinition.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Terrain)
                .Select(space => space.Coordinate));
            occupancies = new List<HeroOccupancy>();
            obstacleOccupancies = new List<ObstacleOccupancy>();
            occupancyIndex = new BoardOccupancyIndex();
            var materializedMinions = initialMinions.ToArray();
            var materializedTokens = initialTokens.ToArray();

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

            // Per-kind identity and supply validation stays with the kind; the shared index still owns
            // the one-occupant-per-space rule through the same single Add every other kind uses.
            if (materializedTokens.Any(token => token == null) ||
                materializedTokens.Select(token => token.Id).Distinct().Count() != materializedTokens.Length)
            {
                throw new ArgumentException("Initial tokens must be non-null and have unique IDs.", nameof(initialTokens));
            }

            foreach (var group in materializedTokens.GroupBy(token => token.Definition.Reference))
            {
                if (group.Count() > group.First().Definition.SupplyCount)
                {
                    throw new ArgumentException(
                        $"More '{group.Key}' tokens are on the board than the trusted supply contains.",
                        nameof(initialTokens));
                }
            }

            foreach (var token in materializedTokens)
            {
                if (!this.boardCoordinates.Contains(token.Coordinate))
                {
                    throw new ArgumentException("Initial token placement must be on the board.", nameof(initialTokens));
                }

                occupancyIndex.Add(token.Coordinate, BoardOccupant.ForToken(token.Id), nameof(initialTokens));
            }

            minions = new List<MinionOccupancy>(materializedMinions);
            tokens = new List<PlacedToken>(materializedTokens);
            occupanciesView = occupancies.AsReadOnly();
            obstacleOccupanciesView = obstacleOccupancies.AsReadOnly();
            minionsView = minions.AsReadOnly();
            tokensView = tokens.AsReadOnly();
            boardCoordinatesView = this.boardCoordinates.ToArray();
        }

        // Already-validated copy constructor for CreateValidationFork: the source authority's
        // state was validated when it was built and mutated only through validated commits, so a
        // fork clones that state without re-running the constructor validation.
        private AdjacentMoveAuthority(AdjacentMoveAuthority source)
        {
            MapDefinition = source.MapDefinition;
            boardCoordinates = source.boardCoordinates;
            terrainCoordinates = source.terrainCoordinates;
            occupancies = new List<HeroOccupancy>(source.occupancies);
            obstacleOccupancies = new List<ObstacleOccupancy>(source.obstacleOccupancies);
            minions = new List<MinionOccupancy>(source.minions);
            tokens = new List<PlacedToken>(source.tokens);
            occupancyIndex = source.occupancyIndex.Clone();
            occupanciesView = occupancies.AsReadOnly();
            obstacleOccupanciesView = obstacleOccupancies.AsReadOnly();
            minionsView = minions.AsReadOnly();
            tokensView = tokens.AsReadOnly();
            boardCoordinatesView = source.boardCoordinatesView;
        }

        public IReadOnlyList<HeroOccupancy> Occupancies => occupanciesView;

        public BoardMapDefinition MapDefinition { get; }

        public MapDefinitionReference SelectedMap => MapDefinition.Reference;

        public IReadOnlyList<ObstacleOccupancy> ObstacleOccupancies => obstacleOccupanciesView;

        public IReadOnlyList<MinionOccupancy> Minions => minionsView;

        public IReadOnlyList<PlacedToken> PlacedTokens => tokensView;

        /// <summary>
        /// Every persistent property currently supplied by a token on the board. This is derived from
        /// placed instances rather than copied into timing state, so removing a token removes its
        /// sources and card/hero Active-effect cancellation cannot touch them.
        /// </summary>
        public IReadOnlyList<ContinuousRuleSource> TokenPropertySources => Array.AsReadOnly(tokens
            .SelectMany(token => token.PropertySources)
            .ToArray());

        public IReadOnlyList<HexCoordinate> BoardCoordinates => boardCoordinatesView;

        internal bool IsOccupied(HexCoordinate coordinate) => occupancyIndex.IsOccupied(coordinate);

        /// <summary>
        /// Whether the coordinate is a playable space of the selected map. Anything else is terrain -
        /// a modelled space that exists and permanently blocks, see <see cref="IsTerrain"/> - or a
        /// coordinate that is not on the map at all. Since milestone 6.8e those are distinct answers:
        /// terrain is an obstacle the Move-Through permission can cross, off-board blocks everything.
        /// </summary>
        internal bool IsPlayable(HexCoordinate coordinate) => boardCoordinates.Contains(coordinate);

        /// <summary>
        /// Whether the coordinate is a declared terrain space of the selected map. Terrain is not an
        /// occupant and never enters the occupancy index; it exists on the board and always blocks
        /// endpoints, placement, and every occupancy consumer, while blocking transit only for a
        /// mover without the Move-Through permission (rulebook p.18). Milestone 6.8e.
        /// </summary>
        internal bool IsTerrain(HexCoordinate coordinate) => terrainCoordinates.Contains(coordinate);

        internal IReadOnlyCollection<HexCoordinate> OccupiedCoordinates => occupancyIndex.OccupiedCoordinates;

        internal IReadOnlyCollection<HexCoordinate> PlayableCoordinateSet => boardCoordinates;

        internal IReadOnlyCollection<HexCoordinate> TerrainCoordinateSet => terrainCoordinates;

        internal AdjacentMoveAuthority CreateValidationFork() => new AdjacentMoveAuthority(this);

        public BoardRestoreState CaptureRestoreState() =>
            new BoardRestoreState(
                occupancies,
                obstacleOccupancies,
                minions.Select(minion => new RestoredMinionPosition(minion.MinionId, minion.Coordinate)),
                tokens);

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
            return new AdjacentMoveAuthority(
                mapDefinition,
                state.HeroOccupancies,
                state.ObstacleOccupancies,
                remaining,
                state.PlacedTokens);
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
                return AdjacentMoveResult.Reject(terrainCoordinates.Contains(command.Destination)
                    ? AdjacentMoveRejectionReason.DestinationTerrain
                    : AdjacentMoveRejectionReason.DestinationOffBoard);
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

        /// <summary>
        /// Commits a whole adjacent route in one transaction, asserting occupancy only at its endpoint.
        ///
        /// Milestone 6.8c added this because rulebook p.18's Move-Through permission cannot be expressed
        /// one space at a time: a unit that "is allowed to enter a space with an obstacle and leave that
        /// space in any direction, while moving" never occupies that space, and the one-obstacle-per-space
        /// index would refuse it if it tried. So the route is validated whole - every space it enters for
        /// transit, its last space for an endpoint - and the occupant then moves from origin to endpoint
        /// with no intermediate index state existing at all.
        ///
        /// One event per space entered, exactly as the per-step commit produced, because the events
        /// narrate the movement and p.18 says the unit really does enter and leave those spaces. A route
        /// crossing nothing occupied therefore produces the identical event stream it always did.
        /// </summary>
        public AdjacentMoveRouteResult TryCommitRoute(
            HeroId heroId,
            IReadOnlyList<HexCoordinate> route,
            RuleModificationSet modifications)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            var heroIndex = occupancies.FindIndex(occupancy => occupancy.HeroId.Equals(heroId));
            if (heroIndex < 0)
            {
                return AdjacentMoveRouteResult.Reject(AdjacentMoveRejectionReason.UnknownHero, 0);
            }

            if (route.Count == 0)
            {
                return AdjacentMoveRouteResult.Reject(AdjacentMoveRejectionReason.DestinationNotAdjacent, 0);
            }

            var origin = occupancies[heroIndex].Coordinate;
            var mover = RuleObject.ForHero(heroId);
            var previous = origin;
            for (var index = 0; index < route.Count; index++)
            {
                var space = route[index];
                if (!boardCoordinates.Contains(space) && !terrainCoordinates.Contains(space))
                {
                    return AdjacentMoveRouteResult.Reject(AdjacentMoveRejectionReason.DestinationOffBoard, index);
                }

                if (!previous.GetNeighbors().Contains(space))
                {
                    return AdjacentMoveRouteResult.Reject(AdjacentMoveRejectionReason.DestinationNotAdjacent, index);
                }

                var isEndpoint = index == route.Count - 1;
                var refused = isEndpoint
                    ? ObstacleQuery.BlocksEndOfMovement(this, space)
                    : ObstacleQuery.BlocksTransit(this, space, mover, modifications);
                if (refused)
                {
                    return AdjacentMoveRouteResult.Reject(
                        terrainCoordinates.Contains(space)
                            ? AdjacentMoveRejectionReason.DestinationTerrain
                            : AdjacentMoveRejectionReason.DestinationOccupied,
                        index);
                }

                previous = space;
            }

            // Validated whole, so nothing below can refuse and leave a half-walked route behind.
            var destination = route[route.Count - 1];
            var events = new List<AdjacentMoveEvent>(route.Count);
            var from = origin;
            foreach (var space in route)
            {
                events.Add(new AdjacentMoveEvent(heroId, from, space));
                from = space;
            }

            occupancyIndex.Move(origin, destination);
            occupancies[heroIndex] = new HeroOccupancy(heroId, destination);
            return AdjacentMoveRouteResult.Accept(events);
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

        public bool TryGetHeroCoordinate(HeroId heroId, out HexCoordinate coordinate)
        {
            var index = occupancies.FindIndex(occupancy => occupancy.HeroId.Equals(heroId));
            coordinate = index < 0 ? default : occupancies[index].Coordinate;
            return index >= 0;
        }

        public bool IsEmptyPlayableSpace(HexCoordinate coordinate) =>
            boardCoordinates.Contains(coordinate) && !occupancyIndex.IsOccupied(coordinate);

        // ---------------------------------------------------------------------------------------
        // Milestone 6.4: the displacement port. Direct Move and Object Push relocate an object that
        // is already on the board without going through a Movement action, so these sit beside the
        // movement commits above rather than inside them - p.11 and p.18 keep the operations
        // distinct, and CARD_EFFECT_CAPABILITIES.md forbids treating them as interchangeable.
        // ---------------------------------------------------------------------------------------

        public bool TryGetObjectAt(HexCoordinate coordinate, out BoardObjectReference subject)
        {
            if (!occupancyIndex.TryGetOccupant(coordinate, out var occupant))
            {
                subject = default;
                return false;
            }

            switch (occupant.Kind)
            {
                case BoardOccupantKind.Hero:
                    subject = BoardObjectReference.ForHero(occupant.HeroId);
                    return true;
                case BoardOccupantKind.StandardMinion:
                    subject = BoardObjectReference.ForMinion(occupant.MinionId);
                    return true;
                case BoardOccupantKind.Token:
                    subject = BoardObjectReference.ForToken(occupant.TokenInstanceId);
                    return true;
                default:
                    // A generic obstacle occupies a space and blocks, but no rule yet displaces one,
                    // so it is reported as no displaceable subject rather than as an invented kind.
                    subject = default;
                    return false;
            }
        }

        public bool TryGetObjectCoordinate(BoardObjectReference subject, out HexCoordinate coordinate)
        {
            switch (subject.Kind)
            {
                case BoardObjectKind.Hero:
                    return TryGetHeroCoordinate(subject.HeroId, out coordinate);
                case BoardObjectKind.Minion:
                {
                    var index = minions.FindIndex(minion => minion.MinionId.Equals(subject.MinionId));
                    coordinate = index < 0 ? default : minions[index].Coordinate;
                    return index >= 0;
                }
                case BoardObjectKind.Token:
                {
                    var index = tokens.FindIndex(token => token.Id.Equals(subject.TokenInstanceId));
                    coordinate = index < 0 ? default : tokens[index].Coordinate;
                    return index >= 0;
                }
                default:
                    coordinate = default;
                    return false;
            }
        }

        public PushPathResult ResolvePush(
            HexCoordinate pusher,
            HexCoordinate target,
            int requestedDistance,
            RuleObject targetObject,
            bool pushIgnoresObstacles,
            RuleModificationSet modifications) =>
            PushPathQuery.Resolve(
                this, pusher, target, requestedDistance, targetObject, pushIgnoresObstacles, modifications);

        public IReadOnlyList<ReachableMovementDestination> FindDirectMoveDestinations(
            HeroId mover,
            int allowance,
            RuleModificationSet modifications)
        {
            var index = occupancies.FindIndex(occupancy => occupancy.HeroId.Equals(mover));
            if (index < 0)
            {
                return Array.Empty<ReachableMovementDestination>();
            }

            // The same reachability the Movement action uses. p.11: movement in the card text
            // "follows the same rules and restrictions as a Movement action", so a second traversal
            // written for effects would be a place for the two to disagree.
            return MovementReachabilityQuery.FindReachableDestinations(
                occupancies[index], this, allowance, modifications);
        }

        public DisplacementCommitResult CommitValidatedDisplacement(
            BoardObjectReference subject,
            HexCoordinate destination)
        {
            if (!TryGetObjectCoordinate(subject, out var origin))
            {
                return DisplacementCommitResult.Reject(DisplacementRejectionReason.UnknownObject);
            }

            if (origin.Equals(destination))
            {
                // A zero-space push (p.18) is a legal outcome that relocates nothing. Accepting it
                // here without touching the index keeps the caller from having to special-case it.
                return DisplacementCommitResult.Accept(subject, origin, destination);
            }

            if (!boardCoordinates.Contains(destination))
            {
                return DisplacementCommitResult.Reject(terrainCoordinates.Contains(destination)
                    ? DisplacementRejectionReason.DestinationTerrain
                    : DisplacementRejectionReason.DestinationOffBoard);
            }

            if (occupancyIndex.IsOccupied(destination))
            {
                return DisplacementCommitResult.Reject(DisplacementRejectionReason.DestinationOccupied);
            }

            switch (subject.Kind)
            {
                case BoardObjectKind.Hero:
                {
                    var index = occupancies.FindIndex(occupancy => occupancy.HeroId.Equals(subject.HeroId));
                    occupancyIndex.Move(origin, destination);
                    occupancies[index] = new HeroOccupancy(subject.HeroId, destination);
                    break;
                }

                case BoardObjectKind.Minion:
                {
                    var index = minions.FindIndex(minion => minion.MinionId.Equals(subject.MinionId));
                    var space = MapDefinition.Spaces.Single(candidate =>
                        candidate.Kind == BoardMapSpaceKind.Playable &&
                        candidate.Coordinate.Equals(destination));
                    occupancyIndex.Move(origin, destination);
                    minions[index] = minions[index].WithPosition(
                        new ConfiguredSpaceReference(MapDefinition.Id, space.Id), destination);
                    break;
                }

                case BoardObjectKind.Token:
                {
                    // p.18 names "a unit or a token" as push targets, so a token relocates like any
                    // other occupant. It keeps its instance identity, its owner, and its property
                    // sources: milestone 6.3 ties a token's continuous sources to its remaining
                    // placed, and moving it never stops it being placed.
                    var index = tokens.FindIndex(token => token.Id.Equals(subject.TokenInstanceId));
                    occupancyIndex.Move(origin, destination);
                    tokens[index] = tokens[index].WithCoordinate(destination);
                    break;
                }

                default:
                    return DisplacementCommitResult.Reject(DisplacementRejectionReason.UnknownObject);
            }

            return DisplacementCommitResult.Accept(subject, origin, destination);
        }

        public void CommitValidatedTokenPlacement(PlacedToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            if (tokens.Any(existing => existing.Id.Equals(token.Id)))
            {
                throw new InvalidOperationException("Token instance IDs must be unique on the board.");
            }

            if (!IsEmptyPlayableSpace(token.Coordinate))
            {
                throw new InvalidOperationException(
                    "A validated token placement destination is no longer empty and playable.");
            }

            // The supply is the definition's total minus what is already out; there is no separate
            // counter, so this cannot disagree with the board it is guarding.
            var placed = tokens.Count(existing => existing.Definition.Reference.Equals(token.Definition.Reference));
            if (placed >= token.Definition.SupplyCount)
            {
                throw new InvalidOperationException(
                    $"Token '{token.Definition.Reference}' has no copy left in the supply to place.");
            }

            occupancyIndex.Add(token.Coordinate, BoardOccupant.ForToken(token.Id), nameof(token));
            tokens.Add(token);
        }

        public PlacedToken CommitValidatedTokenReclamation(TokenInstanceId tokenInstanceId)
        {
            var index = tokens.FindIndex(token => token.Id.Equals(tokenInstanceId));
            if (index < 0)
            {
                throw new InvalidOperationException("A validated token is no longer on the board.");
            }

            var removed = tokens[index];
            tokens.RemoveAt(index);
            occupancyIndex.Remove(removed.Coordinate);
            return removed;
        }

        /// <summary>
        /// The token-specific first step of an already validated unit Spawn. General Spawn supply,
        /// eligibility, destination, and occupant creation remain the owning Spawn capability's job;
        /// this board operation only enforces rulebook pp.8/19 before that later commit occurs.
        /// </summary>
        /// <returns>The removed token, or <c>null</c> when the Spawn point is not token-covered.</returns>
        public PlacedToken CommitValidatedTokenRemovalBeforeUnitSpawn(
            ConfiguredSpawnPointReference spawnPoint)
        {
            if (!spawnPoint.MapId.Equals(MapDefinition.Id))
            {
                throw new InvalidOperationException(
                    "A validated unit Spawn must name a Spawn point on the selected map.");
            }

            var definition = MapDefinition.SpawnPoints.SingleOrDefault(candidate =>
                candidate.Id.Equals(spawnPoint.SpawnPointId));
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "A validated unit Spawn must name a current Spawn point.");
            }

            var coordinate = MapDefinition.Spaces.Single(space =>
                space.Id.Equals(definition.SpaceId) && space.Kind == BoardMapSpaceKind.Playable).Coordinate;
            var token = tokens.SingleOrDefault(candidate => candidate.Coordinate.Equals(coordinate));
            return token == null ? null : CommitValidatedTokenReclamation(token.Id);
        }
    }
}
