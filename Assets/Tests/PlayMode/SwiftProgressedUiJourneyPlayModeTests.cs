using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.SwiftUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Swift the Sharpshooter's printed faces, driven through the controls a player actually reaches.
    /// Milestone 8.38f, and the content half of the THIRTEENTH hero's player-path gate - the component
    /// half is <see cref="SwiftEngagedUiJourneyPlayModeTests"/>.
    ///
    /// <para><b>What each journey asserts, and why it is mostly the OFFER.</b> Her novelty is not in
    /// how a player names a choice - milestone 8.38f's shape derivation found she introduces no new
    /// interaction shape at all - but in WHICH spaces and units an offer contains. So the assertions
    /// below read the candidate set against the printed sentence and then complete the interaction,
    /// rather than chasing a long chain whose end says nothing the offer did not already say.</para>
    ///
    /// <para>The staged scenes are milestone 8.38e's: the base engagement for her five starting-hand
    /// faces, the Tier II ALTERNATE branch for Shotgun, Delayed Jump and Mark for Death, and a Tier III
    /// staging for Bullet Time, which p.16 unlocks there.</para>
    /// </summary>
    public sealed class SwiftProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// The enemy seat this staging deals opposite her - Wasp on Red. Named through the runtime
        /// rather than by side, so a restaging that moves her cannot silently point this at herself.
        /// </summary>
        private static HeroId EnemyOf(ConfiguredMatchSceneRuntime runtime, HeroId swiftId) =>
            OtherHeroId(runtime, swiftId);

        /// <summary>
        /// Drives her named card to the point where its primary action is hers to submit, and returns
        /// with the turn standing on her. Every content journey below opens this way.
        /// </summary>
        private static IEnumerator HerTurnOn(
            MatchSceneCompositionRoot root, HeroId swiftId, CardDefinitionId definitionId)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, swiftId, definitionId);
            yield return YieldUntilTheNamedSeatActs(runtime, swiftId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(swiftId),
                "Her card has to open the turn for this journey to drive it. " + Remaining(runtime));
        }

        /// <summary>
        /// Snipe: "Target a unit at maximum range, and in a straight line."
        ///
        /// <para><b>The engaged board offers this face NOTHING, and that is a measured fact rather
        /// than a surprise.</b> The milestone 8.15 engagement stands her at (-3,1) with the nearest
        /// enemies at 1, 2, 3 and 5 - nothing at her printed Range 4 at all - so a journey that
        /// committed Snipe where she stands would prove only that an empty offer is empty. She spends
        /// a turn walking two spaces to (-4,0) first, which is the one nearby space that puts TWO
        /// enemies at exactly four on a file: red-melee-03 at (-8,4) and red-melee-04 at (-4,4).</para>
        ///
        /// <para>That is what makes the assertion two-sided. The offer must contain exactly those two
        /// and nothing else - not the adjacent ranged minion, not the melee pair at two, not the enemy
        /// hero - so both halves of "at maximum range, AND in a straight line" are doing visible work.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator SnipeOffersOnlyTheMaximumRangeFile_AndNothingBesideIt()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var swiftId = SwiftSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var firingStep = new HexCoordinate(-4, 0);

            // One turn spent walking, on a card that is not Snipe - a secondary Movement and a primary
            // Attack are two different actions and p.9 lets a card give only one.
            CommitSoTheNamedSeatActs(runtime, swiftId, SwiftCardContent.SteamJump);
            yield return YieldUntilTheNamedSeatActs(runtime, swiftId);
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(
                runtime.PendingMovementChoice.LegalDestinations.Any(value =>
                    value.Destination.Equals(firingStep)),
                Is.True,
                "Her printed boot reaches the firing step two spaces away. " + Remaining(runtime));

            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(firingStep));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(firingStep));

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);
            yield return HerTurnOn(root, swiftId, SwiftCardContent.Snipe);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null,
                "Snipe's ranged Attack opens a target choice. " + Remaining(runtime));

            var offered = choice.LegalTargets
                .Select(target => target.Kind == AttackTargetKind.Hero
                    ? runtime.Heroes.Single(hero => hero.HeroId.Equals(target.HeroId)).Coordinate
                    : runtime.ReadModel.Minions
                        .Single(minion => minion.MinionId.Equals(target.MinionId)).Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();

            Assert.That(offered,
                Is.EqualTo(new[] { new HexCoordinate(-8, 4), new HexCoordinate(-4, 4) })
                    .AsCollection,
                "Exactly the two units at four on a file, and nothing nearer or off the file.");

            var shot = choice.LegalTargets[0].MinionId;
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(shot)),
                Is.True,
                "The narrowed target is takeable through the projected command a player submits.");
            yield return null;
            yield return null;

            // MILESTONE 8.86. What the Attack DID. This journey proved the offer exactly - two units,
            // at four, on a file - and then submitted a target and stopped, so a narrowing that was
            // perfect and an Attack that never landed would have read the same. p.13: "when you
            // target an enemy minion with an attack, it is simply defeated."
            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(shot)),
                Is.False,
                "The miniature at exactly her printed Range 4 was defeated and taken off the board. " +
                Remaining(runtime));
            Assert.That(
                Object.FindObjectsByType<StandardMinionView>(FindObjectsSortMode.None)
                    .Any(view => view.Occupancy != null &&
                        view.Occupancy.MinionId.Equals(shot)),
                Is.False,
                "and its piece left with it rather than standing where it was shot.");
        }

        /// <summary>
        /// Bounce: "Move 2 spaces in a straight line, ignoring obstacles; if this card is already
        /// resolved as you perform this action, may repeat once."
        ///
        /// <para><b>(-3,2) is what makes "ignoring obstacles" visible.</b> A friendly melee minion
        /// stands there, directly between her at (-3,1) and (-3,3) - so a destination two spaces along
        /// that file is reachable ONLY by a move that crosses an occupied space. The journey asserts
        /// that space is offered, which a move walking p.19's ordinary stopping rule could not.</para>
        ///
        /// <para>The repeat is NOT offered here, and that is the printed reading rather than an
        /// omission: the rider asks whether this card is ALREADY resolved as the action is performed,
        /// and on the turn Bounce itself is being resolved it is not.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BounceWalksAStraightLineThroughObstacles_AndOffersNoRepeatOnItsOwnTurn()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;
            var from = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;

            yield return HerTurnOn(root, swiftId, SwiftCardContent.Bounce);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);

            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null,
                "Bounce's Basic Skill asks where to land. " + Remaining(runtime));

            var destinations = choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            Assert.That(destinations, Is.Not.Empty, "She has somewhere to bounce to.");

            var shape = string.Join(", ", destinations.Select(value =>
                value + "/d" + HexDistanceQuery.Calculate(from, value)));
            foreach (var destination in destinations)
            {
                // Milestone 8.59. EXACTLY two, not within two. This read `Is.InRange(1, 2)` until a
                // player at the table asked why the spaces one away were lit when the card cannot
                // move to them: "Move 2 spaces" is a count, and `bounce-move` refuses a shorter walk,
                // so every landing one away was an answer that could only throw the action away.
                Assert.That(HexDistanceQuery.Calculate(from, destination),
                    Is.EqualTo(2),
                    "Every offered landing is the printed two spaces away. Offer was: " + shape);
                Assert.That(
                    destination.Q == from.Q || destination.R == from.R ||
                    (destination.Q + destination.R) == (from.Q + from.R),
                    Is.True,
                    "Every offered landing stands on one of p.19's straight lines from her. Offer " +
                    "was: " + shape);
            }

            // MEASURED, and the obstacle proof is on the q+r file rather than the q one: a Red ranged
            // minion stands at (-4,2), directly between her at (-3,1) and (-5,3) - and (-5,3) IS
            // offered. A move walking p.19's ordinary stopping rule could not reach it. The q file
            // going the other way stops at the friendly minion on (-3,2) because (-3,3) is not a
            // playable landing at all, which is why the crossing is asserted where the board allows it.
            var beyondTheObstacle = new HexCoordinate(-5, 3);
            Assert.That(destinations, Has.Member(beyondTheObstacle),
                "The space two along the file an enemy minion occupies is reachable, which is what " +
                "'ignoring obstacles' means. Offer was: " + shape);

            Assert.That(runtime.SubmitEffectChoiceValue(beyondTheObstacle), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(beyondTheObstacle),
                "She crossed the occupied space and landed beyond it.");

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "No repeat stands: the rider asks whether this card is ALREADY resolved, and on its " +
                "own turn it is not. " + Remaining(runtime));
        }

        /// <summary>
        /// Reload!: "Choose one - - Perform the primary action of your rightmost resolved card.
        /// - Defeat a minion adjacent to you."
        ///
        /// <para><b>This journey takes the BORROW bullet, and which bullet it takes is not a free
        /// choice.</b> `HeroPlayerPathMatrix` derives `HandCardSelection` from the
        /// `borrow-primary-action` capability - naming WHICH card to borrow is the hand selection -
        /// so only the borrow arm proves the shape the gate binds here. The DEFEAT arm is driven end
        /// to end by the Mark for Death journey below, which takes that same second bullet to trigger
        /// its bounty, so both printed arms are proven by real player input across the two journeys
        /// rather than one arm twice.</para>
        ///
        /// <para>A turn is spent resolving STEAM JUMP first, chosen deliberately: milestone 8.38c's
        /// pool is the cards this hero resolved THIS ROUND in committed order, so the borrow needs
        /// something behind it - and Steam Jump's own primary places her and then pushes an adjacent
        /// enemy, which is what makes the borrowed performance reach a board SPACE and a board UNIT
        /// rather than a move alone.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ReloadOffersItsTwoBulletsAndDefeatsAnAdjacentMinion()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;

            yield return HerTurnOn(root, swiftId, SwiftCardContent.SteamJump);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            for (var step = 0; step < 8 && runtime.PendingEffectChoice != null; step++)
            {
                var pause = runtime.PendingEffectChoice;
                if (pause.Candidates.Any(c => c.Value.Kind == EffectValueKind.Coordinate))
                {
                    runtime.SubmitEffectChoiceValue(pause.Candidates
                        .First(c => c.Value.Kind == EffectValueKind.Coordinate).Value.Coordinate);
                }
                else if (pause.SelectInvocations.Count > 0)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.SelectInvocations[0]));
                }
                else if (pause.ConfirmInvocation != null)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.ConfirmInvocation));
                }
                else
                {
                    break;
                }

                yield return null;
            }

            if (runtime.IsTurnActive)
            {
                runtime.SubmitEndTurn();
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);
            yield return HerTurnOn(root, swiftId, SwiftCardContent.Reload);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);

            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null,
                "Reload's Choose one stands as a modal. " + Remaining(runtime));
            Assert.That(modal.Candidates.Count, Is.EqualTo(2),
                "BOTH printed bullets are live once a card has been resolved this round - the state " +
                "worth asserting, because on her first turn the pool names nothing.");

            // -----------------------------------------------------------------------------------
            // Milestone 8.58. THE WINDOW, end to end, on the card the user photographed.
            //
            // Their words on the shipped build: "This sucks. We said we will show the card, and have
            // nice labels on the buttons?" - over two controls reading `MODE #1 RELOAD-BORROW-ARM`
            // and `MODE #2 RELOAD-DEFEAT-ARM`, on a card whose arms have both been DECLARED since
            // 8.46a. What was missing was the source card the label is keyed on, and this is where a
            // regression of that would show up in play rather than in a fixture.
            // -----------------------------------------------------------------------------------
            yield return null;
            var window = runtime.DecisionWindowView.Model;
            Assert.That(window, Is.Not.Null, "Reload!'s modal opens the decision window.");
            Assert.That(window.Family, Is.EqualTo(DecisionWindowFamily.Mode));
            Assert.That(window.ContextCards, Has.Count.EqualTo(1),
                "The card whose printed text is asking is drawn beside the question.");
            var reloadInstance = runtime.Cards.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(SwiftCardContent.Reload) &&
                card.OwnerId.Equals(swiftId));
            Assert.That(window.ContextCards[0].Card.CardInstanceId, Is.EqualTo(reloadInstance.Id),
                "and it is Reload! itself.");
            // `Scoped` appends the deciding hero on the DIRECTOR composition this scene runs, which is
            // why this is a prefix rather than an equality: a seat's own screen reads "RELOAD!".
            Assert.That(window.Title, Does.StartWith("RELOAD!"),
                "The window is titled with the card rather than with `CHOOSE ONE`.");
            Assert.That(
                window.Controls.Select(control => control.Label),
                Is.EquivalentTo(new[]
                {
                    "PERFORM THE PRIMARY ACTION OF YOUR RIGHTMOST RESOLVED CARD",
                    "DEFEAT A MINION ADJACENT TO YOU"
                }),
                "and both controls say what pressing them does, in the printed text's own words. " +
                "Drawn: " + string.Join(" | ", window.Controls.Select(control => control.Label)));
            foreach (var control in window.Controls)
            {
                Assert.That(control.Label, Does.Not.Contain("MODE #"),
                    "A seat never sees a node id.");
            }

            // The same photograph the staged frames in `DecisionWindowMockupCapturePlayModeTests`
            // take, on the card the user actually photographed, reached by real play.
            yield return UiPlayer.CaptureScreen(
                "tmp/858-captures/mode-choice-reload-live-" +
                Screen.width + "x" + Screen.height + ".png");

            Assert.That(runtime.SubmitEffectChoiceOption(modal.Candidates[0].OptionNodeId), Is.True);
            yield return null;

            // HandCardSelection: naming WHICH card is borrowed.
            var whichCard = runtime.PendingEffectChoice;
            Assert.That(whichCard, Is.Not.Null,
                "The borrow asks which resolved card it takes. " + Remaining(runtime));
            Assert.That(
                whichCard.Candidates.Any(candidate =>
                    candidate.Value.Kind == EffectValueKind.CardInstance),
                Is.True,
                "The borrow's subject is a CARD, which is the hand selection the matrix derives from " +
                "`borrow-primary-action`. Offered: " + string.Join("; ",
                    whichCard.Candidates.Select(c => c.Kind + ":" + c.Value.Kind)));

            var pool = whichCard.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                .Select(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .DefinitionReference.DefinitionId.Value)
                .ToArray();
            Assert.That(pool, Has.Member(SwiftCardContent.SteamJump.Value),
                "The pool names the card she actually resolved. Pool: " + string.Join(", ", pool));
            Assert.That(pool, Has.No.Member(SwiftCardContent.Reload.Value),
                "And NOT Reload! itself, which sits in the turn slots while it asks - milestone " +
                "8.38c's own recorded first false answer.");

            var chosen = whichCard.SelectInvocations.Count > 0
                ? whichCard.SelectInvocations[0]
                : whichCard.ConfirmInvocation;
            Assert.That(chosen, Is.Not.Null);
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(chosen)),
                Is.True);
            yield return null;

            // The card selection is answered by its Confirm and does not advance on the frame it is
            // submitted, so the borrowed primary is waited for rather than assumed - the same
            // two-stage shape Bullet Time's grant takes.
            PendingEffectChoice borrowed = null;
            for (var step = 0; step < 8; step++)
            {
                borrowed = runtime.PendingEffectChoice;
                if (borrowed != null &&
                    borrowed.Candidates.Any(c => c.Value.Kind == EffectValueKind.Coordinate))
                {
                    break;
                }

                if (borrowed != null)
                {
                    if (borrowed.SelectInvocations.Count > 0)
                    {
                        runtime.SubmitProjectedCommand(
                            DirectorCommand.AnswerEffectChoice(borrowed.SelectInvocations[0]));
                    }
                    else if (borrowed.ConfirmInvocation != null)
                    {
                        runtime.SubmitProjectedCommand(
                            DirectorCommand.AnswerEffectChoice(borrowed.ConfirmInvocation));
                    }
                }

                yield return null;
            }

            Assert.That(borrowed, Is.Not.Null,
                "The borrowed primary opens Steam Jump's own choice. " + Remaining(runtime));
            Assert.That(
                borrowed.Candidates.Any(candidate =>
                    candidate.Value.Kind == EffectValueKind.Coordinate),
                Is.True,
                "BORROWED OFFER: kind=" + borrowed.Kind + " min=" + borrowed.MinimumSelections +
                " max=" + borrowed.MaximumSelections + " confirm=" +
                (borrowed.ConfirmInvocation != null) + " select=" +
                borrowed.SelectInvocations.Count + " candidates=[" + string.Join("; ",
                    borrowed.Candidates.Select(c => c.Kind + ":" + c.Value.Kind)) + "]");

            // MILESTONE 8.86. A landing that is the space she is already standing on would satisfy
            // "she is at the landing" while the borrowed performance had done nothing at all, so the
            // one taken is a landing that travels and that is stated rather than hoped for.
            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;
            var travelling = borrowed.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .Where(space => !space.Equals(stoodAt))
                .ToArray();
            Assert.That(travelling, Is.Not.Empty,
                "\"Place yourself into a space in a straight line in radius\" - the only landing " +
                "offered was the space she already stood on, so the borrowed primary could not " +
                "place her anywhere. " + Remaining(runtime));
            var landing = travelling[0];
            Assert.That(runtime.SubmitEffectChoiceValue(landing), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(landing),
                "The borrowed primary moved her, which is Reload! performing another card's action.");
            Assert.That(HeroView(swiftId).Coordinate, Is.EqualTo(landing),
                "and her token followed the borrowed performance.");
        }

        /// <summary>
        /// Steam Jump: "Place yourself into a space in a straight line in radius. Push an enemy unit
        /// adjacent to you up to 1 space."
        ///
        /// <para>Milestone 8.38b's `synthetic-place-in-radius-in-straight-line@1`, reached through a
        /// player's own pointer. The landing is chosen so the Red ranged minion on (-4,2) ends up
        /// adjacent to her, because the second sentence needs a subject and a place that left her
        /// beside nobody would prove only that an empty push is empty.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SteamJumpPlacesHerOnAFileInRadius_ThenPushesWhatSheLandsBeside()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;
            var from = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;

            yield return HerTurnOn(root, swiftId, SwiftCardContent.SteamJump);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);

            var placement = runtime.PendingEffectChoice;
            Assert.That(placement, Is.Not.Null,
                "Steam Jump asks where to place her. " + Remaining(runtime));

            var landings = placement.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            Assert.That(landings, Is.Not.Empty);
            var shape = string.Join(", ", landings.Select(value =>
                value + "/d" + HexDistanceQuery.Calculate(from, value)));

            foreach (var landing in landings)
            {
                Assert.That(HexDistanceQuery.Calculate(from, landing), Is.InRange(1, 3),
                    "Every landing is inside the printed Radius 3. Offer was: " + shape);
                Assert.That(
                    landing.Q == from.Q || landing.R == from.R ||
                    (landing.Q + landing.R) == (from.Q + from.R),
                    Is.True,
                    "Every landing stands on a straight line from her, which is what separates this " +
                    "face from Delayed Jump's complement. Offer was: " + shape);
            }

            Assert.That(landings, Has.Member(new HexCoordinate(-4, 1)),
                "The landing that keeps the Red ranged minion adjacent. Offer was: " + shape);

            // MILESTONE 8.83 CHOOSES THE LANDING RATHER THAN NAMING IT, because the second printed
            // sentence is now really answered: a push has to have somewhere to push TO, and p.18 is
            // explicit that a unit stopped by an obstacle or the map's edge is pushed nought spaces
            // and the push is still valid. Landing beside a minion with its back to a wall would
            // therefore prove nothing about the distance this milestone corrected. So the landing is
            // the one that leaves an adjacent unit with a free space directly away from her, read off
            // the live board - and that such a landing exists at all is asserted rather than assumed.
            var pushable = landings
                .SelectMany(landing => runtime.ReadModel.Minions
                    .Where(minion =>
                        HexDistanceQuery.Calculate(landing, minion.Coordinate) == 1 &&
                        IsFree(Beyond(landing, minion.Coordinate)))
                    .Select(minion => (Landing: landing, At: minion.Coordinate)))
                .ToArray();
            Assert.That(pushable, Is.Not.Empty,
                "No landing on this board leaves an adjacent unit anywhere to be pushed, so the " +
                "printed push could not be measured at all. Offer was: " + shape);

            var beside = pushable[0].Landing;
            Assert.That(runtime.SubmitEffectChoiceValue(beside), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(beside));

            // The push, which is the second printed sentence and a separate pause.
            //
            // MILESTONE 8.83 FINISHED THIS HALF OF THE JOURNEY, and it had to: the journey used to
            // name a subject and stop, so it could not tell a push from a subject nobody moved - the
            // same blind spot 8.82 found on Mark for Death. The push declared `may` beside a printed
            // "up to 1 space", and `AdmittedDistances.ForDeclared` reads `may` as the declared number
            // and nothing less, so there was no distance to answer and `OfferTargets` set its minimum
            // to its maximum. It declares `up to` now, which is what her gate row has claimed all
            // along in `PushDistanceChoice`.
            var push = runtime.PendingEffectChoice;
            Assert.That(push, Is.Not.Null,
                "Landing beside an enemy unit opens the printed push. " + Remaining(runtime));
            Assert.That(push.SelectInvocations, Is.Not.Empty,
                "The push names a subject a player submits. Offer kind: " + push.Kind);
            Assert.That(push.DeclineInvocation, Is.Not.Null,
                "\"Push AN enemy unit ... up to 1 space\" has a floor of nought on its own count, " +
                "so not pushing is one of the answers. " + Remaining(runtime));

            var subject = push.SelectInvocations.FirstOrDefault(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                IsFree(Beyond(beside, invocation.Candidate.Value.Coordinate)));
            Assert.That(subject, Is.Not.Null,
                "The subject this landing was chosen for has to be one the push offers. " +
                Remaining(runtime));
            var pushedAt = subject.Candidate.Value.Coordinate;
            var pushed = runtime.ReadModel.Minions
                .FirstOrDefault(minion => minion.Coordinate.Equals(pushedAt));
            Assert.That(pushed, Is.Not.Null,
                "The subject offered stands where the offer says it does. " + Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(subject)),
                Is.True);
            yield return null;

            if (runtime.PendingEffectChoice?.ConfirmInvocation != null &&
                runtime.PendingEffectChoice.SelectedCandidates.Count > 0)
            {
                runtime.SubmitProjectedCommand(
                    DirectorCommand.AnswerEffectChoice(
                        runtime.PendingEffectChoice.ConfirmInvocation));
                yield return null;
            }

            // The distance. p.18 settles it over the units already named, and "up to 1 space" draws
            // it as the integers nought and one - which is the `PushDistanceChoice` her row declares
            // and which no player was ever shown before this milestone.
            var distance = runtime.PendingEffectChoice;
            Assert.That(distance, Is.Not.Null,
                "\"up to 1 space\" is a distance a player picks. " + Remaining(runtime));
            var furthest = distance.SelectInvocations
                .Where(invocation => invocation.Candidate.Value.Kind == EffectValueKind.Integer)
                .OrderByDescending(invocation => invocation.Candidate.Value.IntegerValue)
                .FirstOrDefault();
            Assert.That(furthest, Is.Not.Null,
                "The distances are the answers. Offer kind: " + distance.Kind + ". " +
                Remaining(runtime));
            Assert.That(
                distance.SelectInvocations.Any(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Integer &&
                    invocation.Candidate.Value.IntegerValue == 0),
                Is.True,
                "and nought is one of them, which is the whole of what \"up to\" adds to a number.");
            runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(furthest));
            yield return null;
            yield return null;

            Assert.That(
                runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(pushed.MinionId)).Coordinate,
                Is.EqualTo(Beyond(beside, pushedAt)),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the unit she landed beside really moved, " +
                "one space directly away from her, which is p.19's own measure of a push.");
        }

        /// <summary>
        /// The space a unit standing at <paramref name="target"/> is pushed into by a pusher standing
        /// at <paramref name="pusher"/> - one step further along the line between them, which is what
        /// p.18's push does to an adjacent unit at a distance of one.
        /// </summary>
        private static HexCoordinate Beyond(HexCoordinate pusher, HexCoordinate target) =>
            new HexCoordinate((2 * target.Q) - pusher.Q, (2 * target.R) - pusher.R);

        /// <summary>
        /// Whether the board draws that space as PLAYABLE and nothing is standing on it - Brogan's own
        /// push journey's reading, copied rather than re-derived: the board draws a tile for terrain
        /// too, and a piece pushed against a rock travels nowhere, which is a legal p.18 result and an
        /// impossible thing for a journey to assert a move about.
        /// </summary>
        private static bool IsFree(HexCoordinate space) =>
            Object.FindObjectsByType<HexTileView>()
                .Any(tile => tile.Coordinate.Equals(space) && tile.IsPlayable) &&
            !Object.FindObjectsByType<StandardMinionView>()
                .Any(view => view.Occupancy != null && view.Occupancy.Coordinate.Equals(space)) &&
            !Object.FindObjectsByType<PrimitiveHeroView>()
                .Any(view => view.Coordinate.Equals(space));

        /// <summary>
        /// Suppress: "An enemy hero in radius, who is not adjacent to terrain discards a card, if
        /// able."
        ///
        /// <para><b>The engaged board gives this face NO legal subject, and that is measured rather
        /// than guessed.</b> The one enemy hero stands on (-2,2), which has exactly one terrain space
        /// beside it - so the clause excludes him where he starts and the primary refuses outright.
        /// A journey that committed Suppress where he stands would prove only that a refusal
        /// refuses.</para>
        ///
        /// <para>So the journey spends the ENEMY's own turn walking him one space to (-2,1), which the
        /// board probe named: playable, free, no terrain beside it, and one space from her - inside her
        /// printed Radius 3. That single step is the difference between a face with no subject and a
        /// face with one, which is the clause doing visible work.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SuppressReachesOnlyAnEnemyAwayFromTerrain_AndTakesACardFromThem()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var swiftId = SwiftSeat(runtime).HeroId;
            var enemyId = EnemyOf(runtime, swiftId);
            var offTerrain = new HexCoordinate(-2, 1);

            // The enemy's own turn, spent stepping off the terrain that excludes him.
            // Both seats are committed explicitly, which is the shape the defense journey in the base
            // fixture already proves: CommitSoTheNamedSeatActs picks the OTHER seat's card itself and
            // that pick does not survive a hand this fixture has already reached into.
            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            Commit(runtime, swiftId,
                LowestInitiativeIdOf(runtime, swiftId, SwiftCardContent.Suppress));
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(
                runtime.PendingMovementChoice.LegalDestinations.Any(value =>
                    value.Destination.Equals(offTerrain)),
                Is.True,
                "His printed boot reaches the one space beside her that terrain does not touch.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(offTerrain));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(offTerrain));

            if (runtime.IsTurnActive)
            {
                runtime.SubmitEndTurn();
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);
            yield return HerTurnOn(root, swiftId, SwiftCardContent.Suppress);

            var handBefore = runtime.Cards
                .Count(card => card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.Hand);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True,
                "With the one enemy hero off terrain, Suppress has a subject and its primary runs. " +
                Remaining(runtime));

            // MILESTONE 8.86. The loop below presses the first invocation at every pause, so what
            // every hero-valued offer ADMITS is asserted as it goes: "an enemy hero in radius, who is
            // not adjacent to terrain" is the whole of what this face does, and a loop taking
            // whatever is first could not otherwise say which hero it reached.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The primary opens its own selection. " + Remaining(runtime));

            for (var step = 0; step < 8 && runtime.PendingEffectChoice != null; step++)
            {
                var pause = runtime.PendingEffectChoice;
                var heroes = pause.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                    .Select(candidate => candidate.Value.HeroId)
                    .ToArray();
                if (heroes.Length > 0)
                {
                    Assert.That(heroes, Is.EquivalentTo(new[] { enemyId }),
                        "The only hero this face offers is the one the walked turn stepped off " +
                        "terrain: the clause excludes him where he started, and it is the clause " +
                        "doing the work rather than the board having one hero on it. " +
                        Remaining(runtime));
                }

                if (pause.SelectInvocations.Count > 0)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.SelectInvocations[0]));
                }
                else if (pause.ConfirmInvocation != null)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.ConfirmInvocation));
                }
                else
                {
                    break;
                }

                yield return null;
            }

            yield return null;
            var handAfter = runtime.Cards
                .Count(card => card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.Hand);
            Assert.That(handAfter, Is.EqualTo(handBefore - 1),
                "The enemy she reached is one card lighter. " + Remaining(runtime));
        }

        /// <summary>
        /// Shotgun: "Target a unit in range. Before the attack, an enemy hero adjacent to the target
        /// discards a card, if able."
        ///
        /// <para><b>The bystander clause needs a board that does not exist until the journey builds
        /// it.</b> The engagement puts the one enemy hero on (-2,2), and no RED unit stands beside him
        /// within her printed Range 2 - so the clause would silently skip on "if able" and prove
        /// nothing. The journey spends his own turn walking him to (-1,1), which puts him beside the
        /// Red melee minion on (-1,0); that minion is exactly two from her, so it is a legal target
        /// AND it has an enemy hero adjacent to it.</para>
        ///
        /// <para>That is what makes the discard attributable: it fires because of who stands beside the
        /// TARGET, not because of who she attacked.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ShotgunStopsBeforeTheAttack_AndScattersOntoAHeroBesideTheTarget()
        {
            yield return ScatterOntoBystander(false);
        }

        [UnityTest]
        public IEnumerator SuperShotgunStopsBeforeTheAttack_AndScattersOntoAHeroBesideTheTarget()
        {
            yield return ScatterOntoBystander(true);
        }

        [UnityTest]
        public IEnumerator SuperShotgunDefeatsAnEmptyHandedBystander_ThenAttacksTheOriginalMinion()
        {
            yield return ScatterOntoBystander(true, true);
        }

        private static IEnumerator ScatterOntoBystander(bool super, bool emptyHand = false)
        {
            var cardId = super ? SwiftCardContent.SuperShotgun : SwiftCardContent.Shotgun;
            yield return LoadStagedScene(
                super ? SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSwift :
                    SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var swiftId = SwiftSeat(runtime).HeroId;
            var enemyId = EnemyOf(runtime, swiftId);
            var besideTheMinion = new HexCoordinate(-1, 1);

            if (emptyHand)
            {
                var onlyCard = HighestInitiativeAttackIdOf(runtime, enemyId);
                var nextTurnCard = runtime.Cards.First(card => card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.Hand &&
                    !card.DefinitionReference.DefinitionId.Equals(onlyCard)).DefinitionReference.DefinitionId;
                MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                    heroes: new[] { new SandboxHeroStaging(enemyId,
                        runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                        hand: new[] { onlyCard, nextTurnCard }) });
                yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSwift);
                root = Root();
                runtime = root.Runtime;
                hud = root.Hud;
            }

            // His turn, spent stepping beside the Red melee minion she can reach.
            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            Commit(runtime, swiftId, LowestInitiativeIdOf(runtime, swiftId, cardId));
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;
            Assert.That(
                runtime.PendingMovementChoice.LegalDestinations.Any(value =>
                    value.Destination.Equals(besideTheMinion)),
                Is.True,
                "His boot reaches the space beside the Red melee minion. " + Remaining(runtime));

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(besideTheMinion));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(besideTheMinion));

            if (runtime.IsTurnActive)
            {
                runtime.SubmitEndTurn();
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);
            yield return HerTurnOn(root, swiftId, cardId);

            var handBefore = runtime.Cards
                .Count(card => card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.Hand);
            if (emptyHand) Assert.That(handBefore, Is.Zero);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, Remaining(runtime));

            var minionTargets = choice.LegalTargets
                .Where(target => target.Kind == AttackTargetKind.Minion)
                .Select(target => runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(target.MinionId)))
                .ToArray();
            var beside = minionTargets.FirstOrDefault(minion =>
                HexDistanceQuery.Calculate(minion.Coordinate, besideTheMinion) == 1);
            Assert.That(beside, Is.Not.Null,
                "A legal target stands beside the hero she just walked into place. Offer: " +
                string.Join(", ", minionTargets.Select(m => m.MinionId.Value + m.Coordinate)));

            Assert.That(
                runtime.SubmitProjectedCommand(
                    DirectorCommand.ChooseAttackTarget(beside.MinionId)),
                Is.True);
            yield return null;

            var beforeAnswer = runtime.CaptureSnapshot();
            Assert.That(runtime.RestoreSnapshot(beforeAnswer), Is.True);
            yield return null;

            // "Before the attack" - the pause stands between the target being named and p.14's step 4.
            for (var step = 0; step < 8 && runtime.PendingEffectChoice != null; step++)
            {
                var pause = runtime.PendingEffectChoice;
                if (pause.SelectInvocations.Count > 0)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.SelectInvocations[0]));
                }
                else if (pause.ConfirmInvocation != null)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.ConfirmInvocation));
                }
                else
                {
                    break;
                }

                yield return null;
            }

            yield return null;
            var handAfter = runtime.Cards
                .Count(card => card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.Hand);
            Assert.That(handAfter, Is.EqualTo(emptyHand ? 0 : handBefore - 1),
                "The hero standing beside her target is one card lighter. " + Remaining(runtime));
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.EqualTo(!emptyHand));
            Assert.That(runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(beside.MinionId)),
                Is.False, "The original minion is attacked after the bystander's discard or Defeat.");
        }

        [UnityTest]
        public IEnumerator ShotgunAttackingAnIsolatedHero_DoesNotMakeItsTargetDiscard()
        {
            yield return AttackIsolatedHero(false);
        }

        [UnityTest]
        public IEnumerator SuperShotgunAttackingAnIsolatedHero_DoesNotMakeItsTargetDiscard()
        {
            yield return AttackIsolatedHero(true);
        }

        private static IEnumerator AttackIsolatedHero(bool super)
        {
            var cardId = super ? SwiftCardContent.SuperShotgun : SwiftCardContent.Shotgun;
            yield return LoadStagedScene(super
                ? SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSwift
                : SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;
            var enemyId = EnemyOf(runtime, swiftId);
            yield return HerTurnOn(root, swiftId, cardId);
            var handBefore = runtime.Cards.Count(card => card.OwnerId.Equals(enemyId) &&
                card.State.Location == CardLocation.Hand);
            yield return PointTrayAt(root, swiftId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargets.Any(target =>
                target.Kind == AttackTargetKind.Hero && target.HeroId.Equals(enemyId)), Is.True);
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var enemy = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId));
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(enemy.Coordinate));
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "An isolated attack target is not its own adjacent bystander.");
            Assert.That(runtime.Cards.Count(card => card.OwnerId.Equals(enemyId) &&
                card.State.Location == CardLocation.Hand), Is.EqualTo(handBefore));
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The original attack continues to its normal Defense response.");
            Assert.That(runtime.DeclineDefense(), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.False,
                "The original target, not a substituted bystander, receives the attack.");
        }

        /// <summary>
        /// Delayed Jump: "End of turn: Place yourself into a space in radius not in a straight line
        /// from you."
        ///
        /// <para>The complement of Steam Jump, and milestone 8.38b's
        /// `synthetic-place-in-radius-off-every-straight-line@1`. Two things are asserted that the
        /// in-line face cannot show: the promise stands rather than resolving when the card does, and
        /// the offer EXCLUDES every space on a file - which is p.19's adjacency rule showing through a
        /// clause that never mentions it, because every neighbour of hers is on some file.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DelayedJumpStandsItsPromise_AndPlacesHerWhenTheTurnEnds()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;
            var from = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;

            yield return HerTurnOn(root, swiftId, SwiftCardContent.DelayedJump);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);

            // The promise STANDS: nothing places her on the frame the card resolves.
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(from),
                "An End-of-turn clause has not fired while the turn is still hers.");

            // ActiveEffectReading: the promise is READABLE as a live Active effect sourced by the
            // card that made it, which is what a player sees on the card while it stands.
            var promiseCard = runtime.Cards.Single(card =>
                card.OwnerId.Equals(swiftId) &&
                card.DefinitionReference.DefinitionId.Equals(SwiftCardContent.DelayedJump));
            Assert.That(runtime.IsActiveEffectSource(promiseCard.Id), Is.True,
                "The standing End-of-turn promise reads as an Active effect on its own card.");

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                yield return null;
            }

            // The promise fires as the turn closes, and closing is not a single frame: the seat hands
            // the turn on and the end-of-turn window runs after that. Wait for the pause rather than
            // assuming the frame after SubmitEndTurn is the one that carries it.
            PendingEffectChoice placement = null;
            for (var step = 0; step < 12; step++)
            {
                placement = runtime.PendingEffectChoice;
                if (placement != null)
                {
                    break;
                }

                if (runtime.IsTurnActive)
                {
                    runtime.SubmitEndTurn();
                }

                yield return null;
            }

            Assert.That(placement, Is.Not.Null,
                "Ending the turn is what the promise was waiting for. She stands at " +
                runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate +
                " (started " + from + "). " + Remaining(runtime));

            var landings = placement.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            Assert.That(landings, Is.Not.Empty);
            var shape = string.Join(", ", landings.Select(value =>
                value + "/d" + HexDistanceQuery.Calculate(from, value)));

            foreach (var landing in landings)
            {
                Assert.That(HexDistanceQuery.Calculate(from, landing), Is.InRange(1, 2),
                    "Every landing is inside the printed Radius 2. Offer was: " + shape);
                Assert.That(
                    landing.Q == from.Q || landing.R == from.R ||
                    (landing.Q + landing.R) == (from.Q + from.R),
                    Is.False,
                    "NO landing stands on a straight line from her, which is the whole of what this " +
                    "face prints that Steam Jump does not. Offer was: " + shape);
            }

            Assert.That(runtime.SubmitEffectChoiceValue(landings[0]), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(landings[0]),
                "She stands where the promise placed her.");
            // MILESTONE 8.86: a Place is only a Place if the space is a different one. The loop above
            // already refuses every landing on a file from her, which excludes her own space by
            // construction - this says so out loud rather than leaving it to be re-derived.
            Assert.That(landings[0], Is.Not.EqualTo(from),
                "and it is not the space she was standing on when the promise was made. " +
                Remaining(runtime));
            Assert.That(HeroView(swiftId).Coordinate, Is.EqualTo(landings[0]),
                "with her token drawn where the promise put her.");
        }

        /// <summary>
        /// Mobile Scout: "End of turn: Place yourself into a space in radius not in a straight line
        /// from you. You may then fast travel once, if able."
        ///
        /// <para><b>Milestone 8.117, and the only Fast Travel in her set.</b> Both questions belong to
        /// the Active effect's own trigger body, so both are met at the turn's close. "If able" is the
        /// board's to answer: measured headlessly on this staging, the landings inside the Battle Zone
        /// leave an enemy in her Zone and nothing is asked, while (-4,0) is in her own approach Zone,
        /// where p.11 admits the Fast Travel - so the journey lands there and is offered the printed
        /// "may" beside the lit Zone spaces, and takes it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MobileScoutPlacesHerAtTheTurnsCloseAndThenOffersItsFastTravel()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var quietLanding = new HexCoordinate(-4, 0);

            yield return HerTurnOn(root, swiftId, SwiftCardContent.MobileScout);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "\"End of turn:\" asks nothing while the Skill resolves. " + Remaining(runtime));

            PendingEffectChoice placement = null;
            for (var step = 0; step < 12; step++)
            {
                placement = runtime.PendingEffectChoice;
                if (placement != null)
                {
                    break;
                }

                if (runtime.IsTurnActive || runtime.PendingTurnRelease != null)
                {
                    runtime.SubmitEndTurn();
                }

                yield return null;
            }

            Assert.That(placement, Is.Not.Null,
                "Ending the turn is what the promise was waiting for. " + Remaining(runtime));
            Assert.That(placement.NodeId.Value, Is.EqualTo("late-place"), Remaining(runtime));
            Assert.That(LitDestinations(runtime), Contains.Item(quietLanding),
                "The measured landing is inside the printed Radius 2 and off every file from her. " +
                Remaining(runtime));
            Assert.That(UiPlayer.ClickBoard(
                pointerInput, root.Hud, Tile(quietLanding).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(quietLanding), "She stands where the promise placed her.");

            var travel = runtime.PendingEffectChoice;
            Assert.That(travel, Is.Not.Null,
                "\"You may then fast travel once, if able\" - and on this landing she is able. " +
                Remaining(runtime));
            Assert.That(travel.NodeId.Value, Is.EqualTo("late-travel"), Remaining(runtime));
            Assert.That(travel.ChooserId, Is.EqualTo(swiftId));
            Assert.That(travel.DeclineInvocation, Is.Not.Null,
                "A \"may\" a player cannot refuse is not a May. " + Remaining(runtime));

            var destination = LitDestinations(runtime)
                .First(space => !space.Equals(quietLanding));
            Assert.That(UiPlayer.ClickBoard(
                pointerInput, root.Hud, Tile(destination).transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, travel.NodeId);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(destination), "The Fast Travel moved her. " + Remaining(runtime));
            Assert.That(HeroView(swiftId).Coordinate, Is.EqualTo(destination));
        }

        /// <summary>
        /// Hunting Season: "Move up to two enemy minions in radius up to 3 spaces to spaces in
        /// radius. **Next turn:** The first two times an enemy minion in radius is defeated, gain 1
        /// coin."
        ///
        /// <para><b>Milestone 8.83, and the control this journey exists for is the one the card grew.</b>
        /// The face composed ONE subject and ONE move until 8.83 - only the bounty count doubled - so
        /// "up to two" asked nothing and a player met exactly Mark for Death's questions. It now
        /// composes two passes, each an <see cref="EffectNodeKind.Optional"/> carrying
        /// <see cref="EffectFailurePolicy.UpTo"/>, and that is the `ConfirmOrDecline` her gate row now
        /// declares for this face and for no other reason.</para>
        ///
        /// <para><b>What it drives, and why it stops where it does.</b> The first pass is TAKEN, a
        /// minion is named, a destination that really travels is named, and the board is read
        /// afterwards - the assertion milestone 8.82 learned to make. The second pass is then
        /// DECLINED, because "up to two" includes one and a journey that could only finish by moving
        /// two would be asserting the opposite of what the card says. That the second pass's own offer
        /// excludes the minion the first one moved is measured against the real definition in
        /// <c>SwiftMarkForDeathTests.HuntingSeasonsSecondSelection_ExcludesTheMinionTheFirstOneNamed</c>,
        /// where a board can be built to hold three minions and the answer is about the content rather
        /// than about the staging.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HuntingSeasonMovesOneMinion_ThenLetsHerWalkAwayFromTheSecond()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;

            yield return HerTurnOn(root, swiftId, SwiftCardContent.HuntingSeason);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);

            var firstPass = runtime.PendingEffectChoice;
            Assert.That(firstPass, Is.Not.Null,
                "\"Move UP TO two enemy minions in radius\" opens a decision before it opens a " +
                "selection: the count's floor is zero. " + Remaining(runtime));
            Assert.That(firstPass.ConfirmInvocation, Is.Not.Null,
                "The pass can be taken. " + Remaining(runtime));
            Assert.That(firstPass.DeclineInvocation, Is.Not.Null,
                "THE CONTROL THIS FACE GREW AT 8.83: \"up to two\" includes none, so walking away " +
                "from the first pass is one of the answers a player is shown. " + Remaining(runtime));
            runtime.SubmitProjectedCommand(
                DirectorCommand.AnswerEffectChoice(firstPass.ConfirmInvocation));
            yield return null;

            var chosenMinion = default(MinionId);
            var movedFrom = default(HexCoordinate);
            var secondPass = default(PendingEffectChoice);
            for (var step = 0; step < 8 && runtime.PendingEffectChoice != null; step++)
            {
                var pause = runtime.PendingEffectChoice;
                var minionOffer = pause.SelectInvocations.FirstOrDefault(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Minion);
                if (minionOffer != null && !chosenMinion.IsDefined)
                {
                    chosenMinion = minionOffer.Candidate.Value.MinionId;
                    movedFrom = runtime.ReadModel.Minions
                        .Single(minion => minion.MinionId.Equals(chosenMinion)).Coordinate;
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(minionOffer));
                }
                else if (chosenMinion.IsDefined && pause.SelectInvocations.Any(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate))
                {
                    var her = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;
                    var travelling = pause.SelectInvocations
                        .Where(invocation =>
                            invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                            !invocation.Candidate.Value.Coordinate.Equals(movedFrom))
                        .ToArray();
                    var travels =
                        travelling.FirstOrDefault(invocation =>
                            HexDistanceQuery.Calculate(
                                her, invocation.Candidate.Value.Coordinate) == 1)
                        ?? travelling.FirstOrDefault();
                    Assert.That(travels, Is.Not.Null,
                        "\"up to 3 spaces to spaces in radius\" - the only destination offered was " +
                        "the space the minion already stood on. " + Remaining(runtime));
                    runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(travels));
                }
                else if (pause.DeclineInvocation != null && chosenMinion.IsDefined)
                {
                    // The SECOND pass. It is the same decision as the first, asked again because the
                    // sentence names two minions and the player has moved one.
                    secondPass = pause;
                    break;
                }
                else if (pause.ConfirmInvocation != null)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.ConfirmInvocation));
                }
                else
                {
                    break;
                }

                yield return null;
            }

            Assert.That(chosenMinion.IsDefined, Is.True,
                "\"Move up to two enemy minions in radius\" - the first pass has to offer one. " +
                Remaining(runtime));
            Assert.That(
                runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(chosenMinion)).Coordinate,
                Is.Not.EqualTo(movedFrom),
                "and the minion really left the space it stood on - a journey that only answers " +
                "cannot tell a move from a step that never ran. " + Remaining(runtime));

            Assert.That(secondPass, Is.Not.Null,
                "The second pass asks before it happens: \"up to TWO\" is two decisions, not one " +
                "decision and one forced repetition. " + Remaining(runtime));
            Assert.That(secondPass.DeclineInvocation, Is.Not.Null,
                "and walking away from it is legal, which is what \"up to\" means over a count.");
            runtime.SubmitProjectedCommand(
                DirectorCommand.AnswerEffectChoice(secondPass.DeclineInvocation));
            yield return null;
            yield return null;

            Assert.That(
                runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(chosenMinion)).Coordinate,
                Is.Not.EqualTo(movedFrom),
                "and the declined second pass leaves the first move standing.");
        }

        /// <summary>
        /// Mark for Death: "Move an enemy minion in radius up to 3 spaces to a space in radius.
        /// **Next turn:** the first time an enemy minion in radius is defeated, gain 1 coin."
        ///
        /// <para>Milestone 8.38c1's counted bounty, across the turn boundary it is printed to cross.
        /// The promise is stood on one turn and PAID on the next, and the payment is measured against
        /// the same Defeat performed with no promise standing - which is what separates the bounty
        /// from p.13's own coins for defeating a minion.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MarkForDeathMovesAMinion_ThenPaysItsCountedBountyNextTurn()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;

            yield return HerTurnOn(root, swiftId, SwiftCardContent.MarkForDeath);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);

            // The printed move: a subject, then where it goes. Both are answered through projected
            // commands rather than by reaching past the offer.
            //
            // MILESTONE 8.82. This loop used to answer `SelectInvocations[0]` blindly at every
            // pause, and that is why it passed for four milestones while nothing moved an inch: the
            // move's leash was declared and bound no number, `DirectMoveDestinations.Admit` read
            // that as admitting nothing, and the selection that declares the move as its consumer
            // was therefore narrowed to NOBODY - so neither printed question was ever asked and the
            // loop simply found nothing to answer. A journey that takes whatever is first cannot
            // tell a move from a stand-still, or from a step that never ran; this one names the
            // subject, refuses its own space as the destination, and reads the board afterwards.
            var chosenMinion = default(MinionId);
            var movedFrom = default(HexCoordinate);
            for (var step = 0; step < 8 && runtime.PendingEffectChoice != null; step++)
            {
                var pause = runtime.PendingEffectChoice;
                var minionOffer = pause.SelectInvocations.FirstOrDefault(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Minion);
                if (minionOffer != null)
                {
                    chosenMinion = minionOffer.Candidate.Value.MinionId;
                    movedFrom = runtime.ReadModel.Minions
                        .Single(minion => minion.MinionId.Equals(chosenMinion)).Coordinate;
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(minionOffer));
                }
                else if (pause.SelectInvocations.Any(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate))
                {
                    Assert.That(chosenMinion.IsDefined, Is.True,
                        "A destination is offered only after the minion it is for has been named.");
                    // A destination that really travels, preferring one beside her: the minion this
                    // face moves is the minion the NEXT turn's Reload defeats, and a move that
                    // walked it out of her reach would prove the leash by breaking the rest of the
                    // journey. Her own space is where the disk is measured from, so "beside her" is
                    // inside it by construction.
                    var her = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;
                    var travelling = pause.SelectInvocations
                        .Where(invocation =>
                            invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                            !invocation.Candidate.Value.Coordinate.Equals(movedFrom))
                        .ToArray();
                    var travels =
                        travelling.FirstOrDefault(invocation =>
                            HexDistanceQuery.Calculate(
                                her, invocation.Candidate.Value.Coordinate) == 1)
                        ?? travelling.FirstOrDefault();
                    Assert.That(travels, Is.Not.Null,
                        "\"Move an enemy minion in radius up to 3 spaces to a space in radius\" - " +
                        "the only destination offered was the space it already stood on, so the " +
                        "card could not move anything. Offer was: " + string.Join(", ",
                            pause.SelectInvocations
                                .Where(invocation =>
                                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate)
                                .Select(invocation =>
                                    invocation.Candidate.Value.Coordinate.ToString())));
                    runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(travels));
                }
                else if (pause.ConfirmInvocation != null)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.ConfirmInvocation));
                }
                else
                {
                    break;
                }

                yield return null;
            }

            Assert.That(chosenMinion.IsDefined, Is.True,
                "\"Move an enemy minion in radius\" - the selection has to offer one. It offered " +
                "nobody until milestone 8.82, because the move it declares as its consumer could " +
                "act on nobody. " + Remaining(runtime));
            Assert.That(
                runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(chosenMinion)).Coordinate,
                Is.Not.EqualTo(movedFrom),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the minion really left the space it " +
                "stood on. " + Remaining(runtime));

            // ActiveEffectReading: the bounty stands as a live Active effect on the card that made
            // it, and it has to still be standing when her NEXT turn opens - which is the whole of
            // what "Next turn:" means and the reason this is read here rather than inferred later.
            var markCard = runtime.Cards.Single(card =>
                card.OwnerId.Equals(swiftId) &&
                card.DefinitionReference.DefinitionId.Equals(SwiftCardContent.MarkForDeath));
            // The source-card reminder begins as soon as the promise stands, while the original
            // active-only list remains empty until the next-turn activation anchor. That split keeps
            // the player warned without lighting the target/radius or active-value surfaces early.
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Empty,
                "The next-turn bounty is not mechanically active on the turn that created it.");
            Assert.That(runtime.StandingActiveEffectSourceCardInstanceIds,
                Does.Contain(markCard.Id),
                "The source card stays violet continuously from creation through activation.");

            if (runtime.IsTurnActive)
            {
                runtime.SubmitEndTurn();
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);



            // Next turn. Reload's second bullet is the Defeat the promise is watching for.
            yield return HerTurnOn(root, swiftId, SwiftCardContent.Reload);
            var before = runtime.CoinsOf(swiftId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);

            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null, Remaining(runtime));
            Assert.That(modal.Candidates.Count, Is.EqualTo(2),
                "Mark for Death was resolved last round, so both bullets stand.");
            Assert.That(runtime.SubmitEffectChoiceOption(modal.Candidates[1].OptionNodeId), Is.True);
            yield return null;

            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null,
                "The defeat bullet asks which adjacent minion. " + Remaining(runtime));
            Assert.That(subject.SelectInvocations, Is.Not.Empty);

            // MILESTONE 8.86. The minion is NAMED and the board is read afterwards. This took
            // `SelectInvocations[0]` and asserted only that coins went up, so a bullet that defeated
            // nobody while p.13 paid for something else would have read the same - and the bounty
            // this journey exists for is precisely a payment attributable to one Defeat.
            var herAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;
            var doomed = subject.SelectInvocations.FirstOrDefault(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Minion);
            Assert.That(doomed, Is.Not.Null,
                "\"Defeat a minion adjacent to you\" names a MINION. Offered: " + string.Join("; ",
                    subject.SelectInvocations.Select(invocation =>
                        invocation.Candidate.Value.Kind.ToString())) + ". " + Remaining(runtime));
            var doomedId = doomed.Candidate.Value.MinionId;
            var doomedAt = runtime.ReadModel.Minions
                .Single(minion => minion.MinionId.Equals(doomedId)).Coordinate;
            Assert.That(HexDistanceQuery.Calculate(doomedAt, herAt), Is.EqualTo(1),
                "and it is standing beside her, which is the other half of the printed bullet.");
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(doomed)),
                Is.True);
            yield return null;

            for (var step = 0; step < 8 && runtime.PendingEffectChoice != null; step++)
            {
                var pause = runtime.PendingEffectChoice;
                if (pause.SelectInvocations.Count > 0)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.SelectInvocations[0]));
                }
                else if (pause.ConfirmInvocation != null)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(pause.ConfirmInvocation));
                }
                else
                {
                    break;
                }

                yield return null;
            }

            if (runtime.IsTurnActive)
            {
                runtime.SubmitEndTurn();
                yield return null;
            }

            yield return null;
            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(doomedId)),
                Is.False,
                "MILESTONE 8.86: the named minion really left the board, which is the cause the " +
                "bounty below is being paid for. " + Remaining(runtime));
            var gained = runtime.CoinsOf(swiftId) - before;
            Assert.That(gained, Is.GreaterThan(0),
                "Defeating a minion inside the marked radius pays. Gained " + gained + " coins. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Bullet Time: "After you resolve a basic card you may perform the primary action on that
        /// card; you cannot target the same enemy hero twice in the same turn this way."
        ///
        /// <para>Milestone 8.38c2's fifth `UltimateTriggerCondition`, which reads a basic CARD being
        /// resolved rather than a basic ACTION completing. p.16 stands the Ultimate faceup and
        /// permanent, so nothing is committed to reach it - resolving Bounce is what offers the
        /// grant.</para>
        ///
        /// <para>The grant is a `may`, so the journey asserts it is OFFERED before taking it: an
        /// Ultimate that fired silently would be a different rule from the one printed.</para>
        ///
        /// <para><b>Milestone 8.54 carried it one performance further, and that performance is the
        /// shape Bounce's own gate row has always rested on this journey for.</b> The instance the
        /// Ultimate grants is taken off a card p.16 has already resolved, so Bounce's printed "may
        /// repeat once" is asked of it - and until 8.54 it was not, because the engine commits the
        /// card's resolution status below the checkpoint the grant fires at. The repeat is answered in
        /// the DECISION WINDOW the way a player answers it (milestone 8.58), and the board is measured:
        /// three legs of exactly two spaces onto three spaces she had not been standing on.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BulletTimeGrantsTheResolvedCardsPrimary_AndNamesItsOwnRefusal()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;

            // UltimateRailVisibility: p.16 stands the unlocked Ultimate OUT of the deck and on her
            // rail, which is what makes it permanent and reachable without being committed.
            var ultimate = runtime.Cards.Single(card =>
                card.OwnerId.Equals(swiftId) &&
                card.DefinitionReference.DefinitionId.Equals(SwiftCardContent.BulletTime));
            Assert.That(ultimate.State.Location, Is.Not.EqualTo(CardLocation.Deck),
                "Her unlocked Ultimate stands on the rail rather than in the deck.");
            Assert.That(ultimate.State.Location, Is.Not.EqualTo(CardLocation.Hand),
                "And it is not a card in hand: p.16 keeps the Ultimate out of the five.");

            yield return HerTurnOn(root, swiftId, SwiftCardContent.Bounce);
            var start = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var landing = runtime.PendingEffectChoice;
            Assert.That(landing, Is.Not.Null, Remaining(runtime));
            var first = landing.Candidates
                .First(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Value.Coordinate;
            Assert.That(runtime.SubmitEffectChoiceValue(first), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(first),
                "The card's own primary moved her before the Ultimate is reached.");

            // The grant. It is a `may`, so it stands as an offer rather than firing.
            yield return null;
            var grant = runtime.PendingEffectChoice;
            Assert.That(grant, Is.Not.Null,
                "Resolving a basic card offers the Ultimate's grant. Status: " + Remaining(runtime));

            var accept = grant.ConfirmInvocation ??
                (grant.SelectInvocations.Count > 0 ? grant.SelectInvocations[0] : null);
            Assert.That(accept, Is.Not.Null,
                "The grant is answerable through a projected command. Kind: " + grant.Kind +
                " candidates=" + grant.Candidates.Count +
                " confirm=" + (grant.ConfirmInvocation != null) +
                " decline=" + (grant.DeclineInvocation != null));
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(accept)),
                Is.True);
            yield return null;

            // Taking it performs the SAME card's printed primary a second time - but not in one step.
            // MEASURED: the grant first asks WHICH CARD, as `SelectTarget min=1 max=1` over a single
            // `CardInstance` candidate answered by its Confirm, and only then does that card's own
            // primary open its choice. That two-stage shape is the seam milestone 8.38d records, where
            // a permanent ability could choose a card and grant nothing.
            PendingEffectChoice again = null;
            for (var step = 0; step < 8; step++)
            {
                again = runtime.PendingEffectChoice;
                if (again == null)
                {
                    yield return null;
                    continue;
                }

                if (again.Candidates.Any(c => c.Value.Kind == EffectValueKind.Coordinate))
                {
                    break;
                }

                if (again.SelectInvocations.Count > 0)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(again.SelectInvocations[0]));
                }
                else if (again.ConfirmInvocation != null)
                {
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(again.ConfirmInvocation));
                }
                else
                {
                    break;
                }

                yield return null;
            }

            Assert.That(again, Is.Not.Null,
                "The granted performance opens the card's own primary. " + Remaining(runtime));
            Assert.That(again.Candidates.Any(c => c.Value.Kind == EffectValueKind.Coordinate),
                Is.True,
                "The granted primary asks where to bounce a second time. Kind: " + again.Kind +
                " candidates=[" + string.Join("; ",
                    again.Candidates.Select(c => c.Kind + ":" + c.Value.Kind)) + "]");

            var second = again.Candidates
                .First(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Value.Coordinate;
            Assert.That(runtime.SubmitEffectChoiceValue(second), Is.True);
            yield return null;

            var finished = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;
            Assert.That(finished, Is.EqualTo(second));
            Assert.That(finished, Is.Not.EqualTo(start),
                "Two performances of the same printed primary moved her twice in one turn.");

            // Milestone 8.54, and the shape Bounce's own row has always rested on this journey for:
            // RepeatDecision. The granted performance was taken off a card p.16 has already resolved,
            // so Bounce's "may repeat once" is asked - in the decision window, where a player answers
            // it.
            yield return YieldUntilTheRepeatStands(runtime);
            var hud = root.Hud;
            var take = TheRepeatsAnswer(runtime, EffectChoiceCommandKind.Confirm);
            Assert.That(take, Is.Not.Null,
                "The repeat is a decision the player takes. " + Remaining(runtime));
            Assert.That(TheRepeatsAnswer(runtime, EffectChoiceCommandKind.Decline), Is.Not.Null,
                "and one they may refuse, which is what makes it an offer rather than a rule that " +
                "fires. " + Remaining(runtime));
            UiPlayer.ClickEffectAnswer(runtime, hud, take);
            yield return null;

            var third = runtime.PendingEffectChoice;
            Assert.That(third, Is.Not.Null,
                "Taking the repeat asks where to bounce a third time. " + Remaining(runtime));
            var last = third.Candidates
                .First(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Value.Coordinate;
            Assert.That(runtime.SubmitEffectChoiceValue(last), Is.True);
            yield return null;

            var ended = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;
            Assert.That(ended, Is.EqualTo(last));
            var walk = string.Join(" -> ", new[] { start, first, second, ended });
            foreach (var leg in new[] { (start, first), (first, second), (second, ended) })
            {
                Assert.That(leg.Item2, Is.Not.EqualTo(leg.Item1),
                    "Each of the three performances moved her: " + walk);
                Assert.That(HexDistanceQuery.Calculate(leg.Item1, leg.Item2), Is.EqualTo(2),
                    "\"Move 2 spaces\" is a count, and every one of the three legs walked it: " +
                    walk);
            }
        }

        /// <summary>
        /// Milestone 8.54's other half, driven the same way: the repeat is REFUSABLE in the decision
        /// window, and refusing it leaves the two moves p.16's grant alone buys.
        ///
        /// <para>A separate journey rather than a branch of the one above, because the two arms end in
        /// different board states and a `may` is only proved an offer when both of its answers are
        /// shown to be reachable through the control a player presses.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BulletTimesGrantedRepeat_IsRefusedInTheDecisionWindow_AndLeavesTwoMoves()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var swiftId = SwiftSeat(runtime).HeroId;

            yield return HerTurnOn(root, swiftId, SwiftCardContent.Bounce);
            var start = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var landing = runtime.PendingEffectChoice;
            Assert.That(landing, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitEffectChoiceValue(landing.Candidates
                .First(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Value.Coordinate), Is.True);
            yield return null;
            var first = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;

            // The grant, then the card it names, then the granted performance's own destination.
            yield return null;
            for (var step = 0; step < 8; step++)
            {
                var standing = runtime.PendingEffectChoice;
                if (standing == null)
                {
                    yield return null;
                    continue;
                }

                if (standing.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.Coordinate))
                {
                    break;
                }

                var answer = standing.SelectInvocations.Count > 0
                    ? standing.SelectInvocations[0]
                    : standing.ConfirmInvocation;
                Assert.That(answer, Is.Not.Null, Remaining(runtime));
                Assert.That(runtime.SubmitProjectedCommand(
                    DirectorCommand.AnswerEffectChoice(answer)), Is.True);
                yield return null;
            }

            var granted = runtime.PendingEffectChoice;
            Assert.That(granted, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitEffectChoiceValue(granted.Candidates
                .First(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Value.Coordinate), Is.True);
            yield return null;
            var second = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;

            yield return YieldUntilTheRepeatStands(runtime);
            var refuse = TheRepeatsAnswer(runtime, EffectChoiceCommandKind.Decline);
            Assert.That(refuse, Is.Not.Null,
                "A printed `may` is refusable through a control of its own. " + Remaining(runtime));
            UiPlayer.ClickEffectAnswer(runtime, root.Hud, refuse);
            yield return null;
            yield return null;

            var ended = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;
            Assert.That(ended, Is.EqualTo(second),
                "A refused repeat performs nothing, so she is standing where the grant left her: " +
                string.Join(" -> ", new[] { start, first, second, ended }));
            Assert.That(HexDistanceQuery.Calculate(start, first), Is.EqualTo(2),
                "Her own primary walked the printed count.");
            Assert.That(HexDistanceQuery.Calculate(first, second), Is.EqualTo(2),
                "and so did the one the Ultimate granted - two legs of two, and no third.");
        }

        /// <summary>
        /// Waits for Bounce's own repeat question to stand, which is the node the granted performance
        /// reaches after its move. Milestone 8.54.
        /// </summary>
        private static IEnumerator YieldUntilTheRepeatStands(ConfiguredMatchSceneRuntime runtime)
        {
            for (var step = 0; step < 12; step++)
            {
                var standing = runtime.PendingEffectChoice;
                if (standing != null && standing.NodeId.Value == "bounce-repeat")
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(
                "The granted performance never reached Bounce's \"may repeat once\". " +
                Remaining(runtime));
        }

        /// <summary>
        /// The repeat question's own invocation of the named kind, read off the round model the
        /// pause published - which is the same object the decision window's control is bound to.
        /// Milestone 8.54.
        /// </summary>
        private static DirectorCommand TheRepeatsAnswer(
            ConfiguredMatchSceneRuntime runtime, EffectChoiceCommandKind kind) =>
            runtime.RoundView.Model.EffectChoices
                .Select(choice => choice.Command)
                .FirstOrDefault(command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Kind == kind);
    }
}
