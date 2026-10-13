using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using Object = UnityEngine.Object;
using T = GuardsDigital.Core.Cards.Verified.EmmittCardContent;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Milestone 9.50: Emmitt's configured, real-input player-path journeys.</summary>
    public sealed class EmmittUiJourneyPlayModeTests
    {
        private static readonly HexCoordinate EmmittSpace = new HexCoordinate(-3, 1);
        private static readonly HexCoordinate WaspBeside = new HexCoordinate(-2, 1);
        private static readonly HexCoordinate ArienBeside = new HexCoordinate(-3, 0);
        private static (HeroId Emmitt, HeroId Wasp, (MinionId Id, HexCoordinate At)[] Minions)? cached;

        private static HeroId Emmitt(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == T.ContentSetId).HeroId;

        private static HeroId Wasp(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == WaspCardContent.ContentSetId).HeroId;

        private static IEnumerator Stage(CardDefinitionId[] hand, HexCoordinate? waspAt = null,
            CardDefinitionId[] enemyHand = null)
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            if (!cached.HasValue)
            {
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedEmmitt);
                var plain = Root().Runtime;
                cached = (Emmitt(plain), Wasp(plain),
                    plain.ReadModel.Minions.Select(value => (value.MinionId, value.Coordinate)).ToArray());
            }

            var seats = cached.Value;
            var waspSpace = waspAt ?? WaspBeside;
            var occupied = new[] { EmmittSpace, waspSpace };
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(seats.Emmitt, EmmittSpace, hand: hand),
                    new SandboxHeroStaging(seats.Wasp, waspSpace, hand: enemyHand)
                },
                removedMinions: seats.Minions.Where(value => occupied.Contains(value.At))
                    .Select(value => value.Id));
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedEmmitt);
            Assert.That(Root().Runtime.Heroes.Single(hero => hero.HeroId.Equals(seats.Emmitt)).Coordinate,
                Is.EqualTo(EmmittSpace));
        }

        private static IEnumerator StageWithAlly(CardDefinitionId[] emmittHand,
            CardDefinitionId[] arienHand, CardDefinitionId[] waspHand)
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedStagedEmmittWithAlly);
            var plain = Root().Runtime;
            var emmitt = Emmitt(plain);
            var wasp = Wasp(plain);
            var arien = plain.Setup.Heroes.Single(hero => hero.ContentId.Value == "arien").HeroId;
            var occupied = new[] { EmmittSpace, WaspBeside, ArienBeside };
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(emmitt, EmmittSpace, hand: emmittHand),
                    new SandboxHeroStaging(wasp, WaspBeside, hand: waspHand),
                    new SandboxHeroStaging(arien, ArienBeside, hand: arienHand)
                },
                removedMinions: plain.ReadModel.Minions.Where(value =>
                    occupied.Contains(value.Coordinate)).Select(value => value.MinionId));
            yield return LoadStagedScene(SceneCardContent.VerifiedStagedEmmittWithAlly);
        }

        private static IEnumerator StageTierThree(CardDefinitionId[] hand,
            CardDefinitionId[] enemyHand)
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedEmmitt);
            var plain = Root().Runtime;
            var emmitt = Emmitt(plain);
            var wasp = Wasp(plain);
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(emmitt, EmmittSpace, hand: hand),
                    new SandboxHeroStaging(wasp, WaspBeside, hand: enemyHand)
                },
                removedMinions: plain.ReadModel.Minions.Where(value =>
                    value.Coordinate.Equals(EmmittSpace) ||
                    value.Coordinate.Equals(WaspBeside)).Select(value => value.MinionId));
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedEmmitt);
        }

        [UnityTest]
        public IEnumerator ReverseTimeAndTemporalPunchLineAttackFromTheirOwnFaces()
        {
            foreach (var face in new[]
                { T.ReverseTime, T.TemporalPunch, T.TemporalSlam, T.TemporalJudgement })
            {
            yield return Stage(new[] { face, T.TimeSnare },
                enemyHand: face.Equals(T.ReverseTime)
                    ? new[]
                        { WaspCardContent.Shock, WaspCardContent.Electrocute,
                            WaspCardContent.MagneticDagger }
                    : null);
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var wasp = Wasp(runtime);
            yield return CommitEverySeat(root, emmitt, NameOf(runtime, face));
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(wasp).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            var source = runtime.Cards.Single(card => card.OwnerId.Equals(emmitt) &&
                card.DefinitionReference.DefinitionId.Equals(face));
            if (!face.Equals(T.ReverseTime))
                Assert.That(runtime.IsActiveEffectSource(source.Id), Is.True,
                    "The Initiative substitution is active in the enemy's visible Defense window.");
            if (face.Equals(T.ReverseTime))
                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            else
                UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            yield return null;
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
            if (face.Equals(T.ReverseTime))
            {
                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(wasp)), Is.True,
                    "Her printed Defense preserves the following live turn.");
                Assert.That(runtime.IsActiveEffectSource(source.Id), Is.False,
                    "Next turn has not started yet; the inversion is dormant.");
                for (var budget = 0; budget < 16 && runtime.TurnIndex == 1; budget++)
                    yield return NextRoundDecision(root);
                Assert.That(runtime.RoundIndex, Is.EqualTo(1), Remaining(runtime));
                Assert.That(runtime.TurnIndex, Is.EqualTo(2), Remaining(runtime));
                yield return PointTrayAt(root, emmitt);
                yield return CommitThroughTray(root, NameOf(runtime, T.TimeSnare));
                yield return PointTrayAt(root, wasp);
                yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.MagneticDagger));
                Assert.That(runtime.IsActiveEffectSource(source.Id), Is.True,
                    "The next-turn Initiative inversion is active in the next committed turn.");
            }
            else
                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(wasp)), Is.False,
                    "The attack beats the declined defense and removes its target.");
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesEmmittThroughItsCardControl()
        {
            foreach (var definition in T.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                var other = definition.Id.Equals(T.TimeSnare) ? T.TemporalPunch : T.TimeSnare;
                yield return Stage(new[] { definition.Id, other });
                var root = Root();
                var runtime = root.Runtime;
                var emmitt = Emmitt(runtime);
                yield return CommitEverySeat(root, emmitt, NameOf(runtime, definition.Id));
                yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
                yield return PointTrayAt(root, emmitt);
                yield return StartActionThroughControl(root, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, definition.Id.Value + " " + Remaining(runtime));
                var step = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                    runtime.PendingMovementChoice.LegalDestinations.Where(value => value.Cost == 1)
                        .Select(value => value.Destination));
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, Tile(step).transform.position), Is.True);
                yield return null;
                Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(emmitt)).Coordinate,
                    Is.EqualTo(step), definition.Id.Value);
                Assert.That(HeroView(emmitt).Coordinate, Is.EqualTo(step), definition.Id.Value);
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                    Has.None.Contains("EngineFault"), definition.Id.Value);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersFromItsOwnFace()
        {
            foreach (var definition in T.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction)))
            {
                var committed = definition.Id.Equals(T.TimeSnare) ? T.TemporalPunch : T.TimeSnare;
                yield return Stage(new[] { definition.Id, committed });
                var root = Root();
                var runtime = root.Runtime;
                var emmitt = Emmitt(runtime);
                UiPlayer.EndAnyTurnInProgress(runtime);
                Commit(runtime, emmitt, committed);
                Commit(runtime, Wasp(runtime), WaspCardContent.MagneticDagger);
                yield return null;
                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(Wasp(runtime)), Remaining(runtime));
                yield return PointTrayAt(root, Wasp(runtime));
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, HeroView(emmitt).transform.position), Is.True);
                yield return null;
                Assert.That(runtime.PendingDefenseChoice?.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)), Is.True,
                    definition.Id.Value + " " + Remaining(runtime));
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, definition.Id.Value);
                Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(emmitt) &&
                    card.DefinitionReference.DefinitionId.Equals(definition.Id)).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile), definition.Id.Value);
            }
        }

        [UnityTest]
        public IEnumerator GlitchOnTheLiveBoardUsesItsPreparedOfficialFace()
        {
            yield return Stage(new[] { T.UnstableTimeline, T.TemporalPunch });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            yield return CommitEverySeat(root, emmitt, NameOf(runtime, T.UnstableTimeline));
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingEffectChoice.Candidates.Where(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(space), "place Glitch for art inspection");

            var officialFace = Resources.Load<Texture2D>("Private/Tokens/emmitt-glitch");
            Assert.That(officialFace, Is.Not.Null,
                "Prepare-TokenArtwork.ps1 -Hero emmitt must provide the private face.");
            var view = runtime.BoardView.TokenViews.Single(token =>
                token.gameObject.activeSelf && token.Token != null &&
                token.Token.Definition.Id.Equals(EmmittComponentContent.Glitch.Id));
            Assert.That(view.GetComponent<MeshRenderer>().sharedMaterial.mainTexture,
                Is.SameAs(officialFace));
            yield return UiPlayer.CaptureGameView("9-53", "emmitt-glitch-official-face");
        }

        [UnityTest]
        public IEnumerator UnstableTimelinePlacesSpacedGlitchesAndTheEnemyChoosesHisSwap()
        {
            yield return Stage(new[] { T.UnstableTimeline, T.TemporalPunch });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var wasp = Wasp(runtime);
            yield return CommitEverySeat(root, emmitt, NameOf(runtime, T.UnstableTimeline));
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var first = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingEffectChoice.Candidates.Where(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(first),
                "first Glitch");
            Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                token.Definition.Id.Equals(EmmittComponentContent.Glitch.Id)), Is.EqualTo(1));

            var second = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingEffectChoice.Candidates.Where(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate &&
                    HexDistanceQuery.Calculate(candidate.Value.Coordinate, first) >=
                    EmmittComponentContent.MinimumGlitchHexDistance)
                    .Select(candidate => candidate.Value.Coordinate));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(second),
                "second Glitch");
            Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                token.Definition.Id.Equals(EmmittComponentContent.Glitch.Id)), Is.EqualTo(2));
            Assert.That(runtime.BoardView.TokenViews.Count(view => view.gameObject.activeSelf),
                Is.GreaterThanOrEqualTo(2));
            yield return UiPlayer.CaptureGameView("9-50", "emmitt-glitches-and-choice");

            Assert.That(runtime.PendingEffectChoice.Candidates.Any(candidate =>
                candidate.Value.Kind == EffectValueKind.Hero &&
                candidate.Value.HeroId.Equals(wasp)), Is.True, Remaining(runtime));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "enemy chooses Glitch");
            var destination = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            Assert.That(new[] { first, second }, Does.Contain(destination));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(destination), "enemy's token selection");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(emmitt)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator UnstableTimelineDefensePlacesThreeGlitchesFromItsWholeFace()
        {
            yield return Stage(new[] { T.UnstableTimeline, T.TimeSnare },
                enemyHand: new[] { WaspCardContent.MagneticDagger });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var wasp = Wasp(runtime);
            Commit(runtime, emmitt, T.TimeSnare);
            Commit(runtime, wasp, WaspCardContent.MagneticDagger);
            yield return null;
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(wasp));
            yield return PointTrayAt(root, wasp);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(emmitt).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice?.LegalDefenseOffers.Any(offer =>
                offer.SourceDefinitionReference.DefinitionId.Equals(T.UnstableTimeline)), Is.True);
            yield return ClickDefenseFace(root, T.UnstableTimeline);
            for (var index = 1; index <= 3; index++)
            {
                var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                    runtime.PendingEffectChoice.Candidates.Where(candidate =>
                        candidate.Value.Kind == EffectValueKind.Coordinate)
                        .Select(candidate => candidate.Value.Coordinate)
                        .OrderBy(candidate => HexDistanceQuery.Calculate(candidate, EmmittSpace)));
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(space), "defense Glitch " + index);
                Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                    token.Definition.Id.Equals(EmmittComponentContent.Glitch.Id)), Is.EqualTo(index));
            }
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "enemy chooses from all three defense Glitches");
            var destination = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate)
                    .OrderBy(candidate => HexDistanceQuery.Calculate(candidate, EmmittSpace)));
            var tokenChoice = runtime.PendingEffectChoice;
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(destination), "enemy's defense Glitch choice");
            yield return ConfirmIfStillStanding(root, tokenChoice.NodeId);
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(emmitt)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(runtime.DiagnosticEvents.Last(value =>
                    value.DisplayText.Contains("Action.DefenseTotalCalculated")).DisplayText,
                Does.Contain("defense=5"),
                "p.14 step 5 totals the Defense after the Glitch swap changes adjacent minions.");
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator FlashbackAndDejaVuPlaceGlitchesThenLetTheEnemyChooseTheirSwap()
        {
            foreach (var face in new[] { T.Flashback, T.DejaVu })
            {
                yield return Stage(new[] { face, T.TimeSnare });
                var root = Root();
                var runtime = root.Runtime;
                var emmitt = Emmitt(runtime);
                var wasp = Wasp(runtime);
                yield return CommitEverySeat(root, emmitt, NameOf(runtime, face));
                yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
                yield return PointTrayAt(root, emmitt);
                yield return StartActionThroughControl(root, ActionKind.Attack);
                var enemyMinion = runtime.ReadModel.Minions.Single(minion =>
                    minion.Coordinate.Equals(new HexCoordinate(-4, 2))).MinionId;
                Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds,
                    Has.Member(enemyMinion), Remaining(runtime));
                var target = Object.FindObjectsByType<StandardMinionView>()
                    .Single(view => view.Occupancy != null &&
                        view.Occupancy.MinionId.Equals(enemyMinion));
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, target.transform.position), Is.True);
                yield return null;
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
                yield return GarrusUiJourneyTests.May(root, true);
                var count = face.Equals(T.Flashback) ? 3 : 2;
                for (var index = 1; index <= count; index++)
                {
                    var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                        runtime.PendingEffectChoice.Candidates.Where(candidate =>
                            candidate.Value.Kind == EffectValueKind.Coordinate)
                            .Select(candidate => candidate.Value.Coordinate));
                    yield return TakahideUiJourneyPlayModeTests.Pick(root,
                        value => value.Kind == EffectValueKind.Coordinate &&
                            value.Coordinate.Equals(space), "Glitch " + index);
                    Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                        token.Definition.Id.Equals(EmmittComponentContent.Glitch.Id)),
                        Is.EqualTo(index));
                }
                yield return GarrusUiJourneyTests.May(root, true);
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    "enemy hero in the printed radius");
                var tokenSpace = runtime.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).First();
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(tokenSpace), "enemy's chosen Glitch");
                Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate,
                    Is.EqualTo(tokenSpace));
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                    Has.None.Contains("EngineFault"));
            }
        }

        private static IEnumerator BeginResolvedEnemySkill(CardDefinitionId face,
            CardDefinitionId[] enemyHand = null)
        {
            yield return Stage(new[] { face, T.TemporalPunch },
                enemyHand: enemyHand ?? new[]
                { WaspCardContent.MagneticDagger, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var wasp = Wasp(runtime);
            Commit(runtime, emmitt, face);
            Commit(runtime, wasp, WaspCardContent.MagneticDagger);
            yield return null;
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(wasp), Remaining(runtime));
            yield return PointTrayAt(root, wasp);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            var step = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingMovementChoice.LegalDestinations.Where(value => value.Cost == 1 &&
                    HexDistanceQuery.Calculate(value.Destination, EmmittSpace) <= 2)
                    .Select(value => value.Destination));
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, Tile(step).transform.position), Is.True);
            yield return null;
            if (runtime.RoundView.Model.EndTurnInvocation.Command.Kind ==
                GuardsDigital.Core.Replay.DirectorCommandKind.StopMovement)
                yield return TakahideUiJourneyPlayModeTests.StopMoving(root);
            UiPlayer.Click(UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
            yield return null;
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            yield return StartActionThroughControl(root, ActionKind.Skill);
        }

        [UnityTest]
        public IEnumerator TimeSnareAndTrapTargetOnlyAResolvedEnemyAndMakeHerDiscard()
        {
            foreach (var face in new[] { T.TimeSnare, T.TimeTrap })
            {
                yield return BeginResolvedEnemySkill(face);
                var root = Root();
                var runtime = root.Runtime;
                var wasp = Wasp(runtime);
            Assert.That(runtime.PendingEffectChoice.Candidates.Any(candidate =>
                candidate.Value.Kind == EffectValueKind.Hero &&
                candidate.Value.HeroId.Equals(wasp)), Is.True, Remaining(runtime));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "resolved enemy in range");
            yield return TakahideUiJourneyPlayModeTests.PickCard(root,
                WaspCardContent.Electrocute, "enemy discards her remaining card");
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(wasp) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Electrocute))
                .State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
            }
        }

        [UnityTest]
        public IEnumerator TimeLoopSwapsWithTheAlreadyResolvedEnemy()
        {
            yield return BeginResolvedEnemySkill(T.TimeLoop);
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var wasp = Wasp(runtime);
            var enemyBefore = runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate;
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "resolved enemy for Time Loop");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(emmitt)).Coordinate,
                Is.EqualTo(enemyBefore));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate,
                Is.EqualTo(EmmittSpace));
            Assert.That(HeroView(emmitt).Coordinate, Is.EqualTo(enemyBefore));
            Assert.That(HeroView(wasp).Coordinate, Is.EqualTo(EmmittSpace));
        }

        [UnityTest]
        public IEnumerator TimeBombMakesTheResolvedEnemyDiscardOrFallWhenEmpty()
        {
            foreach (var hasCard in new[] { true, false })
            {
                yield return BeginResolvedEnemySkill(T.TimeBomb, hasCard
                    ? new[] { WaspCardContent.MagneticDagger, WaspCardContent.Electrocute }
                    : new[] { WaspCardContent.MagneticDagger });
                var root = Root();
                var runtime = root.Runtime;
                var wasp = Wasp(runtime);
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    "resolved enemy for Time Bomb");
                if (hasCard)
                {
                    yield return TakahideUiJourneyPlayModeTests.PickCard(root,
                        WaspCardContent.Electrocute, "discard instead of defeat");
                    Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(wasp) &&
                        card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Electrocute))
                        .State.Location, Is.EqualTo(CardLocation.DiscardPile));
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(wasp)), Is.True);
                }
                else
                {
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(wasp)), Is.False,
                        "With no hand card to discard, Time Bomb defeats her.");
                }
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                    Has.None.Contains("EngineFault"));
            }
        }

        [UnityTest]
        public IEnumerator TimeWarpChoosesItsResolvedEnemySwapArmAndExchangesBothPieces()
        {
            yield return BeginResolvedEnemySkill(T.TimeWarp);
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var wasp = Wasp(runtime);
            var enemyBefore = runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate;
            var mode = runtime.PendingEffectChoice;
            Assert.That(mode, Is.Not.Null, Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            var control = UiPlayer.DecisionControl(runtime, command =>
                command.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Candidate.OptionNodeId.Value == "time-warp-swap-arm");
            Assert.That(control, Is.Not.Null, Remaining(runtime));
            UiPlayer.ClickDecisionControl(runtime, root.Hud, control);
            yield return null;
            yield return ConfirmIfStillStanding(root, mode.NodeId);
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "resolved enemy for the chosen swap arm");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(emmitt)).Coordinate,
                Is.EqualTo(enemyBefore));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate,
                Is.EqualTo(EmmittSpace));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator AlternativeTimelinesCommitsTwoFacesAndRetrievesEitherAtReveal()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedEmmitt);
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            Assert.That(UiPlayer.ItemStripDescendants(root.Hud).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "Alternative Timelines stands faceup in Emmitt's Ultimate rail.");

            yield return PointTrayAt(root, emmitt);
            foreach (var face in new[] { T.ReverseTime, T.TemporalJudgement })
            {
                var title = NameOf(runtime, face);
                yield return UiPlayer.WaitForNamedElement(root.Hud, "gd-card-choice", title);
                UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(root.Hud, "gd-card-choice", title));
                yield return null;
                UiPlayer.Click(UiPlayer.NamedElementWithText(root.Hud, "gd-card-choice", title));
                yield return null;
            }
            UiPlayer.Click(UiPlayer.Button(root.Hud, "CONFIRM TWO FACEDOWN"));
            yield return null;
            yield return PointTrayAt(root, Wasp(runtime));
            yield return CommitThroughTray(root,
                XargathaUiJourneyPlayModeTests.LowestInitiativeCardOf(runtime, Wasp(runtime)));
            Assert.That(runtime.RoundView.Model.RevealRetrievalChoices.Count, Is.EqualTo(2),
                "Both faces reveal together before Emmitt chooses one to retrieve.");
            var chosen = runtime.RoundView.Model.RevealRetrievalChoices.Single(choice =>
                choice.Command.CardInstanceId.Equals(runtime.Cards.Single(card =>
                    card.OwnerId.Equals(emmitt) &&
                    card.DefinitionReference.DefinitionId.Equals(T.ReverseTime)).Id));
            UiPlayer.Click(UiPlayer.Button(root.Hud, chosen.Label));
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(emmitt) &&
                card.DefinitionReference.DefinitionId.Equals(T.ReverseTime)).State.Location,
                Is.EqualTo(CardLocation.Hand));
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(emmitt) &&
                card.DefinitionReference.DefinitionId.Equals(T.TemporalJudgement)).State.Location,
                Is.EqualTo(CardLocation.TurnArea));
            Assert.That(runtime.RoundView.Model.RevealRetrievalChoices, Is.Empty,
                "The old reveal choice clears after its one accepted answer.");
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator TimeWalkAndFastForwardMoveAStationaryEnemyExactlyTwoStraightSpaces()
        {
            foreach (var face in new[] { T.TimeWalk, T.FastForward })
            {
            yield return Stage(new[] { T.TemporalPunch, face },
                enemyHand: new[] { WaspCardContent.MagneticDagger, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var wasp = Wasp(runtime);
            Commit(runtime, emmitt, T.TemporalPunch);
            Commit(runtime, wasp, WaspCardContent.MagneticDagger);
            yield return null;
            for (var budget = 0; budget < 30 && runtime.RoundIndex == 1; budget++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.RoundIndex, Is.EqualTo(2), Remaining(runtime));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate,
                Is.EqualTo(WaspBeside), "Wasp did not change spaces throughout the first round.");

            yield return PointTrayAt(root, emmitt);
            yield return CommitThroughTray(root, NameOf(runtime, face));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.Electrocute));
            yield return GarrusUiJourneyTests.WaitForActor(root, wasp);
            UiPlayer.Click(UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
            yield return null;
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "stationary enemy in printed range");
            var destination = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingEffectChoice.Candidates.Where(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            Assert.That(HexDistanceQuery.Calculate(WaspBeside, destination), Is.EqualTo(2));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(destination), "two-space straight line");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(HeroView(wasp).Coordinate, Is.EqualTo(destination));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
            }
        }

        [UnityTest]
        public IEnumerator BackToTheFutureOffersItsModalMovementAndMovesTheChosenStationaryEnemy()
        {
            yield return StageTierThree(
                new[] { T.TemporalJudgement, T.BackToTheFuture },
                new[] { WaspCardContent.MagneticDagger, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var wasp = Wasp(runtime);
            Commit(runtime, emmitt, T.TemporalJudgement);
            Commit(runtime, wasp, WaspCardContent.MagneticDagger);
            yield return null;
            for (var budget = 0; budget < 30 && runtime.RoundIndex == 1; budget++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.RoundIndex, Is.EqualTo(2), Remaining(runtime));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate,
                Is.EqualTo(WaspBeside));

            yield return PointTrayAt(root, emmitt);
            yield return CommitThroughTray(root, NameOf(runtime, T.BackToTheFuture));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.Electrocute));
            yield return GarrusUiJourneyTests.WaitForActor(root, wasp);
            UiPlayer.Click(UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
            yield return null;
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var mode = runtime.PendingEffectChoice;
            Assert.That(mode, Is.Not.Null, Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            var control = UiPlayer.DecisionControl(runtime, command =>
                command.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Candidate.OptionNodeId.Value ==
                    T.BackToTheFuture.Value + "-root");
            Assert.That(control, Is.Not.Null, Remaining(runtime));
            UiPlayer.ClickDecisionControl(runtime, root.Hud, control);
            yield return null;
            yield return ConfirmIfStillStanding(root, mode.NodeId);
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "stationary enemy in the chosen movement arm");
            var destination = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingEffectChoice.Candidates.Where(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            Assert.That(HexDistanceQuery.Calculate(WaspBeside, destination), Is.EqualTo(2));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(destination), "two-space modal line");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator FutureProofChoosesTheVisibleThisTurnImmunityArm()
        {
            yield return Stage(new[] { T.FutureProof, T.TimeSnare });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            yield return CommitEverySeat(root, emmitt, NameOf(runtime, T.FutureProof));
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null, Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            var control = UiPlayer.DecisionControl(runtime, command =>
                command.Kind == GuardsDigital.Core.Replay.DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Candidate.OptionNodeId.Value ==
                    T.FutureProof.Value + "-text-root");
            Assert.That(control, Is.Not.Null, Remaining(runtime));
            UiPlayer.ClickDecisionControl(runtime, root.Hud, control);
            yield return null;
            yield return ConfirmIfStillStanding(root, modal.NodeId);
            var source = runtime.Cards.Single(card => card.OwnerId.Equals(emmitt) &&
                card.DefinitionReference.DefinitionId.Equals(T.FutureProof));
            Assert.That(runtime.IsActiveEffectSource(source.Id), Is.True,
                "The card's chosen This turn protection is active on the board state.");
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator TimeCapsuleLetsEmmittAndHisAllyRetrieveOnlyThisTurnsDiscards()
        {
            yield return StageWithAlly(
                new[] { T.TimeCapsule, T.TemporalPunch },
                new[] { ArienCardContent.LiquidLeap, ArienCardContent.AspiringDuelist },
                new[] { WaspCardContent.Shock });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var wasp = Wasp(runtime);
            var arien = runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "arien").HeroId;
            yield return PointTrayAt(root, emmitt);
            yield return CommitThroughTray(root, NameOf(runtime, T.TimeCapsule));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.Shock));
            yield return PointTrayAt(root, arien);
            yield return CommitThroughTray(root, NameOf(runtime, ArienCardContent.LiquidLeap));
            yield return GarrusUiJourneyTests.WaitForActor(root, wasp);
            yield return PointTrayAt(root, wasp);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(emmitt).transform.position), Is.True);
            yield return null;
            yield return ClickDefenseFace(root, T.TemporalPunch);
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(emmitt)), Is.True);
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(arien),
                "Shock's nonadjacent enemy hero");
            yield return TakahideUiJourneyPlayModeTests.PickCard(root,
                ArienCardContent.AspiringDuelist, "Arien's this-turn discard");
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(arien) &&
                card.DefinitionReference.DefinitionId.Equals(ArienCardContent.AspiringDuelist))
                .State.Location, Is.EqualTo(CardLocation.DiscardPile));
            UiPlayer.Click(UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
            yield return null;
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            var skillLabel = runtime.RoundView.Model.Actions.Single(action =>
                action.Command.ActionDefinitionId.Equals(T.PrimarySkill)).Label;
            bool SkillReady() => UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>()
                .Any(element => UiPlayer.IsShown(element) &&
                    element.name == "gd-printed-action-region" &&
                    element.userData is PrintedActionRegionViewModel region &&
                    region.ActionLabel == skillLabel);
            for (var frame = 0; frame < 180 && !SkillReady(); frame++)
            {
                if (UiPlayer.HasButton(root.Hud, "CONTINUE"))
                {
                    var next = UiPlayer.Button(root.Hud, "CONTINUE");
                    if (next.enabledInHierarchy && next.worldBound.width > 0f &&
                        next.worldBound.height > 0f)
                        UiPlayer.Click(next);
                }
                yield return null;
            }
            Assert.That(SkillReady(), Is.True,
                skillLabel + " " + Remaining(runtime) + " " + UiPlayer.Describe(root.Hud) +
                " regions=" + string.Join(" | ", runtime.RoundView.Model.PrintedActionRegions
                    .Select(region => region.ActionLabel + "/" + region.ActionKind)));
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(emmitt));
            yield return GarrusUiJourneyTests.May(root, true);
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(emmitt) &&
                card.DefinitionReference.DefinitionId.Equals(T.TemporalPunch)).State.Location,
                Is.EqualTo(CardLocation.Hand));
            Assert.That(runtime.PendingEffectChoice.Candidates.Any(candidate =>
                candidate.Value.Kind == EffectValueKind.Hero &&
                candidate.Value.HeroId.Equals(arien)), Is.True,
                "Emmitt first names a friendly hero with a current-turn discard. " + Remaining(runtime));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(arien),
                "friendly hero with a current-turn discard");
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(arien));
            yield return GarrusUiJourneyTests.May(root, true);
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(arien) &&
                card.DefinitionReference.DefinitionId.Equals(ArienCardContent.AspiringDuelist))
                .State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator AGlitchPlacementPauseRestoresAndAnswersOnTheRebuiltBoard()
        {
            yield return Stage(new[] { T.UnstableTimeline, T.TimeSnare });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            yield return CommitEverySeat(root, emmitt, NameOf(runtime, T.UnstableTimeline));
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var asked = runtime.PendingEffectChoice;
            Assert.That(asked, Is.Not.Null, Remaining(runtime));
            var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                asked.Candidates.Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            var pause = runtime.CaptureSnapshot();
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(space),
                "first Glitch before restore");
            var after = runtime.CaptureSnapshot();
            Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                token.Definition.Id.Equals(EmmittComponentContent.Glitch.Id)), Is.EqualTo(1));

            Assert.That(runtime.RestoreSnapshot(pause), Is.True, runtime.LastOperationDetail);
            yield return null;
            yield return null;
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked.NodeId));
            Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                token.Definition.Id.Equals(EmmittComponentContent.Glitch.Id)), Is.Zero);
            Assert.That(runtime.BoardView.HeroViews.Count(view =>
                view.Occupancy.HeroId.Equals(emmitt)), Is.EqualTo(1));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(space),
                "first Glitch after restore");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(after),
                "Reanswering the restored question reconstructs the same authoritative bytes.");
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator AGlitchPlacementUndoesToItsChoiceAndReplaysExactly()
        {
            yield return Stage(new[] { T.UnstableTimeline, T.TimeSnare });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            yield return CommitEverySeat(root, emmitt, NameOf(runtime, T.UnstableTimeline));
            yield return GarrusUiJourneyTests.WaitForActor(root, emmitt);
            yield return PointTrayAt(root, emmitt);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var asked = runtime.PendingEffectChoice;
            Assert.That(asked, Is.Not.Null, Remaining(runtime));
            var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                asked.Candidates.Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            var pause = runtime.CaptureSnapshot();
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(space),
                "first Glitch before Undo");
            var after = runtime.CaptureSnapshot();
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
            yield return null;
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked.NodeId), Remaining(runtime));
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(space),
                "first Glitch after Undo");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(after));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator EmmittFallsRespawnsByBoardClickAndRestartRebuildsHisPiece()
        {
            yield return Stage(new[] { T.TimeSnare, T.TimeTrap, T.TimeWalk },
                enemyHand: new[]
                    { WaspCardContent.MagneticDagger, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            var standee = HeroView(emmitt);
            Assert.That(standee.HeroContentId.Value.Value, Is.EqualTo(T.ContentSetId));
            Commit(runtime, emmitt, T.TimeSnare);
            Commit(runtime, Wasp(runtime), WaspCardContent.MagneticDagger);
            yield return null;
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(Wasp(runtime)));
            yield return PointTrayAt(root, Wasp(runtime));
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, standee.transform.position), Is.True);
            yield return null;
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(emmitt)), Is.False,
                "Emmitt's declined Defense removes his piece.");
            Assert.That(standee.gameObject.activeSelf, Is.False);
            Assert.That(runtime.BoardView.HeroDefeatGhostFor(standee), Is.Not.Null);

            yield return YieldUntilTheNamedSeatActs(runtime, emmitt);
            CommitSoTheNamedSeatActs(runtime, emmitt, T.TimeTrap);
            yield return YieldUntilTheNamedSeatActs(runtime, emmitt);
            var respawn = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.RoundView.Model.HeroRespawnOptions.Select(option => option.Destination));
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, Tile(respawn).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(emmitt)).Coordinate,
                Is.EqualTo(respawn));
            Assert.That(HeroView(emmitt).Coordinate, Is.EqualTo(respawn));

            yield return UiPlayer.WaitForControl(root.Hud, "DIRECTOR");
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(emmitt)).Coordinate,
                Is.EqualTo(EmmittSpace));
            Assert.That(HeroView(emmitt).Coordinate, Is.EqualTo(EmmittSpace));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator StagedEmmittSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedEmmitt);
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = runtime.Setup.Heroes.Single(hero =>
                hero.ContentId.Value == EmmittCardContent.ContentSetId).HeroId;
            var view = HeroView(emmitt);
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo(EmmittCardContent.ContentSetId));
            Assert.That(view.HasTokenVisual, Is.True, "Emmitt's prepared standee face is drawn.");
            Assert.That(view.Coordinate,
                Is.EqualTo(runtime.Heroes.Single(hero => hero.HeroId.Equals(emmitt)).Coordinate));
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(emmitt));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Count(hero => hero.HeroId.Equals(emmitt)), Is.EqualTo(1));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator EveryEmmittFaceKeepsTheStandardCardFootprint()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedEmmitt);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Emmitt", EmmittCardContent.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator EmmittsFiveCardHandStaysReachableAtTheShortDesktopViewport()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedEmmitt);
            var root = Root();
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            UiPlayer.SetPlayerViewport(2560, 540, "Emmitt short desktop viewport");
            yield return null;
            root.Runtime.RefreshAll();
            yield return null;
            yield return null;

            var width = Screen.width;
            var height = Screen.height;
            var tray = UiPlayer.Descendants(root.Hud.RootElement)
                .Single(element => element.name == "gd-tray-region");
            var region = tray.worldBound;
            var scroll = UiPlayer.Descendants(tray).OfType<ScrollView>().Single();
            var cards = UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-card-choice").ToArray();
            var cardSizes = cards.Select(card => card.worldBound.size).ToArray();
            var lastReachable = cards.Length == 5 && UiPlayer.IsShown(cards[4]) &&
                cards[4].enabledInHierarchy && cards[4].worldBound.xMax <= region.xMax + 0.5f;
            var scrollMode = scroll.mode;
            var horizontalVisibility = scroll.horizontalScrollerVisibility;
            var verticalVisibility = scroll.verticalScrollerVisibility;

            UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight,
                "restore viewport after Emmitt short-desktop check");
            yield return null;
            root.Runtime.RefreshAll();
            yield return null;
            yield return null;

            Assert.That(width, Is.EqualTo(2560));
            Assert.That(height, Is.EqualTo(540));
            Assert.That(cardSizes, Has.Length.EqualTo(5));
            foreach (var size in cardSizes)
            {
                Assert.That(size.x, Is.EqualTo(CardFootprint.Width).Within(UiPlayer.PixelSnapping(2)));
                Assert.That(size.y, Is.EqualTo(CardFootprint.Height).Within(UiPlayer.PixelSnapping(2)));
            }
            Assert.That(scrollMode, Is.EqualTo(ScrollViewMode.Vertical));
            Assert.That(horizontalVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
            Assert.That(verticalVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
            Assert.That(lastReachable, Is.True,
                "The ordinary five-card hand remains pointer-reachable without horizontal travel.");
            Assert.That((uint)Screen.width, Is.EqualTo(restoreWidth));
            Assert.That((uint)Screen.height, Is.EqualTo(restoreHeight));
        }

        [UnityTest]
        public IEnumerator EmmittsLevelUpAddsTheChosenCardAndShowsTheSiblingItem()
        {
            yield return Stage(new[] { T.TemporalPunch, T.TimeSnare });
            var root = Root();
            var runtime = root.Runtime;
            var emmitt = Emmitt(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(emmitt)); budget++)
            {
                if (runtime.PendingMinionBattleChoice != null)
                {
                    var battle = runtime.MinionChoiceView.Model;
                    if (battle != null && battle.Candidates.Count > 0)
                        Assert.That(runtime.SubmitMinionChoiceCandidate(
                            battle.Candidates.First(value => value.Destination == null).MinionId), Is.True);
                    Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                    yield return null;
                }
                else
                    yield return NextRoundDecision(root);
            }
            Assert.That(runtime.SystemTransitionPrompt?.Kind,
                Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade), Remaining(runtime));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .First(option => option.Card != null && option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(emmitt));
            var previous = before.ItemBonuses.Where(value => value.Key == upgrade.ItemIcon)
                .Select(value => value.Value).SingleOrDefault();
            var button = GarrusLifecycleJourneyTests.UpgradeButton(root, upgrade);
            UiPlayer.EnsureVisible(button);
            yield return null;
            UiPlayer.Click(button);
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(upgrade.Card.CardInstanceId)).State.Location,
                Is.EqualTo(CardLocation.Hand));
            for (var budget = 0; budget < 50 && runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
                yield return NextRoundDecision(root);
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(emmitt));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value,
                Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(emmitt))
                .ItemBonuses, Is.Not.Empty);
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }
    }
}
