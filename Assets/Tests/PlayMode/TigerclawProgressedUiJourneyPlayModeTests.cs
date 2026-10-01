using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Participants;
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
        ///
        /// <para><b>Milestone 8.35z25 re-drove this in the printed order.</b> It used to
        /// answer p.14's ordinary target question first and then the move, which is the ordering that
        /// milestone corrected: the face prints the move before the targeting, so the move is now the
        /// first thing a player is asked and the target is named afterwards, by a board click on the
        /// unit - the same card-text unit selection Light Fingered's journey drives.</para>
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

            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "The face prints its move before its targeting, so p.14's ordinary target question " +
                "is not what opens this Attack. " + Remaining(runtime));

            // Step 1, first sentence: the printed `you may`, declined by name.
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

            // Step 1, second sentence: "Target a unit adjacent to you.", named by a board click on
            // the unit itself - the card-text unit selection rather than p.14's own question.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Target a unit adjacent to you.\" follows the move. " + Remaining(runtime));
            var beside = runtime.BoardView.Model.Minions
                .Where(minion => HexDistanceQuery.Calculate(minion.Coordinate, startedAt) == 1)
                .ToArray();
            Assert.That(beside, Is.Not.Empty,
                "The staged board has to leave a unit beside him for the printed targeting to have " +
                "anything to name. " + Remaining(runtime));
            var victim = StandingNode(runtime);
            // Milestone 9.10 orders the projection's minions by id, so "the first beside him" is no
            // longer the one the staging happened to place first: name one the selection offers.
            var offeredBeside = beside.Where(minion => runtime.PendingEffectChoice.Candidates
                .Any(candidate => candidate.Value.MinionId.Equals(minion.MinionId))).ToArray();
            Assert.That(offeredBeside, Is.Not.Empty,
                "The printed targeting offers a unit beside him. " + Remaining(runtime));
            var takenId = offeredBeside[0].MinionId;
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, hud, MinionView(takenId).transform.position),
                Is.True,
                "The unit this Attack takes is named by clicking it.");
            yield return null;
            yield return ConfirmIfStillStanding(root, victim);
            yield return null;

            // MILESTONE 8.86. What the Attack DID. This journey named the unit and went straight on to
            // the second move, so a targeting that named the right piece and struck nobody would have
            // read the same. p.13: a targeted enemy minion is simply defeated.
            Assert.That(
                Object.FindObjectsByType<StandardMinionView>()
                    .Any(view => view.Occupancy != null && view.Occupancy.MinionId.Equals(takenId)),
                Is.False,
                "\"Target a unit adjacent to you\" - the piece the board click named was defeated " +
                "and removed. " + Remaining(runtime));

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
        /// his own action - so the only candidate is the enemy. This journey owns the click reaching
        /// the rules and stops there; milestone 8.23a added
        /// <see cref="APoisonedHero_ReadsTheMarkerAndItsThreeLostValuesOnTheirOwnRail"/> for what the
        /// click then puts on the screen, which was the debt milestone 8.23's gate row recorded. What
        /// the attachment costs its holder in the rules is pinned by
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

            // MILESTONE 8.86. THE MARKER IS READ. Everything above this was about the OFFER and the
            // click; the one thing the printed sentence promises - "give a hero in range a Poison
            // marker" - was left for the sibling journey to notice, so a click that named the right
            // hero and gave them nothing would have passed here. He is Blue and the hero he named is
            // Red, and p.18 gives the marker to its HOLDER, so both rails are read and only one of
            // them may be carrying it.
            yield return null;
            HeroRailMarkerCheck.AssertReadings(
                hud, TeamSide.Red, "the answered Poisoned Dagger",
                HeroRailMarkerCheck.PoisonOneReading);
            HeroRailMarkerCheck.AssertReadings(
                hud, TeamSide.Blue, "the answered Poisoned Dagger");
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
            // MILESTONE 8.86. "He took the step the rider offered" was asserted of the space that was
            // CLICKED rather than of the board, and it had to be: p.14's step 6 removes him a moment
            // later, so there is no surviving piece to read a coordinate off afterwards. What can be
            // read is the offer, and the claim worth making of it is that NO answer to this rider is
            // a stand-still - so whichever space is pressed, the rider really moved him.
            Assert.That(destinations, Has.No.Member(stoodAt),
                "\"You may move 1 space\" offers no space he is already standing on, so every answer " +
                "to it is a step. Offered: " +
                string.Join(", ", destinations.Select(space => space.ToString())) + ". " +
                Remaining(runtime));
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
            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate;
            var destinations = LitDestinations(runtime)
                .Where(space => !space.Equals(stoodAt))
                .ToArray();
            Assert.That(destinations, Is.Not.Empty, "\"You may move 1 space.\" " + Remaining(runtime));
            var step = StandingNode(runtime);
            var chosen = FirstClickableSpace(pointerInput, destinations);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, step);
            yield return null;

            // MILESTONE 8.86. The step this journey took was never read off the board at all - it
            // clicked a space and moved straight on to the retrieve, so a rider that offered a step
            // and refused to take it would have passed. Unlike Sidestep's, Evade's Block is printed
            // against an attack this one IS, so he survives to be read.
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate,
                Is.EqualTo(chosen),
                "He is standing on the space the rider's step was clicked on. " + Remaining(runtime));
            Assert.That(HexDistanceQuery.Calculate(chosen, stoodAt), Is.EqualTo(1),
                "one space, which is the printed count and not an \"up to\".");
            Assert.That(HeroView(tigerclawId).Coordinate, Is.EqualTo(chosen),
                "with his token drawn there rather than where the Attack found him.");

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
            // MILESTONE 8.86. The face pressed is NAMED, so the card the assertion below is about is
            // the card the player chose rather than "whatever the hand count says".
            var takenBackId = faces[0].Command.EffectChoice.Candidate.Value.CardInstanceId;
            var takenBack = runtime.Cards.Single(card => card.Id.Equals(takenBackId));
            Assert.That(takenBack.OwnerId, Is.EqualTo(tigerclawId),
                "\"your resolved or discarded basic skill card\" - the card offered is HIS.");
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-effect-card", faces[0].Label));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faces[0].Label));
            yield return null;
            yield return ConfirmIfStillStanding(root, retrieve);
            yield return null;
            yield return null;

            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(takenBack.Id)).State.Location,
                Is.EqualTo(CardLocation.Hand),
                "THAT card is back in his hand, which is what \"retrieve\" means - a count that went " +
                "up could have been any card arriving from anywhere. " + Remaining(runtime));
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(tigerclawId) && card.State.Location == CardLocation.Hand),
                Is.GreaterThanOrEqualTo(handBefore),
                "so answering the Defense cost him one card and the retrieve gave one back.");
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
            Assert.That(UiPlayer.ItemStripDescendants(hud)
                    .Any(element => element.name == "gd-ultimate-card"), Is.True,
                "and it is drawn on the rail rather than dealt into the tray. Shown: " +
                UiPlayer.Describe(hud));

            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.BlinkStrike);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId));
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate;
            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var line = StandingNode(runtime);
            var offered = MinionsAt(
                runtime.RoundView.Model.EffectChoiceTargets.Select(candidate => candidate.Space));
            Assert.That(offered, Is.Not.Empty, Remaining(runtime));
            var crossedId = offered[0].Occupancy.MinionId;
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, offered[0].transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, line);
            yield return null;
            yield return null;

            // MILESTONE 8.86. "The performed basic action" is the premise of everything below, and
            // this journey took it on trust: it clicked the line and read straight on to the repeat
            // offer. Blink Strike's own journey proves the whole sentence; what is asserted here is
            // that the action the Ultimate is watching really happened.
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate,
                Is.Not.EqualTo(startedAt),
                "\"Move 2 spaces in a straight line through an enemy unit\" - the basic action moved " +
                "him. " + Remaining(runtime));
            Assert.That(
                Object.FindObjectsByType<StandardMinionView>().Any(view =>
                    view.Occupancy != null && view.Occupancy.MinionId.Equals(crossedId)),
                Is.False,
                "and \"target that unit\" took the piece he crossed off the board, which is the " +
                "basic ATTACK action the Ultimate's offer is about. " + Remaining(runtime));

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The performed basic action is what the Ultimate watches, so the game stops on its " +
                "printed offer. " + Remaining(runtime));
            var repeat = StandingNode(runtime);
            // Milestone 8.46e: both answers to the printed `may` are the decision window's controls
            // now, so they are read off its model by command kind - what is SHOWN is the window's
            // pair, labelled for their consequences, not the strip's `ACCEPT` and `DECLINE`.
            var decline = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Decline);
            var confirm = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Confirm);
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

        /// <summary>
        /// The other answer, taken - and what taking it does.
        ///
        /// <para>Milestone 8.25a. The journey above proves the decision is drawn and that declining is
        /// accepted; until that milestone it could prove nothing more, because taking the repeat
        /// settled an instance nothing ever started, so both answers looked alike from the surface.
        /// Rulebook p.10 makes the repeat a separate action performed on the same card by the same
        /// hero, so what a player sees after confirming is that card's own action asked again with the
        /// turn still theirs.</para>
        ///
        /// <para><b>The repeated action here is a Movement, not the Attack the journey above takes.</b>
        /// That is the shape milestone 8.25a's six-player evidence stalled on - a
        /// <c>secondary-movement</c> repeat surfacing in another hero's turn - and it is the one that
        /// reliably has somewhere to go: Blink Strike's line names the unit it crossed and the card
        /// forbids repeating onto the same one, so on this staging that Attack's repeat correctly finds
        /// nothing and skips in silence, which proves the turn was held but not that the second
        /// instance runs.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CloakAndDaggers_TakenRepeatRunsHisBasicActionAgainOnTheSameCard()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.BlinkStrike);
            var actingCard = runtime.SelectedActor.CardInstanceId;
            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Movement);

            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "The clicked control opens p.10's movement session. " + Remaining(runtime));
            yield return StepOntoTheFirstLitSpace(root, pointerInput);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The performed basic action is what the Ultimate watches, so the game stops on its " +
                "printed offer once the Movement is spent. " + Remaining(runtime));
            // Milestone 8.46e: the accept is the decision window's control, labelled for what taking
            // the repeat does, so it is read off the window's model by command kind and pressed by
            // that label rather than by the strip's.
            var confirm = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Confirm);
            Assert.That(confirm, Is.Not.Null,
                "\"you may repeat it once\" - taking it is an answer a player has to be able to give " +
                "by name. Shown: " + UiPlayer.Describe(hud));

            UiPlayer.Click(UiPlayer.Button(hud, confirm.Label));
            yield return null;
            yield return null;

            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId),
                "p.10's repeated action is performed by the same hero, so the turn is still his - " +
                "and before milestone 8.25a Initiative had already left him by this point. " +
                Remaining(runtime));
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.EqualTo(actingCard),
                "and it is still the card he took the action on, which is why that card had to stay " +
                "unresolved across the decision.");
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "Taking the repeat starts the second instance rather than owing one, so the Movement " +
                "is asked again and the board lights for it. Shown: " + UiPlayer.Describe(hud) + " " +
                Remaining(runtime));

            var walkedTo = runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate;
            yield return StepOntoTheFirstLitSpace(root, pointerInput);
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate,
                Is.Not.EqualTo(walkedTo),
                "and answering it moves him again - the second instance is an action he performs, " +
                "not a decision that closes.");

            yield return CaptureGameView("cloak-and-daggers-repeats-the-action-he-just-took");
        }

        /// <summary>
        /// Clicks the first lit destination of the standing movement session, the way the walk journey
        /// does: the tile has to be lit before it is clicked, because a player can only click what the
        /// board is showing them.
        /// </summary>
        private static IEnumerator StepOntoTheFirstLitSpace(
            MatchSceneCompositionRoot root, PrimitiveHeroPointerInput pointerInput)
        {
            var runtime = root.Runtime;
            var destinations = runtime.PendingMovementChoice.LegalDestinations
                .Select(value => value.Destination)
                .ToArray();
            Assert.That(destinations, Is.Not.Empty,
                "A standing movement session lights somewhere to go. " + Remaining(runtime));
            var chosen = FirstClickableSpace(pointerInput, destinations);
            var tile = Tile(chosen);
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind,
                Is.EqualTo(GuardsDigital.Presentation.Board.HexCornerHighlightKind.Movement));
            Assert.That(UiPlayer.ClickBoard(pointerInput, root.Hud, tile.transform.position), Is.True);
            yield return null;
            yield return null;
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

        /// <summary>
        /// Milestone 8.23a: the hero who was given the marker reads it, and what it costs them, on
        /// their own rail.
        ///
        /// <para>This is the debt milestone 8.23 recorded. The click that gives the marker is
        /// <see cref="PoisonedDagger_GivesThePoisonMarkerToTheHeroItsBoardClickNames"/>'s subject;
        /// what this journey adds is the half that had nowhere to happen - the marker is drawn, and
        /// the three values its printed sentence lowers are drawn beside it.</para>
        ///
        /// <para><b>The reading is on the enemy's rail and the assertion says so in as many words.</b>
        /// Tigerclaw sits on the Blue seat and the marker goes to the Red hero he named, because p.18
        /// gives markers to other heroes and a marker property affects its HOLDER. A build that drew
        /// the marker on the rail of the hero who gave it would pass every test about "a marker is
        /// drawn" and would be telling the wrong player their values dropped, so both rails are read
        /// and only one of them may carry it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator APoisonedHero_ReadsTheMarkerAndItsThreeLostValuesOnTheirOwnRail()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Nobody is holding anything before the Skill is performed, on either rail. Without this
            // the journey could not tell a marker it caused from one the staging had already dealt.
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the staged position");
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "the staged position");

            yield return GivePoisonThroughTheBoard(root, tigerclawId, enemyId, pointerInput);

            // The whole of milestone 8.23a, in one assertion: the hero the marker was given to reads
            // its name and the three channels it lowers, drawn on the rail that is theirs.
            HeroRailMarkerCheck.AssertReadings(
                hud, TeamSide.Red, "the answered Poisoned Dagger",
                HeroRailMarkerCheck.PoisonOneReading);
            HeroRailMarkerCheck.AssertReadings(
                hud, TeamSide.Blue, "the answered Poisoned Dagger");

            // THE MEASUREMENT MILESTONE 8.23a LEAVES BEHIND, and it is a negative one.
            //
            // The acceptance check asks for the recomposed rail's height recorded against a third of
            // the hero-rail rect, so whether three fit is measured rather than claimed. At 8.23a it
            // was measured and did not fit: a rail carrying a held marker draws about 239 against a
            // budget of about 156 in the 491-tall rail rect the band above the tray left at 1920x1080.
            // The recomposition took a row out and that was not close to enough.
            //
            // **Milestone 8.25 closed it from the other side.** The shortfall was a fact about a
            // stacked column inside that band rather than about the roster, so the rails took the whole
            // screen edge and the tray took the band between the two columns; the same rail now stacks
            // three times inside a rect of about 1000. So this asserts both halves: the rail does not
            // grow back past the height 8.23a achieved, and three of them fit the column that has to
            // hold rulebook p.6's three heroes a side.
            // Milestone 8.25c: the column the renderer reserved, in the panel's own reference units -
            // which is what the rail's laid-out box below is measured in too. Reading the screen's
            // pixel count here would compare a rail drawn in reference units against a column measured
            // in device ones.
            var railRect = hud.RedHeroRailRect;
            var budget = HeroRailMarkerCheck.ThreeUpBudget(railRect.height);
            var drawn = HeroRailCoinCheck.BoxOf(hud, TeamSide.Red).height;
            TestContext.WriteLine(
                "8.25 rail measurement: a rail holding a marker is " + drawn + " tall; a third of " +
                    "the " + railRect.height + " rail rect leaves " + budget + " for one of three.");
            // Milestone 8.25c: the ceiling is a reference-unit claim and the measurement is taken off a
            // scaled panel, so it carries the panel's own pixel grid - one device pixel per stacked
            // row, stated by `UiPlayer.PixelSnapping`. What is allowed is the snapping; a rail that
            // grew a row still fails.
            var ceiling = HeroRailMarkerCheck.RecordedRailCeiling + UiPlayer.PixelSnapping(HeroRailMarkerCheck.RailRowCount);
            Assert.That(drawn, Is.LessThanOrEqualTo(ceiling),
                "The recomposed rail draws " + drawn + " against the " +
                    HeroRailMarkerCheck.RecordedRailCeiling + " this build measured (allowing " +
                    UiPlayer.PixelSnapping(HeroRailMarkerCheck.RailRowCount) + " for the panel's pixel grid), so a later " +
                    "change grew it back.");
            // Asserted wherever a third of the column could hold the recorded ceiling at all. A
            // viewport short enough that it cannot is one where three rails were never going to stack,
            // and the ceiling above is what guards the rail there.
            if (budget >= HeroRailMarkerCheck.RecordedRailCeiling)
            {
                Assert.That(drawn, Is.LessThanOrEqualTo(budget),
                    "A rail holding a marker draws " + drawn + " against the " + budget + " a third " +
                        "of the " + railRect.height + " column leaves, so three do not stack for " +
                        "p.6's three heroes a side.");
            }
            else
            {
                TestContext.WriteLine(
                    "Three-up budget not asserted at " + Screen.width + "x" + Screen.height +
                        ": a third of the " + railRect.height + " column is " + budget + ".");
            }

            yield return CaptureGameView("a-poisoned-hero-reads-the-marker-on-their-own-rail");
        }

        /// <summary>
        /// Milestone 8.23a: the reading follows the attachment's own lifetime rather than a card's.
        ///
        /// <para>Four reconstructions and one ending, which is the milestone's acceptance check
        /// stated as one journey. A snapshot restore, a presentation rebuild and a retained-prefix
        /// Undo each rebuild the register the reading is derived from, so each must redraw the same
        /// row with nothing stale left behind - and p.8's "Remove Tokens, Return Markers" must take
        /// it away, because the sentence is printed in the present tense about the holder and stops
        /// being true when the marker goes back to the supply.</para>
        ///
        /// <para>The Undo half is the one worth stating plainly: undoing the command that gave the
        /// marker must remove the row, because a reading that survived its own cause would be
        /// presentation holding state the authority no longer has - which is exactly the accounting
        /// this milestone's acceptance check forbids.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ThePoisonReading_SurvivesRestoreReplayAndUndo_AndEndsWhenTheRoundReturnsTheMarker()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return GivePoisonThroughTheBoard(root, tigerclawId, enemyId, pointerInput);
            HeroRailMarkerCheck.AssertReadings(
                hud, TeamSide.Red, "the answered Poisoned Dagger",
                HeroRailMarkerCheck.PoisonOneReading);

            // Presentation rebuild: a redrawn HUD reconstructs the row from the projection alone,
            // with no second row left behind by the one it replaced.
            runtime.RefreshAll();
            yield return null;
            HeroRailMarkerCheck.AssertReadings(
                hud, TeamSide.Red, "a rebuilt presentation",
                HeroRailMarkerCheck.PoisonOneReading);

            // The position is captured before the Undo rather than after it, and the order is
            // load-bearing: a restore reconstructs the session from the payload and leaves it with no
            // accepted command history, so an Undo attempted afterwards is refused for having nothing
            // to undo. Milestone 8.23a learned that from a red run reading `Round 1, turn 2: awaiting
            // commitments` - the state was right and the history was gone.
            var captured = runtime.CaptureSnapshot();

            // Undo: the retained prefix puts the position back before the marker was given, and the
            // row goes with the attachment rather than outliving it.
            Assert.That(runtime.Undo(), Is.True,
                "The command that gave the marker is undoable. " + Remaining(runtime));
            yield return null;
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "a retained-prefix Undo");
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "a retained-prefix Undo");

            // Restore: the same position out of bytes redraws the same row, which is also what proves
            // the Undo removed a reading rather than broke one.
            Assert.That(runtime.RestoreSnapshot(captured), Is.True);
            yield return null;
            HeroRailMarkerCheck.AssertReadings(
                hud, TeamSide.Red, "a restored position",
                HeroRailMarkerCheck.PoisonOneReading);
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "a restored position");

            // p.8's end of round returns every marker, and the reading ends with it.
            yield return CrossTheRoundBoundary(runtime);
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the round after p.8 returned it");
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "the round after p.8 returned it");

            yield return CaptureGameView("the-poison-reading-ends-when-the-round-returns-the-marker");
        }

        /// <summary>
        /// Plays every remaining turn of the current round and its whole end-of-round stage list, so a
        /// caller can reach what rulebook p.8 does at a round boundary. Milestone 8.23a.
        ///
        /// <para>The existing pair - <see cref="TigerclawEngagedUiJourneyPlayModeTests.YieldUntilNobodyIsActing"/>
        /// then <see cref="TigerclawEngagedUiJourneyPlayModeTests.AdvanceToNextTurn"/> - reaches the
        /// next TURN, and a round is four of them. A caller wanting the boundary has to keep going
        /// until the round index actually moves, which is what this loops on and what it asserts:
        /// milestone 8.23a's first red run read the marker still held, because the round had only
        /// reached turn two and p.8's Return Markers had not run at all.</para>
        /// </summary>
        private static IEnumerator CrossTheRoundBoundary(ConfiguredMatchSceneRuntime runtime)
        {
            var startedIn = runtime.RoundIndex;
            for (var budget = 0; runtime.RoundIndex == startedIn; budget++)
            {
                Assert.That(budget, Is.LessThan(40),
                    "A round has to end within a bounded number of turns and stage answers. " +
                    Remaining(runtime));

                if (runtime.Phase == RoundResolutionPhase.EndOfRound)
                {
                    yield return AdvanceToNextTurn(runtime);
                    continue;
                }

                // Milestone 8.35g. A turn held for its own player is somebody acting, even though the
                // selection is empty - reading only the selection would fall through to the
                // commitment arm below and try to commit into a round that is still resolving.
                if (runtime.SelectedActor.IsDefined || runtime.PendingTurnRelease != null)
                {
                    yield return YieldUntilNobodyIsActing(runtime);
                    continue;
                }

                // Awaiting the next turn's commitments: every seat commits whatever it still holds,
                // which spends the round without any of it being this journey's subject.
                foreach (var seat in runtime.Setup.Heroes.Select(hero => hero.HeroId).ToArray())
                {
                    var held = runtime.Cards
                        .Where(card => card.OwnerId.Equals(seat) &&
                            card.State.Location == CardLocation.Hand)
                        .OrderBy(card => card.Id.Value, System.StringComparer.Ordinal)
                        .FirstOrDefault();
                    if (held == null)
                    {
                        continue;
                    }

                    Assert.That(runtime.SelectCommitmentCard(held.Id), Is.True);
                    Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
                }

                yield return null;
            }

            yield return null;
        }

        /// <summary>
        /// Plays Poisoned Dagger and answers its one board question, which is the setup both readings
        /// above stand on rather than the subject of either.
        ///
        /// <para>It is the same sequence
        /// <see cref="PoisonedDagger_GivesThePoisonMarkerToTheHeroItsBoardClickNames"/> asserts its way
        /// through, driven here without those assertions: that journey owns proving the click reaches
        /// the rules, and these two own what the rules then put on the screen.</para>
        /// </summary>
        private static IEnumerator GivePoisonThroughTheBoard(
            MatchSceneCompositionRoot root,
            HeroId tigerclawId,
            HeroId enemyId,
            PrimitiveHeroPointerInput pointerInput)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.PoisonedDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, tigerclawId);
            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            var target = StandingNode(runtime);
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, HeroView(enemyId).transform.position),
                Is.True,
                "The hero who receives the marker is named by clicking them.");
            yield return null;
            yield return ConfirmIfStillStanding(root, target);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The answered selection completes the action. " + Remaining(runtime));
        }
    }
}
