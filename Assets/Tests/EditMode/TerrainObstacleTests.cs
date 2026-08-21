using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8e. Terrain is a passable obstacle, not off-board.
    ///
    /// Rulebook p.18 makes terrain an obstacle like a unit or token, so a unit ignoring obstacles may
    /// enter and leave its space while moving, while no unit may ever end a movement there and nothing
    /// may be placed there. Before 6.8e the movement board was built from playable coordinates alone,
    /// so terrain and a coordinate off the map were the same unanswerable question and the Move-Through
    /// permission could not carry a unit across the 82 terrain cells the official map declares.
    ///
    /// The synthetic board is the smallest shape with the distinction: a playable line interrupted by
    /// one declared terrain cell, with genuine off-board space beyond the line's ends. The official-map
    /// cases prove the same rule against real map data: terrain `q4_r0` sits between playable `(4,-1)`
    /// and `(4,1)`, which are not adjacent to each other, so crossing genuinely uses the terrain cell.
    /// </summary>
    public sealed class TerrainObstacleTests
    {
        private static readonly HeroId Mover = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

        private static readonly HexCoordinate Terrain = new HexCoordinate(1, 0);
        private static readonly HexCoordinate Beyond = new HexCoordinate(2, 0);
        private static readonly HexCoordinate OffBoard = new HexCoordinate(9, 0);

        // ---------------------------------------------------------------------------------------
        // ObstacleQuery classification
        // ---------------------------------------------------------------------------------------

        [Test]
        public void Terrain_IsAnObstacleThatBlocksTransitOnlyWithoutThePermission()
        {
            var board = CreateTerrainLineBoard();
            var mover = RuleObject.ForHero(Mover);
            var permitted = MoveThroughFor(Mover);

            Assert.That(ObstacleQuery.IsObstacle(board, Terrain, RuleModificationSet.Empty), Is.True,
                "p.18 lists terrain as an obstacle exactly like a unit or token.");
            Assert.That(ObstacleQuery.BlocksTransit(board, Terrain, mover, permitted), Is.False,
                "A unit ignoring obstacles may enter and leave a terrain space while moving.");
            Assert.That(
                ObstacleQuery.BlocksTransit(board, Terrain, mover, RuleModificationSet.Empty),
                Is.True,
                "Without the permission terrain blocks transit like any obstacle.");
            Assert.That(ObstacleQuery.BlocksEndOfMovement(board, Terrain, RuleModificationSet.Empty), Is.True,
                "No unit may end its movement on an obstacle, permission or not.");
        }

        [Test]
        public void OffBoard_BlocksEverythingForEveryone_PermissionOrNot()
        {
            var board = CreateTerrainLineBoard();
            var mover = RuleObject.ForHero(Mover);

            Assert.That(
                ObstacleQuery.BlocksTransit(board, OffBoard, mover, MoveThroughFor(Mover)),
                Is.True,
                "A coordinate that is not on the map is not an obstacle to ignore; nothing lets a " +
                "unit leave the board.");
            Assert.That(ObstacleQuery.BlocksEndOfMovement(board, OffBoard, RuleModificationSet.Empty), Is.True);
        }

        // ---------------------------------------------------------------------------------------
        // Reachability
        // ---------------------------------------------------------------------------------------

        [Test]
        public void WithoutThePermission_TerrainBlocksTheRouteBeyondIt()
        {
            var board = CreateTerrainLineBoard();

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                Occupancy(board), board, 3, RuleModificationSet.Empty);

            Assert.That(
                destinations.Select(destination => destination.Destination),
                Is.EquivalentTo(new[] { new HexCoordinate(-1, 0) }),
                "Terrain blocks transit for an ordinary mover, so only the open side is reachable.");
        }

        [Test]
        public void WithThePermission_TheRouteCrossesTerrainButNeverStopsOnIt()
        {
            var board = CreateTerrainLineBoard();

            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                Occupancy(board), board, 3, MoveThroughFor(Mover));

            Assert.That(
                destinations.Select(destination => destination.Destination),
                Is.EquivalentTo(new[]
                {
                    new HexCoordinate(-1, 0),
                    Beyond,
                    new HexCoordinate(3, 0)
                }),
                "The permission carries the mover across terrain to the playable space beyond.");
            Assert.That(destinations.Any(destination => destination.Destination.Equals(Terrain)),
                Is.False,
                "Terrain is never offered as a destination, permission or not.");

            var beyond = destinations.Single(destination => destination.Destination.Equals(Beyond));
            Assert.That(beyond.Cost, Is.EqualTo(2),
                "Entering terrain costs the same movement as entering any other space.");
            Assert.That(beyond.Route, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 0),
                Terrain,
                Beyond
            }), "The route names every space entered, including the terrain passed through.");
        }

        // ---------------------------------------------------------------------------------------
        // Route commitment
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ACommittedRoute_CrossesTerrainWithoutOccupyingItAndEndsBeyond()
        {
            var board = CreateTerrainLineBoard();
            var permitted = MoveThroughFor(Mover);

            var result = board.TryCommitRoute(Mover, new[] { Terrain, Beyond }, permitted);

            Assert.That(result.Accepted, Is.True,
                "A validated route may enter and leave a terrain space for transit.");
            Assert.That(result.Events.Select(value => value.Destination),
                Is.EqualTo(new[] { Terrain, Beyond }),
                "One event per space entered, terrain included: the unit really crosses it.");
            Assert.That(Occupancy(board).Coordinate, Is.EqualTo(Beyond));

            var back = board.TryCommitRoute(
                Mover, new[] { Terrain, new HexCoordinate(0, 0) }, permitted);
            Assert.That(back.Accepted, Is.True,
                "The same terrain space is immediately crossable again in the other direction, so " +
                "the passing unit never occupied it and the one-obstacle-per-space index was never " +
                "transiently violated.");
        }

        [Test]
        public void ARouteEndingOnTerrain_IsRefusedWithTheTerrainReasonAndMutatesNothing()
        {
            var board = CreateTerrainLineBoard();

            var result = board.TryCommitRoute(Mover, new[] { Terrain }, MoveThroughFor(Mover));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(AdjacentMoveRejectionReason.DestinationTerrain),
                "Ending on terrain is refused as terrain, not as off-board: the space exists.");
            Assert.That(result.RejectedRouteIndex, Is.EqualTo(0));
            Assert.That(Occupancy(board).Coordinate, Is.EqualTo(new HexCoordinate(0, 0)),
                "A refused route leaves the board untouched.");
        }

        [Test]
        public void WithoutThePermission_ARouteAcrossTerrainIsRefusedAsTerrainNotOffBoard()
        {
            var board = CreateTerrainLineBoard();

            var result = board.TryCommitRoute(Mover, new[] { Terrain, Beyond },
                RuleModificationSet.Empty);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(AdjacentMoveRejectionReason.DestinationTerrain));
            Assert.That(result.RejectedRouteIndex, Is.EqualTo(0));
            Assert.That(Occupancy(board).Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
        }

        [Test]
        public void ARouteLeavingTheMap_IsStillRefusedAsOffBoard()
        {
            var board = CreateTerrainLineBoard();

            var result = board.TryCommitRoute(
                Mover,
                new[] { new HexCoordinate(-1, 0), new HexCoordinate(-2, 0) },
                MoveThroughFor(Mover));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(AdjacentMoveRejectionReason.DestinationOffBoard),
                "A coordinate that is not on the map keeps its own refusal; no permission answers it.");
            Assert.That(result.RejectedRouteIndex, Is.EqualTo(1));
        }

        [Test]
        public void ASingleStepOntoTerrain_IsRefusedWithTheTerrainReason()
        {
            var board = CreateTerrainLineBoard();

            var result = board.TryCommit(new AdjacentMoveCommand(
                Mover, new HexCoordinate(0, 0), Terrain));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(AdjacentMoveRejectionReason.DestinationTerrain));
        }

        // ---------------------------------------------------------------------------------------
        // The official map's declared terrain
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheAuthority_CarriesEveryDeclaredTerrainCellOfTheOfficialMap()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var board = CreateOfficialBoard(new HexCoordinate(4, -1));
            var mover = RuleObject.ForHero(Mover);
            var permitted = MoveThroughFor(Mover);
            var declaredTerrain = map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Terrain)
                .Select(space => space.Coordinate)
                .ToArray();

            Assert.That(declaredTerrain, Has.Length.EqualTo(82),
                "The reviewed official map declares 82 terrain cells.");
            foreach (var cell in declaredTerrain)
            {
                var label = $"q{cell.Q} r{cell.R}";
                Assert.That(ObstacleQuery.IsObstacle(board, cell, RuleModificationSet.Empty), Is.True, label);
                Assert.That(ObstacleQuery.BlocksTransit(board, cell, mover, permitted), Is.False,
                    label + ": every declared terrain cell is crossable under the permission; " +
                    "before 6.8e each one answered as off-board.");
                Assert.That(
                    ObstacleQuery.BlocksTransit(board, cell, mover, RuleModificationSet.Empty),
                    Is.True, label);
                Assert.That(ObstacleQuery.BlocksEndOfMovement(board, cell, RuleModificationSet.Empty), Is.True, label);
            }
        }

        /// <summary>
        /// Milestone 6.8e's failing observation: before the fix the reachability board was built from
        /// playable coordinates alone, so this crossing did not exist on the official map.
        /// </summary>
        [Test]
        public void OnTheOfficialMap_ThePermissionCarriesAHeroAcrossDeclaredTerrain()
        {
            var origin = new HexCoordinate(4, -1);
            var terrain = new HexCoordinate(4, 0);
            var beyond = new HexCoordinate(4, 1);
            var board = CreateOfficialBoard(origin);

            var withPermission = MovementReachabilityQuery.FindReachableDestinations(
                Occupancy(board), board, 2, MoveThroughFor(Mover));
            var crossing = withPermission.SingleOrDefault(destination =>
                destination.Destination.Equals(beyond));

            Assert.That(crossing, Is.Not.Null,
                "The playable space beyond the declared terrain cell must be reachable with the " +
                "permission; before 6.8e terrain was indistinguishable from off-board and blocked it.");
            Assert.That(crossing.Cost, Is.EqualTo(2));
            Assert.That(crossing.Route, Is.EqualTo(new[] { origin, terrain, beyond }),
                "The route crosses the real declared terrain cell, proved against map data rather " +
                "than a synthetic board.");
            Assert.That(withPermission.Any(destination => destination.Destination.Equals(terrain)),
                Is.False, "The terrain cell itself is never a destination.");

            var withoutPermission = MovementReachabilityQuery.FindReachableDestinations(
                Occupancy(board), board, 2, RuleModificationSet.Empty);
            Assert.That(withoutPermission.Any(destination => destination.Destination.Equals(beyond)),
                Is.False,
                "Without the permission the crossing does not exist, exactly as before the fix.");
        }

        [Test]
        public void OnTheOfficialMap_TerrainClassifiesAsObstacleAndOffMapAsNothing()
        {
            var board = CreateOfficialBoard(new HexCoordinate(4, -1));
            var mover = RuleObject.ForHero(Mover);
            var terrain = new HexCoordinate(4, 0);
            var offMap = new HexCoordinate(99, 99);

            Assert.That(ObstacleQuery.IsObstacle(board, terrain, RuleModificationSet.Empty), Is.True);
            Assert.That(ObstacleQuery.BlocksTransit(board, terrain, mover, MoveThroughFor(Mover)),
                Is.False);
            Assert.That(
                ObstacleQuery.BlocksTransit(board, terrain, mover, RuleModificationSet.Empty),
                Is.True);
            Assert.That(ObstacleQuery.BlocksEndOfMovement(board, terrain, RuleModificationSet.Empty), Is.True);
            Assert.That(ObstacleQuery.BlocksTransit(board, offMap, mover, MoveThroughFor(Mover)),
                Is.True);
            Assert.That(ObstacleQuery.BlocksEndOfMovement(board, offMap, RuleModificationSet.Empty), Is.True);
        }

        /// <summary>
        /// Milestone 8.8, and half of ledger question W-04's answer: the official map already models
        /// p.19's surrounding water, so nothing leaves the play area without meeting terrain first.
        ///
        /// The question W-04 asks is whether a push stopped at the board's edge is stopped by an
        /// obstacle. On this map the edge is not where the spaces stop - every space beside a playable
        /// one is either playable or declared terrain - so a unit shoved off the play area lands
        /// against a modelled obstacle and the card's sentence is answered by the ordinary rule. The
        /// reading recorded on <c>PushPathResult.StoppedByAnObstacle</c> covers what is past the ring,
        /// where the classification does answer "nothing"; this is why no map change was needed to
        /// resolve the question.
        /// </summary>
        [Test]
        public void EverySpaceBesideAPlayableOne_IsPlayableOrDeclaredTerrain()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var playable = new HashSet<HexCoordinate>(map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate));
            var declared = new HashSet<HexCoordinate>(map.Spaces.Select(space => space.Coordinate));

            var unmodelled = playable
                .SelectMany(space => space.GetNeighbors())
                .Distinct()
                .Where(neighbour => !declared.Contains(neighbour))
                .OrderBy(neighbour => neighbour.Q)
                .ThenBy(neighbour => neighbour.R)
                .ToArray();

            Assert.That(playable, Is.Not.Empty, "A vacuous ring would prove nothing.");
            Assert.That(
                unmodelled.Select(neighbour => $"q{neighbour.Q} r{neighbour.R}"),
                Is.Empty,
                "A playable space with an unmodelled neighbour would let a unit be pushed straight " +
                "out of the map without meeting the water p.19 calls terrain.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static HeroOccupancy Occupancy(AdjacentMoveAuthority board) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Mover));

        /// <summary>
        /// A live set carrying only the Move-Through permission, built the way the match builds it:
        /// from an Active effect in its active phase, not hand-assembled.
        /// </summary>
        private static RuleModificationSet MoveThroughFor(HeroId heroId)
        {
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"terrain-move-through-{heroId.Value}"),
                GuardsDigital.Core.Effects.Synthetic.SyntheticModificationCapabilities
                    .MoveThroughPermissionDefinition,
                heroId,
                TeamSide.Red,
                default,
                new CardInstanceId("terrain-move-through-source-card"),
                new ActionDefinitionId("terrain-move-through-source-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2),
                new TieBreakerState(TeamSide.Red));

            var participants = PlaceholderOneVsOneParticipants.Create();
            var set = RuleModificationQuery.Build(
                timing, null, participants, new TieBreakerState(TeamSide.Red), UnlockedUltimateSet.None, RuleObject.None, null);
            Assert.That(
                set.HasPermission(RuleObject.ForHero(heroId), RulePermissionKind.MoveThroughObstacles),
                Is.True,
                "The fixture must actually hold the permission, or every case below proves nothing.");
            return set;
        }

        /// <summary>
        /// Hero at (0,0) on a playable line (-1,0)..(3,0) whose (1,0) cell is declared terrain, so
        /// the only way past it is through it. (-2,0) and (4,0) are genuinely off the map.
        /// </summary>
        private static AdjacentMoveAuthority CreateTerrainLineBoard()
        {
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("synthetic-terrain-line"),
                1,
                new[]
                {
                    new HexCoordinate(-1, 0),
                    new HexCoordinate(0, 0),
                    new HexCoordinate(2, 0),
                    new HexCoordinate(3, 0)
                },
                new[] { Terrain });
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Mover, new HexCoordinate(0, 0)) },
                Array.Empty<ObstacleOccupancy>());
        }

        private static AdjacentMoveAuthority CreateOfficialBoard(HexCoordinate heroAt)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Mover, heroAt) },
                Array.Empty<ObstacleOccupancy>());
        }
    }
}
