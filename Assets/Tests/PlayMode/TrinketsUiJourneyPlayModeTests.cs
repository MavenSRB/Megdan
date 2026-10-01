using System;
using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Real pointer/control journeys for the one persistent Turret obstacle.</summary>
    public sealed class TrinketsUiJourneyPlayModeTests
    {
        private static readonly HexCoordinate ActorSpace = new HexCoordinate(-3, 1);
        private static readonly HexCoordinate EnemySpace = new HexCoordinate(-2, 1);
        private static readonly HexCoordinate InitialTurretSpace = new HexCoordinate(-3, 0);

        internal static HeroId Trinkets(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "trinkets").HeroId;

        internal static IEnumerator Stage(CardDefinitionId face, bool withTurret = false,
            HexCoordinate? turretSpace = null, CardDefinitionId[] enemyHand = null,
            CardDefinitionId[] ownHand = null, int? coins = null, int? level = null,
            HexCoordinate? enemyMinionSpace = null)
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTrinkets);
            var runtime = Root().Runtime;
            var actor = Trinkets(runtime);
            var enemy = OtherHeroId(runtime, actor);
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(actor, ActorSpace, level: level, coins: coins,
                        hand: ownHand ?? new[]
                    {
                        face, TrinketsCardContent.RapidRedeployment,
                        TrinketsCardContent.SalvageParts,
                        TrinketsCardContent.MakeshiftMinigun,
                        TrinketsCardContent.DisruptorJolt,
                        TrinketsCardContent.EarlyPrototype
                    }.Distinct().Take(5)),
                    new SandboxHeroStaging(enemy, EnemySpace, hand: enemyHand ?? new[]
                    {
                        WaspCardContent.Shock, WaspCardContent.Electrocute
                    })
                },
                spawnedMinions: enemyMinionSpace.HasValue
                    ? new[] { new SandboxMinionStaging(
                        new MinionId("trinkets-repeat-target"), TeamSide.Red,
                        StandardMinionType.Melee, enemyMinionSpace.Value) }
                    : null,
                removedMinions: enemyMinionSpace.HasValue
                    ? runtime.ReadModel.Minions.Where(minion =>
                        minion.Coordinate.Equals(enemyMinionSpace.Value))
                        .Select(minion => minion.MinionId)
                    : null);
            if (withTurret)
                MatchSceneCompositionRoot.NextComposedStagedObstacles = new[]
                {
                    new ObstacleOccupancy(
                        OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, actor),
                        turretSpace ?? InitialTurretSpace)
                };
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTrinkets);
        }

        internal static IEnumerator Begin(MatchSceneCompositionRoot root, CardDefinitionId face,
            ActionKind action)
        {
            var actor = Trinkets(root.Runtime);
            yield return CommitEverySeat(root, actor, NameOf(root.Runtime, face));
            yield return WaitForActor(root, actor);
            yield return PointTrayAt(root, actor);
            yield return StartActionThroughControl(root, action);
            Healthy(root.Runtime);
        }

        [UnityTest]
        public IEnumerator RapidRedeploymentPlacesAndRepositionsOnlyOnePersistentTurret()
        {
            foreach (var preplaced in new[] { false, true })
            {
                yield return Stage(TrinketsCardContent.RapidRedeployment, preplaced);
                var root = Root();
                var runtime = root.Runtime;
                var actor = Trinkets(runtime);
                var turretId = OwnedObstacleIdentity.For(
                    TrinketsComponentContent.TurretKind, actor);
                Assert.That(runtime.Projection.Obstacles.Count(obstacle =>
                    obstacle.ObstacleId.Equals(turretId)), Is.EqualTo(preplaced ? 1 : 0));

                yield return Begin(root, TrinketsCardContent.RapidRedeployment,
                    ActionKind.Skill);
                UiPlayer.Click(UiPlayer.Button(root.Hud,
                    BulletLabelFor(runtime, "move-place")));
                yield return null;
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
                yield return Space(root);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    "The first choice moves Trinkets; the second places the Turret.");
                var offered = runtime.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind ==
                        GuardsDigital.Core.Effects.EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate)
                    .Where(space => !preplaced || !space.Equals(InitialTurretSpace))
                    .ToArray();
                Assert.That(offered, Is.Not.Empty);
                var chosen = FirstClickableSpace(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    offered);
                yield return Space(root, chosen);

                var projected = runtime.Projection.Obstacles.Where(obstacle =>
                    obstacle.ObstacleId.Equals(turretId)).ToArray();
                Assert.That(projected, Has.Length.EqualTo(1));
                Assert.That(projected[0].Coordinate, Is.EqualTo(chosen));
                Assert.That(runtime.Projection.PlacedTokens.Any(token =>
                    token.Definition.Id.Equals(
                        TrinketsComponentContent.BarrierId)), Is.False,
                    "The Turret is an obstacle, not a Barrier token.");
                var visible = Object.FindObjectsByType<PlaceholderObstacleView>(FindObjectsSortMode.None)
                    .Where(view => view.gameObject.activeSelf &&
                        view.Occupancy.ObstacleId.Equals(turretId)).ToArray();
                Assert.That(visible, Has.Length.EqualTo(1));
                Assert.That(visible[0].Coordinate, Is.EqualTo(chosen));
            }
        }

        [UnityTest]
        public IEnumerator RapidRedeploymentDefeatsTheAdjacentMinionOnItsOtherBullet()
        {
            yield return Stage(TrinketsCardContent.RapidRedeployment,
                enemyMinionSpace: new HexCoordinate(-3, 2));
            var root = Root();
            var runtime = root.Runtime;
            var minion = new MinionId("trinkets-repeat-target");
            yield return Begin(root, TrinketsCardContent.RapidRedeployment,
                ActionKind.Skill);
            UiPlayer.Click(UiPlayer.Button(root.Hud,
                BulletLabelFor(runtime, "defeat-arm")));
            yield return null;
            var chosen = runtime.PendingEffectChoice.Candidates.Single(candidate =>
                candidate.Value.Kind == GuardsDigital.Core.Effects.EffectValueKind.Minion &&
                candidate.Value.MinionId.Equals(minion));
            yield return Pick(root, chosen);
            Assert.That(runtime.Projection.Minions.Any(value =>
                value.MinionId.Equals(minion)), Is.False);
            Assert.That(runtime.Projection.Obstacles, Is.Empty,
                "Defeating the minion does not also place a Turret.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator FiveRedFacesTakeOnlyTurretQualifiedTargetsThroughTheAttackControl()
        {
            var turretSpace = new HexCoordinate(-2, 0);
            foreach (var face in TrinketsCardContent.RedDefinitions.Select(card => card.Id))
            {
                yield return Stage(face, withTurret: true, turretSpace: turretSpace,
                    enemyHand: new[] { WaspCardContent.Shock });
                var root = Root();
                var runtime = root.Runtime;
                var actor = Trinkets(runtime);
                var enemy = OtherHeroId(runtime, actor);
                yield return Begin(root, face, ActionKind.Attack);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null, face.Value);
                Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo("trinkets-red-target"));
                var chosen = runtime.PendingEffectChoice.Candidates.Single(candidate =>
                    candidate.Value.Kind == GuardsDigital.Core.Effects.EffectValueKind.Hero &&
                    candidate.Value.HeroId.Equals(enemy));
                yield return Pick(root, chosen);
                Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                    face.Value + " must reach the defender's p.14 choice. " + Remaining(runtime));
                UiPlayer.Click(UiPlayer.Button(root.Hud, "TAKE THE HIT · NO DEFENSE"));
                yield return null;
                Assert.That(runtime.Projection.Heroes.Any(hero => hero.HeroId.Equals(enemy)),
                    Is.False, face.Value + " must finish its attack against the named Wasp hero. " +
                        Remaining(runtime));
                if (face.Equals(TrinketsCardContent.SteamDischarge) ||
                    face.Equals(TrinketsCardContent.FlameBelcher))
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                        "The printed different-unit repeat must be offered after the Attack.");
                    yield return May(root, false);
                }
                Assert.That(runtime.Projection.Obstacles.Single(obstacle =>
                    obstacle.ObstacleId.Equals(OwnedObstacleIdentity.For(
                        TrinketsComponentContent.TurretKind, actor))).Coordinate,
                    Is.EqualTo(turretSpace), "A Red Attack cannot move the Turret.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator SteamAndFlameAcceptTheirPrintedRepeatAgainstADifferentEnemyUnit()
        {
            var turretSpace = new HexCoordinate(-2, 0);
            var minionSpace = new HexCoordinate(-2, -1);
            foreach (var face in new[]
            {
                TrinketsCardContent.SteamDischarge,
                TrinketsCardContent.FlameBelcher
            })
            {
                yield return Stage(face, withTurret: true, turretSpace: turretSpace,
                    enemyHand: new[] { WaspCardContent.Shock },
                    enemyMinionSpace: minionSpace);
                var root = Root();
                var runtime = root.Runtime;
                var actor = Trinkets(runtime);
                var enemy = OtherHeroId(runtime, actor);
                var minion = new MinionId("trinkets-repeat-target");
                yield return Begin(root, face, ActionKind.Attack);
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(candidate =>
                    candidate.Value.Kind == GuardsDigital.Core.Effects.EffectValueKind.Hero &&
                    candidate.Value.HeroId.Equals(enemy)));
                Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
                UiPlayer.Click(UiPlayer.Button(root.Hud, "TAKE THE HIT · NO DEFENSE"));
                yield return null;
                Assert.That(runtime.Projection.Heroes.Any(hero =>
                    hero.HeroId.Equals(enemy)), Is.False);
                yield return May(root, true);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " must ask for a different unit after accepting repeat.");
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(candidate =>
                    candidate.Value.Kind == GuardsDigital.Core.Effects.EffectValueKind.Minion &&
                    candidate.Value.MinionId.Equals(minion)));
                Assert.That(runtime.Projection.Minions.Any(value =>
                    value.MinionId.Equals(minion)), Is.False,
                    "The repeated Attack must resolve against the selected minion.");
                if (runtime.PendingEffectChoice != null) yield return May(root, false);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator ThreeDisruptorFacesActivateFromTheirPrintedSkillControl()
        {
            foreach (var face in TrinketsCardContent.DisruptorDefinitions.Select(card => card.Id))
            {
                yield return Stage(face, withTurret: true);
                var root = Root();
                var runtime = root.Runtime;
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Empty);
                yield return Begin(root, face, ActionKind.Skill);
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Has.Count.EqualTo(1),
                    face.Value + " must leave its this-turn disruption active. " + Remaining(runtime));
                var turretId = OwnedObstacleIdentity.For(
                    TrinketsComponentContent.TurretKind, Trinkets(runtime));
                Assert.That(runtime.Projection.Obstacles.Count(obstacle =>
                    obstacle.ObstacleId.Equals(turretId)),
                    Is.EqualTo(1), "Activating disruption does not move or remove the Turret.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator DisruptorTriggerPrecedesEnemyPrimaryAndUsesTheirRemainingHand()
        {
            foreach (var face in TrinketsCardContent.DisruptorDefinitions.Select(card => card.Id))
            {
                var defeatIfEmpty = face.Equals(TrinketsCardContent.DisruptorGrid);
                yield return Stage(face, withTurret: true,
                    enemyHand: defeatIfEmpty
                        ? new[] { WaspCardContent.Shock }
                        : new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var actor = Trinkets(runtime);
                var enemy = OtherHeroId(runtime, actor);
                yield return Begin(root, face, ActionKind.Skill);
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Has.Count.EqualTo(1));
                yield return YieldUntilTheNamedSeatActs(runtime, enemy);
                yield return PointTrayAt(root, enemy);
                var commandCount = runtime.AcceptedCommands.Count;
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(runtime.AcceptedCommands.Count, Is.GreaterThan(commandCount),
                    face.Value + " must accept the clicked enemy primary. " + Remaining(runtime) +
                    " detail=" + runtime.LastOperationDetail);
                if (defeatIfEmpty)
                {
                    Assert.That(runtime.Projection.Heroes.Any(hero =>
                        hero.HeroId.Equals(enemy)), Is.False,
                        "Grid defeats the empty-handed enemy before their primary Attack target. " +
                        Remaining(runtime) + " hand=" + string.Join(",", runtime.Cards
                            .Where(card => card.OwnerId.Equals(enemy) &&
                                card.State.Location == CardLocation.Hand)
                            .Select(card => card.DefinitionReference.DefinitionId.Value)) +
                        " active=" + runtime.ActiveEffectSourceCardInstanceIds.Count);
                }
                else
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                        face.Value + " must ask the enemy for a card before Attack targeting.");
                    var discard = runtime.PendingEffectChoice.Candidates.Single(candidate =>
                        candidate.Value.Kind ==
                            GuardsDigital.Core.Effects.EffectValueKind.CardInstance &&
                        runtime.Cards.Single(card => card.Id.Equals(
                            candidate.Value.CardInstanceId)).DefinitionReference.DefinitionId
                            .Equals(WaspCardContent.Electrocute));
                    yield return Pick(root, discard);
                    Assert.That(runtime.Cards.Single(card => card.Id.Equals(
                        discard.Value.CardInstanceId)).State.Location,
                        Is.EqualTo(CardLocation.DiscardPile));
                    Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Empty,
                        "A successful discard deactivates the one-shot disruption.");
                }
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator BothBarrierFacesPlaceTheirPrintedCountThroughMapChoices()
        {
            foreach (var face in TrinketsCardContent.BarrierDefinitions.Select(card => card.Id))
            {
                yield return Stage(face, withTurret: true);
                var root = Root();
                var runtime = root.Runtime;
                yield return Begin(root, face, ActionKind.Skill);
                yield return May(root, true);
                var maximum = face.Equals(TrinketsCardContent.DeployableBastion) ? 3 : 2;
                for (var index = 0; index < maximum; index++)
                {
                    if (index > 0) yield return May(root, true);
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                        face.Value + " must offer Barrier destination " + index + ". " + Remaining(runtime));
                    var offered = runtime.PendingEffectChoice.Candidates
                        .Where(candidate => candidate.Value.Kind ==
                            GuardsDigital.Core.Effects.EffectValueKind.Coordinate)
                        .Select(candidate => candidate.Value.Coordinate).ToArray();
                    var chosen = FirstClickableSpace(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), offered);
                    yield return Space(root, chosen);
                    Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                        token.Definition.Id.Equals(TrinketsComponentContent.BarrierId)),
                        Is.EqualTo(index + 1), "Every confirmed destination puts one physical Barrier on the map.");
                }
                var turretId = OwnedObstacleIdentity.For(
                    TrinketsComponentContent.TurretKind, Trinkets(runtime));
                Assert.That(runtime.Projection.Obstacles.Count(obstacle =>
                    obstacle.ObstacleId.Equals(turretId)),
                    Is.EqualTo(1));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator SalvagePartsCanPlaceOrRemoveTheTurretThroughItsNamedBullets()
        {
            foreach (var remove in new[] { false, true })
            {
                yield return Stage(TrinketsCardContent.SalvageParts, withTurret: true);
                var root = Root();
                var runtime = root.Runtime;
                var actor = Trinkets(runtime);
                var turretId = OwnedObstacleIdentity.For(
                    TrinketsComponentContent.TurretKind, actor);
                yield return Begin(root, TrinketsCardContent.SalvageParts, ActionKind.Skill);
                UiPlayer.Click(UiPlayer.Button(root.Hud,
                    BulletLabelFor(runtime, remove ? "move-arm" : "place-arm")));
                yield return null;
                if (remove)
                {
                    Assert.That(runtime.Projection.Obstacles.Any(obstacle =>
                        obstacle.ObstacleId.Equals(turretId)), Is.False,
                        "The remove-and-move bullet removes the one Turret first.");
                    yield return Space(root);
                    Assert.That(runtime.Projection.Obstacles.Any(obstacle =>
                        obstacle.ObstacleId.Equals(turretId)), Is.False);
                }
                else
                {
                    var offered = runtime.PendingEffectChoice.Candidates
                        .Where(candidate => candidate.Value.Kind ==
                            GuardsDigital.Core.Effects.EffectValueKind.Coordinate)
                        .Select(candidate => candidate.Value.Coordinate)
                        .Where(space => !space.Equals(InitialTurretSpace)).ToArray();
                    var chosen = FirstClickableSpace(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), offered);
                    yield return Space(root, chosen);
                    Assert.That(runtime.Projection.Obstacles.Single(obstacle =>
                        obstacle.ObstacleId.Equals(turretId)).Coordinate, Is.EqualTo(chosen));
                }
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator SalvagePartsRemovesTurretAndRetrievesTheDefenseCardJustSpent()
        {
            yield return Stage(TrinketsCardContent.SalvageParts, withTurret: true,
                enemyHand: new[] { WaspCardContent.MagneticDagger },
                ownHand: new[]
                {
                    TrinketsCardContent.SalvageParts,
                    TrinketsCardContent.DisruptorGrid
                });
            var root = Root();
            var runtime = root.Runtime;
            var actor = Trinkets(runtime);
            var enemy = OtherHeroId(runtime, actor);
            yield return CommitEverySeat(root, actor,
                NameOf(runtime, TrinketsCardContent.SalvageParts));
            yield return YieldUntilTheNamedSeatActs(runtime, enemy);
            yield return PointTrayAt(root, enemy);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                HeroView(actor).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            yield return ClickDefenseFace(root, TrinketsCardContent.DisruptorGrid);
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(actor) &&
                card.DefinitionReference.DefinitionId.Equals(
                    TrinketsCardContent.DisruptorGrid)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            yield return YieldUntilTheNamedSeatActs(runtime, actor);
            yield return PointTrayAt(root, actor);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            UiPlayer.Click(UiPlayer.Button(root.Hud,
                BulletLabelFor(runtime, "retrieve-arm")));
            yield return null;
            Assert.That(runtime.Projection.Obstacles.Any(obstacle =>
                obstacle.ObstacleId.Equals(OwnedObstacleIdentity.For(
                    TrinketsComponentContent.TurretKind, actor))), Is.False);
            yield return May(root, true);
            var choice = runtime.PendingEffectChoice.Candidates.Single(candidate =>
                candidate.Value.Kind == GuardsDigital.Core.Effects.EffectValueKind.CardInstance &&
                runtime.Cards.Single(card => card.Id.Equals(
                    candidate.Value.CardInstanceId)).DefinitionReference.DefinitionId
                    .Equals(TrinketsCardContent.DisruptorGrid));
            yield return Pick(root, choice);
            Assert.That(runtime.Cards.Single(card =>
                card.Id.Equals(choice.Value.CardInstanceId)).State.Location,
                Is.EqualTo(CardLocation.Hand));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator PrototypeFacesSwapWithTheChosenBoardPieceAndObserveTheirTurretClause()
        {
            foreach (var face in new[]
            {
                TrinketsCardContent.EarlyPrototype,
                TrinketsCardContent.UpdatedDesign
            })
            {
                yield return Stage(face, withTurret: true);
                var root = Root();
                var runtime = root.Runtime;
                var actor = Trinkets(runtime);
                var enemy = OtherHeroId(runtime, actor);
                var turretId = OwnedObstacleIdentity.For(
                    TrinketsComponentContent.TurretKind, actor);
                yield return Begin(root, face, ActionKind.Skill);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " must offer a piece in the Turret radius. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice.Candidates.Any(candidate =>
                    candidate.Value.Kind == GuardsDigital.Core.Effects.EffectValueKind.Coordinate &&
                    candidate.Value.Coordinate.Equals(EnemySpace)), Is.True);
                yield return Space(root, EnemySpace);
                Assert.That(runtime.Projection.Heroes.Single(hero =>
                    hero.HeroId.Equals(actor)).Coordinate, Is.EqualTo(EnemySpace));
                Assert.That(runtime.Projection.Heroes.Single(hero =>
                    hero.HeroId.Equals(enemy)).Coordinate, Is.EqualTo(ActorSpace));
                Assert.That(runtime.Projection.Obstacles.Any(obstacle =>
                    obstacle.ObstacleId.Equals(turretId)),
                    Is.EqualTo(face.Equals(TrinketsCardContent.UpdatedDesign)),
                    "Only Early Prototype removes the Turret after swapping.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator PerfectedDesignPlacesTrinketsInTheTurretRadiusWithoutMovingTheTurret()
        {
            yield return Stage(TrinketsCardContent.PerfectedDesign, withTurret: true);
            var root = Root();
            var runtime = root.Runtime;
            var actor = Trinkets(runtime);
            var turretId = OwnedObstacleIdentity.For(
                TrinketsComponentContent.TurretKind, actor);
            yield return Begin(root, TrinketsCardContent.PerfectedDesign, ActionKind.Skill);
            UiPlayer.Click(UiPlayer.Button(root.Hud, BulletLabelFor(runtime, "place-arm")));
            yield return null;
            var offered = runtime.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind ==
                    GuardsDigital.Core.Effects.EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .Where(space => !space.Equals(ActorSpace)).ToArray();
            var chosen = FirstClickableSpace(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), offered);
            yield return Space(root, chosen);
            Assert.That(runtime.Projection.Heroes.Single(hero =>
                hero.HeroId.Equals(actor)).Coordinate, Is.EqualTo(chosen));
            Assert.That(runtime.Projection.Obstacles.Single(obstacle =>
                obstacle.ObstacleId.Equals(turretId)).Coordinate,
                Is.EqualTo(InitialTurretSpace));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator PerfectedDesignAlsoSwapsWithTheChosenUnitWithoutRemovingTurret()
        {
            yield return Stage(TrinketsCardContent.PerfectedDesign, withTurret: true);
            var root = Root();
            var runtime = root.Runtime;
            var actor = Trinkets(runtime);
            var enemy = OtherHeroId(runtime, actor);
            var turretId = OwnedObstacleIdentity.For(
                TrinketsComponentContent.TurretKind, actor);
            yield return Begin(root, TrinketsCardContent.PerfectedDesign, ActionKind.Skill);
            UiPlayer.Click(UiPlayer.Button(root.Hud,
                BulletLabelFor(runtime, "swap-arm")));
            yield return null;
            Assert.That(runtime.PendingEffectChoice.Candidates.Any(candidate =>
                candidate.Value.Kind == GuardsDigital.Core.Effects.EffectValueKind.Coordinate &&
                candidate.Value.Coordinate.Equals(EnemySpace)), Is.True);
            yield return Space(root, EnemySpace);
            Assert.That(runtime.Projection.Heroes.Single(hero =>
                hero.HeroId.Equals(actor)).Coordinate, Is.EqualTo(EnemySpace));
            Assert.That(runtime.Projection.Heroes.Single(hero =>
                hero.HeroId.Equals(enemy)).Coordinate, Is.EqualTo(ActorSpace));
            Assert.That(runtime.Projection.Obstacles.Single(obstacle =>
                obstacle.ObstacleId.Equals(turretId)).Coordinate,
                Is.EqualTo(InitialTurretSpace));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator DetonationFacesAskTheEnemyForDiscardAndRemoveTheTurret()
        {
            foreach (var face in new[]
            {
                TrinketsCardContent.SelfDestruct,
                TrinketsCardContent.EmergencyProtocol
            })
            {
                yield return Stage(face, withTurret: true);
                var root = Root();
                var runtime = root.Runtime;
                var actor = Trinkets(runtime);
                var enemy = OtherHeroId(runtime, actor);
                var turretId = OwnedObstacleIdentity.For(
                    TrinketsComponentContent.TurretKind, actor);
                yield return Begin(root, face, ActionKind.Skill);
                // Milestone 9.1, ruling W3: the enemy hero in the Turret's radius is hit - Trinkets
                // is asked which hero, never whether.
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " must ask which enemy hero. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice.DeclineInvocation, Is.Null,
                    face.Value + ": W3 leaves nothing to decline. " + Remaining(runtime));
                var target = runtime.PendingEffectChoice.Candidates.Single(candidate =>
                    candidate.Value.Kind == GuardsDigital.Core.Effects.EffectValueKind.Hero &&
                    candidate.Value.HeroId.Equals(enemy));
                yield return Pick(root, target);
                if (runtime.PendingEffectChoice != null &&
                    runtime.PendingEffectChoice.Candidates.Any(candidate =>
                        candidate.Value.Kind ==
                            GuardsDigital.Core.Effects.EffectValueKind.CardInstance))
                    yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(candidate =>
                        candidate.Value.Kind ==
                            GuardsDigital.Core.Effects.EffectValueKind.CardInstance));
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    face.Value + ": one enemy hero stands in radius, so the second pass asks " +
                    "nothing. " + Remaining(runtime));
                Assert.That(runtime.Projection.Obstacles.Any(obstacle =>
                    obstacle.ObstacleId.Equals(turretId)), Is.False,
                    face.Value + " removes the Turret after the discard branch. " + Remaining(runtime));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesTrinketsThroughItsCardControl()
        {
            foreach (var definition in TrinketsCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement)))
            {
                yield return Stage(definition.Id);
                var root = Root();
                var runtime = root.Runtime;
                var actor = Trinkets(runtime);
                yield return Begin(root, definition.Id, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                    definition.Id.Value + " must offer a secondary Movement destination.");
                var target = FirstClickableSpace(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    runtime.PendingMovementChoice.LegalDestinations
                        .Where(value => value.Cost == 1)
                        .Select(value => value.Destination).ToArray());
                Assert.That(UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                    Tile(target).transform.position), Is.True);
                yield return null;
                Assert.That(runtime.Projection.Heroes.Single(hero =>
                    hero.HeroId.Equals(actor)).Coordinate, Is.EqualTo(target),
                    definition.Id.Value + " must move Trinkets to the clicked tile.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace()
        {
            foreach (var definition in TrinketsCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense)))
            {
                var committed = definition.Id.Equals(TrinketsCardContent.RapidRedeployment)
                    ? TrinketsCardContent.SalvageParts
                    : TrinketsCardContent.RapidRedeployment;
                yield return Stage(definition.Id,
                    enemyHand: new[] { WaspCardContent.MagneticDagger },
                    ownHand: new[] { definition.Id, committed });
                var root = Root();
                var runtime = root.Runtime;
                var defender = Trinkets(runtime);
                var enemy = OtherHeroId(runtime, defender);
                yield return PointTrayAt(root, defender);
                yield return CommitThroughTray(root, NameOf(runtime, committed));
                yield return PointTrayAt(root, enemy);
                yield return CommitThroughTray(root,
                    NameOf(runtime, WaspCardContent.MagneticDagger));
                yield return YieldUntilTheNamedSeatActs(runtime, enemy);
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                    HeroView(defender).transform.position), Is.True);
                yield return null;
                Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                    definition.Id.Value + " must reach p.14's Defense response. " + Remaining(runtime));
                Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)),
                    Is.True, definition.Id.Value + " must be an offered Defense face.");
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null);
                Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(defender) &&
                    card.DefinitionReference.DefinitionId.Equals(definition.Id)).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator TurretPlacementPauseRestoresAndRetainedPrefixUndoReplaysExactly()
        {
            yield return Stage(TrinketsCardContent.RapidRedeployment, withTurret: true);
            var root = Root();
            var runtime = root.Runtime;
            yield return Begin(root, TrinketsCardContent.RapidRedeployment, ActionKind.Skill);
            UiPlayer.Click(UiPlayer.Button(root.Hud,
                BulletLabelFor(runtime, "move-place")));
            yield return null;
            var moveTo = FirstClickableSpace(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                runtime.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind ==
                        GuardsDigital.Core.Effects.EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).ToArray());
            yield return Space(root, moveTo);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "After moving, the Turret placement remains to be answered.");
            var pause = runtime.CaptureSnapshot();
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("move"),
                "One Undo keeps the committed card and selected bullet, but takes back the move.");
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;
            yield return Space(root, moveTo);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause),
                "Re-answering the retained prefix must reconstruct the identical placement pause.");
            Assert.That(runtime.RestoreSnapshot(pause), Is.True);
            yield return null;
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place"));
            var destination = FirstClickableSpace(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                runtime.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind ==
                        GuardsDigital.Core.Effects.EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).ToArray());
            yield return Space(root, destination);
            Assert.That(runtime.Projection.Obstacles.Single(obstacle =>
                obstacle.ObstacleId.Equals(OwnedObstacleIdentity.For(
                    TrinketsComponentContent.TurretKind, Trinkets(runtime)))).Coordinate,
                Is.EqualTo(destination));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator TurretRemainsVisibleAfterTrinketsDefeatAndTheRoundBoundary()
        {
            yield return Stage(TrinketsCardContent.RapidRedeployment,
                withTurret: true,
                enemyHand: new[] { WaspCardContent.MagneticDagger, WaspCardContent.Shock },
                ownHand: new[] { TrinketsCardContent.RapidRedeployment });
            var root = Root();
            var runtime = root.Runtime;
            var actor = Trinkets(runtime);
            var enemy = OtherHeroId(runtime, actor);
            var turretId = OwnedObstacleIdentity.For(
                TrinketsComponentContent.TurretKind, actor);
            yield return CommitEverySeat(root, actor,
                NameOf(runtime, TrinketsCardContent.RapidRedeployment));
            yield return YieldUntilTheNamedSeatActs(runtime, enemy);
            yield return PointTrayAt(root, enemy);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                HeroView(actor).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(runtime.Projection.Heroes.Any(hero =>
                hero.HeroId.Equals(actor)), Is.False,
                "The attacker defeats Trinkets, so this is not merely an ordinary round transition.");
            Assert.That(runtime.Projection.Obstacles.Count(obstacle =>
                obstacle.ObstacleId.Equals(turretId)), Is.EqualTo(1),
                "Defeat does not remove the separate obstacle.");
            var defeatedInRound = runtime.RoundIndex;
            for (var budget = 0; budget < 180 &&
                runtime.RoundIndex == defeatedInRound; budget++)
            {
                if (runtime.RoundView.Model.ContinueEndOfRoundInvocation != null)
                {
                    Assert.That(runtime.ContinueEndOfRound(), Is.True,
                        "The filler advances p.8; the board-click respawn below is this journey's subject.");
                    yield return null;
                }
                else yield return NextRoundDecision(root);
            }
            Assert.That(runtime.RoundIndex, Is.EqualTo(defeatedInRound + 1),
                "The persistence assertion must cross the actual round boundary.");
            Assert.That(runtime.Projection.Obstacles.Count(obstacle =>
                obstacle.ObstacleId.Equals(turretId)), Is.EqualTo(1),
                "End-of-round cleanup removes tokens, not the Turret obstacle.");
            Assert.That(Object.FindObjectsByType<PlaceholderObstacleView>(FindObjectsSortMode.None).Count(view =>
                view.gameObject.activeSelf && view.Occupancy.ObstacleId.Equals(turretId)),
                Is.EqualTo(1));
            CommitSoTheNamedSeatActs(runtime, actor,
                TrinketsCardContent.RapidRedeployment);
            yield return YieldUntilTheNamedSeatActs(runtime, actor);
            var respawn = FirstClickableSpace(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                runtime.RoundView.Model.HeroRespawnOptions
                    .Select(option => option.Destination).ToArray());
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var camera = pointer.EventCamera;
            var tile = Tile(respawn);
            var depth = camera.WorldToScreenPoint(tile.transform.position).z;
            camera.transform.position += tile.transform.position -
                camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
            Assert.That(UiPlayer.ClickBoard(
                pointer, root.Hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.Projection.Heroes.Single(hero =>
                hero.HeroId.Equals(actor)).Coordinate, Is.EqualTo(respawn));
            Assert.That(runtime.Projection.Obstacles.Count(obstacle =>
                obstacle.ObstacleId.Equals(turretId)), Is.EqualTo(1),
                "Respawn does not manufacture or remove a Turret.");
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "RESET"));
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "Reset reconstructs the initial staged session.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>().Count(view =>
                view.Occupancy.HeroId.Equals(actor)), Is.EqualTo(1));
            Assert.That(runtime.Projection.Obstacles.Count(obstacle =>
                obstacle.ObstacleId.Equals(turretId)), Is.EqualTo(1),
                "Restart reconstructs exactly the one staged Turret.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator StagedTrinketsSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTrinkets);
            var root = Root();
            var setup = root.Runtime.Setup.Heroes.Single(hero =>
                hero.ContentId.Value == "trinkets");
            var views = Object.FindObjectsByType<PrimitiveHeroView>();
            var view = views.Single(value => value.Occupancy.HeroId.Equals(setup.HeroId));
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo("trinkets"));
            Assert.That(view.HasTokenVisual, Is.True);
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(setup.HeroId));
            Healthy(root.Runtime);
        }

        [UnityTest]
        public IEnumerator EveryTrinketsFaceKeepsTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTrinkets);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Trinkets", TrinketsCardContent.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator TrinketsLevelUpAddsTheChosenCardAndShowsItsSiblingItem()
        {
            yield return Stage(TrinketsCardContent.MakeshiftMinigun, coins: 0);
            var root = Root();
            var runtime = root.Runtime;
            var actor = Trinkets(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind ==
                    SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(actor));
                budget++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind,
                Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade),
                "Trinkets must reach a player-facing card upgrade.");
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .First(option => option.Card != null &&
                    option.ItemIcon == PrintedValueChannel.Attack);
            var before = runtime.Projection.HeroProgression.Single(value =>
                value.HeroId.Equals(actor));
            var previous = before.ItemBonuses.Where(value => value.Key == upgrade.ItemIcon)
                .Select(value => value.Value).SingleOrDefault();
            UiPlayer.EnsureVisible(UpgradeButton(root, upgrade));
            yield return null;
            UiPlayer.Click(UpgradeButton(root, upgrade));
            yield return null;
            Assert.That(runtime.Cards.Single(card =>
                card.Id.Equals(upgrade.Card.CardInstanceId)).State.Location,
                Is.EqualTo(CardLocation.Hand));
            for (var budget = 0; budget < 50 &&
                runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
                yield return NextRoundDecision(root);
            var after = runtime.Projection.HeroProgression.Single(value =>
                value.HeroId.Equals(actor));
            Assert.That(after.ItemBonuses.Single(value =>
                value.Key == upgrade.ItemIcon).Value, Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero =>
                hero.HeroId.Equals(actor)).ItemBonuses, Is.Not.Empty,
                "The selected upgrade's sibling item is shown to the player.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator UnlimitedFirepowerIsVisibleInTheTierThreeConfiguredScene()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeEngagedStagedTrinkets);
            var root = Root();
            var runtime = root.Runtime;
            var actor = Trinkets(runtime);
            Assert.That(runtime.Cards.Single(card =>
                card.OwnerId.Equals(actor) &&
                card.DefinitionReference.DefinitionId.Equals(
                    TrinketsCardContent.UnlimitedFirepower)).State.Location,
                Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero =>
                hero.HeroId.Equals(actor)).UltimateCard.DisplayName,
                Is.EqualTo("Unlimited Firepower"));
            Assert.That(UiPlayer.ItemStripDescendants(root.Hud).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "The face-up Tier III Ultimate belongs on the hero rail.");
            Healthy(runtime);
        }
    }
}
