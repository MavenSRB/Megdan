using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.32c: spaces that are obstacles for the enemy units only, the shape of Takahide's
    /// Spinning Blade and Blade Helix - "After the attack: This turn: Empty spaces adjacent to you [in
    /// radius] count as obstacles for the enemy units." TK-Q3 (the user, 2026-10-01): an enemy unit
    /// cannot move through or end on those spaces (a piece ignoring obstacles still passes through),
    /// cannot be placed, pushed or respawned into them; the spaces are read around Takahide's current
    /// space and "empty" is read when asked; friendly units, tokens, and every question naming no piece
    /// are unchanged; the effect ends with the turn. Proven on <see cref="SyntheticEnemyObstacleHand"/>.
    /// </summary>
    public sealed class TakahideEnemyObstacleTests
    {
        private static readonly HexCoordinate TakaAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(1, 0);
        private static readonly HexCoordinate AllyAt = new HexCoordinate(-2, 0);

        // The ring around Takahide (0,0), and the enemy's other neighbours.
        private static readonly HexCoordinate RingNorthEast = new HexCoordinate(1, -1);
        private static readonly HexCoordinate RingSouth = new HexCoordinate(0, 1);
        private static readonly HexCoordinate RingWest = new HexCoordinate(-1, 0);

        [Test]
        public void TheEnemysMovement_AvoidsTheEmptySpacesAdjacentToTakahide()
        {
            var stage = new Stage(SyntheticEnemyObstacleHand.BladeId);
            Assert.That(stage.EnemyMovementDestinations(), Is.SupersetOf(new[] { RingNorthEast, RingSouth }),
                "before the attack the enemy may step beside Takahide");
            stage.AttackAndDefend();
            stage.ReleaseIfAsked();
            Assert.That(stage.Turn.SelectedActor.ActorId, Is.EqualTo(Stage.Enemy));

            var destinations = stage.EnemyMovementDestinations();
            Assert.That(destinations, Has.No.Member(RingNorthEast).And.No.Member(RingSouth),
                "\"Empty spaces adjacent to you count as obstacles for the enemy units\"");
            Assert.That(destinations, Is.SupersetOf(new[] { new HexCoordinate(2, 0), new HexCoordinate(1, 1), new HexCoordinate(2, -1) }),
                "every other space is untouched");
        }

        [Test]
        public void AFriendlyUnit_IsUnaffected()
        {
            var stage = StageAfterTheAttack(SyntheticEnemyObstacleHand.BladeId);
            var mods = stage.Modifications;
            Assert.That(Reach(stage, AllyAt, RuleObject.ForHero(Stage.Ally), 1, mods, false), Has.Member(RingWest),
                "\"for the enemy units\": Takahide's teammate still steps beside him");
            Assert.That(ObstacleQuery.BlocksEndOfMovement(stage.Board, RingWest, mods, RuleObject.ForHero(Stage.Ally)), Is.False);
            Assert.That(ObstacleQuery.BlocksEndOfMovement(stage.Board, RingWest, mods, RuleObject.ForHero(Stage.Enemy)), Is.True);
            Assert.That(ObstacleQuery.BlocksEndOfMovement(stage.Board, RingWest, mods), Is.False,
                "a question naming no piece is unchanged (TK-Q3: presences, lines and ranges)");
            Assert.That(mods.AnyOverlayMakesAnObstacleOf(RingWest), Is.False);
        }

        [Test]
        public void AnEnemyThatIgnoresObstacles_PassesThrough_ButCannotEndThere()
        {
            var stage = StageAfterTheAttack(SyntheticEnemyObstacleHand.BladeId);
            var beyond = new HexCoordinate(-1, 2);
            var enemy = RuleObject.ForHero(Stage.Enemy);
            Assert.That(Reach(stage, EnemyAt, enemy, 2, stage.Modifications, false), Has.No.Member(beyond),
                "the only two-step route runs through the barred space beside Takahide");
            var ignoring = Reach(stage, EnemyAt, enemy, 2, stage.Modifications, true);
            Assert.That(ignoring, Has.Member(beyond), "a piece ignoring obstacles passes through (p.18)");
            Assert.That(ignoring, Has.No.Member(RingSouth), "but never ends on one (p.18 Ignoring Obstacles)");
        }

        [Test]
        public void APush_StopsAnEnemyBeforeTheSpace_AndNotAFriend()
        {
            var stage = StageAfterTheAttack(SyntheticEnemyObstacleHand.BladeId);
            var mods = stage.Modifications;
            var enemyPush = PushPathQuery.Resolve(stage.Board, new HexCoordinate(2, -1), EnemyAt, 1,
                RuleObject.ForHero(Stage.Enemy), false, mods);
            Assert.That(enemyPush.ActualDistance, Is.Zero, "pushed toward the barred space, the enemy moves 0");
            Assert.That(enemyPush.StoppedByAnObstacle, Is.True);
            var allyPush = PushPathQuery.Resolve(stage.Board, new HexCoordinate(-3, 0), AllyAt, 1,
                RuleObject.ForHero(Stage.Ally), false, mods);
            Assert.That(allyPush.Endpoint, Is.EqualTo(RingWest), "the teammate is pushed beside Takahide");
        }

        [Test]
        public void APlacement_IsRefusedForAnEnemyUnit_AndNotAFriend()
        {
            var stage = StageAfterTheAttack(SyntheticEnemyObstacleHand.BladeId);
            var policy = new PlacePolicy(
                PlaceSourceContainer.Board, PlaceEligiblePieceKind.GamePiece,
                PlaceDestinationScope.EmptySpacesWithinRadius, 1, PlaceChooserKind.ActingHero,
                PlaceNoDestinationPolicy.Impossible, PlaceDestinationAnchor.ActingHero);
            var forEnemy = PlaceDestinationQuery.FindDestinations(policy, stage.Board, TakaAt,
                Array.Empty<HexCoordinate>(), stage.Modifications, placedPiece: RuleObject.ForHero(Stage.Enemy));
            var forAlly = PlaceDestinationQuery.FindDestinations(policy, stage.Board, TakaAt,
                Array.Empty<HexCoordinate>(), stage.Modifications, placedPiece: RuleObject.ForHero(Stage.Ally));
            Assert.That(forEnemy, Is.Empty, "p.18 places into an empty space; these are not empty for an enemy unit");
            Assert.That(forAlly, Is.SupersetOf(new[] { RingNorthEast, RingSouth, RingWest }));
        }

        [Test]
        public void TheSpacesFollowTakahide_AndAreReadWhenAsked()
        {
            var stage = StageAfterTheAttack(SyntheticEnemyObstacleHand.BladeId);
            Assert.That(stage.Board.CommitValidatedDisplacement(
                BoardObjectReference.ForHero(Stage.Taka), new HexCoordinate(0, -1)).Accepted, Is.True,
                "Takahide is moved after the effect began");
            var mods = stage.Turn.CurrentModifications();
            var enemy = RuleObject.ForHero(Stage.Enemy);
            Assert.That(ObstacleQuery.BlocksEndOfMovement(stage.Board, RingSouth, mods, enemy), Is.False,
                "(0,1) is no longer beside him (p.17: the disk moves with its hero)");
            Assert.That(ObstacleQuery.BlocksEndOfMovement(stage.Board, TakaAt, mods, enemy), Is.True,
                "and the space he left, now empty and beside him, is barred");
        }

        [Test]
        public void BladeHelix_ReachesItsPrintedRadius()
        {
            var blade = StageAfterTheAttack(SyntheticEnemyObstacleHand.BladeId);
            var helix = StageAfterTheAttack(SyntheticEnemyObstacleHand.HelixId);
            var two = new HexCoordinate(2, -1);
            var enemy = RuleObject.ForHero(Stage.Enemy);
            Assert.That(ObstacleQuery.BlocksEndOfMovement(blade.Board, two, blade.Modifications, enemy), Is.False,
                "two spaces away is not adjacent");
            Assert.That(ObstacleQuery.BlocksEndOfMovement(helix.Board, two, helix.Modifications, enemy), Is.True,
                "\"in radius\": the printed Radius 2 (R1)");
            Assert.That(ObstacleQuery.BlocksEndOfMovement(helix.Board, new HexCoordinate(3, -1), helix.Modifications, enemy),
                Is.False);
        }

        [Test]
        public void TheEffectEndsWithTheTurn()
        {
            var stage = StageAfterTheAttack(SyntheticEnemyObstacleHand.BladeId);
            stage.HoldForTheSelectedHero();
            stage.HoldForTheSelectedHero();
            stage.CommitTurnTwo();
            var enemy = RuleObject.ForHero(Stage.Enemy);
            Assert.That(ObstacleQuery.BlocksEndOfMovement(stage.Board, RingSouth, stage.Turn.CurrentModifications(), enemy),
                Is.False, "\"This turn\" ended");
        }

        [Test]
        public void TheEffectSurvivesARestore()
        {
            var stage = StageAfterTheAttack(SyntheticEnemyObstacleHand.BladeId);
            var restored = stage.Restore(stage.Snapshot());
            Assert.That(restored.EnemyMovementDestinations(), Is.EquivalentTo(stage.EnemyMovementDestinations()));
            Assert.That(restored.EnemyMovementDestinations(), Has.No.Member(RingSouth));
        }

        [Test]
        public void AnEnemyHeroCannotRespawnIntoABarredSpace()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var redSpawns = map.SpawnPoints
                .Where(spawn => spawn.TeamId == "red" && spawn.UnitType == "hero")
                .Select(spawn => map.Spaces.Single(space => space.Id.Equals(spawn.SpaceId)).Coordinate)
                .ToArray();
            var barred = redSpawns[0];
            var playable = new HashSet<HexCoordinate>(map.Spaces
                .Where(space => space.Kind != BoardMapSpaceKind.Terrain).Select(space => space.Coordinate));
            var takaAt = barred.GetNeighbors().First(neighbor => playable.Contains(neighbor) && !redSpawns.Contains(neighbor));
            var board = new AdjacentMoveAuthority(map,
                new[] { new HeroOccupancy(Stage.Taka, takaAt) }, Array.Empty<ObstacleOccupancy>());
            var overlay = AppliedObstacleOverlay.Bind(
                ConditionalObstacleOverlay.DeclareForUnits(SourceTeamRelation.OpposingSource),
                new[] { takaAt }, 1, "test-overlay", Stage.Taka, TeamSide.Blue, null, default,
                new[] { RuleObject.ForHero(Stage.Enemy) });
            var mods = new RuleModificationSet(Array.Empty<AppliedRuleModification>(), new[] { overlay },
                new TieBreakerState(TeamSide.Red));

            var plain = HeroRespawnRules.CreateChoice(Stage.Enemy, new CardInstanceId("c"), participants, board,
                RuleModificationSet.Empty);
            var withOverlay = HeroRespawnRules.CreateChoice(Stage.Enemy, new CardInstanceId("c"), participants, board, mods);
            Assert.That(plain.Destinations.Select(value => value.Coordinate), Has.Member(barred));
            Assert.That(withOverlay.Destinations.Select(value => value.Coordinate), Has.No.Member(barred),
                "TK-Q3: an enemy hero cannot respawn into a barred space");
            Assert.That(withOverlay.Destinations, Is.Not.Empty, "the other spawn points stay");
        }

        private static Stage StageAfterTheAttack(CardDefinitionId card)
        {
            var stage = new Stage(card);
            stage.AttackAndDefend();
            stage.ReleaseIfAsked();
            return stage;
        }

        private static IReadOnlyList<HexCoordinate> Reach(
            Stage stage, HexCoordinate origin, RuleObject mover, int allowance, RuleModificationSet mods, bool ignores) =>
            MovementReachabilityQuery.FindReachableDestinations(origin, mover, stage.Board, allowance, mods, ignores)
                .Select(value => value.Destination).ToArray();

        // -------------------------------------------------------------------------------------------
        // A staged round: Takahide (Blue) attacks the enemy (Red) beside him; his teammate stands apart
        // -------------------------------------------------------------------------------------------

        internal sealed class Stage
        {
            internal static readonly HeroId Taka = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly HeroId Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            private static readonly CardInstanceId TakaPlayed = new CardInstanceId("taka-played");
            private static readonly CardInstanceId AllyPlayed = new CardInstanceId("ally-played");
            private static readonly CardInstanceId EnemyPlayed = new CardInstanceId("enemy-played");
            private static readonly CardInstanceId EnemyWall = new CardInstanceId("enemy-wall");
            private static readonly ActionDefinitionId Movement = new ActionDefinitionId("movement");

            private readonly MatchContentCatalogs catalogs;

            internal Stage(CardDefinitionId played)
            {
                catalogs = CreateCatalogs();
                Definitions = SyntheticEnemyObstacleHand.Definitions
                    .Concat(new[] { Plain(5), Plain(2), Plain(9), Plain(8), Plain(7), Wall }).ToArray();
                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(TakaPlayed, Definitions.Single(value => value.Id.Equals(played)).Reference, Taka),
                    CardInstance.CreateInHand(EnemyPlayed, Plain(5).Reference, Enemy),
                    CardInstance.CreateInHand(EnemyWall, Wall.Reference, Enemy),
                    CardInstance.CreateInHand(AllyPlayed, Plain(2).Reference, Ally),
                    CardInstance.CreateInHand(new CardInstanceId("taka-next"), Plain(9).Reference, Taka),
                    CardInstance.CreateInHand(new CardInstanceId("enemy-next"), Plain(8).Reference, Enemy),
                    CardInstance.CreateInHand(new CardInstanceId("ally-next"), Plain(7).Reference, Ally)
                };
                Cards = cards.ToArray();
                var spaces = (from q in Enumerable.Range(-4, 9)
                              from r in Enumerable.Range(-4, 9)
                              where Math.Abs(q + r) <= 4
                              select new HexCoordinate(q, r)).ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("enemy-obstacles"), 1, spaces),
                    new[]
                    {
                        new HeroOccupancy(Taka, TakaAt),
                        new HeroOccupancy(Enemy, EnemyAt),
                        new HeroOccupancy(Ally, AllyAt)
                    },
                    Array.Empty<ObstacleOccupancy>());
                var markers = new MarkerAttachmentRegister();
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, StagedBoardParticipants.CreateWithBlueAlly(), new TieBreakerState(TeamSide.Blue), 1,
                    new RoundResolutionComposition(new CardActionResolutionAuthority(catalogs, Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
                Accept(Round.TryCommitCard(Taka, TakaPlayed));
                Accept(Round.TryCommitCard(Enemy, EnemyPlayed));
                Accept(Round.TryCommitCard(Ally, AllyPlayed));
            }

            private Stage(Stage template, string snapshot)
            {
                catalogs = template.catalogs;
                Definitions = template.Definitions;
                var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, snapshot, Definitions,
                    new[] { template.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Round = restored.Round;
                Board = restored.Board;
                Cards = restored.Cards.ToArray();
            }

            private static MatchContentCatalogs CreateCatalogs()
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                return new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)
                        .Concat(SyntheticEnemyObstacleHand.Handlers())),
                    defaults.Components,
                    defaults.TriggeredEffects);
            }

            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal RuleModificationSet Modifications => Turn.CurrentModifications();

            internal Stage Restore(string snapshot) => new Stage(this, snapshot);
            internal string Snapshot() => AuthoritativeSnapshotSerializer.Serialize(Round, Board);

            /// <summary>The enemy's one-space Movement, asked as an offer would ask it.</summary>
            internal IReadOnlyList<HexCoordinate> EnemyMovementDestinations() =>
                MovementReachabilityQuery.FindReachableDestinations(
                        new HeroOccupancy(Enemy, EnemyAt), Board, 1, Turn.CurrentModifications())
                    .Select(value => value.Destination).ToArray();

            internal void AttackAndDefend()
            {
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Taka));
                var offer = Turn.GetTurnActionOffers().Single(value =>
                    value.SourceCardInstanceId.Equals(TakaPlayed) &&
                    value.ActionDefinitionId.Equals(SyntheticEnemyObstacleHand.PrimaryId));
                Accept(Round.TryStartAttack(new ActionResolutionId("blade"), offer));
                var target = Turn.PendingAttackTargetChoice;
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)));
                var defense = Turn.PendingDefenseChoice;
                Accept(Round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, EnemyWall)));
            }

            internal void HoldForTheSelectedHero()
            {
                Accept(Round.TryPerformHold(Turn.SelectedActor.CardInstanceId));
                ReleaseIfAsked();
            }

            internal void CommitTurnTwo()
            {
                Accept(Round.TryCommitCard(Taka, new CardInstanceId("taka-next")));
                Accept(Round.TryCommitCard(Enemy, new CardInstanceId("enemy-next")));
                Accept(Round.TryCommitCard(Ally, new CardInstanceId("ally-next")));
            }

            internal void ReleaseIfAsked()
            {
                if (Turn != null && Turn.PendingTurnRelease != null)
                {
                    Accept(Round.TryReleaseTurn());
                }
            }

            private void Accept(RoundResolutionResult result) =>
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);

            private static CardDefinition Plain(int initiative) => new CardDefinition(
                new CardDefinitionId("enemy-obstacle-plain-" + initiative), "rev-1", "TEST - Initiative " + initiative,
                new InitiativeValue(initiative), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(Movement,
                        ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                });

            private static readonly CardDefinition Wall = new CardDefinition(
                new CardDefinitionId("enemy-obstacle-wall"), "rev-1", "TEST - Wall",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 12) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                });
        }
    }
}
