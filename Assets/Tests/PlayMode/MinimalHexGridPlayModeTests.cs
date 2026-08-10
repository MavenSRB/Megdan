using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class MinimalHexGridPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_LoadsExpandedVisibleHexTilesUnderFixedCamera()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

            Assert.That(tiles, Has.Length.EqualTo(19));
            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(tiles, Has.All.Matches<HexTileView>(tile =>
            {
                var edgeRenderer = tile.GetComponentInChildren<LineRenderer>();
                return tile.GetComponent<MeshRenderer>().enabled &&
                       tile.GetComponent<MeshFilter>().sharedMesh != null &&
                       edgeRenderer != null &&
                       edgeRenderer.enabled;
            }));
        }

        [UnityTest]
        public IEnumerator Scene_RendersTopRightAuthoritativeTieBreakerIndicator()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var side = controller.CurrentTieBreakerState.RevealedSide;
            var indicatorRect = controller.TieBreakerIndicatorRect;

            Assert.That(side, Is.EqualTo(TeamSide.Red).Or.EqualTo(TeamSide.Blue));
            Assert.That(
                controller.TieBreakerIndicatorText,
                Does.Contain(side == TeamSide.Red ? "[R] RED" : "[B] BLUE"));
            Assert.That(indicatorRect.xMax, Is.EqualTo(Screen.width - 12f).Within(0.01f));
            Assert.That(indicatorRect.yMin, Is.EqualTo(12f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Scene_LoadsVisibleRedAndBluePrimitiveHeroes()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);

            Assert.That(heroes, Has.Length.EqualTo(2));
            Assert.That(
                heroes.Select(hero => hero.Occupancy.HeroId.Value),
                Is.EquivalentTo(new[]
                {
                    PlaceholderOneVsOneParticipants.RedHeroIdValue,
                    PlaceholderOneVsOneParticipants.BlueHeroIdValue
                }));
            Assert.That(heroes, Has.All.Matches<PrimitiveHeroView>(hero =>
                hero.GetComponent<MeshRenderer>().enabled &&
                hero.GetComponent<MeshFilter>().sharedMesh != null));

            var redHero = FindRedHero(heroes);
            var blueHero = heroes.Single(hero => hero != redHero);
            Assert.That(redHero.TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(blueHero.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(redHero.GetComponent<MeshRenderer>().sharedMaterial.color.r,
                Is.GreaterThan(redHero.GetComponent<MeshRenderer>().sharedMaterial.color.b));
            Assert.That(blueHero.GetComponent<MeshRenderer>().sharedMaterial.color.b,
                Is.GreaterThan(blueHero.GetComponent<MeshRenderer>().sharedMaterial.color.r));
            Assert.That(redHero.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.Not.EqualTo(blueHero.GetComponent<MeshFilter>().sharedMesh.vertexCount));
        }

        [UnityTest]
        public IEnumerator Scene_ProjectsBothDirectorHandsAndSelectingBlueCardDoesNotPlayIt()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var hands = controller.VisibleDirectorHands;
            var blueHand = hands.Single(hand => hand.TeamSide == TeamSide.Blue);
            var beforeStates = hands.SelectMany(hand => hand.Instances)
                .ToDictionary(card => card.Id, card => card.State);

            var selected = controller.TrySelectDirectorCard(blueHand.Instances[0].Id);

            Assert.That(hands.Select(hand => hand.Label), Is.EqualTo(new[] { "[R] RED HERO", "[B] BLUE HERO" }));
            Assert.That(selected, Is.True);
            Assert.That(controller.ActiveHeroView.Occupancy.HeroId, Is.EqualTo(blueHand.OwnerId));
            Assert.That(controller.SelectedCardInstanceId, Is.EqualTo(blueHand.Instances[0].Id));
            Assert.That(controller.IsCardResolutionPending, Is.False);
            Assert.That(controller.CardResolutionEvents, Is.Empty);
            Assert.That(controller.VisibleDirectorHands.SelectMany(hand => hand.Instances), Has.All.Matches<CardInstance>(card =>
                card.State.Equals(beforeStates[card.Id])));
        }

        [UnityTest]
        public IEnumerator Scene_ConfirmsBothDirectorCardsFacedownThenRevealsThemTogether()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var redCard = controller.VisibleDirectorHands
                .Single(hand => hand.TeamSide == TeamSide.Red).Instances[0];
            var blueCard = controller.VisibleDirectorHands
                .Single(hand => hand.TeamSide == TeamSide.Blue).Instances[1];

            Assert.That(controller.TrySelectDirectorCard(redCard.Id), Is.True);
            Assert.That(controller.TryConfirmSelectedTurnCard(), Is.True);
            Assert.That(redCard.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));
            Assert.That(controller.GetDirectorCommitmentLabel(TeamSide.Red),
                Is.EqualTo("FACEDOWN COMMITTED"));

            Assert.That(controller.TrySelectDirectorCard(blueCard.Id), Is.True);
            Assert.That(controller.TryConfirmSelectedTurnCard(), Is.True);
            yield return null;

            Assert.That(controller.IsTurnCardRevealComplete, Is.True);
            Assert.That(redCard.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
            Assert.That(blueCard.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
            Assert.That(controller.GetDirectorCommitmentLabel(TeamSide.Red), Does.StartWith("REVEALED:"));
            Assert.That(controller.GetDirectorCommitmentLabel(TeamSide.Blue), Does.StartWith("REVEALED:"));
            Assert.That(controller.CardResolutionEvents, Is.Empty);
            Assert.That(controller.IsVisibleTurnActive, Is.True);
            Assert.That(controller.VisibleTurnActor.CardInstanceId, Is.EqualTo(redCard.Id));
            Assert.That(controller.SelectedCardInstanceId, Is.EqualTo(redCard.Id));
            Assert.That(controller.SelectedCardOffers, Is.Not.Empty);
            Assert.That(controller.CardStatus, Does.Contain("[R] RED"));

            var redHero = controller.ActiveHeroView;
            var redOrigin = redHero.Coordinate;
            Assert.That(controller.TrySelectAction(new ActionDefinitionId("secondary-movement")), Is.True);
            var destination = controller.PendingCardDestinations
                .OrderByDescending(candidate => candidate.Cost)
                .First().Destination;
            var destinationTile = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                .Single(tile => tile.Coordinate.Equals(destination));
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(destinationTile.transform.position)), Is.True);
            Assert.That(redHero.Coordinate, Is.EqualTo(destination));
            Assert.That(redHero.Coordinate, Is.Not.EqualTo(redOrigin));
            if (controller.IsMovementSessionActive)
            {
                Assert.That(controller.TryStopCardMovement(), Is.True);
            }

            Assert.That(redCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(blueCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(controller.VisibleTurnActor.CardInstanceId, Is.EqualTo(blueCard.Id));
            Assert.That(controller.ActiveHeroView.Occupancy.HeroId, Is.EqualTo(blueCard.OwnerId));
            Assert.That(controller.SelectedCardInstanceId, Is.EqualTo(blueCard.Id));
        }

        [UnityTest]
        public IEnumerator Scene_ResolvesRevealedTurnAttackDefenseAndFocusesNextActor()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);
            var redHero = FindRedHero(heroes);
            var blueHero = heroes.Single(hero => hero != redHero);
            var redAttackCard = controller.VisibleDirectorHands
                .Single(hand => hand.TeamSide == TeamSide.Red).Instances[0];
            var blueTurnCard = controller.VisibleDirectorHands
                .Single(hand => hand.TeamSide == TeamSide.Blue).Instances[1];
            var blueDefenseCard = controller.VisibleDirectorHands
                .Single(hand => hand.TeamSide == TeamSide.Blue).Instances[0];
            Assert.That(controller.TrySelectDirectorCard(redAttackCard.Id), Is.True);
            Assert.That(controller.TryConfirmSelectedTurnCard(), Is.True);
            Assert.That(controller.TrySelectDirectorCard(blueTurnCard.Id), Is.True);
            Assert.That(controller.TryConfirmSelectedTurnCard(), Is.True);
            yield return null;

            Assert.That(controller.TrySelectAction(new ActionDefinitionId("primary-attack")), Is.True);
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(blueHero.transform.position)), Is.True);
            yield return null;
            Assert.That(controller.IsDefenseResponsePending, Is.True);
            Assert.That(controller.VisibleTurnActor.CardInstanceId, Is.EqualTo(redAttackCard.Id));
            Assert.That(controller.TrySubmitDefenseCard(blueDefenseCard.Id), Is.True);
            yield return null;

            Assert.That(blueDefenseCard.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(redAttackCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(blueTurnCard.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(controller.PendingAttackResolution, Is.Null);
            Assert.That(controller.VisibleTurnActor.CardInstanceId, Is.EqualTo(blueTurnCard.Id));
            Assert.That(controller.ActiveHeroView, Is.SameAs(blueHero));
            Assert.That(controller.SelectedCardInstanceId, Is.EqualTo(blueTurnCard.Id));
            Assert.That(controller.CardStatus, Does.Match("successfully defended|attack successful"));
            Assert.That(controller.CardStatus, Does.Contain("[B] BLUE"));
        }

        [UnityTest]
        public IEnumerator Scene_PlaysTwoUninterruptedAuthoritativeRoundsThroughVisibleEndOfRound()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);
            var redHero = FindRedHero(heroes);
            var blueHero = heroes.Single(hero => hero != redHero);
            var red = controller.VisibleDirectorHands
                .Single(hand => hand.TeamSide == TeamSide.Red).Instances.ToArray();
            var blue = controller.VisibleDirectorHands
                .Single(hand => hand.TeamSide == TeamSide.Blue).Instances.ToArray();

            CommitSceneTurn(controller, red[0], blue[0]);
            Assert.That(controller.TrySelectAction(new ActionDefinitionId("primary-attack")), Is.True);
            Assert.That(controller.TrySubmitAttackTarget(blueHero.Occupancy.HeroId), Is.True);
            Assert.That(controller.TrySubmitDefenseCard(blue[4].Id), Is.True);
            Assert.That(controller.TryPerformHold(blue[0].Id), Is.True);
            yield return null;
            Assert.That(blue[4].State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(controller.VisibleTurnIndex, Is.EqualTo(2));

            CommitSceneTurn(controller, red[3], blue[3]);
            Assert.That(controller.TrySelectAction(new ActionDefinitionId("secondary-movement")), Is.True);
            var destination = controller.PendingCardDestinations.First(candidate => candidate.Cost == 1);
            Assert.That(controller.TrySubmitCardDestination(destination.Destination), Is.True);
            if (controller.IsCardMovementResolutionPending)
            {
                Assert.That(controller.TryStopCardMovement(), Is.True);
            }

            Assert.That(controller.TryPerformHold(blue[3].Id), Is.True);
            yield return null;

            CommitSceneTurn(controller, red[1], blue[1]);
            var tieBreakerBeforeDeferredFlip = controller.CurrentTieBreakerState.RevealedSide;
            var firstTiedActor = controller.VisibleTurnActor.CardInstanceId;
            var secondTiedActor = firstTiedActor.Equals(red[1].Id) ? blue[1].Id : red[1].Id;
            Assert.That(controller.TryPerformHold(firstTiedActor), Is.True);
            Assert.That(controller.CurrentTieBreakerState.RevealedSide,
                Is.EqualTo(tieBreakerBeforeDeferredFlip == TeamSide.Red ? TeamSide.Blue : TeamSide.Red));
            Assert.That(controller.TryPerformHold(secondTiedActor), Is.True);

            CommitSceneTurn(controller, red[2], blue[2]);
            Assert.That(controller.TryPerformHold(red[2].Id), Is.True);
            Assert.That(controller.TryPerformHold(blue[2].Id), Is.True);
            yield return null;

            Assert.That(controller.IsVisibleRoundEnded, Is.True);
            Assert.That(controller.VisibleRoundPhase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            Assert.That(controller.CompletedTurnSlots, Has.Count.EqualTo(4));
            Assert.That(controller.CardStatus, Does.Contain("END OF ROUND"));
            Assert.That(controller.VisibleDirectorHands.SelectMany(hand => hand.Instances),
                Has.No.Member(blue[4]));
            var eventCount = controller.RoundResolutionEvents.Count;
            var redOccupancy = redHero.Occupancy;
            Assert.That(controller.TryPassTurnCard(TeamSide.Red), Is.False);
            Assert.That(controller.LastRoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.RoundAlreadyEnded));
            Assert.That(controller.RoundResolutionEvents, Has.Count.EqualTo(eventCount));
            Assert.That(redHero.Occupancy, Is.EqualTo(redOccupancy));

            while (controller.IsVisibleRoundEnded)
            {
                Assert.That(controller.TryAdvanceEndOfRound(), Is.True, controller.CardStatus);
                yield return null;
            }

            Assert.That(controller.VisibleRoundIndex, Is.EqualTo(2));
            Assert.That(controller.VisibleTurnIndex, Is.EqualTo(1));
            Assert.That(controller.CompletedRounds, Has.Count.EqualTo(1));
            Assert.That(controller.CompletedTurnSlots, Is.Empty);
            Assert.That(controller.VisibleDirectorHands.SelectMany(hand => hand.Instances),
                Has.Member(blue[4]));

            CommitSceneTurn(controller, red[0], blue[0]);
            Assert.That(controller.TryPerformHold(red[0].Id), Is.True);
            Assert.That(controller.TryPerformHold(blue[0].Id), Is.True);

            CommitSceneTurn(controller, red[3], blue[3]);
            Assert.That(controller.TrySelectAction(new ActionDefinitionId("secondary-movement")), Is.True);
            Assert.That(controller.TryStopCardMovement(), Is.True);
            Assert.That(controller.TryPerformHold(blue[3].Id), Is.True);

            CommitSceneTurn(controller, red[2], blue[2]);
            Assert.That(controller.TrySelectAction(new ActionDefinitionId("primary-ranged-attack")), Is.True);
            Assert.That(controller.TrySubmitAttackTarget(blueHero.Occupancy.HeroId), Is.True);
            Assert.That(controller.TrySubmitDefenseCard(blue[4].Id), Is.True);
            Assert.That(controller.TryPerformHold(blue[2].Id), Is.True);

            CommitSceneTurn(controller, red[1], blue[1]);
            Assert.That(controller.TryPerformHold(controller.VisibleTurnActor.CardInstanceId), Is.True);
            Assert.That(controller.TryPerformHold(controller.VisibleTurnActor.CardInstanceId), Is.True);
            yield return null;

            Assert.That(controller.IsVisibleRoundEnded, Is.True);
            Assert.That(controller.VisibleRoundIndex, Is.EqualTo(2));
            while (controller.IsVisibleRoundEnded)
            {
                Assert.That(controller.TryAdvanceEndOfRound(), Is.True, controller.CardStatus);
                yield return null;
            }

            Assert.That(controller.VisibleRoundIndex, Is.EqualTo(3));
            Assert.That(controller.CompletedRounds.Select(record => record.RoundIndex),
                Is.EqualTo(new[] { 1, 2 }));
        }

        [UnityTest]
        public IEnumerator Scene_SelectsVisibleCardOfferThenClicksReachableDestinationInOneClick()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);
            var hero = FindRedHero(heroes);
            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var obstacles = Object.FindObjectsByType<PlaceholderObstacleView>(FindObjectsSortMode.None);
            var origin = hero.Coordinate;
            var originOccupancy = hero.Occupancy;
            var destination = new HexCoordinate(-2, 0);
            var destinationTile = tiles.Single(tile => tile.Coordinate.Equals(destination));

            var selectedCard = controller.VisibleCardInstances[3];
            var selected = controller.TrySelectCard(selectedCard.Id);
            var selectedAction = controller.TrySelectAction(
                new ActionDefinitionId("secondary-movement"));
            var highlightedCoordinates = tiles
                .Where(tile => tile.IsHighlighted)
                .Select(tile => tile.Coordinate)
                .ToArray();
            var expectedDestinations = MovementReachabilityQuery.FindReachableDestinations(
                    originOccupancy,
                    tiles.Select(tile => tile.Coordinate),
                    heroes.Select(candidate => candidate.Occupancy),
                    obstacles.Select(obstacle => obstacle.Occupancy),
                    controller.LastCardMovementAllowance)
                .Select(reachable => reachable.Destination)
                .ToArray();
            var moved = pointerInput.TryHandlePointerRay(CreateDownRay(destinationTile.transform.position));
            var activeAfterMove = controller.IsMovementSessionActive;
            var remainingAfterMove = controller.RemainingMovementAllowance;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(selected, Is.True);
            Assert.That(selectedAction, Is.True);
            Assert.That(highlightedCoordinates, Is.EquivalentTo(expectedDestinations));
            Assert.That(highlightedCoordinates, Has.No.Member(origin));
            Assert.That(highlightedCoordinates, Has.No.Member(new HexCoordinate(1, 0)));
            Assert.That(highlightedCoordinates, Has.No.Member(new HexCoordinate(0, 1)));
            Assert.That(highlightedCoordinates, Has.No.Member(heroes.Single(candidate => candidate != hero).Coordinate));
            Assert.That(moved, Is.True);
            Assert.That(activeAfterMove, Is.False);
            Assert.That(remainingAfterMove, Is.EqualTo(0));
            Assert.That(controller.IsMovementSessionActive, Is.False);
            Assert.That(controller.MovementRoute, Is.EqualTo(new[]
            {
                origin,
                new HexCoordinate(-1, 0),
                destination
            }));
            Assert.That(hero.Coordinate, Is.EqualTo(destination));
            Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
        }

        [UnityTest]
        public IEnumerator Scene_SelectsBlueHeroAndMovesItWithItsOwnCard()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);
            var redHero = FindRedHero(heroes);
            var blueHero = heroes.Single(hero => hero != redHero);
            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var redCardIds = controller.VisibleCardInstances.Select(card => card.Id).ToArray();
            var blueOrigin = blueHero.Coordinate;

            Assert.That(controller.ActiveHeroView, Is.SameAs(redHero));
            Assert.That(redCardIds.All(id => id.Value.StartsWith("red-")), Is.True);
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(blueHero.transform.position)), Is.True);

            var blueCards = controller.VisibleCardInstances.ToArray();
            Assert.That(controller.ActiveHeroView, Is.SameAs(blueHero));
            Assert.That(blueCards.All(card => card.OwnerId.Equals(blueHero.Occupancy.HeroId)), Is.True);
            Assert.That(blueCards.All(card => card.Id.Value.StartsWith("blue-")), Is.True);
            Assert.That(blueCards.Select(card => card.Id).Intersect(redCardIds), Is.Empty);
            Assert.That(controller.TrySelectCard(blueCards[1].Id), Is.True);
            Assert.That(controller.TrySelectAction(
                new ActionDefinitionId("secondary-movement")), Is.True);
            Assert.That(controller.PendingCardChoice.Origin, Is.EqualTo(blueOrigin));
            Assert.That(controller.HighlightedCoordinates, Has.No.Member(redHero.Coordinate));

            var destination = new HexCoordinate(1, -1);
            var destinationTile = tiles.Single(tile => tile.Coordinate.Equals(destination));
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(destinationTile.transform.position)), Is.True);

            Physics.SyncTransforms();
            yield return null;

            Assert.That(blueHero.Coordinate, Is.EqualTo(destination));
            Assert.That(redHero.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(controller.IsCardResolutionPending, Is.False);
            Assert.That(controller.MovementRoute, Is.EqualTo(new[] { blueOrigin, destination }));
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(redHero.transform.position)), Is.True);
            Assert.That(controller.ActiveHeroView, Is.SameAs(redHero));
            Assert.That(controller.VisibleCardInstances.Select(card => card.Id), Is.EquivalentTo(redCardIds));
        }

        [UnityTest]
        public IEnumerator Scene_SelectsVisibleAttackAndClicksAdjacentEnemyHero()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var heroes = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);
            var redHero = FindRedHero(heroes);
            var blueHero = heroes.Single(hero => hero != redHero);
            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var bulwark = controller.VisibleCardInstances.Single(card =>
                card.Id.Value == "red-bulwark-card");

            Assert.That(controller.TrySelectCard(bulwark.Id), Is.True);
            Assert.That(
                controller.SelectedCardOffers.Select(offer => offer.ActionDefinitionId.Value),
                Has.Member("primary-attack"));
            Assert.That(controller.TrySelectAction(new ActionDefinitionId("primary-attack")), Is.True);
            Assert.That(controller.IsAttackTargetPending, Is.True);
            Assert.That(controller.HighlightedCoordinates, Is.EqualTo(new[] { blueHero.Coordinate }));
            Assert.That(tiles.Single(tile => tile.Coordinate.Equals(blueHero.Coordinate)).IsHighlighted, Is.True);

            var targeted = pointerInput.TryHandlePointerRay(CreateDownRay(blueHero.transform.position));
            Physics.SyncTransforms();
            yield return null;

            Assert.That(targeted, Is.True);
            Assert.That(controller.ActiveHeroView, Is.SameAs(redHero));
            Assert.That(controller.IsDefenseResponsePending, Is.True);
            Assert.That(controller.PendingDefenseChoice.ChooserId, Is.EqualTo(blueHero.Occupancy.HeroId));
            Assert.That(controller.PendingDefenseChoice.AttackTotal, Is.EqualTo(3));
            Assert.That(controller.CardStatus, Does.Contain("Defense response pending"));
            Assert.That(controller.HighlightedCoordinates, Is.Empty);
            Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
            Assert.That(redHero.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(blueHero.Coordinate, Is.EqualTo(new HexCoordinate(0, -1)));

            var defenseCardId = controller.PendingDefenseChoice.LegalDefenseOffers[0].SourceCardInstanceId;
            Assert.That(controller.TrySubmitDefenseCard(defenseCardId), Is.True);
            Assert.That(controller.IsDefenseResponsePending, Is.False);
            Assert.That(controller.IsAttackOutcomePending, Is.False);
            Assert.That(controller.PendingAttackResolution, Is.Null);
            Assert.That(controller.LastAttackOutcome, Is.Not.EqualTo(AttackResolutionOutcome.None));
            Assert.That(controller.CardStatus, Does.Match("successfully defended|attack successful"));
        }

        [UnityTest]
        public IEnumerator Scene_ClicksPartialMovementWaypointThenStopsWithAllowanceRemaining()
        {
            var loadOperation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            var hero = FindRedHero(Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None));
            var controller = Object.FindFirstObjectByType<HeroSelectionController>();
            var pointerInput = Object.FindFirstObjectByType<PrimitiveHeroPointerInput>();
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var origin = hero.Coordinate;
            var firstDestination = new HexCoordinate(-1, 0);

            Assert.That(controller.TrySelectCard(controller.VisibleCardInstances[3].Id), Is.True);
            Assert.That(controller.TrySelectAction(new ActionDefinitionId("secondary-movement")), Is.True);
            Assert.That(pointerInput.TryHandlePointerRay(CreateDownRay(
                tiles.Single(tile => tile.Coordinate.Equals(firstDestination)).transform.position)), Is.True);
            Assert.That(controller.IsMovementSessionActive, Is.True);
            Assert.That(controller.RemainingMovementAllowance, Is.EqualTo(1));
            Assert.That(controller.PendingCardChoice.Origin, Is.EqualTo(firstDestination));
            Assert.That(tiles.Where(tile => tile.IsHighlighted).Select(tile => tile.Coordinate),
                Is.EquivalentTo(controller.PendingCardDestinations.Select(destination => destination.Destination)));

            Assert.That(controller.MovementRoute, Is.EqualTo(new[]
            {
                origin,
                firstDestination
            }));
            Assert.That(controller.TryStopCardMovement(), Is.True);

            Physics.SyncTransforms();
            yield return null;

            Assert.That(hero.Coordinate, Is.EqualTo(firstDestination));
            Assert.That(controller.IsMovementSessionActive, Is.False);
            Assert.That(controller.RemainingMovementAllowance, Is.EqualTo(1));
            Assert.That(tiles, Has.All.Matches<HexTileView>(tile => !tile.IsHighlighted));
        }

        private static Ray CreateDownRay(Vector3 target)
        {
            return new Ray(target + (Vector3.up * 2f), Vector3.down);
        }

        private static void CommitSceneTurn(
            HeroSelectionController controller,
            CardInstance redCard,
            CardInstance blueCard)
        {
            Assert.That(controller.TrySelectDirectorCard(redCard.Id), Is.True);
            Assert.That(controller.TryConfirmSelectedTurnCard(), Is.True);
            Assert.That(controller.TrySelectDirectorCard(blueCard.Id), Is.True);
            Assert.That(controller.TryConfirmSelectedTurnCard(), Is.True);
        }

        private static PrimitiveHeroView FindRedHero(PrimitiveHeroView[] heroes)
        {
            return heroes.Single(hero =>
                hero.Occupancy.HeroId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue);
        }
    }
}
