using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
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
    /// Milestone 8.114g. Real pointer and control journeys for Wuk the Grove Keeper, on boards measured
    /// headlessly before any was written. Wuk stands on (-3, 1): Mystic Saplings reaches (-4, 1),
    /// (-3, 0) and (-2, 0) beside him; the file east of him - (-2, 1), (-1, 1), (0, 1) - is the line a
    /// trample walks through Wasp; the Red ranged minion on (-4, 2) is the enemy minion beside him at
    /// the round's Minion Battle, where excluding it turns a tie into a Red loss. The sandbox stages no
    /// token, so every Tree face's journey plants its Trees with Mystic Saplings on turn one. Every
    /// journey asserts what changed on the board, in a hand or in the battle.
    /// </summary>
    public sealed class WukUiJourneyPlayModeTests
    {
        private static readonly HexCoordinate WukSpace = new HexCoordinate(-3, 1);
        private static readonly HexCoordinate AdjacentSpace = new HexCoordinate(-2, 1);
        private static readonly HexCoordinate TwoAwaySpace = new HexCoordinate(-1, 1);
        private static readonly HexCoordinate FarSpace = new HexCoordinate(0, 1);
        private static readonly HexCoordinate ArienSpace = new HexCoordinate(-3, 2);
        private static readonly MinionId StagedMinion = new MinionId("wuk-journey-minion");

        internal static HeroId Wuk(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "wuk").HeroId;

        private static HeroId HeroOf(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, string content) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == content).HeroId;

        private static HexCoordinate SpaceOf(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Projection.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate;

        private static bool OnBoard(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Projection.Heroes.Any(value => value.HeroId.Equals(hero));

        private static HexCoordinate[] Trees(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Projection.PlacedTokens
                .Where(token => token.Definition.Id.Equals(WukComponentContent.TreeId))
                .Select(token => token.Coordinate).ToArray();

        private static int DrawnTrees() =>
            Object.FindObjectsByType<PlacedTokenView>(FindObjectsSortMode.None)
                .Count(view => view.Token != null &&
                    view.Token.Definition.Id.Equals(WukComponentContent.TreeId));

        private static CardInstance Card(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId owner,
            CardDefinitionId face) =>
            runtime.Cards.Single(card => card.OwnerId.Equals(owner) &&
                card.DefinitionReference.DefinitionId.Equals(face));

        private static int HandOfCount(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId owner) =>
            runtime.Cards.Count(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);

        /// <summary>
        /// Loads <paramref name="scene"/>, then reloads it staged: Wuk on <paramref name="wuk"/> holding
        /// <paramref name="hand"/>, Wasp where asked, Arien (on the ally board) holding two cards - one
        /// for each of the journey's two turns - so the rest of his set is in his discard, and
        /// optionally one melee minion of Wuk's side.
        /// </summary>
        internal static IEnumerator Stage(SceneCardContent scene, CardDefinitionId[] hand,
            HexCoordinate? wuk = null, HexCoordinate? wasp = null, HexCoordinate? arien = null,
            HexCoordinate? wuksMinion = null, int? coins = null, bool minionIsEnemy = false)
        {
            yield return LoadStagedScene(scene);
            var runtime = Root().Runtime;
            var wukId = Wuk(runtime);
            var waspId = HeroOf(runtime, "wasp");
            var wukAt = wuk ?? WukSpace;
            var waspAt = wasp ?? AdjacentSpace;
            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(wukId, wukAt, coins: coins, hand: hand),
                new SandboxHeroStaging(waspId, waspAt, hand: new[]
                {
                    WaspCardContent.MagneticDagger, WaspCardContent.StopProjectiles,
                    WaspCardContent.Shock, WaspCardContent.LiftUp
                })
            };
            var occupied = new List<HexCoordinate> { wukAt, waspAt };
            if (runtime.Setup.Heroes.Any(hero => hero.ContentId.Value == "arien"))
            {
                var arienAt = arien ?? ArienSpace;
                heroes.Add(new SandboxHeroStaging(HeroOf(runtime, "arien"), arienAt,
                    hand: ArienCardContent.DealtDefinitions.Take(2).Select(definition => definition.Id)));
                occupied.Add(arienAt);
            }

            if (wuksMinion.HasValue) occupied.Add(wuksMinion.Value);
            var wukSide = runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(wukId)).TeamSide;
            // Milestone 9.2: an enemy minion where the case needs one Wuk may defeat (W4).
            var minionSide = minionIsEnemy
                ? runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(waspId)).TeamSide
                : wukSide;
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: heroes,
                spawnedMinions: wuksMinion.HasValue
                    ? new[]
                    {
                        new SandboxMinionStaging(StagedMinion, minionSide, StandardMinionType.Melee,
                            wuksMinion.Value)
                    }
                    : null,
                removedMinions: runtime.ReadModel.Minions
                    .Where(value => occupied.Contains(value.Coordinate))
                    .Select(value => value.MinionId));
            yield return LoadStagedScene(scene);
        }

        /// <summary>Commits the face this turn and starts its action of <paramref name="kind"/> through the tray control.</summary>
        internal static IEnumerator BeginWuk(MatchSceneCompositionRoot root, CardDefinitionId face,
            ActionKind kind)
        {
            var runtime = root.Runtime;
            var wuk = Wuk(runtime);
            yield return CommitEverySeat(root, wuk, NameOf(runtime, face));
            yield return WaitForActor(root, wuk);
            yield return PointTrayAt(root, wuk);
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

        /// <summary>Picks a clickable space among the candidates <paramref name="which"/> admits.</summary>
        private static IEnumerator PickSpace(MatchSceneCompositionRoot root, Func<HexCoordinate, bool> which,
            string why)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, why + " " + Remaining(runtime));
            var spaces = runtime.PendingEffectChoice.Candidates
                .Where(value => value.Value.Kind == EffectValueKind.Coordinate && which(value.Value.Coordinate))
                .Select(value => value.Value.Coordinate).ToArray();
            Assert.That(spaces, Is.Not.Empty, why + " " + Remaining(runtime));
            yield return Space(root, FirstClickableSpace(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), spaces));
        }

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

        /// <summary>Turn 1: Mystic Saplings plants <paramref name="count"/> Trees beside Wuk and declines the rest.</summary>
        private static IEnumerator PlantTrees(MatchSceneCompositionRoot root, int count)
        {
            var runtime = root.Runtime;
            var wuk = Wuk(runtime);
            yield return BeginWuk(root, WukCardContent.MysticSaplings, ActionKind.Skill);
            for (var placed = 0; placed < 3 && runtime.PendingEffectChoice != null; placed++)
            {
                if (placed < count)
                {
                    var at = SpaceOf(runtime, wuk);
                    yield return PickSpace(root, space => HexDistanceQuery.Calculate(at, space) == 1,
                        "Mystic Saplings offers a space beside Wuk.");
                }
                else
                {
                    yield return May(root, false);
                }
            }

            Assert.That(Trees(runtime), Has.Length.EqualTo(count), Remaining(runtime));
        }

        /// <summary>Wasp attacks Wuk with Magnetic Dagger this turn and waits at his Defense.</summary>
        private static IEnumerator WaspAttacksWuk(MatchSceneCompositionRoot root, CardDefinitionId wukCommits)
        {
            var runtime = root.Runtime;
            var wuk = Wuk(runtime);
            var wasp = HeroOf(runtime, "wasp");
            yield return PointTrayAt(root, wuk);
            yield return CommitThroughTray(root, NameOf(runtime, wukCommits));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.MagneticDagger));
            yield return YieldUntilTheNamedSeatActs(runtime, wasp);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(wuk).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
        }

        /// <summary>Walks Wuk's open Movement to <paramref name="destination"/> by a board click, then stops it.</summary>
        private static IEnumerator WalkAndStop(MatchSceneCompositionRoot root, HexCoordinate destination)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingMovementChoice?.LegalDestinations
                .Any(value => value.Destination.Equals(destination)), Is.True,
                "The walk offers " + destination + ". " + Remaining(runtime));
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, Tile(destination).transform.position), Is.True);
            yield return null;
            if (runtime.PendingMovementChoice != null)
            {
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                Assert.That(stop.Command.Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
                UiPlayer.Click(UiPlayer.Button(root.Hud, stop.Label));
                yield return null;
            }
        }

        // -------------------------------------------------------------------------------------------
        // Gold and Silver
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator MysticSaplingsPlantsTwoTreesBesideWukAndDeclinesTheThird()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                new[] { WukCardContent.MysticSaplings, WukCardContent.TreeSlam }, wasp: FarSpace);
            var root = Root();
            var runtime = root.Runtime;
            yield return PlantTrees(root, 2);
            Assert.That(DrawnTrees(), Is.EqualTo(2), "Both Trees are drawn on the board.");
            Assert.That(runtime.PendingEffectChoice, Is.Null, "The third placement was declined.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator TreeSlamRemovesTheTreeBesideWukThenHitsWaspInRange()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                new[] { WukCardContent.MysticSaplings, WukCardContent.TreeSlam, WukRedFaces.NaturesProtector },
                wasp: TwoAwaySpace);
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            yield return PlantTrees(root, 1);
            yield return EndTurnsUntil(root, 2);
            yield return BeginWuk(root, WukCardContent.TreeSlam, ActionKind.Attack);
            yield return Bullet(root, "tree-slam-tree-arm");
            yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate,
                "Tree Slam names the Tree beside Wuk.");
            Assert.That(Trees(runtime), Is.Empty, "The Tree is removed. " + Remaining(runtime));
            if (runtime.PendingEffectChoice != null)
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero &&
                    value.HeroId.Equals(wasp), "Tree Slam targets Wasp in range.");
            if (runtime.PendingAttackTargetChoice != null) yield return Target(root, wasp);
            yield return TakeTheHit(root);
            Assert.That(OnBoard(runtime, wasp), Is.False, "The Attack defeats Wasp. " + Remaining(runtime));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Red
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator NaturesAttacksHitTheHeroBesideWuk()
        {
            foreach (var face in new[]
                     {
                         WukRedFaces.NaturesProtector, WukRedFaces.NaturesGuardian, WukRedFaces.NaturesChampion
                     })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                    new[] { face, WukCardContent.MysticSaplings });
                var root = Root();
                var runtime = root.Runtime;
                var wasp = HeroOf(runtime, "wasp");
                yield return BeginWuk(root, face, ActionKind.Attack);
                yield return Bullet(root, WukRedFaces.NatureAdjacentBulletNode);
                if (runtime.PendingAttackTargetChoice != null) yield return Target(root, wasp);
                yield return TakeTheHit(root);
                Assert.That(OnBoard(runtime, wasp), Is.False, face.Value + ". " + Remaining(runtime));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator TramplesWalkThroughWaspInALineWhoDiscardsAndDefeatTheMinionCrossed()
        {
            foreach (var face in new[] { WukRedFaces.Trample, WukRedFaces.AngryStampede })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                    new[] { face, WukCardContent.MysticSaplings }, wuksMinion: TwoAwaySpace,
                    minionIsEnemy: true);
                var root = Root();
                var runtime = root.Runtime;
                var wuk = Wuk(runtime);
                var wasp = HeroOf(runtime, "wasp");
                yield return BeginWuk(root, face, ActionKind.Movement);
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds.Contains(Card(runtime, wuk, face).Id),
                    Is.True, face.Value + "'s text stands before the walk. " + Remaining(runtime));
                var before = HandOfCount(runtime, wasp);
                yield return WalkAndStop(root, FarSpace);
                Assert.That(SpaceOf(runtime, wuk), Is.EqualTo(FarSpace), face.Value);
                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(wasp),
                    "Wasp, moved through, chooses her discard. " + Remaining(runtime));
                yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance, "Wasp's hand.");
                Assert.That(HandOfCount(runtime, wasp), Is.EqualTo(before - 1), face.Value);
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Minion &&
                    value.MinionId.Equals(StagedMinion), "The enemy minion moved through (W4, milestone 9.2).");
                Assert.That(runtime.Projection.Minions.Any(value => value.MinionId.Equals(StagedMinion)),
                    Is.False, face.Value + " defeats the minion it crossed. " + Remaining(runtime));
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Blue
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator BlueFacesTakeATreeAndRetrieveACardForWukOrAFriend()
        {
            foreach (var face in new[]
                     {
                         WukBlueFaces.GiftsOfNature, WukBlueFaces.TreeOfPlenty, WukBlueFaces.Abundance
                     })
            {
                yield return Stage(SceneCardContent.VerifiedStagedWukWithAlly,
                    new[] { WukCardContent.MysticSaplings, face }, wasp: FarSpace);
                var root = Root();
                var runtime = root.Runtime;
                var wuk = Wuk(runtime);
                var arien = HeroOf(runtime, "arien");
                yield return PlantTrees(root, 1);
                yield return EndTurnsUntil(root, 2);
                var wukHand = HandOfCount(runtime, wuk);
                yield return BeginWuk(root, face, ActionKind.Skill);
                // Counted after the commitments: Arien has just committed his turn-two card.
                var arienHand = HandOfCount(runtime, arien);
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate,
                    face.Value + " names the Tree in radius.");
                Assert.That(Trees(runtime), Is.Empty, face.Value + " removes the Tree.");
                var friend = !face.Equals(WukBlueFaces.GiftsOfNature);
                if (friend) yield return Bullet(root, WukBlueFaces.FriendBulletNode);
                else yield return May(root, true);
                if (friend)
                    yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero &&
                        value.HeroId.Equals(arien), "The friendly hero in radius.");
                if (runtime.PendingEffectChoice != null && runtime.PendingEffectChoice.ConfirmInvocation != null &&
                    runtime.PendingEffectChoice.SelectInvocations.Count == 0)
                    yield return May(root, true);
                yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance,
                    "A discarded card to retrieve.");
                if (friend)
                    Assert.That(HandOfCount(runtime, arien), Is.EqualTo(arienHand + 1), face.Value);
                else
                    Assert.That(HandOfCount(runtime, wuk), Is.EqualTo(wukHand), face.Value +
                        ": the played face left the hand and the retrieved card joined it.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator DominanceTurnsATiedBattleIntoARedLossBeforeTheBattle()
        {
            foreach (var face in new[] { WukBlueFaces.ClaimDominance, WukBlueFaces.AssertDominance })
            {
                // Measured headlessly: on this board the round's battle is a tie; excluding the Red
                // ranged minion beside Wuk leaves Red one short, so Red must remove one.
                yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                    new[] { face, WukCardContent.MysticSaplings }, wasp: FarSpace);
                var root = Root();
                var runtime = root.Runtime;
                yield return BeginWuk(root, face, ActionKind.Skill);
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds.Contains(
                    Card(runtime, Wuk(runtime), face).Id), Is.True, face.Value + " stands for the round.");
                var round = runtime.RoundIndex;
                var asked = false;
                var redBefore = runtime.Projection.Minions.Count(value => value.TeamSide == TeamSide.Red);
                var battled = false;
                for (var budget = 0; budget < 200 && runtime.RoundIndex == round; budget++)
                {
                    if (runtime.PendingEffectChoice != null &&
                        runtime.PendingEffectChoice.NodeId.Value == WukDominanceContent.ExcludeNode)
                    {
                        asked = true;
                        yield return PickWhere(root, value => value.Kind == EffectValueKind.Minion,
                            "The enemy minion beside Wuk.");
                        continue;
                    }

                    if (runtime.PendingMinionBattleChoice != null)
                    {
                        battled = true;
                        var battle = runtime.MinionChoiceView.Model;
                        if (battle != null && battle.Candidates.Count > 0)
                            Assert.That(runtime.SubmitMinionChoiceCandidate(
                                battle.Candidates.First(value => value.Destination == null).MinionId), Is.True);
                        Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                        yield return null;
                        continue;
                    }

                    if (runtime.RoundView.Model.ContinueEndOfRoundInvocation != null)
                    {
                        Assert.That(runtime.ContinueEndOfRound(), Is.True);
                        yield return null;
                        continue;
                    }

                    yield return NextRoundDecision(root);
                    if (battled) break;
                }

                Assert.That(asked, Is.True, face.Value + " asks Wuk at the pre-battle moment.");
                Assert.That(battled, Is.True, "Red lost the battle it would have tied. " + Remaining(runtime));
                Assert.That(runtime.Projection.Minions.Count(value => value.TeamSide == TeamSide.Red),
                    Is.LessThan(redBefore), face.Value);
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Green
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ThrowsPlaceWaspIntoASpaceInRangeAndMonstrousThrowRepeats()
        {
            foreach (var face in new[]
                     {
                         WukGreenFaces.TossAway, WukGreenFaces.MightyThrow, WukGreenFaces.MonstrousThrow
                     })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                    new[] { face, WukCardContent.MysticSaplings });
                var root = Root();
                var runtime = root.Runtime;
                var wasp = HeroOf(runtime, "wasp");
                yield return BeginWuk(root, face, ActionKind.Skill);
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(AdjacentSpace), "Wasp, beside Wuk.");
                var landing = new HexCoordinate(-3, 0);
                yield return PickSpace(root, space => space.Equals(landing), "A space in range, still beside Wuk.");
                Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(landing), face.Value);
                if (face.Equals(WukGreenFaces.MonstrousThrow))
                {
                    yield return May(root, true);
                    yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(landing), "The same Wasp again (p.10).");
                    yield return PickSpace(root, space => space.Equals(AdjacentSpace), "Back beside Wuk.");
                    Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(AdjacentSpace), "The repeat throws again.");
                }

                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator SwapsTradeWukOrAFriendWithATree()
        {
            foreach (var face in new[] { WukGreenFaces.IntoTheCanopy, WukGreenFaces.TreetopRide })
            {
                yield return Stage(SceneCardContent.VerifiedStagedWukWithAlly,
                    new[] { WukCardContent.MysticSaplings, face }, wasp: FarSpace);
                var root = Root();
                var runtime = root.Runtime;
                var wuk = Wuk(runtime);
                var arien = HeroOf(runtime, "arien");
                yield return PlantTrees(root, 2);
                yield return EndTurnsUntil(root, 2);
                var trees = Trees(runtime);
                yield return BeginWuk(root, face, ActionKind.Skill);
                // The swap is asked as a "do it / stop here" before its bullets are offered.
                if (runtime.PendingEffectChoice != null && runtime.PendingEffectChoice.SelectInvocations.Count == 0)
                    yield return May(root, true);
                yield return Bullet(root, WukGreenFaces.SwapWithTreeBulletNode(0));
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate &&
                    trees.Contains(value.Coordinate), "A Tree in radius.");
                Assert.That(trees, Has.Member(SpaceOf(runtime, wuk)), face.Value + ": Wuk stands where a Tree was.");
                Assert.That(Trees(runtime), Has.Member(WukSpace), face.Value + ": the Tree stands where Wuk was.");
                if (face.Equals(WukGreenFaces.TreetopRide))
                {
                    var arienAt = SpaceOf(runtime, arien);
                    if (runtime.PendingEffectChoice != null && runtime.PendingEffectChoice.SelectInvocations.Count == 0)
                        yield return May(root, true);
                    yield return Bullet(root, WukGreenFaces.SwapFriendBulletNode(1));
                    var tree = Trees(runtime).First(space => !space.Equals(arienAt));
                    yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(arienAt), "Arien, the friendly unit.");
                    if (runtime.PendingEffectChoice != null)
                        yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate &&
                            Trees(runtime).Contains(value.Coordinate), "A Tree to trade with him.");
                    Assert.That(SpaceOf(runtime, arien), Is.Not.EqualTo(arienAt),
                        "The second performance swaps Arien with a Tree. " + Remaining(runtime));
                }

                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Ultimate
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator MarchOfNaturePlantsATreeAfterWukResolvesACard()
        {
            yield return Stage(SceneCardContent.VerifiedTierThreeEngagedStagedWuk,
                new[] { WukCardContent.TreeSlam, WukCardContent.MysticSaplings }, wasp: FarSpace);
            var root = Root();
            var runtime = root.Runtime;
            var wuk = Wuk(runtime);
            Assert.That(UiPlayer.ItemStripDescendants(root.Hud).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "March of Nature stands face up on Wuk's rail.");
            yield return BeginWuk(root, WukCardContent.TreeSlam, ActionKind.Movement);
            yield return WalkAndStop(root, new HexCoordinate(-3, 0));
            Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(wuk),
                "Resolving Tree Slam fires March of Nature. " + Remaining(runtime));
            var at = SpaceOf(runtime, wuk);
            yield return PickSpace(root, space => HexDistanceQuery.Calculate(at, space) <= 3,
                "A space in Radius 3.");
            Assert.That(Trees(runtime), Has.Length.EqualTo(1), "The Ultimate plants one Tree.");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Every face's ribbon
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesWukThroughItsCardControl()
        {
            foreach (var definition in WukCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                var other = definition.Id.Equals(WukCardContent.MysticSaplings)
                    ? WukCardContent.TreeSlam : WukCardContent.MysticSaplings;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk, new[] { definition.Id, other },
                    wasp: FarSpace);
                var root = Root();
                var runtime = root.Runtime;
                var wuk = Wuk(runtime);
                yield return BeginWuk(root, definition.Id, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, definition.Id.Value);
                var target = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    runtime.PendingMovementChoice.LegalDestinations
                        .Where(value => value.Cost == 1).Select(value => value.Destination).ToArray());
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, Tile(target).transform.position), Is.True);
                yield return null;
                Assert.That(SpaceOf(runtime, wuk), Is.EqualTo(target), definition.Id.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace()
        {
            foreach (var definition in WukCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense &&
                    action.DefenseResolution == PrintedDefenseResolution.Value)))
            {
                var committed = definition.Id.Equals(WukGreenFaces.TossAway)
                    ? WukGreenFaces.MightyThrow : WukGreenFaces.TossAway;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk, new[] { definition.Id, committed });
                var root = Root();
                var runtime = root.Runtime;
                var wuk = Wuk(runtime);
                yield return WaspAttacksWuk(root, committed);
                Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)), Is.True, definition.Id.Value);
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, definition.Id.Value);
                Assert.That(Card(runtime, wuk, definition.Id).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile), definition.Id.Value);
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Session continuity, lifecycle and components
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator APausedTrampleDiscardRestoresAtWaspsQuestion()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                new[] { WukRedFaces.Trample, WukCardContent.MysticSaplings });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            yield return BeginWuk(root, WukRedFaces.Trample, ActionKind.Movement);
            yield return WalkAndStop(root, TwoAwaySpace);
            var asked = runtime.PendingEffectChoice;
            Assert.That(asked?.ChooserId ?? default, Is.EqualTo(wasp), Remaining(runtime));
            var before = HandOfCount(runtime, wasp);
            var pause = runtime.CaptureSnapshot();
            yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance, "Wasp's hand.");
            Assert.That(HandOfCount(runtime, wasp), Is.EqualTo(before - 1));
            Assert.That(runtime.RestoreSnapshot(pause), Is.True);
            yield return null;
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked.NodeId),
                "The restored match waits at the same question, the walk read from the board. " +
                Remaining(runtime));
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance, "Wasp's hand.");
            Assert.That(HandOfCount(runtime, wasp), Is.EqualTo(before - 1), "The restored discard resolves.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator ASaplingsPlacementPauseUndoesToItsRetainedPrefixAndReplaysExactly()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                new[] { WukCardContent.MysticSaplings, WukCardContent.TreeSlam }, wasp: FarSpace);
            var root = Root();
            var runtime = root.Runtime;
            var wuk = Wuk(runtime);
            yield return BeginWuk(root, WukCardContent.MysticSaplings, ActionKind.Skill);
            var first = runtime.PendingEffectChoice.NodeId;
            var pause = runtime.CaptureSnapshot();
            yield return PickSpace(root, space => HexDistanceQuery.Calculate(WukSpace, space) == 1,
                "The first Tree.");
            Assert.That(Trees(runtime), Has.Length.EqualTo(1));
            var planted = Trees(runtime)[0];
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
            Assert.That(Trees(runtime), Is.Empty);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause), "The retained prefix is the same pause.");
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;
            yield return PickSpace(root, space => space.Equals(planted), "The same space again.");
            Assert.That(Trees(runtime), Is.EqualTo(new[] { planted }));
            Assert.That(SpaceOf(runtime, wuk), Is.EqualTo(WukSpace));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator TreesOutlastWuksDefeatAndTheRoundEndBeforeARestart()
        {
            // Two cards: Wuk holds nothing after turn 2, so he is not asked to respawn inside the round.
            yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                new[] { WukCardContent.MysticSaplings, WukCardContent.TreeSlam });
            var root = Root();
            var runtime = root.Runtime;
            var wuk = Wuk(runtime);
            yield return PlantTrees(root, 2);
            var planted = Trees(runtime);
            yield return EndTurnsUntil(root, 2);
            yield return WaspAttacksWuk(root, WukCardContent.TreeSlam);
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(OnBoard(runtime, wuk), Is.False, "Wasp defeats Wuk. " + Remaining(runtime));
            Assert.That(Trees(runtime), Is.EquivalentTo(planted), "The Trees stand through his defeat.");
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
                else yield return NextRoundDecision(root);
            }

            Assert.That(runtime.RoundIndex, Is.EqualTo(round + 1));
            Assert.That(Trees(runtime), Is.EquivalentTo(planted),
                "WU-Q2: Mystic Saplings' Trees are not removed at the end of the round.");
            Assert.That(DrawnTrees(), Is.EqualTo(planted.Length), "The rebuilt board still draws them.");
            CommitSoTheNamedSeatActs(runtime, wuk, WukCardContent.TreeSlam);
            yield return YieldUntilTheNamedSeatActs(runtime, wuk);
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
            Assert.That(SpaceOf(runtime, wuk), Is.EqualTo(respawn));
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "RESET"));
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(SpaceOf(runtime, wuk), Is.EqualTo(WukSpace), "Restart reconstructs the staged board.");
            Assert.That(Trees(runtime), Is.Empty);
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator StagedWukSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWuk);
            var root = Root();
            var wuk = Wuk(root.Runtime);
            var view = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(value => value.Occupancy.HeroId.Equals(wuk));
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo("wuk"));
            Assert.That(view.HasTokenVisual, Is.True);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(wuk));
            Healthy(root.Runtime);
        }

        [UnityTest]
        public IEnumerator EveryWukFaceKeepsTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWuk);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Wuk", WukCardContent.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator WukLevelUpAddsTheChosenCardAndShowsItsSiblingItem()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedWuk,
                new[] { WukCardContent.TreeSlam, WukCardContent.MysticSaplings }, coins: 0);
            var root = Root();
            var runtime = root.Runtime;
            var wuk = Wuk(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(wuk));
                budget++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind,
                Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .First(option => option.Card != null && option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(wuk));
            var previous = before.ItemBonuses.Where(value => value.Key == upgrade.ItemIcon)
                .Select(value => value.Value).SingleOrDefault();
            UiPlayer.EnsureVisible(UpgradeButton(root, upgrade));
            yield return null;
            UiPlayer.Click(UpgradeButton(root, upgrade));
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(upgrade.Card.CardInstanceId))
                .State.Location, Is.EqualTo(CardLocation.Hand));
            for (var budget = 0; budget < 50 && runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
                yield return NextRoundDecision(root);
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(wuk));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value,
                Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero =>
                hero.HeroId.Equals(wuk)).ItemBonuses, Is.Not.Empty);
            Healthy(runtime);
        }
    }
}
