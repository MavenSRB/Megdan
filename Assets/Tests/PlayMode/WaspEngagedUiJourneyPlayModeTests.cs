using System.Collections;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.15: Wasp's starting-hand cards played through the real player pipeline from the
    /// engaged position - both Wasps beside one Blue melee minion, two spaces from each other.
    ///
    /// <para>Every decision under test is made on the real surface: live UI Toolkit controls through
    /// <see cref="UiPlayer"/>, and board decisions through the camera's screen-point ray after the
    /// HUD's own pointer-blocking answer. The engaged scene content exists so these journeys spend
    /// their commands on the cards they verify rather than on the eight-turn approach march the
    /// 8.11e regression already owns.</para>
    /// </summary>
    public sealed class WaspEngagedUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator Shock_AsksItsTwoStepSevenQuestionsThroughTheBoardAndTheDiscardControls()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            yield return CommitThroughTray(root, "SHOCK");
            yield return CommitThroughTray(root, "STOP PROJECTILES");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Shock's printed Initiative 8 acts before Stop Projectiles' 3.");

            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Is.Empty,
                "The Blue hero stands two spaces away, and Shock targets only adjacency.");
            var minionView = Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null &&
                    view.Occupancy.MinionId.Equals(targetId));
            // Milestone 8.62, at the user's direction. This assertion USED TO SAY `None`: 8.30h routed
            // every board choice into the hex's six corner marks and left the fitted piece ring built
            // and never drawn, so a targeted minion was marked only by the sand around it - "they are
            // almost impossible to see like this". The piece now wears the projection's own cue, and
            // the corner marks below are unchanged.
            Assert.That(minionView.TargetCueState,
                Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Eligible)));
            Assert.That(minionView.TargetRing.ConsequenceIntent,
                Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetConsequenceIntent.Attack)));
            Assert.That(minionView.TargetRing.IsVisible,
                Is.EqualTo(BoardTargetCuePresentation.DrawsTheRingOnThePiece));
            Assert.That(Object.FindObjectsByType<HexTileView>().Single(value =>
                    value.Coordinate.Equals(minionView.Occupancy.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack));

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, minionView.transform.position), Is.True);
            yield return null;

            // MILESTONE 8.86. The Attack's own outcome, which this journey drove and never read: an
            // attacked minion is taken off the board by p.13, so the piece that was clicked has to be
            // gone before the rider below means anything.
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(targetId),
                "\"Target a unit adjacent to you\" - the clicked minion is defeated by the Attack.");

            // Rulebook p.14 step 7: the card text stops the Attack on its first question, and the one
            // enemy hero in the printed Radius 2 disk who is not adjacent is marked on the board.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Shock's rider must stop the Attack on the actor's hero question.");
            var blueHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(blueHeroId));
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Not.Empty,
                "The hero candidate is a named board target, not a prompt control.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, blueHeroView.transform.position), Is.True,
                "The chosen hero is chosen by clicking them.");
            yield return null;

            // The second question is the chosen hero's own: which card leaves their hand. It is a
            // prompt decision, offered as one real control per card, each named as the card it would
            // give up - the committed Stop Projectiles is in the turn area, so four remain.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The discard is the chosen hero's own decision and stops the Attack again.");
            var discardChoices = runtime.RoundView.Model.EffectChoices
                .Where(value =>
                    value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(discardChoices, Has.Length.EqualTo(4),
                "The chosen Wasp holds four cards beside its committed one. Shown controls: " +
                UiPlayer.Describe(hud));
            var blueHandNames = runtime.Cards
                .Where(card => card.OwnerId.Equals(blueHeroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .DisplayName.ToUpperInvariant());
            Assert.That(discardChoices.Select(value => value.Label),
                Is.EquivalentTo(blueHandNames),
                "Each control is named as the card it would give up, never an anonymous ordinal.");
            Assert.That(discardChoices.Select(value => value.Card), Has.All.Not.Null,
                "and each is the whole printed face - milestone 8.15a, at the user's direction.");
            var faceName = discardChoices[0].Card.DisplayName.ToUpperInvariant();
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null, "Both questions are answered.");
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(blueHeroId) &&
                    card.State.Location == CardLocation.DiscardPile),
                Is.EqualTo(1),
                "Exactly the clicked card left the chosen hero's hand.");
            var blueRail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(blueHeroId));
            Assert.That(blueRail.DiscardCards, Has.Count.EqualTo(1),
                "and the discard is on the hero's own rail where a player reads it.");
        }

        /// <summary>
        /// Shock's p.14 step-7 pause, answered again after a retained-prefix Undo and again after a
        /// snapshot restore, both on the surface a player is looking at. Milestone 8.20b.
        ///
        /// <para>The permanent hero player-path gate asks that "at least one pausing path unique to or
        /// first made reachable by the hero must continue after snapshot restore and retained-prefix
        /// Undo through the same visible player surface". Wasp's after-attack rider is that path: it is
        /// her own printed sentence, it stops the Attack twice, and the two stops are answered on two
        /// different surfaces - the board for the hero, the tray's own card faces for the discard - so
        /// the Undo and the restore are each checked against a different one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ShocksAfterAttackPause_IsAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Commit(runtime, TeamSide.Red, WaspCardContent.Shock);
            Commit(runtime, TeamSide.Blue, WaspCardContent.StopProjectiles);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Shock's printed Initiative 8 acts before Stop Projectiles' 3.");

            yield return StartActionThroughControl(root, ActionKind.Attack);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            var minionView = Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null &&
                    view.Occupancy.MinionId.Equals(targetId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, minionView.transform.position), Is.True);
            yield return null;

            // MILESTONE 8.86: the Attack really took the minion, which is the state the Undo and the
            // restore below have to carry across as much as the pause itself.
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(targetId),
                "\"Target a unit adjacent to you\" - the clicked minion is defeated by the Attack.");

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The journey needs Shock's own after-attack pause standing.");
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Not.Empty,
                "Its first question is the board-answered hero selection.");
            var commandsAtBoardQuestion = runtime.AcceptedCommands.Count;

            // Answer the board question, so the pause is standing on its second, card-valued one.
            var blueHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(blueHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, blueHeroView.transform.position), Is.True);
            yield return null;
            Assert.That(EffectCardChoices(runtime), Is.Not.Empty,
                "The chosen hero's own discard stops the Attack again, as whole card faces. " +
                "Shown: " + UiPlayer.Describe(hud));

            // The card-face prompt as a player meets it, which is the half a batch tier cannot see.
            //
            // **Milestone 8.20b recorded a defect here and milestone 8.20d corrected it.** The four
            // faces were the whole of the question and each was named as the card it would give up,
            // but they were appended to the shared contextual row that `BuildActionRow` labels
            // ACTIONS, beneath a round status line still reading "choose an action or End Turn" - so
            // a player being asked which card to discard was labelled as though they were picking an
            // action. It was how every effect choice had been drawn since milestone 8.15, Arien's
            // included, so it was corrected once for all of them rather than inside this hero's gate.
            // The before-picture is `tmp/8-20b-shock-discard-faces-under-the-shared-action-row.png`.
            var prompt = runtime.RoundView.Model.EffectChoicePrompt;
            Assert.That(prompt, Is.Not.Null,
                "A standing decision carries the prompt that names it.");
            Assert.That(prompt.SectionLabel, Is.EqualTo("CARD CHOICE"));
            // Milestone 8.35n: "YOUR CARD CHOICE — " now leads the sentence.
            Assert.That(runtime.RoundView.Model.Status, Does.Contain("Choose 1 of "));
            Assert.That(runtime.RoundView.Model.Status,
                Does.EndWith(" cards for Wasp to discard."),
                "The narrative bar says why these cards are being chosen.");
            Assert.That(runtime.RoundView.Model.Status,
                Does.Not.Contain("choose an action, or end your turn"),
                "which is the line this discard chooser used to arrive under.");

            yield return UiPlayer.WaitForDecisionWindow(runtime, hud);
            // Milestone 8.46e: the window's own eyebrow names the family; the STRIP is what must not
            // print the heading a second time over faces that already say what they are.
            var heading = UiPlayer.Descendants(hud.RootElement)
                .OfType<Label>()
                .Where(label => UiPlayer.IsShown(label) && label.text == prompt.SectionLabel &&
                    UiPlayer.IsOutsideDecisionWindow(label))
                .ToArray();
            Assert.That(heading, Is.Empty,
                "The redundant CARD CHOICE text is absent. " + UiPlayer.Describe(hud));
            // Milestone 8.46e: the offered faces are drawn on the decision window now, under the
            // same `gd-effect-card` name every card-valued choice keeps.
            Assert.That(
                UiPlayer.Descendants(hud.RootElement)
                    .Count(element => element.name == "gd-effect-card" && UiPlayer.IsShown(element) &&
                        !UiPlayer.IsOutsideDecisionWindow(element)),
                Is.EqualTo(EffectCardChoices(runtime).Length),
                "Every offered face is drawn on the decision window. " + UiPlayer.Describe(hud));

            yield return null;
            yield return UiPlayer.CaptureGameView(
                "8-20d", "shock-discard-faces-under-their-own-card-choice-heading");
            var commandsAtCapture = runtime.AcceptedCommands.Count;
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);

            // Retained-prefix Undo first, because a restore adopts a state rather than a history and
            // leaves nothing to undo afterwards.
            //
            // Milestone 8.46b: **ONE BOARD CLICK ON A CHOICE OF EXACTLY ONE IS ONE ACCEPTED COMMAND** -
            // the select carries the confirm the authority offered behind it - so one UNDO gives back
            // the board question the click answered, which is the one step a player walks back
            // through. (Before 8.46b the click was two commands and this took two presses.)
            //
            // The press is also this journey's replay evidence rather than a separate claim: Undo is
            // implemented as a reconstruction of the retained prefix into a fresh aggregate, so what
            // comes back on screen after a press is what replaying those commands produces.
            yield return UiPlayer.WaitForControl(hud, "DIRECTOR");
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtCapture - 1),
                "One UNDO drops exactly one accepted command.");
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtBoardQuestion),
                "and that one command was the whole click, so the board question is given back.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Shock's rider is standing again rather than having resolved.");
            Assert.That(EffectCardChoices(runtime), Is.Empty,
                "The discard the Undo dropped leaves no card face on screen. " +
                UiPlayer.Describe(hud));
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Not.Empty,
                "and the hero question is drawn on the board again, as candidates rather than " +
                "controls. " + UiPlayer.Describe(hud));
            // Close the drawer the same way it was opened, on the toggle rather than on its text: an
            // open drawer relabels its own control to "DIRECTOR" plus a chevron, so the label that
            // opened it finds nothing once it is open.
            if (hud.IsDirectorDrawerOpen)
            {
                UiPlayer.Click(DirectorToggle(hud));
                yield return null;
                Assert.That(hud.IsDirectorDrawerOpen, Is.False,
                    "The clicked toggle must close the drawer before the restored prompt is answered.");
            }

            // The snapshot taken on the discard question, restored onto the same session: it has to
            // come back as a pause a player can answer, not as a state that merely deserializes.
            Assert.That(runtime.RestoreSnapshot(payload), Is.True,
                "The captured payload must restore.");
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is stopped on the same decision it was captured on.");
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "carrying no accepted history, because a restore adopts a state rather than a " +
                "history - which is why the Undo half above is proved on the live session first,");
            var restoredChoices = EffectCardChoices(runtime);
            Assert.That(restoredChoices, Is.Not.Empty,
                "and drawing its own card faces again. " + UiPlayer.Describe(hud));

            // and it is answerable on the same visible surface, by clicking a real printed face.
            var faceName = restoredChoices[0].Card.DisplayName.ToUpperInvariant();
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The click a player made after the restore finished the rider.");
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "and the Attack it interrupted completed.");
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(blueHeroId) &&
                    card.State.Location == CardLocation.DiscardPile),
                Is.EqualTo(1),
                "Exactly the clicked card left the chosen hero's hand.");
            Assert.That(
                UiPlayer.Descendants(hud.RootElement)
                    .Where(element => element.name == "gd-effect-card")
                    .ToArray(),
                Is.Empty,
                "and the answered question leaves no face behind for a stale click to hit.");
        }

        [UnityTest]
        public IEnumerator MagneticDagger_RunsItsAttackAndItsThisTurnPinWearsTheActiveOutline()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);

            yield return CommitThroughTray(root, "MAGNETIC DAGGER");
            yield return CommitThroughTray(root, "LIFT UP");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Magnetic Dagger's printed Initiative 12 acts before Lift Up's 10.");
            var daggerCardId = runtime.SelectedActor.CardInstanceId;

            yield return StartActionThroughControl(root, ActionKind.Attack);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            var minionView = Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null &&
                    view.Occupancy.MinionId.Equals(targetId));
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, minionView.transform.position), Is.True);
            yield return null;

            // MILESTONE 8.86: the Attack's own printed outcome, beside the pin it raises.
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(targetId),
                "\"Target a unit adjacent to you\" - the clicked minion is defeated by the Attack, " +
                "which is what makes the after-attack pin below an AFTER.");

            // "After the attack: This turn:" - the pin is created by the completed Attack and lives
            // until the turn's own cancellation checkpoint, so the player sees the played card as the
            // source of a live Active effect.
            Assert.That(runtime.IsActiveEffectSource(daggerCardId), Is.True,
                "The played Magnetic Dagger sources the live pinning field.");

            // The player reads that on the acting hero's own tray: point the tray at the Red hero by
            // clicking them on the map, then find the played card's slot wearing the violet outline.
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, redHeroView.transform.position), Is.True);
            yield return null;
            var slot = UiPlayer.NamedElementWithText(
                hud, "gd-card-slot", "MAGNETIC DAGGER");
            Assert.That(slot.resolvedStyle.borderTopColor.a, Is.GreaterThan(0.5f),
                "The card's slot wears the Active-effect outline while the pin lives.");

            // The pin ends at the turn's cancellation checkpoint: play the Blue turn out and the
            // outline's source is gone.
            var endTurn = runtime.RoundView.Model.EndTurnInvocation;
            Assert.That(endTurn, Is.Not.Null,
                "Shown controls: " + UiPlayer.Describe(hud));
            UiPlayer.Click(UiPlayer.Button(hud, endTurn.Label));
            yield return null;

            // Milestone 8.35g, and this journey is where the milestone's timing decision is visible
            // on screen. That press finished the card's resolution; the turn's own cancellation
            // checkpoint no longer runs there, it runs at the RELEASE - which is exactly why an undo
            // inside the pause cannot cross checkpoint state. So the pin is still lit here, and the
            // second press is what puts it out.
            Assert.That(runtime.IsActiveEffectSource(daggerCardId), Is.True,
                "p.17's This turn outlives the resolution: the turn is not over until released.");
            var release = runtime.RoundView.Model.EndTurnInvocation;
            Assert.That(release, Is.Not.Null,
                "and the control returns for the release. Shown controls: " + UiPlayer.Describe(hud));
            UiPlayer.Click(UiPlayer.Button(hud, release.Label));
            yield return null;

            // The checkpoint belongs to the TURN rather than to one actor's resolution, so whatever
            // the turn still holds is played out before the pin can be read as ended. The click
            // above is the evidence that the control itself carries the release; the rest is filler
            // and goes through the headless press.
            UiPlayer.EndAnyTurnInProgress(runtime);
            yield return null;

            Assert.That(runtime.IsActiveEffectSource(daggerCardId), Is.False,
                "p.17's This turn ends with the turn, and the outline's source with it.");
        }

        /// <summary>
        /// Milestone 8.21a. Rulebook p.13's Defeat reward reaches the rail of the hero it paid, and no
        /// other rail and no rail's own box moves with it.
        ///
        /// <para>The coins are earned the only way this build earns them - Magnetic Dagger's printed
        /// Attack resolves against p.13's minion arm inside one command - and every step of it is a
        /// real click: both cards are committed through the tray, the Attack is started by its own
        /// control, and the target is chosen by clicking the minion on the board. What the journey then
        /// reads is the number a player reads, off the live element, rather than the projection the
        /// presenter was built from.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheCoinBalance_MovesOnTheRailOfTheHeroTheDefeatPaidAndOnNoOther()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            Assert.That(HeroRailCoinCheck.CoinAmountOn(hud, TeamSide.Red), Is.EqualTo("0"),
                "Nobody has defeated anything yet, and an empty purse is drawn as a nought rather " +
                "than by drawing nothing.");
            Assert.That(HeroRailCoinCheck.CoinAmountOn(hud, TeamSide.Blue), Is.EqualTo("0"));
            Assert.That(runtime.CoinsOf(redHeroId), Is.EqualTo(0));
            var redRailBefore = HeroRailCoinCheck.BoxOf(hud, TeamSide.Red);
            var blueRailBefore = HeroRailCoinCheck.BoxOf(hud, TeamSide.Blue);
            HeroRailCoinCheck.AssertTheCountStandsClear(hud, "the opening position");

            yield return CommitThroughTray(root, "MAGNETIC DAGGER");
            yield return CommitThroughTray(root, "LIFT UP");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Magnetic Dagger's printed Initiative 12 acts before Lift Up's 10.");

            yield return StartActionThroughControl(root, ActionKind.Attack);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            var minionView = Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null && view.Occupancy.MinionId.Equals(targetId));
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, minionView.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.CoinsOf(redHeroId), Is.GreaterThan(0),
                "The clicked Attack has to defeat the minion, or there is no reward to draw.");
            Assert.That(HeroRailCoinCheck.CoinAmountOn(hud, TeamSide.Red),
                Is.EqualTo(runtime.CoinsOf(redHeroId).ToString()),
                "The acting hero's rail shows what p.13 just paid them.");
            Assert.That(HeroRailCoinCheck.CoinAmountOn(hud, TeamSide.Blue), Is.EqualTo("0"),
                "and the other hero's rail is untouched, because the reward was not theirs.");
            Assert.That(runtime.CoinsOf(blueHeroId), Is.EqualTo(0));

            // Milestone 8.25c: compared dimension by dimension inside the panel's own pixel grid
            // rather than as one Rect. A scaled panel snaps every laid-out edge to the nearer device
            // pixel, so two measurements of an unchanged rail can differ in the last bits even when
            // nothing about it moved - and `Rect.Equals` reports that as a difference while printing
            // two identical-looking rectangles, which is exactly the failure this used to give.
            var railSnapping = UiPlayer.PixelSnapping(HeroRailMarkerCheck.RailRowCount);
            foreach (var (side, before) in new[]
            {
                (TeamSide.Red, redRailBefore), (TeamSide.Blue, blueRailBefore)
            })
            {
                var after = HeroRailCoinCheck.BoxOf(hud, side);
                Assert.That(after.x, Is.EqualTo(before.x).Within(UiPlayer.PixelSnapping()), $"{side}");
                Assert.That(after.y, Is.EqualTo(before.y).Within(UiPlayer.PixelSnapping()), $"{side}");
                Assert.That(after.width, Is.EqualTo(before.width).Within(UiPlayer.PixelSnapping()),
                    $"{side}");
                Assert.That(after.height, Is.EqualTo(before.height).Within(railSnapping),
                    "The rail's own box is exactly what it was, net of the marker the tray moved: a " +
                    "balance that resized the rail would move the board framing this milestone may " +
                    $"not touch. ({side})");
            }
            HeroRailCoinCheck.AssertTheCountStandsClear(hud, "the paid Defeat reward");

            yield return UiPlayer.CaptureGameView("8-21a", "the-defeat-reward-on-the-red-rail");
        }

        [UnityTest]
        public IEnumerator StaticBarrier_IsPerformedByItsControlAndItsFieldReadsActiveOnTheTray()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);

            yield return CommitThroughTray(root, "STATIC BARRIER");
            yield return CommitThroughTray(root, "LIFT UP");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Static Barrier's printed Initiative 13 acts before Lift Up's 10.");
            var barrierCardId = runtime.SelectedActor.CardInstanceId;

            // Milestone 8.30d, final direction: an offered primary glows as its whole printed
            // section. Archived from the exact state the Skill click below consumes.
            yield return UiPlayer.CaptureGameView(
                "8-30d2", "the-offered-primary-glows-as-its-section");

            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;
            Assert.That(runtime.IsActiveEffectSource(barrierCardId), Is.True,
                "The performed basic Skill creates the printed two-arm obstacle field.");
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Static Barrier asks nothing; its whole text is the field.");
        }

        [UnityTest]
        public IEnumerator LiftUp_OrbitsTheChosenUnitAndRepeatsOnTheSameTargetThroughRealControls()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);

            yield return CommitThroughTray(root, "LIFT UP");
            yield return CommitThroughTray(root, "STOP PROJECTILES");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Lift Up's printed Initiative 10 acts before Stop Projectiles' 3.");

            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Skill asks which unit or token in the printed Radius 2 to move.");

            // Choose a minion inside the disk on the board. The engaged minion is always one, and any
            // further minion the mid-board position puts in radius is as legal a choice.
            var engagedMinionId = runtime.BoardView.Model.Minions
                .Where(minion => runtime.RoundView.Model.EffectChoiceTargets
                    .Any(target => target.Space.Equals(minion.Coordinate)))
                .OrderBy(minion => minion.MinionId.Value, System.StringComparer.Ordinal)
                .First()
                .MinionId;
            var minionView = Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null &&
                    view.Occupancy.MinionId.Equals(engagedMinionId));
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, minionView.transform.position), Is.True);
            yield return null;

            // The destination is one lit space of the orbit - the spaces equidistant from Wasp.
            //
            // MILESTONE 8.86: it is a space the piece is NOT already standing on. "Move a unit, or a
            // token, in radius 1 space" is an exact step, so the piece's own space is not a legal
            // answer to it - and reading the piece back onto the space that was clicked would be
            // true of a piece that never moved if it ever were.
            var orbitedFrom = runtime.BoardView.Model.Minions
                .Single(minion => minion.MinionId.Equals(engagedMinionId)).Coordinate;
            var destination = TravellingOrbitSpace(runtime, orbitedFrom);
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(
                runtime.BoardView.Model.Minions
                    .Single(minion => minion.MinionId.Equals(engagedMinionId)).Coordinate,
                Is.EqualTo(destination),
                "The chosen piece stands where the clicked orbit space is.");
            Assert.That(
                runtime.BoardView.Model.Minions
                    .Single(minion => minion.MinionId.Equals(engagedMinionId)).Coordinate,
                Is.Not.EqualTo(orbitedFrom),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the piece really left the space it stood " +
                "on, which is what \"move ... 1 space\" says and what a lit own-space would fake.");

            // "May repeat once on the same target": the repeat is a real confirm-or-decline control,
            // and accepting it moves the same piece again without asking which piece.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed May repeat stops the Skill on its own question.");
            // Milestone 8.46e: the printed may is drawn on the decision window, its two answers
            // naming what accepting and declining DO - read off its model rather than the strip's.
            var accept = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Confirm);
            Assert.That(accept, Is.Not.Null,
                "The repeat offers its confirm. Shown controls: " + UiPlayer.Describe(hud));
            UiPlayer.Click(UiPlayer.Button(hud, accept.Label));
            yield return null;

            Assert.That(runtime.RoundView.Model.EffectChoiceDestinations, Is.Not.Empty,
                "The repeated instance goes straight to the same piece's destinations.");
            var secondDestination = TravellingOrbitSpace(runtime, destination);
            var secondTile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(secondDestination));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, secondTile.transform.position), Is.True);
            yield return null;
            Assert.That(
                runtime.BoardView.Model.Minions
                    .Single(minion => minion.MinionId.Equals(engagedMinionId)).Coordinate,
                Is.EqualTo(secondDestination),
                "The same piece moved again to the second clicked space.");
            Assert.That(secondDestination, Is.Not.EqualTo(destination),
                "and the repeat is a second MOVE rather than a second seating on the same space.");
        }

        /// <summary>
        /// One lit orbit space that is not the space the piece already stands on. Milestone 8.86:
        /// "move ... 1 space" is an exact step, so a destination equal to the subject's own space
        /// would be a move that moved nothing - and a journey reading the piece back onto the space
        /// it clicked could not tell the two apart.
        /// </summary>
        private static HexCoordinate TravellingOrbitSpace(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate from)
        {
            var offered = runtime.RoundView.Model.EffectChoiceDestinations
                .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate)
                .ToArray();
            Assert.That(offered, Is.Not.Empty,
                "The orbit offers at least one destination on this board.");
            var travelling = offered.Where(value => !value.Equals(from)).ToArray();
            Assert.That(travelling, Is.Not.Empty,
                "\"Move a unit, or a token, in radius 1 space\" - the only space offered was the one " +
                "the piece already stood on, so the Skill could not move it. Offered: " +
                string.Join(", ", offered.Select(value => value.ToString())));
            return travelling[0];
        }

        [UnityTest]
        public IEnumerator StopProjectiles_ClickedAgainstAMeleeAttackDefeatsItsHeroWhoRespawnsByClick()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Round 1: the Blue Wasp closes the two-space gap to adjacency with its secondary
            // Movement; the Red Wasp holds. Filler decisions ride the established headless surface -
            // the journey's own subject starts at the Defense response below.
            Commit(runtime, TeamSide.Red, WaspCardContent.Shock);
            Commit(runtime, TeamSide.Blue, WaspCardContent.MagneticDagger);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId),
                "Blue's Magnetic Dagger at 12 acts before Red's Shock at 8.");
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var redAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate;
            var step = runtime.PendingMovementChoice.LegalDestinations
                .First(value => HexDistanceQuery.Calculate(value.Destination, redAt) == 1);
            Assert.That(runtime.BoardPresenter.SubmitDestination(step.Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its turn.");
            for (var budget = 0; runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                Assert.That(budget, Is.LessThan(40),
                    "The end of round must complete within a bounded number of answers.");
                AnswerAnyEndOfRoundPause(runtime);
            }

            // Round 2: Blue attacks the now-adjacent Red hero with its own unspent Shock, which
            // targets any adjacent unit - the Magnetic Dagger was resolved by round 1's Movement.
            // Red commits Lift Up and Holds, so its Stop Projectiles stays in the hand p.14's
            // Defense response is offered from.
            Commit(runtime, TeamSide.Blue, WaspCardContent.Shock);
            Commit(runtime, TeamSide.Red, WaspCardContent.LiftUp);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Red's Lift Up at 10 acts before Blue's Shock at 8.");
            // Milestone 8.35v: one press both Holds and releases.
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its turn.");
            Assert.That(runtime.PendingTurnRelease, Is.Null);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId));
            var attack = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds,
                Does.Contain(redHeroId),
                "The adjacent Red hero is a legal Shock target.");
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, redHeroView.transform.position), Is.True);
            yield return null;

            // The journey's subject: p.11's Defense response as the defender's own clicked card face.
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            var lifeBefore = hud.Header.RedLifeLabel;
            // Milestone 8.58, the user's last word on the Defense window: "Remove it and keep the old
            // mouseover when mousing over the defense cards." The panel prints no arithmetic of its
            // own; resting the pointer on a candidate opens the SAME per-card breakdown the strip's
            // offer opened, on the same layer, because the decision moving into the window did not
            // move the popup with it. Proved on a live pause, where there is a real
            // `PendingDefensePreview` to break down.
            var defenseCard =
                UiPlayer.NamedElementWithText(hud, "gd-defense-card", "STOP PROJECTILES");
            UiPlayer.EnsureVisible(defenseCard);
            yield return null;
            var breakdown = UiPlayer.Descendants(hud.RootElement)
                .Single(element => element.name == "gd-defense-preview");
            Assert.That(UiPlayer.IsShown(breakdown), Is.False,
                "Nothing is open before the pointer rests on a face.");
            UiPlayer.PointAt(defenseCard);
            yield return null;
            Assert.That(UiPlayer.IsShown(breakdown), Is.True,
                "The old mouseover breakdown opens from inside the window.");
            Assert.That(runtime.RoundView.ActiveDefensePreview, Is.Not.Null);
            Assert.That(
                UiPlayer.Descendants(breakdown).OfType<Label>()
                    .Select(element => element.text),
                Has.Some.StartsWith("ATTACK TOTAL"),
                "and it is the whole reading - attack, defense, result - not a chip.");
            UiPlayer.PointAway(defenseCard);
            for (var frame = 0; frame < 300 && UiPlayer.IsShown(breakdown); frame++)
            {
                yield return null;
            }

            Assert.That(UiPlayer.IsShown(breakdown), Is.False,
                "and it closes again when the pointer leaves.");

            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-defense-card", "STOP PROJECTILES"));
            yield return null;
            UiPlayer.Click(
                UiPlayer.NamedElementWithText(hud, "gd-defense-card", "STOP PROJECTILES"));
            yield return null;

            // p.14: the Block's condition - a ranged attack - is unfulfilled against Shock, so the
            // defender is defeated whatever the totals, and p.13's consequences follow.
            Assert.That(runtime.PendingDefenseChoice, Is.Null);
            Assert.That(hud.Header.RedLifeLabel, Is.Not.EqualTo(lifeBefore),
                "The defeat spends a Red Life, and the status strip says so.");

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(redHeroId)), Is.False,
                "The defeated Wasp is off the board until it respawns.");

            // p.13's respawn is offered at the defeated hero's own next turn, which is round 3's.
            // The rest of round 2 and its end are filler; the journey resumes at the respawn.
            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            for (var budget = 0; runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                Assert.That(budget, Is.LessThan(40),
                    "The second end of round must complete within a bounded number of answers.");
                AnswerAnyEndOfRoundPause(runtime);
            }

            CommitHighestAgainstLowest(runtime, redHeroId);
            yield return null;

            // The defeated hero's own respawn is a board decision: the offered spawn spaces are
            // marked, and clicking one puts the hero there before any ordinary action is offered.
            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Wasp must be offered its respawn at its own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawnOption = runtime.RoundView.Model.HeroRespawnOptions.First();
            var respawnTile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(respawnOption.Destination));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, respawnTile.transform.position), Is.True,
                "The respawn space is answered on the board.");
            yield return null;
            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Empty,
                "The answered respawn withdraws its options.");
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate,
                Is.EqualTo(respawnOption.Destination),
                "The Red Wasp stands on the clicked spawn space.");
        }

        /// <summary>
        /// Milestone 8.16c's live regression. A seeded walk reaches the real blocked-Spawn-point Lane-Push
        /// pause through projected commands. Setup is driven headlessly because none of those
        /// decisions is this case's subject; the pending answer itself uses the configured
        /// camera ray, HUD pointer exclusion, tile collider, board view, presenter, and exact command.
        /// </summary>
        [UnityTest]
        public IEnumerator BlockedLanePushRespawnPlace_LightsAndAnswersEverySpaceOnTheBoard()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var random = new SeededChoice(58);
            var commandCount = 0;

            while (runtime.SystemTransitionPrompt?.Kind !=
                SystemTransitionPromptKind.LanePushRespawnPlace)
            {
                // Milestone 8.35g re-derived the seed offline over seeds 1-60 rather than by
                // bisecting Unity runs, using a copy of EnumerateProjectedCommands and this
                // fixture's own SeededChoice. The release draws from the same stream even when it
                // is the only legal command, so seed 10 now walks a different match entirely and
                // ends it in round 30 without ever reaching this pause - the offline probe reproduces
                // that number exactly, which is what says it walks the same walk. Seed 7 reaches the
                // real pause at command 324 in round 9, a mid-match arrival like the one this case
                // has always been about. The probe must deal the VERIFIED mirror deck this scene
                // deals: a first scan against the internal test deck disagreed with Unity by twenty
                // rounds, because a different deck is a different walk.
                //
                // Milestone 8.35u re-derived it again, 7 -> 4, by the same offline method and for
                // the same class of reason: rulebook p.15 now asks every hero that bought a level at
                // once, so the number of standing answers at an end of round changed and every
                // trajectory diverges from the first level-up onward. Seed 7 no longer reaches this
                // pause at all - it is still playing in round 46 at 1,400 commands - and the probe
                // reproduces that before its answer is trusted, which is what says it walks the same
                // walk. Of sixty seeds seven reach the pause (29, 31, 35, 37, 42, 45,
                // 47); seed 29 reaches it at command 346 in round 10, which is within twenty
                // commands and one round of the arrival seed 7 used to give - the case keeps its
                // shape rather than taking the earliest arrival available.
                //
                // THE PROBE HAD TO BE CORRECTED BEFORE IT COULD BE BELIEVED, and that is the
                // reusable half: a first scan composed this mirror through ConfiguredMatchFixtures,
                // whose staging is WithBlueHeroBesideTheRedOne, while the SCENE stages with
                // EngagedMirrorStaging.Restage. A different staging is a different walk exactly as a
                // different deck is, Unity disproved that scan's answer on the first focused run,
                // and the corrected probe reproduces BOTH live numbers to the command - seed 4 ends
                // with no command at round 9 turn 4, seed 7 stops at round 36 turn 1 in commitments.
                //
                // Milestone 8.46b re-derived it a third time, 29 -> 58, by the same corrected probe:
                // a choice of exactly one now answers in ONE press, so every trajectory diverges from
                // the first such choice onward. Seed 29 now plays to round 35 turn 2 at 1,100
                // commands - the probe reproduces that live number to the command, which is what
                // says it still walks the same walk. Of sixty seeds five reach the pause (14, 45,
                // 49, 56, 58); seed 58 reaches it at command 203 in round 6, the latest arrival
                // available, which keeps this the mid-match case it has always been.
                Assert.That(commandCount, Is.LessThan(1100),
                    $"Seed 58 did not reach the blocked Lane-Push respawn pause; stopped in " +
                    $"round {runtime.RoundIndex}, turn {runtime.TurnIndex}, phase {runtime.Phase}.");
                var offered = runtime.EnumerateProjectedCommands();
                Assert.That(offered, Is.Not.Empty,
                    $"The live match offered no command in round {runtime.RoundIndex}, " +
                    $"turn {runtime.TurnIndex}, phase {runtime.Phase}.");
                var chosen = offered[random.Next(offered.Count)];
                Assert.That(runtime.SubmitProjectedCommand(chosen), Is.True,
                    $"Projected command {commandCount + 1} ({chosen.Kind}) was refused.");
                commandCount++;
            }

            yield return null;

            var prompt = runtime.SystemTransitionPrompt;
            var destinations = runtime.BoardView.Model.SystemTransitionDestinations;
            Assert.That(destinations, Has.Count.EqualTo(prompt.Options.Count),
                "Every equally-nearest empty space is projected before the pointer moves.");
            Assert.That(destinations.Select(value => value.Command.SystemTransitionChoice),
                Is.EqualTo(prompt.Options.Select(option => option.Command.SystemTransitionChoice)),
                "Each lit space keeps the exact continuation invocation it answers.");
            Assert.That(destinations, Is.Not.Empty);

            var tiles = Object.FindObjectsByType<HexTileView>();
            foreach (var destination in destinations)
            {
                var offered = tiles.Single(tile => tile.Coordinate.Equals(destination.Destination));
                Assert.That(offered.IsHighlighted, Is.False);
                Assert.That(offered.CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.Movement),
                    $"Offered placement space {destination.Destination} lacks green corner marks.");
            }

            Assert.That(UiPlayer.Describe(root.Hud), Does.Not.Contain("PLACE AT"),
                "The HUD explains the pause but must not duplicate its board-valued answers.");

            if (!UnityEngine.Application.isBatchMode)
            {
                var screenshotPath = Path.GetFullPath("tmp/8-16c-lane-push-board-choice.png");
                if (File.Exists(screenshotPath))
                {
                    File.Delete(screenshotPath);
                }

                ScreenCapture.CaptureScreenshot(screenshotPath);
                yield return new WaitForEndOfFrame();
                yield return null;
                Assert.That(File.Exists(screenshotPath), Is.True,
                    "The targeted Lane-Push Game View evidence must be captured under tmp/.");
            }

            var unoffered = tiles.First(tile => tile.IsPlayable &&
                destinations.All(value => !value.Destination.Equals(tile.Coordinate)));
            Assert.That(runtime.BoardPresenter.SubmitDestination(unoffered.Coordinate), Is.False,
                "A coordinate the continuation did not offer executes nothing.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var camera = pointerInput.EventCamera;
            var chosenDestination = destinations.First(value =>
            {
                var tile = tiles.Single(candidate => candidate.Coordinate.Equals(value.Destination));
                var screen = camera.WorldToScreenPoint(tile.transform.position);
                return screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width &&
                    screen.y >= 0f && screen.y <= Screen.height &&
                    !root.Hud.BlocksPointerAt(new Vector2(screen.x, screen.y));
            });
            var chosenTile = tiles.Single(tile =>
                tile.Coordinate.Equals(chosenDestination.Destination));
            var minionsBefore = runtime.BoardView.Model.Minions
                .Select(minion => minion.MinionId).ToArray();

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, chosenTile.transform.position), Is.True,
                "The highlighted destination must be answerable through the configured camera ray.");
            yield return null;

            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.AnswerSystemTransitionStep));
            Assert.That(runtime.AcceptedCommands.Last().SystemTransitionChoice,
                Is.EqualTo(chosenDestination.Command.SystemTransitionChoice),
                "The click forwarded the exact invocation paired with the chosen space.");
            Assert.That(runtime.BoardView.Model.Minions.Any(minion =>
                    !minionsBefore.Contains(minion.MinionId) &&
                    minion.Coordinate.Equals(chosenDestination.Destination)),
                Is.True,
                "The respawning minion now stands in the clicked nearest empty space.");
            Assert.That(runtime.BoardView.Model.SystemTransitionDestinations.Any(value =>
                    value.Destination.Equals(chosenDestination.Destination)),
                Is.False,
                "The answered space is no longer offered by this or a following pause.");
            Assert.That(chosenTile.IsHighlighted, Is.False,
                "The accepted answer clears its selectable-space cue.");
            Assert.That(chosenTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.None));
            Assert.That(runtime.BoardPresenter.SubmitDestination(chosenDestination.Destination),
                Is.False,
                "A repeated click on the answered space executes nothing.");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        private static HeroId HeroOf(ConfiguredMatchSceneRuntime runtime, TeamSide side) =>
            runtime.Setup.Heroes.Single(hero => hero.TeamSide == side).HeroId;

        /// <summary>
        /// The Director drawer's own toggle, found by the one label prefix it keeps in both states.
        /// Milestone 8.20b.
        /// </summary>
        private static UnityEngine.UIElements.VisualElement DirectorToggle(
            ConfiguredMatchHudRenderer hud)
        {
            var toggles = UiPlayer.Descendants(hud.RootElement)
                .OfType<UnityEngine.UIElements.Button>()
                .Where(button => UiPlayer.IsShown(button) && button.text != null &&
                    button.text.StartsWith("DIRECTOR", System.StringComparison.Ordinal))
                .ToArray();
            Assert.That(toggles, Has.Length.EqualTo(1),
                "Exactly one shown control opens and closes the Director drawer; shown: " +
                UiPlayer.Describe(hud));
            return toggles[0];
        }

        /// <summary>
        /// The card-valued answers the standing effect pause is drawing, each carrying its whole
        /// printed face under milestone 8.15a's contract. Milestone 8.20b.
        /// </summary>
        private static TypedInvocationViewModel[] EffectCardChoices(
            ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoices
                .Where(value =>
                    value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    value.Card != null)
                .ToArray();

        private sealed class SeededChoice
        {
            private uint state;

            public SeededChoice(int seed)
            {
                state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
            }

            public int Next(int exclusiveUpperBound)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return (int)(state % (uint)exclusiveUpperBound);
            }
        }

        /// <summary>
        /// Commits the round so the named hero acts first: their highest-Initiative in-hand card
        /// against the other hand's lowest. A cross-team tie resolves to the Tie Breaker side, which
        /// `DEV-005` fixes to Red - the hero these journeys give the first turn to.
        /// </summary>
        private static void CommitHighestAgainstLowest(
            ConfiguredMatchSceneRuntime runtime, HeroId firstActorHeroId)
        {
            foreach (var hero in runtime.Setup.Heroes)
            {
                var cards = runtime.Cards
                    .Where(card => card.OwnerId.Equals(hero.HeroId) &&
                        card.State.Location == CardLocation.Hand)
                    .OrderBy(card => runtime.Definitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .Initiative.Value)
                    .ToArray();
                Assert.That(cards, Is.Not.Empty,
                    $"The {hero.TeamSide} hand must hold a committable card.");
                var chosen = hero.HeroId.Equals(firstActorHeroId) ? cards.Last() : cards.First();
                Assert.That(runtime.SelectCommitmentCard(chosen.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            }
        }

        /// <summary>Headless filler commitment for turns that are not the journey's subject.</summary>
        private static void Commit(
            ConfiguredMatchSceneRuntime runtime, TeamSide side, CardDefinitionId definitionId)
        {
            // Milestone 8.35g. Every actor now costs two presses of the same control, so the
            // commitment phase this reads into is not open until the turn in progress is finished.
            UiPlayer.EndAnyTurnInProgress(runtime);

            var heroId = runtime.Setup.Heroes.Single(hero => hero.TeamSide == side).HeroId;
            var card = runtime.Cards.Single(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True,
                $"{definitionId.Value} must be selectable from the {side} hand.");
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
        }

        /// <summary>
        /// Answers whichever end-of-round pause stands with its first projected option, then
        /// advances. Filler for a journey whose subject is elsewhere.
        /// </summary>
        private static void AnswerAnyEndOfRoundPause(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.PendingMinionBattleChoice != null)
            {
                var battle = runtime.MinionChoiceView.Model;
                if (battle != null && battle.Candidates.Count > 0)
                {
                    Assert.That(runtime.SubmitMinionChoiceCandidate(
                        battle.Candidates.First(value => value.Destination == null).MinionId),
                        Is.True);
                }

                Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                return;
            }

            if (runtime.PausedSystemTransitionOptions.Count > 0)
            {
                Assert.That(
                    runtime.AnswerPausedSystemTransition(runtime.PausedSystemTransitionOptions[0]),
                    Is.True);
                return;
            }

            Assert.That(runtime.ContinueEndOfRound(), Is.True);
        }

        /// <summary>
        /// Commits the currently viewed hand's named card through the real tray controls.
        /// </summary>
        private static IEnumerator CommitThroughTray(
            MatchSceneCompositionRoot root, string printedName)
        {
            var hud = root.Hud;
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-card-choice", printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-card-choice", printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "CONFIRM FACEDOWN"));
            yield return null;
        }

        /// <summary>Starts the selected actor's one action of the given kind by its real control.</summary>
        private static IEnumerator StartActionThroughControl(
            MatchSceneCompositionRoot root, ActionKind kind)
        {
            // One frame first, so a contextual region rebuilt this frame has resolved its layout.
            yield return null;
            var runtime = root.Runtime;
            var action = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == kind);
            UiPlayer.Click(UiPlayer.Button(root.Hud, action.Label));
            yield return null;
        }

        private static ActionKind ActionKindOf(
            ConfiguredMatchSceneRuntime runtime, ActionDefinitionId actionId)
        {
            var offer = runtime.AvailableTurnActions.Single(value =>
                value.ActionDefinitionId.Equals(actionId));
            return runtime.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(actionId)).Kind;
        }

        private static IEnumerator LoadEngagedScene()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent =
                SceneCardContent.VerifiedEngagedMirror;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

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
