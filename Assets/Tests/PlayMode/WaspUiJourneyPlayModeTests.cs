using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
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

            UiPlayer.EnsureVisible(TrayCard(hud, InitiativeEightCardOf(runtime, firstHeroId)));
            yield return null;
            UiPlayer.Click(TrayCard(hud, InitiativeEightCardOf(runtime, firstHeroId)));
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

            UiPlayer.EnsureVisible(TrayCard(hud, InitiativeEightCardOf(runtime, secondHeroId)));
            yield return null;
            UiPlayer.Click(TrayCard(hud, InitiativeEightCardOf(runtime, secondHeroId)));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "CONFIRM FACEDOWN"));
            yield return null;

            // Both seats committed a card printing Initiative 8, so p.7's joint reveal arrives tied
            // across the teams. The Tie Breaker side wins the tie, and with exactly one candidate of
            // its own there is no choice to pause on: the actor is selected at once and the flip is
            // deferred to the turn's resolution. A pending choice here would be a question with one
            // answer.
            //
            // Milestone 8.20 made the two cards different faces rather than the same one. The staged
            // match is Wasp against Arien now, so a tie can no longer be produced by dealing one
            // printed set twice - it is produced by two heroes who each print an 8, which is the
            // ordinary way ties arise at a real table and a stronger subject than the mirror was.
            Assert.That(runtime.PendingInitiativeChoice, Is.Null,
                "A cross-team tie with one candidate per side pauses nothing in the 1v1 staging.");
            Assert.That(runtime.IsTurnActive, Is.True);
            var tieSide = runtime.TieBreakerState.RevealedSide;
            var tieSideHeroId = runtime.Setup.Heroes
                .Single(hero => hero.TeamSide == tieSide).HeroId;
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tieSideHeroId),
                "The Tie Breaker side's own card acts first.");
            Assert.That(runtime.Setup.Heroes, Has.All.Matches<QuickGameHeroSetupState>(value =>
                runtime.GetCommitmentStatus(value.HeroId) == TurnCardCommitmentStatus.Revealed));

            // Nothing stands beside either spawn, so the revealed card offers no Attack: the primary
            // is drawn as a real greyed control a player can read and cannot press.
            var unavailable = runtime.RoundView.Model.UnavailableActions;
            Assert.That(unavailable, Is.Not.Empty,
                "The revealed printed Attack has nothing adjacent to target from the spawn.");
            foreach (var action in unavailable)
            {
                var control = UiPlayer.Button(hud, action.Label);
                Assert.That(control.enabledInHierarchy, Is.False,
                    $"The greyed '{action.Label}' control must not be pressable.");
            }
        }

        /// <summary>
        /// Milestone 8.20b made the piece this clicks Wasp's own rather than whichever seat the coin
        /// flip left the tray away from. The staged match deals two different heroes since milestone
        /// 8.20, so "the other hero" was Arien on half the runs - which is fine for the fall-through
        /// contract this case owns and useless as Wasp's evidence for it. The tray is pointed at the
        /// other seat first, so the click under test is what brings it back rather than something that
        /// was already true.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_PointsTheTrayAtTheOtherHeroThroughAMapClickTheHudDoesNotBlock()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            LogUiEnvironment(root);

            var waspHeroId = WaspSeat(runtime).HeroId;
            var otherHeroId = runtime.Setup.Heroes
                .Single(hero => !hero.HeroId.Equals(waspHeroId)).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var waspHero = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(waspHeroId));

            if (runtime.ViewedHeroId.Equals(waspHeroId))
            {
                var otherHero = Object.FindObjectsByType<PrimitiveHeroView>()
                    .Single(view => view.Occupancy.HeroId.Equals(otherHeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, otherHero.transform.position),
                    Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(waspHeroId),
                "The tray must be pointed away before the click this case owns.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, waspHero.transform.position),
                Is.True,
                "The map click on Wasp's own piece must be claimed by the selector chain.");
            yield return null;

            Assert.That(runtime.ViewedHeroId, Is.EqualTo(waspHeroId),
                "A click the authority has no use for falls through to pointing the tray.");
        }

        /// <summary>
        /// Wasp's own board piece at p.6's spawn, and the whole visual layer rebuilt around it.
        /// Milestone 8.20b.
        ///
        /// <para>The permanent hero player-path gate asks for "initial hero spawn and identity" and for
        /// a presentation rebuild that reconstructs the hero "without stale or duplicate views". Wasp's
        /// journeys have driven her cards since milestone 8.15 and never once asked what the piece
        /// itself is, which is the half this closes.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HeroPiece_StandsAtTheRedSpawnAsWaspAndSurvivesAPresentationRebuild()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "The staged match deals one hero a side, so two pieces stand on the board and a " +
                "third would be a stale or duplicated view. Found: " +
                string.Join(", ", heroViews.Select(view => view.Occupancy.HeroId.Value)));

            var waspSetup = WaspSeat(runtime);
            var waspView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(waspSetup.HeroId));

            // Identity: the piece knows which printed set it is, wears the prepared official token
            // rather than the generated prism fallback, and stands on the side its seat was dealt.
            Assert.That(waspView.HeroContentId.HasValue, Is.True,
                "The staged Red piece carries no content identity, so nothing on the board says " +
                "which hero it is.");
            Assert.That(waspView.HeroContentId.Value.Value,
                Is.EqualTo(WaspCardContent.ContentSetId));
            Assert.That(waspView.TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(waspView.HasTokenVisual, Is.True,
                "Wasp's official circular token is prepared, so the piece must be wearing it " +
                "rather than the generated prism the fallback draws.");

            // Placement: the configured Red hero spawn, and the space the authority agrees she is on.
            Assert.That(waspView.ConfiguredSpawnPointId.HasValue, Is.True);
            Assert.That(waspView.ConfiguredSpawnPointId.Value,
                Is.EqualTo(waspSetup.SpawnPoint.SpawnPointId),
                "The piece must stand at the spawn point the configuration assigned it.");
            Assert.That(waspView.Coordinate, Is.EqualTo(waspSetup.Coordinate));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(waspSetup.HeroId)).Coordinate,
                Is.EqualTo(waspView.Coordinate),
                "The drawn piece and the authoritative state must name one space.");

            // The hand the piece is dealt is Wasp's printed set and nobody else's, which is what makes
            // this piece this hero rather than a Red-coloured one.
            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(waspSetup.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(WaspCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()));

            // Wasp's whole player surface as she is dealt it: her own token on its spawn, her five
            // starting faces in the tray, and the rails either side of the board.
            yield return PointTrayAt(root, waspSetup.HeroId);
            yield return UiPlayer.CaptureGameView("8-20b", "wasps-piece-and-her-own-tray");

            // Presentation rebuild: the whole visual layer is rebuilt from the current projection and
            // must come back as one piece per hero, in the same places, with no orphan left behind.
            runtime.RefreshAll();
            yield return null;
            var rebuilt = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(rebuilt, Has.Length.EqualTo(2),
                "A rebuild that leaves a stale or duplicated hero view is the defect this checks.");
            var rebuiltWasp = rebuilt.Single(view =>
                view.Occupancy.HeroId.Equals(waspSetup.HeroId));
            Assert.That(rebuiltWasp.Coordinate, Is.EqualTo(waspSetup.Coordinate));
            Assert.That(rebuiltWasp.HasTokenVisual, Is.True);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(waspSetup.HeroId),
                "The rebuilt tray still follows the hero the player pointed it at.");
            Assert.That(
                runtime.HandsView.Model.Hands
                    .Single(hand => hand.HeroId.Equals(waspSetup.HeroId)).TrayCards,
                Has.Count.EqualTo(WaspCardContent.StartingHand.Count),
                "and her whole starting hand is drawn again from the projection alone.");
        }

        [UnityTest]
        public IEnumerator Scene_MovesByTheSecondaryMovementThroughRealControlsAndABoardClick()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            // Wasp plays the highest-Initiative card in her hand that prints a secondary Movement,
            // against the other hand's lowest, so she takes the first turn whichever seat the coin
            // flip opened the tray on. Milestone 8.20 made the card a question worth asking of the
            // hand rather than answering with a name - the two seats deal different printed sets now -
            // and milestone 8.20b fixed the acting hero to Wasp, because a journey that walked
            // whichever seat the tray opened on was Wasp's evidence only half the time.
            var actingHeroId = WaspSeat(runtime).HeroId;
            var otherHeroId = runtime.Setup.Heroes
                .Single(hero => !hero.HeroId.Equals(actingHeroId)).HeroId;
            yield return PointTrayAt(root, actingHeroId);
            yield return CommitThroughTray(root, MovingCardOf(runtime, actingHeroId));
            yield return PointTrayAt(root, otherHeroId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, otherHeroId));
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

            // The piece follows the state: this is the hero-piece movement half of the row.
            var waspView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(actingHeroId));
            Assert.That(waspView.Coordinate, Is.EqualTo(destination),
                "The token must be standing where Wasp now is, not where she was.");

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
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, committedHeroId));
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
        /// Wasp's half of milestone 8.20c's standard-footprint conformance: every one of her verified
        /// faces, drawn on the live panel at the validated player viewport, is exactly
        /// <see cref="CardFootprint"/> and shows its content whole.
        ///
        /// <para>The measurement is <see cref="StandardCardFootprintCheck"/>'s and is hero-neutral;
        /// this case supplies <see cref="WaspCardContent"/> and the panel. It covers every face rather
        /// than the five a hand happens to hold, because the footprint has to hold for the card a
        /// player meets in a Defense response, an effect choice or an upgrade prompt as much as for the
        /// one they were dealt - and because a face that will not fit must block this hero's gate
        /// whether or not this staging deals it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEveryWaspFaceAtTheStandardCardFootprint()
        {
            yield return LoadScene();
            var root = Root();
            LogUiEnvironment(root);

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Wasp", WaspCardContent.AllDefinitions, root.Hud);
        }

        /// <summary>
        /// Milestone 8.20c's focused regression: pointing the tray from one hero to the other through
        /// the configured map leaves every card exactly the same size, and moves nothing around them.
        ///
        /// <para>This is the defect stated as a journey. A tray card used to take a floor rather than a
        /// height and the row stretched all five to the tallest one's <em>content</em>, so a hand's
        /// card size was a function of the hero holding it: switching heroes resized every card on the
        /// screen, and the tray region reflowed under them. Two heroes are configured, which is what
        /// makes this observable at all - and 8.21 adds a third printed set to the same surface.</para>
        ///
        /// <para>It also holds the two states a player can reach from here - the shared selection
        /// outline, and pointer/keyboard focus - to changing nothing about the box. The remaining
        /// treatments (in play, unavailable, Active-effect) are exhaustively covered at the Edit Mode
        /// tier by <c>MatchHudPresentationTests</c>, because reaching all five through real play would
        /// prove the same property at several times the cost.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_KeepsEveryTrayCardTheSameSizeWhenTheMapPointerSwitchesHeroes()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            LogUiEnvironment(root);

            var waspHeroId = WaspSeat(runtime).HeroId;
            var otherHeroId = runtime.Setup.Heroes
                .Single(hero => !hero.HeroId.Equals(waspHeroId)).HeroId;

            yield return PointTrayAt(root, waspHeroId);
            yield return null;
            AssertEveryTrayCardIsTheStandardFootprint(hud, "Wasp's own hand");

            // The laid-out row, not the region's arithmetic rect: the rect is a function of the screen
            // and could not move, while the row is what a content-sized card actually grew.
            var rowBefore = NamedElement(hud, "gd-card-buttons").worldBound;
            var regionBefore = NamedElement(hud, "gd-tray-region").worldBound;

            yield return UiPlayer.CaptureGameView("8-20c", "wasp-hand-at-the-standard-footprint");

            yield return PointTrayAt(root, otherHeroId);
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(otherHeroId),
                "The map click must have pointed the tray at the other hero.");
            AssertEveryTrayCardIsTheStandardFootprint(hud, "the other hero's hand");
            yield return UiPlayer.CaptureGameView("8-20c", "arien-hand-at-the-standard-footprint");

            // Back again, because the property under test is symmetric and a one-way check would pass
            // on a surface that had simply settled at the second hero's size.
            yield return PointTrayAt(root, waspHeroId);
            yield return null;
            AssertEveryTrayCardIsTheStandardFootprint(hud, "Wasp's hand after switching back");

            Assert.That(NamedElement(hud, "gd-card-buttons").worldBound, Is.EqualTo(rowBefore),
                "Switching heroes must not move or resize the row the cards stand on.");
            Assert.That(NamedElement(hud, "gd-tray-region").worldBound, Is.EqualTo(regionBefore),
                "and must not reflow the tray region around it.");

            // The bounded scroll a fixed footprint relies on at a short supported viewport: the cards
            // stand inside a scrolling region, so a card taller than the region is reached by
            // scrolling rather than by being made smaller.
            var scroll = UiPlayer.Descendants(NamedElement(hud, "gd-tray-region"))
                .OfType<UnityEngine.UIElements.ScrollView>()
                .ToArray();
            Assert.That(scroll, Has.Length.EqualTo(1),
                "The tray region holds exactly one scroll view, which is what keeps every card and " +
                "control reachable without resizing any of them.");
            Assert.That(
                UiPlayer.Descendants(scroll[0]).Any(element => element.name == "gd-card-buttons"),
                Is.True,
                "and the cards are inside it.");

            // Selection and focus, on a real card, through the real control. Both are drawn as an
            // always-present outline that only changes colour, and this is what says so on the live
            // panel rather than in the builder's comment.
            var card = TrayCard(hud, InitiativeEightCardOf(runtime, waspHeroId));
            UiPlayer.EnsureVisible(card);
            yield return null;
            var beforeSelection = card.worldBound;
            UiPlayer.Click(TrayCard(hud, InitiativeEightCardOf(runtime, waspHeroId)));
            yield return null;

            Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId.HasValue, Is.True,
                "The click must have picked the card up, or this proves nothing about selection.");
            AssertEveryTrayCardIsTheStandardFootprint(hud, "the hand with a card picked up");
            var selected = TrayCard(hud, InitiativeEightCardOf(runtime, waspHeroId));
            Assert.That(selected.worldBound.size, Is.EqualTo(beforeSelection.size),
                "Selecting a card must not change its size.");

            selected.Focus();
            yield return null;
            Assert.That(
                TrayCard(hud, InitiativeEightCardOf(runtime, waspHeroId)).worldBound.size,
                Is.EqualTo(beforeSelection.size),
                "and neither must focusing it.");
        }

        /// <summary>
        /// Milestone 8.20c's bounded short-viewport fallback, on the live surface: at a short supported
        /// desktop screen the region holding the cards scrolls and the cards keep their size.
        ///
        /// <para>It is the other half of the standard footprint's bargain. One fixed height for every
        /// card in the game is only supportable if some screens can show less of the tray rather than a
        /// smaller card, and this is the screen where that happens: at 1280x720 the tray region is
        /// shorter than one card, so the region's own scroll view is what keeps every card reachable -
        /// which the journey proves by scrolling to the last card and clicking it.</para>
        ///
        /// <para><b>Why the assertions come after the viewport is restored.</b> This is the one journey
        /// in the suite that shrinks the play view, and every other journey reads its geometry from
        /// whatever size is left behind - <c>UiPlayer.EnsurePlayerViewport</c> accepts 1280x720 as
        /// already a real player's. So the facts are collected, the size this session was running at is
        /// put back, and only then are they judged, which keeps a failure here from silently changing
        /// the geometry every later journey runs at.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_KeepsTheCardFootprintAtAShortViewportAndScrollsTheRegion()
        {
            yield return LoadScene();
            var root = Root();
            var hud = root.Hud;

            // The size to put back afterwards is the one this session was actually running at, not the
            // validated default: a non-batch pass runs at whatever window the developer opened, and
            // milestone 8.20b's captures were taken at 2560x1440.
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;

            UiPlayer.SetPlayerViewport(ShortViewportWidth, ShortViewportHeight, "8.20c short viewport");
            yield return null;

            // `ConfiguredMatchHudRenderer.Layout` applies the current viewport to every absolutely
            // placed region and runs on a projection refresh rather than every frame, so a resized
            // screen is followed at the next refresh - which is what this asks for, through the same
            // `RefreshAll` the presentation-rebuild journey uses. Two frames after it, because an
            // element rebuilt this frame has no resolved layout until the panel's next update.
            root.Runtime.RefreshAll();
            yield return null;
            yield return null;

            var shortWidth = Screen.width;
            var shortHeight = Screen.height;
            var region = NamedElement(hud, "gd-tray-region").worldBound;
            var scroll = UiPlayer.Descendants(NamedElement(hud, "gd-tray-region"))
                .OfType<UnityEngine.UIElements.ScrollView>()
                .Single();
            var content = scroll.contentContainer.worldBound.height;
            var viewport = scroll.contentViewport.worldBound.height;
            var sizes = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-card-choice")
                .Select(element => element.worldBound.size)
                .ToArray();

            // Reachability, before anything is judged: the last card in the row is scrolled to and
            // clicked through the real panel, which is what "bounded scrolling keeps every card
            // reachable" means when the region cannot show them all at once.
            var last = UiPlayer.Descendants(hud.RootElement)
                .Last(element => element.name == "gd-card-choice");
            UiPlayer.EnsureVisible(last);
            yield return null;
            var reached = UiPlayer.Descendants(hud.RootElement)
                .Last(element => element.name == "gd-card-choice");
            var reachedIsClickable = UiPlayer.IsShown(reached) && reached.enabledInHierarchy;

            yield return UiPlayer.CaptureGameView("8-20c", "short-viewport-scrolls-rather-than-resizing");

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.20c restore the viewport this session was running at");
            yield return null;
            root.Runtime.RefreshAll();
            yield return null;
            yield return null;

            Assert.That(shortWidth, Is.EqualTo((int)ShortViewportWidth),
                "The journey must actually have run at the short viewport.");
            Assert.That(shortHeight, Is.EqualTo((int)ShortViewportHeight));
            Assert.That(sizes, Is.Not.Empty, "The short viewport still draws the hand.");
            foreach (var size in sizes)
            {
                Assert.That(size.x, Is.EqualTo(CardFootprint.Width).Within(0.01f),
                    "A card is the same width on a short screen as on a tall one.");
                Assert.That(size.y, Is.EqualTo(CardFootprint.Height).Within(0.01f),
                    "and the same height - what gives on a short screen is the region, not the card.");
            }

            Assert.That(region.height, Is.LessThan(CardFootprint.Height + 40f),
                "The short viewport is the case this rule is about: the region cannot comfortably " +
                "hold a whole card.");
            Assert.That(content, Is.GreaterThan(viewport),
                "so its scroll view has more content than it can show, which is the bounded scroll " +
                "that replaces resizing the cards.");
            Assert.That(reachedIsClickable, Is.True,
                "and the last card in the row is still shown and pressable after scrolling to it.");
            Assert.That((uint)Screen.width, Is.EqualTo(restoreWidth),
                "The viewport this session was running at is restored before this journey ends, " +
                "because every other journey reads its geometry from whatever size is left behind.");
            Assert.That((uint)Screen.height, Is.EqualTo(restoreHeight));
        }

        /// <summary>
        /// The short supported desktop viewport milestone 8.20c states the bounded-scroll fallback at.
        /// 1280x720 is the smallest screen this project's own HUD arithmetic is written against.
        /// </summary>
        private const uint ShortViewportWidth = 1280;

        private const uint ShortViewportHeight = 720;

        /// <summary>The one live element carrying <paramref name="name"/>.</summary>
        private static UnityEngine.UIElements.VisualElement NamedElement(
            ConfiguredMatchHudRenderer hud, string name) =>
            UiPlayer.Descendants(hud.RootElement).Single(element => element.name == name);

        /// <summary>
        /// Every card the tray is currently drawing, held to the standard footprint as the panel
        /// resolved it. Reads the live elements rather than the view model, because the question is
        /// what a player's screen actually shows.
        /// </summary>
        private static void AssertEveryTrayCardIsTheStandardFootprint(
            ConfiguredMatchHudRenderer hud, string what)
        {
            var cards = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-card-choice")
                .ToArray();
            Assert.That(cards, Is.Not.Empty, $"The tray draws no cards for {what}.");

            foreach (var card in cards)
            {
                var box = card.worldBound;
                Assert.That(box.width, Is.EqualTo(CardFootprint.Width).Within(0.01f),
                    $"A card in {what} is {box.width} wide rather than the standard footprint.");
                Assert.That(box.height, Is.EqualTo(CardFootprint.Height).Within(0.01f),
                    $"A card in {what} is {box.height} tall rather than the standard footprint.");
            }
        }

        /// <summary>
        /// The staged seat dealt Wasp's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour - the seat is Red today and the question this asks is
        /// which seat is Wasp's. Milestone 8.20b, and the twin of
        /// <see cref="ArienUiJourneyPlayModeTests.ArienSeat"/>.
        /// </summary>
        internal static QuickGameHeroSetupState WaspSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == WaspCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Wasp to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0];
        }

        /// <summary>
        /// Points the tray at one hero through a board click on that hero's own piece, which is the
        /// route a player has. A tray already pointing there is left alone.
        /// </summary>
        private static IEnumerator PointTrayAt(
            MatchSceneCompositionRoot root, GuardsDigital.Core.Units.Heroes.HeroId heroId)
        {
            if (root.Runtime.ViewedHeroId.Equals(heroId))
            {
                yield break;
            }

            var view = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(candidate => candidate.Occupancy.HeroId.Equals(heroId));
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointerInput, root.Hud, view.transform.position), Is.True,
                "The board click that points the tray at " + heroId.Value + " must be claimed.");
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(heroId));
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
        /// The Initiative 8 card in this hero's starting hand, by the name a player reads on its face.
        ///
        /// <para>Milestone 8.20. The staged match deals two different heroes, so a journey about a
        /// cross-team Initiative tie can no longer name one card for both seats. It is read off the
        /// hero's own dealt hand rather than hard-coded per hero, so the journey states what it needs -
        /// two cards printing the same Initiative - rather than which two cards happen to do it.</para>
        /// </summary>
        /// <summary>
        /// The cards this hero is holding, as their printed definitions. Milestone 8.20: every card
        /// these journeys reach for is now chosen off the hand rather than named, because the two seats
        /// deal different printed sets and the coin flip decides which one the tray opens on.
        /// </summary>
        private static CardDefinition[] HandOf(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)))
                .ToArray();

        /// <summary>
        /// The name a player reads on this hero's highest-Initiative card that also prints a secondary
        /// Movement - which is what a journey about moving by that secondary needs, and what naming a
        /// card could not stay true to across two printed sets.
        /// </summary>
        private static string MovingCardOf(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId)
        {
            var candidates = HandOf(runtime, heroId)
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.Kind == GuardsDigital.Core.Cards.ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary))
                .OrderByDescending(definition => definition.Initiative.Value)
                .ToArray();

            Assert.That(candidates, Is.Not.Empty,
                heroId.Value + " holds no card printing a secondary Movement, so this journey has " +
                "nothing to move by.");
            return candidates[0].DisplayName.ToUpperInvariant();
        }

        /// <summary>The name on this hero's lowest-Initiative card.</summary>
        private static string LowestInitiativeCardOf(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId) =>
            HandOf(runtime, heroId)
                .OrderBy(definition => definition.Initiative.Value)
                .First()
                .DisplayName
                .ToUpperInvariant();

        private static string InitiativeEightCardOf(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId)
        {
            var candidates = runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)))
                .Where(definition => definition.Initiative.IsDefined &&
                    definition.Initiative.Value == 8)
                .ToArray();

            Assert.That(candidates, Has.Length.EqualTo(1),
                "This journey needs exactly one Initiative 8 card in " + heroId.Value +
                "'s starting hand to produce a cross-team tie; it has " + candidates.Length + ".");
            return candidates[0].DisplayName.ToUpperInvariant();
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
