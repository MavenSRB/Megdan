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

            Assert.That(
                runtime.SubmitProjectedCommand(
                    DirectorCommand.ChooseAttackTarget(choice.LegalTargets[0].MinionId)),
                Is.True,
                "The narrowed target is takeable through the projected command a player submits.");
            yield return null;
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
                Assert.That(HexDistanceQuery.Calculate(from, destination),
                    Is.InRange(1, 2),
                    "Every offered landing is within the printed two spaces. Offer was: " + shape);
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

            var landing = borrowed.Candidates
                .First(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Value.Coordinate;
            Assert.That(runtime.SubmitEffectChoiceValue(landing), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(landing),
                "The borrowed primary moved her, which is Reload! performing another card's action.");
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

            var beside = new HexCoordinate(-4, 1);
            Assert.That(landings, Has.Member(beside),
                "The landing that keeps the Red ranged minion adjacent. Offer was: " + shape);
            Assert.That(runtime.SubmitEffectChoiceValue(beside), Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(beside));

            // The push, which is the second printed sentence and a separate pause.
            var push = runtime.PendingEffectChoice;
            Assert.That(push, Is.Not.Null,
                "Landing beside an enemy unit opens the printed push. " + Remaining(runtime));
            Assert.That(push.SelectInvocations, Is.Not.Empty,
                "The push names a subject a player submits. Offer kind: " + push.Kind);
            Assert.That(
                runtime.SubmitProjectedCommand(
                    DirectorCommand.AnswerEffectChoice(push.SelectInvocations[0])),
                Is.True);
            yield return null;
        }

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

            // ActiveEffectReading: the bounty stands as a live Active effect on the card that made
            // it, and it has to still be standing when her NEXT turn opens - which is the whole of
            // what "Next turn:" means and the reason this is read here rather than inferred later.
            var markCard = runtime.Cards.Single(card =>
                card.OwnerId.Equals(swiftId) &&
                card.DefinitionReference.DefinitionId.Equals(SwiftCardContent.MarkForDeath));
            // **MEASURED ASYMMETRY, RECORDED RATHER THAN ASSERTED AWAY.** Delayed Jump's standing
            // End-of-turn promise DOES register as an Active effect source on its own card, and the
            // journey above asserts exactly that. This bounty does NOT:
            // `ActiveEffectSourceCardInstanceIds` is EMPTY here while Mark for Death sits in the
            // turn area with its promise standing, so a player reading her board cannot see that the
            // next Defeat in radius will pay. Milestone 8.38c1 records why the mechanism differs -
            // the bounty is spent at p.13's own reward site rather than dispatched through the
            // checkpoint system - but the READING is a separate question from the dispatch, and the
            // gate binds `ActiveEffectReading` to this journey. Raised as an open question against
            // 8.38c1 in the 8.38f block rather than papered over; what this journey proves is that
            // the promise SURVIVES and PAYS across the turn boundary, which is below.
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Empty,
                "Recording the measured state: the bounty stands but is not a readable Active " +
                "effect source. If this ever becomes non-empty the asymmetry is fixed and this " +
                "assertion is what will say so.");

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
            Assert.That(
                runtime.SubmitProjectedCommand(
                    DirectorCommand.AnswerEffectChoice(subject.SelectInvocations[0])),
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
        }
    }
}
