using System;
using System.Collections.Generic;
using System.Linq;
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
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Min the Dragonmonk's Smoke bomb and its targeting screen. Milestone 8.61a.
    ///
    /// <para>Two printed sentences are measured here and they are one card's: "Place a [Smoke bomb]
    /// smoke bomb token in radius; enemy heroes cannot target you or another unit if the smoke bomb
    /// token is on a straight line between that enemy hero and their target." The first half is a p.18
    /// Place with p.19's lifetime; the second is the first rule in this project that answers about a
    /// PAIR of positions rather than about an object, which is why it is read at the one place p.19's
    /// Target has an owner - <see cref="TargetQuery.IsPrevented"/> - and nowhere else.</para>
    ///
    /// <para>The board is a radius-4 disc centred on the acting hero, because every claim below is
    /// about a file running out of one space: six directions, a bomb standing one or two spaces along
    /// it, and a unit at two or three. Min herself is parked at (2,1), which is on no file from the
    /// centre at all, so she never accidentally sits on a line under test.</para>
    /// </summary>
    public sealed class MinSmokeBombTests
    {
        private static readonly MapId MapIdentity = new MapId("min-smoke-bomb-map");
        private static readonly SpawnPointId Spawn = new SpawnPointId("min-smoke-spawn");

        private static readonly HeroId Min = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        /// <summary>Distance 3 from the centre and on none of its six files.</summary>
        private static readonly HexCoordinate MinsCorner = new HexCoordinate(2, 1);

        /// <summary>A second space off every file from the centre, for the bomb that must not screen.</summary>
        private static readonly HexCoordinate OffEveryFile = new HexCoordinate(1, 1);

        /// <summary>The one space the map declares a Spawn point on - p.18's own removal case.</summary>
        private static readonly HexCoordinate SpawnSpace = new HexCoordinate(-4, 0);

        private static readonly MinionId RedMinion = new MinionId("min-smoke-red-minion");

        private static readonly HexDirection[] EveryAxis =
        {
            HexDirection.East,
            HexDirection.NorthEast,
            HexDirection.NorthWest,
            HexDirection.West,
            HexDirection.SouthWest,
            HexDirection.SouthEast
        };

        // -------------------------------------------------------------------------------------
        // The piece itself.
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheSmokeBombIsOnePieceThatLeavesAtEndOfRoundAndCarriesExactlyOneScreen()
        {
            var definition = MinComponentContent.SmokeBombDefinition;

            Assert.That(definition.DisplayName, Is.EqualTo("Smoke bomb"),
                "The printed glyph's own name, as her six faces spell it.");
            Assert.That(definition.SupplyCount, Is.EqualTo(1),
                "MI-Q1's working count: every sentence of hers says \"a\" or \"the\" Smoke bomb and " +
                "no face ever places two.");
            Assert.That(definition.RemovalCondition, Is.EqualTo(TokenRemovalCondition.EndOfRoundCleanup),
                "p.19: \"Unless the card text says otherwise, tokens are removed at the end of " +
                "round\" - and her card text does not say otherwise.");
            Assert.That(definition.SharesASupply, Is.False,
                "Her mines share a pile (8.61d); the bomb is its own piece.");

            var property = definition.Properties.Single();
            Assert.That(property.AffectedObject, Is.EqualTo(TokenPropertyAffectedObject.TheTokenItself),
                "The rule is geometric and its geometry is the token's own space; addressed to the " +
                "enemy heroes the sentence names, each copy would have had to carry a coordinate an " +
                "applied modification bound to a hero cannot hold.");
            Assert.That(property.Modification.Kind,
                Is.EqualTo(RuleModificationKind.EnemyHeroesCannotTargetThroughThisTokensSpace));
            Assert.That(property.Modification.Activities, Is.Empty,
                "The sentence names no activity: p.19's Target is every effect on a unit at once.");
        }

        [Test]
        public void APlacedBombPublishesItsScreenAsOneModificationAddressedToItself()
        {
            var board = Board();
            var token = Place(board, Origin.GetNeighbor(HexDirection.East));

            var screens = Set(board).TargetingScreens;

            Assert.That(screens.Count, Is.EqualTo(1));
            Assert.That(screens[0].AffectedObject, Is.EqualTo(RuleObject.ForToken(token.Id)));
            Assert.That(screens[0].ControllerTeamSide, Is.EqualTo(TeamSide.Red),
                "\"Enemy\" is measured against the side that placed it, which is what the applied " +
                "modification already carries.");
            Assert.That(Set(Board()).TargetingScreens, Is.Empty,
                "With no bomb on the board nothing screens, which is what keeps every existing " +
                "targeting answer in this build exactly as it was.");
        }

        // -------------------------------------------------------------------------------------
        // p.18's Place: from the supply, as a relocation, and with nowhere to go.
        // -------------------------------------------------------------------------------------

        [TestCase(0, 3)]
        [TestCase(1, 4)]
        public void TheSilverPlacesFromTheSupplyAnywhereInsideItsPrintedRadius(int item, int reach)
        {
            var board = Board();
            var handler = Handler(MinComponentContent.PlaceSmokeBombInRadiusV1);

            var result = handler.Execute(Context(board, items: item == 0 ? null : RadiusItem()));

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            var offered = result.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
            var expected = board.MapDefinition.PlayableCoordinates
                .Where(space => HexDistanceQuery.Calculate(MinsCorner, space) <= reach &&
                    board.IsEmptyPlayableSpace(space))
                .ToArray();
            Assert.That(offered, Is.EquivalentTo(expected),
                "p.9: the Radius icon is the maximum, and p.16's item raises the ICON - so a Radius " +
                "item makes the printed 3 reach 4 without the placement knowing an item exists.");
            Assert.That(offered, Has.No.Member(MinsCorner),
                "p.18's Place takes an EMPTY space; Min is standing on this one.");
        }

        [Test]
        public void WithTheBombStandingTheSilverRelocatesThatSamePieceRatherThanDrawingASecond()
        {
            var first = Origin.GetNeighbor(HexDirection.East);
            var destination = Origin.GetNeighbor(HexDirection.West);
            var board = Board(minsSpace: Origin, enemyAt: MinsCorner);
            var standing = Place(board, first);

            var probe = new SkillProbe(board);
            Assert.That(probe.Choice, Is.Not.Null,
                "The presence fact is a read-only branch, so the first pause is the Place itself.");
            probe.Pick(EffectValue.Space(destination));

            Assert.That(probe.Step.IsComplete, Is.True);
            var placed = board.PlacedTokens.Single();
            Assert.That(placed.Id, Is.EqualTo(standing.Id),
                "With one piece in the box a second Place cannot come from the supply, so the " +
                "sentence moves the piece that is out - Widget's Dragon Bond arm, and the reason " +
                "p.18's short-supply reclaim never fires here.");
            Assert.That(placed.Coordinate, Is.EqualTo(destination));
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(1));
        }

        [Test]
        public void WithNoBombStandingTheSilverTakesTheSupplyArm()
        {
            var board = Board(minsSpace: Origin, enemyAt: MinsCorner);
            var probe = new SkillProbe(board);
            probe.Pick(EffectValue.Space(Origin.GetNeighbor(HexDirection.West)));

            Assert.That(probe.Step.IsComplete, Is.True);
            Assert.That(board.PlacedTokens.Single().Definition.Reference,
                Is.EqualTo(MinComponentContent.SmokeBombDefinition.Reference));
        }

        [Test]
        public void AMandatoryPlaceWithNoEmptySpaceInRadiusIsAnImpossibleStep()
        {
            // A board of exactly Min's own space: p.18's Place needs an empty one and there is none.
            var board = new AdjacentMoveAuthority(
                Map(new[] { MinsCorner }),
                new[] { new HeroOccupancy(Min, MinsCorner) },
                Array.Empty<ObstacleOccupancy>());

            var handler = Handler(MinComponentContent.PlaceSmokeBombInRadiusV1);

            Assert.That(handler.Execute(Context(board)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "p.10: a mandatory step that cannot be performed is impossible, not silently skipped.");
            Assert.That(handler.Anticipate(Context(board)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "The probe and the operation answer the same question, so an offer cannot be shown " +
                "and then refused.");
        }

        // -------------------------------------------------------------------------------------
        // p.19's lifetime, and the three removals that are not her card's.
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheBombIsTakenByTheEndOfRoundCleanup()
        {
            var board = Board();
            var token = Place(board, Origin.GetNeighbor(HexDirection.East));

            Assert.That(
                ComponentCleanupRules.TokensRemovedAtEndOfRound(board.PlacedTokens).Select(t => t.Id),
                Is.EqualTo(new[] { token.Id }),
                "p.8's \"Remove all tokens from the board\", which p.19 says applies unless the card " +
                "text says otherwise.");
        }

        [Test]
        public void ASpawnOntoItsSpaceTakesIt_AndNothingRefusesAnEnemysClear()
        {
            var board = Board();
            var token = Place(board, SpawnSpace);

            Assert.That(
                TargetQuery.IsPrevented(
                    RuleObject.ForToken(token.Id), RuleActivity.Clear, RuleObject.ForHero(Enemy),
                    board, Participants, Set(board)),
                Is.False,
                "The bomb prints no immunity, and its own screen is about units - so p.13's Clear " +
                "answers it. A screen that protected the token would have made the counterplay " +
                "vanish.");

            var removed = board.CommitValidatedTokenRemovalBeforeUnitSpawn(
                new ConfiguredSpawnPointReference(MapIdentity, Spawn));

            Assert.That(removed?.Id, Is.EqualTo(token.Id),
                "p.18's Spawn-point rule: a token covering the point is removed before the unit " +
                "arrives.");
            Assert.That(Set(board).TargetingScreens, Is.Empty,
                "Nothing is stored: the screen is derived from the placed token and goes with it.");
        }

        [Test]
        public void MinsOwnDefeatNeitherRemovesTheBombNorStopsItScreening()
        {
            var target = Step(HexDirection.East, 2);
            var board = Board(enemyAllyAt: target, minIsOffTheBoard: true);
            Place(board, Origin.GetNeighbor(HexDirection.East));

            Assert.That(board.Occupancies.Any(value => value.HeroId.Equals(Min)), Is.False,
                "The precondition: she is off the board, which is where p.13 puts a defeated hero.");
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(1),
                "p.19: \"Tokens are not removed when the hero who placed them is defeated.\"");
            Assert.That(ScreenedAttack(board, Enemy, RuleObject.ForHero(HeroAt(board, target))), Is.True,
                "Her controller is provenance rather than lifetime, so the smoke goes on screening " +
                "while she is gone.");
        }

        // -------------------------------------------------------------------------------------
        // MI-Q2's geometry: p.19's straight line, strictly between.
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheScreenHoldsOnEverySixAxisWithTheBombStrictlyBetween(
            [ValueSource(nameof(EveryAxis))] HexDirection axis,
            [Values(2, 3)] int distance)
        {
            for (var gap = 1; gap < distance; gap++)
            {
                var targetAt = Step(axis, distance);
                var board = Board(enemyAllyAt: targetAt);
                Place(board, Step(axis, gap));

                Assert.That(ScreenedAttack(board, Enemy, RuleObject.ForHero(EnemyAlly)), Is.True,
                    $"The bomb stands {gap} space(s) along {axis} and the unit {distance}: p.19's " +
                    "straight line puts it between them.");
            }
        }

        [Test]
        public void AnAdjacentPairIsNeverScreened(
            [ValueSource(nameof(EveryAxis))] HexDirection axis)
        {
            var targetAt = Step(axis, 1);
            var board = Board(enemyAllyAt: targetAt);

            // The only spaces on this file are beyond one end or the other, and neither is between.
            foreach (var bombAt in new[] { Step(axis, 2), Step(Opposite(axis), 1) })
            {
                board.PlacedTokens.ToList().ForEach(t => board.CommitValidatedTokenReclamation(t.Id));
                Place(board, bombAt);
                Assert.That(ScreenedAttack(board, Enemy, RuleObject.ForHero(EnemyAlly)), Is.False,
                    "p.19's line puts nothing between two adjacent spaces, whichever side the token " +
                    "stands on.");
            }
        }

        [Test]
        public void ABombOffTheFileNeverScreens()
        {
            var board = Board(enemyAllyAt: Step(HexDirection.East, 3));
            Place(board, OffEveryFile);

            Assert.That(ScreenedAttack(board, Enemy, RuleObject.ForHero(EnemyAlly)), Is.False,
                "p.19's straight line is a file, and (1,1) is on none of the six running out of the " +
                "attacker's space.");
        }

        [Test]
        public void TheScreenIsReDerivedFromTheLiveBoardEveryTimeItIsAsked()
        {
            var board = Board(enemyAllyAt: Step(HexDirection.East, 3));
            var token = Place(board, Step(HexDirection.East, 1));

            Assert.That(ScreenedAttack(board, Enemy, RuleObject.ForHero(EnemyAlly)), Is.True);

            board.CommitValidatedDisplacement(
                BoardObjectReference.ForToken(token.Id), OffEveryFile);

            Assert.That(ScreenedAttack(board, Enemy, RuleObject.ForHero(EnemyAlly)), Is.False,
                "One step off the file and the smoke stops screening - nothing here is stored, so " +
                "the answer follows the piece.");
        }

        // -------------------------------------------------------------------------------------
        // p.19's Target, at each of the four consumers the page's definition reaches.
        // -------------------------------------------------------------------------------------

        [Test]
        public void AnEnemyHeroesAttackFindsNeitherAScreenedHeroNorAScreenedMinion()
        {
            var board = Board(enemyAllyAt: Step(HexDirection.East, 3), minionAt: Step(HexDirection.West, 2));
            var open = AttackTargetQuery.FindLegalEnemyTargets(
                Enemy, AttackRangeKind.Ranged, 4, Participants, board, Set(board));
            Assert.That(open.Select(target => target.Kind),
                Is.EquivalentTo(new[] { AttackTargetKind.Hero, AttackTargetKind.Minion }),
                "The precondition: with no smoke out, the Ranged Attack reaches Min's minion and " +
                "nobody screens the ally.");

            Place(board, Step(HexDirection.West, 1));
            var screened = AttackTargetQuery.FindLegalEnemyTargets(
                Enemy, AttackRangeKind.Ranged, 4, Participants, board, Set(board));

            Assert.That(screened.Any(target => target.Kind == AttackTargetKind.Minion), Is.False,
                "\"you or another unit\" is p.19's units, and a minion is one.");
            Assert.That(screened.Any(target => target.Kind == AttackTargetKind.Hero), Is.True,
                "The hero stands on a different file, so nothing is between him and the attacker.");
        }

        [Test]
        public void AnEnemyHeroesCardTextUnitChoiceDropsTheScreenedUnit()
        {
            var board = Board(enemyAllyAt: Step(HexDirection.East, 3), minionAt: Step(HexDirection.West, 2));
            var handler = Handler(SyntheticRosterCapabilities.ChooseTargetV2);
            var inputs = new Dictionary<string, EffectValue>
            {
                { SyntheticRosterCapabilities.OriginParameter, EffectValue.Integer((int)RosterQueryOrigin.Actor) },
                { SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(4) },
                { SyntheticRosterCapabilities.UnitKindParameter, EffectValue.Integer((int)RosterUnitKind.Unit) },
                { SyntheticRosterCapabilities.RelationParameter, EffectValue.Integer((int)SourceTeamRelation.AnyTeam) },
                { "activity", EffectValue.Integer((int)RuleActivity.Skill) }
            };

            var open = handler.Execute(Context(board, actor: Enemy, inputs: inputs));
            Assert.That(open.Choice.Candidates.Any(candidate => candidate.Kind == EffectValueKind.Minion), Is.True,
                "The precondition: the selector offers Min's minion while no smoke stands.");

            Place(board, Step(HexDirection.West, 1));
            var screened = handler.Execute(Context(board, actor: Enemy, inputs: inputs));

            Assert.That(screened.Choice.Candidates.Any(candidate => candidate.Kind == EffectValueKind.Minion), Is.False,
                "p.19: \"Whenever your action has any effect on another unit in any way, you target " +
                "that unit\" - so a card-text choice is a targeting, and the screen reaches it " +
                "through the one query the selector already asks.");
            Assert.That(screened.Choice.Candidates.Any(candidate => candidate.Kind == EffectValueKind.Hero), Is.True);
        }

        [Test]
        public void AnEnemyHeroCannotPushAUnitItCannotTarget()
        {
            var board = Board(minionAt: Step(HexDirection.West, 2));
            var handler = Handler(SyntheticDisplacementCapabilities.PushTargetV3);
            var inputs = new Dictionary<string, EffectValue>
            {
                { "target", EffectValue.Space(Step(HexDirection.West, 2)) },
                { "distance", EffectValue.Integer(1) },
                { SyntheticDisplacementCapabilities.LeastDistanceParameterName, EffectValue.Integer(0) },
                { "ignoresObstacles", EffectValue.Boolean(false) },
                { SyntheticExchangeCapabilities.EligiblePieceParameterName,
                    EffectValue.Integer((int)PushEligiblePieceKind.Unit) }
            };

            Assert.That(handler.Execute(Context(board, actor: Enemy, inputs: inputs)).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "The precondition: p.18's Push works on this minion while no smoke stands.");

            var pushed = Board(minionAt: Step(HexDirection.West, 2));
            Place(pushed, Step(HexDirection.West, 1));

            Assert.That(handler.Execute(Context(pushed, actor: Enemy, inputs: inputs)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "p.18's Push moves the unit, so it has an effect on it, so p.19 says it targets it.");
        }

        [Test]
        public void AnEnemyHeroCannotSwapWithAUnitItCannotTarget()
        {
            var handler = Handler(SyntheticExchangeCapabilities.SwapWithActorV1);
            var inputs = new Dictionary<string, EffectValue>
            {
                { "subject", EffectValue.Space(Step(HexDirection.West, 2)) },
                { SyntheticExchangeCapabilities.EligiblePieceParameterName,
                    EffectValue.Integer((int)SwapEligiblePieceKind.GamePiece) }
            };

            var open = Board(minionAt: Step(HexDirection.West, 2));
            Assert.That(handler.Execute(Context(open, actor: Enemy, inputs: inputs)).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "The precondition: p.19's Swap exchanges the two while no smoke stands.");

            var screened = Board(minionAt: Step(HexDirection.West, 2));
            Place(screened, Step(HexDirection.West, 1));

            Assert.That(handler.Execute(Context(screened, actor: Enemy, inputs: inputs)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A Swap moves the other unit, which p.19 counts as targeting it.");
            Assert.That(screened.Minions.Single().Coordinate, Is.EqualTo(Step(HexDirection.West, 2)),
                "A refused Swap moves nothing.");
        }

        // -------------------------------------------------------------------------------------
        // What the sentence does NOT reach.
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheControllerTargetsThroughHerOwnSmokeFreely()
        {
            var board = Board(minionAt: Step(HexDirection.West, 2), minsSpace: Origin, enemyAt: MinsCorner);
            Place(board, Step(HexDirection.West, 1));

            Assert.That(
                TargetQuery.IsPrevented(
                    RuleObject.ForMinion(RedMinion), RuleActivity.Attack, RuleObject.ForHero(Min),
                    board, Participants, Set(board)),
                Is.False,
                "The sentence says ENEMY heroes; the side is measured against the hero who placed it.");
        }

        [Test]
        public void AHeroOnTheControllersOwnSideIsNeverScreened_AndAnEnemyOnTheSameBoardIs()
        {
            // The bomb is the BLUE hero's here, so the Blue ally is friendly to it and Min is not.
            // Both would-be attackers stand on the file the bomb sits on: the Blue hero at its centre
            // and Min one space the other way, so the only thing that differs is whose side the bomb is.
            var board = Board(
                enemyAllyAt: Step(HexDirection.East, 3), minsSpace: Step(HexDirection.West, 1));
            Place(board, Step(HexDirection.East, 1), controller: Enemy);

            Assert.That(
                TargetQuery.IsPrevented(
                    RuleObject.ForHero(EnemyAlly), RuleActivity.Attack, RuleObject.ForHero(Enemy),
                    board, Participants, Set(board)),
                Is.False,
                "\"Enemy heroes\" is read from the token's controller, so its own team targets " +
                "through it.");
            Assert.That(
                TargetQuery.IsPrevented(
                    RuleObject.ForHero(EnemyAlly), RuleActivity.Attack, RuleObject.ForHero(Min),
                    board, Participants, Set(board)),
                Is.True,
                "Min is the enemy of this bomb's controller, so the same smoke stops her.");
        }

        [Test]
        public void APresenceQuestionIsUntouchedByAnySmoke()
        {
            var board = Board(minionAt: Step(HexDirection.West, 2));
            Place(board, Step(HexDirection.West, 1));

            Assert.That(
                UnitPresenceQuery.IsPresentWithin(
                    Origin, 3, EffectiveUnitKind.Minion, board, Set(board), RuleObject.None),
                Is.True,
                "p.19 separates presence from targeting - \"if you are adjacent to a minion\" has no " +
                "effect on the minion at all - and the presence half of the pair consults no " +
                "modification, so the screen cannot reach it.");
        }

        [Test]
        public void TheTokenItselfStaysTargetableAndASpaceIsNotATargetAtAll()
        {
            var board = Board(enemyAllyAt: Step(HexDirection.East, 3));
            var behind = Place(board, Step(HexDirection.East, 2));
            var between = Place(board, Step(HexDirection.East, 1), controller: Enemy,
                definition: SecondBombDefinition, instanceId: "enemy-bomb");

            Assert.That(
                TargetQuery.IsPrevented(
                    RuleObject.ForToken(behind.Id), RuleActivity.Clear, RuleObject.ForHero(Enemy),
                    board, Participants, Set(board)),
                Is.False,
                "p.19 says tokens are not units, so \"you or another unit\" does not name one - " +
                "which is why the token a screen stands in front of is still removable.");

            var reachable = TargetQuery.FindTargetsWithin(
                Origin, 3, false, RuleObject.ForHero(Enemy), RuleActivity.Attack, board, Participants,
                Set(board));
            Assert.That(reachable.Where(found => found.Kind == BoardObjectKind.Token).Select(t => t.TokenInstanceId),
                Is.EquivalentTo(new[] { behind.Id, between.Id }),
                "Both tokens are still in the disk the query returns: a token reaches the screen as " +
                "the named absence rather than as a unit standing behind smoke.");
        }

        [Test]
        public void NoScreenReachesAnActingMinionOrASystemLevelResolutionThatNamesNoActor()
        {
            var board = Board(enemyAllyAt: Step(HexDirection.East, 3), minionAt: Step(HexDirection.East, 2));
            Place(board, Step(HexDirection.East, 1));

            Assert.That(
                TargetQuery.IsPrevented(
                    RuleObject.ForHero(EnemyAlly), RuleActivity.Attack, RuleObject.ForMinion(RedMinion),
                    board, Participants, Set(board)),
                Is.False,
                "The sentence names enemy HEROES; a minion performs no action this rule speaks about.");
            Assert.That(
                TargetQuery.IsPrevented(
                    RuleObject.ForHero(EnemyAlly), RuleActivity.All, RuleObject.None,
                    board, Participants, Set(board)),
                Is.False,
                "p.8's Minion Battle is nobody's action - it names no acting hero at all - so its " +
                "removals pass this question untouched, which is the same boundary RULING 26 draws " +
                "for Tali's immune totem.");
        }

        // -------------------------------------------------------------------------------------
        // The sub-graphs the later blocks compose on.
        // -------------------------------------------------------------------------------------

        [TestCase(PrintedValueChannel.Radius)]
        [TestCase(PrintedValueChannel.Range)]
        public void TheSwapSubGraphExchangesMinWithHerBombWhenItIsWithinTheCardsOwnIcon(
            PrintedValueChannel channel)
        {
            var nodes = channel == PrintedValueChannel.Radius
                ? MinEffectContent.SwapWithSmokeBombInRadius("swap")
                : MinEffectContent.SwapWithSmokeBombInRange("swap");
            var effect = new EffectDefinition(
                new EffectDefinitionId("min-swap-probe"), MinComponentContent.PrepRevision,
                MinEffectContent.SubGraphRootNodeId("swap"), nodes);
            Assert.That(EffectDefinitionValidator.Validate(effect, Capabilities), Is.Empty);

            var inReach = Board(minsSpace: Origin, enemyAt: MinsCorner);
            Place(inReach, Step(HexDirection.East, 3));
            var step = Run(effect, inReach, channel);
            Assert.That(step.IsComplete, Is.True);
            Assert.That(inReach.PlacedTokens.Single().Coordinate, Is.EqualTo(Origin));
            Assert.That(HeroSpace(inReach, Min), Is.EqualTo(Step(HexDirection.East, 3)),
                "p.19's Swap exchanges the two, and \"does not count as movement or placement\".");

            var outOfReach = Board(minsSpace: Origin, enemyAt: MinsCorner);
            Place(outOfReach, Step(HexDirection.East, 4));
            Run(effect, outOfReach, channel);
            Assert.That(HeroSpace(outOfReach, Min), Is.EqualTo(Origin),
                "A bomb beyond the card's printed icon publishes no space at all, so the pair has " +
                "nothing to swap with - p.10's impossible step, which applies nothing rather than " +
                "reaching for a piece the card cannot see.");
            Assert.That(outOfReach.PlacedTokens.Single().Coordinate,
                Is.EqualTo(Step(HexDirection.East, 4)),
                "And the bomb stays where it is: p.9's icon is a ceiling, not a suggestion.");
        }

        [TestCase(PrintedValueChannel.Radius)]
        [TestCase(PrintedValueChannel.Range)]
        public void TheRePlaceSubGraphPicksUpTheStandingBombAndPutsItDownWithinReachOfMin(
            PrintedValueChannel channel)
        {
            var nodes = channel == PrintedValueChannel.Radius
                ? MinEffectContent.PlaceSmokeBombInRadius("again")
                : MinEffectContent.PlaceSmokeBombInRange("again");
            var effect = new EffectDefinition(
                new EffectDefinitionId("min-replace-probe"), MinComponentContent.PrepRevision,
                MinEffectContent.SubGraphRootNodeId("again"), nodes);
            Assert.That(EffectDefinitionValidator.Validate(effect, Capabilities), Is.Empty);

            var board = Board(minsSpace: Origin, enemyAt: MinsCorner);
            var standing = Place(board, Step(HexDirection.East, 4));
            var step = Run(effect, board, channel);

            Assert.That(step.Frame.PendingChoice, Is.Not.Null,
                "The bomb's own position is read UNBOUNDED here: the clause places \"the Smoke " +
                "bomb\" - the piece just swapped with - and the printed icon bounds the DESTINATION.");
            var offered = step.Frame.PendingChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate).ToArray();
            Assert.That(offered, Has.All.Matches<HexCoordinate>(space =>
                    HexDistanceQuery.Calculate(Origin, space) <= 3),
                "Measured from Min, at the printed 3.");
            Assert.That(offered, Has.Member(Step(HexDirection.West, 1)));

            Answer(step, effect, board, channel, EffectValue.Space(Step(HexDirection.West, 1)));
            Assert.That(board.PlacedTokens.Single().Id, Is.EqualTo(standing.Id),
                "The same piece, moved - which is what one physical bomb can mean.");
        }

        // -------------------------------------------------------------------------------------
        // Snapshot, restore, replay - and the deferral, from both sides.
        // -------------------------------------------------------------------------------------

        [Test]
        public void AScreenedAttackOfferSurvivesSnapshotRestoreAndReplay()
        {
            var board = Board(enemyAllyAt: Step(HexDirection.East, 3));
            Place(board, Step(HexDirection.East, 1));

            var definitions = new[] { MinCardContent.SmokeBombDefinition() };
            var cards = new[]
            {
                CardInstance.CreateInHand(new CardInstanceId("min-card"), definitions[0].Reference, Min),
                CardInstance.CreateInHand(new CardInstanceId("enemy-card"), definitions[0].Reference, Enemy),
                CardInstance.CreateInHand(new CardInstanceId("ally-card"), definitions[0].Reference, EnemyAlly)
            };
            var catalogs = new MatchContentCatalogs(Capabilities, Components);
            var round = new RoundResolutionAuthority(
                definitions, cards, Participants, new TieBreakerState(TeamSide.Red), 1,
                RoundResolutionComposition.ForCardActionsOnly(
                    new CardActionResolutionAuthority(catalogs, board)));

            Assert.That(ScreenedAttack(board, Enemy, RuleObject.ForHero(EnemyAlly)), Is.True,
                "The precondition this whole test is about.");

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                catalogs, payload, definitions, new[] { board.MapDefinition },
                Array.Empty<QuickGameMinionSetupState>());

            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload),
                "A restore that re-serializes differently has changed the match it claims to be.");
            Assert.That(restored.Board.PlacedTokens.Single().Definition,
                Is.SameAs(MinComponentContent.SmokeBombDefinition),
                "The restored instance is bound to the catalog's own definition object, so its " +
                "properties are the trusted content rather than anything the payload carried.");
            Assert.That(ScreenedAttack(restored.Board, Enemy, RuleObject.ForHero(EnemyAlly)), Is.True,
                "Nothing about the screen is serialized: the property is re-derived from the " +
                "definition the restored token rebinds to, which is why this needed no schema change.");

            restored.Board.CommitValidatedDisplacement(
                BoardObjectReference.ForToken(restored.Board.PlacedTokens.Single().Id),
                OffEveryFile);
            Assert.That(ScreenedAttack(restored.Board, Enemy, RuleObject.ForHero(EnemyAlly)), Is.False,
                "And the restored match goes on answering from the live board, not from a copy taken " +
                "when it was written.");
        }

        [Test]
        public void NothingOfTheSmokeBombIsRegisteredInThisBuild()
        {
            var build = MatchContentCatalogs.ForThisBuild();

            Assert.That(MinComponentContent.SmokeBombRegisteredHandlers(), Is.Empty,
                "This side of the gate: the content file claims no registration.");
            foreach (var handler in MinComponentContent.SmokeBombDeferredHandlers())
            {
                Assert.That(build.Capabilities.Registered, Has.No.Member(handler.Reference),
                    $"The other side: '{handler.Reference}' is not in the build's capability catalog, " +
                    "so no configured match can start it. 8.61j is the block that flips this.");
            }

            Assert.That(
                () => build.Components.LoadToken(MinComponentContent.SmokeBombDefinition.Reference),
                Throws.InstanceOf<ComponentDefinitionLoadException>(),
                "And the component catalog does not hold the token, so a snapshot naming it could " +
                "not be restored by a configured match either.");
        }

        // =====================================================================================
        // Fixture support.
        // =====================================================================================

        private static MatchParticipants Participants => StagedBoardParticipants.CreateWithBlueAlly();

        private static EffectCapabilityCatalog Capabilities
        {
            get
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                return new EffectCapabilityCatalog(defaults.Capabilities.Registered
                    .Select(defaults.Capabilities.Load)
                    .Concat(MinComponentContent.SmokeBombDeferredHandlers()));
            }
        }

        private static ComponentDefinitionCatalog Components =>
            new ComponentDefinitionCatalog(
                MinComponentContent.SmokeBombTokens(), Array.Empty<MarkerDefinition>());

        /// <summary>A second screening definition, for the one case that needs two bombs at once.</summary>
        private static readonly TokenDefinition SecondBombDefinition = new TokenDefinition(
            new TokenDefinitionId("fixture-second-smoke-bomb"), 1, "Smoke bomb", 1,
            MinComponentContent.SmokeBombDefinition.Properties,
            TokenRemovalCondition.EndOfRoundCleanup);

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            Capabilities.Load(reference);

        private static HexCoordinate Step(HexDirection direction, int distance) =>
            StraightLineQuery.Step(Origin, direction, distance);

        private static HexDirection Opposite(HexDirection direction) =>
            EveryAxis[(Array.IndexOf(EveryAxis, direction) + 3) % EveryAxis.Length];

        private static RuleModificationSet Set(AdjacentMoveAuthority board) =>
            RuleModificationQuery.Build(
                null, board, Participants, TieBreakerState.Unrevealed, UnlockedUltimateSet.None,
                RuleObject.None, null, null);

        private static bool ScreenedAttack(
            AdjacentMoveAuthority board, HeroId actor, RuleObject target) =>
            TargetQuery.IsPrevented(
                target, RuleActivity.Attack, RuleObject.ForHero(actor), board, Participants, Set(board));

        private static HeroId HeroAt(AdjacentMoveAuthority board, HexCoordinate coordinate) =>
            board.Occupancies.Single(value => value.Coordinate.Equals(coordinate)).HeroId;

        private static HexCoordinate HeroSpace(AdjacentMoveAuthority board, HeroId hero) =>
            board.Occupancies.Single(value => value.HeroId.Equals(hero)).Coordinate;

        private static PlacedToken Place(
            AdjacentMoveAuthority board,
            HexCoordinate coordinate,
            HeroId controller = default,
            TokenDefinition definition = null,
            string instanceId = "min-smoke-bomb-instance")
        {
            var owner = controller.IsDefined ? controller : Min;
            var token = new PlacedToken(
                new TokenInstanceId(instanceId),
                definition ?? MinComponentContent.SmokeBombDefinition,
                owner,
                owner,
                new CardInstanceId("min-smoke-bomb-source"),
                coordinate);
            board.CommitValidatedTokenPlacement(token);
            return token;
        }

        private static BoardMapDefinition Map(IEnumerable<HexCoordinate> spaces)
        {
            var zone = new BoardMapZone(
                new ZoneId("min-smoke-zone"), "Min Smoke Zone", "synthetic", null, false, false);
            var playable = spaces
                .Select(space => new BoardMapSpace(
                    SpaceIdOf(space), space, BoardMapSpaceKind.Playable, zone.Id))
                .ToArray();
            var spawnPoints = playable.Any(space => space.Coordinate.Equals(SpawnSpace))
                ? new[]
                {
                    new BoardMapSpawnPoint(
                        Spawn, SpaceIdOf(SpawnSpace), StagedBoardParticipants.RedTeamIdValue, "melee")
                }
                : Array.Empty<BoardMapSpawnPoint>();
            return new BoardMapDefinition(
                MapIdentity, 1, "Min smoke map", playable, new[] { zone }, spawnPoints,
                Array.Empty<ZoneId>());
        }

        private static SpaceId SpaceIdOf(HexCoordinate coordinate) =>
            new SpaceId($"q{coordinate.Q}_r{coordinate.R}");

        /// <summary>
        /// The radius-4 disc this fixture measures on. The acting enemy hero stands at its centre so
        /// every file it asks about runs out of one space; Min stands off every one of them.
        /// </summary>
        private static AdjacentMoveAuthority Board(
            HexCoordinate? enemyAllyAt = null,
            HexCoordinate? minionAt = null,
            HexCoordinate? minsSpace = null,
            HexCoordinate? enemyAt = null,
            bool minIsOffTheBoard = false)
        {
            var spaces = new List<HexCoordinate>();
            for (var q = -4; q <= 4; q++)
            {
                for (var r = -4; r <= 4; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    if (HexDistanceQuery.Calculate(Origin, coordinate) <= 4)
                    {
                        spaces.Add(coordinate);
                    }
                }
            }

            var heroes = new List<HeroOccupancy>
            {
                new HeroOccupancy(Enemy, enemyAt ?? Origin)
            };
            if (!minIsOffTheBoard)
            {
                heroes.Add(new HeroOccupancy(Min, minsSpace ?? MinsCorner));
            }

            if (enemyAllyAt.HasValue)
            {
                heroes.Add(new HeroOccupancy(EnemyAlly, enemyAllyAt.Value));
            }

            var minions = minionAt.HasValue
                ? new[]
                {
                    new QuickGameMinionSetupState(
                        RedMinion,
                        new TeamId(StagedBoardParticipants.RedTeamIdValue),
                        TeamSide.Red,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(MapIdentity, Spawn),
                        new ConfiguredSpaceReference(MapIdentity, SpaceIdOf(minionAt.Value)),
                        minionAt.Value)
                }
                : Array.Empty<QuickGameMinionSetupState>();

            return new AdjacentMoveAuthority(
                Map(spaces), heroes, Array.Empty<ObstacleOccupancy>(), minions);
        }

        private static HeroItemState RadiusItem() => HeroItemState.Restore(
            Participants,
            new HeroItemRestoreState(new[]
            {
                new HeroItem(
                    Min, new CardInstanceId("min-radius-item"),
                    HeroDashboard.RulebookLayout.Slots[3].Position, PrintedValueChannel.Radius, 1)
            }));

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            HeroId actor = default,
            IReadOnlyDictionary<string, EffectValue> inputs = null,
            HeroItemState items = null)
        {
            var definition = MinCardContent.SmokeBombDefinition();
            var acting = actor.IsDefined ? actor : Min;
            return new EffectCapabilityContext(
                new EffectNodeId("min-smoke-bomb-node"),
                EffectFailurePolicy.Mandatory,
                acting,
                acting,
                new CardInstanceId("min-smoke-bomb-source"),
                definition.Reference,
                MinCardContent.PrimaryBasicSkill,
                inputs ?? new Dictionary<string, EffectValue>(),
                1,
                1,
                Participants,
                null,
                Set(board),
                EffectCardState.Of(new[] { definition }, Array.Empty<CardInstance>()),
                ActionKind.Skill,
                new MatchComponents(Components, board, new MarkerAttachmentRegister(), board, board, board, board),
                heroItems: items);
        }

        private static EffectResolutionContext ResolutionContext(
            AdjacentMoveAuthority board, PrintedValueChannel? channel = null)
        {
            var definition = channel == PrintedValueChannel.Range
                ? RangeProbeCard
                : MinCardContent.SmokeBombDefinition();
            return new EffectResolutionContext(
                null, Capabilities, Participants, null, 1, 1, Set(board),
                EffectCardState.Of(new[] { definition }, Array.Empty<CardInstance>()),
                ActionKind.Skill,
                components: new MatchComponents(
                    Components, board, new MarkerAttachmentRegister(), board, board, board, board));
        }

        /// <summary>
        /// A fixture face printing a Range 3 icon where her Silver prints a Radius 3 one, so the
        /// Range-reaching twins of both sub-graphs can be measured against the same distances.
        /// </summary>
        private static CardDefinition RangeProbeCard { get; } = new CardDefinition(
            new CardDefinitionId("min-range-probe"), MinComponentContent.PrepRevision,
            "TEST - Min printed Range", new InitiativeValue(12),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    MinCardContent.PrimaryBasicSkill, ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    null,
                    // The probe exists to print a Range icon the sub-graphs can read; the body under
                    // test is supplied by the fixture, not by this face.
                    PrintedTextComposition.DeferredUntil("the Min fixture's own composed sub-graph"))
            });

        private static EffectResolutionStepResult Run(
            EffectDefinition effect, AdjacentMoveAuthority board, PrintedValueChannel channel)
        {
            var definition = channel == PrintedValueChannel.Range
                ? RangeProbeCard
                : MinCardContent.SmokeBombDefinition();
            var context = new EffectResolutionContext(
                effect, Capabilities, Participants, null, 1, 1, Set(board),
                EffectCardState.Of(new[] { definition }, Array.Empty<CardInstance>()),
                ActionKind.Skill,
                components: new MatchComponents(
                    Components, board, new MarkerAttachmentRegister(), board, board, board, board));
            return EffectResolutionSession.Start(
                new EffectResolutionId("min-smoke-bomb-probe"),
                new CardInstanceId("min-smoke-bomb-source"), definition.Reference,
                MinCardContent.PrimaryBasicSkill, Min, Min, ActionInvocationContext.PrintedPrimary,
                context);
        }

        private static void Answer(
            EffectResolutionStepResult step, EffectDefinition effect, AdjacentMoveAuthority board,
            PrintedValueChannel channel, EffectValue value)
        {
            var definition = channel == PrintedValueChannel.Range
                ? RangeProbeCard
                : MinCardContent.SmokeBombDefinition();
            var context = new EffectResolutionContext(
                effect, Capabilities, Participants, null, 1, 1, Set(board),
                EffectCardState.Of(new[] { definition }, Array.Empty<CardInstance>()),
                ActionKind.Skill,
                components: new MatchComponents(
                    Components, board, new MarkerAttachmentRegister(), board, board, board, board));
            var choice = step.Frame.PendingChoice;
            var command = choice.SelectInvocations.Single(
                invocation => invocation.Candidate.Value.Equals(value));
            var answered = EffectResolutionSession.Answer(step.Frame, command, context);
            Assert.That(answered.Accepted, Is.True, answered.RejectionReason.ToString());
            var pending = answered.Frame?.PendingChoice;
            if (pending != null && pending.ChoiceId.Equals(choice.ChoiceId))
            {
                Assert.That(
                    EffectResolutionSession.Answer(answered.Frame, pending.ConfirmInvocation, context)
                        .Accepted,
                    Is.True);
            }
        }

        /// <summary>Her Silver basic, driven whole, so the branch is measured rather than read.</summary>
        private sealed class SkillProbe
        {
            private readonly EffectResolutionContext context;

            internal SkillProbe(AdjacentMoveAuthority board)
            {
                var definition = MinCardContent.SmokeBombDefinition();
                var effect = MinEffectContent.SmokeBomb(
                    MinCardContent.SmokeBomb, MinComponentContent.PrepRevision);
                Assert.That(EffectDefinitionValidator.Validate(effect, Capabilities), Is.Empty);
                context = new EffectResolutionContext(
                    effect, Capabilities, Participants, null, 1, 1, Set(board),
                    EffectCardState.Of(new[] { definition }, Array.Empty<CardInstance>()),
                    ActionKind.Skill,
                    components: new MatchComponents(
                        Components, board, new MarkerAttachmentRegister(), board, board, board, board));
                Step = EffectResolutionSession.Start(
                    new EffectResolutionId("min-smoke-bomb-skill"),
                    new CardInstanceId("min-smoke-bomb-source"), definition.Reference,
                    MinCardContent.PrimaryBasicSkill, Min, Min, ActionInvocationContext.PrintedPrimary,
                    context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
            }

            internal EffectResolutionStepResult Step { get; private set; }

            internal PendingEffectChoice Choice => Step.Frame?.PendingChoice;

            internal void Pick(EffectValue value)
            {
                var id = Choice.ChoiceId;
                Answer(Choice.SelectInvocations.Single(
                    invocation => invocation.Candidate.Value.Equals(value)));
                if (Choice != null && Choice.ChoiceId.Equals(id))
                {
                    Answer(Choice.ConfirmInvocation);
                }
            }

            private void Answer(EffectChoiceCommand command)
            {
                Step = EffectResolutionSession.Answer(Step.Frame, command, context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
            }
        }
    }
}
