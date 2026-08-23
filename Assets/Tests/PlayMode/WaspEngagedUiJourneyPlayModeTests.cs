using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
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
            Assert.That(minionView.TargetCueState, Is.EqualTo(BoardTargetCueState.Eligible),
                "The one legal target is marked where it stands.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, minionView.transform.position), Is.True);
            yield return null;

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
            var slot = UiPlayer.Descendants(hud.RootElement)
                .Single(element => element.name == "gd-card-slot" &&
                    UiPlayer.Descendants(element).Any(child =>
                        child.name == "gd-card-name" &&
                        child is UnityEngine.UIElements.Label label &&
                        label.text == "MAGNETIC DAGGER"));
            Assert.That(slot.resolvedStyle.borderTopColor.a, Is.GreaterThan(0.5f),
                "The card's slot wears the Active-effect outline while the pin lives.");

            // The pin ends at the turn's cancellation checkpoint: play the Blue turn out and the
            // outline's source is gone.
            var endTurn = runtime.RoundView.Model.EndTurnInvocation;
            Assert.That(endTurn, Is.Not.Null,
                "Shown controls: " + UiPlayer.Describe(hud));
            UiPlayer.Click(UiPlayer.Button(hud, endTurn.Label));
            yield return null;
            Assert.That(runtime.IsActiveEffectSource(daggerCardId), Is.False,
                "p.17's This turn ends with the turn, and the outline's source with it.");
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
            Assert.That(runtime.RoundView.Model.EffectChoiceDestinations, Is.Not.Empty,
                "The orbit offers at least one destination on this board.");
            var destination = runtime.RoundView.Model.EffectChoiceDestinations
                .First().Command.EffectChoice.Candidate.Value.Coordinate;
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.True,
                "The orbit destination the player is about to click is lit.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(
                runtime.BoardView.Model.Minions
                    .Single(minion => minion.MinionId.Equals(engagedMinionId)).Coordinate,
                Is.EqualTo(destination),
                "The chosen piece stands where the clicked orbit space is.");

            // "May repeat once on the same target": the repeat is a real confirm-or-decline control,
            // and accepting it moves the same piece again without asking which piece.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed May repeat stops the Skill on its own question.");
            var accept = runtime.RoundView.Model.EffectChoices.SingleOrDefault(value =>
                value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(accept, Is.Not.Null,
                "The repeat offers its confirm. Shown controls: " + UiPlayer.Describe(hud));
            UiPlayer.Click(UiPlayer.Button(hud, accept.Label));
            yield return null;

            Assert.That(runtime.RoundView.Model.EffectChoiceDestinations, Is.Not.Empty,
                "The repeated instance goes straight to the same piece's destinations.");
            var secondDestination = runtime.RoundView.Model.EffectChoiceDestinations
                .First().Command.EffectChoice.Candidate.Value.Coordinate;
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
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its turn.");
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

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        private static HeroId HeroOf(ConfiguredMatchSceneRuntime runtime, TeamSide side) =>
            runtime.Setup.Heroes.Single(hero => hero.TeamSide == side).HeroId;

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
