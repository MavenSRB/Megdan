using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
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
            Assert.That(selectors, Has.Length.EqualTo(2));
            Assert.That(selectors, Has.All.Matches<PrimitiveHeroPointerSelector>(selector =>
                selector.GetComponent<PrimitiveHeroView>() != null &&
                selector.GetComponent<CapsuleCollider>() != null));
            Assert.That(heroes, Has.All.Matches<PrimitiveHeroView>(hero =>
                hero.GetComponent<MeshRenderer>().enabled &&
                hero.GetComponent<MeshFilter>().sharedMesh != null));
        }

        [UnityTest]
        public IEnumerator Scene_RoutesConfiguredMinionAttackCuePointerAndRemovalThroughFeatureBoundary()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var transientSetupField = ReflectedMember.RequiredInstanceField(
                typeof(StandardMinionView),
                "occupancy");
            foreach (var configuredMinion in Object.FindObjectsByType<StandardMinionView>())
            {
                transientSetupField.SetValue(configuredMinion, null);
            }

            mapView.Rebuild(mapView.RenderedDefinition, runtime.Setup);
            yield return null;

            var reboundViews = Object.FindObjectsByType<StandardMinionView>();
            Assert.That(reboundViews, Has.Length.EqualTo(12));
            Assert.That(runtime.BoardView.MinionViews, Is.EquivalentTo(reboundViews));
            Assert.That(pointerInput.MinionSelectors.Select(value =>
                value.GetComponent<StandardMinionView>()), Is.EquivalentTo(reboundViews));
            var redHeroId = new GuardsDigital.Core.Units.Heroes.HeroId(
                PlaceholderOneVsOneParticipants.RedHeroIdValue);

            for (var approachTurn = 0; approachTurn < 3; approachTurn++)
            {
                var red = Hand(runtime, TeamSide.Red).Cards
                    .OrderByDescending(card => runtime.Definitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                    .First();
                var blue = Hand(runtime, TeamSide.Blue).Cards
                    .OrderBy(card => runtime.Definitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                    .First();
                Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
                Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
                if (runtime.PendingInitiativeChoice != null)
                {
                    Assert.That(runtime.SubmitInitiativeChoice(red.Id), Is.True);
                }

                if (!runtime.SelectedActor.ActorId.Equals(redHeroId))
                {
                    Assert.That(runtime.SubmitEndTurn(), Is.True,
                        "The earlier opposing actor should complete by Hold before the Red approach action.");
                }

                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
                var movement = runtime.RoundView.Model.Actions.Single(value =>
                    ActionKind(runtime, value.Command.ActionDefinitionId) ==
                    GuardsDigital.Core.Cards.ActionKind.Movement);
                Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
                while (runtime.PendingMovementChoice != null)
                {
                    var blueMinions = runtime.BoardView.Model.Minions
                        .Where(value => value.TeamSide == TeamSide.Blue &&
                            value.MinionType != StandardMinionType.Heavy)
                        .ToArray();
                    var destination = runtime.PendingMovementChoice.LegalDestinations
                        .OrderBy(value => blueMinions.Min(minion =>
                            HexDistance(value.Destination, minion.Coordinate)))
                        .ThenByDescending(value => value.Cost)
                        .First().Destination;
                    Assert.That(runtime.BoardPresenter.SubmitDestination(destination), Is.True);
                }

                if (runtime.IsTurnActive)
                {
                    Assert.That(runtime.SelectedActor.ActorId, Is.Not.EqualTo(redHeroId));
                    Assert.That(runtime.SubmitEndTurn(), Is.True,
                        "The later opposing actor should complete by Hold after the approach action.");
                }
            }

            var attackCard = Hand(runtime, TeamSide.Red).Cards
                .Where(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).ActionDefinitions.Any(action =>
                        action.Kind == GuardsDigital.Core.Cards.ActionKind.Attack &&
                        action.AttackRange == AttackRangeKind.Ranged))
                .OrderByDescending(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                .First();
            var finalBlueCard = Hand(runtime, TeamSide.Blue).Cards
                .OrderBy(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                .First();
            Assert.That(runtime.SelectCommitmentCard(attackCard.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(finalBlueCard.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            if (runtime.PendingInitiativeChoice != null)
            {
                Assert.That(runtime.SubmitInitiativeChoice(attackCard.Id), Is.True);
            }

            if (!runtime.SelectedActor.ActorId.Equals(redHeroId))
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "The earlier opposing actor should complete by Hold before the Red Attack.");
            }

            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
            var attack = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Is.Not.Empty);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.First();
            var minion = Object.FindObjectsByType<StandardMinionView>()
                .Single(value => value.Occupancy.MinionId.Equals(targetId));
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(minion.Occupancy.Coordinate));
            Assert.That(runtime.BoardView.Model.MinionAttackTargets, Does.Contain(targetId));
            Assert.That(tile.IsHighlighted, Is.True,
                "The exact projected minion occupancy must receive the configured target cue.");
            Assert.That(minion.GetComponent<StandardMinionPointerSelector>(), Is.Not.Null);
            var pendingTargetSnapshot = runtime.CaptureSnapshot();
            var commandCount = runtime.AcceptedCommands.Count;
            Assert.That(pointerInput.TryHandlePointerRay(DownRay(minion.transform.position)), Is.True);
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandCount + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseAttackTarget));
            Assert.That(runtime.AcceptedCommands.Last().MinionId, Is.EqualTo(targetId));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(attackCard.Id)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.False);
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(runtime.DiagnosticEvents.Select(value => value.EventType), Does.Contain(
                "Action.MinionDefeatRecorded"));
            Assert.That(runtime.DiagnosticEvents.Select(value => value.EventType), Does.Contain(
                "Action.MinionRewardAwarded"));
            Assert.That(runtime.DiagnosticEvents.Select(value => value.EventType), Does.Contain(
                "Action.MinionRemovalCompleted"));

            Assert.That(runtime.Undo(), Is.True);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Does.Contain(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.True);
            Assert.That(tile.IsHighlighted, Is.True);

            Assert.That(runtime.Restart(), Is.True);
            yield return null;
            Assert.That(runtime.BoardView.Model.Minions, Has.Count.EqualTo(12));
            Assert.That(runtime.BoardView.MinionViews.Count, Is.EqualTo(12));
            Assert.That(pointerInput.MinionSelectors.Count, Is.EqualTo(12));

            Assert.That(runtime.RestoreSnapshot(pendingTargetSnapshot), Is.True, runtime.LastOperationDetail);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Does.Contain(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.True);
            Assert.That(tile.IsHighlighted, Is.True);
            Assert.That(pointerInput.TryHandlePointerRay(DownRay(minion.transform.position)), Is.True);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.False);
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
                Is.EqualTo("FAST TRAVEL"));
            Assert.That(runtime.RoundView.Model.Actions, Is.Not.Empty);
            Assert.That(runtime.BoardView.Model.FastTravelDestinations, Is.Empty,
                "Fast Travel must not be the configured board's default interaction state.");
            Assert.That(Object.FindObjectsByType<HexTileView>(),
                Has.All.Matches<HexTileView>(value => !value.IsHighlighted));

            runtime.RoundView.ForwardFastTravelSelection(
                runtime.RoundView.Model.FastTravelOffers.Single());
            yield return null;

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
        public IEnumerator Scene_PassiveDefensePreviewAndSignedMinionCuesUseConfiguredViews()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var supporting = runtime.BoardView.Model.Minions.First(value => value.TeamSide == TeamSide.Blue);
            var hindering = runtime.BoardView.Model.Minions.First(value => value.TeamSide == TeamSide.Red);
            var baseBoard = runtime.BoardView.Model;
            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 1,
                baseBoard.Heroes,
                baseBoard.Minions,
                baseBoard.MovementDestinations,
                baseBoard.AttackTargets,
                System.Array.Empty<MinionId>(),
                baseBoard.FocusedHeroId,
                baseBoard.FastTravelDestinations,
                new[]
                {
                    new MinionDefenseCueViewModel(supporting.MinionId, 1),
                    new MinionDefenseCueViewModel(hindering.MinionId, -1)
                }));

            var supportingView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(supporting.MinionId));
            var hinderingView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(hindering.MinionId));
            Assert.That(supportingView.DefenseCueAmount, Is.EqualTo(1));
            var supportingCue = supportingView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Defense Contribution Cue");
            var supportingBacking = supportingView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Defense Contribution Cue Background");
            Assert.That(supportingCue.text, Is.EqualTo("+1"));
            Assert.That(supportingCue.characterSize, Is.EqualTo(StandardMinionView.DefenseCueCharacterSize));
            Assert.That(supportingCue.transform.localScale, Is.EqualTo(new Vector3(-1f, 1f, 1f)));
            Assert.That(supportingBacking.transform.localScale.y,
                Is.EqualTo(StandardMinionView.DefenseCueBackgroundVerticalScale));
            Assert.That(supportingBacking.transform.localScale.y,
                Is.GreaterThan(supportingBacking.transform.localScale.x));
            var supportingCenterDelta = supportingView.transform.InverseTransformVector(
                supportingCue.GetComponent<MeshRenderer>().bounds.center -
                supportingBacking.GetComponent<MeshRenderer>().bounds.center);
            Assert.That(supportingCenterDelta.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(supportingCenterDelta.y,
                Is.EqualTo(StandardMinionView.DefenseCueOpticalVerticalOffset).Within(0.0001f));
            Assert.That(hinderingView.DefenseCueAmount, Is.EqualTo(-1));
            var hinderingCue = hinderingView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Defense Contribution Cue");
            var hinderingBacking = hinderingView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Defense Contribution Cue Background");
            Assert.That(hinderingCue.text, Is.EqualTo("-1"));
            Assert.That(hinderingCue.characterSize, Is.EqualTo(StandardMinionView.DefenseCueCharacterSize));
            Assert.That(hinderingCue.transform.localScale, Is.EqualTo(new Vector3(-1f, 1f, 1f)));
            Assert.That(hinderingBacking.transform.localScale.y,
                Is.EqualTo(StandardMinionView.DefenseCueBackgroundVerticalScale));
            Assert.That(hinderingBacking.transform.localScale.y,
                Is.GreaterThan(hinderingBacking.transform.localScale.x));
            var hinderingCenterDelta = hinderingView.transform.InverseTransformVector(
                hinderingCue.GetComponent<MeshRenderer>().bounds.center -
                hinderingBacking.GetComponent<MeshRenderer>().bounds.center);
            Assert.That(hinderingCenterDelta.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(hinderingCenterDelta.y,
                Is.EqualTo(StandardMinionView.DefenseCueOpticalVerticalOffset).Within(0.0001f));

            var resolutionId = new ActionResolutionId("configured-preview-resolution");
            var choiceId = new ActionResolutionStepId("configured-preview-choice");
            var cardBreakdown = new DefenseBreakdownViewModel(
                12,
                resolutionId,
                choiceId,
                Hand(runtime, TeamSide.Blue).Cards[0].Id,
                "TEST DEFENSE CARD DEFENSE",
                "ATTACK:\n  +3 — Test Attack Card\nATTACK TOTAL: 3\n\n" +
                "DEFENSE:\n  CARD: TEST DEFENSE CARD\n  +3 — Test Defense Card\n" +
                "DEFENSE TOTAL: 3\nRESULT: DEFENDED",
                AttackResolutionOutcome.SuccessfullyDefended);
            var declineBreakdown = new DefenseBreakdownViewModel(
                12,
                resolutionId,
                choiceId,
                null,
                "DECLINE PREVIEW",
                "ATTACK:\n  +3 — Test Attack Card\nATTACK TOTAL: 3\n\n" +
                "DEFENSE:\n  NO DEFENSE TOTAL\nRESULT: NO DEFENSE TOTAL - ATTACK SUCCEEDS",
                AttackResolutionOutcome.AttackSuccessful);
            var currentRound = runtime.RoundView.Model;
            var cardInvocation = new TypedInvocationViewModel(
                "DISCARD TEST DEFENSE CARD\nPRINTED DEFENSE 3",
                DirectorCommand.ChooseDefenseCard(Hand(runtime, TeamSide.Blue).Cards[0].Id),
                cardBreakdown);
            var declineInvocation = new TypedInvocationViewModel(
                "DECLINE DEFENSE",
                DirectorCommand.DeclineDefense(),
                declineBreakdown);
            runtime.RoundView.Render(new RoundActionViewModel(
                12,
                currentRound.RoundIndex,
                currentRound.TurnIndex,
                currentRound.Phase,
                "Defense response pending against Attack 3.",
                currentRound.Actions,
                currentRound.EndTurnInvocation,
                currentRound.ContinueEndOfRoundInvocation,
                currentRound.InitiativeChoices,
                new[] { cardInvocation, declineInvocation },
                currentRound.FastTravelOffers));

            runtime.RoundView.ShowDefensePreview(cardBreakdown, new Rect(120f, 220f, 190f, 46f));
            yield return null;
            Assert.That(runtime.RoundView.ActiveDefensePreview.Text, Does.Contain("DEFENSE TOTAL: 3"));
            runtime.RoundView.ShowDefensePreview(declineBreakdown, new Rect(320f, 220f, 190f, 46f));
            yield return null;
            Assert.That(runtime.RoundView.ActiveDefensePreview.Text,
                Does.Contain("NO DEFENSE TOTAL - ATTACK SUCCEEDS"));
            Assert.That(supportingView.DefenseCueAmount, Is.EqualTo(1),
                "Moving between Defense choices must not rebuild attack-scoped cues.");
            Assert.That(hinderingView.DefenseCueAmount, Is.EqualTo(-1));

            if (!UnityEngine.Application.isBatchMode)
            {
                var screenshotPath = Path.GetFullPath("tmp/milestone-6-1b-defense-preview.png");
                if (File.Exists(screenshotPath))
                {
                    File.Delete(screenshotPath);
                }
                ScreenCapture.CaptureScreenshot(screenshotPath);
                yield return new WaitForEndOfFrame();
                yield return null;
                Assert.That(File.Exists(screenshotPath), Is.True,
                    "The targeted Game View visual evidence must be captured under tmp/.");
            }
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
            AssertCurrentMinionBindings(first);

            yield return LoadScene();
            yield return null;
            var second = Root();
            Assert.That(second.OwnedHighlightNativeResourceCount, Is.EqualTo(1));
            Assert.That(CountHighlightMaterials(), Is.EqualTo(1));
            AssertCurrentMinionBindings(second);
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

        private static int HexDistance(HexCoordinate first, HexCoordinate second)
        {
            var deltaQ = first.Q - second.Q;
            var deltaR = first.R - second.R;
            return (System.Math.Abs(deltaQ) + System.Math.Abs(deltaR) +
                System.Math.Abs(deltaQ + deltaR)) / 2;
        }

        private static Ray DownRay(Vector3 position) =>
            new Ray(position + (Vector3.up * 2f), Vector3.down);

        private static int CountHighlightMaterials() =>
            Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null && value.name == DestinationHighlightMaterialOwner.GeneratedMaterialName);

        private static void AssertCurrentMinionBindings(MatchSceneCompositionRoot root)
        {
            var views = Object.FindObjectsByType<StandardMinionView>();
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(views, Has.Length.EqualTo(12));
            Assert.That(root.Runtime.BoardView.MinionViews, Is.EquivalentTo(views));
            Assert.That(pointerInput.MinionSelectors.Count, Is.EqualTo(12));
            Assert.That(pointerInput.MinionSelectors.Select(value =>
                value.GetComponent<StandardMinionView>()), Is.EquivalentTo(views));
        }
    }
}
