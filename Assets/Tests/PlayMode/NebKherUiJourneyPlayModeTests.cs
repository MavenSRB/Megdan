using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Replay;
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
using T = GuardsDigital.Core.Cards.Verified.NebKherCardContent;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Milestone 9.54: configured NebKher journeys through player input.</summary>
    public sealed class NebKherUiJourneyPlayModeTests
    {
        private static readonly HexCoordinate NebKherSpace = new HexCoordinate(-3, 1);
        private static readonly HexCoordinate WaspBeside = new HexCoordinate(-2, 1);
        private static (HeroId NebKher, HeroId Wasp,
            (MinionId Id, HexCoordinate At)[] Minions)? cached;

        private static HeroId NebKher(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == T.ContentSetId).HeroId;

        private static HeroId Wasp(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero =>
                hero.ContentId.Value == WaspCardContent.ContentSetId).HeroId;

        private static IEnumerator Stage(CardDefinitionId[] hand,
            HexCoordinate? waspAt = null, CardDefinitionId[] enemyHand = null,
            SceneCardContent scene = SceneCardContent.VerifiedEngagedStagedNebKher)
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            if (!cached.HasValue)
            {
                yield return LoadStagedScene(scene);
                var plain = Root().Runtime;
                cached = (NebKher(plain), Wasp(plain),
                    plain.ReadModel.Minions.Select(value =>
                        (value.MinionId, value.Coordinate)).ToArray());
            }

            var seats = cached.Value;
            var enemySpace = waspAt ?? WaspBeside;
            var occupied = new[] { NebKherSpace, enemySpace };
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(seats.NebKher, NebKherSpace, hand: hand),
                    new SandboxHeroStaging(seats.Wasp, enemySpace, hand: enemyHand)
                },
                removedMinions: seats.Minions.Where(value => occupied.Contains(value.At))
                    .Select(value => value.Id));
            yield return LoadStagedScene(scene);
            Assert.That(Root().Runtime.Heroes.Single(hero =>
                hero.HeroId.Equals(seats.NebKher)).Coordinate, Is.EqualTo(NebKherSpace));
        }

        private static IEnumerator ChooseWindowOption(MatchSceneCompositionRoot root,
            System.Func<EffectChoiceCandidate, bool> which, string why)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, why + " " + Remaining(runtime));
            var selected = choice.SelectInvocations.SingleOrDefault(value =>
                which(value.Candidate));
            Assert.That(selected, Is.Not.Null, why + " " + Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            var control = UiPlayer.DecisionControl(runtime, command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.Equals(selected.Candidate));
            if (control == null && runtime.DecisionWindowView.Model?.State ==
                DecisionWindowState.Docked)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "EXPAND"));
                yield return null;
                control = UiPlayer.DecisionControl(runtime, command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    command.EffectChoice.Candidate.Equals(selected.Candidate));
            }
            Assert.That(control, Is.Not.Null,
                why + "; window=" + runtime.DecisionWindowView.Model?.State +
                "; controls=" + string.Join(" | ", runtime.DecisionWindowView.Model?.Controls
                    .Select(value => value.Label + ":" + value.Command?.Kind + ":" +
                        value.Command?.EffectChoice?.Candidate.Value.IntegerValue) ??
                    System.Array.Empty<string>()) + "; " + Remaining(runtime));
            UiPlayer.EnsureVisible(runtime.DecisionWindowView.ElementFor(control));
            yield return null;
            UiPlayer.ClickDecisionControl(runtime, root.Hud, control);
            yield return null;
            yield return ConfirmIfStillStanding(root, choice.NodeId);
            var after = runtime.CaptureSnapshot();
            Assert.That(runtime.SubmitProjectedCommand(
                DirectorCommand.AnswerEffectChoice(selected)), Is.False,
                "The already answered window choice is stale.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(after));
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesNebKherThroughItsCardControl()
        {
            foreach (var definition in T.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                var other = definition.Id.Equals(T.MindGrip)
                    ? T.FleetingImage : T.MindGrip;
                yield return Stage(new[] { definition.Id, other });
                var root = Root();
                var runtime = root.Runtime;
                var nebkher = NebKher(runtime);
                yield return CommitEverySeat(root, nebkher, NameOf(runtime, definition.Id));
                yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
                yield return PointTrayAt(root, nebkher);
                yield return StartActionThroughControl(root, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                    definition.Id.Value + " " + Remaining(runtime));
                var step = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                    runtime.PendingMovementChoice.LegalDestinations.Where(value =>
                        value.Cost == 1).Select(value => value.Destination));
                Assert.That(UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                    Tile(step).transform.position), Is.True);
                yield return null;
                Assert.That(runtime.Heroes.Single(hero =>
                    hero.HeroId.Equals(nebkher)).Coordinate, Is.EqualTo(step),
                    definition.Id.Value);
                Assert.That(HeroView(nebkher).Coordinate, Is.EqualTo(step),
                    definition.Id.Value);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersFromItsOwnFace()
        {
            foreach (var definition in T.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction)))
            {
                var committed = definition.Id.Equals(T.ImbueDoubt)
                    ? T.TimeToReconsider : T.ImbueDoubt;
                yield return Stage(new[] { definition.Id, committed },
                    enemyHand: new[] { WaspCardContent.MagneticDagger });
                var root = Root();
                var runtime = root.Runtime;
                var nebkher = NebKher(runtime);
                yield return PointTrayAt(root, nebkher);
                yield return CommitThroughTray(root, NameOf(runtime, committed));
                yield return PointTrayAt(root, Wasp(runtime));
                yield return CommitThroughTray(root,
                    NameOf(runtime, WaspCardContent.MagneticDagger));
                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(Wasp(runtime)),
                    definition.Id.Value + " " + Remaining(runtime));
                yield return PointTrayAt(root, Wasp(runtime));
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                    definition.Id.Value + " " + Remaining(runtime));
                Assert.That(UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                    HeroView(nebkher).transform.position), Is.True);
                yield return null;
                Assert.That(runtime.PendingDefenseChoice?.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)),
                    Is.True, definition.Id.Value + " " + Remaining(runtime));
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, definition.Id.Value);
                Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(nebkher) &&
                    card.DefinitionReference.DefinitionId.Equals(definition.Id))
                    .State.Location, Is.EqualTo(CardLocation.DiscardPile), definition.Id.Value);
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                    value.DisplayText), Has.None.Contains("EngineFault"), definition.Id.Value);
            }
        }

        [UnityTest]
        public IEnumerator EveryDelayedBlueFaceNamesAColorOnItsRealWindow()
        {
            foreach (var face in new[]
                { T.ImbueDoubt, T.TimeToReconsider, T.AnIllusionOfChoice })
            {
            yield return Stage(new[] { face, T.MindGrip });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            yield return CommitEverySeat(root, nebkher, NameOf(runtime, face));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.Candidates, Has.Count.EqualTo(6));
            yield return ChooseWindowOption(root, candidate =>
                candidate.OptionNodeId.Value.EndsWith("-red-create"), "name Red");
            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(nebkher) &&
                card.DefinitionReference.DefinitionId.Equals(face))
                .State.Location, Is.EqualTo(CardLocation.TurnArea), face.Value);
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                value.DisplayText), Has.None.Contains("EngineFault"), face.Value);
            }
        }

        [UnityTest]
        public IEnumerator DelayedBlueNextRevealAsksForEnemyAndWholeCardThroughRealControls()
        {
            yield return Stage(new[] { T.ImbueDoubt, T.PhantasmalSentry },
                enemyHand: new[] { WaspCardContent.Shock,
                    WaspCardContent.MagneticDagger, WaspCardContent.LiftUp });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            var wasp = Wasp(runtime);
            yield return PointTrayAt(root, nebkher);
            yield return CommitThroughTray(root, NameOf(runtime, T.ImbueDoubt));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.Shock));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return ChooseWindowOption(root, candidate =>
                candidate.OptionNodeId.Value.EndsWith("-gold-create"), "name Gold");
            var source = runtime.Cards.Single(card => card.OwnerId.Equals(nebkher) &&
                card.DefinitionReference.DefinitionId.Equals(T.ImbueDoubt));
            Assert.That(source.State.Location, Is.EqualTo(CardLocation.TurnArea));
            for (var budget = 0; budget < 28 && runtime.TurnIndex == 1; budget++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.TurnIndex, Is.EqualTo(2), Remaining(runtime));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.LiftUp));
            yield return PointTrayAt(root, nebkher);
            yield return CommitThroughTray(root, NameOf(runtime, T.PhantasmalSentry));
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The named-color effect interrupts the next reveal. " + Remaining(runtime));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "choose the enemy at the next reveal");
            var gold = runtime.Cards.Single(card => card.OwnerId.Equals(wasp) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.MagneticDagger));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.CardInstance &&
                    value.CardInstanceId.Equals(gold.Id), "discard the named Gold face");
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(gold.Id)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator BothRealitySplitsChooseTheirAxisOnTheRealWindow()
        {
            foreach (var face in new[] { T.CrackInReality, T.ShiftReality })
            {
            yield return Stage(new[] { face, T.MindGrip });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            yield return CommitEverySeat(root, nebkher, NameOf(runtime, face));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.Candidates, Has.Count.EqualTo(3));
            var firstAxis = runtime.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.IntegerValue).Min();
            yield return ChooseWindowOption(root, candidate =>
                candidate.Value.Kind == EffectValueKind.Integer &&
                candidate.Value.IntegerValue == firstAxis, "choose a board axis");
            var source = runtime.Cards.Single(card => card.OwnerId.Equals(nebkher) &&
                card.DefinitionReference.DefinitionId.Equals(face));
            Assert.That(runtime.IsActiveEffectSource(source.Id), Is.True, face.Value);
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                value.DisplayText), Has.None.Contains("EngineFault"), face.Value);
            }
        }

        [UnityTest]
        public IEnumerator DiabolicalLaughterLaughsThenPlacesAnIllusionThroughItsPrintedModes()
        {
            yield return Stage(new[] { T.DiabolicalLaughter, T.MindGrip });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            yield return CommitEverySeat(root, nebkher,
                NameOf(runtime, T.DiabolicalLaughter));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var laugh = runtime.PendingEffectChoice;
            Assert.That(laugh, Is.Not.Null, Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return ConfirmIfStillStanding(root, laugh.NodeId);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var first = runtime.PendingEffectChoice;
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return ConfirmIfStillStanding(root, first.NodeId);
            Assert.That(runtime.PendingEffectChoice?.Candidates.Any(candidate =>
                candidate.OptionNodeId.Value.EndsWith("-place-body")), Is.True,
                Remaining(runtime));
            yield return ChooseWindowOption(root, candidate =>
                candidate.OptionNodeId.Value.EndsWith("-place-body"),
                "place Illusion in first laughter mode");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingEffectChoice.Candidates.Where(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(space), "place laughter Illusion");
            Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id)),
                Is.EqualTo(1));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                value.DisplayText), Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator DiabolicalLaughterSwapsTwoResolvedEnemyCardsThroughTheirFaces()
        {
            yield return Stage(new[] { T.FleetingImage, T.MindGrip, T.DiabolicalLaughter },
                enemyHand: new[] { WaspCardContent.Shock,
                    WaspCardContent.MagneticDagger, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            var wasp = Wasp(runtime);
            var waspFirst = runtime.Cards.Single(card => card.OwnerId.Equals(wasp) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Shock)).Id;
            var waspSecond = runtime.Cards.Single(card => card.OwnerId.Equals(wasp) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.MagneticDagger)).Id;
            var blueFaces = new[] { T.FleetingImage, T.MindGrip };
            var redFaces = new[] { WaspCardContent.Shock, WaspCardContent.MagneticDagger };
            for (var index = 0; index < 2; index++)
            {
                yield return PointTrayAt(root, nebkher);
                yield return CommitThroughTray(root, NameOf(runtime, blueFaces[index]));
                yield return PointTrayAt(root, wasp);
                yield return CommitThroughTray(root, NameOf(runtime, redFaces[index]));
                for (var budget = 0; budget < 28 && runtime.TurnIndex == index + 1; budget++)
                    yield return NextRoundDecision(root);
                Assert.That(runtime.TurnIndex, Is.EqualTo(index + 2), Remaining(runtime));
            }
            yield return PointTrayAt(root, nebkher);
            yield return CommitThroughTray(root, NameOf(runtime, T.DiabolicalLaughter));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.Electrocute));
            Assert.That(runtime.TurnSlots[0].Assignments.Single(value =>
                value.HeroId.Equals(wasp)).CardInstanceId, Is.EqualTo(waspFirst));
            Assert.That(runtime.TurnSlots[1].Assignments.Single(value =>
                value.HeroId.Equals(wasp)).CardInstanceId, Is.EqualTo(waspSecond));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var laugh = runtime.PendingEffectChoice;
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return ConfirmIfStillStanding(root, laugh.NodeId);
            var optional = runtime.PendingEffectChoice;
            Assert.That(optional, Is.Not.Null, Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return ConfirmIfStillStanding(root, optional.NodeId);
            yield return ChooseWindowOption(root, candidate =>
                candidate.OptionNodeId.Value.EndsWith("-cards-body"),
                "swap resolved enemy cards");
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "choose enemy resolved slots");
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.CardInstance &&
                    value.CardInstanceId.Equals(waspFirst), "first resolved card face");
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.CardInstance &&
                    value.CardInstanceId.Equals(waspSecond), "second resolved card face");
            Assert.That(runtime.TurnSlots[0].Assignments.Single(value =>
                value.HeroId.Equals(wasp)).CardInstanceId, Is.EqualTo(waspSecond));
            Assert.That(runtime.TurnSlots[1].Assignments.Single(value =>
                value.HeroId.Equals(wasp)).CardInstanceId, Is.EqualTo(waspFirst));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator UnlockedUltimateInterruptsOneChosenLaughAndEnemyDiscardsAWholeFace()
        {
            yield return Stage(new[] { T.DiabolicalLaughter, T.MindGrip },
                enemyHand: new[] { WaspCardContent.Shock,
                    WaspCardContent.MagneticDagger },
                scene: SceneCardContent.VerifiedTierThreeEngagedStagedNebKher);
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            var wasp = Wasp(runtime);
            var ultimate = runtime.Cards.Single(card => card.OwnerId.Equals(nebkher) &&
                card.DefinitionReference.DefinitionId.Equals(T.WhatTheHellAreYou));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea));
            yield return PointTrayAt(root, nebkher);
            yield return CommitThroughTray(root, NameOf(runtime, T.DiabolicalLaughter));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root,
                NameOf(runtime, WaspCardContent.Shock));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var laugh = runtime.PendingEffectChoice;
            Assert.That(laugh?.ConfirmInvocation, Is.Not.Null, Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            UiPlayer.ClickEffectControl(runtime, root.Hud,
                EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return ConfirmIfStillStanding(root, laugh.NodeId);
            Assert.That(runtime.PendingEffectChoice?.ChooserId, Is.EqualTo(wasp),
                "The Ultimate interrupts the laugh before the first optional mode. " +
                Remaining(runtime));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.CardInstance &&
                    runtime.Cards.Single(card => card.Id.Equals(value.CardInstanceId))
                        .DefinitionReference.DefinitionId.Equals(
                            WaspCardContent.MagneticDagger),
                "enemy discards his remaining whole card face");
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(wasp) &&
                card.DefinitionReference.DefinitionId.Equals(
                    WaspCardContent.MagneticDagger)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(runtime.PendingEffectChoice?.ChooserId, Is.EqualTo(nebkher),
                "The laughter action resumes after exactly one Ultimate discard.");
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                value.DisplayText), Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator PhantasmalSentryChoosesItsAdjacentAttackArmThenTargetsOnTheBoard()
        {
            yield return Stage(new[] { T.PhantasmalSentry, T.MindGrip },
                enemyHand: new[] { WaspCardContent.MagneticDagger,
                    WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            var wasp = Wasp(runtime);
            yield return CommitEverySeat(root, nebkher,
                NameOf(runtime, T.PhantasmalSentry));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed target mode is asked before the ranged Attack. " +
                Remaining(runtime));
            yield return ChooseWindowOption(root, candidate =>
                candidate.OptionNodeId.Value.EndsWith("-adjacent-arm"),
                "target an adjacent unit");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "target adjacent Wasp");
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(wasp)), Is.False);
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                value.DisplayText), Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator PhantasmalWarriorAndChampionChooseTheirAdjacentAttackArmOnTheBoard()
        {
            foreach (var face in new[] { T.PhantasmalWarrior, T.PhantasmalChampion })
            {
                yield return Stage(new[] { face, T.MindGrip },
                    enemyHand: new[] { WaspCardContent.MagneticDagger,
                        WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var nebkher = NebKher(runtime);
                var wasp = Wasp(runtime);
                yield return CommitEverySeat(root, nebkher, NameOf(runtime, face));
                yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
                yield return PointTrayAt(root, nebkher);
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " " + Remaining(runtime));
                yield return ChooseWindowOption(root, candidate =>
                    candidate.OptionNodeId.Value.EndsWith("-adjacent-arm"),
                    "target an adjacent unit with " + face.Value);
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    "target adjacent Wasp with " + face.Value);
                Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
                UiPlayer.ClickDefenseDecline(runtime, root.Hud);
                yield return null;
                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(wasp)), Is.False,
                    face.Value);
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                    Has.None.Contains("EngineFault"), face.Value);
            }
        }

        [UnityTest]
        public IEnumerator WarriorAndChampionMoveAnIllusionThroughTheirPrintedAttackArm()
        {
            foreach (var face in new[] { T.PhantasmalWarrior, T.PhantasmalChampion })
            {
                yield return Stage(new[] { T.FleetingImage, face },
                    enemyHand: new[] { WaspCardContent.Shock,
                        WaspCardContent.MagneticDagger });
                var root = Root();
                var runtime = root.Runtime;
                var nebkher = NebKher(runtime);
                var wasp = Wasp(runtime);
                yield return PointTrayAt(root, nebkher);
                yield return CommitThroughTray(root, NameOf(runtime, T.FleetingImage));
                yield return PointTrayAt(root, wasp);
                yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.Shock));
                yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
                yield return PointTrayAt(root, nebkher);
                yield return StartActionThroughControl(root, ActionKind.Skill);
                var nearEnemy = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                    runtime.PendingEffectChoice.Candidates.Where(candidate =>
                        candidate.Value.Kind == EffectValueKind.Coordinate &&
                        candidate.Value.Coordinate.Equals(new HexCoordinate(-3, 0)))
                        .Select(candidate => candidate.Value.Coordinate));
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(nearEnemy), "place attack-setup Illusion");
                if (runtime.PendingEffectChoice != null)
                {
                    yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
                    UiPlayer.ClickEffectControl(runtime, root.Hud,
                        EffectChoiceCommandKind.Decline);
                    yield return null;
                }
                for (var budget = 0; budget < 28 && runtime.TurnIndex == 1; budget++)
                    yield return NextRoundDecision(root);
                Assert.That(runtime.TurnIndex, Is.EqualTo(2), Remaining(runtime));
                Assert.That(runtime.Projection.PlacedTokens.Single(token =>
                    token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id))
                    .Coordinate, Is.EqualTo(nearEnemy));
                yield return PointTrayAt(root, nebkher);
                yield return CommitThroughTray(root, NameOf(runtime, face));
                yield return PointTrayAt(root, wasp);
                yield return CommitThroughTray(root,
                    NameOf(runtime, WaspCardContent.MagneticDagger));
                yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
                yield return PointTrayAt(root, nebkher);
                yield return StartActionThroughControl(root, ActionKind.Attack);
                yield return ChooseWindowOption(root, candidate =>
                    candidate.OptionNodeId.Value.EndsWith("-image-arm"),
                    "move Illusion before " + face.Value + " attack");
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    "choose enemy beside Illusion");
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(nearEnemy), "choose Illusion to move");
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
                var moveSpaces = runtime.PendingEffectChoice.Candidates.Where(candidate =>
                        candidate.Value.Kind == EffectValueKind.Coordinate &&
                        !candidate.Value.Coordinate.Equals(nearEnemy))
                        .Select(candidate => candidate.Value.Coordinate).ToArray();
                Assert.That(moveSpaces, Is.Not.Empty,
                    face.Value + " from " + nearEnemy + " choices=" +
                    string.Join(" | ", runtime.PendingEffectChoice.Candidates.Select(candidate =>
                        candidate.Value.ToString())) + " " + Remaining(runtime));
                var destination = TakahideUiJourneyPlayModeTests.ReachableSpace(root, moveSpaces);
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(destination), "move Illusion beside enemy");
                Assert.That(runtime.Projection.PlacedTokens.Single(token =>
                    token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id))
                    .Coordinate, Is.EqualTo(destination));
                Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
                UiPlayer.ClickDefenseDecline(runtime, root.Hud);
                yield return null;
                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(wasp)), Is.False);
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                    Has.None.Contains("EngineFault"), face.Value);
            }
        }

        [UnityTest]
        public IEnumerator TwistFateAndDeviousSchemeTargetADistantUnitThroughTheirOwnAttack()
        {
            foreach (var face in new[] { T.TwistFate, T.DeviousScheme })
            {
                yield return Stage(new[] { face, T.MindGrip },
                    waspAt: new HexCoordinate(-1, 1),
                    enemyHand: new[] { WaspCardContent.MagneticDagger,
                        WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var nebkher = NebKher(runtime);
                var wasp = Wasp(runtime);
                Assert.That(HexDistanceQuery.Calculate(NebKherSpace,
                    HeroView(wasp).Coordinate), Is.EqualTo(2));
                yield return CommitEverySeat(root, nebkher, NameOf(runtime, face));
                yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
                yield return PointTrayAt(root, nebkher);
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                    face.Value + " " + Remaining(runtime));
                Assert.That(UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                    HeroView(wasp).transform.position), Is.True);
                yield return null;
                Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                    face.Value + " " + Remaining(runtime));
                UiPlayer.ClickDefenseDecline(runtime, root.Hud);
                yield return null;
                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(wasp)),
                    Is.False, face.Value);
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                    value.DisplayText), Has.None.Contains("EngineFault"), face.Value);
            }
        }

        [UnityTest]
        public IEnumerator TwistFateAndDeviousSchemeTakeTheirOptionalIllusionSwapAfterAttack()
        {
            foreach (var face in new[] { T.TwistFate, T.DeviousScheme })
            {
                yield return Stage(new[] { T.FleetingImage, face },
                    enemyHand: new[] { WaspCardContent.Shock,
                        WaspCardContent.MagneticDagger });
                var root = Root();
                var runtime = root.Runtime;
                var nebkher = NebKher(runtime);
                var wasp = Wasp(runtime);
                var tokenStart = new HexCoordinate(-3, 0);
                yield return PointTrayAt(root, nebkher);
                yield return CommitThroughTray(root, NameOf(runtime, T.FleetingImage));
                yield return PointTrayAt(root, wasp);
                yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.Shock));
                yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
                yield return PointTrayAt(root, nebkher);
                yield return StartActionThroughControl(root, ActionKind.Skill);
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(tokenStart), "place adjacent swap Illusion");
                if (runtime.PendingEffectChoice != null)
                {
                    yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
                    UiPlayer.ClickEffectControl(runtime, root.Hud,
                        EffectChoiceCommandKind.Decline);
                    yield return null;
                }
                for (var budget = 0; budget < 28 && runtime.TurnIndex == 1; budget++)
                    yield return NextRoundDecision(root);
                Assert.That(runtime.TurnIndex, Is.EqualTo(2), Remaining(runtime));
                yield return PointTrayAt(root, nebkher);
                yield return CommitThroughTray(root, NameOf(runtime, face));
                yield return PointTrayAt(root, wasp);
                yield return CommitThroughTray(root,
                    NameOf(runtime, WaspCardContent.MagneticDagger));
                yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
                yield return PointTrayAt(root, nebkher);
                yield return StartActionThroughControl(root, ActionKind.Attack);
                var enemyMinion = runtime.ReadModel.Minions.Single(minion =>
                    minion.Coordinate.Equals(new HexCoordinate(-4, 2))).MinionId;
                Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds,
                    Has.Member(enemyMinion), Remaining(runtime));
                var view = Object.FindObjectsByType<StandardMinionView>()
                    .Single(value => value.Occupancy != null &&
                        value.Occupancy.MinionId.Equals(enemyMinion));
                Assert.That(UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                    view.transform.position), Is.True);
                yield return null;
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    "The after-attack may-swap must be offered. " + Remaining(runtime));
                yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
                UiPlayer.ClickEffectControl(runtime, root.Hud,
                    EffectChoiceCommandKind.Confirm);
                yield return null;
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    "choose enemy to swap");
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(tokenStart), "choose Illusion to swap");
                Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate,
                    Is.EqualTo(tokenStart));
                Assert.That(runtime.Projection.PlacedTokens.Single(token =>
                    token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id))
                    .Coordinate, Is.EqualTo(WaspBeside));
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                    Has.None.Contains("EngineFault"), face.Value);
            }
        }

        [UnityTest]
        public IEnumerator MindGripChoosesThePrintedAdjacentMinionDefeatOnTheBoard()
        {
            yield return Stage(new[] { T.MindGrip, T.FleetingImage });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            var target = runtime.ReadModel.Minions.FirstOrDefault(minion =>
                minion.TeamSide != runtime.Setup.Heroes.Single(hero =>
                    hero.HeroId.Equals(nebkher)).TeamSide &&
                HexDistanceQuery.Calculate(minion.Coordinate, NebKherSpace) == 1);
            Assert.That(target, Is.Not.Null, "An enemy minion must stand beside NebKher.");
            yield return CommitEverySeat(root, nebkher, NameOf(runtime, T.MindGrip));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            yield return ChooseWindowOption(root, candidate =>
                candidate.OptionNodeId.Value.EndsWith("-minion-arm"),
                "choose Mind Grip's adjacent defeat");
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Minion &&
                    value.MinionId.Equals(target.MinionId), "defeat adjacent minion");
            Assert.That(runtime.ReadModel.Minions.Any(minion =>
                minion.MinionId.Equals(target.MinionId)), Is.False);
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                value.DisplayText), Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator MindGripBorrowsTheEnemysPreviousSecondaryMovementThroughItsRealControls()
        {
            yield return Stage(new[] { T.FleetingImage, T.MindGrip },
                enemyHand: new[] { WaspCardContent.MagneticDagger,
                    WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            var wasp = Wasp(runtime);
            yield return PointTrayAt(root, nebkher);
            yield return CommitThroughTray(root, NameOf(runtime, T.FleetingImage));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root,
                NameOf(runtime, WaspCardContent.MagneticDagger));
            for (var budget = 0; budget < 28 && runtime.TurnIndex == 1; budget++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.TurnIndex, Is.EqualTo(2), Remaining(runtime));
            yield return PointTrayAt(root, nebkher);
            yield return CommitThroughTray(root, NameOf(runtime, T.MindGrip));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root,
                NameOf(runtime, WaspCardContent.Electrocute));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            yield return ChooseWindowOption(root, candidate =>
                candidate.OptionNodeId.Value.EndsWith("-borrow-arm"),
                "borrow an enemy previous-slot action");
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Hero &&
                    value.HeroId.Equals(wasp), "choose previous-slot enemy");
            Assert.That(runtime.PendingEffectChoice?.Candidates.Any(candidate =>
                candidate.Value.Kind == EffectValueKind.Integer &&
                candidate.Value.IntegerValue == 1), Is.True, Remaining(runtime));
            yield return ChooseWindowOption(root, candidate =>
                candidate.Value.Kind == EffectValueKind.Integer &&
                candidate.Value.IntegerValue == 1,
                "borrow the printed secondary Movement");
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            var step = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingMovementChoice.LegalDestinations.Where(value =>
                    value.Cost == 1).Select(value => value.Destination));
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                Tile(step).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(nebkher))
                .Coordinate, Is.EqualTo(step));
            Assert.That(HeroView(nebkher).Coordinate, Is.EqualTo(step));
            Assert.That(runtime.Cards.Single(card => card.OwnerId.Equals(wasp) &&
                card.DefinitionReference.DefinitionId.Equals(
                    WaspCardContent.MagneticDagger)).State.Location,
                Is.EqualTo(CardLocation.TurnArea),
                "Borrowing does not move the enemy's card.");
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                value.DisplayText), Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator EveryUpToImageAndArmyFacePlacesItsFirstIllusionThroughRealControls()
        {
            foreach (var face in new[]
            {
                T.MultipleProjections, T.MasterOfIllusions,
                T.IllusionaryForce, T.IllusionaryArmy
            })
            {
                yield return Stage(new[] { face, T.MindGrip });
                var root = Root();
                var runtime = root.Runtime;
                var nebkher = NebKher(runtime);
                yield return CommitEverySeat(root, nebkher, NameOf(runtime, face));
                yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
                yield return PointTrayAt(root, nebkher);
                yield return StartActionThroughControl(root, ActionKind.Skill);
                for (var budget = 0; budget < 8 && runtime.PendingEffectChoice != null &&
                    !runtime.PendingEffectChoice.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.Coordinate); budget++)
                {
                    var choice = runtime.PendingEffectChoice;
                    Assert.That(choice.ConfirmInvocation, Is.Not.Null,
                        face.Value + " must offer its up-to placement. " + Remaining(runtime));
                    yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
                    UiPlayer.ClickEffectControl(runtime, root.Hud,
                        EffectChoiceCommandKind.Confirm);
                    yield return null;
                    yield return ConfirmIfStillStanding(root, choice.NodeId);
                }
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " " + Remaining(runtime));
                var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                    runtime.PendingEffectChoice.Candidates.Where(candidate =>
                        candidate.Value.Kind == EffectValueKind.Coordinate)
                        .Select(candidate => candidate.Value.Coordinate));
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(space), "place " + face.Value + " Illusion");
                Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                    token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id)),
                    Is.EqualTo(1), face.Value);
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                    value.DisplayText), Has.None.Contains("EngineFault"), face.Value);
            }
        }

        [UnityTest]
        public IEnumerator ForceAndArmyFinishTheirUpToPlacementAndExposeTheActionOnlyOverlay()
        {
            foreach (var face in new[] { T.IllusionaryForce, T.IllusionaryArmy })
            {
                yield return Stage(new[] { face, T.MindGrip });
                var root = Root();
                var runtime = root.Runtime;
                var nebkher = NebKher(runtime);
                yield return CommitEverySeat(root, nebkher, NameOf(runtime, face));
                yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
                yield return PointTrayAt(root, nebkher);
                yield return StartActionThroughControl(root, ActionKind.Skill);
                for (var budget = 0; budget < 8 && runtime.PendingEffectChoice != null &&
                    !runtime.PendingEffectChoice.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.Coordinate); budget++)
                {
                    var choice = runtime.PendingEffectChoice;
                    Assert.That(choice.ConfirmInvocation, Is.Not.Null, Remaining(runtime));
                    yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
                    UiPlayer.ClickEffectControl(runtime, root.Hud,
                        EffectChoiceCommandKind.Confirm);
                    yield return null;
                    yield return ConfirmIfStillStanding(root, choice.NodeId);
                }
                var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                    runtime.PendingEffectChoice.Candidates.Where(candidate =>
                        candidate.Value.Kind == EffectValueKind.Coordinate)
                        .Select(candidate => candidate.Value.Coordinate));
                yield return TakahideUiJourneyPlayModeTests.Pick(root,
                    value => value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(space), "place one " + face.Value + " Illusion");
                for (var budget = 0; budget < 8 && runtime.PendingEffectChoice != null; budget++)
                {
                    var choice = runtime.PendingEffectChoice;
                    Assert.That(choice.DeclineInvocation, Is.Not.Null,
                        face.Value + " must let the player stop placing. " + Remaining(runtime));
                    yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
                    UiPlayer.ClickEffectControl(runtime, root.Hud,
                        EffectChoiceCommandKind.Decline);
                    yield return null;
                }
                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                var source = runtime.Cards.Single(card => card.OwnerId.Equals(nebkher) &&
                    card.DefinitionReference.DefinitionId.Equals(face));
                Assert.That(runtime.IsActiveEffectSource(source.Id), Is.True,
                    face.Value + " action-only minion overlay is readable on its source.");
                Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                    token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id)),
                    Is.EqualTo(1));
                for (var budget = 0; budget < 28 && runtime.TurnIndex == 1; budget++)
                    yield return NextRoundDecision(root);
                Assert.That(runtime.TurnIndex, Is.EqualTo(2), Remaining(runtime));
                Assert.That(runtime.IsActiveEffectSource(source.Id), Is.True,
                    face.Value + " overlay remains readable on the following turn.");
                Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                    Has.None.Contains("EngineFault"), face.Value);
            }
        }

        [UnityTest]
        public IEnumerator StagedNebKherSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedNebKher);
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            var view = HeroView(nebkher);
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo(T.ContentSetId));
            Assert.That(view.HasTokenVisual, Is.True,
                "NebKher's prepared standee face is drawn.");
            Assert.That(view.Coordinate,
                Is.EqualTo(runtime.Heroes.Single(hero => hero.HeroId.Equals(nebkher)).Coordinate));
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(nebkher));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Count(hero =>
                hero.HeroId.Equals(nebkher)), Is.EqualTo(1));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator EveryNebKherFaceKeepsTheStandardCardFootprint()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedNebKher);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "NebKher", T.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator FleetingImagePlacesAnOfficialIllusionThroughTheBoardChoice()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedNebKher);
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            yield return CommitEverySeat(root, nebkher, NameOf(runtime, T.FleetingImage));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.PendingEffectChoice.Candidates.Where(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(space), "place Illusion");
            Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id)), Is.EqualTo(1));

            var officialFace = Resources.Load<Texture2D>("Private/Tokens/nebkher-illusion");
            Assert.That(officialFace, Is.Not.Null,
                "Prepare-TokenArtwork.ps1 -Hero nebkher must provide the private face.");
            var view = runtime.BoardView.TokenViews.Single(token =>
                token.gameObject.activeSelf && token.Token != null &&
                token.Token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id));
            Assert.That(view.GetComponent<MeshRenderer>().sharedMaterial.mainTexture,
                Is.SameAs(officialFace));
            yield return UiPlayer.CaptureGameView("9-54", "nebkher-illusion-official-face");

            var swap = runtime.PendingEffectChoice;
            Assert.That(swap?.ConfirmInvocation, Is.Not.Null,
                "The optional swap remains a real choice after placement. " +
                Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            UiPlayer.ClickEffectControl(runtime, root.Hud,
                EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return ConfirmIfStillStanding(root, swap.NodeId);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(space), "swap with placed Illusion");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(nebkher))
                .Coordinate, Is.EqualTo(space));
            Assert.That(HeroView(nebkher).Coordinate, Is.EqualTo(space));
            Assert.That(runtime.Projection.PlacedTokens.Single(token =>
                token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id))
                .Coordinate, Is.EqualTo(NebKherSpace));
        }

        [UnityTest]
        public IEnumerator IllusionPlacementPauseRestoresAndAnswersOnTheRebuiltBoard()
        {
            yield return Stage(new[] { T.FleetingImage, T.MindGrip });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            yield return CommitEverySeat(root, nebkher,
                NameOf(runtime, T.FleetingImage));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var asked = runtime.PendingEffectChoice;
            Assert.That(asked, Is.Not.Null, Remaining(runtime));
            var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                asked.Candidates.Where(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            var pause = runtime.CaptureSnapshot();
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(space), "first Illusion before restore");
            var after = runtime.CaptureSnapshot();
            Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id)),
                Is.EqualTo(1));

            Assert.That(runtime.RestoreSnapshot(pause), Is.True,
                runtime.LastOperationDetail);
            yield return null;
            yield return null;
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked.NodeId));
            Assert.That(runtime.Projection.PlacedTokens.Count(token =>
                token.Definition.Id.Equals(NebKherComponentContent.Illusion.Id)), Is.Zero);
            Assert.That(runtime.BoardView.HeroViews.Count(view =>
                view.Occupancy.HeroId.Equals(nebkher)), Is.EqualTo(1));
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(space), "first Illusion after restore");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(after));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                value.DisplayText), Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator IllusionPlacementUndoesToItsChoiceAndReplaysExactly()
        {
            yield return Stage(new[] { T.FleetingImage, T.MindGrip });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            yield return CommitEverySeat(root, nebkher,
                NameOf(runtime, T.FleetingImage));
            yield return GarrusUiJourneyTests.WaitForActor(root, nebkher);
            yield return PointTrayAt(root, nebkher);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var asked = runtime.PendingEffectChoice;
            Assert.That(asked, Is.Not.Null, Remaining(runtime));
            var space = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                asked.Candidates.Where(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate));
            var pause = runtime.CaptureSnapshot();
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(space), "first Illusion before Undo");
            var after = runtime.CaptureSnapshot();
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
            yield return null;
            Assert.That(runtime.PendingEffectChoice?.NodeId,
                Is.EqualTo(asked.NodeId), Remaining(runtime));
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;
            yield return TakahideUiJourneyPlayModeTests.Pick(root,
                value => value.Kind == EffectValueKind.Coordinate &&
                    value.Coordinate.Equals(space), "first Illusion after Undo");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(after));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value =>
                value.DisplayText), Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator NebKherFallsRespawnsByBoardClickAndRestartRebuildsHisPiece()
        {
            yield return Stage(new[] { T.MindGrip, T.FleetingImage, T.IllusionaryForce },
                enemyHand: new[]
                    { WaspCardContent.MagneticDagger, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            var standee = HeroView(nebkher);
            Assert.That(standee.HeroContentId.Value.Value, Is.EqualTo(T.ContentSetId));
            Commit(runtime, nebkher, T.MindGrip);
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
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(nebkher)), Is.False);
            Assert.That(standee.gameObject.activeSelf, Is.False);
            Assert.That(runtime.BoardView.HeroDefeatGhostFor(standee), Is.Not.Null);

            yield return YieldUntilTheNamedSeatActs(runtime, nebkher);
            CommitSoTheNamedSeatActs(runtime, nebkher, T.FleetingImage);
            yield return YieldUntilTheNamedSeatActs(runtime, nebkher);
            var respawn = TakahideUiJourneyPlayModeTests.ReachableSpace(root,
                runtime.RoundView.Model.HeroRespawnOptions.Select(option => option.Destination));
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, Tile(respawn).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(nebkher)).Coordinate,
                Is.EqualTo(respawn));
            Assert.That(HeroView(nebkher).Coordinate, Is.EqualTo(respawn));

            yield return UiPlayer.WaitForControl(root.Hud, "DIRECTOR");
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(nebkher)).Coordinate,
                Is.EqualTo(NebKherSpace));
            Assert.That(HeroView(nebkher).Coordinate, Is.EqualTo(NebKherSpace));
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }

        [UnityTest]
        public IEnumerator NebKhersFiveCardHandStaysReachableAtTheShortDesktopViewport()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedNebKher);
            var root = Root();
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            UiPlayer.SetPlayerViewport(2560, 540, "NebKher short desktop viewport");
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
                "restore viewport after NebKher short-desktop check");
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
                "The five-card hand remains pointer-reachable without horizontal travel.");
            Assert.That((uint)Screen.width, Is.EqualTo(restoreWidth));
            Assert.That((uint)Screen.height, Is.EqualTo(restoreHeight));
        }

        [UnityTest]
        public IEnumerator NebKhersLevelUpAddsTheChosenCardAndShowsTheSiblingItem()
        {
            yield return Stage(new[] { T.MindGrip, T.FleetingImage });
            var root = Root();
            var runtime = root.Runtime;
            var nebkher = NebKher(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(nebkher)); budget++)
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
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(nebkher));
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
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(nebkher));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value,
                Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(nebkher))
                .ItemBonuses, Is.Not.Empty);
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
        }
    }
}
