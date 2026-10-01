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
    /// Milestone 8.112h. Real pointer and control journeys for Mrak the Rockshaper, on boards measured
    /// headlessly before any was written: Mrak on (-3, 1), whose south neighbour (-3, 2) touches two
    /// terrain hexes, so "adjacent to terrain" is satisfiable without a rock; and, for Fissure's short
    /// line, Mrak on (-1, 0) facing Wasp on (0, 0), where the line runs to the board edge through two
    /// empty spaces. Every journey asserts what changed on the board or in a hand.
    /// </summary>
    public sealed class MrakUiJourneyPlayModeTests
    {
        private static readonly HexCoordinate MrakSpace = new HexCoordinate(-3, 1);
        private static readonly HexCoordinate AdjacentSpace = new HexCoordinate(-2, 1);
        private static readonly HexCoordinate BesideTerrainSpace = new HexCoordinate(-3, 2);
        private static readonly HexCoordinate TwoAwaySpace = new HexCoordinate(-1, 1);
        private static readonly HexCoordinate FarSpace = new HexCoordinate(0, 1);
        private static readonly MinionId StagedMinion = new MinionId("mrak-journey-minion");

        internal static HeroId Mrak(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "mrak").HeroId;

        private static HeroId HeroOf(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, string content) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == content).HeroId;

        private static HexCoordinate SpaceOf(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Projection.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate;

        private static bool OnBoard(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Projection.Heroes.Any(value => value.HeroId.Equals(hero));

        private static HexCoordinate[] Rocks(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Projection.PlacedTokens
                .Where(token => token.Definition.Id.Equals(MrakComponentContent.RockId))
                .Select(token => token.Coordinate).ToArray();

        private static int DrawnRocks() =>
            Object.FindObjectsByType<PlacedTokenView>(FindObjectsSortMode.None)
                .Count(view => view.Token != null &&
                    view.Token.Definition.Id.Equals(MrakComponentContent.RockId));

        private static CardInstance Card(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId owner,
            CardDefinitionId face) =>
            runtime.Cards.Single(card => card.OwnerId.Equals(owner) &&
                card.DefinitionReference.DefinitionId.Equals(face));

        private static int HandOfCount(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId owner) =>
            runtime.Cards.Count(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);

        /// <summary>
        /// Loads <paramref name="scene"/>, then reloads it staged: Mrak on <paramref name="mrak"/>
        /// holding <paramref name="hand"/>, Wasp (and Arien where the board deals her) where asked,
        /// and optionally one melee minion of Mrak's side. Minions standing where a staged piece goes
        /// are cleared.
        /// </summary>
        internal static IEnumerator Stage(SceneCardContent scene, CardDefinitionId[] hand,
            HexCoordinate? mrak = null, HexCoordinate? wasp = null, CardDefinitionId[] waspHand = null,
            HexCoordinate? arien = null, HexCoordinate? mraksMinion = null, int? coins = null)
        {
            yield return LoadStagedScene(scene);
            var runtime = Root().Runtime;
            var mrakId = Mrak(runtime);
            var waspId = HeroOf(runtime, "wasp");
            var mrakAt = mrak ?? MrakSpace;
            var waspAt = wasp ?? AdjacentSpace;
            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(mrakId, mrakAt, coins: coins, hand: hand),
                new SandboxHeroStaging(waspId, waspAt, hand: waspHand ?? new[]
                {
                    WaspCardContent.MagneticDagger, WaspCardContent.StopProjectiles,
                    WaspCardContent.Shock, WaspCardContent.LiftUp
                })
            };
            var occupied = new List<HexCoordinate> { mrakAt, waspAt };
            if (runtime.Setup.Heroes.Any(hero => hero.ContentId.Value == "arien"))
            {
                var arienAt = arien ?? new HexCoordinate(-4, 4);
                heroes.Add(new SandboxHeroStaging(HeroOf(runtime, "arien"), arienAt));
                occupied.Add(arienAt);
            }

            if (mraksMinion.HasValue) occupied.Add(mraksMinion.Value);
            var mrakSide = runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(mrakId)).TeamSide;
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: heroes,
                spawnedMinions: mraksMinion.HasValue
                    ? new[]
                    {
                        new SandboxMinionStaging(StagedMinion, mrakSide, StandardMinionType.Melee,
                            mraksMinion.Value)
                    }
                    : null,
                removedMinions: runtime.ReadModel.Minions
                    .Where(value => occupied.Contains(value.Coordinate))
                    .Select(value => value.MinionId));
            yield return LoadStagedScene(scene);
        }

        /// <summary>Commits the face this turn and starts its action of <paramref name="kind"/> through the tray control.</summary>
        internal static IEnumerator BeginMrak(MatchSceneCompositionRoot root, CardDefinitionId face,
            ActionKind kind)
        {
            var runtime = root.Runtime;
            var mrak = Mrak(runtime);
            yield return CommitEverySeat(root, mrak, NameOf(runtime, face));
            yield return WaitForActor(root, mrak);
            yield return PointTrayAt(root, mrak);
            yield return StartActionThroughControl(root, kind);
        }

        /// <summary>Picks the one candidate <paramref name="which"/> admits, through the board or the window.</summary>
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

        /// <summary>Answers a printed distance question by its window control, as a player presses it.</summary>
        private static IEnumerator PickDistance(MatchSceneCompositionRoot root, int distance)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, Remaining(runtime));
            var node = choice.NodeId;
            var candidate = choice.Candidates.Single(value =>
                value.Value.Kind == EffectValueKind.Integer && value.Value.IntegerValue == distance);
            var control = UiPlayer.DecisionControl(runtime, command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.Equals(candidate));
            Assert.That(control, Is.Not.Null, "No window control for distance " + distance + ". " +
                UiPlayer.Describe(root.Hud));
            UiPlayer.Click(UiPlayer.Button(root.Hud, control.Label));
            yield return null;
            yield return ConfirmIfStillStanding(root, node);
            yield return null;
        }

        /// <summary>Wasp attacks Mrak with Magnetic Dagger this turn and waits at his Defense.</summary>
        private static IEnumerator WaspAttacksMrak(MatchSceneCompositionRoot root,
            CardDefinitionId mrakCommits)
        {
            var runtime = root.Runtime;
            var mrak = Mrak(runtime);
            var wasp = HeroOf(runtime, "wasp");
            yield return PointTrayAt(root, mrak);
            yield return CommitThroughTray(root, NameOf(runtime, mrakCommits));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.MagneticDagger));
            yield return YieldUntilTheNamedSeatActs(runtime, wasp);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(mrak).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
        }

        /// <summary>Turn 1: the Carapace's primary Movement raises its This round grant; the turn ends unmoved.</summary>
        private static IEnumerator PlayCarapace(MatchSceneCompositionRoot root, CardDefinitionId face)
        {
            var runtime = root.Runtime;
            var mrak = Mrak(runtime);
            yield return BeginMrak(root, face, ActionKind.Movement);
            // Rock Solid first offers "You may retrieve a discarded card".
            if (runtime.PendingEffectChoice != null) yield return May(root, false);
            var carapace = Card(runtime, mrak, face);
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds.Contains(carapace.Id), Is.True,
                face.Value + " raises its This round grant before the movement. " + Remaining(runtime));
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            yield return EndTurnsUntil(root, 2);
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds.Contains(carapace.Id), Is.True,
                face.Value + "'s grant still stands on the next turn.");
        }

        // -------------------------------------------------------------------------------------------
        // Red
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator SeismicSkillsMakeTheEnemyBesideTerrainDiscard()
        {
            foreach (var face in new[] { MrakRedFaces.SeismicSlam, MrakRedFaces.SeismicAssault })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                    new[] { face, MrakCardContent.Fissure }, wasp: BesideTerrainSpace);
                var root = Root();
                var runtime = root.Runtime;
                var wasp = HeroOf(runtime, "wasp");
                yield return BeginMrak(root, face, ActionKind.Skill);
                var before = HandOfCount(runtime, wasp);
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero &&
                    value.HeroId.Equals(wasp), face.Value + " asks for the enemy hero beside terrain.");
                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(wasp),
                    face.Value + ": Wasp chooses the card he discards. " + Remaining(runtime));
                yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance,
                    face.Value + " offers Wasp's hand.");
                Assert.That(HandOfCount(runtime, wasp), Is.EqualTo(before - 1), face.Value);
                Assert.That(OnBoard(runtime, wasp), Is.True, face.Value + ": a discard answers it.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EpicentreRepeatsOnTheOtherEnemyHeroBesideTerrain()
        {
            yield return Stage(SceneCardContent.VerifiedStagedMrakVersusTwo,
                new[] { MrakRedFaces.Epicentre, MrakCardContent.Fissure }, wasp: BesideTerrainSpace);
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            var arien = HeroOf(runtime, "arien");
            yield return BeginMrak(root, MrakRedFaces.Epicentre, ActionKind.Skill);
            var arienHand = HandOfCount(runtime, arien);
            yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "Epicentre names Wasp first.");
            yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance, "Wasp discards.");
            // Milestone 9.1, ruling W1: "Repeat once" prints no "may", so the repeat follows with no
            // question - the next thing asked is the repeat's own target.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Epicentre repeats on a different target. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.DeclineInvocation, Is.Null,
                "W1: the repeat is not Mrak's to decline.");
            Assert.That(runtime.PendingEffectChoice.Candidates.Any(value =>
                value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(wasp)), Is.False,
                "The repeat excludes the hero already chosen.");
            yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(arien),
                "The repeat names Arien, the other enemy beside terrain.");
            yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance, "Arien discards.");
            Assert.That(HandOfCount(runtime, arien), Is.EqualTo(arienHand - 1));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryAttackDefeatsTheAdjacentHeroFromItsOwnControl()
        {
            foreach (var definition in MrakCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Attack &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                    new[] { definition.Id, MrakCardContent.Fissure });
                var root = Root();
                var runtime = root.Runtime;
                var wasp = HeroOf(runtime, "wasp");
                Assert.That(definition.ActionDefinitions.Count(action => action.Kind == ActionKind.Attack),
                    Is.EqualTo(1), definition.Id.Value + " prints one Attack, its secondary.");
                yield return BeginMrak(root, definition.Id, ActionKind.Attack);
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                    definition.Id.Value + " starts its printed secondary Attack. " + Remaining(runtime));
                yield return Target(root, wasp);
                Assert.That(runtime.PendingDefenseChoice?.ChooserId ?? default, Is.EqualTo(wasp),
                    definition.Id.Value + " reaches Wasp's Defense. " + Remaining(runtime));
                yield return TakeTheHit(root);
                Assert.That(OnBoard(runtime, wasp), Is.False,
                    definition.Id.Value + "'s undefended secondary Attack defeats Wasp.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator CarapacesLetTheResolvedCardAnswerTheDiscardShockDemands()
        {
            foreach (var face in new[] { MrakRedFaces.StoneCarapace, MrakRedFaces.RockSolid })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                    new[] { face, MrakGreenFaces.TreacherousGround, MrakCardContent.Fissure },
                    wasp: TwoAwaySpace,
                    waspHand: new[] { WaspCardContent.Shock, WaspCardContent.StopProjectiles, WaspCardContent.LiftUp },
                    mraksMinion: new HexCoordinate(-1, 2));
                var root = Root();
                var runtime = root.Runtime;
                var mrak = Mrak(runtime);
                var wasp = HeroOf(runtime, "wasp");
                yield return PlayCarapace(root, face);
                yield return PointTrayAt(root, wasp);
                yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.Shock));
                yield return PointTrayAt(root, mrak);
                yield return CommitThroughTray(root, NameOf(runtime, MrakGreenFaces.TreacherousGround));
                yield return YieldUntilTheNamedSeatActs(runtime, wasp);
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, MinionView(StagedMinion).transform.position), Is.True);
                yield return null;
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(mrak),
                    "Shock names Mrak, two away.");
                var carapace = Card(runtime, mrak, face);
                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(mrak), Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice.Candidates.Any(value =>
                    value.Value.Kind == EffectValueKind.CardInstance &&
                    value.Value.CardInstanceId.Equals(carapace.Id)), Is.True,
                    face.Value + " is offered from where it rests. " + Remaining(runtime));
                yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance &&
                    value.CardInstanceId.Equals(carapace.Id), face.Value);
                Assert.That(Card(runtime, mrak, face).State.Location, Is.EqualTo(CardLocation.DiscardPile), face.Value);
                Assert.That(Card(runtime, mrak, MrakCardContent.Fissure).State.Location,
                    Is.EqualTo(CardLocation.Hand), face.Value + ": the hand card is kept.");
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds.Contains(carapace.Id), Is.False,
                    face.Value + ": p.17 cancels the grant once its card changed state.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator CarapacesDefendFromWhereTheyRestAsIfInHand()
        {
            foreach (var face in new[] { MrakRedFaces.StoneCarapace, MrakRedFaces.RockSolid })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                    new[] { face, MrakGreenFaces.TreacherousGround, MrakCardContent.Fissure });
                var root = Root();
                var runtime = root.Runtime;
                var mrak = Mrak(runtime);
                yield return PlayCarapace(root, face);
                yield return WaspAttacksMrak(root, MrakGreenFaces.TreacherousGround);
                Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(face)), Is.True,
                    face.Value + " offers its own Defense from the turn area.");
                yield return ClickDefenseFace(root, face);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, face.Value);
                Assert.That(Card(runtime, mrak, face).State.Location, Is.EqualTo(CardLocation.DiscardPile), face.Value);
                Assert.That(Card(runtime, mrak, MrakCardContent.Fissure).State.Location,
                    Is.EqualTo(CardLocation.Hand), face.Value);
                Assert.That(OnBoard(runtime, mrak), Is.True, face.Value + "'s Defense 6 holds.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator RockSolidRetrievesTheCardMrakSpentDefending()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                new[] { MrakRedFaces.RockSolid, MrakCardContent.Fissure, MrakGreenFaces.TreacherousGround });
            var root = Root();
            var runtime = root.Runtime;
            var mrak = Mrak(runtime);
            yield return WaspAttacksMrak(root, MrakRedFaces.RockSolid);
            yield return ClickDefenseFace(root, MrakCardContent.Fissure);
            var fissure = Card(runtime, mrak, MrakCardContent.Fissure);
            Assert.That(fissure.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            yield return WaitForActor(root, mrak);
            yield return PointTrayAt(root, mrak);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Rock Solid offers its retrieve before the movement. " + Remaining(runtime));
            yield return May(root, true);
            yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance &&
                value.CardInstanceId.Equals(fissure.Id), "Rock Solid offers the spent Fissure.");
            Assert.That(Card(runtime, mrak, MrakCardContent.Fissure).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds.Contains(Card(runtime, mrak, MrakRedFaces.RockSolid).Id),
                Is.True, "and raises its This round grant.");
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Gold and Silver
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator StoneGripAsksAboutItsTieAndPlacesThreeRocksBesideTheTarget()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                new[] { MrakCardContent.StoneGrip, MrakCardContent.Fissure }, wasp: new HexCoordinate(-1, 0));
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            yield return BeginMrak(root, MrakCardContent.StoneGrip, ActionKind.Skill);
            yield return Target(root, wasp);
            Assert.That(runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate),
                Is.EquivalentTo(new[] { new HexCoordinate(-2, 0), new HexCoordinate(-2, 1) }),
                "MR-Q3: the tie among the next-farthest spaces is Mrak's. " + Remaining(runtime));
            yield return PickSpace(root, _ => true, "Stone Grip's tie.");
            Assert.That(Rocks(runtime), Has.Length.EqualTo(3), Remaining(runtime));
            Assert.That(Rocks(runtime).All(space => HexDistanceQuery.Calculate(space, new HexCoordinate(-1, 0)) == 1),
                Is.True, "Every rock stands beside the target.");
            Assert.That(DrawnRocks(), Is.EqualTo(3));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator FissureTakesBackTwoOfThreeRocksAndLinesThemToTheBoardEdge()
        {
            var mrakAt = new HexCoordinate(-1, 0);
            var waspAt = new HexCoordinate(0, 0);
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                new[] { MrakCardContent.StoneGrip, MrakCardContent.Fissure, MrakGreenFaces.TreacherousGround },
                mrak: mrakAt, wasp: waspAt);
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            yield return BeginMrak(root, MrakCardContent.StoneGrip, ActionKind.Skill);
            yield return Target(root, wasp);
            Assert.That(Rocks(runtime), Is.EquivalentTo(new[]
            {
                new HexCoordinate(0, 1), new HexCoordinate(1, -1), new HexCoordinate(1, 0)
            }), Remaining(runtime));
            yield return EndTurnsUntil(root, 2);
            yield return BeginMrak(root, MrakCardContent.Fissure, ActionKind.Attack);
            yield return Target(root, wasp);
            yield return TakeTheHit(root);
            Assert.That(OnBoard(runtime, wasp), Is.False);
            var reclaim = runtime.PendingEffectChoice;
            Assert.That(reclaim, Is.Not.Null, "p.18: the empty pile takes back rocks first. " + Remaining(runtime));
            Assert.That(reclaim.MinimumSelections, Is.EqualTo(2), Remaining(runtime));
            Assert.That(reclaim.MaximumSelections, Is.EqualTo(2));
            // The multi-pick is completed by the second click, never skipped (carried lesson 5).
            yield return Space(root, new HexCoordinate(0, 1));
            Assert.That(runtime.PendingEffectChoice?.SelectedCandidates.Count ?? -1, Is.EqualTo(1),
                "One of two chosen; the reclaim still stands. " + Remaining(runtime));
            yield return Space(root, new HexCoordinate(1, -1));
            Assert.That(Rocks(runtime), Is.EquivalentTo(new[]
            {
                new HexCoordinate(1, 0), waspAt, new HexCoordinate(2, 0)
            }), "The unchosen rock stays; the line fills its two empty spaces. " + Remaining(runtime));
            Assert.That(DrawnRocks(), Is.EqualTo(3));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Blue
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator BouldersPushTheAdjacentEnemyTwoSpacesAndFollowAlongThePush()
        {
            foreach (var face in new[] { MrakBlueFaces.BoulderRush, MrakBlueFaces.BoulderBlitz, MrakBlueFaces.Boulderdozer })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak, new[] { face, MrakCardContent.Fissure });
                var root = Root();
                var runtime = root.Runtime;
                var mrak = Mrak(runtime);
                var wasp = HeroOf(runtime, "wasp");
                yield return BeginMrak(root, face, ActionKind.Skill);
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(AdjacentSpace), face.Value + " offers Wasp beside Mrak.");
                Assert.That(runtime.PendingEffectChoice?.Candidates.Count(value =>
                    value.Value.Kind == EffectValueKind.Integer) ?? 0, Is.GreaterThan(1),
                    face.Value + " asks how far. " + Remaining(runtime));
                yield return PickDistance(root, 2);
                Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(FarSpace), face.Value + " pushed Wasp two spaces.");
                Assert.That(runtime.PendingEffectChoice?.DeclineInvocation, Is.Not.Null,
                    face.Value + "'s follow is a may. " + Remaining(runtime));
                yield return PickSpace(root, space => space.Equals(TwoAwaySpace), face.Value + "'s follow.");
                Assert.That(SpaceOf(runtime, mrak), Is.EqualTo(TwoAwaySpace), face.Value + ": Mrak followed the push.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator StompsMoveAUnitBesideTerrainAndLeaveARockWhereItStood()
        {
            foreach (var face in new[] { MrakBlueFaces.StompingStep, MrakBlueFaces.GroundShaker })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                    new[] { face, MrakCardContent.Fissure }, wasp: BesideTerrainSpace);
                var root = Root();
                var runtime = root.Runtime;
                var wasp = HeroOf(runtime, "wasp");
                yield return BeginMrak(root, face, ActionKind.Skill);
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    face.Value + " offers Wasp, beside terrain.");
                yield return PickSpace(root, _ => true, face.Value + " steps Wasp one space.");
                Assert.That(SpaceOf(runtime, wasp), Is.Not.EqualTo(BesideTerrainSpace), face.Value);
                Assert.That(Rocks(runtime), Is.EqualTo(new[] { BesideTerrainSpace }),
                    face.Value + " leaves a rock where Wasp stood. " + Remaining(runtime));
                if (face.Equals(MrakBlueFaces.GroundShaker))
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null, "Ground Shaker may repeat. " + Remaining(runtime));
                    yield return May(root, true);
                    Assert.That(runtime.PendingEffectChoice.Candidates.Any(value =>
                        value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(wasp)), Is.False,
                        "on a different target");
                    var minion = runtime.PendingEffectChoice.Candidates.First(value =>
                        value.Value.Kind == EffectValueKind.Minion).Value.MinionId;
                    var stood = runtime.Projection.Minions.Single(value => value.MinionId.Equals(minion)).Coordinate;
                    yield return PickWhere(root, value => value.Kind == EffectValueKind.Minion &&
                        value.MinionId.Equals(minion), "Ground Shaker's second target.");
                    yield return PickSpace(root, _ => true, "The second step.");
                    Assert.That(Rocks(runtime), Is.EquivalentTo(new[] { BesideTerrainSpace, stood }),
                        "The repeat leaves its own rock. " + Remaining(runtime));
                }

                Assert.That(DrawnRocks(), Is.EqualTo(Rocks(runtime).Length));
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Green
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ShiftsMayMoveAUnitInRangeOneSpaceToBesideTerrain()
        {
            foreach (var face in new[] { MrakGreenFaces.TreacherousGround, MrakGreenFaces.Rockslide, MrakGreenFaces.Avalanche })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                    new[] { face, MrakCardContent.Fissure }, wasp: TwoAwaySpace);
                var root = Root();
                var runtime = root.Runtime;
                var wasp = HeroOf(runtime, "wasp");
                yield return BeginMrak(root, face, ActionKind.Skill);
                Assert.That(runtime.PendingEffectChoice?.DeclineInvocation, Is.Not.Null,
                    face.Value + " is a may. " + Remaining(runtime));
                yield return May(root, true);
                yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    face.Value + " offers Wasp in range.");
                Assert.That(runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate),
                    Is.EquivalentTo(new[] { new HexCoordinate(-2, 2) }),
                    face.Value + ": the one step that ends beside terrain. " + Remaining(runtime));
                yield return PickSpace(root, _ => true, face.Value);
                Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(new HexCoordinate(-2, 2)), face.Value);
                if (face.Equals(MrakGreenFaces.Avalanche))
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null, "Avalanche may repeat. " + Remaining(runtime));
                    yield return May(root, true);
                    yield return May(root, true);
                    yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                        "Avalanche may repeat on the same unit.");
                    yield return PickSpace(root, _ => true, "Avalanche's second step.");
                    Assert.That(SpaceOf(runtime, wasp), Is.Not.EqualTo(new HexCoordinate(-2, 2)),
                        "The repeat moved Wasp again.");
                }

                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator RollsCrossObstaclesInAStraightLineToAChosenSpace()
        {
            foreach (var face in new[] { MrakGreenFaces.RollingStone, MrakGreenFaces.StrollingStone })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak, new[] { face, MrakCardContent.Fissure });
                var root = Root();
                var runtime = root.Runtime;
                var mrak = Mrak(runtime);
                yield return BeginMrak(root, face, ActionKind.Skill);
                Assert.That(runtime.PendingEffectChoice?.DeclineInvocation, Is.Not.Null, face.Value + ": standing still is legal.");
                // (-4, 1) lies past no obstacle; (-5, 3) is reached only across the minion on (-4, 2).
                var across = new HexCoordinate(-5, 3);
                Assert.That(runtime.PendingEffectChoice.Candidates.Any(value =>
                    value.Value.Kind == EffectValueKind.Coordinate && value.Value.Coordinate.Equals(across)),
                    Is.True, face.Value + " rolls over the minion beside Mrak. " + Remaining(runtime));
                yield return Space(root, across);
                Assert.That(SpaceOf(runtime, mrak), Is.EqualTo(across), face.Value);
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Ultimate
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator RockAndAHardPlaceMakesTheHeroBesideTheNewRockDiscard()
        {
            yield return Stage(SceneCardContent.VerifiedTierThreeEngagedStagedMrak,
                new[] { MrakBlueFaces.StompingStep, MrakCardContent.Fissure }, wasp: BesideTerrainSpace);
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            Assert.That(UiPlayer.ItemStripDescendants(root.Hud).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "Rock and a Hard Place stands face up on Mrak's rail.");
            yield return BeginMrak(root, MrakBlueFaces.StompingStep, ActionKind.Skill);
            yield return PickWhere(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "Stomping Step names Wasp.");
            var before = HandOfCount(runtime, wasp);
            yield return PickSpace(root, _ => true, "Wasp's step.");
            Assert.That(Rocks(runtime), Is.EqualTo(new[] { BesideTerrainSpace }));
            Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(wasp),
                "The rock beside Wasp fires the Ultimate; Wasp chooses his discard. " + Remaining(runtime));
            yield return PickWhere(root, value => value.Kind == EffectValueKind.CardInstance, "Wasp's hand.");
            Assert.That(HandOfCount(runtime, wasp), Is.EqualTo(before - 1));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Every face's ribbon
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesMrakThroughItsCardControl()
        {
            foreach (var definition in MrakCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                var other = definition.Id.Equals(MrakCardContent.Fissure)
                    ? MrakCardContent.StoneGrip : MrakCardContent.Fissure;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak, new[] { definition.Id, other });
                var root = Root();
                var runtime = root.Runtime;
                var mrak = Mrak(runtime);
                yield return BeginMrak(root, definition.Id, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, definition.Id.Value);
                var target = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    runtime.PendingMovementChoice.LegalDestinations
                        .Where(value => value.Cost == 1).Select(value => value.Destination).ToArray());
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, Tile(target).transform.position), Is.True);
                yield return null;
                Assert.That(SpaceOf(runtime, mrak), Is.EqualTo(target), definition.Id.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace()
        {
            foreach (var definition in MrakCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense &&
                    action.DefenseResolution == PrintedDefenseResolution.Value)))
            {
                // A committed face whose printed name no other tray card contains ("ROLLING STONE"
                // is inside "STROLLING STONE").
                var committed = definition.Id.Equals(MrakGreenFaces.TreacherousGround)
                    ? MrakGreenFaces.Avalanche : MrakGreenFaces.TreacherousGround;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak, new[] { definition.Id, committed });
                var root = Root();
                var runtime = root.Runtime;
                var mrak = Mrak(runtime);
                yield return WaspAttacksMrak(root, committed);
                Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)), Is.True, definition.Id.Value);
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, definition.Id.Value);
                Assert.That(Card(runtime, mrak, definition.Id).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile), definition.Id.Value);
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Session continuity, lifecycle and components
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator APausedSecondaryAttackRestoresAtWaspsDefense()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                new[] { MrakRedFaces.SeismicSlam, MrakCardContent.Fissure });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            yield return BeginMrak(root, MrakRedFaces.SeismicSlam, ActionKind.Attack);
            yield return Target(root, wasp);
            var defense = runtime.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null, Remaining(runtime));
            var pause = runtime.CaptureSnapshot();
            yield return TakeTheHit(root);
            Assert.That(OnBoard(runtime, wasp), Is.False);
            Assert.That(runtime.RestoreSnapshot(pause), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice?.ResolutionId, Is.EqualTo(defense.ResolutionId),
                "The restored match waits at the same Defense. " + Remaining(runtime));
            Assert.That(OnBoard(runtime, wasp), Is.True);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            yield return TakeTheHit(root);
            Assert.That(OnBoard(runtime, wasp), Is.False, "The restored Attack still resolves.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator AShiftsDestinationPauseUndoesToItsRetainedPrefixAndReplaysExactly()
        {
            // Wasp stands far off, so the unit shifted is Mrak's own melee minion beside terrain: no
            // opponent is interacted with and the rewind is not walled.
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                new[] { MrakGreenFaces.TreacherousGround, MrakCardContent.Fissure }, wasp: FarSpace,
                mraksMinion: BesideTerrainSpace);
            var root = Root();
            var runtime = root.Runtime;
            yield return BeginMrak(root, MrakGreenFaces.TreacherousGround, ActionKind.Skill);
            yield return May(root, true);
            var select = runtime.PendingEffectChoice.NodeId;
            yield return PickWhere(root, value => value.Kind == EffectValueKind.Minion &&
                value.MinionId.Equals(StagedMinion), "Treacherous Ground offers Mrak's own minion.");
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.Not.EqualTo(select),
                "The step's destination is asked next. " + Remaining(runtime));
            var pause = runtime.CaptureSnapshot();
            OpenDirectorDrawer(root.Hud);
            yield return null;
            // One board click is two accepted commands (the select and its confirm), so the unit
            // question comes back empty after at most two presses.
            for (var press = 0; press < 3 && !(runtime.PendingEffectChoice != null &&
                runtime.PendingEffectChoice.NodeId.Equals(select) &&
                runtime.PendingEffectChoice.SelectedCandidates.Count == 0); press++)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(select),
                "Undo gives the unit question back and keeps the accepted may. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.SelectedCandidates, Is.Empty);
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;
            yield return PickWhere(root, value => value.Kind == EffectValueKind.Minion &&
                value.MinionId.Equals(StagedMinion), "The same answer again.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause),
                "Re-answering the retained prefix reconstructs the identical pause.");
            Assert.That(runtime.RestoreSnapshot(pause), Is.True);
            yield return null;
            yield return PickSpace(root, _ => true, "The minion's step.");
            Assert.That(runtime.Projection.Minions.Single(value => value.MinionId.Equals(StagedMinion)).Coordinate,
                Is.EqualTo(new HexCoordinate(-2, 2)), "The minion steps beside terrain.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator RocksOutlastMraksDefeatAndLeaveAtTheRoundEndBeforeARestart()
        {
            // Two cards: Mrak holds nothing after turn 2, so he is not asked to respawn inside the
            // round and the round runs to its end.
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                new[] { MrakCardContent.StoneGrip, MrakCardContent.Fissure });
            var root = Root();
            var runtime = root.Runtime;
            var mrak = Mrak(runtime);
            var wasp = HeroOf(runtime, "wasp");
            yield return BeginMrak(root, MrakCardContent.StoneGrip, ActionKind.Skill);
            yield return Target(root, wasp);
            Assert.That(Rocks(runtime), Has.Length.EqualTo(3), Remaining(runtime));
            yield return EndTurnsUntil(root, 2);
            yield return WaspAttacksMrak(root, MrakCardContent.Fissure);
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(OnBoard(runtime, mrak), Is.False, "Wasp defeats Mrak. " + Remaining(runtime));
            Assert.That(Rocks(runtime), Has.Length.EqualTo(3), "p.8: rocks stay through the placer's defeat.");
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
                    // Wasp's coins for defeating Mrak buy a level: answered in the window as the
                    // level-up journey presses it, one frame after the window is drawn.
                    yield return null;
                    var shown = UiPlayer.Descendants(root.Hud.RootElement)
                        .OfType<UnityEngine.UIElements.Button>()
                        .Where(element => element.name == "gd-upgrade-option" && UiPlayer.IsShown(element))
                        .ToArray();
                    Assert.That(shown, Is.Not.Empty, "The level-up window draws its upgrade options for " +
                        runtime.Projection.PendingSystemTransitionDescription.HeroId.Value + ". " +
                        UiPlayer.Describe(root.Hud));
                    UiPlayer.EnsureVisible(shown[0]);
                    yield return null;
                    UiPlayer.Click(shown[0]);
                    yield return null;
                }
                else yield return NextRoundDecision(root);
            }

            Assert.That(runtime.RoundIndex, Is.EqualTo(round + 1));
            Assert.That(Rocks(runtime), Is.Empty, "p.8/p.19: every rock leaves at the end of the round.");
            Assert.That(DrawnRocks(), Is.EqualTo(0), "The rebuilt board draws no rock.");
            CommitSoTheNamedSeatActs(runtime, mrak, MrakCardContent.Fissure);
            yield return YieldUntilTheNamedSeatActs(runtime, mrak);
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
            Assert.That(SpaceOf(runtime, mrak), Is.EqualTo(respawn));
            // The spotlight of the Defeat that sent him to respawn may still be entering (see
            // UiPlayer.WaitForControl); take the turn controls until the Director control returns.
            yield return UiPlayer.WaitForControl(root.Hud, "DIRECTOR");
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "RESET"));
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(SpaceOf(runtime, mrak), Is.EqualTo(MrakSpace), "Restart reconstructs the staged board.");
            Assert.That(Rocks(runtime), Is.Empty);
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator StagedMrakSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMrak);
            var root = Root();
            var mrak = Mrak(root.Runtime);
            var view = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(value => value.Occupancy.HeroId.Equals(mrak));
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo("mrak"));
            Assert.That(view.HasTokenVisual, Is.True);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(mrak));
            Healthy(root.Runtime);
        }

        [UnityTest]
        public IEnumerator EveryMrakFaceKeepsTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMrak);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Mrak", MrakCardContent.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator MrakLevelUpAddsTheChosenCardAndShowsItsSiblingItem()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedMrak,
                new[] { MrakCardContent.Fissure, MrakCardContent.StoneGrip }, coins: 0);
            var root = Root();
            var runtime = root.Runtime;
            var mrak = Mrak(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(mrak));
                budget++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind,
                Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .First(option => option.Card != null && option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(mrak));
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
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(mrak));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value,
                Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero =>
                hero.HeroId.Equals(mrak)).ItemBonuses, Is.Not.Empty);
            Healthy(runtime);
        }
    }
}
