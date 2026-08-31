using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.ArienEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.ArienUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Arien's Tier II and Tier III cards, and his unlocked Ultimate, played through the real player
    /// pipeline. Milestone 8.20a.
    ///
    /// <para>The scene composes the staged Wasp-versus-Arien engagement at the requested p.15 stage -
    /// the position the page's own procedure produces, which a live match cannot be seeded to. p.15
    /// offers a pair at each Tier and one seat can hold only one of each, so the two branches are
    /// separate stagings and each journey asks for the one holding its card. Commitment through the
    /// real tray is proved by <see cref="ArienUiJourneyPlayModeTests"/>, so these commit headlessly
    /// and spend their clicks on the card under test.</para>
    /// </summary>
    public sealed class ArienProgressedUiJourneyPlayModeTests
    {
        // ---------------------------------------------------------------------------------------
        // Tier II
        // ---------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator RogueWave_PushesAnAdjacentEnemyAfterItsRangedAttackByItsOwnControls()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedArien);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitActingFirst(runtime, arienId, ArienCardContent.RogueWave);

            // "Target a unit in range" - the enemy hero stands two spaces away on the staged bent
            // line, which is inside the printed Range 2 and outside adjacency.
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Does.Contain(waspId),
                "The enemy hero must be reachable at the printed Range 2.");
            var waspView = HeroView(waspId);
            Assert.That(waspView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(Tile(waspView.Occupancy.Coordinate).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "and must carry blood-red corners where they stand before the click.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, waspView.transform.position), Is.True);
            yield return null;

            // p.14's Defense response is the defender's, and this journey's subject is what happens
            // after the attack - so it is declined on its own visible control.
            if (runtime.PendingDefenseChoice != null)
            {
                UiPlayer.Click(UiPlayer.Button(hud, "DECLINE DEFENSE"));
                yield return null;
            }

            // "After the attack: You may push an enemy unit adjacent to you up to 2 spaces."
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The after-attack push must stop the Attack. " + Remaining(runtime));
            var pushed = Object.FindObjectsByType<StandardMinionView>()
                .Where(view => view.Occupancy != null)
                .FirstOrDefault(view => runtime.RoundView.Model.EffectChoiceTargets
                    .Any(target => target.Space.Equals(view.Occupancy.Coordinate)));
            Assert.That(pushed, Is.Not.Null,
                "An enemy unit beside Arien must be offered on the board. " + Remaining(runtime));
            var pushedId = pushed.Occupancy.MinionId;
            var arienAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate;
            var distanceBefore = HexDistanceQuery.Calculate(
                MinionCoordinate(runtime, pushedId), arienAt);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, pushed.transform.position), Is.True,
                "The pushed unit is chosen where it stands.");
            yield return null;

            yield return AnswerEveryRemainingEffectControl(root);
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The push's own controls carry it to completion. " + Remaining(runtime));

            // p.18 resolves a push against the pre-push state and an occupied space is an obstacle,
            // so the exact distance belongs to the board rather than to the card. What the card
            // promises is that a push never brings its unit closer.
            Assert.That(
                HexDistanceQuery.Calculate(MinionCoordinate(runtime, pushedId), arienAt),
                Is.GreaterThanOrEqualTo(distanceBefore),
                "A push moves its unit away from Arien or not at all.");
        }

        [UnityTest]
        public IEnumerator SlipperyGround_AppliesItsTextBeforeMovingAndStopsTheEnemysFastTravel()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedArien);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitActingFirst(runtime, arienId, ArienCardContent.SlipperyGround);
            var slipperyCardId = runtime.SelectedActor.CardInstanceId;

            // p.11: "When you perform the Primary action on a Movement card, apply the card text in
            // addition to moving ... Unless it says otherwise, the card text comes into effect before
            // movement." So the lock is live before a single space is walked.
            yield return StartActionThroughControl(root, ActionKind.Movement);
            Assert.That(runtime.IsActiveEffectSource(slipperyCardId), Is.True,
                "The Movement card's text is applied before the walk, not after it.");
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "and the movement session is open in the same action.");

            // The printed lock reaches "enemy heroes adjacent to you", re-derived wherever Arien is
            // standing when it is asked - so the walk is what brings the enemy inside it.
            var waspAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(waspId)).Coordinate;
            var step = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => HexDistanceQuery.Calculate(value.Destination, waspAt) == 1)
                .OrderBy(value => value.Cost)
                .FirstOrDefault();
            Assert.That(step, Is.Not.Null,
                "The printed Movement 3 must reach a space beside the enemy hero.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(step.Destination).transform.position),
                Is.True);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate,
                Is.EqualTo(step.Destination));

            if (runtime.PendingMovementChoice != null)
            {
                UiPlayer.Click(
                    UiPlayer.Button(hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }

            if (runtime.SelectedActor.ActorId.Equals(arienId))
            {
                UiPlayer.Click(
                    UiPlayer.Button(hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }

            // The enemy's own activation, inside the same turn: what a player must be able to see is
            // that they are stopped. p.20 keeps the lock live for the whole turn.
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(waspId));
            Assert.That(runtime.IsActiveEffectSource(slipperyCardId), Is.True,
                "The `This turn` lock is still standing on the enemy's activation.");
            yield return PointTrayAt(root, waspId);
            yield return null;

            var enemyMovement = runtime.RoundView.Model.Actions.SingleOrDefault(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(enemyMovement, Is.Not.Null,
                "The card caps the enemy's Movement rather than forbidding it. Shown controls: " +
                UiPlayer.Describe(hud));
            UiPlayer.Click(UiPlayer.Button(hud, enemyMovement.Label));
            yield return null;

            Assert.That(runtime.PendingMovementChoice, Is.Not.Null);
            Assert.That(
                runtime.PendingMovementChoice.LegalDestinations
                    .All(value => value.Cost <= 1), Is.True,
                "\"cannot ... move more than 1 space with a movement action\" - every destination " +
                "the enemy is offered is one space away.");
            Assert.That(runtime.RoundView.Model.FastTravelOffers, Is.Empty,
                "and \"cannot fast travel\" leaves them no Fast Travel to take.");
        }

        [UnityTest]
        public IEnumerator ExpertDuelist_DefendsAndItsThisTurnImmunityIsReadOnTheTray()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoEngagedStagedArien);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);

            yield return CloseToAdjacency(root, arienId, waspId, ArienCardContent.ExpertDuelist);

            // The enemy attacks; Arien defends by clicking the whole Duelist face, which is p.14's
            // Primary Defense Value.
            yield return AttackArienWithTheEnemysAdjacentAttack(
                root, arienId, waspId, ArienCardContent.ExpertDuelist);
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The Attack must reach p.14's Defense response. " + Remaining(runtime));
            var duelistCardId = runtime.Cards.Single(card =>
                card.OwnerId.Equals(arienId) &&
                card.DefinitionReference.DefinitionId.Equals(ArienCardContent.ExpertDuelist)).Id;
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-defense-card", "EXPERT DUELIST"));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", "EXPERT DUELIST"));
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked face answers the response. " + Remaining(runtime));
            Assert.That(runtime.IsActiveEffectSource(duelistCardId), Is.True,
                "\"This turn: You are immune to attack actions of all enemy heroes, except this " +
                "attacker\" - the spent card is the live source a player can read.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(arienId)), Is.True,
                "and the printed Defense 6 answered the Attack it was spent on.");
        }

        [UnityTest]
        public IEnumerator ArcaneWhirlpool_SwapsWithTheClickedEnemyMinionThroughTheBoard()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedArien);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitActingFirst(runtime, arienId, ArienCardContent.ArcaneWhirlpool);
            var arienAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate;

            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Swap with an enemy minion in range\" asks which one. " + Remaining(runtime));

            var target = Object.FindObjectsByType<StandardMinionView>()
                .Where(view => view.Occupancy != null)
                .FirstOrDefault(view => runtime.RoundView.Model.EffectChoiceTargets
                    .Any(value => value.Space.Equals(view.Occupancy.Coordinate)));
            Assert.That(target, Is.Not.Null,
                "An enemy minion in the printed Range 4 must be marked on the board.");
            var targetId = target.Occupancy.MinionId;
            var targetAt = target.Occupancy.Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, target.transform.position), Is.True,
                "The swapped minion is chosen where it stands.");
            yield return null;

            yield return AnswerEveryRemainingEffectControl(root);
            Assert.That(runtime.PendingEffectChoice, Is.Null, "The swap asks nothing else.");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate,
                Is.EqualTo(targetAt),
                "Arien stands where the clicked minion was.");
            Assert.That(MinionCoordinate(runtime, targetId), Is.EqualTo(arienAt),
                "and the minion stands where Arien was - a swap, which p.19 separates from movement.");
        }

        // ---------------------------------------------------------------------------------------
        // Tier III
        // ---------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ViolentTorrent_RepeatsItsBeforeAttackTextOnADifferentUnitByItsRepeatControl()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedStagedArien);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitActingFirst(runtime, arienId, ArienCardContent.ViolentTorrent);

            yield return StartActionThroughControl(root, ActionKind.Attack);

            // At p.15's Tier III stage the Ultimate is already unlocked, and Living Tsunami's own
            // trigger stops the game before this Attack. It is declined on its control - the trigger
            // itself is LivingTsunami_IsDrawnOnTheRailAndOffersItsOneSpaceStepBeforeAnAttack's
            // subject, and what this journey is about is the repeat behind the card text.
            yield return DeclineAnyStandingTrigger(root);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The Attack asks for its target once the Ultimate's trigger is answered. " +
                Remaining(runtime));
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.First();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(targetId).transform.position),
                Is.True);
            yield return null;

            // The before-attack line runs first. "Up to 1 enemy hero" has a floor of zero, so taking
            // nobody is an answer with its own name - and a selection is told from the repeat behind
            // it by whether the frame is offering candidates at all.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The before-attack line must stop the Attack. " + Remaining(runtime));
            if (runtime.PendingEffectChoice.Candidates.Count > 0)
            {
                var declineLine = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
                Assert.That(declineLine, Is.Not.Null,
                    "\"Up to 1 enemy hero\" must be refusable by name. " + Remaining(runtime));
                UiPlayer.Click(UiPlayer.Button(hud, declineLine.Label));
                yield return null;
            }

            // "May repeat once on a different unit" - the printed decision, declined on its control.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The repeat is asked once the line has been answered. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.Candidates, Is.Empty,
                "and it is a decision rather than a selection. " + Remaining(runtime));
            var repeat = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            Assert.That(repeat, Is.Not.Null,
                "The repeat must be offered as a decision a player can refuse. " +
                Remaining(runtime));
            var commandsBefore = runtime.AcceptedCommands.Count;
            UiPlayer.Click(UiPlayer.Button(hud, repeat.Label));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsBefore + 1),
                "One click on the repeat's control is one command.");
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Declining the repeat ends the card's text after its one run. " + Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "and the Attack it was printed on completed.");
        }

        [UnityTest]
        public IEnumerator MasterDuelist_ImmunityIsStillReadOnALaterTurnOfTheSameRound()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedStagedArien);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var waspId = OtherHeroId(runtime, arienId);

            yield return CloseToAdjacency(root, arienId, waspId, ArienCardContent.MasterDuelist);
            yield return AttackArienWithTheEnemysAdjacentAttack(
                root, arienId, waspId, ArienCardContent.MasterDuelist);

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The Attack must reach p.14's Defense response. " + Remaining(runtime));
            var duelistCardId = runtime.Cards.Single(card =>
                card.OwnerId.Equals(arienId) &&
                card.DefinitionReference.DefinitionId.Equals(ArienCardContent.MasterDuelist)).Id;
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-defense-card", "MASTER DUELIST"));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", "MASTER DUELIST"));
            yield return null;
            Assert.That(runtime.IsActiveEffectSource(duelistCardId), Is.True);

            // The turn the card was spent on ends, and the next one begins inside the same round.
            // "This round" is what makes the reading survive it - p.20's other duration, and the
            // first in this build to outlive the turn that raised it.
            var turnBefore = runtime.TurnIndex;
            var roundBefore = runtime.RoundIndex;
            yield return PlayTheTurnOut(root);
            yield return AdvanceToNextTurn(runtime);

            Assert.That(runtime.RoundIndex, Is.EqualTo(roundBefore),
                "The journey stays inside the round the effect names.");
            Assert.That(runtime.TurnIndex, Is.GreaterThan(turnBefore),
                "and reaches a later turn of it.");
            Assert.That(runtime.IsActiveEffectSource(duelistCardId), Is.True,
                "Master Duelist's `This round` immunity is still standing on a turn that did not " +
                "raise it, where Expert Duelist's `This turn` would already be gone.");
        }

        [UnityTest]
        public IEnumerator EbbAndFlow_SwapsAndItsAdjacentRepeatIsDeclinedByItsOwnControl()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedArien);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitActingFirst(runtime, arienId, ArienCardContent.EbbAndFlow);
            var arienAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate;

            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);

            // "if it was adjacent to you, may repeat once" - so the clicked minion is an adjacent one,
            // which is what makes the repeat a decision rather than an absence.
            var adjacent = Object.FindObjectsByType<StandardMinionView>()
                .Where(view => view.Occupancy != null &&
                    HexDistanceQuery.Calculate(view.Occupancy.Coordinate, arienAt) == 1)
                .FirstOrDefault(view => runtime.RoundView.Model.EffectChoiceTargets
                    .Any(value => value.Space.Equals(view.Occupancy.Coordinate)));
            Assert.That(adjacent, Is.Not.Null,
                "An enemy minion beside Arien must be offered, or the repeat never arises. " +
                Remaining(runtime));
            var adjacentAt = adjacent.Occupancy.Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, adjacent.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate,
                Is.EqualTo(adjacentAt),
                "The swap happened where the click asked for it.");

            var decline = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            Assert.That(decline, Is.Not.Null,
                "The conditional repeat must be offered because the minion was adjacent. " +
                Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Declining ends the Skill after its one swap. " + Remaining(runtime));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate,
                Is.EqualTo(adjacentAt),
                "and Arien stays where the single swap left him.");
        }

        [UnityTest]
        public IEnumerator LivingTsunami_IsDrawnOnTheRailAndOffersItsOneSpaceStepBeforeAnAttack()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedStagedArien);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var arienId = ArienSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // p.16 turns the unlocked Ultimate faceup in the hero's area, and a player has to be able
            // to see it there before it is a thing they can use.
            yield return PointTrayAt(root, arienId);
            yield return null;
            var rail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(arienId));
            Assert.That(rail.UltimateCard, Is.Not.Null,
                "Arien's own rail must say he has unlocked an Ultimate.");
            Assert.That(rail.UltimateCard.DisplayName, Is.EqualTo("Living Tsunami"));
            Assert.That(rail.UltimateCard.HistoryLabel, Is.EqualTo("UNLOCKED"));
            var markers = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-ultimate-card")
                .ToArray();
            Assert.That(markers, Has.Length.EqualTo(2),
                "Both seats unlocked an Ultimate at this stage, and each rail draws its own marker.");

            // "Once per turn, before performing an Attack action, you may move 1 space." The trigger
            // stops the game before the Attack it is printed for.
            CommitActingFirst(runtime, arienId, ArienCardContent.ViolentTorrent);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Ultimate's before-Attack trigger must stop the game on its own question. " +
                Remaining(runtime));

            // "you may move 1 space" - the printed `may` is a decision, so refusing it has to be an
            // answer a player can give. It is asked of the projected command set rather than of the
            // drawn controls, so a decline that exists and is merely not drawn reads differently from
            // one that was never offered.
            var declineCommands = runtime.EnumerateProjectedCommands()
                .Where(command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Kind == EffectChoiceCommandKind.Decline)
                .ToArray();
            Assert.That(declineCommands, Is.Not.Empty,
                "Refusing the printed `may` must be an offered answer. Projected: [" +
                string.Join(", ", runtime.EnumerateProjectedCommands()
                    .Where(command => command.Kind == DirectorCommandKind.AnswerEffectChoice)
                    .Select(command => command.EffectChoice.Kind.ToString())) + "] " +
                Remaining(runtime));
            Assert.That(runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline), Is.True,
                "and it must be drawn as a control, not merely projected. " + Remaining(runtime));

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate;
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "The step's spaces are answered on the board. " + Remaining(runtime));
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(space, startedAt) == 1),
                Is.True,
                "and every offered space is the one the printed text allows.");
            // The Ultimate's step as a player meets it: four lit spaces on the board and a decline
            // beside them, where before this milestone it was four anonymous prompt options and no
            // way to refuse at all.
            yield return CaptureGameView("living-tsunami-step-and-its-decline");

            var stepTo = FirstClickableSpace(pointerInput, destinations);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(stepTo).transform.position), Is.True);
            yield return null;

            // A step a player may refuse is taken in two moves rather than one: the space is marked
            // by the click and spent by the confirm, so the click that names it is not also the one
            // that commits to moving at all. The decline stands beside it the whole time.
            var take = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(take, Is.Not.Null,
                "The marked space is spent by its own confirm. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, take.Label));
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate,
                Is.EqualTo(stepTo),
                $"Arien took the clicked step before his Attack; he started on {startedAt}, the " +
                $"offered spaces were [{string.Join(", ", destinations.Select(space => space.ToString()))}] " +
                $"and the click went to {stepTo}. " + Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "and the Attack the trigger stopped is now asking for its target. " +
                Remaining(runtime));
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Commits the named card for one hero and the other hand's lowest-Initiative card, so the
        /// named hero takes the turn's first activation. Which card the other hand spends is read off
        /// the hand rather than named: the two seats deal different printed sets and each p.15 stage
        /// deals a different five.
        /// </summary>
        private static void CommitActingFirst(
            ConfiguredMatchSceneRuntime runtime, HeroId actorId, CardDefinitionId cardId)
        {
            var definition = runtime.Definitions.Single(value => value.Id.Equals(cardId));
            var otherId = OtherHeroId(runtime, actorId);
            var slower = HandOf(runtime, otherId)
                .Where(value => value.Initiative.IsDefined &&
                    value.Initiative.Value < definition.Initiative.Value)
                .OrderBy(value => value.Initiative.Value)
                .ToArray();
            Assert.That(slower, Is.Not.Empty,
                $"{otherId.Value} holds no card slower than {definition.DisplayName}'s printed " +
                $"Initiative {definition.Initiative.Value}, so this journey cannot give Arien the " +
                "first activation.");

            Commit(runtime, actorId, cardId);
            Commit(runtime, otherId, slower[0].Id);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(actorId),
                $"{definition.DisplayName} must take the turn's first activation.");
        }

        /// <summary>
        /// Commits so the enemy takes the turn's first activation and Arien keeps the card a later
        /// step needs. Which two cards that is belongs to the staged hands rather than to the journey:
        /// p.15 deals a different five at each stage, and a hand spends a card every turn, so the pair
        /// is searched for rather than named. The fastest qualifying enemy card wins, so the enemy
        /// acts as early in the turn as the hands allow.
        /// </summary>
        private static void CommitEnemyActingFirst(
            ConfiguredMatchSceneRuntime runtime,
            HeroId enemyId,
            HeroId arienId,
            CardDefinitionId keepInAriensHand,
            System.Func<CardDefinition, bool> enemyCardQualifies)
        {
            var ariensSpendable = HandOf(runtime, arienId)
                .Where(value => !value.Id.Equals(keepInAriensHand) && value.Initiative.IsDefined)
                .ToArray();
            var pairs = HandOf(runtime, enemyId)
                .Where(value => value.Initiative.IsDefined && enemyCardQualifies(value))
                // A cross-team tie is decided by the Tie Breaker side, which `DEV-005` fixes to Red -
                // the enemy's own side in this staging - so an equal Initiative still gives them the
                // first activation. The assertion below is what holds that to being true rather than
                // assumed.
                .SelectMany(enemyCard => ariensSpendable
                    .Where(ariensCard => ariensCard.Initiative.Value <= enemyCard.Initiative.Value)
                    .Select(ariensCard => (enemyCard, ariensCard)))
                .OrderByDescending(pair => pair.enemyCard.Initiative.Value)
                .ThenBy(pair => pair.ariensCard.Initiative.Value)
                .ToArray();
            Assert.That(pairs, Is.Not.Empty,
                "No pair of held cards gives the enemy the first activation while Arien keeps " +
                keepInAriensHand.Value + ". Enemy holds " +
                string.Join(", ", HandOf(runtime, enemyId)
                    .Select(value => $"{value.DisplayName}@{value.Initiative.Value}")) +
                "; Arien holds " + string.Join(", ", HandOf(runtime, arienId)
                    .Select(value => $"{value.DisplayName}@{value.Initiative.Value}")));

            Commit(runtime, enemyId, pairs[0].enemyCard.Id);
            Commit(runtime, arienId, pairs[0].ariensCard.Id);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                $"{pairs[0].enemyCard.DisplayName} must act before {pairs[0].ariensCard.DisplayName}.");
        }

        /// <summary>
        /// Spends one turn walking the enemy into contact with Arien, headlessly - the approach march
        /// is the 8.11e regression's subject, not this fixture's. Arien holds, keeping his hand for
        /// the Defense the journey is about.
        /// </summary>
        private static IEnumerator CloseToAdjacency(
            MatchSceneCompositionRoot root, HeroId arienId, HeroId waspId,
            CardDefinitionId keepInAriensHand)
        {
            var runtime = root.Runtime;
            CommitEnemyActingFirst(
                runtime, waspId, arienId, keepInAriensHand,
                definition => definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Movement));

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

            Assert.That(runtime.SubmitEndTurn(), Is.True, "Arien holds the approach turn.");
            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>
        /// The enemy's own adjacent Attack on Arien, headlessly up to the moment the defender is
        /// asked - which is where each Defense journey's own clicks begin.
        /// </summary>
        private static IEnumerator AttackArienWithTheEnemysAdjacentAttack(
            MatchSceneCompositionRoot root, HeroId arienId, HeroId waspId,
            CardDefinitionId keepInAriensHand)
        {
            var runtime = root.Runtime;
            CommitEnemyActingFirst(
                runtime, waspId, arienId, keepInAriensHand,
                definition => definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Attack &&
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary));

            var attack = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Does.Contain(arienId),
                "The adjacent Arien must be a legal target of the enemy's printed Attack.");
            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(
                runtime.SubmitProjectedCommand(runtime.EnumerateProjectedCommands().Single(command =>
                    command.Kind == DirectorCommandKind.ChooseAttackTarget &&
                    command.HeroId.Equals(arienId))),
                Is.True,
                "and choosing him must be accepted. " + choice.ChoiceId.Value);
            yield return null;
        }

        /// <summary>Plays whatever is left of the current turn out, headlessly.</summary>
        private static IEnumerator PlayTheTurnOut(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            for (var budget = 0; runtime.IsTurnActive && budget < 12; budget++)
            {
                if (!runtime.SubmitEndTurn())
                {
                    break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Answers every control a paused frame still offers, by clicking it, until nothing is
        /// paused. A confirm is preferred over a decline so a batch runs rather than being refused.
        /// </summary>
        private static IEnumerator AnswerEveryRemainingEffectControl(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            for (var budget = 0; runtime.PendingEffectChoice != null && budget < 6; budget++)
            {
                var choices = runtime.RoundView.Model.EffectChoices;

                // A printed distance range - "up to 2 spaces" - reaches the player as one control per
                // number, which is the Integer arm milestone 8.15 named after finding them drawn as
                // anonymous ordinals. The furthest is taken, so the push is a push.
                var selects = choices
                    .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                    .ToArray();
                var next = selects.Length > 0
                    ? selects[selects.Length - 1]
                    : choices.FirstOrDefault(value =>
                        value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm) ??
                      choices.FirstOrDefault(value =>
                        value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
                if (next == null)
                {
                    yield break;
                }

                UiPlayer.Click(UiPlayer.Button(root.Hud, next.Label));
                yield return null;
            }
        }

        /// <summary>
        /// Declines whatever trigger is standing before the action a journey is about, on its own
        /// control. A frame with nothing standing is left alone.
        /// </summary>
        private static IEnumerator DeclineAnyStandingTrigger(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            if (runtime.PendingEffectChoice == null)
            {
                yield break;
            }

            var decline = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            if (decline != null)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, decline.Label));
                yield return null;
                yield break;
            }

            // A trigger whose whole answer is a space is answered on the board - the Ultimate's own
            // journey owns that shape; here it is only the step in front of the card under test.
            var spaces = LitDestinations(runtime);
            Assert.That(spaces, Is.Not.Empty,
                "A trigger standing before the action must be answerable. " + Remaining(runtime));
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointer, root.Hud, Tile(spaces[0]).transform.position), Is.True);
            yield return null;
        }

        private static HexCoordinate MinionCoordinate(
            ConfiguredMatchSceneRuntime runtime, MinionId minionId) =>
            runtime.BoardView.Model.Minions
                .Single(minion => minion.MinionId.Equals(minionId)).Coordinate;

        private static IEnumerator LoadStage(SceneCardContent stage)
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent = stage;
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
