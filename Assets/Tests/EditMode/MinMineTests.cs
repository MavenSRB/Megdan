using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Min the Dragonmonk's facedown mines. Milestone 8.61d.
    ///
    /// <para>Three printed clauses are measured here and each is a sentence rather than a shape:
    /// <b>"1 blast and 1 dud"</b> - two definitions, one pile, and p.18's short-supply step reaching
    /// across kinds; <b>"units may move through them"</b> - p.18's obstacle-side clause, over every
    /// displacement the engine has and never over an endpoint; and <b>"facedown"</b> - p.4's hidden
    /// information, as a per-seat projection filter that the authority, the wire's shape and the rules
    /// are all untouched by.</para>
    ///
    /// <para>The trap the same sentences end with is milestone 8.61e's and is asserted here only as a
    /// DEBT: the three faces declare it owed, so a later composition cannot quietly mark them complete.
    /// </para>
    /// </summary>
    public sealed class MinMineTests
    {
        private static readonly HeroId Min = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        // -------------------------------------------------------------------------------------
        // "1 blast and 1 dud": two definitions, one pile (MI-Q1, and Tali's RULING 44 shape).
        // -------------------------------------------------------------------------------------

        [Test]
        public void BothKindsAndTheFacedownFaceDrawOnOnePileOfFour()
        {
            foreach (var definition in MinComponentContent.MineTokens())
            {
                Assert.That(definition.SupplyCount,
                    Is.EqualTo(MinComponentContent.MineSupplyCount),
                    "MI-Q1's working answer: two blasts and two duds, so the one pile is four.");
                Assert.That(definition.SupplyGroupId,
                    Is.EqualTo(MinComponentContent.MineSupplyGroupId),
                    "One physical pile, named the same way by every face that draws from it.");
                Assert.That(definition.RemovalCondition,
                    Is.EqualTo(TokenRemovalCondition.EndOfRoundCleanup),
                    "p.19's baseline; no mine face says otherwise.");
            }

            Assert.That(MinComponentContent.MineTokens().Count(), Is.EqualTo(3),
                "A blast, a dud, and the face every other seat sees.");

            // The catalog refuses a group whose members disagree about the pile's size, which is what
            // makes the shared supply one number rather than three that can drift.
            Assert.That(() => new ComponentDefinitionCatalog(
                    new[]
                    {
                        MinComponentContent.MineBlastDefinition,
                        new TokenDefinition(
                            new TokenDefinitionId("min-mine-disagreeing"), 1, "Mine", 2,
                            Array.Empty<TokenPropertyDefinition>(),
                            TokenRemovalCondition.EndOfRoundCleanup,
                            MinComponentContent.MineSupplyGroupId)
                    },
                    Array.Empty<MarkerDefinition>()),
                Throws.ArgumentException);
        }

        [Test]
        public void PlacingABlastLeavesThreeForADud_BecauseTheSupplyIsOnePile()
        {
            var board = LineBoard();
            var catalog = MineCatalog();

            Assert.That(
                catalog.CountAvailableTokens(
                    MinComponentContent.MineDudDefinition.Reference, board.PlacedTokens),
                Is.EqualTo(4));

            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            Assert.That(
                catalog.CountAvailableTokens(
                    MinComponentContent.MineDudDefinition.Reference, board.PlacedTokens),
                Is.EqualTo(3),
                "A blast on the board is a token out of the one pile, whatever the next copy's role.");
        }

        [Test]
        public void AShortSupplyReclaimsAcrossKinds_WhichIsWhatOneTypeMeans()
        {
            var board = LineBoard();
            var catalog = MineCatalog();
            var duds = new[]
            {
                PlaceMine(board, MinComponentContent.MineDudDefinition, 1, "dud-1"),
                PlaceMine(board, MinComponentContent.MineDudDefinition, 2, "dud-2"),
                PlaceMine(board, MinComponentContent.MineDudDefinition, 3, "dud-3"),
                PlaceMine(board, MinComponentContent.MineDudDefinition, 4, "dud-4")
            };

            Assert.That(
                catalog.CountAvailableTokens(
                    MinComponentContent.MineBlastDefinition.Reference, board.PlacedTokens),
                Is.EqualTo(0),
                "Four duds empty the pile a blast would be drawn from.");

            var reclaimable = catalog.ReclaimableForPlacement(
                MinComponentContent.MineBlastDefinition.Reference, board.PlacedTokens).ToArray();
            Assert.That(reclaimable.Select(token => token.Id),
                Is.EquivalentTo(duds.Select(token => token.Id)),
                "p.18's \"tokens of the same type\" is answered over the pile, so a blast placement " +
                "may take back a standing dud.");
        }

        // -------------------------------------------------------------------------------------
        // "Facedown": the declaration, and the properties that make the hiding costless.
        // -------------------------------------------------------------------------------------

        [Test]
        public void EachKindHidesItselfBehindOneFaceThatNamesNoKind()
        {
            foreach (var kind in new[]
            {
                MinComponentContent.MineBlastDefinition, MinComponentContent.MineDudDefinition
            })
            {
                Assert.That(kind.HidesItsKind, Is.True, "Every mine face prints \"facedown\".");
                Assert.That(kind.FacedownFace,
                    Is.SameAs(MinComponentContent.MineFacedownDefinition),
                    "Both kinds wear the SAME face, or the face itself would name the kind.");
            }

            Assert.That(MinComponentContent.MineFacedownDefinition.HidesItsKind, Is.False);
            Assert.That(MinComponentContent.MineFacedownDefinition.DisplayName, Is.EqualTo("Mine"));
            Assert.That(MinComponentContent.MineFacedownDefinition.Id.Value,
                Does.Not.Contain("blast").And.Not.Contain("dud"));

            // A chain would leave "what does another seat see" with two answers, and a self-reference
            // with none.
            Assert.That(() => new TokenDefinition(
                    new TokenDefinitionId("chained"), 1, "Chained", 4,
                    Array.Empty<TokenPropertyDefinition>(),
                    TokenRemovalCondition.EndOfRoundCleanup,
                    MinComponentContent.MineSupplyGroupId,
                    MinComponentContent.MineBlastDefinition),
                Throws.ArgumentException);
        }

        [Test]
        public void BothKindsPrintTheSameProperties_WhichIsWhatMakesTheHidingCostless()
        {
            Assert.That(Describe(MinComponentContent.MineBlastDefinition),
                Is.EqualTo(Describe(MinComponentContent.MineDudDefinition)),
                "A blast and a dud differ only in what a REMOVAL does, so a masked board answers " +
                "every standing question exactly as the authority's does.");
            Assert.That(Describe(MinComponentContent.MineFacedownDefinition),
                Is.EqualTo(Describe(MinComponentContent.MineBlastDefinition)),
                "The face a client is handed has to walk the same routes the host walks.");

            foreach (var definition in MinComponentContent.MineTokens())
            {
                Assert.That(ObstacleQuery.DeclaresMayBeMovedThrough(definition), Is.True);
                Assert.That(
                    definition.Properties.Any(property =>
                        property.Modification != null &&
                        property.Modification.Kind == RuleModificationKind.Immunity),
                    Is.False,
                    "No mine face prints an immunity, so p.13's Clear reaches one.");
            }
        }

        // -------------------------------------------------------------------------------------
        // "Units may move through them": p.18's obstacle-side clause, at its one owner.
        // -------------------------------------------------------------------------------------

        [Test]
        public void AMovementWalksThroughAMineAndMayNotStopOnIt()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 1, "blast-1");

            var reachable = MovementReachabilityQuery.FindReachableDestinations(
                new HeroOccupancy(Min, new HexCoordinate(0, 0)), board, 3, RuleModificationSet.Empty);
            var spaces = reachable.Select(destination => destination.Destination).ToArray();

            Assert.That(spaces, Has.None.EqualTo(new HexCoordinate(1, 0)),
                "p.18: \"a unit can never end its movement on the same space as an obstacle.\"");
            Assert.That(spaces, Contains.Item(new HexCoordinate(2, 0)),
                "\"Units may move through them\" - the space beyond the mine is reachable.");
            Assert.That(
                reachable.Single(destination => destination.Destination.Equals(new HexCoordinate(3, 0)))
                    .Route,
                Contains.Item(new HexCoordinate(1, 0)),
                "And the route that reaches it is the one that crosses the mine.");
        }

        [Test]
        public void AnOrdinaryTokenStillStopsTheSameWalk()
        {
            var board = LineBoard();
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId("ice-1"), TaliComponentContent.IceDefinition, Min, Min,
                new CardInstanceId("fixture-card"), new HexCoordinate(1, 0)));

            var spaces = MovementReachabilityQuery.FindReachableDestinations(
                    new HeroOccupancy(Min, new HexCoordinate(0, 0)), board, 3,
                    RuleModificationSet.Empty)
                .Select(destination => destination.Destination)
                .ToArray();

            Assert.That(spaces, Is.Empty,
                "A token that prints no move-through clause is the obstacle it always was, which is " +
                "what says the mine's walkability is the mine's own declaration and not a widening.");
        }

        [Test]
        public void ACardTextDirectMoveCrossesAMineTheSameWay()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineDudDefinition, 1, "dud-1");

            var destinations = board.FindDirectMoveDestinations(
                BoardObjectReference.ForHero(Min), 2, RuleModificationSet.Empty,
                moveIgnoresObstacles: false);

            Assert.That(destinations.Select(destination => destination.Destination),
                Contains.Item(new HexCoordinate(2, 0)));
            Assert.That(destinations.Select(destination => destination.Destination),
                Has.None.EqualTo(new HexCoordinate(1, 0)));
        }

        [Test]
        public void APushCarriesAUnitThroughAMine_AndStopsShortWhenTheSpaceBeyondIsTaken()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 2, "blast-1");

            // Min at (0,0) pushes the enemy standing at (1,0) two spaces east, across the mine.
            board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Enemy), new HexCoordinate(1, 0));
            var carried = board.ResolvePush(
                new HexCoordinate(0, 0), new HexCoordinate(1, 0), 2,
                RuleObject.ForHero(Enemy), false, RuleModificationSet.Empty);

            Assert.That(carried.Endpoint, Is.EqualTo(new HexCoordinate(3, 0)));
            Assert.That(carried.ActualDistance, Is.EqualTo(2));
            Assert.That(carried.StoppingReason,
                Is.EqualTo(PushStoppingReason.ReachedRequestedDistance),
                "p.18: \"the obstacle can be 'moved through' [so] the pushed target is moved through " +
                "obstacles as long as it can reach an empty space.\"");

            // The same push with the only space beyond the mine occupied: it is walked over and the
            // push stops short rather than landing on it.
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId("ice-beyond"), TaliComponentContent.IceDefinition, Min, Min,
                new CardInstanceId("fixture-card"), new HexCoordinate(3, 0)));
            board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Enemy), new HexCoordinate(1, 0));
            var blocked = board.ResolvePush(
                new HexCoordinate(0, 0), new HexCoordinate(1, 0), 2,
                RuleObject.ForHero(Enemy), false, RuleModificationSet.Empty);

            Assert.That(blocked.ActualDistance, Is.EqualTo(0));
            Assert.That(blocked.Endpoint, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(blocked.StoppedByAnObstacle, Is.True);
        }

        [Test]
        public void ACoupledMoveCrossesAMine()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineDudDefinition, 3, "dud-1");
            board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Enemy), new HexCoordinate(1, 0));

            Assert.That(
                board.CanMoveCoupled(
                    BoardObjectReference.ForHero(Enemy), BoardObjectReference.ForHero(Min),
                    HexDirection.East, 4, RuleModificationSet.Empty, ignoresObstacles: false),
                Is.True,
                "The pair walks (1,0)->(5,0) and (0,0)->(4,0), crossing the mine at (3,0) on the " +
                "way and stopping on neither.");
            Assert.That(
                board.CanMoveCoupled(
                    BoardObjectReference.ForHero(Enemy), BoardObjectReference.ForHero(Min),
                    HexDirection.East, 3, RuleModificationSet.Empty, ignoresObstacles: false),
                Is.False,
                "The same pair one space shorter ends Min ON the mine, which p.18 refuses for every " +
                "unit whatever the obstacle says about being moved through.");
        }

        [Test]
        public void AMineIsAnObstacleForEverythingThatIsNotTransit()
        {
            var board = LineBoard();
            var mine = PlaceMine(board, MinComponentContent.MineBlastDefinition, 1, "blast-1");

            Assert.That(ObstacleQuery.IsObstacle(board, mine.Coordinate, RuleModificationSet.Empty),
                Is.True, "p.19: all tokens are obstacles.");
            Assert.That(
                ObstacleQuery.BlocksEndOfMovement(board, mine.Coordinate, RuleModificationSet.Empty),
                Is.True);
            Assert.That(
                ObstacleQuery.BlocksPlacement(board, mine.Coordinate, RuleModificationSet.Empty),
                Is.True, "A Place needs an empty space, and a mine's space is not one.");
            Assert.That(board.IsEmptyPlayableSpace(mine.Coordinate), Is.False,
                "Which is also the answer Fast Travel's destination test reads.");
            Assert.That(
                board.TryCommit(new AdjacentMoveCommand(
                    Min, new HexCoordinate(0, 0), mine.Coordinate)).Accepted,
                Is.False,
                "A single step always ENDS where it goes, so it is refused.");
            Assert.That(
                board.FindEmptyPathRoutes(new HexCoordinate(0, 0), 3)
                    .Select(destination => destination.Destination),
                Has.None.EqualTo(new HexCoordinate(2, 0)),
                "p.18's displaced-minion correction walks empty spaces only, and a mine's is not one.");
        }

        // -------------------------------------------------------------------------------------
        // Lifetime: p.19's end of round, p.13's Clear, p.8's Spawn point.
        // -------------------------------------------------------------------------------------

        [Test]
        public void EveryMineLeavesAtTheEndOfTheRound_AndEitherKindCanSimplyBeRemoved()
        {
            var board = LineBoard();
            PlaceMine(board, MinComponentContent.MineBlastDefinition, 1, "blast-1");
            PlaceMine(board, MinComponentContent.MineDudDefinition, 2, "dud-1");

            var removed = ComponentCleanupRules.TokensRemovedAtEndOfRound(board.PlacedTokens);
            // Counted through the list rather than with Has.Count: the value is an array behind an
            // IReadOnlyList, and Unity's NUnit 3.5 resolves that constraint by reflecting for a
            // "Count" property an array does not have. The harness's newer NUnit does not care, which
            // is exactly why this one is written the way both tiers read.
            Assert.That(removed.Count, Is.EqualTo(2));
            foreach (var token in removed)
            {
                board.CommitValidatedTokenReclamation(token.Id);
            }

            Assert.That(board.PlacedTokens, Is.Empty);

            // p.13's Clear removes an adjacent token, and the operation it commits is this one.
            var cleared = PlaceMine(board, MinComponentContent.MineBlastDefinition, 1, "blast-2");
            Assert.That(board.CommitValidatedTokenReclamation(cleared.Id).Id, Is.EqualTo(cleared.Id));
            Assert.That(board.PlacedTokens, Is.Empty);
        }

        [Test]
        public void AMineOnASpawnPointIsTakenBeforeTheUnitSpawns()
        {
            var board = MappedBoard();
            var spawnPoint = board.MapDefinition.SpawnPoints.First();
            var coordinate = board.MapDefinition.Spaces
                .Single(space => space.Id.Equals(spawnPoint.SpaceId)).Coordinate;
            var mine = new PlacedToken(
                new TokenInstanceId("blast-on-spawn"), MinComponentContent.MineBlastDefinition,
                Min, Min, new CardInstanceId("fixture-card"), coordinate);
            board.CommitValidatedTokenPlacement(mine);

            var removed = board.CommitValidatedTokenRemovalBeforeUnitSpawn(
                new ConfiguredSpawnPointReference(board.MapDefinition.Id, spawnPoint.Id));

            Assert.That(removed, Is.Not.Null);
            Assert.That(removed.Id, Is.EqualTo(mine.Id));
            Assert.That(board.PlacedTokens, Is.Empty,
                "p.8's spawn clearance asks no kind question and no immunity question.");
        }

        // -------------------------------------------------------------------------------------
        // The composition: 1 + 1, 1 + 2, 2 + 1, each mandatory and each preceded by p.18's reclaim.
        // -------------------------------------------------------------------------------------

        [TestCase("min-trip-mine", 1, 1)]
        [TestCase("min-cluster-mine", 1, 2)]
        [TestCase("min-mine-field", 2, 1)]
        public void EachFacePlacesItsPrintedKindsInItsPrintedOrder(
            string cardId, int blasts, int duds)
        {
            var card = MinCardContent.MineDefinition(new CardDefinitionId(cardId));
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var effect = skill.EffectRoot;
            Assert.That(effect, Is.Not.Null);

            var placements = effect.Nodes
                .Where(node => node.Id.Value.StartsWith("place-mine-", StringComparison.Ordinal))
                .OrderBy(node => int.Parse(node.Id.Value.Substring("place-mine-".Length)))
                .ToArray();

            Assert.That(placements.Select(node => node.Capability),
                Is.EqualTo(Enumerable
                    .Repeat(MinComponentContent.PlaceMineBlastV1, blasts)
                    .Concat(Enumerable.Repeat(MinComponentContent.PlaceMineDudV1, duds))
                    .ToArray()),
                "MI-Q10d: the card prints \"blasts and duds\" in that order, and with fewer empty " +
                "spaces than copies the printed order is what decides which copies happen.");

            Assert.That(placements.Select(node => node.FailurePolicy),
                Is.All.EqualTo(EffectFailurePolicy.Mandatory),
                "p.10: the text carries none of \"you may\", \"up to\" or \"if able\".");
            Assert.That(effect.Nodes.Any(node => node.Kind == EffectNodeKind.Optional), Is.False);
            Assert.That(
                effect.Nodes
                    .Where(node => node.Id.Value.StartsWith("place-reclaim-", StringComparison.Ordinal))
                    .ToArray(),
                Has.Length.EqualTo(blasts + duds)
                    .And.All.Matches<EffectNodeDefinition>(node =>
                        node.FailurePolicy == EffectFailurePolicy.IfAble &&
                        node.Capability.Equals(MinComponentContent.ReclaimMineV1)),
                "p.18 puts the short-supply step first, and closes it with \"if able\".");

            // And every pair is that reclamation followed by that placement, in that order.
            for (var index = 0; index < blasts + duds; index++)
            {
                var pair = effect.Nodes.Single(node => node.Id.Value == $"place-pair-{index}");
                Assert.That(pair.ChildNodeIds.Select(child => child.Value),
                    Is.EqualTo(new[] { $"place-reclaim-{index}", $"place-mine-{index}" }));
            }
        }

        [Test]
        public void EveryMineFacePrintsItsRowAndStillOwesTheTrap()
        {
            var expected = new Dictionary<string, int[]>
            {
                ["min-trip-mine"] = new[] { 10, 4, 2 },
                ["min-cluster-mine"] = new[] { 11, 4, 2 },
                ["min-mine-field"] = new[] { 11, 5, 2 }
            };

            foreach (var card in MinCardContent.MineDefinitions())
            {
                var row = expected[card.Id.Value];
                Assert.That(card.Initiative.Value, Is.EqualTo(row[0]));
                Assert.That(Printed(card, ActionKind.Defense, PrintedValueChannel.Defense),
                    Is.EqualTo(row[1]));
                Assert.That(Printed(card, ActionKind.Movement, PrintedValueChannel.Movement),
                    Is.EqualTo(row[2]));
                Assert.That(Printed(card, ActionKind.Skill, PrintedValueChannel.Radius),
                    Is.EqualTo(3), "All three faces print Radius 3 and no Range at all.");
                Assert.That(
                    card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill).BaseValues
                        .Any(value => value.Channel == PrintedValueChannel.Range),
                    Is.False, "A missing icon is absent, never zero.");

                Assert.That(MinEffectContent.MineTrapOwed[card.Id],
                    Is.EqualTo(MinEffectContent.MineTrapOwedTo),
                    "The trap is owed rather than stubbed.");
            }

            Assert.That(MinEffectContent.MineTrapOwed.Keys,
                Is.EquivalentTo(MinCardContent.MineFaces));

            // The other half of the debt: nothing this block composed removes a mine or asks anyone
            // for a card, so a reader of the graph cannot mistake a placement for the trap.
            foreach (var card in MinCardContent.MineDefinitions())
            {
                var effect = card.ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Skill).EffectRoot;
                Assert.That(
                    effect.Nodes.Select(node => node.Capability)
                        .Where(capability => capability.IsDefined)
                        .Distinct(),
                    Is.EquivalentTo(new[]
                    {
                        MinComponentContent.ReclaimMineV1,
                        MinComponentContent.PlaceMineBlastV1,
                        MinComponentContent.PlaceMineDudV1
                    }.Where(reference =>
                        effect.Nodes.Any(node => node.Capability.Equals(reference)))),
                    "p.18's short-supply step and the placements, and nothing else: no removal, no " +
                    "discard, no reveal.");
            }
        }

        // -------------------------------------------------------------------------------------
        // The placement itself, driven through the handlers the composition names.
        // -------------------------------------------------------------------------------------

        [Test]
        public void APlacementReachesThePrintedRadius_AndAnItemRaisesIt()
        {
            var board = DiscBoard();
            var handler = PlaceBlastHandler();

            var offered = handler.Execute(Context(board)).Choice.Candidates
                .Select(candidate => candidate.Coordinate)
                .ToArray();
            Assert.That(offered, Is.Not.Empty);
            Assert.That(
                offered.All(space => HexDistanceQuery.Calculate(MinAt, space) <= 3), Is.True,
                "p.9: the printed Radius icon bounds \"in radius\".");
            Assert.That(offered.Any(space => HexDistanceQuery.Calculate(MinAt, space) == 3), Is.True);
            Assert.That(offered.Any(space => HexDistanceQuery.Calculate(MinAt, space) == 4), Is.False);

            var raised = handler.Execute(Context(board, RadiusItem())).Choice.Candidates
                .Select(candidate => candidate.Coordinate)
                .ToArray();
            Assert.That(raised.Any(space => HexDistanceQuery.Calculate(MinAt, space) == 4), Is.True,
                "p.16's item raises the icon the placement reads, without the handler knowing it.");
        }

        [Test]
        public void AMandatoryPlacementWithNoEmptySpaceLeftIsImpossible_SoAsManyAsPossibleArePlaced()
        {
            // A five-space line - the enemy, Min, and three spaces - so Radius 3 of Min is the whole
            // board and three foreign tokens leave nowhere at all to put a mine. The blockers are
            // another hero's tokens rather than mines, so the refusal below is about SPACES and could
            // not be the supply answering instead.
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(-1, 5).Select(q => new HexCoordinate(q, 0)).ToArray(),
                new[]
                {
                    new HeroOccupancy(Min, MinAt), new HeroOccupancy(Enemy, new HexCoordinate(-1, 0))
                });
            var handler = PlaceBlastHandler();

            foreach (var q in new[] { 1, 2, 3 })
            {
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("ice-" + q), TaliComponentContent.IceDefinition, Min, Min,
                    new CardInstanceId("fixture-card"), new HexCoordinate(q, 0)));
            }

            Assert.That(
                MineCatalog().CountAvailableTokens(
                    MinComponentContent.MineBlastDefinition.Reference, board.PlacedTokens),
                Is.EqualTo(MinComponentContent.MineSupplyCount),
                "The pile is untouched, so nothing but the board can make this placement impossible.");

            Assert.That(handler.Anticipate(Context(board)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "p.10's impossible step: with nowhere in radius to put it, the placement stops the " +
                "sequence and the copies already placed stand.");
        }

        [Test]
        public void AMintedInstanceIdNamesNoKind()
        {
            var board = DiscBoard();
            var blast = Placed(board, PlaceBlastHandler(), "place-mine-0", new HexCoordinate(1, 0));
            var dud = Placed(board, PlaceDudHandler(), "place-mine-1", new HexCoordinate(2, 0));

            foreach (var token in new[] { blast, dud })
            {
                Assert.That(token.Id.Value,
                    Does.Not.Contain("blast").And.Not.Contain("dud"),
                    "The wire writes this string verbatim on every token row, so a kind in it would " +
                    "hand every seat exactly what the definition masking just took away.");
                Assert.That(token.Id.Value,
                    Does.Contain(MinComponentContent.MineInstanceIdentityLabel));
            }

            Assert.That(blast.Id, Is.Not.EqualTo(dud.Id),
                "The node ID the placement already carries is what keeps two copies distinct.");
        }

        // -------------------------------------------------------------------------------------
        // "Facedown", per seat: the projection filter, the wire, and what stays unmasked.
        // -------------------------------------------------------------------------------------

        [Test]
        public void EverySeatButTheControllersIsHandedAFacedownMine_AndTheWireCarriesTheSameRow()
        {
            var staged = StagedHostWithMines();
            var seen = 0;

            foreach (var seat in staged.Host.Seats.Seats)
            {
                var projection = staged.Host.ProjectionForSeat(seat.Id);
                var rows = projection.PlacedTokens
                    .Where(token => staged.MineIds.Contains(token.Id))
                    .ToArray();
                Assert.That(rows, Has.Length.EqualTo(2), "Both mines are on every seat's board.");

                var owns = seat.Owns(staged.Controller);
                foreach (var token in rows)
                {
                    Assert.That(token.Definition.Id,
                        owns
                            ? Is.EqualTo(staged.KindOf(token.Id))
                            : Is.EqualTo(MinComponentContent.MineFacedown),
                        owns
                            ? "The hero who placed it knows which is which."
                            : "p.4: a facedown component's kind is hidden from every other seat, " +
                              "teammates included.");
                }

                // The same view once it has crossed the wire: the row shape is unchanged and the
                // instance ID - the handle a Clear offer and every displacement name - is intact.
                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(projection), out var record), Is.True);
                foreach (var token in rows)
                {
                    var row = record.PlacedTokens.Single(value =>
                        value.TokenInstanceId == token.Id.Value);
                    Assert.That(row.TokenDefinitionId, Is.EqualTo(token.Definition.Id.Value));
                    Assert.That(row.ControllerHeroId, Is.EqualTo(staged.Controller.Value),
                        "Who placed it is public; which kind it is, is not.");
                }

                seen++;
            }

            Assert.That(seen, Is.GreaterThan(1), "The claim is about several seats.");
            Assert.That(
                staged.Host.Seats.Seats.Count(seat => seat.Owns(staged.Controller)), Is.EqualTo(1));
        }

        [Test]
        public void TheAuthorityKeepsTheRealKinds_WhichIsWhatASnapshotAndAReplayWrite()
        {
            var staged = StagedHostWithMines();

            foreach (var id in staged.MineIds)
            {
                Assert.That(
                    staged.Session.Projection.PlacedTokens
                        .Single(token => token.Id.Equals(id)).Definition.Id,
                    Is.EqualTo(staged.KindOf(id)),
                    "The mask is a projection filter, so the authoritative list a snapshot writes " +
                    "and a replay re-derives still names the kind.");
            }

            // And a restore rebinds that definition from the catalog rather than from bytes, so the
            // kind survives exactly as the reference does.
            var catalog = MineCatalog();
            Assert.That(catalog.LoadToken(MinComponentContent.MineBlastDefinition.Reference),
                Is.SameAs(MinComponentContent.MineBlastDefinition));
            Assert.That(catalog.LoadToken(MinComponentContent.MineFacedownDefinition.Reference),
                Is.SameAs(MinComponentContent.MineFacedownDefinition),
                "The facedown face is registered so a client can load the ID the wire handed it.");
        }

        [Test]
        public void AClearOfferNamesNoKindToAnybodyButTheController()
        {
            var board = LineBoard();
            var blast = PlaceMine(board, MinComponentContent.MineBlastDefinition, 1, "blast-1");
            var dud = PlaceMine(board, MinComponentContent.MineDudDefinition, 2, "dud-1");
            var option = new ClearOptionProjection(
                ClearOffer(), MinCardContent.MinePrimarySkill, new[] { blast, dud });

            var masked = FacedownTokenMask
                .MaskClearOptions(new[] { option }, SeatOf(Enemy))
                .Single();

            Assert.That(masked.AdjacentTokens.Select(token => token.Definition.DisplayName),
                Is.All.EqualTo("Mine"),
                "The Clear selection label is composed from DisplayName, so an unmasked offer would " +
                "have printed \"Mine (blast)\" in a list to the hero standing beside it.");
            Assert.That(masked.AdjacentTokens.Select(token => token.Id),
                Is.EqualTo(new[] { blast.Id, dud.Id }),
                "The instance IDs and their order are untouched, so every subset invocation the " +
                "rebuilt option enumerates is a command the authority still accepts.");
            Assert.That(masked.SubsetInvocations.Count,
                Is.EqualTo(option.SubsetInvocations.Count));

            Assert.That(
                FacedownTokenMask.MaskClearOptions(new[] { option }, SeatOf(Min)).Single(),
                Is.SameAs(option),
                "Her own seat is handed the authority's own object, unallocated and unmasked.");
        }

        [Test]
        public void ABoardWithNothingFacedownOnItIsHandedOverUntouched()
        {
            var board = LineBoard();
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId("ice-1"), TaliComponentContent.IceDefinition, Min, Min,
                new CardInstanceId("fixture-card"), new HexCoordinate(1, 0)));

            Assert.That(
                FacedownTokenMask.Mask(board.PlacedTokens, SeatOf(Enemy)),
                Is.SameAs(board.PlacedTokens),
                "Every board in the build until Min is dealt takes this path and allocates nothing.");
        }

        // -------------------------------------------------------------------------------------
        // The deferral, asserted from both sides.
        // -------------------------------------------------------------------------------------

        [Test]
        public void NothingAboutHerMinesIsRegisteredUntilTheDealing()
        {
            var catalogs = MatchContentCatalogs.ForThisBuild();

            Assert.That(
                catalogs.Components.RegisteredTokens.Select(reference => reference.DefinitionId.Value),
                Has.None.StartsWith("min-"),
                "A component registered before a dealt card could place it moves pinned bytes for " +
                "nothing; milestone 8.61j moves all three across in one change.");

            var deferred = MinComponentContent.MineDeferredHandlers().ToArray();
            Assert.That(deferred.Select(handler => handler.Reference),
                Is.EquivalentTo(new[]
                {
                    MinComponentContent.PlaceMineBlastV1,
                    MinComponentContent.PlaceMineDudV1,
                    MinComponentContent.ReclaimMineV1
                }));
            foreach (var handler in deferred)
            {
                Assert.That(catalogs.Capabilities.TryLoad(handler.Reference, out _), Is.False,
                    $"'{handler.Reference}' is deferred, so this build must not be able to run it.");
            }

            var configured = new HashSet<CardDefinitionId>(
                MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red)
                    .CardDefinitions.Select(definition => definition.Id));
            Assert.That(
                MinCardContent.MineDefinitions().Where(definition => configured.Contains(definition.Id)),
                Is.Empty,
                "And no configured match deals one of her faces yet.");
        }

        // -------------------------------------------------------------------------------------
        // Fixture plumbing.
        // -------------------------------------------------------------------------------------

        private static readonly HexCoordinate MinAt = new HexCoordinate(0, 0);

        private static string Describe(TokenDefinition definition) =>
            string.Join("|", definition.Properties.Select(property => string.Join(
                ":",
                property.PropertyId,
                property.RuleKind,
                property.ApplicabilityKind,
                property.AffectedObject.ToString(),
                property.Modification == null ? "none" : property.Modification.Kind.ToString())));

        private static int Printed(CardDefinition card, ActionKind kind, PrintedValueChannel channel) =>
            card.ActionDefinitions.Single(action => action.Kind == kind).BaseValues
                .Single(value => value.Channel == channel).Amount;

        private static PlacedToken PlaceMine(
            AdjacentMoveAuthority board, TokenDefinition definition, int q, string instanceId)
        {
            var token = new PlacedToken(
                new TokenInstanceId(instanceId), definition, Min, Min,
                new CardInstanceId("fixture-card"), new HexCoordinate(q, 0));
            board.CommitValidatedTokenPlacement(token);
            return token;
        }

        private static ComponentDefinitionCatalog MineCatalog() =>
            new ComponentDefinitionCatalog(
                MinComponentContent.MineTokens(), Array.Empty<MarkerDefinition>());

        /// <summary>A straight line with Min at (0,0) and the enemy behind her at (-1,0).</summary>
        private static AdjacentMoveAuthority LineBoard() => new AdjacentMoveAuthority(
            Enumerable.Range(-2, 10).Select(q => new HexCoordinate(q, 0)).ToArray(),
            new[]
            {
                new HeroOccupancy(Min, MinAt), new HeroOccupancy(Enemy, new HexCoordinate(-1, 0))
            });

        /// <summary>A radius-4 disc, so a Radius 3 placement and a raised one can be told apart.</summary>
        private static AdjacentMoveAuthority DiscBoard() => new AdjacentMoveAuthority(
            Disc(4), new[] { new HeroOccupancy(Min, MinAt) });

        /// <summary>
        /// The official map, which is the only one in the build that declares Spawn points - p.8's
        /// clearance is asked of a real one rather than of a synthetic space called one.
        /// </summary>
        private static AdjacentMoveAuthority MappedBoard()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            return new AdjacentMoveAuthority(
                map,
                Enumerable.Empty<HeroOccupancy>(),
                Enumerable.Empty<ObstacleOccupancy>(),
                Enumerable.Empty<QuickGameMinionSetupState>());
        }

        /// <summary>One real seat per hero, built the way the host builds them.</summary>
        private static MatchSeat SeatOf(HeroId heroId) =>
            MatchSeatBinding.ForParticipants(StagedBoardParticipants.Create())
                .Seats.Single(seat => seat.Owns(heroId));

        /// <summary>
        /// A replacement offer, which is the only shape <see cref="ClearOptionProjection"/> accepts -
        /// p.13's Clear replaces an Attack action.
        /// </summary>
        private static ActionOffer ClearOffer() =>
            new ActionOffer(
                new CardInstanceId("fixture-card"),
                MinCardContent.MineDefinition(MinCardContent.TripMine).Reference,
                MinCardContent.MinePrimarySkill,
                Min,
                ActionInvocationContext.Replacement,
                true);

        private static HexCoordinate[] Disc(int radius)
        {
            var spaces = new List<HexCoordinate>();
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    if (HexDistanceQuery.Calculate(MinAt, coordinate) <= radius)
                    {
                        spaces.Add(coordinate);
                    }
                }
            }

            return spaces.ToArray();
        }

        private static IEffectCapabilityHandler PlaceBlastHandler() =>
            MinComponentContent.MineDeferredHandlers().Single(handler =>
                handler.Reference.Equals(MinComponentContent.PlaceMineBlastV1));

        private static IEffectCapabilityHandler PlaceDudHandler() =>
            MinComponentContent.MineDeferredHandlers().Single(handler =>
                handler.Reference.Equals(MinComponentContent.PlaceMineDudV1));

        private static PlacedToken Placed(
            AdjacentMoveAuthority board,
            IEffectCapabilityHandler handler,
            string nodeId,
            HexCoordinate at)
        {
            var before = board.PlacedTokens.Select(token => token.Id).ToArray();
            var result = handler.Resume(
                Context(board, null, nodeId), new[] { EffectValue.Space(at) });
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            return board.PlacedTokens.Single(token => !before.Contains(token.Id));
        }

        private static HeroItemState RadiusItem() => HeroItemState.Restore(
            StagedBoardParticipants.Create(),
            new HeroItemRestoreState(new[]
            {
                new HeroItem(
                    Min, new CardInstanceId("item"), HeroDashboard.RulebookLayout.Slots[3].Position,
                    PrintedValueChannel.Radius, 1)
            }));

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, HeroItemState items = null, string nodeId = "place-mine-0")
        {
            var definition = MinCardContent.MineDefinition(MinCardContent.TripMine);
            return new EffectCapabilityContext(
                new EffectNodeId(nodeId), EffectFailurePolicy.Mandatory, Min, Min,
                new CardInstanceId("fixture-card"), definition.Reference,
                MinCardContent.MinePrimarySkill, new Dictionary<string, EffectValue>(), 1, 1,
                StagedBoardParticipants.Create(), null, RuleModificationSet.Empty,
                EffectCardState.Of(MinCardContent.MineDefinitions(), Array.Empty<CardInstance>()),
                ActionKind.Skill,
                new MatchComponents(
                    MineCatalog(), board, new MarkerAttachmentRegister(), board, board, board),
                heroItems: items);
        }


        /// <summary>
        /// A composed six-player host with one blast and one dud standing on the board, placed onto
        /// the authority directly because no configured match deals her faces until milestone 8.61j.
        /// </summary>
        private static StagedMines StagedHostWithMines()
        {
            var session = new MatchSession(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var board = session.aggregate.Board;
            var controller = session.Projection.Participants.Heroes.First().Id;
            var spaces = board.BoardCoordinates
                .Where(board.IsEmptyPlayableSpace)
                .OrderBy(space => space.Q)
                .ThenBy(space => space.R)
                .Take(2)
                .ToArray();
            Assert.That(spaces, Has.Length.EqualTo(2));

            var kinds = new Dictionary<TokenInstanceId, TokenDefinitionId>();
            for (var index = 0; index < 2; index++)
            {
                var definition = index == 0
                    ? MinComponentContent.MineBlastDefinition
                    : MinComponentContent.MineDudDefinition;
                var token = new PlacedToken(
                    new TokenInstanceId(
                        $"fixture-card:place-mine-{index}:" +
                        $"{MinComponentContent.MineInstanceIdentityLabel}:r0t0:" +
                        $"{spaces[index].Q}:{spaces[index].R}"),
                    definition, controller, controller,
                    new CardInstanceId("fixture-card"), spaces[index]);
                board.CommitValidatedTokenPlacement(token);
                kinds[token.Id] = definition.Id;
            }

            var host = new MatchHost(session);
            // One accepted command republishes the projection the seats read.
            Assert.That(OwnerScopedProjectionTests.DriveThroughSeats(host, 1), Is.EqualTo(1));
            return new StagedMines(session, host, controller, kinds);
        }

        private sealed class StagedMines
        {
            private readonly IReadOnlyDictionary<TokenInstanceId, TokenDefinitionId> kinds;

            internal StagedMines(
                MatchSession session,
                MatchHost host,
                HeroId controller,
                IReadOnlyDictionary<TokenInstanceId, TokenDefinitionId> kinds)
            {
                Session = session;
                Host = host;
                Controller = controller;
                this.kinds = kinds;
            }

            internal MatchSession Session { get; }
            internal MatchHost Host { get; }
            internal HeroId Controller { get; }
            internal IEnumerable<TokenInstanceId> MineIds => kinds.Keys;
            internal TokenDefinitionId KindOf(TokenInstanceId id) => kinds[id];
        }
    }
}
