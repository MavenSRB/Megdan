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
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TrinketsComponentFoundationTests
    {
        private static readonly HeroId Owner = new HeroId("trinkets-test-owner");
        private static readonly HeroId Ally = new HeroId("trinkets-test-ally");
        private static readonly HeroId Enemy = new HeroId("trinkets-test-enemy");
        private static readonly HexCoordinate OwnerAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate AllyAt = new HexCoordinate(2, 0);
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(0, 2);

        [Test]
        public void Barrier_HasThreePhysicalCopiesAndAnEndOfRoundTokenLifetime()
        {
            var definition = TrinketsComponentContent.Barrier;
            var catalog = new ComponentDefinitionCatalog(
                TrinketsComponentContent.Tokens(), Array.Empty<MarkerDefinition>());
            var board = Board();
            var spaces = new[]
            {
                new HexCoordinate(1, 0), new HexCoordinate(1, -1), new HexCoordinate(2, -1)
            };

            Assert.That(definition.SupplyCount, Is.EqualTo(3));
            Assert.That(definition.RemovalCondition,
                Is.EqualTo(TokenRemovalCondition.EndOfRoundCleanup));
            for (var index = 0; index < spaces.Length; index++)
            {
                Place(board, spaces[index], index);
            }

            Assert.That(catalog.CountAvailableTokens(definition.Reference, board.PlacedTokens),
                Is.Zero);
            Assert.Throws<InvalidOperationException>(() =>
                Place(board, new HexCoordinate(3, -1), 3));
            Assert.That(board.PlacedTokens, Has.Count.EqualTo(3));
        }

        [Test]
        public void PersistentObstacleCapability_RepositionsOnePieceAndRejectsStaleOrOccupiedAnswer()
        {
            var board = Board();
            var reference = new EffectCapabilityReference(
                new EffectCapabilityId("test-place-persistent-obstacle"), 1);
            var place = SyntheticObstacleCapabilities.PlaceAdjacentHandler(reference, "test-turret");
            var space = SyntheticObstacleCapabilities.SpaceHandler(reference, "test-turret");
            var remove = SyntheticObstacleCapabilities.RemoveHandler(reference, "test-turret");
            var context = Context(board);
            var first = new HexCoordinate(1, 0);
            var second = new HexCoordinate(0, 1);

            Assert.That(space.Execute(context).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(place.Execute(context).Choice.Candidates,
                Has.Member(EffectValue.Space(first)));
            Assert.That(place.Resume(context, new[] { EffectValue.Space(first) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(place.Resume(context, new[] { EffectValue.Space(first) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(place.Resume(context, new[] { EffectValue.Space(AllyAt) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(place.Resume(context, new[] { EffectValue.Space(second) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.ObstacleOccupancies, Has.Count.EqualTo(1));
            Assert.That(ObstacleQuery.IsObstacle(board, second, RuleModificationSet.Empty), Is.True);
            Assert.That(TerrainPresenceQuery.IsTerrain(
                board, second, RuleModificationSet.Empty), Is.False);
            Assert.That(space.Execute(context).Outcomes.Single().Value.Coordinate,
                Is.EqualTo(second));
            Assert.That(board.PlacedTokens, Is.Empty);
            Assert.That(remove.Execute(context).Outcomes.Single().Value.Coordinate,
                Is.EqualTo(second));
            Assert.That(remove.Execute(context).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(board.ObstacleOccupancies, Is.Empty);
        }

        [Test]
        public void BarrierPlacement_ThreeCopiesAndFirstAdjacentToLiveTurret()
        {
            var board = Board();
            var turret = new ObstacleId("test-turret:" + Owner.Value);
            var turretAt = new HexCoordinate(0, 1);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, turretAt), Is.True);
            var constrainedHandler = SyntheticTokenCapabilities.PlaceHandler(
                new EffectCapabilityReference(
                    new EffectCapabilityId("test-place-barrier"), 1),
                TrinketsComponentContent.Barrier, PrintedValueChannel.Radius,
                adjacentToObstacleKind: "test-turret");
            var freeHandler = SyntheticTokenCapabilities.PlaceHandler(
                new EffectCapabilityReference(
                    new EffectCapabilityId("test-place-following-barrier"), 1),
                TrinketsComponentContent.Barrier, PrintedValueChannel.Radius);
            var constrained = Context(board);
            var nonAdjacent = new HexCoordinate(2, -1);
            var adjacent = new HexCoordinate(1, 1);
            Assert.That(constrainedHandler.Execute(constrained).Choice.Candidates,
                Has.Member(EffectValue.Space(adjacent)));
            Assert.That(constrainedHandler.Execute(constrained).Choice.Candidates,
                Has.No.Member(EffectValue.Space(nonAdjacent)));
            Assert.That(constrainedHandler.Resume(constrained,
                new[] { EffectValue.Space(nonAdjacent) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(board.TryPlaceOrRepositionObstacle(turret,
                new HexCoordinate(-1, 0)), Is.True);
            Assert.That(constrainedHandler.Resume(constrained,
                new[] { EffectValue.Space(adjacent) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A Turret relocation after the offer must invalidate its old adjacent choice.");
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, turretAt), Is.True);
            Assert.That(constrainedHandler.Resume(constrained,
                new[] { EffectValue.Space(adjacent) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));

            var free = Context(board);
            Assert.That(freeHandler.Resume(free,
                new[] { EffectValue.Space(nonAdjacent) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(freeHandler.Resume(free,
                new[] { EffectValue.Space(new HexCoordinate(1, -1)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(freeHandler.Execute(free).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(board.PlacedTokens, Has.Count.EqualTo(3));
        }

        [Test]
        public void RoundCleanup_RemovesBarrierButNotTheDistinctTurretObstacle()
        {
            var board = Board();
            var turret = new ObstacleId("test-turret:" + Owner.Value);
            var turretAt = new HexCoordinate(0, 1);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, turretAt), Is.True);
            Place(board, new HexCoordinate(1, 0), 0);

            var removed = ComponentCleanupRules.TokensRemovedAtEndOfRound(board.PlacedTokens);
            Assert.That(removed.Select(token => token.Id),
                Is.EqualTo(new[] { new TokenInstanceId("barrier-0") }));
            foreach (var token in removed)
                board.CommitValidatedTokenReclamation(token.Id);

            Assert.That(board.PlacedTokens, Is.Empty);
            Assert.That(board.ObstacleOccupancies.Single().ObstacleId, Is.EqualTo(turret));
            Assert.That(board.ObstacleOccupancies.Single().Coordinate, Is.EqualTo(turretAt));
            Assert.That(DefenseBonus(board, Owner), Is.Zero);
        }

        [Test]
        public void AbsentOwner_DoesNotRemoveTurretOrBarrierProperties()
        {
            var turret = new ObstacleId("test-turret:" + Owner.Value);
            var turretAt = new HexCoordinate(0, 1);
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    new MapId("trinkets-absent-owner"), 1,
                    Enumerable.Range(-3, 7).SelectMany(q =>
                        Enumerable.Range(-3, 7).Select(r => new HexCoordinate(q, r)))),
                new[]
                {
                    new HeroOccupancy(Ally, AllyAt),
                    new HeroOccupancy(Enemy, EnemyAt)
                },
                new[] { new ObstacleOccupancy(turret, turretAt) });
            Place(board, new HexCoordinate(1, 0), 0);
            var position = SyntheticObstacleCapabilities.SpaceHandler(
                new EffectCapabilityReference(
                    new EffectCapabilityId("test-absent-owner-position"), 1),
                "test-turret");

            Assert.That(position.Execute(Context(board)).Outcomes.Single().Value.Coordinate,
                Is.EqualTo(turretAt));
            Assert.That(DefenseBonus(board, Ally), Is.EqualTo(1));
            Assert.That(board.ObstacleOccupancies, Has.Count.EqualTo(1));
        }

        [Test]
        public void ComponentPreimageRestoreAndDeterministicReplay_KeepBothKindsDistinct()
        {
            var board = Board();
            var map = board.MapDefinition;
            var initial = board.CaptureRestoreState();
            var turret = OwnedObstacleIdentity.For("test-turret", Owner);
            var turretAt = new HexCoordinate(0, 1);
            var barrierAt = new HexCoordinate(1, 0);

            Assert.That(board.TryPlaceOrRepositionObstacle(turret, turretAt), Is.True);
            Place(board, barrierAt, 0);
            var final = board.CaptureRestoreState();

            var replayed = AdjacentMoveAuthority.Restore(initial, map,
                Array.Empty<QuickGameMinionSetupState>());
            Assert.That(replayed.TryPlaceOrRepositionObstacle(turret, turretAt), Is.True);
            Place(replayed, barrierAt, 0);
            Assert.That(replayed.ObstacleOccupancies, Is.EqualTo(final.ObstacleOccupancies));
            Assert.That(replayed.PlacedTokens.Select(token => token.Id),
                Is.EqualTo(final.PlacedTokens.Select(token => token.Id)));
            Assert.That(DefenseBonus(replayed, Ally), Is.EqualTo(DefenseBonus(board, Ally)));

            var undone = AdjacentMoveAuthority.Restore(initial, map,
                Array.Empty<QuickGameMinionSetupState>());
            Assert.That(undone.ObstacleOccupancies, Is.Empty);
            Assert.That(undone.PlacedTokens, Is.Empty);
            Assert.That(undone.IsEmptyPlayableSpace(turretAt), Is.True);
            Assert.That(undone.IsEmptyPlayableSpace(barrierAt), Is.True);
        }

        [Test]
        public void GenericObstacleAppearsIdenticallyInBothOwnerScopedSeatProjections()
        {
            var configuration = MatchConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants, map, configuration.TieBreakerState);
            var occupied = setup.Heroes.Select(value => value.Coordinate)
                .Concat(setup.Minions.Select(value => value.Coordinate)).ToArray();
            var at = map.Spaces.First(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                !occupied.Contains(space.Coordinate)).Coordinate;
            var obstacle = new ObstacleOccupancy(
                new ObstacleId("synthetic-projected-turret"), at);
            var scenario = new InitialScenarioDescription(
                configuration.Participants, configuration.CardDefinitions,
                configuration.CardInstances, configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                new[] { obstacle });
            var session = new MatchSession(
                new MatchSessionInitialState(scenario, setup, new[] { map }));
            var seats = MatchSeatBinding.ForParticipants(configuration.Participants).Seats;

            Assert.That(session.Projection.Obstacles, Is.EqualTo(new[] { obstacle }));
            foreach (var seat in seats)
            {
                var scoped = SeatProjectionFactory.ForSeat(session.Projection, seat);
                Assert.That(scoped.Obstacles, Is.EqualTo(new[] { obstacle }));
            }
        }

        [Test]
        public void Barrier_DefenseReachesAdjacentFriendlyHeroesOnlyAndStacksPerToken()
        {
            var board = Board();
            Place(board, new HexCoordinate(1, 0), 0);
            Place(board, new HexCoordinate(1, -1), 1);

            Assert.That(DefenseBonus(board, Owner), Is.EqualTo(2));
            Assert.That(DefenseBonus(board, Ally), Is.EqualTo(1));
            Assert.That(DefenseBonus(board, Enemy), Is.Zero);

            board.CommitValidatedTokenReclamation(new TokenInstanceId("barrier-1"));
            Assert.That(DefenseBonus(board, Owner), Is.EqualTo(1));
            Assert.That(DefenseBonus(board, Ally), Is.EqualTo(1));
        }

        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(4, false)]
        public void RedTarget_AnswerRechecksTheLiveTurret(int redIndex, bool dualRange)
        {
            var board = Board();
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Owner);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(0, 1)), Is.True);
            var handler = SyntheticTargetingCapabilities.OwnedObstacleRangeUnitHandler(
                dualRange ? TrinketsComponentContent.DualRangeTurretUnitV1 :
                    TrinketsComponentContent.AdjacentTurretUnitV1,
                TrinketsComponentContent.TurretKind, dualRange);
            var card = TrinketsCardContent.RedDefinitions[redIndex];
            var action = card.ActionDefinitions.Single(candidate =>
                candidate.Id.Equals(TrinketsCardContent.PrimaryRangedAttack));
            var instance = new CardInstance(new CardInstanceId("red-target-probe"), card.Reference,
                Owner, CardStateFacts.InitialInHand());
            var inputs = new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.Attack),
                [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
            };
            EffectCapabilityContext Probe(HeroItemState items = null) => new EffectCapabilityContext(
                new EffectNodeId("red-target-probe-node"), EffectFailurePolicy.Mandatory,
                Owner, Owner, instance.Id, card.Reference, action.Id, inputs, 1, 1,
                Participants(), null, RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, new[] { instance }), ActionKind.Attack,
                new MatchComponents(
                    new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                        Array.Empty<MarkerDefinition>()),
                    board, new MarkerAttachmentRegister(), board, board, board,
                    obstacles: board),
                items ?? new HeroItemState(Participants()));

            var choice = new[] { EffectValue.Hero(Enemy) };
            Assert.That(handler.Execute(Probe()).Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(handler.Resume(Probe(), choice).Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(-2, 0)), Is.True);
            Assert.That(handler.Resume(Probe(), choice).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            if (redIndex == 0)
            {
                Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(-1, 0)), Is.True);
                Assert.That(handler.Resume(Probe(), choice).Kind,
                    Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                    "distance three from the Turret exceeds the printed Range two");
                var rangeItem = HeroItemState.Restore(Participants(), new HeroItemRestoreState(new[]
                {
                    new HeroItem(Owner, new CardInstanceId("range-item-probe"),
                        HeroDashboard.RulebookLayout.Slots[3].Position,
                        PrintedValueChannel.Range, 1)
                }));
                Assert.That(handler.Resume(Probe(rangeItem), choice).Kind,
                    Is.EqualTo(EffectExecutionResultKind.Applied),
                    "the actor's Range item extends both independently measured disks");
            }
            Assert.That(board.TryRemoveObstacle(turret), Is.True);
            Assert.That(handler.Execute(Probe()).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        [TestCase(false, 2)]
        [TestCase(true, 3)]
        public void RedLineBonus_RequiresBothIndependentStraightLines(bool supercharged, int bonus)
        {
            var board = Board();
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Owner);
            var card = TrinketsCardContent.RedDefinitions[supercharged ? 3 : 0];
            var action = card.ActionDefinitions.Single(candidate =>
                candidate.Id.Equals(TrinketsCardContent.PrimaryRangedAttack));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("line-bonus-probe"), card.Reference, Owner);
            var offer = new ActionOffer(instance.Id, card.Reference, action.Id, Owner,
                ActionInvocationContext.PrintedPrimary, true, AttackRangeKind.Ranged, default);
            int LineBonus() => RosterDynamicValueQuery.Contributions(
                    action, offer, AttackTarget.Hero(Enemy), PrintedValueChannel.Attack,
                    new[] { card }, new[] { instance }, Participants(), board,
                    RuleModificationSet.Empty, EffectRosterState.Absent, 1, 1,
                    spaces: board, obstacles: board)
                .Sum(contribution => contribution.Amount);

            Assert.That(LineBonus(), Is.Zero, "no Turret means no bonus");
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(0, 1)), Is.True);
            Assert.That(LineBonus(), Is.EqualTo(bonus), "both origins line up with the target");
            board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Owner), new HexCoordinate(1, 0));
            Assert.That(LineBonus(), Is.Zero, "the Turret's line alone is insufficient");
            board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Owner), OwnerAt);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(1, 0)), Is.True);
            Assert.That(LineBonus(), Is.Zero, "the actor's line alone is insufficient");
            Assert.That(board.TryRemoveObstacle(turret), Is.True);
            Assert.That(LineBonus(), Is.Zero, "removal loses the bonus without changing the card");
        }

        [Test]
        public void RedTarget_SmokeBombBetweenTrinketsAndEnemyScreensDespiteTurretOffThatLine()
        {
            var board = Board();
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Owner);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(-1, 2)), Is.True);
            var card = TrinketsCardContent.RedDefinitions[0];
            var action = card.ActionDefinitions.Single(candidate =>
                candidate.Id.Equals(TrinketsCardContent.PrimaryRangedAttack));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("smoke-red-probe"), card.Reference, Owner);
            var inputs = new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.Attack),
                [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
            };
            var handler = TrinketsComponentContent.ObstacleHandlers().Single(candidate =>
                candidate.Reference.Equals(TrinketsComponentContent.DualRangeTurretUnitV1));
            EffectCapabilityContext Probe(RuleModificationSet modifications) =>
                new EffectCapabilityContext(
                    new EffectNodeId("smoke-red-node"), EffectFailurePolicy.Mandatory,
                    Owner, Owner, instance.Id, card.Reference, action.Id, inputs, 1, 1,
                    Participants(), null, modifications,
                    EffectCardState.Of(new[] { card }, new[] { instance }), ActionKind.Attack,
                    new MatchComponents(
                        new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                            Array.Empty<MarkerDefinition>()),
                        board, new MarkerAttachmentRegister(), board, board, board,
                        obstacles: board));

            Assert.That(handler.Execute(Probe(RuleModificationSet.Empty)).Choice.Candidates,
                Does.Contain(EffectValue.Hero(Enemy)));
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId("enemy-smoke"), MinComponentContent.SmokeBombDefinition,
                Enemy, Enemy, new CardInstanceId("enemy-smoke-card"), new HexCoordinate(0, 1)));
            var screened = RuleModificationQuery.Build(
                null, board, Participants(), TieBreakerState.Unrevealed,
                UnlockedUltimateSet.None, RuleObject.ForHero(Owner), null, null);
            var result = handler.Execute(Probe(screened));
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Min's token screens the line from Trinkets, not a line from the Turret");
        }

        [TestCase(0, 2, true)]
        [TestCase(1, 3, true)]
        [TestCase(2, 4, false)]
        [TestCase(3, 3, true)]
        [TestCase(4, 4, false)]
        public void RedTarget_RangeItemExtendsActorsPrintedBoundaryOnEveryFace(
            int redIndex, int printedRange, bool dualRange)
        {
            var targetAt = new HexCoordinate(0, printedRange + 1);
            var board = Board(printedRange + 2, targetAt);
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Owner);
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(0, printedRange)), Is.True);
            var card = TrinketsCardContent.RedDefinitions[redIndex];
            var action = card.ActionDefinitions.Single(candidate =>
                candidate.Id.Equals(TrinketsCardContent.PrimaryRangedAttack));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("range-boundary-probe"), card.Reference, Owner);
            var handler = TrinketsComponentContent.ObstacleHandlers().Single(candidate =>
                candidate.Reference.Equals(dualRange
                    ? TrinketsComponentContent.DualRangeTurretUnitV1
                    : TrinketsComponentContent.AdjacentTurretUnitV1));
            var inputs = new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.Attack),
                [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
            };
            EffectCapabilityContext Probe(HeroItemState items) => new EffectCapabilityContext(
                new EffectNodeId("range-boundary-node"), EffectFailurePolicy.Mandatory,
                Owner, Owner, instance.Id, card.Reference, action.Id, inputs, 1, 1,
                Participants(), null, RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, new[] { instance }), ActionKind.Attack,
                new MatchComponents(
                    new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                        Array.Empty<MarkerDefinition>()),
                    board, new MarkerAttachmentRegister(), board, board, board,
                    obstacles: board), items);
            var noItems = new HeroItemState(Participants());
            Assert.That(handler.Execute(Probe(noItems)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            var rangeItem = HeroItemState.Restore(Participants(), new HeroItemRestoreState(new[]
            {
                new HeroItem(Owner, new CardInstanceId("range-boundary-item"),
                    HeroDashboard.RulebookLayout.Slots[3].Position,
                    PrintedValueChannel.Range, 1)
            }));
            Assert.That(handler.Execute(Probe(rangeItem)).Choice.Candidates,
                Does.Contain(EffectValue.Hero(Enemy)));
            Assert.That(handler.Resume(Probe(rangeItem), new[] { EffectValue.Hero(Enemy) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RedTarget_EnemyMinionIsAUnitButTurretIsNot(bool dualRange)
        {
            var minion = new MinionId("trinkets-red-minion");
            var minionAt = new HexCoordinate(-1, 2);
            var board = Board(minions: new[]
            {
                new QuickGameMinionSetupState(
                    minion, new TeamId("blue"), TeamSide.Blue, StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(
                        new MapId("trinkets-components-test"), new SpawnPointId("probe-spawn")),
                    new ConfiguredSpaceReference(
                        new MapId("trinkets-components-test"), new SpaceId("q-1_r2")),
                    minionAt)
            });
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Owner);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(0, 1)), Is.True);
            var card = TrinketsCardContent.RedDefinitions[dualRange ? 0 : 2];
            var action = card.ActionDefinitions.Single(candidate =>
                candidate.Id.Equals(TrinketsCardContent.PrimaryRangedAttack));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("red-minion-probe"), card.Reference, Owner);
            var handler = TrinketsComponentContent.ObstacleHandlers().Single(candidate =>
                candidate.Reference.Equals(dualRange
                    ? TrinketsComponentContent.DualRangeTurretUnitV1
                    : TrinketsComponentContent.AdjacentTurretUnitV1));
            var result = handler.Execute(new EffectCapabilityContext(
                new EffectNodeId("red-minion-node"), EffectFailurePolicy.Mandatory,
                Owner, Owner, instance.Id, card.Reference, action.Id,
                new Dictionary<string, EffectValue>
                {
                    ["activity"] = EffectValue.Integer((int)RuleActivity.Attack),
                    [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                        EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
                },
                1, 1, Participants(), null, RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, new[] { instance }), ActionKind.Attack,
                new MatchComponents(
                    new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                        Array.Empty<MarkerDefinition>()),
                    board, new MarkerAttachmentRegister(), board, board, board,
                    obstacles: board)));
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates, Does.Contain(EffectValue.Minion(minion)));
            Assert.That(result.Choice.Candidates.All(candidate =>
                candidate.Kind == EffectValueKind.Hero || candidate.Kind == EffectValueKind.Minion),
                Is.True, "the Turret itself is an obstacle, never a unit target");
        }

        private static int DefenseBonus(AdjacentMoveAuthority board, HeroId hero) =>
            RuleModificationQuery.Build(
                    null, board, Participants(), TieBreakerState.Unrevealed,
                    UnlockedUltimateSet.None, RuleObject.None, null, null)
                .For(RuleObject.ForHero(hero))
                .Where(value => value.Modification.Kind == RuleModificationKind.ValueModifier &&
                                value.Modification.Channel == PrintedValueChannel.Defense)
                .Sum(value => value.Modification.Amount);

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, IReadOnlyDictionary<string, EffectValue> inputs = null) =>
            new EffectCapabilityContext(
                new EffectNodeId("test-obstacle-node"), EffectFailurePolicy.Mandatory,
                Owner, Owner, new CardInstanceId("test-source"),
                GuardsDigital.Core.Cards.Verified.TaliCardContent.Definition(
                    GuardsDigital.Core.Cards.Verified.TaliCardContent.GlacialBarrier).Reference,
                GuardsDigital.Core.Cards.Verified.TaliCardContent.PrimarySkill,
                inputs ?? new Dictionary<string, EffectValue>(), 1, 1, Participants(), null,
                RuleModificationSet.Empty,
                EffectCardState.Of(
                    GuardsDigital.Core.Cards.Verified.TaliCardContent.AllDefinitions,
                    Array.Empty<CardInstance>()),
                ActionKind.Skill,
                new MatchComponents(
                    new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                        Array.Empty<MarkerDefinition>()),
                    board, new MarkerAttachmentRegister(), board, board, board,
                    obstacles: board));

        private static void Place(AdjacentMoveAuthority board, HexCoordinate at, int index) =>
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId($"barrier-{index}"), TrinketsComponentContent.Barrier,
                Owner, Owner, new CardInstanceId("barrier-source"), at));

        private static AdjacentMoveAuthority Board(
            int radius = 3, HexCoordinate? enemyCoordinate = null,
            QuickGameMinionSetupState[] minions = null)
        {
            var spaces = Enumerable.Range(-radius, radius * 2 + 1)
                .SelectMany(q => Enumerable.Range(-radius, radius * 2 + 1)
                    .Select(r => new HexCoordinate(q, r)))
                .ToArray();
            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("trinkets-components-test"), 1, spaces),
                new[]
                {
                    new HeroOccupancy(Owner, OwnerAt),
                    new HeroOccupancy(Ally, AllyAt),
                    new HeroOccupancy(Enemy, enemyCoordinate ?? EnemyAt)
                },
                Array.Empty<ObstacleOccupancy>(),
                minions ?? Array.Empty<QuickGameMinionSetupState>());
        }

        private static MatchParticipants Participants() => new MatchParticipants(
            new[]
            {
                new Team(new TeamId("red"), TeamSide.Red),
                new Team(new TeamId("blue"), TeamSide.Blue)
            },
            new[]
            {
                new Player(new PlayerId("owner"), new TeamId("red")),
                new Player(new PlayerId("ally"), new TeamId("red")),
                new Player(new PlayerId("enemy"), new TeamId("blue"))
            },
            new[]
            {
                new Hero(Owner, new PlayerId("owner"), HeroDashboard.RulebookLayout),
                new Hero(Ally, new PlayerId("ally"), HeroDashboard.RulebookLayout),
                new Hero(Enemy, new PlayerId("enemy"), HeroDashboard.RulebookLayout)
            });
    }
}
