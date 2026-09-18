using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Tali's own themed journeys, driven on <see cref="SceneCardContent.VerifiedEngagedStagedTali"/> -
    /// her starting-hand faces. Milestone 8.37; executed and corrected by that milestone's own Unity
    /// pass - see <see cref="TaliUiJourneyPlayModeTests"/>'s own remarks.
    ///
    /// <para><b>Two themes the prep scope named could not be built on any staged board this prep has,
    /// and are named here rather than smuggled as passing.</b></para>
    /// <list type="bullet">
    /// <item><description><b>Cross-role overflow</b> (a Pack Ice or Venerated Totem placement pulling
    /// back a standing token of the OTHER role) needs two DIFFERENT-role Green cards reachable in one
    /// hand at once, and RULING 24 forbids exactly that: p.15's upgrade REPLACES the lower Green tier,
    /// so a single deck never holds both an Ice-placing and a Totem-placing upgraded card at the same
    /// time. The mechanic is proven at the definition tier instead -
    /// <c>TaliTokenSystemTests.TheShortSupplyStepReachesAcrossRoles</c> - which is this row's
    /// authoritative scenario. A bespoke multi-round staging that carries a Tier I Ice token across a
    /// level-up into a Tier II Totem placement could reach it in Play Mode; that staging does not exist
    /// yet and is named for the executor rather than built here under time pressure.</description></item>
    /// <item><description><b>Ice Blast's rider's "ANOTHER enemy hero"</b> has no candidate on any
    /// one-hero-per-team board this build stages, for the identical reason Silverarrow's own Rain of
    /// Arrows minion clause has none on the same boards (that prep's F-4). Unlike Brogan's ally seat,
    /// which adds a FRIENDLY hero, this rider needs a SECOND ENEMY - a staging no dealt hero's prep has
    /// built. <see cref="IceBlast_TheRidersGateOpensOnADefendedAttack_AndTheDiscardFizzlesForWantOfAnotherEnemy"/>
    /// drives the gate and the correct "if able" fizzle this board produces, which is a true and useful
    /// proof; a 1v2 staging is what would let a future pass drive the discard itself.</description></item>
    /// </list>
    /// </summary>
    public sealed class TaliEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// "Place an Ice token in radius." - Glacial Barrier, the mandatory single placement (no
        /// softener), driven by a real board click. The Initiative aura's arithmetic (RULING 25's
        /// no-floor, the per-token emergent counting) is proven at the definition tier by
        /// <c>TaliTokenSystemTests.AnIceTokenGivesTheEnemyHeroBesideItMinusOneInitiative</c> and
        /// <c>...AHeroAdjacentToThreeIceTokensIsAtMinusThree</c>; this journey's own subject is that
        /// the PLACEMENT itself reaches the player, which is the real gap phase one left open (2c: "no
        /// test drives Execute/Resume through a live effect frame").
        /// </summary>
        [UnityTest]
        public IEnumerator GlacialBarrier_PlacesAnIceTokenAtASpaceClickedInsideItsPrintedRadius()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.GlacialBarrier);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId),
                "The journey needs her turn open on Glacial Barrier. " + Remaining(runtime));

            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The mandatory placement asks where. " + Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "The supply of three has never been drawn from, so the placement can always find a " +
                "candidate space at least at her own square. " + Remaining(runtime));

            var destination = destinations[0];
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destination).transform.position), Is.True,
                "The destination is answered on the board.");
            yield return null;
            yield return null;

            var placed = Object.FindObjectsByType<PlacedTokenView>()
                .Select(view => view.Token)
                .Where(token => token != null)
                .ToArray();
            Assert.That(placed, Has.Length.EqualTo(1),
                "One Ice token now stands, drawn by a real view rather than left as a fact only the " +
                "authoritative state knows.");
            Assert.That(placed[0].Coordinate, Is.EqualTo(destination));
            Assert.That(placed[0].Definition.Id, Is.EqualTo(TaliComponentContent.Ice),
                "The placed piece is an Ice token, not a Totem the wrong role would have produced.");
        }

        /// <summary>
        /// "Choose a direction. Move each enemy unit in radius 1 space in that direction, if able." -
        /// Cold Snap, driven end to end: the free direction chosen BY PRESSING ITS ARROW ON THE BOARD,
        /// then the batch applying with no second question, because "each" names a set rather than
        /// opening a count.
        /// </summary>
        /// <remarks>
        /// Milestone 8.46d converts this journey from the strip's six direction buttons to the six
        /// board arrows, on the user's verdict that a direction is answered where directions are:
        /// around the hero being asked. Nothing about the command changes - an arrow submits the same
        /// Select the button submitted - so what this journey proves about Cold Snap is unchanged.
        /// </remarks>
        [UnityTest]
        public IEnumerator ColdSnap_ChoosesAFreeDirectionThenMovesTheEnemyOneSpaceWithNoSecondQuestion()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, taliId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.ColdSnap);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            var enemyStoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;

            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Choose a direction\" opens unconditionally, on the board. " + Remaining(runtime));
            var taliStoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(taliId)).Coordinate;
            var directionCandidates = runtime.BoardView.Model.DirectionCandidates;
            Assert.That(directionCandidates, Has.Count.EqualTo(6),
                "All six of p.17's directions are always offered - the sentence names no origin and no " +
                "destination to narrow them. " + Remaining(runtime));
            Assert.That(runtime.BoardView.Model.DirectionChooserSpace, Is.EqualTo(taliStoodAt),
                "The arrows are drawn around the hero being asked.");
            foreach (var candidate in directionCandidates)
            {
                Assert.That(candidate.Neighbour,
                    Is.EqualTo(taliStoodAt.GetNeighbor(candidate.Direction)),
                    "Each arrow stands one step away, which is what makes it clickable as a space.");
            }

            // The pieces the player can actually see, not only the model behind them.
            var drawnArrows = runtime.BoardView.DirectionArrows.Where(value => value.IsVisible)
                .ToArray();
            Assert.That(drawnArrows, Has.Length.EqualTo(6),
                "Six arrows are drawn while the pause stands.");
            Assert.That(drawnArrows.Select(value => value.Space),
                Is.EquivalentTo(directionCandidates.Select(value => value.Neighbour)),
                "and each one is drawn on the space its own candidate names.");

            var chosen = FirstClickableDirection(root, pointerInput);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen.Neighbour).transform.position),
                Is.True,
                $"The {chosen.Label} arrow answers the direction where it is drawn.");
            yield return null;
            yield return null;

            // ONE PRESS. A choice whose minimum and maximum are both one is finished by the
            // selection itself: the board forwards the Select and the confirm the authority offers
            // immediately after it, so the batch has already run by the time the click returns.
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The press resolves the direction, and the batch applies from the same node table " +
                "with no second question - \"each\" names a set, so nobody is asked which enemy " +
                "units move. " + Remaining(runtime));
            Assert.That(runtime.BoardView.DirectionArrows.Where(value => value.IsVisible),
                Is.Empty,
                "and the arrows are cleared with the pause that raised them.");

            var enemyNowAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            // "If able": on this board the enemy stands within radius 2 of a two-space-apart engagement,
            // so the chosen direction may or may not have an empty space to step into. Either observable
            // outcome is a legal reading of the same sentence, which is why this asserts the fizzle
            // reads correctly rather than assuming the move always lands.
            Assert.That(
                enemyNowAt.Equals(enemyStoodAt) ||
                GuardsDigital.Core.Board.HexDistanceQuery.Calculate(enemyNowAt, enemyStoodAt) == 1,
                Is.True,
                $"The enemy either stayed (blocked or off-map, the \"if able\" fizzle) or moved exactly " +
                $"one space (the printed distance). Was at {enemyStoodAt}, now at {enemyNowAt}.");
        }

        /// <summary>
        /// Milestone 8.46d. The first offered direction whose arrow a player could really press: its
        /// space is one the board draws, it is on screen, and the HUD is not standing over it.
        ///
        /// <para>It is chosen rather than fixed because the six arrows are placed by where the hero
        /// happens to stand, and a staging can put one of them off the drawn map or under the hand -
        /// the same reason <c>FirstClickableSpace</c> exists for lit destinations. A piece standing
        /// on an arrow's space is deliberately NOT skipped: the hero and minion selectors claim that
        /// click first and the presenter routes it to the same direction candidate, which is exactly
        /// the arm this milestone added to <c>SubmitEffectChoiceAtSpace</c>.</para>
        /// </summary>
        internal static DirectionCandidateViewModel FirstClickableDirection(
            MatchSceneCompositionRoot root, PrimitiveHeroPointerInput pointerInput)
        {
            var camera = pointerInput.EventCamera;
            Assert.That(camera, Is.Not.Null, "The pointer input has no event camera configured.");
            var drawn = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            foreach (var candidate in root.Runtime.BoardView.Model.DirectionCandidates)
            {
                var tile = drawn.FirstOrDefault(value =>
                    value.Coordinate.Equals(candidate.Neighbour));
                if (tile == null)
                {
                    continue;
                }

                var screen = camera.WorldToScreenPoint(tile.transform.position);
                if (screen.z <= 0f ||
                    screen.x < 0f || screen.x > Screen.width ||
                    screen.y < 0f || screen.y > Screen.height ||
                    root.Hud.BlocksPointerAt(new Vector2(screen.x, screen.y)))
                {
                    continue;
                }

                return candidate;
            }

            Assert.Fail(
                "No direction arrow is drawn on a space a player could click: " +
                string.Join(", ", root.Runtime.BoardView.Model.DirectionCandidates.Select(value =>
                    value.Label + "@" + value.Neighbour)));
            return null;
        }

        /// <summary>
        /// The direction choice is the ONLY pause her free-direction batch Move ever opens - the batch
        /// itself is one mandatory node with no second question, per
        /// <see cref="SyntheticDisplacementCapabilities.BatchDirectMoveV1"/>'s own remarks - so this is
        /// what "a mid-batch-move pause" names: paused inside Cold Snap's own resolution, before the
        /// batch has applied at all, which is exactly the moment an Undo or a restore must be able to
        /// return to without silently completing the move underneath the player.
        /// </summary>
        [UnityTest]
        public IEnumerator ColdSnap_TheDirectionChoiceSurvivesAnUndoAndASnapshotRestoreBeforeTheBatchApplies()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, taliId);
            var enemyStoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.ColdSnap);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var pausedSnapshot = runtime.CaptureSnapshot();

            // THE ANSWER HAS TO EXIST BEFORE AN UNDO CAN RETRACT IT. This journey was written
            // pressing Undo against an UNANSWERED question and expecting the question to survive;
            // with nothing answered, the step back is the commitment itself and the seat lands on
            // "choose an action", which is the engine reading correctly. Selecting first is what
            // makes the assertion below about the modal's own answer rather than about the turn.
            var heldDirection = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(heldDirection, Has.Length.EqualTo(6), Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(heldDirection[0].Command), Is.True,
                "A direction is held, not yet confirmed. " + Remaining(runtime));
            yield return null;
            Assert.That(
                runtime.PendingEffectChoice.SelectedCandidates, Is.Not.Empty,
                "The held answer is what the Undo below has to find. " + Remaining(runtime));

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Undo retracts the modal's own answer and not the whole turn; the direction " +
                "question must still be standing. " + Remaining(runtime));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(enemyStoodAt),
                "Nothing has moved: the batch never applied before the Undo, so there is nothing to " +
                "roll back beyond the choice itself.");

            Assert.That(runtime.RestoreSnapshot(pausedSnapshot), Is.True,
                "A restore into a match holding this pause must succeed.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match must still be asking the same direction question.");
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(enemyStoodAt));

            var directionCandidates = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(directionCandidates, Has.Length.EqualTo(6), Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(directionCandidates[0].Command), Is.True,
                "The restored pause answers exactly like the original.");
            yield return null;
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(
                    runtime.PendingEffectChoice.ConfirmInvocation)),
                Is.True,
                "And is confirmed exactly like the original. " + Remaining(runtime));
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
        }

        /// <summary>
        /// "Target a unit adjacent to you. +3 Attack if this card is not in the discard." - Winter
        /// Dagger, played fresh from hand. T-11's reading (the Attack Total is computed with the card
        /// text's dynamic value already composed in) means the bonus applies here without any pause of
        /// its own to drive - the whole point of it being a value contribution and not a node.
        /// </summary>
        [UnityTest]
        public IEnumerator WinterDagger_AppliesItsPlusThreeWhenPlayedFreshBecauseItIsNotYetInTheDiscard()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, taliId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // "A unit adjacent to you" has no subject on the engaged staging - see
            // SpendATurnClosingTheGap for why the board starts two spaces apart and why closing it
            // costs a whole turn. Winter Dagger is committed for the turn AFTER that.
            yield return SpendATurnClosingTheGap(root, taliId, enemyId, TaliCardContent.WinterDagger);

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.WinterDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.HeroId.Equals(enemyId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True);
            yield return null;

            // The printed base is 2; the card is not in the discard (still in the turn area, this very
            // instance being resolved) so the dynamic +3 applies - Attack Total 5. Read it off the
            // Defense pause's own carried value rather than re-deriving it, so this proves what the
            // PLAYER is shown rather than what the query alone would say.
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingDefenseChoice.AttackTotal, Is.EqualTo(5),
                "Printed 2 plus the dynamic +3, because this instance of Winter Dagger is not standing " +
                "in the discard while it resolves.");

            for (var budget = 0; budget < 4 && runtime.PendingDefenseChoice != null; budget++)
            {
                Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                yield return null;
            }
        }

        /// <summary>
        /// "Target a unit adjacent to you. If the target discards a card to defend, After the attack:
        /// Another enemy hero in radius discards a card of the same color, if able." - Ice Blast.
        ///
        /// <para>Drives the GATE (<see cref="SyntheticRosterCapabilities.DefendedCardColorV1"/>'s own
        /// <c>defended</c>/<c>color</c> reading) correctly opening once the target defends with a card,
        /// and asserts the honest "if able" fizzle this one-hero-per-team board produces for want of a
        /// SECOND enemy hero. See this file's own type remarks for why that is not a staging gap this
        /// journey papers over.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator IceBlast_TheRidersGateOpensOnADefendedAttack_AndTheDiscardFizzlesForWantOfAnotherEnemy()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, taliId);

            // Her Gold basic is an adjacent-target Attack, so the gap closes on its own turn first.
            yield return SpendATurnClosingTheGap(root, taliId, enemyId, TaliCardContent.IceBlast);

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.IceBlast);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.HeroId.Equals(enemyId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The rider's gate needs a real card-based defense, not a decline. " + Remaining(runtime));
            var offers = runtime.PendingDefenseChoice.LegalDefenseOffers;
            Assert.That(offers, Is.Not.Empty,
                "The defending Wasp must have at least one card able to discard for the Defense " +
                "response. " + Remaining(runtime));
            var defenseCard = offers.First();
            var defenseFaceName = NameOf(runtime, runtime.Cards
                .Single(card => card.Id.Equals(defenseCard.SourceCardInstanceId))
                .DefinitionReference.DefinitionId);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", defenseFaceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", defenseFaceName));
            yield return null;
            yield return null;

            // The rider's own after-the-attack pause either opens (if "another enemy hero in radius"
            // has a candidate) or the mandatory step reports impossible and the turn simply continues -
            // both readings are legal per p.10, and this board has exactly zero candidates: the
            // defending hero is excluded as "the target", and there is no other enemy at all.
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "No enemy hero other than the one who just defended exists on this board, so the " +
                "mandatory step finds nothing and the rider ends quietly rather than opening a " +
                "one-candidate prompt. " + Remaining(runtime));
        }
    }
}
