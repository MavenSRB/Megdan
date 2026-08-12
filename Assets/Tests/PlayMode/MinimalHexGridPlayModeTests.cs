using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class MinimalHexGridPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_LoadsCompleteSelectedMapPresentationUnderFramedFixedCamera()
        {
            yield return LoadScene();

            var tiles = Object.FindObjectsByType<HexTileView>();
            var markers = Object.FindObjectsByType<BoardMapSpawnMarkerView>();
            var cameras = Object.FindObjectsByType<Camera>();
            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            var minions = Object.FindObjectsByType<StandardMinionView>();
            var root = Root();

            Assert.That(Object.FindObjectsByType<HeroSelectionController>(), Is.Empty);
            Assert.That(root.Runtime.Setup.WaveCount, Is.EqualTo(3));
            Assert.That(root.Runtime.Setup.Teams,
                Has.All.Matches<QuickGameTeamSetupState>(team => team.LifeCount == 4));
            Assert.That(mapView.RenderedDefinition.Reference, Is.EqualTo(OfficialBoardMapCatalog.SelectedMap));
            Assert.That(tiles, Has.Length.EqualTo(292));
            Assert.That(tiles.Count(tile => tile.IsPlayable), Is.EqualTo(210));
            Assert.That(tiles.Count(tile => tile.SpaceKind == BoardMapSpaceKind.Terrain), Is.EqualTo(82));
            Assert.That(markers, Has.Length.EqualTo(40));
            Assert.That(minions, Has.Length.EqualTo(12));
            Assert.That(minions, Has.All.Matches<StandardMinionView>(view =>
                view.GetComponent<SpriteRenderer>().sprite != null));
            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].orthographic, Is.True);
            Assert.That(tiles.All(tile =>
            {
                var viewport = cameras[0].WorldToViewportPoint(tile.transform.position);
                return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            }), Is.True);
        }

        [UnityTest]
        public IEnumerator Scene_RendersHeaderAndBlocksBoardPointerBehindConfiguredHud()
        {
            yield return LoadScene();
            var root = Root();
            var header = root.Hud.Header;
            var side = root.Runtime.TieBreakerState.RevealedSide;
            var indicator = root.Hud.TieBreakerIndicatorRect;

            Assert.That(header.TieBreaker, Is.EqualTo(root.Runtime.Setup.TieBreakerState));
            Assert.That(header.Text, Does.Contain(side == TeamSide.Red ? "[R] RED" : "[B] BLUE"));
            Assert.That(header.Text, Does.Contain("WAVES 3"));
            Assert.That(header.Text, Does.Contain("LIVES  [R] 4  [B] 4"));
            Assert.That(indicator.xMax, Is.EqualTo(Screen.width - 12f).Within(0.01f));
            Assert.That(indicator.yMin, Is.EqualTo(12f).Within(0.01f));
            var guiPoint = indicator.center;
            Assert.That(root.Hud.BlocksPointerAt(new Vector2(guiPoint.x, Screen.height - guiPoint.y)), Is.True);

        }

        [UnityTest]
        public IEnumerator Scene_KeepsConfiguredCardPanelInsideRuntimeViewport()
        {
            yield return LoadScene();

            var panel = Root().Hud.CardPanelRect;
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(Screen.width));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(Screen.height));
            Assert.That(panel.width,
                Is.EqualTo(Mathf.Min(1280f, Mathf.Max(0f, Screen.width - 24f))).Within(0.01f));
            Assert.That(panel.height,
                Is.EqualTo(Mathf.Min(360f, Mathf.Max(0f, Screen.height - 24f))).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Scene_LoadsVisibleConfiguredHeroesAndPassiveBoardCallbacks()
        {
            yield return LoadScene();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();
            var selectors = Object.FindObjectsByType<PrimitiveHeroPointerSelector>();

            Assert.That(heroes, Has.Length.EqualTo(2));
            Assert.That(heroes.Select(hero => hero.Occupancy.HeroId.Value), Is.EquivalentTo(new[]
            {
                PlaceholderOneVsOneParticipants.RedHeroIdValue,
                PlaceholderOneVsOneParticipants.BlueHeroIdValue
            }));
            Assert.That(selectors, Has.All.Matches<PrimitiveHeroPointerSelector>(selector =>
                selector.SelectionController == null));
            Assert.That(heroes, Has.All.Matches<PrimitiveHeroView>(hero =>
                hero.GetComponent<MeshRenderer>().enabled &&
                hero.GetComponent<MeshFilter>().sharedMesh != null));
        }

        [UnityTest]
        public IEnumerator Scene_CommitsBothHandsAndResolvesHoldThroughFeaturePresenters()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            var blue = Hand(runtime, TeamSide.Blue).Cards[1];

            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.GetCommitmentStatus(red.OwnerId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            yield return null;

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.Setup.Heroes, Has.All.Matches<QuickGameHeroSetupState>(value =>
                runtime.GetCommitmentStatus(value.HeroId) == TurnCardCommitmentStatus.Revealed));
            Assert.That(runtime.RoundView.Model.EndTurnInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.PerformHold));
            var firstActor = runtime.SelectedActor.CardInstanceId;
            Assert.That(runtime.SubmitEndTurn(), Is.True);
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(firstActor)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.Not.EqualTo(firstActor));
        }

        [UnityTest]
        public IEnumerator Scene_RoutesMovementDestinationAndStopThroughBoardViewPresenter()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            CommitNextTurn(runtime);
            var actorId = runtime.SelectedActor.ActorId;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null);
            Assert.That(runtime.RoundView.Model.EndTurnInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.StopMovement));

            var destination = runtime.PendingMovementChoice.LegalDestinations
                .First(value => value.Cost == 1).Destination;
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var commandCount = runtime.AcceptedCommands.Count;
            Assert.That(pointer.TryHandlePointerRay(DownRay(tile.transform.position)), Is.True);
            Assert.That(runtime.AcceptedCommands.Count, Is.EqualTo(commandCount + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseMovementDestination));
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                Assert.That(runtime.AcceptedCommands.Last().Kind,
                    Is.EqualTo(DirectorCommandKind.StopMovement));
            }

            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(runtime.BoardView.Model.MovementDestinations, Is.Empty);
            var fastTravelDestinations = runtime.BoardView.Model.FastTravelDestinations;
            Assert.That(Object.FindObjectsByType<HexTileView>(),
                Has.All.Matches<HexTileView>(value =>
                    value.IsHighlighted == fastTravelDestinations.Contains(value.Coordinate)));
        }

        [UnityTest]
        public IEnumerator Scene_OffersAndRoutesFastTravelThroughConfiguredPassiveViews()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitNextTurn(runtime);
            yield return null;

            var actorId = runtime.SelectedActor.ActorId;
            var sourceCardId = runtime.SelectedActor.CardInstanceId;
            Assert.That(runtime.RoundView.Model.FastTravelOffers, Has.Count.EqualTo(1));
            Assert.That(runtime.RoundView.Model.FastTravelOffers[0].Label,
                Is.EqualTo("REPLACEMENT FAST TRAVEL\nSELECT A HIGHLIGHTED SPACE"));
            Assert.That(runtime.BoardView.Model.FastTravelDestinations,
                Has.Count.EqualTo(runtime.RoundView.Model.FastTravelOffers[0].LegalDestinationCount));

            var destination = runtime.BoardView.Model.FastTravelDestinations[0];
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.True);
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var commandCount = runtime.AcceptedCommands.Count;

            Assert.That(pointer.TryHandlePointerRay(DownRay(tile.transform.position)), Is.True);
            yield return null;

            Assert.That(runtime.AcceptedCommands.Count, Is.EqualTo(commandCount + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.PerformFastTravel));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(sourceCardId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [UnityTest]
        public IEnumerator Scene_RejectsUnavailableAttackWithoutMutationThroughRoundPresenter()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitNextTurn(runtime);
            var attack = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Attack);
            var snapshot = runtime.CaptureSnapshot();
            var commandCount = runtime.AcceptedCommands.Count;

            Assert.That(runtime.RoundPresenter.Submit(attack), Is.False);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandCount));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.PendingDefenseChoice, Is.Null);
        }

        [UnityTest]
        public IEnumerator Scene_PassiveRoundViewPreservesFourPrintedDefenseInvocationsAndDeclineCommands()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var defenseCards = Hand(runtime, TeamSide.Blue).Cards.Take(4).ToArray();
            var invocations = new[]
            {
                new TypedInvocationViewModel(
                    "DISCARD TEST - BLUE FEINT\nPRINTED DEFENSE 2",
                    DirectorCommand.ChooseDefenseCard(defenseCards[0].Id)),
                new TypedInvocationViewModel(
                    "DISCARD TEST - BLUE SENTINEL\nPRINTED DEFENSE 4",
                    DirectorCommand.ChooseDefenseCard(defenseCards[1].Id)),
                new TypedInvocationViewModel(
                    "DISCARD TEST - BLUE SCOUT SHOT\nPRINTED DEFENSE 1",
                    DirectorCommand.ChooseDefenseCard(defenseCards[2].Id)),
                new TypedInvocationViewModel(
                    "DISCARD TEST - BLUE HAMMER\nPRINTED DEFENSE 1",
                    DirectorCommand.ChooseDefenseCard(defenseCards[3].Id))
            };
            var declineCommand = DirectorCommand.DeclineDefense();
            var declineInvocation = new TypedInvocationViewModel(
                "DECLINE DEFENSE",
                declineCommand);
            var choices = invocations.Concat(new[] { declineInvocation }).ToArray();
            var model = runtime.RoundView.Model;
            TypedInvocationViewModel submitted = null;
            runtime.RoundView.InvocationSubmitted += value => submitted = value;
            runtime.RoundView.Render(new RoundActionViewModel(
                model.Version,
                model.RoundIndex,
                model.TurnIndex,
                model.Phase,
                model.Status,
                model.Actions,
                model.EndTurnInvocation,
                model.ContinueEndOfRoundInvocation,
                model.InitiativeChoices,
                choices));

            yield return null;

            Assert.That(runtime.RoundView.Model.DefenseChoices, Has.Count.EqualTo(5));
            Assert.That(runtime.RoundView.Model.DefenseChoices[2].Label,
                Is.EqualTo("DISCARD TEST - BLUE SCOUT SHOT\nPRINTED DEFENSE 1"));
            Assert.That(runtime.RoundView.Model.DefenseChoices[4].Label, Is.EqualTo("DECLINE DEFENSE"));
            foreach (var invocation in invocations)
            {
                runtime.RoundView.ForwardInvocation(invocation);
                Assert.That(submitted, Is.SameAs(invocation));
                Assert.That(submitted.Command.Kind, Is.EqualTo(DirectorCommandKind.ChooseDefenseCard));
                Assert.That(submitted.Command.CardInstanceId, Is.EqualTo(invocation.Command.CardInstanceId));
            }

            runtime.RoundView.ForwardInvocation(declineInvocation);
            Assert.That(submitted, Is.SameAs(declineInvocation));
            Assert.That(submitted.Command, Is.EqualTo(declineCommand));
        }

        [UnityTest]
        public IEnumerator Scene_UndoRestartRestoreAndDiagnosticsStayEquivalent()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var initial = runtime.CaptureSnapshot();
            Assert.That(runtime.DiagnosticEvents.Single().EventType,
                Is.EqualTo("Setup.MatchInitialized"));
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            var blue = Hand(runtime, TeamSide.Blue).Cards[1];
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            var afterRed = runtime.CaptureSnapshot();
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            var revealed = runtime.CaptureSnapshot();
            var revealedEvents = runtime.DiagnosticEvents.Select(value => value.DisplayText).ToArray();

            runtime.DiagnosticPresenter.Toggle();
            Assert.That(runtime.DiagnosticView.Model.IsExpanded, Is.True);
            Assert.That(runtime.Undo(), Is.True);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(afterRed));
            Assert.That(runtime.Restart(), Is.True);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(initial));
            Assert.That(runtime.RestoreSnapshot(revealed), Is.True, runtime.LastOperationDetail);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(revealed));
            Assert.That(runtime.DiagnosticEvents.Select(value => value.DisplayText),
                Is.EqualTo(revealedEvents));
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_PlaysTwoCompleteRoundsThroughConfiguredFeatureBoundary()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;

            for (var round = 1; round <= 2; round++)
            {
                for (var turn = 1; turn <= 4; turn++)
                {
                    CommitNextTurn(runtime);
                    Assert.That(runtime.SubmitEndTurn(), Is.True);
                    Assert.That(runtime.SubmitEndTurn(), Is.True);
                }

                Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
                while (runtime.Phase == RoundResolutionPhase.EndOfRound)
                {
                    Assert.That(runtime.ContinueEndOfRound(), Is.True);
                }

                Assert.That(runtime.RoundIndex, Is.EqualTo(round + 1));
            }

            Assert.That(runtime.CompletedRounds.Select(value => value.RoundIndex),
                Is.EqualTo(new[] { 1, 2 }));
        }

        [UnityTest]
        public IEnumerator SceneReload_ReleasesAndReusesExactlyOneOwnedHighlightMaterial()
        {
            yield return LoadScene();
            var first = Root();
            Assert.That(first.OwnsGeneratedHighlightMaterial, Is.True);
            Assert.That(first.OwnedHighlightNativeResourceCount, Is.EqualTo(1));
            Assert.That(CountHighlightMaterials(), Is.EqualTo(1));

            yield return LoadScene();
            yield return null;
            var second = Root();
            Assert.That(second.OwnedHighlightNativeResourceCount, Is.EqualTo(1));
            Assert.That(CountHighlightMaterials(), Is.EqualTo(1));
        }

        private static IEnumerator LoadScene()
        {
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        private static DirectorHandViewModel Hand(ConfiguredMatchSceneRuntime runtime, TeamSide side) =>
            runtime.HandsView.Model.Hands.Single(value => value.TeamSide == side);

        private static void CommitNextTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            var blue = Hand(runtime, TeamSide.Blue).Cards[0];
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            Assert.That(runtime.IsTurnActive, Is.True);
        }

        private static void ResolveInitiativeChoice(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.PendingInitiativeChoice != null)
            {
                Assert.That(runtime.SubmitInitiativeChoice(
                    runtime.PendingInitiativeChoice.CandidateCardInstanceIds[0]), Is.True);
            }
        }

        private static GuardsDigital.Core.Cards.ActionKind ActionKind(
            ConfiguredMatchSceneRuntime runtime,
            ActionDefinitionId actionId)
        {
            var offer = runtime.AvailableTurnActions.Single(value =>
                value.ActionDefinitionId.Equals(actionId));
            return runtime.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(actionId)).Kind;
        }

        private static Ray DownRay(Vector3 position) =>
            new Ray(position + (Vector3.up * 2f), Vector3.down);

        private static int CountHighlightMaterials() =>
            Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null && value.name == DestinationHighlightMaterialOwner.GeneratedMaterialName);
    }
}
