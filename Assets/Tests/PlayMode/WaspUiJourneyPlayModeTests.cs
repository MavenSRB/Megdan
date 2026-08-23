using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.15: the configured Wasp mirror played the way a player plays it.
    ///
    /// <para>Every decision these journeys make is made on the real surface: tray cards and controls
    /// are clicked as live UI Toolkit elements through <see cref="UiPlayer"/>, and board decisions go
    /// through the camera's screen-point ray after the HUD has been asked whether it blocks that
    /// point. The headless command surface the other Play Mode fixtures drive is deliberately not
    /// used for the decision under test - it bypasses the control's own binding, the element's
    /// enabled and displayed state, occlusion, and pointer blocking, which is exactly the band where
    /// defects were previously caught only by hand.</para>
    /// </summary>
    public sealed class WaspUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_CommitsBothHandsThroughTheRealTrayControlsAndResolvesTheTiedReveal()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            LogUiEnvironment(root);

            Assert.That(hud.RootElement, Is.Not.Null, "The HUD built no visual tree.");
            Assert.That(hud.RootElement.panel, Is.Not.Null, "The HUD tree is not on a live panel.");
            Assert.That(Screen.width, Is.GreaterThanOrEqualTo(1280),
                $"The journey needs a real player viewport and got {Screen.width}x{Screen.height}.");

            // The tray follows the hero the match is waiting on; during commitment that is one of the
            // two, and after that hand commits the tray must follow to the other of its own accord.
            var firstHeroId = runtime.ViewedHeroId;
            var secondHeroId = runtime.Setup.Heroes
                .Single(hero => !hero.HeroId.Equals(firstHeroId)).HeroId;

            Assert.That(UiPlayer.HasButton(hud, "CONFIRM FACEDOWN"), Is.False,
                "Before any card is picked up there is nothing to confirm.");

            UiPlayer.EnsureVisible(TrayCard(hud, "SHOCK"));
            yield return null;
            UiPlayer.Click(TrayCard(hud, "SHOCK"));
            yield return null;
            Assert.That(UiPlayer.HasButton(hud, "CONFIRM FACEDOWN"), Is.True,
                "Picking a card up is what makes the confirm control exist. Shown controls: " +
                UiPlayer.Describe(hud));
            var acceptedBeforeConfirm = runtime.AcceptedCommands.Count;
            UiPlayer.Click(UiPlayer.Button(hud, "CONFIRM FACEDOWN"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBeforeConfirm + 1),
                "One click is one command - a control that fired twice would have tried to commit " +
                "a second card.");
            Assert.That(runtime.GetCommitmentStatus(firstHeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown),
                "The clicked confirm must commit the clicked card facedown.");
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(secondHeroId),
                "The tray follows the hero the match is now waiting on.");

            UiPlayer.EnsureVisible(TrayCard(hud, "SHOCK"));
            yield return null;
            UiPlayer.Click(TrayCard(hud, "SHOCK"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "CONFIRM FACEDOWN"));
            yield return null;

            // Both seats committed the same printed card, so p.7's joint reveal arrives tied across
            // the teams. The Tie Breaker side wins the tie, and with exactly one candidate of its own
            // there is no choice to pause on: the actor is selected at once and the flip is deferred
            // to the turn's resolution. A pending choice here would be a question with one answer.
            Assert.That(runtime.PendingInitiativeChoice, Is.Null,
                "A cross-team tie with one candidate per side pauses nothing in the 1v1 mirror.");
            Assert.That(runtime.IsTurnActive, Is.True);
            var tieSide = runtime.TieBreakerState.RevealedSide;
            var tieSideHeroId = runtime.Setup.Heroes
                .Single(hero => hero.TeamSide == tieSide).HeroId;
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tieSideHeroId),
                "The Tie Breaker side's own Shock acts first.");
            Assert.That(runtime.Setup.Heroes, Has.All.Matches<QuickGameHeroSetupState>(value =>
                runtime.GetCommitmentStatus(value.HeroId) == TurnCardCommitmentStatus.Revealed));

            // Nothing stands beside either spawn, so the revealed Shock offers no Attack: the primary
            // is drawn as a real greyed control a player can read and cannot press.
            var unavailable = runtime.RoundView.Model.UnavailableActions;
            Assert.That(unavailable, Is.Not.Empty,
                "Shock's printed Attack has nothing adjacent to target from the spawn.");
            foreach (var action in unavailable)
            {
                var control = UiPlayer.Button(hud, action.Label);
                Assert.That(control.enabledInHierarchy, Is.False,
                    $"The greyed '{action.Label}' control must not be pressable.");
            }
        }

        [UnityTest]
        public IEnumerator Scene_PointsTheTrayAtTheOtherHeroThroughAMapClickTheHudDoesNotBlock()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            LogUiEnvironment(root);

            var viewedHeroId = runtime.ViewedHeroId;
            var otherHero = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => !view.Occupancy.HeroId.Equals(viewedHeroId));
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, otherHero.transform.position),
                Is.True,
                "The map click on the other hero must be claimed by the selector chain.");
            yield return null;

            Assert.That(runtime.ViewedHeroId, Is.EqualTo(otherHero.Occupancy.HeroId),
                "A click the authority has no use for falls through to pointing the tray.");
        }

        [UnityTest]
        public IEnumerator Scene_MovesByTheSecondaryMovementThroughRealControlsAndABoardClick()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            // The first-viewed hand plays Magnetic Dagger at Initiative 12 against the other hand's
            // Stop Projectiles at 3, so the clicked hand acts first whichever seat it is.
            var actingHeroId = runtime.ViewedHeroId;
            yield return CommitThroughTray(root, "MAGNETIC DAGGER");
            yield return CommitThroughTray(root, "STOP PROJECTILES");
            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(actingHeroId));

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Movement);
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;

            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "The clicked movement control must start the movement session.");
            var destination = runtime.PendingMovementChoice.LegalDestinations
                .First(value => value.Cost == 1).Destination;
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.True,
                "The legal destination a player is about to click must be lit on the board.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True,
                "The board click on the lit destination must be claimed.");
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(actingHeroId)).Coordinate,
                Is.EqualTo(destination),
                "The clicked destination is where the hero now stands.");

            if (runtime.PendingMovementChoice != null)
            {
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                Assert.That(stop.Command.Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
                UiPlayer.Click(UiPlayer.Button(hud, stop.Label));
                yield return null;
            }

            Assert.That(runtime.PendingMovementChoice, Is.Null);
            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(actingHeroId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"),
                "The spent card reads as this round's first turn on the tray.");
        }

        [UnityTest]
        public IEnumerator Scene_OpensTheDirectorDrawerUndoesTogglesTheLogAndResets()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var committedHeroId = runtime.ViewedHeroId;
            yield return CommitThroughTray(root, "LIFT UP");
            Assert.That(runtime.GetCommitmentStatus(committedHeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            var drawer = UiPlayer.Descendants(hud.RootElement)
                .Single(element => element.name == "gd-director-drawer");
            Assert.That(UiPlayer.IsShown(drawer), Is.False,
                "The drawer is closed until its toggle is clicked.");
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);
            Assert.That(UiPlayer.IsShown(drawer), Is.True);

            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(committedHeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "The clicked UNDO must retract the committed card.");

            var logToggleLabel = runtime.DiagnosticView.Model.ToggleLabel;
            var logPanel = UiPlayer.Descendants(hud.RootElement)
                .Single(element => element.name == "gd-event-log");
            Assert.That(UiPlayer.IsShown(logPanel), Is.False);
            UiPlayer.Click(UiPlayer.Button(hud, logToggleLabel));
            yield return null;
            Assert.That(UiPlayer.IsShown(logPanel), Is.True,
                "The clicked EVENT LOG toggle must open the log panel.");
            Assert.That(runtime.DiagnosticView.Model.IsExpanded, Is.True);

            yield return CommitThroughTray(root, "LIFT UP");
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(committedHeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "RESET restarts the match, so the commitment is gone.");
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");
        }

        /// <summary>
        /// Selects and confirms one card of the currently viewed hand through the real tray controls.
        /// </summary>
        private static IEnumerator CommitThroughTray(
            MatchSceneCompositionRoot root, string printedName)
        {
            var hud = root.Hud;
            UiPlayer.EnsureVisible(TrayCard(hud, printedName));
            yield return null;
            UiPlayer.Click(TrayCard(hud, printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "CONFIRM FACEDOWN"));
            yield return null;
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

        /// <summary>
        /// The one tray card whose name band reads <paramref name="printedName"/> - the uppercased
        /// display name a player reads on the face.
        /// </summary>
        private static UnityEngine.UIElements.VisualElement TrayCard(
            ConfiguredMatchHudRenderer hud, string printedName) =>
            UiPlayer.NamedElementWithText(hud, "gd-card-choice", printedName);

        /// <summary>
        /// One line of environment facts per run, so a geometry failure in the batch viewport is
        /// diagnosable from the log alone.
        /// </summary>
        private static void LogUiEnvironment(MatchSceneCompositionRoot root)
        {
            var hud = root.Hud;
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();
            var camera = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>().EventCamera;
            var heroReport = string.Join("; ", heroes.Select(hero =>
            {
                var screen = camera.WorldToScreenPoint(hero.transform.position);
                var blocked = hud.BlocksPointerAt(new Vector2(screen.x, screen.y));
                return $"{hero.Occupancy.HeroId.Value}@({screen.x:F0},{screen.y:F0},z{screen.z:F0})" +
                    $" blocked={blocked}";
            }));
            Debug.Log(
                $"[8.15 UI] viewport {Screen.width}x{Screen.height} " +
                $"panel={(hud.RootElement?.panel == null ? "none" : "live")} " +
                $"tray={hud.CardPanelRect} status={hud.StatusBarRect} " +
                $"redRail={hud.RedHeroRailRect} blueRail={hud.BlueHeroRailRect} " +
                $"heroes: {heroReport}");
        }

        private static IEnumerator LoadScene()
        {
            // Before the scene composes, so the HUD's first layout is already at player size.
            UiPlayer.EnsurePlayerViewport();
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            // Two frames: one for the composition's own enable pass, one for the panel's first layout,
            // without which every worldBound this fixture reads is NaN.
            yield return null;
            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }
    }
}
