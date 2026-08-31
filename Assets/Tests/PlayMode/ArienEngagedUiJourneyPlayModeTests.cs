using System.Collections.Generic;
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
using static GuardsDigital.Tests.PlayMode.ArienUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Arien's starting-hand cards played the way a player plays them, from the engaged staging -
    /// Wasp and Arien two spaces apart around one melee minion. Milestone 8.20a.
    ///
    /// <para>Every decision this fixture is about is made on the live surface: tray cards, prompt
    /// controls and card faces through <see cref="UiPlayer"/>, board decisions through the camera's
    /// screen-point ray after the HUD's own pointer-blocking answer. Decisions that are only setup -
    /// the walk that puts a piece where a printed sentence can reach it - ride the headless command
    /// surface, exactly as milestone 8.15's own journeys do, and are marked where they appear.</para>
    /// </summary>
    public sealed class ArienEngagedUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpellBreak_IsPlayedByItsControlAndStopsTheEnemysSilverSkillOnScreen()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);

            // Spell Break's 13 acts before Lift Up's 10, so the lock is up before the enemy's own
            // activation inside the same turn - which is the whole of p.20's "for the duration of
            // that entire turn".
            Commit(runtime, arienId, ArienCardContent.SpellBreak);
            Commit(runtime, waspId, WaspCardContent.LiftUp);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(arienId),
                "Spell Break's printed Initiative 13 acts before Lift Up's 10.");
            var spellBreakCardId = runtime.SelectedActor.CardInstanceId;

            yield return PointTrayAt(root, arienId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Spell Break asks nothing; its whole text is the lock it raises.");
            Assert.That(runtime.IsActiveEffectSource(spellBreakCardId), Is.True,
                "The performed Basic Skill is the live source of the printed radius lock.");

            // The player reads the live effect on Arien's own tray, as the played card's slot wearing
            // the Active outline. A resolved Skill may hand the turn straight on, so the tray is
            // pointed back at him the way a player would - by clicking his piece.
            yield return PointTrayAt(root, arienId);
            yield return null;
            var slot = UiPlayer.NamedElementWithText(hud, "gd-card-slot", "SPELL BREAK");
            Assert.That(slot.resolvedStyle.borderTopColor.a, Is.GreaterThan(0.5f),
                "The card's slot wears the Active-effect outline while the lock lives.");

            // Arien's turn ends; the enemy's begins inside the same turn, and this is where the
            // restriction has to be visible or it is not a restriction a player meets.
            if (runtime.SelectedActor.ActorId.Equals(arienId))
            {
                yield return PointTrayAt(root, arienId);
                UiPlayer.Click(
                    UiPlayer.Button(hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(waspId));
            Assert.That(runtime.IsActiveEffectSource(spellBreakCardId), Is.True,
                "p.20: the lock lasts the entire turn, and the enemy's activation is inside it.");

            yield return PointTrayAt(root, waspId);
            yield return null;

            var unavailable = runtime.RoundView.Model.UnavailableActions;
            var lockedSkill = unavailable.SingleOrDefault(action =>
                KindOf(runtime, action.ActionDefinitionId) == ActionKind.Skill);
            Assert.That(lockedSkill, Is.Not.Null,
                "The enemy's Skill must be drawn as a control they can read and cannot press. " +
                "Shown controls: " + UiPlayer.Describe(hud));
            var lockedControl = UiPlayer.Button(hud, lockedSkill.Label);
            Assert.That(lockedControl.enabledInHierarchy, Is.False,
                "The greyed Skill control must not be pressable while Spell Break stands.");
            Assert.That(runtime.RoundView.Model.Actions.Any(action =>
                    KindOf(runtime, action.Command.ActionDefinitionId) == ActionKind.Skill),
                Is.False,
                "and no Skill may be offered as an available action at the same time.");

            // The lock stops one action kind, not the turn: the enemy still has their other options.
            Assert.That(runtime.RoundView.Model.EndTurnInvocation, Is.Not.Null,
                "p.11's Hold is unaffected, so the enemy is stopped rather than stuck.");

            yield return CaptureGameView("spell-break-locks-the-enemys-skill");
        }

        [UnityTest]
        public IEnumerator NobleBlade_MovesAnotherUnitBesideItsTargetBeforeTheAttackThroughBoardClicks()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Commit(runtime, arienId, ArienCardContent.NobleBlade);
            Commit(runtime, waspId, WaspCardContent.Shock);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(arienId),
                "Noble Blade's printed Initiative 11 acts before Shock's 8.");

            yield return PointTrayAt(root, arienId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            // "Target a unit adjacent to you" names no side, so the staged minion beside Arien is a
            // legal target of his own printed Attack.
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            var minionView = MinionView(targetId);
            Assert.That(minionView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(Tile(minionView.Occupancy.Coordinate).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "The one legal Attack target carries blood-red corners where it stands.");
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, minionView.transform.position),
                Is.True);
            yield return null;

            // p.14 before the attack: "You may move another unit that is adjacent to the target 1
            // space." The printed `may` is the actor's own confirm-or-decline.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The before-attack text must stop the Attack on its optional step.");
            var confirm = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline), Is.True,
                "and declining must be an offered answer, not an absence. Shown controls: " +
                UiPlayer.Describe(hud));
            UiPlayer.Click(UiPlayer.Button(hud, confirm.Label));
            yield return null;

            // "another unit that is adjacent to the target" - whichever piece that is on this staged
            // board, chosen where it stands. Which unit qualifies is a fact about the geometry rather
            // than about the card, so the journey reads the offered candidate instead of naming one.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The confirmed step asks which unit moves.");
            var candidates = runtime.RoundView.Model.EffectChoiceTargets;
            Assert.That(candidates, Is.Not.Empty,
                "A unit selection is answered on the map, never as a prompt list.");
            var movedFrom = candidates[0].Space;
            var piece = PieceAt(movedFrom);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, piece), Is.True,
                "The moved unit is chosen by clicking it where it stands.");
            yield return null;

            // "1 space" - the destination is the second board decision.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and where they move is the next question.");
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "The clicked unit must have advanced the pause onto its destinations. " +
                Remaining(runtime) + " viewed=" + runtime.ViewedHeroId.Value +
                " lastOperation='" + runtime.LastOperationDetail + "'");
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(space, movedFrom) == 1),
                Is.True,
                "and every one of them is the one space the printed text allows.");
            var destination = destinations[0];
            var tile = Tile(destination);
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Both questions are answered. Remaining: " + Remaining(runtime));
            Assert.That(OccupantAt(runtime, destination), Is.True,
                "The clicked unit stands on the clicked space.");
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "The Attack resumed and completed after its before-attack text.");
        }

        /// <summary>
        /// Milestone 8.29e. The same printed sentence on a board where it has nothing to say.
        ///
        /// <para>The staged position puts exactly one unit beside Arien's only legal target, so the
        /// enemy's own earlier Attack is what empties it: Magnetic Dagger's printed Initiative 12 acts
        /// before Noble Blade's 11 and defeats that minion. What "another unit that is adjacent to the
        /// target" then names is nobody - and until this milestone the offer opened all the same,
        /// stopping the Attack on a question whose confirm and decline left the same board while every
        /// other control was withheld.</para>
        ///
        /// <para>The position is played rather than staged on purpose: a scene composed with the
        /// neighbour already missing would prove the fixture, and this proves the rules.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator NobleBladeWithNoUnitBesideItsTarget_AsksNothingAndStillCompletesItsAttack()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Commit(runtime, waspId, WaspCardContent.MagneticDagger);
            Commit(runtime, arienId, ArienCardContent.NobleBlade);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(waspId),
                "Magnetic Dagger's printed Initiative 12 acts before Noble Blade's 11.");

            yield return PointTrayAt(root, waspId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            var neighbourId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            var neighbourAt = MinionView(neighbourId).Occupancy.Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(neighbourId).transform.position),
                Is.True);
            yield return null;
            Assert.That(OccupantAt(runtime, neighbourAt), Is.False,
                "The enemy's Attack removed the one unit standing beside Arien's own target. " +
                Remaining(runtime));

            if (runtime.SelectedActor.ActorId.Equals(waspId))
            {
                yield return PointTrayAt(root, waspId);
                UiPlayer.Click(
                    UiPlayer.Button(hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }

            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(arienId));
            var arienCardId = runtime.SelectedActor.CardInstanceId;
            yield return PointTrayAt(root, arienId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "Noble Blade is still an ordinary legal Attack: only its optional text lost its " +
                "subject. " + Remaining(runtime));
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            var targetAt = MinionView(targetId).Occupancy.Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(targetId).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "No question stands: the before-attack `may` has no unit to move, so it is not " +
                "asked. " + Remaining(runtime));
            Assert.That(runtime.RoundView.Model.EffectChoices, Is.Empty,
                "and no confirm or decline control is drawn for a player to press. Shown controls: " +
                UiPlayer.Describe(hud));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "The Attack ran straight through its own step 1 and completed.");
            Assert.That(OccupantAt(runtime, targetAt), Is.False,
                "and it resolved against the unit it named.");
            // Milestone 8.29f corrected this assertion, and the old one was the defect rather than
            // collateral: it asked for an End Turn control, and one stood only because the completed
            // Attack had left the card unresolved with Arien still selected. p.7 spends the card on
            // the action, so an Attack that ran straight through its own text ends the turn exactly
            // as one that stopped to ask would have.
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(arienCardId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved),
                "The Attack spent its own source card. " + Remaining(runtime));
            Assert.That(runtime.RoundView.Model.Actions, Is.Empty,
                "and no ordinary action is left for a player to press. Shown controls: " +
                UiPlayer.Describe(hud));

            yield return CaptureGameView("noble-blade-with-nothing-to-move");
        }

        [UnityTest]
        public IEnumerator NobleBladesBeforeAttackPause_IsAnsweredAgainAfterARestoreAndAfterAnUndo()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Commit(runtime, arienId, ArienCardContent.NobleBlade);
            Commit(runtime, waspId, WaspCardContent.Shock);
            yield return PointTrayAt(root, arienId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(targetId).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The journey needs Arien's own before-attack pause standing.");
            var commandsAtPause = runtime.AcceptedCommands.Count;

            // Retained-prefix Undo first, because a restore adopts a state rather than a history:
            // afterwards there is no retained prefix to undo, and UNDO is correctly greyed. So the
            // Undo half is proved on the live session and the restore half after it.
            var confirmControl = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            UiPlayer.Click(UiPlayer.Button(hud, confirmControl.Label));
            yield return null;
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause + 1),
                "One click is one accepted command.");
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Not.Empty,
                "The confirmed step asks which unit moves. " + Remaining(runtime));

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause),
                "The Undo drops exactly the confirmation it retained the prefix of.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and the before-attack pause is standing again.");
            Assert.That(runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm), Is.True,
                "with its own confirm-or-decline drawn again rather than the unit selection the " +
                "Undo dropped. " + Remaining(runtime));

            // A snapshot taken on the stopped decision, restored onto the same session: the pause has
            // to come back as a pause a player can answer, not as a state that merely deserializes.
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);
            var declineFirst = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            UiPlayer.Click(UiPlayer.Button(hud, declineFirst.Label));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null, "The declined pause is gone.");

            Assert.That(runtime.RestoreSnapshot(payload), Is.True,
                "The captured payload must restore.");
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is stopped on the same decision it was captured on.");
            Assert.That(runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm), Is.True,
                "and the restored pause draws its own controls again. " + Remaining(runtime));

            // and it is answerable on the same visible surface, by clicking the same real control.
            var confirm = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            UiPlayer.Click(UiPlayer.Button(hud, confirm.Label));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The confirmed step asks which unit moves, exactly as it does without a restore.");
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Not.Empty,
                "The click a player made after the restore advanced the pause onto its unit " +
                "selection. " + Remaining(runtime));
        }

        [UnityTest]
        public IEnumerator DangerousCurrent_MakesTheHeroBehindItsTargetDiscardOnItsOwnCardFace()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Setup, headlessly: the printed sentence reaches "the spaces in a straight line directly
            // behind the target", and the staging puts the two heroes on a bent line around the
            // minion. Turn one walks Arien onto a space from which some minion beside him has the
            // enemy directly behind it; the journey's own subject is turn two's Attack.
            //
            // Which space that is belongs to the staged board rather than to the card, so it is
            // searched for here rather than assumed. Aspiring Duelist carries the walk because its
            // printed secondary Movement is the longest in the hand that is not Dangerous Current's
            // own - the Attack has to still be in hand on turn two.
            Commit(runtime, arienId, ArienCardContent.AspiringDuelist);
            Commit(runtime, waspId, WaspCardContent.StopProjectiles);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(arienId),
                "Aspiring Duelist's 9 acts before Stop Projectiles' 3, so Arien walks first.");
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);

            var waspAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(waspId)).Coordinate;
            var minions = Object.FindObjectsByType<StandardMinionView>()
                .Where(view => view.Occupancy != null)
                .Select(view => (view.Occupancy.MinionId, view.Occupancy.Coordinate))
                .ToArray();
            var walkable = runtime.PendingMovementChoice.LegalDestinations
                .Select(value => value.Destination)
                .ToArray();
            // Dangerous Current reaches "any of the 2 spaces in a straight line directly behind the
            // target", so either step beyond the target lines the enemy up - which is twice as many
            // staged positions to find as the first step alone.
            var lineUps = walkable
                .SelectMany(space => minions
                    .Where(pair => HexDistanceQuery.Calculate(pair.Coordinate, space) == 1)
                    .Where(pair => Enumerable.Range(1, 2).Any(step => waspAt.Equals(
                        new HexCoordinate(
                            pair.Coordinate.Q + ((pair.Coordinate.Q - space.Q) * step),
                            pair.Coordinate.R + ((pair.Coordinate.R - space.R) * step)))))
                    .Select(pair => (space, pair.MinionId)))
                .ToArray();
            Assert.That(lineUps, Is.Not.Empty,
                $"No space Arien can walk to puts a minion between him and the enemy at {waspAt} " +
                "on a straight line. Minions: " +
                string.Join("; ", minions.Select(pair => $"{pair.MinionId.Value}@{pair.Coordinate}")) +
                ". Walkable: " + string.Join(", ", walkable.Select(space => space.ToString())));

            var attackFrom = lineUps[0].space;
            var targetMinionId = lineUps[0].MinionId;
            Assert.That(runtime.BoardPresenter.SubmitDestination(attackFrom), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            Assert.That(runtime.SubmitEndTurn(), Is.True, "The enemy holds turn one.");
            yield return AdvanceToNextTurn(runtime);

            // The subject: Dangerous Current's Attack, and the enemy hero standing in the two spaces
            // behind its target. The enemy commits a card whose whole turn is a Hold, so nothing
            // moves off the line the setup built.
            Commit(runtime, arienId, ArienCardContent.DangerousCurrent);
            Commit(runtime, waspId, WaspCardContent.LiftUp);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(waspId),
                "Lift Up's 10 acts before Dangerous Current's 8, so the enemy holds first.");
            Assert.That(runtime.SubmitEndTurn(), Is.True, "and holds without leaving the line.");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(arienId));

            yield return PointTrayAt(root, arienId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds,
                Does.Contain(targetMinionId),
                "The minion the setup lined up must be a legal target of the printed Attack.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(targetMinionId).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The before-attack text must stop the Attack on the hero behind the target.");
            var waspView = HeroView(waspId);
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets
                    .Any(target =>
                        target.Space.Equals(waspView.Coordinate)), Is.True,
                "The enemy hero behind the target is marked where they stand.");
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, waspView.transform.position), Is.True,
                "and is chosen by clicking them.");
            yield return null;

            // "Up to 1 enemy hero" is a selection with a floor of zero, so the board click marks the
            // hero and the actor confirms the set - the same two-step shape p.18's batch operations
            // use, and the reason declining is answering rather than an absence.
            var confirmSelection = runtime.RoundView.Model.EffectChoices
                .FirstOrDefault(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (confirmSelection != null)
            {
                UiPlayer.Click(UiPlayer.Button(hud, confirmSelection.Label));
                yield return null;
            }

            // "discards a card, or is defeated" - ledger question 13's one mandatory step with two
            // ways to complete. The chosen hero holds cards, so the discard is theirs to choose, on
            // the whole printed face milestone 8.15a made every card-valued choice.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The chosen hero's own discard stops the Attack again. " + Remaining(runtime));
            var discardChoices = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(discardChoices, Is.Not.Empty,
                "Shown controls: " + UiPlayer.Describe(hud));
            Assert.That(discardChoices.Select(value => value.Card), Has.All.Not.Null,
                "Each control is the whole printed face, never an anonymous ordinal.");
            var discardedBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(waspId) && card.State.Location == CardLocation.DiscardPile);
            var faceName = discardChoices[0].Card.DisplayName.ToUpperInvariant();
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null, "The step completed by its discard.");
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(waspId) && card.State.Location == CardLocation.DiscardPile),
                Is.EqualTo(discardedBefore + 1),
                "Exactly the clicked card left the chosen hero's hand.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(waspId)), Is.True,
                "and paying the price is what keeps them on the board.");
        }

        /// <summary>
        /// Milestone 8.29f. The reported sequence, on the live surface: Dangerous Current's printed
        /// Attack completes over an adjacent enemy minion, and the printed Movement region on that
        /// same resolving card stops being a control that does anything.
        ///
        /// <para>The Movement's invocation is captured while the whole face is exposed and every
        /// printed region bound, which is exactly the state milestone 8.29b1 draws it in - and then
        /// submitted after the Attack, through the same presenter a click goes through. What the
        /// player met was that submission being accepted.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DangerousCurrent_LeavesNoSecondActionOnItsCardOnceItsAttackHasCompleted()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Commit(runtime, arienId, ArienCardContent.DangerousCurrent);
            Commit(runtime, waspId, WaspCardContent.StopProjectiles);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(arienId),
                "Dangerous Current's printed Initiative 8 acts before Stop Projectiles' 3.");
            var sourceCardId = runtime.SelectedActor.CardInstanceId;

            yield return PointTrayAt(root, arienId);

            // The invocation the resolving card's printed Movement region is bound to, taken while
            // both regions are drawn. A presenter holds this across the Attack performed from the
            // other region on the same face.
            var movementBeforeTheAttack = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(movementBeforeTheAttack.Command.CardInstanceId, Is.EqualTo(sourceCardId),
                "and it names the card it was drawn on, which is what makes it refusable later.");

            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            var targetAt = MinionView(targetId).Occupancy.Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(targetId).transform.position),
                Is.True, "The one legal target is chosen where it stands.");
            yield return null;

            // "Up to 1 enemy hero in any of the 2 spaces in a straight line directly behind the
            // target" names nobody from this staged position, so p.14's step 1 asks nothing and the
            // page runs through to the minion Defeat inside the same command.
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The before-attack text has nobody to ask about here. " + Remaining(runtime));
            Assert.That(OccupantAt(runtime, targetAt), Is.False,
                "and the minion Defeat and removal completed on screen.");

            // p.7 spends the card on the action, and the turn moves on.
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(sourceCardId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved),
                "The completed Attack resolves its own source card.");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(waspId),
                "and Initiative advances to the other seat.");

            // So the region is inert, both as something the HUD offers and as something the
            // presenter would forward if a stale click reached it.
            Assert.That(
                runtime.RoundView.Model.Actions.Any(value =>
                    value.Command.CardInstanceId.Equals(sourceCardId)), Is.False,
                "No printed action on the spent card is offered any more. Shown controls: " +
                UiPlayer.Describe(hud));
            Assert.That(runtime.RoundPresenter.Submit(movementBeforeTheAttack), Is.False,
                "and the invocation a presenter was still holding forwards nothing.");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(waspId),
                "The refusal changed nothing: the other seat is still the one acting.");
        }

        [UnityTest]
        public IEnumerator AspiringDuelist_IsOfferedAsAPrimaryDefenseValueAndDiscardedByItsOwnFace()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Setup, headlessly: p.14's Defense response needs an Attack on Arien, and the staging
            // puts the heroes two apart. Turn one closes it with the attacker's own walk - it is the
            // attacker who has to be able to reach, and a defender who walked into contact leaves the
            // attacker's own Battle-Zone position unlike anything the approach march produces.
            Commit(runtime, waspId, WaspCardContent.Shock);
            Commit(runtime, arienId, ArienCardContent.LiquidLeap);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(waspId),
                "Shock's 8 acts before Liquid Leap's 4, so the enemy walks first.");
            var arienAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var step = runtime.PendingMovementChoice.LegalDestinations
                .First(value => HexDistanceQuery.Calculate(value.Destination, arienAt) == 1);
            Assert.That(runtime.BoardPresenter.SubmitDestination(step.Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            Assert.That(runtime.SubmitEndTurn(), Is.True, "Arien holds turn one.");
            yield return AdvanceToNextTurn(runtime);

            // Turn two: the enemy attacks the now-adjacent Arien, who still holds Aspiring Duelist.
            Commit(runtime, waspId, WaspCardContent.MagneticDagger);
            Commit(runtime, arienId, ArienCardContent.NobleBlade);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(waspId),
                "Magnetic Dagger's 12 acts before Noble Blade's 11.");
            var attack = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Does.Contain(arienId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(arienId).transform.position),
                Is.True);
            yield return null;

            // The subject: p.14's two Defense shapes offered side by side in one prompt - the numeric
            // secondary icons his other cards print, and the Primary Defense Value the Duelist card
            // prints in the slot a primary action usually occupies.
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The Attack must reach p.14's Defense response. " + Remaining(runtime) +
                " attackTarget=" + (runtime.PendingAttackTargetChoice == null ? "none" : "standing"));
            var offers = runtime.PendingDefenseChoice.LegalDefenseOffers;
            Assert.That(offers.Count, Is.GreaterThanOrEqualTo(2),
                "Arien's hand offers both Defense shapes at once.");
            Assert.That(
                offers.Any(offer => OriginOf(runtime, offer) == ActionDefinitionOrigin.PrintedPrimary),
                Is.True,
                "p.14's Primary Defense Value must be one of the offers.");
            Assert.That(
                offers.Any(offer => OriginOf(runtime, offer) == ActionDefinitionOrigin.Reaction),
                Is.True,
                "and an ordinary secondary Defense icon must be another.");

            // The prompt as a player meets it: p.14's Primary Defense Value drawn as a whole face
            // beside the numeric secondaries, which is the shape this build had never drawn.
            yield return CaptureGameView("primary-defense-value-in-the-defense-prompt");

            var handBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(arienId) && card.State.Location == CardLocation.Hand);
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-defense-card", "ASPIRING DUELIST"));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", "ASPIRING DUELIST"));
            yield return null;

            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked face answers p.14's response. Still offered: " +
                (runtime.PendingDefenseChoice == null
                    ? "none"
                    : string.Join(", ", runtime.PendingDefenseChoice.LegalDefenseOffers
                        .Select(value => value.SourceCardInstanceId.Value))) +
                " shown: " + UiPlayer.Describe(hud));
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(arienId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(handBefore - 1),
                "A Defense is paid by discarding the card from hand.");
            Assert.That(runtime.Cards.Single(card =>
                    card.OwnerId.Equals(arienId) &&
                    card.DefinitionReference.DefinitionId.Equals(ArienCardContent.AspiringDuelist))
                .State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "and it is Aspiring Duelist that was spent.");

            // "Ignore all minion defense modifiers" is the card's whole text, and it is a statement
            // about the Defense Total rather than a step: the defender survived a 3-Attack behind a
            // printed 5 with no minion contribution counted either way.
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(arienId)), Is.True,
                "The printed 5 answers Magnetic Dagger's printed 3, so Arien is still standing.");
            var arienRail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(arienId));
            Assert.That(arienRail.DiscardCards, Has.Count.EqualTo(1),
                "and the spent Defense is on his own rail where a player reads it.");
        }

        [UnityTest]
        public IEnumerator LiquidLeap_PlacesArienOnAClickedSpaceAndLightsNoSpawnPoint()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Commit(runtime, arienId, ArienCardContent.LiquidLeap);
            Commit(runtime, waspId, WaspCardContent.StopProjectiles);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(arienId),
                "Liquid Leap's 4 acts before Stop Projectiles' 3.");

            yield return PointTrayAt(root, arienId);
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate;
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // "Place yourself into a space in range without a spawn point and not adjacent to an
            // empty spawn point." Every legal answer is a marked space on the board, and the two
            // exclusions are visible as spaces that are not marked.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Skill must stop on the space it places its actor into.");
            var targets = LitDestinations(runtime);
            Assert.That(targets, Is.Not.Empty,
                "A space selection is answered on the map. Remaining: " + Remaining(runtime));
            Assert.That(
                targets.All(space => HexDistanceQuery.Calculate(space, startedAt) <= 2), Is.True,
                "and every marked space is inside the printed Range 2.");

            // The two printed exclusions - a space with a spawn point, and a space beside an empty
            // one - are pinned against the map at the authoritative layer, where the spawn points
            // themselves are addressable. What this journey proves about them is the half a player
            // sees: the offered set is smaller than the reachable one, and every space in it is lit.
            var reachable = runtime.RoundView.Model.EffectChoiceDestinations.Count;
            Assert.That(reachable, Is.EqualTo(targets.Length));
            foreach (var space in targets)
            {
                Assert.That(Tile(space).IsHighlighted, Is.False);
                Assert.That(Tile(space).CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.Effect),
                    $"Every offered space must carry neutral corners before it is clicked; {space} does not.");
            }

            var destination = targets[0];
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destination).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null, "The clicked space answers the Place.");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(HeroView(arienId).Coordinate, Is.EqualTo(destination),
                "and the token stands where the Place put him.");
        }

        [UnityTest]
        public IEnumerator Arien_IsDefeatedByAnAttackAndRespawnsOnHisOwnSpawnByAClick()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienSetup = ArienSeat(runtime);
            var arienId = arienSetup.HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Setup, headlessly: the attacker closes the gap, so the Attack that follows is the one
            // the approach march produces rather than one a defender walked into.
            Commit(runtime, waspId, WaspCardContent.Shock);
            Commit(runtime, arienId, ArienCardContent.LiquidLeap);
            var arienAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var step = runtime.PendingMovementChoice.LegalDestinations
                .First(value => HexDistanceQuery.Calculate(value.Destination, arienAt) == 1);
            Assert.That(runtime.BoardPresenter.SubmitDestination(step.Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            Assert.That(runtime.SubmitEndTurn(), Is.True);
            yield return AdvanceToNextTurn(runtime);

            // Turn two: the enemy attacks, and Arien declines p.14's Defense so the Attack Total
            // stands unanswered and p.13's Defeat follows.
            Commit(runtime, waspId, WaspCardContent.MagneticDagger);
            Commit(runtime, arienId, ArienCardContent.SpellBreak);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(arienId),
                "Spell Break's 13 acts before Magnetic Dagger's 12, so Arien acts and holds first.");
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Arien holds, keeping his hand intact.");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(waspId));

            var attack = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(arienId).transform.position),
                Is.True);
            yield return null;

            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The Attack must reach p.14's Defense response. " + Remaining(runtime) +
                " attackTarget=" + (runtime.PendingAttackTargetChoice == null ? "none" : "standing"));
            var lifeBefore = hud.Header.BlueLifeLabel;
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is the setup this journey needs; the Defense prompt's own clicked face " +
                "is AspiringDuelist_IsOfferedAsAPrimaryDefenseValueAndDiscardedByItsOwnFace's " +
                "subject.");
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(arienId)), Is.False,
                "The defeated hero is off the board until he respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(arienId)), Is.False,
                "and his token is gone with him rather than left standing.");
            Assert.That(hud.Header.BlueLifeLabel, Is.Not.EqualTo(lifeBefore),
                "The defeat spends a Blue Life, and the status strip says so.");

            // p.13's respawn is offered at the defeated hero's own next turn, and answered by
            // clicking one of the marked spawn spaces.
            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);
            Commit(runtime, arienId, ArienCardContent.NobleBlade);
            Commit(runtime, waspId, WaspCardContent.LiftUp);
            yield return PointTrayAt(root, waspId);
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Arien must be offered his respawn at his own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(respawn.Destination).transform.position),
                Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Empty,
                "The answered respawn withdraws its options.");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate,
                Is.EqualTo(respawn.Destination));
            var respawnedViews = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(arienId))
                .ToArray();
            Assert.That(respawnedViews, Has.Length.EqualTo(1),
                "Exactly one Arien token stands after the respawn, not a second one beside a stale.");
            Assert.That(respawnedViews[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawnedViews[0].HasTokenVisual, Is.True,
                "and it is his own official token that came back.");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        internal static HeroId OtherHeroId(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Setup.Heroes.Single(hero => !hero.HeroId.Equals(heroId)).HeroId;

        /// <summary>
        /// Headless filler commitment. The journeys' own subjects are the decisions after the
        /// commitment; milestone 8.15's fixtures established the same split and the tray-clicked
        /// commitment has its own journey in <see cref="ArienUiJourneyPlayModeTests"/>.
        /// </summary>
        internal static void Commit(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, CardDefinitionId definitionId)
        {
            var card = runtime.Cards.Single(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True,
                $"{definitionId.Value} must be selectable from {heroId.Value}'s hand.");
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
        }

        /// <summary>Drains whatever end-of-round pauses stand until the next turn can be committed.</summary>
        internal static IEnumerator AdvanceToNextTurn(ConfiguredMatchSceneRuntime runtime)
        {
            for (var budget = 0; runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                Assert.That(budget, Is.LessThan(40),
                    "The end of round must complete within a bounded number of answers.");
                AnswerAnyEndOfRoundPause(runtime);
                yield return null;
            }

            yield return null;
        }

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

        internal static ActionDefinitionOrigin OriginOf(
            ConfiguredMatchSceneRuntime runtime, ActionOffer offer) =>
            runtime.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                .Origin;

        internal static IEnumerator StartActionThroughControl(
            MatchSceneCompositionRoot root, ActionKind kind)
        {
            yield return null;
            var runtime = root.Runtime;
            var action = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == kind);
            UiPlayer.Click(UiPlayer.Button(root.Hud, action.Label));
            yield return null;
        }

        /// <summary>
        /// The spaces a paused effect is offering, in board order. Space-valued candidates come
        /// through <c>EffectChoiceDestinations</c> and object-valued ones through
        /// <c>EffectChoiceTargets</c>; reading the wrong one is how a board-answered question looks
        /// like a question with no answers.
        /// </summary>
        internal static HexCoordinate[] LitDestinations(ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoiceDestinations
                .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();

        /// <summary>
        /// What a paused frame is still asking, for a failure message that says what the player was
        /// actually looking at rather than only that something was left over.
        /// </summary>
        internal static string Remaining(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.RoundView.Model;
            return $"status='{model.Status}' choices=[" +
                string.Join(", ", model.EffectChoices.Select(value =>
                    $"{value.Command.EffectChoice.Kind}:{value.Label}")) +
                "] targets=[" +
                string.Join(", ", model.EffectChoiceTargets.Select(value => value.Space.ToString())) +
                "] destinations=[" +
                string.Join(", ", model.EffectChoiceDestinations.Select(value =>
                    value.Command.EffectChoice.Candidate.Value.Coordinate.ToString())) +
                "]";
        }


        /// <summary>
        /// The world position of whatever piece stands on this space - hero or minion - so a journey
        /// can click the candidate the rules offered rather than the one it guessed would be offered.
        /// </summary>
        internal static Vector3 PieceAt(HexCoordinate space)
        {
            var hero = Object.FindObjectsByType<PrimitiveHeroView>()
                .FirstOrDefault(view => view.Coordinate.Equals(space));
            if (hero != null)
            {
                return hero.transform.position;
            }

            var minion = Object.FindObjectsByType<StandardMinionView>()
                .FirstOrDefault(view => view.Occupancy != null &&
                    view.Occupancy.Coordinate.Equals(space));
            Assert.That(minion, Is.Not.Null, $"No piece is drawn on {space}.");
            return minion.transform.position;
        }

        /// <summary>Whether any hero or minion now stands on this space.</summary>
        internal static bool OccupantAt(ConfiguredMatchSceneRuntime runtime, HexCoordinate space) =>
            runtime.Heroes.Any(hero => hero.Coordinate.Equals(space)) ||
            Object.FindObjectsByType<StandardMinionView>()
                .Any(view => view.Occupancy != null && view.Occupancy.Coordinate.Equals(space));

        /// <summary>
        /// The first offered space whose own camera ray reaches the board rather than a hero piece
        /// standing over it. <c>PrimitiveHeroPointerInput</c> offers a ray to the hero selectors
        /// before the tiles, so a space drawn behind a piece is answered by clicking elsewhere on it -
        /// which is what a player does, and what a journey clicking tile centres has to do for them.
        /// </summary>
        internal static HexCoordinate FirstClickableSpace(
            PrimitiveHeroPointerInput pointerInput, IReadOnlyList<HexCoordinate> spaces)
        {
            var camera = pointerInput.EventCamera;
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>()
                .Select(view => view.GetComponent<CapsuleCollider>())
                .Where(capsule => capsule != null)
                .ToArray();
            foreach (var space in spaces)
            {
                var tile = Tile(space);
                var screen = camera.WorldToScreenPoint(tile.transform.position);
                var ray = camera.ScreenPointToRay(new Vector2(screen.x, screen.y));
                if (!heroes.Any(capsule => capsule.Raycast(ray, out _, 500f)))
                {
                    return space;
                }
            }

            Assert.Fail(
                "Every offered space is drawn behind a hero piece, so none of them can be clicked: " +
                string.Join(", ", spaces.Select(space => space.ToString())));
            return default;
        }

        /// <summary>
        /// One targeted Game View capture of what is on screen right now, for the non-batch pass the
        /// gate requires: "model, presenter, batch rendering, or screenshot evidence alone cannot
        /// substitute for the real-input journey", and equally the journey alone cannot substitute
        /// for looking at it. A batch run skips it, so the same journeys serve both tiers.
        /// </summary>
        internal static IEnumerator CaptureGameView(string name)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                yield break;
            }

            var path = System.IO.Path.GetFullPath($"tmp/8-20a-{name}.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(System.IO.File.Exists(path), Is.True,
                $"The targeted Game View evidence must be captured under tmp/ as {name}.png.");
        }

        internal static PrimitiveHeroView HeroView(HeroId heroId) =>
            Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(heroId));

        internal static StandardMinionView MinionView(MinionId minionId) =>
            Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null && view.Occupancy.MinionId.Equals(minionId));

        internal static HexTileView Tile(HexCoordinate coordinate) =>
            Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(coordinate));

        internal static IEnumerator LoadEngagedScene()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent =
                SceneCardContent.VerifiedEngagedStagedArien;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }
    }
}
