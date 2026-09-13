using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.CutterUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.WhisperUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Cutter's Tier II and Tier III faces and her Ultimate, driven through the controls a player
    /// actually reaches. Milestone 8.39j, and the content half of the FOURTEENTH hero's player-path
    /// gate - her starting hand is in <see cref="CutterUiJourneyPlayModeTests"/> and the component
    /// half in <see cref="CutterEngagedUiJourneyPlayModeTests"/>.
    ///
    /// <para><b>Two shapes here have never been driven by any gate in this build.</b> The first is an
    /// enemy-chosen modal put to the enemy's OWN seat, which Whisper's Lesser Evil established and
    /// these two Blue faces are the second content to print. The second is a journey that ENDS THE
    /// MATCH inside itself: no dealt hero's gate has ever ended one through a card, because until
    /// milestone 8.39g no card could.</para>
    /// </summary>
    public sealed class CutterProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Evasive Shot: "Target a unit in range and in a straight line. After the attack: Move up to
        /// 2 spaces in the opposite direction."
        ///
        /// <para><b>WHICH unit she shoots decides whether the retreat exists at all</b>, and that is
        /// measured rather than assumed. Two units sit in the printed Range 2 on a straight line out
        /// of her space: the Red ranged minion at (-4,2), one away, and the Red melee minion at
        /// (-1,-1), two away on the other half of the same file. Shooting the far one points the
        /// retreat back up the file into the near one's space, so "up to 2" has nowhere to go and the
        /// rider silently applies nothing. Shooting the NEAR one points it the other way, where
        /// (-2,0) is free - so the offer is exactly one space and the sentence does something.</para>
        ///
        /// <para>That asymmetry is the journey's subject: an offer read off the space the attack
        /// captured, not off her own facing.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator EvasiveShotFiresDownAFile_ThenRetreatsInTheOppositeDirection()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedCutter);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var from = runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate;
            var shotAt = new HexCoordinate(-4, 2);
            var retreatTo = new HexCoordinate(-2, 0);

            yield return HerTurnOn(root, cutterId, CutterCardContent.EvasiveShot);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null,
                "Her ranged Red opens p.14's target question. " + Remaining(runtime));
            var offered = choice.LegalTargets
                .Select(target => runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(target.MinionId)).Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();
            Assert.That(offered, Is.EqualTo(new[]
                {
                    new HexCoordinate(-4, 2), new HexCoordinate(-1, -1)
                }.OrderBy(value => value.Q).ThenBy(value => value.R).ToArray()).AsCollection,
                "\"in range AND in a straight line\" admits exactly the two units on a file within " +
                "the printed Range 2, and neither of the two off it. Offer was: " +
                string.Join(", ", offered.Select(value => value.ToString())));

            var near = runtime.ReadModel.Minions.Single(minion => minion.Coordinate.Equals(shotAt));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(near.MinionId).transform.position),
                Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            // "After the attack" - p.14's own second moment, and the pause is the shape the row binds.
            var retreat = runtime.PendingEffectChoice;
            Assert.That(retreat, Is.Not.Null,
                "The rider stops the page after the attack has been applied. " + Remaining(runtime));
            Assert.That(retreat.NodeId.Value, Is.EqualTo("retreat-line"), Remaining(runtime));

            // **MEASURED, AND RECORDED RATHER THAN ASSERTED AWAY.** The printed "up to" draws NO
            // control of its own: `choose-line-destination@1` offers 1..N spaces and declares a floor
            // of one, so the pause stands with neither a confirm nor a decline and the whole answer is
            // a board click. That is what this milestone's derivation refinement now reads, and
            // Silverarrow's own nought-distance journey says the same thing in as many words - "the
            // zero-distance answer is the occupied origin on the board, not a decline button".
            //
            // It is ALSO a gap against this face's own composition, which states in its remarks that
            // the `UpTo` policy is what turns "an offer she declines" into a step not taken: on this
            // board the offer holds one space and she cannot decline it. Raised in the completion
            // evidence; the same is true of Misa's five Blue Skills and Silverarrow's line faces, so
            // it is a standing question about the handler's floor rather than anything Cutter
            // introduced.
            Assert.That(retreat.MinimumSelections, Is.EqualTo(1), Remaining(runtime));
            Assert.That(retreat.DeclineInvocation, Is.Null,
                "Recording the measured state: the printed \"up to\" opens no decline of its own. " +
                "If this ever becomes non-null the handler has lowered its floor and this assertion " +
                "is what will say so. " + Remaining(runtime));

            var landings = OfferedSpaces(runtime);
            Assert.That(landings, Is.EqualTo(new[] { retreatTo }).AsCollection,
                "Every offered space lies on the heading AWAY from the space the attack captured, " +
                "and the second one along it is occupied. Offer was: " +
                string.Join(", ", landings.Select(value => value.ToString())));
            Assert.That(
                StraightLineQuery.AreInStraightLine(from, landings[0]) &&
                HexDistanceQuery.Calculate(shotAt, landings[0]) >
                    HexDistanceQuery.Calculate(shotAt, from),
                Is.True,
                "and the retreat moves her further from what she shot, which is what \"the opposite " +
                "direction\" means.");

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(retreatTo).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate,
                Is.EqualTo(retreatTo),
                "She backed away down the file she fired along. " + Remaining(runtime));
        }

        /// <summary>
        /// X Marks the Spot: "An enemy hero in radius chooses one — • You place that hero in a space
        /// in radius. • You gain 2 coins."
        ///
        /// <para><b>The modal is NOT hers, and that single assertion is why this journey exists.</b>
        /// She picks WHICH enemy hero; that hero picks the BULLET; and she then carries out whichever
        /// one they picked. p.10 permits exactly that - "You make all the choices in your card's text,
        /// unless the card text instructs the other hero to make a choice" - and Whisper's Lesser Evil
        /// is the seam this card reaches rather than a new one.</para>
        ///
        /// <para><b>The enemy takes the PLACE arm here</b>, which is the arm that draws a board space,
        /// and A Fistful of Coins' journey below takes the other. Between them both printed bullets of
        /// this composition are driven by real input, rather than one bullet twice.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator XMarksTheSpotPutsItsModalToTheEnemySeat_WhoChoosesToBeMoved()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedCutter);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, cutterId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var her = runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate;

            yield return HerTurnOn(root, cutterId, CutterCardContent.XMarksTheSpot);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            // HER OWN QUESTION FIRST: "an enemy hero in radius" is her selection at the printed
            // Radius 3, and it is hers because the sentence names the hero it is about rather than
            // handing anybody a choice yet.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, cutterId), Is.True,
                "She picks WHICH enemy hero; the modal beneath it is the only part she does not " +
                "own. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The enemy hero is named by clicking the piece where they stand.");
            yield return null;
            yield return null;

            // THE MODAL, AND IT IS NOT HERS.
            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null,
                "\"chooses one\" opens once she has named whom it is put to. " + Remaining(runtime));
            Assert.That(modal.NodeId.Value, Is.EqualTo("chosen-modal"), Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "The pending choice's chooser is what SeatProjection.OwnerOfStandingDecision reads, " +
                "so it is the field that decides which seat is shown these bullets. " +
                Remaining(runtime));
            Assert.That(modal.ChooserId, Is.Not.EqualTo(cutterId),
                "and a chooser that fell back to the actor would silently hand the opponent's " +
                "choice to the card's own controller - which from her side of the table would look " +
                "like a working prompt. " + Remaining(runtime));
            Assert.That(modal.Candidates.Count, Is.EqualTo(2),
                "Both printed bullets reach the enemy seat. " + Remaining(runtime));

            // The enemy seat answers through its own projected commands, exactly as Whisper's
            // opponent-chosen modal is answered: selecting is not taking, so the confirm follows.
            var place = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                value.Command.EffectChoice.Candidate.OptionNodeId.Value == "chosen-place-arm");
            Assert.That(runtime.SubmitProjectedCommand(place.Command), Is.True,
                "The bullet is the enemy seat's to take. " + Remaining(runtime));
            yield return null;
            yield return null;
            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(confirm, Is.Not.Null,
                "The electing seat closes its own selection. " + Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(confirm.Command), Is.True);
            yield return null;
            yield return null;

            // AND THE PLACE COMES BACK TO HER. "YOU place that hero" - the enemy picked which thing
            // happens to them and she does it, which is the half a projection could get wrong in the
            // other direction.
            var placement = runtime.PendingEffectChoice;
            Assert.That(placement, Is.Not.Null,
                "The taken bullet asks where he goes. " + Remaining(runtime));
            Assert.That(placement.NodeId.Value, Is.EqualTo("chosen-place"), Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, cutterId), Is.True,
                "and the decision came BACK to her. " + Remaining(runtime));

            var spaces = OfferedSpaces(runtime);
            Assert.That(spaces, Is.Not.Empty, Remaining(runtime));
            foreach (var space in spaces)
            {
                Assert.That(HexDistanceQuery.Calculate(her, space), Is.InRange(1, 3),
                    "\"a space in radius\" is measured from HER, which is what the printed \"You " +
                    "place\" says. Offer was: " +
                    string.Join(", ", spaces.Select(value => value.ToString())));
            }

            var destination = FirstClickableSpace(pointerInput, spaces);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destination).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(destination),
                "The hero who chose to be moved is standing where she put him. " + Remaining(runtime));
        }

        /// <summary>
        /// Outmaneuver: "Swap with an enemy minion in radius; you may move that minion up to 2 spaces."
        ///
        /// <para>Misa's <c>synthetic-swap-actor@1</c> reached from a printed Radius, and the second
        /// sentence is an <c>up to</c> direct Move - which expresses its distance inside the
        /// destination selection itself rather than opening a separate confirm, the distinction
        /// milestone 8.25e5f1 made observable. So the row declares one board-unit click and one
        /// board-space click and no decline at all, and this journey drives exactly that pair.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator OutmaneuverSwapsHerWithAnEnemyMinion_ThenWalksThatMinionAway()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedCutter);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, cutterId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var her = runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate;
            var enemySide = TeamSideOf(runtime, enemyId);

            yield return HerTurnOn(root, cutterId, CutterCardContent.Outmaneuver);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null,
                "The swap asks which minion. " + Remaining(runtime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("swap-subject"), Remaining(runtime));
            var offered = subject.Candidates
                .Select(candidate => runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(candidate.Value.MinionId)))
                .ToArray();
            Assert.That(offered, Is.Not.Empty, Remaining(runtime));
            foreach (var minion in offered)
            {
                Assert.That(minion.TeamSide, Is.EqualTo(enemySide),
                    "\"an ENEMY minion\" offers only the other side's miniatures.");
                Assert.That(HexDistanceQuery.Calculate(her, minion.Coordinate), Is.InRange(1, 3),
                    "and only those inside the printed Radius 3.");
            }

            var chosen = offered.OrderBy(minion =>
                HexDistanceQuery.Calculate(her, minion.Coordinate)).First();
            var stoodAt = chosen.Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(chosen.MinionId).transform.position),
                Is.True,
                "The minion is named by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate,
                Is.EqualTo(stoodAt),
                "She is standing where the minion was. " + Remaining(runtime));
            Assert.That(
                runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(chosen.MinionId)).Coordinate,
                Is.EqualTo(her),
                "and the minion is standing where she was, which is what a swap is.");

            var walk = runtime.PendingEffectChoice;
            Assert.That(walk, Is.Not.Null,
                "\"you may move that minion up to 2 spaces\" opens after the swap. " +
                Remaining(runtime));
            Assert.That(walk.NodeId.Value, Is.EqualTo("swap-move"), Remaining(runtime));
            var destinations = OfferedSpaces(runtime);
            Assert.That(destinations, Is.Not.Empty, Remaining(runtime));
            foreach (var destination in destinations)
            {
                Assert.That(HexDistanceQuery.Calculate(her, destination), Is.InRange(0, 2),
                    "Every destination is within the printed two spaces of where that minion now " +
                    "stands. Offer was: " +
                    string.Join(", ", destinations.Select(value => value.ToString())));
            }

            var step = FirstClickableSpace(
                pointerInput,
                destinations.Where(value => !value.Equals(her)).ToArray());
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(step).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(
                runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(chosen.MinionId)).Coordinate,
                Is.EqualTo(step),
                "The minion walked where she sent it. " + Remaining(runtime));
        }

        /// <summary>
        /// Broadside: "An enemy hero adjacent to another enemy unit and not adjacent to you discards a
        /// card, if able. May repeat once on a different target."
        ///
        /// <para><b>The repeat is a real control and this journey DECLINES it, which is the printed
        /// answer on a board holding one enemy hero.</b> "On a different target" has no second subject
        /// here, so taking the repeat would reach an offer the exclusion has emptied - and what the
        /// clause asks a player is whether to repeat, which declining answers.</para>
        ///
        /// <para><b>THE PRE-FIX BEHAVIOUR WAS MEASURED AND IT WAS WRONG.</b> Driven at milestone 8.39j
        /// before anything was changed, confirming the repeat re-offered the hero the first instance
        /// had just taken and the enemy lost TWO cards to a clause that prints "different". The cause
        /// was in <c>RepeatedActionQuery.TryCaptureBoundTarget</c>, which recorded the excluded piece
        /// only from a COORDINATE outcome fact; this card's subject node publishes the chosen hero and
        /// nothing else, so nothing was ever captured and the exclusion below it silently narrowed
        /// nothing. Fixed at this milestone with the space still preferred, so every bound selection
        /// written before it captures byte-identically.</para>
        ///
        /// <para><b>What is NOT fixed here, and is recorded rather than papered over:</b> a Skill's own
        /// printed repeat is offered without asking whether its instance could do anything - the
        /// feasibility gate milestone 8.25e added is deliberately null for that family - so a player on
        /// this board may confirm a repeat that then reaches nobody. That is a wider rule than this
        /// milestone's subject and it is raised in the completion evidence.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BroadsideTakesACard_AndOffersItsRepeatOnADifferentTarget()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedTierThreeEngagedStagedCutter,
                enemyAt: new HexCoordinate(-1, 1));
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, cutterId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return HerTurnOn(root, cutterId, CutterCardContent.Broadside);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            var handBefore = HandCountOf(runtime, enemyId);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("bombard-subject"),
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId));
            yield return ClickACardFace(root);
            Assert.That(HandCountOf(runtime, enemyId), Is.EqualTo(handBefore - 1), Remaining(runtime));

            // p.10's repeat, answered on its own control.
            var repeat = runtime.PendingEffectChoice;
            Assert.That(repeat, Is.Not.Null,
                "Only Broadside repeats, and the clause is an offer. " + Remaining(runtime));
            Assert.That(repeat.NodeId.Value, Is.EqualTo("bombard-repeat"), Remaining(runtime));
            Assert.That(repeat.ChooserId, Is.EqualTo(cutterId),
                "p.10 gives an unassigned decision to the card's controller, and \"may repeat once\" " +
                "assigns none. " + Remaining(runtime));
            Assert.That(repeat.ConfirmInvocation, Is.Not.Null,
                "The repeat can be taken. " + Remaining(runtime));
            Assert.That(repeat.DeclineInvocation, Is.Not.Null,
                "and refused, which is what makes \"may\" a decision rather than a step. " +
                Remaining(runtime));

            var decline = DeclineControl(runtime);
            Assert.That(decline, Is.Not.Null, Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Refusing the repeat finishes the card. " + Remaining(runtime));
            Assert.That(HandCountOf(runtime, enemyId), Is.EqualTo(handBefore - 1),
                "and exactly ONE card left the one hero the printed sentence could reach. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Legend of the Skies: "The first time each turn after you perform a primary action, you may
        /// perform the primary action of a card in the previous turn slot."
        ///
        /// <para>Milestone 8.39f1's sixth <c>UltimateTriggerCondition</c> and milestone 8.39f's
        /// previous-turn-slot pool. p.16 stands the unlocked Ultimate faceup in the hero's area, so
        /// nothing is committed to reach it - performing a primary is what offers the grant.</para>
        ///
        /// <para><b>It takes two rounds and neither is filler.</b> The pool is HER OWN previous turn
        /// slot - CU-Q3, closed owner-scoped by p.6 step 6, "the current Turn slot of your hero
        /// dashboard" - so a first round has to put a card in one. Round one resolves Grappling Bolt
        /// against the obstacle ALREADY adjacent to her, which moves nobody and leaves the board the
        /// second round measures. Round two resolves Walk the Plank's minion bullet, and the grant
        /// then performs Grappling Bolt's primary a second time - this time against terrain three
        /// away, which walks her two spaces and is how the journey can tell the borrowed primary
        /// really ran.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LegendOfTheSkiesBorrowsThePreviousTurnSlotsPrimary_AndPerformsItAgain()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedCutter);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // UltimateRailVisibility: p.16 stands the unlocked Ultimate OUT of the deck and where a
            // player can see it, which is what makes it permanent and reachable without a commitment.
            var ultimate = runtime.Cards.Single(card =>
                card.OwnerId.Equals(cutterId) &&
                card.DefinitionReference.DefinitionId.Equals(CutterCardContent.LegendOfTheSkies));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea),
                "Her unlocked Ultimate stands in her own area rather than in the deck or the hand.");
            var rail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(cutterId));
            Assert.That(rail.UltimateCard, Is.Not.Null,
                "and it is DRAWN on her rail, which is the reading half of p.16.");
            Assert.That(rail.UltimateCard.DisplayName.ToUpperInvariant(),
                Is.EqualTo("LEGEND OF THE SKIES"));

            // ROUND ONE: a card into the turn slot the grant will read back.
            yield return HerTurnOn(root, cutterId, CutterCardContent.GrapplingBolt);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var adjacentObstacle = new HexCoordinate(-4, 2);
            Assert.That(OfferedSpaces(runtime), Has.Member(adjacentObstacle), Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(adjacentObstacle).transform.position),
                Is.True,
                UiPlayer.DescribeBoardRay(pointerInput, Tile(adjacentObstacle).transform.position));
            yield return null;
            yield return null;

            // The grant is not offered on this turn: the previous turn slot is empty in round one, so
            // there is no card whose primary could be performed and the step is not drawn at all.
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Round one's pool names nothing, so nothing is offered. " + Remaining(runtime));

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);

            // ROUND TWO.
            yield return HerTurnOn(root, cutterId, CutterCardContent.WalkThePlank);
            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate;
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            yield return PressEffectControl(root, BulletLabelFor(runtime, "plank-minion-arm"));

            var minion = runtime.PendingEffectChoice;
            Assert.That(minion, Is.Not.Null,
                "\"Defeat a minion adjacent to you\" asks which. " + Remaining(runtime));
            Assert.That(minion.NodeId.Value, Is.EqualTo("plank-minion"), Remaining(runtime));
            var subject = runtime.ReadModel.Minions.Single(candidate =>
                candidate.MinionId.Equals(minion.Candidates[0].Value.MinionId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(subject.MinionId).transform.position),
                Is.True);
            yield return null;
            yield return null;

            // THE GRANT. It is a `may`, so it stands as an offer rather than firing.
            var grant = runtime.PendingEffectChoice;
            Assert.That(grant, Is.Not.Null,
                "Performing a primary action offers her Ultimate's grant. " + Remaining(runtime));
            Assert.That(grant.NodeId.Value, Is.EqualTo("legend-borrow"), Remaining(runtime));
            Assert.That(grant.DeclineInvocation, Is.Not.Null,
                "\"you MAY perform\" is answerable both ways. " + Remaining(runtime));
            var pool = grant.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                .Select(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .DefinitionReference.DefinitionId.Value)
                .ToArray();
            Assert.That(pool, Is.EqualTo(new[] { CutterCardContent.GrapplingBolt.Value }).AsCollection,
                "\"a card in the previous turn slot\" names the one card her own dashboard held " +
                "last round, and not the card resolving now. Pool: " + string.Join(", ", pool));

            // HandCardSelection: naming WHICH card is borrowed, on the whole printed face.
            yield return ClickACardFace(root);

            var borrowed = runtime.PendingEffectChoice;
            Assert.That(borrowed, Is.Not.Null,
                "The granted performance opens that card's own primary. " + Remaining(runtime));
            Assert.That(borrowed.NodeId.Value, Is.EqualTo("grapple-obstacle"), Remaining(runtime));
            var terrain = new HexCoordinate(-3, -2);
            Assert.That(OfferedSpaces(runtime), Has.Member(terrain), Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(terrain).transform.position), Is.True);
            yield return null;
            yield return null;

            var finished = runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate;
            Assert.That(finished, Is.EqualTo(new HexCoordinate(-3, -1)),
                "The borrowed primary reeled her to the terrain three away, which is a second " +
                "card's printed action performed inside one turn. " + Remaining(runtime));
            Assert.That(finished, Is.Not.EqualTo(stoodAt));
        }

        /// <summary>
        /// A Fistful of Coins: "An enemy hero in radius chooses one — • You place that hero in a space
        /// in radius. • You gain 3 coins. If you have 13 or more coins, you alone win the game."
        ///
        /// <para><b>THE ONLY JOURNEY IN THIS BUILD THAT ENDS THE MATCH.</b> No dealt hero's gate has
        /// ever ended one through a card, because until milestone 8.39g no card could: the three
        /// rulebook causes are all team sentences and every one of them is re-derived on restore from
        /// state that was already authoritative. This one names a HERO, and the winning fact has no
        /// other home, which is what moved the snapshot schema at that milestone.</para>
        ///
        /// <para><b>The ten coins are STAGED and that is the honest way to reach them.</b> p.13 pays
        /// two coins for a melee or ranged minion and four for a heavy, so farming to ten inside one
        /// journey would be five Defeats across five rounds - an arc no round holds and one that would
        /// prove nothing this journey is about. The Director's own sandbox stages a purse, which is
        /// the seam Silverarrow's and Tali's journeys already use for a board, and the card then adds
        /// the three it prints.</para>
        ///
        /// <para><b>The claim is written inside the frame and the MATCH ends at the command
        /// boundary</b>, which is milestone 8.39g1's seam: a capability must never call EndGame from
        /// inside a frame that is still running. So the outcome is waited for across frames rather
        /// than read on the one that answered the bullet.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AFistfulOfCoinsPaysItsThirteenthCoin_AndOneHeroAloneWinsTheMatch()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedCutter,
                cutterCoins: 10);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, cutterId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Assert.That(runtime.CoinsOf(cutterId), Is.EqualTo(10),
                "The staged purse is what the printed three completes.");
            Assert.That(runtime.ReadModel.Outcome.HasEnded, Is.False,
                "and the match is running before the card is played.");

            yield return HerTurnOn(root, cutterId, CutterCardContent.AFistfulOfCoins);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return null;

            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null, Remaining(runtime));
            Assert.That(modal.NodeId.Value, Is.EqualTo("chosen-modal"), Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "The Tier III printing hands the same choice to the same seat Tier II's does. " +
                Remaining(runtime));

            // The enemy takes the coins rather than being repositioned - which on this card is the
            // bullet that loses them the game, and is a legal answer they are entitled to pick.
            var coinArm = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                value.Command.EffectChoice.Candidate.OptionNodeId.Value == "chosen-coin-arm");
            Assert.That(runtime.SubmitProjectedCommand(coinArm.Command), Is.True, Remaining(runtime));
            yield return null;
            yield return null;

            // THE ELECTING SEAT CLOSES ITS OWN SELECTION, on its own drawn control.
            var closing = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(closing, Is.Not.Null,
                "The electing seat closes its own selection. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, closing.Label));
            yield return null;
            yield return null;

            Assert.That(runtime.CoinsOf(cutterId), Is.EqualTo(13),
                "Ten plus the printed three is the threshold the card names. Last refusal: '" +
                runtime.LastOperationDetail + "'. " + Remaining(runtime));

            // The command boundary, which is where every mid-turn ending in this build is noticed:
            // the claim is written inside the frame and the MATCH ends one command later, which is
            // milestone 8.39g1's seam - a capability must never call EndGame from inside a frame that
            // is still running.
            for (var budget = 0; budget < 12 && !runtime.ReadModel.Outcome.HasEnded; budget++)
            {
                if (runtime.IsTurnActive && runtime.RoundView.Model.EndTurnInvocation != null)
                {
                    runtime.SubmitEndTurn();
                }

                yield return null;
            }

            var outcome = runtime.ReadModel.Outcome;
            Assert.That(outcome.HasEnded, Is.True,
                "\"you alone win the game\" ends the match. " + Remaining(runtime));
            Assert.That(outcome.Cause, Is.EqualTo(
                GuardsDigital.Core.Rules.Victory.MatchVictoryCause.CoinHoardClaimed),
                "and it ends for the cause this card prints rather than for one of p.8's team " +
                "sentences.");
            Assert.That(outcome.HasSoloWinner, Is.True);
            Assert.That(outcome.WinningHeroId, Is.EqualTo(cutterId),
                "The hero who resolved the branch holding thirteen is the one who won, alone.");
        }
    }
}
