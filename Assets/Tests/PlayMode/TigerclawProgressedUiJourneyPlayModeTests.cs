using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.TigerclawEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TigerclawUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Tigerclaw's upgraded cards played through the real surface, from rulebook p.15's staged Tier II
    /// and Tier III positions. Milestone 8.23, on the `DEV-004` terms milestone 8.15 recorded: coins
    /// are earned only through p.13's Defeat, so the rounds a live progression needs are outside a
    /// journey's budget and these fixtures start from the position the page's own procedure produces.
    ///
    /// <para>The branch each staging takes is p.15's choice, not a variant: a seat holds three of a
    /// Tier's six cards and the sibling it did not take becomes p.16's item. The second-sibling arms
    /// are the half Backstab, Parry, Poisoned Dagger, Backstab with a Ballista, Riposte and Poisoned
    /// Dart are on.</para>
    ///
    /// <para><b>Each journey pays for its own board.</b> His Blue kit answers an enemy Attack and his
    /// Green kit needs an enemy hero within reach, so most of these spend a turn walking into the
    /// position the printed sentence is about - and every one of those setup turns rides the headless
    /// command surface, because the walk is
    /// <see cref="TigerclawUiJourneyPlayModeTests.Tigerclaw_CommitsFromHisOwnTrayAndWalksHisSecondaryMovementToAClickedSpace"/>'s
    /// subject rather than theirs.</para>
    /// </summary>
    public sealed class TigerclawProgressedUiJourneyPlayModeTests
    {

        /// <summary>
        /// Combat Reflexes: "Before the attack: You may move 1 space. Target a unit adjacent to you.
        /// After the attack: If you did not move before the attack, you may move 1 space."
        ///
        /// <para>One face asking twice, six numbered steps apart, with the target chosen and the
        /// attack resolved in between. Declining the first is what opens the second - and a player
        /// gives that decline by name, on its own control, which is the answer milestone 8.20a made
        /// something a printed `may` has to offer.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CombatReflexes_DeclinesItsFirstMoveAndTakesTheSecondAfterTheAttack()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.CombatReflexes);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId));
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate;

            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "p.14 names the target before the page's own step 1 asks anything. " +
                Remaining(runtime));
            var targets = runtime.PendingAttackTargetChoice.LegalTargetMinionIds;
            Assert.That(targets, Is.Not.Empty);
            var targetView = MinionView(targets[0]);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, targetView.transform.position), Is.True);
            yield return null;
            yield return null;

            // Step 1: the printed `you may`, declined by name.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Before the attack: You may move 1 space.\" " + Remaining(runtime));
            var decline = DeclineControl(runtime);
            Assert.That(decline, Is.Not.Null,
                "A printed `may` has to offer a decline a player can press. Shown: " +
                UiPlayer.Describe(hud));
            UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate,
                Is.EqualTo(startedAt),
                "The declined step moves nobody.");

            // Step 7: the second move, open because the first was declined.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"If you did not move before the attack, you may move 1 space\" - he did not. " +
                Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty);
            var step = StandingNode(runtime);
            var chosen = FirstClickableSpace(pointerInput, destinations);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, step);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate,
                Is.EqualTo(chosen),
                "and the second move is the one that moved him.");
            Assert.That(HexDistanceQuery.Calculate(chosen, startedAt), Is.EqualTo(1));
            Assert.That(HeroView(tigerclawId).Coordinate, Is.EqualTo(chosen));

            yield return CaptureGameView("combat-reflexes-asks-again-after-the-attack");
        }

        /// <summary>
        /// Poisoned Dagger: "Give a hero in range a Poison marker."
        ///
        /// <para>One board click on the hero standing in the printed disk, and p.10 keeps him out of
        /// his own action - so the only candidate is the enemy. <b>What the marker then does is
        /// authoritative and invisible.</b> This build draws no held marker anywhere, which milestone
        /// 8.23's own gate row records as a debt and milestone 8.23a is scheduled to close; what the
        /// attachment costs its holder is pinned by
        /// <c>TigerclawPrintedSetTests.PoisonedDagger_GivesTheMarker_AndTheHolderLosesThreeValues</c>.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PoisonedDagger_GivesThePoisonMarkerToTheHeroItsBoardClickNames()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.PoisonedDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, tigerclawId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId),
                "Poisoned Dagger's printed Initiative 2 is the lowest in his hand, so his turn " +
                "follows the enemy's inside the same round. " + Remaining(runtime));

            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"a hero in range\" is a question. " + Remaining(runtime));
            var offered = runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space)
                .ToArray();
            Assert.That(offered, Has.Length.EqualTo(1),
                "One other hero is on this board and p.10 keeps him out of his own action, so one " +
                "piece is offered. " + Remaining(runtime));
            Assert.That(offered[0],
                Is.EqualTo(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate),
                "and the piece offered is the enemy hero rather than himself.");

            var target = StandingNode(runtime);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The hero who receives the marker is named by clicking them.");
            yield return null;
            yield return ConfirmIfStillStanding(root, target);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The answered selection completes the action rather than asking again. " +
                Remaining(runtime));
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.True,
                "A marker is given rather than a hero removed.");
        }

        /// <summary>
        /// Parry: "Block a non-ranged attack. The attacker discards a card, if able."
        ///
        /// <para>Two things a player does, and the second is the first printed rider in this build
        /// that asks the hero on the OTHER side of p.14's moment: the defender answers with a whole
        /// Block face, and the attacker then picks which of their own cards to lose.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Parry_BlocksTheAttackItIsPrintedAgainstAndItsRiderTakesTheAttackersCard()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);

            yield return TakeTheEnemysAttack(
                root, tigerclawId, enemyId, TigerclawCardContent.PoisonedDagger,
                TigerclawCardContent.BlendIntoShadows, WaspCardContent.Electrocute);

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            var attackersHand = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);
            var faceName = NameOf(runtime, TigerclawCardContent.Parry);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"The attacker discards a card, if able\" is p.14's step 4 and it asks the " +
                "attacker. " + Remaining(runtime));
            var faces = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(faces, Is.Not.Empty,
                "The attacker's own cards have to be drawn as faces they can press. Shown: " +
                UiPlayer.Describe(hud));
            // Milestone 8.15a: a card-valued choice is a WHOLE FACE named `gd-effect-card`, so it is
            // clicked as the face rather than as a labelled button.
            var discard = StandingNode(runtime);
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-effect-card", faces[0].Label));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faces[0].Label));
            yield return null;
            yield return ConfirmIfStillStanding(root, discard);
            yield return null;

            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(attackersHand - 1),
                "The card leaves the ATTACKER's hand, not the defender's.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(tigerclawId)), Is.True,
                "and the Block succeeded, because the attack it answered is not a ranged one and " +
                "Parry is printed against exactly that.");

            yield return CaptureGameView("parry-takes-the-attackers-card");
        }

        /// <summary>
        /// Sidestep: "Block a ranged attack. You may move 1 space."
        ///
        /// <para>The rider is the defender's own step, taken at p.14's step 4 - before the page has
        /// compared anything - so a player takes it and then meets the comparison from somewhere
        /// else. He is defeated all the same, and that is the content rather than a surprise: no
        /// staged enemy hand holds a ranged Attack, and a Block whose printed condition is not met
        /// removes the hero who answered with it however far they stepped.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Sidestep_StepsOneSpaceOnItsOwnDefenseRiderAfterTheBlockIsAnswered()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // The setup walk rides his Green card, which is the one this staging deals that is
            // neither the Block being answered with nor the card committed for the turn it answers.
            yield return TakeTheEnemysAttack(
                root, tigerclawId, enemyId, TigerclawCardContent.PickPocket,
                TigerclawCardContent.BlendIntoShadows, WaspCardContent.Electrocute);

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate;
            var faceName = NameOf(runtime, TigerclawCardContent.Sidestep);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"You may move 1 space\" is the defender's own step at p.14's step 4. " +
                Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty);
            var step = StandingNode(runtime);
            var chosen = FirstClickableSpace(pointerInput, destinations);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, step);
            yield return null;
            yield return null;

            Assert.That(chosen, Is.Not.EqualTo(stoodAt),
                "He took the step the rider offered.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(tigerclawId)), Is.False,
                "and p.14 still reached its own step 6: a Block against an attack its condition " +
                "does not name removes the hero who answered with it, wherever they moved to.");
        }

        /// <summary>
        /// Evade: "Block a ranged attack. You may move 1 space. You may retrieve your resolved or
        /// discarded basic skill card."
        ///
        /// <para>Three printed steps on one Defense, and the third is a card taken back off the table
        /// - which is why this journey spends a turn resolving his basic Skill before the Attack
        /// arrives: a retrieve with nothing to retrieve is a step p.10 skips, and the point of the
        /// journey is the face a player picks.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Evade_SpendsACardAndThenStepsOnItsOwnDefenseRider()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Turn one walks him into contact; turn two resolves his basic Skill so the retrieve has
            // a card to be about. Both ride the headless surface - they are setup.
            // Both setup turns name the enemy's card as well as his, because committing their lowest
            // every turn spends their hand in ascending order and takes the Attack turn three needs.
            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            yield return WalkToward(
                root, tigerclawId, enemyId, TigerclawCardContent.LeapingStrike,
                WaspCardContent.StaticBarrier, enemyAt);
            yield return ResolveHisBasicSkill(
                root, tigerclawId, enemyId, WaspCardContent.MagneticDagger);

            Commit(runtime, enemyId, WaspCardContent.Electroblast);
            Commit(runtime, tigerclawId, TigerclawCardContent.MasterThief);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Electroblast's 9 takes the turn over Master Thief's 1, which is what keeps Evade " +
                "in his hand to answer with.");
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds,
                Does.Contain(tigerclawId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(tigerclawId)),
                Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            var handBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(tigerclawId) && card.State.Location == CardLocation.Hand);
            var faceName = NameOf(runtime, TigerclawCardContent.Evade);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            yield return null;

            // The step first, then the retrieve: the card prints them in that order.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty, "\"You may move 1 space.\" " + Remaining(runtime));
            var step = StandingNode(runtime);
            var chosen = FirstClickableSpace(pointerInput, destinations);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, step);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"You may retrieve your resolved or discarded basic skill card\" is the third " +
                "step, and the resolved Blend Into Shadows is what it is about. " + Remaining(runtime));
            var faces = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(faces, Is.Not.Empty,
                "The card he may take back has to be drawn as a face he can press. Shown: " +
                UiPlayer.Describe(hud));
            var retrieve = StandingNode(runtime);
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-effect-card", faces[0].Label));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faces[0].Label));
            yield return null;
            yield return ConfirmIfStillStanding(root, retrieve);
            yield return null;
            yield return null;

            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(tigerclawId) && card.State.Location == CardLocation.Hand),
                Is.GreaterThan(handBefore - 1),
                "The retrieved card came back into his hand, so answering the Defense cost him one " +
                "card and gave one back.");
        }

        /// <summary>
        /// Backstab with a Ballista: "Target a unit in range; If a friendly unit is adjacent to the
        /// target +2 Attack and the target cannot perform a primary action to defend."
        ///
        /// <para>His only face printing a Range icon, and the only one whose target is chosen at a
        /// printed reach rather than by adjacency. What a player does is one board click on a piece
        /// inside that reach; the two clauses behind the semicolon are a value and a restriction on
        /// the answer, and neither asks anybody anything.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BackstabWithABallista_ShootsTheEnemyMinionAtItsPrintedRange()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(
                runtime, tigerclawId, TigerclawCardContent.BackstabWithABallista);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId));

            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The clicked Attack control must open p.14's target question. " + Remaining(runtime));
            var targets = runtime.PendingAttackTargetChoice.LegalTargetMinionIds;
            Assert.That(targets, Is.Not.Empty,
                "A piece has to stand inside the printed Range for the shot to be about anybody.");
            var targetView = MinionView(targets[0]);
            var targetId = targetView.Occupancy.MinionId;
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, targetView.transform.position), Is.True,
                "The target is chosen by clicking the piece where it stands.");
            yield return null;
            yield return null;

            Assert.That(
                Object.FindObjectsByType<StandardMinionView>().Any(view =>
                    view.Occupancy != null && view.Occupancy.MinionId.Equals(targetId)),
                Is.False,
                "The shot resolved against the piece it named, and p.14's step 6 removed it.");
        }

        /// <summary>
        /// Cloak and Daggers: "After you perform a basic action you may repeat it once; if you repeat
        /// an attack action, you cannot target the same unit."
        ///
        /// <para>p.16's unlocked Ultimate is drawn where a player can see it - on his own rail, not in
        /// the tray - and it stops the game after the basic action it watched, with both answers
        /// standing. <b>Both had to be able to be given</b>: the decision is raised after its own
        /// action has ended, so Initiative had already moved past it and every answer to it was being
        /// rolled back, which is the defect this milestone's guard closes.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CloakAndDaggers_IsDrawnOnTigerclawsRailAndRepeatsTheBasicActionHeJustTook()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return PointTrayAt(root, tigerclawId);
            yield return null;
            var ultimate = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(tigerclawId))
                .UltimateCard;
            Assert.That(ultimate, Is.Not.Null,
                "p.16 puts the unlocked Ultimate faceup in the hero's own area, so his rail has to " +
                "be drawing it before it is a thing he can use.");
            Assert.That(UiPlayer.Descendants(hud.RootElement)
                    .Any(element => element.name == "gd-ultimate-card"), Is.True,
                "and it is drawn on the rail rather than dealt into the tray. Shown: " +
                UiPlayer.Describe(hud));

            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.BlinkStrike);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId));
            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var line = StandingNode(runtime);
            var offered = MinionsAt(
                runtime.RoundView.Model.EffectChoiceTargets.Select(candidate => candidate.Space));
            Assert.That(offered, Is.Not.Empty, Remaining(runtime));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, offered[0].transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, line);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The performed basic action is what the Ultimate watches, so the game stops on its " +
                "printed offer. " + Remaining(runtime));
            var repeat = StandingNode(runtime);
            var decline = DeclineControl(runtime);
            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(decline, Is.Not.Null,
                "\"you may repeat it once\" - the decline is an answer a player has to be able to " +
                "give by name. Shown: " + UiPlayer.Describe(hud));
            Assert.That(confirm, Is.Not.Null,
                "and taking it is the other. Shown: " + UiPlayer.Describe(hud));

            UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The answered decision closes rather than being rolled back under the player. " +
                Remaining(runtime));

            yield return CaptureGameView("cloak-and-daggers-offers-its-repeat");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Spends a turn walking him to a space beside the named coordinate, headlessly. It is setup:
        /// the walk itself is the tray journey's subject.
        /// </summary>
        private static IEnumerator WalkToward(
            MatchSceneCompositionRoot root,
            HeroId tigerclawId,
            HeroId enemyId,
            CardDefinitionId walkOn,
            CardDefinitionId theyCommit,
            HexCoordinate destination)
        {
            var runtime = root.Runtime;
            CommitBoth(runtime, tigerclawId, walkOn, enemyId, theyCommit);
            yield return YieldUntilTheNamedSeatActs(runtime, tigerclawId);
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var step = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => HexDistanceQuery.Calculate(value.Destination, destination) == 1)
                .ToArray();
            Assert.That(step, Is.Not.Empty,
                "No step of his stands beside " + destination + " on this staging.");
            Assert.That(runtime.BoardPresenter.SubmitDestination(step[0].Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>
        /// Spends a turn performing his basic Skill so a later retrieve has a resolved card to name.
        /// Headless: what the Skill itself does is
        /// <see cref="TigerclawEngagedUiJourneyPlayModeTests.BlendIntoShadows_PlacesHimInsideItsRadiusAndDrawsTheActiveEffectItRaises"/>'s
        /// subject, and here it is only the fact of having been played.
        /// </summary>
        private static IEnumerator ResolveHisBasicSkill(
            MatchSceneCompositionRoot root,
            HeroId tigerclawId,
            HeroId enemyId,
            CardDefinitionId theyCommit)
        {
            var runtime = root.Runtime;
            CommitBoth(
                runtime, tigerclawId, TigerclawCardContent.BlendIntoShadows, enemyId, theyCommit);
            yield return YieldUntilTheNamedSeatActs(runtime, tigerclawId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            for (var budget = 0; runtime.PendingEffectChoice != null && budget < 8; budget++)
            {
                var standing = runtime.RoundView.Model.EffectChoices.FirstOrDefault();
                Assert.That(standing, Is.Not.Null, Remaining(runtime));
                Assert.That(runtime.RoundPresenter.Submit(standing), Is.True);
                yield return null;
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);
        }
    }
}
