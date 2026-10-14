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
    public sealed class AdjacentMoveAuthority :
        ITokenPlacementAuthority, IBoardDisplacementAuthority, IUnitSpawnAuthority,
        IBoardObjectExchangeAuthority, ILaneAdvanceAuthority, IObstaclePlacementAuthority,
        IMinionBattleExclusionAuthority, ICompletedMovementWalkRecord, IUnitTurnHistory
    {
        // Milestone 8.114b1, schema v47. See IMinionBattleExclusionAuthority and CompletedMovementWalk.
        private readonly HashSet<MinionId> minionBattleExclusions = new HashSet<MinionId>();
        private CompletedMovementWalk lastCompletedMovementWalk;
        private readonly List<ZoneId> currentBattleZoneIds = new List<ZoneId>();

        // Milestone 9.21a, schema v49. The heroes that declare more than one body (RZ-Q1), and how
        // many each has in all. A hero absent from this map has one body, which is every hero the
        // build deals today; the one-space-per-hero rule is relaxed for the heroes named here only.
        private readonly Dictionary<HeroId, int> heroBodySupplies = new Dictionary<HeroId, int>();

        // Milestone 9.21b, schema v50. The acting body settled for a many-bodied hero's current action
        // (RZ-Q4): the player's pick when several bodies stand as his card resolves, or the one body
        // when one stands. Only heroes named in the supply map ever appear; the turn authority clears
        // the map whenever it selects the next actor.
        private readonly Dictionary<HeroId, int> actingBodies = new Dictionary<HeroId, int>();

        public IReadOnlyCollection<MinionId> MinionsExcludedFromMinionBattle =>
            minionBattleExclusions.OrderBy(value => value.Value, StringComparer.Ordinal).ToArray();

        public void ExcludeFromMinionBattle(IEnumerable<MinionId> excluded)
        {
            foreach (var minion in excluded ?? throw new ArgumentNullException(nameof(excluded)))
            {
                if (!minion.IsDefined)
                {
                    throw new ArgumentException("An exclusion names a minion.", nameof(excluded));
                }

                minionBattleExclusions.Add(minion);
            }
        }

        /// <summary>p.8: the battle is over, so nothing it excluded is excluded any longer.</summary>
        public void ClearMinionBattleExclusions() => minionBattleExclusions.Clear();

        public CompletedMovementWalk LastCompletedMovementWalk => lastCompletedMovementWalk;

        /// <summary>Records the whole route a Movement action just walked, replacing the last one.</summary>
        public void RecordCompletedMovementWalk(HeroId mover, IEnumerable<HexCoordinate> route) =>
            lastCompletedMovementWalk = new CompletedMovementWalk(mover, route);

        private readonly HashSet<HexCoordinate> boardCoordinates;
        private readonly HashSet<HexCoordinate> terrainCoordinates;
        private readonly List<HeroOccupancy> occupancies;
        private readonly List<ObstacleOccupancy> obstacleOccupancies;
        private readonly List<MinionOccupancy> minions;

        /// <summary>
        /// The declared minion miniatures that are not standing on the board - rulebook p.18's supply
        /// for a unit Spawn. It is board-adjacent runtime state rather than a second persisted list:
        /// a removal moves a miniature here, a Spawn moves one back, and a restore rebuilds it as the
        /// configured roster minus the restored positions, so nothing about it reaches the wire.
        ///
        /// Each entry keeps its identity-bound content - team, type, and Spawn point - and carries the
        /// declared setup placement in its position fields, which mean nothing while the miniature is
        /// off the board and are replaced when it Spawns.
        /// </summary>
        private readonly List<MinionOccupancy> reserveMinions;

        private readonly List<PlacedToken> tokens;

        /// <summary>
        /// The displacements of heroes that entered and left a space a trap was standing on, since the
        /// rules layer last took them. Milestone 8.61e, and it is board-adjacent within-command state
        /// exactly as <see cref="reserveMinions"/> is board-adjacent runtime state.
        ///
        /// <para><b>Why the board records the fact and prices none of it.</b> "After an enemy hero moves
        /// through a mine, remove it" needs two things this type has and one it does not: it has the
        /// route and the pieces standing on it, and it has no idea whose team anybody is on. So the
        /// crossing is recorded here, where every displacement already passes, and
        /// <c>MoveThroughTrapOperation</c> answers p.19's sides and takes the pieces off at the one
        /// altitude that holds the participants. A board with no trap standing on the route records
        /// nothing at all, so every match that has never seen one of Min's mines allocates nothing and
        /// behaves byte for byte as it did.</para>
        ///
        /// <para><b>It is never persisted, and does not need to be.</b> A crossing is recorded and taken
        /// inside one command - between the displacement and the consequence the rules run for it - and
        /// what a pause carries across a snapshot is the trigger frame that consequence opened, which
        /// the card-action payload already writes. A validation fork starts with none, because a fork
        /// exists to answer a question without doing anything.</para>
        /// </summary>
        private readonly List<MoveThroughCrossing> moveThroughCrossings = new List<MoveThroughCrossing>();

        private readonly BoardOccupancyIndex occupancyIndex;
        private readonly List<UnitTurnPosition> currentTurnStart = new List<UnitTurnPosition>();
        private readonly List<UnitTurnPosition> previousTurnStart = new List<UnitTurnPosition>();
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
                        : MinionOccupancy.FromSetup(minion)),
                Enumerable.Empty<PlacedToken>(),
                QuickGameSetupAuthority.CreateLaneReserve(mapDefinition, initialMinions)
                    .Select(MinionOccupancy.FromSetup),
                Enumerable.Empty<HeroBodySupply>())
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

        /// <summary>
        /// Milestone 9.21a. A board on which the named heroes may stand on several spaces at once,
        /// each up to its declared supply of bodies (RZ-Q1). Every other hero keeps one space.
        /// </summary>
        public AdjacentMoveAuthority(
            BoardMapDefinition mapDefinition,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies,
            IEnumerable<QuickGameMinionSetupState> initialMinions,
            IEnumerable<HeroBodySupply> heroBodySupplies)
            : this(
                mapDefinition,
                initialOccupancies,
                initialObstacleOccupancies,
                (initialMinions ?? throw new ArgumentNullException(nameof(initialMinions)))
                    .Select(minion => minion == null
                        ? throw new ArgumentException("Initial minions must be non-null and have unique IDs.", nameof(initialMinions))
                        : MinionOccupancy.FromSetup(minion)),
                Enumerable.Empty<PlacedToken>(),
                QuickGameSetupAuthority.CreateLaneReserve(mapDefinition, initialMinions)
                    .Select(MinionOccupancy.FromSetup),
                heroBodySupplies)
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
            : this(
                mapDefinition, initialOccupancies, initialObstacleOccupancies, initialMinions,
                initialTokens, Enumerable.Empty<MinionOccupancy>(), Enumerable.Empty<HeroBodySupply>())
        {
        }

        // The reserve arm, for a restore that knows the whole configured roster and only some of it is
        // standing. W-078 also supplies the two-lane setup reserve here; removal returns a miniature
        // to the same pool during play.
        private AdjacentMoveAuthority(
            BoardMapDefinition mapDefinition,
            IEnumerable<HeroOccupancy> initialOccupancies,
            IEnumerable<ObstacleOccupancy> initialObstacleOccupancies,
            IEnumerable<MinionOccupancy> initialMinions,
            IEnumerable<PlacedToken> initialTokens,
            IEnumerable<MinionOccupancy> initialReserveMinions,
            IEnumerable<HeroBodySupply> initialHeroBodySupplies)
        {
            MapDefinition = mapDefinition ?? throw new ArgumentNullException(nameof(mapDefinition));

            // Milestone 9.21a. Read before the occupancies, because they are what decides whether a
            // second occupancy of one hero is a second body or a duplicate.
            foreach (var supply in initialHeroBodySupplies ??
                throw new ArgumentNullException(nameof(initialHeroBodySupplies)))
            {
                if (!supply.HeroId.IsDefined || heroBodySupplies.ContainsKey(supply.HeroId))
                {
                    throw new ArgumentException(
                        "A hero body supply names a defined hero once.", nameof(initialHeroBodySupplies));
                }

                if (supply.Bodies > 1)
                {
                    heroBodySupplies.Add(supply.HeroId, supply.Bodies);
                }
            }

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

            if (initialReserveMinions == null)
            {
                throw new ArgumentNullException(nameof(initialReserveMinions));
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

                // Milestone 9.21a. A hero that declares no bodies still stands on one space - the
                // guard every earlier board had, unchanged for it. A hero that declares a supply may
                // stand once per body up to that supply, and never twice as the same body.
                if (occupancies.Any(existing => existing.HeroId.Equals(occupancy.HeroId) &&
                        (existing.Body == occupancy.Body || HeroBodySupplyOf(occupancy.HeroId) == 1)))
                {
                    throw new ArgumentException("Initial occupancies must have unique hero IDs.", nameof(initialOccupancies));
                }

                if (occupancy.Body > HeroBodySupplyOf(occupancy.HeroId))
                {
                    throw new ArgumentException(
                        $"Initial occupancy names body {occupancy.Body} of {occupancy.HeroId}, whose " +
                        $"supply is {HeroBodySupplyOf(occupancy.HeroId)}.",
                        nameof(initialOccupancies));
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

            // The supply is validated for identity alone: a reserve miniature stands nowhere, so no
            // space, occupancy, or map question applies to it until it Spawns.
            var materializedReserve = initialReserveMinions.ToArray();
            if (materializedReserve.Any(minion => minion == null) ||
                materializedReserve.Select(minion => minion.MinionId).Distinct().Count() != materializedReserve.Length ||
                materializedReserve.Any(minion =>
                    materializedMinions.Any(placed => placed.MinionId.Equals(minion.MinionId))))
            {
                throw new ArgumentException(
                    "Reserve minions must be non-null, uniquely identified, and absent from the board.",
                    nameof(initialReserveMinions));
            }

            // Milestone 6.10. The opening value of p.8's Battle Zone is the one the map flags, and this
            // is the only place the flag decides it: from here on the zone is authoritative state that
            // the Lane-Push Zone stage advances, so every rules question reads this field rather than
            // the flag. A map that declares no starting Battle Zone - every synthetic board - opens
            // with the named absence rather than with a guess drawn from where minions happen to stand.
            currentBattleZoneIds.AddRange(BattleZoneQuery.GetStartingBattleZones(mapDefinition)
                .Select(zone => zone.Id));

            minions = new List<MinionOccupancy>(materializedMinions);
            reserveMinions = new List<MinionOccupancy>(materializedReserve);
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
            currentBattleZoneIds.AddRange(source.currentBattleZoneIds);
            boardCoordinates = source.boardCoordinates;
            terrainCoordinates = source.terrainCoordinates;
            occupancies = new List<HeroOccupancy>(source.occupancies);
            obstacleOccupancies = new List<ObstacleOccupancy>(source.obstacleOccupancies);
            minions = new List<MinionOccupancy>(source.minions);
            reserveMinions = new List<MinionOccupancy>(source.reserveMinions);
            tokens = new List<PlacedToken>(source.tokens);
            minionBattleExclusions = new HashSet<MinionId>(source.minionBattleExclusions);
            heroBodySupplies = new Dictionary<HeroId, int>(source.heroBodySupplies);
            actingBodies = new Dictionary<HeroId, int>(source.actingBodies);
            lastCompletedMovementWalk = source.lastCompletedMovementWalk;
            occupancyIndex = source.occupancyIndex.Clone();
            currentTurnStart.AddRange(source.currentTurnStart);
            previousTurnStart.AddRange(source.previousTurnStart);
            occupanciesView = occupancies.AsReadOnly();
            obstacleOccupanciesView = obstacleOccupancies.AsReadOnly();
            minionsView = minions.AsReadOnly();
            tokensView = tokens.AsReadOnly();
            boardCoordinatesView = source.boardCoordinatesView;
        }

        /// <summary>
        /// Every hero body on the board. Milestone 9.21a: a many-bodied hero appears once per body
        /// standing, so a presence, adjacency or obstacle read over this list sees every body (RZ-Q2);
        /// a read that wants one space per hero asks <see cref="TryGetHeroCoordinate"/> (the acting
        /// body) or <see cref="GetHeroBodies"/> (all of them) instead of taking a single row.
        /// </summary>
        public IReadOnlyList<HeroOccupancy> Occupancies => occupanciesView;

        /// <summary>
        /// Milestone 9.21a. The heroes that declare more than one body, by hero id. Every hero not
        /// listed has one body.
        /// </summary>
        public IReadOnlyList<HeroBodySupply> HeroBodySupplies =>
            Array.AsReadOnly(heroBodySupplies
                .OrderBy(entry => entry.Key.Value, StringComparer.Ordinal)
                .Select(entry => new HeroBodySupply(entry.Key, entry.Value))
                .ToArray());

        /// <summary>
        /// Milestone 9.21b. The acting bodies settled for the current action, by hero id - empty for
        /// every hero with one body.
        /// </summary>
        public IReadOnlyList<HeroActingBody> ActingBodies =>
            Array.AsReadOnly(actingBodies
                .OrderBy(entry => entry.Key.Value, StringComparer.Ordinal)
                .Select(entry => new HeroActingBody(entry.Key, entry.Value))
                .ToArray());

        /// <summary>
        /// Milestone 9.21b. The body settled to perform this many-bodied hero's current action (RZ-Q4),
        /// whether or not it still stands; false when none is settled.
        /// </summary>
        public bool TryGetActingBody(HeroId heroId, out int body) =>
            actingBodies.TryGetValue(heroId, out body);

        /// <summary>
        /// Milestone 9.21b. Settles the body that performs this hero's current action. Only a hero
        /// that declares more than one body has one, and only a body that stands may be settled.
        /// </summary>
        internal void SettleActingBody(HeroId heroId, int body)
        {
            if (HeroBodySupplyOf(heroId) < 2)
            {
                throw new InvalidOperationException(
                    $"{heroId} declares one body; an acting body is settled only for a many-bodied hero.");
            }

            if (!TryGetHeroBodyCoordinate(heroId, body, out _))
            {
                throw new InvalidOperationException(
                    $"Body {body} of {heroId} does not stand, so it cannot be the acting body.");
            }

            actingBodies[heroId] = body;
        }

        /// <summary>
        /// Milestone 9.21b. Forgets every settled acting body - called when the turn selects its next
        /// actor, so the pick is asked again the next time a many-bodied hero's card resolves.
        /// </summary>
        internal void ClearActingBodies() => actingBodies.Clear();

        /// <summary>
        /// Milestone 9.21h. "A different you" performs a repeated instance: the body standing on this
        /// space becomes the acting body, so every read of the repeated instance - its adjacency, its
        /// Range, its spawn's Radius - is measured from it (RZ-Q11, RZ-Q12). It stays settled until the
        /// turn selects its next actor, as a picked body does.
        /// </summary>
        internal void SettleActingBodyAt(HeroId heroId, HexCoordinate space)
        {
            var index = occupancies.FindIndex(occupancy =>
                occupancy.HeroId.Equals(heroId) && occupancy.Coordinate.Equals(space));
            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"No body of {heroId} stands on {space}, so it cannot perform the repeat.");
            }

            SettleActingBody(heroId, occupancies[index].Body);
        }

        /// <summary>
        /// Milestone 9.21h. Answers a feasibility probe as if this body were acting, and leaves the
        /// settled acting body exactly as it was - the question "could the repeat start from body N"
        /// that a "different you" repeat asks before it offers N. Body 0 asks with the body acting now;
        /// a body that does not stand cannot perform anything.
        /// </summary>
        internal bool ProbeWithActingBody(HeroId heroId, int body, Func<bool> probe)
        {
            if (probe == null) throw new ArgumentNullException(nameof(probe));
            if (body <= 0)
            {
                return probe();
            }

            if (!TryGetHeroBodyCoordinate(heroId, body, out _))
            {
                return false;
            }

            var had = actingBodies.TryGetValue(heroId, out var previous);
            actingBodies[heroId] = body;
            try
            {
                return probe();
            }
            finally
            {
                if (had)
                {
                    actingBodies[heroId] = previous;
                }
                else
                {
                    actingBodies.Remove(heroId);
                }
            }
        }

        /// <summary>Milestone 9.21a. How many bodies this hero has in all; 1 unless it declares more.</summary>
        public int HeroBodySupplyOf(HeroId heroId) =>
            heroBodySupplies.TryGetValue(heroId, out var bodies) ? bodies : 1;

        /// <summary>
        /// Milestone 9.21a. Whether any body of this hero stands on the board - p.7's respawn question,
        /// p.16's "as long as your hero is on the board", and every other presence check that asks
        /// about the hero rather than one of its spaces.
        /// </summary>
        public bool IsHeroOnBoard(HeroId heroId) =>
            occupancies.Any(occupancy => occupancy.HeroId.Equals(heroId));

        /// <summary>Milestone 9.21a. Every body of this hero on the board, in body order.</summary>
        public IReadOnlyList<HeroOccupancy> GetHeroBodies(HeroId heroId) =>
            Array.AsReadOnly(occupancies
                .Where(occupancy => occupancy.HeroId.Equals(heroId))
                .OrderBy(occupancy => occupancy.Body)
                .ToArray());

        /// <summary>
        /// Milestone 9.21a. Where one named body of a hero stands - the read for a seam that knows
        /// which body it means (the attacked body of an Attack, the defending body of a Defense).
        /// </summary>
        public bool TryGetHeroBodyCoordinate(HeroId heroId, int body, out HexCoordinate coordinate)
        {
            var index = occupancies.FindIndex(occupancy =>
                occupancy.HeroId.Equals(heroId) && occupancy.Body == body);
            coordinate = index < 0 ? default : occupancies[index].Coordinate;
            return index >= 0;
        }

        /// <summary>
        /// Milestone 9.21h (RZ-Q13, settled by the user as "any body"). The spaces a radius "of you" is
        /// measured from when the hero it is anchored on is not acting - an Active effect's disk around
        /// its controller, a Defeat-bounty promise's, a before-action trigger's: every body standing, in
        /// body order. A space is in radius of him if it is in radius of at least one body, so a reader
        /// asks whether ANY anchor holds it (<see cref="IsWithinRadiusOfAnyBody"/>). A hero with one body
        /// has one anchor, exactly as before; a hero off the board has none, and reaches nothing.
        ///
        /// <para>It replaces 9.21a's <c>TryGetSoleHeroCoordinate</c>, which refused a many-bodied hero
        /// loudly at these reads until the reading was settled.</para>
        /// </summary>
        public IReadOnlyList<HexCoordinate> GetHeroRadiusAnchors(HeroId heroId) =>
            Array.AsReadOnly(GetHeroBodies(heroId).Select(body => body.Coordinate).ToArray());

        /// <summary>
        /// Milestone 9.21h (RZ-Q13). Whether a space lies within <paramref name="radius"/> of at least
        /// one of these anchors - p.9's inclusive disk, drawn around every body of a many-bodied hero.
        /// </summary>
        public static bool IsWithinRadiusOfAnyBody(
            IReadOnlyList<HexCoordinate> anchors, int radius, HexCoordinate space) =>
            anchors != null && anchors.Any(anchor => HexDistanceQuery.Calculate(anchor, space) <= radius);

        /// <summary>
        /// Milestone 9.21a. The index of the body that answers for the hero as a whole: the only body
        /// when one stands, and the ACTING body when several do. Milestone 9.21b: the acting body is the
        /// one settled for the current action (the player's pick, RZ-Q4) while it stands; with none
        /// settled - another hero's turn - it is <see cref="HeroOccupancy.FirstBody"/>, and a board on
        /// which several bodies stand and that one does not has no acting body to name, and the read
        /// refuses loudly rather than silently taking whichever body happens to be listed first. -1
        /// when no body stands.
        /// </summary>
        private int ActingBodyIndex(HeroId heroId)
        {
            if (actingBodies.TryGetValue(heroId, out var settled))
            {
                var acting = occupancies.FindIndex(occupancy =>
                    occupancy.HeroId.Equals(heroId) && occupancy.Body == settled);
                if (acting >= 0)
                {
                    return acting;
                }
            }

            var only = -1;
            var firstBody = -1;
            var count = 0;
            for (var index = 0; index < occupancies.Count; index++)
            {
                if (!occupancies[index].HeroId.Equals(heroId))
                {
                    continue;
                }

                count++;
                only = index;
                if (occupancies[index].Body == HeroOccupancy.FirstBody)
                {
                    firstBody = index;
                }
            }

            if (count <= 1)
            {
                return only;
            }

            if (firstBody >= 0)
            {
                return firstBody;
            }

            throw new InvalidOperationException(
                $"{heroId} stands on {count} spaces and none of them is body {HeroOccupancy.FirstBody}, " +
                "the default acting body, and no acting body is settled; a read that names the hero " +
                "rather than a body cannot say which space it means (RZ-Q4).");
        }

        /// <summary>Milestone 9.21a. The index of the body of this hero standing on this space, or -1.</summary>
        private int BodyIndexAt(HeroId heroId, HexCoordinate coordinate) =>
            occupancies.FindIndex(occupancy =>
                occupancy.HeroId.Equals(heroId) && occupancy.Coordinate.Equals(coordinate));

        public BoardMapDefinition MapDefinition { get; }

        public MapDefinitionReference SelectedMap => MapDefinition.Reference;

        /// <summary>
        /// Rulebook p.8's Battle Zone, as authoritative map-scoped state rather than a derivation
        /// (milestone 6.10).
        ///
        /// It opens at the zone the map flags and moves only through
        /// <see cref="CommitValidatedBattleZoneAdvance"/>, so it is independent of presentation and of
        /// where minions happen to be standing: an emptied Battle Zone is still the Battle Zone, and a
        /// zone full of minions that the Lane never reached is not one. That independence is the whole
        /// point of holding it here - inferring it from occupancy would answer p.8's own question with
        /// the board state p.8 is about to rearrange.
        ///
        /// The named absence belongs to boards whose map declares no starting Battle Zone; on those the
        /// Minion Battle, the Heavy-immunity accompaniment, the displaced-minion correction and the
        /// Lane-Push cause all report "no zone" exactly as they did before this field existed.
        /// </summary>
        public ZoneId? CurrentBattleZoneId => currentBattleZoneIds.Count == 0
            ? (ZoneId?)null
            : currentBattleZoneIds[0];
        public IReadOnlyList<ZoneId> CurrentBattleZoneIds => currentBattleZoneIds.AsReadOnly();

        public IReadOnlyList<ObstacleOccupancy> ObstacleOccupancies => obstacleOccupanciesView;

        public bool TryGetObstacleCoordinate(ObstacleId obstacleId, out HexCoordinate coordinate)
        {
            var index = obstacleOccupancies.FindIndex(value => value.ObstacleId.Equals(obstacleId));
            coordinate = index < 0 ? default : obstacleOccupancies[index].Coordinate;
            return index >= 0;
        }

        public bool TryPlaceOrRepositionObstacle(ObstacleId obstacleId, HexCoordinate destination)
        {
            if (!obstacleId.IsDefined) throw new ArgumentException("Obstacle ID is required.", nameof(obstacleId));
            if (!IsEmptyPlayableSpace(destination)) return false;

            var index = obstacleOccupancies.FindIndex(value => value.ObstacleId.Equals(obstacleId));
            if (index < 0)
            {
                occupancyIndex.Add(destination, BoardOccupant.ForObstacle(obstacleId), nameof(destination));
                obstacleOccupancies.Add(new ObstacleOccupancy(obstacleId, destination));
                return true;
            }

            var origin = obstacleOccupancies[index].Coordinate;
            occupancyIndex.Move(origin, destination);
            obstacleOccupancies[index] = new ObstacleOccupancy(obstacleId, destination);
            return true;
        }

        public bool TryRemoveObstacle(ObstacleId obstacleId)
        {
            if (!obstacleId.IsDefined) throw new ArgumentException("Obstacle ID is required.", nameof(obstacleId));
            var index = obstacleOccupancies.FindIndex(value => value.ObstacleId.Equals(obstacleId));
            if (index < 0) return false;

            occupancyIndex.Remove(obstacleOccupancies[index].Coordinate);
            obstacleOccupancies.RemoveAt(index);
            return true;
        }

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

        /// <summary>Capture the board at the reveal checkpoint, before turn actions resolve.</summary>
        public void RecordTurnStart()
        {
            previousTurnStart.Clear();
            previousTurnStart.AddRange(currentTurnStart);
            currentTurnStart.Clear();
            foreach (var hero in occupancies)
                currentTurnStart.Add(new UnitTurnPosition(
                    BoardObjectReference.ForHeroBody(hero.HeroId, hero.Body), hero.Coordinate,
                    occupancyIndex.MutationCount(BoardOccupant.ForHero(hero.HeroId))));
            foreach (var minion in minions)
                currentTurnStart.Add(new UnitTurnPosition(
                    BoardObjectReference.ForMinion(minion.MinionId), minion.Coordinate,
                    occupancyIndex.MutationCount(BoardOccupant.ForMinion(minion.MinionId))));
        }

        private int SubjectBody(BoardObjectReference unit) => unit.Kind == BoardObjectKind.Hero
            ? (unit.HeroBody > 0 ? unit.HeroBody :
                actingBodies.TryGetValue(unit.HeroId, out var acting) ? acting : HeroOccupancy.FirstBody)
            : 0;

        private static bool SameUnit(UnitTurnPosition position, BoardObjectReference unit, int body) =>
            position.Unit.Equals(unit) &&
            (unit.Kind != BoardObjectKind.Hero || position.Unit.HeroBody == body);

        public bool TryGetTurnStartPosition(BoardObjectReference unit, out HexCoordinate coordinate)
        {
            var body = SubjectBody(unit);
            foreach (var position in currentTurnStart)
                if (SameUnit(position, unit, body))
                {
                    coordinate = position.Coordinate;
                    return true;
                }
            coordinate = default;
            return false;
        }

        /// <summary>True only if this unit has never changed board position since the prior turn began.</summary>
        public bool RemainedSincePreviousTurn(BoardObjectReference unit)
        {
            var body = SubjectBody(unit);
            foreach (var position in previousTurnStart)
                if (SameUnit(position, unit, body))
                {
                    if (!TryGetObjectCoordinate(unit, out var current) ||
                        !current.Equals(position.Coordinate)) return false;
                    var occupant = unit.Kind == BoardObjectKind.Hero
                        ? BoardOccupant.ForHero(unit.HeroId)
                        : BoardOccupant.ForMinion(unit.MinionId);
                    return occupancyIndex.MutationCount(occupant) == position.Mutation;
                }
            return false;
        }

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
            BoardRestoreState.ForBattleZones(
                occupancies,
                obstacleOccupancies,
                minions.Select(minion => new RestoredMinionPosition(minion.MinionId, minion.Coordinate)),
                tokens,
                CurrentBattleZoneIds,
                minionBattleExclusions,
                lastCompletedMovementWalk,
                HeroBodySupplies,
                ActingBodies,
                currentTurnStart,
                previousTurnStart,
                occupancyIndex.UnitMutations);

        public static AdjacentMoveAuthority Restore(
            BoardRestoreState state,
            BoardMapDefinition mapDefinition,
            IEnumerable<QuickGameMinionSetupState> configuredMinions)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (mapDefinition == null) throw new ArgumentNullException(nameof(mapDefinition));
            if (configuredMinions == null) throw new ArgumentNullException(nameof(configuredMinions));

            var configured = configuredMinions.ToArray();
            var available = configured.Concat(
                QuickGameSetupAuthority.CreateLaneReserve(mapDefinition, configured)).ToArray();
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
            }).ToArray();

            // Milestone 6.5's supply, rebuilt rather than read: every declared miniature the restored
            // positions do not account for is one that has left the board, which is exactly p.18's
            // supply. Deriving it here is what keeps it off the wire - a stored supply would be a
            // second count that could disagree with the positions beside it.
            var reserve = available
                .Where(minion => !state.RemainingMinions.Any(position =>
                    position.MinionId.Equals(minion.MinionId)))
                .Select(MinionOccupancy.FromSetup)
                .ToArray();

            var restored = new AdjacentMoveAuthority(
                mapDefinition,
                state.HeroOccupancies,
                state.ObstacleOccupancies,
                remaining,
                state.PlacedTokens,
                reserve,
                state.HeroBodySupplies);

            // Milestone 9.21b, schema v50. Read after the supplies the constructor has just declared,
            // because an acting body belongs only to a hero that declares bodies.
            foreach (var acting in state.ActingBodies)
            {
                if (restored.HeroBodySupplyOf(acting.HeroId) < acting.Body ||
                    restored.HeroBodySupplyOf(acting.HeroId) < 2)
                {
                    throw new ArgumentException(
                        "A restored acting body names a hero that declares bodies, within its supply.",
                        nameof(state));
                }

                restored.actingBodies[acting.HeroId] = acting.Body;
            }

            // Milestone 6.10. The constructor has already opened the board at the zone the map flags,
            // and this replaces that opening value with the restored one - never the other way round.
            // A restored zone the selected map does not declare, or that the map declares off the
            // ordered Lane, is refused here rather than quietly falling back to the flag: the whole
            // reason the zone is persisted is that no derivation from setup or occupancy can tell which
            // zone a pushed Lane reached, so a fallback would restore a match to the wrong board.
            // Refusing throws, which is what makes it atomic - the caller receives no half-restored
            // authority, exactly as a mismatched minion identity is refused above.
            if (restored.CurrentBattleZoneIds.Count != state.CurrentBattleZoneIds.Count)
            {
                throw new ArgumentException(
                    "A restored Battle Zone must be present for every Lane the selected map declares.",
                    nameof(state));
            }

            for (var laneIndex = 0; laneIndex < state.CurrentBattleZoneIds.Count; laneIndex++)
            {
                var zoneId = state.CurrentBattleZoneIds[laneIndex];
                if (!BattleZoneQuery.CanBeCurrentBattleZone(mapDefinition, zoneId) ||
                    (mapDefinition.OrderedLanes.Count > 0 &&
                        !mapDefinition.OrderedLanes[laneIndex].Contains(zoneId)))
                {
                    throw new ArgumentException(
                        $"Restored Battle Zone '{zoneId}' is not on Lane {laneIndex + 1} of map '{mapDefinition.Reference}'.",
                        nameof(state));
                }
            }
            restored.currentBattleZoneIds.Clear();
            restored.currentBattleZoneIds.AddRange(state.CurrentBattleZoneIds);

            // Milestone 8.114b1, schema v47.
            restored.ExcludeFromMinionBattle(state.MinionBattleExclusions);
            restored.lastCompletedMovementWalk = state.LastCompletedMovementWalk;
            restored.currentTurnStart.AddRange(state.CurrentTurnStart);
            restored.previousTurnStart.AddRange(state.PreviousTurnStart);
            if (state.UnitMutations.Count > 0)
                restored.occupancyIndex.RestoreUnitMutations(state.UnitMutations);
            return restored;
        }

        public AdjacentMoveResult TryCommit(AdjacentMoveCommand command)
        {
            if (!IsHeroOnBoard(command.HeroId))
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.UnknownHero);
            }

            // Milestone 9.21a. The command names its origin, which for a many-bodied hero is also the
            // name of the body that steps; a hero with one body is found exactly as before.
            var heroIndex = BodyIndexAt(command.HeroId, command.Origin);
            if (heroIndex < 0)
            {
                return AdjacentMoveResult.Reject(AdjacentMoveRejectionReason.WrongOrigin);
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
            occupancies[heroIndex] = occupancies[heroIndex].MovedTo(command.Destination);
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
            RuleModificationSet modifications,
            bool moverIgnoresObstacles = false)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            var heroIndex = ActingBodyIndex(heroId);
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
                    ? ObstacleQuery.BlocksEndOfMovement(this, space, modifications, mover)
                    // Milestone 8.35z30, RULING 47. This validates the route a Movement
                    // action walked, so it asks the permission question as that action - the same ask
                    // MovementReachabilityQuery's hero overload made when it offered the route.
                    : ObstacleQuery.BlocksTransit(
                        this, space, mover, modifications,
                        moverIgnoresObstacles: moverIgnoresObstacles,
                        permittedActivity: RuleActivity.Movement);
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
            occupancies[heroIndex] = occupancies[heroIndex].MovedTo(destination);

            // Milestone 8.61e. Every space of the route but its last was entered AND left, which is
            // exactly p.18's "moves through" - the page's own reason this commit is whole rather than
            // per space. The last one is where the hero is standing, and p.18 lets nobody stand on an
            // obstacle, so it can never have carried a trap anyway.
            RecordMoveThroughCrossing(
                heroId, Array.AsReadOnly(route.Take(route.Count - 1).ToArray()));
            return AdjacentMoveRouteResult.Accept(events);
        }

        internal FastTravelEvent CommitValidatedFastTravel(HeroId heroId, HexCoordinate destination)
        {
            var heroIndex = ActingBodyIndex(heroId);
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
            occupancies[heroIndex] = occupancies[heroIndex].MovedTo(destination);
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

            // A miniature that leaves the board is back in the supply (p.18's Respawn states the
            // ordering outright: "a minion must first be removed, before it can be respawned"), so
            // the Spawn supply is live rather than fixed at setup.
            reserveMinions.Add(removed);
            return removed;
        }

        /// <summary>
        /// p.13's "Remove the defeated hero from the board", every body at once. Milestone 9.21a
        /// (RZ-Q3, Stunt Doubles' "If defeated, remove all of you"): a Defeat is the hero's, so it
        /// takes every body that stands, in body order; a hero with one body loses exactly the one
        /// occupancy it always did.
        /// </summary>
        internal IReadOnlyList<HeroOccupancy> CommitValidatedHeroRemoval(HeroId heroId)
        {
            var removed = GetHeroBodies(heroId);
            if (removed.Count == 0)
            {
                throw new InvalidOperationException("A validated hero target is no longer on the board.");
            }

            foreach (var body in removed)
            {
                occupancies.Remove(body);
                occupancyIndex.Remove(body.Coordinate);
            }

            // Milestone 9.21b. A hero with no body standing has no acting body; a respawn settles one.
            actingBodies.Remove(heroId);
            return removed;
        }

        /// <summary>
        /// p.7's respawn: a hero with no body on the board returns as one body, body 1, however many
        /// it had before (milestone 9.21a). A hero any of whose bodies still stands is not respawned.
        /// </summary>
        internal bool CommitValidatedHeroRespawn(HeroId heroId, HexCoordinate destination)
        {
            if (IsHeroOnBoard(heroId) ||
                !IsEmptyPlayableSpace(destination))
            {
                return false;
            }

            occupancyIndex.Add(destination, BoardOccupant.ForHero(heroId), nameof(heroId));
            occupancies.Add(new HeroOccupancy(heroId, HeroOccupancy.FirstBody, destination));
            return true;
        }

        /// <summary>
        /// W-022, PVE-D05/D12. A mode's round-start placement of a hero not yet on the board: the same
        /// board mutation as <see cref="CommitValidatedHeroRespawn"/>, named for what it is.
        /// </summary>
        internal bool CommitValidatedOffBoardHeroPlacement(HeroId heroId, HexCoordinate destination) =>
            CommitValidatedHeroRespawn(heroId, destination);

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

        /// <summary>
        /// Where the hero stands as ONE space: its only body, or - for a many-bodied hero (milestone
        /// 9.21a) - its acting body, the one settled for the current action (9.21b's pick). Every
        /// caller of this read is either the actor's own space or has been moved to
        /// <see cref="IsHeroOnBoard"/>, <see cref="GetHeroBodies"/> or
        /// <see cref="TryGetHeroBodyCoordinate"/>; see the site list in Rules/HERO_RAZZLE.md.
        /// </summary>
        public bool TryGetHeroCoordinate(HeroId heroId, out HexCoordinate coordinate)
        {
            var index = ActingBodyIndex(heroId);
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
                    // Milestone 9.21c. A many-bodied hero's space names the body standing on it, so a
                    // selection that picked this space reads, pushes, swaps and moves that body
                    // (RZ-Q2). A hero with one body is named as it always was.
                    subject = HeroBodySupplyOf(occupant.HeroId) > 1
                        ? BoardObjectReference.ForHeroBody(
                            occupant.HeroId, occupancies[BodyIndexAt(occupant.HeroId, coordinate)].Body)
                        : BoardObjectReference.ForHero(occupant.HeroId);
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
                    // Milestone 9.21c. A reference naming a body is read at that body; one naming the
                    // hero alone at its only (or acting) body, as before.
                    return subject.HeroBody > 0
                        ? TryGetHeroBodyCoordinate(subject.HeroId, subject.HeroBody, out coordinate)
                        : TryGetHeroCoordinate(subject.HeroId, out coordinate);
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
            BoardObjectReference mover,
            int allowance,
            RuleModificationSet modifications,
            bool moveIgnoresObstacles,
            RuleActivity permittedActivity = RuleActivity.None)
        {
            if (!TryGetObjectCoordinate(mover, out var origin))
            {
                return Array.Empty<ReachableMovementDestination>();
            }

            // The same reachability the Movement action uses. p.11: movement in the card text
            // "follows the same rules and restrictions as a Movement action", so a second traversal
            // written for effects would be a place for the two to disagree. A token's rules object is
            // the named absence, which the transit predicate already reads as holding no permission.
            return MovementReachabilityQuery.FindReachableDestinations(
                origin, mover.AsRuleObject(), this, allowance, modifications, moveIgnoresObstacles,
                permittedActivity);
        }

        public IReadOnlyList<ReachableMovementDestination> FindEmptyPathRoutes(
            HexCoordinate origin,
            int maximumDistance) =>
            MovementReachabilityQuery.FindReachableThroughEmptySpaces(origin, this, maximumDistance);

        public bool CanMoveCoupled(BoardObjectReference first, BoardObjectReference second, HexDirection direction,
            int distance, RuleModificationSet modifications, bool ignoresObstacles) =>
            CoupledMoveQuery.CanMove(this, first, second, direction, distance, modifications, ignoresObstacles);

        public bool TryMoveCoupled(BoardObjectReference first, BoardObjectReference second, HexDirection direction,
            int distance, RuleModificationSet modifications, bool ignoresObstacles)
        {
            if (!CanMoveCoupled(first, second, direction, distance, modifications, ignoresObstacles)) return false;
            TryGetObjectCoordinate(first, out var firstAt);
            TryGetObjectCoordinate(second, out var secondAt);
            var firstEnd = StraightLineQuery.Step(firstAt, direction, distance);
            var secondEnd = StraightLineQuery.Step(secondAt, direction, distance);
            // Milestone 8.61e. A coupled move walks a straight line exactly as p.18's Push does, so the
            // spaces it entered and left are the realised line between where each object started and
            // where it stopped.
            var firstLine = MoveThroughTrapQuery.RealisedLine(firstAt, firstEnd);
            var secondLine = MoveThroughTrapQuery.RealisedLine(secondAt, secondEnd);
            // Only a following endpoint can need the other's old occupancy. Vacate the front first;
            // this is one synchronous commit after both paths passed, not two independent effects.
            if (firstEnd.Equals(secondAt))
            {
                if (!CommitValidatedDisplacement(second, secondEnd, secondLine).Accepted ||
                    !CommitValidatedDisplacement(first, firstEnd, firstLine).Accepted)
                    throw new InvalidOperationException("A validated coupled displacement could not commit.");
            }
            else if (!CommitValidatedDisplacement(first, firstEnd, firstLine).Accepted ||
                !CommitValidatedDisplacement(second, secondEnd, secondLine).Accepted)
                throw new InvalidOperationException("A validated coupled displacement could not commit.");
            return true;
        }

        /// <summary>
        /// Moves p.8's Battle Zone to <paramref name="destination"/>, or refuses and leaves it where it
        /// is (milestone 6.10).
        ///
        /// Which zone is next is a rules decision the Lane-Push Zone stage makes from the ordered Lane
        /// and the losing team's Throne; what is verified here is that the answer is a zone this map
        /// actually has, that it lies on the ordered Lane, and that it is exactly one step from the
        /// current zone along that Lane. So "advance one verified Zone" is one commit rather than an
        /// assignment a caller could repeat, skip or reverse, in the shape
        /// <see cref="CommitValidatedDisplacement"/> already gives a validated destination.
        /// </summary>
        public bool CommitValidatedBattleZoneAdvance(ZoneId destination)
        {
            return CurrentBattleZoneId.HasValue &&
                CommitValidatedBattleZoneAdvance(CurrentBattleZoneId.Value, destination);
        }

        public bool CommitValidatedBattleZoneAdvance(ZoneId currentBattleZone, ZoneId destination)
        {
            if (!MapDefinition.TryGetLaneContaining(currentBattleZone, out _, out var lane)) return false;
            var current = -1;
            var next = -1;
            for (var index = 0; index < lane.Count; index++)
            {
                if (lane[index].Equals(currentBattleZone)) current = index;
                if (lane[index].Equals(destination)) next = index;
            }

            if (current < 0 || next < 0 || Math.Abs(next - current) != 1)
            {
                return false;
            }

            var stateIndex = currentBattleZoneIds.FindIndex(zone => zone.Equals(currentBattleZone));
            if (stateIndex < 0) return false;
            currentBattleZoneIds[stateIndex] = destination;
            return true;
        }

        // ---------------------------------------------------------------------------------------
        // Milestone 6.11: the occupancy half of p.8's Lane Push. Milestone 6.10 left these out on
        // purpose - a port that could reach them would have exposed half a transition - and this is
        // the milestone that completes it.
        // ---------------------------------------------------------------------------------------

        public IReadOnlyList<MinionOccupancy> FindMinionsInZone(ZoneId zoneId)
        {
            return minions
                .Where(minion => TryGetZoneForCoordinate(minion.Coordinate, out var zone) &&
                    zone.Equals(zoneId))
                .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<MinionOccupancy> FindMinionSupply() =>
            reserveMinions
                .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                .ToArray();

        public bool CommitValidatedLanePushRemoval(MinionId minionId)
        {
            if (!minions.Any(minion => minion.MinionId.Equals(minionId)))
            {
                return false;
            }

            CommitValidatedMinionRemoval(minionId);
            return true;
        }

        public UnitSpawnCommitResult CommitValidatedRespawnAtSpawnPoint(
            MinionId minionId, SpawnPointId spawnPointId)
        {
            var index = reserveMinions.FindIndex(minion => minion.MinionId.Equals(minionId));
            if (index < 0)
            {
                return UnitSpawnCommitResult.Reject(
                    minions.Any(minion => minion.MinionId.Equals(minionId))
                        ? UnitSpawnRejectionReason.UnitAlreadyOnBoard
                        : UnitSpawnRejectionReason.UnknownUnit);
            }

            var spawnPoint = new ConfiguredSpawnPointReference(MapDefinition.Id, spawnPointId);
            if (!TryGetSpawnPointCoordinate(spawnPoint, out var destination))
            {
                return UnitSpawnCommitResult.Reject(UnitSpawnRejectionReason.UnknownSpawnPoint);
            }

            // p.19's clearance, which p.8 restates for exactly this case: "If a Spawn point is occupied
            // by a token, first remove that token, then respawn the minion." Any other obstacle blocks,
            // and p.8's sidebar sends that minion to a chosen empty space instead - a decision this
            // commit reports rather than makes.
            PlacedToken clearedToken = null;
            if (occupancyIndex.IsOccupied(destination))
            {
                if (!tokens.Any(token => token.Coordinate.Equals(destination)))
                {
                    return UnitSpawnCommitResult.Reject(UnitSpawnRejectionReason.SpawnPointOccupied);
                }

                clearedToken = CommitValidatedTokenRemovalBeforeUnitSpawn(spawnPoint);
            }

            var reserved = reserveMinions[index];
            var space = MapDefinition.Spaces.Single(candidate =>
                candidate.Kind == BoardMapSpaceKind.Playable && candidate.Coordinate.Equals(destination));
            // Only the position moves. A miniature's declared Spawn point is identity-bound setup
            // content, and a restore rebuilds the supply as the configured roster minus the restored
            // positions, so a respawn that rewrote it would be a change no snapshot carries.
            var spawned = reserved.WithPosition(
                new ConfiguredSpaceReference(MapDefinition.Id, space.Id), destination);
            occupancyIndex.Add(destination, BoardOccupant.ForMinion(spawned.MinionId), nameof(minionId));
            reserveMinions.RemoveAt(index);
            minions.Add(spawned);
            return UnitSpawnCommitResult.Accept(spawned, destination, clearedToken);
        }

        public bool CommitValidatedLanePushPlacement(MinionId minionId, HexCoordinate destination)
        {
            var index = reserveMinions.FindIndex(minion => minion.MinionId.Equals(minionId));
            if (index < 0 || !IsEmptyPlayableSpace(destination))
            {
                return false;
            }

            var reserved = reserveMinions[index];
            var space = MapDefinition.Spaces.Single(candidate =>
                candidate.Kind == BoardMapSpaceKind.Playable && candidate.Coordinate.Equals(destination));
            var placed = reserved.WithPosition(
                new ConfiguredSpaceReference(MapDefinition.Id, space.Id), destination);
            occupancyIndex.Add(destination, BoardOccupant.ForMinion(placed.MinionId), nameof(minionId));
            reserveMinions.RemoveAt(index);
            minions.Add(placed);
            return true;
        }

        public bool HasTokenAt(HexCoordinate coordinate) =>
            tokens.Any(token => token.Coordinate.Equals(coordinate));

        /// <summary>
        /// Whether a hero has entered and left a space something declaring p.18's move-through trap was
        /// standing on, and the rules have not yet priced it. Milestone 8.61e.
        /// </summary>
        public bool HasRecordedMoveThroughCrossings => moveThroughCrossings.Count > 0;

        /// <summary>
        /// Hands over every recorded crossing and clears them, so one displacement is priced exactly
        /// once however many altitudes look. The order is the order the displacements committed.
        /// </summary>
        public IReadOnlyList<MoveThroughCrossing> TakeRecordedMoveThroughCrossings()
        {
            if (moveThroughCrossings.Count == 0)
            {
                return Array.Empty<MoveThroughCrossing>();
            }

            var taken = moveThroughCrossings.ToArray();
            moveThroughCrossings.Clear();
            return Array.AsReadOnly(taken);
        }

        /// <summary>
        /// Records that <paramref name="mover"/> entered and left <paramref name="spacesMovedThrough"/>,
        /// if anything standing on them declares a trap. Milestone 8.61e.
        ///
        /// <para>Only heroes are recorded, because only a hero can spring one: p.19 separates heroes
        /// from minions and the printed sentence names heroes. Whether the hero is an ENEMY of any of
        /// those pieces is deliberately not asked here - this type does not know the teams, and
        /// answering it in two places is how two readers start disagreeing.</para>
        /// </summary>
        private void RecordMoveThroughCrossing(
            BoardObjectReference subject, IReadOnlyList<HexCoordinate> spacesMovedThrough) =>
            RecordMoveThroughCrossing(
                subject.Kind == BoardObjectKind.Hero ? subject.HeroId : default, spacesMovedThrough);

        private void RecordMoveThroughCrossing(
            HeroId mover, IReadOnlyList<HexCoordinate> spacesMovedThrough)
        {
            if (!mover.IsDefined || spacesMovedThrough == null || spacesMovedThrough.Count == 0)
            {
                return;
            }

            List<HexCoordinate> crossed = null;
            foreach (var space in spacesMovedThrough)
            {
                for (var index = 0; index < tokens.Count; index++)
                {
                    if (!tokens[index].Coordinate.Equals(space) ||
                        !MoveThroughTrapQuery.Declares(tokens[index].Definition))
                    {
                        continue;
                    }

                    crossed = crossed ?? new List<HexCoordinate>();
                    crossed.Add(space);
                    break;
                }
            }

            if (crossed != null)
            {
                moveThroughCrossings.Add(new MoveThroughCrossing(mover, crossed.AsReadOnly()));
            }
        }

        private bool TryGetZoneForCoordinate(HexCoordinate coordinate, out ZoneId zoneId)
        {
            zoneId = default;
            var space = MapDefinition.Spaces.FirstOrDefault(candidate =>
                candidate.Kind == BoardMapSpaceKind.Playable && candidate.Coordinate.Equals(coordinate));
            if (space?.ZoneId == null) return false;
            zoneId = space.ZoneId.Value;
            return true;
        }

        /// <param name="spacesMovedThrough">
        /// Milestone 8.61e. The spaces this displacement entered and left, or <c>null</c> for one that
        /// crossed none. It is supplied by the caller rather than derived here because only the caller
        /// knows which kind of displacement this is: p.18's Push walks a straight line, p.11's card-text
        /// Move walks a route that may bend around an obstacle, and a Place - which reaches this same
        /// commit - crosses nothing at all, because p.18 puts a piece INTO a space rather than moving it
        /// along one. Deriving a line from the two coordinates would have made every Place a move.
        /// </param>
        public DisplacementCommitResult CommitValidatedDisplacement(
            BoardObjectReference subject,
            HexCoordinate destination,
            IReadOnlyList<HexCoordinate> spacesMovedThrough = null)
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
                    var index = BodyIndexAt(subject.HeroId, origin);
                    occupancyIndex.Move(origin, destination);
                    occupancies[index] = occupancies[index].MovedTo(destination);
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

            RecordMoveThroughCrossing(subject, spacesMovedThrough);
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

        // ---------------------------------------------------------------------------------------
        // Milestone 6.6: the board-object exchange port. p.19 keeps Swap, Replace, and Remove as three
        // more distinct operations, and none of them is expressible through the displacement commit
        // above: that commit needs an empty destination, and a Swap has two occupied spaces, a Replace
        // never empties the one it works in, and a Remove has no board destination at all.
        // ---------------------------------------------------------------------------------------

        public BoardObjectSwapCommitResult CommitValidatedSwap(
            BoardObjectReference first,
            BoardObjectReference second)
        {
            if (first.Equals(second))
            {
                return BoardObjectSwapCommitResult.Reject(BoardObjectSwapRejectionReason.SameObject);
            }

            if (!TryGetObjectCoordinate(first, out var firstOrigin) ||
                !TryGetObjectCoordinate(second, out var secondOrigin))
            {
                return BoardObjectSwapCommitResult.Reject(BoardObjectSwapRejectionReason.UnknownObject);
            }

            // Neither space needs an emptiness or terrain check. Each object is entering a space the
            // other currently stands in, so both are playable spaces this board already admits an
            // occupant in, and the exchange leaves exactly as many occupants as it found.
            occupancyIndex.Exchange(firstOrigin, secondOrigin);
            Reposition(first, firstOrigin, secondOrigin);
            Reposition(second, secondOrigin, firstOrigin);
            return BoardObjectSwapCommitResult.Accept(first, second, firstOrigin, secondOrigin);
        }

        public BoardObjectRemovalCommitResult CommitValidatedRemoval(BoardObjectReference subject,
            MatchModeRules modeRules = null)
        {
            if (subject.Kind == BoardObjectKind.Hero)
            {
                if (modeRules?.HasMinionCategory(subject.HeroId) == true &&
                    TryGetObjectCoordinate(subject, out var modeOrigin))
                {
                    CommitValidatedHeroRemoval(subject.HeroId);
                    return BoardObjectRemovalCommitResult.Accept(subject, modeOrigin);
                }
                // p.13 takes a hero off the board through Defeat and its consequences, so this port is
                // not the back door to it.
                return BoardObjectRemovalCommitResult.Reject(
                    BoardObjectRemovalRejectionReason.HeroRequiresDefeat);
            }

            if (!TryGetObjectCoordinate(subject, out var origin))
            {
                return BoardObjectRemovalCommitResult.Reject(
                    BoardObjectRemovalRejectionReason.UnknownObject);
            }

            switch (subject.Kind)
            {
                case BoardObjectKind.Minion:
                    // The same commit the Minion Battle and the Attack's Defeat stage use, so a
                    // removed miniature rejoins the reserve exactly once and by one route.
                    CommitValidatedMinionRemoval(subject.MinionId);
                    break;
                case BoardObjectKind.Token:
                    CommitValidatedTokenReclamation(subject.TokenInstanceId);
                    break;
                default:
                    return BoardObjectRemovalCommitResult.Reject(
                        BoardObjectRemovalRejectionReason.UnknownObject);
            }

            return BoardObjectRemovalCommitResult.Accept(subject, origin);
        }

        public BoardObjectReplacementCommitResult CommitValidatedTokenReplacement(
            TokenInstanceId outgoing,
            PlacedToken incoming)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));

            var index = tokens.FindIndex(token => token.Id.Equals(outgoing));
            if (index < 0)
            {
                return BoardObjectReplacementCommitResult.Reject(
                    BoardObjectReplacementRejectionReason.OutgoingNotOnBoard);
            }

            if (tokens.Any(token => token.Id.Equals(incoming.Id)))
            {
                return BoardObjectReplacementCommitResult.Reject(
                    BoardObjectReplacementRejectionReason.IncomingAlreadyOnBoard);
            }

            var replaced = tokens[index];
            if (!replaced.Coordinate.Equals(incoming.Coordinate))
            {
                return BoardObjectReplacementCommitResult.Reject(
                    BoardObjectReplacementRejectionReason.DestinationMismatch);
            }

            // One transaction in one space. The index is substituted rather than emptied and refilled,
            // so no other commit can see the space free and take it.
            occupancyIndex.Substitute(replaced.Coordinate, BoardOccupant.ForToken(incoming.Id));
            tokens[index] = incoming;
            return BoardObjectReplacementCommitResult.Accept(replaced, incoming);
        }

        public BoardObjectRemovalCommitResult CommitValidatedHeroReplacement(HeroId outgoing, PlacedToken incoming)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            var bodies = GetHeroBodies(outgoing);
            if (bodies.Count != 1 || !bodies[0].Coordinate.Equals(incoming.Coordinate) ||
                tokens.Any(token => token.Id.Equals(incoming.Id)) ||
                tokens.Count(token => token.Definition.Reference.Equals(incoming.Definition.Reference)) >= incoming.Definition.SupplyCount)
                return BoardObjectRemovalCommitResult.Reject(BoardObjectRemovalRejectionReason.UnknownObject);
            occupancies.Remove(bodies[0]);
            actingBodies.Remove(outgoing);
            occupancyIndex.Substitute(incoming.Coordinate, BoardOccupant.ForToken(incoming.Id));
            tokens.Add(incoming);
            return BoardObjectRemovalCommitResult.Accept(BoardObjectReference.ForHero(outgoing), incoming.Coordinate);
        }

        public BoardObjectRemovalCommitResult CommitValidatedMinionReplacement(
            MinionId outgoing,
            PlacedToken incoming)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));

            var index = minions.FindIndex(minion => minion.MinionId.Equals(outgoing));
            if (index < 0)
            {
                return BoardObjectRemovalCommitResult.Reject(BoardObjectRemovalRejectionReason.UnknownObject);
            }

            var replaced = minions[index];
            if (!replaced.Coordinate.Equals(incoming.Coordinate) ||
                tokens.Any(token => token.Id.Equals(incoming.Id)) ||
                tokens.Count(token => token.Definition.Reference.Equals(incoming.Definition.Reference)) >=
                    incoming.Definition.SupplyCount)
            {
                return BoardObjectRemovalCommitResult.Reject(BoardObjectRemovalRejectionReason.UnknownObject);
            }

            // One transaction in one space, as the token-for-token arm: the index is substituted rather
            // than emptied and refilled, and the miniature rejoins the reserve by the one route every
            // removal takes.
            minions.RemoveAt(index);
            reserveMinions.Add(replaced);
            occupancyIndex.Substitute(replaced.Coordinate, BoardOccupant.ForToken(incoming.Id));
            tokens.Add(incoming);
            return BoardObjectRemovalCommitResult.Accept(
                BoardObjectReference.ForMinion(outgoing), replaced.Coordinate);
        }

        /// <summary>
        /// Records an already exchanged object's new space in its own occupancy list. It never touches
        /// the index, because <see cref="BoardOccupancyIndex.Exchange"/> has already moved both at once.
        /// </summary>
        private void Reposition(BoardObjectReference subject, HexCoordinate origin, HexCoordinate destination)
        {
            switch (subject.Kind)
            {
                case BoardObjectKind.Hero:
                {
                    var index = BodyIndexAt(subject.HeroId, origin);
                    occupancies[index] = occupancies[index].MovedTo(destination);
                    break;
                }

                case BoardObjectKind.Minion:
                {
                    var index = minions.FindIndex(minion => minion.MinionId.Equals(subject.MinionId));
                    var space = MapDefinition.Spaces.Single(candidate =>
                        candidate.Kind == BoardMapSpaceKind.Playable &&
                        candidate.Coordinate.Equals(destination));
                    minions[index] = minions[index].WithPosition(
                        new ConfiguredSpaceReference(MapDefinition.Id, space.Id), destination);
                    break;
                }

                case BoardObjectKind.Token:
                {
                    var index = tokens.FindIndex(token => token.Id.Equals(subject.TokenInstanceId));
                    tokens[index] = tokens[index].WithCoordinate(destination);
                    break;
                }

                default:
                    throw new InvalidOperationException(
                        "A board exchange can only reposition a hero, a minion, or a token.");
            }
        }

        // ---------------------------------------------------------------------------------------
        // Milestone 6.5: the unit Spawn port. Taking a miniature out of the supply is a different
        // authority question from relocating one that already stands, so these sit beside the
        // displacement and token commits rather than inside them - p.18 keeps Place, Push, and Spawn
        // distinct operations, and CARD_EFFECT_CAPABILITIES.md forbids treating them as
        // interchangeable.
        // ---------------------------------------------------------------------------------------

        public IReadOnlyList<SpawnableUnit> FindUnitSupply() =>
            reserveMinions
                .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                .Select(minion => new SpawnableUnit(
                    minion.MinionId, minion.TeamSide, minion.MinionType, minion.SpawnPoint))
                .ToArray();

        public IReadOnlyList<HexCoordinate> FindSpawnPointSpaces() =>
            MapDefinition.SpawnPoints
                .Select(point => MapDefinition.Spaces.SingleOrDefault(space =>
                    space.Id.Equals(point.SpaceId) && space.Kind == BoardMapSpaceKind.Playable))
                .Where(space => space != null)
                .Select(space => space.Coordinate)
                .Distinct()
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R)
                .ToArray();

        public bool TryGetSpawnPointCoordinate(
            ConfiguredSpawnPointReference spawnPoint, out HexCoordinate coordinate)
        {
            coordinate = default;
            if (!spawnPoint.MapId.Equals(MapDefinition.Id))
            {
                return false;
            }

            var definition = MapDefinition.SpawnPoints.SingleOrDefault(candidate =>
                candidate.Id.Equals(spawnPoint.SpawnPointId));
            if (definition == null)
            {
                return false;
            }

            var space = MapDefinition.Spaces.SingleOrDefault(candidate =>
                candidate.Id.Equals(definition.SpaceId) && candidate.Kind == BoardMapSpaceKind.Playable);
            if (space == null)
            {
                return false;
            }

            coordinate = space.Coordinate;
            return true;
        }

        public UnitSpawnCommitResult CommitValidatedUnitSpawn(MinionId minionId)
        {
            var index = reserveMinions.FindIndex(minion => minion.MinionId.Equals(minionId));
            if (index < 0)
            {
                return UnitSpawnCommitResult.Reject(
                    minions.Any(minion => minion.MinionId.Equals(minionId))
                        ? UnitSpawnRejectionReason.UnitAlreadyOnBoard
                        : UnitSpawnRejectionReason.UnknownUnit);
            }

            var reserved = reserveMinions[index];
            if (!TryGetSpawnPointCoordinate(reserved.SpawnPoint, out var destination))
            {
                return UnitSpawnCommitResult.Reject(UnitSpawnRejectionReason.UnknownSpawnPoint);
            }

            // p.19: "If a unit Spawns in a Spawn Point covered by a token, remove the token first,
            // then spawn the unit." Any other obstacle simply blocks - the same page's
            // one-obstacle-per-space rule admits no second occupant, and no rule clears a miniature
            // out of the way.
            PlacedToken clearedToken = null;
            if (occupancyIndex.IsOccupied(destination))
            {
                var covering = tokens.SingleOrDefault(token => token.Coordinate.Equals(destination));
                if (covering == null)
                {
                    return UnitSpawnCommitResult.Reject(UnitSpawnRejectionReason.SpawnPointOccupied);
                }

                clearedToken = CommitValidatedTokenRemovalBeforeUnitSpawn(reserved.SpawnPoint);
            }

            var space = MapDefinition.Spaces.Single(candidate =>
                candidate.Kind == BoardMapSpaceKind.Playable && candidate.Coordinate.Equals(destination));
            var spawned = reserved.WithPosition(
                new ConfiguredSpaceReference(MapDefinition.Id, space.Id), destination);
            occupancyIndex.Add(destination, BoardOccupant.ForMinion(spawned.MinionId), nameof(minionId));
            reserveMinions.RemoveAt(index);
            minions.Add(spawned);
            return UnitSpawnCommitResult.Accept(spawned, destination, clearedToken);
        }

        /// <summary>
        /// Milestone 9.21d. The declared bodies not standing, in body order (RZ-Q1: the supply, never
        /// the board). A hero that declares one body has none to spawn.
        /// </summary>
        public IReadOnlyList<int> FindHeroBodySupply(HeroId heroId)
        {
            var supply = HeroBodySupplyOf(heroId);
            if (supply < 2)
            {
                return Array.Empty<int>();
            }

            var standing = new HashSet<int>(GetHeroBodies(heroId).Select(body => body.Body));
            return Enumerable.Range(HeroOccupancy.FirstBody, supply)
                .Where(body => !standing.Contains(body))
                .ToArray();
        }

        /// <summary>
        /// Milestone 9.21d. p.19's Spawn of one more body of a hero already on the board: the lowest
        /// body left in the supply, onto an empty playable space. The acting body settled for the
        /// current action (9.21b) is untouched, so a body spawned during an action never changes who
        /// performs it.
        /// </summary>
        public bool CommitValidatedHeroBodySpawn(
            HeroId heroId, HexCoordinate destination, out HeroOccupancy spawned)
        {
            spawned = default;
            var supply = FindHeroBodySupply(heroId);
            if (supply.Count == 0 || !IsHeroOnBoard(heroId) || !IsEmptyPlayableSpace(destination))
            {
                return false;
            }

            spawned = new HeroOccupancy(heroId, supply[0], destination);
            occupancyIndex.Add(destination, BoardOccupant.ForHero(heroId), nameof(heroId));
            occupancies.Add(spawned);
            return true;
        }

        /// <summary>
        /// Milestone 9.21e. p.19's Remove of named bodies of a many-bodied hero by his own card text
        /// (RZ-Q6): never a Defeat, so nothing here reaches <c>HeroDefeatResolution</c>, Life or
        /// coins, and the bodies are back in the supply because <see cref="FindHeroBodySupply"/> reads
        /// the board. Removing the body settled to act for this action (RZ-Q6: "one of you" may be it)
        /// settles the LOWEST body left in its place, so every later read of the actor's space in the
        /// action answers a body that stands (never 9.21a's refusal, never a body gone); with none
        /// left the settled body is forgotten and the hero is off the board, as after a Defeat.
        /// </summary>
        public bool CommitValidatedHeroBodyRemoval(
            HeroId heroId, IReadOnlyList<int> bodies, out IReadOnlyList<HeroOccupancy> removed)
        {
            removed = Array.Empty<HeroOccupancy>();
            if (bodies == null || bodies.Count == 0 || HeroBodySupplyOf(heroId) < 2 ||
                bodies.Distinct().Count() != bodies.Count)
            {
                return false;
            }

            var taken = GetHeroBodies(heroId).Where(body => bodies.Contains(body.Body)).ToArray();
            if (taken.Length != bodies.Count)
            {
                return false;
            }

            foreach (var body in taken)
            {
                occupancies.Remove(body);
                occupancyIndex.Remove(body.Coordinate);
            }

            if (actingBodies.TryGetValue(heroId, out var acting) && taken.Any(body => body.Body == acting))
            {
                var left = GetHeroBodies(heroId);
                if (left.Count == 0)
                {
                    actingBodies.Remove(heroId);
                }
                else
                {
                    actingBodies[heroId] = left[0].Body;
                }
            }

            removed = Array.AsReadOnly(taken);
            return true;
        }
    }
}
