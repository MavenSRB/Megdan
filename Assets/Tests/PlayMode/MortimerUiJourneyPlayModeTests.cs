using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.SnorriUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.8g. Real pointer and control journeys for Mortimer the Awakener, on a board measured
    /// headlessly before any was written. Mortimer stands on (-3, 1) and Wasp on (-2, 1); the two
    /// spaces beside both are (-3, 2) - cleared of its Blue minion by the staging - and (-2, 0), so
    /// Awaken! on turn one puts a Zombie on each and every Zombie face plays on turn two. The enemy
    /// minion a Red face targets stands on (-4, 2); Gathering Horde and Army of Darkness replace one
    /// staged on Wasp's space with Wasp moved away. Every journey asserts what changed on the board,
    /// in a hand, in the coins or in a standing effect.
    /// </summary>
    public sealed class MortimerUiJourneyPlayModeTests
    {
        private static readonly HexCoordinate MortimerSpace = new HexCoordinate(-3, 1);
        private static readonly HexCoordinate WaspSpace = new HexCoordinate(-2, 1);
        private static readonly HexCoordinate SouthZombie = new HexCoordinate(-3, 2);
        private static readonly HexCoordinate NorthZombie = new HexCoordinate(-2, 0);
        private static readonly HexCoordinate MinionSpace = new HexCoordinate(-4, 2);
        private static readonly HexCoordinate FarSpace = new HexCoordinate(0, 1);
        private static readonly MinionId StagedMinion = new MinionId("mortimer-journey-minion");

        internal static HeroId Mortimer(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "mortimer").HeroId;

        private static HeroId Wasp(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "wasp").HeroId;

        private static HexCoordinate SpaceOf(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Projection.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate;

        private static bool OnBoard(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Projection.Heroes.Any(value => value.HeroId.Equals(hero));

        private static HexCoordinate[] Zombies(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Projection.PlacedTokens
                .Where(token => token.Definition.Id.Equals(MortimerComponentContent.ZombieId))
                .Select(token => token.Coordinate).ToArray();

        private static int DrawnZombies() =>
            Object.FindObjectsByType<PlacedTokenView>(FindObjectsSortMode.None)
                .Count(view => view.Token != null &&
                    view.Token.Definition.Id.Equals(MortimerComponentContent.ZombieId));

        private static CardInstance Card(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId owner,
            CardDefinitionId face) =>
            runtime.Cards.Single(card => card.OwnerId.Equals(owner) &&
                card.DefinitionReference.DefinitionId.Equals(face));

        private static int HandOfCount(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId owner) =>
            runtime.Cards.Count(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);

        private static int CoinsOf(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(hero)).Coins;

        /// <summary>
        /// Loads <paramref name="scene"/>, then reloads it staged: Mortimer on (-3, 1) holding
        /// <paramref name="hand"/> (the rest of his set in his discard), Wasp where asked, (-3, 2)
        /// cleared, and optionally one enemy melee minion.
        /// </summary>
        internal static IEnumerator Stage(SceneCardContent scene, CardDefinitionId[] hand,
            HexCoordinate? wasp = null, HexCoordinate? enemyMinion = null, int? coins = null,
            bool clearSouth = true)
        {
            yield return LoadStagedScene(scene);
            var runtime = Root().Runtime;
            var mortimer = Mortimer(runtime);
            var waspId = Wasp(runtime);
            var waspAt = wasp ?? WaspSpace;
            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(mortimer, MortimerSpace, coins: coins, hand: hand),
                new SandboxHeroStaging(waspId, waspAt, hand: new[]
                {
                    WaspCardContent.MagneticDagger, WaspCardContent.StopProjectiles,
                    WaspCardContent.Shock, WaspCardContent.LiftUp
                })
            };
            var occupied = new List<HexCoordinate> { MortimerSpace, waspAt };
            if (clearSouth) occupied.Add(SouthZombie);
            if (enemyMinion.HasValue) occupied.Add(enemyMinion.Value);
            var enemySide = runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(waspId)).TeamSide;
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: heroes,
                spawnedMinions: enemyMinion.HasValue
                    ? new[]
                    {
                        new SandboxMinionStaging(StagedMinion, enemySide, StandardMinionType.Melee,
                            enemyMinion.Value)
                    }
                    : null,
                removedMinions: runtime.ReadModel.Minions
                    .Where(value => occupied.Contains(value.Coordinate))
                    .Select(value => value.MinionId));
            yield return LoadStagedScene(scene);
        }

        /// <summary>Commits the face this turn and starts its action of <paramref name="kind"/> through the tray control.</summary>
        internal static IEnumerator BeginMortimer(MatchSceneCompositionRoot root, CardDefinitionId face,
            ActionKind kind)
        {
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);
            yield return CommitEverySeat(root, mortimer, NameOf(runtime, face));
            yield return WaitForActor(root, mortimer);
            yield return PointTrayAt(root, mortimer);
            yield return StartActionThroughControl(root, kind);
        }

        /// <summary>Picks the first candidate <paramref name="which"/> admits, through the board or the window.</summary>
        private static IEnumerator PickWhere(MatchSceneCompositionRoot root, Func<EffectValue, bool> which,
            string why)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, why + " " + Remaining(runtime));
            var candidates = runtime.PendingEffectChoice.Candidates.Where(value => which(value.Value)).ToArray();
            Assert.That(candidates, Is.Not.Empty, why + " " + Remaining(runtime));
            yield return Pick(root, candidates[0]);
        }

        private static IEnumerator PickSpace(MatchSceneCompositionRoot root, HexCoordinate space, string why) =>
            PickWhere(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(space), why);

        /// <summary>Takes the modal bullet named by its node through the decision window's control.</summary>
        private static IEnumerator Bullet(MatchSceneCompositionRoot root, string node)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "A modal stands. " + Remaining(runtime));
            var control = UiPlayer.DecisionControl(runtime, command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.OptionNodeId.Value == node);
            Assert.That(control, Is.Not.Null, "No window control for bullet " + node + ". " +
                UiPlayer.Describe(root.Hud));
            UiPlayer.ClickDecisionControl(runtime, root.Hud, control);
            yield return null;
            yield return ConfirmIfStillStanding(root, choice.NodeId);
            yield return null;
        }

        private static bool At(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, string node) =>
            runtime.PendingEffectChoice != null && runtime.PendingEffectChoice.NodeId.Value == node;

        /// <summary>Takes "up to N times" pass <paramref name="pass"/> of the face whose nodes start with <paramref name="prefix"/>, if it is asked.</summary>
        private static IEnumerator TakePass(MatchSceneCompositionRoot root, string prefix, int pass)
        {
            if (At(root.Runtime, MortimerRepeatedChoice.UpToNode(prefix, pass))) yield return May(root, true);
        }

        /// <summary>Declines every further "up to N times" pass still asked.</summary>
        private static IEnumerator StopPasses(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            for (var budget = 0; budget < 6 && runtime.PendingEffectChoice != null &&
                runtime.PendingEffectChoice.NodeId.Value.EndsWith("-up-to", StringComparison.Ordinal); budget++)
                yield return May(root, false);
        }

        /// <summary>Turn 1: Awaken! puts a Zombie on each of <paramref name="spaces"/> and declines the rest.</summary>
        private static IEnumerator AwakenZombies(MatchSceneCompositionRoot root, params HexCoordinate[] spaces)
        {
            var runtime = root.Runtime;
            yield return BeginMortimer(root, MortimerCardContent.Awaken, ActionKind.Skill);
            for (var placed = 0; placed < 4 && runtime.PendingEffectChoice != null; placed++)
            {
                if (placed < spaces.Length) yield return PickSpace(root, spaces[placed], "Awaken! offers " + spaces[placed] + ".");
                else yield return May(root, false);
            }

            Assert.That(Zombies(runtime), Is.EquivalentTo(spaces), Remaining(runtime));
        }

        /// <summary>Turn 1 places both Zombies beside Mortimer and Wasp; turn 2 starts <paramref name="face"/>.</summary>
        private static IEnumerator ZombiesThen(MatchSceneCompositionRoot root, CardDefinitionId face, ActionKind kind)
        {
            yield return AwakenZombies(root, SouthZombie, NorthZombie);
            yield return EndTurnsUntil(root, 2);
            yield return BeginMortimer(root, face, kind);
        }

        /// <summary>Names the staged minion as the Attack's target by clicking its piece.</summary>
        private static IEnumerator TargetMinion(MatchSceneCompositionRoot root, MinionId minion)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetMinionIds, Has.Member(minion), Remaining(runtime));
            var view = Object.FindObjectsByType<StandardMinionView>()
                .Single(value => value.Occupancy != null && value.Occupancy.MinionId.Equals(minion));
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, view.transform.position), Is.True);
            yield return null;
        }

        /// <summary>
        /// One step toward the next round. Clearing (-3, 2) leaves Mortimer's side a minion short, so
        /// a round end may ask it to remove one at the Minion Battle; that is answered here, as Wuk's
        /// Dominance journey answers it.
        /// </summary>
        private static IEnumerator RoundStep(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            if (runtime.PendingMinionBattleChoice != null)
            {
                var battle = runtime.MinionChoiceView.Model;
                if (battle != null && battle.Candidates.Count > 0)
                    Assert.That(runtime.SubmitMinionChoiceCandidate(
                        battle.Candidates.First(value => value.Destination == null).MinionId), Is.True);
                Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                yield return null;
                yield break;
            }

            yield return NextRoundDecision(root);
        }

        /// <summary>Wasp attacks Mortimer with Magnetic Dagger this turn and waits at his Defense.</summary>
        private static IEnumerator WaspAttacksMortimer(MatchSceneCompositionRoot root, CardDefinitionId mortimerCommits)
        {
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);
            var wasp = Wasp(runtime);
            yield return PointTrayAt(root, mortimer);
            yield return CommitThroughTray(root, NameOf(runtime, mortimerCommits));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.MagneticDagger));
            yield return YieldUntilTheNamedSeatActs(runtime, wasp);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(mortimer).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
        }

        // -------------------------------------------------------------------------------------------
        // Gold and Silver
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator AwakenPlacesTwoZombiesBesideMortimerAndDeclinesTheRest()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                new[] { MortimerCardContent.Awaken, MortimerAttackFaces.KnifeOfTheLivingDead }, wasp: FarSpace);
            var root = Root();
            var runtime = root.Runtime;
            yield return AwakenZombies(root, SouthZombie, NorthZombie);
            Assert.That(DrawnZombies(), Is.EqualTo(2), "Both Zombies are drawn on the board.");
            Assert.That(runtime.PendingEffectChoice, Is.Null, "The third and fourth placements were declined.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator AwakenDiscardedToDefendStillPlacesItsZombies()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                new[] { MortimerCardContent.Awaken, MortimerGreenFaces.StageDive });
            var root = Root();
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);
            yield return WaspAttacksMortimer(root, MortimerGreenFaces.StageDive);
            Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                offer.SourceDefinitionReference.DefinitionId.Equals(MortimerCardContent.Awaken)), Is.True,
                "Awaken! is a Defense/Skill card (p.13-14): it may be discarded to defend.");
            yield return ClickDefenseFace(root, MortimerCardContent.Awaken);
            Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(mortimer),
                "Its text applies when discarded to defend: Mortimer places a Zombie. " + Remaining(runtime));
            yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate,
                "A space Awaken! offers.");
            for (var budget = 0; budget < 4 && runtime.PendingEffectChoice != null &&
                runtime.PendingEffectChoice.ChooserId.Equals(mortimer); budget++)
                yield return May(root, false);
            Assert.That(Zombies(runtime), Has.Length.EqualTo(1), "The defense placed one Zombie. " + Remaining(runtime));
            Assert.That(Card(runtime, mortimer, MortimerCardContent.Awaken).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator KnifeRemovesAZombieBesideWaspForAPlusOneAttackAndDefeatsHer()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                new[] { MortimerCardContent.Awaken, MortimerAttackFaces.KnifeOfTheLivingDead });
            var root = Root();
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);
            var wasp = Wasp(runtime);
            yield return ZombiesThen(root, MortimerAttackFaces.KnifeOfTheLivingDead, ActionKind.Attack);
            if (runtime.PendingAttackTargetChoice != null) yield return Target(root, wasp);
            yield return TakePass(root, MortimerAttackFaces.KnifePrefix, 1);
            yield return Bullet(root, MortimerAttackFaces.RemoveBullet(1));
            yield return PickSpace(root, SouthZombie, "A Zombie beside Wasp.");
            yield return StopPasses(root);
            Assert.That(Zombies(runtime), Is.EqualTo(new[] { NorthZombie }), "The Zombie is removed. " + Remaining(runtime));
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Has.Member(
                Card(runtime, mortimer, MortimerAttackFaces.KnifeOfTheLivingDead).Id),
                "The removal gives Mortimer +1 Attack for the Attack.");
            yield return TakeTheHit(root);
            Assert.That(OnBoard(runtime, wasp), Is.False, "The Attack defeats Wasp. " + Remaining(runtime));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Red
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator DeadFacesHitTheMinionThenWaspBesideAZombieDiscards()
        {
            foreach (var face in new[]
                     {
                         MortimerAttackFaces.CrawlingDead, MortimerAttackFaces.WalkingDead, MortimerAttackFaces.RacingDead
                     })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                    new[] { MortimerCardContent.Awaken, face }, enemyMinion: MinionSpace);
                var root = Root();
                var runtime = root.Runtime;
                var wasp = Wasp(runtime);
                yield return ZombiesThen(root, face, ActionKind.Attack);
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, face.Value + " " + Remaining(runtime));
                yield return TargetMinion(root, StagedMinion);
                yield return TakePass(root, MortimerAttackFaces.DeadPrefix, 1);
                var before = HandOfCount(runtime, wasp);
                yield return Bullet(root, MortimerAttackFaces.DiscardBullet(1));
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    "Another enemy hero in radius beside a Zombie.");
                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(wasp),
                    "Wasp chooses her own discard. " + Remaining(runtime));
                yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance, "Wasp's hand.");
                yield return StopPasses(root);
                Assert.That(HandOfCount(runtime, wasp), Is.EqualTo(before - 1), face.Value + " " + Remaining(runtime));
                Assert.That(runtime.Projection.Minions.Any(value => value.MinionId.Equals(StagedMinion)), Is.False,
                    face.Value + " defeated the targeted minion first.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator BrainsFacesRetrieveACardBeforeTheAttack()
        {
            foreach (var face in new[] { MortimerAttackFaces.Braains, MortimerAttackFaces.Braaaaaaains })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                    new[] { MortimerCardContent.Awaken, face });
                var root = Root();
                var runtime = root.Runtime;
                var mortimer = Mortimer(runtime);
                var wasp = Wasp(runtime);
                yield return ZombiesThen(root, face, ActionKind.Attack);
                if (runtime.PendingAttackTargetChoice != null) yield return Target(root, wasp);
                var before = HandOfCount(runtime, mortimer);
                yield return TakePass(root, MortimerAttackFaces.BrainsPrefix, 1);
                yield return Bullet(root, MortimerAttackFaces.RetrieveBullet(1));
                yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance,
                    "A discarded card of Mortimer's.");
                yield return StopPasses(root);
                Assert.That(HandOfCount(runtime, mortimer), Is.EqualTo(before + 1), face.Value + " " + Remaining(runtime));
                yield return TakeTheHit(root);
                Assert.That(OnBoard(runtime, wasp), Is.False, face.Value + " " + Remaining(runtime));
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Blue
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator SlamFacesPushWaspAwayFromAZombie()
        {
            foreach (var face in new[]
                     {
                         MortimerBlueFaces.CorpseSlam, MortimerBlueFaces.MorbidMosh, MortimerBlueFaces.MacabreMayhem
                     })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                    new[] { MortimerCardContent.Awaken, face });
                var root = Root();
                var runtime = root.Runtime;
                var wasp = Wasp(runtime);
                yield return ZombiesThen(root, face, ActionKind.Skill);
                yield return TakePass(root, MortimerBlueFaces.SlamPrefix, 1);
                yield return Bullet(root, MortimerBlueFaces.ShoveBullet(1));
                yield return PickSpace(root, NorthZombie, "The Zombie on (-2, 0), in range.");
                yield return PickSpace(root, NorthZombie, "It moves 0 spaces (W5).");
                if (runtime.PendingEffectChoice != null && runtime.PendingEffectChoice.SelectInvocations.Count == 0)
                    yield return May(root, true);
                yield return PickSpace(root, WaspSpace, "Wasp, beside that Zombie.");
                yield return StopPasses(root);
                Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(new HexCoordinate(-2, 2)),
                    face.Value + " pushes Wasp directly away from the Zombie. " + Remaining(runtime));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator RobbingFacesMoveAZombieAndGainACoin()
        {
            foreach (var face in new[] { MortimerBlueFaces.RobbingZombies, MortimerBlueFaces.StalkingScalpers })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                    new[] { MortimerCardContent.Awaken, face }, coins: 0);
                var root = Root();
                var runtime = root.Runtime;
                var mortimer = Mortimer(runtime);
                yield return ZombiesThen(root, face, ActionKind.Skill);
                var coins = CoinsOf(runtime, mortimer);
                yield return TakePass(root, MortimerBlueFaces.RobPrefix, 1);
                yield return Bullet(root, MortimerBlueFaces.CoinBullet(1));
                yield return PickSpace(root, SouthZombie, "The Zombie on (-3, 2), in range.");
                yield return PickSpace(root, new HexCoordinate(-2, 2), "One space.");
                yield return StopPasses(root);
                Assert.That(Zombies(runtime), Has.Member(new HexCoordinate(-2, 2)), face.Value);
                Assert.That(CoinsOf(runtime, mortimer), Is.EqualTo(coins + 1), face.Value + " " + Remaining(runtime));
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Green
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator DiveFacesSwapMortimerWithAZombie()
        {
            foreach (var face in new[]
                     {
                         MortimerGreenFaces.StageDive, MortimerGreenFaces.CrowdDrift, MortimerGreenFaces.CrowdSurf
                     })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                    new[] { MortimerCardContent.Awaken, face });
                var root = Root();
                var runtime = root.Runtime;
                var mortimer = Mortimer(runtime);
                yield return ZombiesThen(root, face, ActionKind.Skill);
                yield return TakePass(root, MortimerGreenFaces.DivePrefix, 1);
                yield return Bullet(root, MortimerGreenFaces.SwapBullet(1));
                yield return PickSpace(root, NorthZombie, "A Zombie in range.");
                yield return StopPasses(root);
                Assert.That(SpaceOf(runtime, mortimer), Is.EqualTo(NorthZombie), face.Value + " " + Remaining(runtime));
                Assert.That(Zombies(runtime), Has.Member(MortimerSpace), face.Value + ": the Zombie stands where he was.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator HordeFacesReplaceAnEnemyMinionBesideTwoZombiesWithAZombie()
        {
            foreach (var face in new[] { MortimerGreenFaces.GatheringHorde, MortimerGreenFaces.ArmyOfDarkness })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                    new[] { MortimerCardContent.Awaken, face }, wasp: FarSpace, enemyMinion: WaspSpace);
                var root = Root();
                var runtime = root.Runtime;
                yield return ZombiesThen(root, face, ActionKind.Skill);
                yield return TakePass(root, MortimerGreenFaces.HordePrefix, 1);
                yield return Bullet(root, MortimerGreenFaces.ReplaceBullet(1));
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Minion &&
                    value.MinionId.Equals(StagedMinion), "The enemy minion beside two Zombies.");
                yield return StopPasses(root);
                Assert.That(runtime.Projection.Minions.Any(value => value.MinionId.Equals(StagedMinion)), Is.False,
                    face.Value + " " + Remaining(runtime));
                Assert.That(Zombies(runtime), Has.Member(WaspSpace), face.Value + ": a Zombie takes its space.");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Ultimate
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator MasterOfPuppetsOpensAFourthAndFifthPass()
        {
            yield return Stage(SceneCardContent.VerifiedTierThreeEngagedStagedMortimer,
                new[] { MortimerBlueFaces.MacabreMayhem, MortimerCardContent.Awaken }, wasp: FarSpace);
            var root = Root();
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "Master of Puppets stands face up on Mortimer's rail.");
            yield return BeginMortimer(root, MortimerBlueFaces.MacabreMayhem, ActionKind.Skill);
            var steps = 0;
            for (var pass = 1; pass <= 5; pass++)
            {
                Assert.That(At(runtime, MortimerRepeatedChoice.UpToNode(MortimerBlueFaces.SlamPrefix, pass)), Is.True,
                    "Pass " + pass + " is offered. " + Remaining(runtime));
                if (pass < 4)
                {
                    yield return May(root, true);
                    yield return Bullet(root, MortimerBlueFaces.StepBullet(MortimerBlueFaces.SlamPrefix, pass));
                    yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate &&
                        !value.Coordinate.Equals(SpaceOf(runtime, mortimer)), "One space.");
                    steps++;
                }
                else
                {
                    // Passes four and five exist only with the Ultimate; taking the fourth proves it.
                    if (pass == 4)
                    {
                        yield return May(root, true);
                        yield return Bullet(root, MortimerBlueFaces.StepBullet(MortimerBlueFaces.SlamPrefix, pass));
                        yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate &&
                            !value.Coordinate.Equals(SpaceOf(runtime, mortimer)), "One space.");
                        steps++;
                    }
                    else yield return May(root, false);
                }
            }

            Assert.That(steps, Is.EqualTo(4));
            Assert.That(SpaceOf(runtime, mortimer), Is.Not.EqualTo(MortimerSpace));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Every face's ribbon
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesMortimerThroughItsCardControl()
        {
            foreach (var definition in MortimerCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                    new[] { definition.Id, MortimerCardContent.Awaken }, wasp: FarSpace);
                var root = Root();
                var runtime = root.Runtime;
                var mortimer = Mortimer(runtime);
                yield return BeginMortimer(root, definition.Id, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, definition.Id.Value);
                var target = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    runtime.PendingMovementChoice.LegalDestinations
                        .Where(value => value.Cost == 1).Select(value => value.Destination).ToArray());
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, Tile(target).transform.position), Is.True);
                yield return null;
                Assert.That(SpaceOf(runtime, mortimer), Is.EqualTo(target), definition.Id.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace()
        {
            foreach (var definition in MortimerCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                var committed = definition.Id.Equals(MortimerGreenFaces.StageDive)
                    ? MortimerBlueFaces.CorpseSlam : MortimerGreenFaces.StageDive;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer, new[] { definition.Id, committed });
                var root = Root();
                var runtime = root.Runtime;
                var mortimer = Mortimer(runtime);
                yield return WaspAttacksMortimer(root, committed);
                Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)), Is.True, definition.Id.Value);
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, definition.Id.Value);
                Assert.That(Card(runtime, mortimer, definition.Id).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile), definition.Id.Value);
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Session continuity, lifecycle and components
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator APausedCrawlingDiscardRestoresAtWaspsQuestion()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                new[] { MortimerCardContent.Awaken, MortimerAttackFaces.CrawlingDead }, enemyMinion: MinionSpace);
            var root = Root();
            var runtime = root.Runtime;
            var wasp = Wasp(runtime);
            yield return ZombiesThen(root, MortimerAttackFaces.CrawlingDead, ActionKind.Attack);
            yield return TargetMinion(root, StagedMinion);
            yield return Bullet(root, MortimerAttackFaces.DiscardBullet(1));
            yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "Wasp, beside a Zombie.");
            var asked = runtime.PendingEffectChoice;
            Assert.That(asked?.ChooserId ?? default, Is.EqualTo(wasp), Remaining(runtime));
            var before = HandOfCount(runtime, wasp);
            var pause = runtime.CaptureSnapshot();
            yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance, "Wasp's hand.");
            Assert.That(HandOfCount(runtime, wasp), Is.EqualTo(before - 1));
            Assert.That(runtime.RestoreSnapshot(pause), Is.True);
            yield return null;
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked.NodeId),
                "The restored match waits at Wasp's discard. " + Remaining(runtime));
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            Assert.That(Zombies(runtime), Is.EquivalentTo(new[] { SouthZombie, NorthZombie }),
                "The Zombies are read back from the snapshot.");
            yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance, "Wasp's hand.");
            Assert.That(HandOfCount(runtime, wasp), Is.EqualTo(before - 1), "The restored discard resolves.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator AnAwakenPlacementPauseUndoesToItsRetainedPrefixAndReplaysExactly()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                new[] { MortimerCardContent.Awaken, MortimerAttackFaces.KnifeOfTheLivingDead }, wasp: FarSpace);
            var root = Root();
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);
            yield return BeginMortimer(root, MortimerCardContent.Awaken, ActionKind.Skill);
            var first = runtime.PendingEffectChoice.NodeId;
            var pause = runtime.CaptureSnapshot();
            yield return PickSpace(root, SouthZombie, "The first Zombie.");
            Assert.That(Zombies(runtime), Is.EqualTo(new[] { SouthZombie }));
            OpenDirectorDrawer(root.Hud);
            yield return null;
            for (var press = 0; press < 3 && !(runtime.PendingEffectChoice != null &&
                runtime.PendingEffectChoice.NodeId.Equals(first)); press++)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(first),
                "Undo gives the first placement back. " + Remaining(runtime));
            Assert.That(Zombies(runtime), Is.Empty);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause), "The retained prefix is the same pause.");
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;
            yield return PickSpace(root, SouthZombie, "The same space again.");
            Assert.That(Zombies(runtime), Is.EqualTo(new[] { SouthZombie }));
            Assert.That(SpaceOf(runtime, mortimer), Is.EqualTo(MortimerSpace));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator ZombiesOutlastMortimersDefeatAndTheRoundEndBeforeARestart()
        {
            // Two cards: Mortimer holds nothing after turn 2, so he is not asked to respawn inside the round.
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                new[] { MortimerCardContent.Awaken, MortimerAttackFaces.KnifeOfTheLivingDead });
            var root = Root();
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);
            yield return AwakenZombies(root, SouthZombie, NorthZombie);
            var placed = Zombies(runtime);
            yield return EndTurnsUntil(root, 2);
            yield return WaspAttacksMortimer(root, MortimerAttackFaces.KnifeOfTheLivingDead);
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(OnBoard(runtime, mortimer), Is.False, "Wasp defeats Mortimer. " + Remaining(runtime));
            Assert.That(Zombies(runtime), Is.EquivalentTo(placed), "The Zombies stand through his defeat.");
            var round = runtime.RoundIndex;
            for (var budget = 0; budget < 180 && runtime.RoundIndex == round; budget++)
            {
                if (runtime.RoundView.Model.ContinueEndOfRoundInvocation != null)
                {
                    Assert.That(runtime.ContinueEndOfRound(), Is.True);
                    yield return null;
                }
                else if (runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade)
                {
                    yield return null;
                    var shown = UiPlayer.Descendants(root.Hud.RootElement)
                        .OfType<UnityEngine.UIElements.Button>()
                        .Where(element => element.name == "gd-upgrade-option" && UiPlayer.IsShown(element))
                        .ToArray();
                    Assert.That(shown, Is.Not.Empty, UiPlayer.Describe(root.Hud));
                    UiPlayer.EnsureVisible(shown[0]);
                    yield return null;
                    UiPlayer.Click(shown[0]);
                    yield return null;
                }
                else yield return RoundStep(root);
            }

            Assert.That(runtime.RoundIndex, Is.EqualTo(round + 1));
            Assert.That(Zombies(runtime), Is.EquivalentTo(placed),
                "MO-Q4: Zombie tokens are not removed at the end of the round.");
            Assert.That(DrawnZombies(), Is.EqualTo(placed.Length), "The rebuilt board still draws them.");
            CommitSoTheNamedSeatActs(runtime, mortimer, MortimerAttackFaces.KnifeOfTheLivingDead);
            yield return YieldUntilTheNamedSeatActs(runtime, mortimer);
            var respawn = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                runtime.RoundView.Model.HeroRespawnOptions.Select(option => option.Destination).ToArray());
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var camera = pointer.EventCamera;
            var tile = Tile(respawn);
            var depth = camera.WorldToScreenPoint(tile.transform.position).z;
            camera.transform.position += tile.transform.position -
                camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(SpaceOf(runtime, mortimer), Is.EqualTo(respawn));
            // After the respawn the turn controls stand over the Director control, and the spotlight
            // of the Defeat that sent him there may still be entering: take whichever is enabled,
            // waiting for the spotlight to arm, until the Director control returns.
            yield return UiPlayer.WaitForControl(root.Hud, "DIRECTOR");

            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "RESET"));
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(SpaceOf(runtime, mortimer), Is.EqualTo(MortimerSpace), "Restart reconstructs the staged board.");
            Assert.That(Zombies(runtime), Is.Empty);
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator StagedMortimerSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMortimer);
            var root = Root();
            var mortimer = Mortimer(root.Runtime);
            var view = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(value => value.Occupancy.HeroId.Equals(mortimer));
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo("mortimer"));
            Assert.That(view.HasTokenVisual, Is.True);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(mortimer));
            Healthy(root.Runtime);
        }

        [UnityTest]
        public IEnumerator EveryMortimerFaceKeepsTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMortimer);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Mortimer", MortimerCardContent.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator MortimerLevelUpAddsTheChosenCardAndShowsItsSiblingItem()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMortimer,
                new[] { MortimerAttackFaces.KnifeOfTheLivingDead, MortimerCardContent.Awaken }, coins: 0,
                clearSouth: false);
            var root = Root();
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(mortimer));
                budget++)
                yield return RoundStep(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind,
                Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .First(option => option.Card != null && option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(mortimer));
            var previous = before.ItemBonuses.Where(value => value.Key == upgrade.ItemIcon)
                .Select(value => value.Value).SingleOrDefault();
            UiPlayer.EnsureVisible(UpgradeButton(root, upgrade));
            yield return null;
            UiPlayer.Click(UpgradeButton(root, upgrade));
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(upgrade.Card.CardInstanceId))
                .State.Location, Is.EqualTo(CardLocation.Hand));
            for (var budget = 0; budget < 50 && runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
                yield return RoundStep(root);
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(mortimer));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value,
                Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero =>
                hero.HeroId.Equals(mortimer)).ItemBonuses, Is.Not.Empty);
            Healthy(runtime);
        }
    }
}
