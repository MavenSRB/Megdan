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
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
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

            // MILESTONE 8.86. The direction is CHOSEN rather than taken first-come, and the choice is
            // what makes the assertion below a real one: this journey used to press whichever arrow a
            // player could reach and then accept EITHER outcome - "the enemy either stayed (the 'if
            // able' fizzle) or moved exactly one space" - which is a sentence that cannot fail. A
            // direction whose own step is blocked proves nothing about a batch Move; the one pressed
            // here is a direction the enemy hero can really be pushed along, read off the live board,
            // and that such a direction exists at all is asserted rather than hoped for.
            var chosen = FirstClickableDirection(root, pointerInput, candidate =>
                NothingStandsOn(enemyStoodAt.GetNeighbor(candidate.Direction)));
            var pushedTo = enemyStoodAt.GetNeighbor(chosen.Direction);
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
            Assert.That(enemyNowAt, Is.Not.EqualTo(enemyStoodAt),
                "THE ASSERTION THIS JOURNEY WAS MISSING: \"Move each enemy unit in radius 1 space in " +
                $"that direction\" - the enemy hero really left {enemyStoodAt}. The direction pressed " +
                "was chosen because its own step was free, so the \"if able\" fizzle is not one of " +
                "the readings this board admits. " + Remaining(runtime));
            Assert.That(enemyNowAt, Is.EqualTo(pushedTo),
                $"and he stands one space along the {chosen.Label} arrow that was pressed - the " +
                "printed distance, in the printed direction, and no other space.");
            Assert.That(HeroView(enemyId).Coordinate, Is.EqualTo(enemyNowAt),
                "with his token drawn where he now is rather than where he stood.");
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
            MatchSceneCompositionRoot root, PrimitiveHeroPointerInput pointerInput) =>
            FirstClickableDirection(root, pointerInput, _ => true);

        /// <summary>
        /// Milestone 8.86. The same reading, narrowed to the directions a journey's own printed
        /// sentence needs - the overload every batch-Move journey now uses, because a direction whose
        /// step is blocked lets the batch legally move nobody and a journey that pressed it could not
        /// tell the printed move from the "if able" fizzle.
        /// </summary>
        internal static DirectionCandidateViewModel FirstClickableDirection(
            MatchSceneCompositionRoot root, PrimitiveHeroPointerInput pointerInput,
            System.Func<DirectionCandidateViewModel, bool> admits)
        {
            var camera = pointerInput.EventCamera;
            Assert.That(camera, Is.Not.Null, "The pointer input has no event camera configured.");
            var drawn = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            foreach (var candidate in root.Runtime.BoardView.Model.DirectionCandidates)
            {
                if (!admits(candidate))
                {
                    continue;
                }

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
                "No direction arrow this journey admits is drawn on a space a player could click: " +
                string.Join(", ", root.Runtime.BoardView.Model.DirectionCandidates.Select(value =>
                    value.Label + "@" + value.Neighbour + (admits(value) ? "" : " (not admitted)"))));
            return null;
        }

        /// <summary>
        /// Milestone 8.86. Whether the board draws that space as PLAYABLE with nothing standing on it -
        /// Swift's own push journey's reading, which is what tells a direction a unit can really be
        /// moved along from one the batch would legally leave it standing in.
        /// </summary>
        internal static bool NothingStandsOn(GuardsDigital.Core.Board.HexCoordinate space) =>
            Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                .Any(tile => tile.Coordinate.Equals(space) && tile.IsPlayable) &&
            !Object.FindObjectsByType<StandardMinionView>(FindObjectsSortMode.None)
                .Any(view => view.Occupancy != null && view.Occupancy.Coordinate.Equals(space)) &&
            !Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                .Any(view => view.Coordinate.Equals(space));

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
            // Milestones 8.46b and 8.46d: a direction is a choice of exactly one, drawn as six arrows
            // on the board and answered in ONE press - the select carries its own confirm - so there
            // is no held, unconfirmed selection any more. What the Undo retracts is the whole answer,
            // the batch it applied included, and the question then stands again exactly as it stood.
            var arrows = runtime.BoardView.Model.DirectionCandidates;
            Assert.That(arrows, Has.Count.EqualTo(6), Remaining(runtime));
            // MILESTONE 8.86. The arrow pressed is the one that really moves somebody, not `arrows[0]`.
            // "Nothing stays moved" is only evidence about an Undo if something WAS moved: a direction
            // whose own step is blocked applies a batch that legally displaces nobody, and the
            // assertion below would then hold with the retraction doing no work at all.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var mover = FirstClickableDirection(root, pointerInput, candidate =>
                NothingStandsOn(enemyStoodAt.GetNeighbor(candidate.Direction)));
            Assert.That(
                runtime.SubmitProjectedCommand(mover.Command), Is.True,
                "A direction is answered in one press. " + Remaining(runtime));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "and the question closes on that press. " + Remaining(runtime));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(enemyStoodAt.GetNeighbor(mover.Direction)),
                "and the batch it carried really moved the enemy hero one space along the " +
                $"{mover.Label} arrow, which is what the Undo below then has to take back. " +
                Remaining(runtime));

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Undo retracts the modal's own answer and not the whole turn; the direction " +
                "question must be standing again. " + Remaining(runtime));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(enemyStoodAt),
                "Nothing stays moved: the batch the answer applied is retracted with the answer.");

            Assert.That(runtime.RestoreSnapshot(pausedSnapshot), Is.True,
                "A restore into a match holding this pause must succeed.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match must still be asking the same direction question.");
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(enemyStoodAt));

            var restoredArrows = runtime.BoardView.Model.DirectionCandidates;
            Assert.That(restoredArrows, Has.Count.EqualTo(6), Remaining(runtime));
            var restoredMover = restoredArrows.Single(candidate =>
                candidate.Direction == mover.Direction);
            Assert.That(
                runtime.SubmitProjectedCommand(restoredMover.Command), Is.True,
                "The restored pause answers exactly like the original, in one press.");
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "and closes on it exactly like the original. " + Remaining(runtime));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(enemyStoodAt.GetNeighbor(mover.Direction)),
                "and carries the same batch: the restored answer put the enemy hero on the same " +
                "space the live one did, which is what makes the restore a continuation rather than " +
                "a question drawn over a board that has stopped listening. " + Remaining(runtime));
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

            yield return null;
            // MILESTONE 8.86. The Attack's own outcome, which this journey used to leave unread: it
            // asserted the raised total the defender was shown and then declined out of the window
            // without ever asking what the Attack did. A declined Defense is a Defense Total of
            // nought against a total of 5, and p.14's step 6 is what that means.
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.False,
                "\"Target a unit adjacent to you\" answered with a decline is p.14's Defeat, so the " +
                "enemy hero is off the board. " + Remaining(runtime));
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                    .Any(view => view.Occupancy.HeroId.Equals(enemyId)),
                Is.False,
                "and their token left with them rather than standing where they were struck.");
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
            var spent = defenseCard.SourceCardInstanceId;
            var enemyHandBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", defenseFaceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", defenseFaceName));
            yield return null;
            yield return null;

            // MILESTONE 8.86. The GATE'S OWN PRECONDITION, read in state rather than assumed from the
            // click: "if the target discards a card to defend". A journey whose only remaining
            // assertion is that nothing further stands would hold just as well if the Defense had
            // never been paid at all - and then the fizzle it records would be about the wrong thing.
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(spent)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The face the defender pressed left their hand for their discard pile, which is what " +
                "\"discards a card to defend\" means and what opens the rider's gate at all. " +
                Remaining(runtime));
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(enemyHandBefore - 1),
                "and their hand is one card lighter for it.");
            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The pressed face answered p.14's response, so no Defense question is left standing " +
                "for the rider's gate to be read across. " + Remaining(runtime));

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
