using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.4's board displacement port. Direct Move and Object Push both relocate an object
    /// already on the board, and both do it without becoming a Movement action, so these cases pin what
    /// the authority does with each occupant kind and what it refuses.
    /// </summary>
    public sealed class BoardDisplacementAuthorityTests
    {
        private static readonly HeroId Mover = new HeroId("displacement-hero");
        private static readonly MinionId Minion = new MinionId("displacement-minion");
        private static readonly MapId Map = new MapId("synthetic-displacement-map");

        [Test]
        public void AHeroDisplacement_MovesTheOccupantAndLeavesItsOriginEmpty()
        {
            var board = CreateBoard();
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(2, 0);

            var result = board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Mover), destination);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Origin, Is.EqualTo(origin));
            Assert.That(result.Destination, Is.EqualTo(destination));
            Assert.That(
                board.Occupancies.Single(o => o.HeroId.Equals(Mover)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(board.IsEmptyPlayableSpace(origin), Is.True);
            Assert.That(board.IsEmptyPlayableSpace(destination), Is.False);
        }

        [Test]
        public void AMinionDisplacement_UpdatesTheOccupancyAndItsMapQualifiedSpace()
        {
            var board = CreateBoard(withMinion: true);
            var destination = new HexCoordinate(3, 0);

            var result = board.CommitValidatedDisplacement(
                BoardObjectReference.ForMinion(Minion), destination);

            Assert.That(result.Accepted, Is.True);
            var moved = board.Minions.Single();
            Assert.That(moved.Coordinate, Is.EqualTo(destination));
            Assert.That(
                moved.OccupiedSpace.MapId,
                Is.EqualTo(Map),
                "A displaced minion keeps a map-qualified space, not a bare coordinate.");
            Assert.That(moved.MinionType, Is.EqualTo(StandardMinionType.Melee), "Identity is preserved.");
        }

        [Test]
        public void ATokenDisplacement_KeepsItsInstanceIdentityOwnerAndPropertySources()
        {
            // p.18 names "a unit or a token" as push targets. Milestone 6.3 ties a token's continuous
            // sources to its remaining placed, and relocating it never stops it being placed.
            var board = CreateBoard();
            var token = PlaceToken(board, new HexCoordinate(1, 0));
            var destination = new HexCoordinate(3, 0);
            var sourceCountBefore = token.PropertySources.Count;

            var result = board.CommitValidatedDisplacement(
                BoardObjectReference.ForToken(token.Id), destination);

            Assert.That(result.Accepted, Is.True);
            var moved = board.PlacedTokens.Single();
            Assert.That(moved.Id, Is.EqualTo(token.Id));
            Assert.That(moved.Coordinate, Is.EqualTo(destination));
            Assert.That(moved.OwnerHeroId, Is.EqualTo(token.OwnerHeroId));
            Assert.That(moved.Definition.Reference, Is.EqualTo(token.Definition.Reference));
            Assert.That(moved.PropertySources.Count, Is.EqualTo(sourceCountBefore));
            Assert.That(
                board.IsEmptyPlayableSpace(new HexCoordinate(1, 0)),
                Is.True,
                "The token's former space is free for the shared occupancy index.");
        }

        [Test]
        public void AnOccupiedDestination_IsRefusedWithoutMovingAnything()
        {
            var board = CreateBoard(withMinion: true);
            var minionAt = board.Minions.Single().Coordinate;

            var result = board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Mover), minionAt);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(DisplacementRejectionReason.DestinationOccupied));
            Assert.That(
                board.Occupancies.Single(o => o.HeroId.Equals(Mover)).Coordinate,
                Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(board.Minions.Single().Coordinate, Is.EqualTo(minionAt));
        }

        [Test]
        public void AnOffBoardDestination_IsRefusedWithItsOwnReason()
        {
            var board = CreateBoard();

            var result = board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Mover), new HexCoordinate(99, 99));

            Assert.That(result.RejectionReason, Is.EqualTo(DisplacementRejectionReason.DestinationOffBoard));
        }

        [Test]
        public void ATerrainDestination_IsRefusedSeparatelyFromOffBoard()
        {
            var board = CreateTerrainBoard();

            var result = board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Mover), new HexCoordinate(2, 0));

            Assert.That(
                result.RejectionReason,
                Is.EqualTo(DisplacementRejectionReason.DestinationTerrain),
                "Milestone 6.8e: terrain exists and permanently blocks; off-board is nothing at all.");
        }

        [Test]
        public void AnObjectNoLongerOnTheBoard_IsRefusedRatherThanThrowing()
        {
            var board = CreateBoard();

            var result = board.CommitValidatedDisplacement(
                BoardObjectReference.ForMinion(new MinionId("absent-minion")), new HexCoordinate(2, 0));

            Assert.That(result.Accepted, Is.False);
            Assert.That(
                result.RejectionReason,
                Is.EqualTo(DisplacementRejectionReason.UnknownObject),
                "A stale validated subject is a race the effect frame reports, not an engine defect.");
        }

        [Test]
        public void AZeroSpaceDisplacement_IsAcceptedAndChangesNothing()
        {
            // p.18's zero-space push. Accepting it here spares every caller a special case.
            var board = CreateBoard();
            var origin = new HexCoordinate(0, 0);

            var result = board.CommitValidatedDisplacement(BoardObjectReference.ForHero(Mover), origin);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Origin, Is.EqualTo(origin));
            Assert.That(result.Destination, Is.EqualTo(origin));
            Assert.That(board.Occupancies.Single(o => o.HeroId.Equals(Mover)).Coordinate, Is.EqualTo(origin));
        }

        [Test]
        public void TryGetObjectAt_NamesEveryDisplaceableOccupantKindByItsSpace()
        {
            var board = CreateBoard(withMinion: true);
            var token = PlaceToken(board, new HexCoordinate(1, 0));

            Assert.That(board.TryGetObjectAt(new HexCoordinate(0, 0), out var hero), Is.True);
            Assert.That(hero.Kind, Is.EqualTo(BoardObjectKind.Hero));
            Assert.That(hero.HeroId, Is.EqualTo(Mover));

            Assert.That(board.TryGetObjectAt(new HexCoordinate(4, 0), out var minion), Is.True);
            Assert.That(minion.Kind, Is.EqualTo(BoardObjectKind.Minion));
            Assert.That(minion.MinionId, Is.EqualTo(Minion));

            Assert.That(board.TryGetObjectAt(new HexCoordinate(1, 0), out var placed), Is.True);
            Assert.That(placed.Kind, Is.EqualTo(BoardObjectKind.Token));
            Assert.That(placed.TokenInstanceId, Is.EqualTo(token.Id));

            Assert.That(board.TryGetObjectAt(new HexCoordinate(3, 0), out _), Is.False, "Empty space.");
        }

        [Test]
        public void AsRuleObject_NamesUnitsAndReportsATokenAsTheNamedAbsence()
        {
            // p.19: tokens are not units, so no restriction, permission, immunity, or Counts As is
            // addressed to one. RuleObject.None is the honest answer rather than an invented member.
            Assert.That(
                BoardObjectReference.ForHero(Mover).AsRuleObject(),
                Is.EqualTo(RuleObject.ForHero(Mover)));
            Assert.That(
                BoardObjectReference.ForMinion(Minion).AsRuleObject(),
                Is.EqualTo(RuleObject.ForMinion(Minion)));
            Assert.That(
                BoardObjectReference.ForToken(new TokenInstanceId("any-token")).AsRuleObject().IsDefined,
                Is.False);
        }

        [Test]
        public void FindDirectMoveDestinations_ReusesMovementReachabilityRatherThanASecondTraversal()
        {
            // p.11: movement in the card text "follows the same rules and restrictions as a Movement
            // action", so an occupied space blocks a direct Move exactly as it blocks Movement.
            var board = CreateBoard(withMinion: true);

            var destinations = board.FindDirectMoveDestinations(Mover, 2, RuleModificationSet.Empty);

            var reached = destinations.Select(destination => destination.Destination).ToArray();
            Assert.That(reached, Has.Member(new HexCoordinate(2, 0)));
            Assert.That(reached, Has.No.Member(new HexCoordinate(4, 0)), "The minion's space is an obstacle.");
            Assert.That(
                destinations.All(destination => destination.Route.Count > 0),
                Is.True,
                "Each destination carries the disclosed route the Movement action already projects.");
        }

        [Test]
        public void FindDirectMoveDestinations_ForAHeroNotOnTheBoard_IsEmpty()
        {
            var board = CreateBoard();

            Assert.That(
                board.FindDirectMoveDestinations(new HeroId("absent"), 3, RuleModificationSet.Empty),
                Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A playable East-West file (-1,0)..(5,0) with the hero at (0,0) and, optionally, a Melee
        /// minion at (4,0).
        /// </summary>
        private static AdjacentMoveAuthority CreateBoard(bool withMinion = false)
        {
            var map = BoardMapDefinition.CreateSynthetic(Map, 1, Line());
            var minions = withMinion
                ? new[] { CreateMinionSetup(new HexCoordinate(4, 0)) }
                : Array.Empty<QuickGameMinionSetupState>();
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Mover, new HexCoordinate(0, 0)) },
                Array.Empty<ObstacleOccupancy>(),
                minions);
        }

        /// <summary>The same file with (2,0) declared terrain rather than playable.</summary>
        private static AdjacentMoveAuthority CreateTerrainBoard()
        {
            var map = BoardMapDefinition.CreateSynthetic(
                Map,
                1,
                Line().Where(coordinate => !coordinate.Equals(new HexCoordinate(2, 0))).ToArray(),
                new[] { new HexCoordinate(2, 0) });
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Mover, new HexCoordinate(0, 0)) },
                Array.Empty<ObstacleOccupancy>());
        }

        private static HexCoordinate[] Line() =>
            Enumerable.Range(-1, 7).Select(q => new HexCoordinate(q, 0)).ToArray();

        private static QuickGameMinionSetupState CreateMinionSetup(HexCoordinate coordinate) =>
            new QuickGameMinionSetupState(
                Minion,
                new TeamId(PlaceholderOneVsOneParticipants.RedTeamIdValue),
                TeamSide.Red,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId("spawn-displacement")),
                new ConfiguredSpaceReference(Map, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);

        private static PlacedToken PlaceToken(AdjacentMoveAuthority board, HexCoordinate coordinate)
        {
            var catalog = SyntheticComponentDefinitions.CreateCatalog();
            var token = new PlacedToken(
                new TokenInstanceId($"displacement-token-{coordinate.Q}-{coordinate.R}"),
                catalog.LoadToken(SyntheticComponentDefinitions.HazardV3),
                Mover,
                Mover,
                new CardInstanceId("displacement-token-source"),
                coordinate);
            board.CommitValidatedTokenPlacement(token);
            return token;
        }
    }
}
